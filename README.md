# SeaBattle2026

>在README.md文件中记录开发过程中的信息。

## 分工

客户端按模块职责拆分为三部分，每人独立负责一个模块，通过约定接口协作。

### 网络通信模块（人员 A）
负责客户端所有网络收发，包括 TCP 命令发送和 UDP 状态接收。

**需创建的类：**

| 类名 | 作用 |
|------|------|
| `NetworkService` | 核心网络管理器：<br>1. 通过 `TcpClient` 连接服务器（IP:18000），提供 `SendCommand(string)` 方法发送 Login/Move/Fire/Logout 消息<br>2. 后台线程持续读取 TCP 流的服务器应答（备用）<br>3. 创建 `UdpClient` 绑定 18001 端口，监听服务器 UDP 广播，将收到的 Online/Data 消息放入 `ConcurrentQueue<string>` |
| `MessageQueue` | 线程安全队列，供 UI 主循环读取 UDP 广播消息 |
| `NetworkConfig` | 静态常量类，存放服务器 IP、TCP 端口(18000)、UDP 监听端口(18001) |

**对外接口：**
- `void Connect(string serverIp)`：建立 TCP 连接并启动 UDP 监听
- `void SendCommand(string message)`：通过 TCP 发送命令
- `ConcurrentQueue<string> ReceivedMessages`：UI 主循环从这里取服务器广播消息
- `void Disconnect()`：关闭连接

---

### 游戏逻辑模块（人员 B）
维护本地舰队状态（由服务器广播同步），不自行计算规则，只做数据解析与提供。

**需创建的类：**

| 类名 | 作用 |
|------|------|
| `Ship` | 舰队数据模型：ShipID、ShipName、CaptainName、CrewNames、Px、Py、Fx、Fy、HP、Score（全部来自服务器 Data 消息） |
| `GameState` | 逻辑状态管理器：<br>1. 解析服务器消息：`Online`（更新在线玩家列表）、`Data`（更新各舰位置、HP、炮击位置、击沉数）<br>2. 提供 `Dictionary<string, Ship> AllShips` 给 UI 层读取<br>3. 维护本地玩家标识（自己的 ShipID）以便 UI 突出显示 |
| `GameConstants`（可选） | 存放地图尺寸、坐标范围等常量 |

**对外接口：**
- `void ProcessServerMessage(string message)`：处理一条 UDP 广播消息
- `IReadOnlyDictionary<string, Ship> AllShips`：UI 读取绘制
- `Ship LocalShip`：当前客户端对应的舰队（用于显示自身状态）

---

### 用户界面与主循环（人员 C）
负责画面渲染、玩家输入、游戏主循环集成。

**需创建的类：**

| 类名 | 作用 |
|------|------|
| `MainForm`（WinForms） | 程序入口与主窗口：<br>1. 登录面板（输入船名、船长、船员）<br>2. 创建并初始化 `NetworkService` 和 `GameState`<br>3. 启动主循环 `GameTimer`（间隔 50ms） |
| `BattlefieldRenderer` | 自定义绘制控件，绘制 100×100 地图、所有舰队标记、名称、HP 条、炮击动画等 |
| `InputController` | 处理键盘方向键/WASD（转换为移动增量 dx,dy）和开火键（F 或鼠标点击，计算相对目标偏移）<br>调用 `NetworkService.SendCommand` 发送 Move/Fire 命令 |
| `GameLoopManager`（可并入 MainForm） | `Timer` 每帧执行：<br>1. 从 `NetworkService.ReceivedMessages` 取出所有消息，调用 `GameState.ProcessServerMessage`<br>2. 刷新 `BattlefieldRenderer`<br>3. 检查移动/开火冷却（本地辅助判断，实际以服务器为准） |

---

### 接口与消息约定

#### 客户端发送（TCP）
- `Login,shipName,CaptainName,crewNames`
- `Move,x,y`（x,y ∈ [-1,1]）
- `Fire,x,y`（x,y ∈ [-5,5], x²+y² ≤ 25）
- `Logout`

#### 服务器广播（UDP，客户端接收）
- `Online,shipID,shipName,CaptainName,crewNames,shipID2,...`（每次在线列表变化时）
- `Data,shipID,px,py,fx,fy,HP,score,shipID2,px2,...`（每 500ms 定时）

#### 模块通信方式
- **UI → 网络**：调用 `NetworkService.SendCommand`
- **网络 → 逻辑**：UI 主循环从 `NetworkService.ReceivedMessages` 取消息，传给 `GameState.ProcessServerMessage`
- **逻辑 → UI**：UI 直接读取 `GameState.AllShips` 和 `GameState.LocalShip`

---

### 开发建议
1. **人员 A** 先实现 TCP 连接和 UDP 监听框架，提供可用的发送和接收队列，可用 `Console.WriteLine` 测试消息收发。
2. **人员 B** 根据服务器 Data/Online 格式实现解析，构造 Ship 字典，可使用 A 的模拟消息进行单元测试。
3. **人员 C** 先用假数据完成 UI 绘制和输入，然后接入真实的网络和逻辑模块。
4. 三人约定好服务器 IP（演示时改为实际 IP）和端口，统一使用 `NetworkConfig` 类。