using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample.Services;

/// <summary>
/// グラフ画面の日別キャッシュ(Logic4 などの 15 秒窓 + マーカー)を、ページの求めで一覧・読み・書きする
/// (ページ → アプリの cacheList / cacheRead / cacheWrite、返りは jmasHost.cacheResult。仕様は webview/BRIDGE.md の Cache files)。
/// Mac 版 CacheStore.swift と同じ規則(名前の形・16 MB の上限・.tmp に書いてから置き換える・エラーの名前・置き場所の決め方)。
///
/// - 置き場所は <c>&lt;フォルダ&gt;/logic4-cache/&lt;device&gt;/&lt;YYYY-MM-DD&gt;.json</c>。フォルダは設定の Graph Cache Folder
///   (<see cref="UserSetting.CacheFolder"/>)、空なら保存フォルダ(Save File Path。データ CSV と同じ)。選んだフォルダの名前が
///   logic4-cache なら、その中へ直に置く(Android・Mac と同じ。Syncthing で同期している logic4-cache を選べる)。
///   設定は読み書きのたびに見る(計測の途中で変えても次から効く)。どちらも空なら error = "no_access"。
/// - 書くときは <c>&lt;name&gt;.tmp</c> に全部書いてから <c>&lt;name&gt;</c> へ置き換える(同期の道具に書きかけを渡さない)。
///   置き換えは File.Move(overwrite: true)(同じボリュームの中では名前の変更 = MoveFileEx の REPLACE_EXISTING)。
/// - このフォルダの外は読み書きしない(device / name は形を決めて検査し、決まった 2 段の下にしか触らない)。
/// - 入出力は 1 本ずつ順に別スレッドで行い(<see cref="HandleAsync"/>)、結果は JSON の文字列で返す。
/// </summary>
public sealed partial class CacheStore
{
    /// <summary>start の features・openReplay の features に入れる名前</summary>
    public const string Feature = "cache";

    public const string DirName = "logic4-cache";

    /// <summary>1 ファイルの上限(バイト。BRIDGE.md)</summary>
    public const int MaxBytes = 16 * 1024 * 1024;

    private const string TmpSuffix = ".tmp";

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>logic4-cache の場所を返す(null = 置き場所が決まっていない → no_access)。テストでは一時フォルダを返すものを渡す</summary>
    private readonly Func<string?> rootOf;

    /// <summary>ページの求めを届いた順に 1 本ずつ行う</summary>
    private readonly SemaphoreSlim gate = new(1, 1);

    public CacheStore(Func<string?> rootOf) => this.rootOf = rootOf;

    /// <summary>設定から: Graph Cache Folder、空なら Save File Path。名前が logic4-cache ならそのもの、違えばその下の logic4-cache</summary>
    public static string? RootFrom(UserSetting setting)
    {
        var chosen = setting.CacheFolder?.Trim() ?? "";
        var basePath = chosen.Length > 0 ? chosen : setting.SaveFilePath?.Trim() ?? "";
        if (basePath.Length == 0)
        {
            return null;
        }

        var trimmed = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(Path.GetFileName(trimmed), DirName, StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : Path.Combine(trimmed, DirName);
    }

    #region 名前

    [GeneratedRegex("^[0-9A-F]{12}$")]
    private static partial Regex DeviceRegex();

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}\.json$")]
    private static partial Regex NameRegex();

    [GeneratedRegex("^([0-9A-Fa-f]{12})_")]
    private static partial Regex FileDeviceRegex();

    public static bool ValidDevice(string s) => DeviceRegex().IsMatch(s);

    public static bool ValidName(string s) => NameRegex().IsMatch(s);

    /// <summary>端末のアドレス(AA:BB:… / AABB…)→ device(コロンなしの大文字 12 桁)。形が違えば null(ページはキャッシュを使わない)</summary>
    public static string? DeviceOfAddress(string? address)
    {
        var s = (address ?? "").Replace(":", "", StringComparison.Ordinal).ToUpperInvariant();
        return ValidDevice(s) ? s : null;
    }

    /// <summary>データの CSV 名の先頭(&lt;MAC&gt;_YYYYmmddHHMMSS…)→ device。取れなければ null</summary>
    public static string? DeviceOfFileName(string? fileName)
    {
        var m = FileDeviceRegex().Match(fileName ?? "");
        return m.Success ? m.Groups[1].Value.ToUpperInvariant() : null;
    }

    #endregion

    #region ページからのメッセージ

    /// <summary>
    /// kind = cacheList / cacheRead / cacheWrite を別スレッドで、届いた順に 1 本ずつ行う。
    /// idJson はページの id を JSON にしたもの(そのまま返りに入れる)。返りは jmasHost.cacheResult に渡す JSON の文字列。
    /// </summary>
    public async Task<string> HandleAsync(string idJson, string kind, string device, string name, string text)
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var r = await Task.Run(() => Run(kind, device, name, text)).ConfigureAwait(false);
            return Answer(r, idJson);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>1 件ぶんを同期に行う(HandleAsync の中・テストから)。返りは cacheResult の中身(id を除く)</summary>
    public Dictionary<string, object?> Run(string kind, string device, string name, string text)
    {
        if (!ValidDevice(device))
        {
            return Err("bad_name");
        }

        if (kind != "cacheList" && !ValidName(name))
        {
            return Err("bad_name");
        }

        var root = rootOf();
        if (string.IsNullOrEmpty(root))
        {
            return Err("no_access");
        }

        var dir = Path.Combine(root, device);
        try
        {
            switch (kind)
            {
                case "cacheList":
                {
                    var files = new List<Dictionary<string, object?>>();
                    if (!Directory.Exists(dir))
                    {
                        return new() { ["ok"] = true, ["files"] = files };
                    }

                    foreach (var path in Directory.EnumerateFiles(dir).Order(StringComparer.Ordinal))
                    {
                        var n = Path.GetFileName(path);
                        if (!ValidName(n))
                        {
                            continue;
                        }

                        var info = new FileInfo(path);
                        files.Add(new()
                        {
                            ["name"] = n,
                            ["size"] = info.Length,
                            ["mtime"] = Math.Round((info.LastWriteTimeUtc - DateTime.UnixEpoch).TotalMilliseconds),
                        });
                    }

                    return new() { ["ok"] = true, ["files"] = files };
                }

                case "cacheRead":
                {
                    var f = Path.Combine(dir, name);
                    if (!File.Exists(f))
                    {
                        return Err("not_found");
                    }

                    if (new FileInfo(f).Length > MaxBytes)
                    {
                        return Err("too_large");
                    }

                    var data = File.ReadAllBytes(f);
                    if (data.Length > MaxBytes)
                    {
                        return Err("too_large");
                    }

                    return new() { ["ok"] = true, ["text"] = Utf8.GetString(data) };
                }

                case "cacheWrite":
                {
                    var data = Utf8.GetBytes(text);
                    if (data.Length > MaxBytes)
                    {
                        return Err("too_large");
                    }

                    Directory.CreateDirectory(dir);
                    var dst = Path.Combine(dir, name);
                    var tmp = dst + TmpSuffix;
                    File.Delete(tmp);                          // 前に落ちたときの書きかけ(無ければ何もしない)
                    File.WriteAllBytes(tmp, data);
                    File.Move(tmp, dst, overwrite: true);      // 同じボリュームの中の置き換え(名前の変更)
                    return new() { ["ok"] = true, ["size"] = data.Length };
                }

                default:
                    return Err("bad_kind");
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Debug.WriteLine($"[CacheStore] {kind} {device}/{name} failed: {e.Message}");
            return Err("io_error");
        }
    }

    private static Dictionary<string, object?> Err(string code) => new() { ["ok"] = false, ["error"] = code };

    /// <summary>返りの JSON(先頭に id を入れる)</summary>
    public static string Answer(Dictionary<string, object?> r, string idJson)
    {
        string body;
        try
        {
            body = JsonSerializer.Serialize(r);
        }
        catch (NotSupportedException)
        {
            return $"{{\"id\":{idJson},\"ok\":false,\"error\":\"io_error\"}}";
        }

        // "{" を外して id を前に足す
        return body == "{}" ? $"{{\"id\":{idJson}}}" : $"{{\"id\":{idJson},{body[1..]}";
    }

    #endregion
}
