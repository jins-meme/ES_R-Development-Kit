using System.Diagnostics;
using System.Globalization;
using MEME_Academic_Sample.Services;
using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample;

/// <summary>Setting ダイアログ。Mac 版 SettingsView に対応する。</summary>
public partial class SettingsForm : Form
{
    private readonly UserSetting setting;
    private readonly WebContentStore webContent;

    /// <summary>計測中・再生中は Display Engine の zip を切り替えさせない(グラフ画面を読み込み直すと表示中のものが消えるため)</summary>
    private readonly bool canSwitchEngine;

    public SettingsForm(UserSetting setting, WebContentStore webContent, bool canSwitchEngine)
    {
        this.setting = setting;
        this.webContent = webContent;
        this.canSwitchEngine = canSwitchEngine;
        InitializeComponent();
        Icon = AppInfo.LoadIcon();
        LoadSettings();
        ShowWebContent();
    }

    private void LoadSettings()
    {
        tb_SaveFilePath.Text = setting.SaveFilePath;
        tb_AccOffsetX.Text = setting.AccOffsetX.ToString("0.###", CultureInfo.InvariantCulture);
        tb_AccOffsetY.Text = setting.AccOffsetY.ToString("0.###", CultureInfo.InvariantCulture);
        tb_AccOffsetZ.Text = setting.AccOffsetZ.ToString("0.###", CultureInfo.InvariantCulture);
        ck_CompressSaveFile.Checked = setting.CompressSaveFile;
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
        setting.CompressSaveFile = ck_CompressSaveFile.Checked;
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
            MessageBox.Show(this, $"フォルダを開けませんでした。\n{FileErrorText.Of(ex)}", "Setting",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    #region Display Engine(グラフ画面の zip)

    private void ShowWebContent()
    {
        var m = webContent.Manifest;
        lb_WebContent.Text = m is null ? "(none)" : m.DisplayName;
        bt_ChooseZip.Enabled = canSwitchEngine;
        bt_UseBuiltIn.Enabled = canSwitchEngine && webContent.Source == WebContentStore.ContentSource.Custom;
    }

    /// <summary>zip を選んだらその場で取り込む(Apply を待たない。Mac・Android と同じ)。失敗したときだけ理由を出す。</summary>
    private void bt_ChooseZip_Click(object sender, EventArgs e)
    {
        if (!canSwitchEngine)
        {
            return;
        }

        using var dialog = new OpenFileDialog { Filter = "zip (*.zip)|*.zip" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            webContent.ImportZip(dialog.FileName);
            lb_WebContentError.Visible = false;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            lb_WebContentError.Text = ex.Message;
            lb_WebContentError.Visible = true;
        }
        finally
        {
            Cursor = Cursors.Default;
        }

        ShowWebContent();
    }

    private void bt_UseBuiltIn_Click(object sender, EventArgs e)
    {
        if (!canSwitchEngine)
        {
            return;
        }

        webContent.UseBundled();
        lb_WebContentError.Visible = false;
        ShowWebContent();
    }

    #endregion
}
