using System.Globalization;
using System.Text;
using MEMELib_Academic;
using MEME_Academic_Sample.Models;

namespace MEME_Academic_Sample.Services;

/// <summary>
/// CSV のヘッダ生成・行整形・バッファ保存を担当する。Mac 版 DataPersistenceService の移植。
/// 1 行ごとに open/close すると 100Hz に追いつかないため、一定件数たまってから書き出す。
/// </summary>
public sealed class DataPersistenceService : IDisposable
{
    private const string DateFormat = "yyyy/MM/dd HH:mm:ss.ff";

    /// <summary>
    /// 数値と日時はこの書式で書く。現在のカルチャに任せると、スウェーデン語などでは負の数が U+2212(−)に、
    /// タイ語・アラビア語では年が仏暦・ヒジュラ暦になり、Mac 版・Android 版・グラフ画面で読めない CSV になる。
    /// </summary>
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private readonly Lock _gate = new();
    private readonly List<string> _pendingRows = [];

    private string? _directory;
    private string? _fileName;
    private string? _header;
    private MeasurementMode _mode = MeasurementMode.Full;
    private int _flushThreshold = 100;

    /// <summary>書き出し中の CSV のパス。まだ 1 件も書き出していなければ null。</summary>
    public string? CurrentFilePath { get; private set; }

    /// <summary>整形済みの 1 行が確定するたびに発火する。TCP 出力へ横流しするために使う。</summary>
    public event Action<string>? RowFormatted;

    /// <summary>計測開始。実ファイルは最初のフラッシュ時に作る(空ファイルを残さないため)。</summary>
    /// <param name="compressed">
    /// gz 圧縮して保存するか(Setting の Save Format)。拡張子で圧縮の有無が決まり、
    /// 読み込み側は設定に関係なく .csv / .csv.gz の両方を受け付ける。
    /// </param>
    public void Begin(string directory, string macAddress, string header, MeasurementMode mode, MEMEQuality quality, bool compressed)
    {
        lock (_gate)
        {
            _pendingRows.Clear();
            _directory = directory;
            _header = header;
            _mode = mode;
            _fileName = FileName(macAddress, DateTime.UtcNow, compressed);
            _flushThreshold = Math.Max(100 / Math.Max((int)quality, 1), 1);
            CurrentFilePath = null;
        }
    }

    /// <summary>1 行を整形して溜める(受信スレッド)。モードと違う型のサンプルは書かない(列が崩れるため)。</summary>
    public void Append(AcademicData data, int packetCount, DateTime recordedUtc, bool freeMarking)
    {
        if (FormatRow(_mode, data, packetCount, recordedUtc, freeMarking) is not { } row)
        {
            return;
        }

        RowFormatted?.Invoke(row);

        lock (_gate)
        {
            if (_fileName is null)
            {
                return;
            }

            _pendingRows.Add(row);
            if (_pendingRows.Count >= _flushThreshold)
            {
                FlushCore();
            }
        }
    }

    /// <summary>計測停止。残りを書き出してファイルを確定する。</summary>
    public void End()
    {
        lock (_gate)
        {
            FlushCore();
            _fileName = null;
            _header = null;
            _directory = null;
        }
    }

    private void FlushCore()
    {
        if (_pendingRows.Count == 0 || _directory is null || _fileName is null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(_directory);
            // ファイル名は秒までしか持たないため、同じ秒に始め直すと衝突しうる。
            // 既存ファイルへ追記すると別セッションが 1 つの CSV に混ざるので、
            // 初回書き出しのときだけ空いている名前を選ぶ。
            var path = CurrentFilePath ?? ResolveUniquePath(Path.Combine(_directory, _fileName));
            var buffer = new StringBuilder();
            if (CurrentFilePath is null)
            {
                buffer.Append(_header);
            }

            foreach (var row in _pendingRows)
            {
                buffer.Append(row).Append("\r\n");
            }

            // .csv.gz ならこの 1 回ぶんが gzip の 1 メンバーとして連結される。
            CsvFile.AppendText(path, buffer.ToString());
            CurrentFilePath = path;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // 書けなくても計測とチャートは続ける。次のフラッシュで再試行される。
            return;
        }

        _pendingRows.Clear();
    }

    /// <summary>同名のファイルがあれば `_2`, `_3` … を足して空いている名前を返す。</summary>
    private static string ResolveUniquePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        // ".csv.gz" は 2 段なので Path.GetExtension では切り分けられない。
        var name = CsvFile.BaseName(path);
        var extension = CsvFile.MatchingExtension(path);
        for (var suffix = 2; suffix < 1000; suffix++)
        {
            var candidate = Path.Combine(directory, $"{name}_{suffix}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        return path;
    }

    /// <summary>CSV のファイル名(&lt;MAC アドレス&gt;_&lt;UTC 日時&gt;.csv[.gz])。日時は DATE 列・Mac 版・Android 版と揃えて UTC。</summary>
    public static string FileName(string macAddress, DateTime utc, bool compressed) =>
        $"{macAddress}_{utc.ToString("yyyyMMddHHmmss", Invariant)}{CsvFile.SaveExtension(compressed)}";

    /// <summary>計測パラメータから CSV ヘッダを組み立てる。列は Mac 版・Android 版と共通。</summary>
    public static string BuildHeader(
        MeasurementMode mode, MEMEQuality quality, MEMEAccelRange accelRange, MEMEGyroRange gyroRange) =>
        string.Create(Invariant,
            $"// Data mode  : {mode.Label}\r\n" +
            $"// Transmission speed  : {MeasurementRange.Hz(quality)}Hz\r\n" +
            $"// Acceleration sensor's range  : {MeasurementRange.G(accelRange)}g\r\n" +
            $"// Gyroscope sensor's range  : {MeasurementRange.Dps(gyroRange)}dps\r\n" +
            $"//\r\n" +
            $"//{string.Join(',', new[] { "ARTIFACT", "NUM", "DATE" }.Concat(mode.Columns))}\r\n");

    /// <summary>
    /// CSV / TCP 共通の 1 行整形。DATE 列は UTC。サンプルの型が <paramref name="mode"/> と違えば null。
    /// </summary>
    public static string? FormatRow(MeasurementMode mode, AcademicData data, int packetCount, DateTime recordedUtc, bool freeMarking)
    {
        if (mode.Values(data) is not { } values)
        {
            return null;
        }

        var row = new StringBuilder(96)
            .Append(freeMarking ? "X" : string.Empty)
            .Append(',').Append(packetCount.ToString(Invariant))
            .Append(',').Append(recordedUtc.ToString(DateFormat, Invariant));
        foreach (var v in values)
        {
            row.Append(',').Append(v.ToString(Invariant));
        }

        return row.ToString();
    }

    public void Dispose() => End();
}
