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
        host.BackColor = Color.Transparent;
        host.Padding = Padding.Empty;

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var subtitleLabel = new Label
        {
            Text = subtitle,
            Dock = DockStyle.Top,
            Height = metric == null ? 44 : 22,
            Font = BodyFont,
            ForeColor = DarkMode.TextDisabled,
            BackColor = Color.Transparent
        };
        card.Controls.Add(subtitleLabel);

        if (metric != null)
        {
            card.Controls.Add(new Label
            {
                Text = metric,
                Dock = DockStyle.Top,
                Height = 48,
                Font = MetricFont,
                ForeColor = metricColor ?? DarkMode.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            });
        }

        card.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 28,
            Font = TitleFont,
            ForeColor = DarkMode.TextPrimary,
            BackColor = Color.Transparent
        });

        host.Controls.Add(card);
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
}
