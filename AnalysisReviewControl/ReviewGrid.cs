using Themes;

namespace AnalysisReviewControl;

internal sealed class ReviewGrid : DataGridView
{
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
}
