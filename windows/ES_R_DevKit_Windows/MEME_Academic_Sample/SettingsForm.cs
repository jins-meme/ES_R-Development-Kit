using System.Diagnostics;
using System.Globalization;
using MEME_Academic_Sample.Services;
using MEME_Academic_Sample.UI;
using MEME_Academic_Sample.Utility;

namespace MEME_Academic_Sample;

/// <summary>Setting ダイアログ。Mac 版 SettingsView に対応する。</summary>
public partial class SettingsForm : Form
{
    private readonly UserSetting setting;

    /// <summary>Graph Cache Folder の行(日別キャッシュ logic4-cache の置き場所。Mac の SettingsView と同じ)。デザイナーではなくコードで足す</summary>
    private readonly TextBox tb_CacheFolder = new() { Name = "tb_CacheFolder", Location = new Point(165, 274), Size = new Size(380, 23), TabIndex = 11,
        PlaceholderText = "(empty = Save File Path)" };

    /// <summary>グラフ画面の中身(zip)の切り替えは、メニューの Display Engine(DisplayEngineForm)から。</summary>
    public SettingsForm(UserSetting setting)
    {
        this.setting = setting;
        InitializeComponent();
        AddCacheFolderRow();
        Icon = AppInfo.LoadIcon();
        LoadSettings();
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
        tb_CacheFolder.Text = setting.CacheFolder;
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
        setting.CacheFolder = tb_CacheFolder.Text.Trim();
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

    /// <summary>
    /// Local Port の下に「Graph Cache Folder」の行を足す(区切り線・Cancel・Apply とダイアログの高さを 1 行ぶん下げる)。
    /// グラフ画面(advanced2)が日別のキャッシュを置くフォルダ。空なら Save File Path の下の logic4-cache。名前が logic4-cache のフォルダを
    /// 選べばその中へ直に置く(Syncthing などで同期しているフォルダを選べる)。webview/BRIDGE.md の Cache files
    /// </summary>
    private void AddCacheFolderRow()
    {
        const int Row = 40;
        SuspendLayout();
        foreach (var c in new Control[] { separator, bt_Cancel, bt_Apply })
        {
            c.Top += Row;
        }

        ClientSize = new Size(ClientSize.Width, ClientSize.Height + Row);
        var label = new Label { Name = "lb_CacheFolder", Location = new Point(20, 278), Size = new Size(140, 20), Text = "Graph Cache Folder" };
        var select = new RoundedButton { Name = "bt_SelectCacheFolder", Location = new Point(553, 273), Size = new Size(70, 26), TabIndex = 12,
            Text = "Select", UseVisualStyleBackColor = true };
        var open = new RoundedButton { Name = "bt_OpenCacheFolder", Location = new Point(629, 273), Size = new Size(100, 26), TabIndex = 13,
            Text = "Open Folder", UseVisualStyleBackColor = true };
        select.Click += bt_SelectCacheFolder_Click;
        open.Click += bt_OpenCacheFolder_Click;
        Controls.AddRange([label, tb_CacheFolder, select, open]);
        bt_Apply.TabIndex = 14;
        bt_Cancel.TabIndex = 15;
        ResumeLayout(false);
    }

    private void bt_SelectCacheFolder_Click(object? sender, EventArgs e)
    {
        var current = tb_CacheFolder.Text.Trim();
        using var dialog = new FolderBrowserDialog
        {
            Description = "グラフの日別キャッシュ(logic4-cache)の置き場所",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(current) ? current
                : Directory.Exists(tb_SaveFilePath.Text) ? tb_SaveFilePath.Text : UserSetting.DefaultSaveDirectory(),
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            tb_CacheFolder.Text = dialog.SelectedPath;
        }
    }

    /// <summary>実際に使う logic4-cache(空なら Save File Path の下)を開く。無ければ作る</summary>
    private void bt_OpenCacheFolder_Click(object? sender, EventArgs e)
    {
        var root = CacheStore.RootFrom(new UserSetting { SaveFilePath = tb_SaveFilePath.Text.Trim(), CacheFolder = tb_CacheFolder.Text.Trim() });
        if (root is null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(root);
            Process.Start(new ProcessStartInfo(root) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show(this, $"フォルダを開けませんでした。\n{FileErrorText.Of(ex)}", "Setting",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
}
