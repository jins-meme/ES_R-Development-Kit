package com.jins_jp.meme.academic.data

import com.jins_jp.meme.academic.ble.MemeBleConstants
import java.nio.ByteBuffer
import java.nio.ByteOrder

data class ParsedPacket(
    val type: Byte,
    val totalCountIncrement: Long,
    val packetCount: Short,
    val batteryLevel: Short,
    val values: IntArray,
) {
    /** All fields needed for CSV output and live graphs. */
    val accX: Short get() = values.getOrElse(0) { 0 }.toShort()
    val accY: Short get() = values.getOrElse(1) { 0 }.toShort()
    val accZ: Short get() = values.getOrElse(2) { 0 }.toShort()

    // For ACADEMIA1 (Standard): values are arranged as
    // [accX, accY, accZ, eogL1, eogR1, eogL2, eogR2, eogH(diff), eogH2, eogV1, eogV2]
    // For ACADEMIA2 (Full): values[3..5] = gyro, values[6..7] = eogL/R, plus derived.
    override fun equals(other: Any?): Boolean = this === other
    override fun hashCode(): Int = System.identityHashCode(this)
}

/**
 * Parses ACADEMIA1/2/3 packets following the layout used by the original Java sources.
 */
object DataParser {
    fun parse(data: ByteArray): ParsedPacket? {
        if (data.size < 20) return null
        if (data[0] != MemeBleConstants.DATA_LENGTH) return null
        return when (val type = data[1]) {
            MemeBleConstants.AUP_REPORT_ACADEMIA1 -> parseAcademia1(data, type)
            MemeBleConstants.AUP_REPORT_ACADEMIA2 -> parseAcademia2(data, type)
            MemeBleConstants.AUP_REPORT_ACADEMIA3 -> parseAcademia3(data, type)
            else -> null
        }
    }

    private fun parseAcademia1(data: ByteArray, type: Byte): ParsedPacket {
        val head = ByteBuffer.wrap(byteArrayOf(data[2], data[3]))
            .order(ByteOrder.LITTLE_ENDIAN).short
        val count = (head.toInt() and 0x0FFF).toShort()
        val level = ((head.toInt() and 0xF000) ushr 12).toShort()

        val values = IntArray(11)
        var idx = 0
        var i = 4
        while (i < 18) {
            val short = ByteBuffer.wrap(byteArrayOf(data[i], data[i + 1]))
                .order(ByteOrder.LITTLE_ENDIAN).short.toInt()
            values[idx++] = short
            i += 2
        }
        values[7] = (values[3] - values[4]).toShort().toInt()
        values[8] = (values[5] - values[6]).toShort().toInt()
        values[9] = (0 - (values[3] + values[4]) / 2).toShort().toInt()
        values[10] = (0 - (values[5] + values[6]) / 2).toShort().toInt()

        return ParsedPacket(type, 0L, count, level, values)
    }

    private fun parseAcademia2(data: ByteArray, type: Byte): ParsedPacket {
        val head = ByteBuffer.wrap(byteArrayOf(data[2], data[3]))
            .order(ByteOrder.LITTLE_ENDIAN).short
        val count = (head.toInt() and 0x0FFF).toShort()
        val level = ((head.toInt() and 0xF000) ushr 12).toShort()

        val values = IntArray(10)
        var idx = 0
        var i = 4
        while (i < 20) {
            val short = ByteBuffer.wrap(byteArrayOf(data[i], data[i + 1]))
                .order(ByteOrder.LITTLE_ENDIAN).short.toInt()
            values[idx++] = short
            i += 2
        }
        values[8] = (values[6] - values[7]).toShort().toInt()
        values[9] = (0 - (values[6] + values[7]) / 2).toShort().toInt()
        return ParsedPacket(type, 0L, count, level, values)
    }

    private fun parseAcademia3(data: ByteArray, type: Byte): ParsedPacket {
        val head = ByteBuffer.wrap(byteArrayOf(data[2], data[3]))
            .order(ByteOrder.LITTLE_ENDIAN).short
        val count = (head.toInt() and 0x0FFF).toShort()
        val level = ((head.toInt() and 0xF000) ushr 12).toShort()

        val values = IntArray(4)
        var idx = 0
        var i = 4
        while (i < 20) {
            val int = ByteBuffer.wrap(byteArrayOf(data[i], data[i + 1], data[i + 2], data[i + 3]))
                .order(ByteOrder.LITTLE_ENDIAN).int
            values[idx++] = int
            i += 4
        }
        return ParsedPacket(type, 0L, count, level, values)
    }
}
