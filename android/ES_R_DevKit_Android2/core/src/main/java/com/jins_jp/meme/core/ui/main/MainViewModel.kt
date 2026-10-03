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
import com.jins_jp.meme.core.data.LocationSampler
import com.jins_jp.meme.core.data.MeasurementSettings
import com.jins_jp.meme.core.data.MemeMode
import com.jins_jp.meme.core.data.MemeQuality
import com.jins_jp.meme.core.data.SampleCounter
import com.jins_jp.meme.core.data.SettingsStore
import com.jins_jp.meme.core.data.mergeLabelsIntoCsv
import com.jins_jp.meme.core.data.formatRow
import com.jins_jp.meme.core.service.MeasurementService
import com.jins_jp.meme.core.web.DetectorOutputs
import com.jins_jp.meme.core.web.WebBridge
import com.jins_jp.meme.core.web.WebContentStore
import android.provider.OpenableColumns
import org.json.JSONArray
import org.json.JSONObject
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

private const val TAG = "MainViewModel"

// スキャン窓内で 1 台も見つからなかった時に一度だけ張り直す前の小休止。
// コントローラのスキャン窓をリセットさせるための短い間隔。
private const val SCAN_RETRY_GAP_MS = 500L

// 端末(ES_R)の電池残量を logcat へ出す最長間隔。残量が変わらなくてもこの間隔で
// 1 行出し、長時間計測中の減り方を追えるようにする。
private const val BATTERY_LOG_INTERVAL_MS = 60_000L

// 計測を止めてから判定器の表を閉じるまで待つ時間。ページは stop を受けてから溜めた行(最大 1 秒ぶん)を送るので、
// それが届くのを待つ(DetectorOutputs。webview/BRIDGE.md の Detector notifications and tables)。
private const val OUTPUTS_STOP_GRACE_MS = 1000L

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
    // CSV 再生中か（再生はグラフ画面のページが受け持つ。devkit-webview.md §5）。再生は BLE が切断されている
    // ときだけ始められ、再生中は接続しない（自動接続もしない）ので、再生中に計測が走ることはない。
    val isReplaying: Boolean = false,
    // 再生中のファイル名（状態の表示用）
    val replayName: String? = null,
    val bluetoothError: Boolean = false,
    val autoConnect: Boolean = false,
    val reconnectEnabled: Boolean = false,
    val isReconnecting: Boolean = false,
    // 計測完了時に「その他のアプリと共有」を自動で開くか。
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

    private val csv = CsvWriter(application)

    // 判定器の通知と演算結果の表(ページ → アプリの notify / table / records)。ライブ計測の間だけ受ける
    private val outputs = DetectorOutputs(application)

    // Sample / timing book-keeping
    private val counter = SampleCounter()
    private var prevTimeMs: Long = 0

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

    // 計測中の大まかな位置の記録（設定で ON のとき）。グラフ画面にも印を出す
    // （CSV への書き込みは次のデータ行なので、ずれは 1 サンプル以内）。
    private val locationSampler = LocationSampler(application)
    private val location = LocationRecorder(
        scope = viewModelScope,
        ui = _ui,
        sample = locationSampler::sample,
        onRecorded = { text -> web.mark(currentLabelKey(), text) },
    )

    // Disconnect の長押しで端末を Shelf mode へ移す。
    private val shelf = ShelfModeController(
        scope = viewModelScope,
        ui = _ui,
        connection = repo.connection,
        reconnect = reconnect,
        send = ::sendEncoded,
    )

    init {
        viewModelScope.launch { collectScanning() }
        viewModelScope.launch { collectDevices() }
        viewModelScope.launch { collectConnection() }
        viewModelScope.launch { collectIncoming() }
        viewModelScope.launch { collectDescriptorWritten() }
        web.onArtifact = ::receiveArtifact
        web.onOutput = outputs::receive
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

    /** 最後に停止した計測で作った判定器の表の CSV（自己テスト用。[lastSaved] より先に入る） */
    internal var lastSavedTables: List<Uri> = emptyList()
        private set

    /** 判定器の通知・表の受け口（自己テスト用） */
    internal val detectorOutputs: DetectorOutputs get() = outputs

    /**
     * 自己テストの間は、設定に関わらず計測後の共有シートを開かない（シートがアプリを覆って裏に回すと、
     * グラフ画面への送りが止まり、続くテストの手順が進まなくなるため）。設定値そのものは変えない。
     */
    internal var suppressShareForAutotest = false

    /** 設定の Display Engine「Use Built-in」 */
    fun useBuiltInGraph() {
        webStore.useBundled()
        web.load()
        refreshGraphContent()
    }

    /** アプリが前に出た / 裏に回った（裏の間はページへの push をやめ、戻ったら途切れを知らせる） */
    fun setForeground(foreground: Boolean) {
        outputs.foreground = foreground
        web.setForeground(foreground)
    }

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
        if (ui.value.isMeasuring) location.start() else location.stop()
        // 計測中なら FGS を上げ直して foregroundServiceType を評価し直す。
        // location 型が付いていないと画面 OFF 中の測位が落ちるため
        // （[MeasurementService.foregroundServiceType]）。冪等なので通知は増えない。
        if (ui.value.isMeasuring) {
            MeasurementService.start(getApplication())
        }
    }

    fun dismissShareRequest() { _ui.update { it.copy(shareRequest = null) } }

    /**
     * Play-button entry point（詳細は [PlaybackController.start]）。選んだ CSV をグラフ画面のページに再生させる
     * （読み込み・再生・巻き戻し・再生速度はページ側）。終わりは Disconnect。
     */
    fun startPlayback(uri: Uri?) = playback.start(uri)

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
        // 再生中は繋がない（繋ぐと計測を始められてしまい、ページの再生と取り合う）
        if (st.isReplaying) return
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
                    location.stop()
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
                    if (wasMeasuring) {
                        web.stop()
                        // 判定器の表はページが stop の後に送る残りを待ってから閉じる(閉じる前に次の計測が始まっていたら何もしない)
                        val s = outputs.session
                        viewModelScope.launch { delay(OUTPUTS_STOP_GRACE_MS); outputs.stop(s) }
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
                        )
                    }
                    // 計測中の予期しない切断のみ、従来どおり自動再接続する(reconnect 設定 ON 時)。
                    val willReconnect = wasMeasuring &&
                        _ui.value.reconnectEnabled &&
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
        if (st.isReplaying) { playback.exit(); return }
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

    /** Disconnect の長押しで Shelf mode の確認ダイアログを開く（詳細は [ShelfModeController]） */
    fun requestShelfMode() = shelf.request()

    fun dismissShelfDialog() = shelf.dismiss()

    /** 確認ダイアログの Yes。端末を Shelf mode へ移す（受理されると端末が自ら切断する） */
    fun confirmShelfMode() = shelf.confirm()

    fun canEnterShelfMode(): Boolean = shelf.canEnter()

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
            counter.reset()
            prevTimeMs = 0
            tapLabels.clear()
            location.reset()
            // 新しいセッションの開始残量を必ず 1 行残す（再接続直後で残量が
            // 変わっていなくても間引かれないように）。
            lastLoggedBattery = Int.MIN_VALUE

            val addr = repo.currentAddress()
            if (addr == null || ui.value.isReplaying) {
                _ui.update { it.copy(isStarting = false) }
                return@launch
            }
            csv.start(addr, ui.value.settings, ui.value.gzipCompression)
            outputs.start(csv.baseName ?: "", csv.compressed, ui.value.settings.quality.hz) { web.pageName }
            // 計測中はフォアグラウンドサービスでプロセス／CPU を保護し、
            // バックグラウンド・スリープ中も BLE 受信が途切れないようにする。
            MeasurementService.start(getApplication())

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
            location.start()
        }
    }

    /** グラフ画面へ渡す計測条件(BRIDGE.md の start)。値は端末へ送った設定 */
    private fun startCondition(address: String): JSONObject {
        val s = ui.value.settings
        return JSONObject()
            .put("label", address)
            .put("mode", s.mode.pageName)
            .put("cps", s.quality.hz)
            .put("accRange", s.accRange.g)
            .put("gyroRange", s.gyroRange.dps)
            .put("columns", JSONArray(if (s.mode.hasGraph) s.mode.columns else emptyList()))
            .put("startedAt", System.currentTimeMillis())
            .put("timeZone", "local")
            .put("accOffset", JSONArray(listOf(0, 0, 0)))
            .put("features", JSONArray(DetectorOutputs.FEATURES))
    }

    private fun stopMeasurement() {
        viewModelScope.launch {
            sendEncoded(MemeCommands.startStop(false))
            web.stop()
            val stoppedAt = SystemClock.elapsedRealtime()
            val outputsSession = outputs.session
            val stopResult = csv.stop()
            stopCommTicker()
            location.stop()
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
            // 判定器の表: ページが stop の後に送る残りを待ってから閉じる(統合で既に待ったぶんは差し引く)
            delay((OUTPUTS_STOP_GRACE_MS - (SystemClock.elapsedRealtime() - stoppedAt)).coerceAtLeast(0))
            val tableUris = outputs.stop(outputsSession)
            lastSavedTables = tableUris
            lastSaved = stopResult.dataUri to (lastSaved.second + 1)
            // 共有シートは統合が終わってから開く。統合は元ファイルを丸ごと置き換える
            // ので、待たずに渡すと受け手が統合前・置き換え途中のファイルを掴む。判定器の表の CSV も一緒に渡す
            val shareUris = listOfNotNull(stopResult.dataUri) + if (stopResult.dataUri != null) tableUris else emptyList()
            if (shareUris.isNotEmpty() && ui.value.openSharingOnComplete && !suppressShareForAutotest) {
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
            st.isReplaying -> tapLabels += LabelMerger.Entry(i + 1, clean)
            st.isMeasuring -> tapLabels += LabelMerger.Entry(i, clean)
        }
    }

    /** ラベル 1 件（Free Marking の "X" など）を停止時の CSV 統合用に控える。 */
    private fun addLabel(num: Long, text: String) {
        tapLabels += LabelMerger.Entry(num, text)
    }

    /**
     * 蓄積したタップラベルを [target] のデータCSVへ統合する（ARTIFACT 列を置換。[mergeLabelsIntoCsv]）。
     * 呼び出し時点でラベルを引き取り、二重統合（Stop 後の切断イベント等）を防ぐ。完了まで返らない。
     */
    private suspend fun mergeTapLabels(target: Uri?, byRowIndex: Boolean) {
        if (tapLabels.isEmpty()) return
        val labels = tapLabels.toList()
        tapLabels.clear()
        if (target == null) return
        mergeLabelsIntoCsv(getApplication(), target, labels, byRowIndex)
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
        // 「処理中サンプルの時刻」で、CSV 行の DATE になる。
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

            // 位置情報は測位できた直後の 1 行へその場で書く（後からの統合は
            // 全行の書き直しになるため）。タップラベル/Free Marking は受信時には
            // 分からないので、従来どおり停止時に LabelMerger が同じ列へ統合する。
            val row = formatRow(location.takePendingArtifact(), counter.totalCount, prevTimeMs, packet.values)
            csv.writeRow(row)
            outputs.recordSample(counter.totalCount, prevTimeMs)     // 判定器の表の DATE 列(同じ NUM の行の DATE)
        }

        val battery = packets.last().batteryLevel.toInt()
        logBatteryLevel(battery)
        _ui.update {
            it.copy(
                recordingRows = csv.recordedRows,
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
        shelf.onResp(data.getOrNull(2) == 0x00.toByte())
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
