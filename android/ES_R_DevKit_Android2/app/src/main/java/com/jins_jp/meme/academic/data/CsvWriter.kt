package com.jins_jp.meme.academic.data

import android.content.ContentValues
import android.content.Context
import android.net.Uri
import android.os.Environment
import android.provider.MediaStore
import java.io.OutputStreamWriter
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.TimeZone

/**
 * Writes CSV files into the public Downloads/ESR Logger directory via MediaStore.
 */
class CsvWriter(private val context: Context) {

    private var uri: Uri? = null
    private var pendingHeader: String? = null
    // 本体データCSVを遅延生成するためのファイル名。最初のデータ行が来た時に初めて
    // MediaStore へファイルを作成する。再生(再生モード)など 1 行もデータが来ない場合は
    // ファイル自体を作らないため、ヘッダーだけの空CSVが残らない。
    private var dataFileName: String? = null
    private val buffer: ArrayDeque<String> = ArrayDeque()
    private val flushThreshold = 100
    private var rowCount: Long = 0

    val recordedRows: Long get() = rowCount

    fun start(address: String, settings: MeasurementSettings) {
        val nameFmt = SimpleDateFormat("yyyyMMddHHmmss", Locale.getDefault()).apply {
            timeZone = TimeZone.getTimeZone("GMT")
        }
        val timestamp = nameFmt.format(Date())
        val safeAddress = address.replace(":", "")
        // MediaStore ファイルは即時生成せず、最初のデータ行が来た時に [flush] で
        // 遅延生成する。これにより 1 行もデータが来なければ空CSVは残らない。
        uri = null
        dataFileName = "${safeAddress}_$timestamp.csv"
        rowCount = 0
        buffer.clear()
        pendingHeader = buildHeader(settings)
    }

    fun writeRow(row: String) {
        buffer.addLast(row)
        if (buffer.size >= flushThreshold) flush()
    }

    /** 計測終了。書き出した本体データCSVの URI を返す(共有シートに渡すため)。 */
    fun stop(): Uri? {
        flush()
        val u = uri
        if (u != null) {
            val finish = ContentValues().apply {
                put(MediaStore.Downloads.IS_PENDING, 0)
            }
            runCatching { context.contentResolver.update(u, finish, null, null) }
        }
        uri = null
        pendingHeader = null
        dataFileName = null
        return u
    }

    /** 最初のデータ行が来た時に本体CSVを MediaStore へ遅延生成する。 */
    private fun createDataFile() {
        val name = dataFileName ?: return
        val values = ContentValues().apply {
            put(MediaStore.Downloads.DISPLAY_NAME, name)
            put(MediaStore.Downloads.MIME_TYPE, "text/csv")
            put(
                MediaStore.Downloads.RELATIVE_PATH,
                "${Environment.DIRECTORY_DOWNLOADS}/ESR Logger",
            )
            put(MediaStore.Downloads.IS_PENDING, 1)
        }
        uri = context.contentResolver.insert(
            MediaStore.Downloads.EXTERNAL_CONTENT_URI,
            values,
        )
    }

    private fun flush() {
        // データ行が一切無いときはファイルを作らない(ヘッダーだけのCSVを残さない)。
        if (buffer.isEmpty()) return
        if (uri == null) createDataFile()
        val u = uri ?: return
        val header = pendingHeader
        runCatching {
            context.contentResolver.openOutputStream(u, "wa")?.use { os ->
                OutputStreamWriter(os, Charsets.UTF_8).buffered().use { w ->
                    if (header != null) {
                        w.write(header)
                        w.write("\r\n")
                        pendingHeader = null
                    }
                    while (buffer.isNotEmpty()) {
                        w.write(buffer.removeFirst())
                        w.write("\r\n")
                        rowCount++
                    }
                }
            }
        }
    }

    private fun buildHeader(s: MeasurementSettings): String {
        val sb = StringBuilder()
        sb.append("// Data mode  : ${s.mode.display}").append("\r\n")
        sb.append("// Transmission speed  : ${s.quality.display}").append("\r\n")
        sb.append("// Acceleration sensor's range  : ${s.accRange.display}").append("\r\n")
        val gyroDisplay = if (s.mode == MemeMode.Quaternion) "2000dps" else s.gyroRange.display
        sb.append("// Gyroscope sensor's range  : $gyroDisplay").append("\r\n")
        sb.append(
            when (s.mode) {
                MemeMode.Standard ->
                    "//\r\n//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z," +
                            "EOG_L1,EOG_R1,EOG_L2,EOG_R2,EOG_H1,EOG_H2,EOG_V1,EOG_V2"
                MemeMode.Full ->
                    "//\r\n//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z," +
                            "GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V"
                MemeMode.Quaternion ->
                    "//\r\n//ARTIFACT,NUM,DATE," +
                            "QUATERNION_W,QUATERNION_X,QUATERNION_Y,QUATERNION_Z"
            }
        )
        return sb.toString()
    }
}

/** Format a single CSV row matching the original Java sources. */
fun formatRow(
    isMarking: Boolean,
    totalCount: Long,
    timeMillisGmt: Long,
    values: IntArray,
): String {
    val df = SimpleDateFormat("yyyy/MM/dd HH:mm:ss.SSS", Locale.getDefault()).apply {
        timeZone = TimeZone.getTimeZone("GMT")
    }
    val sb = StringBuilder()
    sb.append(if (isMarking) "X" else "").append(",")
    sb.append(totalCount).append(",")
    sb.append(df.format(Date(timeMillisGmt)))
    for (v in values) {
        sb.append(",").append(v)
    }
    return sb.toString()
}