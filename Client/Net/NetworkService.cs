using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// deepseek 写的，没有检查
namespace Client.Net
{
    public class NetworkService : IDisposable
    {
        // TCP 相关
        private TcpClient? _tcpClient;
        private NetworkStream? _tcpStream;
        private StreamWriter? _tcpWriter;
        private readonly string _serverIp;
        private readonly int _tcpPort = 18000;

        // UDP 相关
        private UdpClient? _udpClient;
        private readonly int _udpPort = 18001;
        private CancellationTokenSource? _cts;
        private Task? _udpListenTask;

        // 消息队列
        public ConcurrentQueue<string> ReceivedMessages { get; } = new();

        // 连接状态
        public bool IsConnected => _tcpClient?.Connected ?? false;

        public NetworkService(string serverIp = "127.0.0.1")
        {
            _serverIp = serverIp;
        }

        /// <summary>
        /// 连接到服务器（TCP）并启动 UDP 监听
        /// </summary>
        public async Task ConnectAsync()
        {
            try
            {
                // 1. 建立 TCP 连接
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(IPAddress.Parse(_serverIp), _tcpPort);
                _tcpStream = _tcpClient.GetStream();
                _tcpWriter = new StreamWriter(_tcpStream, Encoding.UTF8) { AutoFlush = true };
                Debug.WriteLine($"[TCP] 已连接到服务器 {_serverIp}:{_tcpPort}");

                // 2. 启动 UDP 监听
                _udpClient = new UdpClient();
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.ExclusiveAddressUse = false;  // 允许端口复用（多客户端测试用）
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _udpPort));
                _udpClient.EnableBroadcast = true;
                Debug.WriteLine($"[UDP] 开始监听端口 {_udpPort} 的广播");

                _cts = new CancellationTokenSource();
                _udpListenTask = Task.Run(() => ListenUdp(_cts.Token));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NetworkService] 连接失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 通过 TCP 发送命令到服务器
        /// </summary>
        public async Task SendCommandAsync(string message)
        {
            if (_tcpWriter == null || !IsConnected)
            {
                Debug.WriteLine("[TCP] 未连接，无法发送命令");
                return;
            }

            try
            {
                await _tcpWriter.WriteLineAsync(message);
                await _tcpWriter.FlushAsync();
                Debug.WriteLine($"[TCP] 发送命令: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TCP] 发送失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 后台线程：持续接收 UDP 广播
        /// </summary>
        private void ListenUdp(CancellationToken token)
        {
            if (_udpClient == null) return;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _udpClient.Receive(ref remoteEP);  // 同步阻塞，支持取消

                    string message = Encoding.UTF8.GetString(data);
                    Debug.WriteLine($"[UDP] 收到: {message}");
                    ReceivedMessages.Enqueue(message);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    // 正常关闭
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[UDP] 监听异常: {ex.Message}");
                    if (!token.IsCancellationRequested)
                        Thread.Sleep(100); // 短暂等待后重试
                }
            }
        }

        /// <summary>
        /// 断开所有连接并停止监听
        /// </summary>
        public void Disconnect()
        {
            // 停止 UDP 监听
            _cts?.Cancel();
            _udpClient?.Close();
            _udpListenTask?.Wait(TimeSpan.FromSeconds(2));
            Debug.WriteLine("[UDP] 监听已停止");

            // 关闭 TCP 连接
            _tcpWriter?.Close();
            _tcpStream?.Close();
            _tcpClient?.Close();
            Debug.WriteLine("[TCP] 连接已关闭");
        }

        public void Dispose()
        {
            Disconnect();
            _cts?.Dispose();
            _udpClient?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}