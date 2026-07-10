package com.jins_jp.meme.core.ui.main

import android.app.Application
import android.net.Uri
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.CreationExtras
import com.jins.meme.academic.util.DataEncryption
import com.jins.meme.academic.util.HexDump
import com.jins.meme.academic.util.LogCat
import com.jins_jp.meme.core.App
import com.jins_jp.meme.core.ble.ConnectionState
import com.jins_jp.meme.core.ble.MemeBleConstants
import com.jins_jp.meme.core.ble.MemeBleRepository
import com.jins_jp.meme.core.ble.MockMemeBleEngine
import com.jins_jp.meme.core.data.CsvWriter
import com.jins_jp.meme.core.data.DataParser
import com.jins_jp.meme.core.data.MeasurementSettings
import com.jins_jp.meme.core.data.MemeMode
import com.jins_jp.meme.core.data.MemeQuality
import com.jins_jp.meme.core.data.MockCsvFormatException
import com.jins_jp.meme.core.data.MockCsvLoader
import com.jins_jp.meme.core.data.SettingsStore
import com.jins_jp.meme.core.data.formatRow
import com.jins_jp.meme.core.plugin.AlgoPlugin
import com.jins_jp.meme.core.service.MeasurementService
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.withContext
import kotlinx.coroutines.withTimeoutOrNull
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

private const val TAG = "MainViewModel"

// Auto-reconnect tuning. The retry window is comfortably longer than
// SCAN_TIMEOUT_MS (8s) so each scan attempt has time to auto-stop before the
// next retry, matching the "scan stops once within the window" assumption.
private const val RECONNECT_RETRY_INTERVAL_MS = 15_000L
private const val RECONNECT_CONNECT_TIMEOUT_MS = 15_000L
private const val RECONNECT_NOTIFY_TIMEOUT_MS = 6_000L

// スキャン窓内で 1 台も見つからなかった時に一度だけ張り直す前の小休止。
// コントローラのスキャン窓をリセットさせるための短い間隔。
private const val SCAN_RETRY_GAP_MS = 500L

data class MainUiState(
    val scanning: Boolean = false,
    val devices: List<String> = emptyList(),
    val selectedDeviceIndex: Int = 0,
    val connection: ConnectionState = ConnectionState.Disconnected,
    val firmwareVersion: String? = null,
    val settings: MeasurementSettings = MeasurementSettings(),
    val isMeasuring: Boolean = false,
    val isStarting: Boolean = false,
    val isInitializing: Boolean = false,
    val isMarking: Boolean = false,
    val recordingRows: Long = 0L,
    val batteryLevel: Int = -1,
    val successRate: Double = 1.0,
    val commRate: Double = 1.0,
    val toast: String? = null,
    val mockEnabled: Boolean = false,
    val mockError: String? = null,
    val bluetoothError: Boolean = false,
    val autoConnect: Boolean = false,
    val reconnectEnabled: Boolean = false,
    val isReconnecting: Boolean = false,
    // 計測完了時に「その他のアプリと共有」を自動で開くか（モックでない実機計測のみ対象）。
    val openSharingOnComplete: Boolean = false,
    val shareRequest: ShareRequest? = null,
)

/** 計測完了後に共有シートへ渡す CSV（本体データ＋サイドカーのうち存在するもの）の URI。 */
data class ShareRequest(val uris: List<Uri>)

sealed class GraphEvent {
    data class Eog(val x: Long, val vh: Float, val vv: Float) : GraphEvent()
    data class Acc(val x: Long, val x1: Float, val y: Float, val z: Float) : GraphEvent()
    data class Gyro(val x: Long, val x1: Float, val y: Float, val z: Float) : GraphEvent()
    /**
     * プラグイン発の任意ペイロード（マーカー・追加系列など）。型はアプリ側の知識で、
     * core は基本イベントと同一ストリームに順序を保って中継するだけ。
     */
    data class Custom(val payload: Any) : GraphEvent()
    object Reset : GraphEvent()
}

class MainViewModel(
    application: Application,
    private val repo: MemeBleRepository,
    val plugins: List<AlgoPlugin> = emptyList(),
) : AndroidViewModel(application) {

    private val settingsStore = SettingsStore(application)

    private val _ui = MutableStateFlow(
        MainUiState(
            settings = settingsStore.load(),
            // Playback (mock) is started on demand from the Play button and is
            // never persisted: every launch begins in live-BLE mode.
            autoConnect = settingsStore.loadAutoConnect(),
            reconnectEnabled = settingsStore.loadReconnectEnabled(),
            openSharingOnComplete = settingsStore.loadOpenSharingOnComplete(),
        )
    )
    val ui: StateFlow<MainUiState> = _ui.asStateFlow()

    private val _graph = MutableSharedFlow<GraphEvent>(extraBufferCapacity = 1024)
    val graph: SharedFlow<GraphEvent> = _graph.asSharedFlow()

    private val csv = CsvWriter(application)

    // Sample / timing book-keeping (mirrors the original totalCountUp logic)
    private var totalCount = 0L
    private var errorCount = 0L
    private var prevCount: Long = -1
    private var prevTimeMs: Long = 0
    private var graphSkipCount: Long = 4

    // EOG プロット点の通し番号（1 始まり）。プラグインがマーカー座標を
    // プロット点座標系へ換算するために onPlotPoint で渡す。
    private var plotCount = 0L

    // 自動接続：1スキャンにつき一度だけ発火（再接続ループを防ぐ）
    private var autoConnectAttempted = false

    // 切断時の自動再接続。lastDeviceAddress は切断時に BLE コールバックが
    // currentAddress をクリアした後も同一デバイスを再スキャンするため保持する。
    private var reconnectJob: Job? = null
    private var lastDeviceAddress: String? = null
    private var userInitiatedDisconnect = false

    // Comm-rate periodic job
    private var commTickerJob: Job? = null
    private var prevTotalPrev: Long = 0
    private var ratioPrev: Long = 100

    init {
        val emitter: (Any) -> Unit = { payload -> _graph.tryEmit(GraphEvent.Custom(payload)) }
        for (p in plugins) p.onAttached(emitter)
        viewModelScope.launch { collectScanning() }
        viewModelScope.launch { collectDevices() }
        viewModelScope.launch { collectConnection() }
        viewModelScope.launch { collectIncoming() }
        viewModelScope.launch { collectDescriptorWritten() }
    }

    override fun onCleared() {
        // Activity が破棄されると viewModelScope のデータパイプラインも止まるため、
        // 常駐サービスだけを残さない（プロセスが生きていても受信処理が死ぬため）。
        MeasurementService.stop(getApplication())
        super.onCleared()
    }

    fun setAutoConnect(enabled: Boolean) {
        if (ui.value.autoConnect == enabled) return
        settingsStore.saveAutoConnect(enabled)
        _ui.update { it.copy(autoConnect = enabled) }
        // 有効化時、すでに発見済みかつ未接続なら即接続
        if (enabled) maybeAutoConnect()
    }

    fun setReconnectEnabled(enabled: Boolean) {
        if (ui.value.reconnectEnabled == enabled) return
        settingsStore.saveReconnectEnabled(enabled)
        _ui.update { it.copy(reconnectEnabled = enabled) }
        if (!enabled) cancelAutoReconnect()
    }

    fun setOpenSharingOnComplete(enabled: Boolean) {
        if (ui.value.openSharingOnComplete == enabled) return
        settingsStore.saveOpenSharingOnComplete(enabled)
        _ui.update { it.copy(openSharingOnComplete = enabled) }
    }

    fun dismissShareRequest() { _ui.update { it.copy(shareRequest = null) } }

    /**
     * Play-button entry point. Opens the CSV chosen in the file dialog and, when
     * it is a valid logger CSV, enters playback (mock) mode: load the rows into
     * the mock engine, reflect/persist the CSV's measurement settings, then
     * emulate Scan device → Connect against the mock device (see
     * [startPlaybackScanConnect]). On failure surface the reason via
     * [MainUiState.mockError] and leave the current mode untouched. A null uri
     * means the user cancelled the dialog.
     *
     * Playback is invoked fresh on every Play tap, so this always re-enters mock
     * mode from a clean state even if a previous playback is still running.
     */
    fun startPlayback(uri: Uri?) {
        if (uri == null) return
        viewModelScope.launch {
            val result = runCatching {
                withContext(Dispatchers.IO) {
                    val app = getApplication<Application>()
                    val stream = app.contentResolver.openInputStream(uri)
                        ?: throw MockCsvFormatException("ファイルを開けませんでした。")
                    stream.use { MockCsvLoader.parse(it) }
                }
            }
            result.onSuccess { data ->
                cancelAutoReconnect()
                if (_ui.value.isMeasuring) stopMeasurement()
                // Force a clean (re-)entry into mock mode even when a previous
                // playback was already running, so each Play starts from scratch.
                if (repo.mockMode) repo.mockMode = false
                repo.mockMode = true
                repo.loadMockCsv(data)
                settingsStore.save(data.settings)
                _ui.update {
                    it.copy(
                        mockEnabled = true,
                        settings = data.settings,
                        isInitializing = false,
                        firmwareVersion = null,
                        mockError = null,
                        toast = "再生データを読み込みました（${data.rows.size} 行）",
                    )
                }
                startPlaybackScanConnect()
            }.onFailure { e ->
                _ui.update {
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
    private fun startPlaybackScanConnect() {
        // Suppress the discovery-driven auto-connect so it does not race us.
        autoConnectAttempted = true
        viewModelScope.launch {
            repo.startScan()
            val found = withTimeoutOrNull(MemeBleConstants.SCAN_TIMEOUT_MS) {
                repo.devices.first { MockMemeBleEngine.MOCK_ADDRESS in it }
            } != null
            repo.stopScan()
            if (!found) return@launch
            userInitiatedDisconnect = false
            lastDeviceAddress = MockMemeBleEngine.MOCK_ADDRESS
            _ui.update {
                it.copy(
                    selectedDeviceIndex =
                        it.devices.indexOf(MockMemeBleEngine.MOCK_ADDRESS).coerceAtLeast(0),
                )
            }
            repo.connect(MockMemeBleEngine.MOCK_ADDRESS)
        }
    }

    /** Leave playback mode and return to the initial live-BLE state. */
    private fun exitPlayback() {
        cancelAutoReconnect()
        if (_ui.value.isMeasuring) stopMeasurement()
        repo.mockMode = false
        _ui.update {
            it.copy(
                mockEnabled = false,
                isInitializing = false,
                firmwareVersion = null,
            )
        }
    }

    fun dismissMockError() { _ui.update { it.copy(mockError = null) } }

    fun dismissBluetoothError() { _ui.update { it.copy(bluetoothError = false) } }

    private suspend fun collectScanning() {
        repo.scanning.collect { v -> _ui.update { it.copy(scanning = v) } }
    }

    private suspend fun collectDevices() {
        repo.devices.collect { set ->
            _ui.update { st ->
                val list = set.toList().sorted()
                val idx = list.indexOf(list.getOrNull(st.selectedDeviceIndex)).coerceAtLeast(0)
                st.copy(devices = list, selectedDeviceIndex = idx.coerceAtMost((list.size - 1).coerceAtLeast(0)))
            }
            maybeAutoConnect()
        }
    }

    private fun maybeAutoConnect() {
        val st = ui.value
        if (!st.autoConnect || autoConnectAttempted) return
        if (st.connection != ConnectionState.Disconnected) return
        val address = st.devices.firstOrNull() ?: return
        autoConnectAttempted = true
        userInitiatedDisconnect = false
        lastDeviceAddress = address
        _ui.update { it.copy(selectedDeviceIndex = 0) }
        repo.connect(address)
    }

    private suspend fun collectConnection() {
        repo.connection.collect { state ->
            _ui.update { it.copy(connection = state) }
            when (state) {
                ConnectionState.ServicesReady -> {
                    delay(1500)
                    repo.enableNotifications()
                }
                ConnectionState.Disconnected -> {
                    val wasMeasuring = _ui.value.isMeasuring
                    // On any drop, finish appending and release the CSV files and
                    // stop the comm ticker. stopMeasurement() is not reached on an
                    // unexpected disconnect, so this is the only place that closes
                    // the files in that case (no-op if nothing was being written).
                    stopCommTicker()
                    for (p in plugins) p.onDisconnected(csv)
                    csv.stop()
                    // 切断イベント検知時は、原因(Disconnect ボタン/予期しない切断)によらず接続に
                    // 紐づく状態をすべて初期化し、スキャン前相当の idle へ確実に戻す。
                    // デバイス一覧・設定・自動接続などの永続項目は保持する。
                    _ui.update {
                        it.copy(
                            firmwareVersion = null,
                            isMeasuring = false,
                            isStarting = false,
                            isInitializing = false,
                            isMarking = false,
                            recordingRows = 0L,
                            batteryLevel = -1,
                            successRate = 1.0,
                            commRate = 1.0,
                        )
                    }
                    // 計測中の予期しない切断のみ、従来どおり自動再接続する(reconnect 設定 ON 時)。
                    val willReconnect = wasMeasuring &&
                        _ui.value.reconnectEnabled &&
                        !_ui.value.mockEnabled &&
                        !userInitiatedDisconnect
                    if (willReconnect) {
                        // 再接続ループは同一の計測セッションの続きなので、バックグラウンドで
                        // 切断されてもプロセスが死なないようサービスは止めずに維持する。
                        startAutoReconnect()
                    } else {
                        // 再接続ループが動いていない通常の切断は完全に idle へ戻す。
                        // ループ中の一時的な切断ならループに任せ、再接続表示は消さない。
                        if (reconnectJob == null) _ui.update { it.copy(isReconnecting = false) }
                        MeasurementService.stop(getApplication())
                    }
                }
                else -> Unit
            }
        }
    }

    private suspend fun collectDescriptorWritten() {
        repo.descriptorWritten.collect {
            delay(300); requestDeviceInfo()
        }
    }

    private suspend fun collectIncoming() {
        repo.incoming.collect { data ->
            handleIncoming(data)
        }
    }

    /* ---- User commands ---- */

    fun startScan() {
        // A manual scan overrides any auto-reconnect in progress.
        cancelAutoReconnect()
        if (!repo.hasScanPermission()) return
        if (!repo.isBluetoothEnabled()) {
            _ui.update { it.copy(bluetoothError = true) }
            return
        }
        autoConnectAttempted = false
        viewModelScope.launch {
            repo.startScan()
            delay(MemeBleConstants.SCAN_TIMEOUT_MS)
            repo.stopScan()
            // スキャン窓内で 1 台も見つからなければ、一度だけ張り直す。
            // (Android のスキャン頻度制限は 30 秒に 5 回なので 2 回は安全)
            if (repo.devices.value.isEmpty()) {
                delay(SCAN_RETRY_GAP_MS)
                repo.startScan()
                delay(MemeBleConstants.SCAN_TIMEOUT_MS)
                repo.stopScan()
            }
        }
    }

    fun selectDevice(index: Int) { _ui.update { it.copy(selectedDeviceIndex = index) } }

    fun connectOrDisconnect() {
        val st = ui.value
        if (st.connection != ConnectionState.Disconnected && st.connection != ConnectionState.Disconnecting) {
            // Mark the disconnect as deliberate so it does not trigger reconnect.
            userInitiatedDisconnect = true
            cancelAutoReconnect()
            // Disconnecting from playback returns to the initial live-BLE state.
            if (st.mockEnabled) exitPlayback() else repo.disconnect()
        } else {
            // A manual connect overrides any auto-reconnect in progress.
            cancelAutoReconnect()
            val address = st.devices.getOrNull(st.selectedDeviceIndex) ?: return
            userInitiatedDisconnect = false
            lastDeviceAddress = address
            repo.connect(address)
        }
    }

    fun updateSettings(transform: (MeasurementSettings) -> MeasurementSettings) {
        _ui.update { it.copy(settings = transform(it.settings)) }
        settingsStore.save(_ui.value.settings)
    }

    fun initialize() {
        viewModelScope.launch {
            _ui.update { it.copy(isInitializing = true) }
            val data = ByteArray(20)
            data[0] = MemeBleConstants.DATA_LENGTH
            data[1] = MemeBleConstants.ADN_CLR_PARAMS
            data[2] = 0xFF.toByte()
            sendEncoded(data)
            // Wait for ACK in collectIncoming
        }
    }

    fun toggleMeasurement() {
        if (ui.value.isMeasuring) stopMeasurement() else startMeasurement()
    }

    private fun startMeasurement() {
        viewModelScope.launch {
            _ui.update { it.copy(isStarting = true) }
            graphSkipCount = if (ui.value.settings.quality == MemeQuality.Hz100) 4L else 2L
            _graph.tryEmit(GraphEvent.Reset)
            plotCount = 0
            totalCount = 0; errorCount = 0; prevCount = -1; prevTimeMs = 0
            prevTotalPrev = 0; ratioPrev = 100

            val addr = repo.currentAddress()
            if (addr == null) {
                _ui.update { it.copy(isStarting = false) }
                return@launch
            }
            if (!ui.value.mockEnabled) {
                csv.start(addr, ui.value.settings)
                // 実機計測中はフォアグラウンドサービスでプロセス／CPU を保護し、
                // バックグラウンド・スリープ中も BLE 受信が途切れないようにする。
                // Mock 再生は BLE を使わないので不要。
                MeasurementService.start(getApplication())
            }
            // 検出器のリセットや mock 時のサイドカー CSV 準備などはプラグインが行う。
            for (p in plugins) {
                p.onMeasurementStart(ui.value.settings, csv, addr, ui.value.mockEnabled)
            }

            delay(0); sendSetMode()
            // 端末が実際に適用したモード/速度を読み戻して Toast で確認する。
            delay(300); requestMode()
            delay(500); sendSetParams()
            delay(1000)
            val startCmd = ByteArray(20)
            startCmd[0] = MemeBleConstants.DATA_LENGTH
            startCmd[1] = MemeBleConstants.ADN_START_STOP_SEND
            startCmd[2] = 0x01
            sendEncoded(startCmd)
            _ui.update { it.copy(isMeasuring = true, isStarting = false, recordingRows = 0L) }
            startCommTicker()
        }
    }

    private fun stopMeasurement() {
        viewModelScope.launch {
            val stopCmd = ByteArray(20)
            stopCmd[0] = MemeBleConstants.DATA_LENGTH
            stopCmd[1] = MemeBleConstants.ADN_START_STOP_SEND
            stopCmd[2] = 0x00
            sendEncoded(stopCmd)
            // 未確定の検出結果（1 秒未満の区間など）をプラグインが書き切ってから閉じる。
            for (p in plugins) p.onMeasurementStop(csv)
            val stopResult = csv.stop()
            stopCommTicker()
            MeasurementService.stop(getApplication())
            val shareUris = listOfNotNull(stopResult.dataUri, stopResult.classificationUri)
            _ui.update {
                it.copy(
                    isMeasuring = false,
                    recordingRows = 0L,
                    shareRequest = if (
                        it.openSharingOnComplete && !it.mockEnabled && shareUris.isNotEmpty()
                    ) ShareRequest(shareUris) else it.shareRequest,
                )
            }
        }
    }

    /* ---- Auto-reconnect ---- */

    /**
     * Re-scan for the last connected device and, once found, reconnect and
     * restart measurement (with a fresh CSV file). Retries every
     * [RECONNECT_RETRY_INTERVAL_MS] until it succeeds or is cancelled by a
     * manual scan/connect or by turning the setting off.
     */
    private fun startAutoReconnect() {
        val addr = lastDeviceAddress ?: return
        reconnectJob?.cancel()
        autoConnectAttempted = true
        reconnectJob = viewModelScope.launch {
            _ui.update { it.copy(isReconnecting = true) }
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
                _ui.update { it.copy(isReconnecting = false) }
            }
        }
    }

    /**
     * Connect to [addr], wait for services and notifications to come up the same
     * way the normal connect flow does, then start a fresh measurement. Returns
     * true once measurement has been (re)started.
     */
    private suspend fun reconnectAndRestart(addr: String): Boolean {
        userInitiatedDisconnect = false
        lastDeviceAddress = addr
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
        startMeasurement()
        return true
    }

    private fun cancelAutoReconnect() {
        val job = reconnectJob ?: return
        reconnectJob = null
        job.cancel()
        if (repo.scanning.value) repo.stopScan()
        _ui.update { it.copy(isReconnecting = false) }
        // 再接続を諦めた／ユーザー操作で中断した場合、計測もしていないなら
        // 切断中に維持していたサービスをここで畳む。
        if (!ui.value.isMeasuring) MeasurementService.stop(getApplication())
    }

    fun marking() {
        viewModelScope.launch {
            _ui.update { it.copy(isMarking = true) }
            delay(150)
            _ui.update { it.copy(isMarking = false) }
        }
    }

    /**
     * グラフ上のタップ位置（0..1 の横位置と可視プロット点数）をサンプル通し番号
     * (NUM) へ換算し、ラベル CSV サイドカーへ 1 行追記する。
     */
    fun markTap(fraction: Float, visiblePoints: Int) {
        if (!ui.value.isMeasuring) return
        val f = fraction.coerceIn(0f, 1f)
        val samplesBack = ((1f - f) * (visiblePoints - 1) * graphSkipCount).toLong()
        val markNum = (totalCount - samplesBack).coerceAtLeast(0L)
        csv.writeLabel(markNum)
        _ui.update { it.copy(toast = "マーク NUM=$markNum") }
    }

    fun dismissToast() { _ui.update { it.copy(toast = null) } }

    /* ---- Protocol helpers ---- */

    private fun requestDeviceInfo() {
        val data = ByteArray(20)
        data[0] = MemeBleConstants.DATA_LENGTH
        data[1] = MemeBleConstants.ADN_GET_DEV_INFO
        sendEncoded(data)
    }

    private fun requestMode() {
        val data = ByteArray(20)
        data[0] = MemeBleConstants.DATA_LENGTH
        data[1] = MemeBleConstants.ADN_GET_MODE
        sendEncoded(data)
    }

    private fun sendSetMode() {
        val s = ui.value.settings
        val data = ByteArray(20)
        data[0] = MemeBleConstants.DATA_LENGTH
        data[1] = MemeBleConstants.ADN_SET_MODE
        // ファーム(MEMELib memeAdnSetMode)は mode=byte4, quality(transMode)=byte5 を読む。
        data[4] = ((s.mode.ordinal + 1) and 0xFF).toByte()
        data[5] = ((s.quality.ordinal + 1) and 0xFF).toByte()
        sendEncoded(data)
    }

    private fun sendSetParams() {
        val s = ui.value.settings
        val data = ByteArray(20)
        data[0] = MemeBleConstants.DATA_LENGTH
        data[1] = MemeBleConstants.ADN_SET_6AXIS_PARAMS
        val gyroIdx = if (s.mode == MemeMode.Quaternion) 3 else s.gyroRange.ordinal
        data[2] = (s.accRange.ordinal and 0xFF).toByte()
        data[3] = (gyroIdx and 0xFF).toByte()
        sendEncoded(data)
    }

    private fun sendEncoded(data: ByteArray) {
        LogCat.d(TAG, "send: " + HexDump.toHexString(data))
        repo.send(DataEncryption.encode(data))
    }

    /* ---- Incoming dispatch ---- */

    private fun handleIncoming(data: ByteArray) {
        LogCat.d(TAG, "recv: " + HexDump.toHexString(data))
        val packets = DataParser.parse(data)
        if (packets.isEmpty()) {
            if (data.size >= 2) {
                when (data[1]) {
                    MemeBleConstants.AUP_REPORT_DEV_INFO -> handleDevInfo(data)
                    MemeBleConstants.AUP_REPORT_MODE -> handleMode(data)
                    MemeBleConstants.AUP_REPORT_RESP -> handleResp(data)
                }
            }
            return
        }

        // DATE: 通知(パケット)を受信日時で刻む。100Hz で 40byte(2 パケット)が届いた
        // 場合、1 行目は受信日時、2 行目は 1 行目 + 1/transmission_speed(s)。prevTimeMs は
        // 「処理中サンプルの時刻」として CSV 行・プラグインの両方から参照される。
        val recvTimeMs = System.currentTimeMillis()
        val intervalMs = 1000L / ui.value.settings.quality.hz // 100Hz→10ms, 50Hz→20ms
        for ((index, packet) in packets.withIndex()) {
            // 最初のパケットは前回カウンタの基準取得のみに使い、記録・検出しない。
            if (!totalCountUp(packet.packetCount)) continue
            prevTimeMs = recvTimeMs + index * intervalMs

            // 全サンプル（間引き前）をプラグインへ渡す。検出器はプロットより
            // 高い分解能で回し、確定結果だけを GraphEvent.Custom で発行させる。
            for (p in plugins) p.onSample(packet.type, packet.values, totalCount, prevTimeMs, csv)

            if ((totalCount % graphSkipCount) == 0L) {
                val x = totalCount / graphSkipCount
                when (packet.type) {
                    MemeBleConstants.AUP_REPORT_ACADEMIA1 -> {
                        val v = packet.values
                        _graph.tryEmit(GraphEvent.Eog(x, v[7].toFloat(), v[9].toFloat()))
                        _graph.tryEmit(GraphEvent.Acc(x, v[0].toFloat(), v[1].toFloat(), v[2].toFloat()))
                    }
                    MemeBleConstants.AUP_REPORT_ACADEMIA2 -> {
                        val v = packet.values
                        _graph.tryEmit(GraphEvent.Eog(x, v[8].toFloat(), v[9].toFloat()))
                        _graph.tryEmit(GraphEvent.Acc(x, v[0].toFloat(), v[1].toFloat(), v[2].toFloat()))
                        _graph.tryEmit(GraphEvent.Gyro(x, v[3].toFloat(), v[4].toFloat(), v[5].toFloat()))
                    }
                    MemeBleConstants.AUP_REPORT_ACADEMIA3 -> Unit
                }
                // EOG プロット点を発行したパケットのみプロット通し番号を進め、
                // プラグインへマーカー座標の基準を知らせる。
                if (packet.type == MemeBleConstants.AUP_REPORT_ACADEMIA1 ||
                    packet.type == MemeBleConstants.AUP_REPORT_ACADEMIA2
                ) {
                    plotCount += 1
                    for (p in plugins) {
                        p.onPlotPoint(packet.type, packet.values, plotCount, graphSkipCount)
                    }
                }
            }

            if (!ui.value.mockEnabled) {
                val row = formatRow(ui.value.isMarking, totalCount, prevTimeMs, packet.values)
                csv.writeRow(row)
            }
        }

        _ui.update {
            it.copy(
                recordingRows = if (ui.value.mockEnabled) 0 else csv.recordedRows,
                batteryLevel = packets.last().batteryLevel.toInt(),
            )
        }

        // success rate from total/error
        if (totalCount > 0) {
            val rate = 1.0 - errorCount.toDouble() / totalCount.toDouble()
            _ui.update { it.copy(successRate = rate.coerceIn(0.0, 1.0)) }
        }
    }

    private fun handleDevInfo(data: ByteArray) {
        val major = (data[3].toInt() and 0xFF) shl 8 or (data[2].toInt() and 0xFF)
        val v = "%X-%d.%d.%d".format(major, data[6].toInt(), data[5].toInt(), data[4].toInt())
        _ui.update { it.copy(firmwareVersion = v) }
    }

    /**
     * AUP_REPORT_MODE(0x83): 端末が実際に適用したモード/伝送速度を読み出した結果。
     * mode=byte4, quality=byte5(いずれも ordinal+1)。設定が反映されたか確認できる
     * よう Toast で表示する。
     */
    private fun handleMode(data: ByteArray) {
        val modeVal = data[4].toInt() and 0xFF
        val qualityVal = data[5].toInt() and 0xFF
        val modeName = MemeMode.entries.getOrNull(modeVal - 1)?.display ?: "Mode $modeVal"
        val hz = MemeQuality.entries.getOrNull(qualityVal - 1)?.display ?: "Quality $qualityVal"
        _ui.update { it.copy(toast = "モード設定: $modeName / $hz") }
    }

    private fun handleResp(data: ByteArray) {
        if (ui.value.isInitializing) {
            when (data[2]) {
                0x00.toByte() -> _ui.update { it.copy(isInitializing = false, toast = "Success to initialize") }
                0xFF.toByte() -> _ui.update { it.copy(isInitializing = false, toast = "Failed to initialize") }
            }
        }
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
    private fun totalCountUp(count: Short): Boolean {
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

    private fun startCommTicker() {
        commTickerJob?.cancel()
        commTickerJob = viewModelScope.launch {
            delay(200)
            while (true) {
                val s = ui.value
                val period = 400L / (((s.settings.quality.ordinal + 1) and 0xFF) * 10L)
                val count = totalCount - prevTotalPrev
                val ratioLast = if (period == 0L) 0L
                else ((count.toDouble() / period.toDouble()) * 100.0).toLong()
                val ratio = (ratioLast + ratioPrev) / 2
                prevTotalPrev += count
                ratioPrev = ratioLast
                _ui.update { it.copy(commRate = (ratio.coerceIn(0, 100)).toDouble() / 100.0) }
                delay(400)
            }
        }
    }

    private fun stopCommTicker() {
        commTickerJob?.cancel(); commTickerJob = null
    }

    companion object {
        /** プラグインなし（素のロガー挙動）のファクトリ。 */
        val Factory: ViewModelProvider.Factory = factory()

        /**
         * アプリ固有の [AlgoPlugin] を差し込むファクトリ。core 自体はプラグインを
         * 一切登録しないので、引数なしはプラグインなしと同義。
         */
        fun factory(vararg plugins: AlgoPlugin): ViewModelProvider.Factory =
            object : ViewModelProvider.Factory {
                override fun <T : androidx.lifecycle.ViewModel> create(
                    modelClass: Class<T>,
                    extras: CreationExtras,
                ): T {
                    val app = extras[ViewModelProvider.AndroidViewModelFactory.APPLICATION_KEY] as App
                    @Suppress("UNCHECKED_CAST")
                    return MainViewModel(app, app.bleRepository, plugins.toList()) as T
                }
            }
    }
}
