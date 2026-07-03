package com.jins_jp.meme.academic.ble

import com.jins.meme.academic.util.DataEncryption
import com.jins_jp.meme.academic.data.MockCsvData
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlin.math.sin

/**
 * Software emulator for the MEME device. Reuses the same MutableStateFlows
 * /MutableSharedFlows the real BLE callback drives, so MainViewModel observes
 * scan results, connection state changes and incoming packets the same way it
 * does for live hardware. All command parsing follows the protocol described
 * in MemeBleConstants and DataParser.
 */
class MockMemeBleEngine(
    private val scanning: MutableStateFlow<Boolean>,
    private val devices: MutableStateFlow<Set<String>>,
    private val connection: MutableStateFlow<ConnectionState>,
    private val incoming: MutableSharedFlow<ByteArray>,
    private val descriptorWritten: MutableSharedFlow<Unit>,
) {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)

    private var address: String? = null
    private var streamJob: Job? = null
    private var packetCount = 0
    // 1=Standard/100Hz defaults match the device factory state.
    private var modeId = 1
    private var qualityId = 1
    private var accRangeId = 0
    private var gyroRangeId = 0

    // Logged data loaded from a CSV; when present the stream replays these rows
    // instead of generating synthetic sine waves.
    private var csvRows: List<IntArray>? = null
    private var csvIndex = 0

    /** Replace the synthetic generator with rows parsed from a logger CSV. */
    fun loadCsv(data: MockCsvData) {
        csvRows = data.rows
        csvIndex = 0
    }

    fun startScan() {
        scanning.value = true
        scope.launch {
            delay(400)
            devices.update { it + MOCK_ADDRESS }
        }
    }

    fun stopScan() {
        scanning.value = false
    }

    fun connect(addr: String): Boolean {
        address = addr
        DataEncryption.setKey(addr)
        connection.value = ConnectionState.Connecting
        scope.launch {
            delay(200)
            connection.value = ConnectionState.Connected
            delay(200)
            connection.value = ConnectionState.ServicesReady
        }
        return true
    }

    fun disconnect() {
        stopStream()
        scope.launch {
            connection.value = ConnectionState.Disconnecting
            delay(150)
            connection.value = ConnectionState.Disconnected
            address = null
        }
    }

    fun enableNotifications(): Boolean {
        scope.launch {
            delay(100)
            descriptorWritten.tryEmit(Unit)
        }
        return true
    }

    fun send(encoded: ByteArray): Boolean {
        val data = runCatching { DataEncryption.decode(encoded) }.getOrNull() ?: return false
        scope.launch {
            // Simulate a small command-to-response round-trip latency.
            delay(40)
            handleCommand(data)
        }
        return true
    }

    /** Called when the repository switches back to real BLE mode. */
    fun reset() {
        stopStream()
        scanning.value = false
        devices.value = emptySet()
        connection.value = ConnectionState.Disconnected
        address = null
        packetCount = 0
        csvRows = null
        csvIndex = 0
    }

    private fun handleCommand(data: ByteArray) {
        if (data.size < 2) return
        when (data[1]) {
            MemeBleConstants.ADN_GET_DEV_INFO -> respondDevInfo()
            MemeBleConstants.ADN_GET_MODE -> respondMode()
            MemeBleConstants.ADN_GET_6AXIS_PARAMS -> respondParams()
            MemeBleConstants.ADN_CLR_PARAMS -> {
                modeId = 1; qualityId = 1; accRangeId = 0; gyroRangeId = 0
                respondOk()
            }
            MemeBleConstants.ADN_SET_MODE -> {
                // ファーム同様 mode=byte4, quality=byte5。
                modeId = data[4].toInt() and 0xFF
                qualityId = data[5].toInt() and 0xFF
                respondOk()
            }
            MemeBleConstants.ADN_SET_6AXIS_PARAMS -> {
                accRangeId = data[2].toInt() and 0xFF
                gyroRangeId = data[3].toInt() and 0xFF
                respondOk()
            }
            MemeBleConstants.ADN_START_STOP_SEND -> {
                if (data[2] == 0x01.toByte()) startStream() else stopStream()
                // The device acks start/stop with a RESP too.
                respondOk()
            }
        }
    }

    private fun respondDevInfo() {
        val resp = ByteArray(20)
        resp[0] = MemeBleConstants.DATA_LENGTH
        resp[1] = MemeBleConstants.AUP_REPORT_DEV_INFO
        // Major version 0xCAFE so a mock firmware ID is recognisable in logs.
        resp[2] = 0xFE.toByte() // little endian
        resp[3] = 0xCA.toByte()
        resp[4] = 0  // patch
        resp[5] = 0  // minor
        resp[6] = 1  // major
        emit(resp)
    }

    private fun respondMode() {
        val resp = ByteArray(20)
        resp[0] = MemeBleConstants.DATA_LENGTH
        resp[1] = MemeBleConstants.AUP_REPORT_MODE
        resp[4] = (modeId and 0xFF).toByte()
        resp[5] = (qualityId and 0xFF).toByte()
        emit(resp)
    }

    private fun respondParams() {
        val resp = ByteArray(20)
        resp[0] = MemeBleConstants.DATA_LENGTH
        resp[1] = MemeBleConstants.AUP_REPORT_6AXIS_PARAMS
        resp[2] = (accRangeId and 0xFF).toByte()
        resp[3] = (gyroRangeId and 0xFF).toByte()
        emit(resp)
    }

    private fun respondOk() {
        val resp = ByteArray(20)
        resp[0] = MemeBleConstants.DATA_LENGTH
        resp[1] = MemeBleConstants.AUP_REPORT_RESP
        resp[2] = 0x00  // success
        emit(resp)
    }

    private fun emit(packet: ByteArray) {
        incoming.tryEmit(packet)
    }

    private fun startStream() {
        stopStream()
        packetCount = 0
        // Restart CSV playback from the first logged row on each measurement.
        csvIndex = 0
        val quality = qualityId // 1=100Hz, 2=50Hz (ファームの transMode 値)
        val type = when (modeId) {
            2 -> MemeBleConstants.AUP_REPORT_ACADEMIA2
            3 -> MemeBleConstants.AUP_REPORT_ACADEMIA3
            else -> MemeBleConstants.AUP_REPORT_ACADEMIA1
        }
        streamJob = scope.launch {
            while (isActive) {
                if (quality == 1 && (type == MemeBleConstants.AUP_REPORT_ACADEMIA1 || type == MemeBleConstants.AUP_REPORT_ACADEMIA2)) {
                    // 100Hz: 2 サンプルを 40byte(2 パケット)にまとめて送る。
                    val p1 = createDataPacket(type)
                    val p2 = createDataPacket(type)
                    emit(p1 + p2)
                } else {
                    emit(createDataPacket(type))
                }
                // 50Hz: 1 sample / 20ms. 100Hz: 2 samples / 20ms.
                delay(20L)
            }
        }
    }

    private fun stopStream() {
        streamJob?.cancel()
        streamJob = null
    }

    private fun createDataPacket(type: Byte): ByteArray {
        val packet = ByteArray(20)
        packet[0] = MemeBleConstants.DATA_LENGTH
        packet[1] = type
        // Head: low 12 bits = packetCount, high 4 bits = battery (fixed full).
        val head = (packetCount and 0x0FFF) or ((BATTERY_LEVEL and 0x0F) shl 12)
        packet[2] = (head and 0xFF).toByte()
        packet[3] = ((head ushr 8) and 0xFF).toByte()

        val rows = csvRows
        if (rows != null && rows.isNotEmpty()) {
            // Replay one logged row, looping back to the start once exhausted.
            encodeRow(packet, type, rows[csvIndex])
            csvIndex = (csvIndex + 1) % rows.size
        } else {
            encodeSynthetic(packet, type, packetCount.toDouble() * 0.01)
        }

        packetCount = (packetCount + 1) and 0x0FFF
        return packet
    }

    /** Re-encode a CSV value row into a packet, the inverse of DataParser. */
    private fun encodeRow(packet: ByteArray, type: Byte, row: IntArray) {
        when (type) {
            // accX/Y/Z + raw EOG L1/R1/L2/R2; H/V columns are re-derived on parse.
            MemeBleConstants.AUP_REPORT_ACADEMIA1 ->
                for (i in 0 until 7) putShortLE(packet, 4 + i * 2, row.getOrElse(i) { 0 })
            // accX/Y/Z + gyroX/Y/Z + raw EOG L/R.
            MemeBleConstants.AUP_REPORT_ACADEMIA2 ->
                for (i in 0 until 8) putShortLE(packet, 4 + i * 2, row.getOrElse(i) { 0 })
            // Quaternion W/X/Y/Z as 32-bit values.
            MemeBleConstants.AUP_REPORT_ACADEMIA3 ->
                for (i in 0 until 4) putIntLE(packet, 4 + i * 4, row.getOrElse(i) { 0 })
        }
    }

    private fun encodeSynthetic(packet: ByteArray, type: Byte, t: Double) {
        when (type) {
            MemeBleConstants.AUP_REPORT_ACADEMIA1 -> {
                // accX/Y/Z then 4 raw EOG channels.
                putShortLE(packet, 4, sineI(t, 0.8, 8000, 0.0))
                putShortLE(packet, 6, sineI(t, 0.8, 8000, 0.5))
                putShortLE(packet, 8, sineI(t, 0.4, 6000, 0.25) + 16000)
                putShortLE(packet, 10, sineI(t, 0.5, 180, 0.0))
                putShortLE(packet, 12, sineI(t, 0.5, 180, 0.3))
                putShortLE(packet, 14, sineI(t, 0.7, 160, 0.6))
                putShortLE(packet, 16, sineI(t, 0.7, 160, 0.9))
            }
            MemeBleConstants.AUP_REPORT_ACADEMIA2 -> {
                putShortLE(packet, 4, sineI(t, 0.8, 8000, 0.0))
                putShortLE(packet, 6, sineI(t, 0.8, 8000, 0.5))
                putShortLE(packet, 8, sineI(t, 0.4, 6000, 0.25) + 16000)
                putShortLE(packet, 10, sineI(t, 1.0, 12000, 0.0))
                putShortLE(packet, 12, sineI(t, 1.0, 12000, 0.33))
                putShortLE(packet, 14, sineI(t, 1.0, 12000, 0.66))
                putShortLE(packet, 16, sineI(t, 0.5, 180, 0.0))
                putShortLE(packet, 18, sineI(t, 0.5, 180, 0.5))
            }
            MemeBleConstants.AUP_REPORT_ACADEMIA3 -> {
                // Quaternion values are 32-bit, sourced from a slow rotation.
                putIntLE(packet, 4, sineI(t, 0.2, 1_000_000, 0.0))
                putIntLE(packet, 8, sineI(t, 0.2, 1_000_000, 0.25))
                putIntLE(packet, 12, sineI(t, 0.2, 1_000_000, 0.5))
                putIntLE(packet, 16, sineI(t, 0.2, 1_000_000, 0.75))
            }
        }
    }

    private fun sineI(t: Double, hz: Double, amp: Int, phase: Double): Int {
        val v = sin((t * hz + phase) * 2.0 * Math.PI) * amp
        return v.toInt()
    }

    private fun putShortLE(arr: ByteArray, idx: Int, value: Int) {
        arr[idx] = (value and 0xFF).toByte()
        arr[idx + 1] = ((value ushr 8) and 0xFF).toByte()
    }

    private fun putIntLE(arr: ByteArray, idx: Int, value: Int) {
        arr[idx] = (value and 0xFF).toByte()
        arr[idx + 1] = ((value ushr 8) and 0xFF).toByte()
        arr[idx + 2] = ((value ushr 16) and 0xFF).toByte()
        arr[idx + 3] = ((value ushr 24) and 0xFF).toByte()
    }

    companion object {
        const val MOCK_ADDRESS = "MO:CK:00:00:00:01"
        private const val BATTERY_LEVEL = 5
    }
}
