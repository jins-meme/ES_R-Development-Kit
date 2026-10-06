//
//  WebContentStore.swift
//  MEME_Academic
//
//  グラフ画面(WebView)の中身(Display Engine)の置き場。中身は zip で、アプリに同梱した標準版(standard.zip)と、
//  Display Engine ダイアログで取り込んだ zip(高機能版など、いくつでも)を展開して持っておき、そのうち 1 つだけを使う。
//  仕様は webview/README.md。
//
//  - 展開先: ~/Library/Application Support/<bundle id>/WebContent/
//      bundled/        同梱の標準版。消せない(ID は "standard")
//      zips/<ID>/      取り込んだ zip。ID は取り込んだときに振る UUID
//    使っている 1 つの ID は UserSetting.webContentActive。
//  - 同梱の標準版は、アプリに入っている zip の中身が変わったとき(SHA-256 で見る)だけ展開し直す。
//  - 取り込む zip は ZipExtractor で検査しながら一時フォルダへ展開し(zip slip・zip 爆弾・リンクなど。展開する前に目次で弾く)、
//    manifest.json・bridgeApi・入口を確かめてから置く。通らなければ何も変わらない。規則は webview/BRIDGE.md の Limits。
//      zips/<ID>.sha256  取り込んだ zip ファイルの SHA-256(同じファイルを 2 度取り込まないため)
//  - 同じファイル(SHA-256 が同じ)をもう一度取り込んでも何もしない(一覧に重ならない)。
//  - manifest の name が同じ zip を取り込んだら、新しい版として置き換える(ID・一覧での位置・使っているかどうかはそのまま)。
//  - 1.5.0 build 35 までの custom/(選んだ zip を 1 つだけ持てた)は、起動時に zips/ の 1 つへ移す。
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

/// 持っている中身 1 つ(Display Engine ダイアログの 1 行)
struct WebContentEntry: Identifiable, Equatable {
    let id: String
    let manifest: WebContentManifest
    var isBuiltIn: Bool { id == WebContentStore.builtInId }
}

@MainActor
final class WebContentStore {

    static let shared = WebContentStore()

    /// このアプリが話せるブリッジの版(webview/BRIDGE.md)。zip の manifest.json の bridgeApi と一致しないものは読まない。
    nonisolated static let bridgeApi = 1
    /// 同梱の標準版の ID(規定。消せない)
    nonisolated static let builtInId = "standard"

    private let fm = FileManager.default
    private(set) var root: URL
    /// 持っている中身。先頭が同梱の標準版、あとは取り込んだ順
    private(set) var entries: [WebContentEntry] = []
    /// 使っている中身の ID
    private(set) var activeId: String = WebContentStore.builtInId

    private init() {
        let base = (try? FileManager.default.url(for: .applicationSupportDirectory, in: .userDomainMask,
                                                 appropriateFor: nil, create: true))
            ?? URL(fileURLWithPath: NSTemporaryDirectory())
        let dir = base.appendingPathComponent(Bundle.main.bundleIdentifier ?? "MEME_Academic", isDirectory: true)
            .appendingPathComponent("WebContent", isDirectory: true)
        root = dir
        try? fm.createDirectory(at: dir.appendingPathComponent("zips", isDirectory: true), withIntermediateDirectories: true)
        prepare()
    }

    private var bundledDir: URL { root.appendingPathComponent("bundled", isDirectory: true) }
    private var zipsDir: URL { root.appendingPathComponent("zips", isDirectory: true) }
    /// 1.5.0 build 35 までの「選んだ zip」の置き場(移したら無くなる)
    private var legacyCustomDir: URL { root.appendingPathComponent("custom", isDirectory: true) }

    private func dir(of id: String) -> URL {
        id == Self.builtInId ? bundledDir : zipsDir.appendingPathComponent(id, isDirectory: true)
    }
    private func hashFile(of id: String) -> URL { zipsDir.appendingPathComponent("\(id).sha256") }

    /// 今使う中身のフォルダ(仮想ホストの根)
    var activeDir: URL { dir(of: activeId) }
    /// 今使う中身の manifest
    var manifest: WebContentManifest? { entries.first { $0.id == activeId }?.manifest }

    /// 起動時: 同梱の標準版を必要なら展開し、持っている中身を読み、設定で有効になっている 1 つを使う。
    func prepare() {
        do { try extractBundledIfNeeded() } catch { NSLog("[WebContent] bundled: %@", error.localizedDescription) }
        dropLeftovers()
        migrateLegacyCustom()
        reloadEntries()
        let wanted = UserSetting.getWebContentActive() ?? Self.builtInId
        activeId = entries.contains { $0.id == wanted } ? wanted : Self.builtInId
        if activeId != wanted { UserSetting.setWebContentActive(activeId) }
    }

    /// 途中で止まった取り込み・置き換えの残り(tmp-* / old-*)を消す。起動時は何も取り込んでいないので、残っていれば全部ゴミ。
    private func dropLeftovers() {
        for name in (try? fm.contentsOfDirectory(atPath: root.path)) ?? [] where name.hasPrefix("tmp-") || name.hasPrefix("old-") {
            try? fm.removeItem(at: root.appendingPathComponent(name))
        }
    }

    /// zips/ の中を読み直す。manifest を読めないフォルダ(途中で止まった取り込みの残りなど)は消す。
    private func reloadEntries() {
        var list: [WebContentEntry] = []
        if let m = try? Self.readManifest(in: bundledDir) { list.append(.init(id: Self.builtInId, manifest: m)) }
        let keys: [URLResourceKey] = [.isDirectoryKey, .creationDateKey]
        let dirs = (try? fm.contentsOfDirectory(at: zipsDir, includingPropertiesForKeys: keys)) ?? []
        var imported: [(Date, WebContentEntry)] = []
        for d in dirs {
            let v = try? d.resourceValues(forKeys: Set(keys))
            guard v?.isDirectory == true else { continue }
            guard let m = try? Self.readManifest(in: d) else {
                NSLog("[WebContent] drop unreadable %@", d.lastPathComponent)
                try? fm.removeItem(at: d)
                try? fm.removeItem(at: hashFile(of: d.lastPathComponent))
                continue
            }
            imported.append((v?.creationDate ?? .distantPast, .init(id: d.lastPathComponent, manifest: m)))
        }
        list += imported.sorted { $0.0 < $1.0 }.map(\.1)
        entries = list
    }

    /// 1.5.0 build 35 までの custom/ を zips/ の 1 つへ移す。使っていたなら、移した先を使う設定にする。
    private func migrateLegacyCustom() {
        let legacy = UserSetting.takeLegacyWebContentSource()
        guard fm.fileExists(atPath: legacyCustomDir.path) else { return }
        let id = UUID().uuidString
        do {
            try fm.moveItem(at: legacyCustomDir, to: dir(of: id))
            if legacy == "custom" { UserSetting.setWebContentActive(id) }
        } catch {
            NSLog("[WebContent] migrate custom: %@", error.localizedDescription)
        }
    }

    // MARK: - 同梱の標準版

    private static var bundledZipURL: URL? {
        Bundle.main.url(forResource: "standard", withExtension: "zip")
            ?? Bundle.main.url(forResource: "standard", withExtension: "zip", subdirectory: "WebContent")
    }

    private static func sha256(of file: URL) throws -> String {
        SHA256.hash(data: try Data(contentsOf: file, options: .mappedIfSafe)).map { String(format: "%02x", $0) }.joined()
    }

    private func extractBundledIfNeeded() throws {
        guard let zip = Self.bundledZipURL else { throw WebContentError.unzipFailed("standard.zip is not in the app bundle.") }
        let hash = try Self.sha256(of: zip)
        let mark = root.appendingPathComponent("bundled.sha256")
        if (try? String(contentsOf: mark, encoding: .utf8)) == hash, fm.fileExists(atPath: bundledDir.path) { return }
        let tmp = try extractAndValidate(zip)
        try replace(bundledDir, with: tmp)
        try hash.write(to: mark, atomically: true, encoding: .utf8)
    }

    // MARK: - 取り込み・有効化・削除(Display Engine ダイアログ)

    enum AddOutcome { case added, replaced, alreadyAdded }

    /// zip を検査して取り込み、一覧に足す(有効にはしない)。検査に通らなければ投げ、何も変わらない。
    /// - 同じファイルを既に取り込んでいれば何もしない(.alreadyAdded。一覧に重ならない)。
    /// - manifest の name が同じものを持っていれば、それを置き換える(.replaced。ID・位置・使っているかどうかはそのまま)。
    /// 戻り値の `replacedActive` が true なら、使っている中身が変わったのでグラフ画面を読み込み直すこと。
    @discardableResult
    func add(_ zip: URL) throws -> (entry: WebContentEntry, outcome: AddOutcome, replacedActive: Bool) {
        let hash: String
        do { hash = try Self.sha256(of: zip) } catch { throw WebContentError.unzipFailed(error.localizedDescription) }
        if let same = entries.first(where: {
            !$0.isBuiltIn && (try? String(contentsOf: hashFile(of: $0.id), encoding: .utf8)) == hash
        }) {
            return (same, .alreadyAdded, false)
        }
        let tmp = try extractAndValidate(zip)
        let m = try Self.readManifest(in: tmp)
        let existing = entries.first { !$0.isBuiltIn && $0.manifest.name == m.name }?.id
        let id = existing ?? UUID().uuidString
        // 一覧は取り込んだ順(フォルダの作成日時)。置き換えても並びが変わらないよう、前の作成日時を引き継ぐ
        let created = (try? fm.attributesOfItem(atPath: dir(of: id).path))?[.creationDate]
        try replace(dir(of: id), with: tmp)
        if let created { try? fm.setAttributes([.creationDate: created], ofItemAtPath: dir(of: id).path) }
        try? hash.write(to: hashFile(of: id), atomically: true, encoding: .utf8)
        reloadEntries()
        guard let entry = entries.first(where: { $0.id == id }) else { throw WebContentError.noManifest }
        return (entry, existing == nil ? .added : .replaced, existing != nil && id == activeId)
    }

    /// 使う中身を切り替える(1 つだけ)。呼んだ側でグラフ画面を読み込み直すこと。
    func activate(_ id: String) {
        guard entries.contains(where: { $0.id == id }) else { return }
        activeId = id
        UserSetting.setWebContentActive(id)
    }

    /// 取り込んだ zip を消す。同梱の標準版は消せない。使っていたものを消したら標準版に戻す(戻り値 true。グラフ画面を読み込み直すこと)。
    @discardableResult
    func remove(_ id: String) throws -> Bool {
        guard id != Self.builtInId, entries.contains(where: { $0.id == id }) else { return false }
        try fm.removeItem(at: dir(of: id))
        try? fm.removeItem(at: hashFile(of: id))
        let wasActive = id == activeId
        if wasActive { activate(Self.builtInId) }
        reloadEntries()
        return wasActive
    }

    /// 取り込んで、すぐ使う(自己テスト用の近道)
    @discardableResult
    func importAndActivate(_ zip: URL) throws -> WebContentManifest {
        let (entry, _, _) = try add(zip)
        activate(entry.id)
        return entry.manifest
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
        if hadOld {
            do { try fm.moveItem(at: dest, to: old) } catch {
                try? fm.removeItem(at: tmp)   // 前の中身をどけられなければ、展開したものも残さない
                throw error
            }
        }
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
    /// 取り込んだ zip 全部と、使っている ID を一時フォルダへ写す(自己テストが zip を取り込む前に。同じ name だと置き換えるため)
    func copyImportedAside() -> (dir: URL, activeId: String)? {
        let dst = fm.temporaryDirectory.appendingPathComponent("autotest-zips-\(UUID().uuidString)", isDirectory: true)
        do { try fm.copyItem(at: zipsDir, to: dst); return (dst, activeId) } catch {
            NSLog("[WebContent] copy zips aside: %@", error.localizedDescription); return nil
        }
    }

    /// copyImportedAside で写したものを戻し、使っていた ID に戻す
    func restoreImported(from kept: (dir: URL, activeId: String)) {
        try? fm.removeItem(at: zipsDir)
        do { try fm.moveItem(at: kept.dir, to: zipsDir) } catch {
            NSLog("[WebContent] restore zips: %@", error.localizedDescription)
            try? fm.createDirectory(at: zipsDir, withIntermediateDirectories: true)
        }
        UserSetting.setWebContentActive(kept.activeId)
        prepare()
    }
}
#endif
