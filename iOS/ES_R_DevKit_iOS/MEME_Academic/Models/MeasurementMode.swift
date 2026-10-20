//
//  MeasurementMode.swift
//  MEME_Academic
//
//  計測モードごとの列と、計測条件(送信頻度・レンジ)の表。
//  CSV のヘッダと行、グラフ画面へ渡す列と値、左の欄の選択肢はすべてここを見る
//  (列を足す・並べ替えるときはここだけ直せば、CSV とグラフがずれない)。
//

import Foundation

/// 計測モード。rawValue は端末の MEMEMode_* と同じ。並び(allCases)は左の欄の選択肢の並び。
enum MeasurementMode: UInt32, CaseIterable {
    case standard = 1     // MEMEMode_Standard
    case full = 2         // MEMEMode_Full
    case quaternion = 3   // MEMEMode_Quaternion

    /// 左の欄の選択肢の番号から
    init(pickerIndex: Int) { self = Self.allCases[pickerIndex] }

    /// 左の欄の選択肢の番号
    var pickerIndex: Int { Self.allCases.firstIndex(of: self)! }

    /// 画面の選択肢と CSV ヘッダの "// Data mode" に出す名前
    var label: String {
        switch self {
        case .standard: return "Standard"
        case .full: return "Full"
        case .quaternion: return "Quaternion"
        }
    }

    /// グラフ画面とのやり取りで使う名前(webview/BRIDGE.md の mode)
    var pageName: String { label.lowercased() }

    init?(pageName: String) {
        guard let m = Self.allCases.first(where: { $0.pageName == pageName }) else { return nil }
        self = m
    }

    /// グラフ画面に波形を出すか(Quaternion はグラフを持たない)
    var hasGraph: Bool { self != .quaternion }

    /// 1 サンプルの列(CSV の列名。ARTIFACT・NUM・DATE より後ろ)。values(of:) と同じ並び。
    var columns: [String] {
        switch self {
        case .standard:
            return ["ACC_X", "ACC_Y", "ACC_Z", "EOG_L1", "EOG_R1", "EOG_L2", "EOG_R2", "EOG_H1", "EOG_H2", "EOG_V1", "EOG_V2"]
        case .full:
            return ["ACC_X", "ACC_Y", "ACC_Z", "GYRO_X", "GYRO_Y", "GYRO_Z", "EOG_L", "EOG_R", "EOG_H", "EOG_V"]
        case .quaternion:
            return ["QUATERNION_W", "QUATERNION_X", "QUATERNION_Y", "QUATERNION_Z"]
        }
    }

    /// サンプルの型から
    init?(of data: AcademicData) {
        switch data {
        case is AcademicStandardData: self = .standard
        case is AcademicFullData: self = .full
        case is AcademicQuaternionData: self = .quaternion
        default: return nil
        }
    }

    /// サンプルの値を columns の並びで。このモードのデータでなければ nil。
    func values(of data: AcademicData) -> [Int64]? {
        switch (self, data) {
        case (.standard, let d as AcademicStandardData):
            return [d.accX, d.accY, d.accZ, d.eogL1, d.eogR1, d.eogL2, d.eogR2, d.eogH1, d.eogH2, d.eogV1, d.eogV2].map { Int64($0) }
        case (.full, let d as AcademicFullData):
            return [d.accX, d.accY, d.accZ, d.gyroX, d.gyroY, d.gyroZ, d.eogL, d.eogR, d.eogH, d.eogV].map { Int64($0) }
        case (.quaternion, let d as AcademicQuaternionData):
            return [d.quaternionW, d.quaternionX, d.quaternionY, d.quaternionZ]
        default:
            return nil
        }
    }
}

/// 送信頻度・加速度レンジ・ジャイロレンジ。配列の番号は左の欄の選択肢の番号で、
/// レンジは端末の値(MEMEAccelRange_* / MEMEGyroRange_*)とも同じ。送信頻度は番号 + 1 が MEMEQuality_*。
enum MeasurementRange {
    static let transHz = [100, 50]
    static let accelG = [2, 4, 8, 16]
    static let gyroDps = [250, 500, 1000, 2000]

    /// 端末の送信頻度(MEMEQuality_*)→ Hz
    static func hz(quality: UInt32) -> Int { quality == MEMEQuality_High ? transHz[0] : transHz[1] }
    /// 端末の加速度レンジ → g(範囲外は端に寄せる)
    static func accelG(device range: UInt32) -> Int { accelG[min(Int(range), accelG.count - 1)] }
    /// 端末のジャイロレンジ → dps(範囲外は端に寄せる)
    static func gyroDps(device range: UInt32) -> Int { gyroDps[min(Int(range), gyroDps.count - 1)] }
}
