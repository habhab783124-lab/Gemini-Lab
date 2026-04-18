# Modules/Travel/ — 旅行系统

## 文件夹职责
处理"去旅游"指令全生命周期：离场 → 异步等待 OpenClaw 生成旅行小记/照片 → 回归时触发心情与性格奖励，并向 UI 广播画廊数据。

## 核心文件/类（预估）

| 文件 | 说明 |
| :--- | :--- |
| `TravelService.cs` | 对外门面 `ITravelService`：`StartTravel`、`OnTravelReturned`、`GetGallery`。 |
| `TravelCommand.cs` | 命令对象（附带目的地标签、持续时长偏好）。 |
| `TravelSession.cs` | 单次旅行运行时对象（状态机：Leaving → Away → Returning → Settled）。 |
| `TravelRewardResolver.cs` | 将 Gateway 返回的 `TravelDigest` 转换为性格/心情增量。 |
| `Data/TravelDigest.cs`、`TravelPhoto.cs` | 旅行小记 DTO。 |
| `Events/TravelEvents.cs` | `TravelStartedEvent`、`TravelReturnedEvent`、`TravelPhotosReadyEvent`。 |

## 依赖关系
- **依赖**：`Core`、`Gateway`（下发旅行指令、监听返回）、`Pet`（写入性格/心情奖励）、`Persistence`（持久化旅行历史与相册）。
- **被依赖**：`UI`（相册 UI、旅行日志）。
- asmdef：`GeminiLab.Modules.Travel`。

## 代码规范/注意事项
1. 旅行期间宠物实体应被隐藏而非销毁，保持状态连续；离场通过 `PetService.SetOffscreen(true)` 控制。
2. 旅行结果**只能由 Gateway 回调触发**，客户端不得本地伪造（防止性格值异常增长）。
3. 照片文件落地路径：`Application.persistentDataPath/Travel/{sessionId}/`；命名 `photo_{index}.webp`。
4. 超时策略：旅行默认最长 24h（真实时间），超时自动触发"迷路"事件并惩罚勇敢值。
5. 性格奖励必须通过 `PersonalityMatrix.ApplyDelta(source: "Travel")` 写入，便于追溯来源。
