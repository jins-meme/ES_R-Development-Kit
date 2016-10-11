using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 軸パラメータを保持します。
    /// </summary>
    public enum AxisIds
    {
        /// <summary>
        /// 加速度
        /// </summary>
        Accelerometer = 1,
        /// <summary>
        /// 角速度
        /// </summary>
        AngularVelocity = 2,
        /// <summary>
        /// 眼電位（人感センサ）
        /// </summary>
        Electrooculography = 3,
        /// <summary>
        /// クォータニオン
        /// </summary>
        Quaternion = 4,
    }

    /// <summary>
    /// 軸データ項目
    /// </summary>
    public class AxisBean
    {
        /// <summary>
        /// 項目数
        /// </summary>
        private static int fieldNum = 12;

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
        /// 単位名称
        /// </summary>
        public string UnitName { get; set; }

        /// <summary>
        /// AD変換レンジ最大
        /// </summary>
        public double AdRangeMax { get; set; }

        /// <summary>
        /// AD変換レンジ最小
        /// </summary>
        public double AdRangeMin { get; set; }

        /// <summary>
        /// グラフY軸レンジ最大
        /// </summary>
        public double AxisMax { get; set; }

        /// <summary>
        /// グラフY軸レンジ最小
        /// </summary>
        public double AxisMin { get; set; }

        /// <summary>
        /// グリッド線の表示・非表示
        /// </summary>
        public bool GridLineVisible { get; set; }

        /// <summary>
        /// グリッド線幅
        /// </summary>
        public double GridResolution { get; set; }

        /// <summary>
        /// 軸の色
        /// </summary>
        public Color AxisColor { get; set; }

        /// <summary>
        /// 表示順
        /// </summary>
        public int DispOrder { get; set; }

        /// <summary>
        /// Y2軸に表示？
        /// </summary>
        public bool IsY2Axis { get; set; }

        /// <summary>
        /// Y軸Index
        /// </summary>
        public int YAxisIndex { get; set; }


        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public AxisBean()
        {
            this.Id = LastMaxId;
            this.Name = string.Format("新しい軸{0:D}", this.Id);
            this.UnitName = "新しい単位";
            this.AdRangeMax = 10;
            this.AdRangeMin = -10;
            this.AxisMax = 1000;
            this.AxisMin = 0;
            this.AxisColor = Color.Black;
            this.GridLineVisible = true;
            this.GridResolution = 100;
            this.DispOrder = this.Id;
            this.IsY2Axis = false;
            this.YAxisIndex = 0;

        }

        /// <summary>
        /// CSVからオブジェクト生成
        /// </summary>
        /// <param name="csv"></param>
        /// <returns></returns>
        public static AxisBean CreateFromCsv(string csv)
        {
            AxisBean ret = null;

            string[] fields = CsvUtil.Split(csv);

            if (fields.Count() == fieldNum)
            {
                ret = new AxisBean();

                ret.Id = int.Parse(fields[0]);
                ret.Name = fields[1];
                ret.UnitName = fields[2];
                ret.AdRangeMax = double.Parse(fields[3]);
                ret.AdRangeMin = double.Parse(fields[4]);
                ret.AxisMax = double.Parse(fields[5]);
                ret.AxisMin = double.Parse(fields[6]);
                ret.GridLineVisible = bool.Parse(fields[7]);
                ret.GridResolution = double.Parse(fields[8]);
                ret.AxisColor = ColorUtil.NameToColor(fields[9]);
                ret.DispOrder = int.Parse(fields[10]);
                ret.IsY2Axis = bool.Parse(fields[11]);
            }

            return ret;
        }

        /// <summary>
        /// CSV文字列取得
        /// </summary>
        /// <returns></returns>
        public string ToCsv()
        {
            string ret = string.Empty;

            string[] fields = new string[fieldNum];

            fields[0] = string.Format("{0:D}", this.Id);
            fields[1] = string.Format("{0}", this.Name);
            fields[2] = string.Format("{0}", this.UnitName);
            fields[3] = string.Format("{0:F5}", this.AdRangeMax);
            fields[4] = string.Format("{0:F5}", this.AdRangeMin);
            fields[5] = string.Format("{0:F5}", this.AxisMax);
            fields[6] = string.Format("{0:F5}", this.AxisMin);
            fields[7] = string.Format("{0}", this.GridLineVisible);
            fields[8] = string.Format("{0:F5}", this.GridResolution);
            fields[9] = string.Format("{0}", this.AxisColor.Name);
            fields[10] = string.Format("{0:D}", this.DispOrder);
            fields[11] = string.Format("{0}", this.IsY2Axis);

            ret = CsvUtil.Join(fields);

            return ret;
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
        public AxisBean Clone()
        {
            AxisBean ret = new AxisBean();

            ret.Id = this.Id;
            ret.Name = this.Name;
            ret.UnitName = this.UnitName;
            ret.AdRangeMax = this.AdRangeMax;
            ret.AdRangeMin = this.AdRangeMin;
            ret.AxisMax = this.AxisMax;
            ret.AxisMin = this.AxisMin;
            ret.GridLineVisible = this.GridLineVisible;
            ret.GridResolution = this.GridResolution;
            ret.AxisColor = this.AxisColor;
            ret.DispOrder = this.DispOrder;

            return ret;
        }

        /// <summary>
        /// 文字列
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// AD値変換
        /// </summary>
        /// <param name="adraw"></param>
        /// <returns></returns>
        public double CalcAdValue(int adraw)
        {
            return (double)AdRangeMin + (adraw * (AdRangeMax - AdRangeMin) / (double)0xFFFF);
        }

    }
}
