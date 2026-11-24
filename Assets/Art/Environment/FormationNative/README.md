# LS-5 原作环境部件

本目录是《明日方舟》LS-5真实场景的独立网格和贴图导出，依据用户已确认的[关卡原图](https://torappu.prts.wiki/assets/map_preview/wk_kc_5.png)。不是AI概念图，也不是把关卡截图贴在地板上。

来源版本、官方包URL、MD5/SHA256、对象ID、加工范围和权利状态见 `docs/art/source-records/formation-ls5-native.json`。原媒体权利属于对应权利人，未核实开放再分发许可；解析工具许可不代表媒体许可。

## 导入接口

`YesterdayConvoy.Editor.FormationNativeAssetTools.EnsureImported()` 读取 `ls5-native.json`，导入12张贴图、5份材质和143份独立持久Mesh资产；不修改场景。`FindPartName()` / `FindParts()`查询源部件；`CreatePart(part,parent,useSourcePosition)` 创建引用这些持久资产的可编辑对象。场景和最终布局由独立地图工具保存。

JSON的 vertices/normals/uv/uv2/colors 是扁平数组，分别每3/3/2/2/4项组成一个顶点属性。每个部件保留原Renderer/网格对象ID、submesh材质对应、原transform和转换后的bounds。原源场景没有顶点色时保留空数组。

源静态合批网格已经是源世界坐标；导出转为 `(x,-z,y)`，然后仅在地面x/z方向以每个部件bounds中心重定位。不要再次应用原节点transform。`SourcePosition`可恢复原布局，模型自身Y高度不变。

## 地砖与外围

普通一格地砖候选17个：`S_Ground_tiles_Qcity_0242`、`0275`、`0106`、`0250`、`0267`、`0272`、`0110`、`0107`、`0273`、`0263`、`0234`、`0264`、`0114`、`0274`、`0270`、`0116`、`0233`（后续短名均指同一完整前缀）。宽深均1格，顶面局部Y为 `-.0200154`；令对象Y等于期望地面Y加 `.0200154` 可对齐。`0276`和`0180`包含原蓝门地洞，不作为开放地面。

外围可查询 `S_Outworld_prop`、`S_export_cube`、`OP_`、`S_ladder`；它们均为原场景实际部件，部分名字为原DCC通用名称，不能按名称误认为程序方块。`BG_Qcity`是原作远景网格与原背景贴图。`Shadow`对象是源遮光几何，布置前需独立评估遮挡，不复制到可玩棋盘。

原红/蓝门复用 `BlackForestNative/NativeForest.prefab` 中的 `Native tile_start` / `Native tile_end` 及其持久Mesh/透明材质。门原宽深高均约1格，不采用旧程序线框或低矮替代门。

## 光照与镜头

`ls5-scene-settings.json`完整记录源相机、4盏灯、RenderSettings和LightmapSettings。相机转换到本作坐标后位于 `(0,8.04,-5.08)`，60°俯角、FOV40，参考场景视距约9.51；新14×8范围需要保持方向并重新取景。原主光为带软阴影的方向光，颜色约 `(1,.693,.228)`、强度3；另外3盏是烘焙补光，不能把其本地旋转误当世界旋转。

保留两张原光照图和142个部件的UV2。静态合批后的UV2已经落在整个光照图集中，**不能再乘原Renderer lightmapScaleOffset**。导入创建的Renderer默认不启用旧布局的烘焙阴影，以免地形重排后出现错误遮影；源数据可供后续明确使用。兼容材质与重排后灯光不等于原作逐像素复刻。

当前开放棋盘保留原主光方向，27 项环境 SH 通过 Unity 实际基函数旋转到本作坐标。原狭窄走廊的三个 ShadowsOnly 遮光体不放入重排场景；小型高空零件也不脱离原支撑悬空摆放。专用 Shader／烘焙补光未整体迁入，因此采用场景局部中性主光 `(0.92,0.94,1)`、强度 1.35、阴影强度 0.45，避免单独复制源暖光造成大面积偏黄。没有修改项目全局画质设置。

## 可重复导出

使用现有Python3.13和包含 `tools-python/UnityPy`、`external-tools/aklz4.py`、`hot_update_list.json` 的本机原作解析工作目录，依次运行 `tools/art/acquire_formation_native.py --runtime <工作目录>` 与 `tools/art/export_formation_native.py --runtime <工作目录>`。前者仅把4个已记录原包下载到忽略的 `artifacts/formation-native-source` 并核对MD5；后者生成本目录的JSON和PNG。不提交原AB、缓存或原作Shader二进制。

导出数据静态检查通过：143部件、14919顶点、9711三角形；法线/UV0长度、UV2长度、索引范围、有限数值、材质引用和12张PNG的SHA256均有效。Unity导入、场景视觉与Player验证由集成者实际执行并另记结果。
