using MEMELib_Academic;
using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample;

internal static class Program
{
    /// <summary>起動引数(Debug ビルドの自己テストが読む。MainForm.AutoTest.cs)</summary>
    internal static string[] Args { get; private set; } = [];

    /// <summary>アプリケーションのメイン エントリ ポイントです。</summary>
    /// <param name="args">
    /// エクスプローラーの「プログラムから開く」から渡される CSV(.csv / .csv.gz)のパス。
    /// 指定されていれば起動直後に File Replay として読み込む。
    /// </param>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        FileAssociation.EnsureRegistered();

        Args = args;
        // 自己テストの引数(--csv など)は再生するファイルとして扱わない
        var replayPath = args.Contains("--autotest")
            ? null
            : args.FirstOrDefault(a => CsvFile.IsOpenTarget(a) && File.Exists(a));

        Application.Run(new MainForm(replayPath));
    }
}
