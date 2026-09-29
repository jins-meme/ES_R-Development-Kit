//
//  DebugAutoTest.swift
//  MEME_Academic
//
//  Debug ビルドだけの自己テスト。環境変数 MEME_AUTOTEST_DIR にフォルダを渡して起動すると、
//  モック(MEME_Academic Mock スキーム)で「スキャン → 接続 → 計測(Full・100Hz)→ アーティファクト → 停止 →
//  保存した CSV を再生」を自動で回し、グラフ画面のスナップショット(PNG)と結果(result.json)をそのフォルダに書いて終わる。
//  画面を操作できない環境(CI・Claude Code)から、グラフ画面が動いていることを確かめるため。
//
//      MEME_AUTOTEST_DIR=/tmp/autotest build/…/MEME_Academic.app/Contents/MacOS/MEME_Academic -mock
//
//  **-mock を付けないと動かない**(付けずに実行ファイルを直接起動すると本物の BLE になり、近くの実機に繋いで計測してしまう)。
//
//  MEME_AUTOTEST_SUITE=settings MEME_AUTOTEST_REAL=1 を足すと、実機で計測条件(Mode / Trans Speed / Accel Range /
//  Gyro Range)を切り替えて短く計測し、保存した CSV のヘッダ・列・行数・送信頻度・静止時の加速度の大きさ(レンジで換算して
//  約 1 G になるか = 端末に実際に効いたか)を確かめる。実機を使うのは MEME_AUTOTEST_REAL=1 を明示したときだけ。
//
//  MEME_AUTOTEST_ZIP=<zip> を足すと、始める前に設定の Display Engine と同じ経路(WebContentStore.importZip)でその zip を読み込み、
//  終わったら元の中身に戻す(高機能版 advanced.zip の確かめ用。ページに検出器があれば、その状態も result.json に書く)。
//
//  MEME_AUTOTEST_SUITE=zip MEME_AUTOTEST_BADZIPS=<フォルダ> は、フォルダの中の zip を 1 つずつ読み込み、
//  名前が good で始まるものは通り、それ以外は断られて今の中身が変わらず、展開先の外に何も書かれないことを見る
//  (zip slip・zip 爆弾などの検査。悪い zip は webview/tools/make_bad_zips.py が作る)。
//
//  リリースビルドには入らない(#if DEBUG)。
//

#if DEBUG
import AppKit
import WebKit

@MainActor
enum DebugAutoTest {

    static func startIfRequested(_ vm: MEMEViewModel) {
        guard let dir = ProcessInfo.processInfo.environment["MEME_AUTOTEST_DIR"] else { return }
        let out = URL(fileURLWithPath: dir, isDirectory: true)
        try? FileManager.default.createDirectory(at: out, withIntermediateDirectories: true)
        Task { @MainActor in
            var result: [String: Any] = ["mock": MEMELibFactory.isMock]
            // 保存先はテスト用のフォルダにする(利用者の保存先を汚さない)。終わったら戻す
            let savedPath = UserSetting.getSaveFilePath()
            let csvDir = out.appendingPathComponent("csv", isDirectory: true)
            try? FileManager.default.createDirectory(at: csvDir, withIntermediateDirectories: true)
            UserSetting.setSaveFilePath(csvDir.path)
            let store = WebContentStore.shared
            let wasCustom = store.source == .custom
            do {
                let env = ProcessInfo.processInfo.environment
                if let zip = env["MEME_AUTOTEST_ZIP"] {
                    let m = try store.importZip(URL(fileURLWithPath: zip))
                    result["zip"] = m.displayName
                    vm.reloadGraph()
                }
                if env["MEME_AUTOTEST_SUITE"] == "zip" {
                    try await runZip(vm, dir: URL(fileURLWithPath: env["MEME_AUTOTEST_BADZIPS"] ?? ""), result: &result)
                } else if env["MEME_AUTOTEST_SUITE"] == "settings" {
                    guard MEMELibFactory.isMock || env["MEME_AUTOTEST_REAL"] == "1" else {
                        throw Timeout(what: "refused: settings suite needs -mock or MEME_AUTOTEST_REAL=1")
                    }
                    try await runSettings(vm, out: out, csvDir: csvDir, result: &result)
                } else {
                    guard MEMELibFactory.isMock else { throw Timeout(what: "refused: run with -mock (would connect to a real device)") }
                    try await run(vm, out: out, result: &result)
                }
                result["ok"] = true
            } catch {
                result["ok"] = false
                result["error"] = String(describing: error)
            }
            if let data = try? JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys]) {
                try? data.write(to: out.appendingPathComponent("result.json"))
            }
            UserSetting.setSaveFilePath(savedPath)   // terminate は戻らないので defer でなくここで戻す
            if ProcessInfo.processInfo.environment["MEME_AUTOTEST_ZIP"] != nil || ProcessInfo.processInfo.environment["MEME_AUTOTEST_SUITE"] == "zip",
               !wasCustom {
                store.useBundled()                    // 読み込んだ zip を残さない(もともと選んだ zip を使っていたらそのまま)
            }
            NSApp.terminate(nil)
        }
    }

    private struct Timeout: Error, CustomStringConvertible { let what: String; var description: String { "timeout: \(what)" } }

    private static func sleep(_ s: Double) async { try? await Task.sleep(nanoseconds: UInt64(s * 1e9)) }

    private static func wait(_ what: String, _ seconds: Double = 15, _ cond: () -> Bool) async throws {
        let end = Date().addingTimeInterval(seconds)
        while !cond() {
            if Date() > end { throw Timeout(what: what) }
            await sleep(0.1)
        }
    }

    private static func snapshot(_ vm: MEMEViewModel, _ name: String, _ out: URL) async {
        guard let image = try? await vm.web.webView.takeSnapshot(configuration: nil),
              let tiff = image.tiffRepresentation, let rep = NSBitmapImageRep(data: tiff),
              let png = rep.representation(using: .png, properties: [:]) else { return }
        try? png.write(to: out.appendingPathComponent("\(name).png"))
    }

    /// ページの状態(グラフの枚数・見出し・メッセージ・ステータス・アーティファクトの数)
    /// MEME_AUTOTEST_PROBE に書いた JS の式をページで評価した値(調べもの用。Promise も待つ)
    private static func probe(_ vm: MEMEViewModel) async -> Any {
        guard let expr = ProcessInfo.processInfo.environment["MEME_AUTOTEST_PROBE"] else { return NSNull() }
        do {
            let v = try await vm.web.webView.callAsyncJavaScript("return JSON.stringify(await (\(expr)))", contentWorld: .page)
            return (v as? String) ?? String(describing: v)
        } catch { return "probe error: \(error)" }
    }

    private static func pageState(_ vm: MEMEViewModel) async -> Any {
        let js = """
        JSON.stringify({charts: [...document.querySelectorAll('.chart .ctitle')].map(e => e.textContent),
          message: document.querySelector('.message')?.hidden ? '' : document.querySelector('.message')?.textContent,
          status: document.querySelector('.status')?.textContent, ready: !!window.jmasHost,
          detector: window.jmasEngine ? {state: jmasEngine.state, error: jmasEngine.error, fed: jmasEngine.fed, rows: jmasEngine.store?.n,
                                         final: jmasEngine.finalSent, counts: jmasEngine.counts} : null})
        """
        guard let s = try? await vm.web.webView.evaluateJavaScript(js) as? String,
              let obj = try? JSONSerialization.jsonObject(with: Data(s.utf8)) else { return NSNull() }
        return obj
    }

    private static func run(_ vm: MEMEViewModel, out: URL, result: inout [String: Any]) async throws {
        try await wait("page ready", 20) { vm.web.isReady }
        result["page"] = vm.web.pageName
        await snapshot(vm, "0-idle", out)

        // 接続して Full・100Hz で計測
        vm.startScan()
        try await wait("device found") { vm.phase == .deviceFound }
        vm.toggleConnect()
        try await wait("connected") { vm.phase == .connected }
        vm.selectMode = Int(MEMEMode_Full) - 1
        vm.transSpeed = 0
        vm.accelRange = 2
        vm.gyroRange = 2
        vm.toggleMeasurement()
        try await wait("measuring") { vm.phase == .measuring }
        await sleep(8)
        if try await hasDetector(vm) {                        // 高機能版: 検出器が読み込めて、届いた行に追いついている
            try await waitJs(vm, "detector live", 30, "jmasEngine.state === 'ready' && jmasEngine.fed > 0 && jmasEngine.store.n - jmasEngine.fed < 200")
        }
        result["measuring"] = await pageState(vm)
        result["probeMeasuring"] = await probe(vm)
        await snapshot(vm, "1-measuring", out)

        // ページで付けたのと同じ形でアーティファクトを送る(サンプル 300 = CSV の 299 行目)
        _ = try? await vm.web.webView.evaluateJavaScript(
            "window.webkit.messageHandlers.jmas.postMessage({kind: 'artifact', i: 300, text: 'autotest'}); 0")
        await sleep(0.5)

        vm.toggleMeasurement()
        try await wait("stopped") { vm.phase == .connected }
        await sleep(1.5)

        // 保存した CSV(保存先で一番新しいもの)に書き戻されたか
        let dir = URL(fileURLWithPath: UserSetting.getSaveFilePath())
        let files = (try? FileManager.default.contentsOfDirectory(at: dir, includingPropertiesForKeys: [.contentModificationDateKey])) ?? []
        let csv = files.filter { CsvFile.isSupported(fileName: $0.lastPathComponent) }
            .max { a, b in
                let da = (try? a.resourceValues(forKeys: [.contentModificationDateKey]).contentModificationDate) ?? .distantPast
                let db = (try? b.resourceValues(forKeys: [.contentModificationDateKey]).contentModificationDate) ?? .distantPast
                return da < db
            }
        guard let csv else { throw Timeout(what: "saved csv") }
        result["savedCsv"] = csv.path
        let text = (try? CsvFile.readText(at: csv)) ?? ""
        let dataLines = text.components(separatedBy: "\n").drop { !$0.hasPrefix("//ARTIFACT") }.dropFirst().filter { !$0.isEmpty }
        result["savedRows"] = dataLines.count
        result["artifactRow"] = dataLines.firstIndex { $0.hasPrefix("autotest,") }.map { $0 - dataLines.startIndex } ?? -1

        // 再生
        vm.toggleConnect()
        try await wait("disconnected") { vm.phase == .idle }
        vm.openReplayFile(url: csv)
        await sleep(4)
        if try await hasDetector(vm) {                        // 高機能版: 再生したファイルを最後まで解析し終える
            try await waitJs(vm, "detector replay", 60, "jmasEngine.finalSent === true")
        }
        result["replay"] = await pageState(vm)
        result["probeReplay"] = await probe(vm)
        await snapshot(vm, "2-replay", out)
        vm.toggleConnect()
        await sleep(0.5)
    }

    private static func hasDetector(_ vm: MEMEViewModel) async throws -> Bool {
        (try? await vm.web.webView.evaluateJavaScript("!!window.jmasEngine") as? Bool) ?? false
    }

    /// ページの式 expr が true になるまで待つ
    private static func waitJs(_ vm: MEMEViewModel, _ what: String, _ seconds: Double, _ expr: String) async throws {
        let end = Date().addingTimeInterval(seconds)
        while (try? await vm.web.webView.evaluateJavaScript("!!(\(expr))") as? Bool) != true {
            if Date() > end { throw Timeout(what: what) }
            await sleep(0.2)
        }
    }

    // MARK: - zip の検査(悪い zip を断るか)

    private static func runZip(_ vm: MEMEViewModel, dir: URL, result: inout [String: Any]) async throws {
        let store = WebContentStore.shared
        let fm = FileManager.default
        let zips = ((try? fm.contentsOfDirectory(at: dir, includingPropertiesForKeys: nil)) ?? [])
            .filter { $0.pathExtension == "zip" }.sorted { $0.lastPathComponent < $1.lastPathComponent }
        guard !zips.isEmpty else { throw Timeout(what: "no zips in MEME_AUTOTEST_BADZIPS") }
        // 展開先の外に書かれたら見つかるように、WebContent の上と zip のフォルダの中身を覚えておく
        let watch = [store.root.deletingLastPathComponent(), store.root, dir]
        func listing() -> Set<String> {
            Set(watch.flatMap { d in ((try? fm.contentsOfDirectory(atPath: d.path)) ?? []).map { d.path + "/" + $0 } })
                .filter { !$0.contains("/tmp-") && !$0.contains("/old-") && !$0.hasSuffix("/custom") }   // custom = 受け入れた zip の置き場
        }
        var rows: [[String: Any]] = []
        for zip in zips {
            let name = zip.lastPathComponent
            let before = (store.source, store.manifest)
            let beforeFiles = listing()
            let t0 = Date()
            var row: [String: Any] = ["zip": name]
            do {
                let m = try store.importZip(zip)
                row["accepted"] = m.displayName
            } catch {
                row["refused"] = error.localizedDescription
                row["unchanged"] = before.0 == store.source && before.1 == store.manifest
            }
            row["ms"] = Int(Date().timeIntervalSince(t0) * 1000)
            let extra = listing().subtracting(beforeFiles)
            if !extra.isEmpty { row["writtenOutside"] = Array(extra).sorted() }
            let leftovers = ((try? fm.contentsOfDirectory(atPath: store.root.path)) ?? []).filter { $0.hasPrefix("tmp-") }
            if !leftovers.isEmpty { row["tmpLeft"] = leftovers }
            let wantAccept = name.hasPrefix("good")
            row["ok"] = (row["accepted"] != nil) == wantAccept && row["writtenOutside"] == nil && row["tmpLeft"] == nil
                && (wantAccept || row["unchanged"] as? Bool == true)
            rows.append(row)
        }
        result["zips"] = rows
        result["allOk"] = rows.allSatisfy { $0["ok"] as? Bool == true }
        vm.reloadGraph()
    }

    // MARK: - 計測条件の切り替え(実機)

    private struct Combo { let mode: Int; let speed: Int; let acc: Int; let gyro: Int }   // 画面の選択肢の番号

    private static let combos: [Combo] = [
        Combo(mode: 1, speed: 0, acc: 0, gyro: 0),   // Full 100Hz ±2G ±250dps
        Combo(mode: 1, speed: 1, acc: 1, gyro: 1),   // Full 50Hz ±4G ±500dps
        Combo(mode: 0, speed: 0, acc: 2, gyro: 2),   // Standard 100Hz ±8G ±1000dps
        Combo(mode: 0, speed: 1, acc: 3, gyro: 3),   // Standard 50Hz ±16G ±2000dps
        Combo(mode: 1, speed: 0, acc: 3, gyro: 3),   // Full 100Hz ±16G ±2000dps
        Combo(mode: 2, speed: 0, acc: 2, gyro: 2),   // Quaternion 100Hz
    ]

    private static func runSettings(_ vm: MEMEViewModel, out: URL, csvDir: URL, result: inout [String: Any]) async throws {
        try await wait("page ready", 20) { vm.web.isReady }
        vm.startScan()
        try await wait("device found", 30) { vm.phase == .deviceFound }
        result["device"] = vm.selectedDevice
        vm.toggleConnect()
        try await wait("connected", 30) { vm.phase == .connected }
        result["memeVersion"] = vm.memeVersionText
        var rows: [[String: Any]] = []
        // MEME_AUTOTEST_COMBOS="mode,speed,acc,gyro;…"(画面の選択肢の番号)で組み合わせを差し替え、MEME_AUTOTEST_SECONDS で 1 回の長さ
        let env = ProcessInfo.processInfo.environment
        let list = env["MEME_AUTOTEST_COMBOS"].map { spec in
            spec.split(separator: ";").compactMap { part -> Combo? in
                let v = part.split(separator: ",").compactMap { Int($0.trimmingCharacters(in: .whitespaces)) }
                return v.count == 4 ? Combo(mode: v[0], speed: v[1], acc: v[2], gyro: v[3]) : nil
            }
        } ?? combos
        let seconds = Double(env["MEME_AUTOTEST_SECONDS"] ?? "") ?? 6
        for (k, c) in list.enumerated() {
            vm.selectMode = c.mode; vm.transSpeed = c.speed; vm.accelRange = c.acc; vm.gyroRange = c.gyro
            let want: [String: Any] = ["mode": vm.selectModeOptions[c.mode], "speed": vm.transSpeedOptions[c.speed],
                                       "acc": vm.accelRangeOptions[c.acc], "gyro": vm.gyroRangeOptions[c.gyro]]
            let before = Set((try? FileManager.default.contentsOfDirectory(atPath: csvDir.path)) ?? [])
            let t0 = Date()
            vm.toggleMeasurement()
            try await wait("measuring \(k)") { vm.phase == .measuring }
            await sleep(seconds)
            let page = await pageState(vm)
            let successRate = vm.successRateText
            await snapshot(vm, "settings-\(k)", out)
            vm.toggleMeasurement()
            try await wait("stopped \(k)") { vm.phase == .connected }
            await sleep(1.5)
            let after = Set((try? FileManager.default.contentsOfDirectory(atPath: csvDir.path)) ?? [])
            var row: [String: Any] = ["want": want, "page": page, "seconds": Date().timeIntervalSince(t0), "successRate": successRate]
            if let name = after.subtracting(before).sorted().last {
                row["csv"] = name
                row["check"] = checkCsv(csvDir.appendingPathComponent(name), want: want)
            } else {
                row["check"] = ["ok": false, "problem": "no new CSV"]
            }
            rows.append(row)
            if vm.phase != .connected { break }                     // 切れたらやめる
        }
        result["settings"] = rows
        result["allOk"] = rows.allSatisfy { (($0["check"] as? [String: Any])?["ok"] as? Bool) == true }
        vm.toggleConnect()
        await sleep(1)
    }

    /// 保存した CSV のヘッダ・列・行数・送信頻度・静止時の加速度の大きさを見る。
    private static func checkCsv(_ url: URL, want: [String: Any]) -> [String: Any] {
        guard let text = try? CsvFile.readText(at: url) else { return ["ok": false, "problem": "unreadable"] }
        let lines = text.components(separatedBy: "\n")
        func header(_ prefix: String) -> String? {
            lines.first { $0.hasPrefix(prefix) }.flatMap { $0.split(separator: ":", maxSplits: 1).last }
                .map { $0.trimmingCharacters(in: .whitespaces) }
        }
        let mode = header("// Data mode") ?? "", speed = header("// Transmission speed") ?? ""
        let acc = header("// Acceleration sensor's range") ?? "", gyro = header("// Gyroscope sensor's range") ?? ""
        guard let hi = lines.firstIndex(where: { $0.hasPrefix("//ARTIFACT") }) else { return ["ok": false, "problem": "no //ARTIFACT"] }
        let columns = lines[hi].dropFirst(2).split(separator: ",").map(String.init)
        let data = lines[(hi + 1)...].filter { !$0.isEmpty }.map { $0.split(separator: ",", omittingEmptySubsequences: false).map(String.init) }
        var problems: [String] = []
        let wantMode = want["mode"] as! String
        if mode != wantMode { problems.append("mode \(mode) != \(wantMode)") }
        if speed != (want["speed"] as! String) { problems.append("speed \(speed) != \(want["speed"]!)") }
        let wantAcc = (want["acc"] as! String).replacingOccurrences(of: "±", with: "").lowercased()
        let wantGyro = (want["gyro"] as! String).replacingOccurrences(of: "±", with: "")
        if acc != wantAcc { problems.append("acc \(acc) != \(wantAcc)") }
        if gyro != wantGyro { problems.append("gyro \(gyro) != \(wantGyro)") }
        let wantCols: [String] = wantMode == "Full" ? ["ACC_X", "ACC_Y", "ACC_Z", "GYRO_X", "GYRO_Y", "GYRO_Z", "EOG_L", "EOG_R", "EOG_H", "EOG_V"]
            : wantMode == "Standard" ? ["ACC_X", "ACC_Y", "ACC_Z", "EOG_L1", "EOG_R1", "EOG_L2", "EOG_R2", "EOG_H1", "EOG_H2", "EOG_V1", "EOG_V2"]
            : ["QUATERNION_W", "QUATERNION_X", "QUATERNION_Y", "QUATERNION_Z"]
        if Array(columns.dropFirst(3)) != wantCols { problems.append("columns \(columns)") }
        if data.contains(where: { $0.count != columns.count }) { problems.append("rows with wrong field count") }
        // 送信頻度(DATE 列の最初と最後)
        let f = DateFormatter(); f.locale = Locale(identifier: "en_US_POSIX"); f.timeZone = TimeZone(secondsFromGMT: 0)
        f.dateFormat = "yyyy/MM/dd HH:mm:ss.SS"
        var hz: Double = 0
        if data.count > 10, let a = f.date(from: data.first![2]), let b = f.date(from: data.last![2]), b > a {
            hz = Double(data.count - 1) / b.timeIntervalSince(a)
        }
        let wantHz = (want["speed"] as! String) == "100Hz" ? 100.0 : 50.0
        if abs(hz - wantHz) > wantHz * 0.15 { problems.append(String(format: "rate %.1f Hz != %.0f", hz, wantHz)) }
        // 静止時の加速度の大きさ(レンジで換算)。端末に効いていれば約 1 G(外し方・動きで多少ぶれる)
        var accG: Double = -1
        if let ix = columns.firstIndex(of: "ACC_X"), let range = Double(wantAcc.replacingOccurrences(of: "g", with: "")) {
            let mags = data.dropFirst(5).compactMap { r -> Double? in
                guard let x = Double(r[ix]), let y = Double(r[ix + 1]), let z = Double(r[ix + 2]) else { return nil }
                return (x * x + y * y + z * z).squareRoot() * range / 32768
            }.sorted()
            if !mags.isEmpty { accG = mags[mags.count / 2] }
            if accG < 0.7 || accG > 1.3 { problems.append(String(format: "|acc| median %.2f G (expected ~1 G)", accG)) }
        }
        return ["ok": problems.isEmpty, "problems": problems, "header": ["mode": mode, "speed": speed, "acc": acc, "gyro": gyro],
                "rows": data.count, "hz": (hz * 10).rounded() / 10, "accG": (accG * 100).rounded() / 100]
    }
}
#endif
