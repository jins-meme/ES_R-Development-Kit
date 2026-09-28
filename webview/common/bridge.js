// アプリ(ネイティブ)⇄ ページのブリッジ。bridgeApi 1。仕様は webview/BRIDGE.md。
//
// アプリ → ページ: window.jmasHost.<関数>(引数)。引数は値か、その JSON 文字列(evaluateJavaScript で渡しやすいように)。
// ページ → アプリ: post(kind, data)。届け先は環境ごとに違う:
//   Mac(WKWebView)      window.webkit.messageHandlers.jmas.postMessage(オブジェクト)
//   Windows(WebView2)   window.chrome.webview.postMessage(オブジェクト)
//   Android(WebView)    window.jmasNative.postMessage(JSON 文字列)(WebViewCompat.addWebMessageListener で注入)
// どれも無ければ(ブラウザで開いたとき)console に出すだけ。

export const BRIDGE_API = 1;

const parse = (v) => (typeof v === "string" ? JSON.parse(v) : v);

/** ページ → アプリ */
export function post(kind, data = {}) {
  const msg = { kind, ...data };
  try {
    if (window.webkit?.messageHandlers?.jmas) { window.webkit.messageHandlers.jmas.postMessage(msg); return; }
    if (window.chrome?.webview) { window.chrome.webview.postMessage(msg); return; }
    if (window.jmasNative?.postMessage) { window.jmasNative.postMessage(JSON.stringify(msg)); return; }
  } catch (e) {
    console.warn("post failed", e);
  }
  console.debug("[bridge →app]", msg);
}

/** アプリの中で開かれているか(ブラウザで直接開いたときは false) */
export const embedded = () => !!(window.webkit?.messageHandlers?.jmas || window.chrome?.webview || window.jmasNative);

/**
 * window.jmasHost を置く。handlers はページ側の受け口:
 *   start(cond)        計測・再生の開始。cond は BRIDGE.md の start の中身
 *   rows(rows)         [[i, 値…], …](値の並びは start の columns)
 *   gap()              受信の途切れ
 *   status(text)       ステータス欄の 1 行
 *   replay({url,name}) CSV 再生(ページが url から読む)
 *   mark({i, text})    アプリが付けた印(Free Marking など)
 *   theme(t)           "light" / "dark"
 *   stop()             計測・再生の終わり
 */
export function installHost(handlers) {
  const call = (name, v) => {
    try { return handlers[name]?.(v); }
    catch (e) { post("log", { level: "error", message: `${name}: ${e?.stack || e}` }); throw e; }
  };
  window.jmasHost = {
    bridgeApi: BRIDGE_API,
    start: (cond) => call("start", parse(cond)),
    push: (rows) => call("rows", parse(rows)),
    gap: () => call("gap"),
    status: (text) => call("status", String(text ?? "")),
    openReplay: (arg) => call("replay", parse(arg)),
    mark: (arg) => call("mark", parse(arg)),
    setTheme: (t) => call("theme", String(t)),
    stop: () => call("stop"),
  };
  addEventListener("error", (e) => post("log", { level: "error", message: `${e.message} (${e.filename}:${e.lineno})` }));
  addEventListener("unhandledrejection", (e) => post("log", { level: "error", message: String(e.reason?.stack || e.reason) }));
  return window.jmasHost;
}
