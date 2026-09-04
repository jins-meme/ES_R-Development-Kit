//
//  CsvManager.swift
//  MEME_Academic
//
//  Created by Celleus on 2022/09/06.
//  Copyright © 2022 jins-jp. All rights reserved.
//

import Foundation

/// 計測中のCSVをディスクへ書き出す。ファイル名が .csv.gz なら gzip 圧縮して書く。
///
/// 圧縮は「1回の書き出し＝gzipの1メンバー」とし、それをファイルへ連結していく。
/// gzip は複数メンバーの連結を1ファイルとして扱えるので、これで
/// `gzip -d` でもアプリの再生でもそのまま読める。
/// ストリームを開きっぱなしにして最後にトレーラを書く方式と違い、
/// アプリが落ちても／切断で計測が途切れても、その時点までのファイルが常に完結している。
/// （連続ストリームに比べた圧縮率の悪化は実測で数％。長時間計測の取りこぼしを防ぐ方を採る。）
class CsvManager: NSObject {
    var saveDirectoryPath: String?
    var saveFilePath: String?
    var saveFileName: String?
    var isSave: Bool = false

    /// 書き出し先が gz 圧縮か（create でファイル名から決まる）。
    private var isCompressed: Bool = false

    func reset() {
        isSave = false
        saveDirectoryPath = nil
        saveFilePath = nil
        saveFileName = nil
        isCompressed = false
    }

    @discardableResult
    func create(directoryPath: String, fileName: String, firstData: Data) -> Bool {
        saveDirectoryPath = directoryPath
        NSLog("saveDirectoryPath:%@", directoryPath)
        saveFileName = fileName
        NSLog("saveFileName:%@", fileName)
        let path = (directoryPath as NSString).appendingPathComponent(fileName)
        saveFilePath = path
        NSLog("saveFilePath:%@", path)
        isCompressed = CsvFile.isGzip(fileName: fileName)

        let directoryURL = URL(fileURLWithPath: directoryPath, isDirectory: true)
        do {
            try FileManager.default.createDirectory(at: directoryURL, withIntermediateDirectories: true, attributes: nil)
        } catch {
            NSLog("ディレクトリ作成失敗:%@", error.localizedDescription)
            isSave = false
            return isSave
        }

        guard let payload = encoded(firstData) else {
            isSave = false
            return isSave
        }
        do {
            try payload.write(to: URL(fileURLWithPath: path), options: .atomic)
            isSave = true
        } catch {
            isSave = false
        }
        return isSave
    }

    func append(_ appendData: Data) {
        guard let path = saveFilePath else { return }
        guard let payload = encoded(appendData) else { return }
        guard let fileHandle = try? FileHandle(forWritingTo: URL(fileURLWithPath: path)) else {
            return
        }
        fileHandle.seekToEndOfFile()
        fileHandle.write(payload)
        try? fileHandle.close()
    }

    /// 書き出す実バイト列。gz 指定なら1メンバーぶんへ圧縮する。
    private func encoded(_ data: Data) -> Data? {
        guard isCompressed else { return data }
        do {
            return try Gzip.compress(data)
        } catch {
            NSLog("gzip圧縮失敗:%@", error.localizedDescription)
            return nil
        }
    }
}
