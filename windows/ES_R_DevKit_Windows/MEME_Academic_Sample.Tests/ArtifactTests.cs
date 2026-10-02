using MEMELib_Academic;
using MEME_Academic_Sample.Services;
using Xunit;

namespace MEME_Academic_Sample.Tests;

/// <summary>ページで付けた Artifact の無害化・番号の換算と、CSV への書き戻し。</summary>
public class ArtifactTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("meme_artifact_tests").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Theory]
    [InlineData("", "X")]
    [InlineData("  ", "X")]
    [InlineData("blink", "blink")]
    [InlineData("a,b\nc\r", "a b c")]
    [InlineData("=SUM(A1)", null)]
    [InlineData("+1", null)]
    [InlineData("-1", null)]
    [InlineData("@x", null)]
    [InlineData(" =x", null)]
    public void Sanitize(string text, string? expected) => Assert.Equal(expected, ArtifactBuffer.Sanitize(text));

    [Fact]
    public void Sanitize_Truncates() =>
        Assert.Equal(ArtifactBuffer.MaxLength, ArtifactBuffer.Sanitize(new string('a', 100))!.Length);

    [Fact]
    public void LiveRows_AreSampleNumberMinusOne()
    {
        var buffer = new ArtifactBuffer();
        Assert.True(buffer.Add(0, "dropped"));     // サンプル 0 は CSV に無い
        Assert.True(buffer.Add(300, "first"));
        Assert.True(buffer.Add(300, "last"));      // 同じ番号は上書き
        Assert.False(buffer.Add(5, "=1+1"));
        Assert.Equal(new Dictionary<int, string> { [299] = "last" }, buffer.TakeLiveRows());
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void ReplayRows_KeepTheRowNumber()
    {
        var buffer = new ArtifactBuffer();
        buffer.Add(-3, "");
        buffer.Add(10, "a");
        Assert.Equal(new Dictionary<int, string> { [0] = "X", [10] = "a" }, buffer.TakeReplayRows());
        Assert.Equal(0, buffer.Count);
    }

    [Theory]
    [InlineData("a.csv")]
    [InlineData("a.csv.gz")]
    public void Writer_ReplacesTheFirstColumn_AndKeepsTheFormat(string name)
    {
        var path = Path.Combine(_directory, name);
        CsvFile.WriteAllLines(path, ["// Data mode  : Full", "//ARTIFACT,NUM,DATE", ",1,d", "", ",2,d", "X,3,d"]);

        CsvArtifactWriter.Apply(path, new Dictionary<int, string> { [1] = "blink", [2] = "Y" });

        Assert.Equal(["// Data mode  : Full", "//ARTIFACT,NUM,DATE", ",1,d", "", "blink,2,d", "Y,3,d"], CsvFile.ReadAllLines(path));
        Assert.Equal(CsvFile.IsGzip(path), File.ReadAllBytes(path)[..2].SequenceEqual(new byte[] { 0x1F, 0x8B }));
        Assert.Single(Directory.GetFiles(_directory));   // 一時ファイルを残さない
    }

    [Fact]
    public void Writer_RefusesOtherFiles()
    {
        var path = Path.Combine(_directory, "other.csv");
        File.WriteAllText(path, "a,b\r\n1,2\r\n");
        Assert.Throws<InvalidDataException>(() => CsvArtifactWriter.Apply(path, new Dictionary<int, string> { [0] = "X" }));
    }
}
