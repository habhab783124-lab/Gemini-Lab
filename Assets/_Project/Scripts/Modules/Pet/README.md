# Modules/Pet/ — 宠物养成与行为驱动

## 文件夹职责
负责**宠物实体**的运行时表现：状态值/性格值数据、行为权重决策（公寓桌宠）、HFSM 状态节点宿主（WorldMap）、交流系统与对外事件广播。

## 数值口径（docs/数值规则_策划补齐版.md）
公寓场景的双宠自主行为由**行为权重系统**驱动（§25）：

```text
FinalWeight = BaseWeight × PersonalityMultiplier × EnergyMultiplier × MoodMultiplier × RepeatMultiplier
```

- **性格倍率（§2）**：`Clamp(1 + Σ(Direction × Trait × 0.25), 0.5, 1.5)`。内部性格以 **-1..1** 存储，与文档 0~100 量表的 `(Trait-50)/50` 完全等价（如文档 Integrity=88 → 内部 0.76）。
- **精力倍率（§3）**：`0.6 + 0.8 × (1 - |Energy - PreferredEnergy|/100)`（0.6~1.4）。
- **精力硬规则（§4）**：>70 不睡觉；<10 禁活跃行为（`BehaviorTag.Active`）；≤5 强制睡觉（绕过冷却与权重，避免死锁）。
- **心情分档/倍率（§5/§6）**：<30 低 / 30~69 中 / ≥70 高，按 `BehaviorCategory` 查表。
- **重复倍率（§9/§10）**：冷却 0 > 上一个 0.2 > 最近三个 0.6 > 1.0；冷却从行为**结束**时起算。
- **精力自然变化（§11）**：清醒每现实 3 分钟 -1；睡觉期间不自然衰减（恢复由睡觉行为完成时 +25 一次性结算，§12）。
- **心情回归（§13）**：每 5 分钟向 50 回归 1 点（不越过）。
- **离线规则（§20）**：精力不衰减；心情按离线时长向 50 回归（每 5 分钟 1 点，最多 6 点）。

## 核心文件/类

| 文件 | 说明 |
| :--- | :--- |
| `ApartmentPetMovement.cs` / `ApartmentWalkPath.cs` | 公寓显式绑定的固定物理帧移动、脚底边界、家具扫掠/滑动与可见图绕行；不替换 WorldMap 路径。详见 `docs/apartment-movement.md`。 |
| `PetController.cs` | `MonoBehaviour` 运行时宿主。玩家可控宠走输入链；非玩家可控宠在配置了 `_behaviorConfig` 时走**行为驱动循环**（待机/抽取 → Moving → 执行绑定交互 → §12 结算 → §9/§10 记录），否则回退旧随机漫游；无输入控制器的宠（WorldMap）走 FSM。 |
| `Behavior/BehaviorConfigSO.cs` | 每只宠一张行为表：BaseWeight/Category/性格标签/偏好精力/结算增量/冷却/绑定 InteractionType。资产位于 `_Project/ScriptableObjects/PetConfig/BehaviorConfig_{Angel,Devil}.asset`。 |
| `Behavior/BehaviorWeightCalculator.cs` | 四倍率纯函数（§2/§3/§6/§9）。 |
| `Behavior/BehaviorSelector.cs` | 候选 → 精力硬规则 → 冷却 → 四倍率 → 归一化加权随机；强制睡觉与全零兜底待机。 |
| `Behavior/BehaviorRuntimeState.cs` | 当前行为、最近 3 个行为、冷却表（运行期，不持久化）。 |
| `Social/PetSocialService.cs` | 交流系统（§14-19）：对子亲密度（初始 30）、ResponseType（NEED_SPACE/WARM/NORMAL）、结算表、300s 防刷冷却、`IPersistentService`（Key=`pet_social`）。点击宠物触发：被点击者为“对方”，另一只为“发起者”（`PetClickReactionController`）。 |
| `PetRuntimeSaveService.cs` | 双宠运行态存档（Key=`pet_runtime`，v2 附保存时刻，恢复时按 §20 做离线心情回归）。 |
| `PetStateValueSO.cs` | 数值阈值配置（§4/§8/§11/§13 + WorldMap FSM 旧阈值）。 |
| `Personality/PersonalityEvolutionService.cs` | 7 维性格运行态演化（-1..1）。初始值资产 `PersonalityMatrix_{Angel,Devil}.asset` 已按 §23 配置。 |
| `StatTickService.cs` | 每秒数值 Tick（§11/§13 + 饱食缓慢衰减）。 |
| `PetRuntimeBootstrap.cs` | 场景加载后兜底注册 Roster / SaveService / SocialService。 |

## 依赖关系
- **依赖**：`Core`（FSM、EventBus、ServiceLocator）、`Navigation`（仅接口 `INavigationService`）、`Furniture`。
- **被依赖**：`Gateway`（读取状态值组装 Prompt）、`Furniture`（交互回调写入 StateValues）、`UI`（订阅状态变更事件）、`Travel`、`Persistence`。
- asmdef：`GeminiLab.Modules.Pet`，仅引用 `GeminiLab.Core`、`GeminiLab.Modules.Navigation.Abstractions`、`GeminiLab.Modules.Furniture`。

## 代码规范/注意事项
1. **所有状态值变更必须走 `StatTickService`**（`Tick` / `ApplyEnvironmentalBuff` / `ApplySatietyDelta`），不允许在其他地方直接赋值，否则钳制与事件无法保证。
2. 性格值演化遵循"事件 → 累积 → 阶跃"三段式：记录事件、累加权重、达到阈值再改变性格等级。
3. `PetController` **禁止** `using UnityEngine.UI`；UI 只能订阅事件。
4. 行为表的"魔法数字"全部在 `BehaviorConfigSO` / `PetStateValueSO` 资产上调整，不写死在代码里；数值口径以策划文档章节号注释为准。
5. 行为→家具交互走宠物 prefab 上 `PetPlayerFurnitureInteractionController` 的手动绑定（`BindingInteractionType` 唯一对应一条绑定）；公寓 `_placedFurniture` 为空，不要依赖 FurnitureService 查询。
6. 2D 渲染顺序：宠物根节点挂 `SortingGroup`，`sortingLayer = "Pet"`；`sortingOrder` 按 Y 轴动态刷新以实现与家具的正确遮挡。
7. 所有物理位移走 `Rigidbody2D` (`bodyType = Dynamic`, `gravityScale = 0`, `freezeRotation = Z`)，严禁直接修改 `transform.position` 作为常规移动链，否则 NavAgent 与 Physics2D 都会错位。
8. 当前玩家控制链与运行时位置回写都应通过 `PetController` 内部统一的物理位置入口推进，不允许在别的脚本里绕过刚体直接推角色。
9. 桌宠活动范围当前采用场景内独立的 `PetMovementBounds`（`BoxCollider2D` + `isTrigger = true`）表示矩形可行动区域；`PetController` 在移动推进前会按这个矩形范围做位置钳制，后续只需在 Inspector 里调整该 `BoxCollider2D` 的位置和尺寸即可。
10. 行为驱动模式下 `RandomWander.ExternalControlEnabled = true`：漫游组件只负责移动执行，目标由行为驱动循环通过 `SetExternalTarget` 下发；旧随机选目标逻辑被跳过。

