#if DEBUG
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using MEMELib_Academic;
using Microsoft.Web.WebView2.Core;
using MEME_Academic_Sample.Services;

namespace MEME_Academic_Sample;

/// <summary>
/// Debug ビルドだけの自己テスト(Mac 版 DebugAutoTest.swift に対応)。画面を操作できない環境(Mac から Parallels の
/// Windows を動かすときなど)から、グラフ画面が動いていることを確かめる。結果(result.json)とスナップショット(PNG)を
/// 渡したフォルダに書いて、アプリを閉じる。
///
///     JINS_MEME_DataLogger.exe --autotest &lt;dir&gt; [--suite live|replay|zip|settings] [--csv &lt;golden の CSV&gt;] [--zip &lt;zip&gt;]
///                              [--mode full|standard] [--seconds 20] [--badzips &lt;dir&gt;] [--probe &lt;JS の式 | @ファイル&gt;]
///
/// - live(既定): 実機の代わりに --csv の行を 100 Hz で受信の口(HandleSample)へ流して「計測 → アーティファクト → 停止 →
///   保存した CSV を再生」を回す(BLE には触らない)。--mode standard は同じ値を Standard の形に詰め替えて流す。
/// - replay: --csv の写しを再生し(元のファイルは書き換えない)、高機能版なら最後まで解析し終えるのを待つ。
///   アーティファクトを付けて Save Artifacts で書き戻されることも見る。
/// - zip: --badzips の中の zip を 1 つずつ読み込み、名前が good で始まるものは通り、それ以外は断られて今の中身が変わらず、
///   展開先の外に何も書かれないことを見る(悪い zip は webview/tools/make_bad_zips.py が作る)。
/// --zip は始める前に設定の Display Engine と同じ経路(WebContentStore.ImportZip)で読み込み、終わったら元に戻す。
/// 保存先はテスト用のフォルダ(&lt;dir&gt;\csv)に切り替え、終わったら戻す。
/// </summary>
public partial class MainForm
{
    private sealed class TestTimeout(string what) : Exception($"timeout: {what}");

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
        var wasCustom = webContent.Source == WebContentStore.ContentSource.Custom;
        var sw = Stopwatch.StartNew();
        try
        {
            if (web.InitError is { } err)
            {
                throw new InvalidOperationException(err);
            }

            if (Arg(args, "--zip") is { } zip)
            {
                result["zip"] = webContent.ImportZip(zip).DisplayName;
                web.Load();
            }

            switch (Arg(args, "--suite") ?? "live")
            {
                case "zip":
                    await RunZipSuite(Arg(args, "--badzips") ?? "", result);
                    break;
                case "settings":
                    // 設定画面の見た目(Display Engine の欄)を撮る。計測中の形(zip の切り替え不可)も撮る
                    foreach (var (name, can) in new[] { ("settings", true), ("settings-busy", false) })
                    {
                        using var form = new SettingsForm(setting, webContent, can);
                        form.Show(this);
                        await Sleep(1);
                        using var bmp = new Bitmap(form.Width, form.Height);
                        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                        bmp.Save(Path.Combine(dir, name + ".png"));
                        form.Close();
                    }

                    break;
                case "replay":
                    await RunReplaySuite(args, dir, result);
                    break;
                default:
                    await RunLiveSuite(args, dir, result);
                    break;
            }

            result["ok"] = true;
        }
        catch (Exception e)
        {
            result["ok"] = false;
            result["error"] = e.ToString();
        }

        result["seconds"] = Math.Round(sw.Elapsed.TotalSeconds, 1);
        await File.WriteAllTextAsync(Path.Combine(dir, "result.json"),
            result.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        setting.SaveFilePath = savedPath;
        setting.ShowSaveFileDialog = savedDialog;
        if ((Arg(args, "--zip") is not null || Arg(args, "--suite") == "zip") && !wasCustom)
        {
            webContent.UseBundled();   // 読み込んだ zip を残さない(もともと選んだ zip を使っていたらそのまま)
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
                                             rows: jmasEngine.store?.n, final: jmasEngine.finalSent, counts: jmasEngine.counts} : null})
            """;
        var raw = await Eval(js);
        return JsonNode.Parse(JsonSerializer.Deserialize<string>(raw) ?? "null");
    }

    /// <summary>--probe に書いた JS の式をページで評価した値(調べもの用。Promise も待つ)</summary>
    private async Task<JsonNode?> Probe(string[] args)
    {
        if (Arg(args, "--probe") is not { } expr)
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

    #endregion

    #region live(受信の代わりに CSV を流す)

    private async Task RunLiveSuite(string[] args, string dir, JsonObject result)
    {
        var csv = Arg(args, "--csv") ?? throw new ArgumentException("--csv is required");
        var standard = Arg(args, "--mode") == "standard";
        var seconds = double.Parse(Arg(args, "--seconds") ?? "20", System.Globalization.CultureInfo.InvariantCulture);

        await Wait("page ready", 30, () => web.IsReady);
        result["page"] = web.PageName;
        await Snapshot(dir, "0-idle");

        // Full(または Standard)・100Hz・±8G・±1000dps で「計測」(BLE は繋がっていないので端末への設定は NG で素通りする)
        mode = standard ? MEMEMode.Standard : MEMEMode.Full;
        quality = MEMEQuality.High;
        accelRange = MEMEAccelRange.Range8G;
        gyroRange = MEMEGyroRange.Range1000dps;
        phase = Phase.Connected;
        StartMeasurement();
        result["phase"] = phase.ToString();

        var rows = DataRows(csv);
        var n = Math.Min(rows.Count, (int)(seconds * 100));
        var feeder = Task.Run(async () =>
        {
            var t0 = Stopwatch.StartNew();
            for (var k = 0; k < n; k++)
            {
                // 実時間で流す(0.01 秒ごと。遅れたら追いつくまでまとめて)
                var due = k * 10.0;
                var wait = due - t0.Elapsed.TotalMilliseconds;
                if (wait > 2)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(wait));
                }

                HandleSample(ToSample(rows[k], k, standard));
            }
        });
        await feeder;
        await Sleep(1);

        if (!standard && await HasDetector())
        {
            // 高機能版: 検出器が読み込めて、届いた行に追いついている
            await WaitJs("detector live", 60, "jmasEngine.state === 'ready' && jmasEngine.fed > 0 && jmasEngine.store.n - jmasEngine.fed < 200");
        }

        result["fed"] = n;
        result["measuring"] = await PageState();
        result["probeMeasuring"] = await Probe(args);
        await Snapshot(dir, "1-measuring");

        // アーティファクト(サンプル 300 = CSV の 299 行目)
        await SendArtifact(300, "autotest");
        await Sleep(0.5);
        result["pendingArtifacts"] = pendingArtifacts.Count;

        StopMeasurement();
        await Sleep(1);
        var saved = persistence.CurrentFilePath ??
                    Directory.EnumerateFiles(setting.SaveFilePath).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
                    ?? throw new TestTimeout("saved csv");
        result["savedCsv"] = saved;
        var data = DataRows(saved);
        result["savedRows"] = data.Count;
        result["artifactRow"] = data.FindIndex(r => r[0] == "autotest");

        // 保存した CSV を再生
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
        EndReplaySession();
        await Sleep(0.5);
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

        // アーティファクトを付けて Save Artifacts(再生中の書き戻し)。i はデータ行の番号そのまま
        await SendArtifact(299, "autotest");
        await Sleep(0.5);
        bt_SaveArtifacts.PerformClick();
        var after = DataRows(csv);
        var changed = Enumerable.Range(0, Math.Min(before.Count, after.Count))
            .Where(k => string.Join(',', before[k]) != string.Join(',', after[k])).ToList();
        result["rows"] = after.Count;
        result["changedRows"] = new JsonArray(changed.Select(k => (JsonNode)k).ToArray());
        result["row299"] = string.Join(',', after[299]);
        EndReplaySession();
        await Sleep(0.5);
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
            .Where(p => !p.Contains(@"\tmp-") && !p.Contains(@"\old-") && !p.EndsWith(@"\custom", StringComparison.Ordinal) &&
                        !p.EndsWith("webview-debug.log", StringComparison.Ordinal))
            .ToHashSet();

        var rows = new JsonArray();
        var allOk = true;
        foreach (var zip in zips)
        {
            var name = Path.GetFileName(zip);
            var before = (webContent.Source, webContent.Manifest);
            var beforeFiles = Listing();
            var sw = Stopwatch.StartNew();
            var row = new JsonObject { ["zip"] = name };
            var accepted = false;
            try
            {
                row["accepted"] = webContent.ImportZip(zip).DisplayName;
                accepted = true;
            }
            catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                row["refused"] = e.Message;
                row["unchanged"] = before == (webContent.Source, webContent.Manifest);
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
    }

    #endregion
}
#endif
