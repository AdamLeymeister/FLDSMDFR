using FLDSMDFR.Themes;

namespace FLDSMDFR;

public partial class UtilitiesView : UserControl
{
    public UtilitiesView()
    {
        InitializeComponent();
        ConfigureView();
    }

    private void ConfigureView()
    {
        ModernUi.StylePage(this, pnlContent, tlpUtilities);

        ModernUi.FillCard(
            pnlCardOverview,
            "Utilities",
            "Maintenance tools and stuff.");

        ModernUi.FillCard(
            pnlUtilities,
            "Workspace",
            "Exports, cleanup, and batch helpers will appear in this card.");
    }
}
