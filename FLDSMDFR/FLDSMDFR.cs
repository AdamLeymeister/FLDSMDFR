using FLDSMDFR.Themes;

namespace FLDSMDFR;

public partial class FLDSMDFR : Form
{
    public FLDSMDFR()
    {
        InitializeComponent();

        BackColor = DarkMode.Background;
        ForeColor = DarkMode.TextPrimary;

        // sidePanel.BackColor = AppColors.Surface;
        // topPanel.BackColor = AppColors.Surface;

        // mainPanel.BackColor = AppColors.Background;

        // analyticsPanel.BackColor = AppColors.ElevatedSurface;
        // btnRun.BackColor = AppColors.Primary;
        // btnRun.ForeColor = AppColors.Background;
        // btnRun.FlatStyle = FlatStyle.Flat;
        // btnRun.FlatAppearance.BorderSize = 0;
        // titleLabel.ForeColor = AppColors.TextPrimary;
        // descriptionLabel.ForeColor = AppColors.TextSecondary;

        ShowDashboard();

    }

    private void ShowDashboard()
    {
        pnlMain.Controls.Clear();

        var dashboard = new DashboardView
        {
            Dock = DockStyle.Fill
        };

        pnlMain.Controls.Add(dashboard);
    }

    private void FLDSMDFR_Load(object sender, EventArgs e)
    {

    }
}