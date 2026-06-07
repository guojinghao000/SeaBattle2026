using System.Drawing.Drawing2D;
using Client.Game;
using Client.Net;

namespace Client;

public partial class Main : Form
{
    private NetworkService? _net;
    private GameState? _state;
    private readonly System.Windows.Forms.Timer _gameTimer = new() { Interval = 100 };
    private readonly System.Windows.Forms.Timer _moveTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _fireTimer = new() { Interval = 2000 };
    private int _moveDx, _moveDy;
    private bool _canMove = true;
    private bool _canFire = true;
    private bool _loggedIn;
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
        };

        _gameTimer.Tick += GameTick;
        _moveTimer.Tick += MoveTick;
        _fireTimer.Tick += FireTick;
        pbBattlefield.MouseClick += PbBattlefield_MouseClick;
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
        }
        catch (Exception ex)
        {
            MessageBox.Show($"连接失败: {ex.Message}");
            lblStatus.Text = "连接失败";
            btnConnect.Enabled = true;
            _net?.Dispose();
            _net = null;
        }
    }

    private async void BtnDisconnect_Click(object sender, EventArgs e)
    {
        await Disconnect();
    }

    private void Main_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (_net != null && _loggedIn)
        {
            try
            {
                // 同步发送 Logout 确保服务器收到
                _net.SendCommandAsync("Logout").Wait(TimeSpan.FromSeconds(1));
            }
            catch { }
        }
        _gameTimer.Stop();
        _moveTimer.Stop();
        _fireTimer.Stop();
        _net?.Dispose();
        _net = null;
    }

    private async Task Disconnect()
    {
        if (_net != null && _loggedIn)
            await _net.SendCommandAsync("Logout");

        _gameTimer.Stop();
        _moveTimer.Stop();
        _fireTimer.Stop();

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
    }

    private void OnStateChanged()
    {
        RefreshScoreList();
    }

    private void RefreshScoreList()
    {
        if (_state == null) return;
        var sorted = _state.AllShips
            .OrderByDescending(s => s.Score)
            .ToList();

        lstScore.Items.Clear();
        for (int i = 0; i < sorted.Count; i++)
        {
            var s = sorted[i];
            string prefix = s == _state.LocalShip ? "► " : "   ";
            string cdStatus = s.FireCooldownMs > 0
                ? $"CD:{s.FireCooldownMs / 1000.0:F1}s"
                : "就绪  ";
            lstScore.Items.Add($"{prefix}#{i + 1,-3} {s.ShipName,-12} {s.Score,3}沉  HP:{s.HP}  {cdStatus}");
        }
    }

    private void GameTick(object? sender, EventArgs e)
    {
        ProcessMessages();
        RenderBattlefield();
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
        if (_net == null || !_canMove || (_moveDx == 0 && _moveDy == 0)) return;

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

    private void Main_KeyDown(object sender, KeyEventArgs e)
    {
        _heldKeys.Add(e.KeyCode);
        UpdateMoveDirection();

        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.J)
        {
            ManualFire();
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
        int rangeSq = 5 * 5;

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

    private async void ManualFire()
    {
        if (_net == null || !_canFire || _state?.LocalShip == null) return;

        var target = GetNearestTargetInRange();
        if (target == null) return;

        await FireAt(target);
    }

    private async void PbBattlefield_MouseClick(object? sender, MouseEventArgs e)
    {
        if (_net == null || !_canFire || _state?.LocalShip == null) return;

        int w = pbBattlefield.Width;
        int h = pbBattlefield.Height;
        float cellSize = Math.Min(w, h) / 100f;
        float gridPx = 100 * cellSize;
        float offsetX = (w - gridPx) / 2f;
        float offsetY = (h - gridPx) / 2f;

        // Convert pixel to grid coordinates
        int clickGridX = (int)((e.X - offsetX) / cellSize);
        int clickGridY = (int)((e.Y - offsetY) / cellSize);

        // Find enemy closest to click point within range
        Fleet? bestTarget = null;
        double bestDist = double.MaxValue;
        int rangeSq = 5 * 5;

        foreach (var ship in _state.AllShips)
        {
            if (ship == _state.LocalShip) continue;

            int dx = ship.Px - _state.LocalShip.Px;
            int dy = ship.Py - _state.LocalShip.Py;
            int distSq = dx * dx + dy * dy;

            if (distSq > rangeSq) continue;

            // Distance from click to this ship
            double distToClick = Math.Sqrt(
                (ship.Px - clickGridX) * (ship.Px - clickGridX) +
                (ship.Py - clickGridY) * (ship.Py - clickGridY));

            if (distToClick < bestDist)
            {
                bestDist = distToClick;
                bestTarget = ship;
            }
        }

        if (bestTarget != null)
            await FireAt(bestTarget);
    }

    private async Task FireAt(Fleet target)
    {
        if (_net == null || _state?.LocalShip == null) return;

        int dx = target.Px - _state.LocalShip.Px;
        int dy = target.Py - _state.LocalShip.Py;

        // Clamp to range [-5,5] within circle radius 5
        if (dx * dx + dy * dy > 25)
        {
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double scale = 5.0 / dist;
            dx = (int)(dx * scale);
            dy = (int)(dy * scale);
        }

        _canFire = false;
        _fireTimer.Stop();
        _fireTimer.Start();
        await _net.SendCommandAsync($"Fire,{dx},{dy}");
    }

    /// <summary>
    /// Renders the battlefield to an off-screen bitmap and displays it.
    /// This avoids GDI+ conflicts with PictureBox's internal paint handling.
    /// </summary>
    private void RenderBattlefield()
    {
        int w = pbBattlefield.Width;
        int h = pbBattlefield.Height;
        if (w <= 0 || h <= 0) return;

        var bitmap = new Bitmap(w, h);
        try
        {
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cellSize = Math.Min(w, h) / 100f;
            float gridPx = 100 * cellSize;
            float offsetX = (w - gridPx) / 2f;
            float offsetY = (h - gridPx) / 2f;
            int gridSize = 100;

            g.Clear(BgColor);

            // Draw grid lines
            for (int i = 0; i <= gridSize; i++)
            {
                float p = i * cellSize;
                g.DrawLine(_gridPen, offsetX + p, offsetY, offsetX + p, offsetY + gridSize * cellSize);
                g.DrawLine(_gridPen, offsetX, offsetY + p, offsetX + gridSize * cellSize, offsetY + p);
            }

            if (_state == null) { pbBattlefield.Image = bitmap; bitmap = null; return; }

            // Draw range circle around local ship
            if (_state.LocalShip != null)
            {
                float cx = offsetX + _state.LocalShip.Px * cellSize;
                float cy = offsetY + _state.LocalShip.Py * cellSize;
                float rangePx = 5 * cellSize;
                g.DrawEllipse(_rangePen, cx - rangePx, cy - rangePx, rangePx * 2, rangePx * 2);
            }

            // Draw fire marks
            foreach (var ship in _state.AllShips)
            {
                if (ship.Fx >= 0 && ship.Fy >= 0)
                {
                    float fx = offsetX + ship.Fx * cellSize;
                    float fy = offsetY + ship.Fy * cellSize;
                    g.FillEllipse(Brushes.Yellow, fx - 4, fy - 4, 8, 8);
                }
            }

            // Determine which enemies are in range
            var allInRange = new HashSet<Fleet>();
            if (_state.LocalShip != null)
            {
                int rangeSq = 5 * 5;
                foreach (var ship in _state.AllShips)
                {
                    if (ship == _state.LocalShip) continue;
                    int dx = ship.Px - _state.LocalShip.Px;
                    int dy = ship.Py - _state.LocalShip.Py;
                    if (dx * dx + dy * dy <= rangeSq)
                        allInRange.Add(ship);
                }
            }

            // Draw ships
            foreach (var ship in _state.AllShips)
            {
                bool isLocal = ship == _state.LocalShip;
                bool inRange = allInRange.Contains(ship);
                float sx = offsetX + ship.Px * cellSize;
                float sy = offsetY + ship.Py * cellSize;
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

                // HP bar
                float barWidth = 24;
                float barHeight = 4;
                float barX = sx - barWidth / 2;
                float barY = sy + r + 2;
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

                // Cooldown ring
                float cdRingR = r + 6;
                if (ship.FireCooldownMs > 0)
                {
                    // Draw red arc: full circle = 2000ms, arc shows remaining cooldown
                    float cdRatio = Math.Clamp(ship.FireCooldownMs / 2000f, 0f, 1f);
                    float sweepAngle = cdRatio * 360f;
                    var cdRect = new RectangleF(sx - cdRingR, sy - cdRingR, cdRingR * 2, cdRingR * 2);
                    g.DrawArc(_cdChargingPen, cdRect, -90, sweepAngle);
                }
                else
                {
                    // Ready — subtle green ring
                    var cdRect = new RectangleF(sx - cdRingR, sy - cdRingR, cdRingR * 2, cdRingR * 2);
                    g.DrawEllipse(_cdReadyPen, cdRect);
                }
            }

            // Display the rendered bitmap
            var oldImage = pbBattlefield.Image;
            pbBattlefield.Image = bitmap;
            oldImage?.Dispose();
            bitmap = null; // ownership transferred to PictureBox
        }
        finally
        {
            bitmap?.Dispose();
        }
    }
}
