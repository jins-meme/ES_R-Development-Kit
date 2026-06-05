//
//  MockMEMELib_Academic.swift
//  MEME_Academic
//
//  実機 (CoreBluetooth) を使わずアプリ内でMEMEの動作を模擬するモック実装。
//  本プロジェクトでは Swift 側に DEBUG が定義されていないため #if DEBUG ガードは
//  かけず、起動引数 (-mock) でのみ呼び分けるようにしている。
//
//  状態遷移: idle → scanning → connecting → connected → (disconnect) → idle
//  実機の Scan / Connect / Disconnect / 再Scan の挙動に合わせている。
//

import Foundation

@MainActor
final class MockMEMELib_Academic: MEMELibInterface {

    weak var delegate: MEMELibAcademicDelegate?
    let memeVersion: Version
    var macAddress: String = "MOCK00000000"

    private enum State: String {
        case idle
        case scanning
        case connecting
        case connected
    }
    private var state: State = .idle

    /// scan / connect の遅延コールバック用。状態が変わるとキャンセルする。
    /// disconnect の通知は途中で潰されないよう、このプロパティでは管理しない。
    private var pendingTask: Task<Void, Never>?

    private var selectMode: UInt32 = MEMEMode_Standard
    private var transMode: UInt32 = MEMEQuality_High
    private var accelRange: UInt32 = MEMEAccelRange_2G
    private var gyroRange: UInt32 = MEMEGyroRange_250dps

    private var dataTimer: Timer?
    private var counter: UInt32 = 0
    private var phase: Double = 0.0

    private let mockDeviceName = "ESR_MOCK"
    private let mockUUID = "MOCK-0000-0000-0000-000000000001"
    private let battLvMock: UInt16 = 4

    init() {
        let version = Version()
        version.major = 99
        version.minor = 0
        version.revision = 0
        self.memeVersion = version
        NSLog("[MockMEMELib_Academic] initialized")
    }

    // MARK: - Scan / Connection

    @discardableResult
    func startScanningPeripherals() -> UInt32 {
        // idle または scanning からのみ受け付け（再スキャンも許可）
        guard state == .idle || state == .scanning else {
            NSLog("[Mock] startScanningPeripherals rejected (state=\(state.rawValue))")
            return MEMELIB_NG
        }
        transition(to: .scanning)
        scheduleScanCallback()
        return MEMELIB_OK
    }

    @discardableResult
    func stopScanningPeripherals() -> UInt32 {
        guard state == .scanning else {
            NSLog("[Mock] stopScanningPeripherals rejected (state=\(state.rawValue))")
            return MEMELIB_NG
        }
        transition(to: .idle)
        return MEMELIB_OK
    }

    @discardableResult
    func connectPeripheral(deviceName: String) -> UInt32 {
        guard state == .scanning else {
            NSLog("[Mock] connectPeripheral rejected (state=\(state.rawValue))")
            return MEMELIB_NG
        }
        guard deviceName == mockDeviceName else {
            NSLog("[Mock] connectPeripheral unknown device: \(deviceName)")
            return MEMELIB_NG
        }
        transition(to: .connecting)
        scheduleConnectCallback()
        return MEMELIB_OK
    }

    @discardableResult
    func disconnectPeripheral() -> UInt32 {
        guard state == .connected else {
            NSLog("[Mock] disconnectPeripheral rejected (state=\(state.rawValue))")
            return MEMELIB_NG
        }
        stopDataReportInternal()
        transition(to: .idle)
        // disconnect 通知は途中で潰されたくないので pendingTask とは別に発火する
        Task { @MainActor [weak self] in
            try? await Task.sleep(nanoseconds: 100_000_000)
            guard let self else { return }
            NSLog("[Mock] emit memePeripheralDisconnectedDelegate")
            self.delegate?.memePeripheralDisconnectedDelegate(result: MEMELIB_OK)
        }
        return MEMELIB_OK
    }

    // MARK: - State helpers

    private func transition(to newState: State) {
        NSLog("[Mock] state \(state.rawValue) -> \(newState.rawValue)")
        state = newState
        // 旧状態に紐づくコールバックは抑制する
        pendingTask?.cancel()
        pendingTask = nil
    }

    private func scheduleScanCallback() {
        pendingTask = Task { @MainActor [weak self] in
            try? await Task.sleep(nanoseconds: 300_000_000)
            if Task.isCancelled { return }
            guard let self, self.state == .scanning else { return }
            NSLog("[Mock] emit memePeripheralFoundDelegate: \(self.mockDeviceName)")
            self.delegate?.memePeripheralFoundDelegate(result: MEMELIB_OK,
                                                      deviceName: self.mockDeviceName,
                                                      uuid: self.mockUUID)
        }
    }

    private func scheduleConnectCallback() {
        pendingTask = Task { @MainActor [weak self] in
            try? await Task.sleep(nanoseconds: 300_000_000)
            if Task.isCancelled { return }
            guard let self, self.state == .connecting else { return }
            self.state = .connected
            NSLog("[Mock] emit memePeripheralConnectedDelegate (state=connected)")
            self.delegate?.memePeripheralConnectedDelegate(result: MEMELIB_OK)
        }
    }

    // MARK: - Mode / Range

    func getSelectMode() -> UInt32 { selectMode }
    @discardableResult
    func setSelectMode(_ mode: UInt32) -> UInt32 { selectMode = mode; return MEMELIB_OK }

    func getTransMode() -> UInt32 { transMode }
    @discardableResult
    func setTransMode(_ mode: UInt32) -> UInt32 { transMode = mode; return MEMELIB_OK }

    func getAccelRange() -> UInt32 { accelRange }
    @discardableResult
    func setAccelRange(_ range: UInt32) -> UInt32 { accelRange = range; return MEMELIB_OK }

    func getGyroRange() -> UInt32 { gyroRange }
    @discardableResult
    func setGyroRange(_ range: UInt32) -> UInt32 { gyroRange = range; return MEMELIB_OK }

    // MARK: - Data Report

    @discardableResult
    func startDataReport() -> UInt32 {
        guard state == .connected, dataTimer == nil else { return MEMELIB_NG }
        counter = 0
        phase = 0
        let interval: TimeInterval = (transMode == MEMEQuality_High) ? 0.01 : 0.02
        let timer = Timer.scheduledTimer(withTimeInterval: interval, repeats: true) { [weak self] _ in
            Task { @MainActor [weak self] in
                self?.tick()
            }
        }
        dataTimer = timer
        NSLog("[Mock] startDataReport (mode=\(selectMode), interval=\(interval))")
        return MEMELIB_OK
    }

    @discardableResult
    func stopDataReport() -> UInt32 {
        guard dataTimer != nil else { return MEMELIB_NG }
        stopDataReportInternal()
        return MEMELIB_OK
    }

    private func stopDataReportInternal() {
        dataTimer?.invalidate()
        dataTimer = nil
    }

    // MARK: - Data generation

    private func tick() {
        // 接続が切れたあと万一 Timer が走り続けた場合の安全網
        guard state == .connected else {
            stopDataReportInternal()
            return
        }
        counter = (counter + 1) & 0x0FFF
        phase += 0.05
        if phase > .pi * 2 { phase -= .pi * 2 }

        switch selectMode {
        case MEMEMode_Full:
            delegate?.memeAcademicFullDataReceivedDelegate(data: makeFullData())
        case MEMEMode_Quaternion:
            delegate?.memeAcademicQuaternionDataReceivedDelegate(data: makeQuaternionData())
        default:
            delegate?.memeAcademicStandardDataReceivedDelegate(data: makeStandardData())
        }
    }

    private func sinValue(_ amplitude: Double, freq: Double, phaseShift: Double = 0) -> Int16 {
        let v = amplitude * sin(phase * freq + phaseShift)
        return Int16(clamping: Int(v))
    }

    private func makeStandardData() -> AcademicStandardData {
        let d = AcademicStandardData()
        d.cnt = counter
        d.battLv = battLvMock
        d.accX = sinValue(800, freq: 1.0)
        d.accY = sinValue(800, freq: 1.0, phaseShift: .pi / 2)
        d.accZ = sinValue(800, freq: 0.7)
        d.eogL1 = sinValue(400, freq: 2.0)
        d.eogR1 = sinValue(400, freq: 2.0, phaseShift: 0.3)
        d.eogL2 = sinValue(400, freq: 2.5)
        d.eogR2 = sinValue(400, freq: 2.5, phaseShift: 0.3)
        d.eogH1 = d.eogL1 &- d.eogR1
        d.eogH2 = d.eogL2 &- d.eogR2
        let sum1 = Int32(d.eogL1) + Int32(d.eogR1)
        let sum2 = Int32(d.eogL2) + Int32(d.eogR2)
        d.eogV1 = Int16(truncatingIfNeeded: 0 - (sum1 / 2))
        d.eogV2 = Int16(truncatingIfNeeded: 0 - (sum2 / 2))
        return d
    }

    private func makeFullData() -> AcademicFullData {
        let d = AcademicFullData()
        d.cnt = counter
        d.battLv = battLvMock
        d.accX = sinValue(800, freq: 1.0)
        d.accY = sinValue(800, freq: 1.0, phaseShift: .pi / 2)
        d.accZ = sinValue(800, freq: 0.7)
        d.gyroX = sinValue(5000, freq: 1.5)
        d.gyroY = sinValue(5000, freq: 1.5, phaseShift: .pi / 2)
        d.gyroZ = sinValue(5000, freq: 1.3)
        d.eogL = sinValue(400, freq: 2.0)
        d.eogR = sinValue(400, freq: 2.0, phaseShift: 0.3)
        d.eogH = d.eogL &- d.eogR
        let sum = Int32(d.eogL) + Int32(d.eogR)
        d.eogV = Int16(truncatingIfNeeded: 0 - (sum / 2))
        return d
    }

    private func makeQuaternionData() -> AcademicQuaternionData {
        let d = AcademicQuaternionData()
        d.cnt = counter
        d.battLv = battLvMock
        let scale: Double = 1_073_741_824 // 2^30 程度の固定小数点表現を想定
        d.quaternionW = Int64(scale * cos(phase * 0.5))
        d.quaternionX = Int64(scale * sin(phase * 0.5))
        d.quaternionY = Int64(scale * sin(phase * 0.7))
        d.quaternionZ = Int64(scale * sin(phase * 0.9))
        return d
    }
}
