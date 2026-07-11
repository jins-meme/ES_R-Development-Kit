package com.jins_jp.meme.core.ble

import com.jins_jp.meme.core.data.MockCsvData
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow

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

    /** 詳細は [MemeBleRepository.mockMode]。 */
    var mockMode: Boolean

    fun loadMockCsv(data: MockCsvData)
    fun startScan()
    fun stopScan()
    fun connect(address: String): Boolean
    fun disconnect()
}
