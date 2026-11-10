# 数值v2：九条羁绊、支援体与特殊规则

2026-11-08基线记录（历史）。**人类已同意登记的数值v2试验基线；当时尚未接入Unity，不是最终平衡或实机验证结论。** 数值归属与适用边界见[总则](README.md)。

2026-11-10最新覆盖（人类确认）：整合运动2/4/6/8档新增成员最大生命与攻击＋10/20/30/40%，包含五费角色，援军及其他派生体不再独立叠加。本条适用于正式行动v2与旧羁绊专项；原援军、总星缩放和高档编制门槛保持。实际接入与验证状态见[STATUS](../../STATUS.md)及[QA](../../QA.md)，不把本表参数视作最终平衡结论。

全部9条羁绊、29档均显式列值；档位只取最高达成档，例外副形态按各条规定。机制沿既定快照、触发和派生边界，不把无限测试便利入口视为正式获取成本。

## 通用结算

| 参数/规则 | 值 |
| --- | --- |
| snapshot | 战前编队不同种类与实际出战星级锁定；同名只计出战实例，备战、支援、重复部署不计；死亡、穿门、跨波不改档。 |
| time | 所有逻辑使用共享模拟时钟；暂停冻结；波次跨波保留双方、CD、次数、SP、资源与待结算事件。 |
| tier | 同羁绊只取最高档，不叠加低档；六感染的二档副形态是已定例外。 |
| derived | 援军、灵体、巨像、感染副体均为支援；不产军令、不再牧群转化、不触发羁绊递归、不作为羁绊人数或总星。派生伤害不递归产军令/碎冰；仅深池自身燎烧保留已定传火例外。 |
| noNewGlobalCap | 不引入统一伤害上限、回费上限或额外全局CD。各机制现有次数、资源、目标上限分别控制。 |
| scaling | 整合Q=(S/K)^0.5；雪怪Q=(S/K)^0.4；深池Q=(S/K)^(1/3)。S为该羁绊全部出战成员总星，K为最高激活档门槛。其余不额外套成员星级倍率。 |
| stacking | 攻击星级先生成一次面板。整合成员加成与萨卡兹生命/攻击加成、传令/爱国者等同类队伍加成在对应加法项合并；原生阶段倍率仍另乘。雪怪/深池各自独立伤害不再乘承契攻击；海嗣只加神经积累、不乘普通伤害；军令读取已修饰攻击一次。 |
| supportGate | 全部支援体穿门0；感染集群仅主实体保留原穿门，副体0。已核对EngagementCombat.RegisterStrongholdBreach出口按SynergySupport直接扣0，构造参数中的Gate=1不会使正式交战支援扣蓝门。 |

## 整合运动（reunion）

成员：猎狗（CARD_M02）、游击队猎犬（CARD_M03）、冰爆源石虫（CARD_M06）、士兵（CARD_M07）、弩手（CARD_M08）、术师（CARD_M09）、重装防御者（CARD_M10）、雪怪狙击手（CARD_M11）、雪怪术师（CARD_M12）、雪怪小队凿冰人（CARD_M13）、霜星（CARD_M14）、游击队战士（CARD_M15）、游击队狙击手（CARD_M16）、游击队传令兵（CARD_M17）、游击队盾卫（CARD_M18）、游击队萨卡兹战士（CARD_M19）、爱国者（CARD_M20）、宿主士兵（CARD_M21）、宿主拾荒者（CARD_M22）、宿主流浪者（CARD_M23）、梅菲斯特（CARD_M25）、塔露拉（CARD_TALULAH_TRIAL）。

作用范围：战前编队整合成员（含五费）按本场最高达成档获得最大生命与攻击加成；成员成功部署计调集，免费援军在触发者当前路线生成。援军本身不再次调集，也不独立获得成员加成。

星级缩放：成员加成为已升星面板上的固定档位比例，不另乘总星倍率；仅援军HP与攻击×sqrt(S/K)，分别四舍五入，护甲/范围/数量/次数不乘。

### 2档

整合成员最大生命与攻击＋10%；每部署2种不同整合成员调集1近战援军，最多1批。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 成员最大生命与攻击加成 | `memberStatBonus` | 0.10 | 比例 |
| 调集所需编队 | `requiredFormations` | 1 | 见参数名称 |
| 每批所需不同种类 | `distinctKindsPerMuster` | 2 | 见参数名称 |
| 调集批次上限 | `musterLimit` | 1 | 见参数名称 |
| 每批援军数 | `supportsPerMuster` | 1 | 见参数名称 |
| 近战援军生命 | `meleeHP` | 100 | 见参数名称 |
| 近战援军攻击 | `meleeAttack` | 16 | 见参数名称 |
| 近战援军护甲 | `meleeArmor` | 0 | 见参数名称 |
| 攻击间隔 | `intervalSeconds` | 1.8 | 模拟秒 |
| 近战援军范围 | `meleeRange` | 0.75 | 距离单位 |
| 移速 | `speed` | 0.7 | 见参数名称 |
| 入场护盾生命比例 | `entryShieldFraction` | 0 | 倍率 |
| 入场护盾时长 | `entryShieldSeconds` | 0 | 模拟秒 |

### 4档

整合成员最大生命与攻击＋20%；每3种成员调集1名整备时选定近战/弩手，最多2批。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 成员最大生命与攻击加成 | `memberStatBonus` | 0.20 | 比例 |
| 调集所需编队 | `requiredFormations` | 1 | 见参数名称 |
| 每批所需不同种类 | `distinctKindsPerMuster` | 3 | 见参数名称 |
| 调集批次上限 | `musterLimit` | 2 | 见参数名称 |
| 每批援军数 | `supportsPerMuster` | 1 | 见参数名称 |
| 近战援军生命 | `meleeHP` | 150 | 见参数名称 |
| 近战援军攻击 | `meleeAttack` | 24 | 见参数名称 |
| 近战援军护甲 | `meleeArmor` | 2 | 见参数名称 |
| 远程援军生命 | `rangedHP` | 110 | 见参数名称 |
| 远程援军攻击 | `rangedAttack` | 30 | 见参数名称 |
| 远程援军护甲 | `rangedArmor` | 0 | 见参数名称 |
| 攻击间隔 | `intervalSeconds` | 1.8 | 模拟秒 |
| 近战援军范围 | `meleeRange` | 0.75 | 距离单位 |
| 远程援军范围 | `rangedRange` | 2.7 | 距离单位 |
| 移速 | `speed` | 0.7 | 见参数名称 |
| 入场护盾生命比例 | `entryShieldFraction` | 0 | 倍率 |
| 入场护盾时长 | `entryShieldSeconds` | 0 | 模拟秒 |

### 6档

至少2编制；整合成员最大生命与攻击＋30%，每3种成员调集1近战+1弩手，最多2批。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 成员最大生命与攻击加成 | `memberStatBonus` | 0.30 | 比例 |
| 调集所需编队 | `requiredFormations` | 2 | 见参数名称 |
| 每批所需不同种类 | `distinctKindsPerMuster` | 3 | 见参数名称 |
| 调集批次上限 | `musterLimit` | 2 | 见参数名称 |
| 每批援军数 | `supportsPerMuster` | 2 | 见参数名称 |
| 近战援军生命 | `meleeHP` | 210 | 见参数名称 |
| 近战援军攻击 | `meleeAttack` | 32 | 见参数名称 |
| 近战援军护甲 | `meleeArmor` | 4 | 见参数名称 |
| 远程援军生命 | `rangedHP` | 150 | 见参数名称 |
| 远程援军攻击 | `rangedAttack` | 40 | 见参数名称 |
| 远程援军护甲 | `rangedArmor` | 1 | 见参数名称 |
| 攻击间隔 | `intervalSeconds` | 1.8 | 模拟秒 |
| 近战援军范围 | `meleeRange` | 0.75 | 距离单位 |
| 远程援军范围 | `rangedRange` | 2.7 | 距离单位 |
| 移速 | `speed` | 0.7 | 见参数名称 |
| 入场护盾生命比例 | `entryShieldFraction` | 0 | 倍率 |
| 入场护盾时长 | `entryShieldSeconds` | 0 | 模拟秒 |

### 8档

至少3编制；整合成员最大生命与攻击＋40%，每3种成员调集精英近战+弩手，最多3批。各精英首次承受合法守军攻击时获得20%最大生命盾，5秒，一次。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 成员最大生命与攻击加成 | `memberStatBonus` | 0.40 | 比例 |
| 调集所需编队 | `requiredFormations` | 3 | 见参数名称 |
| 每批所需不同种类 | `distinctKindsPerMuster` | 3 | 见参数名称 |
| 调集批次上限 | `musterLimit` | 3 | 见参数名称 |
| 每批援军数 | `supportsPerMuster` | 2 | 见参数名称 |
| 近战援军生命 | `meleeHP` | 280 | 见参数名称 |
| 近战援军攻击 | `meleeAttack` | 44 | 见参数名称 |
| 近战援军护甲 | `meleeArmor` | 6 | 见参数名称 |
| 远程援军生命 | `rangedHP` | 200 | 见参数名称 |
| 远程援军攻击 | `rangedAttack` | 54 | 见参数名称 |
| 远程援军护甲 | `rangedArmor` | 2 | 见参数名称 |
| 攻击间隔 | `intervalSeconds` | 1.8 | 模拟秒 |
| 近战援军范围 | `meleeRange` | 0.75 | 距离单位 |
| 远程援军范围 | `rangedRange` | 2.7 | 距离单位 |
| 移速 | `speed` | 0.7 | 见参数名称 |
| 入场护盾生命比例 | `entryShieldFraction` | 0.2 | 倍率 |
| 入场护盾时长 | `entryShieldSeconds` | 5 | 模拟秒 |

### 交互与边界

- 成员加成战前锁定，仅取最高达成档；重复部署、跨波、死亡和复起不再次叠加。援军、感染副体和牧群灵体不作为独立成员再次获得加成，既有派生预算/继承口径保持。
- 成员生命/攻击加成与其他同类队伍加成相加，原生阶段倍率另乘。固定技能基值不重写；读取现有攻击倍率的技能仍按原口径结算，塔露拉手动六波火雨固定基值保持。
- 每批清空本批不同种类记录，后续重复部署可重新贡献种类。
- 援军不吃萨卡兹、感染、牧群、深池、溟痕成员收益；仍可吃现有全队传令光环，其影响须另计。

- 8档必须人数与编制同时满足；未满足编制按低档回退。
- 本表比当前援军略强，但不把援军做成五费替代品。

## 雪怪小队（snow）

成员：冰爆源石虫（CARD_M06）、雪怪狙击手（CARD_M11）、雪怪术师（CARD_M12）、雪怪小队凿冰人（CARD_M13）、霜星（CARD_M14）。

作用范围：冻住守军后由全队不同普通卡种协同碎冰；天气全场守军。

星级缩放：碎冰与天气伤害统一Q=(S/K)^0.4；不再乘攻击倍率；控制时长/保护/寒意不随星级增长。

### 2档

被冻结守军在2.5秒内被2种普通单位命中，碎冰50Q法伤，最多2目标；二档消费当前寒冷/冻结。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 所需不同命中种类 | `requiredDistinctHitKinds` | 2 | 见参数名称 |
| 命中窗口 | `hitWindowSeconds` | 2.5 | 模拟秒 |
| 目标碎冰冷却 | `targetShatterCooldownSeconds` | 4 | 模拟秒 |
| 碎冰法伤 | `shatterArts` | 50 | 见参数名称 |
| 碎冰半径 | `shatterRadius` | 0.9 | 距离单位 |
| 碎冰目标数 | `shatterTargets` | 2 | 见参数名称 |
| 消耗冻结 | `consumeFreeze` | 是 | 见参数名称 |
| 冻结时长 | `freezeSeconds` | 1.2 | 模拟秒 |
| 基础冻结保护时长 | `baseFreezeProtectionSeconds` | 1.2 | 模拟秒 |
| 寒冷攻速倍率 | `coldAttackRateMultiplier` | 0.7 | 倍率 |
| 天气次数 | `weatherCharges` | 0 | 见参数名称 |
| 天气持续 | `weatherDurationSeconds` | 0 | 模拟秒 |

### 5档

碎冰100Q法伤、最多5目标；成功碎冰每次积1寒意，3点触发8秒暴风雪。四次寒潮对每名存活守军各造成(40+1.25%目标最大生命)Q法伤并按既有寒冷/冻结/保护结算。霜星手动冰封起手可启动同一天气。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 所需不同命中种类 | `requiredDistinctHitKinds` | 2 | 见参数名称 |
| 命中窗口 | `hitWindowSeconds` | 2.5 | 模拟秒 |
| 目标碎冰冷却 | `targetShatterCooldownSeconds` | 3 | 模拟秒 |
| 碎冰法伤 | `shatterArts` | 100 | 见参数名称 |
| 碎冰半径 | `shatterRadius` | 1.5 | 距离单位 |
| 碎冰目标数 | `shatterTargets` | 5 | 见参数名称 |
| 消耗冻结 | `consumeFreeze` | 否 | 见参数名称 |
| 冻结时长 | `freezeSeconds` | 1.2 | 模拟秒 |
| 基础冻结保护时长 | `baseFreezeProtectionSeconds` | 1.2 | 模拟秒 |
| 寒冷攻速倍率 | `coldAttackRateMultiplier` | 0.7 | 倍率 |
| 天气次数 | `weatherCharges` | 3 | 见参数名称 |
| 天气持续 | `weatherDurationSeconds` | 8 | 模拟秒 |
| 天气脉冲时刻[0] | `weatherPulseAt[0]` | 0 | 模拟秒 |
| 天气脉冲时刻[1] | `weatherPulseAt[1]` | 2 | 模拟秒 |
| 天气脉冲时刻[2] | `weatherPulseAt[2]` | 4 | 模拟秒 |
| 天气脉冲时刻[3] | `weatherPulseAt[3]` | 6 | 模拟秒 |
| 天气固定法伤 | `weatherFlatArts` | 40 | 见参数名称 |
| 天气目标生命比例 | `weatherTargetMaxHPFraction` | 0.0125 | 倍率 |
| 天气寒冷时长 | `weatherColdSeconds` | 3.5 | 模拟秒 |
| 天气额外冻结保护 | `weatherExtraFreezeProtectionSeconds` | 3 | 模拟秒 |

### 交互与边界

- 任何不同普通卡种可凑双击，支援不算；一次碎冰命中多人只积1寒意。
- 天气期间不积寒意、不叠加不续时；自动冰环/不足5档冰封不启动天气。
- 寒冷/冻结保护只阻控制不阻寒潮伤害；派生碎冰与天气不产下一次碎冰或军令。

- 保留2/5档与3次碎冰天气机制，仅压低5档跨目标伤害，以适配后期总有效HP3500–6500标尺。

## 感染生物（infection）

成员：源石虫（CARD_ORIGINIUM_SLUG）、猎狗（CARD_M02）、游击队猎犬（CARD_M03）、高能源石虫（CARD_M04）、酸液源石虫（CARD_M05）、冰爆源石虫（CARD_M06）。

作用范围：仅普通感染成员部署生效；2/4档选主形态，6档再选不同二档副形态。

星级缩放：集群HP/攻击读取角色已升星面板再乘一次各自比例；爆裂=档位基值×[1+0.30×(该卡星级−1)]；盾读变形后实体MaxHP，不再套能力倍率。

### 2档

集群：共2个实体（含主实体），各55%HP/攻击；甲壳：18%最大生命盾6秒；爆裂：主实体最终死亡35×个人星级式法伤，最多2人。三选一。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 集群体数 | `swarmBodies` | 2 | 见参数名称 |
| 副体生命继承 | `swarmHPFraction` | 0.55 | 倍率 |
| 副体攻击继承 | `swarmAttackFraction` | 0.55 | 倍率 |
| 甲壳护盾比例 | `shellShieldFraction` | 0.18 | 倍率 |
| 甲壳护甲加成 | `shellArmorAdd` | 0 | 见参数名称 |
| 甲壳护盾时长 | `shellShieldSeconds` | 6 | 模拟秒 |
| 爆发基础法伤 | `burstBaseArts` | 35 | 见参数名称 |
| 爆发半径 | `burstRadius` | 1 | 距离单位 |
| 爆发目标数 | `burstTargets` | 2 | 见参数名称 |
| 爆发延迟 | `burstDelaySeconds` | 0.2 | 模拟秒 |

### 4档

集群2体各65%；甲壳28%盾+2甲；爆裂70基值、最多3人。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 集群体数 | `swarmBodies` | 2 | 见参数名称 |
| 副体生命继承 | `swarmHPFraction` | 0.65 | 倍率 |
| 副体攻击继承 | `swarmAttackFraction` | 0.65 | 倍率 |
| 甲壳护盾比例 | `shellShieldFraction` | 0.28 | 倍率 |
| 甲壳护甲加成 | `shellArmorAdd` | 2 | 见参数名称 |
| 甲壳护盾时长 | `shellShieldSeconds` | 6 | 模拟秒 |
| 爆发基础法伤 | `burstBaseArts` | 70 | 见参数名称 |
| 爆发半径 | `burstRadius` | 1.2 | 距离单位 |
| 爆发目标数 | `burstTargets` | 3 | 见参数名称 |
| 爆发延迟 | `burstDelaySeconds` | 0.2 | 模拟秒 |

### 6档

主形态：集群3体各50%、甲壳38%盾+3甲、爆裂110基值；另获不同副形态二档。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 集群体数 | `swarmBodies` | 3 | 见参数名称 |
| 副体生命继承 | `swarmHPFraction` | 0.5 | 倍率 |
| 副体攻击继承 | `swarmAttackFraction` | 0.5 | 倍率 |
| 甲壳护盾比例 | `shellShieldFraction` | 0.38 | 倍率 |
| 甲壳护甲加成 | `shellArmorAdd` | 3 | 见参数名称 |
| 甲壳护盾时长 | `shellShieldSeconds` | 6 | 模拟秒 |
| 爆发基础法伤 | `burstBaseArts` | 110 | 见参数名称 |
| 爆发半径 | `burstRadius` | 1.3 | 距离单位 |
| 爆发目标数 | `burstTargets` | 3 | 见参数名称 |
| 爆发延迟 | `burstDelaySeconds` | 0.2 | 模拟秒 |
| 副档位 | `secondaryTier` | 2 | 见参数名称 |

### 交互与边界

- 集群数量含主实体，不能描述成额外多出2/3体；副体支援不产点/再转化/穿门。
- 原生死亡爆炸总预算在集群各体间均分1/体数；羁绊爆裂只给主实体一次，与原生爆炸独立。
- 六档爆裂主+集群副时主实体伤害比例降低，但羁绊爆裂基值不额外按副体数翻倍。

- 保留三形态与6档二档副形态；不新增召唤专属库存或额外DP。

## 游击队（guerrilla）

成员：游击队猎犬（CARD_M03）、游击队战士（CARD_M15）、游击队狙击手（CARD_M16）、游击队传令兵（CARD_M17）、游击队盾卫（CARD_M18）、游击队萨卡兹战士（CARD_M19）、爱国者（CARD_M20）。

作用范围：军令获取与出手覆盖全队普通实体，不限游击成员；追击对全场每名存活守军分别结算。

星级缩放：读取角色已修饰UnitAttack一次；不加另一军令星级倍数。原生冻结增伤若已有则仍作用，须在角色预算中扣除。

### 2档

4点自动整令，最多6普通实体各贡献35%当前攻击。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 完整军令消耗 | `fullOrderCost` | 4 | 见参数名称 |
| 初始军令 | `initialOrders` | 0 | 见参数名称 |
| 参与上限 | `participantLimit` | 6 | 见参数名称 |
| 攻击倍率 | `attackMultiplier` | 0.35 | 倍率 |
| 允许半令 | `halfAllowed` | 否 | 见参数名称 |
| 近战护盾比例 | `meleeShieldFraction` | 0 | 倍率 |
| 护盾时长 | `shieldSeconds` | 0 | 模拟秒 |
| 扩展资格 | `expandedEligibility` | 否 | 见参数名称 |

### 4档

5点自动整令，最多8实体各50%攻击；出手机会中的近战各获8%MaxHP临时盾3秒。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 完整军令消耗 | `fullOrderCost` | 5 | 见参数名称 |
| 初始军令 | `initialOrders` | 0 | 见参数名称 |
| 参与上限 | `participantLimit` | 8 | 见参数名称 |
| 攻击倍率 | `attackMultiplier` | 0.5 | 倍率 |
| 允许半令 | `halfAllowed` | 否 | 见参数名称 |
| 近战护盾比例 | `meleeShieldFraction` | 0.08 | 倍率 |
| 护盾时长 | `shieldSeconds` | 3 | 模拟秒 |
| 扩展资格 | `expandedEligibility` | 否 | 见参数名称 |

### 6档

6点自动整令，最多12实体各65%；允许手动3点前半令+3点后半令，各最多6，后半排除前半实体；近战12%盾5秒。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 完整军令消耗 | `fullOrderCost` | 6 | 见参数名称 |
| 初始军令 | `initialOrders` | 0 | 见参数名称 |
| 参与上限 | `participantLimit` | 12 | 见参数名称 |
| 攻击倍率 | `attackMultiplier` | 0.65 | 倍率 |
| 允许半令 | `halfAllowed` | 是 | 见参数名称 |
| 半令消耗 | `halfOrderCost` | 3 | 见参数名称 |
| 半令参与上限 | `halfParticipantLimit` | 6 | 见参数名称 |
| 近战护盾比例 | `meleeShieldFraction` | 0.12 | 倍率 |
| 护盾时长 | `shieldSeconds` | 5 | 模拟秒 |
| 扩展资格 | `expandedEligibility` | 否 | 见参数名称 |

### 8档

保留高档奖励：开战2点，3点自动整令，全体合格普通实体180%；不受普通射程/普攻动作窗口限制、不重置普攻。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 完整军令消耗 | `fullOrderCost` | 3 | 见参数名称 |
| 初始军令 | `initialOrders` | 2 | 见参数名称 |
| 参与上限 | `participantLimit` | all_eligible_normal_entities | 见参数名称 |
| 攻击倍率 | `attackMultiplier` | 1.8 | 倍率 |
| 允许半令 | `halfAllowed` | 否 | 见参数名称 |
| 近战护盾比例 | `meleeShieldFraction` | 0.12 | 倍率 |
| 护盾时长 | `shieldSeconds` | 5 | 模拟秒 |
| 扩展资格 | `expandedEligibility` | 是 | 见参数名称 |

### 9档

开战2点，2点自动整令，全体合格普通实体300%。稀有终局，不作为普通13关通关要求。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 完整军令消耗 | `fullOrderCost` | 2 | 见参数名称 |
| 初始军令 | `initialOrders` | 2 | 见参数名称 |
| 参与上限 | `participantLimit` | all_eligible_normal_entities | 见参数名称 |
| 攻击倍率 | `attackMultiplier` | 3 | 倍率 |
| 允许半令 | `halfAllowed` | 否 | 见参数名称 |
| 近战护盾比例 | `meleeShieldFraction` | 0.12 | 倍率 |
| 护盾时长 | `shieldSeconds` | 5 | 模拟秒 |
| 扩展资格 | `expandedEligibility` | 是 | 见参数名称 |

### 资源规则

| 参数/规则 | 值 |
| --- | --- |
| contactPointsPerEntity | 1 |
| maxContactPointsPerKindPerBattle | 2 |
| pointsFromSupports | 0 |
| pointsFromDerived | 0 |
| maxFullOrderCasts | no_artificial_cap |
| presentationDurationSeconds | 3.55 |
| strikeAtSeconds[0] | 0.9 |
| strikeAtSeconds[1] | 1.2 |
| strikeAtSeconds[2] | 1.5 |
| strikeAtSeconds[3] | 1.8 |
| strikeAtSeconds[4] | 2.8 |
| strikeWeights[0] | 0.15 |
| strikeWeights[1] | 0.15 |
| strikeWeights[2] | 0.15 |
| strikeWeights[3] | 0.15 |
| strikeWeights[4] | 0.4 |
| maxTransfers | 2 |
| transferGoldCost | 0 |
| transferCostMeaning | 测试关免费指定；正式需要真实徽章，未引入新的金币购买渠道。 |

### 交互与边界

- 军令预算先按来源对每名守军过甲/抗，随后拆五段15/15/15/15/40%；不能五次扣甲。
- 自动整令只在有点、有合格参与者、活守军且演出闲置时提交；暂停/忙碌/待后半令不扣余额。
- 低档要求普通射程和动作门禁；所有档仍禁支援、复起、施法/特殊锁定、硬控、传送等现有不合格状态。
- 来源在后续段前死亡/无效便停止该来源剩余段；终局取消未结算段。
- 近战临时盾为同一个SynergyShield槽取高刷新，不逐次累加；不覆盖为另一套无限叠盾。

- 原2/4/6倍率0.60/0.80/0.90改为0.35/0.50/0.65；8/9已定1.8/3保留。
- 正常10人阵容最多20点接敌收入，高档再加2初始点；这是资源上界，不代表能用完，也不能按游击成员数代替全队种类数。
- 8/9必须7原生+1/2转职；正式徽章获取尚未实现，基线无额外赠送，因此普通自然13关无8/9通关依赖。

## 泥岩小队（mudrock）

成员：泥岩小队掷能者（CARD_M26）、泥岩小队践行者（CARD_M27）、泥岩小队掷能者组长（CARD_M42）、泥岩小队践行者组长（CARD_M43）、泥岩（CARD_M28）。

作用范围：巨像作为一场一次的付DP支援；历史培养跨关，开战人数/总星决定可用上限。

星级缩放：S为小队全部出战成员总星，转职同样计入；巨像阶段按固定表，不把巨像HP/攻击再乘队伍星级。

### 2档

开启巨像I→II培养；至少2种成员。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 阶段上限 | `maximumStage` | 2 | 见参数名称 |
| 所需总星数 | `requiredTotalStars` | 2 | 见参数名称 |
| 战斗经验公式 | `battleXPFormula` | tier+floor(S/2) | XP |
| 金币投入 | `goldInvestment` | 0 | 见参数名称 |

### 4档

至少4种且总星7，允许巨像III；低于7星仍受上限II。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 阶段上限 | `maximumStage` | 3 | 见参数名称 |
| 所需总星数 | `requiredTotalStars` | 7 | 见参数名称 |
| 战斗经验公式 | `battleXPFormula` | tier+floor(S/2) | XP |
| 金币投入 | `goldInvestment` | 0 | 见参数名称 |

### 5档

至少5种且总星10，允许IV；不要求每一名都二星，只检验星级总和。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 阶段上限 | `maximumStage` | 4 | 见参数名称 |
| 所需总星数 | `requiredTotalStars` | 10 | 见参数名称 |
| 战斗经验公式 | `battleXPFormula` | tier+floor(S/2) | XP |
| 金币投入 | `goldInvestment` | 0 | 见参数名称 |

### 6档

至少6种且总星12，允许V；现5名原生，需1泥岩徽章转职。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 阶段上限 | `maximumStage` | 5 | 见参数名称 |
| 所需总星数 | `requiredTotalStars` | 12 | 见参数名称 |
| 战斗经验公式 | `battleXPFormula` | tier+floor(S/2) | XP |
| 金币投入 | `goldInvestment` | 0 | 见参数名称 |
| 传递上限 | `maxTransfers` | 1 | 见参数名称 |

### 培养规则

| 参数/规则 | 值 |
| --- | --- |
| initialXP | 0 |
| stageThresholds[0] | 0 |
| stageThresholds[1] | 6 |
| stageThresholds[2] | 16 |
| stageThresholds[3] | 30 |
| stageThresholds[4] | 48 |
| battleXPFormula | highestTier+floor(totalStars/2) |
| victoryRequired | 否 |
| deployedOrHitRequired | 否 |
| goldInvestmentPerXP | 0 |
| goldInvestmentMeaning | 金币投资已退役；不提供金币→巨像XP转换。 |
| supplyXP | 0 |
| restartXP | 0 |
| exitXP | 0 |
| awardFormula | min(rawXP,max(0,threshold(currentCapacity)-historicalXP)) |
| overflowStored | 否 |
| historyDecreasesOnDowngrade | 否 |
| lowerFormationLimitsNextBattleStage | 是 |
| newChallengeResetsXP | 是 |
| formalTransferAvailability | 运行只有测试免费指定；正式真实徽章取得规则不在此数值重设计新增。 |

### 交互与边界

- 支援不占普通卡库存或编队位，但占当场DP及唯一巨像使用次数；不穿门、不接军令、不产军令、不能牧群转化。
- 既有全队指挥光环仍可影响巨像攻击；巨像第三击全场法伤不能反过来积军令/碎冰。
- IV/V回血每轮实际普攻命中最多一次，不按三目标多回3次。

- 保持最新2/4/5/6档与I–V，覆盖旧2/4/5+金币投资草案。
- 普通终局无IV/V必备要求；可用III或其他体系通关。

## 牧群（herd）

成员：宿主士兵（CARD_M21）、宿主拾荒者（CARD_M22）、宿主流浪者（CARD_M23）、萨卡兹宿主百夫长（CARD_M24）、梅菲斯特（CARD_M25）。

作用范围：全队非五费普通实体最终死亡转冰蓝灵体，并非仅牧群成员。

星级缩放：读取来源已升星MaxHP/UnitAttack各乘一次比例；不重新升星、不继承完整技能或被动效果；来源动作总时长限制灵体最小攻击间隔。

### 2档

最多3次，灵体30%原体MaxHP/攻击，2甲、近战0.75，起身后有效10秒。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 每场转化次数 | `conversionsPerBattle` | 3 | 见参数名称 |
| 灵体生命继承 | `spiritHPFraction` | 0.3 | 倍率 |
| 灵体攻击继承 | `spiritAttackFraction` | 0.3 | 倍率 |
| 灵体护甲 | `spiritArmor` | 2 | 见参数名称 |
| 有效寿命 | `effectiveLifetimeSeconds` | 10 | 模拟秒 |
| 坠落时长 | `fallSeconds` | 0.7 | 模拟秒 |
| 起身时长 | `riseSeconds` | 0.55 | 模拟秒 |
| 灵体范围 | `spiritRange` | 0.75 | 距离单位 |
| 灵体最小攻击间隔 | `spiritMinimumIntervalSeconds` | 2 | 模拟秒 |
| 移速 | `speed` | 0.65 | 见参数名称 |
| 远程继承 | `rangedInherited` | 否 | 见参数名称 |
| 远程攻击折损倍率 | `rangedAttackMultiplier` | 1 | 倍率 |
| 穿门伤害 | `gateDamage` | 0 | 见参数名称 |

### 4档

最多5次，灵体40%原体MaxHP/攻击，4甲；保留原远程者2.5射程并额外×0.85攻击，近战仍0.75；有效12秒。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 每场转化次数 | `conversionsPerBattle` | 5 | 见参数名称 |
| 灵体生命继承 | `spiritHPFraction` | 0.4 | 倍率 |
| 灵体攻击继承 | `spiritAttackFraction` | 0.4 | 倍率 |
| 灵体护甲 | `spiritArmor` | 4 | 见参数名称 |
| 有效寿命 | `effectiveLifetimeSeconds` | 12 | 模拟秒 |
| 坠落时长 | `fallSeconds` | 0.7 | 模拟秒 |
| 起身时长 | `riseSeconds` | 0.55 | 模拟秒 |
| 灵体范围 | `spiritRange` | 2.5 | 距离单位 |
| 灵体最小攻击间隔 | `spiritMinimumIntervalSeconds` | 2 | 模拟秒 |
| 移速 | `speed` | 0.65 | 见参数名称 |
| 远程继承 | `rangedInherited` | 是 | 见参数名称 |
| 远程攻击折损倍率 | `rangedAttackMultiplier` | 0.85 | 倍率 |
| 穿门伤害 | `gateDamage` | 0 | 见参数名称 |

### 交互与边界

- 灵体在原死亡路线进度起身；倒下0.7+起身0.55秒内不移动/攻击/阻挡/受伤/被选取，有效寿命从就绪后计。
- 灵体为支援，不继续转灵体、不生产军令、不参与萨卡兹/感染/深池/溟痕机制。
- 五费、巨像、援军、感染副体排除。

- 2/4档比例由35/45%降30/40%，生命与攻击同时生效，不能解释为30/40%完整原体战斗强度。

## 深池（deep-pool）

成员：深池方阵战士（CARD_M29）、深池逐火战士（CARD_M30）、深池焚毁者（CARD_M31）、蔓德拉（CARD_M32）。

作用范围：深池普通成员原生普攻/技能有效命中施加独立燎烧；火种传播与四档手动引爆。

星级缩放：燎烧和引爆仅×Q=(S/K)^(1/3)，不再吃来源攻击/技能星级第二倍率。

### 2档

燎烧12Q法伤/秒4秒；燃烧守军被正常伤害或深池燎烧消灭留火种，累计最多4；向1.8内最多2目标传播。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 每秒灼燃法伤 | `burnArtsPerSecond` | 12 | 见参数名称 |
| 灼燃持续 | `burnDurationSeconds` | 4 | 模拟秒 |
| 种子寿命 | `seedLifetime` | entire_battle | 见参数名称 |
| 种子累计上限 | `seedCumulativeLimit` | 4 | 见参数名称 |
| 传火半径 | `spreadRadius` | 1.8 | 距离单位 |
| 传火目标数 | `spreadTargets` | 2 | 见参数名称 |
| 引爆次数 | `detonationCasts` | 0 | 见参数名称 |
| 引爆种子消耗 | `detonationSeedCost` | 0 | 见参数名称 |
| 引爆法伤 | `detonationArts` | 0 | 见参数名称 |
| 引爆半径 | `detonationRadius` | 0 | 距离单位 |
| 引爆目标数 | `detonationTargets` | 0 | 见参数名称 |

### 4档

燎烧22Q/秒4秒；最多6枚累计火种，传播2.2内3目标。手动引爆自动取有活目标的未耗火种，耗1枚、160Q法伤、1.8内最多4目标、全场2次。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 每秒灼燃法伤 | `burnArtsPerSecond` | 22 | 见参数名称 |
| 灼燃持续 | `burnDurationSeconds` | 4 | 模拟秒 |
| 种子寿命 | `seedLifetime` | entire_battle | 见参数名称 |
| 种子累计上限 | `seedCumulativeLimit` | 6 | 见参数名称 |
| 传火半径 | `spreadRadius` | 2.2 | 距离单位 |
| 传火目标数 | `spreadTargets` | 3 | 见参数名称 |
| 引爆次数 | `detonationCasts` | 2 | 见参数名称 |
| 引爆种子消耗 | `detonationSeedCost` | 1 | 见参数名称 |
| 引爆法伤 | `detonationArts` | 160 | 见参数名称 |
| 引爆半径 | `detonationRadius` | 1.8 | 距离单位 |
| 引爆目标数 | `detonationTargets` | 4 | 见参数名称 |
| 引爆冷却 | `detonationCooldownSeconds` | 0 | 模拟秒 |

### 交互与边界

- 同名燎烧取高刷新、不按来源数叠加；与角色原生灼燃独立。
- 燎烧本身不附赠原生减法抗；目标若已有原生减抗则按既有抗性管道结算。
- 首击击杀也可留种；累计上限含已消耗种；引爆伤害不留种/返种、不无限自繁殖。
- 无合法目标/暂停/失败请求不扣种；保持幂等。

- 不接入火种充当逐火环境火源，仍为未实现边界。

## 萨卡兹（sarkaz）

成员：游击队萨卡兹战士（CARD_M19）、爱国者（CARD_M20）、萨卡兹宿主百夫长（CARD_M24）、泥岩小队掷能者（CARD_M26）、泥岩小队践行者（CARD_M27）、泥岩（CARD_M28）、泥岩小队掷能者组长（CARD_M42）、泥岩小队践行者组长（CARD_M43）、萨卡兹大剑手（CARD_M37）、萨卡兹术师（CARD_M38）、维维美（CARD_WISADEL_APRIL）。

作用范围：全萨卡兹生命加成；整备指定一名当前萨卡兹承契者，普通部署时自动入场盾+全场攻击加成。

星级缩放：生命与攻击只通过已升星角色面板成长；盾=加成后的实体MaxHP×档位盾比例×分享比例；不再套技能成长。

### 2档

全萨卡兹HP+8%；承契者攻击+25%、一次15%MaxHP盾。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 全部萨卡兹生命加成 | `allSarkazHPAdd` | 0.08 | 见参数名称 |
| 承契者攻击加成 | `holderAttackAdd` | 0.25 | 见参数名称 |
| 承契者护盾生命比例 | `holderShieldMaxHPFraction` | 0.15 | 倍率 |
| 其他友方分担比例 | `otherShareFraction` | 0 | 倍率 |
| 护盾持续 | `shieldDurationSeconds` | until_consumed_or_final_removal | 模拟秒 |
| 每实体护盾重施次数 | `shieldReapplicationsPerEntity` | 0 | 见参数名称 |

### 4档

全体HP+15%；承契者攻击+45%、一次25%盾。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 全部萨卡兹生命加成 | `allSarkazHPAdd` | 0.15 | 见参数名称 |
| 承契者攻击加成 | `holderAttackAdd` | 0.45 | 见参数名称 |
| 承契者护盾生命比例 | `holderShieldMaxHPFraction` | 0.25 | 倍率 |
| 其他友方分担比例 | `otherShareFraction` | 0 | 倍率 |
| 护盾持续 | `shieldDurationSeconds` | until_consumed_or_final_removal | 模拟秒 |
| 每实体护盾重施次数 | `shieldReapplicationsPerEntity` | 0 | 见参数名称 |

### 6档

全体HP+22%；承契者攻击+65%、35%盾，其他萨卡兹半额分享攻击与盾。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 全部萨卡兹生命加成 | `allSarkazHPAdd` | 0.22 | 见参数名称 |
| 承契者攻击加成 | `holderAttackAdd` | 0.65 | 见参数名称 |
| 承契者护盾生命比例 | `holderShieldMaxHPFraction` | 0.35 | 倍率 |
| 其他友方分担比例 | `otherShareFraction` | 0.5 | 倍率 |
| 护盾持续 | `shieldDurationSeconds` | until_consumed_or_final_removal | 模拟秒 |
| 每实体护盾重施次数 | `shieldReapplicationsPerEntity` | 0 | 见参数名称 |

### 8档

全体HP+30%；全萨卡兹完整分享攻击+85%、一次45%盾。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 全部萨卡兹生命加成 | `allSarkazHPAdd` | 0.3 | 见参数名称 |
| 承契者攻击加成 | `holderAttackAdd` | 0.85 | 见参数名称 |
| 承契者护盾生命比例 | `holderShieldMaxHPFraction` | 0.45 | 倍率 |
| 其他友方分担比例 | `otherShareFraction` | 1 | 倍率 |
| 护盾持续 | `shieldDurationSeconds` | until_consumed_or_final_removal | 模拟秒 |
| 每实体护盾重施次数 | `shieldReapplicationsPerEntity` | 0 | 见参数名称 |

### 9档

全体HP+40%；全萨卡兹攻击+110%、一次55%盾；稀有9人成型奖励。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 全部萨卡兹生命加成 | `allSarkazHPAdd` | 0.4 | 见参数名称 |
| 承契者攻击加成 | `holderAttackAdd` | 1.1 | 见参数名称 |
| 承契者护盾生命比例 | `holderShieldMaxHPFraction` | 0.55 | 倍率 |
| 其他友方分担比例 | `otherShareFraction` | 1 | 倍率 |
| 护盾持续 | `shieldDurationSeconds` | until_consumed_or_final_removal | 模拟秒 |
| 每实体护盾重施次数 | `shieldReapplicationsPerEntity` | 0 | 见参数名称 |

### 交互与边界

- 同实体只在合法部署一次发盾，原生复起/阶段变化不重发；不是周期补盾或手动技。
- 承契者必须为已编入萨卡兹，不能把无此标签者变为承契者；开战锁定，其他萨卡兹分享门槛保持6/8。
- 攻击与TacticalAttackMultiplier内全队光环按加法合并，例如1名10%指挥+四档承契=1+0.10+0.45=1.55，而不是1.10×1.45。
- 支援体排除；盾保留原伤害类型吸收与真实伤害绕盾规则，不新增真伤吸收。

- 当前存在11名原生萨卡兹，9档无需虚构转职来源，但需要9编队位和高费成员收集。

## 海嗣（marine）

成员：囊海爬行者（CARD_M33）、钵海收割者（CARD_M34）、壳海狂奔者（CARD_M35）、首言者（CARD_M36）。

作用范围：普通海嗣真实接敌落种，四邻地面扩散；海嗣站在溟痕上时原生神经积累强化；地面守军受地面侵蚀。

星级缩放：不另加平均/总星倍率；原生神经若取攻击基值已随角色星级成长，地面3点固定。

### 2档

扩散6秒、最多8格；驻留溟痕海嗣原生神经×1.35；地面守军每秒+3神经积累。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 传火间隔 | `spreadIntervalSeconds` | 6 | 模拟秒 |
| 地块上限 | `tileLimit` | 8 | 见参数名称 |
| 原生神经积累倍率 | `nativeNeuralMultiplier` | 1.35 | 倍率 |
| 每秒地面神经积累 | `groundNeuralPerSecond` | 3 | 见参数名称 |
| 神经阈值 | `neuralThreshold` | 100 | 见参数名称 |
| 神经爆发真伤 | `neuralBurstTrueDamage` | 100 | 见参数名称 |
| 神经爆发持续 | `neuralBurstDurationSeconds` | 10 | 模拟秒 |

### 4档

扩散4秒、最多16格；原生神经×1.75；地面侵蚀仍每秒3，不随倍率再乘。

| 参数 | 稳定key | 目标值 | 单位 |
| --- | --- | --- | --- |
| 传火间隔 | `spreadIntervalSeconds` | 4 | 模拟秒 |
| 地块上限 | `tileLimit` | 16 | 见参数名称 |
| 原生神经积累倍率 | `nativeNeuralMultiplier` | 1.75 | 倍率 |
| 每秒地面神经积累 | `groundNeuralPerSecond` | 3 | 见参数名称 |
| 神经阈值 | `neuralThreshold` | 100 | 见参数名称 |
| 神经爆发真伤 | `neuralBurstTrueDamage` | 100 | 见参数名称 |
| 神经爆发持续 | `neuralBurstDurationSeconds` | 10 | 模拟秒 |

### 交互与边界

- 每种海嗣本场只落一个接敌根；扩散预留尚未接敌成员落种格；每次只长一圈四邻，不递归铺全图。
- 只铺保存的地面格，高台不铺，同格不叠；无存活普通海嗣时休眠，重新有单位后续进度，跨波保留、跨关清空。
- 100积累触发100真伤和10秒爆发，期间不再积累且不能行动/阻挡；保留原机制，不能再加同类控制延长。
- 不恢复旧跨关海嗣经验/适应、金币投资或转职。

- 海嗣强项是路径控制与占地，不拿100神经积累当作100直接DPS。
- 多源原生神经可接近长时间控制，已有10秒爆发期间不积累构成间隔；须用实际守军保护与技能复验。

## 支援体与巨像I–V

| 参数/规则 | 值 |
| --- | --- |
| [0].id | mudrock-golem |
| [0].name | 泥岩巨像 |
| [0].source[0] | E:/珍贵资料/昨日圆车/Assets/Scripts/Runtime/GolemGrowth.cs |
| [0].source[1] | E:/珍贵资料/昨日圆车/Assets/Scripts/Runtime/AdvancedSynergies.cs |
| [0].scope | 已实现I–V；下列为项目外重设值，保留原动作、5.2秒间隔、唯一部署与高档机制。 |
| [0].common.usesPerBattle | 1 |
| [0].common.speed | 0.45 |
| [0].common.intervalSeconds | 5.2 |
| [0].common.attackDurationSeconds | 5.166667 |
| [0].common.windupSeconds | 2.746667 |
| [0].common.projectileSeconds | 0.02 |
| [0].common.gateDamage | 0 |
| [0].common.SP | 0 |
| [0].common.skill | 0 |
| [0].common.synergySupport | 是 |
| [0].stages[0].stage | 1 |
| [0].stages[0].requiredXP | 0 |
| [0].stages[0].requiredKinds | 2 |
| [0].stages[0].requiredTotalStars | 2 |
| [0].stages[0].hp | 500 |
| [0].stages[0].attack | 100 |
| [0].stages[0].armor | 8 |
| [0].stages[0].resistance | 0 |
| [0].stages[0].dp | 4 |
| [0].stages[0].range | 1.1 |
| [0].stages[0].targets | 1 |
| [0].stages[0].thirdHitPrimaryPhysical | 0 |
| [0].stages[0].thirdHitSidePhysical | 0 |
| [0].stages[0].thirdHitSideTargets | 0 |
| [0].stages[0].globalThirdHitArtsFraction | 0 |
| [0].stages[0].healPerSuccessfulAttackFraction | 0 |
| [0].stages[0].controlImmune | 否 |
| [0].stages[0].canHitHighGround | 否 |
| [0].stages[1].stage | 2 |
| [0].stages[1].requiredXP | 6 |
| [0].stages[1].requiredKinds | 2 |
| [0].stages[1].requiredTotalStars | 2 |
| [0].stages[1].hp | 900 |
| [0].stages[1].attack | 170 |
| [0].stages[1].armor | 10 |
| [0].stages[1].resistance | 0 |
| [0].stages[1].dp | 5 |
| [0].stages[1].range | 1.1 |
| [0].stages[1].targets | 1 |
| [0].stages[1].thirdHitPrimaryPhysical | 80 |
| [0].stages[1].thirdHitSidePhysical | 0 |
| [0].stages[1].thirdHitSideTargets | 0 |
| [0].stages[1].globalThirdHitArtsFraction | 0 |
| [0].stages[1].healPerSuccessfulAttackFraction | 0 |
| [0].stages[1].controlImmune | 否 |
| [0].stages[1].canHitHighGround | 否 |
| [0].stages[2].stage | 3 |
| [0].stages[2].requiredXP | 16 |
| [0].stages[2].requiredKinds | 4 |
| [0].stages[2].requiredTotalStars | 7 |
| [0].stages[2].hp | 2200 |
| [0].stages[2].attack | 380 |
| [0].stages[2].armor | 18 |
| [0].stages[2].resistance | 20 |
| [0].stages[2].dp | 8 |
| [0].stages[2].range | 1.1 |
| [0].stages[2].targets | 1 |
| [0].stages[2].thirdHitPrimaryPhysical | 120 |
| [0].stages[2].thirdHitSidePhysical | 100 |
| [0].stages[2].thirdHitSideTargets | 2 |
| [0].stages[2].thirdHitSideRadius | 1 |
| [0].stages[2].globalThirdHitArtsFraction | 0 |
| [0].stages[2].healPerSuccessfulAttackFraction | 0 |
| [0].stages[2].controlImmune | 否 |
| [0].stages[2].canHitHighGround | 否 |
| [0].stages[3].stage | 4 |
| [0].stages[3].requiredXP | 30 |
| [0].stages[3].requiredKinds | 5 |
| [0].stages[3].requiredTotalStars | 10 |
| [0].stages[3].hp | 4000 |
| [0].stages[3].attack | 520 |
| [0].stages[3].armor | 22 |
| [0].stages[3].resistance | 30 |
| [0].stages[3].dp | 14 |
| [0].stages[3].range | 2.2 |
| [0].stages[3].targets | 3 |
| [0].stages[3].thirdHitPrimaryPhysical | 0 |
| [0].stages[3].thirdHitSidePhysical | 0 |
| [0].stages[3].thirdHitSideTargets | 0 |
| [0].stages[3].globalThirdHitArtsFraction | 0.8 |
| [0].stages[3].healPerSuccessfulAttackFraction | 0.08 |
| [0].stages[3].controlImmune | 是 |
| [0].stages[3].canHitHighGround | 是 |
| [0].stages[4].stage | 5 |
| [0].stages[4].requiredXP | 48 |
| [0].stages[4].requiredKinds | 6 |
| [0].stages[4].requiredTotalStars | 12 |
| [0].stages[4].hp | 5200 |
| [0].stages[4].attack | 700 |
| [0].stages[4].armor | 26 |
| [0].stages[4].resistance | 40 |
| [0].stages[4].dp | 16 |
| [0].stages[4].range | 2.2 |
| [0].stages[4].targets | 3 |
| [0].stages[4].thirdHitPrimaryPhysical | 0 |
| [0].stages[4].thirdHitSidePhysical | 0 |
| [0].stages[4].thirdHitSideTargets | 0 |
| [0].stages[4].globalThirdHitArtsFraction | 0.8 |
| [0].stages[4].healPerSuccessfulAttackFraction | 0.08 |
| [0].stages[4].controlImmune | 是 |
| [0].stages[4].canHitHighGround | 是 |
| [0].notes[0] | 实际第三次普攻命中才触发；无命中不触发、无目标不空放。IV/V每轮多目标只回8%一次。 |
| [0].notes[1] | 巨像不承担普通终战必须达到的伤害阈值；III也可用于常规，IV/V是高投入培养路径。 |
| [0].notes[2] | 本表删除旧60甲/60抗对低攻击守军近免伤，并把IV/V攻击1600/2000改为520/700。 |

## 银狼与暂缓专属

银狼的已接入招募/战斗保留，后续制作及数值接入仍暂缓；本附录列值不解冻该安排，也不承诺实现准备阶段接入、补丁或三星管理员。

| 参数/规则 | 值 |
| --- | --- |
| [0].id | silver-wolf-lv999 |
| [0].name | 银狼LV.999：已接入战斗 |
| [0].cardId | CARD_M41 |
| [0].source[0] | E:/珍贵资料/昨日圆车/Assets/Scripts/Runtime/SilverWolfCombat.cs |
| [0].source[1] | E:/珍贵资料/昨日圆车/Assets/Scripts/Runtime/EngagementCombat.cs |
| [0].source[2] | E:/珍贵资料/昨日圆车/Assets/Data/Battles/M41.asset |
| [0].status | 战斗已实现，以下为项目外新数值；准备阶段接入/补丁/管理员接管仍暂缓。 |
| [0].scope | 常态面板由怪物总表定义，不重复覆盖；技能仅覆盖已实现变身三盒。 |
| [0].parameters[0].key | initialSP |
| [0].parameters[0].value | 24 |
| [0].parameters[0].unit | specified_in_effect |
| [0].parameters[1].key | requiredSP |
| [0].parameters[1].value | 28 |
| [0].parameters[1].unit | specified_in_effect |
| [0].parameters[2].key | spPerSecond |
| [0].parameters[2].value | 1 |
| [0].parameters[2].unit | specified_in_effect |
| [0].parameters[3].key | skillAmmo |
| [0].parameters[3].value | 3 |
| [0].parameters[3].unit | specified_in_effect |
| [0].parameters[4].key | transformSeconds |
| [0].parameters[4].value | 1.43 |
| [0].parameters[4].unit | simulation_seconds |
| [0].parameters[5].key | boxActionSeconds |
| [0].parameters[5].value | 4.6 |
| [0].parameters[5].unit | simulation_seconds |
| [0].parameters[6].key | skillRange |
| [0].parameters[6].value | 3.8 |
| [0].parameters[6].unit | world_units |
| [0].parameters[7].key | boxArtsBudgetByStar |
| [0].parameters[7].value[0] | 225 |
| [0].parameters[7].value[1] | 450 |
| [0].parameters[7].value[2] | 1125 |
| [0].parameters[7].unit | specified_in_effect |
| [0].parameters[8].key | swordProbability |
| [0].parameters[8].value | 0.333333 |
| [0].parameters[8].unit | specified_in_effect |
| [0].parameters[9].key | bombProbability |
| [0].parameters[9].value | 0.333333 |
| [0].parameters[9].unit | specified_in_effect |
| [0].parameters[10].key | beanProbability |
| [0].parameters[10].value | 0.333333 |
| [0].parameters[10].unit | specified_in_effect |
| [0].parameters[11].key | swordHitSeconds |
| [0].parameters[11].value | 1.76 |
| [0].parameters[11].unit | simulation_seconds |
| [0].parameters[12].key | swordFinishSeconds |
| [0].parameters[12].value | 2.96 |
| [0].parameters[12].unit | simulation_seconds |
| [0].parameters[13].key | bombHitSeconds |
| [0].parameters[13].value | 2.54 |
| [0].parameters[13].unit | simulation_seconds |
| [0].parameters[14].key | beanHitSeconds |
| [0].parameters[14].value | 2.29 |
| [0].parameters[14].unit | simulation_seconds |
| [0].parameters[15].key | swordBonusActualDamageFraction |
| [0].parameters[15].value | 0.2 |
| [0].parameters[15].unit | ratio |
| [0].parameters[16].key | bombDP |
| [0].parameters[16].value | 2 |
| [0].parameters[16].unit | DP |
| [0].parameters[17].key | beanSelfSP |
| [0].parameters[17].value | 3 |
| [0].parameters[17].unit | specified_in_effect |
| [0].parameters[18].key | resourceDisplayCost |
| [0].parameters[18].value | 0 |
| [0].parameters[18].unit | specified_in_effect |
| [0].starScaling | 技能表225/450/1125已经是最终星级独立预算，与player-roster.json一致；只应用当前战斗攻击修饰一次，不再乘技能星级成长。 |
| [0].interactions[0] | 每盒独立等概率，可重复；每盒法伤总预算在当时存活守军之间均分，不按人数复制预算。 |
| [0].interactions[1] | 大剑追加该盒实际总伤20%真伤给追击时HP最高存活守军；炸弹+2全队DP，豆+3自身SP。 |
| [0].interactions[2] | 三盒完且完整动作结束回常态；技能期间停自然回SP，豆回SP可留下一轮；暂停冻结，无额外回费上限。 |
| [0].interactions[3] | 准备阶段头号玩家羁绊接入、补丁及三星管理员接管不设计为已实现能力。 |
| [0].notes[0] | 与player-roster.json共用角色技能数值；该表是候选参数，银狼制作仍暂停，不恢复准备阶段接入/补丁/接管。 |
| [1].id | not-implemented-five-cost-construction |
| [1].name | 五费准备阶段专属边界 |
| [1].status | 不作为推荐基线新增机制 |
| [1].parameters[0].key | newRecruitmentPrivileges |
| [1].parameters[0].value | 0 |
| [1].parameters[0].unit | specified_in_effect |
| [1].parameters[1].key | newGoldSources |
| [1].parameters[1].value | 0 |
| [1].parameters[1].unit | specified_in_effect |
| [1].parameters[2].key | newFreeRerolls |
| [1].parameters[2].value | 0 |
| [1].parameters[2].unit | specified_in_effect |
| [1].parameters[3].key | newTransferSources |
| [1].parameters[3].value | 0 |
| [1].parameters[3].unit | specified_in_effect |
| [1].parameters[4].key | newAdministratorXP |
| [1].parameters[4].value | 0 |
| [1].parameters[4].unit | XP |
| [1].parameters[5].key | newAdministratorLevels |
| [1].parameters[5].value | 0 |
| [1].parameters[5].unit | specified_in_effect |
| [1].notes[0] | 塔露拉领袖/爱国者搭档/霜星备战协议等旧构筑专属未实施的部分不因本轮重设数值伪装完成。 |
| [1].notes[1] | 已接入的角色普攻、复起、原生技能由怪物总表单列；本文件只给羁绊相互作用。 |

## 验证范围与未测项

- 本文件是数值建议和公式核算，不是Unity实现、实际战斗模拟或真实玩家胜率。
- 完整战斗需父任务集成后验证路线、阻挡、射程、技能前摇/复起/控制、治疗、护甲/法抗、部署时序、成员收集和星级分布。
- 高档转职获取尚未正式接入；不以测试关免费指定推导正式成型率。
