// ============================================================
// MonitorLayoutForm.cs — 可视化多显示器布局器
// 玩家可通过拖拽方块预设每个窗口出现在哪个显示器的具体区域
// ============================================================

using System.Drawing;
using D2RMultiPlay.Core.Config;
using D2RMultiPlay.Core.Monitors;

namespace D2RMultiPlay.App;

public sealed class MonitorLayoutForm : Form
{
    public AppConfig Result { get; private set; }

    private readonly List<MonitorInfo> _monitors;
    private readonly List<WindowTile> _tiles = [];
    private Panel _canvas = null!;
    private Button _btnAutoGrid = null!;
    private Button _btnRefresh = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;

    // 画布缩放参数
    private double _scale = 1.0;
    private int _offsetX, _offsetY;

    private static Resources.Strings S => new();

    public MonitorLayoutForm(AppConfig config)
    {
        Result = config;
        _monitors = MonitorEnumerator.Enumerate();
        BuildUI();
        SyncMonitorsToConfig();
        CreateTiles();
        CalculateCanvasTransform();
    }

    private void BuildUI()
    {
        Text = S.MonitorLayoutTitle;
        Size = new Size(900, 600);
        MinimumSize = new Size(600, 400);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);

        // 顶部按钮栏
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(4)
        };

        _btnRefresh = new Button { Text = S.BtnRefreshMonitors, Width = 130 };
        _btnRefresh.Click += (_, _) => RefreshMonitors();

        _btnAutoGrid = new Button { Text = S.BtnAutoGrid, Width = 130 };
        _btnAutoGrid.Click += (_, _) => AutoGridLayout();

        toolbar.Controls.AddRange([_btnRefresh, _btnAutoGrid]);

        // 画布
        _canvas = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 48)
        };
        _canvas.Paint += Canvas_Paint;
        _canvas.Resize += (_, _) => { CalculateCanvasTransform(); _canvas.Invalidate(); };

        // 底部按钮
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };

        _btnCancel = new Button { Text = S.Cancel, DialogResult = DialogResult.Cancel, Width = 80 };
        _btnOk = new Button { Text = S.OK, Width = 80 };
        _btnOk.Click += BtnOk_Click;
        btnPanel.Controls.AddRange([_btnCancel, _btnOk]);

        Controls.Add(_canvas);
        Controls.Add(toolbar);
        Controls.Add(btnPanel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }

    // ======== 画布变换 ========

    private void CalculateCanvasTransform()
    {
        if (_monitors.Count == 0) return;

        // 计算所有显示器的包围盒
        int minX = _monitors.Min(m => m.Bounds.X);
        int minY = _monitors.Min(m => m.Bounds.Y);
        int maxX = _monitors.Max(m => m.Bounds.Right);
        int maxY = _monitors.Max(m => m.Bounds.Bottom);

        int totalW = maxX - minX;
        int totalH = maxY - minY;

        if (totalW <= 0 || totalH <= 0) return;

        int padding = 40;
        double scaleX = (double)(_canvas.Width - padding * 2) / totalW;
        double scaleY = (double)(_canvas.Height - padding * 2) / totalH;
        _scale = Math.Min(scaleX, scaleY);

        // 居中偏移
        _offsetX = (int)((_canvas.Width - totalW * _scale) / 2) - (int)(minX * _scale);
        _offsetY = (int)((_canvas.Height - totalH * _scale) / 2) - (int)(minY * _scale);

        // 更新窗口方块位置
        foreach (var tile in _tiles)
        {
            tile.UpdateCanvasPosition(_scale, _offsetX, _offsetY);
        }
    }

    /// <summary>物理坐标 → 画布坐标</summary>
    private Point PhysicalToCanvas(int px, int py) =>
        new((int)(px * _scale) + _offsetX, (int)(py * _scale) + _offsetY);

    /// <summary>画布坐标 → 物理坐标</summary>
    private Point CanvasToPhysical(int cx, int cy) =>
        new((int)((cx - _offsetX) / _scale), (int)((cy - _offsetY) / _scale));

    // ======== 绘制 ========

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // 绘制显示器矩形
        foreach (var mon in _monitors)
        {
            var topLeft = PhysicalToCanvas(mon.Bounds.X, mon.Bounds.Y);
            int w = (int)(mon.Bounds.Width * _scale);
            int h = (int)(mon.Bounds.Height * _scale);

            // 工作区（半透明蓝）
            using var fillBrush = new SolidBrush(Color.FromArgb(40, 80, 160, 255));
            g.FillRectangle(fillBrush, topLeft.X, topLeft.Y, w, h);

            // 边框
            using var pen = new Pen(mon.IsPrimary ? Color.Gold : Color.SteelBlue, 2);
            g.DrawRectangle(pen, topLeft.X, topLeft.Y, w, h);

            // 标签
            var label = $"{mon.DeviceName}\n{mon.Bounds.Width}x{mon.Bounds.Height} @{mon.DpiScale * 100:F0}%";
            if (mon.IsPrimary) label += " ★";
            using var font = new Font("Segoe UI", 8F);
            using var textBrush = new SolidBrush(Color.White);
            g.DrawString(label, font, textBrush, topLeft.X + 4, topLeft.Y + 4);
        }
    }

    // ======== 窗口方块 ========

    private void CreateTiles()
    {
        foreach (var tile in _tiles)
            _canvas.Controls.Remove(tile);
        _tiles.Clear();

        foreach (var acct in Result.Accounts.Where(a => a.Enabled))
        {
            var tile = new WindowTile(acct, _scale, _offsetX, _offsetY);
            tile.TileMoved += OnTileMoved;
            _tiles.Add(tile);
            _canvas.Controls.Add(tile);
        }
    }

    private void OnTileMoved(WindowTile tile)
    {
        // 将画布位置反算为物理坐标
        var phys = CanvasToPhysical(tile.Left, tile.Top);
        tile.Account.Layout.X = phys.X;
        tile.Account.Layout.Y = phys.Y;

        // 判断落在哪个显示器
        var center = CanvasToPhysical(tile.Left + tile.Width / 2, tile.Top + tile.Height / 2);
        var targetMon = _monitors.FirstOrDefault(m => m.Bounds.Contains(center));
        if (targetMon != null)
            tile.Account.Layout.MonitorId = targetMon.DeviceName;
    }

    // ======== 自动网格排列 ========

    private void AutoGridLayout()
    {
        if (_monitors.Count == 0) return;

        // 按角色分组：master → 主显示器, slave → 副显示器(或同一个)
        var primary = _monitors.FirstOrDefault(m => m.IsPrimary) ?? _monitors[0];
        var secondary = _monitors.Count > 1 ? _monitors.First(m => !m.IsPrimary) : primary;

        var masters = Result.Accounts.Where(a => a.Enabled && a.Role == "master").ToList();
        var slaves = Result.Accounts.Where(a => a.Enabled && a.Role == "slave").ToList();

        ArrangeInGrid(masters, primary);
        ArrangeInGrid(slaves, secondary);

        // 刷新方块位置
        foreach (var tile in _tiles)
            tile.UpdateCanvasPosition(_scale, _offsetX, _offsetY);
        _canvas.Invalidate();
    }

    private static void ArrangeInGrid(List<AccountConfig> accounts, MonitorInfo monitor)
    {
        if (accounts.Count == 0) return;

        int cols = accounts.Count <= 2 ? accounts.Count
                 : accounts.Count <= 4 ? 2
                 : accounts.Count <= 6 ? 3 : 4;
        int rows = (int)Math.Ceiling((double)accounts.Count / cols);

        int cellW = monitor.WorkArea.Width / cols;
        int cellH = monitor.WorkArea.Height / rows;

        for (int i = 0; i < accounts.Count; i++)
        {
            int col = i % cols;
            int row = i / cols;
            accounts[i].Layout.MonitorId = monitor.DeviceName;
            accounts[i].Layout.X = monitor.WorkArea.X + col * cellW;
            accounts[i].Layout.Y = monitor.WorkArea.Y + row * cellH;
            accounts[i].Layout.W = cellW;
            accounts[i].Layout.H = cellH;
        }
    }

    // ======== 显示器刷新 ========

    private void RefreshMonitors()
    {
        _monitors.Clear();
        _monitors.AddRange(MonitorEnumerator.Enumerate());
        SyncMonitorsToConfig();
        CalculateCanvasTransform();
        _canvas.Invalidate();
    }

    private void SyncMonitorsToConfig()
    {
        Result.Monitors = _monitors.Select(m => new MonitorConfig
        {
            Id = m.DeviceName,
            Bounds = [m.Bounds.X, m.Bounds.Y, m.Bounds.Width, m.Bounds.Height],
            WorkArea = [m.WorkArea.X, m.WorkArea.Y, m.WorkArea.Width, m.WorkArea.Height],
            DpiScale = m.DpiScale,
            IsPrimary = m.IsPrimary,
            RefreshHz = m.RefreshHz
        }).ToList();
    }

    // ======== 确认 ========

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
        Close();
    }
}

// ============================================================
// WindowTile — 可拖拽的窗口方块控件
// ============================================================

internal sealed class WindowTile : Control
{
    public AccountConfig Account { get; }
    public event Action<WindowTile>? TileMoved;

    private bool _dragging;
    private Point _dragStart;

    private static readonly Color MasterColor = Color.FromArgb(180, 76, 175, 80);
    private static readonly Color SlaveColor = Color.FromArgb(180, 255, 152, 0);

    public WindowTile(AccountConfig account, double scale, int offsetX, int offsetY)
    {
        Account = account;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Cursor = Cursors.SizeAll;
        UpdateCanvasPosition(scale, offsetX, offsetY);
    }

    public void UpdateCanvasPosition(double scale, int offsetX, int offsetY)
    {
        int x = (int)(Account.Layout.X * scale) + offsetX;
        int y = (int)(Account.Layout.Y * scale) + offsetY;
        int w = Math.Max(20, (int)(Account.Layout.W * scale));
        int h = Math.Max(15, (int)(Account.Layout.H * scale));
        SetBounds(x, y, w, h);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var color = Account.Role == "slave" ? SlaveColor : MasterColor;
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, 0, 0, Width - 1, Height - 1);

        using var pen = new Pen(Color.White, 1);
        g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

        var label = $"#{Account.Id}\n{Account.Name}";
        using var font = new Font("Segoe UI", 7F);
        using var textBrush = new SolidBrush(Color.White);
        g.DrawString(label, font, textBrush, 2, 2);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            _dragStart = e.Location;
            BringToFront();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
        {
            int dx = e.X - _dragStart.X;
            int dy = e.Y - _dragStart.Y;

            // Shift 吸附到网格
            int newX = Left + dx;
            int newY = Top + dy;

            if (ModifierKeys.HasFlag(Keys.Shift))
            {
                int gridSize = Math.Max(20, Width);
                newX = (newX / gridSize) * gridSize;
                newY = (newY / gridSize) * gridSize;
            }

            Location = new Point(newX, newY);
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_dragging)
        {
            _dragging = false;
            TileMoved?.Invoke(this);
        }
        base.OnMouseUp(e);
    }
}

// ============================================================
// 双缓冲 Panel 子类（避免反射，AOT 兼容）
// ============================================================

internal sealed class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }
}
