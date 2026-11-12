# 热交换站 V5 看稿接入

2026-11-12。用户确认项目外 V5 完整地图预览，并要求进入游戏查看效果。本轮只更新第五张独立热交换站场景及相应美术表现；真实战斗和运行验证结果见 STATUS/QA。

入口：直接打开 `Builds/Windows/YesterdayConvoy.exe`，从游戏内角色测试页进入“战术地图”并选择“05 热交换站”。普通启动继续正式行动。开发验证可用现有 `-approvedMap 5` 参数，不提供 CMD/BAT 启动器。

## 场景与范围

- `Assets/Scenes/Map05HeatExchange.unity` 保存本版几何、材质和照明。原左上28格高台保留6格：2×2初雪小台、1×2辅助台；22格降为检修庭院。140个地块均有独立元数据与可编辑顶面，不在Play时生成整张地图。
- 两条固定路线、三门坐标、热泵时序/伤害及共享防御保持。独立Map05保留D01/D04/D07/D08，额外在预览位置放入既有D27配置；正式第十关的守军阵容不改。
- 使用已批准的三维几何和分材质规则，52,081顶点、31,825三角；不使用整图截图作为地面贴图。428个导出网格保留140顶面，其余不透明细件按等价材质合并。顶面使用已有火雨识别名称，外围与侧墙不作地面。
- 角色仍为原二维帧；初雪的起手、攻击、命中与积雪绑定实际战斗状态和共享模拟钟，不移植外部预览的固定五次攻击、进退场或三秒换雪编排。

## 美术绑定

| 项目 | 采用资源 | 运行连接 |
| --- | --- | --- |
| 布局/模型 | 已批准V5 geometry与材质角色 | HeatExchangeV5Tools仅在Editor导入，场景保存静态Mesh/Material |
| 混凝土/金属 | V3来源纹理，V5克制细节与明暗规则 | HeatStationSurface支持世界尺度UV、板面变化、接缝积灰与高粗糙度 |
| D27身体 | 工程已有原皮正背面动作 | 原DefenderRosterView与战斗动作事件 |
| caster / hit | 原Shader透明HDR采样，49/30秒与1.5秒 | 真实强化攻击及命中事件；保留源片段长度 |
| buff01 / range | 已批准原采样层 | 实际技能有效区间与施法者位置 |
| ground01/02/03 | 已批准原积雪采样 | 实际SnowCells层数与位置，暂停/重开同步 |
| 接地 | 已批准脚底软影、环境融合与人物轮廓投影方向 | 只改变表现，不修改路点、索敌、阻挡、伤害或模拟钟 |

原caster/hit及补层需要保留alpha为0处的加色RGB。少量页面通过 `Resources/PramanixV5/*.bytes` 无损gzip保存RGBA，`ArtSequence`按清单可选读取；其他角色继续既有预处理纹理路径。原PNG、运行清单、bytes与必要meta一起随源码提交；Library处理缓存、外部预览和构建不提交。

## 来源与限制

地图几何/布景由本轮制作，视觉参考此前实际查看的《明日方舟》切尔诺伯格·破碎大道；不是原地图完整复刻。混凝土来自 [ambientCG Concrete034](https://ambientcg.com/view?id=Concrete034)，金属来自 [Quaternius Modular Sci-Fi MegaKit](https://quaternius.itch.io/modular-sci-fi-megakit)，两者为CC0-1.0。金属使用原Trim01板面裁片，同一裁片用于颜色/法线/ORM；部分物理平铺尺度为看稿假设，未独立证明裁片完美无缝。导出材质还包括本轮程序绘制的文字/窗口纹理。

原D27人物与原FX归原权利人，沿用既有源记录与本轮采样来源；用户接入及上传要求不记为权利人开放再分发许可。当前接入覆盖caster/hit、buff01/range、三个积雪层、确认C的flow，以及后续确认的原雪地接触01。三常驻层只初播冷启动，之后重复1–3秒稳态，避免三秒归零。D27地面光按用户要求位于演员身后；技能攻击读取原CombatShot，踩雪01读取原成功结算的内部只读记录，两类分开。原RGBA/尺寸/伤害/SP保持。buff02、原精确挂点/停发尾段/循环接缝、积雪最终合成和专用音效仍有未核实或未接入部分，不称完整客户端还原。最新实机与资源证据见QA及初雪接入说明。

关键数据：`Assets/Data/ArtCatalogs/HeatExchangeV5/map-v5.json`是Editor源；`Map05.asset.gridRows`与`layouts.json`保持相同格点；`Map05Battle.asset`保存独立试玩守军；`ApprovedMapBootstrap`只加载战斗，`ApprovedHeatMarker`只读热泵模拟状态。`HeatExchangeV5Tools.Apply`只修改这一张图，不调用重建八图的旧批量入口。
