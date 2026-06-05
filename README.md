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

### 用户界面与主循环（人员 C）
- 绘制 100×100 坐标地图，显示所有舰队名称、HP、击沉数、实时位置
- 处理用户输入：移动（方向键/WASD）、开火（F 键或鼠标）、登录/登出
- 实现游戏主循环（`Timer` 约 50ms 间隔）：
  1. 从网络队列取出所有消息，调用 `ProcessReceivedMessage`
  2. 更新 UI 绘制
- 集成网络模块和逻辑模块，按钮操作时调用逻辑接口并发送消息

### 接口约定
- 网络层通过 `MessageReceived` 事件将原始字符串传给逻辑层
- 逻辑层方法返回的消息字符串，由 UI 层调用 `INetworkService.Send` 广播
- UI 层只通过 `IGameLogic` 读取状态和触发操作，不直接修改数据
- 网络接收线程与 UI 线程分离，消息队列保证线程安全