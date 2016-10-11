using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using ZedGraph;
using System.Diagnostics;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// グラフ表示のユーザーコントロール
    /// </summary>
    public partial class GraphView : UserControl
    {
        /// <summary>
        /// 初期化済みフラグ
        /// </summary>
        private bool isInitialize = false;

        /// <summary>
        /// 項目ラベルVisible
        /// </summary>
        private bool itemLabelVisible = false;

        /// <summary>
        /// X軸
        /// </summary>
        private AxisBean xAxis;

        /// <summary>
        /// Y軸リスト
        /// </summary>
        private List<AxisBean> yAxisList;

        /// <summary>
        /// 項目リスト
        /// </summary>
        private List<ItemBean> itemList;

        /// <summary>
        /// データリスト
        /// </summary>
        private List<MeasureBean> data;

        /// <summary>
        /// 描画済みのデータ数
        /// </summary>
        private int lastDataCount = 0;


        /// <summary>
        /// グラフタイトル
        /// </summary>
        private string title;
        public string Title
        {
            get { return this.title; }
            set
            {
                this.title = value;
                this.DispTitle();
            }
        }

        /// <summary>
        /// グラフタイトルフォントサイズ
        /// </summary>
        private int titleFontSize = 22;
        public int TitleFontSize
        {
            get { return this.titleFontSize; }
            set
            {
                this.titleFontSize = value;
                if (!this.isInitialize || this.itemList == null)
                {
                    return;
                }

                this.zedGraph.GraphPane.Title.FontSpec.Size = this.titleFontSize;
                this.zedGraph.AxisChange();
                this.zedGraph.Invalidate();

            }
        }

        /// <summary>
        /// グラフタイトルフォントサイズ
        /// </summary>
        private Color titleColor = Color.DarkGray;
        public Color TitleColor
        {
            get { return this.titleColor; }
            set
            {
                this.titleColor = value;
                if (!this.isInitialize || this.itemList == null)
                {
                    return;
                }

                this.zedGraph.GraphPane.Title.FontSpec.FontColor = this.titleColor;
                this.zedGraph.AxisChange();
                this.zedGraph.Invalidate();
            }
        }

        /// <summary>
        /// Y軸タイトルフォントサイズ
        /// </summary>
        private int yAxisTitleFontSize = 22;
        public int YAxisTitleFontSize 
        {
            get { return this.yAxisTitleFontSize; }
            set
            {
                this.yAxisTitleFontSize = value;
                if (!this.isInitialize || this.itemList == null)
                {
                    return;
                }

                foreach (AxisBean axis in this.yAxisList)
                {
                    this.zedGraph.GraphPane.YAxisList[axis.YAxisIndex].Title.FontSpec.Size = this.yAxisTitleFontSize;
                }
                this.zedGraph.AxisChange();
                this.zedGraph.Invalidate();
            }
        }

        /// <summary>
        /// Y軸目盛りフォントサイズ
        /// </summary>
        private int yAxisScaleFontSize = 22;
        public int YAxisScaleFontSize
        {
            get { return this.yAxisScaleFontSize; }
            set
            {
                this.yAxisScaleFontSize = value;
                if (!this.isInitialize || this.itemList == null)
                {
                    return;
                }

                foreach (AxisBean axis in this.yAxisList)
                {
                    if (!axis.IsY2Axis)
                    {
                        this.zedGraph.GraphPane.YAxisList[axis.YAxisIndex].Scale.FontSpec.Size = this.yAxisScaleFontSize;
                    }
                    else
                    {
                        this.zedGraph.GraphPane.Y2AxisList[axis.YAxisIndex].Scale.FontSpec.Size = this.yAxisScaleFontSize;
                    }
                }
                this.zedGraph.AxisChange();
                this.zedGraph.Invalidate();
            }
        }

        /// <summary>
        /// X軸タイトルフォントサイズ
        /// </summary>
        private int xAxisTitleFontSize = 22;
        public int XAxisTitleFontSize
        {
            get { return this.xAxisTitleFontSize; }
            set
            {
                this.xAxisTitleFontSize = value;
                if (!this.isInitialize || this.itemList == null)
                {
                    return;
                }

                this.zedGraph.GraphPane.XAxis.Title.FontSpec.Size = this.xAxisTitleFontSize;
            }
        }

        /// <summary>
        /// X軸目盛りフォントサイズ
        /// </summary>
        private int xAxisScaleFontSize = 22;
        public int XAxisScaleFontSize
        {
            get { return this.xAxisScaleFontSize; }
            set
            {
                this.xAxisScaleFontSize = value;
                if (!this.isInitialize || this.itemList == null)
                {
                    return;
                }

                this.zedGraph.GraphPane.XAxis.Scale.FontSpec.Size = this.xAxisScaleFontSize;
            }
        }

        /// <summary>
        /// 項目ラベルフォントサイズ
        /// </summary>
        private int dataLabelFontSize = 22;
        public int DataLabelFontSize
        {
            get { return this.dataLabelFontSize; }
            set
            {
                this.dataLabelFontSize = value;
                if (!this.isInitialize || this.itemList == null)
                {
                    return;
                }

                if (this.zedGraph.GraphPane.CurveList != null && this.zedGraph.GraphPane.CurveList.Count == this.itemList.Count)
                {
                    for (int i = 0; i < this.itemList.Count; i++)
                    {
                        ItemBean item = this.itemList[i];
                        CurveItem curve = this.zedGraph.GraphPane.CurveList[i];

                        curve.Label.FontSpec.Size = this.dataLabelFontSize;
                    }

                    this.zedGraph.AxisChange();
                    this.zedGraph.Invalidate();
                }
            }
        }

        /// <summary>
        /// ZOOM倍率
        /// </summary>
        public double ZoomFraction { get; set; }

        /// <summary>
        /// グラフ背景色
        /// </summary>
        //private Color graphBackColor = Color.FromArgb(255, 255, 200);
        private Color graphBackColor = Color.Silver;
        public Color GraphBackColor
        {
            get
            {
                return this.graphBackColor;
            }
            set
            {
                this.graphBackColor = value;
                if (this.isInitialize)
                {
                    Color color1 = (this.gradation) ? Color.White : this.graphBackColor;
                    Color color2 = this.graphBackColor;
                    this.zedGraph.GraphPane.Chart.Fill = new Fill(color1, color2, 45.0F);
                }
            }
        }

        /// <summary>
        /// グラフ背景のグラデーション有無
        /// </summary>
        private bool gradation = true;
        public bool Gradation
        {
            get
            {
                return this.gradation;
            }
            set
            {
                this.gradation = value;
                if (this.isInitialize)
                {
                    Color color1 = (this.gradation) ? Color.White : this.graphBackColor;
                    Color color2 = this.graphBackColor;
                    this.zedGraph.GraphPane.Chart.Fill = new Fill(color1, color2, 45.0F);
                }
            }
        }


        /// <summary>
        /// データのX軸最小値
        /// </summary>
        public double DataXmin
        {
            get
            {
                if (!this.isInitialize || this.data == null || this.data.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return this.data[0].X;
                }
            }
        }

        /// <summary>
        /// データのX軸最大値
        /// </summary>
        public double DataXmax
        {
            get
            {
                if (!this.isInitialize || this.data == null || this.data.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return this.data.Last().X;
                }
            }
        }

        /// <summary>
        /// 最新データのみ設定モードにする
        /// zoom　Resetで解除
        /// </summary>
        /// <param name="timeSpan"></param>
        private bool newDataOnlyMode = false;
        public bool NewDataOnlyMode
        {
            get { return this.newDataOnlyMode; }
            set
            {
                if (this.newDataOnlyMode != value)
                {
                    if (value)
                    {
                        // MajorStepをAUTOにする
                        this.zedGraph.GraphPane.XAxis.Scale.MajorStepAuto = true;

                        // MinorTickを表示
                        this.zedGraph.GraphPane.XAxis.MinorGrid.IsVisible = true;

                        this.zedGraph.AxisChange();
                    }
                    else
                    {
                        this.ZoomReset();
                    }
                }
                this.newDataOnlyMode = value;
            }
        }

        /// <summary>
        /// 最新値のみ表示する時の表示幅
        /// </summary>
        private double newDataOnlySpan = 0.3;
        public double NewDataOnlySpan
        {
            get { return this.newDataOnlySpan; }
            set
            {
                this.newDataOnlySpan = value;

                this.xAxis.AxisMax = value * 1000;
            }
        }

        /// <summary>
        /// 表示範囲を過ぎたデータはメモリから破棄するモード
        /// </summary>
        /// <param name="timeSpan"></param>
        private bool dataEraseMode = false;
        public bool DataEraseMode
        {
            get { return this.dataEraseMode; }
            set
            {
                this.dataEraseMode = value;

                this.zedGraph.IsShowPointValues = !dataEraseMode;

                //this.zedGraph.IsEnableHPan = !dataEraseMode;
                //this.zedGraph.IsEnableVPan = !dataEraseMode;
                //this.zedGraph.IsEnableHZoom = !dataEraseMode;
                //this.zedGraph.IsEnableVZoom = !dataEraseMode;
                //this.zedGraph.IsEnableWheelZoom = !dataEraseMode;
            }
        }

        /// <summary>
        /// グラフ操作の有効／無効
        /// </summary>
        private bool graphOperation = true;
        public bool GraphOperation
        {
            get { return this.graphOperation; }
            set
            {
                this.graphOperation = value;

                //this.zedGraph.IsShowPointValues = value;
                this.zedGraph.IsEnableHPan = value;
                this.zedGraph.IsEnableVPan = value;
                this.zedGraph.IsEnableHZoom = value;
                this.zedGraph.IsEnableVZoom = value;
                this.zedGraph.IsEnableWheelZoom = value;
            }
        }
        
        /// <summary>
        /// 垂直スクロールバーの表示／非表示
        /// </summary>
        public bool ShowVerticalScrollBar
        {
            get { return this.zedGraph.IsShowVScrollBar; }
            set { this.zedGraph.IsShowVScrollBar = value; }
        }

        /// <summary>
        /// 水平スクロールバーの表示／非表示
        /// </summary>
        public bool ShowHorizontalScrollBar
        {
            get { return this.zedGraph.IsShowHScrollBar; }
            set { this.zedGraph.IsShowHScrollBar = value; }
        }

        /// <summary>
        /// マウスズームの有効／無効
        /// </summary>
        private bool mouseZoomEnabled = true;
        public bool MouseZoomEnabled
        {
            get { return this.mouseZoomEnabled; }
            set
            {
                this.mouseZoomEnabled = value;

                if (this.isInitialize)
                {
                    this.zedGraph.IsEnableWheelZoom = this.mouseZoomEnabled;
                }

            }
        }


        /// <summary>
        /// グラフ位置変更通知
        /// </summary>
        /// <param name="status"></param>
        public delegate void ChangeGraphPointHandler(int graphNumber, double axisXMin, double axisXMax);
        /// <summary>
        /// グラフ位置変更通知イベント
        /// </summary>
        public event ChangeGraphPointHandler ChangeGraphPointEvent = null;

        /// <summary>
        /// グラフ番号（識別用）
        /// </summary>
        public int GraphNumber { get; set; }

        /// <summary>
        /// Ｘ軸グラフスケール変更
        /// </summary>
        /// <param name="axisXScaleMin"></param>
        /// <param name="axisXScaleMax"></param>
        /// <param name="axisXScrollMin"></param>
        /// <param name="axisXScrollMax"></param>
        public void ChangeXAxisGraphScale(double axisXScaleMin, double axisXScaleMax, double axisXScrollMin, double axisXScrollMax)
        {
            this.zedGraph.GraphPane.XAxis.Scale.Min = axisXScaleMin;
            this.zedGraph.GraphPane.XAxis.Scale.Max = axisXScaleMax;
            this.zedGraph.ScrollMinX = axisXScrollMin;
            this.zedGraph.ScrollMaxX = axisXScrollMax;

            // グラフを再描画させる
            this.zedGraph.Invalidate();
        }

        /// <summary>
        /// Ｘ軸グラフスケール取得
        /// </summary>
        public void GetXAxisGraphScale(ref double axisXScaleMin, ref double axisXScaleMax)
        {
            axisXScaleMin = this.zedGraph.GraphPane.XAxis.Scale.Min;
            axisXScaleMax = this.zedGraph.GraphPane.XAxis.Scale.Max;
        }

        /// <summary>
        /// 前回マーキングデータ有無
        /// </summary>
        private bool getPreMarkingData = false;



        /// <summary>
        /// コンストラクタ
        /// </summary>
        public GraphView()
        {
            InitializeComponent();

            this.ZoomFraction = 0.1;

            this.zedGraph.Visible = false;

            this.xAxis = new AxisBean();
            this.xAxis.GridLineVisible = true;
            this.xAxis.GridResolution = 60 * 1000;
            this.xAxis.AxisMax = 10 * 1000;
            this.xAxis.AxisMin = 0;

            this.itemControl1.ItemColorChanged += ItemColorChanged;
            this.itemControl1.ItemVisibleChanged += ItemVisibleChanged;
        }

        /// <summary>
        /// 項目リスト設定
        /// 項目リストからY軸を抽出し、
        /// X軸、Y軸の初期化を行う。
        /// </summary>
        /// <param name="this.itemList"></param>
        /// <param name="clearData"></param>
        public void SetItems(List<ItemBean> itemList, bool clearData = true)
        {
            this.Initialize();

            this.itemList = itemList;

            this.itemControl1.SetItems(this.itemList);

            this.Title = this.title;

            // 軸を抽出
            List<AxisBean> alist = ItemListAccess.SelectAxisList(this.itemList);

            // 表示順でソート
            this.yAxisList = alist.OrderBy(d => d.DispOrder).ToList();

            if (clearData)
            {
                this.data = null;
            }

            this.lastDataCount = 0;
            this.zedGraph.GraphPane.CurveList.Clear();
            this.zedGraph.GraphPane.YAxisList.Clear();
            this.zedGraph.GraphPane.Y2AxisList.Clear();


            // 最新データのみ表示モードリセット
            //this.newDataOnlyMode = false;

            this.zedGraph.GraphPane.Title.FontSpec.Size = this.titleFontSize;
            this.zedGraph.GraphPane.Title.FontSpec.FontColor = this.titleColor;

            // X軸設定
            this.zedGraph.GraphPane.XAxis.Title.Text = "Seconds";
            this.zedGraph.GraphPane.XAxis.MajorGrid.IsVisible = this.xAxis.GridLineVisible;
            this.zedGraph.GraphPane.XAxis.Scale.MajorStep = this.xAxis.GridResolution / 1000.0;
            this.zedGraph.GraphPane.XAxis.Title.FontSpec.Size = this.XAxisTitleFontSize;
            this.zedGraph.GraphPane.XAxis.Scale.FontSpec.Size = this.XAxisScaleFontSize;
            this.zedGraph.GraphPane.XAxis.Title.FontSpec.FontColor = this.xAxis.AxisColor;
            this.zedGraph.GraphPane.XAxis.Scale.FontSpec.FontColor = this.xAxis.AxisColor;
            this.zedGraph.GraphPane.XAxis.MajorTic.Color = this.xAxis.AxisColor;
            this.zedGraph.GraphPane.XAxis.MinorTic.Color = this.xAxis.AxisColor;
            this.zedGraph.GraphPane.XAxis.Scale.MagAuto = false;
            this.zedGraph.GraphPane.XAxis.Scale.MinorStepAuto = true;
            this.zedGraph.GraphPane.XAxis.Scale.MajorStepAuto = true;
            this.zedGraph.GraphPane.XAxis.MinorGrid.IsVisible = true;
            this.zedGraph.GraphPane.XAxis.Title.IsVisible = true;

            // minorグリッドの線をmajorグリッドと同じに
            this.zedGraph.GraphPane.XAxis.MinorGrid.Color = this.zedGraph.GraphPane.XAxis.MajorGrid.Color;
            this.zedGraph.GraphPane.XAxis.MinorGrid.DashOn = this.zedGraph.GraphPane.XAxis.MajorGrid.DashOn;
            this.zedGraph.GraphPane.XAxis.MinorGrid.DashOff = this.zedGraph.GraphPane.XAxis.MajorGrid.DashOff;

            // X軸レンジ設定
            this.zedGraph.GraphPane.XAxis.Scale.Min = this.xAxis.AxisMin / 1000.0;
            this.zedGraph.GraphPane.XAxis.Scale.Max = this.xAxis.AxisMax / 1000.0;
            this.zedGraph.ScrollMinX = this.xAxis.AxisMin / 1000.0;
            this.zedGraph.ScrollMaxX = this.xAxis.AxisMax / 1000.0;

            // Y軸の設定
            int yAxisIndex = 0;
            int y2AxisIndex = 0;

            this.zedGraph.YScrollRangeList.Clear();
            this.zedGraph.Y2ScrollRangeList.Clear();

            for (int i = 0; i < this.yAxisList.Count; i++)
            {
                AxisBean axis = this.yAxisList[i];

                // 軸の表示・非表示を決める
                bool isVisible = ItemListAccess.IsAxisVisible(this.itemList, axis);

                // 軸設定に従い左右に振る
                if (!axis.IsY2Axis)
                {
                    if (yAxisIndex == 0)
                    {
                        //this.zedGraph.GraphPane.YAxis.Title.CommentText = string.Format("{0}({1})", axis.Name, axis.UnitName);
                        //this.zedGraph.GraphPane.AddYAxis(string.Format("{0}{1}", axis.Name, axis.UnitName.Equals("") ? "" : "(" + axis.UnitName + ")"));
                        this.zedGraph.GraphPane.AddYAxis(string.Format("{0}",axis.UnitName));
                    }
                    else
                    {
                        this.zedGraph.GraphPane.YAxisList.Add(string.Format("{0}{1}", axis.Name, axis.UnitName.Equals("") ? "" : "(" + axis.UnitName + ")"));
                    }
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Scale.Min = axis.AxisMin;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Scale.Max = axis.AxisMax;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].MajorTic.IsOpposite = false;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].MinorTic.IsOpposite = false;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].IsVisible = true;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Title.FontSpec.Size = this.YAxisTitleFontSize;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Scale.FontSpec.Size = this.YAxisScaleFontSize;

                    // TODO : Ｙ軸スケール自動化（桁が大きくなると、指数表示になってしまう）
                    //this.zedGraph.GraphPane.YAxisList[yAxisIndex].Scale.MagAuto = true;
                    // TODO : Ｙ軸スケールを手動変更
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Scale.MagAuto = false;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Scale.MajorStep = axis.GridResolution;
                    
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].MajorGrid.IsVisible = axis.GridLineVisible;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].IsVisible = isVisible;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Title.Gap = 0.1F;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Scale.LabelGap = 0.1F;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Title.IsVisible = true;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].MajorGrid.Color = axis.AxisColor;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].MajorGrid.DashOn = 2;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].MajorGrid.DashOff = 1;

                    // 今回はY軸は必ず1軸でX軸の色もY軸にあわせる
                    this.zedGraph.GraphPane.XAxis.MajorGrid.Color = axis.AxisColor;

                    // TODO : 単位タイトルカラーを固定変更
                    //this.zedGraph.GraphPane.XAxis.Title.FontSpec.FontColor = axis.AxisColor;
                    this.zedGraph.GraphPane.XAxis.Title.FontSpec.FontColor = Color.Red;
                    
                    this.zedGraph.GraphPane.XAxis.Scale.FontSpec.FontColor = axis.AxisColor;

                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Color = axis.AxisColor;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Scale.FontSpec.FontColor = axis.AxisColor;

                    // TODO : 単位タイトルカラーを固定変更
                    //this.zedGraph.GraphPane.YAxisList[yAxisIndex].Title.FontSpec.FontColor = axis.AxisColor;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Title.FontSpec.FontColor = Color.Red;
                    
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].MajorTic.Color = axis.AxisColor;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].MinorTic.Color = axis.AxisColor;

                    this.zedGraph.YScrollRangeList.Add(new ScrollRange(axis.AxisMin,axis.AxisMax,true));

                    // 表示軸情報を保存
                    axis.YAxisIndex = yAxisIndex;

                    yAxisIndex++;
                }
                else
                {
                    if (y2AxisIndex == 0)
                    {
                        this.zedGraph.GraphPane.AddY2Axis(string.Format("{0}{1}", axis.Name, axis.UnitName.Equals("") ? "" : "(" + axis.UnitName + ")"));
                    }
                    else
                    {
                        this.zedGraph.GraphPane.Y2AxisList.Add(string.Format("{0}{1}", axis.Name, axis.UnitName.Equals("") ? "" : "(" + axis.UnitName + ")"));
                    }
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Scale.Min = axis.AxisMin;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Scale.Max = axis.AxisMax;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].MajorTic.IsOpposite = false;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].MinorTic.IsOpposite = false;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].IsVisible = true;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Title.FontSpec.Size = this.YAxisTitleFontSize;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Scale.FontSpec.Size = this.YAxisScaleFontSize;

                    // TODO : Ｙ軸スケール自動化（桁が大きくなると、指数表示になってしまう）
                    //this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Scale.MagAuto = true;
                    // TODO : Ｙ軸スケールを手動変更
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Scale.MagAuto = false;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Scale.MajorStep = axis.GridResolution;
                    
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].MajorGrid.IsVisible = axis.GridLineVisible;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].IsVisible = isVisible;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Title.Gap = 0.1F;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Scale.LabelGap = 0.1F;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Title.IsVisible = false;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].MajorGrid.Color = axis.AxisColor;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].MajorGrid.DashOn = 2;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].MajorGrid.DashOff = 1;

                    // 今回はY軸は必ず1軸でX軸の色もY軸にあわせる
                    this.zedGraph.GraphPane.XAxis.MajorGrid.Color = axis.AxisColor;
                    this.zedGraph.GraphPane.XAxis.Title.FontSpec.FontColor = axis.AxisColor;
                    this.zedGraph.GraphPane.XAxis.Scale.FontSpec.FontColor = axis.AxisColor;

                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Color = axis.AxisColor;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Scale.FontSpec.FontColor = axis.AxisColor;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].Title.FontSpec.FontColor = axis.AxisColor;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].MajorTic.Color = axis.AxisColor;
                    this.zedGraph.GraphPane.Y2AxisList[y2AxisIndex].MinorTic.Color = axis.AxisColor;

                    this.zedGraph.Y2ScrollRangeList.Add(new ScrollRange(axis.AxisMin, axis.AxisMax, true));

                    // 表示軸情報を保存
                    axis.YAxisIndex = y2AxisIndex;

                    y2AxisIndex++;
                }
            }

            if (yAxisIndex == 0)
            {
                // 軸がひとつも無いようだと落ちるので、ダミー軸追加
                this.zedGraph.GraphPane.AddYAxis("default");
                this.zedGraph.GraphPane.YAxis.IsVisible = false;
            }

            if (y2AxisIndex == 0)
            {
                // 軸がひとつも無いようだと落ちるので、ダミー軸追加
                this.zedGraph.GraphPane.AddY2Axis("default");
                this.zedGraph.GraphPane.Y2Axis.IsVisible = false;
            }
            this.zedGraph.AxisChange();


        }


        /// <summary>
        /// 削除する範囲を表示する
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void ShowDeleteRange(double from, double to)
        {
            BoxObj box = new BoxObj(from, this.zedGraph.GraphPane.YAxis.Scale.Max, from - to, this.zedGraph.GraphPane.YAxis.Scale.Max - this.zedGraph.GraphPane.YAxis.Scale.Min);
            box.Fill.Color = Color.FromArgb(128,Color.LightSteelBlue);
            box.ZOrder = ZOrder.E_BehindCurves;

            this.zedGraph.GraphPane.GraphObjList.Add(box);

            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();
        }

        /// <summary>
        /// 削除する範囲を消去する
        /// </summary>
        public void HideDeleteRange()
        {
            // グラフオブジェクトを消去する
            this.zedGraph.GraphPane.GraphObjList.Clear();

            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();
        }


        /// <summary>
        /// グラフ描画
        /// SetItemsでいったんクリアする。
        /// その後データが増えるごとに呼ぶと、増えた分だけ描画する。
        /// </summary>
        /// <param name="axis"></param>
        public void Draw(List<MeasureBean> data)
        {
            this.data = data;

            this.draw();
        }

        /// <summary>
        /// データクリア
        /// </summary>
        public void Clear()
        {
            this.data = null;

            this.lastDataCount = 0;

            this.zedGraph.GraphPane.CurveList.Clear();
            this.zedGraph.GraphPane.GraphObjList.Clear();

            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();
        }

        /// <summary>
        /// スクロールリセット
        /// </summary>
        public void ScrollReset()
        {
            // 
            this.zedGraph.GraphPane.YAxis.Scale.Max = itemList[0].YAxisMax;
            this.zedGraph.GraphPane.YAxis.Scale.Min = itemList[0].YAxisMin;

            this.zedGraph.GraphPane.XAxis.Scale.Max = this.newDataOnlySpan * 1.1;
            this.zedGraph.GraphPane.XAxis.Scale.Min = 0;


            // Ｘ軸を初期位置移動
            double range = this.zedGraph.GraphPane.XAxis.Scale.Max - this.zedGraph.GraphPane.XAxis.Scale.Min;
            this.zedGraph.GraphPane.XAxis.Scale.Min = 0;
            this.zedGraph.GraphPane.XAxis.Scale.Max = range;
            this.zedGraph.ScrollMinX = 0;
            this.zedGraph.ScrollMaxX = range;

            // Ｙ軸を初期位置移動
            range = this.zedGraph.GraphPane.YAxis.Scale.Max - this.zedGraph.GraphPane.YAxis.Scale.Min;
            this.zedGraph.GraphPane.YAxis.Scale.Min = 0 - range / 2;
            this.zedGraph.GraphPane.YAxis.Scale.Max = range / 2;
            this.zedGraph.ScrollMinY = 0 - range / 2;
            this.zedGraph.ScrollMaxY = range / 2;

            // TODO : Ｙ軸目盛りリセット
            this.zedGraph.GraphPane.YAxis.Scale.MajorStep = itemList[0].Axis.GridResolution;

            this.getPreMarkingData = false;

            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();
        }

        /// <summary>
        /// 項目色変更
        /// </summary>
        /// <param name="item"></param>
        public void ItemColorChanged(ItemBean item)
        {
            if (!this.isInitialize || this.itemList == null)
            {
                return;
            }

            if (this.zedGraph.GraphPane.CurveList != null && this.zedGraph.GraphPane.CurveList.Count > 0)
            {

                int index = this.itemList.FindIndex(d => d.Id == item.Id);

                this.zedGraph.GraphPane.CurveList[index].Color = item.LineColor;

                this.zedGraph.AxisChange();
                this.zedGraph.Invalidate();
            }
        }

        /// <summary>
        /// 項目の表示・非表示変更
        /// </summary>
        /// <param name="item"></param>
        public void ItemVisibleChanged(ItemBean item)
        {
            if (!this.isInitialize || this.itemList == null)
            {
                return;
            }

            if (this.zedGraph.GraphPane.CurveList != null && this.zedGraph.GraphPane.CurveList.Count > 0)
            {
                // 折れ線の表示・非表示
                int index = this.itemList.FindIndex(d => d.Id == item.Id);

                this.zedGraph.GraphPane.CurveList[index].IsVisible = item.Visible;
                this.zedGraph.GraphPane.CurveList[index].Label.IsVisible = (this.itemLabelVisible & item.Visible);

                // 対応する軸の表示・非表示
                AxisBean axis = this.FindAxis(item);
                bool isVisible = ItemListAccess.IsAxisVisible(this.itemList, axis);

                // 他に使っているitemがなければ非表示にする
                if (!axis.IsY2Axis)
                {
                    this.zedGraph.GraphPane.YAxisList[axis.YAxisIndex].IsVisible = isVisible;
                }
                else
                {
                    this.zedGraph.GraphPane.Y2AxisList[axis.YAxisIndex].IsVisible = isVisible;
                }


                this.zedGraph.AxisChange();
                this.zedGraph.Invalidate();
            }
        }

        /// <summary>
        /// 項目の線幅変更
        /// </summary>
        /// <param name="item"></param>
        public void ItemWidthChanged(ItemBean item)
        {
            if (!this.isInitialize || this.itemList == null)
            {
                return;
            }

            if (this.zedGraph.GraphPane.CurveList != null && this.zedGraph.GraphPane.CurveList.Count > 0)
            {
                int index = this.itemList.FindIndex(d => d.Id == item.Id);

                LineItem line = (LineItem)this.zedGraph.GraphPane.CurveList[index];
                line.Line.Width = (float)item.LineWidth;

                this.zedGraph.AxisChange();
                this.zedGraph.Invalidate();
            }
        }

        /// <summary>
        /// 項目毎のレンジ変更
        /// 軸を再抽出する
        /// </summary>
        /// <param name="item"></param>
        public void ItemRangeChanged(ItemBean item)
        {
            if (!this.isInitialize || this.itemList == null)
            {
                return;
            }

            this.SetItems(this.itemList, false);

            this.draw();
        }

        /// <summary>
        /// 項目の名称変更
        /// </summary>
        /// <param name="item"></param>
        public void ItemNameChanged(ItemBean item)
        {
            if (!this.isInitialize || this.itemList == null)
            {
                return;
            }

            if (this.zedGraph.GraphPane.CurveList != null && this.zedGraph.GraphPane.CurveList.Count > 0)
            {
                int index = this.itemList.FindIndex(d => d.Id == item.Id);

                LineItem line = (LineItem)this.zedGraph.GraphPane.CurveList[index];
                line.Label.Text = item.Name;

                this.zedGraph.AxisChange();
                this.zedGraph.Invalidate();
            }
        }


        /// <summary>
        /// 軸一括レンジ変更
        /// </summary>
        /// <param name="axis"></param>
        public void YAxisRangeChanged(AxisBean axis)
        {
            if (!this.isInitialize || this.itemList == null)
            {
                return;
            }

            for (int i = 0; i < this.itemList.Count; i++)
            {
                if (this.itemList[i].Axis.Id == axis.Id)
                {
                    this.itemList[i].YAxisMax = axis.AxisMax;
                    this.itemList[i].YAxisMin = axis.AxisMin;

                    int yAxisIndex = this.itemList[i].Axis.YAxisIndex;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Scale.Min = axis.AxisMin;
                    this.zedGraph.GraphPane.YAxisList[yAxisIndex].Scale.Max = axis.AxisMax;
                }
            }


            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();


            //SetItems(this.this.itemList, false);

            //draw();
        }

        /// <summary>
        /// Y軸グリッド変更
        /// </summary>
        /// <param name="axis"></param>
        public void YAxisGridChanged(AxisBean axis)
        {
            if (!this.isInitialize || this.yAxisList == null)
            {
                return;
            }

            AxisBean a = this.yAxisList.Find(d => d.Id == axis.Id);

            if (!a.IsY2Axis)
            {
                this.zedGraph.GraphPane.YAxisList[a.YAxisIndex].Scale.MajorStep = axis.GridResolution;
                this.zedGraph.GraphPane.YAxisList[a.YAxisIndex].MajorGrid.IsVisible = axis.GridLineVisible;
            }
            else
            {
                this.zedGraph.GraphPane.Y2AxisList[a.YAxisIndex].Scale.MajorStep = axis.GridResolution;
                this.zedGraph.GraphPane.Y2AxisList[a.YAxisIndex].MajorGrid.IsVisible = axis.GridLineVisible;
            }
            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();

        }

        /// <summary>
        /// Y軸名称変更
        /// </summary>
        /// <param name="axis"></param>
        public void YAxisNameChanged(AxisBean axis)
        {
            if (!this.isInitialize || this.yAxisList == null)
            {
                return;
            }

            AxisBean a = this.yAxisList.Find(d => d.Id == axis.Id);

            if (!a.IsY2Axis)
            {
                this.zedGraph.GraphPane.YAxisList[a.YAxisIndex].Title.Text = string.Format("{0}{1}", axis.Name, axis.UnitName.Equals("") ? "" : "(" + axis.UnitName + ")");
            }
            else
            {
                this.zedGraph.GraphPane.Y2AxisList[a.YAxisIndex].Title.Text = string.Format("{0}{1}", axis.Name, axis.UnitName.Equals("") ? "" : "(" + axis.UnitName + ")");
            }
            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();

        }

        /// <summary>
        /// Y軸色変更
        /// </summary>
        /// <param name="axis"></param>
        public void YAxisColorChanged(AxisBean axis)
        {
            if (!this.isInitialize || this.yAxisList == null)
            {
                return;
            }

            AxisBean a = this.yAxisList.Find(d => d.Id == axis.Id);

            if (!a.IsY2Axis)
            {
                this.zedGraph.GraphPane.YAxisList[a.YAxisIndex].Color = axis.AxisColor;
                this.zedGraph.GraphPane.YAxisList[a.YAxisIndex].Scale.FontSpec.FontColor = axis.AxisColor;
                this.zedGraph.GraphPane.YAxisList[a.YAxisIndex].Title.FontSpec.FontColor = axis.AxisColor;
                this.zedGraph.GraphPane.YAxisList[a.YAxisIndex].MajorTic.Color = axis.AxisColor;
                this.zedGraph.GraphPane.YAxisList[a.YAxisIndex].MinorTic.Color = axis.AxisColor;
                this.zedGraph.GraphPane.YAxisList[a.YAxisIndex].MajorGrid.Color = axis.AxisColor;
            }
            else
            {
                this.zedGraph.GraphPane.Y2AxisList[a.YAxisIndex].Color = axis.AxisColor;
                this.zedGraph.GraphPane.Y2AxisList[a.YAxisIndex].Scale.FontSpec.FontColor = axis.AxisColor;
                this.zedGraph.GraphPane.Y2AxisList[a.YAxisIndex].Title.FontSpec.FontColor = axis.AxisColor;
                this.zedGraph.GraphPane.Y2AxisList[a.YAxisIndex].MajorTic.Color = axis.AxisColor;
                this.zedGraph.GraphPane.Y2AxisList[a.YAxisIndex].MinorTic.Color = axis.AxisColor;
                this.zedGraph.GraphPane.Y2AxisList[a.YAxisIndex].MajorGrid.Color = axis.AxisColor;
            }

            // 今回はY軸は必ず1軸でX軸の色もY軸にあわせる
            this.zedGraph.GraphPane.XAxis.MajorGrid.Color = axis.AxisColor;
            this.zedGraph.GraphPane.XAxis.Title.FontSpec.FontColor = axis.AxisColor;
            this.zedGraph.GraphPane.XAxis.Scale.FontSpec.FontColor = axis.AxisColor;


            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();

        }

        /// <summary>
        /// Y軸の軸変更
        /// </summary>
        /// <param name="item"></param>
        public void YAxisAxisChanged(AxisBean axis)
        {
            if (!this.isInitialize || this.itemList == null)
            {
                return;
            }

            this.SetItems(this.itemList, false);

            this.draw();
        }


        /// <summary>
        /// X軸変更
        /// </summary>
        /// <param name="axis"></param>
        public void XAxisChanged(AxisBean axis)
        {
            if (!this.isInitialize)
            {
                return;
            }

            this.xAxis = axis;

            this.zedGraph.GraphPane.XAxis.Scale.MajorStep = axis.GridResolution / 1000.0;
            this.zedGraph.GraphPane.XAxis.MajorGrid.IsVisible = axis.GridLineVisible;

            this.zedGraph.ScrollMinX = axis.AxisMin / 1000.0;
            this.zedGraph.ScrollMaxX = axis.AxisMax / 1000.0;

            this.zedGraph.GraphPane.XAxis.Scale.Min = this.zedGraph.ScrollMinX;
            this.zedGraph.GraphPane.XAxis.Scale.Max = this.zedGraph.ScrollMaxX;

            this.zedGraph.GraphPane.XAxis.Title.FontSpec.FontColor = this.xAxis.AxisColor;
            this.zedGraph.GraphPane.XAxis.Scale.FontSpec.FontColor = this.xAxis.AxisColor;
            this.zedGraph.GraphPane.XAxis.MajorTic.Color = this.xAxis.AxisColor;
            this.zedGraph.GraphPane.XAxis.MinorTic.Color = this.xAxis.AxisColor;

            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();
        }



        /// <summary>
        /// X軸範囲指定拡大
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void ZoomXRange(double from, double to)
        {
            if (!this.isInitialize || this.itemList == null)
            {
                return;
            }

            this.zedGraph.GraphPane.XAxis.Scale.MajorStepAuto = true;

            // MinorTickを表示
            this.zedGraph.GraphPane.XAxis.MinorGrid.IsVisible = true;

             this.zedGraph.GraphPane.XAxis.Scale.Min = from;
            this.zedGraph.GraphPane.XAxis.Scale.Max = to;

            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();
        }

        /// <summary>
        /// zoom初期化
        /// </summary>
        public void ZoomReset()
        {
            if (!this.isInitialize || this.itemList == null)
            {
                return;
            }

            // MajorStepを設定しないとAUTOのままになる
            this.zedGraph.GraphPane.XAxis.Scale.MajorStep = this.xAxis.GridResolution / 1000.0;

            // MinorTickを表示
            this.zedGraph.GraphPane.XAxis.MinorGrid.IsVisible = false;

            // 最新データのみ表示モードリセット
            this.newDataOnlyMode = false;

            this.zedGraph.GraphPane.XAxis.Scale.Min = this.zedGraph.ScrollMinX;
            this.zedGraph.GraphPane.XAxis.Scale.Max = this.zedGraph.ScrollMaxX;

            foreach (AxisBean axis in this.yAxisList)
            {
                if (!axis.IsY2Axis)
                {
                    ScrollRange range = this.zedGraph.YScrollRangeList[axis.YAxisIndex];
                    this.zedGraph.GraphPane.YAxisList[axis.YAxisIndex].Scale.Min = range.Min;
                    this.zedGraph.GraphPane.YAxisList[axis.YAxisIndex].Scale.Max = range.Max;
                }
                else
                {
                    ScrollRange range = this.zedGraph.Y2ScrollRangeList[axis.YAxisIndex];
                    this.zedGraph.GraphPane.Y2AxisList[axis.YAxisIndex].Scale.Min = range.Min;
                    this.zedGraph.GraphPane.Y2AxisList[axis.YAxisIndex].Scale.Max = range.Max;
                }
            }

            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();
        }



        /// <summary>
        /// 項目名称の表示・非表示
        /// </summary>
        /// <param name="visible"></param>
        public void ItemLabelVisible(bool visible)
        {
            this.itemLabelVisible = visible;

            if (!this.isInitialize || this.itemList == null)
            {
                return;
            }

            if (this.zedGraph.GraphPane.CurveList != null)
            {
                for (int i = 0; i < this.itemList.Count; i++)
                {
                    ItemBean item = this.itemList[i];
                    CurveItem curve = this.zedGraph.GraphPane.CurveList[i];

                    curve.Label.IsVisible = (this.itemLabelVisible & item.Visible);
                }

                this.zedGraph.AxisChange();
                this.zedGraph.Invalidate();
            }
        }

        /// <summary>
        /// 画像をクリップボードにコピー
        /// </summary>
        public void CopyImage()
        {
            if (this.isInitialize)
            {
                this.zedGraph.Copy(true);
            }
        }

        /// <summary>
        /// ZOOMイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="oldState"></param>
        /// <param name="newState"></param>
        private void zedGraph_ZoomEvent(ZedGraphControl sender, ZoomState oldState, ZoomState newState)
        {
            //Tracer.WriteVerbose("{0}",newState.TypeString);

            if (newState.Type == ZoomState.StateType.WheelZoom)
            {
                // MajorStepをAUTOにする
                this.zedGraph.GraphPane.XAxis.Scale.MajorStepAuto = true;

                // MinorTickを表示
                this.zedGraph.GraphPane.XAxis.MinorGrid.IsVisible = true;


                this.debugWriteXMinMax();
            }
            else if (newState.Type == ZoomState.StateType.Pan)
            {
                this.debugWriteXMinMax();
            }

            if (this.dataEraseMode)
            {
                // Xが大きくなりすぎないように戻す
                if ((this.zedGraph.GraphPane.XAxis.Scale.Max - this.zedGraph.GraphPane.XAxis.Scale.Min) > this.newDataOnlySpan)
                {
                    this.zedGraph.GraphPane.XAxis.Scale.Min = this.zedGraph.GraphPane.XAxis.Scale.Max - this.newDataOnlySpan;
                    zedGraph.AxisChange();
                }
            }

            // 現在の表示高さ（Ｙ軸幅）を取得
            double yAxisRange = this.getYAxisRange();

            // Yは元の設定より大きくはしない
            if (this.zedGraph.GraphPane.YAxis.Scale.Max > itemList[0].YAxisMax)
            {
                this.zedGraph.GraphPane.YAxis.Scale.Max = itemList[0].YAxisMax;
                this.zedGraph.GraphPane.YAxis.Scale.Min = itemList[0].YAxisMax - yAxisRange;
                if (this.zedGraph.GraphPane.YAxis.Scale.Min < itemList[0].YAxisMin)
                {
                    this.zedGraph.GraphPane.YAxis.Scale.Min = itemList[0].YAxisMin;
                }
            }
            if (this.zedGraph.GraphPane.YAxis.Scale.Min < itemList[0].YAxisMin)
            {
                this.zedGraph.GraphPane.YAxis.Scale.Min = itemList[0].YAxisMin;
                this.zedGraph.GraphPane.YAxis.Scale.Max = itemList[0].YAxisMin + yAxisRange;
                if (this.zedGraph.GraphPane.YAxis.Scale.Max > itemList[0].YAxisMax)
                {
                    this.zedGraph.GraphPane.YAxis.Scale.Max = itemList[0].YAxisMax;
                }
            }

            // TODO : Ｙ軸グリッド幅を決める
            this.setYAxisGrid();
        }

        /// <summary>
        /// Ｙ軸の表示幅を求める
        /// </summary>
        /// <returns></returns>
        private double getYAxisRange()
        {
            double yAxisRange;
            if ((this.zedGraph.GraphPane.YAxis.Scale.Max >= 0) && (this.zedGraph.GraphPane.YAxis.Scale.Min >= 0))
            {
                yAxisRange = this.zedGraph.GraphPane.YAxis.Scale.Max - this.zedGraph.GraphPane.YAxis.Scale.Min;
            }
            else if ((this.zedGraph.GraphPane.YAxis.Scale.Max < 0) && (this.zedGraph.GraphPane.YAxis.Scale.Min < 0))
            {
                yAxisRange = Math.Abs(this.zedGraph.GraphPane.YAxis.Scale.Min) - Math.Abs(this.zedGraph.GraphPane.YAxis.Scale.Max);
            }
            else
            {
                yAxisRange = this.zedGraph.GraphPane.YAxis.Scale.Max + Math.Abs(this.zedGraph.GraphPane.YAxis.Scale.Min);
            }
            return yAxisRange;
        }

        /// <summary>
        /// Ｙ軸のグリッド幅を設定
        /// </summary>
        /// <param name="yAxisRange"></param>
        private void setYAxisGrid()
        {
            double yAxisRange = this.getYAxisRange();

            //System.Diagnostics.Debug.WriteLine("Y Axis Range : {0}", yAxisRange);
            
            // 領域を６分割する
            double yAxisSprit = yAxisRange / 6;
            string yAxisSpritString = string.Format("{0}", yAxisSprit);
            
            //System.Diagnostics.Debug.WriteLine("Y Axis Sprit Range : {0}", yAxisSprit);

            if (yAxisSprit >= 10)
            {
                // 整数部の値文字を取得する
                int pointPos = yAxisSpritString.IndexOf('.');
                if (pointPos == -1)
                {
                    pointPos = yAxisSpritString.Length;
                }
                yAxisSpritString = yAxisSpritString.Substring(0, pointPos);

                // 下位１桁を丸める
                //string work = yAxisSpritString.Substring(0, yAxisSpritString.Length - 1) + (Convert.ToInt32(yAxisSpritString.Substring(yAxisSpritString.Length - 1, 1)) >= 5 ? "5" : "0");
                string work = yAxisSpritString.Substring(0, yAxisSpritString.Length - 1) + "0";

                //System.Diagnostics.Debug.WriteLine(string.Format("Y Axis Grid : {0} --> {1}", yAxisSpritString, work));
                
                this.zedGraph.GraphPane.YAxis.Scale.MajorStep = Convert.ToDouble(work);
            }
            else if (yAxisSprit >= 1)
            {
                // 整数部の値文字を取得する
                int pointPos = yAxisSpritString.IndexOf('.');
                if (pointPos == -1)
                {
                    pointPos = yAxisSpritString.Length;
                }
                yAxisSpritString = yAxisSpritString.Substring(0, pointPos);

                //System.Diagnostics.Debug.WriteLine(string.Format("Y Axis Grid : {0}", yAxisSpritString.Substring(0, 1)));
                
                this.zedGraph.GraphPane.YAxis.Scale.MajorStep = Convert.ToDouble(yAxisSpritString.Substring(0, 1));
            }
            else
            {
                // 指数表示を除く
                if (yAxisSpritString.IndexOf("E-") == -1)
                {
                    string work = yAxisSpritString.Substring(0, 2);
                    for (int index = 2; index < yAxisSpritString.Length; index++)
                    {
                        work += yAxisSpritString.Substring(index, 1);
                        if (yAxisSpritString.Substring(index, 1) != "0")
                        {
                            break;
                        }
                    }

                    //System.Diagnostics.Debug.WriteLine(string.Format("Y Axis Grid : {0} --> {1}", yAxisSpritString, work));
                    
                    this.zedGraph.GraphPane.YAxis.Scale.MajorStep = Convert.ToDouble(work);
                }
            }
        }


        /// <summary>
        /// スクロールイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void zedGraph_ScrollEvent(object sender, ScrollEventArgs e)
        {
            //Tracer.WriteVerbose("ScrollEvent  {0}", e.Type.ToString());
            //if ((e.Type == ScrollEventType.ThumbTrack) ||
            //    (e.Type == ScrollEventType.EndScroll))
            if (e.Type == ScrollEventType.EndScroll)
            {
                this.debugWriteXMinMax();
            }
        }

        /// <summary>
        /// マウスDown状態
        /// </summary>
        private bool mouseDownStatus = false;

        /// <summary>
        /// マウスDOWNイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool zedGraph_MouseDownEvent(ZedGraphControl sender, MouseEventArgs e)
        {
            if (this.zedGraph.IsEnableWheelZoom)
            {
                this.mouseDownStatus = true;
            }
            return default(bool);
        }

        /// <summary>
        /// マウスMOVEイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool zedGraph_MouseMoveEvent(ZedGraphControl sender, MouseEventArgs e)
        {
            if (this.zedGraph.IsEnableWheelZoom)
            {
                if (this.mouseDownStatus)
                {
                    //Tracer.WriteVerbose("MouseMoveEvent");
                    //this.debugWriteXMinMax();
                }
            }
            return default(bool);
        }

        /// <summary>
        /// マウスUPイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool zedGraph_MouseUpEvent(ZedGraphControl sender, MouseEventArgs e)
        {
            if (this.zedGraph.IsEnableWheelZoom)
            {
                //Tracer.WriteVerbose("MouseUpEvent");
                this.mouseDownStatus = false;

                //this.debugWriteXMinMax();
            }

            return default(bool);
        }

        /// <summary>
        /// グラフのＸ軸最小／最大をデバッグ表示
        /// </summary>
        private void debugWriteXMinMax()
        {
            //Tracer.WriteVerbose("Min={0}  Max={1}", this.zedGraph.GraphPane.XAxis.Scale.Min, this.zedGraph.GraphPane.XAxis.Scale.Max);
            
            // グラフ位置変更通知
            if (this.ChangeGraphPointEvent != null)
            {
                this.ChangeGraphPointEvent(this.GraphNumber, this.zedGraph.GraphPane.XAxis.Scale.Min, this.zedGraph.GraphPane.XAxis.Scale.Max);
            }
        }



        /// <summary>
        /// 初期化
        /// this.zedGraphの初期設定を行う
        /// </summary>
        private void Initialize()
        {
            if (this.isInitialize)
            {
                return;
            }

            MasterPane masterPane = this.zedGraph.MasterPane;
            masterPane.PaneList.Clear();
            masterPane.Add(new GraphPane());
            masterPane.SetLayout(CreateGraphics(), PaneLayout.SingleRow);

            GraphPane myPane = this.zedGraph.GraphPane;

            // グラフ領域の内側をグラデーションに
            Color color1 = (this.gradation) ? Color.White : this.graphBackColor;
            Color color2 = this.graphBackColor;
            this.zedGraph.GraphPane.Chart.Fill = new Fill(color1, color2, 45.0F);

            // ポイントの値をToolTip風に表示する。
            this.zedGraph.IsShowPointValues = true;

            //コンテキストメニュー非表示
            this.zedGraph.IsShowContextMenu = false;

            //マウス左ボタンでのパン
            this.zedGraph.IsEnableHPan = true;
            this.zedGraph.IsEnableVPan = true;
            this.zedGraph.PanModifierKeys = Keys.None;
            this.zedGraph.PanButtons = MouseButtons.Left;

            this.zedGraph.IsEnableHEdit = false;
            this.zedGraph.IsEnableHZoom = true;
            this.zedGraph.IsEnableSelection = false;
            this.zedGraph.IsEnableVEdit = false;
            this.zedGraph.IsEnableVZoom = true;
            this.zedGraph.IsEnableWheelZoom = this.mouseZoomEnabled;
            //this.zedGraph.IsEnableZoom = true;
            this.zedGraph.IsShowContextMenu = false;

            this.zedGraph.IsShowVScrollBar = true;

            this.zedGraph.AxisChange();
            this.zedGraph.Invalidate();

            this.zedGraph.Visible = true;

            this.isInitialize = true;
        }

        /// <summary>
        /// グラフタイトルの表示
        /// </summary>
        private void DispTitle()
        {
            this.zedGraph.GraphPane.Title.Text = this.title;
        }

        /// <summary>
        /// 描画処理
        /// </summary>
        private void draw()
        {
            if (this.data == null)
            {
                return;
            }

            List<PointPair> markingPoints = new List<PointPair>();
            List<PointPairList> pplists = new List<PointPairList>();
            int dataCount = this.lastDataCount;

            try
            {
                //排他する
                //MeasureController.MeasureDataMutex.WaitOne();

                dataCount = this.data.Count;

                // PointPairList作成
                for (int i = 0; i < this.itemList.Count; i++)
                {
                    PointPairList list = new PointPairList();
                    if (i == 0)
                    {
                        bool preMarking = false;
                        double saveXPoint = 0;                        
                        for (int j = 0; j < dataCount; j++)
                        {
                            // 秒に変換する
                            double x = this.data[j].X / 1000.0;
                            double y = this.data[j].Y[i];

                            list.Add(new PointPair(x, y));

                            // TODO : マーキング位置取得
                            if (this.data[j].Marking == true)
                            {
                                if (preMarking == false)
                                {
                                    preMarking = true;
                                    if (this.getPreMarkingData == false)
                                    {
                                        saveXPoint = x;
                                    }
                                    else
                                    {
                                        this.getPreMarkingData = false;
                                        BoxObj preBox = (BoxObj)this.zedGraph.GraphPane.GraphObjList[this.zedGraph.GraphPane.GraphObjList.Count - 1];
                                        this.zedGraph.GraphPane.GraphObjList.RemoveAt(this.zedGraph.GraphPane.GraphObjList.Count - 1);
                                        saveXPoint = preBox.Location.X;
                                    }
                                }
                            }
                            else if (preMarking == true)
                            {
                                preMarking = false;
                                this.getPreMarkingData = false;
                                //markingPoints.Add(new PointPair(saveXPoint, this.data[j - 1].X / 1000.0));
                                markingPoints.Add(new PointPair(saveXPoint, this.data[j].X / 1000.0));
                            }
                            else
                            {
                                this.getPreMarkingData = false;
                            }
                        }
                        if (preMarking == true)
                        {
                            this.getPreMarkingData = true;
                            markingPoints.Add(new PointPair(saveXPoint, this.data[dataCount - 1].X / 1000.0));
                        }
                    }
                    else
                    {
                        for (int j = 0; j < dataCount; j++)
                        {
                            // 秒に変換する
                            double x = this.data[j].X / 1000.0;
                            double y = this.data[j].Y[i];

                            list.Add(new PointPair(x, y));
                        }
                    }
                    pplists.Add(list);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                // Mutexロック解放
                //MeasureController.MeasureDataMutex.ReleaseMutex();
            }

            // 最初はcurveListを追加する
            if (this.zedGraph.GraphPane.CurveList == null || this.zedGraph.GraphPane.CurveList.Count == 0)
            {
                for (int i = 0; i < this.itemList.Count; i++)
                {
                    // 軸の取得
                    AxisBean axis = this.FindAxis(this.itemList[i]);

                    // 表示
                    LineItem line = this.zedGraph.GraphPane.AddCurve(this.itemList[i].Name, pplists[i], this.itemList[i].LineColor, SymbolType.None);
                    line.IsY2Axis = axis.IsY2Axis;
                    line.YAxisIndex = axis.YAxisIndex;
                    line.Label.FontSpec = new FontSpec("System", (float)this.DataLabelFontSize, Color.Black, false, false, false);
                    line.Label.FontSpec.Border = new Border(false, Color.White, 0.0F);
                    line.Label.IsVisible = (this.itemLabelVisible & this.itemList[i].Visible);
                    line.IsVisible = this.itemList[i].Visible;
                    line.Line.Width = (float)this.itemList[i].LineWidth;
                }
            }
            else
            {
                double mergin = this.newDataOnlySpan * 0.2;
                double scalemin = this.zedGraph.GraphPane.XAxis.Scale.Max - this.newDataOnlySpan - mergin;

                // ２回目以降はデータをAddするのみ
                for (int i = 0; i < this.itemList.Count; i++)
                {
                    if (pplists.Count <= i || pplists[i] == null)
                    {
                        continue;
                    }
                        
                    LineItem line = (LineItem)this.zedGraph.GraphPane.CurveList[i];

                    foreach(PointPair pp in pplists[i])
                    {
                        line.AddPoint(pp);
                        line.Line.Width = (float)this.itemList[i].LineWidth;
                    }

                    // X軸の表示外になったらグラフデータを削除する
                    if (this.newDataOnlyMode && this.dataEraseMode)
                    {
                        int delIdx = -1;
                        for (int j = 0; j < line.Points.Count; j++)
                        {
                            if (line.Points[j].X >= scalemin)
                            {
                                delIdx = j;
                                break;
                            }
                        }
                        if (delIdx >= 0)
                        {
                            for (int j = 0; j < delIdx; j++)
                            {
                                line.RemovePoint(0);
                            }
                        }
                    }
                }
            }

            this.lastDataCount = dataCount;

            // X軸範囲
            if (this.newDataOnlyMode)
            {
                // 最新値のみ表示するモード
                if (pplists != null && pplists.Count != 0 && pplists[0].Count != 0)
                {
                    // 現在の表示幅
                    double span = this.zedGraph.GraphPane.XAxis.Scale.Max - this.zedGraph.GraphPane.XAxis.Scale.Min;
                    if (span > this.newDataOnlySpan)
                    {
                        span = this.newDataOnlySpan;
                    }

                    // 右余白を計算
                    double rightMargin = span / 10.0;

                    this.zedGraph.GraphPane.XAxis.Scale.Max = pplists[0].Last().X + rightMargin;
                    double xMin = this.zedGraph.GraphPane.XAxis.Scale.Max - span;
                    this.zedGraph.GraphPane.XAxis.Scale.Min = xMin ;
                }

                this.zedGraph.ScrollMaxX = this.zedGraph.GraphPane.XAxis.Scale.Max;
                this.zedGraph.ScrollMinX = this.zedGraph.GraphPane.XAxis.Scale.Min;

                this.zedGraph.AxisChange();
            }
            else
            {
                //// 右余白を計算
                //double rightMargin = this.zedGraph.ScrollMaxX / 50.0;

                //// 通常モード
                //if (pplists[0].Last().X + rightMargin > this.zedGraph.ScrollMaxX)
                //{
                //    // レンジを超えたら自動的に縮小
                //    this.zedGraph.GraphPane.XAxis.Scale.Max = pplists[0].Last().X + rightMargin;
                //    this.zedGraph.AxisChange();
                //}
            }

            // TODO : Ｘ軸範囲外のマーキング領域を消去する
            if (this.zedGraph.GraphPane.GraphObjList.Count != 0)
            {
                for (int index = 0; index < this.zedGraph.GraphPane.GraphObjList.Count; index++)
                {
                    BoxObj markingBox = (BoxObj)this.zedGraph.GraphPane.GraphObjList[index];
                    if ((markingBox.Location.X + markingBox.Location.Width) < this.zedGraph.ScrollMinX)
                    {
                        this.zedGraph.GraphPane.GraphObjList.RemoveAt(index);
                        index--;
                        continue;
                    }
                    if (markingBox.Location.X > this.zedGraph.ScrollMaxX)
                    {
                        this.zedGraph.GraphPane.GraphObjList.RemoveAt(index);
                        index--;
                        continue;
                    }
                }
            }
            // TODO : マーキング領域を追加する
            if (markingPoints.Count != 0)
            {
                for (int index = 0; index < markingPoints.Count; index++)
                {
                    BoxObj markingBox = new BoxObj(
                            markingPoints[index].X,                                                                                 // 描画開始のＸ値
                            this.zedGraph.GraphPane.YAxis.Scale.Max,                                                                // 描画開始のＹ値（固定）
                            markingPoints[index].Y - markingPoints[index].X,                                                        // 描画幅
                            Math.Abs(this.zedGraph.GraphPane.YAxis.Scale.Max) + Math.Abs(this.zedGraph.GraphPane.YAxis.Scale.Min),  // 描画高さ（固定）
                            Color.White, Color.White);                                                                              // 描画色
                    //markingBox.Fill = new Fill(Color.White, Color.FromArgb(200, Color.LightGreen), 45.0F);
                    markingBox.ZOrder = ZOrder.E_BehindCurves;
                    markingBox.IsClippedToChartRect = true;
                    markingBox.Location.CoordinateFrame = CoordType.AxisXYScale;
                    this.zedGraph.GraphPane.GraphObjList.Add(markingBox);
                }
            }

            this.zedGraph.Invalidate();
        }

        private AxisBean FindAxis(ItemBean item)
        {
            AxisBean axis = this.yAxisList.Find(a => a.Id == item.Axis.Id && a.AxisMax == item.YAxisMax && a.AxisMin == item.YAxisMin);

            return axis;
        }
    }
}
