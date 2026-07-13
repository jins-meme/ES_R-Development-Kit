package com.jins_jp.meme.core.ui.main

import android.net.Uri
import com.jins_jp.meme.core.ble.MockMemeBleEngine
import com.jins_jp.meme.core.data.AccRange
import com.jins_jp.meme.core.data.GyroRange
import com.jins_jp.meme.core.data.MeasurementSettings
import com.jins_jp.meme.core.data.MemeMode
import com.jins_jp.meme.core.data.MemeQuality
import java.io.IOException
import java.io.InputStream
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.TestScope
import kotlinx.coroutines.test.advanceUntilIdle
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.mockito.Mockito

/**
 * [PlaybackController] の検証: Play ボタンからの CSV 読み込み → mock モード突入 →
 * mock デバイスへのスキャン・接続エミュレーション、および各種読み込み失敗と exit。
 */
@OptIn(ExperimentalCoroutinesApi::class)
class PlaybackControllerTest {

    private val repo = FakeMemeBleClient()
    private val ui = MutableStateFlow(MainUiState())
    private var suppressCount = 0
    private var stopMeasurementCount = 0
    private val savedSettings = mutableListOf<MeasurementSettings>()

    /** テストごとに差し替える「URI を開く」実装。既定は正常な CSV。 */
    private var openInput: (Uri) -> InputStream? = { VALID_CSV.byteInputStream() }

    // メソッドは一切呼ばれない（openInput ラムダへ素通しされるだけ）ので mock で足りる。
    private val uri: Uri = Mockito.mock(Uri::class.java)

    private fun TestScope.newController(): PlaybackController {
        val reconnect = ReconnectController(
            scope = this,
            repo = repo,
            ui = ui,
            onSuppressAutoConnect = {},
            restartMeasurement = {},
            stopMeasurementService = {},
        )
        return PlaybackController(
            scope = this,
            repo = repo,
            ui = ui,
            reconnect = reconnect,
            onSuppressAutoConnect = { suppressCount++ },
            stopMeasurement = { stopMeasurementCount++ },
            openInput = { openInput(it) },
            saveSettings = { savedSettings += it },
            ioDispatcher = StandardTestDispatcher(testScheduler),
        )
    }

    @Test
    fun nullUriMeansCancelledDialogAndDoesNothing() = runTest {
        newController().start(null)
        advanceUntilIdle()
        assertTrue(repo.mockModeSets.isEmpty())
        assertFalse(ui.value.mockEnabled)
        assertNull(ui.value.mockError)
    }

    @Test
    fun validCsvEntersMockModeAndConnectsToMockDevice() = runTest {
        newController().start(uri)
        advanceUntilIdle()

        // mock モードへ入り、CSV の内容と設定が反映・永続化される。
        assertTrue(repo.mockMode)
        assertEquals(1, repo.loadedCsv!!.rows.size)
        assertEquals(CSV_SETTINGS, savedSettings.single())
        assertEquals(CSV_SETTINGS, ui.value.settings)
        assertTrue(ui.value.mockEnabled)
        assertNull(ui.value.mockError)
        assertNull(ui.value.firmwareVersion)
        assertFalse(ui.value.isInitializing)
        assertEquals("再生データを読み込みました（1 行）", ui.value.toast)

        // スキャン→接続エミュレーション: 自動接続を抑止し、mock デバイスへ接続。
        assertEquals(1, suppressCount)
        assertEquals(1, repo.startScanCount)
        assertEquals(1, repo.stopScanCount)
        assertEquals(listOf(MockMemeBleEngine.MOCK_ADDRESS), repo.connectedAddresses)
    }

    @Test
    fun unopenableUriSurfacesMockError() = runTest {
        openInput = { null }
        newController().start(uri)
        advanceUntilIdle()
        assertEquals("ファイルを開けませんでした。", ui.value.mockError)
        assertFalse(repo.mockMode)
        assertFalse(ui.value.mockEnabled)
        assertEquals(0, repo.startScanCount)
    }

    @Test
    fun invalidCsvSurfacesParserMessageAndLeavesModeUntouched() = runTest {
        openInput = { "not,a,logger,csv".byteInputStream() }
        newController().start(uri)
        advanceUntilIdle()
        assertNotNull(ui.value.mockError)
        assertTrue(repo.mockModeSets.isEmpty())
        assertFalse(ui.value.mockEnabled)
    }

    @Test
    fun unexpectedReadErrorFallsBackToGenericMessage() = runTest {
        openInput = { throw IOException("boom") }
        newController().start(uri)
        advanceUntilIdle()
        assertEquals("CSVの読み込みに失敗しました。", ui.value.mockError)
        assertFalse(repo.mockMode)
    }

    @Test
    fun startWhileMeasuringStopsMeasurementFirst() = runTest {
        ui.update { it.copy(isMeasuring = true) }
        newController().start(uri)
        advanceUntilIdle()
        assertEquals(1, stopMeasurementCount)
        assertTrue(ui.value.mockEnabled)
    }

    @Test
    fun replayWhileAlreadyInMockModeForcesCleanReentry() = runTest {
        repo.mockMode = true
        repo.mockModeSets.clear()
        newController().start(uri)
        advanceUntilIdle()
        // 一旦 false に落としてから true へ入り直す（クリーンな再突入）。
        assertEquals(listOf(false, true), repo.mockModeSets)
        assertTrue(ui.value.mockEnabled)
    }

    @Test
    fun scanTimeoutWithoutMockDeviceLeavesPlaybackUnconnected() = runTest {
        repo.advertiseOnScan = false
        newController().start(uri)
        advanceUntilIdle()
        assertTrue(ui.value.mockEnabled)
        assertEquals(1, repo.startScanCount)
        assertEquals(1, repo.stopScanCount)
        assertTrue(repo.connectedAddresses.isEmpty())
    }

    @Test
    fun exitReturnsToLiveBleState() = runTest {
        val c = newController()
        c.start(uri)
        advanceUntilIdle()
        assertTrue(repo.mockMode)

        c.exit()
        advanceUntilIdle()
        assertFalse(repo.mockMode)
        assertFalse(ui.value.mockEnabled)
        assertNull(ui.value.firmwareVersion)
        assertEquals(0, stopMeasurementCount)
    }

    @Test
    fun exitWhileMeasuringStopsMeasurement() = runTest {
        val c = newController()
        c.start(uri)
        advanceUntilIdle()
        ui.update { it.copy(isMeasuring = true) }

        c.exit()
        advanceUntilIdle()
        assertEquals(1, stopMeasurementCount)
        assertFalse(repo.mockMode)
    }

    @Test
    fun pauseFreezesPlaybackWhileMeasuring() = runTest {
        val c = newController()
        c.start(uri)
        advanceUntilIdle()
        ui.update { it.copy(isMeasuring = true) }

        c.pause()
        assertEquals(1, repo.pausePlaybackCount)
        assertTrue(ui.value.isPlaybackPaused)

        // A second Pause while already paused does not re-trigger the repo call.
        c.pause()
        assertEquals(1, repo.pausePlaybackCount)
    }

    @Test
    fun pauseOutsidePlaybackOrWhileNotMeasuringIsNoOp() = runTest {
        newController().pause()
        assertEquals(0, repo.pausePlaybackCount)
        assertFalse(ui.value.isPlaybackPaused)
    }

    @Test
    fun resumeContinuesPlaybackFromPause() = runTest {
        val c = newController()
        c.start(uri)
        advanceUntilIdle()
        ui.update { it.copy(isMeasuring = true) }
        c.pause()

        c.resume()
        assertEquals(1, repo.resumePlaybackCount)
        assertFalse(ui.value.isPlaybackPaused)
    }

    @Test
    fun resumeWithoutPauseIsNoOp() = runTest {
        val c = newController()
        c.start(uri)
        advanceUntilIdle()
        ui.update { it.copy(isMeasuring = true) }

        c.resume()
        assertEquals(0, repo.resumePlaybackCount)
    }

    @Test
    fun seekForwardsDeltaWhileMeasuring() = runTest {
        val c = newController()
        c.start(uri)
        advanceUntilIdle()
        ui.update { it.copy(isMeasuring = true) }

        c.seek(-5.0)
        c.seek(5.0)
        assertEquals(listOf(-5.0, 5.0), repo.seekPlaybackCalls)
    }

    @Test
    fun seekOutsidePlaybackOrWhileNotMeasuringIsNoOp() = runTest {
        newController().seek(5.0)
        assertTrue(repo.seekPlaybackCalls.isEmpty())
    }

    private companion object {
        /** CsvWriter が出力する形式の最小 CSV（Standard は値 11 列）。 */
        val VALID_CSV = """
            // Data mode  : Standard
            // Transmission speed  : 50Hz
            // Acceleration sensitivity  : 4g
            // Gyroscope sensitivity  : 500dps
            //ARTIFACT,NUM,DATE
            0,1,2026/01/01 00:00:00.000,1,2,3,4,5,6,7,8,9,10,11
        """.trimIndent()

        /** [VALID_CSV] のヘッダに対応する設定（既定値と異なる値で解析を確かめる）。 */
        val CSV_SETTINGS = MeasurementSettings(
            mode = MemeMode.Standard,
            quality = MemeQuality.Hz50,
            accRange = AccRange.G4,
            gyroRange = GyroRange.Dps500,
        )
    }
}
