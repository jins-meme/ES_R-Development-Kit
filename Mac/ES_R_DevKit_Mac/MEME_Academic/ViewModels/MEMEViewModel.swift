//
//  MEMEViewModel.swift
//  MEME_Academic
//
//  SwiftUI 用 ViewModel。
//  状態管理・アクション・delegate 振り分けに専念し、
//  CSV 保存／通信統計は専用サービスへ委譲する。
//  グラフは WebView(webview/ の標準版 zip、または設定で選んだ zip)が描く。受信したサンプルは
//  WebBridge で流し、CSV 再生もページが受け持つ(アプリはファイルを仮想ホストに出すだけ)。
//  アーティファクトはページで入力され、WebBridge.onArtifact で届く。CSV への書き戻しは従来どおり停止時。
//

import Foundation
import Observation
import CoreBluetooth
import AppKit
import SwiftUI
import UniformTypeIdentifiers

@MainActor
@Observable
final class MEMEViewModel: NSObject {

    // MARK: - Phase

    enum Phase {
        case idle
        case deviceFound
        case connected
        case measuring
        case replaying
    }

    // MARK: - Static options

    let selectModeOptions = MeasurementMode.allCases.map(\.label)
    let transSpeedOptions = MeasurementRange.transHz.map { "\($0)Hz" }
    let accelRangeOptions = MeasurementRange.accelG.map { "±\($0)G" }
    let gyroRangeOptions = MeasurementRange.gyroDps.map { "±\($0)dps" }

    // MARK: - Observable state

    var phase: Phase = .idle

    // Scan / Connect
    var foundDevices: [String] = []
    var selectedDevice: String = ""
    var isScanning: Bool = false
    var isConnecting: Bool = false
    var connectionStateText: String = "State : Disconnected"
    var memeVersionText: String = "MEME Version："

    // Settings selectors
    var selectMode: Int = 0
    var transSpeed: Int = 0
    var accelRange: Int = 0
    var gyroRange: Int = 0

    // Stats
    var successRateText: String = "0.0%"
    var successRateValue: Double = 0
    var communicationText: String = "0.0%"
    var communicationValue: Double = 0

    // App / Network info
    var appVersionText: String = ""
    var localAddressText: String = "IP address:"
    var localPortText: String = "Port:"
    var socketStatusText: String = "Status : "

    // Settings sheet presentation
    var showingSettings: Bool = false

    // Shelf mode（Disconnect 長押しで開く確認ダイアログ）
    var showingShelfDialog: Bool = false
    /// Shelf 移行コマンドの送信中。完了は端末側からの切断で判断する。
    var isEnteringShelf: Bool = false

    /// グラフ画面(WebView)。中身の切り替えは設定から(WebContentStore)。
    let web = WebBridge()

    // MARK: - Private state

    private var memelib: (any MEMELibInterface)!
    private var isFreeMarking = false

    private var peripheralManager: CBPeripheralManager?
    private var socket: TCPSocket?
    private var socketDatas: [CsvRow] = []

    /// 再生中の CSV(ページが読んで再生する)
    private var replayFile: URL?

    /// 計測中のサンプル番号(計測開始から 0, 1, 2 …。先頭パケットも数える)。グラフ画面へ渡し、
    /// アーティファクトはこの番号で返ってくる。CSV は先頭パケットを 1 件落とすので、データ行 = 番号 − 1。
    private var liveSampleIndex = 0

    /// ページで付けた Artifact(計測中はサンプル番号、再生中は CSV のデータ行の番号)。停止時にCSVへ書き戻す。
    private var pendingArtifacts = ArtifactBuffer()

    /// SHELF 送信後、端末が自ら切断するのを待つタイマー。切断が来たら無効化する。
    private var shelfDisconnectTimer: Timer?

    // MARK: - Services

    private let persistence = DataPersistenceService()
    private let stats = CommunicationStatsTracker()
    /// 判定器の通知と演算結果の表(ページ → アプリの notify / table / records)。ライブ計測の間だけ受ける
    let outputs = DetectorOutputs()

    /// 計測を止めてから判定器の表を閉じるまで待つ時間。ページは stop を受けてから溜めた行(最大 1 秒ぶん)を送るので、
    /// それが届くのを待つ(webview/BRIDGE.md の Detector notifications and tables。Android と同じ 1 秒)
    private static let outputsStopGrace: TimeInterval = 1.0

    // MARK: - Init

    override init() {
        super.init()
        UserSetting.fristSetting()
        memelib = MEMELibFactory.make()
        memelib.delegate = self

        stats.onSuccessRate = { [weak self] value, text in
            self?.successRateValue = value
            self?.successRateText = text
        }
        stats.onCommunication = { [weak self] value, text in
            self?.communicationValue = value
            self?.communicationText = text
        }

        web.onArtifact = { [weak self] i, text in self?.receiveArtifact(i: i, text: text) }
        web.onReplayInfo = { [weak self] info in self?.applyReplayInfo(info) }
        web.onOutput = { [weak self] body in self?.outputs.receive(body) }

        showAppVersion()
        showLocalAddress()
        showLocalPort()
        socketStart()
    }

    // MARK: - App version / Network info

    private func showAppVersion() {
        let version = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? ""
        let build = Bundle.main.object(forInfoDictionaryKey: "CFBundleVersion") as? String ?? ""
        appVersionText = "Version \(version).\(build)"
    }

    private func showLocalAddress() {
        localAddressText = "IP address:\(Common.getIPAddress())"
    }

    private func showLocalPort() {
        localPortText = "Port:\(UserSetting.getLocalPort())"
    }

    // MARK: - Reset

    private func reset() {
        persistence.reset()
        stats.reset()
        socketDatas = []
        socketStatusText = "Status : "
        isFreeMarking = false
        pendingArtifacts.removeAll()
    }

    // MARK: - Scan / Connect actions

    func toggleScan() {
        if isScanning {
            stopScan()
        } else {
            startScan()
        }
    }

    func startScan() {
        NSLog("Call : startScanningPeripherals")
        isScanning = true
        connectionStateText = "State : Scanning..."
        foundDevices.removeAll()
        selectedDevice = ""
        if MEMELibFactory.isMock {
            memelib.startScanningPeripherals()
            return
        }
        // CBPeripheralManager は BT 状態通知（OFF時のシステムアラート）用に
        // 1回だけ生成し以降使い回す。state が既に .poweredOn なら即スキャンを開始し、
        // それ以外（.unknown/.resetting/.poweredOff など）は
        // peripheralManagerDidUpdateState 経由でスキャンを起動する。
        if peripheralManager == nil {
            peripheralManager = CBPeripheralManager(delegate: self,
                                                    queue: nil,
                                                    options: [CBPeripheralManagerOptionShowPowerAlertKey: "YES"])
        }
        if peripheralManager?.state == .poweredOn {
            memelib.startScanningPeripherals()
        }
    }

    func stopScan() {
        NSLog("Call : stopScanningPeripherals")
        memelib.stopScanningPeripherals()
        isScanning = false
        foundDevices.removeAll()
        selectedDevice = ""
        connectionStateText = "State : Disconnected"
        phase = .idle
    }

    func toggleConnect() {
        // Shelf mode の確認中／移行中は触らせない。長押しが成立したジェスチャの
        // クリックがここへ届いても切断しないための保険でもある（ConnectButton 参照）。
        guard !showingShelfDialog, !isEnteringShelf else { return }
        if phase == .replaying {
            disconnectReplay()
            return
        }
        if isConnected {
            NSLog("Call : disconnectPeripheral")
            memelib.disconnectPeripheral()
        } else {
            guard !selectedDevice.isEmpty else { return }
            NSLog("Call : connectPeripheral")
            isConnecting = true
            memelib.connectPeripheral(deviceName: selectedDevice)
        }
    }

    // MARK: - Shelf mode

    /// Disconnect の 5 秒長押しで確認ダイアログを出す時間。
    static let shelfLongPressSeconds: Double = 5

    /// SHELF 送信後、端末が自ら切断するのを待つ時間（切断＝移行成功）。
    private static let shelfDisconnectTimeout: TimeInterval = 5

    /// Shelf mode へ移行できる状態か。SHELF コマンドは実機に接続済みで計測していない
    /// ときだけ受理されるので、モック（CSV再生用のダミー）と計測中は対象外。
#if DEBUG
    /// 接続中の端末のアドレス(接続後に端末から読む。自己テストが繋いだ端末を確かめる用)
    var connectedMacAddress: String { memelib?.macAddress ?? "" }
#endif

    var canEnterShelfMode: Bool {
        phase == .connected && !MEMELibFactory.isMock && !isEnteringShelf && !isConnecting
    }

    /// Disconnect の長押しで Shelf mode の確認ダイアログを開く。
    /// 移行できる状態でなければ何も起きない（隠し操作なので通知もしない）。
    func requestShelfMode() {
        guard canEnterShelfMode else { return }
        showingShelfDialog = true
    }

    func cancelShelfMode() {
        showingShelfDialog = false
    }

    /// 端末を Shelf mode（保管モード）へ移行させる。CONFIG モードへの遷移が
    /// 受理されてから SHELF が送られ、受理されると端末は自ら切断する。
    /// 復帰は充電のみで、アプリからは戻せない。
    func confirmShelfMode() {
        showingShelfDialog = false
        guard canEnterShelfMode else { return }
        isEnteringShelf = true
        connectionStateText = "State : Entering shelf mode..."
        memelib.enterShelfMode { [weak self] sent in
            guard let self else { return }
            guard sent else {
                // SHELF はまだ送っていないので端末は通常モードのまま。
                self.finishShelfMode(entered: false)
                return
            }
            // SHELF は送信済み。端末側からの切断が来れば成功。
            self.shelfDisconnectTimer = Timer.scheduledTimer(
                withTimeInterval: Self.shelfDisconnectTimeout, repeats: false) { [weak self] _ in
                Task { @MainActor in
                    self?.finishShelfMode(entered: false)
                }
            }
        }
    }

    /// Shelf 移行の結果を確定して知らせる（成功＝端末が切断した、失敗＝ACK 無し／切断待ちタイムアウト）。
    private func finishShelfMode(entered: Bool) {
        guard isEnteringShelf else { return }
        shelfDisconnectTimer?.invalidate()
        shelfDisconnectTimer = nil
        isEnteringShelf = false
        if !entered && isConnected {
            connectionStateText = "State : Connected"
        }
        // 成功時はここが CoreBluetooth の切断コールバックの中なので、
        // モーダルを積む前に delegate を抜けさせる。
        DispatchQueue.main.async {
            let alert = NSAlert()
            alert.alertStyle = entered ? .informational : .warning
            alert.messageText = entered ? "Entered shelf mode" : "Failed to enter shelf mode"
            alert.informativeText = entered
                ? "To exit shelf mode, please recharge the device."
                : "The device is still in normal mode."
            alert.runModal()
        }
    }

    // MARK: - File Replay actions

    func chooseReplayFile() {
        // BLE 接続中は再生に入れない。
        guard !isConnected else { return }
        prepareForReplay()
        let panel = NSOpenPanel()
        panel.canChooseFiles = true
        panel.canChooseDirectories = false
        panel.allowsMultipleSelection = false
        panel.level = .modalPanel
        // .csv と .csv.gz の両方を選べるようにする。
        panel.allowedContentTypes = CsvFile.openPanelTypes
        guard let window = NSApp.mainWindow else { return }
        panel.beginSheetModal(for: window) { [weak self] result in
            guard let self else { return }
            guard result == .OK, let url = panel.url else { return }
            self.loadReplayFile(url: url)
        }
    }

    /// Finder の「このアプリで開く」など、外部から渡されたCSVを File Replay として読み込む。
    func openReplayFile(url: URL) {
        // BLE 接続中／計測中は再生に入れない。
        guard !isConnected else {
            let alert = NSAlert()
            alert.alertStyle = .warning
            alert.messageText = "Cannot open file while connected"
            alert.informativeText = "Disconnect the BLE device before opening a CSV for replay."
            alert.runModal()
            return
        }
        prepareForReplay()
        loadReplayFile(url: url)
    }

    /// 再生に入る前に、スキャン中なら止め、前の再生があれば(Artifact を書き戻して)閉じる。
    private func prepareForReplay() {
        if isScanning {
            stopScan()
        }
        if phase == .replaying {
            disconnectReplay()
        }
    }

    /// 再生はグラフ画面(ページ)が受け持つ: ファイルを仮想ホストに出してページに読ませる。
    /// 読み込み・再生・一時停止・速度・シークはページ側。形式が違えばページがその旨を表示する。
    private func loadReplayFile(url: URL) {
        replayFile = url
        pendingArtifacts.removeAll()
        connectionStateText = "State : \(url.lastPathComponent)"
        phase = .replaying
        web.openReplay(file: url, extra: displayOptions())
    }

    /// ページが CSV を読み終えたら、計測条件の表示(左の欄)を CSV に合わせる。
    private func applyReplayInfo(_ info: [String: Any]) {
        guard phase == .replaying else { return }
        if let name = info["mode"] as? String, let m = MeasurementMode(pageName: name) { selectMode = m.pickerIndex }
        if let c = (info["cps"] as? NSNumber)?.intValue, let k = MeasurementRange.transHz.firstIndex(of: c) { transSpeed = k }
        if let g = (info["accRange"] as? NSNumber)?.intValue, let k = MeasurementRange.accelG.firstIndex(of: g) { accelRange = k }
        if let d = (info["gyroRange"] as? NSNumber)?.intValue, let k = MeasurementRange.gyroDps.firstIndex(of: d) { gyroRange = k }
    }

    /// 再生中に付けた Artifact を、今すぐ再生元CSVへ書き戻す(再生は続ける)。
    func saveReplayArtifacts() {
        guard phase == .replaying else { return }
        flushArtifacts()
    }

    private func disconnectReplay() {
        // 再生中に切断された場合も、記録済み Artifact は書き戻す。
        flushArtifacts()
        web.closeReplay()
        replayFile = nil
        phase = .idle
        connectionStateText = "State : Disconnected"
        reset()
    }

    // MARK: - Artifact

    /// ページで付けた Artifact を控える(無害化と番号の扱いは ArtifactBuffer)。
    private func receiveArtifact(i: Int, text: String) {
        guard phase == .replaying || phase == .measuring else { return }
        pendingArtifacts.add(i: i, text: text)
    }

    /// 再生中に付けた Artifact を再生元CSVの ARTIFACT 列へ書き戻す(Save Artifacts / 切断時)。
    private func flushArtifacts() {
        pendingArtifacts.flush(to: replayFile, numbering: .csvRow)
    }

    /// 計測中に付けた Artifact を、保存済みCSVの ARTIFACT 列へ書き戻す（停止時）。
    private func flushLiveArtifacts() {
        pendingArtifacts.flush(to: persistence.savedFileURL, numbering: .liveSample)
    }

    // MARK: - Measurement

    func toggleMeasurement() {
        if phase != .measuring {
            startMeasurement()
        } else {
            stopMeasurement()
        }
    }

    private func startMeasurement() {
        stats.startMeasurement(quality: transSpeed + 1)

        memelib.setSelectMode(MeasurementMode(pickerIndex: selectMode).rawValue)
        memelib.setTransMode(UInt32(transSpeed + 1))
        memelib.setAccelRange(UInt32(accelRange))
        memelib.setGyroRange(UInt32(gyroRange))

        if let socket = socket {
            socket.headerString = headerString()
            socket.writeHeader()
        }

        liveSampleIndex = 0
        outputs.start(cps: MeasurementRange.hz(quality: UInt32(transSpeed + 1)),
                      dataFile: { [weak self] in self?.persistence.savedFileURL },
                      pageName: { [weak self] in self?.web.pageName ?? "" })
        web.start(liveCondition())

        phase = .measuring
        memelib.startDataReport()
    }

    private func stopMeasurement() {
        memelib.stopDataReport()
        stats.stopMeasurement()
        web.stop()
        closeOutputsLater()

        // 届きかけのパケットを待ってから締める。
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) { [weak self] in
            guard let self = self else { return }
            // 待つ間に切断されていたら(phase は .idle)、切断の側で締めてある。
            guard self.phase == .measuring else { return }
            self.phase = .connected
            self.finishMeasurement()
        }
    }

    /// 計測を締める(停止のとき・計測中に切断されたとき): CSV を書き切り、計測中に付けた Artifact を
    /// 書き戻し、設定なら保存ダイアログを出して、次の計測が新しいファイルになるよう状態を戻す。
    private func finishMeasurement() {
        flushCsv()
        outputs.noteDataFile()      // 表はこの後(ページの残りを待って)閉じるので、データ CSV の場所を覚えさせる
        // 確定したCSVファイルへ、計測中に付けた Artifact を書き戻す。
        // （保存ダイアログでファイルを移動する前に、元パスへ書き込んでおく。）
        flushLiveArtifacts()

        if UserSetting.getShowSaveFileDialog() {
            persistence.presentSaveDialog { [weak self] from, to in self?.outputs.dataFileMoved(from: from, to: to) }
        } else {
            persistence.resetCsvManager()
        }
        reset()
    }

    /// 判定器の表は、ページが stop の後に送る残りを待ってから閉じる(閉じる前に次の計測が始まっていたら何もしない)
    private func closeOutputsLater() {
        let s = outputs.session
        DispatchQueue.main.asyncAfter(deadline: .now() + Self.outputsStopGrace) { [weak self] in
            self?.outputs.stop(session: s)
        }
    }

    func toggleFreeMarking() {
        isFreeMarking = true
    }

    // MARK: - Graph (WebView)

    /// 表示の設定(時刻の表示・加速度のオフセット・テーマ)。計測・再生どちらでもページへ渡す。
    private func displayOptions() -> [String: Any] {
        let dark = NSApp.effectiveAppearance.bestMatch(from: [.darkAqua, .aqua]) == .darkAqua
        return [
            "timeZone": UserSetting.getConvertToLocalTime() ? "local" : "utc",
            "accOffset": [UserSetting.getXAxis(), UserSetting.getYAxis(), UserSetting.getZAxis()],
            "theme": dark ? "dark" : "light",
        ]
    }

    /// 計測の開始時にページへ渡す条件(webview/BRIDGE.md の start)。値は端末に設定したもの。
    private func liveCondition() -> [String: Any] {
        let mode = MeasurementMode(rawValue: memelib.getSelectMode()) ?? .standard
        var cond = displayOptions()
        cond["label"] = selectedDevice.isEmpty ? "JINS MEME" : selectedDevice
        cond["mode"] = mode.pageName
        cond["cps"] = MeasurementRange.hz(quality: memelib.getTransMode())
        cond["accRange"] = MeasurementRange.accelG(device: memelib.getAccelRange())
        cond["gyroRange"] = MeasurementRange.gyroDps(device: memelib.getGyroRange())
        cond["columns"] = mode.hasGraph ? mode.columns : []
        cond["startedAt"] = Date().timeIntervalSince1970 * 1000
        cond["features"] = DetectorOutputs.features
        return cond
    }

    /// 1 サンプルをグラフ画面へ。freeMarked なら、CSV に X を書いた行と同じ位置へ印を出す。
    private func pushToGraph(_ data: AcademicData, freeMarked: Bool) {
        let i = liveSampleIndex
        liveSampleIndex += 1
        if let mode = MeasurementMode(of: data), mode.hasGraph, let values = mode.values(of: data) {
            web.push(i: i, values: values)
        }
        if freeMarked {
            web.mark(i: i, text: "X")
        }
    }

    /// グラフ画面の中身が切り替わったら読み込み直す(設定から)。
    func reloadGraph() {
        web.load()
    }

    // MARK: - Settings sheet

    func openSettings() {
        showingSettings = true
    }

    func settingsDidApply() {
        socketStop()
        socketStart()
        showLocalPort()
    }

    // MARK: - CSV / Socket

    private func saveCsv() { saveCsvIfNeeded(force: false) }
    private func flushCsv() { saveCsvIfNeeded(force: true) }

    private func saveCsvIfNeeded(force: Bool) {
        persistence.saveIfNeeded(force: force,
                                 macAddress: memelib.macAddress,
                                 quality: max(stats.quality, 1),
                                 mode: memelib.getSelectMode(),
                                 header: headerString())
    }

    private func writeSocket() {
        if socketDatas.count >= 10 {
            var buffer = ""
            persistence.dataToStoring(socketDatas, stringBuffer: &buffer, mode: memelib.getSelectMode())
            socket?.writeData(buffer)
            socketDatas.removeAll()
        }
    }

    private func headerString() -> String {
        DataPersistenceService.headerString(mode: memelib.getSelectMode(),
                                            transMode: memelib.getTransMode(),
                                            accelRange: memelib.getAccelRange(),
                                            gyroRange: memelib.getGyroRange())
    }

    // MARK: - Socket

    private func socketStart() {
        if UserSetting.getExtermalOutputSocket() {
            let s = TCPSocket()
            s.delegate = self
            s.headerString = headerString()
            let status = s.start()
            socketStatusText = "Status : \(status)"
            socket = s
        }
    }

    private func socketStop() {
        socket?.stop()
        socket = nil
        socketStatusText = "Status : "
    }

    // MARK: - AUP_REPORT_MODE / AUP_REPORT_6AXIS_PRMS

    private func syncDeviceSettings() {
        if let mode = MeasurementMode(rawValue: memelib.getSelectMode()) { selectMode = mode.pickerIndex }
        let transIdx = Int(memelib.getTransMode()) - 1
        if (0..<transSpeedOptions.count).contains(transIdx) { transSpeed = transIdx }
        let accelIdx = Int(memelib.getAccelRange())
        if (0..<accelRangeOptions.count).contains(accelIdx) { accelRange = accelIdx }
        let gyroIdx = Int(memelib.getGyroRange())
        if (0..<gyroRangeOptions.count).contains(gyroIdx) { gyroRange = gyroIdx }
    }

    // MARK: - Phase computed convenience

    /// 実機(またはモック)に接続済み(計測中を含む)
    private var isConnected: Bool { phase == .connected || phase == .measuring }

    var showScanButton: Bool { phase == .idle || phase == .deviceFound }
    var scanButtonLabel: String { isScanning ? "Stop Scan" : "Start Scan" }
    // BLE デバイス接続中（接続完了 or 計測中）以外は常に表示する。
    var showFileReplay: Bool { !isConnected }
    // スキャン中はデバイス選択（(no device) 表示）を触らせない。
    // デバイスが見つかったら選択できるようにする。
    var isDeviceSelectionDisabled: Bool { isInputDisabled || (isScanning && phase != .deviceFound) }
    var showConnect: Bool { phase == .deviceFound || phase == .connected || phase == .replaying }
    var connectButtonLabel: String {
        (phase == .connected || phase == .replaying) ? "Disconnect" : "Connect"
    }
    var showMeasurement: Bool { isConnected }
    var showFreeMarking: Bool { phase == .measuring }
    /// 再生中は「Save Artifacts」(付けた Artifact を今すぐ CSV へ書き戻す)を出す。
    /// 再生・一時停止・速度・シークはグラフ画面の中にある。
    var showReplayControls: Bool { phase == .replaying }
    var isInputDisabled: Bool { phase == .measuring || phase == .replaying }
}

// =============================================================================
// MARK: - CBPeripheralManagerDelegate
// =============================================================================
extension MEMEViewModel: @preconcurrency CBPeripheralManagerDelegate {
    func peripheralManagerDidUpdateState(_ peripheral: CBPeripheralManager) {
        if peripheral.state == .poweredOn {
            NSLog("bluetooth ON")
            // startScan で連発される foundDevices クリアは行わない。
            // 既に startScan 側で初期化済みのため。
            memelib.startScanningPeripherals()
        } else {
            NSLog("bluetooth それ以外")
            connectionStateText = "State : Bluetooth is off"
            let alert = NSAlert()
            alert.messageText = "端末のBluetoothをオンにしてください"
            alert.informativeText = ""
            alert.runModal()
        }
    }
}

// =============================================================================
// MARK: - MEMELibAcademicDelegate
// =============================================================================
extension MEMEViewModel: MEMELibAcademicDelegate {

    func memePeripheralFoundDelegate(result: UInt32, deviceName: String?, uuid: String?) {
        if result == MEMELIB_OK {
            NSLog("memePeripheralFoundDelegate %d %@ %@", result, deviceName ?? "", uuid ?? "")
            if let name = deviceName, !foundDevices.contains(name) {
                foundDevices.append(name)
                selectedDevice = name
            }
            connectionStateText = "State : Device found"
            phase = .deviceFound
        } else {
            NSLog("memePeripheralFoundDelegate %d", result)
            memelib.stopScanningPeripherals()
            isScanning = false
            connectionStateText = "State : Scan timeout. Tap Start Scan to retry."
        }
    }

    func memePeripheralConnectedDelegate(result: UInt32) {
        NSLog("memePeripheralConnectedDelegate : %d", result)
        isConnecting = false
        guard result == MEMELIB_OK else {
            connectionStateText = "State : Connect failed"
            return
        }
        isScanning = false
        connectionStateText = "State : Connected"
        memeVersionText = "MEME Version：\(memelib.memeVersion.major).\(memelib.memeVersion.minor).\(memelib.memeVersion.revision)"
        phase = .connected
        syncDeviceSettings()
    }

    func memePeripheralDisconnectedDelegate(result: UInt32) {
        NSLog("memePeripheralDisconnectedDelegate : %d", result)
        let wasMeasuring = phase == .measuring
        isConnecting = false
        isScanning = false
        connectionStateText = "State : Disconnected"
        foundDevices.removeAll()
        selectedDevice = ""
        phase = .idle
        web.stop()
        // 計測中に切れた(電池切れ・電波など)ときも、停止と同じく締める。
        // 締めないと次の計測が同じ CSV に書き足され、NUM も続きから数えられてしまう。
        if wasMeasuring {
            NSLog("disconnected while measuring; finishing the measurement")
            stats.stopMeasurement()
            closeOutputsLater()
            finishMeasurement()
        }
        // SHELF 送信後の切断は端末が移行を受理した合図。
        if isEnteringShelf {
            finishShelfMode(entered: true)
        }
    }

    func memeAcademicStandardDataReceivedDelegate(data: AcademicStandardData) {
        let freeMarked = ingestPacket(data: data)
        pushToGraph(data, freeMarked: freeMarked)
    }

    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData) {
        let freeMarked = ingestPacket(data: data)
        pushToGraph(data, freeMarked: freeMarked)
    }

    func memeAcademicQuaternionDataReceivedDelegate(data: AcademicQuaternionData) {
        // Quaternion はグラフを持たないので、Free Marking は CSV に書くだけで印は出さない。
        _ = ingestPacket(data: data)
    }

    /// 受信パケットを CSV／ソケットへ流す。このパケットの行へ Free Marking の X を書いたら
    /// true を返す（呼び出し側がグラフの同じサンプル位置へ印を出す）。
    private func ingestPacket(data: AcademicData) -> Bool {
        // 受信時刻をサンプル自身に持たせる。CSV/ソケットの DATE 列はこの時刻を使う。
        data.date = Date()
        var freeMarked = false
        // 最初の1パケットは前回カウンタの基準取得のみに使い、CSVには記録しない。
        // Free Marking のフラグも消費せず、記録される次のパケットへ持ち越す。
        if stats.registerPacket(count: Int(data.cnt)) {
            freeMarked = isFreeMarking
            isFreeMarking = false
            let row = CsvRow(data: data, packetCount: stats.totalCount, date: data.date ?? Date(), isFreeMarking: freeMarked)
            persistence.append(row)
            // 判定器の表の NUM / DATE 列(このパケットがページへ渡る番号 = liveSampleIndex。pushToGraph はこの後)
            outputs.recordSample(i: liveSampleIndex, num: row.packetCount, date: row.date)
            saveCsv()
            if socket?.isConnected() == true {
                socketDatas.append(row)
                writeSocket()
            }
        }
        stats.bumpDataCount()
        return freeMarked
    }
}

// =============================================================================
// MARK: - TcpSocketDelegate
// =============================================================================
extension MEMEViewModel: TcpSocketDelegate {
    func didAccept() {
        NSLog("didAccept")
        socketStatusText = "Status : Accept"
    }

    func socketDidDisconnect(error: Error?) {
        NSLog("didDisconnect")
        socketStatusText = "Status : "
        socketStart()
    }
}
