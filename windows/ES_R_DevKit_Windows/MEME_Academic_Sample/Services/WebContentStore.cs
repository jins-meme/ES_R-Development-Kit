using System.Security.Cryptography;
using System.Text.Json;
using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample.Services;

/// <summary>zip の manifest.json(webview/BRIDGE.md)。</summary>
public sealed record WebContentManifest(string Name, string? Title, string Version, int BridgeApi, string Entry)
{
    public string DisplayName => $"{Title ?? Name} {Version}";
}

/// <summary>持っている中身 1 つ(Display Engine ダイアログの 1 行)</summary>
public sealed record WebContentEntry(string Id, WebContentManifest Manifest)
{
    public bool IsBuiltIn => Id == WebContentStore.BuiltInId;
}

/// <summary>
/// グラフ画面(WebView)の中身(Display Engine)の置き場。Mac 版 WebContentStore.swift に対応する。中身は zip で、
/// アプリに同梱した標準版(standard.zip)と、Display Engine ダイアログで取り込んだ zip(高機能版など、いくつでも)を
/// 展開して持っておき、そのうち 1 つだけを使う。仕様は webview/README.md。
///
/// - 展開先: %LOCALAPPDATA%\JINS\MEME_Academic\WebContent\
///     bundled\         同梱の標準版。消せない(ID は "standard")
///     zips\&lt;ID&gt;\       取り込んだ zip。ID は取り込んだときに振る GUID
///     zips\&lt;ID&gt;.sha256  取り込んだ zip ファイルの SHA-256(同じファイルを 2 度取り込まないため)
///   使っている 1 つの ID は <see cref="UserSetting.WebContentActive"/>。
/// - 同梱の標準版は、アプリに入っている zip の中身が変わったとき(SHA-256 で見る)だけ展開し直す。
/// - 取り込む zip は <see cref="ZipExtractor"/> で検査しながら一時フォルダへ展開し(zip slip・zip 爆弾・リンクなど。
///   展開する前に目次で弾く)、manifest.json・bridgeApi・入口を確かめてから置く。通らなければ何も変わらない。
///   規則は webview/BRIDGE.md の Limits。
/// - 同じファイル(SHA-256 が同じ)をもう一度取り込んでも何もしない(一覧に重ならない)。
/// - manifest の name が同じ zip を取り込んだら、新しい版として置き換える(ID・一覧での位置・使っているかどうかはそのまま)。
/// - 以前の版の custom\(選んだ zip を 1 つだけ持てた)は、起動時に zips\ の 1 つへ移す。
/// - 取り込んだ zip のページが続けて落ちる場合の保護(<see cref="FallBackToBuiltIn"/>): ページのプロセスが落ちると WebBridge が
///   読み込み直すので、落ちる zip だとそれを繰り返す。続けて落ちたら標準版へ戻して知らせる(Mac・Android と同じ)。
/// </summary>
public sealed class WebContentStore
{
    /// <summary>このアプリが話せるブリッジの版(webview/BRIDGE.md)。manifest.json の bridgeApi と一致しない zip は読まない。</summary>
    public const int BridgeApi = 1;

    /// <summary>同梱の標準版の ID(規定。消せない)</summary>
    public const string BuiltInId = "standard";

    public enum AddOutcome { Added, Replaced, AlreadyAdded }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly UserSetting setting;

    public WebContentStore(UserSetting setting)
    {
        this.setting = setting;
        Root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JINS", "MEME_Academic", "WebContent");
        Directory.CreateDirectory(ZipsDir);
        Prepare();
    }

    public string Root { get; }

    /// <summary>持っている中身。先頭が同梱の標準版、あとは取り込んだ順</summary>
    public IReadOnlyList<WebContentEntry> Entries { get; private set; } = [];

    /// <summary>使っている中身の ID</summary>
    public string ActiveId { get; private set; } = BuiltInId;

    /// <summary>今使う中身の manifest</summary>
    public WebContentManifest? Manifest => Entries.FirstOrDefault(e => e.Id == ActiveId)?.Manifest;

    private string BundledDir => Path.Combine(Root, "bundled");

    private string ZipsDir => Path.Combine(Root, "zips");

    /// <summary>以前の版の「選んだ zip」の置き場(移したら無くなる)</summary>
    private string LegacyCustomDir => Path.Combine(Root, "custom");

    private string DirOf(string id) => id == BuiltInId ? BundledDir : Path.Combine(ZipsDir, id);

    private string HashFileOf(string id) => Path.Combine(ZipsDir, id + ".sha256");

    /// <summary>今使う中身のフォルダ(仮想ホストの根)</summary>
    public string ActiveDir => DirOf(ActiveId);

    /// <summary>アプリに同梱した標準版(ビルドで実行ファイルの隣の WebContent\ へ写す)</summary>
    private static string BundledZipPath => Path.Combine(AppContext.BaseDirectory, "WebContent", "standard.zip");

    /// <summary>起動時: 同梱の標準版を必要なら展開し、持っている中身を読み、設定で有効になっている 1 つを使う。</summary>
    internal void Prepare()
    {
        try
        {
            ExtractBundledIfNeeded();
        }
        catch (Exception e)
        {
            Log($"bundled: {e.Message}");
        }

        DropLeftovers();
        MigrateLegacyCustom();
        ReloadEntries();
        var wanted = setting.WebContentActive;
        ActiveId = Entries.Any(e => e.Id == wanted) ? wanted : BuiltInId;
        if (ActiveId != wanted)
        {
            setting.WebContentActive = ActiveId;
            setting.Save();
        }
    }

    /// <summary>途中で止まった取り込み・置き換えの残り(tmp-* / old-*)を消す。起動時は何も取り込んでいないので、残っていれば全部ゴミ。</summary>
    private void DropLeftovers()
    {
        foreach (var d in Directory.GetDirectories(Root, "tmp-*").Concat(Directory.GetDirectories(Root, "old-*")))
        {
            TryDelete(d);
        }
    }

    /// <summary>zips\ の中を読み直す。manifest を読めないフォルダ(途中で止まった取り込みの残りなど)は消す。</summary>
    private void ReloadEntries()
    {
        var list = new List<WebContentEntry>();
        if (TryReadManifest(BundledDir) is { } std)
        {
            list.Add(new WebContentEntry(BuiltInId, std));
        }

        var imported = new List<(DateTime Created, WebContentEntry Entry)>();
        foreach (var d in Directory.Exists(ZipsDir) ? Directory.GetDirectories(ZipsDir) : [])
        {
            var id = Path.GetFileName(d);
            if (TryReadManifest(d) is not { } m)
            {
                Log($"drop unreadable {id}");
                TryDelete(d);
                TryDeleteFile(HashFileOf(id));
                continue;
            }

            imported.Add((Directory.GetCreationTimeUtc(d), new WebContentEntry(id, m)));
        }

        list.AddRange(imported.OrderBy(x => x.Created).Select(x => x.Entry));
        Entries = list;
    }

    /// <summary>以前の版の custom\ を zips\ の 1 つへ移す。使っていたなら、移した先を使う設定にする。</summary>
    private void MigrateLegacyCustom()
    {
        var legacy = setting.WebContentSource;
        if (legacy is not null)
        {
            setting.WebContentSource = null;   // 旧形式の項目は settings.json から消す
            setting.Save();
        }

        if (!Directory.Exists(LegacyCustomDir))
        {
            return;
        }

        var id = Guid.NewGuid().ToString("N");
        try
        {
            Directory.Move(LegacyCustomDir, DirOf(id));
            if (legacy == "custom")
            {
                setting.WebContentActive = id;
                setting.Save();
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log($"migrate custom: {e.Message}");
        }
    }

    private static string Sha256Of(string file)
    {
        using var stream = File.OpenRead(file);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    private void ExtractBundledIfNeeded()
    {
        if (!File.Exists(BundledZipPath))
        {
            throw new FileNotFoundException("standard.zip is not next to the app.", BundledZipPath);
        }

        var hash = Sha256Of(BundledZipPath);
        var mark = Path.Combine(Root, "bundled.sha256");
        if (File.Exists(mark) && File.ReadAllText(mark) == hash && Directory.Exists(BundledDir))
        {
            return;
        }

        var tmp = ExtractAndValidate(BundledZipPath);
        Replace(BundledDir, tmp);
        File.WriteAllText(mark, hash);
    }

    #region 落ちる zip から抜ける

    /// <summary>標準版へ戻したときの知らせ。Display Engine ダイアログを次に開いたときにも出す(<see cref="TakeFallbackNotice"/> で 1 度だけ)</summary>
    private string? fallbackNotice;

    public string? TakeFallbackNotice()
    {
        var notice = fallbackNotice;
        fallbackNotice = null;
        return notice;
    }

    /// <summary>使っている取り込んだ zip をやめて標準版へ戻す。戻したら知らせの文を返す(why は文の後半。例 "kept crashing the graph view")</summary>
    public string? FallBackToBuiltIn(string why)
    {
        if (ActiveId == BuiltInId)
        {
            return null;
        }

        var name = Manifest?.DisplayName ?? ActiveId;
        Activate(BuiltInId);
        fallbackNotice = $"{name} {why}, so the built-in Standard is used now. You can choose it again in Display Engine.";
        Log($"fall back to built-in: {name} {why}");
        return fallbackNotice;
    }

    #endregion

    #region 取り込み・有効化・削除(Display Engine ダイアログ)

    /// <summary>
    /// zip を検査して取り込み、一覧に足す(有効にはしない)。検査に通らなければ投げ、何も変わらない。
    /// - 同じファイルを既に取り込んでいれば何もしない(<see cref="AddOutcome.AlreadyAdded"/>。一覧に重ならない)。
    /// - manifest の name が同じものを持っていれば、それを置き換える(<see cref="AddOutcome.Replaced"/>。ID・位置・使っているかどうかはそのまま)。
    /// ReplacedActive が true なら、使っている中身が変わったのでグラフ画面を読み込み直すこと。
    /// </summary>
    public (WebContentEntry Entry, AddOutcome Outcome, bool ReplacedActive) Add(string zip)
    {
        string hash;
        try
        {
            hash = Sha256Of(zip);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"The zip file could not be extracted. {e.Message}", e);
        }

        var same = Entries.FirstOrDefault(e => !e.IsBuiltIn && ReadHash(e.Id) == hash);
        if (same is not null)
        {
            return (same, AddOutcome.AlreadyAdded, false);
        }

        var tmp = ExtractAndValidate(zip);
        var m = ReadManifest(tmp);
        var existing = Entries.FirstOrDefault(e => !e.IsBuiltIn && e.Manifest.Name == m.Name)?.Id;
        var id = existing ?? Guid.NewGuid().ToString("N");
        // 一覧は取り込んだ順(フォルダの作成日時)。置き換えても並びが変わらないよう、前の作成日時を引き継ぐ
        DateTime? created = existing is not null ? Directory.GetCreationTimeUtc(DirOf(id)) : null;
        Replace(DirOf(id), tmp);
        Directory.SetCreationTimeUtc(DirOf(id), created ?? DateTime.UtcNow);
        try
        {
            File.WriteAllText(HashFileOf(id), hash);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log($"hash: {e.Message}");
        }

        ReloadEntries();
        var entry = Entries.FirstOrDefault(e => e.Id == id)
                    ?? throw new InvalidDataException("manifest.json was not found at the top of the zip.");
        return (entry, existing is null ? AddOutcome.Added : AddOutcome.Replaced, existing is not null && id == ActiveId);
    }

    private string? ReadHash(string id)
    {
        try
        {
            return File.Exists(HashFileOf(id)) ? File.ReadAllText(HashFileOf(id)) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>使う中身を切り替える(1 つだけ)。呼んだ側でグラフ画面を読み込み直すこと。</summary>
    public void Activate(string id)
    {
        if (Entries.All(e => e.Id != id))
        {
            return;
        }

        ActiveId = id;
        setting.WebContentActive = id;
        setting.Save();
    }

    /// <summary>
    /// 取り込んだ zip を消す。同梱の標準版は消せない。使っていたものを消したら標準版に戻す
    /// (戻り値 true。グラフ画面を読み込み直すこと)。
    /// </summary>
    public bool Remove(string id)
    {
        if (id == BuiltInId || Entries.All(e => e.Id != id))
        {
            return false;
        }

        Directory.Delete(DirOf(id), recursive: true);
        TryDeleteFile(HashFileOf(id));
        var wasActive = id == ActiveId;
        if (wasActive)
        {
            Activate(BuiltInId);
        }

        ReloadEntries();
        return wasActive;
    }

    /// <summary>取り込んで、すぐ使う(自己テスト用の近道)</summary>
    public WebContentManifest ImportAndActivate(string zip)
    {
        var (entry, _, _) = Add(zip);
        Activate(entry.Id);
        return entry.Manifest;
    }

    #endregion

#if DEBUG
    /// <summary>
    /// 取り込んだ zip 全部と、使っている ID を一時フォルダへ写す(自己テストが zip を取り込む前に。同じ name だと置き換えるため)。
    /// 写せなければ null。<see cref="RestoreImported"/> で戻す。
    /// </summary>
    internal (string Dir, string ActiveId)? CopyImportedAside()
    {
        var dst = Path.Combine(Path.GetTempPath(), "autotest-zips-" + Guid.NewGuid().ToString("N"));
        try
        {
            CopyDir(ZipsDir, dst);
            return (dst, ActiveId);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log($"copy zips aside: {e.Message}");
            TryDelete(dst);
            return null;
        }

        static void CopyDir(string from, string to)
        {
            Directory.CreateDirectory(to);
            Directory.SetCreationTimeUtc(to, Directory.GetCreationTimeUtc(from));   // 一覧の並び(作成日時)も写す
            foreach (var f in Directory.GetFiles(from))
            {
                File.Copy(f, Path.Combine(to, Path.GetFileName(f)));
            }

            foreach (var d in Directory.GetDirectories(from))
            {
                CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
            }
        }
    }

    /// <summary><see cref="CopyImportedAside"/> で写したものを zips\ へ戻し、使っていた ID に戻す。</summary>
    internal void RestoreImported((string Dir, string ActiveId) kept)
    {
        TryDelete(ZipsDir);
        try
        {
            Directory.Move(kept.Dir, ZipsDir);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log($"restore zips: {e.Message}");
            Directory.CreateDirectory(ZipsDir);
        }

        setting.WebContentActive = kept.ActiveId;
        setting.Save();
        Prepare();
    }
#endif

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
            try
            {
                Directory.Move(dest, old);
            }
            catch
            {
                TryDelete(tmp);   // 前の中身をどけられなければ、展開したものも残さない
                throw;
            }
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

    private static void TryDeleteFile(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log($"could not delete {file}: {e.Message}");
        }
    }

    private static void Log(string message) => System.Diagnostics.Debug.WriteLine($"[WebContent] {message}");
}
