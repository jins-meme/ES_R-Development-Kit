using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 項目パターン定義
    /// </summary>
    public class ItemMasterBean
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// X軸
        /// </summary>
        public AxisBean XAxis { get; set; }

        /// <summary>
        /// 項目リスト
        /// </summary>
        public List<ItemBean> ItemList { get; set; }

        /// <summary>
        /// 表示順
        /// </summary>
        public int DispOrder { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ItemMasterBean()
        {
            XAxis = new AxisBean();
            // X軸情報初期化
            XAxis.GridLineVisible = true;
            XAxis.GridResolution = 60 * 1000;
            XAxis.AxisMin = 0;
            XAxis.AxisMax = 900 * 1000;

            ItemList = new List<ItemBean>();
        }

        /// <summary>
        /// 複製
        /// </summary>
        /// <returns></returns>
        public ItemMasterBean Clone()
        {
            ItemMasterBean clone = new ItemMasterBean();

            clone.Name = this.Name;
            clone.XAxis = this.XAxis.Clone();
            clone.ItemList = new List<ItemBean>(this.ItemList.ToArray());

            return clone;
        }
    }
}
