package com.jins_jp.meme.core.ui.main

import com.jins_jp.meme.core.data.LocationFix
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.test.TestScope
import kotlinx.coroutines.test.advanceTimeBy
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Test

/**
 * [LocationRecorder] の検証。仮想時間で [LOCATION_INTERVAL_MS] ごとの測位と、
 * [LOCATION_MOVE_THRESHOLD_M] 未満の移動を書かないこと、計測していない間の結果を捨てることを通す。
 */
@OptIn(ExperimentalCoroutinesApi::class)
class LocationRecorderTest {

    private val ui = MutableStateFlow(MainUiState(locationLogging = true, isMeasuring = true))
    private val fixes = ArrayDeque<LocationFix?>()
    private var sampleCount = 0
    private val recorded = mutableListOf<String>()

    private fun TestScope.newRecorder() = LocationRecorder(
        scope = backgroundScope,
        ui = ui,
        sample = { sampleCount++; fixes.removeFirstOrNull() },
        onRecorded = { recorded += it },
    )

    // 緯度 0.001 度 ≒ 111 m
    private val tokyo = LocationFix(35.6802, 139.7521)
    private val nearby = LocationFix(35.68021, 139.75211)   // 数 m
    private val moved = LocationFix(35.6812, 139.7521)      // 約 111 m

    @Test
    fun doesNothingWhenTheSettingIsOff() = runTest {
        ui.update { it.copy(locationLogging = false) }
        val r = newRecorder()
        r.start()
        advanceTimeBy(LOCATION_INTERVAL_MS * 3)
        runCurrent()
        assertEquals(0, sampleCount)
        assertEquals("", r.takePendingArtifact())
    }

    @Test
    fun recordsTheFirstFixAtOnceForTheNextRow() = runTest {
        fixes += tokyo
        val r = newRecorder()
        r.start()
        runCurrent()
        assertEquals(1, sampleCount)
        assertEquals(listOf("lc:35.6802_139.7521"), recorded)
        assertEquals("lc:35.6802_139.7521", r.takePendingArtifact())
        assertEquals("", r.takePendingArtifact())                  // 1 行に載せたら空に戻る
    }

    @Test
    fun skipsSmallMovesAndRecordsLargeOnes() = runTest {
        fixes += listOf(tokyo, nearby, moved)
        val r = newRecorder()
        r.start()
        runCurrent()
        r.takePendingArtifact()
        advanceTimeBy(LOCATION_INTERVAL_MS)
        runCurrent()
        assertEquals(2, sampleCount)
        assertEquals("", r.takePendingArtifact())                  // 数 m の移動は書かない
        advanceTimeBy(LOCATION_INTERVAL_MS)
        runCurrent()
        assertEquals(3, sampleCount)
        assertEquals("lc:35.6812_139.7521", r.takePendingArtifact())
    }

    @Test
    fun dropsAFixThatArrivesAfterTheMeasurementEnded() = runTest {
        fixes += tokyo
        ui.update { it.copy(isMeasuring = false) }
        val r = newRecorder()
        r.start()
        runCurrent()
        assertEquals(1, sampleCount)
        assertEquals("", r.takePendingArtifact())
        assertEquals(emptyList<String>(), recorded)
    }

    @Test
    fun stopEndsTheLoopAndResetForgetsTheLastFix() = runTest {
        fixes += listOf(tokyo, tokyo)
        val r = newRecorder()
        r.start()
        runCurrent()
        r.stop()
        advanceTimeBy(LOCATION_INTERVAL_MS * 3)
        runCurrent()
        assertEquals(1, sampleCount)
        // 次のセッションは「前回地点なし」から始まるので、同じ地点でも 1 回目は書く
        r.takePendingArtifact()
        r.start()
        runCurrent()
        assertEquals("lc:35.6802_139.7521", r.takePendingArtifact())
    }
}
