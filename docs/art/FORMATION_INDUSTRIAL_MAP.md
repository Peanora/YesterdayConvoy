# 矩形攻坚：工业庭院地图接入

2026-11-17。本轮针对 `Assets/Scenes/FormationBattle.unity` 的静态环境外观接入，延续[固定战场布阵战斗](../FORMATION_BATTLE.md)。用户已批准本轮工业矩形预览，并明确指定中线一排红门、最右一排蓝门。此记录区分批准方向、运行资源来源与尚待实机确认的结果，不把预览批准或既有素材的历史验证当作本轮 Player 验收。

## 已批准范围与坐标

| 项目 | 本轮范围 |
| --- | --- |
| 棋盘 | 沿用 14×8、112 格矩形战场；不改变格点坐标与地面逻辑 |
| 红门 | 沿已有部署区分界虚线 x=5.5，在 row=0–7 各设置一个红色门架 |
| 蓝门 | 最右 x=13，在 row=0–7 各设置一个蓝色门架 |
| 环境 | 外围厂房、卷闸门、通风设备、箱体、栏杆、管线与门架；中央保留开放战斗区域 |
| 外观方向 | 参考批准概念的低饱和工业庭院、模块地表与清楚的红蓝边界，接入为可查看、选择和保存的真实 3D 场景物件 |

红门位于部署区右边界，不是把全部角色挤到同一点生成的新出兵口。蓝门保留原有清场后进门目标的可见标识。两排门架不新增可点击部署、阻挡、费用、额外门血量或复活规则。

下列战斗逻辑保持：左侧战前格点布阵、跨关继承、从原部署位置出生／复活、自动攻击和技能、先消灭全部守军才开始进门、至少一名存活我方本体实际进门才获胜，以及覆盖复活和进门过程的整关模拟时限。本轮不调整随机守军、角色数值、技能或时间参数。

## 保存场景与编辑边界

`FormationIndustrialTools.Apply` 为显式 Editor 接入入口，仅处理 `FormationBattle.unity` 中的本轮静态工业环境与相应持久材质，不修改其他地图或角色资源。

地表、厂房、门架和外围物件以场景内容保存，材质以资产保存；进入 Play 读取已保存场景，不运行工具重新搭建整张地图。日常调整通过编辑场景后保存完成，不把显式 Apply 当作普通运行或保存按钮。显式重新应用前应核对工具将改动的环境范围，避免覆盖希望保留的人工环境编辑。

中央棋盘保持开放可读，外围装饰不添加影响原移动／索敌的玩法障碍。布局、站位与蓝门判定继续由原战斗模型负责，静态美术不另维护一套战斗坐标或触发规则。

## 批准概念的身份

- 本轮 AI 概念生成日期为 2026-11-17，参考工程既有 Approved Maps 03“雨夜立交”和 06“矿运折廊”的工业体块、材质与气氛。参考关系不把这两张旧图的路线、机关和门位带入新战场。
- 概念原始输出位于项目外的 Codex `generated_images` 目录，文件名 `exec-e85ac373-84c5-42d3-bfe8-ecd6a96da4af.png`，SHA256 为 `256A2F91033A69D8F33D8E45D74AA1814C53FBA41064F9CAAFF0778F36A888DE`。本轮不把预览复制进 Assets，也不编造额外许可。
- 概念用于人类看稿确认和 3D 接入参考，**不作为运行中的整张地板贴图，也不以铺一张概念图冒充可编辑的工业场景**。
- AI 概念不登记为 CC0。用户确认接入方向不等于获得任何参考作品的开放再分发许可，也不扩大既有角色媒体的权利范围。
- 厂房、设备比例、局部细节、光照和遮挡以最终实际相机画面继续核对；本记录不承诺概念与实机逐像素一致。

既有八图的参考与生成关系见[八图说明](../APPROVED_MAPS_V2.md)及 [V2 生成记录](source-records/approved-maps-v2-generation.json)。它们是参考来源，不是本轮工业庭院完成验证的证据。

## 运行素材来源与许可

本轮复用已在工程中的环境资源，没有为了地图重新导入未经核查的外部资源包。以下来源已核对项目内 [ASSET_SOURCES](ASSET_SOURCES.md)、[ASSET_MANIFEST](ASSET_MANIFEST.csv) 与 Quaternius 本地许可文本；沿用各素材自己的许可，不将其扩大到 AI 概念或《明日方舟》角色媒体。

| 实际资源 | 工程路径 | 已有来源与权利记录 |
| --- | --- | --- |
| 混凝土颜色图 | `Assets/Art/Environment/Industrial/Textures/concrete-color.jpg` | ambientCG [Concrete034](https://ambientcg.com/view?id=Concrete034)，项目清单登记 CC0-1.0；原始 Color 图，沿用材质调色 |
| 沥青颜色图 | `Assets/Art/Environment/Industrial/Textures/asphalt-color.jpg` | ambientCG [Asphalt012](https://ambientcg.com/view?id=Asphalt012)，项目清单登记 CC0-1.0；原始 Color 图，沿用材质调色 |
| trim-01／trim-02 颜色与法线 | `Assets/Art/Environment/Industrial/Textures/trim-01-color.png`、`trim-01-normal.png`、`trim-02-color.png`、`trim-02-normal.png` | Quaternius Modular Sci-Fi MegaKit Standard，项目清单登记 CC0-1.0 |
| 箱体模型 | `Assets/Art/Environment/Industrial/Models/Prop_Crate3.fbx` | 同一 Quaternius Standard 包，清单 ID `MAP-Q-PROP_CRATE3`，CC0-1.0 |
| 通风设备模型 | `Assets/Art/Environment/Industrial/Models/Prop_Vent_Big.fbx` | 同一 Quaternius Standard 包，清单 ID `MAP-Q-PROP_VENT_BIG`，CC0-1.0 |
| 本轮厂房／门架等组合几何与材质配置 | `FormationBattle.unity` 及本轮保存的材质资产 | 项目场景制作；复用纹理和模型的部分继续受各自来源记录约束，不把整个场景统称为外部 CC0 包 |

Quaternius 的原发布页为 [Modular Sci-Fi MegaKit](https://quaternius.com/packs/modularscifimegakit.html)／[官方 itch 页](https://quaternius.itch.io/modular-sci-fi-megakit)，工程保留的 Standard 包许可见 [quaternius-modular-scifi-standard.txt](licenses/quaternius-modular-scifi-standard.txt)。ambientCG 许可参考为[官方许可页](https://docs.ambientcg.com/license/)。本轮记录沿用既有原包与许可核查，未把付费 PRO／SOURCE 档内容当作免费 Standard 包使用。

## 验证状态与待核对项

本页只记录批准范围、接入约定和素材来源，不预先宣布本轮编译、场景校验、PlayMode、Windows 构建或 Player 画面通过。实际执行结果由 [STATUS](../STATUS.md)／[QA](../QA.md) 记录。

接入后需要实际核对：112 格原坐标与合法布阵区域不变；两排各八个门架位置正确；静态材质与模型引用可在编辑模式保存；外围物件不挡住角色、血条和战斗操作；暂停、复活、清场到蓝门及整关计时继续读取原模型。图片细节与实机观感仍待用户反馈，技术验证不能替代这一项。
