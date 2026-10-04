using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MEMELib_Academic;

namespace MEME_Academic_Sample.Services;

/// <summary>
/// グラフ画面の判定器から届く通知と演算結果の表(ページ → アプリの notify / table / records)。
/// 仕様は webview/BRIDGE.md の Detector notifications and tables。Android の DetectorOutputs.kt・Mac の DetectorOutputs.swift と同じ作り。
/// 規則(名前・列名の形、上限、文字列の扱い、ライブの間だけ受ける、同じ tag は 2 秒に 1 件)は webview/common/dev.js の
/// 受け口の真似と同じ。**アプリは判定器の中身を知らない**(何を通知する・何列書くかはページが決める)。
///
/// - 受け付けるのは <see cref="Start"/>(計測の開始)から <see cref="Stop"/> まで。再生の解析中に届いたものは捨てる。
/// - 通知: <see cref="DetectorNotifications"/>(Windows App SDK の AppNotificationManager)。同じ tag は 1 件に上書き
///   (上書きでも毎回出る)、2 秒に 1 件まで(超えたぶんは最後の 1 件だけ 2 秒後に出す)。tag は 1 計測 16 まで。
/// - 表: 1 つを CSV 1 本に。データ CSV と同じベース名 + "_&lt;name&gt;"、同じフォルダ・同じ圧縮。最初の行が届いたときに作り、
///   100 行ごとに追記する(gzip は追記ごとに 1 メンバー。データ CSV と同じ)。改行はデータ CSV と同じ CRLF。
/// - **番号**: ページの i はアプリのサンプル番号(計測開始から 0, 1, 2 …。先頭パケットも数える。MainForm.liveSampleIndex)で、
///   データ CSV の NUM(端末のカウンタから数えた番号。パケットが落ちると飛ぶ)とは違う。表の NUM 列はデータ CSV の NUM に直して書き、
///   DATE はその行の DATE(直近 30 分を覚えておく)。i → NUM の差はパケットが落ちたときだけ変わるので、変わり目だけ全部覚えておく。
///   データ CSV に行の無いサンプル(先頭パケット)の行は書かない。
///
/// <see cref="RecordSample"/> は受信スレッド、それ以外は UI スレッドから呼ぶ(WebView2 のメッセージは UI スレッドで来る)。
/// </summary>
public sealed partial class DetectorOutputs
{
    /// <summary>start の features に入れる(このアプリが受け付けるページ → アプリの追加メッセージ)</summary>
    public static readonly string[] Features = ["notify", "records"];

    private const int MaxMessage = 64_000;
    private const int MaxTags = 16;
    private const int MaxTables = 16;
    private const int TagIntervalMs = 2000;
    private const int FlushRowCount = 100;
    private const int DateKeepSeconds = 30 * 60;

    [GeneratedRegex("^[a-z][a-z0-9_]{0,31}$")]
    private static partial Regex NameRe();

    [GeneratedRegex("^[A-Z][A-Z0-9_]{0,31}$")]
    private static partial Regex ColumnRe();

    private readonly Lock gate = new();            // 受信スレッド(RecordSample)と UI スレッドの間
    private readonly Control ui;
    private bool active;
    private Func<string> pageName = () => "";
    private Func<string?> dataFileSource = () => null;
    private string? knownDataFile;

    // サンプル番号 i → (NUM, DATE)。直近 30 分(i % 容量 の位置に置く)
    private int[] keyI = [];
    private int[] nums = [];
    private DateTime[] dates = [];
    // i → NUM の差(NUM − i)の変わり目。30 分より古い i の NUM もこれで分かる
    private readonly List<(int I, int D)> offsets = [];

    private readonly Dictionary<string, Table> tables = [];
    private readonly List<string> tableOrder = [];
    private readonly Dictionary<string, TagState> tags = [];

    /// <param name="ui">2 秒待ちの通知と、通知のクリックでの前面化を UI スレッドへ戻すため</param>
    public DetectorOutputs(Control ui) => this.ui = ui;

    /// <summary>今の計測の番号(<see cref="Stop"/> に渡して、前の計測の遅れた stop で次の計測を閉じないように)</summary>
    public int Session { get; private set; }

    /// <summary>アプリが前に出ているか(裏の間に届いたかを数えるだけ)</summary>
    public bool Foreground { get; set; } = true;

    /// <summary>最後に閉じた計測で作った表の CSV(保存ダイアログでデータ CSV と一緒に移す・自己テスト用)</summary>
    public List<string> LastFiles { get; private set; } = [];

    /// <summary>確かめる用(自己テスト)</summary>
    public sealed class Counters
    {
        public int NotifyReceived, NotifyShown, RowsReceived, RowsInBackground, NotifyInBackground;

        /// <summary>データ CSV に行の無いサンプル(先頭パケット = i 0 は CSV に書かない)の行。NUM が無いので書かない</summary>
        public int RowsNotInData;

        public readonly List<string> Warnings = [];
    }

    public Counters Stats { get; private set; } = new();

    /// <summary>計測の始まり。前の計測の表がまだ開いていれば閉じる。cps で i → DATE を覚える数(30 分ぶん)を決める</summary>
    /// <param name="dataFile">データ CSV のパス(最初の書き出しで作られる。それまでは null)</param>
    public int Start(int cps, Func<string?> dataFile, Func<string> page)
    {
        if (active)
        {
            Stop();
        }

        lock (gate)
        {
            Session++;
            dataFileSource = dataFile;
            knownDataFile = null;
            pageName = page;
            var cap = DateKeepSeconds * Math.Max(cps, 1);
            keyI = Enumerable.Repeat(-1, cap).ToArray();
            nums = new int[cap];
            dates = new DateTime[cap];
            offsets.Clear();
            active = true;
        }

        tables.Clear();
        tableOrder.Clear();
        foreach (var t in tags.Values)
        {
            t.Timer?.Dispose();
        }

        tags.Clear();
        LastFiles = [];
        Stats = new Counters();
        return Session;
    }

    /// <summary>データ CSV に 1 行書いた(受信スレッド。i = ページへ渡したサンプル番号、num = その行の NUM、utc = その行の DATE)</summary>
    public void RecordSample(int i, int num, DateTime utc)
    {
        lock (gate)
        {
            if (!active || i < 0 || keyI.Length == 0)
            {
                return;
            }

            var k = i % keyI.Length;
            keyI[k] = i;
            nums[k] = num;
            dates[k] = utc;
            if (offsets.Count == 0 || offsets[^1].D != num - i)
            {
                offsets.Add((i, num - i));
            }
        }
    }

    /// <summary>i の行の (NUM, DATE)。データ CSV に書かれていない i なら null。DATE は直近 30 分だけ</summary>
    private (int Num, DateTime? Date)? Lookup(int i)
    {
        lock (gate)
        {
            if (keyI.Length > 0 && keyI[i % keyI.Length] == i)
            {
                var k = i % keyI.Length;
                return (nums[k], dates[k]);
            }

            // 30 分より古い: 差の変わり目から(i より前の最後の変わり目の差)
            if (offsets.Count == 0 || i < offsets[0].I)
            {
                return null;
            }

            int lo = 0, hi = offsets.Count - 1;
            while (lo < hi)
            {
                var mid = (lo + hi + 1) / 2;
                if (offsets[mid].I <= i) { lo = mid; } else { hi = mid - 1; }
            }

            return (i + offsets[lo].D, null);
        }
    }

    /// <summary>データ CSV(分かったら覚えておく。計測を締めた後で表を閉じるときも同じ場所を使う)</summary>
    private string? DataFile => knownDataFile ??= dataFileSource();

    /// <summary>計測を締めるとき(データ CSV を書き切った直後)に呼ぶ。表はページの残りを待ってから閉じるので、その場所を覚えておく</summary>
    public void NoteDataFile() => _ = DataFile;

    /// <summary>
    /// 計測の終わり。表の残りを書いて閉じ、作ったファイル(行のあった表)を返す。session を渡したときは、
    /// それが今の計測のときだけ閉じる(停止から少し待って閉じる間に、次の計測が始まっていることがある)。
    /// </summary>
    public List<string> Stop(int? session = null)
    {
        lock (gate)
        {
            if (!active || (session is { } s && s != Session))
            {
                return [];
            }

            active = false;
        }

        foreach (var t in tags.Values)
        {
            t.Timer?.Dispose();                   // 2 秒待ちの通知は出さない(計測は終わった)
        }

        tags.Clear();
        LastFiles = tableOrder.Select(n => tables[n].Close()).OfType<string>().ToList();
        Log($"stop: tables {string.Join(", ", tableOrder.Select(n => $"{n}={tables[n].Rows}"))}, notify {Stats.NotifyReceived}/{Stats.NotifyShown}, warnings {Stats.Warnings.Count}");
        return LastFiles;
    }

    /// <summary>
    /// 保存ダイアログでデータ CSV を移したとき、表の CSV も同じフォルダ・同じベース名へ移す。
    /// まだ閉じていなければ(ページの残りを待つ間に保存した)ここで閉じる。
    /// </summary>
    public void DataFileMoved(string source, string destination)
    {
        if (active && DataFile is { } df && string.Equals(Path.GetFullPath(df), Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase))
        {
            Stop();
        }

        var oldStem = CsvFile.BaseName(source);
        var newStem = CsvFile.BaseName(destination);
        var dir = Path.GetDirectoryName(destination) ?? "";
        LastFiles = LastFiles.Select(file =>
        {
            var name = Path.GetFileName(file);
            if (!name.StartsWith(oldStem + "_", StringComparison.Ordinal))
            {
                return file;
            }

            var to = Path.Combine(dir, newStem + name[oldStem.Length..]);
            try
            {
                File.Move(file, to, overwrite: true);
                return to;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Log($"could not move {name}: {e.Message}");
                return file;
            }
        }).ToList();
    }

    /// <summary>表ごとの結果(自己テスト用。<see cref="Stop"/> の後も次の <see cref="Start"/> まで残る)</summary>
    public IEnumerable<(string Name, string[] Columns, int Rows, string? File, bool Bad)> TableSummaries =>
        tableOrder.Select(n => tables[n]).Select(t => (t.Name, t.Columns, t.Rows, t.File, t.Bad));

    private void Warn(string text)
    {
        Stats.Warnings.Add(text);
        Log(text);
    }

    private static void Log(string text) => System.Diagnostics.Debug.WriteLine($"[DetectorOutputs] {text}");

    // ------------------------------------------------------------------ ページ → アプリ

    /// <summary>WebBridge から(kind は notify / table / records。UI スレッド)</summary>
    public void Receive(JsonElement body, int rawLength)
    {
        var kind = Str(body, "kind") ?? "";
        if (!active)
        {
            Log($"{kind}: not in a live measurement, ignored");
            return;
        }

        if (rawLength > MaxMessage)
        {
            Warn($"{kind}: message over {MaxMessage} characters, ignored");
            return;
        }

        switch (kind)
        {
            case "notify":
                Notify(body);
                break;
            case "table":
                DeclareTable(body);
                break;
            case "records":
                Records(body);
                break;
        }
    }

    private static string? Str(JsonElement o, string key) =>
        o.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool IsNull(JsonElement o, string key) =>
        !o.TryGetProperty(key, out var v) || v.ValueKind == JsonValueKind.Null;

    /// <summary>1 行の文字列(タイトル・本文・表題): 長さと制御文字を見る。合わなければ null</summary>
    private static string? Text(JsonElement o, string key, int max) =>
        Str(o, key) is { } s && s.Length <= max && !s.Any(c => c < 0x20 || c == 0x7f) ? s : null;

    /// <summary>0 以上の整数(サンプル番号)。合わなければ null</summary>
    private static int? Index(JsonElement v) =>
        v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) && d >= 0 && d == Math.Floor(d) && d <= int.MaxValue ? (int)d : null;

    // ---------------------------------------------------------------- 通知

    private sealed record Notice(string Tag, string Title, string? Text, DateTime? Time);

    private sealed class TagState
    {
        public DateTime Last = DateTime.MinValue;      // 最後に出した時刻(UTC)
        public Notice? Pending;
        public System.Threading.Timer? Timer;
    }

    private void Notify(JsonElement o)
    {
        Stats.NotifyReceived++;
        if (!Foreground)
        {
            Stats.NotifyInBackground++;
        }

        var tag = Str(o, "tag");
        if (tag is null || !NameRe().IsMatch(tag))
        {
            Warn($"notify: bad tag {(o.TryGetProperty("tag", out var t) ? t.GetRawText() : "null")}");
            return;
        }

        if (Text(o, "title", 64) is not { } title)
        {
            Warn($"notify {tag}: title is required (at most 64 characters, no control characters)");
            return;
        }

        string? body = null;
        if (!IsNull(o, "text") && (body = Text(o, "text", 200)) is null)
        {
            Warn($"notify {tag}: text must be at most 200 characters without control characters");
            return;
        }

        DateTime? time = null;
        if (!IsNull(o, "i"))
        {
            if (Index(o.GetProperty("i")) is not { } i)
            {
                Warn($"notify {tag}: i must be a sample number");
                return;
            }

            time = Lookup(i)?.Date;                  // 時刻はイベントのサンプルの DATE(届いた時刻ではない。判定器は遅れて確定する)
        }

        if (!tags.TryGetValue(tag, out var st))
        {
            if (tags.Count >= MaxTags)
            {
                Warn($"notify: more than {MaxTags} tags, {tag} ignored");
                return;
            }

            tags[tag] = st = new TagState();
        }

        var n = new Notice(tag, title, body, time);
        var wait = (st.Last.AddMilliseconds(TagIntervalMs) - DateTime.UtcNow).TotalMilliseconds;
        if (wait <= 0)
        {
            Show(st, n);
            return;
        }

        // 2 秒以内の続き: 最後の 1 件だけを後で出す
        st.Pending = n;
        st.Timer ??= new System.Threading.Timer(_ => ui.BeginInvoke(() =>
        {
            st.Timer?.Dispose();
            st.Timer = null;
            if (active && st.Pending is { } p)
            {
                st.Pending = null;
                Show(st, p);
            }
        }), null, (int)Math.Ceiling(wait), Timeout.Infinite);
    }

    private void Show(TagState st, Notice n)
    {
        st.Last = DateTime.UtcNow;
        if (DetectorNotifications.Show(n.Tag, n.Title, n.Text, n.Time))
        {
            Stats.NotifyShown++;
        }
    }

    // ---------------------------------------------------------------- 表

    private sealed class Table(DetectorOutputs owner, string name, string[] columns, string title)
    {
        private readonly List<string> buf = [];
        private bool header = true;

        public string Name => name;
        public string[] Columns => columns;
        public string? File { get; private set; }
        public int Rows { get; private set; }
        public bool Bad { get; set; }                    // 違う列で宣言し直された(以後の行は捨てる)

        public void Add(string line)
        {
            buf.Add(line);
            Rows++;
            if (buf.Count >= FlushRowCount)
            {
                Flush();
            }
        }

        /// <summary>溜めた行を書く。データ CSV がまだ無ければ(最初の書き出しの前)溜めたまま待つ</summary>
        public void Flush()
        {
            if (buf.Count == 0)
            {
                return;
            }

            if (File is null)
            {
                if (owner.DataFile is not { } data)
                {
                    return;
                }

                File = Path.Combine(Path.GetDirectoryName(data) ?? "", $"{CsvFile.BaseName(data)}_{name}{CsvFile.MatchingExtension(data)}");
            }

            var text = new StringBuilder();
            if (header)
            {
                var t = title.Length > 0 ? $" ({title})" : "";
                text.Append($"// Detector output  : {name}{t}\r\n");
                text.Append($"// Page  : {owner.pageName()}\r\n");
                text.Append($"// Data file  : {Path.GetFileName(owner.DataFile)}\r\n");
                text.Append("//\r\n");
                text.Append("//").Append(string.Join(',', new[] { "NUM", "DATE" }.Concat(columns))).Append("\r\n");
            }

            foreach (var l in buf)
            {
                text.Append(l).Append("\r\n");
            }

            try
            {
                if (header && System.IO.File.Exists(File))
                {
                    System.IO.File.Delete(File);         // 同名の古いファイルへ続けて書かない
                }

                // .csv.gz ならこの 1 回ぶんが gzip の 1 メンバーとして連結される(データ CSV と同じ)
                CsvFile.AppendText(File, text.ToString());
                header = false;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                owner.Warn($"table {name}: write failed: {e.Message}");
            }

            buf.Clear();
        }

        public string? Close()
        {
            Flush();
            if (buf.Count > 0)
            {
                owner.Warn($"table {name}: no data CSV, {buf.Count} row(s) not written");
                buf.Clear();
            }

            return File;
        }
    }

    private void DeclareTable(JsonElement o)
    {
        var name = Str(o, "name");
        if (name is null || !NameRe().IsMatch(name) || name == "disconnect")
        {
            Warn($"table: bad name {(o.TryGetProperty("name", out var n) ? n.GetRawText() : "null")}");
            return;
        }

        string[]? columns = null;
        if (o.TryGetProperty("columns", out var c) && c.ValueKind == JsonValueKind.Array)
        {
            var list = c.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() : null).ToList();
            if (list.All(s => s is not null))
            {
                columns = list.Select(s => s!).ToArray();
            }
        }

        if (columns is null || columns.Length is < 1 or > 32 ||
            columns.Any(s => !ColumnRe().IsMatch(s) || s is "NUM" or "DATE") || columns.Distinct().Count() != columns.Length)
        {
            Warn($"table {name}: bad columns {(o.TryGetProperty("columns", out var cc) ? cc.GetRawText() : "null")}");
            return;
        }

        var title = "";
        if (!IsNull(o, "title"))
        {
            if (Text(o, "title", 64) is not { } t)
            {
                Warn($"table {name}: title must be at most 64 characters without control characters");
                return;
            }

            title = t;
        }

        if (tables.TryGetValue(name, out var old))
        {
            // 宣言し直し(WebView のプロセスが落ちて読み込み直した・start を送り直した): 同じ列なら同じファイルへ続ける
            if (!old.Columns.SequenceEqual(columns) && !old.Bad)
            {
                old.Bad = true;
                Warn($"table {name}: declared again with other columns; its rows are dropped");
            }

            return;
        }

        if (tables.Count >= MaxTables)
        {
            Warn($"table: more than {MaxTables} tables, {name} ignored");
            return;
        }

        tables[name] = new Table(this, name, columns, title);
        tableOrder.Add(name);
    }

    private void Records(JsonElement o)
    {
        var name = Str(o, "name") ?? "";
        if (!tables.TryGetValue(name, out var t))
        {
            Warn($"records: table {name} was not declared");
            return;
        }

        if (t.Bad)
        {
            return;
        }

        if (!o.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            Warn($"records {name}: rows must be an array");
            return;
        }

        var dropped = 0;
        foreach (var r in rows.EnumerateArray())
        {
            if (r.ValueKind != JsonValueKind.Array || r.GetArrayLength() != t.Columns.Length + 1 || Index(r[0]) is not { } i)
            {
                dropped++;
                continue;
            }

            var cells = r.EnumerateArray().Skip(1).Select(Cell).ToList();
            if (cells.Any(x => x is null))
            {
                dropped++;
                continue;
            }

            // NUM はデータ CSV の NUM に直し、DATE はその行の DATE(直近 30 分)。データ CSV に行の無いサンプルは書かない
            if (Lookup(i) is not { } at)
            {
                Stats.RowsNotInData++;
                continue;
            }

            var date = at.Date is { } d ? DataPersistenceService.FormatDate(d) : "";
            t.Add(string.Join(',', new[] { at.Num.ToString(CultureInfo.InvariantCulture), date }.Concat(cells!)));
            Stats.RowsReceived++;
            if (!Foreground)
            {
                Stats.RowsInBackground++;
            }
        }

        if (dropped > 0)
        {
            Warn($"records {name}: {dropped} row(s) dropped (need [i, {t.Columns.Length} values], values: number, string or null)");
        }
    }

    /// <summary>値 1 つ → CSV の欄。数は JSON の数のまま、文字列は 64 文字・区切りと改行は空白・数式に読まれる書き出しは空欄。それ以外は null</summary>
    private static string? Cell(JsonElement v)
    {
        switch (v.ValueKind)
        {
            case JsonValueKind.Null:
                return "";
            case JsonValueKind.Number:
                var raw = v.GetRawText();          // JSON の数のまま(ページが送った書き方)
                if (v.TryGetDouble(out var d) && d == Math.Floor(d) && Math.Abs(d) < 1e15)
                {
                    return ((long)d).ToString(CultureInfo.InvariantCulture);    // 2.0 → 2(Android・Mac と同じ)
                }

                return raw;
            case JsonValueKind.String:
                var s = v.GetString() ?? "";
                s = s.Length > 64 ? s[..64] : s;
                s = s.Replace(',', ' ').Replace('\r', ' ').Replace('\n', ' ');
                return s.Length > 0 && "=+-@".Contains(s[0]) ? "" : s;
            default:
                return null;                       // 真偽値・配列・オブジェクト
        }
    }
}
