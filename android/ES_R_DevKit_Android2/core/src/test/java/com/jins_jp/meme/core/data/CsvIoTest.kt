package com.jins_jp.meme.core.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.ByteArrayInputStream
import java.io.ByteArrayOutputStream
import java.util.zip.GZIPInputStream
import java.util.zip.GZIPOutputStream

/**
 * [decompressIfGzip] と、[CsvWriter] が作る「連結 gzip メンバ」形式が普通の
 * gzip ファイルとして読めることの検証。
 */
class CsvIoTest {

    /** [CsvWriter.flush] と同じ書き方（1 チャンク = 独立した gzip メンバ 1 つ）。 */
    private fun writeAsConcatenatedMembers(chunks: List<String>): ByteArray {
        val out = ByteArrayOutputStream()
        for (chunk in chunks) {
            // 1 メンバごとに閉じきる（追記のたびにトレーラまで書く実装と同じ）。
            GZIPOutputStream(out).use { it.write(chunk.toByteArray(Charsets.UTF_8)) }
        }
        return out.toByteArray()
    }

    private fun readAll(bytes: ByteArray): String =
        decompressIfGzip(ByteArrayInputStream(bytes)).readBytes().toString(Charsets.UTF_8)

    @Test
    fun passesThroughPlainTextUnchanged() {
        val text = "// Data mode  : Standard\r\n,1,2026/01/01 00:00:00.000,1,2\r\n"
        assertEquals(text, readAll(text.toByteArray(Charsets.UTF_8)))
    }

    @Test
    fun decompressesSingleGzipMember() {
        val text = "hello\r\nworld\r\n"
        val gz = ByteArrayOutputStream().also { out ->
            GZIPOutputStream(out).use { it.write(text.toByteArray(Charsets.UTF_8)) }
        }.toByteArray()
        assertEquals(text, readAll(gz))
    }

    /**
     * これが [CsvWriter] の書き方の要。flush ごとに独立したメンバを追記しても、
     * 読み出しでは 1 本の連続したテキストに戻らなければならない。
     */
    @Test
    fun readsConcatenatedGzipMembersAsOneStream() {
        val chunks = listOf("header\r\n", "row1\r\n", "row2\r\n", "row3\r\n")
        val bytes = writeAsConcatenatedMembers(chunks)
        assertEquals(chunks.joinToString(""), readAll(bytes))
    }

    @Test
    fun detectsGzipByMagicBytesNotByName() {
        val gz = ByteArrayOutputStream().also { out ->
            GZIPOutputStream(out).use { it.write("x".toByteArray(Charsets.UTF_8)) }
        }.toByteArray()
        assertTrue(decompressIfGzip(ByteArrayInputStream(gz)) is GZIPInputStream)
        val plain = decompressIfGzip(ByteArrayInputStream("x".toByteArray(Charsets.UTF_8)))
        assertTrue(plain !is GZIPInputStream)
    }

    /** 空ストリーム（マジックを読もうとして EOF）でも例外にせず素通しする。 */
    @Test
    fun handlesEmptyInput() {
        assertEquals("", readAll(ByteArray(0)))
    }

    /* ---- 設定による本体データCSVの名前と MIME ---- */

    @Test
    fun dataFileNameFollowsTheCompressionSetting() {
        assertEquals("AABBCC_20260904012345.csv.gz", dataFileName("AABBCC_20260904012345", true))
        assertEquals("AABBCC_20260904012345.csv", dataFileName("AABBCC_20260904012345", false))
        assertEquals("application/gzip", dataFileMime(true))
        assertEquals("text/csv", dataFileMime(false))
    }

    /* ---- ローダが両形式を同じように読めること ---- */

    private fun loggerCsv(): String = buildString {
        append("// Data mode  : Full\r\n")
        append("// Transmission speed  : 100Hz\r\n")
        append("// Acceleration sensor's range  : 8g\r\n")
        append("// Gyroscope sensor's range  : 1000dps\r\n")
        append("//\r\n")
        append("//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V\r\n")
        for (i in 1..3) {
            append("," + i + ",2026/01/01 00:00:0" + i + ".000,1,2,3,4,5,6,7,8,9,10\r\n")
        }
        append("lc:35.6802_139.7521,4,2026/01/01 00:00:04.000,1,2,3,4,5,6,7,8,9,10\r\n")
    }

    private fun assertParsedLoggerCsv(data: MockCsvData) {
        assertEquals(MemeMode.Full, data.settings.mode)
        assertEquals(MemeQuality.Hz100, data.settings.quality)
        assertEquals(AccRange.G8, data.settings.accRange)
        assertEquals(GyroRange.Dps1000, data.settings.gyroRange)
        assertEquals(4, data.rows.size)
        assertEquals(1, data.artifacts.size)
        assertEquals(4, data.artifacts[0].rowNumber)
        assertEquals("lc:35.6802_139.7521", data.artifacts[0].text)
    }

    @Test
    fun loaderReadsPlainCsv() {
        val bytes = loggerCsv().toByteArray(Charsets.UTF_8)
        assertParsedLoggerCsv(MockCsvLoader.parse(ByteArrayInputStream(bytes)))
    }

    /** 実際に書かれるのと同じ「ヘッダ + 100 行ごと」の連結メンバ形式で読めること。 */
    @Test
    fun loaderReadsGzippedCsvWrittenAsConcatenatedMembers() {
        val text = loggerCsv()
        val split = text.indexOf("//ARTIFACT")
        val bytes = writeAsConcatenatedMembers(
            listOf(text.substring(0, split), text.substring(split)),
        )
        assertParsedLoggerCsv(MockCsvLoader.parse(ByteArrayInputStream(bytes)))
    }
}
