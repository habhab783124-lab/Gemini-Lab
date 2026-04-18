# Scenes/ — 工程场景（2D）

## 文件夹职责
存放**所有 Unity 2D 场景文件**。以"功能/形态"划分，不以版本号划分（版本差异用 VCS 管理）。

## 场景清单（规划）

| 场景 | 形态 | 说明 |
| :--- | :--- | :--- |
| `Boot.unity` | 启动场景 | 仅含 `GameBootstrap`，加载核心服务后跳转到默认场景。 |
| `Apartment/Apartment_Main.unity` | 公寓主场景（2D 正交） | Grid + 多层 Tilemap（Floor / Wall / Decoration）+ CompositeCollider2D + `NavMeshSurface2d` 根节点 + 2D Global Light + Orthographic Camera。 |
| `DesktopOverlay/Overlay_Main.unity` | 桌面 Overlay 主场景 | 透明 Orthographic 摄像机 + UI Root + 宠物 Overlay Prefab；无 Tilemap。 |
| `Dev/FSM_Sandbox.unity` | FSM 沙盒 | 仅供 Phase 1 联调，Release 构建不打包。 |
| `Dev/Furniture_Sandbox.unity` | 家具 & 2D NavMesh 沙盒 | 仅供 Phase 2 联调，预置一套 Tilemap 与若干测试家具。 |

## 依赖关系
- 场景文件**只能引用** `_Project/Prefabs/**`、`_Project/ScriptableObjects/**`、`_Project/Art/**`（含 Tiles / Sprites）、`_Project/Audio/**`。
- **禁止**在场景中直接放置编辑器临时 Prefab、插件自带 Demo Prefab。

## 代码规范/注意事项
1. 每个场景必须有一个 `_SceneRoot` GameObject，所有对象作为其子节点，方便批量管理。
2. **摄像机必须为 Orthographic**；`Size` 与像素基准在 `Settings/Rendering/CameraPreset.asset` 统一配置，禁止逐场景随意调整。
3. **Sorting Layers** 顺序在 ProjectSettings 中统一定义：`Background → Floor → Wall → Furniture → Pet → FX → UI`；场景内禁止自建新 Sorting Layer。
4. 默认 `Boot.unity` 为 Build Settings 的 index 0；构建脚本会校验。
5. `Dev/` 目录内的沙盒场景在 `BuildPipeline` 中通过 scene 名前缀 `Dev_*` 排除。
6. 场景内不得残留仅本机可用的绝对路径引用；提交前运行 `Tools/Scene Validator` 自检。
7. **2D 场景无光照烘焙**；2D Lights（Global / Point / Freeform / Spot）默认为实时光。若使用 Shadow Caster 2D，需要在 `Renderer Features` 中显式启用 `Shadow Caster Pass`。
8. 场景合并冲突：启用 `Force Text` + `SmartMerge`，禁止二进制合并。
