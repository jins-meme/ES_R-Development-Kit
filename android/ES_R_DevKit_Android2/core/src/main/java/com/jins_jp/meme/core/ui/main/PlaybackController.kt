package com.jins_jp.meme.core.ui.main

import android.app.Application
import android.net.Uri
import com.jins_jp.meme.core.ble.MemeBleConstants
import com.jins_jp.meme.core.ble.MemeBleRepository
import com.jins_jp.meme.core.ble.MockMemeBleEngine
import com.jins_jp.meme.core.data.MockCsvFormatException
import com.jins_jp.meme.core.data.MockCsvLoader
import com.jins_jp.meme.core.data.SettingsStore
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import kotlinx.coroutines.withTimeoutOrNull

/**
 * CSV 再生（mock）モードへの出入りを担うコントローラ。[MainViewModel] からの
 * 抽出で、UI 状態は [ui]（mockEnabled/mockError/settings など）へ直接反映する。
 */
internal class PlaybackController(
    private val application: Application,
    private val scope: CoroutineScope,
    private val repo: MemeBleRepository,
    private val settingsStore: SettingsStore,
    private val ui: MutableStateFlow<MainUiState>,
    private val reconnect: ReconnectController,
    // 再生のスキャン→接続エミュレーションと発見イベント駆動の自動接続が競合しないよう抑止する。
    private val onSuppressAutoConnect: () -> Unit,
    private val stopMeasurement: () -> Unit,
) {
    /**
     * Play-button entry point. Opens the CSV chosen in the file dialog and, when
     * it is a valid logger CSV, enters playback (mock) mode: load the rows into
     * the mock engine, reflect/persist the CSV's measurement settings, then
     * emulate Scan device → Connect against the mock device (see
     * [startScanConnect]). On failure surface the reason via
     * [MainUiState.mockError] and leave the current mode untouched. A null uri
     * means the user cancelled the dialog.
     *
     * Playback is invoked fresh on every Play tap, so this always re-enters mock
     * mode from a clean state even if a previous playback is still running.
     */
    fun start(uri: Uri?) {
        if (uri == null) return
        scope.launch {
            val result = runCatching {
                withContext(Dispatchers.IO) {
                    val stream = application.contentResolver.openInputStream(uri)
                        ?: throw MockCsvFormatException("ファイルを開けませんでした。")
                    stream.use { MockCsvLoader.parse(it) }
                }
            }
            result.onSuccess { data ->
                reconnect.cancel()
                if (ui.value.isMeasuring) stopMeasurement()
                // Force a clean (re-)entry into mock mode even when a previous
                // playback was already running, so each Play starts from scratch.
                if (repo.mockMode) repo.mockMode = false
                repo.mockMode = true
                repo.loadMockCsv(data)
                settingsStore.save(data.settings)
                ui.update {
                    it.copy(
                        mockEnabled = true,
                        settings = data.settings,
                        isInitializing = false,
                        firmwareVersion = null,
                        mockError = null,
                        toast = "再生データを読み込みました（${data.rows.size} 行）",
                    )
                }
                startScanConnect()
            }.onFailure { e ->
                ui.update {
                    it.copy(
                        mockError = (e as? MockCsvFormatException)?.message
                            ?: "CSVの読み込みに失敗しました。",
                    )
                }
            }
        }
    }

    /**
     * Playback one-shot: emulate the Scan device button against the mock engine,
     * then auto-connect to the mock device as soon as it is advertised. Mirrors
     * the manual Scan → Connect flow so the rest of the app sees an ordinary
     * connection coming up.
     */
    private fun startScanConnect() {
        // Suppress the discovery-driven auto-connect so it does not race us.
        onSuppressAutoConnect()
        scope.launch {
            repo.startScan()
            val found = withTimeoutOrNull(MemeBleConstants.SCAN_TIMEOUT_MS) {
                repo.devices.first { MockMemeBleEngine.MOCK_ADDRESS in it }
            } != null
            repo.stopScan()
            if (!found) return@launch
            reconnect.noteConnectIntent(MockMemeBleEngine.MOCK_ADDRESS)
            ui.update {
                it.copy(
                    selectedDeviceIndex =
                        it.devices.indexOf(MockMemeBleEngine.MOCK_ADDRESS).coerceAtLeast(0),
                )
            }
            repo.connect(MockMemeBleEngine.MOCK_ADDRESS)
        }
    }

    /** Leave playback mode and return to the initial live-BLE state. */
    fun exit() {
        reconnect.cancel()
        if (ui.value.isMeasuring) stopMeasurement()
        repo.mockMode = false
        ui.update {
            it.copy(
                mockEnabled = false,
                isInitializing = false,
                firmwareVersion = null,
            )
        }
    }
}
