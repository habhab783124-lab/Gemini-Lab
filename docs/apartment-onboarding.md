# 公寓家具悬停与新手引导

更新：2026-09-11。以下为当前工程已经整合的行为。

## 使用入口

打开 `Apartment_Main`，进入 Play 后点击「空间系统」。首次进入时显示六页引导；可翻页、跳过或完成，右下角「新手引导」可以重看。引导关闭后，将鼠标放在家具上即可看到金色描边与底部文字，移开后收起。

本功能基于 main 增量扩展已有家具反馈。引导面板、命中区域及反馈均已预置在 Apartment_Main 中。

## 实现与作者化

- `ApartmentViewportInputBridge` 接收 uGUI 的指针进入、移动与离开事件，将视口坐标转换为公寓世界坐标。点击继续使用原门、宠物、家具和页面入口链路。
- `ApartmentFurnitureSelectionPresenter` 显式绑定 38 件家具的目标、SpriteRenderer、描边节点与描述。按可见 Sprite 网格命中，避免只能命中家具脚底的移动碰撞体。按渲染排序选择前景对象，UI 遮挡、失焦、页面停用及建造模式下清除反馈。
- `Furniture/ApartmentFurnitureSelection` 保存反馈组件，家具下面的 `FurnitureSelectionHighlight` 保存 Sprite 和描边材质；`ApartmentViewportHost/FurnitureSelectionMessage` 保存提示框布局。
- `ApartmentOnboarding` 保存六页引导、翻页与跳过按钮、遮罩和重新打开入口。每页图文、尺寸、资源引用均保存在 Scene 中，可通过 Inspector 调整。运行时只切换预置状态、填充文字和管理输入阻挡。
- `ApartmentTutorialProgress` 使用现有持久化注册表和存档协调器，存档 key 为 `apartment_tutorial`，区分跳过与完成。旧存档无字段时视为未看过；重看后跳过不会取消已完成状态。
- 引导复用 main 已有的 `AutoSaveManager.InitialLoadCompleted`，等待读档完成再决定是否显示。
- `GameplayInputBlock` 为引导持有独立的输入阻挡令牌，覆盖宠物移动、家具互动、视口点击、小门 Update 和建造快捷键。关闭后恢复，并阻挡关闭当帧的输入穿透。

作者化菜单：`Tools/Gemini-Lab/Apartment/Author Onboarding`。已有完整引导时保留场景布局；不要通过重建场景调整 UI。`Setup Furniture Selection` 仅负责家具反馈节点。原家具互动绑定不作为本次作者化的副作用重写。

新增美术位于 `Assets/_Project/Art/UI/ApartmentOnboarding/`：Panel 为上一轮参考项目风格生成的奶油金色纸张面板；左右箭头复用 `Art/新手引导/left.png` 和 `right.png`，未复制重复 GUID 资源。插图复用现有家具、宠物、交流和遗留物图片。字体加粗与深棕色也已作者化。

## 验证范围与结果

功能先整合到用户实际打开的工程完成 Unity/Play 验证，再移植到此 PR 分支。PR 分支保留 main 上其他模块，以当前工程作者化的反馈/引导节点替换旧反馈节点，复用 main 已有的 Prefab 引用。两种验证范围分别记录：

- PR 分支：28 个程序集使用 Unity 2022.3.62f3 自带 Roslyn 和缓存依赖编译通过；范围闸门、视觉契约、Scene 引用及重复 Prefab 引用检查通过。独立工作区完整 Unity 导入与 Play 仍受既有 UPM 启动问题阻碍，以下运行结果来自实际使用工程，不冒充 PR 分支运行结果。

- 实际使用工程的 Unity 2022.3.62f3 编译通过；功能 Play 验证期间 Console 无 error。收尾重载场景时再次触发此前已知的 `EmotionGardenService` 在 EditMode 调用 `DontDestroyOnLoad` 异常，该模块未在本次修改范围内。
- 38 项 EditMode 测试通过：10 项悬停/引导测试及 28 项现有室内小门测试。测试覆盖视觉网格超出脚底碰撞体、前后遮挡、移开清除、分页边界、输入令牌、存档往返与旧槽位兼容。
- 在当前工程实际 Play 中，通过模拟 uGUI 指针事件、真实视口坐标换算与 UI Raycast，38/38 件家具均能触发描边；离开视口清除。竖琴截图同时确认描边和提示文字。
- Play 验证六页一次只显示一页、完成关闭、重看回到第一页、引导阻挡手动输入并清除悬停、真实组件停用释放阻挡、建造模式清除与恢复。
- Play 点击扭蛋机仍打开 Collection，点击水晶球仍打开 Tarot。完成后重新进入 Play，进度已恢复且不自动重复引导。
- 场景原有对象无丢失；已有 SpriteRenderer、碰撞体、相机、Animator 的序列化内容无改动。任务闸门及 Scene/运行时视觉契约通过。
- 测试后退出 Play，三份用户数据文件恢复到测试前 SHA256 一致。Packages、ProjectSettings 及任务外本地改动保留。

证据位于本地忽略目录 `Logs/HoverIntegration/`：`test-results.json`、`hover-sweep.json`、`scene-preservation.json`、`play-hover-final.png`、`play-tutorial-page4-final.png`。本次验证窗口为 1329×600；未做独立构建、多分辨率或真实硬件鼠标的自动化测试。

## 描边可见度调整（2026-09-11）

按反馈将家具专用材质 `FurnitureSelectionOutline.mat` 的 `_OutlineThickness` 从 1.5 提高至 4，`_OutlineColor` 调整为 (1, 0.94, 0.28, 1)。38 件家具共享该保存材质，Scene/Play 一致；未修改共享 Shader。已检查吉他的实际渲染描边，临时预览状态已恢复，场景未产生修改。截图：`Logs/HoverOutline/outline-strong.png`。

## 审查截图

以下截图来自用户实际使用工程，展示相同功能资源的效果，不作为 PR 分支已运行 Play 的证据。

![六页引导中的家具说明](images/apartment-onboarding-page4.png)

![加粗后的吉他描边（编辑器预览）](images/apartment-hover-outline.png)
