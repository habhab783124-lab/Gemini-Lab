# _Project/ — Gemini-Lab 核心业务目录

> 本目录是项目**自研业务代码与美术资源的唯一落脚点**。下划线前缀 `_` 用于保证在 Unity Project 面板中永远排序置顶，与 `Plugins/`、`ThirdParty/` 等外部资源严格隔离。

## 技术基线

- **Engine**：Unity 2022.3 LTS · **2D 模板** (URP 2D Renderer + 2D Lights)
- **Camera**：Orthographic
- **Physics**：Physics2D（Rigidbody2D / Collider2D）
- **Tiling**：Tilemap + Rule Tile + CompositeCollider2D
- **Animation**：Unity 2D Animation 包（骨骼绑定 / PSD Importer）+ Animator Controller
- **Pathfinding**：NavMeshPlus（2D NavMesh 端口）
- **Sorting Layers**：`Background → Floor → Wall → Furniture → Pet → FX → UI`

## 根目录职责划分

| 子目录 | 职责（一句话） |
| :--- | :--- |
| `Scripts/` | 项目全部 C# 业务代码（Core 基础设施 / Modules 业务模块 / UI / Editor 扩展）。 |
| `Prefabs/` | 所有运行时可实例化的预制体（宠物、家具、UI、环境）。 |
| `ScriptableObjects/` | 数据驱动配置资产（宠物模板、家具定义、性格矩阵、网关配置等）。 |
| `Scenes/` | 工程场景文件（公寓、桌面 Overlay、Boot 引导）。 |
| `Art/` | 美术资源（Models / Materials / Textures / Animations / Shaders）。 |
| `Audio/` | 音频资源（BGM / SFX）。 |
| `Settings/` | URP/Input/Physics 等 Unity 运行期配置资产。 |

## 总体约束

1. **禁止跨模块直接引用**：模块间通过 `EventBus` 或 `ServiceLocator` 通信；Prefabs/ScriptableObjects 不允许硬引用其他业务 `Modules` 的具体类。
2. **禁止污染外部目录**：所有业务资源必须落在 `_Project/` 下，不得写入 `Assets/Plugins/` 或 `Assets/ThirdParty/`。
3. **命名规范**：目录使用 `PascalCase`，脚本文件名与类名一致。
4. **asmdef 分层**：每个 `Modules/*` 子目录独立一个 `asmdef`，依赖关系通过 Assembly Definition References 显式声明，禁止循环依赖。
