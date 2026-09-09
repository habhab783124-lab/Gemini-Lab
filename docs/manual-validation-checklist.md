# Gemini-Lab 人工验证清单

## 2026-09-09 双宠交流气泡验证

- 52 项 EditMode 全部通过，涵盖双方三种回应、先后显示、提前关闭、精力不足、过期清理及只结算一次。
- Play 30 组话题/回应全部通过：气泡文本与资产完全一致，无姓名前缀，无 TMP 文本溢出，所有气泡 Graphic 的 raycastTarget 为 false。
- Play 已验证跟随移动、翻转不镜像、独立家具姿态位置、停用组件清理、切换收藏页面后清理；水晶球/扭蛋机仍通过实际 UI 点击及建造模式拦截检查。
- Game View 已检查先发起与双方回应画面；Play Console 0 error。测试过程中曾触发既有 EditMode EmotionGardenService 恢复异常，最终测试通过。
- 已退出 Play，旧中央 Conversation 节点不存在，气泡默认隐藏，时序保存为 2.5 / 8 秒，仅两只原层级宠物；存档 SHA256 全部恢复一致，无新增文件。
- 证据 `Logs/DoorBubbles/`；真实键盘手工操作、其他窗口比例和独立构建未覆盖。

## 2026-09-09 室内小门专项验证

- 已通过：38 项 EditMode；Unity Play 模拟视口点击开门、身体区域点选；F 优先处理入口下 10 个话题 × 3 回应；关闭对话不重复结算，其他页面禁止交流；箭头随选中与页面开关正确显示。
- 已通过：最终设置下 10 条家具路径实际物理步进，均到达且未越界；Game View 对话/开门形态检查，无文字溢出，最终 Console 0 error。
- 已退出 Play，场景已保存且只有两只原层级宠物；原存档哈希一致且无新增文件。
- 人工仍可复核：不同窗口尺寸下的阅读与操作手感；真实键盘连续 WASD→F→Esc；独立构建与长时间运行未覆盖。


Updated: 2026-08-07

## 公寓移动优化验证（2026-09-09）

35 项 EditMode 测试通过（0 失败/跳过），包含 7 项新增导航回归以及输入、动画方向、遗留物回归。证据在 `Logs/ApartmentMovement/final-tests.json`。

| 检查项 | 结果 | 方式 |
| --- | --- | --- |
| 房间限制包含脚底偏移，左右镜像不改变边界 | 通过 | EditMode + Scene 参数 |
| 绕家具、不可达目标、斜向滑动、高速薄障碍 | 通过 | EditMode |
| 10 个家具站立点实际到达且不越界 | 10/10 通过 | Play 模式逐步 Physics2D.Simulate，非仅路径查询 |
| 两宠各 8 方向、速度 10 持续移动 | 16/16 通过 | Play 实际物理步进，全程检查脚底碰撞体边界 |
| 天使弹琴完成后继续行为、恶魔绕床后睡眠 | 通过 | 正常 Update/FixedUpdate 24 秒观察与 Game View 截图 |
| 存档保护 | 通过 | 独立测试存档，原文件 SHA256 恢复核对一致 |
| 任意家具摆放组合、长时间运行、独立构建 | 未覆盖 | 复杂/封闭布局仍需补验 |

最后 Play Console 0 error，测试已停止；完整实现与保守碰撞包围盒限制见 `docs/apartment-movement.md`。

## 遗留物完善验证（2026-09-09）

Unity 2022.3.62f3，通过本地 MCP 连接当前项目验证。`RoomRelicServiceTests`、`RoomRelicIntegrationTests`、`DeferredSaveRestoreTests` 与 `SaveSystemTests` 合计 29 项 EditMode 测试通过，0 失败、0 跳过。最终结果在本机忽略目录 `Logs/RoomRelicPlayCheck/final-editmode.json`。

| 检查项 | 结果 | 备注 |
| --- | --- | --- |
| Unity 脚本编译 | 通过 | 修改后完成重编译与域重载，无 C# 编译错误 |
| 版本 1 迁移、首次奖励同日重启、消费后恢复 | EditMode 通过 | 同日不重复抽取，次日正常判定 |
| 晚注册模块恢复、等待期间保存、切换待恢复存档 | EditMode 通过 | SaveCoordinator 注册事件补恢复 |
| 双房间解锁、暂停/恢复订阅、Bootstrap 重新接入 | EditMode 通过 | 组件入口测试，不等同完整场景往返 |
| 赠礼固定槽位、读档刷新、弹窗失败不消费 | EditMode 通过 | 已验证 shelf 先获得不会占 desk 槽 |
| Scene/Runtime 视觉契约 | 通过 | 预置节点与 Sprite 引用存在，无运行时最终视觉生成 |
| 场景作者化迁移范围 | 通过 | 新增 24 个槽位字段，4 个为 desk/shelf；另保存弹窗字号、颜色、关闭按钮与赠礼布局修复；未改 Sprite |
| 公寓 → 世界地图 → 公寓 | Play 通过 | 通过场景服务真实加载；两房间各 1 件赠礼保留，降到好友度 40 后仍在 |
| 房间真实物理触发与双房间解锁 | Play 通过 | 重置空状态、重新启用触发器后，真实物理帧生成两房遗物；好友度 80 时各获赠礼，无直接调用进入方法 |
| 独立测试目录实际保存和读取 | Play 通过 | slot_3 保存后清空模块再读取，赠礼恢复且已读纸条不复活；原存档 SHA256 全部恢复一致 |
| 公寓视口点击、弹窗阅读与关闭 | Play 通过 | EventSystem RaycastAll + 模拟 pointerClickHandler，命中 ApartmentViewportImage，经桥接器打开；关闭返回 SpaceSys |
| 弹窗排版与作者化持久性 | Game View 截图通过 | 正式纸条内容、关闭 X、遗物详情和赠礼提示可读；重新进入 Play 使用 Scene 保存的布局；未逐项截图对照所有 Scene 状态 |
| 正常 Boot → 主菜单 → 公寓完整 UI 流程、独立构建 | 未覆盖 | 本轮从已打开的 Apartment + Boot 进入 Play，不声称全游戏验收 |
| 速写、小星星吊坠正式图标 | 未完成 | 素材仍缺，保留原有占位 |

测试后 Console 出现现有 `EditorBootSceneLoader.InitBootstraps` 调用 `EmotionGardenRuntimeBootstrap.Awake` 导致编辑模式执行 `DontDestroyOnLoad` 的异常（后者第 36 行）。相关源文件不在本轮修改范围，未顺带修复；不能宣称整个项目 Console 零错误。

最后 Play Console 为 0 error（`Logs/RoomRelicPlayCheck/final-play-console.json`），截图包括 `note-final.png`、`relic-detail.png` 和 `gift-fixed.png`。旧 Catalog 内存对象曾显示占位文案，卸载并重新加载该资产后，域重载与再次 Play 已确认使用磁盘新文案。测试已停止，原存档目录无新增文件。

## B18. 苹果资源系统（2026-08-14）

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 新档进入游戏后苹果余额为 20，Apartment 四个资源栏复用原有 `BalanceLabel` 显示 `20` | 脚本与场景通过，Play 待目视 | `AppleRuntimeBootstrap` 已保存到 `Boot/BootstrapRoot`，不再新增 `AppleBalanceLabel` |
| 等待或用开发者时钟快进 6 小时后点击「大树 1」～「大树 5」，树缓存苹果转入余额 | 未验证 | 每棵树每 6 小时 1 个、单树最多缓存 3 个；需在 WorldMap PlayMode 逐棵点击 |
| 同一棵树重复点击不会重复领取；退出并重启后未领取缓存仍保留 | 未验证 | 检查 `apple` 存档中的 TreeId、LastGeneratedUtcTicks、PendingCount |
| 花朵首次成熟奖励 1 个苹果，重复成熟不重复奖励 | 脚本通过 | `EmotionGardenService.BloomAt` 只在状态首次转为 Bloomed 时调用 `IAppleService.Add(1)` |
| 扭蛋单抽/五连分别消耗 1/5 个苹果；余额不足按钮不可用且服务不扣余额 | 脚本通过，Play 待目视 | 四个页面资源栏和 GachaPanel 都读取苹果余额，文本只显示数字；金币服务不再驱动该资源栏 |
| 塔罗开始抽牌消耗 1 个苹果；余额不足不进入选牌；解读完成后不会自动再扣一枚苹果 | 脚本通过，Play 待目视 | `TarotService.CreateSession` 扣费，面板回到 Idle 等待下一次明确点击 |
| AppleService Capture/Restore 往返后余额、树缓存和时间戳一致 | 通过 | `AppleResourceServiceTests` 覆盖 |
| Scene 与 Play 视图未新增运行时树/苹果/UI 视觉对象 | 脚本与场景通过 | 运行时只更新 Scene 中已有 `BalanceLabel` 文本和树交互状态 |

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
| 点击邮箱和 5 棵大树能看到 `[ClickableSceneObject]` 占位日志 | 未验证 | 具体业务交互尚未由策划确定 |

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
- 当前未能在本机调用 Unity Editor / `unity-mcp-cli` 实跑 Unity Test Runner，EditMode / PlayMode 结果需在 Unity 内补验。

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
| 选择单花或花丛后显示半透明 Scene 预览 | 未验证 | 预览变体已作者化在 `FlowerPlacementPreview`，运行时只切换显隐和颜色透明度 |
| 预览位置按花丛 Sprite 完整尺寸在 X/Y 两轴网格吸附 | 脚本与场景通过，Play 待目视 | `PlacementGrid.mat`、10 条竖线、5 条横线已保存；单元尺寸为 `4.01 x 2.24` |
| 点击有效草地后使用预置槽显示正式花卉 | 代码修复，需有库存的 Play 存档复测 | 启动日志已确认 `32/32` 槽位、`1152` 个绑定；`WorldMapPlacementSlot` 直接验证可进入占用状态；不使用运行时 Instantiate |
| 点击无效区域不会落位或残留预览 | 未验证 | 有效区域由独立 `FlowerPlacementBounds` 提供，不复用宠物移动边界 |
| 放置模式显示覆盖区域的完整二维网格线 | 脚本与场景通过，Play 待目视 | `FlowerPlacementGrid` 下已保存 10 条竖线和 5 条横线 |
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
- 夜幕资源：`Assets/_Project/Art/WorldMap/garden/天气（最上层）/夜幕.png`

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| Scene 中存在 `WorldMapNightOverlay`，并引用现有夜幕 Sprite | 未验证 | 场景 YAML 已写入，需 Unity Inspector 确认引用 |
| 白天时夜幕隐藏 | 未验证 | 当前作者化时间为 14:42，场景保存为隐藏 |
| 夜晚时夜幕显示并覆盖整个室外场景 | 未验证 | 需使用系统时间或调试时间跨过 18:00 验证 |
| 夜幕位于室外场景和桌宠上方、UI 下方 | 未验证 | SpriteRenderer sorting order 为 2000，UI 使用独立 Canvas |
| 夜幕不阻挡宠物、场景物和花朵交互 | 未验证 | 夜幕 BoxCollider2D 已禁用 |
| 跨越昼夜边界后运行时自动切换 | 未验证 | `WorldMapDayNightController` 每 5 秒检查 `IGameClock.Now` |

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

- 小门后续回归：43 项 EditMode 全部通过；实际 UI Raycast → PointerClick 验证 8 张纸条、10 个临时遗物、2 个正式素材赠礼。两宠均通过实际物理步进的开门跨越、关闭阻挡、隔墙阻挡、访客不瞬移回家、占用门口拒绝关闭。存档哈希已恢复，编辑器退出 Play，原层级宠物仅两只。测试恢复后 Console 记录既有 `EmotionGardenService` 在 EditMode 调用 DontDestroyOnLoad 的异常，本次未改该模块。

- 水晶球/扭蛋机专项：实际 UI 射线点击跳转正确，其他页面和建造模式不误打开；20 个遗留物点击及 10 条家具物理路线回归通过，Game View 素材/布局已检查，Play Console 0 error；原存档哈希恢复一致，未测试独立构建。

- 控制标识遮挡修复：Game View 前后对照确认箭头不再盖住普通点击气泡；Play 两宠身体点击均通过，所有气泡 Renderer 排序高于标识，过期后气泡隐藏且当前宠物控制箭头正常显示。Console 0 error，证据 `Logs/BubbleIndicatorFix/`。
