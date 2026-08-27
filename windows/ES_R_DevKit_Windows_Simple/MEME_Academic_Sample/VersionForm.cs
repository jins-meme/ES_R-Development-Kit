using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace MEME_Academic_Sample
{
    public partial class VersionForm : Form
    {
        public VersionForm()
        {
            InitializeComponent();
            this.Icon = AppInfo.LoadIcon();
        }
        private void VersionForm_Load(object sender, EventArgs e)
        {
            this.applicationVersionLabel.Text = string.Format("{0}  Version {1}   ( {2}-bit running )", AppInfo.ProductName, AppInfo.Version, IntPtr.Size * 8);
        }
    }
}
