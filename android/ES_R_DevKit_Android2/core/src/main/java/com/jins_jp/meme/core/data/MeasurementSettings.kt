package com.jins_jp.meme.core.data

enum class MemeMode(val display: String) {
    Standard("Standard"),
    Full("Full"),
    Quaternion("Quaternion");

    companion object {
        fun fromIndex(i: Int) = entries.getOrElse(i) { Standard }
    }
}

enum class MemeQuality(val display: String, val hz: Int) {
    Hz100("100Hz", 100),
    Hz50("50Hz", 50);

    companion object {
        fun fromIndex(i: Int) = entries.getOrElse(i) { Hz100 }
    }
}

enum class AccRange(val display: String, val g: Int) {
    G2("2g", 2),
    G4("4g", 4),
    G8("8g", 8),
    G16("16g", 16);

    companion object {
        fun fromIndex(i: Int) = entries.getOrElse(i) { G2 }
    }
}

enum class GyroRange(val display: String, val dps: Int) {
    Dps250("250dps", 250),
    Dps500("500dps", 500),
    Dps1000("1000dps", 1000),
    Dps2000("2000dps", 2000);

    companion object {
        fun fromIndex(i: Int) = entries.getOrElse(i) { Dps250 }
    }
}

data class MeasurementSettings(
    val mode: MemeMode = MemeMode.Standard,
    val quality: MemeQuality = MemeQuality.Hz100,
    val accRange: AccRange = AccRange.G2,
    val gyroRange: GyroRange = GyroRange.Dps250,
)
