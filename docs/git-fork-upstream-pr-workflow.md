# Gemini-Lab Fork / Upstream / PR 版本控制流程

Updated: 2026-04-27

## 这份文档的定位
这份文档是 Gemini-Lab 当前项目的标准 Git 开发流程说明。

它不是可选建议，而是当前项目中涉及版本控制、分支开发、同步原仓库、推送 fork、提交 PR 时应遵守的默认流程。

## 适用范围
适用于以下任务：
- 从原仓库同步最新代码到自己的 fork
- 从同步后的 `main` 创建新功能分支
- 开发过程中持续同步 `upstream`
- 推送到自己的 GitHub 仓库
- 发起 Pull Request
- 在 PR 期间继续跟进原仓库更新

## 当前项目远程约定

### `origin`
指向你自己的 fork。

当前项目使用 SSH：
```bash
git@github.com:habhab783124-lab/Gemini-Lab.git
```

### `upstream`
指向原仓库。

当前项目建议同样使用 SSH：
```bash
git@github.com:ULookup/Gemini-Lab.git
```

说明：
- 在这台机器上，GitHub 的 HTTPS 连接不稳定，SSH 已验证可用。
- 后续同步 `upstream` 时优先沿用 SSH。

## 智能体强制规则
1. `main` 只用于同步原仓库，不在 `main` 上做日常功能开发。
2. 每次开始开发前，先同步 `main`，再从 `main` 创建新分支。
3. 日常开发只在功能分支、修复分支或文档分支上进行。
4. 涉及 PR 的分支，开发过程中要持续同步 `upstream/main`，避免最后集中冲突。
5. 允许对 `origin/main` 执行 `--force`，但前提是它的目的只是“让 fork 的 `main` 精确追平 `upstream/main`”。
6. 对功能分支使用 `--force` 时，只用于 rebase 后更新自己的 fork 分支，不用于覆盖别人的协作分支。
7. 不直接向 `upstream` 推送代码；提交改动通过 PR 回到原仓库。

## 一次性初始化

### 1. clone 你的 fork
```bash
git clone git@github.com:你的用户名/项目名.git
cd 项目名
```

这一步在做什么：
- 拉下你自己控制的仓库副本
- 后续所有开发先落在你的 fork 上

### 2. 添加 upstream
```bash
git remote add upstream git@github.com:原作者/项目名.git
```

验证：
```bash
git remote -v
```

你应该看到：
- `origin` -> 你的 repo
- `upstream` -> 原 repo

这一步在做什么：
- 给当前本地仓库增加一个“原仓库入口”
- 以后所有“同步官方最新代码”的动作都从 `upstream` 来

## 日常开发标准流程

### Step 1. 同步原仓库到本地 main
```bash
git checkout main
git fetch upstream
git rebase upstream/main
git push origin main --force
```

这一步在做什么：
- `fetch upstream`：拉取原仓库最新代码
- `rebase upstream/main`：让本地 `main` 基于原仓库最新 `main`
- `push origin main --force`：把你 fork 上的 `main` 也同步到同样状态

结果：
- 你的本地 `main` 和 `origin/main` 都尽量贴近 `upstream/main`

### Step 2. 创建开发分支
```bash
git checkout -b feature-xxx
```

例如：
```bash
git checkout -b feature-login
```

这一步在做什么：
- 从最新的、干净的 `main` 拉一个专门的工作分支
- 后续开发不污染 `main`

### Step 3. 开发并提交
```bash
git add .
git commit -m "feat: 实现登录功能"
```

建议：
- 多次小提交，不要把所有改动挤成一个 commit
- commit message 清晰可读，方便 review

### Step 4. 开发过程中同步 upstream
如果开发期间原仓库有更新：

```bash
git fetch upstream
git rebase upstream/main
```

这一步在做什么：
- 让当前功能分支持续贴近原仓库最新 `main`
- 避免最后提 PR 时才一次性处理大量冲突

### Step 5. 推送到你的 fork
```bash
git push origin feature-login
```

这一步在做什么：
- 把你的开发分支同步到 GitHub
- 为后续开 PR 做准备

### Step 6. 在 GitHub 上创建 PR
在 GitHub 上：
- 打开你的 fork
- 点击 `Compare & pull request`
- 确认：
  - `base` = 原仓库的 `main`
  - `compare` = 你的功能分支
- 填写描述并提交

这一步在做什么：
- 请求原仓库把你的分支改动合并进去

### Step 7. PR 期间继续同步
如果 PR 提出后，原仓库又更新了：

```bash
git fetch upstream
git rebase upstream/main
git push origin feature-login --force
```

这一步在做什么：
- 保证你的 PR 始终基于最新的 `upstream/main`
- 让 review 更集中在你的真实改动，而不是历史偏差

## 流程全图
```text
upstream（原仓库）
        ↓
     fetch
        ↓
main（只做同步）
        ↓
   新建分支
        ↓
feature-xxx（开发）
        ↓
     push
        ↓
origin（你的 fork）
        ↓
      PR
        ↓
upstream（合并）
```

## 常见错误

### 错误 1：直接在 `main` 上开发
后果：
- 同步困难
- PR 很乱

### 错误 2：只 `pull origin`
```bash
git pull origin main
```

问题：
- `origin` 是你自己的 fork
- 它不会自动帮你同步 `upstream`

### 错误 3：不 rebase 就提 PR
后果：
- 冲突集中爆发
- PR 审查成本上升

### 错误 4：乱用 `force push`
允许的场景：
- `git push origin main --force`
  - 仅用于让 fork 的 `main` 重新对齐 `upstream/main`
- `git push origin feature-xxx --force`
  - 仅用于功能分支 rebase 后更新自己的分支

不建议：
- 对多人共用的长期协作分支乱用 `--force`

## rebase 冲突处理
当你看到：
```text
CONFLICT (content): Merge conflict
```

处理方式：
```bash
# 手动修改冲突文件
git add .
git rebase --continue
```

如果要放弃这次 rebase：
```bash
git rebase --abort
```

## 最简记忆版
```bash
# 同步 main
git checkout main
git fetch upstream
git rebase upstream/main
git push origin main --force

# 新分支开发
git checkout -b feature-xxx

# 提交
git add .
git commit -m "xxx"

# 开发中同步
git fetch upstream
git rebase upstream/main

# 推送
git push origin feature-xxx
```

## 当前项目特别说明
### 1. 2026-04-27 的一次性对齐处理
Gemini-Lab 当前仓库在 `2026-04-27` 做过一次特殊处理：
- 本地 `main` 与 `upstream/main` 原本不是同一条历史
- 为了让 fork 真正对齐原仓库，保留了备份分支后，直接把 `main` 对齐到 `upstream/main`

这属于一次性初始化纠偏，不是日常开发中的常规步骤。

### 2. 当前推荐分支命名
- 功能：`feature/xxx`
- 修复：`fix/xxx`
- 文档：`docs/xxx`
- 工具或工程整理：`chore/xxx`

### 3. 当前项目的智能体要求
后续智能体在做任何涉及 Git 的任务前，都应先阅读本文件，再执行实际版本控制操作。
