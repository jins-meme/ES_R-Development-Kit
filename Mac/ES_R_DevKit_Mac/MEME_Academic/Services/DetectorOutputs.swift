//
//  DetectorOutputs.swift
//  MEME_Academic
//
//  グラフ画面の判定器から届く通知と演算結果の表(ページ → アプリの notify / table / records)。
//  仕様は webview/BRIDGE.md の Detector notifications and tables。Android の DetectorOutputs.kt と同じ作り。
//  規則(名前・列名の形、上限、文字列の扱い、ライブの間だけ受ける、同じ tag は 2 秒に 1 件)は webview/common/dev.js の
//  受け口の真似と同じ。**アプリは判定器の中身を知らない**(何を通知する・何列書くかはページが決める)。
//
//  - 受け付けるのは start(計測の開始)から stop まで。再生の解析中に届いたものは捨てる。
//  - 通知: UNUserNotificationCenter(許可はアプリの起動時に求める。DetectorNotifications)。同じ tag は 1 件に上書き
//    (上書きでも毎回出る)、2 秒に 1 件まで(超えたぶんは最後の 1 件だけ 2 秒後に出す)。tag は 1 計測 16 まで。
//    許可が無ければ出さない(計測は続ける)。アプリが前面にあっても出す。クリックでアプリを前面に出す。
//  - 表: 1 つを CSV 1 本に。データ CSV と同じベース名 + "_<name>"、同じフォルダ・同じ圧縮。最初の行が届いたときに作り、
//    100 行ごとに追記する(gzip は追記ごとに 1 メンバー。CsvManager と同じ)。改行はデータ CSV と同じ LF。
//  - **番号**: ページの i はアプリのサンプル番号(計測開始から 0, 1, 2 …。先頭パケットも数える。MEMEViewModel.liveSampleIndex)で、
//    データ CSV の NUM(端末のカウンタから数えた番号。パケットが落ちると飛ぶ)とは違う。表の NUM 列はデータ CSV の NUM に直して書き、
//    DATE はその行の DATE(直近 30 分を覚えておく)。i → NUM の差はパケットが落ちたときだけ変わるので、変わり目だけ全部覚えておく。
//

import Foundation
import AppKit
import UserNotifications

@MainActor
final class DetectorOutputs {

    /// start の features に入れる(このアプリが受け付けるページ → アプリの追加メッセージ)
    static let features = ["notify", "records"]

    private static let name = try! NSRegularExpression(pattern: "^[a-z][a-z0-9_]{0,31}$")
    private static let column = try! NSRegularExpression(pattern: "^[A-Z][A-Z0-9_]{0,31}$")
    private static let maxMessage = 64_000
    private static let maxTags = 16
    private static let maxTables = 16
    private static let tagInterval: TimeInterval = 2
    private static let flushRows = 100
    private static let dateKeepSeconds = 30 * 60

    /// 今の計測の番号(stop に渡して、前の計測の遅れた stop で次の計測を閉じないように)
    private(set) var session = 0
    private var active = false
    private var pageName: () -> String = { "" }
    /// データ CSV(最初の書き出しで作られる。それまでは nil)。計測を締めると呼び出し元は忘れるので、分かったら覚えておく(dataFileURL)
    private var dataFileSource: () -> URL? = { nil }
    private var knownDataFile: URL?
    fileprivate var dataFileURL: URL? {
        if knownDataFile == nil { knownDataFile = dataFileSource() }
        return knownDataFile
    }

    // サンプル番号 i → (NUM, DATE)。直近 30 分(i % 容量 の位置に置く)
    private var keyI: [Int] = []
    private var nums: [Int] = []
    private var dates: [Date] = []
    // i → NUM の差(NUM − i)の変わり目 [(その差になった最初の i, 差)]。30 分より古い i の NUM もこれで分かる
    private var offsets: [(i: Int, d: Int)] = []

    private var tables: [String: Table] = [:]
    private var tableOrder: [String] = []
    private var tags: [String: TagState] = [:]

    /// 最後に閉じた計測で作った表の CSV(保存ダイアログでデータ CSV と一緒に移す・自己テスト用)
    private(set) var lastFiles: [URL] = []

    /// 確かめる用(自己テスト): 受けた通知・出した通知・受けた行・アプリが裏の間に受けた行/通知・規則に合わず捨てたもの
    struct Stats {
        var notifyReceived = 0, notifyShown = 0, rowsReceived = 0, rowsInBackground = 0, notifyInBackground = 0
        /// データ CSV に行の無いサンプル(先頭パケット = i 0 は CSV に書かない)の行。NUM が無いので書かない
        var rowsNotInData = 0
        var warnings: [String] = []
        var json: [String: Any] {
            ["notifyReceived": notifyReceived, "notifyShown": notifyShown, "rowsReceived": rowsReceived, "rowsNotInData": rowsNotInData,
             "rowsInBackground": rowsInBackground, "notifyInBackground": notifyInBackground, "warnings": Array(warnings.suffix(10))]
        }
    }
    private(set) var stats = Stats()

    /// 計測の始まり。前の計測の表がまだ開いていれば閉じる。cps で i → DATE を覚える数(30 分ぶん)を決める
    @discardableResult
    func start(cps: Int, dataFile: @escaping () -> URL?, pageName: @escaping () -> String) -> Int {
        if active { stop() }
        session += 1
        self.dataFileSource = dataFile
        knownDataFile = nil
        self.pageName = pageName
        let cap = Self.dateKeepSeconds * max(cps, 1)
        keyI = Array(repeating: -1, count: cap)
        nums = Array(repeating: 0, count: cap)
        dates = Array(repeating: .distantPast, count: cap)
        offsets.removeAll()
        tables.removeAll(); tableOrder.removeAll()
        for t in tags.values { t.flush?.cancel() }
        tags.removeAll()
        lastFiles.removeAll()
        stats = Stats()
        active = true
        return session
    }

    /// データ CSV に 1 行書いた(i = ページへ渡したサンプル番号、num = その行の NUM、date = その行の DATE)
    func recordSample(i: Int, num: Int, date: Date) {
        guard active, i >= 0, !keyI.isEmpty else { return }
        let k = i % keyI.count
        keyI[k] = i; nums[k] = num; dates[k] = date
        if offsets.last?.d != num - i { offsets.append((i, num - i)) }
    }

    /// i の行の NUM(データ CSV に書かれていない i なら nil)
    private func numOf(_ i: Int) -> Int? {
        let k = i % max(keyI.count, 1)
        if !keyI.isEmpty, keyI[k] == i { return nums[k] }
        // 30 分より古い: 差の変わり目から(i より前の最後の変わり目の差)
        guard let first = offsets.first, i >= first.i else { return nil }
        var lo = 0, hi = offsets.count - 1
        while lo < hi { let mid = (lo + hi + 1) / 2; if offsets[mid].i <= i { lo = mid } else { hi = mid - 1 } }
        return i + offsets[lo].d
    }

    private func dateOf(_ i: Int) -> Date? {
        guard !keyI.isEmpty else { return nil }
        let k = i % keyI.count
        return keyI[k] == i ? dates[k] : nil
    }

    /// 計測の終わり。表の残りを書いて閉じ、作ったファイル(行のあった表)を返す。session を渡したときは、
    /// それが今の計測のときだけ閉じる(停止から少し待って閉じる間に、次の計測が始まっていることがある)。
    @discardableResult
    func stop(session: Int? = nil) -> [URL] {
        guard active, session == nil || session == self.session else { return [] }
        active = false
        for t in tags.values { t.flush?.cancel() }     // 2 秒待ちの通知は出さない(計測は終わった)
        tags.removeAll()
        lastFiles = tableOrder.compactMap { tables[$0]?.close() }
        NSLog("[DetectorOutputs] stop: tables %@ %@", tableOrder.map { "\($0)=\(tables[$0]?.rows ?? 0)" }.joined(separator: ", "),
              String(describing: stats.json))
        return lastFiles
    }

    /// 計測を締める前(データ CSV を書き切った直後)に呼ぶ。この後、呼び出し元はデータ CSV を忘れる(次の計測の用意)が、
    /// 表はページの残りを待ってから閉じるので、その場所を覚えておく
    func noteDataFile() { _ = dataFileURL }

    /// 保存ダイアログでデータ CSV を移したとき、表の CSV も同じフォルダ・同じベース名へ移す。
    /// まだ閉じていなければ(ページの残りを待つ間に保存した)ここで閉じる。
    func dataFileMoved(from source: URL, to destination: URL) {
        if active, dataFileURL?.standardizedFileURL == source.standardizedFileURL { stop() }
        let oldStem = Self.stem(source), newStem = Self.stem(destination)
        lastFiles = lastFiles.map { file in
            let name = file.lastPathComponent
            guard name.hasPrefix(oldStem + "_") else { return file }
            let to = destination.deletingLastPathComponent().appendingPathComponent(newStem + name.dropFirst(oldStem.count))
            do {
                if FileManager.default.fileExists(atPath: to.path) { try FileManager.default.removeItem(at: to) }
                try FileManager.default.moveItem(at: file, to: to)
                return to
            } catch {
                NSLog("[DetectorOutputs] could not move %@: %@", name, error.localizedDescription)
                return file
            }
        }
    }

    /// 表ごとの結果(自己テスト用。stop の後も次の start まで残る)
    var tablesJson: [[String: Any]] {
        tableOrder.compactMap { tables[$0] }.map {
            ["name": $0.name, "columns": $0.columns, "rows": $0.rows, "file": $0.file?.path ?? NSNull(), "bad": $0.bad]
        }
    }

    private func warn(_ text: String) {
        stats.warnings.append(text)
        NSLog("[DetectorOutputs] %@", text)
    }

    /// データ CSV の名前から拡張子(.csv / .csv.gz)を除いたもの
    private static func stem(_ url: URL) -> String {
        let name = url.lastPathComponent
        for ext in [".csv.gz", ".csv"] where name.lowercased().hasSuffix(ext) { return String(name.dropLast(ext.count)) }
        return (name as NSString).deletingPathExtension
    }

    // MARK: - ページ → アプリ

    /// WebBridge から(kind は notify / table / records)
    func receive(_ body: [String: Any]) {
        let kind = body["kind"] as? String ?? ""
        guard active else { NSLog("[DetectorOutputs] %@: not in a live measurement, ignored", kind); return }
        if let data = try? JSONSerialization.data(withJSONObject: body), data.count > Self.maxMessage {
            return warn("\(kind): message over \(Self.maxMessage) characters, ignored")
        }
        switch kind {
        case "notify": notify(body)
        case "table": table(body)
        case "records": records(body)
        default: break
        }
    }

    private static func matches(_ re: NSRegularExpression, _ s: String) -> Bool {
        re.firstMatch(in: s, range: NSRange(s.startIndex..., in: s)) != nil
    }

    /// 1 行の文字列(タイトル・本文・表題): 長さと制御文字を見る。合わなければ nil
    private static func text(_ v: Any?, max: Int) -> String? {
        guard let s = v as? String, s.count <= max,
              !s.unicodeScalars.contains(where: { $0.value < 0x20 || $0.value == 0x7f }) else { return nil }
        return s
    }

    /// JS の真偽値は NSNumber で届くので、数とは型で分ける
    private static func isBool(_ n: NSNumber) -> Bool { CFGetTypeID(n) == CFBooleanGetTypeID() }

    /// 0 以上の整数(サンプル番号)。合わなければ nil
    private static func index(_ v: Any?) -> Int? {
        guard let n = v as? NSNumber, !isBool(n) else { return nil }
        let d = n.doubleValue
        guard d.isFinite, d >= 0, d == d.rounded(), d < 9e15 else { return nil }
        return Int(d)
    }

    // MARK: - 通知

    private struct Notice { let tag: String; let title: String; let text: String? }

    @MainActor private final class TagState {
        var last: Date = .distantPast          // 最後に出した時刻
        var pending: Notice?
        var flush: DispatchWorkItem?
    }

    private func notify(_ o: [String: Any]) {
        stats.notifyReceived += 1
        if !NSApp.isActive { stats.notifyInBackground += 1 }
        guard let tag = o["tag"] as? String, Self.matches(Self.name, tag) else { return warn("notify: bad tag \(o["tag"] ?? "nil")") }
        guard let title = Self.text(o["title"], max: 64) else {
            return warn("notify \(tag): title is required (at most 64 characters, no control characters)")
        }
        var body: String?
        if let t = o["text"], !(t is NSNull) {
            guard let s = Self.text(t, max: 200) else { return warn("notify \(tag): text must be at most 200 characters without control characters") }
            body = s
        }
        if let i = o["i"], !(i is NSNull), Self.index(i) == nil { return warn("notify \(tag): i must be a sample number") }
        if tags[tag] == nil && tags.count >= Self.maxTags { return warn("notify: more than \(Self.maxTags) tags, \(tag) ignored") }
        let st = tags[tag] ?? { let s = TagState(); tags[tag] = s; return s }()
        let n = Notice(tag: tag, title: title, text: body)
        let wait = st.last.addingTimeInterval(Self.tagInterval).timeIntervalSinceNow
        if wait <= 0 { show(st, n); return }
        // 2 秒以内の続き: 最後の 1 件だけを後で出す
        st.pending = n
        if st.flush == nil {
            let item = DispatchWorkItem { [weak self, weak st] in
                guard let self, let st, let p = st.pending else { return }
                st.pending = nil; st.flush = nil
                self.show(st, p)
            }
            st.flush = item
            DispatchQueue.main.asyncAfter(deadline: .now() + wait, execute: item)
        }
    }

    private func show(_ st: TagState, _ n: Notice) {
        st.last = Date()
        DetectorNotifications.post(tag: n.tag, title: n.title, text: n.text) { [weak self] shown in
            if shown { self?.stats.notifyShown += 1 }
        }
    }

    // MARK: - 表

    @MainActor private final class Table {
        let name: String
        let columns: [String]
        let title: String
        unowned let owner: DetectorOutputs
        var file: URL?
        var rows = 0
        var bad = false                        // 違う列で宣言し直された(以後の行は捨てる)
        private var buf: [String] = []
        private var header = true

        init(name: String, columns: [String], title: String, owner: DetectorOutputs) {
            self.name = name; self.columns = columns; self.title = title; self.owner = owner
        }

        func add(_ line: String) {
            buf.append(line)
            rows += 1
            if buf.count >= DetectorOutputs.flushRows { flush() }
        }

        /// 溜めた行を書く。データ CSV がまだ無ければ(最初の書き出しの前)溜めたまま待つ
        func flush() {
            guard !buf.isEmpty else { return }
            if file == nil {
                guard let data = owner.dataFileURL else { return }
                let gz = CsvFile.isGzip(data)
                let stem = DetectorOutputs.stem(data)
                file = data.deletingLastPathComponent().appendingPathComponent("\(stem)_\(name).\(CsvFile.saveExtension(compressed: gz))")
            }
            guard let file else { return }
            var text = ""
            if header {
                let t = title.isEmpty ? "" : " (\(title))"
                text += "// Detector output  : \(name)\(t)\n"
                text += "// Page  : \(owner.pageName())\n"
                text += "// Data file  : \(owner.dataFileURL?.lastPathComponent ?? "")\n"
                text += "//\n"
                text += "//" + (["NUM", "DATE"] + columns).joined(separator: ",") + "\n"
            }
            for l in buf { text += l + "\n" }
            do {
                let utf8 = Data(text.utf8)
                // データ CSV と同じく、1 回の追記ごとに独立した gzip メンバを書く(落ちてもそこまでは読める。CsvManager)
                let payload = CsvFile.isGzip(file) ? try Gzip.compress(utf8) : utf8
                if header {
                    try payload.write(to: file, options: .atomic)
                } else {
                    let h = try FileHandle(forWritingTo: file)
                    defer { try? h.close() }
                    try h.seekToEnd()
                    try h.write(contentsOf: payload)
                }
                header = false
            } catch {
                owner.warn("table \(name): write failed: \(error.localizedDescription)")
            }
            buf.removeAll()
        }

        func close() -> URL? {
            flush()
            if !buf.isEmpty { owner.warn("table \(name): no data CSV, \(buf.count) row(s) not written"); buf.removeAll() }
            return file
        }
    }

    private func table(_ o: [String: Any]) {
        guard let name = o["name"] as? String, Self.matches(Self.name, name), name != "disconnect" else {
            return warn("table: bad name \(o["name"] ?? "nil")")
        }
        let raw = o["columns"] as? [Any]
        let columns = raw?.compactMap { $0 as? String } ?? []
        guard let raw, raw.count == columns.count, (1...32).contains(columns.count),
              columns.allSatisfy({ Self.matches(Self.column, $0) && $0 != "NUM" && $0 != "DATE" }),
              Set(columns).count == columns.count else {
            return warn("table \(name): bad columns \(o["columns"] ?? "nil")")
        }
        var title = ""
        if let t = o["title"], !(t is NSNull) {
            guard let s = Self.text(t, max: 64) else { return warn("table \(name): title must be at most 64 characters without control characters") }
            title = s
        }
        if let old = tables[name] {
            // 宣言し直し(WebView のプロセスが落ちて読み込み直した・start を送り直した): 同じ列なら同じファイルへ続ける
            if old.columns != columns && !old.bad { old.bad = true; warn("table \(name): declared again with other columns; its rows are dropped") }
            return
        }
        if tables.count >= Self.maxTables { return warn("table: more than \(Self.maxTables) tables, \(name) ignored") }
        tables[name] = Table(name: name, columns: columns, title: title, owner: self)
        tableOrder.append(name)
    }

    private func records(_ o: [String: Any]) {
        let name = o["name"] as? String ?? ""
        guard let t = tables[name] else { return warn("records: table \(name) was not declared") }
        if t.bad { return }
        guard let rows = o["rows"] as? [Any] else { return warn("records \(name): rows must be an array") }
        var dropped = 0
        let background = !NSApp.isActive
        for r in rows {
            guard let r = r as? [Any], r.count == t.columns.count + 1, let i = Self.index(r[0]) else { dropped += 1; continue }
            let cells = r.dropFirst().map(Self.cell)
            if cells.contains(where: { $0 == nil }) { dropped += 1; continue }
            // NUM はデータ CSV の NUM に直し、DATE はその行の DATE(直近 30 分)。データ CSV に行の無いサンプルは書かない
            guard let num = numOf(i) else { stats.rowsNotInData += 1; continue }
            let date = dateOf(i).map(DataPersistenceService.formatDate) ?? ""
            t.add(([String(num), date] + cells.map { $0! }).joined(separator: ","))
            stats.rowsReceived += 1
            if background { stats.rowsInBackground += 1 }
        }
        if dropped > 0 {
            warn("records \(name): \(dropped) row(s) dropped (need [i, \(t.columns.count) values], values: number, string or null)")
        }
    }

    /// 値 1 つ → CSV の欄。数は JSON の数のまま、文字列は 64 文字・区切りと改行は空白・数式に読まれる書き出しは空欄。それ以外は nil
    private static func cell(_ v: Any) -> String? {
        if v is NSNull { return "" }
        if let n = v as? NSNumber {
            if isBool(n) { return nil }
            let d = n.doubleValue
            guard d.isFinite else { return nil }
            if d == d.rounded(), abs(d) < 1e15 { return String(Int64(d)) }
            return String(d)
        }
        if let s = v as? String {
            let t = String(s.prefix(64)).map { $0 == "," || $0 == "\r" || $0 == "\n" ? " " : String($0) }.joined()
            if let c = t.first, "=+-@".contains(c) { return "" }
            return t
        }
        return nil
    }
}

// MARK: - 通知センター

/// UNUserNotificationCenter の受け持ち。許可はアプリの起動時に求め(requestAuthorization)、アプリが前面にあっても
/// バナーを出し(willPresent)、クリックでアプリを前面に出す(didReceive)。
/// 識別子は "detector.<tag>" なので、同じ tag の新しい通知は前のものを置き換える。
final class DetectorNotifications: NSObject, UNUserNotificationCenterDelegate, @unchecked Sendable {   // 状態を持たない

    static let shared = DetectorNotifications()

    /// 起動時に呼ぶ(AppDelegate)。拒否されていても計測には関係ない
    @MainActor
    static func setUp() {
        let center = UNUserNotificationCenter.current()
        center.delegate = shared
        center.requestAuthorization(options: [.alert, .sound]) { granted, error in
            NSLog("[DetectorNotifications] authorization granted=%@ %@", granted ? "yes" : "no", error?.localizedDescription ?? "")
        }
    }

    /// 出す(許可が無ければ出さず、done(false))
    @MainActor
    static func post(tag: String, title: String, text: String?, done: @escaping @MainActor (Bool) -> Void) {
        let center = UNUserNotificationCenter.current()
        center.getNotificationSettings { settings in
            let allowed = settings.authorizationStatus == .authorized || settings.authorizationStatus == .provisional
            guard allowed else {
                NSLog("[DetectorNotifications] not shown (not allowed): [%@] %@", tag, title)
                Task { @MainActor in done(false) }
                return
            }
            let content = UNMutableNotificationContent()
            content.title = title
            if let text { content.body = text }
            content.sound = .default
            content.threadIdentifier = "detector.\(tag)"
            let request = UNNotificationRequest(identifier: "detector.\(tag)", content: content, trigger: nil)
            center.add(request) { error in
                if let error { NSLog("[DetectorNotifications] add failed: %@", error.localizedDescription) }
                Task { @MainActor in done(error == nil) }
            }
        }
    }

    // アプリが前面にあってもバナーを出す
    func userNotificationCenter(_ center: UNUserNotificationCenter, willPresent notification: UNNotification,
                                withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void) {
        completionHandler([.banner, .list, .sound])
    }

    // クリックでアプリを前面に出す
    func userNotificationCenter(_ center: UNUserNotificationCenter, didReceive response: UNNotificationResponse,
                                withCompletionHandler completionHandler: @escaping () -> Void) {
        Task { @MainActor in
            NSApp.activate(ignoringOtherApps: true)
            NSApp.windows.first { $0.canBecomeMain }?.makeKeyAndOrderFront(nil)
        }
        completionHandler()
    }
}
