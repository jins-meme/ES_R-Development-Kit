package com.jins_jp.meme.core.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * [SampleCounter] の検証: 12bit カウンタからの NUM 積算・取りこぼし数・通信レート
 * 平滑化（元 MainViewModel の totalCountUp / startCommTicker と同一ロジック）。
 */
class SampleCounterTest {

    @Test
    fun firstPacketIsBaselineOnlyAndReturnsFalse() {
        val c = SampleCounter()
        assertFalse(c.countUp(5.toShort()))
        assertEquals(0L, c.totalCount)
        assertEquals(0L, c.errorCount)
    }

    @Test
    fun sequentialCountsIncreaseMonotonicallyWithoutErrors() {
        val c = SampleCounter()
        c.countUp(0.toShort()) // baseline
        assertTrue(c.countUp(1.toShort()))
        assertEquals(1L, c.totalCount)
        assertTrue(c.countUp(2.toShort()))
        assertEquals(2L, c.totalCount)
        assertTrue(c.countUp(3.toShort()))
        assertEquals(3L, c.totalCount)
        assertEquals(0L, c.errorCount)
    }

    @Test
    fun wraps12BitCounterFrom4095To0() {
        val c = SampleCounter()
        c.countUp(4095.toShort()) // baseline: prevCount = 4095
        assertTrue(c.countUp(0.toShort())) // wraps: 0 - 4095 + 4096 = 1
        assertEquals(1L, c.totalCount)
        assertEquals(0L, c.errorCount)
    }

    @Test
    fun missedSamplesIncrementErrorCountByStepMinusOne() {
        val c = SampleCounter()
        c.countUp(0.toShort()) // baseline
        assertTrue(c.countUp(3.toShort())) // step = 3 -> +2 missed
        assertEquals(3L, c.totalCount)
        assertEquals(2L, c.errorCount)
    }

    @Test
    fun successRateReflectsErrorRatio() {
        val c = SampleCounter()
        c.countUp(0.toShort()) // baseline
        c.countUp(3.toShort()) // totalCount=3, errorCount=2
        assertEquals(1.0 - 2.0 / 3.0, c.successRate, 1e-9)
    }

    @Test
    fun resetReturnsToInitialState() {
        val c = SampleCounter()
        c.countUp(0.toShort())
        c.countUp(3.toShort())
        c.reset()
        assertEquals(0L, c.totalCount)
        assertEquals(0L, c.errorCount)
        // reset 後は次の countUp も基準取得のみ(false)から再開する。
        assertFalse(c.countUp(10.toShort()))
    }

    @Test
    fun commRateTickSmoothsWithPreviousRatio() {
        val c = SampleCounter()
        c.countUp(0.toShort()) // baseline
        for (i in 1..40) c.countUp(i.toShort()) // Hz100: period=40 → totalCount=40

        // 初回: count=40, period=40 → ratioLast=100, ratioPrev(初期値)=100 → (100+100)/2=100 → 1.0
        assertEquals(1.0, c.commRateTick(MemeQuality.Hz100), 1e-9)

        // 次 tick: 追加受信 0 → count=0 → ratioLast=0, 前回の ratioLast=100 → (0+100)/2=50 → 0.5
        assertEquals(0.5, c.commRateTick(MemeQuality.Hz100), 1e-9)
    }
}
