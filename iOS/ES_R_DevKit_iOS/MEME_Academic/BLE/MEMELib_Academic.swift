//
//  MEMELib_Academic.swift
//  MEME_Academic (iOS)
//
//  CoreBluetooth で ES_R とやり取りする。手順とコマンドは Mac 版 MEMELib_Academic.swift と同じで、iOS 版では次を変えてある。
//  - 1 回の通知に入っている 20byte のパケットを全部読む(Mac 版は最後の 1 個だけ。iOS は MTU を広げるので連結されうる)
//  - 切断の理由を delegate へ渡す(切断ログ _disconnect.csv に残す。Android と同じ考え方)
//  - reconnectPeripheral: 最後に繋いだ端末へ接続要求を出したままにする。iOS の接続要求は期限が無く、
//    端末が電波の届くところへ戻れば(アプリが裏にいても)繋がる
//  - Bluetooth が OFF になったら、繋がっていた端末は切断として知らせる(iOS は didDisconnect を呼ばないことがある)
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
let ADN_SHELF: UInt8 = 0x41

let MEMEQuality_High: UInt32 = 1
let MEMEQuality_Low: UInt32 = 2

let MEMEAccelRange_2G: UInt32 = 0
let MEMEGyroRange_250dps: UInt32 = 0
/// Quaternion モードでは端末がジャイロ 2000dps で動くので、その値を送る(Android と同じ)
let MEMEGyroRange_2000dps: UInt32 = 3

/// スキャンを続ける時間。見つからなければ memePeripheralFoundDelegate(MEMELIB_TIMEOUT)
let SCAN_TIMEOUT_TIME: TimeInterval = 10
/// Connect ボタンからの接続を待つ時間(再接続 reconnectPeripheral は期限なし)
let CONNECT_TIMEOUT_TIME: TimeInterval = 15
/// コマンドの ACK(AUP_REPORT_RESP)を待つ時間(Shelf の CONFIG 遷移・初期化)
let RESP_TIMEOUT_TIME: TimeInterval = 3
let PACKET_LENGTH: Int = 20

@MainActor
final class MEMELib_Academic: NSObject, MEMELibInterface {

    weak var delegate: MEMELibAcademicDelegate?
    private(set) var memeVersion = Version()
    private(set) var macAddress: String = ""
    var isPoweredOn: Bool { central.state == .poweredOn }

    private var central: CBCentralManager!
    /// スキャンで見つけた端末(名前 → 端末)
    private var found: [String: CBPeripheral] = [:]
    /// 今の(切れた後は最後に繋いだ)端末
    private var peripheral: CBPeripheral?
    private var notifyChar: CBCharacteristic?
    private var writeChar: CBCharacteristic?

    private var pendingScan = false
    private var scanning = false
    /// 端末の設定を読み終えて計測できる状態
    private var ready = false
    private var measuring = false
    /// 接続を要求してから切れるまで(接続待ちを含む)
    private var linking = false

    private var scanTimer: Timer?
    private var connectTimer: Timer?

    /// コマンドの ACK(AUP_REPORT_RESP)を 1 件だけ待つ(Shelf の CONFIG 遷移・初期化)
    private var respHandler: ((Bool) -> Void)?
    private var respTimer: Timer?

    private var selectMode: UInt32 = MEMEMode_Standard
    private var transMode: UInt32 = MEMEQuality_High
    private var accelRange: UInt32 = MEMEAccelRange_2G
    private var gyroRange: UInt32 = MEMEGyroRange_250dps

    override init() {
        super.init()
        central = CBCentralManager(delegate: self, queue: nil)
    }

    // MARK: - Scan / Connection

    @discardableResult
    func startScanningPeripherals() -> UInt32 {
        guard !linking else { return MEMELIB_NG }
        guard isPoweredOn else {
            // まだ .poweredOn でない(起動直後など)。状態が変わったら始める
            pendingScan = true
            return MEMELIB_OK
        }
        if scanning { central.stopScan() }
        found.removeAll()
        scanning = true
        central.scanForPeripherals(withServices: [CBUUID(string: SERVICES_UUID)],
                                   options: [CBCentralManagerScanOptionAllowDuplicatesKey: false])
        // OS や他のアプリが既に繋いでいる ES_R は広告を出さないので、先に一覧へ入れる(Android の mergeSystemDevices と同じ)
        // 名前がまだ分からない端末(前に繋いだアプリが落ちて接続だけ残ったときなど)も、識別子の頭で一覧に入れる
        for p in central.retrieveConnectedPeripherals(withServices: [CBUUID(string: SERVICES_UUID)]) {
            NSLog("[MEMELib] already connected: %@ %@", p.name ?? "(no name)", p.identifier.uuidString)
            add(p, name: p.name ?? "ES_R \(p.identifier.uuidString.prefix(8))")
        }
        scanTimer?.invalidate()
        scanTimer = Timer.scheduledTimer(withTimeInterval: SCAN_TIMEOUT_TIME, repeats: false) { [weak self] _ in
            MainActor.assumeIsolated { self?.scanTimedOut() }
        }
        return MEMELIB_OK
    }

    @discardableResult
    func stopScanningPeripherals() -> UInt32 {
        pendingScan = false
        guard scanning else { return MEMELIB_NG }
        stopScan()
        return MEMELIB_OK
    }

    private func stopScan() {
        scanTimer?.invalidate()
        scanTimer = nil
        if scanning, isPoweredOn { central.stopScan() }
        scanning = false
    }

    private func scanTimedOut() {
        guard scanning else { return }
        stopScan()
        delegate?.memePeripheralFoundDelegate(result: MEMELIB_TIMEOUT, deviceName: nil, uuid: nil)
    }

    private func add(_ p: CBPeripheral, name: String?) {
        guard let name, found[name] == nil else { return }
        found[name] = p
        delegate?.memePeripheralFoundDelegate(result: MEMELIB_OK, deviceName: name, uuid: p.identifier.uuidString)
    }

    @discardableResult
    func connectPeripheral(deviceName: String) -> UInt32 {
        guard isPoweredOn, !linking, let p = found[deviceName] else { return MEMELIB_NG }
        stopScan()
        connect(p)
        connectTimer = Timer.scheduledTimer(withTimeInterval: CONNECT_TIMEOUT_TIME, repeats: false) { [weak self] _ in
            MainActor.assumeIsolated { self?.connectTimedOut() }
        }
        return MEMELIB_OK
    }

    @discardableResult
    func reconnectPeripheral() -> UInt32 {
        guard isPoweredOn, !linking, let p = peripheral else { return MEMELIB_NG }
        connect(p)
        return MEMELIB_OK
    }

    private func connect(_ p: CBPeripheral) {
        peripheral = p
        p.delegate = self
        linking = true
        ready = false
        measuring = false
        notifyChar = nil
        writeChar = nil
        central.connect(p, options: nil)
    }

    private func connectTimedOut() {
        connectTimer = nil
        guard linking, !ready, let p = peripheral else { return }
        central.cancelPeripheralConnection(p)
        resetLink()
        delegate?.memePeripheralConnectedDelegate(result: MEMELIB_NG)
    }

    @discardableResult
    func disconnectPeripheral() -> UInt32 {
        guard linking, let p = peripheral else { return MEMELIB_NG }
        if ready {
            // 切断は didDisconnect で知らせる
            if let c = notifyChar { p.setNotifyValue(false, for: c) }
            central.cancelPeripheralConnection(p)
        } else {
            // 接続待ち・準備中の取り消し。まだ繋がっていなければ didDisconnect は来ないので、ここで知らせる
            central.cancelPeripheralConnection(p)
            resetLink()
            delegate?.memePeripheralDisconnectedDelegate(result: MEMELIB_OK, reason: "CANCELLED")
        }
        return MEMELIB_OK
    }

    private func resetLink() {
        connectTimer?.invalidate()
        connectTimer = nil
        finishResp(false)
        linking = false
        ready = false
        measuring = false
        notifyChar = nil
        writeChar = nil
    }

    // MARK: - Mode / Range

    func getSelectMode() -> UInt32 { selectMode }
    func getTransMode() -> UInt32 { transMode }
    func getAccelRange() -> UInt32 { accelRange }
    func getGyroRange() -> UInt32 { gyroRange }

    func applySettings(mode: UInt32, trans: UInt32, accel: UInt32, gyro: UInt32) {
        selectMode = mode
        transMode = trans
        accelRange = accel
        gyroRange = mode == MEMEMode_Quaternion ? MEMEGyroRange_2000dps : gyro
        send(0xA4, [4: UInt8(selectMode & 0xFF), 5: UInt8(transMode & 0xFF)])
        send(0xAA, [2: UInt8(accelRange & 0xFF), 3: UInt8(gyroRange & 0xFF)])
        // 端末が実際に使う値を読み返す(0x83 → GET_6AXIS → 0x89 で get* が端末の値になる。Android も読み返して確かめている)
        send(0xA3)
    }

    // MARK: - Data Report

    @discardableResult
    func startDataReport() -> UInt32 {
        guard ready, !measuring else { return MEMELIB_NG }
        measuring = true
        send(0xA0, [2: 0x01])
        return MEMELIB_OK
    }

    @discardableResult
    func stopDataReport() -> UInt32 {
        guard ready, measuring else { return MEMELIB_NG }
        measuring = false
        send(0xA0, [2: 0x00])
        return MEMELIB_OK
    }

    // MARK: - Parameters / Shelf mode

    func initializeParams(completion: @escaping (Bool) -> Void) {
        guard ready, !measuring else { completion(false); return }
        waitResp(completion)
        send(0xA8, [2: 0xFF])
    }

    /// 端末を Shelf mode（保管モード）へ移行させる。Web Bluetooth 版 SDK と同じ順序で
    /// (1) CONFIG モードへの遷移を送り (2) その ACK を待ってから (3) SHELF を送る。
    /// ACK が来なければ SHELF は送らないため、失敗しても端末は通常モードのまま。
    func enterShelfMode(completion: @escaping (Bool) -> Void) {
        guard ready, !measuring else { completion(false); return }
        waitResp { [weak self] acked in
            guard let self, acked else { completion(false); return }
            var shelf: [Int: UInt8] = [:]
            for (i, c) in Array("SHELF".utf8).enumerated() { shelf[i + 2] = c }
            self.send(ADN_SHELF, shelf)
            completion(true)
        }
        send(0xA4, [4: UInt8(MEMEMode_Config & 0xFF), 5: 0])
    }

    /// 待ちは常に 1 件。多重に呼ばれたら前の待ちは失敗として畳む
    private func waitResp(_ handler: @escaping (Bool) -> Void) {
        finishResp(false)
        respHandler = handler
        respTimer = Timer.scheduledTimer(withTimeInterval: RESP_TIMEOUT_TIME, repeats: false) { [weak self] _ in
            MainActor.assumeIsolated { self?.finishResp(false) }
        }
    }

    private func finishResp(_ acked: Bool) {
        respTimer?.invalidate()
        respTimer = nil
        guard let handler = respHandler else { return }
        respHandler = nil
        handler(acked)
    }

    // MARK: - Packets

    /// 20byte のコマンドを暗号化して書く。params は [バイト位置: 値]
    private func send(_ op: UInt8, _ params: [Int: UInt8] = [:]) {
        guard let p = peripheral, let c = writeChar else { return }
        p.writeValue(MemePacket.command(op, params), for: c, type: .withResponse)
    }

    private func handle(_ b: [UInt8]) {
        switch b[1] {
        case 0x81:  // AUP_REPORT_DEV_INFO
            memeVersion.major = UInt32(b[6])
            memeVersion.minor = UInt32(b[5])
            memeVersion.revision = UInt32(b[4])
            send(0xA3)
        case 0x83:  // AUP_REPORT_MODE
            selectMode = UInt32(b[4])
            transMode = UInt32(b[5])
            send(0xA9)
        case 0x89:  // AUP_REPORT_6AXIS_PRMS
            accelRange = UInt32(b[2])
            gyroRange = UInt32(b[3])
            if !ready {
                ready = true
                connectTimer?.invalidate()
                connectTimer = nil
                delegate?.memePeripheralConnectedDelegate(result: MEMELIB_OK)
            }
        case 0x8F:  // AUP_REPORT_RESP(b[2] == 0 が ACK)
            finishResp(b[2] == 0x00)
        case 0x98, 0x99, 0x9A:
            guard let data = MemePacket.parse(b) else { return }
            switch data {
            case let d as AcademicStandardData: delegate?.memeAcademicStandardDataReceivedDelegate(data: d)
            case let d as AcademicFullData: delegate?.memeAcademicFullDataReceivedDelegate(data: d)
            case let d as AcademicQuaternionData: delegate?.memeAcademicQuaternionDataReceivedDelegate(data: d)
            default: break
            }
        default:
            break
        }
    }

    /// CoreBluetooth の切断理由を、切断ログに書く名前へ(Android の gattDisconnectReason に寄せる)
    static func disconnectReason(_ error: Error?) -> String {
        guard let error else { return "SUCCESS" }
        let ns = error as NSError
        guard ns.domain == CBErrorDomain else { return "\(ns.domain)_\(ns.code)" }
        switch CBError.Code(rawValue: ns.code) {
        case .connectionTimeout: return "CONNECTION_TIMEOUT"          // 電波が届かなくなった
        case .peripheralDisconnected: return "REMOTE_USER_TERMINATED" // 端末側から切れた(電源断・電池切れ)
        case .connectionFailed: return "CONNECTION_FAILED"
        case .operationCancelled: return "CANCELLED"
        default: return "CB_ERROR_\(ns.code)"
        }
    }
}

// MARK: - CBCentralManagerDelegate
extension MEMELib_Academic: @preconcurrency CBCentralManagerDelegate {

    func centralManagerDidUpdateState(_ central: CBCentralManager) {
        let on = central.state == .poweredOn
        if on {
            if pendingScan {
                pendingScan = false
                startScanningPeripherals()
            }
        } else {
            stopScan()
            if linking {
                // OFF で接続は消えるが、didDisconnect は来ないことがある。こちらで切断として畳む
                let wasReady = ready
                resetLink()
                if wasReady {
                    delegate?.memePeripheralDisconnectedDelegate(result: MEMELIB_NG, reason: "ADAPTER_OFF")
                } else {
                    delegate?.memePeripheralConnectedDelegate(result: MEMELIB_NG)
                }
            }
        }
        delegate?.memeBluetoothStateDelegate(poweredOn: on)
    }

    func centralManager(_ central: CBCentralManager, didDiscover peripheral: CBPeripheral,
                        advertisementData: [String: Any], rssi RSSI: NSNumber) {
        add(peripheral, name: peripheral.name ?? advertisementData[CBAdvertisementDataLocalNameKey] as? String)
    }

    func centralManager(_ central: CBCentralManager, didConnect peripheral: CBPeripheral) {
        guard peripheral == self.peripheral else { return }
        peripheral.discoverServices([CBUUID(string: SERVICES_UUID), CBUUID(string: DEVICE_INFORMATION_UUID)])
    }

    func centralManager(_ central: CBCentralManager, didFailToConnect peripheral: CBPeripheral, error: Error?) {
        guard peripheral == self.peripheral, linking else { return }
        resetLink()
        delegate?.memePeripheralConnectedDelegate(result: MEMELIB_NG)
    }

    func centralManager(_ central: CBCentralManager, didDisconnectPeripheral peripheral: CBPeripheral, error: Error?) {
        guard peripheral == self.peripheral, linking else { return }
        let wasReady = ready
        resetLink()
        if wasReady {
            delegate?.memePeripheralDisconnectedDelegate(result: error == nil ? MEMELIB_OK : MEMELIB_NG,
                                                         reason: Self.disconnectReason(error))
        } else {
            // 準備の途中で切れた
            delegate?.memePeripheralConnectedDelegate(result: MEMELIB_NG)
        }
    }
}

// MARK: - CBPeripheralDelegate
extension MEMELib_Academic: @preconcurrency CBPeripheralDelegate {

    func peripheral(_ peripheral: CBPeripheral, didDiscoverServices error: Error?) {
        guard error == nil else { return }
        for service in peripheral.services ?? [] {
            if service.uuid == CBUUID(string: SERVICES_UUID) {
                peripheral.discoverCharacteristics([CBUUID(string: CHARACTERISTICS_READ_UUID),
                                                    CBUUID(string: CHARACTERISTICS_WRITE_UUID)], for: service)
            } else if service.uuid == CBUUID(string: DEVICE_INFORMATION_UUID) {
                peripheral.discoverCharacteristics([CBUUID(string: SYSTEM_ID_UUID)], for: service)
            }
        }
    }

    func peripheral(_ peripheral: CBPeripheral, didDiscoverCharacteristicsFor service: CBService, error: Error?) {
        guard error == nil else { return }
        for c in service.characteristics ?? [] {
            switch c.uuid {
            case CBUUID(string: CHARACTERISTICS_READ_UUID):
                notifyChar = c
            case CBUUID(string: CHARACTERISTICS_WRITE_UUID):
                writeChar = c
            case CBUUID(string: SYSTEM_ID_UUID):
                peripheral.readValue(for: c)
            default:
                break
            }
        }
        // 通知を張れたら(didUpdateNotificationState)端末の設定を読み始める
        if service.uuid == CBUUID(string: SERVICES_UUID), let c = notifyChar {
            peripheral.setNotifyValue(true, for: c)
        }
    }

    func peripheral(_ peripheral: CBPeripheral, didModifyServices invalidatedServices: [CBService]) {
        peripheral.discoverServices([CBUUID(string: SERVICES_UUID)])
    }

    func peripheral(_ peripheral: CBPeripheral, didUpdateNotificationStateFor characteristic: CBCharacteristic, error: Error?) {
        guard error == nil, characteristic.isNotifying else { return }
        send(0xA1)   // ADN_GET_DEV_INFO → 0x81 → GET_MODE → 0x83 → GET_6AXIS → 0x89 で準備完了
    }

    func peripheral(_ peripheral: CBPeripheral, didUpdateValueFor characteristic: CBCharacteristic, error: Error?) {
        guard error == nil, let value = characteristic.value else { return }
        if characteristic.uuid == CBUUID(string: SYSTEM_ID_UUID) {
            // System ID の 0..2 と 5..7 バイトが端末のアドレス(Mac 版と同じ)
            guard value.count >= 8 else { return }
            let b = [UInt8](value)
            macAddress = (Array(b[0..<3]) + Array(b[5..<8])).map { String(format: "%02X", $0) }.joined()
            return
        }
        for packet in MemePacket.split(value) { handle(packet) }
    }
}
