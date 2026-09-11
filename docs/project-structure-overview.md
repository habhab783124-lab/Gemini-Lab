# Gemini-Lab 项目结构总览

## 2026-09-11 宠物交互视觉作者化

- 两个 Pet Prefab 已包含主 Animator 和 `InteractionVisual` 子节点（SpriteRenderer / Animator 默认禁用）；Apartment 的宠物实例继承该结构，WorldMap 两只宠物也已保存对应节点。
- PetController 的交互视觉引用由 Inspector 保存；运行时只切换可见状态、播放已绑定动画并应用家具请求中的姿态。子节点会维持交互世界坐标，避免隐藏身体的移动/缩放改变交互画面。
- 可用 `Tools/Gemini-Lab/Pet/Author Visual Bindings` 补齐当前场景绑定。批量迁移需显式调用编辑器方法，不在载入项目时执行。

## 2026-09-09 交流选项与纸条变体

`ApartmentViewportImage` 下新增 `AngelConversationChoices`、`DevilConversationChoices`，分别保存 `Chat` / `Decline` 按钮；只显示访客所在房间的一组。原双宠气泡保留。门组件显式绑定 `_angelRoom` / `_devilRoom`，交流不再按门边距离触发。`ArtGenerated/RoomRelic` 的 6 个纸条槽各有 17 个预置变体，共 102 个 ID 绑定。`Art/UI/Communication` 为四张按钮资源；所有 Sprite 和布局保存在 Scene。详见 `docs/indoor-door-interaction.md`。

## 2026-09-09 室内小门已落地

Apartment 场景中 `ArtGenerated/Environment/door` 绑定 `ApartmentDoorInteraction`，下方 `OpenDoor/Stripe0..4` 为开门形态；`ApartmentViewportHost/IndoorDoorUI` 保存操作提示，旧 `Conversation` 已移除。视口图片下 `AngelDialogueBubble` / `DevilDialogueBubble` 分别保存两只宠物的气泡，子节点 `Content/Tail` 和 `Content/Body/Text` 可在 Scene / Inspector 调整；默认隐藏 Content，交流时按顺序显示。两只宠物下新增只用于点选的 `SelectionArea`，原 `ControlIndicator` 继续显示金色箭头。内容资产为 `IndoorDoorDialogues.asset`，门边接近点复用现有 `Approach_*_门边`。详细规则与验证见 `docs/indoor-door-interaction.md`。


Updated: 2026-07-30

## 公寓移动（2026-09-09）

Apartment 两宠新增显式绑定的 `ApartmentPetMovement`，Scene 中保存 10 个 `Approach_*` 站立点，分别供天使 6 个、恶魔 4 个家具行为使用。原家具层级、姿势、Sprite 与碰撞体保留。组件 Inspector 可调房间、脚底体积引用、安全间距及障碍更新周期；详见 `docs/apartment-movement.md`。

## 遗留物系统（2026-09-09）

- 当前功能分支的 Apartment 已保存 `ArtGenerated/RoomRelic`、两房间预置物品槽与三弹窗，详见 `docs/room-relic-system.md`。
- 赠礼槽通过 `RoomRelicView._displaySlotId` 对应配置的 `desk` / `shelf`，运行时不移动布局或赋 Sprite。
- 运行时服务支持延迟读档和场景往返；编辑器作者化保留人工修改，新 `Upgrade Room Relic Bindings` 菜单仅补绑定和占位文案。
- 三类遗留物弹窗保留底层空间页；Apartment Scene 已保存纸条/详情文字颜色、字号、关闭 X 和赠礼布局。Play 点击、场景往返与存读档验证见人工验证清单。

## 苹果资源系统（2026-08-14）

## 2026-09-09 WorldMap outdoor pet facing: fixed-step single-source correction

- The WorldMap outdoor animation adapter samples horizontal Rigidbody movement in `FixedUpdate` and applies normal `Idle_Side`/`Move_Side` playback from that sample only.
- Facing changes require two consecutive physics samples in the opposite direction. The adapter does not infer normal facing from input, roaming targets, or interaction targets.
- `PetController` keeps movement, player control, roaming, and bridge traversal. Its generic side mirror is suppressed only while the explicit WorldMap animation owner is active.

## 2026-09-09 WorldMap 室外普通移动朝向单一位移来源修复

- 室外双宠普通移动只按各自 `Rigidbody2D.position.x` 的实际相邻帧位移判断左右：超过阈值向左/向右时更新朝向，停止或微小修正时保持当前朝向。
- 普通朝向不再混用玩家输入、漫游目标和实际位移回退；特殊动作期间继续由特殊动作目标朝向控制，结束后再交回普通位移朝向。
- `PetController` 的移动、玩家操控、漫游、桥面和过桥流程，以及天使/恶魔独立交互映射保持不变。

## 2026-09-09 WorldMap 室外普通移动朝向稳定性修复

- `WorldMapPetAnimationTriggerController` 继续作为室外双宠普通动画的唯一运行时播放者；实际横向位移只用于判断 `Move_Side` / `Idle_Side`，不再直接用逐帧位移差的正负切换 `flipX`。
- 玩家控制使用当前横向输入朝向，漫游使用各自 `RandomWander` 或行为目标的横向方向，并保留独立的上一次稳定朝向，避免碰撞和插值产生左右抖动。
- 特殊动作的目标朝向仍优先于普通移动朝向；移动、漫游、玩家控制、桥面 `WalkableSurface` 和过桥流程仍由 `PetController` 及原有组件负责。
- 本次不修改 WorldMap/室内 Animator、Animation Clip、关键帧、循环设置、Motion、Sprite、Scene 或 Prefab。

## 2026-09-09 WorldMap 玩家操控与漫游移动动画控制权修复

- WorldMap 室外双宠的普通动画由 `WorldMapPetAnimationTriggerController` 唯一播放：它读取每只桌宠 Rigidbody2D 的实际横向位置变化，只有真实移动才进入对应 `Move_Side`，停止后进入 `Idle_Side`。
- `PetController` 继续驱动移动、玩家输入、漫游、物理和 `WalkableSurface` 过桥；WorldMap 只通过动画控制权接口阻止通用动画写入，不改变移动核心或过桥状态。
- 运行时绕过现有 Move Any State 自身转场造成的重复重置，但不修改两个 Animator Controller、Animation Clip、关键帧、循环设置、Motion、Sprite、Scene 或 Prefab。

## 2026-09-09 WorldMap 普通移动动画职责收口

- `_SceneRoot/WorldMapPetAnimationTriggerController` 不再每帧与 `PetController` 竞争普通 `Idle/Move` Animator 状态；它只处理室外特殊动作和动作期间的移动锁，并在特殊动作释放边界做一次性 Idle 恢复。普通移动动画由每只桌宠自身的 `PetController.UpdateMovementAnimation()` 统一更新。
- WorldMap 场景中的天使和恶魔 Animator Controller、Move Clip、Scene/Prefab 序列化引用均保持原样。过桥、玩家操纵、漫游和其他室外功能继续由原有组件负责。

## 2026-09-09 WorldMap 玩家控制期间屏蔽漫游特殊动画

- 室外桌宠处于玩家控制时，`WorldMapPetAnimationTriggerController` 只保留玩家主动 F 键交互，屏蔽天使/恶魔各自的漫游特殊动画触发并清理邻近目标锁存；这保证玩家移动经过有效目标时不会被浇水、坐地、睡觉、祈祷或施法打断。
- 该修复只改变室外动画触发适配层，保留桌宠移动、玩家控制、漫游、过桥和过桥后恢复逻辑。

## 2026-09-09 WorldMap 作者化花朵区域作为唯一边界

- WorldMap 花朵摆放在 `_placementRegions` 有效时，按所选 owner 的 `FlowerPlacementRegion_Angel` / `FlowerPlacementRegion_Demon` Collider2D 判定完整 footprint；不再被旧 `FlowerPlacementBounds` 强制取交集。
- `FlowerPlacementBounds` 只为没有作者化区域的旧场景保留回退，Scene 中的区域 Collider 尺寸和序列化引用仍由作者直接调整。

## 2026-09-08 WorldMap 室外双宠动画状态机最终阶段

- `_SceneRoot/WorldMapPetAnimationTriggerController` 是室外天使与恶魔 Animator 的唯一最终仲裁入口。它只读取现有 WorldMap Animator 状态和动画资源，按实际横向位移裁决 `Idle/Move`，按明确来源触发坐地、祈祷、浇水、开心、睡觉、施法和得意。
- 特殊动作通过现有 `PetController.SetExternalMovementLock` 暂停受影响桌宠；动作结束时根据最新真实位移恢复普通状态。移动、玩家控制、漫游、桥面 `WalkableSurface` 和过桥流程仍由原脚本负责。
- 交互目标使用 `WorldMap_Main.unity` 中已有序列化引用和 Collider2D 最近点；花朵触发使用 EmotionGarden 成功记录及已占用的 WorldMap 摆花槽落脚区域。室内桌宠、动画资产和 Scene/Prefab 序列化引用不属于本阶段修改对象。
- 静态编译已通过；Unity MCP Play 验证因当前编辑器进程无响应尚未完成，不能视为用户最终实机确认。
- 室外动画适配器还负责隔离两只室外桌宠上的旧家具交互组件，防止家具绑定的 fallback 世界坐标或交互姿势把桌宠移动到白名单之外；它只对显式绑定的室外宠物生效，禁用时恢复旧组件状态。室外动画对象按桌宠分开白名单：天使为苹果树、许愿树、天使花，恶魔为苹果树、恶魔标牌、恶魔花。
- 2026-09-09：天使玩家控制模式按 F 可对最近有效天使花朵触发浇水；漫游浇水期间持续面向花朵的 Collider/落脚区域最近点，动作结束后交还普通动画状态。

## 2026-09-08 WorldMap UI 点击优先级修正

- WorldMap 点击路由在 Collider2D 命中前先读取当前指针位置的 EventSystem UI 射线结果。
- 只要命中有效的 `GraphicRaycaster` / `Graphic` 且 `raycastTarget` 开启，UI 面板就拥有最高点击优先级；透明但用于阻挡输入的作者化 Graphic 也不能让点击穿透到下方物体。
- UI 内部点击继续由按钮、输入框和面板自身处理；只有 UI 没有命中时，才进入既有 WorldMap 目标的 Collider2D 路由。本次不改变 WorldMap 业务映射。

## 2026-09-08 WorldMap 第一阶段点击与室外桌宠链路

- WorldMap 的交互目标由 `WorldMapSceneInteractionRouter` 统一裁决，业务映射必须保持为：标牌→对应情绪输入、邮箱→AI 每日总结、许愿树→许愿系统、大树 2/3/5→苹果树掉落、桌宠→对应玩家控制权。
- 点击命中只读取目标自身作者化 `Collider2D`；桌宠使用独立整物体点击 Collider，不能复用只覆盖落脚区域的物理 CapsuleCollider2D。
- 室外桌宠移动仍由 `PetController` 驱动，`RandomWander` 提供漫游目标，`PetPlayerInputController` 提供玩家输入，`WalkableSurface` 消费桥 PolygonCollider2D 的上侧轮廓。WorldMap 点击脚本不得直接写刚体位置或强制固定 Y。
- `WorldMapPetAnimationTriggerController` 只负责室外特殊动作和 `PetController` 移动锁，与室内动画状态机及动画资源保持边界；2026-09-08 修正尚未经过 Play 实机确认。

## 2026-08-21 室内家具选中反馈

`ApartmentFurnitureSelectionAuthoring` 将家具选中描边、句子提示和 `ApartmentViewportInputBridge` 路由作者化到 `Apartment_Main`；`ApartmentFurnitureSelectionPresenter` 只切换现有节点并填充文本，遮挡判定复用 `ClickOcclusionUtility`。当前 9 个原目标加现有 `家具_装饰_储物的家具_恶魔_01` 共 10 个目标已落盘；需求中的“苹果垫”是该储物家具的语义名称。

Updated: 2026-08-22

## 2026-08-18 verification addendum

- Outdoor WorldMap mailbox routing and daily-summary panel activation were verified through Unity MCP; the retired Apartment entry is inactive.
- WorldMap dual-pet free wander and Animator direction parameters were verified in Play; both pets use Scene-authored `RandomWander` bounds and no runtime visual construction.

## 苹果资源系统（2026-08-18 新版规则）

- `Assets/_Project/Scripts/Modules/Apple/AppleService.cs` 负责 20 个初始苹果、按 `IGameClock` 的 45–90 分钟随机轮次、每日 5 轮上限、70/30 数量、缓存领取与 JSON 持久化；WorldMap 的「大树 2」～「大树 5」由 `WorldMapAppleTreeAuthoring` 绑定 `AppleTreeInteractable` 与 `AppleTreeFeedback`，「大树 1」明确排除。
- 苹果是游戏 UI 资源栏的唯一货币，由四个页面已有的 `TopResource/BalanceLabel` 显示；`StubPanelBase` 与 `GachaPanelController` 统一读取 `IAppleService`，运行时只更新原文本数字，不创建新的 `AppleBalanceLabel`。成熟花朵在 `EmotionGardenService` 首次奖励 12 个苹果；`GachaService` 与 `TarotService` 分别以 20/100 和 8 个苹果消费。
- 测试位于 `Assets/_Project/Tests/EditMode/AppleResourceServiceTests.cs`；编辑器作者化入口为 `BootAppleBootstrapAuthoring`、`WorldMapAppleTreeAuthoring` 和 `ApartmentAppleBalanceAuthoring`。
- `AppleRuntimeBootstrap.EnsureRegistered()` 复用 Boot 已注册的 `IAppleService`，并为 WorldMap 直启补齐现有 `AppleService` 入口；`AppleTreeDropController` 在树收获前调用该幂等入口。该路径不改变成熟时间、掉落分配或苹果领取规则。

## 这份文档怎么看
这不是“理想中的最终目录图”，而是“当前仓库已经有什么，以及这些目录将来分别负责什么”的说明。

Gemini-Lab 当前已经不再是纯骨架仓库，而是“文档 + 原型实现并行”的早期阶段，所以这份文档会同时标明：
- 现在已经存在的结构
- 还没落地的资源与实现缺口
- 将来准备继续往哪里收口

## 当前顶层结构

### `docs/`
项目文档根目录，用来承载 AI 记忆、玩法规范、验证清单、工作流文档与 skill 说明。

当前已包含：
- `docs/ai-memory/`
- `docs/project-skill-catalog.md`
- `docs/skill-design-boundary.md`
- `docs/git-fork-upstream-pr-workflow.md`
- `tools/run-unity-editor-method.ps1`：本地 Unity batchmode 执行入口，当前带 `-nographics`、启动日志超时、总执行超时和子进程 watchdog，避免 Unity 启动卡住时无期限阻塞
- `tools/check-task-gate.ps1`：写入和 review 前的任务闸门；会自动识别视觉任务并要求 Scene/Play 一致性声明
- `tools/task-card-utils.ps1`：任务卡读取、仓库路径规范化和不可变计划 hash 计算
- `tools/check-task-scope.ps1`：记录任务开始时工作树基线，并拒绝未声明文件进入当前任务变更
- `tools/verify-task.ps1`：统一执行 review 闸门、任务范围、差异空白和 PowerShell 语法检查，输出机器可读报告
- `tools/check-scene-visual-contract.ps1`：按任务卡契约检查 Scene 节点和序列化 Sprite 引用
- `tools/check-runtime-visual-contract.ps1`：扫描运行时代码是否直接写入最终视觉资源或动态生成 UI 视觉节点
- `Assets/_Project/Scripts/Core/UI/UIRouter.cs`：当前顶层面板互斥切换的统一入口；打开新面板前会关闭当前已开的顶层面板，避免输入面板和图鉴面板同时显示
- `Assets/_Project/Scripts/Core/DevMode.cs`：当前还收口了通用点击遮挡裁决工具 `ClickOcclusionUtility`，供世界地图入口、宠物点击和相机取消选中共用

### `AGENTS.md`
当前项目总入口文档。

作用：
- 告诉后续智能体先读什么、后读什么
- 固定执行规则
- 固定文档更新触发器

### `Assets/`
Unity 项目主资源目录。

其中最关键的是 `Assets/_Project/`：
- 这是自研业务代码、场景、Prefab、SO、资源和设置的正式落点
- 后续功能实现原则上都应该往这里收口

### `Packages/`
包依赖定义与嵌入式包目录。

当前已知重点：
- 已有 Unity MCP 相关包
- 当前 `Packages/` 内保留的是嵌入式 `SkillsForUnity`
- `Packages/manifest.json` 当前已启用 `com.ivanmurzak.unity.mcp` `0.88.0`；旧版 4 个 MCP 包备份仍保留在 `PackageBackups/MCP-disabled-2026-06-02/`，不作为活动包参与解析
- `Assets/Plugins/NuGet` 当前也已临时移出到 `PackageBackups/NuGet-disabled-2026-06-02/`，避免 Burst 从活动资源路径扫描到 MCP 残留 DLL
- 已有 `com.unity.ai.navigation`
- `com.kirurobo.uniwinc` 当前使用官方 GitHub UPM URL；此前失效的本地 `file:` 路径会阻塞 Unity 打开项目
- 有嵌入式 `SkillsForUnity`
- 已补 `Assets/_Project/Scripts/Editor/Build/McpNuGetPlayerImportGuard.cs`，用于阻止 MCP 依赖解析器落地到 `Assets/Plugins/NuGet` 的外部 DLL 进入正式 Player 构建
- MCP 工具链现在由全局 `unity-mcp-cli` `0.88.0` 管理；Unity 已安全重启并连接 `http://localhost:27345`，可通过 CLI 调用 Unity Test Runner/MCP 工具

### `ProjectSettings/`
Unity 项目级设置目录。

### `.cursor/` 与 `.agents/`
AI 协作工具链目录。

当前已确认：
- `.cursor/mcp.json` 指向本地 MCP 服务
- `.cursor/skills/` 与 `.agents/skills/` 都存在
- 两套项目本地 skill 当前仍为镜像关系

## `_Project/` 当前结构

### `Assets/_Project/Scripts/`
这里已经不是纯目录蓝图，而是实际承载运行时代码的主战场。

当前真实状态：
- `Core/` 已有 `GameBootstrap`、`ServiceLocator`、`EventBus`、`CommandDispatcher`、FSM
- `Modules/` 已有 `Pet`、`Furniture`、`Navigation`、`Gateway`、`Travel`、`Persistence`、`UI`、`DesktopOverlay` 等真实代码
- `Editor/` 已有编辑器脚本（当前共 9 个编辑器工具：DebugDisplayWindow、ReadingBubbleLayoutSync、SaveSlotTemplateCreator、SettingsAndSaveSlotsPanelAuthoring、ApartmentFurnitureAuthoringBootstrapEditor、PetMoveAnimationSetupEditor、McpNuGetPlayerImportGuard 等）
- `Scripts/UI/` 目前主要仍承载目录说明；真实 UI 运行时代码当前主要在 `Scripts/Modules/UI/`
- `Pet` 与 `Furniture` 当前已经开始补“现有场景家具交互 + 运行时状态显示”链路
- `Pet` 当前还新增了玩家直接控制入口：`PetPlayerInputController` 已开始用于 `Apartment_Main.unity` 中的 `Pet_Angel`
- `Pet` 当前还新增了点击回应入口：`PetClickReactionController` 与本地 `PetClickResponseLibrary` 已开始用于 `Pet_Angel` 的点击气泡反馈
- `Furniture` 当前还新增了场景显式绑定入口，用于把 `Apartment_Main.unity` 里已摆好的关键家具对象稳态接入家具系统，而不是继续完全依赖名称推断
- `Furniture` 与 `Pet` 当前还在继续补“交互类型级”运行时数据链路：交互类型、交互持续时间、状态面板摘要与动画分支已经开始从家具定义一路传到宠物状态机
- `ApartmentSceneFurnitureBindings` 当前还承担一层场景作者化兜底：需要持续清理重复 `_target`、修正误绑的定义 ID / 类别，并让场景中的显式绑定与真实 Sprite 资源名保持一致

### `Assets/_Project/Scenes/`
当前已存在真实场景文件：
- `Boot.unity`
- `Apartment/Apartment_Main.unity`
- `WorldMap/WorldMap_Main.unity`
- `Desktop/Desktop_Overlay.unity`

当前判断：
- 这 4 个场景已经足以说明仓库不再是“没有场景”的状态
- 但它们仍然更接近原型场景，而不是完整量产场景集
- `Apartment_Main.unity` 当前已经承载宠物、现成家具与状态/库存/概览面板，是任务 1 的主验证场景
- `Apartment_Main.unity` 当前也是玩家控制宠物移动的主验证场景；`Pet_Angel` 已开始通过 `WASD` / 方向键直接控制
- `2026-05-22` 起，`Apartment_Main.unity` 的 `Pet` 根节点已包含 `Pet_Angel` 与 `Pet_Devil`；当前双宠输入方式为“默认天使可控，点击恶魔后切换恶魔主控”，且未被选中的桌宠保持 `Idle`，不继续自动睡觉
- `2026-05-22` 起，`Apartment_Main.unity` 中双宠也不再共用同一个移动边界；左侧恶魔区域已新增 `PetMovementBounds_Devil`，供 `Pet_Devil` 单独使用
- `2026-05-25` 起，`Apartment_Main.unity` 已开始搭建“公寓世界作为 UI 视窗显示”的第一版骨架：当前 `ApartmentViewportHost` / `ApartmentViewportImage` 已归属 `Panel_SpaceSys/Content`，并新增独立 `ApartmentViewportCamera` 与 `RenderTexture` 引用落点；当前已补最小输入桥接，支持 viewport 内的桌宠点击、当前宠物的家具交互尝试，以及建造模式开启时的放置/删除家具桥接
- `Apartment_Main.unity` 当前除可交互家具对象外，还可能包含一个仅承载纯静态补景家具的 `StaticFurnitureDecorOnly` 子节点
- `StaticFurnitureDecorOnly` 当前不只用于“纯视觉补景”，也开始承载一部分已有独立 Sprite 家具对象；这些对象若需要进入交互系统，必须同步补场景显式绑定，不能只把 Sprite 摆进场景
- `2026-05-26` 已完成 Apartment UI 的 P0 清理：`Apartment_Main.unity` 中旧的 `TopLeft_StatusPanel`、`Right_InventoryPanel`、`BottomRight_PersonalityRadar` 残留节点已移除；当前新主界面骨架保留 `Panel_PetStatus`、`Panel_SpaceSys`、`Sidebar`、`SidebarOverlay`、`ApartmentViewportHost`、`ApartmentViewportImage` 与 `ApartmentViewportCamera`，其中公寓 viewport 已改挂到 `Panel_SpaceSys`，后续 Apartment UI 继续基于 `Assets/_Project/Art/UI/` 下的新美术资源重新作者化。
- `2026-05-26` 已撤回此前对 `Panel_PetStatus` 的 `profile` 资料卡美术绑定尝试；场景不再保留刚才绑定的 `profile` 贴图、宠物正面待机预览 Sprite、雷达配色与尺寸微调。具体 UI 美术资源选择、贴图映射与最终视觉作者化后续由人工完成。
- `2026-05-26` 已完成 Apartment UI 的非美术技术收口：`ApartmentViewportInputBridge` 现在只处理落在 RawImage 矩形内的点击，并暴露可测试的坐标转换；`SidebarController` / `StubPanelBase` 可在直接打开 Apartment 调试时兜底创建 `IUIRouter` / `EventBus`；`ProfilePanelStub`、`TarotPanelStub`、`InventoryPanelStub` 已补服务缺失或空数据提示。
- `unity-mcp-cli` 已可在当前 shell 使用，MCP 只读检查与情绪花园定向 EditMode 测试已实跑；viewport 点击、Sidebar 面板切换、建造模式桥接仍需在 Unity PlayMode 中按 `docs/manual-validation-checklist.md` 的 E2 章节补验。
- `2026-07-28` 起，`WorldMap_Main.unity` 中桥对象 `桥` 的过桥移动轮廓以同物体 `PolygonCollider2D` 上侧轮廓为唯一事实源；`WalkableSurface` 会按桌宠当前世界 X 取 polygon 边交点的最高 Y，不再维护 `_profileLocalPoints` 独立折线轨道。`PetController.ResolveGroundY` 会把桥面 surface Y 当作脚底/行走锚点高度，再换算成 transform Y。当前脚本级构建已通过，PlayMode 过桥表现仍需人工补验。
- `2026-07-29` 起，`WorldMap_Main.unity` 的 `Panel_EmotionCollection` 图鉴 UI 改为书本式 Scene 作者化结构：`WorldMapEmotionGardenUIPatch.SetupFlowerCollectionBookContent` 负责在 `Content` 下搭建 `CodexView` 与 `DetailView`，并接入 `Assets/_Project/Art/WorldMap/UI/flowerCodex`、`Assets/_Project/Art/WorldMap/UI/flower_info` 的拆分资源；`FlowerCollectionPanelStub` 运行时只填数据和切换视图。本轮已重新执行 `WorldMapEmotionGardenUIPatch.Patch()` 并落盘，`WorldMap_Main.unity` 已包含 `CodexView`、`DetailView`、`TitlePlate`、`CategoryTabs`、`CodexCardSlot_00...11`、`StockPlate` 等节点及当前 UI 美术资源 GUID；PlayMode 点击和最终视觉微调仍需在 Unity 中补验。
- `Panel_WeeklyGarden` 的瓶内保留按花类型显示真实资源的 `FlowerImage`；它仅在已开花时显示对应的带枝叶完整花图。
- `2026-08-10` 起，`Panel_WeeklyGarden/Content/UIbar` 是唯一的集中信息条，由 `DateText`、`EmotionText`、`FlowerLanguageText` 显示当前默认日期或选中日期的信息；面板根 Scene 缩放为 `0.85`，UIbar 根节点缩放为 `1.5`（实际显示约 `360×102`），UIbar 字号为 `18/16/18`，资源入口为 `Assets/_Project/Art/WorldMap/UI/garden_week/UIbar.png`。
- `Day0`~`Day6/Bottle` 使用 `WeeklyGardenBottleInteraction`：悬浮缩放瓶子，选中激活使用 `SpriteAlphaOutline` 材质的 `SelectedHighlight` 外圈；`Content/BlankClickArea` 点击后清除选择。所有视觉节点、材质引用和默认状态由 Scene 作者化，运行时不生成最终视觉。
- `2026-08-09` 起，成长阶段 icon 位于每个 `UIbar/Growth`，使用 `Assets/_Project/Art/WorldMap/花朵图鉴/花朵/` 下按培育者和情绪类型匹配的花头资源；有当天记录时显示，无记录时隐藏。运行时只切换 Scene 预置变体，不写入 Sprite。已开花时瓶内仍由 `FlowerImage` 显示带枝叶完整花图；`Panel_EmotionCollection` 图鉴只展示已开花收集项。
- `2026-08-07` 起，每周种植面板的已显示花卉按“完整花图 + `Assets/_Project/Art/WorldMap/flower/土壤.PNG`”组合；图鉴列表卡片和图鉴详情只显示完整花图，不显示土壤。场景中的每周 `SoilImage` 由 `WorldMapEmotionGardenUIPatch` 作者化并绑定，图鉴土壤节点不再绑定。
- `2026-08-07` 修正图鉴列表：卡片空 Sprite 的默认半透明颜色不再泄漏到已收集花图，运行时和 Scene 作者化都保持完整花图不透明；每周格子、图鉴卡片和详情页的土壤位置已收紧并保存到 `WorldMap_Main.unity`。
- `2026-08-07` 新增 WorldMap 花卉布局复用工具 `Assets/_Project/Scripts/Editor/Tools/WorldMapFlowerSoilLayoutWindow.cs`，从 `Tools/Gemini-Lab/WorldMap 花卉布局复用` 打开。窗口有每周种植、图鉴列表、图鉴详情三个按钮，使用各自参考对象复制 `FlowerImage` / `SoilImage` 的 RectTransform 布局，并支持 Undo 与场景 dirty 标记。
- `2026-08-09` 起，每周培育的 `Bottle` 是固定 Scene 美术节点，所有日期都使用 `UI/garden_week/bottle.png`；培育者和成长阶段只影响瓶内花朵与 UIbar，不影响瓶子。旧 `DayText` 默认关闭，星期只使用 `DaySprite`。
- 同轮，图鉴卡片的锁定态也直接作者化在 Scene 中：未解锁卡片的 `LockedImage` 开启，`FlowerImage`、`UnlockedContent` 关闭；当前 Scene 默认保留前三张已收集卡的真实花枝预览，运行时只根据收集数据切换这些已有节点。
- `2026-08-09` 起，`SceneAuthoredImageVariantView.cs` 是每周花图、成长花头、图鉴卡片和详情页的稳定序列化变体组件；每周无花状态的瓶内花、成长 icon、土壤和花朵预览均隐藏，三个 UIbar 信息区显示真实数据或 `---`。
- 图鉴详情页改为每个 `Variant_00...` 独立包含 `FlowerArt`；每周培育格仍保留独立 `SoilImage`，图鉴详情不再创建或显示土壤。`WorldMapFlowerSoilLayoutWindow` 的每周布局入口继续可用，图鉴目标只复制花图布局。
- `2026-08-10` 起，WorldMap 花朵摆放入口已改为右侧 `FlowerPlacementPanel` 滚动侧边栏（`2026-08-11` 已按参考图修正锚点、条目内部图标/名称位置与原始资源尺寸，并重构为固定详情页手风琴）；`FlowerList/FlowerOption_00...17` 使用 `arrange` 正式 UI 资源，标题花头取 `花朵图鉴/花朵`，放置单花取 `花朵图鉴/花朵放置`，花丛取 `花朵图鉴/花丛`。`FlowerList` 已启用子项高度控制，展开时当前标题与上方条目位置固定，详情页显示在标题下方，所有下方条目统一下移固定高度且不遮挡详情。`WorldMapFlowerPlacementAuthoring` 将 18 组条目、36 个预览、32 个放置槽和 10×5 的完整二维网格直接保存到 `WorldMap_Main.unity`；旧原型按钮节点已清理，32 个槽位的正式视觉引用已重新保存。
- 摆放层进一步以 `BaselineItem` 的基线/排序层为准：内部吸附在相邻层半格错位但不显示网格、同层单格占用、跨层可重叠；作者化只收录摆放区域内基线，花丛按场景 `花丛 3` 的实际碰撞体尺寸作为一格，并在点击合法草地区域时提交。
- `WorldMapFlowerPlacementController` 只负责侧栏显隐、条目展开、共享库存显示/消耗、3 单花合成 1 花丛、不可见吸附计算、有效区域校验、预置槽位移动和 `Esc` 退出；选择单花/花丛后自动隐藏侧栏但保留当前选择和预览，点击场景 UI 不会被误判为落位；不在运行时创建 UI、Sprite、网格线或最终摆放物。`2026-08-12` 起库存由 `EmotionGardenService` 按“情绪类型 + 培育者”持久化，花朵开花、摆放与合成使用同一份数据；旧存档升级到版本 3 时一次性迁移。版本 4 保存 `PlacedFlowers` 并在成功摆放后立即 autosave，重启恢复槽位/坐标且不二次扣库存；自由摆放不添加土壤。旧顶层 `FlowerPlacementStatusBar` 已移除，提示文字归属侧栏内 `PlacementStatus`。`2026-08-14` 起，`WorldMapPlacementSlot` 使用独立稳定 GUID 与显式占用状态，提交期间忽略同步恢复事件重入，避免成功点击后槽位被清空；Play 启动校验应看到 `32/32` 槽位、`1152` 个绑定。花卉视觉排序使用 `Default` Sorting Layer，与桌宠共享 `BaselineItem.SortingOrder` 主层级，同层再按基线 Y 决定前后，完全同线时桌宠略优先；AutoSetup 63 已将 `Pet_Angel`、`Pet_Devil` 根对象作者化为 `BaselineItem` 并保持实体碰撞。
- `2026-08-23` 起，`FlowerPlacementBounds` 可在 Scene/Inspector 直接调整；`WorldMapPlacedFlowers` 下新增 `FlowerPlacementRegion_Angel` / `FlowerPlacementRegion_Demon` 作为 owner 分区。两区用可禁用的 BoxCollider2D 保存范围，区域列表非空时运行时只按所选 owner 和完整 footprint 校验，不再静默使用 `FlowerPlacementBounds`；只有完全没有区域配置的旧场景才回退总范围。该逻辑不改变 BaselineItem 的基线/SortingOrder 遮挡关系。
- `FlowerSidebarViewport` 在 Scene 中保存左右 34、顶部 132、底部 56 的拉伸边距，列表位于标题下方并被窗口裁剪；独立作者化菜单为 `Tools/Gemini-Lab/WorldMap/Author Flower Placement`。
- `2026-07-30` 起，`WorldMap_Main.unity` 中 `CabinReturnPortal` / `WorldMapGardenZone` / `ClickableSceneObject` / `BaselineItem` / `PetPlayerInputController` / `PetClickReactionController` / `WorldMapCameraController` 都先用 `ClickOcclusionUtility` 裁决“当前最上层 2D 点击目标”再响应，避免房子被桌宠或 UI 遮挡时仍误跳公寓；`PetController` 也已加上 WorldMap 场景级双宠碰撞忽略，`Pet_Angel` 与 `Pet_Devil` 在室外场景不会互相挡路。
- `2026-08-05` 起，`WorldMap_Main.unity` 中的 `室内`、`邮箱`、`大树 1`～`大树 5` 由 `WorldMapInteractiveObjectAuthoring` 统一补齐 `WorldMapInteractiveObjectFeedback`；悬停缩放直接以 Scene 中对象的 localScale 为基准。点击由 `CabinReturnPortal` 或 `ClickableSceneObject` 承载，但大树 1 暂不绑定点击入口。
- `2026-07-30` 起，顶层 UI 路由已改为互斥切换：`UIRouter.Open` 会在打开新面板前关闭当前已开的顶层面板，因此 `Panel_EmotionInput` 与 `Panel_EmotionCollection` 这类入口不会再同时显示；`StockPlate` 仍只是详情页里的库存展示牌，不是独立按钮。
- `2026-07-31` 起，情绪花园的种植链路已经从固定占位值恢复为真实数据流：`EmotionFlowerModels` 中的 `EmotionFlowerCatalog` 负责 9 种情绪的本地轻量判定与 `angel / demon` 两位培育者对应的 18 个花名映射；`EmotionGardenService` 会把最终花名写入 `EmotionFlowerData.FlowerName`；`EmotionInputPanelStub` 提交原始心情文本后会自动切到 `WeeklyGardenView`；`WeeklyGardenPanelStub` 和 `FlowerCollectionPanelStub` 现在都读取真实花数据并显示花名、情绪、培育者与状态。

### `Assets/_Project/Tests/`
当前已存在：
- `EditMode/`
- `PlayMode/`
- 对应测试 asmdef 与多组测试脚本

这意味着测试目录和测试程序集已经真实落地。

### `Assets/_Project/Prefabs/`
Prefab 结构规划已写明，而且当前已经开始落地真实 `.prefab` 资产。

当前真实状态：
- `Assets/_Project/Prefabs/Furniture/**` 当前已覆盖 `Art/Sprites/Furniture/**/` 下的全部家具 Sprite 资源
- `Assets/_Project/Prefabs/UI/Tarot/**` 当前已有 `TarotHistoryEntry.prefab` 与 `TarotGuideCard.prefab`
- `Assets/_Project/Prefabs/UI/Panels/README.md` 与 `Assets/_Project/Prefabs/UI/Widgets/README.md` 已建立 UI prefab 工程落点说明；但 `Panels` / `Widgets` 下尚无正式 UI prefab 交付
- 当前 prefab 化仍主要集中在 `Furniture` 这条线，`Pet / UI / Environment / FX` 仍未收口

### `Assets/_Project/ScriptableObjects/`
SO 分类规划已写明，而且当前已经开始落地实际 `.asset` 文件。

需要区分：
- SO 类型代码已经存在于 `Scripts/Modules/**`
- 当前 `Assets/_Project/ScriptableObjects/FurnitureConfig/**` 已覆盖 `Art/Sprites/Furniture/**/` 下的全部真实 `FurnitureDefinitionSO` 资产
- 但其他模块配置资产还没有真正作者化落地

### `Assets/_Project/Art/` 与 `Assets/_Project/Animations/`
这里已经开始承接真实资源，而不只是目录规范。

当前可见内容包括：
- 宠物移动帧
- 宠物移动帧当前已切换为 `Frames/Move/正面`、`背面`、`侧面` 三个子目录
- 宠物交互帧（`read`、`beside door`），并已按非方向型交互变体规范统一重命名
- 家具与环境示例 Sprite
- `Art/Sprites/Furniture/` 下已开始承接从 `公寓场景.psd` 派生出来、准备进入家具系统接线的独立 Sprite，后续按中文语义命名维护
- 宠物动画片段与 Animator Controller
- WorldMap 图鉴 UI 资源：`Assets/_Project/Art/WorldMap/UI/flowerCodex` 当前承载图鉴列表页书本、卡、未知卡、关闭和左右按钮；`Assets/_Project/Art/WorldMap/UI/flower_info` 当前承载详情页书本、库存条、关闭和左右按钮；`Assets/_Project/Art/WorldMap/flower` 当前承载按情绪类型和培育者区分的真实花图，并由 `EmotionFlowerArtCatalog` 统一映射。`flower_codex.png` 与 `flower_info.png` 是参考合成图，不应作为最终整张锁死背景使用。
- `2026-05-23` 起，恶魔也已拥有自己的门边交互动画 `Pet_Devil_Interact_BesideDoor.anim`，当前通过 `Pet_Devil.controller` 的 `Interact_BesideDoor` 状态接入
- `2026-05-27` 起，恶魔还新增 `Pet_Devil_Interact_Write.anim` 与 `Pet_Devil_Interact_PlayingMusic.anim`，当前分别通过 `Pet_Devil.controller` 的 `Interact_Write` 与 `Interact_PlayingMusic` 状态接入
- `2026-05-27` 起，`Apartment_Main.unity` 中恶魔现有玩家交互绑定已把旧的天使竖琴 / 写字目标替换为恶魔 `玩掌机 / 画画`：`玩掌机` 坐到 `家具_装饰_沙发_恶魔_02`，`画画` 对着 `家具_休闲_画架_恶魔_01` 触发并坐到 `家具_装饰_椅子_恶魔_01`

### `Assets/_Project/Audio/`
当前仍主要是目录规范与 README，真实音频资产尚未开始落地。

### `Assets/_Project/Settings/`
当前仍主要是目录规范与 README；后续运行期设置资产应继续往这里收口。

### `Assets/_Project/Docs/`
当前已存在项目内补充文档，如桌面 Overlay 指南、Gateway Mock 合同、Phase 4 发布清单等。

## 当前项目的真实实现密度

### 已经具备
- 产品愿景与玩法说明
- 模块职责划分
- 真实运行时代码
- 场景资源
- asmdef
- EditMode / PlayMode 测试目录
- 示例美术与动画资源
- AI 工具链入口
- 文档协作体系

### 仍未完全具备
- `Furniture` 之外的真实 Prefab 资产
- `FurnitureConfig` 之外的真实 ScriptableObject 配置资产
- 完整的 2D NavMesh 实现
- 原生桌面透明窗口 / 点击穿透实现
- 完整收口后的正式场景 / 资源作者化体系

## 工作时的结构判断原则
1. 看到 README 不等于看到实现。
2. 看到目录不等于看到资源。
3. 只有仓库里真实存在的 `.cs`、`.unity`、`.prefab`、`.asset` 等文件，才算已落地内容。
4. 看到“可运行原型”也不等于看到“最终正式结构”；仍要判断哪些地方是占位实现、哪些地方是资产化落地。
5. 结构变化以后，要同时更新文档和索引，不能只改文件夹。
6. 当前阶段如果文档提到“大模型驱动”“自主行动”，要先确认它说的是长期规划还是现阶段原型；现阶段原型已切换为玩家直接控制宠物移动。
7. 涉及视觉、布局、UI、相机、装饰层的开发时，默认要求 `Play` 视图与 `Scene` 视图一致；最终视觉结果应优先作者化到 Scene / Prefab / Inspector，而不是运行时脚本。

## WorldMap 昼夜结构

- `WorldMapNightOverlay` 位于 `WorldMap_Main.unity`，直接引用 `weather/夜幕.png`，排序高于室外世界与桌宠、低于 UI；旧 `garden/天气（最上层）` 不作为运行时天气来源。
- `WorldMapDayNightController` 切换已作者化的夜幕与星星 SpriteRenderer 的启用状态，时间来源为 Core 的 `IGameClock`；白天 06:00–18:00，夜晚为其余时间。星星节点为 `WorldMapWeatherStars`，只在夜晚显示。
- `WorldMapDayNightAuthoring` 负责 Scene / Inspector 中的夜幕位置、碰撞禁用、排序和作者化时刻初始状态。

## WorldMap 当地天气结构

- `WorldMapWeatherController` 与 `WorldMapWeatherService` 位于 WorldMap 模块；控制器通过 `IWeatherProvider` 请求 Open-Meteo 当前天气，并把 WMO weather code 归类为 `Sunny` 或 `Rainy`。
- `WorldMap_Main.unity` 保存 `WorldMapWeatherRainOverlay`、`WorldMapWeatherClouds` 与 `WorldMapWeatherStars` 场景节点；天气美术统一来自 `Assets/_Project/Art/WorldMap/weather/`，雨天节点引用 `rain.png`，云层引用 `云层.jpg`，星星引用 `星星.PNG`，晴天在专用资源到位前使用场景底图，不再引用旧 `garden/天气（最上层）`。控制器只切换已作者化 SpriteRenderer 的启用状态，不在运行时创建 GameObject、Sprite 或 UI。
- 默认 Inspector 坐标为上海 `31.2304, 121.4737`、时区 `auto`，刷新间隔 30 分钟；正式项目接入定位后只需替换 Scene 中的经纬度。网络失败时保留最近一次成功状态，首次失败回退晴天。
- 天气覆盖层与昼夜夜幕是独立维度：昼夜仍由 `IGameClock` 按 06:00–18:00 切换，天气由远端当前 weather code 切换；两者均要求 Scene 与 Play 共享已保存的视觉节点。
- `WorldMapNightOverlay` 使用 `ProjectSettings/TagManager.asset` 中位于 `Default` 之后的专用 Sorting Layer，确保运行时按 `BaselineItem` 重排的两名桌宠和花朵也会被夜幕覆盖；它仍由 Scene 作者化，运行时只切换启用状态。

## WorldMap 环境动画结构

- `_SceneRoot` 上的 `WorldMapAmbientAnimationController` 使用已保存的 `WorldMapWeatherClouds`、`WorldMapPlacedFlowers` 单花节点和许愿树/大树 2～5引用。
- 云层仅沿 X 轴平滑移动；单朵花按确定性相位轻微旋转，花丛不旋转；树木以 Sprite 底部为根部轻微摆动。所有 Y 基线和资源引用仍由 Scene/Inspector 作者化，运行时不创建视觉节点。

## WorldMap 桌宠数字键动画调试结构

- `WorldMapPetAnimationTriggerController` 位于 WorldMap 模块并挂在 `WorldMap_Main.unity/_SceneRoot`，只负责当前联调阶段的数字键触发，不参与 Apartment 桌宠资源或动画状态机。
- 当前状态映射为：`1` 天使 `Outdoor_Sit`、`2` 天使 `Outdoor_Pray`、`3` 天使 `Outdoor_Happy`、`4` 天使 `Outdoor_Water`、`5` 恶魔 `Outdoor_Sleep`、`6` 恶魔 `Outdoor_Cast`、`7` 恶魔 `Outdoor_Proud`。
- 旧的 `WorldMapAnimationTriggers` 和五个临时点位已删除；不再用可视化/空物体表达尚未确定的区域、标牌或苹果树位置。
- 天使坐地、祈祷序列帧已由 `WorldMapOutdoorPetAnimationAuthoring` 生成/更新对应 Clip，并绑定到 WorldMap 专用 `WorldMap_Angel.controller`；Apartment 控制器不复用这套资源。
- 数字触发结束后回到普通 Idle / Move；自动巡航、自动触发、家具识别触发仍待最终策划位置和条件确认。
- 非移动数字动画播放期间，`PetController.SetExternalMovementLock` 会按桌宠暂停玩家输入、随机漫游和刚体速度；结束后解除锁定，不影响另一只桌宠。

## 推荐阅读顺序
1. `AGENTS.md`
2. `docs/ai-memory/gemini-lab-memory-main.md`
3. `docs/ai-memory/gemini-lab-project-file-guide.md`
4. `README.md`
5. `Assets/README.md`
6. `Assets/plan.md`
7. 再进入 `Assets/_Project/` 的实际脚本、场景与模块 README

- `2026-07-30` 起，`WorldMap_Main.unity` 的 `Panel_WeeklyGarden/Grid/CellTemplate` 维持编辑器模板用途但默认不可见；`WeeklyGardenPanelStub` 会在运行时再次隐藏它，面板实际只显示 7 个瓶子。
- 同轮，`Panel_WeeklyGarden/Grid` 已改为纯容器，不再依赖 `HorizontalLayoutGroup` 排布，`Day0`~`Day6` 可直接在 Scene 里自由摆位。

## WorldMap 双宠动画调整场景

- `Assets/_Project/Scenes/WorldMap/WorldMap_PetAnimationPreview.unity` 是只用于动画调整的轻量场景，由 `_SceneRoot` 承载 `Main Camera`、`Pet_Angel`、`Pet_Devil`。
- 预览场景使用室外专用 Sprite 与 `Assets/_Project/Animations/WorldMap/Pet/` 下的两套 WorldMap Animator Controller；Apartment 宠物资源保持独立。
- `WorldMap_Main.unity` 与预览场景共享 Controller / AnimationClip 资产。Animation 窗口中的动画修改应落在共享 `.anim` / `.controller` 资产上，才能同步室外场景。
- 场景作者化脚本为 `Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapPetAnimationPreviewAuthoring.cs`；它只校准共享引用，不重建或复制现有动画资源。

- 小门下新增作者化碰撞节点 `DividerUpper`、`DividerLower`、`PassageBlocker`；两宠 motor 显式绑定另一侧 `PetMovementBounds`，原 door BoxCollider 改为点击 Trigger。只开放门洞，外墙和家具仍阻挡。

- Apartment_Main 新增根节点家具“水晶球”“扭蛋机”，分别为 CrystalBall/Gacha Prefab 实例。定义在 FurnitureConfig/Leisure，绑定页面 Tarot/Collection；视口桥新增 `_furniturePageLinks` 序列化引用。

- 两只 Apartment 宠物的 `PetClickReactionController._controlIndicator` 绑定自身 `ControlIndicator`，普通点击气泡位于该标识上方，不再让箭头遮住文字；现有节点层级保持不变。

## AI 每日小结邮箱

- `WorldMap_Main.unity` 的 `Canvas` 下保存 `Panel_DailySummaryMailbox`、`DailySummaryContent` 与隐藏 `WorldMapDailySummaryMailboxOpenTarget`；唯一入口是 `室外背景/邮箱` 的序列化点击事件。Apartment 中旧 `MailboxButton` 与同名面板为 inactive 遗留对象，不再作为入口。
- `EmotionGardenService` 将每日输入、花朵信息、总结、天使笔记和恶魔笔记写入 `DailySummaries`，重启后通过同一服务恢复；旧存档按已有花朵记录补齐摘要。
- `PersistenceBootstrap` 监听情绪提交事件并串行保存 `autosave`，不再只依赖应用退出回调；这保证停止 Play 后再次运行时也能读取最新摘要。
- 现阶段使用本地确定性 AI 风格生成器保证离线可用；网关具备结构化协议后只替换生成层。

## WorldMap 自由行走与动画状态

- `WorldMap_Main.unity` 中的 `Pet_Angel` 与 `Pet_Devil` 均作者化保存 `RandomWander` 和输入控制组件，默认不抢占玩家控制。
- `RandomWander` 在边界内选择目标并交给 `PetController`；移动时由现有 Animator 参数切换 `Move_Front`、`Move_Back`、`Move_Side`，停下时回到对应 Idle。点击桌宠取得控制后暂停漫游，释放控制再恢复。

### AI 每日小结历史日期与资源弹窗（2026-08-22）

- 左侧日期列表由 `WorldMap_Main.unity` 中保存的 ScrollRect、Viewport、Content 和固定 `DailySummaryDateOption` 节点组成，不能运行时 Instantiate 日期 UI。
- `DailySummaryMailboxPanel` 通过 `IEmotionGardenService.GetDailySummaryDates()` 切换选中日期，并把 `GetDailySummary(dateIso)` 填入 SummaryView 与便签文本。
- `PopupButton` 放大显示 `弹窗1.png`；预览节点、关闭按钮和遮罩均为 Scene 作者化，`PopupContent` 不承载纯色背景。

### WorldMap 苹果树交互轮廓（2026-08-24）

- `WorldMap_Main.unity` 中「大树 2」～「大树 5」的根节点保存各自 Sprite 透明轮廓生成的 `PolygonCollider2D`，悬停与点击共用这个轮廓；不再保存旧的整块 `BoxCollider2D`。
- 轮廓生成属于 `WorldMapInteractiveObjectAuthoring` 的编辑器作者化步骤，Play 期间只读取 Scene 中已保存的 Collider。更换树 Sprite 后需重新执行作者化入口。
- 「大树 1」仍按策划未定规则保留悬停但不接苹果或通用点击。

### AI 每日小结二级弹窗文字叠加（2026-08-23）

- 放大便签/总结资源的动态正文使用 `PopupContent/PopupBodyText`，位置和尺寸直接保存在 Scene，运行时只填充文本。
- `PopupView`（`弹窗1.png`）为自带文字的资源，运行时隐藏通用正文节点，避免重复叠字。
## Apartment 遗留物表现结构（2026-08-20）

- `Apartment_Main.unity/ArtGenerated/ApartmentKeepsakeWorldPresentation` 承载固定纸条点位、纪念物点位与 `ApartmentKeepsakePresenter`；节点默认隐藏，Sprite/Collider 均由 Scene 保存。
- `Apartment_Main.unity/UI_Sidebar/ApartmentKeepsakeOverlay` 承载详情弹窗、Angel/Devil 来源木牌、赠礼收藏面板和图鉴入口；弹窗覆盖层使用 sorting order 103。
- `ApartmentViewportInputBridge` 是 Apartment RenderTexture 的统一输入入口，遗留物实现 `IApartmentWorldPointInteractable` 后由桥路由，避免直接依赖场景对象鼠标回调。
## AI diary visual update (2026-08-21)

- WorldMap `Panel_DailySummaryMailbox` now uses Scene-authored `AI_diary` Sprite references for the board, tabs, notes, summary and character cards.
- `AngelNoteButton`, `SummaryButton`, `DevilNoteButton`, `AngelCardButton` and `DevilCardButton` each open its own enlarged Scene-authored preview under `DailySummaryDetailPopup`; the runtime never substitutes `弹窗1.png` as fixed content.
- `DailySummaryMailboxPanel` only toggles the selected preview and fills TMP text. It does not create final UI or assign runtime Sprites.
## AI diary reference layout calibration (2026-08-22)

- `DailySummaryMailboxAuthoring` keeps the board background and all individual AI diary resources as editable Scene nodes, while aligning the title, left tabs, notes, summary and character cards with the reference composition.
- `DailySummaryDetailPopup` continues to display the resource that was clicked, enlarged through a pre-authored preview node; no fixed `弹窗1.png` content is substituted.

## WorldMap 固定基线（2026-09-01）

- `WorldMapPlacedFlowers/FlowerPlacementGrid` 是室外基线的唯一作者化容器，固定保存十一条基线定义，不随绑定物体生命周期变化。
- 初始配置的相对渲染顺序（`RenderOrder` 从小到大）为：天空、星星、云、蓝色树木后排、蓝色树木前排、蓝色地面、白色花朵后排、白色花朵中后排、红色人物层、白色花朵中前排、白色花朵前排；工具显示时反向排列为前到后。
- 云和星星现在有独立场景资源节点，分别直接绑定到 `Environment_Clouds` / `Environment_Stars`；云层使用颜色键材质去除 JPG 白底，星星由昼夜控制器在夜间显示。
- 花朵放置控制器只使用四条白色槽位；基线 Y、X 范围、错位量和排序槽位均由 Scene/Inspector 调整。
- 编辑器工具 `WorldMapFlowerBaselineToolWindow` 在 Scene 视图绘制所有固定线并提供垂直拖拽；绑定 `BaselineItem` 会随线同步移动。工具面板按 `RenderOrder` 相对值从大到小显示，可直接编辑该共享值，也可用固定点击式“前移/后移”交换相邻基线；数值只比较大小，不代表基线条数。
- `WorldMapBaselineDefinition` 是基线身份、Y、X 范围、花朵放置许可和 RenderOrder 的唯一事实源；`BaselineItem` 保存对该定义的序列化引用及隐藏的偏移保护值，不保存本地 Y、范围或排序参数。Inspector 与基线工具编辑的是同一份定义。
- 当前 WorldMap 保留所有已绑定物体原有的基线轴心偏移，物体自身只能沿 X 轴移动并锁定在 `BaselineY + 原偏移`；只有场景基线工具调整基线 Y 时，才会同步移动同线物体的 Y。
- The saved `WorldMap_Main` scene and the TMP font asset contain the final visual references. Runtime code only switches authored nodes, fills text and controls visibility.

## WorldMap PSD 相对位置与草地取景（2026-09-03）

- 基线绑定不重写 PSD 子物体的相对布局；场景中保存的物体位置仍是最终视觉来源。
- 花朵恢复和新放置均以解析出的基线 Y 为锚点，并以 `FlowerPlacementBounds` 的草地矩形限制可放置范围。
- 活动相机覆盖已作者化的天空与地面范围，避免额外空白区域。

## WorldMap PSD 相对位置复核（2026-09-03）

- `WorldMap_Main.unity` 中 `室外背景` 的子物体局部坐标必须保持 PSD 作者化结果；基线同步只能通过世界坐标 API 正确换算，不能把世界 Y 当作局部 Y 写回。
- 场景恢复后，天空、地面、树、桥、花丛和桌宠继续保持原有相对位置，活动相机使用原始取景，Play 画面不得出现由背景上移造成的空白蓝区。

## WorldMap 许愿系统（2026-09-04）

- 入口：场景对象 `许愿树` → `WorldMapWishTreeInteractable` → `WorldMapWishSystemController.OpenPanel()`。
- UI：`Canvas/WorldMapWishSystemPanel` 下作者化 `MainView`、`InputView`、`DetailView`、`MemoryListView` 与 12 个 `WishStarSlot_00..11`。
- 星位：`Canvas/WorldMapWishSystemPanel/Window/MainView/WishStarSlot_00..11` 作者化在主背景左上角插画许愿树区域；旧世界空间星位不渲染。
- 数据：`WorldMapWishService` 保存 Active/Fulfilled/Archived，最多 12 个活动显示槽位，跨重启恢复。
- `MemoryListView` 的“全部”列表包含三种状态的历史记录；归档只移出活动星位，不删除记录。
- 许愿专用资源位于 `Assets/_Project/Art/WorldMap/许愿树/`；运行时只做状态切换、文字填充和交互，不替换 Sprite。

## WorldMap 愿望 UI 流程修正（2026-09-05）

- 主面板 `item_button.png` 打开单一详情页；右侧愿望列表支持滚轮，左侧显示所选愿望详情。
- 愿望星星只显示在主面板左上角许愿树插画范围内，点击星星不打开详情页。

## WorldMap 云层与单花环境动画（2026-09-05）

- 天气资源从 `Assets/_Project/Art/WorldMap/weather/` 读取；云层显示使用作者化的 `WorldMapClouds_Alpha.png`，源 JPG 保持不变。
- `WorldMapWeatherClouds` 保存在 WorldMap Scene 中并绑定 `Environment_Clouds` 基线；标准 Sprite 材质保证透明云朵在 Scene 与 Play 中一致显示。
- `WorldMapAmbientAnimationController` 只对作者化单花视觉节点做独立相位的轻微 Z 轴旋转，花丛保持静止；最终节点、引用和参数仍可在 Scene/Inspector 中调整。
## WorldMap 双宠动画触发状态机（2026-09-05）

- `WorldMapPetAnimationTriggerController` 位于 `_SceneRoot`，只管理临时特殊动作；普通待机和走路仍由两个 `PetController` 的移动状态驱动。
- 天使：漫游进入苹果树附近随机坐地、进入许愿树附近随机祈祷、进入天使区域摆放花朵/花丛附近浇水；玩家控制时 F 键在对应苹果树/许愿树附近触发坐地/祈祷；新增天使花朵触发开心。
- 恶魔：漫游进入苹果树附近随机睡觉、进入恶魔标牌附近随机施法；玩家控制时 F 键在对应目标附近触发；新增恶魔花朵触发得意。
- 触发对象通过作者化脚本序列化，缺失标牌引用不会生成临时物体；阈值、概率、冷却和每个动作持续时间可在 Inspector 调整。
## WorldMap 苹果树掉落交互（2026-09-05）

- `WorldMapAppleTreeAuthoring` 在 `WorldMap_Main.unity/_SceneRoot/WorldMapAppleDrops` 下作者化每棵苹果树的 3 个地面掉落槽位，槽位保存 `apple.png`、碰撞体和 TMP 收获文字。
- 点击苹果树先锁定服务层批次总量，随机拆成 1–3 个正整数；点击地面苹果逐个领取，文字在苹果上方显示约 2 秒。
- 「大树 1」/「许愿树」不挂苹果掉落逻辑；现有生成周期、每日轮数和消费价格保持不变。树连续摆动幅度与点击时的快速摆动参数均可在 Inspector 调整。

## WorldMap 输入框视觉状态与情绪入口（2026-09-05）

- 情绪输入面板 `Panel_EmotionInput` 的 AngelTheme/DemonTheme 各保存提示图 `输入心情/*/input.png` 与 `UI输入框去字/angel_emotion_input.png`、`devil_emotion_input.png` 两个 Image 节点；`EmotionInputPanelStub` 只按焦点和结束编辑切换显隐。
- 许愿系统 `WorldMapWishSystemPanel/InputView/WishInputField` 保存 `许愿树/input.png` 与 `WishInputFocusedVisual`（`UI输入框去字/wish_input.png`）两套 Image；输入页打开不自动聚焦，点击输入框后才切换无字资源。
- 情绪入口由场景真实物体 `天使标牌`、`恶魔标牌` 上的 `WorldMapGardenZone` 提供；旧 `EmotionEntry_Angel/Demon` 节点停用，右上 `Btn_EmotionInput` 停用。图鉴卡片和详情花图不显示 `SoilImage`，每周培育仍保留土壤。

## WorldMap AI 情绪花园（2026-09-05）

- AI 服务复用室内聊天的 `Resources/LLMConfig.asset`；`EmotionGardenAiRuntimeBootstrap` 只注册服务，不创建场景视觉对象。
- 情绪输入提交后由 `EmotionGardenService` 统一调用 AI 并保存结果；没有配置或请求失败时使用本地规则兜底。
- 每周培育的集中信息栏显示 AI 关键词和花语；每日总结面板继续通过日期读取 AI 生成的 summary、天使短记和恶魔短记。
- 许愿面板 AI 接入暂缓。

## WorldMap 室外新手指引（2026-09-06）

- `Canvas/Panel_OutdoorTutorial` 保存七张可替换的教程页面，以及上一页、下一页、关闭按钮；面板默认关闭。
- `Canvas/Btn_OutdoorTutorial` 是当前的占位入口，后续可直接替换其 Sprite 或 UnityEvent，不需要改动分页代码。
- 所有图片引用和 RectTransform 都保存在 Scene；`SceneAuthoredImageVariantView` 运行时只做页面显隐与首尾边界控制，符合 Scene/Play 视觉一致约束。
### 2026-09-07 WorldMap 室外点击交互

- WorldMap 室外点击入口由 `_SceneRoot/WorldMapSceneInteractionRouter` 统一裁决，路由目标使用 Scene 序列化引用；背景、基线、种植区等未注册 Collider 不参与入口裁决。
- Apple 模块只提供树木和掉落槽目标接口，WorldMap 模块负责路由与面板/许愿树/桌宠目标；两者通过 Core 的 `IWorldMapSceneClickTarget` 通信。
- 本阶段仅完成静态路由接入，未进行 Play 实机确认。
### 2026-09-08 WorldMap Collider2D 点击命中修正

- WorldMap 统一路由仍使用 Scene 序列化目标引用；目标命中区域现在统一由目标自身 Collider2D 的 `OverlapPoint` 决定。邮箱、天使/恶魔标牌和两只室外桌宠不再使用 SpriteRenderer 的 bounds 作为点击区域。
- 本次未修改 Collider2D 的 Scene 几何、WorldMap 场景整体结构、动画资源或室内系统；Play 点击范围仍需人工确认。

## WorldMap 苹果成熟状态初始化（2026-09-08）

- `AppleRuntimeBootstrap` 在场景加载阶段确保 `IAppleService` 已注册，并为当前场景中已作者化的 `AppleTreeInteractable` 建立树状态；这保证调试时钟快进作用于已有树状态。
- `WorldMap_Main.unity` 仍是树 Collider2D、掉落槽、CollectionText 和反馈节点的唯一作者化事实源，本次不重写场景、不创建运行时视觉对象。
- `AppleService` 的生成、固定批次总量、逐个领取和货币入账规则未改变；Play 验证仍待人工执行。

## WorldMap 苹果掉落与反馈（2026-09-08）

- 苹果系统已按“服务预留固定批次 → Scene 槽位分配 → 单个槽位成功领取 → 余额入账”的顺序重写，`AppleService` 是唯一货币状态权威。
- 三棵目标树仍由 `WorldMap_Main.unity` 中各自的 PolygonCollider2D、AppleTreeDropController、3 个 AppleDropSlot 和 AppleTreeFeedback 组成；未使用运行时创建的苹果或文字对象。
- 收获提示和未成熟提示均使用 Scene 已有 TMP MeshRenderer，当前绑定 `NotoSansSC_SDF`，并清除旧 Liberation 实例材质覆盖。

## WorldMap 云层与室内入口（2026-09-08）

- 云朵视觉节点仍是 `WorldMap_Main.unity/WorldMapWeatherClouds`；完整运动范围由 Scene 中的 `BaselineLine_云` 和 `_cloudMoveMinX` / `_cloudMoveMaxX` 保存。
- 外场 `室内` 节点使用自身 `BoxCollider2D` 和 `CabinReturnPortal`，由 `WorldMapSceneInteractionRouter` 统一裁决点击并通过 `ISceneFlowService` 进入 `Apartment_Main`。
- 本次没有改动 Apartment 场景、室内交互或桌宠动画资源。

## WorldMap 云层范围与移速校正（2026-09-08）

- 云朵节点仍为 `WorldMap_Main.unity/WorldMapWeatherClouds`，范围参考为同场景已有的 `天空` SpriteRenderer。
- `WorldMapAmbientAnimationController` 使用天空与云层的真实渲染边界计算中心点和半跨度，并保留旧版 `0.12` 正弦往返、端点折返方式；不回写天空或云层的尺寸和位置作者化参数。
- 该改动只涉及 WorldMap 云层运行时辅助逻辑和序列化引用，不扩展到室内系统或其他交互。

## WorldMap 云层速度再次校正（2026-09-08）

- 云层仍使用天空宽度派生的移动范围和原有端点折返方式，仅把序列化速度参数从 `0.12` 降为 `0.06`。
- 不修改天空、云层资源、场景布局、相机或其他 WorldMap 功能。
