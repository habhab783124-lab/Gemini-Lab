# AI Memory Tools

Updated: 2026-04-21

## 目录作用
这个目录预留给后续的记忆维护脚本、索引刷新脚本和文档校验脚本。

当前仓库刚完成 `docs/` 初始化，还没有自动化脚本落地，所以这里只保留约定。

## 未来建议放什么
- `refresh-memory-index.ps1`
  - 扫描 `docs/` 与关键入口文件，刷新 `memory-index.paths.txt`
- `validate-doc-links.ps1`
  - 校验记忆文档里的相对链接是否失效
- `diff-planned-vs-implemented.ps1`
  - 对比 README 规划路径与仓库实际文件，帮助识别“文档超前”问题

## 当前手工规则
1. 新增或删除记忆文档后，要手工更新 `memory-index.paths.txt`。
2. 路径变更后，要同步检查：
   - 主记忆
   - 文件指南
   - 项目结构总览
3. 如果以后增加脚本，请保持 UTF-8 编码并优先使用 PowerShell，方便在当前工作区直接运行。
