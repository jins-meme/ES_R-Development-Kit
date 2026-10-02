//
//  WebContentStore.swift
//  MEME_Academic
//
//  グラフ画面(WebView)の中身の置き場。中身は zip で、アプリに同梱した標準版(standard.zip)か、
//  設定で選んだ zip(高機能版など)を展開して使う。仕様は webview/README.md。
//
//  - 展開先: ~/Library/Application Support/<bundle id>/WebContent/{bundled,custom}/
//  - 同梱の標準版は、アプリに入っている zip の中身が変わったとき(SHA-256 で見る)だけ展開し直す。
//  - 選んだ zip は ZipExtractor で検査しながら一時フォルダへ展開し(zip slip・zip 爆弾・リンクなど。展開する前に目次で弾く)、
//    manifest.json・bridgeApi・入口を確かめてから差し替える。通らなければ元のまま。規則は webview/BRIDGE.md の Limits。
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

    var errorDescription: String? {
        switch self {
        case .unzipFailed(let s): return "The zip file could not be extracted. \(s)"
        case .noManifest: return "manifest.json was not found at the top of the zip."
        case .badManifest(let s): return "manifest.json could not be read. \(s)"
        case .unsupportedBridge(let v): return "This zip needs bridge API \(v), but this app supports \(WebContentStore.bridgeApi)."
        case .missingEntry(let s): return "The entry page \(s) is missing."
        case .unsafePath(let s): return "The zip contains a link or path outside itself: \(s)"
        }
    }
}

@MainActor
final class WebContentStore {

    static let shared = WebContentStore()

    /// このアプリが話せるブリッジの版(webview/BRIDGE.md)。zip の manifest.json の bridgeApi と一致しないものは読まない。
    nonisolated static let bridgeApi = 1

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
        let tmp = root.appendingPathComponent("tmp-\(UUID().uuidString)", isDirectory: true)
        try fm.createDirectory(at: tmp, withIntermediateDirectories: true)
        do {
            do { _ = try ZipExtractor.extract(zip, to: tmp) }
            catch let e as ZipExtractor.Failure { throw WebContentError.unzipFailed(e.localizedDescription) }
            try checkTree(tmp)
            let m = try Self.readManifest(in: tmp)
            guard m.bridgeApi == Self.bridgeApi else { throw WebContentError.unsupportedBridge(m.bridgeApi) }
            guard (try? ZipExtractor.checkName(m.entry, raw: m.entry, limits: .init())) != nil,
                  fm.fileExists(atPath: tmp.appendingPathComponent(m.entry).path) else {
                throw WebContentError.missingEntry(m.entry)
            }
            return tmp
        } catch {
            try? fm.removeItem(at: tmp)
            throw error
        }
    }

    /// 念押し: 展開したものにシンボリックリンクや外へ出るパスが無いこと(ZipExtractor が目次で弾いているので、通常は何も見つからない)。
    private func checkTree(_ dir: URL) throws {
        let base = dir.resolvingSymlinksInPath().path + "/"
        let keys: [URLResourceKey] = [.isSymbolicLinkKey]
        guard let e = fm.enumerator(at: dir, includingPropertiesForKeys: keys) else { return }
        for case let url as URL in e {
            let v = try url.resourceValues(forKeys: Set(keys))
            if v.isSymbolicLink == true { throw WebContentError.unsafePath(url.lastPathComponent) }
            if !url.resolvingSymlinksInPath().path.hasPrefix(base) { throw WebContentError.unsafePath(url.path) }
        }
    }

    /// 前の中身は新しいものを置けてから消す(置けなければ元へ戻す)
    private func replace(_ dest: URL, with tmp: URL) throws {
        let old = root.appendingPathComponent("old-\(UUID().uuidString)", isDirectory: true)
        let hadOld = fm.fileExists(atPath: dest.path)
        if hadOld { try fm.moveItem(at: dest, to: old) }
        do {
            try fm.moveItem(at: tmp, to: dest)
        } catch {
            if hadOld { try? fm.moveItem(at: old, to: dest) }
            try? fm.removeItem(at: tmp)
            throw error
        }
        if hadOld { try? fm.removeItem(at: old) }
    }

    static func readManifest(in dir: URL) throws -> WebContentManifest {
        let url = dir.appendingPathComponent("manifest.json")
        guard let data = try? Data(contentsOf: url) else { throw WebContentError.noManifest }
        guard data.count <= 64_000 else { throw WebContentError.badManifest("too large") }
        let m: WebContentManifest
        do { m = try JSONDecoder().decode(WebContentManifest.self, from: data) }
        catch { throw WebContentError.badManifest(error.localizedDescription) }
        // 設定画面にそのまま出すので、短く・制御文字なし
        for (k, v) in [("name", m.name), ("title", m.title ?? ""), ("version", m.version)] {
            if v.count > 64 || v.unicodeScalars.contains(where: { $0.value < 0x20 || $0.value == 0x7F }) {
                throw WebContentError.badManifest("\(k) must be at most 64 characters without control characters")
            }
        }
        return m
    }
}

#if DEBUG
// MARK: - 自己テスト用(DebugAutoTest)

extension WebContentStore {
    /// 選んだ zip の展開先を一時フォルダへ写す(自己テストが zip を取り込んで上書きする前に)
    func copyCustomAside() -> URL? {
        let dst = fm.temporaryDirectory.appendingPathComponent("autotest-custom-\(UUID().uuidString)", isDirectory: true)
        do { try fm.copyItem(at: customDir, to: dst); return dst } catch {
            NSLog("[WebContent] copy custom aside: %@", error.localizedDescription); return nil
        }
    }

    /// copyCustomAside で写したものを戻し、選んだ zip を使う設定に戻す
    func restoreCustom(from kept: URL) {
        try? fm.removeItem(at: customDir)
        do { try fm.moveItem(at: kept, to: customDir) } catch {
            NSLog("[WebContent] restore custom: %@", error.localizedDescription)
        }
        UserSetting.setWebContentSource(Source.custom.rawValue)
        prepare()
    }
}
#endif
