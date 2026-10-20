//
//  SampleCounter.swift
//  MEME_Academic (iOS)
//
//  受信パケットの 12bit カウンタから、単調増加のサンプル通し番号(NUM)・取りこぼし数・通信レートを出す。
//  Android2 の SampleCounter.kt と同じ計算。
//

import Foundation

struct SampleCounter {
    private(set) var totalCount = 0
    private(set) var errorCount = 0
    private var prevCount = -1

    // 通信レートの移動平均の状態
    private var prevTotal = 0
    private var ratioPrev = 100

    mutating func reset() { self = SampleCounter() }

    /// NUM を進める。記録すべきパケットなら true、最初のパケット(前回カウンタの基準を取るだけで CSV に残さない)なら false。
    /// カウンタ(0..4095)の差分を積算する: 前回 < 今回 なら 今回 − 前回、それ以外(周回した)は 今回 − 前回 + 4096。
    /// 差分が 2 以上なら、飛んだぶんを取りこぼしとして数える。
    mutating func countUp(_ count: Int) -> Bool {
        let cnt = count & 0x0FFF
        guard prevCount >= 0 else {
            prevCount = cnt
            return false
        }
        let step = prevCount < cnt ? cnt - prevCount : cnt - prevCount + 4096
        if step > 1 { errorCount += step - 1 }
        totalCount += step
        prevCount = cnt
        return true
    }

    /// 受信成功率(0...1)。カウンタの飛びから数えた取りこぼしに基づく
    var successRate: Double {
        guard totalCount > 0 else { return 1 }
        return min(max(1 - Double(errorCount) / Double(totalCount), 0), 1)
    }

    /// 0.4 秒ごとに呼ぶ。直前の呼び出しからの受信数を期待数と比べた通信レート(0...1)を、前回値との平均で返す
    mutating func commRateTick(hz: Int) -> Double {
        let period = 400 * hz / 1000
        let count = totalCount - prevTotal
        let ratioLast = period == 0 ? 0 : count * 100 / period
        let ratio = (ratioLast + ratioPrev) / 2
        prevTotal += count
        ratioPrev = ratioLast
        return Double(min(max(ratio, 0), 100)) / 100
    }
}
