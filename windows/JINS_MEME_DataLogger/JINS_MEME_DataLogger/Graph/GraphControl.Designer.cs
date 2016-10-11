namespace JINS_MEME_DataLogger
{
    partial class GraphControl
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

        #region コンポーネント デザイナーで生成されたコード

        /// <summary> 
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GraphControl));
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.graphTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.graphView1 = new JINS_MEME_DataLogger.GraphView();
            this.graphView2 = new JINS_MEME_DataLogger.GraphView();
            this.graphView3 = new JINS_MEME_DataLogger.GraphView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.settingButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.axisMasterMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.graphSettingMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripLabel4 = new System.Windows.Forms.ToolStripLabel();
            this.newDataOnlySpan = new System.Windows.Forms.ToolStripTextBox();
            this.autoScrollCheck = new JINS_MEME_DataLogger.ToolStripCheckBox();
            this.zoomInButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.zoomToText = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.zoomFromText = new System.Windows.Forms.ToolStripTextBox();
            this.elapsedLabel = new System.Windows.Forms.ToolStripLabel();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.graphTableLayout.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.graphTableLayout);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(823, 394);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(823, 419);
            this.toolStripContainer1.TabIndex = 2;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
            // 
            // graphTableLayout
            // 
            this.graphTableLayout.ColumnCount = 1;
            this.graphTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.graphTableLayout.Controls.Add(this.graphView1, 0, 0);
            this.graphTableLayout.Controls.Add(this.graphView2, 0, 1);
            this.graphTableLayout.Controls.Add(this.graphView3, 0, 2);
            this.graphTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphTableLayout.Location = new System.Drawing.Point(0, 0);
            this.graphTableLayout.Name = "graphTableLayout";
            this.graphTableLayout.RowCount = 3;
            this.graphTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.graphTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.graphTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.graphTableLayout.Size = new System.Drawing.Size(823, 394);
            this.graphTableLayout.TabIndex = 0;
            // 
            // graphView1
            // 
            this.graphView1.DataEraseMode = false;
            this.graphView1.DataLabelFontSize = 9;
            this.graphView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphView1.Gradation = true;
            //this.graphView1.GraphBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(200)))));
            this.graphView1.GraphBackColor = System.Drawing.Color.Silver;
            this.graphView1.GraphOperation = true;
            this.graphView1.Location = new System.Drawing.Point(3, 3);
            this.graphView1.MouseZoomEnabled = true;
            this.graphView1.Name = "graphView1";
            this.graphView1.NewDataOnlyMode = false;
            this.graphView1.NewDataOnlySpan = 0.3D;
            this.graphView1.ShowHorizontalScrollBar = true;
            this.graphView1.ShowVerticalScrollBar = false;
            this.graphView1.Size = new System.Drawing.Size(817, 125);
            this.graphView1.TabIndex = 0;
            this.graphView1.Title = null;
            this.graphView1.TitleColor = System.Drawing.Color.DarkGray;
            this.graphView1.TitleFontSize = 22;
            this.graphView1.XAxisScaleFontSize = 22;
            this.graphView1.XAxisTitleFontSize = 22;
            this.graphView1.YAxisScaleFontSize = 22;
            this.graphView1.YAxisTitleFontSize = 22;
            this.graphView1.ZoomFraction = 0.1D;
            // 
            // graphView2
            // 
            this.graphView2.DataEraseMode = false;
            this.graphView2.DataLabelFontSize = 9;
            this.graphView2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphView2.Gradation = true;
            //this.graphView2.GraphBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(200)))));
            this.graphView2.GraphBackColor = System.Drawing.Color.Silver;
            this.graphView2.GraphOperation = true;
            this.graphView2.Location = new System.Drawing.Point(3, 134);
            this.graphView2.MouseZoomEnabled = true;
            this.graphView2.Name = "graphView2";
            this.graphView2.NewDataOnlyMode = false;
            this.graphView2.NewDataOnlySpan = 0.3D;
            this.graphView2.ShowHorizontalScrollBar = true;
            this.graphView2.ShowVerticalScrollBar = false;
            this.graphView2.Size = new System.Drawing.Size(817, 125);
            this.graphView2.TabIndex = 1;
            this.graphView2.Title = null;
            this.graphView2.TitleColor = System.Drawing.Color.DarkGray;
            this.graphView2.TitleFontSize = 22;
            this.graphView2.XAxisScaleFontSize = 22;
            this.graphView2.XAxisTitleFontSize = 22;
            this.graphView2.YAxisScaleFontSize = 22;
            this.graphView2.YAxisTitleFontSize = 22;
            this.graphView2.ZoomFraction = 0.1D;
            // 
            // graphView3
            // 
            this.graphView3.DataEraseMode = false;
            this.graphView3.DataLabelFontSize = 9;
            this.graphView3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphView3.Gradation = true;
            //this.graphView3.GraphBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(200)))));
            this.graphView3.GraphBackColor = System.Drawing.Color.Silver;
            this.graphView3.GraphOperation = true;
            this.graphView3.Location = new System.Drawing.Point(3, 265);
            this.graphView3.MouseZoomEnabled = true;
            this.graphView3.Name = "graphView3";
            this.graphView3.NewDataOnlyMode = false;
            this.graphView3.NewDataOnlySpan = 0.3D;
            this.graphView3.ShowHorizontalScrollBar = true;
            this.graphView3.ShowVerticalScrollBar = false;
            this.graphView3.Size = new System.Drawing.Size(817, 126);
            this.graphView3.TabIndex = 2;
            this.graphView3.Title = null;
            this.graphView3.TitleColor = System.Drawing.Color.DarkGray;
            this.graphView3.TitleFontSize = 22;
            this.graphView3.XAxisScaleFontSize = 22;
            this.graphView3.XAxisTitleFontSize = 22;
            this.graphView3.YAxisScaleFontSize = 22;
            this.graphView3.YAxisTitleFontSize = 22;
            this.graphView3.ZoomFraction = 0.1D;
            // 
            // toolStrip1
            // 
            this.toolStrip1.AutoSize = false;
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingButton,
            this.toolStripLabel4,
            this.newDataOnlySpan,
            this.autoScrollCheck,
            this.zoomInButton,
            this.toolStripLabel1,
            this.zoomToText,
            this.toolStripLabel2,
            this.zoomFromText,
            this.elapsedLabel});
            this.toolStrip1.Location = new System.Drawing.Point(31, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(753, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Visible = false;
            // 
            // settingButton
            // 
            this.settingButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.settingButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.axisMasterMenu,
            this.graphSettingMenu});
            this.settingButton.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.settingButton.Image = ((System.Drawing.Image)(resources.GetObject("settingButton.Image")));
            this.settingButton.ImageTransparentColor = System.Drawing.Color.White;
            this.settingButton.Name = "settingButton";
            this.settingButton.Size = new System.Drawing.Size(77, 22);
            this.settingButton.Text = "settings";
            // 
            // axisMasterMenu
            // 
            this.axisMasterMenu.Image = ((System.Drawing.Image)(resources.GetObject("axisMasterMenu.Image")));
            this.axisMasterMenu.Name = "axisMasterMenu";
            this.axisMasterMenu.Size = new System.Drawing.Size(132, 22);
            this.axisMasterMenu.Text = "axis master";
            this.axisMasterMenu.Click += new System.EventHandler(this.axisMasterMenu_Click);
            // 
            // graphSettingMenu
            // 
            this.graphSettingMenu.Image = ((System.Drawing.Image)(resources.GetObject("graphSettingMenu.Image")));
            this.graphSettingMenu.Name = "graphSettingMenu";
            this.graphSettingMenu.Size = new System.Drawing.Size(132, 22);
            this.graphSettingMenu.Text = "graph";
            this.graphSettingMenu.Click += new System.EventHandler(this.graphSettingButton_Click);
            // 
            // toolStripLabel4
            // 
            this.toolStripLabel4.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel4.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripLabel4.Margin = new System.Windows.Forms.Padding(0, 1, 3, 2);
            this.toolStripLabel4.Name = "toolStripLabel4";
            this.toolStripLabel4.Size = new System.Drawing.Size(24, 22);
            this.toolStripLabel4.Text = "Seconds";
            // 
            // newDataOnlySpan
            // 
            this.newDataOnlySpan.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.newDataOnlySpan.AutoSize = false;
            this.newDataOnlySpan.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.newDataOnlySpan.MaxLength = 9;
            this.newDataOnlySpan.Name = "newDataOnlySpan";
            this.newDataOnlySpan.Size = new System.Drawing.Size(70, 23);
            this.newDataOnlySpan.Text = "1234567.0";
            this.newDataOnlySpan.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // autoScrollCheck
            // 
            this.autoScrollCheck.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.autoScrollCheck.Checked = false;
            this.autoScrollCheck.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.autoScrollCheck.Name = "autoScrollCheck";
            this.autoScrollCheck.Size = new System.Drawing.Size(81, 22);
            this.autoScrollCheck.Text = "auto scroll";
            this.autoScrollCheck.CheckChanged += new System.EventHandler(this.newDataModeCheck_CheckChanged);
            // 
            // zoomInButton
            // 
            this.zoomInButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.zoomInButton.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.zoomInButton.Image = ((System.Drawing.Image)(resources.GetObject("zoomInButton.Image")));
            this.zoomInButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.zoomInButton.Name = "zoomInButton";
            this.zoomInButton.Size = new System.Drawing.Size(57, 22);
            this.zoomInButton.Text = "zoom";
            this.zoomInButton.Click += new System.EventHandler(this.zoomInButton_Click);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripLabel1.Margin = new System.Windows.Forms.Padding(0, 1, 3, 2);
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(24, 22);
            this.toolStripLabel1.Text = "Seconds";
            // 
            // zoomToText
            // 
            this.zoomToText.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.zoomToText.AutoSize = false;
            this.zoomToText.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.zoomToText.MaxLength = 9;
            this.zoomToText.Name = "zoomToText";
            this.zoomToText.Size = new System.Drawing.Size(70, 23);
            this.zoomToText.Text = "1234567.0";
            this.zoomToText.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.zoomToText.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.zoomRange_KeyPress);
            this.zoomToText.Validating += new System.ComponentModel.CancelEventHandler(this.zoomRange_Validating);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel2.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(19, 22);
            this.toolStripLabel2.Text = "～";
            // 
            // zoomFromText
            // 
            this.zoomFromText.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.zoomFromText.AutoSize = false;
            this.zoomFromText.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.zoomFromText.MaxLength = 9;
            this.zoomFromText.Name = "zoomFromText";
            this.zoomFromText.Size = new System.Drawing.Size(70, 23);
            this.zoomFromText.Text = "1234567.0";
            this.zoomFromText.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.zoomFromText.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.zoomRange_KeyPress);
            this.zoomFromText.Validating += new System.ComponentModel.CancelEventHandler(this.zoomRange_Validating);
            // 
            // elapsedLabel
            // 
            this.elapsedLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.elapsedLabel.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.elapsedLabel.Margin = new System.Windows.Forms.Padding(0, 1, 3, 2);
            this.elapsedLabel.Name = "elapsedLabel";
            this.elapsedLabel.Size = new System.Drawing.Size(49, 22);
            this.elapsedLabel.Text = "00:00:00";
            // 
            // GraphControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.toolStripContainer1);
            this.Name = "GraphControl";
            this.Size = new System.Drawing.Size(823, 419);
            this.Load += new System.EventHandler(this.GraphControl_Load);
            this.Resize += new System.EventHandler(this.GraphControl_Resize);
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.graphTableLayout.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.TableLayoutPanel graphTableLayout;
        private GraphView graphView1;
        private GraphView graphView2;
        private GraphView graphView3;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripTextBox zoomFromText;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripTextBox zoomToText;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton zoomInButton;
        private System.Windows.Forms.ToolStripTextBox newDataOnlySpan;
        private System.Windows.Forms.ToolStripLabel toolStripLabel4;
        private System.Windows.Forms.ToolStripDropDownButton settingButton;
        private ToolStripCheckBox autoScrollCheck;
        private System.Windows.Forms.ToolStripLabel elapsedLabel;
        private System.Windows.Forms.ToolStripMenuItem axisMasterMenu;
        private System.Windows.Forms.ToolStripMenuItem graphSettingMenu;
    }
}
