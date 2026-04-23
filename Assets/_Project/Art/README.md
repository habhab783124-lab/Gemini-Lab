# Art/ — 美术资源（2D）

## 文件夹职责
存放**所有自研 2D 美术资源**：Sprite、Sprite Atlas、2D 骨骼动画、PSB 分层源文件、Tilemap 用 Rule Tile、2D Shader、UI 图标。**第三方素材包禁止进入此目录**，应独立置于 `Assets/ThirdParty/`。

当前美术交付为**序列帧 + GIF**，本项目统一采用：
- **序列帧（PNG）作为唯一运行时资产**（打包、播放、碰撞、特效同步都基于序列帧）。
- **GIF 仅用于预览和评审**（用于需求确认，不进入运行时资源链路）。

## 子目录规划

| 子目录 | 内容 |
| :--- | :--- |
| `Sprites/Pet/` | 宠物 Sprite 序列 / 骨骼绑定 PSB 源文件。 |
| `Sprites/Furniture/` | 家具 Sprite（按品类子分目录）。 |
| `Sprites/Environment/` | 场景 Sprite（地板、墙体 Tile 源图、装饰物）。 |
| `Sprites/UI/` | UI Sprite 源图（会被打入 Sprite Atlas）。 |
| `Sprites/AnimFrames/` | 动效序列帧源图（按对象/动作拆分目录，运行时使用）。 |
| `References/GIF/` | GIF 预览图（仅评审用，不参与打包）。 |
| `Atlases/` | `SpriteAtlas` 资产（按模块拆分，例：`Atlas_Furniture.spriteatlas`）。 |
| `Tiles/` | `TileBase` 资产（`RuleTile`、`AnimatedTile`、`ScriptableTile`）。 |
| `Animations/` | `.anim` 剪辑 + `AnimatorController`（2D 帧动画 / 骨骼动画）。 |
| `Shaders/` | 自研 2D Shader（URP 2D Renderer 兼容；含描边、溶解、材质变色等）。 |
| `Icons/` | 家具缩略图、UI Icon。 |

## 依赖关系
- **被依赖**：Prefabs、Scenes、UI、ScriptableObjects（例如家具 Icon 字段）。
- **自身**：`SpriteAtlas` 聚合 `Sprites/**`；`AnimatorController` 引用 `AnimationClip` 与对应 Sprite；`RuleTile` 引用 `Sprite`。
- **GIF**：仅作视觉参考，不被 `Prefab / Scene / Controller / Addressables` 直接引用。
- **禁止**：Art 资产引用业务脚本（美术资产应脚本无关）。

## 序列帧 + GIF 最佳实践（本项目标准）
1. **统一方案**：使用序列帧制作 `AnimationClip`，由 `Animator` 播放；GIF 不直接用于运行时播放。
2. **原因**：
   - GIF 在 Unity 运行时生态中不稳定、解码成本高，且不利于精确控制关键帧事件。
   - 序列帧可直接走 Sprite Atlas 合批，性能更稳定，便于事件帧（音效、特效、判定）对齐。
   - 与现有 `Animations/`、`AnimatorController`、Prefab 工作流完全兼容，维护成本最低。
3. **GIF 使用边界**：
   - 保留在 `References/GIF/` 供策划/美术验收。
   - 不允许出现在运行时资源引用链路中。
   - 若 GIF 与序列帧不一致，以序列帧为准，GIF 仅用于“目标效果对照”。

## 代码规范/注意事项
1. **Sprite 导入**：
   - `Pixels Per Unit` 项目统一为 `100`（或按像素艺术需要统一 `16 / 32`，但全项目必须一致）。
   - `Filter Mode`: 像素风统一 `Point (no filter)`；插画风 `Bilinear`。
   - `Compression`: PC 默认 `BC7`；移动端 `ASTC 6x6`。
   - 序列帧 `Texture Type = Sprite (2D and UI)`，`Sprite Mode = Single`，命名连续编号（`_0001` 起）。
2. **Sprite Atlas 必装**：所有运行时 Sprite 必须被某个 `SpriteAtlas` 收录，否则批处理失败 Draw Call 暴涨。
3. **命名**：`{模块}_{对象}_{动作或编号}` —— 例：`Pet_Cat_Idle_01.png`、`Furniture_Sofa_Luxury.png`、`UI_Icon_Sofa.png`。
   - 序列帧推荐：`{对象}_{动作}_{方向}_{帧号4位}`，例：`Pet_Cat_Idle_Down_0001.png`。
4. **骨骼动画（2D Animation 包）**：使用 **PSD Importer** 导入 `.psb` 分层文件；骨骼绑定工作台在 `Sprite Editor → Skinning Editor`。
5. **Tilemap 资源**：
   - 每个房间风格一套 `RuleTile`，放置于 `Tiles/Apartment/`。
   - 墙体 `RuleTile` 需启用 `Collider Type = Grid`，供 `CompositeCollider2D` 合并使用。
6. **Animator Controller** 必须按宠物/家具分目录；参数命名遵循 `b_IsMoving`、`t_Trigger`、`f_Speed` 匈牙利前缀。
   - 序列帧动画帧率默认 `12 FPS`（角色可提升到 `15/24 FPS`，需在需求中注明）。
7. **Shader 必须兼容 URP 2D Renderer**；禁止遗留 Built-in Sprite Shader 引用。所有发光/扭曲效果用 2D Light 或 Shader Graph (2D target)。
8. 大文件（PSB、源 PNG > 5MB）必须通过 **Git LFS** 追踪；在 `.gitattributes` 中注册 `*.psb *.png *.psd filter=lfs`。
   - GIF 若仅作预览可不进 LFS；若单文件过大（>5MB）也应走 LFS。
9. 颜色值遵循项目色板（`_Project/ScriptableObjects/UIConfig/Palette.asset`），禁止 Sprite 颜色外挂未在色板定义的颜色。

## 交付检查清单（提测前）
- 序列帧已按对象/动作归档到 `Sprites/AnimFrames/`，且编号连续无缺帧。
- 对应 `.anim` 和 `AnimatorController` 已创建并在场景/Prefab 验证循环正常。
- 所有运行时帧图已进入正确 `SpriteAtlas`。
- `References/GIF/` 中存在对应预览 GIF（用于评审），但运行时无直接引用。
- 美术交付与程序接入在帧率、循环模式（Loop/Once）、首尾帧衔接上已对齐。
