using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// ＣＳＶ操作処理クラスです。
    /// </summary>
    public class CsvUtil
    {
        /// <summary>
        /// 文字列中の文字をカウントする
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int CountChar(string s,char c)
        {
            return s.Length - s.Replace(c.ToString(), "").Length;
        }

        /// <summary>
        /// CSVを配列に変換
        /// </summary>
        /// <param name="csvText">CSVの内容が入ったString</param>
        /// <returns>変換結果の配列</returns>
        public static string[] Split(string csvText)
        {
            //前後の改行を削除しておく
            csvText = csvText.Trim(new char[] { '\r', '\n' });

            List<string> csvFields = new List<string>();

            int csvTextLength = csvText.Length;
            int startPos = 0, endPos = 0;
            string field = "";

            while (true)
            {
                //空白を飛ばす
                while (startPos < csvTextLength &&
                    (csvText[startPos] == ' ' || csvText[startPos] == '\t'))
                {
                    startPos++;
                }

                //データの最後の位置を取得
                if (startPos < csvTextLength && csvText[startPos] == '"')
                {
                    //"で囲まれているとき
                    //最後の"を探す
                    endPos = startPos;
                    while (true)
                    {
                        endPos = csvText.IndexOf('"', endPos + 1);
                        if (endPos < 0)
                        {
                            throw new ApplicationException("\"が不正");
                        }
                        //"が2つ続かない時は終了
                        if (endPos + 1 == csvTextLength || csvText[endPos + 1] != '"')
                        {
                            break;
                        }
                        //"が2つ続く
                        endPos++;
                    }

                    //一つのフィールドを取り出す
                    field = csvText.Substring(startPos, endPos - startPos + 1);
                    //""を"にする
                    field = field.Substring(1, field.Length - 2).Replace("\"\"", "\"");

                    endPos++;
                    //空白を飛ばす
                    while (endPos < csvTextLength &&
                        csvText[endPos] != ',' && csvText[endPos] != '\n')
                    {
                        endPos++;
                    }
                }
                else
                {
                    //"で囲まれていない
                    //カンマか改行の位置
                    endPos = startPos;
                    while (endPos < csvTextLength &&
                        csvText[endPos] != ',' && csvText[endPos] != '\n')
                    {
                        endPos++;
                    }

                    //一つのフィールドを取り出す
                    field = csvText.Substring(startPos, endPos - startPos);
                    //後の空白を削除
                    field = field.TrimEnd();
                }

                //フィールドの追加
                csvFields.Add(field);

                //行の終了か調べる
                if (endPos >= csvTextLength || csvText[endPos] == '\n')
                {
                    //行の終了
                    break;
                }

                //次のデータの開始位置
                startPos = endPos + 1;
            }

            return csvFields.ToArray<string>();
        }

        /// <summary>
        /// 配列をCSVに変換
        /// </summary>
        /// <param name="csvText">CSVの内容が入ったString</param>
        /// <returns>変換結果の配列</returns>
        public static string Join(string[] fields)
        {
            string csvText = string.Empty;

            if (fields.Count() < 1)
            {
                return csvText;
            }

            csvText = string.Format("\"{0}\"", fields[0]);

            for (int i = 1; i < fields.Count(); i++)
            {
                string f = fields[i];
                csvText += string.Format(",\"{0}\"", f);
            }

            return csvText;
        }
    }
}
