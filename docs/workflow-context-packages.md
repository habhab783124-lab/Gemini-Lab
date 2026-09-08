# Gemini-Lab 上下文包

Updated: 2026-08-21

## 这份文档的定位
这份文档定义 Gemini-Lab 当前推荐的“作用域上下文组装”方式。

目标不是让智能体每次都读完整个项目，而是：
- 按任务类型加载最小必要规则
- 减少上下文污染
- 降低从 A 任务漂移到 B 任务的概率

## 三层上下文

### L1 当前任务卡
- 文件：`docs/current-task-card.md`
- 用途：当前这一轮任务的边界、目标、完成标准
- 特点：短、小、频繁更新

### L2 常驻项目记忆
- 目录：`docs/ai-memory/`
- 用途：稳定规则、结构、历史决策、文件导航
- 特点：默认可信，但需要持续维护

### L3 完整历史
- 来源：
  - git 历史
  - PR 历史
  - 长文档
  - 旧阶段记录
- 用途：按需搜索，不默认全量加载

## 推荐上下文包

### 1. Git / PR / Upstream 包
适用任务：
- 同步 upstream
- 整理 main
- 提交 PR
- 解决分支冲突

优先加载：
- `AGENTS.md`
- `docs/current-task-card.md`
- `docs/git-fork-upstream-pr-workflow.md`
- `docs/ai-memory/gemini-lab-memory-rules-and-history.md`

### 2. Apartment 场景包
适用任务：
- 公寓场景摆放
- 场景回退
- 相机、层级、对象引用排查

优先加载：
- `AGENTS.md`
- `docs/current-task-card.md`
- `docs/project-structure-overview.md`
- `docs/ai-memory/gemini-lab-project-file-guide.md`
- `docs/ai-memory/gemini-lab-memory-main.md`

### 3. Pet 动画包
适用任务：
- Move / Idle / Sleep / Interact 动画接线
- AnimatorController 调整
- 动画资源命名

优先加载：
- `AGENTS.md`
- `docs/current-task-card.md`
- `Assets/_Project/Art/Sprites/Pet/README.md`
- `docs/ai-memory/gemini-lab-project-file-guide.md`
- `docs/manual-validation-checklist.md`

### 4. 家具交互包
适用任务：
- FurnitureService
- ApartmentSceneFurnitureBindings
- 家具显式绑定与交互排查

优先加载：
- `AGENTS.md`
- `docs/current-task-card.md`
- `Assets/_Project/Scripts/Modules/Furniture/README.md`
- `docs/furniture-interaction-coverage-map.md`
- `docs/ai-memory/gemini-lab-memory-main.md`

## 使用原则
1. 先选包，再读文件，不反过来。
2. 同一轮任务尽量只装一个主包；只有跨域任务才增加第二个包。
3. 如果任务升级或改变边界，应先更新 `docs/current-task-card.md` 再继续执行。

## 标准使用步骤

### Step 0. 先更新当前任务卡
在进入任何实际执行前，先更新：
- `docs/current-task-card.md`

至少填清楚这 6 项：
- 当前任务
- 本轮要做
- 本轮明确不做
- 完成标准
- 直接相关文件
- 风险与注意事项

如果这 6 项还没写清楚，就不进入“执行”阶段。

### Step 0.5. 通过兼容性机器闸门

当前任务卡在保留原字段的基础上还应声明：

- `workflow_contract_version`
- `task_id`
- `human_approved`
- `approval_source`
- `plan_hash`

确认任务卡后按顺序执行：

1. `tools/check-task-gate.ps1 -Mode write`
2. `tools/check-task-scope.ps1 -Mode CreateBaseline`
3. 再进入实际脚本、资源或场景写入。

这里的 `human_approved` 由用户原本的“执行”确认语义映射而来，不增加新的用户操作；`plan_hash` 只覆盖任务意图、边界、验收标准和允许文件，不覆盖执行状态。

### Step 1. 选择主上下文包
根据当前任务类型，先选择一个主包：
- Git / PR / Upstream 包
- Apartment 场景包
- Pet 动画包
- 家具交互包

默认不要一开始就同时加载多个包。

### Step 2. 再按包读取文件
选完包之后，再读该包里列出的优先文件。

如果任务跨度扩大：
- 先更新 `docs/current-task-card.md`
- 再增加第二个包

### Step 3. 先探索，再规划，再行动
在当前包上下文下：
- 探索：只读检查
- 规划：复述理解、列边界、等确认
- 行动：确认后再改文件、场景、资源或执行 git

### Step 4. 任务切换时重写任务卡
如果任务已经从 A 变成 B，不要继续沿用旧任务卡。

应该：
1. 重写 `docs/current-task-card.md`
2. 重新选包
3. 再进入下一轮探索 / 规划 / 行动

### Step 5. 统一收尾验证

任务完成前运行：

```powershell
tools/verify-task.ps1 -JsonOnly
```

它会组合 review 闸门、任务范围、`git diff --check` 和 PowerShell 语法检查。任一检查失败时，任务不能进入 `done`；原有 Unity 编译、测试和视觉契约检查仍按任务卡范围执行。
