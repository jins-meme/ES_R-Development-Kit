//
//  DataPersistenceService.swift
//  MEME_Academic
//
//  CSV ヘッダー生成・行整形・ファイル保存／移動を担当するサービス。
//

import Foundation
import AppKit
import UniformTypeIdentifiers

@MainActor
final class DataPersistenceService {

    private var csvManager = CsvManager()
    private var pendingCsvRows: [[String: Any]] = []

    // MARK: - Reset

    func reset() {
        csvManager = CsvManager()
        pendingCsvRows.removeAll()
    }

    func resetCsvManager() {
        csvManager.reset()
    }

    // MARK: - Buffer

    func append(_ row: [String: Any]) {
        pendingCsvRows.append(row)
    }

    var lastRow: [String: Any]? { pendingCsvRows.last }

    /// 現在書き出し中のCSVファイルURL。まだ1件も書き出していなければ nil。
    /// 計測停止時に、タップで付けた Artifact を確定済みファイルへ書き戻すために使う。
    var savedFileURL: URL? {
        guard let path = csvManager.saveFilePath, !path.isEmpty else { return nil }
        return URL(fileURLWithPath: path)
    }

    // MARK: - Save trigger

    /// バッファ件数が閾値を超える or force=true のとき書き出す。
    /// 初回書き込み時は header を含めたうえで保存する。
    func saveIfNeeded(force: Bool,
                      macAddress: String,
                      quality: Int,
                      mode: UInt32,
                      header: @autoclosure () -> String) {
        if pendingCsvRows.isEmpty { return }
        if force || pendingCsvRows.count >= 100 / quality {
            if !csvManager.isSave {
                let directoryPath = UserSetting.getSaveFilePath()
                // ファイル名の日時もUTC（DATE列・Android版 DevKit と揃える）。
                let formatter = DateFormatter()
                formatter.locale = Locale(identifier: "en_US_POSIX")
                formatter.timeZone = TimeZone(secondsFromGMT: 0)
                formatter.dateFormat = "yyyyMMddHHmmss"
                let dateString = formatter.string(from: Date())
                // 拡張子で圧縮の有無が決まる（CsvManager が .csv.gz なら gzip で書く）。
                // 読み込み側は設定に関係なく .csv / .csv.gz の両方を受け付ける。
                let ext = CsvFile.saveExtension(compressed: UserSetting.getCompressSaveFile())
                let fileName = "\(macAddress)_\(dateString).\(ext)"
                var buffer = header()
                dataToStoring(pendingCsvRows, stringBuffer: &buffer, mode: mode)
                if let data = buffer.data(using: .utf8) {
                    csvManager.create(directoryPath: directoryPath, fileName: fileName, firstData: data)
                }
            } else {
                var buffer = ""
                dataToStoring(pendingCsvRows, stringBuffer: &buffer, mode: mode)
                if let data = buffer.data(using: .utf8) {
                    csvManager.append(data)
                }
            }
            pendingCsvRows.removeAll()
        }
    }

    // MARK: - Header / row formatting

    /// 計測パラメータから CSV ヘッダ文字列を生成する。
    static func headerString(mode: UInt32, transMode: UInt32, accelRange: UInt32, gyroRange: UInt32) -> String {
        let selectModeStr: String = {
            switch mode {
            case MEMEMode_Standard: return "Standard"
            case MEMEMode_Full: return "Full"
            default: return "Quaternion"
            }
        }()
        let transModeStr = transMode == MEMEQuality_High ? "100Hz" : "50Hz"
        let accelRangeStr: String = {
            switch accelRange {
            case MEMEAccelRange_2G: return "2g"
            case MEMEAccelRange_4G: return "4g"
            case MEMEAccelRange_8G: return "8g"
            default: return "16g"
            }
        }()
        let gyroRangeStr: String = {
            switch gyroRange {
            case MEMEGyroRange_250dps: return "250dps"
            case MEMEGyroRange_500dps: return "500dps"
            case MEMEGyroRange_1000dps: return "1000dps"
            default: return "2000dps"
            }
        }()
        var s = ""
        s += "// Data mode  : \(selectModeStr)\n"
        s += "// Transmission speed  : \(transModeStr)\n"
        s += "// Acceleration sensor's range  : \(accelRangeStr)\n"
        s += "// Gyroscope sensor's range  : \(gyroRangeStr)\n"
        s += "//\n"
        switch mode {
        case MEMEMode_Standard:
            s += "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,EOG_L1,EOG_R1,EOG_L2,EOG_R2,EOG_H1,EOG_H2,EOG_V1,EOG_V2\n"
        case MEMEMode_Full:
            s += "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V\n"
        default:
            s += "//ARTIFACT,NUM,DATE,QUATERNION_W,QUATERNION_X,QUATERNION_Y,QUATERNION_Z\n"
        }
        return s
    }

    /// CSV/Socket 共通の1行整形ロジック。
    /// DATE列はUTCで書き出す（Android版 DevKit と同じ ES_R CSV フォーマット）。
    /// 表示側（チャートX軸）が Setting の "Convert displayed time to local time" に従って変換する。
    func dataToStoring(_ datas: [[String: Any]], stringBuffer: inout String, mode: UInt32) {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.timeZone = TimeZone(secondsFromGMT: 0)
        formatter.dateFormat = "yyyy/MM/dd HH:mm:ss.SS"
        for dic in datas {
            let date = dic["date"] as? Date ?? Date()
            let dateString = formatter.string(from: date)
            let packetCount = (dic["packetCount"] as? NSNumber)?.intValue ?? 0
            let isFreeMarkingValue = (dic["isFreeMarking"] as? NSNumber)?.boolValue ?? false
            let mark = isFreeMarkingValue ? "x" : ""
            switch mode {
            case MEMEMode_Standard:
                if let d = dic["data"] as? AcademicStandardData {
                    stringBuffer += "\(mark),\(packetCount),\(dateString),\(d.accX),\(d.accY),\(d.accZ),\(d.eogL1),\(d.eogR1),\(d.eogL2),\(d.eogR2),\(d.eogH1),\(d.eogH2),\(d.eogV1),\(d.eogV2)\n"
                }
            case MEMEMode_Full:
                if let d = dic["data"] as? AcademicFullData {
                    stringBuffer += "\(mark),\(packetCount),\(dateString),\(d.accX),\(d.accY),\(d.accZ),\(d.gyroX),\(d.gyroY),\(d.gyroZ),\(d.eogL),\(d.eogR),\(d.eogH),\(d.eogV)\n"
                }
            default:
                if let d = dic["data"] as? AcademicQuaternionData {
                    stringBuffer += "\(mark),\(packetCount),\(dateString),\(d.quaternionW),\(d.quaternionX),\(d.quaternionY),\(d.quaternionZ)\n"
                }
            }
        }
    }

    // MARK: - File move (save dialog)

    func presentSaveDialog() {
        guard let sourceFilePath = csvManager.saveFilePath, !sourceFilePath.isEmpty else {
            csvManager.reset()
            return
        }
        let saveFileName = csvManager.saveFileName ?? ""

        let savePanel = NSSavePanel()
        savePanel.canCreateDirectories = true
        savePanel.showsTagField = false
        savePanel.isExtensionHidden = false
        if let type = CsvFile.contentType(forFileName: saveFileName) {
            savePanel.allowedContentTypes = [type]
        }
        savePanel.nameFieldStringValue = saveFileName
        savePanel.level = .modalPanel

        guard let window = NSApp.mainWindow else { return }
        savePanel.beginSheetModal(for: window) { result in
            if result == .OK, let url = savePanel.url {
                do {
                    try FileManager.default.copyItem(at: URL(fileURLWithPath: sourceFilePath), to: url)
                    try? FileManager.default.removeItem(at: URL(fileURLWithPath: sourceFilePath))
                } catch {
                    NSLog("コピー失敗:%@", error.localizedDescription)
                }
            }
        }
    }
}
