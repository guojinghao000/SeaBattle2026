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
└── Server2026/                      # 服务端项目
    ├── Form1.cs + Form1.Designer.cs
    ├── Ship.cs
    └── Program.cs
```

## 模块说明

### Net — `NetworkService`

- **TCP 连接**：连接服务器 `IP:18000`，`SendCommandAsync(message)` 发送命令
- **TCP 接收**：后台线程持续读取 TCP 流的服务器回复（Data/Online），入队 `ReceivedMessages`
- **UDP 监听**（非致命）：绑定 18001 端口尝试监听 UDP 广播，失败不阻塞连接
- **生命周期**：`ConnectAsync()` → `SendCommandAsync()` → `ReceivedMessages` → `Disconnect()`

### Game — `Fleet` + `GameState`

| 类 | 职责 |
|----|------|
| `Fleet` | 数据模型：ShipID, ShipName, CaptainName, CrewNames, Px, Py, Fx, Fy, HP, Score |
| `GameState` | 解析 `Online,`（更新在线列表）和 `Data,`（更新位置/HP/得分）；`AllShips` 集合；`LocalShip` 自动识别（通过设置 `LocalShipName/LocalCaptainName`） |

### UI — `Main`

- **登录面板**：IP / 舰队名称 / 船长名称 / 船员列表 → 连接后发送 `Login`
- **战场绘制**：100×100 居中网格，绿色圆点（自身）、红色圆点（敌舰）、HP 条、名称标签、黄色炮击标记、虚线范围圈（半径 10 格）、范围内敌舰橙色高亮
- **键盘控制**：WASD/方向键（每秒 Move），空格/F 自动攻击范围内最近敌人（2s 冷却）
- **排行榜**：右侧按击沉数降序排列
- **状态栏**：显示本地舰船位置、HP、击沉数
- **游戏循环**：100ms 间隔轮询消息队列、刷新绘制、更新状态

## 协议

### 客户端发送（TCP）
| 命令 | 格式 | 说明 |
|------|------|------|
| Login | `Login,shipName,CaptainName,crewNames` | 船员逗号分隔 |
| Move | `Move,x,y` | x,y ∈ [-1,1]，每秒一次 |
| Fire | `Fire,dx,dy` | dx,dy ∈ [-10,10] 且 dx²+dy² ≤ 100，相对舰船的偏移，2s 冷却 |
| Logout | `Logout` | 断开前发送 |

### 服务器回复（TCP）
| 消息 | 格式 | 说明 |
|------|------|------|
| Online | `Online,shipID,shipName,CaptainName,crewNames,...` | 每 4 字段一组，在线变化时发送 |
| Data | `Data,shipID,px,py,fx,fy,HP,score,...` | 每 7 字段一组，500ms 定时发送 |

## 服务端特性

### 机器人靶船
- 服务启动时自动生成 5 个机器人（`靶船-A` ~ `靶船-E`），机器人不移动不开火
- 被击沉后自动重生，攻击者得分+1
- 每 500ms 检查数量，不足 5 时自动补充
- 机器人通过 `Online`/`Data` 消息广播，客户端可见可攻击

### 线程安全
- `shipList` 的所有读写操作使用 `lock(_shipLock)` 保护
- 数据定时器 `UpdateTimer_Elapsed` 与客户端接收线程 `ReceiveData` 通过快照隔离

## 客户端操作

| 按键 | 功能 |
|------|------|
| W / ↑ | 向上移动 |
| S / ↓ | 向下移动 |
| A / ← | 向左移动 |
| D / → | 向右移动 |
| Space / F | 自动攻击距离最近且在射程内的敌人 |

## 服务器改动汇总

| 改动 | 原因 |
|------|------|
| `TcpListener(IPAddress.Any, 18000)` | 原版绑定特定 IP，`127.0.0.1` 无法连接 |
| 窗口标题显示 `IP:端口` | 方便查看服务器地址 |
| 构造时自动调用 Start | 无需手动点菜单启动 |
| `shipList` 加锁 | 多线程并发导致移除失败、集合异常 |
| `Fire` 处理：`fx = px + dx` | 原版存相对偏移，命中检测永远失败 |
| `ReceiveData` 退出时清理资源 | 停止 timer、关闭 Writer/Reader/Client |
| `pictureBox1.Invalidate()` | 删除舰队后画布不刷新，图标残留 |
| 中文乱码修复 | 文件编码问题导致全部中文注释/日志损坏 |
| `FormClosing` 退出进程 | 关闭窗口后进程不退出 |
| 机器人靶船 | 开发测试用固定靶标 |

## 构建与运行

```bash
# 构建
dotnet build Server2026\Server2026.csproj
dotnet build Client\Client.csproj

# 运行（先启动服务端）
start dotnet run --project Server2026\Server2026.csproj
start dotnet run --project Client\Client.csproj
```
