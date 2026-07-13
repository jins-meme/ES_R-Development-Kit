package com.jins_jp.meme.core.data

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

/** URIs of the main data CSV and classification CSV finalized by [CsvWriter.stop]. */
data class CsvStopResult(val dataUri: Uri?, val classificationUri: Uri?)

/**
 * Writes CSV files into the public Downloads/ESR Logger directory via MediaStore.
 */
class CsvWriter(private val context: Context) {

    private var uri: Uri? = null
    private var pendingHeader: String? = null
    // 本体データCSVを遅延生成するためのベース名。最初のデータ行が来た時に初めて
    // MediaStore へファイルを作成する。再生(再生モード)など 1 行もデータが来ない場合は
    // ファイル自体を作らないため、ヘッダーだけの空CSVが残らない。
    private var dataBaseName: String? = null
    private val buffer: ArrayDeque<String> = ArrayDeque()
    private val flushThreshold = 100
    private var rowCount: Long = 0

    // 行動分類(測定状態)を 1 秒ごとに書き出すサイドカー("<base>_classification.csv")。
    // 本体CSVと同じベース名(=MACアドレス_日時)を共有し、NUM で本体データ行へ対応づける。
    // 1 秒に 1 行と疎なので、最初の行が来た時に遅延生成し、各行を即フラッシュする。
    private var classificationUri: Uri? = null
    private var classificationBaseName: String? = null

    val recordedRows: Long get() = rowCount

    fun start(address: String, settings: MeasurementSettings) {
        val base = makeBaseName(address)
        // MediaStore ファイルは即時生成せず、最初のデータ行が来た時に [flush] で
        // 遅延生成する。これにより 1 行もデータが来なければ空CSVは残らない。
        uri = null
        dataBaseName = base
        rowCount = 0
        buffer.clear()
        pendingHeader = buildHeader(settings)
        classificationBaseName = base
        classificationUri = null
    }

    /**
     * Mock 再生時など本体データCSVを書かない場合に、分類CSVサイドカーのみを
     * 有効化する。本体ファイルは作らず、サイドカーのベース名だけを用意する。
     */
    fun startClassificationOnly(address: String) {
        // 本体データCSVは一切作らない。念のため前回計測のデータ用状態も破棄する。
        uri = null
        dataBaseName = null
        pendingHeader = null
        buffer.clear()
        rowCount = 0
        classificationBaseName = makeBaseName(address)
        classificationUri = null
    }

    private fun makeBaseName(address: String): String {
        val nameFmt = SimpleDateFormat("yyyyMMddHHmmss", Locale.getDefault()).apply {
            timeZone = TimeZone.getTimeZone("GMT")
        }
        val timestamp = nameFmt.format(Date())
        val safeAddress = address.replace(":", "")
        return "${safeAddress}_$timestamp"
    }

    fun writeRow(row: String) {
        buffer.addLast(row)
        if (buffer.size >= flushThreshold) flush()
    }

    /**
     * Appends one behavior-classification row (DATE,LABEL) to a sidecar file
     * "<base>_classification.csv" next to the main CSV. Called once per second as
     * the status detector commits a 1-second segment (plus a final partial segment
     * on stop). [dateGmtMillis] is the GMT wall-clock the label maps to (already
     * delay-compensated by the caller); it is formatted the same way as the main
     * CSV's DATE column so the two files line up on time. The file is created
     * lazily on the first row. Rows are sparse, so each is flushed immediately
     * so a crash never loses earlier rows and the main CSV is never rewritten.
     */
    fun writeClassification(dateGmtMillis: Long, label: String) {
        val base = classificationBaseName ?: return
        val isNew = classificationUri == null
        if (isNew) {
            val values = ContentValues().apply {
                put(MediaStore.Downloads.DISPLAY_NAME, "${base}_classification.csv")
                put(MediaStore.Downloads.MIME_TYPE, "text/csv")
                put(
                    MediaStore.Downloads.RELATIVE_PATH,
                    "${Environment.DIRECTORY_DOWNLOADS}/ESR Logger",
                )
            }
            classificationUri = context.contentResolver.insert(
                MediaStore.Downloads.EXTERNAL_CONTENT_URI,
                values,
            )
        }
        val u = classificationUri ?: return
        val df = SimpleDateFormat("yyyy/MM/dd HH:mm:ss.SSS", Locale.getDefault()).apply {
            timeZone = TimeZone.getTimeZone("GMT")
        }
        runCatching {
            context.contentResolver.openOutputStream(u, "wa")?.use { os ->
                OutputStreamWriter(os, Charsets.UTF_8).buffered().use { w ->
                    if (isNew) {
                        w.write("// Behavior classification for $base.csv"); w.write("\r\n")
                        w.write("// DATE,LABEL"); w.write("\r\n")
                    }
                    w.write("${df.format(Date(dateGmtMillis))},$label"); w.write("\r\n")
                }
            }
        }
    }

    /**
     * 計測終了。残りの本体データをファイルへ書き出し、このセッションで生成された
     * 本体データCSVと分類CSVの URI を返す(共有シートに渡すため)。ファイルは IS_PENDING
     * を付けず即公開しているので、計測中の各 flush 追記がそのまま最終ファイルとなり、
     * 終了時の finalize は不要。
     */
    fun stop(): CsvStopResult {
        flush()
        val result = CsvStopResult(dataUri = uri, classificationUri = classificationUri)
        uri = null
        pendingHeader = null
        dataBaseName = null

        classificationUri = null
        classificationBaseName = null
        return result
    }

    /**
     * 最初のデータ行が来た時に本体CSVを MediaStore へ遅延生成する。
     * IS_PENDING は付けず即公開する。こうすると 100 件ごとの [flush] 追記が計測中に
     * その都度ファイルへ反映され、Downloads で更新され続ける様子が見える。IS_PENDING=1
     * のままだと計測終了([stop] で公開)まで隠れ、終了時に一括で現れてしまうため。
     */
    private fun createDataFile() {
        val base = dataBaseName ?: return
        val values = ContentValues().apply {
            put(MediaStore.Downloads.DISPLAY_NAME, "$base.csv")
            put(MediaStore.Downloads.MIME_TYPE, "text/csv")
            put(
                MediaStore.Downloads.RELATIVE_PATH,
                "${Environment.DIRECTORY_DOWNLOADS}/ESR Logger",
            )
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