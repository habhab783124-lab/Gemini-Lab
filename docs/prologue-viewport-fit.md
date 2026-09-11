# 序章窗口适配

2026-09-11：针对视频大小与窗口不匹配，修改 `Assets/_Project/Scenes/Intro/Prologue.unity` 的预置布局。

- 28 个源视频均为 1280×720，现有视频 RenderTexture 为 1920×1080。保留视频和资源引用。
- CanvasScaler 从固定像素改为 Scale With Screen Size，参考尺寸 1920×1080、Screen Match Mode = Expand。较小窗口按比例缩小图文。
- `Canvas/BackgroundVideo `（原节点名末尾有空格）新增 AspectRatioFitter：Fit In Parent，16:9；VideoPlayer 使用 Fit Inside。完整显示视频，非16:9窗口留边，不裁剪或拉伸。
- `Canvas/VideoLetterbox` 是视频下方预置的全屏黑色 Image，不拦截输入；避免附加 Boot 摄像机的蓝色背景影响留边。
- 第7、9、16段文字位置收进1920×1080参考画布的24像素安全边距，未修改文案、顺序、文本框大小或运行时逻辑。
- 所有适配组件和参数保存在场景中，可在 Inspector 调整；未新增运行时脚本。

## 验证

实际 Play 视频已准备并持续播放；1329×600、约1067×600（16:9）、960×600（16:10）三种 Game View 下，视频显示区分别约1066.67×600、1066.67×600、960×540，宽高比均为16:9，边界位于窗口内。

当前1329×600下，Scene与Play的视频矩形尺寸一致；20段文字框经静态边界检查，均处于参考画布安全范围。实际观看验证覆盖首段视频及首段文字，未完整播放全部剧情或执行独立构建。

截图和原场景、验证存档备份在本地忽略目录 `Logs/PrologueViewportFit/`。验证后恢复原存档；Game View恢复Free Aspect，退出Play。原有启动和序章进度/跳转问题不在本次修复范围。
