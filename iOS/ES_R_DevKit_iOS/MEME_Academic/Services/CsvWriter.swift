//
//  CsvWriter.swift
//  MEME_Academic (iOS)
//
//  計測中の CSV を Documents へ書く(ファイル App の「このiPhone内 > ES_R DevKit」から見える。Info.plist の
//  UIFileSharingEnabled / LSSupportsOpeningDocumentsInPlace)。形式は Mac・Android と同じ ES_R CSV。
//
//  - ファイル名は "<端末のアドレス>_<UTC の日時>.csv.gz"(設定で .csv)。最初のデータ行が来たときに作る(1 行も無ければ作らない)。
//  - 100 行ごとに追記する。gz は追記ごとに 1 メンバー(CsvManager)なので、アプリが落ちてもそこまでは読める。
//  - 切断ログ "<ベース名>_disconnect.csv": 計測中に切れた時刻・理由・直前の電池残量を 1 行ずつ足す(Android の CsvWriter と同じ)。
//

import Foundation

/// CSV に書く 1 行ぶん(SampleCounter.countUp が true を返したパケット)
struct CsvRow {
    /// 値(MeasurementMode.columns の並び)
    let values: [Int64]
    /// NUM 列
    let num: Int
    /// DATE 列(UTC で書く)
    let date: Date
    /// Free Marking の印(ARTIFACT 列に X)
    let isFreeMarking: Bool
}

@MainActor
final class CsvWriter {

    private let csvManager = CsvManager()
    private var pending: [CsvRow] = []
    private var baseName: String?
    private var header = ""
    private var compress = true
    private var disconnectFile: URL?

    /// 今の計測でファイルへ書いた行数
    private(set) var recordedRows = 0
    /// 今の計測の行数(まだファイルへ書いていない行を含む)
    var rowCount: Int { recordedRows + pending.count }

    private static let fileNameFormatter = utcFormatter("yyyyMMddHHmmss")
    /// DATE 列(UTC。Android 版と同じミリ秒まで)
    private static let dateFormatter = utcFormatter("yyyy/MM/dd HH:mm:ss.SSS")

    /// DATE 列の書き方(判定器の表の DATE も同じにする。DetectorOutputs)
    static func formatDate(_ date: Date) -> String { dateFormatter.string(from: date) }

    private static func utcFormatter(_ format: String) -> DateFormatter {
        let f = DateFormatter()
        f.locale = Locale(identifier: "en_US_POSIX")
        f.timeZone = TimeZone(secondsFromGMT: 0)
        f.dateFormat = format
        return f
    }

    /// CSV の置き場(Documents)
    static var directory: URL {
        FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
    }

    /// 今の計測のデータ CSV(最初の 100 行を書くまでは nil)
    var savedFileURL: URL? {
        guard let path = csvManager.saveFilePath, !path.isEmpty else { return nil }
        return URL(fileURLWithPath: path)
    }

    /// 計測を始める。圧縮するかはここで決め、計測中に設定を変えても書きかけのファイルの形式は変えない
    func start(address: String, mode: MeasurementMode, transMode: UInt32, accelRange: UInt32, gyroRange: UInt32, compress: Bool) {
        csvManager.reset()
        pending.removeAll()
        recordedRows = 0
        disconnectFile = nil
        self.compress = compress
        let safe = address.isEmpty ? "ESR" : address.replacingOccurrences(of: ":", with: "")
        baseName = "\(safe)_\(Self.fileNameFormatter.string(from: Date()))"
        header = Self.headerString(mode: mode, transMode: transMode, accelRange: accelRange, gyroRange: gyroRange)
    }

    func append(_ row: CsvRow) {
        guard baseName != nil else { return }
        pending.append(row)
        if pending.count >= 100 { flush() }
    }

    /// 溜まった行を書く。最初の書き出しでファイルを作り、ヘッダを付ける
    func flush() {
        guard let base = baseName, !pending.isEmpty else { return }
        var text = ""
        for row in pending {
            text += row.isFreeMarking ? "X" : ""
            text += ",\(row.num),\(Self.formatDate(row.date))"
            for v in row.values { text += ",\(v)" }
            text += "\n"
        }
        recordedRows += pending.count
        pending.removeAll()
        if csvManager.isSave {
            csvManager.append(Data(text.utf8))
        } else {
            let name = "\(base).\(CsvFile.saveExtension(compressed: compress))"
            csvManager.create(directoryPath: Self.directory.path, fileName: name, firstData: Data((header + text).utf8))
        }
    }

    /// 切断の時刻・理由・直前の電池残量(0〜5、分からなければ -1)を "<ベース名>_disconnect.csv" へ 1 行足す。
    /// データ CSV がまだ無い(1 行も受けていない)計測では書かない。
    func writeDisconnect(at date: Date, reason: String, battery: Int) {
        flush()
        guard let base = baseName, let data = savedFileURL else { return }
        let line = "\(Self.formatDate(date)),\(reason),\(battery)\n"
        if let file = disconnectFile, let h = try? FileHandle(forWritingTo: file) {
            defer { try? h.close() }
            _ = try? h.seekToEnd()
            try? h.write(contentsOf: Data(line.utf8))
            return
        }
        let file = Self.directory.appendingPathComponent("\(base)_disconnect.\(CsvFile.plainExtension)")
        let text = "// Disconnect log for \(data.lastPathComponent)\n// DATE,REASON,BATTERY\n" + line
        do {
            try Data(text.utf8).write(to: file, options: .atomic)
            disconnectFile = file
        } catch {
            NSLog("[CsvWriter] disconnect log: %@", error.localizedDescription)
        }
    }

    /// 計測を締める。残りを書き、データ CSV の場所を返す(1 行も無ければ nil)
    @discardableResult
    func stop() -> URL? {
        flush()
        let url = savedFileURL
        csvManager.reset()
        baseName = nil
        disconnectFile = nil
        return url
    }

    /// 計測条件から CSV ヘッダを作る(Mac 版 DataPersistenceService.headerString と同じ)
    static func headerString(mode: MeasurementMode, transMode: UInt32, accelRange: UInt32, gyroRange: UInt32) -> String {
        var s = ""
        s += "// Data mode  : \(mode.label)\n"
        s += "// Transmission speed  : \(MeasurementRange.hz(quality: transMode))Hz\n"
        s += "// Acceleration sensor's range  : \(MeasurementRange.accelG(device: accelRange))g\n"
        s += "// Gyroscope sensor's range  : \(MeasurementRange.gyroDps(device: gyroRange))dps\n"
        s += "//\n"
        s += "//" + (["ARTIFACT", "NUM", "DATE"] + mode.columns).joined(separator: ",") + "\n"
        return s
    }
}
