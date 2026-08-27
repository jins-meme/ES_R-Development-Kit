namespace MEME_Academic_Sample.Charting;

/// <summary>チャート1枚が表示するセンサー種別。Mac 版 chartCategoryOptions と同じ並び。</summary>
public enum ChartCategory
{
    Electrooculography = 0,
    Gyroscope = 1,
    Accelerometer = 2,
}

/// <summary>波形1本ぶん。</summary>
public sealed class ChartSeries(string name, Color color, double[] values)
{
    public string Name { get; } = name;
    public Color Color { get; } = color;
    public double[] Values { get; } = values;
}

/// <summary>チャート上に縦線とラベルで示すイベント位置。</summary>
public sealed class ChartArtifact(int sampleIndex, string text)
{
    public int SampleIndex { get; } = sampleIndex;
    public string Text { get; } = text;
}

public struct EogToggles()
{
    // Left / Right は生の電位で振れが大きく、既定では ΔH / ΔV の判読を妨げるため非表示。
    public bool Left = false;
    public bool Right = false;
    public bool DeltaH = true;
    public bool DeltaV = true;
}

public struct AxisToggles()
{
    public bool X = true;
    public bool Y = true;
    public bool Z = true;
}

/// <summary>
/// チャート1枚の描画状態。<see cref="Services.ChartService"/> が更新し、
/// <see cref="ChartCanvas"/> が描画する。Mac 版 ChartPlot の移植。
/// </summary>
public sealed class ChartPlot
{
    /// <summary>
    /// 縦軸の拡大率。小さいほど拡大(max-min が狭い)。等倍を中央に上下2段階ずつ。
    /// Gyro/Accel(基準 ±8000)では ±2000 / ±4000 / ±8000 / ±16000 / ±32000 になる。
    /// 生値は符号付き 16bit なので ±32000 が実質の上限。
    /// </summary>
    public static readonly double[] YScaleOptions = [0.25, 0.5, 1, 2, 4];

    private const int DefaultYScaleIndex = 2;

    public ChartPlot(double baseYMin, double baseYMax)
    {
        BaseYMin = baseYMin;
        BaseYMax = baseYMax;
    }

    /// <summary>カテゴリごとに決まる基準の縦軸範囲。実際の表示範囲はこれに YScale を掛けたもの。</summary>
    public double BaseYMin { get; private set; }

    public double BaseYMax { get; private set; }

    public int YScaleIndex { get; private set; } = DefaultYScaleIndex;

    public double YScale => YScaleOptions[Math.Clamp(YScaleIndex, 0, YScaleOptions.Length - 1)];

    public double YMin => BaseYMin * YScale;

    public double YMax => BaseYMax * YScale;

    public bool CanZoomInY => YScaleIndex > 0;

    public bool CanZoomOutY => YScaleIndex < YScaleOptions.Length - 1;

    /// <summary>拡大：縦軸の max-min を半分にする。</summary>
    public void ZoomInY()
    {
        if (CanZoomInY)
        {
            YScaleIndex--;
        }
    }

    /// <summary>縮小：縦軸の max-min を2倍にする。</summary>
    public void ZoomOutY()
    {
        if (CanZoomOutY)
        {
            YScaleIndex++;
        }
    }

    /// <summary>表示ウィンドウの全幅(サンプル数) = XRangeSeconds × SampleRate。X 座標の正規化に使う。</summary>
    public int WindowSamples { get; set; } = 7 * 100;

    /// <summary>最新サンプル(= 右端)のストリーム全体での絶対サンプル位置。</summary>
    public int LatestSampleIndex { get; set; }

    public int SampleRate { get; set; } = 100;

    /// <summary>最新サンプル(= 右端)の記録時刻(UTC)。X 軸ラベルはここから遡って求める。</summary>
    public DateTime? LatestSampleUtc { get; set; }

    /// <summary>X 軸のタイムスタンプをローカルタイムで表示するか。false なら UTC のまま。</summary>
    public bool ConvertToLocalTime { get; set; } = true;

    public IReadOnlyList<ChartSeries> Series { get; set; } = [];

    public IReadOnlyList<ChartArtifact> Artifacts { get; set; } = [];

    public void Reset()
    {
        LatestSampleIndex = 0;
        LatestSampleUtc = null;
        Series = [];
        Artifacts = [];
    }

    /// <summary>カテゴリ変更時に基準レンジを付け替え、描き途中の系列を捨てる。</summary>
    public void ApplyCategory(ChartCategory category)
    {
        if (category == ChartCategory.Electrooculography)
        {
            BaseYMin = -1200;
            BaseYMax = 1200;
        }
        else
        {
            BaseYMin = -8000;
            BaseYMax = 8000;
        }

        Series = [];
    }
}

/// <summary>系列の色。Mac 版と対応させている。</summary>
public static class ChartColors
{
    public static readonly Color EogLeft = Color.FromArgb(230, 200, 40);
    public static readonly Color EogRight = Color.FromArgb(70, 200, 90);
    public static readonly Color EogDeltaH = Color.FromArgb(235, 80, 70);
    public static readonly Color EogDeltaV = Color.FromArgb(80, 150, 245);

    public static readonly Color AxisX = EogDeltaH;
    public static readonly Color AxisY = EogRight;
    public static readonly Color AxisZ = EogDeltaV;
}
