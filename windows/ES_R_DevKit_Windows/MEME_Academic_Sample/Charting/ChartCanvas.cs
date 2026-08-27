using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace MEME_Academic_Sample.Charting;

/// <summary>
/// 波形描画コントロール。ハム(50/60Hz)成分を残すため間引かず全サンプルを描く。
/// 波形は右詰めで、右端が最新サンプル。Mac 版 RealtimeChartView の移植。
/// </summary>
public sealed class ChartCanvas : Control
{
    private const int LeftInset = 52;
    private const int RightInset = 10;
    private const int TopInset = 8;
    private const int BottomInset = 20;

    /// <summary>横軸に置く目盛りの最大本数。これを超えない中で最も細かい間隔を選ぶ。</summary>
    private const int MaxTimeTicks = 8;

    /// <summary>横軸の目盛り間隔の候補(秒)。ラベルが丸い時刻になるよう秒の約数だけを並べる。</summary>
    private static readonly double[] TimeTickIntervals = [1, 2, 5, 10, 15, 30, 60, 120, 300];

    /// <summary>縦軸グリッドの分割数。</summary>
    private const int YGridDivisions = 4;

    private static readonly Color PlotBackColor = Color.FromArgb(28, 30, 34);
    private static readonly Color GridColor = Color.FromArgb(58, 62, 70);
    private static readonly Color ZeroLineColor = Color.FromArgb(96, 102, 112);
    private static readonly Color AxisTextColor = Color.FromArgb(168, 176, 188);
    private static readonly Color ArtifactColor = Color.FromArgb(240, 170, 60);

    /// <summary>タップと区別するため、これ以上動かしたドラッグだけを範囲選択とみなす。</summary>
    private const int DragThreshold = 4;

    private static readonly Color SelectionColor = Color.FromArgb(64, 120, 180, 240);
    private static readonly Color SelectionEdgeColor = Color.FromArgb(160, 200, 230);

    /// <summary>再描画のたびに確保し直さないための座標バッファ。</summary>
    private PointF[] _pointBuffer = [];

    private int? _dragStartX;
    private int? _dragCurrentX;

    public ChartCanvas()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        BackColor = PlotBackColor;
    }

    /// <summary>クリックされたサンプル位置(絶対)を通知する。</summary>
    public event Action<int>? RowTapped;

    /// <summary>ドラッグで選択された区間(絶対サンプル位置、開始 ≦ 終了)を通知する。</summary>
    public event Action<int, int>? RangeSelected;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ChartPlot? Plot { get; set; }

    /// <summary>ドラッグによる範囲選択を受け付けるか(ファイル再生中のみ true にする)。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool RangeSelectionEnabled { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(PlotBackColor);

        var plotLeft = LeftInset;
        var plotTop = TopInset;
        var plotRight = Width - RightInset;
        var plotBottom = Height - BottomInset;
        if (plotRight <= plotLeft || plotBottom <= plotTop)
        {
            return;
        }

        var plot = Plot;
        if (plot is null)
        {
            return;
        }

        var area = RectangleF.FromLTRB(plotLeft, plotTop, plotRight, plotBottom);
        DrawGridAndYLabels(g, area, plot);
        DrawTimeAxis(g, area, plot);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        var clip = g.Clip;
        g.SetClip(area);
        DrawSeries(g, area, plot);
        DrawArtifacts(g, area, plot);
        g.Clip = clip;
        g.SmoothingMode = SmoothingMode.Default;

        DrawDragSelection(g, area);
    }

    #region Tap / drag

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _dragStartX = e.X;
            _dragCurrentX = e.X;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragStartX is null || !RangeSelectionEnabled)
        {
            return;
        }

        _dragCurrentX = e.X;
        if (Math.Abs(e.X - _dragStartX.Value) >= DragThreshold)
        {
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        var start = _dragStartX;
        _dragStartX = null;
        _dragCurrentX = null;

        if (e.Button != MouseButtons.Left || start is null || Plot is null)
        {
            return;
        }

        var distance = Math.Abs(e.X - start.Value);
        if (RangeSelectionEnabled && distance >= DragThreshold)
        {
            Invalidate();
            var left = RowAt(Math.Min(start.Value, e.X));
            var right = RowAt(Math.Max(start.Value, e.X));
            RangeSelected?.Invoke(left, right);
            return;
        }

        if (distance < DragThreshold)
        {
            RowTapped?.Invoke(RowAt(e.X));
        }
    }

    /// <summary>X 座標を、右詰め描画に合わせて絶対サンプル位置へ変換する。</summary>
    private int RowAt(int x)
    {
        var plot = Plot;
        if (plot is null)
        {
            return 0;
        }

        var plotLeft = (float)LeftInset;
        var plotWidth = Math.Max(1f, Width - LeftInset - RightInset);
        var clamped = Math.Clamp(x, plotLeft, plotLeft + plotWidth);
        // 右端が最新(0)、左端が最古(1)。
        var fromRight = (plotLeft + plotWidth - clamped) / plotWidth;
        var offset = (int)Math.Round(fromRight * Math.Max(plot.WindowSamples, 1));
        return Math.Max(0, plot.LatestSampleIndex - offset);
    }

    private void DrawDragSelection(Graphics g, RectangleF area)
    {
        if (!RangeSelectionEnabled || _dragStartX is not { } start || _dragCurrentX is not { } current)
        {
            return;
        }

        var left = Math.Clamp(Math.Min(start, current), area.Left, area.Right);
        var right = Math.Clamp(Math.Max(start, current), area.Left, area.Right);
        if (right - left < DragThreshold)
        {
            return;
        }

        using var brush = new SolidBrush(SelectionColor);
        using var pen = new Pen(SelectionEdgeColor);
        g.FillRectangle(brush, left, area.Top, right - left, area.Height);
        g.DrawLine(pen, left, area.Top, left, area.Bottom);
        g.DrawLine(pen, right, area.Top, right, area.Bottom);
    }

    #endregion

    private void DrawGridAndYLabels(Graphics g, RectangleF area, ChartPlot plot)
    {
        using var gridPen = new Pen(GridColor);
        using var zeroPen = new Pen(ZeroLineColor);
        using var textBrush = new SolidBrush(AxisTextColor);
        using var format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

        var range = plot.YMax - plot.YMin;
        if (range <= 0)
        {
            return;
        }

        for (var i = 0; i <= YGridDivisions; i++)
        {
            var value = plot.YMax - range * i / YGridDivisions;
            var y = area.Top + area.Height * i / YGridDivisions;
            var isZero = Math.Abs(value) < range / 1000.0;
            g.DrawLine(isZero ? zeroPen : gridPen, area.Left, y, area.Right, y);

            var label = Math.Abs(value) >= 1000 ? $"{value / 1000:0.#}k" : $"{value:0}";
            g.DrawString(label, Font, textBrush,
                new RectangleF(0, y - 8, LeftInset - 6, 16), format);
        }

        g.DrawRectangle(gridPen, area.Left, area.Top, area.Width, area.Height);
    }

    /// <summary>
    /// 横軸の目盛り。ラベルは画面上の固定位置ではなく「丸い時刻」に打つため、
    /// 波形と同じ速さで左へ流れていく。目盛り線もラベルと同じ位置に引く。
    /// </summary>
    private void DrawTimeAxis(Graphics g, RectangleF area, ChartPlot plot)
    {
        var rate = Math.Max(plot.SampleRate, 1);
        var windowSeconds = plot.WindowSamples / (double)rate;
        if (windowSeconds <= 0)
        {
            return;
        }

        var interval = ChooseTickInterval(windowSeconds);
        var pixelsPerSecond = area.Width / windowSeconds;

        using var textBrush = new SolidBrush(AxisTextColor);
        using var gridPen = new Pen(GridColor);

        if (plot.LatestSampleUtc is { } latestUtc)
        {
            var latest = plot.ConvertToLocalTime ? latestUtc.ToLocalTime() : latestUtc;
            // 右端(最新サンプル)より前で、interval の倍数になる最初の時刻から左へ辿る。
            var step = TimeSpan.FromSeconds(interval).Ticks;
            var tick = new DateTime(latest.Ticks / step * step, latest.Kind);
            for (; ; tick = tick.AddSeconds(-interval))
            {
                var x = area.Right - (float)((latest - tick).TotalSeconds * pixelsPerSecond);
                if (x < area.Left)
                {
                    break;
                }

                DrawTick(g, area, gridPen, textBrush, x, tick.ToString("HH:mm:ss"));
            }
        }
        else
        {
            // 記録時刻が分からない場合は、経過時間(絶対サンプル位置 / 周波数)へフォールバックする。
            var latestSeconds = plot.LatestSampleIndex / (double)rate;
            for (var seconds = Math.Floor(latestSeconds / interval) * interval; seconds >= 0; seconds -= interval)
            {
                var x = area.Right - (float)((latestSeconds - seconds) * pixelsPerSecond);
                if (x < area.Left)
                {
                    break;
                }

                DrawTick(g, area, gridPen, textBrush, x, $"{seconds:0}s");
            }
        }
    }

    /// <summary>目盛り本数が <see cref="MaxTimeTicks"/> 以内に収まる、最も細かい間隔を選ぶ。</summary>
    private static double ChooseTickInterval(double windowSeconds)
    {
        foreach (var candidate in TimeTickIntervals)
        {
            if (windowSeconds / candidate <= MaxTimeTicks)
            {
                return candidate;
            }
        }

        return TimeTickIntervals[^1];
    }

    private void DrawTick(Graphics g, RectangleF area, Pen gridPen, Brush textBrush, float x, string label)
    {
        g.DrawLine(gridPen, x, area.Top, x, area.Bottom);

        // ラベルは目盛り中央に置きつつ、両端で見切れないようプロット領域内へ寄せる。
        var width = g.MeasureString(label, Font).Width;
        var left = Math.Clamp(x - width / 2, area.Left, Math.Max(area.Left, area.Right - width));
        g.DrawString(label, Font, textBrush, left, area.Bottom + 2);
    }

    private void DrawSeries(Graphics g, RectangleF area, ChartPlot plot)
    {
        var range = plot.YMax - plot.YMin;
        if (range <= 0 || plot.WindowSamples <= 0)
        {
            return;
        }

        var step = area.Width / plot.WindowSamples;
        var scale = area.Height / (float)range;

        foreach (var series in plot.Series)
        {
            var values = series.Values;
            if (values.Length < 2)
            {
                continue;
            }

            // DrawLines は配列を要求するので、長さが変わったときだけ確保し直して使い回す。
            // 同一フレーム内の系列は長さが揃うため、実質的に確保はウィンドウ変更時のみ。
            if (_pointBuffer.Length != values.Length)
            {
                _pointBuffer = new PointF[values.Length];
            }

            for (var i = 0; i < values.Length; i++)
            {
                var x = area.Right - (values.Length - 1 - i) * step;
                var y = area.Bottom - (float)(values[i] - plot.YMin) * scale;
                // 範囲外の値で座標が発散すると GDI+ が描画に失敗するため、少し外側で止める。
                _pointBuffer[i] = new PointF(x, Math.Clamp(y, area.Top - area.Height, area.Bottom + area.Height));
            }

            using var pen = new Pen(series.Color, 1.2f);
            g.DrawLines(pen, _pointBuffer);
        }
    }

    private void DrawArtifacts(Graphics g, RectangleF area, ChartPlot plot)
    {
        if (plot.Artifacts.Count == 0 || plot.WindowSamples <= 0)
        {
            return;
        }

        using var pen = new Pen(ArtifactColor);
        using var brush = new SolidBrush(ArtifactColor);
        var step = area.Width / plot.WindowSamples;

        foreach (var artifact in plot.Artifacts)
        {
            var samplesFromRight = plot.LatestSampleIndex - artifact.SampleIndex;
            var x = area.Right - samplesFromRight * step;
            if (x < area.Left || x > area.Right)
            {
                continue;
            }

            g.DrawLine(pen, x, area.Top, x, area.Bottom);
            if (!string.IsNullOrEmpty(artifact.Text))
            {
                g.DrawString(artifact.Text, Font, brush, x + 2, area.Top + 2);
            }
        }
    }

}
