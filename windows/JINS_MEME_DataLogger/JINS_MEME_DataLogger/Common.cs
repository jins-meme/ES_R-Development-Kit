using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 各クラスから共通に呼び出される関数を記載します。
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// メッセージＢＯＸ表示
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="buttons"></param>
        /// <param name="icon"></param>
        public static void ShowMessageBox(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Error:
                    Tracer.WriteError("[{0}]:{1}", title, message);
                    break;
                case MessageBoxIcon.Warning:
                    Tracer.WriteWarning("[{0}]:{1}", title, message);
                    break;
                default:
                    Tracer.WriteInformation("[{0}]:{1}", title, message);
                    break;
            }
            MessageBox.Show(message, title, buttons, icon);
        }
    }
}
