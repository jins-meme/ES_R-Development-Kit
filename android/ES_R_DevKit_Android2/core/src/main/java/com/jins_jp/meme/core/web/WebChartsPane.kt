package com.jins_jp.meme.core.web

import android.view.ViewGroup
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.key
import androidx.compose.ui.Modifier
import androidx.compose.ui.viewinterop.AndroidView
import androidx.lifecycle.compose.collectAsStateWithLifecycle

/**
 * グラフ画面（WebView）。描画・操作（表示幅・再生・巻き戻し・拡大縮小・アーティファクト）は全部ページ側で、
 * ここは WebView を置いてアプリのテーマを渡すだけ。WebView は [WebBridge] が持つ（画面の回転などで作り直さない）。
 */
@Composable
fun WebChartsPane(bridge: WebBridge, modifier: Modifier = Modifier) {
    val webView by bridge.webView.collectAsStateWithLifecycle()
    val dark = isSystemInDarkTheme()
    LaunchedEffect(dark) { bridge.setTheme(dark) }
    key(webView) {                       // レンダラが落ちて作り直したら置き直す
        AndroidView(
            modifier = modifier.fillMaxSize(),
            factory = { ctx ->
                bridge.attachTo(ctx)                     // <select> の一覧を出すため Activity の Context に
                (webView.parent as? ViewGroup)?.removeView(webView)
                webView.layoutParams = ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
                webView
            },
            onRelease = {
                (it.parent as? ViewGroup)?.removeView(it)   // WebView は捨てない(bridge が持ち続ける)
                bridge.detach()
            },
        )
    }
}
