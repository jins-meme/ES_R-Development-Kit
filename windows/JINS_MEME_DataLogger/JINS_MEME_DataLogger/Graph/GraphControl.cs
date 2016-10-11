using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// ３グラフを纏めたユーザーコントロールです。
    /// </summary>
    public partial class GraphControl : UserControl
    {
        /// <summary>
        /// 最新データ表示モード
        /// </summary>
        private bool newestDataOnly = false;

        /// <summary>
        /// グラフ関連の設定値
        /// </summary>
        private SettingTable graphSettings = new SettingTable("GraphSettings");

        private long lastElapsed = 0;

        /// <summary>
        /// アクセス簡便化のコントロール配列
        /// </summary>
        private GraphView[] graphViews;

        /// <summary>
        /// グラフデータ種別
        /// </summary>
        private int[] graphDataType = new int[3];

        /// <summary>
        /// 加速度グラフは何番目？
        /// 未使用時は-1をセットする
        /// </summary>
        private int accelerationChartIndex = 0;

        /// <summary>
        /// 角速度グラフは何番目？
        /// 未使用時は-1をセットする
        /// </summary>
        private int angularVelocityChartIndex = 1;

        /// <summary>
        /// 眼電位グラフは何番目？
        /// 未使用時は-1をセットする
        /// </summary>
        private int electrooculographyChartIndex = 2;

        /// <summary>
        /// クォータニオングラフは何番目？
        /// 未使用時は-1をセットする
        /// </summary>
        private int quaternionChartIndex = -1;


        /// <summary>
        /// 加速度グラフ
        /// </summary>
        private GraphView AccelerationChart 
        { 
            get 
            {
                return (accelerationChartIndex < 0) ? null : graphViews[accelerationChartIndex];
            }
        }

        /// <summary>
        /// 角速度グラフ
        /// </summary>
        private GraphView AngularVelocityChart
        {
            get
            {
                return (angularVelocityChartIndex < 0) ? null : graphViews[angularVelocityChartIndex];
            }
        }

        /// <summary>
        /// 眼電位グラフ
        /// </summary>
        private GraphView ElectrooculographyChart
        {
            get
            {
                return (electrooculographyChartIndex < 0) ? null : graphViews[electrooculographyChartIndex];
            }
        }

        /// <summary>
        /// クォータニオングラフ
        /// </summary>
        private GraphView QuaternionChart
        {
            get
            {
                return (quaternionChartIndex < 0) ? null : graphViews[quaternionChartIndex];
            }
        }

        /// <summary>
        /// Chartのインデックスを消去
        /// </summary>
        public void ClearChartIndex()
        {
            this.accelerationChartIndex = -1;
            this.angularVelocityChartIndex = -1;
            this.electrooculographyChartIndex = -1;
            this.quaternionChartIndex = -1;
            for (int graphIndex = 0; graphIndex < this.graphViews.Length; graphIndex++)
            {
                this.graphDataType[graphIndex] = 0;
            }
        }

        /// <summary>
        /// Chart1の軸
        /// </summary>
        private AxisBean chart1Axis;
        [Browsable(false)]
        public AxisBean Chart1Axis 
        {
            get { return chart1Axis; }
            set
            {
                chart1Axis = value;
                if (chart1Axis == null)
                {
                    return;
                }

                //graphView1.Title = string.Format("Chart1 : {0} {1}", chart1Axis.Name, chart1Axis.UnitName.Equals("") ? "" : "(" + chart1Axis.UnitName + ")");
                graphView1.Title = string.Format("Chart1 : {0}", chart1Axis.Name);
                graphDataType[0] = chart1Axis.Id;

                // Chart1のindexと項目設定
                switch ((AxisIds)chart1Axis.Id)
                {
                    case AxisIds.Accelerometer:
                        accelerationChartIndex = 0;
                        Chart1Items = ItemMaster.AccelerationItems[0].ItemList;
                        break;
                    case AxisIds.AngularVelocity:
                        angularVelocityChartIndex = 0;
                        Chart1Items = ItemMaster.AngularVelocityItems[0].ItemList;
                        break;
                    case AxisIds.Electrooculography:
                        electrooculographyChartIndex = 0;
                        Chart1Items = ItemMaster.ElectrooculographyItems[0].ItemList;
                        break;
                    case AxisIds.Quaternion:
                        quaternionChartIndex = 0;
                        Chart1Items = ItemMaster.QuaternionItems[0].ItemList;
                        break;
                }
                graphView1.Refresh();
            }
        }

        /// <summary>
        /// Chart2の軸
        /// </summary>
        private AxisBean chart2Axis;
        [Browsable(false)]
        public AxisBean Chart2Axis
        {
            get { return chart2Axis; }
            set
            {
                chart2Axis = value;
                if (chart2Axis == null)
                {
                    return;
                }

                //graphView2.Title = string.Format("Chart2 : {0} {1}", chart2Axis.Name, chart2Axis.UnitName.Equals("") ? "" : "(" + chart2Axis.UnitName + ")");
                graphView2.Title = string.Format("Chart2 : {0}", chart2Axis.Name);
                graphDataType[1] = chart2Axis.Id;

                // Chart2のindexと項目設定
                switch ((AxisIds)chart2Axis.Id)
                {
                    case AxisIds.Accelerometer:
                        accelerationChartIndex = 1;
                        Chart2Items = ItemMaster.AccelerationItems[1].ItemList;
                        break;
                    case AxisIds.AngularVelocity:
                        angularVelocityChartIndex = 1;
                        Chart2Items = ItemMaster.AngularVelocityItems[1].ItemList;
                        break;
                    case AxisIds.Electrooculography:
                        electrooculographyChartIndex = 1;
                        Chart2Items = ItemMaster.ElectrooculographyItems[1].ItemList;
                        break;
                    case AxisIds.Quaternion:
                        quaternionChartIndex = 1;
                        Chart2Items = ItemMaster.QuaternionItems[1].ItemList;
                        break;
                }
                graphView2.Refresh();
            }
        }

        /// <summary>
        /// Chart3の軸
        /// </summary>
        private AxisBean chart3Axis;
        [Browsable(false)]
        public AxisBean Chart3Axis
        {
            get { return chart3Axis; }
            set
            {
                chart3Axis = value;
                if (chart3Axis == null)
                {
                    return;
                }

                //graphView3.Title = string.Format("Chart3 : {0} {1}", chart3Axis.Name, chart3Axis.UnitName.Equals("") ? "" : "(" + chart3Axis.UnitName + ")");
                graphView3.Title = string.Format("Chart3 : {0}", chart3Axis.Name);
                graphDataType[2] = chart3Axis.Id;

                // Chart3のindexと項目設定
                switch ((AxisIds)chart3Axis.Id)
                {
                    case AxisIds.Accelerometer:
                        accelerationChartIndex = 2;
                        Chart3Items = ItemMaster.AccelerationItems[2].ItemList;
                        break;
                    case AxisIds.AngularVelocity:
                        angularVelocityChartIndex = 2;
                        Chart3Items = ItemMaster.AngularVelocityItems[2].ItemList;
                        break;
                    case AxisIds.Electrooculography:
                        electrooculographyChartIndex = 2;
                        Chart3Items = ItemMaster.ElectrooculographyItems[2].ItemList;
                        break;
                    case AxisIds.Quaternion:
                        quaternionChartIndex = 2;
                        Chart3Items = ItemMaster.QuaternionItems[2].ItemList;
                        break;
                }
                graphView3.Refresh();
            }
        }

        /// <summary>
        /// Chart1の項目
        /// </summary>
        private List<ItemBean> chart1Items;
        private List<ItemBean> Chart1Items 
        {
            get { return chart1Items; }
            set
            {
                chart1Items = value;
                graphView1.SetItems(chart1Items);
            }
        }

        /// <summary>
        /// Chart2の項目
        /// </summary>
        private List<ItemBean> chart2Items;
        private List<ItemBean> Chart2Items 
        {
            get { return chart2Items; }
            set
            {
                chart2Items = value;
                graphView2.SetItems(chart2Items);
            }
        }

        /// <summary>
        /// Chart3の項目
        /// </summary>
        private List<ItemBean> chart3Items;
        private List<ItemBean> Chart3Items 
        {
            get { return chart3Items; }
            set
            {
                chart3Items = value;
                graphView3.SetItems(chart3Items);
            }
        }

        /// <summary>
        /// AUTO スクロール、かつ流れていったデータをメモリから破棄する
        /// リアルタイム表示のときはtrueにして負荷を減らす
        /// </summary>
        private bool dataEraseMode = false;
        public bool DataEraseMode 
        {
            get { return dataEraseMode; }
            set 
            {
                dataEraseMode = value;

                if (dataEraseMode && !autoScrollCheck.Checked)
                {
                    autoScrollCheck.Checked = dataEraseMode;
                }

                if (dataEraseMode)
                {
                    autoScrollCheck.Enabled = false;
                    newDataOnlySpan.Enabled = false;
                }
                else
                {
                    autoScrollCheck.Enabled = true;
                    newDataOnlySpan.Enabled = true;
                }

                for (int graphIndex = 0; graphIndex < this.graphViews.Length; graphIndex++)
                {
                    this.graphViews[graphIndex].DataEraseMode = dataEraseMode;
                }
            }
        }

        /// <summary>
        /// グラフ操作有効／無効
        /// </summary>
        private bool graphOperation = true;
        public bool GraphOperation
        {
            get { return this.graphOperation; }
            set
            {
                this.graphOperation = value;

                for (int graphIndex = 0; graphIndex < this.graphViews.Length; graphIndex++)
                {
                    this.graphViews[graphIndex].GraphOperation = value;
                }
            }
        }

        /// <summary>
        /// グラフ画面処理
        /// 加速度、角速度、眼電位のグラフを制御する
        /// </summary>
        public GraphControl()
        {
            InitializeComponent();

            graphViews = new GraphView[] { graphView1, graphView2, graphView3 };

            // 項目マスターロード
            AxisMaster.Load();
            ItemMaster.Load();

            // defaultは chart1=加速度 chart2=角速度 chart3=眼電圧
            Chart1Axis = AxisMaster.AccelerationAxis;
            Chart2Axis = AxisMaster.AngularVelocityAxis;
            Chart3Axis = AxisMaster.ElectrooculographyAxis;
        }

        /// <summary>
        /// 加速度のデータを追加する
        /// </summary>
        /// <param name="data"></param>
        public void AddAccelerationData(List<MeasureBean> data)
        {
            //if (AccelerationChart != null)
            //{
            //    AccelerationChart.Draw(data);
            //}

            this.drawGraphData(AxisIds.Accelerometer, data);
        }

        /// <summary>
        /// 加速度のデータをクリアする
        /// </summary>
        /// <param name="data"></param>
        public void ClearAccelerationData()
        {
            //if (AccelerationChart != null)
            //{
            //    AccelerationChart.Clear();
            //}

            this.clearGraphData(AxisIds.Accelerometer);
        }


        /// <summary>
        /// 角速度のデータを追加する
        /// </summary>
        /// <param name="data"></param>
        public void AddAngularVelocityData(List<MeasureBean> data)
        {
            //if (AngularVelocityChart != null)
            //{
            //    AngularVelocityChart.Draw(data);
            //}

            this.drawGraphData(AxisIds.AngularVelocity, data);
        }

        /// <summary>
        /// 角速度のデータをクリアする
        /// </summary>
        /// <param name="data"></param>
        public void ClearAngularVelocityData()
        {
            //if (AngularVelocityChart != null)
            //{
            //    AngularVelocityChart.Clear();
            //}

            this.clearGraphData(AxisIds.AngularVelocity);
        }

        /// <summary>
        /// 眼電位のデータを追加する
        /// </summary>
        /// <param name="data"></param>
        public void AddElectrooculographyData(List<MeasureBean> data)
        {
            //if (ElectrooculographyChart != null)
            //{
            //    ElectrooculographyChart.Draw(data);
            //}

            this.drawGraphData(AxisIds.Electrooculography, data);
        }

        /// <summary>
        /// 眼電位のデータをクリアする
        /// </summary>
        /// <param name="data"></param>
        public void ClearElectrooculographyData()
        {
            //if (ElectrooculographyChart != null)
            //{
            //    ElectrooculographyChart.Clear();
            //}

            this.clearGraphData(AxisIds.Electrooculography);
        }

        /// <summary>
        /// クォータニオンのデータを追加する
        /// </summary>
        /// <param name="data"></param>
        public void AddQuaternionData(List<MeasureBean> data)
        {
            //if (QuaternionChart != null)
            //{
            //    QuaternionChart.Draw(data);
            //}

            this.drawGraphData(AxisIds.Quaternion, data);
        }

        /// <summary>
        /// クォータニオンのデータをクリアする
        /// </summary>
        /// <param name="data"></param>
        public void ClearQuaternionData()
        {
            //if (QuaternionChart != null)
            //{
            //    QuaternionChart.Clear();
            //}

            this.clearGraphData(AxisIds.Quaternion);
        }

        /// <summary>
        /// グラフデータを描画する
        /// </summary>
        /// <param name="drawAxisIds"></param>
        private void drawGraphData(AxisIds drawAxisIds, List<MeasureBean> data)
        {
            for (int graphIndex = 0; graphIndex < this.graphViews.Length; graphIndex++)
            {
                if (this.graphDataType[graphIndex] == (int)drawAxisIds)
                {
                    this.graphViews[graphIndex].Draw(data);
                }
            }
        }

        /// <summary>
        /// グラフデータを消去する
        /// </summary>
        /// <param name="drawAxisIds"></param>
        private void clearGraphData(AxisIds drawAxisIds)
        {
            for (int graphIndex = 0; graphIndex < this.graphViews.Length; graphIndex++)
            {
                if (this.graphDataType[graphIndex] == (int)drawAxisIds)
                {
                    this.graphViews[graphIndex].Clear();
                }
            }
        }

        /// <summary>
        /// スクロールリセット
        /// </summary>
        public void ScrollReset()
        {
            for (int graphIndex = 0; graphIndex < this.graphViews.Length; graphIndex++)
            {
                this.graphViews[graphIndex].ScrollReset();
            }
        }

        /// <summary>
        /// 経過時間の初期化
        /// </summary>
        public void ClearElapsed()
        {
            lastElapsed = 0;
            elapsedLabel.Text = "00:00:00";
        }

        /// <summary>
        /// 経過時間を表示
        /// </summary>
        /// <param name="obj"></param>
        public void ShowElapsed(long elapsed)
        {
            this.BeginInvoke(new Action<long>(_showElapsed), new object[] { elapsed });
        }
        private void _showElapsed(long elapsed)
        {
            if (elapsed - lastElapsed > 1000)
            {
                TimeSpan span = new TimeSpan(0, 0, 0, 0, (int)elapsed);
                elapsedLabel.Text = string.Format(@"{0:hh\:mm\:ss}", span);
                lastElapsed = elapsed;
            }
        }

        /// <summary>
        /// フォームロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GraphControl_Load(object sender, EventArgs e)
        {
            this.toolStrip1.Width = this.Width;

            zoomFromText.Text = string.Format("{0:F1}", 0 / 1000.0);
            zoomToText.Text = string.Format("{0:F1}", 60 * 1000 / 1000.0);

            int displayDataSpan = Constants.GRAPH_REFRASH_INTERVAL;

            // グラフ設定値の取得
            graphSettings.ReadConfig();

            foreach (GraphView graphView in graphViews)
            {
                graphView.TitleFontSize = graphSettings.GetInteger("Graph", "TitleFontSize", 22);
                graphView.TitleColor = graphSettings.GetColor("Graph", "TitleColor", Color.Red);
                graphView.YAxisTitleFontSize = graphSettings.GetInteger("Graph", "AxisFontSize", 22);
                graphView.YAxisScaleFontSize = graphSettings.GetInteger("Graph", "AxisFontSize", 22);
                graphView.XAxisTitleFontSize = graphSettings.GetInteger("Graph", "AxisFontSize", 22);
                graphView.XAxisScaleFontSize = graphSettings.GetInteger("Graph", "AxisFontSize", 22);
                graphView.DataLabelFontSize = graphSettings.GetInteger("Graph", "ItemFontSize", 2);
                //graphView.GraphBackColor = graphSettings.GetColor("Graph", "GraphBackColor", Color.FromArgb(0xff, 0x53, 0x53, 0x53));
                graphView.GraphBackColor = graphSettings.GetColor("Graph", "GraphBackColor", Color.Silver);
                graphView.Gradation = graphSettings.GetBool("Graph", "Gradation", true);
                graphView.NewDataOnlySpan = Convert.ToDouble(displayDataSpan);
            }

            // 最新表示モード時間幅
            //newDataOnlySpan.Text = graphSettings.GetStringValue("newDataOnlySpan", "span", "15.0");
            //zoomToText.Text = graphSettings.GetStringValue("newDataOnlySpan", "span", "15.0");
            newDataOnlySpan.Text = string.Format("{0:0.0}", displayDataSpan);
            zoomToText.Text = string.Format("{0:0.0}", displayDataSpan);

            this.ParentForm.FormClosed += new FormClosedEventHandler(ParentForm_FormClosed);


            // デフォルトをスクロールモードにする
            this.autoScrollCheck.Checked = true;
        }

        /// <summary>
        /// 親フォームのクローズド
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ParentForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            ItemMaster.Save();

            // 最新表示モード時間幅
            //graphSettings.SetStringValue("newDataOnlySpan", "span", newDataOnlySpan.Text);
        }

        /// <summary>
        /// グラフ表示割合変更
        /// </summary>
        /// <param name="graphIndex">グラフインデックス（0～2）</param>
        /// <param name="graphPercent">グラフの表示割合</param>
        public void DisplayGraphView(int graphIndex, float graphPercent)
        {
            graphTableLayout.RowStyles[graphIndex] = new RowStyle(SizeType.Percent, graphPercent);
        }

        /// <summary>
        /// グラフ表示／非表示切り替え
        /// </summary>
        /// <param name="graphIndex"></param>
        /// <param name="visible"></param>
        public void VisibleGraphViwe(int graphIndex, bool visible)
        {
            GraphView[] views = new GraphView[] { this.graphView1, this.graphView2, this.graphView3 };
            views[graphIndex].Visible = visible;
        }

        /// <summary>
        /// 拡大ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void zoomInButton_Click(object sender, EventArgs e)
        {
            Tracer.WriteInformation("zoom inボタンクリック");

            try
            {
                double from = double.Parse(zoomFromText.Text);
                double to = double.Parse(zoomToText.Text);

                if (from >= to)
                {
                    MessageBox.Show(string.Format("range must be min < max"), "Graph Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (from < 0)
                {
                    MessageBox.Show(string.Format("min must be greater than 0"), "Graph Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // データ無いときはこのチェックは行わないようにする
                if (graphView1.DataXmax > 0.0 && to > graphView1.DataXmax)
                {
                    MessageBox.Show(string.Format("max must be lesser than data max"), "Graph Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                foreach (GraphView graphView in graphViews)
                {
                    graphView.ZoomReset();
                    graphView.ZoomXRange(from, to);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
                MessageBox.Show(ex.Message, "Graph Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// 最新データモードのチェック変化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newDataModeCheck_CheckChanged(object sender, EventArgs e)
        {
            newestDataOnly = autoScrollCheck.Checked;

            foreach (GraphView graphView in graphViews)
            {
                if (newestDataOnly)
                {
                    graphView.ZoomReset();
                }
                graphView.NewDataOnlySpan = double.Parse(newDataOnlySpan.Text);
                graphView.NewDataOnlyMode = newestDataOnly;
            }

            if (!newestDataOnly)
            {
                zoomInButton_Click(sender, e);
            }

            zoomFromText.Enabled = !newestDataOnly;
            zoomToText.Enabled = !newestDataOnly;
 
        }



        /// <summary>
        /// 軸マスター編集クリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void axisMasterMenu_Click(object sender, EventArgs e)
        {
            Tracer.WriteInformation("軸マスター設定メニュークリック");

            try
            {
                AxisMasterForm axisMasterForm = new AxisMasterForm();
                DialogResult result = axisMasterForm.ShowDialog();

                //if (axisMasterForm.HasEdited)
                if (result == DialogResult.OK)
                {
                    AxisBean[] axisBeans = new AxisBean[] { chart1Axis, chart2Axis, chart3Axis };

                    for (int i = 0; i < graphViews.Length; i++)
                    {
                        GraphView graphView = graphViews[i];
                        AxisBean axis = axisBeans[i];

                        if (graphView != null && axis != null)
                        {
                            graphView.YAxisColorChanged(axis);
                            graphView.YAxisGridChanged(axis);
                            graphView.YAxisNameChanged(axis);
                            graphView.YAxisRangeChanged(axis);
                            graphView.Title = string.Format("Chart{0} : {1} {2}", i + 1, axis.Name, axis.UnitName.Equals("") ? "" : "(" + axis.UnitName + ")");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
                MessageBox.Show(ex.Message, "Graph Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// グラフ設定ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void graphSettingButton_Click(object sender, EventArgs e)
        {
            GraphSettingForm settingForm = new GraphSettingForm();

            if (settingForm.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                this.graphSettings = settingForm.graphSettings;

                List<ItemBean>[] itemLists = new List<ItemBean>[] { chart1Items, chart2Items, chart3Items };

                for (int i = 0; i < graphViews.Length; i++)
                {
                    GraphView graphView = graphViews[i];
                    List<ItemBean> itemList = itemLists[i];

                    graphView.GraphBackColor = settingForm.GraphBackColor;
                    graphView.Gradation = settingForm.Gradation;
                    graphView.YAxisTitleFontSize = settingForm.AxisFontSize;
                    graphView.YAxisScaleFontSize = settingForm.AxisFontSize;
                    graphView.XAxisTitleFontSize = settingForm.AxisFontSize;
                    graphView.XAxisScaleFontSize = settingForm.AxisFontSize;
                    graphView.TitleFontSize = settingForm.TitleFontSize;
                    graphView.TitleColor = settingForm.TitleColor;

                    for (int j = 0; j < itemList.Count; j++)
                    {
                        itemList[j].LineWidth = settingForm.LineWidth;
                        graphView.ItemWidthChanged(itemList[j]);
                    }
                }
            }
        }

        /// <summary>
        /// X軸ZOOM範囲テキストBOXの入力制限
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void zoomRange_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 入力可能文字 
            if ((e.KeyChar < '0' || e.KeyChar > '9') && e.KeyChar != '\b' && e.KeyChar != '.' && e.KeyChar != '-')
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// X軸ZOOM範囲テキストBOXのValidate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void zoomRange_Validating(object sender, CancelEventArgs e)
        {
            double dblValue;
            ToolStripTextBox zText = (ToolStripTextBox)sender;

            string colName = "X Axis zoom min";
            if (zText.Name == "zoomToText")
            {
                colName = "X Axis zoom max";
            }
            else
            {
                colName = "window size";
            }

            // null？
            if (string.IsNullOrEmpty(zText.Text))
            {
                MessageBox.Show(string.Format("must input {0} ", colName), "Graph Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }

            // doubleに変換可能か
            else if (!double.TryParse(zText.Text, out dblValue))
            {
                MessageBox.Show(string.Format("illegal format of {0}.[{1}]", colName, zText.Text), "Graph Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        /// <summary>
        /// リサイズ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GraphControl_Resize(object sender, EventArgs e)
        {
            this.toolStrip1.Width = this.Width;
        }

        /// <summary>
        /// グラフ位置変更通知関数設定
        /// </summary>
        /// <param name="method"></param>
        public void SetChangeGraphPointMethod(GraphView.ChangeGraphPointHandler method)
        {
            for (int index=0 ; index<this.graphViews.Length; index++)
            {
                this.graphViews[index].GraphNumber = index;
                this.graphViews[index].ChangeGraphPointEvent += method;
            }
        }

        /// <summary>
        /// グラフスケール変更
        /// </summary>
        /// <param name="graphNumber"></param>
        /// <param name="axisXMin"></param>
        /// <param name="axisXMax"></param>
        public void ChangeXAxisGraphScale(double axisXScaleMin, double axisXScaleMax, double axisXScrollMin, double axisXScrollMax)
        {
            for (int index = 0; index < this.graphViews.Length; index++)
            {
                //this.graphViews[index].ZoomReset();
                //this.graphViews[index].ZoomXRange(axisXMin, axisXMax);

                this.graphViews[index].ChangeXAxisGraphScale(axisXScaleMin, axisXScaleMax, axisXScrollMin, axisXScrollMax);
            }
        }

        /// <summary>
        /// Ｘ軸グラフスケール取得
        /// </summary>
        public void GetXAxisGraphScale(ref double axisXScaleMin, ref double axisXScaleMax)
        {
            double graphRange = 0, min = 0, max = 0;
            for (int graphIndex = 0; graphIndex < this.graphViews.Length; graphIndex++)
            {
                this.graphViews[graphIndex].GetXAxisGraphScale(ref min, ref max);

                if (graphRange < (max - min))
                {
                    axisXScaleMin = min;
                    axisXScaleMax = max;
                }
            }
        }
    }
}
