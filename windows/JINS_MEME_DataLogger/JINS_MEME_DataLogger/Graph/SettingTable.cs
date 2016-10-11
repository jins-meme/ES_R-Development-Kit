using System;
using System.Data;
using System.IO;
using System.Drawing;
using System.Threading;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 設定値の読出し、保存の処理を記載します。
    /// </summary>
    /// <para>設定テーブル。</para>
    /// <para>設定名.XMLファイルからの読み出し、保存を行います。</para>
    /// <para>設定項目はカテゴリー＋KEYの組み合わせで管理します。</para>
    /// <para>設定ファイルは実行パス\Configにあることを前提とします。</para>
    public class SettingTable : DataTable
    {
        /// <summary>設定名。</summary>
        private String settingName;
        /// <summary>ファイルアクセス排他用MUTEX。</summary>
        private static Mutex mutex = new Mutex(false, "SettingTable");

        /// <summary>
        /// コンストラクタ。
        /// 値指定してインスタンスを作成します。
        /// </summary>
        /// <param name="settingName">設定名。</param>
        public SettingTable(String settingName)
        {
            DataColumn[] keys = new DataColumn[2];
            DataColumn column;

            this.settingName = settingName;
            base.TableName = this.settingName;

            // category列
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Category";
            base.Columns.Add(column);

            // primaryキーに追加
            keys[0] = column;

            // Key列
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Key";
            base.Columns.Add(column);

            // primaryキーに追加
            keys[1] = column;

            // Value列
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Value";
            base.Columns.Add(column);

            // Set the PrimaryKeys property to the array.
            base.PrimaryKey = keys;

            //ファイル読み込み
            this.ReadConfig();

        }
        /// <summary>
        /// 項目取得インデクサ。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <returns>設定値文字列</returns>
        public String this[String category, String key]
        {
            get
            {

                return GetStringValue(category, key, "");
            }
            set
            {
                SetStringValue(category, key, value);
            }
        }
        /// <summary>
        /// Integerで項目取得。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="defaultValue">項目未設定の場合のデフォルト値。</param>
        /// <returns>設定値</returns>
        public int GetInteger(String category, String key, int defaultValue)
        {
            String str = GetStringValue(category, key, defaultValue.ToString());
            int value = defaultValue;
            int result;
            if (int.TryParse(str, out result) == true)
            {
                value = result;
            }
            return value;
        }
        /// <summary>
        /// Integerで項目設定。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="value">設定値。</param>
        public void SetInteger(String category, String key, int value)
        {
            SetStringValue(category, key, value.ToString());
        }
        /// <summary>
        /// Doubleで項目取得。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="defaultValue">項目未設定の場合のデフォルト値。</param>
        /// <returns>設定値</returns>
        public Double GetDouble(String category, String key, Double defaultValue)
        {
            String str = GetStringValue(category, key, defaultValue.ToString());
            Double value = defaultValue;
            Double result;
            if (Double.TryParse(str, out result) == true)
            {
                value = result;
            }
            return value;
        }
        /// <summary>
        /// Doubleで項目設定。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="value">設定値。</param>
        public void SetDouble(String category, String key, Double value)
        {
            SetStringValue(category, key, value.ToString());
        }
        /// <summary>
        /// Colorで項目取得。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="defaultValue">項目未設定の場合のデフォルト値。</param>
        /// <returns>設定値</returns>
        public Color GetColor(String category, String key, Color defaultValue)
        {
            return StringToColor(GetStringValue(category, key, defaultValue.Name));
        }
        /// <summary>
        /// Colorで項目設定。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="value">設定値。</param>
        public void SetColor(String category, String key, Color value)
        {
            SetStringValue(category, key, value.Name);
        }
        /// <summary>
        /// boolで項目取得。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="defaultValue">項目未設定の場合のデフォルト値。</param>
        /// <returns>設定値</returns>
        public bool GetBool(String category, String key, bool defaultValue)
        {
            String str = GetStringValue(category, key, defaultValue.ToString());
            bool value = true;

            if (!bool.TryParse(str, out value))
            {
                value = true;
                int result;
                if (int.TryParse(str, out result) == true && result == 0)
                {
                    value = false;
                }
            }


            return value;
        }
        /// <summary>
        /// boolで項目設定。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="value">設定値。</param>
        public void SetBool(String category, String key, bool value)
        {
            int intValue = 0;
            if (value)
            {
                intValue = 1;
            }
            SetStringValue(category, key, intValue.ToString());
        }
        /// <summary>
        /// DateTimeで項目取得。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="defaultValue">項目未設定の場合のデフォルト値。</param>
        /// <returns>設定値</returns>
        public DateTime GetDateTime(String category, String key, DateTime defaultValue)
        {
            String str = GetStringValue(category, key, defaultValue.ToString("F"));
            DateTime value = defaultValue;
            DateTime result ;
            if (DateTime.TryParse(str, out result) == true)
            {
                value = result;
            }
            return result;
        }
        /// <summary>
        /// DateTimeで項目設定。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="value">設定値。</param>
        public void SetDateTime(String category, String key, DateTime value)
        {
            SetStringValue(category, key, value.ToString("F"));
        }

        /// <summary>
        /// Stringで項目取得。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="defaultValue">項目未設定の場合のデフォルト値。</param>
        /// <returns>設定値</returns>
        public String GetStringValue(String category, String key, String defaultValue)
        {
            String ret = defaultValue;
            DataRow row = base.Rows.Find(new object[] { category, key });
            if (row != null)
            {
                ret = (String)row["Value"];
            }
            return ret;
        }
        /// <summary>
        /// Stringで項目設定。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="value">設定値。</param>
        public void SetStringValue(String category, String key, String value)
        {
            try
            {
                //排他する
                mutex.WaitOne();

                DataRow row = base.Rows.Find(new object[] { category, key });
                if (row != null)
                {
                    row["Value"] = value;
                }
                else
                {
                    Add(category, key, value);
                }
            }
            catch
            {
            }
            finally
            {
                // Mutexロック解放
                mutex.ReleaseMutex();

                //書き込み
                this.WriteConfig();
            }
        }
        /// <summary>
        /// PointでウィンドウのLocation取得。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="defaultLocation"></param>
        /// <returns>設定値</returns>
        public Point GetWindowLocation(String category,Point defaultLocation)
        {
            int top = GetInteger(category, "Top", defaultLocation.X);
            int left = GetInteger(category, "Left", defaultLocation.Y);
            Point location = new Point(left, top);

            return location;
        }
        /// <summary>
        /// SizeでウィンドウのSize取得。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="defaultSize"></param>
        /// <returns>設定値</returns>
        public Size GetWindowSize(String category, Size defaultSize)
        {
            int width = GetInteger(category, "Width", defaultSize.Width);
            int height = GetInteger(category, "Height", defaultSize.Height);
            Size size = new Size(width, height);

            return size;
        }


        /// <summary>
        /// PointでウィンドウのLocation設定。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="location">Location。</param>
        public void SetWindowLocation(String category, Point location)
        {
            SetInteger(category, "Top", location.Y);
            SetInteger(category, "Left", location.X);
        }

        /// <summary>
        /// SizeでウィンドウのSize設定。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="size">Size。</param>
        public void SetWindowSize(String category, Size size)
        {
            SetInteger(category, "Width", size.Width);
            SetInteger(category, "Height", size.Height);
        }


        /// <summary>
        /// テーブルにADDする。
        /// </summary>
        /// <param name="category">キー(カテゴリー)。</param>
        /// <param name="key">キー(KEY)。</param>
        /// <param name="value">設定値。</param>
        private void Add(String category, String key, Object value)
        {
            base.Rows.Add(new object[] { category, key, value });
        }

        /// <summary>
        /// ファイルから読む。
        /// </summary>
        public void ReadConfig()
        {
            try
            {
                //排他する
                mutex.WaitOne();

                //一旦削除
                base.Clear();

                String path = GetPath();
                base.ReadXml(path);
            }
            catch 
            {
            }
            finally
            {
                // Mutexロック解放
                mutex.ReleaseMutex();
            }

        }

        /// <summary>
        /// ファイルに書く。
        /// </summary>
        public void WriteConfig()
        {
            try
            {
                //排他する
                mutex.WaitOne();

                String path = GetPath();
                String directory = Directory.GetParent(path).FullName;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                base.WriteXml(path);
            }
            catch (Exception e)
            {
                throw (e);
            }
            finally
            {
                // Mutexロック解放
                mutex.ReleaseMutex();
            }
　       }



        /// <summary>
        /// パス生成 "実行パス\Config"固定。
        /// </summary>
        private String GetPath()
        {
            //string appPath = Path.Combine(System.Windows.Forms.Application.LocalUserAppDataPath, "Config");
            //String commonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            //                                Application.CompanyName, Application.ProductName,"Config");

            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                                    System.Windows.Forms.Application.CompanyName,
                                                    //System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion,
                                                    "MEME academic",
                                                    "Config");

            String path = Path.Combine(configPath, settingName + ".Config"); 

            return path;
        }

        /// <summary>
        /// <para>文字列からColorに変換する。</para>
        /// <para>文字列はシステム定義のもの(Blue,Yellow等)か、１６進形式のARGB値。</para>
        /// </summary>
        /// <param name="colorString">文字列。</param>
        /// <returns>設定値</returns>
        public static Color StringToColor(String colorString)
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
