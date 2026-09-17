using FLDSMDFR.Core.Models;
using FLDSMDFR.Core.Services;
using Themes;

namespace AnalysisReviewControl;

public partial class AnalysisReviewControl : ReviewUserControl
{
    private const string ColSelect = "Select";
    private const string ColState = "State";
    private const string ColItem = "Item";
    private const string ColLine = "Line";
    private const string ColFile = "File";
    private const string ColSummary = "Summary";

    private const string AllSports = "(All sports)";
    private const string AllFiles = "(All files)";
    private const string StatusAll = "All rows";
    private const string StatusReview = "Needs review";
    private const string StatusAccurate = "Accurate";
    private const string StatusDenied = "Denied";

    private const int MaxFileFilterItems = 400;
    private const int WaitCursorThreshold = 200_000;

    private AnalysisTableRow[] _rows = Array.Empty<AnalysisTableRow>();
    private readonly List<SportNode> _sports = new();
    private readonly List<OutlineRow> _visible = new();
    private int[] _sportIndexByRow = Array.Empty<int>();
    private int[] _fileIndexByRow = Array.Empty<int>();
    private CloneKey[] _cloneKeyByRow = Array.Empty<CloneKey>();
    private bool[] _countsInTree = Array.Empty<bool>();
    private readonly Dictionary<CloneKey, int[]> _clonesByKey = new();

    private readonly RoundedCardPanel _card = new();
    private readonly Panel _pnlToolbar = new();
    private readonly Panel _pnlStatus = new();
    private readonly TextBox _txtSearch = new();
    private readonly ThemedDropDown _cboSport = new();
    private readonly ThemedDropDown _cboFile = new();
    private readonly ThemedDropDown _cboStatus = new();
    private readonly Button _btnConfirm = new();
    private readonly Button _btnDeny = new();
    private readonly Button _btnUndo = new();
    private readonly Button _btnNext = new();
    private readonly Button _btnCollapse = new();
    private readonly Label _lblStats = new();
    private readonly Label _lblHelp = new();
    private readonly ReviewGrid _grid = new();
    private readonly System.Windows.Forms.Timer _searchDebounce = new();
    private readonly Stack<List<(int RowIndex, ReviewDecision Previous)>> _undoStack = new();
    private List<(int RowIndex, ReviewDecision Previous)>? _pendingUndo;

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
        AutoScaleMode = AutoScaleMode.None;
        BackColor = DarkMode.Background;
        Padding = new Padding(4);
        Font = CreateOwnedFont("Segoe UI", 10.5f);
        _sportFont = CreateOwnedFont("Segoe UI", 11.5f, FontStyle.Bold);
        _fileFont = CreateOwnedFont("Segoe UI", 10.5f);

        ConfigureToolbar();
        ConfigureStatusBar();
        ConfigureGrid();
        ConfigureCard();
        WireEvents();
        UpdateStats();
    }

    private void ConfigureToolbar()
    {
        _pnlToolbar.Dock = DockStyle.Top;
        _pnlToolbar.Height = 86;
        _pnlToolbar.BackColor = DarkMode.Surface;
        _pnlToolbar.Padding = new Padding(14, 10, 14, 8);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = DarkMode.Surface
        };

        _txtSearch.PlaceholderText = "Search sport, file, word, or line";
        StyleTextBox(_txtSearch);

        StyleCombo(_cboSport, 168);
        StyleCombo(_cboFile, 218);
        StyleCombo(_cboStatus, 148);

        _cboStatus.Items.AddRange(new object[]
        {
            StatusAll,
            StatusReview,
            StatusAccurate,
            StatusDenied
        });
        _cboStatus.SelectedIndex = 0;
        _cboSport.Items.Add(AllSports);
        _cboSport.SelectedIndex = 0;
        _cboFile.Items.Add(AllFiles);
        _cboFile.SelectedIndex = 0;

        StyleActionButton(_btnConfirm, "Confirm  Y", DarkMode.Success, DarkMode.Background, 118);
        StyleActionButton(_btnDeny, "Deny  N", BlendErrorButton(), DarkMode.Error, 104);
        StyleActionButton(_btnUndo, "Undo  Z", DarkMode.ElevatedSurface, DarkMode.TextPrimary, 92);
        StyleActionButton(_btnNext, "Next  F3", DarkMode.ElevatedSurface, DarkMode.TextPrimary, 100);
        StyleActionButton(_btnCollapse, "Collapse all", DarkMode.ElevatedSurface, DarkMode.TextPrimary, 118);
        UpdateUndoButton();

        flow.Controls.Add(Wrap(CreateCaption("Search"), new ModernFieldHost(_txtSearch, 268, searchIcon: true)));
        flow.Controls.Add(Wrap(CreateCaption("Sport"), _cboSport));
        flow.Controls.Add(Wrap(CreateCaption("File"), _cboFile));
        flow.Controls.Add(Wrap(CreateCaption("View"), _cboStatus));
        flow.Controls.Add(Wrap(CreateCaption("Review"), CreateButtonRow()));

        _pnlToolbar.Controls.Add(flow);
    }

    private static Color BlendErrorButton()
    {
        return Color.FromArgb(
            (DarkMode.Surface.R * 4 + DarkMode.Error.R) / 5,
            (DarkMode.Surface.G * 4 + DarkMode.Error.G) / 5,
            (DarkMode.Surface.B * 4 + DarkMode.Error.B) / 5);
    }

    private Control CreateButtonRow()
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Width = 580,
            Height = 36,
            BackColor = DarkMode.Surface,
            Margin = Padding.Empty
        };

        panel.Controls.Add(_btnConfirm);
        panel.Controls.Add(_btnDeny);
        panel.Controls.Add(_btnUndo);
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
            Height = 58,
            BackColor = DarkMode.Surface,
            Margin = new Padding(0, 0, 14, 0)
        };

        caption.Dock = DockStyle.Top;
        caption.Height = 16;
        editor.Dock = DockStyle.Bottom;
        editor.Height = 36;
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
        _lblHelp.Text = "Click a match to open in VS Code   Ctrl+Z undo   Y confirm   N deny   Space cycle";

        _pnlStatus.Controls.Add(_lblHelp);
        _pnlStatus.Controls.Add(_lblStats);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.Font = CreateOwnedFont("Segoe UI", 10.5f);
        _grid.ColumnHeadersDefaultCellStyle.Font = CreateOwnedFont("Segoe UI", 9f, FontStyle.Bold);
        _grid.RowTemplate.Height = 50;

        _grid.Columns.Add(CreateTextColumn(ColSelect, "", 48, 48));
        _grid.Columns.Add(CreateTextColumn(ColState, "Status", 120, 108));
        _grid.Columns.Add(CreateTextColumn(ColItem, "Item", 280, 140));
        _grid.Columns.Add(CreateTextColumn(ColLine, "Line", 72, 56));
        _grid.Columns.Add(CreateTextColumn(ColFile, "File", 180, 100));
        _grid.Columns.Add(CreateTextColumn(ColSummary, "Summary", 240, 140));

        _grid.Columns[ColSelect].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _grid.Columns[ColSelect].Width = 48;
        _grid.Columns[ColSelect].FillWeight = 1;
        _grid.Columns[ColSelect].Resizable = DataGridViewTriState.False;
        _grid.Columns[ColState].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _grid.Columns[ColState].Width = 120;
        _grid.Columns[ColState].FillWeight = 1;
        _grid.Columns[ColLine].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _grid.Columns[ColLine].Width = 72;
        _grid.Columns[ColLine].FillWeight = 1;
        _grid.Columns[ColItem].FillWeight = 40;
        _grid.Columns[ColFile].FillWeight = 22;
        _grid.Columns[ColSummary].FillWeight = 30;
    }

    private void ConfigureCard()
    {
        _card.Dock = DockStyle.Fill;
        _card.Padding = new Padding(18);
        _card.BackColor = Color.Transparent;
        _card.BackdropColor = DarkMode.Background;
        _card.FillColor = DarkMode.Surface;

        _card.Controls.Add(_grid);
        _card.Controls.Add(_pnlToolbar);
        _card.Controls.Add(_pnlStatus);
        _grid.BringToFront();

        Controls.Add(_card);
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

        _btnConfirm.Click += (_, _) => ReviewSelected(decision: ReviewDecision.Accurate, advance: true);
        _btnDeny.Click += (_, _) => ReviewSelected(decision: ReviewDecision.Denied, advance: true);
        _btnUndo.Click += (_, _) => UndoLast();
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
        if (keyData == (Keys.Control | Keys.Z) && !_txtSearch.ContainsFocus)
        {
            UndoLast();
            return true;
        }

        if (_grid.Focused || _grid.ContainsFocus)
        {
            if (keyData == (Keys.Control | Keys.A))
            {
                _grid.SelectAllVisibleRows();
                return true;
            }

            if (keyData == (Keys.Control | Keys.Y))
            {
                ReviewAllFiltered(ReviewDecision.Accurate);
                return true;
            }

            if (keyData == (Keys.Control | Keys.N))
            {
                ReviewAllFiltered(ReviewDecision.Denied);
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    public void LoadData(IEnumerable<AnalysisTableRow> rows)
    {
        _rows = rows as AnalysisTableRow[] ?? rows.ToArray();
        _undoStack.Clear();
        _pendingUndo = null;
        BuildSportIndex();
        RebuildSportFilter();
        RebuildFileFilter();
        ApplyFilterAndRebuild(keepCurrent: false);
        UpdateUndoButton();

        if (_visible.Count > 0)
        {
            SelectVisibleRow(0);
            _grid.Focus();
        }
    }

    private void BuildSportIndex()
    {
        _sports.Clear();
        _clonesByKey.Clear();
        _sportIndexByRow = new int[_rows.Length];
        _fileIndexByRow = new int[_rows.Length];
        _cloneKeyByRow = new CloneKey[_rows.Length];
        _countsInTree = new bool[_rows.Length];
        Array.Fill(_sportIndexByRow, -1);
        Array.Fill(_fileIndexByRow, -1);

        var cloneLists = new Dictionary<CloneKey, List<int>>();
        for (int i = 0; i < _rows.Length; i++)
        {
            CloneKey key = CloneKey.From(_rows[i]);
            _cloneKeyByRow[i] = key;
            if (!cloneLists.TryGetValue(key, out List<int>? members))
            {
                members = new List<int>();
                cloneLists.Add(key, members);
            }

            members.Add(i);
        }

        foreach (KeyValuePair<CloneKey, List<int>> pair in cloneLists)
        {
            int[] members = pair.Value.ToArray();
            _clonesByKey[pair.Key] = members;
            UnifyCloneDecision(members);
        }

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

            _sportIndexByRow[i] = -1;
            _fileIndexByRow[i] = -1;

            CloneKey key = _cloneKeyByRow[i];
            if (sport.SeenCloneKeys.Add(key))
            {
                sport.RowIndices.Add(i);
                sport.Total++;
                _countsInTree[i] = true;
                AddDecision(sport, row.Decision, 1);
            }
        }

        _sports.AddRange(sports.Values.OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase));

        for (int sportIndex = 0; sportIndex < _sports.Count; sportIndex++)
        {
            SportNode sport = _sports[sportIndex];
            foreach (int rowIndex in sport.RowIndices)
            {
                _sportIndexByRow[rowIndex] = sportIndex;
            }

            foreach (int rowIndex in GetSportCloneRows(sport))
            {
                _sportIndexByRow[rowIndex] = sportIndex;
            }
        }
    }

    private void UnifyCloneDecision(int[] members)
    {
        if (members.Length <= 1)
        {
            return;
        }

        ReviewDecision decision = _rows[members[0]].Decision;
        for (int i = 1; i < members.Length; i++)
        {
            if (_rows[members[i]].Decision != decision)
            {
                decision = ReviewDecision.Pending;
                break;
            }
        }

        foreach (int rowIndex in members)
        {
            _rows[rowIndex].Decision = decision;
        }
    }

    private IEnumerable<int> GetSportCloneRows(SportNode sport)
    {
        var seen = new HashSet<int>();
        foreach (int representative in sport.RowIndices)
        {
            foreach (int clone in GetClones(representative))
            {
                if (string.Equals(_rows[clone].Sport, sport.Name, StringComparison.OrdinalIgnoreCase) &&
                    seen.Add(clone))
                {
                    yield return clone;
                }
            }
        }
    }

    private int[] GetClones(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _cloneKeyByRow.Length)
        {
            return Array.Empty<int>();
        }

        return _clonesByKey.TryGetValue(_cloneKeyByRow[rowIndex], out int[]? members)
            ? members
            : new[] { rowIndex };
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
            AddDecision(file, row.Decision, 1);
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
                sport.FilteredDenied = 0;
                continue;
            }

            if (!scanRows)
            {
                sport.FilteredTotal = sport.Total;
                sport.FilteredAccurate = sport.Accurate;
                sport.FilteredDenied = sport.Denied;
                if (sport.FilesBuilt)
                {
                    foreach (FileNode file in sport.Files)
                    {
                        file.FilteredTotal = file.RowIndices.Count;
                        file.FilteredAccurate = file.Accurate;
                        file.FilteredDenied = file.Denied;
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
                file.FilteredDenied = file.Denied;
                continue;
            }

            int total = 0;
            int accurate = 0;
            int denied = 0;
            foreach (int rowIndex in file.RowIndices)
            {
                if (!MatchesRow(rowIndex, fileFilter, status))
                {
                    continue;
                }

                total++;
                CountDecision(_rows[rowIndex].Decision, ref accurate, ref denied);
            }

            file.FilteredTotal = total;
            file.FilteredAccurate = accurate;
            file.FilteredDenied = denied;
        }
    }

    private void CountFiltered(SportNode sport, string? fileFilter, string status)
    {
        int total = 0;
        int accurate = 0;
        int denied = 0;

        foreach (int rowIndex in sport.RowIndices)
        {
            AnalysisTableRow row = _rows[rowIndex];
            if (!MatchesRow(rowIndex, fileFilter, status))
            {
                continue;
            }

            total++;
            CountDecision(row.Decision, ref accurate, ref denied);
        }

        sport.FilteredTotal = total;
        sport.FilteredAccurate = accurate;
        sport.FilteredDenied = denied;

        if (sport.FilesBuilt)
        {
            RefreshFileFilterCounts(sport);
        }
    }

    private bool MatchesRow(int rowIndex, string? fileFilter, string status)
    {
        AnalysisTableRow row = _rows[rowIndex];
        if (status == StatusAccurate && row.Decision != ReviewDecision.Accurate)
        {
            return false;
        }

        if (status == StatusDenied && row.Decision != ReviewDecision.Denied)
        {
            return false;
        }

        if (status == StatusReview && row.Decision != ReviewDecision.Pending)
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

        foreach (int cloneIndex in GetClones(rowIndex))
        {
            AnalysisTableRow clone = _rows[cloneIndex];
            if (Contains(clone.Sport, _searchText) ||
                Contains(clone.Found, _searchText) ||
                Contains(clone.Word, _searchText) ||
                Contains(clone.File, _searchText) ||
                Contains(clone.LineNumber.ToString(), _searchText))
            {
                return true;
            }
        }

        return false;
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

                IEnumerable<int> matches = file.RowIndices
                    .Where(rowIndex => MatchesRow(rowIndex, fileFilter, status))
                    .OrderBy(rowIndex => _rows[rowIndex].LineNumber)
                    .ThenBy(rowIndex => DisplayWord(_rows[rowIndex]), StringComparer.OrdinalIgnoreCase);

                foreach (int rowIndex in matches)
                {
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
            ColSelect => string.Empty,
            ColState => GetStateText(outline),
            ColItem => GetItemText(outline),
            ColLine => GetLineText(outline),
            ColFile => GetFileText(outline),
            ColSummary => GetSummaryText(outline),
            _ => string.Empty
        };
    }

    private string GetStateText(OutlineRow outline)
    {
        if (outline.Kind == OutlineKind.Match)
        {
            return StateLabel(_rows[outline.RowIndex].Decision);
        }

        (int total, int accurate, int denied) = GetCounts(outline);

        if (total == 0)
        {
            return string.Empty;
        }

        if (accurate == total)
        {
            return "Accurate";
        }

        if (denied == total)
        {
            return "Denied";
        }

        if (accurate == 0 && denied == 0)
        {
            return "Review";
        }

        return "Mixed";
    }

    private static string StateLabel(ReviewDecision decision)
    {
        return decision switch
        {
            ReviewDecision.Accurate => "Accurate",
            ReviewDecision.Denied => "Denied",
            _ => "Review"
        };
    }

    private string GetItemText(OutlineRow outline)
    {
        return outline.Kind switch
        {
            OutlineKind.Sport => $"{Glyph(_sports[outline.SportIndex].Expanded)}  {_sports[outline.SportIndex].Name}",
            OutlineKind.File => $"{Glyph(_sports[outline.SportIndex].Files[outline.FileIndex].Expanded)}  {DisplayFileName(_sports[outline.SportIndex].Files[outline.FileIndex].Name)}",
            _ => DisplayWord(_rows[outline.RowIndex])
        };
    }

    private static string DisplayWord(AnalysisTableRow row)
    {
        return string.IsNullOrWhiteSpace(row.Word) ? row.Found : row.Word;
    }

    private static string DisplayFileName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        try
        {
            return Path.GetFileName(path.Replace('/', Path.DirectorySeparatorChar));
        }
        catch (ArgumentException)
        {
            return path;
        }
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

        return DisplayFileName(_rows[outline.RowIndex].File);
    }

    private string GetLineText(OutlineRow outline)
    {
        if (outline.Kind != OutlineKind.Match)
        {
            return string.Empty;
        }

        int line = _rows[outline.RowIndex].LineNumber;
        return line > 0 ? line.ToString() : string.Empty;
    }

    private string GetSummaryText(OutlineRow outline)
    {
        if (outline.Kind == OutlineKind.Match)
        {
            return GetMatchSummary(_rows[outline.RowIndex], outline.RowIndex);
        }

        (int total, int accurate, int denied) = GetCounts(outline);
        return $"{total:N0} matches  ·  {accurate:N0} accurate  ·  {denied:N0} denied";
    }

    private string GetMatchSummary(AnalysisTableRow row, int rowIndex)
    {
        int[] clones = GetClones(rowIndex);
        var terms = clones
            .Select(index => _rows[index].Found)
            .Where(found => !string.IsNullOrWhiteSpace(found))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(found => found, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (terms.Count == 0)
        {
            return row.LineNumber > 0 ? $"line {row.LineNumber}" : string.Empty;
        }

        if (clones.Length == 1 &&
            terms.Count == 1 &&
            string.Equals(terms[0], DisplayWord(row), StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        string termList = string.Join(", ", terms);
        return clones.Length > 1
            ? $"{termList}  ·  {clones.Length} hits"
            : termList;
    }

    private (int total, int accurate, int denied) GetCounts(OutlineRow outline)
    {
        if (outline.Kind == OutlineKind.Sport)
        {
            SportNode sport = _sports[outline.SportIndex];
            return (sport.FilteredTotal, sport.FilteredAccurate, sport.FilteredDenied);
        }

        if (outline.Kind == OutlineKind.File)
        {
            FileNode file = _sports[outline.SportIndex].Files[outline.FileIndex];
            return (file.FilteredTotal, file.FilteredAccurate, file.FilteredDenied);
        }

        ReviewDecision decision = _rows[outline.RowIndex].Decision;
        return (
            1,
            decision == ReviewDecision.Accurate ? 1 : 0,
            decision == ReviewDecision.Denied ? 1 : 0);
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
                "Denied" => DarkMode.Error,
                "Review" => DarkMode.Warning,
                "Mixed" => DarkMode.Secondary,
                _ => DarkMode.TextSecondary
            };
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
        }
        else if (column == ColLine)
        {
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            e.CellStyle.ForeColor = DarkMode.TextSecondary;
            e.CellStyle.SelectionForeColor = DarkMode.TextSecondary;
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

        if ((ModifierKeys & (Keys.Control | Keys.Shift)) != Keys.None)
        {
            return;
        }

        OutlineRow outline = _visible[e.RowIndex];
        string column = _grid.Columns[e.ColumnIndex].Name;

        if (column == ColSelect)
        {
            return;
        }

        if (column == ColState)
        {
            ReviewSelected(toggle: true, advance: false);
            return;
        }

        if (outline.Kind == OutlineKind.Match)
        {
            OpenInVsCode(_rows[outline.RowIndex]);
            return;
        }

        if (outline.Kind == OutlineKind.File && column == ColFile)
        {
            OpenInVsCode(_sports[outline.SportIndex].Files[outline.FileIndex].Name);
            return;
        }

        ToggleExpanded(outline);
    }

    private void Grid_CellMouseEnter(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex < 0)
        {
            _grid.Cursor = Cursors.Default;
            return;
        }

        string column = _grid.Columns[e.ColumnIndex].Name;
        if (e.RowIndex >= 0 && e.RowIndex < _visible.Count)
        {
            OutlineRow outline = _visible[e.RowIndex];
            if (outline.Kind == OutlineKind.Match ||
                (outline.Kind == OutlineKind.File && column == ColFile) ||
                column is ColState or ColSelect)
            {
                _grid.Cursor = Cursors.Hand;
                return;
            }
        }

        _grid.Cursor = column is ColState or ColItem or ColSelect
            ? Cursors.Hand
            : Cursors.Default;
    }

    private void OpenInVsCode(AnalysisTableRow row)
    {
        string highlight = string.IsNullOrWhiteSpace(row.Word) ? row.Found : row.Word;
        OpenInVsCode(row.File, highlight, row.LineNumber);
    }

    private void OpenInVsCode(string path, string? searchText = null, int lineNumber = 0)
    {
        Form? form = FindForm();
        try
        {
            int selectLength = VsCodeLauncher.Open(path, searchText, lineNumber);
            ScheduleHighlightThenRestore(form, selectLength);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Open in VS Code",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void RestoreReviewFocus(Form? form)
    {
        if (form is { IsHandleCreated: true, IsDisposed: false })
        {
            NativeMethods.SetForegroundWindow(form.Handle);
            form.Activate();
        }

        if (_grid.CanFocus)
        {
            _grid.Focus();
        }
    }

    private void ScheduleHighlightThenRestore(Form? form, int selectLength)
    {
        int attempts = 0;
        var waitForCode = new System.Windows.Forms.Timer { Interval = 80 };
        waitForCode.Tick += (_, _) =>
        {
            attempts++;
            bool ready = NativeMethods.IsVsCodeForeground();
            if (!ready && attempts < 12)
            {
                return;
            }

            waitForCode.Stop();
            waitForCode.Dispose();

            if (ready && selectLength > 0)
            {
                NativeMethods.SelectNextCharacters(selectLength);
            }

            var restore = new System.Windows.Forms.Timer { Interval = 120 };
            restore.Tick += (_, _) =>
            {
                restore.Stop();
                restore.Dispose();
                RestoreReviewFocus(form);
            };
            restore.Start();
        };
        waitForCode.Start();
    }

    private void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Control && !e.Shift)
        {
            OutlineRow? current = GetCurrentOutline();
            if (current?.Kind == OutlineKind.Match)
            {
                OpenInVsCode(_rows[current.Value.RowIndex]);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (current?.Kind == OutlineKind.File)
            {
                OpenInVsCode(_sports[current.Value.SportIndex].Files[current.Value.FileIndex].Name);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
        }

        if (e.KeyCode is Keys.Y or Keys.N && !e.Control)
        {
            ReviewSelected(
                decision: e.KeyCode == Keys.Y
                    ? ReviewDecision.Accurate
                    : ReviewDecision.Denied,
                advance: true);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Z && e.Control)
        {
            UndoLast();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Space && !e.Control && !e.Shift)
        {
            ReviewSelected(toggle: true, advance: false);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Left && !e.Control && !e.Shift)
        {
            CollapseCurrent();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Right && !e.Control && !e.Shift)
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

    private void ReviewSelected(
        ReviewDecision decision = ReviewDecision.Pending,
        bool toggle = false,
        bool advance = false)
    {
        List<int> selected = GetSelectedVisibleRows();
        if (selected.Count == 0)
        {
            return;
        }

        OutlineRow first = _visible[selected[0]];

        BeginUndoBatch();
        foreach (int visibleIndex in selected)
        {
            ReviewOutline(_visible[visibleIndex], decision, toggle);
        }
        CommitUndoBatch();

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
        ReviewDecision decision,
        bool toggle)
    {
        string? fileFilter = GetSelectedFile();
        string status = _cboStatus.SelectedItem as string ?? StatusAll;

        if (outline.Kind == OutlineKind.Match)
        {
            ReviewDecision value = toggle
                ? NextDecision(_rows[outline.RowIndex].Decision)
                : decision;
            SetRowDecision(outline.RowIndex, value);
            return;
        }

        if (outline.Kind == OutlineKind.File)
        {
            FileNode file = _sports[outline.SportIndex].Files[outline.FileIndex];
            ReviewDecision value = toggle
                ? NextGroupDecision(file.FilteredTotal, file.FilteredAccurate, file.FilteredDenied)
                : decision;

            foreach (int rowIndex in file.RowIndices)
            {
                if (MatchesRow(rowIndex, fileFilter, status))
                {
                    SetRowDecision(rowIndex, value);
                }
            }

            return;
        }

        SportNode sport = _sports[outline.SportIndex];
        ReviewDecision sportValue = toggle
            ? NextGroupDecision(sport.FilteredTotal, sport.FilteredAccurate, sport.FilteredDenied)
            : decision;

        foreach (int rowIndex in sport.RowIndices)
        {
            if (MatchesRow(rowIndex, fileFilter, status))
            {
                SetRowDecision(rowIndex, sportValue);
            }
        }
    }

    private static ReviewDecision NextDecision(ReviewDecision current)
    {
        return current switch
        {
            ReviewDecision.Pending => ReviewDecision.Accurate,
            ReviewDecision.Accurate => ReviewDecision.Denied,
            _ => ReviewDecision.Pending
        };
    }

    private static ReviewDecision NextGroupDecision(int total, int accurate, int denied)
    {
        if (accurate == total && total > 0)
        {
            return ReviewDecision.Denied;
        }

        if (denied == total && total > 0)
        {
            return ReviewDecision.Pending;
        }

        return ReviewDecision.Accurate;
    }

    private void SetRowDecision(int rowIndex, ReviewDecision decision)
    {
        foreach (int cloneIndex in GetClones(rowIndex))
        {
            AnalysisTableRow row = _rows[cloneIndex];
            if (row.Decision == decision)
            {
                continue;
            }

            ReviewDecision previous = row.Decision;
            if (_pendingUndo != null)
            {
                _pendingUndo.Add((cloneIndex, previous));
            }

            row.Decision = decision;

            if (!_countsInTree[cloneIndex])
            {
                continue;
            }

            int sportIndex = _sportIndexByRow[cloneIndex];
            if (sportIndex < 0 || sportIndex >= _sports.Count)
            {
                continue;
            }

            SportNode sport = _sports[sportIndex];
            AddDecision(sport, previous, -1);
            AddDecision(sport, decision, 1);

            int fileIndex = _fileIndexByRow[cloneIndex];
            if (fileIndex >= 0 && fileIndex < sport.Files.Count)
            {
                FileNode file = sport.Files[fileIndex];
                AddDecision(file, previous, -1);
                AddDecision(file, decision, 1);
            }
        }
    }

    private static void AddDecision(SportNode sport, ReviewDecision decision, int delta)
    {
        if (decision == ReviewDecision.Accurate)
        {
            sport.Accurate += delta;
            sport.FilteredAccurate += delta;
        }
        else if (decision == ReviewDecision.Denied)
        {
            sport.Denied += delta;
            sport.FilteredDenied += delta;
        }
    }

    private static void AddDecision(FileNode file, ReviewDecision decision, int delta)
    {
        if (decision == ReviewDecision.Accurate)
        {
            file.Accurate += delta;
            file.FilteredAccurate += delta;
        }
        else if (decision == ReviewDecision.Denied)
        {
            file.Denied += delta;
            file.FilteredDenied += delta;
        }
    }

    private static void CountDecision(ReviewDecision decision, ref int accurate, ref int denied)
    {
        if (decision == ReviewDecision.Accurate)
        {
            accurate++;
        }
        else if (decision == ReviewDecision.Denied)
        {
            denied++;
        }
    }

    private void BeginUndoBatch()
    {
        _pendingUndo = new List<(int RowIndex, ReviewDecision Previous)>();
    }

    private void CommitUndoBatch()
    {
        if (_pendingUndo != null && _pendingUndo.Count > 0)
        {
            _undoStack.Push(_pendingUndo);
        }

        _pendingUndo = null;
        UpdateUndoButton();
    }

    private void UndoLast()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        List<(int RowIndex, ReviewDecision Previous)> batch = _undoStack.Pop();
        for (int i = batch.Count - 1; i >= 0; i--)
        {
            SetRowDecision(batch[i].RowIndex, batch[i].Previous);
        }

        RefreshFilterCounts();
        RebuildVisible();
        UpdateStats();
        UpdateUndoButton();

        int vis = FindVisibleMatch(batch[0].RowIndex);
        if (vis < 0 && _visible.Count > 0)
        {
            vis = 0;
        }

        if (vis >= 0)
        {
            SelectVisibleRow(vis);
            _grid.Focus();
        }
    }

    private int FindVisibleMatch(int rowIndex)
    {
        HashSet<int> clones = GetClones(rowIndex).ToHashSet();
        for (int i = 0; i < _visible.Count; i++)
        {
            OutlineRow outline = _visible[i];
            if (outline.Kind == OutlineKind.Match && clones.Contains(outline.RowIndex))
            {
                return i;
            }
        }

        return -1;
    }

    private void UpdateUndoButton()
    {
        bool enabled = _undoStack.Count > 0;
        _btnUndo.Enabled = enabled;
        _btnUndo.ForeColor = enabled ? DarkMode.TextPrimary : DarkMode.TextDisabled;
    }

    private void ReviewAllFiltered(ReviewDecision decision)
    {
        if (_rows.Length == 0)
        {
            return;
        }

        string? sportFilter = GetSelectedSport();
        string? fileFilter = GetSelectedFile();
        string status = _cboStatus.SelectedItem as string ?? StatusAll;

        BeginUndoBatch();
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
                    if (MatchesRow(rowIndex, fileFilter, status))
                    {
                        SetRowDecision(rowIndex, decision);
                    }
                }
            }

            RefreshFilterCounts();
            RebuildVisible();
        });
        CommitUndoBatch();

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
                if (!MatchesRow(rowIndex, fileFilter, status) ||
                    _rows[rowIndex].Decision != ReviewDecision.Pending)
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
        _grid.SetSelectionAnchor(visibleIndex);

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
        int denied = 0;

        foreach (SportNode sport in _sports)
        {
            total += sport.FilteredTotal;
            accurate += sport.FilteredAccurate;
            denied += sport.FilteredDenied;
        }

        int remaining = total - accurate - denied;
        string treeNote = _rows.Length == 0
            ? string.Empty
            : $"   ·   {_visible.Count:N0} tree rows";

        _lblStats.Text =
            $"{total:N0} matches   ·   {accurate:N0} accurate   ·   {denied:N0} denied   ·   {remaining:N0} to review{treeNote}";
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
        textBox.BorderStyle = BorderStyle.None;
        textBox.BackColor = DarkMode.ElevatedSurface;
        textBox.ForeColor = DarkMode.TextPrimary;
        textBox.Font = CreateOwnedFont("Segoe UI", 10f);
        textBox.Margin = Padding.Empty;
    }

    private void StyleCombo(ThemedDropDown comboBox, int width)
    {
        comboBox.Width = width;
        comboBox.Height = 36;
        comboBox.Font = CreateOwnedFont("Segoe UI", 10f);
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
        button.Height = 36;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor =
            Color.FromArgb(
                Math.Min(255, backColor.R + 18),
                Math.Min(255, backColor.G + 18),
                Math.Min(255, backColor.B + 18));
        button.FlatAppearance.MouseDownBackColor = backColor;
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
        public int Denied { get; set; }
        public int FilteredTotal { get; set; }
        public int FilteredAccurate { get; set; }
        public int FilteredDenied { get; set; }
        public HashSet<CloneKey> SeenCloneKeys { get; } = new();
        public List<int> RowIndices { get; } = new();
        public List<FileNode> Files { get; } = new();
    }

    private readonly record struct CloneKey(string File, int Line, string Word)
    {
        public static CloneKey From(AnalysisTableRow row)
        {
            string word = string.IsNullOrWhiteSpace(row.Word) ? row.Found : row.Word;
            return new CloneKey(
                (row.File ?? string.Empty).ToUpperInvariant(),
                row.LineNumber,
                (word ?? string.Empty).ToUpperInvariant());
        }
    }

    private sealed class FileNode
    {
        public string Name { get; set; } = string.Empty;
        public bool Expanded { get; set; }
        public int Accurate { get; set; }
        public int Denied { get; set; }
        public int FilteredTotal { get; set; }
        public int FilteredAccurate { get; set; }
        public int FilteredDenied { get; set; }
        public List<int> RowIndices { get; } = new();
    }
}
