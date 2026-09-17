using System.Drawing.Drawing2D;
using Themes;

namespace AnalysisReviewControl;

internal sealed class SourcePreviewPanel : Control
{
    private const int ContextRadius = 3;
    private readonly Font _captionFont = new("Segoe UI", 7.5f, FontStyle.Bold);
    private readonly Font _titleFont = new("Segoe UI", 11f, FontStyle.Bold);
    private readonly Font _metaFont = new("Segoe UI", 9f);
    private readonly Font _codeFont = new("Consolas", 10f);
    private readonly Font _codeBold = new("Consolas", 10f, FontStyle.Bold);

    private string _fileName = string.Empty;
    private string _reason = string.Empty;
    private string _emptyMessage = "Select a hit to preview the source line.";
    private int _lineNumber;
    private string _highlight = string.Empty;
    private string[] _lines = Array.Empty<string>();

    public SourcePreviewPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        BackColor = DarkMode.Surface;
        Dock = DockStyle.Fill;
        Padding = new Padding(18);
    }

    public void ShowEmpty(string message)
    {
        _fileName = string.Empty;
        _reason = string.Empty;
        _emptyMessage = message;
        _lineNumber = 0;
        _highlight = string.Empty;
        _lines = Array.Empty<string>();
        Invalidate();
    }

    public void ShowHit(
        string fileName,
        int lineNumber,
        string reason,
        string highlight,
        string[] lines)
    {
        _fileName = fileName;
        _lineNumber = lineNumber;
        _reason = reason;
        _highlight = highlight;
        _lines = lines ?? Array.Empty<string>();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(DarkMode.Surface);

        Rectangle bounds = Rectangle.Inflate(ClientRectangle, -18, -18);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        TextRenderer.DrawText(
            e.Graphics,
            "SOURCE",
            _captionFont,
            bounds,
            DarkMode.TextDisabled,
            TextFormatFlags.Top | TextFormatFlags.Left | TextFormatFlags.NoPadding);

        if (string.IsNullOrWhiteSpace(_fileName) && _lines.Length == 0)
        {
            var empty = new Rectangle(bounds.X, bounds.Y + 36, bounds.Width, bounds.Height - 36);
            TextRenderer.DrawText(
                e.Graphics,
                _emptyMessage,
                _metaFont,
                empty,
                DarkMode.TextDisabled,
                TextFormatFlags.WordBreak | TextFormatFlags.Top | TextFormatFlags.Left);
            return;
        }

        var titleBounds = new Rectangle(bounds.X, bounds.Y + 18, bounds.Width, 28);
        TextRenderer.DrawText(
            e.Graphics,
            string.IsNullOrWhiteSpace(_fileName) ? "Unknown file" : _fileName,
            _titleFont,
            titleBounds,
            DarkMode.TextPrimary,
            TextFormatFlags.EndEllipsis | TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

        string meta = _lineNumber > 0 ? $"Line {_lineNumber}" : string.Empty;
        if (!string.IsNullOrWhiteSpace(_reason))
        {
            meta = string.IsNullOrEmpty(meta) ? _reason : $"{meta}  ·  {_reason}";
        }

        var metaBounds = new Rectangle(bounds.X, bounds.Y + 46, bounds.Width, 22);
        TextRenderer.DrawText(
            e.Graphics,
            meta,
            _metaFont,
            metaBounds,
            DarkMode.Secondary,
            TextFormatFlags.EndEllipsis | TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

        int top = bounds.Y + 78;
        if (_lines.Length == 0)
        {
            TextRenderer.DrawText(
                e.Graphics,
                "Source file was not found on disk.",
                _metaFont,
                new Rectangle(bounds.X, top, bounds.Width, 40),
                DarkMode.TextDisabled,
                TextFormatFlags.WordBreak | TextFormatFlags.Left);
            return;
        }

        int start = Math.Max(1, _lineNumber - ContextRadius);
        int end = Math.Min(_lines.Length, Math.Max(_lineNumber, 1) + ContextRadius);
        int lineHeight = 26;
        int gutter = 48;

        for (int line = start; line <= end; line++)
        {
            int y = top + (line - start) * lineHeight;
            if (y + lineHeight > bounds.Bottom)
            {
                break;
            }

            bool current = line == _lineNumber;
            var row = new Rectangle(bounds.X, y, bounds.Width, lineHeight);
            if (current)
            {
                using var fill = new SolidBrush(Color.FromArgb(40, DarkMode.Primary));
                e.Graphics.FillRectangle(fill, row);
            }

            TextRenderer.DrawText(
                e.Graphics,
                line.ToString(),
                _codeFont,
                new Rectangle(bounds.X, y, gutter - 8, lineHeight),
                current ? DarkMode.Primary : DarkMode.TextDisabled,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            string text = _lines[line - 1].TrimEnd('\r', '\n');
            var textBounds = new Rectangle(
                bounds.X + gutter,
                y,
                Math.Max(0, bounds.Width - gutter),
                lineHeight);

            if (current && !string.IsNullOrWhiteSpace(_highlight))
            {
                PaintHighlightedLine(
                    e.Graphics,
                    textBounds,
                    text,
                    _highlight,
                    _codeBold,
                    DarkMode.TextPrimary);
            }
            else
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    _codeFont,
                    textBounds,
                    DarkMode.TextSecondary,
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding |
                    TextFormatFlags.NoPrefix);
            }
        }
    }

    internal static void PaintHighlightedLine(
        Graphics graphics,
        Rectangle bounds,
        string text,
        string highlight,
        Font font,
        Color color)
    {
        (int start, int length) = FindHighlight(text, highlight);
        TextFormatFlags flags =
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding |
            TextFormatFlags.NoPrefix |
            TextFormatFlags.EndEllipsis;

        if (start < 0 || length <= 0)
        {
            TextRenderer.DrawText(graphics, text, font, bounds, color, flags);
            return;
        }

        string prefix = text[..start];
        string hit = text.Substring(start, length);
        Size prefixSize = TextRenderer.MeasureText(
            graphics,
            string.IsNullOrEmpty(prefix) ? " " : prefix,
            font,
            bounds.Size,
            flags & ~TextFormatFlags.EndEllipsis);
        Size hitSize = TextRenderer.MeasureText(
            graphics,
            hit,
            font,
            bounds.Size,
            flags & ~TextFormatFlags.EndEllipsis);

        int prefixWidth = string.IsNullOrEmpty(prefix) ? 0 : prefixSize.Width;
        var hitBounds = new Rectangle(
            bounds.X + prefixWidth,
            bounds.Y + 4,
            Math.Max(hitSize.Width, 8),
            bounds.Height - 8);

        using var fill = new SolidBrush(Color.FromArgb(70, DarkMode.Primary));
        graphics.FillRectangle(fill, hitBounds);
        TextRenderer.DrawText(graphics, text, font, bounds, color, flags);
    }

    internal static (int Start, int Length) FindHighlight(string text, string highlight)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(highlight))
        {
            return (-1, 0);
        }

        int at = text.IndexOf(highlight, StringComparison.OrdinalIgnoreCase);
        return at < 0 ? (-1, 0) : (at, highlight.Length);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _captionFont.Dispose();
            _titleFont.Dispose();
            _metaFont.Dispose();
            _codeFont.Dispose();
            _codeBold.Dispose();
        }

        base.Dispose(disposing);
    }
}
