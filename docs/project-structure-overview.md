# Gemini-Lab 项目结构总览

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

- `Assets/_Project/Scripts/Modules/Apple/AppleService.cs` 负责 20 个初始苹果、按 `IGameClock` 的大树缓存生成、领取与 JSON 持久化；WorldMap 现有「大树 1」～「大树 5」由 `WorldMapAppleTreeAuthoring` 绑定 `AppleTreeInteractable`。
- 苹果是游戏 UI 资源栏的唯一货币，由四个页面已有的 `TopResource/BalanceLabel` 显示；`StubPanelBase` 与 `GachaPanelController` 统一读取 `IAppleService`，运行时只更新原文本数字，不创建新的 `AppleBalanceLabel`。成熟花朵在 `EmotionGardenService` 奖励 1 个苹果；`GachaService` 与 `TarotService` 分别以 1/5 和 1 个苹果消费。
- 测试位于 `Assets/_Project/Tests/EditMode/AppleResourceServiceTests.cs`；编辑器作者化入口为 `BootAppleBootstrapAuthoring`、`WorldMapAppleTreeAuthoring` 和 `ApartmentAppleBalanceAuthoring`。

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
- 4 个 Unity MCP 包当前已临时移出到 `PackageBackups/MCP-disabled-2026-06-02/`，不再作为活动包参与解析
- `Assets/Plugins/NuGet` 当前也已临时移出到 `PackageBackups/NuGet-disabled-2026-06-02/`，避免 Burst 从活动资源路径扫描到 MCP 残留 DLL
- 已有 `com.unity.ai.navigation`
- `com.kirurobo.uniwinc` 当前使用官方 GitHub UPM URL；此前失效的本地 `file:` 路径会阻塞 Unity 打开项目
- 有嵌入式 `SkillsForUnity`
- 已补 `Assets/_Project/Scripts/Editor/Build/McpNuGetPlayerImportGuard.cs`，用于阻止 MCP 依赖解析器落地到 `Assets/Plugins/NuGet` 的外部 DLL 进入正式 Player 构建
- 若后续要恢复 MCP，需把这 4 个包目录移回 `Packages/` 并把对应依赖加回 `Packages/manifest.json`

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
- 本轮未能在当前 shell 直接运行 Unity Editor / `unity-mcp-cli`，所以 viewport 点击、Sidebar 面板切换、建造模式桥接仍需在 Unity PlayMode 中按 `docs/manual-validation-checklist.md` 的 E2 章节补验。
- `2026-07-28` 起，`WorldMap_Main.unity` 中桥对象 `桥` 的过桥移动轮廓以同物体 `PolygonCollider2D` 上侧轮廓为唯一事实源；`WalkableSurface` 会按桌宠当前世界 X 取 polygon 边交点的最高 Y，不再维护 `_profileLocalPoints` 独立折线轨道。`PetController.ResolveGroundY` 会把桥面 surface Y 当作脚底/行走锚点高度，再换算成 transform Y。当前脚本级构建已通过，PlayMode 过桥表现仍需人工补验。
- `2026-07-29` 起，`WorldMap_Main.unity` 的 `Panel_EmotionCollection` 图鉴 UI 改为书本式 Scene 作者化结构：`WorldMapEmotionGardenUIPatch.SetupFlowerCollectionBookContent` 负责在 `Content` 下搭建 `CodexView` 与 `DetailView`，并接入 `Assets/_Project/Art/WorldMap/UI/flowerCodex`、`Assets/_Project/Art/WorldMap/UI/flower_info` 的拆分资源；`FlowerCollectionPanelStub` 运行时只填数据和切换视图。本轮已重新执行 `WorldMapEmotionGardenUIPatch.Patch()` 并落盘，`WorldMap_Main.unity` 已包含 `CodexView`、`DetailView`、`TitlePlate`、`CategoryTabs`、`CodexCardSlot_00...11`、`StockPlate` 等节点及当前 UI 美术资源 GUID；PlayMode 点击和最终视觉微调仍需在 Unity 中补验。
- `Panel_WeeklyGarden` 的瓶内保留按花类型显示真实资源的 `FlowerImage`；它仅在已开花时显示对应的带枝叶完整花图。
- `2026-08-10` 起，`Panel_WeeklyGarden/Content/UIbar` 是唯一的集中信息条，由 `DateText`、`EmotionText`、`FlowerLanguageText` 显示当前默认日期或选中日期的信息；面板根 Scene 缩放为 `0.85`，UIbar 根节点缩放为 `1.5`（实际显示约 `360×102`），UIbar 字号为 `18/16/18`，资源入口为 `Assets/_Project/Art/WorldMap/UI/garden_week/UIbar.png`。
- `Day0`~`Day6/Bottle` 使用 `WeeklyGardenBottleInteraction`：悬浮缩放瓶子，选中激活使用 `SpriteAlphaOutline` 材质的 `SelectedHighlight` 外圈；`Content/BlankClickArea` 点击后清除选择。所有视觉节点、材质引用和默认状态由 Scene 作者化，运行时不生成最终视觉。
- `2026-08-09` 起，成长阶段 icon 位于每个 `UIbar/Growth`，使用 `Assets/_Project/Art/WorldMap/花朵图鉴/花朵/` 下按培育者和情绪类型匹配的花头资源；有当天记录时显示，无记录时隐藏。运行时只切换 Scene 预置变体，不写入 Sprite。已开花时瓶内仍由 `FlowerImage` 显示带枝叶完整花图；`Panel_EmotionCollection` 图鉴只展示已开花收集项。
- `2026-08-07` 起，每周种植、图鉴列表和图鉴详情的已显示花卉都按同一规则组合：对应情绪/培育者的带枝叶完整花图 + 下方 `Assets/_Project/Art/WorldMap/flower/土壤.PNG`；三处只保留各自显示尺寸差异。场景中的 `SoilImage` 由 `WorldMapEmotionGardenUIPatch` 作者化并绑定，运行时只按数据显隐；空白格、锁定卡片和未选中详情不显示土壤。
- `2026-08-07` 修正图鉴列表：卡片空 Sprite 的默认半透明颜色不再泄漏到已收集花图，运行时和 Scene 作者化都保持完整花图不透明；每周格子、图鉴卡片和详情页的土壤位置已收紧并保存到 `WorldMap_Main.unity`。
- `2026-08-07` 新增 WorldMap 花卉布局复用工具 `Assets/_Project/Scripts/Editor/Tools/WorldMapFlowerSoilLayoutWindow.cs`，从 `Tools/Gemini-Lab/WorldMap 花卉布局复用` 打开。窗口有每周种植、图鉴列表、图鉴详情三个按钮，使用各自参考对象复制 `FlowerImage` / `SoilImage` 的 RectTransform 布局，并支持 Undo 与场景 dirty 标记。
- `2026-08-09` 起，每周培育的 `Bottle` 是固定 Scene 美术节点，所有日期都使用 `UI/garden_week/bottle.png`；培育者和成长阶段只影响瓶内花朵与 UIbar，不影响瓶子。旧 `DayText` 默认关闭，星期只使用 `DaySprite`。
- 同轮，图鉴卡片的锁定态也直接作者化在 Scene 中：未解锁卡片的 `LockedImage` 开启，`FlowerImage`、`SoilImage`、`UnlockedContent` 关闭；当前 Scene 默认保留前三张已收集卡的真实花枝与土壤预览，运行时只根据收集数据切换这些已有节点。
- `2026-08-09` 起，`SceneAuthoredImageVariantView.cs` 是每周花图、成长花头、图鉴卡片和详情页的稳定序列化变体组件；每周无花状态的瓶内花、成长 icon、土壤和花朵预览均隐藏，三个 UIbar 信息区显示真实数据或 `---`。
- 图鉴详情页改为每个 `Variant_00...` 独立包含 `FlowerArt` 与 `SoilImage`，土壤 Y 坐标按花型单独作者化；`WorldMapFlowerSoilLayoutWindow` 的详情复用入口也按同名 Variant 配对复制。
- `2026-08-10` 起，WorldMap 花朵摆放入口已改为右侧 `FlowerPlacementPanel` 滚动侧边栏（`2026-08-11` 已按参考图修正锚点、条目内部图标/名称位置与原始资源尺寸，并重构为固定详情页手风琴）；`FlowerList/FlowerOption_00...17` 使用 `arrange` 正式 UI 资源，标题花头取 `花朵图鉴/花朵`，单花取 `花枝`，花丛取 `花丛`。`FlowerList` 已启用子项高度控制，展开时当前标题与上方条目位置固定，详情页显示在标题下方，所有下方条目统一下移固定高度且不遮挡详情。`WorldMapFlowerPlacementAuthoring` 将 18 组条目、36 个预览、32 个放置槽和 10×5 的完整二维网格直接保存到 `WorldMap_Main.unity`；旧原型按钮节点已清理，32 个槽位的正式视觉引用已重新保存。
- 摆放层进一步以 `BaselineItem` 的基线/排序层为准：内部吸附在相邻层半格错位但不显示网格、同层单格占用、跨层可重叠；作者化只收录摆放区域内基线，花丛按场景 `花丛 3` 的实际碰撞体尺寸作为一格，并在点击合法草地区域时提交。
- `WorldMapFlowerPlacementController` 只负责侧栏显隐、条目展开、共享库存显示/消耗、3 单花合成 1 花丛、不可见吸附计算、有效区域校验、预置槽位移动和 `Esc` 退出；不在运行时创建 UI、Sprite、网格线或最终摆放物。`2026-08-12` 起库存由 `EmotionGardenService` 按“情绪类型 + 培育者”持久化，花朵开花、摆放与合成使用同一份数据；旧存档升级到版本 3 时一次性迁移。版本 4 保存 `PlacedFlowers` 并在成功摆放后立即 autosave，重启恢复槽位/坐标且不二次扣库存；自由摆放不添加土壤。旧顶层 `FlowerPlacementStatusBar` 已移除，提示文字归属侧栏内 `PlacementStatus`。`2026-08-14` 起，`WorldMapPlacementSlot` 使用独立稳定 GUID 与显式占用状态，提交期间忽略同步恢复事件重入，避免成功点击后槽位被清空；Play 启动校验应看到 `32/32` 槽位、`1152` 个绑定。花卉视觉排序使用 `Default` Sorting Layer，与桌宠共享 `BaselineItem.SortingOrder` 主层级，同层再按基线 Y 决定前后，完全同线时桌宠略优先；AutoSetup 63 已将 `Pet_Angel`、`Pet_Devil` 根对象作者化为 `BaselineItem` 并保持实体碰撞。
- `FlowerSidebarViewport` 在 Scene 中保存左右 34、顶部 132、底部 56 的拉伸边距，列表位于标题下方并被窗口裁剪；独立作者化菜单为 `Tools/Gemini-Lab/WorldMap/Author Flower Placement`。
- `2026-07-30` 起，`WorldMap_Main.unity` 中 `CabinReturnPortal` / `WorldMapGardenZone` / `ClickableSceneObject` / `BaselineItem` / `PetPlayerInputController` / `PetClickReactionController` / `WorldMapCameraController` 都先用 `ClickOcclusionUtility` 裁决“当前最上层 2D 点击目标”再响应，避免房子被桌宠或 UI 遮挡时仍误跳公寓；`PetController` 也已加上 WorldMap 场景级双宠碰撞忽略，`Pet_Angel` 与 `Pet_Devil` 在室外场景不会互相挡路。
- `2026-08-05` 起，`WorldMap_Main.unity` 中的 `室内`、`邮箱`、`大树 1`～`大树 5` 由 `WorldMapInteractiveObjectAuthoring` 统一补齐 `WorldMapInteractiveObjectFeedback`；悬停缩放直接以 Scene 中对象的 localScale 为基准，点击仍由 `CabinReturnPortal` 或 `ClickableSceneObject` 承载。
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

- `WorldMapNightOverlay` 位于 `WorldMap_Main.unity`，直接引用 `天气（最上层）/夜幕.png`，排序高于室外世界与桌宠、低于 UI。
- `WorldMapDayNightController` 只切换夜幕 SpriteRenderer 的启用状态，时间来源为 Core 的 `IGameClock`；白天 06:00–18:00，夜晚为其余时间。
- `WorldMapDayNightAuthoring` 负责 Scene / Inspector 中的夜幕位置、碰撞禁用、排序和作者化时刻初始状态。

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
