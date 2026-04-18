# Scripts/Core/ — 底层基础设施

## 文件夹职责
提供**与具体业务无关、可被所有模块复用**的基础设施代码（HFSM、事件总线、命令模式、ServiceLocator、日志、工具类）。

## 核心文件/类（预估）

| 子目录 / 文件 | 说明 |
| :--- | :--- |
| `FSM/IState.cs` | 状态接口：`Enter / Tick / FixedTick / Exit`。 |
| `FSM/StateMachine.cs` | 通用状态机泛型容器 `StateMachine<TContext>`。 |
| `FSM/SubStateMachine.cs` | 分层 HFSM 支持。 |
| `FSM/StateTransition.cs` | 带条件的状态转移规则。 |
| `Events/EventBus.cs` | 全局事件总线（基于类型 Key 的发布/订阅）。 |
| `Events/ICommand.cs`、`CommandDispatcher.cs` | 事件驱动的命令模式实现。 |
| `ServiceLocator.cs` | 轻量 IoC 容器，注册运行期单例服务。 |
| `GameBootstrap.cs` | 启动引导入口，负责按序初始化核心服务。 |
| `Utils/` | `CoroutineRunner`、`MainThreadDispatcher`、`ObjectPool<T>`、`Logger` 等工具。 |
| `Interfaces/` | 跨模块共享的抽象接口（如 `ISaveable`、`ITickable`）。 |

## 依赖关系
- **对外**：不依赖任何其他业务模块或 UI。可依赖 Unity 核心 API (`UnityEngine`、`UnityEngine.Events`) 与少量稳定第三方库。
- **对内**：被 `Scripts/Modules/**` 与 `Scripts/UI/**` 大量引用。
- asmdef 命名：`GeminiLab.Core`。

## 代码规范/注意事项
1. **零业务耦合**：不得出现"宠物""家具""网关"等业务词汇；只提供泛型/抽象能力。
2. **零 GC 敏感**：事件总线、对象池需提供 struct event 版本，避免 `Update` 热路径分配。
3. **单元测试覆盖率 ≥ 80%**，所有公开 API 必须有 EditMode 测试。
4. **禁止 `MonoBehaviour` 承载纯逻辑**；除非必要（如 `CoroutineRunner`），否则使用 POCO + 接口。
5. 接口命名以 `I` 开头；抽象基类以 `Abstract` 或 `Base` 前缀可选，保持团队一致。
