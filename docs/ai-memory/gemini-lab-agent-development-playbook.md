# Gemini-Lab Agent Development Playbook

Updated: 2026-04-21

## 这份文档解决什么问题
它告诉后续进入 Gemini-Lab 的智能体：
- 每次开工前先读什么
- 不同任务该看哪些文件
- 收尾时哪些文档必须回写

## 开工固定流程
1. 先读 [AGENTS.md](../../AGENTS.md)。
2. 再读 [主记忆](./gemini-lab-memory-main.md)。
3. 再读 [文件指南](./gemini-lab-project-file-guide.md)。
4. 按任务类型补读：
   - 架构或系统边界问题：读 [架构记忆](./gemini-lab-memory-architecture.md)
   - 历史决策、已知缺口：读 [规则与历史](./gemini-lab-memory-rules-and-history.md)
   - 产品目标、玩法定义：读 [玩法规范](../gameplay-spec.md)
   - 资源/目录层级：读 [项目结构总览](../project-structure-overview.md)
   - skill 的适用边界：读 [Skill 设计边界](../skill-design-boundary.md)
5. 查看当前工作树状态，避免覆盖用户未提交改动。
6. 在真正改文件前，再次确认这次任务面对的是“规划文档”还是“真实实现”。

## 当前入口约定
当前正式入口顺序固定为：
1. `AGENTS.md`
2. `docs/ai-memory/gemini-lab-memory-main.md`
3. `docs/ai-memory/gemini-lab-project-file-guide.md`
4. 任务相关专题文档

## 按任务类型的阅读顺序

### 文档类任务
1. 主记忆
2. 文件指南
3. 规则与历史
4. 相关源 README

### 架构或脚手架任务
1. 主记忆
2. 架构记忆
3. `Assets/README.md`
4. `Assets/plan.md`
5. 模块 README

### 场景 / Prefab / ScriptableObject 任务
1. 项目结构总览
2. `Assets/_Project/Scenes/README.md`
3. `Assets/_Project/Prefabs/README.md`
4. `Assets/_Project/ScriptableObjects/README.md`
5. 人工验证清单

### 美术替换或资源整理任务
1. [美术替换工作流](../art-replacement-workflow.md)
2. `Assets/_Project/Art/README.md`
3. `Assets/_Project/Prefabs/README.md`
4. `Assets/_Project/ScriptableObjects/README.md`

### 工具链 / 包依赖 / MCP 任务
1. `Packages/manifest.json`
2. `Packages/packages-lock.json`
3. `.cursor/mcp.json`
4. `.cursor/skills/` 与 `.agents/skills/`
5. 规则与历史

## 执行规则
1. 每次新需求先复述理解，再执行。
2. 用户没有明确说“执行”“修改”“创建”“提交”前，不直接改文件。
3. 中文必须正常显示，输出和文档都不要出现乱码。
4. 不回滚不属于自己的改动。
5. 写文档时优先真实、可维护、可接手，不追求表面完整。
6. 能写成“可验证陈述”的地方，不写成模糊愿景句。
7. 如果仓库现状和 README 规划不一致，要显式标出来。

## 收尾验证流程
1. 查看 `git status`，确认这次任务实际改了哪些文件。
2. 检查是否引入以下变化：
   - 文件结构变化
   - 场景结构变化
   - 关键脚本变化
   - 包依赖变化
   - skill 清单或 skill 边界变化
   - 人工验证结果变化
3. 若变化存在，至少同步更新：
   - 主记忆
   - 文件指南
   - 结构总览
   - `memory-index.paths.txt`
4. 若涉及验证结果，回写人工验证清单。
5. 若涉及长期规则、历史决策或已知缺口，更新规则与历史文档。

## 当前推荐开发顺序
1. 搭建 Phase 1 工程骨架
2. 让 README 中的规划资源开始真正落地
3. 修正文档与包配置之间的差异
4. 再推进 Phase 2 以后的大系统

## 当前最容易踩的坑
1. 把 README 里的未来场景当成已经存在的场景来操作。
2. 以为 `_Project/Scripts/` 下已经有代码实现，实际上现在几乎只有 README。
3. 忽略 `manifest.json` 与 README 的依赖差异，直接假设导航方案已经装好。
4. 改了目录或资源路径，却没有同步刷新索引和文件指南。
