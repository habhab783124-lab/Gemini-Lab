# Gemini-Lab Memory Rules And History

Updated: 2026-08-21

## 长期规则
1. 所有中文文档、中文注释、中文说明都必须保持 UTF-8 正常显示。
2. 新增或修改文档时，要明确写出哪些内容是”已实现”，哪些只是”规划中”。
3. 不要把 README 中的目标状态误写成仓库现状。
4. 业务代码与资源统一放在 `Assets/_Project/`，不要把自研内容散落到第三方目录。
5. 模块之间不直接硬引用具体实现，只通过接口、事件或服务门面通信。
6. UI 不承载业务规则、网络请求和持久化逻辑。
7. 运行态数据不写回 ScriptableObject 资产。
8. 文件结构、场景结构或工具入口变更后，至少同步更新：
   - `memory-index.paths.txt`
   - 文件指南
   - 项目结构总览
   - 主记忆
9. 手工验证结果要落到 `docs/manual-validation-checklist.md`，不能只停留在聊天记录里。
10. 所有视觉类开发都必须保证 `Play` 视图与 `Scene` 视图效果一致；不要让运行时脚本覆盖 `Scene` 中已经调好的视觉结果。
11. 对视觉有影响的结果，优先作者化到 `Scene / Prefab / Inspector`，而不是依赖运行时临时生成。
12. 任何编辑器工具、脚本或代码改动，如果会产生以下效果，**必须先停下来询问用户确认**，不得擅自执行：
   - 修改 Unity scene 文件（.unity）
   - 修改场景中的 GameObject（创建、删除、移动、改名）
   - 修改场景中对象的组件属性（通过 SerializedObject 或其他方式）
   - 此规则同样适用于 `OnEnable`/`OnDisable`/`OnValidate` 等编辑器回调中的隐式场景修改——不能让脚本重编译或域重载悄悄破坏用户已在 Scene 视图中调好的对象。

## 当前已知问题
1. `Assets/_Project/Prefabs/` 与 `Assets/_Project/ScriptableObjects/` 已开始落地真实作者化资源，其中 `Furniture` / `FurnitureConfig` 已覆盖当前全部家具 Sprite 资源；`Pet`、`UI`、`Gateway`、`Travel` 等模块仍未完成同等程度的资产化。
2. `FurnitureService` 当前已支持优先使用真实 `FurnitureDefinitionSO`，但整个家具链路仍同时保留 `SceneFurnitureDefinitionHint` 和名称推断兜底，属于“作者化与原型并存”的过渡态。
3. `Packages/manifest.json` 已经包含 `com.unity.ai.navigation`，但当前 `NavigationService` / `NavMesh2DRebaker` 仍是占位实现，不等于真实 2D NavMesh 链路已完成。
4. `DesktopOverlay` 模块已有运行时代码，但 `WindowModeAdapter` 还没有落真实原生透明窗口 / 点击穿透实现。
5. 桌面相关文档目前同时存在“设计名 `Desktop`”和“运行时代码目录 `DesktopOverlay`”两套表述，阅读时要显式区分。
6. 文档体系在 2026-04-21 建立时曾按“骨架阶段”描述仓库，后续若实现继续推进，必须持续刷新文档，避免再出现口径滞后。
7. `Gateway`、`Travel` 与 AI 对话相关代码当前仍存在于仓库中，但 2026-05-08 起不再作为现阶段原型的默认开发入口；当前阶段应以玩家直接控制桌宠移动与场景交互为准。

## 最近进展

### 2026-07-12 — 误删场景对象事故与准则更新
- **事故描述**：
  - 为 `SaveSlotsPanel` 添加编辑器预览功能时，在 `OnDisable()` 中挂了 `ClearEditorPreview()` 方法
  - 该方法使用 `DestroyImmediate` 清空 `_slotContainer` 下的所有子物体
  - 当 Unity 脚本重编译触发 `OnDisable → OnEnable` 周期时，rei 在 Scene 视图中手动调好的 `Slot_slot_1` UI 对象被自动删除
  - 更严重的是，后续又写了一个"创建模板"工具，在未询问 rei 的情况下直接修改场景，进一步加剧了问题
- **违反的规则**：
  - 未遵循"先问用户再改场景"的原则（当时尚未明文化）
  - 在编辑器回调中放置不可逆的破坏性操作，且未考虑脚本重编译场景
- **新增长期规则 #12**：任何可能修改 scene 或场景对象的操作，必须先停下来询问用户确认
- **技术教训**：
  - `[ExecuteAlways]` + `OnDisable` 中绝对不能放 `DestroyImmediate` 清理逻辑——脚本重编译会触发完整的 OnDisable/OnEnable 周期
  - 编辑器预览应只在容器为空时创建，永远不主动删除用户手动创建或修改的对象
  - 需要"刷新预览"功能时，应提供显式的菜单按钮，而不是自动触发

### 2026-05-20
- 新增视觉一致性硬规则：
  - `Play` 视图与 `Scene` 视图必须一致
  - 视觉结果必须优先作者化到 `Scene / Prefab / Inspector`
  - 运行时脚本不得让 `Scene` 中已调好的视觉结果进入 `Play` 后失效
- 这条规则当前视为长期协作底线，后续涉及 UI、相机、场景展示、装饰层和布局任务时默认生效。

### 2026-05-13 最小闭环任务闸门
- 新增：
  - `docs/current-task-card.json`
  - `tools/check-task-gate.ps1`
- 当前项目的写操作前置条件升级为：
  1. 先更新 `current-task-card.md`
  2. 再更新 `current-task-card.json`
  3. 再通过 `check-task-gate.ps1`
- 当前仍然属于最小闭环版：
  - 先做任务卡机器检查
  - 暂未做更深层的写后审计和自动拦截包装

### 2026-08-21 工作流兼容增强
- 保留原有“探索 → 规划 → 用户确认 → 行动”顺序和四段式用户模板，不把机器字段暴露为新的用户操作。
- 新任务卡增加 `workflow_contract_version`、`task_id`、`human_approved`、`approval_source` 和 `plan_hash`；批准后的任务意图和边界变化会导致 hash 校验失败。
- `tools/check-task-scope.ps1` 以任务开始时的工作树路径和摘要为基线，检查当前任务是否新增或改变了未声明文件；它不会回滚或清理历史改动。
- `tools/verify-task.ps1` 作为统一收尾入口，组合 review 闸门、范围检查、差异空白检查和 PowerShell 语法检查，并以非零退出码阻止不完整任务进入完成状态。
- 该增强属于仓库级可执行防线，不是操作系统权限隔离；如果未来需要完全阻止直接文件工具绕过，应在外部 Agent 编排器或工具权限层增加写入 guardrail。

### 2026-04-21
- 浏览并梳理整个 Gemini-Lab 仓库。
- 校验了项目当时的真实状态：
  - Unity 版本为 `2022.3.62f3c1`
  - `docs/` 原本不存在
  - `_Project/` 当时主要仍处于脚手架阶段
  - MCP、skills 和嵌入式技能包已接入
- 依据《新项目 AI 工作环境搭建方法论》初始化了 `docs/` 体系：
  - `docs/ai-memory/`
  - 主记忆、架构、规则历史、开发手册、文件指南
  - 玩法规范、结构总览、人工验证清单、美术替换工作流
- 新增 `docs/project-skill-catalog.md`，整理项目本地 skill 清单。
- 新增根目录 `AGENTS.md`，把入口协议、项目规则和文档更新触发器正式固定下来。
- 依据方法论再做一次逐条合规审计，补齐过期表述、skill 设计边界和方法论对齐说明。

### 2026-04-27
- 将 `upstream` 远程从 HTTPS 调整为 SSH，以适配当前机器的 GitHub 访问方式。
- 抓取 `upstream/main` 并确认原本本地 `main` 与原仓库 `main` 历史不一致。
- 创建本地备份分支 `backup/main-before-upstream-sync-2026-04-27`。
- 将本地 `main` 对齐到 `upstream/main`，并强制推送到 `origin/main`，完成 fork 与原仓库的主线同步。
- 从同步后的 `main` 创建新开发分支 `docs/git-fork-pr-workflow`。
- 新增 `docs/git-fork-upstream-pr-workflow.md`，把当前项目的 fork / upstream / feature branch / PR 流程固化成正式文档。
- 修复 `Assets/_Project/Scripts/Modules/Pet/PetRuntimeData.cs` 缺少 `TargetFurnitureCategory` 字段的问题。
- 修复 `Assets/_Project/Scripts/Modules/Furniture/FurnitureModels.cs` 缺少 `FurnitureInteractionQuery.BedOnly` 的问题。
- 重新浏览仓库后确认：`_Project/` 已经存在真实场景、运行时代码、asmdef、测试程序集、示例动画与美术资源。
- 刷新主记忆、架构、文件指南、结构总览、skill 清单与索引，移除“没有场景 / 没有脚本 / 没有 asmdef / 没有导航包”的过期表述。
- 重新确认项目本地 skill 两套目录目前仍保持镜像关系，当前统计为 `72` 项。

### 2026-04-28
- 开始推进任务 1“物件交互和状态显示”。
- 在不新增缺失美术资源的前提下，优先接入已有内容：
  - 让 Apartment 场景里的现成家具对象注册进 `FurnitureService`
  - 让宠物运行时状态通过事件快照驱动现有 UI 面板
  - 让状态面板、家具库存面板、右下角概览面板显示真实运行时数据
- 新增或更新的关键代码点包括：
  - `Assets/_Project/Scripts/Modules/Furniture/FurnitureService.cs`
  - `Assets/_Project/Scripts/Modules/Pet/PetRuntimeSnapshotChangedEvent.cs`
  - `Assets/_Project/Scripts/Modules/UI/StatusPanelController.cs`
  - `Assets/_Project/Scripts/Modules/UI/FurnitureInventoryPanelController.cs`
  - `Assets/_Project/Scripts/Modules/UI/PersonalityRadarView.cs`
- 当前仍未验证：
  - Unity Editor 内的真实运行效果
  - EditMode / PlayMode 测试执行结果
- 开始推进任务 2“桌宠基础动画资源”的范围确认，而不是直接补图。
- 当前确认到的动画现状：
  - `Assets/_Project/Art/Sprites/Pet/Frames/Move/` 已有 `Front / Back / Side` 三组序列帧
  - `Idle/`、`Emotion/` 目录当前为空；`Interact/` 已新增 `read/` 与 `beside door/` 两组状态帧
  - `Assets/_Project/Animations/Pet/` 已有 3 个移动 `.anim` 和 1 个 `Pet_Angel.controller`
  - 当前 controller 只覆盖 `Move_Front / Move_Back / Move_Side`
- 因此，任务 2 现阶段可直接进入制作的范围是：
  - 移动动画 clip / controller / Animator 挂载链路补齐
  - 不包括缺失美术前提下的正式 `Idle / Interact / Emotion` 资源交付
- 已补的动画链路改动：
  - `PetController` 新增显式 `RuntimeAnimatorController` 引用，并在缺少 `Animator` 时自动补组件并赋值
  - `PetMoveAnimationSetupEditor` 现在会同时把 controller 回写到 `PetController`
  - `Apartment_Main.unity` 中的 `Pet_Angel` 已绑定 `Pet_Angel.controller` 资源引用
- 基于新增帧资源继续推进任务 2：
  - 新增 `Pet_Angel_Interact_Read.anim`
  - 新增 `Pet_Angel_Interact_BesideDoor.anim`
  - 这两个 clip 都按“首帧停 10 帧、尾帧停 10 帧”构建
  - `Pet_Angel.controller` 新增 `Interact_Read`、`Interact_BesideDoor` 两个状态
  - `PetController` 在 `Interacting / Working` 状态下会切到对应交互动画
- 当前策略约定：
  - `WorkDesk`、`Leisure` 暂时复用 `Interact_Read`
  - `Decoration` 暂时复用 `Interact_BesideDoor`
  - `公寓场景.psd` 当前仅作为新增环境资源存在，尚未在未确认切图映射前直接替换场景中的 `RoomBase` 贴图
- 开始修正“桌宠白天过于频繁选择 sleep 家具”的自主选目标问题。
- 当前定位到的根因：
  - 问题主要在 `MovingState` 的自主目标选择逻辑，而不是寻路算法本身
  - 旧逻辑对 `Bed` 的选择过于硬阈值化，且缺少“现实夜间”和“轻重疲劳区间”的分层判断
- 当前修正方向：
  - 新增白天 / 夜间的 bed 偏好阈值
  - 白天中等能量优先 `Leisure`
  - 夜间中等能量才更偏向 `Bed`
  - 补了 `MovingStateTargetSelectionTests` 覆盖白天 / 夜间 / 极低能量三种情况

### 2026-04-29
- 将 `公寓场景.psd` 在原目录下备份为 `公寓场景.backup.psd`。
- 将 PSD Importer 已导出的公寓场景大层子资源拆成独立 Sprite，放入 `Assets/_Project/Art/Sprites/Environment/ApartmentScene_Extracted/`。
- 公寓场景派生 Sprite 的正式命名规则改为“中文语义命名”。
- 当前第一轮实际重命名工作已转移到 `Assets/_Project/Art/Sprites/Furniture/Bed|Decoration|Leisure|WorkDesk/` 下进行。
- 为了配合中文命名，`FurnitureService` 的家具类别推断与部分 buff 推断已兼容中文关键词：
  - `床`
  - `工作桌 / 工作台 / 书桌`
  - `休闲 / 竖琴`
  - `装饰 / 床头柜 / 书柜 / 镜子 / 植物 / 摆件`
- 在恢复到 `93a9af5` 快照后，重新尝试了“只还原家具层效果”的方案：
  - 不改 `RoomBase`
  - 不整体平移 `Furniture` 根
  - 仅在 `Furniture` 下新增 `StaticFurnitureDecorOnly`，补入书柜、花盆方桌、窗台花和底部盆栽四个纯静态装饰

### 2026-05-01
- 开始进入“基于当前公寓场景真实游戏物体”的脚本编写阶段，而不是继续只改资源和场景 YAML。
- 新增 `SceneFurnitureDefinitionHint`，用于在 Scene / Inspector 中显式标注场景家具对象的：
  - 定义 ID
  - 家具类别
  - 放置类型
  - 占格尺寸
  - Buff
  - 是否进入 build palette
- 新增 `ApartmentSceneFurnitureBindings`，作为 `Apartment_Main.unity` 的场景绑定入口：
  - 当前挂在 `Furniture` 根上
  - 首批已扩展到 8 个关键对象：天使床、天使床头柜、天使竖琴、天使工作桌、恶魔工作桌、天使书柜、天使花盆方桌、天使底部盆栽
- `FurnitureService.ResolveSceneFurnitureDefinition` 现已优先读取 `SceneFurnitureDefinitionHint`，从“纯名称推断”升级为“显式提示优先、推断兜底”。
- 新增 `FurnitureServiceSceneHintTests.cs`，覆盖“场景提示优先于名称推断”的最小验证。
- 开始把静态家具交互从“类别级”推进到“交互类型级”：
  - 新增 `FurnitureInteractionType`
  - 当前首批重点落地类型：`SleepRest`、`DecorInspect`、`LeisureEngage`
  - `FurnitureService` 会为场景家具推断或读取交互类型与交互时长
  - `PetController`、`InteractingState`、`PetStateMachineBuilder`、`PetStatusViewModel` 已开始消费这些交互类型
- 在此基础上，继续把部分家具推进到对象级交互类型：
  - `SleepInBed`
  - `InspectBookshelf`
  - `InspectMirror`
  - `InspectNightstand`
  - `PlayHarp`
  - `PlayGuitar`
  - `PaintAtEasel`
  - `ViewPhotoBoard`
  - `ObservePlant`
  - `RestOnRug`
  - `LoungeOnSofa`
  - `SitOnSeat`
- `Apartment_Main.unity` 当前已把镜子对象补进场景显式绑定；
  现在已继续把镜子、恶魔地毯、恶魔沙发、恶魔凳子、天使凳子、恶魔画架、恶魔照片板、恶魔椅子接进 `Apartment_Main` 的显式绑定或静态摆件层。
- 当前阶段的重点已经从“只给类别”推进到“给场景里真实出现的具体对象配置对象级交互类型与持续时长”。
- 已继续把第三轮装饰类对象推进到对象级交互类型并接入场景绑定：
  - `InspectPapers`
  - `ListenToAudio`
  - `OrganizeStorage`
- 已继续把第四轮剩余装饰对象推进到对象级交互或场景绑定：
  - 窗台与窗台盆栽继续复用 `ObservePlant`
  - 床上玩偶当前继续作为轻观察对象处理
  - 沙发上枕头当前继续并入 `LoungeOnSofa`
- 已对 Apartment 家具交互链路做一轮精修：
  - 清理 `ApartmentSceneFurnitureBindings` 中 3 组重复 `_target`
  - 把 `花盆方桌` 的场景定义从误写的装饰类修正为 `WorkDesk`
  - 新增 `ObserveWindow`、`InspectToy`、`ArrangePillow` 三个对象级交互类型，替换此前对窗台、玩偶、枕头的语义借位
  - 让 `小圆镜`、`园地毯`、`左下小家具`、`左下窄家具` 的脚本覆盖情况与交互覆盖表重新对齐
### 2026-05-03
- 继续处理“已有资源但未完全落场景”的尾项：
  - 修掉 `ApartmentSceneFurnitureBindings` 中 `照片板` 的空 `_target`
  - 把场景里已存在的 `园地毯` 正式接进显式绑定
  - 把 `小圆镜`、`羽翼边柜`、`恶魔盆栽`、`左下小家具`、`左下窄家具` 真正补进 `StaticFurnitureDecorOnly`
  - 新增对象同步接入 `ApartmentSceneFurnitureBindings`，不再停留在“脚本能识别但场景没对象”的状态
- 桌宠移动美术资源已切换到新的目录结构：
  - `Assets/_Project/Art/Sprites/Pet/Frames/Move/正面`
  - `Assets/_Project/Art/Sprites/Pet/Frames/Move/背面`
  - `Assets/_Project/Art/Sprites/Pet/Frames/Move/侧面`
- `PetMoveAnimationSetupEditor` 已更新为优先读取上述三个子目录，并保留旧前缀命名兜底。
- `Pet_Angel_Move_Front.anim`、`Pet_Angel_Move_Back.anim`、`Pet_Angel_Move_Side.anim` 的 sprite 引用已切到新移动资源。
- 当前桌宠移动规则明确为：
  - 前进使用正面动画
  - 后退使用背面动画
  - 左右移动共用侧面动画
  - 左右差异由 `PetController` 里的 `SpriteRenderer.flipX` 处理

### 2026-05-06
- 已开始为当前 Apartment 交互家具批量生成真实作者化资产：
  - `Assets/_Project/ScriptableObjects/FurnitureConfig/**` 下已生成对应 `FurnitureDefinitionSO`
  - `Assets/_Project/Prefabs/Furniture/**` 下已生成对应 `Prefab`
  - `Apartment_Main.unity` 已开始转为引用这些 Prefab 实例，而不再只依赖纯场景直挂对象
- `FurnitureService.ResolveSceneFurnitureDefinition` 现已新增“优先读取场景对象上已赋值的真实 `FurnitureDefinitionSO`”逻辑。
- 为了完成这轮作者化，新增了编辑器工具：
  - `Assets/_Project/Scripts/Editor/Furniture/ApartmentFurnitureAuthoringBootstrapEditor.cs`
- 已继续把剩余 `11` 个未作者化家具资源补齐，当前 `Art/Sprites/Furniture/**/` 下的全部 `49` 个家具 Sprite 资源都已有对应 `FurnitureDefinitionSO` 与 `Prefab`。

### 2026-05-07
- 已统一整理宠物 `Interact` 帧命名：
  - `read/` 目录改为 `Pet_Angel_Interact_Read_0001...0006.png`
  - `beside door/` 目录改为 `Pet_Angel_Interact_BesideDoor_0001...0005.png`
- `Assets/_Project/Art/Sprites/Pet/README.md` 已改为更符合当前项目真实情况的命名规则：
  - `Move` 等方向型序列帧继续保留方向字段
  - `Interact_Read`、`Interact_BesideDoor` 这类非方向型交互帧允许使用“状态 + 变体 + 帧号”命名，不强行补虚假的方向字段

### 2026-05-08
- 新增 `Assets/_Project/Scripts/Modules/Pet/PetPlayerInputController.cs`，作为当前阶段桌宠的玩家输入组件。
- 新增 `Assets/_Project/Scripts/Modules/Pet/PetPlayerFurnitureInteractionController.cs`，作为当前阶段玩家手动触发桌宠家具交互的入口组件。
- 新增 `Assets/_Project/Scripts/Modules/Pet/PetClickReactionController.cs`，用于鼠标左键点击桌宠后的表情 Debug 输出与气泡回复。
- 新增 `Assets/_Project/Scripts/Modules/Pet/PetClickResponseLibrary.cs`，提供当前阶段点击桌宠时使用的 10 句本地回复语料。
- `Apartment_Main.unity` 中的 `Pet_Angel` 已挂载 `PetPlayerInputController`，当前支持 `WASD` 与方向键直接控制移动。
- `Apartment_Main.unity` 中的 `Pet_Angel` 已挂载 `PetPlayerFurnitureInteractionController`，并预填了门边、盆栽、竖琴、书柜、天使床五组玩家手动交互配置。
- `Apartment_Main.unity` 中的 `Pet_Angel` 已挂载 `PetClickReactionController`，当前支持点击桌宠后显示本地气泡回复。
- `PetController` 已新增玩家控制分支：检测到玩家输入组件后，不再走自主移动 / 调试工作请求链路，而是改由玩家驱动 `Idle / Moving`。
- 新增 `PetPlayerInputControllerTests.cs`，覆盖键盘输入向量的基础组合与归一化规则。
- 新增 `PetPlayerFurnitureInteractionControllerTests.cs`，覆盖 self 交互变体名到目录名的映射规则。
- 新增 `PetClickResponseLibraryTests.cs`，覆盖本地点击回复语料的数量与非空校验。
- 文档口径已同步调整：当前阶段暂不接入大模型，桌宠主要由玩家直接控制移动；Gateway / Travel / AI 对话仍保留为后续规划能力。
- 修正家具进入 Play 后“被清空 / 几乎全消失”的运行时根因：
  - `FurnitureLayoutPersistence` 当前默认禁用自动恢复
  - 家具布局存档当前只记录运行时摆放家具，`RestoreLayout` 不再销毁场景预摆家具
  - `ApartmentSceneFurnitureBindings` 当前新增缺失引用自动找回逻辑，优先按 `SceneFurnitureDefinitionHint`、对象名、Sprite 名恢复 `_target`
- 接入新增 `Idle` / `Sleep` 美术资源到桌宠状态机：
  - `Idle` 三视图动画当前按“首帧保持 4 帧、尾帧保持 4 帧、整体循环”构建
  - `Sleep` 动画当前按循环状态接入
  - `PetController` 当前在静止时播放 `Idle_*`，在 `SleepingState` 时播放 `Sleep`
  - `PetMoveAnimationSetupEditor` 当前已同步支持 `Idle` / `Sleep` 资源目录与对应 clip / state 重建

## 已确认决策

### 2026-04-21：记忆文件命名采用 `gemini-lab-*`
原因：
- 与项目名直接对应
- 对后续扩展多个记忆文档最稳定

### 2026-04-21：当前文档统一把“现状”和“规划”分开描述
原因：
- 这个仓库文档密度高
- 如果不分开，后续智能体极易误判哪些资源已经存在

### 2026-04-21：已补 `AGENTS.md`，正式把它作为第一入口
原因：
- 方法论文档把 `AGENTS.md` 作为整个项目的总入口
- 后续所有智能体都应先读 `AGENTS.md` 再进入主记忆和文件指南

### 2026-04-21：新增单独的 skill 设计边界文档
原因：
- 方法论文档明确要求约定 skill 的设计边界与组织方法
- skill 清单文档只回答“有哪些 skill”，不能代替“什么该写成 skill”

### 2026-04-27：主记忆与结构文档必须描述“当前原型状态”，不能继续停留在 2026-04-21 的骨架快照
原因：
- 仓库真实状态已经明显前进
- 如果继续沿用旧口径，会直接误导后续实现与排错

### 2026-04-27：桌面模块在文档中继续保留“Desktop”设计概念名，但必须同时注明当前真实运行时代码目录为 `DesktopOverlay`
原因：
- 现有模块 README 仍以 `Desktop` 命名
- 当前真实 asmdef 与运行时代码目录是 `DesktopOverlay`

## 后续推荐动作
1. 优先补齐 Prefab 与 ScriptableObject 资产，减少运行时兜底。
2. 把导航与桌面 Overlay 从占位实现推进到真实可验证实现。
3. 在 Unity 中补跑 EditMode / PlayMode / 场景验证，并回写人工验证清单。
4. 每次新增真实场景、Prefab、SO、关键脚本或 skill 结构变化后，及时刷新主记忆、文件指南、结构总览与索引。
## 2026-05-12 工作方式升级
- 开始把智能体协作方式收口为“硬协作契约 + 上下文包 + 轻量三级记忆”的组合。
- `AGENTS.md` 已强化：
  - 新需求先复述理解
  - 用户确认后再执行
  - 当前任务只处理当前任务边界
  - 发现别的问题只提醒，不顺带处理
  - 默认采用“探索 → 规划 → 行动”三段式
  - 引入命令风险分类（安全 / 有风险 / 危险）
- 新增：
  - `docs/current-task-card.md` 作为 L1 当前任务卡
  - `docs/workflow-context-packages.md` 作为任务上下文装配入口
- `docs/ai-memory/` 继续作为 L2 常驻项目记忆主体，不整体推翻重做。
- git 历史、PR 历史和长文档继续视作 L3 完整历史，仅按需搜索，不默认整段加载。
- 同日开始落第一批 workflow skill：
  - `git-sync-upstream-main`
  - 目标是把高风险、重复使用的 upstream 同步操作收口成单用途流程，而不是继续依赖临时命令链。
  - `unity-clear-generated-cache`
  - 目标是把“只清缓存、不改源码”的高风险清理操作也收口成单用途流程。
  - `apartment-scene-rollback-to-commit`
  - 目标是把“精准回退公寓场景到指定 commit”的高风险 git 操作收口成单用途流程，防止回退范围失控。
  - `pet-animation-reference-rebuild`
  - 目标是把“只修复 Pet 动画引用链，不顺手改场景和交互逻辑”的高频动画修复操作收口成单用途流程。
  - `furniture-binding-check`
  - 目标是把“只读盘点家具绑定问题、先分类再决定修复”的高频排查操作收口成单用途流程。

### 2026-05-12 第二部分工作方式升级
- 已把“渐进式上下文压缩 / 做梦整理 / L2 技能手册 / L3 知识库”写入项目工作方式设计。
- 当前明确结论：
  - 渐进式上下文压缩：已纳入设计，但当前默认不启用自动化
  - 做梦整理：当前先采用人工整理 + 半自动提示
  - L2 技能手册：通过 workflow skill 持续沉淀
  - L3 知识库：当前先做索引式长期档案，不做复杂系统

### 2026-09-05 WorldMap 云层与单花环境动画修复

- 历史云层使用颜色键材质处理无 Alpha 的 JPG，会把亮白云朵误判为背景；本次改为编辑器阶段生成 `WorldMapClouds_Alpha.png`，只抠除边缘连通背景，不覆盖源文件。
- 运行时不生成云层或花朵视觉对象。云层 Sprite、材质、基线和变换均保存于 `WorldMap_Main.unity`；运行时控制器只移动已有云层并对已有单花 Transform 施加轻微旋转。
- Play 验证已确认两块云朵轮廓完整可见；多个单花 Z 角度随时间变化且相位不同，花丛保持静止。
### 2026-09-05 WorldMap 桌宠动画触发条件落地
- 室外特殊动画不再共用一个全局 active 状态；天使和恶魔分别拥有自己的 action、计时器、移动锁和待播队列，避免一只播放时阻塞另一只。
- Idle/Move 是 `PetController` 的基础状态：无特殊动作时始终根据移动速度保持待机或走路，两个桌宠移动时都使用自己的 WorldMap Animator Controller。
- 范围触发使用“进入范围”锁存，漫游随机动作只在进入一次时掷概率；离开范围后才允许下一次判定。花朵摆放事件以 slot/owner/type/cluster 生成键，初次快照仅建立基线。
- F 键只路由到当前 `IsPlayerControlEnabled` 的桌宠，并按“苹果树优先、再许愿树/恶魔标牌”判断；缺少标牌引用时不创建占位对象，相关触发保持禁用直到 Inspector 绑定。
### 2026-09-05 苹果树交互更新

- 苹果树本轮总量在开始掉落时保留，分配到 1–3 个苹果后逐个领取，避免点击树时直接结算整批。
- 掉落苹果、碰撞体和“收获 +N”提示必须在 Scene/Inspector 作者化；运行时只做显隐、下落、晃动和文本填充。
