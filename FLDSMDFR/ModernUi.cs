using AnalysisReviewControl;
using FLDSMDFR.Themes;

namespace FLDSMDFR;

internal static class ModernUi
{
    public static readonly Font UiFont = new("Segoe UI", 9.5f);
    public static readonly Font TitleFont = new("Segoe UI", 13f, FontStyle.Bold);
    public static readonly Font CaptionFont = new("Segoe UI", 7.5f, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 9f);
    public static readonly Font MetricFont = new("Segoe UI", 28f, FontStyle.Bold);

    public static void StylePage(Control view, Panel content, TableLayoutPanel table)
    {
        if (view is ContainerControl container)
        {
            container.AutoScaleMode = AutoScaleMode.None;
        }

        view.BackColor = DarkMode.Background;
        view.ForeColor = DarkMode.TextPrimary;
        view.Font = UiFont;
        content.BackColor = DarkMode.Background;
        content.Padding = new Padding(16);
        table.BackColor = DarkMode.Background;
    }

    public static RoundedCardPanel FillCard(
        Panel host,
        string title,
        string subtitle,
        string? metric = null,
        Color? metricColor = null)
    {
        host.BackColor = DarkMode.Background;
        host.Padding = Padding.Empty;

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            BackdropColor = DarkMode.Background,
            FillColor = DarkMode.Surface
        };

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        stack.Controls.Add(CreateWrappingLabel(title, TitleFont, DarkMode.TextPrimary, 4));

        if (metric != null)
        {
            stack.Controls.Add(CreateWrappingLabel(
                metric,
                MetricFont,
                metricColor ?? DarkMode.TextPrimary,
                8));
        }

        stack.Controls.Add(CreateWrappingLabel(subtitle, BodyFont, DarkMode.TextDisabled, 0));

        void FitLabels()
        {
            int width = Math.Max(32, stack.ClientSize.Width);
            foreach (Control child in stack.Controls)
            {
                child.MaximumSize = new Size(width, 0);
                child.MinimumSize = new Size(width, 0);
            }
        }

        stack.Resize += (_, _) => FitLabels();
        card.Controls.Add(stack);
        host.Controls.Add(card);
        FitLabels();
        return card;
    }

    public static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = DarkMode.PrimaryHover;
        button.FlatAppearance.MouseDownBackColor = DarkMode.PrimaryMuted;
        button.BackColor = DarkMode.Primary;
        button.ForeColor = DarkMode.Background;
        button.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.TabStop = false;
        button.UseVisualStyleBackColor = false;
        button.Height = 36;
        button.TextAlign = ContentAlignment.MiddleCenter;
    }

    private static Label CreateWrappingLabel(
        string text,
        Font font,
        Color color,
        int bottomMargin)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = font,
            ForeColor = color,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, bottomMargin),
            UseMnemonic = false
        };
    }
}
