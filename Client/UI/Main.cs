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

    private static readonly Color BgColor = Color.FromArgb(16, 40, 72);
    private static readonly Color GridColor = Color.FromArgb(30, 60, 100);
    private static readonly Color SelfColor = Color.LimeGreen;
    private static readonly Color EnemyColor = Color.OrangeRed;
    private static readonly Color RangeColor = Color.FromArgb(30, Color.White);
    private static readonly Color InRangeBorder = Color.Orange;
    private float _offsetX, _offsetY;

    public Main()
    {
        InitializeComponent();
        _gameTimer.Tick += GameTick;
        _moveTimer.Tick += MoveTick;
        _fireTimer.Tick += FireTick;
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
            lstScore.Items.Add($"{prefix}#{i + 1,-3} {s.ShipName,-12} {s.Score,3}沉  HP:{s.HP}");
        }
    }

    private void GameTick(object? sender, EventArgs e)
    {
        ProcessMessages();
        TryFireAtNearestTarget();
        pbBattlefield.Invalidate();
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
        lblShipStatus.Text = $"位置:({s.Px},{s.Py})  HP:{s.HP}/3  击沉:{s.Score}";
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

    private async void TryFireAtNearestTarget()
    {
        if (_net == null || !_canFire || _state?.LocalShip == null) return;

        var target = GetNearestTargetInRange();
        if (target == null) return;

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
        await _net.SendCommandAsync($"Fire,{dx},{dy}");
        _fireTimer.Start();
    }

    private void PbBattlefield_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float cellSize = GetCellSize();
        int gridSize = 100;

        g.Clear(BgColor);

        using var gridPen = new Pen(GridColor, 1);
        for (int i = 0; i <= gridSize; i++)
        {
            float p = i * cellSize;
            g.DrawLine(gridPen, _offsetX + p, _offsetY, _offsetX + p, _offsetY + gridSize * cellSize);
            g.DrawLine(gridPen, _offsetX, _offsetY + p, _offsetX + gridSize * cellSize, _offsetY + p);
        }

        if (_state == null) return;

        if (_state.LocalShip != null)
        {
            float cx = _offsetX + _state.LocalShip.Px * cellSize;
            float cy = _offsetY + _state.LocalShip.Py * cellSize;
            float rangePx = 5 * cellSize;
            using var rangePen = new Pen(Color.FromArgb(60, 255, 255, 255), 1) { DashStyle = DashStyle.Dash };
            g.DrawEllipse(rangePen, cx - rangePx, cy - rangePx, rangePx * 2, rangePx * 2);
        }

        foreach (var ship in _state.AllShips)
        {
            if (ship.Fx >= 0 && ship.Fy >= 0)
            {
                float fx = _offsetX + ship.Fx * cellSize;
                float fy = _offsetY + ship.Fy * cellSize;
                g.FillEllipse(Brushes.Yellow, fx - 4, fy - 4, 8, 8);
            }
        }

        // Determine which enemies are in range
        var inRangeTarget = GetNearestTargetInRange();
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

        foreach (var ship in _state.AllShips)
        {
            bool isLocal = ship == _state.LocalShip;
            bool inRange = allInRange.Contains(ship);
            float sx = _offsetX + ship.Px * cellSize;
            float sy = _offsetY + ship.Py * cellSize;
            float r = isLocal ? 10 : 8;

            if (inRange)
            {
                r = 10;
                using var glowBrush = new SolidBrush(Color.FromArgb(60, InRangeBorder));
                g.FillEllipse(glowBrush, sx - 14, sy - 14, 28, 28);
            }

            using var brush = new SolidBrush(isLocal ? SelfColor : EnemyColor);
            g.FillEllipse(brush, sx - r, sy - r, r * 2, r * 2);

            using var borderPen = new Pen(
                inRange ? InRangeBorder : isLocal ? Color.White : Color.FromArgb(180, 180, 180), inRange ? 3 : 2);
            g.DrawEllipse(borderPen, sx - r, sy - r, r * 2, r * 2);

            // HP bar
            float barWidth = 24;
            float barHeight = 4;
            float barX = sx - barWidth / 2;
            float barY = sy + r + 2;
            g.FillRectangle(Brushes.Gray, barX, barY, barWidth, barHeight);
            float hpRatio = Math.Clamp(ship.HP / 3f, 0, 1);
            using var hpBrush = new SolidBrush(hpRatio > 0.5f ? Color.Green : hpRatio > 0.25f ? Color.Orange : Color.Red);
            g.FillRectangle(hpBrush, barX, barY, barWidth * hpRatio, barHeight);

            // Name label
            string label = isLocal ? $"★ {ship.ShipName}" : ship.ShipName;
            using var nameFont = new Font("Microsoft YaHei", 8, isLocal ? FontStyle.Bold : FontStyle.Regular);
            var textSize = g.MeasureString(label, nameFont);
            float textX = sx - textSize.Width / 2;
            float textY = barY + barHeight + 1;
            g.DrawString(label, nameFont, Brushes.White, textX, textY);
        }
    }

    private float GetCellSize()
    {
        float cs = Math.Min(pbBattlefield.Width, pbBattlefield.Height) / 100f;
        float gridPx = 100 * cs;
        _offsetX = (pbBattlefield.Width - gridPx) / 2f;
        _offsetY = (pbBattlefield.Height - gridPx) / 2f;
        return cs;
    }
}
