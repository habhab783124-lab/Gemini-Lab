# Gemini-Lab Memory Rules And History

Updated: 2026-04-29

## 长期规则
1. 所有中文文档、中文注释、中文说明都必须保持 UTF-8 正常显示。
2. 新增或修改文档时，要明确写出哪些内容是“已实现”，哪些只是“规划中”。
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

## 当前已知问题
1. `Assets/_Project/Prefabs/` 仍没有真实 `.prefab` 资产，很多场景对象与运行时结构尚未完成 Prefab 化。
2. `Assets/_Project/ScriptableObjects/` 仍没有真实 `.asset` 配置资产；`GatewayConfigSO`、`FurnitureDefinitionSO`、`PetStateValueSO` 等类型已存在，但当前仍依赖场景引用或运行时兜底。
3. `Packages/manifest.json` 已经包含 `com.unity.ai.navigation`，但当前 `NavigationService` / `NavMesh2DRebaker` 仍是占位实现，不等于真实 2D NavMesh 链路已完成。
4. `DesktopOverlay` 模块已有运行时代码，但 `WindowModeAdapter` 还没有落真实原生透明窗口 / 点击穿透实现。
5. 桌面相关文档目前同时存在“设计名 `Desktop`”和“运行时代码目录 `DesktopOverlay`”两套表述，阅读时要显式区分。
6. 文档体系在 2026-04-21 建立时曾按“骨架阶段”描述仓库，后续若实现继续推进，必须持续刷新文档，避免再出现口径滞后。

## 最近进展

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
