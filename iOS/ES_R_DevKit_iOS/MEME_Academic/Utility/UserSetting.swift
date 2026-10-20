//
//  UserSetting.swift
//  MEME_Academic (iOS)
//
//  UserDefaults に置く設定。項目は Android2 の設定ダイアログ(SettingsStore.kt)に合わせ、
//  Mac 版にしかない保存先・保存ダイアログ・TCP 出力・加速度オフセットは持たない。
//  未設定のときの既定値は各 get の中で決める(bool(forKey:) は未設定でも false を返すため object(forKey:) で見る)。
//

import Foundation

enum UserSetting {

    private static let kSelectMode = "SelectMode"
    private static let kTransSpeed = "TransSpeed"
    private static let kAccelRange = "AccelRange"
    private static let kGyroRange = "GyroRange"
    private static let kAutoConnect = "AutoConnect"
    private static let kReconnect = "Reconnect"
    private static let kOpenSharing = "OpenSharingOnComplete"
    private static let kCompressSaveFile = "CompressSaveFile"
    private static let kConvertToLocalTime = "ConvertToLocalTime"
    /// グラフ画面(WebView)で有効な中身の ID: "standard"(同梱の標準版)/ 取り込んだ zip の ID。WebContentStore 参照。
    private static let kWebContentActive = "WebContentActive"

    private static var d: UserDefaults { .standard }

    private static func bool(_ key: String, default value: Bool) -> Bool {
        d.object(forKey: key) as? Bool ?? value
    }

    // MARK: - 計測条件(選択肢の番号。MEMEViewModel の *Options の並び)

    static var selectMode: Int {
        get { d.object(forKey: kSelectMode) as? Int ?? MeasurementMode.standard.pickerIndex }
        set { d.set(newValue, forKey: kSelectMode) }
    }
    static var transSpeed: Int {
        get { d.integer(forKey: kTransSpeed) }
        set { d.set(newValue, forKey: kTransSpeed) }
    }
    static var accelRange: Int {
        get { d.integer(forKey: kAccelRange) }
        set { d.set(newValue, forKey: kAccelRange) }
    }
    static var gyroRange: Int {
        get { d.integer(forKey: kGyroRange) }
        set { d.set(newValue, forKey: kGyroRange) }
    }

    // MARK: - 動作

    /// 見つかったら自動で繋ぐ(既定 OFF)
    static var autoConnect: Bool {
        get { bool(kAutoConnect, default: false) }
        set { d.set(newValue, forKey: kAutoConnect) }
    }

    /// 計測中に BLE が切れたら繋ぎ直して計測を続ける(既定 ON。OFF だと 1 回切れただけで長時間計測が終わる。Android と同じ)
    static var reconnect: Bool {
        get { bool(kReconnect, default: true) }
        set { d.set(newValue, forKey: kReconnect) }
    }

    /// 計測を止めたら共有シートを開く(既定 OFF)
    static var openSharingOnComplete: Bool {
        get { bool(kOpenSharing, default: false) }
        set { d.set(newValue, forKey: kOpenSharing) }
    }

    /// 計測データを gz 圧縮して保存するか(既定 ON)。ON なら ".csv.gz"、OFF なら ".csv"。
    static var compressSaveFile: Bool {
        get { bool(kCompressSaveFile, default: true) }
        set { d.set(newValue, forKey: kCompressSaveFile) }
    }

    /// グラフの時刻を端末の時刻帯で出すか(既定 ON)。CSV の DATE 列は常に UTC。
    static var convertToLocalTime: Bool {
        get { bool(kConvertToLocalTime, default: true) }
        set { d.set(newValue, forKey: kConvertToLocalTime) }
    }

    // MARK: - グラフ画面の中身(WebContentStore)

    static func setWebContentActive(_ value: String) {
        d.set(value, forKey: kWebContentActive)
    }
    static func getWebContentActive() -> String? {
        d.string(forKey: kWebContentActive)
    }
}
