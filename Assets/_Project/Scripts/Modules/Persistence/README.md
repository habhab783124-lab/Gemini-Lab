# Modules/Persistence/ — 存档与持久化

## 文件夹职责
统一封装**本地存档/读档**：宠物状态、性格矩阵、家具布局、旅行相册、离线队列；预留云同步接口。

## 核心文件/类（预估）

| 文件 | 说明 |
| :--- | :--- |
| `SaveSystem.cs` | 对外门面 `ISaveSystem`：`SaveAsync<T>`、`LoadAsync<T>`、`DeleteSlot`。 |
| `Storage/JsonFileStorage.cs` | JSON + 可选 AES 加密的本地存储实现。 |
| `Storage/ICloudStorage.cs` | 云同步接口占位。 |
| `Migrations/SaveMigrator.cs` | 存档版本迁移器，支持跨版本 schema 升级。 |
| `Snapshots/PetSnapshot.cs`、`FurnitureLayoutSnapshot.cs`、`TravelGallerySnapshot.cs` | 各模块存档 DTO。 |
| `Interfaces/ISaveable.cs` | 需持久化的模块实现此接口。 |

## 依赖关系
- **依赖**：`Core`。
- **被依赖**：`Pet` / `Furniture` / `Travel` / `Gateway`（离线队列）/ `Desktop`（使用习惯）。
- asmdef：`GeminiLab.Modules.Persistence`。

## 代码规范/注意事项
1. **所有存档结构必须带 `SchemaVersion`**；新增字段时在 `SaveMigrator` 写入迁移方法，严禁破坏性改动老字段。
2. 文件落地路径统一走 `Application.persistentDataPath/Saves/`；照片/大文件走独立子目录。
3. 存档写入必须"先写 tmp 文件再原子 Rename"，防止掉电损坏。
4. DTO 禁止直接引用 `MonoBehaviour`；模块需提供 `ToSnapshot() / FromSnapshot(...)` 转换方法。
5. 加密 Key 不得硬编码在脚本中；通过 `GatewayConfigSO` 的运行期下发或 Keychain 获取。
