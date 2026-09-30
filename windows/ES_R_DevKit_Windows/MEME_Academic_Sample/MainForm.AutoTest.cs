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
///                              [--mode full|standard] [--seconds 20] [--badzips &lt;dir&gt;] [--probe &lt;JS の式 | @ファイル&gt;] [--probe-live &lt;同&gt;]
///                              [--expect &lt;瞬目,EMR,EML[,歩のイベント]&gt;] [--real [--device &lt;アドレスか名前の末尾&gt;]]
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
/// --zip は始める前に設定の Display Engine と同じ経路(WebContentStore.ImportZip)で読み込み、終わったら元に戻す
/// (もともと選んだ zip を使っていたら、それを退避しておいて戻す。zip の組も同じ)。
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
        var wasCustom = webContent.Source == WebContentStore.ContentSource.Custom;
        var usesZips = Arg(args, "--zip") is not null || Arg(args, "--suite") == "zip";
        var kept = usesZips && wasCustom ? KeepCustom() : null;   // 取り込みで上書きされる前に、選んでいた zip を退避
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
        if (usesZips)
        {
            // 読み込んだ zip を残さない。もともと選んだ zip を使っていたらそれに戻す
            if (kept is not null)
            {
                RestoreCustom(kept);
            }
            else
            {
                webContent.UseBundled();
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

    /// <summary>選んでいた zip の展開先(custom)を一時フォルダへ写す</summary>
    private string? KeepCustom()
    {
        var src = webContent.CustomDirForTest;
        var dst = Path.Combine(Path.GetTempPath(), "autotest-custom-" + Guid.NewGuid().ToString("N"));
        try
        {
            CopyDir(src, dst);
            return dst;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"[AutoTest] keep custom: {e.Message}");
            return null;
        }

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
    }

    /// <summary>KeepCustom で写したものを custom へ戻し、選んだ zip を使う設定に戻す</summary>
    private void RestoreCustom(string kept)
    {
        var dst = webContent.CustomDirForTest;
        try
        {
            if (Directory.Exists(dst))
            {
                Directory.Delete(dst, recursive: true);
            }

            Directory.Move(kept, dst);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"[AutoTest] restore custom: {e.Message}");
        }

        setting.WebContentSource = "custom";
        setting.Save();
        webContent.ReloadForTest();
    }

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
        result["page"] = web.PageName;
        result["real"] = real;
        await Snapshot(dir, "0-idle");

        if (real)
        {
            await ConnectReal(Arg(args, "--device"), result);
        }
        else
        {
            phase = Phase.Connected;                          // BLE は繋がっていないので端末への設定は NG で素通りする
        }

        // Full(または Standard)・100Hz・±8G・±1000dps で計測
        mode = standard ? MEMEMode.Standard : MEMEMode.Full;
        quality = MEMEQuality.High;
        accelRange = MEMEAccelRange.Range8G;
        gyroRange = MEMEGyroRange.Range1000dps;
        StartMeasurement();
        result["phase"] = phase.ToString();

        var rows = real ? new List<string[]>() : DataRows(csv!);
        var n = real ? 0 : Math.Min(rows.Count, (int)(seconds * 100));
        var feeder = real ? Task.Delay(TimeSpan.FromSeconds(seconds)) : Task.Run(async () =>
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

        // 再生中に付けたアーティファクトの書き戻し(Save Artifacts = 500 行目、Disconnect = 600 行目)
        var changed = await ReplayWriteBack(saved, 500, 600, result);
        if (!changed.SequenceEqual([500, 600]))
        {
            problems.Add($"replay write-back changed rows [{string.Join(",", changed)}], expected [500,600]");
        }

        if (problems.Count > 0)
        {
            throw new CheckFailed(problems);
        }
    }

    /// <summary>--real: スキャンして --device(アドレス "D6260AC6E90D" か名前の末尾。省略時は最初の端末)に繋ぐ</summary>
    private async Task ConnectReal(string? suffix, JsonObject result)
    {
        var want = suffix?.Replace(":", "").ToUpperInvariant();
        bool Want(MEMEDevice d) => want is null || d.Address.EndsWith(want, StringComparison.Ordinal) ||
                                   d.Name.EndsWith(suffix!, StringComparison.OrdinalIgnoreCase);

        bt_Scan_Click(this, EventArgs.Empty);
        await Wait("device found", 30, () => cb_DeviceList.Items.OfType<MEMEDevice>().Any(Want));
        var device = cb_DeviceList.Items.OfType<MEMEDevice>().First(Want);
        cb_DeviceList.SelectedItem = device;
        if (isScanning)
        {
            bt_Scan_Click(this, EventArgs.Empty);             // スキャンを止める
        }

        result["device"] = $"{device.Name} {device.Address}";
        bt_Connect_Click(this, EventArgs.Empty);
        await Wait("connected", 30, () => phase == Phase.Connected);
        await Sleep(2);                                       // 通知の有効化・端末情報の取得を待つ
        result["memeVersion"] = lb_MemeVersion.Text;
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
        if (!allOk)                                   // どれか 1 つでも NG なら、組全体も ok にしない
        {
            throw new CheckFailed(rows.Where(r => r?["ok"]?.GetValue<bool>() != true).Select(r => "zip " + r!["zip"]));
        }
    }

    #endregion
}
#endif
