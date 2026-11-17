# Formation 棋盘专用原材质兼容层

本目录仅服务 `FormationBattle` 独立模式，原 LS-5 网格／贴图、BlackForest 原门及其他地图材质不修改。

`FormationBoardMaterialTools.EnsureImported()` 创建持久反射 Cubemap 与表面材质副本；`GetMaterial(sourceName,checker)` 查询。checker0仍为原工业外围；checker1/2为同一族哑光细颗粒混凝土，只用0.99/1.01自然变化，不再做强烈明暗棋盘。`GetGateMaterial(source,red)` 在本目录创建原门材质副本，保留纹理、UV变换及原目标混合；门使用源乘2规则与红／蓝适配Tint。

中央混凝土使用独立 `_ConcreteMode` 分支，世界XZ采样 `Assets/Art/Environment/TacticalV2/city-concrete.png`，保留现场方向阴影，采用中性漫反射、细颗粒与轻微边缘磨损。此分支不采旧atlas间接光／原反射Cube，不做镜面或发光，因此不会带入源路面白短线与米金反光。竖向台面侧壁采用对应立面投影。外围checker0仍走原UV／BRDF分支。

`GetConcreteMaterial(raised=false)` 创建持久 `ConcreteFloor.mat`，raised=true取略深色 `ConcreteRaised.mat`；`GetTrimMaterial()` 创建深灰低光 `ConcreteIndustrialTrim.mat`。所有输出仅在本目录 `Materials`；这些API不创建／修改场景或Mesh。实际相机首屏调到 `_ConcreteTextureScale=1.05`、中性ambient=.70、direct=.32，细缝／磨边分别覆盖约.012–.023／.023–.045世界单位；均为本轮试玩值，不登记为最终美术数值。

上述细颗粒纹理是项目已有AI生成运行素材（1254×1254），来源见 `docs/art/ASSET_MANIFEST.csv` 的 `MAP-V2-1` 和 `docs/art/TACTICAL-MAPS-V2-GENERATION.json`，不冒称原作或CC0；本次复用原像素，没有截取概念图、生成新纹理或引入新外部依赖。

外围的Surface分支读取原 `FormationNative` 漫反射、法线、金属度／光滑度、发光以及原UV2的RGBM图集，仅将图集用作可转移间接光。**不采原 Shadowmask，也不保留原走廊阴影位置**；新棋盘直接光／阴影来自当前场景方向光和实际新几何。最新 `FormationReferenceMapTools` 采用偏暖主光强度1.5、方向(-.62,-1.2,-.72)，并给外围原材质最终色乘.78、反射强度.38；不是之前源主光强度3的复刻方案。外围仍使用Gamma BRDF2兼容近似和源Probe的mip5/3；远景源背景保留，近处由保存的工业墙板、服务走道及钢结构承接。

`ReflectionSource` 保留原 `ReflectionProbe-0` 的六面和八层已编码mip；Editor首次导入创建 `OriginalLS5Reflection.cubemap`，不重算mip。来源版本、对象ID、逐PNG SHA256与真实权利状态见 `source-reflection.json`；源包URL／校验沿用 `docs/art/source-records/formation-ls5-native.json`。RGBM解码系数5不是已恢复的原运行时HDR参数，最新原材质外围Probe强度.38；混凝土分支不读取此Probe。

这是一套独立兼容着色器，**不是原游戏Shader二进制的移植或逐像素复刻证明**。主光Gamma转换、反射方向／强度、法线导入和棋盘重排后的现场阴影均需在Unity实际渲染和Player中核对。原图旧门图标与2026源贴图的警告图标版本差异继续存在，Tint仅适配颜色，不冒称恢复旧图标。门原本不投影／不接收地形阴影，继续保持透明标记行为；Surface具有真实Caster与Receiver。
