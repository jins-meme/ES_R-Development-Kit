using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JINS_MEME_DataLogger
{
    public partial class PostureForm : Form
    {
        bool drawEnabled = true;

        public PostureForm()
        {
            InitializeComponent();
        }

        public void Draw(long[] quat)
        {
            if (this == null || this.IsDisposed || !this.Visible || !this.drawEnabled)
            {
                return;
            }

            if (this.InvokeRequired)
            {
                Invoke(new Action<long[]>(Draw), new object[] { quat });
                return;
            }

            postureControl.SetValue(quat);
            postureControl.Refresh();
        }

        private void PostureForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            drawEnabled = false;
        }


    }
}
