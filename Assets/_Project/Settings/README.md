# Settings/ — Unity 运行期配置资产（2D）

## 文件夹职责
存放 Unity 原生的**运行期设置资产**：URP 2D 渲染管线 Asset、Input Actions、Physics2D 层设置、2D 摄像机预设等。**仅包含 Unity 系统级配置**，业务数值配置放在 `_Project/ScriptableObjects/`。

## 子目录规划

| 子目录 | 内容 |
| :--- | :--- |
| `Rendering/` | `UniversalRenderPipelineAsset` + **2D Renderer Data** (`Renderer2DData`)；`CameraPreset.asset`（Orthographic 基准 Size、Pixel Perfect 参数）。 |
| `Input/` | `InputActionAsset`（Apartment / Overlay / Build 三套 ActionMap）。 |
| `Physics2D/` | `Physics 2D` 层碰撞矩阵导出 (`Physics2DSettings.asset` 镜像)；预设的 `PhysicsMaterial2D`。 |
| `Localization/` | 本地化 Tables、Locale 配置（若启用 Unity Localization）。 |

## 依赖关系
- **被引用**：`ProjectSettings/GraphicsSettings` 指向本目录的 URP 2D Asset；Prefabs 引用 `PhysicsMaterial2D`。
- **禁止**：业务脚本直接修改这些资产的字段（运行时）；所有动态调节通过对应包的公共 API（如 `Light2D.intensity`、`InputSystem.actions.Enable`）。

## 代码规范/注意事项
1. URP Pipeline Asset 命名：`URP2D_{平台}_{质量}.asset`（例 `URP2D_PC_High.asset`、`URP2D_Mobile_Low.asset`）；每个 Asset 必须挂 2D `Renderer2DData`。
2. **Renderer 必须为 2D Renderer**；若发现 `ForwardRenderer` 配置一律 PR 拒绝（2D 模板禁止 3D Renderer）。
3. Input Action Map 划分固定三套：`Apartment`（公寓内）、`Overlay`（桌面模式）、`Build`（建造模式长按 `V`）；不得混用。
4. **Physics2D 层矩阵**：固定层位 `Default / Pet / Furniture / Wall / Interactable / UI`；`Pet × Wall = On`、`Pet × Furniture = Off (由 NavMesh 解决)`、`Furniture × Wall = Off (摆放时 Overlap 检测单独处理)`。矩阵变更必须提 PR 评审。
5. 本地化 Table 采用 Key 命名 `{面板}.{功能}.{项}`，例 `Chat.Input.Placeholder`。
6. 所有 Settings 资产的修改必须在 PR 描述中体现"影响的场景/构建目标"，便于 QA 校验。
7. **禁止**在此目录放置 ScriptableObject 业务配置（那些属于 `_Project/ScriptableObjects/`）。
