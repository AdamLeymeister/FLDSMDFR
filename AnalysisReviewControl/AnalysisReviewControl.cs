using FLDSMDFR.Core.Models;
using Themes;

namespace AnalysisReviewControl;


public partial class AnalysisReviewControl : UserControl
{
    private readonly List<AnalysisTableRow> _rows = new();

    public AnalysisReviewControl()
    {
        InitializeComponent();

        ConfigureControl();
    }

    private void ConfigureControl()
    {
        BackColor = DarkMode.Background;

        AutoScroll = true;
    }

    public void LoadData(IEnumerable<AnalysisTableRow> rows)
    {
        _rows.Clear();
        _rows.AddRange(rows);

        BuildGroups();
    }

    private void BuildGroups()
    {
        Controls.Clear();

        var searchTermGroups = _rows
            .GroupBy(x => x.Sport)
            .OrderBy(x => x.Key)
            .ToList();

        foreach (var group in searchTermGroups)
        {
            var searchTermControl =
                new SearchTermGroupControl();

            searchTermControl.Dock = DockStyle.Top;

            searchTermControl.LoadGroup(
                group.Key,
                group.ToList());

            Controls.Add(searchTermControl);

            // DockStyle.Top stacks backwards.
            Controls.SetChildIndex(
                searchTermControl,
                0);
        }
    }
}
