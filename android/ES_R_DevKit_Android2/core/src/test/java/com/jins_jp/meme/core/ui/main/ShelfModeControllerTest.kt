package com.jins_jp.meme.core.ui.main

import com.jins_jp.meme.core.ble.ConnectionState
import com.jins_jp.meme.core.ble.MemeCommands
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
 * [ShelfModeController] の検証。CONFIG への遷移 → ACK → SHELF → 端末からの切断、の順序と、
 * ACK が来ない・切断が来ないときに「失敗」で終わること（ACK が無ければ SHELF は送らない）を通す。
 */
@OptIn(ExperimentalCoroutinesApi::class)
class ShelfModeControllerTest {

    private val repo = FakeMemeBleClient()
    private val connection = MutableStateFlow(ConnectionState.ServicesReady)
    private val ui = MutableStateFlow(MainUiState(connection = ConnectionState.ServicesReady))
    private val sent = mutableListOf<ByteArray>()

    private fun TestScope.newController(): Pair<ShelfModeController, ReconnectController> {
        val reconnect = ReconnectController(
            scope = this, repo = repo, ui = ui,
            onSuppressAutoConnect = {}, restartMeasurement = {}, stopMeasurementService = {},
        )
        return ShelfModeController(this, ui, connection, reconnect) { sent += it } to reconnect
    }

    private fun sentConfig() = sent.any { it.contentEquals(MemeCommands.setConfigMode()) }
    private fun sentShelf() = sent.any { it.contentEquals(MemeCommands.shelf()) }

    @Test
    fun requestOpensTheDialogOnlyWhenConnectedAndIdle() = runTest {
        val (c, _) = newController()
        ui.update { it.copy(isMeasuring = true) }
        c.request()
        assertFalse(ui.value.showShelfDialog)
        ui.update { it.copy(isMeasuring = false, isReplaying = true) }
        c.request()
        assertFalse(ui.value.showShelfDialog)
        ui.update { it.copy(isReplaying = false) }
        c.request()
        assertTrue(ui.value.showShelfDialog)
        c.dismiss()
        assertFalse(ui.value.showShelfDialog)
    }

    @Test
    fun sendsShelfAfterTheConfigAckAndSucceedsOnDisconnect() = runTest {
        val (c, reconnect) = newController()
        c.confirm()
        runCurrent()
        assertTrue(ui.value.isEnteringShelf)
        assertTrue(reconnect.userInitiatedDisconnect)                 // 移行後の切断で再接続しない
        assertTrue(sentConfig())
        assertFalse(sentShelf())
        c.onResp(true)
        runCurrent()
        assertTrue(sentShelf())
        connection.value = ConnectionState.Disconnected
        runCurrent()
        assertFalse(ui.value.isEnteringShelf)
        assertEquals("Entered shelf mode", ui.value.toast)
    }

    @Test
    fun withoutAckItFailsAndNeverSendsShelf() = runTest {
        val (c, _) = newController()
        c.confirm()
        runCurrent()
        advanceTimeBy(SHELF_CONFIG_ACK_TIMEOUT_MS + 1)
        runCurrent()
        assertFalse(sentShelf())
        assertFalse(ui.value.isEnteringShelf)
        assertEquals("Failed to enter shelf mode", ui.value.toast)
    }

    @Test
    fun nackAlsoFailsWithoutSendingShelf() = runTest {
        val (c, _) = newController()
        c.confirm()
        runCurrent()
        c.onResp(false)
        runCurrent()
        assertFalse(sentShelf())
        assertEquals("Failed to enter shelf mode", ui.value.toast)
    }

    @Test
    fun failsWhenTheDeviceDoesNotDisconnect() = runTest {
        val (c, _) = newController()
        c.confirm()
        runCurrent()
        c.onResp(true)
        runCurrent()
        advanceTimeBy(SHELF_DISCONNECT_TIMEOUT_MS + 1)
        runCurrent()
        assertTrue(sentShelf())
        assertFalse(ui.value.isEnteringShelf)
        assertEquals("Failed to enter shelf mode", ui.value.toast)
    }
}
