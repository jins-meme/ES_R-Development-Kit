using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JINS_MEME_DataLogger.Graph
{
    /// <summary>
    /// グラフ項目の処理を記載します。
    /// </summary>
    public partial class ItemControl : UserControl
    {
        /// <summary>
        /// bindingSource
        /// </summary>
        private BindingSource itemBindingSource = null;

        /// <summary>
        /// dataGridViewComboBox
        /// </summary>
        private DataGridViewComboBoxEditingControl dataGridViewComboBox = null;

        /// <summary>
        /// 項目リスト
        /// </summary>
        public List<ItemBean> ItemList { get; private set; }

        /// <summary>
        /// 項目色変化イベント
        /// </summary>
        public event Action<ItemBean> ItemColorChanged;

        /// <summary>
        /// 項目の表示・非表示変化イベント
        /// </summary>
        public event Action<ItemBean> ItemVisibleChanged;

        /// <summary>
        /// 項目の線幅変化イベント
        /// </summary>
        public event Action<ItemBean> ItemWidthChanged;

        /// <summary>
        /// 項目のY軸レンジ変化イベント
        /// </summary>
        public event Action<ItemBean> ItemRangeChanged;

        /// <summary>
        /// 項目の名称変化イベント
        /// </summary>
        public event Action<ItemBean> ItemNameChanged;

        /// <summary>
        /// 項目の現在値表示・非表示変化イベント
        /// </summary>
        public event Action<ItemBean> ItemCurrentValueVisibleChanged;

        public ItemControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 項目リスト設定
        /// </summary>
        /// <param name="itemList"></param>
        public void SetItems(List<ItemBean> itemList)
        {
            this.ItemList = itemList;

            if (ItemList != null)
            {
                itemBindingSource = new BindingSource(itemList, string.Empty);
                dgvItem.DataSource = itemBindingSource;
                dgvItem.ClearSelection();
            }
        }


        /// <summary>
        /// 表示フォーマット処理
        /// Colorの背景色変更とコンボの値設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvItem_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvItem.Columns[e.ColumnIndex].Name == "Color")
            {
                ItemBean item = (ItemBean)dgvItem.Rows[e.RowIndex].DataBoundItem;

                e.CellStyle.BackColor = item.LineColor;
                e.CellStyle.SelectionBackColor = item.LineColor;
            }

            if (dgvItem.Columns[e.ColumnIndex].Name == "lineWidthCombo")
            {
                ItemBean item = (ItemBean)dgvItem.Rows[e.RowIndex].DataBoundItem;

                e.Value = string.Format("{0:F1}", item.LineWidth);
            }
        }



        /// <summary>
        /// CellClickハンドラ
        /// カラーダイアログを表示し、色設定する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvItem_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //"Color"列がクリックされた
            if (dgvItem.Columns[e.ColumnIndex].Name == "Color")
            {
                if (colorDialog.ShowDialog(this) == DialogResult.OK)
                {
                    ItemBean item = (ItemBean)dgvItem.Rows[e.RowIndex].DataBoundItem;
                    item.LineColor = colorDialog.Color;
                    dgvItem.CommitEdit(DataGridViewDataErrorContexts.Commit);

                    // イベント発行
                    if (ItemColorChanged != null)
                    {
                        ItemColorChanged(item);
                    }
                }
            }

        }

        /// <summary>
        ///　CellValueChangedイベントは、チェックボックスがチェックされた後に別のセルにフォーカスを移すなどして
        ///　値がコミットされた時にしか発生しないので、チェックされた後に強制的にコミットする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvItem_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            string colName = dgvItem.CurrentCell.OwningColumn.Name;

            if ((colName == "visibleCheck" || colName == "CurrentValueVisible") && dgvItem.IsCurrentCellDirty)
            {
                //コミットする
                dgvItem.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// セルの値が変化した。
        /// セルに対応するイベントを発行する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvItem_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            //"visibleCheck"列ならば、チェックがクリックされた
            if (dgvItem.Columns[e.ColumnIndex].Name == "visibleCheck")
            {
                ItemBean item = (ItemBean)dgvItem.Rows[e.RowIndex].DataBoundItem;

                // イベント発行
                if (ItemVisibleChanged != null)
                {
                    ItemVisibleChanged(item);
                }
            }

            //"CurrentValueVisible"列ならば、チェックがクリックされた
            if (dgvItem.Columns[e.ColumnIndex].Name == "CurrentValueVisible")
            {
                ItemBean item = (ItemBean)dgvItem.Rows[e.RowIndex].DataBoundItem;

                // イベント発行
                if (ItemCurrentValueVisibleChanged != null)
                {
                    ItemCurrentValueVisibleChanged(item);
                }
            }


            // "yAxisMin"か"yAxisMax"ならレンジ変更
            if (dgvItem.Columns[e.ColumnIndex].Name == "YAxisMin"
                || dgvItem.Columns[e.ColumnIndex].Name == "YAxisMax")
            {
                ItemBean item = (ItemBean)dgvItem.Rows[e.RowIndex].DataBoundItem;

                // イベント発行
                if (ItemRangeChanged != null)
                {
                    ItemRangeChanged(item);
                }
            }

            // "name"なら名称変更
            if (dgvItem.Columns[e.ColumnIndex].Name == "name")
            {
                ItemBean item = (ItemBean)dgvItem.Rows[e.RowIndex].DataBoundItem;

                // イベント発行
                if (ItemNameChanged != null)
                {
                    ItemNameChanged(item);
                }
            }


        }

        /// <summary>
        /// 表示されているコントロールがDataGridViewComboBoxEditingControlなら
        /// SelectIndexChangedイベントを登録する
        /// 入力制限をするため、
        /// 表示されているコントロールがDataGridViewTextBoxEditingControlなら
        /// KeyPressイベントを登録する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvItem_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            //表示されているコントロールがDataGridViewComboBoxEditingControlなら
            //SelectIndexChangedイベントを登録する
            if (e.Control is DataGridViewComboBoxEditingControl)
            {
                DataGridView dgv = (DataGridView)sender;

                //該当する列か調べる
                if (dgv.CurrentCell.OwningColumn.Name == "lineWidthCombo")
                {
                    //編集のために表示されているコントロールを取得
                    this.dataGridViewComboBox = (DataGridViewComboBoxEditingControl)e.Control;
                    //SelectedIndexChangedイベントハンドラを追加
                    this.dataGridViewComboBox.SelectedIndexChanged += new EventHandler(dataGridViewComboBox_SelectedIndexChanged);
                }
            }

            //表示されているコントロールがDataGridViewTextBoxEditingControlなら
            //KeyPressイベントを登録する
            if (e.Control is DataGridViewTextBoxEditingControl)
            {
                DataGridView dgv = (DataGridView)sender;
                DataGridViewTextBoxEditingControl tb = (DataGridViewTextBoxEditingControl)e.Control;
                tb.KeyPress -= new KeyPressEventHandler(dataGridViewTextBox_KeyPress);
                tb.ImeMode = ImeMode.On;

                if (dgv.CurrentCell.OwningColumn.Name == "YAxisMin"
                    || dgv.CurrentCell.OwningColumn.Name == "YAxisMax") // 数字入力制限の列を指定する 
                {
                    tb.ImeMode = ImeMode.Disable;
                    tb.KeyPress += new KeyPressEventHandler(dataGridViewTextBox_KeyPress);
                }
            }

        }

        /// <summary>
        /// SelectedIndexChangedイベントハンドラを削除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvItem_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            //　SelectedIndexChangedイベントハンドラを削除
            if (this.dataGridViewComboBox != null)
            {
                this.dataGridViewComboBox.SelectedIndexChanged -= new EventHandler(dataGridViewComboBox_SelectedIndexChanged);
                this.dataGridViewComboBox = null;
            }
        }


        /// <summary>
        ///　DataGridViewに表示されているコンボボックスの
        ///　SelectedIndexChangedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridViewComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //選択されたアイテムを取得
            DataGridViewComboBoxEditingControl cb = (DataGridViewComboBoxEditingControl)sender;

            ItemBean item = (ItemBean)dgvItem.Rows[cb.EditingControlRowIndex].DataBoundItem;
            item.LineWidth = double.Parse(cb.SelectedItem.ToString());

            // イベント発行
            if (ItemWidthChanged != null)
            {
                ItemWidthChanged(item);
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
        /// 選択されたコントロールがDataGridViewComboBoxCellなら
        /// ワンクリックで選択できるようにする
        /// 選択されたコントロールがDataGridViewTextBoxCellなら
        /// ワンクリックで編集できるようにする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvItem_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            //選択されたコントロールがDataGridViewComboBoxCellなら
            //ワンクリックで選択できるようにする
            if (dgvItem.Columns[e.ColumnIndex].CellType.Name == "DataGridViewComboBoxCell")
            {
                SendKeys.Send("{F4}");
            }

            //選択されたコントロールがDataGridViewTextBoxCellなら
            //ワンクリックで編集できるようにする
            if (dgvItem.Columns[e.ColumnIndex].CellType.Name == "DataGridViewTextBoxCell")
            {
                if (dgvItem.Focused)
                {
                    dgvItem.BeginEdit(true);
                }
            }
        }

        /// <summary>
        /// CellValidatingハンドラ
        /// 数値項目の形式チェック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvItem_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            double dblValue;

            // "yAxisMin"か"yAxisMax"なら数字入力
            if (dgvItem.Columns[e.ColumnIndex].Name == "YAxisMin"
                || dgvItem.Columns[e.ColumnIndex].Name == "YAxisMax")
            {
                string colName = dgvItem.Columns[e.ColumnIndex].HeaderText;

                // null？
                if (string.IsNullOrEmpty(e.FormattedValue.ToString()))
                {
                    MessageBox.Show(string.Format("{0}の値は省略できません。", colName), "Item Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                }

                // doubleに変換可能か
                else if (!double.TryParse(e.FormattedValue.ToString(), out dblValue))
                {
                    MessageBox.Show(string.Format("{0}に入力した数値形式が不正です。[{1}]", colName, e.FormattedValue.ToString()), "Item Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                }
            }
        }
    }
}
