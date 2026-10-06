#if DEBUG
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using MEMELib_Academic;
using Microsoft.Web.WebView2.Core;
using MEME_Academic_Sample.Models;
using MEME_Academic_Sample.Services;

namespace MEME_Academic_Sample;

/// <summary>
/// Debug ビルドだけの自己テスト(Mac 版 DebugAutoTest.swift に対応)。画面を操作できない環境(Mac から Parallels の
/// Windows を動かすときなど)から、グラフ画面が動いていることを確かめる。結果(result.json)とスナップショット(PNG)を
/// 渡したフォルダに書いて、アプリを閉じる。
///
///     JINS_MEME_DataLogger.exe --autotest &lt;dir&gt; [--suite live|replay|zip|engines|settings|webcrash|reconnect] [--csv &lt;golden の CSV&gt;] [--zip &lt;zip&gt;]
///                              [--mode full|standard] [--seconds 20] [--badzips &lt;dir&gt;] [--probe &lt;JS の式 | @ファイル&gt;] [--probe-live &lt;同&gt;]
///                              [--expect &lt;瞬目,EMR,EML[,歩のイベント]&gt;] [--real [--device &lt;アドレスか名前の末尾&gt;]]
///                              [--socket &lt;ポート&gt; [--socket-stall]] [--outputs]
///
/// - live(既定): 実機の代わりに --csv の行を 100 Hz で受信の口(HandleSample)へ流して「計測 → アーティファクト → 停止 →
///   保存した CSV を再生」を回す(BLE には触らない)。--mode standard は同じ値を Standard の形に詰め替えて流す。
///   保存した CSV のモード・番号(NUM)の抜け・アーティファクトの行(299)を確かめ、再生中に付けたアーティファクトが
///   Save Artifacts(500 行目)と Disconnect(600 行目)で書き戻されることも見る(Windows では再生中の CSV を
///   WebView2 が開いたままにしていて、書き戻しが Access denied になったことがある)。
///   **--real を付けたときだけ実機(BLE)を使う**: --device(省略時は最初に見つかったもの)に繋いで --seconds 計測する(--csv は要らない)。
/// - replay: --csv の写しを再生し(元のファイルは書き換えない)、高機能版なら最後まで解析し終えるのを待つ。
///   アーティファクトを付けて Save Artifacts で書き戻され(299 行目)、Disconnect でも書き戻される(600 行目)ことを見る。
///   --expect を付けると判定数(高機能版)をその値と比べる(golden w-sit-jump-stairs なら 459,1169,1045,2837)。
/// - zip: --badzips の中の zip を 1 つずつ読み込み、名前が good で始まるものは通り、それ以外は断られて今の中身が変わらず、
///   展開先の外に何も書かれないことを見る(悪い zip は webview/tools/make_bad_zips.py が作る)。
/// - engines: Display Engine ダイアログの操作(WebContentStore の Add / Activate / Remove)を通しで見る(Mac 版の engines と同じ):
///   標準版は先頭で消せない・取り込んでも有効にはならない・同じファイルは重ならない・使うのは 1 つだけ(ページが読む manifest もそれ)・
///   同じ name は置き換え(位置も使っているかもそのまま)・使っているものを消すと標準版に戻る・起動し直しても(Prepare)選んだものが残る・
///   無い ID なら標準版・以前の版の custom\ が zips\ の 1 つに移り、使っていたならそれを使う・取り込んだ zip のページが 60 秒以内に
///   2 回落ちたら標準版へ戻って知らせる(1 回では戻らない)。試す zip は同梱の標準版の中身から作る。
/// - settings: 設定画面と Display Engine ダイアログ(計測中の形も)を撮る。
/// - webcrash: --csv の行を流して計測している最中と、その CSV を再生している最中に、グラフ画面のプロセスを落とす
///   (DevTools の Page.crash)。読み込み直したページで計測・再生が続いているか(start / openReplay を送り直したか)を見る。
/// - reconnect: 計測中に切断 → 繋ぎ直して Start Measurement で計測が始まるか、切断した回の CSV が停止と同じく締められるか
///   (ファイルが分かれる・NUM が続かない・付けた Artifact が書き戻される)を見る。--real なら実機で、無ければ --csv の行を流し、
///   切断は端末側から切られたときと同じ口(OnPeripheralDisconnected)を呼ぶ。
/// --outputs は live で(--zip で高機能版を読み込んだとき)、計測の前に高機能版の設定の Notify(立ち座り)・CSV(高さ・速度)をオンにして
/// 読み込み直し、判定器の表の CSV(データ CSV と同じベース名 + _hve・同じ圧縮)ができたこと・ページが送った行と通知の数をアプリが受けたこと・
/// NUM がデータ CSV の行と DATE ごと一致すること・受け口の検査規則(数式風・カンマ・真偽値・列数・名前)・保存ダイアログで移したときに表も移ること、
/// 通知が使えたか(Windows App Runtime が無ければ、案内のダイアログを出そうとしたこと)を outputs に書く(Android の autotest_outputs と同じ)。
/// --socket はテストの間だけ TCP 出力をそのポートで有効にし、live の間テスト自身が受け取って、届いたヘッダと行が保存した CSV と
/// 同じかを見る。--socket-stall を足すと受け取る側が読まないままにし、送信が詰まっても受信(計測)が止まらないことを見る。
/// --zip は始める前に Display Engine ダイアログと同じ経路(WebContentStore.Add → Activate)で取り込んで使い、終わったら元に戻す
/// (取り込んである zip の一覧と使っている 1 つは、退避しておいて戻す。同じ name の zip は置き換えになるため。zip の組・engines も同じ)。
/// 確かめたことが合わなければ result.json の ok が false になり、error に理由が入る。
/// 保存先はテスト用のフォルダ(&lt;dir&gt;\csv)に切り替え、終わったら戻す。
/// </summary>
public partial class MainForm
{
    private sealed class TestTimeout(string what) : Exception($"timeout: {what}");

    private sealed class CheckFailed(IEnumerable<string> problems) : Exception("check failed: " + string.Join("; ", problems));

    /// <summary>自己テストの間だけ、書き戻しの失敗などをダイアログでなくここへ集める(null なら通常どおりダイアログ)</summary>
    private List<string>? autoTestErrors;

    internal static void StartAutoTestIfRequested(MainForm form, string[] args)
    {
        var dir = Arg(args, "--autotest");
        if (dir is null)
        {
            return;
        }

        _ = form.RunAutoTest(args, dir);
    }

    private static string? Arg(string[] args, string name)
    {
        var k = Array.IndexOf(args, name);
        return k >= 0 && k + 1 < args.Length ? args[k + 1] : null;
    }

    private async Task RunAutoTest(string[] args, string dir)
    {
        Directory.CreateDirectory(dir);
        var result = new JsonObject();
        var savedPath = setting.SaveFilePath;
        var savedDialog = setting.ShowSaveFileDialog;
        setting.ShowSaveFileDialog = false;
        var csvDir = Path.Combine(dir, "csv");
        Directory.CreateDirectory(csvDir);
        setting.SaveFilePath = csvDir;   // Save はしない(利用者の設定ファイルを書き換えない)
        var usesZips = Arg(args, "--zip") is not null || Arg(args, "--suite") is "zip" or "engines";
        var startIds = webContent.Entries.Select(e => e.Id).ToHashSet();
        var startActive = webContent.ActiveId;
        var kept = usesZips ? webContent.CopyImportedAside() : null;   // 取り込みで置き換わる前に、取り込んである zip を退避
        var savedSocket = (setting.ExternalOutputSocket, setting.LocalPort);
        if (Arg(args, "--socket") is { } port)
        {
            setting.ExternalOutputSocket = true;      // Save はしない(利用者の設定ファイルを書き換えない)
            setting.LocalPort = port;
            ApplySettings();
        }

        autoTestErrors = [];
        var sw = Stopwatch.StartNew();
        try
        {
            if (web.InitError is { } err)
            {
                throw new InvalidOperationException(err);
            }

            if (Arg(args, "--zip") is { } zip)
            {
                result["zip"] = webContent.ImportAndActivate(zip).DisplayName;
                web.Load();
            }

            switch (Arg(args, "--suite") ?? "live")
            {
                case "zip":
                    await RunZipSuite(Arg(args, "--badzips") ?? "", result);
                    break;
                case "engines":
                    await RunEnginesSuite(dir, result);
                    break;
                case "settings":
                    // 設定画面と Display Engine ダイアログの見た目を撮る。ダイアログは計測中の形(切り替え不可)も撮る
                    foreach (var (name, make) in new (string, Func<Form>)[]
                             {
                                 ("settings", () => new SettingsForm(setting)),
                                 ("display-engine", () => new DisplayEngineForm(webContent, busy: false)),
                                 ("display-engine-busy", () => new DisplayEngineForm(webContent, busy: true)),
                             })
                    {
                        using var form = make();
                        form.Show(this);
                        await Sleep(1);
                        using var bmp = new Bitmap(form.Width, form.Height);
                        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                        bmp.Save(Path.Combine(dir, name + ".png"));
                        form.Close();
                    }

                    // メニューバー(Setting の隣の Display Engine)も撮る。グラフ画面(WebView2)は DrawToBitmap に写らない
                    using (var main = new Bitmap(Width, Height))
                    {
                        DrawToBitmap(main, new Rectangle(0, 0, Width, Height));
                        main.Save(Path.Combine(dir, "main.png"));
                    }

                    break;
                case "replay":
                    await RunReplaySuite(args, dir, result);
                    break;
                case "webcrash":
                    await RunWebCrashSuite(args, dir, result);
                    break;
                case "reconnect":
                    await RunReconnectSuite(args, result);
                    break;
                default:
                    await RunLiveSuite(args, dir, result);
                    break;
            }

            if (autoTestErrors.Count > 0)
            {
                throw new CheckFailed(autoTestErrors);
            }

            result["ok"] = true;
        }
        catch (Exception e)
        {
            result["ok"] = false;
            result["error"] = e.ToString();
        }

        autoTestErrors = null;

        result["seconds"] = Math.Round(sw.Elapsed.TotalSeconds, 1);
        await File.WriteAllTextAsync(Path.Combine(dir, "result.json"),
            result.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        setting.SaveFilePath = savedPath;
        setting.ShowSaveFileDialog = savedDialog;
        if (Arg(args, "--socket") is not null)
        {
            (setting.ExternalOutputSocket, setting.LocalPort) = savedSocket;
            ApplySettings();
        }
        if (usesZips)
        {
            // 取り込んだ zip を残さない。もともと使っていたものに戻す
            if (kept is { } k)
            {
                webContent.RestoreImported(k);
            }
            else
            {
                // 退避できなかったときは、増えた分だけ消す(置き換えた分は戻せない)
                foreach (var e in webContent.Entries.Where(e => !startIds.Contains(e.Id)).ToArray())
                {
                    try { webContent.Remove(e.Id); } catch (IOException) { }
                }

                webContent.Activate(startActive);
            }
        }

        Close();
    }

    #region 小物

    private static Task Sleep(double seconds) => Task.Delay(TimeSpan.FromSeconds(seconds));

    private static async Task Wait(string what, double seconds, Func<bool> cond)
    {
        var end = DateTime.UtcNow.AddSeconds(seconds);
        while (!cond())
        {
            if (DateTime.UtcNow > end)
            {
                throw new TestTimeout(what);
            }

            await Sleep(0.1);
        }
    }

    private async Task<string> Eval(string js) => await web.WebView.CoreWebView2.ExecuteScriptAsync(js);

    /// <summary>ページの式 expr が true になるまで待つ</summary>
    private async Task WaitJs(string what, double seconds, string expr)
    {
        var end = DateTime.UtcNow.AddSeconds(seconds);
        while (await Eval($"!!({expr})") != "true")
        {
            if (DateTime.UtcNow > end)
            {
                throw new TestTimeout($"{what} ({await Eval("JSON.stringify(window.jmasEngine ? {state: jmasEngine.state, error: String(jmasEngine.error), fed: jmasEngine.fed, rows: jmasEngine.store?.n, final: jmasEngine.finalSent} : null)")})");
            }

            await Sleep(0.2);
        }
    }

    private async Task<bool> HasDetector() => await Eval("!!window.jmasEngine") == "true";

    /// <summary>ページの状態(グラフの見出し・メッセージ・ステータス・検出器)</summary>
    private async Task<JsonNode?> PageState()
    {
        const string js = """
            JSON.stringify({charts: [...document.querySelectorAll('.chart .ctitle')].map(e => e.textContent),
              message: document.querySelector('.message')?.hidden ? '' : document.querySelector('.message')?.textContent,
              toast: document.querySelector('.toast')?.textContent ?? null,
              status: document.querySelector('.status')?.textContent, ready: !!window.jmasHost,
              detector: window.jmasEngine ? {state: jmasEngine.state, error: jmasEngine.error ? String(jmasEngine.error) : null, fed: jmasEngine.fed,
                                             rows: jmasEngine.store?.n, final: jmasEngine.finalSent, counts: jmasEngine.counts,
                                             stepEvents: jmasEngine.results?.ev?.step?.length ?? null} : null})
            """;
        var raw = await Eval(js);
        return JsonNode.Parse(JsonSerializer.Deserialize<string>(raw) ?? "null");
    }

    /// <summary>--probe に書いた JS の式をページで評価した値(調べもの用。Promise も待つ)</summary>
    private async Task<JsonNode?> Probe(string[] args, string option = "--probe")
    {
        if (Arg(args, option) is not { } expr)
        {
            return null;
        }

        if (expr.StartsWith('@'))
        {
            expr = await File.ReadAllTextAsync(expr[1..]);   // 引用符の多い式はファイルで渡す
        }

        try
        {
            var raw = await web.WebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Runtime.evaluate",
                JsonSerializer.Serialize(new { expression = $"(async () => JSON.stringify(await ({expr})))()", awaitPromise = true, returnByValue = true }));
            return JsonNode.Parse(raw)?["result"]?["value"]?.GetValue<string>() is { } s ? JsonNode.Parse(s) : JsonNode.Parse(raw);
        }
        catch (Exception e)
        {
            return $"probe error: {e.Message}";
        }
    }

    private async Task Snapshot(string dir, string name)
    {
        await using var f = File.Create(Path.Combine(dir, name + ".png"));
        await web.WebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, f);
    }

    /// <summary>ウィンドウ全体(左の欄。グラフ画面の中は写らない)</summary>
    private void FormSnapshot(string dir, string name)
    {
        using var bmp = new Bitmap(Width, Height);
        DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
        bmp.Save(Path.Combine(dir, name + ".png"));
    }

    /// <summary>ページから送ったのと同じ形でアーティファクトを送る</summary>
    private Task SendArtifact(int i, string text) =>
        Eval($"window.chrome.webview.postMessage({{kind: 'artifact', i: {i}, text: {JsonSerializer.Serialize(text)}}}); 0");

    /// <summary>CSV のデータ行(//ARTIFACT の次から、空行を除く)</summary>
    private static List<string[]> DataRows(string path)
    {
        var lines = CsvFile.ReadAllLines(path);
        var h = Array.FindIndex(lines, l => l.StartsWith("//ARTIFACT", StringComparison.Ordinal));
        return lines.Skip(h + 1).Where(l => l.Trim().Length > 0).Select(l => l.Split(',')).ToList();
    }

    /// <summary>
    /// 実機の代わりに CSV の行 [from, from + count) を受信の口(HandleSample)へ実時間で流す(0.01 秒ごと。遅れたら追いつくまでまとめて)。
    /// 端末カウンタ(Cnt)は行の番号から作るので、続けて呼べば続きの番号になる。
    /// </summary>
    private Task Feed(List<string[]> rows, int from, int count, bool standard) => Task.Run(async () =>
    {
        var t0 = Stopwatch.StartNew();
        for (var k = 0; k < count && from + k < rows.Count; k++)
        {
            var wait = k * 10.0 - t0.Elapsed.TotalMilliseconds;
            if (wait > 2)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(wait));
            }

            HandleSample(ToSample(rows[from + k], from + k, standard));
        }
    });

    /// <summary>保存した CSV の記録モード(ヘッダの // Data mode)</summary>
    private static string SavedMode(string path) =>
        CsvFile.ReadAllLines(path).FirstOrDefault(l => l.StartsWith("// Data mode", StringComparison.Ordinal))?
            .Split(':', 2)[1].Trim() ?? "";

    /// <summary>番号(NUM 列)が 1 ずつ増えていない所の数</summary>
    private static int NumGaps(List<string[]> data)
    {
        var nums = data.Select(r => int.TryParse(r[1], out var v) ? v : -1).ToList();
        return nums.Zip(nums.Skip(1)).Count(p => p.Second != p.First + 1);
    }

    /// <summary>再生中にアーティファクトを付け、Save Artifacts と Disconnect のそれぞれで書き戻されたか見る。変わった行の番号を返す</summary>
    private async Task<List<int>> ReplayWriteBack(string csv, int saveRow, int endRow, JsonObject result)
    {
        var before = DataRows(csv);
        // i はデータ行の番号そのまま
        await SendArtifact(saveRow, "autotest-save");
        await Sleep(0.5);
        bt_SaveArtifacts.PerformClick();
        var afterSave = DataRows(csv);
        result["savedByButton"] = afterSave.Count > saveRow && afterSave[saveRow][0] == "autotest-save";

        await SendArtifact(endRow, "autotest-end");
        await Sleep(0.5);
        EndReplaySession();                                   // Disconnect と同じ
        await Sleep(0.5);
        var after = DataRows(csv);
        result["savedByDisconnect"] = after.Count > endRow && after[endRow][0] == "autotest-end";
        var changed = Enumerable.Range(0, Math.Min(before.Count, after.Count))
            .Where(k => string.Join(',', before[k]) != string.Join(',', after[k])).ToList();
        result["replayRows"] = after.Count;
        result["replayChangedRows"] = new JsonArray(changed.Select(k => (JsonNode)k).ToArray());
        return changed;
    }

    #endregion

    #region live(受信の代わりに CSV を流す)

    private async Task RunLiveSuite(string[] args, string dir, JsonObject result)
    {
        var real = args.Contains("--real");
        var csv = real ? null : Arg(args, "--csv") ?? throw new ArgumentException("--csv is required (or --real)");
        var standard = Arg(args, "--mode") == "standard";
        var seconds = double.Parse(Arg(args, "--seconds") ?? "20", System.Globalization.CultureInfo.InvariantCulture);

        await Wait("page ready", 30, () => web.IsReady);
        var outputsTest = args.Contains("--outputs");
        if (outputsTest)
        {
            // 高機能版の設定の Notify(立ち座り)・CSV(高さ・速度)をオンにして読み込み直す(設定はページの localStorage)
            await Eval("localStorage.setItem('advanced.notify', JSON.stringify(['posture'])); localStorage.setItem('advanced.csv', JSON.stringify(['hve'])); 0");
            var readyBefore = web.ReadyCount;
            web.Load();
            await Wait("page reloaded", 30, () => web.ReadyCount > readyBefore && web.IsReady);
            DetectorNotifications.SuppressedDialogs = [];
        }

        result["page"] = web.PageName;
        result["real"] = real;
        await Snapshot(dir, "0-idle");
        FormSnapshot(dir, "0-idle-form");

        if (real)
        {
            await ConnectReal(Arg(args, "--device"), result);
        }
        else
        {
            phase = Phase.Connected;                          // BLE は繋がっていないので端末への設定は NG で素通りする
        }

        // --socket: 計測を始める前に繋いでおく(計測開始のときにヘッダが届く)
        using var socket = await SocketReceiver.ConnectAsync(args);

        // Full(または Standard)・100Hz・±8G・±1000dps で計測
        mode = standard ? MeasurementMode.Standard : MeasurementMode.Full;
        quality = MEMEQuality.High;
        accelRange = MEMEAccelRange.Range8G;
        gyroRange = MEMEGyroRange.Range1000dps;
        StartMeasurement();
        result["phase"] = phase.ToString();

        var rows = real ? new List<string[]>() : DataRows(csv!);
        var n = real ? 0 : Math.Min(rows.Count, (int)(seconds * 100));
        var feeder = real ? Task.Delay(TimeSpan.FromSeconds(seconds)) : Feed(rows, 0, n, standard);
        // --probe-live は流している最中に評価する(描画の滑らかさを測るときなど)
        await Sleep(Math.Min(3, seconds / 4));
        result["probeLive"] = await Probe(args, "--probe-live");
        await feeder;
        await Sleep(1);

        if (!standard && await HasDetector())
        {
            // 高機能版: 検出器が読み込めて、届いた行に追いついている
            await WaitJs("detector live", 60, "jmasEngine.state === 'ready' && jmasEngine.fed > 0 && jmasEngine.store.n - jmasEngine.fed < 200");
        }

        if (!real)
        {
            result["fed"] = n;
        }

        result["measuring"] = await PageState();
        result["probeMeasuring"] = await Probe(args);
        await Snapshot(dir, "1-measuring");
        if (outputsTest)
        {
            // 受け口の検査規則(dev.js と同じ): 数式風・カンマ・真偽値・列数の違い・名前の形・同じ列での宣言し直し
            await Eval("""
                for (const m of [
                  {kind: 'table', name: 'selftest', columns: ['A', 'B'], title: 'self test'},
                  {kind: 'table', name: 'selftest', columns: ['A', 'B']},
                  {kind: 'records', name: 'selftest', rows: [[300, '=SUM(1)', 'a,b'], [301, true, 1], [302, 1.5, null], [303, 1], [-1, 1, 2], [304, -7, 'x\ny']]},
                  {kind: 'table', name: 'Bad', columns: ['A']},
                  {kind: 'table', name: 'nodate', columns: ['DATE']},
                  {kind: 'notify', tag: 'selftest', title: 'Self test', text: 'from the Windows self test', i: 300},
                  {kind: 'notify', tag: 'Bad', title: 'x'},
                ]) window.chrome.webview.postMessage(m); 0
                """);
            await Sleep(0.5);
        }

        // アーティファクト(サンプル 300 = CSV の 299 行目)
        await SendArtifact(300, "autotest");
        await Sleep(0.5);
        result["pendingArtifacts"] = artifacts.Count;

        StopMeasurement();
        await Sleep(1);
        var saved = persistence.CurrentFilePath ??
                    Directory.EnumerateFiles(setting.SaveFilePath).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
                    ?? throw new TestTimeout("saved csv");
        result["savedCsv"] = saved;
        var data = DataRows(saved);
        result["savedRows"] = data.Count;
        result["artifactRow"] = data.FindIndex(r => r[0] == "autotest");
        result["savedMode"] = SavedMode(saved);
        result["numGaps"] = NumGaps(data);
        var problems = new List<string>();
        var wantMode = standard ? "Standard" : "Full";
        if (result["savedMode"]!.GetValue<string>() != wantMode)
        {
            problems.Add($"mode {result["savedMode"]} in CSV, expected {wantMode}");
        }

        if (result["numGaps"]!.GetValue<int>() != 0)
        {
            problems.Add($"NUM has {result["numGaps"]} gaps");
        }

        if (result["artifactRow"]!.GetValue<int>() != 299)
        {
            problems.Add($"artifact at row {result["artifactRow"]}, expected 299");
        }

        var det = result["measuring"]?["detector"];
        if (standard && det is not null &&
            (det["state"]?.GetValue<string>() != "off" ||
             !(result["measuring"]?["toast"]?.GetValue<string>() ?? "").StartsWith("No detection", StringComparison.Ordinal)))
        {
            problems.Add("advanced page in Standard: detector should be off with the 'No detection' toast");
        }

        if (data.Count < seconds * 80)
        {
            problems.Add($"only {data.Count} rows for {seconds} s");
        }

        if (outputsTest)
        {
            await Sleep(1);                                   // 表はページの残りを待って 1 秒後に閉じる
            problems.AddRange(await CheckOutputs(saved, data, result));
        }

        if (socket is not null)
        {
            problems.AddRange(await socket.Check(saved, lb_SocketStatus.Text, result));
        }

        // 保存した CSV を再生
        if (real)
        {
            memeLib.disconnectPeripheral();
            await Wait("disconnected", 15, () => phase is Phase.Idle or Phase.DeviceFound);
        }

        phase = Phase.Idle;
        UpdateUiState();
        LoadReplayFile(saved);
        await Sleep(4);
        if (!standard && await HasDetector())
        {
            await WaitJs("detector replay", 120, "jmasEngine.finalSent === true");
        }

        result["replay"] = await PageState();
        result["probeReplay"] = await Probe(args);
        await Snapshot(dir, "2-replay");
        FormSnapshot(dir, "2-replay-form");        // 左の欄が CSV の記録条件(Full・100Hz・±8G・±1000dps)になっている
        result["replayConditions"] = $"{cb_SelectMode.Text} {cb_TransSpeed.Text} {cb_AccelRange.Text} {cb_GyroRange.Text}";
        if (!real && result["replayConditions"]!.GetValue<string>() != $"{wantMode} 100Hz ±8G ±1000dps")
        {
            problems.Add($"conditions after replay-info: {result["replayConditions"]}");
        }

        // 再生中に付けたアーティファクトの書き戻し(Save Artifacts = 500 行目、Disconnect = 600 行目)
        var changed = await ReplayWriteBack(saved, 500, 600, result);
        if (!changed.SequenceEqual([500, 600]))
        {
            problems.Add($"replay write-back changed rows [{string.Join(",", changed)}], expected [500,600]");
        }

        if (outputsTest)
        {
            problems.AddRange(CheckOutputsMove(saved, result));
            DetectorNotifications.SuppressedDialogs = null;
        }

        if (problems.Count > 0)
        {
            throw new CheckFailed(problems);
        }
    }

    /// <summary>
    /// 判定器の表の CSV と通知(--outputs)。表 hve ができ、ページが送った行・通知をアプリが受け、表の NUM がデータ CSV の行と DATE ごと一致し、
    /// 受け口の検査規則の確かめ(selftest の表)が合っているか
    /// </summary>
    private async Task<List<string>> CheckOutputs(string dataCsv, List<string[]> data, JsonObject result)
    {
        var problems = new List<string>();
        var st = outputs.Stats;
        var o = new JsonObject
        {
            ["app"] = new JsonObject
            {
                ["notifyReceived"] = st.NotifyReceived, ["notifyShown"] = st.NotifyShown, ["rowsReceived"] = st.RowsReceived,
                ["rowsNotInData"] = st.RowsNotInData, ["rowsInBackground"] = st.RowsInBackground,
                ["warnings"] = new JsonArray(st.Warnings.Select(w => (JsonNode)w).ToArray()),
            },
            ["notificationsAvailable"] = DetectorNotifications.Available,
            ["notificationsError"] = DetectorNotifications.LastError,
            ["runtimeHelp"] = new JsonArray((DetectorNotifications.SuppressedDialogs ?? []).Select(m => (JsonNode)m).ToArray()),
        };
        var sent = JsonNode.Parse(JsonSerializer.Deserialize<string>(await Eval("JSON.stringify(window.jmasOutputs?.sent ?? {})")) ?? "{}");
        o["pageSent"] = sent;
        // 規則の確かめで送った分(selftest の表 3 行・通知 2 件)を除いて、ページが送った数と比べる
        string[] wantWarnings = ["records selftest: 3 row(s) dropped", "table: bad name \"Bad\"", "table nodate: bad columns", "notify: bad tag \"Bad\""];
        var unexpected = st.Warnings.Where(w => !wantWarnings.Any(w.StartsWith)).ToList();
        var missing = wantWarnings.Where(w => !st.Warnings.Any(x => x.StartsWith(w, StringComparison.Ordinal))).ToList();
        if (unexpected.Count > 0 || missing.Count > 0)
        {
            problems.Add($"outputs warnings: unexpected [{string.Join(" | ", unexpected)}], missing [{string.Join(" | ", missing)}]");
        }

        var pageRecords = sent?["records"]?.GetValue<int>() ?? -1;
        var pageNotify = sent?["notify"]?.GetValue<int>() ?? -1;
        if (pageRecords != st.RowsReceived - 3 + st.RowsNotInData || st.RowsNotInData > 1)
        {
            problems.Add($"table rows: page sent {pageRecords}, app wrote {st.RowsReceived - 3} + {st.RowsNotInData} not in the data CSV");
        }

        if (pageNotify != st.NotifyReceived - 2)
        {
            problems.Add($"notify: page sent {pageNotify}, app received {st.NotifyReceived - 2}");
        }

        // 通知: ランタイムがあれば出せた数が受けた数(規則に合わない 1 件を除く)と同じ。無ければ出さず、案内を 1 回だけ出そうとした
        var helps = DetectorNotifications.SuppressedDialogs?.Count ?? 0;
        if (DetectorNotifications.Available == true && st.NotifyShown != st.NotifyReceived - 1)
        {
            problems.Add($"notifications: shown {st.NotifyShown} of {st.NotifyReceived - 1}");
        }

        if (DetectorNotifications.Available == false && (st.NotifyShown != 0 || helps != 1))
        {
            problems.Add($"no runtime: shown {st.NotifyShown}, help dialogs {helps} (expected 0 and 1)");
        }

        var byNum = data.ToDictionary(r => r[1], r => r[2]);
        var stem = CsvFile.BaseName(dataCsv);
        var files = new JsonArray();
        if (!outputs.LastFiles.Any(f => f.Contains("_hve.", StringComparison.Ordinal)))
        {
            problems.Add("no hve table CSV");
        }

        foreach (var f in outputs.LastFiles)
        {
            var lines = CsvFile.ReadAllLines(f);
            var head = lines.TakeWhile(l => l.StartsWith("//", StringComparison.Ordinal)).ToArray();
            var rows = lines.Skip(head.Length).Where(l => l.Length > 0).Select(l => l.Split(',')).ToList();
            var joined = rows.Count(r => byNum.TryGetValue(r[0], out var d) && d == r[1]);
            var sameBase = Path.GetFileName(f).StartsWith(stem + "_", StringComparison.Ordinal) &&
                           Path.GetDirectoryName(f) == Path.GetDirectoryName(dataCsv) && CsvFile.IsGzip(f) == CsvFile.IsGzip(dataCsv);
            files.Add(new JsonObject
            {
                ["file"] = Path.GetFileName(f), ["header"] = new JsonArray(head.Select(h => (JsonNode)h).ToArray()), ["rows"] = rows.Count,
                ["matchDataNumAndDate"] = joined, ["sameBaseAsData"] = sameBase,
                ["first"] = new JsonArray(rows.Take(3).Select(r => (JsonNode)string.Join(',', r)).ToArray()),
            });
            if (rows.Count == 0 || joined != rows.Count)
            {
                problems.Add($"{Path.GetFileName(f)}: {joined} of {rows.Count} rows match the data CSV's NUM and DATE");
            }

            if (!sameBase)
            {
                problems.Add($"{Path.GetFileName(f)}: not next to {Path.GetFileName(dataCsv)} with the same base name / compression");
            }

            if (f.Contains("_selftest.", StringComparison.Ordinal))
            {
                var cells = rows.Select(r => string.Join(',', r.Skip(2))).ToArray();
                if (!cells.SequenceEqual([",a b", "1.5,", "-7,x y"]))
                {
                    problems.Add($"selftest table cells [{string.Join(" | ", cells)}]");
                }

                if (!head.Contains("// Detector output  : selftest (self test)"))
                {
                    problems.Add("selftest table title");
                }
            }
            else if (f.Contains("_hve.", StringComparison.Ordinal) && head.LastOrDefault() != "//NUM,DATE,HEIGHT_CM,VELOCITY_CM_S")
            {
                problems.Add($"hve header {head.LastOrDefault()}");
            }
        }

        o["files"] = files;
        result["outputs"] = o;
        return problems;
    }

    /// <summary>保存ダイアログでデータ CSV を移したときと同じ扱い(DataFileMoved)で、表も同じフォルダ・同じベース名へ移るか</summary>
    private List<string> CheckOutputsMove(string dataCsv, JsonObject result)
    {
        var dir = Path.Combine(Path.GetDirectoryName(dataCsv) ?? "", "moved");
        Directory.CreateDirectory(dir);
        var to = Path.Combine(dir, "renamed" + CsvFile.MatchingExtension(dataCsv));
        var before = outputs.LastFiles.ToList();
        File.Move(dataCsv, to, overwrite: true);
        outputs.DataFileMoved(dataCsv, to);
        var after = outputs.LastFiles;
        result["outputsMoved"] = new JsonArray(after.Select(f => (JsonNode)Path.GetFileName(f)).ToArray());
        var problems = new List<string>();
        foreach (var (a, b) in before.Zip(after))
        {
            var suffix = Path.GetFileName(a)[CsvFile.BaseName(dataCsv).Length..];
            if (Path.GetDirectoryName(b) != dir || Path.GetFileName(b) != "renamed" + suffix || !File.Exists(b) || File.Exists(a))
            {
                problems.Add($"table not moved with the data CSV: {Path.GetFileName(a)} -> {b}");
            }
        }

        return problems;
    }

    /// <summary>--real: スキャンして --device(アドレス "D6260AC6E90D" か名前の末尾。省略時は最初の端末)に繋ぐ</summary>
    private async Task ConnectReal(string? suffix, JsonObject result)
    {
        var want = suffix?.Replace(":", "").ToUpperInvariant();
        bool Want(MEMEDevice d) => want is null || d.Address.EndsWith(want, StringComparison.Ordinal) ||
                                   d.Name.EndsWith(suffix!, StringComparison.OrdinalIgnoreCase);

        bt_Scan_Click(this, EventArgs.Empty);
        // ライブラリのスキャン(30 秒)が見つけるか、見つけずに終わるまで
        await Wait("device found", 35, () => cb_DeviceList.Items.OfType<MEMEDevice>().Any(Want));
        var device = cb_DeviceList.Items.OfType<MEMEDevice>().First(Want);
        cb_DeviceList.SelectedItem = device;
        if (isScanning)
        {
            bt_Scan_Click(this, EventArgs.Empty);             // スキャンを止める
        }

        result["device"] = $"{device.Name} {device.Address}";
        bt_Connect_Click(this, EventArgs.Empty);
        await WaitConnected("connected");
        await Sleep(2);                                       // 通知の有効化・端末情報の取得を待つ
        result["memeVersion"] = lb_MemeVersion.Text;
    }

    /// <summary>接続を待つ。繋がらなければ左の欄の State(Connect failed / timeout)を理由に添える</summary>
    private async Task WaitConnected(string what)
    {
        try
        {
            await Wait(what, 30, () => phase == Phase.Connected);
        }
        catch (TestTimeout)
        {
            throw new TestTimeout($"{what} ({lb_ConnectionState.Text})");
        }
    }

    /// <summary>CSV の 1 行(ARTIFACT,NUM,DATE,ACC×3,GYRO×3,EOG_L,EOG_R,EOG_H,EOG_V)を受信したサンプルの形に</summary>
    private static AcademicData ToSample(string[] r, int k, bool standard)
    {
        short V(int col) => short.Parse(r[col], System.Globalization.CultureInfo.InvariantCulture);
        if (standard)
        {
            // 同じ値を Standard の形(1 パケットに EOG 2 サンプル)に詰め替える。表示の確かめ用
            return new AcademicStandardData
            {
                Cnt = k % 0x1000, AccX = V(3), AccY = V(4), AccZ = V(5),
                EogL1 = V(9), EogR1 = V(10), EogL2 = V(9), EogR2 = V(10),
                EogH1 = V(11), EogH2 = V(11), EogV1 = V(12), EogV2 = V(12),
            };
        }

        return new AcademicFullData
        {
            Cnt = k % 0x1000, AccX = V(3), AccY = V(4), AccZ = V(5), GyroX = V(6), GyroY = V(7), GyroZ = V(8),
            EogL = V(9), EogR = V(10), EogH = V(11), EogV = V(12),
        };
    }

    #endregion

    #region replay

    private async Task RunReplaySuite(string[] args, string dir, JsonObject result)
    {
        var src = Arg(args, "--csv") ?? throw new ArgumentException("--csv is required");
        // 元のファイルは書き換えない(アーティファクトを書き戻すので写しを使う)
        var csv = Path.Combine(setting.SaveFilePath, Path.GetFileName(src));
        File.Copy(src, csv, overwrite: true);
        var before = DataRows(csv);

        await Wait("page ready", 30, () => web.IsReady);
        result["page"] = web.PageName;
        var sw = Stopwatch.StartNew();
        LoadReplayFile(csv);
        await Sleep(3);
        if (await HasDetector())
        {
            await WaitJs("detector replay", 300, "jmasEngine.finalSent === true");
            result["analysisSeconds"] = Math.Round(sw.Elapsed.TotalSeconds, 1);
        }

        result["replay"] = await PageState();
        result["probeReplay"] = await Probe(args);
        await Snapshot(dir, "replay");

        var problems = new List<string>();
        if (Arg(args, "--expect") is { } expect && await HasDetector())
        {
            // 判定数を比べる(瞬目,EMR,EML[,歩のイベント])
            var want = expect.Split(',').Select(int.Parse).ToArray();
            var det = result["replay"]?["detector"];
            var got = new[] { det?["counts"]?["blink"], det?["counts"]?["emr"], det?["counts"]?["eml"], det?["stepEvents"] }
                .Select(x => x?.GetValue<int>() ?? -1).Take(want.Length).ToArray();
            result["expect"] = new JsonObject { ["want"] = expect, ["got"] = string.Join(",", got) };
            if (!got.SequenceEqual(want))
            {
                problems.Add($"counts {string.Join(",", got)}, expected {expect}");
            }
        }

        // アーティファクトを付けて Save Artifacts(299 行目)と Disconnect(600 行目)で書き戻す
        var changed = await ReplayWriteBack(csv, 299, 600, result);
        result["rows"] = before.Count;
        if (!changed.SequenceEqual([299, 600]))
        {
            problems.Add($"write-back changed rows [{string.Join(",", changed)}], expected [299,600]");
        }

        if (problems.Count > 0)
        {
            throw new CheckFailed(problems);
        }
    }

    #endregion

    #region webcrash(グラフ画面のプロセスが落ちたとき)

    /// <summary>
    /// 計測中・再生中に WebView のプロセスを落とし、読み込み直したページで計測・再生が続いているか
    /// (以前は読み込み直しで start / openReplay が送り直されず、グラフ画面が空のままだった)。
    /// </summary>
    private async Task RunWebCrashSuite(string[] args, string dir, JsonObject result)
    {
        var rows = DataRows(Arg(args, "--csv") ?? throw new ArgumentException("--csv is required"));
        await Wait("page ready", 30, () => web.IsReady);
        phase = Phase.Connected;
        mode = MeasurementMode.Full;
        quality = MEMEQuality.High;
        StartMeasurement();
        var feeder = Feed(rows, 0, 800, standard: false);
        await Sleep(2);
        await CrashPage("live");
        await Sleep(3);
        var live = await PageState();
        result["live"] = live;
        await Snapshot(dir, "crash-live");
        await feeder;
        StopMeasurement();
        await Sleep(1);
        var saved = persistence.CurrentFilePath ?? throw new TestTimeout("saved csv");

        phase = Phase.Idle;
        LoadReplayFile(saved);
        await Sleep(3);
        await CrashPage("replay");
        await Sleep(3);
        var replay = await PageState();
        result["replay"] = replay;
        await Snapshot(dir, "crash-replay");
        EndReplaySession();

        // ステータスの行は、start / openReplay を受けたときだけ計測の名前・ファイル名と計測条件を出す
        var problems = new List<string>();
        var liveStatus = live?["status"]?.GetValue<string>() ?? "";
        var replayStatus = replay?["status"]?.GetValue<string>() ?? "";
        if (!liveStatus.Contains("JINS MEME", StringComparison.Ordinal) || !liveStatus.Contains("100 Hz", StringComparison.Ordinal))
        {
            problems.Add($"live not resumed: {liveStatus}");
        }

        if (!replayStatus.Contains(Path.GetFileName(saved), StringComparison.Ordinal))
        {
            problems.Add($"replay not resumed: {replayStatus}");
        }

        if (problems.Count > 0)
        {
            throw new CheckFailed(problems);
        }
    }

    /// <summary>ページのプロセスを落とし(ProcessFailed が呼ばれる)、読み込み直して ready になるまで待つ</summary>
    private async Task CrashPage(string what)
    {
        var readies = web.ReadyCount;
        // 落ちたページからは返事が来ないので待たない
        _ = web.WebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.crash", "{}")
            .ContinueWith(t => _ = t.Exception, TaskScheduler.Default);
        await Wait($"page ready after crash ({what})", 30, () => web.ReadyCount > readies && web.IsReady);
    }

    #endregion

    #region reconnect(計測中の切断)

    /// <summary>
    /// 計測中に切断されたあと、繋ぎ直して Start Measurement で計測が始まるか。切断した回も停止と同じく締められ
    /// (CSV が分かれ、NUM が続かない)、付けた Artifact が書き戻されるか。
    /// </summary>
    private async Task RunReconnectSuite(string[] args, JsonObject result)
    {
        var real = args.Contains("--real");
        var rows = real ? new List<string[]>() : DataRows(Arg(args, "--csv") ?? throw new ArgumentException("--csv is required (or --real)"));
        await Wait("page ready", 30, () => web.IsReady);
        mode = MeasurementMode.Full;
        quality = MEMEQuality.High;
        for (var round = 1; round <= 2; round++)
        {
            if (real)
            {
                if (round == 1)
                {
                    await ConnectReal(Arg(args, "--device"), result);
                }
                else
                {
                    bt_Connect_Click(this, EventArgs.Empty);     // 一覧に残っている同じ端末へ
                    await WaitConnected($"connected {round}");
                    await Sleep(2);
                }
            }
            else
            {
                phase = Phase.Connected;
            }

            // 画面の Start Measurement と同じ口(以前は切断の後で停止として扱われた)
            bt_Measurement_Click(this, EventArgs.Empty);
            if (phase != Phase.Measuring)
            {
                throw new CheckFailed([$"round {round}: phase {phase} after Start Measurement"]);
            }

            if (real)
            {
                await Sleep(3);
            }
            else
            {
                await Feed(rows, (round - 1) * 300, 300, standard: false);
            }

            if (round == 2)
            {
                break;
            }

            // サンプル 50 = CSV の 49 行目。切断で締めたときに書き戻されるか
            await SendArtifact(50, "cut");
            await Sleep(0.3);
            // 端末側から切られたときと同じく、計測を止めずに切る(画面からは計測中に切断できない)
            if (real)
            {
                memeLib.disconnectPeripheral();
            }
            else
            {
                OnPeripheralDisconnected(memeLib, MEMEStatus.MEMELIB_NG);
            }

            await Wait("disconnected", 15, () => phase is Phase.Idle or Phase.DeviceFound);
            await Sleep(1);
            result["phaseAfterDisconnect"] = phase.ToString();
        }

        StopMeasurement();
        await Sleep(1);
        if (real)
        {
            memeLib.disconnectPeripheral();
            await Wait("disconnected at end", 15, () => phase is Phase.Idle or Phase.DeviceFound);
        }

        // 保存先の CSV(作った順)
        var csvs = Directory.GetFiles(setting.SaveFilePath).Where(CsvFile.IsSupported).OrderBy(File.GetCreationTimeUtc).ToList();
        var problems = new List<string>();
        var files = new JsonArray();
        string? row49 = null;
        foreach (var csv in csvs)
        {
            var data = DataRows(csv);
            var gaps = NumGaps(data);
            var name = Path.GetFileName(csv);
            files.Add(new JsonObject
            {
                ["name"] = name, ["rows"] = data.Count, ["numGaps"] = gaps,
                ["firstNum"] = data.Count > 0 ? data[0][1] : "", ["row49"] = data.Count > 49 ? data[49][0] : "?",
                ["headers"] = CsvFile.ReadAllLines(csv).Count(l => l.StartsWith("//ARTIFACT", StringComparison.Ordinal)),
            });
            row49 ??= data.Count > 49 ? data[49][0] : "?";
            if (gaps != 0)
            {
                problems.Add($"{name}: NUM has {gaps} gaps");
            }
        }

        result["csvs"] = files;
        if (csvs.Count != 2)
        {
            problems.Add($"{csvs.Count} CSV files, expected 2 (one per measurement)");
        }

        if (row49 != "cut")
        {
            problems.Add($"artifact of the cut measurement: {row49}");
        }

        if (problems.Count > 0)
        {
            throw new CheckFailed(problems);
        }
    }

    #endregion

    #region --socket(TCP 出力)

    /// <summary>テストの間だけ TCP 出力を受け取る(--socket)。--socket-stall なら読まずに送信を詰まらせる。</summary>
    private sealed class SocketReceiver : IDisposable
    {
        private readonly System.Net.Sockets.TcpClient client;
        private readonly bool stall;
        private readonly Task<string> reading;

        private SocketReceiver(System.Net.Sockets.TcpClient client, bool stall)
        {
            this.client = client;
            this.stall = stall;
            reading = stall ? Task.FromResult("") : Task.Run(async () =>
            {
                using var reader = new StreamReader(client.GetStream());
                return await reader.ReadToEndAsync();
            });
        }

        public static async Task<SocketReceiver?> ConnectAsync(string[] args)
        {
            if (Arg(args, "--socket") is not { } port)
            {
                return null;
            }

            var stall = args.Contains("--socket-stall");
            // 読まない側は受信の窓を小さくして、送信をすぐ詰まらせる
            var client = new System.Net.Sockets.TcpClient { ReceiveBufferSize = stall ? 256 : 1 << 16 };
            await client.ConnectAsync("127.0.0.1", int.Parse(port, System.Globalization.CultureInfo.InvariantCulture));
            await Sleep(0.5);                                 // 受け付けられる(Accepted)まで
            return new SocketReceiver(client, stall);
        }

        /// <summary>計測を止めた後に呼ぶ。届いたもの(ヘッダと行)が保存した CSV と同じか</summary>
        public async Task<List<string>> Check(string saved, string status, JsonObject result)
        {
            result["socketStatus"] = status;
            if (stall)
            {
                // 詰まったクライアントは切られ、計測(受信)は止まらない(行数・NUM の抜けは呼び出し側が見る)
                return status.Contains("Disconnected", StringComparison.Ordinal) ? [] : [$"stalled client was not dropped: {status}"];
            }

            client.Client.Shutdown(System.Net.Sockets.SocketShutdown.Send);
            await Sleep(1);
            client.Close();                                   // 読み取りを終わらせる(届いた分は読み終えている)
            var text = await reading.ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : "", TaskScheduler.Default);
            // ARTIFACT 列は比べない(ページで付けた Artifact は停止したときに CSV へだけ書き戻すので)
            static string WithoutArtifact(string line) => line.IndexOf(',') is var k and >= 0 ? line[k..] : line;
            var received = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Select(WithoutArtifact).ToArray();
            var file = CsvFile.ReadAllLines(saved).Where(l => l.Length > 0).Select(WithoutArtifact).ToArray();
            result["socketLines"] = received.Length;
            result["csvLines"] = file.Length;
            var firstDiff = Enumerable.Range(0, Math.Min(received.Length, file.Length)).FirstOrDefault(k => received[k] != file[k], -1);
            return received.SequenceEqual(file)
                ? []
                : [$"TCP output ({received.Length} lines) differs from the CSV ({file.Length} lines) at line {firstDiff}"];
        }

        public void Dispose() => client.Dispose();
    }

    #endregion

    #region Display Engine(複数の zip を持ち、1 つだけ使う)

    private async Task RunEnginesSuite(string dir, JsonObject result)
    {
        var store = webContent;
        var work = Path.Combine(dir, "engines");
        if (Directory.Exists(work))
        {
            Directory.Delete(work, recursive: true);
        }

        Directory.CreateDirectory(work);
        const string builtIn = WebContentStore.BuiltInId;
        var std = store.Entries.FirstOrDefault(e => e.IsBuiltIn) ?? throw new TestTimeout("no built-in entry");
        var bundled = Path.Combine(store.Root, "bundled");

        static void CopyDir(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (var f in Directory.GetFiles(from))
            {
                File.Copy(f, Path.Combine(to, Path.GetFileName(f)));
            }

            foreach (var d in Directory.GetDirectories(from))
            {
                CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
            }
        }

        // 同梱の標準版の中身を写し、manifest の name・title・version だけ変える
        string MakeTree(string name, string version)
        {
            var src = Path.Combine(work, $"{name}-{version}");
            CopyDir(bundled, src);
            var mPath = Path.Combine(src, "manifest.json");
            var m = JsonNode.Parse(File.ReadAllText(mPath))!.AsObject();
            m["name"] = name;
            m["title"] = name.ToUpperInvariant();
            m["version"] = version;
            File.WriteAllText(mPath, m.ToJsonString());
            return src;
        }

        string MakeZip(string name, string version)
        {
            var src = MakeTree(name, version);
            var zip = src + ".zip";
            System.IO.Compression.ZipFile.CreateFromDirectory(src, zip);
            return zip;
        }

        // ページ(WebBridge.Origin)が読んでいる manifest の name
        async Task<string> ServedName()
        {
            var readies = web.ReadyCount;
            web.Load();
            await Wait("page ready", 30, () => web.ReadyCount > readies && web.IsReady);
            var raw = await Eval("(() => { const x = new XMLHttpRequest(); x.open('GET', 'manifest.json', false); x.send(); return JSON.parse(x.responseText).name; })()");
            return JsonSerializer.Deserialize<string>(raw) ?? "";
        }

        var problems = new List<string>();
        var steps = new JsonArray();
        void Check(string what, bool ok, object? detail = null)
        {
            steps.Add(new JsonObject { ["step"] = what, ["ok"] = ok, ["detail"] = detail?.ToString() ?? "" });
            if (!ok)
            {
                problems.Add(what);
            }
        }

        string Names() => string.Join(",", store.Entries.Select(e => e.IsBuiltIn ? "*" + e.Manifest.Name : e.Manifest.Name));
        var stdName = "*" + std.Manifest.Name;

        // 始めは取り込んだものを持たない状態にする(元のものは呼び出し側が退避して戻す)
        foreach (var e in store.Entries.Where(e => !e.IsBuiltIn).ToArray())
        {
            store.Remove(e.Id);
        }

        store.Activate(builtIn);
        Check("built-in only, first and active", Names() == stdName && store.ActiveId == builtIn, Names());
        Check("built-in cannot be removed", !store.Remove(builtIn) && store.Entries[0].IsBuiltIn);
        Check("page serves built-in", await ServedName() == std.Manifest.Name);

        var zipA1 = MakeZip("enginea", "1.0.0");
        var (a, aOutcome, aActive) = store.Add(zipA1);
        var (b, _, _) = store.Add(MakeZip("engineb", "1.0.0"));
        Check("add keeps active, appends in order", aOutcome == WebContentStore.AddOutcome.Added && !aActive &&
              store.ActiveId == builtIn && Names() == $"{stdName},enginea,engineb", Names());

        // 同じファイルをもう一度(別の場所に写したものでも)取り込んでも重ならない
        var copyA1 = Path.Combine(work, "copy-of-enginea.zip");
        File.Copy(zipA1, copyA1);
        var (same1, o1, _) = store.Add(zipA1);
        var (same2, o2, _) = store.Add(copyA1);
        Check("same file is not added twice", o1 == WebContentStore.AddOutcome.AlreadyAdded &&
              o2 == WebContentStore.AddOutcome.AlreadyAdded && same1.Id == a.Id && same2.Id == a.Id &&
              Names() == $"{stdName},enginea,engineb", Names());

        store.Activate(a.Id);
        Check("activate one", store.ActiveId == a.Id && setting.WebContentActive == a.Id &&
              store.Manifest?.Name == "enginea" && Path.GetFileName(store.ActiveDir) == a.Id);
        Check("page serves the active one", await ServedName() == "enginea");

        var (a2, a2Outcome, replacedActive) = store.Add(MakeZip("enginea", "2.0.0"));
        Check("same name replaces (same id, still active, new version)", a2.Id == a.Id &&
              a2Outcome == WebContentStore.AddOutcome.Replaced && replacedActive && store.Entries.Count == 3 &&
              store.ActiveId == a.Id && store.Manifest?.Version == "2.0.0", Names());
        Check("replacing keeps the position in the list", Names() == $"{stdName},enginea,engineb", Names());

        var (_, _, replacedB) = store.Add(MakeZip("engineb", "2.0.0"));
        Check("replacing an inactive one does not touch the active one", !replacedB && store.ActiveId == a.Id);

        Check("remove active falls back to built-in", store.Remove(a.Id) && store.ActiveId == builtIn &&
              setting.WebContentActive == builtIn && Names() == $"{stdName},engineb", Names());
        Check("removed folder is gone", !Directory.Exists(Path.Combine(store.Root, "zips", a.Id)) &&
              !File.Exists(Path.Combine(store.Root, "zips", a.Id + ".sha256")));
        var (readd, readdOutcome, _) = store.Add(zipA1);
        Check("a removed zip can be added again", readdOutcome == WebContentStore.AddOutcome.Added && readd.Id != a.Id, Names());
        store.Remove(readd.Id);
        Check("page serves built-in after remove", await ServedName() == std.Manifest.Name);

        store.Activate(b.Id);
        store.Prepare();
        Check("active survives restart", store.ActiveId == b.Id && store.Manifest?.Name == "engineb");

        setting.WebContentActive = "no-such-id";
        store.Prepare();
        Check("unknown active id falls back to built-in", store.ActiveId == builtIn && setting.WebContentActive == builtIn);

        var refused = false;
        try
        {
            store.Add(Path.Combine(work, "enginea-1.0.0", "manifest.json"));
        }
        catch (InvalidDataException)
        {
            refused = true;
        }

        Check("bad file refused, nothing changes", refused && Names() == $"{stdName},engineb", Names());

        // 以前の版の形: custom\ に 1 つだけ、WebContentSource = "custom"
        var legacySrc = MakeTree("legacy", "0.9.0");
        CopyDir(legacySrc, Path.Combine(store.Root, "custom"));   // 作業フォルダ(Z: など)から別のドライブへは Move できない
        setting.WebContentSource = "custom";
        setting.WebContentActive = builtIn;
        store.Prepare();
        Check("legacy custom migrated and active", store.Manifest?.Name == "legacy" && Names().Contains("legacy") &&
              Names().Contains("engineb") && !Directory.Exists(Path.Combine(store.Root, "custom")) &&
              setting.WebContentSource is null && setting.WebContentActive == store.ActiveId, Names());
        Check("page serves migrated one", await ServedName() == "legacy");

        // 取り込んだ zip のページが 1 回落ちただけでは戻さず、60 秒以内に 2 回落ちたら標準版へ戻して知らせる
        var legacyId = store.ActiveId;
        store.TakeFallbackNotice();
        lastEngineFallback = null;
        await CrashPage("engine crash 1");
        Check("one page crash keeps the zip", store.ActiveId == legacyId && lastEngineFallback is null);
        await CrashPage("engine crash 2");
        Check("second page crash falls back to built-in", store.ActiveId == builtIn &&
              lastEngineFallback?.Contains("0.9.0") == true && store.TakeFallbackNotice() is not null, lastEngineFallback);
        Check("page serves built-in after fallback", await ServedName() == std.Manifest.Name);

        result["engines"] = steps;
        if (problems.Count > 0)
        {
            throw new CheckFailed(problems);
        }
    }

    #endregion

    #region zip の検査(悪い zip を断るか)

    private async Task RunZipSuite(string badDir, JsonObject result)
    {
        var zips = Directory.Exists(badDir)
            ? Directory.GetFiles(badDir, "*.zip").OrderBy(Path.GetFileName, StringComparer.Ordinal).ToArray()
            : [];
        if (zips.Length == 0)
        {
            throw new TestTimeout("no zips in --badzips");
        }

        // 展開先の外に書かれたら見つかるように、WebContent の上と zip のフォルダの中身を覚えておく
        var watch = new[] { Path.GetDirectoryName(webContent.Root)!, webContent.Root, badDir };
        HashSet<string> Listing() => watch
            .SelectMany(d => Directory.Exists(d) ? Directory.EnumerateFileSystemEntries(d) : [])
            .Where(p => !p.Contains(@"\tmp-") && !p.Contains(@"\old-") && !p.EndsWith(@"\zips", StringComparison.Ordinal) &&
                        !p.EndsWith("webview-debug.log", StringComparison.Ordinal))
            .ToHashSet();

        var rows = new JsonArray();
        var allOk = true;
        foreach (var zip in zips)
        {
            var name = Path.GetFileName(zip);
            var before = (webContent.ActiveId, string.Join("|", webContent.Entries));
            var beforeFiles = Listing();
            var sw = Stopwatch.StartNew();
            var row = new JsonObject { ["zip"] = name };
            var accepted = false;
            try
            {
                row["accepted"] = webContent.ImportAndActivate(zip).DisplayName;
                accepted = true;
            }
            catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                row["refused"] = e.Message;
                row["unchanged"] = before == (webContent.ActiveId, string.Join("|", webContent.Entries));
            }

            row["ms"] = sw.ElapsedMilliseconds;
            var extra = Listing().Except(beforeFiles).Order().ToArray();
            if (extra.Length > 0)
            {
                row["writtenOutside"] = new JsonArray(extra.Select(x => (JsonNode)x).ToArray());
            }

            var leftovers = Directory.EnumerateDirectories(webContent.Root, "tmp-*").ToArray();
            if (leftovers.Length > 0)
            {
                row["tmpLeft"] = new JsonArray(leftovers.Select(x => (JsonNode)x).ToArray());
            }

            var wantAccept = name.StartsWith("good", StringComparison.Ordinal);
            var ok = accepted == wantAccept && extra.Length == 0 && leftovers.Length == 0 &&
                     (wantAccept || row["unchanged"]?.GetValue<bool>() == true);
            row["ok"] = ok;
            allOk &= ok;
            rows.Add(row);
        }

        result["zips"] = rows;
        result["allOk"] = allOk;
        web.Load();
        await Sleep(0.5);
        if (!allOk)                                   // どれか 1 つでも NG なら、組全体も ok にしない
        {
            throw new CheckFailed(rows.Where(r => r?["ok"]?.GetValue<bool>() != true).Select(r => "zip " + r!["zip"]));
        }
    }

    #endregion
}
#endif
