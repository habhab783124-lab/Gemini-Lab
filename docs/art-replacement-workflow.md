# Gemini-Lab 美术替换工作流

Updated: 2026-04-21

## 文档目标
这份文档约束 Gemini-Lab 后续进行 Sprite、Tile、动画、UI 图标等视觉资源替换时的标准流程，目标是：
- 不把资源放乱
- 不打断现有引用
- 不让场景、Prefab、SO 和 Atlas 失去同步

## 基本原则
1. 自研美术资源进入 `Assets/_Project/Art/`。
2. 第三方素材包不要混进 `_Project/Art/`，应放到 `Assets/ThirdParty/`。
3. 替换资源时，不只替图片本身，还要同步检查：
   - Prefab
   - Animator
   - SpriteAtlas
   - RuleTile
   - ScriptableObject 引用
4. 任何替换都要让后续场景作者和美术继续看得懂。

## 当前资源组织约定
推荐目录如下：
- `Assets/_Project/Art/Sprites/Pet/`
- `Assets/_Project/Art/Sprites/Furniture/`
- `Assets/_Project/Art/Sprites/Environment/`
- `Assets/_Project/Art/Sprites/UI/`
- `Assets/_Project/Art/Atlases/`
- `Assets/_Project/Art/Tiles/`
- `Assets/_Project/Art/Animations/`
- `Assets/_Project/Art/Shaders/`
- `Assets/_Project/Art/Icons/`

## 标准替换流程

### 1. 先判断替换对象属于哪一类
- 宠物立绘 / 动画
- 家具 Sprite
- 环境 Tile / RuleTile
- UI 图标 / 面板贴图
- Shader 或特效相关资源

### 2. 把资源放进正确目录
- 宠物资源放入 `Sprites/Pet/`
- 家具资源放入 `Sprites/Furniture/`
- 环境资源放入 `Sprites/Environment/` 或 `Tiles/`
- UI 资源放入 `Sprites/UI/` 或 `Icons/`

### 3. 统一命名
命名格式优先遵循：
- `{模块}_{对象}_{动作或编号}`

示例：
- `Pet_Cat_Idle_01.png`
- `Furniture_Sofa_Luxury.png`
- `UI_Icon_Travel.png`

### 4. 配置导入设置
至少确认以下内容：
- `Pixels Per Unit`
- `Filter Mode`
- `Compression`
- `Sprite Mode`
- Pivot
- Packing Tag 或 Atlas 收录方式

不要让同一类资源出现完全不同的导入标准。

### 5. 同步更新下游引用
按资源类型检查：

#### 宠物资源
- Animator Clip
- Animator Controller
- 宠物 Prefab
- 宠物 Icon 或展示图

#### 家具资源
- 家具 Prefab
- `FurnitureDefinitionSO`
- 家具缩略图 Icon
- 交互锚点预览与排序效果

#### 环境资源
- RuleTile
- Tile Palette
- 场景 Tilemap
- Collider 与遮挡表现

#### UI 资源
- SpriteAtlas
- 面板 Prefab
- Widget Prefab
- 多分辨率下的显示效果

### 6. 回到场景里验证
至少验证：
- 有没有丢图
- 有没有 Sprite 尺寸不对
- 有没有 Sorting Layer / Sorting Order 错误
- 有没有 Tile 对齐错误
- 有没有动画帧错乱
- 有没有引用丢失

### 7. 回写文档与验证结果
如果替换影响了结构、命名或资源路径，要同步更新：
- `docs/project-structure-overview.md`
- `docs/manual-validation-checklist.md`
- 相关模块 README 或文件指南

## 按资源类型的额外注意事项

### 宠物资源
- 宠物通常会关联动画、碰撞体、排序组和状态机表现，所以替换不能只换一张图。
- 如果资源尺寸发生明显变化，要重新检查：
  - 碰撞体范围
  - 动画位移
  - 与家具的遮挡关系

### 家具资源
- 家具不只是装饰，还是 Buff 和交互节点的一部分。
- 替换家具资源后，要检查：
  - 占用格子尺寸
  - 贴墙 / 地板放置类型
  - 与导航障碍的关系

### 环境资源
- 环境资源通常会影响导航与碰撞。
- 替换 Tile 或墙体资源时，要重新检查：
  - `RuleTile`
  - `CompositeCollider2D`
  - 走位空间

### UI 资源
- UI 图标和贴图需要检查 Atlas 是否同步。
- 不能只在一个 Prefab 里临时替掉，忘了更新公共 Widget 或通用样式。

## 大文件与版本管理
- 大图、PSB、PSD、超大 PNG 走 Git LFS。
- 不把运行期缓存、导出中间件、临时文件提交进仓库。

## 当前项目特别需要注意的点
1. 这个仓库目前还处于骨架阶段，很多资源目录还没有真实资产，因此替换流程会经常伴随“第一次落资源”。
2. 文档里规划了不少 Prefab、SO、Tile 和动画结构，但它们现在还没有全部实现，替换前要先确认引用对象是否已经存在。
3. 如果替换导致资源结构第一次成型，要顺手把文件指南和结构总览一起更新。
