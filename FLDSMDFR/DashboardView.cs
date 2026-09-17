using FLDSMDFR.Themes;

namespace FLDSMDFR;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        ConfigureView();
    }

    private void ConfigureView()
    {
        ModernUi.StylePage(this, pnlContent, tlpDashboard);

        ModernUi.FillCard(
            pnlCardOverview,
            "Overview",
            "Progress bar and ETA estimates.",
            "—",
            DarkMode.TextPrimary);

        ModernUi.FillCard(
            pnlCardActivity,
            "Activity",
            "TBD",
            "Ready",
            DarkMode.Secondary);

        ModernUi.FillCard(
            pnlCardStatus,
            "Status",
            "Time Left",
            "Review",
            DarkMode.Warning);

        ModernUi.FillCard(
            pnlCard4,
            "Coverage",
            "TBD");

        ModernUi.FillCard(
            pnlCard5,
            "Shortcuts",
            "Customization Options");

        ModernUi.FillCard(
            pnlCard6,
            "Workspace",
            "TBD");
    }
}
