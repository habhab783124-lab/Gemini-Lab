# 存档清理调试工具

入口：`Tools > Gemini-Lab > Save Data Debug`，窗口标题「存档调试」。工具位于 `Assets/_Project/Scripts/Editor/Persistence/SaveDataDebugWindow.cs`，仅编辑器可用。

## 使用

1. 退出 Play，等待编译和资源导入结束。
2. 打开工具，核对当前 `Application.persistentDataPath` 和文件预览。
3. 选择开发存档 `Saves-Dev` / 玩家存档 `Saves`、共用聊天记录、共用进度。默认选择当前 Dev Mode 对应的目录，并选中聊天和进度。
4. 点击「备份并清理所选存档」，确认后执行；下一次进入 Play 时使用新数据。
5. 仅验证序章时，可点击「仅重置序章播放标记」，保留所有文件存档。

公寓引导进度是存档 bundle 中的 `apartment_tutorial` 服务；清理相应目录的存档会一起重置。引导仍需进入空间系统页面、等待首次读档尝试结束才出现。工具不改变现有 Boot 活动场景判断和序章流程。

## 精确范围与备份

- 仅处理所选目录顶层的合法槽位 `*.sav`、`*.sav.tmp`、`*.sav.bak`，包括 autosave、手动槽位、furniture_layout；不遍历子目录，不处理未知扩展名。
- 聊天文件：根目录的 `chat_history.json`。
- 全局进度键：`HasPlayedPrologue`、`GeminiLab.Debug.ClockOffsetDays`、`GeminiLab.DailyReset.LastDate`、`geminilab.worldmap.wishes.v1`。音量、语言、窗口配置不清理；不调用 `PlayerPrefs.DeleteAll`。全局键和聊天会影响两种存档模式。
- 原文件移到同一存档根目录的 `SaveDebugBackups/<UTC时间>-<唯一标识>/`，保留原相对路径。manifest.json 记录来源、文件列表、进度键原值及是否存在。
- 清理中途失败会尝试恢复已移动文件和进度键；若文件被占用或同时被其他进程写入，可能无法完全回滚，窗口会报告备份位置。恢复时先退出 Play，按备份中的相对路径复制文件回原根目录；进度键原值见 manifest.json（工具不提供自动恢复按钮）。
- 不处理符号链接或目录联接。Windows 独立运行的游戏与编辑器可能共用该持久化目录，使用前请关闭运行中的独立游戏。

## 验证

Unity 编译完成，8 项专项 EditMode 测试全部通过：已知文件筛选、精确字节备份与无关数据保留、仅聊天清理、备份创建失败不动源文件、Windows 文件占用时回滚、重复清理保留旧备份、拒绝相对/磁盘根路径、限定进度键集合。菜单窗口已打开并确认存在。测试仅操作临时目录，未清理用户真实存档或修改真实 PlayerPrefs；真实确认按钮的清理操作留给使用者。当前 Console 原有 UPM 包解析中止记录不属于本工具新增错误。
