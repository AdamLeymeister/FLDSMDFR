using FLDSMDFR.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Themes;

namespace AnalysisReviewControl
{
    public partial class FileGroupControl : UserControl
    {
        private readonly List<AnalysisTableRow> _rows = new();

        private bool _expanded = false;
        private bool _updatingCheckbox;

        public event EventHandler? AccuracyChanged;

        private const int HeaderHeight = 40;

        public FileGroupControl()
        {
            InitializeComponent();

            ConfigureControl();
        }

        private void ConfigureControl()
        {
            // ------------------------------------------------------------
            // Main Control
            // ------------------------------------------------------------

            BackColor = DarkMode.Background;

            Height = HeaderHeight;

            Margin =
                Padding.Empty;

            Padding =
                new Padding(
                    0,
                    0,
                    0,
                    4);

            MinimumSize = new Size(
                0,
                HeaderHeight);

            // ------------------------------------------------------------
            // Header
            // ------------------------------------------------------------

            pnlHeader.Dock =
                DockStyle.Top;

            pnlHeader.Height =
                HeaderHeight;

            pnlHeader.BackColor =
                DarkMode.ElevatedSurface;

            pnlHeader.Padding =
                new Padding(
                    6,
                    0,
                    10,
                    0);

            pnlHeader.Cursor =
                Cursors.Hand;

            // ------------------------------------------------------------
            // Matches Container
            // ------------------------------------------------------------

            pnlMatches.Dock =
                DockStyle.Top;

            pnlMatches.BackColor =
                DarkMode.Background;

            pnlMatches.Padding =
                new Padding(
                    20,
                    2,
                    0,
                    2);

            pnlMatches.Visible =
                _expanded;

            pnlMatches.Height =
                0;

            // ------------------------------------------------------------
            // Expand Button
            // ------------------------------------------------------------

            btnExpand.Dock =
                DockStyle.Left;

            btnExpand.Width =
                40;

            btnExpand.FlatStyle =
                FlatStyle.Flat;

            btnExpand.FlatAppearance.BorderSize =
                0;

            btnExpand.FlatAppearance.MouseOverBackColor =
                DarkMode.HoverSurface;

            btnExpand.FlatAppearance.MouseDownBackColor =
                DarkMode.Surface;

            btnExpand.BackColor =
                Color.Transparent;

            btnExpand.ForeColor =
                DarkMode.TextSecondary;

            btnExpand.Font =
                new Font(
                    "Segoe UI Symbol",
                    9f,
                    FontStyle.Regular);

            btnExpand.Text =
                _expanded
                    ? "▼"
                    : "▶";

            btnExpand.Cursor =
                Cursors.Hand;

            btnExpand.TabStop =
                false;

            // ------------------------------------------------------------
            // Bulk Checkbox
            // ------------------------------------------------------------

            chkBulk.Dock =
                DockStyle.Right;

            chkBulk.Width =
                110;

            chkBulk.ThreeState =
                true;

            chkBulk.Text =
                "Accurate";

            chkBulk.TextAlign =
                ContentAlignment.MiddleLeft;

            chkBulk.BackColor =
                Color.Transparent;

            chkBulk.ForeColor =
                DarkMode.TextSecondary;

            chkBulk.Font =
                new Font(
                    "Segoe UI",
                    9f,
                    FontStyle.Regular);

            chkBulk.Cursor =
                Cursors.Hand;

            // ------------------------------------------------------------
            // Count Label
            // ------------------------------------------------------------

            lblCount.Dock =
                DockStyle.Right;

            lblCount.Width =
                180;

            lblCount.ForeColor =
                DarkMode.TextSecondary;

            lblCount.BackColor =
                Color.Transparent;

            lblCount.Font =
                new Font(
                    "Segoe UI",
                    8.5f,
                    FontStyle.Regular);

            lblCount.TextAlign =
                ContentAlignment.MiddleRight;

            lblCount.Padding =
                new Padding(
                    0,
                    0,
                    12,
                    0);

            // ------------------------------------------------------------
            // File Label
            // ------------------------------------------------------------

            lblFile.Dock =
                DockStyle.Fill;

            lblFile.ForeColor =
                DarkMode.TextPrimary;

            lblFile.BackColor =
                Color.Transparent;

            lblFile.Font =
                new Font(
                    "Segoe UI",
                    9f,
                    FontStyle.Regular);

            lblFile.TextAlign =
                ContentAlignment.MiddleLeft;

            lblFile.Padding =
                new Padding(
                    8,
                    0,
                    0,
                    0);

            lblFile.AutoEllipsis =
                true;

            lblFile.Cursor =
                Cursors.Hand;

            // ------------------------------------------------------------
            // Events
            // ------------------------------------------------------------

            btnExpand.Click +=
                btnExpand_Click;

            chkBulk.CheckStateChanged +=
                chkBulk_CheckStateChanged;

            pnlHeader.Click +=
                Header_Click;

            lblFile.Click +=
                Header_Click;

            lblCount.Click +=
                Header_Click;

            pnlHeader.MouseEnter +=
                Header_MouseEnter;

            pnlHeader.MouseLeave +=
                Header_MouseLeave;

            lblFile.MouseEnter +=
                Header_MouseEnter;

            lblFile.MouseLeave +=
                Header_MouseLeave;

            lblCount.MouseEnter +=
                Header_MouseEnter;

            lblCount.MouseLeave +=
                Header_MouseLeave;
        }

        public void LoadGroup(
            string file,
            List<AnalysisTableRow> rows)
        {
            _rows.Clear();
            _rows.AddRange(rows);

            lblFile.Text =
                file;

            lblFile.Tag =
                file;

            BuildMatchRows();

            UpdateBulkCheckbox();

            UpdateHeight();
        }

        private void BuildMatchRows()
        {
            pnlMatches.SuspendLayout();

            pnlMatches.Controls.Clear();

            foreach (var row in _rows)
            {
                var matchControl =
                    new MatchRowControl();

                matchControl.Dock =
                    DockStyle.Top;

                matchControl.Margin =
                    new Padding(
                        0,
                        1,
                        0,
                        1);

                matchControl.LoadRow(
                    row);

                matchControl.AccuracyChanged +=
                    MatchAccuracyChanged;

                pnlMatches.Controls.Add(
                    matchControl);

                pnlMatches.Controls.SetChildIndex(
                    matchControl,
                    0);
            }

            pnlMatches.ResumeLayout();
        }

        private void Header_Click(
            object? sender,
            EventArgs e)
        {
            ToggleExpanded();
        }

        private void btnExpand_Click(
            object? sender,
            EventArgs e)
        {
            ToggleExpanded();
        }

        private void ToggleExpanded()
        {
            _expanded =
                !_expanded;

            pnlMatches.Visible =
                _expanded;

            btnExpand.Text =
                _expanded
                    ? "▼"
                    : "▶";

            UpdateHeight();
        }

        private void Header_MouseEnter(
            object? sender,
            EventArgs e)
        {
            pnlHeader.BackColor =
                DarkMode.HoverSurface;
        }

        private void Header_MouseLeave(
            object? sender,
            EventArgs e)
        {
            pnlHeader.BackColor =
                DarkMode.ElevatedSurface;
        }

        private void chkBulk_CheckStateChanged(
            object? sender,
            EventArgs e)
        {
            if (_updatingCheckbox)
            {
                return;
            }

            if (chkBulk.CheckState ==
                CheckState.Indeterminate)
            {
                return;
            }

            bool value =
                chkBulk.CheckState ==
                CheckState.Checked;

            foreach (var row in _rows)
            {
                row.IsAccurate =
                    value;
            }

            RefreshFromData();

            AccuracyChanged?.Invoke(
                this,
                EventArgs.Empty);
        }

        private void MatchAccuracyChanged(
            object? sender,
            EventArgs e)
        {
            UpdateBulkCheckbox();

            AccuracyChanged?.Invoke(
                this,
                EventArgs.Empty);
        }

        public void RefreshFromData()
        {
            foreach (
                MatchRowControl matchControl
                in pnlMatches.Controls)
            {
                matchControl.RefreshFromData();
            }

            UpdateBulkCheckbox();
        }

        private void UpdateBulkCheckbox()
        {
            _updatingCheckbox =
                true;

            int accurateCount =
                _rows.Count(
                    row =>
                        row.IsAccurate);

            if (accurateCount == 0)
            {
                chkBulk.CheckState =
                    CheckState.Unchecked;
            }
            else if (
                accurateCount ==
                _rows.Count)
            {
                chkBulk.CheckState =
                    CheckState.Checked;
            }
            else
            {
                chkBulk.CheckState =
                    CheckState.Indeterminate;
            }

            lblCount.Text =
                $"{_rows.Count:N0} matches  •  " +
                $"{accurateCount:N0} accurate";

            _updatingCheckbox =
                false;
        }

        private void UpdateHeight()
        {
            int contentHeight;

            if (!_expanded)
            {
                pnlMatches.Height = 0;

                contentHeight =
                    pnlHeader.Height;
            }
            else
            {
                int matchesHeight =
                    pnlMatches.Controls
                        .Cast<Control>()
                        .Sum(control =>
                            control.Height +
                            control.Margin.Vertical);

                pnlMatches.Height =
                    matchesHeight +
                    pnlMatches.Padding.Vertical;

                contentHeight =
                    pnlHeader.Height +
                    pnlMatches.Height;
            }

            int newHeight =
                contentHeight +
                Padding.Bottom;

            if (Height != newHeight)
            {
                Height = newHeight;
            }
        }
    }
}
