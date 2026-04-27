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
- 每次收到新需求，先复述理解，再执行。
- 用户没有明确说“执行”“修改”“创建”“提交”前，不直接改文件；先给理解、检查结果或建议。
- 文档中文必须正常显示，脚本注释中文也必须正常显示。
- 所有输出都要明确区分：
  - 仓库中已经存在的事实
  - 文档里规划中的目标状态
- 优先显式引用、序列化引用和清晰接口，不依赖对象名猜测。
- 优先 Scene 友好、Inspector 友好、美术替换友好的结构。
- 不回滚、不覆盖、不顺手清理不属于自己的改动，除非用户明确要求。
- 当文件结构、场景结构、工具入口或长期规则变化时，同任务内同步更新相关文档。

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
