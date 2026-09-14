using FLDSMDFR.Core.Models;
using Themes;

namespace AnalysisReviewControl;

public partial class AnalysisReviewControl : ReviewUserControl
{
    private const string ColState = "State";
    private const string ColItem = "Item";
    private const string ColFile = "File";
    private const string ColSummary = "Summary";

    private const string AllSports = "(All sports)";
    private const string AllFiles = "(All files)";
    private const string StatusAll = "All rows";
    private const string StatusReview = "Needs review";
    private const string StatusAccurate = "Accurate";

    private const int MaxFileFilterItems = 400;
    private const int WaitCursorThreshold = 200_000;

    private AnalysisTableRow[] _rows = Array.Empty<AnalysisTableRow>();
    private readonly List<SportNode> _sports = new();
    private readonly List<OutlineRow> _visible = new();
    private int[] _sportIndexByRow = Array.Empty<int>();
    private int[] _fileIndexByRow = Array.Empty<int>();

    private readonly Panel _pnlToolbar = new();
    private readonly Panel _pnlStatus = new();
    private readonly TextBox _txtSearch = new();
    private readonly ComboBox _cboSport = new();
    private readonly ComboBox _cboFile = new();
    private readonly ComboBox _cboStatus = new();
    private readonly Button _btnConfirm = new();
    private readonly Button _btnDeny = new();
    private readonly Button _btnNext = new();
    private readonly Button _btnCollapse = new();
    private readonly Label _lblStats = new();
    private readonly Label _lblHelp = new();
    private readonly ReviewGrid _grid = new();
    private readonly System.Windows.Forms.Timer _searchDebounce = new();

    private Font? _sportFont;
    private Font? _fileFont;
    private bool _updatingUi;
    private string _searchText = string.Empty;

    public AnalysisReviewControl()
    {
        InitializeComponent();
        ConfigureControl();
    }

    private void ConfigureControl()
    {
        BackColor = DarkMode.Background;
        Font = CreateOwnedFont("Segoe UI", 9.5f);
        _sportFont = CreateOwnedFont("Segoe UI", 10f, FontStyle.Bold);
        _fileFont = CreateOwnedFont("Segoe UI", 9.5f);

        ConfigureToolbar();
        ConfigureStatusBar();
        ConfigureGrid();
        WireEvents();
        UpdateStats();
    }

    private void ConfigureToolbar()
    {
        _pnlToolbar.Dock = DockStyle.Top;
        _pnlToolbar.Height = 72;
        _pnlToolbar.BackColor = DarkMode.Surface;
        _pnlToolbar.Padding = new Padding(12, 10, 12, 10);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = DarkMode.Surface
        };

        _txtSearch.Width = 220;
        _txtSearch.PlaceholderText = "Filter sport, file, or match";
        StyleTextBox(_txtSearch);

        StyleCombo(_cboSport, 150);
        StyleCombo(_cboFile, 200);
        StyleCombo(_cboStatus, 130);

        _cboStatus.Items.AddRange(new object[] { StatusAll, StatusReview, StatusAccurate });
        _cboStatus.SelectedIndex = 0;
        _cboSport.Items.Add(AllSports);
        _cboSport.SelectedIndex = 0;
        _cboFile.Items.Add(AllFiles);
        _cboFile.SelectedIndex = 0;

        StyleActionButton(_btnConfirm, "Confirm  Y", DarkMode.Success, DarkMode.Background, 110);
        StyleActionButton(_btnDeny, "Deny  N", DarkMode.Error, DarkMode.Background, 100);
        StyleActionButton(_btnNext, "Next  F3", DarkMode.ElevatedSurface, DarkMode.TextPrimary, 96);
        StyleActionButton(_btnCollapse, "Collapse all", DarkMode.ElevatedSurface, DarkMode.TextPrimary, 110);

        flow.Controls.Add(Wrap(CreateCaption("Search"), _txtSearch));
        flow.Controls.Add(Wrap(CreateCaption("Sport"), _cboSport));
        flow.Controls.Add(Wrap(CreateCaption("File"), _cboFile));
        flow.Controls.Add(Wrap(CreateCaption("View"), _cboStatus));
        flow.Controls.Add(Wrap(CreateCaption("Review"), CreateButtonRow()));

        _pnlToolbar.Controls.Add(flow);
        Controls.Add(_pnlToolbar);
    }

    private Control CreateButtonRow()
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Width = 450,
            Height = 28,
            BackColor = DarkMode.Surface,
            Margin = Padding.Empty
        };

        panel.Controls.Add(_btnConfirm);
        panel.Controls.Add(_btnDeny);
        panel.Controls.Add(_btnNext);
        panel.Controls.Add(_btnCollapse);
        return panel;
    }

    private Label CreateCaption(string text)
    {
        return new Label
        {
            Text = text.ToUpperInvariant(),
            AutoSize = true,
            ForeColor = DarkMode.TextDisabled,
            BackColor = DarkMode.Surface,
            Font = CreateOwnedFont("Segoe UI", 7.5f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 2)
        };
    }

    private Control Wrap(Label caption, Control editor)
    {
        var panel = new Panel
        {
            Width = Math.Max(editor.Width, 90),
            Height = 52,
            BackColor = DarkMode.Surface,
            Margin = new Padding(0, 0, 16, 0)
        };

        caption.Dock = DockStyle.Top;
        caption.Height = 16;
        editor.Dock = DockStyle.Bottom;
        editor.Height = 28;
        panel.Controls.Add(editor);
        panel.Controls.Add(caption);
        panel.Width = editor.Width;
        return panel;
    }

    private void ConfigureStatusBar()
    {
        _pnlStatus.Dock = DockStyle.Bottom;
        _pnlStatus.Height = 32;
        _pnlStatus.BackColor = DarkMode.Surface;
        _pnlStatus.Padding = new Padding(12, 0, 12, 0);

        _lblStats.Dock = DockStyle.Left;
        _lblStats.AutoSize = false;
        _lblStats.Width = 520;
        _lblStats.TextAlign = ContentAlignment.MiddleLeft;
        _lblStats.ForeColor = DarkMode.TextSecondary;
        _lblStats.BackColor = DarkMode.Surface;

        _lblHelp.Dock = DockStyle.Fill;
        _lblHelp.TextAlign = ContentAlignment.MiddleRight;
        _lblHelp.ForeColor = DarkMode.TextDisabled;
        _lblHelp.BackColor = DarkMode.Surface;
        _lblHelp.Text = "Click group to expand   Y/N confirm/deny   Left/Right collapse/expand   F3 next review";

        _pnlStatus.Controls.Add(_lblHelp);
        _pnlStatus.Controls.Add(_lblStats);
        Controls.Add(_pnlStatus);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.Font = CreateOwnedFont("Segoe UI", 9.5f);
        _grid.ColumnHeadersDefaultCellStyle.Font = CreateOwnedFont("Segoe UI", 8.5f, FontStyle.Bold);
        _grid.AlternatingRowsDefaultCellStyle = _grid.DefaultCellStyle.Clone();

        _grid.Columns.Add(CreateTextColumn(ColState, "Status", 96, 88));
        _grid.Columns.Add(CreateTextColumn(ColItem, "Item", 320, 160));
        _grid.Columns.Add(CreateTextColumn(ColFile, "File", 180, 100));
        _grid.Columns.Add(CreateTextColumn(ColSummary, "Summary", 220, 140));

        _grid.Columns[ColState].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _grid.Columns[ColState].Width = 96;
        _grid.Columns[ColState].FillWeight = 1;
        _grid.Columns[ColItem].FillWeight = 48;
        _grid.Columns[ColFile].FillWeight = 24;
        _grid.Columns[ColSummary].FillWeight = 27;

        _grid.Columns[ColState].DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;
        _grid.Columns[ColState].HeaderCell.Style.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        Controls.Add(_grid);
        _grid.BringToFront();
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(
        string name,
        string header,
        int width,
        int minWidth)
    {
        return new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            MinimumWidth = minWidth,
            Width = width,
            FillWeight = width,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            Resizable = DataGridViewTriState.True
        };
    }

    private void WireEvents()
    {
        _searchDebounce.Interval = 180;
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            _searchText = _txtSearch.Text.Trim();
            ApplyFilterAndRebuild(keepCurrent: true);
        };

        components?.Add(_searchDebounce);

        _txtSearch.TextChanged += (_, _) =>
        {
            _searchDebounce.Stop();
            _searchDebounce.Start();
        };

        _cboSport.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingUi) return;
            RebuildFileFilter();
            ApplyFilterAndRebuild(keepCurrent: true);
        };

        _cboFile.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingUi) return;
            ApplyFilterAndRebuild(keepCurrent: true);
        };

        _cboStatus.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingUi) return;
            ApplyFilterAndRebuild(keepCurrent: true);
        };

        _btnConfirm.Click += (_, _) => ReviewSelected(accurate: true, advance: true);
        _btnDeny.Click += (_, _) => ReviewSelected(accurate: false, advance: true);
        _btnNext.Click += (_, _) => JumpToNextReview();
        _btnCollapse.Click += (_, _) => CollapseAll();

        _grid.CellValueNeeded += Grid_CellValueNeeded;
        _grid.CellFormatting += Grid_CellFormatting;
        _grid.CellMouseClick += Grid_CellMouseClick;
        _grid.CellMouseEnter += Grid_CellMouseEnter;
        _grid.KeyDown += Grid_KeyDown;
        _grid.DataError += (_, e) => e.ThrowException = false;
        _grid.Paint += Grid_Paint;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_grid.Focused || _grid.ContainsFocus)
        {
            if (keyData == (Keys.Control | Keys.A))
            {
                return true;
            }

            if (keyData == (Keys.Control | Keys.Y))
            {
                ReviewAllFiltered(accurate: true);
                return true;
            }

            if (keyData == (Keys.Control | Keys.N))
            {
                ReviewAllFiltered(accurate: false);
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    public void LoadData(IEnumerable<AnalysisTableRow> rows)
    {
        _rows = rows as AnalysisTableRow[] ?? rows.ToArray();
        BuildSportIndex();
        RebuildSportFilter();
        RebuildFileFilter();
        ApplyFilterAndRebuild(keepCurrent: false);

        if (_visible.Count > 0)
        {
            SelectVisibleRow(0);
            _grid.Focus();
        }
    }

    private void BuildSportIndex()
    {
        _sports.Clear();
        _sportIndexByRow = new int[_rows.Length];
        _fileIndexByRow = new int[_rows.Length];

        var sports = new Dictionary<string, SportNode>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _rows.Length; i++)
        {
            AnalysisTableRow row = _rows[i];
            string sportName = row.Sport;

            if (!sports.TryGetValue(sportName, out SportNode? sport))
            {
                sport = new SportNode { Name = sportName };
                sports.Add(sportName, sport);
            }

            sport.RowIndices.Add(i);
            sport.Total++;
            if (row.IsAccurate)
            {
                sport.Accurate++;
            }
        }

        _sports.AddRange(sports.Values.OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase));

        for (int sportIndex = 0; sportIndex < _sports.Count; sportIndex++)
        {
            SportNode sport = _sports[sportIndex];
            foreach (int rowIndex in sport.RowIndices)
            {
                _sportIndexByRow[rowIndex] = sportIndex;
                _fileIndexByRow[rowIndex] = -1;
            }
        }
    }

    private void EnsureFiles(SportNode sport)
    {
        if (sport.FilesBuilt)
        {
            return;
        }

        var files = new Dictionary<string, FileNode>(StringComparer.OrdinalIgnoreCase);

        foreach (int rowIndex in sport.RowIndices)
        {
            AnalysisTableRow row = _rows[rowIndex];
            if (!files.TryGetValue(row.File, out FileNode? file))
            {
                file = new FileNode { Name = row.File };
                files.Add(row.File, file);
            }

            file.RowIndices.Add(rowIndex);
            if (row.IsAccurate)
            {
                file.Accurate++;
            }
        }

        sport.Files.AddRange(
            files.Values.OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase));
        sport.FilesBuilt = true;

        for (int fileIndex = 0; fileIndex < sport.Files.Count; fileIndex++)
        {
            foreach (int rowIndex in sport.Files[fileIndex].RowIndices)
            {
                _fileIndexByRow[rowIndex] = fileIndex;
            }
        }

        RefreshFileFilterCounts(sport);
    }

    private void ApplyFilterAndRebuild(bool keepCurrent)
    {
        OutlineRow? current = keepCurrent ? GetCurrentOutline() : null;

        RunPossiblyExpensive(() =>
        {
            RefreshFilterCounts();
            RebuildVisible();
        });

        int select = 0;
        if (current != null)
        {
            int found = FindVisibleIndex(current.Value);
            if (found >= 0)
            {
                select = found;
            }
        }

        if (_visible.Count > 0)
        {
            SelectVisibleRow(Math.Min(select, _visible.Count - 1));
        }

        UpdateStats();
    }

    private void RefreshFilterCounts()
    {
        string? sportFilter = GetSelectedSport();
        string? fileFilter = GetSelectedFile();
        string status = _cboStatus.SelectedItem as string ?? StatusAll;
        bool scanRows = _searchText.Length > 0 ||
                        fileFilter != null ||
                        status != StatusAll;

        foreach (SportNode sport in _sports)
        {
            if (sportFilter != null &&
                !string.Equals(sport.Name, sportFilter, StringComparison.OrdinalIgnoreCase))
            {
                sport.FilteredTotal = 0;
                sport.FilteredAccurate = 0;
                continue;
            }

            if (!scanRows)
            {
                sport.FilteredTotal = sport.Total;
                sport.FilteredAccurate = sport.Accurate;
                if (sport.FilesBuilt)
                {
                    foreach (FileNode file in sport.Files)
                    {
                        file.FilteredTotal = file.RowIndices.Count;
                        file.FilteredAccurate = file.Accurate;
                    }
                }

                continue;
            }

            CountFiltered(sport, fileFilter, status);
        }
    }

    private void RefreshFileFilterCounts(SportNode sport)
    {
        string? fileFilter = GetSelectedFile();
        string status = _cboStatus.SelectedItem as string ?? StatusAll;
        bool scanRows = _searchText.Length > 0 ||
                        fileFilter != null ||
                        status != StatusAll;

        foreach (FileNode file in sport.Files)
        {
            if (!scanRows)
            {
                file.FilteredTotal = file.RowIndices.Count;
                file.FilteredAccurate = file.Accurate;
                continue;
            }

            int total = 0;
            int accurate = 0;
            foreach (int rowIndex in file.RowIndices)
            {
                if (!MatchesRow(_rows[rowIndex], fileFilter, status))
                {
                    continue;
                }

                total++;
                if (_rows[rowIndex].IsAccurate)
                {
                    accurate++;
                }
            }

            file.FilteredTotal = total;
            file.FilteredAccurate = accurate;
        }
    }

    private void CountFiltered(SportNode sport, string? fileFilter, string status)
    {
        int total = 0;
        int accurate = 0;

        foreach (int rowIndex in sport.RowIndices)
        {
            AnalysisTableRow row = _rows[rowIndex];
            if (!MatchesRow(row, fileFilter, status))
            {
                continue;
            }

            total++;
            if (row.IsAccurate)
            {
                accurate++;
            }
        }

        sport.FilteredTotal = total;
        sport.FilteredAccurate = accurate;

        if (sport.FilesBuilt)
        {
            RefreshFileFilterCounts(sport);
        }
    }

    private bool MatchesRow(AnalysisTableRow row, string? fileFilter, string status)
    {
        if (status == StatusAccurate && !row.IsAccurate)
        {
            return false;
        }

        if (status == StatusReview && row.IsAccurate)
        {
            return false;
        }

        if (fileFilter != null &&
            !string.Equals(row.File, fileFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_searchText.Length == 0)
        {
            return true;
        }

        return Contains(row.Sport, _searchText) ||
               Contains(row.Found, _searchText) ||
               Contains(row.File, _searchText);
    }

    private static bool Contains(string value, string search)
    {
        return value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RebuildVisible()
    {
        _visible.Clear();

        for (int sportIndex = 0; sportIndex < _sports.Count; sportIndex++)
        {
            SportNode sport = _sports[sportIndex];
            if (sport.FilteredTotal == 0)
            {
                continue;
            }

            _visible.Add(OutlineRow.Sport(sportIndex));

            if (!sport.Expanded)
            {
                continue;
            }

            EnsureFiles(sport);

            for (int fileIndex = 0; fileIndex < sport.Files.Count; fileIndex++)
            {
                FileNode file = sport.Files[fileIndex];
                if (file.FilteredTotal == 0)
                {
                    continue;
                }

                _visible.Add(OutlineRow.File(sportIndex, fileIndex));

                if (!file.Expanded)
                {
                    continue;
                }

                string? fileFilter = GetSelectedFile();
                string status = _cboStatus.SelectedItem as string ?? StatusAll;

                foreach (int rowIndex in file.RowIndices)
                {
                    if (!MatchesRow(_rows[rowIndex], fileFilter, status))
                    {
                        continue;
                    }

                    _visible.Add(OutlineRow.Match(sportIndex, fileIndex, rowIndex));
                }
            }
        }

        _updatingUi = true;
        _grid.RowCount = 0;
        _grid.RowCount = _visible.Count;
        _updatingUi = false;
        _grid.Invalidate();
    }

    private void Grid_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _visible.Count)
        {
            return;
        }

        OutlineRow outline = _visible[e.RowIndex];
        string name = _grid.Columns[e.ColumnIndex].Name;

        e.Value = name switch
        {
            ColState => GetStateText(outline),
            ColItem => GetItemText(outline),
            ColFile => GetFileText(outline),
            ColSummary => GetSummaryText(outline),
            _ => string.Empty
        };
    }

    private string GetStateText(OutlineRow outline)
    {
        (int total, int accurate) = GetCounts(outline);

        if (total == 0)
        {
            return string.Empty;
        }

        if (accurate == 0)
        {
            return "Review";
        }

        if (accurate == total)
        {
            return "Accurate";
        }

        return "Mixed";
    }

    private string GetItemText(OutlineRow outline)
    {
        return outline.Kind switch
        {
            OutlineKind.Sport => $"{Glyph(_sports[outline.SportIndex].Expanded)}  {_sports[outline.SportIndex].Name}",
            OutlineKind.File => $"{Glyph(_sports[outline.SportIndex].Files[outline.FileIndex].Expanded)}  {_sports[outline.SportIndex].Files[outline.FileIndex].Name}",
            _ => _rows[outline.RowIndex].Found
        };
    }

    private static string Glyph(bool expanded)
    {
        return expanded ? "▼" : "▶";
    }

    private string GetFileText(OutlineRow outline)
    {
        if (outline.Kind != OutlineKind.Match)
        {
            return string.Empty;
        }

        return _rows[outline.RowIndex].File;
    }

    private string GetSummaryText(OutlineRow outline)
    {
        if (outline.Kind == OutlineKind.Match)
        {
            return string.Empty;
        }

        (int total, int accurate) = GetCounts(outline);
        return $"{total:N0} matches  ·  {accurate:N0} accurate";
    }

    private (int total, int accurate) GetCounts(OutlineRow outline)
    {
        if (outline.Kind == OutlineKind.Sport)
        {
            SportNode sport = _sports[outline.SportIndex];
            return (sport.FilteredTotal, sport.FilteredAccurate);
        }

        if (outline.Kind == OutlineKind.File)
        {
            FileNode file = _sports[outline.SportIndex].Files[outline.FileIndex];
            return (file.FilteredTotal, file.FilteredAccurate);
        }

        return (1, _rows[outline.RowIndex].IsAccurate ? 1 : 0);
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _visible.Count)
        {
            return;
        }

        OutlineRow outline = _visible[e.RowIndex];
        string column = _grid.Columns[e.ColumnIndex].Name;

        Color back = outline.Kind switch
        {
            OutlineKind.Sport => DarkMode.Surface,
            OutlineKind.File => DarkMode.ElevatedSurface,
            _ => DarkMode.Background
        };

        e.CellStyle.BackColor = back;
        e.CellStyle.SelectionBackColor = DarkMode.HoverSurface;

        int indent = outline.Kind switch
        {
            OutlineKind.Sport => 10,
            OutlineKind.File => 28,
            _ => 48
        };

        if (column == ColItem)
        {
            e.CellStyle.Padding = new Padding(indent, 0, 10, 0);
            e.CellStyle.Font = outline.Kind switch
            {
                OutlineKind.Sport => _sportFont,
                OutlineKind.File => _fileFont,
                _ => Font
            };
            e.CellStyle.ForeColor = outline.Kind == OutlineKind.Sport
                ? DarkMode.Primary
                : DarkMode.TextPrimary;
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
        }
        else if (column == ColState)
        {
            string state = GetStateText(outline);
            e.CellStyle.ForeColor = state switch
            {
                "Accurate" => DarkMode.Success,
                "Review" => DarkMode.Warning,
                "Mixed" => DarkMode.Secondary,
                _ => DarkMode.TextSecondary
            };
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
        }
        else
        {
            e.CellStyle.ForeColor = DarkMode.TextSecondary;
            e.CellStyle.SelectionForeColor = DarkMode.TextSecondary;
        }
    }

    private void Grid_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= _visible.Count)
        {
            return;
        }

        OutlineRow outline = _visible[e.RowIndex];
        string column = _grid.Columns[e.ColumnIndex].Name;

        if (column == ColState)
        {
            ReviewSelected(toggle: true, advance: false);
            return;
        }

        if (outline.Kind != OutlineKind.Match)
        {
            ToggleExpanded(outline);
        }
    }

    private void Grid_CellMouseEnter(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex < 0)
        {
            _grid.Cursor = Cursors.Default;
            return;
        }

        string column = _grid.Columns[e.ColumnIndex].Name;
        _grid.Cursor = column == ColState || column == ColItem
            ? Cursors.Hand
            : Cursors.Default;
    }

    private void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Y or Keys.N && !e.Control)
        {
            ReviewSelected(accurate: e.KeyCode == Keys.Y, advance: true);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Space)
        {
            ReviewSelected(toggle: true, advance: false);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Left)
        {
            CollapseCurrent();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Right)
        {
            ExpandCurrent();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.F3)
        {
            JumpToNextReview();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void Grid_Paint(object? sender, PaintEventArgs e)
    {
        if (_visible.Count > 0)
        {
            return;
        }

        string message = _rows.Length == 0
            ? "Import a JSON file to start reviewing matches."
            : "No rows match the current filters.";

        TextRenderer.DrawText(
            e.Graphics,
            message,
            Font,
            _grid.ClientRectangle,
            DarkMode.TextDisabled,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private void ToggleExpanded(OutlineRow outline)
    {
        if (outline.Kind == OutlineKind.Sport)
        {
            SportNode sport = _sports[outline.SportIndex];
            sport.Expanded = !sport.Expanded;
            if (sport.Expanded)
            {
                EnsureFiles(sport);
            }
        }
        else if (outline.Kind == OutlineKind.File)
        {
            FileNode file = _sports[outline.SportIndex].Files[outline.FileIndex];
            file.Expanded = !file.Expanded;
        }
        else
        {
            return;
        }

        ApplyFilterAndRebuild(keepCurrent: true);
    }

    private void ExpandCurrent()
    {
        OutlineRow? current = GetCurrentOutline();
        if (current == null)
        {
            return;
        }

        OutlineRow outline = current.Value;
        if (outline.Kind == OutlineKind.Sport && !_sports[outline.SportIndex].Expanded)
        {
            ToggleExpanded(outline);
        }
        else if (outline.Kind == OutlineKind.File &&
                 !_sports[outline.SportIndex].Files[outline.FileIndex].Expanded)
        {
            ToggleExpanded(outline);
        }
    }

    private void CollapseCurrent()
    {
        OutlineRow? current = GetCurrentOutline();
        if (current == null)
        {
            return;
        }

        OutlineRow outline = current.Value;

        if (outline.Kind == OutlineKind.Match)
        {
            _sports[outline.SportIndex].Files[outline.FileIndex].Expanded = false;
            ApplyFilterAndRebuild(keepCurrent: false);
            SelectGroup(OutlineKind.File, outline.SportIndex, outline.FileIndex);
            return;
        }

        if (outline.Kind == OutlineKind.File &&
            _sports[outline.SportIndex].Files[outline.FileIndex].Expanded)
        {
            ToggleExpanded(outline);
            return;
        }

        if (outline.Kind == OutlineKind.File)
        {
            _sports[outline.SportIndex].Expanded = false;
            ApplyFilterAndRebuild(keepCurrent: false);
            SelectGroup(OutlineKind.Sport, outline.SportIndex, -1);
            return;
        }

        if (outline.Kind == OutlineKind.Sport && _sports[outline.SportIndex].Expanded)
        {
            ToggleExpanded(outline);
        }
    }

    private void CollapseAll()
    {
        foreach (SportNode sport in _sports)
        {
            sport.Expanded = false;
            foreach (FileNode file in sport.Files)
            {
                file.Expanded = false;
            }
        }

        ApplyFilterAndRebuild(keepCurrent: false);
        if (_visible.Count > 0)
        {
            SelectVisibleRow(0);
        }
    }

    private void ReviewSelected(bool accurate = false, bool toggle = false, bool advance = false)
    {
        List<int> selected = GetSelectedVisibleRows();
        if (selected.Count == 0)
        {
            return;
        }

        OutlineRow first = _visible[selected[0]];

        foreach (int visibleIndex in selected)
        {
            ReviewOutline(_visible[visibleIndex], accurate, toggle);
        }

        RefreshFilterCounts();
        RebuildVisible();
        UpdateStats();

        int keep = FindVisibleIndex(first);
        if (keep < 0)
        {
            keep = Math.Min(selected[0], _visible.Count - 1);
        }

        if (!advance)
        {
            if (keep >= 0)
            {
                SelectVisibleRow(keep);
            }

            return;
        }

        int next = keep + 1;
        if (next < _visible.Count)
        {
            SelectVisibleRow(next);
        }
        else if (keep >= 0)
        {
            SelectVisibleRow(keep);
        }
    }

    private void ReviewOutline(
        OutlineRow outline,
        bool accurate,
        bool toggle)
    {
        string? fileFilter = GetSelectedFile();
        string status = _cboStatus.SelectedItem as string ?? StatusAll;

        if (outline.Kind == OutlineKind.Match)
        {
            bool value = toggle ? !_rows[outline.RowIndex].IsAccurate : accurate;
            SetRowAccuracy(outline.RowIndex, value);
            return;
        }

        if (outline.Kind == OutlineKind.File)
        {
            FileNode file = _sports[outline.SportIndex].Files[outline.FileIndex];
            bool value = toggle ? file.FilteredAccurate != file.FilteredTotal : accurate;
            foreach (int rowIndex in file.RowIndices)
            {
                if (MatchesRow(_rows[rowIndex], fileFilter, status))
                {
                    SetRowAccuracy(rowIndex, value);
                }
            }

            return;
        }

        SportNode sport = _sports[outline.SportIndex];
        bool sportValue = toggle ? sport.FilteredAccurate != sport.FilteredTotal : accurate;
        foreach (int rowIndex in sport.RowIndices)
        {
            if (MatchesRow(_rows[rowIndex], fileFilter, status))
            {
                SetRowAccuracy(rowIndex, sportValue);
            }
        }
    }

    private void SetRowAccuracy(int rowIndex, bool accurate)
    {
        AnalysisTableRow row = _rows[rowIndex];
        if (row.IsAccurate == accurate)
        {
            return;
        }

        row.IsAccurate = accurate;
        int delta = accurate ? 1 : -1;

        SportNode sport = _sports[_sportIndexByRow[rowIndex]];
        sport.Accurate += delta;
        sport.FilteredAccurate += delta;

        int fileIndex = _fileIndexByRow[rowIndex];
        if (fileIndex >= 0 && fileIndex < sport.Files.Count)
        {
            FileNode file = sport.Files[fileIndex];
            file.Accurate += delta;
            file.FilteredAccurate += delta;
        }
    }

    private void ReviewAllFiltered(bool accurate)
    {
        if (_rows.Length == 0)
        {
            return;
        }

        string? sportFilter = GetSelectedSport();
        string? fileFilter = GetSelectedFile();
        string status = _cboStatus.SelectedItem as string ?? StatusAll;

        RunPossiblyExpensive(() =>
        {
            foreach (SportNode sport in _sports)
            {
                if (sportFilter != null &&
                    !string.Equals(sport.Name, sportFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (int rowIndex in sport.RowIndices)
                {
                    if (MatchesRow(_rows[rowIndex], fileFilter, status))
                    {
                        SetRowAccuracy(rowIndex, accurate);
                    }
                }
            }

            RefreshFilterCounts();
            RebuildVisible();
        });

        UpdateStats();
    }

    private void JumpToNextReview()
    {
        string? fileFilter = GetSelectedFile();
        string status = _cboStatus.SelectedItem as string ?? StatusAll;

        OutlineRow? current = GetCurrentOutline();
        bool passedCurrent = current == null;
        int currentRow = current?.Kind == OutlineKind.Match ? current.Value.RowIndex : -1;

        for (int sportIndex = 0; sportIndex < _sports.Count; sportIndex++)
        {
            SportNode sport = _sports[sportIndex];
            if (sport.FilteredTotal == 0)
            {
                continue;
            }

            foreach (int rowIndex in sport.RowIndices)
            {
                if (!MatchesRow(_rows[rowIndex], fileFilter, status) ||
                    _rows[rowIndex].IsAccurate)
                {
                    continue;
                }

                if (!passedCurrent)
                {
                    bool reached;

                    if (current == null)
                    {
                        reached = true;
                    }
                    else if (current.Value.Kind == OutlineKind.Sport)
                    {
                        reached = sportIndex == current.Value.SportIndex;
                    }
                    else if (current.Value.Kind == OutlineKind.File)
                    {
                        reached = sportIndex == current.Value.SportIndex &&
                                  _fileIndexByRow[rowIndex] == current.Value.FileIndex;
                    }
                    else
                    {
                        reached = rowIndex == currentRow;
                    }

                    if (!reached)
                    {
                        continue;
                    }

                    passedCurrent = true;

                    if (current?.Kind == OutlineKind.Match)
                    {
                        continue;
                    }
                }

                sport.Expanded = true;
                EnsureFiles(sport);
                int fileIndex = _fileIndexByRow[rowIndex];
                if (fileIndex >= 0)
                {
                    sport.Files[fileIndex].Expanded = true;
                }

                RebuildVisible();
                int visibleIndex = fileIndex >= 0
                    ? FindVisibleIndex(OutlineRow.Match(sportIndex, fileIndex, rowIndex))
                    : -1;

                if (visibleIndex >= 0)
                {
                    SelectVisibleRow(visibleIndex);
                }

                UpdateStats();
                return;
            }
        }
    }

    private List<int> GetSelectedVisibleRows()
    {
        var selected = new List<int>();
        int rowIndex = _grid.Rows.GetFirstRow(DataGridViewElementStates.Selected);

        while (rowIndex >= 0)
        {
            if (rowIndex < _visible.Count)
            {
                selected.Add(rowIndex);
            }

            rowIndex = _grid.Rows.GetNextRow(rowIndex, DataGridViewElementStates.Selected);
        }

        if (selected.Count == 0 &&
            _grid.CurrentCell != null &&
            _grid.CurrentCell.RowIndex >= 0 &&
            _grid.CurrentCell.RowIndex < _visible.Count)
        {
            selected.Add(_grid.CurrentCell.RowIndex);
        }

        return selected;
    }

    private OutlineRow? GetCurrentOutline()
    {
        int rowIndex = _grid.CurrentCell?.RowIndex ?? -1;
        if (rowIndex < 0 || rowIndex >= _visible.Count)
        {
            return null;
        }

        return _visible[rowIndex];
    }

    private int FindVisibleIndex(OutlineRow target)
    {
        for (int i = 0; i < _visible.Count; i++)
        {
            OutlineRow row = _visible[i];
            if (row.Kind == target.Kind &&
                row.SportIndex == target.SportIndex &&
                row.FileIndex == target.FileIndex &&
                row.RowIndex == target.RowIndex)
            {
                return i;
            }
        }

        return -1;
    }

    private void SelectGroup(OutlineKind kind, int sportIndex, int fileIndex)
    {
        var target = new OutlineRow
        {
            Kind = kind,
            SportIndex = sportIndex,
            FileIndex = fileIndex,
            RowIndex = -1
        };

        int found = FindVisibleIndex(target);
        if (found >= 0)
        {
            SelectVisibleRow(found);
        }
    }

    private void SelectVisibleRow(int visibleIndex)
    {
        if (visibleIndex < 0 || visibleIndex >= _visible.Count || _grid.RowCount == 0)
        {
            return;
        }

        _grid.ClearSelection();
        _grid.CurrentCell = _grid.Rows[visibleIndex].Cells[ColItem];
        _grid.Rows[visibleIndex].Selected = true;

        try
        {
            int window = Math.Max(_grid.DisplayedRowCount(false) / 3, 2);
            _grid.FirstDisplayedScrollingRowIndex = Math.Max(0, visibleIndex - window);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void UpdateStats()
    {
        int total = 0;
        int accurate = 0;

        foreach (SportNode sport in _sports)
        {
            total += sport.FilteredTotal;
            accurate += sport.FilteredAccurate;
        }

        int remaining = total - accurate;
        string treeNote = _rows.Length == 0
            ? string.Empty
            : $"   ·   {_visible.Count:N0} tree rows";

        _lblStats.Text =
            $"{total:N0} matches   ·   {accurate:N0} accurate   ·   {remaining:N0} to review{treeNote}";
    }

    private void RebuildSportFilter()
    {
        _updatingUi = true;
        _cboSport.BeginUpdate();
        _cboSport.Items.Clear();
        _cboSport.Items.Add(AllSports);

        foreach (SportNode sport in _sports)
        {
            _cboSport.Items.Add(sport.Name);
        }

        _cboSport.SelectedIndex = 0;
        _cboSport.EndUpdate();
        _updatingUi = false;
    }

    private void RebuildFileFilter()
    {
        string? selectedSport = GetSelectedSport();
        var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        bool capped = false;

        foreach (SportNode sport in _sports)
        {
            if (selectedSport != null &&
                !string.Equals(sport.Name, selectedSport, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (sport.FilesBuilt)
            {
                foreach (FileNode file in sport.Files)
                {
                    files.Add(file.Name);
                    if (files.Count > MaxFileFilterItems)
                    {
                        capped = true;
                        break;
                    }
                }
            }
            else
            {
                foreach (int rowIndex in sport.RowIndices)
                {
                    files.Add(_rows[rowIndex].File);
                    if (files.Count > MaxFileFilterItems)
                    {
                        capped = true;
                        break;
                    }
                }
            }

            if (capped)
            {
                break;
            }
        }

        _updatingUi = true;
        _cboFile.BeginUpdate();
        _cboFile.Items.Clear();
        _cboFile.Items.Add(AllFiles);

        if (!capped)
        {
            foreach (string file in files)
            {
                _cboFile.Items.Add(file);
            }

            _cboFile.Enabled = true;
        }
        else
        {
            _cboFile.Enabled = false;
        }

        _cboFile.SelectedIndex = 0;
        _cboFile.EndUpdate();
        _updatingUi = false;
    }

    private string? GetSelectedSport()
    {
        string? selected = _cboSport.SelectedItem as string;
        return string.IsNullOrEmpty(selected) || selected == AllSports ? null : selected;
    }

    private string? GetSelectedFile()
    {
        if (!_cboFile.Enabled)
        {
            return null;
        }

        string? selected = _cboFile.SelectedItem as string;
        return string.IsNullOrEmpty(selected) || selected == AllFiles ? null : selected;
    }

    private void RunPossiblyExpensive(Action action)
    {
        bool showWait = _rows.Length >= WaitCursorThreshold;
        if (showWait)
        {
            UseWaitCursor = true;
            Cursor.Current = Cursors.WaitCursor;
        }

        try
        {
            action();
        }
        finally
        {
            if (showWait)
            {
                UseWaitCursor = false;
                Cursor.Current = Cursors.Default;
            }
        }
    }

    private void StyleTextBox(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = DarkMode.ElevatedSurface;
        textBox.ForeColor = DarkMode.TextPrimary;
        textBox.Margin = Padding.Empty;
    }

    private static void StyleCombo(ComboBox comboBox, int width)
    {
        comboBox.Width = width;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.BackColor = DarkMode.ElevatedSurface;
        comboBox.ForeColor = DarkMode.TextPrimary;
        comboBox.Margin = Padding.Empty;
    }

    private static void StyleActionButton(
        Button button,
        string text,
        Color backColor,
        Color foreColor,
        int width)
    {
        button.Text = text;
        button.AutoSize = false;
        button.Width = width;
        button.Height = 28;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(0, 0, 8, 0);
        button.TabStop = false;
        button.UseVisualStyleBackColor = false;
    }

    private enum OutlineKind
    {
        Sport,
        File,
        Match
    }

    private struct OutlineRow
    {
        public OutlineKind Kind;
        public int SportIndex;
        public int FileIndex;
        public int RowIndex;

        public static OutlineRow Sport(int sportIndex) =>
            new() { Kind = OutlineKind.Sport, SportIndex = sportIndex, FileIndex = -1, RowIndex = -1 };

        public static OutlineRow File(int sportIndex, int fileIndex) =>
            new() { Kind = OutlineKind.File, SportIndex = sportIndex, FileIndex = fileIndex, RowIndex = -1 };

        public static OutlineRow Match(int sportIndex, int fileIndex, int rowIndex) =>
            new() { Kind = OutlineKind.Match, SportIndex = sportIndex, FileIndex = fileIndex, RowIndex = rowIndex };
    }

    private sealed class SportNode
    {
        public string Name { get; set; } = string.Empty;
        public bool Expanded { get; set; }
        public bool FilesBuilt { get; set; }
        public int Total { get; set; }
        public int Accurate { get; set; }
        public int FilteredTotal { get; set; }
        public int FilteredAccurate { get; set; }
        public List<int> RowIndices { get; } = new();
        public List<FileNode> Files { get; } = new();
    }

    private sealed class FileNode
    {
        public string Name { get; set; } = string.Empty;
        public bool Expanded { get; set; }
        public int Accurate { get; set; }
        public int FilteredTotal { get; set; }
        public int FilteredAccurate { get; set; }
        public List<int> RowIndices { get; } = new();
    }
}
