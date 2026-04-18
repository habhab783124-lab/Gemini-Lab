# Gemini-Lab 敏捷开发计划（Milestone Plan）

---

## 阶段总览

| 阶段 | 主题 | 预估周期 | 关键里程碑 |
| :-- | :--- | :-- | :--- |
| **Phase 1** | 基础工程构建与状态机 (FSM) 核心搭建 | 2 – 3 周 | 占位宠物可在 FSM 节点间切换；核心基础设施可被单测覆盖 |
| **Phase 2** | 场景交互系统（V-Decor 家具 + NavMesh 寻路） | 3 – 4 周 | 玩家可摆家具；宠物自主寻路与家具交互 |
| **Phase 3** | OpenClaw 网关接入与大模型通信链路 | 2 – 3 周 | 真实 OpenClaw 聊天/工作闭环端到端打通 |
| **Phase 4** | UI 交互、桌面场景透传与旅行系统 | 3 – 4 周 | 双场景无缝切换；完整用户旅程烟雾测试通过 |

---

## Phase 1 — 基础工程构建与状态机 (FSM) 核心搭建 · 预计 **2–3 周**

### Sprint 级任务拆解

| Sprint | 关键任务 | 产出物 |
| :--- | :--- | :--- |
| S1 | 工程初始化（**Unity 2D 模板 + URP 2D Renderer**、asmdef 划分、Git LFS、ProjectSettings 基线）；`_Project/` 目录骨架；编码规范 & PR 模板 | 可通过 CI 空构建 |
| S1 | 核心基础设施：`EventBus`、`ServiceLocator`、`GameBootstrap`、命令模式骨架 `ICommand` + `CommandDispatcher` | 可被单测覆盖的中台 |
| S2 | HFSM 抽象层：`IState`、`StateMachine<TContext>`、`StateTransition`、`SubStateMachine`；日志可视化调试器 | FSM 核心库 v0.1 |
| S2 | 宠物五大状态节点落地：`IdleState / MovingState / InteractingState / WorkingState / SleepingState` | 占位立方体可自主切换状态 |
| S3 | 养成数据层：`PetStateValueSO`、`PersonalityMatrixSO`、`StatTickService`（精力衰减/心情恢复） | 数据驱动可在 Inspector 调参 |
| S3 | 存档骨架：`SaveSystem`（JSON + AES 加密占位） | 启动 → 运行 → 退出可复位状态 |

### Definition of Done（完成定义）

- [ ] 空场景中宠物能依据状态值在 `Idle → Moving → Sleeping` 间自动切换。
- [ ] FSM 核心类单元测试覆盖率 **≥ 80%**，CI 绿灯。
- [ ] 所有核心类 100% 通过 asmdef 隔离，不存在循环依赖（用 Assembly Definition References 图校验）。
- [ ] 代码规范文档合入仓库，`.editorconfig` 生效。
- [ ] `GameBootstrap` 能在 `Boot.unity` 中按序完成服务注册，无硬编码路径。

---

## Phase 2 — 场景交互系统（V-Decor 家具 + NavMesh 寻路）· 预计 **3–4 周**

### Sprint 级任务拆解

| Sprint | 关键任务 | 产出物 |
| :--- | :--- | :--- |
| S4 | Apartment 2D 场景搭建：Tilemap 结构墙 + CompositeCollider2D、左右房间分区、2D Lights (Global + Point) 基线 | 可游玩的白模 2D 场景 |
| S4 | 2D NavMesh 动态方案：**NavMeshPlus** (`NavMeshSurface2d`) 运行时 Bake + `NavMeshModifier` 障碍物适配 | 家具摆放后实时生成路径 |
| S5 | `IFurniture` + `FurnitureDefinitionSO` + `InteractionAnchor` 架构；环境 Buff 聚合器 `BuffAggregator` | 家具具备数据与锚点 |
| S5 | V-Decor 建造模式：长按 `V` 进入/退出、Grid/Tilemap 吸附、贴墙/地板分类检测、`Physics2D.OverlapBox` 碰撞预检 | 玩家可摆放/拆除/旋转家具 |
| S6 | 自主寻路交互：需求权重评估（饱食/心情/精力）、家具扫描器、`HabitTagService`（羁绊 → 性格偏移） | 空闲态自动寻找沙发/床 |
| S6 | 指令交互链路：`CommandLinkService`，强制打断与唤醒惩罚（睡眠中强制唤醒 → 心情 -30） | 用户命令优先级最高 |
| S7 | 集成测试 + 视觉抛光（占位动画） | 可演示 Demo |

### Definition of Done

- [ ] 玩家可无异常摆放 **≥ 5 种**家具（地板、贴墙 Sprite 混合）。
- [ ] 家具摆放/拆除后 2D NavMesh ≤ **200ms** 内完成增量烘焙（NavMeshPlus `BuildNavMeshAsync`）。
- [ ] 宠物能自主走向高权重家具并完整播放交互循环。
- [ ] 强制唤醒后心情值立即下降、行为日志可追溯（附 traceId）。
- [ ] 家具摆放布局可被 `Persistence` 模块持久化与复原。

---

## Phase 3 — OpenClaw 网关接入与大模型通信链路 · 预计 **2–3 周**

### Sprint 级任务拆解

| Sprint | 关键任务 | 产出物 |
| :--- | :--- | :--- |
| S8 | 通信层封装：`IGatewayClient`（HTTP + WebSocket 双通道）、重试/退避/超时策略、断网降级队列 | 稳定的网关 SDK |
| S8 | Context 组装：`PromptContextBuilder`（UserMessage + State + Personality + FSMState → Payload DTO） | 可被 Gateway 消费的请求体 |
| S9 | 聊天模式：文本响应 → 气泡 UI，不打断当前 FSM 状态；流式（SSE）可选 | 支持"边看书边回答" |
| S9 | 工作模式：指令驱动进入 `WorkingState`，锁定工位，等待"任务完成"事件回调 | 工作闭环跑通 |
| S10 | 动态 System Prompt：性格值 → Prompt 模板注入；Mock Gateway 压测 | 100 条并发无异常 |
| S10 | 事件响应分发：`GatewayEventRouter`（文本/事件/旅行/错误分流） | 统一回调分发中枢 |

### Definition of Done

- [ ] 本地 Mock Server + 真实 OpenClaw 双环境 E2E 均通过。
- [ ] 聊天往返延迟 **P95 ≤ 2s**；断网 30s 内能恢复队列重放。
- [ ] 工作任务从下发到完成回调完整打印 `traceId`，日志可复盘。
- [ ] 性格值变化能实时反映在 AI 回复语气上（人工评审 **≥ 3 组**对照）。
- [ ] 敏感字段（Token、用户 ID）在所有日志输出中打码。

---

## Phase 4 — UI 交互、桌面场景透传与旅行系统 · 预计 **3–4 周**

### Sprint 级任务拆解

| Sprint | 关键任务 | 产出物 |
| :--- | :--- | :--- |
| S11 | UI 框架（UGUI + MVVM）：聊天面板、状态面板、家具库存、性格雷达图 | UI Toolkit v1 |
| S11 | 桌面 Overlay 模式：透明无边框窗口（Win32 `SetWindowLong` / `DwmExtendFrameIntoClientArea`）、点击穿透、Always On Top | 宠物可驻留桌面边缘 |
| S12 | 系统级感知：`ForegroundWindowProbe`（轮询/钩子）、规则引擎 `AppContextRule`（VS Code → 主动提议工作模式） | 主动发起对话能力 |
| S12 | 旅行系统：`TravelCommand` → 离场动画 → 异步拉取旅行小记/照片 → 回归触发奖励 | 旅行闭环 |
| S13 | 照片画廊 UI、旅行日志时间线、勇敢/见识性格加成生效 | 可展示旅行成果 |
| S13 | 双场景无缝切换（Apartment ↔ Desktop Overlay）、状态持久化迁移 | 模式切换 ≤ 1s |
| S14 | 性能优化（GC Alloc、Draw Call、内存）、打包、代码签名、自动化冒烟 | Release Candidate |

### Definition of Done

- [ ] Desktop Overlay 模式在 **1080p / 4K / 多屏**下窗口定位、点击穿透均正常。
- [ ] 完整用户旅程（登录 → 摆家具 → 聊天 → 工作 → 旅行 → 回归）烟雾测试通过。
- [ ] Release 包体运行帧率 **≥ 60fps**（公寓 2D）/ **≥ 30fps**（桌面 Overlay）；内存 **≤ 500MB**（2D 项目基线更低）。
- [ ] 所有 Phase 1–4 文档（开发手册 + 运维手册）归档完毕。
- [ ] 通过代码签名后的安装包在 Windows 10/11 上无 SmartScreen 拦截。

---

## 交叉风险与缓解措施

| 风险 | 影响阶段 | 缓解策略 |
| :--- | :--- | :--- |
| 2D NavMesh 动态烘焙卡顿 | Phase 2 | NavMeshPlus 异步增量 Bake；家具拖动预览阶段仅用临时 Obstacle，不触发 Bake |
| OpenClaw 接口变更 | Phase 3 | Gateway 模块走适配层 `IGatewayClient`；DTO 版本化 |
| 透明窗口跨屏坐标异常 | Phase 4 | 多分辨率/多 DPI 自动化测试脚本 |
| 性格值失衡导致 AI 语气抽风 | Phase 3 / 4 | Prompt 模板回归测试集 + 人工评审 |
| 旅行结果被本地伪造 | Phase 4 | 结果仅由 Gateway 回调触发，客户端鉴权校验 |

---

## 验收总览

项目 Release 前必须满足：

1. 所有阶段 DoD 全部勾选。
2. 单元测试覆盖率：`Core` ≥ 80%、`Modules` ≥ 60%。
3. 端到端自动化冒烟测试 100% 通过。
4. 性能指标达标（帧率、内存、启动时长 ≤ 3s）。
5. 所有公开 API 有 XML Doc；`_Project/` 全部子目录 README 与实际实现一致。
