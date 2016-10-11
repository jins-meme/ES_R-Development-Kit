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
    /// グラフ設定画面処理を記述します。
    /// </summary>
    public partial class GraphSettingForm : Form
    {
        /// <summary>
        /// グラフ関連の設定値
        /// </summary>
        public SettingTable graphSettings = new SettingTable("GraphSettings");

        /// <summary>
        /// 軸フォントサイズ項目
        /// </summary>
        private ComboItemObj[] axisFontSizeItems = new ComboItemObj[]
        {
            new ComboItemObj("14 pt", 14),
            new ComboItemObj("18 pt", 18),
            new ComboItemObj("22 pt", 22),
            new ComboItemObj("26 pt", 28),
        };

        /// <summary>
        /// タイトルフォントサイズ項目
        /// </summary>
        private ComboItemObj[] titleFontSizeItems = new ComboItemObj[]
        {
            new ComboItemObj("14 pt", 14),
            new ComboItemObj("18 pt", 18),
            new ComboItemObj("22 pt", 22),
            new ComboItemObj("26 pt", 28),
        };

        /// <summary>
        /// LINE幅フォントサイズ項目
        /// </summary>
        private ComboItemObj[] lineWidthItems = new ComboItemObj[]
        {
            new ComboItemObj("1pt", 1),
            new ComboItemObj("2pt", 2),
            new ComboItemObj("3pt", 3),
            new ComboItemObj("4pt", 4),
        };


        public Color GraphBackColor
        {
            get
            {
                return backColorLabel.BackColor;
            }
            private set
            {
                backColorLabel.BackColor = value;
                backColorLabel.Text = value.Name;
            }
        }

        public bool Gradation
        {
            get
            {
                return gradationCheck.Checked;
            }
            private set 
            {
                gradationCheck.Checked = value;
            }
        }

        public int AxisFontSize
        {
            get
            {
                return (int)axisFontSizeCombo.SelectedValue;
            }
        }

        public int TitleFontSize
        {
            get
            {
                return (int)titleFontSizeCombo.SelectedValue;
            }
        }

        public Color TitleColor
        {
            get
            {
                return titleColorLabel.BackColor;
            }
            private set
            {
                titleColorLabel.BackColor = value;
                titleColorLabel.Text = value.Name;
            }
        }

        public int LineWidth
        {
            get
            {
                return (int)lineWidthCombo.SelectedValue;
            }
        }



        public GraphSettingForm()
        {
            InitializeComponent();
        }

        private void GraphSettingForm_Load(object sender, EventArgs e)
        {
            // グラフ設定値の取得
            graphSettings.ReadConfig();

            // 軸フォントサイズのコンボ項目設定
            axisFontSizeCombo.SetComboItems(axisFontSizeItems, graphSettings.GetInteger("Graph", "AxisFontSize", 22));

            // タイトルフォントサイズのコンボ項目設定
            titleFontSizeCombo.SetComboItems(titleFontSizeItems, graphSettings.GetInteger("Graph", "TitleFontSize", 22));

            // 背景色
            //GraphBackColor = graphSettings.GetColor("Graph", "GraphBackColor", Color.FromArgb(0xff, 0x53, 0x53, 0x53));
            GraphBackColor = graphSettings.GetColor("Graph", "GraphBackColor", Color.Silver);

            // グラデーション
            Gradation = graphSettings.GetBool("Graph", "Gradation", true);

            // タイトル文字色
            TitleColor = graphSettings.GetColor("Graph", "TitleColor", Color.Olive);

            // LINE幅
            lineWidthCombo.SetComboItems(lineWidthItems, graphSettings.GetInteger("Graph", "LineWidth", 1));

        }

        private void backColorLabel_Click(object sender, EventArgs e)
        {
            DialogResult res = colorDialog.ShowDialog(this);

            if (res == System.Windows.Forms.DialogResult.OK)
            {
                backColorLabel.BackColor = colorDialog.Color;
                backColorLabel.Text = colorDialog.Color.Name;
            }
        }

        private void titleColorLabel_Click(object sender, EventArgs e)
        {
            DialogResult res = colorDialog.ShowDialog(this);

            if (res == System.Windows.Forms.DialogResult.OK)
            {
                titleColorLabel.BackColor = colorDialog.Color;
                titleColorLabel.Text = colorDialog.Color.Name;
            }

        }



        private void applyButton_Click(object sender, EventArgs e)
        {
            graphSettings.SetInteger("Graph", "AxisFontSize", AxisFontSize);
            graphSettings.SetInteger("Graph", "TitleFontSize", TitleFontSize);
            graphSettings.SetColor("Graph", "GraphBackColor", GraphBackColor);
            graphSettings.SetBool("Graph", "Gradation", Gradation);
            graphSettings.SetColor("Graph", "TitleColor", TitleColor);
            graphSettings.SetInteger("Graph", "LineWidth", LineWidth);

            graphSettings.WriteConfig();

            this.DialogResult = System.Windows.Forms.DialogResult.OK;

            this.Close();
        }


    }
}
