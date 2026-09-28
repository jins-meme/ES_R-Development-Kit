//
//  WebChartView.swift
//  MEME_Academic
//
//  グラフ画面(WKWebView)を SwiftUI に置く。中身と操作は webview/(標準版 zip など)。
//  外観(ライト/ダーク)が変わったらページへ伝える。
//

import SwiftUI
import WebKit

struct WebChartView: NSViewRepresentable {
    let bridge: WebBridge
    @Environment(\.colorScheme) private var colorScheme

    func makeNSView(context: Context) -> WKWebView {
        bridge.setTheme(dark: colorScheme == .dark)
        return bridge.webView
    }

    func updateNSView(_ nsView: WKWebView, context: Context) {
        bridge.setTheme(dark: colorScheme == .dark)
    }
}
