using System.Drawing.Drawing2D;

namespace MEME_Academic_Sample.UI;

/// <summary>
/// 角丸のボタン。WinForms の標準ボタンは直角なので、Mac 版の見た目に合わせて自前で描く。
/// 標準ボタンの描画を止める(UserPaint)ため、背景・枠線・文字・フォーカス枠はすべてここで描く。
/// </summary>
public sealed class RoundedButton : Button
{
    private static readonly Color Face = Color.FromArgb(252, 252, 253);
    private static readonly Color FaceHover = Color.FromArgb(240, 242, 245);
    private static readonly Color FacePressed = Color.FromArgb(225, 228, 233);
    private static readonly Color FaceDisabled = Color.FromArgb(244, 244, 245);
    private static readonly Color BorderDisabled = Color.FromArgb(222, 224, 228);

    private bool _hovered;
    private bool _pressed;

    public RoundedButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;

        // 角丸の外側は親の色で塗りつぶす。ボタン自身の BackColor は使わない。
        g.Clear(Parent?.BackColor ?? SystemColors.Control);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (var path = UiTheme.RoundedRect(bounds, UiTheme.CornerRadius))
        {
            var face = !Enabled ? FaceDisabled
                : _pressed ? FacePressed
                : _hovered ? FaceHover
                : Face;

            using var brush = new SolidBrush(face);
            using var pen = new Pen(Enabled ? UiTheme.Border : BorderDisabled);
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        // 標準の点線フォーカス枠が出なくなるので、キーボード操作用に内側へ一本引く。
        if (Enabled && Focused && ShowFocusCues)
        {
            var inner = RectangleF.Inflate(bounds, -2f, -2f);
            using var path = UiTheme.RoundedRect(inner, UiTheme.CornerRadius - 2f);
            using var pen = new Pen(SystemColors.Highlight);
            g.DrawPath(pen, path);
        }

        g.SmoothingMode = SmoothingMode.Default;
        TextRenderer.DrawText(
            g, Text, Font, ClientRectangle,
            Enabled ? ForeColor : SystemColors.GrayText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
        _pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        if (mevent.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _pressed = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (!Enabled)
        {
            // 無効化されるとマウスが離れても OnMouseLeave が来ないことがある。
            _hovered = false;
            _pressed = false;
        }

        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }
}
