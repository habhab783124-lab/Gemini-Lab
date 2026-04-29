# ApartmentScene_Extracted

这个目录最初用于存放从 `公寓场景.psd` 派生出来、准备用作普通独立资源的场景 Sprite。

当前状态：

- 第一轮真正用于家具接线的独立子资源，已经开始迁移到 `Assets/_Project/Art/Sprites/Furniture/` 下的分类子目录里维护
- 这里目前主要保留过渡说明，不再作为后续家具资源命名的主工作目录

当前命名原则：

- 要参与家具系统的资源，用 `家具_...`
- 纯背景/地板/墙面/组合层，用 `环境_...`
- 家具名里必须显式包含类别词：
  - `床`
  - `工作桌`
  - `休闲`
  - `竖琴`
  - `装饰`

原因：

- 当前代码会直接根据名称推断家具类别
- 如果只有位置描述名，后续交互接线正确率会明显下降

详细规则见：

- [docs/apartment-scene-sprite-naming-guide.md](/d:/unity/project/Gemini-Lab/docs/apartment-scene-sprite-naming-guide.md)
