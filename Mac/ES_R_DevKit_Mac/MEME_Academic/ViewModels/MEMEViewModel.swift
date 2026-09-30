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

    let selectModeOptions = ["Standard", "Full", "Quaternion"]
    let transSpeedOptions = ["100Hz", "50Hz"]
    let accelRangeOptions = ["±2G", "±4G", "±8G", "±16G"]
    let gyroRangeOptions = ["±250dps", "±500dps", "±1000dps", "±2000dps"]
    /// accelRange / gyroRange の番号 → g / dps(グラフの換算に渡す)
    private static let accelG = [2, 4, 8, 16]
    private static let gyroDps = [250, 500, 1000, 2000]

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

    // Latest data display values
    var displayCnt: UInt32 = 0
    var displayAccX: Int16 = 0
    var displayAccY: Int16 = 0
    var displayAccZ: Int16 = 0
    var displayGyroX: Int16 = 0
    var displayGyroY: Int16 = 0
    var displayGyroZ: Int16 = 0
    var displayEogL: Int16 = 0
    var displayEogR: Int16 = 0
    var displayEogH: Int16 = 0
    var displayEogV: Int16 = 0
    var displayBattLv: UInt16 = 0

    // Stats
    var successRateText: String = "0.0%"
    var successRateValue: Double = 0
    var communicationText: String = "0.0%"
    var communicationValue: Double = 0

    // App / Network info
    var appVersionText: String = ""
    var localAddressText: String = "IP address:"
    var localPortText: String = "Prot:"
    var socketStatusText: String = "Status : "

    // Settings sheet presentation
    var showingSettings: Bool = false

    // Shelf mode（Disconnect 長押しで開く確認ダイアログ）
    var showingShelfDialog: Bool = false
    /// Shelf 移行コマンドの送信中。完了は端末側からの切断で判断する。
    var isEnteringShelf: Bool = false

    /// グラフ画面(WebView)。中身の切り替えは設定から(WebContentStore)。
    let web = WebBridge()
    /// グラフ画面に今載っている中身(設定に出す)
    var webContentText: String = ""

    // MARK: - Private state

    private var memelib: (any MEMELibInterface)!
    private var connectedFlag = false
    private var measurementFlag = false
    private var isFreeMarking = false

    private var peripheralManager: CBPeripheralManager?
    private var socket: TCPSocket?
    private var socketDatas: [[String: Any]] = []

    /// 再生中の CSV(ページが読んで再生する)
    private var replayFile: URL?

    /// 計測中のサンプル番号(計測開始から 0, 1, 2 …。先頭パケットも数える)。グラフ画面へ渡し、
    /// アーティファクトはこの番号で返ってくる。CSV は先頭パケットを 1 件落とすので、データ行 = 番号 − 1。
    private var liveSampleIndex = 0

    /// ページで付けた Artifact(計測中はサンプル番号、再生中は CSV のデータ行の番号 → 文字列)。停止時にCSVへ書き戻す。
    private var pendingArtifacts: [Int: String] = [:]

    /// SHELF 送信後、端末が自ら切断するのを待つタイマー。切断が来たら無効化する。
    private var shelfDisconnectTimer: Timer?

    // MARK: - Services

    private let persistence = DataPersistenceService()
    private let stats = CommunicationStatsTracker()

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
        web.onReady = { [weak self] name, version in self?.webContentText = "\(name) \(version)" }

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
        localPortText = "Prot:\(UserSetting.getLocalPort())"
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
        if connectedFlag {
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
        if !entered && connectedFlag {
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
        guard phase != .connected && phase != .measuring else { return }
        // スキャン中なら現在のスキャンを停止してからダイアログを開く。
        if isScanning {
            stopScan()
        }
        // 既存の再生セッションがあれば破棄してからダイアログを開く。
        if phase == .replaying {
            disconnectReplay()
        }
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
        guard phase != .connected && phase != .measuring else {
            let alert = NSAlert()
            alert.alertStyle = .warning
            alert.messageText = "Cannot open file while connected"
            alert.informativeText = "Disconnect the BLE device before opening a CSV for replay."
            alert.runModal()
            return
        }
        // スキャン中なら停止し、既存の再生セッションがあれば破棄してから読み込む。
        if isScanning {
            stopScan()
        }
        if phase == .replaying {
            disconnectReplay()
        }
        loadReplayFile(url: url)
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
        switch info["mode"] as? String {
        case "standard": selectMode = Int(MEMEMode_Standard) - 1
        case "full": selectMode = Int(MEMEMode_Full) - 1
        case "quaternion": selectMode = Int(MEMEMode_Quaternion) - 1
        default: break
        }
        if let cps = (info["cps"] as? NSNumber)?.intValue { transSpeed = cps == 100 ? 0 : 1 }
        if let g = (info["accRange"] as? NSNumber)?.intValue, let k = Self.accelG.firstIndex(of: g) { accelRange = k }
        if let d = (info["gyroRange"] as? NSNumber)?.intValue, let k = Self.gyroDps.firstIndex(of: d) { gyroRange = k }
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

    /// ページで付けた Artifact を控える。空なら "X"、カンマ/改行は列崩れ防止のため除去(同一行は上書き)。
    /// 表計算ソフトで数式として読まれる書き出し(= + - @)は受けない(CSV 注入。ページも入力時に断る。webview/BRIDGE.md)。
    private func receiveArtifact(i: Int, text: String) {
        guard phase == .replaying || phase == .measuring else { return }
        let sanitized = String(text
            .replacingOccurrences(of: ",", with: " ")
            .replacingOccurrences(of: "\n", with: " ")
            .replacingOccurrences(of: "\r", with: " ")
            .trimmingCharacters(in: .whitespaces)
            .prefix(64))
        if let c = sanitized.first, "=+-@".contains(c) {
            NSLog("[Artifact] refused (formula-like): %@", sanitized)
            return
        }
        pendingArtifacts[max(i, 0)] = sanitized.isEmpty ? "X" : sanitized
    }

    /// 再生中に付けた Artifact を再生元CSVの ARTIFACT 列へ書き戻す(Save Artifacts / 切断時)。
    /// キーは CSV のデータ行の番号(ページが返す番号そのまま)。
    private func flushArtifacts() {
        guard !pendingArtifacts.isEmpty, let url = replayFile else { return }
        do {
            try CsvArtifactWriter.apply(url: url, artifacts: pendingArtifacts)
        } catch {
            NSLog("[Artifact] failed to write: %@", error.localizedDescription)
        }
        pendingArtifacts.removeAll()
    }

    /// 計測中に付けた Artifact を、保存済みCSVの ARTIFACT 列へ書き戻す（停止時）。
    /// pendingArtifacts のキーはサンプル番号。CSVは先頭パケットを1件落とすため、
    /// データ行インデックス = サンプル番号 − 1（サンプル0はCSVに無いので除外する）。
    private func flushLiveArtifacts() {
        defer { pendingArtifacts.removeAll() }
        guard !pendingArtifacts.isEmpty, let url = persistence.savedFileURL else { return }
        var rowKeyed: [Int: String] = [:]
        for (sampleIndex, text) in pendingArtifacts where sampleIndex >= 1 {
            rowKeyed[sampleIndex - 1] = text
        }
        do {
            try CsvArtifactWriter.apply(url: url, artifacts: rowKeyed)
        } catch {
            NSLog("[Artifact] failed to write (live): %@", error.localizedDescription)
        }
    }

    // MARK: - Measurement

    func toggleMeasurement() {
        if !measurementFlag {
            startMeasurement()
        } else {
            stopMeasurement()
        }
    }

    private func startMeasurement() {
        stats.startMeasurement(quality: transSpeed + 1)

        memelib.setSelectMode(UInt32(selectMode + 1))
        memelib.setTransMode(UInt32(transSpeed + 1))
        memelib.setAccelRange(UInt32(accelRange))
        memelib.setGyroRange(UInt32(gyroRange))

        if let socket = socket {
            socket.headerString = headerString()
            socket.writeHeader()
        }

        liveSampleIndex = 0
        web.start(liveCondition())

        measurementFlag = true
        phase = .measuring
        memelib.startDataReport()
    }

    private func stopMeasurement() {
        memelib.stopDataReport()
        stats.stopMeasurement()
        web.stop()

        DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) { [weak self] in
            guard let self = self else { return }
            self.measurementFlag = false
            self.phase = .connected

            self.flushCsv()
            // 確定したCSVファイルへ、計測中に付けた Artifact を書き戻す。
            // （保存ダイアログでファイルを移動する前に、元パスへ書き込んでおく。）
            self.flushLiveArtifacts()

            if UserSetting.getShowSaveFileDialog() {
                self.persistence.presentSaveDialog()
            } else {
                self.persistence.resetCsvManager()
            }
            self.reset()
        }
    }

    func toggleFreeMarking() {
        isFreeMarking = true
    }

    // MARK: - Graph (WebView)

    /// 各モードで 1 サンプルぶんとしてページへ渡す列(CSV の列名と同じ)。
    private static let fullColumns = ["ACC_X", "ACC_Y", "ACC_Z", "GYRO_X", "GYRO_Y", "GYRO_Z", "EOG_L", "EOG_R", "EOG_H", "EOG_V"]
    private static let standardColumns = ["ACC_X", "ACC_Y", "ACC_Z", "EOG_L1", "EOG_R1", "EOG_L2", "EOG_R2",
                                          "EOG_H1", "EOG_H2", "EOG_V1", "EOG_V2"]

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
        let mode = memelib.getSelectMode()
        let modeName = mode == MEMEMode_Full ? "full" : mode == MEMEMode_Quaternion ? "quaternion" : "standard"
        let columns: [String] = mode == MEMEMode_Full ? Self.fullColumns
            : mode == MEMEMode_Standard ? Self.standardColumns : []
        var cond = displayOptions()
        cond["label"] = selectedDevice.isEmpty ? "JINS MEME" : selectedDevice
        cond["mode"] = modeName
        cond["cps"] = memelib.getTransMode() == MEMEQuality_High ? 100 : 50
        cond["accRange"] = Self.accelG[min(max(Int(memelib.getAccelRange()), 0), 3)]
        cond["gyroRange"] = Self.gyroDps[min(max(Int(memelib.getGyroRange()), 0), 3)]
        cond["columns"] = columns
        cond["startedAt"] = Date().timeIntervalSince1970 * 1000
        return cond
    }

    /// 1 サンプルをグラフ画面へ。freeMarked なら、CSV に x を書いた行と同じ位置へ印を出す。
    private func pushToGraph(_ data: AcademicData, freeMarked: Bool) {
        let i = liveSampleIndex
        liveSampleIndex += 1
        if let f = data as? AcademicFullData {
            web.push(i: i, values: [Int(f.accX), Int(f.accY), Int(f.accZ), Int(f.gyroX), Int(f.gyroY), Int(f.gyroZ),
                                    Int(f.eogL), Int(f.eogR), Int(f.eogH), Int(f.eogV)])
        } else if let s = data as? AcademicStandardData {
            web.push(i: i, values: [Int(s.accX), Int(s.accY), Int(s.accZ), Int(s.eogL1), Int(s.eogR1), Int(s.eogL2), Int(s.eogR2),
                                    Int(s.eogH1), Int(s.eogH2), Int(s.eogV1), Int(s.eogV2)])
        }
        if freeMarked {
            web.mark(i: i, text: "x")
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

    // MARK: - Data → dictionary

    /// CSV／ソケットへ流す1行ぶんの辞書を作る（registerPacket 済みのパケットに対して呼ぶ）。
    private func dataToDictionary(_ data: AcademicData, isFreeMarking: Bool) -> [String: Any] {
        [
            "data": data,
            "packetCount": NSNumber(value: stats.totalCount),
            "date": data.date ?? Date(),
            "isFreeMarking": NSNumber(value: isFreeMarking)
        ]
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
        let modeIdx = Int(memelib.getSelectMode()) - 1
        if (0..<selectModeOptions.count).contains(modeIdx) { selectMode = modeIdx }
        let transIdx = Int(memelib.getTransMode()) - 1
        if (0..<transSpeedOptions.count).contains(transIdx) { transSpeed = transIdx }
        let accelIdx = Int(memelib.getAccelRange())
        if (0..<accelRangeOptions.count).contains(accelIdx) { accelRange = accelIdx }
        let gyroIdx = Int(memelib.getGyroRange())
        if (0..<gyroRangeOptions.count).contains(gyroIdx) { gyroRange = gyroIdx }
    }

    // MARK: - Phase computed convenience

    var showScanButton: Bool { phase == .idle || phase == .deviceFound }
    var scanButtonLabel: String { isScanning ? "Stop Scan" : "Start Scan" }
    // BLE デバイス接続中（接続完了 or 計測中）以外は常に表示する。
    var showFileReplay: Bool { phase != .connected && phase != .measuring }
    // スキャン中はデバイス選択（(no device) 表示）を触らせない。
    // デバイスが見つかったら選択できるようにする。
    var isDeviceSelectionDisabled: Bool { isInputDisabled || (isScanning && phase != .deviceFound) }
    var showConnect: Bool { phase == .deviceFound || phase == .connected || phase == .replaying }
    var connectButtonLabel: String {
        (phase == .connected || phase == .replaying) ? "Disconnect" : "Connect"
    }
    var showMeasurement: Bool { phase == .connected || phase == .measuring }
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
        connectedFlag = true
        isScanning = false
        connectionStateText = "State : Connected"
        memeVersionText = "MEME Version：\(memelib.memeVersion.major).\(memelib.memeVersion.minor).\(memelib.memeVersion.revision)"
        phase = .connected
        syncDeviceSettings()
    }

    func memePeripheralDisconnectedDelegate(result: UInt32) {
        NSLog("memePeripheralDisconnectedDelegate : %d", result)
        connectedFlag = false
        isConnecting = false
        isScanning = false
        connectionStateText = "State : Disconnected"
        foundDevices.removeAll()
        selectedDevice = ""
        phase = .idle
        web.stop()
        // SHELF 送信後の切断は端末が移行を受理した合図。
        if isEnteringShelf {
            finishShelfMode(entered: true)
        }
    }

    func memeAcademicStandardDataReceivedDelegate(data: AcademicStandardData) {
        let freeMarked = ingestPacket(data: data)
        ingestForDisplay(standard: data)
        pushToGraph(data, freeMarked: freeMarked)
    }

    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData) {
        let freeMarked = ingestPacket(data: data)
        ingestForDisplay(full: data)
        pushToGraph(data, freeMarked: freeMarked)
    }

    func memeAcademicQuaternionDataReceivedDelegate(data: AcademicQuaternionData) {
        // Quaternion はグラフを持たないので、Free Marking は CSV に書くだけで印は出さない。
        _ = ingestPacket(data: data)
        displayCnt = data.cnt
    }

    /// 受信パケットを CSV／ソケットへ流す。このパケットの行へ Free Marking の x を書いたら
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
            persistence.append(dataToDictionary(data, isFreeMarking: freeMarked))
            saveCsv()
            if socket?.isConnected() == true, let last = persistence.lastRow {
                socketDatas.append(last)
                writeSocket()
            }
        }
        stats.bumpDataCount()
        displayBattLv = data.battLv
        return freeMarked
    }

    private func ingestForDisplay(standard d: AcademicStandardData) {
        displayCnt = d.cnt
        displayAccX = d.accX; displayAccY = d.accY; displayAccZ = d.accZ
        displayEogL = d.eogL1; displayEogR = d.eogR1
        displayEogH = d.eogH1; displayEogV = d.eogV1
    }

    private func ingestForDisplay(full d: AcademicFullData) {
        displayCnt = d.cnt
        displayAccX = d.accX; displayAccY = d.accY; displayAccZ = d.accZ
        displayGyroX = d.gyroX; displayGyroY = d.gyroY; displayGyroZ = d.gyroZ
        displayEogL = d.eogL; displayEogR = d.eogR
        displayEogH = d.eogH; displayEogV = d.eogV
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
