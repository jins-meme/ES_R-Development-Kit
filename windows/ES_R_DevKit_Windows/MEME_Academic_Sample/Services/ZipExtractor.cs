using System.IO.Compression;
using System.Text;

namespace MEME_Academic_Sample.Services;

/// <summary>
/// グラフ画面の zip(webview/BRIDGE.md の「Limits」)を安全に展開する。Mac 版 ZipExtractor.swift と同じ規則。
///
/// 展開する前に zip の目次(セントラルディレクトリ)を全部読んで検査し、1 つでも通らなければ何も書かない:
///   - パス: 空・絶対パス(/…、C:…)・「\」・NUL や制御文字・「.」「..」の要素・Windows で使えない名前
///           (CON など、末尾の「.」や空白、「:」)→ zip slip(展開先の外への書き込み)を防ぐ
///   - 重複: 同じ名前、大文字小文字・Unicode の正規化(NFC/NFD)の違いだけの名前、ファイルとフォルダの同名
///   - 種類: シンボリックリンク(外部属性)・暗号化・ZIP64・分割 zip・圧縮方式が「無圧縮 / Deflate」以外
///   - 大きさ: ファイル数・1 ファイル・合計(宣言値)の上限、圧縮率の上限 → zip 爆弾を防ぐ
/// 展開は System.IO.Compression.ZipFile に任せず自分で行う。宣言された大きさを超えて出てきたら途中で止める
/// (大きさを偽った zip 爆弾)。CRC-32 も照合する。
/// </summary>
public static class ZipExtractor
{
    public sealed class Limits
    {
        public long MaxZipBytes { get; init; } = 100_000_000;   // zip ファイルそのもの
        public long MaxTotal { get; init; } = 100_000_000;      // 展開後の合計
        public long MaxEntry { get; init; } = 50_000_000;       // 1 ファイル
        public int MaxFiles { get; init; } = 2000;
        public long MaxRatio { get; init; } = 200;              // 展開後 / 圧縮後(1 MB を超えるファイルだけ見る)
        public int MaxNameBytes { get; init; } = 255;
        public int MaxDepth { get; init; } = 16;
    }

    /// <summary>zip を断った理由。メッセージは設定画面にそのまま出す。</summary>
    public sealed class Failure(string message) : Exception(message)
    {
        public static Failure NotZip(string s) => new($"Not a zip file ({s}).");
        public static Failure Unsupported(string s) => new($"The zip uses a feature this app does not accept: {s}.");
        public static Failure UnsafeName(string s) => new($"The zip contains a path that is not allowed: {s}");
        public static Failure Duplicate(string s) => new($"The zip contains the same path twice: {s}");
        public static Failure TooLarge(string s) => new($"The zip is too large: {s}.");
        public static Failure Corrupt(string s) => new($"The zip is damaged: {s}.");
    }

    private sealed record Entry(string Name, bool IsDir, ushort Method, uint Crc, long CSize, long USize, int LocalOffset);

    /// <summary>zip を dir(空のフォルダ)へ展開する。検査に通らなければ投げる(その前には何も書かない)。</summary>
    public static (int Files, long Bytes) Extract(string zip, string dir, Limits? limits = null)
    {
        limits ??= new Limits();
        var size = new FileInfo(zip).Length;
        if (size > limits.MaxZipBytes)
        {
            throw Failure.TooLarge($"{size / 1_000_000} MB, limit {limits.MaxZipBytes / 1_000_000} MB");
        }

        var data = File.ReadAllBytes(zip);
        var entries = ReadDirectory(data, limits);
        var files = 0;
        long total = 0;
        foreach (var e in entries)
        {
            var path = Path.Combine(dir, e.Name.Replace('/', Path.DirectorySeparatorChar));
            if (e.IsDir)
            {
                Directory.CreateDirectory(path);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var body = ReadBody(data, e);
            if (body.Length != e.USize)
            {
                throw Failure.Corrupt($"{e.Name}: size differs from the directory");
            }

            if (Crc32(body) != e.Crc)
            {
                throw Failure.Corrupt($"{e.Name}: CRC mismatch");
            }

            // 作ったばかりの空のフォルダの中なので、既存のファイルを上書きすることはない(CreateNew で念押し)
            using (var f = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                f.Write(body);
            }

            files++;
            total += e.USize;
        }

        return (files, total);
    }

    #region 目次

    private static List<Entry> ReadDirectory(byte[] d, Limits limits)
    {
        if (d.Length < 22)
        {
            throw Failure.NotZip("too short");
        }

        // End of central directory(末尾から探す。コメントは最大 65535 バイト)
        var eocd = -1;
        var lo = Math.Max(0, d.Length - 22 - 65535);
        for (var p = d.Length - 22; p >= lo; p--)
        {
            if (U32(d, p) == 0x0605_4b50)
            {
                eocd = p;
                break;
            }
        }

        if (eocd < 0)
        {
            throw Failure.NotZip("no end of central directory");
        }

        int disk = U16(d, eocd + 4), cdDisk = U16(d, eocd + 6);
        int nDisk = U16(d, eocd + 8), n = U16(d, eocd + 10);
        long cdSize = U32(d, eocd + 12), cdOff = U32(d, eocd + 16);
        if (disk != 0 || cdDisk != 0 || nDisk != n)
        {
            throw Failure.Unsupported("split zip");
        }

        if (n == 0xFFFF || cdSize == 0xFFFF_FFFF || cdOff == 0xFFFF_FFFF)
        {
            throw Failure.Unsupported("ZIP64");
        }

        if (n > limits.MaxFiles)
        {
            throw Failure.TooLarge($"{n} entries, limit {limits.MaxFiles}");
        }

        if (cdOff + cdSize > eocd)
        {
            throw Failure.Corrupt("central directory out of range");
        }

        var output = new List<Entry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);        // 大文字小文字・正規化をそろえた名前(ファイルとフォルダ)
        var dirsNeeded = new HashSet<string>(StringComparer.Ordinal);  // ファイルの親フォルダ(同名のファイルがあってはならない)
        long total = 0;
        var q = (int)cdOff;
        for (var k = 0; k < n; k++)
        {
            if (q + 46 > eocd || U32(d, q) != 0x0201_4b50)
            {
                throw Failure.Corrupt("central directory entry");
            }

            var madeBy = U16(d, q + 4) >> 8;
            var flags = U16(d, q + 8);
            var method = U16(d, q + 10);
            var crc = U32(d, q + 16);
            long csize = U32(d, q + 20), usize = U32(d, q + 24);
            int nameLen = U16(d, q + 28), extraLen = U16(d, q + 30), commentLen = U16(d, q + 32);
            var ext = U32(d, q + 38);
            long local = U32(d, q + 42);
            if (q + 46 + nameLen + extraLen + commentLen > eocd)
            {
                throw Failure.Corrupt("central directory entry");
            }

            var name0 = DecodeUtf8(d, q + 46, nameLen) ?? throw Failure.UnsafeName("(not UTF-8)");
            q += 46 + nameLen + extraLen + commentLen;

            if ((flags & 0x0001) != 0 || (flags & 0x0040) != 0)
            {
                throw Failure.Unsupported($"encryption ({name0})");
            }

            if (csize == 0xFFFF_FFFF || usize == 0xFFFF_FFFF || local == 0xFFFF_FFFF)
            {
                throw Failure.Unsupported($"ZIP64 ({name0})");
            }

            // Unix で作った zip は外部属性の上位 16 ビットが mode。S_IFLNK(0o120000)はシンボリックリンク
            if (madeBy == 3 && ((ext >> 16) & 0xF000) == 0xA000)
            {
                throw Failure.UnsafeName($"{name0} (symbolic link)");
            }

            var isDir = name0.EndsWith('/');
            var name = (isDir ? name0[..^1] : name0).Normalize(NormalizationForm.FormC);
            CheckName(name, name0, limits);
            if (isDir)
            {
                if (usize != 0)
                {
                    throw Failure.Corrupt($"{name0}: directory with data");
                }
            }
            else
            {
                if (method != 0 && method != 8)
                {
                    throw Failure.Unsupported($"compression method {method} ({name0})");
                }

                if (method == 0 && csize != usize)
                {
                    throw Failure.Corrupt($"{name0}: stored size mismatch");
                }

                if (usize > limits.MaxEntry)
                {
                    throw Failure.TooLarge($"{name0} is {usize / 1_000_000} MB, limit {limits.MaxEntry / 1_000_000} MB");
                }

                if (usize > 1_000_000 && usize > csize * limits.MaxRatio)
                {
                    throw Failure.TooLarge($"{name0} expands {usize / Math.Max(csize, 1)}×, limit {limits.MaxRatio}×");
                }

                total += usize;
                if (total > limits.MaxTotal)
                {
                    throw Failure.TooLarge($"more than {limits.MaxTotal / 1_000_000} MB when extracted");
                }
            }

            var key = name.ToLowerInvariant();
            if (!seen.Add(key) && !(isDir && dirsNeeded.Contains(key)))   // フォルダは「ファイルの親」として先に数えたもの
            {
                throw Failure.Duplicate(name0);
            }

            // 親のフォルダはファイルであってはならない(a と a/b)
            var parts = key.Split('/');
            var acc = "";
            for (var i = 0; i < parts.Length - 1; i++)
            {
                acc = acc.Length == 0 ? parts[i] : acc + "/" + parts[i];
                dirsNeeded.Add(acc);
                seen.Add(acc);
            }

            if (local + 30 > d.Length)
            {
                throw Failure.Corrupt($"{name0}: local header out of range");
            }

            output.Add(new Entry(name, isDir, method, crc, csize, usize, (int)local));
        }

        // ファイルとして出てくる名前が、別のファイルの親フォルダになっていないか
        foreach (var e in output)
        {
            if (!e.IsDir && dirsNeeded.Contains(e.Name.ToLowerInvariant()))
            {
                throw Failure.Duplicate($"{e.Name} (file and folder)");
            }
        }

        return output;
    }

    private static readonly HashSet<string> Reserved =
    [
        "con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9",
    ];

    /// <summary>zip の中のパスとして許すか(3 アプリで同じ規則。webview/BRIDGE.md の Limits)</summary>
    public static void CheckName(string name, string raw, Limits? limits = null)
    {
        limits ??= new Limits();
        if (!IsAllowedName(name, limits))
        {
            throw Failure.UnsafeName(raw);
        }
    }

    private static bool IsAllowedName(string name, Limits limits)
    {
        if (name.Length == 0 || Encoding.UTF8.GetByteCount(name) > limits.MaxNameBytes)
        {
            return false;
        }

        if (name.StartsWith('/') || name.Contains('\\') || name.Contains(':'))
        {
            return false;
        }

        if (name.Any(c => c < 0x20 || c == 0x7F))
        {
            return false;
        }

        var parts = name.Split('/');
        if (parts.Length > limits.MaxDepth)
        {
            return false;
        }

        foreach (var p in parts)
        {
            if (p.Length == 0 || p == "." || p == "..")
            {
                return false;
            }

            if (p.EndsWith('.') || p.EndsWith(' '))
            {
                return false;
            }

            if (Reserved.Contains(p.Split('.', 2)[0].ToLowerInvariant()))
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region 中身

    private static byte[] ReadBody(byte[] d, Entry e)
    {
        var h = e.LocalOffset;
        if (U32(d, h) != 0x0403_4b50)
        {
            throw Failure.Corrupt($"{e.Name}: local header");
        }

        int nameLen = U16(d, h + 26), extraLen = U16(d, h + 28);
        var start = h + 30 + nameLen + extraLen;
        if (start + e.CSize > d.Length)
        {
            throw Failure.Corrupt($"{e.Name}: data out of range");
        }

        // 目次と中身の見出しで名前が違う zip は受けない(展開の道具によって違う名前で書かれるのを避ける)
        var local = DecodeUtf8(d, h + 30, nameLen)?.Normalize(NormalizationForm.FormC) ?? "";
        if (local != e.Name && local != e.Name + "/")
        {
            throw Failure.Corrupt($"{e.Name}: names differ");
        }

        if (e.Method == 0)
        {
            return d.AsSpan(start, (int)e.CSize).ToArray();
        }

        return Inflate(d, start, (int)e.CSize, (int)e.USize, e.Name);
    }

    /// <summary>生の Deflate を展開する。expected を 1 バイトでも超えたら止める</summary>
    private static byte[] Inflate(byte[] d, int offset, int count, int expected, string name)
    {
        var output = new byte[expected + 1];   // 1 バイト余分に取り、超えたことを見分ける
        var n = 0;
        try
        {
            using var src = new MemoryStream(d, offset, count, writable: false);
            using var inflater = new DeflateStream(src, CompressionMode.Decompress);
            while (n < output.Length)
            {
                var r = inflater.Read(output, n, output.Length - n);
                if (r == 0)
                {
                    break;
                }

                n += r;
            }
        }
        catch (InvalidDataException)
        {
            throw Failure.Corrupt($"{name}: could not be decompressed");
        }

        if (n > expected)
        {
            throw Failure.TooLarge($"{name} expands beyond its declared size");
        }

        if (n != expected)
        {
            throw Failure.Corrupt($"{name}: could not be decompressed");
        }

        Array.Resize(ref output, n);
        return output;
    }

    #endregion

    #region 小物

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static string? DecodeUtf8(byte[] d, int offset, int count)
    {
        try
        {
            return StrictUtf8.GetString(d, offset, count);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    private static ushort U16(byte[] d, int i) =>
        i >= 0 && i + 2 <= d.Length ? (ushort)(d[i] | d[i + 1] << 8) : (ushort)0;

    private static uint U32(byte[] d, int i) =>
        i >= 0 && i + 4 <= d.Length ? (uint)(d[i] | d[i + 1] << 8 | d[i + 2] << 16 | d[i + 3] << 24) : 0;

    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(i =>
    {
        var c = (uint)i;
        for (var k = 0; k < 8; k++)
        {
            c = (c & 1) != 0 ? 0xEDB8_8320 ^ (c >> 1) : c >> 1;
        }

        return c;
    }).ToArray();

    public static uint Crc32(ReadOnlySpan<byte> data)
    {
        var c = 0xFFFF_FFFF;
        foreach (var b in data)
        {
            c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        }

        return c ^ 0xFFFF_FFFF;
    }

    #endregion
}
