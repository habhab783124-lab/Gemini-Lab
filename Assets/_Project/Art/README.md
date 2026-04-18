# Art/ — 美术资源（2D）

## 文件夹职责
存放**所有自研 2D 美术资源**：Sprite、Sprite Atlas、2D 骨骼动画、PSB 分层源文件、Tilemap 用 Rule Tile、2D Shader、UI 图标。**第三方素材包禁止进入此目录**，应独立置于 `Assets/ThirdParty/`。

## 子目录规划

| 子目录 | 内容 |
| :--- | :--- |
| `Sprites/Pet/` | 宠物 Sprite 序列 / 骨骼绑定 PSB 源文件。 |
| `Sprites/Furniture/` | 家具 Sprite（按品类子分目录）。 |
| `Sprites/Environment/` | 场景 Sprite（地板、墙体 Tile 源图、装饰物）。 |
| `Sprites/UI/` | UI Sprite 源图（会被打入 Sprite Atlas）。 |
| `Atlases/` | `SpriteAtlas` 资产（按模块拆分，例：`Atlas_Furniture.spriteatlas`）。 |
| `Tiles/` | `TileBase` 资产（`RuleTile`、`AnimatedTile`、`ScriptableTile`）。 |
| `Animations/` | `.anim` 剪辑 + `AnimatorController`（2D 帧动画 / 骨骼动画）。 |
| `Shaders/` | 自研 2D Shader（URP 2D Renderer 兼容；含描边、溶解、材质变色等）。 |
| `Icons/` | 家具缩略图、UI Icon。 |

## 依赖关系
- **被依赖**：Prefabs、Scenes、UI、ScriptableObjects（例如家具 Icon 字段）。
- **自身**：`SpriteAtlas` 聚合 `Sprites/**`；`AnimatorController` 引用 `AnimationClip` 与对应 Sprite；`RuleTile` 引用 `Sprite`。
- **禁止**：Art 资产引用业务脚本（美术资产应脚本无关）。

## 代码规范/注意事项
1. **Sprite 导入**：
   - `Pixels Per Unit` 项目统一为 `100`（或按像素艺术需要统一 `16 / 32`，但全项目必须一致）。
   - `Filter Mode`: 像素风统一 `Point (no filter)`；插画风 `Bilinear`。
   - `Compression`: PC 默认 `BC7`；移动端 `ASTC 6x6`。
2. **Sprite Atlas 必装**：所有运行时 Sprite 必须被某个 `SpriteAtlas` 收录，否则批处理失败 Draw Call 暴涨。
3. **命名**：`{模块}_{对象}_{动作或编号}` —— 例：`Pet_Cat_Idle_01.png`、`Furniture_Sofa_Luxury.png`、`UI_Icon_Sofa.png`。
4. **骨骼动画（2D Animation 包）**：使用 **PSD Importer** 导入 `.psb` 分层文件；骨骼绑定工作台在 `Sprite Editor → Skinning Editor`。
5. **Tilemap 资源**：
   - 每个房间风格一套 `RuleTile`，放置于 `Tiles/Apartment/`。
   - 墙体 `RuleTile` 需启用 `Collider Type = Grid`，供 `CompositeCollider2D` 合并使用。
6. **Animator Controller** 必须按宠物/家具分目录；参数命名遵循 `b_IsMoving`、`t_Trigger`、`f_Speed` 匈牙利前缀。
7. **Shader 必须兼容 URP 2D Renderer**；禁止遗留 Built-in Sprite Shader 引用。所有发光/扭曲效果用 2D Light 或 Shader Graph (2D target)。
8. 大文件（PSB、源 PNG > 5MB）必须通过 **Git LFS** 追踪；在 `.gitattributes` 中注册 `*.psb *.png *.psd filter=lfs`。
9. 颜色值遵循项目色板（`_Project/ScriptableObjects/UIConfig/Palette.asset`），禁止 Sprite 颜色外挂未在色板定义的颜色。
