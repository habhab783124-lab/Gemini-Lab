# Sprites/Furniture/ — 家具美术资源规范（统一技术栈）

## 目标
本目录用于存放家具相关的 2D 美术资源，并与 `Assets/_Project/Art/README.md` 保持一致：
- **序列帧（PNG）是唯一运行时动画资源**。
- **GIF 仅用于预览和评审，不参与运行时引用**。

## 目录建议
- `Bed/`：床类家具静态图、序列帧。
- `Decoration/`：装饰类家具静态图、序列帧。
- `Leisure/`：休闲类家具静态图、序列帧。
- `WorkDesk/`：工作台/桌类家具静态图、序列帧。
- `_PreviewGIF/`：家具预览 GIF（仅评审，不参与打包）。

## 命名规范
1. 静态图：`Furniture_{对象名}_{风格或变体}.png`
   - 示例：`Furniture_Sofa_Modern.png`
2. 序列帧：`Furniture_{对象名}_{动作}_{方向}_{帧号4位}.png`
   - 示例：`Furniture_Lamp_Blink_Front_0001.png`
3. 预览 GIF：`Furniture_{对象名}_{动作}_Preview.gif`

## 导入与播放规范
1. 序列帧导入参数：
   - `Texture Type = Sprite (2D and UI)`
   - `Sprite Mode = Single`
   - `Pixels Per Unit = 100`（若像素风项目统一改为 `16/32`，必须全项目一致）
   - `Filter Mode`：像素风 `Point (no filter)`；插画风 `Bilinear`
2. 动画制作：
   - 使用序列帧生成 `.anim`，由 `AnimatorController` 播放。
   - 默认帧率 `12 FPS`，特殊家具动效可用 `15/24 FPS`（需标注需求）。
3. 性能要求：
   - 运行时序列帧必须进入对应 `SpriteAtlas`，避免 Draw Call 激增。
   - GIF 严禁被 Prefab/Scene/Controller 直接引用。

## 交付清单（家具资源）
- 序列帧目录完整，编号连续、无缺帧。
- 对应 `.anim` 与 `AnimatorController` 已建立并可正常循环/触发。
- 运行时帧图已纳入 `SpriteAtlas`。
- 预览 GIF 已放入 `_PreviewGIF/`，仅用于评审。
