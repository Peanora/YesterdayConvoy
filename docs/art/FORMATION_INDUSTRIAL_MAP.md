# 矩形攻坚：LS-5 原作环境接入

2026-11-17。用户最终确认的是[《明日方舟》LS-5 原关卡图](https://torappu.prts.wiki/assets/map_preview/wk_kc_5.png)，随后明确回复“可以”接入。先前工业庭院 AI 图、方块式重建和将 AI 图投影到地面的方案均已被这次修订覆盖，不能继续登记为本轮的最终美术依据。接入范围为**原作地砖、台面、工业外围网格与贴图，按本作 14×8 开放棋盘重排，并复用原作完整红蓝门**；原图确认不等于本轮实机已经验收。

## 地图、门与战斗坐标

| 项目 | 当前接入范围 |
| --- | --- |
| 棋盘 | 14×8、112 个保存的可编辑格对象；每格引用原 LS-5 持久 Mesh 与材质，中央保持开放 |
| 格距 | `FormationBattleController.WorldPoint(cell)=(cell.x-6.5, .12, cell.y-3.5)`，x/z 均为 1 个世界单位；地砖顶面对齐 y=.12 |
| 部署／守军 | 左侧 col=0–5 战前布阵，右侧 col=8–12 守军站位；不复制原 LS-5 的狭窄通道、阻挡或旧路线 |
| 红门 | col=6，即左侧六列部署区紧右的第七整列，row=0–7；门以格中心定位，不再吊在 x=5.5 的格缝上 |
| 蓝门 | 最右 col=13，row=0–7，每格一个完整原作门 |
| 门的形态 | 原 `tile_start`／`tile_end` 四层 MeshRenderer、贴图与透明／加色材质；宽、深、高各约 1 格，缩放保持 1，相邻格边缘紧贴；保留顶面三角／叹号及斜角刻线 |
| 外围 | 原 LS-5 服务带、台面、建筑、栏杆脚手架等实际部件移到开放棋盘四周；原 `BG_Qcity` 网格与背景贴图提供远景 |

红门是本模式的阵地区界标识，蓝门是清场后的实际终点；门没有碰撞体，不增加阻挡、门血量、费用、手动部署或复活入口。不得再次把完整门缩成低矮四角标、细长地贴、窄门带或实心墙，也不把八个门沿高度叠成塔。每一格保留一个完整门，相邻门沿地面纵深逐格紧贴。

战斗仍采用左侧战前布阵、跨关继承、原部署位置出生与复活、自动攻击和技能、先清空守军才进门、至少一名存活本体实际进门才胜利，以及覆盖复活和进门过程的整关模拟时限。本次原作环境接入不自行改动这些规则或角色数值。

## 已保存场景与编辑工具

场景为 `Assets/Scenes/FormationBattle.unity`，静态环境根名为 `LS5 native formation courtyard`。112 个 `Tile col,row` 对象在编辑模式中可查看、选择和保存，引用原 LS-5 持久 Mesh；地砖局部顶面约 y=-.0200154，保存对象以相应高度补偿对齐逻辑地面。外围、远景与 16 个原门同样保存于场景，Play 仅创建动态角色、界面与战斗反馈，不在运行时重建整图。

- `FormationNativeAssetTools.EnsureImported()`：从 `Assets/Art/Environment/FormationNative/ls5-native.json` 导入原部件持久 Mesh、材质与贴图，不修改场景；`FindPartName`／`FindParts` 查询部件，`CreatePart` 创建引用这些资源的对象。
- `FormationNativeMapTools.Apply()`：显式菜单 `Yesterday Convoy → Formation Battle → Apply Original LS-5 Environment`，将原资源重排并保存到本模式场景，移除先前绘制庭院／方块庭院根节点；不改其他地图。
- `FormationNativeMapTools.Validate()`：菜单 `Validate Original LS-5 Environment`，检查 112 格持久原网格、16 个完整原门、格中心与相机投影范围。执行结果须由 STATUS／QA 记录，不据入口存在就宣布通过。
- `FormationBattleTools.Author()`（菜单 `Create Missing Scene`）仍仅用于缺失场景的基础创建；本轮环境以 Native 工具接入，旧 Industrial／Painted 工具不是当前地图的编辑依据。

重新应用是显式的环境重排动作，不能当作普通运行或保存按钮。人工调整应在唯一场景写入者协调下保存，避免后续重新应用覆盖希望保留的改动。物理格距、相机取景和格点点击需一起验证，不能只改变画面而让逻辑继续使用旧 z 方向间距。

## 镜头、光照与原 A 界面

相机保留 LS-5 的 **60° 俯角、FOV40、偏航／翻滚为 0 的轻透视方向**，使用完整相机视口，再按本作 14×8 棋盘和原 A 界面留出的中部战场区域调整距离／偏移。当前工具的目标框为虚拟 1280×720 坐标中的 `(200,108,792,365)`；它是本次 UI 适配框，不宣称等于原 LS-5 的地图尺寸或相机距离。先前绘制方案的约 38.86°／FOV4.95 与 1280×500 压缩背景不再适用。

环境材质使用原漫反射、法线、金属度等贴图与原材质参数，在本作兼容 Shader 上显示。源相机、4 盏灯、环境设置与两张光照图保留在 `ls5-scene-settings.json`／贴图目录；当前场景保留原主光方向，环境 SH 按坐标系旋转后应用。原作混合／烘焙光照和专用 Shader 未整体迁入，单用源暖光会造成偏黄，因此本场景采用中性主光 `(0.92,0.94,1)`／强度 1.35、阴影强度 0.45 的显示适配，不改全局画质或其他地图。移除原狭窄走廊专用的三份 ShadowsOnly 遮光体，避免重排后出现没有对应建筑的巨型阴影。静态合批 UV2 已处于原光照图集坐标，不能再次乘旧 Renderer ST；默认不直接套用旧布局的烘焙阴影。

整备界面直接共用 `ApprovedShopPresentation` 和 `ApprovedUi` 的原 A 实现，包括 Market 商品面板、真实 idle 模型的 RenderTexture 平台、商品入场／合成 Flow 与资源显示。底部上阵槽 y=516、备战槽 y=627，沿用原槽几何和实际角色待机动作，不用重新绘制的相似卡槽或静态头像代替平台模型。界面不持有另一套金币、库存、合成或战斗状态；交易与编排仍由 `FormationShopSession`、`ShopTrialModel`、`ShopSeatLayout` 负责。

本轮显眼入口为默认行动准备阶段的“战前布阵 · 新玩法试玩 ▶”；旧右上菜单“矩形攻坚 · 新玩法 Demo”别名与 `-formationDemo` 直接进入参数继续保留。EXE 默认仍进入原挑战，不新增启动脚本。已在普通 EXE 实际点击默认入口并走通购买、升人口、上阵／换位、自动通关、领奖和站位继承；两尺寸 Player 及测试范围见 QA，不把技术验证等同用户最终美术验收。

## 来源与还原边界

原资源版本为 `26-10-29-15-36-11-33649006`：

- 场景：`scenes/obt/weekly/level_weekly_killcost_5/level_weekly_killcost_5.ab`。
- 依赖：`arts/maps/map_chernobog_a/res.ab`、`arts/maps/map_chernobog_a/background_mesh/bg_qcity.ab` 与本关 `lightingdata.ab`。
- 导出：143 个原 Renderer 部件、12 张贴图、5 份原材质数据；保存位置／法线／UV0／UV2、submesh 材质对应、原 transform、bounds 和对象引用。源场景没有顶点色的部件保持空数组，不虚构颜色数据。
- 门：复用已接入 `Assets/Art/Environment/BlackForestNative/NativeForest.prefab` 中原 `tile_start`／`tile_end` 网格、UV、贴图与透明／加色兼容材质，来源见既有 BlackForest 记录。

详细官方包 URL、MD5／SHA256、工具版本、导出哈希与权利状态见[LS-5 原素材记录](source-records/formation-ls5-native.json)和[资源接口说明](../../Assets/Art/Environment/FormationNative/README.md)。本轮上传源码、导出的运行资源与必要 `.meta`；原 AB、加工缓存、原 Shader 二进制与本地验证产物不纳入运行素材提交。

原作媒体权利属于相应权利人，未核实开放再分发或商业使用许可；用户确认接入和上传不登记为权利人许可，解析工具许可也不赋予媒体许可。兼容 Shader、重排后的地图、适配镜头与本作灯光**不等于原作 Shader、烘焙光照、动画粒子及后处理的逐像素复刻**。先前 AI PNG 与旧 CC0 方块环境的来源记录只保留历史追溯，不作为当前场景素材依据。

## 验证状态

本页记录已批准范围、当前代码连接与素材来源，不预先宣布本轮 Unity 编译、Apply／Validate、PlayMode、Windows 构建或 Player 画面通过。导出静态数据校验与运行验证分别记录，后者以 [STATUS](../STATUS.md)／[QA](../QA.md) 的实际结果为准。

需核对：112 格与 1×1 格距、col6／13 全高透明门逐格紧贴、角色脚底与格点点击、外围遮挡与原 A 安全区；商店展开／收起、真实 idle 平台模型及购买／合成 Flow；暂停、原位复活、清场后实际进门和整关计时。用户最终实机观感仍待反馈，技术检查不能代替美术验收。
