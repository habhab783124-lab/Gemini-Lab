# Gemini-Lab 项目结构总览

Updated: 2026-04-21

## 这份文档怎么看
这不是“理想中的最终目录图”，而是“当前仓库已经有什么，以及这些目录将来分别负责什么”的说明。

目前 Gemini-Lab 还处在骨架阶段，所以这份文档会同时标明：
- 现在已经存在的结构
- 未来准备往哪里落实现

## 当前顶层结构

### `docs/`
本轮建立的项目文档根目录，用来承载 AI 记忆、玩法规范、验证清单和工作流文档。

当前已包含一份项目本地 skill 总表：
- `docs/project-skill-catalog.md`
- `docs/skill-design-boundary.md`

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
- 有 Unity MCP 相关包
- 有嵌入式 `SkillsForUnity`

### `ProjectSettings/`
Unity 项目级设置目录。

### `.cursor/` 与 `.agents/`
AI 协作工具链目录。

当前已确认：
- `.cursor/mcp.json` 指向本地 MCP 服务
- `.cursor/skills/` 与 `.agents/skills/` 都存在
- 两套项目本地 skill 当前可参考 `docs/project-skill-catalog.md`

## `_Project/` 当前结构

### `Assets/_Project/Scripts/`
代码分层蓝图已经定义为：
- `Core`
- `Modules`
- `UI`
- `Editor`

当前状态：
- 目录与 README 已存在
- 真实 C# 实现还未开始落地

### `Assets/_Project/Scenes/`
场景规划已经写明，但当前没有 `.unity` 文件。

规划中的核心场景包括：
- `Boot.unity`
- `Apartment_Main.unity`
- `Overlay_Main.unity`
- 若干 Dev 沙盒场景

### `Assets/_Project/Prefabs/`
Prefab 结构规划已写明，但当前没有真正的 Prefab 资产。

### `Assets/_Project/ScriptableObjects/`
SO 分类规划已写明，但当前没有实际 `.asset` 文件。

### `Assets/_Project/Art/`
美术资源目录规范已定义，后续会承接：
- 宠物 Sprite
- 家具 Sprite
- 环境 Tile 资源
- UI 图标
- Atlas、动画、Shader

### `Assets/_Project/Audio/`
音频目录规划已定义，后续会承接 BGM、SFX 与语音缓存策略。

### `Assets/_Project/Settings/`
运行期设置资产落点，和业务 SO 分开管理。

## 当前项目的真实实现密度

### 已经具备
- 产品愿景与玩法说明
- 模块职责划分
- 目录骨架
- 里程碑计划
- AI 工具链入口
- 文档协作体系

### 还没有真正具备
- 运行时代码
- 场景资源
- Prefab 资源
- SO 配置资产
- 模块 asmdef
- 单元测试目录

## 工作时的结构判断原则
1. 看到 README 不等于看到实现。
2. 看到目录不等于看到资源。
3. 只有仓库里真实存在的 `.cs`、`.unity`、`.prefab`、`.asset` 等文件，才算已落地内容。
4. 结构变化以后，要同时更新文档和索引，不能只改文件夹。

## 推荐阅读顺序
1. `README.md`
2. `Assets/README.md`
3. `Assets/plan.md`
4. `docs/ai-memory/gemini-lab-memory-main.md`
5. 各子目录 README
