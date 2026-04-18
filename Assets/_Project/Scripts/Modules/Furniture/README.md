# Modules/Furniture/ — V-Decor 建造系统（2D）

## 文件夹职责
实现 **2D 视角下** 长按 `V` 键的家具建造模式、基于 `Grid / Tilemap` 的网格吸附、环境 Buff 聚合、家具羁绊（习惯标签）与交互锚点管理。

## 核心文件/类（预估）

| 文件 | 说明 |
| :--- | :--- |
| `FurnitureService.cs` | 对外门面 `IFurnitureService`（查询库存/已放置/Buff 总和）。 |
| `BuildMode/BuildModeController.cs` | 监听 `V` 长按，切换建造模式 UI 与 `InputActionMap`。 |
| `BuildMode/Grid2DSnapper.cs` | 屏幕坐标 → 世界坐标 → `Grid.WorldToCell` 吸附；区分地板层 / 贴墙层。 |
| `BuildMode/PlacementValidator2D.cs` | 基于 `Physics2D.OverlapBox` / `Tilemap.HasTile` 的合法性校验（避免重叠、越界、墙外）。 |
| `Furniture.cs` | 运行时家具组件（挂在 Prefab 根节点，包含 `SpriteRenderer` / `Animator` / `BoxCollider2D`）。 |
| `InteractionAnchor.cs` | 交互锚点（Vector2 坐标 + 朝向位 + 可用状态位）。 |
| `BuffAggregator.cs` | 汇总所有已摆放家具的 `EnvironmentalBuff`。 |
| `HabitTagService.cs` | 记录宠物-家具交互频率，生成"习惯标签"。 |
| `Events/FurnitureEvents.cs` | `FurniturePlacedEvent`、`FurnitureRemovedEvent`。 |

## 依赖关系
- **依赖**：`Core`、`Navigation`（放置后触发 2D NavMesh 增量烘焙）、Unity `Tilemap` 包。
- **被依赖**：`Pet`（读取 Buff、通过 `InteractionAnchor` 发起交互）、`UI`（库存/建造预览 UI）、`Persistence`（存档家具布局）。
- asmdef：`GeminiLab.Modules.Furniture`。

## 代码规范/注意事项
1. 家具数据 = `FurnitureDefinitionSO`，包含 **Sprite 引用、Animator Controller、Buff 列表、可放置层（Floor / Wall）、占用格子尺寸 (Vector2Int)**。**严禁硬编码家具参数**。
2. 家具 Prefab 根节点统一使用 **`SortingGroup`** 组件统一渲染顺序；`SpriteRenderer.sortingLayer = "Furniture"`，`sortingOrder = -(int)(transform.position.y * 100)` 实现 Y 轴排序。
3. 放置合法性判定由 `PlacementValidator2D` 一家说了算（Tilemap 层校验 + Physics2D.OverlapBox），UI 预览颜色变化订阅其事件。
4. **不得直接修改宠物状态值**；通过广播 `FurnitureInteractionEvent`，由 `Pet` 模块决定数值变化。
5. 每次家具放置/拆除必须同步通知 `NavigationService.RebuildAsync()`，否则会出现寻路穿模。
6. 建造模式下全局时间尺度保持 1.0，不得使用 `Time.timeScale = 0`（会影响 Gateway 心跳）。
7. 贴墙家具（如壁画、挂钟）通过 `Furniture.Placement = Wall`，吸附到 Tilemap 的 "Wall" 子层，**渲染 SortingOrder 高于地板家具**，交互锚点的朝向固定向外。
