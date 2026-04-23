# Sprites/Pet/ — 宠物美术资源规范（统一技术栈）

## 目标
本目录用于存放宠物相关 2D 美术资源，并与 `Assets/_Project/Art/README.md` 保持一致：
- **序列帧（PNG）是唯一运行时动画资源**。
- **GIF 仅用于预览和评审，不参与运行时引用**。

## 目录建议
- `{PetName}/Frames/Idle/`：待机序列帧。
- `{PetName}/Frames/Move/`：移动序列帧。
- `{PetName}/Frames/Interact/`：交互序列帧（如吃饭、玩耍）。
- `{PetName}/Frames/Emotion/`：情绪序列帧（如开心、困倦）。
- `{PetName}/PreviewGIF/`：宠物预览 GIF（仅评审，不参与打包）。

## 命名规范
1. 序列帧：`Pet_{宠物名}_{状态}_{方向}_{帧号4位}.png`
   - 示例：`Pet_Cat_Idle_Down_0001.png`
2. 预览 GIF：`Pet_{宠物名}_{状态}_Preview.gif`
   - 示例：`Pet_Cat_Idle_Preview.gif`

## 导入与播放规范
1. 序列帧导入参数：
   - `Texture Type = Sprite (2D and UI)`
   - `Sprite Mode = Single`
   - `Pixels Per Unit = 100`（若像素风项目统一改为 `16/32`，必须全项目一致）
   - `Filter Mode`：像素风 `Point (no filter)`；插画风 `Bilinear`
2. 动画制作：
   - 使用序列帧生成 `.anim`，由 `AnimatorController` 播放。
   - 默认帧率建议：`Idle = 8~12 FPS`，`Move = 12~15 FPS`，`Interact/Emotion = 12~24 FPS`。
3. 状态机约定（建议）：
   - 最少包含 `Idle`、`Move`、`Interact` 三类状态。
   - 参数命名遵循项目规范：`b_IsMoving`、`t_Trigger`、`f_Speed`。
4. 性能要求：
   - 运行时序列帧必须进入对应 `SpriteAtlas`，避免 Draw Call 激增。
   - GIF 严禁被 Prefab/Scene/Controller/Addressables 直接引用。

## 交付清单（宠物资源）
- 序列帧目录完整，编号连续、无缺帧。
- 各状态动画时长与帧率已与策划需求对齐。
- 对应 `.anim` 与 `AnimatorController` 已建立并在场景/Prefab 验证可播放。
- 运行时帧图已纳入 `SpriteAtlas`。
- 预览 GIF 已放入 `PreviewGIF/`，仅用于评审。
