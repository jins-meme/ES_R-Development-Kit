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
