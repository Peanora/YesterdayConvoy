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

### 2026-11-21 复核并重接塔露拉原包层

重新读取本机 PC 客户端 `26-10-20-11-10-38-04170990` 的 `battle/enm_pfb_18.ab`、`pkgrps/btl_pfb_fx_enemy_1.ab`、`battle/prefabs/[uc]projectiles.ab` 与 `refs/arts/enm_art_16.ab`。两个形态的 Ignite 与 DragonFireExplode 对应 `Skill_1` 和 `Skill_2`。`ScreenAttack` 的原动作字段为空，其独立投射物为 `projectile_enemy_bsnake`。与保留的官方 Android `26-10-29-15-36-11-33649006` 资源相比，塔露拉 `.skel` 和 `.atlas` 的 SHA-256 相同。点燃、龙炎及火浪的 21 个对照特效根、284 个节点路径一致，归一化对象引用后比较的 774 个变换、粒子、拖尾、网格及 Animator 组件字段一致。保留样本用 Android 资源离线采样，平台纹理格式和镜头各异。保留原权利状态。

| 本作表现 | 对应原资源及播放时点 |
|---|---|
| 三费点燃 | `ignite-A/B-caster` 从施法开始播放，`enemy_bsnake_skill_01_hit` 与 reborn 变体从实际命中播放。身体 `Skill_1_A/B` 的原接触标记为 1.167 源秒，基础 2× 时约 0.583 对局秒。 |
| 四、五费龙炎 | `explode-A/B-caster` 和 `explode-A/B-target` 一起从 `ON_CAST_START` 开始。目标层挂在本作已选守军；原两层 `enemy_bsnake_skill_02_hit`、`enemy_bsnake_skill_02_hit_02` 及对应 reborn 变体。`Skill_2_A/B` 的接触标记为 1.033 源秒，基础 2× 时约 0.517 对局秒。 |
| 五费横向火浪 | 替换成 `projectile_enemy_bsnake` 指定的 `enemy_bsnake_reborn_01_trail`。每条火流使用原效果朝向，按源速 2.5 原世界单位/秒和 2× 时钟前进，行起势延差使用源 0.100 秒上限，逐格贴右侧八行的实际地面。 |
| 五费实际直接命中 | `enemy_bsnake_reborn_01_trail_hit_01` 播完整原采样，原首帧可以透明。灼烧结算无额外爆闪。五费播完整 `Skill_2_A/B` 后回本形态待机，8 秒期间仍保持既定攻击锁。 |

本作仍按原有六段直伤与八秒灼烧结算。原 `ScreenAttack` 配置的波间距为 2 源秒。以 30 fps 分析留存的原作 2× 实录，橙色火流跨同一区域的相关峰值相隔 30 帧，关键通过时间为 2.967、3.967、4.967、5.967 和 6.967 秒。棋盘以相同的 2× 表现速率每 1 对局秒发射下一波视觉，发射边界及行间相位适配当前场地。首波从本作烧区开始，原作复生到首波的绝对偏移仍缺确证。原 `ScreenAttack` 属复生阶段并依源投射物拦截，本作用作独立渲染层，火流位置不产生新的伤害、声音或命中。四费保留真实烧区中心持续层，使用 `enemy_bsnake_skill_01_buff` 的既有循环采样。

`tools/package_talulah_native.py --verify-only` 回读原 30 fps HDR 采样和 Git PNG，以记录中的镜像、裁切及补边逐帧校对，39 段、83 页、4218 帧一致。Windows Player 仅导出已绑定的补充包 56 页。火流及命中保留 RGBA32 预处理，避免压缩改变源 HDR 梯度。原资源片段、2× 对照和关键帧位于 `artifacts/formation-original-fidelity/reference`。场景播放与复核结果见本轮 QA。
