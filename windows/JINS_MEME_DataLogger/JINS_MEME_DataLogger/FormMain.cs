using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// メイン画面の処理を記述します。
    /// </summary>
    public partial class mainForm : BaseForm
    {
        /// <summary>
        /// データ管理クラス
        /// </summary>
        private DataManager dataManager = new DataManager();

        /// <summary>
        /// ファイルアクセスクラス
        /// </summary>
        private FileAccess file = new FileAccess();

        /// <summary>
        /// 測定データスレッド
        /// </summary>
        private Thread measureDataThread;

        /// <summary>
        /// 測定データスレッド継続状態
        /// </summary>
        private bool measureDataThreadLoop;

        /// <summary>
        /// 画面状態定義
        /// </summary>
        private enum DISPLAY_STATUS
        {
            /// <summary>
            /// 起動
            /// </summary>
            STARTUP,
            /// <summary>
            /// ComPortが見つからない
            /// </summary>
            LOST_COM,
            /// <summary>
            /// ComPortが見つかった
            /// </summary>
            FOUND_COM,
            /// <summary>
            /// ComPort切断
            /// </summary>
            DISCONNECT_COM,
            /// <summary>
            /// ドングル接続中
            /// </summary>
            INCONNECT_DONGLE,
            /// <summary>
            /// ドングル接続
            /// </summary>
            CONNECT_DONGLE,
            /// <summary>
            /// Bluetooth検索
            /// </summary>
            SCAN_BLUETOOTH,
            /// <summary>
            /// Bluetoothデバイスが見つからない
            /// </summary>
            LOST_BLUETOOTH,
            /// <summary>
            /// Bluetoothデバイスが見つかった
            /// </summary>
            FOUND_BLUETOOTH,
            /// <summary>
            /// Bluetooth接続中
            /// </summary>
            INCONNECT_BLUETOOTH,
            /// <summary>
            /// Bluetooth切断中
            /// </summary>
            DISCONNECT_BLUETOOTH,
            /// <summary>
            /// Bluetooth接続
            /// </summary>
            CONNECT_BLUETOOTH,
            /// <summary>
            /// デバイスパラメータ初期化
            /// </summary>
            CLEAR_PARAMS,
            /// <summary>
            /// デバイスパラメータ設定
            /// </summary>
            SET_PARAMS,
            /// <summary>
            /// 計測開始
            /// </summary>
            START_MEASUREMENT,

            /// <summary>
            /// 履歴ファイルが見つかった
            /// </summary>
            FOUND_HISTORY_FILE,
            /// <summary>
            /// 履歴ファイルが見つからない
            /// </summary>
            LOST_HISTORY_FILE,
            /// <summary>
            /// 履歴再生開始
            /// </summary>
            START_REPLAY,
            /// <summary>
            /// 履歴再生一時停止
            /// </summary>
            PAUSE_REPLAY,
            /// <summary>
            /// 履歴一括再生
            /// </summary>
            PLOT_REPLAY,
        }

        /// <summary>
        /// 画面状態
        /// </summary>
        private DISPLAY_STATUS displayStatus = DISPLAY_STATUS.STARTUP;

        /// <summary>
        /// ファイル保存ダイアログ表示設定
        /// </summary>
        private bool showSaveFileDialog = false;

        /// <summary>
        /// 履歴ファイルフォルダ
        /// </summary>
        private string historyDataFolder = string.Empty;

        /// <summary>
        /// 保存ファイル名
        /// </summary>
        private string saveFilePath = string.Empty;

        /// <summary>
        /// 加速度Ｘ軸オフセット
        /// </summary>
        private int accXAxisOffset = 0;

        /// <summary>
        /// 加速度Ｙ軸オフセット
        /// </summary>
        private int accYAxisOffset = 0;

        /// <summary>
        /// 加速度Ｚ軸オフセット
        /// </summary>
        private int accZAxisOffset = 0;

        /// <summary>
        /// 計測データ表示状態（true:計測データ  false:履歴データ）
        /// </summary>
        private bool displayMeasureData = false;


        /// <summary>
        /// 2015.4 openGLでクォータニオン表示する
        /// </summary>
        private PostureForm postureForm = new PostureForm();

        /// <summary>
        /// 再生時のモード保存
        /// </summary>
        private string replayMode = "";

        /// <summary>
        /// 通信成功率プログレス値
        /// </summary>
        private double successRateProgressValue = 0;

        /// <summary>
        /// 過渡データ損失プログレス値
        /// </summary>
        private double transientDataLossProgressValue = 0;

        /// <summary>
        /// アプリケーション終了中
        /// </summary>
        private bool appClosing = false;

        /// <summary>
        /// グラフアクセスMutex
        /// </summary>
        private static Mutex graphMutex = new Mutex();


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public mainForm()
        {
            InitializeComponent();

            // 2015.4 openGLでクォータニオン表示する
            postureForm.Visible = false;
            AddOwnedForm(postureForm);
        }

        /// <summary>
        /// フォームロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainForm_Load(object sender, EventArgs e)
        {
            // アカデミアデータモードコンボ初期化
            this.modeSelectCombo.Items.Add("Standard");
            this.modeSelectCombo.Items.Add("Full");
            this.modeSelectCombo.Items.Add("Quaternion");

            // データ品質初期化
            this.transmissionSpeedCombo.Items.Add("100 Hz");
            this.transmissionSpeedCombo.Items.Add("50 Hz");

            // 加速度センサ測定レンジコンボ初期化
            this.accRangeCombo.Items.Add("2g");
            this.accRangeCombo.Items.Add("4g");
            this.accRangeCombo.Items.Add("8g");
            this.accRangeCombo.Items.Add("16g");

            // 角速度センサ測定レンジコンボ初期化
            this.gyroscopeRangeCombo.Items.Add("250dps");
            this.gyroscopeRangeCombo.Items.Add("500dps");
            this.gyroscopeRangeCombo.Items.Add("1000dps");
            this.gyroscopeRangeCombo.Items.Add("2000dps");

            // 再生速度コンボ初期化
            this.replaySpeedCombo.Items.Add("x 1");
            this.replaySpeedCombo.Items.Add("x 2");
            this.replaySpeedCombo.Items.Add("x 4");
            //this.replaySpeedCombo.Items.Add("x 8");
            //this.replaySpeedCombo.Items.Add("x 16");
            //this.replaySpeedCombo.Items.Add("x 32");
            this.replaySpeedCombo.SelectedIndex = 0;

            // Chartコンボに軸オブジェクトを追加
            ComboItemObj[] axisItems = new ComboItemObj[]
            {
                new ComboItemObj("Hide", null),
                new ComboItemObj("Accelerometer", AxisMaster.AccelerationAxis),
                new ComboItemObj("Gyroscope", AxisMaster.AngularVelocityAxis),
                new ComboItemObj("Electrooculography", AxisMaster.ElectrooculographyAxis),
                // TODO : クォータニオン除外
                //new ComboItemObj("Quaternion", AxisMaster.QuaternionAxis),
            };

            // Chartコンボの初期値
            this.chart1Select.SetComboItems(axisItems, AxisMaster.AccelerationAxis);
            this.chart2Select.SetComboItems(axisItems, AxisMaster.AngularVelocityAxis);
            this.chart3Select.SetComboItems(axisItems, AxisMaster.ElectrooculographyAxis);

            // 設定ファイルから選択値を取得する
            this.chart1Select.SelectedIndex = settings.GetInteger("chart1Select", "index", 1);
            this.chart2Select.SelectedIndex = settings.GetInteger("chart2Select", "index", 2);
            this.chart3Select.SelectedIndex = settings.GetInteger("chart3Select", "index", 3);

            // Chartコンボの確定処理
            this.changeChart();

            // イベント通知設定
            this.dataManager.ScanComPortProgressEvent += new DataManager.ScanComPortProgressHandler(this.OnScanComPortProgress);
            this.dataManager.DongleConnectEvent += new DataManager.DongleConnectHandler(this.OnDongleConnect);
            this.dataManager.BluetoothMacAddressEvent += new DataManager.BluetoothMacAddressHandler(this.OnBluetoothMacAddress);
            this.dataManager.BluetoothConnectEvent += new DataManager.BluetoothConnectHandler(this.OnBluetoothConnect);
            this.dataManager.DeviceInitializeEvent += new DataManager.DeviceInitializeHandler(this.OnDeviceInitialize);
            this.dataManager.MeasureStartEvent += new DataManager.MeasureStartHandler(this.OnMeasureStart);
            this.dataManager.MeasureStopEvent += new DataManager.MeasureStopHandler(this.OnMeasureStop);
            this.dataManager.FreeMarkingEndEvent += new DataManager.FreeMarkingEndHandler(this.OnFreeMarkingEnd);
            this.dataManager.ErrorRateEvent += new DataManager.ErrorRateHandler(this.OnErrorRate);
            this.file.EndFileDataEvent += new FileAccess.EndFileDataEventHandler(this.OnEndFileData);
            this.file.FileReadProgressEvent += new FileAccess.FileReadProgressEventHandler(this.OnFileReadProgress);
            this.file.SocketStatusEvent += new FileAccess.SocketStatusHandler(this.OnSocketStatus);

            // その他設定値読込
            SettingForm form = new SettingForm(this.settings);
            this.file.SaveFolder = form.SaveFolder;
            this.file.UseSocket = form.UseSocket;
            this.file.SocketPort = form.SocketPort;
            this.file.SocketAddress = form.SocketAddress;
            this.file.RecordFileDateFormat = form.RecordFileDateFormat;
            this.dataManager.FreeMarkingTime = form.MarkingTimeValue;
            this.accXAxisOffset = form.AccXAxisOffset;
            this.accYAxisOffset = form.AccYAxisOffset;
            this.accZAxisOffset = form.AccZAxisOffset;
            this.showSaveFileDialog = form.ShowSaveFileDialog;

            if (form.UseSocket == false)
            {
                this.socketIPAddressLabel.Text = "IP address : ";
                this.socketPortLabel.Text = "Port : ";
                this.socketStatusLabel.Text = "Status : Disable";
            }
            else
            {
                this.socketIPAddressLabel.Text = string.Format("IP address : {0}", form.SocketAddress);
                this.socketPortLabel.Text = string.Format("Port : {0}", form.SocketPort);
            }

            // データフォルダ一覧を取得
            this.historyDataFolder = this.file.SaveFolder;
            this.seekHistoryData();
            this.changeDisplayStatus(DISPLAY_STATUS.LOST_HISTORY_FILE);

            // バッテリーレベルのプログレスバーを独自描画する
            typeof(Control).GetMethod("SetStyle", BindingFlags.NonPublic | BindingFlags.Instance)
               .Invoke(this.batteryLevelProgress.ProgressBar, new object[] { ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true });
            this.batteryLevelProgress.Tag = 0;

            // 通信エラー値のプログレスを独自描画する
            typeof(Control).GetMethod("SetStyle", BindingFlags.NonPublic | BindingFlags.Instance)
               .Invoke(this.successRateProgress.ProgressBar, new object[] { ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true });
            typeof(Control).GetMethod("SetStyle", BindingFlags.NonPublic | BindingFlags.Instance)
               .Invoke(this.transientDataLossProgress.ProgressBar, new object[] { ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true });

            // グラフ位置変更通知
            this.graphControl.SetChangeGraphPointMethod(this.OnChangeGraphPoint);

            // Comポート名の取得
            this.changeDisplayStatus(DISPLAY_STATUS.STARTUP);
            this.getComPortName();
        }

        /// <summary>
        /// フォームクロージング
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 測定中は閉じられないようにする
            if (this.measureDataThreadLoop)
            {
                Common.ShowMessageBox("Please stop the measurement.", "Close Window", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
            }
            else if ((this.displayStatus >= DISPLAY_STATUS.INCONNECT_BLUETOOTH) && (this.displayStatus <= DISPLAY_STATUS.START_MEASUREMENT))
            {
                Common.ShowMessageBox("Please disconnect \"MEME\" device.", "Close Window", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
            }
            else
            {
                this.appClosing = true;
            }
        }

        /// <summary>
        /// フォームクローズ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 測定スレッドが継続中の場合は、測定停止要求を行う
            if (this.measureDataThreadLoop)
            {
                this.batteryLevelTimer.Enabled = false;
                this.dataManager.MeasureStop();
                this.measureDataThreadLoop = false;
                this.measureDataThread.Join(500);
                this.measureDataThread = null;
                this.measureButton.Text = "Start Measurement";
            }

            // ソケットを切断する
            this.file.CloseSocket();

            // シリアルポート検索を終了する
            this.dataManager.StopScanComPort();

            // 設定ファイルに選択値を保存する
            settings.SetInteger("chart1Select", "index", this.chart1Select.SelectedIndex);
            settings.SetInteger("chart2Select", "index", this.chart2Select.SelectedIndex);
            settings.SetInteger("chart3Select", "index", this.chart3Select.SelectedIndex);

            //settings.SetStringValue("history", "folder", this.historyDataFolder);
        }

        /// <summary>
        /// 画面状態変更
        /// </summary>
        /// <param name="status"></param>
        private void changeDisplayStatus(DISPLAY_STATUS status)
        {
            Color enableBackColor = Color.FromArgb(0xE0, 0x00, 0x02);
            Color enableForeColor = Color.White;
            Color disableBackColor = Color.LightGray;
            Color disableForeColor = Color.FromKnownColor(KnownColor.ControlText);

            switch (status)
            {
                case DISPLAY_STATUS.STARTUP:
                    this.setControlStatus(false, this.scanPortButton, disableForeColor, disableBackColor);
                    this.scanComPortProgress.Visible = true;
                    this.setControlStatus(false, this.comPortNameCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.comConnectButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothScanButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothMacCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothConnectButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.modeSelectCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.transmissionSpeedCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.accRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.gyroscopeRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.initializeButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.measureButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.freeMarkingButton, disableForeColor, disableBackColor);
                    this.successRateLabel.Visible = false;
                    this.successRateProgress.Visible = false;
                    this.transientDataLossLabel.Visible = false;
                    this.transientDataLossProgress.Visible = false;
                    break;
                case DISPLAY_STATUS.LOST_COM:
                    this.setControlStatus(true, this.scanPortButton, enableForeColor, enableBackColor);
                    this.scanComPortProgress.Visible = false;
                    this.setControlStatus(false, this.comPortNameCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.comConnectButton, disableForeColor, disableBackColor);
                    break;
                case DISPLAY_STATUS.FOUND_COM:
                    this.setControlStatus(true, this.scanPortButton, enableForeColor, enableBackColor);
                    this.scanComPortProgress.Visible = false;
                    this.setControlStatus(true, this.comPortNameCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(true, this.comConnectButton, enableForeColor, enableBackColor);
                    break;
                case DISPLAY_STATUS.DISCONNECT_COM:
                    this.setControlStatus(true, this.scanPortButton, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.comPortNameCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(true, this.comConnectButton, enableForeColor, enableBackColor);
                    this.comConnectButton.Text = "Open";
                    this.setControlStatus(false, this.bluetoothScanButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothConnectButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothMacCombo, disableForeColor, disableBackColor);
                    this.bluetoothMacCombo.Items.Clear();
                    this.bluetoothConnectButton.Text = "Connect";
                    this.connectionStatusLabel.Text = "Status : Disconnect";
                    this.setControlStatus(false, this.modeSelectCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.transmissionSpeedCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.accRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.gyroscopeRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.initializeButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.measureButton, disableForeColor, disableBackColor);
                    this.measureButton.Text = "Start Measurement";
                    this.setControlStatus(false, this.freeMarkingButton, disableForeColor, disableBackColor);
                    this.chartGroupBox.Enabled = true;
                    this.batteryLevelProgress.Value = 0;
                    this.deviceVersionLabel.Text = "NotFound";
                    this.settingToolStripMenuItem.Enabled = true;
                    this.tabPage2.Enabled = true;
                    this.successRateLabel.Visible = false;
                    this.successRateProgress.Visible = false;
                    this.transientDataLossLabel.Visible = false;
                    this.transientDataLossProgress.Visible = false;
                    this.seekHistoryData();
                    this.changeDisplayStatus(DISPLAY_STATUS.LOST_HISTORY_FILE);
                    break;
                case DISPLAY_STATUS.INCONNECT_DONGLE:
                    this.setControlStatus(false, this.scanPortButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.comPortNameCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.comConnectButton, disableForeColor, disableBackColor);
                    break;
                case DISPLAY_STATUS.CONNECT_DONGLE:
                    this.setControlStatus(false, this.scanPortButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.comPortNameCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(true, this.comConnectButton, enableForeColor, enableBackColor);
                    this.comConnectButton.Text = "Close";
                    this.setControlStatus(true, this.bluetoothScanButton, enableForeColor, enableBackColor);
                    this.settingToolStripMenuItem.Enabled = false;
                    this.tabPage2.Enabled = false;
                    this.setControlStatus(false, this.previousFileList, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.setDataFolderButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.fromDatetimeText, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.toDatetimeText, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replaySpeedCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replayStartButton, disableForeColor, disableBackColor);
                    this.historyFileModeLabel.Text = "Data mode  :  ";
                    this.historyTransmissionSpeedLabel.Text = "Transmission speed  :  ";
                    this.historyFileAccRangeLabel.Text = "Accelerometer sensor's range  :  ";
                    this.historyFileGyroRangeLabel.Text = "Gyroscope sensor's range  :  ";
                    this.fromDatetimeText.Text = string.Empty;
                    this.toDatetimeText.Text = string.Empty;
                    break;
                case DISPLAY_STATUS.SCAN_BLUETOOTH:
                    this.setControlStatus(false, this.comConnectButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothScanButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothMacCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothConnectButton, disableForeColor, disableBackColor);
                    break;
                case DISPLAY_STATUS.LOST_BLUETOOTH:
                    this.batteryLevelProgress.Value = 0;
                    this.deviceVersionLabel.Text = "NotFound";
                    this.setControlStatus(true, this.comConnectButton, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.bluetoothScanButton, enableForeColor, enableBackColor);
                    this.bluetoothMacCombo.Items.Clear();
                    this.setControlStatus(false, this.bluetoothMacCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothConnectButton, disableForeColor, disableBackColor);
                    this.bluetoothConnectButton.Text = "Connect";
                    this.connectionStatusLabel.Text = "Status : Disconnect";
                    this.setControlStatus(false, this.modeSelectCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.transmissionSpeedCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.accRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.gyroscopeRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.initializeButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.measureButton, disableForeColor, disableBackColor);
                    this.measureButton.Text = "Start Measurement";
                    this.setControlStatus(false, this.freeMarkingButton, disableForeColor, disableBackColor);
                    this.chartGroupBox.Enabled = true;
                    this.successRateLabel.Visible = false;
                    this.successRateProgress.Visible = false;
                    this.transientDataLossLabel.Visible = false;
                    this.transientDataLossProgress.Visible = false;
                    break;
                case DISPLAY_STATUS.FOUND_BLUETOOTH:
                    this.setControlStatus(true, this.comConnectButton, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.bluetoothScanButton, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.bluetoothMacCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(true, this.bluetoothConnectButton, enableForeColor, enableBackColor);
                    this.bluetoothConnectButton.Text = "Connect";
                    this.connectionStatusLabel.Text = "Status : Disconnect";
                    this.setControlStatus(false, this.modeSelectCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.transmissionSpeedCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.accRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.gyroscopeRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.initializeButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.measureButton, disableForeColor, disableBackColor);
                    this.successRateLabel.Visible = false;
                    this.successRateProgress.Visible = false;
                    this.transientDataLossLabel.Visible = false;
                    this.transientDataLossProgress.Visible = false;
                    break;
                case DISPLAY_STATUS.INCONNECT_BLUETOOTH:
                    this.setControlStatus(false, this.comConnectButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothScanButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothMacCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothConnectButton, disableForeColor, disableBackColor);
                    break;
                case DISPLAY_STATUS.DISCONNECT_BLUETOOTH:
                    this.batteryLevelProgress.Value = 0;
                    this.deviceVersionLabel.Text = "NotFound";
                    this.setControlStatus(false, this.comConnectButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothScanButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothMacCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.bluetoothConnectButton, disableForeColor, disableBackColor);
                    break;
                case DISPLAY_STATUS.CONNECT_BLUETOOTH:                    
                    this.setControlStatus(true, this.bluetoothConnectButton, enableForeColor, enableBackColor);
                    this.bluetoothConnectButton.Text = "Disconnect";
                    this.connectionStatusLabel.Text = "Status : Connected";
                    this.setControlStatus(true, this.modeSelectCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(true, this.transmissionSpeedCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(true, this.accRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(true, this.gyroscopeRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(true, this.initializeButton, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.measureButton, enableForeColor, enableBackColor);
                    this.measureButton.Text = "Start Measurement";
                    this.setControlStatus(false, this.freeMarkingButton, disableForeColor, disableBackColor);
                    this.chartGroupBox.Enabled = true;
                    this.successRateLabel.Visible = false;
                    this.successRateProgress.Visible = false;
                    this.transientDataLossLabel.Visible = false;
                    this.transientDataLossProgress.Visible = false;
                    break;
                case DISPLAY_STATUS.CLEAR_PARAMS:
                case DISPLAY_STATUS.SET_PARAMS:
                    this.setControlStatus(false, this.bluetoothConnectButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.modeSelectCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.transmissionSpeedCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.accRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.gyroscopeRangeCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.initializeButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.measureButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.freeMarkingButton, disableForeColor, disableBackColor);
                    this.chartGroupBox.Enabled = false;
                    this.successRateLabel.Visible = false;
                    this.successRateProgress.Visible = false;
                    this.transientDataLossLabel.Visible = false;
                    this.transientDataLossProgress.Visible = false;
                    break;
                case DISPLAY_STATUS.START_MEASUREMENT:
                    this.setControlStatus(true, this.measureButton, enableForeColor, enableBackColor);
                    this.measureButton.Text = "Stop Measurement";
                    this.setControlStatus(true, this.freeMarkingButton, enableForeColor, enableBackColor);
                    this.chartGroupBox.Enabled = false;
                    this.successRateLabel.Visible = true;
                    this.successRateProgress.Visible = true;
                    this.transientDataLossLabel.Visible = true;
                    this.transientDataLossProgress.Visible = true;
                    break;

                case DISPLAY_STATUS.FOUND_HISTORY_FILE:
                    this.setControlStatus(true, this.previousFileList, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.setDataFolderButton, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.fromDatetimeText, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.toDatetimeText, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.replaySpeedCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(true, this.replayStartButton, enableForeColor, enableBackColor);
                    this.replayPauseButton.Text = "Pause";
                    this.setControlStatus(false, this.replayPauseButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replayStopButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replayPlotButton, disableForeColor, disableBackColor);
                    this.fileReadProgress.Visible = false;
                    this.chartGroupBox.Enabled = true;
                    this.tabPage1.Enabled = true;
                    if (this.comPortNameCombo.Items.Count == 0)
                    {
                        this.changeDisplayStatus(DISPLAY_STATUS.LOST_COM);
                    }
                    else
                    {
                        this.changeDisplayStatus(DISPLAY_STATUS.FOUND_COM);
                    }
                    break;
                case DISPLAY_STATUS.LOST_HISTORY_FILE:
                    this.setControlStatus(true, this.previousFileList, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.setDataFolderButton, enableForeColor, enableBackColor);
                    this.setControlStatus(false, this.fromDatetimeText, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.toDatetimeText, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replaySpeedCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replayStartButton, disableForeColor, disableBackColor);
                    this.replayPauseButton.Text = "Pause";
                    this.setControlStatus(false, this.replayPauseButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replayStopButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replayPlotButton, disableForeColor, disableBackColor);
                    this.historyFileModeLabel.Text = "Data mode  :  ";
                    this.historyTransmissionSpeedLabel.Text = "Transmission speed  :  ";
                    this.historyFileAccRangeLabel.Text = "Accelerometer sensor's range  :  ";
                    this.historyFileGyroRangeLabel.Text = "Gyroscope sensor's range  :  ";
                    this.fromDatetimeText.Text = string.Empty;
                    this.toDatetimeText.Text = string.Empty;
                    this.chartGroupBox.Enabled = true;
                    this.tabPage1.Enabled = true;
                    if (this.comPortNameCombo.Items.Count == 0)
                    {
                        this.changeDisplayStatus(DISPLAY_STATUS.LOST_COM);
                    }
                    else
                    {
                        this.changeDisplayStatus(DISPLAY_STATUS.FOUND_COM);
                    }
                    break;
                case DISPLAY_STATUS.START_REPLAY:
                    this.tabPage1.Enabled = false;
                    this.setControlStatus(false, this.scanPortButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.comPortNameCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.comConnectButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.previousFileList, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.setDataFolderButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.fromDatetimeText, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.toDatetimeText, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replaySpeedCombo, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replayStartButton, disableForeColor, disableBackColor);
                    this.replayPauseButton.Text = "Pause";
                    this.setControlStatus(true, this.replayPauseButton, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.replayStopButton, enableForeColor, enableBackColor);
                    this.setControlStatus(true, this.replayPlotButton, enableForeColor, enableBackColor);
                    this.chartGroupBox.Enabled = false;
                    break;
                case DISPLAY_STATUS.PAUSE_REPLAY:
                    this.replayPauseButton.Text = "Resume";
                    this.setControlStatus(false, this.replayPlotButton, disableForeColor, disableBackColor);
                    break;
                case DISPLAY_STATUS.PLOT_REPLAY:
                    this.setControlStatus(false, this.replayPauseButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replayStopButton, disableForeColor, disableBackColor);
                    this.setControlStatus(false, this.replayPlotButton, disableForeColor, disableBackColor);
                    this.fileReadProgress.Visible = true;
                    break;
            }

            this.displayStatus = status;
        }

        /// <summary>
        /// コントロール状態変更
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="control"></param>
        /// <param name="foreColor"></param>
        /// <param name="backColor"></param>
        private void setControlStatus(bool enable, Control control, Color foreColor, Color backColor)
        {
            control.Enabled = enable;
            control.ForeColor = foreColor;
            control.BackColor = backColor;
        }

        /// <summary>
        /// Comポート検索ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void scanPortButton_Click(object sender, EventArgs e)
        {
            Tracer.WriteInformation("Push [scan Port] button.");
            this.getComPortName();
        }

        /// <summary>
        /// ComPort検索進捗状況通知
        /// </summary>
        /// <param name="progress"></param>
        private void OnScanComPortProgress(int progress)
        {
            if (this.appClosing == true)
            {
                return;
            }

            if (this.InvokeRequired)
            {
                BeginInvoke(new DataManager.ScanComPortProgressHandler(OnScanComPortProgress), new object[] { progress });
                return;
            }

            Tracer.WriteVerbose("Scan com port progress. {0}", progress);
            this.scanComPortProgress.Value = progress;
            if (progress >= 100)
            {
                foreach (string portName in this.dataManager.DonglePortList)
                {
                    this.comPortNameCombo.Items.Add(portName);
                }
                if (this.comPortNameCombo.Items.Count == 0)
                {
                    this.comPortNameCombo.SelectedIndex = -1;
                    this.changeDisplayStatus(DISPLAY_STATUS.LOST_COM);
                }
                else
                {
                    this.comPortNameCombo.SelectedIndex = 0;
                    this.changeDisplayStatus(DISPLAY_STATUS.FOUND_COM);
                }
            }
        }

        /// <summary>
        /// Ｃｏｍポート名取得
        /// </summary>
        private void getComPortName()
        {
            /*
            // Comポートをコンボに追加
            this.comPortNameCombo.Items.Clear();
            foreach (string name in this.dataManager.ComPortNameList)
            {
                this.comPortNameCombo.Items.Add(name);
            }

            // Com検索状態によりコントロール状態を変更
            if (this.comPortNameCombo.Items.Count == 0)
            {
                this.comPortNameCombo.SelectedIndex = -1;
                this.changeDisplayStatus(DISPLAY_STATUS.LOST_COM);
            }
            else
            {
                this.comPortNameCombo.SelectedIndex = 0;
                this.changeDisplayStatus(DISPLAY_STATUS.FOUND_COM);
            }
            */

            this.changeDisplayStatus(DISPLAY_STATUS.STARTUP);
            this.comPortNameCombo.Items.Clear();
            this.dataManager.ScanComPort();
        }

        /// <summary>
        /// Comポート接続（切断）ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comConnectButton_Click(object sender, EventArgs e)
        {
            this.dongleVersionLabel.Text = "NotFound";

            if (this.comConnectButton.Text.Equals("Open"))
            {
                Tracer.WriteInformation("Push [Connect (serial)] button. ({0})", this.comPortNameCombo.Text);
                if (this.dataManager.ConnectComPort(this.comPortNameCombo.Text))
                {
                    this.changeDisplayStatus(DISPLAY_STATUS.INCONNECT_DONGLE);
                }
                else
                {
                    Common.ShowMessageBox(string.Format("Failure serial connected. ({0})", this.comPortNameCombo.Text), "Serial", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Tracer.WriteInformation("Push [Disconnect (serial)] button. ({0})", this.comPortNameCombo.Text);
                this.bluetoothMacCombo.Items.Clear();
                this.dataManager.DisconnectComPort();
                this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
            }
        }

        /// <summary>
        /// Bluetooth検索ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bluetoothScanButton_Click(object sender, EventArgs e)
        {
            if (this.bluetoothScanButton.Enabled)
            {
                Tracer.WriteInformation("Push [scan Device] button.");
                this.bluetoothMacCombo.Items.Clear();
                this.changeDisplayStatus(DISPLAY_STATUS.SCAN_BLUETOOTH);
                if (this.dataManager.ScanBluetoothDevice() == false)
                {
                    this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                    Common.ShowMessageBox("Bluetooth failed to scan.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Bluetooth MACアドレス受信
        /// </summary>
        /// <param name="macAddress"></param>
        private void OnBluetoothMacAddress(string macAddress)
        {
            if (this.displayStatus != DISPLAY_STATUS.SCAN_BLUETOOTH)
            {
                return;
            }

            if (this.InvokeRequired)
            {
                Invoke(new DataManager.BluetoothMacAddressHandler(OnBluetoothMacAddress), new object[] { macAddress });
                return;
            }

            if (macAddress.Equals(string.Empty))
            {
                // 終了通知
                this.changeDisplayStatus(this.bluetoothMacCombo.Items.Count == 0 ? DISPLAY_STATUS.LOST_BLUETOOTH : DISPLAY_STATUS.FOUND_BLUETOOTH);
                return;
            }

            if(this.bluetoothMacCombo.FindStringExact(macAddress) == -1)
            {
                this.bluetoothMacCombo.Items.Add(macAddress);
                this.bluetoothMacCombo.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Bluetooth接続（切断）ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bluetoothConnectButton_Click(object sender, EventArgs e)
        {
            if (this.bluetoothConnectButton.Text.Equals("Connect"))
            {
                Tracer.WriteInformation("Push [Connect (Bluetooth)] button. ({0})", this.bluetoothMacCombo.Text);
                this.changeDisplayStatus(DISPLAY_STATUS.INCONNECT_BLUETOOTH);
                if (this.dataManager.ConnectBluetooth(this.bluetoothMacCombo.Text) == false)
                {
                    this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                    Common.ShowMessageBox("Bluetooth failed to connect.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Tracer.WriteInformation("Push [Disconnect (Bluetooth)] button. ({0})", this.bluetoothMacCombo.Text);
                this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_BLUETOOTH);
                if (this.dataManager.DisconnectBluetooth() == false)
                {
                    this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                    Common.ShowMessageBox("Bluetooth failed to disconnect.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// ドングル接続通知
        /// </summary>
        /// <param name="status"></param>
        private void OnDongleConnect(byte status, string dongleVersion)
        {
            if (this.InvokeRequired)
            {
                Invoke(new DataManager.DongleConnectHandler(OnDongleConnect), new object[] { status, dongleVersion });
                return;
            }

            // 画面状態により、処理分岐
            switch(this.displayStatus)
            {
                case DISPLAY_STATUS.INCONNECT_DONGLE:
                    if (status != Constants.RESPONSE_ACK)
                    {
                        // エラー時は、ドングルバージョンにエラーメッセージが入っているので表示する
                        this.changeDisplayStatus(DISPLAY_STATUS.FOUND_COM);
                        Common.ShowMessageBox(dongleVersion, "Serial", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        // 正常終了時は、ドングルバージョンをステータスに表示する
                        this.changeDisplayStatus(DISPLAY_STATUS.CONNECT_DONGLE);
                        this.dongleVersionLabel.Text = dongleVersion;
                    }
                    break;

                case DISPLAY_STATUS.SCAN_BLUETOOTH:
                    if (status != Constants.RESPONSE_ACK)
                    {
                        // エラー時は、ドングルバージョンにエラーメッセージが入っているので表示する
                        this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                        Common.ShowMessageBox(dongleVersion, "Serial", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;
            }
        }

        /// <summary>
        /// Bluetooth 接続受信
        /// </summary>
        /// <param name="status">
        /// 正常              0x00    Constants.RESPONSE_ACK
        /// 異常              0xFF    Constants.RESPONSE_NACK
        /// 信号切断          0xF0    Constants.RESPONSE_DISCONNECT
        /// タイムアウト      0xF1    Constants.RESPONSE_TIMEOUT
        /// その他異常        0xF2    Constants.RESPONSE_ETC_ERROR    
        /// </param>
        private void OnBluetoothConnect(byte status)
        {
            if (this.InvokeRequired)
            {
                Invoke(new DataManager.BluetoothConnectHandler(OnBluetoothConnect), new object[] { status });
                return;
            }

            // 画面状態により、処理分岐
            switch(this.displayStatus)
            {
                case DISPLAY_STATUS.CONNECT_DONGLE:
                    if (status == Constants.RESPONSE_DISCONNECT)
                    {
                        this.changeDisplayStatus(DISPLAY_STATUS.FOUND_COM);
                        Common.ShowMessageBox("Bluetooth failed to connect.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                   break;
                case DISPLAY_STATUS.SCAN_BLUETOOTH:
                    if (status != Constants.RESPONSE_ACK)
                    {
                        this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                        Common.ShowMessageBox("Bluetooth failed to connect.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;

                case DISPLAY_STATUS.INCONNECT_BLUETOOTH:
                    if (status == Constants.RESPONSE_ACK)
                    {
                        // TODO : 最終リリース時には、ファームバージョン比較を行う？
                        string firmVersion = this.dataManager.FirmwareVersion;


                        this.deviceVersionLabel.Text = firmVersion;
                        this.modeSelectCombo.SelectedIndex = this.dataManager.DeviceMode - 1;
                        this.transmissionSpeedCombo.SelectedIndex = this.dataManager.TransmissionSpeed - 1;
                        this.accRangeCombo.SelectedIndex = this.dataManager.AccelerationSensorRange;
                        this.gyroscopeRangeCombo.SelectedIndex = this.dataManager.GyroSensorRange;

                        // 角速度レンジ変更
                        this.changeAngularRange();

                        this.changeDisplayStatus(DISPLAY_STATUS.CONNECT_BLUETOOTH);
                    }
                    else
                    {
                        this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                        Common.ShowMessageBox("Bluetooth failed to connect.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;

                case DISPLAY_STATUS.DISCONNECT_BLUETOOTH:
                    if (status == Constants.RESPONSE_DISCONNECT)
                    {
                        this.changeDisplayStatus(DISPLAY_STATUS.FOUND_BLUETOOTH);
                    }
                    else
                    {
                        this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                        Common.ShowMessageBox("Bluetooth failed to connect.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;

                case DISPLAY_STATUS.CONNECT_BLUETOOTH:
                case DISPLAY_STATUS.CLEAR_PARAMS:
                case DISPLAY_STATUS.SET_PARAMS:
                    if (status != Constants.RESPONSE_ACK)
                    {
                        this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                        Common.ShowMessageBox("Bluetooth failed to connect.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;

                case DISPLAY_STATUS.START_MEASUREMENT:
                    if (status != Constants.RESPONSE_ACK)
                    {
                        this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                        Common.ShowMessageBox("Bluetooth failed to connect.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        this.measureThreadStop();

                        // OpenGLウィンドウを閉じる
                        if (postureForm != null && !postureForm.IsDisposed)
                        {
                            postureForm.Close();
                            postureForm = null;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// デバイスパラメータ初期化ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void initializeButton_Click(object sender, EventArgs e)
        {
            Tracer.WriteInformation("Push [initialize] button.");
            this.changeDisplayStatus(DISPLAY_STATUS.CLEAR_PARAMS);
            if (this.dataManager.ClearParameter() == false)
            {
                this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                Common.ShowMessageBox("Failure device initialize.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// デバイスパラメータ初期化応答受信
        /// </summary>
        /// <param name="status">
        /// 正常              0x00    Constants.RESPONSE_ACK
        /// 異常              0xFF    Constants.RESPONSE_NACK
        /// 信号切断          0xF0    Constants.RESPONSE_DISCONNECT
        /// タイムアウト      0xF1    Constants.RESPONSE_TIMEOUT
        /// その他異常        0xF2    Constants.RESPONSE_ETC_ERROR    
        /// </param>
        private void OnDeviceInitialize(byte status)
        {
            if (this.InvokeRequired)
            {
                Invoke(new DataManager.DeviceInitializeHandler(OnDeviceInitialize), new object[] { status });
                return;
            }

            if (status == Constants.RESPONSE_ACK)
            {
                this.modeSelectCombo.SelectedIndex = this.dataManager.DeviceMode - 1;
                this.transmissionSpeedCombo.SelectedIndex = this.dataManager.TransmissionSpeed - 1;
                this.accRangeCombo.SelectedIndex = this.dataManager.AccelerationSensorRange;
                this.gyroscopeRangeCombo.SelectedIndex = this.dataManager.GyroSensorRange;

                // 角速度レンジ変更
                this.changeAngularRange();

                this.changeDisplayStatus(DISPLAY_STATUS.CONNECT_BLUETOOTH);
            }
            else
            {
                this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                Common.ShowMessageBox("Bluetooth failed to connect.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 測定開始ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void measureButton_Click(object sender, EventArgs e)
        {
            //if (this.dataManager.MeasureStatus == false)
            if (this.measureDataThreadLoop == false)
            {
                Tracer.WriteInformation("Push [Measurement start] button.");
                // 測定開始
                this.dataManager.ClearSensorData();
                this.graphControl.ClearElapsed();
                this.graphControl.ClearAccelerationData();
                this.graphControl.ClearAngularVelocityData();
                this.graphControl.ClearElectrooculographyData();
                this.graphControl.ClearQuaternionData();
                this.graphControl.ScrollReset();
                this.graphControl.GraphOperation = true;
                this.graphControl.DataEraseMode = true;
                this.displayMeasureData = true;
                this.successRateProgress.Value = 99;
                this.transientDataLossProgress.Value = 99;

                // 角速度レンジ変更
                this.changeAngularRange();
                
                // 2015.4 openGLでクォータニオン表示する
                // モード３のとき姿勢表示画面を表示
                if ((this.modeSelectCombo.SelectedIndex + 1) == 3)
                {
                    if (postureForm == null || postureForm.IsDisposed)
                    {
                        postureForm = new PostureForm();
                    }
                    // TODO : OpenGL表示を暫定非表示
                    //postureForm.BringToFront();
                    //postureForm.Visible = true;
                }


                this.changeDisplayStatus(DISPLAY_STATUS.SET_PARAMS);
                if (this.dataManager.MeasureStart((byte)(this.modeSelectCombo.SelectedIndex + 1), (byte)(this.transmissionSpeedCombo.SelectedIndex + 1), (byte)this.accRangeCombo.SelectedIndex, (byte)this.gyroscopeRangeCombo.SelectedIndex) == false)
                {
                    this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                    Common.ShowMessageBox("Failure measurement start.", "Measurement", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Tracer.WriteInformation("Push [Measurement stop] button.");

                this.graphControl.GraphOperation = true;
                this.graphControl.DataEraseMode = false;

                // 測定停止
                if (this.dataManager.MeasureStop())
                {
                    this.changeDisplayStatus(DISPLAY_STATUS.SET_PARAMS);
                }
                else
                {
                    this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                    Common.ShowMessageBox("Failure measurement stop.", "Measurement", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // 計測スレッド停止
                    this.measureThreadStop();

                    // OpenGLウィンドウを閉じる
                    if (postureForm != null && !postureForm.IsDisposed)
                    {
                        postureForm.Close();
                        postureForm = null;
                    }
                }
           }
        }

        /// <summary>
        /// 測定開始応答受信
        /// </summary>
        /// <param name="status">
        /// 正常              0x00    Constants.RESPONSE_ACK
        /// 異常              0xFF    Constants.RESPONSE_NACK
        /// 信号切断          0xF0    Constants.RESPONSE_DISCONNECT
        /// タイムアウト      0xF1    Constants.RESPONSE_TIMEOUT
        /// その他異常        0xF2    Constants.RESPONSE_ETC_ERROR    
        /// </param>
        private void OnMeasureStart(byte status)
        {
            if (this.InvokeRequired)
            {
                Invoke(new DataManager.MeasureStartHandler(OnMeasureStart), new object[] { status });
                return;
            }

            if (status != Constants.RESPONSE_ACK)
            {
                this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                Common.ShowMessageBox("Failure measurement start.", "Measurement", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                // ファイル記録開始
                DateTime dateTime = DateTime.UtcNow;
                this.saveFilePath = this.file.SaveFolder + @"\" + this.bluetoothMacCombo.Text.Replace(":", "") + "_" +
                                    string.Format("{0,0:D4}{1,0:D2}{2,0:D2}{3,0:D2}{4,0:D2}{5,0:D2}.csv", dateTime.Year, dateTime.Month, dateTime.Day,
                                                                                                            dateTime.Hour, dateTime.Minute, dateTime.Second);
                this.file.RecordStart(this.saveFilePath, this.file.RecordFileDateFormat,
                                        this.dataManager.MeasureMode, this.dataManager.MeasureTransmissionSpeed, this.dataManager.MeasureAccRange, this.dataManager.MeasureGyroRange);

                // データ表示スレッド起動
                this.measureDataThreadLoop = true;
                this.measureDataThread = new Thread(new ThreadStart(this.recvMeasureData));
                this.measureDataThread.Name = "Measure data thread (Sensor)";
                this.measureDataThread.Start();

                this.batteryChargeCount = 0;
                this.batteryLevelProgress.Tag = 0;
                this.batteryLevelTimer.Enabled = true;
                this.changeDisplayStatus(DISPLAY_STATUS.START_MEASUREMENT);
            }
        }

        /// <summary>
        /// 測定停止応答受信
        /// </summary>
        /// <param name="status">
        /// 正常              0x00    Constants.RESPONSE_ACK
        /// 異常              0xFF    Constants.RESPONSE_NACK
        /// 信号切断          0xF0    Constants.RESPONSE_DISCONNECT
        /// タイムアウト      0xF1    Constants.RESPONSE_TIMEOUT
        /// その他異常        0xF2    Constants.RESPONSE_ETC_ERROR    
        /// </param>
        private void OnMeasureStop(byte status)
        {
            if (this.InvokeRequired)
            {
                Invoke(new DataManager.MeasureStopHandler(OnMeasureStop), new object[] { status });
                return;
            }

            this.changeDisplayStatus(DISPLAY_STATUS.CONNECT_BLUETOOTH);

            if (status != Constants.RESPONSE_ACK)
            {
                this.changeDisplayStatus(DISPLAY_STATUS.DISCONNECT_COM);
                Common.ShowMessageBox("Failure measurement stop.", "Measurement", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // 計測スレッド停止
            this.measureThreadStop();

            // 2015.4 openGLでクォータニオン表示する
            // モード３のとき姿勢表示画面を表示
            if ((this.modeSelectCombo.SelectedIndex + 1) == 3)
            {
                if (postureForm != null && !postureForm.IsDisposed)
                {
                    postureForm.Close();
                    postureForm = null;
                }
            }
        }

        /// <summary>
        /// 計測スレッド停止
        /// </summary>
        private void measureThreadStop()
        {
            // バッテリーレベルタイマー停止
            this.batteryLevelTimer.Enabled = false;
            this.batteryLevelProgress.Tag = 0;
            this.batteryChargeCount = 0;
            this.setBatteryLevel(0);

            // スレッド停止
            if (this.measureDataThread != null)
            {
                this.measureDataThreadLoop = false;
                this.measureDataThread.Join(500);
                this.measureDataThread = null;
            }

            // ファイル記録停止
            this.file.RecordStop();

            if (this.showSaveFileDialog)
            {
                // ファイル保存ダイアログを表示する
                this.saveFileDialog.FileName = Path.GetFileName(this.saveFilePath);
                this.saveFileDialog.InitialDirectory = Path.GetDirectoryName(this.saveFilePath);
                this.saveFileDialog.Filter = "csvファイル(*.csv)|*.csv|すべてのファイル(*.*)|*.*";
                this.saveFileDialog.Title = "Please specify the save file name.";
                this.saveFileDialog.OverwritePrompt = false;
                if (this.saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (this.saveFileDialog.FileName.Equals(this.saveFilePath) == false)
                    {
                        // ファイルのリネーム
                        if (File.Exists(this.saveFileDialog.FileName))
                        {
                            File.Delete(this.saveFileDialog.FileName);
                        }
                        File.Move(this.saveFilePath, this.saveFileDialog.FileName);
                    }
                }
            }
        }

        /// <summary>
        /// フリーマーキングボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void freeMarkingButton_Click(object sender, EventArgs e)
        {
            Tracer.WriteInformation("Push [Free marking] button.");
            Color disableBackColor = Color.LightGray;
            Color disableForeColor = Color.FromKnownColor(KnownColor.ControlText);
            this.setControlStatus(false, this.freeMarkingButton, disableForeColor, disableBackColor);
            this.dataManager.FreeMarking();
        }

        /// <summary>
        /// フリーマーキング終了通知
        /// </summary>
        private void OnFreeMarkingEnd()
        {
            if (this.InvokeRequired)
            {
                Invoke(new DataManager.FreeMarkingEndHandler(OnFreeMarkingEnd));
                return;
            }

            if (this.displayStatus == DISPLAY_STATUS.START_MEASUREMENT)
            {
                Color enableBackColor = Color.FromArgb(0xE0, 0x00, 0x02);
                Color enableForeColor = Color.White;
                this.setControlStatus(true, this.freeMarkingButton, enableForeColor, enableBackColor);
            }
        }

        /// <summary>
        /// グラフ設定適用ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chartApplyButton_Click(object sender, EventArgs e)
        {
            Tracer.WriteInformation("Push [apply] button.");

            this.changeChart();
        }

        /// <summary>
        /// グラフ表示変更
        /// </summary>
        private void changeChart()
        {

            ComboBoxEx[] combos = new ComboBoxEx[] { chart1Select, chart2Select, chart3Select };
            int countVisible = combos.Length;
            for (int i = 0; i < combos.Length; i++)
            {
                if (combos[i].Text.Equals("Hide"))
                {
                    countVisible--;
                    continue;
                }
            }

            // グラフの表示・非表示切替
            if (countVisible == 0)
            {
                this.graphControl.VisibleGraphViwe(0, false);
                this.graphControl.VisibleGraphViwe(1, false);
                this.graphControl.VisibleGraphViwe(2, false);
            }
            else
            {
                this.graphControl.VisibleGraphViwe(0, true);
                this.graphControl.VisibleGraphViwe(1, true);
                this.graphControl.VisibleGraphViwe(2, true);
            }

            // グラフの高さ割合設定
            for (int index = 0; index < combos.Length; index++)
            {
                if (combos[index].Text.Equals("Hide"))
                {
                    this.graphControl.DisplayGraphView(index, 0);
                }
                else
                {
                    this.graphControl.DisplayGraphView(index, 100F / countVisible);
                }
            }

            Tracer.WriteInformation("Change chart.  chart1:{0}  chart2:{1}  chart3:{2}", combos[0].Text, combos[1].Text, combos[2].Text);

            // チェックOKなら
            graphControl.ClearChartIndex();
            graphControl.Chart1Axis = (AxisBean)chart1Select.SelectedValue;
            graphControl.Chart2Axis = (AxisBean)chart2Select.SelectedValue;
            graphControl.Chart3Axis = (AxisBean)chart3Select.SelectedValue;

            // 最初に強制移動する
            double min = 0, max = 0, range = 0;
            this.graphControl.GetXAxisGraphScale(ref min, ref max);
            range = max - min;
            this.OnChangeGraphPoint(0, 0 - (range * 0.1), range - (range * 0.1));
        }

        /// <summary>
        /// 測定データ受信スレッド
        /// </summary>
        private void recvMeasureData()
        {
            int receiveInterval = Constants.RECEIVE_PROC_INTERVAL;
            while (this.measureDataThreadLoop)
            {
                List<MeasureBean> sensorDataList = this.dataManager.GetSensorData();

                if (sensorDataList != null)
                {
                    try
                    {
                        // 排他する
                        graphMutex.WaitOne();

                        this.file.AddSensorData(sensorDataList);

                        List<MeasureBean> accList = new List<MeasureBean>();
                        List<MeasureBean> gyroList = new List<MeasureBean>();
                        List<MeasureBean> eogList = new List<MeasureBean>();
                        // TODO : クォータニオン除外
                        //List<MeasureBean> quaList = new List<MeasureBean>();
                        MeasureBean quatBean;

                        switch (this.dataManager.DeviceMode)
                        {
                            case 1:     // アカデミアデータ１：加速度、眼電位
                                foreach (MeasureBean bean in sensorDataList)
                                {
                                    // TODO : マーキング０データ表示を消去
                                    //if (bean.Marking)
                                    //{
                                    //    accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0 }));
                                    //    eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0, 0 }));
                                    //}
                                    //else
                                    //{
                                        accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[0] - this.accXAxisOffset,
                                                                                bean.Y[1] - this.accYAxisOffset,
                                                                                bean.Y[2] - this.accZAxisOffset }));
                                        eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[5], bean.Y[6], bean.Y[8], bean.Y[10] }));
                                    //}
                                }
                                graphControl.AddAccelerationData(accList);
                                graphControl.AddElectrooculographyData(eogList);
                                break;

                            case 2:     // アカデミアデータ２：加速度、角速度、眼電位
                                foreach (MeasureBean bean in sensorDataList)
                                {
                                    // TODO : マーキング０データ表示を消去
                                    //if (bean.Marking)
                                    //{
                                    //    accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0 }));
                                    //    gyroList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0 }));
                                    //    eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0, 0 }));
                                    //}
                                    //else
                                    //{
                                        accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[0] - this.accXAxisOffset,
                                                                                bean.Y[1] - this.accYAxisOffset,
                                                                                bean.Y[2] - this.accZAxisOffset }));
                                        gyroList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                        new int[] { bean.Y[3], bean.Y[4], bean.Y[5] }));
                                        eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[6], bean.Y[7], bean.Y[8], bean.Y[9] }));
                                    //}
                                }
                                graphControl.AddAccelerationData(accList);
                                graphControl.AddAngularVelocityData(gyroList);
                                graphControl.AddElectrooculographyData(eogList);
                                break;

                            // クォータニオン3D姿勢表示
                            case 3:     // アカデミアデータ３：クォータニオン
                                // 最後のデータのみ
                                quatBean = sensorDataList.Last();
                                long[] quat;
                                // TODO : マーキング０データ表示を消去
                                //if (quatBean.Marking)
                                //{
                                //    quat = new long[] { 0, 0, 0, 0 };
                                //}
                                //else
                                //{
                                    quat = new long[] { quatBean.Y[0], quatBean.Y[1], quatBean.Y[2], quatBean.Y[3] };
                                //}

                                // 3D姿勢表示
                                if (postureForm != null && !postureForm.IsDisposed && this.measureDataThreadLoop)
                                {
                                    postureForm.Draw(quat);
                                }
                                break;

                            // TODO : クォータニオン除外
                            //case 3:     // アカデミアデータ３：クォータニオン
                            //    foreach (MeasureBean bean in sensorDataList)
                            //    {
                            //        quaList.Add(new MeasureBean(bean.X, new int[] { bean.Y[0], bean.Y[1], bean.Y[2], bean.Y[3] }));
                            //    }
                            //    graphControl.AddQuaternionData(quaList);
                            //    break;
                        }
                        graphControl.ShowElapsed((long)sensorDataList.Last().X);

                    }
                    catch (Exception ex)
                    {
                        Tracer.WriteException(ex);
                    }
                    finally
                    {
                        // Mutexロック解放
                        graphMutex.ReleaseMutex();
                    }
                }
                Thread.Sleep(receiveInterval);
            }
        }

        /// <summary>
        /// バージョンメニュークリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void versionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VersionForm form = new VersionForm();
            form.ShowDialog();
        }

        /// <summary>
        /// データ種別タブ切り替え
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void controlPanelTab_SelectedIndexChanged(object sender, EventArgs e)
        {
            // chart選択コントロールの親変更
            this.chartGroupBox.Parent = this.controlPanelTab.SelectedTab;
        }

        /// <summary>
        /// 設定メニュークリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void settingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingForm form = new SettingForm(this.settings);
            if (form.ShowDialog() == DialogResult.OK)
            {
                this.file.SaveFolder = form.SaveFolder;
                this.file.UseSocket = form.UseSocket;
                this.file.SocketPort = form.SocketPort;
                this.file.SocketAddress = form.SocketAddress;
                this.file.RecordFileDateFormat = form.RecordFileDateFormat;
                this.dataManager.FreeMarkingTime = form.MarkingTimeValue;
                this.accXAxisOffset = form.AccXAxisOffset;
                this.accYAxisOffset = form.AccYAxisOffset;
                this.accZAxisOffset = form.AccZAxisOffset;
                this.showSaveFileDialog = form.ShowSaveFileDialog;

                if (form.UseSocket == false)
                {
                    this.socketIPAddressLabel.Text = "IP address : ";
                    this.socketPortLabel.Text = "Port : ";
                    this.socketStatusLabel.Text = "Status : Disable";
                }
                else
                {
                    this.socketIPAddressLabel.Text = string.Format("IP address : {0}", form.SocketAddress);
                    this.socketPortLabel.Text = string.Format("Port : {0}", form.SocketPort);
                }
            }
        }

        /// <summary>
        /// バッテリーレベル表示タイマー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void batteryLevelTimer_Tick(object sender, EventArgs e)
        {
            this.setBatteryLevel(this.dataManager.BatteryLevel);
        }

        /// <summary>
        /// バッテリーレベル設定
        /// </summary>
        /// <param name="level"></param>
        private void setBatteryLevel(int level)
        {
            if (level == 0)
            {
                this.batteryLevelProgress.Tag = 1;
                level = this.batteryChargeCount++;
                if (this.batteryChargeCount > 5)
                {
                    this.batteryChargeCount = 0;
                }
            }
            else
            {
                this.batteryLevelProgress.Tag = 0;
                this.batteryChargeCount = 0;
            }
            this.batteryLevelProgress.Value = level;
        }

        /// <summary>
        /// バッテリー充電カウント
        /// </summary>
        private int batteryChargeCount = 0;

        /// <summary>
        /// バッテリーレベルプログレスの描画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void batteryLevelProgress_Paint(object sender, PaintEventArgs e)
        {
            Brush brush = Brushes.Lime;
            int value = this.batteryLevelProgress.Value;
            if ((int)this.batteryLevelProgress.Tag == 1)
            {
                ;
            }
            else if (value <= 1)
            {
                brush = Brushes.Red;
            }
            else if (value <= 3)
            {
                brush = Brushes.Orange;
            }

            // 内側を描画
            double percent = (double)value / this.batteryLevelProgress.Maximum;
            int valueLength = Convert.ToInt32(percent * (this.batteryLevelProgress.ProgressBar.Width - 2));
            Rectangle progressRect = new Rectangle(1, 1, valueLength, (this.batteryLevelProgress.Height - 2));
            e.Graphics.FillRectangle(brush, progressRect);

            // 外枠を描画
            Pen pen = new Pen(Color.LightGray, 2);
            e.Graphics.DrawRectangle(pen, 0, 0, this.batteryLevelProgress.Width, this.batteryLevelProgress.Height);
            pen.Dispose();
        }

        /// <summary>
        /// 通信成功率プログレスの描画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void successRateProgress_Paint(object sender, PaintEventArgs e)
        {
            Brush brush = Brushes.Lime;
            int value = this.successRateProgress.Value;

            // 内側を描画
            double percent = (double)value / this.successRateProgress.Maximum;
            int valueLength = Convert.ToInt32(percent * (this.successRateProgress.Width - 2));
            Rectangle progressRect = new Rectangle(1, 1, valueLength, (this.successRateProgress.Height - 2));
            e.Graphics.FillRectangle(brush, progressRect);

            // 外枠を描画
            Pen pen = new Pen(Color.LightGray, 2);
            e.Graphics.DrawRectangle(pen, 0, 0, this.successRateProgress.Width, this.successRateProgress.Height);
            pen.Dispose();

            // 文字列を描画する
            string displayText = string.Format("{0:0.000} %", this.successRateProgressValue);
            TextFormatFlags textFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(e.Graphics, displayText, this.Font, this.successRateProgress.ProgressBar.ClientRectangle, SystemColors.ControlText, textFormat);
        }

        /// <summary>
        /// 通信エラー（過渡損失）プログレスの描画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void transientDataLossProgress_Paint(object sender, PaintEventArgs e)
        {
            //string displayText = string.Empty;

            Brush brush = null;
            Color textColor;
            double value = this.transientDataLossProgressValue;
            if (value == 100)
            {
                brush = Brushes.Lime;
                textColor = Color.Black;
                //displayText = "";
            }
            else if (value > 70)
            {
                brush = Brushes.Yellow;
                textColor = Color.Black;
                //displayText = "CUATION";
            }
            else
            {
                brush = Brushes.Red;
                textColor = Color.White;
                //displayText = "WARNING";
            }

            // 内側を描画
            //double percent = (double)value / this.transientDataLossProgress.Maximum;
            double percent = 100;
            int valueLength = Convert.ToInt32(percent * (this.transientDataLossProgress.Width - 2));
            Rectangle progressRect = new Rectangle(1, 1, valueLength, (this.transientDataLossProgress.Height - 2));
            e.Graphics.FillRectangle(brush, progressRect);

            // 外枠を描画
            Pen pen = new Pen(Color.LightGray, 2);
            e.Graphics.DrawRectangle(pen, 0, 0, this.transientDataLossProgress.Width, this.transientDataLossProgress.Height);
            pen.Dispose();

            // 文字列を描画する
            string displayText = string.Format("{0:0.000} %", this.transientDataLossProgressValue);
            TextFormatFlags textFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(e.Graphics, displayText, this.Font, this.transientDataLossProgress.ProgressBar.ClientRectangle, textColor, textFormat);
        }

        /// <summary>
        /// データフォルダーボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void setDataFolderButton_Click(object sender, EventArgs e)
        {
            this.folderBrowserDialog.Description = "Select sensor history data folder.";
            this.folderBrowserDialog.SelectedPath = this.historyDataFolder;
            this.folderBrowserDialog.ShowNewFolderButton = false;
            if (this.folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Tracer.WriteInformation("Select sensor history data folder. {0}", this.folderBrowserDialog.SelectedPath);
                this.historyDataFolder = this.folderBrowserDialog.SelectedPath;
                this.seekHistoryData();
                this.changeDisplayStatus(DISPLAY_STATUS.LOST_HISTORY_FILE);
            }
        }

        /// <summary>
        /// 履歴データ検索
        /// </summary>
        private void seekHistoryData()
        {
            this.previousFileList.Items.Clear();
            string[] files = Directory.GetFiles(this.historyDataFolder, "*.csv");
            foreach (string filePath in files)
            {
                this.previousFileList.Items.Add(Path.GetFileName(filePath));
            }
        }

        /// <summary>
        /// 履歴データ選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void previousFileList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.previousFileList.SelectedItem == null)
            {
                return;
            }
            
            string filePath = this.historyDataFolder + "\\" + this.previousFileList.SelectedItem.ToString();

            Cursor.Current = Cursors.WaitCursor;

            string mode = string.Empty, speed = string.Empty, accRange = string.Empty, gyroRange = string.Empty;
            DateTime fromDateTime = new DateTime(), toDateTime = new DateTime();
            if (this.file.GetHistoryFileInfo(filePath, ref mode, ref speed, ref accRange, ref gyroRange, ref fromDateTime, ref toDateTime))
            {
                this.historyFileModeLabel.Text = string.Format("Data mode  :  {0}", mode);
                this.historyTransmissionSpeedLabel.Text = string.Format("Transmission speed  :  {0}", speed);
                this.historyFileAccRangeLabel.Text = string.Format("Accelerometer sensor's range  :  {0}", accRange);
                this.historyFileGyroRangeLabel.Text = string.Format("Gyroscope sensor's range  :  {0}", gyroRange);
                this.fromDatetimeText.Text = fromDateTime.ToShortDateString() + " " +
                                                string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", fromDateTime.Hour, fromDateTime.Minute, fromDateTime.Second, fromDateTime.Millisecond);
                this.toDatetimeText.Text = toDateTime.ToShortDateString() + " " +
                                            string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", toDateTime.Hour, toDateTime.Minute, toDateTime.Second, toDateTime.Millisecond);

                Tracer.WriteInformation("Select file path:{0}", this.file.HistoryFilePath);
                Tracer.WriteInformation(this.historyFileModeLabel.Text);
                Tracer.WriteInformation(this.historyTransmissionSpeedLabel.Text);
                Tracer.WriteInformation(this.historyFileAccRangeLabel.Text);
                Tracer.WriteInformation(this.historyFileGyroRangeLabel.Text);
                Tracer.WriteInformation("From date time:{0}", this.fromDatetimeText.Text);
                Tracer.WriteInformation(" To  date time:{0}", this.toDatetimeText.Text);

                this.changeDisplayStatus(DISPLAY_STATUS.FOUND_HISTORY_FILE);
            }
            else
            {
                this.changeDisplayStatus(DISPLAY_STATUS.LOST_HISTORY_FILE);
            }

            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Startボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void replayStartButton_Click(object sender, EventArgs e)
        {
            Tracer.WriteInformation("Push [start] button.  (replay)");
            
            this.graphControl.ClearElapsed();
            this.graphControl.ClearAccelerationData();
            this.graphControl.ClearAngularVelocityData();
            this.graphControl.ClearElectrooculographyData();
            this.graphControl.ClearQuaternionData();
            this.graphControl.ScrollReset();
            this.graphControl.GraphOperation = true;
            this.graphControl.DataEraseMode = true;
            this.displayMeasureData = false;

            string result = this.file.ReplayStart(1 << this.replaySpeedCombo.SelectedIndex, this.fromDatetimeText.Text, this.toDatetimeText.Text);
            if (result.Equals(string.Empty) == false)
            {
                Common.ShowMessageBox(result, "Replay Start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Tracer.WriteInformation("Start replay.  Speed:{0}", this.replaySpeedCombo.Text);

                // 2015.4 openGLでクォータニオン表示する
                // モード３のとき姿勢表示画面を表示
                string filePath = this.historyDataFolder + "\\" + this.previousFileList.SelectedItem.ToString();
                string mode = string.Empty, speed = string.Empty, accRange = string.Empty, gyroRange = string.Empty;
                DateTime fromDateTime = new DateTime(), toDateTime = new DateTime();
                if (this.file.GetHistoryFileInfo(filePath, ref mode, ref speed, ref accRange, ref gyroRange, ref fromDateTime, ref toDateTime))
                {
                    if (mode == "Quaternion")
                    {
                        if (postureForm == null || postureForm.IsDisposed)
                        {
                            postureForm = new PostureForm();
                        }
                        // TODO : OpenGL表示を暫定非表示
                        //postureForm.BringToFront();
                        //postureForm.Visible = true;
                    }
                }
                replayMode = mode;


                // データ表示スレッド起動
                this.measureDataThreadLoop = true;
                this.measureDataThread = new Thread(new ThreadStart(this.recvFileData));
                this.measureDataThread.Name = "Measure data thread (File)";
                this.measureDataThread.Start();

                this.changeDisplayStatus(DISPLAY_STATUS.START_REPLAY);
            }
        }

        /// <summary>
        /// ファイルデータ受信スレッド
        /// </summary>
        private void recvFileData()
        {
            int receiveInterval = Constants.RECEIVE_PROC_INTERVAL;
            while (this.measureDataThreadLoop)
            {
                List<MeasureBean> sensorDataList = this.file.GetFileData();

                if (sensorDataList.Count != 0)
                {
                    try
                    {
                        // 排他する
                        graphMutex.WaitOne();

                        List<MeasureBean> accList = new List<MeasureBean>();
                        List<MeasureBean> gyroList = new List<MeasureBean>();
                        List<MeasureBean> eogList = new List<MeasureBean>();
                        // TODO : クォータニオン除外
                        //List<MeasureBean> quaList = new List<MeasureBean>();
                        MeasureBean quatBean;

                        switch (this.file.HistoryDataMode)
                        {
                            case 1:     // アカデミアデータ１：加速度、眼電位
                                foreach (MeasureBean bean in sensorDataList)
                                {
                                    // TODO : マーキング０データ表示を消去
                                    //if (bean.Marking)
                                    //{
                                    //    accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0 }));
                                    //    eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0, 0 }));
                                    //}
                                    //else
                                    //{
                                        accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[0] - this.accXAxisOffset,
                                                                            bean.Y[1] - this.accYAxisOffset,
                                                                            bean.Y[2] - this.accZAxisOffset }));
                                        eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[5], bean.Y[6], bean.Y[8], bean.Y[10] }));
                                    //}
                                }
                                graphControl.AddAccelerationData(accList);
                                graphControl.AddElectrooculographyData(eogList);
                                break;

                            case 2:     // アカデミアデータ２：加速度、角速度、眼電位
                                foreach (MeasureBean bean in sensorDataList)
                                {
                                    // TODO : マーキング０データ表示を消去
                                    //if (bean.Marking)
                                    //{
                                    //    accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0 }));
                                    //    gyroList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0 }));
                                    //    eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0, 0 }));
                                    //}
                                    //else
                                    //{
                                        accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[0] - this.accXAxisOffset,
                                                                            bean.Y[1] - this.accYAxisOffset,
                                                                            bean.Y[2] - this.accZAxisOffset }));
                                        gyroList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                        new int[] { bean.Y[3], bean.Y[4], bean.Y[5] }));
                                        eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[6], bean.Y[7], bean.Y[8], bean.Y[9] }));
                                    //}
                                }
                                graphControl.AddAccelerationData(accList);
                                graphControl.AddAngularVelocityData(gyroList);
                                graphControl.AddElectrooculographyData(eogList);
                                break;

                            // クォータニオン3D姿勢表示
                            case 3:     // アカデミアデータ３：クォータニオン
                                // 最後のデータのみ
                                quatBean = sensorDataList.Last();
                                long[] quat;
                                // TODO : マーキング０データ表示を消去
                                //if (quatBean.Marking)
                                //{
                                //    quat = new long[] { 0, 0, 0, 0 };
                                //}
                                //else
                                //{
                                    quat = new long[] { quatBean.Y[0], quatBean.Y[1], quatBean.Y[2], quatBean.Y[3] };
                                //}

                                // 3D姿勢表示
                                if (postureForm != null && !postureForm.IsDisposed)
                                {
                                    postureForm.Draw(quat);
                                }
                                break;

                            // TODO : クォータニオン除外
                            //case 3:     // アカデミアデータ３：クォータニオン
                            //    foreach (MeasureBean bean in sensorDataList)
                            //    {
                            //        quaList.Add(new MeasureBean(bean.X, new int[] { bean.Y[0], bean.Y[1], bean.Y[2], bean.Y[3] }));
                            //    }
                            //    graphControl.AddQuaternionData(quaList);
                            //    break;
                        }
                        graphControl.ShowElapsed((long)sensorDataList.Last().X);
                    }
                    catch (Exception ex)
                    {
                        Tracer.WriteException(ex);
                    }
                    finally
                    {
                        // Mutexロック解放
                        graphMutex.ReleaseMutex();
                    }
                }
                Thread.Sleep(receiveInterval);
            }
        }

        /// <summary>
        /// ソケット状態通知
        /// </summary>
        /// <param name="status"></param>
        private void OnSocketStatus(string status)
        {
            if (this.InvokeRequired)
            {
                Invoke(new FileAccess.SocketStatusHandler(this.OnSocketStatus), new object[] { status });
                return;
            }

            this.socketStatusLabel.Text = string.Format("Status : {0}", status);
        }

        /// <summary>
        /// ファイル読込み進捗通知
        /// </summary>
        /// <param name="percent"></param>
        private void OnFileReadProgress(int percent)
        {
            if (this.InvokeRequired)
            {
                Invoke(new FileAccess.FileReadProgressEventHandler(OnFileReadProgress), new object[] { percent });
                return;
            }

            this.fileReadProgress.Value = percent;
        }

        /// <summary>
        /// ファイルデータ終了通知
        /// </summary>
        /// <param name="sender"></param>
        private void OnEndFileData()
        {
            if (this.InvokeRequired)
            {
                Invoke(new FileAccess.EndFileDataEventHandler(OnEndFileData));
                return;
            }

            this.changeDisplayStatus(DISPLAY_STATUS.FOUND_HISTORY_FILE);

            this.measureDataThreadLoop = false;
            if (this.measureDataThread != null)
            {
                this.measureDataThread.Join(500);
                this.measureDataThread = null;
            }

            this.graphControl.GraphOperation = true;
            this.graphControl.DataEraseMode = false;

            if (this.file.PlotData)
            {
                // 最後に強制移動する
                MeasureBean lastBean = this.file.GetLastFileData();
                if (lastBean != null)
                {
                    double min = 0 , max = 0, lastX, range;
                    this.graphControl.GetXAxisGraphScale(ref min, ref max);
                    range = max - min;
                    lastX = lastBean.X / (double)1000;
                    this.OnChangeGraphPoint(0, lastX - range + (range * 0.1), lastX + (range * 0.1));
                }
            }

            // 2015.4 openGLでクォータニオン表示する
            // モード３のとき姿勢表示画面を表示
            if (replayMode == "Quaternion")
            {
                if (postureForm != null && !postureForm.IsDisposed)
                {
                    postureForm.Close();
                }
            }

        }

        /// <summary>
        /// Pause/Resumeボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void replayPauseButton_Click(object sender, EventArgs e)
        {
            if (this.displayStatus == DISPLAY_STATUS.START_REPLAY)
            {
                Tracer.WriteInformation("Push [pause] button.  (replay)");
                this.file.RelpayPause(true);
                this.changeDisplayStatus(DISPLAY_STATUS.PAUSE_REPLAY);
                this.graphControl.GraphOperation = true;
                this.graphControl.DataEraseMode = false;
            }
            else
            {
                Tracer.WriteInformation("Push [resume] button.  (replay)");
                this.file.RelpayPause(false);
                this.changeDisplayStatus(DISPLAY_STATUS.START_REPLAY);
                this.graphControl.GraphOperation = true;
                this.graphControl.DataEraseMode = true;

                if (postureForm != null && !postureForm.IsDisposed)
                {
                    postureForm.BringToFront();
                }

            }
        }

        /// <summary>
        /// Stopボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void replayStopButton_Click(object sender, EventArgs e)
        {
            Tracer.WriteInformation("Push [stop] button.  (replay)");
            this.measureDataThreadLoop = false;
            this.file.ReplayStop();

        }

        /// <summary>
        /// Plotボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void replayPlotButton_Click(object sender, EventArgs e)
        {
            Tracer.WriteInformation("Push [plot] button.  (replay)");
            
            this.changeDisplayStatus(DISPLAY_STATUS.PLOT_REPLAY);

            this.file.PlotData = true;
        }

        /// <summary>
        /// グラフ位置変更通知
        /// </summary>
        /// <param name="graphNumber"></param>
        /// <param name="axisXMin"></param>
        /// <param name="axisXMax"></param>
        private void OnChangeGraphPoint(int graphNumber, double axisXMin, double axisXMax)
        {
            if (this.graphControl.GraphOperation == false)
            {
                return;
            }

            Tracer.WriteVerbose("Graph:{0}  Min:{1}  Max:{2}", graphNumber, axisXMin, axisXMax);

            double scaleXMax = 0;
            List<MeasureBean> sensorDataList = null;
            int dataMode;
            if (this.displayMeasureData)
            {
                dataMode = this.dataManager.DeviceMode;
                if (dataMode != 0)
                {
                    sensorDataList = this.dataManager.GetSensorHistory(Convert.ToInt32(axisXMin * 1000), Convert.ToInt32(axisXMax * 1000), ref scaleXMax);
                }
            }
            else
            {
                dataMode = this.file.HistoryDataMode;
                if (dataMode != 0)
                {
                    sensorDataList = this.file.GetSensorHistory(Convert.ToInt32(axisXMin * 1000), Convert.ToInt32(axisXMax * 1000), ref scaleXMax);
                }
            }
            if ((sensorDataList != null) && (sensorDataList.Count != 0))
            {
                    try
                    {
                        // 排他する
                        graphMutex.WaitOne();

                        this.graphControl.ClearElapsed();
                        this.graphControl.ClearAccelerationData();
                        this.graphControl.ClearAngularVelocityData();
                        this.graphControl.ClearElectrooculographyData();
                        this.graphControl.ClearQuaternionData();

                        List<MeasureBean> accList = new List<MeasureBean>();
                        List<MeasureBean> gyroList = new List<MeasureBean>();
                        List<MeasureBean> eogList = new List<MeasureBean>();
                        // TODO : クォータニオン除外
                        //List<MeasureBean> quaList = new List<MeasureBean>();

                        switch (dataMode)
                        {
                            case 1:     // アカデミアデータ１：加速度、眼電位
                                foreach (MeasureBean bean in sensorDataList)
                                {
                                    // TODO : マーキング０データ表示を消去
                                    //if (bean.Marking)
                                    //{
                                    //    accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0 }));
                                    //    eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0, 0 }));
                                    //}
                                    //else
                                    //{
                                        accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[0] - this.accXAxisOffset,
                                                                                bean.Y[1] - this.accYAxisOffset,
                                                                                bean.Y[2] - this.accZAxisOffset }));
                                        eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[5], bean.Y[6], bean.Y[8], bean.Y[10] }));
                                    //}
                                }
                                graphControl.AddAccelerationData(accList);
                                graphControl.AddElectrooculographyData(eogList);
                                break;

                            case 2:     // アカデミアデータ２：加速度、角速度、眼電位
                                foreach (MeasureBean bean in sensorDataList)
                                {
                                    // TODO : マーキング０データ表示を消去
                                    //if (bean.Marking)
                                    //{
                                    //    accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0 }));
                                    //    gyroList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0 }));
                                    //    eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X, new int[] { 0, 0, 0, 0 }));
                                    //}
                                    //else
                                    //{
                                        accList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[0] - this.accXAxisOffset,
                                                                                bean.Y[1] - this.accYAxisOffset,
                                                                                bean.Y[2] - this.accZAxisOffset }));
                                        gyroList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                        new int[] { bean.Y[3], bean.Y[4], bean.Y[5] }));
                                        eogList.Add(new MeasureBean(bean.Marking, bean.SerialNumber, bean.X,
                                                                    new int[] { bean.Y[6], bean.Y[7], bean.Y[8], bean.Y[9] }));
                                    //}
                                }
                                graphControl.AddAccelerationData(accList);
                                graphControl.AddAngularVelocityData(gyroList);
                                graphControl.AddElectrooculographyData(eogList);
                                break;

                            // TODO : クォータニオン除外
                            //case 3:     // アカデミアデータ３：クォータニオン
                            //    foreach (MeasureBean bean in sensorDataList)
                            //    {
                            //        quaList.Add(new MeasureBean(bean.X, new int[] { bean.Y[0], bean.Y[1], bean.Y[2], bean.Y[3] }));
                            //    }
                            //    graphControl.AddQuaternionData(quaList);
                            //    break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Tracer.WriteException(ex);
                    }
                    finally
                    {
                        // Mutexロック解放
                        graphMutex.ReleaseMutex();
                    }
                    //graphControl.ShowElapsed((long)sensorDataList.Last().X);
                }

            // グラフ表示範囲設定
            double spaceValue = (axisXMax - axisXMin) / 10;
            double axisXScrollMin = 0 - spaceValue, axisXScrollMax = scaleXMax + spaceValue;
            if (axisXMin < axisXScrollMin)
            {
                axisXScrollMin = axisXMin;
            }            
            if(axisXMax > axisXScrollMax)
            {
                axisXScrollMax = axisXMax;
            }
            this.graphControl.ChangeXAxisGraphScale(axisXMin, axisXMax, axisXScrollMin, axisXScrollMax);
        }

        /// <summary>
        /// モード変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void modeSelectCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 角速度レンジ変更
            this.changeAngularRange();
        }

        /// <summary>
        /// 角速度レンジ変更
        /// </summary>
        private void changeAngularRange()
        {
            if (this.modeSelectCombo.SelectedIndex == 2)
            {
                // クォータニオン計測（Mode3）の場合は、角速度レンジを2000dpsに変更する。
                this.gyroscopeRangeCombo.SelectedIndex = 3;
            }
        }

        /// <summary>
        /// 通信成功／エラー率通知
        /// </summary>
        /// <param name="successRate"></param>
        /// <param name="transientDataLoss"></param>
        private void OnErrorRate(double successRate, double transientDataLoss)
        {
            if (this.InvokeRequired)
            {
                Invoke(new DataManager.ErrorRateHandler(OnErrorRate), new object[] { successRate, transientDataLoss });
                return;
            }

            this.successRateProgressValue = successRate;
            this.transientDataLossProgressValue = transientDataLoss;

            this.successRateProgress.Value = Convert.ToInt32(successRate);
            this.transientDataLossProgress.Value = Convert.ToInt32(transientDataLoss);
        }
    }
}
