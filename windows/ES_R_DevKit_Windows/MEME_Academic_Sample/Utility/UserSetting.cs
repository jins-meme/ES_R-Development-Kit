using System.Text.Json;
using System.Text.Json.Serialization;

namespace MEME_Academic_Sample.Utility;

/// <summary>
/// アプリ設定。Mac 版は UserDefaults に持つが、Windows では
/// %APPDATA%\JINS\MEME_Academic\settings.json に置く。
/// </summary>
public sealed class UserSetting
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JINS", "MEME_Academic");

    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    /// <summary>CSV の保存先。既定は ドキュメント\JINS\MEME_Academic。</summary>
    public string SaveFilePath { get; set; } = DefaultSaveDirectory();

    /// <summary>加速度に加算する表示オフセット。CSV には影響せず、チャートの見え方だけを変える。</summary>
    public double AccOffsetX { get; set; }

    public double AccOffsetY { get; set; }

    public double AccOffsetZ { get; set; }

    /// <summary>計測終了後に保存先を選び直すダイアログを出すか。</summary>
    public bool ShowSaveFileDialog { get; set; }

    /// <summary>
    /// 時刻表示をローカルタイムへ変換するか(既定 ON)。CSV／ソケットへ記録する時刻は
    /// 常に UTC で、この設定はチャートの X 軸ラベルにのみ効く。
    /// </summary>
    public bool ConvertToLocalTime { get; set; } = true;

    /// <summary>TCP ソケットによる外部出力を行うか。</summary>
    public bool ExternalOutputSocket { get; set; }

    /// <summary>待ち受けポート。Mac 版の既定値に合わせて 88。</summary>
    public string LocalPort { get; set; } = "88";

    public static string DefaultSaveDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JINS", "MEME_Academic");

    public static UserSetting Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var loaded = JsonSerializer.Deserialize<UserSetting>(File.ReadAllText(SettingsPath), JsonOptions);
                if (loaded is not null)
                {
                    return loaded;
                }
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // 壊れた設定ファイルで起動できなくなるより、既定値で立ち上げるほうがよい。
        }

        var setting = new UserSetting();
        setting.Save();
        return setting;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // 保存できなくても計測は続けられるようにする。
        }
    }

    /// <summary>保存先を確保して返す。作成できなければ既定のディレクトリへ退避する。</summary>
    public string EnsureSaveDirectory()
    {
        var candidates = new[] { SaveFilePath, DefaultSaveDirectory() };
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            try
            {
                Directory.CreateDirectory(candidate);
                return candidate;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
            {
                // 次の候補を試す。
            }
        }

        return Directory.GetCurrentDirectory();
    }
}
