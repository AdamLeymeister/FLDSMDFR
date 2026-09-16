using Themes;

namespace AnalysisReviewControl;

internal sealed class ReviewGrid : DataGridView
{
    private int _selectionAnchor;

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
        BackgroundColor = DarkMode.Background;
        BorderStyle = BorderStyle.None;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        ColumnHeadersHeight = 36;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        EditMode = DataGridViewEditMode.EditProgrammatically;
        EnableHeadersVisualStyles = false;
        GridColor = DarkMode.Border;
        MultiSelect = true;
        RowHeadersVisible = false;
        RowTemplate.Height = 30;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        ShowCellToolTips = true;
        StandardTab = true;

        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = DarkMode.Surface,
            ForeColor = DarkMode.TextSecondary,
            SelectionBackColor = DarkMode.Surface,
            SelectionForeColor = DarkMode.TextSecondary,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0),
            WrapMode = DataGridViewTriState.False
        };

        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = DarkMode.Background,
            ForeColor = DarkMode.TextPrimary,
            SelectionBackColor = DarkMode.HoverSurface,
            SelectionForeColor = DarkMode.TextPrimary,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0),
            WrapMode = DataGridViewTriState.False
        };

        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = DarkMode.ElevatedSurface,
            ForeColor = DarkMode.TextPrimary,
            SelectionBackColor = DarkMode.HoverSurface,
            SelectionForeColor = DarkMode.TextPrimary,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0),
            WrapMode = DataGridViewTriState.False
        };
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

        bool control = IsControlDown();
        bool shift = IsShiftDown();

        if (!control && !shift)
        {
            _selectionAnchor = e.RowIndex;
            base.OnCellMouseDown(e);
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

        if (e.Control && e.KeyCode == Keys.Space)
        {
            int row = CurrentCell?.RowIndex ?? _selectionAnchor;
            ToggleFocusedRow(row);
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

        if (e.Control && e.KeyCode is Keys.Up or Keys.Down)
        {
            int current = CurrentCell?.RowIndex ?? 0;
            int next = e.KeyCode == Keys.Down ? current + 1 : current - 1;
            next = Math.Clamp(next, 0, RowCount - 1);
            ApplySelection(CaptureSelection(), next, SanitizeColumnIndex(CurrentCell?.ColumnIndex ?? 0));
            EnsureRowVisible(next);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.Shift && e.KeyCode is Keys.Up or Keys.Down)
        {
            int current = CurrentCell?.RowIndex ?? _selectionAnchor;
            int next = e.KeyCode == Keys.Down ? current + 1 : current - 1;
            next = Math.Clamp(next, 0, RowCount - 1);

            var selected = new HashSet<int>();
            int start = Math.Min(_selectionAnchor, next);
            int end = Math.Max(_selectionAnchor, next);
            for (int i = start; i <= end; i++)
            {
                selected.Add(i);
            }

            ApplySelection(selected, next, SanitizeColumnIndex(CurrentCell?.ColumnIndex ?? 0));
            EnsureRowVisible(next);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void ToggleFocusedRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= RowCount)
        {
            return;
        }

        var selected = CaptureSelection();
        if (!selected.Add(rowIndex))
        {
            selected.Remove(rowIndex);
        }

        ApplySelection(
            selected,
            rowIndex,
            SanitizeColumnIndex(CurrentCell?.ColumnIndex ?? 0));
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
}
