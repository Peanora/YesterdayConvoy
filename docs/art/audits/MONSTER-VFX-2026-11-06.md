# 玩家怪物特效替换审计（2026-11-06，只读）

核对工程：E:/珍贵资料/昨日圆车，42卡；没有修改Unity工程、分支、索引或启动主Unity。角色原动作已存在不等于外部粒子已还原。以下仅审计运行效果，不把血条、跳字、路线、选择圈、技能就绪图标计为角色特效。

## 需替换的运行效果

| 优先 | 角色/覆盖范围 | 当前事件与实际读取点 | 当前类型 | 原资源与缺口 |
|---|---|---|---|---|
| P0 | M40维维美 | EngagementView.cs:190-275，普通/强化弹道、命中漫画爆炸及大头 | LineRenderer弹道/爆炸+用户GIF表情；不是原原作粒子 | 大头是人类指定风格，需保留；倒走/范围结算归根任务，不碰。原爆裂黎明FX需另定位，不能默认换正常皮肤角色 |
| P0 | M14霜星 | EngagementView.cs:94-101自动冰环；FrostSealView.cs读取FrostSeals与SnowstormPulses | 自动环是程序线圈；手动冰封为程序风雪+image_gen黑冰，不是原作粒子 | 当前角色原Skill_1动作有；原独立FX尚未进Assets。已取frstar2.ab 15原Prefabs，明确是另一形态候选；当前enemy_1505_frstar第一形态FX继续定位，不能冒称同版 |
| P0 | M31焚毁者 | FlamerazerEffectView.cs:28-60，FlameSprays/FuelExplosions | 五条程序火焰线带和冲击圆 | 已取enemy4原包，确认enemy_duspfr_2_attack_01_front_start2/attack_01_hit及die_01/die_01_hit/die_02/die_03；现有M31模型enemy_1173_duspfr需核对变体/挂点，尚未采样 |
| P1 | M04高能源石虫、M06冰爆源石虫 | MonsterDeathEffectView.cs:28-57，DeathBlasts真实预警/爆炸 | 线圈+12根冰片/爆炸碎线 | 角色原死亡动作有，独立爆炸FX未入Assets；需查原slime/爆虫变体，不能仅用普通敌死亡冒充爆炸 |
| P1 | M33囊海爬行者 | NeuralEffectView.cs:27-43，CreeperPulses | 16组程序尖刺 | 已取enemy4，enemy_dsurch_01_start和_02存在；需核对原前后层和目标影响，尚未采样 |
| P1 | M34钵海收割者 | NeuralEffectView.cs:45-53，Harvester Combat神经光环 | 程序圆环 | 已取enemy3，enemy_dslntc_buff_01/buff_02/skill_begin_buff/attack_01_hit存在；原狂暴身体动作已有，不重绘 |
| P1 | M36首言者 | NeuralEffectView.cs:67-92，CorrosiveBubbles的球/地块/激光及普通弹道 | 程序圆/五格线框/直线 | 已取enemy4，dsmant_attack_01_start/trail/hit、attack_02_start/02/hit及skill_01_start/buff/02/03/hit存在；需保持现有液泡脉冲事件，不新增技能规则 |
| P1 | M38萨卡兹术师 | NeuralEffectView.cs:54-66，Shackles | 折线锁链+封印椭圆 | 已取dmage.ab，dmage_skill_01_start/line/buff三个原Prefab；已有原技能身体动作，不需AI生成 |
| P2 | M30逐火者 | DemoSpriteVisual.cs:261-270原余烬/复活身体；普攻仍EngagementView | 原身体已好；外部命中沿用程序圆 | 已取enemy4，duskls_attack_start/hit、weapon_buff、idle_2、revive_2/3/4存在；只补FX，不改5次受击余烬/10秒复活逻辑 |
| P1共用 | 远程M05/M08/M09/M11/M12/M16/M26/M42 | EngagementView.cs:190-275，CombatShot.StartedAt/ImpactAt/Applied | 直线/抛物线短线、彩色命中圆与射线，按酸液/法术/寒冷换色 | common.ab已实际获取并核对MD5；common_hit_01/magic_hit_01/ranger_hit_01/enemy_hit_01/firemagic_hit_01/cuting_hit_01可构成统一原作基底。具体毒弹/箭矢/冰弹仍需对应原projectile，不把通用hit说成完整弹道 |
| P1共用 | 近战/承伤/被动 M01/M02/M03/M07/M10/M13/M15/M17/M18/M19/M21/M22/M23/M24/M27/M29/M35/M37/M43（及上列有普攻者） | EngagementView.cs:289-304，PresentMelee | .16秒小圆圈；角色挥击原动作本身不改 | 可共享common物理/斩击命中；M17号令、M27/43法术屏障没有单独自制外部导演，不虚构其“已有独特特效待替换”。角色原动画中的自带视觉继续保留 |
| P2共用 | 全体生成/最终死亡 | EngagementView.cs:170-183与360，AddPuff | .35/.5秒青/红线圈，原作角色死亡帧另播 | common_char_birth_01/common_enemy_die_01候选可统一；避免给已有完整原死亡导演的角色叠加旧圈 |
| P2羁绊适配 | 深池火种、感染爆裂、泥岩巨像冲击、雪怪碎冰 | EngagementView.cs:70-93，DeepSeeds/InfectionBursts/GolemPulses/ShatterEvents | 程序标记/扩散圆 | 这些为本作新羁绊，没有一对一原作规则。可用确认的原公共粒子适配真实事件，但要标“本作组合”，不能称原机制照搬；独立告警/部署标识保留功能性 |

表按事件分组，角色存在交叉，不用行数冒充独立角色数。完整42卡仍以docs/MONSTER_COMPLETION_AUDIT.md为准，银狼未制作。

## 已原素材，保留而非重做

| 角色 | 已绑定导演 | 保留范围 |
|---|---|---|
| M20爱国者 | PatriotEffectView.cs、Resources/PatriotVfx/catalog | 原四连击/复起/投矛/灼烧，不用泛用圈重新包一遍 |
| M25梅菲斯特 | MephistoEffectView.cs、MephistoVfxArt.cs | 原治疗施法/命中/自然回复增幅起止 |
| M28泥岩 | MudrockVfxView.cs、MudrockVfxArt.cs | 原普攻/三层屏障/补盾/占据/留声机原动画；装置攻击/治疗细线为本作连接反馈，不当成角色自制粒子重复重做 |
| M32蔓德拉 | MandragoraEffectView.cs、MandragoraVfxArt.cs | 原石盾/凝视/重生/悬浮/支柱，不新增地图支柱或技能规则 |
| M39塔露拉 | TalulahAttackVfx.cs、OriginalTalulahArt、FireRainView | 已原普攻/A-B/阶段层全部保留；人类确认的早期橙色穿透火浪是明确例外，不能为“全原作”撤掉 |

## 本轮外部来源与工具

- 实际获取记录：同目录ACQUISITION.json（官方固定版本26-10-29-15-36-11-33649006，内层AB MD5逐项等于缓存官方hot_update_list）。DAT是ZIP外壳，存档与解包AB分开；不拿ZIP MD5与AB MD5混比。
- 复用外部2026-11-05_塔露拉原作重制-v13/native-baker的NativeJobs.Bake和Vulkan原Shader采样；本轮另建monsters/native-baker，不与守军代理共写，不启动主工程。
- 当前仅原包/Prefab存在性已证实，尚未将以上新FX采样/接入或验为VERIFIED。原资源权利归Hypergryph，公开再分发许可未确认，下载/人类确认不是权利人授权。
