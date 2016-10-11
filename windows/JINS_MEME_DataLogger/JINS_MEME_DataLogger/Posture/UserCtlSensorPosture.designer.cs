namespace JINS_MEME_DataLogger
{
    partial class UserCtlSensorPosture
    {
        /// <summary> 
        /// 必要なデザイナ変数です。
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

        #region コンポーネント デザイナで生成されたコード

        /// <summary> 
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.lblE = new System.Windows.Forms.Label();
            this.lblZ = new System.Windows.Forms.Label();
            this.lblN = new System.Windows.Forms.Label();
            this.lblDebugMon = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblE
            // 
            this.lblE.AutoSize = true;
            this.lblE.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lblE.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblE.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.lblE.Location = new System.Drawing.Point(49, 87);
            this.lblE.Name = "lblE";
            this.lblE.Size = new System.Drawing.Size(17, 16);
            this.lblE.TabIndex = 5;
            this.lblE.Text = "Y";
            // 
            // lblZ
            // 
            this.lblZ.AutoSize = true;
            this.lblZ.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lblZ.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblZ.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.lblZ.Location = new System.Drawing.Point(73, 48);
            this.lblZ.Name = "lblZ";
            this.lblZ.Size = new System.Drawing.Size(17, 16);
            this.lblZ.TabIndex = 4;
            this.lblZ.Text = "Z";
            // 
            // lblN
            // 
            this.lblN.AutoSize = true;
            this.lblN.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lblN.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblN.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.lblN.Location = new System.Drawing.Point(85, 87);
            this.lblN.Name = "lblN";
            this.lblN.Size = new System.Drawing.Size(18, 16);
            this.lblN.TabIndex = 3;
            this.lblN.Text = "X";
            // 
            // lblDebugMon
            // 
            this.lblDebugMon.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lblDebugMon.Font = new System.Drawing.Font("ＭＳ ゴシック", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblDebugMon.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.lblDebugMon.Location = new System.Drawing.Point(3, 0);
            this.lblDebugMon.Name = "lblDebugMon";
            this.lblDebugMon.Size = new System.Drawing.Size(144, 48);
            this.lblDebugMon.TabIndex = 6;
            this.lblDebugMon.Text = "角度:\r\nX:\r\ny:\r\nz:";
            // 
            // UserCtlSensorPosture
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblDebugMon);
            this.Controls.Add(this.lblE);
            this.Controls.Add(this.lblZ);
            this.Controls.Add(this.lblN);
            this.Name = "UserCtlSensorPosture";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.UserCtlSensorPosture_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UserCtlSensorPosture_MouseDown);
            this.Move += new System.EventHandler(this.UserCtlSensorPosture_Move);
            this.Resize += new System.EventHandler(this.UserCtlSensorPosture_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblE;
        private System.Windows.Forms.Label lblZ;
        private System.Windows.Forms.Label lblN;
        private System.Windows.Forms.Label lblDebugMon;
    }
}
