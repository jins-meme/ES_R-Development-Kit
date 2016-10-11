using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using JINS_MEME_DataLogger.Data;
using System.Timers;
using System.Diagnostics;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// データ管理クラスです。
    /// </summary>
    /// <remarks>センサーからのデータを画面処理等に渡します。</remarks>
    public class DataManager
    {
        /// <summary>
        /// ComPort検索進捗状況イベント定義
        /// </summary>
        /// <param name="progress"></param>
        public delegate void ScanComPortProgressHandler(int progress);
        /// <summary>
        /// ComPort検索進捗状況イベント
        /// </summary>
        public event ScanComPortProgressHandler ScanComPortProgressEvent = null;

        /// <summary>
        /// ドングル接続通知イベント定義
        /// </summary>
        /// <param name="status"></param>
        public delegate void DongleConnectHandler(byte status, string dongleVersion);
        /// <summary>
        /// ドングル接続通知イベント
        /// </summary>
        public event DongleConnectHandler DongleConnectEvent = null;

        /// <summary>
        /// Bluetooth MACアドレス通知イベント定義
        /// </summary>
        /// <param name="macAddress"></param>
        public delegate void BluetoothMacAddressHandler(string macAddress);
        /// <summary>
        /// Bluetooth MACアドレス通知イベント
        /// </summary>
        public event BluetoothMacAddressHandler BluetoothMacAddressEvent = null;

        /// <summary>
        /// Bluetooth接続通知イベント定義
        /// </summary>
        /// <param name="status"></param>
        public delegate void BluetoothConnectHandler(byte status);
        /// <summary>
        /// Bluetooth接続通知イベント
        /// </summary>
        public event BluetoothConnectHandler BluetoothConnectEvent = null;

        /// <summary>
        /// デバイス初期化完了通知イベント定義
        /// </summary>
        public delegate void DeviceInitializeHandler(byte status);
        /// <summary>
        /// デバイス初期化完了通知イベント
        /// </summary>
        public event DeviceInitializeHandler DeviceInitializeEvent = null;

        /// <summary>
        /// 測定開始通知イベント定義
        /// </summary>
        /// <param name="status"></param>
        public delegate void MeasureStartHandler(byte status);
        /// <summary>
        /// 測定開始通知イベント
        /// </summary>
        public event MeasureStartHandler MeasureStartEvent = null;

        /// <summary>
        /// 測定停止通知イベント定義
        /// </summary>
        /// <param name="status"></param>
        public delegate void MeasureStopHandler(byte status);
        /// <summary>
        /// 測定停止通知イベント
        /// </summary>
        public event MeasureStopHandler MeasureStopEvent = null;

        /// <summary>
        /// フリーマーキング終了通知イベント定義
        /// </summary>
        public delegate void FreeMarkingEndHandler();
        /// <summary>
        /// フリーマーキング終了通知イベント
        /// </summary>
        public event FreeMarkingEndHandler FreeMarkingEndEvent = null;

        /// <summary>
        /// エラー率通知イベント定義
        /// </summary>
        /// <param name="accumulatedDataLoss"></param>
        /// <param name="transientDataLoss"></param>
        public delegate void ErrorRateHandler(double accumulatedDataLoss, double transientDataLoss);
        /// <summary>
        /// エラー率通知イベント
        /// </summary>
        public event ErrorRateHandler ErrorRateEvent = null;


        /// <summary>
        /// 読込みデータモード定義
        /// </summary>
        public enum DefReadDataMode
        {
            Serial,
            File,
        };

        /// <summary>
        /// 読込みデータモード
        /// </summary>
        public DefReadDataMode ReadDataMode
        {
            get;
            set;
        }

        /// <summary>
        /// Comポート名取得
        /// </summary>
        public List<string> ComPortNameList
        {
            get { return this.com.GetComPortNameList(); }
        }

        /// <summary>
        /// Comポート接続状態取得
        /// </summary>
        public bool ComPortConnectStatus
        {
            get { return this.com.IsSerialConnect; }
        }

        /// <summary>
        /// バッテリー残量
        /// </summary>
        private int batteryLevel = 0x00;
        public int BatteryLevel
        {
            get { return this.batteryLevel; }
        }

        /// <summary>
        /// ファームウェアバージョン
        /// </summary>
        private string firmwareVersion = string.Empty;
        public string FirmwareVersion
        {
            get { return this.firmwareVersion; }
        }

        /// <summary>
        /// デバイス動作モード
        /// </summary>
        private byte deviceMode = 0;
        public byte DeviceMode
        {
            get { return this.deviceMode; }
        }

        /// <summary>
        /// 送信速度（データ品質）
        /// </summary>
        private byte transmissionSpeed = 0;
        public byte TransmissionSpeed
        {
            get { return this.transmissionSpeed; }
        }

        /// <summary>
        /// 加速度センサ測定レンジ
        /// </summary>
        private byte accelerationSensorRange = 0;
        public byte AccelerationSensorRange
        {
            get { return this.accelerationSensorRange; }
        }

        /// <summary>
        /// 角速度センサ測定レンジ
        /// </summary>
        private byte gyroSensorRange = 0;
        public byte GyroSensorRange
        {
            get { return this.gyroSensorRange; }
        }

        /// <summary>
        /// 測定状態
        /// </summary>
        private bool measureStatus;
        public bool MeasureStatus
        {
            get { return this.measureStatus; }
        }

        /// <summary>
        /// マーキング時間（設定値）
        /// </summary>
        public int FreeMarkingTime
        {
            get;
            set;
        }

        /// <summary>
        /// マーキング状態
        /// </summary>
        private bool freeMarkingStatus = false;

        /// <summary>
        /// マーキング終了時間
        /// </summary>
        private DateTime freeMarkingEndTime = DateTime.Now;

        /// <summary>
        /// センサーデータ連番
        /// </summary>
        private long sensorDataSerialNumber = 0;


        /// <summary>
        /// シリアル通信クラス
        /// </summary>
        private Communication com = new Communication();

        /// <summary>
        /// データアクセスMutex
        /// </summary>
        private static Mutex measureDataMutex = new Mutex();

        /// <summary>
        /// センサーデータ保持リスト
        /// </summary>
        private List<SensorDataArgs> sensorDataList = null;

        /// <summary>
        /// センサーデータ履歴リスト
        /// </summary>
        private List<MeasureBean> sensorHistoryList = new List<MeasureBean>();

        /// <summary>
        /// 経過時間
        /// </summary>
        private long timeElapsed = 0;

        /// <summary>
        /// 前回のタイムカウンタ値
        /// </summary>
        private int preTimeCounter = -1;

        /// <summary>
        /// サンプリングレート
        /// </summary>
        private int samplingRate = 0;

        /// <summary>
        /// 計測モード
        /// </summary>
        private byte measureMode = 0;
        public byte MeasureMode
        {
            get { return this.measureMode; }
        }

        /// <summary>
        /// 計測送信速度（データ品質）
        /// </summary>
        private byte measureTransmissionSpeed = 0;
        public byte MeasureTransmissionSpeed
        {
            get { return this.measureTransmissionSpeed; }
        }

        /// <summary>
        /// 計測加速度レンジ
        /// </summary>
        private byte measureAccRange = 0;
        public byte MeasureAccRange
        {
            get { return this.measureAccRange; }
        }

        /// <summary>
        /// 計測角速度レンジ
        /// </summary>
        private byte measureGyroRange = 0;
        public byte MeasureGyroRange
        {
            get { return this.measureGyroRange; }
        }



        /// <summary>
        /// ACK待ちコマンド
        /// </summary>
        private enum ACK_WAIT_COMMAND
        {
            /// <summary>
            /// 待機
            /// </summary>
            WAITING,
            /// <summary>
            /// パラメータ初期化
            /// </summary>
            CLEAR_PARAMS,
            /// <summary>
            /// モード設定
            /// </summary>
            SET_MODE,
            /// <summary>
            /// パラメータ設定
            /// </summary>
            SET_6AXIS_PARAMS,
            /// <summary>
            /// 測定開始
            /// </summary>
            START_MEASURE,
            /// <summary>
            /// 測定停止
            /// </summary>
            STOP_MEASURE,
        }

        /// <summary>
        /// ACK待ちコマンド
        /// </summary>
        private ACK_WAIT_COMMAND ackWaitCommand = ACK_WAIT_COMMAND.WAITING;

        /// <summary>
        /// コマンドレスポンスタイマー
        /// </summary>
        private System.Timers.Timer commandResponseTimer = new System.Timers.Timer();

        /// <summary>
        /// シーケンスステータス
        /// </summary>
        private enum SEQUENCE_STATUS
        {
            /// <summary>
            /// 待機
            /// </summary>
            WAITING,
            /// <summary>
            /// 初期切断
            /// </summary>
            DISCONNECT_INITIALIZE,
            /// <summary>
            /// ドングル名取得
            /// </summary>
            GET_DONGLE_NAME,
            /// <summary>
            /// ドングルバージョン取得
            /// </summary>
            GET_DONGLE_VERSION,
            /// <summary>
            /// Bluetooth検索
            /// </summary>
            SCAN_BLUETOOTH,
            /// <summary>
            /// Bluetooth接続
            /// </summary>
            CONNECT_BLUETOOTH,
            /// <summary>
            /// Bluetooth切断
            /// </summary>
            DISCONNECT_BLUETOOTH,
            /// <summary>
            /// デバイスパラメータ初期化
            /// </summary>
            CLEAR_PARAMS,
            /// <summary>
            /// 計測データ送信開始
            /// </summary>
            SEND_START,
            /// <summary>
            /// 計測データ送信停止
            /// </summary>
            SEND_STOP,
        }

        /// <summary>
        /// シーケンスステータス
        /// </summary>
        private SEQUENCE_STATUS seqenceStatus = SEQUENCE_STATUS.WAITING;

        /// <summary>
        /// ComPort検索スレッド処理ループ状態
        /// </summary>
        private bool scanComPortThreadLoop = false;

        /// <summary>
        /// ComPort検索スレッド
        /// </summary>
        private Thread scanComPortThread = null;

        /// <summary>
        /// ComPortリスト排他オブジェクト
        /// </summary>
        private Object comListLock = new Object();

        /// <summary>
        /// ドングルポートリスト
        /// </summary>
        private List<string> donglePortList = new List<string>();
        public List<string> DonglePortList
        {
            get { return this.donglePortList; }
        }


        /// <summary>
        /// 最後に受信したセンサーデータ時刻
        /// </summary>
        private DateTime lastSensorDataTime;

        /// <summary>
        /// エラー率計算タイマー
        /// </summary>
        private System.Timers.Timer errorRateTimer = new System.Timers.Timer();

        /// <summary>
        /// センサーデータ数の合計
        /// </summary>
        private double totalSensorDataCount = 0;

        /// <summary>
        /// カウンターエラー
        /// </summary>
        private double counterError = 0;

        /// <summary>
        /// カウンターエラー前回値
        /// </summary>
        private double preCounterError = 0;

        /// <summary>
        /// カウンターエラー保存配列数
        /// </summary>
        private const int LENGTH_SaveCounterError = 5;

        /// <summary>
        /// カウンターエラー保存配列
        /// </summary>
        private double[] saveCounterError = new double[LENGTH_SaveCounterError];

        /// <summary>
        /// カウンターエラー保存配列インデックス
        /// </summary>
        private int saveCounterErrorIndex = 0;



        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DataManager()
        {
            // センサーデータ通知
            this.com.SensorDataEvent += new Communication.SensorDataEventHandler(this.sensorDataEvent);

            // センサーデータ登録リスト
            this.sensorDataList = new List<SensorDataArgs>();

            // 読み取りデータモードをシリアル通信に設定
            this.ReadDataMode = DefReadDataMode.Serial;
            this.measureStatus = false;

            // サンプリングレート
            this.samplingRate = Constants.SENSING_INTERVAL;

            // コマンド応答タイマー設定
            this.commandResponseTimer.Enabled = false;
            this.commandResponseTimer.AutoReset = false;
            this.commandResponseTimer.Interval = Constants.COMMAND_RESPONSE_TIMER;
            this.commandResponseTimer.Elapsed += new ElapsedEventHandler(this.OnCommandResponseTimerout);

            // エラー率計算タイマー設定
            this.errorRateTimer.Enabled = false;
            this.errorRateTimer.AutoReset = true;
            this.errorRateTimer.Interval = 200;
            this.errorRateTimer.Elapsed += new ElapsedEventHandler(this.OnErrorRateTimerout);

            // 内部ステータスを初期化
            this.ackWaitCommand = ACK_WAIT_COMMAND.WAITING;
            this.seqenceStatus = SEQUENCE_STATUS.WAITING;
        }


        /// <summary>
        /// ComPort検索
        /// </summary>
        public void ScanComPort()
        {
            this.scanComPortThreadLoop = true;
            this.scanComPortThread = new Thread(new ThreadStart(this.scanDonglePort));
            this.scanComPortThread.Name = "Scan com port thread";
            this.scanComPortThread.Start();
        }

        /// <summary>
        /// ComPort検索停止
        /// </summary>
        public void StopScanComPort()
        {
            if (this.scanComPortThread != null)
            {
                this.scanComPortThreadLoop = false;
                this.scanComPortThread.Join(3000);
                this.scanComPortThread = null;
            }
        }

        /// <summary>
        /// ComPort検索応答
        /// </summary>
        private List<SensorDataArgs> scanComResponse = new List<SensorDataArgs>();

        /// <summary>
        /// ドングルPort検索スレッド
        /// </summary>
        private void scanDonglePort()
        {
            // ドングルポートリスト消去
            this.donglePortList.Clear();

            // ComPort一覧取得
            List<string> comList = this.com.GetComPortNameList();

            Communication scanCom = new Communication();
            scanCom.SensorDataEvent += new Communication.SensorDataEventHandler(this.scanDongleDataEvent);

            for (int index = 0; index < comList.Count; index++)
            {
                // スレッド終了要求確認
                if (this.scanComPortThreadLoop == false)
                {
                    break;
                }

                // ComPort検索進捗状況通知
                if (this.ScanComPortProgressEvent != null)
                {
                    this.ScanComPortProgressEvent(Convert.ToInt32(((double)index / comList.Count) * 100));
                }

                // Comオープン
                Tracer.WriteInformation("Scan com port. {0}", comList[index]);
                if (scanCom.ConnectComPort(comList[index]) == true)
                {
                    // コマンドレスポンス消去
                    this.scanComResponse.Clear();

                    // デバイス名確認コマンド出力
                    byte[] sendData = new byte[] { Constants.SIZE_DON_REQ_DEVICE_NAME, Constants.CMD_DONGLE, Constants.CMD_DON_REQ_DEVICE_NAME };
                    if (scanCom.SendCommand(sendData, Constants.SIZE_DON_REQ_DEVICE_NAME, false))
                    {
                        for (int waitCount = 0; waitCount < 15; waitCount++)
                        {
                            // コマンド受信待ち
                            Thread.Sleep(100);

                            // コマンド受信確認
                            if (this.scanComResponse.Count != 0)
                            {
                                DON_RES_DEVICE_NAME dongleData = (DON_RES_DEVICE_NAME)this.scanComResponse[0];
                                if ((dongleData.Length == Constants.SIZE_DON_RES_DEVICE_NAME) &&
                                    (dongleData.EventCode == Constants.CMD_DONGLE) &&
                                    (dongleData.SubCode == Constants.CMD_DON_RES_DEVICE_NAME))
                                {
                                    // 受信できれば、通信ポート一覧に追加する。
                                    Tracer.WriteInformation("Dongle com port. {0}", comList[index]);
                                    this.donglePortList.Add(comList[index]);
                                }
                                break;
                            }
                        }


                    }

                    // Comクローズ
                    scanCom.DisconnectComPort();
                }
            }

            // 受信イベント解除
            scanCom.SensorDataEvent -= new Communication.SensorDataEventHandler(this.scanDongleDataEvent);

            // コマンドレスポンス消去
            this.scanComResponse.Clear();

            // ComPort検索進捗状況通知（終了）
            if (this.ScanComPortProgressEvent != null)
            {
                this.ScanComPortProgressEvent(100);
            }
        }

        /// <summary>
        /// ドングル検索データ通知イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void scanDongleDataEvent(object sender, SensorDataArgs data)
        {
            Tracer.WriteInformation("Command Response = 0x{0:X2},0x{1:X2},0x{2:X2}", data.SensorData[0], data.SensorData[1], data.SensorData[2]);

            this.scanComResponse.Add(data);
        }

        /// <summary>
        /// ComPort接続
        /// </summary>
        /// <param name="portName"></param>
        /// <returns></returns>
        public bool ConnectComPort(string portName)
        {

            this.ackWaitCommand = ACK_WAIT_COMMAND.WAITING;
            this.seqenceStatus = SEQUENCE_STATUS.WAITING;

            bool result = this.com.ConnectComPort(portName);
            Tracer.WriteInformation("Connect com port. ({0}={1})", portName, result);

            if (result)
            {
                result = false;

                byte[] sendData = new byte[] { Constants.SIZE_DON_REQ_DISCONNECT_BLUETOOTH, Constants.CMD_DONGLE, Constants.CMD_DON_REQ_DISCONNECT_BLUETOOTH };
                if (this.com.SendCommand(sendData, Constants.SIZE_DON_REQ_DISCONNECT_BLUETOOTH, false))
                {
                    this.seqenceStatus = SEQUENCE_STATUS.DISCONNECT_INITIALIZE;
                    this.resetResponseTimer();

                    Tracer.WriteInformation("Request bluetooth disconnect. (initialize)");
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// ComPort切断
        /// </summary>
        public void DisconnectComPort()
        {
            Tracer.WriteInformation("Disconnect com port.");

            this.ackWaitCommand = ACK_WAIT_COMMAND.WAITING;
            this.seqenceStatus = SEQUENCE_STATUS.WAITING;
            this.commandResponseTimer.Enabled = false;
            this.errorRateTimer.Enabled = false;

            this.com.DisconnectComPort();
        }


        /// <summary>
        /// コマンド応答監視タイマーリセット
        /// </summary>
        private void resetResponseTimer()
        {
            this.commandResponseTimer.Enabled = false;
            this.commandResponseTimer.Enabled = true;
        }

        /// <summary>
        /// Bluetoothデバイス検索
        /// </summary>
        /// <returns></returns>
        public bool ScanBluetoothDevice()
        {
            bool result = false;

            try
            {
                if (this.com.IsSerialConnect)
                {
                    // MACアドレス取得コマンド送信（結果はイベント通知）
                    byte[] sendData = new byte[] { Constants.SIZE_DON_REQ_SCAN_BLUETOOTH, Constants.CMD_DONGLE, Constants.CMD_DON_REQ_SCAN_BLUETOOTH };
                    if (this.com.SendCommand(sendData, Constants.SIZE_DON_REQ_SCAN_BLUETOOTH, false))
                    {
                        this.seqenceStatus = SEQUENCE_STATUS.SCAN_BLUETOOTH;
                        this.resetResponseTimer();

                        Tracer.WriteInformation("Scan bluetooth MAC address.");
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// Bluetoothデバイス（相手機器）接続
        /// </summary>
        /// <param name="bluetoothMacAddress"></param>
        /// <returns></returns>
        public bool ConnectBluetooth(string bluetoothMacAddress)
        {
            bool result = false;

            try
            {
                if (this.com.IsSerialConnect)
                {
                    this.firmwareVersion = string.Empty;
                    this.com.BluetoothMacAddress = bluetoothMacAddress;

                    // Bluetooth接続コマンド送信
                    byte[] sendData = new byte[Constants.SIZE_DON_REQ_CONNECT_BLUETOOTH];
                    sendData[0] = Constants.SIZE_DON_REQ_CONNECT_BLUETOOTH;
                    sendData[1] = Constants.CMD_DONGLE;
                    sendData[2] = Constants.CMD_DON_REQ_CONNECT_BLUETOOTH;
                    Array.Copy(Encoding.ASCII.GetBytes(bluetoothMacAddress), 0, sendData, 3, Constants.SIZE_DON_REQ_CONNECT_BLUETOOTH - 3);
                    if (this.com.SendCommand(sendData, Constants.SIZE_DON_REQ_CONNECT_BLUETOOTH, false))
                    {
                        this.seqenceStatus = SEQUENCE_STATUS.CONNECT_BLUETOOTH;
                        this.resetResponseTimer();

                        Tracer.WriteInformation("Connect bluetooth.  ({0})", bluetoothMacAddress);
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// Bluetoothデバイス（相手機器）切断
        /// </summary>
        /// <returns></returns>
        public bool DisconnectBluetooth()
        {
            bool result = false;

            try
            {
                if (this.com.IsSerialConnect)
                {
                    this.firmwareVersion = string.Empty;

                    // TODO : 待ち時間を入れる。（ドングル切断要求前）
                    Thread.Sleep(Constants.PARAMATER_SETTING_WAIT_500);

                    // Bluetooth切断コマンド送信
                    byte[] sendData = new byte[Constants.SIZE_DON_REQ_DISCONNECT_BLUETOOTH];
                    sendData[0] = Constants.SIZE_DON_REQ_DISCONNECT_BLUETOOTH;
                    sendData[1] = Constants.CMD_DONGLE;
                    sendData[2] = Constants.CMD_DON_REQ_DISCONNECT_BLUETOOTH;
                    if (this.com.SendCommand(sendData, Constants.SIZE_DON_REQ_DISCONNECT_BLUETOOTH, false))
                    {
                        this.seqenceStatus = SEQUENCE_STATUS.DISCONNECT_BLUETOOTH;
                        this.resetResponseTimer();

                        Tracer.WriteInformation("Disconnect bluetooth.");
                        result = true;

                        // TODO : 待ち時間を入れる。（ドングル切断要求後）
                        Thread.Sleep(Constants.PARAMATER_SETTING_WAIT_500);
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// デバイスパラメータ初期化
        /// </summary>
        /// <returns></returns>
        public bool ClearParameter()
        {
            bool result = false;

            try
            {
                if (this.com.IsSerialConnect)
                {
                    this.firmwareVersion = string.Empty;

                    // 初期化コマンド送信
                    byte[] sendData = new byte[Constants.SIZE_ADN_CLR_PARAMS];
                    sendData[0] = Constants.SIZE_ADN_CLR_PARAMS;
                    sendData[1] = Constants.CMD_ADN_CLR_PARAMS;
                    sendData[2] = 0xFF;
                    if (this.com.SendCommand(sendData, Constants.SIZE_ADN_CLR_PARAMS, true))
                    {
                        this.seqenceStatus = SEQUENCE_STATUS.CLEAR_PARAMS;
                        this.ackWaitCommand = ACK_WAIT_COMMAND.CLEAR_PARAMS;
                        this.resetResponseTimer();

                        Tracer.WriteInformation("Clear device parameter.");
                        result = true;

                        // TODO : 待ち時間を入れる。（パラメータ初期化要求後）
                        Thread.Sleep(Constants.PARAMATER_SETTING_WAIT_500);
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// 計測開始
        /// </summary>
        public bool MeasureStart(byte measureMode, byte measureTransmissionSpeed, byte measureAccRange, byte measureGyroRange)
        {
            this.measureMode = measureMode;
            this.measureTransmissionSpeed = measureTransmissionSpeed;
            this.measureAccRange = measureAccRange;
            this.measureGyroRange = measureGyroRange;

            bool result = false;

            try
            {
                if (this.com.IsSerialConnect)
                {
                    this.preTimeCounter = -1;
                    this.seqenceStatus = SEQUENCE_STATUS.SEND_START;

                    // 動作モード取得コマンド送信
                    if (this.getDeviceMode())
                    {
                        result = true;
                    }
                    else
                    {
                        this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// 計測停止
        /// </summary>
        public bool MeasureStop()
        {
            bool result = false;

            try
            {
                if (this.com.IsSerialConnect)
                {
                    this.seqenceStatus = SEQUENCE_STATUS.SEND_STOP;
                    // 計測停止コマンド送信
                    if (this.setMeasureStop())
                    {
                        result = true;
                    }
                    else
                    {
                        this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// フリーマーキング
        /// </summary>
        /// <returns></returns>
        public void FreeMarking()
        {
            this.freeMarkingEndTime = DateTime.Now.AddMilliseconds(this.FreeMarkingTime);
            this.freeMarkingStatus = true;
        }

        /// <summary>
        /// コマンド応答待ちタイマー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCommandResponseTimerout(object sender, ElapsedEventArgs e)
        {
            Tracer.WriteInformation("Command Timeout !!  {0}", this.seqenceStatus.ToString());

            switch (this.seqenceStatus)
            {
                case SEQUENCE_STATUS.DISCONNECT_INITIALIZE:
                case SEQUENCE_STATUS.GET_DONGLE_NAME:
                case SEQUENCE_STATUS.GET_DONGLE_VERSION:
                case SEQUENCE_STATUS.SCAN_BLUETOOTH:
                    this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                    if (this.DongleConnectEvent != null)
                    {
                        this.DongleConnectEvent(Constants.RESPONSE_TIMEOUT, "Please check the COM port.");
                    }
                    break;

                case SEQUENCE_STATUS.CONNECT_BLUETOOTH:
                case SEQUENCE_STATUS.DISCONNECT_BLUETOOTH:
                    this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                    if (this.BluetoothConnectEvent != null)
                    {
                        this.BluetoothConnectEvent(Constants.RESPONSE_TIMEOUT);
                    }
                    break;
                case SEQUENCE_STATUS.CLEAR_PARAMS:
                    this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                    this.ackWaitCommand = ACK_WAIT_COMMAND.WAITING;
                    if (this.DeviceInitializeEvent != null)
                    {
                        this.DeviceInitializeEvent(Constants.RESPONSE_TIMEOUT);
                    }
                    break;
                case SEQUENCE_STATUS.SEND_START:
                    this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                    this.ackWaitCommand = ACK_WAIT_COMMAND.WAITING;
                    if (this.MeasureStartEvent != null)
                    {
                        this.MeasureStartEvent(Constants.RESPONSE_TIMEOUT);
                    }
                    break;
                case SEQUENCE_STATUS.SEND_STOP:
                    this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                    this.ackWaitCommand = ACK_WAIT_COMMAND.WAITING;
                    if (this.MeasureStopEvent != null)
                    {
                        this.MeasureStopEvent(Constants.RESPONSE_TIMEOUT);
                    }
                    break;
            }
        }

        /// <summary>
        /// センサーデータ通知イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sensorDataEvent(object sender, SensorDataArgs data)
        {
            try
            {
                // 排他する
                measureDataMutex.WaitOne();

                if (data.DataType == 0x01)
                {
                    // フリーマーキング
                    if (this.freeMarkingStatus)
                    {
                        // TODO : 一定時間によるフリーマーキングを中止し、１データのみとする
                        //if (this.freeMarkingEndTime.CompareTo(DateTime.Now) < 0)
                        //{
                        //    this.freeMarkingStatus = false;
                        //    this.FreeMarkingEndEvent();
                        //}
                        //data.Marking = this.freeMarkingStatus;

                        data.Marking = true;
                        this.freeMarkingStatus = false;
                        this.FreeMarkingEndEvent();
                    }

                    this.sensorDataList.Add(data);
                }
                else
                {
                    this.commandResponse(data);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                // Mutexロック解放
                measureDataMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// コマンドレスポンス処理
        /// </summary>
        /// <param name="data"></param>
        private void commandResponse(SensorDataArgs data)
        {
            Tracer.WriteInformation("Command Response = 0x{0:X2},0x{1:X2},0x{2:X2}", data.SensorData[0], data.SensorData[1], data.SensorData[2]);

            switch (data.EventCode)
            {
                case Constants.CMD_AUP_REPORT_DEV_INFO:
                    this.receiveDeviceInfo(data);
                    break;
                case Constants.CMD_AUP_REPORT_MODE:
                    this.receiveDeviceMode(data);
                    break;
                case Constants.CMD_AUP_REPORT_6AXIS_PARAMS:
                    this.receive6AxistParams(data);
                    break;
                case Constants.CMD_AUP_REPORT_RESP:
                    this.receiveResponse(data);
                    break;

                case Constants.CMD_DONGLE:
                    // ドングルコマンドの場合は、サブコードを確認する
                    switch (data.SensorData[2])
                    {
                        case Constants.CMD_DON_RES_DEVICE_NAME:
                            this.receiveDongleName(data);
                            break;
                        case Constants.CMD_DON_RES_DEVICE_VERSION:
                            this.receiveDongleVersion(data);
                            break;
                        case Constants.CMD_DON_RES_SCAN_BLUETOOTH:
                        case Constants.CMD_DON_RES_SCAN_END:
                            this.receiveBluetoothDevice(data.SensorData[2], data);
                            break;
                        case Constants.CMD_DON_RES_CONNECT_BLUETOOTH:
                            this.receiveBluetoothConnect(data);
                            break;
                        case Constants.CMD_DON_RES_DISCONNECT_BLUETOOTH:
                            this.receiveBluetoothDisconnect(data);
                            break;
                        case Constants.CMD_DON_RES_SUPERVISION_TIMEOUT:
                            this.receiveBluetoothSupervisionTimeout(data);
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// 機器情報受信
        /// </summary>
        /// <param name="data"></param>
        private void receiveDeviceInfo(SensorDataArgs data)
        {
            if (this.seqenceStatus != SEQUENCE_STATUS.CONNECT_BLUETOOTH)
            {
                return;
            }

            this.commandResponseTimer.Enabled = false;

            // ファームバージョンを取得
            APU_REPORT_DEV_INFO apuData = (APU_REPORT_DEV_INFO)data;
            this.firmwareVersion = string.Format("{0}{1}-{2:D2}.{3:D2}.{4:D2}", apuData.FWVModelHi, apuData.FWVModelLo,
                                                                                int.Parse(apuData.FWVMajor, System.Globalization.NumberStyles.HexNumber),
                                                                                int.Parse(apuData.FWVMinor, System.Globalization.NumberStyles.HexNumber),
                                                                                int.Parse(apuData.FWVRevision, System.Globalization.NumberStyles.HexNumber));
            Tracer.WriteInformation("Firmware version.  ({0})", this.firmwareVersion);

            // 動作モード取得コマンド送信
            if (this.getDeviceMode() == false)
            {
                this.seqenceStatus = SEQUENCE_STATUS.WAITING;

                // 画面クラスへ通知
                if (this.BluetoothConnectEvent != null)
                {
                    this.BluetoothConnectEvent(Constants.RESPONSE_ETC_ERROR);
                }
            }
        }

        /// <summary>
        /// 動作モード取得
        /// </summary>
        /// <returns></returns>
        private bool getDeviceMode()
        {
            bool result = false;

            try
            {
                if (this.com.IsSerialConnect)
                {
                    // TODO : 待ち時間を入れる。（デバイス動作モード取得要求前）
                    Thread.Sleep(Constants.PARAMATER_SETTING_WAIT_1000);

                    byte[] sendData = new byte[Constants.SIZE_ADN_GET_MODE];
                    sendData[0] = Constants.SIZE_ADN_GET_MODE;
                    sendData[1] = Constants.CMD_ADN_GET_MODE;
                    sendData[2] = 0;
                    if (this.com.SendCommand(sendData, Constants.SIZE_ADN_GET_MODE, true))
                    {
                        this.resetResponseTimer();

                        Tracer.WriteInformation("Get device mode.");
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// 機器動作モード受信
        /// </summary>
        /// <param name="data"></param>
        private void receiveDeviceMode(SensorDataArgs data)
        {
            if ((this.seqenceStatus != SEQUENCE_STATUS.CONNECT_BLUETOOTH) &&
                (this.seqenceStatus != SEQUENCE_STATUS.CLEAR_PARAMS) &&
                (this.seqenceStatus != SEQUENCE_STATUS.SEND_START))
            {
                return;
            }

            this.commandResponseTimer.Enabled = false;

            // 動作モードを取得
            APU_REPORT_MODE apuData = (APU_REPORT_MODE)data;
            this.deviceMode = apuData.AcademiaMode;
            this.transmissionSpeed = apuData.TransmissionSpeed;
            Tracer.WriteInformation("Receive device mode.  ({0})", this.deviceMode);
            Tracer.WriteInformation("Receive transmission speed.  ({0})", this.transmissionSpeed);

            if ((this.transmissionSpeed != 1) && (this.transmissionSpeed != 2))
            {
                this.transmissionSpeed = 1;
            }

            switch (this.seqenceStatus)
            {
                case SEQUENCE_STATUS.CONNECT_BLUETOOTH:
                case SEQUENCE_STATUS.CLEAR_PARAMS:
                    // ６軸センサパラメータ取得コマンド送信
                    if (this.get6AxisParams() == false)
                    {
                        this.seqenceStatus = SEQUENCE_STATUS.WAITING;

                        // 画面クラスへ通知
                        if (this.seqenceStatus == SEQUENCE_STATUS.CONNECT_BLUETOOTH)
                        {
                            if (this.BluetoothConnectEvent != null)
                            {
                                this.BluetoothConnectEvent(Constants.RESPONSE_ETC_ERROR);
                            }
                        }
                        else
                        {
                            if (this.DeviceInitializeEvent != null)
                            {
                                this.DeviceInitializeEvent(Constants.RESPONSE_ETC_ERROR);
                            }
                        }
                    }
                    break;
                case SEQUENCE_STATUS.SEND_START:
                    // 機器モード設定送信
                    if (this.setDeviceMode(this.measureMode, this.measureTransmissionSpeed) == false)
                    {
                        this.seqenceStatus = SEQUENCE_STATUS.WAITING;

                        // 画面クラスへ通知
                        if (this.MeasureStartEvent != null)
                        {
                            this.MeasureStartEvent(Constants.RESPONSE_ETC_ERROR);
                        }
                    }
                    break;

            }
        }

        /// <summary>
        /// ６軸センサパラメータ取得
        /// </summary>
        /// <returns></returns>
        private bool get6AxisParams()
        {
            bool result = false;

            try
            {
                if (this.com.IsSerialConnect)
                {
                    byte[] sendData = new byte[Constants.SIZE_ADN_GET_6AXIS_PARAMS];
                    sendData[0] = Constants.SIZE_ADN_GET_6AXIS_PARAMS;
                    sendData[1] = Constants.CMD_ADN_GET_6AXIS_PARAMS;
                    sendData[2] = 0;
                    if (this.com.SendCommand(sendData, Constants.SIZE_ADN_GET_6AXIS_PARAMS, true))
                    {
                        this.resetResponseTimer();

                        Tracer.WriteInformation("Get 6Axis parameters.");
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// ６軸センサパラメータ受信
        /// </summary>
        /// <param name="data"></param>
        private void receive6AxistParams(SensorDataArgs data)
        {
            if ((this.seqenceStatus != SEQUENCE_STATUS.CONNECT_BLUETOOTH) &&
                (this.seqenceStatus != SEQUENCE_STATUS.CLEAR_PARAMS) &&
                (this.seqenceStatus != SEQUENCE_STATUS.SEND_START))
            {
                return;
            }

            this.commandResponseTimer.Enabled = false;

            // ６軸センサパラメータを取得
            APU_REPORT_6AXIS_PARAMS apuData = (APU_REPORT_6AXIS_PARAMS)data;
            this.accelerationSensorRange = apuData.AccRange;
            this.gyroSensorRange = apuData.GyroRange;
            Tracer.WriteInformation("Receive 6Axis parameters.  (AccRange:{0}  GyroRange:{1})", this.accelerationSensorRange, this.gyroSensorRange);

            switch (this.seqenceStatus)
            {
                case SEQUENCE_STATUS.CONNECT_BLUETOOTH:
                    this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                    // 画面クラスへ結果通知
                    if (this.BluetoothConnectEvent != null)
                    {
                        this.BluetoothConnectEvent(Constants.RESPONSE_ACK);
                    }
                    break;
                case SEQUENCE_STATUS.CLEAR_PARAMS:
                    this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                    // 画面クラスへ結果通知
                    if (this.DeviceInitializeEvent != null)
                    {
                        this.DeviceInitializeEvent(Constants.RESPONSE_ACK);
                    }
                    break;
                case SEQUENCE_STATUS.SEND_START:
                    // ６軸センサパラメータ送信
                    if (this.set6AxisParams(this.measureAccRange, this.measureGyroRange) == false)
                    {
                        this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                        // 画面クラスへ結果通知
                        if (this.MeasureStartEvent != null)
                        {
                            this.MeasureStartEvent(Constants.RESPONSE_ETC_ERROR);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 機器モード設定
        /// </summary>
        /// <param name="mode">動作モード</param>
        /// <param name="speed">送信速度（データ品質）</param>
        /// <returns></returns>
        private bool setDeviceMode(byte mode, byte speed)
        {
            bool result = false;

            try
            {
                if (this.com.IsSerialConnect)
                {
                    byte[] sendData = new byte[Constants.SIZE_ADN_SET_MODE];
                    sendData[0] = Constants.SIZE_ADN_SET_MODE;
                    sendData[1] = Constants.CMD_ADN_SET_MODE;
                    sendData[2] = 0;
                    sendData[3] = 0;
                    sendData[4] = mode;
                    sendData[5] = speed;
                    if (this.com.SendCommand(sendData, Constants.SIZE_ADN_SET_MODE, true))
                    {
                        this.ackWaitCommand = ACK_WAIT_COMMAND.SET_MODE;
                        this.resetResponseTimer();

                        Tracer.WriteInformation("Set device mode.  ({0})", mode);
                        result = true;

                        // TODO : 待ち時間を入れる。（デバイス動作モード設定要求後）
                        Thread.Sleep(Constants.PARAMATER_SETTING_WAIT_500);
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// 測定開始
        /// </summary>
        /// <returns></returns>
        private bool setMeasureStart()
        {
            bool result = false;

            try
            {
                this.freeMarkingStatus = false;
                this.freeMarkingEndTime = DateTime.Now;
                this.sensorDataSerialNumber = 0;
                this.totalSensorDataCount = 0;
                this.counterError = this.preCounterError = 0;
                this.saveCounterErrorIndex = 0;
                for (int index = 0; index < LENGTH_SaveCounterError; index++)
                {
                    this.saveCounterError[index] = 0;
                }
                this.lastSensorDataTime = DateTime.Now;
                this.errorRateTimer.Enabled = true;

                if (this.com.IsSerialConnect)
                {
                    byte[] sendData = new byte[Constants.SIZE_ADN_START_STOP_SEND];
                    sendData[0] = Constants.SIZE_ADN_START_STOP_SEND;
                    sendData[1] = Constants.CMD_ADN_START_STOP_SEND;
                    sendData[2] = 1;
                    if (this.com.SendCommand(sendData, Constants.SIZE_ADN_START_STOP_SEND, true))
                    {
                        this.ackWaitCommand = ACK_WAIT_COMMAND.START_MEASURE;
                        this.resetResponseTimer();

                        Tracer.WriteInformation("Set measure start. ");
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// 測定停止
        /// </summary>
        /// <returns></returns>
        private bool setMeasureStop()
        {
            bool result = false;

            try
            {
                this.errorRateTimer.Enabled = false;

                if (this.com.IsSerialConnect)
                {
                    byte[] sendData = new byte[Constants.SIZE_ADN_START_STOP_SEND];
                    sendData[0] = Constants.SIZE_ADN_START_STOP_SEND;
                    sendData[1] = Constants.CMD_ADN_START_STOP_SEND;
                    sendData[2] = 0;
                    if (this.com.SendCommand(sendData, Constants.SIZE_ADN_START_STOP_SEND, true))
                    {
                        this.ackWaitCommand = ACK_WAIT_COMMAND.STOP_MEASURE;
                        this.resetResponseTimer();

                        Tracer.WriteInformation("Set measure stop. ");
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// コマンドレスポンス受信
        /// </summary>
        /// <param name="data"></param>
        private void receiveResponse(SensorDataArgs data)
        {
            if ((this.seqenceStatus != SEQUENCE_STATUS.CLEAR_PARAMS) &&
                (this.seqenceStatus != SEQUENCE_STATUS.SEND_START) &&
                (this.seqenceStatus != SEQUENCE_STATUS.SEND_STOP))
            {
                return;
            }

            this.commandResponseTimer.Enabled = false;

            // レスポンスを取得
            APU_REPORT_RESP apuData = (APU_REPORT_RESP)data;

            switch (this.seqenceStatus)
            {
                case SEQUENCE_STATUS.CLEAR_PARAMS:
                    this.rcvRespClearParams(apuData.Result);
                    break;
                case SEQUENCE_STATUS.SEND_START:
                    this.rcvRespSendStart(apuData.Result);
                    break;
                case SEQUENCE_STATUS.SEND_STOP:
                    this.rcvRespSendStop(apuData.Result);
                    break;
            }
        }

        /// <summary>
        /// パラメータ初期化応答受信
        /// </summary>
        /// <param name="result"></param>
        private void rcvRespClearParams(byte result)
        {
            Tracer.WriteInformation("Response clear parameters.  ({0})", result);

            if (result != Constants.RESPONSE_ACK)
            {
                this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                this.ackWaitCommand = ACK_WAIT_COMMAND.WAITING;

                // 画面クラスへ通知
                if (this.DeviceInitializeEvent != null)
                {
                    this.DeviceInitializeEvent(Constants.RESPONSE_NACK);
                }
            }
            else
            {
                // 動作モード取得コマンド送信
                if (this.getDeviceMode() == false)
                {
                    this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                    this.ackWaitCommand = ACK_WAIT_COMMAND.WAITING;

                    // 画面クラスへ通知
                    if (this.DeviceInitializeEvent != null)
                    {
                        this.DeviceInitializeEvent(Constants.RESPONSE_ETC_ERROR);
                    }
                }
            }
        }

        /// <summary>
        /// 測定開始応答受信
        /// </summary>
        /// <param name="result"></param>
        private void rcvRespSendStart(byte result)
        {
            Tracer.WriteInformation("Response measure start.  ({0}={1})", this.ackWaitCommand.ToString(), result);

            if (result != Constants.RESPONSE_ACK)
            {
                this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                this.measureStatus = false;

                // 画面クラスへ通知
                if (this.MeasureStartEvent != null)
                {
                    this.MeasureStartEvent(result);
                }
            }
            else
            {
                switch (this.ackWaitCommand)
                {
                    case ACK_WAIT_COMMAND.SET_MODE:
                        this.deviceMode = this.measureMode;
                        this.transmissionSpeed = this.measureTransmissionSpeed;
                        // ６軸センサパラメータ取得コマンド送信
                        if(this.get6AxisParams() == false)
                        {
                            if (this.MeasureStartEvent != null)
                            {
                                this.MeasureStartEvent(Constants.RESPONSE_ETC_ERROR);
                            }
                        }
                        break;
                    case ACK_WAIT_COMMAND.SET_6AXIS_PARAMS:
                        this.accelerationSensorRange = this.measureAccRange;
                        this.gyroSensorRange = this.measureGyroRange;
                        // 測定開始コマンド送信
                        if(this.setMeasureStart() == false)
                        {
                            if (this.MeasureStartEvent != null)
                            {
                                this.MeasureStartEvent(Constants.RESPONSE_ETC_ERROR);
                            }
                        }
                        break;
                    case ACK_WAIT_COMMAND.START_MEASURE:
                        // 画面クラスへ結果通知
                        if (this.MeasureStartEvent != null)
                        {
                            this.MeasureStartEvent(result);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// ６軸センサパラメータ設定
        /// </summary>
        /// <param name="accRange"></param>
        /// <param name="gyroRange"></param>
        /// <returns></returns>
        private bool set6AxisParams(byte accRange, byte gyroRange)
        {
            bool result = false;

            try
            {
                if (this.com.IsSerialConnect)
                {
                    byte[] sendData = new byte[Constants.SIZE_ADN_SET_6AXIS_PARAMS];
                    sendData[0] = Constants.SIZE_ADN_SET_6AXIS_PARAMS;
                    sendData[1] = Constants.CMD_ADN_SET_6AXIS_PARAMS;
                    sendData[2] = accRange;
                    sendData[3] = gyroRange;
                    if (this.com.SendCommand(sendData, Constants.SIZE_ADN_SET_6AXIS_PARAMS, true))
                    {
                        this.resetResponseTimer();
                        this.ackWaitCommand = ACK_WAIT_COMMAND.SET_6AXIS_PARAMS;

                        Tracer.WriteInformation("Set 6Axis parameters.  (AccRange:{0}  GyroRange:{1})", accRange, gyroRange);
                        result = true;

                        // TODO : 待ち時間を入れる。（デバイス６軸パラメータ設定要求後）
                        Thread.Sleep(Constants.PARAMATER_SETTING_WAIT_500);
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// 測定停止応答受信
        /// </summary>
        /// <param name="result"></param>
        private void rcvRespSendStop(byte result)
        {
            Tracer.WriteInformation("Response measure stop.  ({0})", result);

            this.seqenceStatus = SEQUENCE_STATUS.WAITING;
            this.measureStatus = false;

            // 画面クラスへ通知
            if (this.MeasureStopEvent != null)
            {
                this.MeasureStopEvent(result);
            }
        }

        /// <summary>
        /// ドングル名受信
        /// </summary>
        /// <param name="data"></param>
        private void receiveDongleName(SensorDataArgs data)
        {
            string errorMessage = string.Empty;

            DON_RES_DEVICE_NAME dongleData = (DON_RES_DEVICE_NAME)data;
            string dongleName = dongleData.DeviceName;

            Tracer.WriteInformation("Receive dongle name.  ({0})", dongleName);

            this.commandResponseTimer.Enabled = false;

            // TODO : 最終リリース時は、ドングル名の比較を行う？
            //if (dongleName.Equals(Constants.DONGLE_NAME) == false)
            //{
            //    errorMessage = "Please check the COM port.";
            //}
            //else
            //{
                // ドングルバージョン取得要求送信
                byte[] sendData = new byte[Constants.SIZE_DON_REQ_DEVICE_VERSION];
                sendData[0] = Constants.SIZE_DON_REQ_DEVICE_VERSION;
                sendData[1] = Constants.CMD_DONGLE;
                sendData[2] = Constants.CMD_DON_REQ_DEVICE_VERSION;
                if (this.com.SendCommand(sendData, Constants.SIZE_DON_REQ_DEVICE_VERSION, false))
                {
                    this.resetResponseTimer();
                    this.seqenceStatus = SEQUENCE_STATUS.GET_DONGLE_VERSION;

                    Tracer.WriteInformation("Get dongle version.");
                }
                else
                {
                    errorMessage = "Please check the COM port.";
                }
            //}

            if (errorMessage.Equals(string.Empty) == false)
            {
                this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                if (this.DongleConnectEvent != null)
                {
                    this.DongleConnectEvent(Constants.RESPONSE_ETC_ERROR, errorMessage);
                }
            }
        }

        /// <summary>
        /// ドングルバージョン受信
        /// </summary>
        /// <param name="data"></param>
        private void receiveDongleVersion(SensorDataArgs data)
        {
            DON_RES_DEVICE_VERSION dongleData = (DON_RES_DEVICE_VERSION)data;
            string dongleVersion = dongleData.DeviceVersion;

            string[] splitVersion = dongleVersion.Split('.');
            dongleVersion = string.Format("{0:D2}.{1:D2}.{2:D2}", int.Parse(splitVersion[0], System.Globalization.NumberStyles.HexNumber), 
                                                                    int.Parse(splitVersion[1], System.Globalization.NumberStyles.HexNumber),
                                                                    int.Parse(splitVersion[2], System.Globalization.NumberStyles.HexNumber));

            Tracer.WriteInformation("Receive dongle version.  ({0})", dongleVersion);

            byte status = Constants.RESPONSE_ACK;

            // TODO : 最終リリース時には、ドングルバージョン比較を行う？
            //string[] splitVersion = Constants.DONGLE_VERSION.Split('.');
            //string compareVersion = splitVersion[0] + "." + splitVersion[1];
            //if (compareVersion.Equals(dongleVersion.Substring(0, compareVersion.Length)) == false)
            //{
            //    status = Constants.RESPONSE_ETC_ERROR;
            //    dongleVersion = "Please refer to the version up of dongle.";
            //}

            this.commandResponseTimer.Enabled = false;
            this.seqenceStatus = SEQUENCE_STATUS.WAITING;
            if (this.DongleConnectEvent != null)
            {
                this.DongleConnectEvent(status, dongleVersion);
            }
        }

        /// <summary>
        /// Bluetoothデバイス情報受信
        /// </summary>
        /// <param name="e"></param>
        private void receiveBluetoothDevice(byte subCode, SensorDataArgs data)
        {
            DON_RES_SCAN_BLUETOOTH dongleData = (DON_RES_SCAN_BLUETOOTH)data;

            string macAddress = string.Empty;
            if (subCode == Constants.CMD_DON_RES_SCAN_BLUETOOTH)
            {
                macAddress = dongleData.MacAddress;
                Tracer.WriteInformation("Receive bluetooth MAC address.  ({0})", dongleData.MacAddress);
            }
            else
            {
                Tracer.WriteInformation("End bluetooth scan.");
            }

            if (this.BluetoothMacAddressEvent != null)
            {
                this.BluetoothMacAddressEvent(dongleData.MacAddress);
            }
        }

        /// <summary>
        /// Bluetoothデバイス接続受信
        /// </summary>
        /// <param name="data"></param>
        private void receiveBluetoothConnect(SensorDataArgs data)
        {
            if ((this.seqenceStatus != SEQUENCE_STATUS.CONNECT_BLUETOOTH) &&
                (this.seqenceStatus != SEQUENCE_STATUS.DISCONNECT_BLUETOOTH))
            {
                return;
            }

            this.commandResponseTimer.Enabled = false;

            DON_RES_CONNECT_BLUETOOTH dongleData = (DON_RES_CONNECT_BLUETOOTH)data;

            byte connectStatus = Constants.RESPONSE_ACK;
            if (this.seqenceStatus == SEQUENCE_STATUS.CONNECT_BLUETOOTH)
            {
                Tracer.WriteInformation("Receive bluetooth connect.  (0x{0:X2})", dongleData.SubCode);

                if (dongleData.SubCode == 0x41)
                {
                    // 正常終了時、機器情報取得コマンド送信
                    if (this.getDeviceInfo() == false)
                    {
                        connectStatus = Constants.RESPONSE_ETC_ERROR;
                    }
                }
                else
                {
                    connectStatus = Constants.RESPONSE_ETC_ERROR;
                }
            }
            else
            {
                Tracer.WriteInformation("Receive bluetooth disconnect.  (0x{0:X2})", dongleData.SubCode);
                if (dongleData.SubCode == 0x51)
                {
                    connectStatus = Constants.RESPONSE_DISCONNECT;
                }
                else
                {
                    connectStatus = Constants.RESPONSE_ETC_ERROR;
                }
            }

            if ((connectStatus != Constants.RESPONSE_ACK) || (this.seqenceStatus == SEQUENCE_STATUS.DISCONNECT_BLUETOOTH))
            {
                this.seqenceStatus = SEQUENCE_STATUS.WAITING;

                // 画面クラスへ通知
                if (this.BluetoothConnectEvent != null)
                {
                    this.BluetoothConnectEvent(connectStatus);
                }
            }
        }

        /// <summary>
        /// 機器情報取得
        /// </summary>
        /// <returns></returns>
        private bool getDeviceInfo()
        {
            bool result = false;

            try
            {
                if (this.com.IsSerialConnect)
                {
                    // デバイス接続後の待ち時間（必須）
                    Thread.Sleep(100);

                    byte[] sendData = new byte[Constants.SIZE_ADN_DEV_INFO];
                    sendData[0] = Constants.SIZE_ADN_DEV_INFO;
                    sendData[1] = Constants.CMD_ADN_DEV_INFO;
                    sendData[2] = 0;
                    if (this.com.SendCommand(sendData, Constants.SIZE_ADN_DEV_INFO, true))
                    {
                        this.resetResponseTimer();

                        Tracer.WriteInformation("Get Device Info.");
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// Bluetooth接続切断通知
        /// </summary>
        /// <param name="data"></param>
        private void receiveBluetoothDisconnect(SensorDataArgs data)
        {
            try
            {
                Tracer.WriteInformation("Receive bluetooth disconnect.");

                if (this.seqenceStatus == SEQUENCE_STATUS.DISCONNECT_INITIALIZE)
                {
                    // ドングル名取得
                    byte[] sendData = new byte[] { Constants.SIZE_DON_REQ_DEVICE_NAME, Constants.CMD_DONGLE, Constants.CMD_DON_REQ_DEVICE_NAME };
                    if (this.com.SendCommand(sendData, Constants.SIZE_DON_REQ_DEVICE_NAME, false))
                    {
                        this.seqenceStatus = SEQUENCE_STATUS.GET_DONGLE_NAME;
                        this.resetResponseTimer();

                        Tracer.WriteInformation("Request dongle name.");
                    }
                    else
                    {
                        this.commandResponseTimer.Enabled = false;
                        this.errorRateTimer.Enabled = false;
                        this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                        this.ackWaitCommand = ACK_WAIT_COMMAND.WAITING;

                        if (this.DongleConnectEvent != null)
                        {
                            this.DongleConnectEvent(Constants.RESPONSE_ETC_ERROR, "Please check the COM port.");
                        }
                    }
                }
                else
                {
                    this.commandResponseTimer.Enabled = false;
                    this.errorRateTimer.Enabled = false;
                    this.seqenceStatus = SEQUENCE_STATUS.WAITING;
                    this.ackWaitCommand = ACK_WAIT_COMMAND.WAITING;

                    // Bluetooth接続切断を画面クラスへ通知
                    if (this.BluetoothConnectEvent != null)
                    {
                        this.BluetoothConnectEvent(Constants.RESPONSE_DISCONNECT);
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
        }

        /// <summary>
        /// Bluetooth接続切断通知（Supervision Timeout）
        /// </summary>
        /// <param name="data"></param>
        private void receiveBluetoothSupervisionTimeout(SensorDataArgs data)
        {
            this.commandResponseTimer.Enabled = false;
            this.errorRateTimer.Enabled = false;
            this.seqenceStatus = SEQUENCE_STATUS.WAITING;
            this.ackWaitCommand = ACK_WAIT_COMMAND.WAITING;

            // Bluetooth接続切断を画面クラスへ通知
            if (this.BluetoothConnectEvent != null)
            {
                this.BluetoothConnectEvent(Constants.RESPONSE_DISCONNECT);
            }
        }

        /// <summary>
        /// センサーデータ消去
        /// </summary>
        public void ClearSensorData()
        {
            try
            {
                measureDataMutex.WaitOne();

                this.sensorDataList.Clear();
                this.sensorHistoryList.Clear();
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                measureDataMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// センサーデータ取得
        /// </summary>
        /// <returns></returns>
        public List<MeasureBean> GetSensorData()
        {
            List<MeasureBean> result = new List<MeasureBean>();

            try
            {
                // 排他する
                measureDataMutex.WaitOne();

                int listCount = this.sensorDataList.Count;
                if (listCount == 0)
                {
                    return null;
                }

                for (int index = 0; index < listCount; index++)
                {
                    SensorDataArgs data = this.sensorDataList[index];
                    MeasureBean bean = null;
                    switch (data.EventCode)
                    {
                        case Constants.CMD_AUP_REPORT_ACADEMIA1:
                            bean = this.receiveAcademia1(data);
                            break;
                        case Constants.CMD_AUP_REPORT_ACADEMIA2:
                            bean = this.receiveAcademia2(data);
                            break;
                        case Constants.CMD_AUP_REPORT_ACADEMIA3:
                            bean = this.receiveAcademia3(data);
                            break;
                    }
                    result.Add(bean);
                    this.sensorHistoryList.Add(bean);

                    this.lastSensorDataTime = DateTime.Now;
                }
                this.sensorDataList.RemoveRange(0, listCount);
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                // Mutexロック解放
                measureDataMutex.ReleaseMutex();
            }
            return result;
        }

        /// <summary>
        /// エラー率計算タイマー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnErrorRateTimerout(object sender, ElapsedEventArgs e)
        {
            if (this.totalSensorDataCount == 0)
            {
                return;
            }

            double accumulatedDataLoss = 0, transientDataLoss = 0, difference = 0, transientDataNum = this.errorRateTimer.Interval / (Constants.SENSING_INTERVAL * this.measureTransmissionSpeed);

            // 最後にセンサーデータを受信した時間差により、算出する方式を変更する。
            TimeSpan timeSpan = DateTime.Now - this.lastSensorDataTime;
            if (timeSpan.TotalMilliseconds < (this.errorRateTimer.Interval + 100))
            {
                // 実際に確認したデータ損失を算出する。
                if (this.totalSensorDataCount != 0)
                {
                    accumulatedDataLoss = (this.totalSensorDataCount - this.counterError) / this.totalSensorDataCount * 100;
                    difference = this.counterError - this.preCounterError;
                    if (difference > transientDataNum)
                    {
                        difference = transientDataNum;
                    }
                }
            }
            else
            {
                // 一定時間（Timer.Interval + 100 = 300ms）データが全く通知されない場合は、最後にセンサーデータを受信した時間を基に算出する。
                double expectedLoss = Convert.ToUInt32(timeSpan.TotalMilliseconds / (Constants.SENSING_INTERVAL * this.measureTransmissionSpeed));
                double total = this.totalSensorDataCount + expectedLoss;
                double error = this.counterError + expectedLoss;

                accumulatedDataLoss = (total - error) / total * 100;
                difference = transientDataNum;
            }
            this.preCounterError = this.counterError;

            // 過渡データ損失率を算出する
            this.saveCounterError[this.saveCounterErrorIndex] = difference;
            this.saveCounterErrorIndex++;
            if (this.saveCounterErrorIndex >= LENGTH_SaveCounterError)
            {
                this.saveCounterErrorIndex = 0;
            }
            difference = 0;
            for (int index = 0; index < LENGTH_SaveCounterError; index++)
            {
                difference += this.saveCounterError[index];
            }
            transientDataLoss = (transientDataNum - (difference / LENGTH_SaveCounterError)) / transientDataNum * 100;

            // エラー率イベント通知
            this.ErrorRateEvent(accumulatedDataLoss, transientDataLoss);
        }

        /// <summary>
        /// センシング時間取得
        /// </summary>
        /// <param name="counter">カウンタ</param>
        /// <returns>センシング時間</returns>
        private long getSensingTime(int counter)
        {
            long time = 0;

            if (this.preTimeCounter < 0)
            {
                this.timeElapsed = 0;
                this.totalSensorDataCount = 1;
            }
            else if (this.preTimeCounter <= counter)
            {
                time = counter - this.preTimeCounter;
                this.totalSensorDataCount += time;
            }
            else
            {
                time = Constants.SENSING_COUNTER_MAX - this.preTimeCounter + counter + 1;
                this.totalSensorDataCount += time;
            }
            this.preTimeCounter = counter;

            // カウンターエラーを積算
            if (time > 1)
            {
                this.counterError += (time - 1);
            }

            this.timeElapsed += (time * this.samplingRate * this.transmissionSpeed);

            return this.timeElapsed;
        }

        /// <summary>
        /// アカデミアデータ（スタンダード）受信
        /// </summary>
        /// <param name="data"></param>
        private MeasureBean receiveAcademia1(SensorDataArgs data)
        {
            APU_REPORT_ACADEMIA1 apuData = (APU_REPORT_ACADEMIA1)data;

            this.batteryLevel = apuData.Battery;

            //if (data.Marking)
            //{
            //    return new MeasureBean(data.Marking, ++this.sensorDataSerialNumber, this.getSensingTime(apuData.Counter), new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            //}

            //double accX = AxisMaster.AccelerationAxis.CalcAdValue(apuData.AccX);
            //double accY = AxisMaster.AccelerationAxis.CalcAdValue(apuData.AccY);
            //double accZ = AxisMaster.AccelerationAxis.CalcAdValue(apuData.AccZ);
            //double eogL1 = AxisMaster.ElectrooculographyAxis.CalcAdValue(apuData.EogL1);
            //double eogR1 = AxisMaster.ElectrooculographyAxis.CalcAdValue(apuData.EogR1);
            //double eogL2 = AxisMaster.ElectrooculographyAxis.CalcAdValue(apuData.EogL2);
            //double eogR2 = AxisMaster.ElectrooculographyAxis.CalcAdValue(apuData.EogR2);
            //double deltaH1 = eogL1 - eogR1;
            //double deltaH2 = eogL2 - eogR2;
            //double deltaV1 = 0 - ((eogL1 + eogR1) / 2);
            //double deltaV2 = 0 - ((eogL2 + eogR2) / 2);

            return new MeasureBean(data.Marking,
                                    ++this.sensorDataSerialNumber,
                                    this.getSensingTime(apuData.Counter),
                                    new int[] { apuData.AccX, apuData.AccY, apuData.AccZ,
                                                apuData.EogL1, apuData.EogR1, apuData.EogL2, apuData.EogR2,
                                                apuData.EogL1 - apuData.EogR1, apuData.EogL2 - apuData.EogR2,
                                                0 - ((apuData.EogL1 + apuData.EogR1) / 2),
                                                0 - ((apuData.EogL2 + apuData.EogR2) / 2) });
        }

        /// <summary>
        /// アカデミアデータ（フル）受信
        /// </summary>
        /// <param name="data"></param>
        private MeasureBean receiveAcademia2(SensorDataArgs data)
        {
            APU_REPORT_ACADEMIA2 apuData = (APU_REPORT_ACADEMIA2)data;

            this.batteryLevel = apuData.Battery;

            //if (data.Marking)
            //{
            //    return new MeasureBean(data.Marking, ++this.sensorDataSerialNumber, this.getSensingTime(apuData.Counter), new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            //}

            //double accX = AxisMaster.AccelerationAxis.CalcAdValue(apuData.AccX);
            //double accY = AxisMaster.AccelerationAxis.CalcAdValue(apuData.AccY);
            //double accZ = AxisMaster.AccelerationAxis.CalcAdValue(apuData.AccZ);
            //double gyroX = AxisMaster.AngularVelocityAxis.CalcAdValue(apuData.GyroX);
            //double gyroY = AxisMaster.AngularVelocityAxis.CalcAdValue(apuData.GyroY);
            //double gyroZ = AxisMaster.AngularVelocityAxis.CalcAdValue(apuData.GyroZ);
            //double eogL2 = AxisMaster.ElectrooculographyAxis.CalcAdValue(apuData.EogL);
            //double eogR2 = AxisMaster.ElectrooculographyAxis.CalcAdValue(apuData.EogR);
            //double deltaH2 = eogL2 - eogR2;
            //double deltaV2 = 0 - ((eogL2 + eogR2) / 2);

            return new MeasureBean(data.Marking,
                                    ++this.sensorDataSerialNumber,
                                    this.getSensingTime(apuData.Counter), 
                                    new int[] { apuData.AccX, apuData.AccY, apuData.AccZ,
                                                apuData.GyroX, apuData.GyroY, apuData.GyroZ,
                                                apuData.EogL, apuData.EogR,
                                                apuData.EogL - apuData.EogR,
                                                0 - ((apuData.EogL + apuData.EogR) / 2) });
        }

        /// <summary>
        /// アカデミアデータ（クォータニオン）受信
        /// </summary>
        /// <param name="data"></param>
        private MeasureBean receiveAcademia3(SensorDataArgs data)
        {
            APU_REPORT_ACADEMIA3 apuData = (APU_REPORT_ACADEMIA3)data;

            this.batteryLevel = apuData.Battery;

            //if (data.Marking)
            //{
            //    return new MeasureBean(data.Marking, ++this.sensorDataSerialNumber, this.getSensingTime(apuData.Counter), new int[] { 0, 0, 0, 0 });
            //}

            //double qW = AxisMaster.QuaternionAxis.CalcAdValue(apuData.QuaternionW);
            //double qX = AxisMaster.QuaternionAxis.CalcAdValue(apuData.QuaternionX);
            //double qY = AxisMaster.QuaternionAxis.CalcAdValue(apuData.QuaternionY);
            //double qZ = AxisMaster.QuaternionAxis.CalcAdValue(apuData.QuaternionZ);

            return new MeasureBean(data.Marking,
                                    ++this.sensorDataSerialNumber,
                                    this.getSensingTime(apuData.Counter),
                                    new int[] { apuData.QuaternionW,
                                                apuData.QuaternionX,
                                                apuData.QuaternionY,
                                                apuData.QuaternionZ });
        }

        /// <summary>
        /// センサー履歴データ取得
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public List<MeasureBean> GetSensorHistory(int startTime, int endTime, ref double scaleXMax)
        {
            List<MeasureBean> result = new List<MeasureBean>();

            try
            {
                // 排他する
                measureDataMutex.WaitOne();

                int startIndex = startTime / 10 / this.transmissionSpeed;
                if (startIndex < 0)
                {
                    startIndex = 0;
                }
                int endIndex = (endTime / 10 / this.transmissionSpeed) + 1;
                if (endIndex + startIndex>= this.sensorHistoryList.Count)
                {
                    endIndex = this.sensorHistoryList.Count - 1;
                }
                if (startIndex < endIndex)
                {
                    result = this.sensorHistoryList.GetRange(startIndex, endIndex - startIndex);
                    scaleXMax = this.sensorHistoryList[this.sensorHistoryList.Count - 1].X / 1000;
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                // Mutexロック解放
                measureDataMutex.ReleaseMutex();
            }
            return result;
        }
    }
}
