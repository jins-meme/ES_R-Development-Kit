using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// COMBOBOX拡張クラス
    /// マウスホイールでindex変化しないようにした
    /// </summary>
    public class ComboBoxEx : ComboBox
    {
        private const int WM_MOUSEWHEEL = 0x20A;

        /// <summary>
        /// ComboBoxに項目リスト設定
        /// </summary>
        /// <param name="combo"></param>
        /// <param name="objs"></param>
        public void SetComboItems(ComboItemObj[] objs, object selectedValue)
        {
            this.DataSource = objs.ToList();
            this.DisplayMember = "Label";
            this.ValueMember = "Value";
            this.SelectedValue = selectedValue;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg != WM_MOUSEWHEEL)
            {
                base.WndProc(ref m);
            }
        }
    }
}
