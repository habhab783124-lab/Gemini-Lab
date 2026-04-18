# Modules/Desktop/ — 桌面 Overlay 与系统级感知

## 文件夹职责
将客户端切换为 **透明无边框桌面 Overlay 形态**，宠物驻留屏幕边缘；探测前台应用，触发主动对话（如检测到 VS Code 提议工作模式）。

## 核心文件/类（预估）

| 文件 | 说明 |
| :--- | :--- |
| `DesktopOverlayService.cs` | 对外门面 `IDesktopOverlayService`：`EnableOverlay()`、`DisableOverlay()`、`SetClickThrough(bool)`。 |
| `Native/Win32Interop.cs` | P/Invoke 封装 (`SetWindowLong`、`SetLayeredWindowAttributes`、`DwmExtendFrameIntoClientArea`)。 |
| `Native/MacOverlayBridge.cs` | macOS 占位实现（后续扩展）。 |
| `Sensing/ForegroundWindowProbe.cs` | 轮询/钩子读取前台窗口标题 + 进程名。 |
| `Sensing/AppContextRule.cs` | 规则引擎：前台应用 → 建议动作（打开 VS Code → 提议 Work）。 |
| `Events/DesktopEvents.cs` | `ForegroundAppChangedEvent`、`OverlayClickThroughChangedEvent`。 |

## 依赖关系
- **依赖**：`Core`、`Pet`（读取当前状态决定是否发起提议）、`Gateway`（发起主动对话）。
- **被依赖**：`UI`（桌面聊天窗、点击穿透开关）、`Persistence`（记录用户使用习惯）。
- asmdef：`GeminiLab.Modules.Desktop`；平台相关实现用 `#if UNITY_STANDALONE_WIN` 隔离。

## 代码规范/注意事项
1. **所有 P/Invoke 必须集中在 `Native/`**，且每个方法注释对应 MSDN/苹果官方文档链接。
2. 前台窗口探测频率 ≤ 1Hz，避免 CPU 占用；使用系统钩子时必须在 `OnApplicationQuit` 释放。
3. 隐私合规：**仅读取进程名与窗口标题**，不读窗口内容；用户需在设置中显式授权"应用感知"功能。
4. 透明窗口在不同 DPI/多屏下坐标系不同，必须通过 `Screen.mainWindowPosition`（或 Win32 `GetMonitorInfo`）二次校准。
5. 点击穿透切换需在**主线程同帧**完成，否则与 UGUI 事件冲突。
