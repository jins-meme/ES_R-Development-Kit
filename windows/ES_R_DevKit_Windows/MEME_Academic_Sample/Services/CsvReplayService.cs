using System.Diagnostics;
using System.Text;
using MEMELib_Academic;

namespace MEME_Academic_Sample.Services;

/// <summary>読み込んだ再生用 CSV の中身。</summary>
public sealed record CsvReplayInfo(
    string FilePath,
    string FileName,
    MEMEMode Mode,
    MEMEQuality Quality,
    MEMEAccelRange AccelRange,
    MEMEGyroRange GyroRange,
    IReadOnlyList<AcademicData> Rows,
    IReadOnlyDictionary<int, string> Artifacts);

/// <summary>
/// File Replay 用の CSV 解析と再生タイマー。<see cref="DataPersistenceService"/> が
/// 書き出す本アプリ形式(および Mac / Android 版と同じ形式)の CSV だけを対象にする。
/// Mac 版 CsvReplayService の移植。
/// </summary>
public sealed class CsvReplayService : IDisposable
{
    /// <summary>タイマー周期。実際の消費行数は経過時間から求めるので、精度は描画の滑らかさだけに効く。</summary>
    private const int TimerIntervalMs = 15;

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = TimerIntervalMs };

    private IReadOnlyList<AcademicData> _rows = [];
    private int _rowIndex;
    private int _rowsPerSecond = 100;
    private int _speed = 1;

    /// <summary>端数の行を持ち越すためのアキュムレータ。これが無いと再生が少しずつ遅れる。</summary>
    private double _pendingRows;

    private long _lastTickTimestamp;
    private Action<AcademicData, int, int>? _onRow;
    private Action? _onFinished;

    public CsvReplayService() => _timer.Tick += (_, _) => Tick();

    public bool IsRunning => _timer.Enabled;

    #region Parse

    /// <summary>CSV を読み込む。本アプリ形式でなければ false を返す。</summary>
    public static bool TryParse(string path, out CsvReplayInfo? info, out string error)
    {
        info = null;
        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            error = e.Message;
            return false;
        }

        string? modeText = null;
        string? speedText = null;
        string? qualityText = null;
        string? accelText = null;
        string? gyroText = null;
        var dataStart = -1;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("//ARTIFACT", StringComparison.Ordinal))
            {
                dataStart = i + 1;
                break;
            }

            if (line.StartsWith("// Data mode", StringComparison.Ordinal))
            {
                modeText = ValueAfterColon(line);
            }
            else if (line.StartsWith("// Transmission speed", StringComparison.Ordinal))
            {
                speedText = ValueAfterColon(line);
            }
            else if (line.StartsWith("// Data quality", StringComparison.Ordinal))
            {
                // 旧タイプの CSV は Transmission speed の代わりに Data quality (High/Standard) を持つ。
                qualityText = ValueAfterColon(line);
            }
            else if (line.StartsWith("// Acceleration sensor's range", StringComparison.Ordinal) ||
                     line.StartsWith("// Accelerometer sensor's range", StringComparison.Ordinal))
            {
                // 表記揺れ: Acceleration / Accelerometer
                accelText = ValueAfterColon(line);
            }
            else if (line.StartsWith("// Gyroscope sensor's range", StringComparison.Ordinal))
            {
                gyroText = ValueAfterColon(line);
            }
        }

        speedText ??= qualityText switch { "High" => "100Hz", "Standard" => "50Hz", _ => null };

        if (dataStart < 0 ||
            ParseMode(modeText) is not { } mode ||
            ParseQuality(speedText) is not { } quality ||
            ParseAccelRange(accelText) is not { } accelRange ||
            ParseGyroRange(gyroText) is not { } gyroRange)
        {
            error = "MEME の CSV 形式ではありません。";
            return false;
        }

        var rows = new List<AcademicData>(Math.Max(lines.Length - dataStart, 16));
        var artifacts = new Dictionary<int, string>();
        for (var i = dataStart; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (ParseRow(line, mode) is not { } row)
            {
                continue;
            }

            // ARTIFACT 列(最初のカンマより前)に文字列があれば、その行番号で控える。
            var comma = line.IndexOf(',');
            var artifact = comma > 0 ? line[..comma] : string.Empty;
            if (artifact.Length > 0)
            {
                artifacts[rows.Count] = artifact;
            }

            rows.Add(row);
        }

        if (rows.Count == 0)
        {
            error = "データ行がありません。";
            return false;
        }

        info = new CsvReplayInfo(path, Path.GetFileName(path), mode, quality, accelRange, gyroRange, rows, artifacts);
        error = string.Empty;
        return true;
    }

    private static string? ValueAfterColon(string line)
    {
        var index = line.LastIndexOf(':');
        if (index < 0)
        {
            return null;
        }

        var value = line[(index + 1)..].Trim();
        return value.Length == 0 ? null : value;
    }

    private static MEMEMode? ParseMode(string? text) => text switch
    {
        "Standard" => MEMEMode.Standard,
        "Full" => MEMEMode.Full,
        "Quaternion" => MEMEMode.Quaternion,
        _ => null,
    };

    private static MEMEQuality? ParseQuality(string? text) => text switch
    {
        "100Hz" => MEMEQuality.High,
        "50Hz" => MEMEQuality.Low,
        _ => null,
    };

    private static MEMEAccelRange? ParseAccelRange(string? text) => text switch
    {
        "2g" => MEMEAccelRange.Range2G,
        "4g" => MEMEAccelRange.Range4G,
        "8g" => MEMEAccelRange.Range8G,
        "16g" => MEMEAccelRange.Range16G,
        // 旧 Windows 版は大文字の 2G / 4G … で書き出していた。
        "2G" => MEMEAccelRange.Range2G,
        "4G" => MEMEAccelRange.Range4G,
        "8G" => MEMEAccelRange.Range8G,
        "16G" => MEMEAccelRange.Range16G,
        _ => null,
    };

    private static MEMEGyroRange? ParseGyroRange(string? text) => text switch
    {
        "250dps" => MEMEGyroRange.Range250dps,
        "500dps" => MEMEGyroRange.Range500dps,
        "1000dps" => MEMEGyroRange.Range1000dps,
        "2000dps" => MEMEGyroRange.Range2000dps,
        _ => null,
    };

    /// <summary>"mark,NUM,DATE,&lt;モード固有の値...&gt;" を 1 行ぶん解釈する。</summary>
    private static AcademicData? ParseRow(string line, MEMEMode mode)
    {
        var fields = line.Split(',');
        var recordedUtc = fields.Length > 2 ? ParseUtcDate(fields[2]) : null;
        var cnt = fields.Length > 1 && int.TryParse(fields[1], out var num) ? num : 0;

        switch (mode)
        {
            case MEMEMode.Standard when fields.Length >= 14:
                return new AcademicStandardData
                {
                    Cnt = cnt,
                    RecordedUtc = recordedUtc,
                    AccX = Int16Field(fields, 3),
                    AccY = Int16Field(fields, 4),
                    AccZ = Int16Field(fields, 5),
                    EogL1 = Int16Field(fields, 6),
                    EogR1 = Int16Field(fields, 7),
                    EogL2 = Int16Field(fields, 8),
                    EogR2 = Int16Field(fields, 9),
                    EogH1 = Int16Field(fields, 10),
                    EogH2 = Int16Field(fields, 11),
                    EogV1 = Int16Field(fields, 12),
                    EogV2 = Int16Field(fields, 13),
                };

            case MEMEMode.Full when fields.Length >= 13:
                return new AcademicFullData
                {
                    Cnt = cnt,
                    RecordedUtc = recordedUtc,
                    AccX = Int16Field(fields, 3),
                    AccY = Int16Field(fields, 4),
                    AccZ = Int16Field(fields, 5),
                    GyroX = Int16Field(fields, 6),
                    GyroY = Int16Field(fields, 7),
                    GyroZ = Int16Field(fields, 8),
                    EogL = Int16Field(fields, 9),
                    EogR = Int16Field(fields, 10),
                    EogH = Int16Field(fields, 11),
                    EogV = Int16Field(fields, 12),
                };

            case MEMEMode.Quaternion when fields.Length >= 7:
                return new AcademicQuaternionData
                {
                    Cnt = cnt,
                    RecordedUtc = recordedUtc,
                    QuaternionW = Int32Field(fields, 3),
                    QuaternionX = Int32Field(fields, 4),
                    QuaternionY = Int32Field(fields, 5),
                    QuaternionZ = Int32Field(fields, 6),
                };

            default:
                return null;
        }
    }

    private static short Int16Field(string[] fields, int index) =>
        short.TryParse(fields[index], out var value) ? value : (short)0;

    private static int Int32Field(string[] fields, int index) =>
        int.TryParse(fields[index], out var value) ? value : 0;

    /// <summary>
    /// DATE 列 "yyyy/MM/dd HH:mm:ss.ff"(UTC)を読む。秒未満の桁数は問わない
    /// (Android 版は .fff で書き出す)。数十万行を読むため整数演算で解く。
    /// </summary>
    private static DateTime? ParseUtcDate(string field)
    {
        var s = field.AsSpan().Trim();
        if (s.Length < 19)
        {
            return null;
        }

        if (!TryNumber(s, 0, 4, out var year) ||
            !TryNumber(s, 5, 2, out var month) ||
            !TryNumber(s, 8, 2, out var day) ||
            !TryNumber(s, 11, 2, out var hour) ||
            !TryNumber(s, 14, 2, out var minute) ||
            !TryNumber(s, 17, 2, out var second) ||
            month is < 1 or > 12 || day is < 1 or > 31 ||
            hour > 23 || minute > 59 || second > 59)
        {
            return null;
        }

        var ticks = TimeSpan.TicksPerDay * DaysFromEpoch(year, month, day)
                    + TimeSpan.TicksPerHour * hour
                    + TimeSpan.TicksPerMinute * minute
                    + TimeSpan.TicksPerSecond * second;

        if (s.Length > 20 && s[19] == '.')
        {
            var scale = TimeSpan.TicksPerSecond / 10;
            for (var i = 20; i < s.Length && char.IsAsciiDigit(s[i]) && scale > 0; i++)
            {
                ticks += (s[i] - '0') * scale;
                scale /= 10;
            }
        }

        return new DateTime(DateTime.UnixEpoch.Ticks + ticks, DateTimeKind.Utc);
    }

    private static bool TryNumber(ReadOnlySpan<char> s, int start, int length, out int value)
    {
        value = 0;
        for (var i = start; i < start + length; i++)
        {
            if (!char.IsAsciiDigit(s[i]))
            {
                return false;
            }

            value = value * 10 + (s[i] - '0');
        }

        return true;
    }

    /// <summary>1970-01-01 からの経過日数(Howard Hinnant の days_from_civil)。</summary>
    private static long DaysFromEpoch(int year, int month, int day)
    {
        // 3月始まりの暦に置き換えると閏日が年末に来るため、閏年の場合分けが要らなくなる。
        var y = year - (month <= 2 ? 1 : 0);
        var era = (y >= 0 ? y : y - 399) / 400;
        var yearOfEra = y - era * 400;
        var dayOfYear = (153 * (month + (month > 2 ? -3 : 9)) + 2) / 5 + day - 1;
        var dayOfEra = yearOfEra * 365 + yearOfEra / 4 - yearOfEra / 100 + dayOfYear;
        return era * 146_097L + dayOfEra - 719_468L;
    }

    #endregion

    #region Artifact write-back / range export

    /// <summary>
    /// CSV の ARTIFACT 列(各データ行の先頭カラム)へ書き戻す。キーは 0 始まりのデータ行番号で、
    /// <see cref="TryParse"/> が返す Rows と同じ順序。既に値がある行は上書きする。
    /// </summary>
    public static void ApplyArtifacts(string path, IReadOnlyDictionary<int, string> artifacts)
    {
        if (artifacts.Count == 0)
        {
            return;
        }

        var lines = File.ReadAllLines(path);
        var headerIndex = Array.FindIndex(lines, l => l.StartsWith("//ARTIFACT", StringComparison.Ordinal));
        if (headerIndex < 0)
        {
            throw new InvalidDataException("MEME の CSV 形式ではありません。");
        }

        var dataRow = 0;
        for (var i = headerIndex + 1; i < lines.Length; i++)
        {
            // TryParse と同じく空行はデータ行として数えない。
            if (lines[i].Trim().Length == 0)
            {
                continue;
            }

            if (artifacts.TryGetValue(dataRow, out var artifact))
            {
                lines[i] = ReplaceFirstField(lines[i], artifact);
            }

            dataRow++;
        }

        WriteAtomic(path, lines);
    }

    /// <summary>
    /// データ行 [startRow, endRow](0 始まり・両端含む)だけを含む CSV を書き出す。
    /// ヘッダはそのままコピーし、データ行の内容も加工しない。
    /// </summary>
    public static void ExportRange(string sourcePath, string destinationPath, int startRow, int endRow)
    {
        var lines = File.ReadAllLines(sourcePath);
        var headerIndex = Array.FindIndex(lines, l => l.StartsWith("//ARTIFACT", StringComparison.Ordinal));
        if (headerIndex < 0)
        {
            throw new InvalidDataException("MEME の CSV 形式ではありません。");
        }

        var output = new List<string>(lines[..(headerIndex + 1)]);
        var dataRow = 0;
        for (var i = headerIndex + 1; i < lines.Length; i++)
        {
            if (lines[i].Trim().Length == 0)
            {
                continue;
            }

            if (dataRow > endRow)
            {
                break;
            }

            if (dataRow >= startRow)
            {
                output.Add(lines[i]);
            }

            dataRow++;
        }

        WriteAtomic(destinationPath, output);
    }

    /// <summary>1 行の最初のカンマより前(ARTIFACT 列)を差し替える。</summary>
    private static string ReplaceFirstField(string line, string value)
    {
        var comma = line.IndexOf(',');
        return comma < 0 ? line : value + line[comma..];
    }

    /// <summary>
    /// 書き込み中に落ちても元ファイルを壊さないよう、一時ファイルへ書いてから置き換える。
    /// 数十万行を書き戻すこともあるため、途中で失敗する余地を減らしておく。
    /// </summary>
    private static void WriteAtomic(string path, IEnumerable<string> lines)
    {
        var directory = Path.GetDirectoryName(path);
        var temp = Path.Combine(
            string.IsNullOrEmpty(directory) ? "." : directory,
            Path.GetFileName(path) + ".tmp");

        using (var writer = new StreamWriter(temp, append: false, new UTF8Encoding(false)) { NewLine = "\r\n" })
        {
            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
        }

        File.Move(temp, path, overwrite: true);
    }

    #endregion

    #region Playback

    /// <summary>
    /// Trans Speed(100Hz / 50Hz)に対応する速さで 1 行ずつ <paramref name="onRow"/> を呼ぶ。
    /// 最終行まで再生し終えたら <paramref name="onFinished"/> を呼ぶ。
    /// </summary>
    public void Start(
        IReadOnlyList<AcademicData> rows,
        MEMEQuality quality,
        Action<AcademicData, int, int> onRow,
        Action onFinished)
    {
        Stop();
        _rows = rows;
        _rowIndex = 0;
        _onRow = onRow;
        _onFinished = onFinished;
        _rowsPerSecond = quality == MEMEQuality.High ? 100 : 50;
        _speed = 1;
        _pendingRows = 0;
        _lastTickTimestamp = Stopwatch.GetTimestamp();
        _timer.Start();
    }

    /// <summary>再生速度倍率。再生中でも即座に反映される。</summary>
    public void SetSpeed(int speed) => _speed = Math.Max(1, speed);

    public void Stop() => _timer.Stop();

    public void Pause() => _timer.Stop();

    /// <summary>一時停止中の再生を同じ位置から再開する。末尾に到達済みなら false。</summary>
    public bool Resume()
    {
        if (_timer.Enabled || _rows.Count == 0 || _rowIndex >= _rows.Count)
        {
            return false;
        }

        _pendingRows = 0;
        _lastTickTimestamp = Stopwatch.GetTimestamp();
        _timer.Start();
        return true;
    }

    /// <summary>再生を終了し、保持していた行データも解放する。</summary>
    public void Clear()
    {
        Stop();
        _rows = [];
        _rowIndex = 0;
        _pendingRows = 0;
        _onRow = null;
        _onFinished = null;
    }

    /// <summary>再生位置を指定行へ移す(再生中のタイマーはそのまま)。</summary>
    public void Seek(int index)
    {
        if (_rows.Count == 0)
        {
            return;
        }

        _rowIndex = Math.Clamp(index, 0, _rows.Count);
        _pendingRows = 0;
        _lastTickTimestamp = Stopwatch.GetTimestamp();
    }

    private void Tick()
    {
        var now = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(_lastTickTimestamp, now).TotalSeconds;
        _lastTickTimestamp = now;

        // 経過時間から消費行数を求めるので、タイマーの分解能に関係なく x1 が実時間になる。
        // 端数は次回へ持ち越す。中断後の巨大なバーストは 1 秒ぶんで頭打ちにする。
        _pendingRows += elapsed * _rowsPerSecond * _speed;
        var count = (int)_pendingRows;
        _pendingRows -= count;
        count = Math.Min(count, _rowsPerSecond * _speed);

        for (var i = 0; i < count; i++)
        {
            if (_rowIndex >= _rows.Count)
            {
                // 末尾に到達したらタイマーだけ止める。行データとコールバックは保持し、
                // << で戻ってから Resume で続きを再生できるようにする。
                Stop();
                _onFinished?.Invoke();
                return;
            }

            var index = _rowIndex++;
            _onRow?.Invoke(_rows[index], index, _rows.Count);
        }
    }

    #endregion

    public void Dispose()
    {
        Clear();
        _timer.Dispose();
    }
}
