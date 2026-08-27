using System.ComponentModel;

namespace MEME_Academic_Sample.Charting;

/// <summary>
/// チャート1枚ぶんのパネル。見出し・縦軸ズーム・カテゴリ選択・チャンネル切り替えと
/// 波形描画をまとめる。Mac 版 ChartPanelView に対応する。
/// </summary>
public sealed class ChartPanel : UserControl
{
    private static readonly string[] CategoryNames = ["Electrooculography", "Gyroscope", "Accelerometer"];

    private readonly Label _titleLabel = new();
    private readonly Button _zoomInButton = new();
    private readonly Button _zoomOutButton = new();
    private readonly ComboBox _categoryCombo = new();
    private readonly Button _applyButton = new();
    private readonly FlowLayoutPanel _togglePanel = new();
    private readonly ChartCanvas _canvas = new();

    private readonly List<CheckBox> _toggleBoxes = [];

    public ChartPanel()
    {
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(6);

        _titleLabel.Font = new Font(Font, FontStyle.Bold);
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;

        _zoomInButton.Text = "↕＋";
        _zoomInButton.Width = 42;
        _zoomInButton.Click += (_, _) => { Plot.ZoomInY(); UpdateZoomButtons(); Redraw(); };

        _zoomOutButton.Text = "↕－";
        _zoomOutButton.Width = 42;
        _zoomOutButton.Click += (_, _) => { Plot.ZoomOutY(); UpdateZoomButtons(); Redraw(); };

        _categoryCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _categoryCombo.Width = 170;
        _categoryCombo.Items.AddRange(CategoryNames);
        _categoryCombo.SelectedIndex = 0;

        _applyButton.Text = "Apply";
        _applyButton.Width = 60;
        _applyButton.Click += (_, _) => ApplyRequested?.Invoke(this, EventArgs.Empty);

        var headerButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        headerButtons.Controls.AddRange([_zoomInButton, _zoomOutButton, _categoryCombo, _applyButton]);

        var header = new Panel { Dock = DockStyle.Top, Height = 30 };
        header.Controls.Add(_titleLabel);
        header.Controls.Add(headerButtons);

        _togglePanel.Dock = DockStyle.Left;
        _togglePanel.Width = 130;
        _togglePanel.FlowDirection = FlowDirection.TopDown;
        _togglePanel.WrapContents = false;
        _togglePanel.Padding = new Padding(4, 8, 0, 0);

        _canvas.Dock = DockStyle.Fill;
        _canvas.Plot = Plot;
        _canvas.RowTapped += row => RowTapped?.Invoke(row);
        _canvas.RangeSelected += (start, end) => RangeSelected?.Invoke(start, end);

        var body = new Panel { Dock = DockStyle.Fill };
        body.Controls.Add(_canvas);
        body.Controls.Add(_togglePanel);

        Controls.Add(body);
        Controls.Add(header);

        ApplySelectedCategory();
    }

    /// <summary>Apply ボタンが押された。全チャート一括で適用するため親フォームが処理する。</summary>
    public event EventHandler? ApplyRequested;

    /// <summary>波形がクリックされた。Artifact を付ける対象のサンプル位置を渡す。</summary>
    public event Action<int>? RowTapped;

    /// <summary>波形上でドラッグ選択された区間(絶対サンプル位置)。</summary>
    public event Action<int, int>? RangeSelected;

    /// <summary>ドラッグによる範囲選択を受け付けるか。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool RangeSelectionEnabled
    {
        get => _canvas.RangeSelectionEnabled;
        set => _canvas.RangeSelectionEnabled = value;
    }

    /// <summary>見出しの通し番号(1..3)。</summary>
    [DefaultValue(1)]
    public int Index { get; set; } = 1;

    public ChartPlot Plot { get; } = new(-1200, 1200);

    /// <summary>実際に描画に反映されているカテゴリ。</summary>
    public ChartCategory AppliedCategory { get; private set; } = ChartCategory.Electrooculography;

    /// <summary>コンボボックスで選択中(未適用)のカテゴリ。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ChartCategory SelectedCategory
    {
        get => (ChartCategory)_categoryCombo.SelectedIndex;
        set => _categoryCombo.SelectedIndex = (int)value;
    }

    public EogToggles Eog { get; private set; } = new();

    public AxisToggles Gyro { get; private set; } = new();

    public AxisToggles Accel { get; private set; } = new();

    /// <summary>計測中など、設定変更を受け付けない状態にする。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool InputDisabled
    {
        set
        {
            _categoryCombo.Enabled = !value;
            _applyButton.Enabled = !value;
            foreach (var box in _toggleBoxes)
            {
                box.Enabled = !value;
            }
        }
    }

    /// <summary>コンボボックスの選択を実際の描画へ反映する。</summary>
    public void ApplySelectedCategory()
    {
        AppliedCategory = SelectedCategory;
        Plot.ApplyCategory(AppliedCategory);
        _titleLabel.Text = $"Chart{Index}：{CategoryNames[(int)AppliedCategory]}";
        BuildToggles();
        UpdateZoomButtons();
        Redraw();
    }

    public void Redraw() => _canvas.Invalidate();

    private void UpdateZoomButtons()
    {
        _zoomInButton.Enabled = Plot.CanZoomInY;
        _zoomOutButton.Enabled = Plot.CanZoomOutY;
    }

    private void BuildToggles()
    {
        foreach (var box in _toggleBoxes)
        {
            box.Dispose();
        }

        _toggleBoxes.Clear();
        _togglePanel.Controls.Clear();

        switch (AppliedCategory)
        {
            case ChartCategory.Electrooculography:
                AddToggle("Left", ChartColors.EogLeft, Eog.Left, on => { var t = Eog; t.Left = on; Eog = t; });
                AddToggle("Right", ChartColors.EogRight, Eog.Right, on => { var t = Eog; t.Right = on; Eog = t; });
                AddToggle("ΔH", ChartColors.EogDeltaH, Eog.DeltaH, on => { var t = Eog; t.DeltaH = on; Eog = t; });
                AddToggle("ΔV", ChartColors.EogDeltaV, Eog.DeltaV, on => { var t = Eog; t.DeltaV = on; Eog = t; });
                break;

            case ChartCategory.Gyroscope:
                AddToggle("X Axis", ChartColors.AxisX, Gyro.X, on => { var t = Gyro; t.X = on; Gyro = t; });
                AddToggle("Y Axis", ChartColors.AxisY, Gyro.Y, on => { var t = Gyro; t.Y = on; Gyro = t; });
                AddToggle("Z Axis", ChartColors.AxisZ, Gyro.Z, on => { var t = Gyro; t.Z = on; Gyro = t; });
                break;

            case ChartCategory.Accelerometer:
                AddToggle("X Axis", ChartColors.AxisX, Accel.X, on => { var t = Accel; t.X = on; Accel = t; });
                AddToggle("Y Axis", ChartColors.AxisY, Accel.Y, on => { var t = Accel; t.Y = on; Accel = t; });
                AddToggle("Z Axis", ChartColors.AxisZ, Accel.Z, on => { var t = Accel; t.Z = on; Accel = t; });
                break;
        }
    }

    private void AddToggle(string text, Color color, bool initial, Action<bool> onChanged)
    {
        var box = new CheckBox
        {
            Text = text,
            Checked = initial,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10),
            ForeColor = ControlPaint.Dark(color, 0.25f),
        };
        box.CheckedChanged += (_, _) =>
        {
            onChanged(box.Checked);
            Redraw();
        };

        _toggleBoxes.Add(box);
        _togglePanel.Controls.Add(box);
    }
}
