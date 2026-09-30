using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MEME_Academic_Sample.Services;

/// <summary>
/// グラフ画面(WebView2)とアプリのやり取り。仕様は webview/BRIDGE.md(bridgeApi 1)。Mac 版 WebBridge.swift に対応する。
///
/// - 中身は https://app.memeview.example/… から配る(file:// だとモジュール Worker と wasm が動かないため)。
///   根は <see cref="WebContentStore.ActiveDir"/>。再生する CSV は …/replay/&lt;token&gt;/&lt;名前&gt; で配り、ページが読む。
/// - アプリ → ページ: ExecuteScriptAsync("jmasHost.xxx(JSON)")。ページが ready を返すまでは溜めておく。
/// - 受信したサンプルは 0.05 秒ごとにまとめて push する(1 件ずつ呼ぶと重い)。受信スレッドから呼んでよい。
/// - ページ → アプリ: window.chrome.webview.postMessage({kind, …})。
/// - **ページから外へは通信させない**(zip は任意の JS を動かせるので、計測データを外へ送らせない。webview/BRIDGE.md の Limits):
///   配信は SetVirtualHostNameToFolderMapping ではなく WebResourceRequested で自分で返し、全応答に Content-Security-Policy を付ける
///   (フォルダの割り当てでは応答のヘッダを足せない)。自分のオリジン以外への要求は 403 で断り、CSP の外にある WebRTC は
///   読み込みの最初に消し、外のページへの移動と新しい窓は断る。アプリ自身の通信(TCP の外部出力・BLE)はネイティブなので関係ない。
/// </summary>
public sealed class WebBridge : IDisposable
{
    public const string Host = "app.memeview.example";
    public const string Origin = "https://" + Host;

    /// <summary>全応答に付ける Content-Security-Policy(3 アプリで同じ。webview/BRIDGE.md の Limits)</summary>
    public const string Csp =
        "default-src 'self'; script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' blob: data:; worker-src 'self' blob:; " +
        "media-src 'self' blob: data:; frame-src 'none'; object-src 'none'; form-action 'none'; base-uri 'none'";

    /// <summary>WebRTC(RTCPeerConnection)は CSP の connect-src が効かず、STUN で外へ出られるので、ページのスクリプトより先に消す</summary>
    private const string NoWebRtc = """
        for (const k of ["RTCPeerConnection", "webkitRTCPeerConnection", "RTCDataChannel", "RTCSessionDescription", "RTCIceCandidate"]) {
          try { Object.defineProperty(window, k, { value: undefined, writable: false, configurable: false }); } catch (e) {}
        }
        """;

    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        ["html"] = "text/html", ["js"] = "text/javascript", ["mjs"] = "text/javascript", ["css"] = "text/css",
        ["json"] = "application/json", ["wasm"] = "application/wasm", ["zip"] = "application/zip", ["whl"] = "application/zip",
        ["py"] = "text/plain", ["txt"] = "text/plain", ["svg"] = "image/svg+xml", ["png"] = "image/png", ["gz"] = "application/gzip",
        ["csv"] = "text/csv", ["LICENSE"] = "text/plain",
    };

    private readonly WebContentStore store;
    private readonly Control owner;
    private readonly List<string> pending = [];
    private readonly object rowsLock = new();
    private readonly System.Windows.Forms.Timer flushTimer = new() { Interval = 50 };
    private StringBuilder rows = new();
    private CoreWebView2Environment? env;
    private string? replayToken;
    private string? replayFile;

    /// <summary>ページで付けたアーティファクト(i = ライブはアプリのサンプル番号、再生は CSV のデータ行の番号)</summary>
    public event Action<int, string>? Artifact;

    /// <summary>再生する CSV をページが読み終えた(mode / cps / accRange / gyroRange / rows / warning)</summary>
    public event Action<JsonElement>? ReplayInfo;

    /// <summary>ページの準備ができた(manifest の name / version)</summary>
    public event Action<string, string>? Ready;

    /// <param name="store">中身の置き場</param>
    /// <param name="host">WebView を置くパネル。WebView2 ランタイムが無いときは代わりに案内を置く</param>
    public WebBridge(WebContentStore store, Control host)
    {
        this.store = store;
        owner = host;
        WebView = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.White };
        host.Controls.Add(WebView);
        flushTimer.Tick += (_, _) => FlushRows();
        flushTimer.Start();
    }

    public WebView2 WebView { get; }

    public bool IsReady { get; private set; }

    public string PageName { get; private set; } = "";

    /// <summary>WebView2 の準備ができなかった理由(ランタイムが無いなど)。null なら動いている。</summary>
    public string? InitError { get; private set; }

    /// <summary>WebView2 を用意して中身を読み込む。フォームが出てから呼ぶ。</summary>
    public async Task InitializeAsync()
    {
        try
        {
            // ランタイムの有無を先に見る(無いと CreateAsync が分かりにくい例外で落ちる)
            CoreWebView2Environment.GetAvailableBrowserVersionString();
            var userData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JINS", "MEME_Academic", "WebView2");
            env = await CoreWebView2Environment.CreateAsync(null, userData);
            await WebView.EnsureCoreWebView2Async(env);
        }
        catch (Exception e) when (e is WebView2RuntimeNotFoundException or COMException or InvalidOperationException)
        {
            InitError = e is WebView2RuntimeNotFoundException
                ? "The graphs need Microsoft Edge WebView2 Runtime, which is part of Windows 11."
                : $"The graph view could not start. {e.Message}";
            ShowRuntimeHelp(InitError);
            return;
        }

        var core = WebView.CoreWebView2;
        var s = core.Settings;
#if DEBUG
        // 開発者ツールはデバッグビルドだけ(Mac の isInspectable・Android の WebView のデバッグと揃える。リリースでは中を覗かせない)
        s.AreDevToolsEnabled = true;
        s.AreDefaultContextMenusEnabled = true;
#else
        s.AreDevToolsEnabled = false;
        s.AreDefaultContextMenusEnabled = false;
        s.AreBrowserAcceleratorKeysEnabled = false;
#endif
        s.IsStatusBarEnabled = false;
        s.IsZoomControlEnabled = false;       // Ctrl+ホイールはページが時間の拡大・縮小に使う
        s.IsPinchZoomEnabled = false;
        s.IsSwipeNavigationEnabled = false;
        s.IsGeneralAutofillEnabled = false;
        s.IsPasswordAutosaveEnabled = false;
        s.IsWebMessageEnabled = true;

        await core.AddScriptToExecuteOnDocumentCreatedAsync(NoWebRtc);
        // 全部の要求を見る(自分のオリジンは返し、それ以外は断る)。Worker の中からの要求も含める
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
        core.WebResourceRequested += OnResourceRequested;
        core.WebMessageReceived += OnMessage;
        core.NavigationStarting += (_, e) =>
        {
            if (!IsOwn(e.Uri))
            {
                // URL に載せて外へ出せるので、既定のブラウザでも開かない(Mac と同じ)
                Log($"blocked navigation to {SafeHost(e.Uri)}");
                e.Cancel = true;
            }
        };
        core.FrameNavigationStarting += (_, e) =>
        {
            if (!IsOwn(e.Uri))
            {
                e.Cancel = true;
            }
        };
        core.NewWindowRequested += (_, e) =>
        {
            Log($"blocked new window to {SafeHost(e.Uri)}");
            e.Handled = true;
        };
        core.ProcessFailed += (_, e) =>
        {
            Log($"web process failed ({e.ProcessFailedKind}); reloading");
            if (e.ProcessFailedKind is CoreWebView2ProcessFailedKind.RenderProcessExited
                or CoreWebView2ProcessFailedKind.RenderProcessUnresponsive
                or CoreWebView2ProcessFailedKind.BrowserProcessExited)
            {
                owner.BeginInvoke(Load);
            }
        };
        Load();
    }

    /// <summary>中身を読み込み直す(設定で zip を切り替えたとき)</summary>
    public void Load()
    {
        IsReady = false;
        pending.Clear();
        lock (rowsLock)
        {
            rows.Clear();
        }

        if (WebView.CoreWebView2 is null)
        {
            return;
        }

        var entry = store.Manifest?.Entry ?? "index.html";
        WebView.CoreWebView2.Navigate($"{Origin}/{entry}");
    }

    #region アプリ → ページ

    private void Call(string js)
    {
        if (IsReady && WebView.CoreWebView2 is not null)
        {
            _ = WebView.CoreWebView2.ExecuteScriptAsync(js + ";0");
        }
        else
        {
            pending.Add(js);
        }
    }

    private static string Json(object? value) => JsonSerializer.Serialize(value);

    /// <summary>計測の開始。cond は BRIDGE.md の start の中身(label / mode / cps / accRange / gyroRange / columns / startedAt …)</summary>
    public void Start(Dictionary<string, object?> cond)
    {
        FlushRows();
        Call($"jmasHost.start({Json(cond)})");
    }

    /// <summary>1 サンプルぶん(i = アプリのサンプル番号、values = start の columns の並び)。受信スレッドから呼んでよい。</summary>
    public void Push(int i, ReadOnlySpan<int> values)
    {
        lock (rowsLock)
        {
            rows.Append('[').Append(i);
            foreach (var v in values)
            {
                rows.Append(',').Append(v);
            }

            rows.Append("],");
        }
    }

    /// <summary>溜めた行をページへ送る(UI スレッド)</summary>
    public void FlushRows()
    {
        string batch;
        lock (rowsLock)
        {
            if (rows.Length == 0)
            {
                return;
            }

            rows.Length--;   // 末尾の「,」
            batch = rows.ToString();
            rows = new StringBuilder(batch.Length + 64);
        }

        Call($"jmasHost.push([{batch}])");
    }

    public void Gap()
    {
        FlushRows();
        Call("jmasHost.gap()");
    }

    public void Status(string text) => Call($"jmasHost.status({Json(text)})");

    public void Mark(int i, string text)
    {
        FlushRows();
        Call($"jmasHost.mark({Json(new Dictionary<string, object> { ["i"] = i, ["text"] = text })})");
    }

    public void SetTheme(bool dark) => Call($"jmasHost.setTheme({Json(dark ? "dark" : "light")})");

    public void Stop()
    {
        FlushRows();
        Call("jmasHost.stop()");
    }

    /// <summary>CSV 再生。ファイルを仮想ホストの下に出し、ページに読ませる。</summary>
    public void OpenReplay(string file, Dictionary<string, object?> extra)
    {
        Stop();
        replayToken = Guid.NewGuid().ToString("N");
        replayFile = file;
        var arg = new Dictionary<string, object?>(extra)
        {
            ["url"] = $"{Origin}/replay/{replayToken}/{Uri.EscapeDataString(Path.GetFileName(file))}",
            ["name"] = Path.GetFileName(file),
        };
        Call($"jmasHost.openReplay({Json(arg)})");
    }

    public void CloseReplay()
    {
        Stop();
        replayToken = null;
        replayFile = null;
    }

    #endregion

    #region ページ → アプリ

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // 自分のオリジンのページからだけ受ける
        if (!IsOwn(e.Source))
        {
            return;
        }

        JsonElement body;
        try
        {
            body = JsonDocument.Parse(e.WebMessageAsJson).RootElement;
        }
        catch (JsonException)
        {
            return;
        }

        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("kind", out var kindEl) ||
            kindEl.ValueKind != JsonValueKind.String)
        {
            return;
        }

        switch (kindEl.GetString())
        {
            case "ready":
                var api = body.TryGetProperty("bridgeApi", out var a) && a.TryGetInt32(out var v) ? v : 0;
                var name = Str(body, "name");
                var version = Str(body, "version");
                PageName = $"{name} {version}";
                if (api != WebContentStore.BridgeApi)
                {
                    Log($"bridgeApi mismatch: page {api}, app {WebContentStore.BridgeApi}");
                    return;
                }

                IsReady = true;
                var queued = pending.ToArray();
                pending.Clear();
                foreach (var js in queued)
                {
                    _ = WebView.CoreWebView2.ExecuteScriptAsync(js + ";0");
                }

                Ready?.Invoke(name, version);
                break;
            case "artifact":
                if (body.TryGetProperty("i", out var iEl) && iEl.TryGetInt32(out var i) &&
                    body.TryGetProperty("text", out var tEl) && tEl.ValueKind == JsonValueKind.String)
                {
                    Artifact?.Invoke(i, tEl.GetString() ?? "");
                }

                break;
            case "replay-info":
                ReplayInfo?.Invoke(body.Clone());
                break;
            case "log":
                Log($"[page {Str(body, "level")}] {Str(body, "message")}");
                break;
            default:
                Log($"unknown message {kindEl.GetString()}");
                break;
        }
    }

    private static string Str(JsonElement body, string key) =>
        body.TryGetProperty(key, out var el) ? (el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.ToString()) : "";

    #endregion

    #region 配信(https://app.memeview.example/…)

    private void OnResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (!Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase))
        {
            // 自分のオリジンの外(CSP で止まるはずのものも、ここで念押し)
            e.Response = env!.CreateWebResourceResponse(null, 403, "Forbidden", "");
            return;
        }

        var path = Uri.UnescapeDataString(uri.AbsolutePath);
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // 再生する CSV
        if (parts.Length > 0 && parts[0] == "replay")
        {
            if (parts.Length >= 2 && replayToken is not null && parts[1] == replayToken && replayFile is not null &&
                ReadToMemory(replayFile) is { } csv)
            {
                e.Response = Respond(csv, "application/octet-stream");
            }
            else
            {
                e.Response = Fail(404);
            }

            return;
        }

        // 中身(zip を展開したもの)
        if (parts.Any(p => p == ".." || p == "." || p.Contains('\\') || p.Contains(':')))
        {
            e.Response = Fail(403);
            return;
        }

        var root = Path.GetFullPath(store.ActiveDir) + Path.DirectorySeparatorChar;
        var file = Path.GetFullPath(Path.Combine(root, parts.Length == 0 ? "index.html" : string.Join(Path.DirectorySeparatorChar, parts)));
        if (!file.StartsWith(root, StringComparison.OrdinalIgnoreCase) || OpenRead(file) is not { } stream)
        {
            e.Response = Fail(404);
            return;
        }

        var name = Path.GetFileName(file);
        var ext = Path.GetExtension(name).TrimStart('.');
        e.Response = Respond(stream, Types.GetValueOrDefault(ext.Length == 0 ? name : ext, "application/octet-stream"));
    }

    private static FileStream? OpenRead(string path)
    {
        try
        {
            return File.Exists(path)
                ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1 << 16)
                : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// 再生する CSV はメモリに読んでから渡す。WebView2 は読み終えたストリームを GC まで手放さないので、
    /// FileStream のままだと再生中の Save Artifacts で元ファイルを置き換えられない(Access denied)。
    /// </summary>
    private static MemoryStream? ReadToMemory(string path)
    {
        using var file = OpenRead(path);
        if (file is null)
        {
            return null;
        }

        var memory = new MemoryStream(checked((int)file.Length));
        file.CopyTo(memory);
        memory.Position = 0;
        return memory;
    }

    private CoreWebView2WebResourceResponse Respond(Stream stream, string type)
    {
        var headers = $"Content-Type: {type}\r\nContent-Length: {stream.Length}\r\nCache-Control: no-store\r\n" +
                      $"Access-Control-Allow-Origin: *\r\nContent-Security-Policy: {Csp}";
        return env!.CreateWebResourceResponse(stream, 200, "OK", headers);
    }

    private CoreWebView2WebResourceResponse Fail(int code) =>
        env!.CreateWebResourceResponse(null, code, code == 404 ? "Not Found" : "Forbidden", "Content-Length: 0");

    private static bool IsOwn(string uri) =>
        uri.StartsWith(Origin + "/", StringComparison.OrdinalIgnoreCase) || uri == Origin ||
        uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase) || uri.StartsWith("blob:" + Origin, StringComparison.OrdinalIgnoreCase);

    private static string SafeHost(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var u) ? $"{u.Scheme}://{u.Host}" : "(invalid)";

    #endregion

    /// <summary>WebView2 ランタイムが無いときの案内(Windows 11 は最初から入っている。§10 の 8: 同梱しない)</summary>
    private void ShowRuntimeHelp(string message)
    {
        WebView.Visible = false;
        var link = new LinkLabel
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = message + "\nInstall it from https://developer.microsoft.com/microsoft-edge/webview2/ and restart the app.",
        };
        var url = "https://developer.microsoft.com/microsoft-edge/webview2/";
        var start = link.Text.IndexOf(url, StringComparison.Ordinal);
        link.LinkArea = new LinkArea(start, url.Length);
        link.LinkClicked += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { }
        };
        owner.Controls.Add(link);
        link.BringToFront();
    }

    private static void Log(string message)
    {
        Debug.WriteLine($"[WebBridge] {message}");
#if DEBUG
        // デバッグビルドだけ、ページのログもファイルへ残す(画面を見られない環境からの確かめ用)
        try
        {
            File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JINS", "MEME_Academic", "webview-debug.log"), $"{DateTime.Now:HH:mm:ss.fff} {message}\n");
        }
        catch (IOException) { }
#endif
    }

    public void Dispose()
    {
        flushTimer.Dispose();
        WebView.Dispose();
    }
}
