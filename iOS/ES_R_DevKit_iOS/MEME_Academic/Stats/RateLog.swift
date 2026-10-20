//
//  RateLog.swift
//  MEME_Academic (iOS)
//
//  1 秒ごとの集計を CSV に書く。バックグラウンドでは Timer が当てにならないので、時計ではなく
//  受信の到着で秒を進める。受信の無かった秒も 0 の行で埋め、途切れが行として見えるようにする。
//

import Foundation

final class RateLog {
    static let header = "second,time_utc,phase,samples,lost,notifications,max_interval_ms,events\n"

    private let write: (String) -> Void
    private let start: Date
    private var second = 0
    private var samples = 0
    private var lost = 0
    private var notifications = 0
    private var maxIntervalMs = 0.0
    private var events: [String] = []
    /// 行に書く状態。その秒で最後に受信・イベントがあったときの状態で、受信の無い秒へはそのまま持ち越す。
    private var rowPhase: ProbePhase = .active
    /// 直近に締めた 1 秒のサンプル数(画面の「今の Hz」)
    private(set) var lastFullSecondSamples = 0

    // 書式を変えないまま format だけ呼ぶので、スレッドをまたいでも安全
    nonisolated(unsafe) private static let timeFormat: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime]
        return f
    }()

    init(start: Date, write: @escaping (String) -> Void) {
        self.start = start
        self.write = write
        write(Self.header)
    }

    /// t までに終わった秒の行を書き出し、今の秒の状態を phase にする。
    func advance(to t: Double, phase: ProbePhase) {
        let now = max(0, Int(t.rounded(.down)))
        while second < now {
            flushRow()
            second += 1
        }
        rowPhase = phase
    }

    func addNotification(intervalMs: Double?, at t: Double, phase: ProbePhase) {
        advance(to: t, phase: phase)
        notifications += 1
        if let ms = intervalMs { maxIntervalMs = max(maxIntervalMs, ms) }
    }

    func addSample(lost missing: Int, at t: Double, phase: ProbePhase) {
        advance(to: t, phase: phase)
        samples += 1
        lost += missing
    }

    func event(_ text: String, at t: Double, phase: ProbePhase) {
        advance(to: t, phase: phase)
        events.append(text)
    }

    /// 途中の秒も書き出して閉じる。
    func finish(at t: Double, phase: ProbePhase) {
        advance(to: t, phase: phase)
        flushRow()
    }

    private func flushRow() {
        let time = Self.timeFormat.string(from: start.addingTimeInterval(Double(second)))
        // events は ; 区切り。CSV の区切りと衝突しないよう , は置き換える。
        let ev = events.joined(separator: "; ").replacingOccurrences(of: ",", with: " ")
        write("\(second),\(time),\(rowPhase.rawValue),\(samples),\(lost),\(notifications),"
              + String(format: "%.1f", maxIntervalMs) + ",\(ev)\n")
        lastFullSecondSamples = samples
        samples = 0
        lost = 0
        notifications = 0
        maxIntervalMs = 0
        events.removeAll()
    }
}
