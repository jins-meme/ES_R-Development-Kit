using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// コンボItem用クラス
    /// </summary>
    public class ComboItemObj
    {
        public string Label { get; set; }
        public object Value { get; set; }

        public ComboItemObj(string label, object value)
        {
            Label = label;
            Value = value;
        }
    }
}
