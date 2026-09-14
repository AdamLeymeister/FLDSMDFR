using FLDSMDFR.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Themes;

namespace AnalysisReviewControl
{
    public partial class SearchTermGroupControl : ReviewUserControl
    {
        private readonly List<AnalysisTableRow> _rows = new();

        private bool _expanded = false;
        private bool _updatingCheckbox;

        private const int HeaderHeight = 44;

        public SearchTermGroupControl()
        {
            InitializeComponent();

            ConfigureControl();
        }

        private void ConfigureControl()
        {
            // ------------------------------------------------------------
            // Main Control
            // ------------------------------------------------------------

            BackColor =
                DarkMode.Background;

            AutoSize =
                true;

            AutoSizeMode =
                AutoSizeMode.GrowAndShrink;

            MinimumSize =
                new Size(
                    0,
                    HeaderHeight);

            Margin =
                new Padding(
                    0,
                    0,
                    0,
                    6);

            Padding =
                Padding.Empty;

            // ------------------------------------------------------------
            // Header
            // ------------------------------------------------------------

            pnlHeader.Dock =
                DockStyle.Top;

            pnlHeader.Height =
                HeaderHeight;

            pnlHeader.MinimumSize =
                new Size(
                    0,
                    HeaderHeight);

            pnlHeader.BackColor =
                DarkMode.Surface;

            pnlHeader.Padding =
                new Padding(
                    8,
                    0,
                    12,
                    0);

            pnlHeader.Cursor =
                Cursors.Hand;

            // ------------------------------------------------------------
            // File Container
            // ------------------------------------------------------------

            pnlFiles.Dock =
                DockStyle.Top;

            pnlFiles.AutoSize =
                true;

            pnlFiles.AutoSizeMode =
                AutoSizeMode.GrowAndShrink;

            pnlFiles.BackColor =
                DarkMode.Background;

            pnlFiles.Padding =
                new Padding(
                    12,
                    2,
                    0,
                    2);

            pnlFiles.Visible =
                _expanded;

            // ------------------------------------------------------------
            // Docking Order
            // ------------------------------------------------------------

            Controls.SetChildIndex(
                pnlFiles,
                0);

            Controls.SetChildIndex(
                pnlHeader,
                1);

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
                DarkMode.ElevatedSurface;

            btnExpand.BackColor =
                Color.Transparent;

            btnExpand.ForeColor =
                DarkMode.TextPrimary;

            btnExpand.Font =
                CreateOwnedFont(
                    "Segoe UI Symbol",
                    9f);

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

            chkBulk.AutoSize =
                false;

            chkBulk.Width =
                110;

            chkBulk.ThreeState =
                true;

            chkBulk.Text =
                "Accurate";

            chkBulk.TextAlign =
                ContentAlignment.MiddleLeft;

            chkBulk.ForeColor =
                DarkMode.TextPrimary;

            chkBulk.BackColor =
                Color.Transparent;

            chkBulk.Font =
                CreateOwnedFont(
                    "Segoe UI",
                    9f);

            chkBulk.Cursor =
                Cursors.Hand;

            // ------------------------------------------------------------
            // Count Label
            // ------------------------------------------------------------

            lblCount.Dock =
                DockStyle.Right;

            lblCount.AutoSize =
                false;

            lblCount.Width =
                180;

            lblCount.ForeColor =
                DarkMode.TextSecondary;

            lblCount.BackColor =
                Color.Transparent;

            lblCount.Font =
                CreateOwnedFont(
                    "Segoe UI",
                    8.5f);

            lblCount.TextAlign =
                ContentAlignment.MiddleRight;

            lblCount.Padding =
                new Padding(
                    0,
                    0,
                    12,
                    0);

            // ------------------------------------------------------------
            // Search Term Label
            // ------------------------------------------------------------

            lblSearchTerm.Dock =
                DockStyle.Fill;

            lblSearchTerm.AutoSize =
                false;

            lblSearchTerm.ForeColor =
                DarkMode.Primary;

            lblSearchTerm.BackColor =
                Color.Transparent;

            lblSearchTerm.Font =
                CreateOwnedFont(
                    "Segoe UI",
                    10f,
                    FontStyle.Bold);

            lblSearchTerm.TextAlign =
                ContentAlignment.MiddleLeft;

            lblSearchTerm.Padding =
                new Padding(
                    8,
                    0,
                    0,
                    0);

            lblSearchTerm.AutoEllipsis =
                true;

            lblSearchTerm.Cursor =
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

            lblSearchTerm.Click +=
                Header_Click;

            lblCount.Click +=
                Header_Click;

            pnlHeader.MouseEnter +=
                Header_MouseEnter;

            pnlHeader.MouseLeave +=
                Header_MouseLeave;

            lblSearchTerm.MouseEnter +=
                Header_MouseEnter;

            lblSearchTerm.MouseLeave +=
                Header_MouseLeave;

            lblCount.MouseEnter +=
                Header_MouseEnter;

            lblCount.MouseLeave +=
                Header_MouseLeave;
        }

        public void LoadGroup(
            string sport,
            List<AnalysisTableRow> rows)
        {
            _rows.Clear();

            _rows.AddRange(
                rows);

            lblSearchTerm.Text =
                sport;

            BuildFileGroups();

            UpdateBulkCheckbox();

            PerformLayout();
        }

        private void BuildFileGroups()
        {
            pnlFiles.SuspendLayout();

            DisposeChildren(pnlFiles);

            var fileGroups =
                _rows
                    .GroupBy(
                        row =>
                            row.File)
                    .OrderBy(
                        group =>
                            group.Key)
                    .ToList();

            foreach (var group in fileGroups)
            {
                var fileControl =
                    new FileGroupControl();

                fileControl.Dock =
                    DockStyle.Top;

                fileControl.Margin =
                    new Padding(
                        0,
                        0,
                        0,
                        3);

                fileControl.LoadGroup(
                    group.Key,
                    group.ToList());

                fileControl.AccuracyChanged +=
                    ChildAccuracyChanged;

                pnlFiles.Controls.Add(
                    fileControl);

                pnlFiles.Controls.SetChildIndex(
                    fileControl,
                    0);
            }

            pnlFiles.ResumeLayout(
                true);
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

            pnlFiles.Visible =
                _expanded;

            btnExpand.Text =
                _expanded
                    ? "▼"
                    : "▶";

            PerformLayout();

            Parent?.PerformLayout();
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
                DarkMode.Surface;
        }

        private void chkBulk_CheckStateChanged(
            object? sender,
            EventArgs e)
        {
            if (!TryGetUserBulkAccuracy(
                    chkBulk,
                    ref _updatingCheckbox,
                    out bool isAccurate))
            {
                return;
            }

            foreach (var row in _rows)
            {
                row.IsAccurate =
                    isAccurate;
            }

            foreach (
                FileGroupControl fileControl
                in pnlFiles.Controls)
            {
                fileControl.RefreshFromData();
            }

            UpdateBulkCheckbox();
        }

        private void ChildAccuracyChanged(
            object? sender,
            EventArgs e)
        {
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
    }
}