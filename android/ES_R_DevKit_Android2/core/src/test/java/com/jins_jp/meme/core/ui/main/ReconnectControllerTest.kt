package com.jins_jp.meme.core.ui.main

import com.jins_jp.meme.core.ble.ConnectionState
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.test.TestScope
import kotlinx.coroutines.test.advanceTimeBy
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * [ReconnectController] の検証。仮想時間で再接続ループの
 * スキャン→発見→接続→通知確立→計測再開と、各段階のタイムアウト・
 * リトライ・cancel の後始末を通す。時間の定数はコントローラ内の
 * RECONNECT_*（リトライ窓 15s・接続 15s・通知 6s）と
 * MemeBleConstants.SCAN_TIMEOUT_MS(8s) に対応する。
 */
@OptIn(ExperimentalCoroutinesApi::class)
class ReconnectControllerTest {

    private val repo = FakeMemeBleClient()
    private val ui = MutableStateFlow(MainUiState())
    private var suppressCount = 0
    private var restartCount = 0
    private var stopServiceCount = 0

    private fun TestScope.newController() = ReconnectController(
        scope = this,
        repo = repo,
        ui = ui,
        onSuppressAutoConnect = { suppressCount++ },
        restartMeasurement = { restartCount++ },
        stopMeasurementService = { stopServiceCount++ },
    )

    @Test
    fun startWithoutKnownDeviceDoesNothing() = runTest {
        val c = newController()
        c.start()
        runCurrent()
        assertFalse(c.isRunning)
        assertFalse(ui.value.isReconnecting)
        assertEquals(0, repo.startScanCount)
        assertEquals(0, suppressCount)
    }

    @Test
    fun disconnectIntentFlagFollowsNotes() = runTest {
        val c = newController()
        assertFalse(c.userInitiatedDisconnect)
        c.noteUserDisconnect()
        assertTrue(c.userInitiatedDisconnect)
        // 次の接続意図で「意図しない切断」扱いに戻る。
        c.noteConnectIntent(ADDR)
        assertFalse(c.userInitiatedDisconnect)
    }

    @Test
    fun reconnectsAndRestartsMeasurementWhenDeviceReappears() = runTest {
        val c = newController()
        c.noteConnectIntent(ADDR)
        c.start()
        runCurrent()
        assertTrue(c.isRunning)
        assertTrue(ui.value.isReconnecting)
        assertEquals(1, suppressCount)
        assertEquals(1, repo.startScanCount)

        // デバイス再発見 → スキャン停止して接続。
        repo.devices.value = setOf(ADDR)
        runCurrent()
        assertEquals(1, repo.stopScanCount)
        assertEquals(listOf(ADDR), repo.connectedAddresses)

        // 通常接続と同じく ServicesReady → 通知確立 → 500ms 後に計測再開。
        repo.connection.value = ConnectionState.ServicesReady
        runCurrent()
        assertTrue(repo.descriptorWritten.tryEmit(Unit))
        runCurrent()
        assertEquals(0, restartCount)
        advanceTimeBy(500)
        runCurrent()
        assertEquals(1, restartCount)
        assertFalse(ui.value.isReconnecting)
        assertEquals(0, repo.disconnectCount)
        // 成功してループが終わったら「動作中」ではない。
        assertFalse(c.isRunning)
    }

    @Test
    fun retriesScanWhenDeviceNotFoundInWindow() = runTest {
        val c = newController()
        c.noteConnectIntent(ADDR)
        c.start()
        runCurrent()
        assertEquals(1, repo.startScanCount)

        // スキャンは通常タイムアウト(8s)で自動停止し、窓(15s)の残りは待機。
        advanceTimeBy(8_000)
        runCurrent()
        assertEquals(1, repo.stopScanCount)
        assertTrue(ui.value.isReconnecting)

        // 窓が明けたら再スキャン。
        advanceTimeBy(7_000)
        runCurrent()
        assertEquals(2, repo.startScanCount)
        assertTrue(ui.value.isReconnecting)

        c.cancel()
    }

    @Test
    fun failedConnectRetriesLoop() = runTest {
        repo.connectResult = false
        val c = newController()
        c.noteConnectIntent(ADDR)
        c.start()
        runCurrent()
        repo.devices.value = setOf(ADDR)
        runCurrent()
        // connect 失敗 → 即座に次のスキャンへ。
        assertEquals(listOf(ADDR), repo.connectedAddresses)
        assertEquals(2, repo.startScanCount)
        assertEquals(0, restartCount)
        assertTrue(ui.value.isReconnecting)

        c.cancel()
    }

    @Test
    fun servicesReadyTimeoutDisconnectsAndRetries() = runTest {
        val c = newController()
        c.noteConnectIntent(ADDR)
        c.start()
        runCurrent()
        repo.devices.value = setOf(ADDR)
        runCurrent()
        assertEquals(listOf(ADDR), repo.connectedAddresses)

        // ServicesReady が 15s 来ない → 切断してリトライ。
        advanceTimeBy(15_000)
        runCurrent()
        assertEquals(1, repo.disconnectCount)
        assertEquals(2, repo.startScanCount)
        assertEquals(0, restartCount)

        c.cancel()
    }

    @Test
    fun notificationTimeoutDisconnectsAndRetries() = runTest {
        val c = newController()
        c.noteConnectIntent(ADDR)
        c.start()
        runCurrent()
        repo.devices.value = setOf(ADDR)
        runCurrent()
        repo.connection.value = ConnectionState.ServicesReady
        runCurrent()

        // descriptorWritten が 6s 来ない → 切断してリトライ。
        advanceTimeBy(6_000)
        runCurrent()
        assertEquals(1, repo.disconnectCount)
        assertEquals(2, repo.startScanCount)
        assertEquals(0, restartCount)

        c.cancel()
    }

    @Test
    fun cancelStopsScanAndServiceWhenIdle() = runTest {
        val c = newController()
        c.noteConnectIntent(ADDR)
        c.start()
        runCurrent()
        assertTrue(repo.scanning.value)

        c.cancel()
        runCurrent()
        assertFalse(c.isRunning)
        assertFalse(ui.value.isReconnecting)
        assertFalse(repo.scanning.value)
        // 計測もしていないので常駐サービスを畳む。
        assertEquals(1, stopServiceCount)
    }

    @Test
    fun cancelKeepsServiceWhileMeasuring() = runTest {
        val c = newController()
        c.noteConnectIntent(ADDR)
        c.start()
        runCurrent()

        ui.update { it.copy(isMeasuring = true) }
        c.cancel()
        runCurrent()
        assertEquals(0, stopServiceCount)
    }

    @Test
    fun cancelWithoutRunningLoopIsNoOp() = runTest {
        val c = newController()
        c.cancel()
        assertEquals(0, stopServiceCount)
        assertEquals(0, repo.stopScanCount)
    }

    private companion object {
        const val ADDR = "AA:BB:CC:DD:EE:FF"
    }
}
