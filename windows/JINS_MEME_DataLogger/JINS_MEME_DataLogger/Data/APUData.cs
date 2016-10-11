using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JINS_MEME_DataLogger.Data
{
    #region APU_REPORT_ACADEMIA1 アクセス

    /// <summary>
    /// APU_REPORT_ACADEMIA1 アクセスクラスです。
    /// </summary>
    public class APU_REPORT_ACADEMIA1
    {
        /// <summary>
        /// デバイスデータ
        /// </summary>
        private byte[] deviceData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public APU_REPORT_ACADEMIA1(byte[] data)
        {
            this.deviceData = data;
        }

        /// <summary>
        /// データ長取得
        /// </summary>
        public byte Length
        {
            get { return this.deviceData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public byte EventCode
        {
            get { return this.deviceData[1]; }
        }

        /// <summary>
        /// カウンター取得
        /// </summary>
        public Int32 Counter
        {
            get { return ((this.deviceData[3] & 0x0F) << 8) + this.deviceData[2]; }
        }

        /// <summary>
        /// 電池残量取得
        /// </summary>
        public Int32 Battery
        {
            get { return (this.deviceData[3] >> 4) & 0x0F; }
        }

        /// <summary>
        /// 加速度センサ（Ｘ軸）取得
        /// </summary>
        public Int32 AccX
        {
            get { return (Int16)((this.deviceData[5] << 8) + this.deviceData[4]); }
        }

        /// <summary>
        /// 加速度センサ（Ｙ軸）取得
        /// </summary>
        public Int32 AccY
        {
            get { return (Int16)((this.deviceData[7] << 8) + this.deviceData[6]); }
        }

        /// <summary>
        /// 加速度センサ（Ｚ軸）取得
        /// </summary>
        public Int32 AccZ
        {
            get { return (Int16)((this.deviceData[9] << 8) + this.deviceData[8]); }
        }

        /// <summary>
        /// 人感センサ（左）１取得
        /// </summary>
        public Int32 EogL1
        {
            get { return (Int16)((this.deviceData[11] << 8) + this.deviceData[10]); }
        }

        /// <summary>
        /// 人感センサ（右）１取得
        /// </summary>
        public Int32 EogR1
        {
            get { return (Int16)((this.deviceData[13] << 8) + this.deviceData[12]); }
        }

        /// <summary>
        /// 人感センサ（左）２取得
        /// </summary>
        public Int32 EogL2
        {
            get { return (Int16)((this.deviceData[15] << 8) + this.deviceData[14]); }
        }

        /// <summary>
        /// 人感センサ（右）２取得
        /// </summary>
        public Int32 EogR2
        {
            get { return (Int16)((this.deviceData[17] << 8) + this.deviceData[16]); }
        }
    }

    #endregion APU_REPORT_ACADEMIA1 アクセス



    #region APU_REPORT_ACADEMIA2 アクセス

    /// <summary>
    /// APU_REPORT_ACADEMIA2 アクセスクラスです。
    /// </summary>
    public class APU_REPORT_ACADEMIA2
    {
        /// <summary>
        /// デバイスデータ
        /// </summary>
        private byte[] deviceData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public APU_REPORT_ACADEMIA2(byte[] data)
        {
            this.deviceData = data;
        }

        /// <summary>
        /// データ長取得
        /// </summary>
        public byte Length
        {
            get { return this.deviceData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public byte EventCode
        {
            get { return this.deviceData[1]; }
        }

        /// <summary>
        /// カウンター取得
        /// </summary>
        public Int32 Counter
        {
            get { return ((this.deviceData[3] & 0x0F) << 8) + this.deviceData[2]; }
        }

        /// <summary>
        /// 電池残量取得
        /// </summary>
        public Int32 Battery
        {
            get { return (this.deviceData[3] >> 4) & 0x0F; }
        }

        /// <summary>
        /// 加速度センサ（Ｘ軸）取得
        /// </summary>
        public Int32 AccX
        {
            get { return (Int16)((this.deviceData[5] << 8) + this.deviceData[4]); }
        }

        /// <summary>
        /// 加速度センサ（Ｙ軸）取得
        /// </summary>
        public Int32 AccY
        {
            get { return (Int16)((this.deviceData[7] << 8) + this.deviceData[6]); }
        }

        /// <summary>
        /// 加速度センサ（Ｚ軸）取得
        /// </summary>
        public Int32 AccZ
        {
            get { return (Int16)((this.deviceData[9] << 8) + this.deviceData[8]); }
        }

        /// <summary>
        /// 角速度センサ（Ｘ軸）取得
        /// </summary>
        public Int32 GyroX
        {
            get { return (Int16)((this.deviceData[11] << 8) + this.deviceData[10]); }
        }

        /// <summary>
        /// 角速度センサ（Ｙ軸）取得
        /// </summary>
        public Int32 GyroY
        {
            get { return (Int16)((this.deviceData[13] << 8) + this.deviceData[12]); }
        }

        /// <summary>
        /// 角速度センサ（Ｚ軸）取得
        /// </summary>
        public Int32 GyroZ
        {
            get { return (Int16)((this.deviceData[15] << 8) + this.deviceData[14]); }
        }

        /// <summary>
        /// 人感センサ（左）取得
        /// </summary>
        public Int32 EogL
        {
            get { return (Int16)((this.deviceData[17] << 8) + this.deviceData[16]); }
        }

        /// <summary>
        /// 人感センサ（右）取得
        /// </summary>
        public Int32 EogR
        {
            get { return (Int16)((this.deviceData[19] << 8) + this.deviceData[18]); }
        }
    }

    #endregion APU_REPORT_ACADEMIA2 アクセス



    #region APU_REPORT_ACADEMIA3 アクセス

    /// <summary>
    /// APU_REPORT_ACADEMIA3 アクセスクラスです。
    /// </summary>
    public class APU_REPORT_ACADEMIA3
    {
        /// <summary>
        /// デバイスデータ
        /// </summary>
        private byte[] deviceData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public APU_REPORT_ACADEMIA3(byte[] data)
        {
            this.deviceData = data;
        }

        /// <summary>
        /// データ長取得
        /// </summary>
        public byte Length
        {
            get { return this.deviceData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public byte EventCode
        {
            get { return this.deviceData[1]; }
        }

        /// <summary>
        /// カウンター取得
        /// </summary>
        public Int32 Counter
        {
            get { return ((this.deviceData[3] & 0x0F) << 8) + this.deviceData[2]; }
        }

        /// <summary>
        /// 電池残量取得
        /// </summary>
        public Int32 Battery
        {
            get { return (this.deviceData[3] >> 4) & 0x0F; }
        }

        /// <summary>
        /// クォータニオン（Ｗ）取得
        /// </summary>
        public Int32 QuaternionW
        {
            get { return (this.deviceData[7] << 24) + (this.deviceData[6] << 16) + (this.deviceData[5] << 8) + this.deviceData[4]; }
        }

        /// <summary>
        /// クォータニオン（Ｘ）取得
        /// </summary>
        public Int32 QuaternionX
        {
            get { return (this.deviceData[11] << 24) + (this.deviceData[10] << 16) + (this.deviceData[9] << 8) + this.deviceData[8]; }
        }

        /// <summary>
        /// クォータニオン（Ｙ）取得
        /// </summary>
        public Int32 QuaternionY
        {
            get { return (this.deviceData[15] << 24) + (this.deviceData[14] << 16) + (this.deviceData[13] << 8) + this.deviceData[12]; }
        }

        /// <summary>
        /// クォータニオン（Ｚ）取得
        /// </summary>
        public Int32 QuaternionZ
        {
            get { return (this.deviceData[19] << 24) + (this.deviceData[18] << 16) + (this.deviceData[17] << 8) + this.deviceData[16]; }
        }
    }

    #endregion APU_REPORT_ACADEMIA3 アクセス



    #region APU_REPORT_DEV_INFO アクセス

    /// <summary>
    /// APU_REPORT_DEV_INFO アクセスクラスです。
    /// </summary>
    public class APU_REPORT_DEV_INFO
    {
        /// <summary>
        /// デバイスデータ
        /// </summary>
        private byte[] deviceData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public APU_REPORT_DEV_INFO(byte[] data)
        {
            this.deviceData = data;
        }

        /// <summary>
        /// データ長取得
        /// </summary>
        public byte Length
        {
            get { return this.deviceData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public byte EventCode
        {
            get { return this.deviceData[1]; }
        }

        /// <summary>
        /// Model Lo 取得（ファームウェアバージョン）
        /// </summary>
        public string FWVModelLo
        {
            get { return string.Format("{0:X2}", deviceData[2]); }
        }

        /// <summary>
        /// Model Hi 取得（ファームウェアバージョン）
        /// </summary>
        public string FWVModelHi
        {
            get { return string.Format("{0:X}", deviceData[3]); }
        }

        /// <summary>
        /// Revision 取得（ファームウェアバージョン）
        /// </summary>
        public string FWVRevision
        {
            get { return string.Format("{0:X2}", deviceData[4]); }
        }

        /// <summary>
        /// Minor 取得（ファームウェアバージョン）
        /// </summary>
        public string FWVMinor
        {
            get { return string.Format("{0:X2}", deviceData[5]); }
        }

        /// <summary>
        /// Major 取得（ファームウェアバージョン）
        /// </summary>
        public string FWVMajor
        {
            get { return string.Format("{0:X2}", deviceData[6]); }
        }

        /// <summary>
        /// HW 取得（ハードウェアバージョン）
        /// </summary>
        public string HWVersion
        {
            get { return string.Format("{0:X2}", deviceData[7]); }
        }
    }

    #endregion APU_REPORT_DEV_INFO アクセス



    #region APU_REPORT_MODE アクセス

    /// <summary>
    /// APU_REPORT_MODE アクセスクラスです。
    /// </summary>
    public class APU_REPORT_MODE
    {
        /// <summary>
        /// デバイスデータ
        /// </summary>
        private byte[] deviceData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public APU_REPORT_MODE(byte[] data)
        {
            this.deviceData = data;
        }

        /// <summary>
        /// データ長取得
        /// </summary>
        public byte Length
        {
            get { return this.deviceData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public byte EventCode
        {
            get { return this.deviceData[1]; }
        }

        /// <summary>
        /// アカデミアモード取得
        /// </summary>
        public byte AcademiaMode
        {
            get { return this.deviceData[4]; }
        }

        /// <summary>
        /// 送信速度（データ品質）取得
        /// </summary>
        public byte TransmissionSpeed
        {
            get { return this.deviceData[5]; }
        }
    }

    #endregion APU_REPORT_MODE アクセス



    #region APU_REPORT_6AXIS_PARAMS アクセス

    /// <summary>
    /// APU_REPORT_6AXIS_PARAMS アクセスクラスです。
    /// </summary>
    public class APU_REPORT_6AXIS_PARAMS
    {
        /// <summary>
        /// デバイスデータ
        /// </summary>
        private byte[] deviceData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public APU_REPORT_6AXIS_PARAMS(byte[] data)
        {
            this.deviceData = data;
        }

        /// <summary>
        /// データ長取得
        /// </summary>
        public byte Length
        {
            get { return this.deviceData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public byte EventCode
        {
            get { return this.deviceData[1]; }
        }

        /// <summary>
        /// 加速度センサ測定レンジ取得
        /// </summary>
        public byte AccRange
        {
            get { return this.deviceData[2]; }
        }

        /// <summary>
        /// 角速度センサ測定レンジ取得
        /// </summary>
        public byte GyroRange
        {
            get { return this.deviceData[3]; }
        }
    }

    #endregion APU_REPORT_6AXIS_PARAMS アクセス



    #region APU_REPORT_RESP アクセス

    /// <summary>
    /// APU_REPORT_RESP アクセスクラスです。
    /// </summary>
    public class APU_REPORT_RESP
    {
        /// <summary>
        /// デバイスデータ
        /// </summary>
        private byte[] deviceData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public APU_REPORT_RESP(byte[] data)
        {
            this.deviceData = data;
        }

        /// <summary>
        /// データ長取得
        /// </summary>
        public byte Length
        {
            get { return this.deviceData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public byte EventCode
        {
            get { return this.deviceData[1]; }
        }

        /// <summary>
        /// Result取得
        /// </summary>
        public byte Result
        {
            get { return this.deviceData[2]; }
        }
    }

    #endregion APU_REPORT_RESP アクセス



    #region DON_RES_DEVICE_NAME アクセス

    /// <summary>
    /// DON_RES_DEVICE_NAME アクセスクラスです。
    /// </summary>
    public class DON_RES_DEVICE_NAME
    {
        /// <summary>
        /// デバイスデータ
        /// </summary>
        private byte[] deviceData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public DON_RES_DEVICE_NAME(byte[] data)
        {
            this.deviceData = data;
        }

        /// <summary>
        /// データ長取得
        /// </summary>
        public byte Length
        {
            get { return this.deviceData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public byte EventCode
        {
            get { return this.deviceData[1]; }
        }

        /// <summary>
        /// サブコード取得
        /// </summary>
        public byte SubCode
        {
            get { return this.deviceData[2]; }
        }

        /// <summary>
        /// デバイス名取得
        /// </summary>
        public string DeviceName
        {
            get { return Encoding.ASCII.GetString(deviceData, 3, deviceData.Length - 3); }
        }
    }

    #endregion DON_RES_DEVICE_NAME アクセス



    #region DON_RES_DEVICE_VERSION アクセス

    /// <summary>
    /// DON_RES_DEVICE_NAME アクセスクラスです。
    /// </summary>
    public class DON_RES_DEVICE_VERSION
    {
        /// <summary>
        /// デバイスデータ
        /// </summary>
        private byte[] deviceData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public DON_RES_DEVICE_VERSION(byte[] data)
        {
            this.deviceData = data;
        }

        /// <summary>
        /// データ長取得
        /// </summary>
        public byte Length
        {
            get { return this.deviceData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public byte EventCode
        {
            get { return this.deviceData[1]; }
        }

        /// <summary>
        /// サブコード取得
        /// </summary>
        public byte SubCode
        {
            get { return this.deviceData[2]; }
        }

        /// <summary>
        /// デバイスバージョン取得
        /// </summary>
        public string DeviceVersion
        {
            get { return Encoding.ASCII.GetString(deviceData, 3, deviceData.Length - 3); }
        }
    }

    #endregion DON_RES_DEVICE_VERSION アクセス



    #region DON_RES_SCAN_BLUETOOTH アクセス

    /// <summary>
    /// DON_RES_SCAN_BLUETOOTH アクセスクラスです。
    /// </summary>
    public class DON_RES_SCAN_BLUETOOTH
    {
        /// <summary>
        /// デバイスデータ
        /// </summary>
        private byte[] deviceData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public DON_RES_SCAN_BLUETOOTH(byte[] data)
        {
            this.deviceData = data;
        }

        /// <summary>
        /// データ長取得
        /// </summary>
        public byte Length
        {
            get { return this.deviceData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public byte EventCode
        {
            get { return this.deviceData[1]; }
        }

        /// <summary>
        /// サブコード取得
        /// </summary>
        public byte SubCode
        {
            get { return this.deviceData[2]; }
        }

        /// <summary>
        /// MACアドレス取得
        /// </summary>
        public string MacAddress
        {
            get { return Encoding.ASCII.GetString(deviceData, 3, deviceData.Length - 3); }
        }
    }

    #endregion DON_RES_SCAN_BLUETOOTH アクセス



    #region DON_RES_CONNECT_BLUETOOTH アクセス

    /// <summary>
    /// DON_RES_CONNECT_BLUETOOTH アクセスクラスです。
    /// </summary>
    public class DON_RES_CONNECT_BLUETOOTH
    {
        /// <summary>
        /// デバイスデータ
        /// </summary>
        private byte[] deviceData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public DON_RES_CONNECT_BLUETOOTH(byte[] data)
        {
            this.deviceData = data;
        }

        /// <summary>
        /// データ長取得
        /// </summary>
        public byte Length
        {
            get { return this.deviceData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public byte EventCode
        {
            get { return this.deviceData[1]; }
        }

        /// <summary>
        /// サブコード取得
        /// </summary>
        public byte SubCode
        {
            get { return this.deviceData[2]; }
        }
    }

    #endregion DON_RES_CONNECT_BLUETOOTH アクセス

}
