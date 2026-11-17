# 矩形攻坚：工业庭院地图接入

2026-11-17。本轮针对 `Assets/Scenes/FormationBattle.unity` 的静态环境外观修正，延续[固定战场布阵战斗](../FORMATION_BATTLE.md)。用户此前批准工业庭院 PNG 的画风，并指定中线一排红门、最右一排蓝门；之后明确否定方块式三维接入，要求沿已批准画面修正。本轮采用 **2.5D 绘制环境背景＋投影到真实水平地面格的画面＋实时战斗角色**，不宣称全三维还原。用户本轮修正接入要求适用于这个范围；先前预览批准不等于本轮实机画面已验收。

## 已批准范围与坐标

| 项目 | 本轮范围 |
| --- | --- |
| 棋盘 | 沿用 14×8、112 格矩形战场；不改变格点坐标与地面逻辑 |
| 红门 | 沿已有部署区分界虚线 x=5.5，在 row=0–7 各设置低矮红色门标；逐格断开，保留穿行间隙 |
| 蓝门 | 最右 x=13，在 row=0–7 各设置低矮蓝色门标；不再用高立方门框遮挡角色 |
| 环境 | 沿用批准 PNG 中低饱和工业庭院的厂房、卷闸门、设备、栏杆与管线表现，中央为实际战斗区 |
| 地面 | 112 个真实、水平、可在编辑模式选取和保存的地面格，保留原格点与投影画面；不是直接以概念图格线代替逻辑坐标 |
| 表现范围 | 绘制背景与地表画面承载环境观感，角色仍实时移动、攻击、阵亡和复活；不把画中厂房登记为完整可绕行的三维建筑模型 |

红门位于部署区右边界，不是把全部角色挤到同一点生成的新出兵口。蓝门保留原有清场后进门目标的可见标识。两排门标不新增可点击部署、阻挡、费用、额外门血量或复活规则。

下列战斗逻辑保持：左侧战前格点布阵、跨关继承、从原部署位置出生／复活、自动攻击和技能、先消灭全部守军才开始进门、至少一名存活我方本体实际进门才获胜，以及覆盖复活和进门过程的整关模拟时限。本轮不调整随机守军、角色数值、技能或时间参数。

## 保存场景与编辑边界

上一版 `FormationIndustrialTools.Apply` 的方块式三维组合不是本轮画风修正的验收目标，不应据其旧结果宣布本轮绘制场景完成。

本轮显式 Editor 入口为 `FormationPaintedMapTools.Apply()`，菜单为 `Yesterday Convoy → Formation Battle → Apply Approved Painted Courtyard`；`FormationPaintedMapTools.Validate()` 对应 `Validate Approved Painted Courtyard`。工具只处理 `FormationBattle.unity` 的绘制环境、该场景相机、112 个原地面格及持久投影资源，移除已被否定的 `Industrial courtyard · saved geometry` 根节点；不修改角色素材或其他地图。

- 背景使用与相机对齐的远平面，完整展示同一 PNG 中的绘制环境；这些画中建筑不是本轮新建的完整三维建筑。
- 原 112 个 `Tile col,row` 对象保留，每格保存独立水平网格，地面中心仍读取 `FormationBattleController.WorldPoint`，高度为 y=0.12。地面使用保存的 `ProjectedFloor.shader` 与材质投影参数采样同一 PNG；使用不受光表现，避免已有绘制光影再被实时灯光重复处理。
- 新静态根节点为 `Painted courtyard · approved 2.5D environment`；门标仍名为 `Red gate row N`／`Blue gate row N`，位于原 x=5.5／13、row=0–7。四角低标与短侧柱逐格断开，没有碰撞体，不额外阻挡实时角色。
- 相机按源图地面四角适配本模式的 1280×500 战斗区域，俯角约 38.86°、FOV 约 4.95°，偏航与翻滚仍为 0。这是本轮绘制方案的场景适配参数，覆盖本场景此前镜头，不能擅自套回旧 60°，也不覆盖其他旧模式的相机。

完整 1672×941 原图映入 1280×500 区域时，相对原图比例存在纵向压缩；目前不能宣称与批准图观感完全一致。投影校验检查已定义矩形的格中心和外角，不代替对 AI 绘制格线笔触、角色脚底及前景遮挡的目视核对。相机、背景与保存的投影材质必须一并校准，单独改动镜头可能造成背景／地面漂移。

绘制背景、水平地面格与低矮门标以场景内容保存，材质与必要网格以资产保存；进入 Play 读取已保存场景，不重新搭建整张地图。日常调整通过编辑场景后保存完成，不把显式重新应用工具当作普通运行或保存按钮。重新应用前应核对将改动的环境范围，避免覆盖希望保留的人工环境编辑。

中央棋盘保持开放可读，外围装饰不添加影响原移动／索敌的玩法障碍。布局、站位与蓝门判定继续由原战斗模型负责，静态美术不另维护一套战斗坐标或触发规则。

## 批准 PNG 的身份与本轮用途

- 本轮 AI 概念生成日期为 2026-11-17，参考工程既有 Approved Maps 03“雨夜立交”和 06“矿运折廊”的工业体块、材质与气氛。参考关系不把这两张旧图的路线、机关和门位带入新战场。
- 原始输出来自项目外 Codex `generated_images`，文件名 `exec-e85ac373-84c5-42d3-bfe8-ecd6a96da4af.png`。本轮已将同一 PNG 放入运行资源路径 `Assets/Art/Environment/FormationBattle/Painted/industrial-courtyard.png`，其 SHA256 为 `256A2F91033A69D8F33D8E45D74AA1814C53FBA41064F9CAAFF0778F36A888DE`；本次按实际文件重新核对该哈希。
- 上一版将它仅作为三维接入参考；本轮用户否定方块外观后，明确要求沿已批准画面修正，因此将它改作运行中的绘制环境背景与水平地面格投影来源。**这是有真实格点与动态角色的 2.5D 场景，不是全三维建筑还原，也不是在预览图上播放静态战斗分镜。**
- 该 PNG 是既有 AI 生成预览，不登记为外包资源或 CC0。用户要求接入与上传运行素材，不等于取得参考作品的开放再分发许可，也不扩大既有角色媒体的权利范围。
- 图中的绘制格线、透视与装饰不能代替 14×8 的真实坐标。四角、中心、行列边缘、角色脚底、门标与画面投影需在实际相机下核对；本记录不承诺原预览与实机逐像素一致。

既有八图的参考与生成关系见[八图说明](../APPROVED_MAPS_V2.md)及 [V2 生成记录](source-records/approved-maps-v2-generation.json)。它们是参考来源，不是本轮工业庭院完成验证的证据。

## 运行素材来源与许可

本轮绘制环境的源图为上节记录的批准 PNG；其运行副本、持久材质、投影网格与必要 `.meta` 应随本轮源码按项目素材交付规则提交，来源状态仍为 AI 生成预览。地面网格、门标和场景配置属于本项目接入制作，角色媒体继续沿用各自原有来源记录；不能把所有内容统称为 CC0。

上一版方块环境曾复用下列素材。这些历史来源记录保留，用于区分既有资产许可与本轮 AI 图身份，**不作为本轮绘制场景的当前用料清单或画面验收证据**。既有原包记录见项目 [ASSET_SOURCES](ASSET_SOURCES.md)、[ASSET_MANIFEST](ASSET_MANIFEST.csv) 与 Quaternius 本地许可文本。

| 旧版资源 | 工程路径 | 已有来源与权利记录 |
| --- | --- | --- |
| 混凝土颜色图 | `Assets/Art/Environment/Industrial/Textures/concrete-color.jpg` | ambientCG [Concrete034](https://ambientcg.com/view?id=Concrete034)，项目清单登记 CC0-1.0；原始 Color 图，沿用材质调色 |
| 沥青颜色图 | `Assets/Art/Environment/Industrial/Textures/asphalt-color.jpg` | ambientCG [Asphalt012](https://ambientcg.com/view?id=Asphalt012)，项目清单登记 CC0-1.0；原始 Color 图，沿用材质调色 |
| trim-01／trim-02 颜色与法线 | `Assets/Art/Environment/Industrial/Textures/trim-01-color.png`、`trim-01-normal.png`、`trim-02-color.png`、`trim-02-normal.png` | Quaternius Modular Sci-Fi MegaKit Standard，项目清单登记 CC0-1.0 |
| 箱体模型 | `Assets/Art/Environment/Industrial/Models/Prop_Crate3.fbx` | 同一 Quaternius Standard 包，清单 ID `MAP-Q-PROP_CRATE3`，CC0-1.0 |
| 通风设备模型 | `Assets/Art/Environment/Industrial/Models/Prop_Vent_Big.fbx` | 同一 Quaternius Standard 包，清单 ID `MAP-Q-PROP_VENT_BIG`，CC0-1.0 |
| 上一版厂房／门架等组合几何与材质配置 | `FormationBattle.unity` 的旧环境及对应材质资产 | 历史项目场景制作；复用纹理和模型的部分继续受各自来源记录约束，不把其许可扩大到本轮批准 PNG |

Quaternius 的原发布页为 [Modular Sci-Fi MegaKit](https://quaternius.com/packs/modularscifimegakit.html)／[官方 itch 页](https://quaternius.itch.io/modular-sci-fi-megakit)，工程保留的 Standard 包许可见 [quaternius-modular-scifi-standard.txt](licenses/quaternius-modular-scifi-standard.txt)。ambientCG 许可参考为[官方许可页](https://docs.ambientcg.com/license/)。本轮记录沿用既有原包与许可核查，未把付费 PRO／SOURCE 档内容当作免费 Standard 包使用。

## 验证状态与待核对项

本页只记录批准范围、接入约定和素材来源，不预先宣布本轮编译、场景校验、PlayMode、Windows 构建或 Player 画面通过。实际执行结果由 [STATUS](../STATUS.md)／[QA](../QA.md) 记录。

接入后需要实际核对：112 格原坐标与合法布阵区域不变，四角与中心的背景／地面格投影不漂移；两排各八个低矮断开门标位置正确；地面、网格、纹理及材质引用可在编辑模式保存；两种分辨率与整备商店展开／收起时角色脚底、血条、前景遮挡和点击格一致；暂停、原位复活、清场到蓝门及整关计时继续读取原模型。图片细节与本轮实机观感仍待用户反馈，技术验证不能替代这一项。
