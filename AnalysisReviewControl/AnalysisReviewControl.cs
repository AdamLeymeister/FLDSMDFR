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
    private const string ColContext = "Context";

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
    private readonly Button _btnImport = new();
    private readonly Button _btnExport = new();
    private readonly Button _btnResetFilters = new();
    private readonly Button _btnConfirm = new();
    private readonly Button _btnDeny = new();
    private readonly Button _btnUndo = new();
    private readonly Button _btnNext = new();
    private readonly Button _btnCollapse = new();
    private readonly Button _btnFocus = new();
    private readonly Label _lblStats = new();
    private readonly Label _lblHelp = new();
    private readonly ReviewGrid _grid = new();
    private readonly ThemedScrollHost _scrollHost;
    private readonly SplitContainer _split = new();
    private readonly SourcePreviewPanel _preview = new();
    private readonly FocusReviewPanel _focusPanel = new();
    private readonly SourceTextCache _sourceCache = new();
    private readonly System.Windows.Forms.Timer _searchDebounce = new();
    private readonly Stack<List<(int RowIndex, ReviewDecision Previous)>> _undoStack = new();
    private List<(int RowIndex, ReviewDecision Previous)>? _pendingUndo;

    private Font? _sportFont;
    private Font? _fileFont;
    private bool _updatingUi;
    private string _searchText = string.Empty;
    private readonly List<DateTime> _reviewTimes = new();
    private bool _focusMode;
    private int _focusRowIndex = -1;
    private bool _splitLaidOut;

    public event EventHandler? ImportClicked;

    public event EventHandler? ExportClicked;

    public event EventHandler? ReviewChanged;

    public IReadOnlyList<AnalysisTableRow> Rows => _rows;

    public AnalysisReviewControl()
    {
        _scrollHost = new ThemedScrollHost(_grid);
        InitializeComponent();
        ConfigureControl();
    }

    private void ConfigureControl()
    {
        AutoScaleMode = AutoScaleMode.None;
        BackColor = DarkMode.Background;
        Padding = new Padding(4);
        Font = CreateOwnedFont("Segoe UI", 10.5f);
        _sportFont = CreateOwnedFont("Segoe UI", 9f, FontStyle.Bold);
        _fileFont = CreateOwnedFont("Segoe UI", 11f, FontStyle.Bold);

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
        _cboStatus.SelectedIndex = 1;
        _cboSport.Items.Add(AllSports);
        _cboSport.SelectedIndex = 0;
        _cboFile.Items.Add(AllFiles);
        _cboFile.SelectedIndex = 0;

        StyleActionButton(_btnImport, "Import JSON", DarkMode.Primary, DarkMode.Background, 128);
        StyleActionButton(_btnExport, "Export JSON", DarkMode.ElevatedSurface, DarkMode.TextPrimary, 120);
        StyleActionButton(_btnResetFilters, "Reset", DarkMode.ElevatedSurface, DarkMode.TextPrimary, 80);
        StyleActionButton(_btnConfirm, "Confirm  Y", DarkMode.Success, DarkMode.Background, 118);
        StyleActionButton(_btnDeny, "Deny  N", BlendErrorButton(), DarkMode.Error, 104);
        StyleActionButton(_btnUndo, "Undo  Z", DarkMode.ElevatedSurface, DarkMode.TextPrimary, 92);
        StyleActionButton(_btnNext, "Next  F3", DarkMode.ElevatedSurface, DarkMode.TextPrimary, 100);
        StyleActionButton(_btnCollapse, "Collapse all", DarkMode.ElevatedSurface, DarkMode.TextPrimary, 118);
        StyleActionButton(_btnFocus, "Focus", DarkMode.Primary, DarkMode.Background, 92);
        UpdateUndoButton();
        UpdateExportButton();

        flow.Controls.Add(Wrap(CreateCaption("Source"), CreateSourceRow()));
        flow.Controls.Add(Wrap(CreateCaption("Search"), new ModernFieldHost(_txtSearch, 268, searchIcon: true)));
        flow.Controls.Add(Wrap(CreateCaption("Sport"), _cboSport));
        flow.Controls.Add(Wrap(CreateCaption("File"), _cboFile));
        flow.Controls.Add(Wrap(CreateCaption("View"), CreateViewRow()));
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

    private static Color Blend(Color from, Color to, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            (int)(from.R + (to.R - from.R) * amount),
            (int)(from.G + (to.G - from.G) * amount),
            (int)(from.B + (to.B - from.B) * amount));
    }

    private ReviewRowBand GetRowBand(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _visible.Count)
        {
            return ReviewRowBand.Match;
        }

        return _visible[rowIndex].Kind switch
        {
            OutlineKind.Sport => ReviewRowBand.Sport,
            OutlineKind.File => ReviewRowBand.File,
            _ => ReviewRowBand.Match
        };
    }

    private bool GetRowExpanded(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _visible.Count)
        {
            return false;
        }

        OutlineRow outline = _visible[rowIndex];
        return outline.Kind switch
        {
            OutlineKind.Sport => _sports[outline.SportIndex].Expanded,
            OutlineKind.File => _sports[outline.SportIndex].Files[outline.FileIndex].Expanded,
            _ => true
        };
    }

    private Control CreateSourceRow()
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Width = 256,
            Height = 36,
            BackColor = DarkMode.Surface,
            Margin = Padding.Empty
        };

        _btnImport.Margin = Padding.Empty;
        _btnExport.Margin = new Padding(8, 0, 0, 0);
        panel.Controls.Add(_btnImport);
        panel.Controls.Add(_btnExport);
        return panel;
    }

    private Control CreateViewRow()
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Width = 236,
            Height = 36,
            BackColor = DarkMode.Surface,
            Margin = Padding.Empty
        };

        _cboStatus.Margin = Padding.Empty;
        _btnResetFilters.Margin = new Padding(8, 0, 0, 0);
        panel.Controls.Add(_cboStatus);
        panel.Controls.Add(_btnResetFilters);
        return panel;
    }

    private Control CreateButtonRow()
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Width = 680,
            Height = 36,
            BackColor = DarkMode.Surface,
            Margin = Padding.Empty
        };

        panel.Controls.Add(_btnConfirm);
        panel.Controls.Add(_btnDeny);
        panel.Controls.Add(_btnUndo);
        panel.Controls.Add(_btnNext);
        panel.Controls.Add(_btnCollapse);
        panel.Controls.Add(_btnFocus);
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
        _lblHelp.Text = "Y confirm   N deny   F3 next   Focus for one hit at a time";

        _pnlStatus.Controls.Add(_lblHelp);
        _pnlStatus.Controls.Add(_lblStats);
    }

    private void ConfigureGrid()
    {
        _grid.ResolveRowBand = GetRowBand;
        _grid.ResolveExpanded = GetRowExpanded;
        _grid.ResolveSelectableGroup = ExpandAndCollectGroupRows;
        _grid.ResolveSnippetHighlight = GetSnippetHighlight;
        _grid.ResolveStickyHeader = GetStickyHeader;
        _scrollHost.Dock = DockStyle.Fill;
        _grid.Font = CreateOwnedFont("Segoe UI", 10.5f);
        _grid.ColumnHeadersDefaultCellStyle.Font = CreateOwnedFont("Segoe UI", 9f, FontStyle.Bold);
        _grid.RowTemplate.Height = 56;

        _grid.Columns.Add(CreateTextColumn(ColSelect, "", 48, 48));
        _grid.Columns.Add(CreateTextColumn(ColState, "Status", 120, 108));
        _grid.Columns.Add(CreateTextColumn(ColItem, "Item", 200, 120));
        _grid.Columns.Add(CreateTextColumn(ColLine, "Line", 72, 56));
        _grid.Columns.Add(CreateTextColumn(ColContext, "Source", 320, 160));
        _grid.Columns.Add(CreateTextColumn(ColFile, "File", 180, 100));
        _grid.Columns.Add(CreateTextColumn(ColSummary, "Match", 200, 120));

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
        _grid.Columns[ColItem].FillWeight = 22;
        _grid.Columns[ColFile].Visible = false;
        _grid.Columns[ColContext].FillWeight = 40;
        _grid.Columns[ColSummary].FillWeight = 24;
    }

    private void ConfigureCard()
    {
        _card.Dock = DockStyle.Fill;
        _card.Padding = new Padding(18);
        _card.BackColor = Color.Transparent;
        _card.BackdropColor = DarkMode.Background;
        _card.FillColor = DarkMode.Surface;

        _split.Dock = DockStyle.Fill;
        _split.SplitterWidth = 6;
        _split.BorderStyle = BorderStyle.None;
        _split.BackColor = DarkMode.Border;
        _split.Panel1.BackColor = DarkMode.Surface;
        _split.Panel2.BackColor = DarkMode.Surface;
        _split.Panel1.Padding = new Padding(0, 0, 6, 0);
        _split.Panel2.Padding = new Padding(6, 0, 0, 0);
        _split.Panel1.Controls.Add(_scrollHost);
        _split.Panel2.Controls.Add(_preview);

        _focusPanel.Dock = DockStyle.Fill;

        var work = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DarkMode.Surface
        };
        work.Controls.Add(_split);
        work.Controls.Add(_focusPanel);
        _split.BringToFront();

        _card.Controls.Add(work);
        _card.Controls.Add(_pnlToolbar);
        _card.Controls.Add(_pnlStatus);
        work.BringToFront();

        Controls.Add(_card);
        Load += (_, _) => LayoutSplit(initial: true);
        _split.SizeChanged += (_, _) => LayoutSplit(initial: false);
    }

    private void LayoutSplit(bool initial)
    {
        if (_split.IsDisposed || _split.Width < 80)
        {
            return;
        }

        int splitter = Math.Max(_split.SplitterWidth, 1);
        int available = _split.Width - splitter;
        if (available < 50)
        {
            return;
        }

        int panel1Min = Math.Min(280, Math.Max(40, available / 3));
        int panel2Min = Math.Min(240, Math.Max(40, available / 4));
        if (panel1Min + panel2Min > available)
        {
            panel1Min = Math.Max(25, available / 2);
            panel2Min = Math.Max(25, available - panel1Min);
        }

        try
        {
            _split.Panel1MinSize = panel1Min;
            _split.Panel2MinSize = panel2Min;

            if (initial || !_splitLaidOut)
            {
                int distance = Math.Clamp(
                    _split.Width - Math.Min(360, available / 3 + 40),
                    panel1Min,
                    available - panel2Min);
                _split.SplitterDistance = distance;
                _splitLaidOut = true;
            }
        }
        catch (InvalidOperationException)
        {
        }
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

        _btnImport.Click += (_, _) => ImportClicked?.Invoke(this, EventArgs.Empty);
        _btnExport.Click += (_, _) => ExportClicked?.Invoke(this, EventArgs.Empty);
        _btnResetFilters.Click += (_, _) => ResetFilters();
        _btnConfirm.Click += (_, _) => ReviewSelected(decision: ReviewDecision.Accurate, advance: true);
        _btnDeny.Click += (_, _) => ReviewSelected(decision: ReviewDecision.Denied, advance: true);
        _btnUndo.Click += (_, _) => UndoLast();
        _btnNext.Click += (_, _) => JumpToNextReview();
        _btnCollapse.Click += (_, _) => CollapseAll();
        _btnFocus.Click += (_, _) => ToggleFocusMode();

        _grid.CellValueNeeded += Grid_CellValueNeeded;
        _grid.CellFormatting += Grid_CellFormatting;
        _grid.CellMouseClick += Grid_CellMouseClick;
        _grid.CellMouseEnter += Grid_CellMouseEnter;
        _grid.KeyDown += Grid_KeyDown;
        _grid.DataError += (_, e) => e.ThrowException = false;
        _grid.Paint += Grid_Paint;
        _grid.SelectionChanged += (_, _) => RefreshPreview();
        _grid.CurrentCellChanged += (_, _) => RefreshPreview();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Z) && !_txtSearch.ContainsFocus)
        {
            UndoLast();
            return true;
        }

        if (!_txtSearch.ContainsFocus)
        {
            Keys key = keyData & Keys.KeyCode;
            bool control = (keyData & Keys.Control) == Keys.Control;
            bool shift = (keyData & Keys.Shift) == Keys.Shift;

            if (!control && !shift && key is Keys.Y or Keys.J or Keys.H)
            {
                ReviewSelected(decision: ReviewDecision.Accurate, advance: true);
                return true;
            }

            if (!control && !shift && key is Keys.N or Keys.K or Keys.T)
            {
                ReviewSelected(decision: ReviewDecision.Denied, advance: true);
                return true;
            }

            if (key == Keys.F3)
            {
                JumpToNextReview();
                return true;
            }

            if (_focusMode && key == Keys.Escape)
            {
                SetFocusMode(false);
                return true;
            }
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
        _reviewTimes.Clear();
        _focusRowIndex = -1;
        LoadSourceLines();
        BuildSportIndex();
        RebuildSportFilter();
        RebuildFileFilter();
        ExpandFirstPendingFile();
        ApplyFilterAndRebuild(keepCurrent: false);
        UpdateUndoButton();
        UpdateExportButton();
        RefreshFocusUi();

        if (_visible.Count > 0)
        {
            int firstHit = FindNextHit(-1);
            SelectVisibleRow(firstHit >= 0 ? firstHit : 0);
            _grid.Focus();
        }

        RefreshPreview();
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
        RefreshPreview();
        RefreshFocusUi();
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
                Contains(clone.LineNumber.ToString(), _searchText) ||
                Contains(clone.LineText, _searchText))
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

                foreach (int rowIndex in GetOrderedMatches(file, fileFilter, status))
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
        _scrollHost.SyncFromGrid();
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
            ColContext => GetContextText(outline),
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

        if (accurate + denied == total)
        {
            return "Complete";
        }

        return "Incomplete";
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
            OutlineKind.Sport => _sports[outline.SportIndex].Name.ToUpperInvariant(),
            OutlineKind.File => DisplayFileName(_sports[outline.SportIndex].Files[outline.FileIndex].Name),
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

    private string GetContextText(OutlineRow outline)
    {
        if (outline.Kind != OutlineKind.Match)
        {
            return string.Empty;
        }

        return _rows[outline.RowIndex].LineText;
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
        return GetMatchReason(row, rowIndex);
    }

    private string GetMatchReason(AnalysisTableRow row, int rowIndex)
    {
        string word = DisplayWord(row);
        int[] clones = GetClones(rowIndex);
        var terms = clones
            .Select(index => _rows[index].Found)
            .Where(found => !string.IsNullOrWhiteSpace(found))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(found => found, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (terms.Count == 0)
        {
            return "exact match";
        }

        if (terms.Count == 1 &&
            string.Equals(terms[0], word, StringComparison.OrdinalIgnoreCase))
        {
            return IsFuzzyToken(word) ? "fuzzy token" : "exact match";
        }

        string searched = string.Join(", ", terms.Select(term => $"`{term}`"));
        string suffix = clones.Length > 1 ? $"  ·  {clones.Length} hits" : string.Empty;
        return $"searched {searched} → `{word}`{suffix}";
    }

    private bool IsGroupedHit(OutlineRow outline)
    {
        return outline.Kind == OutlineKind.Match && GetClones(outline.RowIndex).Length > 1;
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
            OutlineKind.Sport => DarkMode.HoverSurface,
            OutlineKind.File => DarkMode.ElevatedSurface,
            _ => DarkMode.Surface
        };

        e.CellStyle.BackColor = back;
        e.CellStyle.SelectionBackColor = DarkMode.HoverSurface;

        int indent = outline.Kind switch
        {
            OutlineKind.Sport => 48,
            OutlineKind.File => 48,
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
            e.CellStyle.ForeColor = outline.Kind switch
            {
                OutlineKind.Sport => DarkMode.Primary,
                OutlineKind.File => DarkMode.TextPrimary,
                _ => DarkMode.TextPrimary
            };
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
                "Complete" => DarkMode.Success,
                "Incomplete" => DarkMode.Warning,
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
        else if (column == ColContext)
        {
            e.CellStyle.ForeColor = DarkMode.TextSecondary;
            e.CellStyle.SelectionForeColor = DarkMode.TextPrimary;
            e.CellStyle.Font = Font;
        }
        else if (column == ColSummary)
        {
            e.CellStyle.ForeColor = IsGroupedHit(outline)
                ? DarkMode.Secondary
                : DarkMode.TextSecondary;
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

        if (outline.Kind is OutlineKind.Sport or OutlineKind.File)
        {
            ToggleExpanded(outline);
        }
    }

    private IReadOnlyList<int> ExpandAndCollectGroupRows(int visibleIndex)
    {
        if (visibleIndex < 0 || visibleIndex >= _visible.Count)
        {
            return Array.Empty<int>();
        }

        OutlineRow outline = _visible[visibleIndex];
        if (outline.Kind == OutlineKind.Match)
        {
            return new[] { visibleIndex };
        }

        bool changed = false;
        if (outline.Kind == OutlineKind.Sport)
        {
            SportNode sport = _sports[outline.SportIndex];
            if (!sport.Expanded)
            {
                sport.Expanded = true;
                changed = true;
            }

            EnsureFiles(sport);
            foreach (FileNode file in sport.Files)
            {
                if (file.FilteredTotal == 0 || file.Expanded)
                {
                    continue;
                }

                file.Expanded = true;
                changed = true;
            }
        }
        else
        {
            FileNode file = _sports[outline.SportIndex].Files[outline.FileIndex];
            if (!file.Expanded)
            {
                file.Expanded = true;
                changed = true;
            }
        }

        if (changed)
        {
            ApplyFilterAndRebuild(keepCurrent: true);
            visibleIndex = FindVisibleIndex(outline);
            if (visibleIndex < 0)
            {
                return Array.Empty<int>();
            }
        }

        return CollectGroupRows(visibleIndex);
    }

    private IReadOnlyList<int> CollectGroupRows(int headerIndex)
    {
        var rows = new List<int> { headerIndex };
        OutlineRow header = _visible[headerIndex];

        for (int i = headerIndex + 1; i < _visible.Count; i++)
        {
            OutlineRow row = _visible[i];
            if (header.Kind == OutlineKind.Sport)
            {
                if (row.Kind == OutlineKind.Sport)
                {
                    break;
                }
            }
            else if (row.Kind != OutlineKind.Match)
            {
                break;
            }

            rows.Add(i);
        }

        return rows;
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

        if (e.KeyCode is Keys.G or Keys.I && !e.Control && !e.Shift)
        {
            int row = _grid.CurrentCell?.RowIndex ?? -1;
            if (row >= 0)
            {
                _grid.SelectGroup(row, toggle: false);
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode is Keys.Y or Keys.J or Keys.H or Keys.N or Keys.K or Keys.T && !e.Control)
        {
            ReviewSelected(
                decision: e.KeyCode is Keys.Y or Keys.J or Keys.H
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

        if (e.KeyCode is Keys.Left or Keys.A && !e.Control && !e.Shift)
        {
            CollapseCurrent();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode is Keys.Right or Keys.D or Keys.E && !e.Control && !e.Shift)
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
            if (file.Expanded)
            {
                CollapseOtherFiles(outline.SportIndex, outline.FileIndex);
            }
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
        if (_focusMode)
        {
            ReviewFocusHit(decision, toggle, advance);
            return;
        }

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

        ExpandNextFileIfComplete(first);
        RefreshFilterCounts();
        RebuildVisible();
        UpdateStats();
        RefreshPreview();
        RefreshFocusUi();

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

        int next = FindNextHit(keep);
        if (next >= 0)
        {
            SelectVisibleRow(next);
        }
        else if (TrySelectNextHit(first))
        {
            return;
        }
        else if (keep >= 0)
        {
            SelectVisibleRow(keep);
        }
    }

    private int FindNextHit(int fromVisibleIndex)
    {
        int start = Math.Max(fromVisibleIndex + 1, 0);
        for (int i = start; i < _visible.Count; i++)
        {
            if (_visible[i].Kind == OutlineKind.Match)
            {
                return i;
            }
        }

        return -1;
    }

    private bool TrySelectNextHit(OutlineRow from)
    {
        if (!TryFindNextHit(from, out int sportIndex, out int fileIndex, out int rowIndex))
        {
            return false;
        }

        int existing = FindVisibleIndex(OutlineRow.Match(sportIndex, fileIndex, rowIndex));
        if (existing >= 0)
        {
            SelectVisibleRow(existing);
            return true;
        }

        SportNode sport = _sports[sportIndex];
        ExpandOnlyFile(sportIndex, fileIndex);

        RebuildVisible();
        UpdateStats();

        int visibleIndex = FindVisibleIndex(OutlineRow.Match(sportIndex, fileIndex, rowIndex));
        if (visibleIndex < 0)
        {
            return false;
        }

        SelectVisibleRow(visibleIndex);
        return true;
    }

    private bool TryFindNextHit(
        OutlineRow from,
        out int sportIndex,
        out int fileIndex,
        out int rowIndex)
    {
        sportIndex = -1;
        fileIndex = -1;
        rowIndex = -1;

        string? fileFilter = GetSelectedFile();
        string status = _cboStatus.SelectedItem as string ?? StatusAll;
        int startSport = from.SportIndex;
        int startFile = from.Kind == OutlineKind.Sport ? -1 : from.FileIndex;
        int startRow = from.Kind == OutlineKind.Match ? from.RowIndex : -1;
        bool skipRestOfSport = from.Kind == OutlineKind.Sport;
        bool skipRestOfFile = from.Kind == OutlineKind.File;

        for (int s = startSport; s < _sports.Count; s++)
        {
            SportNode sport = _sports[s];
            if (sport.FilteredTotal == 0)
            {
                continue;
            }

            if (s == startSport && skipRestOfSport)
            {
                continue;
            }

            EnsureFiles(sport);

            int firstFile = s == startSport && startFile >= 0 ? startFile : 0;
            for (int f = firstFile; f < sport.Files.Count; f++)
            {
                FileNode file = sport.Files[f];
                if (file.FilteredTotal == 0)
                {
                    continue;
                }

                if (s == startSport && f == startFile && skipRestOfFile)
                {
                    continue;
                }

                List<int> matches = GetOrderedMatches(file, fileFilter, status);
                int startMatch = 0;
                if (s == startSport && f == startFile && startRow >= 0)
                {
                    int at = matches.IndexOf(startRow);
                    if (at >= 0)
                    {
                        startMatch = at + 1;
                    }
                    else
                    {
                        startMatch = matches.FindIndex(index => CompareMatchOrder(index, startRow) > 0);
                        if (startMatch < 0)
                        {
                            startMatch = matches.Count;
                        }
                    }
                }

                if (startMatch >= matches.Count)
                {
                    continue;
                }

                sportIndex = s;
                fileIndex = f;
                rowIndex = matches[startMatch];
                return true;
            }
        }

        return false;
    }

    private List<int> GetOrderedMatches(FileNode file, string? fileFilter, string status)
    {
        return file.RowIndices
            .Where(index => MatchesRow(index, fileFilter, status))
            .OrderBy(SuspicionRank)
            .ThenBy(index => _rows[index].LineNumber)
            .ThenBy(index => DisplayWord(_rows[index]), StringComparer.OrdinalIgnoreCase)
            .ThenBy(index => index)
            .ToList();
    }

    private int CompareMatchOrder(int left, int right)
    {
        int cmp = SuspicionRank(left).CompareTo(SuspicionRank(right));
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = _rows[left].LineNumber.CompareTo(_rows[right].LineNumber);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(
            DisplayWord(_rows[left]),
            DisplayWord(_rows[right]),
            StringComparison.OrdinalIgnoreCase);
        return cmp != 0 ? cmp : left.CompareTo(right);
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

            if (_countsInTree[cloneIndex] &&
                previous == ReviewDecision.Pending &&
                decision != ReviewDecision.Pending)
            {
                _reviewTimes.Add(DateTime.UtcNow);
            }

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
        RefreshPreview();

        if (_focusMode)
        {
            _focusRowIndex = batch[0].RowIndex;
            RefreshFocusUi();
            return;
        }

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

    private void UpdateExportButton()
    {
        bool enabled = _rows.Length > 0;
        _btnExport.Enabled = enabled;
        _btnExport.ForeColor = enabled ? DarkMode.TextPrimary : DarkMode.TextDisabled;
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
        RefreshPreview();
        RefreshFocusUi();
    }

    private void JumpToNextReview()
    {
        if (_focusMode)
        {
            MoveFocusToNextPending(skipCurrent: true);
            RefreshFocusUi();
            return;
        }

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

            EnsureFiles(sport);

            foreach (int rowIndex in GetOrderedPending(sport, fileFilter, status))
            {
                if (_rows[rowIndex].Decision != ReviewDecision.Pending)
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

                ExpandOnlyFile(sportIndex, _fileIndexByRow[rowIndex]);
                RebuildVisible();
                int fileIndex = _fileIndexByRow[rowIndex];
                int visibleIndex = fileIndex >= 0
                    ? FindVisibleIndex(OutlineRow.Match(sportIndex, fileIndex, rowIndex))
                    : -1;

                if (visibleIndex >= 0)
                {
                    SelectVisibleRow(visibleIndex);
                }

                UpdateStats();
                RefreshPreview();
                RefreshFocusUi();
                return;
            }
        }
    }

    private IEnumerable<int> GetOrderedPending(SportNode sport, string? fileFilter, string status)
    {
        EnsureFiles(sport);
        foreach (FileNode file in sport.Files)
        {
            foreach (int rowIndex in GetOrderedMatches(file, fileFilter, status))
            {
                yield return rowIndex;
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

        _scrollHost.SyncFromGrid();
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

        ReviewChanged?.Invoke(this, EventArgs.Empty);
    }

    public ReviewProgressSnapshot GetProgress()
    {
        int total = 0;
        int accurate = 0;
        int denied = 0;

        foreach (SportNode sport in _sports)
        {
            total += sport.Total;
            accurate += sport.Accurate;
            denied += sport.Denied;
        }

        int pending = Math.Max(0, total - accurate - denied);
        return new ReviewProgressSnapshot
        {
            Total = total,
            Accurate = accurate,
            Denied = denied,
            Pending = pending,
            EstimatedRemaining = EstimateRemaining(pending)
        };
    }

    private TimeSpan? EstimateRemaining(int pending)
    {
        if (pending <= 0)
        {
            return TimeSpan.Zero;
        }

        if (_reviewTimes.Count < 3)
        {
            return null;
        }

        TimeSpan elapsed = DateTime.UtcNow - _reviewTimes[0];
        if (elapsed.TotalSeconds < 20)
        {
            return null;
        }

        if (_reviewTimes.Count > 200)
        {
            _reviewTimes.RemoveRange(0, _reviewTimes.Count - 200);
            elapsed = DateTime.UtcNow - _reviewTimes[0];
        }

        double perSecond = _reviewTimes.Count / elapsed.TotalSeconds;
        if (perSecond <= 0)
        {
            return null;
        }

        return TimeSpan.FromSeconds(pending / perSecond);
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

    private void ResetFilters()
    {
        _searchDebounce.Stop();
        _updatingUi = true;
        _txtSearch.Clear();
        _searchText = string.Empty;
        if (_cboSport.Items.Count > 0)
        {
            _cboSport.SelectedIndex = 0;
        }

        RebuildFileFilter();
        _updatingUi = true;
        if (_cboStatus.Items.Count > 1)
        {
            _cboStatus.SelectedIndex = 1;
        }

        _updatingUi = false;
        ApplyFilterAndRebuild(keepCurrent: true);
        _grid.Focus();
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

    private void LoadSourceLines()
    {
        _sourceCache.Clear();
        _sourceCache.Load(_rows.Select(row => row.File));
        foreach (AnalysisTableRow row in _rows)
        {
            row.LineText = _sourceCache.GetLine(row.File, row.LineNumber);
        }
    }

    private int SuspicionRank(int rowIndex)
    {
        AnalysisTableRow row = _rows[rowIndex];
        string word = DisplayWord(row);
        if (IsFuzzyToken(word) || IsFuzzyToken(row.Found))
        {
            return 0;
        }

        if (GetClones(rowIndex).Length > 1)
        {
            return 1;
        }

        if (!string.Equals(row.Found, word, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 3;
    }

    private static bool IsFuzzyToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (char c in value)
        {
            if (!char.IsLetterOrDigit(c) && c != '\'')
            {
                return true;
            }
        }

        return false;
    }

    private void ExpandFirstPendingFile()
    {
        for (int sportIndex = 0; sportIndex < _sports.Count; sportIndex++)
        {
            SportNode sport = _sports[sportIndex];
            EnsureFiles(sport);
            for (int fileIndex = 0; fileIndex < sport.Files.Count; fileIndex++)
            {
                if (FileHasPending(sport.Files[fileIndex]))
                {
                    ExpandOnlyFile(sportIndex, fileIndex);
                    return;
                }
            }
        }
    }

    private void CollapseOtherFiles(int sportIndex, int keepFileIndex)
    {
        ExpandOnlyFile(sportIndex, keepFileIndex);
    }

    private void ExpandOnlyFile(int sportIndex, int fileIndex)
    {
        if (sportIndex < 0 || sportIndex >= _sports.Count)
        {
            return;
        }

        for (int s = 0; s < _sports.Count; s++)
        {
            SportNode sport = _sports[s];
            EnsureFiles(sport);
            sport.Expanded = s == sportIndex;
            for (int f = 0; f < sport.Files.Count; f++)
            {
                sport.Files[f].Expanded = s == sportIndex && f == fileIndex;
            }
        }
    }

    private void ExpandNextFileIfComplete(OutlineRow reviewed)
    {
        if (reviewed.Kind == OutlineKind.Sport)
        {
            return;
        }

        int sportIndex = reviewed.SportIndex;
        int fileIndex = reviewed.Kind == OutlineKind.Match
            ? _fileIndexByRow[reviewed.RowIndex]
            : reviewed.FileIndex;

        if (sportIndex < 0 || sportIndex >= _sports.Count)
        {
            return;
        }

        SportNode sport = _sports[sportIndex];
        EnsureFiles(sport);
        if (fileIndex < 0 || fileIndex >= sport.Files.Count)
        {
            return;
        }

        if (FileHasPending(sport.Files[fileIndex]))
        {
            return;
        }

        for (int s = sportIndex; s < _sports.Count; s++)
        {
            SportNode nextSport = _sports[s];
            EnsureFiles(nextSport);
            int startFile = s == sportIndex ? fileIndex + 1 : 0;
            for (int f = startFile; f < nextSport.Files.Count; f++)
            {
                if (FileHasPending(nextSport.Files[f]))
                {
                    ExpandOnlyFile(s, f);
                    return;
                }
            }
        }
    }

    private static bool FileHasPending(FileNode file)
    {
        return file.RowIndices.Count - file.Accurate - file.Denied > 0;
    }

    private string? GetSnippetHighlight(int visibleIndex)
    {
        if (visibleIndex < 0 || visibleIndex >= _visible.Count)
        {
            return null;
        }

        OutlineRow outline = _visible[visibleIndex];
        if (outline.Kind != OutlineKind.Match)
        {
            return null;
        }

        return GetHighlightForRow(_rows[outline.RowIndex]);
    }

    private static string GetHighlightForRow(AnalysisTableRow row)
    {
        string word = DisplayWord(row);
        return string.IsNullOrWhiteSpace(word) ? row.Found : word;
    }

    private StickyOverlay? GetStickyHeader(int firstVisible)
    {
        if (firstVisible < 0 || firstVisible >= _visible.Count)
        {
            return null;
        }

        OutlineRow first = _visible[firstVisible];
        if (first.Kind != OutlineKind.Match)
        {
            return null;
        }

        FileNode file = _sports[first.SportIndex].Files[first.FileIndex];
        string state = file.Accurate + file.Denied == file.RowIndices.Count && file.RowIndices.Count > 0
            ? "Complete"
            : "Incomplete";

        return new StickyOverlay
        {
            Title = DisplayFileName(file.Name),
            State = state,
            Summary = $"{file.FilteredTotal:N0} shown"
        };
    }

    private void ToggleFocusMode()
    {
        SetFocusMode(!_focusMode);
    }

    private void SetFocusMode(bool enabled)
    {
        _focusMode = enabled;
        _split.Visible = !enabled;
        _focusPanel.Visible = enabled;
        _btnFocus.Text = enabled ? "Outline" : "Focus";

        if (enabled)
        {
            OutlineRow? current = GetCurrentOutline();
            _focusRowIndex = current?.Kind == OutlineKind.Match
                ? current.Value.RowIndex
                : -1;
            if (_focusRowIndex < 0 || _rows[_focusRowIndex].Decision != ReviewDecision.Pending)
            {
                MoveFocusToNextPending(skipCurrent: false);
            }

            RefreshFocusUi();
            _focusPanel.BringToFront();
            _focusPanel.Focus();
        }
        else
        {
            _grid.Focus();
            RefreshPreview();
        }
    }

    private void ReviewFocusHit(ReviewDecision decision, bool toggle, bool advance)
    {
        if (_focusRowIndex < 0 || _focusRowIndex >= _rows.Length)
        {
            MoveFocusToNextPending(skipCurrent: false);
            RefreshFocusUi();
            return;
        }

        ReviewDecision value = toggle ? NextDecision(_rows[_focusRowIndex].Decision) : decision;
        OutlineRow from = new()
        {
            Kind = OutlineKind.Match,
            SportIndex = _sportIndexByRow[_focusRowIndex],
            FileIndex = _fileIndexByRow[_focusRowIndex],
            RowIndex = _focusRowIndex
        };

        BeginUndoBatch();
        SetRowDecision(_focusRowIndex, value);
        CommitUndoBatch();
        ExpandNextFileIfComplete(from);
        RefreshFilterCounts();
        RebuildVisible();
        UpdateStats();

        if (advance)
        {
            MoveFocusToNextPending(skipCurrent: true);
        }

        RefreshFocusUi();
    }

    private void MoveFocusToNextPending(bool skipCurrent)
    {
        List<int> pending = CollectOrderedPending();
        if (pending.Count == 0)
        {
            _focusRowIndex = -1;
            return;
        }

        int at = pending.IndexOf(_focusRowIndex);
        int next = skipCurrent
            ? at >= 0 && at + 1 < pending.Count ? at + 1 : 0
            : at >= 0 ? at : 0;

        if (skipCurrent && at < 0)
        {
            next = 0;
        }

        if (skipCurrent && at >= 0 && at + 1 >= pending.Count)
        {
            _focusRowIndex = -1;
            return;
        }

        _focusRowIndex = pending[next];
        int sportIndex = _sportIndexByRow[_focusRowIndex];
        int fileIndex = _fileIndexByRow[_focusRowIndex];
        ExpandOnlyFile(sportIndex, fileIndex);
    }

    private List<int> CollectOrderedPending()
    {
        var pending = new List<int>();
        string? fileFilter = GetSelectedFile();
        foreach (SportNode sport in _sports)
        {
            if (GetSelectedSport() is string sportFilter &&
                !string.Equals(sport.Name, sportFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            EnsureFiles(sport);
            foreach (int rowIndex in GetOrderedPending(sport, fileFilter, StatusReview))
            {
                if (_rows[rowIndex].Decision == ReviewDecision.Pending)
                {
                    pending.Add(rowIndex);
                }
            }
        }

        return pending;
    }

    private void RefreshPreview()
    {
        if (_focusMode)
        {
            return;
        }

        OutlineRow? current = GetCurrentOutline();
        if (current?.Kind != OutlineKind.Match)
        {
            _preview.ShowEmpty("Select a hit to preview the source line.");
            return;
        }

        ShowPreview(_preview, _rows[current.Value.RowIndex], current.Value.RowIndex);
    }

    private void RefreshFocusUi()
    {
        if (!_focusMode)
        {
            return;
        }

        int remaining = CountPending();
        if (_focusRowIndex < 0 || _focusRowIndex >= _rows.Length)
        {
            _focusPanel.ShowEmpty(remaining == 0
                ? "No hits left to review. Press Escape or Outline to return."
                : "No pending hit in the current filters.");
            return;
        }

        AnalysisTableRow row = _rows[_focusRowIndex];
        string progress = remaining == 1
            ? "1 hit left"
            : $"{remaining:N0} hits left  ·  Y confirm   N deny";
        _focusPanel.ShowHit(
            DisplayWord(row),
            DisplayFileName(row.File),
            row.LineNumber,
            GetMatchReason(row, _focusRowIndex),
            GetHighlightForRow(row),
            _sourceCache.GetLines(row.File),
            progress);
    }

    private void ShowPreview(SourcePreviewPanel preview, AnalysisTableRow row, int rowIndex)
    {
        preview.ShowHit(
            DisplayFileName(row.File),
            row.LineNumber,
            GetMatchReason(row, rowIndex),
            GetHighlightForRow(row),
            _sourceCache.GetLines(row.File));
    }

    private int CountPending()
    {
        int pending = 0;
        string? fileFilter = GetSelectedFile();
        foreach (SportNode sport in _sports)
        {
            if (GetSelectedSport() is string sportFilter &&
                !string.Equals(sport.Name, sportFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (int rowIndex in sport.RowIndices)
            {
                if (MatchesRow(rowIndex, fileFilter, StatusReview))
                {
                    pending++;
                }
            }
        }

        return pending;
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
