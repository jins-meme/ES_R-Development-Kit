package com.jins_jp.meme.academic.data

import java.io.BufferedReader
import java.io.InputStream
import java.io.InputStreamReader

/** Parsed contents of a CSV exported by this app, used to drive the mock engine. */
data class MockCsvData(
    val settings: MeasurementSettings,
    val rows: List<IntArray>,
)

/** Thrown when a selected file is not a valid logger CSV. */
class MockCsvFormatException(message: String) : Exception(message)

/**
 * Parses a CSV previously written by [CsvWriter]. The `//` comment header carries
 * the measurement settings; the remaining lines are data rows whose value columns
 * (everything after ARTIFACT,NUM,DATE) are kept verbatim so [com.jins_jp.meme.academic.ble.MockMemeBleEngine]
 * can re-encode them into BLE packets.
 */
object MockCsvLoader {

    /** Number of value columns CsvWriter emits for each mode. */
    private fun expectedValueColumns(mode: MemeMode): Int = when (mode) {
        MemeMode.Standard -> 11   // ACC x3 + EOG_L1/R1/L2/R2 + EOG_H1/H2/V1/V2
        MemeMode.Full -> 10       // ACC x3 + GYRO x3 + EOG_L/R + EOG_H/V
        MemeMode.Quaternion -> 4  // QUATERNION W/X/Y/Z
    }

    @Throws(MockCsvFormatException::class)
    fun parse(input: InputStream): MockCsvData {
        var mode: MemeMode? = null
        var quality: MemeQuality? = null
        var accRange: AccRange? = null
        var gyroRange: GyroRange? = null
        val rows = ArrayList<IntArray>()

        BufferedReader(InputStreamReader(input, Charsets.UTF_8)).use { reader ->
            reader.forEachLine { raw ->
                val line = raw.trim()
                if (line.isEmpty()) return@forEachLine
                if (line.startsWith("//")) {
                    // Metadata lines look like "// Data mode  : Standard". The column
                    // header line ("//ARTIFACT,NUM,...") and the bare "//" have no colon.
                    val colon = line.indexOf(':')
                    if (colon < 0) return@forEachLine
                    val key = line.substring(2, colon).trim().lowercase()
                    val value = line.substring(colon + 1).trim()
                    when {
                        key.startsWith("data mode") -> mode = parseMode(value)
                        key.startsWith("transmission speed") -> quality = parseQuality(value)
                        key.startsWith("acceleration") -> accRange = parseAcc(value)
                        key.startsWith("gyroscope") -> gyroRange = parseGyro(value)
                    }
                    return@forEachLine
                }
                // Data row: ARTIFACT,NUM,DATE,v0,v1,...
                val parts = line.split(',')
                if (parts.size <= 3) return@forEachLine
                val values = IntArray(parts.size - 3) { i ->
                    parts[i + 3].trim().toIntOrNull()
                        ?: throw MockCsvFormatException("数値に変換できない列があります: \"${parts[i + 3]}\"")
                }
                rows.add(values)
            }
        }

        val m = mode ?: throw MockCsvFormatException("ヘッダに \"Data mode\" がありません。本アプリが出力したCSVを選んでください。")
        if (rows.isEmpty()) throw MockCsvFormatException("データ行が見つかりませんでした。")
        val expected = expectedValueColumns(m)
        val bad = rows.firstOrNull { it.size < expected }
        if (bad != null) {
            throw MockCsvFormatException("${m.display} モードは $expected 列必要ですが、${bad.size} 列の行があります。")
        }
        return MockCsvData(
            settings = MeasurementSettings(
                mode = m,
                quality = quality ?: MemeQuality.Hz100,
                accRange = accRange ?: AccRange.G2,
                gyroRange = gyroRange ?: GyroRange.Dps250,
            ),
            rows = rows,
        )
    }

    private fun parseMode(v: String): MemeMode? =
        MemeMode.entries.firstOrNull { it.display.equals(v, ignoreCase = true) }

    private fun parseQuality(v: String): MemeQuality? =
        MemeQuality.entries.firstOrNull { it.display.equals(v, ignoreCase = true) }

    private fun parseAcc(v: String): AccRange? =
        AccRange.entries.firstOrNull { it.display.equals(v, ignoreCase = true) }

    private fun parseGyro(v: String): GyroRange? =
        GyroRange.entries.firstOrNull { it.display.equals(v, ignoreCase = true) }
}
