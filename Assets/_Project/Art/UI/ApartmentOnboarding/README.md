# Apartment Onboarding UI

2026-09-11：Panel.png 为 imagegen 生成的无字奶油纸面/金色边框，参考已有室内引导；最终版本已移除棋盘格背景，采用全幅不透明纸面。

- Unity Sprite Single；180 px 九宫格边界。
- 大引导面板使用 Image Sliced；家具提示卡 pixelsPerUnitMultiplier=4，按钮=6。
- 文案由 TMP 独立排版。左右翻页按钮复用 Art/新手引导；人物、状态、交流、纸条与千纸鹤复用项目原图。
- 正式引用位于 Apartment_Main 场景；修改不会依赖运行时替换 Sprite。
