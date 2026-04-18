# Prefabs/ — 预制体（2D）

## 文件夹职责
存放**所有运行时可实例化的 Unity 2D 预制体**。按业务域分类组织，严禁按场景切分（跨场景复用是预制体的价值来源）。

## 子目录规划

| 子目录 | 内容 |
| :--- | :--- |
| `Pet/` | 宠物主体 Prefab（`SpriteRenderer` + `Animator` + `Rigidbody2D` + `CapsuleCollider2D` + `NavMeshAgent` + `SortingGroup` + `PetController`）。 |
| `Furniture/` | 家具 Prefab（`SpriteRenderer` + 可选 `Animator` + `BoxCollider2D` + `DynamicObstacle2D` + `SortingGroup` + `Furniture`）。 |
| `UI/Panels/` | 聊天面板、库存、性格雷达图等面板预制体（UGUI Canvas）。 |
| `UI/Widgets/` | 可复用 UI 小控件（按钮、列表项、气泡）。 |
| `Environment/` | Tilemap 预制体（Floor Tilemap、Wall Tilemap、Decoration Tilemap）与 2D Light 预制体。 |
| `FX/` | 粒子 / Sprite 动画特效预制体（离场转场、旅行光圈、建造提示）。 |

## 依赖关系
- 预制体上**只允许引用**：
  - `_Project/Scripts/Modules/**` 中的运行时组件（非 `internal`）。
  - `_Project/ScriptableObjects/**` 配置资产。
  - `_Project/Art/**`（Sprites / Animations / Shaders）与 `_Project/Audio/**` 资源。
- **禁止引用**：Editor 脚本、Plugins 中的 Demo Prefab、具体场景里的 GameObject。

## 代码规范/注意事项
1. **Prefab 变体 (Variant) 优先**：家具、UI 面板等系列资产必须基于 Base Prefab 派生，避免重复挂载。
2. 任何脚本上的 `public` 引用槽必须在 Prefab 中预先赋值，避免运行时 `FindObject`。
3. 命名规范：`{领域}_{对象}_{变体}` —— 例：`Furniture_Sofa_Luxury.prefab`、`UI_Panel_Chat.prefab`、`Env_Tilemap_Floor.prefab`。
4. **2D 渲染层级**：
   - 所有 Prefab 根节点必须挂 `SortingGroup` 统一内部渲染顺序。
   - Sorting Layers 顺序（从后往前）：`Background → Floor → Wall → Furniture → Pet → FX → UI`。
   - 动态对象 (Pet / Furniture) 的 `sortingOrder` 由运行时脚本根据 `transform.position.y` 刷新。
5. **2D 物理约束**：
   - 家具 Collider 使用 `BoxCollider2D`，贴墙家具 `isTrigger = true` 避免阻挡宠物贴墙走。
   - 宠物使用 `CapsuleCollider2D`（方向 Vertical），配合 `Rigidbody2D (gravityScale = 0, freezeRotation = Z)`。
6. 家具类 Prefab 根节点必须挂 `DynamicObstacle2D`；否则 2D NavMesh 不识别。
7. 不允许在 Prefab 上直接保存**运行时状态**（如已选中的家具）；状态只能由 Service 管理。
8. Prefab 每次修改必须 Apply 到根资产，禁止保留"Modified on Instance"。
