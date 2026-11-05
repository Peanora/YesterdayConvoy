# 塔露拉原素材动作与特效接入

2026-11-05，人类确认单独普攻看稿，并要求连同此前死亡等动作一起导入。沿用功能分支与 PR #75；保留早期 CC0 橙色火雨。此次范围为视觉、资源和死亡显示生命周期，不增加战斗机制。

## 对局已绑定的部分

| 内容 | 真实事件 | 接入位置 |
| --- | --- | --- |
| A组待机、行走、普攻、施法身体 | 原有四动作继续使用 | `OriginalTalulahArt`、`DemoSpriteVisual` |
| A组脚部常驻 | 存活实体与模拟时钟 | `TalulahAttackVfx.PresentUnit` |
| A组移动火焰 | 实际 `IsMoving`，停止即隐藏 | 同上 |
| 原普攻三组施法者特效 | `CombatShot.StartedAt`，与身体共用原1.0333秒标记映射 | `TalulahAttackVfx.Present` |
| 原普攻目标命中 | 原 `CombatShot.Applied / ImpactAt`，显示在真实目标落点 | 同上；不是固定坐标看稿 |
| 手动施法手部 | 原六秒施法的抬手/保持/收势映射 | `PresentUnit`；不产生点燃目标事件 |
| 原 `Die` | `Resolution.Dead / DeathTime`，80帧、2.666667秒 | `DemoSpriteVisual.PresentOriginal` |
| 完整死亡尾段 | 按每个实体对应卡的时长保留视图，战斗结束后只推进显示时钟 | `ManualBattleController`，结算面板等待显示完成 |

普攻仍为原0.65秒命中、1.7秒周期，伤害数值、SP、路径、索敌和移动逻辑保持。火雨仍使用已确认的橙色穿透火浪、原六秒六波伤害与实时地图覆盖。

## 已导入，但尚未绑定

身体新增包在 `Assets/StreamingAssets/TalulahOriginal/ExtraClips`：12段正时长动作、1167帧、25页，另存2个透明等待姿态。仅 `death` 进入当前预加载；其余11段及等待均为 `IMPORTED_NOT_BOUND`。

独立特效在 `Assets/StreamingAssets/TalulahUnboundVfx`：34组未绑定特效、78页。包括B组普攻/常驻、转阶段、点燃/引爆及目标层、灼息、晶体出生/爆发/四向火球、原版火流。它们随源码和试玩资源交付，但当前不加载、不根据星级或其他角色事件自动触发。

原因：目前没有塔露拉复起/二阶段、点燃/引爆或晶体实体状态。普通死亡不能假扮复起，卡牌升星不能假扮战斗转阶段，其他角色的灼烧字段也不能借来触发这些原技能。新增相应玩法前需明确共享战斗接口、触发规则和数值范围；本次美术确认不替代机制确认。

## 资源与处理

- 已绑定5组特效在 `Assets/Art/VFX/Skills/Talulah/Resources/TalulahNative`，393帧、5页；按需解码，不预加载未绑定阶段。
- 全部特效共39组、4218帧、83页，PNG约22.3MB。打包细节及哈希见未绑定目录的 `package-report.json`。
- 原骨骼 `enemy_1515_bsnake` / Spine 3.8.99；源索引为 <https://torappu.prts.wiki/assets/enemy_spine/enemy_1515_bsnake/enemy_1515_bsnake.skel>。身体来源、缩放与逐页记录在 ExtraClips 的 `SOURCE-RECORD.json`。
- 原特效来自官方Android资源包 `pkgrps_btl_pfb_fx_enemy_1.dat`，资源版本 `26-10-29-15-36-11-33649006`。具体地址：<https://ak.hycdn.cn/assetbundle/official/Android/assets/26-10-29-15-36-11-33649006/pkgrps_btl_pfb_fx_enemy_1.dat>。源缓存 MD5 `46ac009a973053d7ee3a8f4cdd1b201e`。
- 独立工具用 Unity 6000.6.5f1 / Vulkan 播放原材质、Shader、网格和粒子；不在运行时依赖原包或外部工作区。`tools/package_talulah_native.py`统一全片裁切、镜像、pivot/PPU，并保留零Alpha的加色RGB；图格与页面补齐4像素，避免BC7改变网格尺寸。
- 原生世界到现有角色视觉尺度为1.60714285694；身体新片从0.5px/骨骼单位等比变为旧包0.4，PPU88.8888889保持。镜头竖向投影差异在视觉子节点补偿，不改变逻辑Transform。
- `TalulahNativeImport`将五页运行特效明确导入为线性2D BC7，避免Cubemap默认设置；运行Shader按各片段HDR范围还原预乘/加色混合。

原素材权利归原权利人，未取得开放再分发许可。人类要求素材随源码交付，不记为权利人授权；本轮不创建ZIP/Releases。原客户端部分生命周期和全局特效收缩系数未完整取得，不能宣称与原作逐像素一致。

## 验证

真实项目测试、构建及独立Player结果见 `docs/QA.md` 本轮条目。素材打包通过不等于游戏运行通过；无战斗状态的片段不标记为可玩功能。
