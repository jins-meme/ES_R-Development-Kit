package com.jins_jp.meme.core.ui.main

import android.app.Application
import android.net.Uri
import android.os.SystemClock
import android.util.Log
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.CreationExtras
import com.jins.meme.academic.util.DataEncryption
import com.jins.meme.academic.util.HexDump
import com.jins.meme.academic.util.LogCat
import com.jins_jp.meme.core.App
import com.jins_jp.meme.core.ble.ConnectionState
import com.jins_jp.meme.core.ble.GATT_STATUS_NONE
import com.jins_jp.meme.core.ble.MemeBleConstants
import com.jins_jp.meme.core.ble.MemeBleRepository
import com.jins_jp.meme.core.ble.MemeCommands
import com.jins_jp.meme.core.ble.gattDisconnectReason
import com.jins_jp.meme.core.data.CsvWriter
import com.jins_jp.meme.core.data.DataParser
import com.jins_jp.meme.core.data.LabelMerger
import com.jins_jp.meme.core.data.decompressIfGzip
import com.jins_jp.meme.core.data.LocationSampler
import com.jins_jp.meme.core.data.MeasurementSettings
import com.jins_jp.meme.core.data.MemeMode
import com.jins_jp.meme.core.data.MemeQuality
import com.jins_jp.meme.core.data.SampleCounter
import com.jins_jp.meme.core.data.SettingsStore
import com.jins_jp.meme.core.data.formatRow
import com.jins_jp.meme.core.plugin.AlgoPlugin
import com.jins_jp.meme.core.service.MeasurementService
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.coroutines.withTimeoutOrNull
import java.io.File
import java.io.FileInputStream
import java.io.FileOutputStream
import java.io.OutputStreamWriter
import java.util.zip.GZIPInputStream
import java.util.zip.GZIPOutputStream
import kotlin.math.roundToLong

private const val TAG = "MainViewModel"

// スキャン窓内で 1 台も見つからなかった時に一度だけ張り直す前の小休止。
// コントローラのスキャン窓をリセットさせるための短い間隔。
private const val SCAN_RETRY_GAP_MS = 500L

// 端末(ES_R)の電池残量を logcat へ出す最長間隔。残量が変わらなくてもこの間隔で
// 1 行出し、長時間計測中の減り方を追えるようにする。
private const val BATTERY_LOG_INTERVAL_MS = 60_000L

// 計測中に大まかな現在地を ARTIFACT 列へ残す間隔（計測開始時が 1 回目）。
private const val LOCATION_INTERVAL_MS = 60_000L

// Shelf 移行の 1 段目（CONFIG モードへの遷移）の ACK を待つ時間。通常は
// 100ms 台で返る。ここで諦めても SHELF コマンドは送らないので端末は無傷。
private const val SHELF_CONFIG_ACK_TIMEOUT_MS = 3_000L

// SHELF コマンド送信後、端末が自ら切断するのを待つ時間（切断＝移行成功）。
private const val SHELF_DISCONNECT_TIMEOUT_MS = 5_000L

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
    val recordingRows: Long = 0L,
    val batteryLevel: Int = -1,
    val successRate: Double = 1.0,
    val commRate: Double = 1.0,
    val toast: String? = null,
    val mockEnabled: Boolean = false,
    val isPlaybackPaused: Boolean = false,
    // CSV 再生の現在位置/総時間（秒）。再生モード中のチャート X 軸と位置表示に使う。
    val replayPositionSec: Double = 0.0,
    val replayDurationSec: Double = 0.0,
    val mockError: String? = null,
    val bluetoothError: Boolean = false,
    val autoConnect: Boolean = false,
    val reconnectEnabled: Boolean = false,
    val isReconnecting: Boolean = false,
    // 計測完了時に「その他のアプリと共有」を自動で開くか（モックでない実機計測のみ対象）。
    val openSharingOnComplete: Boolean = false,
    val shareRequest: ShareRequest? = null,
    // チャートタップで開くラベル入力ダイアログ。null なら非表示。
    val labelDialog: LabelPrompt? = null,
    // 全チャートへオレンジの縦線＋文字で重ね描きする ARTIFACT イベント。
    // 再生ではソースCSVの ARTIFACT 列由来＋このセッションで確定したラベル、
    // 実機計測ではこのセッションで確定したラベル。
    val artifactEvents: List<ArtifactEvent> = emptyList(),
    // 計測中 1 分に 1 回、大まかな現在地を ARTIFACT 列へ残すか（既定 ON）。
    val locationLogging: Boolean = true,
    // 本体データCSVを gz 圧縮して保存するか（既定 ON）。形式は計測開始時に確定する。
    val gzipCompression: Boolean = true,
    // Disconnect の長押しで開く Shelf mode の確認ダイアログ。
    val showShelfDialog: Boolean = false,
    // Shelf 移行コマンドの送信中（完了は端末側からの切断）。
    val isEnteringShelf: Boolean = false,
)

/** チャートに縦線で示す ARTIFACT イベント。[sec] はデータ先頭からの経過秒。 */
data class ArtifactEvent(val sec: Double, val text: String)

/** 計測完了後に共有シートへ渡す CSV（本体データ＋サイドカーのうち存在するもの）の URI。 */
data class ShareRequest(val uris: List<Uri>)

/**
 * チャートタップで開くラベル入力ダイアログの状態。[num] は実機計測ではサンプル
 * 通し番号(NUM)、CSV 再生ではソースCSVのデータ行番号(1 始まり ≒ NUM 列の値)。
 */
data class LabelPrompt(val num: Long)

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
            locationLogging = settingsStore.loadLocationLogging(),
            gzipCompression = settingsStore.loadGzipCompression(),
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

    // 端末(ES_R)電池残量ログの間引き状態（[logBatteryLevel]）。
    private var lastLoggedBattery = Int.MIN_VALUE
    private var lastBatteryLogAt = 0L

    // 自動接続：1スキャンにつき一度だけ発火（再接続ループを防ぐ）
    private var autoConnectAttempted = false

    // チャートタップで確定したラベル。計測/再生停止時にデータCSVの ARTIFACT 列へ
    // 統合する（実機計測は NUM、再生はソース行番号で対応付け）。
    private val tapLabels = mutableListOf<LabelMerger.Entry>()

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

    // 計測中の位置取得ループ（[LOCATION_INTERVAL_MS] ごと）。
    private val locationSampler = LocationSampler(application)
    private var locationTickerJob: Job? = null

    /**
     * 直前に送ったコマンドの AUP_REPORT_RESP(ACK/NACK)を 1 件だけ受け取るための待ち合わせ。
     * Shelf 移行が「CONFIG への遷移が成功してから SHELF を送る」順序を要求するため、
     * 送信の直前にセットして [handleResp] から完了させる。
     */
    private var pendingRespAck: CompletableDeferred<Boolean>? = null

    init {
        val emitter: (Any) -> Unit = { payload -> _graph.tryEmit(GraphEvent.Custom(payload)) }
        for (p in plugins) p.onAttached(emitter)
        viewModelScope.launch { collectScanning() }
        viewModelScope.launch { collectDevices() }
        viewModelScope.launch { collectConnection() }
        viewModelScope.launch { collectIncoming() }
        viewModelScope.launch { collectDescriptorWritten() }
        viewModelScope.launch { collectPlaybackPosition() }
    }

    private suspend fun collectPlaybackPosition() {
        repo.playbackPosition.collect { p ->
            _ui.update { it.copy(replayPositionSec = p.positionSec, replayDurationSec = p.durationSec) }
        }
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

    /**
     * 保存時の gz 圧縮のオンオフ。次に開くファイルから効く（計測中のファイルは
     * [CsvWriter.start] で形式が確定済みで、途中で混ざることはない）。
     */
    fun setGzipCompression(enabled: Boolean) {
        if (ui.value.gzipCompression == enabled) return
        settingsStore.saveGzipCompression(enabled)
        _ui.update { it.copy(gzipCompression = enabled) }
    }

    fun setLocationLogging(enabled: Boolean) {
        if (ui.value.locationLogging == enabled) return
        settingsStore.saveLocationLogging(enabled)
        _ui.update { it.copy(locationLogging = enabled) }
        // 計測中の切り替えは次の周期を待たずに効かせる。
        if (ui.value.isMeasuring) startLocationTicker() else stopLocationTicker()
    }

    fun dismissShareRequest() { _ui.update { it.copy(shareRequest = null) } }

    /**
     * Play-button entry point（詳細は [PlaybackController.start]）。CSV を検証して
     * 再生（mock）モードへ入り、mock デバイスへのスキャン→接続をエミュレートする。
     */
    fun startPlayback(uri: Uri?) = playback.start(uri)

    /** Pause/Resume ボタン（詳細は [PlaybackController.pause]/[PlaybackController.resume]）。 */
    fun pausePlayback() = playback.pause()
    fun resumePlayback() = playback.resume()

    /** << / >> ボタン。[deltaSeconds] が負なら巻き戻し、正なら早送り。 */
    fun seekPlayback(deltaSeconds: Double) = playback.seek(deltaSeconds)

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
                    stopLocationTicker()
                    for (p in plugins) p.onDisconnected(csv)
                    // なぜ計測が止まったかを後から切り分けられるよう、切断の時刻と
                    // GATT ステータスを "<base>_disconnect.csv" へ残す。csv.stop() が
                    // サイドカーのベース名も畳むので、必ずその前に書く。
                    val status = repo.lastDisconnectStatus
                    val reason = gattDisconnectReason(status)
                    csv.writeDisconnect(System.currentTimeMillis(), status, reason)
                    // 起動直後は connection(StateFlow) の初期値 Disconnected がそのまま
                    // 流れてくる。接続した形跡がない（status 未設定かつ非計測）なら
                    // 実際の切断ではないのでログに残さない。
                    if (status != GATT_STATUS_NONE || wasMeasuring) {
                        Log.i(
                            TAG,
                            "disconnected: status=$status ($reason) measuring=$wasMeasuring " +
                                "battery=${_ui.value.batteryLevel}/5 rows=${csv.recordedRows}",
                        )
                    }
                    val dropResult = csv.stop()
                    // 予期しない切断でもタップラベルを失わないよう本体CSVへ統合する。
                    // 再生停止(Stop Replay/Disconnect)は stopMeasurement 側で統合済み。
                    mergeTapLabels(target = dropResult.dataUri, byRowIndex = false)
                    // 切断イベント検知時は、原因(Disconnect ボタン/予期しない切断)によらず接続に
                    // 紐づく状態をすべて初期化し、スキャン前相当の idle へ確実に戻す。
                    // デバイス一覧・設定・自動接続などの永続項目は保持する。
                    _ui.update {
                        it.copy(
                            firmwareVersion = null,
                            isMeasuring = false,
                            isStarting = false,
                            isInitializing = false,
                            recordingRows = 0L,
                            batteryLevel = -1,
                            successRate = 1.0,
                            commRate = 1.0,
                            labelDialog = null,
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

    /**
     * Disconnect の長押しで Shelf mode の確認ダイアログを開く。移行できる状態
     * （実機に接続済み・非計測）でなければ何も起きない。
     */
    fun requestShelfMode() {
        if (!canEnterShelfMode()) return
        _ui.update { it.copy(showShelfDialog = true) }
    }

    fun dismissShelfDialog() { _ui.update { it.copy(showShelfDialog = false) } }

    /**
     * 端末を Shelf mode（保管モード）へ移行させる。Web Bluetooth 版 SDK と同じ順序で
     * (1) CONFIG モードへの遷移を送り (2) その ACK を待ってから (3) SHELF を送る。
     * 受理されると端末は自分から切断するので、切断が来たら成功と見なす。復帰は
     * 充電のみで、アプリからは戻せない。
     */
    fun confirmShelfMode() {
        _ui.update { it.copy(showShelfDialog = false) }
        if (!canEnterShelfMode()) return
        viewModelScope.launch {
            _ui.update { it.copy(isEnteringShelf = true) }
            // 移行後の切断はこちらの意図した切断。自動再接続に拾わせない。
            reconnect.noteUserDisconnect()
            reconnect.cancel()

            val ack = CompletableDeferred<Boolean>()
            // 送信より先に置く（ACK は同じ viewModelScope から完了させるが、
            // 取りこぼしを構造的に無くしておく）。
            pendingRespAck = ack
            sendEncoded(MemeCommands.setConfigMode())
            val configured = withTimeoutOrNull(SHELF_CONFIG_ACK_TIMEOUT_MS) { ack.await() } == true
            pendingRespAck = null
            if (!configured) {
                // SHELF はまだ送っていないので端末は通常モードのまま。
                _ui.update {
                    it.copy(isEnteringShelf = false, toast = "Failed to enter shelf mode")
                }
                return@launch
            }

            sendEncoded(MemeCommands.shelf())
            val disconnected = withTimeoutOrNull(SHELF_DISCONNECT_TIMEOUT_MS) {
                repo.connection.first { it == ConnectionState.Disconnected }
            } != null
            _ui.update {
                it.copy(
                    isEnteringShelf = false,
                    toast = if (disconnected) "Entered shelf mode"
                    else "Failed to enter shelf mode",
                )
            }
        }
    }

    /**
     * Shelf mode へ移行できる状態か。SHELF コマンドは実機が接続済みで計測していない
     * ときだけ受理されるので、CSV 再生（mock）と計測中は対象外。
     */
    fun canEnterShelfMode(): Boolean {
        val st = ui.value
        return st.connection == ConnectionState.ServicesReady &&
            !st.isMeasuring && !st.mockEnabled && !st.isEnteringShelf
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
            tapLabels.clear()
            // 新しいセッションの開始残量を必ず 1 行残す（再接続直後で残量が
            // 変わっていなくても間引かれないように）。
            lastLoggedBattery = Int.MIN_VALUE

            val addr = repo.currentAddress()
            if (addr == null) {
                _ui.update { it.copy(isStarting = false) }
                return@launch
            }
            if (!ui.value.mockEnabled) {
                // 実機計測は新しいセッション＝前回のイベント表示をクリアする。
                // 再生はソースCSV由来のイベントを Start/Stop をまたいで表示し続ける。
                _ui.update { it.copy(artifactEvents = emptyList()) }
                csv.start(addr, ui.value.settings, ui.value.gzipCompression)
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
            _ui.update {
                it.copy(
                    isMeasuring = true,
                    isStarting = false,
                    recordingRows = 0L,
                    isPlaybackPaused = false,
                )
            }
            startCommTicker()
            startLocationTicker()
        }
    }

    private fun stopMeasurement() {
        // Disconnect 経由（playback.exit → stopMeasurement）では直後に mockEnabled が
        // false へ戻るため、再生停止かどうかはコルーチン開始前にここで確定させる。
        val wasMock = ui.value.mockEnabled
        val replaySource = playback.sourceUri
        viewModelScope.launch {
            sendEncoded(MemeCommands.startStop(false))
            // 未確定の検出結果（1 秒未満の区間など）をプラグインが書き切ってから閉じる。
            for (p in plugins) p.onMeasurementStop(csv)
            val stopResult = csv.stop()
            // Stop Measurement / Stop Replay: タップラベルをデータCSVへ統合する。
            // 実機計測はこのセッションで書いた本体CSV、再生は再生元のCSVが対象。
            mergeTapLabels(
                target = if (wasMock) replaySource else stopResult.dataUri,
                byRowIndex = wasMock,
            )
            stopCommTicker()
            stopLocationTicker()
            MeasurementService.stop(getApplication())
            val shareUris = listOfNotNull(stopResult.dataUri, stopResult.classificationUri)
            _ui.update {
                it.copy(
                    isMeasuring = false,
                    recordingRows = 0L,
                    isPlaybackPaused = false,
                    labelDialog = null,
                    shareRequest = if (
                        it.openSharingOnComplete && !it.mockEnabled && shareUris.isNotEmpty()
                    ) ShareRequest(shareUris) else it.shareRequest,
                )
            }
        }
    }

    /**
     * Free Marking ボタン: タップ 1 回につき現在位置（チャート右端）へ "X" を 1 つ
     * 記録する（旧実装は押下中 isMarking の 150ms 窓に入った全行へ X が入っていた）。
     * 記録・表示・CSV への統合はタップラベルと同じ経路（[addLabel]）。
     */
    fun marking() {
        if (!ui.value.isMeasuring) return
        addLabel(currentLabelKey(), "X")
    }

    /**
     * いまラベルを載せるサンプル位置（[LabelMerger.Entry.key] の座標系）。実機計測は
     * 受信サンプル通し番号(NUM)、再生はシークで NUM とソース位置がずれるため
     * ソースCSVのデータ行番号(1 始まり)。
     */
    private fun currentLabelKey(): Long {
        val base = if (ui.value.mockEnabled) {
            (ui.value.replayPositionSec * replaySampleRateHz()).roundToLong()
        } else {
            counter.totalCount
        }
        return base.coerceAtLeast(0L)
    }

    /**
     * グラフ上のタップ位置（0..1 の横位置と可視プロット点数）をサンプル位置へ換算し、
     * ラベル入力ダイアログを開く。実機計測は NUM（受信サンプル通し番号）、再生は
     * シークで NUM とソース位置がずれるためソースCSVのデータ行番号(1 始まり)を
     * 基準にする（replayPositionSec×レート = 消費済み行数 = 最後に再生した行の
     * 1 始まり行番号。取りこぼしの無いCSVでは NUM 列の値と一致する）。
     */
    fun markTap(fraction: Float, visiblePoints: Int) {
        if (!ui.value.isMeasuring) return
        val f = fraction.coerceIn(0f, 1f)
        val samplesBack = ((1f - f) * (visiblePoints - 1) * graphSkipCount).toLong()
        val markNum = (currentLabelKey() - samplesBack).coerceAtLeast(0L)
        _ui.update { it.copy(labelDialog = LabelPrompt(markNum)) }
    }

    /**
     * ラベル入力ダイアログの OK。入力が空なら "X" を記録する。CSV 構造を壊さない
     * よう区切り文字・改行は空白に置き換える。
     */
    fun confirmLabel(input: String) {
        val prompt = ui.value.labelDialog ?: return
        val text = input.replace(Regex("[,\r\n]"), " ").trim().ifEmpty { "X" }
        addLabel(prompt.num, text)
        _ui.update { it.copy(labelDialog = null) }
    }

    /**
     * ラベル 1 件（タップラベル/Free Marking の "X"）を記録する。停止時の CSV 統合用
     * に蓄積しつつ、その瞬間からチャートに縦線イベントとして表示する。秒換算は
     * チャートの X 軸と同じ基準（実機=NUM/受信Hz、再生=行番号/行消費レート）。
     */
    private fun addLabel(num: Long, text: String) {
        tapLabels += LabelMerger.Entry(num, text)
        val hz = if (ui.value.mockEnabled) replaySampleRateHz() else ui.value.settings.quality.hz
        val event = ArtifactEvent(num.toDouble() / hz, text)
        _ui.update { it.copy(artifactEvents = it.artifactEvents + event) }
    }

    fun dismissLabelDialog() { _ui.update { it.copy(labelDialog = null) } }

    /** 再生時の CSV 行消費レート(行/秒)。MockMemeBleEngine.sampleRateHz と同じ規則。 */
    private fun replaySampleRateHz(): Int {
        val s = ui.value.settings
        return if (s.quality == MemeQuality.Hz100 && s.mode != MemeMode.Quaternion) 100 else 50
    }

    /**
     * 蓄積したタップラベルを [target] のデータCSVへ統合する（ARTIFACT 列を置換）。
     * 呼び出し時点でラベルを引き取り、二重統合（Stop 後の切断イベント等）を防ぐ。
     *
     * 追記ではラベル行だけ差し替えられないので全体を書き直すが、**CSV を
     * メモリに載せずに 1 行ずつ流す**。100Hz の実測は 1 時間で約 28MB の
     * テキストになり、`List<String>` へ読み込むと数時間の計測でヒープを
     * 使い切る（位置記録が 1 分ごとにラベルを積むため、この経路は毎セッション
     * 通る）。いったんキャッシュの一時ファイルへ書き切ってから本体へ流し込む
     * ので、途中で失敗しても元のファイルは壊れない。
     */
    private fun mergeTapLabels(target: Uri?, byRowIndex: Boolean) {
        if (tapLabels.isEmpty()) return
        val labels = tapLabels.toList()
        tapLabels.clear()
        if (target == null) return
        val app = getApplication<Application>()
        val resolver = app.contentResolver
        viewModelScope.launch(Dispatchers.IO) {
            val tmp = runCatching { File.createTempFile("label_merge", ".tmp", app.cacheDir) }
                .getOrNull() ?: return@launch
            try {
                runCatching {
                    val written = resolver.openInputStream(target)?.use { ins ->
                        val decoded = decompressIfGzip(ins)
                        // 読んだ形式のまま書き戻す（本体データCSVは設定により
                        // .csv.gz か .csv、再生元の過去ファイルは非圧縮のこともある）。
                        val compress = decoded is GZIPInputStream
                        FileOutputStream(tmp).use { fos ->
                            val sink = if (compress) GZIPOutputStream(fos) else fos
                            // BufferedWriter の close が連鎖して GZIPOutputStream の
                            // finish とトレーラ書き出しまで行う。
                            OutputStreamWriter(sink, Charsets.UTF_8).buffered().use { w ->
                                decoded.bufferedReader(Charsets.UTF_8).use { r ->
                                    LabelMerger.merge(r, w, labels, byRowIndex)
                                }
                            }
                        }
                        true
                    } ?: false
                    // 一時ファイルが完成した時だけ本体を置き換える。
                    if (written) {
                        resolver.openOutputStream(target, "wt")?.use { os ->
                            FileInputStream(tmp).use { it.copyTo(os) }
                        }
                    }
                }.onFailure { e ->
                    LogCat.d(TAG, "label merge failed: $e")
                }
            } finally {
                tmp.delete()
            }
        }
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
                // ARTIFACT 列は受信時には書かず、停止時に LabelMerger がタップラベル/
                // Free Marking をまとめて統合する。
                val row = formatRow(false, counter.totalCount, prevTimeMs, packet.values)
                csv.writeRow(row)
            }
        }

        val battery = packets.last().batteryLevel.toInt()
        logBatteryLevel(battery)
        _ui.update {
            it.copy(
                recordingRows = if (ui.value.mockEnabled) 0 else csv.recordedRows,
                batteryLevel = battery,
            )
        }

        // success rate from total/error
        if (counter.totalCount > 0) {
            _ui.update { it.copy(successRate = counter.successRate) }
        }
    }

    /**
     * 端末(ES_R)の電池残量を logcat へ出す。残量は 0〜5 の 6 段階（0 は充電中）で
     * 全データパケットに乗ってくるので、100Hz でそのまま出すと logcat が溢れる。
     * 値が変わった時と、変わらなくても [BATTERY_LOG_INTERVAL_MS] ごとに 1 行だけ出す。
     * 長時間計測が切断で止まった時に「切断直前に残量がどこまで落ちていたか」＝
     * メガネの電池切れかどうかを `adb logcat -s MainViewModel` で追うための計装。
     */
    private fun logBatteryLevel(level: Int) {
        val now = SystemClock.elapsedRealtime()
        if (level == lastLoggedBattery && now - lastBatteryLogAt < BATTERY_LOG_INTERVAL_MS) return
        lastLoggedBattery = level
        lastBatteryLogAt = now
        Log.i(TAG, "device battery=$level/5 (0=charging) rows=${csv.recordedRows}")
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
        // 直前のコマンドの完了を待っている処理（Shelf 移行の CONFIG 遷移）へ結果を渡す。
        pendingRespAck?.complete(data.getOrNull(2) == 0x00.toByte())
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

    /**
     * 計測中、[LOCATION_INTERVAL_MS] ごとに大まかな現在地の取得をトリガーする
     * （1 回目は計測開始時）。取得自体は子コルーチンへ投げるので、測位に時間が
     * かかっても次の周期はずれない。取れなければ何も記録しない。
     */
    private fun startLocationTicker() {
        stopLocationTicker()
        val st = ui.value
        // 再生（mock）は過去のログを流しているだけなので、いまの位置は記録しない。
        if (!st.locationLogging || st.mockEnabled) return
        locationTickerJob = viewModelScope.launch {
            while (isActive) {
                launch { recordLocationOnce() }
                delay(LOCATION_INTERVAL_MS)
            }
        }
    }

    private fun stopLocationTicker() {
        locationTickerJob?.cancel(); locationTickerJob = null
    }

    /** 現在地が取れたら "lc:35.6802_139.7521" をタップラベルと同じ経路で 1 件記録する。 */
    private suspend fun recordLocationOnce() {
        val text = runCatching { locationSampler.sample() }.getOrNull() ?: return
        // 測位が返るまでの間に計測が終わっていたら、載せる行が無いので捨てる。
        if (!ui.value.isMeasuring || ui.value.mockEnabled) return
        addLabel(currentLabelKey(), text)
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
