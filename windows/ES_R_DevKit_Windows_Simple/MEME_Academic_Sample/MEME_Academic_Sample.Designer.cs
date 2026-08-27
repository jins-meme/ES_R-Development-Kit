namespace MEME_Academic_Sample
{
    partial class MEME_Academic_Sample
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        private void InitializeComponent()
        {
            this.bluetoothNameLabel = new System.Windows.Forms.Label();
            this.bt_ScanPeripheral = new System.Windows.Forms.Button();
            this.cb_DeviceList = new System.Windows.Forms.ComboBox();
            this.bt_ConnectPeripheral = new System.Windows.Forms.Button();
            this.lb_ConnectionStatus = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.measurementLabel = new System.Windows.Forms.Label();
            this.lb_ModeSelect = new System.Windows.Forms.Label();
            this.cb_ModeSelect = new System.Windows.Forms.ComboBox();
            this.lb_TransmissionSpeed = new System.Windows.Forms.Label();
            this.cb_TransmissionSpeed = new System.Windows.Forms.ComboBox();
            this.lb_AccRange = new System.Windows.Forms.Label();
            this.cb_AccRange = new System.Windows.Forms.ComboBox();
            this.lb_GyroRange = new System.Windows.Forms.Label();
            this.cb_GyroRange = new System.Windows.Forms.ComboBox();
            this.bt_StartMeasurement = new System.Windows.Forms.Button();
            this.freeMarkingButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lb_Cnt = new System.Windows.Forms.Label();
            this.lb_DataCnt = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lb_AccX = new System.Windows.Forms.Label();
            this.lb_DataAccX = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.lb_AccY = new System.Windows.Forms.Label();
            this.lb_DataAccY = new System.Windows.Forms.Label();
            this.panel5 = new System.Windows.Forms.Panel();
            this.lb_AccZ = new System.Windows.Forms.Label();
            this.lb_DataAccZ = new System.Windows.Forms.Label();
            this.panel6 = new System.Windows.Forms.Panel();
            this.lb_GyroX = new System.Windows.Forms.Label();
            this.lb_DataGyroX = new System.Windows.Forms.Label();
            this.panel9 = new System.Windows.Forms.Panel();
            this.lb_GyroY = new System.Windows.Forms.Label();
            this.lb_DataGyroY = new System.Windows.Forms.Label();
            this.panel8 = new System.Windows.Forms.Panel();
            this.lb_GyroZ = new System.Windows.Forms.Label();
            this.lb_DataGyroZ = new System.Windows.Forms.Label();
            this.panel7 = new System.Windows.Forms.Panel();
            this.lb_EogL = new System.Windows.Forms.Label();
            this.lb_DataEogL = new System.Windows.Forms.Label();
            this.panel10 = new System.Windows.Forms.Panel();
            this.lb_EogR = new System.Windows.Forms.Label();
            this.lb_DataEogR = new System.Windows.Forms.Label();
            this.panel11 = new System.Windows.Forms.Panel();
            this.lb_EogH = new System.Windows.Forms.Label();
            this.lb_DataEogH = new System.Windows.Forms.Label();
            this.panel13 = new System.Windows.Forms.Panel();
            this.lb_EogV = new System.Windows.Forms.Label();
            this.lb_DataEogV = new System.Windows.Forms.Label();
            this.panel12 = new System.Windows.Forms.Panel();
            this.lb_BattLv = new System.Windows.Forms.Label();
            this.lb_DataBattLv = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.quitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.versionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.deviceVersionTitleLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.deviceVersionLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.SDKVersionTitleLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.SDKVersionLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.groupBox1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            //
            // bluetoothNameLabel
            //
            this.bluetoothNameLabel.AutoSize = true;
            this.bluetoothNameLabel.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Bold);
            this.bluetoothNameLabel.ForeColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.bluetoothNameLabel.Location = new System.Drawing.Point(16, 40);
            this.bluetoothNameLabel.Name = "bluetoothNameLabel";
            this.bluetoothNameLabel.Size = new System.Drawing.Size(162, 15);
            this.bluetoothNameLabel.TabIndex = 0;
            this.bluetoothNameLabel.Text = "Connect to MEME device";
            //
            // bt_ScanPeripheral
            //
            this.bt_ScanPeripheral.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.bt_ScanPeripheral.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bt_ScanPeripheral.ForeColor = System.Drawing.Color.White;
            this.bt_ScanPeripheral.Location = new System.Drawing.Point(16, 64);
            this.bt_ScanPeripheral.Name = "bt_ScanPeripheral";
            this.bt_ScanPeripheral.Size = new System.Drawing.Size(80, 32);
            this.bt_ScanPeripheral.TabIndex = 1;
            this.bt_ScanPeripheral.Text = "Scan MEME";
            this.bt_ScanPeripheral.UseVisualStyleBackColor = false;
            this.bt_ScanPeripheral.Click += new System.EventHandler(this.bt_ScanPeripheral_Click);
            //
            // cb_DeviceList
            //
            this.cb_DeviceList.BackColor = System.Drawing.Color.LightGray;
            this.cb_DeviceList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_DeviceList.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cb_DeviceList.FormattingEnabled = true;
            this.cb_DeviceList.Location = new System.Drawing.Point(104, 72);
            this.cb_DeviceList.Name = "cb_DeviceList";
            this.cb_DeviceList.Size = new System.Drawing.Size(168, 23);
            this.cb_DeviceList.TabIndex = 2;
            //
            // bt_ConnectPeripheral
            //
            this.bt_ConnectPeripheral.BackColor = System.Drawing.Color.LightGray;
            this.bt_ConnectPeripheral.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bt_ConnectPeripheral.Location = new System.Drawing.Point(16, 112);
            this.bt_ConnectPeripheral.Name = "bt_ConnectPeripheral";
            this.bt_ConnectPeripheral.Size = new System.Drawing.Size(80, 32);
            this.bt_ConnectPeripheral.TabIndex = 3;
            this.bt_ConnectPeripheral.Text = "Connect";
            this.bt_ConnectPeripheral.UseVisualStyleBackColor = false;
            this.bt_ConnectPeripheral.Click += new System.EventHandler(this.bt_ConnectPeripheral_Click);
            //
            // lb_ConnectionStatus
            //
            this.lb_ConnectionStatus.BackColor = System.Drawing.Color.Transparent;
            this.lb_ConnectionStatus.Location = new System.Drawing.Point(112, 112);
            this.lb_ConnectionStatus.Name = "lb_ConnectionStatus";
            this.lb_ConnectionStatus.Size = new System.Drawing.Size(160, 24);
            this.lb_ConnectionStatus.TabIndex = 4;
            this.lb_ConnectionStatus.Text = "Status : Disconnected";
            this.lb_ConnectionStatus.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // panel1
            //
            this.panel1.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel1.Location = new System.Drawing.Point(104, 136);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(168, 2);
            this.panel1.TabIndex = 5;
            //
            // measurementLabel
            //
            this.measurementLabel.AutoSize = true;
            this.measurementLabel.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Bold);
            this.measurementLabel.ForeColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.measurementLabel.Location = new System.Drawing.Point(16, 176);
            this.measurementLabel.Name = "measurementLabel";
            this.measurementLabel.Size = new System.Drawing.Size(59, 15);
            this.measurementLabel.TabIndex = 6;
            this.measurementLabel.Text = "Measure";
            //
            // lb_ModeSelect
            //
            this.lb_ModeSelect.BackColor = System.Drawing.Color.Transparent;
            this.lb_ModeSelect.Location = new System.Drawing.Point(16, 200);
            this.lb_ModeSelect.Name = "lb_ModeSelect";
            this.lb_ModeSelect.Size = new System.Drawing.Size(168, 32);
            this.lb_ModeSelect.TabIndex = 7;
            this.lb_ModeSelect.Text = "Select mode";
            this.lb_ModeSelect.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cb_ModeSelect
            //
            this.cb_ModeSelect.BackColor = System.Drawing.Color.LightGray;
            this.cb_ModeSelect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_ModeSelect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cb_ModeSelect.FormattingEnabled = true;
            this.cb_ModeSelect.Location = new System.Drawing.Point(192, 204);
            this.cb_ModeSelect.Name = "cb_ModeSelect";
            this.cb_ModeSelect.Size = new System.Drawing.Size(80, 23);
            this.cb_ModeSelect.TabIndex = 8;
            //
            // lb_TransmissionSpeed
            //
            this.lb_TransmissionSpeed.BackColor = System.Drawing.Color.Transparent;
            this.lb_TransmissionSpeed.Location = new System.Drawing.Point(16, 240);
            this.lb_TransmissionSpeed.Name = "lb_TransmissionSpeed";
            this.lb_TransmissionSpeed.Size = new System.Drawing.Size(168, 32);
            this.lb_TransmissionSpeed.TabIndex = 9;
            this.lb_TransmissionSpeed.Text = "Transmission speed";
            this.lb_TransmissionSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cb_TransmissionSpeed
            //
            this.cb_TransmissionSpeed.BackColor = System.Drawing.Color.LightGray;
            this.cb_TransmissionSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_TransmissionSpeed.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cb_TransmissionSpeed.FormattingEnabled = true;
            this.cb_TransmissionSpeed.Location = new System.Drawing.Point(192, 244);
            this.cb_TransmissionSpeed.Name = "cb_TransmissionSpeed";
            this.cb_TransmissionSpeed.Size = new System.Drawing.Size(80, 23);
            this.cb_TransmissionSpeed.TabIndex = 10;
            //
            // lb_AccRange
            //
            this.lb_AccRange.BackColor = System.Drawing.Color.Transparent;
            this.lb_AccRange.Location = new System.Drawing.Point(16, 280);
            this.lb_AccRange.Name = "lb_AccRange";
            this.lb_AccRange.Size = new System.Drawing.Size(168, 32);
            this.lb_AccRange.TabIndex = 11;
            this.lb_AccRange.Text = "Measurement range of Accelerometer";
            this.lb_AccRange.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cb_AccRange
            //
            this.cb_AccRange.BackColor = System.Drawing.Color.LightGray;
            this.cb_AccRange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_AccRange.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cb_AccRange.FormattingEnabled = true;
            this.cb_AccRange.Location = new System.Drawing.Point(192, 284);
            this.cb_AccRange.Name = "cb_AccRange";
            this.cb_AccRange.Size = new System.Drawing.Size(80, 23);
            this.cb_AccRange.TabIndex = 12;
            //
            // lb_GyroRange
            //
            this.lb_GyroRange.BackColor = System.Drawing.Color.Transparent;
            this.lb_GyroRange.Location = new System.Drawing.Point(16, 320);
            this.lb_GyroRange.Name = "lb_GyroRange";
            this.lb_GyroRange.Size = new System.Drawing.Size(168, 32);
            this.lb_GyroRange.TabIndex = 13;
            this.lb_GyroRange.Text = "Measurement range of Gyroscope";
            this.lb_GyroRange.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cb_GyroRange
            //
            this.cb_GyroRange.BackColor = System.Drawing.Color.LightGray;
            this.cb_GyroRange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_GyroRange.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cb_GyroRange.FormattingEnabled = true;
            this.cb_GyroRange.Location = new System.Drawing.Point(192, 324);
            this.cb_GyroRange.Name = "cb_GyroRange";
            this.cb_GyroRange.Size = new System.Drawing.Size(80, 23);
            this.cb_GyroRange.TabIndex = 14;
            //
            // bt_StartMeasurement
            //
            this.bt_StartMeasurement.BackColor = System.Drawing.Color.LightGray;
            this.bt_StartMeasurement.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bt_StartMeasurement.Location = new System.Drawing.Point(16, 432);
            this.bt_StartMeasurement.Name = "bt_StartMeasurement";
            this.bt_StartMeasurement.Size = new System.Drawing.Size(130, 32);
            this.bt_StartMeasurement.TabIndex = 15;
            this.bt_StartMeasurement.Text = "Start Measurement";
            this.bt_StartMeasurement.UseVisualStyleBackColor = false;
            this.bt_StartMeasurement.Click += new System.EventHandler(this.bt_StartMeasurement_Click);
            //
            // freeMarkingButton
            //
            this.freeMarkingButton.BackColor = System.Drawing.Color.LightGray;
            this.freeMarkingButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.freeMarkingButton.Location = new System.Drawing.Point(152, 432);
            this.freeMarkingButton.Name = "freeMarkingButton";
            this.freeMarkingButton.Size = new System.Drawing.Size(120, 32);
            this.freeMarkingButton.TabIndex = 16;
            this.freeMarkingButton.Text = "Free Marking";
            this.freeMarkingButton.UseVisualStyleBackColor = false;
            this.freeMarkingButton.Click += new System.EventHandler(this.freeMarkingButton_Click);
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.lb_Cnt);
            this.groupBox1.Controls.Add(this.lb_DataCnt);
            this.groupBox1.Controls.Add(this.panel2);
            this.groupBox1.Controls.Add(this.lb_AccX);
            this.groupBox1.Controls.Add(this.lb_DataAccX);
            this.groupBox1.Controls.Add(this.panel4);
            this.groupBox1.Controls.Add(this.lb_AccY);
            this.groupBox1.Controls.Add(this.lb_DataAccY);
            this.groupBox1.Controls.Add(this.panel5);
            this.groupBox1.Controls.Add(this.lb_AccZ);
            this.groupBox1.Controls.Add(this.lb_DataAccZ);
            this.groupBox1.Controls.Add(this.panel6);
            this.groupBox1.Controls.Add(this.lb_GyroX);
            this.groupBox1.Controls.Add(this.lb_DataGyroX);
            this.groupBox1.Controls.Add(this.panel9);
            this.groupBox1.Controls.Add(this.lb_GyroY);
            this.groupBox1.Controls.Add(this.lb_DataGyroY);
            this.groupBox1.Controls.Add(this.panel8);
            this.groupBox1.Controls.Add(this.lb_GyroZ);
            this.groupBox1.Controls.Add(this.lb_DataGyroZ);
            this.groupBox1.Controls.Add(this.panel7);
            this.groupBox1.Controls.Add(this.lb_EogL);
            this.groupBox1.Controls.Add(this.lb_DataEogL);
            this.groupBox1.Controls.Add(this.panel10);
            this.groupBox1.Controls.Add(this.lb_EogR);
            this.groupBox1.Controls.Add(this.lb_DataEogR);
            this.groupBox1.Controls.Add(this.panel11);
            this.groupBox1.Controls.Add(this.lb_EogH);
            this.groupBox1.Controls.Add(this.lb_DataEogH);
            this.groupBox1.Controls.Add(this.panel13);
            this.groupBox1.Controls.Add(this.lb_EogV);
            this.groupBox1.Controls.Add(this.lb_DataEogV);
            this.groupBox1.Controls.Add(this.panel12);
            this.groupBox1.Controls.Add(this.lb_BattLv);
            this.groupBox1.Controls.Add(this.lb_DataBattLv);
            this.groupBox1.Controls.Add(this.panel3);
            this.groupBox1.Location = new System.Drawing.Point(288, 32);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 504);
            this.groupBox1.TabIndex = 17;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Sensor Data";
            //
            // lb_Cnt
            //
            this.lb_Cnt.Location = new System.Drawing.Point(8, 32);
            this.lb_Cnt.Name = "lb_Cnt";
            this.lb_Cnt.Size = new System.Drawing.Size(88, 24);
            this.lb_Cnt.Text = "Cnt";
            this.lb_Cnt.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataCnt
            //
            this.lb_DataCnt.Location = new System.Drawing.Point(104, 32);
            this.lb_DataCnt.Name = "lb_DataCnt";
            this.lb_DataCnt.Size = new System.Drawing.Size(88, 24);
            this.lb_DataCnt.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel2
            //
            this.panel2.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel2.Location = new System.Drawing.Point(8, 56);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(184, 2);
            //
            // lb_AccX
            //
            this.lb_AccX.Location = new System.Drawing.Point(8, 64);
            this.lb_AccX.Name = "lb_AccX";
            this.lb_AccX.Size = new System.Drawing.Size(88, 24);
            this.lb_AccX.Text = "AccX";
            this.lb_AccX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataAccX
            //
            this.lb_DataAccX.Location = new System.Drawing.Point(104, 64);
            this.lb_DataAccX.Name = "lb_DataAccX";
            this.lb_DataAccX.Size = new System.Drawing.Size(88, 24);
            this.lb_DataAccX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel4
            //
            this.panel4.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel4.Location = new System.Drawing.Point(8, 88);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(184, 2);
            //
            // lb_AccY
            //
            this.lb_AccY.Location = new System.Drawing.Point(8, 104);
            this.lb_AccY.Name = "lb_AccY";
            this.lb_AccY.Size = new System.Drawing.Size(88, 24);
            this.lb_AccY.Text = "AccY";
            this.lb_AccY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataAccY
            //
            this.lb_DataAccY.Location = new System.Drawing.Point(104, 104);
            this.lb_DataAccY.Name = "lb_DataAccY";
            this.lb_DataAccY.Size = new System.Drawing.Size(88, 24);
            this.lb_DataAccY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel5
            //
            this.panel5.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel5.Location = new System.Drawing.Point(8, 128);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(184, 2);
            //
            // lb_AccZ
            //
            this.lb_AccZ.Location = new System.Drawing.Point(8, 144);
            this.lb_AccZ.Name = "lb_AccZ";
            this.lb_AccZ.Size = new System.Drawing.Size(88, 24);
            this.lb_AccZ.Text = "AccZ";
            this.lb_AccZ.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataAccZ
            //
            this.lb_DataAccZ.Location = new System.Drawing.Point(104, 144);
            this.lb_DataAccZ.Name = "lb_DataAccZ";
            this.lb_DataAccZ.Size = new System.Drawing.Size(88, 24);
            this.lb_DataAccZ.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel6
            //
            this.panel6.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel6.Location = new System.Drawing.Point(8, 168);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(184, 2);
            //
            // lb_GyroX
            //
            this.lb_GyroX.Location = new System.Drawing.Point(8, 184);
            this.lb_GyroX.Name = "lb_GyroX";
            this.lb_GyroX.Size = new System.Drawing.Size(88, 24);
            this.lb_GyroX.Text = "GyroX";
            this.lb_GyroX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataGyroX
            //
            this.lb_DataGyroX.Location = new System.Drawing.Point(104, 184);
            this.lb_DataGyroX.Name = "lb_DataGyroX";
            this.lb_DataGyroX.Size = new System.Drawing.Size(88, 24);
            this.lb_DataGyroX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel9
            //
            this.panel9.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel9.Location = new System.Drawing.Point(8, 208);
            this.panel9.Name = "panel9";
            this.panel9.Size = new System.Drawing.Size(184, 2);
            //
            // lb_GyroY
            //
            this.lb_GyroY.Location = new System.Drawing.Point(8, 224);
            this.lb_GyroY.Name = "lb_GyroY";
            this.lb_GyroY.Size = new System.Drawing.Size(88, 24);
            this.lb_GyroY.Text = "GyroY";
            this.lb_GyroY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataGyroY
            //
            this.lb_DataGyroY.Location = new System.Drawing.Point(104, 224);
            this.lb_DataGyroY.Name = "lb_DataGyroY";
            this.lb_DataGyroY.Size = new System.Drawing.Size(88, 24);
            this.lb_DataGyroY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel8
            //
            this.panel8.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel8.Location = new System.Drawing.Point(8, 248);
            this.panel8.Name = "panel8";
            this.panel8.Size = new System.Drawing.Size(184, 2);
            //
            // lb_GyroZ
            //
            this.lb_GyroZ.Location = new System.Drawing.Point(8, 264);
            this.lb_GyroZ.Name = "lb_GyroZ";
            this.lb_GyroZ.Size = new System.Drawing.Size(88, 24);
            this.lb_GyroZ.Text = "GyroZ";
            this.lb_GyroZ.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataGyroZ
            //
            this.lb_DataGyroZ.Location = new System.Drawing.Point(104, 264);
            this.lb_DataGyroZ.Name = "lb_DataGyroZ";
            this.lb_DataGyroZ.Size = new System.Drawing.Size(88, 24);
            this.lb_DataGyroZ.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel7
            //
            this.panel7.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel7.Location = new System.Drawing.Point(8, 288);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(184, 2);
            //
            // lb_EogL
            //
            this.lb_EogL.Location = new System.Drawing.Point(8, 304);
            this.lb_EogL.Name = "lb_EogL";
            this.lb_EogL.Size = new System.Drawing.Size(88, 24);
            this.lb_EogL.Text = "EogL";
            this.lb_EogL.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataEogL
            //
            this.lb_DataEogL.Location = new System.Drawing.Point(104, 304);
            this.lb_DataEogL.Name = "lb_DataEogL";
            this.lb_DataEogL.Size = new System.Drawing.Size(88, 24);
            this.lb_DataEogL.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel10
            //
            this.panel10.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel10.Location = new System.Drawing.Point(8, 328);
            this.panel10.Name = "panel10";
            this.panel10.Size = new System.Drawing.Size(184, 2);
            //
            // lb_EogR
            //
            this.lb_EogR.Location = new System.Drawing.Point(8, 344);
            this.lb_EogR.Name = "lb_EogR";
            this.lb_EogR.Size = new System.Drawing.Size(88, 24);
            this.lb_EogR.Text = "EogR";
            this.lb_EogR.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataEogR
            //
            this.lb_DataEogR.Location = new System.Drawing.Point(104, 344);
            this.lb_DataEogR.Name = "lb_DataEogR";
            this.lb_DataEogR.Size = new System.Drawing.Size(88, 24);
            this.lb_DataEogR.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel11
            //
            this.panel11.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel11.Location = new System.Drawing.Point(8, 368);
            this.panel11.Name = "panel11";
            this.panel11.Size = new System.Drawing.Size(184, 2);
            //
            // lb_EogH
            //
            this.lb_EogH.Location = new System.Drawing.Point(8, 384);
            this.lb_EogH.Name = "lb_EogH";
            this.lb_EogH.Size = new System.Drawing.Size(88, 24);
            this.lb_EogH.Text = "EogH";
            this.lb_EogH.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataEogH
            //
            this.lb_DataEogH.Location = new System.Drawing.Point(104, 384);
            this.lb_DataEogH.Name = "lb_DataEogH";
            this.lb_DataEogH.Size = new System.Drawing.Size(88, 24);
            this.lb_DataEogH.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel13
            //
            this.panel13.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel13.Location = new System.Drawing.Point(8, 408);
            this.panel13.Name = "panel13";
            this.panel13.Size = new System.Drawing.Size(184, 2);
            //
            // lb_EogV
            //
            this.lb_EogV.Location = new System.Drawing.Point(8, 424);
            this.lb_EogV.Name = "lb_EogV";
            this.lb_EogV.Size = new System.Drawing.Size(88, 24);
            this.lb_EogV.Text = "EogV";
            this.lb_EogV.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataEogV
            //
            this.lb_DataEogV.Location = new System.Drawing.Point(104, 424);
            this.lb_DataEogV.Name = "lb_DataEogV";
            this.lb_DataEogV.Size = new System.Drawing.Size(88, 24);
            this.lb_DataEogV.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel12
            //
            this.panel12.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel12.Location = new System.Drawing.Point(8, 448);
            this.panel12.Name = "panel12";
            this.panel12.Size = new System.Drawing.Size(184, 2);
            //
            // lb_BattLv
            //
            this.lb_BattLv.Location = new System.Drawing.Point(8, 464);
            this.lb_BattLv.Name = "lb_BattLv";
            this.lb_BattLv.Size = new System.Drawing.Size(88, 24);
            this.lb_BattLv.Text = "BattLv";
            this.lb_BattLv.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lb_DataBattLv
            //
            this.lb_DataBattLv.Location = new System.Drawing.Point(104, 464);
            this.lb_DataBattLv.Name = "lb_DataBattLv";
            this.lb_DataBattLv.Size = new System.Drawing.Size(88, 24);
            this.lb_DataBattLv.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panel3
            //
            this.panel3.BackColor = System.Drawing.Color.FromArgb(224, 0, 32);
            this.panel3.Location = new System.Drawing.Point(8, 488);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(184, 2);
            //
            // menuStrip1
            //
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.quitToolStripMenuItem,
            this.versionToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(496, 24);
            this.menuStrip1.TabIndex = 18;
            this.menuStrip1.Text = "menuStrip1";
            //
            // quitToolStripMenuItem
            //
            this.quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            this.quitToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.quitToolStripMenuItem.Text = "Quit (&Q)";
            this.quitToolStripMenuItem.Click += new System.EventHandler(this.quitToolStripMenuItem_Click);
            //
            // versionToolStripMenuItem
            //
            this.versionToolStripMenuItem.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.versionToolStripMenuItem.Name = "versionToolStripMenuItem";
            this.versionToolStripMenuItem.Size = new System.Drawing.Size(75, 20);
            this.versionToolStripMenuItem.Text = "Version (&V)";
            this.versionToolStripMenuItem.Click += new System.EventHandler(this.versionToolStripMenuItem_Click);
            //
            // statusStrip1
            //
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deviceVersionTitleLabel,
            this.deviceVersionLabel,
            this.SDKVersionTitleLabel,
            this.SDKVersionLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 547);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(496, 22);
            this.statusStrip1.TabIndex = 19;
            //
            // deviceVersionTitleLabel
            //
            this.deviceVersionTitleLabel.Name = "deviceVersionTitleLabel";
            this.deviceVersionTitleLabel.Size = new System.Drawing.Size(88, 17);
            this.deviceVersionTitleLabel.Text = "MEME version :";
            //
            // deviceVersionLabel
            //
            this.deviceVersionLabel.Name = "deviceVersionLabel";
            this.deviceVersionLabel.Size = new System.Drawing.Size(61, 17);
            this.deviceVersionLabel.Text = "NotFound";
            //
            // SDKVersionTitleLabel
            //
            this.SDKVersionTitleLabel.Name = "SDKVersionTitleLabel";
            this.SDKVersionTitleLabel.Size = new System.Drawing.Size(81, 17);
            this.SDKVersionTitleLabel.Text = "  SDK Version :";
            //
            // SDKVersionLabel
            //
            this.SDKVersionLabel.Name = "SDKVersionLabel";
            this.SDKVersionLabel.Size = new System.Drawing.Size(61, 17);
            this.SDKVersionLabel.Text = "NotFound";
            //
            // MEME_Academic_Sample
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(496, 569);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.freeMarkingButton);
            this.Controls.Add(this.bt_StartMeasurement);
            this.Controls.Add(this.cb_GyroRange);
            this.Controls.Add(this.lb_GyroRange);
            this.Controls.Add(this.cb_AccRange);
            this.Controls.Add(this.lb_AccRange);
            this.Controls.Add(this.cb_TransmissionSpeed);
            this.Controls.Add(this.lb_TransmissionSpeed);
            this.Controls.Add(this.cb_ModeSelect);
            this.Controls.Add(this.lb_ModeSelect);
            this.Controls.Add(this.measurementLabel);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.lb_ConnectionStatus);
            this.Controls.Add(this.bt_ConnectPeripheral);
            this.Controls.Add(this.cb_DeviceList);
            this.Controls.Add(this.bt_ScanPeripheral);
            this.Controls.Add(this.bluetoothNameLabel);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Segoe UI Symbol", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "MEME_Academic_Sample";
            this.Text = "JINS MEME DataLogger";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MEME_Academic_Sample_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label bluetoothNameLabel;
        private System.Windows.Forms.Button bt_ScanPeripheral;
        private System.Windows.Forms.ComboBox cb_DeviceList;
        private System.Windows.Forms.Button bt_ConnectPeripheral;
        private System.Windows.Forms.Label lb_ConnectionStatus;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label measurementLabel;
        private System.Windows.Forms.Label lb_ModeSelect;
        private System.Windows.Forms.ComboBox cb_ModeSelect;
        private System.Windows.Forms.Label lb_TransmissionSpeed;
        private System.Windows.Forms.ComboBox cb_TransmissionSpeed;
        private System.Windows.Forms.Label lb_AccRange;
        private System.Windows.Forms.ComboBox cb_AccRange;
        private System.Windows.Forms.Label lb_GyroRange;
        private System.Windows.Forms.ComboBox cb_GyroRange;
        private System.Windows.Forms.Button bt_StartMeasurement;
        private System.Windows.Forms.Button freeMarkingButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lb_Cnt;
        private System.Windows.Forms.Label lb_DataCnt;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label lb_AccX;
        private System.Windows.Forms.Label lb_DataAccX;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Label lb_AccY;
        private System.Windows.Forms.Label lb_DataAccY;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Label lb_AccZ;
        private System.Windows.Forms.Label lb_DataAccZ;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Label lb_GyroX;
        private System.Windows.Forms.Label lb_DataGyroX;
        private System.Windows.Forms.Panel panel9;
        private System.Windows.Forms.Label lb_GyroY;
        private System.Windows.Forms.Label lb_DataGyroY;
        private System.Windows.Forms.Panel panel8;
        private System.Windows.Forms.Label lb_GyroZ;
        private System.Windows.Forms.Label lb_DataGyroZ;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Label lb_EogL;
        private System.Windows.Forms.Label lb_DataEogL;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.Label lb_EogR;
        private System.Windows.Forms.Label lb_DataEogR;
        private System.Windows.Forms.Panel panel11;
        private System.Windows.Forms.Label lb_EogH;
        private System.Windows.Forms.Label lb_DataEogH;
        private System.Windows.Forms.Panel panel13;
        private System.Windows.Forms.Label lb_EogV;
        private System.Windows.Forms.Label lb_DataEogV;
        private System.Windows.Forms.Panel panel12;
        private System.Windows.Forms.Label lb_BattLv;
        private System.Windows.Forms.Label lb_DataBattLv;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem quitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem versionToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel deviceVersionTitleLabel;
        private System.Windows.Forms.ToolStripStatusLabel deviceVersionLabel;
        private System.Windows.Forms.ToolStripStatusLabel SDKVersionTitleLabel;
        private System.Windows.Forms.ToolStripStatusLabel SDKVersionLabel;
    }
}
