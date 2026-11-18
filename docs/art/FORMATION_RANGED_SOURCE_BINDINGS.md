# 棋盘旧怪物：原弹道核对

2026-11-18，逐节点读取本地官方版本 `26-10-29-15-36-11-33649006` 的 `battle/prefabs/[uc]projectiles.ab`，没有下载、重采样或根据动画片长猜测飞行。

| 角色 | 原 projectile | 原 mainEffect | 原命中 |
|---|---|---|---|
| M11 雪怪狙击手 | projectile_crossbow | common_gongjian_trail_01 | common_hit_01 |
| M12 雪怪术师 | projectile_enemy_snow_magic_ball | snmage_attack_01_trail | snmage_attack_01_hit |
| M16 游击队狙击手 | projectile_sotisp | sotisp_attack_01_trail | sotisp_attack_01_hit |
| M26/M42 泥岩掷能者 | projectile_enemy_mdsnpr | enemy_mdsnpr_attack_01_trail | enemy_mdsnpr_attack_01_hit |
| M36 首言者 | projectile_enemy_dsmant | enemy_dsmant_attack_01_trail | enemy_dsmant_attack_01_hit |
| M38 萨卡兹术师 | projectile_enemy_magic_ball | common_magic_trail_01_m | common_magic_hit_01_m |

以上六种原弹道均为 `AdvancedMovement`，`_speed=10` 原单位/秒、`_delayToStart=0`、`_useDynamicSpeed=0`、`_deltaSpeedPerSec=0`；本作距离换算仍须与各原身体的标尺一致，不能仅因名字相近套成统一延时`.25`。M24近战、M31喷火不在该表内。

M11两条共用资源原本就已存在于正式 `OriginalCombatVfx/Monsters` 清单与图集；本轮只补Formation预热、真实普攻飞行、实际命中绑定，没有新增美术。`MonsterOriginalVfx.RequiredKeys`历史M11空数组不代表其原作没有弩箭。无独立技能弹道事件时，不据单次技能命中伪造飞行。

原节点、完整movement字段、原bundle SHA256留在本轮证据 `artifacts/formation-visual-repair/original-ranged-projectiles.json`；源Actor的卡牌对应分别见既有 `RANGED_MONSTER_NATIVE_BINDINGS.json` 与 `SPECIAL_MONSTER_NATIVE_BINDINGS.json`。

## M17 传令兵单独核对

原 `enemy_1080_sotidp` 位于 `battle/enm_pfb_5.ab`。`Default/Body/Combat` 确实是 `MeleeAttack`，`_animKey=Attack`、`_atkScale=1`、`_waitForAttackEvent=1`；UnitMode的`_combat`指向此能力，远程`_attack`为空。已查看现有原Attack序列的0、.25、.5、.75秒：实际为抬腿、踢腿、收势，**不是吹号动作**。

源能力证明原资源具有近战攻击路径，不证明原客户端每个关卡配置都会启用该路径；当前未读取对应敌人关卡数据库配置，不能声称其原作完全不能攻击。原 `Talents/Strength` 为 `GlobalAuraAbility`，引用 `sotidp_buff_01_range`；不能把踢腿动作改称号令技能，也不能将本作range3导致的远程踢空气说成原作弹道。

证据为 `artifacts/formation-visual-repair/M17-original-actor.json` 与 `M17-original-attack-contact.png`。本核对只读源与现有帧，不修改v0.7数值、M17能力或模型。

## 塔露拉主技身体与施法者层

末轮直接重读本地原 `battle/enm_pfb_18.ab`，而非据当前画面或旧加工命名推断：

| 原Actor字段路径 | 原能力、动画 | 已有原采样caster |
|---|---|---|
| `enemy_1515_bsnake/Modes/Default/Skills/Ignite` | `AnimatedActionToTargetAbility._animKey=Skill_1` → `Skill_1_A` | `ignite-A-caster`（skill_01_start／start_02／start_03） |
| `enemy_1515_bsnake/Modes/Reborn/Skills/Ignite` | 同上 → `Skill_1_B` | `ignite-B-caster`（原reborn变体，含start_04） |
| `enemy_1515_bsnake/Modes/Default/Skills/DragonFireExplode` | `EmptyAnimatedAbility._animKey=Skill_2` → `Skill_2_A` | `explode-A-caster`（skill_02_start_02／start_03／range） |
| `enemy_1515_bsnake/Modes/Reborn/Skills/DragonFireExplode` | 同上 → `Skill_2_B` | `explode-B-caster`（原reborn变体，另含start_04） |

三费本作点燃复用Skill_1_A/B及ignite施法者层；四/五费本作范围火焰复用Skill_2_A/B及explode施法者层。此前body两行反配，且`firecast-A-hand`加工名掩盖了它实际只是原`enemy_bsnake_skill_01_start_02`／Skill_1_A手部层；现已以原Actor字段对齐，不能再凭“看得见火焰”判定配对正确。

四/五费的实际命中、雨与烧区仍是v0.7真实Hit／Zone驱动的适配。原DragonFireExplode、ScreenAttack与本作技能规则不是同一机制；没有因此新增全屏爆炸、原客户端火雨调度、伤害范围或额外结算。原字段摘录为 `artifacts/formation-visual-repair/TAL-raw-actor-skills.json`，原采样来源对应为 `TAL-original-skill-map.json`。c610583影片为修正之前的对照，不作本配对修正后的GPU验证。
