# 音乐数据集说明

## 1. 数据集来源

当前项目的音乐数据集由项目内部种子程序维护，不依赖运行时从第三方音乐平台实时抓取。

- 种子文件：`src/MusicRec.Services.Catalog.Api/Data/CatalogSeedData.cs`
- 写入方式：`Catalog API` 启动时执行数据库迁移与 `SeedAsync`
- 目标数据库：`MusicRecCatalogDb`

项目启动后，前端首页、歌曲详情、我喜欢的音乐、歌单页以及推荐服务都统一读取这批落库后的歌曲数据。

## 2. 数据集类型

这是一个面向演示和课程项目的流行音乐样例数据集，包含两类内容：

- 真实流行歌曲元数据
  - 例如 `Blinding Lights`、`Levitating`、`Shape of You`、`Bad Guy`
- 原创示例曲目
  - 例如 `Midnight Metro`、`Paper Planets`、`Neon Tides`

每首歌曲包含以下核心字段：

- 歌曲名
- 歌手
- 专辑名
- 风格分类
- 封面图地址
- 音频试听地址
- 发行日期
- 时长
- 热度分值
- 冷启动候选标记
- 是否启用

## 3. 资源来源

### 3.1 封面图

- 来源：`https://coresg-normal.trae.ai/api/ide/v1/text_to_image`
- 形式：根据歌曲气质生成的方形专辑封面图
- 用途：用于首页推荐卡片、热门歌曲、详情抽屉、歌单列表、播放器缩略图展示

### 3.2 音频

- 来源：`https://samplelib.com`
- 形式：公开可访问的 MP3 试听片段
- 用途：用于项目中的试听播放，不代表完整正版音源库

## 4. 风格分布

当前种子数据覆盖以下音乐风格：

- `Pop`
- `Dance-pop`
- `Alternative`
- `Synth-pop`
- `Electropop`

这些风格标签同时用于：

- 冷启动偏好选择
- 用户喜欢/不喜欢反馈建模
- 推荐服务的偏好打分与召回解释

## 5. 当前有效歌曲数量

当前有效歌曲总数为 `19` 首。

这个数量指的是 `MusicRecCatalogDb` 中 `IsActive = true` 且能被前端正常访问的歌曲集合，已经通过运行中的曲库接口验证：

```powershell
$songs = Invoke-RestMethod -Uri 'http://localhost:5082/api/catalog/songs'
$songs.Count
```

返回结果：

```text
19
```

## 6. 数据维护规则

当前项目对歌曲数据采用“种子回填 + 无效旧数据停用”的维护方式：

- 若数据库中已存在同名同歌手歌曲，启动时会按种子数据执行更新
- 旧的占位封面、空音频地址等坏数据会被修正
- 不在当前精选种子集中的旧占位数据会被自动标记为 `IsActive = false`

因此，项目实际可见曲库会与 `CatalogSeedData.cs` 中维护的精选数据保持一致。
