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
    /// <summary>
    /// Formの基底クラス
    /// </summary>
    public partial class BaseForm : Form
    {
        /// <summary>
        /// 設定値
        /// </summary>
        protected SettingTable settings;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public BaseForm()
        {
            InitializeComponent();

            settings = new SettingTable(this.GetType().Name);
        }

        /// <summary>
        /// LOADイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaseForm_Load(object sender, EventArgs e)
        {
            string myname = this.GetType().Name;

            Tracer.WriteVerbose(myname + ":　画面ロード");

            //WindowRect復元
            if (settings[myname, "Top"] != "")
            {
                this.Location = settings.GetWindowLocation(myname, this.Location);
                this.Size = settings.GetWindowSize(myname, this.Size);
            }
        }

        /// <summary>
        /// CLOSEDイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaseForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            string myname = this.GetType().Name;

            Tracer.WriteVerbose(myname + ":　画面CLOSED");

            // WINDOW状態を保存する
            Point loc = this.Location;
            //ロケーションがマイナスになるとDockView表示しないことがあるので補正する
            if (this.Location.X < 0)
            {
                loc.X = 0;
            }
            if (this.Location.Y < 0)
            {
                loc.Y = 0;
            }
            settings.SetWindowLocation(myname, loc);
            settings.SetWindowSize(myname, this.Size);
        }
    }
}
