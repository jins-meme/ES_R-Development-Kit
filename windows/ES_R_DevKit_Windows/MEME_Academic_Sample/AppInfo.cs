using System.Reflection;

namespace MEME_Academic_Sample;

/// <summary>アプリ名・バージョン・アイコンの取得口。値は csproj のメタデータが元。</summary>
internal static class AppInfo
{
    /// <summary>app.ico の埋め込みリソース名(RootNamespace + ファイル名)。</summary>
    private const string IconResourceName = "MEME_Academic_Sample.app.ico";

    public static string ProductName =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product
        ?? "JINS MEME DataLogger";

    public static string Version =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0]
        ?? "0.0.0";

    /// <summary>ウィンドウに設定するアイコン。読めなければ null を返し、既定のまま使う。</summary>
    public static Icon? LoadIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(IconResourceName);
        return stream is null ? null : new Icon(stream);
    }
}
