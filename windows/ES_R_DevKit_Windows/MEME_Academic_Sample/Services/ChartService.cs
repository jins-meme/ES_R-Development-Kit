using MEMELib_Academic;
using MEME_Academic_Sample.Charting;

namespace MEME_Academic_Sample.Services;

/// <summary>
/// リアルタイム／リプレイ チャート用のデータ計算サービス。
/// 受信したサンプルをフルレート(間引きなし)でバッファし、表示ウィンドウぶんの
/// <see cref="ChartPlot"/> を更新する。間引かないのはハム(50/60Hz)成分を波形に残すため。
/// Mac 版 ChartService の移植。
/// </summary>
public sealed class ChartService
{
    /// <summary>保持する最大サンプル数。最大ウィンドウ(30秒 × 100Hz)を賄えれば足りる。</summary>
    private const int MaxBufferSamples = 30 * 100;

    private readonly List<AcademicData> _samples = new(MaxBufferSamples);

    /// <summary>Append は BLE 受信スレッド、UpdatePlots は UI スレッドから呼ばれる。</summary>
    private readonly Lock _gate = new();

    /// <summary>_samples[0] のストリーム全体での絶対サンプル位置。</summary>
    private int _baseIndex;

    /// <summary>加速度に加算する Setting のオフセット。</summary>
    public double AccelOffsetX { get; set; }

    public double AccelOffsetY { get; set; }

    public double AccelOffsetZ { get; set; }

    /// <summary>X 軸のタイムスタンプをローカルタイムで表示するか(Setting 由来)。</summary>
    public bool ConvertToLocalTime { get; set; } = true;

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _samples.Count;
            }
        }
    }

    public void Append(AcademicData data)
    {
        lock (_gate)
        {
            _samples.Add(data);
            var overflow = _samples.Count - MaxBufferSamples;
            if (overflow > 0)
            {
                _samples.RemoveRange(0, overflow);
                _baseIndex += overflow;
            }
        }
    }

    /// <summary>
    /// バッファをクリアする。<paramref name="baseIndex"/> には次に Append される
    /// サンプルの絶対位置(リプレイのシーク先など)を渡す。通常は 0。
    /// </summary>
    public void Reset(int baseIndex = 0)
    {
        lock (_gate)
        {
            _samples.Clear();
            _baseIndex = baseIndex;
        }
    }

    /// <summary>各チャートの表示状態から <see cref="ChartPlot"/> を組み立て直す。</summary>
    public void UpdatePlots(
        IReadOnlyList<ChartPanel> panels,
        int sampleRate,
        int xRangeSeconds,
        IReadOnlyDictionary<int, string>? artifacts = null)
    {
        var rate = Math.Max(sampleRate, 1);
        var windowSamples = Math.Max(1, xRangeSeconds * rate);

        // 受信スレッドの Append と競合しないよう、表示ウィンドウをここで一度だけ取り出す。
        AcademicData[] window;
        int latestSampleIndex;
        DateTime? latestSampleUtc;
        lock (_gate)
        {
            // 直近 windowSamples 件のみ描画対象にする(波形は右詰めで、右端が最新サンプル)。
            var windowStart = Math.Max(0, _samples.Count - windowSamples);
            window = _samples.GetRange(windowStart, _samples.Count - windowStart).ToArray();
            latestSampleIndex = _baseIndex + Math.Max(_samples.Count - 1, 0);
            latestSampleUtc = _samples.Count > 0 ? _samples[^1].RecordedUtc : null;
        }

        var visibleArtifacts = ArtifactsInWindow(artifacts, latestSampleIndex, windowSamples);

        foreach (var panel in panels)
        {
            var plot = panel.Plot;
            plot.WindowSamples = windowSamples;
            plot.LatestSampleIndex = latestSampleIndex;
            plot.SampleRate = rate;
            plot.LatestSampleUtc = latestSampleUtc;
            plot.ConvertToLocalTime = ConvertToLocalTime;
            plot.Artifacts = visibleArtifacts;
            plot.Series = panel.AppliedCategory switch
            {
                ChartCategory.Electrooculography => BuildEogSeries(window, panel.Eog),
                ChartCategory.Gyroscope => BuildGyroSeries(window, panel.Gyro),
                ChartCategory.Accelerometer => BuildAccelSeries(window, panel.Accel),
                _ => [],
            };
        }
    }

    /// <summary>可視ウィンドウ(右端 latest、幅 windowSamples)に入る Artifact だけ抽出する。</summary>
    private static IReadOnlyList<ChartArtifact> ArtifactsInWindow(
        IReadOnlyDictionary<int, string>? artifacts, int latest, int windowSamples)
    {
        if (artifacts is null || artifacts.Count == 0)
        {
            return [];
        }

        var leftAbs = latest - windowSamples;
        var result = new List<ChartArtifact>();
        foreach (var (index, text) in artifacts)
        {
            if (index >= leftAbs && index <= latest)
            {
                result.Add(new ChartArtifact(index, text));
            }
        }

        return result;
    }

    /// <summary>
    /// EOG。Standard は 1 組目(EogL1 / EogR1 / EogH1 / EogV1)を、Full は EogL / EogR / EogH / EogV を描く。
    /// Quaternion モードには EOG が無いので何も出ない(Mac 版と同じ)。
    /// </summary>
    private static List<ChartSeries> BuildEogSeries(AcademicData[] window, EogToggles toggles)
    {
        var result = new List<ChartSeries>(4);
        if (toggles.Left)
        {
            result.Add(new ChartSeries("EOG L", ChartColors.EogLeft, Collect(window, d => d switch
            {
                AcademicStandardData s => s.EogL1,
                AcademicFullData f => f.EogL,
                _ => (double?)null,
            })));
        }

        if (toggles.Right)
        {
            result.Add(new ChartSeries("EOG R", ChartColors.EogRight, Collect(window, d => d switch
            {
                AcademicStandardData s => s.EogR1,
                AcademicFullData f => f.EogR,
                _ => (double?)null,
            })));
        }

        if (toggles.DeltaH)
        {
            result.Add(new ChartSeries("ΔH", ChartColors.EogDeltaH, Collect(window, d => d switch
            {
                AcademicStandardData s => s.EogH1,
                AcademicFullData f => f.EogH,
                _ => (double?)null,
            })));
        }

        if (toggles.DeltaV)
        {
            result.Add(new ChartSeries("ΔV", ChartColors.EogDeltaV, Collect(window, d => d switch
            {
                AcademicStandardData s => s.EogV1,
                AcademicFullData f => f.EogV,
                _ => (double?)null,
            })));
        }

        return result;
    }

    /// <summary>ジャイロは Full モードにしか無い。</summary>
    private static List<ChartSeries> BuildGyroSeries(AcademicData[] window, AxisToggles toggles)
    {
        var result = new List<ChartSeries>(3);
        if (toggles.X)
        {
            result.Add(new ChartSeries("Gyro X", ChartColors.AxisX,
                Collect(window, d => d is AcademicFullData f ? f.GyroX : null)));
        }

        if (toggles.Y)
        {
            result.Add(new ChartSeries("Gyro Y", ChartColors.AxisY,
                Collect(window, d => d is AcademicFullData f ? f.GyroY : null)));
        }

        if (toggles.Z)
        {
            result.Add(new ChartSeries("Gyro Z", ChartColors.AxisZ,
                Collect(window, d => d is AcademicFullData f ? f.GyroZ : null)));
        }

        return result;
    }

    private List<ChartSeries> BuildAccelSeries(AcademicData[] window, AxisToggles toggles)
    {
        var result = new List<ChartSeries>(3);
        if (toggles.X)
        {
            result.Add(new ChartSeries("Acc X", ChartColors.AxisX, Collect(window, d => d switch
            {
                AcademicStandardData s => s.AccX + AccelOffsetX,
                AcademicFullData f => f.AccX + AccelOffsetX,
                _ => (double?)null,
            })));
        }

        if (toggles.Y)
        {
            result.Add(new ChartSeries("Acc Y", ChartColors.AxisY, Collect(window, d => d switch
            {
                AcademicStandardData s => s.AccY + AccelOffsetY,
                AcademicFullData f => f.AccY + AccelOffsetY,
                _ => (double?)null,
            })));
        }

        if (toggles.Z)
        {
            result.Add(new ChartSeries("Acc Z", ChartColors.AxisZ, Collect(window, d => d switch
            {
                AcademicStandardData s => s.AccZ + AccelOffsetZ,
                AcademicFullData f => f.AccZ + AccelOffsetZ,
                _ => (double?)null,
            })));
        }

        return result;
    }

    /// <summary>そのモードに存在しない値(selector が null)は詰めずに飛ばす。</summary>
    private static double[] Collect(AcademicData[] window, Func<AcademicData, double?> selector)
    {
        var values = new List<double>(window.Length);
        foreach (var sample in window)
        {
            if (selector(sample) is { } value)
            {
                values.Add(value);
            }
        }

        return [.. values];
    }
}
