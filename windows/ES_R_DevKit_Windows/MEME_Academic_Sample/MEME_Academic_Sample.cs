using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MEMELib_Academic;
using static MEMELib_Academic.MEMELib;

namespace MEME_Academic_Sample
{
    public partial class MEME_Academic_Sample : Form
    {
        /// <summary>
        /// MEMELib
        /// </summary>
        MEMELib memeLib;
        /// <summary>
        /// シリアルポートオープン状態
        /// </summary>
        bool isOpenPort;
        /// <summary>
        /// MEME接続状態
        /// </summary>
        bool isConnectedPeripheral;
        /// <summary>
        /// 計測状態
        /// </summary>
        bool isStartMeasurement;
        /// <summary>
        /// フリーマーキング
        /// </summary>
        bool isFreeMarking;
        /// <summary>
        /// Saveファイル名
        /// </summary>
        string saveFileName;
        /// <summary>
        /// アカデミックモード(Full)
        /// </summary>
        MEMEMode mode;
        /// <summary>
        /// 計測品質(High)
        /// </summary>
        MEMEQuality quality;
        /// <summary>
        /// 3軸加速度センサー計測レンジ
        /// </summary>
        MEMEAccelRange accelRange;
        /// <summary>
        /// 3軸ジャイロセンサー計測レンジ
        /// </summary>
        MEMEGyroRange gyroRange;
        /// <summary>
        /// ハードウェアバージョン
        /// </summary>
        string hwVersion;
        /// <summary>
        /// ファームウェアバージョン
        /// </summary>
        string fwVersion;
        /// <summary>
        /// SDKバージョン
        /// </summary>
        string sdkVersion;

        /// <summary>
        /// MEME_Academic_Sample
        /// </summary>
        public MEME_Academic_Sample()
        {
            InitializeComponent();

            this.SetGUIParam();

            this.memeLib = new MEMELib();
            this.memeLib.memePeripheralFound += new memePeripheralFoundDelegate(this.memePeripheralFound);
            this.memeLib.memePeripheralConnected += new memePeripheralConnectedDelegate(this.memePeripheralConnected);
            this.memeLib.memePeripheralDisconnected += new memePeripheralDisconnectedDelegate(this.memePeripheralDisconnected);
            this.memeLib.memeAcademicFullDataReceived += new memeAcademicFullDataReceivedDelegate(this.memeAcademicFullDataReceived);

            this.isOpenPort = false;
            this.isConnectedPeripheral = false;
            this.isStartMeasurement = false;
            this.isFreeMarking = false;
        }

        #region MEMELib_delegate
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="result"></param>
        /// <param name="deviceAddress"></param>
        private void memePeripheralFound(object sender, MEMEStatus result, string deviceAddress)
        {
            if (result == MEMEStatus.MEMELIB_OK)
            {
                this.SetDeviceAddress(deviceAddress);
            }
            else if (result == MEMEStatus.MEMELIB_TIMEOUT)
            {
                if (this.cb_DeviceList.Items.Count > 0)
                {
                    this.SetTextColor(0x04, "Connect", Color.White, Color.Red);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="result"></param>
        private void memePeripheralConnected(object sender, MEMEStatus result)
        {
            if (result == MEMEStatus.MEMELIB_OK)
            {
                this.isConnectedPeripheral = true;
                this.SetConnectionStatus("Status : Connected");
                this.SetTextColor(0x04, "Disconnect", Color.White, Color.Red);
                this.SetTextColor(0x05, "Start Measurement", Color.White, Color.Red);

                this.fwVersion = this.memeLib.getFWVersion();
                this.deviceVersionLabel.Text = this.fwVersion;
                this.sdkVersion = this.memeLib.getSDKVersion();
                this.SDKVersionLabel.Text = this.sdkVersion;
                this.hwVersion = this.memeLib.getHWVersion();

                this.mode = this.memeLib.getMode();
                this.quality = this.memeLib.getQuality();
                this.accelRange = this.memeLib.getAccelRange();
                this.gyroRange = this.memeLib.getGyroRange();
            }
            else if (result == MEMEStatus.MEMELIB_TIMEOUT)
            {
                this.isConnectedPeripheral = false;
                this.SetConnectionStatus("Status : Disonnected");
                this.SetTextColor(0x04, "Connect", Color.White, Color.Red);
                this.SetTextColor(0x05, "Start Measurement", Color.Black, Color.LightGray);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="result"></param>
        private void memePeripheralDisconnected(object sender, MEMEStatus result)
        {
            if (result == MEMEStatus.MEMELIB_OK)
            {
                this.isConnectedPeripheral = false;
                this.SetConnectionStatus("Status : Disonnected");
                this.SetTextColor(0x04, "Connect", Color.White, Color.Red);
                this.SetTextColor(0x05, "Start Measurement", Color.Black, Color.LightGray);
            }
            else if (result == MEMEStatus.MEMELIB_TIMEOUT)
            {
                this.isConnectedPeripheral = false;
                this.SetConnectionStatus("Status : Disonnected");
                this.SetTextColor(0x04, "Connect", Color.White, Color.Red);
                this.SetTextColor(0x05, "Start Measurement", Color.Black, Color.LightGray);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sendor"></param>
        /// <param name="fullData"></param>
        private void memeAcademicFullDataReceived(object sendor, AcademicFullData fullData)
        {
            int no = 0;
            this.SetData(no++, fullData.Cnt.ToString("D"));
            this.SetData(no++, fullData.BattLv.ToString("D"));
            this.SetData(no++, fullData.AccX.ToString("D"));
            this.SetData(no++, fullData.AccY.ToString("D"));
            this.SetData(no++, fullData.AccZ.ToString("D"));
            this.SetData(no++, fullData.GyroX.ToString("D"));
            this.SetData(no++, fullData.GyroY.ToString("D"));
            this.SetData(no++, fullData.GyroZ.ToString("D"));
            this.SetData(no++, fullData.EogL.ToString("D"));
            this.SetData(no++, fullData.EogR.ToString("D"));
            this.SetData(no++, fullData.EogH.ToString("D"));
            this.SetData(no++, fullData.EogV.ToString("D"));
            this.saveCsvData(fullData);
        }
        #endregion

        #region GUI
        /// <summary>
        /// SetGUIParam
        /// </summary>
        private void SetGUIParam()
        {
            this.cb_ModeSelect.Items.Add("Full");
            this.cb_ModeSelect.SelectedIndex = 0;
            this.cb_TransmissionSpeed.Items.Add("High");
            this.cb_TransmissionSpeed.SelectedIndex = 0;
            this.cb_AccRange.Items.Add("±2G");
            this.cb_AccRange.Items.Add("±4G");
            this.cb_AccRange.Items.Add("±8G");
            this.cb_AccRange.Items.Add("±16G");
            this.cb_AccRange.SelectedIndex = 0;
            this.cb_GyroRange.Items.Add("±250dps");
            this.cb_GyroRange.Items.Add("±500dps");
            this.cb_GyroRange.Items.Add("±1000dps");
            this.cb_GyroRange.Items.Add("±2000dps");
            this.cb_GyroRange.SelectedIndex = 0;
        }
        /// <summary>
        /// SetTextColor
        /// </summary>
        /// <param name="no"></param>
        /// <param name="text"></param>
        /// <param name="foreColor"></param>
        /// <param name="backColor"></param>
        delegate void SetTextColorCallback(int no, String text, Color foreColor, Color backColor);
        private void SetTextColor(int no, String text, Color foreColor, Color backColor)
        {
            switch (no)
            {
                case 0x01:
                    if (this.bt_ScanPort.InvokeRequired)
                    {
                        SetTextColorCallback d = new SetTextColorCallback(SetTextColor);
                        this.Invoke(d, new object[] { no, text, foreColor, backColor });
                    }
                    else
                    {
                        this.bt_ScanPort.Text = text;
                        this.bt_ScanPort.ForeColor = foreColor;
                        this.bt_ScanPort.BackColor = backColor;
                    }
                    break;

                case 0x02:
                    if (this.bt_OpenPort.InvokeRequired)
                    {
                        SetTextColorCallback d = new SetTextColorCallback(SetTextColor);
                        this.Invoke(d, new object[] { no, text, foreColor, backColor });
                    }
                    else
                    {
                        this.bt_OpenPort.Text = text;
                        this.bt_OpenPort.ForeColor = foreColor;
                        this.bt_OpenPort.BackColor = backColor;
                    }
                    break;

                case 0x03:
                    if (this.bt_ScanPeripheral.InvokeRequired)
                    {
                        SetTextColorCallback d = new SetTextColorCallback(SetTextColor);
                        this.Invoke(d, new object[] { no, text, foreColor, backColor });
                    }
                    else
                    {
                        this.bt_ScanPeripheral.Text = text;
                        this.bt_ScanPeripheral.ForeColor = foreColor;
                        this.bt_ScanPeripheral.BackColor = backColor;
                    }
                    break;

                case 0x04:
                    if (this.bt_ConnectPeripheral.InvokeRequired)
                    {
                        SetTextColorCallback d = new SetTextColorCallback(SetTextColor);
                        this.Invoke(d, new object[] { no, text, foreColor, backColor });
                    }
                    else
                    {
                        this.bt_ConnectPeripheral.Text = text;
                        this.bt_ConnectPeripheral.ForeColor = foreColor;
                        this.bt_ConnectPeripheral.BackColor = backColor;
                    }
                    break;

                case 0x05:
                    if (this.bt_StartMeasurement.InvokeRequired)
                    {
                        SetTextColorCallback d = new SetTextColorCallback(SetTextColor);
                        this.Invoke(d, new object[] { no, text, foreColor, backColor });
                    }
                    else
                    {
                        this.bt_StartMeasurement.Text = text;
                        this.bt_StartMeasurement.ForeColor = foreColor;
                        this.bt_StartMeasurement.BackColor = backColor;
                    }
                    break;

                default:
                    break;
            }
        }
        /// <summary>
        /// SetDeviceAddress
        /// </summary>
        /// <param name="deviceAddress"></param>
        delegate void SetDeviceAddressCallback(string deviceAddress);
        private void SetDeviceAddress(string deviceAddress)
        {
            if (this.cb_DeviceList.InvokeRequired)
            {
                SetDeviceAddressCallback d = new SetDeviceAddressCallback(SetDeviceAddress);
                this.Invoke(d, new object[] { deviceAddress });
            }
            else
            {
                this.cb_DeviceList.Items.Add(deviceAddress);
                this.cb_DeviceList.SelectedIndex = 0;
            }
        }
        /// <summary>
        /// SetConnectionStatus
        /// </summary>
        /// <param name="Status"></param>
        delegate void SetConnectionStatusCallback(String Status);
        private void SetConnectionStatus(String Status)
        {
            if (this.lb_ConnectionStatus.InvokeRequired)
            {
                SetConnectionStatusCallback d = new SetConnectionStatusCallback(SetConnectionStatus);
                this.Invoke(d, new object[] { Status });
            }
            else
            {
                this.lb_ConnectionStatus.Text = Status;
            }
        }
        /// <summary>
        /// SetData
        /// </summary>
        /// <param name="no"></param>
        /// <param name="data"></param>
        delegate void SetDataCallback(int no, String data);
        private void SetData(int no, String data)
        {
            switch(no)
            {
                case 0:
                    if (this.lb_DataCnt.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataCnt.Text = data; }
                    break;

                case 1:
                    if (this.lb_DataBattLv.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataBattLv.Text = data; }
                    break;

                case 2:
                    if (this.lb_DataAccX.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataAccX.Text = data; }
                    break;

                case 3:
                    if (this.lb_DataAccY.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataAccY.Text = data; }
                    break;

                case 4:
                    if (this.lb_DataAccZ.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataAccZ.Text = data; }
                    break;

                case 5:
                    if (this.lb_DataGyroX.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataGyroX.Text = data; }
                    break;

                case 6:
                    if (this.lb_DataGyroY.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataGyroY.Text = data; }
                    break;

                case 7:
                    if (this.lb_DataGyroZ.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataGyroZ.Text = data; }
                    break;

                case 8:
                    if (this.lb_DataEogL.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataEogL.Text = data; }
                    break;

                case 9:
                    if (this.lb_DataEogR.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataEogR.Text = data; }
                    break;

                case 10:
                    if (this.lb_DataEogH.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataEogH.Text = data; }
                    break;

                case 11:
                    if (this.lb_DataEogV.InvokeRequired)
                    {
                        SetDataCallback d = new SetDataCallback(SetData);
                        this.Invoke(d, new object[] { no, data });
                    }
                    else { this.lb_DataEogV.Text = data; }
                    break;

                default:
                    break;
            }
        }
        #endregion

        #region Button Event
        /// <summary>
        /// bt_ScanPort_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_ScanPort_Click(object sender, EventArgs e)
        {
            this.cb_PortList.Items.Clear();
            List<string> portNameList = new List<string>();
            portNameList = memeLib.GetComPortNameList();
            for (int i = 0; i < portNameList.Count; i++)
            {
                this.cb_PortList.Items.Add(portNameList[i]);
                this.cb_PortList.SelectedIndex = i;
            }
        }
        /// <summary>
        /// bt_OpenPort_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_OpenPort_Click(object sender, EventArgs e)
        {
            if (this.isOpenPort == false)
            {
                string portName = cb_PortList.SelectedItem.ToString();
                MEMEStatus result = memeLib.ConnectComPort(portName);
                if (result == MEMEStatus.MEMELIB_OK)
                {
                    this.SetTextColor(0x01, "Scan Port", Color.Black, Color.LightGray);
                    this.SetTextColor(0x02, "Close", Color.White, Color.Red);
                    this.SetTextColor(0x03, "Scan MEME", Color.White, Color.Red);
                    this.isOpenPort = true;
                }
                else
                {
                    this.SetTextColor(0x01, "Scan Port", Color.White, Color.Red);
                    this.SetTextColor(0x02, "Open", Color.White, Color.Red);
                    this.SetTextColor(0x03, "Scan MEME", Color.Black, Color.LightGray);
                    this.isOpenPort = false;
                }
            }
            else
            {
                this.memeLib.DisconnectComPort();
                this.isOpenPort = false;
                this.SetTextColor(0x01, "Scan Port", Color.White, Color.Red);
                this.SetTextColor(0x02, "Open", Color.Black, Color.LightGray);
                this.SetTextColor(0x03, "Scan MEME", Color.Black, Color.LightGray);
                this.SetTextColor(0x04, "Connect", Color.Black, Color.LightGray);
            }
        }
        /// <summary>
        /// bt_ScanPeripheral_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_ScanPeripheral_Click(object sender, EventArgs e)
        {
            this.SetTextColor(0x04, "Connect", Color.Black, Color.LightGray);
            this.cb_DeviceList.Items.Clear();
            this.memeLib.startScanningPeripherals();
        }
        /// <summary>
        /// bt_ConnectPeripheral_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_ConnectPeripheral_Click(object sender, EventArgs e)
        {
            if (this.isConnectedPeripheral == false)
            {
                string deviceAddress = cb_DeviceList.SelectedItem.ToString();
                this.memeLib.connectPeripheral(deviceAddress);
            }
            else
            {
                this.memeLib.disconnectPeripheral();
            }
        }
        /// <summary>
        /// bt_StartMeasurement_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_StartMeasurement_Click(object sender, EventArgs e)
        {
            if (isStartMeasurement == false)
            {
                this.accelRange = (MEMEAccelRange)cb_AccRange.SelectedIndex;
                this.memeLib.setAccelRange(this.accelRange);
                this.gyroRange = (MEMEGyroRange)cb_GyroRange.SelectedIndex;
                this.memeLib.setGyroRange(this.gyroRange);

                this.isFreeMarking = false;
                this.memeLib.startDataReport();
                this.isStartMeasurement = true;
                this.SetTextColor(0x05, "Stop Measurement", Color.White, Color.Red);
                this.saveCsvHeader();
            }
            else
            {
                this.memeLib.stopDataReport();
                this.isStartMeasurement = false;
                this.SetTextColor(0x05, "Start Measurement", Color.White, Color.Red);
            }
        }
        /// <summary>
        /// freeMarkingButton_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void freeMarkingButton_Click(object sender, EventArgs e)
        {
            this.isFreeMarking = true;
        }
        /// <summary>
        /// versionToolStripMenuItem_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void versionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VersionForm form = new VersionForm();
            form.ShowDialog();
        }
        #endregion

        #region Save Data
        /// <summary>
        /// saveCsvHeader
        /// </summary>
        private void saveCsvHeader()
        {
            System.IO.Directory.CreateDirectory("Result");

            string writeData;
            string BTAddr = cb_DeviceList.SelectedItem.ToString();
            string saveFileTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            saveFileName = BTAddr + "_" + saveFileTime + ".csv";
            if (this.mode == MEMEMode.Full) { writeData = "// Data mode  : Full"; }
            else { writeData = "// Data mode  : Full"; }
            this.memeLib.saveData("Result", saveFileName, writeData);

            if (this.quality == MEMEQuality.High) { writeData = "// Transmission speed  : 100Hz"; }
            else { writeData = "// Transmission speed  : 100Hz"; }
            this.memeLib.saveData("Result", saveFileName, writeData);

            if (this.accelRange == MEMEAccelRange.Range2G) { writeData = "// Accelerometer sensor's range  : 2G"; }
            else if (this.accelRange == MEMEAccelRange.Range4G) { writeData = "// Accelerometer sensor's range  : 4G"; }
            else if (this.accelRange == MEMEAccelRange.Range8G) { writeData = "// Accelerometer sensor's range  : 8G"; }
            else { writeData = "// Accelerometer sensor's range  : 16G"; }
            this.memeLib.saveData("Result", saveFileName, writeData);

            if (this.gyroRange == MEMEGyroRange.Range250dps) { writeData = "// Gyroscope sensor's range  : 250dps"; }
            else if (this.gyroRange == MEMEGyroRange.Range500dps) { writeData = "// Gyroscope sensor's range  : 500dps"; }
            else if (this.gyroRange == MEMEGyroRange.Range1000dps) { writeData = "// Gyroscope sensor's range  : 1000dps"; }
            else { writeData = "// Gyroscope sensor's range  : 2000dps"; }
            this.memeLib.saveData("Result", saveFileName, writeData);

            writeData = "//";
            this.memeLib.saveData("Result", saveFileName, writeData);

            writeData = "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V,BattLv";
            this.memeLib.saveData("Result", saveFileName, writeData);
        }
        /// <summary>
        /// saveCsvData
        /// </summary>
        /// <param name="fullData"></param>
        private void saveCsvData(AcademicFullData fullData)
        {
            // Save Data
            DateTime targetTime = DateTime.Now;
            string sYear = targetTime.Year.ToString("D4");
            string sMonth = targetTime.Month.ToString("D2");
            string sDay = targetTime.Day.ToString("D2");
            string sHour = targetTime.Hour.ToString("D2");
            string sMinute = targetTime.Minute.ToString("D2");
            string sSecond = targetTime.Second.ToString("D2");
            string sMsecond = targetTime.Millisecond.ToString("D3");
            string sTime = sYear + "/" + sMonth + "/" + sDay + " " + sHour + ":" + sMinute + ":" + sSecond + "." + sMsecond;

            string freeMarking = null;
            if (this.isFreeMarking == true)
            {
                freeMarking = "X";
                this.isFreeMarking = false;
            }

            string writeData =
                freeMarking + "," +
                fullData.Cnt.ToString("D") + "," +
                sTime + "," +
                fullData.AccX.ToString("D") + "," +
                fullData.AccY.ToString("D") + "," +
                fullData.AccZ.ToString("D") + "," +
                fullData.GyroX.ToString("D") + "," +
                fullData.GyroY.ToString("D") + "," +
                fullData.GyroZ.ToString("D") + "," +
                fullData.EogL.ToString("D") + "," +
                fullData.EogR.ToString("D") + "," +
                fullData.EogH.ToString("D") + "," +
                fullData.EogV.ToString("D") + "," +
                fullData.BattLv.ToString("D") + ",";
            this.memeLib.saveData("Result", saveFileName, writeData);
        }
        #endregion
    }
}
