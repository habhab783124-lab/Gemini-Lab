# AGENTS.md

## Workspace Identity
- 项目名称：`Gemini-Lab`
- 项目类型：Unity 2D 桌宠 / AI 虚拟宠物客户端
- Unity 版本：`2022.3.62f3c1`
- 核心外部依赖：`OpenClaw Gateway`
- 当前协作基础设施：
  - `docs/ai-memory/`
  - `.cursor/mcp.json`
  - `.cursor/skills/`
  - `.agents/skills/`

当前仓库处于“文档与工程骨架先行”阶段。

重要现实约束：
- 很多 README 描述的是目标状态，不是已实现状态。
- 目前运行时代码、场景、Prefab、ScriptableObject 资产仍大多未落地。
- 不能把 README 中规划的路径、资源和系统直接当成仓库事实。

本项目对方法论模板的项目化适配是：
- 方法论中的通用目录模板 `Assets/Scripts/`、`Assets/Scenes/`、`Assets/Prefabs/`
- 在本项目中收口为 `Assets/_Project/` 下的 `Scripts/`、`Scenes/`、`Prefabs/`、`ScriptableObjects/`、`Art/`、`Audio/`、`Settings/`

## Startup Protocol
1. 先读本文件。
2. 再读 `docs/ai-memory/gemini-lab-memory-main.md`。
3. 再读 `docs/ai-memory/gemini-lab-project-file-guide.md`。
4. 按任务类型决定是否补读：
   - 架构或模块边界：`docs/ai-memory/gemini-lab-memory-architecture.md`
   - 已知问题、历史决策、最近变更：`docs/ai-memory/gemini-lab-memory-rules-and-history.md`
   - 产品目标与玩法：`docs/gameplay-spec.md`
   - 目录、场景、Prefab、工作流：`docs/project-structure-overview.md`
   - 美术替换：`docs/art-replacement-workflow.md`
   - Git / fork / upstream / PR 流程：`docs/git-fork-upstream-pr-workflow.md`
   - skill 边界与组织方式：`docs/skill-design-boundary.md`
   - 项目本地 skill 清单：`docs/project-skill-catalog.md`
5. 查看当前 git 工作树，避免覆盖用户已有改动。
6. 在执行任何依赖路径、场景、Prefab、脚本的任务前，先确认对应文件真实存在。

## Operating Rules
- 每次收到新需求，先复述理解，再执行；未完成复述确认前，不进入文件修改、命令执行或提交阶段。
- 用户没有明确说“执行”“修改”“创建”“提交”“push”前，不直接改文件；先给理解、检查结果或建议。
- 当前任务只处理当前任务边界内的内容，不自动扩展到旧任务、相邻任务或顺手发现的问题。
- 如果在执行当前任务时发现别的问题，只单独提醒，不顺带处理，除非用户重新确认并明确要求执行。
- 每轮任务按“探索 → 规划 → 行动”三段式推进：
  - 探索：只读检查、收集事实、确认文件真实存在，不改文件。
  - 规划：复述理解、说明边界、列出拟执行动作，等待用户确认。
  - 行动：仅在用户确认后执行修改、资源调整、git 操作或提交。
- 小任务允许极简规划，但不能跳过“先复述理解、等确认再执行”。
- 进入任何写操作前，必须先完成任务卡更新，并通过任务闸门检查。
- 文档中文必须正常显示，脚本注释中文也必须正常显示。
- 以后所有开发都必须保证 `Play` 视图与 `Scene` 视图效果完全一致。
- 不允许因为运行时脚本导致你在 `Scene` 视图中的视觉修改进入 `Play` 后失效，或 `Play` 里的视觉结果和 `Scene` 不一致。
- 视觉效果、布局、相机取景、UI 展示、装饰层级这类结果，原则上都必须能由你在 `Scene` / `Prefab` / `Inspector` 中直接调整。
- 对视觉有影响的运行时脚本，只能做不改变最终视觉结果的辅助逻辑；不能把最终视觉作者化依赖到 `Awake`、`Start`、运行时动态生成或运行时强制回写上。
- 所有输出都要明确区分：
  - 仓库中已经存在的事实
  - 文档里规划中的目标状态
- 优先显式引用、序列化引用和清晰接口，不依赖对象名猜测。
- 优先 Scene 友好、Inspector 友好、美术替换友好的结构。
- 不回滚、不覆盖、不顺手清理不属于自己的改动，除非用户明确要求。
- 当文件结构、场景结构、工具入口或长期规则变化时，同任务内同步更新相关文档。

## 最小闭环任务闸门
- 当前项目启用最小闭环版执行闸门：
  - 人类可读任务卡：`docs/current-task-card.md`
  - 机器可检查任务卡：`docs/current-task-card.json`
  - 闸门脚本：`tools/check-task-gate.ps1`
- 默认规则：
  1. 先更新 `current-task-card.md`
  2. 再更新 `current-task-card.json`
  3. 再运行 `tools/check-task-gate.ps1`
  4. 只有校验通过，才进入写操作
- 如果任务边界变化，必须先重写任务卡，再继续执行。
- 新任务卡还必须声明 `workflow_contract_version`、`task_id`、`human_approved`、`approval_source` 和 `plan_hash`；计划 hash 不覆盖执行状态、批准时间和验证记录。
- 任务闸门会校验批准状态和计划 hash；通过后先运行 `tools/check-task-scope.ps1 -Mode CreateBaseline`，再进入实际写入。
- 收尾统一运行 `tools/verify-task.ps1 -JsonOnly`；它会组合 review 闸门、任务范围、`git diff --check` 和 PowerShell 语法检查。任一失败都不能将任务标记为 `done`。
- 以上是原有流程的机器检查层，不增加新的用户 token 或命令，也不改变“探索 → 规划 → 用户确认 → 行动”的顺序。

### 视觉任务硬闸门
- 每张任务卡都必须声明 `scene_play_parity_required`、`scene_visual_contracts` 和 `runtime_visual_files`；非视觉任务分别使用 `false`、`[]`、`[]`，不能省略字段。
- 闸门会根据 `direct_files` 自动识别场景、艺术资源、运行时模块、UI、SceneBootstrap 和编辑器工具等视觉相关任务。识别为视觉任务时，`scene_play_parity_required` 必须为 `true`。
- `scene_play_parity_required=true` 时，必须为关键 Scene 节点提供 `scene_visual_contracts`，并由 `tools/check-scene-visual-contract.ps1` 验证节点真实存在及关键 Sprite 已保存为非空序列化引用。
- 视觉任务涉及运行时代码时，必须列出 `runtime_visual_files`，并由 `tools/check-runtime-visual-contract.ps1` 扫描。运行时不得直接写入最终 `Sprite` / `runtimeAnimatorController`，不得通过 `new GameObject`、`AddComponent` 或运行时 `Instantiate` 生成最终 UI 视觉。
- 运行时可以读取数据、切换 Scene 中已经作者化的对象或状态、填充文本和控制显示/隐藏；最终资源引用、布局、尺寸、层级和视觉节点必须已经存在于 Scene / Prefab / Inspector 中。
- 两个子检查器必须和主任务闸门一起通过；只在 Play 视图正确、但 Scene 视图没有对应资源引用的实现，视为未完成。

## Project-Specific Rules

### Source Of Truth
- 项目总览与产品目标：`README.md`
- 长期记忆、规则、文件导航：`docs/ai-memory/`
- 自研业务代码与正式生产资源：`Assets/_Project/`

### 代码与资源放置规则
- Runtime 代码放在：
  - `Assets/_Project/Scripts/Core/`
  - `Assets/_Project/Scripts/Modules/`
  - `Assets/_Project/Scripts/UI/`
  - `Assets/_Project/Scripts/Editor/`
- 新脚本必须放进合理分类目录，不要散落在 `_Project/` 外部。
- 新的场景、Prefab、ScriptableObject、Art、Audio、Settings 资源都应遵守 `Assets/_Project/**/README.md` 里的目录约定。
- `_Project/` 外的第三方包、插件、工具资源不要和自研内容混放。

### 架构与实现规则
- 模块之间只通过接口、事件、服务门面或抽象程序集通信。
- `UI` 不承载网络、持久化、FSM 业务逻辑。
- 运行态状态不写回 ScriptableObject 资产。
- 场景对象尽量保持可在 Scene / Inspector 中直接调整，而不是只能靠硬编码改动。
- 任何会影响视觉结果的内容，优先作者化到 Scene / Prefab / Inspector；不要把视觉最终结果藏在运行时脚本里。
- 优先显式依赖和可维护的序列化绑定，不依赖对象名、层级名和隐式查找。

### 文档与协作规则
- 不能把旧项目事实原样搬进本项目；只能复用结构，不能复用事实。
- 写文档时必须写 Gemini-Lab 的真实情况，而不是抽象模板占位文本。
- 如果 README 描述了未来文件但仓库里还不存在，要在文档中明确标注。
- 改动完成后，相关文档要同步更新，不能留到“以后再补”。

### Git 与分支规则
- 涉及 fork / upstream / main / feature 分支 / PR 的操作，遵循 `docs/git-fork-upstream-pr-workflow.md`。
- `main` 默认只做同步原仓库，不作为日常功能开发分支。
- 新开发任务默认从同步后的 `main` 创建新分支开始。

### 命令风险分类
- 安全：
  - 读文件、查状态、搜索、日志读取、只读 git 查询。
  - 默认可直接执行，但仍应先说明当前在做什么。
- 有风险：
  - 改文档、改脚本、改 `.meta`、切分支、普通提交、普通 push、场景内非破坏性修改。
  - 必须完成“复述理解 + 用户确认执行”后再做。
- 危险：
  - `reset --hard`、大规模删除、场景整体覆盖、批量改资源、清理 `Library`、强推分支、改远程地址、批量回退。
  - 必须单独说明影响范围，得到明确确认后才可执行。
- 有风险和危险操作，当前都应先通过任务闸门脚本。

### 上下文装配
- 根据任务类型，优先装配最小必要上下文，不默认把所有记忆文档全部当作当前任务上下文。
- 推荐优先查阅：
  - Git / PR / upstream：`docs/git-fork-upstream-pr-workflow.md`
  - Apartment 场景：`docs/project-structure-overview.md`、`docs/ai-memory/gemini-lab-project-file-guide.md`
  - Pet 动画：`Assets/_Project/Art/Sprites/Pet/README.md`、`Assets/_Project/Animations/Pet/`
  - 家具交互：`Assets/_Project/Scripts/Modules/Furniture/README.md`、`docs/furniture-interaction-coverage-map.md`
- 具体装配规则见 `docs/workflow-context-packages.md`。

### Skill 规则
- skill 只承载可复用流程、工具配合流程和标准化操作步骤。
- 当前项目事实、当前状态、最近进展、长期规则，不应写进 skill，应写进 `docs/ai-memory/` 或其他项目文档。
- 项目本地 skill 目前以 `.agents/skills/` 为工作区主入口，`.cursor/skills/` 为镜像目录；两边变更要保持同步。

## Memory Update Triggers
当以下内容变化后，必须更新记忆文档：
- 核心玩法规则
- 场景结构
- UI 层级
- 关键脚本
- 关键包依赖
- 文件结构
- 已知问题
- 验证状态
- 推荐开发顺序
- MCP / skill / 工具入口路径

最少同步更新：
- `docs/ai-memory/gemini-lab-memory-main.md`
- `docs/ai-memory/gemini-lab-project-file-guide.md`
- `docs/project-structure-overview.md`
- `docs/ai-memory/memory-index.paths.txt`

在以下情况还要额外更新：
- Git 工作流、远程约定、分支规则变化：
  - `docs/git-fork-upstream-pr-workflow.md`
- skill 清单或 skill 组织方式变化：
  - `docs/project-skill-catalog.md`
  - `docs/skill-design-boundary.md`
- 人工验证范围或结果变化：
  - `docs/manual-validation-checklist.md`
