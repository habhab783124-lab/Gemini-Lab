# Scripts/ — C# 业务代码根目录

## 文件夹职责
存放项目**所有自研 C# 脚本**，按 `Core / Modules / UI / Editor` 四层划分，配合 asmdef 实现编译隔离与快速迭代。

## 目录分层（自底向上）

```
Scripts/
├── Core/        # 与 Unity 无关或通用性极强的底层基础设施（FSM、事件、工具、接口）
├── Modules/     # 业务模块（Pet / Furniture / Navigation / Gateway / Travel / Desktop / Persistence）
├── UI/          # 纯视图与 ViewModel 层，不承载业务规则
└── Editor/      # 仅编辑器下运行的扩展（Inspector、Window、Importer、MCP 集成）
```

## 依赖关系（单向向下）

```
Editor   ─┐
UI       ─┼──►  Modules  ──►  Core
         ─┘
```

- `Core` 不依赖任何其他子目录。
- `Modules` 可依赖 `Core`，模块之间**只能通过 EventBus / 接口 / ServiceLocator** 通信。
- `UI` 可依赖 `Core` 与 `Modules` 的对外接口；**反向依赖被禁止**（`Modules` 不得 `using UI`）。
- `Editor` 可依赖所有 Runtime 程序集，但 Runtime 代码**不得依赖 Editor**。

## 代码规范
- 所有 `public` 类型必须写 XML Doc 注释。
- 使用 `#nullable enable`（若团队统一启用）并严格处理空引用。
- 禁止在 `Update` 里做 `GetComponent` / `FindObjectOfType` —— 在 `Awake/Start` 缓存。
- 每个子模块独立 asmdef，命名为 `GeminiLab.<Layer>.<Module>`（例：`GeminiLab.Modules.Gateway`）。
