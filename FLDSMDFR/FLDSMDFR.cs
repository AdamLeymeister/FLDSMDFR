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

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    private static extern bool DwmDefWindowProc(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        out IntPtr result);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attr,
        ref int attrValue,
        int attrSize);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_NCHITTEST = 0x0084;
    private const int WM_NCCALCSIZE = 0x0083;
    private const int WM_NCACTIVATE = 0x0086;
    private const int WM_DWMCOMPOSITIONCHANGED = 0x031E;

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

    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_MAXIMIZEBOX = 0x00010000;
    private const int WS_EX_DLGMODALFRAME = 0x00000001;
    private const int WS_EX_WINDOWEDGE = 0x00000100;
    private const int WS_EX_CLIENTEDGE = 0x00000200;
    private const int WS_EX_STATICEDGE = 0x00020000;

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWCP_ROUND = 2;
    private const int DwmwaColorNone = unchecked((int)0xFFFFFFFE);
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;

    private const int ResizeBorderSize = 10;
    private const int TitleBarHeight = 40;
    private const int WindowButtonWidth = 46;

    private Button[] _navButtons = null!;
    private Button? _activeNavButton;
    private readonly Panel _navIndicator = new();
    private readonly Panel _pnlWindowButtons = new();
    private readonly Font _navFont = new("Segoe UI", 9.5f);
    private readonly Font _navFontActive = new("Segoe UI", 9.5f, FontStyle.Bold);
    private readonly Font _titleFont = new("Segoe UI", 9.5f, FontStyle.Bold);
    private readonly Font _windowGlyphFont = CreateWindowGlyphFont();
    private DashboardView? _dashboardView;
    private ImportView? _importView;
    private UtilitiesView? _utilitiesView;

    public FLDSMDFR()
    {
        InitializeComponent();

        ConfigureForm();
        ConfigureTopBar();
        ConfigureNavigation();

        SetActiveNavButton(btnDashboard);
        ShowView(GetDashboardView());
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.Style |= WS_THICKFRAME;
            cp.Style |= WS_MINIMIZEBOX;
            cp.Style |= WS_MAXIMIZEBOX;
            cp.ExStyle &= ~(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyDwmChrome();
        UpdateMaximizedBounds();
    }

    private void ConfigureForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ControlBox = false;
        BackColor = DarkMode.Surface;
        ForeColor = DarkMode.TextPrimary;

        pnlMain.BackColor = DarkMode.Background;
        pnlSideBar.BackColor = DarkMode.Surface;
        pnlTopBar.BackColor = DarkMode.Surface;

        MinimumSize = new Size(900, 600);

        SetStyle(ControlStyles.ResizeRedraw, true);
        SyncResizePadding();

        pnlTopBar.Paint += TopBar_Paint;
        pnlSideBar.Paint += SideBar_Paint;
        pnlSideBar.Resize += (_, _) => UpdateNavIndicator();
        LocationChanged += (_, _) => UpdateMaximizedBounds();
        SizeChanged += (_, _) => SyncResizePadding();
        Activated += (_, _) => Invalidate(true);
        Deactivate += (_, _) => Invalidate(true);
    }

    private void SyncResizePadding()
    {
        Padding = WindowState == FormWindowState.Maximized
            ? Padding.Empty
            : new Padding(ResizeBorderSize);
    }

    private int HitTestResize(IntPtr lParam)
    {
        long packed = lParam.ToInt64();
        var screen = new Point(
            (short)(packed & 0xFFFF),
            (short)((packed >> 16) & 0xFFFF));
        Point mousePosition = PointToClient(screen);
        int buttonBand = WindowButtonWidth * 3;

        bool left =
            mousePosition.X >= 0 &&
            mousePosition.X <= ResizeBorderSize;

        bool right =
            mousePosition.X <= ClientSize.Width &&
            mousePosition.X >= ClientSize.Width - ResizeBorderSize;

        bool top =
            mousePosition.Y >= 0 &&
            mousePosition.Y <= ResizeBorderSize &&
            mousePosition.X < ClientSize.Width - buttonBand;

        bool bottom =
            mousePosition.Y <= ClientSize.Height &&
            mousePosition.Y >= ClientSize.Height - ResizeBorderSize;

        if (left && top)
        {
            return HTTOPLEFT;
        }

        if (right && top)
        {
            return HTTOPRIGHT;
        }

        if (left && bottom)
        {
            return HTBOTTOMLEFT;
        }

        if (right && bottom)
        {
            return HTBOTTOMRIGHT;
        }

        if (left)
        {
            return HTLEFT;
        }

        if (right)
        {
            return HTRIGHT;
        }

        if (top)
        {
            return HTTOP;
        }

        if (bottom)
        {
            return HTBOTTOM;
        }

        return HTCLIENT;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCCALCSIZE)
        {
            if (m.WParam != IntPtr.Zero && IsZoomed(Handle))
            {
                var ncc = Marshal.PtrToStructure<NcCalcSizeParams>(m.LParam);
                Rectangle working = Screen.FromHandle(Handle).WorkingArea;
                ncc.R0 = new Rect
                {
                    Left = working.Left,
                    Top = working.Top,
                    Right = working.Right,
                    Bottom = working.Bottom
                };
                Marshal.StructureToPtr(ncc, m.LParam, false);
            }

            m.Result = IntPtr.Zero;
            return;
        }

        if (m.Msg == WM_NCACTIVATE)
        {
            m.LParam = new IntPtr(-1);
            base.WndProc(ref m);
            m.Result = (IntPtr)1;
            return;
        }

        if (m.Msg == WM_NCHITTEST &&
            WindowState != FormWindowState.Maximized)
        {
            int hit = HitTestResize(m.LParam);
            if (hit != HTCLIENT)
            {
                m.Result = (IntPtr)hit;
                return;
            }
        }

        if (m.Msg == WM_DWMCOMPOSITIONCHANGED)
        {
            ApplyDwmChrome();
        }

        if (DwmDefWindowProc(m.HWnd, m.Msg, m.WParam, m.LParam, out IntPtr dwmResult))
        {
            m.Result = dwmResult;
            return;
        }

        base.WndProc(ref m);
    }

    private void ApplyDwmChrome()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        int dark = 1;
        DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

        int corners = DWMWCP_ROUND;
        DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref corners, sizeof(int));

        int caption = DwmwaColorNone;
        DwmSetWindowAttribute(Handle, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));

        int border = DwmwaColorNone;
        DwmSetWindowAttribute(Handle, DWMWA_BORDER_COLOR, ref border, sizeof(int));

        SetWindowPos(
            Handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged);
    }

    private void UpdateMaximizedBounds()
    {
        MaximizedBounds = Screen.FromControl(this).WorkingArea;
    }

    private static Font CreateWindowGlyphFont()
    {
        try
        {
            return new Font("Segoe MDL2 Assets", 9f);
        }
        catch (ArgumentException)
        {
            return new Font("Segoe UI", 10f);
        }
    }

    private void ConfigureTopBar()
    {
        pnlTopBar.Height = TitleBarHeight;
        pnlTopBar.Dock = DockStyle.Top;
        pnlTopBar.BackColor = DarkMode.Surface;
        pnlTopBar.Padding = Padding.Empty;

        ConfigureWindowButton(btnMinimize, "\uE921", "—");
        ConfigureWindowButton(btnMaximize, "\uE922", "□");
        ConfigureWindowButton(btnClose, "\uE8BB", "×");

        btnClose.FlatAppearance.MouseOverBackColor = DarkMode.Error;
        btnClose.FlatAppearance.MouseDownBackColor = DarkMode.Error;
        btnClose.MouseEnter += (_, _) => btnClose.ForeColor = Color.White;
        btnClose.MouseLeave += (_, _) => btnClose.ForeColor = DarkMode.TextSecondary;

        _pnlWindowButtons.Dock = DockStyle.Right;
        _pnlWindowButtons.Width = WindowButtonWidth * 3;
        _pnlWindowButtons.BackColor = DarkMode.Surface;
        _pnlWindowButtons.Padding = Padding.Empty;

        btnMinimize.Parent = _pnlWindowButtons;
        btnMaximize.Parent = _pnlWindowButtons;
        btnClose.Parent = _pnlWindowButtons;
        btnMinimize.Dock = DockStyle.Right;
        btnMaximize.Dock = DockStyle.Right;
        btnClose.Dock = DockStyle.Right;
        _pnlWindowButtons.Controls.Add(btnMinimize);
        _pnlWindowButtons.Controls.Add(btnMaximize);
        _pnlWindowButtons.Controls.Add(btnClose);

        lblTitle.Parent = pnlTopBar;
        lblTitle.Text = "FLDSMDFR";
        lblTitle.AutoSize = false;
        lblTitle.Dock = DockStyle.Fill;
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;
        lblTitle.Padding = new Padding(14, 0, 0, 0);
        lblTitle.ForeColor = DarkMode.TextPrimary;
        lblTitle.BackColor = Color.Transparent;
        lblTitle.Font = _titleFont;

        pnlTopBar.Controls.Add(lblTitle);
        pnlTopBar.Controls.Add(_pnlWindowButtons);
        _pnlWindowButtons.BringToFront();

        btnMinimize.Click += btnMinimize_Click;
        btnMaximize.Click += btnMaximize_Click;
        btnClose.Click += btnClose_Click;

        pnlTopBar.MouseDown += TopBar_MouseDown;
        lblTitle.MouseDown += TopBar_MouseDown;
        pnlTopBar.DoubleClick += TopBar_DoubleClick;
        lblTitle.DoubleClick += TopBar_DoubleClick;
    }

    private void ConfigureWindowButton(Button button, string glyph, string fallback)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = DarkMode.HoverSurface;
        button.FlatAppearance.MouseDownBackColor = DarkMode.ElevatedSurface;
        button.BackColor = DarkMode.Surface;
        button.ForeColor = DarkMode.TextSecondary;
        button.Font = _windowGlyphFont;
        button.Text = _windowGlyphFont.Name.Contains("MDL2", StringComparison.OrdinalIgnoreCase)
            ? glyph
            : fallback;
        button.Width = WindowButtonWidth;
        button.Height = TitleBarHeight;
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
            btnMaximize.Text = _windowGlyphFont.Name.Contains("MDL2", StringComparison.OrdinalIgnoreCase)
                ? "\uE922"
                : "□";
        }
        else
        {
            WindowState = FormWindowState.Maximized;
            btnMaximize.Text = _windowGlyphFont.Name.Contains("MDL2", StringComparison.OrdinalIgnoreCase)
                ? "\uE923"
                : "❐";
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

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NcCalcSizeParams
    {
        public Rect R0;
        public Rect R1;
        public Rect R2;
        public IntPtr LpPos;
    }
}