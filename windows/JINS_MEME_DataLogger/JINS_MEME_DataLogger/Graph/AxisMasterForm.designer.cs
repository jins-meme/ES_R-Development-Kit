namespace JINS_MEME_DataLogger
{
    partial class AxisMasterForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AxisMasterForm));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.dgvAxis = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.saveButton = new System.Windows.Forms.ToolStripButton();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.color = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AxisMin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AxisMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.unitName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.adRangeMin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.adRangeMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridLineVisible = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.gridResolution = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.axisBeanBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAxis)).BeginInit();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axisBeanBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            this.toolStripContainer1.BottomToolStripPanelVisible = false;
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.dgvAxis);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(473, 85);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.LeftToolStripPanelVisible = false;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.RightToolStripPanelVisible = false;
            this.toolStripContainer1.Size = new System.Drawing.Size(473, 110);
            this.toolStripContainer1.TabIndex = 0;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
            // 
            // dgvAxis
            // 
            this.dgvAxis.AllowUserToAddRows = false;
            this.dgvAxis.AllowUserToResizeColumns = false;
            this.dgvAxis.AllowUserToResizeRows = false;
            this.dgvAxis.AutoGenerateColumns = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvAxis.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvAxis.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAxis.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.name,
            this.color,
            this.unitName,
            this.adRangeMin,
            this.adRangeMax,
            this.AxisMin,
            this.AxisMax,
            this.gridLineVisible,
            this.gridResolution});
            this.dgvAxis.DataSource = this.axisBeanBindingSource;
            this.dgvAxis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAxis.Location = new System.Drawing.Point(0, 0);
            this.dgvAxis.MultiSelect = false;
            this.dgvAxis.Name = "dgvAxis";
            this.dgvAxis.RowTemplate.Height = 21;
            this.dgvAxis.Size = new System.Drawing.Size(473, 85);
            this.dgvAxis.TabIndex = 0;
            this.dgvAxis.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvAxis_CellClick);
            this.dgvAxis.CellEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvAxis_CellEnter);
            this.dgvAxis.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvAxis_CellFormatting);
            this.dgvAxis.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.dgvAxis_CellValidating);
            this.dgvAxis.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvAxis_CellValueChanged);
            this.dgvAxis.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgvAxis_CurrentCellDirtyStateChanged);
            this.dgvAxis.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dgvAxis_DataError);
            this.dgvAxis.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.dgvAxis_EditingControlShowing);
            this.dgvAxis.MouseLeave += new System.EventHandler(this.dgvAxis_MouseLeave);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveButton});
            this.toolStrip1.Location = new System.Drawing.Point(3, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(67, 25);
            this.toolStrip1.TabIndex = 0;
            // 
            // saveButton
            // 
            this.saveButton.Image = ((System.Drawing.Image)(resources.GetObject("saveButton.Image")));
            this.saveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(55, 22);
            this.saveButton.Text = "save";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // color
            // 
            this.color.HeaderText = "Color";
            this.color.MaxInputLength = 2;
            this.color.Name = "color";
            this.color.ReadOnly = true;
            this.color.Width = 50;
            // 
            // AxisMin
            // 
            this.AxisMin.DataPropertyName = "AxisMin";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.AxisMin.DefaultCellStyle = dataGridViewCellStyle2;
            this.AxisMin.HeaderText = "AxisMin";
            this.AxisMin.Name = "AxisMin";
            this.AxisMin.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // AxisMax
            // 
            this.AxisMax.DataPropertyName = "AxisMax";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.AxisMax.DefaultCellStyle = dataGridViewCellStyle3;
            this.AxisMax.HeaderText = "AxisMax";
            this.AxisMax.Name = "AxisMax";
            this.AxisMax.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // name
            // 
            this.name.DataPropertyName = "Name";
            this.name.HeaderText = "Name";
            this.name.MaxInputLength = 64;
            this.name.Name = "name";
            this.name.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.name.Width = 120;
            // 
            // unitName
            // 
            this.unitName.DataPropertyName = "UnitName";
            this.unitName.HeaderText = "Unit";
            this.unitName.MaxInputLength = 64;
            this.unitName.Name = "unitName";
            this.unitName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.unitName.Visible = false;
            // 
            // adRangeMin
            // 
            this.adRangeMin.DataPropertyName = "AdRangeMin";
            this.adRangeMin.HeaderText = "AdRangeMin";
            this.adRangeMin.MaxInputLength = 10;
            this.adRangeMin.Name = "adRangeMin";
            this.adRangeMin.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.adRangeMin.Visible = false;
            // 
            // adRangeMax
            // 
            this.adRangeMax.DataPropertyName = "AdRangeMax";
            this.adRangeMax.HeaderText = "AdRangeMax";
            this.adRangeMax.MaxInputLength = 10;
            this.adRangeMax.Name = "adRangeMax";
            this.adRangeMax.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.adRangeMax.Visible = false;
            // 
            // gridLineVisible
            // 
            this.gridLineVisible.DataPropertyName = "GridLineVisible";
            this.gridLineVisible.HeaderText = "Grid";
            this.gridLineVisible.Name = "gridLineVisible";
            this.gridLineVisible.Width = 60;
            // 
            // gridResolution
            // 
            this.gridResolution.DataPropertyName = "GridResolution";
            this.gridResolution.HeaderText = "GridWidth";
            this.gridResolution.MaxInputLength = 10;
            this.gridResolution.Name = "gridResolution";
            this.gridResolution.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.gridResolution.Visible = false;
            // 
            // axisBeanBindingSource
            // 
            this.axisBeanBindingSource.DataSource = typeof(JINS_MEME_DataLogger.AxisBean);
            // 
            // AxisMasterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(473, 110);
            this.Controls.Add(this.toolStripContainer1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "AxisMasterForm";
            this.Text = "Axis master";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AxisMasterForm_FormClosing);
            this.Load += new System.EventHandler(this.AxisMasterForm_Load);
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAxis)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axisBeanBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.DataGridView dgvAxis;
        private System.Windows.Forms.BindingSource axisBeanBindingSource;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton saveButton;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.DataGridViewTextBoxColumn name;
        private System.Windows.Forms.DataGridViewTextBoxColumn color;
        private System.Windows.Forms.DataGridViewTextBoxColumn unitName;
        private System.Windows.Forms.DataGridViewTextBoxColumn adRangeMin;
        private System.Windows.Forms.DataGridViewTextBoxColumn adRangeMax;
        private System.Windows.Forms.DataGridViewTextBoxColumn AxisMin;
        private System.Windows.Forms.DataGridViewTextBoxColumn AxisMax;
        private System.Windows.Forms.DataGridViewCheckBoxColumn gridLineVisible;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridResolution;
    }
}