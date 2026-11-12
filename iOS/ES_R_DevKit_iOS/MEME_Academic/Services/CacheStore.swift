//
//  CacheStore.swift
//  MEME_Academic (iOS)
//
//  グラフ画面の日別キャッシュ(Logic4 などの 15 秒窓 + マーカー)を、ページの求めで一覧・読み・書きする
//  (ページ → アプリの cacheList / cacheRead / cacheWrite、返りは jmasHost.cacheResult。仕様は webview/BRIDGE.md の Cache files)。
//  Android の CacheStore.kt と同じ規則(名前の形・16 MB の上限・.tmp に書いてから名前を変える・エラーの名前)。
//
//  - 置き場所は `<データフォルダ>/logic4-cache/<device>/<YYYY-MM-DD>.json`(データフォルダ = Documents。データ CSV と同じ。
//    UIFileSharingEnabled なので「ファイル」App の「このiPhone内」から見える)。Android と違い、フォルダの許可は要らない
//    (自分の Documents なので、ほかのアプリ・同期の道具が置いたファイルもそのまま読み書きできる)。
//  - 書くときは `<name>.tmp` に全部書いてから `<name>` へ置き換える(同期の道具に書きかけを渡さない)。
//  - このフォルダの外は読み書きしない(device / name は形を決めて検査し、決まった 2 段の下にしか触らない)。
//  - 入出力は 1 本の別スレッドで順に行い(受信中は 60 秒ごとに書くので UI スレッドを止めない・同じファイルへの書き込みを重ねない)、
//    結果は JSON の文字列にしてメインスレッドへ返す。
//

import Foundation

final class CacheStore: Sendable {

    /// start の features・openReplay の features に入れる名前
    static let feature = "cache"
    static let dirName = "logic4-cache"
    /// 1 ファイルの上限(バイト。BRIDGE.md)
    static let maxBytes = 16 * 1024 * 1024
    private static let tmpSuffix = ".tmp"

    /// logic4-cache の場所(テストでは一時フォルダを渡す)
    let root: URL
    private let queue = DispatchQueue(label: "logic4-cache")

    /// root を省くと Documents/logic4-cache(CsvWriter.directory と同じ Documents。CsvWriter は MainActor なのでここで求める)
    init(root: URL? = nil) {
        self.root = root ?? FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
            .appendingPathComponent(CacheStore.dirName, isDirectory: true)
    }

    // MARK: - 名前

    static func validDevice(_ s: String) -> Bool { s.range(of: "^[0-9A-F]{12}$", options: .regularExpression) != nil }
    static func validName(_ s: String) -> Bool { s.range(of: "^[0-9]{4}-[0-9]{2}-[0-9]{2}\\.json$", options: .regularExpression) != nil }

    /// 端末のアドレス(AA:BB:… / AABB…)→ device(コロンなしの大文字 12 桁)。形が違えば nil(ページはキャッシュを使わない)
    static func deviceOf(address: String) -> String? {
        let s = address.replacingOccurrences(of: ":", with: "").uppercased()
        return validDevice(s) ? s : nil
    }

    /// データの CSV 名の先頭(<MAC>_YYYYmmddHHMMSS…)→ device。取れなければ nil
    static func deviceOf(fileName: String) -> String? {
        guard let r = fileName.range(of: "^[0-9A-Fa-f]{12}_", options: .regularExpression) else { return nil }
        return String(fileName[r].dropLast()).uppercased()
    }

    // MARK: - ページからのメッセージ

    /// kind = cacheList / cacheRead / cacheWrite。idJSON はページの id を JSON にしたもの(そのまま返りに入れる)。
    /// done はメインスレッドで、jmasHost.cacheResult に渡す JSON の文字列を受け取る
    func handle(idJSON: String, kind: String, device: String, name: String, text: String,
                done: @escaping @MainActor @Sendable (String) -> Void) {
        queue.async { [self] in
            let r = run(kind: kind, device: device, name: name, text: text)
            let json = Self.answer(r, idJSON: idJSON)
            DispatchQueue.main.async { MainActor.assumeIsolated { done(json) } }
        }
    }

    /// 1 件ぶんを同期に行う(handle の中・テストから)。返りは cacheResult の中身(id を除く)
    func run(kind: String, device: String, name: String, text: String) -> [String: Any] {
        guard Self.validDevice(device) else { return Self.err("bad_name") }
        if kind != "cacheList" && !Self.validName(name) { return Self.err("bad_name") }
        let fm = FileManager.default
        let dir = root.appendingPathComponent(device, isDirectory: true)
        do {
            switch kind {
            case "cacheList":
                var files: [[String: Any]] = []
                guard fm.fileExists(atPath: dir.path) else { return ["ok": true, "files": files] }
                for n in try fm.contentsOfDirectory(atPath: dir.path).sorted() where Self.validName(n) {
                    let a = try fm.attributesOfItem(atPath: dir.appendingPathComponent(n).path)
                    guard (a[.type] as? FileAttributeType) == .typeRegular else { continue }
                    let size = (a[.size] as? NSNumber)?.intValue ?? 0
                    let mtime = ((a[.modificationDate] as? Date)?.timeIntervalSince1970 ?? 0) * 1000
                    files.append(["name": n, "size": size, "mtime": mtime.rounded()])
                }
                return ["ok": true, "files": files]
            case "cacheRead":
                let f = dir.appendingPathComponent(name)
                guard fm.fileExists(atPath: f.path) else { return Self.err("not_found") }
                let size = ((try fm.attributesOfItem(atPath: f.path))[.size] as? NSNumber)?.intValue ?? 0
                guard size <= Self.maxBytes else { return Self.err("too_large") }
                let data = try Data(contentsOf: f)
                guard data.count <= Self.maxBytes else { return Self.err("too_large") }
                return ["ok": true, "text": String(decoding: data, as: UTF8.self)]
            case "cacheWrite":
                let data = Data(text.utf8)
                guard data.count <= Self.maxBytes else { return Self.err("too_large") }
                try fm.createDirectory(at: dir, withIntermediateDirectories: true)
                let dst = dir.appendingPathComponent(name), tmp = dir.appendingPathComponent(name + Self.tmpSuffix)
                try? fm.removeItem(at: tmp)
                try data.write(to: tmp)
                if fm.fileExists(atPath: dst.path) {
                    _ = try fm.replaceItemAt(dst, withItemAt: tmp)       // 同じボリュームの中の置き換え(名前の変更)
                } else {
                    try fm.moveItem(at: tmp, to: dst)
                }
                return ["ok": true, "size": data.count]
            default:
                return Self.err("bad_kind")
            }
        } catch {
            NSLog("[CacheStore] %@ %@/%@ failed: %@", kind, device, name, error.localizedDescription)
            return Self.err("io_error")
        }
    }

    private static func err(_ code: String) -> [String: Any] { ["ok": false, "error": code] }

    /// 返りの JSON(先頭に id を入れる)
    static func answer(_ r: [String: Any], idJSON: String) -> String {
        guard let data = try? JSONSerialization.data(withJSONObject: r), var s = String(data: data, encoding: .utf8) else {
            return "{\"id\":\(idJSON),\"ok\":false,\"error\":\"io_error\"}"
        }
        s.removeFirst()                                          // "{" を外して id を前に足す
        return "{\"id\":\(idJSON)" + (s == "}" ? "}" : "," + s)
    }
}
