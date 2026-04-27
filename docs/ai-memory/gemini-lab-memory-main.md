# Gemini-Lab Memory Main

Updated: 2026-04-21

## 定位
这份文档是 Gemini-Lab 的长期项目记忆总览。

`AGENTS.md` 已经作为总入口落地；本文件现在承担“第二入口 + 主记忆总览”的作用。

## 快速导航
- [AGENTS.md](../../AGENTS.md)
- [架构记忆](./gemini-lab-memory-architecture.md)
- [规则与历史](./gemini-lab-memory-rules-and-history.md)
- [开发手册](./gemini-lab-agent-development-playbook.md)
- [文件指南](./gemini-lab-project-file-guide.md)
- [项目结构总览](../project-structure-overview.md)
- [玩法规范](../gameplay-spec.md)
- [人工验证清单](../manual-validation-checklist.md)
- [美术替换工作流](../art-replacement-workflow.md)
- [Git 开发流程](../git-fork-upstream-pr-workflow.md)
- [项目 Skill 清单](../project-skill-catalog.md)
- [Skill 设计边界](../skill-design-boundary.md)
- [方法论对齐审计](../ai-workspace-bootstrap-alignment.md)
- [记忆索引](./memory-index.paths.txt)

## Workspace Identity
- 项目名称：Gemini-Lab
- 项目类型：Unity 2D 桌宠 / AI 虚拟宠物客户端
- Unity 版本：`2022.3.62f3c1`
- 目标体验：让宠物在公寓场景与桌面 Overlay 两种形态中持续陪伴用户，并通过 OpenClaw Gateway 获得对话、工作、旅行等行为能力
- 当前协作工具：Unity MCP、嵌入式 Unity Skills、`.cursor/skills/` 与 `.agents/skills/`

## 当前状态
- 当前仓库处于“文档与工程骨架先行”阶段。
- `AGENTS.md`、`docs/` 与 `docs/ai-memory/` 已于 2026-04-21 建立。
- `2026-04-27` 已把当前 fork 的 `main` 同步到 `upstream/main`，并新增 Git 标准流程文档。
- `Assets/_Project/` 下已经建立了目录和 README 规范，但目前真实落地的运行时代码、场景、Prefab、ScriptableObject 资产还没有开始提交。
- `Assets/plan.md` 已经把项目拆成 Phase 1 至 Phase 4，并定义了每个阶段的里程碑和 DoD。
- README 系列文档描述的是清晰的产品目标与架构目标，但很多内容目前仍属于“规划中”而不是“已实现”。
- 项目本地 skill 目前已整理出清单文档与设计边界文档。

## 当前最重要事实
1. 这个仓库目前“说明文档密度”明显高于“实际实现密度”，做任何开发时都必须先区分规划与现实。
2. `_Project/` 已被定义为自研业务代码与资源的唯一正式落点。
3. 根 README、`Assets/README.md`、`Assets/plan.md` 与各模块 README 已足以支持前期架构设计和脚手架搭建。
4. 已安装并可见的 AI 开发基础包括：
   - `com.ivanmurzak.unity.mcp`
   - `com.ivanmurzak.unity.mcp.particlesystem`
   - 嵌入式包 `Packages/SkillsForUnity`
   - `.cursor/mcp.json`
   - `.cursor/skills/` 与 `.agents/skills/`
5. 计划中反复出现的 `Boot.unity`、Apartment 场景、Overlay 场景、各模块 C# 文件、Prefab 和 SO 资产，当前都还没有真正出现在仓库里。
6. README 规划使用 `NavMeshPlus` / `com.unity.ai.navigation` 路线，但当前 `Packages/manifest.json` 里还没有对应依赖，需要后续同步。
7. 当前工作区存在未提交修改，至少包括：
   - `Packages/packages-lock.json`
   - `ProjectSettings/PackageManagerSettings.asset`
   - `ProjectSettings/ProjectVersion.txt`
   - 工具目录 `.agents/`、`.vscode/`、`Packages/SkillsForUnity/`
8. 当前版本控制协作基线已经固定为 `fork + upstream + feature branch + PR` 工作流。

## 长期目标
- 做成一个真正可持续演化的 AI 桌宠项目，而不是一次性 Demo。
- 让“玩法、架构、工具、验证、文档”同时成长，不把任何一块长期欠账。
- 保持多智能体可接手：新智能体进入项目后，能在较短时间内读懂上下文并安全推进。

## 长期约束
- 所有中文文档和中文注释必须保持 UTF-8 正常显示。
- 文档里必须显式区分“已存在事实”和“规划目标”。
- `UI` 不承载业务逻辑；跨模块通信只走接口、事件或服务定位。
- ScriptableObject 资产在运行期只读，运行态状态进入 Service 或 Snapshot。
- 优先 Scene / Inspector 友好与美术替换友好，不做只能靠硬编码维持的结构。
- `_Project/` 继续作为自研业务资产唯一落点；第三方资源不要混入其中。

## 阶段进度
| Phase | 目标 | 当前判断 |
| :--- | :--- | :--- |
| Phase 1 | 核心基础设施与 FSM 骨架 | 已有规划，尚未开始提交实现 |
| Phase 2 | V-Decor、2D NavMesh、家具交互 | 已有规划，尚未开始提交实现 |
| Phase 3 | OpenClaw 网关与对话/工作链路 | 已有规划，尚未开始提交实现 |
| Phase 4 | UI、桌面 Overlay、旅行系统 | 已有规划，尚未开始提交实现 |

## 近期建议优先级
1. 按 `Assets/plan.md` 开始落 Phase 1 的代码脚手架：asmdef、Core 基础设施、FSM 核心、Boot 场景。
2. 让 `Packages/manifest.json` 与架构文档一致，确认最终采用的导航方案和包依赖。
3. 开始把 README 中的规划资源逐步落成真实场景、Prefab、SO 和脚本。
4. 随着场景、Prefab、SO、脚本落地，持续更新本记忆体系与人工验证清单。

## 更新触发
出现以下任一变化时，必须同步更新记忆文档：
- 核心玩法规则变化
- 场景结构变化
- UI 层级变化
- 关键脚本或关键包变化
- 文件结构变化
- 已知问题状态变化
- 推荐开发顺序变化
