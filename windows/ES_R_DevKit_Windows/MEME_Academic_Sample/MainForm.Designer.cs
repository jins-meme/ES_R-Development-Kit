using MEME_Academic_Sample.Charting;

namespace MEME_Academic_Sample
{
    partial class MainForm
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.settingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.quitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.versionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.leftPanel = new System.Windows.Forms.Panel();
            this.leftFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.lb_AppVersion = new System.Windows.Forms.Label();
            this.lb_MemeVersion = new System.Windows.Forms.Label();
            this.separator1 = new System.Windows.Forms.Panel();
            this.connectRow = new System.Windows.Forms.FlowLayoutPanel();
            this.bt_Scan = new System.Windows.Forms.Button();
            this.bt_FileReplay = new System.Windows.Forms.Button();
            this.cb_DeviceList = new System.Windows.Forms.ComboBox();
            this.bt_Connect = new System.Windows.Forms.Button();
            this.lb_ConnectionState = new System.Windows.Forms.Label();
            this.separator2 = new System.Windows.Forms.Panel();
            this.row_SelectMode = new System.Windows.Forms.Panel();
            this.lb_SelectMode = new System.Windows.Forms.Label();
            this.cb_SelectMode = new System.Windows.Forms.ComboBox();
            this.row_TransSpeed = new System.Windows.Forms.Panel();
            this.lb_TransSpeed = new System.Windows.Forms.Label();
            this.cb_TransSpeed = new System.Windows.Forms.ComboBox();
            this.row_AccelRange = new System.Windows.Forms.Panel();
            this.lb_AccelRange = new System.Windows.Forms.Label();
            this.cb_AccelRange = new System.Windows.Forms.ComboBox();
            this.row_GyroRange = new System.Windows.Forms.Panel();
            this.lb_GyroRange = new System.Windows.Forms.Label();
            this.cb_GyroRange = new System.Windows.Forms.ComboBox();
            this.measureRow = new System.Windows.Forms.FlowLayoutPanel();
            this.bt_Measurement = new System.Windows.Forms.Button();
            this.bt_XRangeIn = new System.Windows.Forms.Button();
            this.bt_XRangeOut = new System.Windows.Forms.Button();
            this.lb_XRange = new System.Windows.Forms.Label();
            this.bt_ReplayRecord = new System.Windows.Forms.Button();
            this.bt_ReplayPause = new System.Windows.Forms.Button();
            this.bt_FreeMarking = new System.Windows.Forms.Button();
            this.replayPanel = new System.Windows.Forms.Panel();
            this.tb_ReplayProgress = new System.Windows.Forms.TrackBar();
            this.replayButtonRow = new System.Windows.Forms.FlowLayoutPanel();
            this.bt_ReplayBack = new System.Windows.Forms.Button();
            this.bt_ReplayForward = new System.Windows.Forms.Button();
            this.bt_ReplaySpeed = new System.Windows.Forms.Button();
            this.separator3 = new System.Windows.Forms.Panel();
            this.row_SuccessRate = new System.Windows.Forms.Panel();
            this.lb_SuccessRateTitle = new System.Windows.Forms.Label();
            this.lb_SuccessRate = new System.Windows.Forms.Label();
            this.pb_SuccessRate = new System.Windows.Forms.ProgressBar();
            this.row_Communication = new System.Windows.Forms.Panel();
            this.lb_CommunicationTitle = new System.Windows.Forms.Label();
            this.lb_Communication = new System.Windows.Forms.Label();
            this.pb_Communication = new System.Windows.Forms.ProgressBar();
            this.lb_LocalAddress = new System.Windows.Forms.Label();
            this.lb_LocalPort = new System.Windows.Forms.Label();
            this.lb_SocketStatus = new System.Windows.Forms.Label();
            this.chartsTable = new System.Windows.Forms.TableLayoutPanel();
            this.chartPanel1 = new MEME_Academic_Sample.Charting.ChartPanel();
            this.chartPanel2 = new MEME_Academic_Sample.Charting.ChartPanel();
            this.chartPanel3 = new MEME_Academic_Sample.Charting.ChartPanel();
            this.menuStrip1.SuspendLayout();
            this.leftPanel.SuspendLayout();
            this.leftFlow.SuspendLayout();
            this.connectRow.SuspendLayout();
            this.row_SelectMode.SuspendLayout();
            this.row_TransSpeed.SuspendLayout();
            this.row_AccelRange.SuspendLayout();
            this.row_GyroRange.SuspendLayout();
            this.measureRow.SuspendLayout();
            this.replayPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tb_ReplayProgress)).BeginInit();
            this.replayButtonRow.SuspendLayout();
            this.row_SuccessRate.SuspendLayout();
            this.row_Communication.SuspendLayout();
            this.chartsTable.SuspendLayout();
            this.SuspendLayout();
            //
            // menuStrip1
            //
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingToolStripMenuItem,
            this.quitToolStripMenuItem,
            this.versionToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1280, 24);
            this.menuStrip1.TabIndex = 0;
            //
            // settingToolStripMenuItem
            //
            this.settingToolStripMenuItem.Name = "settingToolStripMenuItem";
            this.settingToolStripMenuItem.Size = new System.Drawing.Size(70, 20);
            this.settingToolStripMenuItem.Text = "Setting (&S)";
            this.settingToolStripMenuItem.Click += new System.EventHandler(this.settingToolStripMenuItem_Click);
            //
            // quitToolStripMenuItem
            //
            this.quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            this.quitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.quitToolStripMenuItem.ShowShortcutKeys = false;
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
            // leftPanel
            //
            this.leftPanel.Controls.Add(this.leftFlow);
            this.leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.leftPanel.Name = "leftPanel";
            this.leftPanel.Padding = new System.Windows.Forms.Padding(12);
            this.leftPanel.Size = new System.Drawing.Size(320, 796);
            this.leftPanel.TabIndex = 1;
            //
            // leftFlow
            //
            this.leftFlow.AutoScroll = true;
            this.leftFlow.Controls.Add(this.lb_AppVersion);
            this.leftFlow.Controls.Add(this.lb_MemeVersion);
            this.leftFlow.Controls.Add(this.separator1);
            this.leftFlow.Controls.Add(this.connectRow);
            this.leftFlow.Controls.Add(this.cb_DeviceList);
            this.leftFlow.Controls.Add(this.bt_Connect);
            this.leftFlow.Controls.Add(this.lb_ConnectionState);
            this.leftFlow.Controls.Add(this.separator2);
            this.leftFlow.Controls.Add(this.row_SelectMode);
            this.leftFlow.Controls.Add(this.row_TransSpeed);
            this.leftFlow.Controls.Add(this.row_AccelRange);
            this.leftFlow.Controls.Add(this.row_GyroRange);
            this.leftFlow.Controls.Add(this.measureRow);
            this.leftFlow.Controls.Add(this.bt_FreeMarking);
            this.leftFlow.Controls.Add(this.replayPanel);
            this.leftFlow.Controls.Add(this.separator3);
            this.leftFlow.Controls.Add(this.row_SuccessRate);
            this.leftFlow.Controls.Add(this.pb_SuccessRate);
            this.leftFlow.Controls.Add(this.row_Communication);
            this.leftFlow.Controls.Add(this.pb_Communication);
            this.leftFlow.Controls.Add(this.lb_LocalAddress);
            this.leftFlow.Controls.Add(this.lb_LocalPort);
            this.leftFlow.Controls.Add(this.lb_SocketStatus);
            this.leftFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftFlow.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.leftFlow.Name = "leftFlow";
            this.leftFlow.Size = new System.Drawing.Size(276, 772);
            this.leftFlow.TabIndex = 0;
            this.leftFlow.WrapContents = false;
            //
            // lb_AppVersion
            //
            this.lb_AppVersion.AutoSize = true;
            this.lb_AppVersion.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_AppVersion.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this.lb_AppVersion.Name = "lb_AppVersion";
            this.lb_AppVersion.Size = new System.Drawing.Size(60, 15);
            this.lb_AppVersion.TabIndex = 1;
            this.lb_AppVersion.Text = "Version";
            //
            // lb_MemeVersion
            //
            this.lb_MemeVersion.AutoSize = true;
            this.lb_MemeVersion.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_MemeVersion.Margin = new System.Windows.Forms.Padding(3, 2, 3, 0);
            this.lb_MemeVersion.Name = "lb_MemeVersion";
            this.lb_MemeVersion.Size = new System.Drawing.Size(90, 15);
            this.lb_MemeVersion.TabIndex = 2;
            this.lb_MemeVersion.Text = "MEME Version：";
            //
            // separator1
            //
            this.separator1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.separator1.Margin = new System.Windows.Forms.Padding(3, 10, 3, 10);
            this.separator1.Name = "separator1";
            this.separator1.Size = new System.Drawing.Size(258, 1);
            this.separator1.TabIndex = 3;
            //
            // connectRow
            //
            this.connectRow.AutoSize = true;
            this.connectRow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.connectRow.Controls.Add(this.bt_Scan);
            this.connectRow.Controls.Add(this.bt_FileReplay);
            this.connectRow.Margin = new System.Windows.Forms.Padding(0);
            this.connectRow.Name = "connectRow";
            this.connectRow.Size = new System.Drawing.Size(258, 34);
            this.connectRow.TabIndex = 4;
            this.connectRow.WrapContents = false;
            //
            // bt_Scan
            //
            this.bt_Scan.Name = "bt_Scan";
            this.bt_Scan.Size = new System.Drawing.Size(100, 28);
            this.bt_Scan.TabIndex = 0;
            this.bt_Scan.Text = "Scan";
            this.bt_Scan.UseVisualStyleBackColor = true;
            this.bt_Scan.Click += new System.EventHandler(this.bt_Scan_Click);
            //
            // bt_FileReplay
            //
            this.bt_FileReplay.Name = "bt_FileReplay";
            this.bt_FileReplay.Size = new System.Drawing.Size(100, 28);
            this.bt_FileReplay.TabIndex = 1;
            this.bt_FileReplay.Text = "File Replay";
            this.bt_FileReplay.UseVisualStyleBackColor = true;
            this.bt_FileReplay.Click += new System.EventHandler(this.bt_FileReplay_Click);
            //
            // cb_DeviceList
            //
            this.cb_DeviceList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_DeviceList.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.cb_DeviceList.Name = "cb_DeviceList";
            this.cb_DeviceList.Size = new System.Drawing.Size(252, 23);
            this.cb_DeviceList.TabIndex = 5;
            //
            // bt_Connect
            //
            this.bt_Connect.Name = "bt_Connect";
            this.bt_Connect.Size = new System.Drawing.Size(120, 28);
            this.bt_Connect.TabIndex = 6;
            this.bt_Connect.Text = "Connect";
            this.bt_Connect.UseVisualStyleBackColor = true;
            this.bt_Connect.Click += new System.EventHandler(this.bt_Connect_Click);
            //
            // lb_ConnectionState
            //
            this.lb_ConnectionState.AutoSize = true;
            this.lb_ConnectionState.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_ConnectionState.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.lb_ConnectionState.Name = "lb_ConnectionState";
            this.lb_ConnectionState.Size = new System.Drawing.Size(130, 15);
            this.lb_ConnectionState.TabIndex = 7;
            this.lb_ConnectionState.Text = "State : Disconnected";
            //
            // separator2
            //
            this.separator2.BackColor = System.Drawing.SystemColors.ControlDark;
            this.separator2.Margin = new System.Windows.Forms.Padding(3, 10, 3, 10);
            this.separator2.Name = "separator2";
            this.separator2.Size = new System.Drawing.Size(258, 1);
            this.separator2.TabIndex = 8;
            //
            // row_SelectMode
            //
            this.row_SelectMode.Controls.Add(this.cb_SelectMode);
            this.row_SelectMode.Controls.Add(this.lb_SelectMode);
            this.row_SelectMode.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this.row_SelectMode.Name = "row_SelectMode";
            this.row_SelectMode.Size = new System.Drawing.Size(258, 26);
            this.row_SelectMode.TabIndex = 9;
            //
            // lb_SelectMode
            //
            this.lb_SelectMode.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_SelectMode.Location = new System.Drawing.Point(0, 4);
            this.lb_SelectMode.Name = "lb_SelectMode";
            this.lb_SelectMode.Size = new System.Drawing.Size(90, 20);
            this.lb_SelectMode.Text = "Select Mode";
            //
            // cb_SelectMode
            //
            this.cb_SelectMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_SelectMode.Location = new System.Drawing.Point(96, 0);
            this.cb_SelectMode.Name = "cb_SelectMode";
            this.cb_SelectMode.Size = new System.Drawing.Size(150, 23);
            //
            // row_TransSpeed
            //
            this.row_TransSpeed.Controls.Add(this.cb_TransSpeed);
            this.row_TransSpeed.Controls.Add(this.lb_TransSpeed);
            this.row_TransSpeed.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this.row_TransSpeed.Name = "row_TransSpeed";
            this.row_TransSpeed.Size = new System.Drawing.Size(258, 26);
            this.row_TransSpeed.TabIndex = 10;
            //
            // lb_TransSpeed
            //
            this.lb_TransSpeed.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_TransSpeed.Location = new System.Drawing.Point(0, 4);
            this.lb_TransSpeed.Name = "lb_TransSpeed";
            this.lb_TransSpeed.Size = new System.Drawing.Size(90, 20);
            this.lb_TransSpeed.Text = "Trans Speed";
            //
            // cb_TransSpeed
            //
            this.cb_TransSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_TransSpeed.Location = new System.Drawing.Point(96, 0);
            this.cb_TransSpeed.Name = "cb_TransSpeed";
            this.cb_TransSpeed.Size = new System.Drawing.Size(150, 23);
            //
            // row_AccelRange
            //
            this.row_AccelRange.Controls.Add(this.cb_AccelRange);
            this.row_AccelRange.Controls.Add(this.lb_AccelRange);
            this.row_AccelRange.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this.row_AccelRange.Name = "row_AccelRange";
            this.row_AccelRange.Size = new System.Drawing.Size(258, 26);
            this.row_AccelRange.TabIndex = 11;
            //
            // lb_AccelRange
            //
            this.lb_AccelRange.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_AccelRange.Location = new System.Drawing.Point(0, 4);
            this.lb_AccelRange.Name = "lb_AccelRange";
            this.lb_AccelRange.Size = new System.Drawing.Size(90, 20);
            this.lb_AccelRange.Text = "Accel Range";
            //
            // cb_AccelRange
            //
            this.cb_AccelRange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_AccelRange.Location = new System.Drawing.Point(96, 0);
            this.cb_AccelRange.Name = "cb_AccelRange";
            this.cb_AccelRange.Size = new System.Drawing.Size(150, 23);
            //
            // row_GyroRange
            //
            this.row_GyroRange.Controls.Add(this.cb_GyroRange);
            this.row_GyroRange.Controls.Add(this.lb_GyroRange);
            this.row_GyroRange.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.row_GyroRange.Name = "row_GyroRange";
            this.row_GyroRange.Size = new System.Drawing.Size(258, 26);
            this.row_GyroRange.TabIndex = 12;
            //
            // lb_GyroRange
            //
            this.lb_GyroRange.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_GyroRange.Location = new System.Drawing.Point(0, 4);
            this.lb_GyroRange.Name = "lb_GyroRange";
            this.lb_GyroRange.Size = new System.Drawing.Size(90, 20);
            this.lb_GyroRange.Text = "Gyro Range";
            //
            // cb_GyroRange
            //
            this.cb_GyroRange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_GyroRange.Location = new System.Drawing.Point(96, 0);
            this.cb_GyroRange.Name = "cb_GyroRange";
            this.cb_GyroRange.Size = new System.Drawing.Size(150, 23);
            //
            // measureRow
            //
            this.measureRow.AutoSize = true;
            this.measureRow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.measureRow.Controls.Add(this.bt_Measurement);
            this.measureRow.Controls.Add(this.bt_ReplayRecord);
            this.measureRow.Controls.Add(this.bt_ReplayPause);
            this.measureRow.Controls.Add(this.bt_XRangeIn);
            this.measureRow.Controls.Add(this.bt_XRangeOut);
            this.measureRow.Controls.Add(this.lb_XRange);
            this.measureRow.Margin = new System.Windows.Forms.Padding(0);
            this.measureRow.Name = "measureRow";
            this.measureRow.Size = new System.Drawing.Size(258, 34);
            this.measureRow.TabIndex = 13;
            this.measureRow.WrapContents = false;
            //
            // bt_Measurement
            //
            this.bt_Measurement.Name = "bt_Measurement";
            this.bt_Measurement.Size = new System.Drawing.Size(140, 28);
            this.bt_Measurement.TabIndex = 0;
            this.bt_Measurement.Text = "Start Measurement";
            this.bt_Measurement.UseVisualStyleBackColor = true;
            this.bt_Measurement.Click += new System.EventHandler(this.bt_Measurement_Click);
            //
            // bt_XRangeIn
            //
            this.bt_XRangeIn.Name = "bt_XRangeIn";
            this.bt_XRangeIn.Size = new System.Drawing.Size(32, 28);
            this.bt_XRangeIn.TabIndex = 1;
            this.bt_XRangeIn.Text = "＋";
            this.bt_XRangeIn.UseVisualStyleBackColor = true;
            this.bt_XRangeIn.Click += new System.EventHandler(this.bt_XRangeIn_Click);
            //
            // bt_XRangeOut
            //
            this.bt_XRangeOut.Name = "bt_XRangeOut";
            this.bt_XRangeOut.Size = new System.Drawing.Size(32, 28);
            this.bt_XRangeOut.TabIndex = 2;
            this.bt_XRangeOut.Text = "－";
            this.bt_XRangeOut.UseVisualStyleBackColor = true;
            this.bt_XRangeOut.Click += new System.EventHandler(this.bt_XRangeOut_Click);
            //
            // lb_XRange
            //
            this.lb_XRange.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_XRange.Margin = new System.Windows.Forms.Padding(3, 9, 3, 0);
            this.lb_XRange.Name = "lb_XRange";
            this.lb_XRange.Size = new System.Drawing.Size(34, 18);
            this.lb_XRange.TabIndex = 3;
            this.lb_XRange.Text = "7s";
            //
            // bt_ReplayRecord
            //
            this.bt_ReplayRecord.Name = "bt_ReplayRecord";
            this.bt_ReplayRecord.Size = new System.Drawing.Size(76, 28);
            this.bt_ReplayRecord.TabIndex = 4;
            this.bt_ReplayRecord.Text = "Record";
            this.bt_ReplayRecord.UseVisualStyleBackColor = true;
            this.bt_ReplayRecord.Visible = false;
            this.bt_ReplayRecord.Click += new System.EventHandler(this.bt_ReplayRecord_Click);
            //
            // bt_ReplayPause
            //
            this.bt_ReplayPause.Name = "bt_ReplayPause";
            this.bt_ReplayPause.Size = new System.Drawing.Size(76, 28);
            this.bt_ReplayPause.TabIndex = 5;
            this.bt_ReplayPause.Text = "Pause";
            this.bt_ReplayPause.UseVisualStyleBackColor = true;
            this.bt_ReplayPause.Visible = false;
            this.bt_ReplayPause.Click += new System.EventHandler(this.bt_ReplayPause_Click);
            //
            // bt_FreeMarking
            //
            this.bt_FreeMarking.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            this.bt_FreeMarking.Name = "bt_FreeMarking";
            this.bt_FreeMarking.Size = new System.Drawing.Size(120, 28);
            this.bt_FreeMarking.TabIndex = 14;
            this.bt_FreeMarking.Text = "Free Marking";
            this.bt_FreeMarking.UseVisualStyleBackColor = true;
            this.bt_FreeMarking.Click += new System.EventHandler(this.bt_FreeMarking_Click);
            //
            // replayPanel
            //
            this.replayPanel.Controls.Add(this.replayButtonRow);
            this.replayPanel.Controls.Add(this.tb_ReplayProgress);
            this.replayPanel.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.replayPanel.Name = "replayPanel";
            this.replayPanel.Size = new System.Drawing.Size(258, 72);
            this.replayPanel.TabIndex = 15;
            this.replayPanel.Visible = false;
            //
            // tb_ReplayProgress
            //
            this.tb_ReplayProgress.AutoSize = false;
            this.tb_ReplayProgress.Location = new System.Drawing.Point(0, 0);
            this.tb_ReplayProgress.Maximum = 1000;
            this.tb_ReplayProgress.Name = "tb_ReplayProgress";
            this.tb_ReplayProgress.Size = new System.Drawing.Size(252, 30);
            this.tb_ReplayProgress.TabIndex = 0;
            this.tb_ReplayProgress.TickStyle = System.Windows.Forms.TickStyle.None;
            this.tb_ReplayProgress.Scroll += new System.EventHandler(this.tb_ReplayProgress_Scroll);
            this.tb_ReplayProgress.MouseUp += new System.Windows.Forms.MouseEventHandler(this.tb_ReplayProgress_Released);
            this.tb_ReplayProgress.KeyUp += new System.Windows.Forms.KeyEventHandler(this.tb_ReplayProgress_KeyUp);
            //
            // replayButtonRow
            //
            this.replayButtonRow.Controls.Add(this.bt_ReplayBack);
            this.replayButtonRow.Controls.Add(this.bt_ReplayForward);
            this.replayButtonRow.Controls.Add(this.bt_ReplaySpeed);
            this.replayButtonRow.Location = new System.Drawing.Point(0, 34);
            this.replayButtonRow.Name = "replayButtonRow";
            this.replayButtonRow.Size = new System.Drawing.Size(258, 34);
            this.replayButtonRow.TabIndex = 1;
            this.replayButtonRow.WrapContents = false;
            //
            // bt_ReplayBack
            //
            this.bt_ReplayBack.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.bt_ReplayBack.Name = "bt_ReplayBack";
            this.bt_ReplayBack.Size = new System.Drawing.Size(56, 28);
            this.bt_ReplayBack.TabIndex = 0;
            this.bt_ReplayBack.Text = "<<";
            this.bt_ReplayBack.UseVisualStyleBackColor = true;
            this.bt_ReplayBack.Click += new System.EventHandler(this.bt_ReplayBack_Click);
            //
            // bt_ReplayForward
            //
            this.bt_ReplayForward.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.bt_ReplayForward.Name = "bt_ReplayForward";
            this.bt_ReplayForward.Size = new System.Drawing.Size(56, 28);
            this.bt_ReplayForward.TabIndex = 1;
            this.bt_ReplayForward.Text = ">>";
            this.bt_ReplayForward.UseVisualStyleBackColor = true;
            this.bt_ReplayForward.Click += new System.EventHandler(this.bt_ReplayForward_Click);
            //
            // bt_ReplaySpeed
            //
            this.bt_ReplaySpeed.Margin = new System.Windows.Forms.Padding(0);
            this.bt_ReplaySpeed.Name = "bt_ReplaySpeed";
            this.bt_ReplaySpeed.Size = new System.Drawing.Size(60, 28);
            this.bt_ReplaySpeed.TabIndex = 2;
            this.bt_ReplaySpeed.Text = "x1";
            this.bt_ReplaySpeed.UseVisualStyleBackColor = true;
            this.bt_ReplaySpeed.Click += new System.EventHandler(this.bt_ReplaySpeed_Click);
            //
            // separator3
            //
            this.separator3.BackColor = System.Drawing.SystemColors.ControlDark;
            this.separator3.Margin = new System.Windows.Forms.Padding(3, 10, 3, 10);
            this.separator3.Name = "separator3";
            this.separator3.Size = new System.Drawing.Size(258, 1);
            this.separator3.TabIndex = 15;
            //
            // row_SuccessRate
            //
            this.row_SuccessRate.Controls.Add(this.lb_SuccessRate);
            this.row_SuccessRate.Controls.Add(this.lb_SuccessRateTitle);
            this.row_SuccessRate.Margin = new System.Windows.Forms.Padding(3, 0, 3, 2);
            this.row_SuccessRate.Name = "row_SuccessRate";
            this.row_SuccessRate.Size = new System.Drawing.Size(258, 20);
            this.row_SuccessRate.TabIndex = 16;
            //
            // lb_SuccessRateTitle
            //
            this.lb_SuccessRateTitle.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_SuccessRateTitle.Location = new System.Drawing.Point(0, 2);
            this.lb_SuccessRateTitle.Name = "lb_SuccessRateTitle";
            this.lb_SuccessRateTitle.Size = new System.Drawing.Size(100, 18);
            this.lb_SuccessRateTitle.Text = "Success rate:";
            //
            // lb_SuccessRate
            //
            this.lb_SuccessRate.Location = new System.Drawing.Point(104, 2);
            this.lb_SuccessRate.Name = "lb_SuccessRate";
            this.lb_SuccessRate.Size = new System.Drawing.Size(80, 18);
            this.lb_SuccessRate.Text = "0.0%";
            //
            // pb_SuccessRate
            //
            this.pb_SuccessRate.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.pb_SuccessRate.Name = "pb_SuccessRate";
            this.pb_SuccessRate.Size = new System.Drawing.Size(252, 10);
            this.pb_SuccessRate.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.pb_SuccessRate.TabIndex = 17;
            //
            // row_Communication
            //
            this.row_Communication.Controls.Add(this.lb_Communication);
            this.row_Communication.Controls.Add(this.lb_CommunicationTitle);
            this.row_Communication.Margin = new System.Windows.Forms.Padding(3, 0, 3, 2);
            this.row_Communication.Name = "row_Communication";
            this.row_Communication.Size = new System.Drawing.Size(258, 20);
            this.row_Communication.TabIndex = 18;
            //
            // lb_CommunicationTitle
            //
            this.lb_CommunicationTitle.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_CommunicationTitle.Location = new System.Drawing.Point(0, 2);
            this.lb_CommunicationTitle.Name = "lb_CommunicationTitle";
            this.lb_CommunicationTitle.Size = new System.Drawing.Size(100, 18);
            this.lb_CommunicationTitle.Text = "Communication:";
            //
            // lb_Communication
            //
            this.lb_Communication.Location = new System.Drawing.Point(104, 2);
            this.lb_Communication.Name = "lb_Communication";
            this.lb_Communication.Size = new System.Drawing.Size(80, 18);
            this.lb_Communication.Text = "0.0%";
            //
            // pb_Communication
            //
            this.pb_Communication.Margin = new System.Windows.Forms.Padding(3, 0, 3, 12);
            this.pb_Communication.Name = "pb_Communication";
            this.pb_Communication.Size = new System.Drawing.Size(252, 10);
            this.pb_Communication.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.pb_Communication.TabIndex = 19;
            //
            // lb_LocalAddress
            //
            this.lb_LocalAddress.AutoSize = true;
            this.lb_LocalAddress.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_LocalAddress.Margin = new System.Windows.Forms.Padding(3, 0, 3, 2);
            this.lb_LocalAddress.Name = "lb_LocalAddress";
            this.lb_LocalAddress.Size = new System.Drawing.Size(80, 15);
            this.lb_LocalAddress.TabIndex = 20;
            this.lb_LocalAddress.Text = "IP address:";
            //
            // lb_LocalPort
            //
            this.lb_LocalPort.AutoSize = true;
            this.lb_LocalPort.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_LocalPort.Margin = new System.Windows.Forms.Padding(3, 0, 3, 2);
            this.lb_LocalPort.Name = "lb_LocalPort";
            this.lb_LocalPort.Size = new System.Drawing.Size(40, 15);
            this.lb_LocalPort.TabIndex = 21;
            this.lb_LocalPort.Text = "Port:";
            //
            // lb_SocketStatus
            //
            this.lb_SocketStatus.AutoSize = true;
            this.lb_SocketStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_SocketStatus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 2);
            this.lb_SocketStatus.Name = "lb_SocketStatus";
            this.lb_SocketStatus.Size = new System.Drawing.Size(50, 15);
            this.lb_SocketStatus.TabIndex = 22;
            this.lb_SocketStatus.Text = "Status : ";
            //
            // chartsTable
            //
            this.chartsTable.ColumnCount = 1;
            this.chartsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.chartsTable.Controls.Add(this.chartPanel1, 0, 0);
            this.chartsTable.Controls.Add(this.chartPanel2, 0, 1);
            this.chartsTable.Controls.Add(this.chartPanel3, 0, 2);
            this.chartsTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartsTable.Name = "chartsTable";
            this.chartsTable.Padding = new System.Windows.Forms.Padding(0, 12, 12, 12);
            this.chartsTable.RowCount = 3;
            this.chartsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.chartsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.chartsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.chartsTable.Size = new System.Drawing.Size(980, 796);
            this.chartsTable.TabIndex = 2;
            //
            // chartPanel1
            //
            this.chartPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartPanel1.Index = 1;
            this.chartPanel1.Name = "chartPanel1";
            this.chartPanel1.TabIndex = 0;
            //
            // chartPanel2
            //
            this.chartPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartPanel2.Index = 2;
            this.chartPanel2.Name = "chartPanel2";
            this.chartPanel2.TabIndex = 1;
            //
            // chartPanel3
            //
            this.chartPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartPanel3.Index = 3;
            this.chartPanel3.Name = "chartPanel3";
            this.chartPanel3.TabIndex = 2;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 738);
            this.Controls.Add(this.chartsTable);
            this.Controls.Add(this.leftPanel);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(1000, 576);
            this.Name = "MainForm";
            this.Text = "JINS MEME DataLogger";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.leftPanel.ResumeLayout(false);
            this.leftFlow.ResumeLayout(false);
            this.leftFlow.PerformLayout();
            this.connectRow.ResumeLayout(false);
            this.row_SelectMode.ResumeLayout(false);
            this.row_TransSpeed.ResumeLayout(false);
            this.row_AccelRange.ResumeLayout(false);
            this.row_GyroRange.ResumeLayout(false);
            this.measureRow.ResumeLayout(false);
            this.replayPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.tb_ReplayProgress)).EndInit();
            this.replayButtonRow.ResumeLayout(false);
            this.row_SuccessRate.ResumeLayout(false);
            this.row_Communication.ResumeLayout(false);
            this.chartsTable.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem settingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem quitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem versionToolStripMenuItem;
        private System.Windows.Forms.Panel leftPanel;
        private System.Windows.Forms.FlowLayoutPanel leftFlow;
        private System.Windows.Forms.Label lb_AppVersion;
        private System.Windows.Forms.Label lb_MemeVersion;
        private System.Windows.Forms.Panel separator1;
        private System.Windows.Forms.FlowLayoutPanel connectRow;
        private System.Windows.Forms.Button bt_Scan;
        private System.Windows.Forms.Button bt_FileReplay;
        private System.Windows.Forms.ComboBox cb_DeviceList;
        private System.Windows.Forms.Button bt_Connect;
        private System.Windows.Forms.Label lb_ConnectionState;
        private System.Windows.Forms.Panel separator2;
        private System.Windows.Forms.Panel row_SelectMode;
        private System.Windows.Forms.Label lb_SelectMode;
        private System.Windows.Forms.ComboBox cb_SelectMode;
        private System.Windows.Forms.Panel row_TransSpeed;
        private System.Windows.Forms.Label lb_TransSpeed;
        private System.Windows.Forms.ComboBox cb_TransSpeed;
        private System.Windows.Forms.Panel row_AccelRange;
        private System.Windows.Forms.Label lb_AccelRange;
        private System.Windows.Forms.ComboBox cb_AccelRange;
        private System.Windows.Forms.Panel row_GyroRange;
        private System.Windows.Forms.Label lb_GyroRange;
        private System.Windows.Forms.ComboBox cb_GyroRange;
        private System.Windows.Forms.FlowLayoutPanel measureRow;
        private System.Windows.Forms.Button bt_Measurement;
        private System.Windows.Forms.Button bt_XRangeIn;
        private System.Windows.Forms.Button bt_XRangeOut;
        private System.Windows.Forms.Label lb_XRange;
        private System.Windows.Forms.Button bt_ReplayRecord;
        private System.Windows.Forms.Button bt_ReplayPause;
        private System.Windows.Forms.Button bt_FreeMarking;
        private System.Windows.Forms.Panel replayPanel;
        private System.Windows.Forms.TrackBar tb_ReplayProgress;
        private System.Windows.Forms.FlowLayoutPanel replayButtonRow;
        private System.Windows.Forms.Button bt_ReplayBack;
        private System.Windows.Forms.Button bt_ReplayForward;
        private System.Windows.Forms.Button bt_ReplaySpeed;
        private System.Windows.Forms.Panel separator3;
        private System.Windows.Forms.Panel row_SuccessRate;
        private System.Windows.Forms.Label lb_SuccessRateTitle;
        private System.Windows.Forms.Label lb_SuccessRate;
        private System.Windows.Forms.ProgressBar pb_SuccessRate;
        private System.Windows.Forms.Panel row_Communication;
        private System.Windows.Forms.Label lb_CommunicationTitle;
        private System.Windows.Forms.Label lb_Communication;
        private System.Windows.Forms.ProgressBar pb_Communication;
        private System.Windows.Forms.Label lb_LocalAddress;
        private System.Windows.Forms.Label lb_LocalPort;
        private System.Windows.Forms.Label lb_SocketStatus;
        private System.Windows.Forms.TableLayoutPanel chartsTable;
        private ChartPanel chartPanel1;
        private ChartPanel chartPanel2;
        private ChartPanel chartPanel3;
    }
}
