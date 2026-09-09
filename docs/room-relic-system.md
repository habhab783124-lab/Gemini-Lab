# 遗留物系统

更新：2026-09-09。本文描述当前 `feature/room-relic-system` 工作区的实现；同步到本地的 `main`（`a683a60`）尚不包含该模块。

## 玩法与状态

天使和恶魔各自进入对应房间的碰撞区域时判定，不以当前主控角色或 UI 面板开关为条件。两个房间分别记录状态；两个角色同时在各自房间时，都能响应好友度解锁。没有收到初始进入事件的对象通过 `OnTriggerStay2D` 补检测；房间组件禁用时清除占用记录。

| 类型 | 条件 | 刷新与交互 |
| --- | --- | --- |
| 纸条 | 无好友度门槛 | 每房间每日 50%；弹窗成功打开后消耗 |
| 临时遗物 | 45 ≤ 好友度 < 80 | 首次解锁必出，之后每日 50%；弹窗成功打开后消耗 |
| 永久赠礼 | 好友度 ≥ 80 | 首次解锁必出，之后每日 15%；按方向从未获得池抽取，永久保存 |

纸条与遗物/赠礼独立判定。达到 80 后清除临时遗物。永久赠礼在好友度降低后仍保留。每日以 `IGameClock.TodayIso` 为界，在进入房间时处理；不会在停留期间午夜自动刷新，也不补发离线多天奖励。当天失败、消费或同日重新进入不会重复抽取；同日首次跨门槛仍可获得首次奖励。

当前配置有 8 条纸条、10 个临时遗物、4 个赠礼。纸条已有可编辑文案；“速写”“小星星吊坠”仍缺少正式 Sprite，使用原有占位，不能把它们描述为美术已完成。

## 代码与生命周期

- `Assets/_Project/Scripts/Modules/RoomRelic/RoomRelicService.cs`：规则、随机抽取、状态事件、存档与旧档迁移。
- `RoomRelicRuntimeBootstrap.cs`：通过场景序列化 Catalog 建立服务，注册到 `ServiceLocator` 和 `IPersistentServiceRegistry`；场景退出时暂停好友度监听并清除房间占用，再次进入复用服务并 `Resume()`，保留会话进度。
- `RoomRelicRoomView.cs` / `RoomRelicView.cs`：监听服务、切换预置变体；禁用时取消订阅，重新启用时读取快照。
- `RoomRelicInteraction.cs`：只在 `IUIRouter.Open` 成功后消费物品，确保面板先读取完整内容。
- `ApartmentViewportInputBridge.cs`：将 RawImage 内左键点击转发至遗留物；建造模式外的右键不会消费物品。
- `Core/UI/UIRouter.cs`：三类遗留物弹窗覆盖当前页面，关闭后回到原页面；切换弹窗关闭前一个，切换普通页面仍按互斥规则处理。

## 存档

存档 key 保持 `room_relic`。版本 2 保存两房间的状态、入口日期与永久赠礼 ID；每类首次必出也写抽取日期。版本 1 缺失的首次抽取日期保守迁移到该房间最后一次纸条判定日期，避免重启后同日额外抽取。旧版未保存的信息无法完全重建；特别是旧档连纸条判定日期也缺失时，不声称能恢复准确首次解锁时间。

`SaveCoordinator` 暂存读档时尚未注册的模块数据，通过注册表 `Registered` 事件补恢复。等待注册期间保存仍保留这些数据；加载其他有效存档时清除上一个存档的待恢复数据。恢复失败的原数据保留，避免下一次保存以默认值覆盖。无效或不支持的遗留物版本返回失败；成功恢复会通知两个房间刷新，但不播放新的获赠提示。

## Scene / Inspector 作者化

Apartment 的 `ArtGenerated/RoomRelic` 下有两个房间，每房间包含 3 个纸条候选槽、5 个遗物候选槽和 3 个赠礼槽。运行时只激活预置对象，所有 Sprite、Transform 与 UI 引用均来自已保存的 Scene。

每个赠礼 `RoomRelicView` 的 `_displaySlotId` 必须匹配 Catalog 中 `displaySlotId`。目前前两槽分别绑定 `desk` / `shelf`，第三槽保留为空。位置标识与数组排序解耦：先获得 shelf 赠礼，再获得 desk 赠礼，原赠礼不会移动。新增展示位置时须显式填写唯一槽位 ID，并预置相应物品变体。

纸条和遗物通过稳定哈希在候选槽中选择位置，同一物品在不同进程使用同一槽。

- `Tools/Gemini-Lab/Apartment/Author Room Relic`：仅补建缺失的房间/弹窗，保留已有房间与配置，不再删除重建现有视觉。新增 Catalog 才填默认内容。
- `Tools/Gemini-Lab/Apartment/Upgrade Room Relic Bindings`：在当前已打开的 Apartment 中，仅补齐空赠礼槽位标识、替换仍带“【占位】”前缀的纸条。保留已有文案、位置、Sprite、层级与引用；支持 Undo，不自动保存，核对后保存 Scene 和 Catalog。
- `Tools/Gemini-Lab/Room Relic Debug`：原有 Play/DevMode 调试入口，重置日判定并按现有概率抽取；不会清空永久赠礼收藏。

## 验证

回归测试位于 `Assets/_Project/Tests/EditMode/RoomRelicServiceTests.cs`、`RoomRelicIntegrationTests.cs` 和 `DeferredSaveRestoreTests.cs`，覆盖同日消费/恢复、版本 1 迁移、双房间解锁、服务暂停恢复、固定赠礼槽位、失败点击不消费和延迟读档。

本轮 Unity 编译完成；上述测试加原有 `SaveSystemTests` 合计 29 项 EditMode 全部通过（0 失败、0 跳过），包括弹窗保留空间页面的回归。Scene/Runtime 视觉契约通过。

2026-09-09 已在当前 Unity Play 中通过 EventSystem 射线与模拟左键事件验证视口点击纸条/遗物、读取完整文案、消费和关闭返回空间页面；Game View 截图确认文字可读、关闭按钮正常、赠礼图文无重叠。颜色、字号、关闭按钮位置和赠礼布局均已保存到 Apartment Scene。重新启用房间触发器后，由真实物理帧触发两房间首次遗物；好友度提高到 80 后两房间各获得一件赠礼。公寓 → 世界地图 → 公寓后进度保留，好友度降到 40 后永久赠礼仍在。独立测试目录中实际保存/读取 slot_3 后，赠礼恢复，已读纸条保持消耗状态。

编辑器曾持有旧 Catalog 内存对象，重新加载该资产后已确认域重载与再次 Play 都使用新文案。最后 Play Console 无 error。原存档已按测试前 SHA256 核对恢复，编辑器已退出 Play。证据保存在本机忽略目录 `Logs/RoomRelicPlayCheck/`；没有覆盖完整主菜单流程、独立构建或其他玩法。详细边界和现有情绪花编辑模式异常见 [人工验证清单](manual-validation-checklist.md)。

## 2026-09-09 点击修复

空槽位随 `RoomRelicView.Apply` 禁用 Collider，避免不可见槽位抢占点击。永久赠礼可重新打开查看，使用当前变体 ID 查找已放置赠礼，不消费、不重复获得奖励。Play 经 UI 射线与 PointerClick 验证 8 纸条、10 临时遗物、2 正式素材赠礼全部可打开。
