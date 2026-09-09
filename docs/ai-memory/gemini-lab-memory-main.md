# Gemini-Lab Memory Main

## 2026-09-09 室内小门交流完成

- 点击小门开关；选中宠物到任一侧门边按 F 交流，双向各 5 话题和三种回应已存 `IndoorDoorDialogues.asset`。
- 最新展示改为双宠各自气泡：发起句立即出现，2.5 秒后对方回应，8 秒后收起；不再有中央问答窗口和姓名前缀。跟随实际可见宠物（含家具姿态），不会翻字或拦截点击。当前 52 项 EditMode 与 Play 30 组问答通过，证据 `Logs/DoorBubbles/`。
- 按新图片需求修正 NORMAL 双方 E-1/M+1/F+1，NEED_SPACE 全部为 0；WARM 不变。保留发起者 E<10 限制与亲密度 300 秒冷却。
- 公寓宠物新增独立身体点击区域，选中不再结算社交；复用金色头顶箭头并修正页面切换显示。脚底摩擦为 0，skin 降到 0.02。
- 38 项 EditMode、Play 30 个话题/回应组合、视口开门、身体点击与 10 条家具物理路线验证通过；原存档 SHA256 已恢复一致，退出 Play，场景中仅两只宠物。入口与限制见 `docs/indoor-door-interaction.md`。


Updated: 2026-09-04

## 2026-09-09 公寓移动优化

- 两只公寓宠物显式绑定 `ApartmentPetMovement`：手动与自动移动共用脚底边界、扫掠/滑动与固定物理帧运动；自动走位通过可见图绕家具，未修改世界地图移动链路。
- 10 个 `Approach_*` 站立点已保存 Scene，门口/竖琴等目标与交互姿势分开；新增 `Upgrade Pet Movement` 作者化菜单，保留人工站立点。
- 35 项 EditMode 通过；Play 10 条实际物理路线、16 组方向边界测试通过，正常帧弹琴与绕床睡眠通过。原存档已恢复核对。实际边界见 `docs/apartment-movement.md`。

## 2026-09-09 遗留物可靠性完善

- 当前功能分支新增：延迟注册服务补读档；遗留物跨场景复用时恢复好友度订阅；双房间独立占用；首次奖励写入日期并兼容 v1 存档。
- 赠礼按 Scene 中显式 `_displaySlotId` 固定展示；打开弹窗成功才消费纸条/临时遗物；读档通知预置视觉刷新。
- 作者化工具保留已有房间、弹窗、配置和人工布局；新增 `Upgrade Room Relic Bindings` 菜单。8 条纸条文案已替换占位；速写、小星星吊坠仍缺正式素材。
- 实现与验证边界见 `docs/room-relic-system.md` 和 `docs/manual-validation-checklist.md`；本地 main `a683a60` 尚无该模块。
- Play 复验修复：遗留物弹窗保留 SpaceSys 背景，纸条文字/关闭按钮可读，赠礼图文分离；布局已存 Scene。29 项 EditMode 通过；Play 物理触发、模拟视口点击、场景往返与独立目录存读档通过。原存档已恢复核对，尚未覆盖完整主菜单和独立构建。

## 2026-09-03 室内遗留物系统实现（未运行场景作者化）

- 新增 `GeminiLab.Modules.RoomRelic` 运行时模块：`RoomRelicService` 实现每日首次进入判定、纸条 50%、临时遗留物 45~79 每日 50%、永久赠礼 ≥80 每日 15% 且不重复，存档 key 为 `room_relic`。
- `RoomRelicRuntimeBootstrap` 在 Apartment 场景 `RoomRelic` 根节点注册服务；`RoomRelicEntryTrigger` 复用 `PetMovementBounds` / `PetMovementBounds_Devil` 作为房间进入触发。
- 运行时视觉组件只切换场景预置槽位/变体，不创建最终视觉；新增 `RoomNotePopup`、`RoomRelicDetailPopup`、`RoomGiftObtainedPopup` 三个 UI 面板。
- 新增编辑器作者化入口 `ApartmentRoomRelicAuthoring`，菜单为 `Tools/Gemini-Lab/Apartment/Author Room Relic`；`AutoSetup` 版本提升到 66。已通过 MCP 执行作者化，Apartment 场景里已生成 `RoomRelic` 节点、占位槽位与三个弹窗。
- 新增 EditMode 测试 `RoomRelicServiceTests`；代码已通过本地临时项目编译校验，任务闸门和视觉契约检查通过。
- `Assets/_Project/Art/Sprites/Relic/` 中 8 张已提供遗物 Sprite 已绑定到 Apartment 场景 `RelicSpawn` 变体；缺失的 `速写` 和 `小星星吊坠` 仍使用占位。
- 2026-09-04 补充：`RoomRelicDetailPopup` 与 `RoomGiftObtainedPopup` 增加 `_iconView`，复用 `RoomRelicView` 变体切换显示 icon（运行时不写 Sprite）；作者化 `CreatePopup` 生成 icon 变体节点。
- 赠礼数据从占位改为 4 个真实素材：南瓜糖果/速写（恶魔→天使）、羽毛书签/小星星吊坠（天使→恶魔），缺素材的 `roomVisualKey` 留空占位；南瓜糖果、羽毛书签已绑定素材，速写、小星星吊坠仍缺素材。
- 纸条掉落槽位按 `visualType` 绑定纸条/纸团素材（折纸 Origami 形态已取消），赠礼掉落槽位绑定真实素材。
- 2026-09-04 SpaceSys 面板增加「人物主控」通用标识：`SpaceSysPanelStub` 新增 `_angelControlIndicator`/`_devilControlIndicator`（SpriteRenderer），运行时按 `PetPlayerInputController.ActiveTransform` 对应的 `PetController.PetId` 切换 active；标识挂在 `Pet_Angel`/`Pet_Devil` 头顶（世界空间 SpriteRenderer，跟随宠物），绑定 `Assets/_Project/Art/Sprites/Pet/人物主控.png`。
- 2026-09-04 新增调试工具 `Assets/_Project/Scripts/Editor/Tools/RoomRelicDebugWindow.cs`，菜单 `Tools/Gemini-Lab/Room Relic Debug`：Play Mode 下可设置好友度（0/45/80）、触发 Angel/Devil 房间进入判定（反射重置每日判定）、查看当前纸条/遗物/赠礼状态，用于验收遗物掉落。

## 2026-08-14 苹果资源系统

- 新增 `GeminiLab.Modules.Apple`：`AppleService` 以 `IAppleService` 为门面，首次新档余额为 20 个苹果，存档 key 为 `apple`。
- `AppleService` 只通过 `IGameClock.UtcNow` 计算大树离线生成：每棵树每 6 小时生成 1 个，单树缓存上限 3 个；树 ID、上次生成时间、待领取数量和累计领取量都会进入存档，重启不会重复生成。
- `WorldMap_Main.unity` 的「大树 1」～「大树 5」已由 `WorldMapAppleTreeAuthoring` 作者化 `AppleTreeInteractable`，点击树即领取缓存苹果；不创建运行时树/苹果视觉对象。
- 情绪花首次开花通过 `EmotionGardenService.BloomAt` 奖励 1 个苹果；扭蛋单抽/五连分别消耗 1/5 个苹果，塔罗开始一次抽牌会话消耗 1 个苹果。
- `BootAppleBootstrapAuthoring` 已将苹果服务注册宿主保存到 `Boot/BootstrapRoot`；Apartment 四个页面复用原有 `TopResource/BalanceLabel`，由 `StubPanelBase`/`AppleBalanceDisplay` 统一更新苹果数量，不再创建独立的 `AppleBalanceLabel`。苹果是 UI 资源栏的唯一货币，文本只显示数字。
- EditMode 新增 `AppleResourceServiceTests`，覆盖初始余额、树生成/缓存往返、领取、消费和成熟奖励去重；Unity 编译与任务/视觉闸门已通过，PlayMode 晃树与完整抽扭蛋/塔罗仍需人工复验。

## 定位
这份文档是 Gemini-Lab 的长期项目记忆总览。

`AGENTS.md` 已经作为总入口落地；本文件承担“第二入口 + 主记忆总览”的作用。

## 快速导航
- [AGENTS.md](../../AGENTS.md)
- [当前任务卡](../current-task-card.md)
- [当前任务卡 JSON](../current-task-card.json)
- [上下文包](../workflow-context-packages.md)
- [架构记忆](./gemini-lab-memory-architecture.md)
- [规则与历史](./gemini-lab-memory-rules-and-history.md)
- [开发手册](./gemini-lab-agent-development-playbook.md)
- [文件指南](./gemini-lab-project-file-guide.md)
- [项目结构总览](../project-structure-overview.md)
- [玩法规范](../gameplay-spec.md)
- [人工验证清单](../manual-validation-checklist.md)
- [美术替换工作流](../art-replacement-workflow.md)
- [Git 开发流程](../git-fork-upstream-pr-workflow.md)
- [项目 Skill 清单](../project-skill-catalog.md)
- [Skill 设计边界](../skill-design-boundary.md)
- [方法论对齐审计](../ai-workspace-bootstrap-alignment.md)
- [上下文压缩与知识沉淀计划](../context-compression-and-knowledge-plan.md)
- [做梦整理清单](../dream-maintenance-checklist.md)
- [记忆索引](./memory-index.paths.txt)

## 2026-08-07 Scene/Play 视觉一致性硬闸门
- 视觉任务不再只依赖文字约定。`docs/current-task-card.json` 必须声明 `scene_play_parity_required`、`scene_visual_contracts` 和 `runtime_visual_files`。
- `tools/check-task-gate.ps1` 会依据 `direct_files` 自动识别场景、Art、运行时模块、UI 和 SceneBootstrap 等视觉任务；视觉任务若未声明 Scene/Play 一致性会直接失败。
- `tools/check-scene-visual-contract.ps1` 检查任务卡声明的 Scene 节点是否真实存在，并可检查关键 UI 节点的 `m_Sprite` 是否为非空序列化引用。
- `tools/check-runtime-visual-contract.ps1` 拦截运行时代码直接赋值 `Sprite` / `runtimeAnimatorController`，以及用 `new GameObject`、`AddComponent`、`Instantiate` 生成最终 UI 视觉。
- 花卉图鉴的花图、土壤和锁定卡现已作者化到 Scene；运行时只切换预置节点。2026-08-09 的 AutoSetup 41 又补强了锁定卡整对象隐藏，防止未解锁卡片泄露预览花朵。

## 2026-08-09 每周培育与情绪花图鉴回归修复
- `WeeklyGardenPanelStub` 现在将空日期的瓶内 `FlowerImage`、`UIbar/Growth` 与 `SoilImage` 一并隐藏；三个 UIbar 信息区无花时统一显示 `---`，有花时只填入真实日期、情绪和花名/状态。
- `SceneAuthoredImageVariantView.cs` 已从面板脚本中拆为独立序列化组件，Scene 中的变体绑定不再依赖内嵌 MonoScript；No.028 月晕在图鉴列表绑定为 `悲伤|angel|1`。
- 图鉴 Scene 默认显示前三张已收集花的真实花枝与土壤，锁定卡不显示花、土壤或解锁内容；详情页每个花型拥有独立 `Variant`，其中 `FlowerArt` 与 `SoilImage` 可在 Scene 中分别调整。
- `WorldMapFlowerSoilLayoutWindow` 的详情复用按同名 `Variant_XX` 配对复制花枝和土壤布局；`AutoSetup` 已升级到 43 并重新保存 `WorldMap_Main.unity`。
- Unity 编辑器重新编译和 AutoSetup 43 已成功；日志仅保留仓库已有的恶魔孤独完整花资源缺失提示，未新增编译错误。PlayMode 的翻周、空日和详情点击仍需人工复验。

## 2026-08-10 每周种植集中信息栏与瓶子选择
- `Panel_WeeklyGarden/Content/UIbar` 现在是场景中唯一的底部详细信息栏；`Day0`~`Day6` 及 `CellTemplate` 下不再保存每日 UIbar。
- `WeeklyGardenPanelStub` 的集中 UIbar 默认显示当前周当天信息；点击瓶子后显示对应日期，点击 `BlankClickArea` 空白区域后清除选择并恢复当天信息。点击已选瓶子不会取消选择。
- 新增 `WeeklyGardenBottleInteraction.cs`：瓶子悬浮时只缩放瓶子本体；选中时只激活场景中预置的 `SelectedHighlight` 外圈，不改变瓶子颜色，也不在运行时创建视觉节点。
- `WorldMapEmotionGardenUIPatch` 已作者化 1 个 `UIbar`、1 个 `BlankClickArea` 和 7 个瓶子高亮节点；AutoSetup 已升级到 45 并重新保存 `WorldMap_Main.unity`。
- Unity 已完成脚本重新导入、编译和 AutoSetup 45；Scene/Runtime 视觉契约检查通过。实际悬浮、点击瓶子、点击空白和 UIbar 文本切换仍需 PlayMode 人工复验。

## 2026-08-10 瓶子选中高亮修正
- 之前的 `SelectedHighlight` 使用 Unity UI `Outline` 复制完整瓶子 Sprite，即使 Image 本体透明，Outline 仍会产生整只瓶子的染色轮廓，视觉上不是边缘高亮。
- 已新增 `Assets/_Project/Art/WorldMap/UI/garden_week/SpriteAlphaOutline.shader` 与 `SelectedBottleOutline.mat`；材质只根据瓶子 Sprite 的 Alpha 邻域输出透明边缘，不输出瓶子内部填充。
- `WorldMapEmotionGardenUIPatch` 已移除每个 `SelectedHighlight` 上的旧 `Outline` 组件，并将边缘材质作者化到 7 个高亮节点；AutoSetup 已升级到 46 并重新保存 `WorldMap_Main.unity`。
- Unity 已完成 Shader 导入、材质创建、脚本编译和 AutoSetup 46；Scene/Runtime 视觉契约通过。最终高亮线粗细和颜色仍建议在 PlayMode 目视确认。

## Workspace Identity
- 项目名称：Gemini-Lab
- 项目类型：Unity 2D 桌宠客户端（长期目标仍保留 AI 陪伴方向）
- Unity 版本：`2022.3.62f3c1`
- 当前阶段目标体验：让宠物在公寓场景中完成基础移动、家具展示、交互与状态显示；现阶段暂不接入大模型，桌宠主要由玩家直接控制移动
- 当前协作工具：Unity MCP、嵌入式 Unity Skills、`.cursor/skills/` 与 `.agents/skills/`

## 记忆分层
- L1 当前任务卡：`docs/current-task-card.md`
  - 只保存当前这一轮任务目标、边界、完成标准和明确不做项。
- L2 常驻项目记忆：`docs/ai-memory/`
  - 保存长期稳定规则、结构、文件导航、历史决策。
- L3 完整历史：
  - git 历史、PR 历史、长文档与旧阶段记录。
  - 仅支持搜索，不默认整段加载。

## 当前工作方式升级
- 当前项目已开始把智能体工作方式从“长会话自由推进”收口为：
  - 新需求先复述理解
  - 用户确认后再执行
  - 当前任务只做当前任务
  - 发现别的问题只提醒，不顺带处理
- 当前项目已新增一条视觉一致性硬规则：
  - `Play` 视图与 `Scene` 视图必须一致
  - 视觉结果必须优先作者化到 `Scene / Prefab / Inspector`
  - 运行时脚本不能让 `Scene` 中已调好的视觉结果在 `Play` 中失效
- 当前默认使用“探索 → 规划 → 行动”三段式：
  - 探索：只读检查，不改文件
  - 规划：复述理解、列边界、等待确认
  - 行动：确认后才改文件、改场景、执行 git
- 当前推荐按任务类型装配最小必要上下文，不默认把所有项目文档都当作当前任务上下文；具体见 `docs/workflow-context-packages.md`。
- 当前已启用最小闭环任务闸门：
  - `docs/current-task-card.md`
  - `docs/current-task-card.json`
  - `tools/check-task-gate.ps1`
- 渐进式上下文压缩已纳入设计，但当前默认不启用自动压缩；现阶段只保留人工分层方案。
- 做梦整理、L2 技能手册、L3 知识库已纳入第二部分建设，当前以人工整理、skill 沉淀和索引增强为主。
- 第一批 workflow skill 已开始落地：
  - `git-sync-upstream-main`
  - 用于安全同步本地 `main` 到 `ULookup:main`，并在同步后切回用户原来的工作分支
  - `unity-clear-generated-cache`
  - 用于只清理 `Library/ScriptAssemblies`、`Library/Bee`、`Temp`，让 Unity 在当前源码状态下重新完整编译
  - `apartment-scene-rollback-to-commit`
  - 用于只回退 `Apartment_Main.unity` 到指定 commit，避免把场景回退误扩大成整个项目回退
  - `pet-animation-reference-rebuild`
  - 用于只修复 `Pet_Angel` 的动画资源、clip、controller 与场景绑定引用链
  - `furniture-binding-check`
  - 用于只读盘点 Apartment 家具绑定状态，并明确区分脚本层、场景层与资源层问题

## 当前状态
- 当前仓库已经从“文档与工程骨架先行”推进到“文档 + 原型实现并行”阶段。
- `AGENTS.md`、`docs/` 与 `docs/ai-memory/` 于 2026-04-21 建立；2026-04-27 完成 fork 主线同步并补齐 Git 工作流文档。
- `Assets/_Project/` 下已经存在真实运行时代码、场景、asmdef、测试程序集、示例美术资源与动画资源。
- `2026-06-02` 已新增 `Assets/_Project/Scripts/Editor/Build/McpNuGetPlayerImportGuard.cs`，用于把 `Assets/Plugins/NuGet` 下由 Unity MCP 依赖解析器落地的 `McpPlugin / SignalR / Microsoft.Extensions.*` DLL 统一校正为 `Editor-only`，避免 Windows Player Build 在 Burst AOT 阶段因带版本号文件名的预编译程序集解析失败。
- `2026-06-02` 同日先把 4 个 Unity MCP 注册表包嵌入到了 `Packages/`，随后又按“临时停用、保留恢复能力”的方案把它们从 `Packages/` 软移出到 `PackageBackups/MCP-disabled-2026-06-02/`；当前停用范围仅限 `com.ivanmurzak.unity.mcp`、`com.ivanmurzak.unity.mcp.animation`、`com.ivanmurzak.unity.mcp.particlesystem`、`com.ivanmurzak.unity.mcp.probuilder`，`Packages/SkillsForUnity` 明确保留不动。
- `2026-06-02` 同日又把 `Assets/Plugins/NuGet` 及其 `.meta` 从活动资源路径软移出到 `PackageBackups/NuGet-disabled-2026-06-02/`，并清掉了 `ProjectSettings/ProjectSettings.asset` 中 `Standalone` 的 `UNITY_MCP_READY`；当前这样做是为了让 Burst 不再从活动项目路径扫描到 `ReflectorNet / SignalR / Microsoft.Extensions.*` 残留 DLL。
- `2026-07-12` 已完成 HubUI 每日抽签页面的 UI 工具与预览系统收口：
  - `DebugDisplayWindow` 新增 `RefreshPreviewObjects()` 静态方法：Toggle Tarot Preview 开关时自动刷新场景中已 disable 的 `ReadingBubble` / `TarotSummaryPreview` 对象，解决 Scene 视图中预览 UI 被禁用不可见的问题
  - 新增编辑器工具 `ReadingBubbleLayoutSync`（`Tools → Gemini-Lab → Sync Reading Bubble Layouts`）：按 Angel / Devil 关键字分组同步气泡 RectTransform（位置、大小），递归同步所有同名子物体，支持 Undo
  - 新增编辑器工具 `SaveSlotTemplateCreator`（`Tools → Gemini-Lab → Create or Update Slot Template`）：在当前场景 Panel_SaveSlots 下创建/更新 SlotTemplate 模板，不销毁面板其他部分，自动连线 `_slotTemplate` 字段
  - `SaveSlotsPanel` 重大重写：加 `[ExecuteAlways]` 实现编辑器预览；槽位行改用模板克隆（`Instantiate(_slotTemplate, _slotContainer)`）替代纯代码生成 UI；新增 `_slotTemplate` 序列化字段，用户在 Scene 中直接编辑模板即可统一修改美术资源，Play 视图与 Scene 视图完全一致；移除了会误删场景对象的 `OnDisable` / `ClearEditorPreview` 链
  - `SettingsAndSaveSlotsPanelAuthoring` 同步更新：`BuildSaveSlotsPanel` 现在会创建 SlotTemplate（inactive），作为 `SaveSlotsPanel` 的运行时克隆模板
  - 新增长期规则 #12（记录在 `gemini-lab-memory-rules-and-history.md`）：任何涉及修改 Unity scene 文件、场景 GameObject 或组件属性的操作，必须先停下来询问用户确认，不得擅自执行
  - 事故记录：`SaveSlotsPanel.OnDisable()` 中的 `ClearEditorPreview()` 使用 `DestroyImmediate` 清空 `_slotContainer` 子物体；Unity 脚本重编译触发 OnDisable→OnEnable 周期时，用户手动调好的 `Slot_slot_1` 被自动删除。教训：编辑器回调（OnEnable/OnDisable/OnValidate）中绝对不能执行任何会修改场景的操作
- `2026-07-28` 已修复 WorldMap 桥面行走逻辑的脚本侧收口：
  - `WorldMap_Main.unity` 中桥对象 `桥` 的 `PolygonCollider2D` 上侧轮廓现在是桌宠过桥移动轮廓的唯一事实源
  - `WalkableSurface.TryGetSurfaceY` 在存在启用的 `PolygonCollider2D` 时，会按当前世界 X 求 polygon 边交点并取最高 Y；不再使用 `_profileLocalPoints` 独立折线轨道
  - `WorldMapSceneObjectsPatch` 不再回填 `_useProfile/_profileLocalPoints`，只确保桥对象有 `WalkableSurface` 且保留现有 `PolygonCollider2D` 点位
  - `PetController` 的 `WalkableSurface` 刷新帧初值已修正，避免 `int.MinValue` 帧差溢出导致首次刷新被跳过
  - `PetController.ResolveGroundY` 现在把桥面 surface Y 视为脚底/行走锚点高度，并通过 `_sortingAnchor`、`CapsuleCollider2D` 底部或 `SpriteRenderer` 底部换算成 transform Y，避免中心 pivot 贴桥导致脚底下穿
  - `tools/check-task-gate.ps1 write`、`git diff --check`、`dotnet build GeminiLab.Modules.Pet.csproj --no-restore`、`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 已通过；Unity PlayMode 仍需人工验证
- `2026-07-29` 已接入 WorldMap 情绪花图鉴列表页与详情页 UI 美术资源的脚本侧和 authoring 入口：
  - 资源来源为 `Assets/_Project/Art/WorldMap/UI/flowerCodex` 与 `Assets/_Project/Art/WorldMap/UI/flower_info`
  - `FlowerCollectionPanelStub` 已改为 Scene/Inspector 友好结构：运行时只读取 `IEmotionGardenService.GetAllClusters()`、填充卡槽/详情文本、切换 `CodexView` 与 `DetailView`，不再生成旧滚动列表最终视觉
  - `WorldMapEmotionGardenUIPatch.SetupFlowerCollectionBookContent` 会在 `Panel_EmotionCollection/Content` 下作者化真实 UI 子节点：书本背景、12 个图鉴卡槽、未知卡、左右翻页、关闭、详情页花图插槽、库存条和文本字段
  - 当前图鉴卡片与详情页已通过 `EmotionFlowerArtCatalog` 绑定 `Assets/_Project/Art/WorldMap/flower` 的真实花朵 Sprite，运行时按情绪类型、培育者和解锁阶段显示对应基础/完整花图
  - `AutoSetup` 已升级到版本 23；本轮仍保留 `WorldMapEmotionGardenUIPatch.Patch()` 的自动作者化入口，但本机 batchmode runner 在 Unity 启动阶段超时，scene 落盘改由下次编辑器初始化时的 `AutoSetup` 兜底重跑
  - `tools/run-unity-editor-method.ps1` 已改为可靠 batchmode runner：默认带 `-nographics`，通过子进程 watchdog 监控 Unity；如果启动阶段长期不创建日志或总执行超时，会停止本次 Unity PID 并返回非 0，不再无期限阻塞 PowerShell
  - 为解除 Unity 打开阻塞，`Packages/manifest.json` 中 `com.kirurobo.uniwinc` 已从失效本地 `file:` 路径改为官方 GitHub UPM URL，`Packages/packages-lock.json` 记录 hash `304f9ba2aa4a8fae7f3c71f38118c44722a2f6cc`
  - `WorldMap_Main.unity` 已存在 `CodexView`、`DetailView`、`TitlePlate`、`CategoryTabs`、`CodexCardSlot_00...11`、`StockPlate`、`FlowerImage` 等图鉴 UI 节点；`Logs/UnityBatchmode/WorldMapEmotionGardenUIPatch.codex-list.log` 记录了成功执行
  - `UIRouter.Open` 现在会在打开新顶层面板前先关闭当前已开的顶层面板，因此 `Panel_EmotionInput`、`Panel_EmotionCollection` 这类入口会互斥切换，不再同时叠在一起
  - `tools/check-task-gate.ps1 write`、`git diff --check`、`dotnet build GeminiLab.Modules.HubUI.csproj` 与 `dotnet build Assembly-CSharp-Editor.csproj` 已通过；PlayMode 点击、详情切换和最终视觉微调仍需在 Unity 中按 `docs/manual-validation-checklist.md` 的 B10 章节人工验证
  - `EditorBootSceneLoader` 现在也会手动触发 `EmotionGardenRuntimeBootstrap` 的 Awake，避免编辑器直启 `WorldMap_Main` 时情绪花园服务没注册
  - `WeeklyGardenPanelStub` 现在会在每个瓶子里显示按情绪类型和培育者映射的 `FlowerImage`；无匹配花图时才回退到 `Assets/_Project/Art/WorldMap/flower` 的 `种子 / 幼苗`，并订阅提交 / 开花 / 清空事件自动刷新
- `2026-08-09` 已修正每周种植面板 UIbar 的成长阶段 icon：
  - `Growth` 已从每日格根节点迁移到 `Day0~Day6/UIbar/Growth`，可直接在 Scene / Inspector 中调整大小与位置；隐藏的 `CellTemplate` 使用同一结构
  - icon 不再使用通用 `UI/growth/bud.png`，而是使用 `Assets/_Project/Art/WorldMap/花朵图鉴/花朵/` 下按天使/恶魔与九种情绪匹配的18张花头资源
  - `WeeklyGardenPanelStub` 在有当天种植记录时只切换 Scene 中预置的 `flower-head` 变体，无记录时隐藏；瓶内 `FlowerImage` 仍保持带枝叶完整花图规则
  - AutoSetup 已升级到39并重新保存 `WorldMap_Main.unity`；场景序列化核对确认8个 `UIbar/Growth` 均位于 `(-96, 18)`、尺寸为 `36×36`，并包含1个预览图与17个子变体，合计覆盖18种花头
- `2026-08-09` 已修复每周培育与图鉴显示回归：
  - `weekUI.psd` 中的 `bottle` 与六个复制层只是七天排版副本，不是培育者/阶段变体；`WeeklyGardenPanelStub` 现在始终显示 Scene 中作者化的 `bottle.png`，不会因当天存在花卉数据而隐藏瓶子
  - 隐藏模板与 `Day0~Day6` 的旧 `DayLabel/DayText` 均已停用，只保留 Mon～Sun 图片
  - 12 个图鉴卡片在 Scene 中默认保存为锁定安全态；锁定时 `FlowerImage`、`SoilImage` 和 `UnlockedContent` 整体关闭，运行时解锁后再启用并切换预置花图
  - AutoSetup 已升级到41并保存 `WorldMap_Main.unity`；HubUI 与编辑器程序集构建0错误
- `2026-08-06` 已修复 WorldMap 情绪花图鉴 UI 资源目录移动后的引用失配：
  - `WorldMapEmotionGardenUIPatch` 现在从 `Assets/_Project/Art/WorldMap/UI/flowerCodex` 与 `Assets/_Project/Art/WorldMap/UI/flower_info` 加载拆分 Sprite
  - 已重新执行图鉴 UI Scene authoring 并保存 `WorldMap_Main.unity`；场景 YAML 已恢复当前 `book`、`card`、`close`、左右箭头、`unknow`、`stock` 等资源 GUID 引用
  - `dotnet build GeminiLab.Modules.WorldMap.csproj --no-restore` 与 `dotnet build Assembly-CSharp-Editor.csproj --no-restore` 均通过；PlayMode 中图鉴打开、关闭和详情切换仍需人工复验
  - `WorldMapEmotionGardenUIPatch` 现在会作者化 `Growth` 子节点并绑定 `_growthSprites`，确保 Scene 里能直接调整瓶内成长层的布局与大小
- `2026-08-06` 已接入 WorldMap 正式花卉美术资源：
  - 新增 `Assets/_Project/Scripts/Modules/EmotionGarden/EmotionFlowerArtCatalog.cs` 与 `Assets/_Project/Art/WorldMap/flower/EmotionFlowerArtCatalog.asset`，按 9 种情绪 × 天使/恶魔绑定基础花图和完整花图
  - `FlowerCollectionPanelStub` 的图鉴卡片与详情会按花类型显示对应 Sprite；`WeeklyGardenPanelStub` 的每日格新增 `FlowerImage`，按当天花卉数据显示对应 Sprite
  - 每周面板的成长回退资源改为 `Assets/_Project/Art/WorldMap/flower/种子.PNG`、`幼苗.PNG`；`恶魔-孤独（完整）.PNG` 缺失时回退到基础花图
  - 已重新执行 `WorldMapEmotionGardenUIPatch.Patch()` 并保存 `WorldMap_Main.unity`；编辑器、运行时脚本构建通过，PlayMode 图片大小与层级仍需人工复验
- `2026-08-06` 已修正 WorldMap 每周培育面板的 UIbar 用途与整体尺寸：
  - `Assets/_Project/Art/WorldMap/UI/garden_week/UIbar.png` 现在由 `WorldMapEmotionGardenUIPatch` 作者化到每个 `Day0`~`Day6` 瓶子下侧，不再作为 `Content` 顶部装饰
  - 每个 UIbar 下挂 `DateText`、`EmotionText`、`FlowerLanguageText`，`WeeklyGardenPanelStub` 按当天花卉数据填充日期、情绪关键词、花名/花语和开花状态；无花时清空为 `--`
  - `Panel_WeeklyGarden` Scene 根 `localScale` 固定为 `0.85, 0.85, 1`；每日 Cell、瓶子、UIbar 和成长图标已同步放大，UIbar 根节点 `localScale` 为 `1.5, 1.5, 1`，内部字号为 `18/16/18`；相关路径统一为 `UI/garden_week` 与 `UI/growth`
  - `dotnet build GeminiLab.Modules.HubUI.csproj --no-restore` 与 `dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过；最终 Game View 文字溢出和视觉间距仍需人工确认
- `2026-08-06` 已修正 WorldMap 花卉成长与图鉴资源状态边界：
  - `WorldMapEmotionGardenUIPatch` 现在从 `Assets/_Project/Art/WorldMap/UI/growth/seed.png`、`bud.png` 加载成长资源；不再使用 `flower/种子.PNG`、`幼苗.PNG`
  - `WeeklyGardenPanelStub` 仅在 `GrowthState.Growing` 显示 `bud.png`；`GrowthState.Bloomed` 隐藏成长图并通过 `FlowerImage` 显示对应带枝叶 `（完整）.PNG`
  - `FlowerCollectionPanelStub` 的已解锁卡片和详情始终请求 `GrowthState.Bloomed`，图鉴不显示 `bud.png` 或仅有花朵的未开花资源
  - `EmotionFlowerArtCatalog` 与作者化数据不再在缺少 `（完整）.PNG` 时回退到基础花图；当前 `恶魔-孤独（完整）.PNG` 缺失，该组合的完整花图会保持为空，等待正式资源补齐
- `2026-07-30` 已收口 WorldMap 室内入口点击优先级与双宠室外碰撞：
  - 通用点击裁决工具 `ClickOcclusionUtility` 已收在 `Assets/_Project/Scripts/Core/DevMode.cs`
  - `CabinReturnPortal`、`WorldMapGardenZone`、`ClickableSceneObject`、`BaselineItem`、`PetPlayerInputController`、`PetClickReactionController` 与 `WorldMapCameraController` 现在都会先判断鼠标点下的最上层 2D 点击目标再决定是否响应
  - `PetController` 新增 WorldMap 场景级双宠碰撞忽略逻辑，`Pet_Angel` 与 `Pet_Devil` 在 `WorldMap_Main` 中不会再互相挡路
  - `dotnet build Assembly-CSharp-Editor.csproj` 与 `git diff --check` 已通过；房子被桌宠或 UI 遮挡时不响应、以及双宠贴身经过时的实际 PlayMode 体感仍需人工补验
- `2026-07-31` 已恢复情绪花园的真实种植逻辑接线：
  - `EmotionFlowerModels` 中新增 `EmotionFlowerCatalog` 本地目录表，负责 9 种情绪的轻量文本判定，以及 `angel / demon` 两位培育者对应的 18 个花名映射
  - `EmotionGardenService` 现在会在提交时生成真实花名，并把花名写入 `EmotionFlowerData.FlowerName`
  - `EmotionInputPanelStub` 不再提交固定“悲伤”，而是读取心情文本后交给情绪花园服务判定；提交成功后会自动切到 `WeeklyGardenView`
  - `WeeklyGardenPanelStub` 现在会按真实数据展示花名、情绪、培育者和开花状态，并在空格回退时恢复默认瓶子底图
  - `FlowerCollectionPanelStub` 现在按情绪顺序 + 培育者顺序展示图鉴，点击已解锁卡片后进入详情页
  - `dotnet build GeminiLab.Modules.EmotionGarden.csproj --no-restore`、`dotnet build GeminiLab.Modules.HubUI.csproj --no-restore` 与 `dotnet build Assembly-CSharp-Editor.csproj --no-restore` 已通过
- `2026-04-28` 已开始推进任务 1：现有场景家具接入 `FurnitureService`，并让 Apartment 场景里的状态/库存/概览面板显示真实运行时数据。
- `2026-04-28` 已完成任务 2 的首轮范围确认，并把现有 `Move` 动画 controller 显式绑定到 `Apartment_Main.unity` 中的 `Pet_Angel`。
- `2026-04-28` 已基于新增美术资源补上两个交互动画 clip：`Interact_Read` 与 `Interact_BesideDoor`，并把它们接进现有 `Pet_Angel.controller`。
- `2026-04-29` 已把 `公寓场景.psd` 备份到原目录，并把 PSD Importer 子资源转为独立 Sprite；当前第一轮实际用于家具整理的资源已开始放入 `Assets/_Project/Art/Sprites/Furniture/**/`，并按中文语义命名维护。
- `2026-04-29` 在 `Apartment_Main.unity` 中追加了一次“仅家具层补景”的试验：当前会在 `Furniture` 下额外挂一个 `StaticFurnitureDecorOnly`，用于承载与交互逻辑无关的纯静态补景家具。
- `2026-05-01` 已开始把公寓场景里真实存在的关键家具对象显式接入家具系统：
  - 新增 `SceneFurnitureDefinitionHint`
  - 新增 `ApartmentSceneFurnitureBindings`
  - `FurnitureService` 现已支持优先读取场景显式提示，而不是只靠名称推断
  - `Apartment_Main.unity` 的 `Furniture` 根当前已配置首批 8 个关键对象：天使床、天使床头柜、天使竖琴、天使工作桌、恶魔工作桌、天使书柜、天使花盆方桌、天使底部盆栽
- `2026-05-01` 已开始补“最优先的 3 个交互类型”代码：
  - `睡眠交互`
  - `装饰观察`
  - `休闲交互`
  当前交互类型已经进入 `FurnitureDefinitionSO`、`FurnitureInteractionTarget`、`PetRuntimeData` 和状态面板链路，不再只是口头分类。
- `2026-05-01` 已进一步把一部分家具从“类别级交互”推进到“对象级交互类型”：
  - `上床休息`
  - `看书柜`
  - `照镜子`
  - `整理床头柜`
  - `演奏竖琴`
  - `弹吉他`
  - `看画架`
  - `看照片板`
  - `观察植物`
  - `地毯休息`
  - `沙发休息`
  - `坐下休息`
- `2026-05-02` 已继续把第二批对象接入场景与交互链路：
  - 场景里已补进或绑定：镜子、恶魔地毯、恶魔沙发、恶魔凳子、天使凳子、恶魔画架、恶魔照片板、恶魔椅子
  - `ApartmentSceneFurnitureBindings` 当前已从最初的 5 个关键对象扩展到覆盖主要可交互家具与第二批静态装饰对象
- `2026-05-02` 已继续把第三轮装饰类对象补进对象级交互与场景绑定：
  - 纸张
  - 耳机
  - 音响
  - 音响和乐器
  - 柜子
  - 储物家具
- `2026-05-02` 已继续把第四轮剩余可做装饰对象接入对象级交互或场景绑定：
  - 窗台
  - 窗台上的盆栽
  - 床上玩偶
  - 沙发上枕头
- `2026-05-02` 已对 Apartment 家具交互链路做一轮精修：
  - 清理 `ApartmentSceneFurnitureBindings` 中的重复 `_target` 绑定
  - 把 `花盆方桌` 的场景定义从误写的装饰类修正为 `WorkDesk`
  - 为 `窗台 / 玩偶 / 枕头` 补上更贴近对象语义的对象级交互类型
  - 让 `小圆镜`、`园地毯`、`左下小家具`、`左下窄家具` 与当前脚本推断口径重新对齐
- `2026-05-03` 已继续把“已有资源但未完全落场景”的对象收口到 `Apartment_Main.unity`：
  - 修正 `照片板` 的空绑定 `_target`
  - 将 `园地毯` 接入场景显式绑定
  - 将 `小圆镜 / 羽翼边柜 / 恶魔盆栽 / 左下小家具 / 左下窄家具` 补入 `StaticFurnitureDecorOnly` 并接入交互绑定
- `2026-05-03` 已同步适配桌宠新版移动美术资源：
  - `Assets/_Project/Art/Sprites/Pet/Frames/Move/` 当前改为 `正面 / 背面 / 侧面` 三个子目录
  - `PetMoveAnimationSetupEditor` 现会优先读取这三个子目录，旧版 `Pet_Angel_Move_{Front|Back|Side}_0001...` 仍作为兜底
  - `Pet_Angel_Move_Front.anim`、`Pet_Angel_Move_Back.anim`、`Pet_Angel_Move_Side.anim` 已切换到新版移动帧引用
  - 运行时移动表现继续采用“前后分离、左右共用侧面并通过 `SpriteRenderer.flipX` 翻转”的四方向规则
- `2026-05-06` 已开始把 Apartment 家具交互链路从“场景直挂 + 运行时兜底”推进到“真实作者化资源”：
  - 已为当前 `Art/Sprites/Furniture/**/` 下的全部 `49` 个家具 Sprite 资源生成对应 `FurnitureDefinitionSO`
  - 已为同一批 `49` 个家具资源生成对应 `Furniture` Prefab
  - `Apartment_Main.unity` 已开始转成使用这些真实 Prefab 实例
  - `FurnitureService` 现会优先使用场景对象上已赋值的真实 `FurnitureDefinitionSO`
- `2026-05-07` 已完成 `Interact` 资源命名规范收口：
  - `read/` 组重命名为 `Pet_Angel_Interact_Read_0001...0006.png`
  - `beside door/` 组重命名为 `Pet_Angel_Interact_BesideDoor_0001...0005.png`
  - `Assets/_Project/Art/Sprites/Pet/README.md` 已改为“方向型帧保留方向字段，非方向型交互帧允许使用变体名”的真实规则
- `2026-05-08` 已把 Apartment 原型里的桌宠行动入口调整为玩家直接控制：
  - 新增 `PetPlayerInputController`
  - 新增 `PetPlayerFurnitureInteractionController`
  - 新增 `PetClickReactionController`
  - 新增 `PetClickResponseLibrary`
  - `Pet_Angel` 现支持 `WASD` 与方向键移动
  - `Pet_Angel` 现支持靠近指定家具或交互点时按 `F` 触发玩家手动交互
  - `Pet_Angel` 现支持鼠标左键点击后输出当前表情 Debug 信息，并弹出本地语料气泡回复
  - `PetController` 在检测到玩家输入组件后，会停止自主移动链路，改由玩家驱动 `Idle / Moving`
  - 当前文档口径同步收口为：现阶段暂不接入大模型，Gateway / Travel / AI 对话仍保留为后续规划
  - 同期修正 `FurnitureLayoutPersistence` 与 `ApartmentSceneFurnitureBindings`：
    - 当前默认禁用家具布局自动恢复
    - 家具存档只记录运行时摆放家具，不再清掉场景预摆家具
    - `ApartmentSceneFurnitureBindings` 现可按 `definitionId / Sprite 名 / 现有 hint` 自动找回缺失 `_target`
  - 同期补入 `Idle` 三视图与 `Sleep` 动画资源接线：
    - 新增 `Pet_Angel_Idle_Front.anim`
    - 新增 `Pet_Angel_Idle_Back.anim`
    - 新增 `Pet_Angel_Idle_Side.anim`
    - 新增 `Pet_Angel_Sleep.anim`
    - `Pet_Angel.controller` 新增 `Idle_Front / Idle_Back / Idle_Side / Sleep`
    - `PetController` 当前会在静止时切到 `Idle_*`，在 `SleepingState` 时切到 `Sleep`
- `2026-05-26` 已完成 Apartment UI 的 P0 清理收口：`Apartment_Main.unity` 中旧的占位 UI 残留已从场景真实移除：
  - 已移除 `TopLeft_StatusPanel`
  - 已移除 `Right_InventoryPanel`
  - 已移除 `BottomRight_PersonalityRadar`
  - 已移除旧的 `SpaceSystemPrototypeRoot` 原型 UI
  - 当前保留的新主界面骨架包括 `Panel_PetStatus`、`Panel_SpaceSys`、`Sidebar`、`SidebarOverlay`、`ApartmentViewportHost`、`ApartmentViewportImage` 与 `ApartmentViewportCamera`
  - 公寓 viewport 当前归属 `Panel_SpaceSys`，不再挂在 `Profile / Panel_PetStatus`
  - 后续 UI 制作继续改用 `Assets/_Project/Art/UI/` 下的新美术资源做正式作者化
- `2026-05-26` 已撤回 Apartment UI 的 `P1 + P3` 资料卡美术资源绑定尝试：
  - `Apartment_Main.unity` 中 `Panel_PetStatus` 不再保留刚才绑定的 `profile` 贴图、宠物正面待机预览 Sprite、雷达配色与尺寸微调
  - 具体 UI 美术资源选择、贴图映射与最终视觉作者化后续由人工完成
  - AI 后续只继续承接弱视觉或非美术资源相关的逻辑、结构、输入桥接、验证与文档任务
- `2026-05-22` 已把 `Pet_Devil` 接入 `Apartment_Main.unity`：
  - 公寓场景里的 `Pet` 根节点现在包含 `Pet_Angel` 与 `Pet_Devil`
  - `Pet_Devil` 已接入自己的 `Pet_Devil.controller` 与恶魔 `Move / Idle / Sleep` 动画资源
  - 玩家输入链路已补成“双宠点击切换主控”：默认天使可控，点击恶魔后切换为恶魔可控，避免双宠同时响应同一套方向键
  - 同日已修正双宠控制细节：未被选中的桌宠不再继续运行自动 FSM 并触发 `Sleep`，而是保持 `Idle`；点击桌宠时会显式接管 `PetPlayerInputController` 控制权，确保切到恶魔后键盘移动真实生效
  - 同日已修正恶魔无法移动的场景边界问题：`Pet_Devil` 不再复用只覆盖天使右侧区域的共享 `PetMovementBounds`，而是改为绑定左侧 `PetMovementBounds_Devil`
- `2026-05-23` 已为恶魔补上门边“左右看”交互动画像：
  - 新增 `Assets/_Project/Animations/Pet/Pet_Devil_Interact_BesideDoor.anim`
  - `Pet_Devil.controller` 的 `Interact_BesideDoor` 已改接恶魔自己的 clip，不再继续引用天使门边动画
  - `Apartment_Main.unity` 中恶魔现有 `门边 / Interact_BesideDoor / beside door` 触发方式保持不变
- `2026-05-27` 已把恶魔两条新的自交互动画接入 Apartment 原型：
  - 新增 `Assets/_Project/Animations/Pet/Pet_Devil_Interact_Write.anim`
  - 新增 `Assets/_Project/Animations/Pet/Pet_Devil_Interact_PlayingMusic.anim`
  - `Pet_Devil.controller` 的 `Interact_Write` 与 `Interact_PlayingMusic` 已改接恶魔自己的 clip，不再继续引用天使对应交互动画像
  - `Apartment_Main.unity` 中恶魔现有玩家交互绑定已改成 `画画 / 玩掌机`：`画画` 对应 `家具_休闲_画架_恶魔_01` 的交互点并坐到 `家具_装饰_椅子_恶魔_01`，`玩掌机` 坐到 `家具_装饰_沙发_恶魔_02`
  - 为保证这两条交互时恶魔不会被指定家具遮挡，当前场景绑定已关闭它们的 `UseTargetSortingWhileInteracting`，沿用恶魔自身较高的默认排序层
- `2026-05-25` 已为 Apartment 场景补出第一版 viewport 结构骨架：
  - `Panel_SpaceSys/Content` 下新增 `ApartmentViewportHost`
  - 其下新增 `ApartmentViewportImage`，当前引用 `Assets/_Project/Settings/RenderTextures/ApartmentViewport_RT.renderTexture`
  - `ArtGenerated` 下新增独立 `ApartmentViewportCamera`，不再依赖挂在 `Pet_Angel` 下的主相机来承担未来视窗职责
  - 后续已补最小输入桥接：当前 `ApartmentViewportInputBridge` 会先转发桌宠点击，再尝试转发到 `PetPlayerFurnitureInteractionController` 的家具交互入口
  - 后续已继续补建造模式桥接：当 `BuildModeController` 开启时，viewport 内左键/右键会优先转发到放置/删除家具入口，并屏蔽旧的全屏 `Camera.main` 鼠标链路双触发
  - `2026-05-26` 已对非美术 UI 技术链路做一轮收口：viewport 坐标转换抽成可测试方法，点击只在 RawImage 矩形内生效，建造模式开启时会吞掉 viewport 点击并优先交给 `BuildModeController`
  - `2026-05-26` 已补 `HubUI` 对 `Furniture` 的 asmdef 显式依赖，并新增 `ApartmentViewportInputBridgeTests` 覆盖坐标转换基础规则
  - `2026-05-26` 已补 `SidebarController` / `StubPanelBase` 的 `IUIRouter` / `EventBus` 兜底注册，便于直接打开 `Apartment_Main` 调试时面板切换仍可工作
  - `2026-05-26` 已补 `ProfilePanelStub`、`TarotPanelStub`、`InventoryPanelStub` 的服务缺失 / 空数据兜底；其中 `InventoryPanelStub` 未绑定 `_emptyHint` 时会复用 Tooltip 区域显示空状态
  - `2026-05-26` 已建立 `Assets/_Project/Prefabs/UI/Panels` 与 `Assets/_Project/Prefabs/UI/Widgets` 的 README 工程落点；本轮未绑定任何新 UI 美术资源，也未制作正式 UI prefab
  - 本轮 `git diff --check` 通过；由于当前 shell 找不到 Unity Editor / `unity-mcp-cli`，Unity Test Runner 与 PlayMode 仍需在 Unity 内补验，结果已写入 `docs/manual-validation-checklist.md`
- `Assets/_Project/Prefabs/` 与 `Assets/_Project/ScriptableObjects/` 现在都不再是完全空目录，且 `Furniture` / `FurnitureConfig` 这条线已覆盖当前全部家具 Sprite 资源；其他模块仍未完成资产作者化。
- README 系列文档描述的目标状态仍然大于当前实现范围，阅读时必须显式区分“已实现事实”和“规划目标”。
- 项目本地 skill 目录当前仍保持 `.agents/skills/` 与 `.cursor/skills/` 镜像关系，当前统计为 `72` 项。

## 当前最重要事实
1. 这个仓库不再是“只有说明文档”的空骨架，已经有一轮可运行原型；但说明文档密度依然高于最终实现密度。
2. `_Project/` 继续是自研业务代码与资源的唯一正式落点。
3. 当前已真实落地的关键内容包括：
   - 场景：`Boot.unity`、`Apartment/Apartment_Main.unity`、`WorldMap/WorldMap_Main.unity`、`Desktop/Desktop_Overlay.unity`
   - Core：`ServiceLocator`、`EventBus`、`CommandDispatcher`、FSM、`GameBootstrap`
   - 业务模块：`Pet`、`Furniture`、`Navigation`、`Gateway`、`Travel`、`Persistence`、`UI`、`DesktopOverlay`
   - 测试：`EditMode` / `PlayMode` 测试程序集与多组核心模块测试
4. 当前 Apartment 原型里的桌宠主行动方式已经调整为“玩家直接控制移动优先”，不再把自主寻路 / 大模型驱动行为作为当前阶段默认验证目标。
5. `Packages/manifest.json` 当前已经包含：
   - `com.unity.ai.navigation`
   - `com.besty.unity-skills`
   - 4 个 Unity MCP 包当前已从 `Packages/manifest.json` 临时移除，并备份到 `PackageBackups/MCP-disabled-2026-06-02/`
   - `Assets/Plugins/NuGet` 当前也已临时移出到 `PackageBackups/NuGet-disabled-2026-06-02/`
6. 当前原型里仍存在多处“占位实现 / 运行时兜底”：
   - `NavigationService` 与 `NavMesh2DRebaker` 目前更接近占位导航层，不是完整 2D NavMesh 方案
   - `WindowModeAdapter` 目前只提供模式状态与点击穿透标记，没有真正的原生透明窗口实现
   - `GatewayRuntimeHost` 在缺少配置资产时会回退到运行时创建的 Mock 配置
   - `FurnitureService` 在缺少配置资产时会补运行时家具定义
7. 当前最明显的资源层缺口仍是：
   - `Furniture` 之外的大部分 `Prefab` / `ScriptableObject` 资产仍未作者化
   - 真实人格雷达、美术更完整的交互动画与更正式的 UI 资源仍未补齐
   - `Assets/_Project/Art/UI/` 当前已经开始承载新的 UI 美术资源输入，但 Apartment 场景内对应的新 UI 还未重新作者化落地
   - `Pet_Angel` 当前已有 `Move_Front / Move_Back / Move_Side / Interact_Read / Interact_BesideDoor`
   - 但 `Idle` 与更完整的 `Emotion` 仍缺少正式资源与状态链路
8. 当前工作树不是干净状态，执行任何修改前都要先看 `git status`，避免覆盖用户现有改动。
9. 当前版本控制协作基线已经固定为 `fork + upstream + feature branch + PR` 工作流。

## 长期目标
- 做成一个真正可持续演化的 AI 桌宠项目，而不是一次性 Demo。
- 让“玩法、架构、工具、验证、文档”同时成长，不把任何一块长期欠账。
- 保持多智能体可接手：新智能体进入项目后，能在较短时间内读懂上下文并安全推进。

## 长期约束
- 所有中文文档和中文注释必须保持 UTF-8 正常显示。
- 文档里必须显式区分“已存在事实”和“规划目标”。
- `UI` 不承载业务逻辑；跨模块通信只走接口、事件或服务定位。
- ScriptableObject 资产在运行期只读，运行态状态进入 Service 或 Snapshot。
- 优先 Scene / Inspector 友好与美术替换友好，不做只能靠硬编码维持的结构。
- 视觉类结果默认要求 Scene 可见、Inspector 可调、Play/Scene 一致；不要依赖运行时脚本临时拼出最终视觉。
- `_Project/` 继续作为自研业务资产唯一落点；第三方资源不要混入其中。

## 阶段进度
| Phase | 目标 | 当前判断 |
| :--- | :--- | :--- |
| Phase 1 | 核心基础设施与 FSM 骨架 | Core、FSM、`Boot.unity`、存档骨架与测试程序集已落原型 |
| Phase 2 | V-Decor、2D NavMesh、家具交互 | 公寓场景、家具系统与导航抽象已落原型，真实导航实现仍需加强 |
| Phase 3 | OpenClaw 网关与对话/工作链路 | Gateway Client、事件路由、Mock 链路已落原型，真实配置资产与联调仍待补齐 |
| Phase 4 | UI、桌面 Overlay、旅行系统 | UI / Overlay / Travel 已有首轮代码与场景支撑，原生 Overlay 与完整用户旅程仍未收口 |

## 近期建议优先级
1. 补齐 Prefab 与 ScriptableObject 资产，把现在依赖运行时兜底的部分逐步转成真实资源作者化。
2. 把导航与桌面 Overlay 从“占位实现”推进到真实可验证实现。
3. 在 Unity 内补跑测试与场景验证，并把结果回写 `docs/manual-validation-checklist.md`。
4. 随着场景、Prefab、SO、脚本继续落地，持续更新本记忆体系与结构文档。

## 更新触发
出现以下任一变化时，必须同步更新记忆文档：
- 核心玩法规则变化
- 场景结构变化
- UI 层级变化
- 关键脚本或关键包变化
- 文件结构变化
- 已知问题状态变化
- 推荐开发顺序变化

### 2026-08-04 WorldMap 花朵自由摆放 P0-1
- 新增运行时入口 `Assets/_Project/Scripts/Modules/WorldMap/WorldMapFlowerPlacementController.cs`。
- 新增编辑器作者化入口 `Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapFlowerPlacementAuthoring.cs`，由 `WorldMapEmotionGardenUIPatch` 调用，`AutoSetup` 版本提升到 24。
- WorldMap 花朵摆放以 `Assets/_Project/Art/WorldMap/garden/中景/花丛.png` 的 Sprite 完整尺寸作为二维网格标尺；该资源只用于得到 `4.01 x 2.24` Unity 单位的网格尺寸，不作为摆放花朵显示。
- 该阶段的中性占位已由 Scene 作者化的正式单花/花丛预置变体取代；摆放控制器只读取 `花丛.png` 作为网格标尺，运行时只切换已保存的 Sprite 节点。
- 当前功能范围为：布置入口、花朵库存、单花/花丛选择、二维半透明预览、双轴网格吸附、可视化网格线、连续点击落位和 Esc 退出。摆放区域使用独立 `FlowerPlacementBounds`，不再复用仅有 `36 x 0.3` 横向尺寸的 `PetMovementBounds`；最终草地范围、排序和视觉仍需 Unity PlayMode 人工验证。

### 2026-08-10 / 2026-08-11 WorldMap 花朵摆放侧边栏作者化
- `WorldMapFlowerPlacementAuthoring` 已将旧底部库存栏改为 `WorldMap_Main.unity` 中右侧滚动式 `FlowerPlacementPanel`，面板使用 `Assets/_Project/Art/WorldMap/arrange/UIBoard.png` 原始 `498 x 899` 尺寸，条目使用 `item.png`、单花卡 `singleCard.png`、花丛卡 `tripleCard.png`、合成按钮 `synthesis.png`、勾选 `tick.png`、上下箭头和提示气泡等正式资源。`Btn_FlowerPlacement` 已落在右下角。
- 花卉资源不从 `arrange.png` 裁剪：18 个“培育者 + 情绪”组合的花种标题取 `花朵图鉴/花朵`，单花取 `花朵图鉴/花枝/*（完整）.PNG`，花丛取 `花朵图鉴/花丛/*（花丛）.PNG`，均由 Editor 保存为 Scene Sprite 引用。
- `WorldMap_Main.unity` 已作者化 18 个花卉条目、36 个预览变体、32 个有限落位槽及其 1152 个单花/花丛预置显示对象；运行时只切换预置对象、更新槽位位置/占用状态和文本，不创建 GameObject、UI、Sprite 或网格线。
- 网格材质 `Assets/_Project/Art/WorldMap/arrange/PlacementGrid.mat` 与 10 条竖线、5 条横线已落盘；网格单元仍严格读取 `garden/中景/花丛.png` 的 Sprite 完整尺寸 `4.01 x 2.24` Unity 单位，花丛资源本身不作为摆放显示。
- `2026-08-11` 已将条目展开改为固定详情页手风琴：`FlowerList` 是 `ScrollRect` 唯一内容与唯一纵向布局根，`FlowerSidebarContent` 仅保留层级容器。每个 `FlowerOption_00...17` 场景中都预置标题与 `ExpandedOptions`；收起高度为 `73`，展开高度为 `320`，详情页以顶部 Pivot 固定在标题下方 `-76`。`FlowerList.VerticalLayoutGroup.childControlHeight` 已保存为开启，确保展开条目的 320 高度真实参与排版，后续条目会整体下移而不覆盖详情。运行时只切换详情节点和该条目的已序列化高度，并在切换条目时补偿 ScrollRect，使被点击标题栏保持当前视窗位置；上方条目不移动。
- 当前交互为：打开右侧栏 → 展开花种 → 选择单花/花丛 → 网格与半透明预览显示 → 点击有效区域连续落位 → `Esc` 退出；同种 3 朵单花可主动合成 1 个花丛，不足时显示 `组 5.png` 提示气泡。
- `2026-08-12` 起，摆放库存已收口到 `EmotionGardenService` 的 `emotion-garden` 持久化数据：每朵花开花时给对应“情绪类型 + 培育者”增加 1 个单花库存；摆放单花/花丛与“3 单花合成 1 花丛”都直接读写同一库存，并通过 `EmotionFlowerPlacementInventoryChangedEvent` 实时刷新侧栏。存档版本升级为 3，旧存档只在迁移时按已开花记录（缺失时以累计进度补齐）初始化一次单花库存，不会在每次打开侧栏时重复补发。
- `2026-08-13` 起，存档版本为 4：成功摆放以槽位、花型、情绪/培育者和世界坐标写入 `PlacedFlowers`，原子扣除对应库存并立即串行 autosave；启动读档会恢复原槽位和坐标，不会再次扣库存。单花和花丛摆放均不附带土壤；旧的顶层 `Canvas/FlowerPlacementStatusBar` 已从场景移除，当前提示只使用侧栏内 `PlacementStatus`。
- `2026-08-13` 同步清理 `FlowerPlacementPanel` 下旧原型的 `FlowerButtons`、`SingleButton`、`ClusterButton`、`CancelButton`，并重新把 32 个 `WorldMapPlacementSlot` 的正式视觉绑定保存到 Scene；运行时恢复只切换这些已作者化节点。当前 Editor 默认使用 `Saves-Dev`，其中没有 `PlacedFlowers` 的旧记录时无法凭空恢复历史摆放，之后的新摆放会写入版本 4 存档。
- `2026-08-13` 摆放层改为读取 Scene 中的 `BaselineItem`：花卉基线与其排序层绑定，同层才做占用冲突检查，跨层允许重叠；内部吸附仍按相邻层半个单元宽度错位，但 `FlowerPlacementGrid` 不再在运行时显示，避免抽象网格误导玩家。作者化会过滤摆放区域外的基线，花丛 footprint 改为单格，视觉/占用尺寸以场景 `花丛 3` 的 `3.99 x 2.22` 碰撞体基准，点击草地时会真正提交摆放，并清理槽位遗留的重复旧组件。
- `2026-08-14` 修正落位链路：`WorldMapPlacementSlot` 与 `WorldMapPlacedFlower` 使用独立稳定脚本 GUID，运行时校验为 `32/32` 槽位、`1152` 个正式视觉绑定；槽位绑定会扫描已作者化的 `PlacedVisual_*` 子节点，并以显式占用状态参与同层冲突判断。提交期间忽略同步 `EmotionFlowerPlacementsChangedEvent` 的重入恢复，避免库存扣减成功后恢复回调清空刚落位槽位；直接槽位验证已确认绑定 `36`、占用状态为真。鼠标端到端仍需在有可用库存的 Play 存档中人工点击确认。
- `2026-08-14` 同日修正花朵与桌宠的遮挡基准：撤销错误的全局花朵前置偏移，正式花朵/预览和桌宠共享 `BaselineItem.SortingOrder` 主层级，并在同一主层级内按基线 Y 二次排序（Y 越低越靠前，完全同线时桌宠仅提高 1 个排序单位）。`WorldMap_Main.unity` 的 1152 个预置视觉统一保存为 `Default` Sorting Layer，槽位显示树内所有 SpriteRenderer 也会同步该深度键。AutoSetup 63 已给 `Pet_Angel`、`Pet_Devil` 场景根对象保存 `BaselineItem`，基线取碰撞体底部、`solidCollider=true` 且不参与可种植层收集。
- `2026-08-12` 同步修正 `FlowerSidebarViewport`：拉伸锚点下改用 `offsetMin=(34,56)` 与 `offsetMax=(-34,-132)` 保存面板内边距，第一条花种固定在标题下方，列表滚动内容由 `RectMask2D` 裁剪在装饰窗口内部。独立作者化入口为 `Tools/Gemini-Lab/WorldMap/Author Flower Placement`。

- `2026-07-30` 起，`WorldMap_Main.unity` 中 `Panel_WeeklyGarden/Grid/CellTemplate` 需要保持场景里可编辑但默认不可见；`WeeklyGardenPanelStub` 会在运行时兜底隐藏模板，实际面板只应显示 7 个 Day cell。`tools/check-task-gate.ps1 write` 已通过，Scene 视图仍需补验确认没有第 8 个瓶子。
- `2026-07-30` 同轮，`Panel_WeeklyGarden/Grid` 已从自动布局改为纯容器，`Day0`~`Day6` 可以在 Scene / Inspector 里单独移动；作者化脚本不再清理现有格子位置，也不再给模板和新格子补 `HorizontalLayoutGroup` / `LayoutElement`。

### 2026-08-05 WorldMap 昼夜切换
- WorldMap 使用 `Assets/_Project/Scripts/Modules/WorldMap/WorldMapDayNightController.cs` 按 `IGameClock.Now` 切换昼夜。
- 默认 06:00–18:00 为白天，其余时间为夜晚；夜晚启用场景中的 `WorldMapNightOverlay`。
- 夜幕使用已有 `Assets/_Project/Art/WorldMap/garden/天气（最上层）/夜幕.png`，场景作者化入口为 `WorldMapDayNightAuthoring`。
- 当前 Scene 已直接保存 `WorldMapNightOverlay` 的位置、排序和控制器引用；夜幕碰撞已禁用，UI 不受其世界渲染排序影响。

### 2026-08-06 WorldMap 桌宠数字键动画调试
- `WorldMapPetAnimationTriggerController` 现在是无可视化占位物的数字键调试入口，挂在 `_SceneRoot`，不再依赖宠物位置、临时点位或 `F` 键。
- 当前数字映射为：`1` 天使坐地 `Outdoor_Sit`、`2` 天使祈祷 `Outdoor_Pray`、`3` 天使开心 `Outdoor_Happy`、`4` 天使浇水 `Outdoor_Water`、`5` 恶魔睡觉 `Outdoor_Sleep`、`6` 恶魔施法 `Outdoor_Cast`、`7` 恶魔得意 `Outdoor_Proud`；普通 Idle / Move 仍由桌宠移动状态驱动。
- `WorldMapAnimationTriggers` 及五个临时点位已从 `WorldMap_Main.unity` 删除；作者化入口会清理旧对象，AutoSetup 版本提升到 32，后续不会再次创建占位点。
- 天使 `坐地`、`祈祷` 的序列帧目录和 AnimationClip 已绑定到 WorldMap 专用 `WorldMap_Angel.controller` 的 `Outdoor_Sit`、`Outdoor_Pray` 状态；Apartment 控制器和资源不复用。
- 数字触发时调试组件在 `PetController` 更新之后持续维持特殊动画，持续时间结束后交还普通 Idle / Move；自动巡航和最终策划触发条件仍未实现。
- 数字触发期间会通过 `PetController.SetExternalMovementLock` 锁定当前桌宠的玩家输入、随机漫游和刚体速度；锁定按桌宠分别生效，动画结束或调试组件禁用后恢复。
- 天使走路资源的原始侧身朝向按左向处理，因此 WorldMap 天使的 `_sideFramesFaceLeft` 保持 `true`；恶魔配置保持原样。

### 2026-08-05 WorldMap 可交互场景物体反馈
- `WorldMap_Main.unity` 中的 `室内`、`邮箱`、`大树 1`～`大树 5` 已由 `WorldMapInteractiveObjectAuthoring` 作者化为统一可交互对象。
- 新增运行时组件 `WorldMapInteractiveObjectFeedback`：以对象在 Scene 中保存的 localScale 为基准，在通过 UI / 最上层 2D collider 裁决后平滑放大并在移出时恢复，不累计修改最终视觉。
- 邮箱和 5 棵大树接入 `ClickableSceneObject` 的序列化 `UnityEvent` 点击入口；当前只输出占位日志，具体业务交互仍待确定。
- `室内` 继续使用 `CabinReturnPortal` 返回 `Apartment`；`GameBootstrap` 增加服务缺失时的重新注册保护，降低 Editor 反复进入 Play 或关闭域重载导致 `ISceneFlowService` 缺失的概率。
- AutoSetup 版本提升到 31；PlayMode 悬停、遮挡和点击跳转仍需人工验证。
- 后续修正：`WorldMapInteractiveObjectFeedback` 改用鼠标世界坐标与自身 `Collider2D.OverlapPoint` 检测悬停，不再因大树下方的花丛等场景碰撞体排序导致反馈失效；点击入口仍保留 `ClickOcclusionUtility` 的最上层裁决。

### 2026-08-06 WorldMap 双宠动画调整预览场景
- 新增 `Assets/_Project/Scenes/WorldMap/WorldMap_PetAnimationPreview.unity`，由 `_SceneRoot` 统一承载 `Main Camera`、`Pet_Angel`、`Pet_Devil`，用于在不加载室外完整环境的情况下调整两只桌宠动画。
- 新增编辑器作者化入口 `Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapPetAnimationPreviewAuthoring.cs`，可通过 `Tools/Gemini-Lab/WorldMap/Create Pet Animation Preview Scene` 创建或校准预览场景。
- 预览场景中的天使和恶魔分别使用室外待机 Sprite 与 `Assets/_Project/Animations/WorldMap/Pet/WorldMap_Angel.controller`、`WorldMap_Devil.controller`；不使用 Apartment 的 Sprite、AnimatorController 或动画 Clip。
- 作者化入口同时校准 `WorldMap_Main.unity` 中对应 Animator 和 `PetController._movementController` 的共享 Controller 引用；预览场景不复制 `.anim` / `.controller`，因此对共享动画资源的修改会应用到 WorldMap 室外双宠。
- 预览场景只负责动画作者化，不承载移动、数字键调试、交互物体、昼夜或完整环境；预览场景 Transform 的局部视觉调整不会自动写回主场景。

### 2026-08-07 WorldMap 情绪花卉三处显示统一
- 每周种植面板、图鉴列表卡片和图鉴详情页的已显示花卉统一使用 `Assets/_Project/Art/WorldMap/flower` 中按情绪类型与培育者映射的带枝叶完整花图；图鉴列表和详情固定按 `GrowthState.Bloomed` 查询，不显示幼苗或仅花朵资源。
- 三处花卉下方均新增场景作者化的 `SoilImage`，绑定 `Assets/_Project/Art/WorldMap/flower/土壤.PNG`；不同面板只调整 `RectTransform` 尺寸和位置，空白格、锁定卡片和未选中详情隐藏土壤。
- `WorldMapEmotionGardenUIPatch` 已将 `SoilImage`、完整花图和组件序列化引用落盘到 `WorldMap_Main.unity`；运行时脚本只根据花卉数据开关已作者化节点并填充对应 Sprite。
- 当前资源仍缺少 `恶魔-孤独（完整）.PNG`，该情绪/培育者组合不会生成完整花图，待美术资源补齐后由同一映射自动接入。

### 2026-08-07 WorldMap 情绪花卉显示修正
- 图鉴列表卡片的 `FlowerImage` 不能把空 Sprite 的占位透明度 `alpha=0.16` 带入已收集状态；`FlowerCollectionPanelStub` 在绑定完整花图时会恢复 `Color.white`，作者化场景中的卡片花图也保持不透明。
- `WorldMap_Main.unity` 中每周格子和图鉴卡片的 `SoilImage` 已上移到对应完整花图的可见枝叶底部附近；详情页土壤同时与现有手工调整后的 `FlowerImage` X 坐标对齐，并按花图位置计算垂直位置。
- `WorldMapEmotionGardenUIPatch` 的作者化版本提升到 36。当前编辑器会话未自动执行该版本时，场景 YAML 已同步保存相同的 21 个土壤节点位置和 12 个图鉴卡片不透明颜色。

### 2026-08-07 WorldMap 花卉布局复用工具
- 新增 `Assets/_Project/Scripts/Editor/Tools/WorldMapFlowerSoilLayoutWindow.cs`，入口为 `Tools/Gemini-Lab/WorldMap 花卉布局复用`。
- 窗口分别保存每周种植参考格、图鉴列表参考卡和图鉴详情参考页，三个按钮将参考对象下 `FlowerImage` 与 `SoilImage` 的 `RectTransform` 布局复制到对应目标集合。
- 复制目标为 `CellTemplate` 与 `Day0`~`Day6`、`CodexCardSlot_00`~`11` 和所有 `DetailView`；操作使用 Unity Undo 并标记当前场景 dirty，不复制 Sprite、颜色或启用状态。

- 2026-09-09 小门后续修复：空遗留物槽位禁用点击 Collider；永久赠礼支持再次查看；双房间共用外边界，门上下隔墙始终阻挡，开门可双向串门，关门不拉回访客、占用门口时拒绝关门。43 项 EditMode 通过，Play 实际 UI 点击 20/20 内容与双宠跨门物理检查通过。

- 2026-09-09 新增公寓页面家具：天使房水晶球点击打开 Tarot，恶魔房扭蛋机点击打开 Collection；Scene/Prefab/家具定义已保存，显式绑定 FurniturePageLink。实际 UI 点击、建造模式拦截、20 个遗留物点击及 10 条家具物理路线通过。见 `docs/apartment-page-furniture.md`。

- 气泡遮挡修复：普通点击气泡现在通过 `_controlIndicator` 显式引用排在控制箭头上方（原约 1250 < 箭头 9999）。双宠 Play 点击、到期隐藏与控制标识验证通过，Console 0 error；场景绑定已保存。
