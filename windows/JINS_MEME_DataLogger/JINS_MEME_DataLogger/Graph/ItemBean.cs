using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 項目データの項目
    /// </summary>
    public class ItemBean
    {
        /// <summary>
        /// 項目数
        /// </summary>
        private const int dataNum = 19;

        /// <summary>
        /// 対応する軸オブジェクト
        /// </summary>
        public AxisBean Axis { get; set; }

        /// <summary>
        /// いままでの最大ID
        /// </summary>
        public static int LastMaxId { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        private int id = 0;
        public int Id
        {
            get { return id; }
            set
            {
                id = value;

                // いままでの最大IDを保存する
                LastMaxId = (LastMaxId < id) ? id : LastMaxId;
            }
        }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Y軸レンジ最大
        /// </summary>
        public double YAxisMax { get; set; }

        /// <summary>
        /// Y軸レンジ最小
        /// </summary>
        public double YAxisMin { get; set; }

        /// <summary>
        /// 線の色
        /// </summary>
        public Color LineColor { get; set; }

        /// <summary>
        /// 線幅
        /// </summary>
        public double LineWidth { get; set; }

        /// <summary>
        /// グラフの表示・非表示
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ItemBean()
        {
            this.Id = LastMaxId + 1;
            this.Name = string.Format("新しい項目{0:D}",this.Id);
            this.YAxisMin = 0;
            this.YAxisMax = 1000;
            this.LineColor = Color.Red ;
            this.LineWidth = 1;
            this.Visible = true;

            this.Axis = new AxisBean();

        }


        /// <summary>
        /// CSVからオブジェクト生成
        /// </summary>
        /// <param name="csv"></param>
        /// <returns></returns>
        public static ItemBean CreateFromCsv(string csv)
        {
            ItemBean item = null;
            AxisBean axis = null;

            string[] fields = CsvUtil.Split(csv);

            if (fields.Count() == dataNum)
            {
                item = new ItemBean();
                axis = new AxisBean();

                item.Id = int.Parse(fields[0]);
                item.Name = fields[1];
                item.YAxisMax = double.Parse(fields[2]);
                item.YAxisMin = double.Parse(fields[3]);
                item.LineColor = ColorUtil.NameToColor(fields[4]);
                item.LineWidth = double.Parse(fields[5]);
                item.Visible = bool.Parse(fields[6]);

                axis.Id = int.Parse(fields[7]);
                axis.Name = fields[8];
                axis.UnitName = fields[9];
                axis.AxisMax = item.YAxisMax;
                axis.AxisMin = item.YAxisMin;
                axis.GridLineVisible = bool.Parse(fields[10]);
                axis.GridResolution = double.Parse(fields[11]);
                axis.AxisColor = ColorUtil.NameToColor(fields[12]);
                axis.DispOrder = int.Parse(fields[13]);

                item.Axis = axis;

            }

            return item;
        }

        /// <summary>
        /// CSV文字列取得
        /// </summary>
        /// <returns></returns>
        public string ToCsv()
        {
            string ret = string.Empty;

            string[] fields = new string[dataNum];

            fields[0] = string.Format("{0:D}", this.Id);
            fields[1] = string.Format("{0}", this.Name);
            fields[2] = string.Format("{0:F5}", this.YAxisMax);
            fields[3] = string.Format("{0:F5}", this.YAxisMin);
            fields[4] = string.Format("{0}", this.LineColor.Name);
            fields[5] = string.Format("{0:F0}", this.LineWidth);
            fields[6] = string.Format("{0}", this.Visible);

            fields[7] = string.Format("{0:D}", Axis.Id);
            fields[8] = string.Format("{0}", Axis.Name);
            fields[9] = string.Format("{0}", Axis.UnitName);
            fields[10] = string.Format("{0}", Axis.GridLineVisible);
            fields[11] = string.Format("{0:F5}", Axis.GridResolution);
            fields[12] = string.Format("{0}", Axis.AxisColor.Name);
            fields[13] = string.Format("{0:D}", Axis.DispOrder);

            //ret = string.Join(",", fields);
            ret = CsvUtil.Join(fields);

            return ret;
        }

        public ItemBean Clone()
        {
            ItemBean item = new ItemBean();

            item.Id = this.Id;
            item.Name = this.Name;
            item.YAxisMax = this.YAxisMax;
            item.YAxisMin = this.YAxisMin;
            item.LineColor = this.LineColor;
            item.LineWidth = this.LineWidth;
            item.Visible = this.Visible;
            item.Axis = this.Axis.Clone();

            return item;
        }
    }
}
