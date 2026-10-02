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
}
