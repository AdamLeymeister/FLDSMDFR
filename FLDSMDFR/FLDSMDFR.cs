using System.Runtime.InteropServices;
using FLDSMDFR.Themes;

namespace FLDSMDFR;

public partial class FLDSMDFR : Form
{
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam);

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;

    public FLDSMDFR()
    {
        InitializeComponent();

        ConfigureForm();
        ConfigureNavigation();
        ConfigureTopBar();

        SetActiveNavButton(btnDashboard);
        ShowView(new DashboardView());
    }

    private void ConfigureForm()
    {
        FormBorderStyle = FormBorderStyle.None;

        BackColor = DarkMode.Background;
        ForeColor = DarkMode.TextPrimary;

        pnlMain.BackColor = DarkMode.Background;
        pnlSideBar.BackColor = DarkMode.Surface;
        pnlTopBar.BackColor = DarkMode.Surface;
    }

    private void ConfigureTopBar()
    {
        pnlTopBar.Height = 42;
        pnlTopBar.Dock = DockStyle.Top;
        pnlTopBar.BackColor = DarkMode.Surface;

        lblTitle.Text = "FLDSMDFR";
        lblTitle.ForeColor = DarkMode.TextPrimary;
        lblTitle.BackColor = Color.Transparent;
        lblTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

        ConfigureWindowButton(btnMinimize);
        ConfigureWindowButton(btnMaximize);
        ConfigureWindowButton(btnClose);

        btnClose.FlatAppearance.MouseOverBackColor = DarkMode.Error;
        btnClose.FlatAppearance.MouseDownBackColor = DarkMode.Error;

        pnlTopBar.MouseDown += TopBar_MouseDown;
        lblTitle.MouseDown += TopBar_MouseDown;
    }

    private void ConfigureWindowButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;

        button.BackColor = DarkMode.Surface;
        button.ForeColor = DarkMode.TextPrimary;

        button.FlatAppearance.MouseOverBackColor = DarkMode.HoverSurface;
        button.FlatAppearance.MouseDownBackColor = DarkMode.ElevatedSurface;

        button.Dock = DockStyle.Right;
        button.Width = 46;

        button.TabStop = false;
        button.Cursor = Cursors.Hand;
    }

    private void TopBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        ReleaseCapture();

        SendMessage(
            Handle,
            WM_NCLBUTTONDOWN,
            (IntPtr)HT_CAPTION,
            IntPtr.Zero);
    }

    private void btnMinimize_Click(object sender, EventArgs e)
    {
        WindowState = FormWindowState.Minimized;
    }

    private void btnMaximize_Click(object sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Maximized)
        {
            WindowState = FormWindowState.Normal;
            btnMaximize.Text = "□";
        }
        else
        {
            WindowState = FormWindowState.Maximized;
            btnMaximize.Text = "❐";
        }
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
        Close();
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

    private void label1_Click(object sender, EventArgs e)
    {

    }
}