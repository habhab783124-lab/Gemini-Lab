# Gemini-Lab (Unity Client)
Gemini-Lab 是一款基于 **Unity 2022 LTS / 2D 模板 (URP 2D Renderer)** 开发的新一代虚拟宠物 AI 交互客户端。项目通过深度集成 OpenClaw Gateway，结合分层有限状态机 (HFSM) 与性格驱动大模型，实现了一个具备独立性格、多场景无缝切换及高自由度交互的沉浸式宠物陪伴生态。

---

## 🪐 核心场景 (Scene Modes)

项目提供两种核心运行环境，满足用户在娱乐与办公不同场景下的陪伴需求：

### 1. 公寓场景 (Apartment Scene)
公寓是一个 **2D 俯视 / 正交视角**、被结构墙（基于 Tilemap + CompositeCollider2D）划分为左右两个房间（如起居室与工作室）的矩形空间。
* **V-Decor 建造系统**：玩家长按 **`V`** 键进入家具摆放模式。系统基于 **Grid / Tilemap** 的网格吸附（Grid Snapping），支持玩家根据库存将家具摆放至地板或墙体（贴墙 Sprite）。
* **环境增益 (Environmental Buffs)**：家具不仅仅是装饰，还能提供数值加成。例如“高级办公桌”可缩短工作时长，“舒适地毯”可提升空闲态的心情恢复速率。

### 2. 桌面场景 (Desktop Overlay)
* **轻量化后台**：客户端以透明无边框窗口形式在后台运行。
* **跨屏陪伴**：宠物作为前台 Top-Level UI 存在于屏幕边缘。结合**系统级感知**，未来可读取用户的应用状态（如打开 VS Code 时，宠物主动提议进入工作模式）。

---

## 🧠 核心业务系统 (Core Systems)

### AI 智能对话系统 (OpenClaw Gateway)
屏幕右下角集成聊天 UI，支持向指定宠物发送消息。系统分为两种深度交互模式：
* **聊天模式 (Chat Mode)**：纯文本交互。消息经由 OpenClaw 返回后，作为聊天气泡显示，宠物保持当前状态（如正在看书，气泡会显示“边看书边回答”）。
* **工作模式 (Work Mode)**：指令驱动。宠物进入工作状态，通过 **2D NavMesh (NavMeshPlus)** 寻路至绑定的办公桌。直到 OpenClaw 返回“任务完成”响应，宠物才会解除工作状态。

### 养成与性格演化矩阵
宠物的每一次行动都在重塑其内在数值，这些数值会作为 **System Prompt** 实时发送给 AI 网关，决定其后续的对话语气。
| 维度 | 包含数值 | 机制描述 |
| :--- | :--- | :--- |
| **状态值 (State)** | 心情、饱食度、精力、经验值 | 分阶段成长，决定宠物的活跃度与模型表现。精力耗尽会强制进入睡眠。 |
| **性格值 (Personality)** | 正直、冷静、邪恶、善良、勇敢、懦弱、害羞 | 动态演化。如经常工作增加“冷静”，被强制唤醒增加“邪恶/懦弱/害羞”。 |

### 旅行系统 (Travel System)
* 玩家发送“去旅游”指令后，客户端将上下文发送给 OpenClaw，宠物暂时离开场景。
* OpenClaw 异步生成**旅行小记**与**旅行照片**。
* 宠物回归时携带这些素材，提升心情与“勇敢/见识”等性格数值。

---

## 🛋 交互与状态机设计 (Interaction & FSM)

项目采用**分层有限状态机 (HFSM)** 驱动宠物行为。家具不仅仅是碰撞体，更是状态树中的交互节点。

### 交互逻辑
* **自主寻路交互 (Idle Interaction)**：宠物处于“空闲状态”时，会根据自身需求权重，自动扫描场景内“空闲态可用家具”并产生互动（如走向沙发休息）。**家具羁绊系统**会记录交互频率，频繁交互会生成“习惯标签”并影响性格偏移。
* **指令交互 (Command-Link)**：用户强制命令宠物与特定家具交互（如“去睡觉”）。此操作优先级最高，立即打断空闲状态，但若在睡眠状态（躺在床上）被用户强制唤醒，会导致心情值大幅降低。

### 状态机 (FSM) 核心节点
| 状态节点 | 触发机制 | 行为表现 |
| :--- | :--- | :--- |
| **Idle (空闲)** | 默认状态 | 随机游走、触发原地动画或寻找可用家具。 |
| **Moving (移动)** | 触发交互或更换目标 | 调用 2D NavMesh (NavMeshPlus) / A* 寻路至家具交互锚点。 |
| **Interacting (交互中)** | 抵达交互锚点 | 播放特定家具动画，动态消耗/恢复状态值，该状态信息同步给网关。 |
| **Working (工作中)** | 接收到工作模式指令 | 锁定在办公桌，性格值中的“冷静”和“经验值”稳步提升。 |
| **Sleeping (睡眠)** | 精力耗尽或指令触发 | 拒绝常规聊天，强制唤醒触发惩罚机制。 |

---

## 🛠 技术实现细节 (Technical Architecture)

### 1. 技术栈
* **Engine**: Unity 2022.3 LTS · **2D 模板**
* **Rendering**: URP 2D Renderer + 2D Lights · Orthographic Camera
* **Tiling / Scene**: Tilemap + Rule Tile + CompositeCollider2D
* **Physics**: Physics2D（Rigidbody2D / BoxCollider2D / 触发器）
* **Animation**: Unity 2D Animation 包（骨骼绑定 / PSD Importer）+ Animator Controller
* **Scripting**: C# (基于事件驱动的命令模式 Event-Driven Command Pattern)
* **Pathfinding**: **NavMeshPlus (2D NavMesh 端口)** 动态障碍物适配（兼容 V-Decor 家具实时摆放）
* **Networking**: RESTful / WebSocket 连接至 OpenClaw Gateway

### 2. 通信全流程 (Gateway Workflow)
1. **Context 组装**：用户发起聊天时，客户端打包 `User Message` + `State Values` + `Personality Values` + `Current FSM State`。
2. **API 转发**：将打包数据发送给 OpenClaw Gateway。
3. **响应回调**：
   * **文本响应**：渲染气泡 UI，不打断交互。
   * **事件响应**：触发特定逻辑（如完成工作切换 FSM 到 Idle，完成旅行渲染照片画廊）。

### 3. AI 辅助开发工具链 (DevTools) 🌟
项目在开发期集成了 [IvanMurzak / Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)（Model Context Protocol）。开发者可使用外部大模型 Agent 直接连接 Unity 编辑器进行辅助开发：
* **结构设计**：Agent 可根据指令自动生成规范化的目录结构和样板代码。
* **编辑器操作**：通过 MCP 协议，Agent 能够读取场景 Hierarchy，自动完成预制体生成、组件挂载、Sprite / 2D 材质替换等繁琐的编辑器配置任务。
* **规范审查**：实时读取 C# 脚本上下文，辅助校验状态机逻辑与 API 接入的规范性。

---