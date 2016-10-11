using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace JINS_MEME_DataLogger
{
    [ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip)]
    public class ToolStripCheckBox : ToolStripControlHost
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ToolStripCheckBox()
            : base(new CheckBox())
        {
        }


        public CheckBox CheckBox
        {
            get { return (CheckBox)Control; }
        }

        public event EventHandler CheckChanged
        {
            add { this.CheckBox.CheckedChanged += value; }
            remove { this.CheckBox.CheckedChanged -= value; }
        }

        public bool Checked
        {
            get { return CheckBox.Checked; }
            set { this.CheckBox.Checked = value; }
        }
    }
}
