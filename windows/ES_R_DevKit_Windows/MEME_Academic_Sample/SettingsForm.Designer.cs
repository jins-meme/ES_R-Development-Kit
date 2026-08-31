namespace MEME_Academic_Sample
{
    partial class SettingsForm
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
            this.lb_SaveFilePath = new System.Windows.Forms.Label();
            this.tb_SaveFilePath = new System.Windows.Forms.TextBox();
            this.bt_SelectFolder = new MEME_Academic_Sample.UI.RoundedButton();
            this.bt_OpenFolder = new MEME_Academic_Sample.UI.RoundedButton();
            this.lb_AccOffset = new System.Windows.Forms.Label();
            this.lb_AccOffsetX = new System.Windows.Forms.Label();
            this.tb_AccOffsetX = new System.Windows.Forms.TextBox();
            this.lb_AccOffsetY = new System.Windows.Forms.Label();
            this.tb_AccOffsetY = new System.Windows.Forms.TextBox();
            this.lb_AccOffsetZ = new System.Windows.Forms.Label();
            this.tb_AccOffsetZ = new System.Windows.Forms.TextBox();
            this.lb_SaveDialog = new System.Windows.Forms.Label();
            this.ck_ShowSaveFileDialog = new System.Windows.Forms.CheckBox();
            this.lb_TimeDisplay = new System.Windows.Forms.Label();
            this.ck_ConvertToLocalTime = new System.Windows.Forms.CheckBox();
            this.lb_TcpOutput = new System.Windows.Forms.Label();
            this.ck_ExternalOutputSocket = new System.Windows.Forms.CheckBox();
            this.lb_LocalPort = new System.Windows.Forms.Label();
            this.tb_LocalPort = new System.Windows.Forms.TextBox();
            this.lb_LocalIp = new System.Windows.Forms.Label();
            this.separator = new System.Windows.Forms.Panel();
            this.bt_Cancel = new MEME_Academic_Sample.UI.RoundedButton();
            this.bt_Apply = new MEME_Academic_Sample.UI.RoundedButton();
            this.SuspendLayout();
            //
            // lb_SaveFilePath
            //
            this.lb_SaveFilePath.Location = new System.Drawing.Point(20, 22);
            this.lb_SaveFilePath.Name = "lb_SaveFilePath";
            this.lb_SaveFilePath.Size = new System.Drawing.Size(140, 20);
            this.lb_SaveFilePath.Text = "Save File Path";
            //
            // tb_SaveFilePath
            //
            this.tb_SaveFilePath.Location = new System.Drawing.Point(165, 18);
            this.tb_SaveFilePath.Name = "tb_SaveFilePath";
            this.tb_SaveFilePath.Size = new System.Drawing.Size(380, 23);
            this.tb_SaveFilePath.TabIndex = 0;
            //
            // bt_SelectFolder
            //
            this.bt_SelectFolder.Location = new System.Drawing.Point(553, 17);
            this.bt_SelectFolder.Name = "bt_SelectFolder";
            this.bt_SelectFolder.Size = new System.Drawing.Size(70, 26);
            this.bt_SelectFolder.TabIndex = 1;
            this.bt_SelectFolder.Text = "Select";
            this.bt_SelectFolder.UseVisualStyleBackColor = true;
            this.bt_SelectFolder.Click += new System.EventHandler(this.bt_SelectFolder_Click);
            //
            // bt_OpenFolder
            //
            this.bt_OpenFolder.Location = new System.Drawing.Point(629, 17);
            this.bt_OpenFolder.Name = "bt_OpenFolder";
            this.bt_OpenFolder.Size = new System.Drawing.Size(100, 26);
            this.bt_OpenFolder.TabIndex = 2;
            this.bt_OpenFolder.Text = "Open Folder";
            this.bt_OpenFolder.UseVisualStyleBackColor = true;
            this.bt_OpenFolder.Click += new System.EventHandler(this.bt_OpenFolder_Click);
            //
            // lb_AccOffset
            //
            this.lb_AccOffset.Location = new System.Drawing.Point(20, 62);
            this.lb_AccOffset.Name = "lb_AccOffset";
            this.lb_AccOffset.Size = new System.Drawing.Size(140, 20);
            this.lb_AccOffset.Text = "Acc Offset X / Y / Z";
            //
            // lb_AccOffsetX
            //
            this.lb_AccOffsetX.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_AccOffsetX.Location = new System.Drawing.Point(165, 62);
            this.lb_AccOffsetX.Name = "lb_AccOffsetX";
            this.lb_AccOffsetX.Size = new System.Drawing.Size(16, 20);
            this.lb_AccOffsetX.Text = "X";
            //
            // tb_AccOffsetX
            //
            this.tb_AccOffsetX.Location = new System.Drawing.Point(183, 58);
            this.tb_AccOffsetX.Name = "tb_AccOffsetX";
            this.tb_AccOffsetX.Size = new System.Drawing.Size(70, 23);
            this.tb_AccOffsetX.TabIndex = 3;
            //
            // lb_AccOffsetY
            //
            this.lb_AccOffsetY.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_AccOffsetY.Location = new System.Drawing.Point(263, 62);
            this.lb_AccOffsetY.Name = "lb_AccOffsetY";
            this.lb_AccOffsetY.Size = new System.Drawing.Size(16, 20);
            this.lb_AccOffsetY.Text = "Y";
            //
            // tb_AccOffsetY
            //
            this.tb_AccOffsetY.Location = new System.Drawing.Point(281, 58);
            this.tb_AccOffsetY.Name = "tb_AccOffsetY";
            this.tb_AccOffsetY.Size = new System.Drawing.Size(70, 23);
            this.tb_AccOffsetY.TabIndex = 4;
            //
            // lb_AccOffsetZ
            //
            this.lb_AccOffsetZ.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_AccOffsetZ.Location = new System.Drawing.Point(361, 62);
            this.lb_AccOffsetZ.Name = "lb_AccOffsetZ";
            this.lb_AccOffsetZ.Size = new System.Drawing.Size(16, 20);
            this.lb_AccOffsetZ.Text = "Z";
            //
            // tb_AccOffsetZ
            //
            this.tb_AccOffsetZ.Location = new System.Drawing.Point(379, 58);
            this.tb_AccOffsetZ.Name = "tb_AccOffsetZ";
            this.tb_AccOffsetZ.Size = new System.Drawing.Size(70, 23);
            this.tb_AccOffsetZ.TabIndex = 5;
            //
            // lb_SaveDialog
            //
            this.lb_SaveDialog.Location = new System.Drawing.Point(20, 102);
            this.lb_SaveDialog.Name = "lb_SaveDialog";
            this.lb_SaveDialog.Size = new System.Drawing.Size(140, 20);
            this.lb_SaveDialog.Text = "Save Dialog";
            //
            // ck_ShowSaveFileDialog
            //
            this.ck_ShowSaveFileDialog.AutoSize = true;
            this.ck_ShowSaveFileDialog.Location = new System.Drawing.Point(165, 102);
            this.ck_ShowSaveFileDialog.Name = "ck_ShowSaveFileDialog";
            this.ck_ShowSaveFileDialog.Size = new System.Drawing.Size(270, 19);
            this.ck_ShowSaveFileDialog.TabIndex = 6;
            this.ck_ShowSaveFileDialog.Text = "Show save file dialog after measurement";
            this.ck_ShowSaveFileDialog.UseVisualStyleBackColor = true;
            //
            // lb_TimeDisplay
            //
            this.lb_TimeDisplay.Location = new System.Drawing.Point(20, 137);
            this.lb_TimeDisplay.Name = "lb_TimeDisplay";
            this.lb_TimeDisplay.Size = new System.Drawing.Size(140, 20);
            this.lb_TimeDisplay.Text = "Time Display";
            //
            // ck_ConvertToLocalTime
            //
            this.ck_ConvertToLocalTime.AutoSize = true;
            this.ck_ConvertToLocalTime.Location = new System.Drawing.Point(165, 137);
            this.ck_ConvertToLocalTime.Name = "ck_ConvertToLocalTime";
            this.ck_ConvertToLocalTime.Size = new System.Drawing.Size(380, 19);
            this.ck_ConvertToLocalTime.TabIndex = 7;
            this.ck_ConvertToLocalTime.Text = "Convert displayed time to local time (data is recorded in UTC)";
            this.ck_ConvertToLocalTime.UseVisualStyleBackColor = true;
            //
            // lb_TcpOutput
            //
            this.lb_TcpOutput.Location = new System.Drawing.Point(20, 172);
            this.lb_TcpOutput.Name = "lb_TcpOutput";
            this.lb_TcpOutput.Size = new System.Drawing.Size(140, 20);
            this.lb_TcpOutput.Text = "TCP Output";
            //
            // ck_ExternalOutputSocket
            //
            this.ck_ExternalOutputSocket.AutoSize = true;
            this.ck_ExternalOutputSocket.Location = new System.Drawing.Point(165, 172);
            this.ck_ExternalOutputSocket.Name = "ck_ExternalOutputSocket";
            this.ck_ExternalOutputSocket.Size = new System.Drawing.Size(220, 19);
            this.ck_ExternalOutputSocket.TabIndex = 8;
            this.ck_ExternalOutputSocket.Text = "External output via TCP socket";
            this.ck_ExternalOutputSocket.UseVisualStyleBackColor = true;
            //
            // lb_LocalPort
            //
            this.lb_LocalPort.Location = new System.Drawing.Point(20, 207);
            this.lb_LocalPort.Name = "lb_LocalPort";
            this.lb_LocalPort.Size = new System.Drawing.Size(140, 20);
            this.lb_LocalPort.Text = "Local Port";
            //
            // tb_LocalPort
            //
            this.tb_LocalPort.Location = new System.Drawing.Point(165, 203);
            this.tb_LocalPort.Name = "tb_LocalPort";
            this.tb_LocalPort.Size = new System.Drawing.Size(90, 23);
            this.tb_LocalPort.TabIndex = 9;
            //
            // lb_LocalIp
            //
            this.lb_LocalIp.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lb_LocalIp.Location = new System.Drawing.Point(265, 207);
            this.lb_LocalIp.Name = "lb_LocalIp";
            this.lb_LocalIp.Size = new System.Drawing.Size(300, 20);
            this.lb_LocalIp.Text = "Local IP:";
            //
            // separator
            //
            this.separator.BackColor = System.Drawing.SystemColors.ControlDark;
            this.separator.Location = new System.Drawing.Point(20, 245);
            this.separator.Name = "separator";
            this.separator.Size = new System.Drawing.Size(709, 1);
            //
            // bt_Cancel
            //
            this.bt_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bt_Cancel.Location = new System.Drawing.Point(559, 258);
            this.bt_Cancel.Name = "bt_Cancel";
            this.bt_Cancel.Size = new System.Drawing.Size(80, 28);
            this.bt_Cancel.TabIndex = 11;
            this.bt_Cancel.Text = "Cancel";
            this.bt_Cancel.UseVisualStyleBackColor = true;
            //
            // bt_Apply
            //
            this.bt_Apply.Location = new System.Drawing.Point(649, 258);
            this.bt_Apply.Name = "bt_Apply";
            this.bt_Apply.Size = new System.Drawing.Size(80, 28);
            this.bt_Apply.TabIndex = 10;
            this.bt_Apply.Text = "Apply";
            this.bt_Apply.UseVisualStyleBackColor = true;
            this.bt_Apply.Click += new System.EventHandler(this.bt_Apply_Click);
            //
            // SettingsForm
            //
            this.AcceptButton = this.bt_Apply;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bt_Cancel;
            this.ClientSize = new System.Drawing.Size(749, 300);
            this.Controls.Add(this.bt_Apply);
            this.Controls.Add(this.bt_Cancel);
            this.Controls.Add(this.separator);
            this.Controls.Add(this.lb_LocalIp);
            this.Controls.Add(this.tb_LocalPort);
            this.Controls.Add(this.lb_LocalPort);
            this.Controls.Add(this.ck_ExternalOutputSocket);
            this.Controls.Add(this.lb_TcpOutput);
            this.Controls.Add(this.ck_ConvertToLocalTime);
            this.Controls.Add(this.lb_TimeDisplay);
            this.Controls.Add(this.ck_ShowSaveFileDialog);
            this.Controls.Add(this.lb_SaveDialog);
            this.Controls.Add(this.tb_AccOffsetZ);
            this.Controls.Add(this.lb_AccOffsetZ);
            this.Controls.Add(this.tb_AccOffsetY);
            this.Controls.Add(this.lb_AccOffsetY);
            this.Controls.Add(this.tb_AccOffsetX);
            this.Controls.Add(this.lb_AccOffsetX);
            this.Controls.Add(this.lb_AccOffset);
            this.Controls.Add(this.bt_OpenFolder);
            this.Controls.Add(this.bt_SelectFolder);
            this.Controls.Add(this.tb_SaveFilePath);
            this.Controls.Add(this.lb_SaveFilePath);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Setting";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lb_SaveFilePath;
        private System.Windows.Forms.TextBox tb_SaveFilePath;
        private MEME_Academic_Sample.UI.RoundedButton bt_SelectFolder;
        private MEME_Academic_Sample.UI.RoundedButton bt_OpenFolder;
        private System.Windows.Forms.Label lb_AccOffset;
        private System.Windows.Forms.Label lb_AccOffsetX;
        private System.Windows.Forms.TextBox tb_AccOffsetX;
        private System.Windows.Forms.Label lb_AccOffsetY;
        private System.Windows.Forms.TextBox tb_AccOffsetY;
        private System.Windows.Forms.Label lb_AccOffsetZ;
        private System.Windows.Forms.TextBox tb_AccOffsetZ;
        private System.Windows.Forms.Label lb_SaveDialog;
        private System.Windows.Forms.CheckBox ck_ShowSaveFileDialog;
        private System.Windows.Forms.Label lb_TimeDisplay;
        private System.Windows.Forms.CheckBox ck_ConvertToLocalTime;
        private System.Windows.Forms.Label lb_TcpOutput;
        private System.Windows.Forms.CheckBox ck_ExternalOutputSocket;
        private System.Windows.Forms.Label lb_LocalPort;
        private System.Windows.Forms.TextBox tb_LocalPort;
        private System.Windows.Forms.Label lb_LocalIp;
        private System.Windows.Forms.Panel separator;
        private MEME_Academic_Sample.UI.RoundedButton bt_Cancel;
        private MEME_Academic_Sample.UI.RoundedButton bt_Apply;
    }
}
