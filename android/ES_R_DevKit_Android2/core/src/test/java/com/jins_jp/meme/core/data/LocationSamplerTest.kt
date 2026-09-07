package com.jins_jp.meme.core.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.util.Locale

/**
 * ARTIFACT 列へ書く位置文字列の整形（[formatLocationArtifact]）と、記録するかどうかの
 * 判定（[movedAtLeast]）。前者は CSV へそのまま載るので区切り・桁・無効値の扱いを、
 * 後者は「止まっている間は書かない／50m 動いたら書く」の境目を固定する。
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

    /** セッション最初の測位（前回地点なし）は必ず記録する。 */
    @Test
    fun firstFixIsAlwaysRecorded() {
        assertTrue(movedAtLeast(null, LocationFix(35.6802, 139.7521), 50.0))
    }

    /** 同じ場所に留まっている間は記録しない（毎分同じ座標を書かない）。 */
    @Test
    fun stayingPutDoesNotPassThreshold() {
        val prev = LocationFix(35.6802, 139.7521)
        // 緯度 0.0001 度 ≒ 11m、経度 0.0001 度 ≒ 9m（東京）。どちらも 50m 未満。
        assertFalse(movedAtLeast(prev, LocationFix(35.6803, 139.7522), 50.0))
    }

    /** 緯度方向だけで 50m 以上動いたら記録する（0.0005 度 ≒ 56m）。 */
    @Test
    fun northSouthMoveAlonePassesThreshold() {
        val prev = LocationFix(35.6802, 139.7521)
        assertTrue(movedAtLeast(prev, LocationFix(35.6807, 139.7521), 50.0))
    }

    /**
     * 経度方向だけで 50m 以上動いたら記録する。経度は緯度で縮むので、東京
     * (cos35.68 ≒ 0.81)では 1 度 ≒ 90.4km ＝ 50m には 0.00055 度 以上が要る。
     */
    @Test
    fun eastWestMoveAlonePassesThreshold() {
        val prev = LocationFix(35.6802, 139.7521)
        assertFalse(movedAtLeast(prev, LocationFix(35.6802, 139.7526), 50.0))
        assertTrue(movedAtLeast(prev, LocationFix(35.6802, 139.7528), 50.0))
    }

    /** 日付変更線をまたぐ差は短い側で測る（360 度ぶん動いたことにしない）。 */
    @Test
    fun antimeridianUsesShorterSide() {
        // 179.999 → -179.999 は東西 0.002 度 ≒ 223m。
        assertTrue(movedAtLeast(LocationFix(0.0, 179.999), LocationFix(0.0, -179.999), 50.0))
        // 179.9999 → -179.9999 は 0.0002 度 ≒ 22m。またいでも動いていない。
        assertFalse(movedAtLeast(LocationFix(0.0, 179.9999), LocationFix(0.0, -179.9999), 50.0))
    }

    @Test
    fun rejectsOutOfRangeAndNonFiniteValues() {
        assertNull(formatLocationArtifact(91.0, 0.0))
        assertNull(formatLocationArtifact(0.0, 181.0))
        assertNull(formatLocationArtifact(Double.NaN, 0.0))
        assertNull(formatLocationArtifact(0.0, Double.POSITIVE_INFINITY))
    }
}
