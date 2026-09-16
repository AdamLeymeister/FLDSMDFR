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
            "Imported matches and review progress will land here.",
            "—",
            DarkMode.TextPrimary);

        ModernUi.FillCard(
            pnlCardActivity,
            "Activity",
            "Confirm, deny, and undo actions from the Import table.",
            "Ready",
            DarkMode.Secondary);

        ModernUi.FillCard(
            pnlCardStatus,
            "Status",
            "Accurate stays green. Denied stays red. Remaining stays in review.",
            "Review",
            DarkMode.Warning);

        ModernUi.FillCard(
            pnlCard4,
            "Coverage",
            "Sports and files stay nested in the review table until you expand them.");

        ModernUi.FillCard(
            pnlCard5,
            "Shortcuts",
            "Y confirm  ·  N deny  ·  Space cycle  ·  Ctrl+Z undo  ·  F3 next");

        ModernUi.FillCard(
            pnlCard6,
            "Workspace",
            "Utilities and exports will use the same card layout as Import.");
    }
}
