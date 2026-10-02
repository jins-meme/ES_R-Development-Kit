package com.jins_jp.meme.core.data

/**
 * 計測モード。[columns] は 1 サンプルの列（CSV の列名。ARTIFACT・NUM・DATE より後ろ）で、CSV のヘッダとグラフ画面へ渡す
 * 列はここだけを見る。値の並びは [DataParser] が作る [ParsedPacket.values] と同じ（列を足す・並べ替えるときは両方を直す）。
 */
enum class MemeMode(val display: String, val columns: List<String>) {
    Standard("Standard", listOf("ACC_X", "ACC_Y", "ACC_Z", "EOG_L1", "EOG_R1", "EOG_L2", "EOG_R2", "EOG_H1", "EOG_H2", "EOG_V1", "EOG_V2")),
    Full("Full", listOf("ACC_X", "ACC_Y", "ACC_Z", "GYRO_X", "GYRO_Y", "GYRO_Z", "EOG_L", "EOG_R", "EOG_H", "EOG_V")),
    Quaternion("Quaternion", listOf("QUATERNION_W", "QUATERNION_X", "QUATERNION_Y", "QUATERNION_Z"));

    /** グラフ画面とのやり取りで使う名前（webview/BRIDGE.md の mode） */
    val pageName: String get() = display.lowercase()

    /** グラフ画面に波形を出すか（Quaternion はグラフを持たない） */
    val hasGraph: Boolean get() = this != Quaternion

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
