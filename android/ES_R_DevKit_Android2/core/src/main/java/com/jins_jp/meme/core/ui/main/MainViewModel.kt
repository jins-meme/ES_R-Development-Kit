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
import com.jins_jp.meme.core.ble.MemeCommands
import com.jins_jp.meme.core.data.CsvWriter
import com.jins_jp.meme.core.data.DataParser
import com.jins_jp.meme.core.data.MeasurementSettings
import com.jins_jp.meme.core.data.MemeMode
import com.jins_jp.meme.core.data.MemeQuality
import com.jins_jp.meme.core.data.SampleCounter
import com.jins_jp.meme.core.data.SettingsStore
import com.jins_jp.meme.core.data.formatRow
import com.jins_jp.meme.core.plugin.AlgoPlugin
import com.jins_jp.meme.core.service.MeasurementService
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

private const val TAG = "MainViewModel"

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

    // Sample / timing book-keeping
    private val counter = SampleCounter()
    private var prevTimeMs: Long = 0
    private var graphSkipCount: Long = 4

    // EOG プロット点の通し番号（1 始まり）。プラグインがマーカー座標を
    // プロット点座標系へ換算するために onPlotPoint で渡す。
    private var plotCount = 0L

    // 自動接続：1スキャンにつき一度だけ発火（再接続ループを防ぐ）
    private var autoConnectAttempted = false

    // 計測中の予期しない切断からの自動再接続と、CSV 再生（mock）モードの出入り。
    private val reconnect = ReconnectController(
        scope = viewModelScope,
        repo = repo,
        ui = _ui,
        onSuppressAutoConnect = { autoConnectAttempted = true },
        restartMeasurement = ::startMeasurement,
        stopMeasurementService = { MeasurementService.stop(getApplication()) },
    )
    private val playback = PlaybackController(
        scope = viewModelScope,
        repo = repo,
        ui = _ui,
        reconnect = reconnect,
        onSuppressAutoConnect = { autoConnectAttempted = true },
        stopMeasurement = ::stopMeasurement,
        openInput = { uri -> application.contentResolver.openInputStream(uri) },
        saveSettings = settingsStore::save,
    )

    // Comm-rate periodic job
    private var commTickerJob: Job? = null

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
        if (!enabled) reconnect.cancel()
    }

    fun setOpenSharingOnComplete(enabled: Boolean) {
        if (ui.value.openSharingOnComplete == enabled) return
        settingsStore.saveOpenSharingOnComplete(enabled)
        _ui.update { it.copy(openSharingOnComplete = enabled) }
    }

    fun dismissShareRequest() { _ui.update { it.copy(shareRequest = null) } }

    /**
     * Play-button entry point（詳細は [PlaybackController.start]）。CSV を検証して
     * 再生（mock）モードへ入り、mock デバイスへのスキャン→接続をエミュレートする。
     */
    fun startPlayback(uri: Uri?) = playback.start(uri)

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
        reconnect.noteConnectIntent(address)
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
                        !reconnect.userInitiatedDisconnect
                    if (willReconnect) {
                        // 再接続ループは同一の計測セッションの続きなので、バックグラウンドで
                        // 切断されてもプロセスが死なないようサービスは止めずに維持する。
                        reconnect.start()
                    } else {
                        // 再接続ループが動いていない通常の切断は完全に idle へ戻す。
                        // ループ中の一時的な切断ならループに任せ、再接続表示は消さない。
                        if (!reconnect.isRunning) _ui.update { it.copy(isReconnecting = false) }
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
        reconnect.cancel()
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
            reconnect.noteUserDisconnect()
            reconnect.cancel()
            // Disconnecting from playback returns to the initial live-BLE state.
            if (st.mockEnabled) playback.exit() else repo.disconnect()
        } else {
            // A manual connect overrides any auto-reconnect in progress.
            reconnect.cancel()
            val address = st.devices.getOrNull(st.selectedDeviceIndex) ?: return
            reconnect.noteConnectIntent(address)
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
            sendEncoded(MemeCommands.clearParams())
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
            counter.reset()
            prevTimeMs = 0

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
            sendEncoded(MemeCommands.startStop(true))
            _ui.update { it.copy(isMeasuring = true, isStarting = false, recordingRows = 0L) }
            startCommTicker()
        }
    }

    private fun stopMeasurement() {
        viewModelScope.launch {
            sendEncoded(MemeCommands.startStop(false))
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
        val markNum = (counter.totalCount - samplesBack).coerceAtLeast(0L)
        csv.writeLabel(markNum)
        _ui.update { it.copy(toast = "マーク NUM=$markNum") }
    }

    fun dismissToast() { _ui.update { it.copy(toast = null) } }

    /* ---- Protocol helpers ---- */

    private fun requestDeviceInfo() = sendEncoded(MemeCommands.getDeviceInfo())

    private fun requestMode() = sendEncoded(MemeCommands.getMode())

    private fun sendSetMode() = sendEncoded(MemeCommands.setMode(ui.value.settings))

    private fun sendSetParams() = sendEncoded(MemeCommands.set6AxisParams(ui.value.settings))

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
            if (!counter.countUp(packet.packetCount)) continue
            prevTimeMs = recvTimeMs + index * intervalMs

            // 全サンプル（間引き前）をプラグインへ渡す。検出器はプロットより
            // 高い分解能で回し、確定結果だけを GraphEvent.Custom で発行させる。
            for (p in plugins) p.onSample(packet.type, packet.values, counter.totalCount, prevTimeMs, csv)

            if ((counter.totalCount % graphSkipCount) == 0L) {
                val x = counter.totalCount / graphSkipCount
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
                val row = formatRow(ui.value.isMarking, counter.totalCount, prevTimeMs, packet.values)
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
        if (counter.totalCount > 0) {
            _ui.update { it.copy(successRate = counter.successRate) }
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

    private fun startCommTicker() {
        commTickerJob?.cancel()
        commTickerJob = viewModelScope.launch {
            delay(200)
            while (true) {
                val rate = counter.commRateTick(ui.value.settings.quality)
                _ui.update { it.copy(commRate = rate) }
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
