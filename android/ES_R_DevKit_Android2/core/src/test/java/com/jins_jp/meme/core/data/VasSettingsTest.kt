package com.jins_jp.meme.core.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** VAS の間隔入力の検査（[parseVasInterval]）と、ARTIFACT 列へ書く文字列（[formatVasArtifact]）。 */
class VasSettingsTest {

    @Test
    fun acceptsPositiveIntegersInRange() {
        assertEquals(1, parseVasInterval("1"))
        assertEquals(30, parseVasInterval("30"))
        assertEquals(1440, parseVasInterval("1440"))
        assertEquals(5, parseVasInterval("05"))
    }

    @Test
    fun rejectsOutOfRangeAndNonIntegers() {
        for (s in listOf("", "0", "1441", "9999", "-1", "+5", "1.5", "1e2", " 5", "5 ", "abc", "３０", "00000001")) {
            assertNull("\"$s\" should be rejected", parseVasInterval(s))
        }
    }

    @Test
    fun formatsArtifactWithOneBasedIndex() {
        assertEquals("vas1:50", formatVasArtifact(listOf(50)))
        assertEquals("vas1:0;vas2:100;vas3:72", formatVasArtifact(listOf(0, 100, 72)))
    }

    @Test
    fun clampsValuesIntoRange() {
        assertEquals("vas1:0;vas2:100", formatVasArtifact(listOf(-3, 140)))
    }

    @Test
    fun artifactHasNoCsvBreakingCharacters() {
        val s = formatVasArtifact(listOf(1, 2, 3))
        assertFalse(s.contains(',') || s.contains('\n') || s.contains('\r'))
        assertTrue(s.first() !in "=+-@")
    }

    @Test
    fun activeNeedsEnabledAndItems() {
        assertFalse(VasSettings().active)
        assertTrue(VasSettings(enabled = true).active)
        assertFalse(VasSettings(enabled = true, items = emptyList()).active)
        assertEquals(30 * 60_000L, VasSettings().intervalMs)
    }
}
