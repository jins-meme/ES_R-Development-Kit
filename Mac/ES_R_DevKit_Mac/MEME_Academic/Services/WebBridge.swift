//
//  WebBridge.swift
//  MEME_Academic
//
//  グラフ画面(WKWebView)とアプリのやり取り。仕様は webview/BRIDGE.md(bridgeApi 1)。
//
//  - 中身は独自スキーム memeview://app/… から配る(file:// だとモジュール Worker と wasm が動かないため)。
//    根は WebContentStore.activeDir。再生する CSV は memeview://app/replay/<token>/<名前> で配り、ページが読む。
//  - アプリ → ページ: evaluateJavaScript("jmasHost.xxx(JSON)")。ページが ready を返すまでは溜めておく。
//  - 受信したサンプルは 0.05 秒ごとにまとめて push する(1 件ずつ呼ぶと重い)。
//  - ページ → アプリ: window.webkit.messageHandlers.jmas.postMessage({kind, …})。
//  - **ページから外へは通信させない**(zip は任意の JS を動かせるので、計測データを外へ送らせない。webview/BRIDGE.md の Limits):
//    全応答に Content-Security-Policy を付け(fetch・WebSocket・画像・Worker の中まで自分のオリジンだけ)、CSP の外にある
//    WebRTC は読み込みの最初に消し、外のページへの移動は断る。アプリ自身の通信(外部出力のソケットなど)はネイティブなので関係ない。
//

import Foundation
import WebKit
import AppKit

@MainActor
final class WebBridge: NSObject {

    let webView: WKWebView
    private let scheme = WebSchemeHandler()
    private(set) var isReady = false
    private(set) var pageName = ""
    private var pending: [String] = []
    private var rows = ""
    private var flushTimer: Timer?

    /// ページで付けたアーティファクト(i = ライブはアプリのサンプル番号、再生は CSV のデータ行の番号)
    var onArtifact: ((Int, String) -> Void)?
    /// 再生する CSV をページが読み終えた(mode / cps / accRange / gyroRange / rows / warning)
    var onReplayInfo: (([String: Any]) -> Void)?
    /// ページの準備ができた(manifest の name / version)
    var onReady: ((String, String) -> Void)?

    override init() {
        let cfg = WKWebViewConfiguration()
        cfg.setURLSchemeHandler(scheme, forURLScheme: WebSchemeHandler.scheme)
        // メッセージの受け口は WKWebView を作る前に足す(作った後に足しても効かない。設定は作るときに写される)
        let ucc = WKUserContentController()
        ucc.addUserScript(WKUserScript(source: Self.noWebRTC, injectionTime: .atDocumentStart, forMainFrameOnly: false))
        cfg.userContentController = ucc
        // 裏に回っても間引かない(計測中にグラフの窓を他の窓の後ろへ回すことがあるため)
        if #available(macOS 14.0, *) { cfg.preferences.inactiveSchedulingPolicy = .none }
        webView = WKWebView(frame: .zero, configuration: cfg)
        super.init()
        ucc.add(WeakScriptHandler(self), name: "jmas")
        webView.navigationDelegate = self
        #if DEBUG
        // Safari の Web インスペクタはデバッグビルドだけ(Android の WebView のデバッグと揃える。リリースでは中を覗かせない)
        if #available(macOS 13.3, *) { webView.isInspectable = true }
        #endif
        webView.setValue(false, forKey: "drawsBackground")   // 地はページが塗る(読み込み中の白い点滅を避ける)
        load()
    }

    /// WebRTC(RTCPeerConnection)は CSP の connect-src が効かず、STUN で外へ出られるので、ページのスクリプトより先に消す
    static let noWebRTC = """
        for (const k of ["RTCPeerConnection", "webkitRTCPeerConnection", "RTCDataChannel", "RTCSessionDescription", "RTCIceCandidate"]) {
          try { Object.defineProperty(window, k, { value: undefined, writable: false, configurable: false }); } catch (e) {}
        }
        """

    /// 中身を読み込み直す(設定で zip を切り替えたとき)
    func load() {
        isReady = false
        pending.removeAll()
        rows = ""
        let entry = WebContentStore.shared.manifest?.entry ?? "index.html"
        webView.load(URLRequest(url: URL(string: "\(WebSchemeHandler.origin)/\(entry)")!))
    }

    // MARK: - アプリ → ページ

    private func call(_ js: String) {
        if isReady {
            webView.evaluateJavaScript(js + ";0", completionHandler: nil)
        } else {
            pending.append(js)
        }
    }

    private static func json(_ obj: Any) -> String {
        guard let data = try? JSONSerialization.data(withJSONObject: obj, options: [.fragmentsAllowed]),
              let s = String(data: data, encoding: .utf8) else { return "null" }
        return s
    }

    /// 計測の開始。cond は BRIDGE.md の start の中身(label / mode / cps / accRange / gyroRange / columns / startedAt …)
    func start(_ cond: [String: Any]) {
        flushRows()
        call("jmasHost.start(\(Self.json(cond)))")
    }

    /// 1 サンプルぶん(i = アプリのサンプル番号、values = start の columns の並び)
    func push(i: Int, values: [Int]) {
        rows += "[\(i)"
        for v in values { rows += ",\(v)" }
        rows += "],"
        if flushTimer == nil {
            flushTimer = Timer.scheduledTimer(withTimeInterval: 0.05, repeats: true) { [weak self] _ in
                Task { @MainActor in self?.flushRows() }
            }
        }
    }

    func flushRows() {
        guard !rows.isEmpty else { return }
        rows.removeLast()
        call("jmasHost.push([\(rows)])")
        rows = ""
    }

    func gap() { flushRows(); call("jmasHost.gap()") }
    func status(_ text: String) { call("jmasHost.status(\(Self.json(text)))") }
    func mark(i: Int, text: String) { flushRows(); call("jmasHost.mark(\(Self.json(["i": i, "text": text])))") }
    func setTheme(dark: Bool) { call("jmasHost.setTheme(\(Self.json(dark ? "dark" : "light")))") }

    func stop() {
        flushRows()
        flushTimer?.invalidate(); flushTimer = nil
        call("jmasHost.stop()")
    }

    /// CSV 再生。ファイルを仮想ホストの下に出し、ページに読ませる。
    func openReplay(file: URL, extra: [String: Any]) {
        stop()
        let url = scheme.publishReplay(file)
        var arg = extra
        arg["url"] = url
        arg["name"] = file.lastPathComponent
        call("jmasHost.openReplay(\(Self.json(arg)))")
    }

    func closeReplay() {
        stop()
        scheme.unpublishReplay()
    }
}

// MARK: - ページ → アプリ

extension WebBridge: WKScriptMessageHandler {
    func userContentController(_ userContentController: WKUserContentController, didReceive message: WKScriptMessage) {
        guard let body = message.body as? [String: Any], let kind = body["kind"] as? String else { return }
        switch kind {
        case "ready":
            let api = (body["bridgeApi"] as? NSNumber)?.intValue ?? 0
            pageName = "\(body["name"] ?? "") \(body["version"] ?? "")"
            guard api == WebContentStore.bridgeApi else {
                NSLog("[WebBridge] bridgeApi mismatch: page %d, app %d", api, WebContentStore.bridgeApi)
                return
            }
            isReady = true
            let queued = pending; pending.removeAll()
            for js in queued { webView.evaluateJavaScript(js + ";0", completionHandler: nil) }
            onReady?(body["name"] as? String ?? "", body["version"] as? String ?? "")
        case "artifact":
            guard let i = (body["i"] as? NSNumber)?.intValue, let text = body["text"] as? String else { return }
            onArtifact?(i, text)
        case "replay-info":
            onReplayInfo?(body)
        case "log":
            NSLog("[WebView %@] %@", String(describing: body["level"] ?? "info"), String(describing: body["message"] ?? ""))
        default:
            NSLog("[WebView] unknown message %@", kind)
        }
    }
}

// MARK: - ナビゲーション(中身の外へは移動させない)

extension WebBridge: WKNavigationDelegate {
    func webView(_ webView: WKWebView, decidePolicyFor navigationAction: WKNavigationAction,
                 decisionHandler: @escaping @MainActor @Sendable (WKNavigationActionPolicy) -> Void) {
        guard let url = navigationAction.request.url else { decisionHandler(.cancel); return }
        if url.scheme == WebSchemeHandler.scheme || url.scheme == "blob" || url.scheme == "about" {
            decisionHandler(.allow)
        } else {
            // URL に載せて外へ出せるので、既定のブラウザでも開かない
            NSLog("[WebBridge] blocked navigation to %@://%@", url.scheme ?? "", url.host ?? "")
            decisionHandler(.cancel)
        }
    }

    func webViewWebContentProcessDidTerminate(_ webView: WKWebView) {
        NSLog("[WebBridge] web content process terminated; reloading")
        load()
    }
}

/// WKUserContentController は handler を強く持つので、循環参照を避ける中継。
private final class WeakScriptHandler: NSObject, WKScriptMessageHandler {
    weak var target: (any WKScriptMessageHandler)?
    init(_ target: any WKScriptMessageHandler) { self.target = target }
    func userContentController(_ c: WKUserContentController, didReceive m: WKScriptMessage) {
        target?.userContentController(c, didReceive: m)
    }
}

// MARK: - 独自スキーム

/// memeview://app/<パス> を WebContentStore.activeDir の下のファイルで返す。
/// /replay/<token>/<名前> は publishReplay で出した CSV(大きいので 4 MB ずつ流す)。
@MainActor
final class WebSchemeHandler: NSObject, WKURLSchemeHandler {

    static let scheme = "memeview"
    static let origin = "memeview://app"
    /// 全応答に付ける Content-Security-Policy(3 アプリで同じ。webview/BRIDGE.md の Limits)
    static let csp = "default-src 'self'; script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' blob: data:; worker-src 'self' blob:; " +
        "media-src 'self' blob: data:; frame-src 'none'; object-src 'none'; form-action 'none'; base-uri 'none'"

    private var replayToken: String?
    private var replayFile: URL?
    private var stopped = Set<ObjectIdentifier>()

    private static let types: [String: String] = [
        "html": "text/html", "js": "text/javascript", "mjs": "text/javascript", "css": "text/css",
        "json": "application/json", "wasm": "application/wasm", "zip": "application/zip", "whl": "application/zip",
        "py": "text/plain", "txt": "text/plain", "svg": "image/svg+xml", "png": "image/png", "gz": "application/gzip",
        "csv": "text/csv", "LICENSE": "text/plain",
    ]

    func publishReplay(_ file: URL) -> String {
        let token = UUID().uuidString
        replayToken = token
        replayFile = file
        let name = file.lastPathComponent.addingPercentEncoding(withAllowedCharacters: .urlPathAllowed) ?? "data.csv"
        return "\(Self.origin)/replay/\(token)/\(name)"
    }

    func unpublishReplay() { replayToken = nil; replayFile = nil }

    func webView(_ webView: WKWebView, start task: any WKURLSchemeTask) {
        guard let url = task.request.url else { return fail(task, 400) }
        let path = url.path.removingPercentEncoding ?? url.path
        let parts = path.split(separator: "/", omittingEmptySubsequences: true).map(String.init)

        // 再生する CSV
        if parts.first == "replay" {
            guard parts.count >= 2, parts[1] == replayToken, let file = replayFile,
                  let data = try? Data(contentsOf: file, options: .mappedIfSafe) else { return fail(task, 404) }
            respond(task, url: url, type: "application/octet-stream", data: data)
            return
        }

        // 中身(zip を展開したもの)
        guard !parts.contains("..") else { return fail(task, 403) }
        let root = WebContentStore.shared.activeDir.standardizedFileURL
        let file = root.appendingPathComponent(parts.isEmpty ? "index.html" : parts.joined(separator: "/")).standardizedFileURL
        guard file.path.hasPrefix(root.path + "/"), let data = try? Data(contentsOf: file) else { return fail(task, 404) }
        let ext = file.pathExtension.isEmpty ? file.lastPathComponent : file.pathExtension
        respond(task, url: url, type: Self.types[ext] ?? "application/octet-stream", data: data)
    }

    func webView(_ webView: WKWebView, stop task: any WKURLSchemeTask) {
        stopped.insert(ObjectIdentifier(task))
    }

    private func respond(_ task: any WKURLSchemeTask, url: URL, type: String, data: Data) {
        let headers = ["Content-Type": type, "Content-Length": "\(data.count)", "Cache-Control": "no-store",
                       "Access-Control-Allow-Origin": "*", "Content-Security-Policy": Self.csp]
        let id = ObjectIdentifier(task)
        guard !stopped.contains(id) else { stopped.remove(id); return }
        task.didReceive(HTTPURLResponse(url: url, statusCode: 200, httpVersion: "HTTP/1.1", headerFields: headers)!)
        let chunk = 4 << 20
        var off = 0
        while off < data.count {
            if stopped.contains(id) { stopped.remove(id); return }
            let end = min(off + chunk, data.count)
            task.didReceive(data.subdata(in: off..<end))
            off = end
        }
        task.didFinish()
    }

    private func fail(_ task: any WKURLSchemeTask, _ code: Int) {
        let id = ObjectIdentifier(task)
        guard !stopped.contains(id) else { stopped.remove(id); return }
        guard let url = task.request.url else { return }
        task.didReceive(HTTPURLResponse(url: url, statusCode: code, httpVersion: "HTTP/1.1", headerFields: ["Content-Length": "0"])!)
        task.didFinish()
    }
}
