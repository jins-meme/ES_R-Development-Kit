using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace JINS_MEME_DataLogger
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 起動中のプロセス数を取得
            Process hProcess = Process.GetCurrentProcess();
            int appCount = Process.GetProcessesByName(hProcess.ProcessName).Length;
            hProcess.Close();
            hProcess.Dispose();

            if (appCount > 2)
            {
                MessageBox.Show("It is not possible to start the three applications.", "Startup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Tracer.WriteInformation("****** Start application ******");
                Application.Run(new mainForm());
                Tracer.WriteInformation("****** End application ******");
            }
        }
    }
}
