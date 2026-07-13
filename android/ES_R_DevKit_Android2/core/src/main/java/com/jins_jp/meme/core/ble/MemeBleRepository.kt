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
import android.content.Context
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
        if (!hasScanPermission() || adapter?.isEnabled != true) return
        if (_scanning.value) return
        _devices.value = emptySet()
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
