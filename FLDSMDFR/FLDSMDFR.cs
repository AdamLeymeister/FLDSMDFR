using System.Runtime.InteropServices;
using FLDSMDFR.Themes;

namespace FLDSMDFR;

public partial class FLDSMDFR : Form
{
    // -------------------------------------------------------------------------
    // Windows API
    // -------------------------------------------------------------------------

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam);

    // -------------------------------------------------------------------------
    // Native Window Messages / Hit Testing
    // -------------------------------------------------------------------------

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_NCHITTEST = 0x0084;

    private const int HT_CAPTION = 0x0002;

    private const int HTCLIENT = 1;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    // -------------------------------------------------------------------------
    // Native Window Styles
    // -------------------------------------------------------------------------

    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_MAXIMIZEBOX = 0x00010000;

    private const int ResizeBorderSize = 10;

    // -------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------

    private Button[] _navButtons = null!;
    private DashboardView? _dashboardView;
    private ImportView? _importView;
    private UtilitiesView? _utilitiesView;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public FLDSMDFR()
    {
        InitializeComponent();

        ConfigureForm();
        ConfigureTopBar();
        ConfigureNavigation();

        SetActiveNavButton(btnDashboard);
        ShowView(GetDashboardView());
    }

    // -------------------------------------------------------------------------
    // Native Window Configuration
    // -------------------------------------------------------------------------

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;

            cp.Style |= WS_THICKFRAME;
            cp.Style |= WS_MINIMIZEBOX;
            cp.Style |= WS_MAXIMIZEBOX;

            return cp;
        }
    }

    // -------------------------------------------------------------------------
    // Form Configuration
    // -------------------------------------------------------------------------

    private void ConfigureForm()
    {
        FormBorderStyle = FormBorderStyle.None;

        BackColor = DarkMode.Background;
        ForeColor = DarkMode.TextPrimary;

        pnlMain.BackColor = DarkMode.Background;
        pnlSideBar.BackColor = DarkMode.Surface;
        pnlTopBar.BackColor = DarkMode.Surface;

        MinimumSize = new Size(900, 600);

        SetStyle(
            ControlStyles.ResizeRedraw,
            true);
    }

    // -------------------------------------------------------------------------
    // Custom Resize Hit Testing
    // -------------------------------------------------------------------------

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST &&
            WindowState != FormWindowState.Maximized)
        {
            base.WndProc(ref m);

            Point mousePosition = PointToClient(Cursor.Position);

            bool left =
                mousePosition.X >= 0 &&
                mousePosition.X <= ResizeBorderSize;

            bool right =
                mousePosition.X <= ClientSize.Width &&
                mousePosition.X >= ClientSize.Width - ResizeBorderSize;

            bool top =
                mousePosition.Y >= 0 &&
                mousePosition.Y <= ResizeBorderSize;

            bool bottom =
                mousePosition.Y <= ClientSize.Height &&
                mousePosition.Y >= ClientSize.Height - ResizeBorderSize;

            if (left && top)
            {
                m.Result = (IntPtr)HTTOPLEFT;
                return;
            }

            if (right && top)
            {
                m.Result = (IntPtr)HTTOPRIGHT;
                return;
            }

            if (left && bottom)
            {
                m.Result = (IntPtr)HTBOTTOMLEFT;
                return;
            }

            if (right && bottom)
            {
                m.Result = (IntPtr)HTBOTTOMRIGHT;
                return;
            }

            if (left)
            {
                m.Result = (IntPtr)HTLEFT;
                return;
            }

            if (right)
            {
                m.Result = (IntPtr)HTRIGHT;
                return;
            }

            if (top)
            {
                m.Result = (IntPtr)HTTOP;
                return;
            }

            if (bottom)
            {
                m.Result = (IntPtr)HTBOTTOM;
                return;
            }

            return;
        }

        base.WndProc(ref m);
    }

    // -------------------------------------------------------------------------
    // Top Bar
    // -------------------------------------------------------------------------

    private void ConfigureTopBar()
    {
        pnlTopBar.Height = 42;
        pnlTopBar.Dock = DockStyle.Top;
        pnlTopBar.BackColor = DarkMode.Surface;

        ConfigureTitle();

        ConfigureWindowButton(btnMinimize);
        ConfigureWindowButton(btnMaximize);
        ConfigureWindowButton(btnClose);

        btnMinimize.Text = "—";
        btnMaximize.Text = "□";
        btnClose.Text = "×";

        // Destructive close hover.
        btnClose.FlatAppearance.MouseOverBackColor =
            DarkMode.Error;

        btnClose.FlatAppearance.MouseDownBackColor =
            DarkMode.Error;

        // Wire window control events.
        btnMinimize.Click += btnMinimize_Click;
        btnMaximize.Click += btnMaximize_Click;
        btnClose.Click += btnClose_Click;

        // Allow dragging from the top bar/title.
        pnlTopBar.MouseDown += TopBar_MouseDown;
        lblTitle.MouseDown += TopBar_MouseDown;

        // Double-click title bar to maximize/restore.
        pnlTopBar.DoubleClick += TopBar_DoubleClick;
        lblTitle.DoubleClick += TopBar_DoubleClick;
    }

    private void ConfigureTitle()
    {
        lblTitle.Text = "FLDSMDFR";

        lblTitle.ForeColor = DarkMode.TextPrimary;
        lblTitle.BackColor = Color.Transparent;

        lblTitle.Font = new Font(
            "Segoe UI",
            10f,
            FontStyle.Bold);
    }

    private void ConfigureWindowButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;

        button.FlatAppearance.BorderSize = 0;

        button.FlatAppearance.MouseOverBackColor =
            DarkMode.HoverSurface;

        button.FlatAppearance.MouseDownBackColor =
            DarkMode.ElevatedSurface;

        button.BackColor = DarkMode.Surface;
        button.ForeColor = DarkMode.TextSecondary;

        button.Font = new Font(
            "Segoe UI",
            10f,
            FontStyle.Regular);

        button.Width = 46;
        button.Height = 42;

        button.Margin = Padding.Empty;
        button.Padding = Padding.Empty;

        button.TabStop = false;
        button.Cursor = Cursors.Hand;

        button.UseVisualStyleBackColor = false;
    }

    // -------------------------------------------------------------------------
    // Top Bar Movement
    // -------------------------------------------------------------------------

    private void TopBar_MouseDown(
        object? sender,
        MouseEventArgs e)
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

    private void TopBar_DoubleClick(
        object? sender,
        EventArgs e)
    {
        ToggleMaximize();
    }

    // -------------------------------------------------------------------------
    // Window Buttons
    // -------------------------------------------------------------------------

    private void btnMinimize_Click(
        object? sender,
        EventArgs e)
    {
        WindowState = FormWindowState.Minimized;
    }

    private void btnMaximize_Click(
        object? sender,
        EventArgs e)
    {
        ToggleMaximize();
    }

    private void btnClose_Click(
        object? sender,
        EventArgs e)
    {
        Close();
    }

    private void ToggleMaximize()
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

    // -------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------

    private void ConfigureNavigation()
    {
        _navButtons = new Button[]
        {
            btnDashboard,
            btnImport,
            btnUtilities
        };

        foreach (Button button in _navButtons)
        {
            ConfigureNavButton(button);
        }
    }

    private void ConfigureNavButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;

        button.FlatAppearance.BorderSize = 0;

        button.FlatAppearance.MouseOverBackColor =
            DarkMode.HoverSurface;

        button.FlatAppearance.MouseDownBackColor =
            DarkMode.ElevatedSurface;

        button.BackColor = DarkMode.Surface;
        button.ForeColor = DarkMode.TextSecondary;

        button.Cursor = Cursors.Hand;
        button.TabStop = false;

        button.UseVisualStyleBackColor = false;
    }

    private void SetActiveNavButton(Button activeButton)
    {
        foreach (Button button in _navButtons)
        {
            button.BackColor = DarkMode.Surface;
            button.ForeColor = DarkMode.TextSecondary;
        }

        activeButton.BackColor = DarkMode.HoverSurface;
        activeButton.ForeColor = DarkMode.Primary;
    }

    // -------------------------------------------------------------------------
    // View Navigation
    // -------------------------------------------------------------------------

    private void ShowView(UserControl view)
    {
        pnlMain.SuspendLayout();

        foreach (Control existing in pnlMain.Controls)
        {
            existing.Visible = existing == view;
        }

        if (!pnlMain.Controls.Contains(view))
        {
            view.Dock = DockStyle.Fill;
            view.BackColor = DarkMode.Background;
            pnlMain.Controls.Add(view);
        }

        view.Visible = true;
        view.BringToFront();

        pnlMain.ResumeLayout();
    }

    private void NavigateTo(
        Button button,
        UserControl view)
    {
        SetActiveNavButton(button);
        ShowView(view);
    }

    private DashboardView GetDashboardView()
    {
        return _dashboardView ??= new DashboardView();
    }

    private ImportView GetImportView()
    {
        return _importView ??= new ImportView();
    }

    private UtilitiesView GetUtilitiesView()
    {
        return _utilitiesView ??= new UtilitiesView();
    }

    // -------------------------------------------------------------------------
    // Navigation Events
    // -------------------------------------------------------------------------

    private void btnDashboard_Click(
        object sender,
        EventArgs e)
    {
        NavigateTo(
            btnDashboard,
            GetDashboardView());
    }

    private void btnImport_Click(
        object sender,
        EventArgs e)
    {
        NavigateTo(
            btnImport,
            GetImportView());
    }

    private void btnUtilities_Click(
        object sender,
        EventArgs e)
    {
        NavigateTo(
            btnUtilities,
            GetUtilitiesView());
    }
}