//
//  CsvArtifactWriter.swift
//  MEME_Academic
//
//  グラフ画面で付けた Artifact を CSV の ARTIFACT 列(先頭の列)へ書き戻す。
//  本アプリ形式の CSV(DataPersistenceService.headerString() / dataToStoring() が書き出すもの)が対象。
//  CSV の読み込みと再生はグラフ画面(webview/common/csv.js)が受け持つ。
//

import Foundation

enum CsvFileError: Error {
    case unreadable
    case invalidFormat
}

/// グラフ画面で付けた Artifact を、CSV へ書き戻すまで控えておく(同じ番号は上書き)。
struct ArtifactBuffer {

    /// ページが返す番号の意味
    enum Numbering {
        /// 計測中: アプリのサンプル番号(計測開始から 0, 1, 2 …。先頭パケットも数える)
        case liveSample
        /// 再生中: CSV のデータ行の番号
        case csvRow
    }

    private(set) var items: [Int: String] = [:]

    var isEmpty: Bool { items.isEmpty }

    /// 控える。断ったら false。
    @discardableResult
    mutating func add(i: Int, text: String) -> Bool {
        guard let value = Self.sanitize(text) else { return false }
        items[max(i, 0)] = value
        return true
    }

    mutating func removeAll() { items.removeAll() }

    /// CSV の ARTIFACT 列に書ける形にする。空なら "X"、カンマ/改行は列崩れ防止のため空白に。
    /// 表計算ソフトで数式として読まれる書き出し(= + - @)は nil(CSV 注入。ページも入力時に断る。webview/BRIDGE.md)。
    static func sanitize(_ text: String) -> String? {
        let s = String(text
            .replacingOccurrences(of: ",", with: " ")
            .replacingOccurrences(of: "\n", with: " ")
            .replacingOccurrences(of: "\r", with: " ")
            .trimmingCharacters(in: .whitespaces)
            .prefix(64))
        if let c = s.first, "=+-@".contains(c) {
            NSLog("[Artifact] refused (formula-like): %@", s)
            return nil
        }
        return s.isEmpty ? "X" : s
    }

    /// CSV のデータ行の番号に直したもの。CSV は先頭パケットを 1 件落とすので、
    /// サンプル番号 − 1 = データ行(サンプル 0 は CSV に無いので除く)。
    func csvRows(_ numbering: Numbering) -> [Int: String] {
        switch numbering {
        case .csvRow:
            return items
        case .liveSample:
            var rows: [Int: String] = [:]
            for (sample, text) in items where sample >= 1 { rows[sample - 1] = text }
            return rows
        }
    }

    /// 控えたものを url の CSV へ書き戻して空にする(書けなくても空にし、ログに残す)。
    mutating func flush(to url: URL?, numbering: Numbering) {
        defer { items.removeAll() }
        guard !items.isEmpty, let url else { return }
        do {
            try CsvArtifactWriter.apply(url: url, artifacts: csvRows(numbering))
        } catch {
            NSLog("[Artifact] failed to write: %@", error.localizedDescription)
        }
    }
}

enum CsvArtifactWriter {

    /// artifacts のキーは 0 始まりのデータ行の番号(//ARTIFACT 行より後の、空でない行を数える)。
    /// .csv.gz なら読み込み元と同じ形式で書き戻す(再圧縮される)。
    static func apply(url: URL, artifacts: [Int: String]) throws {
        guard !artifacts.isEmpty else { return }
        guard let content = try? CsvFile.readText(at: url) else {
            throw CsvFileError.unreadable
        }
        // 本アプリ形式のCSVは "\n" 区切り。分割→加工→"\n"で連結して構造を保つ。
        var lines = content.components(separatedBy: "\n")
        guard let headerIndex = lines.firstIndex(where: { $0.hasPrefix("//ARTIFACT") }) else {
            throw CsvFileError.invalidFormat
        }

        let dataStart = headerIndex + 1
        var dataRow = 0
        for i in dataStart..<lines.count {
            // 空行はデータ行として数えない(グラフ画面の読み方と同じ)。
            if lines[i].trimmingCharacters(in: .whitespaces).isEmpty { continue }
            if let artifact = artifacts[dataRow] {
                lines[i] = replacingFirstField(in: lines[i], with: artifact)
            }
            dataRow += 1
        }

        try CsvFile.writeText(lines.joined(separator: "\n"), to: url)
    }

    /// 1行の最初のカンマより前（ARTIFACT列）を value に差し替える。
    private static func replacingFirstField(in line: String, with value: String) -> String {
        guard let commaRange = line.range(of: ",") else { return line }
        return value + String(line[commaRange.lowerBound...])
    }
}
