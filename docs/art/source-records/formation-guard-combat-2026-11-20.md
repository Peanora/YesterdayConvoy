# 棋盘守军受击与出手素材

2026-11-20 读取本机《明日方舟》Windows 客户端资源清单 `26-10-20-11-10-38-04170990`。从 `charpack/char_*.ab` 的角色 `UberEffectEmitter` 或攻击组件读取事件，用 `battle/prefabs/[uc]projectiles.ab` 核对独立弹道对象和命中字段。棋盘继续使用先前采样并纳入项目的原特效图集，来源和许可见 [原守军特效记录](original-defender-vfx-2026-11-06.json)。没有新增原作音画文件。开放再分发许可仍未核实。

## 2026-11-21 原弹体与枪口校正

再次读取本机 Windows 客户端同一版本。独立弹体取 `battle/prefabs/[uc]projectiles.ab` 的 `SimpleProjectile._mainEffect`、`AdvancedMovement._speed` 与 `EffectBehaviour._effectsWhenHit`。正面发射点逐个取已保存原骨骼在 `OnAttack` 处的世界坐标，按图集实际 `.38` 采样比例及各角色原 `ppu` 换算到棋盘，反向时随身体镜像。距离使用棋盘平面距离，弹体在相同模拟时间内到达目标身体，接触依然由真实结算生成。

| 角色 | 原弹体和保留动作 | 发射点源像素（相对脚点） | 飞行源速度 |
| --- | --- | --- | --- |
| D05 月见夜 | `projectile_midn`、`projectile_midn_s1`，对应两组 `midn_*_trail` | `F_Muzzle` (116.09, 21.64) | 10 |
| D07 克洛丝 | `projectile_crossbow`、`common_gongjian_trail_01` | `F_Muzzle` (234.02, 238.66) | 10 |
| D08 史都华德 | `projectile_magic_ball`、`projectile_stward_s1`，对应 `common_magic_trail_01` 和 `stward_skill_01_trail` | `F_Muzzle` (196.08, 139.61) | 10 |
| D12 梓兰 | `projectile_orchid`、`orchid_attack_01_trail` | `Muzzle` (285.77, 139.18) | 10 |
| D21 荒芜拉普兰德 | `projectile_chr_whitw2`、`projectile_chr_whitw2_s3`，技能态本体也命中 `whitw2_hit_01`，独立追猎狼继续播放自己的命中 | 普通 `F_Muzzle` (90.84, 521.78)，技能三 (358.50, 191.20) | 10 |
| D23 Mon3tr | `projectile_chr_monstr`、`monstr_attack_trail`，原本体治疗的主弹体发向首次治疗目标 | `F_Muzzle` (-112.00, 397.60) | 10 |
| D24 新约能天使 | `projectile_chr_angel2`、`projectile_chr_angel2_s3`，两种枪光、弹体和身体命中分别使用各自源图集 | `F_Muzzle` (203.58, 166.23)，棋盘正向约 (0.593, 0.484) | 30 |

新约能天使默认前向模式 `charpack/char_1041_angel2.ab` 的 `RangedAttack` 为 `0x36b7d9a8ea71e4ef`，三技能循环 `MultiRangedAttack` 为 `0x655c1903482be4ef`，两者 `_mountPointType=2`。棋盘保留的普通身体片段为 `Attack_Loop`，在第 16 个 60 fps 源帧出手。前向 `Skill_3_Loop` 的五次 `OnAttack` 依次位于第 10、12、14、16、18 帧。枪光为该轮第一发触发，五颗弹各自到达后保留原有数值的五次伤害。两种弹体均为直线，默认 2× 的四格飞行约 0.067 秒。棋盘保留固守方朝向及四轮技能，没有接原客户端向投递目标发射的额外阶段。克洛丝的强化多次同一事件、史都华德多目标、梓兰疾速及 Mon3tr 多人治疗保留棋盘既有技能规则，使用可追溯的主弹体。Mon3tr 独立治疗链条仍缺已验证的端点。安赛尔仍缺可运行飞行采样。

斩业星熊的正面 `skill_attack` 使用 `Skill_3_Loop_A`。本机 `charpack/char_1044_hsgma2.ab` 的 S3 `Attack_Anim_A` 发射器路径 ID 为 `0xd653746ee43b21e2`，在 `_startEvent=2` 播放 `hsgma2_skill_03_start_01`。棋盘仅有一段实际打击，对应首组 `hsgma2_skill_03_hit_01`。原作第二组 `hsgma2_skill_03_hit_01_1`、其余动作变体和临死后的四段分支保留为缺口。凛御银灰第三层 `svash2_skill_03_combat_start_01` 仍缺武器骨骼驱动的原拖尾，望的 `token_wang_skill_01_hit` 仍缺可运行采样。

炎熔原 Actor 的 `startEvent=4` 保持无飞行。普攻和本作强化沿用前向攻击帧的 0.833333 秒标记，发射当刻定位到目标格。地面层置于受击者下方，独立受击保持在身体接触位置。

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

## 三星普通攻击与治疗接触

从已采样的官方正面骨骼重新读取 `OnAttack`，逐一核对片段名和原长度与本作图集清单一致。棋盘先播放起手再播放主段的角色，把起手原长加到主段事件上。下表时间为秒，棋盘在默认 2× 时取源时长的一半。此项只补普通动作与普通治疗的时点，技能仍各按本作已绑定的规则。

| 守军 | 正面片段 | 起手源时长 | 主段事件 | 总源时点 |
| --- | --- | ---: | ---: | ---: |
| D01 米格鲁 | Attack | — | 0.500000 | 0.500000 |
| D02 卡缇 | Attack | — | 0.466667 | 0.466667 |
| D03 斑点 | Attack | — | 0.500000 | 0.500000 |
| D04 玫兰莎 | Attack_Loop | 0.500000 | 0.466667 | 0.966667 |
| D05 月见夜 | Attack_Loop | 0.533333 | 0.500000 | 1.033333 |
| D06 翎羽 | Attack | — | 0.333333 | 0.333333 |
| D07 克洛丝 | Attack | — | 0.433333 | 0.433333 |
| D08 史都华德 | Attack | — | 0.500000 | 0.500000 |
| D09 炎熔 | Attack | — | 0.833333 | 0.833333 |
| D10 芙蓉 | Attack | — | 0.466667 | 0.466667 |
| D11 安赛尔 | Attack | — | 0.333333 | 0.333333 |
| D12 梓兰 | Attack | — | 0.566667 | 0.566667 |

以炎熔为例，原正面普攻标记位于第 50 个 60 fps 源帧，棋盘默认攻击率下于动作开始后约 0.417 秒触发。三星保留角色各自的攻击间隔，接触时间计入实际施法或治疗。原正面文件分别来自本机 `昨日圆车_美术预览` 内的首批八名和其余二十二名守军留存单，`DxxF/source`；炎熔本次读取的 `char_121_lava.skel` SHA-256 为 `271d8b92fec2450280d96504e12a33e0daeb3213a5ba6117ca0c49cdadcf29f7`。

11 月 20 日版本尚未接通守军独立飞行。安赛尔的原拖尾仍缺可验证的采样。Mon3tr 的专用链疗连线、望的棋子额外命中图集和星熊原作多段盾击在这批未扩接。圣聆初雪首领沿用既有已核对的独立表现。现役棋盘没有给固守角色添加移动行为。

本次引用的原 `[uc]projectiles.ab` SHA-256 为 `cc3f21b73b10fbc619a1ff8b099f31e678c883ddacb7bb6775fea960ffecb0e1`。运行画面与用例以本轮 QA 和角色实机视频为准。
