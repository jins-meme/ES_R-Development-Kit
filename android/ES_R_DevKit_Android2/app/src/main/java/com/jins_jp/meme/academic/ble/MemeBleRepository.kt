package com.jins_jp.meme.academic.ble

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
import android.bluetooth.le.ScanResult
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import androidx.core.content.ContextCompat
import com.jins.meme.academic.util.DataEncryption
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update

enum class ConnectionState { Disconnected, Connecting, Connected, ServicesReady, Disconnecting }

@SuppressLint("MissingPermission")
class MemeBleRepository(private val context: Context) {

    private val manager: BluetoothManager? =
        context.getSystemService(Context.BLUETOOTH_SERVICE) as? BluetoothManager
    private val adapter: BluetoothAdapter? = manager?.adapter

    private val _scanning = MutableStateFlow(false)
    val scanning: StateFlow<Boolean> = _scanning.asStateFlow()

    private val _devices = MutableStateFlow<Set<String>>(emptySet())
    val devices: StateFlow<Set<String>> = _devices.asStateFlow()

    private val _connection = MutableStateFlow(ConnectionState.Disconnected)
    val connection: StateFlow<ConnectionState> = _connection.asStateFlow()

    private val _incoming = MutableSharedFlow<ByteArray>(extraBufferCapacity = 256)
    val incoming: SharedFlow<ByteArray> = _incoming.asSharedFlow()

    private val _descriptorWritten = MutableSharedFlow<Unit>(extraBufferCapacity = 4)
    val descriptorWritten: SharedFlow<Unit> = _descriptorWritten.asSharedFlow()

    private var gatt: BluetoothGatt? = null
    private var scanner: BluetoothLeScanner? = null
    private var currentAddress: String? = null

    fun hasConnectPermission(): Boolean {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.S) return true
        return ContextCompat.checkSelfPermission(
            context, Manifest.permission.BLUETOOTH_CONNECT
        ) == PackageManager.PERMISSION_GRANTED
    }

    fun hasScanPermission(): Boolean {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.S) return true
        return ContextCompat.checkSelfPermission(
            context, Manifest.permission.BLUETOOTH_SCAN
        ) == PackageManager.PERMISSION_GRANTED
    }

    fun isBluetoothEnabled(): Boolean = adapter?.isEnabled == true

    private val scanCallback = object : ScanCallback() {
        override fun onScanResult(callbackType: Int, result: ScanResult) {
            val device = result.device ?: return
            if (!hasConnectPermission()) return
            val name = runCatching { device.name }.getOrNull() ?: return
            val record = result.scanRecord?.bytes ?: return
            val uuid = parseServiceUuid(record) ?: return
            if (uuid.equals(MemeBleConstants.SERVICE_UUID.toString(), ignoreCase = true)) {
                val address = device.address
                _devices.update { it + address }
            }
        }

        override fun onScanFailed(errorCode: Int) {
            _scanning.value = false
        }
    }

    fun startScan() {
        if (!hasScanPermission() || adapter?.isEnabled != true) return
        if (_scanning.value) return
        _devices.value = emptySet()
        scanner = adapter.bluetoothLeScanner ?: return
        scanner?.startScan(scanCallback)
        _scanning.value = true
    }

    fun stopScan() {
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

    fun connect(address: String): Boolean {
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

    fun disconnect() {
        gatt?.disconnect()
    }

    fun send(data: ByteArray): Boolean {
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

    /** Parse a 128-bit service UUID from an advertised scan record. */
    private fun parseServiceUuid(record: ByteArray): String? {
        if (record.size < 21) return null
        val sb = StringBuilder()
        for (i in 20 downTo 17) sb.append(hex2(record[i].toInt() and 0xFF))
        sb.append('-')
        for (i in 16 downTo 15) sb.append(hex2(record[i].toInt() and 0xFF))
        sb.append('-')
        for (i in 14 downTo 13) sb.append(hex2(record[i].toInt() and 0xFF))
        sb.append('-')
        for (i in 12 downTo 11) sb.append(hex2(record[i].toInt() and 0xFF))
        sb.append('-')
        for (i in 10 downTo 5) sb.append(hex2(record[i].toInt() and 0xFF))
        return sb.toString()
    }

    private fun hex2(i: Int): String = "%02X".format(i)
}
