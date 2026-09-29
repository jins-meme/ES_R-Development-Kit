package com.jins_jp.meme.core.ui.main

import android.app.Application
import android.net.Uri
import android.os.SystemClock
import android.util.Log
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.CreationExtras
import com.jins.meme.academic.util.HexDump
import com.jins.meme.academic.util.LogCat
import com.jins_jp.meme.core.App
import com.jins_jp.meme.core.ble.ConnectionState
import com.jins_jp.meme.core.ble.GATT_STATUS_NONE
import com.jins_jp.meme.core.ble.MemeBleConstants
import com.jins_jp.meme.core.ble.MemeBleRepository
import com.jins_jp.meme.core.ble.MemeCipher
import com.jins_jp.meme.core.ble.MemeCommands
import com.jins_jp.meme.core.ble.gattDisconnectReason
import com.jins_jp.meme.core.data.CsvWriter
import com.jins_jp.meme.core.data.DataParser
import com.jins_jp.meme.core.data.LabelMerger
import com.jins_jp.meme.core.data.decompressIfGzip
import com.jins_jp.meme.core.data.LocationFix
import com.jins_jp.meme.core.data.LocationSampler
import com.jins_jp.meme.core.data.MeasurementSettings
import com.jins_jp.meme.core.data.MemeMode
import com.jins_jp.meme.core.data.MemeQuality
import com.jins_jp.meme.core.data.SampleCounter
import com.jins_jp.meme.core.data.SettingsStore
import com.jins_jp.meme.core.data.formatLocationArtifact
import com.jins_jp.meme.core.data.formatRow
import com.jins_jp.meme.core.data.movedAtLeast
import com.jins_jp.meme.core.plugin.AlgoPlugin
import com.jins_jp.meme.core.service.MeasurementService
import com.jins_jp.meme.core.web.WebBridge
import com.jins_jp.meme.core.web.WebContentStore
import android.provider.OpenableColumns
import org.json.JSONArray
import org.json.JSONObject
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
import kotlinx.coroutines.withContext
import kotlinx.coroutines.withTimeoutOrNull
import java.io.File
import java.io.FileInputStream
import java.io.FileOutputStream
import java.io.OutputStreamWriter
import java.util.zip.GZIPInputStream
import java.util.zip.GZIPOutputStream

private const val TAG = "MainViewModel"

// スキャン窓内で 1 台も見つからなかった時に一度だけ張り直す前の小休止。
// コントローラのスキャン窓をリセットさせるための短い間隔。
private const val SCAN_RETRY_GAP_MS = 500L

// 端末(ES_R)の電池残量を logcat へ出す最長間隔。残量が変わらなくてもこの間隔で
// 1 行出し、長時間計測中の減り方を追えるようにする。
private const val BATTERY_LOG_INTERVAL_MS = 60_000L

// 計測中に大まかな現在地の取得をトリガーする間隔（計測開始時が 1 回目）。
private const val LOCATION_INTERVAL_MS = 60_000L

// 前回記録した地点からこれだけ動いていたら記録する（緯度方向・経度方向のどちらか）。
// 止まっている間は同じ座標を毎分書かない＝ARTIFACT 列が位置情報で埋まらない。
private const val LOCATION_MOVE_THRESHOLD_M = 50.0

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
    // CSV 再生中か（再生はグラフ画面のページが受け持つ。devkit-webview.md §5）。名前は旧実装（モックの BLE で
    // 再生していた）のまま。
    val mockEnabled: Boolean = false,
    // 再生中のファイル名（状態の表示用）
    val replayName: String? = null,
    val mockError: String? = null,
    val bluetoothError: Boolean = false,
    val autoConnect: Boolean = false,
    val reconnectEnabled: Boolean = false,
    val isReconnecting: Boolean = false,
    // 計測完了時に「その他のアプリと共有」を自動で開くか（モックでない実機計測のみ対象）。
    val openSharingOnComplete: Boolean = false,
    val shareRequest: ShareRequest? = null,
    // 設定の Display Engine: 今のグラフ画面(zip)の名前と、取り込みに失敗したときの理由
    val graphContent: String = "",
    val graphIsCustom: Boolean = false,
    val graphMessage: String? = null,
    // 計測中、大まかな現在地を ARTIFACT 列へ残すか（既定 OFF）。
    val locationLogging: Boolean = false,
    // 本体データCSVを gz 圧縮して保存するか（既定 ON）。形式は計測開始時に確定する。
    val gzipCompression: Boolean = true,
    // Disconnect の長押しで開く Shelf mode の確認ダイアログ。
    val showShelfDialog: Boolean = false,
    // Shelf 移行コマンドの送信中（完了は端末側からの切断）。
    val isEnteringShelf: Boolean = false,
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
            locationLogging = settingsStore.loadLocationLogging(),
            gzipCompression = settingsStore.loadGzipCompression(),
        )
    )
    val ui: StateFlow<MainUiState> = _ui.asStateFlow()

    // グラフ画面(WebView)。中身は zip(同梱の標準版か、設定で選んだもの)。描画・再生の操作・アーティファクトの入力は
    // ページ側で、アプリは計測の値を push し、ページで付けたアーティファクトを CSV へ書き戻す(DevKit webview/BRIDGE.md)。
    private val webStore = WebContentStore(application)
    val web = WebBridge(application, webStore)

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

    // グラフ画面で付けたアーティファクトと Free Marking。計測/再生停止時にデータCSVの ARTIFACT 列へ
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
        ui = _ui,
        reconnect = reconnect,
        stopMeasurement = ::stopMeasurement,
        displayName = ::displayName,
        openReplay = { uri, name ->
            web.openReplay(uri, name, JSONObject().put("timeZone", "local").put("accOffset", JSONArray(listOf(0, 0, 0))))
        },
        closeReplay = { web.closeReplay() },
        mergeLabels = { uri -> mergeTapLabels(target = uri, byRowIndex = true) },
    )

    // Comm-rate periodic job
    private var commTickerJob: Job? = null

    // 計測中の位置取得ループ（[LOCATION_INTERVAL_MS] ごと）。
    private val locationSampler = LocationSampler(application)
    private var locationTickerJob: Job? = null

    // 最後に ARTIFACT 列へ書いた地点。次の測位がここから
    // [LOCATION_MOVE_THRESHOLD_M] 以上離れた時だけ記録する。
    private var lastLocationFix: LocationFix? = null

    // 次に書くデータ行の ARTIFACT 列へ載せる位置情報（1 行消費したら null に戻る）。
    // 測位は別コルーチンで走るが、書き込むのも消費するのも viewModelScope の
    // 既定ディスパッチャ(Main)上なので、この受け渡しに排他は要らない。
    private var pendingLocationArtifact: String? = null

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
        web.onArtifact = ::receiveArtifact
        web.onReplayInfo = { info ->
            _ui.update { it.copy(toast = "再生データを読み込みました（${info.optLong("rows")} 行）") }
        }
        refreshGraphContent()
    }

    // ---- グラフ画面(WebView)の中身 ----

    private fun refreshGraphContent(message: String? = null) {
        val name = webStore.manifest?.displayName ?: "(none)"
        val custom = webStore.source == WebContentStore.Source.Custom
        _ui.update {
            it.copy(graphContent = if (custom) "$name (zip)" else "$name (built-in)", graphIsCustom = custom, graphMessage = message)
        }
    }

    /** 設定の Display Engine「Choose zip…」。検査に通らなければ今の中身のまま、理由を出す */
    fun chooseGraphZip(uri: Uri?) {
        if (uri == null) return
        viewModelScope.launch {
            val r = withContext(Dispatchers.IO) { runCatching { webStore.importZip(uri) } }
            r.onSuccess { web.load(); refreshGraphContent() }
                .onFailure { e -> refreshGraphContent(e.message ?: e.toString()) }
        }
    }

    /** 取り込み(自己テスト用。ファイルから)。結果の manifest か、断られた理由を返す */
    internal suspend fun importGraphZipFile(file: java.io.File): Result<WebContentStore.Manifest> {
        val r = withContext(Dispatchers.IO) { runCatching { webStore.importZip(file) } }
        if (r.isSuccess) web.load()
        refreshGraphContent(r.exceptionOrNull()?.message)
        return r
    }

    internal val graphStore: WebContentStore get() = webStore

    /** 今つないでいる(つなごうとしている)端末のアドレス（自己テスト用） */
    internal fun currentAddress(): String? = repo.currentAddress()

    /** 最後に停止した計測の本体CSV と、停止(書き戻しまで)を終えた回数（自己テスト用） */
    internal var lastSaved: Pair<Uri?, Int> = null to 0
        private set

    /** 設定の Display Engine「Use Built-in」 */
    fun useBuiltInGraph() {
        webStore.useBundled()
        web.load()
        refreshGraphContent()
    }

    /** アプリが前に出た / 裏に回った（裏の間はページへの push をやめ、戻ったら途切れを知らせる） */
    fun setForeground(foreground: Boolean) = web.setForeground(foreground)

    private fun displayName(uri: Uri): String {
        val app = getApplication<Application>()
        return runCatching {
            app.contentResolver.query(uri, arrayOf(OpenableColumns.DISPLAY_NAME), null, null, null)?.use { c ->
                if (c.moveToFirst()) c.getString(0) else null
            }
        }.getOrNull() ?: uri.lastPathSegment?.substringAfterLast('/') ?: "replay.csv"
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
        // 実機計測中なら FGS を上げ直して foregroundServiceType を評価し直す。
        // location 型が付いていないと画面 OFF 中の測位が落ちるため
        // （[MeasurementService.foregroundServiceType]）。冪等なので通知は増えない。
        if (ui.value.isMeasuring && !ui.value.mockEnabled) {
            MeasurementService.start(getApplication())
        }
    }

    fun dismissShareRequest() { _ui.update { it.copy(shareRequest = null) } }

    /**
     * Play-button entry point（詳細は [PlaybackController.start]）。選んだ CSV をグラフ画面のページに再生させる
     * （読み込み・再生・巻き戻し・再生速度はページ側）。終わりは Disconnect。
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
                    if (wasMeasuring) web.stop()
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
        // 再生中の Disconnect は再生を終える(ページで付けたアーティファクトを再生元の CSV へ書き戻す)
        if (st.mockEnabled) { playback.exit(); return }
        if (st.connection != ConnectionState.Disconnected && st.connection != ConnectionState.Disconnecting) {
            // Mark the disconnect as deliberate so it does not trigger reconnect.
            reconnect.noteUserDisconnect()
            reconnect.cancel()
            repo.disconnect()
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
            resetLocationState()
            // 新しいセッションの開始残量を必ず 1 行残す（再接続直後で残量が
            // 変わっていなくても間引かれないように）。
            lastLoggedBattery = Int.MIN_VALUE

            val addr = repo.currentAddress()
            if (addr == null) {
                _ui.update { it.copy(isStarting = false) }
                return@launch
            }
            if (!ui.value.mockEnabled) {
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
            web.start(startCondition(addr))
            _ui.update {
                it.copy(
                    isMeasuring = true,
                    isStarting = false,
                    recordingRows = 0L,
                )
            }
            startCommTicker()
            startLocationTicker()
        }
    }

    /** グラフ画面へ渡す計測条件(BRIDGE.md の start)。値は端末へ送った設定 */
    private fun startCondition(address: String): JSONObject {
        val s = ui.value.settings
        val mode = when (s.mode) { MemeMode.Full -> "full"; MemeMode.Standard -> "standard"; else -> "quaternion" }
        return JSONObject()
            .put("label", address)
            .put("mode", mode)
            .put("cps", s.quality.hz)
            .put("accRange", s.accRange.g)
            .put("gyroRange", s.gyroRange.dps)
            .put("columns", WebBridge.columns(mode))
            .put("startedAt", System.currentTimeMillis())
            .put("timeZone", "local")
            .put("accOffset", JSONArray(listOf(0, 0, 0)))
    }

    private fun stopMeasurement() {
        viewModelScope.launch {
            sendEncoded(MemeCommands.startStop(false))
            web.stop()
            // 未確定の検出結果（1 秒未満の区間など）をプラグインが書き切ってから閉じる。
            for (p in plugins) p.onMeasurementStop(csv)
            val stopResult = csv.stop()
            stopCommTicker()
            stopLocationTicker()
            MeasurementService.stop(getApplication())
            // 停止の見た目（ボタン・行数）は統合を待たずに先に戻す。統合は数百MBの
            // CSV を書き直すことがあり、待たせると Stop が固まったように見える。
            _ui.update {
                it.copy(
                    isMeasuring = false,
                    recordingRows = 0L,
                )
            }
            // Stop Measurement: アーティファクトをこのセッションで書いた本体CSVへ統合する
            // (再生のぶんは再生を終えるとき [PlaybackController.exit] が再生元へ書き戻す)。
            mergeTapLabels(target = stopResult.dataUri, byRowIndex = false)
            lastSaved = stopResult.dataUri to (lastSaved.second + 1)
            // 共有シートは統合が終わってから開く。統合は元ファイルを丸ごと置き換える
            // ので、待たずに渡すと受け手が統合前・置き換え途中のファイルを掴む。
            val shareUris = listOfNotNull(stopResult.dataUri, stopResult.classificationUri)
            if (shareUris.isNotEmpty() && ui.value.openSharingOnComplete) {
                _ui.update { it.copy(shareRequest = ShareRequest(shareUris)) }
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
        val key = currentLabelKey()
        addLabel(key, "X")
        web.mark(key, "X")
    }

    /** いまラベルを載せるサンプル位置（[LabelMerger.Entry.key] の座標系 = 受信サンプル通し番号 NUM）。 */
    private fun currentLabelKey(): Long = counter.totalCount.coerceAtLeast(0L)

    /**
     * グラフ画面で付けたアーティファクト（ページ → アプリ）。空なら "X"（ページが入れてくる）。CSV の列を
     * 壊さないよう区切り文字・改行は空白に置き換え、長さを切り詰める。数式として読まれる書き出しは捨てる。ライブの i は受信サンプル通し番号(NUM)、
     * 再生の i は再生元 CSV のデータ行の番号（0 始まり）で、LabelMerger の行番号（1 始まり）へは +1 する。
     */
    private fun receiveArtifact(i: Long, text: String) {
        val st = ui.value
        val clean = text.replace(Regex("[,\\r\\n]"), " ").trim().take(64).ifEmpty { "X" }
        // 表計算ソフトで数式として読まれる書き出し(= + - @)は受けない(CSV 注入。ページも入力時に断る。webview/BRIDGE.md)
        if (clean.first() in "=+-@") { Log.w(TAG, "artifact refused (formula-like): $clean"); return }
        when {
            st.mockEnabled -> tapLabels += LabelMerger.Entry(i + 1, clean)
            st.isMeasuring -> tapLabels += LabelMerger.Entry(i, clean)
        }
    }

    /** ラベル 1 件（Free Marking の "X" など）を停止時の CSV 統合用に控える。 */
    private fun addLabel(num: Long, text: String) {
        tapLabels += LabelMerger.Entry(num, text)
    }

    /**
     * 蓄積したタップラベルを [target] のデータCSVへ統合する（ARTIFACT 列を置換）。
     * 呼び出し時点でラベルを引き取り、二重統合（Stop 後の切断イベント等）を防ぐ。
     *
     * 追記ではラベル行だけ差し替えられないので全体を書き直すが、**CSV を
     * メモリに載せずに 1 行ずつ流す**。100Hz の実測は 1 時間で約 28MB の
     * テキストになり、`List<String>` へ読み込むと数時間の計測でヒープを
     * 使い切る。いったんキャッシュの一時ファイルへ書き切ってから本体へ流し込む
     * ので、途中で失敗しても元のファイルは壊れない。
     *
     * **完了まで返らない**（suspend）。計測完了時の共有は書き戻し済みのファイルを
     * 渡す必要があり、投げっぱなしだと共有シートが統合前のCSVを掴む。
     */
    private suspend fun mergeTapLabels(target: Uri?, byRowIndex: Boolean) {
        if (tapLabels.isEmpty()) return
        val labels = tapLabels.toList()
        tapLabels.clear()
        if (target == null) return
        val app = getApplication<Application>()
        val resolver = app.contentResolver
        withContext(Dispatchers.IO) {
            val tmp = runCatching { File.createTempFile("label_merge", ".tmp", app.cacheDir) }
                .getOrNull() ?: return@withContext
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
        repo.send(MemeCipher.encode(data))
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

            // グラフ画面へ(値の並びは CSV の列の並び = start の columns)。Quaternion は描くものが無い
            if (packet.type == MemeBleConstants.AUP_REPORT_ACADEMIA1 || packet.type == MemeBleConstants.AUP_REPORT_ACADEMIA2) {
                web.push(counter.totalCount, packet.values)
            }

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
                // 位置情報は測位できた直後の 1 行へその場で書く（後からの統合は
                // 全行の書き直しになるため）。タップラベル/Free Marking は受信時には
                // 分からないので、従来どおり停止時に LabelMerger が同じ列へ統合する。
                val artifact = pendingLocationArtifact ?: ""
                if (artifact.isNotEmpty()) pendingLocationArtifact = null
                val row = formatRow(artifact, counter.totalCount, prevTimeMs, packet.values)
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
        resetLocationState()
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

    /**
     * 位置の記録状態を初期化する。セッションの開始と設定の切り替えで呼び、
     * 最初の 1 回は「前回地点なし」＝必ず記録される状態から始める。前のセッションで
     * 書けなかった位置が新しいCSVの先頭行へ紛れ込まないよう、持ち越しも捨てる。
     */
    private fun resetLocationState() {
        lastLocationFix = null
        pendingLocationArtifact = null
    }

    /**
     * 現在地を 1 回取り、前回記録した地点から [LOCATION_MOVE_THRESHOLD_M] 以上
     * 動いていれば "lc:35.6802_139.7521" を**次に書くデータ行**の ARTIFACT 列へ載せる
     * （[handleIncoming] が消費する）。停止時にまとめて統合していた頃と違い、CSV を
     * 読み直さないので長時間計測でも停止が重くならない。
     */
    private suspend fun recordLocationOnce() {
        val fix = runCatching { locationSampler.sample() }.getOrNull() ?: return
        // 測位が返るまでの間に計測が終わっていたら、載せる行が無いので捨てる。
        if (!ui.value.isMeasuring || ui.value.mockEnabled) return
        if (!movedAtLeast(lastLocationFix, fix, LOCATION_MOVE_THRESHOLD_M)) return
        val text = formatLocationArtifact(fix.latitude, fix.longitude) ?: return
        lastLocationFix = fix
        pendingLocationArtifact = text
        // グラフ画面にも印として出す（CSV への書き込みは次のデータ行なので、ずれは 1 サンプル以内）。
        web.mark(currentLabelKey(), text)
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
