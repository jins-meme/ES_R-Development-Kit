using System.Text;
using System.Text.Json;
using MEME_Academic_Sample.Services;
using MEME_Academic_Sample.Utility;
using Xunit;

namespace MEME_Academic_Sample.Tests;

/// <summary>日別キャッシュ(webview/BRIDGE.md の Cache files)。iOS の CacheStoreTests・Android の CacheStoreTest と同じ確かめ方。</summary>
public class CacheStoreTests : IDisposable
{
    private const string Dev = "D3FE9978A82E";
    private readonly string _root = Path.Combine(Directory.CreateTempSubdirectory("meme_cache_tests").FullName, CacheStore.DirName);
    private readonly CacheStore _store;

    public CacheStoreTests() => _store = new CacheStore(() => _root);

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_root)!, recursive: true);

    [Fact]
    public void ListOfMissingFolderIsEmpty()
    {
        var r = _store.Run("cacheList", Dev, "", "");
        Assert.True((bool)r["ok"]!);
        Assert.Empty((List<Dictionary<string, object?>>)r["files"]!);
    }

    [Fact]
    public void WriteReadListAndOverwrite()
    {
        var r = _store.Run("cacheWrite", Dev, "2026-10-27.json", "{\"a\":\"日本語\"}");
        Assert.True((bool)r["ok"]!);
        Assert.Equal(Encoding.UTF8.GetByteCount("{\"a\":\"日本語\"}"), r["size"]);
        r = _store.Run("cacheWrite", Dev, "2026-10-27.json", "{\"b\":2}");   // 上書き
        Assert.True((bool)r["ok"]!);
        r = _store.Run("cacheRead", Dev, "2026-10-27.json", "");
        Assert.Equal("{\"b\":2}", r["text"]);
        Assert.DoesNotContain(Directory.EnumerateFiles(Path.Combine(_root, Dev)), f => f.EndsWith(".tmp", StringComparison.Ordinal));
        File.WriteAllText(Path.Combine(_root, Dev, "notes.txt"), "x");      // 名前の形が違うものは一覧に出さない
        var files = (List<Dictionary<string, object?>>)_store.Run("cacheList", Dev, "", "")["files"]!;
        Assert.Single(files);
        Assert.Equal("2026-10-27.json", files[0]["name"]);
        Assert.Equal(7L, files[0]["size"]);
        Assert.True((double)files[0]["mtime"]! > 1.7e12);
    }

    [Fact]
    public void Errors()
    {
        Assert.Equal("not_found", _store.Run("cacheRead", Dev, "2026-01-01.json", "")["error"]);
        Assert.Equal("bad_name", _store.Run("cacheList", "d3fe9978a82e", "", "")["error"]);
        Assert.Equal("bad_name", _store.Run("cacheRead", "../" + Dev, "2026-01-01.json", "")["error"]);
        Assert.Equal("bad_name", _store.Run("cacheWrite", Dev, "../x.json", "")["error"]);
        Assert.Equal("bad_name", _store.Run("cacheWrite", Dev, "2026-01-01.json.tmp", "")["error"]);
        Assert.Equal("bad_kind", _store.Run("cacheDelete", Dev, "2026-01-01.json", "")["error"]);
        var big = new string('x', CacheStore.MaxBytes + 1);
        Assert.Equal("too_large", _store.Run("cacheWrite", Dev, "2026-01-01.json", big)["error"]);
        Assert.Equal("no_access", new CacheStore(() => null).Run("cacheList", Dev, "", "")["error"]);
    }

    [Fact]
    public void DeviceNames()
    {
        Assert.Equal(Dev, CacheStore.DeviceOfAddress("d3:fe:99:78:a8:2e"));
        Assert.Equal(Dev, CacheStore.DeviceOfAddress("D3FE9978A82E"));
        Assert.Null(CacheStore.DeviceOfAddress(""));
        Assert.Null(CacheStore.DeviceOfAddress("UNKNOWN"));
        Assert.Equal(Dev, CacheStore.DeviceOfFileName("d3fe9978a82e_20261026234146.csv.gz"));
        Assert.Null(CacheStore.DeviceOfFileName("ESR_20261026234146.csv.gz"));
    }

    [Fact]
    public void RootFromSettings()
    {
        var save = Path.Combine("C:", "data");
        Assert.Equal(Path.Combine(save, CacheStore.DirName), CacheStore.RootFrom(new UserSetting { SaveFilePath = save, CacheFolder = "" }));
        var chosen = Path.Combine("D:", "sync", "ESR Logger");
        Assert.Equal(Path.Combine(chosen, CacheStore.DirName), CacheStore.RootFrom(new UserSetting { SaveFilePath = save, CacheFolder = chosen }));
        var direct = Path.Combine("D:", "sync", "logic4-cache");
        Assert.Equal(direct, CacheStore.RootFrom(new UserSetting { SaveFilePath = save, CacheFolder = direct }));
        Assert.Null(CacheStore.RootFrom(new UserSetting { SaveFilePath = "", CacheFolder = "" }));
    }

    [Fact]
    public void AnswerPutsIdFirst()
    {
        var json = CacheStore.Answer(new() { ["ok"] = true, ["size"] = 3 }, "7");
        Assert.StartsWith("{\"id\":7,", json);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(3, doc.RootElement.GetProperty("size").GetInt32());
        Assert.Equal("{\"id\":\"a\"}", CacheStore.Answer(new(), "\"a\""));
    }

    [Fact]
    public async Task HandleAsyncAnswersWithId()
    {
        var json = await _store.HandleAsync("1", "cacheWrite", Dev, "2026-11-16.json", "{}");
        Assert.StartsWith("{\"id\":1,", json);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
    }
}
