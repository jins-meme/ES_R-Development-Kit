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
            do {
                guard MEMELibFactory.isMock else { throw Timeout(what: "refused: run with -mock (would connect to a real device)") }
                try await run(vm, out: out, result: &result)
                result["ok"] = true
            } catch {
                result["ok"] = false
                result["error"] = String(describing: error)
            }
            if let data = try? JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys]) {
                try? data.write(to: out.appendingPathComponent("result.json"))
            }
            UserSetting.setSaveFilePath(savedPath)   // terminate は戻らないので defer でなくここで戻す
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
    private static func pageState(_ vm: MEMEViewModel) async -> Any {
        let js = """
        JSON.stringify({charts: [...document.querySelectorAll('.chart .ctitle')].map(e => e.textContent),
          message: document.querySelector('.message')?.hidden ? '' : document.querySelector('.message')?.textContent,
          status: document.querySelector('.status')?.textContent, ready: !!window.jmasHost})
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
        result["measuring"] = await pageState(vm)
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
        result["replay"] = await pageState(vm)
        await snapshot(vm, "2-replay", out)
        vm.toggleConnect()
        await sleep(0.5)
    }
}
#endif
