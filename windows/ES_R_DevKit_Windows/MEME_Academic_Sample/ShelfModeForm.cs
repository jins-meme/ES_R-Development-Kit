using MEME_Academic_Sample.UI;

namespace MEME_Academic_Sample;

/// <summary>
/// Disconnect の長押しで出す Shelf mode(保管モード)の確認ダイアログ。
/// Mac 版 ContentView の `.alert("Do you want to enter shelf mode?")` に対応する。
/// ボタンは Yes / Cancel なので、MessageBox ではなく自前のフォームで出す。
/// </summary>
public sealed class ShelfModeForm : Form
{
    private const string Message =
        "In shelf mode, all pairing capabilities are disabled and power consumption is reduced. " +
        "To exit shelf mode, please recharge the device.";

    public ShelfModeForm()
    {
        Icon = AppInfo.LoadIcon();
        Text = "Do you want to enter shelf mode?";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(440, 168);
        Font = new Font("Segoe UI", 9F);

        var heading = new Label
        {
            Text = "Do you want to enter shelf mode?",
            Location = new Point(16, 16),
            Size = new Size(408, 20),
            Font = new Font(Font, FontStyle.Bold),
        };

        var detail = new Label
        {
            Text = Message,
            Location = new Point(16, 44),
            Size = new Size(408, 72),
            ForeColor = SystemColors.GrayText,
        };

        var yes = new RoundedButton
        {
            Text = "Yes",
            DialogResult = DialogResult.Yes,
            Location = new Point(344, 124),
            Size = new Size(80, 28),
            TabIndex = 1,
        };
        var cancel = new RoundedButton
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(256, 124),
            Size = new Size(80, 28),
            TabIndex = 0,
        };

        Controls.AddRange([heading, detail, cancel, yes]);
        // 取り消しのほうを既定にする(初期フォーカスも Enter も Cancel)。
        // Shelf mode からの復帰は充電のみで、アプリからは戻せないため。
        AcceptButton = cancel;
        CancelButton = cancel;
    }
}
