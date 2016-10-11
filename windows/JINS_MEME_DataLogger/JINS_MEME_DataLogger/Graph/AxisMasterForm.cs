using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 軸マスター編集画面
    /// ファイルはひとつ。
    /// ファイルない場合はデフォルト項目を表示
    /// </summary>
    public partial class AxisMasterForm : Form
    {
        /// <summary>
        /// bindingSource
        /// </summary>
        private BindingSource axisBindingSource = null;

        /// <summary>
        /// Clear On dataset / Set true on Edit Data
        /// </summary>
        public bool HasEdited { get; set; }

        /// <summary>
        /// 戻り値
        /// </summary>
        private DialogResult dialogResult = DialogResult.Cancel;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AxisMasterForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// LOADイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AxisMasterForm_Load(object sender, EventArgs e)
        {

            // データバインド
            axisBindingSource = new BindingSource(AxisMaster.AxisList.OrderBy(d => d.DispOrder), string.Empty);
            dgvAxis.DataSource = axisBindingSource;
            dgvAxis.ClearSelection();

            // 編集フラグクリア
            HasEdited = false;

            // 戻り値初期化
            this.dialogResult = DialogResult.Cancel;
        }



        /// <summary>
        /// 保存ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveButton_Click(object sender, EventArgs e)
        {
            // validateしてエラーならメッセージ表示
            string message = string.Empty;
            if (!validateAxisList(ref message))
            {
                MessageBox.Show(message, "Axis Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 保存
            AxisMaster.Save();

            // 編集フラグクリア
            HasEdited = false;


            //// 項目パターンファイルのAxisも更新する

            //// すべての項目パターンについて
            //for (int i = 0; i < ItemMaster.ItemPatternList.Count; i++)
            //{
            //    ItemMasterBean pattern = ItemMaster.ItemPatternList[i];
            //    for (int j = 0; j < pattern.ItemList.Count; j++)
            //    {
            //        // 項目に紐づいている軸を入れ替える
            //        AxisBean axis = AxisMaster.AxisList.Find(d => d.Id == pattern.ItemList[j].Axis.Id);
            //        if (axis != null)
            //        {
            //            pattern.ItemList[j].Axis = axis;
            //        }
            //    }
            //}
            //// 保存
            //ItemMaster.Save();


            MessageBox.Show("Save completed.", "Axis Master", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.dialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// 上へ移動ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void upButton_Click(object sender, EventArgs e)
        {
            if (dgvAxis.SelectedCells.Count <= 0)
            {
                return;
            }

            int rowIndex = dgvAxis.SelectedCells[0].RowIndex;
            if (rowIndex < 1)
            {
                return;
            }

            // Selected Row Data
            DataGridViewRow row = dgvAxis.Rows[rowIndex];
            AxisBean axis = row.DataBoundItem as AxisBean;

            // Selected Row - 1 Data
            DataGridViewRow row1 = dgvAxis.Rows[rowIndex - 1];
            AxisBean axis1 = row1.DataBoundItem as AxisBean;

            if (axis != null && axis1 != null)
            {
                // 表示順をスワップ
                int order = axis.DispOrder;
                int order1 = axis1.DispOrder;

                axis.DispOrder = order1;
                axis1.DispOrder = order;
            }

            // ReDisplay
            axisBindingSource = new BindingSource(AxisMaster.AxisList.OrderBy(d => d.DispOrder), string.Empty);
            dgvAxis.DataSource = axisBindingSource;
            dgvAxis.Rows[rowIndex - 1].Selected = true;

            // 編集フラグON
            HasEdited = true;

        }

        /// <summary>
        /// saveを促す
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AxisMasterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (HasEdited)
            {
                if (DialogResult.Yes == MessageBox.Show("Data has edited. save?", "Axis Master", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                {
                    AxisMaster.Save();
                    DialogResult = DialogResult.OK;
                }
                else
                {
                    // キャンセル処理を追加
                    e.Cancel = true;
                    //DialogResult = DialogResult.Cancel;
                }
            }
            else
            {
                DialogResult = this.dialogResult;
            }
        }



        /// <summary>
        /// 下へ移動ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void downButton_Click(object sender, EventArgs e)
        {
            if (dgvAxis.SelectedCells.Count <= 0)
            {
                return;
            }

            int rowIndex = dgvAxis.SelectedCells[0].RowIndex;
            if (rowIndex >= dgvAxis.Rows.Count - 1)
            {
                return;
            }

            // Selected Row Data
            DataGridViewRow row = dgvAxis.Rows[rowIndex];
            AxisBean axis = row.DataBoundItem as AxisBean;

            // Selected Row - 1 Data
            DataGridViewRow row1 = dgvAxis.Rows[rowIndex + 1];
            AxisBean axis1 = row1.DataBoundItem as AxisBean;

            if (axis != null && axis1 != null)
            {
                // 表示順をスワップ
                int order = axis.DispOrder;
                int order1 = axis1.DispOrder;

                axis.DispOrder = order1;
                axis1.DispOrder = order;
            }

            // ReDisplay
            axisBindingSource = new BindingSource(AxisMaster.AxisList.OrderBy(d => d.DispOrder), string.Empty);
            dgvAxis.DataSource = axisBindingSource;
            dgvAxis.Rows[rowIndex + 1].Selected = true;

            // 編集フラグON
            HasEdited = true;
        }


        /// <summary>
        /// 表示フォーマット処理
        /// Colorの背景色変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvAxis_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvAxis.Columns[e.ColumnIndex].Name == "color")
            {
                AxisBean axis = (AxisBean)dgvAxis.Rows[e.RowIndex].DataBoundItem;

                e.CellStyle.BackColor = axis.AxisColor;
                e.CellStyle.SelectionBackColor = axis.AxisColor;
            }
        }

        /// <summary>
        /// CellClickハンドラ
        /// カラーダイアログを表示し、色設定する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvAxis_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex < 0)
            {
                return;
            }

            //"Color"列がクリックされた
            if (dgvAxis.Columns[e.ColumnIndex].Name == "color")
            {
                if (colorDialog.ShowDialog(this) == DialogResult.OK)
                {
                    AxisBean axis = (AxisBean)dgvAxis.Rows[e.RowIndex].DataBoundItem;
                    axis.AxisColor = colorDialog.Color;
                    dgvAxis.CommitEdit(DataGridViewDataErrorContexts.Commit);

                    // 編集フラグON
                    HasEdited = true;
                }
            }
        }


        /// <summary>
        ///　CellValueChangedイベントは、チェックボックスがチェックされた後に別のセルにフォーカスを移すなどして
        ///　値がコミットされた時にしか発生しないので、チェックされた後に強制的にコミットする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvAxis_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            string colName = dgvAxis.CurrentCell.OwningColumn.Name;

            if ((colName == "gridLineVisible" || colName == "IsY2Axis") && dgvAxis.IsCurrentCellDirty)
            {
                //コミットする
                dgvAxis.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// 入力制限をするため、
        /// 表示されているコントロールがDataGridViewTextBoxEditingControlなら
        /// KeyPressイベントを登録する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvAxis_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is DataGridViewTextBoxEditingControl)
            {
                DataGridView dgv = (DataGridView)sender;
                DataGridViewTextBoxEditingControl tb = (DataGridViewTextBoxEditingControl)e.Control;
                tb.ImeMode = ImeMode.On;
                tb.KeyPress -= new KeyPressEventHandler(dataGridViewTextBox_KeyPress);

                if (dgv.CurrentCell.OwningColumn.Name == "adRangeMin"
                    || dgv.CurrentCell.OwningColumn.Name == "adRangeMax"
                    || dgv.CurrentCell.OwningColumn.Name == "AxisMin"
                    || dgv.CurrentCell.OwningColumn.Name == "AxisMax"
                    || dgv.CurrentCell.OwningColumn.Name == "MovingAverageCount"
                    || dgv.CurrentCell.OwningColumn.Name == "gridResolution") // 数字入力制限の列を指定する 
                {
                    tb.ImeMode = ImeMode.Disable;
                    tb.KeyPress += new KeyPressEventHandler(dataGridViewTextBox_KeyPress);
                }
            }

        }

        /// <summary>
        /// DataGridViewに表示されているテキストボックスの
        /// KeyPressイベントハンドラ
        /// 入力制限を行う。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridViewTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 入力可能文字 
            if ((e.KeyChar < '0' || e.KeyChar > '9') && e.KeyChar != '\b' && e.KeyChar != '.' && e.KeyChar != '-')
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 選択されたコントロールがDataGridViewTextBoxCellなら
        /// ワンクリックで編集できるようにする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvAxis_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvAxis.Columns[e.ColumnIndex].CellType.Name == "DataGridViewTextBoxCell")
            {
                if (dgvAxis.Focused)
                {
                    dgvAxis.BeginEdit(true);
                }
            }
        }

        /// <summary>
        /// CellValidatingハンドラ
        /// 数値項目の形式チェック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvAxis_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            double dblValue;
            int intValue;


            if (dgvAxis.CurrentCell.OwningColumn.Name == "adRangeMin"
                || dgvAxis.CurrentCell.OwningColumn.Name == "adRangeMax"
                || dgvAxis.CurrentCell.OwningColumn.Name == "AxisMin"
                || dgvAxis.CurrentCell.OwningColumn.Name == "AxisMax"
                || dgvAxis.CurrentCell.OwningColumn.Name == "MovingAverageCount"
                || dgvAxis.CurrentCell.OwningColumn.Name == "gridResolution") // 数字列
            {
                string colName = dgvAxis.Columns[e.ColumnIndex].HeaderText;

                // null？
                if (string.IsNullOrEmpty(e.FormattedValue.ToString()))
                {
                    MessageBox.Show(string.Format("{0} must input.", colName), "Axis Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                    return;
                }
            }

            if (dgvAxis.CurrentCell.OwningColumn.Name == "adRangeMin"
                || dgvAxis.CurrentCell.OwningColumn.Name == "adRangeMax"
                || dgvAxis.CurrentCell.OwningColumn.Name == "AxisMin"
                || dgvAxis.CurrentCell.OwningColumn.Name == "AxisMax"
                || dgvAxis.CurrentCell.OwningColumn.Name == "gridResolution") // double数字列
            {
                string colName = dgvAxis.Columns[e.ColumnIndex].HeaderText;

                // doubleに変換可能か
                if (!double.TryParse(e.FormattedValue.ToString(), out dblValue))
                {
                    MessageBox.Show(string.Format("{0} illegal input value.[{1}]", colName, e.FormattedValue.ToString()), "Axis Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                }
            }
            else if (dgvAxis.CurrentCell.OwningColumn.Name == "MovingAverageCount") // int数字列
            {
                string colName = dgvAxis.Columns[e.ColumnIndex].HeaderText;

                // intに変換可能か
                if (!int.TryParse(e.FormattedValue.ToString(), out intValue))
                {
                    MessageBox.Show(string.Format("{0} illegal input value. [{1}]", colName, e.FormattedValue.ToString()), "Axis Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        /// MOUSE LEAVE時に編集確定させる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvAxis_MouseLeave(object sender, EventArgs e)
        {
            this.ValidateChildren();
            dgvAxis.EndEdit();
        }

        /// <summary>
        /// データエラー時の例外を抑制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvAxis_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        /// <summary>
        /// セルの値変化時に編集フラグON
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvAxis_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                HasEdited = true;
            }
        }

        private bool validateAxisList(ref string message)
        {
            List<AxisBean> axisList = AxisMaster.AxisList;
            // min,maxの大小
            if (axisList.Count(d => d.AxisMax <= d.AxisMin) > 0)
            {
                message = "YMin must be lesser than YMax.";
                return false;
            }

            // 名称重複
            var query1 = from axis in axisList group axis by axis.Name;
            if (query1.Count() < axisList.Count)
            {
                message = "name is duplicated";
                return false;
            }

            return true;
        }


    }
}
