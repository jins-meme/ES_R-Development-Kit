//
//  ViewController.swift
//  MEME_Academic
//
//  Created by D-CLUE on 2017/03/22.
//  Copyright © 2017年 jins-jp. All rights reserved.
//

import Cocoa
import CoreBluetooth
import UniformTypeIdentifiers

class ViewController: NSViewController {

    // MARK: - IBOutlets
    @IBOutlet weak var button_StartScan: NSButton!
    @IBOutlet weak var button_Connect: NSButton!
    @IBOutlet weak var combobox_MEME: NSComboBox!
    @IBOutlet weak var label_StateConnect: NSTextField!

    @IBOutlet weak var combobox_SelectMode: NSComboBox!
    @IBOutlet weak var combobox_TransSpeed: NSComboBox!
    @IBOutlet weak var combobox_AccelRange: NSComboBox!
    @IBOutlet weak var combobox_GyroRange: NSComboBox!

    @IBOutlet weak var button_StartMeasurement: NSButton!
    @IBOutlet weak var button_FreeMarking: NSButton!

    @IBOutlet weak var label_DataCnt: NSTextField!
    @IBOutlet weak var label_DataAccX: NSTextField!
    @IBOutlet weak var label_DataAccY: NSTextField!
    @IBOutlet weak var label_DataAccZ: NSTextField!
    @IBOutlet weak var label_DataGyroX: NSTextField!
    @IBOutlet weak var label_DataGyroY: NSTextField!
    @IBOutlet weak var label_DataGyroZ: NSTextField!
    @IBOutlet weak var label_DataEogL: NSTextField!
    @IBOutlet weak var label_DataEogR: NSTextField!
    @IBOutlet weak var label_DataEogH: NSTextField!
    @IBOutlet weak var label_DataEogV: NSTextField!
    @IBOutlet weak var label_DataBattLv: NSTextField!

    @IBOutlet weak var button_Settings: NSButton!
    @IBOutlet weak var scrollview_Chart1: NSScrollView!
    @IBOutlet weak var scrollview_Chart2: NSScrollView!
    @IBOutlet weak var scrollview_Chart3: NSScrollView!
    @IBOutlet weak var box_BatteryLevel: NSBox!
    @IBOutlet weak var label_LocalAddress: NSTextField!
    @IBOutlet weak var label_LocalProt: NSTextField!
    @IBOutlet weak var label_SocketStatus: NSTextField!
    @IBOutlet weak var button_Chart_Apply: NSButton!
    @IBOutlet weak var label_AppVersion: NSTextField!
    @IBOutlet weak var label_MemeVersion: NSTextField!
    @IBOutlet weak var box_SuccessRate: NSBox!
    @IBOutlet weak var label_SuccessRate: NSTextField!
    @IBOutlet weak var box_Communication: NSBox!
    @IBOutlet weak var label_Communication: NSTextField!

    @IBOutlet weak var combobox_Chart1: NSComboBox!
    @IBOutlet weak var combobox_Chart2: NSComboBox!
    @IBOutlet weak var combobox_Chart3: NSComboBox!

    @IBOutlet weak var textField_Chart1: NSTextField!
    @IBOutlet weak var box_Chart1_Electrooculography: NSBox!
    @IBOutlet weak var button_Chart1_Electrooculography_Left: NSButton!
    @IBOutlet weak var button_Chart1_Electrooculography_Right: NSButton!
    @IBOutlet weak var button_Chart1_Electrooculography_DeltaH: NSButton!
    @IBOutlet weak var button_Chart1_Electrooculography_DeltaV: NSButton!
    @IBOutlet weak var box_Chart1_Gyroscope: NSBox!
    @IBOutlet weak var button_Chart1_Gyroscope_X_Axis: NSButton!
    @IBOutlet weak var button_Chart1_Gyroscope_Y_Axis: NSButton!
    @IBOutlet weak var button_Chart1_Gyroscope_Z_Axis: NSButton!
    @IBOutlet weak var box_Chart1_Accelerometer: NSBox!
    @IBOutlet weak var button_Chart1_Accelerometer_X_Axis: NSButton!
    @IBOutlet weak var button_Chart1_Accelerometer_Y_Axis: NSButton!
    @IBOutlet weak var button_Chart1_Accelerometer_Z_Axis: NSButton!
    @IBOutlet weak var textField_Chart1_Y1: NSTextField!
    @IBOutlet weak var textField_Chart1_Y2: NSTextField!
    @IBOutlet weak var textField_Chart1_Y3: NSTextField!
    @IBOutlet weak var textField_Chart1_Y4: NSTextField!
    @IBOutlet weak var textField_Chart1_Y5: NSTextField!
    @IBOutlet weak var textField_Chart1_Y6: NSTextField!
    @IBOutlet weak var textField_Chart1_Y7: NSTextField!

    @IBOutlet weak var textField_Chart2: NSTextField!
    @IBOutlet weak var box_Chart2_Electrooculography: NSBox!
    @IBOutlet weak var button_Chart2_Electrooculography_Left: NSButton!
    @IBOutlet weak var button_Chart2_Electrooculography_Right: NSButton!
    @IBOutlet weak var button_Chart2_Electrooculography_DeltaH: NSButton!
    @IBOutlet weak var button_Chart2_Electrooculography_DeltaV: NSButton!
    @IBOutlet weak var box_Chart2_Gyroscope: NSBox!
    @IBOutlet weak var button_Chart2_Gyroscope_X_Axis: NSButton!
    @IBOutlet weak var button_Chart2_Gyroscope_Y_Axis: NSButton!
    @IBOutlet weak var button_Chart2_Gyroscope_Z_Axis: NSButton!
    @IBOutlet weak var box_Chart2_Accelerometer: NSBox!
    @IBOutlet weak var button_Chart2_Accelerometer_X_Axis: NSButton!
    @IBOutlet weak var button_Chart2_Accelerometer_Y_Axis: NSButton!
    @IBOutlet weak var button_Chart2_Accelerometer_Z_Axis: NSButton!
    @IBOutlet weak var textField_Chart2_Y1: NSTextField!
    @IBOutlet weak var textField_Chart2_Y2: NSTextField!
    @IBOutlet weak var textField_Chart2_Y3: NSTextField!
    @IBOutlet weak var textField_Chart2_Y4: NSTextField!
    @IBOutlet weak var textField_Chart2_Y5: NSTextField!
    @IBOutlet weak var textField_Chart2_Y6: NSTextField!
    @IBOutlet weak var textField_Chart2_Y7: NSTextField!

    @IBOutlet weak var textField_Chart3: NSTextField!
    @IBOutlet weak var box_Chart3_Electrooculography: NSBox!
    @IBOutlet weak var button_Chart3_Electrooculography_Left: NSButton!
    @IBOutlet weak var button_Chart3_Electrooculography_Right: NSButton!
    @IBOutlet weak var button_Chart3_Electrooculography_DeltaH: NSButton!
    @IBOutlet weak var button_Chart3_Electrooculography_DeltaV: NSButton!
    @IBOutlet weak var box_Chart3_Gyroscope: NSBox!
    @IBOutlet weak var button_Chart3_Gyroscope_X_Axis: NSButton!
    @IBOutlet weak var button_Chart3_Gyroscope_Y_Axis: NSButton!
    @IBOutlet weak var button_Chart3_Gyroscope_Z_Axis: NSButton!
    @IBOutlet weak var box_Chart3_Accelerometer: NSBox!
    @IBOutlet weak var button_Chart3_Accelerometer_X_Axis: NSButton!
    @IBOutlet weak var button_Chart3_Accelerometer_Y_Axis: NSButton!
    @IBOutlet weak var button_Chart3_Accelerometer_Z_Axis: NSButton!
    @IBOutlet weak var textField_Chart3_Y1: NSTextField!
    @IBOutlet weak var textField_Chart3_Y2: NSTextField!
    @IBOutlet weak var textField_Chart3_Y3: NSTextField!
    @IBOutlet weak var textField_Chart3_Y4: NSTextField!
    @IBOutlet weak var textField_Chart3_Y5: NSTextField!
    @IBOutlet weak var textField_Chart3_Y6: NSTextField!
    @IBOutlet weak var textField_Chart3_Y7: NSTextField!

    // MARK: - Private Properties
    private var memelib: MEMELib_Academic!
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
    private var chartView1: ChartView!
    private var chartView2: ChartView!
    private var chartView3: ChartView!
    private var socket: TCPSocket?

    private var chart1Y: [NSTextField] = []
    private var chart2Y: [NSTextField] = []
    private var chart3Y: [NSTextField] = []

    private var peripheralManager: CBPeripheralManager?

    // MARK: - View Life Cycle
    override func viewDidLoad() {
        super.viewDidLoad()

        UserSetting.fristSetting()

        memelib = MEMELib_Academic()
        memelib.delegate = self

        connectedFlag = false
        measurementFlag = false

        chart1Y = [textField_Chart1_Y1, textField_Chart1_Y2, textField_Chart1_Y3,
                   textField_Chart1_Y4, textField_Chart1_Y5, textField_Chart1_Y6, textField_Chart1_Y7]
        chart2Y = [textField_Chart2_Y1, textField_Chart2_Y2, textField_Chart2_Y3,
                   textField_Chart2_Y4, textField_Chart2_Y5, textField_Chart2_Y6, textField_Chart2_Y7]
        chart3Y = [textField_Chart3_Y1, textField_Chart3_Y2, textField_Chart3_Y3,
                   textField_Chart3_Y4, textField_Chart3_Y5, textField_Chart3_Y6, textField_Chart3_Y7]

        reset()

        button_StartScan.isHidden = false
        button_Connect.isHidden = true
        button_StartMeasurement.isHidden = true
        button_FreeMarking.isHidden = true

        combobox_MEME.removeAllItems()

        combobox_SelectMode.addItems(withObjectValues: ["Standard", "Full", "Quaternion"])
        combobox_SelectMode.selectItem(at: 0)

        combobox_TransSpeed.addItems(withObjectValues: ["100Hz", "50Hz"])
        combobox_TransSpeed.selectItem(at: 0)

        combobox_AccelRange.addItems(withObjectValues: ["±2G", "±4G", "±8G", "±16G"])
        combobox_AccelRange.selectItem(at: 0)

        combobox_GyroRange.addItems(withObjectValues: ["±250dps", "±500dps", "±1000dps", "±2000dps"])
        combobox_GyroRange.selectItem(at: 0)

        combobox_Chart1.addItems(withObjectValues: ["Electrooculography", "Gyroscope", "Accelerometer"])
        combobox_Chart1.selectItem(at: 0)

        combobox_Chart2.addItems(withObjectValues: ["Electrooculography", "Gyroscope", "Accelerometer"])
        combobox_Chart2.selectItem(at: 1)

        combobox_Chart3.addItems(withObjectValues: ["Electrooculography", "Gyroscope", "Accelerometer"])
        combobox_Chart3.selectItem(at: 2)

        showAppVersion()
        chartView1 = setChartView(scrollview_Chart1, xMaxValue: 200, xMinValue: 0, yMaxValue: 1200, yMinValue: -1200)
        chartView2 = setChartView(scrollview_Chart2, xMaxValue: 200, xMinValue: 0, yMaxValue: 36000, yMinValue: -36000)
        chartView3 = setChartView(scrollview_Chart3, xMaxValue: 200, xMinValue: 0, yMaxValue: 36000, yMinValue: -36000)
        showLocalAddress()
        showLocalPort()
        showBatteryLevel(0)
        updateSuccessRate()
        updateCommunication()
        removeChartSelectionBorders()
        label_MemeVersion.stringValue = "MEME Version："

        let buttons: [NSButton] = [
            button_Settings, button_StartScan, button_Connect,
            button_StartMeasurement, button_FreeMarking, button_Chart_Apply
        ]

        let appearanceName = view.effectiveAppearance.bestMatch(from: [.aqua, .darkAqua])
        let isDark = appearanceName == .darkAqua
        let bgWhite: CGFloat = isDark ? 0.60 : 0.85
        let titleColor: NSColor = isDark ? .labelColor : NSColor(white: 0.15, alpha: 1.0)

        for button in buttons {
            button.wantsLayer = true
            button.layer?.cornerRadius = 14.0
            button.layer?.masksToBounds = true
            button.layer?.backgroundColor = NSColor(white: bgWhite, alpha: 1.0).cgColor

            let attr = NSMutableAttributedString(attributedString: button.attributedTitle)
            let range = NSRange(location: 0, length: attr.length)
            attr.addAttribute(.foregroundColor, value: titleColor, range: range)
            button.attributedTitle = attr
        }

        socketStart()
    }

    private func removeChartSelectionBorders() {
        let boxes: [NSBox] = [
            box_Chart1_Electrooculography, box_Chart1_Gyroscope, box_Chart1_Accelerometer,
            box_Chart2_Electrooculography, box_Chart2_Gyroscope, box_Chart2_Accelerometer,
            box_Chart3_Electrooculography, box_Chart3_Gyroscope, box_Chart3_Accelerometer
        ]
        for box in boxes {
            removeBorderFromChartSelectionBox(box)
            removeBorderFromChartSelectionAncestors(box)
        }
    }

    private func removeBorderFromChartSelectionBox(_ box: NSBox) {
        box.borderColor = .clear
        for subview in box.contentView?.subviews ?? [] {
            if let innerBox = subview as? NSBox {
                removeBorderFromChartSelectionBox(innerBox)
            }
        }
    }

    private func removeBorderFromChartSelectionAncestors(_ view: NSView) {
        var superview: NSView? = view.superview
        while let current = superview {
            if let box = current as? NSBox {
                box.borderColor = .clear
            }
            superview = current.superview
        }
    }

    override func viewWillAppear() {
        super.viewWillAppear()
        view.window?.title = "JINS MEME Academic"
    }

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
        label_SocketStatus.stringValue = "Status : "
        if let view = chartView1 {
            view.xTextFieldValue = 0
            view.xInitialPosition = 0
        }
        if let view = chartView2 {
            view.xTextFieldValue = 0
            view.xInitialPosition = 0
        }
        if let view = chartView3 {
            view.xTextFieldValue = 0
            view.xInitialPosition = 0
        }
        isFreeMarking = false
    }

    // MARK: - Display helpers

    private func showAppVersion() {
        NSLog("appVersion")
        let version = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? ""
        let build = Bundle.main.object(forInfoDictionaryKey: "CFBundleVersion") as? String ?? ""
        label_AppVersion.stringValue = "Version \(version).\(build)"
    }

    private func showLocalAddress() {
        NSLog("localAddress")
        let localAddress = Common.getIPAddress()
        label_LocalAddress.stringValue = "IP address:\(localAddress)"
    }

    private func showLocalPort() {
        NSLog("localPort")
        let localPort = UserSetting.getLocalPort()
        label_LocalProt.stringValue = "Prot:\(localPort)"
    }

    private func showBatteryLevel(_ battLv: Int) {
        box_BatteryLevel.frame = CGRect(x: 0, y: 0, width: CGFloat(100 / 5 * battLv), height: box_BatteryLevel.frame.size.height)
    }

    private func showMemeVersion() {
        NSLog("memeVersion")
        label_MemeVersion.stringValue = "MEME Version：\(memelib.memeVersion.major).\(memelib.memeVersion.minor).\(memelib.memeVersion.revision)"
    }

    // MARK: - Data dictionary

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

    private func saveCsv() {
        saveCsvIfNeeded(force: false)
    }

    private func flushCsv() {
        saveCsvIfNeeded(force: true)
    }

    private func saveCsvIfNeeded(force: Bool) {
        if csvDatas.isEmpty { return }

        if force || csvDatas.count >= 100 / mQuality {
            if !csvManager.isSave {
                NSLog("作成")
                let directoryPath = UserSetting.getSaveFilePath()
                let formatter = DateFormatter()
                formatter.locale = Locale(identifier: "ja_JP")
                formatter.dateFormat = "yyyyMMddHHmmss"
                let dateString = formatter.string(from: Date())
                let macAddressString = memelib.macAddress
                let fileName = "\(macAddressString)_\(dateString).csv"
                var buffer = headerString()
                dataToStoring(csvDatas, stringBuffer: &buffer)
                if let data = buffer.data(using: .utf8) {
                    csvManager.create(directoryPath: directoryPath, fileName: fileName, firstData: data)
                }
            } else {
                NSLog("追記")
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
            let isFreeMarkingString = isFreeMarkingValue ? "x" : ""
            switch memelib.getSelectMode() {
            case MEMEMode_Standard:
                if let d = dic["data"] as? AcademicStandardData {
                    stringBuffer += "\(isFreeMarkingString),\(packetCount),\(dateString),\(d.accX),\(d.accY),\(d.accZ),\(d.eogL1),\(d.eogR1),\(d.eogL2),\(d.eogR2),\(d.eogH1),\(d.eogH2),\(d.eogV1),\(d.eogV2)\n"
                }
            case MEMEMode_Full:
                if let d = dic["data"] as? AcademicFullData {
                    stringBuffer += "\(isFreeMarkingString),\(packetCount),\(dateString),\(d.accX),\(d.accY),\(d.accZ),\(d.gyroX),\(d.gyroY),\(d.gyroZ),\(d.eogL),\(d.eogR),\(d.eogH),\(d.eogV)\n"
                }
            default:
                if let d = dic["data"] as? AcademicQuaternionData {
                    stringBuffer += "\(isFreeMarkingString),\(packetCount),\(dateString),\(d.quaternionW),\(d.quaternionX),\(d.quaternionY),\(d.quaternionZ)\n"
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
        label_SuccessRate.stringValue = String(format: "%.2f%%", rate)
        box_SuccessRate.frame = CGRect(x: 0, y: 0, width: rate, height: box_SuccessRate.frame.size.height)
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
        label_Communication.stringValue = String(format: "%.2f%%", comm)
        dataCount200ms = 0
        box_Communication.frame = CGRect(x: 0, y: 0, width: comm, height: box_Communication.frame.size.height)
    }

    // MARK: - IBActions

    @IBAction func button_Setting_Tapped(_ sender: Any) {
        NSLog("button_Setting_Tapped")
        showSetting()
    }

    private func showSetting() {
        NSLog("showSetting")
        let storyboard = NSStoryboard(name: "Setting", bundle: nil)
        guard let windowController = storyboard.instantiateInitialController() as? NSWindowController,
              let settingVC = windowController.contentViewController as? SettingViewController else {
            return
        }
        settingVC.delegate = self
        windowController.showWindow(self)
    }

    @IBAction func button_StartScan_Tapped(_ sender: Any) {
        NSLog("Call : startScanningPeripherals")
        peripheralManager = CBPeripheralManager(delegate: self, queue: nil, options: [CBPeripheralManagerOptionShowPowerAlertKey: "YES"])
    }

    @IBAction func button_Connect_Tapped(_ sender: Any) {
        if !connectedFlag {
            NSLog("Call : connectPeripheral")
            button_Connect.isEnabled = false
            let name = combobox_MEME.stringValue
            memelib.connectPeripheral(deviceName: name)
        } else {
            NSLog("Call : disconnectPeripheral")
            memelib.disconnectPeripheral()
        }
    }

    @IBAction func button_StartMeasurement_Tapped(_ sender: Any) {
        let accelRange = UInt32(combobox_AccelRange.indexOfSelectedItem)
        let gyroRange = UInt32(combobox_GyroRange.indexOfSelectedItem)

        if !measurementFlag {
            startDate = Date()
            startCommunicationTimer()

            button_StartMeasurement.title = "Stop Measurement"

            button_StartScan.isHidden = true
            button_Connect.isHidden = true
            button_StartMeasurement.isHidden = false
            button_FreeMarking.isHidden = false

            button_Settings.isEnabled = false
            combobox_MEME.isEnabled = false
            combobox_SelectMode.isEnabled = false
            combobox_TransSpeed.isEnabled = false
            combobox_AccelRange.isEnabled = false
            combobox_GyroRange.isEnabled = false
            combobox_Chart1.isEnabled = false
            combobox_Chart2.isEnabled = false
            combobox_Chart3.isEnabled = false
            button_Chart_Apply.isEnabled = false

            memelib.setSelectMode(UInt32(combobox_SelectMode.indexOfSelectedItem + 1))
            memelib.setTransMode(UInt32(combobox_TransSpeed.indexOfSelectedItem + 1))
            mQuality = combobox_TransSpeed.indexOfSelectedItem + 1
            memelib.setAccelRange(accelRange)
            memelib.setGyroRange(gyroRange)

            if let socket = socket {
                socket.headerString = headerString()
                socket.writeHeader()
            }

            measurementFlag = true
            memelib.startDataReport()
        } else {
            button_StartMeasurement.title = "Start Measurement"

            memelib.stopDataReport()
            stopCommunicationTimer()

            DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) { [weak self] in
                guard let self = self else { return }
                self.button_StartScan.isHidden = true
                self.button_Connect.isHidden = false
                self.button_StartMeasurement.isHidden = false
                self.button_FreeMarking.isHidden = true

                self.button_Settings.isEnabled = true
                self.combobox_MEME.isEnabled = true
                self.combobox_SelectMode.isEnabled = true
                self.combobox_TransSpeed.isEnabled = true
                self.combobox_AccelRange.isEnabled = true
                self.combobox_GyroRange.isEnabled = true
                self.combobox_Chart1.isEnabled = true
                self.combobox_Chart2.isEnabled = true
                self.combobox_Chart3.isEnabled = true
                self.button_Chart_Apply.isEnabled = true

                self.measurementFlag = false

                self.flushCsv()

                if UserSetting.getShowSaveFileDialog() {
                    self.fileMove()
                } else {
                    self.csvManager.reset()
                }
                self.reset()
            }
        }
    }

    @IBAction func button_FreeMarking_Tapped(_ sender: Any) {
        NSLog("button_FreeMarking_Tapped")
        isFreeMarking = true
    }

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
        if #available(macOS 11.0, *) {
            if let csvType = UTType(filenameExtension: "csv") {
                savePanel.allowedContentTypes = [csvType]
            } else {
                savePanel.allowedFileTypes = ["csv"]
            }
        } else {
            savePanel.allowedFileTypes = ["csv"]
        }
        savePanel.nameFieldStringValue = saveFileName
        savePanel.level = .modalPanel

        guard let window = NSApp.mainWindow else { return }
        savePanel.beginSheetModal(for: window) { result in
            if result == .OK, let url = savePanel.url {
                NSLog("OK")
                do {
                    try FileManager.default.copyItem(at: URL(fileURLWithPath: sourceFilePath), to: url)
                    do {
                        try FileManager.default.removeItem(at: URL(fileURLWithPath: sourceFilePath))
                        NSLog("成功")
                    } catch {
                        NSLog("削除失敗:%@", error.localizedDescription)
                    }
                } catch {
                    NSLog("コピー失敗:%@", error.localizedDescription)
                }
            } else {
                NSLog("NO")
            }
        }
    }

    private func socketStart() {
        NSLog("socketStart")
        if UserSetting.getExtermalOutputSocket() {
            let s = TCPSocket()
            s.delegate = self
            s.headerString = headerString()
            let status = s.start()
            label_SocketStatus.stringValue = "Status : \(status)"
            socket = s
        } else {
            NSLog("ソケット通信しない")
        }
    }

    private func socketStop() {
        NSLog("stop")
        socket?.stop()
        socket = nil
        label_SocketStatus.stringValue = "Status : "
    }

    // MARK: - Chart helpers

    private func setChartView(_ scrollView: NSScrollView, xMaxValue: Float, xMinValue: Float, yMaxValue: Float, yMinValue: Float) -> ChartView {
        let chartView = ChartView(frame: CGRect(x: 0, y: 0, width: scrollView.frame.size.width, height: scrollView.frame.size.height))
        chartView.xMaxValue = xMaxValue
        chartView.xMinValue = xMinValue
        chartView.xLongScale = 25
        chartView.xShortScale = 5
        chartView.yMaxValue = yMaxValue
        chartView.yMinValue = yMinValue
        scrollView.addSubview(chartView)
        return chartView
    }

    private func appendStandardEogValues(_ datas: [AcademicData], chartView: ChartView,
                                         isLeft: Bool, isRight: Bool, isDelftH: Bool, isDelftV: Bool) {
        var left: [NSNumber] = []
        var right: [NSNumber] = []
        var deltaH: [NSNumber] = []
        var deltaV: [NSNumber] = []
        let limit = Int(chartView.xMaxValue - Float(chartView.xLongScale) / 2)
        let startIndex = max(0, datas.count - limit)
        let range = datas.count >= limit ? startIndex..<datas.count : 0..<chartDatas.count
        let source: [AcademicData] = datas.count >= limit ? datas : chartDatas
        for i in range {
            guard let data = source[i] as? AcademicStandardData else { continue }
            if isLeft { left.append(NSNumber(value: Double(data.eogL1))) }
            if isRight { right.append(NSNumber(value: Double(data.eogR1))) }
            if isDelftH { deltaH.append(NSNumber(value: Double(data.eogH1))) }
            if isDelftV { deltaV.append(NSNumber(value: Double(data.eogV1))) }
        }
        chartView.datas.removeAll()
        if isLeft { chartView.setChartData(left, lineColor: .yellow) }
        if isRight { chartView.setChartData(right, lineColor: .green) }
        if isDelftH { chartView.setChartData(deltaH, lineColor: .red) }
        if isDelftV { chartView.setChartData(deltaV, lineColor: .blue) }
        updateXPosition(chartView: chartView, datasCount: datas.count)
        chartView.needsDisplay = true
    }

    private func appendFullEogValues(_ datas: [AcademicData], chartView: ChartView,
                                     isLeft: Bool, isRight: Bool, isDelftH: Bool, isDelftV: Bool) {
        var left: [NSNumber] = []
        var right: [NSNumber] = []
        var deltaH: [NSNumber] = []
        var deltaV: [NSNumber] = []
        let limit = Int(chartView.xMaxValue - Float(chartView.xLongScale) / 2)
        let startIndex = max(0, datas.count - limit)
        let range = datas.count >= limit ? startIndex..<datas.count : 0..<chartDatas.count
        let source: [AcademicData] = datas.count >= limit ? datas : chartDatas
        for i in range {
            guard let data = source[i] as? AcademicFullData else { continue }
            if isLeft { left.append(NSNumber(value: Double(data.eogL))) }
            if isRight { right.append(NSNumber(value: Double(data.eogR))) }
            if isDelftH { deltaH.append(NSNumber(value: Double(data.eogH))) }
            if isDelftV { deltaV.append(NSNumber(value: Double(data.eogV))) }
        }
        chartView.datas.removeAll()
        if isLeft { chartView.setChartData(left, lineColor: .yellow) }
        if isRight { chartView.setChartData(right, lineColor: .green) }
        if isDelftH { chartView.setChartData(deltaH, lineColor: .red) }
        if isDelftV { chartView.setChartData(deltaV, lineColor: .blue) }
        updateXPosition(chartView: chartView, datasCount: datas.count)
        chartView.needsDisplay = true
    }

    private func appendFullGyroValues(_ datas: [AcademicData], chartView: ChartView,
                                      isGyroX: Bool, isGyroY: Bool, isGyroZ: Bool) {
        var gx: [NSNumber] = []
        var gy: [NSNumber] = []
        var gz: [NSNumber] = []
        let limit = Int(chartView.xMaxValue - Float(chartView.xLongScale) / 2)
        let startIndex = max(0, datas.count - limit)
        let range = datas.count >= limit ? startIndex..<datas.count : 0..<chartDatas.count
        let source: [AcademicData] = datas.count >= limit ? datas : chartDatas
        for i in range {
            guard let data = source[i] as? AcademicFullData else { continue }
            if isGyroX { gx.append(NSNumber(value: Double(data.gyroX))) }
            if isGyroY { gy.append(NSNumber(value: Double(data.gyroY))) }
            if isGyroZ { gz.append(NSNumber(value: Double(data.gyroZ))) }
        }
        chartView.datas.removeAll()
        if isGyroX { chartView.setChartData(gx, lineColor: .red) }
        if isGyroY { chartView.setChartData(gy, lineColor: .green) }
        if isGyroZ { chartView.setChartData(gz, lineColor: .blue) }
        updateXPosition(chartView: chartView, datasCount: datas.count)
        chartView.needsDisplay = true
    }

    private func appendStandardAccelValues(_ datas: [AcademicData], chartView: ChartView,
                                           isAccX: Bool, isAccY: Bool, isAccZ: Bool) {
        var ax: [NSNumber] = []
        var ay: [NSNumber] = []
        var az: [NSNumber] = []
        let limit = Int(chartView.xMaxValue - Float(chartView.xLongScale) / 2)
        let startIndex = max(0, datas.count - limit)
        let range = datas.count >= limit ? startIndex..<datas.count : 0..<chartDatas.count
        let source: [AcademicData] = datas.count >= limit ? datas : chartDatas
        for i in range {
            guard let data = source[i] as? AcademicStandardData else { continue }
            if isAccX { ax.append(NSNumber(value: Double(data.accX) + UserSetting.getXAxis())) }
            if isAccY { ay.append(NSNumber(value: Double(data.accY) + UserSetting.getYAxis())) }
            if isAccZ { az.append(NSNumber(value: Double(data.accZ) + UserSetting.getZAxis())) }
        }
        chartView.datas.removeAll()
        if isAccX { chartView.setChartData(ax, lineColor: .red) }
        if isAccY { chartView.setChartData(ay, lineColor: .green) }
        if isAccZ { chartView.setChartData(az, lineColor: .blue) }
        updateXPosition(chartView: chartView, datasCount: datas.count)
        chartView.needsDisplay = true
    }

    private func appendFullAccelValues(_ datas: [AcademicData], chartView: ChartView,
                                       isAccX: Bool, isAccY: Bool, isAccZ: Bool) {
        var ax: [NSNumber] = []
        var ay: [NSNumber] = []
        var az: [NSNumber] = []
        let limit = Int(chartView.xMaxValue - Float(chartView.xLongScale) / 2)
        let startIndex = max(0, datas.count - limit)
        let range = datas.count >= limit ? startIndex..<datas.count : 0..<chartDatas.count
        let source: [AcademicData] = datas.count >= limit ? datas : chartDatas
        for i in range {
            guard let data = source[i] as? AcademicFullData else { continue }
            if isAccX { ax.append(NSNumber(value: Double(data.accX) + UserSetting.getXAxis())) }
            if isAccY { ay.append(NSNumber(value: Double(data.accY) + UserSetting.getYAxis())) }
            if isAccZ { az.append(NSNumber(value: Double(data.accZ) + UserSetting.getZAxis())) }
        }
        chartView.datas.removeAll()
        if isAccX { chartView.setChartData(ax, lineColor: .red) }
        if isAccY { chartView.setChartData(ay, lineColor: .green) }
        if isAccZ { chartView.setChartData(az, lineColor: .blue) }
        updateXPosition(chartView: chartView, datasCount: datas.count)
        chartView.needsDisplay = true
    }

    private func updateXPosition(chartView: ChartView, datasCount: Int) {
        if Float(datasCount) > chartView.xMaxValue - Float(chartView.xLongScale) {
            let pos = datasCount % chartView.xLongScale
            chartView.xInitialPosition = Float(pos) * -1
            chartView.xTextFieldValue = (datasCount / chartView.xLongScale) - (Int(chartView.xMaxValue) / chartView.xLongScale) + 1
        }
    }

    // MARK: - Battery / success update

    private func batteryLevelAndSuccessRate(battLv: UInt16) {
        dataCount += 1
        updateSuccessRate()
        dataCount200ms += 1
        showBatteryLevel(Int(battLv))
    }

    // MARK: - Chart_Apply

    @IBAction func chart_Apply(_ sender: NSButton) {
        // Chart1
        switch combobox_Chart1.indexOfSelectedItem {
        case 0:
            textField_Chart1.stringValue = "Chart1：Electrooculography"
            setChartY(1200, textFieldArray: chart1Y)
            box_Chart1_Electrooculography.isHidden = false
            box_Chart1_Gyroscope.isHidden = true
            box_Chart1_Accelerometer.isHidden = true
        case 1:
            textField_Chart1.stringValue = "Chart1：Gyroscope"
            setChartY(36000, textFieldArray: chart1Y)
            box_Chart1_Electrooculography.isHidden = true
            box_Chart1_Gyroscope.isHidden = false
            box_Chart1_Accelerometer.isHidden = true
        case 2:
            textField_Chart1.stringValue = "Chart1：Accelerometer"
            setChartY(36000, textFieldArray: chart1Y)
            box_Chart1_Electrooculography.isHidden = true
            box_Chart1_Gyroscope.isHidden = true
            box_Chart1_Accelerometer.isHidden = false
        default: break
        }
        // Chart2
        switch combobox_Chart2.indexOfSelectedItem {
        case 0:
            textField_Chart2.stringValue = "Chart2：Electrooculography"
            setChartY(1200, textFieldArray: chart2Y)
            box_Chart2_Electrooculography.isHidden = false
            box_Chart2_Gyroscope.isHidden = true
            box_Chart2_Accelerometer.isHidden = true
        case 1:
            textField_Chart2.stringValue = "Chart2：Gyroscope"
            setChartY(36000, textFieldArray: chart2Y)
            box_Chart2_Electrooculography.isHidden = true
            box_Chart2_Gyroscope.isHidden = false
            box_Chart2_Accelerometer.isHidden = true
        case 2:
            textField_Chart2.stringValue = "Chart2：Accelerometer"
            setChartY(36000, textFieldArray: chart2Y)
            box_Chart2_Electrooculography.isHidden = true
            box_Chart2_Gyroscope.isHidden = true
            box_Chart2_Accelerometer.isHidden = false
        default: break
        }
        // Chart3
        switch combobox_Chart3.indexOfSelectedItem {
        case 0:
            textField_Chart3.stringValue = "Chart3：Electrooculography"
            setChartY(1200, textFieldArray: chart3Y)
            box_Chart3_Electrooculography.isHidden = false
            box_Chart3_Gyroscope.isHidden = true
            box_Chart3_Accelerometer.isHidden = true
        case 1:
            textField_Chart3.stringValue = "Chart3：Gyroscope"
            setChartY(36000, textFieldArray: chart3Y)
            box_Chart3_Electrooculography.isHidden = true
            box_Chart3_Gyroscope.isHidden = false
            box_Chart3_Accelerometer.isHidden = true
        case 2:
            textField_Chart3.stringValue = "Chart3：Accelerometer"
            setChartY(36000, textFieldArray: chart3Y)
            box_Chart3_Electrooculography.isHidden = true
            box_Chart3_Gyroscope.isHidden = true
            box_Chart3_Accelerometer.isHidden = false
        default: break
        }
    }

    private func setChartY(_ value: Int, textFieldArray: [NSTextField]) {
        let scale = value * 2 / 6
        for i in 0..<min(7, textFieldArray.count) {
            textFieldArray[i].stringValue = "\(value - scale * i)"
        }
    }
}

// MARK: - CBPeripheralManagerDelegate
extension ViewController: @preconcurrency CBPeripheralManagerDelegate {
    func peripheralManagerDidUpdateState(_ peripheral: CBPeripheralManager) {
        if peripheral.state == .poweredOn {
            NSLog("bluetooth ON")
            button_StartScan.isEnabled = false
            combobox_MEME.removeAllItems()
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

// MARK: - MEMELibDelegate
extension ViewController: MEMELibDelegate {

    func memePeripheralFoundDelegate(result: UInt32, deviceName: String?, uuid: String?) {
        if result == MEMELIB_OK {
            NSLog("memePeripheralFoundDelegate %d %@ %@", result, deviceName ?? "", uuid ?? "")
            if let name = deviceName {
                combobox_MEME.addItem(withObjectValue: name)
                combobox_MEME.selectItem(at: combobox_MEME.numberOfItems - 1)
            }
            button_StartScan.isHidden = false
            button_Connect.isHidden = false
            button_StartMeasurement.isHidden = true
            button_FreeMarking.isHidden = true
        } else {
            NSLog("memePeripheralFoundDelegate %d", result)
            NSLog("Call : stopScanningPeripherals")
            button_StartScan.isEnabled = true
            memelib.stopScanningPeripherals()
        }
    }

    func memePeripheralConnectedDelegate(result: UInt32) {
        NSLog("memePeripheralConnectedDelegate : %d", result)
        connectedFlag = true
        button_Connect.title = "Disconnect"
        label_StateConnect.stringValue = "State : Connected"

        button_StartScan.isEnabled = true
        button_StartScan.isHidden = true
        button_Connect.isEnabled = true
        button_Connect.isHidden = false
        button_StartMeasurement.isHidden = false

        showMemeVersion()
    }

    func memePeripheralDisconnectedDelegate(result: UInt32) {
        NSLog("memePeripheralDisconnectedDelegate : %d", result)
        connectedFlag = false
        button_Connect.title = "Connect"
        label_StateConnect.stringValue = "State : Disconnected"

        button_StartScan.isHidden = false
        button_Connect.isHidden = true
        button_StartMeasurement.isHidden = true

        combobox_MEME.isEnabled = true
    }

    func memeAcademicStandardDataReceivedDelegate(data: AcademicStandardData) {
        csvDatas.append(dataToDictionary(data))
        saveCsv()
        if socket?.isConnected() == true, let last = csvDatas.last {
            socketDatas.append(last)
            writeSocket()
        }
        batteryLevelAndSuccessRate(battLv: data.battLv)

        let interval = memelib.getTransMode() == MEMEQuality_High ? 4 : 2
        if mTotalCount % interval == 0 {
            chartDatas.append(data)

            // Chart1
            if combobox_Chart1.indexOfSelectedItem == 0 {
                chartView1.yMaxValue = 1200
                chartView1.yMinValue = -1200
                appendStandardEogValues(chartDatas, chartView: chartView1,
                                        isLeft: button_Chart1_Electrooculography_Left.state == .on,
                                        isRight: button_Chart1_Electrooculography_Right.state == .on,
                                        isDelftH: button_Chart1_Electrooculography_DeltaH.state == .on,
                                        isDelftV: button_Chart1_Electrooculography_DeltaV.state == .on)
            } else if combobox_Chart1.indexOfSelectedItem == 2 {
                chartView1.yMaxValue = 36000
                chartView1.yMinValue = -36000
                appendStandardAccelValues(chartDatas, chartView: chartView1,
                                          isAccX: button_Chart1_Accelerometer_X_Axis.state == .on,
                                          isAccY: button_Chart1_Accelerometer_Y_Axis.state == .on,
                                          isAccZ: button_Chart1_Accelerometer_Z_Axis.state == .on)
            }

            // Chart2
            if combobox_Chart2.indexOfSelectedItem == 0 {
                chartView2.yMaxValue = 1200
                chartView2.yMinValue = -1200
                appendStandardEogValues(chartDatas, chartView: chartView2,
                                        isLeft: button_Chart2_Electrooculography_Left.state == .on,
                                        isRight: button_Chart2_Electrooculography_Right.state == .on,
                                        isDelftH: button_Chart2_Electrooculography_DeltaH.state == .on,
                                        isDelftV: button_Chart2_Electrooculography_DeltaV.state == .on)
            } else if combobox_Chart2.indexOfSelectedItem == 2 {
                chartView2.yMaxValue = 36000
                chartView2.yMinValue = -36000
                appendStandardAccelValues(chartDatas, chartView: chartView2,
                                          isAccX: button_Chart2_Accelerometer_X_Axis.state == .on,
                                          isAccY: button_Chart2_Accelerometer_Y_Axis.state == .on,
                                          isAccZ: button_Chart2_Accelerometer_Z_Axis.state == .on)
            }

            // Chart3
            if combobox_Chart3.indexOfSelectedItem == 0 {
                chartView3.yMaxValue = 1200
                chartView3.yMinValue = -1200
                appendStandardEogValues(chartDatas, chartView: chartView3,
                                        isLeft: button_Chart3_Electrooculography_Left.state == .on,
                                        isRight: button_Chart3_Electrooculography_Right.state == .on,
                                        isDelftH: button_Chart3_Electrooculography_DeltaH.state == .on,
                                        isDelftV: button_Chart3_Electrooculography_DeltaV.state == .on)
            } else if combobox_Chart3.indexOfSelectedItem == 2 {
                chartView3.yMaxValue = 36000
                chartView3.yMinValue = -36000
                appendStandardAccelValues(chartDatas, chartView: chartView3,
                                          isAccX: button_Chart3_Accelerometer_X_Axis.state == .on,
                                          isAccY: button_Chart3_Accelerometer_Y_Axis.state == .on,
                                          isAccZ: button_Chart3_Accelerometer_Z_Axis.state == .on)
            }
        }
    }

    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData) {
        csvDatas.append(dataToDictionary(data))
        saveCsv()
        if socket?.isConnected() == true, let last = csvDatas.last {
            socketDatas.append(last)
            writeSocket()
        }
        batteryLevelAndSuccessRate(battLv: data.battLv)

        let interval = memelib.getTransMode() == MEMEQuality_High ? 4 : 2
        if mTotalCount % interval == 0 {
            chartDatas.append(data)

            // Chart1
            switch combobox_Chart1.indexOfSelectedItem {
            case 0:
                chartView1.yMaxValue = 1200
                chartView1.yMinValue = -1200
                appendFullEogValues(chartDatas, chartView: chartView1,
                                    isLeft: button_Chart1_Electrooculography_Left.state == .on,
                                    isRight: button_Chart1_Electrooculography_Right.state == .on,
                                    isDelftH: button_Chart1_Electrooculography_DeltaH.state == .on,
                                    isDelftV: button_Chart1_Electrooculography_DeltaV.state == .on)
            case 1:
                chartView1.yMaxValue = 36000
                chartView1.yMinValue = -36000
                appendFullGyroValues(chartDatas, chartView: chartView1,
                                     isGyroX: button_Chart1_Gyroscope_X_Axis.state == .on,
                                     isGyroY: button_Chart1_Gyroscope_Y_Axis.state == .on,
                                     isGyroZ: button_Chart1_Gyroscope_Z_Axis.state == .on)
            case 2:
                chartView1.yMaxValue = 36000
                chartView1.yMinValue = -36000
                appendFullAccelValues(chartDatas, chartView: chartView1,
                                      isAccX: button_Chart1_Accelerometer_X_Axis.state == .on,
                                      isAccY: button_Chart1_Accelerometer_Y_Axis.state == .on,
                                      isAccZ: button_Chart1_Accelerometer_Z_Axis.state == .on)
            default: break
            }

            // Chart2
            switch combobox_Chart2.indexOfSelectedItem {
            case 0:
                chartView2.yMaxValue = 1200
                chartView2.yMinValue = -1200
                appendFullEogValues(chartDatas, chartView: chartView2,
                                    isLeft: button_Chart2_Electrooculography_Left.state == .on,
                                    isRight: button_Chart2_Electrooculography_Right.state == .on,
                                    isDelftH: button_Chart2_Electrooculography_DeltaH.state == .on,
                                    isDelftV: button_Chart2_Electrooculography_DeltaV.state == .on)
            case 1:
                chartView2.yMaxValue = 36000
                chartView2.yMinValue = -36000
                appendFullGyroValues(chartDatas, chartView: chartView2,
                                     isGyroX: button_Chart2_Gyroscope_X_Axis.state == .on,
                                     isGyroY: button_Chart2_Gyroscope_Y_Axis.state == .on,
                                     isGyroZ: button_Chart2_Gyroscope_Z_Axis.state == .on)
            case 2:
                chartView2.yMaxValue = 36000
                chartView2.yMinValue = -36000
                appendFullAccelValues(chartDatas, chartView: chartView2,
                                      isAccX: button_Chart2_Accelerometer_X_Axis.state == .on,
                                      isAccY: button_Chart2_Accelerometer_Y_Axis.state == .on,
                                      isAccZ: button_Chart2_Accelerometer_Z_Axis.state == .on)
            default: break
            }

            // Chart3
            switch combobox_Chart3.indexOfSelectedItem {
            case 0:
                chartView3.yMaxValue = 1200
                chartView3.yMinValue = -1200
                appendFullEogValues(chartDatas, chartView: chartView3,
                                    isLeft: button_Chart3_Electrooculography_Left.state == .on,
                                    isRight: button_Chart3_Electrooculography_Right.state == .on,
                                    isDelftH: button_Chart3_Electrooculography_DeltaH.state == .on,
                                    isDelftV: button_Chart3_Electrooculography_DeltaV.state == .on)
            case 1:
                chartView3.yMaxValue = 36000
                chartView3.yMinValue = -36000
                appendFullGyroValues(chartDatas, chartView: chartView3,
                                     isGyroX: button_Chart3_Gyroscope_X_Axis.state == .on,
                                     isGyroY: button_Chart3_Gyroscope_Y_Axis.state == .on,
                                     isGyroZ: button_Chart3_Gyroscope_Z_Axis.state == .on)
            case 2:
                chartView3.yMaxValue = 36000
                chartView3.yMinValue = -36000
                appendFullAccelValues(chartDatas, chartView: chartView3,
                                      isAccX: button_Chart3_Accelerometer_X_Axis.state == .on,
                                      isAccY: button_Chart3_Accelerometer_Y_Axis.state == .on,
                                      isAccZ: button_Chart3_Accelerometer_Z_Axis.state == .on)
            default: break
            }
        }
    }

    func memeAcademicQuaternionDataReceivedDelegate(data: AcademicQuaternionData) {
        csvDatas.append(dataToDictionary(data))
        saveCsv()
        if socket?.isConnected() == true, let last = csvDatas.last {
            socketDatas.append(last)
            writeSocket()
        }
        batteryLevelAndSuccessRate(battLv: data.battLv)
    }
}

// MARK: - TcpSocketDelegate
extension ViewController: TcpSocketDelegate {
    func didAccept() {
        NSLog("didAccept")
        label_SocketStatus.stringValue = "Status : Accept"
    }

    func socketDidDisconnect(error: Error?) {
        NSLog("didDisconnect")
        label_SocketStatus.stringValue = "Status : "
        socketStart()
    }
}

// MARK: - SettingViewControllerDelegate
extension ViewController: SettingViewControllerDelegate {
    func didApply(_ settingViewController: SettingViewController) {
        NSLog("didApply")
        socketStop()
        socketStart()
        showLocalPort()
    }
}
