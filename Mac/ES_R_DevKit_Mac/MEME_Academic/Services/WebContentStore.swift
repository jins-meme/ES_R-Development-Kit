//
//  WebContentStore.swift
//  MEME_Academic
//
//  グラフ画面(WebView)の中身の置き場。中身は zip で、アプリに同梱した標準版(standard.zip)か、
//  設定で選んだ zip(高機能版など)を展開して使う。仕様は webview/README.md。
//
//  - 展開先: ~/Library/Application Support/<bundle id>/WebContent/{bundled,custom}/
//  - 同梱の標準版は、アプリに入っている zip の中身が変わったとき(SHA-256 で見る)だけ展開し直す。
//  - 選んだ zip は一時フォルダへ展開して検査(manifest.json・bridgeApi・パスの抜け・大きさ)してから差し替える。
//    通らなければ元のまま。展開は /usr/bin/ditto に任せる。
//

import Foundation
import CryptoKit

struct WebContentManifest: Decodable, Equatable {
    let name: String
    let title: String?
    let version: String
    let bridgeApi: Int
    let entry: String

    var displayName: String { "\(title ?? name) \(version)" }
}

enum WebContentError: LocalizedError {
    case unzipFailed(String)
    case noManifest
    case badManifest(String)
    case unsupportedBridge(Int)
    case missingEntry(String)
    case unsafePath(String)
    case tooLarge(Int64)

    var errorDescription: String? {
        switch self {
        case .unzipFailed(let s): return "The zip file could not be extracted. \(s)"
        case .noManifest: return "manifest.json was not found at the top of the zip."
        case .badManifest(let s): return "manifest.json could not be read. \(s)"
        case .unsupportedBridge(let v): return "This zip needs bridge API \(v), but this app supports \(WebContentStore.bridgeApi)."
        case .missingEntry(let s): return "The entry page \(s) is missing."
        case .unsafePath(let s): return "The zip contains a link or path outside itself: \(s)"
        case .tooLarge(let n): return "The zip is too large (\(n / 1_000_000) MB, limit \(WebContentStore.maxBytes / 1_000_000) MB)."
        }
    }
}

@MainActor
final class WebContentStore {

    static let shared = WebContentStore()

    /// このアプリが話せるブリッジの版(webview/BRIDGE.md)。zip の manifest.json の bridgeApi と一致しないものは読まない。
    nonisolated static let bridgeApi = 1
    nonisolated static let maxBytes: Int64 = 200_000_000

    enum Source: String { case bundled, custom }

    private let fm = FileManager.default
    private(set) var root: URL
    private(set) var manifest: WebContentManifest?
    private(set) var source: Source = .bundled

    private init() {
        let base = (try? FileManager.default.url(for: .applicationSupportDirectory, in: .userDomainMask,
                                                 appropriateFor: nil, create: true))
            ?? URL(fileURLWithPath: NSTemporaryDirectory())
        let dir = base.appendingPathComponent(Bundle.main.bundleIdentifier ?? "MEME_Academic", isDirectory: true)
            .appendingPathComponent("WebContent", isDirectory: true)
        root = dir
        try? fm.createDirectory(at: dir, withIntermediateDirectories: true)
        prepare()
    }

    private var bundledDir: URL { root.appendingPathComponent("bundled", isDirectory: true) }
    private var customDir: URL { root.appendingPathComponent("custom", isDirectory: true) }

    /// 今使う中身のフォルダ(仮想ホストの根)
    var activeDir: URL { source == .custom ? customDir : bundledDir }

    /// 起動時: 同梱の標準版を必要なら展開し、設定で選ばれている方を読む。
    func prepare() {
        do { try extractBundledIfNeeded() } catch { NSLog("[WebContent] bundled: %@", error.localizedDescription) }
        let wanted = Source(rawValue: UserSetting.getWebContentSource()) ?? .bundled
        if wanted == .custom, let m = try? Self.readManifest(in: customDir) {
            source = .custom; manifest = m
        } else {
            source = .bundled; manifest = try? Self.readManifest(in: bundledDir)
        }
    }

    // MARK: - 同梱の標準版

    private static var bundledZipURL: URL? {
        Bundle.main.url(forResource: "standard", withExtension: "zip")
            ?? Bundle.main.url(forResource: "standard", withExtension: "zip", subdirectory: "WebContent")
    }

    private func extractBundledIfNeeded() throws {
        guard let zip = Self.bundledZipURL else { throw WebContentError.unzipFailed("standard.zip is not in the app bundle.") }
        let data = try Data(contentsOf: zip)
        let hash = SHA256.hash(data: data).map { String(format: "%02x", $0) }.joined()
        let mark = root.appendingPathComponent("bundled.sha256")
        if (try? String(contentsOf: mark, encoding: .utf8)) == hash, fm.fileExists(atPath: bundledDir.path) { return }
        let tmp = try extractAndValidate(zip)
        try replace(bundledDir, with: tmp)
        try hash.write(to: mark, atomically: true, encoding: .utf8)
    }

    // MARK: - 設定で選んだ zip

    /// zip を取り込んで「選んだ zip」に切り替える。検査に通らなければ投げ、今の中身はそのまま。
    @discardableResult
    func importZip(_ zip: URL) throws -> WebContentManifest {
        let tmp = try extractAndValidate(zip)
        try replace(customDir, with: tmp)
        let m = try Self.readManifest(in: customDir)
        source = .custom; manifest = m
        UserSetting.setWebContentSource(Source.custom.rawValue)
        return m
    }

    /// 同梱の標準版に戻す(取り込んだ zip のフォルダは消す)。
    func useBundled() {
        try? fm.removeItem(at: customDir)
        source = .bundled
        manifest = try? Self.readManifest(in: bundledDir)
        UserSetting.setWebContentSource(Source.bundled.rawValue)
    }

    // MARK: - 展開と検査

    private func extractAndValidate(_ zip: URL) throws -> URL {
        let size = (try? fm.attributesOfItem(atPath: zip.path)[.size] as? Int64) ?? 0
        if size > Self.maxBytes { throw WebContentError.tooLarge(size) }
        let tmp = root.appendingPathComponent("tmp-\(UUID().uuidString)", isDirectory: true)
        try fm.createDirectory(at: tmp, withIntermediateDirectories: true)
        do {
            let p = Process()
            p.executableURL = URL(fileURLWithPath: "/usr/bin/ditto")
            p.arguments = ["-x", "-k", "--norsrc", zip.path, tmp.path]
            let err = Pipe(); p.standardError = err
            try p.run(); p.waitUntilExit()
            guard p.terminationStatus == 0 else {
                let msg = String(data: err.fileHandleForReading.readDataToEndOfFile(), encoding: .utf8) ?? ""
                throw WebContentError.unzipFailed(msg)
            }
            try checkTree(tmp)
            let m = try Self.readManifest(in: tmp)
            guard m.bridgeApi == Self.bridgeApi else { throw WebContentError.unsupportedBridge(m.bridgeApi) }
            guard !m.entry.contains(".."), fm.fileExists(atPath: tmp.appendingPathComponent(m.entry).path) else {
                throw WebContentError.missingEntry(m.entry)
            }
            return tmp
        } catch {
            try? fm.removeItem(at: tmp)
            throw error
        }
    }

    /// 展開したものにシンボリックリンクや外へ出るパスが無いこと、合計の大きさが上限以下であること。
    private func checkTree(_ dir: URL) throws {
        let base = dir.resolvingSymlinksInPath().path + "/"
        var total: Int64 = 0
        let keys: [URLResourceKey] = [.isSymbolicLinkKey, .fileSizeKey]
        guard let e = fm.enumerator(at: dir, includingPropertiesForKeys: keys) else { return }
        for case let url as URL in e {
            let v = try url.resourceValues(forKeys: Set(keys))
            if v.isSymbolicLink == true { throw WebContentError.unsafePath(url.lastPathComponent) }
            if !url.resolvingSymlinksInPath().path.hasPrefix(base) { throw WebContentError.unsafePath(url.path) }
            total += Int64(v.fileSize ?? 0)
            if total > Self.maxBytes { throw WebContentError.tooLarge(total) }
        }
    }

    private func replace(_ dest: URL, with tmp: URL) throws {
        if fm.fileExists(atPath: dest.path) { try fm.removeItem(at: dest) }
        try fm.moveItem(at: tmp, to: dest)
    }

    static func readManifest(in dir: URL) throws -> WebContentManifest {
        let url = dir.appendingPathComponent("manifest.json")
        guard let data = try? Data(contentsOf: url) else { throw WebContentError.noManifest }
        do { return try JSONDecoder().decode(WebContentManifest.self, from: data) }
        catch { throw WebContentError.badManifest(error.localizedDescription) }
    }
}
