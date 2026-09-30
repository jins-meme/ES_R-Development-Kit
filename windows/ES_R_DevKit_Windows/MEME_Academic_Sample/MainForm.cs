using System.Text.Json;
using MEMELib_Academic;
using MEME_Academic_Sample.Services;
using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample;

/// <summary>
/// フル機能ロガーのメイン画面。Mac 版 ContentView / MEMEViewModel に対応する。
/// グラフは WebView2(webview/ の標準版 zip、または設定で選んだ zip)が描く。受信したサンプルは
/// <see cref="WebBridge"/> で流し、CSV 再生もページが受け持つ(アプリはファイルを仮想ホストに出すだけ)。
/// アーティファクトはページで入力され、<see cref="WebBridge.Artifact"/> で届く。CSV への書き戻しは従来どおり停止時。
/// </summary>
public partial class MainForm : Form
{
    private static readonly MEMEMode[] SelectableModes =
        [MEMEMode.Standard, MEMEMode.Full, MEMEMode.Quaternion];

    /// <summary>accelRange / gyroRange の番号 → g / dps(グラフの換算に渡す)</summary>
    private static readonly int[] AccelG = [2, 4, 8, 16];

    private static readonly int[] GyroDps = [250, 500, 1000, 2000];

    /// <summary>各モードで 1 サンプルぶんとしてページへ渡す列(CSV の列名と同じ)。</summary>
    private static readonly string[] FullColumns =
        ["ACC_X", "ACC_Y", "ACC_Z", "GYRO_X", "GYRO_Y", "GYRO_Z", "EOG_L", "EOG_R", "EOG_H", "EOG_V"];

    private static readonly string[] StandardColumns =
        ["ACC_X", "ACC_Y", "ACC_Z", "EOG_L1", "EOG_R1", "EOG_L2", "EOG_R2", "EOG_H1", "EOG_H2", "EOG_V1", "EOG_V2"];

    /// <summary>Disconnect を押しっぱなしにして Shelf mode の確認ダイアログが出るまでの時間。</summary>
    private const int ShelfLongPressMs = 5000;

    /// <summary>SHELF 送信後、端末が自ら切断するのを待つ時間(切断＝移行成功)。</summary>
    private const int ShelfDisconnectTimeoutMs = 5000;

    private readonly UserSetting setting = UserSetting.Load();
    private readonly MEMELib memeLib = new();
    private readonly CommunicationStatsTracker stats = new();
    private readonly DataPersistenceService persistence = new();
    private readonly TcpOutputServer tcpServer = new();
    private readonly System.Windows.Forms.Timer shelfLongPressTimer;
    private readonly System.Windows.Forms.Timer shelfDisconnectTimer;
    private readonly WebContentStore webContent;
    private readonly WebBridge web;

    private Phase phase = Phase.Idle;
    private bool isScanning;
    private bool isFreeMarking;

    /// <summary>Shelf 移行コマンドの送信中。完了は端末側からの切断で判断する。</summary>
    private bool isEnteringShelf;

    /// <summary>
    /// 長押しが成立した押下の Click を 1 回だけ捨てるフラグ。WinForms の Click は
    /// マウスを離した時点で走るため、これが無いと確認ダイアログを出しながら切断してしまう。
    /// </summary>
    private bool suppressConnectClick;

    /// <summary>
    /// ページで付けた未書き戻しの Artifact(計測中はサンプル番号、再生中は CSV のデータ行の番号 → 文字列)。
    /// 計測停止時・再生の Save Artifacts / Disconnect で CSV の ARTIFACT 列へ書き戻す。
    /// </summary>
    private readonly Dictionary<int, string> pendingArtifacts = [];

    /// <summary>
    /// 計測中のサンプル番号(計測開始から 0, 1, 2 …。先頭パケットも数える)。グラフ画面へ渡し、
    /// アーティファクトはこの番号で返ってくる。CSV は先頭パケットを 1 件落とすので、データ行 = 番号 − 1。
    /// 受信スレッドだけが進める。
    /// </summary>
    private int liveSampleIndex;

    /// <summary>起動引数で渡された CSV。<see cref="OnShown"/> で一度だけ読み込む。</summary>
    private string? initialReplayPath;

    /// <summary>再生中の CSV(ページが読んで再生する)</summary>
    private string? replayFile;

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

        webContent = new WebContentStore(setting);
        web = new WebBridge(webContent, webHost);
        web.Artifact += ReceiveArtifact;
        web.ReplayInfo += ApplyReplayInfo;

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

        // Disconnect の長押しで Shelf mode へ。隠し操作なので、押している間の表示は変えない。
        shelfLongPressTimer = new System.Windows.Forms.Timer { Interval = ShelfLongPressMs };
        shelfLongPressTimer.Tick += (_, _) => OnShelfLongPress();
        bt_Connect.MouseDown += bt_Connect_MouseDown;
        bt_Connect.MouseUp += (_, _) => shelfLongPressTimer.Stop();
        bt_Connect.MouseLeave += (_, _) => shelfLongPressTimer.Stop();

        shelfDisconnectTimer = new System.Windows.Forms.Timer { Interval = ShelfDisconnectTimeoutMs };
        shelfDisconnectTimer.Tick += (_, _) => FinishShelfMode(entered: false);

        UpdateUiState();
    }

    /// <summary>
    /// グラフ画面(WebView2)を用意し、起動引数で CSV を渡されていれば読み込む。ウィンドウが出てから実行するので、
    /// 形式が違ったときのエラーダイアログにも親ウィンドウが付く。
    /// </summary>
    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await web.InitializeAsync();
        if (initialReplayPath is { } path)
        {
            initialReplayPath = null;
            LoadReplayFile(path);
        }
#if DEBUG
        StartAutoTestIfRequested(this, Program.Args);
#endif
    }

    private enum Phase
    {
        Idle,
        DeviceFound,
        Connected,
        Measuring,
        Replaying,
    }

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
        lb_LocalPort.Text = $"Port:{setting.LocalPort}";

        if (setting.ExternalOutputSocket)
        {
            tcpServer.Start(setting.LocalPort);
        }
        else
        {
            tcpServer.Stop();
        }
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

        RunOnUi(() =>
        {
            // pendingArtifacts は UI スレッドで持つので、書き戻しもここで(CSV は上で閉じ済み)
            FlushLiveArtifacts();
            if (wasMeasuring)
            {
                web.Stop();
            }

            phase = cb_DeviceList.Items.Count > 0 ? Phase.DeviceFound : Phase.Idle;
            lb_ConnectionState.Text = result == MEMEStatus.MEMELIB_OK
                ? "State : Disconnected"
                : "State : Disconnected (link lost)";
            UpdateUiState();
            // SHELF 送信後の切断は、端末が移行を受理した合図。
            if (isEnteringShelf)
            {
                FinishShelfMode(entered: true);
            }

            if (wasMeasuring)
            {
                OfferSaveFileDialog();
            }
        });
    }

    /// <summary>Standard / Full / Quaternion で共通の受信処理(受信スレッド)。</summary>
    private void HandleSample(AcademicData data)
    {
        data.RecordedUtc = DateTime.UtcNow;
        var measuring = phase == Phase.Measuring;

        // グラフへは先頭パケットも渡す(サンプル番号は計測開始からの全パケットの通し番号。BRIDGE.md の push)
        var i = -1;
        if (measuring)
        {
            i = liveSampleIndex++;
            PushToGraph(data, i);
        }

        // 1 件目は端末カウンタの基準取得だけに使い、記録しない。
        if (!stats.RegisterPacket(data.Cnt))
        {
            return;
        }

        stats.BumpDataCount();

        if (!measuring)
        {
            return;
        }

        var freeMarking = isFreeMarking;
        isFreeMarking = false;
        persistence.Append(data, stats.TotalCount, freeMarking);

        // CSV に x を書いた行と同じサンプル位置へ印を出す(Quaternion はグラフが無いので出さない)
        if (freeMarking && data is not AcademicQuaternionData)
        {
            RunOnUi(() => web.Mark(i, "x"));
        }
    }

    /// <summary>1 サンプルをグラフ画面へ(値は start の columns の並び)。</summary>
    private void PushToGraph(AcademicData data, int i)
    {
        switch (data)
        {
            case AcademicFullData f:
                web.Push(i, [f.AccX, f.AccY, f.AccZ, f.GyroX, f.GyroY, f.GyroZ, f.EogL, f.EogR, f.EogH, f.EogV]);
                break;
            case AcademicStandardData d:
                web.Push(i, [d.AccX, d.AccY, d.AccZ, d.EogL1, d.EogR1, d.EogL2, d.EogR2, d.EogH1, d.EogH2, d.EogV1, d.EogV2]);
                break;
        }
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
        var inReplaySession = phase == Phase.Replaying;
        // 計測中・再生中は端末パラメータを触らせない(Mac 版 isInputDisabled)。
        var inputDisabled = measuring || inReplaySession;

        bt_Scan.Text = isScanning ? "Stop Scan" : "Scan";
        bt_Scan.Enabled = !connected && !inReplaySession;
        cb_DeviceList.Enabled = !connected && !isScanning && !inReplaySession;

        // 再生中の Connect は「再生セッションを終える」ボタンとして働く(Mac 版と同じ)。
        // Shelf 移行中だけは、結果が出るまで押させない。
        bt_Connect.Enabled = !isEnteringShelf &&
            (connected || inReplaySession || cb_DeviceList.SelectedItem is MEMEDevice);
        bt_Connect.Text = connected || inReplaySession ? "Disconnect" : "Connect";

        // BLE 接続中は CSV 再生に入れない。
        bt_FileReplay.Enabled = !connected;

        bt_Measurement.Visible = !inReplaySession;
        bt_Measurement.Enabled = connected && !isEnteringShelf;
        bt_Measurement.Text = measuring ? "Stop Measurement" : "Start Measurement";
        bt_FreeMarking.Enabled = measuring;

        // 再生中は、ページで付けた Artifact を今すぐ CSV へ書き戻すボタンを出す(Disconnect でも書き戻す)。
        // 再生・一時停止・速度・位置の操作はグラフ画面(ページ)の中にある。
        bt_SaveArtifacts.Visible = inReplaySession;

        settingToolStripMenuItem.Enabled = !measuring;
        cb_SelectMode.Enabled = !inputDisabled;
        cb_TransSpeed.Enabled = !inputDisabled;
        cb_AccelRange.Enabled = !inputDisabled;
        cb_GyroRange.Enabled = !inputDisabled;

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
        // 長押しが成立した押下のクリックは捨てる。移行中も触らせない。
        if (suppressConnectClick)
        {
            suppressConnectClick = false;
            return;
        }

        if (isEnteringShelf)
        {
            return;
        }

        if (phase == Phase.Replaying)
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

    #region Shelf mode

    /// <summary>
    /// Shelf mode へ移行できる状態か。SHELF コマンドは接続済みで計測していないときだけ
    /// 受理されるので、計測中(Phase.Measuring)と再生中は対象外。
    /// </summary>
    private bool CanEnterShelfMode => phase == Phase.Connected && !isEnteringShelf;

    private void bt_Connect_MouseDown(object? sender, MouseEventArgs e)
    {
        // 押し直しのたびに倒す。ダイアログの外でマウスを離してクリックが来なかった場合に、
        // 次の 1 クリックを取りこぼさないため。
        suppressConnectClick = false;
        if (e.Button == MouseButtons.Left && CanEnterShelfMode)
        {
            shelfLongPressTimer.Start();
        }
    }

    /// <summary>Disconnect が 5 秒押されたまま。確認ダイアログを出す。</summary>
    private void OnShelfLongPress()
    {
        shelfLongPressTimer.Stop();
        if (!CanEnterShelfMode)
        {
            return;
        }

        // ダイアログを出している間にマウスを離すとボタンの Click が走るので、先に倒しておく。
        suppressConnectClick = true;

        using var dialog = new ShelfModeForm();
        if (dialog.ShowDialog(this) != DialogResult.Yes || !CanEnterShelfMode)
        {
            return;
        }

        EnterShelfMode();
    }

    /// <summary>
    /// 端末を Shelf mode(保管モード)へ移行させる。CONFIG モードへの遷移が受理されてから
    /// SHELF が送られ、受理されると端末は自ら切断する。復帰は充電のみで、アプリからは戻せない。
    /// </summary>
    private void EnterShelfMode()
    {
        isEnteringShelf = true;
        lb_ConnectionState.Text = "State : Entering shelf mode...";
        UpdateUiState();

        memeLib.enterShelfMode(sent => RunOnUi(() =>
        {
            if (!sent)
            {
                // SHELF はまだ送っていないので端末は通常モードのまま。
                FinishShelfMode(entered: false);
                return;
            }

            // SHELF は送信済み。端末側からの切断が来れば成功。
            shelfDisconnectTimer.Start();
        }));
    }

    /// <summary>
    /// Shelf 移行の結果を確定して知らせる
    /// (成功＝端末が切断した、失敗＝ACK 無し／切断待ちタイムアウト)。
    /// </summary>
    private void FinishShelfMode(bool entered)
    {
        if (!isEnteringShelf)
        {
            return;
        }

        shelfDisconnectTimer.Stop();
        isEnteringShelf = false;
        if (entered)
        {
            // 移行した端末はもうペアリングに応じないので、スキャン結果に残っていると
            // 選んで Connect できてしまう。通常の切断と違い、一覧ごと捨てて Scan からやり直させる。
            cb_DeviceList.Items.Clear();
            phase = Phase.Idle;
            lb_ConnectionState.Text = "State : Shelf mode";
        }
        else if (phase is Phase.Connected or Phase.Measuring)
        {
            lb_ConnectionState.Text = "State : Connected";
        }

        UpdateUiState();

        MessageBox.Show(
            this,
            entered
                ? "To exit shelf mode, please recharge the device."
                : "The device is still in normal mode.",
            entered ? "Entered shelf mode" : "Failed to enter shelf mode",
            MessageBoxButtons.OK,
            entered ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    #endregion

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

        stats.Reset();
        stats.StartMeasurement((int)quality);
        isFreeMarking = false;
        pendingArtifacts.Clear();

        var header = DataPersistenceService.BuildHeader(mode, quality, accelRange, gyroRange);
        persistence.Begin(
            setting.EnsureSaveDirectory(), CurrentDeviceAddress(), header, quality, setting.CompressSaveFile);
        tcpServer.SetHeader(header);

        liveSampleIndex = 0;
        web.Start(LiveCondition());

        memeLib.startDataReport();
        phase = Phase.Measuring;
        UpdateUiState();
    }

    private void StopMeasurement()
    {
        memeLib.stopDataReport();
        stats.StopMeasurement();
        web.Stop();
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
            // 保存済みファイルの移動なので、選べる形式は書き出した形式に揃える。
            Filter = CsvFile.IsGzip(source)
                ? "gzip CSV (*.csv.gz)|*.csv.gz"
                : "CSV (*.csv)|*.csv",
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
            MessageBox.Show(this, $"保存できませんでした。\n{FileErrorText.Of(ex)}", "Save",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private string CurrentDeviceAddress() =>
        (cb_DeviceList.SelectedItem as MEMEDevice)?.Address ?? "UNKNOWN";

    private void bt_FreeMarking_Click(object sender, EventArgs e) => isFreeMarking = true;

    private void settingToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // 計測中・再生中は Display Engine の zip を切り替えさせない(Mac・Android と同じ)
        var before = (webContent.Source, webContent.Manifest);
        using var form = new SettingsForm(setting, webContent, canSwitchEngine: phase is not (Phase.Measuring or Phase.Replaying));
        var ok = form.ShowDialog(this) == DialogResult.OK;
        if (before != (webContent.Source, webContent.Manifest))
        {
            // 中身が切り替わったら読み込み直す(zip の取り込みは設定画面の中で済んでいる)
            web.Load();
        }

        if (ok)
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
        shelfLongPressTimer.Stop();
        shelfDisconnectTimer.Stop();
        if (phase == Phase.Measuring)
        {
            memeLib.stopDataReport();
        }

        web.Dispose();
        persistence.Dispose();
        tcpServer.Dispose();
        stats.Dispose();
        memeLib.Dispose();
    }

    #endregion

    #region Graph (WebView)

    /// <summary>表示の設定(時刻の表示・加速度のオフセット・テーマ)。計測・再生どちらでもページへ渡す。</summary>
    private Dictionary<string, object?> DisplayOptions() => new()
    {
        ["timeZone"] = setting.ConvertToLocalTime ? "local" : "utc",
        ["accOffset"] = new[] { setting.AccOffsetX, setting.AccOffsetY, setting.AccOffsetZ },
        // この画面はライトだけ(WinForms を OS のダークモードに合わせていない)
        ["theme"] = "light",
    };

    /// <summary>計測の開始時にページへ渡す条件(webview/BRIDGE.md の start)。値は端末に設定したもの。</summary>
    private Dictionary<string, object?> LiveCondition()
    {
        var cond = DisplayOptions();
        cond["label"] = cb_DeviceList.SelectedItem is MEMEDevice d && d.Name.Length > 0 ? d.Name : "JINS MEME";
        cond["mode"] = mode switch
        {
            MEMEMode.Full => "full",
            MEMEMode.Quaternion => "quaternion",
            _ => "standard",
        };
        cond["cps"] = quality == MEMEQuality.High ? 100 : 50;
        cond["accRange"] = AccelG[Math.Clamp((int)accelRange, 0, 3)];
        cond["gyroRange"] = GyroDps[Math.Clamp((int)gyroRange, 0, 3)];
        cond["columns"] = mode switch
        {
            MEMEMode.Full => FullColumns,
            MEMEMode.Standard => StandardColumns,
            _ => Array.Empty<string>(),
        };
        cond["startedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return cond;
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

        using var dialog = new OpenFileDialog
        {
            Filter = CsvFile.OpenFilter,
            InitialDirectory = Directory.Exists(setting.SaveFilePath) ? setting.SaveFilePath : string.Empty,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadReplayFile(dialog.FileName);
        }
    }

    /// <summary>
    /// 再生はグラフ画面(ページ)が受け持つ: ファイルを仮想ホストに出してページに読ませる。
    /// 読み込み・再生・一時停止・速度・シークはページ側。形式が違えばページがその旨を表示する。
    /// </summary>
    private void LoadReplayFile(string path)
    {
        if (phase is Phase.Connected or Phase.Measuring)
        {
            MessageBox.Show(this, "Disconnect the BLE device before opening a CSV for replay.", "File Replay",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (phase == Phase.Replaying)
        {
            EndReplaySession();
        }

        replayFile = path;
        pendingArtifacts.Clear();
        lb_ConnectionState.Text = $"State : {Path.GetFileName(path)}";
        phase = Phase.Replaying;
        web.OpenReplay(path, DisplayOptions());
        UpdateUiState();
    }

    /// <summary>ページが CSV を読み終えたら、計測条件の表示(左の欄)を CSV に合わせる。</summary>
    private void ApplyReplayInfo(JsonElement info)
    {
        if (phase != Phase.Replaying)
        {
            return;
        }

        var modeName = info.TryGetProperty("mode", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
        var modeIndex = modeName switch
        {
            "standard" => Array.IndexOf(SelectableModes, MEMEMode.Standard),
            "full" => Array.IndexOf(SelectableModes, MEMEMode.Full),
            "quaternion" => Array.IndexOf(SelectableModes, MEMEMode.Quaternion),
            _ => -1,
        };
        if (modeIndex >= 0)
        {
            cb_SelectMode.SelectedIndex = modeIndex;
        }

        if (info.TryGetProperty("cps", out var c) && c.TryGetInt32(out var cps))
        {
            cb_TransSpeed.SelectedIndex = cps == 100 ? 0 : 1;
        }

        if (info.TryGetProperty("accRange", out var a) && a.TryGetInt32(out var g) && Array.IndexOf(AccelG, g) is var k and >= 0)
        {
            cb_AccelRange.SelectedIndex = k;
        }

        if (info.TryGetProperty("gyroRange", out var y) && y.TryGetInt32(out var dps) && Array.IndexOf(GyroDps, dps) is var j and >= 0)
        {
            cb_GyroRange.SelectedIndex = j;
        }
    }

    /// <summary>Save Artifacts: 再生中に付けた Artifact を、今すぐ再生元 CSV へ書き戻す(再生は続ける)。</summary>
    private void bt_SaveArtifacts_Click(object sender, EventArgs e)
    {
        if (phase == Phase.Replaying)
        {
            FlushReplayArtifacts();
        }
    }

    /// <summary>再生セッションを終える(Disconnect)。付けた Artifact は書き戻す。</summary>
    private void EndReplaySession()
    {
        FlushReplayArtifacts();
        web.CloseReplay();
        replayFile = null;
        phase = Phase.Idle;
        lb_ConnectionState.Text = "State : Disconnected";
        ResetStatsDisplay();
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

    #endregion

    #region Artifact

    /// <summary>
    /// ページで付けた Artifact を控える(UI スレッド)。空なら "X"、カンマ/改行は列崩れ防止のため空白に(同じ行は上書き)。
    /// 表計算ソフトで数式として読まれる書き出し(= + - @)は受けない(CSV 注入。ページも入力時に断る。webview/BRIDGE.md)。
    /// </summary>
    private void ReceiveArtifact(int i, string text)
    {
        if (phase is not (Phase.Replaying or Phase.Measuring))
        {
            return;
        }

        var sanitized = text.Replace(',', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim(' ');
        if (sanitized.Length > 64)
        {
            sanitized = sanitized[..64];
        }

        if (sanitized.Length > 0 && "=+-@".Contains(sanitized[0]))
        {
            System.Diagnostics.Debug.WriteLine($"[Artifact] refused (formula-like): {sanitized}");
            return;
        }

        pendingArtifacts[Math.Max(i, 0)] = sanitized.Length == 0 ? "X" : sanitized;
    }

    /// <summary>
    /// 再生中に付けた Artifact を再生元 CSV の ARTIFACT 列へ書き戻す(Save Artifacts / Disconnect)。
    /// キーは CSV のデータ行の番号(ページが返す番号そのまま)。
    /// </summary>
    private void FlushReplayArtifacts()
    {
        if (pendingArtifacts.Count == 0 || replayFile is null)
        {
            return;
        }

        try
        {
            CsvArtifactWriter.Apply(replayFile, pendingArtifacts);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, $"Artifact を書き戻せませんでした。\n{FileErrorText.Of(e)}", "Artifact",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        pendingArtifacts.Clear();
    }

    /// <summary>
    /// 計測中に付けた Artifact を、保存した CSV の ARTIFACT 列へ書き戻す(停止時)。
    /// pendingArtifacts のキーはサンプル番号。CSV は先頭パケットを 1 件落とすため、
    /// データ行 = サンプル番号 − 1(サンプル 0 は CSV に無いので除く)。
    /// </summary>
    private void FlushLiveArtifacts()
    {
        if (pendingArtifacts.Count == 0)
        {
            return;
        }

        var path = persistence.CurrentFilePath;
        var rowKeyed = pendingArtifacts.Where(kv => kv.Key >= 1).ToDictionary(kv => kv.Key - 1, kv => kv.Value);
        pendingArtifacts.Clear();
        if (path is null || !File.Exists(path))
        {
            return;
        }

        try
        {
            CsvArtifactWriter.Apply(path, rowKeyed);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, $"Artifact を書き戻せませんでした。\n{FileErrorText.Of(e)}", "Artifact",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    #endregion
}
