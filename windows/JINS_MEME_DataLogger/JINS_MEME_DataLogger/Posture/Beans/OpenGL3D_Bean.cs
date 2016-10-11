using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// OpenGL 3次元座標保存クラス
    /// </summary>
    public class OpenGL3D_Bean
    {
        /// <summary>
        /// X座標
        /// </summary>
        private float x = 0;
        /// <summary>
        /// Y座標
        /// </summary>
        private float y = 0;
        /// <summary>
        /// Z座標
        /// </summary>
        private float z = 0;

        /// <summary>
        /// X座標
        /// </summary>
        public float X { get { return x; } set { x = value; } }
        /// <summary>
        /// Y座標
        /// </summary>
        public float Y { get { return y; } set { y = value; } }
        /// <summary>
        /// Z座標
        /// </summary>
        public float Z { get { return z; } set { z = value; } }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public OpenGL3D_Bean() { }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <param name="z">Z座標</param>
        public OpenGL3D_Bean(float x,float y,float z) 
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        /// <summary>
        /// 値セット
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <param name="z">Z座標</param>
        public void SetValue(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        /// <summary>
        /// クリア
        /// </summary>
        public void Clear()
        {
            this.x = 0;
            this.y = 0;
            this.z = 0;
        }
        /// <summary>
        /// コピー
        /// </summary>
        /// <param name="sorData">コピー元データ</param>
        public void Copy(OpenGL3D_Bean sorData)
        {
            this.x = sorData.X;
            this.y = sorData.Y;
            this.z = sorData.Z;
        }
        /// <summary>
        /// 各アイテム最小値取得
        /// </summary>
        /// <param name="sorData">比較データ</param>
        public void CompItemMinValue(OpenGL3D_Bean sorData) 
        {
            if (this.x > sorData.X) 
            {
                this.x = sorData.X;
            }
            if (this.y > sorData.Y)
            {
                this.y = sorData.Y;
            }
            if (this.z > sorData.Z)
            {
                this.z = sorData.Z;
            }
        }
        /// <summary>
        /// 各アイテム最大値取得
        /// </summary>
        /// <param name="sorData">比較データ</param>
        public void CompItemMaxValue(OpenGL3D_Bean sorData)
        {
            if (this.x < sorData.X)
            {
                this.x = sorData.X;
            }
            if (this.y < sorData.Y)
            {
                this.y = sorData.Y;
            }
            if (this.z < sorData.Z)
            {
                this.z = sorData.Z;
            }
        }


    }
}
