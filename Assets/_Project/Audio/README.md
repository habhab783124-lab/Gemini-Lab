# Audio/ — 音频资源

## 文件夹职责
存放 BGM、SFX、宠物语音等**自研或已授权**音频资源。

## 子目录规划

| 子目录 | 内容 |
| :--- | :--- |
| `BGM/` | 背景音乐（公寓白天 / 夜晚 / 旅行等氛围）。 |
| `SFX/UI/` | UI 音效（按钮、提示、打开面板）。 |
| `SFX/Pet/` | 宠物互动音效（脚步、吃食、睡眠呼吸）。 |
| `SFX/Furniture/` | 家具交互音效（键盘敲击、翻书、躺下）。 |
| `SFX/Travel/` | 旅行特殊音效（出发、回归、相册翻页）。 |
| `Voice/` | 宠物语音片段（如果有 TTS 缓存）。 |

## 依赖关系
- **被依赖**：Prefabs（AudioSource 引用）、Scripts/Modules（通过 `AudioService.PlayXxx` 播放）、UI（UI 音效）。
- **禁止**：音频资产引用脚本或场景。

## 代码规范/注意事项
1. 所有音频统一使用 `AudioService`（位于 Core）进行播放，不直接 `AudioSource.PlayOneShot`，便于音量总控与 Mixer 分组。
2. 格式要求：BGM → `.ogg` (Streaming)；SFX → `.wav` 或 `.ogg` (Decompress On Load)；移动端统一压缩至 Vorbis Q=0.5。
3. 命名：`{分类}_{场景或对象}_{动作}` —— 例 `SFX_Pet_Eat_01.ogg`。
4. 所有 AudioClip 必须分配到对应 `AudioMixerGroup`（BGM / SFX / Voice），便于运行时调节。
5. 音量目标：BGM -14 LUFS；SFX 峰值不高于 -3 dBFS；避免响度战争。
6. 语音缓存位置：运行期 TTS 结果写入 `Application.persistentDataPath/Voice/`，**不得**提交到仓库。
