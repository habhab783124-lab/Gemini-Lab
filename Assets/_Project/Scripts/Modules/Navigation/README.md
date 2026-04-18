# Modules/Navigation/ — 2D 寻路与动态导航网格

## 文件夹职责
封装 **2D NavMesh 运行时烘焙**（基于 [NavMeshPlus](https://github.com/h8man/NavMeshPlus) —— 将 Unity NavMesh 从 XZ 平面投影到 XY 平面，兼容 Unity 2D 模板）、路径请求、障碍物管理，为宠物 `MovingState` 提供统一 2D 寻路 API。

> 备选方案：A* Pathfinding Project（Grid Graph 模式）。接口层以 `INavigationService` 抽象，底层可替换。

## 核心文件/类（预估）

| 文件 | 说明 |
| :--- | :--- |
| `NavigationService.cs` | 对外门面 `INavigationService`，提供 `RequestPath(Vector2 from, Vector2 to)`、`RebuildAsync`、`SampleValidPosition`。 |
| `Runtime/NavMesh2DRebaker.cs` | 基于 `NavMeshSurface2d`（NavMeshPlus）的增量烘焙调度，合并短时间内多次请求。 |
| `Runtime/NavAgent2DBridge.cs` | `NavMeshAgent` (XY 投影模式) 与宠物 FSM 的桥接；内部维持 `Rigidbody2D` 的位置同步。 |
| `Obstacles/DynamicObstacle2D.cs` | 挂在家具根节点，自动维护 `NavMeshModifier` + 2D Collider 的 baking 参数。 |
| `Utils/PathSmoother.cs` | 路径拐角平滑（可选）。 |
| `Events/NavEvents.cs` | `NavMeshRebuiltEvent`、`PathRequestFailedEvent`。 |

## 依赖关系
- **依赖**：`Core`、**NavMeshPlus** 包（`com.unity.navmeshplus`，Git URL 导入）、Unity `com.unity.ai.navigation`。
- **被依赖**：`Pet`（移动状态）、`Furniture`（摆放后触发 Rebake）、`Travel`（离场/回归位置校验）。
- asmdef：`GeminiLab.Modules.Navigation`；对外接口程序集 `GeminiLab.Modules.Navigation.Abstractions` 便于切断循环依赖与底层方案替换。

## 代码规范/注意事项
1. **NavMeshPlus 配置**：`NavMeshSurface2d.collectObjects = Children`，`useGeometry = PhysicsColliders`；根节点 `_Navigation` 的 `transform.up` 必须为 `(0, 0, -1)` 以确保 XY 投影正确。
2. **Rebake 必须是异步增量**（`BuildNavMeshAsync`），严禁在主线程全量重建，会导致 200ms+ 卡顿与 `Rigidbody2D` 抖动。
3. 统一路径请求入口为 `NavigationService.RequestPath`，禁止业务层直接操作 `NavMeshAgent.SetDestination`（无法统一处理失败与日志）。
4. 2D 场景下 `NavMeshAgent` 的 `updatePosition = false` + `updateRotation = false`，位置与朝向由 `NavAgent2DBridge` 同步到 `Rigidbody2D.MovePosition` 与 Sprite 翻转，避免与 Physics2D 冲突。
5. 家具摆放/拆除 → 先启用/禁用 `DynamicObstacle2D` → 再发起 `RebuildAsync`；拖动预览阶段**不触发 Bake**，仅显示 UI 预览。
6. 场景中只允许存在一个 `NavMeshSurface2d` 根节点（位于 Apartment 场景的 `_Navigation` GameObject 下）。
