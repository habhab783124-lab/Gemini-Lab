# Gemini-Lab Agent Development Playbook

Updated: 2026-04-27

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
6. 在真正改文件前，再次确认这次任务面对的是：
   - 目标态规划
   - 当前真实实现
   - 还是两者之间的差距

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
4. 项目结构总览
5. 相关源 README

### 架构或原型实现任务
1. 主记忆
2. 架构记忆
3. `Assets/README.md`
4. `Assets/plan.md`
5. 模块 README
6. 对应真实脚本入口

### 场景 / Prefab / ScriptableObject 任务
1. 项目结构总览
2. `Assets/_Project/Scenes/README.md`
3. `Assets/_Project/Prefabs/README.md`
4. `Assets/_Project/ScriptableObjects/README.md`
5. 人工验证清单
6. 对应真实场景或脚本

### 美术替换或资源整理任务
1. [美术替换工作流](../art-replacement-workflow.md)
2. `Assets/_Project/Art/README.md`
3. `Assets/_Project/Prefabs/README.md`
4. `Assets/_Project/ScriptableObjects/README.md`
5. 受影响的 Animator / 场景 / 脚本

### 工具链 / 包依赖 / MCP 任务
1. `Packages/manifest.json`
2. `Packages/packages-lock.json`
3. `.cursor/mcp.json`
4. `.cursor/skills/` 与 `.agents/skills/`
5. 规则与历史
6. 项目 Skill 清单

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
1. 先补齐 Prefab 与 ScriptableObject 资产，把当前运行时兜底结构收回到可维护的作者化资产体系。
2. 再把导航与桌面 Overlay 的占位实现推进到真实可验证实现。
3. 补跑 Unity 测试与场景验证，并把结果回写人工验证清单。
4. 最后继续扩展 Gateway、Travel、UI 等面向完整体验的功能闭环。

## 当前最容易踩的坑
1. 把 README 里的未来结构当成已经完整实现的结构来操作。
2. 只看 README，不看真实脚本与场景，错过仓库已经落地的原型实现。
3. 看到 SO 类型代码就误以为对应 `.asset` 配置已经存在。
4. 看到桌面模块文档就假设运行时代码仍在 `Desktop/`，忽略实际目录是 `DesktopOverlay/`。
5. 改了目录、资源路径或 skill 清单，却没有同步刷新索引和文件指南。
