//
//  CsvReplayService.swift
//  MEME_Academic
//
//  File Replay 用のCSVパース／再生タイマーを担当するサービス。
//  DataPersistenceService.headerString() / dataToStoring() が書き出す
//  本アプリ形式のCSVのみを読み込み対象とする。
//

import Foundation

enum CsvReplayError: Error {
    case unreadable
    case invalidFormat
}

struct CsvReplayInfo {
    let fileName: String
    let mode: UInt32
    let transMode: UInt32
    let accelRange: UInt32
    let gyroRange: UInt32
    let rows: [AcademicData]
}

@MainActor
final class CsvReplayService {

    private var timer: Timer?
    private var rows: [AcademicData] = []
    private var rowIndex: Int = 0
    private var onRow: ((AcademicData) -> Void)?
    private var onFinished: (() -> Void)?

    // MARK: - Parse

    static func parse(url: URL) throws -> CsvReplayInfo {
        guard let content = try? String(contentsOf: url, encoding: .utf8) else {
            throw CsvReplayError.unreadable
        }
        let lines = content.components(separatedBy: .newlines)

        var modeStr: String?
        var transStr: String?
        var accelStr: String?
        var gyroStr: String?
        var dataStartIndex: Int?

        for (i, line) in lines.enumerated() {
            if line.hasPrefix("//ARTIFACT") {
                dataStartIndex = i + 1
                break
            } else if line.hasPrefix("// Data mode") {
                modeStr = valueAfterColon(line)
            } else if line.hasPrefix("// Transmission speed") {
                transStr = valueAfterColon(line)
            } else if line.hasPrefix("// Acceleration sensor's range") {
                accelStr = valueAfterColon(line)
            } else if line.hasPrefix("// Gyroscope sensor's range") {
                gyroStr = valueAfterColon(line)
            }
        }

        guard let dataStartIndex,
              let modeStr, let transStr, let accelStr, let gyroStr,
              let mode = mode(from: modeStr),
              let transMode = transMode(from: transStr),
              let accelRange = accelRange(from: accelStr),
              let gyroRange = gyroRange(from: gyroStr) else {
            throw CsvReplayError.invalidFormat
        }

        var rows: [AcademicData] = []
        for rawLine in lines[dataStartIndex...] {
            let line = rawLine.trimmingCharacters(in: .whitespaces)
            if line.isEmpty { continue }
            if let row = parseRow(line, mode: mode) {
                rows.append(row)
            }
        }
        guard !rows.isEmpty else { throw CsvReplayError.invalidFormat }

        return CsvReplayInfo(fileName: url.lastPathComponent,
                             mode: mode,
                             transMode: transMode,
                             accelRange: accelRange,
                             gyroRange: gyroRange,
                             rows: rows)
    }

    private static func valueAfterColon(_ line: String) -> String? {
        guard let range = line.range(of: ":", options: .backwards) else { return nil }
        let value = line[range.upperBound...].trimmingCharacters(in: .whitespaces)
        return value.isEmpty ? nil : value
    }

    private static func mode(from s: String) -> UInt32? {
        switch s {
        case "Standard": return MEMEMode_Standard
        case "Full": return MEMEMode_Full
        case "Quaternion": return MEMEMode_Quaternion
        default: return nil
        }
    }

    private static func transMode(from s: String) -> UInt32? {
        switch s {
        case "100Hz": return MEMEQuality_High
        case "50Hz": return MEMEQuality_Low
        default: return nil
        }
    }

    private static func accelRange(from s: String) -> UInt32? {
        switch s {
        case "2g": return MEMEAccelRange_2G
        case "4g": return MEMEAccelRange_4G
        case "8g": return MEMEAccelRange_8G
        case "16g": return MEMEAccelRange_16G
        default: return nil
        }
    }

    private static func gyroRange(from s: String) -> UInt32? {
        switch s {
        case "250dps": return MEMEGyroRange_250dps
        case "500dps": return MEMEGyroRange_500dps
        case "1000dps": return MEMEGyroRange_1000dps
        case "2000dps": return MEMEGyroRange_2000dps
        default: return nil
        }
    }

    /// "mark,packetCount,dateString,<mode固有フィールド...>" を1行分パースする。
    private static func parseRow(_ line: String, mode: UInt32) -> AcademicData? {
        let fields = line.components(separatedBy: ",")
        switch mode {
        case MEMEMode_Standard:
            guard fields.count >= 14 else { return nil }
            let d = AcademicStandardData()
            d.cnt = UInt32(fields[1]) ?? 0
            d.accX = Int16(fields[3]) ?? 0
            d.accY = Int16(fields[4]) ?? 0
            d.accZ = Int16(fields[5]) ?? 0
            d.eogL1 = Int16(fields[6]) ?? 0
            d.eogR1 = Int16(fields[7]) ?? 0
            d.eogL2 = Int16(fields[8]) ?? 0
            d.eogR2 = Int16(fields[9]) ?? 0
            d.eogH1 = Int16(fields[10]) ?? 0
            d.eogH2 = Int16(fields[11]) ?? 0
            d.eogV1 = Int16(fields[12]) ?? 0
            d.eogV2 = Int16(fields[13]) ?? 0
            return d
        case MEMEMode_Full:
            guard fields.count >= 13 else { return nil }
            let d = AcademicFullData()
            d.cnt = UInt32(fields[1]) ?? 0
            d.accX = Int16(fields[3]) ?? 0
            d.accY = Int16(fields[4]) ?? 0
            d.accZ = Int16(fields[5]) ?? 0
            d.gyroX = Int16(fields[6]) ?? 0
            d.gyroY = Int16(fields[7]) ?? 0
            d.gyroZ = Int16(fields[8]) ?? 0
            d.eogL = Int16(fields[9]) ?? 0
            d.eogR = Int16(fields[10]) ?? 0
            d.eogH = Int16(fields[11]) ?? 0
            d.eogV = Int16(fields[12]) ?? 0
            return d
        default:
            guard fields.count >= 7 else { return nil }
            let d = AcademicQuaternionData()
            d.cnt = UInt32(fields[1]) ?? 0
            d.quaternionW = Int64(fields[3]) ?? 0
            d.quaternionX = Int64(fields[4]) ?? 0
            d.quaternionY = Int64(fields[5]) ?? 0
            d.quaternionZ = Int64(fields[6]) ?? 0
            return d
        }
    }

    // MARK: - Playback

    /// Trans Speed（100Hz/50Hz）に対応する間隔で1行ずつ onRow を呼び出す。
    /// 最終行まで再生し終えたら onFinished を呼ぶ。
    func start(rows: [AcademicData],
              transMode: UInt32,
              onRow: @escaping (AcademicData) -> Void,
              onFinished: @escaping () -> Void) {
        stop()
        self.rows = rows
        rowIndex = 0
        self.onRow = onRow
        self.onFinished = onFinished
        let interval: TimeInterval = transMode == MEMEQuality_High ? 0.01 : 0.02
        timer = Timer.scheduledTimer(withTimeInterval: interval, repeats: true) { [weak self] _ in
            Task { @MainActor in
                self?.tick()
            }
        }
    }

    func stop() {
        timer?.invalidate()
        timer = nil
    }

    private func tick() {
        guard rowIndex < rows.count else {
            let finished = onFinished
            stop()
            onRow = nil
            onFinished = nil
            finished?()
            return
        }
        let row = rows[rowIndex]
        rowIndex += 1
        onRow?(row)
    }
}
