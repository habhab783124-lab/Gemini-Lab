# Gemini-Lab 项目结构总览

Updated: 2026-04-28

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

### `Assets/_Project/Scenes/`
当前已存在真实场景文件：
- `Boot.unity`
- `Apartment/Apartment_Main.unity`
- `Desktop/Desktop_Overlay.unity`

当前判断：
- 这 3 个场景已经足以说明仓库不再是“没有场景”的状态
- 但它们仍然更接近原型场景，而不是完整量产场景集
- `Apartment_Main.unity` 当前已经承载宠物、现成家具与状态/库存/概览面板，是任务 1 的主验证场景

### `Assets/_Project/Tests/`
当前已存在：
- `EditMode/`
- `PlayMode/`
- 对应测试 asmdef 与多组测试脚本

这意味着测试目录和测试程序集已经真实落地。

### `Assets/_Project/Prefabs/`
Prefab 结构规划已写明，但当前仍没有真正的 `.prefab` 资产。

### `Assets/_Project/ScriptableObjects/`
SO 分类规划已写明，但当前仍没有实际 `.asset` 文件。

需要区分：
- SO 类型代码已经存在于 `Scripts/Modules/**`
- 但对应配置资产还没有真正作者化落地

### `Assets/_Project/Art/` 与 `Assets/_Project/Animations/`
这里已经开始承接真实资源，而不只是目录规范。

当前可见内容包括：
- 宠物移动帧
- 家具与环境示例 Sprite
- 宠物动画片段与 Animator Controller

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
- 真实 Prefab 资产
- 真实 ScriptableObject 配置资产
- 完整的 2D NavMesh 实现
- 原生桌面透明窗口 / 点击穿透实现
- 完整收口后的正式场景 / 资源作者化体系

## 工作时的结构判断原则
1. 看到 README 不等于看到实现。
2. 看到目录不等于看到资源。
3. 只有仓库里真实存在的 `.cs`、`.unity`、`.prefab`、`.asset` 等文件，才算已落地内容。
4. 看到“可运行原型”也不等于看到“最终正式结构”；仍要判断哪些地方是占位实现、哪些地方是资产化落地。
5. 结构变化以后，要同时更新文档和索引，不能只改文件夹。

## 推荐阅读顺序
1. `AGENTS.md`
2. `docs/ai-memory/gemini-lab-memory-main.md`
3. `docs/ai-memory/gemini-lab-project-file-guide.md`
4. `README.md`
5. `Assets/README.md`
6. `Assets/plan.md`
7. 再进入 `Assets/_Project/` 的实际脚本、场景与模块 README
