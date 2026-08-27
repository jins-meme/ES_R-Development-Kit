using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample;

internal static class Program
{
    /// <summary>アプリケーションのメイン エントリ ポイントです。</summary>
    /// <param name="args">
    /// エクスプローラーの「プログラムから開く」から渡される CSV のパス。
    /// 指定されていれば起動直後に File Replay として読み込む。
    /// </param>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        FileAssociation.EnsureRegistered();

        var replayPath = args.FirstOrDefault(a =>
            a.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) && File.Exists(a));

        Application.Run(new MainForm(replayPath));
    }
}
