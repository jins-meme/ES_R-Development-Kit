namespace JINS_MEME_DataLogger.Graph
{
    partial class ItemControl
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgvItem = new System.Windows.Forms.DataGridView();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.itemBeanBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.visibleCheck = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.lineWidthCombo = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Color = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.YAxisMin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.YAxisMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItem)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemBeanBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvItem
            // 
            this.dgvItem.AllowUserToAddRows = false;
            this.dgvItem.AllowUserToDeleteRows = false;
            this.dgvItem.AutoGenerateColumns = false;
            this.dgvItem.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dgvItem.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvItem.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvItem.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvItem.ColumnHeadersVisible = false;
            this.dgvItem.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.visibleCheck,
            this.lineWidthCombo,
            this.Color,
            this.YAxisMin,
            this.YAxisMax,
            this.nameDataGridViewTextBoxColumn});
            this.dgvItem.DataSource = this.itemBeanBindingSource;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvItem.DefaultCellStyle = dataGridViewCellStyle4;
            this.dgvItem.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvItem.Location = new System.Drawing.Point(0, 0);
            this.dgvItem.MultiSelect = false;
            this.dgvItem.Name = "dgvItem";
            this.dgvItem.RowHeadersVisible = false;
            this.dgvItem.RowHeadersWidth = 20;
            this.dgvItem.RowTemplate.Height = 19;
            this.dgvItem.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvItem.Size = new System.Drawing.Size(192, 331);
            this.dgvItem.TabIndex = 3;
            this.dgvItem.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvItem_CellClick);
            this.dgvItem.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvItem_CellEndEdit);
            this.dgvItem.CellEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvItem_CellEnter);
            this.dgvItem.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvItem_CellFormatting);
            this.dgvItem.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.dgvItem_CellValidating);
            this.dgvItem.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvItem_CellValueChanged);
            this.dgvItem.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgvItem_CurrentCellDirtyStateChanged);
            this.dgvItem.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.dgvItem_EditingControlShowing);
            // 
            // itemBeanBindingSource
            // 
            this.itemBeanBindingSource.DataSource = typeof(JINS_MEME_DataLogger.ItemBean);
            // 
            // visibleCheck
            // 
            this.visibleCheck.DataPropertyName = "Visible";
            this.visibleCheck.HeaderText = "表示";
            this.visibleCheck.Name = "visibleCheck";
            this.visibleCheck.Width = 40;
            // 
            // lineWidthCombo
            // 
            this.lineWidthCombo.HeaderText = "幅";
            this.lineWidthCombo.Items.AddRange(new object[] {
            "1.0",
            "2.0",
            "3.0",
            "4.0",
            "5.0"});
            this.lineWidthCombo.Name = "lineWidthCombo";
            this.lineWidthCombo.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.lineWidthCombo.Visible = false;
            this.lineWidthCombo.Width = 40;
            // 
            // Color
            // 
            this.Color.HeaderText = "色";
            this.Color.MinimumWidth = 20;
            this.Color.Name = "Color";
            this.Color.ReadOnly = true;
            this.Color.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Color.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Color.Width = 20;
            // 
            // YAxisMin
            // 
            this.YAxisMin.DataPropertyName = "YAxisMin";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.YAxisMin.DefaultCellStyle = dataGridViewCellStyle2;
            this.YAxisMin.HeaderText = "YMin";
            this.YAxisMin.Name = "YAxisMin";
            this.YAxisMin.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.YAxisMin.Visible = false;
            this.YAxisMin.Width = 50;
            // 
            // YAxisMax
            // 
            this.YAxisMax.DataPropertyName = "YAxisMax";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.YAxisMax.DefaultCellStyle = dataGridViewCellStyle3;
            this.YAxisMax.HeaderText = "YMax";
            this.YAxisMax.Name = "YAxisMax";
            this.YAxisMax.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.YAxisMax.Visible = false;
            this.YAxisMax.Width = 50;
            // 
            // nameDataGridViewTextBoxColumn
            // 
            this.nameDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.nameDataGridViewTextBoxColumn.DataPropertyName = "Name";
            this.nameDataGridViewTextBoxColumn.HeaderText = "名称";
            this.nameDataGridViewTextBoxColumn.MinimumWidth = 30;
            this.nameDataGridViewTextBoxColumn.Name = "nameDataGridViewTextBoxColumn";
            this.nameDataGridViewTextBoxColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // ItemControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvItem);
            this.Name = "ItemControl";
            this.Size = new System.Drawing.Size(192, 331);
            ((System.ComponentModel.ISupportInitialize)(this.dgvItem)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemBeanBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvItem;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.BindingSource itemBeanBindingSource;
        private System.Windows.Forms.DataGridViewCheckBoxColumn visibleCheck;
        private System.Windows.Forms.DataGridViewComboBoxColumn lineWidthCombo;
        private System.Windows.Forms.DataGridViewTextBoxColumn Color;
        private System.Windows.Forms.DataGridViewTextBoxColumn YAxisMin;
        private System.Windows.Forms.DataGridViewTextBoxColumn YAxisMax;
        private System.Windows.Forms.DataGridViewTextBoxColumn nameDataGridViewTextBoxColumn;
    }
}
