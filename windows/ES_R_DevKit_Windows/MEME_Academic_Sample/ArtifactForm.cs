using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample;

/// <summary>
/// チャートをクリックしたときに出す Artifact 入力ダイアログ。
/// Mac 版 ContentView の `.alert("Artifact")` に対応する。
/// </summary>
public sealed class ArtifactForm : Form
{
    private readonly TextBox _input = new();

    public ArtifactForm()
    {
        Icon = AppInfo.LoadIcon();
        Text = "Artifact";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 128);
        Font = new Font("Segoe UI", 9F);

        var message = new Label
        {
            Text = "Artifact will be added on 'Record' timing",
            Location = new Point(16, 16),
            Size = new Size(330, 20),
            ForeColor = SystemColors.GrayText,
        };

        _input.Location = new Point(16, 42);
        _input.Size = new Size(328, 23);
        _input.PlaceholderText = "X";

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(264, 82),
            Size = new Size(80, 28),
        };
        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(176, 82),
            Size = new Size(80, 28),
        };

        Controls.AddRange([message, _input, cancel, ok]);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    /// <summary>
    /// 入力された文字列。空なら "X"。列が崩れないようカンマと改行は空白へ置き換える。
    /// </summary>
    public string ArtifactText
    {
        get
        {
            var text = _input.Text.Replace(',', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim();
            return text.Length == 0 ? "X" : text;
        }
    }
}
