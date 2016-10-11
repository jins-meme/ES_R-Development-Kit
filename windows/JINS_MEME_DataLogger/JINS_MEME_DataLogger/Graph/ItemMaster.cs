using System;
using System.Collections.Generic;
using System.IO;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 項目パターン
    /// </summary>
    public class ItemMaster
    {
        /// <summary>
        /// 項目パターンリスト
        /// </summary>
        public static List<ItemMasterBean> ItemPatternList { get; set; }

        /// <summary>
        /// 加速度項目
        /// </summary>
        public static List<ItemMasterBean> AccelerationItems { get; set; }

        /// <summary>
        /// 角速度項目
        /// </summary>
        public static List<ItemMasterBean> AngularVelocityItems { get; set; }

        /// <summary>
        /// 眼電位項目
        /// </summary>
        public static List<ItemMasterBean> ElectrooculographyItems { get; set; }

        /// <summary>
        /// クォータニオン項目
        /// </summary>
        public static List<ItemMasterBean> QuaternionItems { get; set; }


        /// <summary>
        /// パス（固定）
        /// </summary>
        public static string FilePath
        {
            get
            {
                //string appPath = Path.Combine(System.Windows.Forms.Application.LocalUserAppDataPath, "Master");
                //String commonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                //                                Application.CompanyName, Application.ProductName, "Master");

                string masterPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                                        System.Windows.Forms.Application.CompanyName,
                                                        //System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion,
                                                        "MEME academic",
                                                        "Master");

                return Path.Combine(masterPath, "ItemPatternMaster.mst");
            }
        }

        /// <summary>
        /// ファイルからロードしてリスト生成
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static void Load()
        {
            string filePath = FilePath;
            ItemPatternList = new List<ItemMasterBean>();
            FileStream fs = null;
            StreamReader sr = null;
            string[] fields;
            ItemMasterBean pattern = null;

            try
            {
                AxisMaster.Load();

                if (!File.Exists(filePath))
                {
                    GenerateDefaultList();
                    return;
                }


                // ファイルオープン
                fs = File.OpenRead(filePath);
                sr = new StreamReader(fs);

                string version = string.Empty;

                while (!sr.EndOfStream)
                {
                    // 読む
                    string rec = sr.ReadLine();

                    // アプリケーションバージョン
                    if (rec.Contains("$ApplicationVersion"))
                    {
                        version = rec.Split(new char[] { '=' })[1];
                    }

                    // 項目パターン名称
                    else if (rec.Contains("$ItemPattern"))
                    {
                        // いままでの項目パターンを保存
                        if (pattern != null)
                        {
                            ItemPatternList.Add(pattern);
                        }

                        // 新しい項目パターン生成
                        pattern = new ItemMasterBean();

                        // '='で分割してItemPatternの名称を取り出す
                        fields = rec.Split(new char[] { '=' });

                        pattern.Name = fields[1].Trim();
                    }

                    // X軸情報
                    else if (rec.Contains("$XAxis"))
                    {
                        // '='で分割してITEMのCSVを取り出す
                        fields = rec.Split(new char[] { '=' });
                        pattern.XAxis = AxisBean.CreateFromCsv(fields[1]);
                    }

                    else
                    {
                        // オブジェクトリストに格納
                        ItemBean item = ItemBean.CreateFromCsv(rec);
                        if (item != null)
                        {
                            // 軸マスター更新されていた時のため、軸をマスターから取り直す
                            AxisBean axis = AxisMaster.AxisList.Find(d => d.Id == item.Axis.Id);
                            item.Axis = axis;

                            pattern.ItemList.Add(item);
                        }
                    }
                }

                if (version.Equals(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion) == false)
                {
                    // バージョン不一致の為、ファイルを破棄
                    if (File.Exists(filePath))
                    {
                        sr.Close();
                        sr = null;
                        fs.Close();
                        fs = null;
                        File.Delete(filePath);
                    }
                    GenerateDefaultList();
                    return;
                }

                // 最後の項目パターンを保存
                if (pattern != null)
                {
                    ItemPatternList.Add(pattern);
                }

                AccelerationItems = new List<ItemMasterBean>();
                AngularVelocityItems = new List<ItemMasterBean>();
                ElectrooculographyItems = new List<ItemMasterBean>();
                // TODO : クォータニオンを無効化
                //QuaternionItems = new List<ItemMasterBean>();
                for (int i = 0; i < 3; i++)
                {
                    AccelerationItems.Add(ItemPatternList.Find(p => p.Name == string.Format("Accelerometer{0}", i)));
                    AngularVelocityItems.Add(ItemPatternList.Find(p => p.Name == string.Format("AngularVelocity{0}",i)));
                    ElectrooculographyItems.Add(ItemPatternList.Find(p => p.Name == string.Format("Electrooculography{0}",i)));
                    // TODO : クォータニオンを無効化
                    //QuaternionItems.Add(ItemPatternList.Find(p => p.Name == string.Format("Quaternion[0]", i)));
                }

            }
            finally
            {
                // 後始末
                if(sr != null)
                {
                    sr.Close();
                }
                if(fs != null)
                {
                    fs.Close();
                }
            }

        }

        /// <summary>
        /// 項目パターンリストをファイルに保存
        /// </summary>
        public static void Save()
        {
            string filePath = FilePath;
            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // ファイルオープン
                fs = File.Open(filePath,FileMode.Create);
                sw = new StreamWriter(fs);
                
                sw.WriteLine(string.Format("$ApplicationVersion={0}", System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion));
                
                foreach (ItemMasterBean pattern in ItemPatternList)
                {
                    sw.WriteLine(string.Format("$ItemPattern={0}", pattern.Name));

                    sw.WriteLine(string.Format("$XAxis={0}", pattern.XAxis.ToCsv()));


                    int id = 1;
                    foreach (ItemBean item in pattern.ItemList)
                    {
                        item.Id = id++;
                        sw.WriteLine(item.ToCsv());
                    }
                }
            }
            finally
            {
                // 後始末
                if (sw != null)
                {
                    sw.Close();
                }
                if (fs != null)
                {
                    fs.Close();
                }
            }
        }

        /// <summary>
        /// 出荷時LIST生成
        /// </summary>
        private static void GenerateDefaultList()
        {
            ItemPatternList = new List<ItemMasterBean>();
            ItemMasterBean pattern = null;

            AxisMaster.Load();

            for (int i = 0; i < 3; i++)
            {

                // 加速度の項目
                pattern = new ItemMasterBean();
                pattern.Name = string.Format("Accelerometer{0}", i);
                pattern.ItemList = ItemListAccess.GenerateAccelerationItems();
                ItemPatternList.Add(pattern);

                // 角速度の項目
                pattern = new ItemMasterBean();
                pattern.Name = string.Format("AngularVelocity{0}",i);
                pattern.ItemList = ItemListAccess.GenerateAngularVelocityItems();
                ItemPatternList.Add(pattern);

                // 眼電位の項目
                pattern = new ItemMasterBean();
                pattern.Name = string.Format("Electrooculography{0}",i);
                pattern.ItemList = ItemListAccess.GenerateElectrooculographyItems();
                ItemPatternList.Add(pattern);

                // TODO : クォータニオンを無効化
                //// クォータニオンの項目
                //pattern = new ItemMasterBean();
                //pattern.Name = string.Format("Quaternion{0}",i);
                //pattern.ItemList = ItemListAccess.GenerateQuaternionItems();
                //ItemPatternList.Add(pattern);
            }


            AccelerationItems = new List<ItemMasterBean>();
            AngularVelocityItems = new List<ItemMasterBean>();
            ElectrooculographyItems = new List<ItemMasterBean>();
            // TODO : クォータニオンを無効化
            //QuaternionItems = new List<ItemMasterBean>();
            for (int i = 0; i < 3; i++)
            {
                AccelerationItems.Add(ItemPatternList.Find(p => p.Name == string.Format("Accelerometer{0}", i)));
                AngularVelocityItems.Add(ItemPatternList.Find(p => p.Name == string.Format("AngularVelocity{0}",i)));
                ElectrooculographyItems.Add(ItemPatternList.Find(p => p.Name == string.Format("Electrooculography{0}",i)));
                // TODO : クォータニオンを無効化
                //QuaternionItems.Add(ItemPatternList.Find(p => p.Name == string.Format("Quaternion[0]",i)));
            }


        }
    }
}
