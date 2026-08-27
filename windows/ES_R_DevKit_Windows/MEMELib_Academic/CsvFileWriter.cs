using System.Text;

namespace MEMELib_Academic;

/// <summary>
/// CSV への追記。100Hz で 1 行ずつ open/close すると取りこぼすため、
/// ファイルは開いたまま保持し、一定行ごとにフラッシュする。
/// </summary>
internal sealed class CsvFileWriter : IDisposable
{
    /// <summary>100Hz 計測でおよそ 1 秒に 1 回フラッシュする。</summary>
    private const int FlushInterval = 100;

    private readonly Lock _gate = new();
    private StreamWriter? _writer;
    private string? _path;
    private int _sinceFlush;

    public void WriteLine(string directory, string fileName, string line)
    {
        var path = Path.Combine(directory, fileName);
        lock (_gate)
        {
            if (_writer is null || !string.Equals(_path, path, StringComparison.OrdinalIgnoreCase))
            {
                CloseCore();
                Directory.CreateDirectory(directory);
                // BOM なし UTF-8。改行は他プラットフォームの出力に合わせて CRLF。
                _writer = new StreamWriter(path, append: true, new UTF8Encoding(false))
                {
                    NewLine = "\r\n",
                };
                _path = path;
                _sinceFlush = 0;
            }

            _writer.WriteLine(line);
            if (++_sinceFlush >= FlushInterval)
            {
                _writer.Flush();
                _sinceFlush = 0;
            }
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            CloseCore();
        }
    }

    private void CloseCore()
    {
        _writer?.Flush();
        _writer?.Dispose();
        _writer = null;
        _path = null;
        _sinceFlush = 0;
    }

    public void Dispose() => Close();
}
