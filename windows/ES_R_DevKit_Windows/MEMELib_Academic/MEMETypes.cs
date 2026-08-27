namespace MEMELib_Academic;

/// <summary>API 呼び出しの結果。旧 MEMELib_Academic.dll と同じ名前を維持している。</summary>
public enum MEMEStatus
{
    MEMELIB_OK = 0,
    MEMELIB_NG = 1,
    MEMELIB_TIMEOUT = 2,
}

/// <summary>
/// 計測モード。値は ADN_SET_MODE(0xA4) / AUP_REPORT_MODE(0x83) の byte4 に載る値そのもの。
/// Android 版の MemeMode.ordinal + 1、Mac 版の MEMEMode_Full = 2 と一致する。
/// </summary>
public enum MEMEMode
{
    Standard = 1,
    Full = 2,
    Quaternion = 3,
}

/// <summary>転送レート。0xA4 の byte5 に載る値。High = 100Hz。</summary>
public enum MEMEQuality
{
    High = 1,
    Low = 2,
}

/// <summary>加速度センサーのレンジ。値は ADN_SET_6AXIS_PARAMS(0xAA) の byte2。</summary>
public enum MEMEAccelRange
{
    Range2G = 0,
    Range4G = 1,
    Range8G = 2,
    Range16G = 3,
}

/// <summary>ジャイロセンサーのレンジ。値は ADN_SET_6AXIS_PARAMS(0xAA) の byte3。</summary>
public enum MEMEGyroRange
{
    Range250dps = 0,
    Range500dps = 1,
    Range1000dps = 2,
    Range2000dps = 3,
}

/// <summary>スキャンで見つかった JINS MEME ES_R。</summary>
public sealed class MEMEDevice
{
    public MEMEDevice(ulong bluetoothAddress, string name)
    {
        BluetoothAddress = bluetoothAddress;
        Name = name;
        Address = bluetoothAddress.ToString("X12");
    }

    /// <summary>WinRT が扱う 48bit の BD_ADDR。</summary>
    public ulong BluetoothAddress { get; }

    /// <summary>"28A183055C47" 形式。CSV ファイル名にもこの表記を使う。</summary>
    public string Address { get; }

    /// <summary>"ESRG2_0" などの広告名。</summary>
    public string Name { get; }

    public override string ToString() =>
        string.IsNullOrEmpty(Name) ? Address : $"{Name}  ({Address})";
}

/// <summary>1 サンプルの共通部分。モードごとの派生クラスがセンサー値を持つ。</summary>
public abstract class AcademicData
{
    /// <summary>0〜4095 で巡回するデータカウンタ。</summary>
    public int Cnt { get; set; }

    /// <summary>バッテリーレベル(5 段階)。</summary>
    public int BattLv { get; set; }

    /// <summary>
    /// このサンプルの記録時刻(UTC)。計測中は受信時刻、ファイル再生中は
    /// 再生元 CSV の DATE 列から復元する。CSV 出力とチャート X 軸の表示に使う。
    /// 時刻が分からない場合(DATE 列が壊れている CSV など)は null。
    /// </summary>
    public DateTime? RecordedUtc { get; set; }
}

/// <summary>
/// Academic Standard モード(AUP_REPORT_ACADEMIA1 = 0x98)の 1 サンプル。
/// EOG は 1 パケットに 2 組(添字 1 / 2)入る。
/// </summary>
public sealed class AcademicStandardData : AcademicData
{
    public short AccX { get; set; }
    public short AccY { get; set; }
    public short AccZ { get; set; }

    public short EogL1 { get; set; }
    public short EogR1 { get; set; }
    public short EogL2 { get; set; }
    public short EogR2 { get; set; }

    /// <summary>横(左右)の視線移動。EogL1 - EogR1。</summary>
    public short EogH1 { get; set; }

    /// <summary>横(左右)の視線移動。EogL2 - EogR2。</summary>
    public short EogH2 { get; set; }

    /// <summary>縦(上下)の視線移動。-(EogL1 + EogR1) / 2。</summary>
    public short EogV1 { get; set; }

    /// <summary>縦(上下)の視線移動。-(EogL2 + EogR2) / 2。</summary>
    public short EogV2 { get; set; }
}

/// <summary>
/// Academic Full モード(AUP_REPORT_ACADEMIA2 = 0x99)の 1 サンプル。
/// フィールド名は旧 SDK の AcademicFullData と互換。
/// </summary>
public sealed class AcademicFullData : AcademicData
{
    public short AccX { get; set; }
    public short AccY { get; set; }
    public short AccZ { get; set; }

    public short GyroX { get; set; }
    public short GyroY { get; set; }
    public short GyroZ { get; set; }

    /// <summary>ブリッジ(レファレンス電極)と左鼻パッド電極の電位差。</summary>
    public short EogL { get; set; }

    /// <summary>ブリッジ(レファレンス電極)と右鼻パッド電極の電位差。</summary>
    public short EogR { get; set; }

    /// <summary>横(左右)の視線移動。EogL - EogR。</summary>
    public short EogH { get; set; }

    /// <summary>縦(上下)の視線移動。-(EogL + EogR) / 2。</summary>
    public short EogV { get; set; }
}

/// <summary>Academic Quaternion モード(AUP_REPORT_ACADEMIA3 = 0x9A)の 1 サンプル。</summary>
public sealed class AcademicQuaternionData : AcademicData
{
    public int QuaternionW { get; set; }
    public int QuaternionX { get; set; }
    public int QuaternionY { get; set; }
    public int QuaternionZ { get; set; }
}
