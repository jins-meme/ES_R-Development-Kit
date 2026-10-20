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

#if DEBUG
import UIKit

@MainActor
final class DebugAutoTest {

    private static var running: DebugAutoTest?

    static func startIfRequested(_ vm: MEMEViewModel) {
        let d = UserDefaults.standard
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
            let data = (try? JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys])) ?? Data()
            try? data.write(to: dir.appendingPathComponent("autotest_result.json"))
            NSLog("[AutoTest] %@", String(data: data, encoding: .utf8) ?? "")
            DebugAutoTest.running = nil
        }
    }
}
#endif
