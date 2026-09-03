package com.jins_jp.meme.core.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import java.util.Locale

/**
 * ARTIFACT 列へ書く位置文字列の整形（[formatLocationArtifact]）。CSV へそのまま
 * 載るので、区切り・桁・無効値の扱いを固定する。
 */
class LocationSamplerTest {

    @Test
    fun formatsWithPrefixAndFourDecimals() {
        assertEquals("lc:35.6802_139.7521", formatLocationArtifact(35.68024, 139.75209))
    }

    @Test
    fun formatsNegativeCoordinates() {
        assertEquals("lc:-33.8688_-70.6693", formatLocationArtifact(-33.8688, -70.6693))
    }

    /**
     * 小数点がカンマになるロケール（de-DE 等）でも CSV を壊さないこと。既定ロケールに
     * 引きずられると "lc:35,6802_139,7521" になり列がずれる。
     */
    @Test
    fun usesDotAsDecimalSeparatorRegardlessOfDefaultLocale() {
        val original = Locale.getDefault()
        try {
            Locale.setDefault(Locale.GERMANY)
            assertEquals("lc:35.6802_139.7521", formatLocationArtifact(35.68024, 139.75209))
        } finally {
            Locale.setDefault(original)
        }
    }

    @Test
    fun rejectsOutOfRangeAndNonFiniteValues() {
        assertNull(formatLocationArtifact(91.0, 0.0))
        assertNull(formatLocationArtifact(0.0, 181.0))
        assertNull(formatLocationArtifact(Double.NaN, 0.0))
        assertNull(formatLocationArtifact(0.0, Double.POSITIVE_INFINITY))
    }
}
