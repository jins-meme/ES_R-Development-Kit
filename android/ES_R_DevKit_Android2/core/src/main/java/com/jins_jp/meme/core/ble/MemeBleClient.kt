package com.jins_jp.meme.core.ble

import com.jins_jp.meme.core.data.MockCsvData
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow

/** CSV 再生の現在位置と総時間（秒）。再生モード外・未ロード時は 0/0。 */
data class PlaybackPosition(val positionSec: Double = 0.0, val durationSec: Double = 0.0)

/**
 * [MemeBleRepository] のうちスキャン・接続まわりのコントローラ
 * (ReconnectController / PlaybackController) が依存する面。ユニットテストで
 * Fake に差し替えるための抽出で、製品コードでの実装は [MemeBleRepository] のみ。
 */
interface MemeBleClient {
    val scanning: StateFlow<Boolean>
    val devices: StateFlow<Set<String>>
    val connection: StateFlow<ConnectionState>
    val descriptorWritten: SharedFlow<Unit>

    /** CSV 再生の現在位置。チャートの X 軸・位置表示が購読する。 */
    val playbackPosition: StateFlow<PlaybackPosition>

    /** 詳細は [MemeBleRepository.mockMode]。 */
    var mockMode: Boolean

    fun loadMockCsv(data: MockCsvData)
    fun startScan()
    fun stopScan()
    fun connect(address: String): Boolean
    fun disconnect()

    /** 詳細は [MemeBleRepository.pausePlayback]。実機モードでは no-op。 */
    fun pausePlayback()

    /** 詳細は [MemeBleRepository.resumePlayback]。実機モードでは no-op。 */
    fun resumePlayback()

    /** 詳細は [MemeBleRepository.seekPlayback]。実機モードでは no-op。 */
    fun seekPlayback(deltaSeconds: Double)
}
