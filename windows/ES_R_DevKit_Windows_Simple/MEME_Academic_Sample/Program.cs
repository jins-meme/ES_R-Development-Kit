namespace MEME_Academic_Sample;

internal static class Program
{
    /// <summary>アプリケーションのメイン エントリ ポイントです。</summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MEME_Academic_Sample());
    }
}
