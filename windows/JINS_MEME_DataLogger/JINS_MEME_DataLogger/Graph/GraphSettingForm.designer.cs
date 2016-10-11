namespace JINS_MEME_DataLogger
{
    partial class GraphSettingForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GraphSettingForm));
            this.label1 = new System.Windows.Forms.Label();
            this.backColorLabel = new System.Windows.Forms.Label();
            this.gradationCheck = new System.Windows.Forms.CheckBox();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.label3 = new System.Windows.Forms.Label();
            this.fontDialog = new System.Windows.Forms.FontDialog();
            this.label6 = new System.Windows.Forms.Label();
            this.applyButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.titleColorLabel = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lineWidthCombo = new JINS_MEME_DataLogger.ComboBoxEx();
            this.titleFontSizeCombo = new JINS_MEME_DataLogger.ComboBoxEx();
            this.axisFontSizeCombo = new JINS_MEME_DataLogger.ComboBoxEx();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "BackColor";
            // 
            // backColorLabel
            // 
            this.backColorLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.backColorLabel.Location = new System.Drawing.Point(75, 10);
            this.backColorLabel.Name = "backColorLabel";
            this.backColorLabel.Size = new System.Drawing.Size(87, 18);
            this.backColorLabel.TabIndex = 1;
            this.backColorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.backColorLabel.Click += new System.EventHandler(this.backColorLabel_Click);
            // 
            // gradationCheck
            // 
            this.gradationCheck.AutoSize = true;
            this.gradationCheck.Location = new System.Drawing.Point(181, 12);
            this.gradationCheck.Name = "gradationCheck";
            this.gradationCheck.Size = new System.Drawing.Size(73, 16);
            this.gradationCheck.TabIndex = 3;
            this.gradationCheck.Text = "Gradation";
            this.gradationCheck.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 45);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 12);
            this.label3.TabIndex = 4;
            this.label3.Text = "Axis font Size";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 71);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(76, 12);
            this.label6.TabIndex = 8;
            this.label6.Text = "Title font size";
            // 
            // applyButton
            // 
            this.applyButton.Location = new System.Drawing.Point(12, 159);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 23);
            this.applyButton.TabIndex = 14;
            this.applyButton.Text = "apply";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(181, 159);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 15;
            this.cancelButton.Text = "cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // titleColorLabel
            // 
            this.titleColorLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.titleColorLabel.Location = new System.Drawing.Point(75, 101);
            this.titleColorLabel.Name = "titleColorLabel";
            this.titleColorLabel.Size = new System.Drawing.Size(87, 18);
            this.titleColorLabel.TabIndex = 17;
            this.titleColorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.titleColorLabel.Click += new System.EventHandler(this.titleColorLabel_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 104);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 12);
            this.label4.TabIndex = 16;
            this.label4.Text = "Title Color";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 131);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 12);
            this.label2.TabIndex = 18;
            this.label2.Text = "Line Width";
            // 
            // lineWidthCombo
            // 
            this.lineWidthCombo.FormattingEnabled = true;
            this.lineWidthCombo.Location = new System.Drawing.Point(144, 128);
            this.lineWidthCombo.Name = "lineWidthCombo";
            this.lineWidthCombo.Size = new System.Drawing.Size(86, 20);
            this.lineWidthCombo.TabIndex = 19;
            // 
            // titleFontSizeCombo
            // 
            this.titleFontSizeCombo.FormattingEnabled = true;
            this.titleFontSizeCombo.Location = new System.Drawing.Point(144, 68);
            this.titleFontSizeCombo.Name = "titleFontSizeCombo";
            this.titleFontSizeCombo.Size = new System.Drawing.Size(86, 20);
            this.titleFontSizeCombo.TabIndex = 13;
            // 
            // axisFontSizeCombo
            // 
            this.axisFontSizeCombo.FormattingEnabled = true;
            this.axisFontSizeCombo.Location = new System.Drawing.Point(144, 42);
            this.axisFontSizeCombo.Name = "axisFontSizeCombo";
            this.axisFontSizeCombo.Size = new System.Drawing.Size(86, 20);
            this.axisFontSizeCombo.TabIndex = 9;
            // 
            // GraphSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(268, 194);
            this.Controls.Add(this.lineWidthCombo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.titleColorLabel);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.titleFontSizeCombo);
            this.Controls.Add(this.axisFontSizeCombo);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.gradationCheck);
            this.Controls.Add(this.backColorLabel);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GraphSettingForm";
            this.Text = "Graph settings";
            this.Load += new System.EventHandler(this.GraphSettingForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label backColorLabel;
        private System.Windows.Forms.CheckBox gradationCheck;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.FontDialog fontDialog;
        private System.Windows.Forms.Label label6;
        private ComboBoxEx axisFontSizeCombo;
        private ComboBoxEx titleFontSizeCombo;
        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label titleColorLabel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private ComboBoxEx lineWidthCombo;
    }
}