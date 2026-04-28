# Gemini-Lab Skill 设计边界

Updated: 2026-04-27

## 这份文档的定位
这份文档专门回答两个问题：
- 什么内容适合在 Gemini-Lab 里写成 skill
- 什么内容不应该写成 skill，而应该写进 `AGENTS.md`、`docs/ai-memory/` 或其他项目文档

它和 [项目 Skill 清单](./project-skill-catalog.md) 的分工不同：
- `project-skill-catalog.md` 回答“现在有哪些 skill”
- 本文档回答“以后什么该做成 skill”

## 当前项目的 skill 组织方式
- 工作区主入口：`.agents/skills/`
- Cursor 镜像目录：`.cursor/skills/`
- 当前两边目录仍为镜像关系，应持续保持同步
- 每个 skill 至少应包含自己的 `SKILL.md`

## 什么适合做 skill
以下内容适合做 skill：

### 1. 跨任务、跨阶段会重复出现的固定流程
例如：
- 场景打开、修改、保存的标准流程
- Prefab 打开、修改、保存、关闭的标准流程
- 日志清空后复现并读取错误的调试流程

### 2. 明显需要工具链协同的标准化操作
例如：
- 用 Unity MCP 查找资产并修改字段
- 读取验证清单并继续追问题
- 包搜索、安装、验证的固定步骤

### 3. 带明确前置条件和收尾条件的可复用工作流
例如：
- 文档修复流程
- 批量 Prefab 重建流程
- 测试前检查场景是否已保存的流程

### 4. 需要引用成套参考资料的技能说明
例如：
- Unity 场景作者化工具使用流程
- 特定 MCP 工具的输入格式与推荐调用顺序

## 什么不适合做 skill
以下内容不应写成 skill：

### 1. 当前项目的即时事实
例如：
- “当前 `Assets/_Project/Scenes/` 已经有哪些真实场景文件”
- “当前 `Assets/_Project/Prefabs/` 里有没有真实 Prefab”
- “当前 `Packages/manifest.json` 里已经接了哪些导航包或 Unity MCP 包”
- “当前本地 skill 数量与镜像状态”

这类内容应进入：
- `docs/ai-memory/`
- `docs/project-structure-overview.md`
- `docs/project-skill-catalog.md`

### 2. 频繁变化的具体玩法内容
例如：
- 当前宠物状态值细节
- 当前旅行奖励规则
- 当前家具 Buff 方案

这类内容应进入：
- `docs/gameplay-spec.md`
- 架构文档
- 模块 README

### 3. 整个项目的长期执行规则
例如：
- 新需求先复述理解
- 用户没说“执行”前不改文件
- 中文不能乱码
- 改动后文档同步更新

这类内容应进入：
- `AGENTS.md`

### 4. 已知问题、历史决策和最近进展
这类内容应进入：
- `docs/ai-memory/gemini-lab-memory-rules-and-history.md`

## 一个实用判断标准
如果某段内容在回答：
- “这个项目现在是什么状态”

那它应该进记忆文档或项目文档，不该进 skill。

如果它在回答：
- “以后反复遇到这类任务时，应该怎么做”

那它更适合做 skill。

## Gemini-Lab 当前的 skill 使用边界

### 应该写成 skill 的方向
- Unity MCP 工具组合使用流程
- 资产读取与修改流程
- 场景 / Prefab 作者化标准流程
- 动画 / Animator 资产操作流程
- 测试与日志排查流程
- 文档修复、索引刷新、验证清单回写流程

### 不应该写成 skill 的方向
- Gemini-Lab 当前阶段进度
- 当前项目结构现状
- 当前哪些场景、Prefab、SO 已存在或尚未存在
- 当前玩法目标与状态机设计细节

## 新增或修改 skill 时的同步要求
新增、删除、重命名或重组项目本地 skill 后，至少同步更新：
- `docs/project-skill-catalog.md`
- `docs/ai-memory/gemini-lab-project-file-guide.md`
- `docs/ai-memory/gemini-lab-memory-main.md`
- `docs/ai-memory/gemini-lab-memory-rules-and-history.md`
- `docs/ai-memory/memory-index.paths.txt`

## 当前维护原则
1. `.agents/skills/` 视为工作区主入口。
2. `.cursor/skills/` 作为镜像目录，应保持一致。
3. skill 写流程，不写项目现状。
4. skill 可以复用结构，但内容必须贴 Gemini-Lab 当前的真实玩法和工具链。
