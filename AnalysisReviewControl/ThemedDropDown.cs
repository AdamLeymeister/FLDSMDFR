using System.Collections;
using System.Drawing.Drawing2D;
using Themes;

namespace AnalysisReviewControl;

internal sealed class ThemedDropDown : Control
{
    private const int ItemHeight = 30;
    private const int MaxVisibleItems = 10;

    private readonly DropDownItems _items;
    private int _selectedIndex = -1;
    private int _updateLevel;
    private bool _hot;
    private PopupList? _popup;

    public ThemedDropDown()
    {
        _items = new DropDownItems(this);

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.Selectable |
            ControlStyles.SupportsTransparentBackColor,
            true);

        Height = 36;
        TabStop = true;
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
        Margin = Padding.Empty;
    }

    public event EventHandler? SelectedIndexChanged;

    public DropDownItems Items => _items;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetSelectedIndex(value, raise: true);
    }

    public object? SelectedItem
    {
        get => _selectedIndex >= 0 && _selectedIndex < _items.Count
            ? _items[_selectedIndex]
            : null;
        set
        {
            if (value == null)
            {
                SelectedIndex = -1;
                return;
            }

            for (int i = 0; i < _items.Count; i++)
            {
                if (Equals(_items[i], value))
                {
                    SelectedIndex = i;
                    return;
                }
            }
        }
    }

    public void BeginUpdate()
    {
        _updateLevel++;
    }

    public void EndUpdate()
    {
        if (_updateLevel > 0)
        {
            _updateLevel--;
        }

        if (_updateLevel == 0)
        {
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;

        bool active = Focused || _popup is { Visible: true };
        Color border = active
            ? DarkMode.Primary
            : _hot
                ? DarkMode.Border
                : Color.FromArgb(
                    (DarkMode.Border.R + DarkMode.Surface.R) / 2,
                    (DarkMode.Border.G + DarkMode.Surface.G) / 2,
                    (DarkMode.Border.B + DarkMode.Surface.B) / 2);

        using GraphicsPath path = CreateRoundPath(bounds, 10);
        using var fill = new SolidBrush(Enabled ? DarkMode.ElevatedSurface : DarkMode.Surface);
        using var pen = new Pen(border);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);

        Color textColor = Enabled ? DarkMode.TextPrimary : DarkMode.TextDisabled;
        TextRenderer.DrawText(
            e.Graphics,
            SelectedItem?.ToString() ?? string.Empty,
            Font,
            new Rectangle(12, 0, Math.Max(0, Width - 34), Height),
            textColor,
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.Left |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        using var chevron = new Pen(DarkMode.TextDisabled, 1.6f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        int cx = Width - 16;
        int cy = Height / 2;
        e.Graphics.DrawLines(
            chevron,
            new[]
            {
                new Point(cx - 4, cy - 2),
                new Point(cx, cy + 2),
                new Point(cx + 4, cy - 2)
            });
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hot = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hot = false;
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (Enabled)
        {
            Focus();
            TogglePopup();
        }
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

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData is Keys.Up or Keys.Down or Keys.F4 or Keys.Space
            || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled)
        {
            base.OnKeyDown(e);
            return;
        }

        if (e.KeyCode is Keys.Space or Keys.F4 or Keys.Down && _popup is not { Visible: true })
        {
            TogglePopup();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Escape && _popup is { Visible: true })
        {
            ClosePopup();
            e.Handled = true;
            return;
        }

        if (e.KeyCode is Keys.Up or Keys.Down && _items.Count > 0)
        {
            int next = _selectedIndex < 0 ? 0 : _selectedIndex + (e.KeyCode == Keys.Down ? 1 : -1);
            SelectedIndex = Math.Clamp(next, 0, _items.Count - 1);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClosePopup();
            _popup?.Dispose();
            _popup = null;
        }

        base.Dispose(disposing);
    }

    internal void OnItemsChanged()
    {
        if (_selectedIndex >= _items.Count)
        {
            SetSelectedIndex(_items.Count - 1, raise: false);
        }

        if (_updateLevel == 0)
        {
            Invalidate();
        }
    }

    private void SetSelectedIndex(int value, bool raise)
    {
        int bounded = _items.Count == 0 ? -1 : Math.Clamp(value, -1, _items.Count - 1);
        if (_selectedIndex == bounded)
        {
            Invalidate();
            return;
        }

        _selectedIndex = bounded;
        Invalidate();

        if (raise && _updateLevel == 0)
        {
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void TogglePopup()
    {
        if (_popup is { Visible: true })
        {
            ClosePopup();
            return;
        }

        ShowPopup();
    }

    private void ShowPopup()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _popup ??= new PopupList();
        _popup.Bind(_items, _selectedIndex, Font, Width, OnPopupChosen);
        Point screen = PointToScreen(new Point(0, Height + 2));
        _popup.StartPosition = FormStartPosition.Manual;
        _popup.Location = screen;
        _popup.Deactivate += Popup_Deactivate;
        Form? owner = FindForm();
        if (owner == null)
        {
            _popup.Show();
        }
        else
        {
            _popup.Show(owner);
        }

        Invalidate();
    }

    private void Popup_Deactivate(object? sender, EventArgs e)
    {
        ClosePopup();
    }

    private void OnPopupChosen(int index)
    {
        SelectedIndex = index;
        ClosePopup();
        Focus();
    }

    private void ClosePopup()
    {
        if (_popup == null)
        {
            return;
        }

        _popup.Deactivate -= Popup_Deactivate;
        if (_popup.Visible)
        {
            _popup.Hide();
        }

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

    internal sealed class DropDownItems : IEnumerable
    {
        private readonly ThemedDropDown _owner;
        private readonly List<object> _items = new();

        public DropDownItems(ThemedDropDown owner)
        {
            _owner = owner;
        }

        public int Count => _items.Count;

        public object this[int index] => _items[index];

        public void Add(object item)
        {
            _items.Add(item);
            _owner.OnItemsChanged();
        }

        public void AddRange(object[] items)
        {
            _items.AddRange(items);
            _owner.OnItemsChanged();
        }

        public void Clear()
        {
            _items.Clear();
            _owner.OnItemsChanged();
        }

        public IEnumerator GetEnumerator() => _items.GetEnumerator();
    }

    private sealed class PopupList : Form
    {
        private readonly ListBox _list = new();
        private Action<int>? _chosen;

        public PopupList()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = DarkMode.Border;
            Padding = new Padding(1);
            MinimumSize = new Size(40, ItemHeight + 2);

            _list.Dock = DockStyle.Fill;
            _list.BorderStyle = BorderStyle.None;
            _list.BackColor = DarkMode.ElevatedSurface;
            _list.ForeColor = DarkMode.TextPrimary;
            _list.IntegralHeight = false;
            _list.DrawMode = DrawMode.OwnerDrawFixed;
            _list.ItemHeight = ItemHeight;
            _list.DrawItem += List_DrawItem;
            _list.Click += List_Click;
            _list.KeyDown += List_KeyDown;
            Controls.Add(_list);
        }

        public void Bind(DropDownItems items, int selectedIndex, Font font, int width, Action<int> chosen)
        {
            _chosen = chosen;
            _list.Font = font;
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (object item in items)
            {
                _list.Items.Add(item);
            }

            _list.EndUpdate();
            if (selectedIndex >= 0 && selectedIndex < _list.Items.Count)
            {
                _list.SelectedIndex = selectedIndex;
            }

            int visible = Math.Min(MaxVisibleItems, Math.Max(1, _list.Items.Count));
            Width = width;
            Height = visible * ItemHeight + 2;
        }

        protected override bool ShowWithoutActivation => false;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        private void List_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Graphics == null)
            {
                return;
            }

            bool selected = (e.State & DrawItemState.Selected) != 0;
            using var fill = new SolidBrush(selected ? DarkMode.HoverSurface : DarkMode.ElevatedSurface);
            e.Graphics.FillRectangle(fill, e.Bounds);

            TextRenderer.DrawText(
                e.Graphics,
                _list.Items[e.Index]?.ToString() ?? string.Empty,
                _list.Font,
                Rectangle.Inflate(e.Bounds, -10, 0),
                DarkMode.TextPrimary,
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.Left |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding);
        }

        private void List_Click(object? sender, EventArgs e)
        {
            if (_list.SelectedIndex >= 0)
            {
                _chosen?.Invoke(_list.SelectedIndex);
            }
        }

        private void List_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && _list.SelectedIndex >= 0)
            {
                _chosen?.Invoke(_list.SelectedIndex);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Hide();
                e.Handled = true;
            }
        }
    }
}
