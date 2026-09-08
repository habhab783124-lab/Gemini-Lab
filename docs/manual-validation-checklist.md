# Gemini-Lab 人工验证清单

## 室内家具选中反馈（任务 2）

| 验收项 | 状态 | 说明 |
|---|---|---|
| 点击现有家具显示其外轮廓描边和对应句子；切换家具只保留一个选中状态 | 待 Play 验证 | 通过 `ApartmentViewportInputBridge` 与 `ClickOcclusionUtility` 路由 |
| 点击视口空白清除描边和句子 | 待 Play 验证 | 运行时只切换 Scene 中已作者化节点 |
| 场景中存在 `ApartmentFurnitureSelection`、`FurnitureSelectionHighlight`、`FurnitureSelectionMessage` | 已通过静态契约 | 原 9 个目标加现有 `家具_装饰_储物的家具_恶魔_01` 共 10 个目标已落盘；仍需 Play 目视确认点击反馈 |

Updated: 2026-08-22

## B18 Play verification addendum (2026-08-18)

- Unity MCP Play verification confirmed outdoor `WorldMap_Main/室外背景/邮箱` routes through the serialized `WorldMapDailySummaryMailboxOpenTarget` to `Panel_DailySummaryMailbox`; the Apartment legacy entry is inactive.
- Both WorldMap pets have serialized `RandomWander` bounds X -18..18 and horizontal baseline Y -1.75. Their transforms changed during Play; `Pet_Angel` reported `IsMoving=true`, `MoveX=1`, `MoveDir=2` while moving.
- Daily-summary submission/autosave and restore remain covered by `EmotionGardenPlacementPersistenceTests` (5/5). The runtime probe was non-destructive and did not submit test data.

## B18. 苹果资源系统（2026-08-18 按新版需求修正）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 新档进入游戏后苹果余额为 20，Apartment 四个资源栏复用原有 `BalanceLabel` 显示 `20` | 脚本与场景通过，Play 待目视 | `AppleRuntimeBootstrap` 已保存到 `Boot/BootstrapRoot`，不再新增 `AppleBalanceLabel` |
| 按开发者时钟快进 45–90 分钟后点击「大树 2」～「大树 5」，树缓存苹果转入余额 | 脚本通过，Play 待目视 | 苹果树每轮 45–90 分钟、每天最多 5 轮、每轮 70% 为 1 个/30% 为 2 个；大树 1 不参与苹果逻辑 |
| 同一棵树重复点击不会重复领取；退出并重启后未领取缓存仍保留 | 脚本通过，Play 待目视 | 检查 `apple` 存档中的 TreeId、NextGenerationUtcTicks、GenerationDayKey、GeneratedRoundsToday、PendingCount |
| 花朵首次成熟奖励 12 个苹果，重复成熟不重复奖励 | 脚本通过 | `EmotionGardenService.BloomAt` 只在状态首次转为 Bloomed 时调用 `IAppleService.Add(12)` |
| 扭蛋单抽/五连分别消耗 20/100 个苹果；余额不足按钮不可用且服务不扣余额 | 脚本通过，Play 待目视 | 四个页面资源栏和 GachaPanel 都读取苹果余额，文本只显示数字；金币服务不再驱动该资源栏 |
| 塔罗开始抽牌消耗 8 个苹果；余额不足不进入选牌；解读完成后不会自动再扣一枚苹果 | 脚本通过，Play 待目视 | `TarotService.CreateSession` 扣费，面板回到 Idle 等待下一次明确点击 |
| 大树 2～5 无可领取苹果时显示“还没成熟哦”并播放两片落叶；有缓存时一次领取全部 | 场景与脚本通过，Play 待目视 | `AppleTreeFeedback` 的文本和两片落叶节点由 WorldMap Scene 作者化，运行时只切换显示和位移动画；大树 1 无该反馈 |
| AppleService Capture/Restore 往返后余额、树缓存、轮次和时间戳一致 | 通过 | `AppleResourceServiceTests` 覆盖新版存档字段及旧版迁移 |
| Scene 与 Play 视图未新增运行时树/苹果/UI 视觉对象 | 脚本与场景通过 | 运行时只更新 Scene 中已有 `BalanceLabel` 和苹果树反馈节点状态 |

## 使用方式
- 人工验证后直接在“结果”列填写：`通过` / `不通过` / `未验证`
- 如有问题，把现象写进“备注”列
- 智能体后续修问题时，优先读取这份清单，而不是重新口头追问

## A. 当前文档与工程骨架
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `AGENTS.md` 存在且可作为项目总入口阅读 |  |  |
| `docs/` 目录存在且可正常打开 |  |  |
| `docs/ai-memory/` 中主记忆、架构、规则历史、开发手册、文件指南都存在 |  |  |
| `docs/skill-design-boundary.md` 存在且内容与当前项目 skill 组织一致 |  |  |
| 文档中文显示正常，无乱码 |  |  |
| `memory-index.paths.txt` 中列出的路径都存在或说明清楚 |  |  |
| `README.md`、`Assets/README.md`、`Assets/plan.md` 可正常阅读 |  |  |
| 当前项目版本确认为 `Unity 2022.3.62f3c1` |  |  |
| `.cursor/mcp.json` 与技能目录可正常访问 |  |  |

## A2. Scene/Play 视觉一致性硬闸门
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 视觉任务卡已声明 `scene_play_parity_required: true`、`scene_visual_contracts` 和 `runtime_visual_files` |  |  |
| 关键 Sprite、AnimatorController、RectTransform、排序层级和 UI 节点在 Scene 视图中已有真实序列化引用 |  |  |
| 未进入 Play 时，Scene 视图已经能看到与目标最终效果一致的视觉资源 |  |  |
| 进入 Play 后没有被运行时脚本替换成另一套最终视觉；Scene 与 Play 取景和资源一致 |  |  |
| `tools/check-task-gate.ps1`、`tools/check-scene-visual-contract.ps1`、`tools/check-runtime-visual-contract.ps1` 均通过 |  |  |
| 运行时只切换已作者化对象/状态或填充数据，不通过 Sprite 赋值、AnimatorController 赋值或动态 UI 节点生成作者化最终视觉 |  |  |

## B. Phase 1 基础工程与 FSM
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `Core` 目录已开始落真实 C# 代码 |  |  |
| asmdef 已按层级拆分完成 |  |  |
| `GameBootstrap` 已存在并能作为启动入口 |  |  |
| FSM 核心类已实现并可编译 |  |  |
| 宠物能在空场景中完成基本状态切换 |  |  |
| EditMode 测试可以运行 |  |  |

## B2. 框架 + 场景切换（P0 2026-05-10 新增）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 从 `Boot.unity` 点 Play 后自动跳到 `MainMenu` |  |  |
| `MainMenu` 的"开始"按钮点击后进入 `Apartment_Main` |  |  |
| `MainMenu` 的"存档"按钮打开 SaveSlots 面板（当前骨架尚未注册可见 Panel 属正常） |  |  |
| `MainMenu` 的"设置"按钮打开 Settings 面板（同上） |  |  |
| `Apartment_Main` 的右上 `UI_WorldMapPortal` 按钮可切到 `WorldMap_Main` |  |  |
| `WorldMap_Main` 的左上 `Return` 按钮可切回 `Apartment_Main` |  |  |
| `WorldMap_Main` 中 `A` / `D` 键可左右平移摄像头；鼠标右键拖拽同理 |  |  |
| 在任意场景按 `F10` 可在公寓与 Desktop Overlay 之间切换（`DesktopOverlayManager`） |  |  |
| 跨场景 `F10` 不再丢失 manager（切回 Apartment 后 F10 仍响应） |  |  |
| EditorBuildSettings 顺序为 `Boot(0) / MainMenu / Apartment_Main / WorldMap_Main / Desktop_Overlay` |  |  |
| `DesktopOverlayManagerEditModeTests` 通过 |  |  |
| Console 无 `CS` 编译错误（CJK 字形 □ 警告在补 CJK 字体前可接受） |  |  |

## B9. WorldMap 桥面行走（2026-07-28）
适用范围：
- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 目标对象：桥 GameObject `桥`
- 当前规则：桥对象上的 `PolygonCollider2D` 上侧轮廓是桌宠过桥移动轮廓的唯一事实源，不再使用 `_profileLocalPoints` 独立折线轨道。

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `WalkableSurface.TryGetSurfaceY` 在有启用的 `PolygonCollider2D` 时按当前 X 求所有边交点并取最高 Y | 通过 | `dotnet build GeminiLab.Modules.Pet.csproj --no-restore` 通过 |
| `PetController` 首次刷新 `WalkableSurface` 列表不会因 `int.MinValue` 帧差溢出而跳过 | 通过 | `_lastWalkableSurfaceRefreshFrame` 初值改为 `-WalkableSurfaceRefreshInterval` |
| `PetController.ResolveGroundY` 会把桥面 surface Y 从脚底/行走锚点换算成 transform Y | 通过 | `dotnet build GeminiLab.Modules.Pet.csproj --no-restore` 通过；PlayMode 仍需看脚底是否贴合桥上轮廓 |
| `WorldMapSceneObjectsPatch` 不再回填 `_useProfile/_profileLocalPoints` 独立折线轨道 | 通过 | `Assembly-CSharp-Editor.csproj` 构建通过 |
| `WorldMap_Main.unity` 桥对象不再序列化 `_useProfile/_profileLocalPoints` | 通过 | `rg` 搜索旧字段无命中 |
| `Pet_Angel` 玩家控制横向走到桥 X 范围时，脚底/行走锚点会沿桥 `PolygonCollider2D` 上轮廓抬升和下降 | 未验证 | 需在 Unity PlayMode 使用 `A/D` 或方向键验证 |
| `Pet_Devil` 玩家控制横向走到桥 X 范围时，脚底/行走锚点会沿桥 `PolygonCollider2D` 上轮廓抬升和下降 | 未验证 | 需在 Unity PlayMode 点击切换主控后验证 |
| 未被选中的桌宠自动横向漫游经过桥时，脚底/行走锚点同样跟随桥上轮廓 | 未验证 | 需在 Unity PlayMode 等待随机漫游过桥 |
| 离开桥两端后，桌宠回到横板基准地面而不是停留在桥面高度 | 未验证 | 需在 Unity PlayMode 验证左右两端 |

## B10. WorldMap 情绪花图鉴 UI 美术接入（2026-07-29）
适用范围：
- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 目标面板：`Panel_EmotionCollection`
- 列表页资源：`Assets/_Project/Art/WorldMap/UI/flowerCodex`
- 详情页资源：`Assets/_Project/Art/WorldMap/UI/flower_info`
- 当前实现原则：书本、卡槽、未知卡、左右箭头、关闭按钮、库存条和文本区域均由 Scene 中真实 UI 子节点承载；`FlowerCollectionPanelStub` 只负责运行时填数据与切换 `CodexView` / `DetailView`，不在 `Awake` / `Start` 临时拼最终视觉。

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `FlowerCollectionPanelStub` 使用 `_codexView`、`_detailView`、`_cardSlots` 与详情字段的序列化引用，不再运行时生成旧列表条目 | 通过 | `dotnet build GeminiLab.Modules.HubUI.csproj` 通过 |
| `WorldMapEmotionGardenUIPatch` 的 `SetupFlowerCollectionBookContent` 会从 `UI/flowerCodex` / `UI/flower_info` 加载拆分美术资源，而不是直接使用整张 mock 合成图 | 通过 | `dotnet build Assembly-CSharp-Editor.csproj` 通过；场景已恢复当前 Sprite GUID 引用 |
| 图鉴卡片和详情通过 `EmotionFlowerArtCatalog` 按情绪类型 + 培育者显示 `Assets/_Project/Art/WorldMap/flower` 中的真实花图 | 通过 | Catalog 资产已包含 18 个组合映射；缺失的恶魔孤独完整图回退基础花图 |
| `AutoSetup` 升级到版本 22 后，会调用 `WorldMapEmotionGardenUIPatch.Patch()` 自动落地图鉴 UI authoring | 通过 | `dotnet build Assembly-CSharp-Editor.csproj` 通过 |
| 本地 Unity batchmode runner 可执行 editor static method，且不会无期限卡住 | 通过 | `tools/run-unity-editor-method.ps1` 已加入 `-nographics`、启动日志超时、总执行超时和子进程 watchdog；`WorldMapEmotionGardenUIPatch.watchdog-test.log.runner.log` 验证 5 秒未生成日志时会停止本次 Unity PID |
| 执行 WorldMap UI authoring 后，`Panel_EmotionCollection/Content/CodexView` 下存在 `Book`、`TitlePlate`、`CategoryTabs`、`Cards/CodexCardSlot_00...11`、`PreviousPageButton`、`NextPageButton`、`CloseButton`、`ProgressText`、`PageText`、`ClickHintText` | 通过 | Unity batchmode patch 成功，日志在 `Logs/UnityBatchmode/WorldMapEmotionGardenUIPatch.codex-list.log`；`rg` 已在 `WorldMap_Main.unity` 命中列表页关键节点；需在 Scene 视图继续做视觉微调 |
| 执行 WorldMap UI authoring 后，`Panel_EmotionCollection/Content/DetailView` 下存在 `Book`、`FlowerImage`、`StockPlate/StockText`、详情文字字段、返回/翻页/关闭按钮 | 通过 | `rg` 已在 `WorldMap_Main.unity` 命中 `DetailView`、`StockPlate`、`FlowerImage` 等节点；需在 Scene 视图继续做视觉微调 |
| PlayMode 中点击 `Btn_EmotionCollection` 打开图鉴列表页，关闭按钮可关闭面板 | 未验证 | 需 Unity PlayMode 人工验证 |
| PlayMode 中已解锁卡片可切到详情页，详情页返回按钮可回到列表页，左右按钮只在存在可切换项时可用 | 未验证 | 需已有情绪花数据或调试数据 |
| 打开 `Panel_EmotionInput` 与 `Panel_EmotionCollection` 时，另一个已开的顶层面板会自动关闭，不会同时叠在一起 | 未验证 | 路由已改为互斥切换，待 Unity PlayMode 再确认视觉表现 |

## B11. WorldMap 房子遮挡点击与双宠碰撞（2026-07-30）
适用范围：
- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 目标入口：`室内` / 返回公寓入口
- 目标宠物：`Pet_Angel`、`Pet_Devil`
- 当前规则：点击入口时先判断鼠标点下的最上层 2D collider；如果房子被桌宠或 UI 遮挡，则入口不响应。WorldMap 中双宠不应互相碰撞挡路。

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `ClickOcclusionUtility` 已收口到 `Assets/_Project/Scripts/Core/DevMode.cs`，且 `CabinReturnPortal` / `WorldMapGardenZone` / `ClickableSceneObject` / `BaselineItem` / `PetPlayerInputController` / `PetClickReactionController` / `WorldMapCameraController` 都改为先裁决最上层点击目标 | 通过 | `dotnet build Assembly-CSharp-Editor.csproj` 通过 |
| `PetController` 的 WorldMap 场景级双宠碰撞忽略逻辑已编译进运行时 | 通过 | `dotnet build Assembly-CSharp-Editor.csproj` 通过 |
| 被 `Pet_Angel` / `Pet_Devil` 或 UI 遮挡时，`室内` 不再误触发跳转到公寓 | 未验证 | 需要 Unity PlayMode 人工验证 |
| `Pet_Angel` 与 `Pet_Devil` 在 `WorldMap_Main` 中不会互相挡路 | 未验证 | 需要 Unity PlayMode 人工验证 |

## B17. WorldMap 可交互场景物体悬停反馈（2026-08-05）
适用范围：
- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 目标对象：`室内`、`邮箱`、`大树 1`～`大树 5`
- 当前规则：对象以自身 Scene 中保存的 localScale 为缩放基准；鼠标悬停在自身 Collider2D 范围内且未被 UI 覆盖时，平滑放大，移出后恢复。悬停反馈不受其他场景碰撞体排序阻断，实际点击仍执行最上层裁决。

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 7 个对象都挂载 `WorldMapInteractiveObjectFeedback`，且参数直接保存在 Scene | 已作者化 | 默认放大倍数 `1.06`，过渡时间 `0.08s` |
| 悬停 `室内` 时出现缩放反馈，同时保留原有高亮变色 | 未验证 | 需要 Unity PlayMode 人工移动鼠标确认 |
| 悬停邮箱和 5 棵大树时出现缩放反馈，移出后恢复原始大小 | 未验证 | 需要 Unity PlayMode 人工移动鼠标确认 |
| UI 覆盖对象时不触发错误缩放，其他场景碰撞体不会阻断目标悬停 | 未验证 | 需要 Unity PlayMode 与 Canvas / 花丛重叠区域人工确认 |
| 点击 `室内` 可以加载 `Apartment_Main` | 未验证 | 需要从 WorldMap PlayMode 点击室内确认 |
| 点击邮箱和大树 2～5 能看到 `[ClickableSceneObject]` 占位日志，大树 1 不触发点击入口 | 未验证 | 大树 1 的交互尚未由策划确定，暂不绑定苹果或通用点击逻辑 |

## B3. 塔罗垂直切片（B1 2026-05-10 新增）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 打开 Apartment → 侧边栏点"每日塔罗"，面板显示"抽今日塔罗"按钮 |  |  |
| 点按钮后卡面刷新、显示牌名 + 正/逆位、按钮变"今天已抽过，明天再来" |  |  |
| 右侧 AngelBubble 显示"（天使视角）… 愿光照亮你…" 或 Gateway 真实回复 |  |  |
| 右侧 DevilBubble 显示"（恶魔视角）… 别被光晃得太舒服…" 或 Gateway 真实回复 |  |  |
| 关闭 Panel 再打开，按钮保持"明天再来"状态（今日已抽过） |  |  |
| 次日重启游戏，按钮恢复成"抽今日塔罗" |  |  |
| Console 无 `TarotBootstrap` ERROR；看到 `[TarotBootstrap] TarotService registered.` |  |  |

## B4. Phase C 底座 + PetStatus 面板（2026-05-11 新增）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| Boot → MainMenu / MainMenu → Apartment 切场景时能看到黑幕淡入淡出 |  |  |
| 抽今日塔罗成功后，屏幕右下角弹出绿色 Toast 显示牌名 + 正/逆位 |  |  |
| Toast 自动淡出，不阻塞点击 |  |  |
| 打开任意面板后按 ESC 能关闭栈顶面板 |  |  |
| 连续按 ESC 能把面板栈全部关空 |  |  |
| 侧边栏点"宠物状态"，面板显示"天使/恶魔"两个 tab + 心情/精力/饱食三条进度条 + 7 维雷达图 |  |  |
| 点"恶魔"页签切换后宠物名改为"恶魔"、雷达图按恶魔人格刷新 |  |  |
| 宠物状态值变化时，面板数值实时更新（订阅 PetRuntimeSnapshotChangedEvent） |  |  |
| Console 无 `GameClock` / `Toast` / `SceneFadeOverlay` / `UIInputRouter` 相关 ERROR |  |  |

## B5. Phase D 面板全建（2026-05-11 新增）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 主菜单点"设置"打开 Panel_Settings，滑条拖动后立即生效（文本百分比同步） |  |  |
| Settings 的"重置为默认"按钮可用，关闭按钮按下或 ESC 都可以退出 |  |  |
| Settings 改动后重启游戏仍保留（PlayerPrefs） |  |  |
| 主菜单点"存档"打开 Panel_SaveSlots，显示 3 个槽位（空槽位为"空槽位"） |  |  |
| 对任一槽位点"新建 / 覆盖"后显示最后游玩时间，关闭重开仍可见 |  |  |
| 侧边栏"物品栏"：初始为空 Grid，调试加道具（`inventory.Add(...)`）后网格刷新 |  |  |
| 物品栏格子 hover 显示 tooltip（中文名 / 分类 / 说明） |  |  |
| 侧边栏"收藏"：抽塔罗成功后切到"塔罗记录"标签能看到新条目 |  |  |
| 收藏三个标签（旅行 / 塔罗 / 花园）切换空态提示正确 |  |  |
| Console 无 `SettingsBootstrap` / `InventoryBootstrap` / `CollectionBootstrap` ERROR |  |  |

## B6. Phase E 存档整合（2026-05-11 新增）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 启动后在主菜单打开"存档"面板显示 3 个槽位（默认均为"空槽位"） |  |  |
| 对任一槽位点"新建 / 覆盖"，摘要改为真实时间戳 `yyyy-MM-dd HH:mm:ss` |  |  |
| 调节音量 / 抽塔罗 / 调用 `InventoryService.Add` 后保存；重启游戏 → 同槽位"读取" → 状态全部恢复 |  |  |
| 读档后自动跳回公寓场景 |  |  |
| "删除"按钮能清掉槽位，摘要回到"空槽位" |  |  |
| 同一槽位反复 Save / Load 无异常，Console 不出现 SaveBundle 序列化错误 |  |  |
| 没有 SaveSlot 的 cold-start：TarotService 能从 PlayerPrefs 读到上次抽卡日期（兼容旧存档） |  |  |
| Console 看到 `[PersistenceBootstrap] SaveCoordinator registered.` 与四条 `*Bootstrap registered.` |  |  |

## B7. Phase F 宠物陈衰 / 每日重置 / 性格演化（2026-05-11 新增）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 公寓静置 3 分钟：`Satiety` 明显下降；`Energy` 持续下降；`Mood` 在两者正常时缓慢回升 |  |  |
| `Satiety` <= 30 或 `Energy` <= 20 时：`Mood` 恢复速度变慢（默认 0.2×），并被扣分 |  |  |
| 让宠物睡觉：`Energy` 上升 / `Mood` 少量回升；但 `Satiety` 仍然持续下降 |  |  |
| 跨天后启动游戏：Console 看到 `NewDayStartedEvent` 广播；塔罗按钮重新变"抽今日塔罗" |  |  |
| 同一天内反复重启：不会重复广播新一天事件 |  |  |
| 抽塔罗正位后：天使雷达某维度（默认善良/正直/冷静等）微升；抽逆位后：恶魔雷达相应维度微升 |  |  |
| 交互书柜：任一只当前宠物 Calmness / Integrity 微升；交互竖琴（天使）：Kindness / Calmness 微升 |  |  |
| 存档 → 重启 → 读档：宠物 Mood / Energy / Satiety / 最近交互 / 性格向量全部恢复 |  |  |
| Console 看到 `PersonalityEvolutionBootstrap` / `PetRuntimeBootstrap` / `DailyResetService` 注册日志 |  |  |
| Boot.BootstrapRoot 上已挂 `PersonalityEvolutionBootstrap`，且 `_rules` 指向 `PersonalityEvolutionRules.asset` |  |  |

## B8. Phase G 花园（实时 2 小时，2026-05-11 新增）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 启动新存档后打开侧边栏，看到第 5 个入口"Garden" |  |  |
| 打开 Garden 面板，看到 3×3 空地块 + 右侧 3 种种子（胡萝卜/番茄/小麦 各 ×5） |  |  |
| 点击种子选中（出现高亮），再点空地块 → 地块变成 Seeded；种子 -1 |  |  |
| 地块显示倒计时（格式 `MM:SS`） |  |  |
| 等过 Growing 阈值（默认 40 分钟，调试时可改 SeedDef 为 60 秒）→ 外观变亮 |  |  |
| 等到 TotalGrowSeconds → 格子切到 Ready，点击可收获 |  |  |
| 收获后：Inventory 增加对应 crop_*；Collection 的"花园收获"分类新增一条（首次） |  |  |
| 种后存档 → 退出 → 过 10 分钟 → 读档：倒计时按真实秒差已推进（离线补算生效） |  |  |
| Boot.BootstrapRoot 上 GardenRuntimeBootstrap 的 `_seedCatalog` 指向 `SeedCatalog.asset` |  |  |
| InventoryRuntimeBootstrap 的 `_starterItems` 配置了 3 种种子各 5 |  |  |
| Console 看到 `[InventoryBootstrap]` / `[GardenBootstrap]` 注册日志，无 ERROR |  |  |

## C. Phase 2 家具、建造与导航
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 公寓主场景已创建 |  |  |
| 家具 Prefab 已开始落地 |  |  |
| `Assets/_Project/Prefabs/Furniture/**` 中已存在当前全部家具 Sprite 资源对应的真实 prefab 资产 |  |  |
| `Assets/_Project/ScriptableObjects/FurnitureConfig/**` 中已存在当前全部家具 Sprite 资源对应的真实 `FurnitureDefinitionSO` 资产 |  |  |
| 长按 `V` 的建造模式可进入 / 退出 |  |  |
| 家具能正确吸附地板或墙体 |  |  |
| 家具放置合法性校验可用 |  |  |
| 当前阶段宠物可由玩家直接控制在场景中移动 |  |  |
| 家具摆放后导航系统能正确更新 |  |  |
| `Furniture/StaticFurnitureDecorOnly` 下的纯静态补景对象与现有可交互家具不会明显重叠或错位 |  |  |
| `Furniture` 根上的 `ApartmentSceneFurnitureBindings` 已把当前主要可交互对象正确接入家具系统，且不存在重复或空 `_target` 绑定 |  | 当前至少应覆盖首批关键家具，并扩展到镜子、地毯、园地毯、沙发、凳子、椅子、纸张、耳机、音响、柜体、窗台、玩偶、枕头、小圆镜、羽翼边柜、左下小家具、左下窄家具、恶魔盆栽、照片板等对象 |
| `SceneFurnitureDefinitionHint` 配置的类别和 Buff 会优先于名称推断生效 |  |  |
| 睡眠交互 / 装饰观察 / 休闲交互 三种交互类型会正确传递到运行时摘要与状态面板 |  |  |
| 镜子 / 地毯 / 沙发 / 凳子 / 椅子 / 画架 / 照片板 已进入对象级交互链路，且在场景中位置可达、不会明显错位 |  |  |
| 当前主控切到 `Pet_Devil` 后，靠近恶魔画架或点击其附近交互点并按 `F`，会播放 `画画` 并坐到 `家具_装饰_椅子_恶魔_01` |  |  |
| 当前主控切到 `Pet_Devil` 后，靠近恶魔沙发或点击其附近交互点并按 `F`，会播放 `玩掌机` 并停在 `家具_装饰_沙发_恶魔_02` |  |  |
| 恶魔播放 `画画` 与 `玩掌机` 时，人物始终显示在对应家具上方，不会被椅子 / 画架 / 沙发遮挡 |  |  |
| 窗台 / 床上玩偶 / 沙发上枕头 已使用更贴近对象语义的交互类型，而不是继续借用植物或沙发语义 |  |  |

## D. Phase 3 Gateway 与 AI 交互（后续规划）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 当前阶段已明确暂不接入大模型，本节默认可标记为 `未验证 / 暂缓` |  |  |
| Gateway 配置资产已落地 |  |  |
| 聊天模式请求链路可用 |  |  |
| 工作模式请求链路可用 |  |  |
| 响应中的 `traceId` 可追踪 |  |  |
| 断网 / 超时 / 重试策略可验证 |  |  |
| 敏感字段未出现在日志中 |  |  |

## E. Phase 4 UI、桌面 Overlay 与旅行
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 聊天面板、状态面板、库存 UI 可正常显示 |  |  |
| 桌面 Overlay 模式可以进入 |  |  |
| 点击穿透可切换且行为正确 |  |  |
| 前台应用感知功能行为符合预期 |  |  |
| 旅行指令可以触发离场与回归 |  |  |
| 相册与旅行小记可展示 |  |  |
| 公寓模式与 Overlay 模式切换正常 |  |  |

## E2. Apartment UI 非美术技术收口（2026-05-26）
适用范围：
- 本节只验证非美术技术链路，不验证 `profile`、`spacesystem`、`tarot` 美术资源落图。
- 当前脚本级静态检查已执行：`git diff --check` 通过。
- 当前已可通过 Unity MCP / `unity-mcp-cli` 实跑 Unity Test Runner；EditMode 定向结果已记录，PlayMode 仍需在 Unity 内补验。

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `ApartmentViewportInputBridge.TryLocalPointToViewportPoint` 对视窗中心点、越界点、无效 Rect 有 EditMode 测试覆盖 | 未验证 | 已新增测试文件，但需在 Unity Test Runner 中运行确认 |
| viewport 点击只在点落入 `ApartmentViewportImage` 矩形内时转换到世界坐标 | 未验证 | 需在 PlayMode 点击视窗四角和视窗外区域 |
| 建造模式开启时，viewport 左键/右键优先交给 `BuildModeController`，不再继续触发桌宠或家具点击链路 | 未验证 | 需在 PlayMode 分别验证放置、删除、非建造模式点击 |
| 侧边栏在直接打开 `Apartment_Main` 调试时仍能解析或创建 `IUIRouter` / `EventBus` | 未验证 | 需在不经 Boot 直接 Play 的场景中验证 |
| 侧边栏切换 `PetStatus`、`SpaceSys`、`Tarot`、`Inventory` 时，高亮状态与面板开关同步 | 未验证 | 需在 PlayMode 逐项点击侧边栏 |
| `PetStatus` 在 `IPetRoster` 未就绪或缺宠物数据时显示 `--`，不残留旧值 | 未验证 | 需在服务缺失或调试场景中验证 |
| `Tarot` 在 `ITarotService` / `ICollectionService` 未就绪时给出明确提示或 Warning | 未验证 | 需在 Boot 缺失和正常 Boot 两种路径验证 |
| `Inventory` 在物品栏为空或服务未注册时显示明确空状态，且不依赖新美术资源 | 未验证 | 当前未绑定 `_emptyHint` 时会复用 Tooltip 文本区域 |
| `Assets/_Project/Prefabs/UI/Panels` 与 `Assets/_Project/Prefabs/UI/Widgets` 已建立工程落点说明 | 通过 | 只建立 README 与 `.meta`，未制作实际 UI prefab |
| `GeminiLab.Modules.HubUI.asmdef` 已显式引用 `GeminiLab.Modules.Furniture` | 通过 | 解决 viewport 桥接引用 `BuildModeController` 的工程依赖 |

## F. 美术与资源替换
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 新 Sprite 放置在 `_Project/Art/` 对应目录 |  |  |
| 第三方资源没有误放进 `_Project/Art/` |  |  |
| 新资源命名符合规范 |  |  |
| Sprite 导入设置符合项目约定 |  |  |
| SpriteAtlas / RuleTile / Animator / Prefab / SO 引用已同步更新 |  |  |
| 替换后场景中无丢图、错位或排序错误 |  |  |

## G. 文档同步
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 文件结构变化后已刷新主记忆与文件指南 |  |  |
| 场景结构变化后已更新结构总览 |  |  |
| 玩法变化后已更新玩法规范 |  |  |
| 手工验证结果已回写本清单 |  |  |

## H. Apartment 物件交互与状态显示联调
适用范围：
- 目标场景：`Assets/_Project/Scenes/Apartment/Apartment_Main.unity`
- 只验证当前已有美术资源支撑的内容
- 当前阶段桌宠移动入口以玩家控制为准：`WASD` / 方向键
- 暂不要求验证缺失美术资源对应的更完整交互动画或正式人格雷达美术

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 打开 `Apartment_Main.unity` 后场景能正常进入 PlayMode |  |  |
| 场景中的现成家具对象会被 `FurnitureService` 注册，而不是只有运行时新摆放家具才能交互 |  |  |
| `Pet_Angel` 挂载玩家输入组件后，可通过 `WASD` 控制移动 |  |  |
| `Pet_Angel` 挂载玩家输入组件后，可通过方向键控制移动 |  |  |
| 无输入时桌宠停留在 `Idle`，有输入时切换到 `Moving` |  |  |
| 玩家控制移动时，桌宠不会再自行发起默认自主寻路 |  |  |
| 玩家控制移动方向会正确驱动前 / 后 / 侧面动画切换 |  |  |
| 鼠标左键点击 `Pet_Angel` 后，会输出当前表情的 Debug 文本并弹出本地语料气泡回复 |  |  |
| 与床交互后，状态面板中的 `Energy` 有提升或符合预期变化 |  |  |
| 与竖琴/休闲家具交互后，状态面板中的 `Mood` 有提升或符合预期变化 |  |  |
| 最近一次交互结果会显示在左上角状态面板的 `Last Interaction` 文本中 |  |  |
| 左上角状态面板会实时显示 `State / Mood / Energy / Satiety / Target / Work` |  |  |
| 右侧库存面板会显示当前 build palette 条目，且包含已有资源对应家具定义 |  |  |
| 按 `V` 可切换建造模式，`Tab` 可轮换 palette，左键摆放、右键移除可用 |  |  |
| 摆放或移除家具后，右侧库存面板中的 `Placed` 数量会同步变化 |  |  |
| 摆放墙面类家具时，其排序表现高于普通地面家具，未出现明显遮挡错误 |  |  |
| 右下角概览面板会显示 `Mood / Energy / Satiety / Work Focus` 百分比文本 |  |  |
| `WorkDesk` 仍可作为工作目标使用，不影响已有工作链路 |  |  |
| 当前文本面板可以工作，即使还没有正式雷达图 / 交互动画美术，也不会阻断本轮联调 |  |  |

## I. Pet 基础动画资源补齐范围确认
适用范围：
- 目标资源目录：`Assets/_Project/Art/Sprites/Pet/Frames/`
- 目标动画目录：`Assets/_Project/Animations/Pet/`
- 目标场景对象：`Assets/_Project/Scenes/Apartment/Apartment_Main.unity` 中的 `Pet_Angel`
- 本节先确认“现有资源可以直接支持哪些补齐项”，不要求本节内直接产出缺失美术

当前确认事实：
- `Move/` 目录当前已切换为三组真实序列帧子目录：`正面 / 背面 / 侧面`
- `Idle/`、`Emotion/` 目录当前为空；`Interact/` 已新增 `read/` 与 `beside door/` 两组状态帧
- 当前已有 `Pet_Angel_Move_Front.anim`、`Pet_Angel_Move_Back.anim`、`Pet_Angel_Move_Side.anim`
- 当前已有 `Pet_Angel.controller`，但其中只有 `Move_Front / Move_Back / Move_Side`
- `Apartment_Main.unity` 里的 `Pet_Angel` 目前还没有实际 `Animator` 组件挂载记录
- 当前编辑器工具 `PetMoveAnimationSetupEditor` 已可优先读取 `正面 / 背面 / 侧面` 子目录，并保留旧命名规则兜底

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `Frames/Move/正面`、`背面`、`侧面` 序列帧目录存在且编号连续、无缺帧 |  |  |
| `Pet_Angel_Move_Front.anim` 可对应 `正面` 6 帧移动序列 |  |  |
| `Pet_Angel_Move_Back.anim` 可对应 `背面` 6 帧移动序列 |  |  |
| `Pet_Angel_Move_Side.anim` 可对应 `侧面` 6 帧移动序列 |  |  |
| `Pet_Angel.controller` 当前只包含 `Move_Front / Move_Back / Move_Side` 三个状态 |  |  |
| `Pet_Angel.controller` 已补入 `Idle_Front / Idle_Back / Idle_Side / Sleep` 四个状态 |  |  |
| 当前 controller 参数与 `PetController` 驱动保持一致：`IsMoving / MoveX / MoveY / MoveDir` |  |  |
| `PetController` 当前会用 `Move_Front / Move_Back / Move_Side` 实现四方向移动表现：前后分离、左右共用侧面并通过 `flipX` 翻转 |  |  |
| `PetController` 当前会在非移动状态下按最后朝向切到 `Idle_Front / Idle_Back / Idle_Side` |  |  |
| `PetController` 当前会在 `SleepingState` 下播放 `Sleep` 动画 |  |  |
| `Apartment_Main.unity` 中的 `Pet_Angel` 已绑定 `Pet_Angel.controller` 引用 |  |  |
| `Pet_Angel` 在进入 PlayMode 后会自动补 `Animator` 并使用现有 Move controller |  |  |
| 使用现有资源可以直接进入补齐范围的内容：移动动画 clip 校验、controller 整理、场景 `Animator` 挂载与移动表现验证 |  |  |
| 使用现有资源暂时不能直接完成的内容：独立 `Idle` 序列、独立 `Interact` 序列、独立 `Emotion` 序列 |  |  |
| 若不新增美术，本轮不应把 `Idle / Interact / Emotion` 扩展成“正式完整资源交付” |  |  |
| 后续若进入正式制作，优先顺序应为：先挂 `Animator` 并验证 Move，再补 `Idle`，最后补 `Interact / Emotion` |  |  |

## K. Idle / Sleep 动画接线
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `Pet_Angel_Idle_Front.anim` 已接入新增正面待机帧并设置循环 |  |  |
| `Pet_Angel_Idle_Back.anim` 已接入新增背面待机帧并设置循环 |  |  |
| `Pet_Angel_Idle_Side.anim` 已接入新增侧面待机帧并设置循环 |  |  |
| 三个 `Idle` clip 都满足首帧保持 4 帧、尾帧保持 4 帧 |  |  |
| `Pet_Angel_Sleep.anim` 已接入睡觉帧并设置循环 |  |  |
| `Pet_Angel.controller` 已包含 `Idle_Front / Idle_Back / Idle_Side / Sleep` 状态 |  |  |

## J. Pet 新增交互动画接线
适用范围：
- 新增状态帧目录：`Assets/_Project/Art/Sprites/Pet/Frames/Interact/read/`
- 新增状态帧目录：`Assets/_Project/Art/Sprites/Pet/Frames/Interact/beside door/`
- 新增动画资产：`Assets/_Project/Animations/Pet/Pet_Angel_Interact_Read.anim`
- 新增动画资产：`Assets/_Project/Animations/Pet/Pet_Angel_Interact_BesideDoor.anim`
- 规则：两个状态的首尾帧都应保持 10 帧时长

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `read` 状态序列帧已按当前资源目录完整纳入 clip |  |  |
| `beside door` 状态序列帧已按当前资源目录完整纳入 clip |  |  |
| `read` 状态帧当前命名符合 `Pet_Angel_Interact_Read_0001.png` 规则 |  |  |
| `beside door` 状态帧当前命名符合 `Pet_Angel_Interact_BesideDoor_0001.png` 规则 |  |  |
| `Pet_Angel_Interact_Read.anim` 的首帧保持时长为 10 帧 |  |  |
| `Pet_Angel_Interact_Read.anim` 的尾帧保持时长为 10 帧 |  |  |
| `Pet_Angel_Interact_BesideDoor.anim` 的首帧保持时长为 10 帧 |  |  |
| `Pet_Angel_Interact_BesideDoor.anim` 的尾帧保持时长为 10 帧 |  |  |
| `Pet_Angel.controller` 已包含 `Interact_Read` 和 `Interact_BesideDoor` 状态 |  |  |
| `PetController` 在 `Interacting` 时会根据目标家具类别切换到新增交互动画状态 |  |  |
| `WorkDesk` 与 `Leisure` 目标当前使用 `Interact_Read` 作为已有资源下的临时交互表现 |  |  |
| `Decoration` 目标当前使用 `Interact_BesideDoor` 作为已有资源下的临时交互表现 |  |  |
| `Apartment_Main.unity` 继续使用现有环境贴图，未擅自把 `公寓场景.psd` 替换进场景 |  |  |

## B12. WorldMap 每周培育面板与 UIbar（2026-07-30、2026-08-06、2026-08-10）
- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 目标面板：`Panel_WeeklyGarden`
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `CellTemplate` 在 Scene 层级中保留，但默认不可见 | 通过 | Scene YAML 已确认 `CellTemplate` inactive |
| `Panel_WeeklyGarden` 只显示 7 个可见瓶子 | 通过 | Scene YAML 已确认 `Day0`~`Day6` active，模板 inactive |
| `Panel_WeeklyGarden/Grid` 不再挂 `HorizontalLayoutGroup`，`Day0`~`Day6` 可单独拖动 | 通过 | `rg` 已确认相关文件无 `HorizontalLayoutGroup` / `LayoutElement` 残留 |
| `Content` 下只有一个集中 `UIbar`，Day0~Day6 下没有每日 UIbar | 通过 | Scene YAML 已确认 `Content/UIbar` 1 个，`Day0`~`Day6` 与 `CellTemplate` 下无 UIbar，根 localScale 为 `1.5, 1.5, 1` |
| `CellTemplate` 与 `Day0`~`Day6` 的 Bottle 固定显示 `bottle.png`，有花数据时不会因状态变体缺失而消失 | 脚本与场景通过，Play 待目视 | `WeeklyGardenPanelStub` 始终调用 `ShowPreview()`；Scene 中8个 Bottle 预览 Image 均保持启用 |
| 星期只显示 Mon～Sun 图片，不出现重复“周一/周二/周三”等文字 | 通过 | Scene YAML 已确认模板及7个日格的 `DayLabel/DayText` 全部 inactive |
| 集中 UIbar 有 `DateText`、`EmotionText`、`FlowerLanguageText` 信息区域 | 通过 | 作者化已写入 `Content/UIbar`；有花时填真实信息，无花时三个区域均为 `---` |
| 集中 UIbar 的 `Growth` 花头 icon 按培育者和情绪显示对应资源 | 脚本与场景通过，Play 待目视 | Scene YAML 已确认集中 `UIbar/Growth` 覆盖18种花头；无花时运行时隐藏，需在 Play 中准备不同花型数据确认图标与文字不重叠 |
| 每个 `Day0`~`Day6` 瓶内都有 `FlowerImage`，按当天情绪类型、培育者和状态显示对应花图 | 脚本与场景通过，Play 待目视 | 有花且已开花时显示带枝叶完整花图；空日期不显示花、成长 icon 或土壤 |
| `Panel_WeeklyGarden` 根节点 localScale 为 `0.85, 0.85, 1`，UIbar 字号为 `18/16/18` | 通过 | Scene YAML 已确认；PlayMode 重新打开后需目视确认取景和文字间距 |
| 当前周未选择瓶子时集中 UIbar 显示当天信息 | 未验证 | 需在 PlayMode 打开当前周，确认默认日期按 `IGameClock` 的当天索引显示 |
| 点击过去某一天瓶子后集中 UIbar 显示该天信息，点击另一个瓶子可切换 | 未验证 | 需在 PlayMode 点击两个不同日期的瓶子并核对日期、情绪和花名/状态 |
| 鼠标悬浮瓶子只缩放，不改变颜色 | 未验证 | 需在 PlayMode 观察瓶子本体颜色保持不变，离开后恢复原始缩放 |
| 选中瓶子只显示 Alpha 边缘外圈高亮，点击面板空白后取消选择并恢复当天信息 | 未验证 | 需在 PlayMode 确认 `SelectedHighlight` 只显示瓶子外轮廓、不填充瓶子内部，点击 `BlankClickArea` 后高亮消失 |

## B13. WorldMap 情绪花种植逻辑恢复（2026-07-31）
适用范围：
- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 目标面板：`Panel_EmotionInput` / `Panel_WeeklyGarden` / `Panel_EmotionCollection`
- 当前规则：`EmotionFlowerCatalog` 负责本地情绪判定和花名映射；`EmotionInputPanelStub` 提交原始心情文本；`WeeklyGardenPanelStub` 与 `FlowerCollectionPanelStub` 读取真实花数据并展示花名、情绪、培育者和状态。

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `EmotionFlowerModels.cs` 中的 `EmotionFlowerCatalog` / `EmotionGardenService` / 三个面板脚本可正常编译 | 通过 | Unity C# 与 Editor 程序集重新编译成功，AutoSetup 43 已执行 |
| `EditorBootSceneLoader` 已把 `EmotionGardenRuntimeBootstrap` 纳入 Awake 顺序，编辑器直启时也会注册情绪花园服务 | 通过 | `dotnet build Assembly-CSharp-Editor.csproj --no-restore` 已通过 |
| `WeeklyGardenPanelStub` 在瓶内按开花状态显示带枝叶 `FlowerImage`，并在 UIbar 内按当天花型显示花头 `Growth` icon | 通过 | `dotnet build GeminiLab.Modules.HubUI.csproj --no-restore` 已通过 |
| `WorldMapEmotionGardenUIPatch` 绑定 `_flowerHeadIconSprites` 并作者化 `UIbar/Growth` 的18种花头变体；AutoSetup 39 已执行 | 通过 | Unity 日志确认 AutoSetup 39 完成，Scene YAML 确认8个 Growth 节点均为 `(-96, 18)`、`36×36` 并覆盖18张花头 |
| `EmotionInputPanelStub` 不再提交固定“悲伤”，而是读取心情文本后交给花园服务判定 | 未验证 | 需 Unity PlayMode 输入一段心情文本验证 |
| 成功提交后会自动切到 `WeeklyGardenView`，不会继续停留在输入面板 | 未验证 | 需 Unity PlayMode 验证面板切换 |
| `WeeklyGardenPanelStub` 会通过 UIbar 显示真实日期、情绪关键词、花名/花语与开花状态，并在空格回退时恢复默认瓶子底图 | 未验证 | 需 Unity PlayMode 翻周 / 刷新验证 |
| `FlowerCollectionPanelStub` 会按情绪顺序和培育者顺序显示图鉴，点击已解锁卡片可进入详情页 | 未验证 | 需已有花数据或调试数据验证 |
| No.028 月晕在图鉴列表中显示悲伤·天使的真实花枝与土壤 | 脚本与场景通过，Play 待目视 | Scene YAML 已确认 `CodexCardSlot_01/FlowerImage` 使用 `悲伤|angel|1`，并与同卡 `SoilImage` 同时激活 |
| 图鉴详情页每种花都有独立花枝/土壤配对，土壤紧贴花枝底部 | 脚本与场景通过，Play 待目视 | Scene YAML 已确认 `Variant_00...16` 各自包含 `FlowerArt` 与 `SoilImage`，土壤 Y 坐标按花型分别保存 |
| 未解锁图鉴卡片不显示花朵、土壤或解锁文字 | 脚本与场景通过，Play 待目视 | Scene 中前三张为已收集预览，其余卡片为 `LockedImage` active，`FlowerImage`/`SoilImage`/`UnlockedContent` inactive；运行时锁定分支同时关闭 FlowerImage 根节点 |
| `Panel_EmotionInput`、`Panel_WeeklyGarden`、`Panel_EmotionCollection` 不会同时叠在一起 | 未验证 | 路由已是互斥切换，仍需 Unity PlayMode 确认 |

## B14. WorldMap 花朵自由摆放侧边栏（2026-08-10，2026-08-11 修正右侧布局）
适用范围：
- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 网格基准：`Assets/_Project/Art/WorldMap/garden/中景/花丛.png` 的完整 Sprite 尺寸（当前 `4.01 x 2.24` Unity 单位），该资源只作标尺，不作为摆放显示
- 侧栏资源：`Assets/_Project/Art/WorldMap/arrange`
- 花卉资源：`Assets/_Project/Art/WorldMap/花朵图鉴/花朵`、`花枝`、`花丛`

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| Scene 中存在右下角“布置”入口和右侧 `FlowerPlacementPanel` | 脚本与场景通过，Play 待目视 | Scene 已保存右侧锚定的 `Btn_FlowerPlacement`、`FlowerPlacementPanel` 和 `UIBoard.png` 原始尺寸引用；需进入 Unity 目视确认位置 |
| 点击“布置”后右侧滚动式花卉侧栏展开，旧底部库存栏、`FlowerButtons`/`SingleButton`/`ClusterButton`/`CancelButton` 和顶层 `FlowerPlacementStatusBar` 均不显示 | 场景通过，Play 待目视 | 旧原型节点已从保存场景清理；需 Unity PlayMode 验证 `FlowerSidebarViewport` 滚动与展开状态 |
| 展开花种详情时，选中标题和其上方条目不移动，详情固定出现在标题下方且不被后续条目遮挡 | 脚本与场景通过，Play 待目视 | `FlowerList` 是唯一纵向布局根，且已启用子项高度控制；条目高度为收起 73 / 展开 320，`ExpandedOptions` 锚定标题下方 |
| 从一个已展开花种切换到另一花种时，被点击标题栏保持视窗位置；仅新标题下方条目按详情高度整体位移 | 脚本与场景通过，Play 待目视 | `WorldMapFlowerPlacementController` 会在重排前后补偿 ScrollRect 的 content 偏移 |
| 侧栏包含 18 个情绪/培育者条目，并使用 arrange 正式 UI 资源 | 脚本与场景通过，Play 待目视 | `FlowerOption_00...17`、`item.png`、上下箭头、单花/花丛卡和合成按钮已保存 |
| 花种图标来自 `花朵`，单花来自 `花枝`，花丛来自 `花丛` | 脚本与场景通过，Play 待目视 | 不使用 `arrange.png` 裁剪花卉；Scene 已保存 18 组对应 Sprite 引用 |
| 单花和花丛显示共享库存，当前选项显示勾选标记 | 脚本通过，Play 待验证 | `EmotionGardenService` 按情绪类型与培育者持久化单花/花丛库存；开花事件与存档恢复事件会刷新现有侧栏节点 |
| 每周培育完成并开花的花可以在摆放侧栏中使用 | 脚本通过，Play 待验证 | `BloomAt` 增加同一 `PlacementFlowerInventory.SingleCount`；旧版存档在升级到版本 3 时按已开花记录迁移一次 |
| 同种单花数量不少于 3 时，点击合成只生成 1 个花丛并即时刷新数量 | 脚本通过，Play 待验证 | 合成直接调用共享服务，原子扣除 3 个单花并增加 1 个花丛；不再只改当前会话本地变量 |
| 摆放单花/花丛后对应共享库存减 1，退出并重新进入后数量保持 | 脚本通过，Play 待验证 | 成功摆放原子扣库存并串行 autosave；应用重启后应恢复原槽位和世界坐标，且不二次扣库存 |
| 同种单花不足 3 朵时显示 `组 5.png` 规则提示气泡 | 未验证 | 需在 PlayMode 用不足库存触发 |
| 选择单花或花丛后右侧库存侧栏自动收起，半透明预览和当前选中状态保留 | 代码修复，需有库存的 Play 存档复测 | 对应新版需求“选择形态后底栏自动收起”；重新点击“布置”可更换花种 |
| 选择单花或花丛后显示半透明 Scene 预览 | 未验证 | 预览变体已作者化在 `FlowerPlacementPreview`，运行时只切换显隐和颜色透明度 |
| 预览位置按花丛 Sprite 完整尺寸在 X/Y 两轴网格吸附 | 脚本与场景通过，Play 待目视 | `PlacementGrid.mat`、10 条竖线、5 条横线已保存；单元尺寸为 `4.01 x 2.24` |
| 点击有效草地后使用预置槽显示正式花卉 | 代码修复，需有库存的 Play 存档复测 | 启动日志已确认 `32/32` 槽位、`1152` 个绑定；`WorldMapPlacementSlot` 直接验证可进入占用状态；不使用运行时 Instantiate |
| 点击无效区域不会落位或残留预览 | 未验证 | 配置区域列表时只使用匹配 owner 的 `FlowerPlacementRegion_Angel` / `FlowerPlacementRegion_Demon`；仅无区域旧场景回退 `FlowerPlacementBounds`，不复用宠物移动边界 |
| Scene 中可以直接调整 `FlowerPlacementBounds`、`FlowerPlacementRegion_Angel`、`FlowerPlacementRegion_Demon` 的 Transform/BoxCollider2D，重跑作者化不覆盖手调尺寸 | 未验证 | 需在 Scene 调整尺寸后重新运行 WorldMap 花朵作者化并检查 Inspector |
| 天使花只能落在 Angel 区、恶魔花只能落在 Demon 区，跨区或 footprint 越界均显示无效 | 未验证 | 需分别选天使/恶魔单花和花丛，在两区边界与角落拖动预览并点击 |
| 放置系统的单花预览、侧栏单花卡和已摆放单花使用 `花朵图鉴/花朵放置` 下 18 张资源 | 脚本与场景通过，Play 待目视 | 已确认 9 种情绪 × 天使/恶魔的 18 个新 Sprite 均有 Scene 引用；花丛和花种列表图标保持原目录 |
| 放置模式不显示抽象二维网格线，但预览仍按内部网格吸附 | 代码与场景通过，Play 待目视 | `FlowerPlacementGrid` 下的线条仅作为作者化/调试资源保存，运行时保持隐藏 |
| 点击有效位置后保持摆放模式，可以连续放置 | 代码修复，需有库存的 Play 存档复测 | 提交期间忽略同步摆放恢复事件重入，需确认每次点击消耗对应库存、占用下一个预置槽并写入版本 4 `PlacedFlowers` |
| 摆放层与 `BaselineItem` 对齐，相邻层网格半格错位；同层同格不可重叠，跨层可形成遮挡；花朵与桌宠按同一基线决定前后 | 代码与场景通过，Play 待目视 | `Pet_Angel`、`Pet_Devil` 根对象已挂 `BaselineItem` 且保持 `solidCollider=true`；花朵与桌宠使用 `Default` Sorting Layer，先按 `BaselineItem.SortingOrder`，同排序值内按基线 Y（Y 越低越靠前），完全同线时桌宠略优先；需在 PlayMode 选择单花和花丛确认不同基线层及同层不同 Y 的相对遮挡 |
| 花丛按场景“花丛 3”尺寸占用一个网格 | 未验证 | 需确认花丛预览/正式节点均以约 `3.99 x 2.22` Sprite 尺寸落位 |
| 按 `Esc` 退出摆放模式，侧栏内 `PlacementStatus`、预览和网格线隐藏 | 未验证 | 旧顶层 `FlowerPlacementStatusBar` 已移除；运行时入口为 `WorldMapFlowerPlacementController` |
| 点击场景空白区域不会误触发 UI；关闭按钮可退出侧栏 | 未验证 | 需 Unity PlayMode 验证 UI 射线与场景点击边界 |
| Scene 与 Play 的侧栏、网格、花卉资源和槽位结构一致 | 脚本与场景通过，Play 待目视 | 运行时不创建最终视觉对象；需打开 Scene 与 Game 视图做最终目视对照 |
| 第一条花种位于侧栏标题下方，滚动列表不越出装饰窗口 | 场景通过，Play 待目视 | `FlowerSidebarViewport` 已保存左右 34、顶部 132、底部 56 边距，序列化结果为 `anchoredPosition=(0,-38)`、`sizeDelta=(-68,-188)` |

## B15. WorldMap 昼夜切换（2026-08-05）
适用范围：
- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 时间规则：本地时间 06:00–18:00 为白天，18:00–次日 06:00 为夜晚
- 夜幕资源：`Assets/_Project/Art/WorldMap/weather/夜幕.png`（旧 `garden/天气（最上层）` 资源不作为来源）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| Scene 中存在 `WorldMapNightOverlay`，并引用现有夜幕 Sprite | 未验证 | 场景 YAML 已写入，需 Unity Inspector 确认引用 |
| 白天时夜幕隐藏 | 未验证 | 当前作者化时间为 14:42，场景保存为隐藏 |
| 夜晚时夜幕显示并覆盖整个室外场景 | 未验证 | 需使用系统时间或调试时间跨过 18:00 验证 |
| 夜幕位于室外场景和桌宠上方、UI 下方 | 未验证 | SpriteRenderer sorting order 为 2000，UI 使用独立 Canvas |
| 夜幕不阻挡宠物、场景物和花朵交互 | 未验证 | 夜幕 BoxCollider2D 已禁用 |
| 跨越昼夜边界后运行时自动切换 | 未验证 | `WorldMapDayNightController` 每 5 秒检查 `IGameClock.Now` |

## B15.1. WorldMap 当地天气切换（2026-08-18）

适用范围：
- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 天气节点：`WorldMapWeatherRainOverlay`；晴天使用场景底图
- 逻辑入口：`WorldMapWeatherController`、`WorldMapWeatherService`、`OpenMeteoWeatherProvider`

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| Scene 中仅存在雨天覆盖节点，且引用 `Assets/_Project/Art/WorldMap/weather/rain.png`；晴天不引用旧天气目录 | 已通过 | `WorldMapWeatherSunnyOverlay` 已移除，晴天由室外场景底图承担 |
| Scene 中不存在旧 `garden/天气（最上层）/圖層 28.png` 的 Sprite GUID 引用 | 已通过 | 旧 GUID 扫描结果为 0 |
| 控制器通过 Inspector 经纬度和 `timezone` 请求 Open-Meteo，默认刷新间隔为 30 分钟 | 已通过 | 默认上海 `31.2304, 121.4737` 是未接入定位服务前的占位配置 |
| WMO 晴朗/多云/雾归类为 Sunny，降雨/雷暴归类为 Rainy | 已通过 | `WorldMapWeatherTests` 覆盖分类边界与 JSON 解析 |
| 网络刷新成功时只启用对应天气 SpriteRenderer，另一层关闭 | 已通过 | Play 冒烟：默认 API 返回 code 3，晴天启用、雨天关闭 |
| 网络失败时保留最近成功状态；首次失败回退 Sunny，不清空场景 | 已通过 | EditMode 服务缓存/失败测试覆盖 |
| 天气覆盖与昼夜夜幕独立，Scene 与 Play 的节点结构一致 | 已通过 | 运行时只切换已作者化节点，不创建最终视觉对象 |
| 天气美术统一从 `Assets/_Project/Art/WorldMap/weather/` 接入，且替换资源不需修改天气逻辑 | 已通过 | 当前雨天使用 `weather/rain.png`；新增天气资源后只需在 Scene/Inspector 更新对应覆盖节点引用 |

### B15.2. WorldMap 云层与夜晚星星

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| Scene 中存在 `WorldMapWeatherClouds`，引用 `weather/云层.jpg` 并绑定 `Environment_Clouds` | 已作者化，待 Play 目视 | `WorldMapCloudColorKey.mat` 去除 JPG 白底 |
| Scene 中存在 `WorldMapWeatherStars`，引用 `weather/星星.PNG` 并绑定 `Environment_Stars` | 已作者化，待 Play 目视 | 节点由 `WorldMapDayNightController` 控制显隐 |
| 06:00–18:00 星星隐藏，18:00–06:00 星星显示 | 待 Unity Play 验证 | 使用项目 `IGameClock` 或调试时间跨越边界验证 |
| 云层、星星与室外背景保持 Scene/Play 相同取景和排序 | 待 Unity Play 验证 | 两个节点均为 Scene 中已保存的 SpriteRenderer，不运行时创建 |

### B15.3. WorldMap 环境动画

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `_SceneRoot` 保存 `WorldMapAmbientAnimationController`，并绑定云层、花朵根节点和 5 棵树 | 已通过 | 作者化日志：cloud=True，single visuals=594，trees=5 |
| 云层只沿 X 轴平滑移动，Y 不变且保持 `Environment_Clouds` 基线 | 已通过 | Play 运行时采样确认 X 变化、Y=2.337166 |
| 单朵花轻微旋转且相位不同，花丛保持静止 | 已通过 | Play 采样确认单花角度变化，Cluster 角度保持 0 |
| 许愿树和大树 2～5 以底部根点摆动 | 已通过 | 控制器绑定 5 个树目标，运行时不创建节点 |

## B16. WorldMap 桌宠数字键动画调试（2026-08-06）
适用范围：
- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 调试入口：`_SceneRoot` 上的 `WorldMapPetAnimationTriggerController`
- 当前规则：Game View 获得键盘焦点后，按数字键直接播放对应桌宠动画，不需要移动到任何位置

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| Scene 中不存在 `WorldMapAnimationTriggers` 和五个旧临时点位 | 未验证 | 这些对象已从场景落盘删除 |
| 按 `1` 播放天使 `Outdoor_Sit` | 未验证 | 天使坐地序列帧和 Clip |
| 按 `2` 播放天使 `Outdoor_Pray` | 未验证 | 天使祈祷序列帧和 Clip |
| 按 `3` 播放天使 `Outdoor_Happy` | 未验证 | |
| 按 `4` 播放天使 `Outdoor_Water` | 未验证 | |
| 按 `5` 播放恶魔 `Outdoor_Sleep` | 未验证 | |
| 按 `6` 播放恶魔 `Outdoor_Cast` | 未验证 | |
| 按 `7` 播放恶魔 `Outdoor_Proud` | 未验证 | |
| 特殊动画播放期间不被 Idle / Move 刷新覆盖，结束后恢复普通动画 | 未验证 | 数字调试组件执行顺序晚于 `PetController` |
| 特殊动画播放期间对应桌宠不能被 WASD/方向键或随机漫游移动 | 未验证 | `PetController.SetExternalMovementLock` 按桌宠独立加锁 |
| 天使播放特殊动画时恶魔仍可独立移动，反之亦然 | 未验证 | 移动锁不共享 |
| 天使和恶魔移动时分别播放对应方向的 `Move_Front` / `Move_Back` / `Move_Side` 动画 | 未验证 | 需确认实际帧序列和左右朝向；天使 `_sideFramesFaceLeft=true` |
| 不会影响 Apartment 场景的桌宠 Sprite、AnimatorController 和动画 | 未验证 | WorldMap 使用专用资源 |
| 自动巡航、自动到点触发和最终策划交互条件未启动 | 未验证 | 当前仅为动画联调入口 |

## B17. WorldMap 双宠动画调整预览场景（2026-08-06）
适用范围：
- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_PetAnimationPreview.unity`
- 对照场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 作者化入口：`Tools/Gemini-Lab/WorldMap/Create Pet Animation Preview Scene`

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 预览场景只有一个 `_SceneRoot`，其下仅有 `Main Camera`、`Pet_Angel`、`Pet_Devil` | 通过 | 场景 YAML 已确认 1 个根对象和 3 个子对象 |
| `Pet_Angel` 显示室外天使待机 Sprite，并有 Animator | 通过 | Scene YAML 已确认室外 Sprite GUID 与 `WorldMap_Angel.controller` |
| `Pet_Devil` 显示室外恶魔待机 Sprite，并有 Animator | 通过 | Scene YAML 已确认室外 Sprite GUID 与 `WorldMap_Devil.controller` |
| 预览场景与 `WorldMap_Main` 的天使 Animator Controller GUID 一致 | 通过 | 两边均引用 `df94c0ec7a0bf504696b47e0d89a7ea6` |
| 预览场景与 `WorldMap_Main` 的恶魔 Animator Controller GUID 一致 | 通过 | 两边均引用 `3f42a9a7549c7cc49b2176612fbf4c3f` |
| 在 Animation 窗口编辑共享 `.anim` / `.controller` 后，室外主场景显示相同动画资源 | 未验证 | 需在 Unity 中修改后切换 `WorldMap_Main` 检查 |
| Apartment 场景的宠物 Sprite / AnimatorController 未被预览场景新增引用 | 通过 | 预览场景只引用 `Art/WorldMap/pets` 与 `Animations/WorldMap/Pet` |
| 预览场景 Play 视图和 Scene 视图均显示相同室外桌宠资源 | 未验证 | 需打开预览场景并进入 PlayMode 目视确认 |

## B18. AI 每日小结邮箱与 WorldMap 自由行走（2026-08-18）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| WorldMap Scene 的 `室外背景/邮箱` 存在 `ClickableSceneObject`，且 UnityEvent 绑定到 `WorldMapDailySummaryMailboxOpenTarget.OnClick` | 通过 | WorldMap 场景 YAML 已保存 1 个持久化监听器；Play 中直接触发后 `DailySummaryContent` 为 active |
| WorldMap Canvas 保存 `Panel_DailySummaryMailbox`、`DailySummaryContent` 和隐藏入口对象，入口 `PanelId` 为 `29` | 通过 | Scene/Play 视觉契约与运行时编译检查通过；Apartment 同名旧入口为 inactive |
| 提交一条情绪后打开邮箱，显示当天输入、Summary、AngelNote、DevilNote | 已通过 MCP Play 路由/空状态验证；提交持久化由 5/5 定向测试覆盖 | 运行时仅填充已作者化 TMP 节点；未在用户存档中写入测试情绪 |
| 情绪提交后立即写入 `autosave`，重启后邮箱仍显示同一天的小结，旧存档缺少小结字段时不报错 | 代码/定向测试通过；完整重启由用户最终验收 | `PersistenceBootstrap` 已绑定提交事件；`DailySummaries` 写入存档，旧花朵记录可生成兼容摘要 |
| WorldMap 两只桌宠在无控制/无交互时于作者化边界内自由行走 | 已通过 MCP Play | 两只桌宠均在 X -18～18、Y -1.95～-1.65 内移动，水平基线为 Y -1.75 |
| 桌宠移动时切换现有 Move 方向状态，停下时回到 Idle；点击控制后暂停漫游 | 已通过 MCP Play/Animator 参数 | 移动时实测 `IsMoving=true`、`MoveDir=2`；使用现有 `PetController` Animator 参数，不新增运行时视觉节点 |
| Scene/Play 视觉契约、Unity 编译与任务闸门 | 已通过 | Scene/运行时契约、Unity 非编译状态、MCP 定向 EditMode 5/5 和闸门均通过；全量 EditMode 仍有既有 Furniture/MovingState 失败 |
## B19. Apartment 遗留物系统首轮（2026-08-20）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| Apartment Scene 保存 `ApartmentKeepsakeWorldPresentation`、4 个纸条点位、6 个纪念物点位及 `ApartmentKeepsakeOverlay` | 待最终人工验收 | Scene/Inspector 中应能直接替换 Sprite 与位置 |
| 首次进入室内当天只进行一次纸条判定，50% 出现且方向/文本随机，同日再次进入不重复抽取 | 定向 EditMode 已通过 | `ApartmentKeepsakeServiceTests` 覆盖日界线与刷新 |
| Relation 44/45/79/80 边界、纪念物首保底与后续 50% 刷新 | 定向 EditMode 已通过 | 低于 45 不解锁，达到 80 转赠礼规则 |
| 赠礼首次保底、后续每日 15% 且只抽未拥有项，重启后仍去重 | 定向 EditMode 已通过 | 收藏面板只显示已拥有槽位 |
| RenderTexture 视口点击纸条/纪念物可打开详情弹窗，关闭按钮可用 | 待最终人工验收 | 输入统一经过 `ApartmentViewportInputBridge` |
| Scene 与 Play 视觉契约、Unity 编译和 Play 启动 | 已通过自动检查 | Play smoke 已注册服务且无本任务异常；全量 EditMode 的 22 个旧 Furniture/MovingState 失败不属于本任务 |
## B20. AI diary resource-specific enlarged previews (2026-08-21)

| Check | Result | Notes |
| :--- | :--- | :--- |
| Outdoor mailbox opens the WorldMap daily-summary board using the `AI_diary` background and authored close/tabs | Pending final Play smoke | Scene contract checks that the Sprite references are saved in `WorldMap_Main.unity`. |
| Clicking each note, summary or character card opens its own matching enlarged resource | Pending final Play smoke | Verify `angel_note`, `summary`, `devil_note`, `angel_card` and `devil_card` one-to-one; do not show fixed `弹窗1.png`. |
| Popup close button and backdrop close return to the board without changing summary data | Pending final Play smoke | Runtime only toggles Scene-authored nodes and TMP text. |
| Main board matches the reference composition: title, left tabs, three notes, two character cards and close button | Pending final Play smoke | Scene YAML contains the calibrated positions/sizes; open the mailbox in Scene and Play to compare against the reference image. |
| Empty-state copy and selected tab read `今日小结` / `今天还没有写下心情，去花园留下一句话吧。` without duplicate legacy text | Pending final Play smoke | Runtime data still replaces the authored defaults when a persisted daily summary exists. |
| AI diary buttons keep their imported colors while hovered or pressed | Passed by Scene authoring | `DailySummaryMailboxAuthoring` sets `Selectable.Transition.None`; verify once in Play. |

## B21. AI diary date history and popup resource (2026-08-22)

| Check | Result | Notes |
| :--- | :--- | :--- |
| Left panel shows a scrollable list of saved summary dates | Pending final Play smoke | Wheel over `DateListViewport`; items are Scene-authored and use `unselected.png` when not selected. |
| Selected date uses `selected.png` and still shows its date | Pending final Play smoke | Click a second date after scrolling; selected state must move without creating a new item. |
| SummaryView displays the AI summary for the selected date | Pending final Play smoke | Verify visible text changes to the selected `GetDailySummary(dateIso)` record. |
| `PopupButton` opens the enlarged `弹窗1.png` resource | Pending final Play smoke | The button art is `弹窗.png`; the enlarged view is `PopupView`, not a fixed global popup image. |
| Detail popup has no pure-color PopupContent background | Passed by Scene contract | `PopupContent` Image is disabled/transparent; only the authored resource and modal backdrop remain. |

## B24. WorldMap 夜幕覆盖桌宠（2026-08-24）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `WorldMapNightOverlay` 使用位于 `Default` 之后的专用 Sorting Layer | 已作者化，待 Play 目视 | Sorting Layer 与场景 SpriteRenderer 引用均已保存 |
| 夜间夜幕覆盖背景、两名桌宠和已摆放花朵 | 待 Unity Play 验证 | 不能只确认背景变暗，需确认两名桌宠也被半透明夜幕着色 |
| 白天夜幕隐藏、夜幕碰撞体禁用 | 待 Unity Play 验证 | `WorldMapDayNightController` 仍按 `IGameClock` 切换，夜幕不参与点击 |

## B22. AI diary popup text overlay (2026-08-23)

| Check | Result | Notes |
| :--- | :--- | :--- |
| SummaryView detail shows the selected date's summary on the enlarged paper | Pending final Play smoke | Text must be inside the `summary.png` paper, not below the popup. |
| AngelNoteView and DevilNoteView show their corresponding note text on the paper | Pending final Play smoke | Text is the selected date's persisted AngelNote/DevilNote. |
| `弹窗1.png` keeps only its baked-in message without duplicate dynamic text | Pending final Play smoke | The shared PopupBodyText is hidden for `DetailKind.Popup`. |

## B25. Pet runtime save conflict resolution (2026-08-24)

| Check | Result | Notes |
| :--- | :--- | :--- |
| v2 `pet_runtime` capture contains `relation` and `savedAtUtcTicks` | Automated EditMode/static checks | Relation persistence and the offline timestamp are serialized together. |
| Restoring after two offline hours moves Mood from 80 to 74 at most, without changing Energy/Satiety | Automated EditMode | Offline regression is one point per five minutes, capped at six points. |
| Restoring a v2 payload restores Relation; restoring a v1 payload preserves the current Relation and skips offline regression | Automated EditMode | Covers backward compatibility for old saves. |
| Branch is rebased onto `upstream/main` with no unmerged paths | Passed | Only `PetRuntimeSaveService.cs` required manual conflict resolution; upstream deletions remain deleted. |

## B26. WorldMap 固定基线与环境层（2026-09-01）

| Check | Result | Notes |
| :--- | :--- | :--- |
| `FlowerPlacementGrid` 下恰好存在十一条 `BaselineLine_*`，包含天空、星星、云、树木、地面、花丛和人物层 | Automated static check | 仅检查该容器，不能以 `BaselineItem` 数量推断基线数量；云和星星本轮允许没有绑定美术对象。 |
| `WorldMap_Main` 的室外根对象、树木、建筑、桌宠、花朵容器和两块区域仍存在 | Automated static check | 本轮只定向移除旧基线线节点，不重建或覆盖场景。 |
| 花朵放置层只引用四条白色固定基线 | Automated static check | `Flower_Back`、`Flower_MidBack`、`Flower_MidFront`、`Flower_Front`。 |
| Scene 工具拖动基线时绑定物体跟随，物体保持基线相对偏移 | Pending Unity Scene validation | 打开 `Tools/Gemini-Lab/WorldMap/场景基线`，逐条拖动手柄并保存场景。 |
| Scene 工具按相对渲染顺序显示基线，并通过点击式前移/后移交换相邻层 | Pending Unity Scene validation | 列表按 `RenderOrder` 从大到小；数值越大越靠前但不代表基线条数；修改后应刷新该基线上的全部 `BaselineItem` 与花朵渲染器。 |
| `BaselineItem` Inspector 与基线工具显示并编辑同一份基线参数 | Pending Unity Scene validation | 绑定对象只保存 `WorldMapBaselineDefinition` 引用；Y、X 范围、放置许可和 RenderOrder 修改后应同步到同线物体与渲染器，不存在本地覆盖参数。 |
| 已绑定 `BaselineItem` 的物体锁定 Y 轴 | Pending Unity Scene validation | WorldMap 场景保留每个物体原有偏移；直接拖动物体或修改 Transform 的 Y 应恢复到 `BaselineY + 原偏移`，沿 X 拖动仍有效；移动基线手柄时同线物体 Y 应同步。 |
| Scene 与 Play 视觉层级一致，桌宠和花朵按固定槽位遮挡 | Pending Unity Play smoke | 需要 Unity 可用时在 Play 视图确认；静态脚本不生成最终视觉节点。 |

## B23. WorldMap 苹果树轮廓点击范围（2026-08-24）

适用范围：

- 目标场景：`Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity`
- 目标对象：`大树 2`、`大树 3`、`大树 4`、`大树 5`
- 当前规则：点击和悬停都必须使用各自 Sprite 可见轮廓的 `PolygonCollider2D`；透明区域不应响应。

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 四棵苹果树根节点各有一个有效 `PolygonCollider2D`，不再有旧 `BoxCollider2D` | 已作者化，待 Play 目视 | 轮廓在编辑器阶段生成并保存到 Scene；静态扫描需确认四棵树各自只有一个 Collider2D |
| 鼠标悬停在树的可见边缘时触发平滑放大，移出透明区域后恢复 | 未验证 | 需 Unity PlayMode 逐棵移动鼠标确认边缘覆盖 |
| 点击四棵树的可见轮廓能进入现有苹果树点击入口，透明区域不触发 | 未验证 | 需使用有苹果树交互入口的 Play 存档验证；不改变苹果生成/领取规则 |
| 大树 1 仍无苹果领取和通用点击入口 | 已静态确认 | 本轮只保留其既有悬停反馈 |

## B27. WorldMap PSD 相对位置与草地取景（2026-09-03）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| Scene 视图中天空、地面、建筑、桌宠与花朵保持 PSD 的相对位置 | 待 Unity Scene/Play 验证 | 本轮通过世界坐标偏移修复，未改变 PSD 子节点布局 |
| 活动相机完整覆盖天空和地面且无额外空白 | 待 Unity Play 验证 | 相机按天空与地面包围范围校准 |
| 已保存花朵重启后落在草地，并遵守基线 Y | 待 Unity Play 验证 | 旧存档恢复时改用解析出的基线 Y |
| 新花放置锚点位于 `FlowerPlacementBounds` 草地区域 | 待 Unity Play 验证 | 以锚点和花朵 footprint 同时检查 |

## B28. WorldMap PSD 局部坐标回归（2026-09-03）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `WorldMap_Main` 的天空、地面、桥、树、花丛和桌宠局部 Transform 与基线修复前的 PSD 作者化值一致 | 已静态核对 | 16 个误写的局部 Y 已恢复，未整体覆盖场景 |
| 活动相机使用原始取景且不显示下方大块空白蓝区 | 已通过 Unity Play 验证 | 2026-09-03 通过 Unity MCP Game View 截图确认 |
| 已保存花朵仍显示在草地并保持基线层级 | 已通过 Unity Play 验证 | 未清空存档，花朵恢复在草地带并遵守共享基线 Y |

## B29. WorldMap 许愿系统（2026-09-04）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `许愿树` 绑定 `WorldMapWishTreeInteractable` 且不绑定苹果领取逻辑 | 已作者化 | 点击入口显式绑定 `WorldMapWishSystemController` |
| 主界面、输入、详情、记忆列表和 12 个星位均存在于 Scene | 已作者化 | 面板位于 `Canvas/WorldMapWishSystemPanel`，活动星位位于主面板左上角插画许愿树区域 |
| 新增愿望随机占用一个槽位，超过 12 个时归档最旧记录 | 已通过代码检查 | 服务层维护 Active/Fulfilled/Archived |
| 愿望跨重启持久化 | 已通过 Unity Play 验证 | 已提交测试愿望，停止并重新进入 Play 后 `VisibleWishCount=1`，随后清理测试存档 |
| 点击 `item_button.png` 查看详情，支持实现、删除和全部列表 | 已通过 Unity Play 验证 | 星星仅作展示；详情页显示选中愿望并支持实现、删除与“全部”列表 |

## B30. WorldMap 愿望 UI 流程修正（2026-09-05）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 主面板星星范围 | 已通过 Play 验证 | 活动星星只出现在左上角插画许愿树区域，旧世界空间星位隐藏 |
| 星星点击行为 | 已通过 Play 验证 | 点击星星不会切换到详情页 |
| `item_button.png` 入口 | 已通过 Play 验证 | 打开唯一的详情/手册页面 |
| 详情列表与内容 | 已通过 Play 验证 | 右侧列表可纵向滚动，选中行显示 `item_selected.png`，左侧内容随选中行更新 |

## B31. WorldMap 云层与单花环境动画（2026-09-05）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| Scene 与 Play 中云层轮廓完整，未误删白色云朵 | 已通过 Play 验证 | `WorldMapWeatherClouds` 使用 `WorldMapClouds_Alpha.png` 与 `Sprites-Default`；两块云朵均可见 |
| 云层沿 X 轴缓慢移动 | 已通过运行态检查 | 由 `WorldMapAmbientAnimationController` 驱动已有 Scene 节点 |
| 至少两朵单花的 Z 角度随时间变化且相位不同 | 已通过运行态检查 | 默认幅度 3.2°、速度 0.9、相位由层级路径稳定生成 |
| 花丛不发生单花旋转 | 已通过运行态检查 | `_Cluster` 视觉节点不加入旋转列表 |
## B32. WorldMap 双宠动画触发状态机（2026-09-05）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 无特殊动作时，两个桌宠都按实际移动状态保持 Idle；移动时保持 Move | 待 Play 验证 | 基础状态仍由各自 `PetController` 驱动 |
| 天使漫游进入苹果树附近可随机坐地，进入许愿树附近可随机祈祷 | 待 Play 验证 | 只在进入范围时掷概率，离开后重置 |
| 天使移动到天使区域已摆放单花/花丛附近播放浇水 | 待 Play 验证 | 仅筛选 `Owner=angel` 的持久化摆放记录 |
| 恶魔漫游进入苹果树附近可随机睡觉，进入恶魔标牌附近可随机施法 | 待 Play 验证 | 标牌需有 Scene 引用；缺失时不创建占位物体 |
| 玩家控制天使/恶魔时按 F 在对应目标附近触发坐地/祈祷/睡觉/施法 | 待 Play 验证 | F 只路由到当前 `IsPlayerControlEnabled` 桌宠 |
| 天使区域摆花后播放开心，恶魔区域摆花后播放得意 | 待 Play 验证 | 首次从存档读取不触发，新增记录才触发 |
| 特殊动作结束后恢复对应桌宠 Idle/Move，另一只桌宠不受阻塞 | 待 Play 验证 | 两只宠物独立计时和移动锁 |
| 现有 `WorldMap_Angel/Devil.controller` 的 `Outdoor_*` Clip 实际被播放 | 待 Play 验证 | 本轮不修改 Clip/Controller 资产 |
## B33. WorldMap 苹果树掉落交互（2026-09-05）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 点击大树 2/3/5 后树快速晃动并出现 1–3 个苹果 | 待 Play 验证 | 苹果使用 Scene 中保存的 apple.png 引用 |
| 掉落苹果的分配值总和等于本轮固定总量 | 已通过 EditMode/代码检查 | 服务层保留批次总量，逐个领取 |
| 点击地面苹果后余额增加并显示“收获 +N”约 2 秒 | 待 Play 验证 | 槽位碰撞体经过点击遮挡判定 |
| 许愿树不触发苹果掉落 | 已通过作者化/代码检查 | 大树 1 与许愿树均排除 |

## B34. WorldMap 输入框视觉状态与情绪入口（2026-09-05）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 天使标牌点击打开天使情绪输入，恶魔标牌点击打开恶魔情绪输入 | 待 Play 验证 | 入口来自真实 `天使标牌` / `恶魔标牌` 的 `WorldMapGardenZone`，旧 EmotionEntry 占位节点停用 |
| 情绪输入框未聚焦显示带提示文字资源，点击后切换无字资源 | 待 Play 验证 | Scene 预置 `InputVisual` / `InputVisual_Focused`，运行时只切换显隐 |
| 情绪输入结束编辑或提交后恢复带提示文字资源 | 待 Play 验证 | 由 `TMP_InputField.onEndEdit` 与提交成功路径复位 |
| 许愿输入框未聚焦显示 `许愿树/input.png`，点击后切换 `UI输入框去字/wish_input.png` | 待 Play 验证 | 打开输入页不自动抢焦点 |
| 图鉴列表卡片和详情花图下方无土壤 | 待 Play 验证 | `FlowerCollectionPanelStub` 强制关闭图鉴土壤节点 |
| 每周培育面板仍显示土壤，右上 `Btn_EmotionInput` 不可见 | 待 Play 验证 | 每周节点未改动；旧按钮仅停用 |

## B35. WorldMap AI 情绪花园接入（2026-09-05）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 情绪提交调用共享 LLM 配置并生成花朵情绪 | 待 Play 验证 | 未配置/超时会显示本地兜底结果，不阻塞提交 |
| AI 关键词和花语写入并在每周培育面板显示 | 待 Play 验证 | 由 `WeeklyGardenPanelStub` 读取持久化花朵字段 |
| 每日总结显示选中日期的 summary、angelNote、devilNote | 待 Play 验证 | 由 `DailySummaryMailboxPanel` 读取持久化总结 |
| 关闭并重启后 AI 生成字段仍存在 | 待 Play 验证 | 存档版本升级到 5，旧记录会补齐字段 |
| 许愿面板保持原流程且未接入 AI | 已通过范围检查 | 本轮明确暂缓 |

## B36. WorldMap 室外新手指引（2026-09-06）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 点击 `Btn_OutdoorTutorial` 后显示 `intro` 页面 | 待 Play 验证 | 当前为可替换占位入口 |
| 点击下一页按 `intro`→`outdoor1`～`outdoor6` 顺序切换 | 待 Play 验证 | 首页/末页保持边界，不循环 |
| 点击上一页按相反顺序切换，首末页不越界 | 待 Play 验证 | 使用点击按钮事件 |
| 点击 `close.png` 关闭面板 | 待 Play 验证 | 面板关闭后入口仍可用 |
| Scene 中七页 Sprite 引用与 Play 显示一致 | 待人工验证 | 页面和布局均已保存到 `WorldMap_Main.unity` |
## B37. WorldMap 第一阶段室外点击交互（2026-09-07）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 点击大树 2、3、5 的真实树木轮廓后只触发对应树晃动与苹果掉落；许愿树不触发苹果逻辑 | 待人工 Play 验证 | 树木使用现有 PolygonCollider2D；本阶段未进入 Play |
| 点击每个已落地苹果后只领取对应数量，并在苹果上方显示现有 `CollectionText` 的“收获 +N”约 2 秒 | 待人工 Play 验证 | 领取成功后才显示；Scene 已有反馈引用 |
| 点击天使标牌、恶魔标牌、邮箱、许愿树分别打开既有面板/系统 | 待人工 Play 验证 | 入口来自真实可见物体区域 |
| 点击两只室外桌宠只负责选中/操控，不打开其他面板；背景、基线、种植区不抢点击 | 待人工 Play 验证 | 本阶段只检查静态路由与注册引用 |
## B39. WorldMap 点击映射与室外桌宠过桥恢复（2026-09-08）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 点击天使标牌只打开 Angel 情绪输入面板，点击恶魔标牌只打开 Devil 情绪输入面板 | 待人工 Play 验证 | 由 `WorldMapGardenZone._owner` 区分 |
| 点击邮箱打开 AI 每日总结，点击许愿树打开许愿系统 | 待人工 Play 验证 | 不应进入情绪输入或苹果逻辑 |
| 点击大树 2/3/5 只触发对应苹果树，点击大树 1 不触发苹果 | 待人工 Play 验证 | 树木轮廓内外各点一次 |
| 点击两只桌宠只切换对应控制权，A/D 或方向键移动时桌宠沿桥面高度行走 | 待人工 Play 验证 | 检查从桥左侧到右侧的连续过桥 |
| 特殊动作期间桌宠暂停移动，动作结束后恢复正常移动 | 待人工 Play 验证 | 本阶段只确认链路恢复，不调整动作 Clip |
| 背景、基线、种植区和无关 Collider 不抢走目标点击 | 待人工 Play 验证 | 逐个目标检查边缘和重叠区域 |

## B38. WorldMap Collider2D 点击命中区域修正（2026-09-08）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 邮箱、天使标牌、恶魔标牌和两只室外桌宠只在各自可见真实物体的 Collider2D 范围内响应 | 待人工 Play 验证 | 本次代码已移除 SpriteRenderer.bounds 命中分支；需在 Scene/Play 对照实际轮廓点击内外边缘 |
| 大树 2/3/5 仍只在各自真实 PolygonCollider2D 轮廓内触发，许愿树和落地苹果不发生点击错位 | 待人工 Play 验证 | 本次未修改场景 Collider 几何或已有树木/苹果命中实现 |
| 背景、基线、种植区等无关 Collider 不抢走目标点击 | 待人工 Play 验证 | 统一路由仍只裁决已序列化注册目标 |

## B40. WorldMap 苹果成熟与调试快进链路（2026-09-08）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 进入 WorldMap 后苹果服务已注册，三棵苹果树状态已建立 | 待人工 Play 验证 | 运行时监听场景加载并调用 `EnsureTree`，不创建视觉对象 |
| 点击调试按钮推进一天后，大树 2、3、5 可以进入现有掉落流程 | 待人工 Play 验证 | 重点确认不再因首次点击树而重新开始成熟计时 |
| 掉落数量为 1～3 个，数量总和固定，逐个领取增加对应苹果并显示已有 CollectionText | 待人工 Play 验证 | 本次未改 `AppleTreeDropController`、`AppleDropSlot` 或 Scene 引用 |
| 现有 AppleService 生成、存档和消费测试仍通过 | 待静态检查/编译 | Unity Play 实机结果尚未确认 |

## B41. WorldMap 苹果货币完整链路与文字可见性（2026-09-08）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 点击大树 2/3/5，在成熟批次存在时只掉落 1～3 个 Scene 苹果 | 待人工 Play 验证 | 领取总量由 `AppleService` 预留，分配和必须一致 |
| 点击每个落地苹果后才增加对应苹果货币 | 待人工 Play 验证 | 服务端拒绝超出当前预留余量的领取 |
| 成功领取后苹果上方可见 `收获 +N`，约 2 秒后隐藏 | 待人工 Play 验证 | `CollectionText` 使用 NotoSansSC 材质且解除旧实例材质覆盖 |
| 未成熟时可见 `还没成熟哦` | 待人工 Play 验证 | `StatusText` 使用已有 Scene 节点、中文字体和 9100 sorting order |
| 不成熟提示、掉落、领取均不动态创建 UI/Sprite/GameObject | 已静态检查 | 仅切换 Scene 已存在对象 |
