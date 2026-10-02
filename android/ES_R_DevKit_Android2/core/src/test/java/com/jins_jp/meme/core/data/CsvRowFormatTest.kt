package com.jins_jp.meme.core.data

import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Before
import org.junit.Test
import java.util.Locale

/**
 * CSV のデータ行（[formatRow]）と DATE 列（[formatGmtDate]）の書式。端末の言語が数字を ASCII 以外で書く
 * ロケール（アラビア語・ペルシア語など）でも、Mac・Windows・グラフ画面が読める ASCII の数字で書くこと。
 */
class CsvRowFormatTest {

    private lateinit var original: Locale

    @Before
    fun saveLocale() { original = Locale.getDefault() }

    @After
    fun restoreLocale() { Locale.setDefault(original) }

    // 2026/10/02 10:24:23.004 GMT
    private val t = 1_790_936_663_004L

    @Test
    fun formatsRowAsAsciiEvenWithArabicLocale() {
        for (tag in listOf("ja-JP", "en-US", "ar-EG", "fa-IR", "bn-BD")) {
            Locale.setDefault(Locale.forLanguageTag(tag))
            assertEquals(tag, ",1,2026/10/02 10:24:23.004,-337,3003", formatRow("", 1, t, intArrayOf(-337, 3003)))
        }
    }

    @Test
    fun keepsArtifactAndMillisecondDigits() {
        Locale.setDefault(Locale.forLanguageTag("ar-EG"))
        assertEquals("X,101,2026/10/02 10:24:23.004", formatRow("X", 101, t, intArrayOf()))
        assertEquals("2026/10/02 10:24:24.000", formatGmtDate(t + 996))
    }
}
