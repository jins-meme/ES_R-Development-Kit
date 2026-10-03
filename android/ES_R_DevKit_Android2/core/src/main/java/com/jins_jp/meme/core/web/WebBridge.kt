package com.jins_jp.meme.core.web

import android.annotation.SuppressLint
import android.content.Context
import android.content.Intent
import android.content.MutableContextWrapper
import android.content.pm.ApplicationInfo
import android.graphics.Color
import android.net.Uri
import android.os.Handler
import android.os.Looper
import android.provider.OpenableColumns
import android.util.Log
import android.webkit.RenderProcessGoneDetail
import android.webkit.WebResourceRequest
import android.webkit.WebResourceResponse
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.webkit.WebViewAssetLoader
import androidx.webkit.WebViewCompat
import androidx.webkit.WebViewFeature
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import org.json.JSONObject
import java.io.ByteArrayInputStream
import java.io.File
import java.io.FileInputStream
import java.util.UUID

/**
 * グラフ画面（WebView）とアプリのやり取り。仕様は DevKit の webview/BRIDGE.md（bridgeApi 1）。Mac の WebBridge.swift と同じ作り。
 *
 *  - 中身は WebViewAssetLoader で https://appassets.androidplatform.net/… から配る（file:// だとモジュール Worker と wasm が
 *    動かないため）。根は [WebContentStore.activeDir]。再生する CSV は …/replay/<token>/<名前> で配り、ページが読む。
 *  - **ページから外へは通信させない**（zip は任意の JS を動かせるので、計測データを外へ送らせない。BRIDGE.md の Limits）:
 *    この仮想ホストの外への読み込みは断り（shouldInterceptRequest）、全応答に Content-Security-Policy を付けて
 *    shouldInterceptRequest を通らない WebSocket も塞ぎ、CSP の外にある WebRTC は読み込みの最初に消す。
 *    外のページへの移動は、利用者がリンクを押したときだけ既定のブラウザで開く。アプリ自身の通信（BLE など）はネイティブなので関係ない。
 *  - アプリ → ページ: evaluateJavascript("jmasHost.xxx(JSON)")。ページが ready を返すまでは溜めておく。
 *  - 受信したサンプルは 0.05 秒ごとにまとめて push する。アプリが裏に回っている間は push をやめ、戻ったら gap を送る
 *    （止まった WebView に積むと戻ったときに一度に流れ込むため。devkit-webview.md §3）。
 *    ただし manifest の runInBackground が true のページ（画面オフ中も演算を続けたいページ）には裏でも送り続ける。
 *    描画は止まるが（requestAnimationFrame が来ない）、evaluateJavascript で呼ぶ push は動く。BRIDGE.md の Running in the background。
 *  - ページ → アプリ: window.jmasNative.postMessage(JSON 文字列)（addWebMessageListener。自分のオリジンのメインフレームだけ受ける）。
 */
class WebBridge(private val context: Context, private val store: WebContentStore) {

    private val main = Handler(Looper.getMainLooper())
    private val _webView = MutableStateFlow(createWebView())
    /** 今の WebView（レンダラが落ちたら作り直すので、描く側はこれを見る） */
    val webView: StateFlow<WebView> = _webView.asStateFlow()

    var isReady = false
        private set
    var pageName = ""
        private set
    private val pending = ArrayList<String>()
    private val rows = StringBuilder()
    private var flushScheduled = false
    private var foreground = true
    /** 今のページが裏でも push を受けたいか（manifest の runInBackground。読み込むたびに見直す） */
    private var keepRunning = false
    private var missedWhileBackground = false
    private var dark = false

    private var replayToken: String? = null
    private var replayUri: Uri? = null

    /** 今ページに出している計測/再生。レンダラが落ちて作り直したときに送り直す */
    private sealed class Session {
        data class Live(val cond: JSONObject) : Session()      // start の中身
        data class Replay(val arg: JSONObject) : Session()     // openReplay の中身
    }
    private var session: Session? = null

    /** ページで付けたアーティファクト（i = ライブはアプリのサンプル番号、再生は CSV のデータ行の番号（0 始まり）） */
    var onArtifact: ((Long, String) -> Unit)? = null
    /** 再生する CSV をページが読み終えた */
    var onReplayInfo: ((JSONObject) -> Unit)? = null

    init { load() }

    // ------------------------------------------------------------------ WebView

    @SuppressLint("SetJavaScriptEnabled")
    private fun createWebView(): WebView {
        if (context.applicationInfo.flags and ApplicationInfo.FLAG_DEBUGGABLE != 0) {
            WebView.setWebContentsDebuggingEnabled(true)     // デバッグビルドだけ(chrome://inspect・自己テスト)
        }
        // WebView は画面の回転などで作り直さないよう ViewModel が持つのでアプリの Context で作るが、そのままだと
        // <select> の選択肢の一覧(ダイアログ)が出せない(Activity の上にしか出せない)。MutableContextWrapper で包み、
        // 画面に置いている間だけ Activity に差し替える(attachTo / detach。WebChartsPane)
        val wv = WebView(MutableContextWrapper(context))
        wv.setBackgroundColor(Color.TRANSPARENT)             // 地はページが塗る(読み込み中の白い点滅を避ける)
        wv.settings.apply {
            javaScriptEnabled = true
            domStorageEnabled = true                          // 表示幅などの好み(localStorage)
            allowFileAccess = false
            allowContentAccess = false
            setSupportMultipleWindows(false)
            setSupportZoom(false)
            builtInZoomControls = false
            textZoom = 100                                     // 端末の文字の大きさの設定でグラフの文字が崩れないように
        }
        val loader = WebViewAssetLoader.Builder()
            .setDomain(HOST)
            .addPathHandler("/", PathHandler())
            .build()
        wv.webViewClient = object : WebViewClient() {
            override fun shouldInterceptRequest(view: WebView, request: WebResourceRequest): WebResourceResponse? {
                if (request.url.host == HOST) {
                    return loader.shouldInterceptRequest(request.url) ?: empty(404)
                }
                // data: / blob: はここへ来ない。それ以外(外のサーバ)は断る
                Log.w(TAG, "blocked: ${request.url.scheme}://${request.url.host}")
                return empty(403)
            }

            override fun shouldOverrideUrlLoading(view: WebView, request: WebResourceRequest): Boolean {
                val url = request.url
                if (url.host == HOST && url.scheme == "https") return false
                if ((url.scheme == "https" || url.scheme == "http") && request.hasGesture() && request.isForMainFrame) {
                    runCatching {
                        context.startActivity(Intent(Intent.ACTION_VIEW, url).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK))
                    }
                }
                return true
            }

            override fun onRenderProcessGone(view: WebView, detail: RenderProcessGoneDetail): Boolean {
                Log.w(TAG, "render process gone (crash=${detail.didCrash()}); recreating")
                main.post { recreate() }
                return true
            }
        }
        if (WebViewFeature.isFeatureSupported(WebViewFeature.DOCUMENT_START_SCRIPT)) {
            WebViewCompat.addDocumentStartJavaScript(wv, NO_WEBRTC, setOf("*"))
        } else {
            Log.e(TAG, "DOCUMENT_START_SCRIPT is not supported by this WebView")
        }
        if (WebViewFeature.isFeatureSupported(WebViewFeature.WEB_MESSAGE_LISTENER)) {
            WebViewCompat.addWebMessageListener(wv, "jmasNative", setOf(ORIGIN)) { _, message, sourceOrigin, isMainFrame, _ ->
                if (!isMainFrame || sourceOrigin.toString().trimEnd('/') != ORIGIN) return@addWebMessageListener
                message.data?.let { receive(it) }
            }
        } else {
            Log.e(TAG, "WEB_MESSAGE_LISTENER is not supported by this WebView")
        }
        // 見えていない間もレンダラの優先度を下げない（画面オフ中も演算を続けるページが、メモリ不足のときに先に落とされないように）。
        // 既定と同じ値だが、裏で動かす前提をここに固定しておく
        wv.setRendererPriorityPolicy(WebView.RENDERER_PRIORITY_IMPORTANT, false)
        return wv
    }

    /** 画面に置くとき: 選択肢の一覧などのダイアログを出せるよう、WebView の Context を Activity にする */
    fun attachTo(activityContext: Context) {
        (_webView.value.context as? MutableContextWrapper)?.baseContext = activityContext
    }

    /** 画面から外すとき: Activity を握ったままにしない(アプリの Context に戻す) */
    fun detach() {
        (_webView.value.context as? MutableContextWrapper)?.baseContext = context
    }

    private fun recreate() {
        val old = _webView.value
        (old.parent as? android.view.ViewGroup)?.removeView(old)
        old.destroy()
        _webView.value = createWebView()
        load()
    }

    /** 中身を読み込み直す（設定で zip を切り替えたとき・レンダラが落ちて作り直したとき） */
    fun load() {
        isReady = false
        pending.clear()
        rows.setLength(0)
        keepRunning = store.manifest?.runInBackground == true
        val entry = store.manifest?.entry ?: "index.html"
        _webView.value.loadUrl("$ORIGIN/$entry")
        resumeSession()
    }

    /**
     * 読み込み直したページへ、テーマと、計測中・再生中ならその条件を送り直す（ready まで溜めておき、届いたサンプルより先に送る）。
     * ライブは読み込み直した後に届いた分から描くので、0 行目の時刻(startedAt)を今にする。
     * 再生は頭から読み直す（付けたまま書き戻していないアーティファクトはグラフから消えるが、アプリが控えていて CSV へは書く）。
     */
    private fun resumeSession() {
        call("jmasHost.setTheme(${JSONObject.quote(if (dark) "dark" else "light")})")
        when (val s = session) {
            is Session.Live -> {
                val cond = JSONObject(s.cond.toString()).put("startedAt", System.currentTimeMillis())
                session = Session.Live(cond)
                call("jmasHost.start(${JSONObject.quote(cond.toString())})")
            }
            is Session.Replay -> {
                val arg = JSONObject(s.arg.toString()).put("theme", if (dark) "dark" else "light")
                call("jmasHost.openReplay(${JSONObject.quote(arg.toString())})")
            }
            null -> Unit
        }
    }

    // ------------------------------------------------------------------ アプリ → ページ

    private fun call(js: String) {
        if (isReady) _webView.value.evaluateJavascript("$js;0", null) else pending += js
    }

    /** 計測の開始。cond は BRIDGE.md の start の中身 */
    fun start(cond: JSONObject) {
        flushRows()
        missedWhileBackground = false
        session = Session.Live(cond)
        call("jmasHost.start(${JSONObject.quote(cond.toString())})")
    }

    /** 1 サンプルぶん（i = アプリのサンプル番号、values = start の columns の並び） */
    fun push(i: Long, values: IntArray) {
        if (!foreground && !keepRunning) { missedWhileBackground = true; return }
        rows.append('[').append(i)
        for (v in values) rows.append(',').append(v)
        rows.append("],")
        if (!flushScheduled) {
            flushScheduled = true
            main.postDelayed({ flushScheduled = false; flushRows() }, 50)
        }
    }

    private fun flushRows() {
        if (rows.isEmpty()) return
        rows.setLength(rows.length - 1)
        call("jmasHost.push([$rows])")
        rows.setLength(0)
    }

    fun gap() { flushRows(); call("jmasHost.gap()") }
    fun mark(i: Long, text: String) {
        flushRows()
        call("jmasHost.mark(${JSONObject().put("i", i).put("text", text)})")
    }
    fun setTheme(dark: Boolean) {
        if (this.dark == dark && isReady) return
        this.dark = dark
        call("jmasHost.setTheme(${JSONObject.quote(if (dark) "dark" else "light")})")
    }

    fun stop() {
        flushRows()
        session = null
        call("jmasHost.stop()")
    }

    /** アプリが前に出た / 裏に回った。裏の間は push をやめ、戻ったら途切れとして知らせる（runInBackground のページには送り続ける） */
    fun setForeground(fg: Boolean) {
        if (foreground == fg) return
        foreground = fg
        if (!fg) { if (!keepRunning) rows.setLength(0) }
        else if (missedWhileBackground) { missedWhileBackground = false; gap() }
    }

    /** CSV 再生。ファイルを仮想ホストの下に出し、ページに読ませる */
    fun openReplay(uri: Uri, name: String, extra: JSONObject) {
        stop()
        val token = UUID.randomUUID().toString()
        replayToken = token
        replayUri = uri
        val arg = JSONObject(extra.toString())
            .put("url", "$ORIGIN/replay/$token/${Uri.encode(name)}")
            .put("name", name)
            .put("theme", if (dark) "dark" else "light")
        session = Session.Replay(arg)
        call("jmasHost.openReplay(${JSONObject.quote(arg.toString())})")
    }

    fun closeReplay() {
        stop()
        replayToken = null
        replayUri = null
    }

    // ------------------------------------------------------------------ ページ → アプリ

    private fun receive(data: String) {
        if (data.length > MAX_MESSAGE) { Log.w(TAG, "message too long (${data.length})"); return }
        val o = runCatching { JSONObject(data) }.getOrNull() ?: return
        when (o.optString("kind")) {
            "ready" -> {
                val api = o.optInt("bridgeApi", 0)
                pageName = "${o.optString("name")} ${o.optString("version")}"
                if (api != WebContentStore.BRIDGE_API) {
                    Log.w(TAG, "bridgeApi mismatch: page $api, app ${WebContentStore.BRIDGE_API}")
                    return
                }
                isReady = true
                val queued = pending.toList(); pending.clear()
                for (js in queued) _webView.value.evaluateJavascript("$js;0", null)
            }
            "artifact" -> {
                if (!o.has("i") || !o.has("text")) return
                val i = o.optLong("i", -1)
                val text = o.optString("text")
                if (i >= 0) onArtifact?.invoke(i, text)
            }
            "replay-info" -> onReplayInfo?.invoke(o)
            "log" -> {
                val msg = o.optString("message").take(2000)
                val level = o.optString("level", "info")
                if (level == "error") Log.e(TAG, "[page] $msg") else Log.i(TAG, "[page] $msg")
            }
            else -> Log.w(TAG, "unknown message ${o.optString("kind").take(40)}")
        }
    }

    // ------------------------------------------------------------------ 仮想ホスト

    /** https://appassets.androidplatform.net/<パス> を activeDir の下のファイルで返す。/replay/<token>/… は再生する CSV */
    private inner class PathHandler : WebViewAssetLoader.PathHandler {
        override fun handle(path: String): WebResourceResponse? {
            val parts = path.split('/').filter { it.isNotEmpty() }
            if (parts.firstOrNull() == "replay") return replay(parts)
            if (parts.any { it == ".." || it == "." }) return empty(403)
            val root = store.activeDir.canonicalFile
            val file = File(root, if (parts.isEmpty()) "index.html" else parts.joinToString("/")).canonicalFile
            if (!file.path.startsWith(root.path + File.separator) || !file.isFile) return empty(404)
            val ext = file.name.substringAfterLast('.', file.name)
            return WebResourceResponse(TYPES[ext] ?: "application/octet-stream", null, 200, "OK",
                mapOf("Cache-Control" to "no-store", "Content-Length" to file.length().toString(), "Content-Security-Policy" to CSP),
                FileInputStream(file))
        }

        private fun replay(parts: List<String>): WebResourceResponse {
            val uri = replayUri
            if (parts.size < 2 || parts[1] != replayToken || uri == null) return empty(404)
            val stream = runCatching { context.contentResolver.openInputStream(uri) }.getOrNull() ?: return empty(404)
            val headers = mutableMapOf("Cache-Control" to "no-store", "Content-Security-Policy" to CSP)
            runCatching {
                context.contentResolver.query(uri, arrayOf(OpenableColumns.SIZE), null, null, null)?.use { c ->
                    if (c.moveToFirst() && !c.isNull(0)) headers["Content-Length"] = c.getLong(0).toString()
                }
            }
            return WebResourceResponse("application/octet-stream", null, 200, "OK", headers, stream)
        }
    }

    companion object {
        private const val TAG = "WebBridge"
        const val HOST = "appassets.androidplatform.net"
        const val ORIGIN = "https://$HOST"
        private const val MAX_MESSAGE = 64_000

        /** 全応答に付ける Content-Security-Policy（3 アプリで同じ。webview/BRIDGE.md の Limits） */
        const val CSP = "default-src 'self'; script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' blob: data:; worker-src 'self' blob:; " +
            "media-src 'self' blob: data:; frame-src 'none'; object-src 'none'; form-action 'none'; base-uri 'none'"

        /** WebRTC（RTCPeerConnection）は CSP の connect-src が効かず STUN で外へ出られるので、ページのスクリプトより先に消す */
        private const val NO_WEBRTC = "for (const k of ['RTCPeerConnection', 'webkitRTCPeerConnection', 'RTCDataChannel', " +
            "'RTCSessionDescription', 'RTCIceCandidate']) { try { Object.defineProperty(window, k, " +
            "{ value: undefined, writable: false, configurable: false }); } catch (e) {} }"
        private val TYPES = mapOf(
            "html" to "text/html", "js" to "text/javascript", "mjs" to "text/javascript", "css" to "text/css",
            "json" to "application/json", "wasm" to "application/wasm", "zip" to "application/zip", "whl" to "application/zip",
            "py" to "text/plain", "txt" to "text/plain", "svg" to "image/svg+xml", "png" to "image/png", "gz" to "application/gzip",
            "csv" to "text/csv", "LICENSE" to "text/plain",
        )

        private fun empty(code: Int) = WebResourceResponse("text/plain", "utf-8", code,
            if (code == 404) "Not Found" else "Forbidden", mapOf("Cache-Control" to "no-store"), ByteArrayInputStream(ByteArray(0)))
    }
}
