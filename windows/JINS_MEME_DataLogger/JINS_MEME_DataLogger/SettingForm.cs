using System;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 動作設定画面
    /// </summary>
    public partial class SettingForm : Form
    {
        /// <summary>
        /// 設定ファイル
        /// </summary>
        private SettingTable settings;

        /// <summary>
        /// 保存フォルダ
        /// </summary>
        private string saveFolder = string.Empty;
        public string SaveFolder
        {
            get { return this.saveFolder; }
        }

        /// <summary>
        /// ファイル保存ダイアログ表示指定
        /// </summary>
        private bool showSaveFileDialog = false;
        public bool ShowSaveFileDialog
        {
            get { return this.showSaveFileDialog; }
        }

        /// <summary>
        /// 外部出力ソケット使用の有無
        /// </summary>
        private bool useSocket = false;
        public bool UseSocket
        {
            get { return this.useSocket; }
        }

        /// <summary>
        /// 外部出力ソケットローカルポート
        /// </summary>
        private int socketPort = 60000;
        public int SocketPort
        {
            get { return this.socketPort; }
        }

        /// <summary>
        /// 外部出力ソケットローカルアドレス
        /// </summary>
        private string socketAddress = string.Empty;
        public string SocketAddress
        {
            get { return this.socketAddress; }
        }

        /// <summary>
        /// 外部出力ソケットローカルアドレスリスト
        /// </summary>
        //private List<string> socketAddressList = new List<string>();
        //public List<string> SocketAddressList
        //{
        //    get { return this.socketAddressList; }
        //}

        /// <summary>
        /// マーキング時間設定値(ms)
        /// </summary>
        private int markingTimeValue = 1000;
        public int MarkingTimeValue
        {
            get { return this.markingTimeValue; }
        }

        /// <summary>
        /// ファイル日付フォーマット指定
        /// </summary>
        private int recordFileDateFormat = 0;
        public int RecordFileDateFormat
        {
            get { return this.recordFileDateFormat; }
        }

        /// <summary>
        /// 加速度Ｘ軸オフセット
        /// </summary>
        private int accXAxisOffset = 0;
        public int AccXAxisOffset
        {
            get { return this.accXAxisOffset; }
        }

        /// <summary>
        /// 加速度Ｙ軸オフセット
        /// </summary>
        private int accYAxisOffset = 0;
        public int AccYAxisOffset
        {
            get { return this.accYAxisOffset; }
        }

        /// <summary>
        /// 加速度Ｚ軸オフセット
        /// </summary>
        private int accZAxisOffset = 0;
        public int AccZAxisOffset
        {
            get { return this.accZAxisOffset; }
        }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SettingForm(SettingTable settingTable)
        {
            InitializeComponent();

            this.settings = settingTable;

            //string defaultPath = Path.Combine(System.Windows.Forms.Application.LocalUserAppDataPath, "SensorData");
            //String defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            //                    Application.CompanyName, Application.ProductName, "SensorData");

            string sensorDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                                    System.Windows.Forms.Application.CompanyName,
                                                    //System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion,
                                                    "MEME academic",
                                                    "SensorData");

            // ファイル保存フォルダ確認
            this.saveFolder = this.settings.GetStringValue("OperationSetting", "SaveFolder", sensorDataPath);
            if (Directory.Exists(this.saveFolder) == false)
            {
                this.saveFolder = sensorDataPath;
                if (Directory.Exists(this.saveFolder) == false)
                {
                    // フォルダを作成
                    Directory.CreateDirectory(this.saveFolder);
                }
            }

            // ファイル保存ダイアログ表示設定
            this.showSaveFileDialog = this.settings.GetBool("OperationSetting", "ShowSaveFileDialog", this.showSaveFileDialog);

            // 外部出力ソケット使用
            this.useSocket = this.settings.GetBool("OperationSetting", "UseSocket", this.useSocket);

            // 外部出力ソケットローカルポート
            this.socketPort = this.settings.GetInteger("OperationSetting", "SocketPort", this.socketPort);

            // 外部出力ソケットローカルアドレス
            this.socketAddress = this.settings.GetStringValue("OperationSetting", "SocketAddress", this.socketAddress);
            
            // ローカルアドレス一覧取得
            bool match = false;
            IPAddress[] ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            for(int index=0 ; index<ipAddress.Length ; index++)
            {
                if (ipAddress[index].AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }
                if(this.socketAddress.Equals(ipAddress[index].ToString()))
                {
                    match = true;
                    this.socketAddressCombo.Items.Insert(0, ipAddress[index].ToString());
                }
                else
                {
                    this.socketAddressCombo.Items.Add(ipAddress[index].ToString());
                }
            }
            if (match)
            {
                this.socketAddressCombo.SelectedIndex = 0;
            }
            else
            {
                this.socketAddress = string.Empty;
                this.socketAddressCombo.SelectedIndex = -1;
            }

            // マーキング時間確認
            this.markingTimeValue = this.settings.GetInteger("OperationSetting", "MarkingTime", this.markingTimeValue);
            if (this.markingTimeValue < 0)
            {
                this.markingTimeValue = 0;
            }

            // ファイル日付フォーマット確認
            this.recordFileDateFormat = this.settings.GetInteger("OperationSetting", "RecordFileDateFormat", this.recordFileDateFormat);

            // 加速度オフセット取得
            this.accXAxisOffset = this.settings.GetInteger("OperationSetting", "AccXAxisOffset", this.accXAxisOffset);
            this.accYAxisOffset = this.settings.GetInteger("OperationSetting", "AccYAxisOffset", this.accYAxisOffset);
            this.accZAxisOffset = this.settings.GetInteger("OperationSetting", "AccZAxisOffset", this.accZAxisOffset);
        }

        /// <summary>
        /// フォームロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingForm_Load(object sender, EventArgs e)
        {
            // コントロールに値を設定
            this.saveFolderText.Text = this.saveFolder;
            this.recordFileDateFormatCombo.SelectedIndex = this.recordFileDateFormat;
            this.showSaveFileDialogCheck.Checked = this.showSaveFileDialog;
            this.useSocketCheck.Checked = this.useSocket;
            this.externalSocketGroup.Enabled = this.useSocket;
            this.socketPortText.Text = Convert.ToString(this.socketPort);
            this.markingTimeText.Text = Convert.ToString(this.markingTimeValue);
            this.accXAxisOffsetText.Text = Convert.ToString(this.accXAxisOffset);
            this.accYAxisOffsetText.Text = Convert.ToString(this.accYAxisOffset);
            this.accZAxisOffsetText.Text = Convert.ToString(this.accZAxisOffset);
        }

        /// <summary>
        /// 確定ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void applyButton_Click(object sender, EventArgs e)
        {
            // 指定フォルダが存在するか？
            string folder = this.saveFolderText.Text;
            if (Directory.Exists(folder) == false)
            {
                Common.ShowMessageBox("Save folder error.", "Save setting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }            

            // 外部出力ソケットローカルポートを数値変換
            int port = 0;
            if (this.changeInteger(this.socketPortText.Text, ref port) == false)
            {
                Common.ShowMessageBox("External output socket port error.", "Save setting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // マーキングデータ数を数値変換
            int timeValue = 0;
            bool result = this.changeInteger(this.markingTimeText.Text, ref timeValue);
            if ((result == false) || (timeValue < 0))
            {
                Common.ShowMessageBox("Marking time error.", "Save setting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 加速度オフセット値
            int[] offset = new int[3];
            TextBox[] textBox = new TextBox[] { this.accXAxisOffsetText, this.accYAxisOffsetText, this.accZAxisOffsetText };
            for (int index = 0; index < offset.Length; index++)
            {
                if (this.changeInteger(textBox[index].Text, ref offset[index]) == false)
                {
                    Common.ShowMessageBox("Accelerometer DC offset error.", "Save setting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }


            this.saveFolder = folder;
            this.recordFileDateFormat = this.recordFileDateFormatCombo.SelectedIndex;
            this.showSaveFileDialog = this.showSaveFileDialogCheck.Checked;
            this.useSocket = this.useSocketCheck.Checked;
            this.socketPort = port;
            this.socketAddress = this.socketAddressCombo.Text;

            this.markingTimeValue = timeValue;
            this.accXAxisOffset = offset[0];
            this.accYAxisOffset = offset[1];
            this.accZAxisOffset = offset[2];

            this.settings.SetStringValue("OperationSetting", "SaveFolder", this.saveFolder);
            this.settings.SetInteger("OperationSetting", "RecordFileDateFormat", this.recordFileDateFormat);
            this.settings.SetBool("OperationSetting", "ShowSaveFileDialog", this.showSaveFileDialog);
            this.settings.SetBool("OperationSetting", "UseSocket", this.useSocket);
            this.settings.SetInteger("OperationSetting", "SocketPort", this.socketPort);
            this.settings.SetStringValue("OperationSetting", "SocketAddress", this.socketAddress);
            this.settings.SetInteger("OperationSetting", "MarkingTime", this.markingTimeValue);
            this.settings.SetInteger("OperationSetting", "DateFormat", this.recordFileDateFormatCombo.SelectedIndex);
            this.settings.SetInteger("OperationSetting", "AccXAxisOffset", this.accXAxisOffset);
            this.settings.SetInteger("OperationSetting", "AccYAxisOffset", this.accYAxisOffset);
            this.settings.SetInteger("OperationSetting", "AccZAxisOffset", this.accZAxisOffset);

            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// string --> int変換
        /// </summary>
        /// <param name="srcValue"></param>
        /// <param name="changeValue"></param>
        /// <returns></returns>
        private bool changeInteger(string srcValue, ref int changeValue)
        {
            bool result = false;
            try
            {
                changeValue = int.Parse(srcValue);
                result = true;
            }
            catch(Exception ex)
            {
                Tracer.WriteException(ex);
            }
            return result;
        }

        /// <summary>
        /// string --> double変換
        /// </summary>
        /// <param name="srcValue"></param>
        /// <param name="changeValue"></param>
        /// <returns></returns>
        private bool changeDouble(string srcValue, ref double changeValue)
        {
            bool result = false;
            try
            {
                changeValue = double.Parse(srcValue);
                result = true;
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            return result;
        }

        /// <summary>
        /// キャンセルボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// フォルダ参照ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveFolderReferenceButton_Click(object sender, EventArgs e)
        {
            this.folderBrowserDialog.Description = "Select destination folder for the sensor data.";
            this.folderBrowserDialog.SelectedPath = this.saveFolder;
            if (this.folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
            {
                // 選択されたフォルダを表示する
                this.saveFolderText.Text = this.folderBrowserDialog.SelectedPath;
            }
        }

        /// <summary>
        /// フォルダオープンボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveFolderOpenButton_Click(object sender, EventArgs e)
        {
            string folder = this.saveFolderText.Text;
            if (Directory.Exists(folder) == false)
            {
                Common.ShowMessageBox(string.Format("Missing folder...{0}", folder), "Setting", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                System.Diagnostics.Process.Start(folder);
            }
        }

        /// <summary>
        /// 外部出力ソケット使用チェック変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void useSocketCheck_CheckedChanged(object sender, EventArgs e)
        {
            this.externalSocketGroup.Enabled = this.useSocketCheck.Checked;
        }

    }
}
