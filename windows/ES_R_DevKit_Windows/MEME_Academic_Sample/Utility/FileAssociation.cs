using Microsoft.Win32;

namespace MEME_Academic_Sample.Utility;

/// <summary>
/// エクスプローラーの「プログラムから開く」に本アプリを載せるための登録。
/// HKEY_CURRENT_USER にだけ書くので管理者権限は要らず、既定の関連付けも奪わない
/// (.csv を開くアプリが Excel のままでも、右クリックの一覧に本アプリが並ぶ)。
/// </summary>
public static class FileAssociation
{
    /// <summary>
    /// 「プログラムから開く」に並べる拡張子。.csv.gz だけを拾いたいが、Windows の
    /// 拡張子判定は最後の 1 段しか見ないため、gz 全般で登録するほかない
    /// (Mac 版も同じ理由で org.gnu.gnu-zip-archive として登録している)。
    /// CSV でない .gz を開いた場合は読み込み時に形式違いとして弾かれる。
    /// </summary>
    private static readonly string[] Extensions = [".csv", ".gz"];

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

                using var commandKey = application.CreateSubKey(@"shell\open\command");
                // 「プログラムから開く」の一覧に並べるための宣言。
                using var supportedTypes = application.CreateSubKey("SupportedTypes");

                // 同じ内容なら書き込みを省く(exe を動かしたときと、対応拡張子が
                // 増えたときだけ更新される)。
                if (commandKey?.GetValue(null) as string == command &&
                    Array.TrueForAll(Extensions, e => supportedTypes?.GetValue(e) is not null))
                {
                    return;
                }

                application.SetValue("FriendlyAppName", AppInfo.ProductName);
                commandKey?.SetValue(null, command);

                using var icon = application.CreateSubKey("DefaultIcon");
                icon?.SetValue(null, $"{exePath},0");

                foreach (var extension in Extensions)
                {
                    supportedTypes?.SetValue(extension, string.Empty);
                }
            }

            // 拡張子側からも候補として参照させる。
            foreach (var extension in Extensions)
            {
                using var openWith = Registry.CurrentUser.CreateSubKey(
                    $@"Software\Classes\{extension}\OpenWithList\{exeName}");
                openWith?.Close();
            }
        }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            // 登録できなくてもアプリ自体は使えるので、黙って諦める。
        }
    }
}
