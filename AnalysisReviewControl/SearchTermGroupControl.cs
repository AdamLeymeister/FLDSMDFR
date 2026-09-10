using FLDSMDFR.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Themes;

namespace AnalysisReviewControl
{
    public partial class SearchTermGroupControl : UserControl
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

            Margin =
                new Padding(0, 0, 0, 6);

            Padding =
                Padding.Empty;

            AutoSize =
                false;

            Height =
                HeaderHeight;

            MinimumSize =
                new Size(0, HeaderHeight);

            // ------------------------------------------------------------
            // Header
            // ------------------------------------------------------------

            pnlHeader.Dock =
                DockStyle.Top;

            pnlHeader.Height =
                HeaderHeight;

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

            pnlFiles.Height =
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
                DarkMode.ElevatedSurface;

            btnExpand.BackColor =
                Color.Transparent;

            btnExpand.ForeColor =
                DarkMode.TextPrimary;

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

            chkBulk.ForeColor =
                DarkMode.TextPrimary;

            chkBulk.BackColor =
                Color.Transparent;

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
            // Search Term Label
            // ------------------------------------------------------------

            lblSearchTerm.Dock =
                DockStyle.Fill;

            lblSearchTerm.ForeColor =
                DarkMode.Primary;

            lblSearchTerm.BackColor =
                Color.Transparent;

            lblSearchTerm.Font =
                new Font(
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
            string searchTerm,
            List<AnalysisTableRow> rows)
        {
            _rows.Clear();
            _rows.AddRange(rows);

            lblSearchTerm.Text =
                searchTerm;

            BuildFileGroups();

            UpdateBulkCheckbox();

            UpdateHeight();
        }

        private void BuildFileGroups()
        {
            pnlFiles.SuspendLayout();

            pnlFiles.Controls.Clear();

            var fileGroups =
                _rows
                    .GroupBy(row => row.File)
                    .OrderBy(group => group.Key)
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

                // This is the inherited WinForms HeightChanged event.
                //
                // When a FileGroupControl expands/collapses,
                // SearchTermGroupControl needs to recalculate its
                // own height.
                fileControl.SizeChanged +=
                    FileControl_SizeChanged;

                pnlFiles.Controls.Add(
                    fileControl);

                pnlFiles.Controls.SetChildIndex(
                    fileControl,
                    0);
            }

            pnlFiles.ResumeLayout();
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

            if (!_expanded)
            {
                UpdateHeight();
                Parent?.PerformLayout();

                return;
            }

            BeginInvoke(
                new Action(() =>
                {
                    pnlFiles.PerformLayout();

                    UpdateHeight();

                    Parent?.PerformLayout();
                }));
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
            if (_updatingCheckbox)
            {
                return;
            }

            // Indeterminate represents a mixed group.
            //
            // It should never be applied to the children.
            if (chkBulk.CheckState ==
                CheckState.Indeterminate)
            {
                return;
            }

            bool isAccurate =
                chkBulk.CheckState ==
                CheckState.Checked;

            // Update every underlying raw record.
            foreach (var row in _rows)
            {
                row.IsAccurate =
                    isAccurate;
            }

            // Tell every FileGroupControl to refresh
            // from those underlying records.
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
            // A child changed, so recalculate the
            // Search Term tri-state checkbox.
            UpdateBulkCheckbox();
        }

        private void FileControl_SizeChanged(
            object? sender,
            EventArgs e)
        {
            if (!_expanded)
            {
                return;
            }

            BeginInvoke(
                new Action(() =>
                {
                    pnlFiles.PerformLayout();

                    UpdateHeight();

                    Parent?.PerformLayout();
                }));
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
            if (!_expanded)
            {
                pnlFiles.Height = 0;

                if (Height != HeaderHeight)
                {
                    Height = HeaderHeight;
                }

                return;
            }

            pnlFiles.PerformLayout();

            int contentBottom =
                pnlFiles.Padding.Top;

            foreach (Control control in pnlFiles.Controls)
            {
                contentBottom =
                    Math.Max(
                        contentBottom,
                        control.Bottom);
            }

            int filesHeight =
                contentBottom +
                pnlFiles.Padding.Bottom;

            if (pnlFiles.Height != filesHeight)
            {
                pnlFiles.Height =
                    filesHeight;
            }

            int newHeight =
                HeaderHeight +
                filesHeight;

            if (Height != newHeight)
            {
                Height =
                    newHeight;
            }
        }
    }
}