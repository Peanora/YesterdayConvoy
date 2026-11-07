# 海嗣溟痕：原作素材与本作适配

2026-11-07，人类确认以单场溟痕蔓延替换旧跨关适应，并明确指定使用《明日方舟》原作溟痕美术直接接入。羁绊规则、实际试玩参数与验收结果以GDD、STATUS和QA本轮记录为准。本说明不把素材导入当作实机显示通过。

## 原素材身份与来源

使用已核对的官方Android资源版本 `26-10-29-15-36-11-33649006`。从 `battle/prefabs/[uc]tiles.ab` 的 `tile_creep` / `tile_creepf` 原始组件读取 `_creepDirectionEffects`，其引用的是 `map_028_juntan_range_[up/down/left/right]_[A/B]` 和 `map_028_juntan_range_B`，再追到 `battle/prefabs/effects/map.ab` 的实际材质及纹理。不是按文件名猜测一张泛用海洋或烟雾贴图。

源原包及所需纹理依赖均与该版本资源清单MD5比对；CAB、对象Path ID、原依赖和导出PNG的SHA256见[来源记录](source-records/marine-tide-original.json)。原包和外部研究脚本在项目外 `E:/珍贵资料/昨日圆车_美术预览/2026-11-07_海嗣溟痕-v1`，运行所需的8张PNG及必要`.meta`进入源码。没有把外部提取/渲染工具代码加入Unity。

## 运行资源契约

位置为 `Assets/Art/VFX/Marine/Resources/MarineTide/`，通过 `Resources.Load<Texture2D>("MarineTide/文件名")` 读取。PNG为UnityPy对原Texture2D解码后直接保存；未重绘、裁切、调色或缩放。

| 文件名 | 尺寸 | 原始用途 | 本作读取要求 |
|---|---:|---|---|
| Special_juntan_01 | 256×256 | B型溟痕深黑蓝孔洞/鳞片地表 | 原RGBA底层，sRGB、Clamp |
| Special_juntan_04 | 256×256 | B型溟痕扭曲层 | 原RGBA叠层，sRGB、Clamp |
| Special_juntan_05 | 256×256 | A型溟痕黏膜地表 | 原RGBA变化底层，sRGB、Clamp |
| Special_juntan_03 | 256×256 | 溟痕脉络溶解遮罩 | 线性数据；Alpha全255，不能单独当透明地面图 |
| flow_43_2 | 128×128 | 原材质UV扭曲输入 | 线性数据、Repeat |
| flow_57 | 128×128 | 原脉络流光输入 | 线性数据、Repeat |
| mask_30 | 256×256 | 原双溶解第一种遮罩 | 线性数据、Clamp |
| mask_99 | 128×128 | 原双溶解第二种遮罩 | 线性数据、Clamp |

合计381,201字节。所有纹理显式设为Texture2D、Bilinear、无mipmap；关闭AlphaIsTransparency，保留原透明像素RGB。实际Unity导入结果仍须由集成验证确认，不能只凭`.meta`声明。

## 原材质与适配边界

- 主层 `028_13` / `028_12` 使用 `Torappu/Particles/Dissolve/Dissolve AB Double`，主纹理为 `Special_juntan_01`，两种遮罩分别为 `mask_30` / `mask_99`。原TintColor约为 `(0.485, 0.771, 0.846, 0.509)`。
- 扭曲层 `028_08` 使用 `Torappu/Particles/Disturb/Disturb Origin`，主纹理为 `Special_juntan_04`、扭曲纹理为 `flow_43_2`，原UV扭曲强度约为 `(0.022, 0.030)`，MainColor约为 `(0.485, 0.771, 0.846, 0.759)`，原混合为SrcAlpha / OneMinusSrcAlpha、双面。
- 脉络层 `028_01` 使用 `Torappu/Particles/Dissolve/Dissolve Add UVTween`，流光为 `flow_57`、溶解遮罩为 `Special_juntan_03`。原UVTween为 `(0, -0.3)`，TintColor约为 `(0.471, 0.715, 1, 0.953)`。

本作使用兼容Windows的Shader和真实模拟时钟呈现逐格生长、流动、休眠与检查反馈。原图案来源于原作，羁绊规则和反馈编排属于本作适配；没有宣称完整重建原客户端Shader、原网格、粒子调度或全部方向起落动画。强弱、覆盖边界和休眠状态必须从真实模型读取，不能只播放一张与战斗无关的贴图。神经损伤条、爆发与部署反馈由本轮表现代码接线，实际状态见QA。

## 权利与交付

素材为《明日方舟》原权利人的专有游戏媒体，不是CC0；具体制作人员和公开再分发许可未确认。用户指定使用并要求随源码上传，不代表取得权利人许可。保留来源与该真实权利状态；本轮运行PNG及meta随功能分支提交，原始包、缓存、项目外研究文件与本机构建不纳入Git。

## 验证范围

已完成：原Prefab引用追踪、官方清单MD5比对、原材质属性读取、PNG尺寸/Alpha范围核对、四张原溟痕图实际目视检查、项目内PNG与项目外导出逐字节比对。

导入时尚未执行Unity资源实际类型检查、GPU相机画面或独立Player验收；最终由本轮集成在QA记录。不以本说明替代这些运行结果。
