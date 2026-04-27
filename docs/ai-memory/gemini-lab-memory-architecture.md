# Gemini-Lab Memory Architecture

Updated: 2026-04-21

## 架构定位
Gemini-Lab 采用“文档先行、实现后落”的分层 Unity 2D 架构。

当前真正落地的是目录分层、模块职责定义、里程碑计划与 AI 工具链接入；真正的运行时代码与场景资源还没有开始实现。

## 当前架构成熟度
- 已落地：
  - 顶层产品 README
  - `Assets/README.md`
  - `Assets/plan.md`
  - `_Project/` 目录骨架与分模块 README
  - Unity MCP 与 Unity Skills 基础接入
- 未落地：
  - `Core / Modules / UI / Editor` 的实际 C# 实现
  - `Boot.unity`、Apartment 场景、Overlay 场景
  - 运行时 Prefab、SO 配置资产、测试程序集

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

### Core
作用：承载与业务无关的基础设施。

预期包含：
- HFSM 抽象
- EventBus
- CommandDispatcher
- ServiceLocator
- GameBootstrap
- Logger / ObjectPool / MainThreadDispatcher 等通用工具

核心约束：
- 不得出现宠物、家具、旅行等业务名词
- 尽量 POCO 化
- 单元测试覆盖率目标最高

### Modules
作用：承载所有业务领域逻辑，每个模块应有独立 asmdef、Service 门面与 Installer。

当前规划模块：
- `Pet`
- `Furniture`
- `Navigation`
- `Gateway`
- `Travel`
- `Desktop`
- `Persistence`

模块边界原则：
- 模块之间只依赖接口、事件、抽象程序集
- 具体实现尽量 `internal`
- 不允许跨模块直接引用对方内部类型

### UI
作用：承载聊天面板、状态 HUD、家具库存、性格雷达图、旅行相册等纯视图层代码。

UI 约束：
- 不写网络、持久化、FSM 业务逻辑
- 只消费模块事件或 ViewModel 输出
- 不允许反向被业务模块依赖

### Editor
作用：只放编辑器侧工具、Inspector 扩展、验证器、资产作者化工具和 MCP 相关编辑器脚本。

Editor 约束：
- Runtime 代码不得引用 Editor 程序集
- 工具逻辑尽量服务于场景作者化、资产验证和流水线自动化

## 业务模块分工

### Pet
- 管理宠物状态值、性格矩阵、FSM 宿主与状态广播
- 对外暴露宠物查询和指令接口
- 依赖 Navigation 抽象接口，不直接耦合导航实现

### Furniture
- 管理 V-Decor 建造模式、家具库存、摆放合法性、Buff 聚合与羁绊标签
- 放置或拆除家具后通知导航系统重建

### Navigation
- 统一提供 2D 导航与路径请求能力
- 目标是封装 NavMeshPlus 或其他可替换的导航实现

### Gateway
- 封装 OpenClaw Gateway 的 HTTP / WebSocket 通道
- 负责 Prompt 上下文组装、事件回调分发、离线队列与重试

### Travel
- 管理旅行会话全生命周期
- 把网关返回的旅行结果转换为宠物奖励与相册数据

### Desktop
- 管理透明桌面 Overlay 形态、点击穿透与前台应用感知
- 平台相关实现通过 `#if` 隔离

### Persistence
- 统一处理本地存档、快照、迁移、离线队列落盘

## 场景架构
当前文档规划的场景集合为：
- `Boot.unity`
- `Apartment/Apartment_Main.unity`
- `DesktopOverlay/Overlay_Main.unity`
- `Dev/FSM_Sandbox.unity`
- `Dev/Furniture_Sandbox.unity`

但现状是：
- `Assets/_Project/Scenes/` 只有 README，没有真正的 `.unity` 文件

因此，任何基于场景路径的自动化或工具脚本，在真实场景落地前都不能假定这些文件已经存在。

## 资源架构

### Prefabs
预期按领域拆成：
- `Pet/`
- `Furniture/`
- `UI/Panels/`
- `UI/Widgets/`
- `Environment/`
- `FX/`

现状：
- `Assets/_Project/Prefabs/` 只有 README

### ScriptableObjects
预期用于承载：
- 宠物模板与状态规则
- 性格演化规则
- 家具定义
- 交互表
- Gateway 配置
- Travel 配置
- UI 配置

现状：
- `Assets/_Project/ScriptableObjects/` 只有 README

### Art / Audio / Settings
这些目录的规范已被定义，但当前仍是说明阶段。

已知重点：
- 美术资源要与第三方素材严格分离
- 运行期设置放在 `_Project/Settings/`
- 业务数值配置放在 `_Project/ScriptableObjects/`

## 外部依赖与工具链

### OpenClaw Gateway
项目外部 AI 服务核心依赖，负责聊天、工作模式回调、旅行结果生成。

### Unity MCP
已安装 `com.ivanmurzak.unity.mcp` 与粒子系统扩展，说明仓库已经为 AI 辅助编辑器操作做了准备。

### Unity Skills
当前仓库存在两套可见技能目录：
- `.cursor/skills/`
- `.agents/skills/`

这说明项目已经把技能工作流作为长期协作基础设施的一部分。

## 架构硬约束
1. `_Project/` 是自研业务代码和资源的正式根。
2. 模块之间只能通过接口、事件、服务定位器或抽象程序集通信。
3. UI 只能消费业务，不得反向承载业务。
4. SO 资产运行期只读，不保存动态状态。
5. 物理移动要走 `Rigidbody2D`，而不是直接改 `transform.position`。
6. 路径、包依赖、场景结构一旦变化，必须同步更新文档与索引。

## 当前架构缺口
1. 文档中反复提到 `NavMeshPlus` / `com.unity.ai.navigation`，但当前 `manifest.json` 尚未体现。
2. 文档中规划了分模块 asmdef 和测试目录，但仓库里还没有这些文件。
3. 文档里定义了场景、Prefab、SO、运行时代码落点，但仓库实际还停留在骨架阶段。
4. `AGENTS.md` 已补齐，但后续如果工作流、skill 入口或目录适配方式变化，必须继续同步维护。
