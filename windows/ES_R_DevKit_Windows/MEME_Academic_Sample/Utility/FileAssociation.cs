using Microsoft.Win32;

namespace MEME_Academic_Sample.Utility;

/// <summary>
/// エクスプローラーの「プログラムから開く」に本アプリを載せるための登録。
/// HKEY_CURRENT_USER にだけ書くので管理者権限は要らず、既定の関連付けも奪わない
/// (.csv を開くアプリが Excel のままでも、右クリックの一覧に本アプリが並ぶ)。
/// </summary>
public static class FileAssociation
{
    private const string Extension = ".csv";

    /// <summary>起動のたびに呼ぶ。内容が変わっていなければ何も書かない。</summary>
    public static void EnsureRegistered()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                return;
            }

            var exeName = Path.GetFileName(exePath);
            var command = $"\"{exePath}\" \"%1\"";

            var applicationKeyPath = $@"Software\Classes\Applications\{exeName}";
            using (var application = Registry.CurrentUser.CreateSubKey(applicationKeyPath))
            {
                if (application is null)
                {
                    return;
                }

                // 同じ内容なら書き込みを省く(exe を動かしたときだけ更新される)。
                using var commandKey = application.CreateSubKey(@"shell\open\command");
                if (commandKey?.GetValue(null) as string == command)
                {
                    return;
                }

                application.SetValue("FriendlyAppName", AppInfo.ProductName);
                commandKey?.SetValue(null, command);

                using var icon = application.CreateSubKey("DefaultIcon");
                icon?.SetValue(null, $"{exePath},0");

                // 「プログラムから開く」の一覧に .csv 用として並べるための宣言。
                using var supportedTypes = application.CreateSubKey("SupportedTypes");
                supportedTypes?.SetValue(Extension, string.Empty);
            }

            // 拡張子側からも候補として参照させる。
            using var openWith = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{Extension}\OpenWithList\{exeName}");
            openWith?.Close();
        }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            // 登録できなくてもアプリ自体は使えるので、黙って諦める。
        }
    }
}
