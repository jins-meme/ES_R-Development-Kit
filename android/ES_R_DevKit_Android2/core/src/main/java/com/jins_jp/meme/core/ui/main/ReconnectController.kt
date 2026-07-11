package com.jins_jp.meme.core.ui.main

import com.jins_jp.meme.core.ble.ConnectionState
import com.jins_jp.meme.core.ble.MemeBleClient
import com.jins_jp.meme.core.ble.MemeBleConstants
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.coroutines.withTimeoutOrNull

// Auto-reconnect tuning. The retry window is comfortably longer than
// SCAN_TIMEOUT_MS (8s) so each scan attempt has time to auto-stop before the
// next retry, matching the "scan stops once within the window" assumption.
private const val RECONNECT_RETRY_INTERVAL_MS = 15_000L
private const val RECONNECT_CONNECT_TIMEOUT_MS = 15_000L
private const val RECONNECT_NOTIFY_TIMEOUT_MS = 6_000L

/**
 * 計測中の予期しない切断からの自動再接続ループと、その判定に使う「最後に接続した
 * デバイス」「意図した切断か」の記録を担うコントローラ。[MainViewModel] からの
 * 抽出で、UI 状態は [ui]（isReconnecting）へ直接反映する。
 */
internal class ReconnectController(
    private val scope: CoroutineScope,
    private val repo: MemeBleClient,
    private val ui: MutableStateFlow<MainUiState>,
    // 再接続ループ中に発見イベント駆動の自動接続が競合しないよう抑止する。
    private val onSuppressAutoConnect: () -> Unit,
    // 再接続成立後に新しい CSV で計測を開始し直す（MainViewModel.startMeasurement）。
    private val restartMeasurement: () -> Unit,
    // 再接続を諦めた／中断した際に常駐サービスを畳む（計測中でなければ）。
    private val stopMeasurementService: () -> Unit,
) {
    private var reconnectJob: Job? = null

    // 切断時の自動再接続。lastDeviceAddress は切断時に BLE コールバックが
    // currentAddress をクリアした後も同一デバイスを再スキャンするため保持する。
    private var lastDeviceAddress: String? = null

    /** Disconnect ボタンなど意図した切断か。true の間は自動再接続しない。 */
    var userInitiatedDisconnect = false
        private set

    /** 再接続ループが動作中か（cancel されるまで true。UI の表示判定に使う）。 */
    val isRunning: Boolean get() = reconnectJob != null

    /** 接続を試みる直前に呼ぶ。再接続対象のアドレスを覚え、意図しない切断として扱う。 */
    fun noteConnectIntent(address: String) {
        userInitiatedDisconnect = false
        lastDeviceAddress = address
    }

    /** ユーザー操作による切断の直前に呼ぶ（自動再接続を抑止する）。 */
    fun noteUserDisconnect() {
        userInitiatedDisconnect = true
    }

    /**
     * Re-scan for the last connected device and, once found, reconnect and
     * restart measurement (with a fresh CSV file). Retries every
     * [RECONNECT_RETRY_INTERVAL_MS] until it succeeds or is cancelled by a
     * manual scan/connect or by turning the setting off.
     */
    fun start() {
        val addr = lastDeviceAddress ?: return
        reconnectJob?.cancel()
        onSuppressAutoConnect()
        reconnectJob = scope.launch {
            ui.update { it.copy(isReconnecting = true) }
            try {
                while (isActive) {
                    // The previous scan should have auto-stopped within the window;
                    // stop it defensively in case it is still running before rescanning.
                    if (repo.scanning.value) repo.stopScan()
                    repo.startScan()
                    // Auto-stop the scan after the normal timeout, well inside the window.
                    val stopper = launch {
                        delay(MemeBleConstants.SCAN_TIMEOUT_MS)
                        repo.stopScan()
                    }
                    val found = withTimeoutOrNull(RECONNECT_RETRY_INTERVAL_MS) {
                        repo.devices.first { addr in it }
                    } != null
                    stopper.cancel()
                    if (found) {
                        repo.stopScan()
                        if (reconnectAndRestart(addr)) break
                    }
                    // else: window elapsed without the device; loop and retry.
                }
            } finally {
                if (repo.scanning.value) repo.stopScan()
                ui.update { it.copy(isReconnecting = false) }
            }
        }
    }

    /**
     * Connect to [addr], wait for services and notifications to come up the same
     * way the normal connect flow does, then start a fresh measurement. Returns
     * true once measurement has been (re)started.
     */
    private suspend fun reconnectAndRestart(addr: String): Boolean {
        noteConnectIntent(addr)
        if (!repo.connect(addr)) return false
        val ready = withTimeoutOrNull(RECONNECT_CONNECT_TIMEOUT_MS) {
            repo.connection.first { it == ConnectionState.ServicesReady }
        } != null
        if (!ready) { repo.disconnect(); return false }
        // collectConnection enables notifications ~1.5s after ServicesReady;
        // wait for that to complete before streaming data.
        val notified = withTimeoutOrNull(RECONNECT_NOTIFY_TIMEOUT_MS) {
            repo.descriptorWritten.first()
        } != null
        if (!notified) { repo.disconnect(); return false }
        delay(500)
        restartMeasurement()
        return true
    }

    fun cancel() {
        val job = reconnectJob ?: return
        reconnectJob = null
        job.cancel()
        if (repo.scanning.value) repo.stopScan()
        ui.update { it.copy(isReconnecting = false) }
        // 再接続を諦めた／ユーザー操作で中断した場合、計測もしていないなら
        // 切断中に維持していたサービスをここで畳む。
        if (!ui.value.isMeasuring) stopMeasurementService()
    }
}
