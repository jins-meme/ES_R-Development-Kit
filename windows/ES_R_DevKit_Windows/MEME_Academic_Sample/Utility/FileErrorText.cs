namespace MEME_Academic_Sample.Utility;

/// <summary>
/// ファイル操作の例外をダイアログ用の日本語にする。
/// .NET の例外メッセージは OS の言語によらず英語("Access to the path is denied." など)なので、種類で言い分ける。
/// </summary>
public static class FileErrorText
{
    public static string Of(Exception e) => e switch
    {
        UnauthorizedAccessException => "ファイルにアクセスできませんでした。別のアプリで開かれていないか、書き込みが許可されているかを確認してください。",
        FileNotFoundException f => $"ファイルが見つかりません。{f.FileName}",
        DirectoryNotFoundException => "フォルダが見つかりません。",
        PathTooLongException => "パスが長すぎます。",
        IOException => "ファイルを読み書きできませんでした。別のアプリで開かれていないか、ディスクの空きを確認してください。",
        ArgumentException => "パスの形式が正しくありません。",
        // InvalidDataException など、アプリ側で日本語の文を入れて投げているもの
        _ => e.Message,
    };
}
