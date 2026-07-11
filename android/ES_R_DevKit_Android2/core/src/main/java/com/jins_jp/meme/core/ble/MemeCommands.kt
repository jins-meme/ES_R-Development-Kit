package com.jins_jp.meme.core.ble

import com.jins_jp.meme.core.data.MeasurementSettings
import com.jins_jp.meme.core.data.MemeMode

/**
 * 20 byte の ADN コマンド(暗号化前の平文)を組み立てる純粋関数群。
 * 送信時は DataEncryption.encode を通す(MainViewModel.sendEncoded)。
 */
object MemeCommands {
    private fun command(op: Byte): ByteArray {
        val data = ByteArray(20)
        data[0] = MemeBleConstants.DATA_LENGTH
        data[1] = op
        return data
    }

    /** 端末パラメータの初期化(ADN_CLR_PARAMS)。 */
    fun clearParams(): ByteArray = command(MemeBleConstants.ADN_CLR_PARAMS).also {
        it[2] = 0xFF.toByte()
    }

    fun getDeviceInfo(): ByteArray = command(MemeBleConstants.ADN_GET_DEV_INFO)

    fun getMode(): ByteArray = command(MemeBleConstants.ADN_GET_MODE)

    /** ファーム(MEMELib memeAdnSetMode)は mode=byte4, quality(transMode)=byte5 を読む。 */
    fun setMode(s: MeasurementSettings): ByteArray = command(MemeBleConstants.ADN_SET_MODE).also {
        it[4] = ((s.mode.ordinal + 1) and 0xFF).toByte()
        it[5] = ((s.quality.ordinal + 1) and 0xFF).toByte()
    }

    fun set6AxisParams(s: MeasurementSettings): ByteArray =
        command(MemeBleConstants.ADN_SET_6AXIS_PARAMS).also {
            val gyroIdx = if (s.mode == MemeMode.Quaternion) 3 else s.gyroRange.ordinal
            it[2] = (s.accRange.ordinal and 0xFF).toByte()
            it[3] = (gyroIdx and 0xFF).toByte()
        }

    /** 計測開始/停止(ADN_START_STOP_SEND, byte2 = 1/0)。 */
    fun startStop(start: Boolean): ByteArray = command(MemeBleConstants.ADN_START_STOP_SEND).also {
        it[2] = if (start) 0x01 else 0x00
    }
}
