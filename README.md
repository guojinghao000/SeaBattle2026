# SeaBattle2026

>在README.md文件中记录开发过程中的信息。

## 分工

客户端按模块职责拆分为三部分，每人独立负责一个模块，通过约定接口协作。

### 网络通信模块（人员 A）
- 使用 `UdpClient` 绑定 `18001` 端口，开启后台线程持续接收广播消息
- 发送 UDP 广播至 `255.255.255.255:18001`
- 维护线程安全的消息接收队列（`ConcurrentQueue<string>`）
- 封装消息的序列化与反序列化（格式：`Login`、`Move`、`Fire`、`Logout`）
- 对外提供 `INetworkService` 接口，包含 `Start()`、`Send(string)` 和 `MessageReceived` 事件

**需创建的类：**
| 类名 | 作用 |
|------|------|
| `NetworkService` | 核心类，实现 `INetworkService`，负责 UDP 广播收发、后台监听线程、消息队列维护、触发 `MessageReceived` 事件 |
| `MessageParser`（可选） | 静态辅助类，提供消息的序列化与反序列化方法，如 `BuildLogin(...)`、`ParseType(string)` 等 |

---

### 游戏逻辑模块（人员 B）
- 定义 `Fleet` 数据结构（船名、船长、船员、HP、位置、击沉数、移动/攻击冷却）
- 实现游戏规则：
  - 移动冷却 1 秒，每次坐标变化范围为 [-1,1]，边界限制 [0,100]
  - 攻击冷却 2 秒，射程 5（偏移满足 x²+y²≤25）
  - HP 为 3，命中减 1，归零后目标随机重生，攻击者击沉数+1、HP 回满
- 管理本地舰队与全局舰队状态（`Dictionary<string, Fleet>`）
- 提供 `IGameLogic` 接口：
  - `DoLogin` / `DoMove` / `DoFire` / `DoLogout` 返回待发送消息
  - `ProcessReceivedMessage(string)` 处理收到的网络消息
- 攻击判定由发起方本地计算，通过广播同步结果

**需创建的类：**
| 类名 | 作用 |
|------|------|
| `Fleet` | 舰队数据模型，包含船名、船长、船员、HP、位置、击沉数、移动/开火冷却时间等属性 |
| `IGameLogic` | 逻辑层接口，定义 UI 可调用的操作及状态获取方法 |
| `GameLogic` | 核心逻辑类，实现 `IGameLogic`，管理所有舰队字典，执行移动/攻击/重生规则，生成和处理消息字符串 |
| `GameConstants`（可选） | 静态常量类，集中定义地图尺寸、HP上限、冷却时间、射程等不变数值 |

---

### 用户界面与主循环（人员 C）
- 绘制 100×100 坐标地图，显示所有舰队名称、HP、击沉数、实时位置
- 处理用户输入：移动（方向键/WASD）、开火（F 键或鼠标）、登录/登出
- 实现游戏主循环（`Timer` 约 50ms 间隔）：
  1. 从网络队列取出所有消息，调用 `ProcessReceivedMessage`
  2. 更新 UI 绘制
- 集成网络模块和逻辑模块，按钮操作时调用逻辑接口并发送消息

**需创建的类：**
| 类名 | 作用 |
|------|------|
| `MainForm`（WinForms） 或 `Program`（控制台） | 程序入口，负责初始化网络服务、逻辑核心，绑定事件，启动 UI 与主循环 |
| `GameRenderer` 或 `GamePanel` | 自定义绘制组件，负责绘制地图网格、各舰队位置、HP、击沉数等信息 |
| `InputHandler` | 处理键盘/鼠标输入，将其转换为 `DoMove(dx,dy)` 或 `DoFire(dx,dy)` 调用 |
| `GameLoopManager`（可选） | 封装 `Timer`，每帧拉取网络消息、刷新渲染，可与窗体合并 |

---

### 接口约定
- 网络层通过 `MessageReceived` 事件将原始字符串传给逻辑层
- 逻辑层方法返回的消息字符串，由 UI 层调用 `INetworkService.Send` 广播
- UI 层只通过 `IGameLogic` 读取状态和触发操作，不直接修改数据
- 网络接收线程与 UI 线程分离，消息队列保证线程安全

### 消息格式（扩展后，所有命令携带 CaptainName）
- `Login,ShipName,CaptainName,Crew1,Crew2,...`
- `Move,CaptainName,dx,dy`
- `Fire,CaptainName,dx,dy,targetCaptain`
- `Logout,CaptainName`