using System.Buffers.Binary;
using System.Text.RegularExpressions;

namespace MEMELib_Academic;

/// <summary>
/// ES_R の GATT プロファイルと 20 byte ADN/AUP プロトコル。BLE スタックに一切
/// 依存しない純粋なロジックだけを置き、単体テストの対象にする。
/// 出典は Mac 版 <c>MEMELib_Academic.swift</c> と Android 版
/// <c>MemeBleConstants.kt</c> / <c>MemeCommands.kt</c> / <c>DataParser.kt</c>。
/// </summary>
public static partial class MemeProtocol
{
    public static readonly Guid ServiceUuid = new("D6F25BD1-5B54-4360-96D8-7AA62E04C7EF");

    /// <summary>端末 → PC。Notify で受信する。</summary>
    public static readonly Guid RxCharacteristicUuid = new("D6F25BD4-5B54-4360-96D8-7AA62E04C7EF");

    /// <summary>PC → 端末。Write with response で送信する。</summary>
    public static readonly Guid TxCharacteristicUuid = new("D6F25BD2-5B54-4360-96D8-7AA62E04C7EF");

    public const int PacketLength = 20;

    // ADN: PC → 端末
    public const byte AdnStartStopSend = 0xA0;
    public const byte AdnGetDevInfo = 0xA1;
    public const byte AdnGetMode = 0xA3;
    public const byte AdnSetMode = 0xA4;
    public const byte AdnGet6AxisParams = 0xA9;
    public const byte AdnSet6AxisParams = 0xAA;

    // AUP: 端末 → PC
    public const byte AupReportDevInfo = 0x81;
    public const byte AupReportMode = 0x83;
    public const byte AupReport6AxisParams = 0x89;
    public const byte AupReportResp = 0x8F;
    public const byte AupReportAcademia1 = 0x98;
    public const byte AupReportAcademia2 = 0x99;
    public const byte AupReportAcademia3 = 0x9A;

    /// <summary>
    /// ES_R 実機の広告名(JINS MEME SDK の "JINSG2_[0-5]" に対応する ES_R 版)。
    /// サービス UUID を広告に載せない個体や、ペアリング済みで広告を出していない
    /// 端末を拾うためのフォールバック判定に使う。
    /// </summary>
    public static Regex DeviceNameRegex => DeviceNameRegexImpl();

    [GeneratedRegex("^ESRG2_[0-5]$")]
    private static partial Regex DeviceNameRegexImpl();

    private static byte[] Command(byte opcode)
    {
        var buf = new byte[PacketLength];
        buf[0] = PacketLength;
        buf[1] = opcode;
        return buf;
    }

    public static byte[] GetDeviceInfo() => Command(AdnGetDevInfo);

    public static byte[] GetMode() => Command(AdnGetMode);

    public static byte[] SetMode(MEMEMode mode, MEMEQuality quality)
    {
        var buf = Command(AdnSetMode);
        buf[4] = (byte)mode;
        buf[5] = (byte)quality;
        return buf;
    }

    public static byte[] Get6AxisParams() => Command(AdnGet6AxisParams);

    public static byte[] Set6AxisParams(MEMEAccelRange accelRange, MEMEGyroRange gyroRange)
    {
        var buf = Command(AdnSet6AxisParams);
        buf[2] = (byte)accelRange;
        buf[3] = (byte)gyroRange;
        return buf;
    }

    /// <summary>計測の開始(true)／停止(false)。</summary>
    public static byte[] StartStop(bool start)
    {
        var buf = Command(AdnStartStopSend);
        buf[2] = start ? (byte)0x01 : (byte)0x00;
        return buf;
    }

    /// <summary>先頭 2 byte が 20 byte パケットの体裁になっているか。</summary>
    public static bool IsValidPacket(ReadOnlySpan<byte> packet) =>
        packet.Length >= PacketLength && packet[0] == PacketLength;

    /// <summary>
    /// AUP_REPORT_ACADEMIA2(0x99) を 1 サンプルへ展開する。
    /// レイアウトは cnt(12bit) / battLv(4bit) / acc・gyro・eog を LE の符号付き 16bit。
    /// </summary>
    public static AcademicFullData ParseFullData(ReadOnlySpan<byte> p)
    {
        if (!IsValidPacket(p))
        {
            throw new ArgumentException("not a 20-byte MEME packet", nameof(p));
        }

        var data = new AcademicFullData
        {
            Cnt = ((p[3] << 8) & 0x0F00) | p[2],
            BattLv = p[3] >> 4,
            AccX = ReadInt16(p, 4),
            AccY = ReadInt16(p, 6),
            AccZ = ReadInt16(p, 8),
            GyroX = ReadInt16(p, 10),
            GyroY = ReadInt16(p, 12),
            GyroZ = ReadInt16(p, 14),
            EogL = ReadInt16(p, 16),
            EogR = ReadInt16(p, 18),
        };

        // Mac 版と同じくラップアラウンドを許容した 16bit 演算で導出する。
        data.EogH = unchecked((short)(data.EogL - data.EogR));
        data.EogV = unchecked((short)(0 - ((data.EogL + data.EogR) / 2)));
        return data;
    }

    /// <summary>AUP_REPORT_DEV_INFO(0x81) のファームウェアバージョン。</summary>
    public static Version ParseFirmwareVersion(ReadOnlySpan<byte> p) => new(p[6], p[5], p[4]);

    private static short ReadInt16(ReadOnlySpan<byte> p, int offset) =>
        BinaryPrimitives.ReadInt16LittleEndian(p.Slice(offset, 2));
}
