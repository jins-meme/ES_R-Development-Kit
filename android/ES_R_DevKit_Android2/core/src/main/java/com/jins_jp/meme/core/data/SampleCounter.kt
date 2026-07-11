package com.jins_jp.meme.core.data

/**
 * 受信パケットの 12bit カウンタから単調増加のサンプル通し番号(NUM)・
 * 取りこぼし数・通信レートを計算する(元 MainViewModel の totalCountUp ほか)。
 */
class SampleCounter {
    var totalCount = 0L
        private set
    var errorCount = 0L
        private set
    private var prevCount: Long = -1

    // 通信レート(commRate)の移動平均状態。
    private var prevTotalPrev: Long = 0
    private var ratioPrev: Long = 100

    fun reset() {
        totalCount = 0; errorCount = 0; prevCount = -1
        prevTotalPrev = 0; ratioPrev = 100
    }

    /**
     * NUM を更新する。記録すべきパケットなら true、最初のパケット(基準取得のみで
     * CSV に残さない)なら false を返す。
     *
     * 最初のパケットのカウンタは 0 とは限らないため、1 個目は前回カウンタの初期値を
     * 取得するためだけに使い、2 個目以降を記録する。以降は受信カウンタ(12bit,
     * 0..4095)の差分を積算して単調増加させる。
     *   前回のカウンタ < 今回のカウンタ … NUM += 今回 - 前回
     *   それ以外(周回した)          … NUM += 今回 - 前回 + 4096
     */
    fun countUp(count: Short): Boolean {
        val cnt = (count.toInt() and 0x0FFF).toLong()
        if (prevCount < 0) {
            // 最初のパケットは前回カウンタの基準取得のみに使い、CSV には記録しない。
            prevCount = cnt
            return false
        }
        val newNum = if (prevCount < cnt) {
            totalCount + cnt - prevCount
        } else {
            totalCount + cnt - prevCount + 4096
        }
        // 差分が 2 以上なら取りこぼしたサンプルぶんを誤り数として数える。
        val step = newNum - totalCount
        if (step > 1) errorCount += step - 1
        totalCount = newNum
        prevCount = cnt
        return true
    }

    /** 受信成功率(0..1)。取りこぼしを NUM 差分から数えた誤り数に基づく。 */
    val successRate: Double
        get() = (1.0 - errorCount.toDouble() / totalCount.toDouble()).coerceIn(0.0, 1.0)

    /**
     * 400ms 周期で呼ぶ。直前 tick からの受信数を期待数(period)と比べた通信レート
     * (0..1)を返す。前回値との単純平均で平滑化(元 startCommTicker のループ本体)。
     */
    fun commRateTick(quality: MemeQuality): Double {
        val period = 400L / (((quality.ordinal + 1) and 0xFF) * 10L)
        val count = totalCount - prevTotalPrev
        val ratioLast = if (period == 0L) 0L
        else ((count.toDouble() / period.toDouble()) * 100.0).toLong()
        val ratio = (ratioLast + ratioPrev) / 2
        prevTotalPrev += count
        ratioPrev = ratioLast
        return ratio.coerceIn(0, 100).toDouble() / 100.0
    }
}
