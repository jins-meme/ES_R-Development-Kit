using MEMELib_Academic;


namespace MEME_Academic_Sample.Services;

/// <summary>
/// ページで付けたアーティファクトを CSV の ARTIFACT 列へ書き戻す。Mac 版 CsvArtifactWriter.swift に対応する。
/// (CSV の読み込みと再生はグラフ画面のページが受け持つので、アプリ側に残るのは書き戻しだけ。)
/// </summary>
public static class CsvArtifactWriter
{
    /// <summary>
    /// CSV の ARTIFACT 列(各データ行の先頭カラム)へ書き戻す。キーは 0 始まりのデータ行番号
    /// (「//ARTIFACT」の見出しの次の行が 0。空行は数えない)。既に値がある行は上書きする。
    /// </summary>
    public static void Apply(string path, IReadOnlyDictionary<int, string> artifacts)
    {
        if (artifacts.Count == 0)
        {
            return;
        }

        var lines = CsvFile.ReadAllLines(path);
        var headerIndex = Array.FindIndex(lines, l => l.StartsWith("//ARTIFACT", StringComparison.Ordinal));
        if (headerIndex < 0)
        {
            throw new InvalidDataException("MEME の CSV 形式ではありません。");
        }

        var dataRow = 0;
        for (var i = headerIndex + 1; i < lines.Length; i++)
        {
            if (lines[i].Trim().Length == 0)
            {
                continue;
            }

            if (artifacts.TryGetValue(dataRow, out var artifact))
            {
                lines[i] = ReplaceFirstField(lines[i], artifact);
            }

            dataRow++;
        }

        WriteAtomic(path, lines);
    }

    /// <summary>1 行の最初のカンマより前(ARTIFACT 列)を差し替える。</summary>
    private static string ReplaceFirstField(string line, string value)
    {
        var comma = line.IndexOf(',');
        return comma < 0 ? line : value + line[comma..];
    }

    /// <summary>
    /// 書き込み中に落ちても元ファイルを壊さないよう、一時ファイルへ書いてから置き換える。
    /// 数十万行を書き戻すこともあるため、途中で失敗する余地を減らしておく。
    /// </summary>
    private static void WriteAtomic(string path, IEnumerable<string> lines)
    {
        var directory = Path.GetDirectoryName(path);
        // 一時ファイルにも本来の拡張子を残す。CsvFile は拡張子で圧縮の有無を決めるので、
        // "….csv.gz.tmp" にすると gz のはずのファイルが非圧縮で書かれてしまう。
        var temp = Path.Combine(
            string.IsNullOrEmpty(directory) ? "." : directory,
            CsvFile.BaseName(path) + ".tmp" + CsvFile.MatchingExtension(path));

        CsvFile.WriteAllLines(temp, lines);
        File.Move(temp, path, overwrite: true);
    }
}
