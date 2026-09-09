# Gemini-Lab Project File Guide

## 2026-09-09 室内小门入口

- 需求、操作和验证：`docs/indoor-door-interaction.md`。
- 对话数据：`Assets/_Project/ScriptableObjects/IndoorDoorDialogues.asset`；模型 `Scripts/Modules/Pet/Social/IndoorDoorDialogueCatalog.cs`。
- 气泡视图：`Assets/_Project/Scripts/Modules/HubUI/PetDialogueBubble.cs`；由 `PetController.VisiblePosition` 提供实际姿态位置。升级菜单 `Tools/Gemini-Lab/Apartment/Author Door Speech Bubbles`。
- 场景交互：`Scripts/Modules/HubUI/ApartmentDoorInteraction.cs`；作者化 `Scripts/Editor/SceneBootstrap/ApartmentDoorAuthoring.cs`。
- 保存的门 Mesh/材质及脚底物理材质：`Assets/_Project/Settings/IndoorDoor/`；回归 `Assets/_Project/Tests/EditMode/IndoorDoorInteractionTests.cs`。


Updated: 2026-09-04

## 公寓移动维护入口（2026-09-09）

- `docs/apartment-movement.md`：实际实现、站立点作者化、验证与限制。
- `Assets/_Project/Scripts/Modules/Pet/ApartmentPetMovement.cs`：物理运动与障碍查询；`ApartmentWalkPath.cs`：路径规划和扫掠。
- `Assets/_Project/Scripts/Editor/SceneBootstrap/ApartmentMovementAuthoring.cs`：两宠移动引用与缺失站立点绑定。
- `Assets/_Project/Tests/EditMode/ApartmentWalkPathTests.cs`：路径、边界、扫掠和脚底偏移回归。

## 遗留物维护入口（2026-09-09）

- `docs/room-relic-system.md`：实际规则、存档 v1→v2 兼容、会话生命周期、固定槽位和作者化边界。
- `Assets/_Project/Tests/EditMode/RoomRelicIntegrationTests.cs`：槽位、点击消费和 Bootstrap 接入回归。
- `Assets/_Project/Scripts/Core/UI/UIRouter.cs`：遗留物弹窗叠加当前页面，关闭回到原页面；相应回归包含在 RoomRelicIntegrationTests 中。
- `Assets/_Project/Tests/EditMode/DeferredSaveRestoreTests.cs`：模块晚注册及等待期间保存回归。
- `Assets/_Project/Scripts/Core/Persistence/IPersistentServiceRegistry.cs` 的 `Registered` 事件与 `Assets/_Project/Scripts/Modules/Persistence/SaveCoordinator.cs` 配合补恢复尚未注册的遗留物模块。

## 室内遗留物系统（2026-09-03）

- 运行时模块：`Assets/_Project/Scripts/Modules/RoomRelic/`；`RoomRelicService` 实现 `IRoomRelicService` 与 `IPersistentService`，存档 key 为 `room_relic`。
- 场景作者化：`Assets/_Project/Scripts/Editor/SceneBootstrap/ApartmentRoomRelicAuthoring.cs`，菜单 `Tools/Gemini-Lab/Apartment/Author Room Relic`；`AutoSetup` 版本 66 会调用。已执行并落盘到 `Apartment_Main.unity`。
- 遗物素材：`Assets/_Project/Art/Sprites/Relic/`，由 `RoomRelicData.roomVisualKey` 记录 GUID；8 张已提供素材绑定到场景变体。
- 房间进入：复用 `PetMovementBounds`（AngelRoom）和 `PetMovementBounds_Devil`（DevilRoom），由 `RoomRelicEntryTrigger` 调用服务。
- UI 弹窗：`RoomNotePopup`、`RoomRelicDetailPopup`、`RoomGiftObtainedPopup` 对应 `PanelId` 29~31；详情/赠礼弹窗通过 `RoomRelicView` 变体视图显示 icon（运行时不写 Sprite）。
- 赠礼素材：南瓜糖果/速写（恶魔→天使）、羽毛书签/小星星吊坠（天使→恶魔）；速写、小星星吊坠仍缺素材占位。
- 测试：`Assets/_Project/Tests/EditMode/RoomRelicServiceTests.cs`。

## 苹果资源系统（2026-08-14）

- 运行时模块：`Assets/_Project/Scripts/Modules/Apple/`；`AppleService` 实现 `IAppleService` 与 `IPersistentService`，存档 key 为 `apple`。
- 规则：新档 20 个苹果；每棵大树每 6 小时生成 1 个、最多缓存 3 个；时间戳和待领取数量随存档保存。时间来源必须是 `IGameClock`。
- WorldMap 交互：`AppleTreeInteractable` 挂在现有「大树 1」～「大树 5」，`WorldMapAppleTreeAuthoring` 只写入树 ID 和组件，不创建运行时视觉。点击树领取缓存苹果。
- 消费入口：`GachaService` 使用苹果支付单抽/五连（1/5）；`TarotService.CreateSession` 使用 1 个苹果开启会话，余额不足时不进入有效选牌。
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
  - 当前用于临时停用并备份 4 个 Unity MCP 包
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
15. `2026-06-02` 同日，项目又把 4 个 Unity MCP 包临时从 `Packages/` 移到了 `PackageBackups/MCP-disabled-2026-06-02/`，并从 `Packages/manifest.json` 取消引用；当前保留不动的是 `Packages/SkillsForUnity`。
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
- 网格配置：由 `WorldMapFlowerPlacementController` 读取 `garden/中景/花丛.png` 的 Sprite 完整尺寸得到二维 `Vector2` 单元（当前为 `4.01 x 2.24` Unity 单位），再结合网格原点和摆放区域生成完整 X/Y 网格线；不绑定或读取图鉴花朵资源作为显示。
- `2026-08-10` 已完成侧边栏版本的实际落盘，并于 `2026-08-11` 修正为参考图对应的右侧布局：`Canvas/Btn_FlowerPlacement` 位于右下角，`Canvas/FlowerPlacementPanel` 锚定右侧并保留 `UIBoard.png` 原始 `498 x 899` 尺寸；`FlowerSidebarViewport`、`FlowerSidebarContent`、`FlowerList/FlowerOption_00...17`、`SynthesisHintBubble`、`WorldMapPlacedFlowers/FlowerPlacementGrid`、`FlowerPlacementPreview`、`PlacementSlot_00...31` 与 `FlowerPlacementBounds` 均存在于 `WorldMap_Main.unity`。旧底部库存节点不参与当前滚动列表，遗留顶层 `FlowerPlacementStatusBar` 已清理。
- `2026-08-11` 侧栏展开已改为确定性的手风琴布局：`FlowerList` 直接作为 `ScrollRect.content` 并管理 18 个 `FlowerOption`；其 `VerticalLayoutGroup.childControlHeight` 已启用，因此每个条目预置的 `LayoutElement` 会以 `73 / 320` 的收起/展开高度真实控制下方条目位移。详情页位于选中标题下方，不会再被后续列表元素遮挡；控制器会保持被点击标题在视窗中的位置，避免从一个条目切换到另一个条目时出现不规则跳动。
- `2026-08-12` 侧栏视窗已保存为左右 34、顶部 132、底部 56 的真实拉伸边距，列表起点固定在标题下方并受 `RectMask2D` 裁剪。可通过 `Tools/Gemini-Lab/WorldMap/Author Flower Placement` 单独重新作者化该系统，不触发其他 WorldMap 补丁。
- `2026-08-13` 摆放系统读取 `BaselineItem` 的实际基线与排序层：同层单格不可重叠，跨层可重叠；内部吸附在相邻层之间半格错位，但 `FlowerPlacementGrid` 运行时保持隐藏。作者化只收录摆放区域内的基线；花丛改为一花丛一格，尺寸取场景 `花丛 3` 的碰撞体基准（约 `3.99 x 2.22`），运行时不生成视觉节点，并清理槽位重复旧组件。
- `2026-08-12` 摆放与每周培育共享 `EmotionGardenService` 内的持久化 `PlacementFlowerInventory`：开花增加单花，单花/花丛摆放消耗对应库存，3 个单花主动合成 1 个花丛；存档版本 3 会从旧档的已开花数据一次性迁移库存。版本 4 另存 `PlacedFlowers`，成功摆放立即 autosave，重启后恢复槽位/坐标且不重复扣库存；自由摆放单花和花丛均不添加土壤。若当前存档没有 `PlacedFlowers`，不会把仅有的开花记录臆测成历史坐标。
- 侧栏 UI 绑定 `Assets/_Project/Art/WorldMap/arrange`；花种/单花/花丛绑定 `Assets/_Project/Art/WorldMap/花朵图鉴/花朵`、`花枝`、`花丛`，不是 `arrange.png` 裁剪图。网格材质为 `arrange/PlacementGrid.mat`，网格线仅作为作者化/调试资源保存，运行时不显示。
- `FlowerPlacementBounds` 默认是 `36 x 8.96` 的禁用 BoxCollider2D，仅存放草地摆放区域数据，可在 Inspector 调整。运行时控制器只切换预置显示、移动空闲 `PlacementSlot` 并写入摆放元数据；不负责创建最终视觉对象。
- `2026-08-14` `WorldMapPlacementSlot` / `WorldMapPlacedFlower` 已拆为独立脚本并使用稳定 GUID；运行时会校验 32 个槽位和每槽 36 个 `PlacedVisual_*` 绑定。点击提交期间暂缓同步摆放恢复事件，避免事件重入清空刚显示的槽位；槽位以显式占用状态参与同层冲突判定。若要验收鼠标端到端，必须使用仍有单花或花丛库存的 Play 存档。
- `2026-08-14` 花朵遮挡修正：`WorldMapFlowerPlacementController` 使用 `Default` Sorting Layer；预览、已摆放花朵和恢复路径与桌宠共享 `BaselineItem.SortingOrder` 主层级，并按基线 Y 二次排序，完全同线时桌宠提高 1 个排序单位。`WorldMapPlacementSlot` 会递归同步花丛显示树内的所有 SpriteRenderer。AutoSetup 版本 63 已给 WorldMap 的两个桌宠根对象保存 `BaselineItem`，基线取碰撞体底部、`solidCollider=true` 并排除出可种植层。

### 2026-08-05 WorldMap 昼夜切换
- `Assets/_Project/Scripts/Modules/WorldMap/WorldMapDayNightController.cs`：WorldMap 按 `IGameClock.Now` 切换夜幕。
- `Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapDayNightAuthoring.cs`：将现有夜幕 Sprite 作者化为 `WorldMapNightOverlay`，设置覆盖范围、排序和当前本地时间初始状态。
- `WorldMap_Main.unity` 已保存 `WorldMapNightOverlay`，引用 `天气（最上层）/夜幕.png`，默认 06:00–18:00 为白天。

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
- 当前目标对象：`WorldMap_Main.unity` 中的 `室内`、`邮箱`、`大树 1`～`大树 5`；缩放参数和组件均应直接保存在 Scene 中。

### 2026-08-06 WorldMap 双宠动画调整场景
- 专用场景：`Assets/_Project/Scenes/WorldMap/WorldMap_PetAnimationPreview.unity`。
- 场景内容：仅有一个 `_SceneRoot` 根对象，其下为 `Main Camera`、`Pet_Angel`、`Pet_Devil`；两只桌宠使用 `SpriteRenderer + Animator`，不挂公寓桌宠的视觉资源。
- 共享动画资源：`Assets/_Project/Animations/WorldMap/Pet/WorldMap_Angel.controller`、`WorldMap_Devil.controller` 及其引用的 `.anim`；预览场景和 `WorldMap_Main.unity` 直接引用同一份资源，不复制控制器或 Clip。
- 作者化入口：`Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapPetAnimationPreviewAuthoring.cs`，菜单为 `Tools/Gemini-Lab/WorldMap/Create Pet Animation Preview Scene`。
- 使用约束：应在共享 `.anim` / `.controller` 上调整动画以同步室外主场景；只调整预览场景的 Transform 或 SpriteRenderer 不会自动同步到 `WorldMap_Main`。

- 小门通行升级仍由 `ApartmentDoorAuthoring.cs` 提供，新增菜单 `Upgrade Door Passage`；`ApartmentPetMovement._connectedRoom` 连接另一侧边界。遗留物点击修复位于 `RoomRelicView.Apply` / `RoomRelicInteraction.Open`。

- 页面家具入口：`docs/apartment-page-furniture.md`；`Scripts/Modules/HubUI/FurniturePageLink.cs`、`Scripts/Editor/SceneBootstrap/ApartmentPageFurnitureAuthoring.cs`；Prefab 在 `Assets/_Project/Prefabs/Furniture/Leisure/CrystalBall.prefab` 与 `Gacha.prefab`。

- 普通点击气泡遮挡入口：`Scripts/Modules/Pet/PetClickReactionController.cs` 的 `_controlIndicator` / `ApplyBubbleSorting`；Apartment 场景显式绑定两宠箭头，首次作者化由 `ApartmentDoorAuthoring` 同步绑定。
