# Modules/Gateway/ — OpenClaw 网关通信

## 文件夹职责
实现与 **OpenClaw Gateway** 的全双工通信：Prompt Context 组装、HTTP/WebSocket 双通道、聊天/工作/事件响应分发、断网降级与重试。

## 核心文件/类（预估）

| 文件 | 说明 |
| :--- | :--- |
| `GatewayClient.cs` | 对外门面 `IGatewayClient`，封装 `SendChatAsync` / `SendWorkCommandAsync` / `SubscribeEvents`。 |
| `Transport/HttpTransport.cs` | 基于 `UnityWebRequest` / `HttpClient` 的 REST 通道。 |
| `Transport/WebSocketTransport.cs` | 长连接事件流通道（工作完成、旅行回归）。 |
| `Prompt/PromptContextBuilder.cs` | 汇总 State/Personality/FSM → `PromptPayload`。 |
| `Prompt/SystemPromptComposer.cs` | 按性格值渲染 System Prompt 模板。 |
| `Routing/GatewayEventRouter.cs` | 根据响应类型分发到 Pet/Travel/Furniture 事件。 |
| `Dto/PromptPayload.cs`、`GatewayResponse.cs` | 序列化 DTO。 |
| `Resilience/RetryPolicy.cs`、`OfflineQueue.cs` | 重试/指数退避/离线队列重放。 |

## 依赖关系
- **依赖**：`Core`、`Pet`（只读状态）、`Persistence`（离线队列落盘）。
- **被依赖**：`Pet`（工作模式状态解锁）、`Travel`（旅行结果回调）、`UI`（聊天气泡）。
- asmdef：`GeminiLab.Modules.Gateway`。

## 代码规范/注意事项
1. **所有网络调用必须走 `IGatewayClient`**；任何脚本里出现 `new UnityWebRequest` / `new HttpClient` 一律 PR 拒绝。
2. 敏感字段（Token、用户 ID）**不得出现在日志**；`GatewayClient` 内统一打码。
3. DTO 使用 `System.Text.Json` + Source Generator；避免反射分配。
4. 超时默认：聊天 10s、工作 30s、WebSocket 心跳 15s；策略集中于 `GatewayConfigSO`。
5. 所有事件响应必须带 `traceId`，与 Pet FSM 日志、Travel 日志可串联。
6. **线程安全**：`HttpClient` 回调可能不在主线程，写 FSM/UI 时需经 `MainThreadDispatcher`。
