using FLDSMDFR.Core.Models;
using System;
using System.Drawing;
using System.Windows.Forms;
using Themes;

namespace AnalysisReviewControl;

public partial class MatchRowControl : UserControl
{
    private AnalysisTableRow? _row;

    private const int RowHeight = 32;

    private bool _updatingCheckbox;

    public event EventHandler? AccuracyChanged;

    public MatchRowControl()
    {
        InitializeComponent();

        ConfigureControl();
    }

    private void ConfigureControl()
    {
        // ------------------------------------------------------------
        // Main Control
        // ------------------------------------------------------------

        Height =
            RowHeight;

        MinimumSize =
            new Size(
                0,
                RowHeight);

        Margin =
            new Padding(
                0,
                1,
                0,
                1);

        Padding =
            Padding.Empty;

        BackColor =
            DarkMode.Background;

        // ------------------------------------------------------------
        // Accurate Checkbox
        // ------------------------------------------------------------

        chkAccurate.Dock =
            DockStyle.Right;

        chkAccurate.Width =
            110;

        chkAccurate.Text =
            "Accurate";

        chkAccurate.TextAlign =
            ContentAlignment.MiddleLeft;

        chkAccurate.ForeColor =
            DarkMode.TextSecondary;

        chkAccurate.BackColor =
            Color.Transparent;

        chkAccurate.Font =
            new Font(
                "Segoe UI",
                8.5f,
                FontStyle.Regular);

        chkAccurate.Cursor =
            Cursors.Hand;

        // ------------------------------------------------------------
        // Found Label
        // ------------------------------------------------------------

        lblFound.Dock =
            DockStyle.Fill;

        lblFound.ForeColor =
            DarkMode.TextPrimary;

        lblFound.BackColor =
            Color.Transparent;

        lblFound.Font =
            new Font(
                "Segoe UI",
                8.5f,
                FontStyle.Regular);

        lblFound.TextAlign =
            ContentAlignment.MiddleLeft;

        lblFound.Padding =
            new Padding(
                12,
                0,
                0,
                0);

        lblFound.AutoEllipsis =
            true;

        // ------------------------------------------------------------
        // Events
        // ------------------------------------------------------------

        chkAccurate.CheckedChanged +=
            chkAccurate_CheckedChanged;
    }

    public void LoadRow(
        AnalysisTableRow row)
    {
        _row =
            row;

        lblFound.Text =
            row.Found;

        RefreshFromData();
    }

    public void RefreshFromData()
    {
        if (_row == null)
        {
            return;
        }

        _updatingCheckbox =
            true;

        chkAccurate.Checked =
            _row.IsAccurate;

        _updatingCheckbox =
            false;
    }

    private void chkAccurate_CheckedChanged(
        object? sender,
        EventArgs e)
    {
        if (_row == null ||
            _updatingCheckbox)
        {
            return;
        }

        _row.IsAccurate =
            chkAccurate.Checked;

        AccuracyChanged?.Invoke(
            this,
            EventArgs.Empty);
    }
}