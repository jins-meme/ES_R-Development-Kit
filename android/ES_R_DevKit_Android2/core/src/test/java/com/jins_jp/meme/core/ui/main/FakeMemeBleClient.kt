package com.jins_jp.meme.core.ui.main

import com.jins_jp.meme.core.ble.ConnectionState
import com.jins_jp.meme.core.ble.MemeBleClient
import com.jins_jp.meme.core.ble.MockMemeBleEngine
import com.jins_jp.meme.core.ble.PlaybackPosition
import com.jins_jp.meme.core.data.MockCsvData
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow

/**
 * 呼び出しを記録し、Flow をテストから直接操作できる [MemeBleClient] の Fake。
 * 実リポジトリの要点だけ模倣する: startScan はデバイス一覧をクリアして
 * scanning=true（mock モード中は mock デバイスを広告）、connect は
 * [connectResult] に応じて Connecting へ遷移。それ以降の状態遷移
 * （ServicesReady 到達や descriptorWritten）はテスト側が明示的に流す。
 */
internal class FakeMemeBleClient : MemeBleClient {

    override val scanning = MutableStateFlow(false)
    override val devices = MutableStateFlow<Set<String>>(emptySet())
    override val connection = MutableStateFlow(ConnectionState.Disconnected)
    override val descriptorWritten = MutableSharedFlow<Unit>(extraBufferCapacity = 4)
    override val playbackPosition = MutableStateFlow(PlaybackPosition())

    /** mockMode 代入の履歴（再突入時の false→true 強制を検証するため）。 */
    val mockModeSets = mutableListOf<Boolean>()
    override var mockMode: Boolean = false
        set(value) {
            field = value
            mockModeSets += value
        }

    var loadedCsv: MockCsvData? = null
    override fun loadMockCsv(data: MockCsvData) {
        loadedCsv = data
    }

    var startScanCount = 0
    var stopScanCount = 0
    var disconnectCount = 0
    val connectedAddresses = mutableListOf<String>()

    /** connect() の戻り値（false で「接続開始に失敗」を再現）。 */
    var connectResult = true

    /** mock モード中のスキャンで mock デバイスを広告するか（実 mock エンジンの既定挙動）。 */
    var advertiseOnScan = true

    override fun startScan() {
        startScanCount++
        scanning.value = true
        devices.value = if (mockMode && advertiseOnScan) {
            setOf(MockMemeBleEngine.MOCK_ADDRESS)
        } else {
            emptySet()
        }
    }

    override fun stopScan() {
        stopScanCount++
        scanning.value = false
    }

    override fun connect(address: String): Boolean {
        connectedAddresses += address
        if (connectResult) connection.value = ConnectionState.Connecting
        return connectResult
    }

    override fun disconnect() {
        disconnectCount++
        connection.value = ConnectionState.Disconnected
    }

    var pausePlaybackCount = 0
    override fun pausePlayback() {
        pausePlaybackCount++
    }

    var resumePlaybackCount = 0
    override fun resumePlayback() {
        resumePlaybackCount++
    }

    val seekPlaybackCalls = mutableListOf<Double>()
    override fun seekPlayback(deltaSeconds: Double) {
        seekPlaybackCalls += deltaSeconds
    }
}
