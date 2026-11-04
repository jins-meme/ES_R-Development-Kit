package com.jins_jp.meme.core.data

import android.content.Context
import org.json.JSONArray

class SettingsStore(context: Context) {
    private val prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    fun load(): MeasurementSettings = MeasurementSettings(
        mode = MemeMode.fromIndex(prefs.getInt(KEY_MODE, MemeMode.Standard.ordinal)),
        quality = MemeQuality.fromIndex(prefs.getInt(KEY_QUALITY, MemeQuality.Hz100.ordinal)),
        accRange = AccRange.fromIndex(prefs.getInt(KEY_ACC, AccRange.G2.ordinal)),
        gyroRange = GyroRange.fromIndex(prefs.getInt(KEY_GYRO, GyroRange.Dps250.ordinal)),
    )

    fun save(s: MeasurementSettings) {
        prefs.edit()
            .putInt(KEY_MODE, s.mode.ordinal)
            .putInt(KEY_QUALITY, s.quality.ordinal)
            .putInt(KEY_ACC, s.accRange.ordinal)
            .putInt(KEY_GYRO, s.gyroRange.ordinal)
            .apply()
    }

    fun loadAutoConnect(): Boolean = prefs.getBoolean(KEY_AUTO_CONNECT, false)

    fun saveAutoConnect(enabled: Boolean) {
        prefs.edit().putBoolean(KEY_AUTO_CONNECT, enabled).apply()
    }

    // 既定 ON。OFF だと計測中に BLE が一度切れただけで切断ハンドラが計測を畳んで
    // しまい、長時間計測がそこで終わる（実際に数時間の計測が何度も途切れた）。
    // ON なら同じ切断で再スキャン→再接続し、新しいCSVで計測を続ける。
    fun loadReconnectEnabled(): Boolean = prefs.getBoolean(KEY_RECONNECT, true)

    fun saveReconnectEnabled(enabled: Boolean) {
        prefs.edit().putBoolean(KEY_RECONNECT, enabled).apply()
    }

    fun loadOpenSharingOnComplete(): Boolean = prefs.getBoolean(KEY_OPEN_SHARING, false)

    fun saveOpenSharingOnComplete(enabled: Boolean) {
        prefs.edit().putBoolean(KEY_OPEN_SHARING, enabled).apply()
    }

    // 既定 ON。本体データCSVを gz 圧縮して "<base>.csv.gz" で保存する（実測で 1/2.9）。
    // OFF にすると従来どおり非圧縮の "<base>.csv"。再生はどちらも開ける
    // （[decompressIfGzip] が中身で見分けるため）。
    fun loadGzipCompression(): Boolean = prefs.getBoolean(KEY_GZIP_COMPRESSION, true)

    fun saveGzipCompression(enabled: Boolean) {
        prefs.edit().putBoolean(KEY_GZIP_COMPRESSION, enabled).apply()
    }

    // 既定 OFF。ON にすると計測中 1 分に 1 回測位し、前回記録した地点から 50m 以上
    // 動いていた時だけ大まかな現在地を ARTIFACT 列へ残す（[LocationSampler]）。
    // 位置は要る人だけが要る情報で、常時オンにするものではないので既定は OFF。
    fun loadLocationLogging(): Boolean = prefs.getBoolean(KEY_LOCATION_LOGGING, false)

    fun saveLocationLogging(enabled: Boolean) {
        prefs.edit().putBoolean(KEY_LOCATION_LOGGING, enabled).apply()
    }

    // 既定 OFF。設定「Record VAS」。間隔(分)と項目のメモ(CSV には書かない)も一緒に残す。
    // 項目はメモの JSON 配列で持つ（項目の数 = 配列の長さ）。
    fun loadVas(): VasSettings {
        val interval = prefs.getInt(KEY_VAS_INTERVAL, VAS_INTERVAL_DEFAULT_MIN)
            .takeIf { it in VAS_INTERVAL_MIN_MIN..VAS_INTERVAL_MAX_MIN } ?: VAS_INTERVAL_DEFAULT_MIN
        val items = prefs.getString(KEY_VAS_ITEMS, null)?.let { json ->
            runCatching {
                val a = JSONArray(json)
                List(a.length()) { VasItem(a.optString(it, "")) }
            }.getOrNull()
        }?.takeIf { it.isNotEmpty() } ?: listOf(VasItem())
        return VasSettings(
            enabled = prefs.getBoolean(KEY_VAS_ENABLED, false),
            intervalMinutes = interval,
            items = items,
        )
    }

    fun saveVas(s: VasSettings) {
        prefs.edit()
            .putBoolean(KEY_VAS_ENABLED, s.enabled)
            .putInt(KEY_VAS_INTERVAL, s.intervalMinutes)
            .putString(KEY_VAS_ITEMS, JSONArray(s.items.map { it.memo }).toString())
            .apply()
    }

    private companion object {
        const val PREFS_NAME = "measurement_settings"
        const val KEY_MODE = "mode"
        const val KEY_QUALITY = "quality"
        const val KEY_ACC = "acc_range"
        const val KEY_GYRO = "gyro_range"
        const val KEY_AUTO_CONNECT = "auto_connect"
        const val KEY_RECONNECT = "reconnect_enabled"
        const val KEY_OPEN_SHARING = "open_sharing_on_complete"
        const val KEY_LOCATION_LOGGING = "location_logging"
        const val KEY_GZIP_COMPRESSION = "gzip_compression"
        const val KEY_VAS_ENABLED = "vas_enabled"
        const val KEY_VAS_INTERVAL = "vas_interval_min"
        const val KEY_VAS_ITEMS = "vas_items"
    }
}