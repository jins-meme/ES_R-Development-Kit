namespace MEME_Academic_Sample.Services;

/// <summary>
/// ページで付けた、まだ CSV へ書き戻していない Artifact(UI スレッドで扱う)。Mac 版 ArtifactBuffer に対応する。
/// キーは、計測中はサンプル番号、再生中は CSV のデータ行の番号(どちらもページが返す番号そのまま)。
/// 計測停止時・再生の Save Artifacts / Disconnect で <see cref="CsvArtifactWriter"/> が書き戻す。
/// </summary>
public sealed class ArtifactBuffer
{
    /// <summary>1 つの Artifact の長さの上限(webview/BRIDGE.md)</summary>
    public const int MaxLength = 64;

    private readonly Dictionary<int, string> pending = [];

    public int Count => pending.Count;

    public void Clear() => pending.Clear();

    /// <summary>
    /// 控える。空なら "X"、カンマ/改行は列崩れ防止のため空白に、<see cref="MaxLength"/> 文字まで(同じ番号は上書き)。
    /// 表計算ソフトで数式として読まれる書き出し(= + - @)は受けない(CSV 注入。ページも入力時に断る)。受けなければ false。
    /// </summary>
    public bool Add(int i, string text)
    {
        if (Sanitize(text) is not { } value)
        {
            return false;
        }

        pending[Math.Max(i, 0)] = value;
        return true;
    }

    /// <summary>CSV の ARTIFACT 列に入れる文字。数式に見えるものは null。</summary>
    public static string? Sanitize(string text)
    {
        var s = text.Replace(',', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim(' ');
        if (s.Length > MaxLength)
        {
            s = s[..MaxLength];
        }

        if (s.Length > 0 && "=+-@".Contains(s[0]))
        {
            return null;
        }

        return s.Length == 0 ? "X" : s;
    }

    /// <summary>再生中に付けた分を取り出して空にする。キーは CSV のデータ行の番号のまま。</summary>
    public Dictionary<int, string> TakeReplayRows()
    {
        var rows = new Dictionary<int, string>(pending);
        pending.Clear();
        return rows;
    }

    /// <summary>
    /// 計測中に付けた分を CSV のデータ行の番号にして取り出し、空にする。CSV は先頭パケットを 1 件落とすので
    /// データ行 = サンプル番号 − 1(サンプル 0 は CSV に無いので除く)。
    /// </summary>
    public Dictionary<int, string> TakeLiveRows()
    {
        var rows = pending.Where(kv => kv.Key >= 1).ToDictionary(kv => kv.Key - 1, kv => kv.Value);
        pending.Clear();
        return rows;
    }
}
