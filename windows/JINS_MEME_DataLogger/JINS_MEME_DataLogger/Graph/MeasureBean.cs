using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 測定データの項目
    /// </summary>
    public class MeasureBean
    {
        /// <summary>
        /// マーキング
        /// </summary>
        public bool Marking { get; set; }

        /// <summary>
        /// シリアル番号
        /// </summary>
        public long SerialNumber { get; set; }

        /// <summary>
        /// X軸＝時間軸　内部ではmsec
        /// </summary>
        public long X { get; set; }

        /// <summary>
        /// Y軸値配列
        /// </summary>
        public int[] Y { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="marking"></param>
        /// <param name="serialNumber"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public MeasureBean(bool marking, long serialNumber, long x, int[] y)
        {
            this.Marking = marking;
            this.SerialNumber = serialNumber;
            this.X = x;
            this.Y = y;
        }


        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
        public MeasureBean Clone()
        {
            return new MeasureBean(this.Marking, this.SerialNumber, this.X, this.Y.ToArray());
        }
    }
}
