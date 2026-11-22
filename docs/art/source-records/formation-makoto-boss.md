# 结城理「见证荣光」的原作来源与绑定

使用 CROSSOVER 时装 `char_4217_makoto@p3r#1`，战斗 Prefab 为 `char_4217_makoto_p3r#1`。结城理本体、塔纳托斯·改和俄耳甫斯·改都在该皮肤骨骼内。正、背面攻击取自同一版本。原作的替换和守备段提供正面动作，棋盘沿用原正面。 [官方活动公告](https://ak.hypergryph.com/news/9681) · [原模型索引](https://torappu.prts.wiki/assets/char_spine/char_4217_makoto/meta.json)

时装对比片左侧为「见证荣光」。`0:27–0:29` 可核对人形挥剑与蓝色剑弧，`0:53–0:54.5` 可核对塔纳托斯弱点攻击，随后至 `0:56` 左右转入蓝色守备区域。该片三技能段从已在场的塔纳托斯切入，完整的本体换形顺序以客户端 Actor 为准。另一个时装慢镜在 `0:28–0:52` 对照人形剑击，`1:48` 起展示三技能。 [时装技能对比](https://www.bilibili.com/video/BV1QStJ6mEn6/) · [时装慢镜](https://www.bilibili.com/video/BV1vdtq6qE2u/) · [官方技能 PV](https://www.bilibili.com/video/BV1RhtV6iEt3/)

客户端资源版本 `26-10-29-15-36-11-33649006`。核验 `charpack/char_4217_makoto.ab`、`skinpack/char_4217_makoto.ab`、`battle/prefabs/effects/makoto.ab` 的原清单 MD5。`makoto_s3_trigger` 按当前状态进入 `Stand_S3_1` 或 `Stand_S3_2`，结合 `CharacterSkinHooker` 和 buff holder 核对阶段、施法与接触层。实际 URL、SHA-256、采样设备、裁切、尺寸适配及图集哈希见运行记录。媒体归原权利人，公开再分发许可未核实。 [运行来源记录](../../../Assets/Data/ArtCatalogs/BossRosterFx/MAKOTO_P3R_1/SOURCE-RECORD.json)

| 对局事件 | 原作动作与表现 | 棋盘绑定 |
| --- | --- | --- |
| 第一阶段 · 结城理本体 | `Default` 采用 `Attack_A/B` 交替近战物理伤害。正背面都为源 `1.20s`，同在 `0.5333s` 触发 `OnAttack`。原皮肤剑弧内置 `0.50s` 延时，B 有独立后向剑弧 | 起手剑弧读该次挥剑，蓝色命中读实际物理接触。沿用原 2× 动作时间 |
| 本体切换塔纳托斯·改 | 共用 `SwitchOut` 后播放 `Doll_Skill_3_P1_SwitchIn`，两段各为源 `1.00s`。入场有 `makoto_change_01` | 两段动作接在首条生命结束后，用原变身层，形态常驻从入场结束起显示 |
| 第二阶段 · 塔纳托斯·改 | `Doll_Skill_3_P1_Attack_A/B` 各为源 `1.80s`，同在 `0.80s` 触发 `OnAttack`。原 buff holder 将物理、法术接触分别映射到 `p1_hit_02`、`p1_hit_01` | 起手跟随正、背面，按实际伤害种类显示命中。由默认法术切成物理时显示弱点标记 |
| 第三阶段 · 俄耳甫斯·改 | `Doll_Skill_3_P1toP2_ChangeBegin` 为源 `0.50s`，`ChangeEnd` 为源 `1.00s`，后者在 `0.80s` 触发变身特效。进入守备后停止攻击；原目标先显示 `p2_heal_01`，`0.50s` 后恢复 | 守备常驻、地面圈和治疗读真实阶段与时钟。自疗、减伤及治疗次数为本作 DPS 校准 |

使用 Unity Vulkan 离线采样。当前三技能流程使用 24 段源特效，5 段本体剑击特效采样均无裁边和 HDR 截断。既有一、二技能图集仍保存在守军库中。本作保留三阶段并采用分血条的转段时机，省略原替身计时结束回到人形和组队总攻击。第三阶段自疗替代原友方群疗，确定性减伤替代闪避。主工程 Player、棋盘遮挡和战斗结果见 [验收记录](../../QA.md)。
