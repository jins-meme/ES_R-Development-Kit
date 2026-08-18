//
//  MEMEViewModel.swift
//  MEME_Academic
//
//  SwiftUI 用 ViewModel。
//  状態管理・アクション・delegate 振り分けに専念し、
//  CSV 保存／チャート計算／通信統計は専用サービスへ委譲する。
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
        case replayReady
        case replaying
    }

    // MARK: - Static options

    let selectModeOptions = ["Standard", "Full", "Quaternion"]
    let transSpeedOptions = ["100Hz", "50Hz"]
    let accelRangeOptions = ["±2G", "±4G", "±8G", "±16G"]
    let gyroRangeOptions = ["±250dps", "±500dps", "±1000dps", "±2000dps"]
    let chartCategoryOptions = ["Electrooculography", "Gyroscope", "Accelerometer"]

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

    // Chart UI state
    var chart1Category: Int = 0
    var chart2Category: Int = 1
    var chart3Category: Int = 2

    var chart1Title: String = "Chart1：Electrooculography"
    var chart2Title: String = "Chart2：Gyroscope"
    var chart3Title: String = "Chart3：Accelerometer"

    var chart1EogToggles = EogToggles()
    var chart1GyroToggles = GyroToggles()
    var chart1AccelToggles = AccelToggles()

    var chart2EogToggles = EogToggles()
    var chart2GyroToggles = GyroToggles()
    var chart2AccelToggles = AccelToggles()

    var chart3EogToggles = EogToggles()
    var chart3GyroToggles = GyroToggles()
    var chart3AccelToggles = AccelToggles()

    // Chart rendered data
    var chart1Plot = ChartPlot(baseYMin: -1200, baseYMax: 1200)
    var chart2Plot = ChartPlot(baseYMin: -8000, baseYMax: 8000)
    var chart3Plot = ChartPlot(baseYMin: -8000, baseYMax: 8000)

    // Settings sheet presentation
    var showingSettings: Bool = false

    // Chart X-axis range (seconds)
    let xRangeOptions: [Int] = [3, 7, 15, 30]
    var xRangeIndex: Int = 1 // 7秒がデフォルト

    // Replay scrubbing
    var replayProgress: Double = 0 // 0...100

    // Replay pause
    var isReplayPaused: Bool = false

    // Replay speed（x1/x2/x4/x8/x16/x32）
    let replaySpeedOptions: [Int] = [1, 2, 4, 8, 16, 32]
    var replaySpeedIndex: Int = 0

    // Artifact tagging dialog
    var showingArtifactDialog: Bool = false
    var artifactInput: String = ""

    // Replay range cut dialog（チャートのドラッグ範囲をCSVへ切り出す）
    var showingCutDialog: Bool = false
    var cutFileNameInput: String = ""
    var cutErrorMessage: String = ""

    // MARK: - Private state

    private var memelib: (any MEMELibInterface)!
    private var connectedFlag = false
    private var measurementFlag = false
    private var isFreeMarking = false

    private var peripheralManager: CBPeripheralManager?
    private var socket: TCPSocket?
    private var socketDatas: [[String: Any]] = []

    // File Replay
    private var replayInfo: CsvReplayInfo?
    private var currentReplayIndex: Int = 0
    private var isScrubbingReplay = false

    /// タップで記録した Artifact（絶対チャートサンプル位置 → 文字列）。停止時にCSVへ書き戻す。
    /// 再生中はサンプル位置＝データ行インデックス。計測中は先頭パケットを1件落とすため
    /// データ行インデックス＝サンプル位置−1（書き戻し時に変換する。flushLiveArtifacts 参照）。
    private var pendingArtifacts: [Int: String] = [:]
    /// Artifact ダイアログの対象サンプル位置（絶対チャートサンプル位置）。
    private var artifactTargetRow: Int = 0

    /// 切り出しダイアログの対象区間（0始まりデータ行インデックス、両端含む）。
    private var cutRange: (start: Int, end: Int) = (0, 0)

    /// 描画スロットリング用カウンタ（appendChartSample で加算）。
    private var chartRenderCounter: Int = 0

    // MARK: - Services

    private let chartService = ChartService()
    private let persistence = DataPersistenceService()
    private let stats = CommunicationStatsTracker()
    private let replayService = CsvReplayService()

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
        isReplayPaused = false
        pendingArtifacts.removeAll()
        showingArtifactDialog = false
        showingCutDialog = false
    }

    /// グラフ（描画バッファ・スロットルカウンタ・各プロット）をクリアする。
    /// Stop Measurement / Record（再生停止）時ではなく、
    /// 次の計測／ファイル再生が始まるタイミングで呼ぶ。
    private func clearChart() {
        chartService.reset()
        chartRenderCounter = 0
        chart1Plot.reset()
        chart2Plot.reset()
        chart3Plot.reset()
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
        if phase == .replayReady || phase == .replaying {
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

    // MARK: - File Replay actions

    func chooseReplayFile() {
        // BLE 接続中は再生に入れない。
        guard phase != .connected && phase != .measuring else { return }
        // スキャン中なら現在のスキャンを停止してからダイアログを開く。
        if isScanning {
            stopScan()
        }
        // 既存の再生セッションがあれば破棄してからダイアログを開く。
        if phase == .replayReady || phase == .replaying {
            disconnectReplay()
        }
        let panel = NSOpenPanel()
        panel.canChooseFiles = true
        panel.canChooseDirectories = false
        panel.allowsMultipleSelection = false
        panel.level = .modalPanel
        if let csvType = UTType(filenameExtension: "csv") {
            panel.allowedContentTypes = [csvType]
        }
        guard let window = NSApp.mainWindow else { return }
        panel.beginSheetModal(for: window) { [weak self] result in
            guard let self else { return }
            guard result == .OK, let url = panel.url else { return }
            self.loadReplayFile(url: url)
        }
    }

    /// Finder の「このアプリで開く」など、外部から渡されたCSVを File Replay として読み込む。
    /// 成功すれば .replayReady（Start measurement 可能）、形式が違えば loadReplayFile がエラーダイアログを出す。
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
        if phase == .replayReady || phase == .replaying {
            disconnectReplay()
        }
        loadReplayFile(url: url)
    }

    private func loadReplayFile(url: URL) {
        do {
            let info = try CsvReplayService.parse(url: url)
            replayInfo = info
            pendingArtifacts.removeAll()
            selectMode = Int(info.mode) - 1
            transSpeed = info.transMode == MEMEQuality_High ? 0 : 1
            accelRange = Int(info.accelRange)
            gyroRange = Int(info.gyroRange)
            connectionStateText = "State : \(info.fileName)"
            phase = .replayReady
            // ファイルを読み込んだら自動で再生を開始する（Start Replay ボタンは廃止）。
            startReplay()
        } catch {
            NSLog("[Replay] failed to parse CSV: %@", url.path)
            let alert = NSAlert()
            alert.alertStyle = .warning
            alert.messageText = "Not a valid MEME CSV file"
            alert.informativeText = url.lastPathComponent
            alert.runModal()
        }
    }

    func toggleReplay() {
        if phase == .replaying {
            finishReplay()
        } else {
            startReplay()
        }
    }

    private func startReplay() {
        guard phase == .replayReady, let info = replayInfo else { return }
        phase = .replaying
        isReplayPaused = false
        replaySpeedIndex = 0
        replayService.start(rows: info.rows,
                            transMode: info.transMode,
                            onRow: { [weak self] data, index, total in
            self?.ingestReplayRow(data, mode: info.mode, index: index, total: total)
        }, onFinished: { [weak self] in
            self?.pauseReplayAtEnd()
        })
        // ウィンドウ幅（例: 7s なら 7s 分）のデータを先読みしてグラフを満たした状態から再生を始める。
        // fillReplayWindow が描画バッファを作り直すため、前回のグラフはここでクリアされる。
        fillReplayWindow(endingAt: xRangeSeconds * chartSampleRate - 1)
    }

    private func finishReplay() {
        replayService.stop()
        guard phase == .replaying else { return }
        // 停止時に、タップで記録した Artifact を再生元CSVへ書き戻す。
        flushArtifacts()
        phase = .replayReady
        isReplayPaused = false
        // グラフはリセットしない（Record／停止時は残し、次の計測・再生開始時にクリアする）。
        stats.reset()
        successRateValue = 0; successRateText = "0.0%"
        communicationValue = 0; communicationText = "0.0%"
        replayProgress = 0
    }

    /// 再生が最後まで到達したときに呼ぶ。Start Replay 状態には戻さず、末尾位置で一時停止した状態にする。
    /// これにより末尾でも Record（グラフを残して停止）や << でのシークが行える。
    private func pauseReplayAtEnd() {
        guard phase == .replaying else { return }
        isReplayPaused = true
        // スロットルで最後の描画更新が漏れることがあるため、末尾サンプルまで確実に反映する。
        updateChartPlots()
    }

    private func disconnectReplay() {
        // 再生セッションを完全に終える。保持していた1ファイル分の行データもここで解放する。
        replayService.clear()
        // 再生中に切断された場合も、記録済み Artifact は書き戻す。
        // replayInfo はこの後破棄するため読み直しは不要。
        flushArtifacts(reload: false)
        replayInfo = nil
        phase = .idle
        connectionStateText = "State : Disconnected"
        reset()
    }

    // MARK: - Replay pause

    func toggleReplayPause() {
        guard phase == .replaying else { return }
        if isReplayPaused {
            // 末尾で一時停止中は再開する行が無いため、resume が失敗したら一時停止のままにする。
            if replayService.resume() {
                isReplayPaused = false
            }
        } else {
            replayService.pause()
            isReplayPaused = true
        }
    }

    // MARK: - Replay speed

    /// 現在の再生速度倍率（1/2/4/8/16）。
    var replaySpeed: Int { replaySpeedOptions[replaySpeedIndex] }
    /// 再生速度ボタンのラベル（例 "x2"）。
    var replaySpeedLabel: String { "x\(replaySpeed)" }

    /// 再生速度ボタン：タップするたびに x1→x2→x4→x8→x16→x32→x1 と循環する。
    /// 描画周期は変えず、1周期で取り込むデータ量が速度倍になる。
    func cycleReplaySpeed() {
        guard phase == .replaying else { return }
        replaySpeedIndex = (replaySpeedIndex + 1) % replaySpeedOptions.count
        replayService.setSpeed(replaySpeed)
    }

    // MARK: - Artifact tagging

    /// チャートタップ時に呼ぶ。再生中／計測中のどちらでも、対象サンプルを控えてダイアログを開く。
    /// row は絶対チャートサンプル位置（右詰め描画の右端＝最新）。
    func chartTapped(row: Int) {
        switch phase {
        case .replaying:
            guard let info = replayInfo, !info.rows.isEmpty else { return }
            artifactTargetRow = min(max(row, 0), info.rows.count - 1)
        case .measuring:
            // 計測中はストリームが開いており上限が無いため下限のみクランプする。
            artifactTargetRow = max(row, 0)
        default:
            return
        }
        artifactInput = ""
        showingArtifactDialog = true
    }

    /// ダイアログOK。空なら "X"、カンマ/改行は列崩れ防止のため除去してメモリに記録（同一行は上書き）。
    func confirmArtifact() {
        let sanitized = artifactInput
            .replacingOccurrences(of: ",", with: " ")
            .replacingOccurrences(of: "\n", with: " ")
            .trimmingCharacters(in: .whitespaces)
        pendingArtifacts[artifactTargetRow] = sanitized.isEmpty ? "X" : sanitized
        artifactInput = ""
        showingArtifactDialog = false
        // 一時停止中は次の tick が来ないため、付けた直後にチャートへ反映されるよう再描画する。
        updateChartPlots()
    }

    func cancelArtifact() {
        artifactInput = ""
        showingArtifactDialog = false
    }

    /// 記録済み Artifact を再生元CSVの ARTIFACT 列へ書き戻す（停止/切断時）。
    /// 書き戻し後、reload=true なら同じファイルを読み直して replayInfo を更新し、
    /// 続けて Start した際に今書き込んだ Artifact がチャートへ反映されるようにする。
    /// （切断時は replayInfo を破棄するため reload=false でよい。）
    private func flushArtifacts(reload: Bool = true) {
        guard !pendingArtifacts.isEmpty, let info = replayInfo else { return }
        do {
            try CsvReplayService.applyArtifacts(url: info.url, artifacts: pendingArtifacts)
            if reload, let refreshed = try? CsvReplayService.parse(url: info.url) {
                replayInfo = refreshed
            }
        } catch {
            NSLog("[Artifact] failed to write: %@", error.localizedDescription)
        }
        pendingArtifacts.removeAll()
    }

    /// 計測中にタップで付けた Artifact を、保存済みCSVの ARTIFACT 列へ書き戻す（停止時）。
    /// pendingArtifacts のキーは絶対チャートサンプル位置。CSVは先頭パケットを1件落とすため、
    /// データ行インデックス = サンプル位置 − 1（サンプル0はCSVに無いので除外する）。
    private func flushLiveArtifacts() {
        defer { pendingArtifacts.removeAll() }
        guard !pendingArtifacts.isEmpty, let url = persistence.savedFileURL else { return }
        var rowKeyed: [Int: String] = [:]
        for (sampleIndex, text) in pendingArtifacts where sampleIndex >= 1 {
            rowKeyed[sampleIndex - 1] = text
        }
        do {
            try CsvReplayService.applyArtifacts(url: url, artifacts: rowKeyed)
        } catch {
            NSLog("[Artifact] failed to write (live): %@", error.localizedDescription)
        }
    }

    // MARK: - Replay range cut

    /// 範囲選択（ドラッグ）を受け付けるか。ファイル再生中（一時停止中を含む）のみ有効。
    var isReplayRangeSelectable: Bool { phase == .replaying }

    /// チャート上のドラッグ範囲選択が終了したときに呼ぶ。行は絶対チャートサンプル位置
    /// （再生中はデータ行インデックスと一致）。区間を控えて保存ダイアログを開く。
    func chartRangeSelected(startRow: Int, endRow: Int) {
        guard phase == .replaying, let info = replayInfo, !info.rows.isEmpty else { return }
        let maxRow = info.rows.count - 1
        let start = min(max(min(startRow, endRow), 0), maxRow)
        let end = min(max(max(startRow, endRow), 0), maxRow)
        guard start < end else { return }
        cutRange = (start, end)
        cutFileNameInput = Self.defaultCutFileName(for: info.url)
        cutErrorMessage = ""
        showingCutDialog = true
    }

    /// 切り出し先のデフォルトファイル名。"current.csv" → "current_1.csv"、
    /// それが既にあれば "current_2.csv" … と存在しない名前までインクリメントする。
    private static func defaultCutFileName(for url: URL) -> String {
        let base = url.deletingPathExtension().lastPathComponent
        let dir = url.deletingLastPathComponent()
        var n = 1
        while FileManager.default.fileExists(atPath: dir.appendingPathComponent("\(base)_\(n).csv").path) {
            n += 1
        }
        return "\(base)_\(n).csv"
    }

    /// 切り出しダイアログOK。再生元CSVと同じフォルダへ、控えた区間のデータ行だけを書き出す。
    /// 同名ファイルが既にある場合はダイアログ内へエラーを表示し、保存もダイアログを閉じることもしない。
    func confirmCutFile() {
        guard let info = replayInfo else {
            showingCutDialog = false
            return
        }
        var name = cutFileNameInput.trimmingCharacters(in: .whitespaces)
        guard !name.isEmpty, !name.contains("/") else {
            cutErrorMessage = "Invalid file name."
            return
        }
        if !name.lowercased().hasSuffix(".csv") {
            name += ".csv"
        }
        let dest = info.url.deletingLastPathComponent().appendingPathComponent(name)
        if FileManager.default.fileExists(atPath: dest.path) {
            cutErrorMessage = "File already exists."
            return
        }
        do {
            try CsvReplayService.exportRange(from: info.url,
                                             to: dest,
                                             startRow: cutRange.start,
                                             endRow: cutRange.end)
            showingCutDialog = false
        } catch {
            NSLog("[Cut] failed to write: %@", error.localizedDescription)
            cutErrorMessage = "Failed to save file."
        }
    }

    func cancelCutFile() {
        showingCutDialog = false
    }

    private func ingestReplayRow(_ data: AcademicData, mode: UInt32, index: Int, total: Int) {
        displayBattLv = data.battLv
        currentReplayIndex = index
        if !isScrubbingReplay {
            replayProgress = total > 1 ? Double(index) / Double(total - 1) * 100 : 0
        }

        switch mode {
        case MEMEMode_Full:
            guard let d = data as? AcademicFullData else { return }
            ingestForDisplay(full: d)
        case MEMEMode_Quaternion:
            displayCnt = data.cnt
            return
        default:
            guard let d = data as? AcademicStandardData else { return }
            ingestForDisplay(standard: d)
        }

        appendChartSample(data)
    }

    // MARK: - Replay scrubbing (slider / jump)

    /// スライダーの操作状態が変わったときに呼ぶ。ドラッグ終了時にシークする。
    func replaySliderEditingChanged(_ editing: Bool) {
        isScrubbingReplay = editing
        if !editing {
            seekReplay(toProgress: replayProgress)
        }
    }

    /// << / >> の移動量（秒）。ウィンドウ幅から 2 秒引いた分だけ移動し、
    /// 前後のウィンドウが 2 秒重なって連続して見えるようにする。
    private var replayJumpSeconds: Int { max(1, xRangeSeconds - 2) }

    /// >> ボタン：ウィンドウ幅−2秒分だけ再生位置を進める。
    func replayJumpForward() {
        jumpReplay(bySeconds: replayJumpSeconds)
    }

    /// << ボタン：ウィンドウ幅−2秒分だけ再生位置を戻す。
    func replayJumpBackward() {
        jumpReplay(bySeconds: -replayJumpSeconds)
    }

    private func jumpReplay(bySeconds seconds: Int) {
        guard phase == .replaying, let info = replayInfo else { return }
        let rowsPerSecond = info.transMode == MEMEQuality_High ? 100 : 50
        seekReplay(toRow: currentReplayIndex + seconds * rowsPerSecond)
    }

    private func seekReplay(toProgress progress: Double) {
        guard let info = replayInfo, info.rows.count > 1 else { return }
        let clampedProgress = min(max(progress, 0), 100)
        let index = Int((clampedProgress / 100) * Double(info.rows.count - 1))
        seekReplay(toRow: index)
    }

    /// 再生中のシーク処理。一時停止中・再生中どちらも、シーク先で終わるウィンドウ幅分の
    /// データを先読みしてグラフを満たしてから、続き（シーク先の次の行）を読み込む。
    /// これにより << / >> ジャンプ後も右端から徐々に埋めるのではなく、
    /// 最初からウィンドウ幅を満たした状態で描画を再開する。
    private func seekReplay(toRow index: Int) {
        guard phase == .replaying, let info = replayInfo, !info.rows.isEmpty else { return }
        fillReplayWindow(endingAt: min(max(index, 0), info.rows.count - 1))
    }

    /// 指定行 endRow で終わるウィンドウ幅（現在のX軸レンジ）分のデータを先読みしてグラフを満たし、
    /// 続きを endRow の次の行から読み込めるよう再生位置を進める。
    /// 再生中なら次の tick から、一時停止中なら Resume 後に、endRow+1 以降が右端へ流れていく。
    /// 再生開始時・シーク（<< / >> ・スライダー）時・一時停止中のX軸レンジ変更時に、
    /// 右端から徐々に埋めるのではなく、最初からウィンドウ幅を満たした状態で描画を（再）開始するために使う。
    private func fillReplayWindow(endingAt endRow: Int) {
        guard let info = replayInfo, !info.rows.isEmpty else { return }
        let windowSamples = max(1, xRangeSeconds * chartSampleRate)
        // 右端（＝再生位置）はウィンドウ幅−1 行より手前へは戻さない。
        // これより手前へ戻すと窓が [0…endRow] の部分窓になり、左端が負の時刻（−ウィンドウ長）に
        // なってしまう。先頭付近では常に先頭ウィンドウ [0 … windowSamples−1] を表示し、左端を 0s にする。
        let minEnd = min(windowSamples - 1, info.rows.count - 1)
        let clamped = min(max(endRow, minEnd), info.rows.count - 1)
        let start = max(0, clamped - windowSamples + 1)
        chartRenderCounter = 0
        currentReplayIndex = clamped
        replayProgress = info.rows.count > 1 ? Double(clamped) / Double(info.rows.count - 1) * 100 : 0
        chartService.reset(baseIndex: start)
        for i in start...clamped {
            chartService.append(info.rows[i])
        }
        updateChartPlots()
        replayService.seek(to: clamped + 1)
    }

    func toggleMeasurement() {
        if !measurementFlag {
            startMeasurement()
        } else {
            stopMeasurement()
        }
    }

    private func startMeasurement() {
        // 次の計測開始時に前回のグラフ（前回計測／再生の残り）をクリアする。
        clearChart()
        stats.startMeasurement(quality: transSpeed + 1)

        memelib.setSelectMode(UInt32(selectMode + 1))
        memelib.setTransMode(UInt32(transSpeed + 1))
        memelib.setAccelRange(UInt32(accelRange))
        memelib.setGyroRange(UInt32(gyroRange))

        if let socket = socket {
            socket.headerString = headerString()
            socket.writeHeader()
        }

        measurementFlag = true
        phase = .measuring
        memelib.startDataReport()
    }

    private func stopMeasurement() {
        memelib.stopDataReport()
        stats.stopMeasurement()

        DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) { [weak self] in
            guard let self = self else { return }
            self.measurementFlag = false
            self.phase = .connected

            self.flushCsv()
            // 確定したCSVファイルへ、計測中にタップで付けた Artifact を書き戻す。
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

    // MARK: - Settings sheet

    func openSettings() {
        showingSettings = true
    }

    func settingsDidApply() {
        socketStop()
        socketStart()
        showLocalPort()
        // ローカルタイム変換の切り替えを、停止中／一時停止中の表示にも即反映する。
        updateChartPlots()
    }

    // MARK: - Chart selection

    func applyChartSelection() {
        chart1Title = "Chart1：\(chartCategoryOptions[chart1Category])"
        chart2Title = "Chart2：\(chartCategoryOptions[chart2Category])"
        chart3Title = "Chart3：\(chartCategoryOptions[chart3Category])"

        chart1Plot.applyCategory(chart1Category)
        chart2Plot.applyCategory(chart2Category)
        chart3Plot.applyCategory(chart3Category)
    }

    // MARK: - Data → dictionary

    /// 最初の1パケットは前回カウンタの基準取得のみに使い、CSVには記録しない (nil を返す)。
    private func dataToDictionary(_ data: AcademicData) -> [String: Any]? {
        guard stats.registerPacket(count: Int(data.cnt)) else { return nil }

        let shouldMark = isFreeMarking
        isFreeMarking = false

        return [
            "data": data,
            "packetCount": NSNumber(value: stats.totalCount),
            "date": data.date ?? Date(),
            "isFreeMarking": NSNumber(value: shouldMark)
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

    // MARK: - Chart update

    /// チャート描画のサンプリング周波数（Hz）。時間軸ラベル（秒 = 行数 / 周波数）に使う。
    /// 再生中は再生ファイルの設定、計測中はデバイスの Trans Speed に従う。
    private var chartSampleRate: Int {
        if let info = replayInfo, phase == .replaying || phase == .replayReady {
            return info.transMode == MEMEQuality_High ? 100 : 50
        }
        return memelib?.getTransMode() == MEMEQuality_High ? 100 : 50
    }

    /// 再描画を間引く周期。データはフルレートで取り込みつつ、Canvas 再描画を抑える。
    /// （2x/3x 再生でデータ取込が速くなっても描画負荷が線形に増えないようにするため。）
    /// 描画点数が多い30秒窓のみ10Hz、それ以外（3/7/15秒窓）は25Hzで再描画する。
    /// 再生速度に比例してストライドも伸ばすことで、速度を上げても描画周期（再描画Hz）は
    /// x1 のときと同じままにし、1回の再描画で進むデータ量だけを増やす。
    private var chartRenderStride: Int {
        let targetHz: Double = xRangeSeconds >= 30 ? 10 : 25
        let base = max(1, Int((Double(chartSampleRate) / targetHz).rounded()))
        let speed = phase == .replaying ? replaySpeed : 1
        return base * speed
    }

    /// 1サンプルをバッファへ追加し、スロットリング周期ごとにプロットを更新する。
    /// ハム（50/60Hz）成分を残すため間引かず全サンプルを保持する。
    private func appendChartSample(_ data: AcademicData) {
        chartService.append(data)
        chartRenderCounter += 1
        if chartRenderCounter % chartRenderStride == 0 {
            updateChartPlots()
        }
    }

    private func updateChartPlots() {
        chartService.updatePlots(chart1: &chart1Plot,
                                 chart1Category: chart1Category,
                                 chart1Eog: chart1EogToggles,
                                 chart1Gyro: chart1GyroToggles,
                                 chart1Accel: chart1AccelToggles,
                                 chart2: &chart2Plot,
                                 chart2Category: chart2Category,
                                 chart2Eog: chart2EogToggles,
                                 chart2Gyro: chart2GyroToggles,
                                 chart2Accel: chart2AccelToggles,
                                 chart3: &chart3Plot,
                                 chart3Category: chart3Category,
                                 chart3Eog: chart3EogToggles,
                                 chart3Gyro: chart3GyroToggles,
                                 chart3Accel: chart3AccelToggles,
                                 sampleRate: chartSampleRate,
                                 xRangeSeconds: xRangeSeconds,
                                 artifacts: currentChartArtifacts())
    }

    /// 各グラフへ表示する Artifact（キー＝絶対チャートサンプル位置 → 文字列）。
    /// 再生中：再生元CSVに記録済みのものと、この再生中にタップで付けた未書き戻しのものを併せて返す。
    /// 計測中：タップで付けた未書き戻しのものを返す（停止時にCSVへ書き戻す）。
    /// それ以外は空。
    private func currentChartArtifacts() -> [Int: String] {
        switch phase {
        case .replaying:
            guard let info = replayInfo else { return [:] }
            guard !pendingArtifacts.isEmpty else { return info.artifacts }
            return info.artifacts.merging(pendingArtifacts) { _, tapped in tapped }
        case .measuring:
            return pendingArtifacts
        default:
            return [:]
        }
    }

    // MARK: - Chart X-axis range

    var xRangeSeconds: Int { xRangeOptions[xRangeIndex] }
    var canZoomInXRange: Bool { xRangeIndex > 0 }
    var canZoomOutXRange: Bool { xRangeIndex < xRangeOptions.count - 1 }

    /// + ボタン：より狭い（短い）X軸レンジへ。
    func zoomInXRange() {
        guard canZoomInXRange else { return }
        xRangeIndex -= 1
        refreshPausedWindowForRangeChange()
    }

    /// － ボタン：より広い（長い）X軸レンジへ。
    func zoomOutXRange() {
        guard canZoomOutXRange else { return }
        xRangeIndex += 1
        refreshPausedWindowForRangeChange()
    }

    /// 一時停止中のみ、X軸レンジ変更を現在位置の静的表示へ即反映する。
    /// （再生中は次の tick が新しいレンジで描画するため何もしなくてよい。）
    private func refreshPausedWindowForRangeChange() {
        guard phase == .replaying, isReplayPaused else { return }
        fillReplayWindow(endingAt: currentReplayIndex)
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
    var showConnect: Bool { phase == .deviceFound || phase == .connected || phase == .replayReady || phase == .replaying }
    var connectButtonLabel: String {
        (phase == .connected || phase == .replayReady || phase == .replaying) ? "Disconnect" : "Connect"
    }
    var showMeasurement: Bool { phase == .connected || phase == .measuring }
    var showFreeMarking: Bool { phase == .measuring }
    // 再生中のみ Record ボタンを表示する（Start Replay は廃止し、読み込み時に自動再生する）。
    var showReplayControls: Bool { phase == .replaying }
    var replayButtonLabel: String { "Record" }
    var showReplayPause: Bool { phase == .replaying }
    var replayPauseButtonLabel: String { isReplayPaused ? "Resume" : "Pause" }
    var isInputDisabled: Bool { phase == .measuring || phase == .replayReady || phase == .replaying }
    var showXRangeControls: Bool { phase == .measuring || phase == .replaying }
    var showReplayScrubber: Bool { phase == .replaying }
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
    }

    func memeAcademicStandardDataReceivedDelegate(data: AcademicStandardData) {
        ingestPacket(data: data)
        ingestForDisplay(standard: data)
        appendChartSample(data)
    }

    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData) {
        ingestPacket(data: data)
        ingestForDisplay(full: data)
        appendChartSample(data)
    }

    func memeAcademicQuaternionDataReceivedDelegate(data: AcademicQuaternionData) {
        ingestPacket(data: data)
        displayCnt = data.cnt
    }

    private func ingestPacket(data: AcademicData) {
        // 受信時刻をサンプル自身に持たせる。CSV/ソケットの DATE 列と、
        // チャートX軸のタイムスタンプ表示はどちらもこの時刻を使う。
        data.date = Date()
        if let row = dataToDictionary(data) {
            persistence.append(row)
            saveCsv()
            if socket?.isConnected() == true, let last = persistence.lastRow {
                socketDatas.append(last)
                writeSocket()
            }
        }
        stats.bumpDataCount()
        displayBattLv = data.battLv
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

// =============================================================================
// MARK: - Supporting types
// =============================================================================

struct EogToggles {
    var left: Bool = false
    var right: Bool = false
    var deltaH: Bool = true
    var deltaV: Bool = true
}

struct GyroToggles {
    var x: Bool = true
    var y: Bool = true
    var z: Bool = true
}

struct AccelToggles {
    var x: Bool = true
    var y: Bool = true
    var z: Bool = true
}

struct ChartSeries: Identifiable {
    var id: String { name }
    let name: String
    let color: Color
    /// 波形の値。X軸位置は配列内インデックス（等間隔サンプリング前提）で表す。
    var values: [Double]
}

/// 再生中にチャート上へ表示する Artifact（1件）。
struct ChartArtifact {
    /// 対象サンプルのストリーム全体での絶対位置（＝再生元CSVのデータ行インデックス）。
    /// 波形と同じ右詰めロジックでX座標へ変換する。
    let sampleIndex: Int
    let text: String
}

struct ChartPlot {
    /// カテゴリごとに決まる基準の縦軸範囲。実際の表示範囲はこれに yScale を掛けたもの。
    var baseYMin: Double
    var baseYMax: Double
    /// 縦軸の拡大率。小さいほど拡大（max-min が狭い）。等倍を中央に、上下2段階ずつ持つ。
    /// Gyro/Accel（基準 ±8000）では ±2000 / ±4000 / ±8000 / ±16000 / ±32000 になる。
    /// 生値は符号付き16bitなので ±32000 が実質の上限。
    static let yScaleOptions: [Double] = [0.25, 0.5, 1, 2, 4]
    /// yScaleOptions のインデックス。既定は等倍（1）。
    var yScaleIndex: Int = 2

    var yScale: Double { Self.yScaleOptions[min(max(yScaleIndex, 0), Self.yScaleOptions.count - 1)] }
    var yMin: Double { baseYMin * yScale }
    var yMax: Double { baseYMax * yScale }

    var canZoomInY: Bool { yScaleIndex > 0 }
    var canZoomOutY: Bool { yScaleIndex < Self.yScaleOptions.count - 1 }

    /// 拡大：縦軸の max-min を半分にする（下限 0.25 倍）。
    mutating func zoomInY() {
        guard canZoomInY else { return }
        yScaleIndex -= 1
    }

    /// 縮小：縦軸の max-min を2倍にする（上限 4 倍）。
    mutating func zoomOutY() {
        guard canZoomOutY else { return }
        yScaleIndex += 1
    }

    /// 表示ウィンドウの全幅（サンプル数）＝ xRangeSeconds × sampleRate。X座標の正規化に使う。
    var windowSamples: Int = 7 * 100
    /// 最新サンプル（＝右端）のストリーム全体での絶対サンプル位置。
    /// 波形を右詰めで描画し、時間軸ラベル（秒 = 絶対サンプル位置 / 周波数）を算出するために使う。
    var latestSampleIndex: Int = 0
    /// サンプリング周波数（Hz）。時間軸ラベル（秒 = 絶対サンプル位置 / 周波数）算出に使う。
    var sampleRate: Int = 100
    /// 最新サンプル（＝右端）の記録時刻（UTC基準の絶対時刻）。
    /// X軸ラベルはこれを基準に、1サンプル = 1/sampleRate 秒として左方向へ遡って求める。
    /// 時刻が分からない場合（DATE列が壊れているCSVなど）は nil で、経過時間表示へフォールバックする。
    var latestSampleDate: Date? = nil
    /// X軸のタイムスタンプをローカルタイムへ変換して表示するか（Setting 由来）。false ならUTCのまま表示する。
    var convertToLocalTime: Bool = true
    var series: [ChartSeries] = []
    /// 可視ウィンドウ内に入る Artifact（再生中のみ設定。Y軸上限付近に文字列を描画する）。
    var artifacts: [ChartArtifact] = []

    mutating func reset() {
        latestSampleIndex = 0
        latestSampleDate = nil
        series.removeAll()
        artifacts.removeAll()
    }

    mutating func applyCategory(_ category: Int) {
        switch category {
        case 0: baseYMin = -1200; baseYMax = 1200
        default: baseYMin = -8000; baseYMax = 8000
        }
        series.removeAll()
    }
}
