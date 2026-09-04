//
//  CsvFile.swift
//  MEME_Academic
//
//  本アプリが扱う CSV ファイル（非圧縮 .csv / gz圧縮 .csv.gz）の
//  拡張子判定・UTType・テキストの読み書きをまとめたヘルパー。
//  「.gz かどうか」の判定をここ1か所に閉じ込め、保存・再生・切り出し・
//  Artifact 書き戻しが同じ規則で動くようにする。
//

import Foundation
import UniformTypeIdentifiers

enum CsvFile {

    /// 非圧縮の拡張子（ドット無し）。
    static let plainExtension = "csv"
    /// gz圧縮の拡張子（ドット無し、2段）。
    static let gzipExtension = "csv.gz"

    /// 新規保存で使う拡張子。圧縮するかは Setting の "Compress saved files" で決まる。
    static func saveExtension(compressed: Bool) -> String {
        compressed ? gzipExtension : plainExtension
    }

    // MARK: - File name

    /// gz 圧縮された CSV か（拡張子で判定、大文字小文字は問わない）。
    static func isGzip(fileName: String) -> Bool {
        fileName.lowercased().hasSuffix("." + gzipExtension)
    }

    static func isGzip(_ url: URL) -> Bool {
        isGzip(fileName: url.lastPathComponent)
    }

    /// 読み込み対象として扱う拡張子（.csv / .csv.gz）を持つか。
    static func isSupported(fileName: String) -> Bool {
        let lower = fileName.lowercased()
        return lower.hasSuffix("." + gzipExtension) || lower.hasSuffix("." + plainExtension)
    }

    static func isSupported(_ url: URL) -> Bool {
        isSupported(fileName: url.lastPathComponent)
    }

    /// 拡張子（".csv" / ".csv.gz"）を取り除いたベース名。
    /// `URL.deletingPathExtension` は "a.csv.gz" から ".gz" しか落とせないので用意する。
    static func baseName(of url: URL) -> String {
        let name = url.lastPathComponent
        let lower = name.lowercased()
        for ext in [gzipExtension, plainExtension] where lower.hasSuffix("." + ext) {
            return String(name.dropLast(ext.count + 1))
        }
        return url.deletingPathExtension().lastPathComponent
    }

    /// url と同じ圧縮形式の拡張子（ドット無し）。切り出しファイルを元ファイルへ揃えるのに使う。
    static func matchingExtension(of url: URL) -> String {
        isGzip(url) ? gzipExtension : plainExtension
    }

    // MARK: - UTType

    /// NSOpenPanel で選ばせる型。gz は .csv.gz だけを狙いたいが、システムには
    /// 2段拡張子ぶんの標準型が無いため gzip 全般を許し、中身のパースで弾く。
    static var openPanelTypes: [UTType] {
        var types: [UTType] = [.commaSeparatedText]
        if let gz = UTType(filenameExtension: "gz") {
            types.append(gz)
        }
        return types
    }

    /// NSSavePanel の allowedContentTypes 用。ファイル名の拡張子に対応する型を返す。
    static func contentType(forFileName name: String) -> UTType? {
        isGzip(fileName: name) ? UTType(filenameExtension: "gz") : UTType(filenameExtension: plainExtension)
    }

    // MARK: - Read / Write

    /// CSV をテキストとして読む。拡張子が .gz か、中身が gzip なら展開してから文字列にする。
    /// 拡張子と中身が食い違うファイル（.csv なのに gz 等）でも中身を優先して読めるようにしている。
    static func readText(at url: URL) throws -> String {
        let raw = try Data(contentsOf: url)
        let bytes = Gzip.isGzipped(raw) ? try Gzip.decompress(raw) : raw
        guard let text = String(data: bytes, encoding: .utf8) else {
            throw CsvReplayError.unreadable
        }
        return text
    }

    /// テキストを CSV として書き出す。url が .csv.gz なら gz 圧縮して書く。
    static func writeText(_ text: String, to url: URL) throws {
        guard let utf8 = text.data(using: .utf8) else { throw CsvReplayError.unreadable }
        let bytes = isGzip(url) ? try Gzip.compress(utf8) : utf8
        try bytes.write(to: url, options: .atomic)
    }
}
