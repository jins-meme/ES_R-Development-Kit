package com.jins_jp.meme.core.data

import com.jins_jp.meme.core.ble.MemeBleConstants
import org.junit.Assert.assertEquals
import org.junit.Test

/**
 * [DataParser] が作る値の数が、各モードの列（[MemeMode.columns]。CSV のヘッダとグラフ画面へ渡す列）と一致すること。
 * ずれると CSV の行とヘッダ、グラフの系列が食い違う。
 */
class DataParserTest {

    private fun packet(type: Byte) = ByteArray(20).also {
        it[0] = MemeBleConstants.DATA_LENGTH
        it[1] = type
    }

    @Test
    fun valuesMatchTheColumnsOfEachMode() {
        val byMode = mapOf(
            MemeMode.Standard to MemeBleConstants.AUP_REPORT_ACADEMIA1,
            MemeMode.Full to MemeBleConstants.AUP_REPORT_ACADEMIA2,
            MemeMode.Quaternion to MemeBleConstants.AUP_REPORT_ACADEMIA3,
        )
        for ((mode, type) in byMode) {
            val parsed = DataParser.parse(packet(type)).single()
            assertEquals(mode.name, mode.columns.size, parsed.values.size)
        }
    }

    @Test
    fun pageNamesFollowTheBridge() {
        assertEquals(listOf("standard", "full", "quaternion"), MemeMode.entries.map { it.pageName })
        assertEquals(listOf(true, true, false), MemeMode.entries.map { it.hasGraph })
    }
}
