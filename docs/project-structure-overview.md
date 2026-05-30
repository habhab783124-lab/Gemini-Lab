# Gemini-Lab 项目结构总览

Updated: 2026-05-31

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
- 已有 `com.unity.ai.navigation`
- 有嵌入式 `SkillsForUnity`

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
- `Editor/` 已有编辑器脚本
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
- `Desktop/Desktop_Overlay.unity`

当前判断：
- 这 3 个场景已经足以说明仓库不再是“没有场景”的状态
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
- `2026-05-23` 起，恶魔也已拥有自己的门边交互动画 `Pet_Devil_Interact_BesideDoor.anim`，当前通过 `Pet_Devil.controller` 的 `Interact_BesideDoor` 状态接入
- `2026-05-27` 起，恶魔还新增 `Pet_Devil_Interact_Write.anim` 与 `Pet_Devil_Interact_PlayingMusic.anim`，当前分别通过 `Pet_Devil.controller` 的 `Interact_Write` 与 `Interact_PlayingMusic` 状态接入
- `2026-05-27` 起，`Apartment_Main.unity` 中恶魔现有玩家交互绑定已把旧的天使竖琴 / 写字目标替换为恶魔 `玩掌机 / 画画`：`玩掌机` 坐到 `家具_装饰_沙发_恶魔_02`，`画画` 对着 `家具_休闲_画架_恶魔_01` 触发并坐到 `家具_装饰_椅子_恶魔_01`
- `2026-05-30` 起，`Apartment_Main.unity` 的双宠仍共用 `PetPlayerFurnitureInteractionController` 作为 `F` 键家具交互入口，但最终显示策略已拆成可序列化的 `PetInteractionVisualStrategy`：当前天使显式保留 `Sleep / Interact_Flower / Interact_PlayingMusic / Interact_Write` 的 detached visual，恶魔显式让 `Interact_LookAround / Interact_PlayGame / Interact_Draw / Interact_DevilSleep` 继续走主 `Pet_Devil` 渲染器
- `2026-05-31` 起，恶魔交互显示异常的排查重点已转向运行时 Animator 覆盖链路；此前一度怀疑 `Pet_Devil.controller` 的 3 条 `Any State -> Idle_*` 过渡会抢回待机，但该假设已被用户否定，对应 controller 删除已撤回
- 同日 `PetController` 已补一轮最小交互位姿修复：当玩家交互启用 `UsePetPoseOverride` 且直接移动主宠物本体时，会同步运行态位置并临时阻止旧世界坐标回写把 pose 顶掉；当前该修复仍待 Unity Play 人工复测

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

## 推荐阅读顺序
1. `AGENTS.md`
2. `docs/ai-memory/gemini-lab-memory-main.md`
3. `docs/ai-memory/gemini-lab-project-file-guide.md`
4. `README.md`
5. `Assets/README.md`
6. `Assets/plan.md`
7. 再进入 `Assets/_Project/` 的实际脚本、场景与模块 README
