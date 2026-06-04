//
//  MEMEViewModel.swift
//  MEME_Academic
//
//  SwiftUI 用 ViewModel。
//  既存 ViewController.swift の状態管理・データ処理ロジックを集約する。
//

import Foundation
import Observation
import CoreBluetooth
import AppKit
import UniformTypeIdentifiers
import SwiftUI

@MainActor
@Observable
final class MEMEViewModel: NSObject {

    // MARK: - Phase

    enum Phase {
        case idle
        case deviceFound
        case connected
        case measuring
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
    var successRateText: String = "0.00%"
    var successRateValue: Double = 0
    var communicationText: String = "0.00%"
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

    private var csvDatas: [[String: Any]] = []
    private var csvManager = CsvManager()
    private var mPrevCount: Int = -1
    private var mPrevTime: Int = 0
    private var mTotalCount: Int = 0
    private var mErrorCount: Int = 0
    private var mQuality: Int = 1
    private var dataCount: Int = 0
    private var startDate = Date()
    private var dataCount200ms: Int = 0
    private var socketDatas: [[String: Any]] = []
    private var chartDatas: [AcademicData] = []
    private var isFreeMarking = false

    private var communicationTimer: Timer?
    private var socket: TCPSocket?

    private var peripheralManager: CBPeripheralManager?

    // Chart view-buffer length matches existing logic: xMax(200) - xLongScale/2(12)
    private let chartLimit: Int = 200 - 25 / 2

    // MARK: - Init

    override init() {
        super.init()
        UserSetting.fristSetting()
        memelib = MEMELibFactory.make()
        memelib.delegate = self

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
        csvDatas = []
        csvManager = CsvManager()
        mPrevCount = -1
        mPrevTime = 0
        mTotalCount = 0
        mErrorCount = 0
        mQuality = 1
        dataCount = 0
        startDate = Date()
        dataCount200ms = 0
        socketDatas = []
        chartDatas = []
        socketStatusText = "Status : "
        isFreeMarking = false
        chart1Plot.reset()
        chart2Plot.reset()
        chart3Plot.reset()
    }

    // MARK: - Scan / Connect actions

    func startScan() {
        NSLog("Call : startScanningPeripherals")
        if MEMELibFactory.isMock {
            foundDevices.removeAll()
            selectedDevice = ""
            memelib.startScanningPeripherals()
        } else {
            peripheralManager = CBPeripheralManager(delegate: self,
                                                    queue: nil,
                                                    options: [CBPeripheralManagerOptionShowPowerAlertKey: "YES"])
        }
    }

    func toggleConnect() {
        if connectedFlag {
            NSLog("Call : disconnectPeripheral")
            memelib.disconnectPeripheral()
        } else {
            guard !selectedDevice.isEmpty else { return }
            NSLog("Call : connectPeripheral")
            memelib.connectPeripheral(deviceName: selectedDevice)
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
        startDate = Date()
        startCommunicationTimer()

        memelib.setSelectMode(UInt32(selectMode + 1))
        memelib.setTransMode(UInt32(transSpeed + 1))
        mQuality = transSpeed + 1
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
        stopCommunicationTimer()

        DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) { [weak self] in
            guard let self = self else { return }
            self.measurementFlag = false
            self.phase = .connected

            self.flushCsv()

            if UserSetting.getShowSaveFileDialog() {
                self.fileMove()
            } else {
                self.csvManager.reset()
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
        let count = Int(data.cnt)
        var deff = 0
        if mPrevCount < 0 {
            deff = 0
            mPrevTime = Int(Date().timeIntervalSince1970)
        } else {
            if mPrevCount < count {
                deff = count - mPrevCount
            } else if mPrevCount > count {
                deff = 0x1000 - mPrevCount + count
            }
        }
        mPrevCount = count
        mPrevTime += deff * 10 * mQuality

        if deff == 0 {
            mTotalCount += deff + 1
            mErrorCount += deff
        } else {
            mTotalCount += deff
            if deff - 1 > 0 {
                mErrorCount += deff - 1
            }
        }

        let shouldMark = isFreeMarking
        isFreeMarking = false

        return [
            "data": data,
            "packetCount": NSNumber(value: mTotalCount),
            "date": Date(),
            "isFreeMarking": NSNumber(value: shouldMark)
        ]
    }

    // MARK: - CSV / Socket

    private func saveCsv() { saveCsvIfNeeded(force: false) }
    private func flushCsv() { saveCsvIfNeeded(force: true) }

    private func saveCsvIfNeeded(force: Bool) {
        if csvDatas.isEmpty { return }
        if force || csvDatas.count >= 100 / mQuality {
            if !csvManager.isSave {
                let directoryPath = UserSetting.getSaveFilePath()
                let formatter = DateFormatter()
                formatter.locale = Locale(identifier: "ja_JP")
                formatter.dateFormat = "yyyyMMddHHmmss"
                let dateString = formatter.string(from: Date())
                let fileName = "\(memelib.macAddress)_\(dateString).csv"
                var buffer = headerString()
                dataToStoring(csvDatas, stringBuffer: &buffer)
                if let data = buffer.data(using: .utf8) {
                    csvManager.create(directoryPath: directoryPath, fileName: fileName, firstData: data)
                }
            } else {
                var buffer = ""
                dataToStoring(csvDatas, stringBuffer: &buffer)
                if let data = buffer.data(using: .utf8) {
                    csvManager.append(data)
                }
            }
            csvDatas.removeAll()
        }
    }

    private func writeSocket() {
        if socketDatas.count >= 10 {
            var buffer = ""
            dataToStoring(socketDatas, stringBuffer: &buffer)
            socket?.writeData(buffer)
            socketDatas.removeAll()
        }
    }

    private func headerString() -> String {
        let selectModeStr: String = {
            switch memelib.getSelectMode() {
            case MEMEMode_Standard: return "Standard"
            case MEMEMode_Full: return "Full"
            default: return "Quaternion"
            }
        }()
        let transModeStr = memelib.getTransMode() == MEMEQuality_High ? "100Hz" : "50Hz"
        let accelRangeStr: String = {
            switch memelib.getAccelRange() {
            case MEMEAccelRange_2G: return "2g"
            case MEMEAccelRange_4G: return "4g"
            case MEMEAccelRange_8G: return "8g"
            default: return "16g"
            }
        }()
        let gyroRangeStr: String = {
            switch memelib.getGyroRange() {
            case MEMEGyroRange_250dps: return "250dps"
            case MEMEGyroRange_500dps: return "500dps"
            case MEMEGyroRange_1000dps: return "1000dps"
            default: return "2000dps"
            }
        }()
        var s = ""
        s += "// Data mode  : \(selectModeStr)\n"
        s += "// Transmission speed  : \(transModeStr)\n"
        s += "// Acceleration sensor's range  : \(accelRangeStr)\n"
        s += "// Gyroscope sensor's range  : \(gyroRangeStr)\n"
        s += "//\n"
        switch memelib.getSelectMode() {
        case MEMEMode_Standard:
            s += "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,EOG_L1,EOG_R1,EOG_L2,EOG_R2,EOG_H1,EOG_H2,EOG_V1,EOG_V2\n"
        case MEMEMode_Full:
            s += "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V\n"
        default:
            s += "//ARTIFACT,NUM,DATE,QUATERNION_W,QUATERNION_X,QUATERNION_Y,QUATERNION_Z\n"
        }
        return s
    }

    private func dataToStoring(_ datas: [[String: Any]], stringBuffer: inout String) {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyy/MM/dd HH:mm:ss.SS"
        for dic in datas {
            let date = dic["date"] as? Date ?? Date()
            let dateString = formatter.string(from: date)
            let packetCount = (dic["packetCount"] as? NSNumber)?.intValue ?? 0
            let isFreeMarkingValue = (dic["isFreeMarking"] as? NSNumber)?.boolValue ?? false
            let mark = isFreeMarkingValue ? "x" : ""
            switch memelib.getSelectMode() {
            case MEMEMode_Standard:
                if let d = dic["data"] as? AcademicStandardData {
                    stringBuffer += "\(mark),\(packetCount),\(dateString),\(d.accX),\(d.accY),\(d.accZ),\(d.eogL1),\(d.eogR1),\(d.eogL2),\(d.eogR2),\(d.eogH1),\(d.eogH2),\(d.eogV1),\(d.eogV2)\n"
                }
            case MEMEMode_Full:
                if let d = dic["data"] as? AcademicFullData {
                    stringBuffer += "\(mark),\(packetCount),\(dateString),\(d.accX),\(d.accY),\(d.accZ),\(d.gyroX),\(d.gyroY),\(d.gyroZ),\(d.eogL),\(d.eogR),\(d.eogH),\(d.eogV)\n"
                }
            default:
                if let d = dic["data"] as? AcademicQuaternionData {
                    stringBuffer += "\(mark),\(packetCount),\(dateString),\(d.quaternionW),\(d.quaternionX),\(d.quaternionY),\(d.quaternionZ)\n"
                }
            }
        }
    }

    private func updateSuccessRate() {
        let timeCount = Date().timeIntervalSince1970 - startDate.timeIntervalSince1970
        let rate: Double
        if timeCount > 0 {
            rate = (Double(dataCount) / (timeCount * 100.0 / Double(mQuality))) * 100.0
        } else {
            rate = 0
        }
        successRateValue = rate
        successRateText = String(format: "%.2f%%", rate)
    }

    private func startCommunicationTimer() {
        communicationTimer = Timer.scheduledTimer(withTimeInterval: 0.2, repeats: true) { [weak self] _ in
            Task { @MainActor in
                self?.updateCommunication()
            }
        }
    }

    private func stopCommunicationTimer() {
        communicationTimer?.invalidate()
        communicationTimer = nil
    }

    private func updateCommunication() {
        let comm = (Double(dataCount200ms) / (0.2 * 100.0 / Double(mQuality))) * 100.0
        communicationValue = comm
        communicationText = String(format: "%.2f%%", comm)
        dataCount200ms = 0
    }

    // MARK: - File move (save dialog)

    private func fileMove() {
        guard let sourceFilePath = csvManager.saveFilePath, !sourceFilePath.isEmpty else {
            csvManager.reset()
            return
        }
        let saveFileName = csvManager.saveFileName ?? ""

        let savePanel = NSSavePanel()
        savePanel.canCreateDirectories = true
        savePanel.showsTagField = false
        savePanel.isExtensionHidden = false
        if let csvType = UTType(filenameExtension: "csv") {
            savePanel.allowedContentTypes = [csvType]
        }
        savePanel.nameFieldStringValue = saveFileName
        savePanel.level = .modalPanel

        guard let window = NSApp.mainWindow else { return }
        savePanel.beginSheetModal(for: window) { result in
            if result == .OK, let url = savePanel.url {
                do {
                    try FileManager.default.copyItem(at: URL(fileURLWithPath: sourceFilePath), to: url)
                    try? FileManager.default.removeItem(at: URL(fileURLWithPath: sourceFilePath))
                } catch {
                    NSLog("コピー失敗:%@", error.localizedDescription)
                }
            }
        }
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

    // MARK: - Chart filtering helpers

    /// 既存ViewControllerと同じ「直近 limit 件」を取り出すロジック。
    private func slicedDatas() -> [AcademicData] {
        let datas = chartDatas
        let limit = chartLimit
        if datas.count >= limit {
            let startIndex = datas.count - limit
            return Array(datas[startIndex..<datas.count])
        } else {
            return datas
        }
    }

    private func computeXOffset(datasCount: Int) -> (xInitial: Float, xLabel: Int) {
        let xMax: Float = 200
        let xLong: Int = 25
        if Float(datasCount) > xMax - Float(xLong) {
            let pos = datasCount % xLong
            let xInitial = Float(pos) * -1
            let xLabel = (datasCount / xLong) - (Int(xMax) / xLong) + 1
            return (xInitial, xLabel)
        }
        return (0, 0)
    }

    private func updateChartPlots() {
        let sliced = slicedDatas()
        let (xInit, xLabel) = computeXOffset(datasCount: chartDatas.count)

        updateChartPlot(&chart1Plot,
                        category: chart1Category,
                        eog: chart1EogToggles,
                        gyro: chart1GyroToggles,
                        accel: chart1AccelToggles,
                        sliced: sliced,
                        xInitial: xInit, xLabel: xLabel)

        updateChartPlot(&chart2Plot,
                        category: chart2Category,
                        eog: chart2EogToggles,
                        gyro: chart2GyroToggles,
                        accel: chart2AccelToggles,
                        sliced: sliced,
                        xInitial: xInit, xLabel: xLabel)

        updateChartPlot(&chart3Plot,
                        category: chart3Category,
                        eog: chart3EogToggles,
                        gyro: chart3GyroToggles,
                        accel: chart3AccelToggles,
                        sliced: sliced,
                        xInitial: xInit, xLabel: xLabel)
    }

    private func updateChartPlot(_ plot: inout ChartPlot,
                                 category: Int,
                                 eog: EogToggles,
                                 gyro: GyroToggles,
                                 accel: AccelToggles,
                                 sliced: [AcademicData],
                                 xInitial: Float,
                                 xLabel: Int) {
        plot.xInitial = xInitial
        plot.xLabelStart = xLabel

        switch category {
        case 0: // EOG
            plot.yMin = -1200; plot.yMax = 1200
            plot.series = buildEogSeries(sliced: sliced, toggles: eog)
        case 1: // Gyro (Full only)
            plot.yMin = -36000; plot.yMax = 36000
            plot.series = buildGyroSeries(sliced: sliced, toggles: gyro)
        case 2: // Accel
            plot.yMin = -36000; plot.yMax = 36000
            plot.series = buildAccelSeries(sliced: sliced, toggles: accel)
        default: break
        }
    }

    private func buildEogSeries(sliced: [AcademicData], toggles: EogToggles) -> [ChartSeries] {
        var L: [Double] = []
        var R: [Double] = []
        var H: [Double] = []
        var V: [Double] = []
        for d in sliced {
            if let s = d as? AcademicStandardData {
                if toggles.left { L.append(Double(s.eogL1)) }
                if toggles.right { R.append(Double(s.eogR1)) }
                if toggles.deltaH { H.append(Double(s.eogH1)) }
                if toggles.deltaV { V.append(Double(s.eogV1)) }
            } else if let f = d as? AcademicFullData {
                if toggles.left { L.append(Double(f.eogL)) }
                if toggles.right { R.append(Double(f.eogR)) }
                if toggles.deltaH { H.append(Double(f.eogH)) }
                if toggles.deltaV { V.append(Double(f.eogV)) }
            }
        }
        var out: [ChartSeries] = []
        if toggles.left { out.append(ChartSeries(name: "EOG L", color: .yellow, values: L)) }
        if toggles.right { out.append(ChartSeries(name: "EOG R", color: .green, values: R)) }
        if toggles.deltaH { out.append(ChartSeries(name: "ΔH", color: .red, values: H)) }
        if toggles.deltaV { out.append(ChartSeries(name: "ΔV", color: .blue, values: V)) }
        return out
    }

    private func buildGyroSeries(sliced: [AcademicData], toggles: GyroToggles) -> [ChartSeries] {
        var X: [Double] = []; var Y: [Double] = []; var Z: [Double] = []
        for d in sliced {
            guard let f = d as? AcademicFullData else { continue }
            if toggles.x { X.append(Double(f.gyroX)) }
            if toggles.y { Y.append(Double(f.gyroY)) }
            if toggles.z { Z.append(Double(f.gyroZ)) }
        }
        var out: [ChartSeries] = []
        if toggles.x { out.append(ChartSeries(name: "Gyro X", color: .red, values: X)) }
        if toggles.y { out.append(ChartSeries(name: "Gyro Y", color: .green, values: Y)) }
        if toggles.z { out.append(ChartSeries(name: "Gyro Z", color: .blue, values: Z)) }
        return out
    }

    private func buildAccelSeries(sliced: [AcademicData], toggles: AccelToggles) -> [ChartSeries] {
        var X: [Double] = []; var Y: [Double] = []; var Z: [Double] = []
        for d in sliced {
            if let s = d as? AcademicStandardData {
                if toggles.x { X.append(Double(s.accX) + UserSetting.getXAxis()) }
                if toggles.y { Y.append(Double(s.accY) + UserSetting.getYAxis()) }
                if toggles.z { Z.append(Double(s.accZ) + UserSetting.getZAxis()) }
            } else if let f = d as? AcademicFullData {
                if toggles.x { X.append(Double(f.accX) + UserSetting.getXAxis()) }
                if toggles.y { Y.append(Double(f.accY) + UserSetting.getYAxis()) }
                if toggles.z { Z.append(Double(f.accZ) + UserSetting.getZAxis()) }
            }
        }
        var out: [ChartSeries] = []
        if toggles.x { out.append(ChartSeries(name: "Acc X", color: .red, values: X)) }
        if toggles.y { out.append(ChartSeries(name: "Acc Y", color: .green, values: Y)) }
        if toggles.z { out.append(ChartSeries(name: "Acc Z", color: .blue, values: Z)) }
        return out
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
    var showConnect: Bool { phase == .deviceFound || phase == .connected }
    var showMeasurement: Bool { phase == .connected || phase == .measuring }
    var showFreeMarking: Bool { phase == .measuring }
    var isInputDisabled: Bool { phase == .measuring }
}

// =============================================================================
// MARK: - CBPeripheralManagerDelegate
// =============================================================================
extension MEMEViewModel: @preconcurrency CBPeripheralManagerDelegate {
    func peripheralManagerDidUpdateState(_ peripheral: CBPeripheralManager) {
        if peripheral.state == .poweredOn {
            NSLog("bluetooth ON")
            foundDevices.removeAll()
            selectedDevice = ""
            memelib.startScanningPeripherals()
        } else {
            NSLog("bluetooth それ以外")
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
            phase = .deviceFound
        } else {
            NSLog("memePeripheralFoundDelegate %d", result)
            memelib.stopScanningPeripherals()
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
        phase = .idle
    }

    func memeAcademicStandardDataReceivedDelegate(data: AcademicStandardData) {
        csvDatas.append(dataToDictionary(data))
        saveCsv()
        if socket?.isConnected() == true, let last = csvDatas.last {
            socketDatas.append(last)
            writeSocket()
        }
        ingestForDisplay(standard: data)
        batteryAndCounters(battLv: data.battLv)

        let interval = memelib.getTransMode() == MEMEQuality_High ? 4 : 2
        if mTotalCount % interval == 0 {
            chartDatas.append(data)
            updateChartPlots()
        }
    }

    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData) {
        csvDatas.append(dataToDictionary(data))
        saveCsv()
        if socket?.isConnected() == true, let last = csvDatas.last {
            socketDatas.append(last)
            writeSocket()
        }
        ingestForDisplay(full: data)
        batteryAndCounters(battLv: data.battLv)

        let interval = memelib.getTransMode() == MEMEQuality_High ? 4 : 2
        if mTotalCount % interval == 0 {
            chartDatas.append(data)
            updateChartPlots()
        }
    }

    func memeAcademicQuaternionDataReceivedDelegate(data: AcademicQuaternionData) {
        csvDatas.append(dataToDictionary(data))
        saveCsv()
        if socket?.isConnected() == true, let last = csvDatas.last {
            socketDatas.append(last)
            writeSocket()
        }
        displayCnt = data.cnt
        batteryAndCounters(battLv: data.battLv)
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

    private func batteryAndCounters(battLv: UInt16) {
        dataCount += 1
        updateSuccessRate()
        dataCount200ms += 1
        displayBattLv = battLv
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
    var left: Bool = true
    var right: Bool = true
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
