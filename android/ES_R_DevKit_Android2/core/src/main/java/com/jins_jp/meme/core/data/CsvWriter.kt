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
import java.util.zip.GZIPOutputStream

/** URI of the main data CSV finalized by [CsvWriter.stop]（1 行も書いていなければ null）. */
data class CsvStopResult(val dataUri: Uri?)

/**
 * Writes CSV files into the public Downloads/ESR Logger directory via MediaStore.
 *
 * 本体データは設定に応じて "<base>.csv.gz"(既定) か "<base>.csv" へ書く
 * （[start] の compress 引数、[dataFileName]）。切断ログのサイドカーは
 * 1 行ずつ追記する疎なファイルで圧縮しても効果が無いため、設定によらず常に .csv。
 */
class CsvWriter(private val context: Context) {

    private var uri: Uri? = null
    private var pendingHeader: String? = null
    // このセッションの本体データを gz 圧縮するか。[start] で確定し、計測中は変えない。
    private var compressData: Boolean = true
    // 本体データCSVを遅延生成するためのベース名。最初のデータ行が来た時に初めて
    // MediaStore へファイルを作成する。再生(再生モード)など 1 行もデータが来ない場合は
    // ファイル自体を作らないため、ヘッダーだけの空CSVが残らない。
    private var dataBaseName: String? = null
    private val buffer: ArrayDeque<String> = ArrayDeque()
    private val flushThreshold = 100
    private var rowCount: Long = 0

    // 切断ログのサイドカー("<base>_disconnect.csv")。本体データCSVと同じベース名を
    // 共有し、実機計測セッションでのみ [writeDisconnect] が遅延生成する。
    private var disconnectUri: Uri? = null

    val recordedRows: Long get() = rowCount

    /** 今のセッションのベース名（"<アドレス>_<GMT の日時>"。[start] から [stop] まで）。判定器の表の CSV が同じベース名を使う */
    val baseName: String? get() = dataBaseName

    /** 今のセッションの本体データCSVを gz 圧縮するか（[start] で確定） */
    val compressed: Boolean get() = compressData

    /**
     * 計測セッションを開始する。[compress] は設定「保存時に gz 圧縮する」(既定 ON)で、
     * このセッションのファイル 1 つ分の形式をここで確定させる。途中で設定を変えても
     * 書きかけのファイルの形式は変わらない（1 ファイル内で形式が混ざらないように）。
     */
    fun start(address: String, settings: MeasurementSettings, compress: Boolean) {
        val base = makeBaseName(address)
        compressData = compress
        // MediaStore ファイルは即時生成せず、最初のデータ行が来た時に [flush] で
        // 遅延生成する。これにより 1 行もデータが来なければ空CSVは残らない。
        uri = null
        dataBaseName = base
        rowCount = 0
        buffer.clear()
        pendingHeader = buildHeader(settings)
        disconnectUri = null
    }

    private fun makeBaseName(address: String): String {
        val timestamp = gmtFormat("yyyyMMddHHmmss").format(Date())
        val safeAddress = address.replace(":", "")
        return "${safeAddress}_$timestamp"
    }

    fun writeRow(row: String) {
        buffer.addLast(row)
        if (buffer.size >= flushThreshold) flush()
    }

    /**
     * 切断の時刻と理由を "<base>_disconnect.csv" へ 1 行追記する。長時間計測が
     * 途中で止まった時に「メガネ側が落ちた(電池切れ・電源断)」のか「電波が
     * 切れた」のかを後から切り分けるための計装で、[stop] がベース名を畳む前
     * ＝切断検知時の [stop] 直前に呼ぶ。[timeGmtMillis] は本体CSVの DATE 列と
     * 同じ GMT 壁時計、[status] は GATT の切断ステータス、[reason] はその名前、
     * [battery] は切断直前に受けたパケットの電池残量（0〜5、未受信なら -1）。
     * 切断の直前に残量が 0〜1 なら電池切れ、と見分けるために残す。
     *
     * ここで本体データを [flush] して切断時点まで確定させ、1 行も受信していない
     * セッションではサイドカーも作らない（本体CSVの無い孤児ファイルを残さない）。
     */
    fun writeDisconnect(timeGmtMillis: Long, status: Int, reason: String, battery: Int) {
        val base = dataBaseName ?: return
        flush()
        // flush 後も本体CSVが無い＝データ 0 行のセッション。記録する対象がない。
        if (uri == null) return
        val isNew = disconnectUri == null
        if (isNew) disconnectUri = createDownload("${base}_disconnect$CSV_EXTENSION", CSV_MIME)
        val u = disconnectUri ?: return
        runCatching {
            context.contentResolver.openOutputStream(u, "wa")?.use { os ->
                OutputStreamWriter(os, Charsets.UTF_8).buffered().use { w ->
                    if (isNew) {
                        w.write("// Disconnect log for ${dataFileName(base, compressData)}")
                        w.write("\r\n")
                        w.write("// DATE,STATUS,REASON,BATTERY"); w.write("\r\n")
                    }
                    w.write("${formatGmtDate(timeGmtMillis)},$status,$reason,$battery"); w.write("\r\n")
                }
            }
        }
    }

    /**
     * 計測終了。残りの本体データをファイルへ書き出し、このセッションで生成された
     * 本体データCSVの URI を返す(共有シートに渡すため)。ファイルは IS_PENDING
     * を付けず即公開しているので、計測中の各 flush 追記がそのまま最終ファイルとなり、
     * 終了時の finalize は不要。
     */
    fun stop(): CsvStopResult {
        flush()
        val result = CsvStopResult(dataUri = uri)
        uri = null
        pendingHeader = null
        dataBaseName = null
        disconnectUri = null
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
        uri = createDownload(dataFileName(base, compressData), dataFileMime(compressData))
    }

    private fun createDownload(name: String, mime: String): Uri? = createEsrLoggerFile(context, name, mime)

    /**
     * 溜まった行を追記する。[compressData] が true なら gz 圧縮する。
     *
     * 圧縮時は**1 回の flush につき独立した gzip メンバを 1 つ**書き、その都度
     * 閉じきる。gzip は連結されたメンバを 1 本のストリームとして読める(RFC 1952)
     * ので、できあがりは `gunzip`・`GZIPInputStream`・Python の `gzip` のいずれでも
     * 普通に開ける 1 つのファイルになる。
     *
     * ストリームを 1 本開きっぱなしにする方が圧縮率は上がるが、それだと計測中に
     * プロセスが落ちた時にトレーラが書かれず全体が展開できなくなる。ここは
     * 100 行ごとの追記がそのまま最終ファイルになる（＝落ちてもそこまでは読める）
     * という非圧縮時からの設計を守る方を採る。メンバ 1 つあたりの固定
     * オーバーヘッドは 18 byte で、100 行(約 6KB)ごとなら誤差の範囲。
     */
    private fun flush() {
        // データ行が一切無いときはファイルを作らない(ヘッダーだけのCSVを残さない)。
        if (buffer.isEmpty()) return
        if (uri == null) createDataFile()
        val u = uri ?: return
        val header = pendingHeader
        runCatching {
            context.contentResolver.openOutputStream(u, "wa")?.use { os ->
                // 圧縮時は GZIPOutputStream の close() が deflate の finish と
                // トレーラまで書き切る（下の Writer の close から連鎖する）。
                val sink = if (compressData) GZIPOutputStream(os) else os
                OutputStreamWriter(sink, Charsets.UTF_8).buffered().use { w ->
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
        sb.append("//\r\n//").append((listOf("ARTIFACT", "NUM", "DATE") + s.mode.columns).joinToString(","))
        return sb.toString()
    }
}

/** Downloads/ESR Logger に [name] のファイルを作る（IS_PENDING は付けず、すぐ見えるようにする）。判定器の表の CSV も使う */
fun createEsrLoggerFile(context: Context, name: String, mime: String): Uri? {
    val values = ContentValues().apply {
        put(MediaStore.Downloads.DISPLAY_NAME, name)
        put(MediaStore.Downloads.MIME_TYPE, mime)
        put(MediaStore.Downloads.RELATIVE_PATH, "${Environment.DIRECTORY_DOWNLOADS}/ESR Logger")
    }
    return context.contentResolver.insert(MediaStore.Downloads.EXTERNAL_CONTENT_URI, values)
}

/**
 * Format a single CSV row matching the original Java sources.
 *
 * [artifact] は先頭の ARTIFACT 列にそのまま入る文字列（通常は空。計測中に確定した
 * 位置情報 "lc:35.6802_139.7521" をその行へ載せる時だけ非空になる）。タップラベルと
 * Free Marking は受信時には分からないので、停止時に [LabelMerger] が同じ列へ統合する。
 */
fun formatRow(
    artifact: String,
    totalCount: Long,
    timeMillisGmt: Long,
    values: IntArray,
): String {
    val sb = StringBuilder()
    sb.append(artifact).append(",")
    sb.append(totalCount).append(",")
    sb.append(formatGmtDate(timeMillisGmt))
    for (v in values) {
        sb.append(",").append(v)
    }
    return sb.toString()
}

/**
 * GMT の日時の書式（ファイル名・DATE 列）。数字が端末の言語で変わらないよう [Locale.US] で作る
 * （既定のロケールだと、アラビア語・ペルシア語などの端末で ASCII 以外の数字になり、CSV が読めなくなる）。
 */
private fun gmtFormat(pattern: String) = SimpleDateFormat(pattern, Locale.US).apply {
    timeZone = TimeZone.getTimeZone("GMT")
}

// DATE 列は 1 行ごとに書くので書式を使い回す（SimpleDateFormat はスレッド安全でないのでスレッドごとに持つ）。
private val gmtDateFormat = ThreadLocal.withInitial { gmtFormat("yyyy/MM/dd HH:mm:ss.SSS") }

/** DATE 列の書式（GMT・ミリ秒まで。例 2026/10/02 10:24:23.004） */
fun formatGmtDate(timeMillisGmt: Long): String = gmtDateFormat.get()!!.format(Date(timeMillisGmt))
