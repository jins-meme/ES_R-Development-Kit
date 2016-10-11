using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Runtime.InteropServices;
using DataEncryption;

namespace JINS_MEME_DataLogger.Data
{
    /// <summary>
    /// シリアル通信の管理を行います。
    /// </summary>
    public class Communication
    {
        /// <summary>
        /// 符号化、暗号化ライブラリ
        /// </summary>
        /// <remarks>符号化、暗号化に利用</remarks>
        DataEncryption.Process enc = new DataEncryption.Process();

        /// <summary>
        /// シリアルポート通信
        /// </summary>
        private SerialPort serialPort = null;

        /// <summary>
        /// シリアルポート接続状態取得
        /// </summary>
        public bool IsSerialConnect
        {
            get { return this.serialPort.IsOpen; } 
        }

        /// <summary>
        /// Bluetooth MACアドレス
        /// </summary>
        /// <remarks>符号化、暗号化に利用</remarks>
        public string BluetoothMacAddress
        {
            set
            {
                /// this.makeCipherKeys(value);
                enc.makeCipherKeys(value);
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Communication()
        {
            this.serialPort = new SerialPort();
            this.serialPort.DataReceived += new SerialDataReceivedEventHandler(this.serialPort_DataReceived);
        }

        /// <summary>
        /// 消滅
        /// </summary>
        public void Dispose()
        {
            this.comClose();
        }

        /// <summary>
        /// ComPort名一覧取得
        /// </summary>
        /// <returns>シリアルポート名一覧</returns>
        public List<string> GetComPortNameList()
        {
            List<string> portNameList = new List<string>();
            foreach (string comName in SerialPort.GetPortNames())
            {
                if (portNameList.Contains(comName) == false)
                {
                    portNameList.Add(comName);
                }
            }
            return portNameList;
        }

        /// <summary>
        /// ComPort接続
        /// </summary>
        /// <param name="portName"></param>
        /// <returns></returns>
        public bool ConnectComPort(string portName)
        {
            bool result = this.comOpen(portName);
            Tracer.WriteVerbose("Connect {0}  result={1}", portName, result);
            return result;
        }

        /// <summary>
        /// ComPort切断
        /// </summary>
        /// <returns></returns>
        public void DisconnectComPort()
        {
            this.comClose();
            Tracer.WriteVerbose("Disconnect {0}", serialPort.PortName);
        }
        
        /// <summary>
        /// シリアル回線オープン
        /// </summary>
        /// <param name="portName"></param>
        /// <returns></returns>
        private bool comOpen(string portName)
        {
            bool result = false;

            try
            {
                // 一度シリアル回線をクローズする
                this.comClose();

                this.serialPort.PortName = portName;
                this.serialPort.BaudRate = Constants.SERIAL_BAUDRATE;
                this.serialPort.DataBits = Constants.SERIAL_DATABITS;
                this.serialPort.Parity = Constants.SERIAL_PARITY;
                this.serialPort.StopBits = Constants.SERIAL_STOPBITS;
                this.serialPort.ReceivedBytesThreshold = 3;
                this.serialPort.ReadBufferSize = 4096 * 10;
                this.serialPort.ReadTimeout = 500;
                this.serialPort.WriteTimeout = 500;

                this.serialPort.Open();

                this.serialPort.DiscardInBuffer();
                this.serialPort.DiscardOutBuffer();
                this.serialPort.DtrEnable = true;
                this.serialPort.RtsEnable = true;

                result = true;
            }
            catch(Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// シリアル回線クローズ
        /// </summary>
        private void comClose()
        {
            if (this.serialPort.IsOpen)
            {
                this.serialPort.DtrEnable = false;
                this.serialPort.RtsEnable = false;

                this.serialPort.Close();
            }
        }

        /// <summary>
        /// コマンド送信
        /// </summary>
        /// <param name="sendData"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool SendCommand(byte[] sendData, int length, bool requestEncode)
        {
            bool result = false;

            try
            {
                if (this.serialPort.IsOpen)
                {
                    if (requestEncode)
                    {
                        //this.encord(ref sendData);
                        enc.encord(ref sendData);
                    }
                    this.serialPort.Write(sendData, 0, length);
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }

            return result;
        }

        /// <summary>
        /// センサーデータ保持バッファ
        /// </summary>
        private byte[] keepSensorDataBuff = new byte[1000];
        /// <summary>
        /// センサーデータ保持バッファ長
        /// </summary>
        private int keepSensorDataBuffLength = 1000;
        /// <summary>
        /// センサーデータ保持サイズ
        /// </summary>
        private int keepSensorDataSize = 0;


        /// <summary>
        /// センサーデータ通知デリゲート宣言
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public delegate void SensorDataEventHandler(object sender, SensorDataArgs args);

        /// <summary>
        /// センサーデータ通知イベント宣言
        /// </summary>
        public event SensorDataEventHandler SensorDataEvent;


        /// <summary>
        /// シリアルデータ受信
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int recvSize = this.serialPort.BytesToRead;
            if (recvSize == 0)
            {
                return;
            }
            byte[] recvData = new byte[recvSize];
            int readSize = this.serialPort.Read(recvData, 0, recvSize);
            if (readSize == 0)
            {
                return;
            }

            if (this.SensorDataEvent == null)
            {
                return;
            }

            // 保持バッファの拡張確認
            if (this.keepSensorDataBuffLength < this.keepSensorDataSize + readSize)
            {
                // バッファのリサイズ
                this.keepSensorDataBuffLength = this.keepSensorDataSize + readSize;
                Array.Resize(ref this.keepSensorDataBuff, this.keepSensorDataBuffLength);
            }
            // 保持バッファにコピー
            Array.Copy(recvData, 0, this.keepSensorDataBuff, this.keepSensorDataSize, readSize);
            this.keepSensorDataSize += readSize;


            int seekStartOffset = 0;
            while (true)
            {
                // イベントコード確認
                byte eventLength = 0;
                byte eventCode = 0;
                int eventOffset = 0;
                byte dataType = 0;
                bool result = this.checkCommandCode(seekStartOffset, ref eventLength, ref eventCode, ref eventOffset, ref dataType);
                if (result == false)
                {
                    // 保持データ内にイベントコードが見つからない
                    break;
                }
                seekStartOffset += eventLength;

                SensorDataArgs args = new SensorDataArgs();
                args.DataType = dataType;
                args.SensorData = new byte[eventLength];
                Array.Copy(this.keepSensorDataBuff, eventOffset, args.SensorData, 0, eventLength);
                if (dataType != 0x00)
                {
                    //this.decord(ref args.SensorData);
                    enc.decord(ref args.SensorData);
                }

                // センサーデータイベント通知
                this.SensorDataEvent(this, args);
            }

            // センサーデータを前に詰める
            this.keepSensorDataSize -= seekStartOffset;
            Array.Copy(this.keepSensorDataBuff, seekStartOffset, this.keepSensorDataBuff, 0, this.keepSensorDataSize);
        }

        /// <summary>
        /// イベントデータ長定義
        /// </summary>
        private byte[] defineEventLength = new byte[] { Constants.SIZE_AUP_REPORT_ACADEMIA1, Constants.SIZE_AUP_REPORT_ACADEMIA2,
                                                        Constants.SIZE_AUP_REPORT_ACADEMIA3,
                                                        Constants.SIZE_AUP_REPORT_DEV_INFO, Constants.SIZE_AUP_REPORT_MODE,
                                                        Constants.SIZE_AUP_REPORT_6AXIS_PARAMS, Constants.SIZE_AUP_REPORT_RESP,
                                                        Constants.SIZE_DON_RES_DEVICE_NAME, Constants.SIZE_DON_RES_DEVICE_VERSION,
                                                        Constants.SIZE_DON_RES_SCAN_BLUETOOTH, Constants.SIZE_DON_RES_CONNECT_BLUETOOTH,
                                                        Constants.SIZE_DON_RES_DISCONNECT_BLUETOOTH, };
        /// <summary>
        /// イベントコード定義
        /// </summary>
        private byte[] defineEventCode = new byte[] { Constants.CMD_AUP_REPORT_ACADEMIA1, Constants.CMD_AUP_REPORT_ACADEMIA2,
                                                        Constants.CMD_AUP_REPORT_ACADEMIA3, 
                                                        Constants.CMD_AUP_REPORT_DEV_INFO, Constants.CMD_AUP_REPORT_MODE,
                                                        Constants.CMD_AUP_REPORT_6AXIS_PARAMS, Constants.CMD_AUP_REPORT_RESP,
                                                        Constants.CMD_DONGLE, Constants.CMD_DONGLE,
                                                        Constants.CMD_DONGLE, Constants.CMD_DONGLE,
                                                        Constants.CMD_DONGLE, };


        /// <summary>
        /// データ種別
        /// </summary>
        /// <remarks>
        /// 0x01    デバイスからのアカデミアデータ（暗号化あり）
        /// 0x02    デバイスからのその他データ（暗号化あり）
        /// 0x00    ドングルからのデータ（暗号化なし）
        /// </remarks>
        private byte[] defineDataType = new byte[] { 0x01, 0x01,
                                                     0x01,
                                                     0x02, 0x02,
                                                     0x02, 0x02,
                                                     0x00, 0x00,
                                                     0x00, 0x00,
                                                     0x00, };

        /// <summary>
        /// イベントコード確認
        /// </summary>
        /// <param name="seekStartOffset">検出開始オフセット</param>
        /// <param name="eventLength">検出したイベント長</param>
        /// <param name="eventCode">検出したイベントコード</param>
        /// <param name="eventOffset">検出したイベントオフセット</param>
        /// <param name="dataType">データ種別</param>
        /// <returns>
        /// true        イベントコード検出
        /// false       イベントコードが見つからない
        /// </returns>
        private bool checkCommandCode(int seekStartOffset, ref byte eventLength, ref byte eventCode, ref int eventOffset, ref byte dataType)
        {
            bool exist = false;

            eventOffset = seekStartOffset;
            while(eventOffset < (this.keepSensorDataSize - 2))
            {
                for (int index = 0; index < defineEventCode.Length; index++)
                {
                    // イベントデータ長とイベント番号を確認
                    if ((this.keepSensorDataBuff[eventOffset] == defineEventLength[index]) &&
                        (this.keepSensorDataBuff[eventOffset + 1] == defineEventCode[index]))
                    {
                        // データが揃っているか？（末尾切れしていない？）
                        if ((this.keepSensorDataSize - eventOffset) >= defineEventLength[index])
                        {
                            eventLength = defineEventLength[index];
                            eventCode = defineEventCode[index];
                            dataType = defineDataType[index];
                            exist = true;
                        }
                        break;
                    }
                }
                if (exist == true)
                {
                    break;
                }
                eventOffset++;
            }

            return exist;
        }
    }
}
