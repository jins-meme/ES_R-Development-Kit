//
//  MEMELibInterface.swift
//  MEME_Academic (iOS)
//
//  ViewModel から使う MEME ライブラリの公開面。実機実装 (MEMELib_Academic) とモック実装 (MockMEMELib_Academic) を
//  起動引数 -mock で差し替える境界(Mac 版と同じ考え方。iOS 版は再接続・初期化・切断理由を足してある)。
//

import Foundation

@MainActor
protocol MEMELibInterface: AnyObject {

    // MARK: - Properties
    var delegate: MEMELibAcademicDelegate? { get set }
    var memeVersion: Version { get }
    /// 端末のアドレス(Device Information の System ID から。読めなければ空)
    var macAddress: String { get }
    /// Bluetooth が使える状態か
    var isPoweredOn: Bool { get }

    // MARK: - Scan / Connection
    @discardableResult func startScanningPeripherals() -> UInt32
    @discardableResult func stopScanningPeripherals() -> UInt32
    @discardableResult func connectPeripheral(deviceName: String) -> UInt32
    /// 最後に繋いだ端末へ繋ぎ直す。iOS の接続要求は期限が無いので、端末が戻ってくるまで待ち続ける(disconnectPeripheral で取り消す)
    @discardableResult func reconnectPeripheral() -> UInt32
    /// 接続を切る(接続待ちも取り消す)
    @discardableResult func disconnectPeripheral() -> UInt32

    // MARK: - Mode / Range
    func getSelectMode() -> UInt32
    func getTransMode() -> UInt32
    func getAccelRange() -> UInt32
    func getGyroRange() -> UInt32
    /// 計測条件を端末へ送る(ADN_SET_MODE と ADN_SET_6AXIS_PARAMS)
    func applySettings(mode: UInt32, trans: UInt32, accel: UInt32, gyro: UInt32)

    // MARK: - Data Report
    @discardableResult func startDataReport() -> UInt32
    @discardableResult func stopDataReport() -> UInt32

    // MARK: - Parameters
    /// 端末のパラメータを初期化する(ADN_CLR_PARAMS)。completion は ACK が来たか
    func initializeParams(completion: @escaping (Bool) -> Void)

    // MARK: - Shelf mode
    /// 端末を Shelf mode（保管モード）へ移行させる。
    /// completion(true) は「SHELF コマンドを送信した」まで。移行の成否は、その後に
    /// 端末側から切断が来るかどうかで判断する（受理されると端末が自ら切断する）。
    func enterShelfMode(completion: @escaping (Bool) -> Void)
}

// MARK: - Delegate Protocol
@MainActor
protocol MEMELibAcademicDelegate: AnyObject {
    /// Bluetooth の状態が変わった
    func memeBluetoothStateDelegate(poweredOn: Bool)
    func memePeripheralFoundDelegate(result: UInt32, deviceName: String?, uuid: String?)
    func memePeripheralConnectedDelegate(result: UInt32)
    /// 切断された。reason は切り分け用の名前(CONNECTION_TIMEOUT = 電波が届かない、REMOTE_USER_TERMINATED = 端末側から切れた …)
    func memePeripheralDisconnectedDelegate(result: UInt32, reason: String)
    func memeAcademicStandardDataReceivedDelegate(data: AcademicStandardData)
    func memeAcademicFullDataReceivedDelegate(data: AcademicFullData)
    func memeAcademicQuaternionDataReceivedDelegate(data: AcademicQuaternionData)
}
