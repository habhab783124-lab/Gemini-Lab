# Gemini-Lab Memory Architecture

Updated: 2026-04-27

## 架构定位
Gemini-Lab 当前采用“文档先行 + 原型实现逐步落地”的分层 Unity 2D 架构。

和 2026-04-21 的纯骨架阶段不同，当前仓库已经存在真实运行时代码、场景、asmdef 与测试程序集；但 Prefab / SO 资产与若干底层实现仍未完全收口。

## 当前架构成熟度

### 已落地
- `Core / Modules / Editor` 的真实 C# 实现
- `Boot.unity`、`Apartment/Apartment_Main.unity`、`Desktop/Desktop_Overlay.unity`
- 分模块 asmdef 与 `EditMode` / `PlayMode` 测试程序集
- 宠物、家具、网关、旅行、存档、UI、桌面 Overlay 的第一轮原型代码
- Unity MCP 与 Unity Skills 基础接入

### 仍未落地或未完成
- 真实 Prefab 资产
- 真实 ScriptableObject 配置资产
- 完整 2D NavMesh 实现
- 原生桌面透明窗口 / 点击穿透实现
- 与 README 目标态完全一致的场景 / 资源作者化体系

## 分层模型

```text
Editor
UI
Modules
Core
```

依赖方向固定为：

```text
Editor -> UI / Modules / Core
UI -> Modules / Core
Modules -> Core
Core -> 无业务依赖
```

## Core
作用：承载与业务无关的基础设施。

当前已经真实存在的代表实现：
- `GameBootstrap`
- `ServiceLocator`
- `EventBus`
- `CommandDispatcher`
- `IState<TContext>` / `StateMachine<TContext>` / `StateTransition<TContext>` / `SubStateMachine<TContext>`

核心约束：
- 不得出现宠物、家具、旅行等业务名词
- 尽量 POCO 化
- 单元测试覆盖率目标最高

## Modules
作用：承载所有业务领域逻辑。

当前真实模块实现包括：
- `Pet`
- `Furniture`
- `Navigation`
- `Gateway`
- `Travel`
- `Persistence`
- `UI`
- `DesktopOverlay`

模块边界原则：
- 模块之间只依赖接口、事件、抽象程序集
- 具体实现尽量收敛在本模块内部
- 不允许跨模块直接引用对方内部类型

### Pet
- 已落地：`PetController`、`PetContext`、`PetRuntimeData`、状态机状态类、`StatTickService`、指令链路
- 作用：管理宠物状态值、FSM 宿主、工作/睡眠/交互状态切换

### Furniture
- 已落地：`FurnitureService`、`FurnitureDefinitionSO` 类型、摆放校验、布局快照、Buff 聚合、羁绊标签
- 当前特征：缺少真实 `.asset` 配置时会回退到运行时创建默认定义

### Navigation
- 已落地：`INavigationService`、`NavigationService`、`NavMesh2DRebaker`
- 当前特征：接口与事件链路已经有了，但路径请求与重建更接近占位实现，不等于真实导航已经完成

### Gateway
- 已落地：`GatewayClient`、`GatewayMessageService`、`PromptContextBuilder`、`GatewayEventRouter`、`GatewayBootstrap`
- 当前特征：具备 Mock / 重试 / 离线队列等原型能力；缺少配置资产时会回退到运行时 Mock 配置

### Travel
- 已落地：`TravelService`、`TravelRewardApplier`、旅行时间线与事件桥接
- 当前特征：旅行闭环代码已存在，但相册资产与更完整展示链路仍待补齐

### Desktop / DesktopOverlay
- 设计说明当前仍在 `Assets/_Project/Scripts/Modules/Desktop/README.md`
- 真实运行时代码当前在 `Assets/_Project/Scripts/Modules/DesktopOverlay/`
- 已落地：`DesktopOverlayManager`、`ForegroundWindowProbe`、`AppContextRuleEngine`、`WindowModeAdapter`
- 当前特征：模式切换、前台应用探测与规则引擎已存在；原生透明窗口能力仍是占位层

### Persistence
- 已落地：`SaveSystem`、`PersistenceBootstrap`
- 当前特征：已有 JSON 存档、slot 校验、加密策略占位、并发锁控制

## UI
作用：承载聊天面板、状态 HUD、家具库存、性格雷达图等纯视图层代码。

当前真实状态：
- `Assets/_Project/Scripts/Modules/UI/` 下已有 `ChatPanelController`、`StatusPanelController`、`FurnitureInventoryPanelController`、`PersonalityRadarView`
- `ViewModels/` 下已有 `ChatViewModel` 与 `PetStatusViewModel`
- `Assets/_Project/Scripts/UI/` 目前仍主要承担目录说明角色

UI 约束：
- 不写网络、持久化、FSM 业务逻辑
- 只消费模块事件或 ViewModel 输出
- 不允许反向被业务模块依赖

## Editor
作用：只放编辑器侧工具、Inspector 扩展、验证器、资产作者化工具和 MCP 相关编辑器脚本。

当前真实状态：
- 已存在 `PetMoveAnimationSetupEditor.cs`

Editor 约束：
- Runtime 代码不得引用 Editor 程序集
- 工具逻辑尽量服务于场景作者化、资产验证和流水线自动化

## 场景架构
当前真实存在的场景为：
- `Boot.unity`
- `Apartment/Apartment_Main.unity`
- `Desktop/Desktop_Overlay.unity`

当前判断：
- `Boot.unity` 已经是实际启动入口
- `Apartment_Main.unity` 已包含宠物、家具、UI、导航根与基础环境结构
- `Desktop_Overlay.unity` 已存在，但还更接近原型桌面场景
- `Dev/` 沙盒场景仍未落地

## 资源架构

### Prefabs
预期仍然按领域拆成：
- `Pet/`
- `Furniture/`
- `UI/Panels/`
- `UI/Widgets/`
- `Environment/`
- `FX/`

现状：
- `Assets/_Project/Prefabs/` 目前仍只有 README

### ScriptableObjects
预期仍用于承载：
- 宠物模板与状态规则
- 性格演化规则
- 家具定义
- Gateway 配置
- Travel 配置
- UI 配置

现状：
- `Assets/_Project/ScriptableObjects/` 目前仍只有 README
- 但对应 SO 类型代码已经开始落在 `Scripts/Modules/**`

### Art / Audio / Settings / Animations
- `Art/` 与 `Animations/` 已经开始落真实资源
- `Audio/` 与 `Settings/` 当前仍主要是目录规范

## 外部依赖与工具链

### OpenClaw Gateway
项目外部 AI 服务核心依赖，负责聊天、工作模式回调、旅行结果生成。

### 导航与 Unity 包
当前 `Packages/manifest.json` 已包含：
- `com.unity.ai.navigation`
- `com.ivanmurzak.unity.mcp`
- `com.ivanmurzak.unity.mcp.particlesystem`
- `com.ivanmurzak.unity.mcp.animation`

### Unity Skills
当前仓库存在两套可见技能目录：
- `.cursor/skills/`
- `.agents/skills/`

它们当前仍为镜像关系，说明 skill 工作流已经成为长期协作基础设施的一部分。

## 架构硬约束
1. `_Project/` 是自研业务代码和资源的正式根。
2. 模块之间只能通过接口、事件、服务定位器或抽象程序集通信。
3. UI 只能消费业务，不得反向承载业务。
4. SO 资产运行期只读，不保存动态状态。
5. 物理移动要走 `Rigidbody2D` 或明确的运行时位置应用逻辑，不要让状态机和表现层各自隐式改位置。
6. 路径、包依赖、场景结构一旦变化，必须同步更新文档与索引。

## 当前架构缺口
1. Prefab 与 ScriptableObject 资产仍未真正作者化落地。
2. 导航模块的接口和事件已落地，但真实 2D NavMesh 能力尚未收口。
3. 桌面 Overlay 的运行时代码已在，但原生透明窗口、点击穿透与多屏校准尚未落地。
4. `Desktop` 设计文档与 `DesktopOverlay` 运行时代码目录的命名仍未统一。
5. README 与部分旧文档曾长期停留在“骨架阶段”口径，后续仍要继续防止文档落后于实现。
