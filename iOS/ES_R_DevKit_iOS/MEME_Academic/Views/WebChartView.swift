//
//  WebChartView.swift
//  MEME_Academic (iOS)
//
//  グラフ画面(WKWebView)を SwiftUI に置く。中身と操作は webview/(標準版 zip など)。
//  外観(ライト/ダーク)が変わったらページへ伝える。
//

import SwiftUI
import WebKit

struct WebChartView: UIViewRepresentable {
    let bridge: WebBridge
    @Environment(\.colorScheme) private var colorScheme

    func makeUIView(context: Context) -> WKWebView {
        bridge.setTheme(dark: colorScheme == .dark)
        return bridge.webView
    }

    func updateUIView(_ uiView: WKWebView, context: Context) {
        bridge.setTheme(dark: colorScheme == .dark)
    }
}
