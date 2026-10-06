package com.jins_jp.meme.core.web

import android.app.KeyguardManager
import android.content.Context
import android.content.Intent
import android.content.pm.ApplicationInfo
import android.net.Uri
import android.os.PowerManager
import android.util.Log
import com.jins_jp.meme.core.ble.ConnectionState
import com.jins_jp.meme.core.data.AccRange
import com.jins_jp.meme.core.data.GyroRange
import com.jins_jp.meme.core.data.MemeMode
import com.jins_jp.meme.core.data.MemeQuality
import com.jins_jp.meme.core.data.decompressIfGzip
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.withContext
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withTimeoutOrNull
import kotlin.coroutines.resume
import androidx.lifecycle.LifecycleCoroutineScope
import com.jins_jp.meme.core.ui.main.MainViewModel
import kotlinx.coroutines.launch
import org.json.JSONArray
import org.json.JSONObject
import java.io.File

/**
 * デバッグビルドだけの自己テストの入口（Mac の DebugAutoTest.swift に当たる）。画面を操作せずに adb から
 * グラフ画面の zip の取り込み・悪い zip の検査・CSV 再生を回すため。ファイルはアプリ専用の外部フォルダ
 * （/sdcard/Android/data/<パッケージ>/files/。adb push で置ける）から読み、結果は同じ場所の autotest/<名前>.json に書く。
 *
 *   adb shell am start -n <パッケージ>/.MainActivity --es autotest_zip advanced.zip        # Display Engine と同じ取り込みをして、それを使う
 *   adb shell am start -n <パッケージ>/.MainActivity --es autotest_badzips badzips         # 名前が good で始まるものだけ通るか
 *       # (取り込んである zip の一覧と使っている 1 つは退避しておき、終わったら戻す)
 *   adb shell am start -n <パッケージ>/.MainActivity --ez autotest_builtin true            # 同梱の標準版を使う(取り込んだ zip は消さない)
 *   adb shell am start -n <パッケージ>/.MainActivity --ez autotest_engines true            # Display Engine の操作を通しで見る(Mac・Windows の engines と同じ)
 *       # 標準版は先頭で消せない・取り込んでも有効にならない・同じファイルは重ならない・使うのは 1 つだけ(ページが読む manifest もそれ)・
 *       # 同じ name は置き換え(位置も使っているかもそのまま)・使っているものを消すと標準版に戻る・起動し直しても(prepare)残る・
 *       # 無い ID なら標準版・以前の版の custom/ が zips/ の 1 つへ移る。試す zip は同梱の標準版の中身から作る。終わったら元に戻す
 *   adb shell am start -n <パッケージ>/.MainActivity --es autotest_replay <CSV>            # 再生を始める
 *   adb shell am start -n <パッケージ>/.MainActivity --ez autotest_end true                # 再生を終える(書き戻し)
 *   adb shell am start -n <パッケージ>/.MainActivity --ez autotest_crash true              # グラフ画面のレンダラを落とす
 *       # 作り直したページで、計測中なら計測(端末のアドレス)、再生中なら再生(ファイル名)が続いているかを crash.json に書く
 *   adb shell am start -n <パッケージ>/.MainActivity --es autotest_live full --ei autotest_seconds 20 --es autotest_device 6E:AD
 *       # **実機(メガネ)を使う**: 条件を full|standard・100Hz・±8g・±1000dps にし、autotest_device(アドレスの末尾。省略時は
 *       # 見つかった最初の端末)に繋いで計測し、
 *       # ページの状態・アーティファクトの書き戻し・保存した CSV(モード・行数・NUM の抜け)を autotest/live-<モード>.json に書く
 *       # (標準版・高機能版のどちらでも回せる。判定器の状態を見るのは高機能版のときだけ)
 *       # --ez autotest_bg true を足すと、計測中に画面を消した間もページへ push が届いたかも見る。画面は adb から消して点ける:
 *       #   adb shell input keyevent KEYCODE_SLEEP   (計測が始まって数秒後)
 *       #   adb shell input keyevent KEYCODE_WAKEUP  (終わる前。点けなくてもよい。画面ロックがあると点けても前には戻らない)
 *       # manifest の runInBackground が true のページは「消えている間も途切れなく届き、gap が来ない」、
 *       # そうでないページは「消えている間は届かず、戻ったら gap が来る」なら ok
 *       # --ez autotest_outputs true を足すと、高機能版の設定の Notify(立ち座り)・CSV(高さ・速度)をオンにして読み込み直してから計測し、
 *       # 判定器の表の CSV ができたこと・行数が 0 でないこと・NUM がデータ CSV の範囲に入っていること・DATE が入っていること・
 *       # 通知を受けた回数(立ち座りが無ければ 0 でよい)を outputs に書く。autotest_bg と一緒なら、画面オフの間も行が届いたかも見る
 *       # (webview/BRIDGE.md の Detector notifications and tables)
 *   adb shell am start -n <パッケージ>/.MainActivity --ez autotest_reconnect true --ei autotest_seconds 15 [--es autotest_device 6E:AD]
 *       # **実機を使う**: 計測を始めて autotest_seconds 秒後にアプリから接続を切り(予期しない切断として扱われる)、自動再接続で
 *       # 計測が始まり直して行が増えるかを autotest/reconnect.json に書く(設定の自動再接続はテストの間だけオンにする)。
 *       # 切る前に画面を消しておくと(adb shell input keyevent KEYCODE_SLEEP)、再開の MeasurementService.start が裏から呼ばれる。
 *       # 裏からの FGS 開始が断られるとアプリが落ちて json は書かれない(logcat の ForegroundServiceStartNotAllowedException)
 *
 * ページの中の状態は chrome://inspect(デバッグビルドは WebView のデバッグが有効)から見る。
 */
object DebugAutoTest {
    private const val TAG = "DebugAutoTest"

    fun handle(context: Context, intent: Intent?, vm: MainViewModel, scope: LifecycleCoroutineScope) {
        if (intent == null || context.applicationInfo.flags and ApplicationInfo.FLAG_DEBUGGABLE == 0) return
        val base = context.getExternalFilesDir(null) ?: return
        val out = File(base, "autotest").apply { mkdirs() }
        intent.getStringExtra("autotest_zip")?.let { name ->
            intent.removeExtra("autotest_zip")
            scope.launch {
                val r = vm.importGraphZipFile(File(base, name))
                write(out, "zip", JSONObject().put("zip", name)
                    .put("accepted", r.getOrNull()?.displayName).put("refused", r.exceptionOrNull()?.message))
            }
        }
        intent.getStringExtra("autotest_badzips")?.let { dir ->
            intent.removeExtra("autotest_badzips")
            scope.launch { write(out, "badzips", keepingEngines(vm) { badZips(vm, File(base, dir)) }) }
        }
        if (intent.getBooleanExtra("autotest_engines", false)) {
            intent.removeExtra("autotest_engines")
            scope.launch {
                write(out, "engines", keepingEngines(vm) {
                    runCatching { engines(context, vm) }.getOrElse { JSONObject().put("ok", false).put("error", it.toString()) }
                })
            }
        }
        if (intent.getBooleanExtra("autotest_builtin", false)) {
            intent.removeExtra("autotest_builtin")
            vm.useBuiltInGraph()
        }
        intent.getStringExtra("autotest_replay")?.let { name ->
            intent.removeExtra("autotest_replay")
            vm.startPlayback(Uri.fromFile(File(base, name)))
        }
        intent.getStringExtra("autotest_live")?.let { mode ->
            intent.removeExtra("autotest_live")
            val seconds = intent.getIntExtra("autotest_seconds", 20)
            val device = intent.getStringExtra("autotest_device")
            val bg = intent.getBooleanExtra("autotest_bg", false)
            val outs = intent.getBooleanExtra("autotest_outputs", false)
            scope.launch {
                // 設定「計測完了時に共有を開く」が ON でも、テストの間は共有シートを出さない（設定値は変えない）
                vm.suppressShareForAutotest = true
                val r = try {
                    runCatching { live(context, vm, mode, seconds, device, bg, outs) }
                } finally {
                    vm.suppressShareForAutotest = false
                }
                if (r.isFailure && vm.ui.value.isMeasuring) vm.toggleMeasurement()   // 途中で失敗したら計測を止めておく
                write(out, "live-$mode", r.getOrElse { JSONObject().put("ok", false).put("error", it.toString()) })
            }
        }
        if (intent.getBooleanExtra("autotest_reconnect", false)) {
            intent.removeExtra("autotest_reconnect")
            val seconds = intent.getIntExtra("autotest_seconds", 15)
            val device = intent.getStringExtra("autotest_device")
            scope.launch {
                vm.suppressShareForAutotest = true
                val r = try {
                    runCatching { reconnectTest(context, vm, seconds, device) }
                } finally {
                    vm.suppressShareForAutotest = false
                }
                if (r.isFailure && vm.ui.value.isMeasuring) vm.toggleMeasurement()
                write(out, "reconnect", r.getOrElse { JSONObject().put("ok", false).put("error", it.toString()) })
            }
        }
        if (intent.getBooleanExtra("autotest_crash", false)) {
            intent.removeExtra("autotest_crash")
            scope.launch {
                write(out, "crash", runCatching { crashPage(vm) }.getOrElse { JSONObject().put("ok", false).put("error", it.toString()) })
            }
        }
        if (intent.getBooleanExtra("autotest_end", false)) {
            intent.removeExtra("autotest_end")
            if (vm.ui.value.isReplaying) vm.connectOrDisconnect()
        }
    }

    private suspend fun badZips(vm: MainViewModel, dir: File): JSONObject {
        val store = vm.graphStore
        val zips = dir.listFiles { f -> f.name.endsWith(".zip") }?.sortedBy { it.name } ?: emptyList()
        val watch = listOf(store.root.parentFile!!, store.root, dir)
        fun listing() = watch.flatMap { d -> d.list()?.map { "${d.path}/$it" } ?: emptyList() }
            .filter { !it.contains("/tmp-") && !it.contains("/old-") && !it.endsWith("/zips") }.toSet()
        val rows = JSONArray()
        var allOk = zips.isNotEmpty()
        for (zip in zips) {
            val before = store.activeId to store.entries
            val beforeFiles = listing()
            val t0 = System.currentTimeMillis()
            val r = vm.importGraphZipFile(zip)
            val row = JSONObject().put("zip", zip.name).put("ms", System.currentTimeMillis() - t0)
            r.onSuccess { row.put("accepted", it.displayName) }
            r.onFailure { row.put("refused", it.message); row.put("unchanged", before == (store.activeId to store.entries)) }
            val extra = listing() - beforeFiles
            if (extra.isNotEmpty()) row.put("writtenOutside", JSONArray(extra.sorted()))
            val left = store.root.list()?.filter { it.startsWith("tmp-") } ?: emptyList()
            if (left.isNotEmpty()) row.put("tmpLeft", JSONArray(left))
            val wantAccept = zip.name.startsWith("good")
            val ok = r.isSuccess == wantAccept && extra.isEmpty() && left.isEmpty() &&
                (wantAccept || row.optBoolean("unchanged"))
            row.put("ok", ok)
            allOk = allOk && ok
            rows.put(row)
        }
        // どれか 1 つでも NG なら、組全体も ok にしない(ほかのテストの json と同じく ok を見れば足りるように)
        val failed = (0 until rows.length()).map { rows.getJSONObject(it) }.filter { !it.optBoolean("ok") }.map { "zip " + it.optString("zip") }
        return JSONObject().put("ok", failed.isEmpty()).put("allOk", allOk).put("zips", rows)
            .apply { if (failed.isNotEmpty()) put("error", "check failed: " + failed.joinToString("; ")) }
    }

    /** 取り込んである zip の一覧と使っている 1 つを退避して block を回し、終わったら戻す(テストで取り込んだ zip を残さない) */
    private suspend fun keepingEngines(vm: MainViewModel, block: suspend () -> JSONObject): JSONObject {
        val store = vm.graphStore
        val kept = withContext(Dispatchers.IO) { store.copyImportedAside() }
            ?: return JSONObject().put("ok", false).put("error", "could not set the imported zips aside")
        return try {
            block()
        } finally {
            withContext(Dispatchers.IO) { store.restoreImported(kept) }
            vm.web.load()
            vm.graphStoreChanged()
        }
    }

    // ---- Display Engine(複数の zip を持ち、1 つだけ使う) ----

    private suspend fun engines(context: Context, vm: MainViewModel): JSONObject {
        val store = vm.graphStore
        val builtIn = WebContentStore.BUILT_IN_ID
        val work = File(context.cacheDir, "autotest-engines").apply { deleteRecursively(); mkdirs() }
        val std = store.entries.firstOrNull { it.isBuiltIn } ?: throw IllegalStateException("no built-in entry")

        // 同梱の標準版の中身を写し、manifest の name・title・version だけ変える
        fun makeTree(name: String, version: String): File {
            val src = File(work, "$name-$version")
            store.bundledDirForTest.copyRecursively(src, overwrite = true)
            val mf = File(src, "manifest.json")
            mf.writeText(JSONObject(mf.readText()).put("name", name).put("title", name.uppercase()).put("version", version).toString())
            return src
        }
        fun makeZip(name: String, version: String): File {
            val src = makeTree(name, version)
            val zip = File(work, "$name-$version.zip")
            java.util.zip.ZipOutputStream(zip.outputStream()).use { zos ->
                src.walkTopDown().filter { it.isFile }.forEach { f ->
                    zos.putNextEntry(java.util.zip.ZipEntry(f.relativeTo(src).invariantSeparatorsPath))
                    f.inputStream().use { it.copyTo(zos) }
                    zos.closeEntry()
                }
            }
            return zip
        }
        // ページ(読み込み直したもの)が読んでいる manifest の name
        suspend fun servedName(): String {
            vm.web.load()
            delay(300)
            waitFor("page ready", 30_000) { vm.web.isReady }
            val r = page(vm, "(() => { const x = new XMLHttpRequest(); x.open('GET', 'manifest.json', false); x.send(); return JSON.parse(x.responseText).name; })()")
            return runCatching { JSONArray("[$r]").getString(0) }.getOrDefault(r)
        }

        val steps = JSONArray()
        val problems = mutableListOf<String>()
        fun check(what: String, ok: Boolean, detail: Any? = "") {
            steps.put(JSONObject().put("step", what).put("ok", ok).put("detail", detail.toString()))
            if (!ok) problems += what
        }
        fun names() = store.entries.joinToString(",") { if (it.isBuiltIn) "*" + it.manifest.name else it.manifest.name }
        val stdName = "*" + std.manifest.name
        suspend fun <T> io(f: () -> T): T = withContext(Dispatchers.IO) { f() }

        // 始めは取り込んだものを持たない状態にする(元のものは keepingEngines が退避して戻す)
        io { store.entries.filter { !it.isBuiltIn }.forEach { store.remove(it.id) }; store.activate(builtIn) }
        check("built-in only, first and active", names() == stdName && store.activeId == builtIn, names())
        check("built-in cannot be removed", !io { store.remove(builtIn) } && store.entries.first().isBuiltIn)
        check("page serves built-in", servedName() == std.manifest.name)

        val zipA1 = io { makeZip("enginea", "1.0.0") }
        val a = io { store.add(zipA1) }
        val b = io { store.add(makeZip("engineb", "1.0.0")) }
        check("add keeps active, appends in order", a.outcome == WebContentStore.AddOutcome.Added && !a.replacedActive &&
            store.activeId == builtIn && names() == "$stdName,enginea,engineb", names())

        // 同じファイルをもう一度(別の場所に写したものでも)取り込んでも重ならない
        val copyA1 = File(work, "copy-of-enginea.zip").also { zipA1.copyTo(it, overwrite = true) }
        val same1 = io { store.add(zipA1) }
        val same2 = io { store.add(copyA1) }
        check("same file is not added twice", same1.outcome == WebContentStore.AddOutcome.AlreadyAdded &&
            same2.outcome == WebContentStore.AddOutcome.AlreadyAdded && same1.entry.id == a.entry.id &&
            same2.entry.id == a.entry.id && names() == "$stdName,enginea,engineb", names())

        store.activate(a.entry.id)
        check("activate one", store.activeId == a.entry.id && store.manifest?.name == "enginea" &&
            store.activeDir.name == a.entry.id)
        check("page serves the active one", servedName() == "enginea")

        val a2 = io { store.add(makeZip("enginea", "2.0.0")) }
        check("same name replaces (same id, still active, new version)", a2.entry.id == a.entry.id &&
            a2.outcome == WebContentStore.AddOutcome.Replaced && a2.replacedActive && store.entries.size == 3 &&
            store.activeId == a.entry.id && store.manifest?.version == "2.0.0", names())
        check("replacing keeps the position in the list", names() == "$stdName,enginea,engineb", names())

        val b2 = io { store.add(makeZip("engineb", "2.0.0")) }
        check("replacing an inactive one does not touch the active one", !b2.replacedActive && store.activeId == a.entry.id)

        check("remove active falls back to built-in", io { store.remove(a.entry.id) } && store.activeId == builtIn &&
            names() == "$stdName,engineb", names())
        check("removed folder is gone", !File(store.root, "zips/${a.entry.id}").exists() &&
            !File(store.root, "zips/${a.entry.id}.sha256").exists())
        val readd = io { store.add(zipA1) }
        check("a removed zip can be added again", readd.outcome == WebContentStore.AddOutcome.Added && readd.entry.id != a.entry.id, names())
        io { store.remove(readd.entry.id) }
        check("page serves built-in after remove", servedName() == std.manifest.name)

        store.activate(b.entry.id)
        io { store.prepare() }
        check("active survives restart", store.activeId == b.entry.id && store.manifest?.name == "engineb")

        store.setActiveForTest("no-such-id")
        io { store.prepare() }
        check("unknown active id falls back to built-in", store.activeId == builtIn)

        val refused = io { runCatching { store.add(File(work, "enginea-1.0.0/manifest.json")) }.isFailure }
        check("bad file refused, nothing changes", refused && names() == "$stdName,engineb", names())

        // 以前の版の形: custom/ に 1 つだけ、source = "Custom"
        io { store.plantLegacyCustom(makeTree("legacy", "0.9.0")); store.prepare() }
        check("legacy custom migrated and active", store.manifest?.name == "legacy" && names().contains("legacy") &&
            names().contains("engineb") && !store.legacyLeft(), names())
        check("page serves migrated one", servedName() == "legacy")

        work.deleteRecursively()
        return JSONObject().put("ok", problems.isEmpty()).put("engines", steps)
            .apply { if (problems.isNotEmpty()) put("error", "check failed: " + problems.joinToString("; ")) }
    }

    private suspend fun waitFor(what: String, ms: Long, cond: () -> Boolean) {
        withTimeoutOrNull(ms) { while (!cond()) delay(100) } ?: throw IllegalStateException("timeout: $what")
    }

    /**
     * ページで JS の式を評価し、JSON.stringify した文字列を返す(evaluateJavascript は結果をもう一度 JSON にして返すので剥がす)。
     * ページが返事をしないとき(読み込み直しの途中など)は待ち続けず、10 秒で失敗にする。
     */
    private suspend fun page(vm: MainViewModel, expr: String): String = withTimeoutOrNull(10_000) {
        suspendCancellableCoroutine { k ->
            vm.web.webView.value.evaluateJavascript("JSON.stringify($expr)") { r ->
                if (k.isActive) k.resume(runCatching { JSONArray("[$r]").getString(0) }.getOrDefault("null"))
            }
        }
    } ?: throw IllegalStateException("timeout: the page did not answer (${expr.take(40)})")

    private const val STATE = "({charts: [...document.querySelectorAll('.chart .ctitle')].map(e => e.textContent)," +
        " status: document.querySelector('.status')?.textContent, toast: document.querySelector('.toast')?.hidden === false ? document.querySelector('.toast').textContent : ''," +
        " detector: window.jmasEngine ? {state: jmasEngine.state, error: jmasEngine.error, fed: jmasEngine.fed, rows: jmasEngine.store?.n, counts: jmasEngine.counts, loadSec: jmasEngine.loadSec} : null})"

    /**
     * 画面オフの確認用: ページの jmasHost.push / gap を包んで、届いた行数・見えていない間に届いた行数・push の最大間隔・
     * gap の回数・隠れた回数を window.__bg に数える(ページは変えない。レンダラが作り直されたら消える)。
     */
    private const val BG_PROBE = """(() => {
  const h = window.jmasHost, b = window.__bg = {rows: 0, hiddenRows: 0, maxHiddenIntervalMs: 0, gaps: 0, hides: 0};
  let last = 0;
  const push = h.push, gap = h.gap;
  h.push = (r) => {
    const n = (typeof r === 'string' ? JSON.parse(r) : r).length, now = performance.now();
    b.rows += n;
    if (document.hidden) { b.hiddenRows += n; if (last) b.maxHiddenIntervalMs = Math.max(b.maxHiddenIntervalMs, Math.round(now - last)); }
    last = now;
    return push(r);
  };
  h.gap = () => { b.gaps++; return gap(); };
  document.addEventListener('visibilitychange', () => { if (document.hidden) b.hides++; });
  return 1;
})()"""

    /** 高機能版の設定(detectors.js)の Notify・CSV をオンにする localStorage のキー(ページは読み込むときに読む) */
    private const val OUTPUTS_ON = "(localStorage.setItem('advanced.notify', JSON.stringify(['posture']))," +
        " localStorage.setItem('advanced.csv', JSON.stringify(['hve'])), 1)"

    /** 画面と画面ロックの状態(裏から呼ばれたかを結果に残す) */
    private fun screen(context: Context): JSONObject = JSONObject()
        .put("interactive", context.getSystemService(PowerManager::class.java).isInteractive)
        .put("locked", context.getSystemService(KeyguardManager::class.java).isKeyguardLocked)

    /**
     * 計測中の予期しない切断 → 自動再接続 → 計測の再開(新しい CSV)を通す。再開は ReconnectController が
     * MainViewModel.startMeasurement を呼び、そこで MeasurementService.start(startForegroundService)をもう一度呼ぶ。
     * 画面を消しておけば、それが裏から呼ばれる(サービスは切断の間も止めずに動いている)。
     */
    private suspend fun reconnectTest(context: Context, vm: MainViewModel, seconds: Int, device: String?): JSONObject {
        val res = JSONObject()
        val reconnect0 = vm.ui.value.reconnectEnabled
        vm.setReconnectEnabled(true)
        try {
            connectIdle(vm, device)
            res.put("device", vm.currentAddress())
            val saved0 = vm.lastSaved.second
            vm.toggleMeasurement()
            waitFor("measuring", 20_000) { vm.ui.value.isMeasuring }
            delay(seconds * 1000L)
            res.put("beforeDrop", screen(context).put("measuring", vm.ui.value.isMeasuring).put("rows", vm.ui.value.recordingRows))
            vm.debugDropConnection()
            waitFor("dropped", 15_000) { !vm.ui.value.isMeasuring }
            val t0 = System.currentTimeMillis()
            waitFor("measuring again", 120_000) { vm.ui.value.isMeasuring }
            res.put("reconnectMs", System.currentTimeMillis() - t0)
            res.put("restarted", screen(context))
            delay(8000)
            val rows = vm.ui.value.recordingRows
            res.put("rowsAfterRestart", rows)
            vm.toggleMeasurement()
            waitFor("stopped", 30_000) { !vm.ui.value.isMeasuring && vm.lastSaved.second > saved0 }
            // 8 秒で 100 Hz なら 800 行近く。再開した計測が行を書いていれば ok
            return res.put("ok", rows > 300)
        } finally {
            vm.setReconnectEnabled(reconnect0)
        }
    }

    /** autotest_device(アドレスの末尾。null なら見つかった最初の端末)に繋ぎ、計測していない状態にする */
    private suspend fun connectIdle(vm: MainViewModel, device: String?) {
        val want = { a: String? -> a != null && (device == null || a.endsWith(device, ignoreCase = true)) }
        val settled = { vm.ui.value.connection.let { it == ConnectionState.Disconnected || it == ConnectionState.ServicesReady } }
        if (!(vm.ui.value.connection == ConnectionState.ServicesReady && want(vm.currentAddress()))) {
            if (vm.ui.value.connection != ConnectionState.Disconnected) {
                waitFor("settled", 20_000, settled)
                if (vm.ui.value.connection != ConnectionState.Disconnected) {
                    vm.connectOrDisconnect()                        // 別の端末につながっている → 切る
                    waitFor("disconnected", 15_000) { vm.ui.value.connection == ConnectionState.Disconnected }
                }
            }
            vm.startScan()
            waitFor("device found", 20_000) { vm.ui.value.devices.any(want) }
            delay(500)
            // 自動接続(設定)が繋ぎ始めていたら、落ち着くのを待ってから確かめる
            if (vm.ui.value.connection != ConnectionState.Disconnected) waitFor("settled", 20_000, settled)
            if (!(vm.ui.value.connection == ConnectionState.ServicesReady && want(vm.currentAddress()))) {
                if (vm.ui.value.connection != ConnectionState.Disconnected) {
                    vm.connectOrDisconnect()
                    waitFor("disconnected", 15_000) { vm.ui.value.connection == ConnectionState.Disconnected }
                }
                vm.selectDevice(vm.ui.value.devices.indexOfFirst(want))
                vm.connectOrDisconnect()
                waitFor("connected", 30_000) { vm.ui.value.connection == ConnectionState.ServicesReady }
            }
            delay(3000)                                            // 通知の有効化・端末情報の取得を待つ
        }
        if (vm.ui.value.isMeasuring) {                              // 前の計測が残っていたら止めてから
            vm.toggleMeasurement()
            waitFor("previous stopped", 30_000) { !vm.ui.value.isMeasuring }
            delay(2000)
        }
    }

    private suspend fun live(context: Context, vm: MainViewModel, mode: String, seconds: Int, device: String?, bg: Boolean, outs: Boolean): JSONObject {
        if (outs) {
            page(vm, OUTPUTS_ON)
            vm.web.load()
            delay(500)
            waitFor("page reloaded", 30_000) { vm.web.isReady }
        }
        val res = JSONObject().put("mode", mode).put("page", vm.web.pageName)
        vm.updateSettings {
            it.copy(mode = if (mode == "standard") MemeMode.Standard else MemeMode.Full,
                quality = MemeQuality.Hz100, accRange = AccRange.G8, gyroRange = GyroRange.Dps1000)
        }
        connectIdle(vm, device)
        res.put("device", vm.currentAddress())
        val saved0 = vm.lastSaved.second
        val keep = vm.graphStore.manifest?.runInBackground == true
        if (bg) { res.put("runInBackground", keep); page(vm, BG_PROBE) }
        vm.toggleMeasurement()
        waitFor("measuring", 20_000) { vm.ui.value.isMeasuring }
        delay(1500)
        res.put("atStart", JSONObject(page(vm, STATE)))
        delay(seconds * 1000L)
        res.put("measuring", JSONObject(page(vm, STATE)))
        // ページで付けたのと同じ形でアーティファクトを送る(持っている先頭から 100 行目のサンプル番号)。
        // 判定器の無いページ(標準版)はページの中の番号を読めないので、101(ライブのサンプル番号 = CSV の NUM。先頭は 1)を送る
        val i = page(vm, "(() => { const s = window.jmasEngine?.store; return s ? s.iOfRow(s.first + 100) : null })()").toLongOrNull() ?: 101L
        vm.web.webView.value.evaluateJavascript("window.jmasNative.postMessage(JSON.stringify({kind: 'artifact', i: $i, text: 'autotest'})); 0", null)
        delay(500)
        res.put("artifactI", i)
        vm.toggleMeasurement()
        waitFor("stopped and merged", 30_000) { vm.lastSaved.second > saved0 }
        val uri = vm.lastSaved.first ?: throw IllegalStateException("no saved CSV")
        res.put("csv", uri.toString())
        // 保存した CSV: ヘッダ・行数・アーティファクトの行(NUM 列)
        val lines = context.contentResolver.openInputStream(uri)!!.use { decompressIfGzip(it).bufferedReader().readLines() }
        val h = lines.indexOfFirst { it.startsWith("//ARTIFACT") }
        val data = lines.drop(h + 1).filter { it.isNotEmpty() }
        res.put("header", JSONArray(lines.take(h + 1).filter { it.startsWith("// Data mode") || it.startsWith("// Transmission") || it.contains("range") }))
        res.put("rows", data.size)
        val hit = data.firstOrNull { it.startsWith("autotest,") }
        res.put("artifactNum", hit?.split(",")?.getOrNull(1))
        val det = res.getJSONObject("measuring").optJSONObject("detector")
        val wantMode = (if (mode == "standard") MemeMode.Standard else MemeMode.Full).display
        val modeOk = lines.take(h + 1).any { it.startsWith("// Data mode") && it.substringAfter(":").trim() == wantMode }
        val artifactOk = hit != null && hit.split(",")[1] == i.toString()
        // 判定器の条件は、判定器のあるページ(高機能版)だけに掛ける。標準版は CSV(モード・行数・アーティファクト)だけを見る
        val detOk = when {
            det == null -> true
            mode == "full" -> det.optString("state") == "ready" && det.optLong("fed") > 0 && det.optLong("rows") - det.optLong("fed") < 300
            else -> det.optString("state") == "off" && res.getJSONObject("atStart").optString("toast").startsWith("No detection")
        }
        val nums = data.mapNotNull { it.split(",").getOrNull(1)?.toLongOrNull() }
        val numGaps = nums.zipWithNext().count { (a, b) -> b != a + 1 }   // 番号(NUM)の抜け・戻り
        res.put("numGaps", numGaps)
        // 画面オフ: 実際に隠れたこと。runInBackground なら全部(止めたときの端数ぶんの余裕を見る)届き gap なし、そうでなければ隠れた間は届かず gap あり
        var bgOk = true
        if (bg) {
            val b = JSONObject(page(vm, "({...window.__bg, hiddenAtEnd: document.hidden})"))
            res.put("background", b)
            bgOk = b.optInt("hides") > 0 && if (keep) {
                b.optInt("hiddenRows") > 0 && b.optInt("gaps") == 0 && b.optInt("rows") >= data.size - 20 && b.optInt("maxHiddenIntervalMs") < 3000
            } else {
                // 隠れる直前に送った分(1 秒未満)は数に入りうる。画面ロックで前に戻れないまま終わったときは gap はまだ来ない
                b.optInt("hiddenRows") < 100 && (b.optInt("gaps") > 0 || b.optBoolean("hiddenAtEnd"))
            }
        }
        val outsOk = if (outs) checkOutputs(context, vm, res, nums, bg && keep) else true
        res.put("check", JSONObject().put("mode", modeOk).put("artifact", artifactOk).put("detector", detOk)
            .put("rows", data.size > seconds * 80).put("num", numGaps == 0).apply { if (bg) put("background", bgOk) }
            .apply { if (outs) put("outputs", outsOk) })
        return res.put("ok", modeOk && artifactOk && detOk && numGaps == 0 && data.size > seconds * 80 && bgOk && outsOk)
    }

    /**
     * 判定器の表の CSV と通知(--ez autotest_outputs true)。表 hve が 1 本でき、行が 0 でなく、NUM がデータ CSV の範囲に入り、
     * DATE が(ほぼ)全部入っていること。ページが送った数(jmasOutputs.sent)とアプリが受けた数も並べる。
     * inBackground(runInBackground のページで画面を消したとき)は、消えている間にも行が届いたこと
     */
    private suspend fun checkOutputs(context: Context, vm: MainViewModel, res: JSONObject, dataNums: List<Long>, inBackground: Boolean): Boolean {
        val o = vm.detectorOutputs
        val stats = o.stats.toJson()
        val out = JSONObject().put("app", stats).put("tables", o.tablesJson())
            .put("pageSent", JSONObject(page(vm, "window.jmasOutputs?.sent ?? null").let { if (it == "null") "{}" else it }))
        val lo = dataNums.minOrNull() ?: 0L
        val hi = dataNums.maxOrNull() ?: -1L
        val files = JSONArray()
        var ok = vm.lastSavedTables.isNotEmpty() && stats.getJSONArray("warnings").length() == 0
        for (uri in vm.lastSavedTables) {
            val lines = context.contentResolver.openInputStream(uri)!!.use { decompressIfGzip(it).bufferedReader().readLines() }
            val head = lines.takeWhile { it.startsWith("//") }
            val rows = lines.drop(head.size).filter { it.isNotEmpty() }.map { it.split(",") }
            val nums = rows.mapNotNull { it.getOrNull(0)?.toLongOrNull() }
            val inRange = nums.isNotEmpty() && nums.all { it in lo..hi }
            val withDate = rows.count { it.getOrNull(1)?.isNotEmpty() == true }
            val f = JSONObject().put("uri", uri.toString()).put("header", JSONArray(head)).put("rows", rows.size)
                .put("numMin", nums.minOrNull()).put("numMax", nums.maxOrNull()).put("numInDataRange", inRange)
                .put("withDate", withDate).put("first", JSONArray(rows.take(3).map { it.joinToString(",") }))
            files.put(f)
            ok = ok && rows.isNotEmpty() && inRange && withDate >= rows.size * 0.95
        }
        out.put("files", files)
        if (inBackground) {
            val bgRows = stats.optLong("rowsInBackground")
            out.put("rowsWhileHidden", bgRows)
            ok = ok && bgRows > 0
        }
        res.put("outputs", out)
        return ok
    }

    /**
     * レンダラを落として(chrome://crash。onRenderProcessGone → WebView を作り直して読み込み直す)、作り直したページで
     * 計測・再生が続いているかを見る。ステータスの行は start / openReplay を受けたときだけ端末のアドレス・ファイル名を出す。
     */
    private suspend fun crashPage(vm: MainViewModel): JSONObject {
        val st = vm.ui.value
        val key = when {
            st.isReplaying -> st.replayName
            st.isMeasuring -> vm.currentAddress()
            else -> null
        }
        val before = JSONObject(page(vm, STATE))
        val old = vm.web.webView.value
        old.loadUrl("chrome://crash")
        waitFor("recreated", 20_000) { vm.web.webView.value !== old && vm.web.isReady }
        delay(3000)
        val after = JSONObject(page(vm, STATE))
        val status = after.optString("status")
        val ok = key != null && status.contains(key) && (!st.isMeasuring || status.contains("Hz"))
        return JSONObject().put("ok", ok).put("key", key).put("measuring", st.isMeasuring).put("replaying", st.isReplaying)
            .put("before", before).put("after", after)
    }

    private fun write(dir: File, name: String, o: JSONObject) {
        File(dir, "$name.json").writeText(o.toString(1))
        Log.i(TAG, "$name: ${o.toString().take(400)}")
    }
}
