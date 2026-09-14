using FLDSMDFR.Core.Models;
using Themes;

namespace AnalysisReviewControl;

public partial class AnalysisReviewControl : ReviewUserControl
{
    private readonly List<AnalysisTableRow> _rows = new();
    private readonly FlowLayoutPanel _pnlGroups = new();
    private bool _sizingGroups;

    public AnalysisReviewControl()
    {
        InitializeComponent();

        ConfigureControl();
    }

    private void ConfigureControl()
    {
        BackColor = DarkMode.Background;
        AutoScroll = false;

        _pnlGroups.Dock = DockStyle.Fill;
        _pnlGroups.FlowDirection = FlowDirection.TopDown;
        _pnlGroups.WrapContents = false;
        _pnlGroups.AutoScroll = true;
        _pnlGroups.BackColor = DarkMode.Background;
        _pnlGroups.Padding = Padding.Empty;
        _pnlGroups.Layout += (_, _) => SizeGroupsToWidth();

        Controls.Add(_pnlGroups);
    }

    public void LoadData(IEnumerable<AnalysisTableRow> rows)
    {
        _rows.Clear();
        _rows.AddRange(rows);

        BuildGroups();
    }

    private void BuildGroups()
    {
        SuspendLayout();
        _pnlGroups.SuspendLayout();

        DisposeChildren(_pnlGroups);

        var sportGroups = _rows
            .GroupBy(row => row.Sport)
            .OrderBy(group => group.Key)
            .ToList();

        foreach (var group in sportGroups)
        {
            var sportGroupControl = new SearchTermGroupControl
            {
                Margin = new Padding(0, 0, 0, 6),
                Width = Math.Max(_pnlGroups.ClientSize.Width, 1)
            };

            sportGroupControl.LoadGroup(
                group.Key,
                group.ToList());

            _pnlGroups.Controls.Add(sportGroupControl);
        }

        _pnlGroups.ResumeLayout(true);
        ResumeLayout(true);

        SizeGroupsToWidth();
    }

    private void SizeGroupsToWidth()
    {
        if (_sizingGroups)
        {
            return;
        }

        _sizingGroups = true;

        try
        {
            int width = Math.Max(_pnlGroups.ClientSize.Width, 1);

            foreach (Control control in _pnlGroups.Controls)
            {
                Size minimumSize = control.MinimumSize;
                if (minimumSize.Width != width)
                {
                    control.MinimumSize = new Size(width, minimumSize.Height);
                }

                Size maximumSize = control.MaximumSize;
                if (maximumSize.Width != width)
                {
                    control.MaximumSize = new Size(width, 0);
                }

                if (control.Width != width)
                {
                    control.Width = width;
                }
            }

            _pnlGroups.HorizontalScroll.Visible = false;
            _pnlGroups.HorizontalScroll.Enabled = false;
        }
        finally
        {
            _sizingGroups = false;
        }
    }
}
