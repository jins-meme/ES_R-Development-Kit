using MEMELib_Academic;
using MEME_Academic_Sample.Models;
using MEME_Academic_Sample.Services;
using Xunit;

namespace MEME_Academic_Sample.Tests;

/// <summary>計測中の CSV の書き出し(溜めて書く・名前がぶつかったら別名・TCP へ同じ行を流す)。</summary>
public class PersistenceTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("meme_persistence_tests").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static AcademicFullData Sample(short v) => new() { AccX = v };

    [Fact]
    public void WritesHeaderAndRows_AndForwardsEachRow()
    {
        using var persistence = new DataPersistenceService();
        var forwarded = new List<string>();
        persistence.RowFormatted += forwarded.Add;
        var header = DataPersistenceService.BuildHeader(
            MeasurementMode.Full, MEMEQuality.High, MEMEAccelRange.Range2G, MEMEGyroRange.Range250dps);

        persistence.Begin(_directory, "AABBCCDDEEFF", header, MeasurementMode.Full, MEMEQuality.High, compressed: true);
        for (var k = 1; k <= 250; k++)
        {
            persistence.Append(Sample((short)-k), k, DateTime.UtcNow, freeMarking: k == 5);
        }

        // モードと違う型のサンプルは CSV にも TCP にも出さない
        persistence.Append(new AcademicStandardData(), 251, DateTime.UtcNow, freeMarking: false);
        persistence.End();

        var path = persistence.CurrentFilePath!;
        Assert.EndsWith(".csv.gz", path);
        var lines = CsvFile.ReadAllLines(path);
        Assert.Equal(header.TrimEnd('\r', '\n').Split("\r\n"), lines[..6]);
        var rows = lines[6..];
        Assert.Equal(250, rows.Length);
        Assert.Equal(forwarded, rows);
        Assert.StartsWith("X,5,", rows[4]);
        Assert.EndsWith(",-250,0,0,0,0,0,0,0,0,0", rows[^1]);
    }

    [Fact]
    public void RowsAfterEnd_AreNotForwarded()
    {
        using var persistence = new DataPersistenceService();
        var forwarded = new List<string>();
        persistence.RowFormatted += forwarded.Add;
        persistence.Begin(_directory, "AABBCCDDEEFF", "//ARTIFACT\r\n", MeasurementMode.Full, MEMEQuality.High, compressed: false);
        persistence.Append(Sample(1), 1, DateTime.UtcNow, freeMarking: false);
        persistence.End();

        // 停止の後に届いたパケット(実機では stopDataReport の後も数個届く)は CSV にも TCP にも出さない
        persistence.Append(Sample(2), 2, DateTime.UtcNow, freeMarking: false);

        var rows = CsvFile.ReadAllLines(persistence.CurrentFilePath!).Where(l => !l.StartsWith("//", StringComparison.Ordinal)).ToArray();
        Assert.Single(rows);
        Assert.Equal(rows, forwarded);
    }

    [Fact]
    public void SameSecond_GetsAnotherName()
    {
        using var persistence = new DataPersistenceService();
        var paths = new List<string>();
        for (var run = 0; run < 2; run++)
        {
            persistence.Begin(_directory, "AABBCCDDEEFF", "//ARTIFACT\r\n", MeasurementMode.Full, MEMEQuality.High, compressed: false);
            persistence.Append(Sample(1), 1, DateTime.UtcNow, freeMarking: false);
            persistence.End();
            paths.Add(persistence.CurrentFilePath!);
        }

        // 同じ秒に 2 回始めても、2 回目が 1 回目の CSV に混ざらない
        Assert.NotEqual(paths[0], paths[1]);
        Assert.All(paths, p => Assert.Single(CsvFile.ReadAllLines(p), l => !l.StartsWith("//", StringComparison.Ordinal)));
    }
}
