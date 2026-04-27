# Gemini-Lab Memory Rules And History

Updated: 2026-04-21

## 长期规则
1. 所有中文文档、中文注释、中文说明都必须保持 UTF-8 正常显示。
2. 新增或修改文档时，要明确写出哪些内容是“已实现”，哪些只是“规划中”。
3. 不要把 README 中的目标状态误写成仓库现状。
4. 业务代码与资源统一放在 `Assets/_Project/`，不要把自研内容散落到第三方目录。
5. 模块之间不直接硬引用具体实现，只通过接口、事件或服务门面通信。
6. UI 不承载业务规则、网络请求和持久化逻辑。
7. 运行态数据不写回 ScriptableObject 资产。
8. 文件结构变更后必须同步更新：
   - `memory-index.paths.txt`
   - 文件指南
   - 项目结构总览
9. 手工验证结果要落到 `docs/manual-validation-checklist.md`，不能只停留在聊天记录里。

## 当前已知问题
1. `_Project/` 目前只有目录、README 和 `.meta`，还没有真正的运行时代码、场景、Prefab 和 SO 资产。
2. 根 README、`Assets/README.md` 和模块 README 都提到了导航方案与场景文件，但当前仓库并未落地对应资源。
3. README 中规划使用 `NavMeshPlus` 路线，但 `Packages/manifest.json` 里还没有对应包。
4. AI 工具目录同时存在 `.cursor/skills/` 与 `.agents/skills/`，说明协作链路已扩展，但后续需要保持两边同步策略明确。

## 最近进展

### 2026-04-21
- 浏览并梳理整个 Gemini-Lab 仓库。
- 校验了项目当前真实状态：
  - Unity 版本为 `2022.3.62f3c1`
  - `docs/` 原本不存在
  - `_Project/` 仍处于脚手架阶段
  - MCP、skills 和嵌入式技能包已接入
- 依据《新项目 AI 工作环境搭建方法论》初始化了 `docs/` 体系：
  - `docs/ai-memory/`
  - 主记忆、架构、规则历史、开发手册、文件指南
  - 玩法规范、结构总览、人工验证清单、美术替换工作流
- 新增 `docs/project-skill-catalog.md`，整理当前项目本地 skill 清单，并确认 `.agents/skills/` 与 `.cursor/skills/` 当前均为 66 项且一一对应。
- 新增根目录 `AGENTS.md`，把入口协议、项目规则和文档更新触发器正式固定下来。
- 依据方法论再做一次逐条合规审计，补齐过期表述、skill 设计边界和方法论对齐说明。

### 2026-04-27
- 将 `upstream` 远程从 HTTPS 调整为 SSH，以适配当前机器的 GitHub 访问方式。
- 抓取 `upstream/main` 并确认原本本地 `main` 与原仓库 `main` 历史不一致。
- 创建本地备份分支 `backup/main-before-upstream-sync-2026-04-27`。
- 将本地 `main` 对齐到 `upstream/main`，并强制推送到 `origin/main`，完成 fork 与原仓库的主线同步。
- 从同步后的 `main` 创建新开发分支 `docs/git-fork-pr-workflow`。
- 新增 `docs/git-fork-upstream-pr-workflow.md`，把当前项目的 fork / upstream / feature branch / PR 流程固化成正式文档。

## 已确认决策

### 2026-04-21：记忆文件命名采用 `gemini-lab-*`
原因：
- 与项目名直接对应
- 对后续扩展多个记忆文档最稳定

### 2026-04-21：当前文档统一把“现状”和“规划”分开描述
原因：
- 这个仓库目前文档多、实现少
- 如果不分开，后续智能体极易误判哪些资源已经存在

### 2026-04-21：本轮先补 `docs/`，暂不擅自新增 `AGENTS.md`
原因：
- 用户本轮需求聚焦在“构建 docs 目录”
- `AGENTS.md` 仍被视为强烈推荐的下一步，但不在本轮主动扩大范围

### 2026-04-21：已补 `AGENTS.md`，正式把它作为第一入口
原因：
- 方法论文档把 `AGENTS.md` 作为整个项目的总入口
- 后续所有智能体都应先读 `AGENTS.md` 再进入主记忆和文件指南

### 2026-04-21：新增单独的 skill 设计边界文档
原因：
- 方法论文档明确要求约定 skill 的设计边界与组织方法
- skill 清单文档只回答“有哪些 skill”，不能代替“什么该写成 skill”

## 后续推荐动作
1. 开始真正落 Phase 1 工程骨架：
   - asmdef
   - Core 基础设施
   - FSM 核心
   - Boot 场景
2. 决定并安装真实导航依赖，修正文档与包配置差异。
3. 每出现一个真正落地的场景、Prefab、SO、脚本目录，都同步刷新：
   - 主记忆
   - 文件指南
   - 项目结构总览
   - 人工验证清单
