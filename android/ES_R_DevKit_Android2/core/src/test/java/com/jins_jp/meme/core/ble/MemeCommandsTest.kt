package com.jins_jp.meme.core.ble

import com.jins_jp.meme.core.data.AccRange
import com.jins_jp.meme.core.data.GyroRange
import com.jins_jp.meme.core.data.MeasurementSettings
import com.jins_jp.meme.core.data.MemeMode
import com.jins_jp.meme.core.data.MemeQuality
import org.junit.Assert.assertEquals
import org.junit.Test

/**
 * [MemeCommands] の検証: 20 byte ADN コマンドの固定レイアウト（元 MainViewModel の
 * ByteArray 組み立てと同一のバイト配置）。
 */
class MemeCommandsTest {

    @Test
    fun clearParamsHasCorrectBytes() {
        val d = MemeCommands.clearParams()
        assertEquals(20, d.size)
        assertEquals(MemeBleConstants.DATA_LENGTH, d[0])
        assertEquals(MemeBleConstants.ADN_CLR_PARAMS, d[1])
        assertEquals(0xFF.toByte(), d[2])
    }

    @Test
    fun getDeviceInfoHasCorrectBytes() {
        val d = MemeCommands.getDeviceInfo()
        assertEquals(20, d.size)
        assertEquals(MemeBleConstants.DATA_LENGTH, d[0])
        assertEquals(MemeBleConstants.ADN_GET_DEV_INFO, d[1])
    }

    @Test
    fun getModeHasCorrectBytes() {
        val d = MemeCommands.getMode()
        assertEquals(20, d.size)
        assertEquals(MemeBleConstants.DATA_LENGTH, d[0])
        assertEquals(MemeBleConstants.ADN_GET_MODE, d[1])
    }

    @Test
    fun setModeEncodesModeAndQualityOrdinalsPlusOne() {
        val s = MeasurementSettings(mode = MemeMode.Full, quality = MemeQuality.Hz50)
        val d = MemeCommands.setMode(s)
        assertEquals(20, d.size)
        assertEquals(MemeBleConstants.DATA_LENGTH, d[0])
        assertEquals(MemeBleConstants.ADN_SET_MODE, d[1])
        assertEquals((MemeMode.Full.ordinal + 1).toByte(), d[4])
        assertEquals((MemeQuality.Hz50.ordinal + 1).toByte(), d[5])
    }

    @Test
    fun set6AxisParamsUsesGyroRangeOrdinalNormally() {
        val s = MeasurementSettings(
            mode = MemeMode.Standard,
            accRange = AccRange.G8,
            gyroRange = GyroRange.Dps1000,
        )
        val d = MemeCommands.set6AxisParams(s)
        assertEquals(20, d.size)
        assertEquals(MemeBleConstants.DATA_LENGTH, d[0])
        assertEquals(MemeBleConstants.ADN_SET_6AXIS_PARAMS, d[1])
        assertEquals(AccRange.G8.ordinal.toByte(), d[2])
        assertEquals(GyroRange.Dps1000.ordinal.toByte(), d[3])
    }

    @Test
    fun set6AxisParamsForcesGyroIdx3WhenQuaternion() {
        val s = MeasurementSettings(mode = MemeMode.Quaternion, gyroRange = GyroRange.Dps250)
        val d = MemeCommands.set6AxisParams(s)
        assertEquals(3.toByte(), d[3])
    }

    @Test
    fun setConfigModePutsConfigValueInModeByte() {
        val d = MemeCommands.setConfigMode()
        assertEquals(20, d.size)
        assertEquals(MemeBleConstants.DATA_LENGTH, d[0])
        assertEquals(MemeBleConstants.ADN_SET_MODE, d[1])
        assertEquals(MemeBleConstants.MODE_CONFIG, d[4])
        // quality は 0 のまま（Web Bluetooth 版 SDK の "14A400000F" と同じ並び）。
        assertEquals(0x00.toByte(), d[5])
    }

    @Test
    fun shelfMatchesWebSdkByteSequence() {
        // tkomde/webbt common/memelib_acp.js startShelf: "14415348454C46"
        val expectedPrefix = byteArrayOf(
            0x14, 0x41, 0x53, 0x48, 0x45, 0x4C, 0x46,
        )
        val d = MemeCommands.shelf()
        assertEquals(20, d.size)
        for ((i, b) in expectedPrefix.withIndex()) assertEquals(b, d[i])
        // 残りは 0 埋め（暗号化側のチェックサム計算が JS 版と一致する条件）。
        for (i in expectedPrefix.size until d.size) assertEquals(0x00.toByte(), d[i])
    }

    @Test
    fun startStopEncodesStartAndStopByte() {
        val start = MemeCommands.startStop(true)
        assertEquals(20, start.size)
        assertEquals(MemeBleConstants.DATA_LENGTH, start[0])
        assertEquals(MemeBleConstants.ADN_START_STOP_SEND, start[1])
        assertEquals(0x01.toByte(), start[2])

        val stop = MemeCommands.startStop(false)
        assertEquals(0x00.toByte(), stop[2])
    }
}
