//
//  UserSetting.swift
//  MEME_Academic
//
//  Created by Celleus on 2022/09/14.
//  Copyright © 2022 jins-jp. All rights reserved.
//

import Foundation

class UserSetting: NSObject {

    class func fristSetting() {
        if UserDefaults.standard.object(forKey: kConst_LocalPort) == nil {
            NSLog("初期設定開始")
            defaultSetting()
        } else {
            NSLog("初期設定済み")
        }
        migrateSaveFilePath()
    }

    /// 以前の初期値は "file:///…" の URL 文字列で、保存先のパスとして使うと
    /// ルート直下の "/file:/…" を作ろうとして失敗し、CSV が保存されなかった。パスに直して書き戻す。
    private class func migrateSaveFilePath() {
        let value = getSaveFilePath()
        guard value.hasPrefix("file://") else { return }
        let path = URL(string: value)?.path ?? String(value.dropFirst("file://".count))
        NSLog("SaveFilePath を URL からパスへ: %@ -> %@", value, path)
        setSaveFilePath(path)
    }

    class func defaultSetting() {
        createDefaultSaveDirectory()
        let userDefaults = UserDefaults.standard
        userDefaults.set(0.0, forKey: kConst_X_Axis)
        userDefaults.set(0.0, forKey: kConst_Y_Axis)
        userDefaults.set(0.0, forKey: kConst_Z_Axis)
        userDefaults.set(false, forKey: kConst_ShowSaveFileDialog)
        userDefaults.set(false, forKey: kConst_ExtermalOutputSocket)
        userDefaults.set("88", forKey: kConst_LocalPort)
        userDefaults.set(true, forKey: kConst_ConvertToLocalTime)
        userDefaults.set(true, forKey: kConst_CompressSaveFile)
    }

    private class func createDefaultSaveDirectory() {
        let paths = NSSearchPathForDirectoriesInDomains(.documentDirectory, .userDomainMask, true)
        guard let documentsDirectory = paths.first else { return }
        let defaultDirectory = (documentsDirectory as NSString).appendingPathComponent("/JINS/MEME_Academic")
        NSLog("defaultDirectory:%@", defaultDirectory)
        let userDefaults = UserDefaults.standard
        if !FileManager.default.isExecutableFile(atPath: defaultDirectory) {
            NSLog("ディレクトリがないので作成")
            do {
                try FileManager.default.createDirectory(atPath: defaultDirectory, withIntermediateDirectories: true, attributes: nil)
                NSLog("ディレクトリ作成 成功")
                userDefaults.set(defaultDirectory, forKey: kConst_SaveFilePath)
            } catch {
                NSLog("ディレクトリ作成 失敗")
                userDefaults.set("", forKey: kConst_SaveFilePath)
            }
        } else {
            NSLog("既にディレクトリがある")
            userDefaults.set(defaultDirectory, forKey: kConst_SaveFilePath)
        }
    }

    // MARK: - Setting

    class func setSaveFilePath(_ value: Any?) {
        UserDefaults.standard.set(value, forKey: kConst_SaveFilePath)
    }
    class func getSaveFilePath() -> String {
        return UserDefaults.standard.object(forKey: kConst_SaveFilePath) as? String ?? ""
    }

    class func setXAxis(_ value: Double) {
        UserDefaults.standard.set(value, forKey: kConst_X_Axis)
    }
    class func getXAxis() -> Double {
        return UserDefaults.standard.double(forKey: kConst_X_Axis)
    }

    class func setYAxis(_ value: Double) {
        UserDefaults.standard.set(value, forKey: kConst_Y_Axis)
    }
    class func getYAxis() -> Double {
        return UserDefaults.standard.double(forKey: kConst_Y_Axis)
    }

    class func setZAxis(_ value: Double) {
        UserDefaults.standard.set(value, forKey: kConst_Z_Axis)
    }
    class func getZAxis() -> Double {
        return UserDefaults.standard.double(forKey: kConst_Z_Axis)
    }

    class func setShowSaveFileDialog(_ value: Bool) {
        UserDefaults.standard.set(value, forKey: kConst_ShowSaveFileDialog)
    }
    class func getShowSaveFileDialog() -> Bool {
        return UserDefaults.standard.bool(forKey: kConst_ShowSaveFileDialog)
    }

    class func setExtermalOutputSocket(_ value: Bool) {
        UserDefaults.standard.set(value, forKey: kConst_ExtermalOutputSocket)
    }
    class func getExtermalOutputSocket() -> Bool {
        return UserDefaults.standard.bool(forKey: kConst_ExtermalOutputSocket)
    }

    /// 時刻表示をローカルタイムへ変換するか（既定：ON）。
    /// CSV／ソケットへ記録する時刻は常にUTCで、この設定は表示（チャートのX軸ラベル）にのみ効く。
    class func setConvertToLocalTime(_ value: Bool) {
        UserDefaults.standard.set(value, forKey: kConst_ConvertToLocalTime)
    }
    class func getConvertToLocalTime() -> Bool {
        // 未設定（この設定より前から使っているユーザー）は ON 扱いにする。
        // bool(forKey:) は未設定でも false を返すため、object(forKey:) で有無を判定する。
        guard let value = UserDefaults.standard.object(forKey: kConst_ConvertToLocalTime) as? Bool else {
            return true
        }
        return value
    }

    /// 計測データを gz 圧縮して保存するか（既定：ON）。
    /// ON なら ".csv.gz"、OFF なら従来どおり ".csv" で保存する。
    /// 読み込み（File Replay / Finder からの「開く」）は設定に関係なく両方を受け付ける。
    class func setCompressSaveFile(_ value: Bool) {
        UserDefaults.standard.set(value, forKey: kConst_CompressSaveFile)
    }
    class func getCompressSaveFile() -> Bool {
        // 未設定（この設定より前から使っているユーザー）は ON 扱いにする。
        // bool(forKey:) は未設定でも false を返すため、object(forKey:) で有無を判定する。
        guard let value = UserDefaults.standard.object(forKey: kConst_CompressSaveFile) as? Bool else {
            return true
        }
        return value
    }

    class func setLocalPort(_ value: Any?) {
        UserDefaults.standard.set(value, forKey: kConst_LocalPort)
    }
    class func getLocalPort() -> String {
        return UserDefaults.standard.object(forKey: kConst_LocalPort) as? String ?? ""
    }

    /// グラフ画面(WebView)で有効な中身の ID。"standard"(同梱の標準版、既定)か、取り込んだ zip の ID。WebContentStore 参照。
    class func setWebContentActive(_ value: String) {
        UserDefaults.standard.set(value, forKey: kConst_WebContentActive)
    }
    class func getWebContentActive() -> String? {
        return UserDefaults.standard.string(forKey: kConst_WebContentActive)
    }

    /// 旧形式(1.5.0 build 35 まで): "bundled" / "custom"(選んだ zip を 1 つだけ持てた)。WebContentStore が新形式へ移すときだけ読む。
    class func takeLegacyWebContentSource() -> String? {
        defer { UserDefaults.standard.removeObject(forKey: kConst_WebContentSource) }
        return UserDefaults.standard.string(forKey: kConst_WebContentSource)
    }
}
