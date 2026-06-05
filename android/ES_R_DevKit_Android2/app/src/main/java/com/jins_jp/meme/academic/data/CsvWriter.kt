package com.jins_jp.meme.academic.data

import android.content.Context
import android.os.Environment
import java.io.File
import java.io.OutputStreamWriter
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.TimeZone

/**
 * Writes CSV files into the app-private Downloads directory (Scoped Storage, no permission needed).
 */
class CsvWriter(private val context: Context) {

    private var file: File? = null
    private var pendingHeader: String? = null
    private val buffer: ArrayDeque<String> = ArrayDeque()
    private val flushThreshold = 100
    private var rowCount: Long = 0

    val recordedRows: Long get() = rowCount

    fun start(address: String, settings: MeasurementSettings) {
        val base = context.getExternalFilesDir(Environment.DIRECTORY_DOWNLOADS) ?: return
        val dir = File(base, "JINS/MEME academic")
        if (!dir.exists()) dir.mkdirs()

        val nameFmt = SimpleDateFormat("yyyyMMddHHmmss", Locale.getDefault()).apply {
            timeZone = TimeZone.getTimeZone("GMT")
        }
        val timestamp = nameFmt.format(Date())
        val safeAddress = address.replace(":", "")
        file = File(dir, "${safeAddress}_$timestamp.csv")
        rowCount = 0
        buffer.clear()
        pendingHeader = buildHeader(settings)
    }

    fun writeRow(row: String) {
        buffer.addLast(row)
        if (buffer.size >= flushThreshold) flush()
    }

    fun stop() {
        flush()
        file = null
        pendingHeader = null
    }

    private fun flush() {
        val target = file ?: return
        if (buffer.isEmpty() && pendingHeader == null) return
        val header = pendingHeader
        if (header != null) {
            buffer.addFirst(header)
            pendingHeader = null
        }
        runCatching {
            target.outputStream().use { fos ->
                // append mode if file exists already
                if (target.length() > 0) {
                    java.io.FileOutputStream(target, true).bufferedWriter(Charsets.UTF_8).use { w ->
                        while (buffer.isNotEmpty()) {
                            w.write(buffer.removeFirst())
                            w.write("\r\n")
                            rowCount++
                        }
                    }
                } else {
                    OutputStreamWriter(fos, Charsets.UTF_8).buffered().use { w ->
                        while (buffer.isNotEmpty()) {
                            w.write(buffer.removeFirst())
                            w.write("\r\n")
                            rowCount++
                        }
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
    val df = SimpleDateFormat("yyyy/MM/dd HH:mm:ss.SS", Locale.getDefault()).apply {
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
