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
    private Button? _activeNavButton;
    private readonly Panel _navIndicator = new();
    private readonly Font _navFont = new("Segoe UI", 9.5f);
    private readonly Font _navFontActive = new("Segoe UI", 9.5f, FontStyle.Bold);
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

        pnlTopBar.Paint += TopBar_Paint;
        pnlSideBar.Paint += SideBar_Paint;
        pnlSideBar.Resize += (_, _) => UpdateNavIndicator();
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

        lblTitle.AutoSize = true;
        lblTitle.Location = new Point(16, 12);
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

    private void TopBar_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(DarkMode.Border);
        e.Graphics.DrawLine(
            pen,
            0,
            pnlTopBar.Height - 1,
            pnlTopBar.Width,
            pnlTopBar.Height - 1);
    }

    private void SideBar_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(DarkMode.Border);
        e.Graphics.DrawLine(
            pen,
            pnlSideBar.Width - 1,
            0,
            pnlSideBar.Width - 1,
            pnlSideBar.Height);
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

        ConfigureSidebarBrand();
        ConfigureNavIndicator();
    }

    private void ConfigureSidebarBrand()
    {
        var brand = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = DarkMode.Surface,
            Padding = new Padding(18, 18, 12, 12)
        };

        var title = new Label
        {
            Text = "FLDSMDFR",
            Dock = DockStyle.Top,
            Height = 22,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = DarkMode.TextPrimary,
            BackColor = Color.Transparent
        };

        var subtitle = new Label
        {
            Text = "Match review",
            Dock = DockStyle.Top,
            Height = 18,
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = DarkMode.TextDisabled,
            BackColor = Color.Transparent
        };

        brand.Controls.Add(subtitle);
        brand.Controls.Add(title);
        brand.Paint += (_, e) =>
        {
            using var pen = new Pen(DarkMode.Border);
            e.Graphics.DrawLine(pen, 16, brand.Height - 1, brand.Width - 16, brand.Height - 1);
        };

        pnlSideBar.Controls.Add(brand);
        brand.BringToFront();
    }

    private void ConfigureNavIndicator()
    {
        _navIndicator.Width = 3;
        _navIndicator.Height = 22;
        _navIndicator.BackColor = DarkMode.Primary;
        pnlSideBar.Controls.Add(_navIndicator);
        _navIndicator.BringToFront();
        UpdateNavIndicator();
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
        button.Font = _navFont;
        button.Height = 48;
        button.Padding = new Padding(22, 0, 12, 0);
        button.TextAlign = ContentAlignment.MiddleLeft;

        button.Cursor = Cursors.Hand;
        button.TabStop = false;

        button.UseVisualStyleBackColor = false;
        button.Resize += (_, _) => UpdateNavIndicator();
    }

    private void SetActiveNavButton(Button activeButton)
    {
        _activeNavButton = activeButton;

        foreach (Button button in _navButtons)
        {
            bool active = button == activeButton;
            button.BackColor = active
                ? DarkMode.HoverSurface
                : DarkMode.Surface;
            button.ForeColor = active
                ? DarkMode.TextPrimary
                : DarkMode.TextSecondary;
            button.Font = active ? _navFontActive : _navFont;
        }

        UpdateNavIndicator();
    }

    private void UpdateNavIndicator()
    {
        if (_activeNavButton == null)
        {
            return;
        }

        pnlSideBar.PerformLayout();
        _navIndicator.Left = 8;
        _navIndicator.Top = _activeNavButton.Top + (_activeNavButton.Height - _navIndicator.Height) / 2;
        _navIndicator.BringToFront();
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