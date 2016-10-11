namespace JINS_MEME_DataLogger
{
    partial class SettingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.applyButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.saveFolderLabel = new System.Windows.Forms.Label();
            this.saveFolderText = new System.Windows.Forms.TextBox();
            this.saveFolderReferenceButton = new System.Windows.Forms.Button();
            this.markingTimeLabel = new System.Windows.Forms.Label();
            this.markingTimeText = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.accZAxisOffsetText = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.accYAxisOffsetText = new System.Windows.Forms.TextBox();
            this.accXAxisOffsetText = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.showSaveFileDialogCheck = new System.Windows.Forms.CheckBox();
            this.recordFileDateFormatCombo = new System.Windows.Forms.ComboBox();
            this.recordFileDateFormatLabel = new System.Windows.Forms.Label();
            this.saveFolderOpenButton = new System.Windows.Forms.Button();
            this.socketPortText = new System.Windows.Forms.TextBox();
            this.socketPortLabel = new System.Windows.Forms.Label();
            this.socketAddressLabel = new System.Windows.Forms.Label();
            this.externalSocketGroup = new System.Windows.Forms.GroupBox();
            this.socketAddressCombo = new System.Windows.Forms.ComboBox();
            this.useSocketCheck = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.externalSocketGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // applyButton
            // 
            this.applyButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.applyButton.Location = new System.Drawing.Point(565, 195);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 25);
            this.applyButton.TabIndex = 12;
            this.applyButton.Text = "Apply";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancelButton.Location = new System.Drawing.Point(647, 195);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 25);
            this.cancelButton.TabIndex = 13;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // saveFolderLabel
            // 
            this.saveFolderLabel.AutoSize = true;
            this.saveFolderLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveFolderLabel.Location = new System.Drawing.Point(23, 29);
            this.saveFolderLabel.Name = "saveFolderLabel";
            this.saveFolderLabel.Size = new System.Drawing.Size(48, 15);
            this.saveFolderLabel.TabIndex = 0;
            this.saveFolderLabel.Text = "Save as:";
            // 
            // saveFolderText
            // 
            this.saveFolderText.Location = new System.Drawing.Point(85, 26);
            this.saveFolderText.Name = "saveFolderText";
            this.saveFolderText.Size = new System.Drawing.Size(523, 23);
            this.saveFolderText.TabIndex = 1;
            // 
            // saveFolderReferenceButton
            // 
            this.saveFolderReferenceButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveFolderReferenceButton.Location = new System.Drawing.Point(621, 11);
            this.saveFolderReferenceButton.Name = "saveFolderReferenceButton";
            this.saveFolderReferenceButton.Size = new System.Drawing.Size(101, 25);
            this.saveFolderReferenceButton.TabIndex = 2;
            this.saveFolderReferenceButton.Text = "Browse";
            this.saveFolderReferenceButton.UseVisualStyleBackColor = true;
            this.saveFolderReferenceButton.Click += new System.EventHandler(this.saveFolderReferenceButton_Click);
            // 
            // markingTimeLabel
            // 
            this.markingTimeLabel.AutoSize = true;
            this.markingTimeLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.markingTimeLabel.Location = new System.Drawing.Point(512, 83);
            this.markingTimeLabel.Name = "markingTimeLabel";
            this.markingTimeLabel.Size = new System.Drawing.Size(105, 15);
            this.markingTimeLabel.TabIndex = 8;
            this.markingTimeLabel.Text = "Marking time (ms)";
            this.markingTimeLabel.Visible = false;
            // 
            // markingTimeText
            // 
            this.markingTimeText.Location = new System.Drawing.Point(527, 101);
            this.markingTimeText.Name = "markingTimeText";
            this.markingTimeText.Size = new System.Drawing.Size(57, 23);
            this.markingTimeText.TabIndex = 9;
            this.markingTimeText.Visible = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.accZAxisOffsetText);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.accYAxisOffsetText);
            this.groupBox1.Controls.Add(this.accXAxisOffsetText);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBox1.Location = new System.Drawing.Point(15, 76);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(154, 122);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Accelerometer DC offset";
            // 
            // accZAxisOffsetText
            // 
            this.accZAxisOffsetText.Location = new System.Drawing.Point(57, 88);
            this.accZAxisOffsetText.Name = "accZAxisOffsetText";
            this.accZAxisOffsetText.Size = new System.Drawing.Size(81, 23);
            this.accZAxisOffsetText.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label5.Location = new System.Drawing.Point(8, 91);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(40, 15);
            this.label5.TabIndex = 4;
            this.label5.Text = "Z-Axis";
            // 
            // accYAxisOffsetText
            // 
            this.accYAxisOffsetText.Location = new System.Drawing.Point(57, 59);
            this.accYAxisOffsetText.Name = "accYAxisOffsetText";
            this.accYAxisOffsetText.Size = new System.Drawing.Size(81, 23);
            this.accYAxisOffsetText.TabIndex = 3;
            // 
            // accXAxisOffsetText
            // 
            this.accXAxisOffsetText.Location = new System.Drawing.Point(57, 30);
            this.accXAxisOffsetText.Name = "accXAxisOffsetText";
            this.accXAxisOffsetText.Size = new System.Drawing.Size(81, 23);
            this.accXAxisOffsetText.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label4.Location = new System.Drawing.Point(8, 62);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(40, 15);
            this.label4.TabIndex = 2;
            this.label4.Text = "Y-Axis";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label3.Location = new System.Drawing.Point(8, 33);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(40, 15);
            this.label3.TabIndex = 0;
            this.label3.Text = "X-Axis";
            // 
            // showSaveFileDialogCheck
            // 
            this.showSaveFileDialogCheck.AutoSize = true;
            this.showSaveFileDialogCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.showSaveFileDialogCheck.Location = new System.Drawing.Point(210, 76);
            this.showSaveFileDialogCheck.Name = "showSaveFileDialogCheck";
            this.showSaveFileDialogCheck.Size = new System.Drawing.Size(133, 19);
            this.showSaveFileDialogCheck.TabIndex = 5;
            this.showSaveFileDialogCheck.Text = "Show save file dialog";
            this.showSaveFileDialogCheck.UseVisualStyleBackColor = true;
            // 
            // recordFileDateFormatCombo
            // 
            this.recordFileDateFormatCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.recordFileDateFormatCombo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.recordFileDateFormatCombo.FormattingEnabled = true;
            this.recordFileDateFormatCombo.Items.AddRange(new object[] {
            "Year/Month/Day",
            "Month/Day/Year",
            "Year-Month-Day",
            "Month-Day-Year"});
            this.recordFileDateFormatCombo.Location = new System.Drawing.Point(527, 153);
            this.recordFileDateFormatCombo.Name = "recordFileDateFormatCombo";
            this.recordFileDateFormatCombo.Size = new System.Drawing.Size(127, 23);
            this.recordFileDateFormatCombo.TabIndex = 11;
            this.recordFileDateFormatCombo.Visible = false;
            // 
            // recordFileDateFormatLabel
            // 
            this.recordFileDateFormatLabel.AutoSize = true;
            this.recordFileDateFormatLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.recordFileDateFormatLabel.Location = new System.Drawing.Point(512, 135);
            this.recordFileDateFormatLabel.Name = "recordFileDateFormatLabel";
            this.recordFileDateFormatLabel.Size = new System.Drawing.Size(128, 15);
            this.recordFileDateFormatLabel.TabIndex = 10;
            this.recordFileDateFormatLabel.Text = "Record file date format";
            this.recordFileDateFormatLabel.Visible = false;
            // 
            // saveFolderOpenButton
            // 
            this.saveFolderOpenButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveFolderOpenButton.Location = new System.Drawing.Point(621, 42);
            this.saveFolderOpenButton.Name = "saveFolderOpenButton";
            this.saveFolderOpenButton.Size = new System.Drawing.Size(101, 25);
            this.saveFolderOpenButton.TabIndex = 3;
            this.saveFolderOpenButton.Text = "Open Folder";
            this.saveFolderOpenButton.UseVisualStyleBackColor = true;
            this.saveFolderOpenButton.Click += new System.EventHandler(this.saveFolderOpenButton_Click);
            // 
            // socketPortText
            // 
            this.socketPortText.Location = new System.Drawing.Point(108, 18);
            this.socketPortText.Name = "socketPortText";
            this.socketPortText.Size = new System.Drawing.Size(57, 23);
            this.socketPortText.TabIndex = 7;
            // 
            // socketPortLabel
            // 
            this.socketPortLabel.AutoSize = true;
            this.socketPortLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.socketPortLabel.Location = new System.Drawing.Point(15, 21);
            this.socketPortLabel.Name = "socketPortLabel";
            this.socketPortLabel.Size = new System.Drawing.Size(60, 15);
            this.socketPortLabel.TabIndex = 6;
            this.socketPortLabel.Text = "Local port";
            // 
            // socketAddressLabel
            // 
            this.socketAddressLabel.AutoSize = true;
            this.socketAddressLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.socketAddressLabel.Location = new System.Drawing.Point(15, 50);
            this.socketAddressLabel.Name = "socketAddressLabel";
            this.socketAddressLabel.Size = new System.Drawing.Size(78, 15);
            this.socketAddressLabel.TabIndex = 14;
            this.socketAddressLabel.Text = "Local address";
            // 
            // externalSocketGroup
            // 
            this.externalSocketGroup.Controls.Add(this.socketAddressCombo);
            this.externalSocketGroup.Controls.Add(this.socketPortText);
            this.externalSocketGroup.Controls.Add(this.socketPortLabel);
            this.externalSocketGroup.Controls.Add(this.socketAddressLabel);
            this.externalSocketGroup.Location = new System.Drawing.Point(210, 122);
            this.externalSocketGroup.Name = "externalSocketGroup";
            this.externalSocketGroup.Size = new System.Drawing.Size(252, 87);
            this.externalSocketGroup.TabIndex = 16;
            this.externalSocketGroup.TabStop = false;
            // 
            // socketAddressCombo
            // 
            this.socketAddressCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.socketAddressCombo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.socketAddressCombo.FormattingEnabled = true;
            this.socketAddressCombo.Location = new System.Drawing.Point(108, 47);
            this.socketAddressCombo.Name = "socketAddressCombo";
            this.socketAddressCombo.Size = new System.Drawing.Size(131, 23);
            this.socketAddressCombo.TabIndex = 15;
            // 
            // useSocketCheck
            // 
            this.useSocketCheck.AutoSize = true;
            this.useSocketCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.useSocketCheck.Location = new System.Drawing.Point(210, 110);
            this.useSocketCheck.Name = "useSocketCheck";
            this.useSocketCheck.Size = new System.Drawing.Size(140, 19);
            this.useSocketCheck.TabIndex = 17;
            this.useSocketCheck.Text = "External output socket";
            this.useSocketCheck.UseVisualStyleBackColor = true;
            this.useSocketCheck.CheckedChanged += new System.EventHandler(this.useSocketCheck_CheckedChanged);
            // 
            // SettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(734, 232);
            this.ControlBox = false;
            this.Controls.Add(this.useSocketCheck);
            this.Controls.Add(this.externalSocketGroup);
            this.Controls.Add(this.saveFolderOpenButton);
            this.Controls.Add(this.recordFileDateFormatLabel);
            this.Controls.Add(this.recordFileDateFormatCombo);
            this.Controls.Add(this.showSaveFileDialogCheck);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.markingTimeText);
            this.Controls.Add(this.markingTimeLabel);
            this.Controls.Add(this.saveFolderReferenceButton);
            this.Controls.Add(this.saveFolderText);
            this.Controls.Add(this.saveFolderLabel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.applyButton);
            this.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingForm";
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.externalSocketGroup.ResumeLayout(false);
            this.externalSocketGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label saveFolderLabel;
        private System.Windows.Forms.TextBox saveFolderText;
        private System.Windows.Forms.Button saveFolderReferenceButton;
        private System.Windows.Forms.Label markingTimeLabel;
        private System.Windows.Forms.TextBox markingTimeText;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox accZAxisOffsetText;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox accYAxisOffsetText;
        private System.Windows.Forms.TextBox accXAxisOffsetText;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox showSaveFileDialogCheck;
        private System.Windows.Forms.ComboBox recordFileDateFormatCombo;
        private System.Windows.Forms.Label recordFileDateFormatLabel;
        private System.Windows.Forms.Button saveFolderOpenButton;
        private System.Windows.Forms.TextBox socketPortText;
        private System.Windows.Forms.Label socketPortLabel;
        private System.Windows.Forms.Label socketAddressLabel;
        private System.Windows.Forms.GroupBox externalSocketGroup;
        private System.Windows.Forms.ComboBox socketAddressCombo;
        private System.Windows.Forms.CheckBox useSocketCheck;
    }
}