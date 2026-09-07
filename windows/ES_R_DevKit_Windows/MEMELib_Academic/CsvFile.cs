using System.IO.Compression;
using System.Text;

namespace MEMELib_Academic;

/// <summary>
/// 本アプリが扱う CSV ファイル(非圧縮 .csv / gz 圧縮 .csv.gz)の拡張子判定と読み書き。
/// 「.gz かどうか」の判定をここ 1 か所に閉じ込め、保存・再生・切り出し・Artifact 書き戻しが
/// 同じ規則で動くようにする。Mac 版 CsvFile.swift に対応する。
///
/// 圧縮は「1 回の書き出し＝gzip の 1 メンバー」とし、それをファイルへ連結していく。
/// gzip は複数メンバーの連結を 1 ファイルとして扱えるので、これで `gzip -d` でも
/// アプリの再生でもそのまま読める。ストリームを開きっぱなしにして最後にトレーラを書く
/// 方式と違い、アプリが落ちても／切断で計測が途切れても、その時点までのファイルが
/// 常に完結している(連続ストリームに比べた圧縮率の悪化は実測で数％)。
/// </summary>
public static class CsvFile
{
    /// <summary>非圧縮の拡張子。</summary>
    public const string PlainExtension = ".csv";

    /// <summary>gz 圧縮の拡張子(2 段)。</summary>
    public const string GzipExtension = ".csv.gz";

    /// <summary>OpenFileDialog 用のフィルタ。.csv と .csv.gz の両方を選べるようにする。</summary>
    public const string OpenFilter =
        "MEME CSV (*.csv;*.csv.gz)|*.csv;*.csv.gz|All files (*.*)|*.*";

    /// <summary>gzip の magic number(1F 8B)。拡張子ではなく中身で判定するのに使う。</summary>
    private static ReadOnlySpan<byte> GzipMagic => [0x1F, 0x8B];

    /// <summary>新規保存で使う拡張子。圧縮するかは Setting の Save Format で決まる。</summary>
    public static string SaveExtension(bool compressed) => compressed ? GzipExtension : PlainExtension;

    /// <summary>gz 圧縮された CSV か(拡張子で判定、大文字小文字は問わない)。</summary>
    public static bool IsGzip(string path) =>
        path.EndsWith(GzipExtension, StringComparison.OrdinalIgnoreCase);

    /// <summary>読み込み対象として扱う拡張子(.csv / .csv.gz)を持つか。</summary>
    public static bool IsSupported(string path) =>
        path.EndsWith(GzipExtension, StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(PlainExtension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// エクスプローラーの「プログラムから開く」で渡されうる拡張子か。
    /// Windows の拡張子判定は最後の 1 段しか見ないため関連付けは .gz 全般になり、
    /// CSV でない .gz も本アプリへ来る(その場合は読み込み時に形式違いで弾かれる)。
    /// </summary>
    public static bool IsOpenTarget(string path) =>
        IsSupported(path) || path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 拡張子(".csv" / ".csv.gz")を取り除いたファイル名。
    /// <c>Path.GetFileNameWithoutExtension</c> は "a.csv.gz" から ".gz" しか落とせないので用意する。
    /// </summary>
    public static string BaseName(string path)
    {
        var name = Path.GetFileName(path);
        string[] extensions = [GzipExtension, PlainExtension];
        foreach (var extension in extensions)
        {
            if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                return name[..^extension.Length];
            }
        }

        return Path.GetFileNameWithoutExtension(name);
    }

    /// <summary>path と同じ圧縮形式の拡張子。切り出しファイルを元ファイルへ揃えるのに使う。</summary>
    public static string MatchingExtension(string path) => IsGzip(path) ? GzipExtension : PlainExtension;

    /// <summary>
    /// CSV をテキストとして読む。中身が gzip なら展開してから行に分ける。
    /// 拡張子と中身が食い違うファイル(.csv なのに gz 等)でも中身を優先して読めるようにしている。
    /// </summary>
    public static string[] ReadAllLines(string path)
    {
        using var stream = OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var lines = new List<string>();
        try
        {
            while (reader.ReadLine() is { } line)
            {
                lines.Add(line);
            }
        }
        catch (InvalidDataException)
        {
            // 末尾が壊れている(書き込み中に落ちた・後ろにゴミが付いている)。
            // そこまでに読めた行を結果とする。長時間計測のログが末尾の欠けで
            // 全部読めなくなるのを避けるため。
        }

        return [.. lines];
    }

    /// <summary>行を書き出す。path が .csv.gz なら gz 圧縮して書く。改行は CRLF。</summary>
    public static void WriteAllLines(string path, IEnumerable<string> lines)
    {
        using var writer = CreateWriter(path, append: false);
        foreach (var line in lines)
        {
            writer.WriteLine(line);
        }
    }

    /// <summary>末尾へ追記する。gz なら 1 回の追記が gzip の 1 メンバーになる。</summary>
    public static void AppendText(string path, string text)
    {
        using var writer = CreateWriter(path, append: true);
        writer.Write(text);
    }

    /// <summary>中身の magic number を見て、gzip なら展開ストリームを被せて返す。</summary>
    private static Stream OpenRead(string path)
    {
        var file = File.OpenRead(path);
        Span<byte> head = stackalloc byte[2];
        var read = file.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
        file.Seek(0, SeekOrigin.Begin);

        return read == head.Length && head.SequenceEqual(GzipMagic)
            ? new GZipStream(file, CompressionMode.Decompress)
            : file;
    }

    private static StreamWriter CreateWriter(string path, bool append)
    {
        var file = new FileStream(
            path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
        Stream stream = IsGzip(path) ? new GZipStream(file, CompressionLevel.Optimal) : file;

        // BOM なし UTF-8。改行は他プラットフォームの出力に合わせて CRLF。
        return new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\r\n" };
    }
}
