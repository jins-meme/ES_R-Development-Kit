using System.Drawing.Drawing2D;

namespace MEME_Academic_Sample.UI;

/// <summary>
/// Mac 版に寄せた見た目の共通定数。角丸と枠線の指定をここへ集める。
/// </summary>
internal static class UiTheme
{
    /// <summary>角丸の半径。Mac 版 ContentView の RoundedRectangle(cornerRadius: 6) に合わせる。</summary>
    public const float CornerRadius = 6f;

    /// <summary>枠線のグレー。Mac 版の Color.secondary.opacity(0.3) 相当。</summary>
    public static readonly Color Border = Color.FromArgb(198, 200, 205);

    /// <summary>角丸の矩形パス。半径が辺より大きい場合は辺に収まるところまで丸める。</summary>
    public static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2f, Math.Min(rect.Width, rect.Height));
        if (diameter <= 0f)
        {
            path.AddRectangle(rect);
            return path;
        }

        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180f, 90f);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270f, 90f);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0f, 90f);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90f, 90f);
        path.CloseFigure();
        return path;
    }
}
