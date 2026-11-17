# Formation 棋盘专用原材质兼容层

本目录仅服务 `FormationBattle` 独立模式，原 LS-5 网格／贴图、BlackForest 原门及其他地图材质不修改。

`FormationBoardMaterialTools.EnsureImported()` 创建持久反射 Cubemap 与15份表面材质副本；`GetMaterial(sourceName,checker)` 查询，checker0为原色，1/2为0.93/1.10中性灰明暗格。`GetGateMaterial(source,red)` 在本目录创建原门材质副本，保留纹理、UV变换及原目标混合；门使用源乘2规则与红／蓝适配Tint。

棋盘Tint只在最终Surface光照结果乘一次，不同时乘在albedo，保证原地砖高光较强时格点仍有稳定明暗差异；外围checker0保持不变。

Surface读取原 `FormationNative` 漫反射、法线、金属度／光滑度、发光以及原UV2的RGBM图集，仅将图集用作可转移间接光。**不采原 Shadowmask，也不保留原走廊阴影位置**；新棋盘直接光／阴影来自当前场景方向光和实际新几何。场景工具提供源主光方向、`source.color.gamma`、强度3；Shader不再次转换该主光色。使用预览Gamma BRDF2兼容近似，并读取源反射Probe的mip5/3。原背景使用独立Unlit原贴图，不随棋盘材质升亮。

`ReflectionSource` 保留原 `ReflectionProbe-0` 的六面和八层已编码mip；Editor首次导入创建 `OriginalLS5Reflection.cubemap`，不重算mip。来源版本、对象ID、逐PNG SHA256与真实权利状态见 `source-reflection.json`；源包URL／校验沿用 `docs/art/source-records/formation-ls5-native.json`。RGBM解码系数5和Probe强度0.93沿用本轮模型预览；前者不是已恢复的原运行时HDR参数。

这是一套独立兼容着色器，**不是原游戏Shader二进制的移植或逐像素复刻证明**。主光Gamma转换、反射方向／强度、法线导入和棋盘重排后的现场阴影均需在Unity实际渲染和Player中核对。原图旧门图标与2026源贴图的警告图标版本差异继续存在，Tint仅适配颜色，不冒称恢复旧图标。门原本不投影／不接收地形阴影，继续保持透明标记行为；Surface具有真实Caster与Receiver。
