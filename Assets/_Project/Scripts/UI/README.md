# Scripts/UI/ — 视图与 ViewModel 层

## 文件夹职责
承载**全部 UI 视图代码**：聊天面板、状态面板、家具库存、性格雷达图、旅行相册等。采用轻量 MVVM，View 负责绑定，不允许承载业务逻辑。

## 核心文件/类（预估）

| 文件 | 说明 |
| :--- | :--- |
| `Framework/UIRoot.cs` | UI 根节点与 Canvas 层级管理（HUD / Modal / Toast）。 |
| `Framework/UIManager.cs` | 面板加载/栈管理（`OpenAsync`、`Close`、`BringToFront`）。 |
| `Framework/BaseView.cs`、`BaseViewModel.cs` | MVVM 基类。 |
| `Chat/ChatPanelView.cs`、`ChatBubbleView.cs`、`ChatViewModel.cs` | 聊天面板与气泡。 |
| `Status/PetStatusView.cs` | 状态值 HUD（心情、饱食、精力、经验值）。 |
| `Personality/PersonalityRadarView.cs` | 7 维性格雷达图。 |
| `Furniture/InventoryView.cs`、`BuildPreviewHUD.cs` | 家具库存与建造模式 HUD。 |
| `Travel/TravelGalleryView.cs`、`TravelDigestPopup.cs` | 相册与旅行小记弹窗。 |
| `Desktop/OverlayChatWindow.cs` | 桌面 Overlay 形态下的聊天窗口。 |

## 依赖关系
- **依赖**：`Core`、`Scripts/Modules/**` 暴露的接口与事件（只读订阅）。
- **被依赖**：无业务模块应反向引用 UI。
- asmdef：`GeminiLab.UI`。

## 代码规范/注意事项
1. **UI 目录下禁止写任何数据持久化、网络请求、FSM 业务逻辑**。发现 `using System.IO` / `UnityWebRequest` 一律拒 PR。
2. View **只允许**订阅 `ViewModel` 暴露的属性/事件；ViewModel 通过 `EventBus` 拉取业务事件。
3. 禁止在 `Update` 中刷新 UI；改为事件驱动的 `SetXxx()` 方法。
4. 所有 UI 文本通过 `LocalizationService.Translate(key)`，严禁硬编码中英文。
5. UI Prefab 必须关联 `View` 脚本且 `SerializeField` 私有字段，不暴露 `public` 组件引用。
6. 性能要求：面板打开 ≤ 100ms；大列表使用 `UIListPool` 对象池。
