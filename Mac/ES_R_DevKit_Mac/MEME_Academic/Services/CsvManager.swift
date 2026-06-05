//
//  CsvManager.swift
//  MEME_Academic
//
//  Created by Celleus on 2022/09/06.
//  Copyright © 2022 jins-jp. All rights reserved.
//

import Foundation

class CsvManager: NSObject {
    var saveDirectoryPath: String?
    var saveFilePath: String?
    var saveFileName: String?
    var isSave: Bool = false

    func reset() {
        isSave = false
        saveDirectoryPath = nil
        saveFilePath = nil
        saveFileName = nil
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

        let directoryURL = URL(fileURLWithPath: directoryPath, isDirectory: true)
        do {
            try FileManager.default.createDirectory(at: directoryURL, withIntermediateDirectories: true, attributes: nil)
        } catch {
            NSLog("ディレクトリ作成失敗:%@", error.localizedDescription)
            isSave = false
            return isSave
        }

        do {
            try firstData.write(to: URL(fileURLWithPath: path), options: .atomic)
            isSave = true
        } catch {
            isSave = false
        }
        return isSave
    }

    func append(_ appendData: Data) {
        guard let path = saveFilePath else { return }
        guard let fileHandle = try? FileHandle(forWritingTo: URL(fileURLWithPath: path)) else {
            return
        }
        fileHandle.seekToEndOfFile()
        fileHandle.write(appendData)
        try? fileHandle.close()
    }
}
