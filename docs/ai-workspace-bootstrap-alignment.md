# Gemini-Lab 与方法论文档对齐审计

Updated: 2026-04-21

## 审计基准
本审计以外部方法论文档 [ai-workspace-bootstrap-methodology.md](d:/unity/做游戏/塔防开发/docs/ai-workspace-bootstrap-methodology.md) 为唯一基准。

目标不是“看起来差不多”，而是逐条检查当前仓库是否已经具备方法论要求的 AI 协作环境骨架。

## 审计结论
当前 Gemini-Lab 仓库已经完成方法论要求的文档骨架搭建，并补齐了以下关键项：
- `AGENTS.md`
- `docs/ai-memory/` 记忆体系
- `gameplay-spec.md`
- `manual-validation-checklist.md`
- `project-structure-overview.md`
- `art-replacement-workflow.md`
- skill 清单与 skill 设计边界文档

当前仍然存在的差距不在“文档骨架”，而在“项目实现阶段”：
- 运行时代码、场景、Prefab、SO 资产仍未大规模落地
- 这属于实现进度问题，不属于方法论文档体系缺失问题

## 逐条对齐结果

### 1. 基础目录结构
| 方法论要求 | 当前状态 |
| :--- | :--- |
| `AGENTS.md` | 已完成 |
| `docs/` | 已完成 |
| `docs/ai-memory/` | 已完成 |
| `memory-index.paths.txt` | 已完成 |
| 主记忆文档 | 已完成 |
| 架构文档 | 已完成 |
| 规则与历史文档 | 已完成 |
| 开发手册 / playbook | 已完成 |
| 文件指南 | 已完成 |
| 玩法规范 | 已完成 |
| 人工验证清单 | 已完成 |
| 结构总览 | 已完成 |
| 美术替换工作流 | 已完成 |

### 2. `AGENTS.md` 结构要求
| 方法论要求 | 当前状态 |
| :--- | :--- |
| `Workspace Identity` | 已完成 |
| `Startup Protocol` | 已完成 |
| `Operating Rules` | 已完成 |
| `Project-Specific Rules` | 已完成 |
| `Memory Update Triggers` | 已完成 |

### 3. `AGENTS.md` 关键规则要求
| 方法论要求 | 当前状态 |
| :--- | :--- |
| 中文不能乱码 | 已写入 |
| 新需求先复述理解 | 已写入 |
| 用户没说“执行”前不改文件 | 已写入 |
| 优先 Scene / Inspector 友好 | 已写入 |
| 美术替换友好 | 已写入 |
| 新脚本必须按目录分类放置 | 已写入 |
| 改动后文档同步更新 | 已写入 |
| 优先显式引用，不依赖对象名 | 已写入 |
| 场景对象尽量可在 Scene 里直接改 | 已写入 |

### 4. 记忆文档分工要求
| 方法论文档职责分工 | 当前状态 |
| :--- | :--- |
| 主记忆：导航 / 当前状态 / 长期约束 / 阶段进度 / 关键事实 | 已覆盖 |
| 架构文档：模块分工 / 边界 / 架构原则 / 长期边界 | 已覆盖 |
| 规则与历史：已知问题 / 最近进展 / 决策 / 长期规则 | 已覆盖 |
| Playbook：开工流程 / 收尾验证 / 当前优先级 / 长期约束 | 已覆盖 |
| 文件指南：入口文件 / 当前脚本目录 / 改什么先看哪里 | 已覆盖 |

### 5. skill 设计边界要求
| 方法论要求 | 当前状态 |
| :--- | :--- |
| 明确什么适合做 skill | 已完成 |
| 明确什么不适合做 skill | 已完成 |
| skill 写流程，不写项目现状 | 已完成 |
| 说明项目本地 skill 组织方式 | 已完成 |

## 项目化适配说明
方法论文档中的通用 Unity 模板目录是：
- `Assets/Scripts/`
- `Assets/Scenes/`
- `Assets/Prefabs/`

Gemini-Lab 当前采用的项目化适配是：
- `Assets/_Project/Scripts/`
- `Assets/_Project/Scenes/`
- `Assets/_Project/Prefabs/`

这属于“按项目内容定制目录命名”的允许范围，不视为不合规。

## 本轮修正的主要问题
本轮审计前存在这些不对齐点：
1. 多份文档仍写着“仓库还没有 `AGENTS.md`”。
2. `AGENTS.md` 没把方法论要求的几条硬规则写全。
3. skill 只有清单，没有单独的设计边界文档。
4. 索引中还没有把 `AGENTS.md` 和新增的对齐文档纳入。

本轮已全部修正。

## 当前审计判断
如果按“方法论要求的文档骨架与规则体系”来判断，当前仓库已对齐。

如果按“完整项目实现程度”来判断，当前仓库仍处于骨架阶段，但这不属于本次方法论文档对齐范围。
