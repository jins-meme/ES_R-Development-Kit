namespace JINS_MEME_DataLogger
{
    partial class sensPostureSetting
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
            this.trackBarAngleH = new System.Windows.Forms.TrackBar();
            this.trackBarAngleV = new System.Windows.Forms.TrackBar();
            this.trackBarDistance = new System.Windows.Forms.TrackBar();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnReg = new System.Windows.Forms.ToolStripButton();
            this.btnClose = new System.Windows.Forms.ToolStripButton();
            this.label6 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblAngleH = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lblAngleV = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.lblDistance = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarAngleH)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarAngleV)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarDistance)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // trackBarAngleH
            // 
            this.trackBarAngleH.AutoSize = false;
            this.trackBarAngleH.BackColor = System.Drawing.SystemColors.Control;
            this.trackBarAngleH.Location = new System.Drawing.Point(45, 1);
            this.trackBarAngleH.Maximum = 359;
            this.trackBarAngleH.Name = "trackBarAngleH";
            this.trackBarAngleH.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.trackBarAngleH.Size = new System.Drawing.Size(141, 20);
            this.trackBarAngleH.TabIndex = 10;
            this.trackBarAngleH.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBarAngleH.Scroll += new System.EventHandler(this.trackBarAngleH_Scroll);
            // 
            // trackBarAngleV
            // 
            this.trackBarAngleV.AutoSize = false;
            this.trackBarAngleV.BackColor = System.Drawing.SystemColors.Control;
            this.trackBarAngleV.Location = new System.Drawing.Point(45, -1);
            this.trackBarAngleV.Maximum = 90;
            this.trackBarAngleV.Minimum = -90;
            this.trackBarAngleV.Name = "trackBarAngleV";
            this.trackBarAngleV.Size = new System.Drawing.Size(141, 20);
            this.trackBarAngleV.TabIndex = 12;
            this.trackBarAngleV.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBarAngleV.Scroll += new System.EventHandler(this.trackBarAngleV_Scroll);
            // 
            // trackBarDistance
            // 
            this.trackBarDistance.AutoSize = false;
            this.trackBarDistance.BackColor = System.Drawing.SystemColors.Control;
            this.trackBarDistance.Location = new System.Drawing.Point(45, -1);
            this.trackBarDistance.Maximum = 1000;
            this.trackBarDistance.Name = "trackBarDistance";
            this.trackBarDistance.Size = new System.Drawing.Size(141, 20);
            this.trackBarDistance.TabIndex = 14;
            this.trackBarDistance.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBarDistance.Scroll += new System.EventHandler(this.trackBarDistance_Scroll);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnReg,
            this.btnClose});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(312, 28);
            this.toolStrip1.TabIndex = 41;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnReg
            // 
            this.btnReg.AutoSize = false;
            //this.btnReg.BackgroundImage = global::Tracer.Properties.Resources.ストリップボタン背景;
            this.btnReg.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnReg.Font = new System.Drawing.Font("メイリオ", 10F, System.Drawing.FontStyle.Bold);
            //this.btnReg.Image = global::Tracer.Properties.Resources.Save;
            this.btnReg.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnReg.Name = "btnReg";
            this.btnReg.Size = new System.Drawing.Size(80, 25);
            this.btnReg.Text = "設定";
            this.btnReg.Click += new System.EventHandler(this.btnReg_Click);
            // 
            // btnClose
            // 
            this.btnClose.AutoSize = false;
            //this.btnClose.BackgroundImage = global::Tracer.Properties.Resources.ストリップボタン背景;
            this.btnClose.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnClose.Font = new System.Drawing.Font("メイリオ", 10F, System.Drawing.FontStyle.Bold);
            //this.btnClose.Image = global::Tracer.Properties.Resources.Cancel;
            this.btnClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 25);
            this.btnClose.Text = "キャンセル";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.SystemColors.Highlight;
            this.label6.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label6.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.label6.Location = new System.Drawing.Point(0, 28);
            this.label6.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(125, 23);
            this.label6.TabIndex = 42;
            this.label6.Text = "方位角";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.SystemColors.Highlight;
            this.label4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label4.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.label4.Location = new System.Drawing.Point(0, 51);
            this.label4.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(125, 23);
            this.label4.TabIndex = 43;
            this.label4.Text = "仰角";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.SystemColors.Highlight;
            this.label5.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label5.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.label5.Location = new System.Drawing.Point(0, 74);
            this.label5.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(125, 23);
            this.label5.TabIndex = 44;
            this.label5.Text = "中心点からの距離";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.lblAngleH);
            this.panel1.Controls.Add(this.trackBarAngleH);
            this.panel1.Location = new System.Drawing.Point(121, 28);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(188, 21);
            this.panel1.TabIndex = 45;
            // 
            // lblAngleH
            // 
            this.lblAngleH.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblAngleH.Location = new System.Drawing.Point(-2, 1);
            this.lblAngleH.Name = "lblAngleH";
            this.lblAngleH.Size = new System.Drawing.Size(41, 20);
            this.lblAngleH.TabIndex = 0;
            this.lblAngleH.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Controls.Add(this.lblAngleV);
            this.panel2.Controls.Add(this.trackBarAngleV);
            this.panel2.Location = new System.Drawing.Point(121, 51);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(188, 21);
            this.panel2.TabIndex = 46;
            // 
            // lblAngleV
            // 
            this.lblAngleV.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblAngleV.Location = new System.Drawing.Point(-2, 1);
            this.lblAngleV.Name = "lblAngleV";
            this.lblAngleV.Size = new System.Drawing.Size(41, 20);
            this.lblAngleV.TabIndex = 48;
            this.lblAngleV.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel3.Controls.Add(this.lblDistance);
            this.panel3.Controls.Add(this.trackBarDistance);
            this.panel3.Location = new System.Drawing.Point(121, 74);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(188, 21);
            this.panel3.TabIndex = 47;
            // 
            // lblDistance
            // 
            this.lblDistance.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblDistance.Location = new System.Drawing.Point(-2, -1);
            this.lblDistance.Name = "lblDistance";
            this.lblDistance.Size = new System.Drawing.Size(41, 20);
            this.lblDistance.TabIndex = 49;
            this.lblDistance.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // sensPostureSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(312, 96);
            this.ControlBox = false;
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.toolStrip1);
            this.Font = new System.Drawing.Font("ＭＳ ゴシック", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "sensPostureSetting";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "センサー姿勢設定";
            ((System.ComponentModel.ISupportInitialize)(this.trackBarAngleH)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarAngleV)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarDistance)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TrackBar trackBarAngleH;
        private System.Windows.Forms.TrackBar trackBarAngleV;
        private System.Windows.Forms.TrackBar trackBarDistance;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnReg;
        private System.Windows.Forms.ToolStripButton btnClose;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label lblAngleH;
        private System.Windows.Forms.Label lblAngleV;
        private System.Windows.Forms.Label lblDistance;
    }
}