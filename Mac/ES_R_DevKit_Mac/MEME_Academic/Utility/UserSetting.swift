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
                userDefaults.set("file://\(defaultDirectory)", forKey: kConst_SaveFilePath)
            } catch {
                NSLog("ディレクトリ作成 失敗")
                userDefaults.set("", forKey: kConst_SaveFilePath)
            }
        } else {
            NSLog("既にディレクトリがある")
            userDefaults.set("file://\(defaultDirectory)", forKey: kConst_SaveFilePath)
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

    class func setLocalPort(_ value: Any?) {
        UserDefaults.standard.set(value, forKey: kConst_LocalPort)
    }
    class func getLocalPort() -> String {
        return UserDefaults.standard.object(forKey: kConst_LocalPort) as? String ?? ""
    }
}
