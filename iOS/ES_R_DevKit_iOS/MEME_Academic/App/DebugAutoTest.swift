//
//  DebugAutoTest.swift
//  MEME_Academic (iOS)
//
//  起動引数で スキャン → 接続 → 計測 → 停止 を無人で回す(Mac 版・Android 版の DebugAutoTest と同じ役割。Debug ビルドだけ)。
//  UserDefaults は `-キー 値` 形式の起動引数を読むので、Mac から次のように起動する(devicectl はアプリの引数の前に `--` が要る):
//
//    xcrun devicectl device process launch --console --terminate-existing --device <id> com.jins-jp.MEME-Academic-iOS -- \
//      -autotestSeconds 60 -autotestDevice ESRG2_5 -autotestMode full -autotestRate 100 -autotestDropAt 20
//
//  autotestSeconds   計測する秒数(これが無ければ何もしない)。再接続した後の計測も含めた、最初の開始からの秒数
//  autotestDevice    端末名の末尾(省略時は最初に見つかった端末)
//  autotestMode      standard | full | quaternion(既定 full)
//  autotestRate      100 | 50(既定 100)
//  autotestAccel     加速度レンジの番号 0..3(2/4/8/16g。既定 0)
//  autotestGyro      ジャイロレンジの番号 0..3(250/500/1000/2000dps。既定 0)
//  autotestDropAt    計測開始から何秒で、Disconnect ボタンを使わずに接続を切るか(再接続して計測を始め直すかを見る)
//  autotestBackgroundAt  計測開始から何秒でアプリを裏へ回すか(前面へ戻すには Mac から同じアプリをもう一度 launch する)
//
//  終わると Documents/autotest_result.json に結果を書き(毎回上書き)、ログにも出す。
//
//  グラフ画面の zip と再生(2026-11-10。日別キャッシュの確かめに足した。ファイルは `devicectl device copy to` で Documents に置く):
//  autotestZip       Documents に置いた zip の名前。Display Engine の Add zip と同じく取り込んで使う(計測・再生の前に)
//  autotestReplay    Documents に置いた CSV の名前。計測の代わりに再生する(autotestSeconds は要らない)。
//                    ページが解析し終わったら、ページの状態(advanced2 の Logic4 タブの表示・window.jmasEngine。無ければ null)と
//                    logic4-cache の一覧を Documents/autotest_replay_result.json に書く(計測の autotest_result.json にも同じものを足す)
//  autotestReplayMode  再生のダイアログ(advanced2 の「Logic4 for this file」)が出たら選ぶもの: update | temp | cache(既定 update)
//  autotestTimeout   再生の解析を待つ秒数(既定 900)
//  autotestIdle      計測も再生もしないで、ページが読めてからこの秒数待ち、ページの状態と画面(WebView の撮影)を
//                    Documents/autotest_idle_result.json・autotest_idle.png に書く(advanced2 のつないでいないときの Logic4 の確かめ)
//  autotestIdleDevice  その前に、グラフの下の MEME の選択(advanced2 の #l4dev)をこの device にする
//  autotestIdleEval    さらにその後、このページの JavaScript を実行する(await できる。日付・表示位置を動かしてから撮るため)
//

#if DEBUG
import UIKit

@MainActor
final class DebugAutoTest {

    private static var running: DebugAutoTest?

    static func startIfRequested(_ vm: MEMEViewModel) {
        let d = UserDefaults.standard
        if let zip = d.string(forKey: "autotestZip"), !zip.isEmpty {
            do {
                let r = try WebContentStore.shared.addAndActivate(CsvWriter.directory.appendingPathComponent(zip))
                NSLog("[AutoTest] zip %@ → %@ (%@)", zip, r.entry.id, String(describing: r.outcome))
                vm.reloadGraph()
            } catch {
                NSLog("[AutoTest] zip %@ failed: %@", zip, error.localizedDescription)
            }
        }
        if d.double(forKey: "autotestIdle") > 0 {
            DebugReplayTest.idle(vm, wait: d.double(forKey: "autotestIdle"), device: d.string(forKey: "autotestIdleDevice"),
                                 eval: d.string(forKey: "autotestIdleEval"))
            return
        }
        if let replay = d.string(forKey: "autotestReplay"), !replay.isEmpty {
            DebugReplayTest.start(vm, file: replay, mode: d.string(forKey: "autotestReplayMode") ?? "update",
                                  timeout: d.double(forKey: "autotestTimeout") > 0 ? d.double(forKey: "autotestTimeout") : 900)
            return
        }
        let seconds = d.double(forKey: "autotestSeconds")
        guard seconds > 0, running == nil else { return }
        let t = DebugAutoTest(vm: vm, seconds: seconds,
                              device: d.string(forKey: "autotestDevice"),
                              mode: d.string(forKey: "autotestMode") ?? "full",
                              rate: d.integer(forKey: "autotestRate") == 50 ? 50 : 100,
                              accel: d.integer(forKey: "autotestAccel"), gyro: d.integer(forKey: "autotestGyro"),
                              dropAt: d.double(forKey: "autotestDropAt"),
                              backgroundAt: d.double(forKey: "autotestBackgroundAt"))
        running = t
        t.run()
    }

    private let vm: MEMEViewModel
    private let seconds: Double
    private let device: String?
    private let mode: String
    private let rate: Int
    private let accel: Int
    private let gyro: Int
    private var measureStartedAt: Date?
    private var measuredSeconds = 0.0
    private let dropAt: Double
    private let backgroundAt: Double

    private var timer: Timer?
    private var startedAt: Date?
    private var scanStartedAt: Date?
    private var dropped = false
    private var backgrounded = false
    private var measurements = 0
    private var reconnects = 0
    private var wasMeasuring = false
    private var files: [String] = []
    private var finished = false
    private var log: [String] = []

    private init(vm: MEMEViewModel, seconds: Double, device: String?, mode: String, rate: Int, accel: Int, gyro: Int,
                 dropAt: Double, backgroundAt: Double) {
        self.vm = vm; self.seconds = seconds; self.device = device; self.mode = mode; self.rate = rate
        self.accel = min(max(accel, 0), 3); self.gyro = min(max(gyro, 0), 3)
        self.dropAt = dropAt; self.backgroundAt = backgroundAt
    }

    private func note(_ s: String) {
        let t = startedAt.map { String(format: "%.1f", Date().timeIntervalSince($0)) } ?? "-"
        log.append("[\(t)] \(s)")
        NSLog("[AutoTest] %@", s)
    }

    private func run() {
        note("plan seconds=\(seconds) device=\(device ?? "(first)") mode=\(mode) rate=\(rate) dropAt=\(dropAt) backgroundAt=\(backgroundAt)")
        vm.selectMode = (MeasurementMode.allCases.firstIndex { $0.pageName == mode } ?? 1)
        vm.transSpeed = rate == 50 ? 1 : 0
        vm.accelRange = accel
        vm.gyroRange = gyro
        // 無人で回すので、計測の途中で画面がロックされないようにする
        UIApplication.shared.isIdleTimerDisabled = true
        vm.reconnectEnabled = true
        vm.openSharingOnComplete = false
        timer = Timer.scheduledTimer(withTimeInterval: 0.5, repeats: true) { [weak self] _ in
            MainActor.assumeIsolated { self?.tick() }
        }
    }

    private func tick() {
        guard !finished else { return }
        // 計測の始まり・終わりを数える(再接続で始め直すと 2 回目になる)
        if vm.isMeasuring && !wasMeasuring {
            measurements += 1
            if measurements > 1 { reconnects += 1 }
            if startedAt == nil { startedAt = Date() }
            measureStartedAt = Date()
            vm.debugResetStats()
            note("measuring (#\(measurements)) firmware=\(vm.debugFirmware) address=\(vm.debugAddress)")
        }
        if !vm.isMeasuring && wasMeasuring {
            if let s = measureStartedAt { measuredSeconds = Date().timeIntervalSince(s) }
            if let f = vm.debugLastFile?.lastPathComponent { files.append(f) }
            note("measurement ended; status=\(vm.statusText)")
        }
        wasMeasuring = vm.isMeasuring

        if let start = startedAt {
            let t = Date().timeIntervalSince(start)
            if t >= seconds {
                if vm.isMeasuring {
                    if let s = measureStartedAt { measuredSeconds = Date().timeIntervalSince(s) }
                    vm.toggleMeasurement()
                }
                finish(vm.isReconnecting ? "timeout while reconnecting" : "completed")
                return
            }
            if dropAt > 0, t >= dropAt, !dropped, vm.isMeasuring {
                dropped = true
                note("dropping the connection")
                vm.debugDropConnection()
            }
            if backgroundAt > 0, t >= backgroundAt, !backgrounded, UIApplication.shared.applicationState == .active {
                backgrounded = true
                note("sending app to background")
                // ホームボタンと同じくアプリを背面へ回す(非公開の挙動。自己テストに限って使う)
                UIControl().sendAction(#selector(URLSessionTask.suspend), to: UIApplication.shared, for: nil)
            }
            return
        }

        // 計測が始まるまで: スキャン → 接続 → 開始
        switch vm.phase {
        case .idle where !vm.isScanning:
            if let name = vm.foundDevices.first(where: matches) {
                note("connect \(name)")
                vm.debugSelect(name)
                vm.connectOrDisconnect()
            } else {
                note("scan")
                vm.startScan()
                scanStartedAt = Date()
            }
        case .idle:
            if let name = vm.foundDevices.first(where: matches) {
                note("connect \(name)")
                vm.debugSelect(name)
                vm.connectOrDisconnect()
            }
        case .connected where !vm.isStarting:
            note("start measurement")
            vm.toggleMeasurement()
        default:
            break
        }
    }

    private func matches(_ name: String) -> Bool {
        guard let want = device, !want.isEmpty else { return true }
        return name.hasSuffix(want)
    }

    private func finish(_ reason: String) {
        finished = true
        timer?.invalidate()
        UIApplication.shared.isIdleTimerDisabled = false
        let device = vm.debugDeviceCondition
        // 止めた後の書き出し(判定器の表は 1 秒待って閉じる)を待ってから結果を書く
        DispatchQueue.main.asyncAfter(deadline: .now() + 2) { [self] in
            if let f = vm.debugLastFile?.lastPathComponent, !files.contains(f) { files.append(f) }
            let dir = CsvWriter.directory
            var sizes: [String: Int] = [:]
            for name in (try? FileManager.default.contentsOfDirectory(atPath: dir.path)) ?? [] {
                let size = (try? FileManager.default.attributesOfItem(atPath: dir.appendingPathComponent(name).path)[.size] as? Int) ?? 0
                sizes[name] = size
            }
            // データ CSV のヘッダ(計測条件の 4 行)
            var header: [String] = []
            if let f = vm.debugLastFile, let text = try? CsvFile.readText(at: f) {
                header = text.split(separator: "\n").prefix(6).map(String.init)
            }
            let result: [String: Any] = [
                "requested": ["mode": mode, "hz": rate, "accelG": MeasurementRange.accelG[accel], "gyroDps": MeasurementRange.gyroDps[gyro]],
                "deviceReported": device,
                "csvHeader": header,
                "measuredSeconds": measuredSeconds,
                "measuredHz": measuredSeconds > 0 ? Double(vm.recordingRows) / measuredSeconds : 0,
                "modeMismatchPackets": vm.debugModeMismatch,
                "accMagnitudeLSB": vm.debugAccMagnitude,
                "reason": reason,
                "device": vm.selectedDevice,
                "firmware": vm.debugFirmware,
                "address": vm.debugAddress,
                "mode": mode,
                "rate": rate,
                "seconds": seconds,
                "measurements": measurements,
                "reconnects": reconnects,
                "lastRows": vm.recordingRows,
                "lastTotal": vm.debugTotalCount,
                "lastErrors": vm.debugErrorCount,
                "lastSuccessRate": vm.successRate,
                "battery": vm.batteryLevel,
                "pageReady": vm.web.isReady,
                "pageName": vm.web.pageName,
                "files": files,
                "documents": sizes,
                "log": log,
            ]
            // ページの状態(advanced2 なら日別キャッシュ・送った通知 / 表の数)と logic4-cache の一覧
            DebugReplayTest.probe(vm) { page in
                var result = result
                result["page"] = page
                result["logic4Cache"] = DebugReplayTest.cacheListing()
                let data = (try? JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys])) ?? Data()
                try? data.write(to: dir.appendingPathComponent("autotest_result.json"))
                NSLog("[AutoTest] %@", String(data: data, encoding: .utf8) ?? "")
                DebugAutoTest.running = nil
            }
        }
    }
}

/// 再生の自己テスト(autotestReplay)。ページが読めるようになったら CSV を開き、再生のダイアログに答え、解析が終わったら結果を書く。
/// ページの中は WKWebView の evaluateJavaScript で覗く(advanced2 の要素の id・window.jmasEngine に合わせてある。無ければ null)。
@MainActor
final class DebugReplayTest {

    private static var running: DebugReplayTest?

    static func start(_ vm: MEMEViewModel, file: String, mode: String, timeout: Double) {
        guard running == nil else { return }
        let t = DebugReplayTest(vm: vm, file: file, mode: mode, timeout: timeout)
        running = t
        t.timer = Timer.scheduledTimer(withTimeInterval: 1, repeats: true) { _ in MainActor.assumeIsolated { t.tick() } }
    }

    private let vm: MEMEViewModel
    private let file: String
    private let mode: String
    private let timeout: Double
    private var timer: Timer?
    private let begun = Date()
    private var openedAt: Date?
    private var doneAt: Date?
    private var dialog: String?
    private var log: [String] = []
    private var busy = false

    private init(vm: MEMEViewModel, file: String, mode: String, timeout: Double) {
        self.vm = vm; self.file = file; self.mode = mode; self.timeout = timeout
    }

    private func note(_ s: String) {
        log.append(String(format: "[%.1f] ", Date().timeIntervalSince(begun)) + s)
        NSLog("[AutoTest replay] %@", s)
    }

    /// ダイアログが出ていれば mode を選んで閉じ、出ていなければ検出器の状態を返す
    private var step: String {
        """
        (() => {
          const d = document.getElementById("dlg");
          if (d && !d.hidden) {
            const st = document.getElementById("dlgState")?.textContent ?? "";
            const pick = { update: "rmUpdate", temp: "rmTemp", cache: "rmCache" }["\(mode)"];
            const r = document.getElementById(pick);
            if (r && !r.disabled) r.checked = true;
            document.querySelector("#dlgForm button[type=submit]")?.click();
            return JSON.stringify({ dialog: st, picked: r && !r.disabled ? "\(mode)" : null });
          }
          const e = window.jmasEngine;
          return JSON.stringify(e ? { state: e.state, progress: e.progress, error: e.error || null } : { state: "none" });
        })()
        """
    }

    private func tick() {
        guard !busy else { return }
        if Date().timeIntervalSince(begun) > timeout { return finish("timeout") }
        if openedAt == nil {
            guard vm.web.isReady else { return }
            note("open \(file) (mode \(mode)) page=\(vm.web.pageName)")
            vm.openReplay(CsvWriter.directory.appendingPathComponent(file))
            openedAt = Date()
            return
        }
        busy = true
        vm.web.webView.evaluateJavaScript(step) { [weak self] value, error in
            MainActor.assumeIsolated {
                guard let self else { return }
                self.busy = false
                guard let s = value as? String, let o = (try? JSONSerialization.jsonObject(with: Data(s.utf8))) as? [String: Any] else {
                    if let error { self.note("probe failed: \(error.localizedDescription)") }
                    return
                }
                if let d = o["dialog"] as? String {
                    self.dialog = d
                    self.note("dialog: \(d) → \(o["picked"] as? String ?? "(not available)")")
                    return
                }
                if (o["state"] as? String) == "error" { return self.finish("page error: \(o["error"] ?? "")") }
                let p = (o["progress"] as? NSNumber)?.doubleValue ?? 0
                if (o["state"] as? String) == "ready", p >= 1 {
                    if self.doneAt == nil { self.doneAt = Date(); self.note("analysed") }
                    // 最後の書き込み(日別ファイル)が届くのを少し待つ
                    if Date().timeIntervalSince(self.doneAt!) >= 4 { self.finish("completed") }
                }
            }
        }
    }

    private func finish(_ reason: String) {
        timer?.invalidate(); timer = nil
        let secs = openedAt.map { (doneAt ?? Date()).timeIntervalSince($0) } ?? 0
        note("finish: \(reason)")
        Self.probe(vm) { [self] page in
            let result: [String: Any] = [
                "reason": reason, "file": file, "mode": mode, "dialog": dialog ?? NSNull(),
                "analyseSeconds": secs, "pageName": vm.web.pageName, "page": page,
                "logic4Cache": Self.cacheListing(), "log": log,
            ]
            let data = (try? JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys])) ?? Data()
            try? data.write(to: CsvWriter.directory.appendingPathComponent("autotest_replay_result.json"))
            NSLog("[AutoTest replay] %@", String(data: data, encoding: .utf8) ?? "")
            Self.running = nil
        }
    }

    /// つないでいないとき: ページが読めたら wait 秒待ち(device があれば MEME の選択を替えて)、状態と画面を書く
    static func idle(_ vm: MEMEViewModel, wait: Double, device: String?, eval: String? = nil, begun: Date = Date()) {
        guard vm.web.isReady || Date().timeIntervalSince(begun) > 60 else {
            DispatchQueue.main.asyncAfter(deadline: .now() + 1) { idle(vm, wait: wait, device: device, eval: eval, begun: begun) }
            return
        }
        DispatchQueue.main.asyncAfter(deadline: .now() + wait) {
            let pick = device.map { d in
                "(() => { const s = document.getElementById('l4dev'); if (!s) return 'none'; s.value = '\(d)'; s.dispatchEvent(new Event('change')); return s.value; })()"
            } ?? "0"
            vm.web.webView.evaluateJavaScript(pick) { picked, _ in
                MainActor.assumeIsolated {
                    DispatchQueue.main.asyncAfter(deadline: .now() + (device == nil ? 0 : 3)) {
                        guard let eval, !eval.isEmpty else { idleWrite(vm, picked: picked); return }
                        vm.web.webView.callAsyncJavaScript(eval, arguments: [:], in: nil, in: .page) { r in
                            MainActor.assumeIsolated {
                                if case .failure(let e) = r { NSLog("[AutoTest idle] eval failed: %@", e.localizedDescription) }
                                idleWrite(vm, picked: picked)
                            }
                        }
                    }
                }
            }
        }
    }

    private static func idleWrite(_ vm: MEMEViewModel, picked: Any?) {
        probe(vm) { page in
            let dir = CsvWriter.directory
            let result: [String: Any] = ["pageReady": vm.web.isReady, "pageName": vm.web.pageName,
                                         "picked": picked ?? NSNull(), "page": page, "logic4Cache": cacheListing()]
            let data = (try? JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys])) ?? Data()
            try? data.write(to: dir.appendingPathComponent("autotest_idle_result.json"))
            NSLog("[AutoTest idle] %@", String(data: data, encoding: .utf8) ?? "")
            vm.web.webView.takeSnapshot(with: nil) { image, _ in
                MainActor.assumeIsolated { try? image?.pngData()?.write(to: dir.appendingPathComponent("autotest_idle.png")) }
            }
        }
    }

    /// ページの状態(advanced2: Logic4 タブに替えて、日別キャッシュの表示・平常値・表示中の日・送った通知 / 表の数)
    static func probe(_ vm: MEMEViewModel, done: @escaping @MainActor ([String: Any]) -> Void) {
        let js = """
            (async () => {
              document.getElementById("tabL4")?.click();
              await new Promise((r) => setTimeout(r, 2500));
              const t = (id) => document.getElementById(id)?.textContent ?? null;
              const e = window.jmasEngine, l4 = window.jmasLogic4, o = window.jmasOutputs;
              let doc = null;
              try { const d = e?.dayText?.(l4?.date); if (d) { const j = JSON.parse(d); const c = {};
                for (const m of j.markers) c[m[1]] = (c[m[1]] || 0) + 1; doc = { date: j.date, windows: j.windows.length, markers: c, sources: j.sources }; } } catch (x) {}
              const sel = document.getElementById("l4dev");
              const dev = sel ? { hidden: sel.hidden, value: sel.value, options: [...sel.options].map((o) => o.value),
                remembered: localStorage.getItem("advanced2.cacheDevices") } : null;
              const emptyEl = document.getElementById("empty");
              return JSON.stringify({ chip: t("cache"), base: t("base"), l4date: l4?.date ?? null,
                empty: emptyEl && !emptyEl.hidden ? emptyEl.textContent : null, dev,
                engine: e ? { state: e.state, progress: e.progress, fed: e.fed, error: e.error || null } : null,
                outputsSent: o?.sent ?? null, dayDoc: doc });
            })()
            """
        vm.web.webView.callAsyncJavaScript("return await " + js, arguments: [:], in: nil, in: .page) { r in
            MainActor.assumeIsolated {
                switch r {
                case .success(let v):
                    let o = (v as? String).flatMap { try? JSONSerialization.jsonObject(with: Data($0.utf8)) } as? [String: Any]
                    done(o ?? ["raw": String(describing: v)])
                case .failure(let e):
                    done(["error": e.localizedDescription])
                }
            }
        }
    }

    /// Documents/logic4-cache の中身(<device>/<name>: バイト数)
    static func cacheListing() -> [String: Int] {
        let fm = FileManager.default, root = CsvWriter.directory.appendingPathComponent(CacheStore.dirName)
        var out: [String: Int] = [:]
        for dev in (try? fm.contentsOfDirectory(atPath: root.path)) ?? [] {
            for n in (try? fm.contentsOfDirectory(atPath: root.appendingPathComponent(dev).path)) ?? [] {
                let a = try? fm.attributesOfItem(atPath: root.appendingPathComponent(dev).appendingPathComponent(n).path)
                out["\(dev)/\(n)"] = (a?[.size] as? NSNumber)?.intValue ?? 0
            }
        }
        return out
    }
}
#endif
