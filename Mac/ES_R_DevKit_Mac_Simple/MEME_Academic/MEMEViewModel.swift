//
//  MEMEViewModel.swift
//  MEME_Academic
//
//  SwiftUI 用 ViewModel。
//  MEMELibAcademicDelegate を受けて UI 状態を保持する。
//

import Foundation
import Observation

@MainActor
@Observable
final class MEMEViewModel {

    // MARK: - Phase

    /// 画面状態（ボタンの出し分けに使用）。
    /// idle → deviceFound → connected → measuring の流れ。
    enum Phase {
        case idle
        case deviceFound
        case connected
        case measuring
    }

    // MARK: - Observable State

    var phase: Phase = .idle
    var foundDevices: [String] = []
    var selectedDevice: String = ""
    var connectionStateText: String = "State : Disconnected"

    var selectMode: Int = 0
    var transSpeed: Int = 0
    var accelRange: Int = 0
    var gyroRange: Int = 0

    var latestData: AcademicFullData = AcademicFullData()

    // MARK: - Static option lists

    let selectModeOptions = ["Full"]
    let transSpeedOptions = ["100Hz"]
    let accelRangeOptions = ["±2G", "±4G", "±8G", "±16G"]
    let gyroRangeOptions = ["±250dps", "±500dps", "±1000dps", "±2000dps"]

    // MARK: - Private

    private let memelib: any MEMELibInterface

    // MARK: - Init

    init() {
        self.memelib = MEMELibFactory.make()
        self.memelib.delegate = self
    }

    // MARK: - Actions

    func startScan() {
        print("Call : startScanningPeripherals")
        foundDevices.removeAll()
        selectedDevice = ""
        _ = memelib.startScanningPeripherals()
    }

    func toggleConnect() {
        if phase == .connected {
            print("Call : disconnectPeripheral")
            _ = memelib.disconnectPeripheral()
        } else {
            guard !selectedDevice.isEmpty else { return }
            print("Call : connectPeripheral")
            _ = memelib.connectPeripheral(deviceName: selectedDevice)
        }
    }

    func toggleMeasurement() {
        if phase == .measuring {
            _ = memelib.stopDataReport()
            phase = .connected
        } else {
            _ = memelib.setSelectMode(MEMEMode_Full)
            _ = memelib.setTransMode(MEMEQuality_High)
            _ = memelib.setAccelRange(UInt32(accelRange))
            _ = memelib.setGyroRange(UInt32(gyroRange))
            _ = memelib.startDataReport()
            phase = .measuring
        }
    }
}

// =============================================================================
// MARK: - MEMELibAcademicDelegate
// =============================================================================
extension MEMEViewModel: MEMELibAcademicDelegate {

    func memePeripheralFoundDelegate(result: UInt32, deviceName: String?, uuid: String?) {
        if result == MEMELIB_OK {
            print("memePeripheralFoundDelegate \(result) \(deviceName ?? "") \(uuid ?? "")")
            if let name = deviceName {
                foundDevices.append(name)
                selectedDevice = name
            }
            phase = .deviceFound
        } else {
            print("memePeripheralFoundDelegate \(result)")
            print("Call : stopScanningPeripherals")
            _ = memelib.stopScanningPeripherals()
        }
    }

    func memePeripheralConnectedDelegate(result: UInt32) {
        print("memePeripheralConnectedDelegate : \(result)")
        connectionStateText = "State : Connected"
        phase = .connected
        syncDeviceSettings()
    }

    func memePeripheralDisconnectedDelegate(result: UInt32) {
        print("memePeripheralDisconnectedDelegate : \(result)")
        connectionStateText = "State : Disconnected"
        phase = .idle
    }

    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData) {
        latestData = data
    }

    /// AUP_REPORT_MODE / AUP_REPORT_6AXIS_PRMS で取得した
    /// デバイス側の現在値を各セレクタに反映する。
    private func syncDeviceSettings() {
        let modeIdx = Int(memelib.getSelectMode()) - 1   // 1=Full
        if selectModeOptions.indices.contains(modeIdx) {
            selectMode = modeIdx
        }
        let transIdx = Int(memelib.getTransMode()) - 1   // 1=High(100Hz)
        if transSpeedOptions.indices.contains(transIdx) {
            transSpeed = transIdx
        }
        let accelIdx = Int(memelib.getAccelRange())      // 0..3
        if accelRangeOptions.indices.contains(accelIdx) {
            accelRange = accelIdx
        }
        let gyroIdx = Int(memelib.getGyroRange())        // 0..3
        if gyroRangeOptions.indices.contains(gyroIdx) {
            gyroRange = gyroIdx
        }
    }
}
