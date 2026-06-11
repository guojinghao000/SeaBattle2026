using System.Drawing.Drawing2D;
using Client.Game;
using Client.Net;

namespace Client;

public partial class Main : Form
{
    private NetworkService? _net;
    private GameState? _state;
    private readonly System.Windows.Forms.Timer _gameTimer = new() { Interval = 500 };
    private readonly System.Windows.Forms.Timer _moveTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _fireTimer = new() { Interval = 2000 };
    private int _moveDx, _moveDy;
    private bool _canMove = true;
    private bool _canFire = true;
    private bool _showEnemyCooldown = true;
    private bool _loggedIn;
    private bool _disconnecting;
    private bool _scoreDirty;
    private readonly HashSet<Keys> _heldKeys = new();

    // Cached GDI objects to avoid per-frame allocation (prevents AccessViolation)
    private readonly Pen _gridPen;
    private readonly Pen _rangePen;
    private readonly SolidBrush _glowBrush;
    private readonly SolidBrush _selfBrush;
    private readonly SolidBrush _enemyBrush;
    private readonly Pen _selfBorderPen;
    private readonly Pen _enemyBorderPen;
    private readonly Pen _inRangeBorderPen;
    private readonly SolidBrush _hpGreenBrush;
    private readonly SolidBrush _hpOrangeBrush;
    private readonly SolidBrush _hpRedBrush;
    private readonly Font _nameFont;
    private readonly Font _nameFontBold;

    // Cooldown display
    private readonly Pen _cdReadyPen;
    private readonly Pen _cdChargingPen;
    private readonly SolidBrush _cdReadyBrush;
    private readonly SolidBrush _cdChargingBrush;

    // Minimap
    private readonly Pen _minimapBorderPen;
    private readonly Pen _minimapRangePen;
    private readonly SolidBrush _minimapSelfBrush;
    private readonly SolidBrush _minimapEnemyBrush;
    private readonly SolidBrush _minimapInRangeBrush;
    private readonly SolidBrush _minimapBgBrush;

    // Viewport control
    private float _viewportCenterX = 50;
    private float _viewportCenterY = 50;
    private float _zoomLevel = 1.0f;
    private bool _isDragging;
    private Point _dragStartMouse;
    private float _dragStartViewportX;
    private float _dragStartViewportY;
    private bool _followPlayer = true;
    private float _viewportMinX, _viewportMinY;
    private float _visibleGridsW, _visibleGridsH;
    private float _cellSize;

    private static readonly Color BgColor = Color.FromArgb(16, 40, 72);
    private static readonly Color GridColor = Color.FromArgb(30, 60, 100);
    private static readonly Color SelfColor = Color.LimeGreen;
    private static readonly Color EnemyColor = Color.OrangeRed;
    private static readonly Color InRangeBorder = Color.Orange;

    public Main()
    {
        InitializeComponent();

        // Initialize cached GDI objects (disposed in Disposed event)
        _gridPen = new Pen(GridColor, 1);
        _rangePen = new Pen(Color.FromArgb(60, 255, 255, 255), 1) { DashStyle = DashStyle.Dash };
        _glowBrush = new SolidBrush(Color.FromArgb(60, InRangeBorder));
        _selfBrush = new SolidBrush(SelfColor);
        _enemyBrush = new SolidBrush(EnemyColor);
        _selfBorderPen = new Pen(Color.White, 2);
        _enemyBorderPen = new Pen(Color.FromArgb(180, 180, 180), 2);
        _inRangeBorderPen = new Pen(InRangeBorder, 3);
        _hpGreenBrush = new SolidBrush(Color.Green);
        _hpOrangeBrush = new SolidBrush(Color.Orange);
        _hpRedBrush = new SolidBrush(Color.Red);
        _nameFont = new Font("Microsoft YaHei", 8, FontStyle.Regular);
        _nameFontBold = new Font("Microsoft YaHei", 8, FontStyle.Bold);
        _cdReadyPen = new Pen(Color.LimeGreen, 2);
        _cdChargingPen = new Pen(Color.Red, 2);
        _cdReadyBrush = new SolidBrush(Color.FromArgb(60, 0, 255, 0));
        _cdChargingBrush = new SolidBrush(Color.FromArgb(60, 255, 0, 0));
        _minimapBorderPen = new Pen(Color.FromArgb(80, 255, 255, 255), 1);
        _minimapRangePen = new Pen(Color.FromArgb(60, 255, 255, 255), 1);
        _minimapSelfBrush = new SolidBrush(Color.LimeGreen);
        _minimapEnemyBrush = new SolidBrush(Color.OrangeRed);
        _minimapInRangeBrush = new SolidBrush(Color.Orange);
        _minimapBgBrush = new SolidBrush(Color.FromArgb(180, 10, 30, 60));

        this.Disposed += (s, e) =>
        {
            _gridPen.Dispose();
            _rangePen.Dispose();
            _glowBrush.Dispose();
            _selfBrush.Dispose();
            _enemyBrush.Dispose();
            _selfBorderPen.Dispose();
            _enemyBorderPen.Dispose();
            _inRangeBorderPen.Dispose();
            _hpGreenBrush.Dispose();
            _hpOrangeBrush.Dispose();
            _hpRedBrush.Dispose();
            _nameFont.Dispose();
            _nameFontBold.Dispose();
            _cdReadyPen.Dispose();
            _cdChargingPen.Dispose();
            _cdReadyBrush.Dispose();
            _cdChargingBrush.Dispose();
            _minimapBorderPen.Dispose();
            _minimapRangePen.Dispose();
            _minimapSelfBrush.Dispose();
            _minimapEnemyBrush.Dispose();
            _minimapInRangeBrush.Dispose();
            _minimapBgBrush.Dispose();
        };

        _gameTimer.Tick += GameTick;
        _moveTimer.Tick += MoveTick;
        _fireTimer.Tick += FireTick;
        pbBattlefield.MouseClick += PbBattlefield_MouseClick;
        pbBattlefield.MouseDown += PbBattlefield_MouseDown;
        pbBattlefield.MouseMove += PbBattlefield_MouseMove;
        pbBattlefield.MouseUp += PbBattlefield_MouseUp;
        pbBattlefield.MouseWheel += PbBattlefield_MouseWheel;
        this.Resize += (s, e) => PositionMinimap();
        PositionMinimap();
    }

    /// <summary>
    /// Positions the minimap at the top-right corner of the battlefield.
    /// </summary>
    private void PositionMinimap()
    {
        if (pbMinimap == null || pbBattlefield == null) return;
        int margin = 12;
        pbMinimap.Location = new Point(
            pbBattlefield.Right - pbMinimap.Width - margin,
            pbBattlefield.Top + margin);
        pbMinimap.BringToFront();
    }

    private async void BtnConnect_Click(object sender, EventArgs e)
    {
        string ip = txtIP.Text.Trim();
        string shipName = txtShipName.Text.Trim();
        string captain = txtCaptain.Text.Trim();
        string crew = txtCrew.Text.Trim();

        if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(shipName) || string.IsNullOrEmpty(captain))
        {
            MessageBox.Show("请填写服务器 IP、舰队名称和船长名称");
            return;
        }

        if (_net != null)
            await Disconnect();

        btnConnect.Enabled = false;
        lblStatus.Text = "正在连接...";

        try
        {
            _state = new GameState
            {
                LocalShipName = shipName,
                LocalCaptainName = captain
            };
            _state.StateChanged += OnStateChanged;

            _net = new NetworkService(ip);
            await _net.ConnectAsync();

            _canMove = true;
            _canFire = true;
            _moveDx = 0;
            _moveDy = 0;
            _moveTimer.Start();
            _gameTimer.Start();

            string loginMsg = $"Login,{shipName},{captain},{crew}";
            await _net.SendCommandAsync(loginMsg);
            _loggedIn = true;

            gbLogin.Enabled = false;
            btnDisconnect.Enabled = true;
            lblStatus.Text = "已连接";
            _net.ConnectionLost += OnConnectionLost;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"连接失败: {ex.Message}");
            lblStatus.Text = "连接失败";
            btnConnect.Enabled = true;
            _disconnecting = false;
            _net?.Dispose();
            _net = null;
        }
    }

    private async void BtnDisconnect_Click(object sender, EventArgs e)
    {
        await Disconnect();
    }

    private void BtnToggleMode_Click(object? sender, EventArgs e)
    {
        _showEnemyCooldown = !_showEnemyCooldown;
        btnToggleMode.Text = _showEnemyCooldown ? "完整模式" : "兼容模式";
    }

    private void OnConnectionLost()
    {
        if (InvokeRequired)
        {
            // Only post once to avoid multiple message boxes
            if (!_disconnecting)
                BeginInvoke(OnConnectionLost);
            return;
        }

        if (_disconnecting) return;
        _disconnecting = true;

        if (_net != null)
            _net.ConnectionLost -= OnConnectionLost;

        MessageBox.Show("与服务器的连接已断开", "连接断开",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        _ = Disconnect();
    }

    private void Main_FormClosing(object sender, FormClosingEventArgs e)
    {
        _disconnecting = true;
        _gameTimer.Stop();
        _moveTimer.Stop();
        _fireTimer.Stop();

        if (_net != null && _loggedIn)
        {
            try
            {
                // Fire-and-forget: don't block UI thread during close
                _ = _net.SendCommandAsync("Logout");
            }
            catch { }
        }
        _net?.Dispose();
        _net = null;
    }

    private async Task Disconnect()
    {
        _disconnecting = true;

        if (_net != null && _loggedIn)
            await _net.SendCommandAsync("Logout");

        _gameTimer.Stop();
        _moveTimer.Stop();
        _fireTimer.Stop();

        if (_net != null)
            _net.ConnectionLost -= OnConnectionLost;
        _net?.Dispose();
        _net = null;
        _state = null;
        _loggedIn = false;
        _canMove = true;
        _canFire = true;
        _moveDx = 0;
        _moveDy = 0;
        _heldKeys.Clear();

        gbLogin.Enabled = true;
        btnConnect.Enabled = true;
        btnDisconnect.Enabled = false;
        lblStatus.Text = "未连接";
        lblShipStatus.Text = "";
        lstScore.Items.Clear();

        _disconnecting = false;
    }

    private void OnStateChanged()
    {
        _scoreDirty = true;
    }

    private void RefreshScoreList()
    {
        if (_state == null || lstScore.IsDisposed) return;
        var sorted = _state.AllShips
            .OrderByDescending(s => s.Score)
            .ToList();

        lstScore.Items.Clear();
        for (int i = 0; i < sorted.Count; i++)
        {
            var s = sorted[i];
            string prefix = s == _state.LocalShip ? "► " : "   ";
            string cdStatus;
            if (s == _state.LocalShip || _showEnemyCooldown)
            {
                cdStatus = s.FireCooldownMs > 0
                    ? $"CD:{s.FireCooldownMs / 1000.0:F1}s"
                    : "就绪  ";
            }
            else
            {
                cdStatus = "--    ";
            }
            lstScore.Items.Add($"{prefix}#{i + 1,-3} {s.ShipName,-12} {s.Score,3}沉  HP:{s.HP}  {cdStatus}");
        }
    }

    private void GameTick(object? sender, EventArgs e)
    {
        if (_disconnecting) return;

        ProcessMessages();

        // Auto-battle AI
        if (cbAutoBattle.Checked && _state?.LocalShip != null)
        {
            AutoMove();
            AutoFire();
        }

        if (_scoreDirty)
        {
            _scoreDirty = false;
            RefreshScoreList();
        }

        try { RenderBattlefield(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Main] RenderBattlefield error: {ex.Message}"); }

        try { RenderMinimap(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Main] RenderMinimap error: {ex.Message}"); }

        UpdateShipStatus();
    }

    private void ProcessMessages()
    {
        if (_net == null || _state == null) return;
        while (_net.ReceivedMessages.TryDequeue(out string? msg) && msg != null)
        {
            _state.ProcessServerMessage(msg);
        }
    }

    private void UpdateShipStatus()
    {
        if (_state?.LocalShip == null)
        {
            lblShipStatus.Text = "";
            return;
        }
        var s = _state.LocalShip;
        string cdText = _canFire ? "可开火" : $"冷却中({s.FireCooldownMs / 1000.0:F1}s)";
        lblShipStatus.Text = $"位置:({s.Px},{s.Py})  HP:{s.HP}/3  击沉:{s.Score}  {cdText}";
    }

    private async void MoveTick(object? sender, EventArgs e)
    {
        if (_disconnecting || _net == null || !_canMove || (_moveDx == 0 && _moveDy == 0)) return;

        _canMove = false;
        await _net.SendCommandAsync($"Move,{_moveDx},{_moveDy}");

        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            _canMove = true;
        });
    }

    private void FireTick(object? sender, EventArgs e)
    {
        _canFire = true;
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData is Keys.Up or Keys.Down or Keys.Left or Keys.Right)
        {
            _heldKeys.Add(keyData);
            UpdateMoveDirection();
            return true;
        }
        return base.ProcessDialogKey(keyData);
    }

    private void Main_KeyDown(object sender, KeyEventArgs e)
    {
        _heldKeys.Add(e.KeyCode);
        UpdateMoveDirection();

        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.J)
        {
            ManualFire();
        }

        if (e.KeyCode == Keys.Home)
        {
            _followPlayer = true;
            _zoomLevel = 1.0f;
        }

        if (e.KeyCode == Keys.F1)
        {
            cbAutoBattle.Checked = !cbAutoBattle.Checked;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        if (e.KeyCode is Keys.W or Keys.A or Keys.S or Keys.D or
            Keys.Space or Keys.J or Keys.Home or Keys.F1)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void Main_KeyUp(object sender, KeyEventArgs e)
    {
        _heldKeys.Remove(e.KeyCode);
        UpdateMoveDirection();
    }

    private void UpdateMoveDirection()
    {
        _moveDx = 0;
        _moveDy = 0;
        if (_heldKeys.Contains(Keys.W) || _heldKeys.Contains(Keys.Up)) _moveDy = -1;
        else if (_heldKeys.Contains(Keys.S) || _heldKeys.Contains(Keys.Down)) _moveDy = 1;
        if (_heldKeys.Contains(Keys.A) || _heldKeys.Contains(Keys.Left)) _moveDx = -1;
        else if (_heldKeys.Contains(Keys.D) || _heldKeys.Contains(Keys.Right)) _moveDx = 1;
    }

    private Fleet? GetNearestTargetInRange()
    {
        if (_state?.LocalShip == null) return null;

        Fleet? nearest = null;
        int minDistSq = int.MaxValue;
        int rangeSq = 10 * 10;

        foreach (var ship in _state.AllShips)
        {
            if (ship == _state.LocalShip) continue;

            int dx = ship.Px - _state.LocalShip.Px;
            int dy = ship.Py - _state.LocalShip.Py;
            int distSq = dx * dx + dy * dy;

            if (distSq <= rangeSq && distSq < minDistSq)
            {
                minDistSq = distSq;
                nearest = ship;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Find best target in range for auto-battle: prioritize low HP (1-hit kill),
    /// then nearest distance, then any in-range.
    /// </summary>
    private Fleet? GetBestTargetInRange()
    {
        if (_state?.LocalShip == null) return null;

        int rangeSq = 5 * 5;
        Fleet? bestHp1 = null, bestHp2 = null, bestHp3 = null;
        int bestDistHp1 = int.MaxValue, bestDistHp2 = int.MaxValue, bestDistHp3 = int.MaxValue;

        foreach (var ship in _state.AllShips)
        {
            if (ship == _state.LocalShip) continue;

            int dx = ship.Px - _state.LocalShip.Px;
            int dy = ship.Py - _state.LocalShip.Py;
            int distSq = dx * dx + dy * dy;

            if (distSq > rangeSq) continue;

            switch (ship.HP)
            {
                case 1:
                    if (distSq < bestDistHp1) { bestDistHp1 = distSq; bestHp1 = ship; }
                    break;
                case 2:
                    if (distSq < bestDistHp2) { bestDistHp2 = distSq; bestHp2 = ship; }
                    break;
                default: // 3 or unknown
                    if (distSq < bestDistHp3) { bestDistHp3 = distSq; bestHp3 = ship; }
                    break;
            }
        }

        return bestHp1 ?? bestHp2 ?? bestHp3;
    }

    /// <summary>
    /// Auto-move toward the nearest enemy (any distance, not just in-range).
    /// Sets _moveDx, _moveDy which MoveTick picks up every 1s.
    /// </summary>
    private void AutoMove()
    {
        if (_state?.LocalShip == null) return;

        // Find nearest enemy (any distance)
        Fleet? nearest = null;
        int minDistSq = int.MaxValue;
        foreach (var ship in _state.AllShips)
        {
            if (ship == _state.LocalShip) continue;
            int dx = ship.Px - _state.LocalShip.Px;
            int dy = ship.Py - _state.LocalShip.Py;
            int distSq = dx * dx + dy * dy;
            if (distSq < minDistSq) { minDistSq = distSq; nearest = ship; }
        }

        if (nearest == null) return;

        int targetDx = nearest.Px - _state.LocalShip.Px;
        int targetDy = nearest.Py - _state.LocalShip.Py;
        int distSqTarget = targetDx * targetDx + targetDy * targetDy;

        // If within range (radius 5), stop moving to hold position and fire
        if (distSqTarget <= 25)
        {
            _moveDx = 0;
            _moveDy = 0;
            return;
        }

        // Move toward target
        _moveDx = Math.Sign(targetDx);
        _moveDy = Math.Sign(targetDy);
    }

    /// <summary>
    /// Auto-fire at the best target in range if cooldown is ready.
    /// </summary>
    private async void AutoFire()
    {
        if (_disconnecting || _net == null || !_canFire) return;

        var target = GetBestTargetInRange();
        if (target == null) return;

        await FireAt(target);
    }

    private async void ManualFire()
    {
        if (_disconnecting || _net == null || !_canFire || _state?.LocalShip == null) return;

        var target = GetNearestTargetInRange();
        if (target == null) return;

        await FireAt(target);
    }

    private async void PbBattlefield_MouseClick(object? sender, MouseEventArgs e)
    {
        if (_disconnecting || _net == null || !_canFire || _state?.LocalShip == null) return;

        // Convert screen pixel to grid coordinates using viewport
        int clickGridX = (int)(e.X / _cellSize + _viewportMinX);
        int clickGridY = (int)(e.Y / _cellSize + _viewportMinY);

        // Find enemy closest to click point within range
        Fleet? bestTarget = null;
        double bestDist = double.MaxValue;
        int rangeSq = 10 * 10;

        foreach (var ship in _state.AllShips)
        {
            if (ship == _state.LocalShip) continue;

            int dx = ship.Px - _state.LocalShip.Px;
            int dy = ship.Py - _state.LocalShip.Py;
            int distSq = dx * dx + dy * dy;

            if (distSq > rangeSq) continue;

            double distToClick = Math.Sqrt(
                (ship.Px - clickGridX) * (ship.Px - clickGridX) +
                (ship.Py - clickGridY) * (ship.Py - clickGridY));

            if (distToClick < bestDist)
            {
                bestDist = distToClick;
                bestTarget = ship;
            }
        }

        // Only fire if click is close to the target (within 3 grid units)
        const double maxClickDist = 3.0;
        if (bestTarget != null && bestDist <= maxClickDist)
            await FireAt(bestTarget);
    }

    // --- Viewport mouse drag & zoom ---

    private void PbBattlefield_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragStartMouse = e.Location;
            _dragStartViewportX = _viewportCenterX;
            _dragStartViewportY = _viewportCenterY;
            _followPlayer = false;
        }
    }

    private void PbBattlefield_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        float deltaGx = (e.X - _dragStartMouse.X) / _cellSize;
        float deltaGy = (e.Y - _dragStartMouse.Y) / _cellSize;
        _viewportCenterX = _dragStartViewportX - deltaGx;
        _viewportCenterY = _dragStartViewportY - deltaGy;

        // Clamp viewport center so viewport stays within map when possible
        float halfW = _visibleGridsW / 2f;
        float halfH = _visibleGridsH / 2f;
        if (_visibleGridsW <= 100)
            _viewportCenterX = Math.Clamp(_viewportCenterX, halfW, 100 - halfW);
        else
            _viewportCenterX = 50;
        if (_visibleGridsH <= 100)
            _viewportCenterY = Math.Clamp(_viewportCenterY, halfH, 100 - halfH);
        else
            _viewportCenterY = 50;
    }

    private void PbBattlefield_MouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    private void PbBattlefield_MouseWheel(object? sender, MouseEventArgs e)
    {
        // Zoom in/out centered on mouse position
        float oldZoom = _zoomLevel;
        float zoomDelta = e.Delta > 0 ? 1.1f : 1f / 1.1f;
        _zoomLevel = Math.Clamp(_zoomLevel * zoomDelta, 0.5f, 3.0f);

        // Adjust viewport center to zoom toward mouse cursor
        if (_zoomLevel != oldZoom)
        {
            float mouseGx = e.X / _cellSize + _viewportMinX;
            float mouseGy = e.Y / _cellSize + _viewportMinY;

            // Recalculate cellSize with new zoom
            int w = pbBattlefield.Width;
            int h = pbBattlefield.Height;
            float newBase = Math.Clamp(Math.Min(w, h) / 50f, 8f, 20f);
            float newCellSize = Math.Clamp(newBase * _zoomLevel, 4f, 30f);
            float newVisW = w / newCellSize;
            float newVisH = h / newCellSize;

            _viewportCenterX = mouseGx;
            _viewportCenterY = mouseGy;

            // Clamp
            if (newVisW <= 100)
                _viewportCenterX = Math.Clamp(_viewportCenterX, newVisW / 2f, 100 - newVisW / 2f);
            else
                _viewportCenterX = 50;
            if (newVisH <= 100)
                _viewportCenterY = Math.Clamp(_viewportCenterY, newVisH / 2f, 100 - newVisH / 2f);
            else
                _viewportCenterY = 50;

            _followPlayer = false;
        }
    }

    private async void PbMinimap_MouseClick(object? sender, MouseEventArgs e)
    {
        // Placeholder: clicking the minimap could later be used to jump
        // the main view to that location if zoom/pan is implemented.
        // For now, treat it the same as clicking the main battlefield.
        if (_disconnecting || _net == null || !_canFire || _state?.LocalShip == null) return;

        int size = pbMinimap.Width;
        float scale = 100f / size;
        int clickGridX = (int)(e.X * scale);
        int clickGridY = (int)(e.Y * scale);

        Fleet? bestTarget = null;
        double bestDist = double.MaxValue;
        int rangeSq = 10 * 10;

        foreach (var ship in _state.AllShips)
        {
            if (ship == _state.LocalShip) continue;

            int dx = ship.Px - _state.LocalShip.Px;
            int dy = ship.Py - _state.LocalShip.Py;
            int distSq = dx * dx + dy * dy;

            if (distSq > rangeSq) continue;

            double distToClick = Math.Sqrt(
                (ship.Px - clickGridX) * (ship.Px - clickGridX) +
                (ship.Py - clickGridY) * (ship.Py - clickGridY));

            if (distToClick < bestDist)
            {
                bestDist = distToClick;
                bestTarget = ship;
            }
        }

        const double maxClickDist = 3.0;
        if (bestTarget != null && bestDist <= maxClickDist)
            await FireAt(bestTarget);
    }

    private async Task FireAt(Fleet target)
    {
        if (_disconnecting || _net == null || _state?.LocalShip == null) return;

        int dx = target.Px - _state.LocalShip.Px;
        int dy = target.Py - _state.LocalShip.Py;

        // Clamp to range [-5,5] within circle radius 5
        if (dx * dx + dy * dy > 100)
        {
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double scale = 10.0 / dist;
            dx = (int)(dx * scale);
            dy = (int)(dy * scale);
        }

        _canFire = false;
        _fireTimer.Stop();
        _fireTimer.Start();
        await _net.SendCommandAsync($"Fire,{dx},{dy}");
    }

    /// <summary>
    /// Renders a small overview minimap showing all ship positions at a glance.
    /// </summary>
    private void RenderMinimap()
    {
        int size = pbMinimap.Width;
        if (size <= 0) return;

        var bitmap = new Bitmap(size, size);
        try
        {
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float scale = size / 100f;
            int dotRadius = 3;

            // Background
            g.FillRectangle(_minimapBgBrush, 0, 0, size, size);
            g.DrawRectangle(_minimapBorderPen, 0, 0, size - 1, size - 1);

            if (_state == null) { pbMinimap.Image = bitmap; bitmap = null; return; }

            // Determine which enemies are in range of local ship
            var inRange = new HashSet<Fleet>();
            if (_state.LocalShip != null)
            {
                int rangeSq = 10 * 10;
                foreach (var ship in _state.AllShips)
                {
                    if (ship == _state.LocalShip) continue;
                    int dx = ship.Px - _state.LocalShip.Px;
                    int dy = ship.Py - _state.LocalShip.Py;
                    if (dx * dx + dy * dy <= rangeSq)
                        inRange.Add(ship);
                }
            }

            // Draw ships
            foreach (var ship in _state.AllShips)
            {
                bool isLocal = ship == _state.LocalShip;
                bool shipInRange = inRange.Contains(ship);
                float sx = ship.Px * scale;
                float sy = ship.Py * scale;

                var brush = isLocal ? _minimapSelfBrush
                    : shipInRange ? _minimapInRangeBrush
                    : _minimapEnemyBrush;

                // Local ship slightly larger
                int r = isLocal ? dotRadius + 1 : dotRadius;
                g.FillEllipse(brush, sx - r, sy - r, r * 2, r * 2);

                // White border for local ship
                if (isLocal)
                    g.DrawEllipse(Pens.White, sx - r, sy - r, r * 2, r * 2);
            }

            // Local ship range circle
            if (_state.LocalShip != null)
            {
                float cx = _state.LocalShip.Px * scale;
                float cy = _state.LocalShip.Py * scale;
                float rangePx = 10 * scale;
                g.DrawEllipse(_minimapRangePen, cx - rangePx, cy - rangePx, rangePx * 2, rangePx * 2);
            }

            // Viewport indicator rectangle
            {
                float vpX = _viewportMinX * scale;
                float vpY = _viewportMinY * scale;
                float vpW = _visibleGridsW * scale;
                float vpH = _visibleGridsH * scale;
                // Clamp to minimap bounds
                vpX = Math.Max(0, vpX);
                vpY = Math.Max(0, vpY);
                vpW = Math.Min(vpW, size - vpX);
                vpH = Math.Min(vpH, size - vpY);
                using var vpPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f);
                g.DrawRectangle(vpPen, vpX, vpY, vpW, vpH);
            }

            var oldImage = pbMinimap.Image;
            pbMinimap.Image = bitmap;
            oldImage?.Dispose();
            bitmap = null;
        }
        finally
        {
            bitmap?.Dispose();
        }
    }

    /// <summary>
    /// Renders the battlefield with viewport support: dynamic cell size based on window
    /// size and zoom level, viewport follows player by default, user can drag/zoom.
    /// </summary>
    private void RenderBattlefield()
    {
        int w = pbBattlefield.Width;
        int h = pbBattlefield.Height;
        if (w <= 0 || h <= 0) return;

        // --- Calculate cell size: aim ~50 cells on the smaller axis ---
        float baseCellSize = Math.Min(w, h) / 50f;
        baseCellSize = Math.Clamp(baseCellSize, 8f, 20f);
        _cellSize = Math.Clamp(baseCellSize * _zoomLevel, 4f, 30f);

        // --- Follow player ---
        if (_followPlayer && _state?.LocalShip != null)
        {
            _viewportCenterX = _state.LocalShip.Px;
            _viewportCenterY = _state.LocalShip.Py;
        }

        // --- Calculate viewport bounds in grid coordinates ---
        _visibleGridsW = w / _cellSize;
        _visibleGridsH = h / _cellSize;
        _viewportMinX = _viewportCenterX - _visibleGridsW / 2f;
        _viewportMinY = _viewportCenterY - _visibleGridsH / 2f;

        // Clamp to map bounds [0, 100]
        if (_visibleGridsW <= 100)
            _viewportMinX = Math.Clamp(_viewportMinX, 0, 100 - _visibleGridsW);
        else
            _viewportMinX = (100 - _visibleGridsW) / 2f;

        if (_visibleGridsH <= 100)
            _viewportMinY = Math.Clamp(_viewportMinY, 0, 100 - _visibleGridsH);
        else
            _viewportMinY = (100 - _visibleGridsH) / 2f;

        // Snap viewport to integer grid so ships and grid lines align exactly
        _viewportMinX = (float)Math.Floor(_viewportMinX);
        _viewportMinY = (float)Math.Floor(_viewportMinY);
        _viewportCenterX = _viewportMinX + _visibleGridsW / 2f;
        _viewportCenterY = _viewportMinY + _visibleGridsH / 2f;

        float viewportMaxX = _viewportMinX + _visibleGridsW;
        float viewportMaxY = _viewportMinY + _visibleGridsH;

        // Grid → screen helper
        float ToScreenX(float gx) => (gx - _viewportMinX) * _cellSize;
        float ToScreenY(float gy) => (gy - _viewportMinY) * _cellSize;

        var bitmap = new Bitmap(w, h);
        try
        {
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.Clear(BgColor);

            // --- Grid step based on cellSize (more granular at small sizes) ---
            int gridStep = _cellSize >= 12 ? 1 : _cellSize >= 7 ? 2 : _cellSize >= 5 ? 4 : 5;

            // First grid line >= viewportMinX (aligned to gridStep)
            int startGx = (int)(Math.Ceiling(_viewportMinX / gridStep) * gridStep);
            int startGy = (int)(Math.Ceiling(_viewportMinY / gridStep) * gridStep);

            // Major grid lines
            for (int i = startGx; i <= 100 && i <= viewportMaxX; i += gridStep)
            {
                float sx = ToScreenX(i);
                g.DrawLine(_gridPen, sx, 0, sx, h);
            }
            for (int i = startGy; i <= 100 && i <= viewportMaxY; i += gridStep)
            {
                float sy = ToScreenY(i);
                g.DrawLine(_gridPen, 0, sy, w, sy);
            }

            // Subtle minor lines when step == 2 and cells are large enough
            if (gridStep == 2 && _cellSize >= 7)
            {
                using var minorPen = new Pen(Color.FromArgb(15, 50, 80), 1);
                int mStartX = (int)Math.Ceiling(_viewportMinX);
                int mStartY = (int)Math.Ceiling(_viewportMinY);
                for (int i = mStartX; i <= 100 && i <= viewportMaxX; i++)
                {
                    if (i % gridStep == 0) continue;
                    float sx = ToScreenX(i);
                    g.DrawLine(minorPen, sx, 0, sx, h);
                }
                for (int i = mStartY; i <= 100 && i <= viewportMaxY; i++)
                {
                    if (i % gridStep == 0) continue;
                    float sy = ToScreenY(i);
                    g.DrawLine(minorPen, 0, sy, w, sy);
                }
            }

            if (_state == null) { pbBattlefield.Image = bitmap; bitmap = null; return; }

            // --- Range circle around local ship ---
            if (_state.LocalShip != null)
            {
                float cx = ToScreenX(_state.LocalShip.Px);
                float cy = ToScreenY(_state.LocalShip.Py);
                float rangePx = 10 * _cellSize;
                g.DrawEllipse(_rangePen, cx - rangePx, cy - rangePx, rangePx * 2, rangePx * 2);
            }

            // --- Fire marks (only visible ones) ---
            foreach (var ship in _state.AllShips)
            {
                if (ship.Fx >= 0 && ship.Fy >= 0)
                {
                    float fx = ToScreenX(ship.Fx);
                    float fy = ToScreenY(ship.Fy);
                    if (fx >= -8 && fx <= w + 8 && fy >= -8 && fy <= h + 8)
                        g.FillEllipse(Brushes.Yellow, fx - 4, fy - 4, 8, 8);
                }
            }

            // --- Determine which enemies are in range ---
            var allInRange = new HashSet<Fleet>();
            if (_state.LocalShip != null)
            {
                int rangeSq = 10 * 10;
                foreach (var ship in _state.AllShips)
                {
                    if (ship == _state.LocalShip) continue;
                    int dx = ship.Px - _state.LocalShip.Px;
                    int dy = ship.Py - _state.LocalShip.Py;
                    if (dx * dx + dy * dy <= rangeSq)
                        allInRange.Add(ship);
                }
            }

            // --- Draw ships ---
            foreach (var ship in _state.AllShips)
            {
                bool isLocal = ship == _state.LocalShip;
                bool inRange = allInRange.Contains(ship);
                float sx = ToScreenX(ship.Px);
                float sy = ToScreenY(ship.Py);

                // Skip ships whose full UI (glow + CD ring + HP bar + label) is outside viewport
                // Top/left: glow max 16px; Bottom: HP bar at r+8 + label ≈ 38px below center
                if (sx < -16 || sx > w + 16 || sy < -16 || sy > h + 45) continue;

                float r = isLocal ? 10 : 8;

                // Glow for in-range enemies
                if (inRange)
                {
                    r = 10;
                    g.FillEllipse(_glowBrush, sx - 14, sy - 14, 28, 28);
                }

                // Ship dot
                var brush = isLocal ? _selfBrush : _enemyBrush;
                g.FillEllipse(brush, sx - r, sy - r, r * 2, r * 2);

                // Border
                var borderPen = inRange ? _inRangeBorderPen : isLocal ? _selfBorderPen : _enemyBorderPen;
                g.DrawEllipse(borderPen, sx - r, sy - r, r * 2, r * 2);

                // Cooldown ring (drawn before HP bar to avoid overlap)
                if (isLocal || _showEnemyCooldown)
                {
                    float cdRingR = r + 6;
                    if (ship.FireCooldownMs > 0)
                    {
                        float cdRatio = Math.Clamp(ship.FireCooldownMs / 2000f, 0f, 1f);
                        float sweepAngle = cdRatio * 360f;
                        var cdRect = new RectangleF(sx - cdRingR, sy - cdRingR, cdRingR * 2, cdRingR * 2);
                        g.DrawArc(_cdChargingPen, cdRect, -90, sweepAngle);
                    }
                    else
                    {
                        var cdRect = new RectangleF(sx - cdRingR, sy - cdRingR, cdRingR * 2, cdRingR * 2);
                        g.DrawEllipse(_cdReadyPen, cdRect);
                    }
                }

                // HP bar (below CD ring: r + 8)
                float barWidth = 24;
                float barHeight = 4;
                float barX = sx - barWidth / 2;
                float barY = sy + r + 8;
                g.FillRectangle(Brushes.Gray, barX, barY, barWidth, barHeight);
                float hpRatio = Math.Clamp(ship.HP / 3f, 0, 1);
                var hpBrush = hpRatio > 0.5f ? _hpGreenBrush : hpRatio > 0.25f ? _hpOrangeBrush : _hpRedBrush;
                g.FillRectangle(hpBrush, barX, barY, barWidth * hpRatio, barHeight);

                // Name label
                string label = isLocal ? $"★ {ship.ShipName}" : ship.ShipName;
                var font = isLocal ? _nameFontBold : _nameFont;
                var textSize = g.MeasureString(label, font);
                float textX = sx - textSize.Width / 2;
                float textY = barY + barHeight + 1;
                g.DrawString(label, font, Brushes.White, textX, textY);
            }

            // Display the rendered bitmap
            var oldImage = pbBattlefield.Image;
            pbBattlefield.Image = bitmap;
            oldImage?.Dispose();
            bitmap = null;
        }
        finally
        {
            bitmap?.Dispose();
        }
    }
}
