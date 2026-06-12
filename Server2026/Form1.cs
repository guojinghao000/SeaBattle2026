using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Server2026
{
    public partial class Form1 : Form
    {
        public IPAddress? serverIP;
        public int tcpPort = 18000;
        public int udpPort = 18001;
        public TcpListener? tcpListener;
        private UdpClient? _udpClient;
        private CancellationTokenSource? _udpCts;
        bool isListening = false;
        public List<Ship> shipList = new();  // 在线的用户
        private readonly object _shipLock = new();
        public System.Timers.Timer updateTimer { get; set; } = new(500);
        private readonly Bitmap shipBitmap = Resource1.ship;
        private readonly Bitmap fireBitmap = Resource1.fire;

        private const int MinBotCount = 5;
        private int _botCycle;

        public Form1()
        {
            InitializeComponent();

            serverIP = GetIpV4Address();
            updateTimer.Elapsed += UpdateTimer_Elapsed;
            updateTimer.Start();
            pictureBox1.Paint += PictureBox1_Paint;

            StartToolStripMenuItem_Click(this, EventArgs.Empty);
        }

        private void PictureBox1_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int dd = Math.Min(pictureBox1.Size.Width, pictureBox1.Size.Height) / 100;
            List<Ship> snapshot;
            lock (_shipLock) { snapshot = shipList.ToList(); }
            foreach (var ship in snapshot)
            {
                g.DrawImage(shipBitmap, ship.px * dd, ship.py * dd);
                if (ship.fx > 0)
                    g.DrawImage(fireBitmap, ship.fx * dd, ship.fy * dd);
            }
        }

        private void UpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            List<Ship> snapshot;
            lock (_shipLock) { snapshot = shipList.ToList(); }

            if (snapshot.Count == 0) { pictureBox1.Invalidate(); return; }

            foreach (var ship in snapshot)
            {
                if (ship.fx != -1)
                {
                    CheckShipHit(snapshot, ship);
                }
            }

            string str = "Data,";
            foreach (var ship in snapshot)
            {
                str += $"{ship.shipID},{ship.px},{ship.py},{ship.fx},{ship.fy},{ship.hp},{ship.score},{ship.FireCooldownMs},";
                ship.fx = -1;
                ship.fy = -1;
            }
            str = str.TrimEnd(',');
            SendToAll(snapshot, str);
            pictureBox1.Invalidate();

            RefillBots();
        }

        private void SpawnBots()
        {
            bool added = false;
            lock (_shipLock)
            {
                for (int i = 0; i < MinBotCount; i++)
                {
                    var bot = new Ship(null)
                    {
                        shipName = $"靶船-{(char)('A' + i)}",
                        captainName = "AI",
                        crewNames = "AI",
                        IsBot = true
                    };
                    bot.moveTimer.Stop();
                    bot.fireTimer.Stop();
                    shipList.Add(bot);
                    added = true;
                }
            }
            if (added)
            {
                List<Ship> snapshot;
                lock (_shipLock) { snapshot = shipList.ToList(); }
                SendToAll(snapshot, GetAllShipName());
                AddMessage($"已生成 {MinBotCount} 个机器人靶船");
            }
        }

        private void RefillBots()
        {
            bool added = false;
            lock (_shipLock)
            {
                int botCount = shipList.Count(s => s.IsBot);
                if (botCount < MinBotCount)
                {
                    int toAdd = MinBotCount - botCount;
                    for (int i = 0; i < toAdd; i++)
                    {
                        var bot = new Ship(null)
                        {
                            shipName = $"靶船-{(char)('Z' - _botCycle % 26)}",
                            captainName = "AI",
                            crewNames = "AI",
                            IsBot = true
                        };
                        _botCycle++;
                        bot.moveTimer.Stop();
                        bot.fireTimer.Stop();
                        shipList.Add(bot);
                    }
                    added = true;
                }
            }
            if (added)
            {
                List<Ship> snapshot;
                lock (_shipLock) { snapshot = shipList.ToList(); }
                SendToAll(snapshot, GetAllShipName());
                AddMessage("已补充机器人靶船");
            }
        }

        public void CheckShipHit(List<Ship> shipList, Ship attacker)
        {
            if (shipList == null || attacker == null)
                return;

            foreach (var targetShip in shipList)
            {
                if (targetShip.shipID == attacker.shipID)
                    continue;

                if (targetShip.px == attacker.fx && targetShip.py == attacker.fy)
                {
                    AddMessage($"命中 {targetShip.shipID}，当前HP: {targetShip.hp}");

                    targetShip.hp--;

                    if (targetShip.hp <= 0)
                    {
                        AddMessage($"击沉 {targetShip.shipID}，正在重生...");
                        targetShip.ReSet();

                        attacker.score++;
                        attacker.hp = 3;
                        AddMessage($"攻击者 {attacker.shipID} 得分+1，当前得分: {attacker.score}");
                    }
                    break;  // 一发炮弹只命中一个目标
                }
            }
        }

        private void StartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartToolStripMenuItem.Enabled = false;
            StopToolStripMenuItem.Enabled = true;

            if (serverIP != null)
            {
                tcpListener = new TcpListener(IPAddress.Any, tcpPort);
                tcpListener.Start();
                isListening = true;
                AddMessage($"开始侦听{serverIP}:{tcpPort}，等待客户连接");
                this.Text = $"大海战 服务端 0.0.1 - {serverIP}:{tcpPort}";
                Thread listenThread = new(ListenClientConnect);
                listenThread.Start();

                StartUdpListener();

                lock (_shipLock)
                {
                    shipList.RemoveAll(s => s.IsBot);
                }
                SpawnBots();
            }
            else
            {
                AddMessage("获取IPv4失败，无法通过IPv4侦听客户连接");
            }
        }

        private void ListenClientConnect()
        {
            while (isListening)
            {
                if (tcpListener == null) return;
                TcpClient newClient = new();
                try
                {
                    newClient = tcpListener.AcceptTcpClient();
                }
                catch (Exception ex)
                {
                    AddMessage($"等待用户连接异常：{ex.Message}");
                    AddMessage("即将停止接收客户连接");
                    break;
                }
                Ship ship = new(newClient);
                lock (_shipLock) { shipList.Add(ship); }
                AddMessage($"来自 {newClient.Client.RemoteEndPoint} 加入游戏");
                lock (_shipLock) { AddMessage($"当前在线用户数：{shipList.Count}"); }

                Thread receiveThread = new(ReceiveData);
                receiveThread.Start(ship);
            }
        }

        public void ReceiveData(object? obj)
        {
            if (obj == null) return;
            Ship ship = (Ship)obj;
            bool exitWhile = false;
            while (exitWhile == false)
            {
                string? receiveString = null;
                try
                {
                    receiveString = ship.SReader?.ReadLine();
                }
                catch (Exception ex)
                {
                    AddMessage($"接收数据失败：{ex.Message}");
                    RemoveShip(ship);
                    break;
                }
                if (receiveString == null)
                {
                    if (ship.Client != null && ship.Client.Connected == true)
                    {
                        AddMessage($"与 {ship.Client.Client.RemoteEndPoint} 失去联系，停止接收该用户信息");
                    }
                    ship.moveTimer.Stop();
                    ship.fireTimer.Stop();
                    RemoveShip(ship);
                    break;
                }
                try
                {
                    string[] splitString = receiveString.Split(',');
                    string command = splitString[0].ToLower();
                    switch (command)
                    {
                        case "login":
                            if (splitString.Length < 4) break;
                            ship.shipName = splitString[1];
                            ship.captainName = splitString[2];
                            ship.crewNames = splitString[3];
                            {
                                List<Ship> snapshot;
                                lock (_shipLock) { snapshot = shipList.ToList(); }
                                SendToAll(snapshot, GetAllShipName());
                            }
                            break;
                        case "logout":
                            AddMessage($"{ship.shipName} 退出游戏");
                            exitWhile = true;
                            break;
                        case "move":
                            if (ship.allowMove)
                            {
                                int x = int.Parse(splitString[1]);
                                int y = int.Parse(splitString[2]);
                                x = Math.Clamp(x, -1, 1);
                                y = Math.Clamp(y, -1, 1);
                                ship.px += x;
                                ship.py += y;
                                ship.px = Math.Clamp(ship.px, 0, 100);
                                ship.py = Math.Clamp(ship.py, 0, 100);
                                ship.allowMove = false;
                            }
                            break;
                        case "fire":
                            if (ship.allowFire)
                            {
                                int dx = int.Parse(splitString[1]);
                                int dy = int.Parse(splitString[2]);
                                var result = ClampToCircle(dx, dy);
                                ship.fx = ship.px + result.limitedX;
                                ship.fy = ship.py + result.limitedY;
                                ship.allowFire = false;
                                ship.LastFireTime = DateTime.Now;
                                ship.fireTimer.Stop();
                                ship.fireTimer.Start();
                            }
                            break;
                        default:
                            {
                                List<Ship> snapshot;
                                lock (_shipLock) { snapshot = shipList.ToList(); }
                                SendToAll(snapshot, "未知数据：" + receiveString);
                            }
                            break;
                    }
                }
                catch (Exception)
                {
                    AddMessage($"解析异常 {ship.shipName}:{receiveString}");
                }
        }
        ship.moveTimer.Stop();
        ship.fireTimer.Stop();
        ship.SWriter?.Close();
        ship.SReader?.Close();
        ship.Client?.Close();
        RemoveShip(ship);
        pictureBox1.Invalidate();
        List<Ship> remaining;
        lock (_shipLock) { remaining = shipList.ToList(); }
        SendToAll(remaining, GetAllShipName());
    }

    private void StartUdpListener()
        {
            try
            {
                _udpClient = new UdpClient();
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress, true);
                _udpClient.ExclusiveAddressUse = false;
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, udpPort));
                _udpClient.EnableBroadcast = true;
                _udpCts = new CancellationTokenSource();
                Thread udpThread = new(() => ListenUdp(_udpCts.Token));
                udpThread.Start();
                AddMessage($"UDP广播监听已启动，端口:{udpPort}");
            }
            catch (Exception ex)
            {
                AddMessage($"UDP监听启动失败：{ex.Message}");
            }
        }

        private void ListenUdp(CancellationToken token)
        {
            if (_udpClient == null) return;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint remoteEP = new(IPAddress.Any, 0);
                    byte[] data = _udpClient.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);
                    AddMessage($"UDP广播[{remoteEP}]: {message}");

                    if (message == "Discovery" && serverIP != null)
                    {
                        byte[] response = Encoding.UTF8.GetBytes(
                            $"Server,{serverIP},{tcpPort}");
                        _udpClient.Send(response, response.Length, remoteEP);
                    }
                }
                catch (SocketException) when (!token.IsCancellationRequested)
                { Thread.Sleep(100); }
                catch (ObjectDisposedException)
                { break; }
            }
        }

        public string GetAllShipName()
        {
            string str = "Online,";
            List<Ship> snapshot;
            lock (_shipLock) { snapshot = shipList.ToList(); }
            foreach (var ship in snapshot)
            {
                str += $"{ship.shipID},{ship.shipName},{ship.captainName},{ship.crewNames},";
            }
            str = str.TrimEnd(',');
            return str;
        }

        private void RemoveShip(Ship ship)
        {
            lock (_shipLock)
            {
                if (shipList.Contains(ship))
                    shipList.Remove(ship);
            }
        }

        private void StopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartToolStripMenuItem.Enabled = true;
            StopToolStripMenuItem.Enabled = false;
            isListening = false;
            _udpCts?.Cancel();
            _udpClient?.Close();
            tcpListener?.Stop();

            List<Ship> snapshot;
            lock (_shipLock) { snapshot = shipList.ToList(); }
            foreach (var ship in snapshot)
            {
                ship.Client?.Close();
            }
            lock (_shipLock) { shipList.Clear(); }
        }

        private void IPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"服务器IP：{serverIP} 端口：{tcpPort}");
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcpListener?.Stop();
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            updateTimer.Stop();
            isListening = false;
            _udpCts?.Cancel();
            _udpClient?.Close();
            tcpListener?.Stop();

            List<Ship> snapshot;
            lock (_shipLock) { snapshot = shipList.ToList(); }
            foreach (var ship in snapshot)
            {
                ship.Client?.Close();
            }
            lock (_shipLock) { shipList.Clear(); }

            Environment.Exit(0);
        }

        public IPAddress? GetIpV4Address()
        {
            IPAddress[] addrIP = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress? localIPv4Address = null;
            foreach (var ip in addrIP)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIPv4Address = ip;
                    break;
                }
            }
            return localIPv4Address;
        }

        public void SendToOne(Ship ship, string message)
        {
            if (ship.SWriter == null) return;
            try
            {
                ship.SWriter.WriteLine(message);
                ship.SWriter.Flush();
            }
            catch (Exception ex)
            {
                AddMessage($"向{ship.shipName}发送消息失败：{ex.Message}");
                RemoveShip(ship);
            }
        }

        public void SendToAll(List<Ship> ships, string str)
        {
            foreach (Ship ship in ships)
            {
                if (ship.Client != null)
                {
                    SendToOne(ship, str);
                }
            }
        }

        public void AddMessage(string str)
        {
            if (listBox1.InvokeRequired)
            {
                try
                {
                    listBox1.Invoke(AddMessage, str);
                }
                catch
                {
                }
            }
            else
            {
                listBox1.Items.Add(str);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                listBox1.ClearSelected();
            }
        }

        public (int limitedX, int limitedY) ClampToCircle(int x, int y)
        {
            const float radius = 10f;

            float squaredDistance = x * x + y * y;

            if (squaredDistance <= radius * radius)
            {
                return (x, y);
            }

            float distance = (float)Math.Sqrt(squaredDistance);
            float scale = radius / distance;
            int limitedX = (int)(x * scale);
            int limitedY = (int)(y * scale);

            return (limitedX, limitedY);
        }
    }
}
