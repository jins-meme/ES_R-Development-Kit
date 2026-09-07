using Xunit;

namespace MEMELib_Academic.Tests;

/// <summary>
/// .csv / .csv.gz の読み書き。計測中は追記のたびに gzip の 1 メンバーを連結していくので、
/// 「連結したものが 1 ファイルとして読めること」と「末尾が欠けても読めるところまで返すこと」
/// がこの仕組みの前提になる。
/// </summary>
public class CsvFileTests : IDisposable
{
    private readonly string _directory =
        Directory.CreateTempSubdirectory("meme_csvfile_tests").FullName;

    [Theory]
    [InlineData("a.csv", false)]
    [InlineData("a.csv.gz", true)]
    [InlineData("a.CSV.GZ", true)]
    [InlineData("a.gz", false)]
    public void IsGzip_LooksAtBothExtensionSegments(string name, bool expected) =>
        Assert.Equal(expected, CsvFile.IsGzip(name));

    [Theory]
    [InlineData("a.csv", "a")]
    [InlineData("a.csv.gz", "a")]
    [InlineData("28A183055C47_20260904010203.csv.gz", "28A183055C47_20260904010203")]
    // ".csv.gz" は 2 段なので Path.GetFileNameWithoutExtension では "a.csv" になってしまう。
    [InlineData(@"C:\tmp\a.csv.gz", "a")]
    public void BaseName_DropsBothExtensionSegments(string path, string expected) =>
        Assert.Equal(expected, CsvFile.BaseName(path));

    [Theory]
    [InlineData("a.csv", true)]
    [InlineData("a.csv.gz", true)]
    [InlineData("a.gz", false)]
    [InlineData("a.txt", false)]
    public void IsSupported_AcceptsOnlyCsvAndCsvGz(string name, bool expected) =>
        Assert.Equal(expected, CsvFile.IsSupported(name));

    /// <summary>
    /// エクスプローラーの関連付けは最後の 1 段しか見ないので .gz 全般が渡ってくる。
    /// 起動引数としては受け取り、CSV でなければ読み込み時に弾く。
    /// </summary>
    [Theory]
    [InlineData("a.csv.gz", true)]
    [InlineData("a.gz", true)]
    [InlineData("a.txt", false)]
    public void IsOpenTarget_AlsoAcceptsBareGz(string name, bool expected) =>
        Assert.Equal(expected, CsvFile.IsOpenTarget(name));

    [Fact]
    public void WriteAllLines_PlainRoundTrips()
    {
        var path = Path.Combine(_directory, "plain.csv");

        CsvFile.WriteAllLines(path, ["//ARTIFACT,NUM", ",1", ",2"]);

        Assert.Equal(["//ARTIFACT,NUM", ",1", ",2"], CsvFile.ReadAllLines(path));
    }

    [Fact]
    public void WriteAllLines_GzipRoundTripsAndIsActuallyCompressed()
    {
        var path = Path.Combine(_directory, "compressed.csv.gz");

        CsvFile.WriteAllLines(path, ["//ARTIFACT,NUM", ",1", ",2"]);

        Assert.Equal([0x1F, 0x8B], File.ReadAllBytes(path)[..2]);
        Assert.Equal(["//ARTIFACT,NUM", ",1", ",2"], CsvFile.ReadAllLines(path));
    }

    /// <summary>
    /// 計測中の書き足しと同じ形。1 回の追記が gzip の 1 メンバーになり、
    /// 連結したものが 1 ファイルとして読める(`gzip -d` の挙動と同じ)。
    /// </summary>
    [Fact]
    public void AppendText_ConcatenatedGzipMembersReadBackAsOneFile()
    {
        var path = Path.Combine(_directory, "appended.csv.gz");

        CsvFile.AppendText(path, "//ARTIFACT,NUM\r\n,1\r\n");
        CsvFile.AppendText(path, ",2\r\n");
        CsvFile.AppendText(path, ",3\r\n");

        Assert.Equal(["//ARTIFACT,NUM", ",1", ",2", ",3"], CsvFile.ReadAllLines(path));
    }

    /// <summary>
    /// 書き込み中に落ちてメンバーの途中で切れていても、例外にせず読めたところまでを返す。
    /// 長時間計測のログが末尾の欠けで全部読めなくなるのを避けるため。
    /// どこまで復元できるかは切れた位置次第(トレーラだけが欠けていれば中身は全部読める)なので、
    /// 「完結したメンバーぶんが必ず残る」ことだけを見る。
    /// </summary>
    [Fact]
    public void ReadAllLines_TruncatedTailKeepsWhatIsReadable()
    {
        var path = Path.Combine(_directory, "truncated.csv.gz");
        CsvFile.AppendText(path, "//ARTIFACT,NUM\r\n,1\r\n");
        var completed = File.ReadAllBytes(path).Length;
        CsvFile.AppendText(path, ",2\r\n");

        // 2 メンバー目を途中で切る。
        var bytes = File.ReadAllBytes(path);
        File.WriteAllBytes(path, bytes[..(completed + ((bytes.Length - completed) / 2))]);

        var lines = CsvFile.ReadAllLines(path);

        Assert.Equal(["//ARTIFACT,NUM", ",1"], lines[..2]);
    }

    /// <summary>
    /// 判定は中身の magic number で行う。拡張子と中身が食い違うファイルでも中身を優先する。
    /// </summary>
    [Fact]
    public void ReadAllLines_DetectsGzipByContentNotExtension()
    {
        var gzipPath = Path.Combine(_directory, "actually_gzip.csv.gz");
        CsvFile.WriteAllLines(gzipPath, ["//ARTIFACT,NUM", ",1"]);

        var misnamed = Path.Combine(_directory, "misnamed.csv");
        File.Copy(gzipPath, misnamed);

        Assert.Equal(["//ARTIFACT,NUM", ",1"], CsvFile.ReadAllLines(misnamed));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // 一時ディレクトリが消せなくてもテスト結果には影響しない。
        }

        GC.SuppressFinalize(this);
    }
}
