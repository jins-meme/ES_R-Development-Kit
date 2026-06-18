package com.jins_jp.meme.academic.data

import android.content.Context

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

    fun loadMockEnabled(): Boolean = prefs.getBoolean(KEY_MOCK, false)

    fun saveMockEnabled(enabled: Boolean) {
        prefs.edit().putBoolean(KEY_MOCK, enabled).apply()
    }

    fun loadReconnectEnabled(): Boolean = prefs.getBoolean(KEY_RECONNECT, false)

    fun saveReconnectEnabled(enabled: Boolean) {
        prefs.edit().putBoolean(KEY_RECONNECT, enabled).apply()
    }

    private companion object {
        const val PREFS_NAME = "measurement_settings"
        const val KEY_MODE = "mode"
        const val KEY_QUALITY = "quality"
        const val KEY_ACC = "acc_range"
        const val KEY_GYRO = "gyro_range"
        const val KEY_MOCK = "mock_enabled"
        const val KEY_RECONNECT = "reconnect_enabled"
    }
}