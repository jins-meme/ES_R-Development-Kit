using System.Text.Json;
using MEMELib_Academic;
using MEME_Academic_Sample.Models;
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

    /// <summary>判定器の通知と演算結果の表(ページ → アプリの notify / table / records)。ライブ計測の間だけ受ける</summary>
    private readonly DetectorOutputs outputs;

    /// <summary>
    /// 計測を止めてから判定器の表を閉じるまで待つ時間。ページは stop を受けてから溜めた行(最大 1 秒ぶん)を送るので、
    /// それが届くのを待つ(webview/BRIDGE.md の Detector notifications and tables。Android・Mac と同じ 1 秒)
    /// </summary>
    private const int OutputsStopGraceMs = 1000;

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

    /// <summary>ページで付けた未書き戻しの Artifact。計測停止時・再生の Save Artifacts / Disconnect で CSV へ書き戻す。</summary>
    private readonly ArtifactBuffer artifacts = new();

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

    private MeasurementMode mode = MeasurementMode.Full;
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
        outputs = new DetectorOutputs(this);
        web.Output += outputs.Receive;
        DetectorNotifications.Attach(this);
        Activated += (_, _) => outputs.Foreground = true;
        Deactivate += (_, _) => outputs.Foreground = false;

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

    /// <summary>左の欄の選択肢。番号はそれぞれ MeasurementMode.All / MeasurementRange の表の並び。</summary>
    private void SetupOptions()
    {
        cb_SelectMode.Items.AddRange([.. MeasurementMode.All.Select(m => m.Label)]);
        cb_SelectMode.SelectedIndexChanged += (_, _) =>
            mode = MeasurementMode.All[cb_SelectMode.SelectedIndex];

        cb_TransSpeed.Items.AddRange([.. MeasurementRange.Qualities.Select(MeasurementRange.HzLabel)]);
        cb_TransSpeed.SelectedIndexChanged += (_, _) =>
            quality = MeasurementRange.Qualities[cb_TransSpeed.SelectedIndex];

        cb_AccelRange.Items.AddRange([.. MeasurementRange.AccelG.Select(MeasurementRange.AccelLabel)]);
        cb_AccelRange.SelectedIndexChanged += (_, _) =>
            accelRange = (MEMEAccelRange)cb_AccelRange.SelectedIndex;

        cb_GyroRange.Items.AddRange([.. MeasurementRange.GyroDps.Select(MeasurementRange.GyroLabel)]);
        cb_GyroRange.SelectedIndexChanged += (_, _) =>
            gyroRange = (MEMEGyroRange)cb_GyroRange.SelectedIndex;

        ShowConditions();
    }

    /// <summary>
    /// mode / quality / accelRange / gyroRange を左の欄に出す。表に無い値(端末が返した見慣れない値など)は
    /// 選べる範囲に丸めてから出す(選択の変更で値は同じものに書き戻る)。
    /// </summary>
    private void ShowConditions()
    {
        quality = quality == MEMEQuality.Low ? MEMEQuality.Low : MEMEQuality.High;
        accelRange = (MEMEAccelRange)Math.Clamp((int)accelRange, 0, cb_AccelRange.Items.Count - 1);
        gyroRange = (MEMEGyroRange)Math.Clamp((int)gyroRange, 0, cb_GyroRange.Items.Count - 1);

        cb_SelectMode.SelectedIndex = MeasurementMode.All.ToList().IndexOf(mode);
        cb_TransSpeed.SelectedIndex = MeasurementRange.Qualities.ToList().IndexOf(quality);
        cb_AccelRange.SelectedIndex = (int)accelRange;
        cb_GyroRange.SelectedIndex = (int)gyroRange;
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
                // Scan を止めた・Connect を押した後に遅れて届いた端末は足さない(接続中に一覧と状態を動かさない)。
                if (!isScanning)
                {
                    return;
                }

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
            RunOnUi(() =>
            {
                // 端末が保持している設定を画面へ反映する(表に無い値なら今の選択のまま)。
                if (MeasurementMode.All.FirstOrDefault(m => m.Device == memeLib.getMode()) is { } deviceMode)
                {
                    mode = deviceMode;
                }

                quality = memeLib.getQuality();
                accelRange = memeLib.getAccelRange();
                gyroRange = memeLib.getGyroRange();
                ShowConditions();

                phase = Phase.Connected;
                lb_ConnectionState.Text = "State : Connected";
                lb_MemeVersion.Text = $"MEME Version：{memeLib.getFWVersion()}";
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
        RunOnUi(outputs.NoteDataFile);

        RunOnUi(() =>
        {
            // Artifact は UI スレッドで持つので、書き戻しもここで(CSV は上で閉じ済み)
            FlushLiveArtifacts();
            if (wasMeasuring)
            {
                web.Stop();
                CloseOutputsLater();
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
        var recordedUtc = DateTime.UtcNow;
        var measuring = phase == Phase.Measuring;

        // グラフへは先頭パケットも渡す(サンプル番号は計測開始からの全パケットの通し番号。BRIDGE.md の push)。
        // 値は start の columns の並び。モードと違う型のサンプルは渡さない
        var i = -1;
        if (measuring)
        {
            i = liveSampleIndex++;
            if (mode.HasGraph && mode.Values(data) is { } values)
            {
                web.Push(i, values);
            }
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
        persistence.Append(data, stats.TotalCount, recordedUtc, freeMarking);
        outputs.RecordSample(i, stats.TotalCount, recordedUtc);   // 判定器の表の NUM / DATE 列(i はこのサンプルをページへ渡した番号)

        // CSV に X を書いた行と同じサンプル位置へ印を出す(Quaternion はグラフが無いので出さない)
        if (freeMarking && mode.HasGraph)
        {
            RunOnUi(() => web.Mark(i, "X"));
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
        // Scan 中も選べる(2 台以上見つかったら、探し終わるのを待たずに選んで Connect できる)。
        cb_DeviceList.Enabled = !connected && !inReplaySession;

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

        // Scan 中に押された場合は、connectPeripheral が Scan も止める。
        isScanning = false;
        bt_Scan.Text = "Scan";
        cb_DeviceList.Enabled = false;
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
        memeLib.setMode(mode.Device, quality);
        memeLib.setAccelRange(accelRange);
        memeLib.setGyroRange(gyroRange);

        stats.Reset();
        stats.StartMeasurement((int)quality);
        isFreeMarking = false;
        artifacts.Clear();

        // 前の計測の表がまだ開いていれば、データ CSV を忘れる前(Begin の前)にここで閉じる
        outputs.Start(MeasurementRange.Hz(quality), () => persistence.CurrentFilePath, () => web.PageName);
        var header = DataPersistenceService.BuildHeader(mode, quality, accelRange, gyroRange);
        persistence.Begin(
            setting.EnsureSaveDirectory(), CurrentDeviceAddress(), header, mode, quality, setting.CompressSaveFile);
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
        outputs.NoteDataFile();
        CloseOutputsLater();
        FlushLiveArtifacts();
        phase = Phase.Connected;
        UpdateUiState();
        OfferSaveFileDialog();
    }

    /// <summary>判定器の表は、ページが stop の後に送る残りを待ってから閉じる(閉じる前に次の計測が始まっていたら何もしない)</summary>
    private void CloseOutputsLater()
    {
        var s = outputs.Session;
        _ = Task.Delay(OutputsStopGraceMs).ContinueWith(_ => RunOnUi(() => outputs.Stop(s)), TaskScheduler.Default);
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
                outputs.DataFileMoved(source, dialog.FileName);   // 判定器の表の CSV も同じフォルダ・同じベース名へ
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

        outputs.Stop();
        DetectorNotifications.Shutdown();
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
        cond["mode"] = mode.PageName;
        cond["cps"] = MeasurementRange.Hz(quality);
        cond["accRange"] = MeasurementRange.G(accelRange);
        cond["gyroRange"] = MeasurementRange.Dps(gyroRange);
        cond["columns"] = mode.HasGraph ? mode.Columns : Array.Empty<string>();
        cond["startedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        cond["features"] = DetectorOutputs.Features;
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
        artifacts.Clear();
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

        if (MeasurementMode.FromPageName(
                info.TryGetProperty("mode", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null) is { } replayMode)
        {
            mode = replayMode;
        }

        if (info.TryGetProperty("cps", out var c) && c.TryGetInt32(out var cps))
        {
            quality = MeasurementRange.QualityOf(cps);
        }

        if (info.TryGetProperty("accRange", out var a) && a.TryGetInt32(out var g) && MeasurementRange.AccelRangeOf(g) is { } acc)
        {
            accelRange = acc;
        }

        if (info.TryGetProperty("gyroRange", out var y) && y.TryGetInt32(out var dps) && MeasurementRange.GyroRangeOf(dps) is { } gyro)
        {
            gyroRange = gyro;
        }

        ShowConditions();
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

    /// <summary>ページで付けた Artifact を控える(UI スレッド)。計測中・再生中だけ。</summary>
    private void ReceiveArtifact(int i, string text)
    {
        if (phase is not (Phase.Replaying or Phase.Measuring))
        {
            return;
        }

        if (!artifacts.Add(i, text))
        {
            System.Diagnostics.Debug.WriteLine($"[Artifact] refused (formula-like): {text}");
        }
    }

    /// <summary>Artifact を書き戻せなかったことを知らせる(自己テストの間は結果に残し、ダイアログで止めない)</summary>
    private void ReportArtifactWriteError(Exception e)
    {
#if DEBUG
        if (autoTestErrors is not null)
        {
            autoTestErrors.Add($"artifact write: {FileErrorText.Of(e)}");
            return;
        }
#endif
        MessageBox.Show(this, $"Artifact を書き戻せませんでした。\n{FileErrorText.Of(e)}", "Artifact",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>再生中に付けた Artifact を再生元 CSV の ARTIFACT 列へ書き戻す(Save Artifacts / Disconnect)。</summary>
    private void FlushReplayArtifacts()
    {
        var rows = artifacts.TakeReplayRows();
        if (replayFile is not null)
        {
            WriteArtifacts(replayFile, rows);
        }
    }

    /// <summary>計測中に付けた Artifact を、保存した CSV の ARTIFACT 列へ書き戻す(停止時・切断時)。</summary>
    private void FlushLiveArtifacts()
    {
        var rows = artifacts.TakeLiveRows();
        if (persistence.CurrentFilePath is { } path && File.Exists(path))
        {
            WriteArtifacts(path, rows);
        }
    }

    private void WriteArtifacts(string path, Dictionary<int, string> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        try
        {
            CsvArtifactWriter.Apply(path, rows);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            ReportArtifactWriteError(e);
        }
    }

    #endregion
}
