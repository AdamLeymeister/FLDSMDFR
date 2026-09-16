using System.Drawing.Drawing2D;
using Themes;

namespace AnalysisReviewControl;

internal sealed class ModernFieldHost : Panel
{
    private readonly bool _searchIcon;
    private bool _hot;
    private bool _focused;

    public ModernFieldHost(Control editor, int width, bool searchIcon = false)
    {
        _searchIcon = searchIcon;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);

        Width = width;
        Height = 36;
        Padding = searchIcon
            ? new Padding(32, 6, 10, 6)
            : new Padding(8, 6, 8, 6);
        BackColor = Color.Transparent;
        Margin = Padding.Empty;

        editor.Dock = DockStyle.Fill;
        editor.Margin = Padding.Empty;
        Controls.Add(editor);

        MouseEnter += (_, _) => SetHot(true);
        MouseLeave += (_, _) => SetHot(ClientRectangle.Contains(PointToClient(Cursor.Position)));
        editor.MouseEnter += (_, _) => SetHot(true);
        editor.MouseLeave += (_, _) => SetHot(false);
        editor.GotFocus += (_, _) => SetFocused(true);
        editor.LostFocus += (_, _) => QueueFocusCheck();
        editor.Enter += (_, _) => SetFocused(true);
        editor.Leave += (_, _) => QueueFocusCheck();
    }

    private void QueueFocusCheck()
    {
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }

        BeginInvoke(() =>
        {
            if (!IsDisposed)
            {
                SetFocused(ContainsFocus);
            }
        });
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;

        Color border = _focused
            ? DarkMode.Primary
            : _hot
                ? DarkMode.Border
                : Color.FromArgb(
                    (DarkMode.Border.R + DarkMode.Surface.R) / 2,
                    (DarkMode.Border.G + DarkMode.Surface.G) / 2,
                    (DarkMode.Border.B + DarkMode.Surface.B) / 2);

        using GraphicsPath path = CreateRoundPath(bounds, 10);
        using var fill = new SolidBrush(DarkMode.ElevatedSurface);
        using var pen = new Pen(border);

        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);

        if (_searchIcon)
        {
            DrawSearchIcon(e.Graphics);
        }
    }

    private void DrawSearchIcon(Graphics graphics)
    {
        using var pen = new Pen(DarkMode.TextDisabled, 1.6f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        int x = 12;
        int y = (Height - 14) / 2;
        graphics.DrawEllipse(pen, x, y, 10, 10);
        graphics.DrawLine(pen, x + 9, y + 9, x + 14, y + 14);
    }

    private void SetHot(bool value)
    {
        if (_hot == value)
        {
            return;
        }

        _hot = value;
        Invalidate();
    }

    private void SetFocused(bool value)
    {
        if (_focused == value)
        {
            return;
        }

        _focused = value;
        Invalidate();
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
