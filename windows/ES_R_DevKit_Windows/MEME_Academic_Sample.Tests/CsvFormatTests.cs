using System.Globalization;
using MEMELib_Academic;
using MEME_Academic_Sample.Models;
using MEME_Academic_Sample.Services;
using Xunit;

namespace MEME_Academic_Sample.Tests;

/// <summary>
/// CSV のヘッダと行の書式。Mac 版・Android 版・グラフ画面が読む共通の形なので、文字単位で固定しておく。
/// </summary>
public class CsvFormatTests
{
    private static readonly DateTime Utc = new(2026, 10, 2, 4, 27, 10, 210, DateTimeKind.Utc);

    private static readonly AcademicFullData Full = new()
    {
        AccX = -200, AccY = -3415, AccZ = -2284, GyroX = 178, GyroY = -343, GyroZ = 763,
        EogL = 2007, EogR = 2001, EogH = 6, EogV = -2004,
    };

    private static readonly AcademicStandardData Standard = new()
    {
        AccX = 1, AccY = -2, AccZ = 3, EogL1 = 4, EogR1 = -5, EogL2 = 6, EogR2 = 7, EogH1 = 8, EogH2 = 9, EogV1 = -10, EogV2 = 11,
    };

    private static readonly AcademicQuaternionData Quaternion = new()
    {
        QuaternionW = 100000, QuaternionX = -1, QuaternionY = 2, QuaternionZ = -3,
    };

    // 書式をまとめる前の BuildHeader が出していたものと同じ
    [Fact]
    public void Header_Full() => Assert.Equal(
        "// Data mode  : Full\r\n// Transmission speed  : 100Hz\r\n// Acceleration sensor's range  : 2g\r\n" +
        "// Gyroscope sensor's range  : 250dps\r\n//\r\n" +
        "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V\r\n",
        DataPersistenceService.BuildHeader(
            MeasurementMode.Full, MEMEQuality.High, MEMEAccelRange.Range2G, MEMEGyroRange.Range250dps));

    [Fact]
    public void Header_Standard() => Assert.Equal(
        "// Data mode  : Standard\r\n// Transmission speed  : 50Hz\r\n// Acceleration sensor's range  : 8g\r\n" +
        "// Gyroscope sensor's range  : 1000dps\r\n//\r\n" +
        "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,EOG_L1,EOG_R1,EOG_L2,EOG_R2,EOG_H1,EOG_H2,EOG_V1,EOG_V2\r\n",
        DataPersistenceService.BuildHeader(
            MeasurementMode.Standard, MEMEQuality.Low, MEMEAccelRange.Range8G, MEMEGyroRange.Range1000dps));

    [Fact]
    public void Header_Quaternion() => Assert.Equal(
        "// Data mode  : Quaternion\r\n// Transmission speed  : 100Hz\r\n// Acceleration sensor's range  : 16g\r\n" +
        "// Gyroscope sensor's range  : 2000dps\r\n//\r\n" +
        "//ARTIFACT,NUM,DATE,QUATERNION_W,QUATERNION_X,QUATERNION_Y,QUATERNION_Z\r\n",
        DataPersistenceService.BuildHeader(
            MeasurementMode.Quaternion, MEMEQuality.High, MEMEAccelRange.Range16G, MEMEGyroRange.Range2000dps));

    [Fact]
    public void Rows_ForEachMode()
    {
        Assert.Equal(
            ",1,2026/10/02 04:27:10.21,-200,-3415,-2284,178,-343,763,2007,2001,6,-2004",
            DataPersistenceService.FormatRow(MeasurementMode.Full, Full, 1, Utc, freeMarking: false));
        Assert.Equal(
            "X,2,2026/10/02 04:27:10.21,1,-2,3,4,-5,6,7,8,9,-10,11",
            DataPersistenceService.FormatRow(MeasurementMode.Standard, Standard, 2, Utc, freeMarking: true));
        Assert.Equal(
            ",3,2026/10/02 04:27:10.21,100000,-1,2,-3",
            DataPersistenceService.FormatRow(MeasurementMode.Quaternion, Quaternion, 3, Utc, freeMarking: false));
    }

    [Fact]
    public void Row_OfAnotherMode_IsNotWritten() =>
        Assert.Null(DataPersistenceService.FormatRow(MeasurementMode.Full, Standard, 1, Utc, freeMarking: false));

    /// <summary>
    /// スウェーデン語などでは .NET が負の数を U+2212(−)で、アラビア語では文字の向きの印を付けて書く。
    /// タイ語・アラビア語では年が仏暦・ヒジュラ暦になる。どの言語の Windows でも同じ CSV を書く。
    /// </summary>
    [Theory]
    [InlineData("sv-SE")]
    [InlineData("fi-FI")]
    [InlineData("ar-SA")]
    [InlineData("th-TH")]
    [InlineData("ja-JP")]
    public void Format_DoesNotDependOnCulture(string culture)
    {
        var saved = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try
        {
            Assert.Equal(
                ",1,2026/10/02 04:27:10.21,-200,-3415,-2284,178,-343,763,2007,2001,6,-2004",
                DataPersistenceService.FormatRow(MeasurementMode.Full, Full, 1, Utc, freeMarking: false));
            Assert.Equal("28A183055C47_20261002042710.csv.gz",
                DataPersistenceService.FileName("28A183055C47", Utc, compressed: true));
            Assert.StartsWith("// Data mode  : Full\r\n// Transmission speed  : 100Hz\r\n",
                DataPersistenceService.BuildHeader(
                    MeasurementMode.Full, MEMEQuality.High, MEMEAccelRange.Range2G, MEMEGyroRange.Range250dps));
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public void ModeTable_MatchesTheDeviceAndThePage()
    {
        Assert.Equal([MEMEMode.Standard, MEMEMode.Full, MEMEMode.Quaternion], MeasurementMode.All.Select(m => m.Device));
        Assert.Same(MeasurementMode.Full, MeasurementMode.FromPageName("full"));
        Assert.Null(MeasurementMode.FromPageName("other"));
        // 分からない値は Quaternion として書く(以前からの扱い)
        Assert.Same(MeasurementMode.Quaternion, MeasurementMode.Of((MEMEMode)9));
        Assert.Equal(MeasurementMode.Full.Columns.Count, MeasurementMode.Full.Values(Full)!.Length);
        Assert.Equal(MeasurementMode.Standard.Columns.Count, MeasurementMode.Standard.Values(Standard)!.Length);
        Assert.Equal(MeasurementMode.Quaternion.Columns.Count, MeasurementMode.Quaternion.Values(Quaternion)!.Length);
    }

    [Fact]
    public void Ranges_RoundTrip()
    {
        Assert.Equal(MEMEAccelRange.Range8G, MeasurementRange.AccelRangeOf(8));
        Assert.Null(MeasurementRange.AccelRangeOf(3));
        Assert.Equal(MEMEGyroRange.Range2000dps, MeasurementRange.GyroRangeOf(2000));
        Assert.Equal(MEMEQuality.Low, MeasurementRange.QualityOf(50));
        Assert.Equal(16, MeasurementRange.G((MEMEAccelRange)7));
        Assert.Equal("±1000dps", MeasurementRange.GyroLabel(1000));
    }
}
