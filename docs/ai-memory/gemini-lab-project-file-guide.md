# Gemini-Lab Project File Guide

## 2026-09-08 苹果掉落核心恢复到 eb1c300

- `Assets/_Project/Scripts/Modules/Apple/AppleTreeDropController.cs`、`AppleDropSlot.cs`、`AppleTreeFeedback.cs` 以 `eb1c300b991d92ec6336afaf5441d0b0ca81544c` 为行为基准。
- `AppleDropSlot` 的点击入口仍由 WorldMap 路由通过自身 Collider2D 调用，不恢复旧 `OnMouseDown`；`AppleRuntimeBootstrap.cs` 保留当前直启初始化。

## 2026-09-08 苹果掉落逻辑恢复基准

- `Assets/_Project/Scripts/Modules/Apple/AppleService.cs` 当前以提交 `cc54b1a9196ff1d16c341ecdfffdd0f27e1c37f1` 为逻辑基准。
- `AppleTreeDropController.cs`、`AppleDropSlot.cs`、`AppleTreeFeedback.cs` 继续使用 Scene 中作者化的掉落槽位和反馈文字；WorldMap 路由适配保留在 `AppleDropSlot`，不恢复旧 `OnMouseDown`。
- 不要在运行时或作者化脚本中把 `TMP_Text.fontMaterial` 设为 null；空值会触发 TMP 3.0.7 的空引用异常。字体和共享材质由 Scene / Inspector 序列化引用提供。

## 2026-09-08 Boot 启动顺序

- `Assets/_Project/Scenes/Boot.unity` 的 `BootstrapRoot` 是核心服务宿主，保留唯一的 `TarotRuntimeBootstrap`，并保存 TarotDeck 与 LLM 配置引用。
- `Assets/_Project/Scripts/Modules/Apple/AppleRuntimeBootstrap.cs` 在核心服务存在后于 `Start` 注册 `IAppleService`；`Assets/_Project/Scripts/Modules/Tarot/TarotRuntimeBootstrap.cs` 在 `Start` 初始化 Tarot，避免 `Awake` 时序竞争。
- 本次只修复 Boot 启动初始化顺序，不触碰 WorldMap 场景、苹果树收集逻辑或桌宠桥交互。

## 2026-09-08 WorldMap 苹果树成熟状态初始化

- `Assets/_Project/Scripts/Modules/Apple/AppleRuntimeBootstrap.cs` 不再只依赖 Scene 中的 Bootstrap GameObject；它会在场景加载阶段确保苹果服务存在，并为当前场景中已作者化的 `AppleTreeInteractable` 建立树状态。
- 这一步必须发生在调试按钮快进时间之前。否则第一次点击大树时才创建状态，成熟计时会从快进后的当前时间重新开始，表现为始终“还没成熟哦”。
- `AppleTreeDropController`、`AppleDropSlot`、树 PolygonCollider2D、掉落槽和 CollectionText 的 Scene 序列化引用仍是原有事实源；本次不改 WorldMap 场景文件。
- 相关回归测试位于 `Assets/_Project/Tests/EditMode/AppleResourceServiceTests.cs`；Play 结果仍由人工验证。

## 2026-09-08 WorldMap 点击与室外桌宠移动边界

- `Assets/_Project/Scripts/Modules/WorldMap/WorldMapSceneInteractionRouter.cs` 是 WorldMap 显式点击目标的唯一裁决入口；邮箱、标牌、许愿树、苹果树、掉落苹果和桌宠都必须在 `WorldMap_Main.unity` 的 `_targets` 中拥有明确序列化引用。
- `WorldMapGardenZone` 的 `_owner` 分别为 `angel` / `demon`，邮箱通过 `PanelOpenButton` 指向 `DailySummaryMailbox`，许愿树通过 `WorldMapWishSystemController` 打开许愿面板；这些业务映射不能依赖对象名称猜测。
- `WorldMapPetInteractionController` 只负责桌宠 Collider2D 命中和控制权切换；普通移动、漫游、玩家输入和桥面高度分别由 `PetController`、`RandomWander`、`PetPlayerInputController` 和 `WalkableSurface` 负责。
- `WorldMapPetAnimationTriggerController` 是室外特殊动画入口，绑定两只 `PetController` 并在动作期间施加移动锁；不能用一个同时直接移动刚体的 WorldMap 脚本替代这条链路。
- 2026-09-08 的 Scene/脚本改动没有修改动画资源；Play 结果仍需人工确认。

## 2026-08-21 furniture selection feedback

Indoor furniture selection paths: `Assets/_Project/Scripts/Modules/HubUI/ApartmentFurnitureSelectionPresenter.cs`, `Assets/_Project/Scripts/Editor/SceneBootstrap/ApartmentFurnitureSelectionAuthoring.cs`, and the authored nodes `ApartmentFurnitureSelection`, `FurnitureSelectionHighlight`, and `FurnitureSelectionMessage` in `Assets/_Project/Scenes/Apartment/Apartment_Main.unity`. Runtime only toggles authored objects and text; the viewport bridge owns click routing. The nine original targets plus `家具_装饰_储物的家具_恶魔_01` are wired; “苹果垫” is the requirement label for this existing storage furniture.

Updated: 2026-08-22

## 2026-08-18 Play verification addendum

- Outdoor `WorldMap_Main/室外背景/邮箱` -> hidden `WorldMapDailySummaryMailboxOpenTarget.PanelOpenButton.OnClick` -> WorldMap `Panel_DailySummaryMailbox` routing was verified after scene authoring; the Apartment legacy entry is inactive.
- Both WorldMap pets moved within their serialized `RandomWander` bounds and drove the existing Animator movement parameters; no runtime visual nodes were created.
- Daily-summary persistence remains covered by the targeted EditMode suite (`EmotionGardenPlacementPersistenceTests`, 5/5).

## 苹果资源系统（2026-08-18 新版规则）

- 运行时模块：`Assets/_Project/Scripts/Modules/Apple/`；`AppleService` 实现 `IAppleService` 与 `IPersistentService`，存档 key 为 `apple`。
- 规则：新档 20 个苹果；每棵大树按现实 UTC 时间 45–90 分钟随机生成一轮、每天最多 5 轮、每轮 70% 为 1 个/30% 为 2 个；未领取缓存不设 3 个上限，时间表、当日轮数和缓存随存档保存。时间来源必须是 `IGameClock`。
- WorldMap 交互：`AppleTreeInteractable` 与 `AppleTreeFeedback` 只挂在「大树 2」～「大树 5」；「大树 1」不是苹果树，不绑定苹果领取或反馈。苹果树点击领取全部缓存，无缓存时播放反馈。
- 消费入口：`GachaService` 使用苹果支付单抽/五连（20/100）；`TarotService.CreateSession` 使用 8 个苹果开启会话，余额不足时不进入有效选牌。
- 花朵奖励：`EmotionGardenService.BloomAt` 首次成熟奖励 12 个苹果，重复成熟不重复奖励。
- UI：`ApartmentAppleBalanceAuthoring` 复用四个现有 `TopResource/BalanceLabel`，移除旧版重复 `AppleBalanceLabel`；`StubPanelBase`、`GachaPanelController` 和 `Collection/AppleBalanceDisplay` 统一更新苹果数字余额。

## 入口文件
- `AGENTS.md`
  - 当前项目总入口。任何智能体进入项目后都应先读它。
- `docs/current-task-card.md`
  - 当前这一轮任务的轻量任务卡（L1）。
- `docs/current-task-card.json`
  - 当前任务卡的机器可检查版本。
- `docs/ai-memory/gemini-lab-memory-main.md`
  - 当前主记忆总览与第二入口。
- `docs/workflow-context-packages.md`
  - 不同任务类型应优先加载哪些上下文文件。
- `docs/context-compression-and-knowledge-plan.md`
  - 第二部分工作方式升级：上下文压缩、做梦整理、L2/L3 知识沉淀计划。
- `docs/dream-maintenance-checklist.md`
  - 当前人工版“做梦整理”执行清单。
- `tools/check-task-gate.ps1`
  - 当前最小闭环执行闸门脚本。
- `tools/task-card-utils.ps1`
  - 读取任务卡、规范化仓库路径并计算排除执行态字段后的计划 hash。
- `tools/check-task-scope.ps1`
  - 创建/验证任务开始时的工作树基线，阻止未列入 `direct_files` 的新增或变化路径。
- `tools/verify-task.ps1`
  - 统一执行 review 闸门、任务范围、`git diff --check` 和 PowerShell 语法检查，并输出 JSON 报告。
- `tools/check-scene-visual-contract.ps1`
  - 按当前任务卡中的 `scene_visual_contracts` 检查 Scene 节点及关键 Sprite 序列化引用。
- `tools/check-runtime-visual-contract.ps1`
  - 按当前任务卡中的 `runtime_visual_files` 扫描运行时代码的禁止视觉作者化模式。
- `tools/run-unity-editor-method.ps1`
  - 项目本地 Unity batchmode runner，可定位 Unity Editor 并执行 editor static method；当前用于在没有完整 MCP 的情况下落地场景 authoring，并带 `-nographics`、启动日志超时、总执行超时和子进程 watchdog，避免 Unity 启动卡住时无期限阻塞。
- `Assets/_Project/Scripts/Core/UI/UIRouter.cs`
  - 当前顶层面板路由入口。`Open` 已改为互斥切换：打开新面板前会先关闭当前已开的顶层面板。
- `README.md`
  - 项目总说明，面向产品、技术栈、整体架构和路线图。
- `Assets/README.md`
  - 更偏业务与 FSM 的设计说明。
- `Assets/plan.md`
  - 阶段里程碑、Sprint 拆解、DoD 与风险表。

## 先看哪里

### 视觉任务的强制字段
- 每次任务卡都必须填写 `scene_play_parity_required`、`scene_visual_contracts` 和 `runtime_visual_files`；不涉及视觉时使用 `false`、`[]`、`[]`。
- `direct_files` 命中 `.unity`、`Assets/_Project/Art/`、运行时 `Scripts/Modules` / `Scripts/UI`、WorldMap SceneBootstrap 或编辑器视觉工具时，会被任务闸门视为视觉任务，必须把 Scene 作为事实源。
- 视觉任务的最终 Sprite、AnimatorController、RectTransform、排序层级和 UI 节点必须在 Scene / Prefab / Inspector 中可见并可调。运行时只允许读取数据、切换已有对象状态和填充动态文本，不允许把最终视觉藏在运行时资源赋值或动态 UI 生成里。
- 修改或评审视觉任务时必须同时运行 `tools/check-task-gate.ps1`、`tools/check-scene-visual-contract.ps1` 和 `tools/check-runtime-visual-contract.ps1`；子检查失败即视为任务未完成。

### 工作流兼容增强
- 原有流程仍然是“探索 → 规划 → 用户确认 → 行动”；机器约束只在任务卡和工具层验证这条流程，不改变四段式规划模板。
- 新任务卡使用 `task_id` 区分任务，使用 `human_approved=true` 和 `approval_source` 记录用户确认，使用 `plan_hash` 防止批准后的范围被悄悄改写。
- 写入前先通过 `tools/check-task-gate.ps1 -Mode write`，然后运行 `tools/check-task-scope.ps1 -Mode CreateBaseline`；收尾运行 `tools/verify-task.ps1 -JsonOnly`。
- 直接文件工具或外部进程仍可能绕过仓库内脚本，因此这些检查是可执行的仓库防线，不等同于操作系统级权限隔离。

### 想知道项目现在处于什么状态
- `AGENTS.md`
- `docs/current-task-card.md`
- `docs/ai-memory/gemini-lab-memory-main.md`
- `docs/ai-memory/gemini-lab-memory-rules-and-history.md`
- `docs/project-structure-overview.md`
- `docs/context-compression-and-knowledge-plan.md`
- `docs/current-task-card.json`

### 想知道项目真正要做成什么
- `docs/gameplay-spec.md`
- `README.md`
- `Assets/README.md`

### 想知道代码和模块应当怎么分层
- `docs/ai-memory/gemini-lab-memory-architecture.md`
- `Assets/_Project/Scripts/README.md`
- `Assets/_Project/Scripts/Core/README.md`
- `Assets/_Project/Scripts/Modules/README.md`
- 各模块 README

### 想知道场景、Prefab、SO 怎么组织
- `docs/project-structure-overview.md`
- `Assets/_Project/Scenes/README.md`
- `Assets/_Project/Prefabs/README.md`
- `Assets/_Project/ScriptableObjects/README.md`

### 想从真实运行时入口开始看
- `Assets/_Project/Scenes/Boot.unity`
- `Assets/_Project/Scripts/Core/GameBootstrap.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetController.cs`
- `Assets/_Project/Scripts/Modules/Pet/WalkableSurface.cs`
- `Assets/_Project/Scripts/Modules/Pet/RandomWander.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetPlayerInputController.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetPlayerFurnitureInteractionController.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetClickReactionController.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetClickResponseLibrary.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetRuntimeSnapshotChangedEvent.cs`
- `Assets/_Project/Scripts/Editor/Pet/PetMoveAnimationSetupEditor.cs`
- `Assets/_Project/Scripts/Editor/Furniture/ApartmentFurnitureAuthoringBootstrapEditor.cs`
- `Assets/_Project/Scripts/Editor/Build/McpNuGetPlayerImportGuard.cs`
- `Assets/_Project/Scripts/Editor/Tools/DebugDisplayWindow.cs`
- `Assets/_Project/Scripts/Editor/Tools/ReadingBubbleLayoutSync.cs`
- `Assets/_Project/Scripts/Editor/Tools/SaveSlotTemplateCreator.cs`
- `Assets/_Project/Scripts/Editor/SceneBootstrap/SettingsAndSaveSlotsPanelAuthoring.cs`
- `Assets/_Project/Scripts/Editor/SceneBootstrap/AutoSetup.cs`
- `Assets/_Project/Scripts/Editor/SceneBootstrap/EditorBootSceneLoader.cs`
- `Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapEmotionGardenUIPatch.cs`
- `Assets/_Project/Scripts/Modules/EmotionGarden/EmotionFlowerModels.cs`
- `Assets/_Project/Scripts/Modules/EmotionGarden/EmotionGardenService.cs`
- `Assets/_Project/Scripts/Modules/HubUI/Panels/EmotionInputPanelStub.cs`
- `Assets/_Project/Scripts/Modules/HubUI/Panels/FlowerCollectionPanelStub.cs`
- `Assets/_Project/Scripts/Modules/HubUI/Panels/SceneAuthoredImageVariantView.cs`
- `Assets/_Project/Scripts/Modules/HubUI/Panels/WeeklyGardenPanelStub.cs`
- `Assets/_Project/Scripts/Modules/Furniture/FurnitureService.cs`
- `Assets/_Project/Scripts/Modules/Furniture/ApartmentSceneFurnitureBindings.cs`
- `Assets/_Project/Scripts/Modules/Furniture/SceneFurnitureDefinitionHint.cs`
- `Assets/_Project/Scripts/Modules/UI/StatusPanelController.cs`
- `Assets/_Project/Animations/Pet/Pet_Angel.controller`
- `Assets/_Project/Scripts/Modules/Gateway/GatewayBootstrap.cs`
- `Assets/_Project/Tests/EditMode/GeminiLab.Tests.EditMode.asmdef`

### 想做美术替换
- `docs/art-replacement-workflow.md`
- `docs/apartment-scene-sprite-naming-guide.md`
- `Assets/_Project/Art/README.md`
- `Assets/_Project/Art/WorldMap/UI/flowerCodex`
- `Assets/_Project/Art/WorldMap/UI/flower_info`
- `Assets/_Project/Art/WorldMap/flower`
- `Assets/_Project/Art/WorldMap/UI/garden_week`
- `Assets/_Project/Art/WorldMap/UI/growth`
- `Assets/_Project/Art/Sprites/Furniture/README.md`
- `Assets/_Project/Art/Sprites/Pet/README.md`
- `Assets/_Project/Prefabs/README.md`
- `Assets/_Project/ScriptableObjects/README.md`

### 想看包依赖和工具链
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Packages/SkillsForUnity/`
- `PackageBackups/MCP-disabled-2026-06-02/`
- `PackageBackups/NuGet-disabled-2026-06-02/`
- `ProjectSettings/ProjectVersion.txt`
- `Assets/_Project/Scripts/Editor/Build/McpNuGetPlayerImportGuard.cs`
- `.cursor/mcp.json`
- `.cursor/skills/`
- `.agents/skills/`
- `docs/project-skill-catalog.md`
- `docs/skill-design-boundary.md`
- `.agents/skills/git-sync-upstream-main/SKILL.md`
- `.agents/skills/unity-clear-generated-cache/SKILL.md`
- `.agents/skills/apartment-scene-rollback-to-commit/SKILL.md`
- `.agents/skills/pet-animation-reference-rebuild/SKILL.md`
- `.agents/skills/furniture-binding-check/SKILL.md`

### 想看版本控制与 PR 流程
- `docs/git-fork-upstream-pr-workflow.md`
- `AGENTS.md`

### 想按任务类型加载最小必要上下文
- `docs/workflow-context-packages.md`
- `docs/current-task-card.md`
- `docs/context-compression-and-knowledge-plan.md`

## 当前真实存在的重要目录

### 文档与协作
- `AGENTS.md`
  - 当前项目总入口与执行规则入口
- `docs/current-task-card.md`
  - 当前轮任务的轻量任务卡
- `docs/current-task-card.json`
  - 当前轮任务卡的机器检查版
- `docs/workflow-context-packages.md`
  - 当前项目推荐的上下文装配方式
- `docs/context-compression-and-knowledge-plan.md`
  - 当前项目第二部分工作方式升级计划
- `docs/dream-maintenance-checklist.md`
  - 当前项目人工版做梦整理清单
- `tools/check-task-gate.ps1`
  - 当前项目最小闭环执行闸门
- `tools/run-unity-editor-method.ps1`
  - 当前项目本地 Unity batchmode 执行入口，带 watchdog 和超时保护
- `Assets/_Project/Scripts/Core/UI/UIRouter.cs`
  - 当前项目顶层 UI 互斥切换的统一入口
- `docs/`
  - 项目文档根目录
- `docs/ai-memory/`
  - 记忆文档与索引
- `docs/project-skill-catalog.md`
  - 当前项目本地 skill 清单与分类说明
- `docs/skill-design-boundary.md`
  - 当前项目 skill 的设计边界与组织方式
- `.agents/skills/git-sync-upstream-main/`
  - 第一批 workflow skill：安全同步本地 `main` 到 `ULookup:main`
- `.agents/skills/unity-clear-generated-cache/`
  - 第二批 workflow skill：只清理 Unity 生成缓存，不改源码
- `.agents/skills/apartment-scene-rollback-to-commit/`
  - 第三批 workflow skill：精准回退 `Apartment_Main.unity` 到指定 commit
- `.agents/skills/pet-animation-reference-rebuild/`
  - 第四批 workflow skill：重建 `Pet_Angel` 的动画资源与 controller 引用链
- `.agents/skills/furniture-binding-check/`
  - 第五批 workflow skill：只读巡检 Apartment 家具绑定状态
- `docs/git-fork-upstream-pr-workflow.md`
  - 当前项目标准 fork / upstream / feature branch / PR 工作流

### Unity 项目
- `Assets/_Project/`
  - 自研业务唯一正式根目录
- `Assets/_Project/Scripts/`
  - 已存在真实 C# 实现、asmdef 与少量编辑器工具
- `Assets/_Project/Scenes/`
  - 已存在 `Boot.unity`、`Apartment/Apartment_Main.unity`、`WorldMap/WorldMap_Main.unity`、`Desktop/Desktop_Overlay.unity`
- `Assets/_Project/Tests/`
  - 已存在 `EditMode` / `PlayMode` 测试程序集
- `Assets/_Project/Art/` 与 `Assets/_Project/Animations/`
  - 已存在宠物、家具、环境示例资源与宠物动画
  - `Assets/_Project/Art/Sprites/Furniture/**/` 当前已开始承接从 `公寓场景.psd` 派生出来、准备用于家具系统接线的独立 Sprite，后续按中文语义命名维护
- `Packages/`
  - 包依赖与嵌入式包
  - 当前保留的关键本地包是 `SkillsForUnity`
- `PackageBackups/`
  - 保留旧版 4 个 Unity MCP 包与 NuGet DLL 的停用备份；当前活动 MCP 插件由 `Packages/manifest.json` 的 `com.ivanmurzak.unity.mcp` `0.88.0` 提供
  - 当前还用于暂存从 `Assets/Plugins/NuGet` 软移出的 MCP NuGet DLL 目录
- `ProjectSettings/`
  - Unity 项目级设置

## 当前真实存在但容易误判的情况
1. `Assets/_Project/Scenes/` 已经有真实 `.unity` 场景文件，但它们当前更接近原型场景，不等于所有 README 里的目标场景集合都已完成。
2. `Assets/_Project/Prefabs/` 已开始落地真实 `.prefab` 资源，当前 `Furniture/` 已覆盖全部家具 Sprite 资源。
3. `Assets/_Project/Scripts/` 已经有大量真实 C# 业务实现与 asmdef，不再是只有 README 的空目录。
4. `Assets/_Project/ScriptableObjects/` 已开始落地实际 `.asset` 配置，当前 `FurnitureConfig/` 已覆盖全部家具 Sprite 资源；很多其他 SO 类型仍只存在于代码层。
5. `Assets/_Project/Scripts/Modules/Desktop/README.md` 仍承载桌面模块的设计说明，但当前真实运行时代码目录是 `Assets/_Project/Scripts/Modules/DesktopOverlay/`。
6. 多个系统当前依赖运行时兜底或 Mock 配置，看到“能跑起来”不等于“资产作者化已完成”。
7. `2026-05-26` 起，Apartment 场景里的旧占位 UI 残留（`TopLeft_StatusPanel`、`Right_InventoryPanel`、`BottomRight_PersonalityRadar`）已从 `Apartment_Main.unity` 真实移除；旧的 `SpaceSystemPrototypeRoot` 原型 UI 也不再作为后续 UI 制作基础。当前保留的新主界面骨架为 `Panel_PetStatus`、`Panel_SpaceSys`、`Sidebar`、`SidebarOverlay`、`ApartmentViewportHost`、`ApartmentViewportImage` 与 `ApartmentViewportCamera`。此前尝试绑定到 `Panel_PetStatus` 的 `profile` 贴图、宠物正面待机预览 Sprite、雷达配色与尺寸微调已撤回；当前公寓 viewport 已改挂到 `Panel_SpaceSys`，`Profile` 重新只承担双宠资料展示。具体 UI 美术资源选择、贴图映射与最终视觉作者化后续由人工完成，AI 后续只继续承接弱视觉或非美术资源相关的逻辑、结构、输入桥接、验证与文档任务。同日已完成一轮非美术 UI 技术收口：`ApartmentViewportInputBridge` 增加矩形内点击判定与可测试坐标转换，`SidebarController` / `StubPanelBase` 增加 `IUIRouter` / `EventBus` 兜底注册，`ProfilePanelStub` / `TarotPanelStub` / `InventoryPanelStub` 增加服务缺失或空数据兜底；`Assets/_Project/Prefabs/UI/Panels` 与 `Assets/_Project/Prefabs/UI/Widgets` 已建立目录说明但尚无正式 UI prefab。
8. `2026-05-22` 起，Apartment 场景中的 `Pet` 根节点已同时包含 `Pet_Angel` 与 `Pet_Devil`；当前玩家控制方式为“默认天使可控，点击恶魔后切换恶魔主控”，不再是简单复制输入组件后让双宠同时吃同一套方向键；未被选中的桌宠当前会保持待机，不再继续跑自动睡觉链路。当前 `Pet_Angel` 与 `Pet_Devil` 也不再共用同一个移动边界：恶魔已切到左侧专用 `PetMovementBounds_Devil`。
9. `Assets/_Project/Animations/Pet/` 当前除 3 个 move clip 外，已新增 `Pet_Angel_Interact_Read.anim` 与 `Pet_Angel_Interact_BesideDoor.anim`，并新增 `Pet_Devil_Move_* / Pet_Devil_Idle_* / Pet_Devil_Sleep.anim`、`Pet_Devil_Interact_BesideDoor.anim`、`Pet_Devil_Interact_Write.anim` 与 `Pet_Devil_Interact_PlayingMusic.anim`；但恶魔其余完整交互动画仍未补齐。
10. `2026-05-25` 起，Apartment 场景已开始搭第一版 `viewport` 结构：当前 `ApartmentViewportHost` 与 `ApartmentViewportImage` 已归属 `Panel_SpaceSys/Content`，并新增 `ArtGenerated/ApartmentViewportCamera`；当前 `RenderTexture` 资产路径为 `Assets/_Project/Settings/RenderTextures/ApartmentViewport_RT.renderTexture`。同日已补 `ApartmentViewportInputBridge`，当前可把 viewport 内点击先桥接到桌宠点击，再桥接到当前宠物的家具交互链路；当 `BuildModeController` 开启时，也会优先桥接到建造模式的放置/删除家具入口。
11. `2026-07-28` 起，`WorldMap_Main.unity` 中桥对象 `桥` 的过桥移动轮廓由同物体 `PolygonCollider2D` 上侧轮廓直接提供。`WalkableSurface` 是运行时读取入口，`WorldMapSceneObjectsPatch` 只确保桥对象有 `WalkableSurface` 并保留现有 collider 点位，不再维护 `_profileLocalPoints` 独立折线轨道。`PetController.ResolveGroundY` 会把桥面 surface Y 当作脚底/行走锚点高度，再换算成 transform Y。相关 PlayMode 表现仍需按 `docs/manual-validation-checklist.md` 的 B9 章节人工补验。
12. `2026-07-29` 起，WorldMap 情绪花图鉴列表页与详情页的美术资源入口为 `Assets/_Project/Art/WorldMap/UI/flowerCodex` 与 `Assets/_Project/Art/WorldMap/UI/flower_info`。运行时入口是 `FlowerCollectionPanelStub`，编辑器作者化入口是 `WorldMapEmotionGardenUIPatch.SetupFlowerCollectionBookContent`。设计要求是 Scene/Inspector 可调：`Panel_EmotionCollection/Content` 下应由 `CodexView`、`DetailView`、书本背景、卡槽、按钮和文本字段这些真实 UI 子节点组成；运行时只填数据和切换视图。本轮已通过 Unity 编辑器一次性作者化脚本执行 `WorldMapEmotionGardenUIPatch.Patch()` 并落盘 `WorldMap_Main.unity`，场景 YAML 已恢复当前 UI 资源 GUID；PlayMode 点击和最终视觉微调仍需在 Unity 中补验。
13. `2026-07-30` 起，WorldMap 的点击与碰撞路由也已收口：
   - `ClickOcclusionUtility` 当前收在 `Assets/_Project/Scripts/Core/DevMode.cs`
   - `CabinReturnPortal`、`WorldMapGardenZone`、`ClickableSceneObject`、`BaselineItem`、`PetPlayerInputController`、`PetClickReactionController`、`WorldMapCameraController` 都先判断最上层 2D 点击目标，再决定是否响应
   - `PetController` 新增 WorldMap 场景级双宠碰撞忽略逻辑，`Pet_Angel` 与 `Pet_Devil` 在 `WorldMap_Main` 中不会互相挡路
13. `Assets/_Project/Art/Sprites/Pet/Frames/Move/` 当前已经从旧的平铺命名，切换为 `正面 / 背面 / 侧面` 三个子目录；对应导入链路由 `PetMoveAnimationSetupEditor` 兼容新旧两套来源。
14. `Assets/_Project/Art/Sprites/Pet/Frames/Interact/` 当前两组交互帧已经统一改为规范命名：`Pet_Angel_Interact_Read_0001...` 与 `Pet_Angel_Interact_BesideDoor_0001...`，不再使用 `IMG_986x.PNG`。
14. `2026-06-02` 起，项目已补一条 Windows 构建防护：`Assets/_Project/Scripts/Editor/Build/McpNuGetPlayerImportGuard.cs` 会把 `Assets/Plugins/NuGet` 下由 Unity MCP 依赖解析器解压出的 `McpPlugin / SignalR / Microsoft.Extensions.*` DLL 统一校正为 `Editor-only`，避免它们以 Player 插件身份进入 Bee / Burst 构建链。
15. `2026-06-02` 同日，项目曾把 4 个旧版 Unity MCP 包临时从 `Packages/` 移到了 `PackageBackups/MCP-disabled-2026-06-02/`；`2026-08-18` 起活动依赖改为 `com.ivanmurzak.unity.mcp` `0.88.0`，旧备份继续保留，`Packages/SkillsForUnity` 不受影响。
16. `2026-08-18` 起全局工具链为 Node.js `v24.19.0` + `unity-mcp-cli` `v0.88.0`；Unity 已安全重启并连接 `http://localhost:27345`，`unity-tool-list` 与定向 EditMode 测试可通过 CLI 实跑。
16. `2026-06-02` 同日，项目还把 `Assets/Plugins/NuGet` 整个目录及其 `.meta` 软移出到了 `PackageBackups/NuGet-disabled-2026-06-02/`，并清空了 `ProjectSettings/ProjectSettings.asset` 里的 `Standalone` `UNITY_MCP_READY`，用于让 Windows Build 不再继续命中这批 MCP NuGet 残留 DLL。
17. `Apartment_Main.unity` 当前并不是所有“看起来像家具”的对象都天然进入家具逻辑；首轮显式接线通过 `ApartmentSceneFurnitureBindings` 给关键对象补 `Furniture` / `InteractionAnchor` / `SceneFurnitureDefinitionHint`。
18. `ApartmentSceneFurnitureBindings` 当前已经覆盖公寓场景里主要可交互对象，但仍要注意两类现实区别：
   - 有些对象已经进入对象级交互类型，却未必已经摆进 `Apartment_Main.unity`
   - 场景绑定里的定义 ID、类别和交互类型需要持续与真实 Sprite 资源名保持一致，不能把 `WorkDesk` 类资源误绑成装饰类
19. `Pet` 模块当前需要区分“长期规划”和“现阶段入口”：
   - 长期规划仍保留 HFSM、自主行为、Gateway / Travel / AI 对话方向
   - 当前 Apartment 原型里，`Pet_Angel` 的主要行动入口已经切换为 `PetPlayerInputController`，由玩家通过 `WASD` / 方向键直接控制移动
   - 当前 Apartment 原型还新增了 `PetPlayerFurnitureInteractionController`，用于靠近特定家具或交互点时按 `F` 触发玩家手动交互
   - 当前 Apartment 原型还新增了 `PetClickReactionController`，用于鼠标左键点击桌宠后输出表情 Debug，并显示本地语料气泡回复
   - 当场景里同时存在 `Pet_Angel` 与 `Pet_Devil` 时，点击桌宠不仅会触发气泡回应，也会显式切换当前键盘控制对象
   - 当前恶魔的 `门边` 交互已沿用天使同一条 `Interact_BesideDoor` 触发链路，但 controller 已切到恶魔自己的 `Pet_Devil_Interact_BesideDoor.anim`
   - `2026-05-27` 起，恶魔当前还额外拥有两条玩家自交互接线：`画画` 会对着 `家具_休闲_画架_恶魔_01` 触发并坐到 `家具_装饰_椅子_恶魔_01`，`玩掌机` 会坐到 `家具_装饰_沙发_恶魔_02`
20. `Apartment_Main.unity` 当前会用 `StaticFurnitureDecorOnly` 承载一部分“已有独立 Sprite、但不直接走原始关卡对象”的静态家具；这类对象进入交互系统时，也要同步补进 `ApartmentSceneFurnitureBindings`，避免出现“场景有图但无交互绑定”或“绑定有定义但 `_target` 为空”。
21. 当前工作流已开始显式区分三层记忆：
   - `L1`：`docs/current-task-card.md`
   - `L2`：`docs/ai-memory/`
   - `L3`：git / PR / 长文档历史
22. 当前工作流已开始显式区分任务上下文包，入口在 `docs/workflow-context-packages.md`。
23. 涉及视觉、布局、UI、相机、装饰层的任务时，默认要求：
   - `Scene` 视图可直接看到
   - `Inspector` 可直接调整
   - `Play` 视图与 `Scene` 视图效果一致
   - 不依赖运行时脚本临时拼出最终视觉
24. `2026-07-12` 起，HubUI 面板（SaveSlotsPanel、ReadingBubble、TarotSummaryPreview）的编辑器预览系统已收口：
   - `SaveSlotsPanel` 现在使用 `[ExecuteAlways]` + 模板克隆模式：场景中的 `SlotTemplate` 为 inactive 模板，运行时和编辑器预览均通过 `Instantiate` 克隆，用户只需编辑模板即可统一修改所有槽位的美术资源
   - `DebugDisplayWindow` 的 Tarot Preview 开关现在会联动刷新场景中 `ReadingBubble` / `TarotSummaryPreview` 的 active 状态
   - 新增 `ReadingBubbleLayoutSync` 工具用于按 Angel/Devil 分组同步气泡布局
   - 新增 `SaveSlotTemplateCreator` 工具用于在场景中创建/更新 SlotTemplate
   - 新增长期规则 #12：禁止在未经用户确认的情况下修改 Unity scene 文件或场景对象（含编辑器回调中的隐式修改）

## 模块 README 导航
- `Assets/_Project/Scripts/Modules/Pet/README.md`
- `Assets/_Project/Scripts/Modules/Furniture/README.md`
- `Assets/_Project/Scripts/Modules/Navigation/README.md`
- `Assets/_Project/Scripts/Modules/Gateway/README.md`
- `Assets/_Project/Scripts/Modules/Travel/README.md`
- `Assets/_Project/Scripts/Modules/Desktop/README.md`
- `Assets/_Project/Scripts/Modules/Persistence/README.md`

## 什么时候刷新这个文件
出现以下任一情况就要更新：
- 新增或删除关键目录
- 场景、Prefab、SO 的真实落地状态变化
- 工具入口变化
- `AGENTS.md` 入口协议变化
- 目录或文件名改动导致现有导航失效

### 2026-07-30 WorldMap WeeklyGarden 备注
- `WorldMap_Main.unity` 中 `Panel_WeeklyGarden/Grid/CellTemplate` 仍保留为 Scene/Inspector 可调模板，但默认必须隐藏。
- `WeeklyGardenPanelStub` 运行时会兜底隐藏 `CellTemplate`，实际只应显示 7 个 Day cell。
- `Panel_WeeklyGarden/Grid` 现在不再挂 `HorizontalLayoutGroup`，`Day0`~`Day6` 是可独立拖动的普通场景节点；`WorldMapEmotionGardenUIPatch` 也不再清理既有格子位置，避免作者化重跑后把手工摆位抹掉。
- `Panel_WeeklyGarden/Grid/CellTemplate` 与 `Day0`~`Day6` 的瓶内保留 `FlowerImage`；`FlowerImage` 仅在已开花时由 `WeeklyGardenPanelStub` 按情绪花类型显示带枝叶完整花图。
- `Panel_WeeklyGarden/Content/UIbar` 是场景中唯一的集中信息栏，子节点为 `Growth`、`DateText`、`EmotionText`、`FlowerLanguageText`；使用 `Assets/_Project/Art/WorldMap/UI/garden_week/UIbar.png`，根节点 `localScale` 为 `1.5, 1.5, 1`。`Growth` 是成长阶段花型 icon，预置 `Assets/_Project/Art/WorldMap/花朵图鉴/花朵/` 下天使/恶魔与九种情绪的18张花头资源，不再使用通用 `bud.png`。`Panel_WeeklyGarden` 根节点的 Scene `localScale` 为 `0.85, 0.85, 1`，UIbar 内部字号为 `18/16/18`。
- `Day0`~`Day6/Bottle` 挂 `WeeklyGardenBottleInteraction`，并保留作者化的 `SelectedHighlight`；高亮 Image 使用 `Assets/_Project/Art/WorldMap/UI/garden_week/SelectedBottleOutline.mat` 的 Alpha 边缘材质，只输出瓶子外轮廓，不输出整张瓶子填充。`Content/BlankClickArea` 负责空白点击取消选择。悬浮只改瓶子 `RectTransform` 缩放，选中只激活外圈。
- `EmotionFlowerArtCatalog` 位于 `Assets/_Project/Art/WorldMap/flower/EmotionFlowerArtCatalog.asset`，由 `WorldMapEmotionGardenUIPatch` 作者化并同时绑定到 `FlowerCollectionPanelStub` 与 `WeeklyGardenPanelStub`。
- 花卉图鉴的已解锁条目只代表已开花并收入图鉴的花朵，`FlowerCollectionPanelStub` 固定使用 `GrowthState.Bloomed`；缺少对应 `（完整）.PNG` 时不回退到仅花朵资源。
- `2026-08-07` 起，`Panel_WeeklyGarden/Grid/Day0`~`Day6` 的有花格子、`Panel_EmotionCollection/CodexView` 的已解锁卡片和 `DetailView` 的已收集花卉均包含 `SoilImage`。它们统一引用 `Assets/_Project/Art/WorldMap/flower/土壤.PNG`，土壤位于带枝叶完整花图的下方；空白、锁定或无对应完整花图的状态隐藏土壤。三处大小和位置仍分别由 Scene/Inspector 调整。
- `2026-08-07` 修正：图鉴卡片 `FlowerImage` 的空 Sprite 占位透明度不再影响运行时完整花图；已收集卡片和详情花图绑定时强制恢复不透明。`SoilImage` 已重新收紧到花图可见枝叶底部，详情页会跟随当前 `FlowerImage` 的手工 X 位置对齐。
- `2026-08-09` 修正：每周培育瓶子固定使用 `Assets/_Project/Art/WorldMap/UI/garden_week/bottle.png`，不再按培育者或成长阶段切换不存在的瓶子变体；`DayLabel/DayText` 旧文字节点保持关闭，只显示 Mon～Sun 图片。
- `2026-08-09` 修正：`CodexCardSlot_00...11` 的 Scene 默认状态由作者化数据明确保存：前三张已收集卡显示真实花枝与土壤，其余锁定卡的 `FlowerImage`、`SoilImage` 与 `UnlockedContent` 整体关闭；`FlowerCollectionPanelStub` 运行时只切换这些预置节点。
- `2026-08-09` 修正：详情页 `FlowerImage/Variant_00...` 每个变体都包含成对的 `FlowerArt` 与 `SoilImage`，土壤位置按花型单独保存；No.028 月晕使用 `悲伤|angel|1` 的完整花图和土壤。
- `2026-08-09` 修正：每周空日的花朵、成长阶段 icon 和土壤全部隐藏，UIbar 无花时显示 `---`；`SceneAuthoredImageVariantView` 负责 Scene 预置变体的显隐，不在运行时创建最终视觉节点。
- `2026-08-10` 修正：每周面板改为单一集中 UIbar；未选择时显示当天，点击瓶子显示对应日期，点击空白恢复当天；悬浮缩放、选中外圈高亮均基于场景预置节点。
- `2026-08-10` 追加修正：选中高亮从填充式 UI `Outline` 改为 `SpriteAlphaOutline` 边缘材质，避免整只瓶子变色。
- `2026-08-07` 新增 `WorldMapFlowerSoilLayoutWindow`：从 `Tools/Gemini-Lab/WorldMap 花卉布局复用` 打开，分别指定三个面板的参考对象后，可将 `FlowerImage` 与 `SoilImage` 的锚点、位置、尺寸、Pivot 和 localScale 批量复制到同类节点；复制支持 Undo，执行后需手动保存场景。

### 2026-08-04 WorldMap 花朵自由摆放
- 运行时脚本：`Assets/_Project/Scripts/Modules/WorldMap/WorldMapFlowerPlacementController.cs`。
- 场景作者化脚本：`Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapFlowerPlacementAuthoring.cs`。
- 网格配置：由 `WorldMapFlowerPlacementController` 读取 `garden/中景/花丛.png` 的 Sprite 完整尺寸得到二维 `Vector2` 单元（当前为 `4.01 x 2.24` Unity 单位），再结合网格原点和摆放区域生成完整 X/Y 网格线；网格线仅作为作者化/调试资源保存，运行时隐藏；不绑定或读取图鉴花朵资源作为显示。
- `2026-08-10` 已完成侧边栏版本的实际落盘，并于 `2026-08-11` 修正为参考图对应的右侧布局：`Canvas/Btn_FlowerPlacement` 位于右下角，`Canvas/FlowerPlacementPanel` 锚定右侧并保留 `UIBoard.png` 原始 `498 x 899` 尺寸；`FlowerSidebarViewport`、`FlowerSidebarContent`、`FlowerList/FlowerOption_00...17`、`SynthesisHintBubble`、`WorldMapPlacedFlowers/FlowerPlacementGrid`、`FlowerPlacementPreview`、`PlacementSlot_00...31` 与 `FlowerPlacementBounds` 均存在于 `WorldMap_Main.unity`。旧底部库存节点不参与当前滚动列表，遗留顶层 `FlowerPlacementStatusBar` 已清理。
- `2026-08-11` 侧栏展开已改为确定性的手风琴布局：`FlowerList` 直接作为 `ScrollRect.content` 并管理 18 个 `FlowerOption`；其 `VerticalLayoutGroup.childControlHeight` 已启用，因此每个条目预置的 `LayoutElement` 会以 `73 / 320` 的收起/展开高度真实控制下方条目位移。详情页位于选中标题下方，不会再被后续列表元素遮挡；控制器会保持被点击标题在视窗中的位置，避免从一个条目切换到另一个条目时出现不规则跳动。
- `2026-08-12` 侧栏视窗已保存为左右 34、顶部 132、底部 56 的真实拉伸边距，列表起点固定在标题下方并受 `RectMask2D` 裁剪。选择单花/花丛后侧栏自动收起但保留当前选择和预览，玩家可直接点击场景；重新点击布置入口可更换花种。可通过 `Tools/Gemini-Lab/WorldMap/Author Flower Placement` 单独重新作者化该系统，不触发其他 WorldMap 补丁。
- `2026-08-13` 摆放系统读取 `BaselineItem` 的实际基线与排序层：同层单格不可重叠，跨层可重叠；内部吸附在相邻层之间半格错位，但 `FlowerPlacementGrid` 运行时保持隐藏。作者化只收录摆放区域内的基线；花丛改为一花丛一格，尺寸取场景 `花丛 3` 的碰撞体基准（约 `3.99 x 2.22`），运行时不生成视觉节点，并清理槽位重复旧组件。
- `2026-08-12` 摆放与每周培育共享 `EmotionGardenService` 内的持久化 `PlacementFlowerInventory`：开花增加单花，单花/花丛摆放消耗对应库存，3 个单花主动合成 1 个花丛；存档版本 3 会从旧档的已开花数据一次性迁移库存。版本 4 另存 `PlacedFlowers`，成功摆放立即 autosave，重启后恢复槽位/坐标且不重复扣库存；自由摆放单花和花丛均不添加土壤。若当前存档没有 `PlacedFlowers`，不会把仅有的开花记录臆测成历史坐标。
- 侧栏 UI 绑定 `Assets/_Project/Art/WorldMap/arrange`；花种列表图标绑定 `Assets/_Project/Art/WorldMap/花朵图鉴/花朵`，放置系统单花绑定 `花朵图鉴/花朵放置`，花丛绑定 `花朵图鉴/花丛`，不是 `arrange.png` 裁剪图。网格材质为 `arrange/PlacementGrid.mat`，网格线仅作为作者化/调试资源保存，运行时不显示。单花目录包含 9 种情绪的天使/恶魔共 18 张 Sprite。
- `FlowerPlacementBounds` 默认是 `36 x 8.96` 的禁用 BoxCollider2D，仅存放草地摆放区域数据，可在 Inspector 调整。运行时控制器只切换预置显示、移动空闲 `PlacementSlot` 并写入摆放元数据；不负责创建最终视觉对象。
- `2026-08-23` `WorldMap_Main.unity` 另有 `WorldMapPlacedFlowers/FlowerPlacementRegion_Angel` 与 `FlowerPlacementRegion_Demon`，每个节点由 `WorldMapFlowerPlacementRegion` + 禁用的 BoxCollider2D 作者化 owner 和可调范围。重跑 `WorldMapFlowerPlacementAuthoring` 只在节点首次创建或尺寸为空时写入默认左右半区，保留后续 Scene/Inspector 手调；控制器按所选花种 owner 限制完整 footprint。区域列表非空时区域是唯一判定来源，缺失引用或 owner 不匹配会直接失败并输出诊断；只有未配置任何区域的旧场景才回退 `FlowerPlacementBounds`。
- `2026-08-14` `WorldMapPlacementSlot` / `WorldMapPlacedFlower` 已拆为独立脚本并使用稳定 GUID；运行时会校验 32 个槽位和每槽 36 个 `PlacedVisual_*` 绑定。点击提交期间暂缓同步摆放恢复事件，避免事件重入清空刚显示的槽位；槽位以显式占用状态参与同层冲突判定。若要验收鼠标端到端，必须使用仍有单花或花丛库存的 Play 存档。
- `2026-08-14` 花朵遮挡修正：`WorldMapFlowerPlacementController` 使用 `Default` Sorting Layer；预览、已摆放花朵和恢复路径与桌宠共享 `BaselineItem.SortingOrder` 主层级，并按基线 Y 二次排序，完全同线时桌宠提高 1 个排序单位。`WorldMapPlacementSlot` 会递归同步花丛显示树内的所有 SpriteRenderer。AutoSetup 版本 63 已给 WorldMap 的两个桌宠根对象保存 `BaselineItem`，基线取碰撞体底部、`solidCollider=true` 并排除出可种植层。

### 2026-08-05 WorldMap 昼夜切换
- `Assets/_Project/Scripts/Modules/WorldMap/WorldMapDayNightController.cs`：WorldMap 按 `IGameClock.Now` 切换夜幕。
- `Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapDayNightAuthoring.cs`：将现有夜幕 Sprite 作者化为 `WorldMapNightOverlay`，设置覆盖范围、排序和当前本地时间初始状态。
- `WorldMap_Main.unity` 已保存 `WorldMapNightOverlay`，引用 `weather/夜幕.png`，默认 06:00–18:00 为白天；旧 `garden/天气（最上层）` 仅保留历史文件。`WorldMapWeatherStars` 同由昼夜控制器在夜间启用。
- 2026-08-24 起，`WorldMapNightOverlay` 使用 `ProjectSettings/TagManager.asset` 中位于 `Default` 之后的专用 Sorting Layer；重新运行 `WorldMapDayNightAuthoring` 不会回退到 Default，夜幕可覆盖两名桌宠和花朵。

### 2026-08-18 WorldMap 当地天气切换
- 运行时入口：`Assets/_Project/Scripts/Modules/WorldMap/WorldMapWeatherController.cs`、`WorldMapWeatherService.cs`、`OpenMeteoWeatherProvider.cs`；通过 `IWeatherProvider` 隔离网络请求，WMO weather code 归类为晴天或雨天。
- Scene 作者化入口：`Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapWeatherAuthoring.cs`；天气美术统一从 `Assets/_Project/Art/WorldMap/weather/` 获取，保存 `WorldMapWeatherRainOverlay` 的 `rain.png`、`WorldMapWeatherClouds` 的 `云层.jpg` 和 `WorldMapWeatherStars` 的 `星星.PNG`。云层使用 `WorldMapCloudColorKey.mat` 去除 JPG 白底，晴天在专用资源到位前使用场景底图，不再保留旧目录引用。
- 默认配置为上海经纬度 `31.2304, 121.4737`、`timezone=auto`、30 分钟刷新；没有定位服务时这是可替换占位值。请求失败保留最近成功状态，首次失败使用晴天，避免断网时场景消失。
- 运行时只切换已作者化 SpriteRenderer 的启用状态；不创建最终 GameObject、Sprite 或 UI。昼夜 `WorldMapNightOverlay` 仍由 `IGameClock` 独立控制。

### 2026-09-04 WorldMap 环境动画

- `Assets/_Project/Scripts/Modules/WorldMap/WorldMapAmbientAnimationController.cs` 挂在 `WorldMap_Main/_SceneRoot`，其云层、花朵根节点和树木引用均序列化保存。
- `Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapAmbientAnimationAuthoring.cs` 绑定 `WorldMapWeatherClouds`、`WorldMapPlacedFlowers` 以及许愿树/大树 2～5，并按 Sprite 边界底部计算树木局部根点。
- 运行时只改已有 Transform 的 X 或 Z 旋转；单花使用确定性相位，花丛不旋转，树木底部根点保持固定。

### 2026-08-06 WorldMap 桌宠数字键动画调试
- 运行时：`Assets/_Project/Scripts/Modules/WorldMap/WorldMapPetAnimationTriggerController.cs`，挂在 `WorldMap_Main.unity/_SceneRoot`，只做当前联调用的数字键触发。
- 作者化：`Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapPetAnimationTriggerAuthoring.cs`；会删除旧的 `WorldMapAnimationTriggers` 及五个临时点位，并把数字映射保存到 Scene。
- 数字映射：`1` Angel `Outdoor_Sit`、`2` Angel `Outdoor_Pray`、`3` Angel `Outdoor_Happy`、`4` Angel `Outdoor_Water`、`5` Devil `Outdoor_Sleep`、`6` Devil `Outdoor_Cast`、`7` Devil `Outdoor_Proud`。
- 天使 `Outdoor_Sit`、`Outdoor_Pray` 使用 `Assets/_Project/Art/WorldMap/pets/天使室外/坐地` 和 `祈祷` 序列帧，并绑定到 `Assets/_Project/Animations/WorldMap/Pet/WorldMap_Angel.controller`；这套资源只供 WorldMap 使用。
- 数字触发结束后恢复普通 Idle / Move；自动巡航、标牌/区域/苹果树触发条件仍待后续策划确认。
- 数字触发时由 `PetController.SetExternalMovementLock` 暂停当前桌宠的输入、随机漫游和刚体速度，另一只桌宠不受影响。

### 2026-08-05 WorldMap 可交互场景物体
- 运行时反馈组件：`Assets/_Project/Scripts/Modules/WorldMap/WorldMapInteractiveObjectFeedback.cs`。
- 点击入口：`Assets/_Project/Scripts/Modules/WorldMap/ClickableSceneObject.cs`；具体业务通过序列化 `UnityEvent` 后续接入。
- 返回公寓入口：`Assets/_Project/Scripts/Modules/WorldMap/CabinReturnPortal.cs`，仍通过 `ISceneFlowService` 加载 `SceneId.Apartment`。
- 场景作者化：`Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapInteractiveObjectAuthoring.cs`，由 `AutoSetup` 版本 31 调用。
- 当前目标对象：`WorldMap_Main.unity` 中的 `室内`、`邮箱`、`大树 1`～`大树 5`；缩放参数和悬停组件均应直接保存在 Scene 中，点击入口暂不包含大树 1。

#### 苹果树交互轮廓（2026-08-24）

- `WorldMapInteractiveObjectAuthoring` 对 `大树 2`～`大树 5` 在编辑器阶段从 Sprite 透明度生成并保存 `PolygonCollider2D`，替换根节点旧 `BoxCollider2D`；透明区域不会再成为点击范围。
- 轮廓只在作者化时生成，运行时不 `AddComponent` 或动态替换碰撞体；`WorldMapInteractiveObjectFeedback`、`ClickableSceneObject` 和 `AppleTreeInteractable` 继续读取同一个根节点碰撞体。
- `大树 1` 仍保留原有 Scene 碰撞体用于悬停反馈，但没有苹果树或通用点击入口。若更换树 Sprite，需要重新运行作者化入口以重新保存轮廓。

### 2026-08-06 WorldMap 双宠动画调整场景
- 专用场景：`Assets/_Project/Scenes/WorldMap/WorldMap_PetAnimationPreview.unity`。
- 场景内容：仅有一个 `_SceneRoot` 根对象，其下为 `Main Camera`、`Pet_Angel`、`Pet_Devil`；两只桌宠使用 `SpriteRenderer + Animator`，不挂公寓桌宠的视觉资源。
- 共享动画资源：`Assets/_Project/Animations/WorldMap/Pet/WorldMap_Angel.controller`、`WorldMap_Devil.controller` 及其引用的 `.anim`；预览场景和 `WorldMap_Main.unity` 直接引用同一份资源，不复制控制器或 Clip。
- 作者化入口：`Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapPetAnimationPreviewAuthoring.cs`，菜单为 `Tools/Gemini-Lab/WorldMap/Create Pet Animation Preview Scene`。
- 使用约束：应在共享 `.anim` / `.controller` 上调整动画以同步室外主场景；只调整预览场景的 Transform 或 SpriteRenderer 不会自动同步到 `WorldMap_Main`。

### 2026-08-18 每日小结邮箱与自由行走
- 每日小结数据与接口：`Assets/_Project/Scripts/Modules/EmotionGarden/EmotionFlowerModels.cs`、`IEmotionGardenService.cs`、`EmotionGardenService.cs`；存档字段为 `DailySummaries`，兼容旧存档缺失字段。
- 提交后的即时保存由 `Assets/_Project/Scripts/Modules/Persistence/PersistenceBootstrap.cs` 监听 `EmotionFlowerSubmittedEvent` 并串行调用 `ISaveCoordinator.SaveAsync("autosave")`；`GeminiLab.Modules.Persistence.asmdef` 因此引用 EmotionGarden 模块。
- 邮箱 UI 运行时脚本：`Assets/_Project/Scripts/Modules/HubUI/Panels/DailySummaryMailboxPanel.cs`；Scene 作者化入口：`Assets/_Project/Scripts/Editor/SceneBootstrap/DailySummaryMailboxAuthoring.cs`；`WorldMap_Main.unity` Canvas 下保存 `Panel_DailySummaryMailbox`、`DailySummaryContent` 与隐藏 `WorldMapDailySummaryMailboxOpenTarget`。
- `WorldMapDailySummaryMailboxOpenTarget.PanelOpenButton._panelId` 的实际序列化值必须是 `29`（`PanelId.DailySummaryMailbox`）；`Apartment_Main.unity` 的旧 `MailboxButton` 与同名面板保持 inactive，不再作为入口。
- WorldMap 自由行走作者化入口：`Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapOutdoorPetAnimationAuthoring.cs`；两个桌宠在 `WorldMap_Main.unity` 保存 `RandomWander`，动画仍复用 `Animations/WorldMap/Pet/WorldMap_Angel.controller` 与 `WorldMap_Devil.controller`。
- `AutoSetup` 当前作者化版本为 66；运行时脚本不得创建最终 UI、Sprite 或 AnimatorController。
### 2026-08-20 Apartment 遗留物系统
- 业务模块：`Assets/_Project/Scripts/Modules/ApartmentKeepsake/`；UI Presenter 与视口输入桥在 `Assets/_Project/Scripts/Modules/HubUI/ApartmentKeepsakePresenter.cs`、`ApartmentViewportInputBridge.cs`。
- Scene 作者化入口：`Assets/_Project/Scripts/Editor/SceneBootstrap/ApartmentKeepsakeAuthoring.cs`；目标场景 `Assets/_Project/Scenes/Apartment/Apartment_Main.unity`。
- 关键 Scene 节点：`ArtGenerated/ApartmentKeepsakeWorldPresentation`、`UI_Sidebar/ApartmentKeepsakeOverlay`、`KeepsakeDetailPopup`、`KeepsakeGiftCollectionPanel`、`KeepsakeGiftCollectionButton`。详情弹窗使用 Canvas sorting order 103，避免被现有侧栏遮挡。
- 规则与存档由 Service 负责；运行时 Presenter 不创建 GameObject、不写入最终 Sprite，仅切换作者化节点、填充 TMP 和更新按钮/槽位状态。
### 2026-08-21 AI diary visual resources

- `Assets/_Project/Art/WorldMap/AI_diary/` supplies the saved board, close button, tabs, notes, summary and character-card Sprites used by `WorldMap_Main.unity`.
- `DailySummaryMailboxAuthoring` authors `DateButton`, `InputButton`, five target buttons and five matching preview views. `DailySummaryDetailPopup` only changes active state and text at runtime.
- The outdoor mailbox remains the sole entry; the fixed `弹窗1.png` image is not used as popup content.
### 2026-08-22 AI diary reference layout calibration

- `DailySummaryMailboxAuthoring` now saves the reference-driven layout for `Title`, `DateButton`, `InputButton`, `AngelNoteButton`, `SummaryButton`, `DevilNoteButton`, `AngelCardButton`, `DevilCardButton` and `CloseButton` under `WorldMap_Main.unity/Canvas/Panel_DailySummaryMailbox/DailySummaryContent`.
- The authored title is `AI每日小结`; the empty input state is the multi-line reminder to leave a sentence in the garden. Runtime still replaces these values with the persisted daily summary when data exists.
- Button transition tint is disabled so the Sprite colors stay faithful to the AI diary art. The TMP font asset may gain glyph entries when Unity imports new Chinese text; this is expected and should remain serialized.

### 2026-08-22 AI diary date list and popup

- `Assets/_Project/Scripts/Modules/HubUI/Panels/DailySummaryDateOption.cs` is the Scene-authored date-list item. Its Button, date TMP and selected/unselected visual references are serialized in `WorldMap_Main.unity`.
- `DateListViewport`, `DateListContent`, `DateButton`, `InputButton` and `DateOption_03`~`DateOption_14` form the scrollable history list; the panel binds them through `_dateOptions`.
- `PopupButton` uses `Assets/_Project/Art/WorldMap/AI_diary/弹窗.png`, while `DailySummaryDetailPopup.PopupView` uses `弹窗1.png`. `PopupContent` has no solid Image background.
- `IEmotionGardenService.GetDailySummaryDates()` is a read-only query over persisted `DailySummaries`; selecting a date never changes the save model.

### 2026-08-23 AI diary popup text overlay

- `WorldMap_Main.unity/PopupContent/PopupBodyText` is a Scene-authored TMP overlay centered on the enlarged note resource. Keep it above `SummaryView`, `AngelNoteView` and `DevilNoteView` in the hierarchy.
- `DailySummaryDetailPopup` hides the overlay for `DetailKind.Popup`, because `弹窗1.png` already contains its own text.

### 2026-08-24 Pet runtime save merge

- Runtime pet persistence is implemented by `Assets/_Project/Scripts/Modules/Pet/PetRuntimeSaveService.cs` and registered through the existing pet persistence bootstrap.
- The v2 JSON schema contains pet mood, energy, satiety, relation, runtime fields, and `savedAtUtcTicks`. Restore applies the offline Mood regression rule while preserving Energy/Satiety and restoring Relation.
- Legacy v1 JSON remains supported. It does not apply offline regression and does not overwrite the current Relation when the field is absent.

### 2026-09-03 WorldMap 视觉位置修复

- WorldMap PSD 子物体继续由 Scene 中的本地 Transform 保持相对位置；`BaselineItem` 的绑定偏移只记录世界坐标差值，不会把父节点层级误当成基线高度。
- 已保存花朵恢复时统一使用解析出的基线 Y；`FlowerPlacementBounds` 限制花朵锚点必须落在草地区域。
- `WorldMap_Main` 的活动相机按天空与地面包围范围校准，Scene 与 Play 应保持同一取景。

### 2026-09-03 WorldMap PSD 相对位置修复复核

- `WorldMap_Main.unity` 的 PSD 子物体局部 Transform 是最终视觉来源；不得把世界 Y 直接写入挂在 `室外背景` 下的子物体 `localPosition.y`。
- 本次恢复天空、地面、桥、树、花丛、桌宠等 16 个误写的局部 Y，并将活动相机恢复为场景原始取景；花朵仍由已保存的基线 Y 恢复。

### WorldMap 固定基线（2026-09-01）

- 基线定义脚本：`Assets/_Project/Scripts/Modules/WorldMap/WorldMapBaselineDefinition.cs`。
- `WorldMap_Main.unity/WorldMapPlacedFlowers/FlowerPlacementGrid` 固定保存十一条 `BaselineLine_*`：天空、星星、云、树木后排、树木前排、地面、四条花丛层和桌宠层；定义节点独立于绑定物体。
- 云和星星已有独立美术节点，直接绑定 `Environment_Clouds` / `Environment_Stars`；星星由 `WorldMapDayNightController` 按 06:00–18:00 / 18:00–06:00 显隐。
- 花朵放置层仅引用四条白线；作者化脚本不再遍历全部 `BaselineItem` 创建基线。
- Scene 调整工具：`Assets/_Project/Scripts/Editor/Tools/WorldMapFlowerBaselineToolWindow.cs`，菜单 `Tools/Gemini-Lab/WorldMap/场景基线`；可拖拽 Y 轴、按 RenderOrder 相对值从大到小查看和编辑，或用固定点击式前移/后移交换相邻基线。RenderOrder 只比较大小，不代表基线条数。
- `WorldMapBaselineDefinition` 负责统一保存基线身份、Y、X 范围、放置许可和 RenderOrder；`SlotIndex` 不因排序调整而改变。`BaselineItem` 只序列化对定义的引用及自动维护的基线偏移保护字段，WorldMap 保留每个物体原有轴心偏移；物体自身只允许沿 X 移动，只有基线工具能带动其 Y。Inspector 与基线工具编辑同一份参数，不再存在单物体 `_sortingOrder` 覆盖值。

### WorldMap 许愿系统（2026-09-04）

- `WorldMapWishService` 位于 `Assets/_Project/Scripts/Modules/WorldMap/`，负责愿望状态、12 个显示槽位和 PlayerPrefs JSON 存档。
- `WorldMapWishSystemController` 连接 `WorldMapWishSystemPanel` 下的作者化视图；运行时不得创建最终 UI 节点。
- “全部”记忆列表显示 Active、Fulfilled、Archived 全部历史记录；Archived 不再占用活动星位。
- `WorldMapWishTreeInteractable` 只挂在场景对象 `许愿树` 上，点击入口与苹果树逻辑分离。
- 主面板 `MainView/WishStarSlot_00..11` 是作者化星位，位置限制在背景左上角插画许愿树内；旧世界空间星位容器保持隐藏。
- `WorldMapWishSystemAuthoring` 菜单：`Tools/Gemini-Lab/WorldMap/Setup Wish System`；`item_button.png` 打开单一详情手册，右侧列表可滚轮，选中行使用 `item_selected.png`。

### WorldMap 愿望 UI 流程修正（2026-09-05）

- 控制器不再为星星绑定详情监听；详情只由主面板 `item_button.png` 进入。
- 详情列表和左侧详情内容均为 Scene 作者化节点，运行时只切换显示状态、填充文本和处理选中事件。

## WorldMap 天气云层与单花环境动画（2026-09-05）

- 天气美术资源统一来自 `Assets/_Project/Art/WorldMap/weather/`；`WorldMapClouds_Alpha.png` 为不修改源 JPG 的派生透明云层 Sprite。
- `WorldMapWeatherAuthoring.cs` 负责把云层、星星和雨层引用保存到 `WorldMap_Main.unity`；云层使用 `Sprites-Default`，不引用 `WorldMapCloudColorKey.mat`。
- `WorldMapAmbientAnimationAuthoring.cs` 只保存云层、单花根节点和树绑定；`WorldMapAmbientAnimationController.cs` 运行时移动云层、旋转 `_Single` 单花，`_Cluster` 花丛不参与旋转。
### 2026-09-05 WorldMap 双宠动画触发状态机
- 运行时入口：`Assets/_Project/Scripts/Modules/WorldMap/WorldMapPetAnimationTriggerController.cs`，挂在 `WorldMap_Main.unity/_SceneRoot`；负责范围触发、F 键触发、摆花事件和数字键调试，不负责 Idle/Move 基础状态。
- 编辑器作者化：`Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapPetAnimationTriggerAuthoring.cs`；菜单为 `Tools/Gemini-Lab/WorldMap/Setup Outdoor Pet Animation Triggers`，会序列化苹果树、许愿树与已命名标牌引用，不创建占位物体。
- 共享动画来源：`Assets/_Project/Animations/WorldMap/Pet/WorldMap_Angel.controller` 与 `WorldMap_Devil.controller` 及其 `Outdoor_*` 状态引用的现有 Clip。动画触发脚本只调用状态名，不覆盖 Controller 或 Sprite 引用。
### 2026-09-05 WorldMap 苹果树掉落交互

- `AppleService` 的 `TryBeginHarvest`/ `TryCollectHarvest` 将当前缓存总量保留为一个可持久化批次，逐个领取后才增加苹果余额；旧 `ShakeTree` API 保持兼容。
- `AppleTreeDropController` 与 `AppleDropSlot` 只切换 Scene 中已作者化的 3 个掉落槽位，使用 `Assets/_Project/Art/WorldMap/苹果云背景补充/apple.png`；运行时不创建视觉对象。
- `WorldMapAppleTreeAuthoring` 为「大树 2」～「大树 5」中实际存在的苹果树保存槽位、碰撞体、收获文字和根部快速晃动参数；「大树 1」与「许愿树」排除。

### WorldMap AI 情绪花园（2026-09-05）

- AI 契约位于 `Assets/_Project/Scripts/Modules/EmotionGarden/IEmotionGardenAiProvider.cs` 与 `EmotionGardenAiModels.cs`；实际 OpenAI 兼容实现位于 `Assets/_Project/Scripts/Modules/AI/EmotionGardenAiProvider.cs`，启动注册位于 `EmotionGardenAiRuntimeBootstrap.cs`。
- `EmotionGardenService` 是 AI 结果与存档的唯一写入入口；面板只负责调用异步接口和显示文本，不直接处理网络或持久化。
- `EmotionInputPanelStub` 提交情绪后生成 AI 花朵与每日总结；`WeeklyGardenPanelStub` 从花朵记录显示关键词、花语；`DailySummaryMailboxPanel` 按日期显示 summary、angelNote、devilNote。

### WorldMap 室外新手指引（2026-09-06）

- 场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`，节点位于 `Canvas/Panel_OutdoorTutorial`。
- 作者化脚本：`Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapOutdoorTutorialAuthoring.cs`，菜单为 `Tools/Gemini-Lab/WorldMap/Author Outdoor Tutorial`。脚本只增量维护新手指引子树。
- 页面资源：`Assets/_Project/Art/新手引导/outdoor/intro.png`、`outdoor1.png`～`outdoor6.png`；按钮资源：`Assets/_Project/Art/新手引导/left.png`、`right.png`、`outdoor/close.png`。
- 运行时分页组件：`Assets/_Project/Scripts/Modules/HubUI/Panels/SceneAuthoredImageVariantView.cs`，只操作 Scene 中的页面节点和按钮事件。
### WorldMap 室外点击路由（2026-09-07）

- 点击契约位于 `Assets/_Project/Scripts/Core/ServiceLocator.cs` 的 `IWorldMapSceneClickTarget`，以避免 Apple 与 WorldMap 程序集互相引用；路由器位于 `Assets/_Project/Scripts/Modules/WorldMap/WorldMapSceneInteractionRouter.cs`。
- `Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity` 的 `_SceneRoot` 路由器显式保存邮箱、标牌、桌宠、许愿树、三棵苹果树和九个掉落槽引用。三棵苹果树保留 PolygonCollider2D，掉落槽保留 Scene `CollectionText` 引用。
- 本阶段不修改动画资源、室内资源、花朵基线或其他视觉对象；Unity Play 验证仍由人工执行。
### 2026-09-08 WorldMap 点击命中区域

- `ClickableSceneObject`、`WorldMapGardenZone`、`WorldMapPetInteractionController` 的点击命中必须使用同一物体上的 Collider2D；`SpriteRenderer.bounds` 只可用于视觉排序或非点击辅助计算。
- 本次只移除三处 bounds-first 命中分支，不改 `WorldMap_Main.unity` 中 Collider2D 的作者化几何，最终覆盖范围由 Scene/Play 人工确认。

### WorldMap 苹果服务入口（2026-09-08）

- `Assets/_Project/Scripts/Modules/Apple/AppleRuntimeBootstrap.cs` 提供幂等的 `EnsureRegistered()`：优先复用已有 `IAppleService`，直启 WorldMap 且已有 `IGameClock` 时才注册现有 `AppleService`，并同步到 `IPersistentServiceRegistry`。
- `Assets/_Project/Scripts/Modules/Apple/AppleTreeDropController.cs` 在开始树收获前确保服务入口可用；它不改变 `TryBeginHarvest` 的成熟批次规则，也不改变 `AppleDropSlot` 成功领取后增加货币的路径。
- Boot 正常启动和 WorldMap 直启都应使用同一 `IAppleService`；树的 Router、Collider2D、Scene 掉落槽和 CollectionText 引用仍以 `WorldMap_Main.unity` 为准。

### WorldMap 苹果收集系统文件边界（2026-09-08）

- 业务状态：`Assets/_Project/Scripts/Modules/Apple/AppleService.cs`、`IAppleService.cs`、`AppleModels.cs`。
- 运行时入口：`AppleRuntimeBootstrap.cs`；树点击与掉落表现：`AppleTreeInteractable.cs`、`AppleTreeDropController.cs`、`AppleDropSlot.cs`、`AppleTreeFeedback.cs`。
- Scene 唯一视觉来源：`WorldMap_Main.unity` 中 `WorldMapAppleDrops` 下的 9 个苹果槽位、9 个 `CollectionText`，以及 3 个树下 `StatusText`。这些节点必须保持序列化引用，运行时只切换显隐和文本。
- 反馈文字使用 `Assets/_Project/Art/Fonts/NotoSansSC_SDF.asset` 及其材质；不要把旧 Liberation 字体实例材质重新绑定到这些节点。

### WorldMap 云层和室内入口（2026-09-08）

- `Assets/_Project/Scripts/Modules/WorldMap/WorldMapAmbientAnimationController.cs` 读取 Scene 序列化的 `_cloudMoveMinX` / `_cloudMoveMaxX`，当前对应 `BaselineLine_云` 的完整室外范围。
- `Assets/_Project/Scripts/Modules/WorldMap/CabinReturnPortal.cs` 是外场 `室内` 物体的 Collider2D 点击目标；`WorldMap_Main.unity/_SceneRoot` 的 `WorldMapSceneInteractionRouter._targets` 显式引用其现有组件。
- `CabinReturnPortal` 只负责从 WorldMap 进入 `Apartment_Main`，不改变 Apartment 场景内容。

### WorldMap 云层范围与移速校正（2026-09-08）

- `WorldMapAmbientAnimationController._skyRenderer` 显式引用 `WorldMap_Main.unity` 中 `天空` 的 SpriteRenderer；云层范围不再依赖 `BaselineLine_云` 或手写最小/最大 X。
- 控制器根据天空与云层的实际渲染边界换算中心点和半跨度，并使用旧版 `0.12` 参数保留中心加正弦、到端点折返的运动方式。
- `WorldMap_Main.unity` 仍是天空、云层和控制器序列化引用的唯一事实源；不创建运行时视觉对象。

### WorldMap 云层速度再次校正（2026-09-08）

- 只将 `WorldMapAmbientAnimationController._cloudMoveSpeed` 和 `WorldMap_Main.unity` 中的序列化值从 `0.12` 改为 `0.06`。
- 天空派生移动范围、中心加正弦位移以及到端点折返方式均保持不变。
