//
//  MEMELibInterface.swift
//  MEME_Academic
//
//  ViewController から利用する MEME ライブラリの公開面を抽象化したプロトコル。
//  実機実装 (MEMELib_Academic) とモック実装 (MockMEMELib_Academic) を
//  Scheme 切り替えで差し替えるための境界として用いる。
//

import Foundation

@MainActor
protocol MEMELibInterface: AnyObject {

    // MARK: - Properties
    var delegate: MEMELibAcademicDelegate? { get set }
    var memeVersion: Version { get }

    // MARK: - Scan / Connection
    @discardableResult func startScanningPeripherals() -> UInt32
    @discardableResult func stopScanningPeripherals() -> UInt32
    @discardableResult func connectPeripheral(deviceName: String) -> UInt32
    @discardableResult func disconnectPeripheral() -> UInt32

    // MARK: - Mode / Range
    func getSelectMode() -> UInt32
    @discardableResult func setSelectMode(mode: UInt32) -> UInt32
    func getTransMode() -> UInt32
    @discardableResult func setTransMode(mode: UInt32) -> UInt32
    func getAccelRange() -> UInt32
    @discardableResult func setAccelRange(range: UInt32) -> UInt32
    func getGyroRange() -> UInt32
    @discardableResult func setGyroRange(range: UInt32) -> UInt32

    // MARK: - Data Report
    @discardableResult func startDataReport() -> UInt32
    @discardableResult func stopDataReport() -> UInt32
}
