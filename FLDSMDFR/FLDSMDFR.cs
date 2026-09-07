using FLDSMDFR.Themes;

namespace FLDSMDFR;

public partial class FLDSMDFR : Form
{
    public FLDSMDFR()
    {
        InitializeComponent();

        ConfigureForm();
        ConfigureNavigation();

        SetActiveNavButton(btnDashboard);
        ShowView(new DashboardView());
    }

    private void ConfigureForm()
    {
        BackColor = DarkMode.Background;
        ForeColor = DarkMode.TextPrimary;

        pnlMain.BackColor = DarkMode.Background;
        pnlSideBar.BackColor = DarkMode.Surface;
        pnlTopBar.BackColor = DarkMode.Surface;
    }

    private void ConfigureNavigation()
    {
        ConfigureNavButton(btnDashboard);
        ConfigureNavButton(btnImport);
        ConfigureNavButton(btnUtilities);
    }

    private void ConfigureNavButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;

        button.FlatAppearance.MouseOverBackColor = DarkMode.HoverSurface;
        button.FlatAppearance.MouseDownBackColor = DarkMode.ElevatedSurface;

        button.BackColor = DarkMode.Surface;
        button.ForeColor = DarkMode.TextSecondary;

        button.Cursor = Cursors.Hand;
    }

    private void SetActiveNavButton(Button activeButton)
    {
        Button[] navButtons =
        {
            btnDashboard,
            btnImport,
            btnUtilities
        };

        foreach (Button button in navButtons)
        {
            button.BackColor = DarkMode.Surface;
            button.ForeColor = DarkMode.TextSecondary;
        }

        activeButton.BackColor = DarkMode.HoverSurface;
        activeButton.ForeColor = DarkMode.Primary;
    }

    private void ShowView(UserControl view)
    {
        pnlMain.SuspendLayout();

        pnlMain.Controls.Clear();

        view.Dock = DockStyle.Fill;

        pnlMain.Controls.Add(view);

        pnlMain.ResumeLayout();
    }

    private void btnDashboard_Click(object sender, EventArgs e)
    {
        SetActiveNavButton(btnDashboard);
        ShowView(new DashboardView());
    }

    private void btnImport_Click(object sender, EventArgs e)
    {
        SetActiveNavButton(btnImport);
        ShowView(new ImportView());
    }

    private void FLDSMDFR_Load(object sender, EventArgs e)
    {
    }

    private void btnUtilities_Click(object sender, EventArgs e)
    {
        SetActiveNavButton(btnUtilities);
        ShowView(new UtilitiesView());
    }
}