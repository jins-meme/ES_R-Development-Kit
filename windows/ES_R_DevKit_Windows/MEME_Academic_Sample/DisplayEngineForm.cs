using MEME_Academic_Sample.Services;
using MEME_Academic_Sample.UI;

namespace MEME_Academic_Sample;

/// <summary>
/// Display Engine ダイアログ。Mac 版 DisplayEngineView に対応する。グラフ画面(WebView)の中身 zip の一覧を出し、
/// 使う 1 つを選ぶ・取り込む・消す。置き場と検査は <see cref="WebContentStore"/>。同梱の標準版(Standard)は規定で、消せない。
/// 使う中身が変わったら <see cref="EngineChanged"/> が true になる(閉じた後に呼んだ側がグラフ画面を読み込み直す)。
/// </summary>
public sealed class DisplayEngineForm : Form
{
    private const int ListWidth = 512;
    private const int RowHeight = 52;

    private readonly WebContentStore store;

    /// <summary>計測中・再生中は切り替えさせない(グラフ画面を読み込み直すと表示中のものが消えるため。Mac・Android と同じ)</summary>
    private readonly bool busy;

    private readonly Font small = new("Segoe UI", 8F);
    private readonly Panel list;
    private readonly Label message;
    private readonly RoundedButton add;

    private readonly Label heading;
    private readonly Label detail;
    private readonly Panel separator;
    private readonly RoundedButton done;

    /// <summary>今出している知らせ(作り直しても残す)</summary>
    private (string? Text, bool IsError) note;

    public DisplayEngineForm(WebContentStore store, bool busy)
    {
        this.store = store;
        this.busy = busy;
        Icon = AppInfo.LoadIcon();
        Text = "Display Engine";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);

        heading = new Label { Text = "Display Engine", AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
        detail = new Label
        {
            Text = "The zip that draws the graph. You can keep several, but only one is active.",
            ForeColor = SystemColors.GrayText,
        };
        list = new Panel { BackColor = SystemColors.Window };
        list.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, list.Width - 1, list.Height - 1);
        };
        message = new Label { Visible = false, UseMnemonic = false };
        separator = new Panel { BackColor = SystemColors.ControlDark };
        add = new RoundedButton { Text = "Add zip…", Enabled = !busy };
        add.Click += (_, _) => AddZip();
        done = new RoundedButton { Text = "Done", DialogResult = DialogResult.OK };

        Controls.AddRange([heading, detail, list, message, separator, add, done]);
        AcceptButton = done;
        CancelButton = done;
        Rebuild();
    }

    /// <summary>使う中身が変わったか(取り込みで使っているものが置き換わった・有効化・使っているものを消した)</summary>
    public bool EngineChanged { get; private set; }

    /// <summary>
    /// 96 dpi での長さを今の画面の長さへ。アプリは PerMonitorV2 で、文字は画面の dpi で大きくなるので、
    /// 置き場所と大きさもこれで合わせる(デザイナーのフォームは AutoScaleMode で合わせている)。
    /// </summary>
    private int S(int value) => LogicalToDeviceUnits(value);

    /// <summary>別の dpi の画面へ移ったら、置き場所を測り直す</summary>
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        Rebuild(note.Text, note.IsError);
    }

    /// <summary>一覧を作り直し、全体の置き場所を合わせる(text は一覧の下に出す知らせ)</summary>
    private void Rebuild(string? text = null, bool isError = false)
    {
        note = (text, isError);
        SuspendLayout();
        var width = S(ListWidth);
        heading.Location = new Point(S(20), S(16));
        detail.Location = new Point(S(20), S(46));
        detail.Size = new Size(width, S(20));

        list.SuspendLayout();
        foreach (Control c in list.Controls.Cast<Control>().ToArray())
        {
            c.Dispose();
        }

        var entries = store.Entries;
        for (var k = 0; k < entries.Count; k++)
        {
            AddRow(entries[k], k);
        }

        list.Location = new Point(S(20), S(74));
        list.Size = new Size(width, S(Math.Max(1, entries.Count) * RowHeight));
        list.ResumeLayout();

        var y = list.Bottom + S(10);
        var shown = text ?? (busy ? "Stop the measurement or replay to change the display engine." : null);
        message.Visible = shown is not null;
        message.Text = shown ?? "";
        message.ForeColor = isError ? Color.Firebrick : SystemColors.GrayText;
        message.Location = new Point(S(20), y);
        message.Size = new Size(width, S(36));
        if (shown is not null)
        {
            y += message.Height + S(4);
        }

        separator.Location = new Point(S(20), y + S(6));
        separator.Size = new Size(width, Math.Max(1, S(1)));
        add.Location = new Point(S(20), y + S(18));
        add.Size = new Size(S(100), S(28));
        done.Size = new Size(S(80), S(28));
        done.Location = new Point(S(20) + width - done.Width, y + S(18));
        ClientSize = new Size(width + S(40), y + S(18 + 28 + 16));
        ResumeLayout();
    }

    private void AddRow(WebContentEntry entry, int index)
    {
        var active = entry.Id == store.ActiveId;
        var top = S(index * RowHeight);
        var width = S(ListWidth);
        if (index > 0)
        {
            list.Controls.Add(new Panel
            {
                BackColor = UiTheme.Border,
                Location = new Point(1, top),
                Size = new Size(width - 2, Math.Max(1, S(1))),
            });
        }

        var radio = new RadioButton
        {
            AutoCheck = false,   // 押されたら店で切り替えてから一覧を作り直す
            Checked = active,
            Enabled = !busy,
            Location = new Point(S(12), top + S(17)),
            Size = new Size(S(18), S(18)),
            AccessibleName = entry.Manifest.DisplayName,
        };
        radio.Click += (_, _) => Activate(entry);
        // 右側(Active・Delete)の分を空けて、名前は省略記号で切る
        var textWidth = width - S(36) - S(entry.IsBuiltIn ? 80 : 160);
        var title = new Label
        {
            Text = entry.Manifest.DisplayName,
            Location = new Point(S(36), top + S(8)),
            Size = new Size(textWidth, S(18)),
            AutoEllipsis = true,
            UseMnemonic = false,
        };
        var sub = new Label
        {
            Text = entry.IsBuiltIn ? "Built-in (default)" : entry.Manifest.Name,
            Location = new Point(S(36), top + S(27)),
            Size = new Size(textWidth, S(16)),
            AutoEllipsis = true,
            UseMnemonic = false,
            ForeColor = SystemColors.GrayText,
            Font = small,
        };
        title.Click += (_, _) => Activate(entry);
        sub.Click += (_, _) => Activate(entry);
        list.Controls.AddRange([radio, title, sub]);

        if (active)
        {
            list.Controls.Add(new Label
            {
                Text = "Active",
                Location = new Point(width - S(entry.IsBuiltIn ? 60 : 140), top + S(18)),
                Size = new Size(S(50), S(16)),
                ForeColor = SystemColors.GrayText,
                Font = small,
            });
        }

        if (!entry.IsBuiltIn)
        {
            var delete = new RoundedButton
            {
                Text = "Delete",
                Location = new Point(width - S(82), top + S(12)),
                Size = new Size(S(70), S(28)),
                Enabled = !busy,
                AccessibleName = $"Delete {entry.Manifest.DisplayName}",
            };
            delete.Click += (_, _) => Remove(entry);
            list.Controls.Add(delete);
        }
    }

    private void Activate(WebContentEntry entry)
    {
        if (busy || entry.Id == store.ActiveId)
        {
            return;
        }

        store.Activate(entry.Id);
        EngineChanged = true;
        RebuildLater();
    }

    /// <summary>
    /// zip を選んで取り込み、一覧に足す(有効にはしない)。検査に通らなければ何も変えず、理由を出す。
    /// 同じファイルなら何もしない。同じ name のものを使っていて置き換えたときは、グラフ画面を読み込み直す。
    /// </summary>
    private void AddZip()
    {
        if (busy)
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
            var (entry, outcome, replacedActive) = store.Add(dialog.FileName);
            EngineChanged |= replacedActive;
            Rebuild(outcome switch
            {
                WebContentStore.AddOutcome.AlreadyAdded => $"{entry.Manifest.DisplayName} is already in the list.",
                WebContentStore.AddOutcome.Replaced =>
                    $"Replaced the zip named \"{entry.Manifest.Name}\" with {entry.Manifest.DisplayName}.",
                _ => null,
            });
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Rebuild(ex.Message, isError: true);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void Remove(WebContentEntry entry)
    {
        if (busy)
        {
            return;
        }

        var answer = MessageBox.Show(this,
            $"Delete {entry.Manifest.DisplayName}?\n\nThe zip is removed from this app. To use it again, add the zip file again.",
            "Display Engine", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.OK)
        {
            return;
        }

        try
        {
            EngineChanged |= store.Remove(entry.Id);
            RebuildLater();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            RebuildLater(ex.Message, isError: true);
        }
    }

    /// <summary>一覧の行(押されたボタン自身)のイベントの中では作り直さず、イベントを抜けてから作り直す(行は Dispose するため)</summary>
    private void RebuildLater(string? text = null, bool isError = false) => BeginInvoke(() => Rebuild(text, isError));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            small.Dispose();
        }

        base.Dispose(disposing);
    }
}
