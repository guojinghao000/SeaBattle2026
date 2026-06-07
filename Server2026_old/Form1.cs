using System.Net.Sockets;
using System.Net;
using Microsoft.VisualBasic.ApplicationServices;
using System.Xml;
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
        public List<Ship> shipList = new();  //连接的用户
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

            //测试
            //shipList.Add(new Ship(null) { px=50,py=50,fx=15,fy=20 });
            //shipList.Add(new Ship(null) { px = 100, py = 100, fx = 15, fy = 20 });
        }

        private void PictureBox1_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int dd = Math.Min(pictureBox1.Size.Width, pictureBox1.Size.Height) / 100;
            foreach (var ship in shipList)
            {
                g.DrawImage(shipBitmap, ship.px * dd, ship.py * dd);
                if(ship.fx>0)
                    g.DrawImage(fireBitmap, ship.fx * dd, ship.fy * dd);
            }
        }

        private void UpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (shipList.Count > 0)
            {
                foreach (var ship in shipList)
                {
                    if (ship.fx != -1)
                    {
                        CheckShipHit(shipList, ship);
                        //ship.fx = -1;不是在这，在发送后再设置为-1，这样可以保证在发送给客户端时fx和fy的值是正确的
                        //ship.fy = -1;
                    }
                }
                //攻击判定完成后统一发送所有船只状态给客户端
                string str = "Data,";
                foreach (var ship in shipList)
                {
                    str += $"{ship.shipID},{ship.px},{ship.py},{ship.fx},{ship.fy},{ship.hp},{ship.score},";                    
                    //发送完后重置fx和fy
                    ship.fx = -1;
                    ship.fy = -1;
                }
                str.TrimEnd(',');
                SendToAll(shipList, str);
                pictureBox1.Invalidate();
            }
        }

        public void CheckShipHit(List<Ship> shipList, Ship attacker)
        {
            // 空值校验
            if (shipList == null || attacker == null)
                return;

            // 遍历所有船只检测击中状态
            foreach (var targetShip in shipList)
            {
                // 跳过攻击者自身（避免自击）
                if (targetShip.shipID == attacker.shipID)
                    continue;

                // 判断是否击中：目标船的坐标等于攻击坐标（fx/fy）
                if (targetShip.px == attacker.fx && targetShip.py == attacker.fy)
                {
                    AddMessage($"舰队 {targetShip.shipID} 被击中！当前HP: {targetShip.hp}");

                    // HP减1
                    targetShip.hp--;

                    // 如果HP减为0，处理重生和攻击者得分
                    if (targetShip.hp <= 0)
                    {
                        AddMessage($"舰队 {targetShip.shipID} 被击沉！正在重生...");
                        targetShip.ReSet();
                        
                        // 攻击者得分+1
                        attacker.score++;
                        attacker.hp = 3; // 重置攻击者HP（如果需要）
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
                tcpListener = new TcpListener(serverIP, tcpPort);
                tcpListener.Start();
                isListening = true;
                AddMessage($"开始在{serverIP}:{tcpPort}监听舰队连接");
                Thread listenThread = new(ListenClientConnect);
                listenThread.Start();
            }
            else
            {
                AddMessage("获取IPv4失败，无法通过IPv4监听客户端连接");
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
                    newClient = tcpListener.AcceptTcpClient();  //等待用户进入
                }
                catch (Exception ex)
                {
                    AddMessage($"等待用户进入出错：{ex.Message}");
                    AddMessage("已终止接收客户端连接");
                    break;
                }
                Ship ship = new(newClient);
                shipList.Add(ship);
                AddMessage($"【{newClient.Client.RemoteEndPoint}】进入海域");
                AddMessage($"当前连接用户数：{shipList.Count}");

                Thread receiveThread = new(ReceiveData);
                receiveThread.Start(ship);
            }
        }

        //每个客户端对应一个ReceiveData线程，用于接收该客户端发送的消息并进行处理
        public void ReceiveData(object? obj)
        {
            if (obj == null)
            {
                return;
            }
            Ship ship = (Ship)obj;
            bool exitWhile = false;   //用于控制是否退出循环
            while (exitWhile == false)
            {
                string? receiveString = null;
                try
                {
                    receiveString = ship.SReader?.ReadLine();
                }
                catch (Exception ex)
                {
                    //该客户底层套接字不存在时会出现异常
                    AddMessage($"接收数据失败：{ex.Message}");
                    //移除该用户
                    RemoveShip(ship);
                }
                if (receiveString == null)
                {
                    if (ship.Client != null)
                    {
                        if (ship.Client.Connected == true) //true表示未停止监听
                        {
                            AddMessage($"与{ship.Client.Client.RemoteEndPoint}失去联系，已终止接收该用户信息");

                            //发送格式：Lost，座位号，用户名
                            //Service.SendToOne(user1, $"Lost,{j},{user1}");

                        }
                    }
                    RemoveShip(ship);
                    break;  //退出循环
                }
                //频率较高的消息可以不显示在服务器界面上，或者只显示部分内容
                //AddMessage($"-----来自{ship.shipName}：{receiveString}");
                try
                {
                    string[] splitString = receiveString.Split(',');
                    string command = splitString[0].ToLower();
                    switch (command)
                    {
                        case "login":  //该用户刚刚登录(格式：Login,shipName,CaptainName)
                            ship.shipName = splitString[1];
                            ship.captainName = splitString[2];
                            ship.crewNames = splitString[3];
                            SendToAll(shipList, GetAllShipName());                            
                            break;
                        case "logout":  //用户退出游戏室(格式：Logout)
                            AddMessage($"{ship.shipName}退出海域");
                            exitWhile = true;   //停止接收该客户端消息
                            break;
                        case "move":  //Move,x,y(x,y取值范围[-1,1])
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
                        case "fire":  //Fire,x,y(x,y取值范围[-5,5]，需满足x2+y2≤25)
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
                            SendToAll(shipList, "未知内容：" + receiveString);
                            break;
                    }
                }
                catch (Exception)
                {
                    AddMessage($"内容异常 {ship.shipName}:{receiveString}");
                }
            }
            ship.Client?.Close();
            RemoveShip(ship);
            //AddMessage($"有一个用户退出，剩余连接用户数：{shipList.Count}");
            SendToAll(shipList, GetAllShipName());
        }

        public string GetAllShipName()
        {
            string str = "Online,";
            foreach (var ship in shipList)
            {
                str += $"{ship.shipID},{ship.shipName},{ship.captainName},{ship.crewNames},";
            }
            str.TrimEnd(',');
            return str;
        }

        private void RemoveShip(Ship ship)
        {
            if (shipList.Contains(ship))
            {
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
                //AddMessage($"向{ship.shipName}发送{message}");
            }
            catch (Exception ex)
            {
                AddMessage($"向{ship.shipName}发送信息失败：{ex.Message}");
                RemoveShip(ship);
            }
        }

        public void SendToAll(List<Ship> shipList, string str)
        {
            foreach (Ship ship in shipList)
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
                    //do nothing
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
            // 圆的半径（固定为5）
            const float radius = 5f;

            // 计算点到圆心(0,0)的距离平方（避免开根号，性能更高）
            float squaredDistance = x * x + y * y;

            // 如果点在圆内/圆上，直接返回原坐标
            if (squaredDistance <= radius * radius)
            {
                return (x, y);
            }

            // 计算实际距离
            float distance = (float)Math.Sqrt(squaredDistance);

            // 计算缩放比例，将点拉到圆边缘
            float scale = radius / distance;

            // 计算限制后的坐标
            int limitedX = (int)(x * scale);
            int limitedY = (int)(y * scale);

            return (limitedX, limitedY);
        }
    }
}
