package com.jins_jp.meme.core.web

import android.content.Context
import android.content.Intent
import android.content.pm.ApplicationInfo
import android.net.Uri
import android.util.Log
import com.jins_jp.meme.core.ble.ConnectionState
import com.jins_jp.meme.core.data.AccRange
import com.jins_jp.meme.core.data.GyroRange
import com.jins_jp.meme.core.data.MemeMode
import com.jins_jp.meme.core.data.MemeQuality
import com.jins_jp.meme.core.data.decompressIfGzip
import kotlinx.coroutines.delay
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
 *   adb shell am start -n <パッケージ>/.MainActivity --es autotest_zip advanced.zip        # 設定の Display Engine と同じ取り込み
 *   adb shell am start -n <パッケージ>/.MainActivity --es autotest_badzips badzips         # 名前が good で始まるものだけ通るか
 *   adb shell am start -n <パッケージ>/.MainActivity --ez autotest_builtin true            # 同梱の標準版に戻す
 *   adb shell am start -n <パッケージ>/.MainActivity --es autotest_replay <CSV>            # 再生を始める
 *   adb shell am start -n <パッケージ>/.MainActivity --ez autotest_end true                # 再生を終える(書き戻し)
 *   adb shell am start -n <パッケージ>/.MainActivity --es autotest_live full --ei autotest_seconds 20 --es autotest_device 6E:AD
 *       # **実機(メガネ)を使う**: 条件を full|standard・100Hz・±8g・±1000dps にし、autotest_device(アドレスの末尾。省略時は
 *       # 見つかった最初の端末)に繋いで計測し、
 *       # ページの状態・アーティファクトの書き戻し・保存した CSV を autotest/live-<モード>.json に書く
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
            scope.launch { write(out, "badzips", badZips(vm, File(base, dir))) }
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
            scope.launch {
                val r = runCatching { live(context, vm, mode, seconds, device) }
                if (r.isFailure && vm.ui.value.isMeasuring) vm.toggleMeasurement()   // 途中で失敗したら計測を止めておく
                write(out, "live-$mode", r.getOrElse { JSONObject().put("ok", false).put("error", it.toString()) })
            }
        }
        if (intent.getBooleanExtra("autotest_end", false)) {
            intent.removeExtra("autotest_end")
            if (vm.ui.value.mockEnabled) vm.connectOrDisconnect()
        }
    }

    private suspend fun badZips(vm: MainViewModel, dir: File): JSONObject {
        val store = vm.graphStore
        val zips = dir.listFiles { f -> f.name.endsWith(".zip") }?.sortedBy { it.name } ?: emptyList()
        val watch = listOf(store.root.parentFile!!, store.root, dir)
        fun listing() = watch.flatMap { d -> d.list()?.map { "${d.path}/$it" } ?: emptyList() }
            .filter { !it.contains("/tmp-") && !it.contains("/old-") && !it.endsWith("/custom") }.toSet()
        val rows = JSONArray()
        var allOk = zips.isNotEmpty()
        for (zip in zips) {
            val before = store.source to store.manifest
            val beforeFiles = listing()
            val t0 = System.currentTimeMillis()
            val r = vm.importGraphZipFile(zip)
            val row = JSONObject().put("zip", zip.name).put("ms", System.currentTimeMillis() - t0)
            r.onSuccess { row.put("accepted", it.displayName) }
            r.onFailure { row.put("refused", it.message); row.put("unchanged", before == (store.source to store.manifest)) }
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
        vm.useBuiltInGraph()
        return JSONObject().put("allOk", allOk).put("zips", rows)
    }

    private suspend fun waitFor(what: String, ms: Long, cond: () -> Boolean) {
        withTimeoutOrNull(ms) { while (!cond()) delay(100) } ?: throw IllegalStateException("timeout: $what")
    }

    /** ページで JS の式を評価し、JSON.stringify した文字列を返す(evaluateJavascript は結果をもう一度 JSON にして返すので剥がす) */
    private suspend fun page(vm: MainViewModel, expr: String): String = suspendCancellableCoroutine { k ->
        vm.web.webView.value.evaluateJavascript("JSON.stringify($expr)") { r ->
            k.resume(runCatching { JSONArray("[$r]").getString(0) }.getOrDefault("null"))
        }
    }

    private const val STATE = "({charts: [...document.querySelectorAll('.chart .ctitle')].map(e => e.textContent)," +
        " status: document.querySelector('.status')?.textContent, toast: document.querySelector('.toast')?.hidden === false ? document.querySelector('.toast').textContent : ''," +
        " detector: window.jmasEngine ? {state: jmasEngine.state, error: jmasEngine.error, fed: jmasEngine.fed, rows: jmasEngine.store?.n, counts: jmasEngine.counts, loadSec: jmasEngine.loadSec} : null})"

    private suspend fun live(context: Context, vm: MainViewModel, mode: String, seconds: Int, device: String?): JSONObject {
        val res = JSONObject().put("mode", mode).put("page", vm.web.pageName)
        vm.updateSettings {
            it.copy(mode = if (mode == "standard") MemeMode.Standard else MemeMode.Full,
                quality = MemeQuality.Hz100, accRange = AccRange.G8, gyroRange = GyroRange.Dps1000)
        }
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
        res.put("device", vm.currentAddress())
        if (vm.ui.value.isMeasuring) {                              // 前の計測が残っていたら止めてから
            vm.toggleMeasurement()
            waitFor("previous stopped", 30_000) { !vm.ui.value.isMeasuring }
            delay(2000)
        }
        val saved0 = vm.lastSaved.second
        vm.toggleMeasurement()
        waitFor("measuring", 20_000) { vm.ui.value.isMeasuring }
        delay(1500)
        res.put("atStart", JSONObject(page(vm, STATE)))
        delay(seconds * 1000L)
        res.put("measuring", JSONObject(page(vm, STATE)))
        // ページで付けたのと同じ形でアーティファクトを送る(持っている先頭から 100 行目のサンプル番号)
        val i = page(vm, "(() => { const s = window.jmasEngine?.store; return s ? s.iOfRow(s.first + 100) : null })()").toLongOrNull()
        if (i != null) {
            vm.web.webView.value.evaluateJavascript("window.jmasNative.postMessage(JSON.stringify({kind: 'artifact', i: $i, text: 'autotest'})); 0", null)
            delay(500)
            res.put("artifactI", i)
        }
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
        val ok = if (mode == "full") {
            det != null && det.optString("state") == "ready" && det.optLong("fed") > 0 &&
                det.optLong("rows") - det.optLong("fed") < 300 && hit != null && hit.split(",")[1] == i.toString()
        } else {
            (det == null || det.optString("state") == "off") &&
                res.getJSONObject("atStart").optString("toast").startsWith("No detection")
        }
        return res.put("ok", ok && data.size > seconds * 80)
    }

    private fun write(dir: File, name: String, o: JSONObject) {
        File(dir, "$name.json").writeText(o.toString(1))
        Log.i(TAG, "$name: ${o.toString().take(400)}")
    }
}
