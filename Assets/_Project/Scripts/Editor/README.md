# Scripts/Editor/ — 编辑器扩展

## 文件夹职责
存放**仅在 Unity Editor 运行时**的工具与扩展：自定义 Inspector、EditorWindow、资产导入管线、MCP 集成辅助脚本、流水线与 CI 脚本。

## 核心文件/类（预估）

| 文件 | 说明 |
| :--- | :--- |
| `Inspectors/PetStateValuesDrawer.cs` | `StateValues` 可视化编辑。 |
| `Inspectors/FurnitureDefinitionInspector.cs` | 家具定义 SO 自定义 Inspector（包含预览缩略图）。 |
| `Windows/FSMDebuggerWindow.cs` | 运行时 FSM 可视化调试窗口。 |
| `Windows/GatewayMockWindow.cs` | 本地 Mock Gateway 控制台，便于不连外网调试。 |
| `Pipeline/BuildPipeline.cs` | CI 打包入口（`-executeMethod`）。 |
| `Importers/FurniturePrefabPostprocessor.cs` | 自动为家具 Prefab 挂载必要组件。 |
| `MCP/McpBootstrap.cs` | IvanMurzak / Unity-MCP 辅助脚本（若项目启用）。 |

## 依赖关系
- **依赖**：`Core`、全部 `Modules/**`、`UI`、UnityEditor API。
- **被依赖**：**无任何 Runtime 代码可引用 Editor**。
- asmdef：`GeminiLab.Editor`，`includePlatforms` 必须只勾选 **Editor**。

## 代码规范/注意事项
1. 所有编辑器脚本必须位于 `Editor/` 或专用子目录，asmdef `includePlatforms = [Editor]`；否则打包会失败。
2. 禁止在 Runtime 类中通过 `#if UNITY_EDITOR` 引用 `UnityEditor` API —— 影响 IL2CPP 构建。
3. EditorWindow 样式统一使用 `EditorStyles` 与项目的 `EditorSkin.uss`，不自创配色。
4. 资产导入钩子要**幂等**，避免反复修改导致 VCS 抖动。
5. MCP 相关脚本置于 `MCP/` 子目录；对应工具链与 `.cursor/skills/` 配合，但**不得**把业务逻辑写进 MCP 入口。
