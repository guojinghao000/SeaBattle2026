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
    /// <summary>已发送但服务端尚未通过 Data 确认的移动偏移量，用于开火补偿</summary>
    private int _pendingDx, _pendingDy;
    private bool _showEnemyCooldown = true;
    private bool _loggedIn;
    private bool _disconnecting;
    private bool _scoreDirty;
    private readonly HashSet<Keys> _heldKeys = new();

    // Advanced AI state
    private string? _lastTargetId;
    private enum AiMode { Aggressive, Survivor, Patrol }
    private AiMode _aiMode = AiMode.Patrol;
    private int _aiStuckCounter;
    private int _aiLastPx, _aiLastPy;

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
        ApplyDarkTheme();
    }

    /// <summary>
    /// Apply dark navy theme with custom-drawn controls.
    /// </summary>
    private void ApplyDarkTheme()
    {
        var bgDark = Color.FromArgb(10, 20, 40);
        var bgPanel = Color.FromArgb(16, 30, 54);
        var bgCard = Color.FromArgb(20, 36, 62);
        var fgText = Color.FromArgb(220, 230, 248);
        var fgDim = Color.FromArgb(140, 160, 190);
        var accent = Color.FromArgb(56, 140, 240);
        var accentBright = Color.FromArgb(80, 170, 255);
        var inputBg = Color.FromArgb(8, 16, 32);
        var borderColor = Color.FromArgb(35, 55, 90);
        var dangerColor = Color.FromArgb(240, 70, 70);
        var successColor = Color.FromArgb(30, 200, 120);
        var warningColor = Color.FromArgb(240, 170, 40);

        // Form
        this.BackColor = bgDark;
        this.ForeColor = fgText;
        this.Padding = new Padding(6);

        // ── GroupBoxes ──
        void StyleGroup(GroupBox gb)
        {
            gb.BackColor = bgPanel;
            gb.ForeColor = accentBright;
            gb.Font = new Font("Microsoft YaHei", 9.5f, FontStyle.Bold);
        }
        StyleGroup(gbLogin);
        StyleGroup(gbHints);
        StyleGroup(gbScore);

        // ── Labels ──
        void StyleLabel(Label lbl, Color? fg = null)
        {
            lbl.BackColor = Color.Transparent;
            lbl.ForeColor = fg ?? fgDim;
            lbl.Font = new Font("Microsoft YaHei", 9f, FontStyle.Regular);
        }
        StyleLabel(lblIP);
        StyleLabel(lblShipName);
        StyleLabel(lblCaptain);
        StyleLabel(lblCrew);
        StyleLabel(lblHint1, fgDim);
        StyleLabel(lblHint2, fgDim);
        StyleLabel(lblHint3, fgDim);
        StyleLabel(lblStatus, Color.FromArgb(120, 150, 190));
        lblStatus.Font = new Font("Microsoft YaHei", 8.5f, FontStyle.Regular);

        lblShipStatus.BackColor = Color.Transparent;
        lblShipStatus.ForeColor = fgText;
        lblShipStatus.Font = new Font("Microsoft YaHei", 9f, FontStyle.Regular);

        // ── Buttons with hover effects ──
        void StyleButton(Button btn, Color bg, Color border)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = border;
            btn.BackColor = bg;
            btn.ForeColor = fgText;
            btn.Font = new Font("Microsoft YaHei", 9f, FontStyle.Regular);
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = ControlPaint.Light(bg, 0.2f);
                btn.FlatAppearance.BorderColor = ControlPaint.Light(border, 0.3f);
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = bg;
                btn.FlatAppearance.BorderColor = border;
            };
        }
        StyleButton(btnConnect, accent, accentBright);
        StyleButton(btnDisconnect, bgCard, borderColor);
        StyleButton(btnToggleMode, bgCard, borderColor);

        // ── CheckBox ──
        cbAutoBattle.BackColor = Color.Transparent;
        cbAutoBattle.ForeColor = successColor;
        cbAutoBattle.Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold);

        // ── TextBoxes ──
        void StyleText(TextBox tb)
        {
            tb.BackColor = inputBg;
            tb.ForeColor = fgText;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = new Font("Microsoft YaHei", 9f, FontStyle.Regular);
        }
        StyleText(txtIP);
        StyleText(txtShipName);
        StyleText(txtCaptain);
        StyleText(txtCrew);

        // ── Score ListBox: owner-draw with HP bar ──
        lstScore.BackColor = bgCard;
        lstScore.ForeColor = fgText;
        lstScore.BorderStyle = BorderStyle.None;
        lstScore.DrawMode = DrawMode.OwnerDrawFixed;
        lstScore.ItemHeight = 28;
        lstScore.DrawItem += (s, e) =>
        {
            if (e.Index < 0 || _state == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var sorted = _state.SortedByScore;
            if (e.Index >= sorted.Count) return;
            var ship = sorted[e.Index];
            bool isLocal = ship == _state.LocalShip;
            var rowRect = e.Bounds;
            int y = rowRect.Y;

            // Row background
            if (isLocal)
                using (var localBg = new SolidBrush(Color.FromArgb(30, 56, 120, 240)))
                    g.FillRectangle(localBg, rowRect);
            else if (e.Index % 2 == 0)
                using (var altBg = new SolidBrush(Color.FromArgb(12, 255, 255, 255)))
                    g.FillRectangle(altBg, rowRect);

            // Bottom separator
            using var sepPen = new Pen(Color.FromArgb(16, 255, 255, 255));
            g.DrawLine(sepPen, 0, rowRect.Bottom - 1, rowRect.Width, rowRect.Bottom - 1);

            // Rank + Name
            string star = isLocal ? "★" : " ";
            string label = $"#{e.Index + 1} {star} {ship.ShipName}";
            var nameFont = isLocal ? new Font("Microsoft YaHei", 9f, FontStyle.Bold)
                                   : new Font("Microsoft YaHei", 9f, FontStyle.Regular);
            var nameBrush = isLocal ? new SolidBrush(accentBright) : new SolidBrush(fgText);
            g.DrawString(label, nameFont, nameBrush, 8, y + 5);
            nameFont.Dispose(); nameBrush.Dispose();

            // Score text (right-aligned)
            string scoreText = $"{ship.Score}杀";
            var scoreFont = new Font("Consolas", 8.5f, FontStyle.Bold);
            var scoreBrush = new SolidBrush(warningColor);
            var scoreSize = g.MeasureString(scoreText, scoreFont);
            float scoreX = rowRect.Width - scoreSize.Width - 10;
            g.DrawString(scoreText, scoreFont, scoreBrush, scoreX, y + 7);
            scoreFont.Dispose(); scoreBrush.Dispose();

            // CD status (left of score)
            string cdText;
            if (ship == _state.LocalShip || _showEnemyCooldown)
                cdText = ship.FireCooldownMs > 0 ? $"CD:{ship.FireCooldownMs / 1000.0:F1}s" : "就绪";
            else
                cdText = "--";
            var cdFont = new Font("Consolas", 7.5f, FontStyle.Regular);
            var cdBrush = new SolidBrush(Color.FromArgb(160, 160, 160));
            var cdSize = g.MeasureString(cdText, cdFont);
            float cdX = scoreX - cdSize.Width - 6;
            g.DrawString(cdText, cdFont, cdBrush, cdX, y + 8);
            cdFont.Dispose(); cdBrush.Dispose();

            // HP bar (left of CD)
            int barX = (int)cdX - 64;
            int barY = y + 10;
            int barW = 60;
            int barH = 5;
            using (var barBg = new SolidBrush(Color.FromArgb(40, 42, 50)))
                g.FillRectangle(barBg, barX, barY, barW, barH);
            float hpRatio = Math.Clamp(ship.HP / 3f, 0, 1);
            var hpColor = hpRatio > 0.5f ? successColor : hpRatio > 0.25f ? warningColor : dangerColor;
            using var hpBrush = new SolidBrush(hpColor);
            g.FillRectangle(hpBrush, barX, barY, barW * hpRatio, barH);
        };

        // ── Status card: custom painted background ──
        lblShipStatus.Paint += (s, e) =>
        {
            if (_state?.LocalShip == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = lblShipStatus.ClientRectangle;

            using var cardBrush = new SolidBrush(bgCard);
            using var cardPath = RoundedRect(rect, 5);
            g.FillPath(cardBrush, cardPath);
            using var cardPen = new Pen(borderColor);
            g.DrawPath(cardPen, cardPath);

            var local = _state.LocalShip;
            using var font = new Font("Microsoft YaHei", 9f, FontStyle.Regular);
            using var fontSmall = new Font("Microsoft YaHei", 8f, FontStyle.Regular);
            var textBrush = new SolidBrush(fgText);

            // Row 1: HP bar + compact stats
            int barX = 8, barY = 5, barW = 65, barH = 6;
            using (var barBg = new SolidBrush(Color.FromArgb(30, 32, 40)))
                g.FillRectangle(barBg, barX, barY, barW, barH);
            float hpRatio = Math.Clamp(local.HP / 3f, 0, 1);
            var hpColor = hpRatio > 0.5f ? successColor : hpRatio > 0.25f ? warningColor : dangerColor;
            using var hpBrush = new SolidBrush(hpColor);
            g.FillRectangle(hpBrush, barX, barY, barW * hpRatio, barH);

            string row1 = $"📍({local.Px},{local.Py})  ❤️{local.HP}/3  💀{local.Score}";
            g.DrawString(row1, font, textBrush, barX + barW + 6, 2);

            // Row 2: AI mode + cooldown
            string cdText = _canFire ? "⚡就绪" : $"⏳{local.FireCooldownMs / 1000.0:F1}s";
            string aiText = cbAutoBattle.Checked ? $"[AI] {_aiMode}" : "[手] 手动";
            g.DrawString($"{aiText}    {cdText}", fontSmall, textBrush, barX, 24);
        };
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
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
        int count = _state.AllShips.Count;
        if (lstScore.Items.Count != count)
        {
            lstScore.Items.Clear();
            for (int i = 0; i < count; i++)
                lstScore.Items.Add(" ");
        }
        else
        {
            lstScore.Invalidate();
        }
    }

    private void GameTick(object? sender, EventArgs e)
    {
        if (_disconnecting) return;

        // 记录处理前本船坐标，用于检测服务端是否已确认移动
        int pxBefore = _state?.LocalShip?.Px ?? -1;
        int pyBefore = _state?.LocalShip?.Py ?? -1;

        ProcessMessages();

        // 如果服务端 Data 更新了本船坐标，说明之前的 Move 已被确认，清零未确认偏移
        if (_state?.LocalShip != null)
        {
            if (_state.LocalShip.Px != pxBefore || _state.LocalShip.Py != pyBefore)
            {
                _pendingDx = 0;
                _pendingDy = 0;
            }
        }

        Console.WriteLine(this.lstScore.Items.Count);

        // Auto-battle AI
        if (cbAutoBattle.Checked && _state?.LocalShip != null)
        {
            SmartTick();
        }
        else if (_heldKeys.Count == 0)
        {
            _moveDx = 0;
            _moveDy = 0;
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
        // 状态信息由 Paint 事件的自定义绘制完成，这里只需触发重绘
        lblShipStatus.Invalidate();
    }

    private async void MoveTick(object? sender, EventArgs e)
    {
        if (_disconnecting || _net == null || !_canMove || (_moveDx == 0 && _moveDy == 0)) return;

        _canMove = false;
        // 记录未确认的移动量，供 FireAt 补偿使用
        _pendingDx += _moveDx;
        _pendingDy += _moveDy;
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

        // 自动战斗：冷却一到立即开火，不等 GameTick 轮询
        if (cbAutoBattle.Checked && _state?.LocalShip != null && !_disconnecting && _net != null)
        {
            var target = GetBestTargetInRange();
            if (target != null)
            {
                _lastTargetId = target.ShipID;
                _ = FireAt(target);
            }
        }
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
        // 登录前焦点在文本框时不拦截，让用户正常输入
        if (!_loggedIn && this.ActiveControl is TextBox)
            return;

        _heldKeys.Add(e.KeyCode);
        UpdateMoveDirection();

        // 空格/Home: 回到自己视角（类似 LoL 空格键）
        if (e.KeyCode is Keys.Space or Keys.Home)
        {
            _followPlayer = true;
        }

        // J: 手动开火
        if (e.KeyCode == Keys.J)
        {
            ManualFire();
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

        // 按移动键时自动恢复视角跟随（类似 LoL：移动英雄后视角跟随回来）
        if (_moveDx != 0 || _moveDy != 0)
            _followPlayer = true;
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
    /// Squared Euclidean distance between two fleets.
    /// </summary>
    private static int GetDistSq(Fleet a, Fleet b)
    {
        int dx = a.Px - b.Px;
        int dy = a.Py - b.Py;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Count enemies within a given radius of a grid point.
    /// </summary>
    private int CountNearbyEnemies(int px, int py, int radius)
    {
        if (_state?.LocalShip == null) return 0;
        var local = _state.LocalShip;
        int rSq = radius * radius;
        int count = 0;
        foreach (var ship in _state.AllShips)
        {
            if (ship == local) continue;
            int dx = ship.Px - px;
            int dy = ship.Py - py;
            if (dx * dx + dy * dy <= rSq) count++;
        }
        return count;
    }

    /// <summary>
    /// Check if local ship is surrounded: enemies on opposing sides within range 12.
    /// </summary>
    private bool IsSurrounded()
    {
        if (_state?.LocalShip == null) return false;
        var local = _state.LocalShip;
        var ships = _state.AllShips;

        bool left = false, right = false, up = false, down = false;
        int checkSq = 12 * 12;
        foreach (var ship in ships)
        {
            if (ship == local) continue;
            int dx = ship.Px - local.Px;
            int dy = ship.Py - local.Py;
            if (dx * dx + dy * dy > checkSq) continue;
            if (dx < -2) left = true;
            if (dx > 2) right = true;
            if (dy < -2) up = true;
            if (dy > 2) down = true;
        }
        return (left && right) || (up && down);
    }

    /// <summary>
    /// Direction away from the center-of-mass of nearby enemies.
    /// </summary>
    private (int dx, int dy) GetEscapeDirection()
    {
        if (_state?.LocalShip == null) return (0, 0);
        var local = _state.LocalShip;
        float sumDx = 0, sumDy = 0;
        int n = 0, rangeSq = 15 * 15;

        foreach (var ship in _state.AllShips)
        {
            if (ship == local) continue;
            int dx = ship.Px - local.Px;
            int dy = ship.Py - local.Py;
            if (dx * dx + dy * dy <= rangeSq)
            { sumDx += dx; sumDy += dy; n++; }
        }
        if (n == 0) return (0, 0);
        return (-Math.Sign((int)sumDx), -Math.Sign((int)sumDy));
    }

    /// <summary>
    /// Score a target: higher = better to attack. Weighted by HP (low=good),
    /// distance (close=good), and persistence bonus for last target.
    /// </summary>
    private double ScoreTarget(Fleet ship)
    {
        if (_state?.LocalShip == null) return double.MinValue;
        int dx = ship.Px - _state.LocalShip.Px;
        int dy = ship.Py - _state.LocalShip.Py;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        // Base: lower HP = much higher value
        double score = (4.0 - ship.HP) * 100;

        // Distance penalty (closer = better, but don't over-penalize)
        score -= dist * 3;

        // Persistence: bonus for last targeted ship (kill confirmation)
        if (ship.ShipID == _lastTargetId)
            score += 80;

        // Cluster bonus: prefer targets in dense areas (more kills nearby)
        int nearby = CountNearbyEnemies(ship.Px, ship.Py, 8);
        score += nearby * 40;

        // Edge penalty: avoid targets hugging map edges
        double centerDist = Math.Sqrt((ship.Px - 50) * (ship.Px - 50) + (ship.Py - 50) * (ship.Py - 50));
        score -= Math.Max(0, centerDist - 40) * 0.5;

        return score;
    }

    /// <summary>
    /// Find best target within range 10 for firing.
    /// </summary>
    private Fleet? GetBestTargetInRange()
    {
        if (_state?.LocalShip == null) return null;

        Fleet? best = null;
        double bestScore = double.MinValue;
        int rangeSq = 10 * 10;

        foreach (var ship in _state.AllShips)
        {
            if (ship == _state.LocalShip) continue;
            int dx = ship.Px - _state.LocalShip.Px;
            int dy = ship.Py - _state.LocalShip.Py;
            if (dx * dx + dy * dy > rangeSq) continue;

            double score = ScoreTarget(ship);
            if (score > bestScore) { bestScore = score; best = ship; }
        }

        return best;
    }

    /// <summary>
    /// Find best target to chase (any distance). Returns null if none.
    /// </summary>
    private Fleet? GetBestChaseTarget()
    {
        if (_state?.LocalShip == null) return null;

        Fleet? best = null;
        double bestScore = double.MinValue;

        foreach (var ship in _state.AllShips)
        {
            if (ship == _state.LocalShip) continue;
            double score = ScoreTarget(ship);
            if (score > bestScore) { bestScore = score; best = ship; }
        }

        return best;
    }

    /// <summary>
    /// Main AI tick: decide mode, move, and fire.
    /// </summary>
    private void SmartTick()
    {
        var local = _state?.LocalShip;
        if (local == null) return;

        // --- Detect stuck: if position unchanged for 3s, clear last target ---
        if (local.Px == _aiLastPx && local.Py == _aiLastPy)
            _aiStuckCounter++;
        else
        { _aiStuckCounter = 0; _aiLastPx = local.Px; _aiLastPy = local.Py; }
        if (_aiStuckCounter > 6) // ~3s at 500ms tick
        { _lastTargetId = null; _aiStuckCounter = 0; }

        // --- Decide mode ---
        var fireTarget = GetBestTargetInRange();
        var chaseTarget = GetBestChaseTarget();

        if (local.HP <= 1)
        {
            // 残血：生存模式，保持距离作战
            _aiMode = AiMode.Survivor;
        }
        else if (fireTarget == null && (chaseTarget == null || GetDistSq(local, chaseTarget) > 400))
        {
            // 射程内无目标且最近敌人距离 > 20，巡逻搜敌
            _aiMode = AiMode.Patrol;
        }
        else
        {
            _aiMode = AiMode.Aggressive;
        }

        // --- Act ---
        switch (_aiMode)
        {
            case AiMode.Aggressive:
                AggressiveMove(chaseTarget, fireTarget);
                break;
            case AiMode.Survivor:
                SurvivorMove(chaseTarget, fireTarget);
                break;
            case AiMode.Patrol:
                PatrolMove();
                break;
        }

        // --- Fire at best target in range (fallback: FireTick handles primary auto-fire) ---
        if (fireTarget != null && _canFire && !_disconnecting && _net != null)
        {
            _lastTargetId = fireTarget.ShipID;
            _ = FireAt(fireTarget);
        }
    }

    /// <summary>
    /// Aggressive: close in, but avoid being surrounded.
    /// When surrounded by enemies on opposing sides, break out first.
    /// </summary>
    private void AggressiveMove(Fleet? chase, Fleet? inRange)
    {
        var local = _state!.LocalShip!;

        if (chase == null) { PatrolMove(); return; }

        // ── Anti-surround: escape if enemies on opposing sides ──
        if (IsSurrounded())
        {
            var (escX, escY) = GetEscapeDirection();
            _moveDx = escX;
            _moveDy = escY;
            ApplyEdgeBias();
            return;
        }

        int dx = chase.Px - local.Px;
        int dy = chase.Py - local.Py;
        int distSq = dx * dx + dy * dy;

        // Already at good firing distance
        if (distSq <= 100)
        {
            // If too close, back off
            if (distSq < 16)
            {
                _moveDx = -Math.Sign(dx);
                _moveDy = -Math.Sign(dy);
            }
            else
            {
                // Strafe: orbit around target while staying in range
                _moveDx = distSq > 36 ? Math.Sign(dx) : -Math.Sign(dy);
                _moveDy = distSq > 36 ? Math.Sign(dy) : Math.Sign(dx);
            }
            return;
        }

        // Chase target
        _moveDx = Math.Sign(dx);
        _moveDy = Math.Sign(dy);
        ApplyEdgeBias();
    }

    /// <summary>
    /// Survivor mode: attack any target in range, but keep safe distance.
    /// Only flee when a threat is dangerously close (&lt; 4 units).
    /// </summary>
    private void SurvivorMove(Fleet? chase, Fleet? inRange)
    {
        var local = _state!.LocalShip!;

        // If target in range, stay put and fire — no need to close distance
        if (inRange != null)
        {
            // If enemy is dangerously close, back away slightly
            int edx = inRange.Px - local.Px;
            int edy = inRange.Py - local.Py;
            if (edx * edx + edy * edy < 16)
            {
                _moveDx = -Math.Sign(edx);
                _moveDy = -Math.Sign(edy);
            }
            else
            {
                _moveDx = 0;
                _moveDy = 0;
            }
            return;
        }

        // Find nearest threat
        Fleet? threat = null;
        int minDistSq = int.MaxValue;
        foreach (var ship in _state.AllShips)
        {
            if (ship == local) continue;
            int dx = ship.Px - local.Px;
            int dy = ship.Py - local.Py;
            int d2 = dx * dx + dy * dy;
            if (d2 < minDistSq) { minDistSq = d2; threat = ship; }
        }

        // Flee from very close threats
        if (threat != null && minDistSq < 16)
        {
            _moveDx = -Math.Sign(threat.Px - local.Px);
            _moveDy = -Math.Sign(threat.Py - local.Py);
            ApplyEdgeBias();
            return;
        }

        // Cautiously approach a low-HP chase target
        if (chase != null && chase.HP == 1)
        {
            _moveDx = Math.Sign(chase.Px - local.Px);
            _moveDy = Math.Sign(chase.Py - local.Py);
            ApplyEdgeBias();
            return;
        }

        // Safe — patrol toward center
        PatrolMove();
    }

    /// <summary>
    /// Patrol: actively hunt the nearest enemy. Fall back to center only if none exist.
    /// </summary>
    private void PatrolMove()
    {
        var local = _state!.LocalShip!;

        // Find nearest enemy at any distance
        Fleet? nearest = null;
        int minDistSq = int.MaxValue;
        foreach (var ship in _state.AllShips)
        {
            if (ship == local) continue;
            int dx = ship.Px - local.Px;
            int dy = ship.Py - local.Py;
            int d2 = dx * dx + dy * dy;
            if (d2 < minDistSq) { minDistSq = d2; nearest = ship; }
        }

        if (nearest != null)
        {
            // Actively hunt nearest enemy
            int dx = nearest.Px - local.Px;
            int dy = nearest.Py - local.Py;
            _moveDx = Math.Sign(dx);
            _moveDy = Math.Sign(dy);
            ApplyEdgeBias();
            return;
        }

        // No enemies exist — wait at center
        int cdx = 50 - local.Px;
        int cdy = 50 - local.Py;
        if (Math.Abs(cdx) <= 3 && Math.Abs(cdy) <= 3)
        {
            _moveDx = (_aiStuckCounter / 3) % 2 == 0 ? 1 : -1;
            _moveDy = 0;
        }
        else
        {
            _moveDx = Math.Sign(cdx);
            _moveDy = Math.Sign(cdy);
        }
    }

    /// <summary>
    /// Bias movement away from map edges to avoid getting trapped.
    /// </summary>
    private void ApplyEdgeBias()
    {
        var local = _state!.LocalShip!;
        const int edgeMargin = 5;

        if (local.Px <= edgeMargin) _moveDx = Math.Max(_moveDx, 1);
        if (local.Px >= 100 - edgeMargin) _moveDx = Math.Min(_moveDx, -1);
        if (local.Py <= edgeMargin) _moveDy = Math.Max(_moveDy, 1);
        if (local.Py >= 100 - edgeMargin) _moveDy = Math.Min(_moveDy, -1);
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

        var local = _state.LocalShip;

        // 使用本船「预期服务端位置」= 上次 Data 确认的坐标 + 已发送未确认的移动量
        // 这补偿了客户端与服务端之间的位置不同步问题
        int localPx = local.Px + _pendingDx;
        int localPy = local.Py + _pendingDy;

        int dx = target.Px - localPx;
        int dy = target.Py - localPy;

        // Clamp to range [-10,10] within circle radius 10
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

    private void pbBattlefield_Click(object sender, EventArgs e)
    {

    }
}
