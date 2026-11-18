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
