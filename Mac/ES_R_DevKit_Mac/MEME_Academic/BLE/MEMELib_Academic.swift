//
//  MEMELib_Academic.swift
//  MEME_Academic
//
//  Created by D-CLUE on 2017/03/22.
//  Copyright © 2017年 jins-jp. All rights reserved.
//

import Foundation
import CoreBluetooth

// MARK: - Global Constants
let MEMELIB_OK: UInt32 = 0
let MEMELIB_NG: UInt32 = 1
let MEMELIB_TIMEOUT: UInt32 = 2

let SERVICES_UUID = "D6F25BD1-5B54-4360-96D8-7AA62E04C7EF"
let CHARACTERISTICS_READ_UUID = "D6F25BD4-5B54-4360-96D8-7AA62E04C7EF"
let CHARACTERISTICS_WRITE_UUID = "D6F25BD2-5B54-4360-96D8-7AA62E04C7EF"
let DEVICE_INFORMATION_UUID = "180A"
let SYSTEM_ID_UUID = "2A23"

let MEMEMode_Standard: UInt32 = 1
let MEMEMode_Full: UInt32 = 2
let MEMEMode_Quaternion: UInt32 = 3

/// ADN_SET_MODE の mode バイト(buff[4])へ入れる CONFIG モードの値。
/// 通常の計測モード(1..3)とは別枠で、SHELF コマンドの前段としてのみ使う。
let MEMEMode_Config: UInt32 = 0x0F

/// 保管(SHELF)モードへの遷移コマンド。op の後ろに ASCII "SHELF" を置く形で、
/// BOOT(0x40 + "BOOT") と同じ「合言葉つき」の系列。CONFIG モードでのみ受理される。
/// 出典は Web Bluetooth 版 SDK (tkomde/webbt common/memelib_acp.js の startShelf)。
let ADN_SHELF: UInt8 = 0x41

let MEMEQuality_High: UInt32 = 1
let MEMEQuality_Low: UInt32 = 2

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

let CHECK_TIMEOUT_TIME: TimeInterval = 30

/// Shelf 移行の1段目(CONFIG モードへの遷移)の ACK を待つ時間。通常は 100ms 台で返る。
/// ここで諦めても SHELF コマンドは送らないので端末は通常モードのまま無傷。
let SHELF_CONFIG_ACK_TIMEOUT_TIME: TimeInterval = 3
let TIME_SYNC_COUNT: Int = 2
let PACKET_LENGTH: Int = 20

func libLog(_ message: String, file: String = #file, line: Int = #line, function: String = #function) {
    #if DEBUG
    let fileName = (file as NSString).lastPathComponent
    print("\(fileName) \(line) \(function) \(message)")
    #endif
}

// MARK: - Delegate Protocol
@MainActor
protocol MEMELibAcademicDelegate: AnyObject {
    func memePeripheralFoundDelegate(result: UInt32, deviceName: String?, uuid: String?)
    func memePeripheralConnectedDelegate(result: UInt32)
    func memePeripheralDisconnectedDelegate(result: UInt32)
    func memeAcademicStandardDataReceivedDelegate(data: AcademicStandardData)
    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData)
    func memeAcademicQuaternionDataReceivedDelegate(data: AcademicQuaternionData)
}

// MARK: - Main Class
@MainActor
class MEMELib_Academic: NSObject, MEMELibInterface {

    var centralManager: CBCentralManager!
    var peripheral: CBPeripheral?
    var peripherals: [CBPeripheral] = []
    var peripheralsLocalName: [String] = []
    var inputCharacteristic: CBCharacteristic?
    var outputCharacteristic: CBCharacteristic?
    var recvData: [Data] = []
    var memeVersion = Version()
    var sdkVersion = Version()
    var macAddress: String = ""

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

    /// central がまだ .poweredOn でない状態で startScanningPeripherals が
    /// 呼ばれたときに保留しておき、状態が .poweredOn になった瞬間に実行する。
    private var pendingScanRequest = false

    /// Shelf 移行で CONFIG モードの ACK(AUP_REPORT_RESP)を1件だけ待つためのハンドラ。
    /// enterShelfMode がコマンド送信の直前にセットし、dataAnalysis / 切断 / タイムアウトが畳む。
    private var shelfAckHandler: ((Bool) -> Void)?
    private var shelfAckTimer: Timer?

    private var libStatus: Int = STATUS_IDLE
    private var selectMode: UInt32 = MEMEMode_Full
    private var transMode: UInt32 = MEMEQuality_High
    private var accelRange: UInt32 = MEMEAccelRange_2G
    private var gyroRange: UInt32 = MEMEGyroRange_250dps

    override init() {
        super.init()
        self.centralManager = CBCentralManager(delegate: self, queue: nil)
    }

    // MARK: - Public Methods

    @discardableResult
    func startScanningPeripherals() -> UInt32 {
        if connectedFlag {
            return MEMELIB_NG
        }
        if scanFlag {
            // すでにスキャン中なら冪等に成功扱い
            return MEMELIB_OK
        }
        if !libraryFlag {
            // BT がまだ .poweredOn でない。リクエストを保留し、
            // centralManagerDidUpdateState(.poweredOn) で発火する。
            pendingScanRequest = true
            return MEMELIB_OK
        }
        deviceScanStart()
        return MEMELIB_OK
    }

    @discardableResult
    func stopScanningPeripherals() -> UInt32 {
        if libraryFlag && scanFlag && !connectedFlag {
            deviceScanStop()
            return MEMELIB_OK
        }
        return MEMELIB_NG
    }

    @discardableResult
    func connectPeripheral(deviceName: String) -> UInt32 {
        if libraryFlag && !connectedFlag {
            deviceConnectionStart(deviceName: deviceName)
            return MEMELIB_OK
        }
        return MEMELIB_NG
    }

    @discardableResult
    func disconnectPeripheral() -> UInt32 {
        if libraryFlag && connectedFlag {
            deviceDisconnectionStart()
            return MEMELIB_OK
        }
        return MEMELIB_NG
    }

    func getSelectMode() -> UInt32 { return selectMode }

    @discardableResult
    func setSelectMode(_ mode: UInt32) -> UInt32 {
        selectMode = mode
        memeAdnSetMode()
        return MEMELIB_OK
    }

    func getTransMode() -> UInt32 { return transMode }

    @discardableResult
    func setTransMode(_ mode: UInt32) -> UInt32 {
        transMode = mode
        memeAdnSetMode()
        return MEMELIB_OK
    }

    func getAccelRange() -> UInt32 { return accelRange }

    @discardableResult
    func setAccelRange(_ range: UInt32) -> UInt32 {
        accelRange = range
        memeAdnSet6AxisParams()
        return MEMELIB_OK
    }

    func getGyroRange() -> UInt32 { return gyroRange }

    @discardableResult
    func setGyroRange(_ range: UInt32) -> UInt32 {
        gyroRange = range
        memeAdnSet6AxisParams()
        return MEMELIB_OK
    }

    @discardableResult
    func startDataReport() -> UInt32 {
        if libraryFlag && connectedFlag && !measureFlag {
            measureFlag = true
            memeAdnGetData(state: 0x01)
            return MEMELIB_OK
        }
        return MEMELIB_NG
    }

    @discardableResult
    func stopDataReport() -> UInt32 {
        if libraryFlag && connectedFlag && measureFlag {
            measureFlag = false
            memeAdnGetData(state: 0x00)
            return MEMELIB_OK
        }
        return MEMELIB_NG
    }

    // MARK: - Shelf mode

    /// 端末を Shelf mode（保管モード）へ移行させる。Web Bluetooth 版 SDK と同じ順序で
    /// (1) CONFIG モードへの遷移を送り (2) その ACK を待ってから (3) SHELF を送る。
    /// completion(true) は「SHELF を送信した」まで。受理されると端末は自ら切断するので、
    /// 移行できたかどうかは呼び出し側が切断の到着で判断する。
    /// ACK が来なければ SHELF は送らないため、失敗しても端末は通常モードのまま。
    func enterShelfMode(completion: @escaping (Bool) -> Void) {
        guard libraryFlag, connectedFlag, !measureFlag else {
            completion(false)
            return
        }
        // 待ちは常に1件。多重に呼ばれたら前の待ちは失敗として畳む。
        finishShelfAck(false)
        shelfAckHandler = { [weak self] acked in
            guard let self, acked else {
                completion(false)
                return
            }
            // CONFIG への遷移が受理されたときだけ SHELF を送る。
            self.memeAdnShelf()
            completion(true)
        }
        shelfAckTimer = Timer.scheduledTimer(withTimeInterval: SHELF_CONFIG_ACK_TIMEOUT_TIME,
                                             repeats: false) { [weak self] _ in
            Task { @MainActor in
                self?.finishShelfAck(false)
            }
        }
        memeAdnSetConfigMode()
    }

    /// 待っている Shelf の ACK を結果付きで畳む（待ちが無ければ何もしない）。
    private func finishShelfAck(_ acked: Bool) {
        shelfAckTimer?.invalidate()
        shelfAckTimer = nil
        guard let handler = shelfAckHandler else { return }
        shelfAckHandler = nil
        handler(acked)
    }

    // MARK: - Private Methods

    private func dataSend(buff: inout [UInt8]) {
        buff.withUnsafeMutableBufferPointer { ptr in
            if let baseAddress = ptr.baseAddress {
                DecEnc.encode(baseAddress)
            }
        }
        let sendData = Data(buff)
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

    /// CONFIG モードへの遷移（ADN_SET_MODE の mode=0x0F, quality=0）。
    /// SHELF コマンドは CONFIG モードでのみ受理されるので、memeAdnShelf の前に送って ACK を待つ。
    private func memeAdnSetConfigMode() {
        var buff = [UInt8](repeating: 0, count: PACKET_LENGTH)
        buff[0] = UInt8(PACKET_LENGTH)
        buff[1] = 0xA4
        buff[4] = UInt8(MEMEMode_Config & 0xFF)
        buff[5] = 0
        dataSend(buff: &buff)
    }

    /// 保管(SHELF)モードへの遷移（op 0x41 + ASCII "SHELF"）。受理されると端末は
    /// ペアリング機能を止めて自ら切断するので、切断が成功の合図になる。
    private func memeAdnShelf() {
        var buff = [UInt8](repeating: 0, count: PACKET_LENGTH)
        buff[0] = UInt8(PACKET_LENGTH)
        buff[1] = ADN_SHELF
        for (i, c) in Array("SHELF".utf8).enumerated() {
            buff[i + 2] = c
        }
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

    private static func toInt16(high: UInt8, low: UInt8) -> Int16 {
        let combined = (UInt16(high) << 8) | UInt16(low)
        return Int16(bitPattern: combined)
    }

    private func dataAnalysis() {
        guard !recvData.isEmpty else { return }
        let cnt = recvData.count - 1
        let data = recvData[cnt]
        // 最新パケットのみ処理し、受信バッファは都度クリアする。
        // （従来は append するだけで recvData が無制限に増え続け、計測中にメモリを消費し続けていた。）
        recvData.removeAll(keepingCapacity: true)

        var buff = [UInt8](repeating: 0, count: PACKET_LENGTH)
        let len = min(data.count, PACKET_LENGTH)
        data.copyBytes(to: &buff, count: len)

        buff.withUnsafeMutableBufferPointer { ptr in
            if let baseAddress = ptr.baseAddress {
                DecEnc.decode(baseAddress)
            }
        }

        if buff[0] == UInt8(PACKET_LENGTH) {
            switch buff[1] {
            // AUP_REPORT_DEV_INFO
            case 0x81:
                memeVersion.major = UInt32(buff[6])
                memeVersion.minor = UInt32(buff[5])
                memeVersion.revision = UInt32(buff[4])
                memeAdnGetMode()
                libLog("0x81 AUP_REPORT_DEV_INFO")
            // AUP_REPORT_MODE
            case 0x83:
                selectMode = UInt32(buff[4])
                transMode = UInt32(buff[5])
                memeAdnGet6AxisParams()
                libLog("0x83 AUP_REPORT_MODE")
            // AUP_REPORT_6AXIS_PRMS
            case 0x89:
                accelRange = UInt32(buff[2])
                gyroRange = UInt32(buff[3])
                delegate?.memePeripheralConnectedDelegate(result: MEMELIB_OK)
                libLog("0x89 AUP_REPORT_6AXIS_PRMS")
            // AUP_REPORT_RESP
            case 0x8F:
                libLog("0x8F AUP_REPORT_RESP")
                // Shelf 移行が CONFIG 遷移の結果を待っていれば渡す（buff[2]==0x00 が ACK）。
                finishShelfAck(buff[2] == 0x00)
            // AUP_REPORT_ACADEMIC1
            case 0x98:
                //libLog("0x98")
                let standardData = AcademicStandardData()
                let cntPart1 = (UInt32(buff[3]) << 8) & 0x0F00
                let cntPart2 = UInt32(buff[2])
                standardData.cnt = cntPart1 | cntPart2
                standardData.battLv = UInt16(buff[3] >> 4)
                standardData.accX = Self.toInt16(high: buff[5], low: buff[4])
                standardData.accY = Self.toInt16(high: buff[7], low: buff[6])
                standardData.accZ = Self.toInt16(high: buff[9], low: buff[8])
                standardData.eogL1 = Self.toInt16(high: buff[11], low: buff[10])
                standardData.eogR1 = Self.toInt16(high: buff[13], low: buff[12])
                standardData.eogL2 = Self.toInt16(high: buff[15], low: buff[14])
                standardData.eogR2 = Self.toInt16(high: buff[17], low: buff[16])
                standardData.eogH1 = standardData.eogL1 &- standardData.eogR1
                standardData.eogH2 = standardData.eogL2 &- standardData.eogR2
                let sum1 = Int32(standardData.eogL1) + Int32(standardData.eogR1)
                let sum2 = Int32(standardData.eogL2) + Int32(standardData.eogR2)
                standardData.eogV1 = Int16(truncatingIfNeeded: 0 - (sum1 / 2))
                standardData.eogV2 = Int16(truncatingIfNeeded: 0 - (sum2 / 2))
                delegate?.memeAcademicStandardDataReceivedDelegate(data: standardData)
            // AUP_REPORT_ACADEMIC2
            case 0x99:
                //libLog("0x99")
                let fullData = AcademicFullData()
                let cntPart1 = (UInt32(buff[3]) << 8) & 0x0F00
                let cntPart2 = UInt32(buff[2])
                fullData.cnt = cntPart1 | cntPart2
                fullData.battLv = UInt16(buff[3] >> 4)
                fullData.accX = Self.toInt16(high: buff[5], low: buff[4])
                fullData.accY = Self.toInt16(high: buff[7], low: buff[6])
                fullData.accZ = Self.toInt16(high: buff[9], low: buff[8])
                fullData.gyroX = Self.toInt16(high: buff[11], low: buff[10])
                fullData.gyroY = Self.toInt16(high: buff[13], low: buff[12])
                fullData.gyroZ = Self.toInt16(high: buff[15], low: buff[14])
                fullData.eogL = Self.toInt16(high: buff[17], low: buff[16])
                fullData.eogR = Self.toInt16(high: buff[19], low: buff[18])
                fullData.eogH = fullData.eogL &- fullData.eogR
                let sumEog = Int32(fullData.eogL) + Int32(fullData.eogR)
                fullData.eogV = Int16(truncatingIfNeeded: 0 - (sumEog / 2))
                delegate?.memeAcademicFullDataReceivedDelegate(data: fullData)
            // AUP_REPORT_ACADEMIC3
            case 0x9A:
                //libLog("0x9A")
                let quaternionData = AcademicQuaternionData()
                let cntPart1 = (UInt32(buff[3]) << 8) & 0x0F00
                let cntPart2 = UInt32(buff[2])
                quaternionData.cnt = cntPart1 | cntPart2
                quaternionData.battLv = UInt16(buff[3] >> 4)
                quaternionData.quaternionW = Int64(Int32(bitPattern:
                    (UInt32(buff[7]) << 24) | (UInt32(buff[6]) << 16) | (UInt32(buff[5]) << 8) | UInt32(buff[4])))
                quaternionData.quaternionX = Int64(Int32(bitPattern:
                    (UInt32(buff[11]) << 24) | (UInt32(buff[10]) << 16) | (UInt32(buff[9]) << 8) | UInt32(buff[8])))
                quaternionData.quaternionY = Int64(Int32(bitPattern:
                    (UInt32(buff[15]) << 24) | (UInt32(buff[14]) << 16) | (UInt32(buff[13]) << 8) | UInt32(buff[12])))
                quaternionData.quaternionZ = Int64(Int32(bitPattern:
                    (UInt32(buff[19]) << 24) | (UInt32(buff[18]) << 16) | (UInt32(buff[17]) << 8) | UInt32(buff[16])))
                delegate?.memeAcademicQuaternionDataReceivedDelegate(data: quaternionData)
            default:
                libLog("default")
            }
        }
    }
}

// MARK: - CBCentralManagerDelegate
extension MEMELib_Academic: @preconcurrency CBCentralManagerDelegate {

    func centralManagerDidUpdateState(_ central: CBCentralManager) {
        libraryFlag = (central.state == .poweredOn)
        if !libraryFlag {
            // BT が OFF/resetting/unauthorized 等になった場合は
            // 進行中のスキャンを止めて整合性を取る。
            if scanFlag {
                deviceScanStop()
            }
            return
        }
        // .poweredOn になった瞬間に保留スキャンを実行する。
        if pendingScanRequest && !connectedFlag && !scanFlag {
            pendingScanRequest = false
            deviceScanStart()
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
        // 切断されたら ACK はもう来ない。待ちがあれば失敗として畳む。
        finishShelfAck(false)
        connectedFlag = false
        measureFlag = false
        serviceFlag = false
    }
}

// MARK: - CBPeripheralDelegate
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
                let targets = [
                    CBUUID(string: CHARACTERISTICS_READ_UUID),
                    CBUUID(string: CHARACTERISTICS_WRITE_UUID)
                ]
                peripheral.discoverCharacteristics(targets, for: service)
            } else if service.uuid == CBUUID(string: DEVICE_INFORMATION_UUID) {
                let targets = [CBUUID(string: SYSTEM_ID_UUID)]
                peripheral.discoverCharacteristics(targets, for: service)
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
        } else if service.uuid == CBUUID(string: DEVICE_INFORMATION_UUID) {
            for characteristic in characteristics {
                if characteristic.uuid == CBUUID(string: SYSTEM_ID_UUID) {
                    libLog("SYSTEM_ID を発見！")
                    peripheral.readValue(for: characteristic)
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
            return
        }
        libLog("Notify状態更新成功！characteristic UUID:\(characteristic.uuid), isNotifying:\(characteristic.isNotifying)")
        connectedFlag = true
        if characteristic.isNotifying {
            if outputCharacteristic == nil {
                serviceFlag = true
            } else {
                memeAdnGetDevInfo()
            }
        }
    }

    func peripheral(_ peripheral: CBPeripheral, didUpdateValueFor characteristic: CBCharacteristic, error: Error?) {
        if let error = error {
            libLog("Read失敗...error:\(error), characteristic uuid:\(characteristic.uuid)")
            return
        }
        if characteristic.uuid == CBUUID(string: SYSTEM_ID_UUID) {
            NSLog("characteristic.value:%@", characteristic.value.debugDescription)
            guard let data = characteristic.value, data.count >= 8 else { return }
            let d1 = data.subdata(in: 0..<3)
            let d2 = data.subdata(in: 5..<8)
            var md = Data()
            md.append(d1)
            md.append(d2)
            var result = ""
            for byte in md {
                result += String(format: "%02X", byte)
            }
            NSLog("result:%@", result)
            macAddress = result
        } else {
            if let value = characteristic.value {
                recvData.append(value)
                dataAnalysis()
            }
        }
    }

    func peripheral(_ peripheral: CBPeripheral, didWriteValueFor characteristic: CBCharacteristic, error: Error?) {
        if let error = error {
            libLog("Write失敗...error:\(error)")
        }
    }
}
