using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 軸定義マスターファイル
    /// </summary>
    public class AxisMaster
    {
        public static List<AxisBean> AxisList { get; set; }

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

                return Path.Combine(masterPath, "AxisMaster.mst"); 
            }
        }

        /// <summary>
        /// 加速度軸定義
        /// </summary>
        public static AxisBean AccelerationAxis { get; set; }

        /// <summary>
        /// 角速度軸定義
        /// </summary>
        public static AxisBean AngularVelocityAxis { get; set; }

        /// <summary>
        /// 眼電位軸定義
        /// </summary>
        public static AxisBean ElectrooculographyAxis { get; set; }

        /// <summary>
        /// クォータニオン軸定義
        /// </summary>
        public static AxisBean QuaternionAxis { get; set; }


        private AxisMaster()
        {
        }


        /// <summary>
        /// ファイルからロードしてリスト生成
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static void Load()
        {
            string filePath = FilePath;
            AxisList = new List<AxisBean>();
            FileStream fs = null;
            StreamReader sr = null;

            try
            {
                if (!File.Exists(filePath))
                {
                    GenerateDefaultList();

                    AccelerationAxis = AxisList.Find(a => a.Id == (int)AxisIds.Accelerometer);
                    AngularVelocityAxis = AxisList.Find(a => a.Id == (int)AxisIds.AngularVelocity);
                    ElectrooculographyAxis = AxisList.Find(a => a.Id == (int)AxisIds.Electrooculography);
                    QuaternionAxis = AxisList.Find(a => a.Id == (int)AxisIds.Quaternion);

                    return;
                }

                // ファイルオープン
                fs = File.OpenRead(filePath);
                sr = new StreamReader(fs);

                while (!sr.EndOfStream)
                {
                    // 読む
                    string rec = sr.ReadLine();

                    // オブジェクトリストに格納
                    AxisBean axis = AxisBean.CreateFromCsv(rec);
                    if (axis != null)
                    {
                        AxisList.Add(axis);
                    }
                }

                AccelerationAxis = AxisList.Find(a => a.Id == (int)AxisIds.Accelerometer);
                AngularVelocityAxis = AxisList.Find(a => a.Id == (int)AxisIds.AngularVelocity);
                ElectrooculographyAxis = AxisList.Find(a => a.Id == (int)AxisIds.Electrooculography);
                QuaternionAxis = AxisList.Find(a => a.Id == (int)AxisIds.Quaternion);
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
        /// 軸リストをファイルに保存
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="yAxisList"></param>
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

                int id = 1;
                foreach (AxisBean axis in AxisList)
                {
                    axis.Id = id++;

                    // Ｙ軸グリッド幅の自動計算を行う
                    string value = Math.Abs(Convert.ToInt32((axis.AxisMax - axis.AxisMin) / 4)).ToString();
                    string resolution = value.Substring(0, 1);
                    for(int index=1; index<value.Length ; index++)
                    {
                        resolution += "0";
                    }
                    axis.GridResolution = Convert.ToInt32(resolution);

                    sw.WriteLine(axis.ToCsv());
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
        public static void GenerateDefaultList()
        {
            AxisList = new List<AxisBean>();

            // TODO : 加速度グラフのデフォルト値
            AxisBean axis;

            axis = new AxisBean();
            axis.Id = (int)AxisIds.Accelerometer;
            axis.Name = "Accelerometer";

            //axis.UnitName = "m/s^2";
            //axis.AdRangeMax = 10.0;
            //axis.AdRangeMin = -10.0;
            //axis.AxisMax = 10.0;
            //axis.AxisMin = -10.0;
            //axis.GridLineVisible = true;
            //axis.GridResolution = 2;
            //axis.DispOrder = axis.Id;
            //axis.AxisColor = Color.Olive;

            axis.UnitName = "LSB";
            axis.AdRangeMax = 10.0;
            axis.AdRangeMin = -10.0;
            //axis.AxisMax = 35000.0;
            //axis.AxisMin = -35000.0;
            axis.AxisMax = 36000.0;
            axis.AxisMin = -36000.0;
            axis.GridLineVisible = true;
            //axis.GridResolution = 10000;
            axis.GridResolution = 12000;
            axis.DispOrder = axis.Id;
            //axis.AxisColor = Color.Olive;
            axis.AxisColor = Color.Silver;

            AxisList.Add(axis);

            // TODO : 角速度グラフのデフォルト値
            axis = new AxisBean();
            axis.Id = (int)AxisIds.AngularVelocity;
            axis.Name = "Gyroscope";

            //axis.UnitName = "rad/s";
            //axis.AdRangeMax = 10.0;
            //axis.AdRangeMin = -10.0;
            //axis.AxisMax = 10.0;
            //axis.AxisMin = -10.0;
            //axis.GridLineVisible = true;
            //axis.GridResolution = 2;
            //axis.DispOrder = axis.Id;
            //axis.AxisColor = Color.Olive;

            axis.UnitName = "LSB";
            axis.AdRangeMax = 10.0;
            axis.AdRangeMin = -10.0;
            //axis.AxisMax = 35000.0;
            //axis.AxisMin = -35000.0;
            axis.AxisMax = 36000.0;
            axis.AxisMin = -36000.0;
            axis.GridLineVisible = true;
            //axis.GridResolution = 10000;
            axis.GridResolution = 12000;
            axis.DispOrder = axis.Id;
            //axis.AxisColor = Color.Olive;
            axis.AxisColor = Color.Silver;

            AxisList.Add(axis);

            // TODO : 眼電位グラフのデフォルト値
            axis = new AxisBean();
            axis.Id = (int)AxisIds.Electrooculography;
            axis.Name = "Electrooculography";

            //axis.UnitName = "mV";
            //axis.AdRangeMax = 10.0;
            //axis.AdRangeMin = -10.0;
            //axis.AxisMax = 10.0;
            //axis.AxisMin = -10.0;
            //axis.GridLineVisible = true;
            //axis.GridResolution = 2;
            //axis.DispOrder = axis.Id;
            //axis.AxisColor = Color.Olive;

            axis.UnitName = "LSB";
            axis.AdRangeMax = 10.0;
            axis.AdRangeMin = -10.0;
            //axis.AxisMax = 1000.0;
            //axis.AxisMin = -1000.0;
            axis.AxisMax = 1200.0;
            axis.AxisMin = -1200.0;
            axis.GridLineVisible = true;
            //axis.GridResolution = 500;
            axis.GridResolution = 400;
            axis.DispOrder = axis.Id;
            //axis.AxisColor = Color.Olive;
            axis.AxisColor = Color.Silver;

            AxisList.Add(axis);

            // TODO : クォータニオングラフのデフォルト値
            // TODO : クォータニオンを無効化
            //axis = new AxisBean();
            //axis.Id = (int)AxisIds.Quaternion;
            //axis.Name = "Quaternion";

            ////axis.UnitName = "";
            ////axis.AdRangeMax = 10.0;
            ////axis.AdRangeMin = -10.0;
            ////axis.AxisMax = 10.0;
            ////axis.AxisMin = -10.0;
            ////axis.GridLineVisible = true;
            ////axis.GridResolution = 2;
            ////axis.DispOrder = axis.Id;
            ////axis.AxisColor = Color.Olive;

            //axis.UnitName = "";
            //axis.AdRangeMax = 10.0;
            //axis.AdRangeMin = -10.0;
            //axis.AxisMax = 5000.0;
            //axis.AxisMin = -5000.0;
            //axis.GridLineVisible = true;
            //axis.GridResolution = 2000;
            //axis.DispOrder = axis.Id;
            //axis.AxisColor = Color.Olive;

            //AxisList.Add(axis);
        }
    }
}
