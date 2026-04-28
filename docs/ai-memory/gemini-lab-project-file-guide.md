# Gemini-Lab Project File Guide

Updated: 2026-04-27

## 入口文件
- `AGENTS.md`
  - 当前项目总入口。任何智能体进入项目后都应先读它。
- `docs/ai-memory/gemini-lab-memory-main.md`
  - 当前主记忆总览与第二入口。
- `README.md`
  - 项目总说明，面向产品、技术栈、整体架构和路线图。
- `Assets/README.md`
  - 更偏业务与 FSM 的设计说明。
- `Assets/plan.md`
  - 阶段里程碑、Sprint 拆解、DoD 与风险表。

## 先看哪里

### 想知道项目现在处于什么状态
- `AGENTS.md`
- `docs/ai-memory/gemini-lab-memory-main.md`
- `docs/ai-memory/gemini-lab-memory-rules-and-history.md`
- `docs/project-structure-overview.md`

### 想知道项目真正要做成什么
- `docs/gameplay-spec.md`
- `README.md`
- `Assets/README.md`

### 想知道代码和模块应当怎么分层
- `docs/ai-memory/gemini-lab-memory-architecture.md`
- `Assets/_Project/Scripts/README.md`
- `Assets/_Project/Scripts/Core/README.md`
- `Assets/_Project/Scripts/Modules/README.md`
- 各模块 README

### 想知道场景、Prefab、SO 怎么组织
- `docs/project-structure-overview.md`
- `Assets/_Project/Scenes/README.md`
- `Assets/_Project/Prefabs/README.md`
- `Assets/_Project/ScriptableObjects/README.md`

### 想从真实运行时入口开始看
- `Assets/_Project/Scenes/Boot.unity`
- `Assets/_Project/Scripts/Core/GameBootstrap.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetController.cs`
- `Assets/_Project/Scripts/Modules/Gateway/GatewayBootstrap.cs`
- `Assets/_Project/Tests/EditMode/GeminiLab.Tests.EditMode.asmdef`

### 想做美术替换
- `docs/art-replacement-workflow.md`
- `Assets/_Project/Art/README.md`
- `Assets/_Project/Prefabs/README.md`
- `Assets/_Project/ScriptableObjects/README.md`

### 想看包依赖和工具链
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `ProjectSettings/ProjectVersion.txt`
- `.cursor/mcp.json`
- `.cursor/skills/`
- `.agents/skills/`
- `docs/project-skill-catalog.md`
- `docs/skill-design-boundary.md`

### 想看版本控制与 PR 流程
- `docs/git-fork-upstream-pr-workflow.md`
- `AGENTS.md`

## 当前真实存在的重要目录

### 文档与协作
- `AGENTS.md`
  - 当前项目总入口与执行规则入口
- `docs/`
  - 项目文档根目录
- `docs/ai-memory/`
  - 记忆文档与索引
- `docs/project-skill-catalog.md`
  - 当前项目本地 skill 清单与分类说明
- `docs/skill-design-boundary.md`
  - 当前项目 skill 的设计边界与组织方式
- `docs/git-fork-upstream-pr-workflow.md`
  - 当前项目标准 fork / upstream / feature branch / PR 工作流

### Unity 项目
- `Assets/_Project/`
  - 自研业务唯一正式根目录
- `Assets/_Project/Scripts/`
  - 已存在真实 C# 实现、asmdef 与少量编辑器工具
- `Assets/_Project/Scenes/`
  - 已存在 `Boot.unity`、`Apartment/Apartment_Main.unity`、`Desktop/Desktop_Overlay.unity`
- `Assets/_Project/Tests/`
  - 已存在 `EditMode` / `PlayMode` 测试程序集
- `Assets/_Project/Art/` 与 `Assets/_Project/Animations/`
  - 已存在宠物、家具、环境示例资源与宠物动画
- `Packages/`
  - 包依赖与嵌入式包
- `ProjectSettings/`
  - Unity 项目级设置

## 当前真实存在但容易误判的情况
1. `Assets/_Project/Scenes/` 已经有真实 `.unity` 场景文件，但它们当前更接近原型场景，不等于所有 README 里的目标场景集合都已完成。
2. `Assets/_Project/Prefabs/` 目前仍没有真实 `.prefab` 资源。
3. `Assets/_Project/Scripts/` 已经有大量真实 C# 业务实现与 asmdef，不再是只有 README 的空目录。
4. `Assets/_Project/ScriptableObjects/` 目前仍没有实际 `.asset` 配置；很多 SO 类型只存在于代码层。
5. `Assets/_Project/Scripts/Modules/Desktop/README.md` 仍承载桌面模块的设计说明，但当前真实运行时代码目录是 `Assets/_Project/Scripts/Modules/DesktopOverlay/`。
6. 多个系统当前依赖运行时兜底或 Mock 配置，看到“能跑起来”不等于“资产作者化已完成”。

## 模块 README 导航
- `Assets/_Project/Scripts/Modules/Pet/README.md`
- `Assets/_Project/Scripts/Modules/Furniture/README.md`
- `Assets/_Project/Scripts/Modules/Navigation/README.md`
- `Assets/_Project/Scripts/Modules/Gateway/README.md`
- `Assets/_Project/Scripts/Modules/Travel/README.md`
- `Assets/_Project/Scripts/Modules/Desktop/README.md`
- `Assets/_Project/Scripts/Modules/Persistence/README.md`

## 什么时候刷新这个文件
出现以下任一情况就要更新：
- 新增或删除关键目录
- 场景、Prefab、SO 的真实落地状态变化
- 工具入口变化
- `AGENTS.md` 入口协议变化
- 目录或文件名改动导致现有导航失效
