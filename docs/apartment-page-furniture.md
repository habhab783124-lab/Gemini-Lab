# 公寓页面入口家具

2026-09-09 已实现并保存到 Apartment_Main。

| 家具 | 位置 | 点击目标 | Prefab |
| --- | --- | --- | --- |
| 水晶球 | 天使房 (8.5, 2.3) | 塔罗 `PanelId.Tarot` | `Assets/_Project/Prefabs/Furniture/Leisure/CrystalBall.prefab` |
| 扭蛋机 | 恶魔房 (-11.2, -0.4) | 收藏 `PanelId.Collection` | `Assets/_Project/Prefabs/Furniture/Leisure/Gacha.prefab` |

使用现有 `Art/Sprites/Furniture/crystal_ball.png` 和 `gacha.png`，未修改素材。对应家具定义位于 `ScriptableObjects/FurnitureConfig/Leisure/CrystalBall.asset`、`Gacha.asset`。

`FurniturePageLink` 放在 HubUI 模块，通过既有 `IUIRouter` 打开页面，不直接依赖塔罗/收藏业务。`ApartmentViewportInputBridge._furniturePageLinks` 显式绑定两件家具。点击不要求宠物接近或选中；只在空间页面响应，尊重世界物体遮挡。建造模式先消费视口点击，避免误跳转页面。

两件家具均使用已保存 SpriteRenderer、SortingGroup、底部实体碰撞体、身体点击 Trigger 和 Furniture 组件。排序 150，位于遗留物的 200 下方，避免遮住对应槽位。自动宠物交互锚点不可用，页面入口不强行启动尚未配置的宠物家具动画。

首次作者化菜单：`Tools/Gemini-Lab/Apartment/Author Page Furniture`。已有实例、Prefab 和定义会保留，不覆盖手调摆放；重复执行仅补齐视口绑定。后续修改 Scene/Prefab 的 Transform、Collider 与 FurniturePageLink 目标页面即可。

验证：Unity 编译通过；Play 实际 UI Raycast → PointerClick 分别打开 Tarot 和 Collection；其他页面和建造模式不误跳转。20 个纸条/遗物/正式素材赠礼点击全部通过，10 条实际 Physics2D 家具路线全部到达且未越界。Game View 已检查两件家具显示，Play Console 0 error。退出 Play 后原存档已恢复并核对 SHA256，无新增存档。证据位于忽略目录 `Logs/FurniturePageLinks/`。未验证独立构建。
