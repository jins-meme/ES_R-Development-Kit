using MEMELib_Academic;

namespace MEME_Academic_Sample;

public partial class MEME_Academic_Sample : Form
{
    /// <summary>結果 CSV の出力先(実行ファイルからの相対)。</summary>
    private const string ResultDirectory = "Result";

    /// <summary>
    /// 画面更新の間隔。センサーは 100Hz で届くが、その都度ラベルを書き換えると
    /// UI スレッドが飽和するため、最新値だけを一定間隔で反映する。
    /// CSV には受信したサンプルをすべて書き出す。
    /// </summary>
    private const int UiRefreshIntervalMs = 50;

    private static readonly Color AccentColor = Color.FromArgb(224, 0, 32);

    private readonly MEMELib memeLib;
    private readonly System.Windows.Forms.Timer uiTimer;

    /// <summary>MEME 接続状態</summary>
    private bool isConnectedPeripheral;

    /// <summary>計測状態</summary>
    private bool isStartMeasurement;

    /// <summary>フリーマーキング</summary>
    private bool isFreeMarking;

    /// <summary>Save ファイル名</summary>
    private string saveFileName = string.Empty;

    /// <summary>アカデミックモード(Full)</summary>
    private MEMEMode mode = MEMEMode.Full;

    /// <summary>計測品質(High)</summary>
    private MEMEQuality quality = MEMEQuality.High;

    /// <summary>3軸加速度センサー計測レンジ</summary>
    private MEMEAccelRange accelRange = MEMEAccelRange.Range2G;

    /// <summary>3軸ジャイロセンサー計測レンジ</summary>
    private MEMEGyroRange gyroRange = MEMEGyroRange.Range250dps;

    /// <summary>画面へ反映する直近のサンプル。BLE スレッドから差し替えられる。</summary>
    private AcademicFullData? latestData;

    public MEME_Academic_Sample()
    {
        InitializeComponent();
        Icon = AppInfo.LoadIcon();
        SetGUIParam();

        memeLib = new MEMELib();
        memeLib.memePeripheralFound += memePeripheralFound;
        memeLib.memePeripheralConnected += memePeripheralConnected;
        memeLib.memePeripheralDisconnected += memePeripheralDisconnected;
        memeLib.memeAcademicFullDataReceived += memeAcademicFullDataReceived;

        SDKVersionLabel.Text = memeLib.getSDKVersion();

        uiTimer = new System.Windows.Forms.Timer { Interval = UiRefreshIntervalMs };
        uiTimer.Tick += (_, _) => RefreshSensorLabels();
        uiTimer.Start();
    }

    #region MEMELib delegate

    private void memePeripheralFound(object sender, MEMEStatus result, MEMEDevice? device)
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

                SetButton(bt_ConnectPeripheral, "Connect", enabled: true);
            });
        }
        else if (result == MEMEStatus.MEMELIB_TIMEOUT)
        {
            RunOnUi(() =>
            {
                SetButton(bt_ScanPeripheral, "Scan MEME", enabled: true);
                if (cb_DeviceList.Items.Count == 0)
                {
                    lb_ConnectionStatus.Text = "Status : Not found";
                }
            });
        }
    }

    private void memePeripheralConnected(object sender, MEMEStatus result)
    {
        if (result == MEMEStatus.MEMELIB_OK)
        {
            isConnectedPeripheral = true;
            mode = memeLib.getMode();
            quality = memeLib.getQuality();
            accelRange = memeLib.getAccelRange();
            gyroRange = memeLib.getGyroRange();

            RunOnUi(() =>
            {
                lb_ConnectionStatus.Text = "Status : Connected";
                deviceVersionLabel.Text = memeLib.getFWVersion();
                SetButton(bt_ConnectPeripheral, "Disconnect", enabled: true);
                SetButton(bt_StartMeasurement, "Start Measurement", enabled: true);
                SetButton(bt_ScanPeripheral, "Scan MEME", enabled: false);
            });
        }
        else
        {
            isConnectedPeripheral = false;
            var reason = result == MEMEStatus.MEMELIB_TIMEOUT ? "timeout" : "failed";
            RunOnUi(() =>
            {
                lb_ConnectionStatus.Text = $"Status : Connect {reason}";
                SetButton(bt_ConnectPeripheral, "Connect", enabled: true);
                SetButton(bt_StartMeasurement, "Start Measurement", enabled: false);
                SetButton(bt_ScanPeripheral, "Scan MEME", enabled: true);
            });
        }
    }

    private void memePeripheralDisconnected(object sender, MEMEStatus result)
    {
        isConnectedPeripheral = false;
        isStartMeasurement = false;

        var suffix = result == MEMEStatus.MEMELIB_OK ? string.Empty : " (link lost)";
        RunOnUi(() =>
        {
            lb_ConnectionStatus.Text = $"Status : Disconnected{suffix}";
            SetButton(bt_ConnectPeripheral, "Connect", enabled: true);
            SetButton(bt_StartMeasurement, "Start Measurement", enabled: false);
            SetButton(bt_ScanPeripheral, "Scan MEME", enabled: true);
        });
    }

    private void memeAcademicFullDataReceived(object sender, AcademicFullData fullData)
    {
        latestData = fullData;
        saveCsvData(fullData);
    }

    #endregion

    #region GUI

    private void SetGUIParam()
    {
        cb_ModeSelect.Items.Add("Full");
        cb_ModeSelect.SelectedIndex = 0;
        cb_TransmissionSpeed.Items.Add("High");
        cb_TransmissionSpeed.SelectedIndex = 0;
        cb_AccRange.Items.AddRange(["±2G", "±4G", "±8G", "±16G"]);
        cb_AccRange.SelectedIndex = 0;
        cb_GyroRange.Items.AddRange(["±250dps", "±500dps", "±1000dps", "±2000dps"]);
        cb_GyroRange.SelectedIndex = 0;

        SetButton(bt_ScanPeripheral, "Scan MEME", enabled: true);
        SetButton(bt_ConnectPeripheral, "Connect", enabled: false);
        SetButton(bt_StartMeasurement, "Start Measurement", enabled: false);
    }

    /// <summary>BLE のコールバックは別スレッドで届くため、UI 操作をここへ集約する。</summary>
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

    private static void SetButton(Button button, string text, bool enabled)
    {
        button.Text = text;
        button.Enabled = enabled;
        button.BackColor = enabled ? AccentColor : Color.LightGray;
        button.ForeColor = enabled ? Color.White : Color.Black;
    }

    private void RefreshSensorLabels()
    {
        var data = latestData;
        if (data is null)
        {
            return;
        }

        lb_DataCnt.Text = data.Cnt.ToString("D");
        lb_DataBattLv.Text = data.BattLv.ToString("D");
        lb_DataAccX.Text = data.AccX.ToString("D");
        lb_DataAccY.Text = data.AccY.ToString("D");
        lb_DataAccZ.Text = data.AccZ.ToString("D");
        lb_DataGyroX.Text = data.GyroX.ToString("D");
        lb_DataGyroY.Text = data.GyroY.ToString("D");
        lb_DataGyroZ.Text = data.GyroZ.ToString("D");
        lb_DataEogL.Text = data.EogL.ToString("D");
        lb_DataEogR.Text = data.EogR.ToString("D");
        lb_DataEogH.Text = data.EogH.ToString("D");
        lb_DataEogV.Text = data.EogV.ToString("D");
    }

    #endregion

    #region Button Event

    private void bt_ScanPeripheral_Click(object sender, EventArgs e)
    {
        cb_DeviceList.Items.Clear();
        SetButton(bt_ConnectPeripheral, "Connect", enabled: false);
        lb_ConnectionStatus.Text = "Status : Scanning...";

        if (memeLib.startScanningPeripherals() == MEMEStatus.MEMELIB_OK)
        {
            SetButton(bt_ScanPeripheral, "Scanning...", enabled: false);
        }
        else
        {
            lb_ConnectionStatus.Text = "Status : Bluetooth unavailable";
        }
    }

    private void bt_ConnectPeripheral_Click(object sender, EventArgs e)
    {
        if (isConnectedPeripheral)
        {
            memeLib.disconnectPeripheral();
            return;
        }

        if (cb_DeviceList.SelectedItem is not MEMEDevice device)
        {
            return;
        }

        lb_ConnectionStatus.Text = "Status : Connecting...";
        SetButton(bt_ConnectPeripheral, "Connecting...", enabled: false);
        memeLib.connectPeripheral(device);
    }

    private void bt_StartMeasurement_Click(object sender, EventArgs e)
    {
        if (isStartMeasurement)
        {
            memeLib.stopDataReport();
            isStartMeasurement = false;
            SetButton(bt_StartMeasurement, "Start Measurement", enabled: true);
            return;
        }

        // Academic Full(0x99) を確実に受け取るため、計測前にモードも送っておく。
        mode = MEMEMode.Full;
        quality = MEMEQuality.High;
        memeLib.setMode(mode, quality);

        accelRange = (MEMEAccelRange)cb_AccRange.SelectedIndex;
        memeLib.setAccelRange(accelRange);
        gyroRange = (MEMEGyroRange)cb_GyroRange.SelectedIndex;
        memeLib.setGyroRange(gyroRange);

        isFreeMarking = false;
        saveCsvHeader();
        memeLib.startDataReport();
        isStartMeasurement = true;
        SetButton(bt_StartMeasurement, "Stop Measurement", enabled: true);
    }

    private void freeMarkingButton_Click(object sender, EventArgs e)
    {
        isFreeMarking = true;
    }

    /// <summary>アプリを終了する。後片付けは FormClosing がまとめて行う。</summary>
    private void quitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

    private void versionToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var form = new VersionForm();
        form.ShowDialog(this);
    }

    private void MEME_Academic_Sample_FormClosing(object sender, FormClosingEventArgs e)
    {
        uiTimer.Stop();
        if (isStartMeasurement)
        {
            memeLib.stopDataReport();
        }

        memeLib.Dispose();
    }

    #endregion

    #region Save Data

    private void saveCsvHeader()
    {
        var device = cb_DeviceList.SelectedItem as MEMEDevice;
        var btAddr = device?.Address ?? "UNKNOWN";
        var saveFileTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        saveFileName = $"{btAddr}_{saveFileTime}.csv";

        memeLib.saveData(ResultDirectory, saveFileName,
            mode == MEMEMode.Full ? "// Data mode  : Full" : $"// Data mode  : {mode}");
        memeLib.saveData(ResultDirectory, saveFileName,
            quality == MEMEQuality.High ? "// Transmission speed  : 100Hz" : "// Transmission speed  : 50Hz");
        memeLib.saveData(ResultDirectory, saveFileName,
            $"// Accelerometer sensor's range  : {AccelRangeText(accelRange)}");
        memeLib.saveData(ResultDirectory, saveFileName,
            $"// Gyroscope sensor's range  : {GyroRangeText(gyroRange)}");
        memeLib.saveData(ResultDirectory, saveFileName, "//");
        memeLib.saveData(ResultDirectory, saveFileName,
            "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V,BattLv");
    }

    private static string AccelRangeText(MEMEAccelRange range) => range switch
    {
        MEMEAccelRange.Range2G => "2G",
        MEMEAccelRange.Range4G => "4G",
        MEMEAccelRange.Range8G => "8G",
        _ => "16G",
    };

    private static string GyroRangeText(MEMEGyroRange range) => range switch
    {
        MEMEGyroRange.Range250dps => "250dps",
        MEMEGyroRange.Range500dps => "500dps",
        MEMEGyroRange.Range1000dps => "1000dps",
        _ => "2000dps",
    };

    private void saveCsvData(AcademicFullData fullData)
    {
        if (saveFileName.Length == 0)
        {
            return;
        }

        // 記録時刻は UTC。Mac / Android 版および変換スクリプトと揃えている。
        var sTime = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.fff");

        string? freeMarking = null;
        if (isFreeMarking)
        {
            freeMarking = "X";
            isFreeMarking = false;
        }

        var writeData = string.Join(',',
            freeMarking,
            fullData.Cnt.ToString("D"),
            sTime,
            fullData.AccX.ToString("D"),
            fullData.AccY.ToString("D"),
            fullData.AccZ.ToString("D"),
            fullData.GyroX.ToString("D"),
            fullData.GyroY.ToString("D"),
            fullData.GyroZ.ToString("D"),
            fullData.EogL.ToString("D"),
            fullData.EogR.ToString("D"),
            fullData.EogH.ToString("D"),
            fullData.EogV.ToString("D"),
            fullData.BattLv.ToString("D")) + ",";

        memeLib.saveData(ResultDirectory, saveFileName, writeData);
    }

    #endregion
}
