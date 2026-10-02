using System.Globalization;
using MEMELib_Academic;

namespace MEME_Academic_Sample.Models;

/// <summary>
/// 計測モードごとの列名・並びの表。Mac 版 Models/MeasurementMode.swift に対応する。
/// CSV のヘッダと行、グラフ画面へ渡す列と値、左の欄の選択肢、再生した CSV の条件の読み取りが同じ表を見る。
/// </summary>
public sealed class MeasurementMode
{
    public static readonly MeasurementMode Standard = new(
        MEMEMode.Standard, "Standard", "standard",
        ["ACC_X", "ACC_Y", "ACC_Z", "EOG_L1", "EOG_R1", "EOG_L2", "EOG_R2", "EOG_H1", "EOG_H2", "EOG_V1", "EOG_V2"],
        hasGraph: true,
        data => data is AcademicStandardData d
            ? [d.AccX, d.AccY, d.AccZ, d.EogL1, d.EogR1, d.EogL2, d.EogR2, d.EogH1, d.EogH2, d.EogV1, d.EogV2]
            : null);

    public static readonly MeasurementMode Full = new(
        MEMEMode.Full, "Full", "full",
        ["ACC_X", "ACC_Y", "ACC_Z", "GYRO_X", "GYRO_Y", "GYRO_Z", "EOG_L", "EOG_R", "EOG_H", "EOG_V"],
        hasGraph: true,
        data => data is AcademicFullData d
            ? [d.AccX, d.AccY, d.AccZ, d.GyroX, d.GyroY, d.GyroZ, d.EogL, d.EogR, d.EogH, d.EogV]
            : null);

    public static readonly MeasurementMode Quaternion = new(
        MEMEMode.Quaternion, "Quaternion", "quaternion",
        ["QUATERNION_W", "QUATERNION_X", "QUATERNION_Y", "QUATERNION_Z"],
        hasGraph: false,
        data => data is AcademicQuaternionData d
            ? [d.QuaternionW, d.QuaternionX, d.QuaternionY, d.QuaternionZ]
            : null);

    /// <summary>左の欄 Select Mode の並び</summary>
    public static readonly IReadOnlyList<MeasurementMode> All = [Standard, Full, Quaternion];

    private readonly Func<AcademicData, int[]?> values;

    private MeasurementMode(
        MEMEMode device, string label, string pageName, string[] columns, bool hasGraph, Func<AcademicData, int[]?> values)
    {
        Device = device;
        Label = label;
        PageName = pageName;
        Columns = columns;
        HasGraph = hasGraph;
        this.values = values;
    }

    /// <summary>端末に設定する値</summary>
    public MEMEMode Device { get; }

    /// <summary>画面と CSV ヘッダの「// Data mode」に出す名前</summary>
    public string Label { get; }

    /// <summary>グラフ画面とのやり取りで使う名前(webview/BRIDGE.md の mode)</summary>
    public string PageName { get; }

    /// <summary>ARTIFACT・NUM・DATE に続く CSV の列。グラフ画面へ渡す列(start の columns)も同じ</summary>
    public IReadOnlyList<string> Columns { get; }

    /// <summary>グラフに出せる波形があるか(Quaternion は無い)</summary>
    public bool HasGraph { get; }

    /// <summary>
    /// 1 サンプルの値を <see cref="Columns"/> の並びで返す。モードと違う型のサンプル(切り替えの境目など)は null。
    /// </summary>
    public int[]? Values(AcademicData data) => values(data);

    /// <summary>端末の値から。分からない値は Quaternion として扱う(CSV ヘッダの以前からの扱い)</summary>
    public static MeasurementMode Of(MEMEMode device) => All.FirstOrDefault(m => m.Device == device) ?? Quaternion;

    /// <summary>グラフ画面が返したモード名から(replay-info)</summary>
    public static MeasurementMode? FromPageName(string? name) => All.FirstOrDefault(m => m.PageName == name);

    public override string ToString() => Label;
}

/// <summary>端末のレンジ・転送レートの値と、物理量(g / dps / Hz)の対応。</summary>
public static class MeasurementRange
{
    /// <summary>左の欄 Trans Speed の並び(番号 0 = 100Hz)</summary>
    public static readonly IReadOnlyList<MEMEQuality> Qualities = [MEMEQuality.High, MEMEQuality.Low];

    private static readonly int[] accelG = [2, 4, 8, 16];
    private static readonly int[] gyroDps = [250, 500, 1000, 2000];

    /// <summary>MEMEAccelRange の番号 → g</summary>
    public static IReadOnlyList<int> AccelG => accelG;

    /// <summary>MEMEGyroRange の番号 → dps</summary>
    public static IReadOnlyList<int> GyroDps => gyroDps;

    public static int Hz(MEMEQuality quality) => quality == MEMEQuality.High ? 100 : 50;

    public static int G(MEMEAccelRange range) => accelG[Math.Clamp((int)range, 0, accelG.Length - 1)];

    public static int Dps(MEMEGyroRange range) => gyroDps[Math.Clamp((int)range, 0, gyroDps.Length - 1)];

    /// <summary>Hz から(replay-info の cps)。100 以外は 50Hz</summary>
    public static MEMEQuality QualityOf(int hz) => hz == 100 ? MEMEQuality.High : MEMEQuality.Low;

    /// <summary>g から。表に無ければ null</summary>
    public static MEMEAccelRange? AccelRangeOf(int g) =>
        Array.IndexOf(accelG, g) is var k and >= 0 ? (MEMEAccelRange)k : null;

    /// <summary>dps から。表に無ければ null</summary>
    public static MEMEGyroRange? GyroRangeOf(int dps) =>
        Array.IndexOf(gyroDps, dps) is var k and >= 0 ? (MEMEGyroRange)k : null;

    /// <summary>画面の選択肢(±2G など)</summary>
    public static string AccelLabel(int g) => $"±{g.ToString(CultureInfo.InvariantCulture)}G";

    public static string GyroLabel(int dps) => $"±{dps.ToString(CultureInfo.InvariantCulture)}dps";

    public static string HzLabel(MEMEQuality quality) => $"{Hz(quality).ToString(CultureInfo.InvariantCulture)}Hz";
}
