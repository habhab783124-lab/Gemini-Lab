# Gemini-Lab Memory Main

## 2026-09-08 苹果掉落核心恢复到 eb1c300

- `AppleTreeDropController.cs`、`AppleDropSlot.cs`、`AppleTreeFeedback.cs` 已恢复到 `eb1c300b991d92ec6336afaf5441d0b0ca81544c` 的掉落表现和反馈逻辑。
- `AppleDropSlot` 仅保留当前 WorldMap `IWorldMapSceneClickTarget` / Collider2D 路由适配，旧 `OnMouseDown` 不恢复；`AppleService.cs` 已与该提交一致。
- 当前 `AppleRuntimeBootstrap` 的直启注册修复、Tarot 启动顺序修复、WorldMap 场景、室外桌宠移动/过桥和动画资源不在本次恢复范围内；Unity Play 仍待人工确认。

## 2026-09-08 苹果掉落逻辑恢复到 cc54 基准

- `Assets/_Project/Scripts/Modules/Apple/AppleService.cs` 已恢复到提交 `cc54b1a9196ff1d16c341ecdfffdd0f27e1c37f1` 的苹果货币与树掉落状态实现。
- `AppleDropSlot` 和 `AppleTreeFeedback` 保留当前 WorldMap Collider2D 路由适配，但恢复 cc54 的 Scene 文本显示语义；运行时不再给 `TMP_Text.fontMaterial` 赋 null。
- TMP 3.0.7 的 `fontMaterial` setter 会对传入值直接调用 `GetInstanceID()`，传入 null 会在树点击后的反馈阶段抛出 `NullReferenceException`。反馈继续使用 Scene 中已有的字体和材质引用。
- 当前 `AppleRuntimeBootstrap`、WorldMap 点击路由、室外桌宠移动/过桥链路、动画资源和室内系统未回退；Unity Play 仍待人工验证。

## 2026-09-08 Boot 苹果服务与 Tarot 启动顺序修正

- `Boot.unity` 的 `BootstrapRoot` 保留一个 `TarotRuntimeBootstrap`，继续使用作者化的 TarotDeck 和 LLM 配置；重复的无 LLM 配置实例已移除。
- `AppleRuntimeBootstrap` 在 `Start` 注册 `IAppleService`，`TarotRuntimeBootstrap` 的服务初始化改为 `Start`，并设置为晚于 Apple bootstrap 的执行顺序，避免 Tarot 在苹果服务注册前读取服务。
- `AppleRuntimeBootstrap`、WorldMap 苹果树、室外桌宠移动/过桥链路、动画资源和室内系统不属于本次修正范围；Unity Play 仍需人工确认。

## 2026-09-08 WorldMap 苹果树成熟与调试快进初始化修复

- 当前苹果树点击已经能够通过真实 `PolygonCollider2D` 命中 `AppleTreeInteractable` 并显示已有 `AppleTreeFeedback`；此前反复点击调试按钮后仍提示“还没成熟哦”的根因是树状态首次在点击树时才创建，快进发生在状态创建之前。
- `AppleRuntimeBootstrap` 现在在运行时场景加载阶段确保 `IAppleService` 注册，并监听后续场景加载；每次场景加载后为当前已作者化的 `AppleTreeInteractable` 调用 `IAppleService.EnsureTree`。不创建任何最终 UI、Sprite、Animator 或视觉 GameObject。
- 现有 `AppleService` 的 45～90 分钟随机生成、每天最多 5 轮、批次固定总量、逐个掉落领取和货币入账逻辑保持不变；本次修复的是服务与三棵树状态的初始化时序。
- 新增 EditMode 回归测试覆盖“先建立树状态、再推进调试时钟、随后可以开始收获”。Unity Play 仍需用户手动确认，当前不能把静态/编译结果当作实机通过。

## 2026-09-08 WorldMap 点击映射与室外桌宠过桥链路恢复

- WorldMap 第一阶段的点击业务固定为：天使标牌打开 Angel 情绪输入、恶魔标牌打开 Devil 情绪输入、邮箱打开 `DailySummaryMailbox`、许愿树打开许愿系统、大树 2/3/5 进入对应苹果树掉落流程、两只室外桌宠只切换对应玩家控制权；大树 1 不接苹果逻辑。
- `WorldMapSceneInteractionRouter` 只裁决 Scene 中显式登记的目标；每个目标通过自身 `Collider2D` 的 `OverlapPoint` 命中。桌宠点击区与底部物理 CapsuleCollider 分离，许愿树点击 Collider 覆盖其作者化 Sprite 区域。
- 室外桌宠的移动链路必须保留 `PetController`、`RandomWander`、`PetPlayerInputController` 和 `WalkableSurface`。桥对象 `桥` 的 PolygonCollider2D 上侧轮廓仍是过桥高度事实源；WorldMap 点击组件不再直接写 Transform/Rigidbody2D 或强制固定基线。
- `WorldMapPetAnimationTriggerController` 只负责 WorldMap 特殊动作并通过 `PetController.SetExternalMovementLock` 暂停移动；它不修改室内 Animator 状态机，也不修改 Clip、关键帧、循环、Motion 或视觉参数。
- 本次静态修正尚未经过用户 Unity Play 实机确认。

## 2026-08-21 indoor furniture selection feedback

Apartment task 2 now has a scene-authored `ApartmentFurnitureSelection` presenter, one inactive `FurnitureSelectionHighlight` per available target, and an authored `FurnitureSelectionMessage` TMP node. The presenter is routed by `ApartmentViewportInputBridge` and uses `ClickOcclusionUtility` for overlapping 2D furniture. The nine original targets plus existing `家具_装饰_储物的家具_恶魔_01` are wired; the requirement's apple-pad name is the semantic label for this storage furniture. Tasks 3 and 4 are out of scope.

Updated: 2026-08-22

## 2026-08-20 verification: daily summary mailbox and WorldMap free wander

- Unity MCP Play verification completed. The outdoor `WorldMap_Main/室外背景/邮箱` has a serialized listener to `WorldMapDailySummaryMailboxOpenTarget.PanelOpenButton.OnClick`; it opens the authored WorldMap `Panel_DailySummaryMailbox` and activates `DailySummaryContent`.
- The retired Apartment `MailboxButton` and `Panel_DailySummaryMailbox` remain in the saved scene only as inactive legacy objects, so they cannot act as a second entry point.
- `Pet_Angel` and `Pet_Devil` in `WorldMap_Main` both use authored `RandomWander` bounds X -18..18 with horizontal baseline Y -1.75. Their transforms changed during Play; the Animator reported `IsMoving=true` and a directional `MoveDir` while moving.
- Summary submission/autosave/restore is covered by `EmotionGardenPlacementPersistenceTests` (5/5); the runtime probe did not submit or persist test data.

## 2026-08-18 WorldMap local weather switching

- `WorldMapWeatherController` now reads current weather through `IWeatherProvider` and `OpenMeteoWeatherProvider`; WMO weather codes are classified into the first-pass `Sunny` and `Rainy` states.
- `WorldMap_Main.unity` now contains only the authored `WorldMapWeatherRainOverlay`; it references `Assets/_Project/Art/WorldMap/weather/rain.png`. Clear weather uses the saved WorldMap scene background, and no legacy `garden/天气（最上层）` Sprite is referenced. Runtime only toggles the saved rain `SpriteRenderer` state.
- The default Inspector location is Shanghai (`31.2304, 121.4737`) with `timezone=auto` and a 30-minute refresh. This is a replaceable placeholder until the project has a location source. The service caches the last successful result and falls back to Sunny on the first network failure.
- Weather and day/night are independent: `WorldMapDayNightController` still uses `IGameClock` for 06:00–18:00 day / 18:00–06:00 night, while weather uses the remote WMO code. Open-Meteo was reachable during the Play smoke test and returned code `3` (Sunny) for the default location.

### 2026-08-21 WorldMap 雨天美术资源接入

- `Assets/_Project/Art/WorldMap/weather/` is the weather-art source folder. `rain.png` is saved on `WorldMapWeatherRainOverlay` in `WorldMap_Main.unity` with the existing full-scene overlay size and sorting order.
- `WorldMapWeatherAuthoring` reads only `weather/rain.png`; it removes the obsolete sunny overlay instead of preserving a legacy Scene reference. Until a dedicated sunny asset is provided, the authored WorldMap background is the clear-weather presentation. Runtime weather logic and the Open-Meteo request are unchanged.

### 2026-09-04 WorldMap 云层与星星资源接入
- `WorldMap_Main.unity` now contains authored `WorldMapWeatherClouds` and `WorldMapWeatherStars` SpriteRenderers. They use `weather/云层.jpg` and `weather/星星.PNG`, fit the saved outdoor background bounds, and bind `BaselineItem` to the existing `Environment_Clouds` / `Environment_Stars` definitions.
- `WorldMapCloudColorKey.mat` uses the authored `GeminiLab/WorldMap/CloudColorKey` shader so the white background of the cloud JPG is transparent while the blue cloud shapes remain visible. No legacy `garden/天气（最上层）` asset is referenced.
- `WorldMapDayNightController` now toggles the saved stars renderer together with `WorldMapNightOverlay`: stars are hidden from 06:00 through 18:00 and visible from 18:00 through 06:00. Runtime only changes `enabled`; it does not create visual objects.

### 2026-09-04 WorldMap 环境动画

- `WorldMapAmbientAnimationController` 已作者化到 `WorldMap_Main` 的 `_SceneRoot`，通过序列化引用驱动云层、`WorldMapPlacedFlowers` 下的单花视觉节点，以及许愿树和大树 2～5。
- 云层只沿 X 轴平滑往返移动；单花节点按名称哈希获得不同确定性相位并轻微旋转，花丛节点不参与；树木以现有 Sprite 边界底部为局部根点做轻微摆动，根点不漂移。
- `WorldMapAmbientAnimationAuthoring` 只负责绑定现有 Scene 节点和保存参数，不在运行时创建最终视觉对象。天气、昼夜、基线和花朵摆放逻辑保持不变。

## 2026-08-18 苹果资源系统按新版需求修正

- `AppleService` 以 `IAppleService` 为门面，新档余额仍为 20；每棵树通过 `IGameClock.UtcNow` 按 45–90 分钟随机生成一轮，每天最多 5 轮，每轮按 70%/30% 生成 1/2 个苹果，`NextGenerationUtcTicks`、当日轮数和未领取缓存进入 `apple` 存档。
- 未领取苹果不再有 3 个上限；跨重启、跨日会继续累计，晃树一次领取该树全部缓存。旧版只保存 `LastGeneratedUtcTicks` 的存档会迁移到新版调度字段，不会清空既有缓存。
- `WorldMap_Main.unity` 的「大树 2」～「大树 5」由 `WorldMapAppleTreeAuthoring` 作者化 `AppleTreeInteractable` 与 `AppleTreeFeedback`；「大树 1」不是苹果树，不再绑定苹果领取或反馈节点。苹果树无可领取时显示“还没成熟哦”并播放两片 Scene 中已保存的落叶文字节点，运行时不创建最终视觉对象。
- 情绪花首次开花通过 `EmotionGardenService.BloomAt` 奖励 12 个苹果且不重复；恶魔房间扭蛋单抽/五连消耗 20/100 个苹果，塔罗开始一次抽牌会话消耗 8 个苹果。
- `BootAppleBootstrapAuthoring` 已将苹果服务注册宿主保存到 `Boot/BootstrapRoot`；Apartment 四个页面复用原有 `TopResource/BalanceLabel`，由 `StubPanelBase`/`AppleBalanceDisplay` 统一更新苹果数量，不再创建独立的 `AppleBalanceLabel`。苹果是 UI 资源栏的唯一货币，文本只显示数字。
- EditMode `AppleResourceServiceTests` 覆盖初始余额、随机间隔、每日轮数上限、70/30 数量、缓存往返、领取、消费和成熟奖励去重；WorldMap 晃树反馈、四页余额和完整扭蛋/塔罗流程仍需 Unity PlayMode 人工复验。

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

## 2026-08-21 工作流兼容增强
- 原有协作流程没有改动，仍按“探索 → 规划 → 用户确认 → 行动”推进，四段式规划模板仍是用户可读入口。
- `docs/current-task-card.json` 现在为新任务增加 `workflow_contract_version`、`task_id`、`human_approved`、`approval_source` 和 `plan_hash`；执行状态、批准时间和验证记录不参与计划 hash。
- `tools/check-task-gate.ps1` 在写入模式校验批准状态、任务 ID 和计划 hash；缺少新字段的旧卡需要先重写为新任务卡，避免误用上一轮任务范围。
- `tools/check-task-scope.ps1` 可记录任务开始时的 git 状态基线，并拒绝不在 `direct_files` 中的新增或变化路径。
- `tools/verify-task.ps1` 统一执行 review 闸门、任务范围、`git diff --check` 和 PowerShell 语法检查，输出机器可读 JSON；Unity 编译、测试及视觉契约仍按任务卡的任务类型执行。
- 这些是仓库内可执行的防线，不是操作系统级写权限隔离；直接文件工具或外部进程仍可能绕过它们。

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
- 当前还启用了工作流兼容检查：
  - `tools/task-card-utils.ps1`
  - `tools/check-task-scope.ps1`
  - `tools/verify-task.ps1`
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
- `2026-06-02` 同日先把 4 个旧版 Unity MCP 注册表包按“临时停用、保留恢复能力”方案软移出到 `PackageBackups/MCP-disabled-2026-06-02/`；`2026-08-18` 起 `Packages/manifest.json` 已启用 `com.ivanmurzak.unity.mcp` `0.88.0`，旧版动画/粒子/ProBuilder 包仍保留在备份目录，`Packages/SkillsForUnity` 明确保留不动。
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
  - `2026-08-18` 已安装 Node.js `v24.19.0` 与 `unity-mcp-cli` `v0.88.0`，并将活动 MCP 插件写入 manifest；Unity 已安全重启并连接 `http://localhost:27345`。情绪花园定向 EditMode 5/5 通过，全量 EditMode 仍有既有 Furniture / MovingState 失败，PlayMode 仍需补验。
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
   - `Packages/manifest.json` 当前启用 `com.ivanmurzak.unity.mcp` `0.88.0`；旧版 4 个包仍备份在 `PackageBackups/MCP-disabled-2026-06-02/`
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
- 花卉资源不从 `arrange.png` 裁剪：18 个“培育者 + 情绪”组合的花种标题取 `花朵图鉴/花朵`，放置系统单花取 `花朵图鉴/花朵放置/*.PNG`，花丛取 `花朵图鉴/花丛/*（花丛）.PNG`，均由 Editor 保存为 Scene Sprite 引用。
- `2026-08-24` 起，WorldMap 花朵放置系统的单花资源切换为 `Assets/_Project/Art/WorldMap/花朵图鉴/花朵放置/` 下 18 张 Sprite（9 种情绪 × 天使/恶魔）；花种列表图标仍取 `花朵`，花丛仍取 `花丛`，其他图鉴和培育模块不变。作者化已重新保存预览与已摆放槽位的 Scene 引用。
- `WorldMap_Main.unity` 已作者化 18 个花卉条目、36 个预览变体、32 个有限落位槽及其 1152 个单花/花丛预置显示对象；运行时只切换预置对象、更新槽位位置/占用状态和文本，不创建 GameObject、UI、Sprite 或网格线。
- 网格材质 `Assets/_Project/Art/WorldMap/arrange/PlacementGrid.mat` 与 10 条竖线、5 条横线已落盘；网格单元仍严格读取 `garden/中景/花丛.png` 的 Sprite 完整尺寸 `4.01 x 2.24` Unity 单位，花丛资源本身不作为摆放显示。
- `2026-08-11` 已将条目展开改为固定详情页手风琴：`FlowerList` 是 `ScrollRect` 唯一内容与唯一纵向布局根，`FlowerSidebarContent` 仅保留层级容器。每个 `FlowerOption_00...17` 场景中都预置标题与 `ExpandedOptions`；收起高度为 `73`，展开高度为 `320`，详情页以顶部 Pivot 固定在标题下方 `-76`。`FlowerList.VerticalLayoutGroup.childControlHeight` 已保存为开启，确保展开条目的 320 高度真实参与排版，后续条目会整体下移而不覆盖详情。运行时只切换详情节点和该条目的已序列化高度，并在切换条目时补偿 ScrollRect，使被点击标题栏保持当前视窗位置；上方条目不移动。
- 当前交互为：打开右侧栏 → 展开花种 → 选择单花/花丛 → 侧栏自动收起并保留半透明预览 → 点击有效区域连续落位 → `Esc` 退出；网格只用于内部吸附计算、运行时不显示；同种 3 朵单花可主动合成 1 个花丛，不足时显示 `组 5.png` 提示气泡。
- `2026-08-12` 起，摆放库存已收口到 `EmotionGardenService` 的 `emotion-garden` 持久化数据：每朵花开花时给对应“情绪类型 + 培育者”增加 1 个单花库存；摆放单花/花丛与“3 单花合成 1 花丛”都直接读写同一库存，并通过 `EmotionFlowerPlacementInventoryChangedEvent` 实时刷新侧栏。存档版本升级为 3，旧存档只在迁移时按已开花记录（缺失时以累计进度补齐）初始化一次单花库存，不会在每次打开侧栏时重复补发。
- `2026-08-13` 起，存档版本为 4：成功摆放以槽位、花型、情绪/培育者和世界坐标写入 `PlacedFlowers`，原子扣除对应库存并立即串行 autosave；启动读档会恢复原槽位和坐标，不会再次扣库存。单花和花丛摆放均不附带土壤；旧的顶层 `Canvas/FlowerPlacementStatusBar` 已从场景移除，当前提示只使用侧栏内 `PlacementStatus`。
- `2026-08-13` 同步清理 `FlowerPlacementPanel` 下旧原型的 `FlowerButtons`、`SingleButton`、`ClusterButton`、`CancelButton`，并重新把 32 个 `WorldMapPlacementSlot` 的正式视觉绑定保存到 Scene；运行时恢复只切换这些已作者化节点。当前 Editor 默认使用 `Saves-Dev`，其中没有 `PlacedFlowers` 的旧记录时无法凭空恢复历史摆放，之后的新摆放会写入版本 4 存档。
- `2026-08-13` 摆放层改为读取 Scene 中的 `BaselineItem`：花卉基线与其排序层绑定，同层才做占用冲突检查，跨层允许重叠；内部吸附仍按相邻层半个单元宽度错位，但 `FlowerPlacementGrid` 不再在运行时显示，避免抽象网格误导玩家。作者化会过滤摆放区域外的基线，花丛 footprint 改为单格，视觉/占用尺寸以场景 `花丛 3` 的 `3.99 x 2.22` 碰撞体基准，点击草地时会真正提交摆放，并清理槽位遗留的重复旧组件。
- `2026-08-14` 修正落位链路：`WorldMapPlacementSlot` 与 `WorldMapPlacedFlower` 使用独立稳定脚本 GUID，运行时校验为 `32/32` 槽位、`1152` 个正式视觉绑定；槽位绑定会扫描已作者化的 `PlacedVisual_*` 子节点，并以显式占用状态参与同层冲突判断。提交期间忽略同步 `EmotionFlowerPlacementsChangedEvent` 的重入恢复，避免库存扣减成功后恢复回调清空刚落位槽位；直接槽位验证已确认绑定 `36`、占用状态为真。鼠标端到端仍需在有可用库存的 Play 存档中人工点击确认。
- `2026-08-14` 同日修正花朵与桌宠的遮挡基准：撤销错误的全局花朵前置偏移，正式花朵/预览和桌宠共享 `BaselineItem.SortingOrder` 主层级，并在同一主层级内按基线 Y 二次排序（Y 越低越靠前，完全同线时桌宠仅提高 1 个排序单位）。`WorldMap_Main.unity` 的 1152 个预置视觉统一保存为 `Default` Sorting Layer，槽位显示树内所有 SpriteRenderer 也会同步该深度键。AutoSetup 63 已给 `Pet_Angel`、`Pet_Devil` 场景根对象保存 `BaselineItem`，基线取碰撞体底部、`solidCollider=true` 且不参与可种植层收集。
- `2026-08-23` 起，花朵摆放保留可在 Scene/Inspector 调整的 `FlowerPlacementBounds`，并在 `WorldMapPlacedFlowers` 下作者化 `FlowerPlacementRegion_Angel` 与 `FlowerPlacementRegion_Demon` 两个 BoxCollider2D 区域。只要区域列表已配置，控制器就把区域作为唯一放置范围和网格原点来源；区域引用缺失或 owner 不匹配时直接判定无效并记录诊断，不再静默回退到 `FlowerPlacementBounds`。只有完全没有区域配置的旧场景才保留 `FlowerPlacementBounds` 回退。区域边界不再受固定层级 XMin/XMax 限制，BaselineItem 仍只负责基线和遮挡层级。旧存档不自动迁移或删除。
- `2026-08-12` 同步修正 `FlowerSidebarViewport`：拉伸锚点下改用 `offsetMin=(34,56)` 与 `offsetMax=(-34,-132)` 保存面板内边距，第一条花种固定在标题下方，列表滚动内容由 `RectMask2D` 裁剪在装饰窗口内部。独立作者化入口为 `Tools/Gemini-Lab/WorldMap/Author Flower Placement`。

- `2026-07-30` 起，`WorldMap_Main.unity` 中 `Panel_WeeklyGarden/Grid/CellTemplate` 需要保持场景里可编辑但默认不可见；`WeeklyGardenPanelStub` 会在运行时兜底隐藏模板，实际面板只应显示 7 个 Day cell。`tools/check-task-gate.ps1 write` 已通过，Scene 视图仍需补验确认没有第 8 个瓶子。
- `2026-07-30` 同轮，`Panel_WeeklyGarden/Grid` 已从自动布局改为纯容器，`Day0`~`Day6` 可以在 Scene / Inspector 里单独移动；作者化脚本不再清理现有格子位置，也不再给模板和新格子补 `HorizontalLayoutGroup` / `LayoutElement`。

### 2026-08-05 WorldMap 昼夜切换
- WorldMap 使用 `Assets/_Project/Scripts/Modules/WorldMap/WorldMapDayNightController.cs` 按 `IGameClock.Now` 切换昼夜。
- 默认 06:00–18:00 为白天，其余时间为夜晚；夜晚启用场景中的 `WorldMapNightOverlay`。
- 夜幕使用已有 `Assets/_Project/Art/WorldMap/weather/夜幕.png`，场景作者化入口为 `WorldMapDayNightAuthoring`；废弃的 `garden/天气（最上层）` 文件不作为天气来源。
- 当前 Scene 已直接保存 `WorldMapNightOverlay` 的位置、排序和控制器引用；夜幕碰撞已禁用，UI 不受其世界渲染排序影响。
- 2026-08-24 修正夜幕覆盖：`WorldMapNightOverlay` 改用 `ProjectSettings/TagManager.asset` 中位于 `Default` 之后的专用 Sorting Layer，确保按 `BaselineItem` 动态排序的两名桌宠和花朵也会被夜幕覆盖；`WorldMapDayNightController` 仍只切换已作者化 SpriteRenderer。

### 2026-08-06 WorldMap 桌宠数字键动画调试
- `WorldMapPetAnimationTriggerController` 现在是无可视化占位物的数字键调试入口，挂在 `_SceneRoot`，不再依赖宠物位置、临时点位或 `F` 键。
- 当前数字映射为：`1` 天使坐地 `Outdoor_Sit`、`2` 天使祈祷 `Outdoor_Pray`、`3` 天使开心 `Outdoor_Happy`、`4` 天使浇水 `Outdoor_Water`、`5` 恶魔睡觉 `Outdoor_Sleep`、`6` 恶魔施法 `Outdoor_Cast`、`7` 恶魔得意 `Outdoor_Proud`；普通 Idle / Move 仍由桌宠移动状态驱动。
- `WorldMapAnimationTriggers` 及五个临时点位已从 `WorldMap_Main.unity` 删除；作者化入口会清理旧对象，AutoSetup 版本提升到 32，后续不会再次创建占位点。
- 天使 `坐地`、`祈祷` 的序列帧目录和 AnimationClip 已绑定到 WorldMap 专用 `WorldMap_Angel.controller` 的 `Outdoor_Sit`、`Outdoor_Pray` 状态；Apartment 控制器和资源不复用。
- 数字触发时调试组件在 `PetController` 更新之后持续维持特殊动画，持续时间结束后交还普通 Idle / Move；自动巡航和最终策划触发条件仍未实现。
- 数字触发期间会通过 `PetController.SetExternalMovementLock` 锁定当前桌宠的玩家输入、随机漫游和刚体速度；锁定按桌宠分别生效，动画结束或调试组件禁用后恢复。
- 天使走路资源的原始侧身朝向按左向处理，因此 WorldMap 天使的 `_sideFramesFaceLeft` 保持 `true`；恶魔配置保持原样。

### 2026-08-05 WorldMap 可交互场景物体反馈
- `WorldMap_Main.unity` 中的 `室内`、`邮箱`、`大树 1`～`大树 5` 已由 `WorldMapInteractiveObjectAuthoring` 作者化悬停反馈；点击入口只保留给 `室内`、`邮箱` 和大树 2～5，大树 1 暂不绑定点击交互。
- 新增运行时组件 `WorldMapInteractiveObjectFeedback`：以对象在 Scene 中保存的 localScale 为基准，在通过 UI / 最上层 2D collider 裁决后平滑放大并在移出时恢复，不累计修改最终视觉。
- 邮箱和大树 2～5 接入 `ClickableSceneObject` 的序列化 `UnityEvent` 点击入口；大树 1 已清理苹果领取和通用点击占位，等待策划确定后再接入业务。
- `室内` 继续使用 `CabinReturnPortal` 返回 `Apartment`；`GameBootstrap` 增加服务缺失时的重新注册保护，降低 Editor 反复进入 Play 或关闭域重载导致 `ISceneFlowService` 缺失的概率。
- AutoSetup 版本提升到 31；PlayMode 悬停、遮挡和点击跳转仍需人工验证。
- 后续修正：`WorldMapInteractiveObjectFeedback` 改用鼠标世界坐标与自身 `Collider2D.OverlapPoint` 检测悬停，不再因大树下方的花丛等场景碰撞体排序导致反馈失效；点击入口仍保留 `ClickOcclusionUtility` 的最上层裁决。

### 2026-08-24 WorldMap 苹果树轮廓交互判定

- `WorldMapInteractiveObjectAuthoring` 现在只对「大树 2」～「大树 5」在编辑器作者化阶段读取各自 Sprite 的透明轮廓，保存为根节点 `PolygonCollider2D`；不再用整块 `BoxCollider2D` 覆盖树的透明区域。
- 轮廓生成通过 Unity 编辑器内部 Sprite 轮廓 API 完成并落盘到 Scene，运行时只读取已保存的碰撞体；悬停 `OverlapPoint` 与点击 `ClickOcclusionUtility` 共用同一轮廓。
- 每棵苹果树现在只有一个有效轮廓碰撞体，位置、缩放、排序、`AppleTreeInteractable`、`ClickableSceneObject` 和 `AppleTreeFeedback` 不变；「大树 1」仍不是苹果树且没有点击入口。
- 最终需要在 Unity Play 视图逐棵点击可见树边缘确认轮廓覆盖；本轮不改变苹果服务规则或树的美术资源。

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

### 2026-08-18 AI 每日小结与 WorldMap 自由行走
- `EmotionGardenService` 新增 `EmotionDailySummaryData` 与 `DailySummaries` 持久化字段；提交情绪时按日期覆盖当天小结，旧存档缺少该字段时从已有花朵记录补出兼容摘要。
- `PersistenceBootstrap` 订阅 `EmotionFlowerSubmittedEvent`，提交成功后串行写入 `autosave`；因此不再依赖 `OnApplicationQuit`，Unity 停止 Play 或异常退出前也能保留最新小结。
- `WorldMap_Main.unity` 的 Canvas 已作者化 `Panel_DailySummaryMailbox`；室外邮箱通过序列化 UnityEvent 打开该面板。Apartment 中同名 `MailboxButton` 与面板已停用，避免错误入口。`DailySummaryMailboxPanel` 只填充 Scene 中已有的 TMP 节点，不在运行时创建 UI。当前网关没有结构化“每日小结”接口，因此使用离线确定性 AI 风格兜底文本，后续可替换生成提供器而不改变邮箱和存档结构。
- `WorldMap_Main.unity` 的 `Pet_Angel`、`Pet_Devil` 均保存 `RandomWander` 和 `PetPlayerInputController`，默认不抢占玩家控制；无控制/交互时在作者化边界内漫游，`PetController` 继续驱动现有 `IsMoving`、方向参数和 Idle/Move Animator 状态，点击后可取得控制权。
- `AutoSetup` 版本提升到 66；Scene 与 Play 的最终 UI、Sprite、Animator 和边界均来自已保存场景/组件，运行时仅更新状态和文本。

### 2026-09-01 WorldMap 环境基线扩展

- `WorldMap_Main.unity/WorldMapPlacedFlowers/FlowerPlacementGrid` 由十一条独立 `WorldMapBaselineDefinition` 节点承载固定基线：天空、星星、云、树木后排、树木前排、地面、四条花丛层和桌宠层。环境基线使用蓝色，花丛基线使用白色，桌宠基线使用红色。
- 云和星星已经有独立场景美术节点：`WorldMapWeatherClouds` / `WorldMapWeatherStars` 分别绑定 `Environment_Clouds` / `Environment_Stars`，位置保持与室外背景的作者化范围一致。
- `BaselineItem` 只通过序列化定义引用读取基线 Y、X 范围和相对渲染顺序值；WorldMap 场景保留每个物体原本的 `_baselineTransformOffset`（由 Sprite 轴心决定），物体自身只沿 X 移动并锁定在 `BaselineY + 原偏移`。删除绑定物体不会删除基线，只有基线工具移动基线时才会同步带动物体 Y。`WorldMapBaselineDefinition` 是基线身份、位置、范围、放置许可和遮挡顺序的唯一事实源，Inspector 与批量基线工具共享同一份参数，不再提供 `BaselineItem` 本地覆盖值。
- 花朵放置控制器只序列化四个花朵层，不再从每个 `BaselineItem` 动态推导层数。
- `Tools/Gemini-Lab/WorldMap/场景基线` 提供 Scene 视图基线手柄，移动基线会同步移动同一基线上的绑定物体；窗口按 RenderOrder 相对值从大到小显示，可直接编辑共享值，并用固定点击式前移/后移交换相邻基线，修改后刷新跟随基线的 SpriteRenderer。RenderOrder 只用于相对比较，不代表基线条数。
### 2026-08-20 Apartment 遗留物系统首轮
- 新增 `ApartmentKeepsake` 模块：`ApartmentKeepsakeService` 实现纸条、Relation 45–79 随机纪念物、Relation≥80 永久赠礼的每日首次室内判定与 JSON 存档；判定日期防重复，状态变更经 `PersistenceBootstrap` 立即写入 autosave。
- `PetRuntimeData.Relation` 已纳入 Pet 存档与快照等价比较；ApartmentKeepsake 模块使用自有 `ApartmentKeepsakeOwner` 枚举，避免依赖循环，Presenter 仅在边界层映射 Angel/Devil。
- `Apartment_Main.unity` 增量作者化 `ArtGenerated/ApartmentKeepsakeWorldPresentation`、`UI_Sidebar/ApartmentKeepsakeOverlay`、详情弹窗、赠礼收藏面板、固定纸条/纪念物点位；所有 Sprite 引用保存于 Scene，运行时只切换显示与填充 TMP。
- `ApartmentViewportInputBridge` 新增世界点交互路由，点击优先级经过 `ClickOcclusionUtility`，确保 RenderTexture 视口内的遗留物点击不依赖 `OnMouseDown`。
- 资产现实：`ui1.0` 目前提供 Angel/Devil 木牌与图鉴入口，不包含正式纸条/遗留物/赠礼本体；首轮复用仓库已有纸张、纪念物和收藏卡，后续美术可直接替换 Scene 引用。
- 验证：ApartmentKeepsakeService 定向 EditMode 7/7 通过；Play smoke 服务注册成功且无本任务异常。全量 EditMode 仍有 22 个既有 Furniture/MovingState 失败，未在本任务扩大范围修复。
### 2026-08-21 WorldMap AI diary visual resources

- `WorldMap_Main.unity` now stores the AI diary board and five clickable resources as Scene-authored Sprite nodes under `Panel_DailySummaryMailbox/DailySummaryContent`.
- Each target opens its matching enlarged preview (`angel_note`, `summary`, `devil_note`, `angel_card`, or `devil_card`) through `DailySummaryDetailPopup`; `弹窗1.png` remains reference-only.
- `DailySummaryMailboxPanel` only toggles authored nodes and fills text. Mailbox routing, summary generation and persistence remain unchanged.
### 2026-08-22 WorldMap AI diary reference layout calibration

- `DailySummaryMailboxAuthoring` now aligns the saved board layout with the reference composition: title `AI每日小结`, selected `今日小结` tab, multi-line empty-state input, three note regions, and the two character cards use Scene-authored positions and sizes.
- The five board buttons use `Selectable.Transition.None` so the imported `AI_diary` Sprite colors remain unchanged during hover/press; the matching enlarged previews remain Scene-authored and are still selected by `DailySummaryDetailPopup` at runtime.
- Unity saved the updated `WorldMap_Main.unity` layout and added the needed Chinese glyphs to `Assets/TextMesh Pro/Resources/Fonts & Materials/LXGWWenKai SDF.asset`; this font asset is part of the visual change, not a runtime-generated UI resource.

### 2026-08-22 WorldMap AI diary date history and popup resource

- `IEmotionGardenService.GetDailySummaryDates()` returns the de-duplicated persisted summary dates in descending order; `DailySummaryMailboxPanel` reads the selected record through `GetDailySummary(dateIso)`.
- `WorldMap_Main.unity` now stores `DateListViewport/DateListContent` with 14 `DailySummaryDateOption` entries. Each entry contains selected/unselected Sprite children and a date TMP; runtime only toggles them and fills the date.
- `PopupButton` uses `AI_diary/弹窗.png` and opens the authored `PopupView` using `AI_diary/弹窗1.png`. `PopupContent` no longer has a solid Image background; the outer backdrop remains the modal close layer.
- `EmotionGardenDailySummaryTests` covers unique descending dates and selecting a historical summary. Mailbox routing, summary generation and persistence format remain unchanged.

### 2026-08-23 AI diary popup text overlay fix

- `DailySummaryMailboxAuthoring` now authors `PopupBodyText` at the center of the enlarged paper resource (`PopupContent` local position `(0,-30)`, size `400x260`) and places it above the preview image in sibling order.
- Summary, angel-note and devil-note previews therefore display the selected date's dynamic text on the paper itself; `PopupView` for `弹窗1.png` keeps its own baked-in message and hides the dynamic text.

### 2026-08-24 Pet runtime save conflict resolution

- `Assets/_Project/Scripts/Modules/Pet/PetRuntimeSaveService.cs` now combines the branch's `PetRuntimeData.Relation` persistence with the upstream v2 offline-mood timestamp rules.
- v2 payloads save `relation` and `savedAtUtcTicks`. On restore, Mood moves toward 50 by one point per five offline minutes, capped at six points; Energy and Satiety restore unchanged. Relation is restored for v2 payloads.
- v1 payloads remain compatible: missing `savedAtUtcTicks` skips offline regression, and missing `relation` leaves the current runtime Relation unchanged.
- The branch was rebased onto `upstream/main`; `PetController` behavior-weight changes and upstream-deleted assets were not reintroduced.

### 2026-09-03 WorldMap 基线视觉修复

- `BaselineItem.RefreshBaselineBinding` now captures the bound object's current world-space Y as its preserved offset before re-aligning, avoiding the previous local-space/world-space mix-up for PSD child objects.
- `WorldMap_Main.unity` keeps the authored PSD-relative local transforms and stores only the corresponding world-space baseline offsets; the active camera is calibrated to cover the authored sky and ground without adding blank framing.
- Flower restore snaps saved flowers to the resolved baseline Y, while `FlowerPlacementBounds` remains the authoritative grass anchor rectangle so legacy saved positions cannot restore above or below the grass.

### 2026-09-03 WorldMap PSD 相对位置修复复核

- 复核发现上次基线作者化曾把 16 个 PSD 子物体的世界 Y 写入了父节点局部 Y，造成背景、地面、天空、树、桥、桌宠和花丛整体上移约一个父节点偏移量。
- 本次只恢复这些物体的原始局部 Transform，并恢复活动相机的原始取景；不改基线定义、排序或存档。
- 以后修改绑定物体时必须区分 `transform.position`（世界坐标）和 `transform.localPosition`（父节点局部坐标），Scene/Play 视觉验证以 `WorldMap_Main` 实际画面为准。

### 2026-09-04 WorldMap 许愿系统

- WorldMap 场景中的 `许愿树`（原“大树 1”）现在绑定 `WorldMapWishTreeInteractable`，点击只打开许愿系统，不执行苹果逻辑。
- `WorldMapWishService` 独立保存 Active、Fulfilled、Archived 愿望记录，使用 PlayerPrefs JSON 跨重启持久化；活动显示槽位固定为 12 个，新增愿望在槽位满时归档最旧记录后复用槽位。
- `WorldMapWishSystemController` 只连接 Scene 中预先作者化的主界面、输入、详情、记忆列表和 12 个星位按钮；运行时只切换显示状态、填充文字和处理按钮事件。
- “全部”记忆列表保留进行中、已实现与已归档记录；归档记录不再占用树上或面板的活动星位。
- 主面板的 12 个愿望星位是 `MainView/WishStarSlot_00..11`，全部作者化在主背景左上角插画许愿树区域内；点击星星不打开详情。
- 右上角作者化的 `item_button.png` 是唯一的愿望详情入口，打开一个右侧可滚轮滚动的愿望列表，并在左侧显示当前选中愿望；旧世界空间星位容器保持隐藏且不参与绑定。

### 2026-09-05 WorldMap 愿望 UI 流程修正

- 已将星星限制为主面板左上角许愿树区域，取消全 panel 随机显示和星星点击详情行为。
- 详情页改为单页滚动手册：打开时优先选中当天最新未归档愿望，列表换选后同步更新左侧内容、日期和状态。

### 2026-09-05 WorldMap 云层与单花环境动画修复

- `Assets/_Project/Art/WorldMap/weather/WorldMapClouds_Alpha.png` 是由天气目录现有云层 JPG 派生的透明 Sprite；仅移除与图像边缘连通的背景白色像素，封闭在云朵轮廓内的白色像素保留，原 JPG 不修改。
- `WorldMapWeatherAuthoring` 将云层绑定到 `WorldMapWeatherClouds` 的 `Environment_Clouds` 基线，并使用 Unity 标准 `Sprites-Default` 材质；不再使用会误删亮色云朵的颜色键材质。云层基线 Y 为 6.8，Scene/Play 均显示完整云朵。
- `WorldMapAmbientAnimationController` 只旋转名称以 `_Single` 结尾且已有 SpriteRenderer 的单花，默认幅度 3.2°、速度 0.9，并以层级路径生成稳定的独立相位；`_Cluster` 花丛不旋转。云层继续沿 X 轴缓慢移动。
### 2026-09-05 WorldMap 双宠动画触发状态机
- `WorldMapPetAnimationTriggerController` 按 PetId 分别维护特殊动画、计时、移动锁和待播放队列；无特殊动作时仍由 `PetController` 根据实际移动状态驱动 Idle/Move。
- 漫游时天使在苹果树、许愿树、天使区域已摆放单花/花丛附近触发坐地、祈祷、浇水；恶魔在苹果树、恶魔标牌附近触发睡觉、施法。苹果树候选排除大树 1/许愿树。
- 玩家控制时 F 键按对应目标触发；新增摆花事件分别触发天使开心或恶魔得意，初次读取存档快照不会误触发。
- 数字键 1～7 调试入口仍保留，动画状态来自现有 `WorldMap_Angel.controller` / `WorldMap_Devil.controller` 的 `Outdoor_*` 状态，不修改 Clip/Controller 资产。
### 2026-09-05 苹果树掉落批次

WorldMap 苹果树点击后由 `AppleTreeDropController` 播放快速晃动并启用 Scene 作者化苹果槽位；`AppleService` 保存批次剩余值，只有逐个点击 `AppleDropSlot` 才增加苹果余额。掉落 Sprite 固定来自 `Assets/_Project/Art/WorldMap/苹果云背景补充/apple.png`，许愿树不参与。

### 2026-09-05 WorldMap 输入框视觉状态与情绪入口

- `EmotionInputPanelStub` 的天使/恶魔主题各自保存提示图 `输入心情/*/input.png` 与无字图 `UI输入框去字/angel_emotion_input.png`、`devil_emotion_input.png`；运行时仅在输入框获得焦点和结束编辑时切换已作者化 Image 的显隐，提交成功后恢复提示图。
- `WorldMapWishSystemController` 的 `WishInputField` 保存提示图 `许愿树/input.png` 与无字图 `UI输入框去字/wish_input.png`；打开输入页不自动抢焦点，点击输入框后切换无字图，结束编辑、取消或提交后恢复提示图。
- WorldMap 情绪输入入口改为真实场景对象 `天使标牌` / `恶魔标牌` 上的 `WorldMapGardenZone`，分别传入 `angel` / `demon`；旧的 `EmotionEntry_Angel` / `EmotionEntry_Demon` 仅作为停用遗留节点，不再作为入口。Canvas 右上 `Btn_EmotionInput` 停用。
- `FlowerCollectionPanelStub` 与 `WorldMapEmotionGardenUIPatch` 不再显示或绑定图鉴卡片、图鉴详情的 `SoilImage`；土壤仅保留在 `WeeklyGardenPanelStub` 的每周培育节点。

### 2026-09-05 WorldMap AI 情绪花园接入

- `EmotionGardenService.SubmitEmotionAsync` 复用室内 `Resources/LLMConfig.asset` 的 OpenAI 兼容配置；AI 负责从九种标准情绪中判定情绪、生成关键词、花朵基础描述、花语，以及每日 `summary`、`angelNote`、`devilNote`。
- AI 结果写入 `EmotionFlowerData` 和 `EmotionDailySummaryData`，随情绪花园存档持久化；旧存档缺少新字段时由本地规则补齐。AI 超时、未配置或返回不合规内容时自动使用本地兜底，不阻塞提交。
- `EmotionInputPanelStub` 使用异步提交并在等待时锁定按钮；`WeeklyGardenPanelStub` 显示 AI 关键词和花语；`DailySummaryMailboxPanel` 继续读取选中日期的持久化每日总结。
- 许愿面板 AI 接入暂缓，当前不修改 `WorldMapWishSystemController` 的本地愿望流程。

### 2026-09-06 WorldMap 室外新手指引

- `WorldMap_Main.unity` 的 `Canvas/Panel_OutdoorTutorial` 已保存七个页面节点：`Page_Intro` 与 `Page_Outdoor1`～`Page_Outdoor6`，图片来自 `Assets/_Project/Art/新手引导/outdoor/`。
- 面板保存 `left.png`、`right.png`、`outdoor/close.png` 三个按钮资源，并提供 `Btn_OutdoorTutorial` 占位入口。入口当前只负责打开第一页，后续可在 Inspector 中替换按钮 Sprite 或绑定正式入口。
- `SceneAuthoredImageVariantView` 负责运行时切换已作者化页面，上一页/下一页在首尾边界停止；运行时不创建 GameObject、不加载路径资源，也不写入最终 Sprite。
- `WorldMapOutdoorTutorialAuthoring` 是定向作者化工具（菜单 `Tools/Gemini-Lab/WorldMap/Author Outdoor Tutorial`），只维护自己的节点，不重建或清空 WorldMap 场景。当前没有绑定邮箱、标牌或首次进入等正式业务触发条件。
### 2026-09-07 WorldMap 第一阶段室外点击路由

- `WorldMapSceneInteractionRouter` 现在统一裁决邮箱、天使标牌、恶魔标牌、两只室外桌宠、许愿树、三棵苹果树和九个现有 `AppleDropSlot`；场景目标通过序列化引用注册，不依赖对象名查找。
- 大树 2、3、5 使用现有作者化 `PolygonCollider2D` 命中；许愿树独立打开许愿系统；大树上的旧 `ClickableSceneObject` 已从 `WorldMap_Main.unity` 移除。
- `AppleDropSlot` 领取后继续复用 Scene `CollectionText`；本阶段只修复点击路由，不改变掉落数量分配、动画触发条件或室内系统。Play 尚未由助手验证。
### 2026-09-08 WorldMap Collider2D 点击命中修正

- WorldMap 路由目标的 `ContainsWorldPoint` 只使用目标自身启用的 `Collider2D.OverlapPoint`；邮箱、天使/恶魔标牌和两只室外桌宠不再以 `SpriteRenderer.bounds` 作为点击区域。
- 树木继续使用 Scene 中保存的真实 `PolygonCollider2D`，许愿树和 `AppleDropSlot` 继续使用各自 Collider2D；本次不调整 Collider 几何。
- 本次只完成静态代码与编译修正，Play 实机点击范围仍待人工确认。

### 2026-09-08 WorldMap 苹果服务入口修正

- `AppleRuntimeBootstrap.EnsureRegistered()` 会复用 Boot 已注册的 `IAppleService`；WorldMap 直启时，如果核心 `IGameClock` 已存在，则注册同一个现有 `AppleService` 实现并加入持久化服务注册表，不创建第二个货币权威。
- `AppleTreeDropController.TryBeginHarvest()` 在读取 `IAppleService` 前调用该幂等入口；树点击仍由 `WorldMapSceneInteractionRouter` 和三棵树自身的 `PolygonCollider2D` 命中，成熟判断、掉落分配和苹果领取逻辑不变。
- WorldMap 直启缺少苹果服务时是树点击无掉落的专项风险；没有成熟批次时仍应显示已有“还没成熟哦”反馈。该修正已通过静态检查和程序集编译，Play 实机仍待人工确认。

### 2026-09-08 WorldMap 苹果货币收集逻辑重写

- `AppleService` 重新收口为苹果货币的唯一状态权威：成熟调度、树的待领取总量、一次掉落批次的预留余量、逐个苹果领取和余额增加均由同一服务完成；领取金额超过当前预留余量时直接失败，不再自动截断。
- `AppleRuntimeBootstrap` 在核心时钟可用后注册服务，并为当前场景已有的三棵目标树建立状态；`AppleTreeDropController` 只分配 Scene 中已有的 1～3 个 `AppleDropSlot`，不创建运行时视觉对象。
- `AppleDropSlot` 只有服务领取成功后才隐藏苹果、增加余额并显示 `CollectionText`；`AppleTreeFeedback` 使用已有 `StatusText` 显示未成熟提示，持续时间约 2 秒。
- 3 个 `StatusText`、9 个 `CollectionText` 的 MeshRenderer 与 TMP 覆盖材质已统一改为现有 `NotoSansSC_SDF`，清除了旧 `LiberationSans` 实例材质，修复中文反馈设置成功但不出字的问题；本阶段未进入 Unity Play。

### 2026-09-08 WorldMap 云层范围与室内入口点击

- `WorldMapAmbientAnimationController` 现在使用 `WorldMap_Main.unity` 中 `BaselineLine_云` 的作者化横向范围 `x=-54.563995` 到 `x=20.005005` 驱动云朵往返移动，不再使用过窄的 `1.2` 对称偏移。
- 外场 `室内` 物体保留原有 SpriteRenderer 和 BoxCollider2D，并通过其 `CabinReturnPortal` 组件登记到 `WorldMapSceneInteractionRouter`；命中自身 Collider2D 后调用 `ISceneFlowService.LoadAsync(SceneId.Apartment)`。
- 本次不修改 `Apartment_Main.unity`、室内家具、室内状态机、桥面逻辑或动画资源；Play 结果仍需人工确认。

### 2026-09-08 WorldMap 云层范围与移速校正

- 上一版使用 `BaselineLine_云` 的手写范围，现改为显式引用 `WorldMap_Main.unity` 中已有的 `天空` SpriteRenderer；云层移动区间根据天空和云层的真实渲染边界计算。
- 云层速度恢复为上一版的 `0.12` 参数，并保留原有“中心位置 + 正弦位移，到端点后折返”的运动方式；只把正弦振幅改为天空可见范围的一半。
- 本次不改 `天空` 或云层的尺寸、位置、Sprite、相机、室内入口、动画资源和其他 WorldMap 交互；Play 结果仍需人工确认。

### 2026-09-08 WorldMap 云层速度再次校正

- 在保留天空派生范围、正弦往返和端点折返方式不变的前提下，`WorldMapAmbientAnimationController` 的 `_cloudMoveSpeed` 从 `0.12` 降为 `0.06`，即当前移动速度减半。
- 本次不改云层移动范围、折返方式、天空/云层视觉资源或其他 WorldMap 系统；Play 结果仍需人工确认。
