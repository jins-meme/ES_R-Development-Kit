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
    var chart1Plot = ChartPlot(yMin: -1200, yMax: 1200)
    var chart2Plot = ChartPlot(yMin: -36000, yMax: 36000)
    var chart3Plot = ChartPlot(yMin: -36000, yMax: 36000)

    // Settings sheet presentation
    var showingSettings: Bool = false

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
    private var replayRowCounter: Int = 0

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
        chartService.reset()
        socketDatas = []
        socketStatusText = "Status : "
        isFreeMarking = false
        chart1Plot.reset()
        chart2Plot.reset()
        chart3Plot.reset()
    }

    // MARK: - Scan / Connect actions

    func startScan() {
        NSLog("Call : startScanningPeripherals")
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
            memelib.connectPeripheral(deviceName: selectedDevice)
        }
    }

    // MARK: - File Replay actions

    func chooseReplayFile() {
        guard phase == .idle else { return }
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

    private func loadReplayFile(url: URL) {
        do {
            let info = try CsvReplayService.parse(url: url)
            replayInfo = info
            selectMode = Int(info.mode) - 1
            transSpeed = info.transMode == MEMEQuality_High ? 0 : 1
            accelRange = Int(info.accelRange)
            gyroRange = Int(info.gyroRange)
            connectionStateText = "State : \(info.fileName)"
            phase = .replayReady
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
        replayRowCounter = 0
        replayService.start(rows: info.rows,
                            transMode: info.transMode,
                            onRow: { [weak self] data in
            self?.ingestReplayRow(data, mode: info.mode)
        }, onFinished: { [weak self] in
            self?.finishReplay()
        })
    }

    private func finishReplay() {
        replayService.stop()
        guard phase == .replaying else { return }
        phase = .replayReady
        chartService.reset()
        chart1Plot.reset()
        chart2Plot.reset()
        chart3Plot.reset()
        stats.reset()
        successRateValue = 0; successRateText = "0.0%"
        communicationValue = 0; communicationText = "0.0%"
    }

    private func disconnectReplay() {
        replayService.stop()
        replayInfo = nil
        phase = .idle
        connectionStateText = "State : Disconnected"
        reset()
    }

    private func ingestReplayRow(_ data: AcademicData, mode: UInt32) {
        displayBattLv = data.battLv

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

        replayRowCounter += 1
        let interval = transSpeed == 0 ? 4 : 2
        if replayRowCounter % interval == 0 {
            chartService.append(data)
            updateChartPlots()
        }
    }

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

    private func dataToDictionary(_ data: AcademicData) -> [String: Any] {
        stats.registerPacket(count: Int(data.cnt))

        let shouldMark = isFreeMarking
        isFreeMarking = false

        return [
            "data": data,
            "packetCount": NSNumber(value: stats.totalCount),
            "date": Date(),
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
                                 chart3Accel: chart3AccelToggles)
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

    var showStartScan: Bool { phase == .idle }
    var showFileReplay: Bool { phase == .idle }
    var showConnect: Bool { phase == .deviceFound || phase == .connected || phase == .replayReady || phase == .replaying }
    var connectButtonLabel: String {
        (phase == .connected || phase == .replayReady || phase == .replaying) ? "Disconnect" : "Connect"
    }
    var showMeasurement: Bool { phase == .connected || phase == .measuring }
    var showFreeMarking: Bool { phase == .measuring }
    var showReplayControls: Bool { phase == .replayReady || phase == .replaying }
    var replayButtonLabel: String { phase == .replaying ? "Stop Replay" : "Start Replay" }
    var isInputDisabled: Bool { phase == .measuring || phase == .replayReady || phase == .replaying }
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
            connectionStateText = "State : Scan timeout. Tap Start Scan to retry."
        }
    }

    func memePeripheralConnectedDelegate(result: UInt32) {
        NSLog("memePeripheralConnectedDelegate : %d", result)
        connectedFlag = true
        connectionStateText = "State : Connected"
        memeVersionText = "MEME Version：\(memelib.memeVersion.major).\(memelib.memeVersion.minor).\(memelib.memeVersion.revision)"
        phase = .connected
        syncDeviceSettings()
    }

    func memePeripheralDisconnectedDelegate(result: UInt32) {
        NSLog("memePeripheralDisconnectedDelegate : %d", result)
        connectedFlag = false
        connectionStateText = "State : Disconnected"
        foundDevices.removeAll()
        selectedDevice = ""
        phase = .idle
    }

    func memeAcademicStandardDataReceivedDelegate(data: AcademicStandardData) {
        ingestPacket(data: data)
        ingestForDisplay(standard: data)

        let interval = memelib.getTransMode() == MEMEQuality_High ? 4 : 2
        if stats.totalCount % interval == 0 {
            chartService.append(data)
            updateChartPlots()
        }
    }

    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData) {
        ingestPacket(data: data)
        ingestForDisplay(full: data)

        let interval = memelib.getTransMode() == MEMEQuality_High ? 4 : 2
        if stats.totalCount % interval == 0 {
            chartService.append(data)
            updateChartPlots()
        }
    }

    func memeAcademicQuaternionDataReceivedDelegate(data: AcademicQuaternionData) {
        ingestPacket(data: data)
        displayCnt = data.cnt
    }

    private func ingestPacket(data: AcademicData) {
        let row = dataToDictionary(data)
        persistence.append(row)
        saveCsv()
        if socket?.isConnected() == true, let last = persistence.lastRow {
            socketDatas.append(last)
            writeSocket()
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
    var values: [Double]
}

struct ChartPlot {
    var yMin: Double
    var yMax: Double
    var xInitial: Float = 0
    var xLabelStart: Int = 0
    var series: [ChartSeries] = []

    mutating func reset() {
        xInitial = 0
        xLabelStart = 0
        series.removeAll()
    }

    mutating func applyCategory(_ category: Int) {
        switch category {
        case 0: yMin = -1200; yMax = 1200
        default: yMin = -36000; yMax = 36000
        }
        series.removeAll()
    }
}
