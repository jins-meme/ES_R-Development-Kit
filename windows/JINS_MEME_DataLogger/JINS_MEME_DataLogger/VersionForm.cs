using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// バージョン情報表示の画面クラスです。
    /// </summary>
    public partial class VersionForm : Form
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public VersionForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// フォームロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VersionForm_Load(object sender, EventArgs e)
        {
            // TODO : OpenGLのバージョン表示を隠す
            this.Height = 280;

            FileVersionInfo appVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            this.applicationVersionLabel.Text = string.Format("{0}  Version {1}   ( {2}-bit running )", Path.GetFileNameWithoutExtension(appVersion.FileName).Replace('_', ' '), appVersion.FileVersion, IntPtr.Size * 8);

            // グラフコントロールバージョン表示
            FileVersionInfo graphVersion = FileVersionInfo.GetVersionInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\ZedGraph.dll");
            this.graphControlVersionLabel.Text = string.Format("Graph control ({0}):  Ver. {1}", Path.GetFileNameWithoutExtension(graphVersion.FileName), graphVersion.FileVersion);

            this.graphControlDescriptionLabel.Text = "This product includes the ZedGraph Class Library." + System.Environment.NewLine +
                                                     "ZedGraph is licensed under the GNU Lesser General Public License (LGPL) version 2.1." + System.Environment.NewLine +
                                                     "Please note that ZedGraph is provided without any warranty." + System.Environment.NewLine +
                                                     System.Environment.NewLine +
                                                     "ZedGraph Class Library - A Flexible Line Graph/Bar Graph Library in C#" + System.Environment.NewLine +
                                                     "Copyright (c) 2005  John Champion";

            // OpenGLライブラリーバージョン表示
            //FileVersionInfo openGLLibraryVersion = FileVersionInfo.GetVersionInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\freeglut.dll");
            //this.openGLLibraryVersionLabel.Text = string.Format("OpenGL Library ({0})  Ver. {1}", Path.GetFileNameWithoutExtension(openGLLibraryVersion.FileName), openGLLibraryVersion.FileVersion);
            this.openGLLibraryVersionLabel.Text = "OpenGL Library (freeglut)  Ver. 2.8.1";

            this.openGLLibraryDescriptionLabel.Text = "The MIT License" + System.Environment.NewLine +
                                                      System.Environment.NewLine +
                                                      "Copyright (c) 1999-2015 Pawel W. Olszta. All Rights Reserved." + System.Environment.NewLine +
                                                      System.Environment.NewLine +
                                                      "Permission is hereby granted, free of charge, to any person obtaining a copy " +
                                                      "of this software and associated documentation files (the \"Software\"), to deal " +
                                                      "in the Software without restriction, including without limitation the rights " +
                                                      "to use, copy, modify, merge, publish, distribute, sublicense, and/or sell" +
                                                      "copies of the Software, and to permit persons to whom the Software is " +
                                                      "furnished to do so, subject to the following conditions:" + System.Environment.NewLine +
                                                      System.Environment.NewLine +
                                                      "The above copyright notice and this permission notice shall be included in " +
                                                      "all copies or substantial portions of the Software." + System.Environment.NewLine +
                                                      System.Environment.NewLine +
                                                      "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR " +
                                                      "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
                                                      "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE " +
                                                      "AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER " +
                                                      "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, " +
                                                      "OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN " +
                                                      "THE SOFTWARE.";
        }

        /// <summary>
        /// グラフコントロールリンクラベルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void graphControlLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.graphControlLinkLabel.LinkVisited = true;
            System.Diagnostics.Process.Start("http://zedgraph.sourceforge.net/index.html");
        }

        /// <summary>
        /// OpenGLライブラリーリンクラベルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openGLLibraryLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.openGLLibraryLinkLabel.LinkVisited = true;
            System.Diagnostics.Process.Start("http://freeglut.sourceforge.net/index.php");
        }
    }
}
