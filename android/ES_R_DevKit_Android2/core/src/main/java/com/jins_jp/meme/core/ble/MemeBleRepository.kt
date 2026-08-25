package com.jins_jp.meme.core.ble

import android.Manifest
import android.annotation.SuppressLint
import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothGatt
import android.bluetooth.BluetoothGattCallback
import android.bluetooth.BluetoothGattCharacteristic
import android.bluetooth.BluetoothGattConnectionSettings
import android.bluetooth.BluetoothGattDescriptor
import android.bluetooth.BluetoothManager
import android.bluetooth.BluetoothProfile
import android.bluetooth.BluetoothStatusCodes
import android.bluetooth.le.BluetoothLeScanner
import android.bluetooth.le.ScanCallback
import android.bluetooth.le.ScanFilter
import android.bluetooth.le.ScanResult
import android.bluetooth.le.ScanSettings
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.content.pm.PackageManager
import android.os.Build
import android.os.ParcelUuid
import androidx.core.content.ContextCompat
import com.jins.meme.academic.util.DataEncryption
import com.jins.meme.academic.util.LogCat
import com.jins_jp.meme.core.data.MockCsvData
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update

enum class ConnectionState { Disconnected, Connecting, Connected, ServicesReady, Disconnecting }

/** [MemeBleRepository.lastDisconnectStatus] の初期値（この接続ではまだ切断していない）。 */
const val GATT_STATUS_NONE = -1

/**
 * 端末の Bluetooth 自体が OFF にされて接続が消えた時の合成ステータス。GATT からは
 * 何のコールバックも来ないので、本物の HCI エラーコードと衝突しない負値を使う。
 */
const val GATT_STATUS_ADAPTER_OFF = -2

/**
 * GATT の切断ステータスを切り分け用の名前へ直す。長時間計測が途中で止まった時に
 * 「メガネ側が落ちた（電源断・電池切れ）」のか「電波が届かなくなった」のかを
 * 切断サイドカーCSVから判別するために使う。値は Bluetooth Core Spec の
 * HCI エラーコードがそのまま上がってくる。
 */
fun gattDisconnectReason(status: Int): String = when (status) {
    GATT_STATUS_NONE -> "UNKNOWN"
    GATT_STATUS_ADAPTER_OFF -> "ADAPTER_OFF"    // 端末の Bluetooth を OFF にした
    0x00 -> "SUCCESS"                       // 正常終了（こちらから disconnect した等）
    0x08 -> "CONNECTION_TIMEOUT"            // リンク監視タイムアウト＝電波が届かなくなった
    0x13 -> "REMOTE_USER_TERMINATED"        // メガネ側から切断＝電源断・電池切れ
    0x16 -> "LOCAL_HOST_TERMINATED"         // Android 側から終了
    0x22 -> "LMP_RESPONSE_TIMEOUT"
    0x3E -> "CONNECTION_FAILED_TO_ESTABLISH"
    0x85 -> "GATT_ERROR"                    // 133: 汎用エラー
    else -> "STATUS_$status"
}

private const val TAG = "MemeBleRepository"

@SuppressLint("MissingPermission")
class MemeBleRepository(private val context: Context) : MemeBleClient {

    private val manager: BluetoothManager? =
        context.getSystemService(Context.BLUETOOTH_SERVICE) as? BluetoothManager
    private val adapter: BluetoothAdapter? = manager?.adapter

    private val _scanning = MutableStateFlow(false)
    override val scanning: StateFlow<Boolean> = _scanning.asStateFlow()

    private val _devices = MutableStateFlow<Set<String>>(emptySet())
    override val devices: StateFlow<Set<String>> = _devices.asStateFlow()

    private val _connection = MutableStateFlow(ConnectionState.Disconnected)
    override val connection: StateFlow<ConnectionState> = _connection.asStateFlow()

    private val _incoming = MutableSharedFlow<ByteArray>(extraBufferCapacity = 256)
    val incoming: SharedFlow<ByteArray> = _incoming.asSharedFlow()

    private val _descriptorWritten = MutableSharedFlow<Unit>(extraBufferCapacity = 4)
    override val descriptorWritten: SharedFlow<Unit> = _descriptorWritten.asSharedFlow()

    private val _playbackPosition = MutableStateFlow(PlaybackPosition())
    override val playbackPosition: StateFlow<PlaybackPosition> = _playbackPosition.asStateFlow()

    private var gatt: BluetoothGatt? = null
    private var scanner: BluetoothLeScanner? = null
    private var currentAddress: String? = null

    /**
     * 直近の切断で GATT が返したステータス（[gattDisconnectReason] で名前になる）。
     * 計測がなぜ止まったかを後から切り分けるための計装で、MainViewModel が
     * 切断サイドカーCSV へ記録する。[connection] が Disconnected を流す前に必ず
     * 更新するので、購読側は同じ切断の理由として読める。GATT コールバックは
     * API 世代によってバインダースレッドから来るため volatile。
     */
    @Volatile
    var lastDisconnectStatus: Int = GATT_STATUS_NONE
        private set

    /**
     * 端末の Bluetooth を OFF にすると GATT の接続は無言で消え、
     * onConnectionStateChange は呼ばれない。購読していないとアプリは
     * 「計測中・受信 0」のまま固まり、CSV も閉じられない。アダプタの状態変化を
     * 拾って、こちら側で切断として畳むためのレシーバ。
     */
    private val adapterStateReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context?, intent: Intent?) {
            if (intent?.action != BluetoothAdapter.ACTION_STATE_CHANGED) return
            val state = intent.getIntExtra(BluetoothAdapter.EXTRA_STATE, BluetoothAdapter.ERROR)
            // TURNING_OFF で先に畳む。続く OFF は同じ値の再代入になり何も起きない。
            if (state == BluetoothAdapter.STATE_TURNING_OFF || state == BluetoothAdapter.STATE_OFF) {
                handleAdapterOff()
            }
        }
    }

    init {
        // Application スコープのシングルトン（App.bleRepository）なのでプロセスが
        // 生きている間ずっと購読する。対応する解除処理は持たない。
        ContextCompat.registerReceiver(
            context,
            adapterStateReceiver,
            IntentFilter(BluetoothAdapter.ACTION_STATE_CHANGED),
            ContextCompat.RECEIVER_NOT_EXPORTED,
        )
    }

    /**
     * Bluetooth OFF による接続消滅を、GATT 切断と同じ経路（[connection] の
     * Disconnected）へ流す。これで MainViewModel 側の切断ハンドラが動き、CSV の
     * クローズ・切断サイドカーの記録・自動再接続が通常の切断と同じように走る。
     */
    private fun handleAdapterOff() {
        if (mockMode) return
        _scanning.value = false
        // すでに切断済みなら何もしない（アイドル中の BT OFF で余計な切断を作らない）。
        if (gatt == null && _connection.value == ConnectionState.Disconnected) return
        // 理由は Disconnected を流す前に確定させる（GATT 経由の切断と同じ順序）。
        lastDisconnectStatus = GATT_STATUS_ADAPTER_OFF
        runCatching { gatt?.close() }
        gatt = null
        currentAddress = null
        _connection.value = ConnectionState.Disconnected
    }

    private val mock = MockMemeBleEngine(
        scanning = _scanning,
        devices = _devices,
        connection = _connection,
        incoming = _incoming,
        descriptorWritten = _descriptorWritten,
        playbackPosition = _playbackPosition,
    )

    /**
     * When true, all calls are routed to [MockMemeBleEngine] and the real
     * GATT stack is never touched. Toggling while connected forces the
     * existing connection (real or mock) closed so the next scan/connect
     * cycle starts from a clean state.
     */
    override var mockMode: Boolean = false
        set(value) {
            if (field == value) return
            field = value
            if (value) {
                runCatching { gatt?.disconnect() }
                runCatching { gatt?.close() }
                gatt = null
            } else {
                mock.reset()
            }
            _scanning.value = false
            _devices.value = emptySet()
            _connection.value = ConnectionState.Disconnected
            currentAddress = null
        }

    /** Hand the mock engine logged rows to replay instead of synthetic data. */
    override fun loadMockCsv(data: MockCsvData) = mock.loadCsv(data)

    /** Freeze CSV playback in place (Pause). No-op outside mock mode. */
    override fun pausePlayback() {
        if (mockMode) mock.pause()
    }

    /** Continue CSV playback from where it was paused (Resume). No-op outside mock mode. */
    override fun resumePlayback() {
        if (mockMode) mock.resume()
    }

    /** Jump the CSV playback position by [deltaSeconds] (negative rewinds). No-op outside mock mode. */
    override fun seekPlayback(deltaSeconds: Double) {
        if (mockMode) mock.seek(deltaSeconds)
    }

    fun hasConnectPermission(): Boolean {
        if (mockMode) return true
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.S) return true
        return ContextCompat.checkSelfPermission(
            context, Manifest.permission.BLUETOOTH_CONNECT
        ) == PackageManager.PERMISSION_GRANTED
    }

    fun hasScanPermission(): Boolean {
        if (mockMode) return true
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.S) return true
        return ContextCompat.checkSelfPermission(
            context, Manifest.permission.BLUETOOTH_SCAN
        ) == PackageManager.PERMISSION_GRANTED
    }

    fun isBluetoothEnabled(): Boolean = mockMode || adapter?.isEnabled == true

    // サービス UUID でコントローラ側マッチングさせるフィルタ。ソフト側の手動バイト解析や
    // device.name への依存を無くし、取りこぼしを減らす（名前はスキャンレスポンスにしか
    // 載らないことがあり、以前は名前が取れないと確定 MEME でも捨てていた）。
    private val scanFilters = listOf(
        ScanFilter.Builder()
            .setServiceUuid(ParcelUuid(MemeBleConstants.SERVICE_UUID))
            .build()
    )

    // 1 回のスキャンで確実に見つけるための設定。LOW_LATENCY で受信窓を最大化し、
    // AGGRESSIVE＋MAX_ADVERTISEMENT で 1 パケットでも即通知させる。
    private val scanSettings = ScanSettings.Builder()
        .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY)
        .setCallbackType(ScanSettings.CALLBACK_TYPE_ALL_MATCHES)
        .setMatchMode(ScanSettings.MATCH_MODE_AGGRESSIVE)
        .setNumOfMatches(ScanSettings.MATCH_NUM_MAX_ADVERTISEMENT)
        .build()

    private val scanCallback = object : ScanCallback() {
        override fun onScanResult(callbackType: Int, result: ScanResult) = addScanResult(result)

        override fun onBatchScanResults(results: MutableList<ScanResult>) {
            results.forEach { addScanResult(it) }
        }

        override fun onScanFailed(errorCode: Int) {
            _scanning.value = false
        }
    }

    // フィルタが MEME であることを保証するので、アドレスをそのまま採用する。
    private fun addScanResult(result: ScanResult) {
        val address = result.device?.address ?: return
        _devices.update { it + address }
    }

    override fun startScan() {
        if (mockMode) { mock.startScan(); return }
        if (_scanning.value) return
        // スキャン開始＝一覧のやり直し。実際にスキャンを張れない場合（BT OFF・権限なし）も
        // 先に空にする。古い発見結果が残っていると再接続ループが毎周「見つかった」と
        // 判断し、即失敗する connect を待ち時間なしで回し続ける空回りになるため。
        _devices.value = emptySet()
        if (!hasScanPermission() || adapter?.isEnabled != true) return
        // アドバタイズを待たずに、OS／他アプリが既に握っている端末を先にリストへ入れる。
        mergeSystemDevices()
        scanner = adapter.bluetoothLeScanner ?: return
        scanner?.startScan(scanFilters, scanSettings, scanCallback)
        _scanning.value = true
    }

    /**
     * OS や他アプリが既に GATT 接続している MEME、および過去にボンディングした MEME を
     * スキャン結果にマージする。JINS MEME SDK と同じく getConnectedDevices(GATT) を使う。
     * 判定は「キャッシュ済みサービス UUID に [MemeBleConstants.SERVICE_UUID] を含む」を優先し、
     * uuids が空の端末は名前パターンで補完する。
     */
    fun mergeSystemDevices() {
        if (mockMode) return
        val m = manager ?: return
        if (!hasConnectPermission()) return
        val candidates = buildSet {
            addAll(runCatching { m.getConnectedDevices(BluetoothProfile.GATT) }.getOrDefault(emptyList()))
            addAll(runCatching { adapter?.bondedDevices }.getOrNull().orEmpty())
        }
        val hits = mutableListOf<String>()
        for (d in candidates) {
            val name = runCatching { d.name }.getOrNull()
            val isMeme = isMemeDevice(d, name)
            // 判定が外れて拾えない時の切り分け用に候補と結果を必ず残す。
            LogCat.d(TAG, "mergeSystemDevices: name=$name addr=${d.address} meme=$isMeme")
            if (isMeme) hits += d.address
        }
        if (hits.isNotEmpty()) _devices.update { it + hits }
    }

    // BLE の GATT サービス UUID は device.uuids(SDP キャッシュ)には基本載らないため、
    // 接続済み端末の判定は公式 SDK 同様に名前で行う。uuids は機種/OS 依存の保険。
    private fun isMemeDevice(device: BluetoothDevice, name: String?): Boolean {
        if (!hasConnectPermission()) return false
        if (name != null && MemeBleConstants.DEVICE_NAME_REGEX.containsMatchIn(name)) return true
        val serviceParcel = ParcelUuid(MemeBleConstants.SERVICE_UUID)
        return runCatching { device.uuids }.getOrNull()?.any { it == serviceParcel } == true
    }

    override fun stopScan() {
        if (mockMode) { mock.stopScan(); return }
        if (!hasScanPermission()) return
        scanner?.stopScan(scanCallback)
        _scanning.value = false
    }

    private val gattCallback = object : BluetoothGattCallback() {
        override fun onConnectionStateChange(g: BluetoothGatt, status: Int, newState: Int) {
            when (newState) {
                BluetoothProfile.STATE_CONNECTED -> {
                    _connection.value = ConnectionState.Connected
                    g.discoverServices()
                }
                BluetoothProfile.STATE_CONNECTING -> _connection.value = ConnectionState.Connecting
                BluetoothProfile.STATE_DISCONNECTING -> _connection.value = ConnectionState.Disconnecting
                BluetoothProfile.STATE_DISCONNECTED -> {
                    // 切断理由は Disconnected を流す前に確定させる。購読側
                    // (MainViewModel.collectConnection) が同じ切断の status として
                    // 読み、サイドカーCSV へ書くため。
                    lastDisconnectStatus = status
                    _connection.value = ConnectionState.Disconnected
                    g.close()
                    gatt = null
                    currentAddress = null
                }
            }
        }

        override fun onServicesDiscovered(g: BluetoothGatt, status: Int) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                _connection.value = ConnectionState.ServicesReady
            }
        }

        override fun onDescriptorWrite(
            g: BluetoothGatt, descriptor: BluetoothGattDescriptor, status: Int
        ) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                _descriptorWritten.tryEmit(Unit)
            }
        }

        @Deprecated("Used on API < 33; API 33+ uses the overload below.")
        @Suppress("DEPRECATION")
        override fun onCharacteristicChanged(
            g: BluetoothGatt, characteristic: BluetoothGattCharacteristic
        ) {
            val value = characteristic.value ?: return
            val decoded = runCatching { DataEncryption.decode(value) }.getOrNull() ?: return
            _incoming.tryEmit(decoded)
        }

        override fun onCharacteristicChanged(
            g: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            value: ByteArray
        ) {
            val decoded = runCatching { DataEncryption.decode(value) }.getOrNull() ?: return
            _incoming.tryEmit(decoded)
        }
    }

    override fun connect(address: String): Boolean {
        // 前の接続の切断理由を持ち越さない（再接続後の切断で古い status を
        // サイドカーへ書かないため）。
        lastDisconnectStatus = GATT_STATUS_NONE
        if (mockMode) {
            currentAddress = address
            return mock.connect(address)
        }
        val a = adapter ?: return false
        if (!hasConnectPermission() || !a.isEnabled) return false
        val device: BluetoothDevice = runCatching { a.getRemoteDevice(address) }
            .getOrNull() ?: return false
        DataEncryption.setKey(address)
        currentAddress = address
        _connection.value = ConnectionState.Connecting
        gatt = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.CINNAMON_BUN) {
            val settings = BluetoothGattConnectionSettings.Builder()
                .setAutoConnectEnabled(false)
                .setTransport(BluetoothDevice.TRANSPORT_LE)
                .build()
            device.connectGatt(settings, ContextCompat.getMainExecutor(context), gattCallback)
        } else {
            @Suppress("DEPRECATION")
            device.connectGatt(context, false, gattCallback, BluetoothDevice.TRANSPORT_LE)
        }
        return gatt != null
    }

    fun enableNotifications(): Boolean {
        if (mockMode) return mock.enableNotifications()
        val g = gatt ?: return false
        val service = g.getService(MemeBleConstants.SERVICE_UUID) ?: return false
        val rx = service.getCharacteristic(MemeBleConstants.RX_CHAR_UUID) ?: return false
        if (!g.setCharacteristicNotification(rx, true)) return false
        val descriptor = rx.getDescriptor(MemeBleConstants.CCCD_UUID) ?: return false
        val payload = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            g.writeDescriptor(descriptor, payload) == BluetoothStatusCodes.SUCCESS
        } else {
            @Suppress("DEPRECATION")
            descriptor.value = payload
            @Suppress("DEPRECATION")
            g.writeDescriptor(descriptor)
        }
    }

    override fun disconnect() {
        if (mockMode) { mock.disconnect(); return }
        gatt?.disconnect()
    }

    fun send(data: ByteArray): Boolean {
        if (mockMode) return mock.send(data)
        val g = gatt ?: return false
        val service = g.getService(MemeBleConstants.SERVICE_UUID) ?: return false
        val tx = service.getCharacteristic(MemeBleConstants.TX_CHAR_UUID) ?: return false
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            g.writeCharacteristic(tx, data, tx.writeType) == BluetoothStatusCodes.SUCCESS
        } else {
            @Suppress("DEPRECATION")
            run { tx.value = data }
            @Suppress("DEPRECATION")
            g.writeCharacteristic(tx)
        }
    }

    fun currentAddress(): String? = currentAddress
}
