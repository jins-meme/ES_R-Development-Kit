//
//  ViewController.swift
//  MEME_Academic
//
//  Created by JINS ASSIST開発実機検証  on 2026/06/02.
//  Copyright © 2026 jins-jp. All rights reserved.
//

import Cocoa

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

    // MARK: - Private Properties
    private var memelib: (any MEMELibInterface)!
    private var connectedFlag = false
    private var measurementFlag = false

    // MARK: - View Life Cycle
    override func viewDidLoad() {
        super.viewDidLoad()

        memelib = MEMELibFactory.make()
        memelib.delegate = self

        connectedFlag = false
        measurementFlag = false
        
        button_StartScan.isHidden = false
        button_Connect.isHidden = true
        button_StartMeasurement.isHidden = true
        
        combobox_MEME.removeAllItems()

        combobox_SelectMode.addItem(withObjectValue: "Full")
        combobox_SelectMode.selectItem(at: 0)
        
        combobox_TransSpeed.addItem(withObjectValue: "100Hz")
        combobox_TransSpeed.selectItem(at: 0)
        
        combobox_AccelRange.addItems(withObjectValues: ["±2G", "±4G", "±8G", "±16G"])
        combobox_AccelRange.selectItem(at: 0)

        combobox_GyroRange.addItems(withObjectValues: ["±250dps", "±500dps", "±1000dps", "±2000dps"])
        combobox_GyroRange.selectItem(at: 0)
    }

    override var representedObject: Any? {
        didSet {
            // 必要に応じてビューを更新
        }
    }

    // MARK: - IBActions
    @IBAction func button_StartScan_Tapped(_ sender: Any) {
        print("Call : startScanningPeripherals")
        combobox_MEME.removeAllItems()
        _ = memelib.startScanningPeripherals()
    }

    @IBAction func button_Connect_Tapped(_ sender: Any) {
        if !connectedFlag {
            print("Call : connectPeripheral")
            let name = combobox_MEME.stringValue
            _ = memelib.connectPeripheral(deviceName: name)
        } else {
            print("Call : disconnectPeripheral")
            _ = memelib.disconnectPeripheral()
        }
    }

    @IBAction func button_StartMeasurement_Tapped(_ sender: Any) {
        let accelrange = combobox_AccelRange.indexOfSelectedItem
        let gyrorange = combobox_GyroRange.indexOfSelectedItem
        
        if !measurementFlag {
            button_StartMeasurement.title = "Stop Measurement"

            button_StartScan.isHidden = true
            button_Connect.isHidden = true
            button_StartMeasurement.isHidden = false

            _ = memelib.setSelectMode(MEMEMode_Full)
            _ = memelib.setTransMode(MEMEQuality_High)
            _ = memelib.setAccelRange(UInt32(accelrange))
            _ = memelib.setGyroRange(UInt32(gyrorange))
            
            measurementFlag = true
            _ = memelib.startDataReport()
        } else {
            button_StartMeasurement.title = "Start Measurement"
            
            button_StartScan.isHidden = true
            button_Connect.isHidden = false
            button_StartMeasurement.isHidden = false

            measurementFlag = false
            _ = memelib.stopDataReport()
        }
    }
}

// =============================================================================
// MARK: - MEMELibAcademicDelegate
// =============================================================================
extension ViewController: MEMELibAcademicDelegate {
    
    func memePeripheralFoundDelegate(result: UInt32, deviceName: String?, uuid: String?) {
        if result == MEMELIB_OK {
            print("memePeripheralFoundDelegate \(result) \(deviceName ?? "") \(uuid ?? "")")

            if let name = deviceName {
                combobox_MEME.addItem(withObjectValue: name)
                combobox_MEME.selectItem(at: combobox_MEME.numberOfItems - 1)
            }

            button_StartScan.isHidden = false
            button_Connect.isHidden = false
            button_StartMeasurement.isHidden = true
        } else {
            print("memePeripheralFoundDelegate \(result)")
            print("Call : stopScanningPeripherals")
            _ = memelib.stopScanningPeripherals()
        }
    }

    func memePeripheralConnectedDelegate(result: UInt32) {
        print("memePeripheralConnectedDelegate : \(result)")
        connectedFlag = true
        button_Connect.title = "Disconnect"
        label_StateConnect.stringValue = "State : Connected"

        button_StartScan.isHidden = true
        button_Connect.isHidden = false
        button_StartMeasurement.isHidden = false
    }

    func memePeripheralDisconnectedDelegate(result: UInt32) {
        print("memePeripheralDisconnectedDelegate : \(result)")
        connectedFlag = false
        button_Connect.title = "Connect"
        label_StateConnect.stringValue = "State : Disconnected"

        button_StartScan.isHidden = false
        button_Connect.isHidden = true
        button_StartMeasurement.isHidden = true
    }

    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData) {
        label_DataCnt.stringValue = "\(data.cnt)"
        label_DataAccX.stringValue = "\(data.accX)"
        label_DataAccY.stringValue = "\(data.accY)"
        label_DataAccZ.stringValue = "\(data.accZ)"
        label_DataGyroX.stringValue = "\(data.gyroX)"
        label_DataGyroY.stringValue = "\(data.gyroY)"
        label_DataGyroZ.stringValue = "\(data.gyroZ)"
        label_DataEogL.stringValue = "\(data.eogL)"
        label_DataEogR.stringValue = "\(data.eogR)"
        label_DataEogH.stringValue = "\(data.eogH)"
        label_DataEogV.stringValue = "\(data.eogV)"
        label_DataBattLv.stringValue = "\(data.battLv)"
    }
}
