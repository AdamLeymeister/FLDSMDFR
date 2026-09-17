using System.Drawing.Drawing2D;
using Themes;

namespace AnalysisReviewControl;

internal sealed class ThemedScrollBar : Control
{
    private const int MinThumb = 32;
    private const int ThumbInset = 2;

    private int _maximum;
    private int _value;
    private int _viewport = 1;
    private bool _hover;
    private bool _dragging;
    private int _dragOffset;

    public event EventHandler? ValueChanged;

    public ThemedScrollBar()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        Width = 14;
        TabStop = false;
        BackColor = DarkMode.Surface;
        Cursor = Cursors.Default;
    }

    public bool IsDragging => _dragging;

    public int Maximum => _maximum;

    public int Viewport => _viewport;

    public int Value => _value;

    public void SetRange(int value, int maximum, int viewport)
    {
        _maximum = Math.Max(0, maximum);
        _viewport = Math.Max(1, viewport);
        _value = Math.Clamp(value, 0, _maximum);
        Invalidate();
    }

    public void ScrollBy(int deltaRows)
    {
        SetValue(_value + deltaRows, raise: true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (!_dragging)
        {
            _hover = false;
            Invalidate();
        }

        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _maximum <= 0)
        {
            return;
        }

        Rectangle thumb = GetThumbBounds();
        if (e.Y >= thumb.Top && e.Y <= thumb.Bottom)
        {
            _dragging = true;
            _dragOffset = e.Y - thumb.Y;
            Capture = true;
            Invalidate();
            return;
        }

        int page = Math.Max(1, _viewport - 1);
        SetValue(e.Y < thumb.Y ? _value - page : _value + page, raise: true);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_dragging || _maximum <= 0)
        {
            return;
        }

        Rectangle track = GetTrackBounds();
        int thumbHeight = GetThumbBounds().Height;
        int travel = Math.Max(1, track.Height - thumbHeight);
        int y = Math.Clamp(e.Y - _dragOffset, track.Y, track.Y + travel);
        int next = (int)Math.Round((y - track.Y) * (double)_maximum / travel);
        SetValue(next, raise: true);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _dragging)
        {
            _dragging = false;
            Capture = false;
            _hover = ClientRectangle.Contains(PointToClient(MousePosition));
            Invalidate();
        }

        base.OnMouseUp(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (_maximum > 0)
        {
            int lines = SystemInformation.MouseWheelScrollLines;
            if (lines <= 0)
            {
                lines = 3;
            }

            SetValue(_value - Math.Sign(e.Delta) * lines, raise: true);
        }

        base.OnMouseWheel(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var back = new SolidBrush(DarkMode.Surface))
        {
            e.Graphics.FillRectangle(back, ClientRectangle);
        }

        if (_maximum <= 0)
        {
            return;
        }

        Rectangle thumb = GetThumbBounds();
        Color fill = _dragging
            ? Blend(DarkMode.Border, DarkMode.TextSecondary, 0.45f)
            : _hover
                ? Blend(DarkMode.Border, DarkMode.TextSecondary, 0.2f)
                : DarkMode.Border;

        using GraphicsPath path = CreateRoundPath(thumb, Math.Min(4, thumb.Width / 2));
        using var brush = new SolidBrush(fill);
        e.Graphics.FillPath(brush, path);
    }

    private void SetValue(int value, bool raise)
    {
        int next = Math.Clamp(value, 0, _maximum);
        if (next == _value)
        {
            return;
        }

        _value = next;
        Invalidate();
        if (raise)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private Rectangle GetTrackBounds()
    {
        return new Rectangle(
            ThumbInset,
            ThumbInset,
            Math.Max(1, Width - ThumbInset * 2),
            Math.Max(1, Height - ThumbInset * 2));
    }

    private Rectangle GetThumbBounds()
    {
        Rectangle track = GetTrackBounds();
        int total = _maximum + _viewport;
        int thumbHeight = Math.Clamp(
            (int)Math.Round(track.Height * (_viewport / (double)Math.Max(1, total))),
            MinThumb,
            track.Height);
        int travel = Math.Max(0, track.Height - thumbHeight);
        int y = track.Y + (_maximum == 0
            ? 0
            : (int)Math.Round(travel * (_value / (double)_maximum)));
        return new Rectangle(track.X, y, track.Width, thumbHeight);
    }

    private static GraphicsPath CreateRoundPath(Rectangle bounds, int radius)
    {
        int d = Math.Max(2, radius * 2);
        if (d > bounds.Width)
        {
            d = Math.Max(2, bounds.Width);
        }

        if (d > bounds.Height)
        {
            d = Math.Max(2, bounds.Height);
        }

        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color Blend(Color from, Color to, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            (int)(from.R + (to.R - from.R) * amount),
            (int)(from.G + (to.G - from.G) * amount),
            (int)(from.B + (to.B - from.B) * amount));
    }
}

internal sealed class ThemedScrollHost : Panel
{
    private readonly ReviewGrid _grid;
    private readonly ThemedScrollBar _vbar = new();
    private bool _syncing;

    public ThemedScrollHost(ReviewGrid grid)
    {
        _grid = grid;
        DoubleBuffered = true;
        BackColor = DarkMode.Surface;

        _grid.ScrollBars = ScrollBars.None;
        _grid.Dock = DockStyle.Fill;
        _vbar.Dock = DockStyle.Right;
        _vbar.Width = 14;

        Controls.Add(_grid);
        Controls.Add(_vbar);
        _vbar.BringToFront();

        _grid.Scroll += (_, _) => SyncFromGrid();
        _grid.Resize += (_, _) => SyncFromGrid();
        _grid.RowHeightChanged += (_, _) => SyncFromGrid();
        _grid.MouseWheel += (_, _) => SyncFromGrid();
        _vbar.ValueChanged += Vbar_ValueChanged;
        _vbar.MouseUp += (_, _) => SyncFromGrid();
    }

    public void SyncFromGrid()
    {
        if (_syncing || _vbar.IsDragging || !IsHandleCreated)
        {
            return;
        }

        int viewport = _grid.GetVisibleRowCount();
        int maximum = _grid.GetScrollMaximum();
        int value = Math.Clamp(_grid.GetFirstVisibleRow(), 0, maximum);

        _syncing = true;
        _vbar.SetRange(value, maximum, viewport);
        _syncing = false;
    }

    private void Vbar_ValueChanged(object? sender, EventArgs e)
    {
        if (_syncing || !_grid.IsHandleCreated)
        {
            return;
        }

        _syncing = true;
        try
        {
            _grid.SetFirstVisibleRow(_vbar.Value);
        }
        finally
        {
            _syncing = false;
        }
    }
}
