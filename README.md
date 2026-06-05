# SeaBattle2026

> 客户端 C# WinForms 海战大联机作业

## 项目结构

```
SeaBattle2026/
├── SeaBattle2026.sln
├── README.md
├── 要求.md
├── Client/                          # 客户端项目
│   ├── Program.cs                   # 入口点
│   ├── Client.csproj                # net8.0-windows
│   ├── Net/
│   │   └── NetworkService.cs        # TCP 发送/接收 + UDP 监听
│   ├── Game/
│   │   ├── Fleet.cs                 # 舰队数据模型
│   │   └── GameState.cs             # 消息解析与状态管理
│   └── UI/
│       ├── Main.cs                  # 主窗口逻辑
│       └── Main.Designer.cs         # 控件布局
└── Server2026/                      # 服务端项目（老师给出）
    ├── Form1.cs + Form1.Designer.cs
    ├── Ship.cs
    └── Program.cs
```

## 模块说明

### Net — `NetworkService`

- **TCP 连接**：连接服务器 `IP:18000`，`SendCommandAsync(message)` 发送命令
- **TCP 接收**：后台线程持续读取 TCP 流的服务器回复（Data/Online），入队 `ReceivedMessages`
- **UDP 监听**：绑定 18001 端口监听 UDP 广播，入队同一队列（兼容备用）
- **生命周期**：`ConnectAsync()` → `SendCommandAsync()` → `ReceivedMessages` → `Disconnect()`

### Game — `Fleet` + `GameState`

| 类 | 职责 |
|----|------|
| `Fleet` | 数据模型：ShipID, ShipName, CaptainName, CrewNames, Px, Py, Fx, Fy, HP, Score |
| `GameState` | 解析 `Online,`（更新在线列表）和 `Data,`（更新位置/HP/得分）；`AllShips` 集合；`LocalShip` 自动识别 |

### UI — `Main`

- **登录面板**：IP / 舰队名称 / 船长名称 / 船员列表 → 连接后发送 `Login`
- **战场绘制**：100×100 网格，绿色圆点（自身）、红色圆点（敌舰）、HP 条、名称标签、黄色炮击标记
- **键盘控制**：WASD/方向键（每秒 Move），空格/F 开火（2s 冷却），鼠标点击开火至指定坐标
- **排行榜**：右侧按击沉数降序排列
- **状态栏**：显示本地舰船位置、HP、击沉数
- **游戏循环**：50ms 间隔轮询消息队列、刷新绘制、更新状态

## 协议

### 客户端发送（TCP）
| 命令 | 格式 | 说明 |
|------|------|------|
| Login | `Login,shipName,CaptainName,crewNames` | 船员逗号分隔 |
| Move | `Move,x,y` | x,y ∈ [-1,1]，每秒一次 |
| Fire | `Fire,x,y` | x,y ∈ [-5,5] 且 x²+y² ≤ 25，2s 冷却 |
| Logout | `Logout` | 断开前发送 |

### 服务器回复（TCP）
| 消息 | 格式 | 说明 |
|------|------|------|
| Online | `Online,shipID,shipName,CaptainName,crewNames,...` | 每 4 字段一组，在线变化时发送 |
| Data | `Data,shipID,px,py,fx,fy,HP,score,...` | 每 7 字段一组，500ms 定时发送 |

## 服务器改动（与老师原版对比）

| 改动 | 原因 |
|------|------|
| `TcpListener(IPAddress.Any, 18000)` | 原版绑定到特定 IP（如 10.16.x.x），客户端用 `127.0.0.1` 无法连接。改为 `0.0.0.0` 接受所有网卡连接 |
| 窗口标题显示 `IP:端口` | 方便用户查看服务器地址 |
| 构造时自动调用 Start | 原来需要手动点击菜单「系统→启动」才能开始监听 |

## 构建与运行

```bash
# 构建
dotnet build Server2026\Server2026.csproj
dotnet build Client\Client.csproj

# 运行（先启动服务端）
start dotnet run --project Server2026\Server2026.csproj
start dotnet run --project Client\Client.csproj
```
