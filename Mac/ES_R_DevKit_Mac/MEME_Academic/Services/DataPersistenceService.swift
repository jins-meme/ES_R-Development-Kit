//
//  DataPersistenceService.swift
//  MEME_Academic
//
//  CSV ヘッダー生成・行整形・ファイル保存／移動を担当するサービス。
//

import Foundation
import AppKit
import UniformTypeIdentifiers

/// CSV／ソケットへ書く 1 行ぶん（registerPacket 済みのパケット）。
struct CsvRow {
    let data: AcademicData
    /// NUM 列
    let packetCount: Int
    /// DATE 列（UTC で書く）
    let date: Date
    /// Free Marking の印（ARTIFACT 列に X）
    let isFreeMarking: Bool
}

@MainActor
final class DataPersistenceService {

    private var csvManager = CsvManager()
    private var pendingCsvRows: [CsvRow] = []

    /// ファイル名の日時（UTC。DATE 列・Android 版 DevKit と揃える）
    private static let fileNameFormatter = utcFormatter("yyyyMMddHHmmss")
    /// DATE 列（UTC。Android 版 DevKit と同じ ES_R CSV フォーマット）
    private static let dateFormatter = utcFormatter("yyyy/MM/dd HH:mm:ss.SS")

    /// DATE 列の書き方(判定器の表の DATE も同じにする。DetectorOutputs)
    static func formatDate(_ date: Date) -> String { dateFormatter.string(from: date) }

    private static func utcFormatter(_ format: String) -> DateFormatter {
        let f = DateFormatter()
        f.locale = Locale(identifier: "en_US_POSIX")
        f.timeZone = TimeZone(secondsFromGMT: 0)
        f.dateFormat = format
        return f
    }

    // MARK: - Reset

    func reset() {
        csvManager = CsvManager()
        pendingCsvRows.removeAll()
    }

    func resetCsvManager() {
        csvManager.reset()
    }

    // MARK: - Buffer

    func append(_ row: CsvRow) {
        pendingCsvRows.append(row)
    }

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
                let dateString = Self.fileNameFormatter.string(from: Date())
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
        let m = csvMode(mode)
        var s = ""
        s += "// Data mode  : \(m.label)\n"
        s += "// Transmission speed  : \(MeasurementRange.hz(quality: transMode))Hz\n"
        s += "// Acceleration sensor's range  : \(MeasurementRange.accelG(device: accelRange))g\n"
        s += "// Gyroscope sensor's range  : \(MeasurementRange.gyroDps(device: gyroRange))dps\n"
        s += "//\n"
        s += "//" + (["ARTIFACT", "NUM", "DATE"] + m.columns).joined(separator: ",") + "\n"
        return s
    }

    /// CSV に書くモード(端末の値が分からなければ Quaternion として書く。以前からの扱い)
    private static func csvMode(_ mode: UInt32) -> MeasurementMode {
        MeasurementMode(rawValue: mode) ?? .quaternion
    }

    /// CSV/Socket 共通の1行整形ロジック。
    /// DATE列はUTCで書き出す（Android版 DevKit と同じ ES_R CSV フォーマット）。
    /// 表示側（チャートX軸）が Setting の "Convert displayed time to local time" に従って変換する。
    func dataToStoring(_ rows: [CsvRow], stringBuffer: inout String, mode: UInt32) {
        let m = Self.csvMode(mode)
        for row in rows {
            // モードと違う型のサンプル(切り替えの境目など)は書かない
            guard let values = m.values(of: row.data) else { continue }
            let mark = row.isFreeMarking ? "X" : ""
            stringBuffer += "\(mark),\(row.packetCount),\(Self.dateFormatter.string(from: row.date))"
            for v in values { stringBuffer += ",\(v)" }
            stringBuffer += "\n"
        }
    }

    // MARK: - File move (save dialog)

    /// moved: 移した後に (元の場所, 移した先) で呼ぶ(判定器の表の CSV も一緒に移すため)
    func presentSaveDialog(moved: @escaping @MainActor (URL, URL) -> Void = { _, _ in }) {
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
                    MainActor.assumeIsolated { moved(URL(fileURLWithPath: sourceFilePath), url) }
                } catch {
                    NSLog("コピー失敗:%@", error.localizedDescription)
                }
            }
        }
    }
}
