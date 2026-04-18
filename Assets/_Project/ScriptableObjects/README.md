# ScriptableObjects/ — 数据驱动配置资产

## 文件夹职责
承载**所有数据驱动的配置类资产**（SO）。任何数值、模板、映射表都必须落在这里，**严禁硬编码在 MonoBehaviour 或静态类字段**。

## 子目录规划

| 子目录 | 内容 |
| :--- | :--- |
| `PetConfig/` | `PetArchetypeSO`（品种模板）、`StateValueRulesSO`（精力衰减曲线）、`StateMachineBlueprintSO`。 |
| `PersonalityConfig/` | 性格演化规则 `PersonalityEvolutionRulesSO`、阈值表、阶跃事件配置。 |
| `FurnitureConfig/` | `FurnitureDefinitionSO`（每件家具一个），包含 Buff 列表、可放置面、交互动画键。 |
| `InteractionConfig/` | `InteractionTableSO`（家具 × 状态值影响表）。 |
| `GatewayConfig/` | `GatewayConfigSO`（Endpoint、超时、重试）、`SystemPromptTemplateSO`（按性格渲染）。 |
| `TravelConfig/` | 目的地池、奖励曲线、迷路超时配置。 |
| `UIConfig/` | UI 主题、色板、字体映射、本地化键。 |

## 依赖关系
- **被依赖**：Prefabs、Scripts/Modules、Scripts/UI 均可只读引用。
- **自身依赖**：SO 之间允许互相引用（如 `FurnitureDefinitionSO` 引用 `InteractionTableSO`），但必须**无环**。
- **不允许**：SO 引用任何场景中的 GameObject 或运行时实例。

## 代码规范/注意事项
1. 所有 SO 类必须打 `[CreateAssetMenu(menuName = "GeminiLab/...")]`，分组命名统一以 `GeminiLab/` 开头。
2. 字段必须用 `[SerializeField]` + `private/protected` + 属性暴露，避免外部代码修改运行时实例字段。
3. **运行期不得修改 SO 字段**（Editor 下除外）；需要运行态数据的，在 `PetService` 等运行时容器里拷贝一份实例。
4. 命名规范：`{领域}_{对象}.asset` —— 例：`Furniture_Sofa_Luxury.asset`、`PetArchetype_Standard.asset`。
5. 资产移动须使用 Unity 菜单而非手动改文件路径，防止 GUID 丢失。
6. 新增 SO 类型时，必须在 `_Project/Scripts/Modules/<Module>/Data/` 同时提供 Editor 校验工具（如重复 ID 检查）。
