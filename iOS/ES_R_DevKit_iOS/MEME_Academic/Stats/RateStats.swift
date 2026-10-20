//
//  RateStats.swift
//  MEME_Academic (iOS)
//
//  受信レートと取りこぼしの集計。時刻は計測開始からの秒で受け取る(テストしやすいよう Date に触らない)。
//

import Foundation

/// アプリの状態。iOS がバックグラウンドや画面ロックで BLE を間引くかを見るため、状態ごとに集計を分ける。
enum ProbePhase: String, CaseIterable, Identifiable {
    case active, inactive, background, locked
    var id: String { rawValue }
}

struct RateStats {
    /// 通知の到着間隔のヒストグラムの区切り(ms)。最後の箱は 100ms 以上。
    static let intervalBoundsMs: [Double] = [2, 5, 10, 15, 20, 30, 50, 100]

    struct PhaseTotal {
        var seconds = 0.0
        var received = 0
        var lost = 0
    }

    private(set) var received = 0
    private(set) var lost = 0
    private(set) var duplicates = 0
    /// 1 回の飛びで失った最大サンプル数
    private(set) var largestGap = 0
    private(set) var notifications = 0
    /// 1 通知のバイト数 → 回数
    private(set) var sizes: [Int: Int] = [:]
    private(set) var intervals = [Int](repeating: 0, count: intervalBoundsMs.count + 1)
    private(set) var maxIntervalMs = 0.0
    private(set) var battery: Int?
    private(set) var disconnects = 0
    private(set) var phase: ProbePhase = .active
    private var phaseTotals: [ProbePhase: PhaseTotal] = [:]
    private var phaseSince = 0.0
    private var prevCounter: Int?
    private var prevSampleAt = 0.0
    private var lastNotificationAt: Double?

    init(phase: ProbePhase = .active) {
        self.phase = phase
    }

    var lossRate: Double {
        received + lost == 0 ? 0 : Double(lost) / Double(received + lost)
    }

    /// データ通知 1 回分。直前の通知からの間隔(ms)を返す(最初は nil)。
    @discardableResult
    mutating func noteNotification(size: Int, at t: Double) -> Double? {
        notifications += 1
        sizes[size, default: 0] += 1
        defer { lastNotificationAt = t }
        guard let last = lastNotificationAt else { return nil }
        let ms = (t - last) * 1000
        maxIntervalMs = max(maxIntervalMs, ms)
        let box = Self.intervalBoundsMs.firstIndex { ms < $0 } ?? Self.intervalBoundsMs.count
        intervals[box] += 1
        return ms
    }

    /// サンプル 1 個分。今回の飛びで取りこぼした数を返す。
    ///
    /// カウンタは 12bit なので、100Hz で 41 秒ぶん途切れると差分だけでは周回数が分からない。
    /// 経過時間から見込んだ件数に一番近い周回数を選ぶ(接続を保ったまま長く止まる場合に備える)。
    @discardableResult
    mutating func noteSample(counter: Int, battery: Int, at t: Double, hz: Int) -> Int {
        self.battery = battery
        defer { prevSampleAt = t }
        guard let prev = prevCounter else {
            prevCounter = counter
            countReceived()
            return 0
        }
        var step = (counter - prev) & 0x0FFF
        if step == 0 {
            duplicates += 1
            return 0
        }
        let expected = (t - prevSampleAt) * Double(hz)
        let wraps = max(0, ((expected - Double(step)) / 4096).rounded())
        step += Int(wraps) * 4096
        prevCounter = counter
        let missing = step - 1
        lost += missing
        largestGap = max(largestGap, missing)
        phaseTotals[phase, default: PhaseTotal()].lost += missing
        countReceived()
        return missing
    }

    private mutating func countReceived() {
        received += 1
        phaseTotals[phase, default: PhaseTotal()].received += 1
    }

    mutating func setPhase(_ p: ProbePhase, at t: Double) {
        guard p != phase else { return }
        phaseTotals[phase, default: PhaseTotal()].seconds += t - phaseSince
        phase = p
        phaseSince = t
    }

    mutating func noteDisconnect() {
        disconnects += 1
    }

    /// 状態ごとの集計。今の状態の秒数は now までを含める。
    func total(for p: ProbePhase, now t: Double) -> PhaseTotal {
        var total = phaseTotals[p] ?? PhaseTotal()
        if p == phase { total.seconds += t - phaseSince }
        return total
    }
}
