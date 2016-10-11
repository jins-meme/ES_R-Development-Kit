using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JINS_MEME_DataLogger.Data;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// センサーデータにアクセスする為の処理を記載します。
    /// </summary>
    public class SensorDataArgs : EventArgs
    {
        public bool Marking;
        public byte DataType;
        public byte[] SensorData;

        /// <summary>
        /// データ長取得
        /// </summary>
        public uint Length
        {
            get { return (uint)SensorData[0]; }
        }

        /// <summary>
        /// イベントコード取得
        /// </summary>
        public uint EventCode
        {
            get { return (uint)SensorData[1]; }
        }

        /// <summary>
        /// APU_REPORT_ACADEMIA1へ型変換
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static implicit operator APU_REPORT_ACADEMIA1(SensorDataArgs args)
        {
            return new APU_REPORT_ACADEMIA1(args.SensorData);
        }

        /// <summary>
        /// APU_REPORT_ACADEMIA2へ型変換
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static implicit operator APU_REPORT_ACADEMIA2(SensorDataArgs args)
        {
            return new APU_REPORT_ACADEMIA2(args.SensorData);
        }

        /// <summary>
        /// APU_REPORT_ACADEMIA3へ型変換
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static implicit operator APU_REPORT_ACADEMIA3(SensorDataArgs args)
        {
            return new APU_REPORT_ACADEMIA3(args.SensorData);
        }

        /// <summary>
        /// APU_REPORT_DEV_INFOへ型変換
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static implicit operator APU_REPORT_DEV_INFO(SensorDataArgs args)
        {
            return new APU_REPORT_DEV_INFO(args.SensorData);
        }

        /// <summary>
        /// APU_REPORT_MODEへ型変換
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static implicit operator APU_REPORT_MODE(SensorDataArgs args)
        {
            return new APU_REPORT_MODE(args.SensorData);
        }

        /// <summary>
        /// APU_REPORT_6AXIS_PARAMSへ型変換
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static implicit operator APU_REPORT_6AXIS_PARAMS(SensorDataArgs args)
        {
            return new APU_REPORT_6AXIS_PARAMS(args.SensorData);
        }

        /// <summary>
        /// APU_REPORT_RESPへ型変換
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static implicit operator APU_REPORT_RESP(SensorDataArgs args)
        {
            return new APU_REPORT_RESP(args.SensorData);
        }





        /// <summary>
        /// DON_RES_DEVICE_NAMEへ型変換
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static implicit operator DON_RES_DEVICE_NAME(SensorDataArgs args)
        {
            return new DON_RES_DEVICE_NAME(args.SensorData);
        }

        /// <summary>
        /// DON_RES_DEVICE_VERSIONへ型変換
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static implicit operator DON_RES_DEVICE_VERSION(SensorDataArgs args)
        {
            return new DON_RES_DEVICE_VERSION(args.SensorData);
        }

        /// <summary>
        /// DON_RES_SCAN_BLUETOOTHへ型変換
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static implicit operator DON_RES_SCAN_BLUETOOTH(SensorDataArgs args)
        {
            return new DON_RES_SCAN_BLUETOOTH(args.SensorData);
        }

        /// <summary>
        /// DON_RES_CONNECT_BLUETOOTHへ型変換
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static implicit operator DON_RES_CONNECT_BLUETOOTH(SensorDataArgs args)
        {
            return new DON_RES_CONNECT_BLUETOOTH(args.SensorData);
        }

    }
}
