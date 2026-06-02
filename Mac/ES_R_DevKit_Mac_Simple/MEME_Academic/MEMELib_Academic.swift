//
//  MEMELib_Academic.swift
//  MEME_Academic
//
//  Created by JINS ASSIST開発実機検証  on 2026/06/02.
//  Copyright © 2026 jins-jp. All rights reserved.
//

import Foundation
import CoreBluetooth

// =============================================================================
// MARK: - Global Constants & Utilities
// =============================================================================
let MEMELIB_OK: UInt32 = 0
let MEMELIB_NG: UInt32 = 1
let MEMELIB_TIMEOUT: UInt32 = 2

let SERVICES_UUID = "D6F25BD1-5B54-4360-96D8-7AA62E04C7EF"
let CHARACTERISTICS_READ_UUID = "D6F25BD4-5B54-4360-96D8-7AA62E04C7EF"
let CHARACTERISTICS_WRITE_UUID = "D6F25BD2-5B54-4360-96D8-7AA62E04C7EF"

let MEMEMode_Full: UInt32 = 2
let MEMEQuality_High: UInt32 = 1

let MEMEAccelRange_2G: UInt32 = 0
let MEMEAccelRange_4G: UInt32 = 1
let MEMEAccelRange_8G: UInt32 = 2
let MEMEAccelRange_16G: UInt32 = 3

let MEMEGyroRange_250dps: UInt32 = 0
let MEMEGyroRange_500dps: UInt32 = 1
let MEMEGyroRange_1000dps: UInt32 = 2
let MEMEGyroRange_2000dps: UInt32 = 3

let STATUS_IDLE: Int = 0x01
let STATUS_SCAN_REQ_EXEC: Int = 0x02
let STATUS_CONNECTION_REQ_EXEC: Int = 0x03
let STATUS_GET_STATUS_REQ_EXEC: Int = 0x04
let STATUS_GET_VERSION_REQ_EXEC: Int = 0x05
let STATUS_DISCONNECTION_REQ_EXEC: Int = 0x06
let STATUS_SET_TIME_REQ_EXEC: Int = 0x07
let STATUS_REC_START_REQ_EXEC: Int = 0x08
let STATUS_REC_STOP_REQ_EXEC: Int = 0x09

let CHECK_TIMEOUT_TIME: TimeInterval = 20
let TIME_SYNC_COUNT: Int = 2
let PACKET_LENGTH: Int = 20

func libLog(_ message: String, file: String = #file, line: Int = #line, function: String = #function) {
    #if DEBUG
    let fileName = (file as NSString).lastPathComponent
    print("\(fileName) \(line) \(function) \(message)")
    #endif
}

// =============================================================================
// MARK: - Delegate Protocol
// =============================================================================
@MainActor // UIスレッドでの実行を保証し、ViewController側のデータレースエラーを防ぐ
protocol MEMELibAcademicDelegate: AnyObject {
    func memePeripheralFoundDelegate(result: UInt32, deviceName: String?, uuid: String?)
    func memePeripheralConnectedDelegate(result: UInt32)
    func memePeripheralDisconnectedDelegate(result: UInt32)
    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData)
}

// =============================================================================
// MARK: - Main Class
// =============================================================================
@MainActor
class MEMELib_Academic: NSObject {
    
    var centralManager: CBCentralManager!
    var peripheral: CBPeripheral?
    var peripherals: [CBPeripheral] = []
    var peripheralsLocalName: [String] = []
    var inputCharacteristic: CBCharacteristic?
    var outputCharacteristic: CBCharacteristic?
    var recvData: [Data] = []
    var memeVersion = Version()
    var sdkVersion = Version()
    
    weak var delegate: MEMELibAcademicDelegate?
    
    private var dataCount: UInt32 = 0
    private var checkTimer: Timer?
    private var calibTimer: Timer?
    private var libraryFlag = false
    private var scanFlag = false
    private var connectedFlag = false
    private var measureFlag = false
    private var serviceFlag = false
    private var analysisFlag = false
    
    private var libStatus: Int = STATUS_IDLE
    private var selectMode: UInt32 = MEMEMode_Full
    private var transMode: UInt32 = MEMEQuality_High
    private var accelRange: UInt32 = MEMEAccelRange_2G
    private var gyroRange: UInt32 = MEMEGyroRange_250dps
    
    override init() {
        super.init()
        // queueにnilを指定することで、CoreBluetoothのコールバックは自動的にメインスレッドで呼ばれます
        self.centralManager = CBCentralManager(delegate: self, queue: nil)
    }
    
    // MARK: - Public Methods
    
    func startScanningPeripherals() -> UInt32 {
        if libraryFlag && !scanFlag && !connectedFlag {
            deviceScanStart()
            return MEMELIB_OK
        } else {
            return MEMELIB_NG
        }
    }
    
    func stopScanningPeripherals() -> UInt32 {
        if libraryFlag && scanFlag && !connectedFlag {
            deviceScanStop()
            return MEMELIB_OK
        } else {
            return MEMELIB_NG
        }
    }
    
    func connectPeripheral(deviceName: String) -> UInt32 {
        if libraryFlag && !connectedFlag {
            deviceConnectionStart(deviceName: deviceName)
            return MEMELIB_OK
        } else {
            return MEMELIB_NG
        }
    }
    
    func disconnectPeripheral() -> UInt32 {
        if libraryFlag && connectedFlag {
            deviceDisconnectionStart()
            return MEMELIB_OK
        } else {
            return MEMELIB_NG
        }
    }
    
    func getSelectMode() -> UInt32 { return selectMode }
    
    func setSelectMode(mode: UInt32) -> UInt32 {
        selectMode = mode
        memeAdnSetMode()
        return MEMELIB_OK
    }
    
    func getTransMode() -> UInt32 { return transMode }
    
    func setTransMode(mode: UInt32) -> UInt32 {
        transMode = mode
        memeAdnSetMode()
        return MEMELIB_OK
    }
    
    func getAccelRange() -> UInt32 { return accelRange }
    
    func setAccelRange(range: UInt32) -> UInt32 {
        accelRange = range
        memeAdnSet6AxisParams()
        return MEMELIB_OK
    }
    
    func getGyroRange() -> UInt32 { return gyroRange }
    
    func setGyroRange(range: UInt32) -> UInt32 {
        gyroRange = range
        memeAdnSet6AxisParams()
        return MEMELIB_OK
    }
    
    func startDataReport() -> UInt32 {
        if libraryFlag && connectedFlag && !measureFlag {
            measureFlag = true
            memeAdnGetData(state: 0x01)
            return MEMELIB_OK
        } else {
            return MEMELIB_NG
        }
    }
    
    func stopDataReport() -> UInt32 {
        if libraryFlag && connectedFlag && measureFlag {
            measureFlag = false
            memeAdnGetData(state: 0x00)
            return MEMELIB_OK
        } else {
            return MEMELIB_NG
        }
    }
    
    // MARK: - Private Methods
    
    private func dataSend(buff: inout [UInt8]) {
        DecEnc.encode(&buff)
        let sendData = Data(bytes: &buff, count: PACKET_LENGTH)
        if let peripheral = self.peripheral, let outputChar = self.outputCharacteristic {
            peripheral.writeValue(sendData, for: outputChar, type: .withResponse)
        }
    }
    
    private func deviceScanStart() {
        self.peripheral = nil
        self.peripherals.removeAll()
        self.peripheralsLocalName.removeAll()
        
        let services = [CBUUID(string: SERVICES_UUID)]
        let options: [String: Any] = [CBCentralManagerScanOptionAllowDuplicatesKey: false]
        centralManager.scanForPeripherals(withServices: services, options: options)
        
        libStatus = STATUS_SCAN_REQ_EXEC
        scanFlag = true
        checkTimerStart()
    }
    
    private func deviceConnectionStart(deviceName: String) {
        var flg = false
        if scanFlag {
            deviceScanStop()
        }
        for p in peripherals {
            if p.name == deviceName {
                flg = true
                self.peripheral = p
                break
            }
        }
        if flg, let peripheral = self.peripheral {
            centralManager.connect(peripheral, options: nil)
            libStatus = STATUS_CONNECTION_REQ_EXEC
            checkTimerStart()
        }
    }
    
    private func deviceDisconnectionStart() {
        if let peripheral = self.peripheral, let inputChar = self.inputCharacteristic {
            peripheral.setNotifyValue(false, for: inputChar)
            centralManager.cancelPeripheralConnection(peripheral)
        }
        libStatus = STATUS_DISCONNECTION_REQ_EXEC
        checkTimerStart()
    }
    
    private func deviceScanStop() {
        centralManager.stopScan()
        checkTimerStop()
        scanFlag = false
    }
    
    private func memeAdnGetDevInfo() {
        var buff = [UInt8](repeating: 0, count: PACKET_LENGTH)
        buff[0] = UInt8(PACKET_LENGTH)
        buff[1] = 0xA1
        dataSend(buff: &buff)
    }
    
    private func memeAdnGetMode() {
        var buff = [UInt8](repeating: 0, count: PACKET_LENGTH)
        buff[0] = UInt8(PACKET_LENGTH)
        buff[1] = 0xA3
        dataSend(buff: &buff)
    }
    
    private func memeAdnSetMode() {
        var buff = [UInt8](repeating: 0, count: PACKET_LENGTH)
        buff[0] = UInt8(PACKET_LENGTH)
        buff[1] = 0xA4
        buff[4] = UInt8(selectMode & 0xFF)
        buff[5] = UInt8(transMode & 0xFF)
        dataSend(buff: &buff)
    }
    
    private func memeAdnGet6AxisParams() {
        var buff = [UInt8](repeating: 0, count: PACKET_LENGTH)
        buff[0] = UInt8(PACKET_LENGTH)
        buff[1] = 0xA9
        dataSend(buff: &buff)
    }
    
    private func memeAdnSet6AxisParams() {
        var buff = [UInt8](repeating: 0, count: PACKET_LENGTH)
        buff[0] = UInt8(PACKET_LENGTH)
        buff[1] = 0xAA
        buff[2] = UInt8(accelRange & 0xFF)
        buff[3] = UInt8(gyroRange & 0xFF)
        dataSend(buff: &buff)
    }
    
    private func memeAdnGetData(state: Int) {
        var buff = [UInt8](repeating: 0, count: PACKET_LENGTH)
        buff[0] = UInt8(PACKET_LENGTH)
        buff[1] = 0xA0
        buff[2] = UInt8(state & 0xFF)
        dataSend(buff: &buff)
    }
    
    private func checkTimerStart() {
        checkTimerStop()
        checkTimer = Timer.scheduledTimer(timeInterval: CHECK_TIMEOUT_TIME,
                                          target: self,
                                          selector: #selector(checkTimeout),
                                          userInfo: nil,
                                          repeats: true)
    }
    
    private func checkTimerStop() {
        if let timer = checkTimer, timer.isValid {
            timer.invalidate()
        }
        checkTimer = nil
    }
    
    @objc private func checkTimeout(_ timer: Timer) {
        checkTimerStop()
        switch libStatus {
        case STATUS_SCAN_REQ_EXEC:
            delegate?.memePeripheralFoundDelegate(result: MEMELIB_TIMEOUT, deviceName: nil, uuid: nil)
        case STATUS_GET_STATUS_REQ_EXEC,
             STATUS_GET_VERSION_REQ_EXEC,
             STATUS_SET_TIME_REQ_EXEC,
             STATUS_CONNECTION_REQ_EXEC:
            deviceScanStop()
        default:
            break
        }
    }
    
    private func dataAnalysis() {
        guard !recvData.isEmpty else { return }
        let cnt = recvData.count - 1
        let data = recvData[cnt]
        
        var buff = [UInt8](repeating: 0, count: PACKET_LENGTH)
        let len = min(data.count, PACKET_LENGTH)
        data.copyBytes(to: &buff, count: len)
        
        DecEnc.decode(&buff)
        
        let fullData = AcademicFullData()
        
        if buff[0] == UInt8(PACKET_LENGTH) {
            switch buff[1] {
            case 0x81:
                memeVersion.major = UInt32(buff[6])
                memeVersion.minor = UInt32(buff[5])
                memeVersion.revision = UInt32(buff[4])
                memeAdnGetMode()
                libLog("0x81")
            case 0x83:
                selectMode = UInt32(buff[4])
                transMode = UInt32(buff[5])
                memeAdnGet6AxisParams()
                libLog("0x83")
            case 0x89:
                accelRange = UInt32(buff[2])
                gyroRange = UInt32(buff[3])
                delegate?.memePeripheralConnectedDelegate(result: MEMELIB_OK)
                libLog("0x89")
            case 0x8F:
                libLog("0x8F")
            case 0x98:
                libLog("0x98")
            case 0x99:
                libLog("0x99")
                
                let cntPart1 = (UInt32(buff[3]) << 8) & 0x0F00
                let cntPart2 = UInt32(buff[2])
                fullData.cnt = cntPart1 | cntPart2
                fullData.battLv = UInt16(buff[3] >> 4)
                
                // 2つのUInt8から符号付きInt16を安全に復元するヘルパー
                func toInt16(high: UInt8, low: UInt8) -> Int16 {
                    let combined = (UInt16(high) << 8) | UInt16(low)
                    return Int16(bitPattern: combined)
                }
                
                fullData.accX = toInt16(high: buff[5], low: buff[4])
                fullData.accY = toInt16(high: buff[7], low: buff[6])
                fullData.accZ = toInt16(high: buff[9], low: buff[8])
                
                fullData.gyroX = toInt16(high: buff[11], low: buff[10])
                fullData.gyroY = toInt16(high: buff[13], low: buff[12])
                fullData.gyroZ = toInt16(high: buff[15], low: buff[14])
                
                fullData.eogL = toInt16(high: buff[17], low: buff[16])
                fullData.eogR = toInt16(high: buff[19], low: buff[18])
                
                fullData.eogH = fullData.eogL &- fullData.eogR
                
                let sumEog = Int32(fullData.eogL) + Int32(fullData.eogR)
                fullData.eogV = Int16(truncatingIfNeeded: 0 - (sumEog / 2))
                
                delegate?.memeAcademicFullDataReceivedDelegate(data: fullData)
            case 0x9A:
                libLog("0x9A")
            default:
                libLog("default")
            }
        }
    }
}

// =============================================================================
// MARK: - CBCentralManagerDelegate
// =============================================================================
// @preconcurrency を付与することで、Apple側の古いプロトコル仕様と
// ユーザー側の厳密な @MainActor クラスとの衝突をコンパイラに安全に許可させます
extension MEMELib_Academic: @preconcurrency CBCentralManagerDelegate {
    
    func centralManagerDidUpdateState(_ central: CBCentralManager) {
        if central.state == .poweredOn {
            libraryFlag = true
        } else {
            libraryFlag = true // 元コードの挙動を再現
        }
    }
    
    func centralManager(_ central: CBCentralManager, didDiscover peripheral: CBPeripheral, advertisementData: [String : Any], rssi RSSI: NSNumber) {
        libLog("Call : didDiscoverPeripheral")
        var alreadyFound = false
        for p in peripherals {
            if p.identifier == peripheral.identifier {
                alreadyFound = true
                break
            }
        }
        if !alreadyFound {
            let uuid = peripheral.identifier.uuidString
            guard let name = peripheral.name else { return }
            let localName = advertisementData[CBAdvertisementDataLocalNameKey] as? String
            
            peripherals.append(peripheral)
            peripheralsLocalName.append(name)
            
            libLog("発見したBLEデバイス:\(peripheral)")
            libLog("アドバタイズメントデータ:\(advertisementData)")
            libLog("Name \(name)")
            libLog("Local Name \(localName ?? "nil")")
            libLog("RSSI:\(RSSI)")
            libLog("UUID:\(uuid)")
            
            delegate?.memePeripheralFoundDelegate(result: MEMELIB_OK, deviceName: name, uuid: uuid)
        }
    }
    
    func centralManager(_ central: CBCentralManager, didConnect peripheral: CBPeripheral) {
        libLog("Call : didConnectPeripheral")
        peripheral.delegate = self
        serviceFlag = false
        peripheral.discoverServices(nil)
    }
    
    func centralManager(_ central: CBCentralManager, didFailToConnect peripheral: CBPeripheral, error: Error?) {
        libLog("Call : didFailToConnectPeripheral")
        delegate?.memePeripheralConnectedDelegate(result: MEMELIB_NG)
        checkTimerStop()
    }
    
    func centralManager(_ central: CBCentralManager, didDisconnectPeripheral peripheral: CBPeripheral, error: Error?) {
        libLog("Call : didDisconnectPeripheral")
        if error != nil {
            delegate?.memePeripheralDisconnectedDelegate(result: MEMELIB_NG)
        } else {
            delegate?.memePeripheralDisconnectedDelegate(result: MEMELIB_OK)
        }
        checkTimerStop()
        connectedFlag = false
        measureFlag = false
        serviceFlag = false
    }
}

// =============================================================================
// MARK: - CBPeripheralDelegate
// =============================================================================
extension MEMELib_Academic: @preconcurrency CBPeripheralDelegate {
    
    func peripheral(_ peripheral: CBPeripheral, didDiscoverServices error: Error?) {
        libLog("Call : didDiscoverServices")
        if let error = error {
            libLog("エラー:\(error)")
            return
        }
        guard let services = peripheral.services else { return }
        for service in services {
            if service.uuid == CBUUID(string: SERVICES_UUID) {
                inputCharacteristic = nil
                outputCharacteristic = nil
                
                let targetCharacteristics = [
                    CBUUID(string: CHARACTERISTICS_READ_UUID),
                    CBUUID(string: CHARACTERISTICS_WRITE_UUID)
                ]
                peripheral.discoverCharacteristics(targetCharacteristics, for: service)
                break
            }
        }
    }
    
    func peripheral(_ peripheral: CBPeripheral, didDiscoverCharacteristicsFor service: CBService, error: Error?) {
        libLog("Call : didDiscoverCharacteristicsForService")
        if let error = error {
            libLog("エラー:\(error)")
            return
        }
        guard let characteristics = service.characteristics else { return }
        if service.uuid == CBUUID(string: SERVICES_UUID) {
            for characteristic in characteristics {
                libLog("characteristic.UUID \(characteristic.uuid)")
                libLog("characteristic.properties \(characteristic.properties.rawValue)")
                libLog("characteristic.description \(characteristic.description)")
                
                if characteristic.uuid == CBUUID(string: CHARACTERISTICS_READ_UUID) {
                    inputCharacteristic = characteristic
                    libLog("Read を発見！")
                    peripheral.setNotifyValue(true, for: characteristic)
                }
                if characteristic.uuid == CBUUID(string: CHARACTERISTICS_WRITE_UUID) {
                    outputCharacteristic = characteristic
                    libLog("Write を発見！")
                    if serviceFlag {
                        memeAdnGetDevInfo()
                    }
                }
            }
        }
    }
    
    func peripheral(_ peripheral: CBPeripheral, didModifyServices invalidatedServices: [CBService]) {
        libLog("Call : didModifyServices")
        let services = [CBUUID(string: SERVICES_UUID)]
        peripheral.discoverServices(services)
    }
    
    func peripheral(_ peripheral: CBPeripheral, didUpdateNotificationStateFor characteristic: CBCharacteristic, error: Error?) {
        libLog("Call : didUpdateNotificationStateForCharacteristic")
        if let error = error {
            libLog("Notify状態更新失敗...error:\(error)")
        } else {
            libLog("Notify状態更新成功！characteristic UUID:\(characteristic.uuid), isNotifying:\(characteristic.isNotifying)")
            connectedFlag = true
            
            if characteristic.isNotifying {
                libLog("characteristic.isNotifying == YES")
                if outputCharacteristic == nil {
                    libLog("characteristic.isNotifying == YES")
                    serviceFlag = true
                } else {
                    libLog("characteristic.isNotifying == NO")
                    memeAdnGetDevInfo()
                }
            }
        }
    }
    
    func peripheral(_ peripheral: CBPeripheral, didUpdateValueFor characteristic: CBCharacteristic, error: Error?) {
        if let error = error {
            libLog("Read失敗...error:\(error), characteristic uuid:\(characteristic.uuid)")
            return
        }
        if let value = characteristic.value {
            recvData.append(value)
            dataAnalysis()
        }
    }
}
