//
//  AutoTest.swift
//  MEME_Academic (iOS)
//
//  起動引数で スキャン → 接続 → 計測 → 停止 を無人で回す(Mac 版・Android 版の DebugAutoTest と同じ役割)。
//  UserDefaults は `-キー 値` 形式の起動引数を読むので、Mac から次のように起動する:
//
//    xcrun devicectl device process launch --device <id> com.jins-jp.MEME-Academic-iOS-test \
//      -autotestSeconds 90 -autotestDevice ESRG2_5 -autotestMode full -autotestRate 100 \
//      -autotestBackgroundAt 30
//
//  autotestSeconds   計測する秒数(これが無ければ自動計測しない)
//  autotestDevice    広告名の末尾(省略時は最初に見つかった端末)
//  autotestMode      standard | full | quaternion(既定 full)
//  autotestRate      100 | 50(既定 100)
//  autotestBackgroundAt  計測開始から何秒でアプリ自身をバックグラウンドへ回すか(省略時は回さない)。
//                    前面へ戻すには Mac から同じアプリをもう一度 launch する。
//
//  終わると Documents/autotest_result.json に結果を書く(毎回上書き)。
//

import UIKit

@MainActor
final class AutoTest {
    struct Plan {
        var seconds: Double
        var device: String?
        var mode: Meme.Mode
        var rate: Meme.Rate
        var backgroundAt: Double?
    }

    static func plan() -> Plan? {
        let d = UserDefaults.standard
        let seconds = d.double(forKey: "autotestSeconds")
        guard seconds > 0 else { return nil }
        let mode: Meme.Mode = switch d.string(forKey: "autotestMode")?.lowercased() {
        case "standard": .standard
        case "quaternion": .quaternion
        default: .full
        }
        let background = d.double(forKey: "autotestBackgroundAt")
        return Plan(seconds: seconds,
                    device: d.string(forKey: "autotestDevice"),
                    mode: mode,
                    rate: d.integer(forKey: "autotestRate") == 50 ? .hz50 : .hz100,
                    backgroundAt: background > 0 ? background : nil)
    }

    private let central: MemeCentral
    private let plan: Plan
    private var timer: Timer?
    private var scanStartedAt: Date?
    private var measureStartedAt: Date?
    private var backgrounded = false
    private var finished = false

    init(central: MemeCentral, plan: Plan) {
        self.central = central
        self.plan = plan
    }

    func run() {
        NSLog("[AutoTest] plan seconds=%.0f device=%@ mode=%@ rate=%d background=%@",
              plan.seconds, plan.device ?? "(first)", plan.mode.label, plan.rate.hz,
              plan.backgroundAt.map { String($0) } ?? "-")
        timer = Timer.scheduledTimer(withTimeInterval: 0.5, repeats: true) { [weak self] _ in
            MainActor.assumeIsolated { self?.tick() }
        }
    }

    private func tick() {
        guard !finished else { return }
        switch central.state {
        case .unavailable:
            break
        case .idle:
            if measureStartedAt != nil {
                // 計測中に切断された
                finish("disconnected")
            } else {
                central.scan()
                scanStartedAt = Date()
            }
        case .scanning:
            if let target = central.found.first(where: matches) {
                central.connect(target.id)
            } else if let since = scanStartedAt, Date().timeIntervalSince(since) > 20 {
                // 見つからなければスキャンをやり直す
                central.stopScan()
            }
        case .connecting, .starting:
            break
        case .ready:
            if measureStartedAt == nil {
                central.mode = plan.mode
                central.rate = plan.rate
                central.start()
                measureStartedAt = Date()
            } else {
                finish("stopped")
            }
        case .measuring:
            guard let start = measureStartedAt else { return }
            let t = Date().timeIntervalSince(start)
            if t >= plan.seconds {
                central.stop()
                finish("completed")
            } else if let bg = plan.backgroundAt, t >= bg, !backgrounded,
                      UIApplication.shared.applicationState == .active {
                backgrounded = true
                NSLog("[AutoTest] sending app to background at %.1f s", t)
                // ホームボタンと同じくアプリを背面へ回す(非公開の挙動。検証用アプリに限って使う)
                UIControl().sendAction(#selector(URLSessionTask.suspend), to: UIApplication.shared, for: nil)
            }
        }
    }

    private func matches(_ d: MemeCentral.Found) -> Bool {
        guard let want = plan.device, !want.isEmpty else { return true }
        return d.name.hasSuffix(want)
    }

    private func finish(_ reason: String) {
        finished = true
        timer?.invalidate()
        timer = nil
        let url = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("autotest_result.json")
        let json = central.summaryJSON(reason: reason)
        try? Data(json.utf8).write(to: url)
        NSLog("[AutoTest] %@", json)
    }
}
