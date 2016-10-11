using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Security.Permissions;

namespace JINS_MEME_DataLogger
{
	[HostProtection(Synchronization = true)]
    public class DailyTraceListener : TraceListener
    {
         private string _fileNameTemplate = null;  
         /// <summary>  
         /// ファイル名のテンプレート  
         /// </summary>  
         private string FileNameTemplate  
         {  
             get { return _fileNameTemplate; }  
         }  
       
         private string _dateFormat = "yyyyMMdd";  
         /// <summary>  
         /// 日付部分のテンプレート  
         /// </summary>  
         private string DateFormat  
         {  
             get { LoadAttribute(); return _dateFormat; }  
         }  
   
         private string _datePlaceHolder = "%YYYYMMDD%";  
         /// <summary>  
         /// ファイル名テンプレートに含まれる日付のプレースホルダ  
         /// </summary>  
         private string DatePlaceHolder  
         {  
             get { LoadAttribute(); return _datePlaceHolder; }  
         }  
   
         private long _maxSize = 10 * 1024 * 1024;  
         /// <summary>  
         /// トレースファイルの最大バイト数  
         /// </summary>  
         private long MaxSize  
         {  
             get { LoadAttribute(); return _maxSize; }  
         }  
         private Encoding _encoding = Encoding.GetEncoding("Shift_JIS");  
         /// <summary>  
         /// 出力ファイルのエンコーディング  
         /// </summary>  
         private Encoding Encoding  
         {  
             get { LoadAttribute(); return _encoding; }  
         }
         private long _saveFileNum = 10;
         /// <summary>  
         /// 保存しておくバックアップファイルの数  
         /// </summary>  
         private long SaveFileNum
         {
             get { LoadAttribute(); return _saveFileNum; }
         }  
  
         /// <summary>  
         /// 出力バッファストリーム  
         /// </summary>  
         private TextWriter _stream = null;  
         /// <summary>  
         /// 実際に出力されるストリーム  
         /// </summary>  
         private Stream _baseStream = null;  
         /// <summary>  
         /// 現在のログ日付  
         /// </summary>  
         private DateTime _logDate = DateTime.MinValue;  
         /// <summary>  
         /// バッファサイズ  
         /// </summary>  
         private int _bufferSize = 4096;  
         /// <summary>  
         /// ロックオブジェクト  
         /// </summary>  
         private object _lockObj = new Object();  
         /// <summary>  
         /// カスタム属性読み込みフラグ  
         /// </summary>  
         private bool _attributeLoaded = false;

        /// <summary>
        /// プロセスＩＤ
        /// </summary>
        private int processID = 0;
   
         /// <summary>  
         /// スレッドセーフ  
         /// </summary>  
         public override bool IsThreadSafe  
         {  
             get  
             {
                 return true;  
             }
         }

         /// <summary>  
         /// コンストラクタ  
         /// </summary>  
         /// <param name="fileNameTemplate">ファイル名のテンプレート</param>  
         public DailyTraceListener(string fileNameTemplate)  
         {  
             _fileNameTemplate = fileNameTemplate;

             this.getProcessID();
         }

        public DailyTraceListener(): base()
        {
            this.getProcessID();
        }

        /// <summary>
        /// プロセス数を取得する
        /// </summary>
        private void getProcessID()
        {
            this.processID = Process.GetCurrentProcess().Id;
        }


         /// <summary>  
         /// メッセージを出力します  
         /// </summary>  
         /// <param name="message"></param>  
         public override void Write(string message)  
         {  
             lock (_lockObj)  
             {  
                 if (EnsureTextWriter())  
                 {  
                     if (NeedIndent)  
                     {  
                         WriteIndent();  
                     }
                     _stream.Write(message);
                     //_stream.Write(DateTime.Now.ToString("yyyy/MM/dd HH:mm.ss.fff") + " : " + message);  
                 }  
             }  
         }  
         public override void WriteLine(string message)  
         {  
             Write(message + Environment.NewLine);  
         }  
         public override void Close()  
         {  
             lock (_lockObj)  
             {  
                 if (_stream != null)  
                 {  
                     _stream.Close();  
                 }  
                 _stream = null;  
                 _baseStream = null;  
             }  
         }  
         public override void Flush()  
         {  
             lock (_lockObj)  
             {  
                 if (_stream != null)  
                 {  
                     _stream.Flush();  
                 }  
             }  
         }  
         /// <summary>  
         /// 廃棄処理  
         /// </summary>  
         /// <param name="disposing"></param>  
         protected override void Dispose(bool disposing)  
         {  
             if (disposing)  
             {  
                 Close();  
             }  
             base.Dispose(disposing);  
         }  
   
         /// <summary>  
         /// 出力ストリームを準備する  
         /// </summary>  
         /// <returns></returns>  
         private bool EnsureTextWriter()  
         {  
             if (string.IsNullOrEmpty(FileNameTemplate)) return false;  
   
             DateTime now = DateTime.Now;  
             if (_logDate.Date != now.Date)  
             {  
                 Close();  
             }  
             if (_stream != null && _baseStream.Length > MaxSize)  
             {  
                 Close();  
             }  
             if (_stream == null)  
             {  
                 string filepath = NextFileName(now);  
                 // フルパスを求めると同時にファイル名に不正文字がないことの検証  
                 string fullpath = Path.GetFullPath(filepath);
                 
                 //ファイルが存在しない＝ファイルが切り替わった
                 if (!File.Exists(fullpath))
                 {
                     //過去ファイルの削除
                     DeleteFile(_saveFileNum);
                 }
   
                 StreamWriter writer = new StreamWriter(fullpath, true, Encoding, _bufferSize);  
                 _stream = writer;  
                 _baseStream = writer.BaseStream;  
                 _logDate = now;  
             }  
   
             return true;  
         }  
   
         /// <summary>  
         /// パスで指定されたディレクトリが存在しなければ  
         /// 作成します。  
         /// </summary>  
         /// <param name="dirpath">ディレクトリのパス</param>  
         /// <returns>作成した場合はtrue</returns>  
         private bool CreateDirectoryIfNotExists(string dirpath)  
         {  
             if (!Directory.Exists(dirpath))  
             {  
                 // 同時に作成してもエラーにならないため例外処理をしない  
                 Directory.CreateDirectory(dirpath);  
                 return true;  
             }  
             return false;  
         }  
         /// <summary>  
         /// 指定されたファイルがログファイルとして使用できるかの判定を行う  
         /// </summary>  
         /// <param name="filepath"></param>  
         /// <returns></returns>  
         private bool IsValidLogFile(string filepath)  
         {  
             if (File.Exists(filepath))  
             {  
                 FileInfo fi = new FileInfo(filepath);  
                 // 最大サイズより小さければ追記書き込みできるので OK  
                 if (fi.Length < MaxSize)  
                 {  
                     return true;  
                 }  
                 // そうでない場合はNG  
                 return false;  
             }  
             return true;  
         }  
         /// <summary>  
         /// 日付に基づくバージョンつきのログファイルのパスを作成する。  
         /// </summary>  
         /// <param name="logDateTime">ログ日付</param>  
         /// <returns></returns>  
         private string NextFileName(DateTime logDateTime)  
         {  
             string filepath = ResolveFileName(logDateTime.ToString(_dateFormat), this.processID);  
             string dir = Path.GetDirectoryName(filepath);  
             CreateDirectoryIfNotExists(dir);  
   
             return filepath;  
         }  
         /// <summary>  
         /// ファイル名のテンプレートから日付バージョンを置き換えるヘルパ
         /// </summary>  
         /// <param name="logDateTime"></param>  
         /// <param name="version"></param>  
         /// <returns></returns>  
         private string ResolveFileName(string strDateTime, int processID)  
         {
             //string appPath = Path.Combine(System.Windows.Forms.Application.LocalUserAppDataPath, "Trace");
             //String commonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
             //                                System.Windows.Forms.Application.CompanyName, System.Windows.Forms.Application.ProductName, "Trace");

             string tracePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                                     System.Windows.Forms.Application.CompanyName,
                                                     //System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion,
                                                     "MEME academic",
                                                     "Trace");

             string t = Path.Combine(tracePath, FileNameTemplate);  
             if (t.Contains(DatePlaceHolder))  
             {
                 t = t.Replace(DatePlaceHolder, strDateTime);
             }
             if (processID >= 0)
             {
                 string directoryName = Path.GetDirectoryName(t);
                 string fileName = Path.GetFileNameWithoutExtension(t);
                 string extName = Path.GetExtension(t);
                 t = string.Format("{0}\\{1}_{2}{3}", directoryName, fileName, processID, extName);
             }

             return t;  
         }

         /// <summary>  
         /// 保存数を超えた古いファイルを削除する  
         /// </summary>  
         /// <param name="saveFileNum"></param>  
         /// <returns></returns>  
         private void DeleteFile(long saveFileNum)
         {
             string filepath = ResolveFileName(DateTime.Now.ToString(_dateFormat), this.processID);
             string dir = Path.GetDirectoryName(filepath);

             if (!Directory.Exists(dir))
             {
                 return;
             }
             //ファイルエントリの取得
             String filter = Path.GetFileName(ResolveFileName("*", -1));
             String[] entries = Directory.GetFileSystemEntries(dir,filter);

             List<FileInfo> infoList = new List<FileInfo>();
             foreach (String entry in entries)
             {
                 FileInfo info = new FileInfo(entry);
                 infoList.Add(info);
             }
             //作成日時でソート
             infoList.Sort(delegate(FileInfo x, FileInfo y)
             {
                 TimeSpan total = x.CreationTime - y.CreationTime;
                 return (int)total.TotalSeconds;
             });
             //総ファイル数
             long totalNum = infoList.Count;
             //削除するファイル数
             long deleteNum = totalNum - saveFileNum + 1;
             if (deleteNum > 0)
             {
                 for (int i = 0; i < deleteNum; i++)
                 {
                     FileInfo info = infoList[i];
                     File.Delete(info.FullName);
                 }
             }
         }
         /// <summary>  
         /// サポートされているカスタム属性  
         /// MaxSize : ログファイルの最大サイズ  
         /// Encoding: 文字コード  
         /// DateFormat:ログファイル名の日付部分のフォーマット文字列  
         /// VersionFormat: ログファイルのバージョン部分のフォーマット文字列  
         /// DatePlaceHolder: ファイル名テンプレートの日付部分のプレースホルダ文字列  
         /// VersionPlaceHolder: ファイル名テンプレートのバージョブ部分のプレースホルダ文字列  
         /// SaveFileNum: 保存するファイル数  
         /// </summary>  
         /// <returns></returns>  
         protected override string[] GetSupportedAttributes()  
         {  
             return new string[] { "MaxSize", "Encoding", "DateFormat",   
                  "DatePlaceHolder", "SaveFileNum" };  
         }  
         /// <summary>  
         /// カスタム属性  
         /// </summary>  
         private void LoadAttribute()  
         {  
             if (!_attributeLoaded)  
             {  
                 // 最大バイト数  
                 if (Attributes.ContainsKey("MaxSize")) { _maxSize = long.Parse(Attributes["MaxSize"]); }  
                 // エンコーディング  
                 if (Attributes.ContainsKey("Encoding")) { _encoding = Encoding.GetEncoding(Attributes["Encoding"]); }  
                 // 日付のフォーマット  
                 if (Attributes.ContainsKey("DateFormat")) { _dateFormat = Attributes["DateFormat"]; }  
                 // 日付のプレースホルダ  
                 if (Attributes.ContainsKey("DatePlaceHolder")) { _datePlaceHolder = Attributes["DatePlaceHolder"]; }  
                 // 保存するファイル数  
                 if (Attributes.ContainsKey("SaveFileNum")) { _saveFileNum = long.Parse(Attributes["SaveFileNum"]); }  

                 _attributeLoaded = true;  
             }  
         }  

	}
}
