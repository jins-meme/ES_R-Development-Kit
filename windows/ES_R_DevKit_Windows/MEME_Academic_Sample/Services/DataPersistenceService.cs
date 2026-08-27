using System.Globalization;
using System.Text;
using MEMELib_Academic;

namespace MEME_Academic_Sample.Services;

/// <summary>
/// CSV のヘッダ生成・行整形・バッファ保存を担当する。Mac 版 DataPersistenceService の移植。
/// 1 行ごとに open/close すると 100Hz に追いつかないため、一定件数たまってから書き出す。
/// </summary>
public sealed class DataPersistenceService : IDisposable
{
    private const string DateFormat = "yyyy/MM/dd HH:mm:ss.ff";

    private readonly Lock _gate = new();
    private readonly List<string> _pendingRows = [];

    private string? _directory;
    private string? _fileName;
    private string? _header;
    private int _flushThreshold = 100;

    /// <summary>書き出し中の CSV のパス。まだ 1 件も書き出していなければ null。</summary>
    public string? CurrentFilePath { get; private set; }

    /// <summary>整形済みの 1 行が確定するたびに発火する。TCP 出力へ横流しするために使う。</summary>
    public event Action<string>? RowFormatted;

    /// <summary>計測開始。実ファイルは最初のフラッシュ時に作る(空ファイルを残さないため)。</summary>
    public void Begin(string directory, string macAddress, string header, MEMEQuality quality)
    {
        lock (_gate)
        {
            _pendingRows.Clear();
            _directory = directory;
            _header = header;
            // ファイル名の日時も UTC(DATE 列・Mac 版・Android 版と揃える)。
            _fileName = $"{macAddress}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            _flushThreshold = Math.Max(100 / Math.Max((int)quality, 1), 1);
            CurrentFilePath = null;
        }
    }

    public void Append(AcademicData data, int packetCount, bool freeMarking)
    {
        var row = FormatRow(data, packetCount, freeMarking);
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

            File.AppendAllText(path, buffer.ToString(), new UTF8Encoding(false));
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
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
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

    /// <summary>計測パラメータから CSV ヘッダを組み立てる。列は Mac 版・Android 版と共通。</summary>
    public static string BuildHeader(
        MEMEMode mode, MEMEQuality quality, MEMEAccelRange accelRange, MEMEGyroRange gyroRange)
    {
        var columns = mode switch
        {
            MEMEMode.Standard =>
                "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,EOG_L1,EOG_R1,EOG_L2,EOG_R2,EOG_H1,EOG_H2,EOG_V1,EOG_V2",
            MEMEMode.Full =>
                "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V",
            _ =>
                "//ARTIFACT,NUM,DATE,QUATERNION_W,QUATERNION_X,QUATERNION_Y,QUATERNION_Z",
        };

        return new StringBuilder()
            .Append("// Data mode  : ").Append(mode).Append("\r\n")
            .Append("// Transmission speed  : ").Append(quality == MEMEQuality.High ? "100Hz" : "50Hz").Append("\r\n")
            .Append("// Acceleration sensor's range  : ").Append(AccelRangeText(accelRange)).Append("\r\n")
            .Append("// Gyroscope sensor's range  : ").Append(GyroRangeText(gyroRange)).Append("\r\n")
            .Append("//\r\n")
            .Append(columns).Append("\r\n")
            .ToString();
    }

    /// <summary>CSV / TCP 共通の 1 行整形。DATE 列は UTC。</summary>
    public static string FormatRow(AcademicData data, int packetCount, bool freeMarking)
    {
        var mark = freeMarking ? "x" : string.Empty;
        var timestamp = (data.RecordedUtc ?? DateTime.UtcNow).ToString(DateFormat, CultureInfo.InvariantCulture);

        return data switch
        {
            AcademicStandardData d => string.Join(',',
                mark, packetCount, timestamp,
                d.AccX, d.AccY, d.AccZ,
                d.EogL1, d.EogR1, d.EogL2, d.EogR2,
                d.EogH1, d.EogH2, d.EogV1, d.EogV2),

            AcademicFullData d => string.Join(',',
                mark, packetCount, timestamp,
                d.AccX, d.AccY, d.AccZ,
                d.GyroX, d.GyroY, d.GyroZ,
                d.EogL, d.EogR, d.EogH, d.EogV),

            AcademicQuaternionData d => string.Join(',',
                mark, packetCount, timestamp,
                d.QuaternionW, d.QuaternionX, d.QuaternionY, d.QuaternionZ),

            _ => string.Join(',', mark, packetCount, timestamp),
        };
    }

    public static string AccelRangeText(MEMEAccelRange range) => range switch
    {
        MEMEAccelRange.Range2G => "2g",
        MEMEAccelRange.Range4G => "4g",
        MEMEAccelRange.Range8G => "8g",
        _ => "16g",
    };

    public static string GyroRangeText(MEMEGyroRange range) => range switch
    {
        MEMEGyroRange.Range250dps => "250dps",
        MEMEGyroRange.Range500dps => "500dps",
        MEMEGyroRange.Range1000dps => "1000dps",
        _ => "2000dps",
    };

    public void Dispose() => End();
}
