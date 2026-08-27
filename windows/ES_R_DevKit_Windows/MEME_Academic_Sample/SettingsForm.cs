using System.Diagnostics;
using System.Globalization;
using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample;

/// <summary>Setting ダイアログ。Mac 版 SettingsView に対応する。</summary>
public partial class SettingsForm : Form
{
    private readonly UserSetting setting;

    public SettingsForm(UserSetting setting)
    {
        this.setting = setting;
        InitializeComponent();
        Icon = AppInfo.LoadIcon();
        LoadSettings();
    }

    private void LoadSettings()
    {
        tb_SaveFilePath.Text = setting.SaveFilePath;
        tb_AccOffsetX.Text = setting.AccOffsetX.ToString("0.###", CultureInfo.InvariantCulture);
        tb_AccOffsetY.Text = setting.AccOffsetY.ToString("0.###", CultureInfo.InvariantCulture);
        tb_AccOffsetZ.Text = setting.AccOffsetZ.ToString("0.###", CultureInfo.InvariantCulture);
        ck_ShowSaveFileDialog.Checked = setting.ShowSaveFileDialog;
        ck_ConvertToLocalTime.Checked = setting.ConvertToLocalTime;
        ck_ExternalOutputSocket.Checked = setting.ExternalOutputSocket;
        tb_LocalPort.Text = setting.LocalPort;
        lb_LocalIp.Text = $"Local IP: {NetworkInfo.GetLocalIPv4Address()}";
    }

    private void bt_Apply_Click(object sender, EventArgs e)
    {
        var port = tb_LocalPort.Text.Trim();
        if (ck_ExternalOutputSocket.Checked && (!ushort.TryParse(port, out var value) || value == 0))
        {
            MessageBox.Show(this, "Local Port には 1〜65535 の値を入れてください。", "Setting",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tb_LocalPort.Focus();
            return;
        }

        setting.SaveFilePath = tb_SaveFilePath.Text.Trim();
        setting.AccOffsetX = ParseOffset(tb_AccOffsetX.Text);
        setting.AccOffsetY = ParseOffset(tb_AccOffsetY.Text);
        setting.AccOffsetZ = ParseOffset(tb_AccOffsetZ.Text);
        setting.ShowSaveFileDialog = ck_ShowSaveFileDialog.Checked;
        setting.ConvertToLocalTime = ck_ConvertToLocalTime.Checked;
        setting.ExternalOutputSocket = ck_ExternalOutputSocket.Checked;
        setting.LocalPort = port;
        setting.Save();

        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>数値として読めない入力は 0 として扱う(Mac 版 `Double(xAxis) ?? 0` と同じ)。</summary>
    private static double ParseOffset(string text) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private void bt_SelectFolder_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "CSV の保存先",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(tb_SaveFilePath.Text)
                ? tb_SaveFilePath.Text
                : UserSetting.DefaultSaveDirectory(),
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            tb_SaveFilePath.Text = dialog.SelectedPath;
        }
    }

    private void bt_OpenFolder_Click(object sender, EventArgs e)
    {
        var path = tb_SaveFilePath.Text.Trim();
        if (path.Length == 0)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show(this, $"フォルダを開けませんでした。\n{ex.Message}", "Setting",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
