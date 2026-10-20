//
//  MEMEViewModel.swift
//  MEME_Academic (iOS)
//
//  画面の状態と操作。流れは Android2 の MainViewModel(スキャン → 接続 → 計測、切断時の再接続、Shelf mode、初期化)、
//  CSV・グラフ画面・アーティファクトの扱いは Mac 版の MEMEViewModel と同じ。
//
//  - 受信したサンプルは CSV(CsvWriter)とグラフ画面(WebBridge)へ。画面の数字(行数・成功率・通信率・電池)は
//    100Hz で描き直すと重いので、0.4 秒ごとに写す(Android の commTicker と同じ周期)。
//  - 計測中に切れたら、その計測は締め(切断ログを残す)、設定が ON なら同じ端末へ接続要求を出し続ける。繋がったら新しい
//    CSV で計測を始め直す(Android と同じ)。iOS の接続要求は期限が無いので、Android のような再スキャンの繰り返しはしない。
//  - 再生はグラフ画面が受け持つ(アプリはファイルを仮想ホストに出すだけ。Mac と同じ)。
//

import Foundation
import Observation
import SwiftUI
import UIKit

@MainActor
@Observable
final class MEMEViewModel: NSObject {

    // MARK: - Phase

    enum Phase {
        case idle
        /// 接続中(再接続の待ちを含む)
        case connecting
        case connected
        case measuring
        case replaying
    }

    // MARK: - Static options

    let selectModeOptions = MeasurementMode.allCases.map(\.label)
    let transSpeedOptions = MeasurementRange.transHz.map { "\($0)Hz" }
    let accelRangeOptions = MeasurementRange.accelG.map { "\($0)g" }
    let gyroRangeOptions = MeasurementRange.gyroDps.map { "\($0)dps" }

    // MARK: - Observable state

    private(set) var phase: Phase = .idle

    // Scan / Connect
    private(set) var foundDevices: [String] = []
    var selectedDevice = ""
    private(set) var isScanning = false
    private(set) var isReconnecting = false
    private(set) var firmwareVersion: String?

    // Measure
    private(set) var isStarting = false
    private(set) var batteryLevel = -1
    private(set) var recordingRows = 0
    private(set) var successRate = 1.0
    private(set) var commRate = 1.0

    // Replay
    private(set) var replayName: String?

    // Dialogs
    var showingSettings = false
    var showingDisplayEngine = false
    var showingReplayPicker = false
    var showingShelfDialog = false
    var showingBluetoothAlert = false
    private(set) var isEnteringShelf = false
    /// Shelf mode の結果(アラート)
    var shelfResult: (title: String, message: String)?
    /// 落ち続ける取り込んだ zip をやめて標準版へ戻したときの知らせ
    var engineFallbackNotice: String?
    /// 計測を止めたあとに共有するファイル(設定「計測を止めたら共有」)
    var shareItems: [URL]?
    /// 画面下に少しの間だけ出す知らせ
    private(set) var toast: String?
    private(set) var isInitializing = false

    // Settings(変えたら保存する)
    var selectMode = UserSetting.selectMode { didSet { UserSetting.selectMode = selectMode } }
    var transSpeed = UserSetting.transSpeed { didSet { UserSetting.transSpeed = transSpeed } }
    var accelRange = UserSetting.accelRange { didSet { UserSetting.accelRange = accelRange } }
    var gyroRange = UserSetting.gyroRange { didSet { UserSetting.gyroRange = gyroRange } }
    var autoConnect = UserSetting.autoConnect { didSet { UserSetting.autoConnect = autoConnect } }
    var reconnectEnabled = UserSetting.reconnect {
        didSet {
            UserSetting.reconnect = reconnectEnabled
            if !reconnectEnabled && isReconnecting { cancelReconnect() }
        }
    }
    var openSharingOnComplete = UserSetting.openSharingOnComplete { didSet { UserSetting.openSharingOnComplete = openSharingOnComplete } }
    var compressSaveFile = UserSetting.compressSaveFile { didSet { UserSetting.compressSaveFile = compressSaveFile } }
    var convertToLocalTime = UserSetting.convertToLocalTime { didSet { UserSetting.convertToLocalTime = convertToLocalTime } }

    /// グラフ画面(WebView)。中身の切り替えは Display Engine から(WebContentStore)。
    let web = WebBridge()
    /// 判定器の通知と演算結果の表(ページ → アプリの notify / table / records)。ライブ計測の間だけ受ける
    let outputs = DetectorOutputs()

    var appVersionText: String {
        let version = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? ""
        let build = Bundle.main.object(forInfoDictionaryKey: "CFBundleVersion") as? String ?? ""
        return "Version \(version) (\(build))"
    }

    // MARK: - Private state

    @ObservationIgnored private var memelib: (any MEMELibInterface)!
    @ObservationIgnored private let csv = CsvWriter()
    @ObservationIgnored private var counter = SampleCounter()
    /// 計測中のサンプル番号(計測開始から 0, 1, 2 …。先頭パケットも数える)。グラフ画面へ渡し、アーティファクトはこの番号で返る
    @ObservationIgnored private var liveSampleIndex = 0
    /// ページで付けた Artifact(計測中はサンプル番号、再生中は CSV のデータ行の番号)。停止時に CSV へ書き戻す
    @ObservationIgnored private var pendingArtifacts = ArtifactBuffer()
    @ObservationIgnored private var isFreeMarking = false
    @ObservationIgnored private var lastBattery = -1
    @ObservationIgnored private var tickTimer: Timer?
    @ObservationIgnored private var startTask: Task<Void, Never>?
    /// Disconnect ボタンなど意図した切断か(true の間は再接続しない)
    @ObservationIgnored private var userDisconnect = false
    /// このスキャンで自動接続を試したか
    @ObservationIgnored private var autoConnectAttempted = false
    /// Bluetooth の状態を一度でも受けたか(受ける前の「OFF」はまだ分からないだけ)
    @ObservationIgnored private var bluetoothKnown = false
    @ObservationIgnored private var replayFile: URL?
    @ObservationIgnored private var replayScoped = false
    @ObservationIgnored private var shelfDisconnectTimer: Timer?
    @ObservationIgnored private var toastTask: Task<Void, Never>?
    /// 最後に締めた計測のデータ CSV
    @ObservationIgnored private(set) var lastSavedFile: URL?

    /// SHELF 送信後、端末が自ら切断するのを待つ時間(切断 = 移行成功)
    private static let shelfDisconnectTimeout: TimeInterval = 5
    /// 計測を止めてから判定器の表を閉じるまで待つ時間(ページは stop の後に溜めた行を送る。Mac・Android と同じ 1 秒)
    private static let outputsStopGrace: TimeInterval = 1.0
    /// 画面の数字を写す周期(Android の commTicker と同じ)
    private static let tickInterval: TimeInterval = 0.4
    /// Disconnect の長押しで Shelf mode の確認を出すまでの時間
    static let shelfLongPressSeconds: Double = 5

    // MARK: - Init

    override init() {
        super.init()
        memelib = MEMELibFactory.make()
        memelib.delegate = self
        web.onArtifact = { [weak self] i, text in self?.receiveArtifact(i: i, text: text) }
        web.onReplayInfo = { [weak self] info in self?.applyReplayInfo(info) }
        web.onEngineFallback = { [weak self] notice in self?.engineFallbackNotice = notice }
        web.onOutput = { [weak self] body in self?.outputs.receive(body) }
    }

    // MARK: - Derived

    var isConnected: Bool { phase == .connected || phase == .measuring }
    var isMeasuring: Bool { phase == .measuring }
    var isReplaying: Bool { phase == .replaying }
    private var currentHz: Int { MeasurementRange.transHz[transSpeed] }

    var statusText: String {
        if phase == .replaying { return replayName ?? "" }
        if isEnteringShelf { return "Entering shelf mode…" }
        if isReconnecting { return "Reconnecting…" }
        switch phase {
        case .idle: return isScanning ? "Scanning…" : "Disconnected"
        case .connecting: return "Connecting…"
        case .connected, .measuring: return firmwareVersion.map { "Connected (ver.\($0))" } ?? "Connected"
        case .replaying: return ""
        }
    }

    var canScan: Bool { phase == .idle && !isScanning }
    var canConnect: Bool {
        switch phase {
        case .idle: return !selectedDevice.isEmpty
        case .connecting, .connected, .replaying: return !isEnteringShelf
        case .measuring: return false
        }
    }
    var connectButtonLabel: String { phase == .idle ? "Connect" : "Disconnect" }
    var canEnterShelfMode: Bool { phase == .connected && !MEMELibFactory.isMock && !isEnteringShelf && !isStarting }

    // MARK: - Scan / Connect

    func startScan() {
        guard canScan else { return }
        if bluetoothKnown && !memelib.isPoweredOn {
            showingBluetoothAlert = true
            return
        }
        foundDevices.removeAll()
        selectedDevice = ""
        autoConnectAttempted = false
        isScanning = true
        memelib.startScanningPeripherals()
    }

    private func stopScan() {
        guard isScanning else { return }
        memelib.stopScanningPeripherals()
        isScanning = false
    }

    func connectOrDisconnect() {
        // Shelf mode の確認中／移行中は触らせない(長押しが成立したジェスチャのタップが届いても切断しないための保険)
        guard !showingShelfDialog, !isEnteringShelf else { return }
        switch phase {
        case .replaying:
            exitReplay()
        case .idle:
            guard !selectedDevice.isEmpty else { return }
            connect(selectedDevice)
        case .connecting:
            // 接続待ち(再接続の待ちを含む)を取り消す
            userDisconnect = true
            isReconnecting = false
            memelib.disconnectPeripheral()
        case .connected:
            userDisconnect = true
            memelib.disconnectPeripheral()
        case .measuring:
            break
        }
    }

    private func connect(_ name: String) {
        stopScan()
        userDisconnect = false
        guard memelib.connectPeripheral(deviceName: name) == MEMELIB_OK else {
            showToast("Could not connect to \(name)")
            return
        }
        phase = .connecting
    }

    private func maybeAutoConnect() {
        guard autoConnect, !autoConnectAttempted, phase == .idle, let first = foundDevices.first else { return }
        autoConnectAttempted = true
        selectedDevice = first
        connect(first)
    }

    // MARK: - Reconnect

    /// 計測中に切れた端末へ接続要求を出したままにする。繋がったら新しい CSV で計測を始め直す
    private func startReconnect() {
        guard memelib.reconnectPeripheral() == MEMELIB_OK else { return }
        isReconnecting = true
        phase = .connecting
        NSLog("[MEMEViewModel] reconnecting")
    }

    private func cancelReconnect() {
        guard isReconnecting else { return }
        isReconnecting = false
        userDisconnect = true
        memelib.disconnectPeripheral()
    }

    // MARK: - Measurement

    func toggleMeasurement() {
        if phase == .measuring { stopMeasurement() } else { startMeasurement() }
    }

    /// 計測を始める。端末へ条件を送り、少し待ってから送信開始を送る(Android も間を空けている)
    private func startMeasurement() {
        guard phase == .connected, !isStarting else { return }
        isStarting = true
        // 判定器の通知(グラフ画面の notify)の許可は、最初の計測を始めるときに求める(起動直後に出すと画面を覆うため。
        // 2 回目からは何も出ない)。拒否されても計測には関係ない
        DetectorNotifications.setUp()
        startTask = Task { @MainActor [weak self] in
            guard let self else { return }
            let mode = MeasurementMode(pickerIndex: self.selectMode)
            self.memelib.applySettings(mode: mode.rawValue, trans: UInt32(self.transSpeed + 1),
                                       accel: UInt32(self.accelRange), gyro: UInt32(self.gyroRange))
            try? await Task.sleep(for: .milliseconds(800))
            guard self.phase == .connected, !Task.isCancelled else { self.isStarting = false; return }

            self.counter.reset()
            self.liveSampleIndex = 0
            self.isFreeMarking = false
            self.pendingArtifacts.removeAll()
            self.recordingRows = 0
            self.successRate = 1
            self.commRate = 1
            self.csv.start(address: self.deviceAddress, mode: mode,
                           transMode: self.memelib.getTransMode(), accelRange: self.memelib.getAccelRange(),
                           gyroRange: self.memelib.getGyroRange(), compress: self.compressSaveFile)
            self.outputs.start(cps: self.currentHz,
                               dataFile: { [weak self] in self?.csv.savedFileURL },
                               pageName: { [weak self] in self?.web.pageName ?? "" })
            self.web.start(self.liveCondition())
            self.phase = .measuring
            self.isStarting = false
            self.memelib.startDataReport()
            self.startTicker()
        }
    }

    private func stopMeasurement() {
        guard phase == .measuring else { return }
        memelib.stopDataReport()
        phase = .connected
        finishMeasurement(share: true)
    }

    /// 計測を締める(停止のとき・計測中に切れたとき): CSV を書き切り、計測中に付けた Artifact を書き戻し、判定器の表を閉じる
    private func finishMeasurement(share: Bool) {
        stopTicker()
        publishCounters()
        web.stop()
        let file = csv.stop()
        if let file { lastSavedFile = file }
        pendingArtifacts.flush(to: file, numbering: .liveSample)
        outputs.noteDataFile()
        // 判定器の表は、ページが stop の後に送る残りを待ってから閉じる(閉じる前に次の計測が始まっていたら何もしない)
        let s = outputs.session
        DispatchQueue.main.asyncAfter(deadline: .now() + Self.outputsStopGrace) { [weak self] in
            guard let self else { return }
            let tables = self.outputs.stop(session: s)
            if share, self.openSharingOnComplete, let file {
                self.shareItems = [file] + tables
            }
        }
        if let file { NSLog("[MEMEViewModel] saved %@ (%d rows)", file.lastPathComponent, recordingRows) }
    }

    func freeMarking() {
        guard phase == .measuring else { return }
        isFreeMarking = true
    }

    /// CSV のファイル名に使う端末の名前(アドレスが読めなければ端末名)
    private var deviceAddress: String {
        memelib.macAddress.isEmpty ? selectedDevice : memelib.macAddress
    }

    // MARK: - Ticker

    private func startTicker() {
        stopTicker()
        tickTimer = Timer.scheduledTimer(withTimeInterval: Self.tickInterval, repeats: true) { [weak self] _ in
            MainActor.assumeIsolated {
                guard let self else { return }
                self.commRate = self.counter.commRateTick(hz: self.currentHz)
                self.publishCounters()
            }
        }
    }

    private func stopTicker() {
        tickTimer?.invalidate()
        tickTimer = nil
    }

    private func publishCounters() {
        recordingRows = csv.rowCount
        successRate = counter.successRate
        batteryLevel = lastBattery
    }

    // MARK: - Graph (WebView)

    /// 表示の設定(時刻の表示・加速度のオフセット・テーマ)。計測・再生どちらでもページへ渡す
    private func displayOptions() -> [String: Any] {
        let dark = UITraitCollection.current.userInterfaceStyle == .dark
        return [
            "timeZone": convertToLocalTime ? "local" : "utc",
            "accOffset": [0, 0, 0],
            "theme": dark ? "dark" : "light",
        ]
    }

    /// 計測の開始時にページへ渡す条件(webview/BRIDGE.md の start)。値は端末に設定したもの
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

    /// グラフ画面の中身が切り替わったら読み込み直す(Display Engine から)
    func reloadGraph() { web.load() }

    /// アプリが前面に出た/裏に回った(裏の間はグラフ画面へ送らない。WebBridge.setForeground)
    func setForeground(_ foreground: Bool) { web.setForeground(foreground) }

    // MARK: - Settings

    /// 端末のパラメータを初期化する(設定の Initialize)
    func initializeDevice() {
        guard phase == .connected, !isInitializing else { return }
        isInitializing = true
        memelib.initializeParams { [weak self] ok in
            guard let self else { return }
            self.isInitializing = false
            self.showToast(ok ? "Success to initialize" : "Failed to initialize")
        }
    }

    func showToast(_ text: String) {
        toast = text
        toastTask?.cancel()
        toastTask = Task { @MainActor [weak self] in
            try? await Task.sleep(for: .seconds(2.5))
            guard !Task.isCancelled else { return }
            self?.toast = nil
        }
    }

    // MARK: - Shelf mode

    /// Disconnect の長押しで Shelf mode の確認を開く。移行できる状態でなければ何も起きない(隠し操作なので知らせない)
    func requestShelfMode() {
        guard canEnterShelfMode else { return }
        showingShelfDialog = true
    }

    /// 端末を Shelf mode（保管モード）へ移す。CONFIG モードへの遷移が受理されてから SHELF を送り、受理されると端末は自ら切断する。
    /// 復帰は充電のみで、アプリからは戻せない。
    func confirmShelfMode() {
        showingShelfDialog = false
        guard canEnterShelfMode else { return }
        isEnteringShelf = true
        memelib.enterShelfMode { [weak self] sent in
            guard let self else { return }
            guard sent else { self.finishShelfMode(entered: false); return }
            self.shelfDisconnectTimer = Timer.scheduledTimer(withTimeInterval: Self.shelfDisconnectTimeout, repeats: false) { [weak self] _ in
                MainActor.assumeIsolated { self?.finishShelfMode(entered: false) }
            }
        }
    }

    private func finishShelfMode(entered: Bool) {
        guard isEnteringShelf else { return }
        shelfDisconnectTimer?.invalidate()
        shelfDisconnectTimer = nil
        isEnteringShelf = false
        shelfResult = entered
            ? ("Entered shelf mode", "To exit shelf mode, please recharge the device.")
            : ("Failed to enter shelf mode", "The device is still in normal mode.")
    }

    // MARK: - Replay

    func chooseReplayFile() {
        guard phase == .idle else { return }
        stopScan()
        showingReplayPicker = true
    }

    /// 選んだ CSV(.csv / .csv.gz)をグラフ画面で再生する。読み込み・再生・一時停止・速度・シークはページ側
    func openReplay(_ url: URL) {
        guard phase == .idle || phase == .replaying else { return }
        if phase == .replaying { exitReplay() }
        // ファイル App で選んだファイルは、使う間だけアクセスを開けておく(再生中にページが読む・終わりに書き戻す)
        replayScoped = url.startAccessingSecurityScopedResource()
        replayFile = url
        replayName = url.lastPathComponent
        pendingArtifacts.removeAll()
        phase = .replaying
        web.openReplay(file: url, extra: displayOptions())
    }

    /// ページが CSV を読み終えた。読めない行などの知らせ(warning)があれば出す
    private func applyReplayInfo(_ info: [String: Any]) {
        guard phase == .replaying else { return }
        if let w = info["warning"] as? String, !w.isEmpty { showToast(w) }
    }

    /// 再生を終える(Disconnect)。ページで付けた Artifact を再生元の CSV へ書き戻す
    private func exitReplay() {
        pendingArtifacts.flush(to: replayFile, numbering: .csvRow)
        web.closeReplay()
        if replayScoped { replayFile?.stopAccessingSecurityScopedResource() }
        replayScoped = false
        replayFile = nil
        replayName = nil
        phase = .idle
    }

    private func receiveArtifact(i: Int, text: String) {
        guard phase == .replaying || phase == .measuring else { return }
        pendingArtifacts.add(i: i, text: text)
    }

    // MARK: - Data

    /// 受信パケットを CSV とグラフ画面へ。最初の 1 パケットは前回カウンタの基準を取るだけで CSV に書かない
    private func ingest(_ data: AcademicData) {
        guard phase == .measuring else { return }
        lastBattery = Int(data.battLv)
        let mode = MeasurementMode(of: data)
        #if DEBUG
        debugNote(data, mode: mode)
        #endif
        let values = mode.flatMap { $0.values(of: data) }
        var freeMarked = false
        if counter.countUp(Int(data.cnt)), let values {
            freeMarked = isFreeMarking
            isFreeMarking = false
            let row = CsvRow(values: values, num: counter.totalCount, date: Date(), isFreeMarking: freeMarked)
            csv.append(row)
            // 判定器の表の NUM / DATE 列(このパケットがページへ渡る番号 = liveSampleIndex)
            outputs.recordSample(i: liveSampleIndex, num: row.num, date: row.date)
        }
        let i = liveSampleIndex
        liveSampleIndex += 1
        if let mode, mode.hasGraph, let values { web.push(i: i, values: values) }
        if freeMarked && mode?.hasGraph == true { web.mark(i: i, text: "X") }
    }

    #if DEBUG
    // MARK: - 自己テスト用(DebugAutoTest)

    /// 計測ごとの確かめ用の数(モードと違う種類のパケット・加速度の大きさの平均)
    @ObservationIgnored private(set) var debugModeMismatch = 0
    @ObservationIgnored private var debugAccSum = 0.0
    @ObservationIgnored private var debugAccCount = 0
    var debugAccMagnitude: Double { debugAccCount > 0 ? debugAccSum / Double(debugAccCount) : 0 }
    /// 端末が使っている計測条件(読み返した値)
    var debugDeviceCondition: [String: Any] {
        ["mode": (MeasurementMode(rawValue: memelib.getSelectMode())?.label ?? "?"),
         "hz": MeasurementRange.hz(quality: memelib.getTransMode()),
         "accelG": MeasurementRange.accelG(device: memelib.getAccelRange()),
         "gyroDps": MeasurementRange.gyroDps(device: memelib.getGyroRange())]
    }

    func debugResetStats() {
        debugModeMismatch = 0
        debugAccSum = 0
        debugAccCount = 0
    }

    private func debugNote(_ data: AcademicData, mode: MeasurementMode?) {
        if mode?.rawValue != memelib.getSelectMode() { debugModeMismatch += 1 }
        let acc: (Int16, Int16, Int16)?
        switch data {
        case let d as AcademicStandardData: acc = (d.accX, d.accY, d.accZ)
        case let d as AcademicFullData: acc = (d.accX, d.accY, d.accZ)
        default: acc = nil
        }
        if let (x, y, z) = acc {
            debugAccSum += (Double(x) * Double(x) + Double(y) * Double(y) + Double(z) * Double(z)).squareRoot()
            debugAccCount += 1
        }
    }

    var debugFirmware: String { firmwareVersion ?? "" }
    var debugAddress: String { memelib.macAddress }
    var debugErrorCount: Int { counter.errorCount }
    var debugTotalCount: Int { counter.totalCount }
    var debugLastFile: URL? { csv.savedFileURL ?? lastSavedFile }

    /// 再接続を確かめるため、Disconnect ボタンを使わずに切る(計測中に電波が切れたのと同じ扱いになる)
    func debugDropConnection() { memelib.disconnectPeripheral() }

    func debugSelect(_ name: String) { selectedDevice = name }
    #endif
}

// MARK: - MEMELibAcademicDelegate

extension MEMEViewModel: MEMELibAcademicDelegate {

    func memeBluetoothStateDelegate(poweredOn: Bool) {
        bluetoothKnown = true
        if !poweredOn {
            isScanning = false
            if isReconnecting { isReconnecting = false; phase = .idle }
        }
    }

    func memePeripheralFoundDelegate(result: UInt32, deviceName: String?, uuid: String?) {
        guard result == MEMELIB_OK else {
            isScanning = false
            return
        }
        guard let name = deviceName, !foundDevices.contains(name) else { return }
        foundDevices.append(name)
        foundDevices.sort()
        if selectedDevice.isEmpty { selectedDevice = name }
        maybeAutoConnect()
    }

    func memePeripheralConnectedDelegate(result: UInt32) {
        guard result == MEMELIB_OK else {
            if isReconnecting {
                // 繋ぎかけて落ちた。もう一度待つ
                if memelib.reconnectPeripheral() != MEMELIB_OK { isReconnecting = false; phase = .idle }
                return
            }
            phase = .idle
            showToast("Connect failed")
            return
        }
        let v = memelib.memeVersion
        firmwareVersion = "\(v.major).\(v.minor).\(v.revision)"
        phase = .connected
        if isReconnecting {
            isReconnecting = false
            NSLog("[MEMEViewModel] reconnected; restarting the measurement")
            startMeasurement()
        }
    }

    func memePeripheralDisconnectedDelegate(result: UInt32, reason: String) {
        let wasMeasuring = phase == .measuring
        startTask?.cancel()
        isStarting = false
        if wasMeasuring {
            NSLog("[MEMEViewModel] disconnected while measuring: %@ battery=%d", reason, lastBattery)
            // なぜ止まったかを後から切り分けられるよう、時刻・理由・直前の電池残量を残す(csv.stop の前に)
            csv.writeDisconnect(at: Date(), reason: reason, battery: lastBattery)
            finishMeasurement(share: false)
        }
        phase = .idle
        firmwareVersion = nil
        batteryLevel = -1
        lastBattery = -1
        isReconnecting = false
        if isEnteringShelf {
            // SHELF 送信後の切断は、端末が移行を受理した合図
            finishShelfMode(entered: true)
            return
        }
        if wasMeasuring && reconnectEnabled && !userDisconnect {
            startReconnect()
        }
    }

    func memeAcademicStandardDataReceivedDelegate(data: AcademicStandardData) { ingest(data) }
    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData) { ingest(data) }
    func memeAcademicQuaternionDataReceivedDelegate(data: AcademicQuaternionData) { ingest(data) }
}
