using MEME_Academic_Sample.UI;
using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample;

/// <summary>
/// ドラッグで選んだ区間を CSV へ切り出すときのファイル名入力ダイアログ。
/// 保存に失敗したらダイアログを閉じずにエラーを出す(Mac 版 CutFileDialogView と同じ)。
/// </summary>
public sealed class CutFileForm : Form
{
    private readonly TextBox _input = new();
    private readonly Label _error = new();
    private readonly string _directory;

    /// <param name="directory">切り出し先のフォルダ(再生元 CSV と同じ場所)。</param>
    /// <param name="defaultName">初期表示するファイル名。</param>
    /// <param name="rowCount">切り出す行数。件数が分かるように見出しへ出す。</param>
    public CutFileForm(string directory, string defaultName, int rowCount)
    {
        _directory = directory;

        Icon = AppInfo.LoadIcon();
        Text = "Save selected range as CSV";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 168);
        Font = new Font("Segoe UI", 9F);

        var heading = new Label
        {
            Text = $"Save selected range as CSV ({rowCount:N0} rows)",
            Location = new Point(16, 16),
            Size = new Size(390, 20),
            Font = new Font(Font, FontStyle.Bold),
        };

        var folder = new Label
        {
            Text = directory,
            Location = new Point(16, 40),
            Size = new Size(390, 20),
            ForeColor = SystemColors.GrayText,
            AutoEllipsis = true,
        };

        _input.Text = defaultName;
        _input.Location = new Point(16, 66);
        _input.Size = new Size(388, 23);
        _input.TextChanged += (_, _) => _error.Text = string.Empty;

        _error.Location = new Point(16, 96);
        _error.Size = new Size(388, 20);
        _error.ForeColor = Color.FromArgb(200, 40, 40);

        var ok = new RoundedButton { Text = "OK", Location = new Point(324, 124), Size = new Size(80, 28) };
        var cancel = new RoundedButton
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(236, 124),
            Size = new Size(80, 28),
        };

        ok.Click += (_, _) => TryAccept();

        Controls.AddRange([heading, folder, _input, _error, cancel, ok]);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    /// <summary>OK が押されて検証を通ったときの出力先フルパス。</summary>
    public string DestinationPath { get; private set; } = string.Empty;

    /// <summary>入力を検証し、通ったらダイアログを閉じる。閉じない場合はエラーを表示する。</summary>
    private void TryAccept()
    {
        var name = _input.Text.Trim();
        if (name.Length == 0 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            _error.Text = "Invalid file name.";
            return;
        }

        if (!name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            name += ".csv";
        }

        var destination = Path.Combine(_directory, name);
        if (File.Exists(destination))
        {
            _error.Text = "File already exists.";
            return;
        }

        DestinationPath = destination;
        DialogResult = DialogResult.OK;
        Close();
    }
}
