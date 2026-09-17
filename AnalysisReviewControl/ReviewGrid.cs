using System.Drawing.Drawing2D;
using Themes;

namespace AnalysisReviewControl;

internal enum ReviewRowBand
{
    Match,
    File,
    Sport
}

internal sealed class ReviewGrid : DataGridView
{
    private const string SelectColumnName = "Select";
    private const int CheckboxSize = 18;
    private const int PillHeight = 24;

    private int _selectionAnchor;
    private int _hoverRow = -1;
    private readonly Font _headerFont = new("Segoe UI", 9f, FontStyle.Bold);
    private readonly Font _pillFont = new("Segoe UI", 8.5f, FontStyle.Bold);

    public Func<int, ReviewRowBand>? ResolveRowBand { get; set; }

    public Func<int, bool>? ResolveExpanded { get; set; }

    public Func<int, IReadOnlyList<int>>? ResolveSelectableGroup { get; set; }

    public ReviewGrid()
    {
        DoubleBuffered = true;
        VirtualMode = true;
        ReadOnly = true;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        AllowUserToOrderColumns = false;
        AutoGenerateColumns = false;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        BackgroundColor = DarkMode.Surface;
        BorderStyle = BorderStyle.None;
        CellBorderStyle = DataGridViewCellBorderStyle.None;
        ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        ColumnHeadersHeight = 48;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        EditMode = DataGridViewEditMode.EditProgrammatically;
        EnableHeadersVisualStyles = false;
        GridColor = DarkMode.Surface;
        MultiSelect = true;
        RowHeadersVisible = false;
        RowTemplate.Height = 50;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        ShowCellToolTips = true;
        StandardTab = true;
        AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
        AdvancedColumnHeadersBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;

        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = DarkMode.Surface,
            ForeColor = DarkMode.TextDisabled,
            SelectionBackColor = DarkMode.Surface,
            SelectionForeColor = DarkMode.TextDisabled,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0),
            WrapMode = DataGridViewTriState.False
        };

        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = DarkMode.Surface,
            ForeColor = DarkMode.TextPrimary,
            SelectionBackColor = Blend(DarkMode.Surface, DarkMode.Primary, 0.12f),
            SelectionForeColor = DarkMode.TextPrimary,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0),
            WrapMode = DataGridViewTriState.False
        };

        AlternatingRowsDefaultCellStyle = DefaultCellStyle.Clone();
    }

    public void SetSelectionAnchor(int rowIndex)
    {
        if (rowIndex >= 0 && rowIndex < RowCount)
        {
            _selectionAnchor = rowIndex;
        }
    }

    public void SelectAllVisibleRows()
    {
        if (RowCount == 0)
        {
            return;
        }

        SelectAll();
        Invalidate();
    }

    protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex < 0 ||
            e.Button != MouseButtons.Left ||
            RowCount == 0)
        {
            base.OnCellMouseDown(e);
            return;
        }

        if (IsSelectColumn(e.ColumnIndex))
        {
            Focus();
            if (ResolveRowBand?.Invoke(e.RowIndex) is ReviewRowBand.Sport or ReviewRowBand.File)
            {
                SelectGroup(e.RowIndex, toggle: true);
                return;
            }

            var checkboxSelection = CaptureSelection();
            if (!checkboxSelection.Add(e.RowIndex))
            {
                checkboxSelection.Remove(e.RowIndex);
            }

            ApplySelection(checkboxSelection, e.RowIndex, SanitizeColumnIndex(e.ColumnIndex));
            return;
        }

        bool control = IsControlDown();
        bool shift = IsShiftDown();

        if (!control && !shift)
        {
            _selectionAnchor = e.RowIndex;
            base.OnCellMouseDown(e);
            Invalidate();
            return;
        }

        Focus();
        int columnIndex = SanitizeColumnIndex(e.ColumnIndex);

        if (shift)
        {
            var selected = control
                ? CaptureSelection()
                : new HashSet<int>();

            int start = Math.Min(_selectionAnchor, e.RowIndex);
            int end = Math.Max(_selectionAnchor, e.RowIndex);
            for (int i = start; i <= end; i++)
            {
                selected.Add(i);
            }

            ApplySelection(selected, e.RowIndex, columnIndex);
            return;
        }

        var toggled = CaptureSelection();
        if (!toggled.Add(e.RowIndex))
        {
            toggled.Remove(e.RowIndex);
        }

        ApplySelection(toggled, e.RowIndex, columnIndex);
    }

    protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
    {
        if (IsSelectColumn(e.ColumnIndex) && e.Button == MouseButtons.Left)
        {
            if (AreAllRowsSelected())
            {
                ClearSelection();
            }
            else
            {
                SelectAllVisibleRows();
            }

            return;
        }

        base.OnColumnHeaderMouseClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (RowCount == 0)
        {
            base.OnKeyDown(e);
            return;
        }

        if (e.Control && e.KeyCode == Keys.A)
        {
            SelectAllVisibleRows();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (TryGetVerticalDelta(e.KeyCode) is int delta && !e.Alt)
        {
            MoveSelection(delta, e.Control, e.Shift);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.Space)
        {
            int row = CurrentCell?.RowIndex ?? _selectionAnchor;
            SelectGroup(row, toggle: true);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.Shift && e.KeyCode == Keys.Space)
        {
            int row = CurrentCell?.RowIndex ?? 0;
            var selected = new HashSet<int>();
            int start = Math.Min(_selectionAnchor, row);
            int end = Math.Max(_selectionAnchor, row);
            for (int i = start; i <= end; i++)
            {
                selected.Add(i);
            }

            ApplySelection(selected, row, SanitizeColumnIndex(CurrentCell?.ColumnIndex ?? 0));
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        HitTestInfo hit = HitTest(e.X, e.Y);
        SetHoverRow(hit.Type == DataGridViewHitTestType.Cell ? hit.RowIndex : -1);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        SetHoverRow(-1);
    }

    private void SetHoverRow(int rowIndex)
    {
        if (_hoverRow == rowIndex)
        {
            return;
        }

        int previous = _hoverRow;
        _hoverRow = rowIndex;
        InvalidateDataRow(previous);
        InvalidateDataRow(_hoverRow);
    }

    private void InvalidateDataRow(int rowIndex)
    {
        if (rowIndex >= 0 && rowIndex < RowCount)
        {
            InvalidateRow(rowIndex);
        }
    }

    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics == null || e.ColumnIndex < 0)
        {
            base.OnCellPainting(e);
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (e.RowIndex < 0)
        {
            PaintHeaderCell(e);
            e.Handled = true;
            return;
        }

        bool selected = (e.State & DataGridViewElementStates.Selected) != 0;
        bool hovered = e.RowIndex == _hoverRow;
        ReviewRowBand band = ResolveRowBand?.Invoke(e.RowIndex) ?? ReviewRowBand.Match;
        Color baseBack = band switch
        {
            ReviewRowBand.Sport => DarkMode.HoverSurface,
            ReviewRowBand.File => DarkMode.ElevatedSurface,
            _ => DarkMode.Surface
        };
        Color rowBack;
        if (band == ReviewRowBand.File)
        {
            rowBack = selected
                ? Blend(baseBack, DarkMode.Secondary, hovered ? 0.14f : 0.08f)
                : hovered
                    ? Blend(baseBack, DarkMode.Secondary, 0.06f)
                    : baseBack;
        }
        else
        {
            rowBack = selected
                ? Blend(baseBack, DarkMode.Primary, hovered ? 0.22f : 0.14f)
                : hovered
                    ? Blend(baseBack, DarkMode.Primary, 0.09f)
                    : baseBack;
        }

        using (var back = new SolidBrush(rowBack))
        {
            e.Graphics.FillRectangle(back, e.CellBounds);
        }

        Color divider = band == ReviewRowBand.Match
            ? Blend(DarkMode.Border, DarkMode.Surface, 0.55f)
            : DarkMode.Border;
        using (var line = new Pen(divider))
        {
            e.Graphics.DrawLine(
                line,
                e.CellBounds.Left,
                e.CellBounds.Bottom - 1,
                e.CellBounds.Right,
                e.CellBounds.Bottom - 1);
        }

        if (e.ColumnIndex == 0 && band == ReviewRowBand.File)
        {
            using var bar = new SolidBrush(DarkMode.Secondary);
            e.Graphics.FillRectangle(
                bar,
                new Rectangle(e.CellBounds.Left, e.CellBounds.Top, 3, e.CellBounds.Height));
        }
        else if (e.ColumnIndex == 0 && band == ReviewRowBand.Sport)
        {
            using var bar = new SolidBrush(DarkMode.PrimaryMuted);
            e.Graphics.FillRectangle(
                bar,
                new Rectangle(e.CellBounds.Left, e.CellBounds.Top, 3, e.CellBounds.Height));
        }

        string column = Columns[e.ColumnIndex].Name;
        string text = e.FormattedValue?.ToString() ?? string.Empty;

        if (column == SelectColumnName)
        {
            PaintCheckbox(e.Graphics, e.CellBounds, selected);
        }
        else if (column == "State")
        {
            PaintStatusPill(e.Graphics, e.CellBounds, text);
        }
        else
        {
            if (column == "Item")
            {
                PaintItemChrome(
                    e.Graphics,
                    e.CellBounds,
                    band,
                    ResolveExpanded?.Invoke(e.RowIndex) ?? false);
            }

            Color textColor = e.CellStyle?.ForeColor ?? DarkMode.TextPrimary;
            Font font = e.CellStyle?.Font ?? Font;
            int padLeft = e.CellStyle?.Padding.Left ?? 8;
            var textBounds = new Rectangle(
                e.CellBounds.X + padLeft,
                e.CellBounds.Y,
                Math.Max(0, e.CellBounds.Width - padLeft - 8),
                e.CellBounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                text,
                font,
                textBounds,
                textColor,
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.Left |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding);
        }

        e.Handled = true;
    }

    private void PaintHeaderCell(DataGridViewCellPaintingEventArgs e)
    {
        using (var back = new SolidBrush(DarkMode.Surface))
        {
            e.Graphics.FillRectangle(back, e.CellBounds);
        }

        using (var line = new Pen(DarkMode.Border))
        {
            e.Graphics.DrawLine(
                line,
                e.CellBounds.Left,
                e.CellBounds.Bottom - 1,
                e.CellBounds.Right,
                e.CellBounds.Bottom - 1);
        }

        if (IsSelectColumn(e.ColumnIndex))
        {
            bool all = AreAllRowsSelected();
            bool any = Rows.GetFirstRow(DataGridViewElementStates.Selected) >= 0;
            PaintCheckbox(e.Graphics, e.CellBounds, all, mixed: any && !all);
            return;
        }

        var textBounds = Rectangle.Inflate(e.CellBounds, -10, 0);
        TextRenderer.DrawText(
            e.Graphics,
            (e.FormattedValue?.ToString() ?? string.Empty).ToUpperInvariant(),
            _headerFont,
            textBounds,
            DarkMode.TextDisabled,
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.Left |
            TextFormatFlags.EndEllipsis);
    }

    protected override void OnRowHeightInfoNeeded(DataGridViewRowHeightInfoNeededEventArgs e)
    {
        ReviewRowBand band = ResolveRowBand?.Invoke(e.RowIndex) ?? ReviewRowBand.Match;
        e.Height = band switch
        {
            ReviewRowBand.Sport => 52,
            ReviewRowBand.File => 58,
            _ => 56
        };
    }

    private static void PaintItemChrome(
        Graphics graphics,
        Rectangle cellBounds,
        ReviewRowBand band,
        bool expanded)
    {
        int midY = cellBounds.Y + cellBounds.Height / 2;

        if (band == ReviewRowBand.Match)
        {
            PaintLedDot(graphics, cellBounds.X + 28, midY, DarkMode.Primary);
            return;
        }

        if (band == ReviewRowBand.File)
        {
            PaintChevron(graphics, cellBounds.X + 8, midY, expanded, DarkMode.Secondary, 8);
            PaintFileIcon(graphics, cellBounds.X + 28, midY, DarkMode.Secondary);
            return;
        }

        PaintChevron(graphics, cellBounds.X + 8, midY, expanded, DarkMode.PrimaryMuted, 7);
        PaintSearchIcon(graphics, cellBounds.X + 26, midY, DarkMode.Primary);
    }

    private static void PaintSearchIcon(Graphics graphics, int x, int midY, Color color)
    {
        using var pen = new Pen(color, 1.7f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        int y = midY - 6;
        graphics.DrawEllipse(pen, x, y, 11, 11);
        graphics.DrawLine(pen, x + 9, y + 9, x + 15, y + 15);
    }

    private static void PaintLedDot(Graphics graphics, int x, int midY, Color color)
    {
        const int core = 9;
        int cy = midY - core / 2;
        using (var glow = new SolidBrush(Color.FromArgb(42, color)))
        {
            graphics.FillEllipse(glow, x - 5, cy - 5, core + 10, core + 10);
        }

        using (var halo = new SolidBrush(Color.FromArgb(96, color)))
        {
            graphics.FillEllipse(halo, x - 2, cy - 2, core + 4, core + 4);
        }

        using (var fill = new SolidBrush(color))
        {
            graphics.FillEllipse(fill, x, cy, core, core);
        }

        using (var shine = new SolidBrush(Color.FromArgb(170, 255, 255, 255)))
        {
            graphics.FillEllipse(shine, x + 2, cy + 2, 3, 3);
        }
    }

    private static void PaintChevron(
        Graphics graphics,
        int x,
        int midY,
        bool expanded,
        Color color,
        int size)
    {
        using var brush = new SolidBrush(color);
        PointF[] points = expanded
            ? new[]
            {
                new PointF(x, midY - size / 3f),
                new PointF(x + size, midY - size / 3f),
                new PointF(x + size / 2f, midY + size / 2.5f)
            }
            : new[]
            {
                new PointF(x, midY - size / 2f),
                new PointF(x + size - 1, midY),
                new PointF(x, midY + size / 2f)
            };
        graphics.FillPolygon(brush, points);
    }

    private static void PaintFileIcon(Graphics graphics, int x, int midY, Color color)
    {
        const int width = 11;
        const int height = 13;
        const int fold = 4;
        int y = midY - height / 2;
        Point[] outline =
        {
            new(x, y),
            new(x + width - fold, y),
            new(x + width, y + fold),
            new(x + width, y + height),
            new(x, y + height)
        };

        using var fill = new SolidBrush(Color.FromArgb(36, color));
        using var pen = new Pen(color, 1.3f)
        {
            LineJoin = LineJoin.Round,
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.FillPolygon(fill, outline);
        graphics.DrawPolygon(pen, outline);
        graphics.DrawLine(pen, x + width - fold, y, x + width - fold, y + fold);
        graphics.DrawLine(pen, x + width - fold, y + fold, x + width, y + fold);
    }

    private void PaintStatusPill(Graphics graphics, Rectangle cellBounds, string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return;
        }

        (Color fill, Color text) = status switch
        {
            "Accurate" => (Blend(DarkMode.Surface, DarkMode.Success, 0.18f), DarkMode.Success),
            "Denied" => (Blend(DarkMode.Surface, DarkMode.Error, 0.2f), DarkMode.Error),
            "Review" => (Blend(DarkMode.Surface, DarkMode.Warning, 0.16f), DarkMode.Warning),
            "Complete" => (Blend(DarkMode.Surface, DarkMode.Success, 0.18f), DarkMode.Success),
            "Incomplete" => (Blend(DarkMode.Surface, DarkMode.Warning, 0.16f), DarkMode.Warning),
            _ => (DarkMode.ElevatedSurface, DarkMode.TextSecondary)
        };

        Size textSize = TextRenderer.MeasureText(status, _pillFont);
        int width = Math.Min(cellBounds.Width - 12, Math.Max(64, textSize.Width + 16));
        int x = cellBounds.X + Math.Max(6, (cellBounds.Width - width) / 2);
        int y = cellBounds.Y + (cellBounds.Height - PillHeight) / 2;
        var pill = new Rectangle(x, y, width, PillHeight);

        using GraphicsPath path = CreateRoundPath(pill, PillHeight / 2);
        using var brush = new SolidBrush(fill);
        graphics.FillPath(brush, path);

        TextRenderer.DrawText(
            graphics,
            status,
            _pillFont,
            pill,
            text,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }

    private static void PaintCheckbox(
        Graphics graphics,
        Rectangle cellBounds,
        bool selected,
        bool mixed = false)
    {
        int x = cellBounds.X + (cellBounds.Width - CheckboxSize) / 2;
        int y = cellBounds.Y + (cellBounds.Height - CheckboxSize) / 2;
        var box = new Rectangle(x, y, CheckboxSize, CheckboxSize);

        using GraphicsPath path = CreateRoundPath(box, 4);

        if (selected || mixed)
        {
            using var fill = new SolidBrush(DarkMode.Primary);
            graphics.FillPath(fill, path);

            using var checkPen = new Pen(DarkMode.Background, 1.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            if (mixed)
            {
                graphics.DrawLine(
                    checkPen,
                    box.Left + 4,
                    box.Y + box.Height / 2f,
                    box.Right - 4,
                    box.Y + box.Height / 2f);
            }
            else
            {
                graphics.DrawLines(
                    checkPen,
                    new[]
                    {
                        new Point(box.Left + 4, box.Y + 10),
                        new Point(box.Left + 8, box.Y + 14),
                        new Point(box.Right - 4, box.Y + 6)
                    });
            }
        }
        else
        {
            using var fill = new SolidBrush(DarkMode.ElevatedSurface);
            using var border = new Pen(DarkMode.Border);
            graphics.FillPath(fill, path);
            graphics.DrawPath(border, path);
        }
    }

    private static int? TryGetVerticalDelta(Keys key)
    {
        return key switch
        {
            Keys.Up or Keys.W or Keys.Oemcomma => -1,
            Keys.Down or Keys.S or Keys.O => 1,
            _ => null
        };
    }

    private void MoveSelection(int delta, bool control, bool shift)
    {
        int current = shift
            ? CurrentCell?.RowIndex ?? _selectionAnchor
            : CurrentCell?.RowIndex ?? 0;
        int next = Math.Clamp(current + delta, 0, RowCount - 1);
        int column = SanitizeColumnIndex(CurrentCell?.ColumnIndex ?? 0);

        if (control && !shift)
        {
            ApplySelection(CaptureSelection(), next, column);
        }
        else if (shift)
        {
            var selected = new HashSet<int>();
            int start = Math.Min(_selectionAnchor, next);
            int end = Math.Max(_selectionAnchor, next);
            for (int i = start; i <= end; i++)
            {
                selected.Add(i);
            }

            ApplySelection(selected, next, column);
        }
        else
        {
            _selectionAnchor = next;
            ApplySelection(new HashSet<int> { next }, next, column);
        }

        EnsureRowVisible(next);
    }

    public void SelectGroup(int rowIndex, bool toggle)
    {
        if (rowIndex < 0 || rowIndex >= RowCount)
        {
            return;
        }

        IReadOnlyList<int> group = ResolveSelectableGroup?.Invoke(rowIndex) ?? new[] { rowIndex };
        if (group.Count == 0)
        {
            return;
        }

        var selected = toggle ? CaptureSelection() : new HashSet<int>();
        if (toggle)
        {
            bool allOn = group.All(selected.Contains);
            foreach (int index in group)
            {
                if (allOn)
                {
                    selected.Remove(index);
                }
                else
                {
                    selected.Add(index);
                }
            }
        }
        else
        {
            foreach (int index in group)
            {
                selected.Add(index);
            }
        }

        int current = group[0];
        _selectionAnchor = current;
        ApplySelection(selected, current, SanitizeColumnIndex(CurrentCell?.ColumnIndex ?? 0));
        EnsureRowVisible(current);
    }

    private HashSet<int> CaptureSelection()
    {
        var selected = new HashSet<int>();
        int rowIndex = Rows.GetFirstRow(DataGridViewElementStates.Selected);

        while (rowIndex >= 0)
        {
            selected.Add(rowIndex);
            rowIndex = Rows.GetNextRow(rowIndex, DataGridViewElementStates.Selected);
        }

        return selected;
    }

    private void ApplySelection(HashSet<int> selected, int currentRow, int currentColumn)
    {
        ClearSelection();

        if (currentRow >= 0 && currentRow < RowCount && Columns.Count > 0)
        {
            CurrentCell = Rows[currentRow].Cells[currentColumn];
        }

        ClearSelection();

        foreach (int rowIndex in selected)
        {
            if (rowIndex >= 0 && rowIndex < RowCount)
            {
                Rows[rowIndex].Selected = true;
            }
        }

        Invalidate();
    }

    private bool AreAllRowsSelected()
    {
        if (RowCount == 0)
        {
            return false;
        }

        int selected = 0;
        int rowIndex = Rows.GetFirstRow(DataGridViewElementStates.Selected);
        while (rowIndex >= 0)
        {
            selected++;
            rowIndex = Rows.GetNextRow(rowIndex, DataGridViewElementStates.Selected);
        }

        return selected == RowCount;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (RowCount <= 0)
        {
            base.OnMouseWheel(e);
            return;
        }

        int lines = SystemInformation.MouseWheelScrollLines;
        if (lines == 0)
        {
            lines = 3;
        }
        else if (lines < 0)
        {
            lines = Math.Max(1, GetVisibleRowCount());
        }

        int next = Math.Clamp(
            GetFirstVisibleRow() - Math.Sign(e.Delta) * lines,
            0,
            GetScrollMaximum());

        SetFirstVisibleRow(next);

        if (e is HandledMouseEventArgs handled)
        {
            handled.Handled = true;
        }

        base.OnMouseWheel(e);
    }

    public int GetVisibleRowCount()
    {
        if (!IsHandleCreated || RowCount <= 0)
        {
            return 1;
        }

        try
        {
            return Math.Max(1, DisplayedRowCount(true));
        }
        catch (ArgumentOutOfRangeException)
        {
            return 1;
        }
        catch (InvalidOperationException)
        {
            return 1;
        }
    }

    public int GetFirstVisibleRow()
    {
        if (!IsHandleCreated || RowCount <= 0)
        {
            return 0;
        }

        try
        {
            return Math.Max(0, FirstDisplayedScrollingRowIndex);
        }
        catch (InvalidOperationException)
        {
            return 0;
        }
    }

    public int GetScrollMaximum()
    {
        return Math.Max(0, RowCount - GetVisibleRowCount());
    }

    public void SetFirstVisibleRow(int rowIndex)
    {
        if (!IsHandleCreated || RowCount <= 0)
        {
            return;
        }

        int next = Math.Clamp(rowIndex, 0, GetScrollMaximum());
        try
        {
            if (FirstDisplayedScrollingRowIndex != next)
            {
                FirstDisplayedScrollingRowIndex = next;
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }

    private void EnsureRowVisible(int rowIndex)
    {
        try
        {
            int first = FirstDisplayedScrollingRowIndex;
            int displayed = DisplayedRowCount(false);
            if (rowIndex < first)
            {
                FirstDisplayedScrollingRowIndex = rowIndex;
            }
            else if (displayed > 0 && rowIndex >= first + displayed)
            {
                FirstDisplayedScrollingRowIndex = Math.Max(0, rowIndex - displayed + 1);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private bool IsSelectColumn(int columnIndex)
    {
        return columnIndex >= 0 &&
               columnIndex < Columns.Count &&
               Columns[columnIndex].Name == SelectColumnName;
    }

    private int SanitizeColumnIndex(int columnIndex)
    {
        if (Columns.Count == 0)
        {
            return 0;
        }

        if (columnIndex < 0 || columnIndex >= Columns.Count)
        {
            return 0;
        }

        return columnIndex;
    }

    private static bool IsControlDown()
    {
        return (ModifierKeys & Keys.Control) == Keys.Control;
    }

    private static bool IsShiftDown()
    {
        return (ModifierKeys & Keys.Shift) == Keys.Shift;
    }

    private static Color Blend(Color from, Color to, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            (int)(from.R + (to.R - from.R) * amount),
            (int)(from.G + (to.G - from.G) * amount),
            (int)(from.B + (to.B - from.B) * amount));
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _headerFont.Dispose();
            _pillFont.Dispose();
        }

        base.Dispose(disposing);
    }
}
