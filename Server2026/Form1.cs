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
        bool isListening = false;
        public List<Ship> shipList = new();  // 在线的用户
        private readonly object _shipLock = new();
        public System.Timers.Timer updateTimer { get; set; } = new(500);
        private readonly Bitmap shipBitmap = Resource1.ship;
        private readonly Bitmap fireBitmap = Resource1.fire;

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
                str += $"{ship.shipID},{ship.px},{ship.py},{ship.fx},{ship.fy},{ship.hp},{ship.score},";
                ship.fx = -1;
                ship.fy = -1;
            }
            str.TrimEnd(',');
            SendToAll(snapshot, str);
            pictureBox1.Invalidate();
        }

        public void CheckShipHit(List<Ship> shipList, Ship attacker)
        {
            if (shipList == null || attacker == null)
                return;

            foreach (var targetShip in shipList)
            {
                // 跳过自身，不能攻击自己
                if (targetShip.shipID == attacker.shipID)
                    continue;

                // 判断是否命中：目标船只位置等于攻击坐标(fx/fy)
                if (targetShip.px == attacker.fx && targetShip.py == attacker.fy)
                {
                    AddMessage($"命中 {targetShip.shipID}，当前HP: {targetShip.hp}");

                    targetShip.hp--;

                    if (targetShip.hp <= 0)
                    {
                        AddMessage($"击沉 {targetShip.shipID}，正在重生...");
                        targetShip.ReSet();

                        attacker.score++;
                        attacker.hp = 3; // 恢复攻击者HP（评分要求）
                        AddMessage($"攻击者 {attacker.shipID} 得分+1，当前得分: {attacker.score}");
                    }
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
                    newClient = tcpListener.AcceptTcpClient();  // 等待用户连接
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

        // 每个客户端对应一个ReceiveData线程，用于接收该客户端发送的信息并处理
        public void ReceiveData(object? obj)
        {
            if (obj == null) return;
            Ship ship = (Ship)obj;
            bool exitWhile = false;   // 用于控制是否退出循环
            while (exitWhile == false)
            {
                string? receiveString = null;
                try
                {
                    receiveString = ship.SReader?.ReadLine();
                }
                catch (Exception ex)
                {
                    // 该客户端网络连接断开时触发异常
                    AddMessage($"接收数据失败：{ex.Message}");
                    RemoveShip(ship);
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
                // 频率较高的信息可以不显示在服务器界面上，这里只显示关键信息
                // AddMessage($"-----来自 {ship.shipName}：{receiveString}");
                try
                {
                    string[] splitString = receiveString.Split(',');
                    string command = splitString[0].ToLower();
                    switch (command)
                    {
                        case "login":  // 用户刚刚登录(格式：Login,shipName,CaptainName,crewNames)
                            ship.shipName = splitString[1];
                            ship.captainName = splitString[2];
                            ship.crewNames = splitString[3];
                            SendToAll(shipList, GetAllShipName());
                            break;
                        case "logout":  // 用户退出游戏(格式：Logout)
                            AddMessage($"{ship.shipName} 退出游戏");
                            exitWhile = true;
                            break;
                        case "move":  // Move,x,y(x,y取值范围[-1,1])
                            if (ship.allowMove)
                            {
                                int x = int.Parse(splitString[1]);
                                int y = int.Parse(splitString[2]);
                                x = Math.Clamp(x, -1, 1);
                                y = Math.Clamp(y, -1, 1);
                                ship.px += x;
                                ship.py += y;
                                ship.px = Math.Clamp(ship.px, 1, 100);
                                ship.py = Math.Clamp(ship.py, 1, 100);
                                ship.allowMove = false;
                            }
                            break;
                        case "fire":  // Fire,x,y(x,y取值范围[-5,5]，需满足x2+y2≤25)
                            if (ship.allowFire)
                            {
                                int x = int.Parse(splitString[1]);
                                int y = int.Parse(splitString[2]);
                                var result = ClampToCircle(x, y);
                                ship.fx = result.limitedX;
                                ship.fy = result.limitedY;
                                ship.allowFire = false;
                            }
                            break;
                        default:
                            SendToAll(shipList, "未知数据：" + receiveString);
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

        public string GetAllShipName()
        {
            string str = "Online,";
            List<Ship> snapshot;
            lock (_shipLock) { snapshot = shipList.ToList(); }
            foreach (var ship in snapshot)
            {
                str += $"{ship.shipID},{ship.shipName},{ship.captainName},{ship.crewNames},";
            }
            str.TrimEnd(',');
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
            tcpListener?.Stop();
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
                // AddMessage($"向{ship.shipName}发送{message}");
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
                    // do nothing
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
            const float radius = 5f;

            // 计算点到圆心(0,0)的距离平方
            float squaredDistance = x * x + y * y;

            // 如果在圆内或圆上，直接返回原始值
            if (squaredDistance <= radius * radius)
            {
                return (x, y);
            }

            // 计算实际距离
            float distance = (float)Math.Sqrt(squaredDistance);

            // 计算缩放比例，映射到圆边缘
            float scale = radius / distance;

            // 计算限制后坐标
            int limitedX = (int)(x * scale);
            int limitedY = (int)(y * scale);

            return (limitedX, limitedY);
        }
    }
}
