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
//  MEME_AUTOTEST_REAL=1 を足すと(-mock なし)、同じ流れを実機で回す。MEME_AUTOTEST_DEVICE で端末を選ぶ
//  (16 進(例 E90D)ならアドレスの末尾。スキャンではアドレスが見えず、同じ広告名の端末は 1 つにまとめられるので、繋いでから
//  端末のアドレスを確かめ、違えば切ってスキャンし直す(4 回まで)。それ以外は広告名の末尾(例 ESRG2_5)。省略時は最初に見つかったもの)、MEME_AUTOTEST_MODE=full|standard で計測モード(既定 full。100Hz・±8G・±1000dps)、
//  MEME_AUTOTEST_SECONDS で計測の長さ(既定 8 秒)。保存した CSV のモード・番号の抜け・アーティファクトの行も result.json に書く。
//
//  MEME_AUTOTEST_ZIP=<zip> を足すと、始める前に Display Engine ダイアログと同じ経路(WebContentStore.add → activate)でその zip を取り込んで使い、
//  終わったら元の中身に戻す(高機能版 advanced.zip の確かめ用。ページに検出器があれば、その状態も result.json に書く)。
//  取り込んである zip の一覧と使っている 1 つは、退避しておいて戻す(同じ name の zip は置き換えになるため。zip の組も同じ)。
//
//  MEME_AUTOTEST_OUTPUTS=1 を足すと(MEME_AUTOTEST_ZIP で高機能版を読み込んだとき)、計測の前に高機能版の設定の Notify(立ち座り)・
//  CSV(高さ・速度)をオンにして読み込み直し、判定器の表の CSV(データ CSV と同じベース名 + _hve・同じ圧縮)ができたこと・行数が 0 でないこと・
//  ページが送った行の数とアプリが受けた数が同じこと・NUM がデータ CSV の範囲に入ること・DATE が入っていること・規則に合わず捨てたものが無いこと、
//  保存ダイアログで移したときに表も同じフォルダ・同じベース名へ移ることを outputs に書く(Android の autotest_outputs と同じ。
//  webview/BRIDGE.md の Detector notifications and tables)。
//
//  MEME_AUTOTEST_SOCKET_PORT=<ポート> を足すと、テストの間だけ TCP 出力(Settings の TCP Output)をそのポートで有効にする
//  (受け取る側は外で用意する。例: nc localhost <ポート> > out.csv)。終わったら元の設定に戻す。
//
//  MEME_AUTOTEST_SUITE=webcrash は(-mock のみ)、計測中・再生中にグラフ画面(WebView)のプロセスを落とし、
//  読み込み直したページで計測・再生が続いているかを見る。
//
//  MEME_AUTOTEST_SUITE=reconnect は(-mock か MEME_AUTOTEST_REAL=1)、計測中に切断 → 繋ぎ直して計測を始められるか、切断した回の CSV が
//  停止と同じく締められるか(ファイルが分かれる・NUM が続かない・Artifact が書き戻される)を見る。
//
//  MEME_AUTOTEST_SUITE=zip MEME_AUTOTEST_BADZIPS=<フォルダ> は、フォルダの中の zip を 1 つずつ読み込み、
//  名前が good で始まるものは通り、それ以外は断られて今の中身が変わらず、展開先の外に何も書かれないことを見る
//  (zip slip・zip 爆弾などの検査。悪い zip は webview/tools/make_bad_zips.py が作る)。
//
//  MEME_AUTOTEST_SUITE=engines は(-mock のみ)、Display Engine ダイアログの操作(WebContentStore の add / activate / remove)を通しで見る:
//  標準版は先頭で消せない・取り込んでも有効にはならない・使うのは 1 つだけ(ページが読む manifest もそれ)・同じ name は置き換え
//  (使っていればそのまま新しい版)・使っているものを消すと標準版に戻る・起動し直しても(prepare)選んだものが残る・無い ID なら標準版・
//  1.5.0 build 35 までの custom/ が zips/ の 1 つに移り、使っていたならそれを使う。試す zip は同梱の標準版の中身から作る。終わったら元に戻す。
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
            // MEME_AUTOTEST_SOCKET_PORT があれば、その間だけ TCP 出力を有効にする(外から nc などで受けて確かめる)。終わったら戻す
            let savedSocket = (UserSetting.getExtermalOutputSocket(), UserSetting.getLocalPort())
            let socketPort = ProcessInfo.processInfo.environment["MEME_AUTOTEST_SOCKET_PORT"]
            if let socketPort {
                UserSetting.setExtermalOutputSocket(true)
                UserSetting.setLocalPort(socketPort)
                vm.settingsDidApply()
            }
            let store = WebContentStore.shared
            let env = ProcessInfo.processInfo.environment
            let usesZips = env["MEME_AUTOTEST_ZIP"] != nil || ["zip", "engines"].contains(env["MEME_AUTOTEST_SUITE"] ?? "")
            let startIds = Set(store.entries.map(\.id)), startActive = store.activeId
            let kept = usesZips ? store.copyImportedAside() : nil   // 取り込みで置き換わる前に、取り込んである zip を退避
            do {
                if let zip = env["MEME_AUTOTEST_ZIP"] {
                    let m = try store.importAndActivate(URL(fileURLWithPath: zip))
                    result["zip"] = m.displayName
                    vm.reloadGraph()
                }
                if env["MEME_AUTOTEST_SUITE"] == "engines" {
                    guard MEMELibFactory.isMock else { throw Timeout(what: "refused: engines suite needs -mock") }
                    try await runEngines(vm, out: out, result: &result)
                } else if env["MEME_AUTOTEST_SUITE"] == "zip" {
                    try await runZip(vm, dir: URL(fileURLWithPath: env["MEME_AUTOTEST_BADZIPS"] ?? ""), result: &result)
                } else if env["MEME_AUTOTEST_SUITE"] == "webcrash" {
                    guard MEMELibFactory.isMock else { throw Timeout(what: "refused: webcrash suite needs -mock") }
                    try await runWebCrash(vm, out: out, result: &result)
                } else if env["MEME_AUTOTEST_SUITE"] == "reconnect" {
                    guard MEMELibFactory.isMock || env["MEME_AUTOTEST_REAL"] == "1" else {
                        throw Timeout(what: "refused: reconnect suite needs -mock or MEME_AUTOTEST_REAL=1")
                    }
                    try await runReconnect(vm, result: &result)
                } else if env["MEME_AUTOTEST_SUITE"] == "settings" {
                    guard MEMELibFactory.isMock || env["MEME_AUTOTEST_REAL"] == "1" else {
                        throw Timeout(what: "refused: settings suite needs -mock or MEME_AUTOTEST_REAL=1")
                    }
                    try await runSettings(vm, out: out, csvDir: csvDir, result: &result)
                } else {
                    guard MEMELibFactory.isMock || env["MEME_AUTOTEST_REAL"] == "1" else {
                        throw Timeout(what: "refused: run with -mock, or MEME_AUTOTEST_REAL=1 for a real device")
                    }
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
            if socketPort != nil {
                UserSetting.setExtermalOutputSocket(savedSocket.0)
                UserSetting.setLocalPort(savedSocket.1)
            }
            if usesZips {                             // 取り込んだ zip を残さない。もともと使っていたものに戻す
                if let kept {
                    store.restoreImported(from: kept)
                } else {                              // 退避できなかったときは、増えた分だけ消す(置き換えた分は戻せない)
                    for e in store.entries where !startIds.contains(e.id) { try? store.remove(e.id) }
                    store.activate(startActive)
                }
            }
            NSApp.terminate(nil)
        }
    }

    private struct CheckFailed: Error, CustomStringConvertible {
        let problems: [String]; var description: String { "check failed: " + problems.joined(separator: "; ") }
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
        let outputsTest = ProcessInfo.processInfo.environment["MEME_AUTOTEST_OUTPUTS"] == "1"
        if outputsTest {
            // 高機能版の設定の Notify(立ち座り)・CSV(高さ・速度)をオンにして読み込み直す(設定はページの localStorage)
            _ = try? await vm.web.webView.evaluateJavaScript(
                "localStorage.setItem('advanced.notify', JSON.stringify(['posture'])); localStorage.setItem('advanced.csv', JSON.stringify(['hve'])); 0")
            vm.reloadGraph()
            await sleep(0.5)
            try await wait("page reloaded", 30) { vm.web.isReady }
        }

        // 接続して 100Hz・±8G・±1000dps で計測(モードは既定 Full。実機では MEME_AUTOTEST_MODE=standard も)
        let env = ProcessInfo.processInfo.environment
        let standard = env["MEME_AUTOTEST_MODE"] == "standard"
        let seconds = Double(env["MEME_AUTOTEST_SECONDS"] ?? "") ?? 8
        let suffix = env["MEME_AUTOTEST_DEVICE"].map { $0.replacingOccurrences(of: ":", with: "").uppercased() }
        let byAddress = suffix.map { !$0.isEmpty && $0.allSatisfy(\.isHexDigit) } ?? false
        let want = { (name: String) in suffix == nil || byAddress || name.uppercased().hasSuffix(suffix!) }
        var tried: [String] = []
        for attempt in 1...4 {
            vm.startScan()
            do {
                try await wait("device found", 30) { vm.foundDevices.contains(where: want) }
            } catch {
                result["found"] = vm.foundDevices             // 名前が合わなかったときに何が見えていたか
                throw error
            }
            vm.selectedDevice = vm.foundDevices.first(where: want)!
            vm.toggleConnect()
            try await wait("connected", 30) { vm.phase == .connected }
            guard byAddress, let suffix else { break }
            try? await wait("address", 5) { !vm.connectedMacAddress.isEmpty }
            if vm.connectedMacAddress.uppercased().hasSuffix(suffix) { break }
            tried.append(vm.connectedMacAddress)             // 同じ名前の別の端末 → 切ってやり直す
            vm.toggleConnect()
            try await wait("disconnected", 15) { vm.phase == .idle }
            if attempt == 4 { result["tried"] = tried; throw CheckFailed(problems: ["device …\(suffix) not reached (got \(tried))"]) }
            await sleep(2)
        }
        if !tried.isEmpty { result["tried"] = tried }
        result["device"] = vm.selectedDevice
        result["address"] = vm.connectedMacAddress
        result["mode"] = standard ? "Standard" : "Full"
        vm.selectMode = (standard ? MeasurementMode.standard : .full).pickerIndex
        vm.transSpeed = 0
        vm.accelRange = 2
        vm.gyroRange = 2
        vm.toggleMeasurement()
        try await wait("measuring") { vm.phase == .measuring }
        await sleep(seconds)
        if try await hasDetector(vm), !standard {             // 高機能版: 検出器が読み込めて、届いた行に追いついている
            try await waitJs(vm, "detector live", 30, "jmasEngine.state === 'ready' && jmasEngine.fed > 0 && jmasEngine.store.n - jmasEngine.fed < 200")
        }
        result["measuring"] = await pageState(vm)
        result["probeMeasuring"] = await probe(vm)
        await snapshot(vm, "1-measuring", out)
        if outputsTest {
            // 受け口の検査規則(dev.js と同じ): 数式風・カンマ・真偽値・列数の違い・名前の形・同じ列 / 違う列での宣言し直し
            _ = try? await vm.web.webView.evaluateJavaScript("""
                for (const m of [
                  {kind: 'table', name: 'selftest', columns: ['A', 'B'], title: 'self test'},
                  {kind: 'table', name: 'selftest', columns: ['A', 'B']},
                  {kind: 'records', name: 'selftest', rows: [[300, '=SUM(1)', 'a,b'], [301, true, 1], [302, 1.5, null], [303, 1], [-1, 1, 2], [304, -7, 'x\\ny']]},
                  {kind: 'table', name: 'Bad', columns: ['A']},
                  {kind: 'table', name: 'nodate', columns: ['DATE']},
                  {kind: 'notify', tag: 'selftest', title: 'Self test', text: 'from the Mac self test', i: 300},
                  {kind: 'notify', tag: 'Bad', title: 'x'},
                ]) window.webkit.messageHandlers.jmas.postMessage(m); 0
                """)
        }

        // ページで付けたのと同じ形でアーティファクトを送る(サンプル 300 = CSV の 299 行目)
        _ = try? await vm.web.webView.evaluateJavaScript(
            "window.webkit.messageHandlers.jmas.postMessage({kind: 'artifact', i: 300, text: 'autotest'}); 0")
        await sleep(0.5)

        vm.toggleMeasurement()
        try await wait("stopped") { vm.phase == .connected }
        await sleep(1.5)

        // 保存した CSV(保存先で一番新しいもの)に書き戻されたか
        guard let csv = newestCsv() else { throw Timeout(what: "saved csv") }
        result["savedCsv"] = csv.path
        let text = (try? CsvFile.readText(at: csv)) ?? ""
        let dataLines = text.components(separatedBy: "\n").drop { !$0.hasPrefix("//ARTIFACT") }.dropFirst().filter { !$0.isEmpty }
        result["savedRows"] = dataLines.count
        result["artifactRow"] = dataLines.firstIndex { $0.hasPrefix("autotest,") }.map { $0 - dataLines.startIndex } ?? -1
        result["savedMode"] = text.components(separatedBy: "\n").first { $0.hasPrefix("// Data mode") }?
            .split(separator: ":", maxSplits: 1).last?.trimmingCharacters(in: .whitespaces) ?? ""
        let nums = dataLines.compactMap { Int($0.split(separator: ",", omittingEmptySubsequences: false).dropFirst().first ?? "") }
        result["numGaps"] = zip(nums, nums.dropFirst()).filter { $1 != $0 + 1 }.count   // 番号(NUM)の抜け・戻り
        var problems: [String] = []
        if result["savedMode"] as? String != result["mode"] as? String { problems.append("mode \(result["savedMode"] ?? "") in CSV") }
        if result["numGaps"] as? Int != 0 { problems.append("NUM has \(result["numGaps"] ?? 0) gaps") }
        if result["artifactRow"] as? Int != 299 { problems.append("artifact at row \(result["artifactRow"] ?? -1), expected 299") }
        if outputsTest { problems += await checkOutputs(vm, dataCsv: csv, dataNums: nums, result: &result) }

        // 再生
        vm.toggleConnect()
        try await wait("disconnected") { vm.phase == .idle }
        vm.openReplayFile(url: csv)
        await sleep(4)
        if try await hasDetector(vm), !standard {             // 高機能版: 再生したファイルを最後まで解析し終える
            try await waitJs(vm, "detector replay", 60, "jmasEngine.finalSent === true")
        }
        result["replay"] = await pageState(vm)
        result["probeReplay"] = await probe(vm)
        await snapshot(vm, "2-replay", out)
        // 再生中のアーティファクト(番号は CSV のデータ行)。カンマは空白に、数式風は断り、空は X。切断時に書き戻す
        for (i, text) in [(5, "a,b"), (10, "=SUM(A1)"), (20, "")] {
            _ = try? await vm.web.webView.evaluateJavaScript(
                "window.webkit.messageHandlers.jmas.postMessage({kind: 'artifact', i: \(i), text: \(String(reflecting: text))}); 0")
        }
        await sleep(0.5)
        vm.toggleConnect()
        await sleep(0.5)
        let after = (try? CsvFile.readText(at: csv)) ?? ""
        let rowsAfter = Array(after.components(separatedBy: "\n").drop { !$0.hasPrefix("//ARTIFACT") }.dropFirst().filter { !$0.isEmpty })
        let firstField = { (k: Int) in k < rowsAfter.count ? String(rowsAfter[k].prefix { $0 != "," }) : "?" }
        result["replayArtifacts"] = ["5": firstField(5), "10": firstField(10), "20": firstField(20), "299": firstField(299)]
        if firstField(5) != "a b" { problems.append("replay artifact row 5 = \(firstField(5)), expected 'a b'") }
        if firstField(10) != "" { problems.append("replay artifact row 10 = \(firstField(10)), expected refused") }
        if firstField(20) != "X" { problems.append("replay artifact row 20 = \(firstField(20)), expected 'X'") }
        if firstField(299) != "autotest" { problems.append("live artifact row 299 lost after replay: \(firstField(299))") }
        if outputsTest { problems += checkOutputsMove(vm, dataCsv: csv, result: &result) }
        if !problems.isEmpty { throw CheckFailed(problems: problems) }
    }

    /// 判定器の表の CSV と通知(MEME_AUTOTEST_OUTPUTS=1)。表 hve が 1 本でき、行が 0 でなく、ページが送った行がすべて書かれ、
    /// NUM がデータ CSV の範囲に入り、DATE が入っているか
    private static func checkOutputs(_ vm: MEMEViewModel, dataCsv: URL, dataNums: [Int], result: inout [String: Any]) async -> [String] {
        let o = vm.outputs
        var out: [String: Any] = ["app": o.stats.json, "tables": o.tablesJson]
        var problems: [String] = []
        var pageSent: [String: Any] = [:]
        if let s = try? await vm.web.webView.evaluateJavaScript("JSON.stringify(window.jmasOutputs?.sent ?? {})") as? String,
           let obj = try? JSONSerialization.jsonObject(with: Data(s.utf8)) as? [String: Any] { pageSent = obj }
        out["pageSent"] = pageSent
        // 規則の確かめで送った分(selftest の表 3 行・通知 2 件)を除いて、ページが送った数と比べる
        let wantWarnings = ["records selftest: 3 row(s) dropped", "table: bad name Bad", "table nodate: bad columns", "notify: bad tag Bad"]
        let unexpected = o.stats.warnings.filter { w in !wantWarnings.contains { w.hasPrefix($0) } }
        let missing = wantWarnings.filter { want in !o.stats.warnings.contains { $0.hasPrefix(want) } }
        if !unexpected.isEmpty || !missing.isEmpty { problems.append("outputs warnings: unexpected \(unexpected), missing \(missing)") }
        let selftestRows = 3, selftestNotify = 2
        // ページが送った行 = 書いた行 + データ CSV に行の無いサンプルの行(先頭パケット。多くて 1 行)
        if (pageSent["records"] as? Int ?? -1) != o.stats.rowsReceived - selftestRows + o.stats.rowsNotInData || o.stats.rowsNotInData > 1 {
            problems.append("table rows: page sent \(pageSent["records"] ?? "?"), app wrote \(o.stats.rowsReceived) + \(o.stats.rowsNotInData) not in the data CSV")
        }
        if (pageSent["notify"] as? Int ?? -1) != o.stats.notifyReceived - selftestNotify {
            problems.append("notify: page sent \(pageSent["notify"] ?? "?"), app received \(o.stats.notifyReceived)")
        }
        let lo = dataNums.min() ?? 0, hi = dataNums.max() ?? -1
        // 規則の確かめの表: 数式風は空欄・カンマと改行は空白・数はそのまま・null は空欄、真偽値・列数違い・負の i の行は捨てる
        if let st = o.lastFiles.first(where: { $0.lastPathComponent.contains("_selftest.") }) {
            let lines = ((try? CsvFile.readText(at: st)) ?? "").components(separatedBy: "\n").filter { !$0.isEmpty }
            let body = lines.filter { !$0.hasPrefix("//") }.map { $0.split(separator: ",", omittingEmptySubsequences: false).map(String.init) }
            let cells = body.map { Array($0.dropFirst(2)) }
            out["selftest"] = ["header": lines.filter { $0.hasPrefix("//") }, "rows": body.map { $0.joined(separator: ",") }]
            if cells != [["", "a b"], ["1.5", ""], ["-7", "x y"]] { problems.append("selftest table cells \(cells)") }
            if body.contains(where: { $0.count < 2 || $0[1].isEmpty }) { problems.append("selftest table rows without DATE") }
            if !lines.contains("// Detector output  : selftest (self test)") { problems.append("selftest table title") }
        } else { problems.append("no selftest table CSV") }
        let dataStem = dataCsv.lastPathComponent.replacingOccurrences(of: ".csv.gz", with: "").replacingOccurrences(of: ".csv", with: "")
        var files: [[String: Any]] = []
        if o.lastFiles.isEmpty { problems.append("no table CSV") }
        for f in o.lastFiles where !f.lastPathComponent.contains("_selftest.") {
            let text = (try? CsvFile.readText(at: f)) ?? ""
            let lines = text.components(separatedBy: "\n")
            let head = Array(lines.prefix { $0.hasPrefix("//") })
            let rows = lines.dropFirst(head.count).filter { !$0.isEmpty }.map { $0.split(separator: ",", omittingEmptySubsequences: false).map(String.init) }
            let nums = rows.compactMap { Int($0.first ?? "") }
            let inRange = !nums.isEmpty && nums.allSatisfy { (lo...hi).contains($0) }
            let withDate = rows.filter { $0.count > 1 && !$0[1].isEmpty }.count
            let sameBase = f.lastPathComponent.hasPrefix(dataStem + "_") && f.deletingLastPathComponent() == dataCsv.deletingLastPathComponent()
                && CsvFile.isGzip(f) == CsvFile.isGzip(dataCsv)
            files.append(["file": f.lastPathComponent, "header": head, "rows": rows.count, "numMin": nums.min() ?? -1, "numMax": nums.max() ?? -1,
                          "numInDataRange": inRange, "withDate": withDate, "sameBaseAsData": sameBase, "first": rows.prefix(3).map { $0.joined(separator: ",") }])
            if rows.isEmpty { problems.append("\(f.lastPathComponent): no rows") }
            if !inRange { problems.append("\(f.lastPathComponent): NUM outside the data CSV (\(nums.min() ?? -1)…\(nums.max() ?? -1) vs \(lo)…\(hi))") }
            if Double(withDate) < Double(rows.count) * 0.95 { problems.append("\(f.lastPathComponent): DATE on \(withDate) of \(rows.count) rows") }
            if !sameBase { problems.append("\(f.lastPathComponent): not next to \(dataCsv.lastPathComponent) with the same base name / compression") }
            if head.last != "//NUM,DATE,HEIGHT_CM,VELOCITY_CM_S" && f.lastPathComponent.contains("_hve.") { problems.append("hve header \(head.last ?? "")") }
        }
        out["files"] = files
        result["outputs"] = out
        return problems
    }

    /// 保存ダイアログでデータ CSV を移したときと同じ扱い(DetectorOutputs.dataFileMoved)で、表も同じフォルダ・同じベース名へ移るか
    private static func checkOutputsMove(_ vm: MEMEViewModel, dataCsv: URL, result: inout [String: Any]) -> [String] {
        let fm = FileManager.default
        let dir = dataCsv.deletingLastPathComponent().appendingPathComponent("moved", isDirectory: true)
        try? fm.createDirectory(at: dir, withIntermediateDirectories: true)
        let ext = CsvFile.isGzip(dataCsv) ? "csv.gz" : "csv"
        let to = dir.appendingPathComponent("renamed.\(ext)")
        let before = vm.outputs.lastFiles
        do { try fm.moveItem(at: dataCsv, to: to) } catch { return ["move data CSV: \(error.localizedDescription)"] }
        vm.outputs.dataFileMoved(from: dataCsv, to: to)
        let after = vm.outputs.lastFiles
        result["outputsMoved"] = after.map(\.lastPathComponent)
        var problems: [String] = []
        for (a, b) in zip(before, after) {
            let suffix = a.lastPathComponent.split(separator: "_").last.map(String.init) ?? ""
            if b.deletingLastPathComponent() != dir || b.lastPathComponent != "renamed_\(suffix)" || !fm.fileExists(atPath: b.path) || fm.fileExists(atPath: a.path) {
                problems.append("table not moved with the data CSV: \(a.lastPathComponent) -> \(b.path)")
            }
        }
        return problems
    }

    /// 保存先で一番新しい CSV
    private static func newestCsv() -> URL? {
        let dir = URL(fileURLWithPath: UserSetting.getSaveFilePath())
        let files = (try? FileManager.default.contentsOfDirectory(at: dir, includingPropertiesForKeys: [.contentModificationDateKey])) ?? []
        // 判定器の表(<データ CSV のベース名>_<名前>.csv)は除く。データ CSV は <アドレス>_<日時>.csv
        return files.filter { CsvFile.isSupported(fileName: $0.lastPathComponent) && $0.lastPathComponent.split(separator: "_").count == 2 }
            .max { a, b in
                let da = (try? a.resourceValues(forKeys: [.contentModificationDateKey]).contentModificationDate) ?? .distantPast
                let db = (try? b.resourceValues(forKeys: [.contentModificationDateKey]).contentModificationDate) ?? .distantPast
                return da < db
            }
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

    // MARK: - グラフ画面のプロセスが落ちたとき(モック)

    /// 計測中・再生中に WebView のプロセスを落とし、読み込み直したページで計測・再生が続いているか
    /// (以前は読み込み直しで start / openReplay が送り直されず、グラフ画面が空のままだった)。
    private static func runWebCrash(_ vm: MEMEViewModel, out: URL, result: inout [String: Any]) async throws {
        try await wait("page ready", 20) { vm.web.isReady }
        vm.startScan()
        try await wait("device found", 30) { vm.phase == .deviceFound }
        vm.toggleConnect()
        try await wait("connected", 30) { vm.phase == .connected }
        vm.toggleMeasurement()
        try await wait("measuring") { vm.phase == .measuring }
        await sleep(2)
        try await killPage(vm, "live")
        await sleep(3)
        let live = await pageState(vm)
        result["live"] = live
        await snapshot(vm, "crash-live", out)
        vm.toggleMeasurement()
        try await wait("stopped") { vm.phase == .connected }
        await sleep(1)
        vm.toggleConnect()
        try await wait("disconnected") { vm.phase == .idle }

        guard let csv = newestCsv() else { throw Timeout(what: "saved csv") }
        vm.openReplayFile(url: csv)
        await sleep(3)
        try await killPage(vm, "replay")
        await sleep(3)
        let replay = await pageState(vm)
        result["replay"] = replay
        await snapshot(vm, "crash-replay", out)
        vm.toggleConnect()
        await sleep(0.5)

        // ステータスの行は、start / openReplay を受けたときだけ端末名・ファイル名と計測条件を出す
        var problems: [String] = []
        let liveStatus = ((live as? [String: Any])?["status"] as? String) ?? ""
        let replayStatus = ((replay as? [String: Any])?["status"] as? String) ?? ""
        if !liveStatus.contains("ESR_MOCK") || !liveStatus.contains("100 Hz") { problems.append("live not resumed: \(liveStatus)") }
        if !replayStatus.contains(csv.lastPathComponent) { problems.append("replay not resumed: \(replayStatus)") }
        if !problems.isEmpty { throw CheckFailed(problems: problems) }
    }

    /// WebView のプロセスを落とし(webViewWebContentProcessDidTerminate が呼ばれる)、別のプロセスで読み込み直して
    /// ready になるまで待つ(読み込み直しは速いので、isReady が倒れるのを見張るのではなくプロセスが替わったかで見る)
    private static func killPage(_ vm: MEMEViewModel, _ what: String) async throws {
        let pidNow = { (vm.web.webView.value(forKey: "_webProcessIdentifier") as? NSNumber)?.int32Value ?? 0 }
        let pid = pidNow()
        guard pid > 0 else { throw Timeout(what: "web content pid (\(what))") }
        kill(pid, SIGKILL)
        try await wait("page ready after kill (\(what))", 20) { pidNow() > 0 && pidNow() != pid && vm.web.isReady }
    }

    // MARK: - 計測中の切断(モック)

    /// 計測中に切断されたあと、繋ぎ直して Start Measurement で計測が始まるか
    /// (以前は計測中の印が残り、繋ぎ直した後の Start が停止として扱われていた)。
    /// 切断した回も停止と同じく締められ(CSV が分かれ、NUM が続かない)、付けた Artifact が書き戻されるか。
    private static func runReconnect(_ vm: MEMEViewModel, result: inout [String: Any]) async throws {
        try await wait("page ready", 20) { vm.web.isReady }
        for round in 1...2 {
            vm.startScan()
            try await wait("device found \(round)", 30) { vm.phase == .deviceFound }
            vm.toggleConnect()
            try await wait("connected \(round)", 30) { vm.phase == .connected }
            vm.toggleMeasurement()
            try await wait("measuring \(round)", 5) { vm.phase == .measuring }
            await sleep(2)
            if round == 2 { break }
            // サンプル 50 = CSV の 49 行目。切断で締めたときに書き戻されるか
            _ = try? await vm.web.webView.evaluateJavaScript(
                "window.webkit.messageHandlers.jmas.postMessage({kind: 'artifact', i: 50, text: 'cut'}); 0")
            await sleep(0.3)
            // 端末側からの切断と同じく、計測を止めずに切る(画面からは計測中に切断できない)
            vm.toggleConnect()
            try await wait("disconnected", 15) { vm.phase == .idle }
            await sleep(1)                                    // 停止の後始末(0.5 秒後)が走っても idle のままか
            result["phaseAfterDisconnect"] = "\(vm.phase)"
            if vm.phase != .idle { throw CheckFailed(problems: ["phase \(vm.phase) after disconnect"]) }
        }
        vm.toggleMeasurement()
        try await wait("stopped", 5) { vm.phase == .connected }
        await sleep(1)
        vm.toggleConnect()
        try await wait("disconnected at end", 15) { vm.phase == .idle }

        // 保存先の CSV(古い順。名前は <アドレス>_<日時> で、2 回で別の端末に繋がることもあるので日時の部分で並べる)
        let dir = URL(fileURLWithPath: UserSetting.getSaveFilePath())
        let stamp = { (u: URL) in u.lastPathComponent.split(separator: "_").last.map(String.init) ?? "" }
        let csvs = ((try? FileManager.default.contentsOfDirectory(at: dir, includingPropertiesForKeys: nil)) ?? [])
            .filter { CsvFile.isSupported(fileName: $0.lastPathComponent) }.sorted { stamp($0) < stamp($1) }
        var problems: [String] = []
        var files: [[String: Any]] = []
        for csv in csvs {
            let text = (try? CsvFile.readText(at: csv)) ?? ""
            let rows = Array(text.components(separatedBy: "\n").drop { !$0.hasPrefix("//ARTIFACT") }.dropFirst().filter { !$0.isEmpty })
            let nums = rows.compactMap { Int($0.split(separator: ",", omittingEmptySubsequences: false).dropFirst().first ?? "") }
            let gaps = zip(nums, nums.dropFirst()).filter { $1 != $0 + 1 }.count
            let row49 = rows.count > 49 ? String(rows[49].prefix { $0 != "," }) : "?"
            files.append(["name": csv.lastPathComponent, "rows": rows.count, "headers": text.components(separatedBy: "\n").filter { $0.hasPrefix("//ARTIFACT") }.count,
                          "numGaps": gaps, "firstNum": nums.first ?? -1, "row49": row49])
            if gaps != 0 { problems.append("\(csv.lastPathComponent): NUM has \(gaps) gaps") }
        }
        result["csvs"] = files
        if csvs.count != 2 { problems.append("\(csvs.count) CSV files, expected 2 (one per measurement)") }
        if let first = files.first, first["row49"] as? String != "cut" { problems.append("artifact of the cut measurement: \(first["row49"] ?? "")") }
        if !problems.isEmpty { throw CheckFailed(problems: problems) }
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
                .filter { !$0.contains("/tmp-") && !$0.contains("/old-") && !$0.hasSuffix("/zips") }   // zips = 受け入れた zip の置き場
        }
        var rows: [[String: Any]] = []
        for zip in zips {
            let name = zip.lastPathComponent
            let before = (store.activeId, store.entries)
            let beforeFiles = listing()
            let t0 = Date()
            var row: [String: Any] = ["zip": name]
            do {
                let m = try store.importAndActivate(zip)
                row["accepted"] = m.displayName
            } catch {
                row["refused"] = error.localizedDescription
                row["unchanged"] = before.0 == store.activeId && before.1 == store.entries
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
        let failed = rows.filter { $0["ok"] as? Bool != true }.map { "zip \($0["zip"] ?? "")" }
        if !failed.isEmpty { throw CheckFailed(problems: failed) }   // どれか 1 つでも NG なら、組全体も ok にしない
    }

    // MARK: - Display Engine(複数の zip を持ち、1 つだけ使う)

    private static func runEngines(_ vm: MEMEViewModel, out: URL, result: inout [String: Any]) async throws {
        let store = WebContentStore.shared
        let fm = FileManager.default
        let work = out.appendingPathComponent("engines", isDirectory: true)
        try? fm.removeItem(at: work)
        try fm.createDirectory(at: work, withIntermediateDirectories: true)
        let builtIn = WebContentStore.builtInId
        guard let std = store.entries.first(where: { $0.isBuiltIn }) else { throw Timeout(what: "no built-in entry") }
        // 同梱の標準版の中身を写し、manifest の name・title・version だけ変えて zip にする
        func makeZip(_ name: String, _ version: String) throws -> URL {
            let src = work.appendingPathComponent("\(name)-\(version)", isDirectory: true)
            try fm.copyItem(at: store.root.appendingPathComponent("bundled", isDirectory: true), to: src)
            let mURL = src.appendingPathComponent("manifest.json")
            var m = try JSONSerialization.jsonObject(with: Data(contentsOf: mURL)) as! [String: Any]
            m["name"] = name; m["title"] = name.uppercased(); m["version"] = version
            try JSONSerialization.data(withJSONObject: m).write(to: mURL)
            let zip = work.appendingPathComponent("\(name)-\(version).zip")
            let p = Process()
            p.executableURL = URL(fileURLWithPath: "/usr/bin/ditto")
            p.arguments = ["-c", "-k", src.path, zip.path]
            try p.run(); p.waitUntilExit()
            guard p.terminationStatus == 0 else { throw Timeout(what: "ditto \(name)") }
            return zip
        }
        /// ページ(memeview://app/)が読んでいる manifest の name
        func servedName() async throws -> String {
            vm.reloadGraph()
            try await wait("page ready", 20) { vm.web.isReady }
            let v = try await vm.web.webView.callAsyncJavaScript(
                "return (await (await fetch('manifest.json', {cache: 'no-store'})).json()).name", contentWorld: .page)
            return v as? String ?? ""
        }
        var problems: [String] = []
        var steps: [[String: Any]] = []
        func check(_ what: String, _ ok: Bool, _ detail: Any = "") {
            steps.append(["step": what, "ok": ok, "detail": "\(detail)"])
            if !ok { problems.append(what) }
        }
        func names() -> [String] { store.entries.map { $0.isBuiltIn ? "*" + $0.manifest.name : $0.manifest.name } }

        // 始めは取り込んだものを持たない状態にする(元のものは呼び出し側が退避して戻す)
        for e in store.entries where !e.isBuiltIn { try store.remove(e.id) }
        store.activate(builtIn)
        check("built-in only, first and active", store.entries.count == 1 && store.entries.first?.isBuiltIn == true
              && store.activeId == builtIn, names())
        check("built-in cannot be removed", (try? store.remove(builtIn)) == false && store.entries.first?.isBuiltIn == true)
        check("page serves built-in", try await servedName() == std.manifest.name)

        let zipA1 = try makeZip("enginea", "1.0.0")
        let (a, aOutcome, aActive) = try store.add(zipA1)
        let (b, _, _) = try store.add(makeZip("engineb", "1.0.0"))
        check("add keeps active, appends in order", aOutcome == .added && !aActive && store.activeId == builtIn
              && names() == ["*" + std.manifest.name, "enginea", "engineb"], names())

        // 同じファイルをもう一度(別の場所に写したものでも)取り込んでも重ならない
        let copyA1 = work.appendingPathComponent("copy-of-enginea.zip")
        try fm.copyItem(at: zipA1, to: copyA1)
        let (same1, o1, _) = try store.add(zipA1)
        let (same2, o2, _) = try store.add(copyA1)
        check("same file is not added twice", o1 == .alreadyAdded && o2 == .alreadyAdded && same1.id == a.id
              && same2.id == a.id && names() == ["*" + std.manifest.name, "enginea", "engineb"], names())

        store.activate(a.id)
        check("activate one", store.activeId == a.id && UserSetting.getWebContentActive() == a.id
              && store.manifest?.name == "enginea" && store.activeDir.lastPathComponent == a.id)
        check("page serves the active one", try await servedName() == "enginea")

        let (a2, a2Outcome, replacedActive) = try store.add(makeZip("enginea", "2.0.0"))
        check("same name replaces (same id, still active, new version)",
              a2.id == a.id && a2Outcome == .replaced && replacedActive && store.entries.count == 3 && store.activeId == a.id
              && store.manifest?.version == "2.0.0", names())
        check("replacing keeps the position in the list", names() == ["*" + std.manifest.name, "enginea", "engineb"], names())

        let (_, _, replacedB) = try store.add(makeZip("engineb", "2.0.0"))
        check("replacing an inactive one does not touch the active one", !replacedB && store.activeId == a.id)

        check("remove active falls back to built-in", (try store.remove(a.id)) && store.activeId == builtIn
              && UserSetting.getWebContentActive() == builtIn && names() == ["*" + std.manifest.name, "engineb"], names())
        check("removed folder is gone", !fm.fileExists(atPath: store.root.appendingPathComponent("zips/\(a.id)").path)
              && !fm.fileExists(atPath: store.root.appendingPathComponent("zips/\(a.id).sha256").path))
        let (readd, readdOutcome, _) = try store.add(zipA1)
        check("a removed zip can be added again", readdOutcome == .added && readd.id != a.id, names())
        try store.remove(readd.id)
        check("page serves built-in after remove", try await servedName() == std.manifest.name)

        store.activate(b.id)
        store.prepare()
        check("active survives restart", store.activeId == b.id && store.manifest?.name == "engineb")

        UserSetting.setWebContentActive("no-such-id")
        store.prepare()
        check("unknown active id falls back to built-in", store.activeId == builtIn && UserSetting.getWebContentActive() == builtIn)

        let refused = (try? store.add(work.appendingPathComponent("enginea-1.0.0/manifest.json"))) == nil
        check("bad file refused, nothing changes", refused && names() == ["*" + std.manifest.name, "engineb"], names())

        // 1.5.0 build 35 までの形: custom/ に 1 つだけ、WebContentSource = "custom"
        let legacySrc = work.appendingPathComponent("legacy-src", isDirectory: true)
        try fm.copyItem(at: store.root.appendingPathComponent("bundled", isDirectory: true), to: legacySrc)
        let lm = legacySrc.appendingPathComponent("manifest.json")
        var m = try JSONSerialization.jsonObject(with: Data(contentsOf: lm)) as! [String: Any]
        m["name"] = "legacy"; m["version"] = "0.9.0"
        try JSONSerialization.data(withJSONObject: m).write(to: lm)
        try fm.moveItem(at: legacySrc, to: store.root.appendingPathComponent("custom", isDirectory: true))
        UserDefaults.standard.set("custom", forKey: kConst_WebContentSource)
        UserDefaults.standard.removeObject(forKey: kConst_WebContentActive)
        store.prepare()
        check("legacy custom migrated and active",
              store.manifest?.name == "legacy" && names().contains("legacy") && names().contains("engineb")
              && !fm.fileExists(atPath: store.root.appendingPathComponent("custom").path)
              && UserDefaults.standard.string(forKey: kConst_WebContentSource) == nil
              && UserSetting.getWebContentActive() == store.activeId, names())
        check("page serves migrated one", try await servedName() == "legacy")

        result["engines"] = steps
        if !problems.isEmpty { throw CheckFailed(problems: problems) }
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
        let wantCols = MeasurementMode.allCases.first { $0.label == wantMode }?.columns ?? []
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
