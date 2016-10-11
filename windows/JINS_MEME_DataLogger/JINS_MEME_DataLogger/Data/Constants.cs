using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// アプリケーション内で使用する定数を定義します。
    /// </summary>
    static class Constants
    {
        #region センシングパラメータ

        /// <summary>
        /// センシング間隔（単位：ms）
        /// </summary>
        public const int SENSING_INTERVAL = 10;

        /// <summary>
        /// センシングカウンター最大値
        /// </summary>
        public const int SENSING_COUNTER_MAX = 0x0FFF;

        #endregion センシングパラメータ


        #region 受信処理パラメータ

        /// <summary>
        /// 受信処理間隔（単位：ms）
        /// </summary>
        public const int RECEIVE_PROC_INTERVAL = 150;

        #endregion


        #region グラフ表示パラメータ

        /// <summary>
        /// グラフ更新間隔（単位：ms）
        /// </summary>
        public const int GRAPH_REFRASH_INTERVAL = 16;

        #endregion


        #region シリアル通信パラーメータ

        /// <summary>
        /// シリアル通信速度
        /// </summary>
        public const int SERIAL_BAUDRATE = 115200;

        /// <summary>
        /// シリアル通信データビット数
        /// </summary>
        public const int SERIAL_DATABITS = 8;

        /// <summary>
        /// シリアル通信パリティ値
        /// </summary>
        public const Parity SERIAL_PARITY = Parity.None;

        /// <summary>
        /// シリアル通信ストップビット値
        /// </summary>
        public const StopBits SERIAL_STOPBITS = StopBits.One;

        #endregion シリアル通信パラメータ



        #region 通信コマンド応答待ちタイマー値

        /// <summary>
        /// コマンド応答待ちタイマー値（６秒）
        /// </summary>
        public const int COMMAND_RESPONSE_TIMER = 6000;

        // TODO : 待ち時間を入れる。（定義）
        /// <summary>
        /// パラメータ設定待ち時間（１秒）
        /// </summary>
        public const int PARAMATER_SETTING_WAIT_1000 = 1000;
        public const int PARAMATER_SETTING_WAIT_500 = 500;
        public const int PARAMATER_SETTING_WAIT_100 = 100;

        #endregion 通信コマンド応答待ちタイマー値



        #region 通信コマンド応答値

        /// <summary>
        /// ＡＣＫ応答
        /// </summary>
        public const byte RESPONSE_ACK = 0x00;

        /// <summary>
        /// ＮＡＣＫ応答
        /// </summary>
        public const byte RESPONSE_NACK = 0xFF;

        /// <summary>
        /// デバイス接続切断
        /// </summary>
        public const byte RESPONSE_DISCONNECT = 0xF0;

        /// <summary>
        /// 応答タイムアウト
        /// </summary>
        public const byte RESPONSE_TIMEOUT = 0xF1;

        /// <summary>
        /// その他通信異常
        /// </summary>
        public const byte RESPONSE_ETC_ERROR = 0xF2;

        #endregion



        #region 通信コマンド（Device --> App）

        /// <summary>
        /// アカデミアデータ（スタンダード）通知
        /// </summary>
        public const byte CMD_AUP_REPORT_ACADEMIA1 = 0x98;

        /// <summary>
        /// アカデミアデータ（フル）通知
        /// </summary>
        public const byte CMD_AUP_REPORT_ACADEMIA2 = 0x99;

        /// <summary>
        /// アカデミアデータ（クォータニオン）通知
        /// </summary>
        public const byte CMD_AUP_REPORT_ACADEMIA3 = 0x9A;

        /// <summary>
        /// 機器情報通知
        /// </summary>
        public const byte CMD_AUP_REPORT_DEV_INFO = 0x81;

        /// <summary>
        /// 動作モード通知
        /// </summary>
        public const byte CMD_AUP_REPORT_MODE = 0x83;

        /// <summary>
        /// ６軸センサパラメータ通知
        /// </summary>
        public const byte CMD_AUP_REPORT_6AXIS_PARAMS = 0x89;

        /// <summary>
        /// レスポンス（ACK/NACK）通知
        /// </summary>
        public const byte CMD_AUP_REPORT_RESP = 0x8F;

        #endregion 通信コマンド（Device --> App）



        #region 通信コマンドサイズ（Device --> App）

        /// <summary>
        /// アカデミアデータ（スタンダード）通知サイズ
        /// </summary>
        public const byte SIZE_AUP_REPORT_ACADEMIA1 = 0x14;

        /// <summary>
        /// アカデミアデータ（フル）通知サイズ
        /// </summary>
        public const byte SIZE_AUP_REPORT_ACADEMIA2 = 0x14;

        /// <summary>
        /// アカデミアデータ（クォータニオン）通知サイズ
        /// </summary>
        public const byte SIZE_AUP_REPORT_ACADEMIA3 = 0x14;

        /// <summary>
        /// 機器情報通知サイズ
        /// </summary>
        public const byte SIZE_AUP_REPORT_DEV_INFO = 0x14;

        /// <summary>
        /// 動作モード通知サイズ
        /// </summary>
        public const byte SIZE_AUP_REPORT_MODE = 0x14;

        /// <summary>
        /// ６軸センサパラメータ通知サイズ
        /// </summary>
        public const byte SIZE_AUP_REPORT_6AXIS_PARAMS = 0x14;

        /// <summary>
        /// レスポンス（ACK/NACK）通知サイズ
        /// </summary>
        public const byte SIZE_AUP_REPORT_RESP = 0x14;

        #endregion 通信コマンドサイズ（Device --> App）



        #region 通信コマンド（App --> Device）

        /// <summary>
        /// データ送信開始／停止
        /// </summary>
        public const byte CMD_ADN_START_STOP_SEND = 0xA0;

        /// <summary>
        /// 機器情報取得
        /// </summary>
        public const byte CMD_ADN_DEV_INFO = 0xA1;

        /// <summary>
        /// 動作モード取得
        /// </summary>
        public const byte CMD_ADN_GET_MODE = 0xA3;

        /// <summary>
        /// 動作モード設定
        /// </summary>
        public const byte CMD_ADN_SET_MODE = 0xA4;

        /// <summary>
        /// パラメータ設定値クリア
        /// </summary>
        public const byte CMD_ADN_CLR_PARAMS = 0xA8;

        /// <summary>
        /// ６軸センサパラメータ設定値取得
        /// </summary>
        public const byte CMD_ADN_GET_6AXIS_PARAMS = 0xA9;

        /// <summary>
        /// ６軸センサパラメータ設定
        /// </summary>
        public const byte CMD_ADN_SET_6AXIS_PARAMS = 0xAA;

        #endregion 通信コマンド（App --> Device）



        #region 通信コマンドサイズ（App --> Device）

        /// <summary>
        /// データ送信開始／停止サイズ
        /// </summary>
        public const byte SIZE_ADN_START_STOP_SEND = 0x14;

        /// <summary>
        /// 機器情報取得サイズ
        /// </summary>
        public const byte SIZE_ADN_DEV_INFO = 0x14;

        /// <summary>
        /// 動作モード取得サイズ
        /// </summary>
        public const byte SIZE_ADN_GET_MODE = 0x14;

        /// <summary>
        /// 動作モード設定サイズ
        /// </summary>
        public const byte SIZE_ADN_SET_MODE = 0x14;

        /// <summary>
        /// パラメータ設定値クリアサイズ
        /// </summary>
        public const byte SIZE_ADN_CLR_PARAMS = 0x14;

        /// <summary>
        /// ６軸センサパラメータ設定値取得サイズ
        /// </summary>
        public const byte SIZE_ADN_GET_6AXIS_PARAMS = 0x14;

        /// <summary>
        /// ６軸センサパラメータ設定
        /// </summary>
        public const byte SIZE_ADN_SET_6AXIS_PARAMS = 0x14;

        #endregion 通信コマンドサイズ（App --> Device）



        #region 通信コマンド（ドングル --> App）

        /// <summary>
        /// デバイス名取得応答
        /// </summary>
        public const byte CMD_DON_RES_DEVICE_NAME = 0x11;

        /// <summary>
        /// デバイスバージョン取得応答
        /// </summary>
        public const byte CMD_DON_RES_DEVICE_VERSION = 0x21;

        /// <summary>
        /// Bluetoothデバイス通知（MACアドレス）
        /// </summary>
        public const byte CMD_DON_RES_SCAN_BLUETOOTH = 0x31;
        /// <summary>
        /// Bluetoothデバイス通知（スキャン終了）
        /// </summary>
        public const byte CMD_DON_RES_SCAN_END = 0x32;

        /// <summary>
        /// Bluetoothデバイス接続通知
        /// </summary>
        public const byte CMD_DON_RES_CONNECT_BLUETOOTH = 0x41;
        /// <summary>
        /// Bluetoothデバイス接続異常通知
        /// </summary>
        public const byte CMD_DON_RES_CONNECT_BLUETOOTH_ERROR = 0x42;

        /// <summary>
        /// Bluetoothデバイス切断通知
        /// </summary>
        public const byte CMD_DON_RES_DISCONNECT_BLUETOOTH = 0x51;
        /// <summary>
        /// Bluetoothデバイス切断通知（Supervision Timeout）
        /// </summary>
        public const byte CMD_DON_RES_SUPERVISION_TIMEOUT = 0x52;

        #endregion 通信コマンド（ドングル --> App）



        #region 通信コマンドサイズ（ドングル --> App）

        /// <summary>
        /// デバイス名取得応答サイズ
        /// </summary>
        public const byte SIZE_DON_RES_DEVICE_NAME = 0x0A;

        /// <summary>
        /// デバイスバージョン取得応答
        /// </summary>
        public const byte SIZE_DON_RES_DEVICE_VERSION = 0x0B;

        /// <summary>
        /// Bluetoothデバイス通知（MACアドレス）サイズ
        /// </summary>
        public const byte SIZE_DON_RES_SCAN_BLUETOOTH = 0x0F;

        /// <summary>
        /// Bluetoothデバイス接続通知サイズ
        /// </summary>
        public const byte SIZE_DON_RES_CONNECT_BLUETOOTH = 0x03;

        /// <summary>
        /// Bluetoothデバイス切断通知サイズ
        /// </summary>
        public const byte SIZE_DON_RES_DISCONNECT_BLUETOOTH = 0x03;

        #endregion 通信コマンドサイズ（ドングル --> App）



        #region 通信コマンド（App --> ドングル）

        /// <summary>
        /// ドングルコマンド（@ = 0x4E）
        /// </summary>
        public const byte CMD_DONGLE = 0x4E;

        /// <summary>
        /// デバイス名取得要求
        /// </summary>
        public const byte CMD_DON_REQ_DEVICE_NAME = 0x10;

        /// <summary>
        /// デバイスバージョン取得
        /// </summary>
        public const byte CMD_DON_REQ_DEVICE_VERSION = 0x20;

        /// <summary>
        /// Bluetoothデバイス検索
        /// </summary>
        public const byte CMD_DON_REQ_SCAN_BLUETOOTH = 0x30;

        /// <summary>
        /// Bluetoothデバイス接続
        /// </summary>
        public const byte CMD_DON_REQ_CONNECT_BLUETOOTH = 0x40;

        /// <summary>
        /// Bluetoothデバイス切断
        /// </summary>
        public const byte CMD_DON_REQ_DISCONNECT_BLUETOOTH = 0x50;

        #endregion 通信コマンド（App --> ドングル）



        #region 通信コマンドサイズ（App --> ドングル）

        /// <summary>
        /// デバイス名取得要求サイズ
        /// </summary>
        public const byte SIZE_DON_REQ_DEVICE_NAME = 0x03;

        /// <summary>
        /// デバイスバージョン取得サイズ
        /// </summary>
        public const byte SIZE_DON_REQ_DEVICE_VERSION = 0x03;

        /// <summary>
        /// Bluetoothデバイス検索サイズ
        /// </summary>
        public const byte SIZE_DON_REQ_SCAN_BLUETOOTH = 0x03;

        /// <summary>
        /// Bluetoothデバイス接続サイズ
        /// </summary>
        public const byte SIZE_DON_REQ_CONNECT_BLUETOOTH = 0x0F;

        /// <summary>
        /// Bluetoothデバイス切断サイズ
        /// </summary>
        public const byte SIZE_DON_REQ_DISCONNECT_BLUETOOTH = 0x03;

        #endregion 通信コマンドサイズ（App --> ドングル）


        #region Bluetoothドングルデータ

        /// <summary>
        /// ドングル名
        /// </summary>
        public const string DONGLE_NAME = "JDG-A01";

        /// <summary>
        /// バージョン情報
        /// </summary>
        public const string DONGLE_VERSION = "00.01.01";

        #endregion Bluetoothドングルデータ


        #region 測定値ファイルヘッダー文字列

        /// <summary>
        /// コメント行
        /// </summary>
        public const string FILE_HEADER_COMMENT = "//";

        /// <summary>
        /// データモード行
        /// </summary>
        public const string FILE_HEADER_DATA_MODE = "// Data mode  : ";

        /// <summary>
        /// 送信速度（データ品質）行
        /// </summary>
        public const string FILE_HEADER_TRANSMISSION_SPEED = "// Transmission speed  : ";

        /// <summary>
        /// 加速度センサーレンジ行
        /// </summary>
        public const string FILE_HEADER_ACC_RANGE = "// Accelerometer sensor's range  : ";

        /// <summary>
        /// 角速度センサーレンジ行
        /// </summary>
        public const string FILE_HEADER_GYRO_RANGE = "// Gyroscope sensor's range  : ";

        #endregion 測定値ファイルヘッダー文字列
    }
}
