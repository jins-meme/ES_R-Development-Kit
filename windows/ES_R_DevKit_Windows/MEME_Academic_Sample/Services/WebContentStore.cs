using System.Security.Cryptography;
using System.Text.Json;
using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample.Services;

/// <summary>zip の manifest.json(webview/BRIDGE.md)。</summary>
public sealed record WebContentManifest(string Name, string? Title, string Version, int BridgeApi, string Entry)
{
    public string DisplayName => $"{Title ?? Name} {Version}";
}

/// <summary>
/// グラフ画面(WebView)の中身の置き場。Mac 版 WebContentStore.swift に対応する。中身は zip で、アプリに同梱した
/// 標準版(standard.zip)か、設定で選んだ zip(高機能版など)を展開して使う。仕様は webview/README.md。
///
/// - 展開先: %LOCALAPPDATA%\JINS\MEME_Academic\WebContent\{bundled,custom}\
/// - 同梱の標準版は、アプリに入っている zip の中身が変わったとき(SHA-256 で見る)だけ展開し直す。
/// - 選んだ zip は <see cref="ZipExtractor"/> で検査しながら一時フォルダへ展開し(zip slip・zip 爆弾・リンクなど。
///   展開する前に目次で弾く)、manifest.json・bridgeApi・入口を確かめてから差し替える。通らなければ元のまま。
///   規則は webview/BRIDGE.md の Limits。
/// </summary>
public sealed class WebContentStore
{
    /// <summary>このアプリが話せるブリッジの版(webview/BRIDGE.md)。manifest.json の bridgeApi と一致しない zip は読まない。</summary>
    public const int BridgeApi = 1;

    public enum ContentSource { Bundled, Custom }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly UserSetting setting;

    public WebContentStore(UserSetting setting)
    {
        this.setting = setting;
        Root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JINS", "MEME_Academic", "WebContent");
        Directory.CreateDirectory(Root);
        Prepare();
    }

    public string Root { get; }

    public WebContentManifest? Manifest { get; private set; }

    public ContentSource Source { get; private set; } = ContentSource.Bundled;

    private string BundledDir => Path.Combine(Root, "bundled");

    private string CustomDir => Path.Combine(Root, "custom");

    /// <summary>今使う中身のフォルダ(仮想ホストの根)</summary>
    public string ActiveDir => Source == ContentSource.Custom ? CustomDir : BundledDir;

    /// <summary>アプリに同梱した標準版(ビルドで実行ファイルの隣の WebContent\ へ写す)</summary>
    private static string BundledZipPath => Path.Combine(AppContext.BaseDirectory, "WebContent", "standard.zip");

    /// <summary>起動時: 同梱の標準版を必要なら展開し、設定で選ばれている方を読む。</summary>
    private void Prepare()
    {
        try
        {
            ExtractBundledIfNeeded();
        }
        catch (Exception e)
        {
            Log($"bundled: {e.Message}");
        }

        if (setting.WebContentSource == "custom" && TryReadManifest(CustomDir) is { } m)
        {
            Source = ContentSource.Custom;
            Manifest = m;
        }
        else
        {
            Source = ContentSource.Bundled;
            Manifest = TryReadManifest(BundledDir);
        }
    }

    private void ExtractBundledIfNeeded()
    {
        if (!File.Exists(BundledZipPath))
        {
            throw new FileNotFoundException("standard.zip is not next to the app.", BundledZipPath);
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(BundledZipPath)));
        var mark = Path.Combine(Root, "bundled.sha256");
        if (File.Exists(mark) && File.ReadAllText(mark) == hash && Directory.Exists(BundledDir))
        {
            return;
        }

        var tmp = ExtractAndValidate(BundledZipPath);
        Replace(BundledDir, tmp);
        File.WriteAllText(mark, hash);
    }

    /// <summary>zip を取り込んで「選んだ zip」に切り替える。検査に通らなければ投げ、今の中身はそのまま。</summary>
    public WebContentManifest ImportZip(string zip)
    {
        var tmp = ExtractAndValidate(zip);
        Replace(CustomDir, tmp);
        var m = ReadManifest(CustomDir);
        Source = ContentSource.Custom;
        Manifest = m;
        setting.WebContentSource = "custom";
        setting.Save();
        return m;
    }

    /// <summary>同梱の標準版に戻す(取り込んだ zip のフォルダは消す)。</summary>
    public void UseBundled()
    {
        TryDelete(CustomDir);
        Source = ContentSource.Bundled;
        Manifest = TryReadManifest(BundledDir);
        setting.WebContentSource = "bundled";
        setting.Save();
    }

    #region 展開と検査

    private string ExtractAndValidate(string zip)
    {
        var tmp = Path.Combine(Root, "tmp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            try
            {
                ZipExtractor.Extract(zip, tmp);
            }
            catch (ZipExtractor.Failure e)
            {
                throw new InvalidDataException(e.Message, e);
            }

            CheckTree(tmp);
            var m = ReadManifest(tmp);
            if (m.BridgeApi != BridgeApi)
            {
                throw new InvalidDataException($"This zip needs bridge API {m.BridgeApi}, but this app supports {BridgeApi}.");
            }

            try
            {
                ZipExtractor.CheckName(m.Entry, m.Entry);
            }
            catch (ZipExtractor.Failure)
            {
                throw new InvalidDataException($"The entry page {m.Entry} is missing.");
            }

            if (!File.Exists(Path.Combine(tmp, m.Entry.Replace('/', Path.DirectorySeparatorChar))))
            {
                throw new InvalidDataException($"The entry page {m.Entry} is missing.");
            }

            return tmp;
        }
        catch
        {
            TryDelete(tmp);
            throw;
        }
    }

    /// <summary>念押し: 展開したものにリンク(再解析ポイント)や外へ出るパスが無いこと(ZipExtractor が目次で弾いているので、通常は何も見つからない)。</summary>
    private static void CheckTree(string dir)
    {
        var root = Path.GetFullPath(dir) + Path.DirectorySeparatorChar;
        foreach (var path in Directory.EnumerateFileSystemEntries(dir, "*", SearchOption.AllDirectories))
        {
            var info = new FileInfo(path);
            if (info.Attributes.HasFlag(FileAttributes.ReparsePoint) || info.LinkTarget is not null)
            {
                throw new InvalidDataException($"The zip contains a link or path outside itself: {info.Name}");
            }

            if (!Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The zip contains a link or path outside itself: {path}");
            }
        }
    }

    /// <summary>前の中身は新しいものを置けてから消す(置けなければ元へ戻す)</summary>
    private void Replace(string dest, string tmp)
    {
        var old = Path.Combine(Root, "old-" + Guid.NewGuid().ToString("N"));
        var hadOld = Directory.Exists(dest);
        if (hadOld)
        {
            Directory.Move(dest, old);
        }

        try
        {
            Directory.Move(tmp, dest);
        }
        catch
        {
            if (hadOld)
            {
                try { Directory.Move(old, dest); } catch (IOException) { }
            }

            TryDelete(tmp);
            throw;
        }

        if (hadOld)
        {
            TryDelete(old);
        }
    }

    private static WebContentManifest? TryReadManifest(string dir)
    {
        try
        {
            return ReadManifest(dir);
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static WebContentManifest ReadManifest(string dir)
    {
        var path = Path.Combine(dir, "manifest.json");
        if (!File.Exists(path))
        {
            throw new InvalidDataException("manifest.json was not found at the top of the zip.");
        }

        var bytes = File.ReadAllBytes(path);
        if (bytes.Length > 64_000)
        {
            throw new InvalidDataException("manifest.json could not be read. too large");
        }

        WebContentManifest? m;
        try
        {
            m = JsonSerializer.Deserialize<WebContentManifest>(bytes, JsonOptions);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"manifest.json could not be read. {e.Message}");
        }

        if (m is null || string.IsNullOrEmpty(m.Name) || m.Version is null || string.IsNullOrEmpty(m.Entry))
        {
            throw new InvalidDataException("manifest.json could not be read. name, version, bridgeApi and entry are required");
        }

        // 設定画面にそのまま出すので、短く・制御文字なし
        foreach (var (k, v) in new[] { ("name", m.Name), ("title", m.Title ?? ""), ("version", m.Version) })
        {
            if (v.Length > 64 || v.Any(c => c < 0x20 || c == 0x7F))
            {
                throw new InvalidDataException($"manifest.json could not be read. {k} must be at most 64 characters without control characters");
            }
        }

        return m;
    }

    #endregion

    private static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log($"could not delete {dir}: {e.Message}");
        }
    }

    private static void Log(string message) => System.Diagnostics.Debug.WriteLine($"[WebContent] {message}");
}
