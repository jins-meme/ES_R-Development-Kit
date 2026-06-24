package com.jins_jp.meme.academic.ui.main

import android.app.Application
import android.net.Uri
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.CreationExtras
import com.jins.meme.academic.util.HexDump
import com.jins.meme.academic.util.LogCat
import com.jins_jp.meme.academic.App
import com.jins_jp.meme.academic.ble.ConnectionState
import com.jins_jp.meme.academic.ble.MemeBleConstants
import com.jins_jp.meme.academic.ble.MemeBleRepository
import com.jins_jp.meme.academic.ble.MockMemeBleEngine
import com.jins_jp.meme.academic.data.AccRange
import com.jins_jp.meme.academic.data.CsvWriter
import com.jins_jp.meme.academic.data.DataParser
import com.jins_jp.meme.academic.data.GyroRange
import com.jins_jp.meme.academic.data.MeasurementSettings
import com.jins_jp.meme.academic.data.MemeMode
import com.jins_jp.meme.academic.data.MemeQuality
import com.jins_jp.meme.academic.data.MockCsvFormatException
import com.jins_jp.meme.academic.data.MockCsvLoader
import com.jins_jp.meme.academic.data.SettingsStore
import com.jins_jp.meme.academic.data.formatRow
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
private const val GRAPH_WIDTH = 200
private const val GRAPH_SKIP_CULL = 25L

// Auto-reconnect tuning. The retry window is comfortably longer than
// SCAN_TIMEOUT_MS (8s) so each scan attempt has time to auto-stop before the
// next retry, matching the "scan stops once within the window" assumption.
private const val RECONNECT_RETRY_INTERVAL_MS = 15_000L
private const val RECONNECT_CONNECT_TIMEOUT_MS = 15_000L
private const val RECONNECT_NOTIFY_TIMEOUT_MS = 6_000L

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
    val reconnectEnabled: Boolean = false,
    val isReconnecting: Boolean = false,
)

sealed class GraphEvent {
    data class Eog(val x: Long, val vh: Float, val vv: Float) : GraphEvent()
    data class Acc(val x: Long, val x1: Float, val y: Float, val z: Float) : GraphEvent()
    data class Gyro(val x: Long, val x1: Float, val y: Float, val z: Float) : GraphEvent()
    object Reset : GraphEvent()
}

class MainViewModel(
    application: Application,
    private val repo: MemeBleRepository,
) : AndroidViewModel(application) {

    private val settingsStore = SettingsStore(application)

    private val _ui = MutableStateFlow(
        MainUiState(
            settings = settingsStore.load(),
            // Playback (mock) is started on demand from the Play button and is
            // never persisted: every launch begins in live-BLE mode.
            reconnectEnabled = settingsStore.loadReconnectEnabled(),
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

    // Comm-rate periodic job
    private var commTickerJob: Job? = null
    private var prevTotalLast: Long = 0
    private var prevTotalPrev: Long = 0
    private var ratioPrev: Long = 100

    // Auto-reconnect book-keeping. lastDeviceAddress survives the BLE callback
    // clearing currentAddress on disconnect so we can rescan for the same device.
    private var reconnectJob: Job? = null
    private var lastDeviceAddress: String? = null
    private var userInitiatedDisconnect = false

    init {
        viewModelScope.launch { collectScanning() }
        viewModelScope.launch { collectDevices() }
        viewModelScope.launch { collectConnection() }
        viewModelScope.launch { collectIncoming() }
        viewModelScope.launch { collectDescriptorWritten() }
    }

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
        }
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
                    // On any drop, finish appending and release the CSV file and
                    // stop the comm ticker. stopMeasurement() is not reached on an
                    // unexpected disconnect, so this is the only place that closes
                    // the file in that case (no-op if nothing was being written).
                    stopCommTicker()
                    if (!_ui.value.mockEnabled) csv.stop()
                    _ui.update {
                        it.copy(
                            firmwareVersion = null,
                            isMeasuring = false,
                            isStarting = false,
                            recordingRows = 0L,
                        )
                    }
                    // Auto-reconnect only on an unexpected drop while measuring.
                    if (wasMeasuring &&
                        _ui.value.reconnectEnabled &&
                        !_ui.value.mockEnabled &&
                        !userInitiatedDisconnect
                    ) {
                        startAutoReconnect()
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
        viewModelScope.launch {
            repo.startScan()
            delay(MemeBleConstants.SCAN_TIMEOUT_MS)
            repo.stopScan()
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

    fun setReconnectEnabled(enabled: Boolean) {
        settingsStore.saveReconnectEnabled(enabled)
        _ui.update { it.copy(reconnectEnabled = enabled) }
        if (!enabled) cancelAutoReconnect()
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
            totalCount = 0; errorCount = 0; prevCount = -1; prevTimeMs = 0
            prevTotalLast = 0; prevTotalPrev = 0; ratioPrev = 100

            val addr = repo.currentAddress()
            if (addr == null) {
                _ui.update { it.copy(isStarting = false) }
                return@launch
            }
            if (!ui.value.mockEnabled) {
                csv.start(addr, ui.value.settings)
            }

            delay(0); sendSetMode()
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
            if (!ui.value.mockEnabled) {
                csv.stop()
            }
            stopCommTicker()
            _ui.update { it.copy(isMeasuring = false, recordingRows = 0L) }
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
    }

    fun marking() {
        viewModelScope.launch {
            _ui.update { it.copy(isMarking = true) }
            delay(150)
            _ui.update { it.copy(isMarking = false) }
        }
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

    private fun requestParams() {
        val data = ByteArray(20)
        data[0] = MemeBleConstants.DATA_LENGTH
        data[1] = MemeBleConstants.ADN_GET_6AXIS_PARAMS
        sendEncoded(data)
    }

    private fun sendSetMode() {
        val s = ui.value.settings
        val data = ByteArray(20)
        data[0] = MemeBleConstants.DATA_LENGTH
        data[1] = MemeBleConstants.ADN_SET_MODE
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
        repo.send(com.jins.meme.academic.util.DataEncryption.encode(data))
    }

    /* ---- Incoming dispatch ---- */

    private fun handleIncoming(data: ByteArray) {
        LogCat.d(TAG, "recv: " + HexDump.toHexString(data))
        if (data.size < 2) return
        when (data[1]) {
            MemeBleConstants.AUP_REPORT_DEV_INFO -> handleDevInfo(data)
            MemeBleConstants.AUP_REPORT_RESP -> handleResp(data)
            MemeBleConstants.AUP_REPORT_ACADEMIA1,
            MemeBleConstants.AUP_REPORT_ACADEMIA2,
            MemeBleConstants.AUP_REPORT_ACADEMIA3 -> handleAcademia(data)
        }
    }

    private fun handleDevInfo(data: ByteArray) {
        val major = (data[3].toInt() and 0xFF) shl 8 or (data[2].toInt() and 0xFF)
        val v = "%X-%d.%d.%d".format(major, data[6].toInt(), data[5].toInt(), data[4].toInt())
        _ui.update { it.copy(firmwareVersion = v) }
    }

    private fun handleResp(data: ByteArray) {
        if (ui.value.isInitializing) {
            when (data[2]) {
                0x00.toByte() -> _ui.update { it.copy(isInitializing = false, toast = "Success to initialize") }
                0xFF.toByte() -> _ui.update { it.copy(isInitializing = false, toast = "Failed to initialize") }
            }
        }
    }

    private fun handleAcademia(data: ByteArray) {
        val packet = DataParser.parse(data) ?: return
        val q = ui.value.settings.quality.hz / 50 // 100Hz→2, 50Hz→1 → ms per sample = 10*q
        totalCountUp(packet.packetCount, q)

        if ((totalCount % graphSkipCount) == 0L) {
            val x = totalCount / graphSkipCount
            when (data[1]) {
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
        }

        val row = formatRow(ui.value.isMarking, totalCount, prevTimeMs, packet.values)
        if (!ui.value.mockEnabled) {
            csv.writeRow(row)
        }
        _ui.update { it.copy(recordingRows = csv.recordedRows, batteryLevel = packet.batteryLevel.toInt()) }

        // success rate from total/error
        if (totalCount > 0) {
            val rate = 1.0 - errorCount.toDouble() / totalCount.toDouble()
            _ui.update { it.copy(successRate = rate.coerceIn(0.0, 1.0)) }
        }
    }

    private fun totalCountUp(count: Short, quality: Int) {
        val cnt = count.toInt() and 0xFFFF
        var diff = 0L
        if (prevCount < 0) {
            diff = 0; prevTimeMs = System.currentTimeMillis()
        } else if (prevCount < cnt) {
            diff = cnt - prevCount
        } else if (prevCount > cnt) {
            diff = 0x1000L - prevCount + cnt
        }
        prevCount = cnt.toLong()
        prevTimeMs += diff * 10L * quality
        if (diff == 0L) {
            totalCount += 1
        } else {
            totalCount += diff
            if (diff - 1 > 0) errorCount += diff - 1
        }
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
        val Factory: ViewModelProvider.Factory = object : ViewModelProvider.Factory {
            override fun <T : androidx.lifecycle.ViewModel> create(
                modelClass: Class<T>,
                extras: CreationExtras,
            ): T {
                val app = extras[ViewModelProvider.AndroidViewModelFactory.APPLICATION_KEY] as App
                @Suppress("UNCHECKED_CAST")
                return MainViewModel(app, app.bleRepository) as T
            }
        }
    }
}
