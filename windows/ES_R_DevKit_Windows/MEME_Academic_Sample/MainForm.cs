using MEMELib_Academic;
using MEME_Academic_Sample.Charting;
using MEME_Academic_Sample.Services;
using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample;

/// <summary>
/// フル機能ロガーのメイン画面。Mac 版 ContentView / MEMEViewModel に対応する。
/// </summary>
public partial class MainForm : Form
{
    /// <summary>画面更新の間隔。センサーは最大 100Hz で届くので、描画は間引く。</summary>
    private const int UiRefreshIntervalMs = 50;

    private static readonly int[] XRangeOptions = [3, 7, 15, 30];

    private static readonly MEMEMode[] SelectableModes =
        [MEMEMode.Standard, MEMEMode.Full, MEMEMode.Quaternion];

    /// <summary>再生速度倍率。ボタンを押すたびに順に切り替える。</summary>
    private static readonly int[] ReplaySpeedOptions = [1, 2, 4, 8, 16, 32];

    /// <summary>再生位置スライダーの目盛り数。</summary>
    private const int ScrubberResolution = 1000;

    private readonly UserSetting setting = UserSetting.Load();
    private readonly MEMELib memeLib = new();
    private readonly ChartService chartService = new();
    private readonly CommunicationStatsTracker stats = new();
    private readonly DataPersistenceService persistence = new();
    private readonly TcpOutputServer tcpServer = new();
    private readonly CsvReplayService replayService = new();
    private readonly System.Windows.Forms.Timer uiTimer;
    private readonly ChartPanel[] chartPanels;

    private Phase phase = Phase.Idle;
    private bool isScanning;
    private bool isFreeMarking;

    /// <summary>
    /// タップで付けた未書き戻しの Artifact(絶対チャートサンプル位置 → 文字列)。
    /// 計測停止時・再生停止時に CSV の ARTIFACT 列へ書き戻す。
    /// </summary>
    private readonly Dictionary<int, string> pendingArtifacts = [];

    /// <summary>起動引数で渡された CSV。<see cref="OnShown"/> で一度だけ読み込む。</summary>
    private string? initialReplayPath;

    private CsvReplayInfo? replayInfo;
    private int currentReplayIndex;
    private bool isReplayPaused;
    private bool isScrubbingReplay;
    private int replaySpeedIndex;

    /// <summary>X 軸レンジ(秒)。既定は 7 秒。</summary>
    private int xRangeIndex = 1;

    private MEMEMode mode = MEMEMode.Full;
    private MEMEQuality quality = MEMEQuality.High;
    private MEMEAccelRange accelRange = MEMEAccelRange.Range2G;
    private MEMEGyroRange gyroRange = MEMEGyroRange.Range250dps;

    /// <param name="initialReplayPath">
    /// 起動時に File Replay として開く CSV。エクスプローラーの「プログラムから開く」から渡される。
    /// </param>
    public MainForm(string? initialReplayPath = null)
    {
        this.initialReplayPath = initialReplayPath;
        InitializeComponent();
        Icon = AppInfo.LoadIcon();

        chartPanels = [chartPanel1, chartPanel2, chartPanel3];
        chartPanel1.SelectedCategory = ChartCategory.Electrooculography;
        chartPanel2.SelectedCategory = ChartCategory.Gyroscope;
        chartPanel3.SelectedCategory = ChartCategory.Accelerometer;
        foreach (var panel in chartPanels)
        {
            panel.ApplySelectedCategory();
            panel.ApplyRequested += (_, _) => ApplyChartSelection();
            panel.RowTapped += ChartTapped;
            panel.RangeSelected += ChartRangeSelected;
        }

        SetupOptions();
        lb_AppVersion.Text = $"Version {AppInfo.Version}";
        lb_LocalAddress.Text = $"IP address:{NetworkInfo.GetLocalIPv4Address()}";

        memeLib.memePeripheralFound += OnPeripheralFound;
        memeLib.memePeripheralConnected += OnPeripheralConnected;
        memeLib.memePeripheralDisconnected += OnPeripheralDisconnected;
        memeLib.memeAcademicStandardDataReceived += (_, data) => HandleSample(data);
        memeLib.memeAcademicFullDataReceived += (_, data) => HandleSample(data);
        memeLib.memeAcademicQuaternionDataReceived += (_, data) => HandleSample(data);

        stats.SuccessRateChanged += (value, text) => RunOnUi(() =>
        {
            lb_SuccessRate.Text = text;
            pb_SuccessRate.Value = (int)Math.Clamp(value, 0, 100);
        });
        stats.CommunicationChanged += (value, text) => RunOnUi(() =>
        {
            lb_Communication.Text = text;
            pb_Communication.Value = (int)Math.Clamp(value, 0, 100);
        });

        // 整形済みの CSV 行をそのまま TCP へ流す(Mac 版と同じ書式)。
        persistence.RowFormatted += line => tcpServer.Send(line);
        tcpServer.StatusChanged += status => RunOnUi(() => lb_SocketStatus.Text = status);

        ApplySettings();

        uiTimer = new System.Windows.Forms.Timer { Interval = UiRefreshIntervalMs };
        uiTimer.Tick += (_, _) => RefreshCharts();
        uiTimer.Start();

        UpdateUiState();
    }

    /// <summary>
    /// 起動引数で CSV を渡されていれば読み込む。ウィンドウが出てから実行するので、
    /// 形式が違ったときのエラーダイアログにも親ウィンドウが付く。
    /// </summary>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (initialReplayPath is { } path)
        {
            initialReplayPath = null;
            LoadReplayFile(path);
        }
    }

    private enum Phase
    {
        Idle,
        DeviceFound,
        Connected,
        Measuring,
        ReplayReady,
        Replaying,
    }

    /// <summary>チャートの時間軸に使う周波数。再生中は再生元 CSV の Trans Speed に従う。</summary>
    private int SampleRate =>
        replayInfo is not null && phase is Phase.ReplayReady or Phase.Replaying
            ? (replayInfo.Quality == MEMEQuality.High ? 100 : 50)
            : (quality == MEMEQuality.High ? 100 : 50);

    private int ReplaySpeed => ReplaySpeedOptions[replaySpeedIndex];

    private int XRangeSeconds => XRangeOptions[xRangeIndex];

    #region Setup

    private void SetupOptions()
    {
        cb_SelectMode.Items.AddRange(["Standard", "Full", "Quaternion"]);
        cb_SelectMode.SelectedIndex = Array.IndexOf(SelectableModes, MEMEMode.Full);
        cb_SelectMode.SelectedIndexChanged += (_, _) =>
            mode = SelectableModes[cb_SelectMode.SelectedIndex];

        cb_TransSpeed.Items.AddRange(["100Hz", "50Hz"]);
        cb_TransSpeed.SelectedIndex = 0;
        cb_TransSpeed.SelectedIndexChanged += (_, _) =>
            quality = cb_TransSpeed.SelectedIndex == 0 ? MEMEQuality.High : MEMEQuality.Low;

        cb_AccelRange.Items.AddRange(["±2G", "±4G", "±8G", "±16G"]);
        cb_AccelRange.SelectedIndex = 0;
        cb_AccelRange.SelectedIndexChanged += (_, _) =>
            accelRange = (MEMEAccelRange)cb_AccelRange.SelectedIndex;

        cb_GyroRange.Items.AddRange(["±250dps", "±500dps", "±1000dps", "±2000dps"]);
        cb_GyroRange.SelectedIndex = 0;
        cb_GyroRange.SelectedIndexChanged += (_, _) =>
            gyroRange = (MEMEGyroRange)cb_GyroRange.SelectedIndex;
    }

    /// <summary>Setting の内容をチャート・TCP 出力へ反映する。</summary>
    private void ApplySettings()
    {
        chartService.AccelOffsetX = setting.AccOffsetX;
        chartService.AccelOffsetY = setting.AccOffsetY;
        chartService.AccelOffsetZ = setting.AccOffsetZ;
        chartService.ConvertToLocalTime = setting.ConvertToLocalTime;

        lb_LocalPort.Text = $"Port:{setting.LocalPort}";

        if (setting.ExternalOutputSocket)
        {
            tcpServer.Start(setting.LocalPort);
        }
        else
        {
            tcpServer.Stop();
        }

        RefreshCharts();
    }

    #endregion

    #region MEMELib callbacks

    private void OnPeripheralFound(object sender, MEMEStatus result, MEMEDevice? device)
    {
        if (result == MEMEStatus.MEMELIB_OK && device is not null)
        {
            RunOnUi(() =>
            {
                cb_DeviceList.Items.Add(device);
                if (cb_DeviceList.SelectedIndex < 0)
                {
                    cb_DeviceList.SelectedIndex = 0;
                }

                phase = Phase.DeviceFound;
                UpdateUiState();
            });
        }
        else if (result == MEMEStatus.MEMELIB_TIMEOUT)
        {
            RunOnUi(() =>
            {
                isScanning = false;
                if (cb_DeviceList.Items.Count == 0)
                {
                    lb_ConnectionState.Text = "State : No device found";
                }

                UpdateUiState();
            });
        }
    }

    private void OnPeripheralConnected(object sender, MEMEStatus result)
    {
        if (result == MEMEStatus.MEMELIB_OK)
        {
            // 端末が保持している設定を画面へ反映する。
            mode = memeLib.getMode();
            quality = memeLib.getQuality();
            accelRange = memeLib.getAccelRange();
            gyroRange = memeLib.getGyroRange();

            RunOnUi(() =>
            {
                phase = Phase.Connected;
                lb_ConnectionState.Text = "State : Connected";
                lb_MemeVersion.Text = $"MEME Version：{memeLib.getFWVersion()}";

                var modeIndex = Array.IndexOf(SelectableModes, mode);
                if (modeIndex >= 0)
                {
                    cb_SelectMode.SelectedIndex = modeIndex;
                }

                cb_TransSpeed.SelectedIndex = quality == MEMEQuality.High ? 0 : 1;
                cb_AccelRange.SelectedIndex = (int)accelRange;
                cb_GyroRange.SelectedIndex = (int)gyroRange;
                UpdateUiState();
            });
        }
        else
        {
            var reason = result == MEMEStatus.MEMELIB_TIMEOUT ? "timeout" : "failed";
            RunOnUi(() =>
            {
                phase = cb_DeviceList.Items.Count > 0 ? Phase.DeviceFound : Phase.Idle;
                lb_ConnectionState.Text = $"State : Connect {reason}";
                UpdateUiState();
            });
        }
    }

    private void OnPeripheralDisconnected(object sender, MEMEStatus result)
    {
        var wasMeasuring = phase == Phase.Measuring;
        stats.StopMeasurement();
        persistence.End();
        FlushLiveArtifacts();

        RunOnUi(() =>
        {
            phase = cb_DeviceList.Items.Count > 0 ? Phase.DeviceFound : Phase.Idle;
            lb_ConnectionState.Text = result == MEMEStatus.MEMELIB_OK
                ? "State : Disconnected"
                : "State : Disconnected (link lost)";
            UpdateUiState();
            if (wasMeasuring)
            {
                OfferSaveFileDialog();
            }
        });
    }

    /// <summary>Standard / Full / Quaternion で共通の受信処理。</summary>
    private void HandleSample(AcademicData data)
    {
        data.RecordedUtc = DateTime.UtcNow;

        // 1 件目は端末カウンタの基準取得だけに使い、記録しない。
        if (!stats.RegisterPacket(data.Cnt))
        {
            return;
        }

        stats.BumpDataCount();
        chartService.Append(data);

        if (phase != Phase.Measuring)
        {
            return;
        }

        var freeMarking = isFreeMarking;
        isFreeMarking = false;
        persistence.Append(data, stats.TotalCount, freeMarking);
    }

    #endregion

    #region UI state

    private void RunOnUi(Action action)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(action);
        }
        else
        {
            action();
        }
    }

    private void UpdateUiState()
    {
        var measuring = phase == Phase.Measuring;
        var connected = phase is Phase.Connected or Phase.Measuring;
        var replaying = phase == Phase.Replaying;
        var inReplaySession = phase is Phase.ReplayReady or Phase.Replaying;
        // 計測中・再生中は端末パラメータやチャート構成を触らせない(Mac 版 isInputDisabled)。
        var inputDisabled = measuring || inReplaySession;

        bt_Scan.Text = isScanning ? "Stop Scan" : "Scan";
        bt_Scan.Enabled = !connected && !inReplaySession;
        cb_DeviceList.Enabled = !connected && !isScanning && !inReplaySession;

        // 再生中の Connect は「再生セッションを終える」ボタンとして働く(Mac 版と同じ)。
        bt_Connect.Enabled = connected || inReplaySession || cb_DeviceList.SelectedItem is MEMEDevice;
        bt_Connect.Text = connected || inReplaySession ? "Disconnect" : "Connect";

        // BLE 接続中は CSV 再生に入れない。
        bt_FileReplay.Enabled = !connected;

        bt_Measurement.Visible = !replaying;
        bt_Measurement.Enabled = connected;
        bt_Measurement.Text = measuring ? "Stop Measurement" : "Start Measurement";
        bt_FreeMarking.Enabled = measuring;

        bt_ReplayRecord.Visible = replaying;
        bt_ReplayPause.Visible = replaying;
        bt_ReplayPause.Text = isReplayPaused ? "Resume" : "Pause";
        replayPanel.Visible = replaying;
        bt_ReplaySpeed.Text = $"x{ReplaySpeed}";

        settingToolStripMenuItem.Enabled = !measuring;
        cb_SelectMode.Enabled = !inputDisabled;
        cb_TransSpeed.Enabled = !inputDisabled;
        cb_AccelRange.Enabled = !inputDisabled;
        cb_GyroRange.Enabled = !inputDisabled;

        bt_XRangeIn.Enabled = xRangeIndex > 0;
        bt_XRangeOut.Enabled = xRangeIndex < XRangeOptions.Length - 1;
        lb_XRange.Text = $"{XRangeSeconds}s";

        foreach (var panel in chartPanels)
        {
            panel.InputDisabled = inputDisabled;
            // 区間の切り出しは再生元 CSV が要るので、ファイル再生中だけ受け付ける。
            panel.RangeSelectionEnabled = replaying;
        }
    }

    private void RefreshCharts()
    {
        chartService.UpdatePlots(chartPanels, SampleRate, XRangeSeconds, CurrentChartArtifacts());
        foreach (var panel in chartPanels)
        {
            panel.Redraw();
        }
    }

    private void ApplyChartSelection()
    {
        foreach (var panel in chartPanels)
        {
            panel.ApplySelectedCategory();
        }

        RefreshCharts();
    }

    #endregion

    #region Actions

    private void bt_Scan_Click(object sender, EventArgs e)
    {
        if (isScanning)
        {
            memeLib.stopScanningPeripherals();
            isScanning = false;
            lb_ConnectionState.Text = "State : Disconnected";
            UpdateUiState();
            return;
        }

        cb_DeviceList.Items.Clear();
        phase = Phase.Idle;
        lb_ConnectionState.Text = "State : Scanning...";

        if (memeLib.startScanningPeripherals() == MEMEStatus.MEMELIB_OK)
        {
            isScanning = true;
        }
        else
        {
            lb_ConnectionState.Text = "State : Bluetooth unavailable";
        }

        UpdateUiState();
    }

    private void bt_Connect_Click(object sender, EventArgs e)
    {
        if (phase is Phase.ReplayReady or Phase.Replaying)
        {
            EndReplaySession();
            return;
        }

        if (phase is Phase.Connected or Phase.Measuring)
        {
            if (phase == Phase.Measuring)
            {
                StopMeasurement();
            }

            memeLib.disconnectPeripheral();
            return;
        }

        if (cb_DeviceList.SelectedItem is not MEMEDevice device)
        {
            return;
        }

        isScanning = false;
        lb_ConnectionState.Text = "State : Connecting...";
        bt_Connect.Enabled = false;
        memeLib.connectPeripheral(device);
    }

    private void bt_Measurement_Click(object sender, EventArgs e)
    {
        if (phase == Phase.Measuring)
        {
            StopMeasurement();
            return;
        }

        StartMeasurement();
    }

    private void StartMeasurement()
    {
        memeLib.setMode(mode, quality);
        memeLib.setAccelRange(accelRange);
        memeLib.setGyroRange(gyroRange);

        chartService.Reset();
        foreach (var panel in chartPanels)
        {
            panel.Plot.Reset();
        }

        stats.Reset();
        stats.StartMeasurement((int)quality);
        isFreeMarking = false;
        pendingArtifacts.Clear();

        var header = DataPersistenceService.BuildHeader(mode, quality, accelRange, gyroRange);
        persistence.Begin(setting.EnsureSaveDirectory(), CurrentDeviceAddress(), header, quality);
        tcpServer.SetHeader(header);

        memeLib.startDataReport();
        phase = Phase.Measuring;
        UpdateUiState();
    }

    private void StopMeasurement()
    {
        memeLib.stopDataReport();
        stats.StopMeasurement();
        persistence.End();
        FlushLiveArtifacts();
        phase = Phase.Connected;
        UpdateUiState();
        OfferSaveFileDialog();
    }

    /// <summary>Setting が ON なら、確定した CSV を任意の場所へ保存し直せるようにする。</summary>
    private void OfferSaveFileDialog()
    {
        var source = persistence.CurrentFilePath;
        if (!setting.ShowSaveFileDialog || source is null || !File.Exists(source))
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = Path.GetFileName(source),
            InitialDirectory = Path.GetDirectoryName(source) ?? string.Empty,
            OverwritePrompt = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            // 移動先が同じなら何もしない(File.Move が失敗するため)。
            if (!string.Equals(Path.GetFullPath(dialog.FileName), Path.GetFullPath(source),
                    StringComparison.OrdinalIgnoreCase))
            {
                File.Move(source, dialog.FileName, overwrite: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"保存できませんでした。\n{ex.Message}", "Save",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private string CurrentDeviceAddress() =>
        (cb_DeviceList.SelectedItem as MEMEDevice)?.Address ?? "UNKNOWN";

    private void bt_FreeMarking_Click(object sender, EventArgs e) => isFreeMarking = true;

    private void bt_XRangeIn_Click(object sender, EventArgs e)
    {
        if (xRangeIndex > 0)
        {
            xRangeIndex--;
            OnXRangeChanged();
        }
    }

    private void bt_XRangeOut_Click(object sender, EventArgs e)
    {
        if (xRangeIndex < XRangeOptions.Length - 1)
        {
            xRangeIndex++;
            OnXRangeChanged();
        }
    }

    private void OnXRangeChanged()
    {
        UpdateUiState();
        if (phase == Phase.Replaying)
        {
            // レンジが変わったら、新しい幅ぶんを再生位置で終わるように詰め直す。
            FillReplayWindow(currentReplayIndex);
            return;
        }

        RefreshCharts();
    }

    private void settingToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var form = new SettingsForm(setting);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            ApplySettings();
        }
    }

    /// <summary>
    /// アプリを終了する。後片付けは MainForm_FormClosing がまとめて行うので、
    /// ここでは閉じるだけでよい(計測停止・CSV フラッシュ・BLE 切断)。
    /// </summary>
    private void quitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

    private void versionToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var form = new VersionForm();
        form.ShowDialog(this);
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        uiTimer.Stop();
        if (phase == Phase.Measuring)
        {
            memeLib.stopDataReport();
        }

        replayService.Dispose();
        persistence.Dispose();
        tcpServer.Dispose();
        stats.Dispose();
        memeLib.Dispose();
    }

    #endregion

    #region File Replay

    private void bt_FileReplay_Click(object sender, EventArgs e)
    {
        if (isScanning)
        {
            memeLib.stopScanningPeripherals();
            isScanning = false;
        }

        if (phase is Phase.ReplayReady or Phase.Replaying)
        {
            EndReplaySession();
        }

        using var dialog = new OpenFileDialog
        {
            Filter = "MEME CSV (*.csv)|*.csv|All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(setting.SaveFilePath) ? setting.SaveFilePath : string.Empty,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadReplayFile(dialog.FileName);
        }
    }

    private void LoadReplayFile(string path)
    {
        if (!CsvReplayService.TryParse(path, out var info, out var error) || info is null)
        {
            MessageBox.Show(this, $"{Path.GetFileName(path)}\n{error}", "File Replay",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        replayInfo = info;

        // 再生元の計測条件を画面へ反映する。
        var modeIndex = Array.IndexOf(SelectableModes, info.Mode);
        if (modeIndex >= 0)
        {
            cb_SelectMode.SelectedIndex = modeIndex;
        }

        cb_TransSpeed.SelectedIndex = info.Quality == MEMEQuality.High ? 0 : 1;
        cb_AccelRange.SelectedIndex = (int)info.AccelRange;
        cb_GyroRange.SelectedIndex = (int)info.GyroRange;
        lb_ConnectionState.Text = $"State : {info.FileName}";

        phase = Phase.ReplayReady;
        // Mac 版と同じく、読み込んだらそのまま再生を始める(Start Replay ボタンは持たない)。
        StartReplay();
    }

    private void StartReplay()
    {
        if (phase != Phase.ReplayReady || replayInfo is null)
        {
            return;
        }

        phase = Phase.Replaying;
        isReplayPaused = false;
        replaySpeedIndex = 0;
        pendingArtifacts.Clear();
        replayService.Start(replayInfo.Rows, replayInfo.Quality, OnReplayRow, OnReplayFinished);

        // ウィンドウ幅ぶんを先読みして、最初から埋まった状態で再生を始める。
        FillReplayWindow(XRangeSeconds * SampleRate - 1);
        UpdateUiState();
    }

    /// <summary>Record ボタン。再生を止めるがグラフは残す(Mac 版 finishReplay)。</summary>
    private void bt_ReplayRecord_Click(object sender, EventArgs e)
    {
        if (phase != Phase.Replaying)
        {
            return;
        }

        replayService.Stop();
        // 停止時に、タップで付けた Artifact を再生元 CSV へ書き戻す。
        FlushReplayArtifacts(reload: true);
        phase = Phase.ReplayReady;
        isReplayPaused = false;
        ResetStatsDisplay();
        tb_ReplayProgress.Value = 0;
        UpdateUiState();
    }

    /// <summary>再生セッションを完全に終える。保持している行データも解放する。</summary>
    private void EndReplaySession()
    {
        replayService.Clear();
        // replayInfo はこの後捨てるので読み直しは要らない。
        FlushReplayArtifacts(reload: false);
        replayInfo = null;
        currentReplayIndex = 0;
        isReplayPaused = false;
        phase = Phase.Idle;
        lb_ConnectionState.Text = "State : Disconnected";
        ResetStatsDisplay();
        tb_ReplayProgress.Value = 0;
        UpdateUiState();
    }

    private void ResetStatsDisplay()
    {
        stats.Reset();
        lb_SuccessRate.Text = "0.0%";
        pb_SuccessRate.Value = 0;
        lb_Communication.Text = "0.0%";
        pb_Communication.Value = 0;
    }

    /// <summary>再生タイマーから 1 行ぶん渡される(UI スレッド)。</summary>
    private void OnReplayRow(AcademicData data, int index, int total)
    {
        currentReplayIndex = index;
        if (!isScrubbingReplay)
        {
            UpdateScrubber(index, total);
        }

        chartService.Append(data);
    }

    /// <summary>末尾に到達。Record 状態には戻さず、末尾で一時停止した状態にする。</summary>
    private void OnReplayFinished()
    {
        if (phase != Phase.Replaying)
        {
            return;
        }

        isReplayPaused = true;
        UpdateUiState();
        RefreshCharts();
    }

    private void bt_ReplayPause_Click(object sender, EventArgs e)
    {
        if (phase != Phase.Replaying)
        {
            return;
        }

        if (isReplayPaused)
        {
            if (replayService.Resume())
            {
                isReplayPaused = false;
            }
        }
        else
        {
            replayService.Pause();
            isReplayPaused = true;
        }

        UpdateUiState();
    }

    private void bt_ReplaySpeed_Click(object sender, EventArgs e)
    {
        if (phase != Phase.Replaying)
        {
            return;
        }

        replaySpeedIndex = (replaySpeedIndex + 1) % ReplaySpeedOptions.Length;
        replayService.SetSpeed(ReplaySpeed);
        UpdateUiState();
    }

    /// <summary>&lt;&lt; / &gt;&gt; の移動量。前後のウィンドウが 2 秒重なって連続して見えるようにする。</summary>
    private int ReplayJumpSeconds => Math.Max(1, XRangeSeconds - 2);

    private void bt_ReplayBack_Click(object sender, EventArgs e) => JumpReplay(-ReplayJumpSeconds);

    private void bt_ReplayForward_Click(object sender, EventArgs e) => JumpReplay(ReplayJumpSeconds);

    private void JumpReplay(int seconds)
    {
        if (phase != Phase.Replaying)
        {
            return;
        }

        FillReplayWindow(currentReplayIndex + seconds * SampleRate);
    }

    private void tb_ReplayProgress_Scroll(object sender, EventArgs e) => isScrubbingReplay = true;

    private void tb_ReplayProgress_Released(object sender, MouseEventArgs e) => FinishScrub();

    private void tb_ReplayProgress_KeyUp(object sender, KeyEventArgs e) => FinishScrub();

    private void FinishScrub()
    {
        if (!isScrubbingReplay)
        {
            return;
        }

        isScrubbingReplay = false;
        if (phase != Phase.Replaying || replayInfo is null || replayInfo.Rows.Count < 2)
        {
            return;
        }

        var progress = tb_ReplayProgress.Value / (double)ScrubberResolution;
        FillReplayWindow((int)(progress * (replayInfo.Rows.Count - 1)));
    }

    private void UpdateScrubber(int index, int total)
    {
        var value = total > 1
            ? (int)Math.Round(index / (double)(total - 1) * ScrubberResolution)
            : 0;
        tb_ReplayProgress.Value = Math.Clamp(value, tb_ReplayProgress.Minimum, tb_ReplayProgress.Maximum);
    }

    /// <summary>
    /// 指定行で終わるウィンドウ幅ぶんを先読みしてグラフを満たし、続きをその次の行から読ませる。
    /// 再生開始・シーク・レンジ変更のいずれでも、右端から徐々に埋めるのではなく
    /// 最初から満たした状態で描画を再開できる。
    /// </summary>
    private void FillReplayWindow(int endRow)
    {
        if (replayInfo is null || replayInfo.Rows.Count == 0)
        {
            return;
        }

        var rows = replayInfo.Rows;
        var windowSamples = Math.Max(1, XRangeSeconds * SampleRate);
        // 先頭付近では常に先頭ウィンドウを表示する。これより手前へ戻すと左端が負の時刻になる。
        var minEnd = Math.Min(windowSamples - 1, rows.Count - 1);
        var clamped = Math.Clamp(endRow, minEnd, rows.Count - 1);
        var start = Math.Max(0, clamped - windowSamples + 1);

        currentReplayIndex = clamped;
        UpdateScrubber(clamped, rows.Count);

        chartService.Reset(start);
        for (var i = start; i <= clamped; i++)
        {
            chartService.Append(rows[i]);
        }

        RefreshCharts();
        replayService.Seek(clamped + 1);
    }

    #endregion

    #region Artifact / range cut

    /// <summary>
    /// 各チャートへ重ねる Artifact。再生中は CSV に記録済みのものとタップで付けたものを
    /// 併せ、計測中はタップぶんだけを返す。
    /// </summary>
    private IReadOnlyDictionary<int, string>? CurrentChartArtifacts()
    {
        switch (phase)
        {
            case Phase.Replaying or Phase.ReplayReady:
                if (replayInfo is null)
                {
                    return null;
                }

                if (pendingArtifacts.Count == 0)
                {
                    return replayInfo.Artifacts;
                }

                var merged = new Dictionary<int, string>(replayInfo.Artifacts);
                foreach (var (row, text) in pendingArtifacts)
                {
                    merged[row] = text;
                }

                return merged;

            case Phase.Measuring:
                return pendingArtifacts;

            default:
                return null;
        }
    }

    /// <summary>チャートがクリックされた。対象サンプルに Artifact を付ける。</summary>
    private void ChartTapped(int row)
    {
        int target;
        switch (phase)
        {
            case Phase.Replaying:
                if (replayInfo is null || replayInfo.Rows.Count == 0)
                {
                    return;
                }

                target = Math.Clamp(row, 0, replayInfo.Rows.Count - 1);
                break;

            case Phase.Measuring:
                // 計測中はストリームに上限が無いので下限だけ丸める。
                target = Math.Max(row, 0);
                break;

            default:
                return;
        }

        using var dialog = new ArtifactForm();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        pendingArtifacts[target] = dialog.ArtifactText;
        // 一時停止中は次の tick が来ないため、付けた直後に反映されるよう描き直す。
        RefreshCharts();
    }

    /// <summary>
    /// 再生中にタップで付けた Artifact を再生元 CSV へ書き戻す。
    /// <paramref name="reload"/> が true なら読み直して、続けて再生したときに反映されるようにする。
    /// </summary>
    private void FlushReplayArtifacts(bool reload)
    {
        if (pendingArtifacts.Count == 0 || replayInfo is null)
        {
            return;
        }

        try
        {
            CsvReplayService.ApplyArtifacts(replayInfo.FilePath, pendingArtifacts);
            if (reload && CsvReplayService.TryParse(replayInfo.FilePath, out var refreshed, out _) && refreshed is not null)
            {
                replayInfo = refreshed;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, $"Artifact を書き戻せませんでした。\n{e.Message}", "Artifact",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        pendingArtifacts.Clear();
    }

    /// <summary>
    /// 計測中にタップで付けた Artifact を、保存した CSV の ARTIFACT 列へ書き戻す。
    /// pendingArtifacts のキーは絶対チャートサンプル位置。CSV は先頭パケットを 1 件落とすため、
    /// データ行番号 = サンプル位置 − 1(サンプル 0 は CSV に無いので除く)。
    /// </summary>
    private void FlushLiveArtifacts()
    {
        if (pendingArtifacts.Count == 0)
        {
            return;
        }

        var path = persistence.CurrentFilePath;
        if (path is null || !File.Exists(path))
        {
            pendingArtifacts.Clear();
            return;
        }

        var rowKeyed = new Dictionary<int, string>();
        foreach (var (sampleIndex, text) in pendingArtifacts)
        {
            if (sampleIndex >= 1)
            {
                rowKeyed[sampleIndex - 1] = text;
            }
        }

        try
        {
            CsvReplayService.ApplyArtifacts(path, rowKeyed);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, $"Artifact を書き戻せませんでした。\n{e.Message}", "Artifact",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        pendingArtifacts.Clear();
    }

    /// <summary>チャート上でドラッグ選択された区間を、別の CSV として切り出す。</summary>
    private void ChartRangeSelected(int startRow, int endRow)
    {
        if (phase != Phase.Replaying || replayInfo is null || replayInfo.Rows.Count == 0)
        {
            return;
        }

        var maxRow = replayInfo.Rows.Count - 1;
        var start = Math.Clamp(Math.Min(startRow, endRow), 0, maxRow);
        var end = Math.Clamp(Math.Max(startRow, endRow), 0, maxRow);
        if (start >= end)
        {
            return;
        }

        var directory = Path.GetDirectoryName(replayInfo.FilePath) ?? ".";
        using var dialog = new CutFileForm(directory, DefaultCutFileName(replayInfo.FilePath), end - start + 1);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            CsvReplayService.ExportRange(replayInfo.FilePath, dialog.DestinationPath, start, end);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, $"切り出せませんでした。\n{e.Message}", "Save selected range",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>"current.csv" なら "current_1.csv"、既にあれば "current_2.csv" … と空きを探す。</summary>
    private static string DefaultCutFileName(string sourcePath)
    {
        var directory = Path.GetDirectoryName(sourcePath) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        for (var n = 1; n < 1000; n++)
        {
            var candidate = $"{baseName}_{n}.csv";
            if (!File.Exists(Path.Combine(directory, candidate)))
            {
                return candidate;
            }
        }

        return $"{baseName}_cut.csv";
    }

    #endregion
}
