using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client.Net;

public class NetworkService : IDisposable
{
    private TcpClient? _tcpClient;
    private NetworkStream? _tcpStream;
    private StreamReader? _tcpReader;
    private StreamWriter? _tcpWriter;
    private readonly string _serverIp;
    private readonly int _tcpPort = 18000;

    private UdpClient? _udpClient;
    private readonly int _udpPort = 18001;
    private CancellationTokenSource? _cts;
    private Task? _udpListenTask;
    private Task? _tcpReadTask;

    public ConcurrentQueue<string> ReceivedMessages { get; } = new();

    public event Action? ConnectionLost;
    public bool IsConnected => _tcpClient?.Connected ?? false;

    public NetworkService(string serverIp = "127.0.0.1")
    {
        _serverIp = serverIp;
    }

    public async Task ConnectAsync()
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(IPAddress.Parse(_serverIp), _tcpPort);
            _tcpStream = _tcpClient.GetStream();
            _tcpWriter = new StreamWriter(_tcpStream, Encoding.UTF8) { AutoFlush = true };
            _tcpReader = new StreamReader(_tcpStream, Encoding.UTF8);

            _cts = new CancellationTokenSource();
            _tcpReadTask = Task.Run(() => ListenTcp(_cts.Token));

            // UDP listener is non-critical; server sends via TCP
            StartUdpListener();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NetworkService] 连接失败: {ex.Message}");
            throw;
        }
    }

    private void StartUdpListener()
    {
        try
        {
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.ExclusiveAddressUse = false;
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _udpPort));
            _udpClient.EnableBroadcast = true;
            _udpListenTask = Task.Run(() => ListenUdp(_cts!.Token));
        }
        catch
        {
            // UDP bind failure is non-fatal
        }
    }

    public async Task SendCommandAsync(string message)
    {
        if (_tcpWriter == null || !IsConnected)
            return;

        try
        {
            await _tcpWriter.WriteLineAsync(message);
            await _tcpWriter.FlushAsync();
        }
        catch
        {
            // ignored
        }
    }

    private void ListenTcp(CancellationToken token)
    {
        if (_tcpReader == null) return;

        while (!token.IsCancellationRequested)
        {
            try
            {
                string? line = _tcpReader.ReadLine();
                if (line == null)
                {
                    ConnectionLost?.Invoke();
                    break;
                }
                ReceivedMessages.Enqueue(line);
            }
            catch when (!token.IsCancellationRequested)
            {
                Thread.Sleep(100);
            }
        }
    }

    private void ListenUdp(CancellationToken token)
    {
        if (_udpClient == null) return;

        while (!token.IsCancellationRequested)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);
                ReceivedMessages.Enqueue(message);
            }
            catch (SocketException) when (!token.IsCancellationRequested)
            {
                Thread.Sleep(100);
            }
        }
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _udpClient?.Close();
        _tcpWriter?.Close();
        _tcpReader?.Close();
        _tcpStream?.Close();
        _tcpClient?.Close();

        while (ReceivedMessages.TryDequeue(out _)) { }
    }

    public void Dispose()
    {
        Disconnect();
        _cts?.Dispose();
        _udpClient?.Dispose();
        _tcpClient?.Dispose();
    }
}