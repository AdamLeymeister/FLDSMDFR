using System.Drawing.Drawing2D;
using Themes;

namespace AnalysisReviewControl;

internal sealed class ThemedComboBox : ComboBox
{
    private const int WM_PAINT = 0x000F;

    public ThemedComboBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        DropDownStyle = ComboBoxStyle.DropDownList;
        FlatStyle = FlatStyle.Flat;
        IntegralHeight = false;
        ItemHeight = 28;
        BackColor = DarkMode.ElevatedSurface;
        ForeColor = DarkMode.TextPrimary;
        Margin = Padding.Empty;
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Graphics == null)
        {
            return;
        }

        bool highlighted = (e.State & DrawItemState.Selected) != 0 &&
                           (e.State & DrawItemState.ComboBoxEdit) == 0;

        using var fill = new SolidBrush(highlighted ? DarkMode.HoverSurface : DarkMode.ElevatedSurface);
        e.Graphics.FillRectangle(fill, e.Bounds);

        string text = Items[e.Index]?.ToString() ?? string.Empty;
        TextRenderer.DrawText(
            e.Graphics,
            text,
            Font,
            Rectangle.Inflate(e.Bounds, -8, 0),
            DarkMode.TextPrimary,
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.Left |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg != WM_PAINT)
        {
            return;
        }

        using Graphics graphics = Graphics.FromHwnd(Handle);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using (var fill = new SolidBrush(BackColor))
        {
            graphics.FillRectangle(fill, ClientRectangle);
        }

        string text = SelectedItem?.ToString() ?? string.Empty;
        TextRenderer.DrawText(
            graphics,
            text,
            Font,
            new Rectangle(8, 0, Math.Max(0, Width - 28), Height),
            ForeColor,
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.Left |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        using var pen = new Pen(DarkMode.TextDisabled, 1.6f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        int cx = Width - 14;
        int cy = Height / 2;
        graphics.DrawLines(
            pen,
            new[]
            {
                new Point(cx - 4, cy - 2),
                new Point(cx, cy + 2),
                new Point(cx + 4, cy - 2)
            });
    }
}
