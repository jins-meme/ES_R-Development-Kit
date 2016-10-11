namespace JINS_MEME_DataLogger
{
    partial class mainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(mainForm));
            JINS_MEME_DataLogger.AxisBean axisBean1 = new JINS_MEME_DataLogger.AxisBean();
            JINS_MEME_DataLogger.AxisBean axisBean2 = new JINS_MEME_DataLogger.AxisBean();
            JINS_MEME_DataLogger.AxisBean axisBean3 = new JINS_MEME_DataLogger.AxisBean();
            this.comPortNameCombo = new System.Windows.Forms.ComboBox();
            this.comConnectButton = new System.Windows.Forms.Button();
            this.comPortNameLabel = new System.Windows.Forms.Label();
            this.measureButton = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.controlPanelTab = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.socketPortPanel = new System.Windows.Forms.Panel();
            this.socketPortLabel = new System.Windows.Forms.Label();
            this.socketStatusPanel = new System.Windows.Forms.Panel();
            this.socketIPAddressPanel = new System.Windows.Forms.Panel();
            this.socketStatusLabel = new System.Windows.Forms.Label();
            this.socketIPAddressLabel = new System.Windows.Forms.Label();
            this.socketTitleLabel = new System.Windows.Forms.Label();
            this.transmissionSpeedCombo = new System.Windows.Forms.ComboBox();
            this.transmissionSpeedLabel = new System.Windows.Forms.Label();
            this.scanComPortProgress = new System.Windows.Forms.ProgressBar();
            this.initializeButton = new System.Windows.Forms.Button();
            this.freeMarkingButton = new System.Windows.Forms.Button();
            this.gyroscopeRangeCombo = new System.Windows.Forms.ComboBox();
            this.gyroscopeRangeLabel = new System.Windows.Forms.Label();
            this.accRangeCombo = new System.Windows.Forms.ComboBox();
            this.accRangeLabel = new System.Windows.Forms.Label();
            this.modeSelectCombo = new System.Windows.Forms.ComboBox();
            this.modeSelectLabel = new System.Windows.Forms.Label();
            this.measurementLabel = new System.Windows.Forms.Label();
            this.connectionStatusLabel = new System.Windows.Forms.Label();
            this.connectionStatusPanel = new System.Windows.Forms.Panel();
            this.bluetoothConnectButton = new System.Windows.Forms.Button();
            this.bluetoothMacCombo = new System.Windows.Forms.ComboBox();
            this.bluetoothScanButton = new System.Windows.Forms.Button();
            this.bluetoothNameLabel = new System.Windows.Forms.Label();
            this.scanPortButton = new System.Windows.Forms.Button();
            this.chartGroupBox = new System.Windows.Forms.GroupBox();
            this.chartApplyButton = new System.Windows.Forms.Button();
            this.chart3Label = new System.Windows.Forms.Label();
            this.chart2Label = new System.Windows.Forms.Label();
            this.chart1Label = new System.Windows.Forms.Label();
            this.chart3Select = new JINS_MEME_DataLogger.ComboBoxEx();
            this.chart2Select = new JINS_MEME_DataLogger.ComboBoxEx();
            this.chart1Select = new JINS_MEME_DataLogger.ComboBoxEx();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.historyTransmissionSpeedLabel = new System.Windows.Forms.Label();
            this.fileReadProgress = new System.Windows.Forms.ProgressBar();
            this.historyFileGyroRangeLabel = new System.Windows.Forms.Label();
            this.historyFileAccRangeLabel = new System.Windows.Forms.Label();
            this.historyFileModeLabel = new System.Windows.Forms.Label();
            this.replayPlotButton = new System.Windows.Forms.Button();
            this.replayStopButton = new System.Windows.Forms.Button();
            this.replayPauseButton = new System.Windows.Forms.Button();
            this.replayStartButton = new System.Windows.Forms.Button();
            this.replaySpeedCombo = new System.Windows.Forms.ComboBox();
            this.replaySpeedLabel = new System.Windows.Forms.Label();
            this.toDatetimeText = new System.Windows.Forms.TextBox();
            this.fromDatetimeText = new System.Windows.Forms.TextBox();
            this.toDatatimeLabel = new System.Windows.Forms.Label();
            this.fromDatetimeLabel = new System.Windows.Forms.Label();
            this.rangeMessageLabel = new System.Windows.Forms.Label();
            this.selectedFileLabel = new System.Windows.Forms.Label();
            this.selectedFilePanel = new System.Windows.Forms.Panel();
            this.setDataFolderButton = new System.Windows.Forms.Button();
            this.previousFileList = new System.Windows.Forms.ListBox();
            this.previousFileLabel = new System.Windows.Forms.Label();
            this.graphControl = new JINS_MEME_DataLogger.GraphControl();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.batteryLevelLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.batteryLevelProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.deviceVersionTitleLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.Label1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.deviceVersionLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.dongleVersionTitleLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.dongleVersionLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.springLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.successRateLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.successRateProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.transientDataLossLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.transientDataLossProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.versionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.batteryLevelTimer = new System.Windows.Forms.Timer(this.components);
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            ((System.ComponentModel.ISupportInitialize)(this.settings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.controlPanelTab.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.chartGroupBox.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // comPortNameCombo
            // 
            this.comPortNameCombo.BackColor = System.Drawing.Color.LightGray;
            this.comPortNameCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.comPortNameCombo, "comPortNameCombo");
            this.comPortNameCombo.FormattingEnabled = true;
            this.comPortNameCombo.Name = "comPortNameCombo";
            // 
            // comConnectButton
            // 
            this.comConnectButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.comConnectButton, "comConnectButton");
            this.comConnectButton.ForeColor = System.Drawing.SystemColors.ControlText;
            this.comConnectButton.Name = "comConnectButton";
            this.comConnectButton.UseVisualStyleBackColor = false;
            this.comConnectButton.Click += new System.EventHandler(this.comConnectButton_Click);
            // 
            // comPortNameLabel
            // 
            resources.ApplyResources(this.comPortNameLabel, "comPortNameLabel");
            this.comPortNameLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comPortNameLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(0)))), ((int)(((byte)(32)))));
            this.comPortNameLabel.Name = "comPortNameLabel";
            // 
            // measureButton
            // 
            this.measureButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.measureButton, "measureButton");
            this.measureButton.Name = "measureButton";
            this.measureButton.UseVisualStyleBackColor = false;
            this.measureButton.Click += new System.EventHandler(this.measureButton_Click);
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.controlPanelTab);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.graphControl);
            // 
            // controlPanelTab
            // 
            this.controlPanelTab.Controls.Add(this.tabPage1);
            this.controlPanelTab.Controls.Add(this.tabPage2);
            resources.ApplyResources(this.controlPanelTab, "controlPanelTab");
            this.controlPanelTab.Name = "controlPanelTab";
            this.controlPanelTab.SelectedIndex = 0;
            this.controlPanelTab.SelectedIndexChanged += new System.EventHandler(this.controlPanelTab_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.socketPortPanel);
            this.tabPage1.Controls.Add(this.socketPortLabel);
            this.tabPage1.Controls.Add(this.socketStatusPanel);
            this.tabPage1.Controls.Add(this.socketIPAddressPanel);
            this.tabPage1.Controls.Add(this.socketStatusLabel);
            this.tabPage1.Controls.Add(this.socketIPAddressLabel);
            this.tabPage1.Controls.Add(this.socketTitleLabel);
            this.tabPage1.Controls.Add(this.transmissionSpeedCombo);
            this.tabPage1.Controls.Add(this.transmissionSpeedLabel);
            this.tabPage1.Controls.Add(this.scanComPortProgress);
            this.tabPage1.Controls.Add(this.initializeButton);
            this.tabPage1.Controls.Add(this.freeMarkingButton);
            this.tabPage1.Controls.Add(this.gyroscopeRangeCombo);
            this.tabPage1.Controls.Add(this.gyroscopeRangeLabel);
            this.tabPage1.Controls.Add(this.accRangeCombo);
            this.tabPage1.Controls.Add(this.accRangeLabel);
            this.tabPage1.Controls.Add(this.modeSelectCombo);
            this.tabPage1.Controls.Add(this.modeSelectLabel);
            this.tabPage1.Controls.Add(this.measurementLabel);
            this.tabPage1.Controls.Add(this.connectionStatusLabel);
            this.tabPage1.Controls.Add(this.connectionStatusPanel);
            this.tabPage1.Controls.Add(this.bluetoothConnectButton);
            this.tabPage1.Controls.Add(this.bluetoothMacCombo);
            this.tabPage1.Controls.Add(this.bluetoothScanButton);
            this.tabPage1.Controls.Add(this.bluetoothNameLabel);
            this.tabPage1.Controls.Add(this.scanPortButton);
            this.tabPage1.Controls.Add(this.chartGroupBox);
            this.tabPage1.Controls.Add(this.comPortNameCombo);
            this.tabPage1.Controls.Add(this.measureButton);
            this.tabPage1.Controls.Add(this.comPortNameLabel);
            this.tabPage1.Controls.Add(this.comConnectButton);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // socketPortPanel
            // 
            this.socketPortPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(0)))), ((int)(((byte)(32)))));
            resources.ApplyResources(this.socketPortPanel, "socketPortPanel");
            this.socketPortPanel.Name = "socketPortPanel";
            // 
            // socketPortLabel
            // 
            resources.ApplyResources(this.socketPortLabel, "socketPortLabel");
            this.socketPortLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.socketPortLabel.Name = "socketPortLabel";
            // 
            // socketStatusPanel
            // 
            this.socketStatusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(0)))), ((int)(((byte)(32)))));
            resources.ApplyResources(this.socketStatusPanel, "socketStatusPanel");
            this.socketStatusPanel.Name = "socketStatusPanel";
            // 
            // socketIPAddressPanel
            // 
            this.socketIPAddressPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(0)))), ((int)(((byte)(32)))));
            resources.ApplyResources(this.socketIPAddressPanel, "socketIPAddressPanel");
            this.socketIPAddressPanel.Name = "socketIPAddressPanel";
            // 
            // socketStatusLabel
            // 
            resources.ApplyResources(this.socketStatusLabel, "socketStatusLabel");
            this.socketStatusLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.socketStatusLabel.Name = "socketStatusLabel";
            // 
            // socketIPAddressLabel
            // 
            resources.ApplyResources(this.socketIPAddressLabel, "socketIPAddressLabel");
            this.socketIPAddressLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.socketIPAddressLabel.Name = "socketIPAddressLabel";
            // 
            // socketTitleLabel
            // 
            resources.ApplyResources(this.socketTitleLabel, "socketTitleLabel");
            this.socketTitleLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.socketTitleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(0)))), ((int)(((byte)(32)))));
            this.socketTitleLabel.Name = "socketTitleLabel";
            // 
            // transmissionSpeedCombo
            // 
            this.transmissionSpeedCombo.BackColor = System.Drawing.Color.LightGray;
            this.transmissionSpeedCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.transmissionSpeedCombo, "transmissionSpeedCombo");
            this.transmissionSpeedCombo.FormattingEnabled = true;
            this.transmissionSpeedCombo.Name = "transmissionSpeedCombo";
            // 
            // transmissionSpeedLabel
            // 
            resources.ApplyResources(this.transmissionSpeedLabel, "transmissionSpeedLabel");
            this.transmissionSpeedLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.transmissionSpeedLabel.Name = "transmissionSpeedLabel";
            // 
            // scanComPortProgress
            // 
            resources.ApplyResources(this.scanComPortProgress, "scanComPortProgress");
            this.scanComPortProgress.Name = "scanComPortProgress";
            this.scanComPortProgress.Step = 1;
            this.scanComPortProgress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // initializeButton
            // 
            this.initializeButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.initializeButton, "initializeButton");
            this.initializeButton.ForeColor = System.Drawing.SystemColors.WindowText;
            this.initializeButton.Name = "initializeButton";
            this.initializeButton.UseVisualStyleBackColor = false;
            this.initializeButton.Click += new System.EventHandler(this.initializeButton_Click);
            // 
            // freeMarkingButton
            // 
            this.freeMarkingButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.freeMarkingButton, "freeMarkingButton");
            this.freeMarkingButton.ForeColor = System.Drawing.SystemColors.WindowText;
            this.freeMarkingButton.Name = "freeMarkingButton";
            this.freeMarkingButton.UseVisualStyleBackColor = false;
            this.freeMarkingButton.Click += new System.EventHandler(this.freeMarkingButton_Click);
            // 
            // gyroscopeRangeCombo
            // 
            this.gyroscopeRangeCombo.BackColor = System.Drawing.Color.LightGray;
            this.gyroscopeRangeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.gyroscopeRangeCombo, "gyroscopeRangeCombo");
            this.gyroscopeRangeCombo.ForeColor = System.Drawing.SystemColors.WindowText;
            this.gyroscopeRangeCombo.FormattingEnabled = true;
            this.gyroscopeRangeCombo.Name = "gyroscopeRangeCombo";
            // 
            // gyroscopeRangeLabel
            // 
            resources.ApplyResources(this.gyroscopeRangeLabel, "gyroscopeRangeLabel");
            this.gyroscopeRangeLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.gyroscopeRangeLabel.Name = "gyroscopeRangeLabel";
            // 
            // accRangeCombo
            // 
            this.accRangeCombo.BackColor = System.Drawing.Color.LightGray;
            this.accRangeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.accRangeCombo, "accRangeCombo");
            this.accRangeCombo.FormattingEnabled = true;
            this.accRangeCombo.Name = "accRangeCombo";
            // 
            // accRangeLabel
            // 
            resources.ApplyResources(this.accRangeLabel, "accRangeLabel");
            this.accRangeLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.accRangeLabel.Name = "accRangeLabel";
            // 
            // modeSelectCombo
            // 
            this.modeSelectCombo.BackColor = System.Drawing.Color.LightGray;
            this.modeSelectCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.modeSelectCombo, "modeSelectCombo");
            this.modeSelectCombo.FormattingEnabled = true;
            this.modeSelectCombo.Name = "modeSelectCombo";
            this.modeSelectCombo.SelectedIndexChanged += new System.EventHandler(this.modeSelectCombo_SelectedIndexChanged);
            // 
            // modeSelectLabel
            // 
            resources.ApplyResources(this.modeSelectLabel, "modeSelectLabel");
            this.modeSelectLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.modeSelectLabel.Name = "modeSelectLabel";
            // 
            // measurementLabel
            // 
            resources.ApplyResources(this.measurementLabel, "measurementLabel");
            this.measurementLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.measurementLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(0)))), ((int)(((byte)(32)))));
            this.measurementLabel.Name = "measurementLabel";
            // 
            // connectionStatusLabel
            // 
            resources.ApplyResources(this.connectionStatusLabel, "connectionStatusLabel");
            this.connectionStatusLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.connectionStatusLabel.Name = "connectionStatusLabel";
            // 
            // connectionStatusPanel
            // 
            this.connectionStatusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(0)))), ((int)(((byte)(32)))));
            resources.ApplyResources(this.connectionStatusPanel, "connectionStatusPanel");
            this.connectionStatusPanel.Name = "connectionStatusPanel";
            // 
            // bluetoothConnectButton
            // 
            this.bluetoothConnectButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.bluetoothConnectButton, "bluetoothConnectButton");
            this.bluetoothConnectButton.Name = "bluetoothConnectButton";
            this.bluetoothConnectButton.UseVisualStyleBackColor = false;
            this.bluetoothConnectButton.Click += new System.EventHandler(this.bluetoothConnectButton_Click);
            // 
            // bluetoothMacCombo
            // 
            this.bluetoothMacCombo.BackColor = System.Drawing.Color.LightGray;
            this.bluetoothMacCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.bluetoothMacCombo, "bluetoothMacCombo");
            this.bluetoothMacCombo.FormattingEnabled = true;
            this.bluetoothMacCombo.Name = "bluetoothMacCombo";
            // 
            // bluetoothScanButton
            // 
            this.bluetoothScanButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.bluetoothScanButton, "bluetoothScanButton");
            this.bluetoothScanButton.Name = "bluetoothScanButton";
            this.bluetoothScanButton.UseVisualStyleBackColor = false;
            this.bluetoothScanButton.Click += new System.EventHandler(this.bluetoothScanButton_Click);
            // 
            // bluetoothNameLabel
            // 
            resources.ApplyResources(this.bluetoothNameLabel, "bluetoothNameLabel");
            this.bluetoothNameLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bluetoothNameLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(0)))), ((int)(((byte)(32)))));
            this.bluetoothNameLabel.Name = "bluetoothNameLabel";
            // 
            // scanPortButton
            // 
            this.scanPortButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.scanPortButton, "scanPortButton");
            this.scanPortButton.Name = "scanPortButton";
            this.scanPortButton.UseVisualStyleBackColor = false;
            this.scanPortButton.Click += new System.EventHandler(this.scanPortButton_Click);
            // 
            // chartGroupBox
            // 
            this.chartGroupBox.BackColor = System.Drawing.Color.White;
            this.chartGroupBox.Controls.Add(this.chartApplyButton);
            this.chartGroupBox.Controls.Add(this.chart3Label);
            this.chartGroupBox.Controls.Add(this.chart2Label);
            this.chartGroupBox.Controls.Add(this.chart1Label);
            this.chartGroupBox.Controls.Add(this.chart3Select);
            this.chartGroupBox.Controls.Add(this.chart2Select);
            this.chartGroupBox.Controls.Add(this.chart1Select);
            this.chartGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            resources.ApplyResources(this.chartGroupBox, "chartGroupBox");
            this.chartGroupBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(0)))), ((int)(((byte)(32)))));
            this.chartGroupBox.Name = "chartGroupBox";
            this.chartGroupBox.TabStop = false;
            // 
            // chartApplyButton
            // 
            this.chartApplyButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.chartApplyButton, "chartApplyButton");
            this.chartApplyButton.ForeColor = System.Drawing.SystemColors.WindowText;
            this.chartApplyButton.Name = "chartApplyButton";
            this.chartApplyButton.UseVisualStyleBackColor = false;
            this.chartApplyButton.Click += new System.EventHandler(this.chartApplyButton_Click);
            // 
            // chart3Label
            // 
            resources.ApplyResources(this.chart3Label, "chart3Label");
            this.chart3Label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chart3Label.ForeColor = System.Drawing.SystemColors.ControlText;
            this.chart3Label.Name = "chart3Label";
            // 
            // chart2Label
            // 
            resources.ApplyResources(this.chart2Label, "chart2Label");
            this.chart2Label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chart2Label.ForeColor = System.Drawing.SystemColors.ControlText;
            this.chart2Label.Name = "chart2Label";
            // 
            // chart1Label
            // 
            resources.ApplyResources(this.chart1Label, "chart1Label");
            this.chart1Label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chart1Label.ForeColor = System.Drawing.SystemColors.ControlText;
            this.chart1Label.Name = "chart1Label";
            // 
            // chart3Select
            // 
            this.chart3Select.BackColor = System.Drawing.Color.LightGray;
            this.chart3Select.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.chart3Select, "chart3Select");
            this.chart3Select.FormattingEnabled = true;
            this.chart3Select.Name = "chart3Select";
            // 
            // chart2Select
            // 
            this.chart2Select.BackColor = System.Drawing.Color.LightGray;
            this.chart2Select.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.chart2Select, "chart2Select");
            this.chart2Select.FormattingEnabled = true;
            this.chart2Select.Name = "chart2Select";
            // 
            // chart1Select
            // 
            this.chart1Select.BackColor = System.Drawing.Color.LightGray;
            this.chart1Select.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.chart1Select, "chart1Select");
            this.chart1Select.FormattingEnabled = true;
            this.chart1Select.Name = "chart1Select";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.historyTransmissionSpeedLabel);
            this.tabPage2.Controls.Add(this.fileReadProgress);
            this.tabPage2.Controls.Add(this.historyFileGyroRangeLabel);
            this.tabPage2.Controls.Add(this.historyFileAccRangeLabel);
            this.tabPage2.Controls.Add(this.historyFileModeLabel);
            this.tabPage2.Controls.Add(this.replayPlotButton);
            this.tabPage2.Controls.Add(this.replayStopButton);
            this.tabPage2.Controls.Add(this.replayPauseButton);
            this.tabPage2.Controls.Add(this.replayStartButton);
            this.tabPage2.Controls.Add(this.replaySpeedCombo);
            this.tabPage2.Controls.Add(this.replaySpeedLabel);
            this.tabPage2.Controls.Add(this.toDatetimeText);
            this.tabPage2.Controls.Add(this.fromDatetimeText);
            this.tabPage2.Controls.Add(this.toDatatimeLabel);
            this.tabPage2.Controls.Add(this.fromDatetimeLabel);
            this.tabPage2.Controls.Add(this.rangeMessageLabel);
            this.tabPage2.Controls.Add(this.selectedFileLabel);
            this.tabPage2.Controls.Add(this.selectedFilePanel);
            this.tabPage2.Controls.Add(this.setDataFolderButton);
            this.tabPage2.Controls.Add(this.previousFileList);
            this.tabPage2.Controls.Add(this.previousFileLabel);
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // historyTransmissionSpeedLabel
            // 
            resources.ApplyResources(this.historyTransmissionSpeedLabel, "historyTransmissionSpeedLabel");
            this.historyTransmissionSpeedLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.historyTransmissionSpeedLabel.Name = "historyTransmissionSpeedLabel";
            // 
            // fileReadProgress
            // 
            resources.ApplyResources(this.fileReadProgress, "fileReadProgress");
            this.fileReadProgress.Name = "fileReadProgress";
            this.fileReadProgress.Step = 1;
            this.fileReadProgress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // historyFileGyroRangeLabel
            // 
            resources.ApplyResources(this.historyFileGyroRangeLabel, "historyFileGyroRangeLabel");
            this.historyFileGyroRangeLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.historyFileGyroRangeLabel.Name = "historyFileGyroRangeLabel";
            // 
            // historyFileAccRangeLabel
            // 
            resources.ApplyResources(this.historyFileAccRangeLabel, "historyFileAccRangeLabel");
            this.historyFileAccRangeLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.historyFileAccRangeLabel.Name = "historyFileAccRangeLabel";
            // 
            // historyFileModeLabel
            // 
            resources.ApplyResources(this.historyFileModeLabel, "historyFileModeLabel");
            this.historyFileModeLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.historyFileModeLabel.Name = "historyFileModeLabel";
            // 
            // replayPlotButton
            // 
            this.replayPlotButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.replayPlotButton, "replayPlotButton");
            this.replayPlotButton.Name = "replayPlotButton";
            this.replayPlotButton.UseVisualStyleBackColor = false;
            this.replayPlotButton.Click += new System.EventHandler(this.replayPlotButton_Click);
            // 
            // replayStopButton
            // 
            this.replayStopButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.replayStopButton, "replayStopButton");
            this.replayStopButton.Name = "replayStopButton";
            this.replayStopButton.UseVisualStyleBackColor = false;
            this.replayStopButton.Click += new System.EventHandler(this.replayStopButton_Click);
            // 
            // replayPauseButton
            // 
            this.replayPauseButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.replayPauseButton, "replayPauseButton");
            this.replayPauseButton.Name = "replayPauseButton";
            this.replayPauseButton.UseVisualStyleBackColor = false;
            this.replayPauseButton.Click += new System.EventHandler(this.replayPauseButton_Click);
            // 
            // replayStartButton
            // 
            this.replayStartButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.replayStartButton, "replayStartButton");
            this.replayStartButton.Name = "replayStartButton";
            this.replayStartButton.UseVisualStyleBackColor = false;
            this.replayStartButton.Click += new System.EventHandler(this.replayStartButton_Click);
            // 
            // replaySpeedCombo
            // 
            this.replaySpeedCombo.BackColor = System.Drawing.Color.LightGray;
            this.replaySpeedCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.replaySpeedCombo, "replaySpeedCombo");
            this.replaySpeedCombo.FormattingEnabled = true;
            this.replaySpeedCombo.Name = "replaySpeedCombo";
            // 
            // replaySpeedLabel
            // 
            resources.ApplyResources(this.replaySpeedLabel, "replaySpeedLabel");
            this.replaySpeedLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.replaySpeedLabel.Name = "replaySpeedLabel";
            // 
            // toDatetimeText
            // 
            resources.ApplyResources(this.toDatetimeText, "toDatetimeText");
            this.toDatetimeText.Name = "toDatetimeText";
            // 
            // fromDatetimeText
            // 
            resources.ApplyResources(this.fromDatetimeText, "fromDatetimeText");
            this.fromDatetimeText.Name = "fromDatetimeText";
            // 
            // toDatatimeLabel
            // 
            resources.ApplyResources(this.toDatatimeLabel, "toDatatimeLabel");
            this.toDatatimeLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.toDatatimeLabel.Name = "toDatatimeLabel";
            // 
            // fromDatetimeLabel
            // 
            resources.ApplyResources(this.fromDatetimeLabel, "fromDatetimeLabel");
            this.fromDatetimeLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.fromDatetimeLabel.Name = "fromDatetimeLabel";
            // 
            // rangeMessageLabel
            // 
            resources.ApplyResources(this.rangeMessageLabel, "rangeMessageLabel");
            this.rangeMessageLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rangeMessageLabel.Name = "rangeMessageLabel";
            // 
            // selectedFileLabel
            // 
            resources.ApplyResources(this.selectedFileLabel, "selectedFileLabel");
            this.selectedFileLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.selectedFileLabel.Name = "selectedFileLabel";
            // 
            // selectedFilePanel
            // 
            this.selectedFilePanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(0)))), ((int)(((byte)(32)))));
            resources.ApplyResources(this.selectedFilePanel, "selectedFilePanel");
            this.selectedFilePanel.Name = "selectedFilePanel";
            // 
            // setDataFolderButton
            // 
            this.setDataFolderButton.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.setDataFolderButton, "setDataFolderButton");
            this.setDataFolderButton.Name = "setDataFolderButton";
            this.setDataFolderButton.UseVisualStyleBackColor = false;
            this.setDataFolderButton.Click += new System.EventHandler(this.setDataFolderButton_Click);
            // 
            // previousFileList
            // 
            this.previousFileList.FormattingEnabled = true;
            resources.ApplyResources(this.previousFileList, "previousFileList");
            this.previousFileList.Name = "previousFileList";
            this.previousFileList.SelectedIndexChanged += new System.EventHandler(this.previousFileList_SelectedIndexChanged);
            // 
            // previousFileLabel
            // 
            resources.ApplyResources(this.previousFileLabel, "previousFileLabel");
            this.previousFileLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.previousFileLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(0)))), ((int)(((byte)(32)))));
            this.previousFileLabel.Name = "previousFileLabel";
            // 
            // graphControl
            // 
            axisBean1.AdRangeMax = 10D;
            axisBean1.AdRangeMin = -10D;
            axisBean1.AxisColor = System.Drawing.Color.Silver;
            axisBean1.AxisMax = 10D;
            axisBean1.AxisMin = -10D;
            axisBean1.DispOrder = 1;
            axisBean1.GridLineVisible = true;
            axisBean1.GridResolution = 2D;
            axisBean1.Id = 1;
            axisBean1.IsY2Axis = false;
            axisBean1.Name = "Accelerometer";
            axisBean1.UnitName = "m/s^2";
            axisBean1.YAxisIndex = 0;
            this.graphControl.Chart1Axis = axisBean1;
            axisBean2.AdRangeMax = 10D;
            axisBean2.AdRangeMin = -10D;
            axisBean2.AxisColor = System.Drawing.Color.Silver;
            axisBean2.AxisMax = 10D;
            axisBean2.AxisMin = -10D;
            axisBean2.DispOrder = 2;
            axisBean2.GridLineVisible = true;
            axisBean2.GridResolution = 2D;
            axisBean2.Id = 2;
            axisBean2.IsY2Axis = false;
            axisBean2.Name = "Gyroscope";
            axisBean2.UnitName = "rad/s";
            axisBean2.YAxisIndex = 0;
            this.graphControl.Chart2Axis = axisBean2;
            axisBean3.AdRangeMax = 10D;
            axisBean3.AdRangeMin = -10D;
            axisBean3.AxisColor = System.Drawing.Color.Silver;
            axisBean3.AxisMax = 10D;
            axisBean3.AxisMin = -10D;
            axisBean3.DispOrder = 3;
            axisBean3.GridLineVisible = true;
            axisBean3.GridResolution = 2D;
            axisBean3.Id = 3;
            axisBean3.IsY2Axis = false;
            axisBean3.Name = "Electrooculography";
            axisBean3.UnitName = "mV";
            axisBean3.YAxisIndex = 0;
            this.graphControl.Chart3Axis = axisBean3;
            this.graphControl.DataEraseMode = false;
            resources.ApplyResources(this.graphControl, "graphControl");
            this.graphControl.GraphOperation = true;
            this.graphControl.Name = "graphControl";
            // 
            // statusStrip1
            // 
            this.statusStrip1.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.statusStrip1, "statusStrip1");
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.batteryLevelLabel,
            this.batteryLevelProgress,
            this.deviceVersionTitleLabel,
            this.Label1,
            this.deviceVersionLabel,
            this.dongleVersionTitleLabel,
            this.dongleVersionLabel,
            this.springLabel,
            this.successRateLabel,
            this.successRateProgress,
            this.transientDataLossLabel,
            this.transientDataLossProgress});
            this.statusStrip1.Name = "statusStrip1";
            // 
            // batteryLevelLabel
            // 
            resources.ApplyResources(this.batteryLevelLabel, "batteryLevelLabel");
            this.batteryLevelLabel.Name = "batteryLevelLabel";
            // 
            // batteryLevelProgress
            // 
            resources.ApplyResources(this.batteryLevelProgress, "batteryLevelProgress");
            this.batteryLevelProgress.Maximum = 5;
            this.batteryLevelProgress.Name = "batteryLevelProgress";
            this.batteryLevelProgress.Paint += new System.Windows.Forms.PaintEventHandler(this.batteryLevelProgress_Paint);
            // 
            // deviceVersionTitleLabel
            // 
            this.deviceVersionTitleLabel.Name = "deviceVersionTitleLabel";
            resources.ApplyResources(this.deviceVersionTitleLabel, "deviceVersionTitleLabel");
            // 
            // Label1
            // 
            this.Label1.Name = "Label1";
            resources.ApplyResources(this.Label1, "Label1");
            // 
            // deviceVersionLabel
            // 
            this.deviceVersionLabel.Name = "deviceVersionLabel";
            resources.ApplyResources(this.deviceVersionLabel, "deviceVersionLabel");
            // 
            // dongleVersionTitleLabel
            // 
            this.dongleVersionTitleLabel.Name = "dongleVersionTitleLabel";
            resources.ApplyResources(this.dongleVersionTitleLabel, "dongleVersionTitleLabel");
            // 
            // dongleVersionLabel
            // 
            this.dongleVersionLabel.Name = "dongleVersionLabel";
            resources.ApplyResources(this.dongleVersionLabel, "dongleVersionLabel");
            // 
            // springLabel
            // 
            this.springLabel.Name = "springLabel";
            resources.ApplyResources(this.springLabel, "springLabel");
            this.springLabel.Spring = true;
            // 
            // successRateLabel
            // 
            this.successRateLabel.Name = "successRateLabel";
            resources.ApplyResources(this.successRateLabel, "successRateLabel");
            // 
            // successRateProgress
            // 
            this.successRateProgress.Name = "successRateProgress";
            resources.ApplyResources(this.successRateProgress, "successRateProgress");
            this.successRateProgress.Paint += new System.Windows.Forms.PaintEventHandler(this.successRateProgress_Paint);
            // 
            // transientDataLossLabel
            // 
            this.transientDataLossLabel.Name = "transientDataLossLabel";
            resources.ApplyResources(this.transientDataLossLabel, "transientDataLossLabel");
            // 
            // transientDataLossProgress
            // 
            this.transientDataLossProgress.Name = "transientDataLossProgress";
            resources.ApplyResources(this.transientDataLossProgress, "transientDataLossProgress");
            this.transientDataLossProgress.Paint += new System.Windows.Forms.PaintEventHandler(this.transientDataLossProgress_Paint);
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.versionToolStripMenuItem,
            this.settingToolStripMenuItem});
            this.menuStrip1.Name = "menuStrip1";
            // 
            // versionToolStripMenuItem
            // 
            this.versionToolStripMenuItem.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            resources.ApplyResources(this.versionToolStripMenuItem, "versionToolStripMenuItem");
            this.versionToolStripMenuItem.Name = "versionToolStripMenuItem";
            this.versionToolStripMenuItem.Click += new System.EventHandler(this.versionToolStripMenuItem_Click);
            // 
            // settingToolStripMenuItem
            // 
            this.settingToolStripMenuItem.Name = "settingToolStripMenuItem";
            resources.ApplyResources(this.settingToolStripMenuItem, "settingToolStripMenuItem");
            this.settingToolStripMenuItem.Click += new System.EventHandler(this.settingToolStripMenuItem_Click);
            // 
            // batteryLevelTimer
            // 
            this.batteryLevelTimer.Interval = 1000;
            this.batteryLevelTimer.Tick += new System.EventHandler(this.batteryLevelTimer_Tick);
            // 
            // mainForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "mainForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.mainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.mainForm_FormClosed);
            this.Load += new System.EventHandler(this.mainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.settings)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.controlPanelTab.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.chartGroupBox.ResumeLayout(false);
            this.chartGroupBox.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comPortNameCombo;
        private System.Windows.Forms.Button comConnectButton;
        private System.Windows.Forms.Label comPortNameLabel;
        private System.Windows.Forms.Button measureButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TabControl controlPanelTab;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private GraphControl graphControl;
        private System.Windows.Forms.GroupBox chartGroupBox;
        private System.Windows.Forms.Button chartApplyButton;
        private System.Windows.Forms.Label chart3Label;
        private System.Windows.Forms.Label chart2Label;
        private System.Windows.Forms.Label chart1Label;
        private ComboBoxEx chart3Select;
        private ComboBoxEx chart2Select;
        private ComboBoxEx chart1Select;
        private System.Windows.Forms.Button scanPortButton;
        private System.Windows.Forms.Label measurementLabel;
        private System.Windows.Forms.Label connectionStatusLabel;
        private System.Windows.Forms.Panel connectionStatusPanel;
        private System.Windows.Forms.Button bluetoothConnectButton;
        private System.Windows.Forms.ComboBox bluetoothMacCombo;
        private System.Windows.Forms.Button bluetoothScanButton;
        private System.Windows.Forms.Label bluetoothNameLabel;
        private System.Windows.Forms.Button initializeButton;
        private System.Windows.Forms.Button freeMarkingButton;
        private System.Windows.Forms.ComboBox gyroscopeRangeCombo;
        private System.Windows.Forms.Label gyroscopeRangeLabel;
        private System.Windows.Forms.ComboBox accRangeCombo;
        private System.Windows.Forms.Label accRangeLabel;
        private System.Windows.Forms.ComboBox modeSelectCombo;
        private System.Windows.Forms.Label modeSelectLabel;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem versionToolStripMenuItem;
        private System.Windows.Forms.Button setDataFolderButton;
        private System.Windows.Forms.ListBox previousFileList;
        private System.Windows.Forms.Label previousFileLabel;
        private System.Windows.Forms.Label selectedFileLabel;
        private System.Windows.Forms.Panel selectedFilePanel;
        private System.Windows.Forms.Label toDatatimeLabel;
        private System.Windows.Forms.Label fromDatetimeLabel;
        private System.Windows.Forms.Label rangeMessageLabel;
        private System.Windows.Forms.TextBox toDatetimeText;
        private System.Windows.Forms.TextBox fromDatetimeText;
        private System.Windows.Forms.Button replayPlotButton;
        private System.Windows.Forms.Button replayStopButton;
        private System.Windows.Forms.Button replayPauseButton;
        private System.Windows.Forms.Button replayStartButton;
        private System.Windows.Forms.ComboBox replaySpeedCombo;
        private System.Windows.Forms.Label replaySpeedLabel;
        private System.Windows.Forms.ToolStripStatusLabel batteryLevelLabel;
        private System.Windows.Forms.ToolStripProgressBar batteryLevelProgress;
        private System.Windows.Forms.ToolStripMenuItem settingToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel deviceVersionTitleLabel;
        private System.Windows.Forms.ToolStripStatusLabel Label1;
        private System.Windows.Forms.ToolStripStatusLabel deviceVersionLabel;
        private System.Windows.Forms.Timer batteryLevelTimer;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.ToolStripStatusLabel dongleVersionTitleLabel;
        private System.Windows.Forms.ToolStripStatusLabel dongleVersionLabel;
        private System.Windows.Forms.Label historyFileGyroRangeLabel;
        private System.Windows.Forms.Label historyFileAccRangeLabel;
        private System.Windows.Forms.Label historyFileModeLabel;
        private System.Windows.Forms.ProgressBar fileReadProgress;
        private System.Windows.Forms.ProgressBar scanComPortProgress;
        private System.Windows.Forms.ComboBox transmissionSpeedCombo;
        private System.Windows.Forms.Label transmissionSpeedLabel;
        private System.Windows.Forms.Label historyTransmissionSpeedLabel;
        private System.Windows.Forms.ToolStripStatusLabel springLabel;
        private System.Windows.Forms.ToolStripStatusLabel successRateLabel;
        private System.Windows.Forms.ToolStripProgressBar successRateProgress;
        private System.Windows.Forms.ToolStripStatusLabel transientDataLossLabel;
        private System.Windows.Forms.ToolStripProgressBar transientDataLossProgress;
        private System.Windows.Forms.Label socketTitleLabel;
        private System.Windows.Forms.Label socketIPAddressLabel;
        private System.Windows.Forms.Label socketStatusLabel;
        private System.Windows.Forms.Panel socketStatusPanel;
        private System.Windows.Forms.Panel socketIPAddressPanel;
        private System.Windows.Forms.Panel socketPortPanel;
        private System.Windows.Forms.Label socketPortLabel;

    }
}

