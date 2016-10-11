using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// センサー位置表示用データ保存クラス
    /// </summary>
    public class SensorLocusData_Bean : OpenGL3D_Bean
    {
        /// <summary>
        /// トリガー
        /// </summary>
        private int torigger = 0;
        /// <summary>
        /// SEQ
        /// </summary>
        private int seq=0;
        /// <summary>
        /// トリガー
        /// </summary>
        public int Torigger { get { return torigger; } set { torigger = value; } }
        /// <summary>
        /// SEQ
        /// </summary>
        public int Seq { get { return seq; } set { seq = value; } } 

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SensorLocusData_Bean() { }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <param name="torigger">トリガー</param>
        /// <param name="seq">seq</param>
        public SensorLocusData_Bean(float x, float y, float z, int torigger, int seq)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Torigger = torigger;
            this.seq = seq;
        }

    }
}
