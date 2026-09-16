using System.Drawing.Drawing2D;

namespace AnalysisReviewControl;

public sealed class RoundedCardPanel : Panel
{
    public int CornerRadius { get; set; } = 18;
    public Color FillColor { get; set; } = Themes.DarkMode.Surface;

    public RoundedCardPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;

        using GraphicsPath path = CreateRoundPath(bounds, CornerRadius);
        using var fill = new SolidBrush(FillColor);
        using var border = new Pen(Themes.DarkMode.Border, 1);

        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    private static GraphicsPath CreateRoundPath(Rectangle bounds, int radius)
    {
        int diameter = Math.Max(radius, 1) * 2;
        var path = new GraphicsPath();

        if (bounds.Width <= diameter || bounds.Height <= diameter)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
