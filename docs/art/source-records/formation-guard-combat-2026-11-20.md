# 棋盘守军受击与出手素材

2026-11-20 读取本机《明日方舟》Windows 客户端资源清单 `26-10-20-11-10-38-04170990`。从 `charpack/char_*.ab` 的角色 `UberEffectEmitter` 或攻击组件读取事件，用 `battle/prefabs/[uc]projectiles.ab` 核对独立弹道对象和命中字段。棋盘继续使用先前采样并纳入项目的原特效图集，来源和许可见 [原守军特效记录](original-defender-vfx-2026-11-06.json)。没有新增原作音画文件。开放再分发许可仍未核实。

| 角色 | 原作数据 | 现役棋盘绑定 |
| --- | --- | --- |
| 米格鲁、卡缇、斑点、玫兰莎、翎羽 | 各角色普通攻击 `_hitEffects` 的 `common_hit_01` | 实际普通或同类接触结算后在被击者身体处播放。斑点在本作的护盾技能沿用其棋盘规则。 |
| 克洛丝 | `projectile_crossbow` 的 `_effectsWhenHit = common_hit_01` | 真实命中后播放原弩击接触。 |
| 月见夜 | `char_283_midn` 两组命中以及 `projectile_midn_s1` 的 `midn_skill_01_hit` | 普通和技能各接自己的命中图集。 |
| 史都华德 | `projectile_magic_ball` 的 `common_magic_hit_01`；`projectile_stward_s1` 的 `stward_skill_01_hit` | 继续沿用已确认的守军普通、强化攻击两条原特效对应关系。 |
| 炎熔 | `char_121_lava` 实体 `0x4a708449be9a48aa`。输入目标 `lava_attack_01_hit`，`startEvent=4`、`useInputPos=1`。命中 `common_firemagic_hit_01` | 实际出手时目标格起火，独立身体受击使用命中时的位置。 |
| 芙蓉、安赛尔 | `char_120_hibisc` 的 `common_heal_start_01` 与 `common_heal_hit_01`；`projectile_ansel` 的 `common_heal_hit_01` | 芙蓉在出手时播放治疗起手，二人仅在有效治疗结算时播放接触。 |
| 梓兰 | `projectile_orchid` 的 `orchid_attack_01_hit` | 每次真实命中后在目标处播放。 |
| 荒芜拉普兰德、余 | 原角色各有普通和技能三攻击起手。余的两段分别为 `yu_attack_01/yu_hit_01` 与 `yu_skill3_attack_01/yu_skill3_hit_01` | 普通起手和技能期间的实际攻击动作各有独立表现，余的火墙仍按已生效范围独立显示。 |
| Mon3tr | `char_4179_monstr` 的 `monstr_attack_start_01`、`projectile_chr_monstr` 的 `monstr_attack_hit_01`，以及重构形态的 `monstr_skill_03_start_01/hit` | 未重构时表现链疗起手和实际治疗接触。重构后攻击对应独立起手和命中。 |
| 新约能天使 | `char_1041_angel2` 普攻、三技能循环攻击起手的 `startEvent=4`。两种弹体的命中分别为 `angel2_attack_loop_hit_01`、`angel2_skill_3_loop_hit_01` | 出手时显示对应火光，实际接触时显示各自命中。同一枪轮同一时刻的五次伤害只叠一份相同的命中图集。 |
| 斩业星熊、凛御银灰 | 基础起手 `hsgma2_attack_01_start_01`；银灰 `Attack_A` 的 `svash2_attack_start_A_01/A_02` 及其三技能保留图集双层 | 星熊普通攻击补起手。银灰将默认 A 动作与两组 A 起手配对，棋盘三技能直线动作补齐可用的另一层。 |
| 望 | `projectile_chr_wang_stone1` 的布置 `wang_passive_01_buff`、触发 `wang_passive_01_buff_02`。本作保存棋子模型的起场 `wang_passive_01_start` | 布置、常驻与实际触发各就其位。守军望在本作不发普通弹体，触发时移除旧的普通弹体命中图集。 |
| 塞雷娅 | `char_202_demkni` 物伤 `demkni_attack_01_hit`，治疗 `common_heal_hit_01` | 真实治疗重新显示接触，与物伤接触分开。 |

`startEvent=2` 使用本作模拟时钟的动作起点，`startEvent=4` 使用实际出手点。命中使用已结算事件的时间和位置。同一来源、目标、时刻的相同命中图集只绘制一份。出手朝向从本次出手与目标位置取值，命中挂到受击身体。已成功绘制的守军命中图集接管原来的通用接触图形。

守军弹道这批沿用模型现有出手与结算点，独立飞行未接入。安赛尔的原拖尾仍缺可验证的采样。Mon3tr 的专用链疗连线、望的棋子额外命中图集和星熊原作多段盾击在这批未扩接。圣聆初雪首领沿用既有已核对的独立表现。现役棋盘没有给固守角色添加移动行为。

本次引用的原 `[uc]projectiles.ab` SHA-256 为 `cc3f21b73b10fbc619a1ff8b099f31e678c883ddacb7bb6775fea960ffecb0e1`。运行画面与用例以本轮 QA 和角色实机视频为准。
