using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// ファイルアクセスを行う為の処理を記載します。
    /// </summary>
    public class FileAccess
    {
        /// <summary>
        /// ファイルデータアクセスMutex
        /// </summary>
        private static Mutex fileDataMutex = new Mutex();

        /// <summary>
        /// データアクセスMutex
        /// </summary>
        private static Mutex measureDataMutex = new Mutex();

        /// <summary>
        /// 保存ファイル名
        /// </summary>
        private string saveFilePath = string.Empty;

        /// <summary>
        /// 日付時刻フォーマット
        /// </summary>
        private string dateTimeFormat = "yyyy/MM/dd hh:mm:ss.fff";

        /// <summary>
        /// データヘッダ
        /// </summary>
        private string dataHeader = string.Empty;

        /// <summary>
        /// 測定モード
        /// </summary>
        private byte measureMode = 0;

        /// <summary>
        /// 測定送信速度（データ品質）
        /// </summary>
        private byte measureTransmissionSpeed = 0;

        /// <summary>
        /// 測定加速度レンジ
        /// </summary>
        private byte measureAccRange = 0;

        /// <summary>
        /// 測定角速度レンジ
        /// </summary>
        private byte measureGyroRange = 0;


        /// <summary>
        /// ファイル書き込みスレッド処理ループ状態
        /// </summary>
        private bool fileWriteThreadLoop = false;
        /// <summary>
        /// ファイル書き込みスレッド
        /// </summary>
        private Thread fileWriteThread = null;

        /// <summary>
        /// ファイル読み込みスレッド処理ループ状態
        /// </summary>
        private bool fileReadThreadLoop = false;

        /// <summary>
        /// ファイル読み込みスレッド
        /// </summary>
        private Thread fileReadThread = null;

        /// <summary>
        /// ファイル読み込み開始時刻
        /// </summary>
        private DateTime fileReadStartTime;

        /// <summary>
        /// ファイル読み込み一時停止時刻
        /// </summary>
        private DateTime fileReadPauseTime;

        /// <summary>
        /// ファイルデータリスト
        /// </summary>
        private List<MeasureBean> fileDataList = new List<MeasureBean>();

        /// <summary>
        /// 通知済みカウント
        /// </summary>
        private int notificationCount = 0;

        /// <summary>
        /// センサーデータ履歴リスト
        /// </summary>
        private List<MeasureBean> fileHistoryList = new List<MeasureBean>();

        /// <summary>
        /// ファイル読込み進捗通知デリゲート宣言
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public delegate void FileReadProgressEventHandler(int percent);

        /// <summary>
        /// ファイル読込み進捗通知イベント宣言
        /// </summary>
        public event FileReadProgressEventHandler FileReadProgressEvent;

        /// <summary>
        /// ファイルデータ終了通知デリゲート宣言
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public delegate void EndFileDataEventHandler();

        /// <summary>
        /// ファイルデータ終了通知イベント宣言
        /// </summary>
        public event EndFileDataEventHandler EndFileDataEvent;

        /// <summary>
        /// ソケット状態イベント定義
        /// </summary>
        /// <param name="progress"></param>
        public delegate void SocketStatusHandler(string status);
        /// <summary>
        /// ソケット状態通知イベント
        /// </summary>
        public event SocketStatusHandler SocketStatusEvent = null;


        /// <summary>
        /// センサーデータ保存フォルダ
        /// </summary>
        public string SaveFolder
        {
            get;
            set;
        }

        /// <summary>
        /// 日付フォーマット
        /// </summary>
        public int RecordFileDateFormat
        {
            get;
            set;
        }

        /// <summary>
        /// プロット（一括表示）状態
        /// </summary>
        public bool PlotData
        {
            get;
            set;
        }

        /// <summary>
        /// ファイルデータ行数
        /// </summary>
        private int fileDataLineCount = 0;

        /// <summary>
        /// 次回通知パーセント
        /// </summary>
        private int nextNotifyPercent = 0;

        /// <summary>
        /// ＴＣＰサーバー通信
        /// </summary>
        private TCPServer tcpServer = new TCPServer();

        /// <summary>
        /// 外部出力ソケット使用の有無
        /// </summary>
        public bool UseSocket
        {
            get;
            set;
        }

        /// <summary>
        /// 外部出力ソケットローカルポート
        /// </summary>
        private int socketPort = -1;
        public int SocketPort
        {
            get { return this.socketPort; }
            set
            {
                int oldPort = this.socketPort;

                this.socketPort = value;

                // 外部出力未使用？
                if (this.UseSocket == false)
                {
                    this.tcpServer.Close();
                }
                else
                {
                    // ポート変更、および、オープン状態確認
                    if ((this.socketPort != oldPort) || (this.tcpServer.IsOpen == false))
                    {
                        this.tcpServer.Close();

                        // アドレス指定確認
                        if (this.socketAddress.Equals(string.Empty) == false)
                        {
                            this.tcpServer.Open(this.socketAddress, this.socketPort);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 外部出力ソケットローカルアドレス
        /// </summary>
        private string socketAddress = string.Empty;
        public string SocketAddress
        {
            get { return this.socketAddress; }
            set
            {
                string oldAddress = this.socketAddress;
                this.socketAddress = value;

                // 外部出力未使用？
                if (this.UseSocket == false)
                {
                    this.tcpServer.Close();
                }
                else
                {
                    // アドレス変更、および、オープン状態確認
                    if ((this.socketAddress.Equals(oldAddress) == false) || (this.tcpServer.IsOpen == false))
                    {
                        this.tcpServer.Close();

                        // ポート指定確認
                        if (this.socketPort != -1)
                        {
                            this.tcpServer.Open(this.socketAddress, this.socketPort);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FileAccess()
        {
            this.UseSocket = false;
            //this.tcpServer.Open();

            this.tcpServer.SocketStatusEvent += new TCPServer.SocketStatusHandler(this.OnSocketStatus);
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~FileAccess()
        {
            this.RecordStop();
            this.tcpServer.Close();
        }

        /// <summary>
        /// センサーデータ記録開始
        /// </summary>
        /// <returns></returns>
        public void RecordStart(string saveFilePath, int dateFormat,
                                byte measureMode, byte transmissionSpeed, byte measureAccRange, byte measureGyroRange)
        {
            Tracer.WriteInformation("Save file name = {0}", saveFilePath);
            this.saveFilePath = saveFilePath;
            switch (dateFormat)
            {
                case 1:
                    //this.dateTimeFormat = "MM/dd/yyyy HH:mm:ss.fff";
                    this.dateTimeFormat = "MM/dd/yyyy HH:mm:ss.ff";
                    break;
                case 2:
                    //this.dateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    this.dateTimeFormat = "yyyy-MM-dd HH:mm:ss.ff";
                    break;
                case 3:
                    //this.dateTimeFormat = "MM-dd-yyyy HH:mm:ss.fff";
                    this.dateTimeFormat = "MM-dd-yyyy HH:mm:ss.ff";
                    break;
                default:
                    //this.dateTimeFormat = "yyyy/MM/dd HH:mm:ss.fff";
                    this.dateTimeFormat = "yyyy/MM/dd HH:mm:ss.ff";
                    break;
            }
            switch (measureMode)
            {
                case 1:
                    this.dataHeader = "ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,EOG_L1,EOG_R1,EOG_L2,EOG_R2,EOG_H1,EOG_H2,EOG_V1,EOG_V2";
                    break;
                case 2:
                    this.dataHeader = "ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V";
                    break;
                case 3:
                    this.dataHeader = "ARTIFACT,NUM,DATE,QUATERNION_W,QUATERNION_X,QUATERNION_Y,QUATERNION_Z";
                    break;
                default:
                    this.dataHeader = string.Empty;
                    break;
            }
            this.measureMode = measureMode;
            this.measureTransmissionSpeed = transmissionSpeed;
            this.measureAccRange = measureAccRange;
            this.measureGyroRange = measureGyroRange;

            this.fileDataList.Clear();
            this.fileHistoryList.Clear();
            this.fileWriteThreadLoop = true;
            this.fileWriteThread = new Thread(new ThreadStart(this.writeSensorData));
            this.fileWriteThread.Name = "File write thread";
            this.fileWriteThread.Start();
        }

        /// <summary>
        /// センサーデータ記録停止
        /// </summary>
        public void RecordStop()
        {
            if (this.fileWriteThread != null)
            {
                this.fileWriteThreadLoop = false;
                this.fileWriteThread.Join(500);
                this.fileWriteThread = null;
            }
        }


        /// <summary>
        /// センサーデータ書き込みスレッド
        /// </summary>
        private void writeSensorData()
        {
            // UTF-8（BOMなし）エンコード未指定でもＯＫ。
            Encoding enc = new UTF8Encoding(false);
            // UTF-8（BOMあり）
            //Encoding enc = new UTF8Encoding(true);
            
            FileStream fs = new FileStream(this.saveFilePath, FileMode.CreateNew);
            StreamWriter sw = new StreamWriter(fs, enc);

            sw.WriteLine(string.Format("{0}{1}", Constants.FILE_HEADER_DATA_MODE, this.changeMeasureMode(this.measureMode)));
            sw.WriteLine(string.Format("{0}{1}", Constants.FILE_HEADER_TRANSMISSION_SPEED, this.changeTransmissionSpeed(this.measureTransmissionSpeed)));
            sw.WriteLine(string.Format("{0}{1}", Constants.FILE_HEADER_ACC_RANGE, this.changeMeasureAccRange(this.measureAccRange)));
            sw.WriteLine(string.Format("{0}{1}", Constants.FILE_HEADER_GYRO_RANGE, this.changeMeasureGyroRange(this.measureGyroRange)));
            sw.WriteLine(Constants.FILE_HEADER_COMMENT);

            if(this.dataHeader.Equals(string.Empty) == false)
            {
                sw.WriteLine(string.Format("{0}{1}", Constants.FILE_HEADER_COMMENT, this.dataHeader));
            }
            
            DateTime utcNow = DateTime.UtcNow;
            DateTime utcAdd = utcNow;
            string yData = string.Empty;
            string lineData = string.Empty;

            try
            {
                while (this.fileWriteThreadLoop)
                {
                    List<MeasureBean> sensorDataList = this.getSensorData();
                    if (sensorDataList == null)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    lineData = string.Empty;
                    for (int index = 0; index < sensorDataList.Count; index++)
                    {
                        MeasureBean bean = sensorDataList[index];

                        utcAdd = utcNow.AddMilliseconds(bean.X);
                        lineData += string.Format("{0},{1},{2},{3}", bean.Marking == true ? "X" : "",
                                                                        bean.SerialNumber,
                                                                        utcAdd.ToString(this.dateTimeFormat), 
                                                                        bean.Y[0]);
                        for (int yIndex = 1; yIndex < bean.Y.Length; yIndex++)
                        {
                            lineData += string.Format(",{0}", bean.Y[yIndex]);
                        }
                        lineData += "\r\n";
                    }
                    sw.Write(lineData);
                    this.tcpServer.SetSendData(lineData);
                }
            }
            finally
            {
                sw.Close();
                fs.Close();
            }
        }

        /// <summary>
        /// データモード文字列変換
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        private string changeMeasureMode(byte mode)
        {
            string result = string.Empty;

            switch (mode)
            {
                case 1:
                    result = "Standard";
                    break;
                case 2:
                    result = "Full";
                    break;
                case 3:
                    result = "Quaternion";
                    break;
                default:
                    result = mode.ToString();
                    break;
            }

            return result;
        }

        /// <summary>
        /// 送信速度（データ品質）文字列変換
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        private string changeTransmissionSpeed(byte speed)
        {
            string result = string.Empty;

            switch(speed)
            {
                case 1:
                    result = "100Hz";
                    break;
                case 2:
                    result = "50Hz";
                    break;
                default:
                    result = speed.ToString();
                    break;
            }

            return result;
        }

        /// <summary>
        /// 加速度レンジ文字列変換
        /// </summary>
        /// <param name="rangeValue"></param>
        /// <returns></returns>
        private string changeMeasureAccRange(byte rangeValue)
        {
            string result = string.Empty;

            switch (rangeValue)
            {
                case 0:
                    result = "2g";
                    break;
                case 1:
                    result = "4g";
                    break;
                case 2:
                    result = "8g";
                    break;
                case 3:
                    result = "16g";
                    break;
                default:
                    result = rangeValue.ToString();
                    break;
            }

            return result;
        }

        /// <summary>
        /// 角速度レンジ文字列変換
        /// </summary>
        /// <param name="rangeValue"></param>
        /// <returns></returns>
        private string changeMeasureGyroRange(byte rangeValue)
        {
            string result = string.Empty;

            switch (rangeValue)
            {
                case 0:
                    result = "250dps";
                    break;
                case 1:
                    result = "500dps";
                    break;
                case 2:
                    result = "1000dps";
                    break;
                case 3:
                    result = "2000dps";
                    break;
                default:
                    result = rangeValue.ToString();
                    break;
            }

            return result;
        }

        /// <summary>
        /// センサーデータ取得
        /// </summary>
        /// <returns></returns>
        private List<MeasureBean> getSensorData()
        {
            List<MeasureBean> list = new List<MeasureBean>();

            try
            {
                // 排他
                fileDataMutex.WaitOne();

                int dataCount = this.fileDataList.Count;
                if (dataCount == 0)
                {
                    return null;
                }

                for (int index = 0; index < dataCount; index++)
                {
                    list.Add(this.fileDataList[index]);
                }
                this.fileDataList.RemoveRange(0, dataCount);
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                // 排他解放
                fileDataMutex.ReleaseMutex();
            }

            return list;
        }

        /// <summary>
        /// センサーデータ追加
        /// </summary>
        /// <returns></returns>
        public void AddSensorData(List<MeasureBean> list)
        {
            try
            {
                // 排他
                fileDataMutex.WaitOne();

                foreach (MeasureBean bean in list)
                {
                    this.fileDataList.Add(bean);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                // 排他解放
                fileDataMutex.ReleaseMutex();
            }
        }


        /// <summary>
        /// 履歴ファイルパス
        /// </summary>
        private string historyFilePath = string.Empty;
        public string HistoryFilePath
        {
            get { return this.historyFilePath; }
        }

        /// <summary>
        /// 履歴データ種別
        /// </summary>
        private int historyDataMode = 0;
        public int HistoryDataMode
        {
            get { return this.historyDataMode; }
        }

        /// <summary>
        /// 履歴送信速度（データ品質）
        /// </summary>
        private int historyTransmissionSpeed = 0;
        public int HistoryTransmissionSpeed
        {
            get { return this.historyTransmissionSpeed; }
        }


        /// <summary>
        /// ファイルデータ開始行
        /// </summary>
        private int fileDataStartLine = 0;

        /// <summary>
        /// ファイル日付フォーマット
        /// </summary>
        private int fileDataDateFormat = 0;

        /// <summary>
        /// ファイルデータ開始日時
        /// </summary>
        private DateTime fileDataFromDateTime;

        /// <summary>
        /// ファイルデータ終了日時
        /// </summary>
        private DateTime fileDataToDateTime;

        /// <summary>
        /// 履歴再生開始日時
        /// </summary>
        private DateTime replayFromDateTime;

        /// <summary>
        /// 履歴再生終了日時
        /// </summary>
        private DateTime replayToDateTime;

        /// <summary>
        /// 履歴再生速度
        /// </summary>
        private int replaySpeed = 1;

        /// <summary>
        /// 履歴再生一時停止状態
        /// </summary>
        private bool replayPause = false;


        /// <summary>
        /// 履歴ファイル情報取得
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="mode"></param>
        /// <param name="speed"></param>
        /// <param name="accRange"></param>
        /// <param name="gyroRange"></param>
        /// <param name="fromDateTime"></param>
        /// <param name="toDateTime"></param>
        /// <returns></returns>
        public bool GetHistoryFileInfo(string filePath, ref string mode, ref string speed, ref string accRange, ref string gyroRange, ref DateTime fromDateTime, ref DateTime toDateTime)
        {
            bool result = false;
            FileStream fs = null;
            StreamReader sr = null;

            try
            {
                this.fileHistoryList.Clear();

                // ファイルの存在確認
                if (File.Exists(filePath) == false)
                {
                    return false;
                }

                // ファイルオープン
                fs = new FileStream(filePath, FileMode.Open, System.IO.FileAccess.Read);
                sr = new StreamReader(fs);

                // ヘッダから各種情報を取得
                this.fileDataStartLine = 0;
                string line = string.Empty;
                while(sr.Peek() >= 0)
                {
                    this.fileDataStartLine++;

                    line = sr.ReadLine();
                    if (line.Substring(0, 2).Equals(Constants.FILE_HEADER_COMMENT) == false)
                    {
                        // ヘッダー終了
                        break;
                    }
                    if (line.IndexOf(Constants.FILE_HEADER_DATA_MODE) >= 0)
                    {
                        mode = line.Substring(Constants.FILE_HEADER_DATA_MODE.Length, line.Length - Constants.FILE_HEADER_DATA_MODE.Length);
                    }
                    else if (line.IndexOf(Constants.FILE_HEADER_TRANSMISSION_SPEED) >= 0)
                    {
                        speed = line.Substring(Constants.FILE_HEADER_TRANSMISSION_SPEED.Length, line.Length - Constants.FILE_HEADER_TRANSMISSION_SPEED.Length);
                    }
                    else if (line.IndexOf(Constants.FILE_HEADER_ACC_RANGE) >= 0)
                    {
                        accRange = line.Substring(Constants.FILE_HEADER_ACC_RANGE.Length, line.Length - Constants.FILE_HEADER_ACC_RANGE.Length);
                    }
                    else if (line.IndexOf(Constants.FILE_HEADER_GYRO_RANGE) >= 0)
                    {
                        gyroRange = line.Substring(Constants.FILE_HEADER_GYRO_RANGE.Length, line.Length - Constants.FILE_HEADER_GYRO_RANGE.Length);
                    }
                }

                // 日付フォーマットを取得する。
                this.fileDataDateFormat = this.getFileDateFormat(line);
                if (this.fileDataDateFormat == -1)
                {
                    return false;
                }

                // 最初の日付時刻取得
                if (this.getLineDateTime(line, this.fileDataDateFormat, ref fromDateTime) == false)
                {
                    return false;
                }

                // 最後の行まで読み飛ばす。
                this.fileDataLineCount = 1;
                while (sr.Peek() > 0)
                {
                    line = sr.ReadLine();
                    this.fileDataLineCount++;
                }

                // 最後の日付時刻取得
                if (this.getLineDateTime(line, this.fileDataDateFormat, ref toDateTime) == false)
                {
                    return false;
                }

                // データモード変換
                if(mode.Equals("Standard"))
                {
                    this.historyDataMode = 1;
                }
                else if (mode.Equals("Full"))
                {
                    this.historyDataMode = 2;
                }
                else if (mode.Equals("Quaternion"))
                {
                    this.historyDataMode = 3;
                }
                else
                {
                    this.historyDataMode = Convert.ToInt32(mode);
                }

                this.historyFilePath = filePath;
                this.historyTransmissionSpeed = speed.Equals("50Hz") == true ? 2 : 1;
                this.fileDataFromDateTime = fromDateTime;
                this.fileDataToDateTime = toDateTime;

                result = true;

            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                if(sr != null)
                {
                    sr.Close();
                }
                if(fs != null)
                {
                    fs.Close();
                }
            }

            return result;
        }

        /// <summary>
        /// ファイル日付フォーマット取得
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private int getFileDateFormat(string line)
        {
            int format = -1;

            string[] splitLine = line.Split(',');
            if (splitLine.Length < 3)
            {
                // 日付対象データが無い場合
                return -1;
            }
            if (splitLine[2].Length < 22)
            {
                // 日付対象データ長が短い（ミリ秒の末尾が無いので、全体で22桁）
                return -1;
            }

            int dateFormatSlash = splitLine[2].IndexOf('/');
            int dateFormatMinus = splitLine[2].IndexOf('-');

            if (dateFormatSlash == 4)
            {
                // 日付フォーマット："yyyy/MM/dd hh:mm:ss.ff"
                format = 0;
                this.changeSensorDateTime = this.changeDateTimeFormatYMD;
            }
            else if (dateFormatSlash == 2)
            {
                // 日付フォーマット："MM/dd/yyyy hh:mm:ss.ff"
                this.changeSensorDateTime = this.changeDateTimeFormatMDY;
                format = 1;
            }
            else if (dateFormatMinus == 4)
            {
                // 日付フォーマット："yyyy-MM-dd hh:mm:ss.ff"
                this.changeSensorDateTime = this.changeDateTimeFormatYMD;
                format = 2;
            }
            else if (dateFormatMinus == 2)
            {
                // 日付フォーマット："MM-dd-yyyy hh:mm:ss.ff"
                this.changeSensorDateTime = this.changeDateTimeFormatMDY;
                format = 3;
            }

            return format;
        }

        /// <summary>
        /// 日付時刻取得
        /// </summary>
        /// <param name="line"></param>
        /// <param name="fileDateFormat"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private bool getLineDateTime(string line, int fileDateFormat, ref DateTime dateTime)
        {
            bool result = false;

            try
            {
                string dateTimeString = line.Split(',')[2] + "0";
                string[] date;
                if ((fileDateFormat == 0) || (fileDateFormat == 1))
                {
                    date = dateTimeString.Substring(0, 10).Split('/');
                }
                else
                {
                    date = dateTimeString.Substring(0, 10).Split('-');
                    string year = date[2];
                    date[2] = date[1];
                    date[1] = date[0];
                    date[0] = year;
                }

                string[] time;
                if ((fileDateFormat == 0) || (fileDateFormat == 2))
                {
                    time = dateTimeString.Substring(11, 12).Split(':');
                }
                else
                {
                    time = dateTimeString.Substring(11, 12).Split(':');
                }
                
                string[] millSec = time[2].Split('.');

                dateTime = new DateTime(Convert.ToInt32(date[0]), Convert.ToInt32(date[1]), Convert.ToInt32(date[2]),
                                        Convert.ToInt32(time[0]), Convert.ToInt32(time[1]), Convert.ToInt32(millSec[0]), Convert.ToInt32(millSec[1]));
                result = true;
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// 履歴再生開始
        /// </summary>
        /// <param name="replaySpeed"></param>
        /// <param name="fromDateTime"></param>
        /// <param name="toDateTime"></param>
        /// <returns></returns>
        public string ReplayStart(int replaySpeed, string fromDateTimeString, string toDateTimeString)
        {
            string result = string.Empty;

            // 日付フォーマットチェック
            if (DateTime.TryParse(fromDateTimeString, out this.replayFromDateTime) == false)
            {
                return "Abnormal date format. (from)";
            }
            if (DateTime.TryParse(toDateTimeString, out this.replayToDateTime) == false)
            {
                return "Abnormal date format. (to)";
            }

            if ((this.replayFromDateTime.CompareTo(this.replayToDateTime) != -1) ||
                (this.replayFromDateTime.CompareTo(this.fileDataToDateTime) != -1) ||
                (this.replayToDateTime.CompareTo(this.fileDataFromDateTime) == -1))
            {
                return "Abnormal date range.";
            }

            this.replaySpeed = replaySpeed;
            this.replayPause = false;
            this.fileReadStartTime = DateTime.Now;
            this.fileDataList.Clear();
            this.notificationCount = 0;
            this.fileHistoryList.Clear();
            this.PlotData = false;

            // ファイル読み込みスレッド開始
            this.fileReadThreadLoop = true;
            this.fileReadThread = new Thread(new ThreadStart(this.readSensorData));
            this.fileReadThread.Name = "File read thread";
            this.fileReadThread.Start();

            return result;
        }

        /// <summary>
        /// 履歴再生一時停止／再開
        /// </summary>
        public void RelpayPause(bool pause)
        {
            if (pause)
            {
                // 履歴再生一時停止
                this.replayPause = true;
                this.fileReadPauseTime = DateTime.Now;
            }
            else
            {
                // 履歴再生再開
                TimeSpan timeSpan = DateTime.Now - this.fileReadPauseTime;
                this.fileReadStartTime = this.fileReadStartTime.Add(timeSpan);
                this.replayPause = false;
            }
        }

        /// <summary>
        /// 履歴再生終了
        /// </summary>
        public void ReplayStop()
        {
            if (this.fileReadThread != null)
            {
                this.fileReadThreadLoop = false;
                this.fileReadThread.Join(500);
                this.fileReadThread = null;

                // 再生終了イベントを通知
                if (this.EndFileDataEvent != null)
                {
                    this.EndFileDataEvent();
                }
            }
        }

        /// <summary>
        /// ファイルデータ数取得
        /// </summary>
        /// <returns></returns>
        public int GetFileDataCount()
        {
            int listCount = 0;

            try
            {
                // 排他する
                measureDataMutex.WaitOne();

                listCount = this.fileDataList.Count;
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

            return listCount;
        }

        /// <summary>
        /// ファイルデータ取得
        /// </summary>
        /// <returns></returns>
        public List<MeasureBean> GetFileData()
        {
            List<MeasureBean> result = new List<MeasureBean>();

            if (this.replayPause)
            {
                return result;
            }

            try
            {
                // 排他する
                measureDataMutex.WaitOne();

                if (this.PlotData == false)
                {
                    // 経過時間算出
                    TimeSpan timeSpan = DateTime.Now - this.fileReadStartTime;
                    long milliSec = Convert.ToUInt32(timeSpan.TotalMilliseconds * this.replaySpeed);

                    // 経過時間を過ぎたデータを通知する
                    int listCount = this.fileDataList.Count;
                    if (listCount > 0)
                    {
                        for (int index = this.notificationCount; index < listCount; index++)
                        {
                            MeasureBean bean = this.fileDataList[index];
                            if (bean.X > milliSec)
                            {
                                break;
                            }
                            result.Add(bean);
                            this.notificationCount++;
                        }
                    }
                }
                else
                {
                    this.notificationCount = this.fileDataList.Count;
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

                if ((this.fileReadEnd) && (this.fileDataList.Count == this.notificationCount))
                {
                    this.fileReadEnd = false;
                    // 再生終了イベントを通知
                    if (this.EndFileDataEvent != null)
                    {
                        this.EndFileDataEvent();
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 最終ファイルデータ取得
        /// </summary>
        /// <returns></returns>
        public MeasureBean GetLastFileData()
        {
            if(this.fileDataList.Count == 0)
            {
                return null;
            }
            return this.fileDataList[this.fileDataList.Count - 1];
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

                int startIndex = startTime / 10 / this.historyTransmissionSpeed;
                if (startIndex < 0)
                {
                    startIndex = 0;
                }
                int endIndex = (endTime / 10 / this.historyTransmissionSpeed) + 1;
                if (endIndex >= this.notificationCount)
                {
                    endIndex = this.notificationCount;
                }
                if (startIndex < endIndex)
                {
                    result = this.fileDataList.GetRange(startIndex, endIndex - startIndex);

                    for (--startIndex; startIndex > 0; startIndex--)
                    {
                        if (this.fileDataList[startIndex].X < startTime)
                        {
                            break;
                        }
                        result.Insert(0, this.fileDataList[startIndex]);
                    }
                    for(++endIndex; endIndex<this.notificationCount; endIndex++)
                    {
                        if(this.fileDataList[endIndex].X > endTime)
                        {
                            break;
                        }
                        result.Add(this.fileDataList[endIndex]);
                    }
                    scaleXMax = (this.fileDataList[this.notificationCount - 1].X + 0.5) / 1000;
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

        /// <summary>
        /// ファイル読み込み終了
        /// </summary>
        private bool fileReadEnd = false;

        /// <summary>
        /// センサーデータ読み込みスレッド
        /// </summary>
        private void readSensorData()
        {
            FileStream fs = null;
            StreamReader sr = null;

            this.fileReadEnd = false;

            try
            {
                // ファイルの存在確認
                if (File.Exists(this.historyFilePath) == false)
                {
                    return;
                }

                // ファイルオープン
                fs = new FileStream(this.historyFilePath, FileMode.Open, System.IO.FileAccess.Read);
                sr = new StreamReader(fs);

                // ヘッダの情報を読み飛ばす
                string line = string.Empty;
                int lineCount = 1;
                do
                {
                    line = sr.ReadLine();
                    if (line == null)
                    {
                        return;
                    }
                    lineCount++;
                } while ((lineCount <= this.fileDataStartLine) && (this.fileReadThreadLoop == true));

                // 開始時刻データを検索
                int readLineCount = 0;
                this.fileReadProgress(readLineCount);
                DateTime dateTime = new DateTime();
                do
                {
                    if (this.changeSensorDateTime(line, ref dateTime) == true)
                    {
                        if (dateTime.CompareTo(this.replayFromDateTime) >= 0)
                        {
                            break;
                        }
                    }

                    // 次行読み込み
                    line = sr.ReadLine();
                    if (line == null)
                    {
                        return;
                    }
                    this.fileReadProgress(++readLineCount);
                } while (this.fileReadThreadLoop == true);

                if (this.fileReadThreadLoop == true)
                {
                    // センサーデータの通知、終了時刻データを検索
                    do
                    {
                        if (this.changeSensorDateTime(line, ref dateTime) == true)
                        {
                            if (dateTime.CompareTo(this.replayToDateTime) > 0)
                            {
                                break;
                            }
                            // センサーデータ通知
                            this.setAcademiaData(line, dateTime);
                        }

                        // 次行読み込み
                        line = sr.ReadLine();
                        if (line == null)
                        {
                            break;
                        }
                        this.fileReadProgress(++readLineCount);
                    } while (this.fileReadThreadLoop == true);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
                if (fs != null)
                {
                    fs.Close();
                }
                this.fileReadProgress(this.fileDataLineCount);
                this.fileReadEnd = true;
            }
        }

        /// <summary>
        /// ファイル読み込み進捗
        /// </summary>
        /// <param name="readCount"></param>
        private void fileReadProgress(int readCount)
        {
            if (this.FileReadProgressEvent == null)
            {
                return;
            }

            // ファイル読み込み進捗通知
            if (readCount == 0)
            {
                this.FileReadProgressEvent(0);
                this.nextNotifyPercent = 10;
            }
            else
            {
                int par = Convert.ToInt32(((double)readCount / this.fileDataLineCount) * 100);
                if (this.nextNotifyPercent <= par)
                {
                    this.FileReadProgressEvent(par);
                    this.nextNotifyPercent += 10;
                }
            }
        }

        /// <summary>
        /// アカデミアデータ設定
        /// </summary>
        /// <param name="lineData"></param>
        private void setAcademiaData(string lineData, DateTime dateTime)
        {
            try
            {
                // 排他する
                measureDataMutex.WaitOne();

                string[] fileString = lineData.Split(',');

                // フリーマーキング列、通番列、日付時刻列の３列を除いた列を数値データとみなす。
                int numericDataLength = fileString.Length - 3;
                int[] numericData = new int[numericDataLength];
                for (int srcIndex = 3, dstIndex = 0; dstIndex < numericDataLength; srcIndex++, dstIndex++)
                {
                    numericData[dstIndex] = Convert.ToInt32(fileString[srcIndex]);
                }

                // ファイルデータ登録
                this.fileDataList.Add(new MeasureBean(fileString[0].Equals("X"),
                                                        Convert.ToInt64(fileString[1]),
                                                        Convert.ToUInt32((dateTime - this.replayFromDateTime).TotalMilliseconds),
                                                        numericData));

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
        /// センサー日付時刻変更関数定義
        /// </summary>
        /// <param name="fileLineData"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private delegate bool changeSensorDateTimeHandler(string fileLineData, ref DateTime dateTime);

        /// <summary>
        /// センサー日付時刻変更関数
        /// </summary>
        private changeSensorDateTimeHandler changeSensorDateTime = null;

        /// <summary>
        /// 日付変換："yyyy/MM/dd hh:mm:ss.ff" or "yyyy-MM-dd hh:mm:ss.ff"
        /// </summary>
        /// <param name="fileLineData"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private bool changeDateTimeFormatYMD(string fileLineData, ref DateTime dateTime)
        {
            bool result = false;

            try
            {
                string dateTimeString = fileLineData.Split(',')[2] + "0";
                dateTime = new DateTime(Convert.ToInt32(dateTimeString.Substring(0, 4)),
                                        Convert.ToInt32(dateTimeString.Substring(5, 2)),
                                        Convert.ToInt32(dateTimeString.Substring(8, 2)),
                                        Convert.ToInt32(dateTimeString.Substring(11, 2)),
                                        Convert.ToInt32(dateTimeString.Substring(14, 2)),
                                        Convert.ToInt32(dateTimeString.Substring(17, 2)),
                                        Convert.ToInt32(dateTimeString.Substring(20, 3)));
                result = true;
            }
            catch(Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// 日付変換："MM/dd/yyyy hh:mm:ss.ff" or "MM-dd-yyyy hh:mm:ss.ff"
        /// </summary>
        /// <param name="fileLineData"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private bool changeDateTimeFormatMDY(string fileLineData, ref DateTime dateTime)
        {
            bool result = false;

            try
            {
                string dateTimeString = fileLineData.Split(',')[2] + "0";
                dateTime = new DateTime(Convert.ToInt32(dateTimeString.Substring(0, 2)),
                                        Convert.ToInt32(dateTimeString.Substring(3, 2)),
                                        Convert.ToInt32(dateTimeString.Substring(6, 4)),
                                        Convert.ToInt32(dateTimeString.Substring(11, 2)),
                                        Convert.ToInt32(dateTimeString.Substring(14, 2)),
                                        Convert.ToInt32(dateTimeString.Substring(17, 2)),
                                        Convert.ToInt32(dateTimeString.Substring(20, 3)));
                result = true;
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// ソケットを閉じる
        /// </summary>
        public void CloseSocket()
        {
            this.tcpServer.Close();
        }

        /// <summary>
        /// ソケット状態通知
        /// </summary>
        /// <param name="status"></param>
        private void OnSocketStatus(string status)
        {
            this.SocketStatusEvent(status);
        }
    }
}
