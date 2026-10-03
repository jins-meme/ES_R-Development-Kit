package com.jins_jp.meme.core.web

import android.Manifest
import android.annotation.SuppressLint
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.os.Handler
import android.os.Looper
import android.os.SystemClock
import android.util.Log
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.core.content.ContextCompat
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.data.createEsrLoggerFile
import com.jins_jp.meme.core.data.dataFileMime
import com.jins_jp.meme.core.data.dataFileName
import com.jins_jp.meme.core.data.formatGmtDate
import org.json.JSONArray
import org.json.JSONObject
import java.io.OutputStreamWriter
import java.util.zip.GZIPOutputStream

/**
 * グラフ画面の判定器から届く通知と演算結果の表（ページ → アプリの notify / table / records）。
 * 仕様は DevKit の webview/BRIDGE.md の Detector notifications and tables。
 * 規則（名前・列名の形、上限、文字列の扱い、ライブの間だけ受ける、同じ tag は 2 秒に 1 件）は
 * webview/common/dev.js の受け口の真似と同じ。**アプリは判定器の中身を知らない**（何を通知する・何列書くかはページが決める）。
 *
 *  - 受け付けるのは [start]（計測の開始）から [stop] まで。再生の解析中に届いたものは捨てる。
 *  - 通知: チャンネル「Detector events」(重要度: 既定)。同じ tag は 1 件に上書き(上書きでも毎回鳴る)、2 秒に 1 件まで
 *    (超えたぶんは最後の 1 件だけ 2 秒後に出す)。tag は 1 計測 16 まで。通知の権限が無ければ出さない(計測は続ける)。
 *  - 表: 1 つを CSV 1 本に。データ CSV と同じベース名 + "_<name>"、同じフォルダ・同じ圧縮。最初の行が届いたときに作り、
 *    100 行ごとに追記する(途中で落ちてもそこまでは残る)。DATE はデータ CSV の同じ NUM の行の DATE(直近 30 分を覚えておく)。
 *
 * どのメソッドもメインスレッドから呼ぶ(WebView のメッセージも受信したサンプルもメインスレッドで来る)。
 */
class DetectorOutputs(private val context: Context) {

    private val main = Handler(Looper.getMainLooper())

    /** 今の計測の番号([stop] に渡して、前の計測の遅れた stop で次の計測を閉じないように) */
    var session = 0
        private set
    private var active = false
    private var baseName = ""
    private var compress = true
    private var pageName: () -> String = { "" }

    /** アプリが前に出ているか(画面オフの間も届いたかを数えるだけ) */
    var foreground = true

    // NUM → DATE(直近 30 分。NUM % 容量 の位置に、その NUM と時刻を置く)
    private var nums = LongArray(0)
    private var times = LongArray(0)

    private val tables = LinkedHashMap<String, Table>()
    private val tags = HashMap<String, TagState>()

    /** 確かめる用(自己テスト): 受けた通知・出した通知・受けた行・画面オフの間に受けた行/通知・規則に合わず捨てたもの */
    val stats = Stats()

    class Stats {
        var notifyReceived = 0; var notifyShown = 0; var rowsReceived = 0L
        var rowsInBackground = 0L; var notifyInBackground = 0
        val warnings = ArrayList<String>()
        fun toJson(): JSONObject = JSONObject().put("notifyReceived", notifyReceived).put("notifyShown", notifyShown)
            .put("rowsReceived", rowsReceived).put("rowsInBackground", rowsInBackground).put("notifyInBackground", notifyInBackground)
            .put("warnings", JSONArray(warnings.takeLast(10)))
    }

    /** 計測の始まり。前の計測の表がまだ開いていれば閉じる。[cps] で NUM → DATE を覚える数(30 分ぶん)を決める */
    fun start(baseName: String, compress: Boolean, cps: Int, pageName: () -> String): Int {
        if (active) stop()
        session++
        this.baseName = baseName
        this.compress = compress
        this.pageName = pageName
        val cap = DATE_KEEP_SEC * cps.coerceAtLeast(1)
        if (nums.size != cap) { nums = LongArray(cap); times = LongArray(cap) }
        nums.fill(-1)
        tables.clear()
        for (t in tags.values) main.removeCallbacks(t.flush)
        tags.clear()
        stats.apply { notifyReceived = 0; notifyShown = 0; rowsReceived = 0; rowsInBackground = 0; notifyInBackground = 0; warnings.clear() }
        active = true
        return session
    }

    /** データ CSV に 1 行書いた(DATE 列のため) */
    fun recordSample(num: Long, timeGmtMillis: Long) {
        if (!active || num < 0) return
        val k = (num % nums.size).toInt()
        nums[k] = num; times[k] = timeGmtMillis
    }

    private fun dateOf(num: Long): Long? {
        if (num < 0 || nums.isEmpty()) return null
        val k = (num % nums.size).toInt()
        return if (nums[k] == num) times[k] else null
    }

    /**
     * 計測の終わり。表の残りを書いて閉じ、作ったファイル(行のあった表)の URI を返す。[session] を渡したときは、
     * それが今の計測のときだけ閉じる(切断から少し待って閉じる間に、次の計測が始まっていることがある)。
     */
    fun stop(session: Int? = null): List<Uri> {
        if (!active || (session != null && session != this.session)) return emptyList()
        active = false
        for (t in tags.values) main.removeCallbacks(t.flush)      // 2 秒待ちの通知は出さない(計測は終わった)
        tags.clear()
        val uris = tables.values.mapNotNull { it.close() }
        Log.i(TAG, "stop: tables ${tables.values.joinToString { "${it.name}=${it.rows}" }} ${stats.toJson()}")
        return uris
    }

    /** 表ごとの結果(自己テスト用。[stop] の後も次の [start] まで残る) */
    fun tablesJson(): JSONArray = JSONArray(tables.values.map {
        JSONObject().put("name", it.name).put("columns", JSONArray(it.columns)).put("rows", it.rows)
            .put("uri", it.uri?.toString()).put("bad", it.bad)
    })

    private fun warn(text: String) {
        stats.warnings += text
        Log.w(TAG, text)
    }

    // ------------------------------------------------------------------ ページ → アプリ

    /** WebBridge から(kind は notify / table / records) */
    fun receive(o: JSONObject) {
        val kind = o.optString("kind")
        if (!active) { Log.d(TAG, "$kind: not in a live measurement, ignored"); return }
        when (kind) {
            "notify" -> notify(o)
            "table" -> table(o)
            "records" -> records(o)
        }
    }

    /** 1 行の文字列(タイトル・本文・表題): 長さと制御文字を見る。合わなければ null */
    private fun text(o: JSONObject, key: String, max: Int): String? {
        val v = o.opt(key) as? String ?: return null
        return if (v.length <= max && !CTRL.containsMatchIn(v)) v else null
    }

    // ---------------------------------------------------------------- 通知

    private class Notice(val tag: String, val title: String, val text: String?, val i: Long?)

    private inner class TagState(val tag: String) {
        var last = Long.MIN_VALUE / 2           // 最後に出した時刻(elapsedRealtime)
        var pending: Notice? = null
        val flush = Runnable { pending?.let { show(this, it) }; pending = null }
    }

    private fun notify(o: JSONObject) {
        stats.notifyReceived++
        if (!foreground) stats.notifyInBackground++
        val tag = o.opt("tag") as? String
        if (tag == null || !NAME.matches(tag)) return warn("notify: bad tag ${o.opt("tag")}")
        val title = text(o, "title", 64) ?: return warn("notify $tag: title is required (at most 64 characters, no control characters)")
        val body = if (o.isNull("text")) null else text(o, "text", 200) ?: return warn("notify $tag: text must be at most 200 characters without control characters")
        val i = if (o.isNull("i")) null else (o.opt("i") as? Number)?.takeIf { it is Int || it is Long }?.toLong()?.takeIf { it >= 0 }
            ?: return warn("notify $tag: i must be a sample number")
        if (tag !in tags && tags.size >= MAX_TAGS) return warn("notify: more than $MAX_TAGS tags, $tag ignored")
        val st = tags.getOrPut(tag) { TagState(tag) }
        val n = Notice(tag, title, body, i)
        val wait = st.last + TAG_INTERVAL_MS - SystemClock.elapsedRealtime()
        if (wait <= 0) { show(st, n); return }
        // 2 秒以内の続き: 最後の 1 件だけを後で出す
        if (st.pending == null) main.postDelayed(st.flush, wait)
        st.pending = n
    }

    @SuppressLint("MissingPermission")    // canPost で権限を見ている
    private fun show(st: TagState, n: Notice) {
        st.last = SystemClock.elapsedRealtime()
        if (!canPost()) { Log.i(TAG, "notification not shown (no permission): [${n.tag}] ${n.title}"); return }
        createChannel()
        val open = context.packageManager.getLaunchIntentForPackage(context.packageName)?.let { launch ->
            launch.flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP
            PendingIntent.getActivity(context, 0, launch, PendingIntent.FLAG_IMMUTABLE)
        }
        val b = NotificationCompat.Builder(context, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_stat_measure)
            .setContentTitle(n.title)
            .setContentIntent(open)
            .setAutoCancel(true)
            .setCategory(NotificationCompat.CATEGORY_EVENT)
            .setPriority(NotificationCompat.PRIORITY_DEFAULT)
        n.text?.let { b.setContentText(it) }
        // 時刻はイベントのサンプルの DATE(届いた時刻ではない。判定器は遅れて確定する)
        n.i?.let { dateOf(it) }?.let { b.setWhen(it).setShowWhen(true) }
        NotificationManagerCompat.from(context).notify(NOTIFY_TAG_PREFIX + n.tag, NOTIFY_ID, b.build())
        stats.notifyShown++
    }

    private fun canPost(): Boolean {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) return false
        return NotificationManagerCompat.from(context).areNotificationsEnabled()
    }

    private fun createChannel() {
        val nm = context.getSystemService(NotificationManager::class.java)
        if (nm.getNotificationChannel(CHANNEL_ID) != null) return
        nm.createNotificationChannel(NotificationChannel(CHANNEL_ID, context.getString(R.string.notification_channel_detector),
            NotificationManager.IMPORTANCE_DEFAULT))
    }

    // ---------------------------------------------------------------- 表

    private inner class Table(val name: String, val columns: List<String>, val title: String) {
        var uri: Uri? = null
        var rows = 0L
        var bad = false                           // 違う列で宣言し直された(以後の行は捨てる)
        private val buf = ArrayList<String>()
        private var header = true

        fun add(line: String) {
            buf += line
            rows++
            if (buf.size >= FLUSH_ROWS) flush()
        }

        fun flush() {
            if (buf.isEmpty()) return
            val fileName = dataFileName("${baseName}_$name", compress)
            if (uri == null) uri = createEsrLoggerFile(context, fileName, dataFileMime(compress))
            val u = uri ?: run { warn("table $name: could not create $fileName"); buf.clear(); return }
            runCatching {
                context.contentResolver.openOutputStream(u, "wa")?.use { os ->
                    // データ CSV と同じく、1 回の追記ごとに独立した gzip メンバを書く(落ちてもそこまでは読める。CsvWriter.flush)
                    val sink = if (compress) GZIPOutputStream(os) else os
                    OutputStreamWriter(sink, Charsets.UTF_8).buffered().use { w ->
                        if (header) {
                            val t = if (title.isNotEmpty()) " ($title)" else ""
                            w.write("// Detector output  : $name$t\r\n")
                            w.write("// Page  : ${pageName()}\r\n")
                            w.write("// Data file  : ${dataFileName(baseName, compress)}\r\n")
                            w.write("//\r\n")
                            w.write("//" + (listOf("NUM", "DATE") + columns).joinToString(",") + "\r\n")
                            header = false
                        }
                        for (l in buf) { w.write(l); w.write("\r\n") }
                    }
                }
            }.onFailure { warn("table $name: write failed: $it") }
            buf.clear()
        }

        fun close(): Uri? { flush(); return uri }
    }

    private fun table(o: JSONObject) {
        val name = o.opt("name") as? String
        if (name == null || !NAME.matches(name) || name == "disconnect") return warn("table: bad name ${o.opt("name")}")
        val arr = o.optJSONArray("columns")
        val columns = arr?.let { a -> (0 until a.length()).map { a.opt(it) as? String } }
        if (columns == null || columns.size !in 1..32 || columns.any { it == null || !COLUMN.matches(it) || it == "NUM" || it == "DATE" } ||
            columns.toSet().size != columns.size) return warn("table $name: bad columns $arr")
        val title = if (o.isNull("title")) "" else text(o, "title", 64)
            ?: return warn("table $name: title must be at most 64 characters without control characters")
        val cols = columns.map { it!! }
        val old = tables[name]
        if (old != null) {
            // 宣言し直し(レンダラが落ちて読み込み直した・start を送り直した): 同じ列なら同じファイルへ続ける
            if (old.columns != cols && !old.bad) { old.bad = true; warn("table $name: declared again with other columns; its rows are dropped") }
            return
        }
        if (tables.size >= MAX_TABLES) return warn("table: more than $MAX_TABLES tables, $name ignored")
        tables[name] = Table(name, cols, title)
    }

    private fun records(o: JSONObject) {
        val name = o.opt("name") as? String
        val t = tables[name] ?: return warn("records: table $name was not declared")
        if (t.bad) return
        val rows = o.optJSONArray("rows") ?: return warn("records $name: rows must be an array")
        var dropped = 0
        for (k in 0 until rows.length()) {
            val r = rows.opt(k) as? JSONArray
            val i = (r?.opt(0) as? Number)?.takeIf { it is Int || it is Long }?.toLong()
            if (r == null || r.length() != t.columns.size + 1 || i == null || i < 0) { dropped++; continue }
            val sb = StringBuilder().append(i).append(',').append(dateOf(i)?.let { formatGmtDate(it) } ?: "")
            var ok = true
            for (c in 1 until r.length()) {
                val cell = cell(r.opt(c))
                if (cell == null) { ok = false; break }
                sb.append(',').append(cell)
            }
            if (!ok) { dropped++; continue }
            t.add(sb.toString())
            stats.rowsReceived++
            if (!foreground) stats.rowsInBackground++
        }
        if (dropped > 0) warn("records $name: $dropped row(s) dropped (need [i, ${t.columns.size} values], values: number, string or null)")
    }

    /** 値 1 つ → CSV の欄。数は JSON の数のまま、文字列は 64 文字・区切りと改行は空白・数式に読まれる書き出しは空欄。それ以外は null */
    private fun cell(v: Any?): String? = when {
        v == null || v == JSONObject.NULL -> ""
        v is Boolean -> null
        v is Number -> if (v is Double && !v.isFinite()) null else JSONObject.numberToString(v)
        v is String -> v.take(64).replace(LINE_OR_COMMA, " ").let { if (it.isNotEmpty() && it[0] in "=+-@") "" else it }
        else -> null
    }

    companion object {
        private const val TAG = "DetectorOutputs"
        /** start の features に入れる(このアプリが受け付けるページ → アプリの追加メッセージ) */
        val FEATURES = listOf("notify", "records")

        private val NAME = Regex("^[a-z][a-z0-9_]{0,31}$")
        private val COLUMN = Regex("^[A-Z][A-Z0-9_]{0,31}$")
        private val CTRL = Regex("[\\u0000-\\u001f\\u007f]")
        private val LINE_OR_COMMA = Regex("[,\\r\\n]")
        private const val MAX_TAGS = 16
        private const val MAX_TABLES = 16
        private const val TAG_INTERVAL_MS = 2000L
        private const val FLUSH_ROWS = 100
        private const val DATE_KEEP_SEC = 30 * 60

        private const val CHANNEL_ID = "detector_events"
        private const val NOTIFY_TAG_PREFIX = "detector:"
        private const val NOTIFY_ID = 2001
    }
}
