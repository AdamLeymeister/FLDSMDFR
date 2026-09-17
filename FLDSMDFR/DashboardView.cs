using AnalysisReviewControl;
using FLDSMDFR.Core.Models;
using FLDSMDFR.Themes;

namespace FLDSMDFR;

public partial class DashboardView : UserControl
{
    private ImportView? _import;
    private readonly Label _lblPercent = CreateMetricLabel("0%");
    private readonly Label _lblStatus = CreateBodyLabel("Import a JSON file to start review.");
    private readonly Label _lblEta = CreateMetricLabel("—");
    private readonly Label _lblEtaDetail = CreateBodyLabel("ETA appears after a few reviews.");
    private readonly Label _lblFalsePositives = CreateMetricLabel("0");
    private readonly Label _lblConfirmed = CreateMetricLabel("0");
    private readonly Label _lblRemaining = CreateMetricLabel("0");
    private readonly ThemedProgressBar _progress = new();

    public DashboardView()
    {
        InitializeComponent();
        ConfigureView();
        VisibleChanged += (_, _) =>
        {
            if (Visible)
            {
                RefreshProgress();
            }
        };
    }

    public void Bind(ImportView import)
    {
        if (ReferenceEquals(_import, import))
        {
            RefreshProgress();
            return;
        }

        if (_import != null)
        {
            _import.ReviewChanged -= Import_ReviewChanged;
        }

        _import = import;
        _import.ReviewChanged += Import_ReviewChanged;
        RefreshProgress();
    }

    private void Import_ReviewChanged(object? sender, EventArgs e)
    {
        if (IsHandleCreated)
        {
            BeginInvoke(new Action(RefreshProgress));
        }
    }

    private void ConfigureView()
    {
        ModernUi.StylePage(this, pnlContent, tlpDashboard);

        tlpDashboard.Controls.Clear();
        tlpDashboard.ColumnStyles.Clear();
        tlpDashboard.RowStyles.Clear();
        tlpDashboard.ColumnCount = 3;
        tlpDashboard.RowCount = 2;
        tlpDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333f));
        tlpDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333f));
        tlpDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33334f));
        tlpDashboard.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));
        tlpDashboard.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));

        tlpDashboard.Controls.Add(pnlCardOverview, 0, 0);
        tlpDashboard.SetColumnSpan(pnlCardOverview, 3);
        tlpDashboard.Controls.Add(pnlCardActivity, 0, 1);
        tlpDashboard.Controls.Add(pnlCardStatus, 1, 1);
        tlpDashboard.Controls.Add(pnlCard4, 2, 1);

        BuildProgressCard(pnlCardOverview);
        BuildStatCard(
            pnlCardActivity,
            "False positives",
            "Hits marked denied.",
            _lblFalsePositives,
            DarkMode.Error);
        BuildStatCard(
            pnlCardStatus,
            "Not false positives",
            "Hits marked accurate.",
            _lblConfirmed,
            DarkMode.Success);
        BuildStatCard(
            pnlCard4,
            "Left to review",
            "Hits still pending.",
            _lblRemaining,
            DarkMode.Warning);

        RefreshProgress();
    }

    private void BuildProgressCard(Panel host)
    {
        host.BackColor = DarkMode.Background;
        host.Padding = Padding.Empty;
        host.Margin = new Padding(8);

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 24, 28, 24),
            BackdropColor = DarkMode.Background,
            FillColor = DarkMode.Surface
        };

        var title = CreateCaptionLabel("IMPORT REVIEW");
        var heading = new Label
        {
            Text = "Review progress",
            AutoSize = true,
            Font = ModernUi.TitleFont,
            ForeColor = DarkMode.TextPrimary,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 12)
        };

        _lblPercent.Margin = new Padding(0, 0, 0, 4);
        _lblStatus.Margin = new Padding(0, 0, 0, 18);

        _progress.Height = 18;
        _progress.Margin = new Padding(0, 0, 0, 22);

        var etaCaption = CreateCaptionLabel("ESTIMATED TIME LEFT");
        _lblEta.Margin = new Padding(0, 6, 0, 4);
        _lblEta.ForeColor = DarkMode.Primary;

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };

        stack.Controls.Add(title);
        stack.Controls.Add(heading);
        stack.Controls.Add(_lblPercent);
        stack.Controls.Add(_lblStatus);
        stack.Controls.Add(_progress);
        stack.Controls.Add(etaCaption);
        stack.Controls.Add(_lblEta);
        stack.Controls.Add(_lblEtaDetail);

        void Fit()
        {
            int width = Math.Max(40, stack.ClientSize.Width);
            foreach (Control child in stack.Controls)
            {
                child.Width = width;
                if (child != _progress)
                {
                    child.MaximumSize = new Size(width, 0);
                    child.MinimumSize = new Size(width, 0);
                }
            }

            _progress.Width = width;
        }

        stack.Resize += (_, _) => Fit();
        card.Controls.Add(stack);
        host.Controls.Add(card);
        Fit();
    }

    private static void BuildStatCard(
        Panel host,
        string title,
        string subtitle,
        Label metric,
        Color metricColor)
    {
        host.BackColor = DarkMode.Background;
        host.Padding = Padding.Empty;
        host.Margin = new Padding(8);

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            BackdropColor = DarkMode.Background,
            FillColor = DarkMode.Surface
        };

        metric.ForeColor = metricColor;
        metric.Margin = new Padding(0, 10, 0, 8);

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        stack.Controls.Add(CreateCaptionLabel(title.ToUpperInvariant()));
        stack.Controls.Add(metric);
        stack.Controls.Add(CreateBodyLabel(subtitle));

        void Fit()
        {
            int width = Math.Max(32, stack.ClientSize.Width);
            foreach (Control child in stack.Controls)
            {
                child.MaximumSize = new Size(width, 0);
                child.MinimumSize = new Size(width, 0);
            }
        }

        stack.Resize += (_, _) => Fit();
        card.Controls.Add(stack);
        host.Controls.Add(card);
        Fit();
    }

    private void RefreshProgress()
    {
        ReviewProgressSnapshot progress = _import?.GetProgress() ?? new ReviewProgressSnapshot();

        _lblFalsePositives.Text = progress.Denied.ToString("N0");
        _lblConfirmed.Text = progress.Accurate.ToString("N0");
        _lblRemaining.Text = progress.Pending.ToString("N0");
        _progress.Value = progress.PercentComplete / 100d;

        if (!progress.HasData)
        {
            _lblPercent.Text = "0%";
            _lblStatus.Text = "Import a JSON file on the Import tab to begin review.";
            _lblEta.Text = "—";
            _lblEtaDetail.Text = "ETA appears after a few reviews.";
            return;
        }

        _lblPercent.Text = $"{progress.PercentComplete:0.#}%";

        if (progress.IsComplete)
        {
            _lblStatus.Text = $"{progress.Total:N0} hits reviewed. Import review is complete.";
            _lblEta.Text = "Complete";
            _lblEtaDetail.Text = "No remaining hits.";
            _progress.FillColor = DarkMode.Success;
            return;
        }

        _lblStatus.Text =
            $"{progress.Accurate + progress.Denied:N0} of {progress.Total:N0} hits reviewed  ·  {progress.Pending:N0} left";
        _progress.FillColor = DarkMode.Primary;

        if (progress.EstimatedRemaining is not { } remaining)
        {
            _lblEta.Text = "—";
            _lblEtaDetail.Text = "Review a few more hits to estimate completion.";
            return;
        }

        _lblEta.Text = FormatRemaining(remaining);
        DateTime eta = DateTime.Now + remaining;
        _lblEtaDetail.Text = remaining.TotalHours >= 6
            ? $"Based on remaining hits and current pace  ·  around {eta:ddd, MMM d}"
            : "Based on remaining hits and the current review pace.";
    }

    private static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining.TotalDays >= 1)
        {
            return $"About {remaining.TotalDays:0.#} days";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"About {remaining.TotalHours:0.#} hours";
        }

        int minutes = Math.Max(1, (int)Math.Round(remaining.TotalMinutes));
        return $"About {minutes} minute{(minutes == 1 ? "" : "s")}";
    }

    private static Label CreateMetricLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = ModernUi.MetricFont,
            ForeColor = DarkMode.TextPrimary,
            BackColor = Color.Transparent,
            UseMnemonic = false
        };
    }

    private static Label CreateBodyLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = ModernUi.BodyFont,
            ForeColor = DarkMode.TextDisabled,
            BackColor = Color.Transparent,
            UseMnemonic = false
        };
    }

    private static Label CreateCaptionLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = ModernUi.CaptionFont,
            ForeColor = DarkMode.TextDisabled,
            BackColor = Color.Transparent,
            UseMnemonic = false
        };
    }

    private sealed class ThemedProgressBar : Control
    {
        private double _value;

        public ThemedProgressBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            Height = 18;
            FillColor = DarkMode.Primary;
        }

        public Color FillColor { get; set; }

        public double Value
        {
            get => _value;
            set
            {
                _value = Math.Clamp(value, 0, 1);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var track = new SolidBrush(DarkMode.ElevatedSurface);
            using var fill = new SolidBrush(FillColor);
            using var path = CreateRoundRect(bounds, Height / 2);
            e.Graphics.FillPath(track, path);

            int fillWidth = (int)Math.Round((Width - 1) * _value);
            if (fillWidth <= 1)
            {
                return;
            }

            var fillBounds = new Rectangle(0, 0, Math.Max(fillWidth, Height), Height - 1);
            using var fillPath = CreateRoundRect(fillBounds, Height / 2);
            e.Graphics.FillPath(fill, fillPath);
        }

        private static System.Drawing.Drawing2D.GraphicsPath CreateRoundRect(Rectangle bounds, int radius)
        {
            int d = Math.Max(2, radius * 2);
            if (d > bounds.Width)
            {
                d = Math.Max(2, bounds.Width);
            }

            if (d > bounds.Height)
            {
                d = Math.Max(2, bounds.Height);
            }

            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
