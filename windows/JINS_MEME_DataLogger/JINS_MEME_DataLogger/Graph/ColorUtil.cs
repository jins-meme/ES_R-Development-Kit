using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// グラフ色操作のクラスです。
    /// </summary>
    public class ColorUtil
    {
        /// <summary>
        /// <para>文字列からColorに変換する。</para>
        /// <para>文字列はシステム定義のもの(Blue,Yellow等)か、１６進形式のARGB値。</para>
        /// </summary>
        /// <param name="colorString">文字列。</param>
        /// <returns>設定値</returns>
        public static Color NameToColor(String colorString)
        {
            Color ret;
            int argb;
            if (Int32.TryParse(colorString, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out argb))
            {
                ret = Color.FromArgb(argb);
            }
            else
            {
                ret = Color.FromName(colorString);
            }
            return ret;
        }

    }
}
