using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// OpenGL 色情報保存クラス
    /// </summary>
    public class OpenGLColor_Bean
    {
        /// <summary>
        /// 赤
        /// </summary>
        private float red = 0;
        /// <summary>
        /// 緑
        /// </summary>
        private float green = 0;
        /// <summary>
        /// 青
        /// </summary>
        private float blue = 0;
        /// <summary>
        /// 透明度
        /// </summary>
        private float alpha = 0;

        /// <summary>
        /// 赤
        /// </summary>
        public float Red { get { return red; } set { red = value; } }
        /// <summary>
        /// 緑
        /// </summary>
        public float Green { get { return green; } set { green = value; } }
        /// <summary>
        /// 青
        /// </summary>
        public float Blue { get { return blue; } set { blue = value; } }
        /// <summary>
        /// 透明度
        /// </summary>
        public float Alpha { get { return alpha; } set { alpha = value; } }

        /// <summary>
        /// 
        /// </summary>
        public OpenGLColor_Bean() { }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="red">赤</param>
        /// <param name="green">緑</param>
        /// <param name="blue">青</param>
        /// <param name="alpha">透明度</param>
        public OpenGLColor_Bean(float red, float green, float blue, float alpha) 
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
            this.alpha = alpha;
        }
        /// <summary>
        /// データセット
        /// </summary>
        /// <param name="red">赤</param>
        /// <param name="green">緑</param>
        /// <param name="blue">青</param>
        /// <param name="alpha">透明度</param>
        public void SetData(float red, float green, float blue, float alpha)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
            this.alpha = alpha;
        }
        /// <summary>
        /// コピー
        /// </summary>
        /// <param name="sorData">コピー元データ</param>
        public void Copy(OpenGLColor_Bean sordata)
        {
            this.red = sordata.Red;
            this.green = sordata.Green;
            this.blue = sordata.Blue;
            this.alpha = sordata.Alpha;
        }
        /// <summary>
        /// Windowsのカラーコードセット
        /// </summary>
        /// <param name="color">カラーコード</param>
        public void SetWinColor(Color color)
        {
            this.red = (float)color.R / 255;
            this.green = (float)color.G / 255;
            this.blue = (float)color.B / 255;
            this.alpha = (float)color.A / 255;

        }
        /// <summary>
        /// Windowsのカラーコード取得
        /// </summary>
        /// <returns></returns>
        public Color GetWinColor() 
        {
            return Color.FromArgb((int)(this.alpha * 255), (int)(this.red* 255), (int)(this.green * 255), (int)(this.Blue * 255));
        }

    }
}
