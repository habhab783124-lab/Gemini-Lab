# 公寓室内小门交流

2026-09-09 已实现并保存到 `Apartment_Main`。本页描述当前代码与场景，不是后续规划。

## 操作

- 点击中间小门切换开关：关闭显示原始带锁 Sprite，打开显示场景作者化的蓝灰竖条。
- 点击宠物身体选中，头顶显示原有金色箭头；WASD / 方向键移动，点击空地取消选中。
- 选中宠物距离任一侧 `Approach_*_门边` 不超过 2.2 世界单位时，开门后按 F 交流。WASD 可以穿过打开的小门进入另一间房；门上下的隔墙始终阻挡。关门后宠物留在当前房间，门口脚底被占用时拒绝关门并提示。
- 交流以两只宠物各自头顶的气泡展示：发起句立即出现，2.5 秒后另一只宠物回应；不添加说话人姓名、话题标题或中央数值面板。回应出现后双方气泡保留 8 秒自动收起，F / Esc 可提前收起。关闭不再次结算，也不会残留延迟回应；两次发起至少间隔 2 秒。切换页面、关闭门或停用门组件时清理气泡。
- 门开关是场景运行状态，下次进入默认使用 Scene 中配置的状态。既有亲密度与宠物数值继续由原持久化服务保存。

## 规则

先检查被交流者：Energy < 30 或 Mood < 30 为 NEED_SPACE，优先于亲密度；否则 Friendship >= 60 为 WARM，其余 NORMAL。

| 回应 | 双方各自精力变化 | 双方各自心情变化 | 亲密度基础变化 |
| --- | ---: | ---: | ---: |
| WARM | -2 | +3 | +2 |
| NORMAL | -1 | +1 | +1 |
| NEED_SPACE | 0 | 0 | 0 |

本次图片数值表覆盖旧的 NORMAL E-2/M+2 和 NEED_SPACE E-1/M-1。保留已有规则：发起者精力不足 10 不发起；获得亲密度后 300 秒内再次交流不加亲密度，心情/精力照常变化。变化继续由现有状态栏体现，气泡只显示宠物说的话。

`IndoorDoorDialogues.asset` 保存图片提供的双向各 5 个话题、10 句发起内容和 30 句回应；按发起者独立轮换，循环完五个再回到第一个。文案可直接在 Inspector 编辑，运行时只读取资产，不回写游标。

## 实现与作者化

- `Modules/HubUI/ApartmentDoorInteraction.cs`：门状态、距离、场景页面检查、F 优先消费、气泡时序与取消。
- `Modules/HubUI/PetDialogueBubble.cs`：将显式绑定宠物的头顶投影至公寓视口，随移动跟随、避免镜像翻字，并限制在视口范围内；根据两只宠物相对位置向两侧展开。
- `Modules/Pet/Social/PetSocialService.cs`：统一判定和结算；`IndoorDoorDialogueCatalog.cs`：可编辑对话数据。
- `PetClickReactionController` 新增显式 `_selectionArea` 与 `_socializeOnClick`：公寓选中不触发双宠交流；世界地图保持原有配置。
- 宠物下 `SelectionArea` 是只供点击的触发器，脚底 Capsule 仍负责物理碰撞。脚底使用 `Settings/IndoorDoor/PetSlide.physicsMaterial2D`，摩擦与弹性为 0；刚体 drag 为 0，导航 skin 从 0.04 调为 0.02。移动仍使用原先的原始键盘输入和速度 10。
- 复用 `ControlIndicator` 的既有 Sprite 与 `SpaceSysPanelStub` 绑定，修正页面开关和无选中时的缓存刷新。
- `ApartmentViewportInputBridge` 显式绑定小门、视口 RawImage 与相机。F 优先处理小门，避免同帧进入旧门边家具动画。
- 门下 `OpenDoor/Stripe0..4` 使用保存的 Mesh/材质，底部操作提示保留在 `ApartmentViewportHost/IndoorDoorUI`；旧 `Conversation` 面板已移除。两个气泡节点保存为 `ApartmentViewportImage/AngelDialogueBubble`、`DevilDialogueBubble`，字体、背景和尾巴均为显式引用，没有运行时生成最终视觉。生成工具尝试的去锁位图含格子背景，未用于工程；最终使用原生几何形状。
- 编辑器菜单 `Tools/Gemini-Lab/Apartment/Author Indoor Door` 负责首次作者化；检测到已绑定的小门会停止，避免覆盖手调内容。`Author Door Speech Bubbles` 菜单可升级旧面板，已有气泡绑定不会被覆盖。调整现有门、话题、UI、接近点请使用 Scene / Inspector。气泡 `Content` 默认为关闭；启用可在 Scene 预览，检查完关闭并保存。气泡使用非射线目标的 UI，不拦截家具、宠物或遗留物点击。交流期间普通点击反应气泡暂时隐藏，选中操作照常生效。

## 验证

- Unity 编译通过；38 项 EditMode 测试通过，覆盖社交数值、阈值边界、关闭/距离限制、双向发起、身体点击、话题循环、路径和遗留物集成。
- Play 中模拟实际视口 PointerEventData 点击开门，使用 F 的优先处理入口运行十个话题 × 三种回应，30/30 文案与双方数值正确。
- Play 身体点击测试：脚底未命中时仍成功选中，精力未变化；页面切换后双方箭头隐藏，隐藏页面的交流被阻止。
- Play 实际 Physics2D 步进复验 10 条家具路线：10/10 到达，脚底全程在各自房间内。独立点击触发器没有影响导航。
- 历史窗口版本的长回应换行与结果行未溢出；Game View 已检查开门、金色箭头和对话展示。最终 Play Console 0 error。
- Unity 测试恢复曾产生两个额外根节点宠物副本；已移除这次测试副本并确认只保留原层级两只宠物和有效门绑定。这是既有编辑器恢复问题，未修改相关 Bootstrap。
- 本地验证证据在忽略目录 `Logs/IndoorDoor/`。未做独立构建、操作系统真实按键手工测试或长时间压力测试。

测试结束已退出 Play，Scene 无未保存改动，保留两只原层级宠物。原存档已恢复，全部 SHA256 与本次测试前一致，无新增存档文件。

## 2026-09-09 遗留物点击与跨房间修复

- `RoomRelicView.Apply` 同步启停当前槽位碰撞体，空的高排序槽位不再遮挡可见物品。永久赠礼按当前变体 ID 打开已有赠礼面板，不消耗或重复发奖。
- `ApartmentPetMovement._connectedRoom` 显式连接另一房间，合并外侧边界并查询两边家具。门下新增 `DividerUpper`、`DividerLower` 和 `PassageBlocker`；前两段永久阻挡，后者随开关切换。原门碰撞体改为只供点击的 Trigger。
- 新菜单 `Tools/Gemini-Lab/Apartment/Upgrade Door Passage` 绑定连通区域与门洞；已有墙形状保留可手调。小门交流范围支持两侧，串门后不受出生房间限制。
- Play 真实 UI Raycast + PointerClick 路径验证 20/20 可见内容：8 张纸条、10 个临时遗物、2 个已有正式素材的永久赠礼。两只宠物的实际 Physics2D 步进均通过开门跨越、关闭阻挡、门外墙面阻挡、访客不瞬移回家、占用门口拒绝关闭；关门后正常帧观察仍留在对面房间。证据 `Logs/DoorPassageFix/`。

最终 43 项 EditMode 全部通过。原存档哈希已恢复一致；场景已保存，Unity 已退出 Play。测试恢复后记录到既有 `EmotionGardenService` 在 EditMode 调用 DontDestroyOnLoad 的 Console 异常，未涉及本次门和遗留物代码。

## 2026-09-09 改为宠物气泡

中央问答窗口已替换为两只宠物各自的气泡，发起句与回应依次展示。`PetController.VisiblePosition` 提供当前实际显示位置，兼容睡觉等独立家具姿态。原有问答内容、回应判定与数值结算保持一致。

最终验证：52 项 EditMode 全通过；Play 30 组问答无文案或溢出错误；跟随、非镜像显示、独立姿态、停用/切页清理及两件页面家具实际 UI 点击通过。Game View 已检查先发起和双气泡回应；Play Console 0 error。测试结束退出 Play 并保存 Scene，2.5 秒回应延迟与 8 秒阅读时间保持默认值，两个 Content 关闭，无旧 Conversation 节点，原存档哈希完全恢复。证据在 `Logs/DoorBubbles/`。

## 控制箭头遮挡气泡修复

普通点击反应气泡此前使用宠物排序 +250（约 1250），低于控制箭头 9999，已在 Game View 复现文字被箭头遮住。`PetClickReactionController._controlIndicator` 显式绑定本宠物箭头，气泡所有渲染层均排序在标识上方；无绑定时保留原行为。两只公寓宠物的绑定已保存，首次小门作者化也会绑定。Play 双宠实际身体点击、全部气泡渲染层排序、气泡过期与选中箭头继续显示均通过，Console 0 error；前后截图和检查记录在 `Logs/BubbleIndicatorFix/`。本次没有重跑无关测试或独立构建。
