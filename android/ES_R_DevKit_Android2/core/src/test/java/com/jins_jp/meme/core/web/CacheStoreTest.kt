package com.jins_jp.meme.core.web

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** 日別キャッシュの名前の検査(BRIDGE.md の Cache files: device = 12 桁の大文字 16 進、name = YYYY-MM-DD.json) */
class CacheStoreTest {

    @Test fun deviceMustBe12UpperHex() {
        assertTrue(CacheStore.validDevice("D3FE9978A82E"))
        assertFalse(CacheStore.validDevice("d3fe9978a82e"))
        assertFalse(CacheStore.validDevice("D3:FE:99:78:A8:2E"))
        assertFalse(CacheStore.validDevice("../D3FE9978A8"))
        assertFalse(CacheStore.validDevice(""))
    }

    @Test fun nameMustBeADayFile() {
        assertTrue(CacheStore.validName("2026-10-15.json"))
        assertFalse(CacheStore.validName("2026-10-15.json.tmp"))
        assertFalse(CacheStore.validName("../2026-10-15.json"))
        assertFalse(CacheStore.validName("2026-10-15 (1).json"))
        assertFalse(CacheStore.validName("2026-10-15.json/x"))
    }

    @Test fun deviceFromAddressAndFileName() {
        assertEquals("D3FE9978A82E", CacheStore.deviceOf("d3:fe:99:78:a8:2e"))
        assertEquals("D3FE9978A82E", CacheStore.deviceOfFileName("D3FE9978A82E_20261014233633.csv.gz"))
        assertEquals("D3FE9978A82E", CacheStore.deviceOfFileName("d3fe9978a82e_20261014233633_w-workday.csv"))
        assertNull(CacheStore.deviceOfFileName("SIMULATED_20261014233633.csv"))
    }
}
