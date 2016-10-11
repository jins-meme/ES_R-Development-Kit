using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 項目リストのアクセスユティリティ
    /// </summary>
    public class ItemListAccess
    {
        /// <summary>
        /// デフォルトの色リスト
        /// </summary>
        //public static Color[] ColorList = new Color[] { Color.Red, Color.Aqua, Color.FromArgb(0xff,0x80,0xff,0x00), Color.FromArgb(0xff,0xff,0x80,0xff), Color.Brown, Color.Purple, 
        public static Color[] ColorList = new Color[] { Color.Red, Color.FromArgb(0xff,0xc0,0x00,0x00), Color.FromArgb(0xff,0x00,0xc0,0x00), Color.FromArgb(0xff,0x00,0x00,0xc0),
                                              Color.Aqua, Color.FromArgb(0xff,0x80,0xff,0x00), Color.FromArgb(0xff,0xff,0x80,0xff), Color.Yellow, Color.Purple,
                                              Color.Pink, Color.LightGreen, Color.LightBlue, Color.OrangeRed,Color.Gold,Color.Maroon,
                                              Color.LightPink,Color.LimeGreen,Color.Cyan,Color.MintCream,Color.Beige,Color.PaleVioletRed,
                                              Color.DarkRed,Color.DarkGreen,Color.DarkBlue,Color.Orange,Color.RosyBrown, Color.DarkMagenta,
                                              Color.DarkSalmon,Color.DarkSeaGreen,Color.DarkSlateBlue,Color.DarkOrange,Color.SaddleBrown,Color.DarkOrchid,
                                              Color.IndianRed,Color.ForestGreen,Color.SteelBlue,Color.PaleVioletRed,Color.SandyBrown,Color.MediumPurple};
        /// <summary>
        /// 項目リストから軸リストを抽出する
        /// 軸IDと項目のYMax,YMinが異なる軸を抽出する
        /// </summary>
        /// <param name="itemList"></param>
        /// <returns></returns>
        public static List<AxisBean> SelectAxisList(List<ItemBean> itemList)
        {
            List<AxisBean> axisList = new List<AxisBean>();

            foreach (ItemBean item in itemList)
            {
                // 同じデータ種類がなければLISTに追加
                AxisBean axis = axisList.Find(a => a.Id == item.Axis.Id && a.AxisMax == item.YAxisMax && a.AxisMin == item.YAxisMin);
                if (axis == null)
                {
                    AxisBean a = item.Axis;
                    a.AxisMax = item.YAxisMax;
                    a.AxisMin = item.YAxisMin;
                    axisList.Add(a);
                }
            }

            return axisList;
        }

        /// <summary>
        /// itemList中に指定軸を使用中で表示中の項目があればtrue
        /// なければfalse
        /// </summary>
        /// <param name="itemList"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static bool IsAxisVisible(List<ItemBean> itemList, AxisBean axis)
        {
            bool isVisible = false;
            for (int i = 0; i < itemList.Count; i++)
            {
                // 軸の取得
                AxisBean a = itemList[i].Axis;

                if (a.Id == axis.Id && itemList[i].Visible)
                {
                    isVisible = true;
                    break;
                }
            }
            return isVisible;
        }

        /// <summary>
        /// 加速度の雛形リストを生成
        /// </summary>
        /// <returns></returns>
        public static List<ItemBean> GenerateAccelerationItems()
        {
            List<ItemBean> list = new List<ItemBean>();

            AxisMaster.Load();

            // 軸に対応する項目を１個づつ生成する
            ItemBean item;
            AxisBean axis;

            int cnt = 1;

            // 加速度X軸
            axis = AxisMaster.AccelerationAxis;

            item = new ItemBean();
            item.Id = cnt;
            item.Name = "X-axis";
            item.Axis = axis;
            item.LineWidth = 1.0;
            item.LineColor = ColorList[cnt % ColorList.Length];
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            cnt++;

            // 加速度Y軸
            item = new ItemBean();
            item.Id = cnt;
            item.Name = "Y-axis";
            item.Axis = axis;
            item.LineWidth = 1.0;
            item.LineColor = ColorList[cnt % ColorList.Length];
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            cnt++;

            // 加速度Z軸
            item = new ItemBean();
            item.Id = cnt;
            item.Name = "Z-axis";
            item.Axis = axis;
            item.LineWidth = 1.0;
            item.LineColor = ColorList[cnt % ColorList.Length];
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            return list;
        }

        /// <summary>
        /// 角速度の雛形リストを生成
        /// </summary>
        /// <returns></returns>
        public static List<ItemBean> GenerateAngularVelocityItems()
        {
            List<ItemBean> list = new List<ItemBean>();

            AxisMaster.Load();

            // 軸に対応する項目を１個づつ生成する
            ItemBean item;
            AxisBean axis;

            int cnt = 1;

            // 角速度X軸
            axis = AxisMaster.AngularVelocityAxis;

            item = new ItemBean();
            item.Id = cnt;
            item.Name = "X-axis";
            item.Axis = axis;
            item.LineWidth = 1.0;
            item.LineColor = ColorList[cnt % ColorList.Length];
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            cnt++;

            // 角速度Y軸
            item = new ItemBean();
            item.Id = cnt;
            item.Name = "Y-axis";
            item.Axis = axis;
            item.LineWidth = 1.0;
            item.LineColor = ColorList[cnt % ColorList.Length];
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            cnt++;

            // 角速度Z軸
            item = new ItemBean();
            item.Id = cnt;
            item.Name = "Z-axis";
            item.Axis = axis;
            item.LineWidth = 1.0;
            item.LineColor = ColorList[cnt % ColorList.Length];
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            return list;
        }

        /// <summary>
        /// 眼電位の雛形リストを生成
        /// </summary>
        /// <returns></returns>
        public static List<ItemBean> GenerateElectrooculographyItems()
        {
            List<ItemBean> list = new List<ItemBean>();

            AxisMaster.Load();

            // 軸に対応する項目を１個づつ生成する
            ItemBean item;
            AxisBean axis;

            int cnt = 1;
            axis = AxisMaster.ElectrooculographyAxis;

            // 眼電位左
            item = new ItemBean();
            item.Id = cnt;
            item.Name = "Left";
            item.Axis = axis;
            item.LineWidth = 1.0;
            //item.LineColor = ColorList[cnt % ColorList.Length];
            item.LineColor = Color.FromArgb(0xff, 0xc0, 0x00, 0x00);
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            cnt++;

            // 眼電位右
            item = new ItemBean();
            item.Id = cnt;
            item.Name = "Right";
            item.Axis = axis;
            item.LineWidth = 1.0;
            //item.LineColor = ColorList[cnt % ColorList.Length];
            item.LineColor = Color.FromArgb(0xff, 0x00, 0x00, 0xc0);
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            cnt++;

            // 眼電位水平差分値
            item = new ItemBean();
            item.Id = cnt;
            item.Name = "Delta H";
            item.Axis = axis;
            item.LineWidth = 1.0;
            //item.LineColor = ColorList[cnt % ColorList.Length];
            item.LineColor = Color.FromArgb(0xff, 0xc0, 0x00, 0xc0);
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            cnt++;

            // 眼電位垂直差分値
            item = new ItemBean();
            item.Id = cnt;
            item.Name = "Delta V";
            item.Axis = axis;
            item.LineWidth = 1.0;
            //item.LineColor = ColorList[cnt % ColorList.Length];
            item.LineColor = Color.FromArgb(0xff, 0x00, 0xc0, 0xc0);
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            return list;
        }

        /// <summary>
        /// クォータニオンの雛形リストを生成
        /// </summary>
        /// <returns></returns>
        public static List<ItemBean> GenerateQuaternionItems()
        {
            List<ItemBean> list = new List<ItemBean>();

            AxisMaster.Load();

            // 軸に対応する項目を１個づつ生成する
            ItemBean item;
            AxisBean axis;

            int cnt = 1;

            // クォータニオンW軸
            axis = AxisMaster.QuaternionAxis;

            item = new ItemBean();
            item.Id = cnt;
            item.Name = "W-axis";
            item.Axis = axis;
            item.LineWidth = 1.0;
            item.LineColor = ColorList[cnt % ColorList.Length];
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            cnt++;

            // クォータニオンX軸
            item = new ItemBean();
            item.Id = cnt;
            item.Name = "X-axis";
            item.Axis = axis;
            item.LineWidth = 1.0;
            item.LineColor = ColorList[cnt % ColorList.Length];
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            cnt++;

            // クォータニオンY軸
            item = new ItemBean();
            item.Id = cnt;
            item.Name = "Y-axis";
            item.Axis = axis;
            item.LineWidth = 1.0;
            item.LineColor = ColorList[cnt % ColorList.Length];
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            cnt++;

            // クォータニオンZ軸
            item = new ItemBean();
            item.Id = cnt;
            item.Name = "Z-axis";
            item.Axis = axis;
            item.LineWidth = 1.0;
            item.LineColor = ColorList[cnt % ColorList.Length];
            item.YAxisMax = axis.AxisMax;
            item.YAxisMin = axis.AxisMin;
            item.Visible = true;

            list.Add(item);

            return list;
        }

    }
}
