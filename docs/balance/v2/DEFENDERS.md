# 数值v2：守方干员、技能与树海独立配置

2026-11-08。**人类已同意登记的数值v2试验基线；尚未接入Unity，不是最终平衡或实机验证结论。** 数值归属与适用边界见[总则](README.md)。

覆盖22名已接入守军，每名T1/T2/T3共66份面板。T档是本作遭遇强度，**不是守军合成星级**；原作稀有度单列且未核实时保留未核实。D13–D20未接入，不能按未来清单编造现有技能。

- T1/T2/T3为本作关卡强度档，不是合成星级；原作稀有度与玩家怪物合成星级分开。
- 以13关完整挑战为主，1/2/5/9/12为保送补给关，无守军且DPS通过阈值0；3/4用T1，6用T2低压精英，7/8用T2，10用T2精英组合，11用T3普通，13用T3有限精英组合。
- 原作稀有度只按本地明确文字核实：D07/D09/D10三星，D21~D30六星；D01~D06/D08/D11/D12未找到明确字段，列未核实，不据记忆猜。
- 玩家一星标准兵费用1~5的HP180/300/480/800/1250、DPS15/24/39/62.5/95；后期4费二星1440HP/93.75DPS、5费二星2500HP/171DPS、3费三星1555HP/87.75DPS。
- 玩家护甲按现有基值0~24，每星+2；常规核算样本0/12/30。法术以0/20/40法抗样本复核，精英特殊护盾/复起另算。
- 现有防守方没有独立逐干员基础法抗字段，基础法抗来自当前对局CombatSettings.Effects.Resistance。本建议全普通守军基础法抗0，圣聆初雪常态保留额外20。若未来逐单位配置法抗，需要工程接口变更；此稿未伪称已实现。
- 保留普通攻击前摇/动作时长/间隔、站位身份、半径范围、阻挡；所有技能时钟与暂停沿用现有规则。
- 技能参数里部分在现有代码硬编码；这里是重新设计的目标值，不能仅改资产就宣称已生效。
- 无战斗倒计时；下方公式窗口只用于测量火力和治疗压力，不引入超时失败。

## D01 · 米格鲁

职责：地面重装/物理阻挡。原作稀有度：未核实。

- 通过较高护甲和挡3制造法术/破甲需求，伤害不是本职业主卖点。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 260 | 16 | 8 | 0 | 1.6 | 0.85 | 3 | Physical |
| 2 | 520 | 32 | 14 | 0 | 1.6 | 0.85 | 3 | Physical |
| 3 | 850 | 50 | 20 | 0 | 1.6 | 0.85 | 3 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.5 |
| 1 | actionDuration | 1 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 10 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 2.5 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0.625 |
| 2 | windup | 0.5 |
| 2 | actionDuration | 1 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 20 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 12.5 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 1.25 |
| 3 | windup | 0.5 |
| 3 | actionDuration | 1 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 31.25 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 23.75 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 12.5 |

### 防御力强化·α型（BeagleGuard）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 0 | 4 | 8 |
| 所需技力 | `requiredSP` | SP | 32 | 34 | 36 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 16 | 20 | 24 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1.4 | 1.45 | 1.5 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1 | 1 | 1 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 通过较高护甲和挡3制造法术/破甲需求，伤害不是本职业主卖点。

## D02 · 卡缇

职责：地面自疗重装。原作稀有度：未核实。

- 自疗只恢复实际缺失生命，满SP与生命不高于80%才使用；治疗不复活。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 300 | 16 | 4 | 0 | 1.2 | 0.9 | 3 | Physical |
| 2 | 580 | 30 | 9 | 0 | 1.2 | 0.9 | 3 | Physical |
| 3 | 900 | 48 | 14 | 0 | 1.2 | 0.9 | 3 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.4 |
| 1 | actionDuration | 1 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 13.333 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 3.333 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0.833 |
| 2 | windup | 0.4 |
| 2 | actionDuration | 1 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 25 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 15 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 0.833 |
| 3 | windup | 0.4 |
| 3 | actionDuration | 1 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 40 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 30 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 15 |

### 生命回复·α型（CardiganRecovery）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 4 | 6 | 8 |
| 所需技力 | `requiredSP` | SP | 24 | 24 | 24 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 0 | 0 | 0 |
| 治疗比例 | `healFraction` | 倍率 | 0.25 | 0.3 | 0.35 |
| 自动治疗生命阈值 | `autoUseBelowHealthFraction` | 倍率 | 0.8 | 0.8 | 0.8 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1 | 1 | 1 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 自疗只恢复实际缺失生命，满SP与生命不高于80%才使用；治疗不复活。

## D03 · 斑点

职责：地面治疗重装。原作稀有度：未核实。

- 技能态停止普攻，治疗附近最低血量比例友军；被沉默/控制时仍遵守现有结算。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 240 | 14 | 6 | 0 | 2.2 | 0.85 | 3 | Physical |
| 2 | 480 | 30 | 12 | 0 | 2.2 | 0.85 | 3 | Physical |
| 3 | 780 | 48 | 18 | 0 | 2.2 | 0.85 | 3 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.5 |
| 1 | actionDuration | 1 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 2.5 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 6.364 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 0.909 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0.455 |
| 1 | skillSingleTargetHps | 13.462 |
| 2 | windup | 0.5 |
| 2 | actionDuration | 1 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 2.5 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 13.636 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 8.182 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 0.455 |
| 2 | skillSingleTargetHps | 31.154 |
| 3 | windup | 0.5 |
| 3 | actionDuration | 1 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 2.5 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 21.818 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 16.364 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 8.182 |
| 3 | skillSingleTargetHps | 53.538 |

### 次级治疗模式（SpotHealingMode）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 6 | 10 | 14 |
| 所需技力 | `requiredSP` | SP | 32 | 34 | 36 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 12 | 16 | 20 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.25 | 1.35 | 1.45 |
| 治疗间隔 | `healingInterval` | 秒 | 1.3 | 1.3 | 1.3 |
| 技能停止普通攻击 | `stopsOrdinaryAttack` | 个/次或开关 | 1 | 1 | 1 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 技能态停止普攻，治疗附近最低血量比例友军；被沉默/控制时仍遵守现有结算。

## D04 · 玫兰莎

职责：地面单挡高单击近卫。原作稀有度：未核实。

- 保留2秒完整动作和2.5秒间隔，靠高单击穿中甲；不为提高DPS缩短动作。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 200 | 30 | 2 | 0 | 2.5 | 0.9 | 1 | Physical |
| 2 | 420 | 62 | 4 | 0 | 2.5 | 0.9 | 1 | Physical |
| 3 | 700 | 100 | 6 | 0 | 2.5 | 0.9 | 1 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.966667 |
| 1 | actionDuration | 2 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 12 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 7.2 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0.4 |
| 2 | windup | 0.966667 |
| 2 | actionDuration | 2 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 24.8 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 20 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 12.8 |
| 3 | windup | 0.966667 |
| 3 | actionDuration | 2 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 40 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 35.2 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 28 |

### 攻击力强化·α型（MelanthaStrength）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 0 | 5 | 10 |
| 所需技力 | `requiredSP` | SP | 30 | 32 | 34 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 12 | 16 | 20 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.3 | 1.4 | 1.5 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 保留2秒完整动作和2.5秒间隔，靠高单击穿中甲；不为提高DPS缩短动作。

## D05 · 月见夜

职责：地面远程近卫/附魔法术。原作稀有度：未核实。

- 远程攻击仍乘0.8；20%暴击×1.5，技能只将伤害转为法术，不取消射程折损。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 190 | 20 | 3 | 0 | 1.3 | 3 | 2 | Physical |
| 2 | 400 | 42 | 6 | 0 | 1.3 | 3 | 2 | Physical |
| 3 | 650 | 66 | 9 | 0 | 1.3 | 3 | 2 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.4 |
| 1 | actionDuration | 1.2 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 0.8 |
| 1 | criticalChance | 0.2 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 16.923 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 7.692 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0.769 |
| 2 | windup | 0.4 |
| 2 | actionDuration | 1.2 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 0.8 |
| 2 | criticalChance | 0.2 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 35.538 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 26.308 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 12.462 |
| 3 | windup | 0.4 |
| 3 | actionDuration | 1.2 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 0.8 |
| 3 | criticalChance | 0.2 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 55.846 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 46.615 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 32.769 |

### 武器附魔·α型（MidnightEnchant）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 10 | 16 | 22 |
| 所需技力 | `requiredSP` | SP | 45 | 48 | 52 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 18 | 22 | 26 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.2 | 1.3 | 1.35 |
| 远程攻击折损倍率 | `rangedAttackMultiplier` | 倍率 | 0.8 | 0.8 | 0.8 |
| 暴击概率 | `criticalChance` | 倍率 | 0.2 | 0.2 | 0.2 |
| 暴击倍率 | `criticalMultiplier` | 倍率 | 1.5 | 1.5 | 1.5 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 远程攻击仍乘0.8；20%暴击×1.5，技能只将伤害转为法术，不取消射程折损。

## D06 · 翎羽

职责：地面单挡快攻。原作稀有度：未核实。

- 没有额外回费/击杀金币；高速单挡身份保留，高甲可以克制。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 180 | 14 | 3 | 0 | 1 | 0.9 | 1 | Physical |
| 2 | 380 | 30 | 6 | 0 | 1 | 0.9 | 1 | Physical |
| 3 | 620 | 46 | 9 | 0 | 1 | 0.9 | 1 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.3 |
| 1 | actionDuration | 0.9 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 14 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 2 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 1 |
| 2 | windup | 0.3 |
| 2 | actionDuration | 0.9 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 30 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 18 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 1 |
| 3 | windup | 0.3 |
| 3 | actionDuration | 0.9 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 46 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 34 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 16 |

### 迅捷打击·α型（PlumeSwiftStrike）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 0 | 4 | 8 |
| 所需技力 | `requiredSP` | SP | 32 | 34 | 36 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 12 | 16 | 20 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.15 | 1.2 | 1.25 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1.15 | 1.2 | 1.25 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 没有额外回费/击杀金币；高速单挡身份保留，高甲可以克制。

## D07 · 克洛丝

职责：高台速射狙击。原作稀有度：3星（本作资料记录）。

- 4次普通攻击回满SP后第5次消耗二连；两个箭头分别扣甲并独立暴击，不按合计伤害只扣一次甲。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 140 | 16 | 2 | 0 | 1.5 | 3.5 | 0 | Physical |
| 2 | 280 | 32 | 4 | 0 | 1.5 | 3.5 | 0 | Physical |
| 3 | 440 | 50 | 6 | 0 | 1.5 | 3.5 | 0 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.433333 |
| 1 | actionDuration | 0.933333 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 3.5 |
| 1 | highGround | 是 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0.2 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 15.957 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 6.357 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0.939 |
| 2 | windup | 0.433333 |
| 2 | actionDuration | 0.933333 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 3.5 |
| 2 | highGround | 是 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0.2 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 31.915 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 22.315 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 7.915 |
| 3 | windup | 0.433333 |
| 3 | actionDuration | 0.933333 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 3.5 |
| 3 | highGround | 是 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0.2 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 49.867 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 40.267 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 25.867 |

### 二连射·自动（KroosDoubleShot）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 0 | 0 | 0 |
| 所需技力 | `requiredSP` | SP | 4 | 4 | 4 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 0 | 0 | 0 |
| 持续时间 | `duration` | 秒 | 0 | 0 | 0 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.4 | 1.4 | 1.4 |
| 每行动命中次数 | `hits` | 个/次或开关 | 2 | 2 | 2 |
| 逐段命中间隔 | `hitSpacing` | 秒 | 0.1 | 0.1 | 0.1 |
| 暴击概率 | `criticalChance` | 倍率 | 0.2 | 0.2 | 0.2 |
| 暴击倍率 | `criticalMultiplier` | 倍率 | 1.5 | 1.5 | 1.5 |
| 每普通攻击回技力 | `spPerOrdinaryAttack` | SP | 1 | 1 | 1 |
| 强化攻击回技力 | `spDuringEmpoweredAttack` | SP | 0 | 0 | 0 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 4次普通攻击回满SP后第5次消耗二连；两个箭头分别扣甲并独立暴击，不按合计伤害只扣一次甲。

## D08 · 史都华德

职责：高台高甲优先术师。原作稀有度：未核实。

- 4次普通攻击后第5次190%法术；现有高甲优先索敌保留。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 140 | 24 | 2 | 0 | 1.8 | 3.5 | 0 | Arts |
| 2 | 280 | 44 | 4 | 0 | 1.8 | 3.5 | 0 | Arts |
| 3 | 440 | 68 | 6 | 0 | 1.8 | 3.5 | 0 | Arts |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.5 |
| 1 | actionDuration | 0.866667 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 3.5 |
| 1 | highGround | 是 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 15.733 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 15.733 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 15.733 |
| 2 | windup | 0.5 |
| 2 | actionDuration | 0.866667 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 3.5 |
| 2 | highGround | 是 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 28.844 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 28.844 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 28.844 |
| 3 | windup | 0.5 |
| 3 | actionDuration | 0.866667 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 3.5 |
| 3 | highGround | 是 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 44.578 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 44.578 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 44.578 |

### 强力击·α型（StewardStrike）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 0 | 0 | 0 |
| 所需技力 | `requiredSP` | SP | 4 | 4 | 4 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 0 | 0 | 0 |
| 持续时间 | `duration` | 秒 | 0 | 0 | 0 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.9 | 1.9 | 1.9 |
| 每行动命中次数 | `hits` | 个/次或开关 | 1 | 1 | 1 |
| 每普通攻击回技力 | `spPerOrdinaryAttack` | SP | 1 | 1 | 1 |
| 强化攻击回技力 | `spDuringEmpoweredAttack` | SP | 0 | 0 | 0 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 4次普通攻击后第5次190%法术；现有高甲优先索敌保留。

## D09 · 炎熔

职责：高台溅射术师。原作稀有度：3星（本作资料记录）。

- 0.65世界单位溅射，单目标DPS低于单法，群体接触时回报更高。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 140 | 28 | 2 | 0 | 2.8 | 3.5 | 0 | Arts |
| 2 | 290 | 50 | 4 | 0 | 2.8 | 3.5 | 0 | Arts |
| 3 | 460 | 76 | 6 | 0 | 2.8 | 3.5 | 0 | Arts |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.833333 |
| 1 | actionDuration | 1.566667 |
| 1 | splashRadius | 0.65 |
| 1 | healingRange | 3.5 |
| 1 | highGround | 是 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 10 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 10 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 10 |
| 2 | windup | 0.833333 |
| 2 | actionDuration | 1.566667 |
| 2 | splashRadius | 0.65 |
| 2 | healingRange | 3.5 |
| 2 | highGround | 是 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 17.857 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 17.857 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 17.857 |
| 3 | windup | 0.833333 |
| 3 | actionDuration | 1.566667 |
| 3 | splashRadius | 0.65 |
| 3 | healingRange | 3.5 |
| 3 | highGround | 是 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 27.143 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 27.143 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 27.143 |

### 战术咏唱·α型（LavaChant）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 0 | 5 | 10 |
| 所需技力 | `requiredSP` | SP | 32 | 34 | 36 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 12 | 16 | 20 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1.25 | 1.35 | 1.5 |
| 溅射半径 | `splashRadius` | 距离单位 | 0.65 | 0.65 | 0.65 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 0.65世界单位溅射，单目标DPS低于单法，群体接触时回报更高。

## D10 · 芙蓉

职责：高台单体医疗。原作稀有度：3星（本作资料记录）。

- attack字段为治疗效力，没有对怪物的普通伤害。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 150 | 20 | 2 | 0 | 2.2 | 3.5 | 0 | Physical |
| 2 | 300 | 34 | 4 | 0 | 2.2 | 3.5 | 0 | Physical |
| 3 | 480 | 52 | 6 | 0 | 2.2 | 3.5 | 0 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.466667 |
| 1 | actionDuration | 1 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 3.5 |
| 1 | highGround | 是 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 1 | ordinarySingleTargetHps | 9.091 |
| 2 | windup | 0.466667 |
| 2 | actionDuration | 1 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 3.5 |
| 2 | highGround | 是 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 2 | ordinarySingleTargetHps | 15.455 |
| 3 | windup | 0.466667 |
| 3 | actionDuration | 1 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 3.5 |
| 3 | highGround | 是 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 3 | ordinarySingleTargetHps | 23.636 |

### 治疗强化·α型（HibiscusHealing）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 0 | 4 | 8 |
| 所需技力 | `requiredSP` | SP | 28 | 30 | 32 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 12 | 16 | 20 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.3 | 1.4 | 1.5 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- attack字段为治疗效力，没有对怪物的普通伤害。

## D11 · 安赛尔

职责：高台扩距/概率双疗。原作稀有度：未核实。

- attack字段为治疗效力；15%额外治疗只能跳至第二个不同且受伤的合法友军。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 150 | 22 | 2 | 0 | 2.85 | 2.5 | 0 | Physical |
| 2 | 300 | 40 | 4 | 0 | 2.85 | 2.5 | 0 | Physical |
| 3 | 480 | 60 | 6 | 0 | 2.85 | 2.5 | 0 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.4 |
| 1 | actionDuration | 1 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 2.5 |
| 1 | highGround | 是 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0.15 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 1 | ordinarySingleTargetHps | 7.719 |
| 2 | windup | 0.4 |
| 2 | actionDuration | 1 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 2.5 |
| 2 | highGround | 是 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0.15 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 2 | ordinarySingleTargetHps | 14.035 |
| 3 | windup | 0.4 |
| 3 | actionDuration | 1 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 2.5 |
| 3 | highGround | 是 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0.15 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 3 | ordinarySingleTargetHps | 21.053 |

### 治疗范围强化（AnselRange）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 5 | 8 | 10 |
| 所需技力 | `requiredSP` | SP | 30 | 32 | 35 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 14 | 18 | 22 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.2 | 1.3 | 1.4 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 2 | 2 | 2 |
| 额外治疗概率 | `extraHealChance` | 倍率 | 0.15 | 0.15 | 0.15 |
| 额外治疗目标 | `extraHealTargets` | 个/次或开关 | 1 | 1 | 1 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- attack字段为治疗效力；15%额外治疗只能跳至第二个不同且受伤的合法友军。

## D12 · 梓兰

职责：高台停顿辅助。原作稀有度：未核实。

- 命中停顿0.8秒、移速×0.2，持续时间刷新不相加；不减攻速；免控单位沿既有免疫。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 140 | 18 | 2 | 0 | 1.8 | 4.5 | 0 | Arts |
| 2 | 280 | 34 | 4 | 0 | 1.8 | 4.5 | 0 | Arts |
| 3 | 440 | 52 | 6 | 0 | 1.8 | 4.5 | 0 | Arts |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.566667 |
| 1 | actionDuration | 1 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 3.5 |
| 1 | highGround | 是 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 10 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 10 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 10 |
| 2 | windup | 0.566667 |
| 2 | actionDuration | 1 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 3.5 |
| 2 | highGround | 是 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 18.889 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 18.889 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 18.889 |
| 3 | windup | 0.566667 |
| 3 | actionDuration | 1 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 3.5 |
| 3 | highGround | 是 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 28.889 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 28.889 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 28.889 |

### 迅捷打击·α型（OrchidSwiftStrike）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 0 | 5 | 10 |
| 所需技力 | `requiredSP` | SP | 34 | 36 | 38 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 12 | 16 | 20 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.15 | 1.2 | 1.25 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1.15 | 1.2 | 1.25 |
| 停顿持续 | `hinderDuration` | 秒 | 0.8 | 0.8 | 0.8 |
| 停顿移速倍率 | `hinderMoveMultiplier` | 倍率 | 0.2 | 0.2 | 0.2 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 命中停顿0.8秒、移速×0.2，持续时间刷新不相加；不减攻速；免控单位沿既有免疫。

## D21 · 荒芜拉普兰德

职责：高台法术/全场浮游追猎。原作稀有度：6星（本作资料记录）。

- 三个浮游可以集中同一目标；范围伤害不重叠，但浮游逐次命中会叠加，必须同时报告分散与集中上界。
- T3全部浮游集火加领域的无抗近似上界125~130 DPS，转火与追踪会降低实战值。
- 浮游不可被单独攻击，没有给它新增生命条。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 360 | 16 | 4 | 0 | 1.3 | 3.5 | 0 | Arts |
| 2 | 650 | 26 | 7 | 0 | 1.3 | 3.5 | 0 | Arts |
| 3 | 900 | 36 | 10 | 0 | 1.3 | 3.5 | 0 | Arts |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.4 |
| 1 | actionDuration | 1.2 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 是 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDirectDpsVsArmor0_12_30[0] | 12.308 |
| 1 | ordinaryDirectDpsVsArmor0_12_30[1] | 12.308 |
| 1 | ordinaryDirectDpsVsArmor0_12_30[2] | 12.308 |
| 1 | ordinarySteadyDpsWithOneDroneAtRampCapVsResistance0 | 17.846 |
| 1 | skillUpperSingleTargetDpsThreeDronesConcentratedVsResistance0 | 41.76 |
| 1 | conditionalSkillMetrics.singleTargetConcentrationUpperDpsNoResist | 41.76 |
| 1 | conditionalSkillMetrics.requires | 3 drones fully ramped on same target and area contact; does not include travel/retarget loss |
| 2 | windup | 0.4 |
| 2 | actionDuration | 1.2 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 是 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDirectDpsVsArmor0_12_30[0] | 20 |
| 2 | ordinaryDirectDpsVsArmor0_12_30[1] | 20 |
| 2 | ordinaryDirectDpsVsArmor0_12_30[2] | 20 |
| 2 | ordinarySteadyDpsWithOneDroneAtRampCapVsResistance0 | 30 |
| 2 | skillUpperSingleTargetDpsThreeDronesConcentratedVsResistance0 | 79.1 |
| 2 | conditionalSkillMetrics.singleTargetConcentrationUpperDpsNoResist | 79.1 |
| 2 | conditionalSkillMetrics.requires | 3 drones fully ramped on same target and area contact; does not include travel/retarget loss |
| 3 | windup | 0.4 |
| 3 | actionDuration | 1.2 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 是 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDirectDpsVsArmor0_12_30[0] | 27.692 |
| 3 | ordinaryDirectDpsVsArmor0_12_30[1] | 27.692 |
| 3 | ordinaryDirectDpsVsArmor0_12_30[2] | 27.692 |
| 3 | ordinarySteadyDpsWithOneDroneAtRampCapVsResistance0 | 42.923 |
| 3 | skillUpperSingleTargetDpsThreeDronesConcentratedVsResistance0 | 126.277 |
| 3 | conditionalSkillMetrics.singleTargetConcentrationUpperDpsNoResist | 126.277 |
| 3 | conditionalSkillMetrics.requires | 3 drones fully ramped on same target and area contact; does not include travel/retarget loss |

### 终幕·浩劫（LapplandHavoc）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 10 | 20 | 30 |
| 所需技力 | `requiredSP` | SP | 45 | 50 | 55 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 16 | 20 | 24 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.3 | 1.4 | 1.5 |
| 浮游单元数 | `droneCount` | 个/次或开关 | 3 | 3 | 3 |
| 浮游起始倍率 | `droneInitialRamp` | 倍率 | 0.2 | 0.2 | 0.2 |
| 浮游每命中增长 | `droneRampPerHit` | 倍率 | 0.1 | 0.125 | 0.15 |
| 浮游倍率上限 | `droneRampCap` | 倍率 | 0.45 | 0.5 | 0.55 |
| 浮游散开起手 | `droneFirstScatterSeconds` | 秒 | 1.3 | 1.3 | 1.3 |
| 浮游接敌恐惧时间 | `droneContactFear` | 个/次或开关 | 0.75 | 1 | 1.25 |
| 浮游领域伤害倍率 | `droneAreaAttackFraction` | 倍率 | 0.2 | 0.25 | 0.3 |
| 浮游领域伤害间隔 | `droneAreaInterval` | 秒 | 1 | 1 | 1 |
| 浮游领域伤害半径 | `droneAreaRadius` | 距离单位 | 0.9 | 0.9 | 0.9 |
| 浮游领域移速倍率 | `droneSlowMultiplier` | 倍率 | 0.8 | 0.7 | 0.6 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 0.8 | 0.7 | 0.6 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 3 | 3 | 3 |

- 三个浮游可以集中同一目标；范围伤害不重叠，但浮游逐次命中会叠加，必须同时报告分散与集中上界。
- T3全部浮游集火加领域的无抗近似上界125~130 DPS，转火与追踪会降低实战值。
- 浮游不可被单独攻击，没有给它新增生命条。

## D22 · 余

职责：地面重装/火墙阵地支援。原作稀有度：6星（本作资料记录）。

- 至少4个守军存活才启用原生命恢复机制；技能时推广全队，未改变此触发身份。
- 火墙只消除跨墙的合格来袭弹道，不删除近战和全屏雨；法术跨墙积累灼燃的机制保留。
- 灼燃爆发属于守军余的数字设计，不擅自套到玩家深池/其他元素源。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 520 | 28 | 12 | 0 | 1.6 | 0.9 | 3 | Physical |
| 2 | 950 | 52 | 20 | 0 | 1.6 | 0.9 | 3 | Physical |
| 3 | 1400 | 78 | 28 | 0 | 1.6 | 0.9 | 3 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.4 |
| 1 | actionDuration | 1.2 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 17.5 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 10 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0.625 |
| 1 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[0] | 24.5 |
| 1 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[1] | 17 |
| 1 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[2] | 5.75 |
| 1 | conditionalSkillMetrics.skillMaxHp | 728 |
| 1 | conditionalSkillMetrics.regenHpsPerDefenderIfFourAlive | 4.368 |
| 2 | windup | 0.4 |
| 2 | actionDuration | 1.2 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 32.5 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 25 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 13.75 |
| 2 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[0] | 48.75 |
| 2 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[1] | 41.25 |
| 2 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[2] | 30 |
| 2 | conditionalSkillMetrics.skillMaxHp | 1425 |
| 2 | conditionalSkillMetrics.regenHpsPerDefenderIfFourAlive | 11.4 |
| 3 | windup | 0.4 |
| 3 | actionDuration | 1.2 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 48.75 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 41.25 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 30 |
| 3 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[0] | 78 |
| 3 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[1] | 70.5 |
| 3 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[2] | 59.25 |
| 3 | conditionalSkillMetrics.skillMaxHp | 2240 |
| 3 | conditionalSkillMetrics.regenHpsPerDefenderIfFourAlive | 22.4 |

### 灶里乾坤（YuKitchen）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 10 | 18 | 26 |
| 所需技力 | `requiredSP` | SP | 42 | 46 | 50 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 18 | 24 | 30 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.4 | 1.5 | 1.6 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1.4 | 1.5 | 1.6 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1.4 | 1.5 | 1.6 |
| 技能半径 | `effectRadius` | 距离单位 | 5 | 5 | 5 |
| 火墙起手 | `wallStartupSeconds` | 秒 | 1.6 | 1.6 | 1.6 |
| 跨墙法术灼燃比例 | `wallElementAttackFraction` | 倍率 | 0.05 | 0.055 | 0.06 |
| 合格来袭弹道消除概率 | `projectileEraseChance` | 倍率 | 0.08 | 0.1 | 0.12 |
| 恢复所需存活守军 | `minLivingDefendersForRegen` | 个/次或开关 | 4 | 4 | 4 |
| 每秒最大生命回复比例 | `regenMaxHpFractionPerSecond` | 倍率 | 0.006 | 0.008 | 0.01 |
| 普通单位灼燃阈值 | `scorchThresholdNormal` | 个/次或开关 | 100 | 100 | 100 |
| 首领灼燃阈值 | `scorchThresholdBoss` | 个/次或开关 | 200 | 200 | 200 |
| 灼燃爆发真伤 | `scorchBurstTrueDamage` | HP | 180 | 280 | 420 |
| 灼燃法抗降低 | `scorchResistanceReduction` | percentage point | 10 | 12 | 15 |
| 灼燃衰减持续 | `scorchDebuffSeconds` | 秒 | 6 | 8 | 10 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0.05 | 0.055 | 0.06 |
| 消弹概率 | `eraseChance` | 倍率 | 0.08 | 0.1 | 0.12 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 至少4个守军存活才启用原生命恢复机制；技能时推广全队，未改变此触发身份。
- 火墙只消除跨墙的合格来袭弹道，不删除近战和全屏雨；法术跨墙积累灼燃的机制保留。
- 灼燃爆发属于守军余的数字设计，不擅自套到玩家深池/其他元素源。

## D23 · Mon3tr

职责：高台链疗→地面三目标真实伤害。原作稀有度：6星（本作资料记录）。

- Mon3tr本体不是新增玩家怪物，平时只链疗，受伤队友不足会延缓技能。
- 治疗行动一次只回1SP，跳跃不额外回SP；技能态不回SP；真实伤害不扣甲/法抗。
- 回到高台仍需被再次击杀，地面技能致命伤返回不等于死亡复活。
- 当前DefenderSkillSettings校验要求spPerSecond字段为正，资产保持1；AttackRecovery路径实际时间回SP为0，此值不是自动回SP。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 400 | 16 | 8 | 0 | 2.833333 | 3.5 | 3 | Physical |
| 2 | 750 | 24 | 12 | 0 | 2.833333 | 3.5 | 3 | Physical |
| 3 | 1100 | 34 | 16 | 0 | 2.833333 | 3.5 | 3 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.35 |
| 1 | actionDuration | 2.833333 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 4 |
| 1 | highGround | 是 |
| 1 | noOrdinaryAttack | 是 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 1 | ordinarySingleTargetHps | 5.647 |
| 1 | chainTotalHpsIfThreeHurtInRange | 13.059 |
| 1 | conditionalSkillMetrics.trueDpsPerTarget | 33.6 |
| 1 | conditionalSkillMetrics.maxTargets | 3 |
| 1 | conditionalSkillMetrics.trueHit | 44.8 |
| 1 | conditionalSkillMetrics.skillMaxHp | 650 |
| 1 | conditionalSkillMetrics.selfLossPerSecond | 4 |
| 2 | windup | 0.35 |
| 2 | actionDuration | 2.833333 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 4 |
| 2 | highGround | 是 |
| 2 | noOrdinaryAttack | 是 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 2 | ordinarySingleTargetHps | 8.471 |
| 2 | chainTotalHpsIfThreeHurtInRange | 19.588 |
| 2 | conditionalSkillMetrics.trueDpsPerTarget | 57.6 |
| 2 | conditionalSkillMetrics.maxTargets | 3 |
| 2 | conditionalSkillMetrics.trueHit | 76.8 |
| 2 | conditionalSkillMetrics.skillMaxHp | 1100 |
| 2 | conditionalSkillMetrics.selfLossPerSecond | 6 |
| 3 | windup | 0.35 |
| 3 | actionDuration | 2.833333 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 4 |
| 3 | highGround | 是 |
| 3 | noOrdinaryAttack | 是 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 3 | ordinarySingleTargetHps | 12 |
| 3 | chainTotalHpsIfThreeHurtInRange | 27.75 |
| 3 | conditionalSkillMetrics.trueDpsPerTarget | 89.25 |
| 3 | conditionalSkillMetrics.maxTargets | 3 |
| 3 | conditionalSkillMetrics.trueHit | 119 |
| 3 | conditionalSkillMetrics.skillMaxHp | 1600 |
| 3 | conditionalSkillMetrics.selfLossPerSecond | 8 |

### 策略：熔毁（Mon3trMeltdown）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 5 | 6 | 7 |
| 所需技力 | `requiredSP` | SP | 14 | 15 | 16 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 12 | 15 | 18 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 2.8 | 3.2 | 3.5 |
| skillAttackInterval | `skillAttackInterval` | 秒 | 1.333333 | 1.333333 | 1.333333 |
| 目标上限 | `maxTargets` | 个/次或开关 | 3 | 3 | 3 |
| 技能半径 | `effectRadius` | 距离单位 | 0.95 | 0.95 | 0.95 |
| 额外生命 | `maxHealthAddition` | HP | 250 | 350 | 500 |
| 每秒自损生命 | `selfHealthLossPerSecond` | HP | 4 | 6 | 8 |
| 攻击自疗比例 | `attackSelfHealFraction` | 倍率 | 0.35 | 0.4 | 0.5 |
| 链疗额外目标 | `chainAdditionalTargets` | 个/次或开关 | 2 | 2 | 2 |
| 链疗跳跃半径 | `chainHopRadius` | 距离单位 | 2 | 2 | 2 |
| 每跳治疗倍率 | `chainHopMultiplier` | 倍率 | 0.75 | 0.75 | 0.75 |
| 每治疗行动回技力 | `spPerHealingAction` | SP | 1 | 1 | 1 |
| 返回保护时间 | `returnProtectionSeconds` | 秒 | 1 | 1 | 1 |
| 返回动作时间 | `returnActionSeconds` | 秒 | 0.833333 | 0.833333 | 0.833333 |
| 技能起手时间 | `startupSeconds` | 秒 | 0.7 | 0.7 | 0.7 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |
| 实际每秒回技力 | `effectiveSPPerSecond` | SP/秒 | 0 | 0 | 0 |

- Mon3tr本体不是新增玩家怪物，平时只链疗，受伤队友不足会延缓技能。
- 治疗行动一次只回1SP，跳跃不额外回SP；技能态不回SP；真实伤害不扣甲/法抗。
- 回到高台仍需被再次击杀，地面技能致命伤返回不等于死亡复活。

## D24 · 新约能天使

职责：远程多弹物理/单次备援投递。原作稀有度：6星（本作资料记录）。

- 50/35/25发弹药是整段技能预算，逐发扣甲；不能以5发总和一次扣甲。
- 只投递尚未登场备援一次，不复活已死守军；树海无备援列表则落地伤害为0。
- duration=99999是当前弹药技能的实现哨兵，实际以弹药/动作结束，不是无限持续。
- 当前代码的1%最大生命/秒流失发生在整个存活期间，不仅技能期间；最低1HP。保留该实际机制，不伪称仅技能态。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 360 | 12 | 4 | 0 | 1.333333 | 3.5 | 1 | Physical |
| 2 | 650 | 20 | 8 | 0 | 1.333333 | 3.5 | 1 | Physical |
| 3 | 950 | 28 | 12 | 0 | 1.333333 | 3.5 | 1 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.35 |
| 1 | actionDuration | 1.333333 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 9 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 0.75 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0.75 |
| 1 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[0] | 59.4 |
| 1 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[1] | 14.4 |
| 1 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[2] | 3.75 |
| 1 | conditionalSkillMetrics.fullAmmoDamagePerTargetArmor0_12_30[0] | 396 |
| 1 | conditionalSkillMetrics.fullAmmoDamagePerTargetArmor0_12_30[1] | 96 |
| 1 | conditionalSkillMetrics.fullAmmoDamagePerTargetArmor0_12_30[2] | 25 |
| 1 | conditionalSkillMetrics.maxFullActions | 5 |
| 2 | windup | 0.35 |
| 2 | actionDuration | 1.333333 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 15 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 6 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 0.75 |
| 2 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[0] | 107.813 |
| 2 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[1] | 62.813 |
| 2 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[2] | 3.75 |
| 2 | conditionalSkillMetrics.fullAmmoDamagePerTargetArmor0_12_30[0] | 1006.25 |
| 2 | conditionalSkillMetrics.fullAmmoDamagePerTargetArmor0_12_30[1] | 586.25 |
| 2 | conditionalSkillMetrics.fullAmmoDamagePerTargetArmor0_12_30[2] | 35 |
| 2 | conditionalSkillMetrics.maxFullActions | 7 |
| 3 | windup | 0.35 |
| 3 | actionDuration | 1.333333 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 21 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 12 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 0.75 |
| 3 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[0] | 170.1 |
| 3 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[1] | 125.1 |
| 3 | conditionalSkillMetrics.physicalDpsPerTargetArmor0_12_30[2] | 57.6 |
| 3 | conditionalSkillMetrics.fullAmmoDamagePerTargetArmor0_12_30[0] | 2268 |
| 3 | conditionalSkillMetrics.fullAmmoDamagePerTargetArmor0_12_30[1] | 1668 |
| 3 | conditionalSkillMetrics.fullAmmoDamagePerTargetArmor0_12_30[2] | 768 |
| 3 | conditionalSkillMetrics.maxFullActions | 10 |

### 使命必达！（ExusiaiDelivery）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 10 | 16 | 22 |
| 所需技力 | `requiredSP` | SP | 32 | 35 | 38 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 99999 | 99999 | 99999 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.1 | 1.15 | 1.2 |
| 逐发伤害倍率 | `perBulletAttackMultiplier` | 倍率 | 1.2 | 1.25 | 1.35 |
| 每行动子弹数 | `bulletsPerAttack` | 个/次或开关 | 5 | 5 | 5 |
| 弹药 | `ammo` | 个/次或开关 | 25 | 35 | 50 |
| 逐发间隔 | `bulletSpacing` | 秒 | 0.033333 | 0.033333 | 0.033333 |
| 仅首次投递 | `reinforcementOnlyOnce` | 个/次或开关 | 1 | 1 | 1 |
| 备援初始技力增量 | `reinforcementSP` | SP | 3 | 4 | 6 |
| 投递落地伤害倍率 | `landingAttackMultiplier` | 倍率 | 1.5 | 2 | 2.5 |
| 投递伤害半径 | `landingRadius` | 距离单位 | 1.3 | 1.3 | 1.3 |
| 每秒最大生命自损比例 | `selfMaxHealthLossFractionPerSecond` | 倍率 | 0.01 | 0.01 | 0.01 |
| 自损最低生命 | `minimumSelfHealth` | HP | 1 | 1 | 1 |
| 技能起手时间 | `startupSeconds` | 秒 | 0.7 | 0.7 | 0.7 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 50/35/25发弹药是整段技能预算，逐发扣甲；不能以5发总和一次扣甲。
- 只投递尚未登场备援一次，不复活已死守军；树海无备援列表则落地伤害为0。
- duration=99999是当前弹药技能的实现哨兵，实际以弹药/动作结束，不是无限持续。

## D25 · 斩业星熊

职责：地面法术双打/负生命保护。原作稀有度：6星（本作资料记录）。

- 我执负生命不能成为普通治疗目标；达到债务阈值且不处于最终保护才击倒。
- 最终保护是有限时间后强制退场；需把HP、债务预算和保护窗口一起写入Boss伤害需求。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 500 | 18 | 12 | 0 | 1.6 | 0.9 | 3 | Physical |
| 2 | 900 | 28 | 20 | 0 | 1.6 | 0.9 | 3 | Physical |
| 3 | 1300 | 38 | 28 | 0 | 1.6 | 0.9 | 3 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.35 |
| 1 | actionDuration | 1.6 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 11.25 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 3.75 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0.625 |
| 1 | conditionalSkillMetrics.skillMaxHp | 700 |
| 1 | conditionalSkillMetrics.debtBudgetBeforeFinalProtection | 525 |
| 1 | conditionalSkillMetrics.maxFinitePreProtectionDamageBudget | 1225 |
| 1 | conditionalSkillMetrics.maximumExtraBudgetAboveBase | 725 |
| 1 | conditionalSkillMetrics.normalArtsDpsPerTarget | 40.5 |
| 1 | conditionalSkillMetrics.finalArtsDpsPerTarget | 81 |
| 1 | conditionalSkillMetrics.targets | 2 |
| 1 | conditionalSkillMetrics.finalProtectionSeconds | 6 |
| 1 | conditionalSkillMetrics.finalProtectionCanBeKilledByDamage | 否 |
| 2 | windup | 0.35 |
| 2 | actionDuration | 1.6 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 17.5 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 10 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 0.625 |
| 2 | conditionalSkillMetrics.skillMaxHp | 1305 |
| 2 | conditionalSkillMetrics.debtBudgetBeforeFinalProtection | 1305 |
| 2 | conditionalSkillMetrics.maxFinitePreProtectionDamageBudget | 2610 |
| 2 | conditionalSkillMetrics.maximumExtraBudgetAboveBase | 1710 |
| 2 | conditionalSkillMetrics.normalArtsDpsPerTarget | 70 |
| 2 | conditionalSkillMetrics.finalArtsDpsPerTarget | 140 |
| 2 | conditionalSkillMetrics.targets | 2 |
| 2 | conditionalSkillMetrics.finalProtectionSeconds | 7 |
| 2 | conditionalSkillMetrics.finalProtectionCanBeKilledByDamage | 否 |
| 3 | windup | 0.35 |
| 3 | actionDuration | 1.6 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 23.75 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 16.25 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 5 |
| 3 | conditionalSkillMetrics.skillMaxHp | 1950 |
| 3 | conditionalSkillMetrics.debtBudgetBeforeFinalProtection | 2437.5 |
| 3 | conditionalSkillMetrics.maxFinitePreProtectionDamageBudget | 4387.5 |
| 3 | conditionalSkillMetrics.maximumExtraBudgetAboveBase | 3087.5 |
| 3 | conditionalSkillMetrics.normalArtsDpsPerTarget | 104.5 |
| 3 | conditionalSkillMetrics.finalArtsDpsPerTarget | 209 |
| 3 | conditionalSkillMetrics.targets | 2 |
| 3 | conditionalSkillMetrics.finalProtectionSeconds | 8 |
| 3 | conditionalSkillMetrics.finalProtectionCanBeKilledByDamage | 否 |

### 地狱变相（HoshigumaInferno）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 8 | 15 | 22 |
| 所需技力 | `requiredSP` | SP | 40 | 45 | 50 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 24 | 28 | 32 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.8 | 2 | 2.2 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1.4 | 1.45 | 1.5 |
| 目标数 | `targets` | 个/次或开关 | 2 | 2 | 2 |
| 技能每行动连击 | `normalSkillHits` | 个/次或开关 | 2 | 2 | 2 |
| 最终阶段每行动连击 | `finalSkillHits` | 个/次或开关 | 4 | 4 | 4 |
| 逐段命中间隔 | `hitSpacing` | 秒 | 0.08 | 0.08 | 0.08 |
| 负生命债务上限倍率 | `debtMaxHealthMultiplier` | 倍率 | 0.75 | 1 | 1.25 |
| 无伤后回复等待 | `noDamageBeforeDebtRecovery` | 个/次或开关 | 3 | 4 | 5 |
| 负生命每秒回复比例 | `debtRecoveryMaxHealthFractionPerSecond` | 倍率 | 0.05 | 0.05 | 0.05 |
| AI终结最早时刻 | `autoCloseAfterSeconds` | 秒 | 12 | 14 | 16 |
| 最终保护时长 | `finalProtectionSeconds` | 秒 | 6 | 7 | 8 |
| 技能半径 | `effectRadius` | 距离单位 | 2.5 | 2.5 | 2.5 |
| 技能起手时间 | `startupSeconds` | 秒 | 0.7 | 0.7 | 0.7 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |

- 我执负生命不能成为普通治疗目标；达到债务阈值且不处于最终保护才击倒。
- 最终保护是有限时间后强制退场；需把HP、债务预算和保护窗口一起写入Boss伤害需求。

## D26 · 凛御银灰

职责：地面直线物理/备援协同。原作稀有度：6星（本作资料记录）。

- 直线范围、反隐、短暂脆弱与搭档第二道攻击保留；攻击可对齐同一目标，重合上界是两路，不可只报一路。
- 费用池是独立守军池，不修改玩家DP/金币；备援最多单伙伴，不能复制已死守军。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 420 | 28 | 8 | 0 | 1.2 | 3.5 | 2 | Physical |
| 2 | 800 | 48 | 14 | 0 | 1.2 | 3.5 | 2 | Physical |
| 3 | 1200 | 72 | 20 | 0 | 1.2 | 3.5 | 2 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.3 |
| 1 | actionDuration | 1.2 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 23.333 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 13.333 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0.833 |
| 1 | conditionalSkillMetrics.physicalDpsPerOriginArmor0_12_30[0] | 30.333 |
| 1 | conditionalSkillMetrics.physicalDpsPerOriginArmor0_12_30[1] | 20.333 |
| 1 | conditionalSkillMetrics.physicalDpsPerOriginArmor0_12_30[2] | 5.333 |
| 1 | conditionalSkillMetrics.maximumOrigins | 2 |
| 1 | conditionalSkillMetrics.steadyDamageTakenMultiplier | 1.1 |
| 1 | conditionalSkillMetrics.requires | partner alive and both line corridors actually hit; amplification after first successful hit |
| 2 | windup | 0.3 |
| 2 | actionDuration | 1.2 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 40 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 30 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 15 |
| 2 | conditionalSkillMetrics.physicalDpsPerOriginArmor0_12_30[0] | 58 |
| 2 | conditionalSkillMetrics.physicalDpsPerOriginArmor0_12_30[1] | 48 |
| 2 | conditionalSkillMetrics.physicalDpsPerOriginArmor0_12_30[2] | 33 |
| 2 | conditionalSkillMetrics.maximumOrigins | 2 |
| 2 | conditionalSkillMetrics.steadyDamageTakenMultiplier | 1.15 |
| 2 | conditionalSkillMetrics.requires | partner alive and both line corridors actually hit; amplification after first successful hit |
| 3 | windup | 0.3 |
| 3 | actionDuration | 1.2 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 60 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 50 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 35 |
| 3 | conditionalSkillMetrics.physicalDpsPerOriginArmor0_12_30[0] | 96 |
| 3 | conditionalSkillMetrics.physicalDpsPerOriginArmor0_12_30[1] | 86 |
| 3 | conditionalSkillMetrics.physicalDpsPerOriginArmor0_12_30[2] | 71 |
| 3 | conditionalSkillMetrics.maximumOrigins | 2 |
| 3 | conditionalSkillMetrics.steadyDamageTakenMultiplier | 1.2 |
| 3 | conditionalSkillMetrics.requires | partner alive and both line corridors actually hit; amplification after first successful hit |

### 变革已至（SilverAshReign）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 10 | 18 | 25 |
| 所需技力 | `requiredSP` | SP | 45 | 50 | 55 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 16 | 20 | 24 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.3 | 1.45 | 1.6 |
| 脆弱增伤 | `vulnerability` | 倍率 | 0.1 | 0.15 | 0.2 |
| 脆弱持续 | `vulnerabilitySeconds` | 秒 | 2 | 2 | 2 |
| 直线半宽 | `lineHalfWidth` | 距离单位 | 0.8 | 0.8 | 0.8 |
| 技能半径 | `effectRadius` | 距离单位 | 5.5 | 5.5 | 5.5 |
| 守军独立费用初始 | `independentBudgetInitial` | independent defender budget | 16 | 22 | 30 |
| 开启时增加守军费用 | `budgetInstant` | independent defender budget | 4 | 5 | 6 |
| 持续期间守军费用总量 | `budgetDuringSkillTotal` | independent defender budget | 8 | 12 | 16 |
| 高费备援原价 | `reserveHighCost` | independent defender budget | 20 | 20 | 20 |
| 低费备援原价 | `reserveLowCost` | independent defender budget | 8 | 8 | 8 |
| 仅首次交换费用 | `exchangeOnlyOnce` | 个/次或开关 | 1 | 1 | 1 |
| 协同搭档上限 | `maxActivePartner` | 个/次或开关 | 1 | 1 | 1 |
| 技能起手时间 | `startupSeconds` | 秒 | 0.35 | 0.35 | 0.35 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 直线范围、反隐、短暂脆弱与搭档第二道攻击保留；攻击可对齐同一目标，重合上界是两路，不可只报一路。
- 费用池是独立守军池，不修改玩家DP/金币；备援最多单伙伴，不能复制已死守军。

## D27 · 圣聆初雪

职责：高台积雪/诱导法术阵地。原作稀有度：6星（本作资料记录）。

- 常态不普攻、护甲×3和额外20法抗；主动时失去该常态防护并开始范围法术攻击。
- 积雪只影响地面且离开清空，诱导沿原路线投影运动，免控/动作锁仍有效。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 340 | 16 | 6 | 0 | 2 | 3.5 | 0 | Physical |
| 2 | 650 | 26 | 10 | 0 | 2 | 3.5 | 0 | Physical |
| 3 | 950 | 38 | 14 | 0 | 2 | 3.5 | 0 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.4 |
| 1 | actionDuration | 2 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 是 |
| 1 | noOrdinaryAttack | 是 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 1 | idleArmor | 18 |
| 1 | idleResistance | 20 |
| 1 | conditionalSkillMetrics.artsDpsPerTargetResistance0_20_40[0] | 25.116 |
| 1 | conditionalSkillMetrics.artsDpsPerTargetResistance0_20_40[1] | 22.604 |
| 1 | conditionalSkillMetrics.artsDpsPerTargetResistance0_20_40[2] | 17.581 |
| 1 | conditionalSkillMetrics.targets | all legal within skill radius; snow entry separate |
| 2 | windup | 0.4 |
| 2 | actionDuration | 2 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 是 |
| 2 | noOrdinaryAttack | 是 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 2 | idleArmor | 30 |
| 2 | idleResistance | 20 |
| 2 | conditionalSkillMetrics.artsDpsPerTargetResistance0_20_40[0] | 45.864 |
| 2 | conditionalSkillMetrics.artsDpsPerTargetResistance0_20_40[1] | 41.278 |
| 2 | conditionalSkillMetrics.artsDpsPerTargetResistance0_20_40[2] | 32.105 |
| 2 | conditionalSkillMetrics.targets | all legal within skill radius; snow entry separate |
| 3 | windup | 0.4 |
| 3 | actionDuration | 2 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 是 |
| 3 | noOrdinaryAttack | 是 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 3 | idleArmor | 42 |
| 3 | idleResistance | 20 |
| 3 | conditionalSkillMetrics.artsDpsPerTargetResistance0_20_40[0] | 74.812 |
| 3 | conditionalSkillMetrics.artsDpsPerTargetResistance0_20_40[1] | 67.331 |
| 3 | conditionalSkillMetrics.artsDpsPerTargetResistance0_20_40[2] | 52.369 |
| 3 | conditionalSkillMetrics.targets | all legal within skill radius; snow entry separate |

### 群山俯首（PramanixSnow）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 10 | 20 | 30 |
| 所需技力 | `requiredSP` | SP | 42 | 47 | 52 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 14 | 18 | 22 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.3 | 1.4 | 1.5 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1.15 | 1.2 | 1.25 |
| 范围攻击倍率 | `areaAttackMultiplier` | 倍率 | 2.1 | 2.1 | 2.1 |
| 忽略法抗 | `ignoreResistance` | percentage point | 10 | 10 | 10 |
| 常态防御倍率 | `idleDefenseMultiplier` | 倍率 | 3 | 3 | 3 |
| 常态额外法抗 | `idleResistanceBonus` | percentage point | 20 | 20 | 20 |
| 常态积雪间隔 | `snowIntervalIdle` | 秒 | 10 | 10 | 10 |
| 技能积雪间隔 | `snowIntervalSkill` | 秒 | 2.5 | 2 | 1.5 |
| 积雪层数上限 | `snowMaxLayers` | 个/次或开关 | 5 | 5 | 5 |
| 每层积雪减速 | `snowSlowPerLayer` | 倍率 | 0.08 | 0.09 | 0.1 |
| 踏雪伤害倍率 | `snowEntryAttackFraction` | 倍率 | 0.3 | 0.4 | 0.5 |
| 雪格触发半径 | `snowCellRadius` | 距离单位 | 0.7 | 0.7 | 0.7 |
| 诱导时长 | `lureDuration` | 秒 | 3 | 4 | 5 |
| 技能半径 | `effectRadius` | 距离单位 | 4.5 | 4.5 | 4.5 |
| 技能起手时间 | `startupSeconds` | 秒 | 0.35 | 0.35 | 0.35 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 常态不普攻、护甲×3和额外20法抗；主动时失去该常态防护并开始范围法术攻击。
- 积雪只影响地面且离开清空，诱导沿原路线投影运动，免控/动作锁仍有效。

## D28 · 望

职责：高台棋子陷阱/有限弹药爆发。原作稀有度：6星（本作资料记录）。

- 棋子不能当作生命单位；相邻激活、范围触发后消失，未增加额外自然补充或玩家选格系统。
- 陷阱按18/28/40基础attack与独立陷阱倍率计算，普通弹与棋子不混为一个攻击。
- duration=99999为弹药哨兵；无目标4秒提前结束并把余弹转库存，上限仍7。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 340 | 18 | 4 | 0 | 2.2 | 3.5 | 0 | Physical |
| 2 | 650 | 28 | 8 | 0 | 2.2 | 3.5 | 0 | Physical |
| 3 | 950 | 40 | 12 | 0 | 2.2 | 3.5 | 0 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.4 |
| 1 | actionDuration | 2.2 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 是 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDirectDpsVsArmor0_12_30[0] | 8.182 |
| 1 | ordinaryDirectDpsVsArmor0_12_30[1] | 2.727 |
| 1 | ordinaryDirectDpsVsArmor0_12_30[2] | 0.455 |
| 1 | passiveTrapHitVsResistance0 | 36 |
| 1 | conditionalSkillMetrics.trapHitArtsNoResist | 36 |
| 1 | conditionalSkillMetrics.maximumPlacedPiecesPerAction | 2 |
| 1 | conditionalSkillMetrics.conditionalTrapDpsIfEveryPieceHitsOneTarget | 32.727 |
| 1 | conditionalSkillMetrics.requires | all spawned pieces trigger within placement cycle; stock, ammo, adjacency and travel constrain this upper bound |
| 2 | windup | 0.4 |
| 2 | actionDuration | 2.2 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 是 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDirectDpsVsArmor0_12_30[0] | 12.727 |
| 2 | ordinaryDirectDpsVsArmor0_12_30[1] | 7.273 |
| 2 | ordinaryDirectDpsVsArmor0_12_30[2] | 0.455 |
| 2 | passiveTrapHitVsResistance0 | 61.6 |
| 2 | conditionalSkillMetrics.trapHitArtsNoResist | 61.6 |
| 2 | conditionalSkillMetrics.maximumPlacedPiecesPerAction | 3 |
| 2 | conditionalSkillMetrics.conditionalTrapDpsIfEveryPieceHitsOneTarget | 84 |
| 2 | conditionalSkillMetrics.requires | all spawned pieces trigger within placement cycle; stock, ammo, adjacency and travel constrain this upper bound |
| 3 | windup | 0.4 |
| 3 | actionDuration | 2.2 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 是 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDirectDpsVsArmor0_12_30[0] | 18.182 |
| 3 | ordinaryDirectDpsVsArmor0_12_30[1] | 12.727 |
| 3 | ordinaryDirectDpsVsArmor0_12_30[2] | 4.545 |
| 3 | passiveTrapHitVsResistance0 | 96 |
| 3 | conditionalSkillMetrics.trapHitArtsNoResist | 96 |
| 3 | conditionalSkillMetrics.maximumPlacedPiecesPerAction | 4 |
| 3 | conditionalSkillMetrics.conditionalTrapDpsIfEveryPieceHitsOneTarget | 174.545 |
| 3 | conditionalSkillMetrics.requires | all spawned pieces trigger within placement cycle; stock, ammo, adjacency and travel constrain this upper bound |

### 天下劫（WangGambit）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 10 | 20 | 30 |
| 所需技力 | `requiredSP` | SP | 42 | 48 | 54 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 99999 | 99999 | 99999 |
| 棋子伤害倍率 | `trapAttackMultiplier` | 倍率 | 2 | 2.2 | 2.4 |
| 棋子初始库存 | `stockInitial` | 个/次或开关 | 7 | 7 | 7 |
| 棋子库存上限 | `stockCap` | 个/次或开关 | 7 | 7 | 7 |
| 开启时增加棋子 | `stockGrantOnSkill` | 个/次或开关 | 4 | 6 | 8 |
| 弹药 | `ammo` | 个/次或开关 | 9 | 12 | 18 |
| 每次额外棋子 | `extraPiecesPerPlacement` | 个/次或开关 | 1 | 2 | 3 |
| 自动布置间隔 | `placementInterval` | 秒 | 2.2 | 2.2 | 2.2 |
| 棋子相邻连线距离 | `linkDistance` | 距离单位 | 1.1 | 1.1 | 1.1 |
| 触发半径 | `triggerRadius` | 距离单位 | 0.85 | 0.85 | 0.85 |
| 爆发半径 | `explosionRadius` | 距离单位 | 1.5 | 1.5 | 1.5 |
| 无目标提前结束 | `noTargetEndSeconds` | 秒 | 4 | 4 | 4 |
| 技能半径 | `effectRadius` | 距离单位 | 5.5 | 5.5 | 5.5 |
| 技能起手时间 | `startupSeconds` | 秒 | 0.35 | 0.35 | 0.35 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1 | 1 | 1 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 棋子不能当作生命单位；相邻激活、范围触发后消失，未增加额外自然补充或玩家选格系统。
- 陷阱按18/28/40基础attack与独立陷阱倍率计算，普通弹与棋子不混为一个攻击。
- duration=99999为弹药哨兵；无目标4秒提前结束并把余弹转库存，上限仍7。

## D29 · 塞雷娅

职责：地面治疗重装/法术增伤阵地。原作稀有度：6星（本作资料记录）。

- 常态优先治疗受伤友军，满血可普攻；技能时停止普通攻击/单体治疗，改为每秒群疗。
- 有效治疗一次给受疗者1SP，激活技能期间仍不能提前回转。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 480 | 24 | 10 | 0 | 1.2 | 0.9 | 3 | Physical |
| 2 | 900 | 40 | 18 | 0 | 1.2 | 0.9 | 3 | Physical |
| 3 | 1350 | 60 | 26 | 0 | 1.2 | 0.9 | 3 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.4 |
| 1 | actionDuration | 1.166667 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 4 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 否 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 20 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 10 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0.833 |
| 1 | ordinarySingleTargetHps | 20 |
| 1 | skillHpsPerTarget | 4.8 |
| 1 | conditionalSkillMetrics.auraHpsEachTarget | 4.8 |
| 1 | conditionalSkillMetrics.artsDamageTakenMultiplier | 1.15 |
| 1 | conditionalSkillMetrics.movementMultiplier | 0.65 |
| 2 | windup | 0.4 |
| 2 | actionDuration | 1.166667 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 4 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 否 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 33.333 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 23.333 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 8.333 |
| 2 | ordinarySingleTargetHps | 33.333 |
| 2 | skillHpsPerTarget | 8.8 |
| 2 | conditionalSkillMetrics.auraHpsEachTarget | 8.8 |
| 2 | conditionalSkillMetrics.artsDamageTakenMultiplier | 1.25 |
| 2 | conditionalSkillMetrics.movementMultiplier | 0.55 |
| 3 | windup | 0.4 |
| 3 | actionDuration | 1.166667 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 4 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 否 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 50 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 40 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 25 |
| 3 | ordinarySingleTargetHps | 50 |
| 3 | skillHpsPerTarget | 15 |
| 3 | conditionalSkillMetrics.auraHpsEachTarget | 15 |
| 3 | conditionalSkillMetrics.artsDamageTakenMultiplier | 1.35 |
| 3 | conditionalSkillMetrics.movementMultiplier | 0.45 |

### 钙质化（SariaCalcification）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 20 | 30 | 40 |
| 所需技力 | `requiredSP` | SP | 45 | 55 | 65 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 10 | 14 | 18 |
| 每秒群疗攻击比例 | `healAttackFractionPerSecond` | 倍率 | 0.2 | 0.22 | 0.25 |
| 领域治疗间隔 | `auraHealingInterval` | 秒 | 1 | 1 | 1 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1.15 | 1.25 | 1.35 |
| 移速倍率 | `moveMultiplier` | 倍率 | 0.65 | 0.55 | 0.45 |
| 每次有效治疗回复技力 | `spPerEffectiveHeal` | SP | 1 | 1 | 1 |
| 技能半径 | `effectRadius` | 距离单位 | 4 | 4 | 4 |
| 技能起手时间 | `startupSeconds` | 秒 | 0.7 | 0.7 | 0.7 |
| 技能停止普通攻击 | `stopsOrdinaryAttack` | 个/次或开关 | 1 | 1 | 1 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1 | 1 | 1 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0.2 | 0.22 | 0.25 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |
| 蓄力时间 | `chargeSeconds` | 秒 | 0 | 0 | 0 |
| 目标数 | `targets` | 个/次 | 1 | 1 | 1 |

- 常态优先治疗受伤友军，满血可普攻；技能时停止普通攻击/单体治疗，改为每秒群疗。
- 有效治疗一次给受疗者1SP，激活技能期间仍不能提前回转。

## D30 · 玛恩纳

职责：地面蓄力解放者/五目标爆发。原作稀有度：6星（本作资料记录）。

- 平时不攻击也不阻挡；技能态按原配置挡3，五目标分别扣甲。
- 满40秒才达到技能蓄力加成上限；AI可以更早开技能，不能把满蓄力峰值当实战常驻DPS。

| 强度档 | HP | 攻击/治疗量 | 甲 | 法抗% | 间隔秒 | 范围 | 阻挡 | 伤害类型 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 420 | 8 | 6 | 0 | 1.2 | 0.9 | 3 | Physical |
| 2 | 800 | 12 | 12 | 0 | 1.2 | 0.9 | 3 | Physical |
| 3 | 1200 | 18 | 18 | 0 | 1.2 | 0.9 | 3 | Physical |

| 档 | 其他字段/条件指标 | 值 |
| --- | --- | --- |
| 1 | windup | 0.4 |
| 1 | actionDuration | 1.2 |
| 1 | splashRadius | 0 |
| 1 | healingRange | 0 |
| 1 | highGround | 否 |
| 1 | noOrdinaryAttack | 是 |
| 1 | rangedAttackMultiplier | 1 |
| 1 | criticalChance | 0 |
| 1 | criticalMultiplier | 1.5 |
| 1 | extraHealChance | 0 |
| 1 | blockRadius | 0.65 |
| 1 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 1 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 1 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 1 | idleBlocks | 0 |
| 1 | skillBlocks | 3 |
| 1 | skillPeakFullChargeSingleTargetDpsVsArmor0 | 49.333 |
| 1 | conditionalSkillMetrics.physicalDpsPerTargetAtFullChargeArmor0_12_30[0] | 46.667 |
| 1 | conditionalSkillMetrics.physicalDpsPerTargetAtFullChargeArmor0_12_30[1] | 36.667 |
| 1 | conditionalSkillMetrics.physicalDpsPerTargetAtFullChargeArmor0_12_30[2] | 21.667 |
| 1 | conditionalSkillMetrics.selfKasimirTrueDpsPerTargetAtFullCharge | 2.667 |
| 1 | conditionalSkillMetrics.maxTargets | 5 |
| 1 | conditionalSkillMetrics.requires | full 40s charge; AI first cast may be earlier; normal kills reduce bonus |
| 2 | windup | 0.4 |
| 2 | actionDuration | 1.2 |
| 2 | splashRadius | 0 |
| 2 | healingRange | 0 |
| 2 | highGround | 否 |
| 2 | noOrdinaryAttack | 是 |
| 2 | rangedAttackMultiplier | 1 |
| 2 | criticalChance | 0 |
| 2 | criticalMultiplier | 1.5 |
| 2 | extraHealChance | 0 |
| 2 | blockRadius | 0.65 |
| 2 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 2 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 2 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 2 | idleBlocks | 0 |
| 2 | skillBlocks | 3 |
| 2 | skillPeakFullChargeSingleTargetDpsVsArmor0 | 77 |
| 2 | conditionalSkillMetrics.physicalDpsPerTargetAtFullChargeArmor0_12_30[0] | 72.5 |
| 2 | conditionalSkillMetrics.physicalDpsPerTargetAtFullChargeArmor0_12_30[1] | 62.5 |
| 2 | conditionalSkillMetrics.physicalDpsPerTargetAtFullChargeArmor0_12_30[2] | 47.5 |
| 2 | conditionalSkillMetrics.selfKasimirTrueDpsPerTargetAtFullCharge | 4.5 |
| 2 | conditionalSkillMetrics.maxTargets | 5 |
| 2 | conditionalSkillMetrics.requires | full 40s charge; AI first cast may be earlier; normal kills reduce bonus |
| 3 | windup | 0.4 |
| 3 | actionDuration | 1.2 |
| 3 | splashRadius | 0 |
| 3 | healingRange | 0 |
| 3 | highGround | 否 |
| 3 | noOrdinaryAttack | 是 |
| 3 | rangedAttackMultiplier | 1 |
| 3 | criticalChance | 0 |
| 3 | criticalMultiplier | 1.5 |
| 3 | extraHealChance | 0 |
| 3 | blockRadius | 0.65 |
| 3 | ordinaryDpsVsArmor0_12_30[0] | 0 |
| 3 | ordinaryDpsVsArmor0_12_30[1] | 0 |
| 3 | ordinaryDpsVsArmor0_12_30[2] | 0 |
| 3 | idleBlocks | 0 |
| 3 | skillBlocks | 3 |
| 3 | skillPeakFullChargeSingleTargetDpsVsArmor0 | 120 |
| 3 | conditionalSkillMetrics.physicalDpsPerTargetAtFullChargeArmor0_12_30[0] | 112.5 |
| 3 | conditionalSkillMetrics.physicalDpsPerTargetAtFullChargeArmor0_12_30[1] | 102.5 |
| 3 | conditionalSkillMetrics.physicalDpsPerTargetAtFullChargeArmor0_12_30[2] | 87.5 |
| 3 | conditionalSkillMetrics.selfKasimirTrueDpsPerTargetAtFullCharge | 7.5 |
| 3 | conditionalSkillMetrics.maxTargets | 5 |
| 3 | conditionalSkillMetrics.requires | full 40s charge; AI first cast may be earlier; normal kills reduce bonus |

### 未照耀的荣光（MlynarGlory）

触发：AI: full SP and legal useful target; active skill has no SP recharge. Attack recovery for Kroos/Steward; healing-action recovery for Mon3tr.。伤害基准：本体attack；治疗角色attack为治疗量，真实伤害/多段/陷阱按下列参数分别计算。

| 参数 | 稳定key | 单位 | T1 | T2 | T3 |
| --- | --- | --- | --- | --- | --- |
| 初始技力 | `initialSP` | SP | 0 | 12 | 15 |
| 所需技力 | `requiredSP` | SP | 40 | 45 | 50 |
| 资产每秒回技力 | `spPerSecond` | SP/秒 | 1 | 1 | 1 |
| 持续时间 | `duration` | 秒 | 14 | 18 | 22 |
| 攻击倍率 | `attackMultiplier` | 倍率 | 1.4 | 1.45 | 1.5 |
| 蓄力时间 | `chargeSeconds` | 秒 | 40 | 40 | 40 |
| 常态满蓄力加成 | `idleChargeAttackBonus` | 倍率 | 2 | 2 | 2 |
| 技能满蓄力加成 | `skillChargeAttackBonus` | 倍率 | 4 | 4 | 4 |
| 普通击杀削减攻击加成 | `attackBonusLossPerOrdinaryKill` | 倍率 | 0.1 | 0.1 | 0.1 |
| 目标数 | `targets` | 个/次或开关 | 5 | 5 | 5 |
| 技能半径 | `effectRadius` | 距离单位 | 3.5 | 3.5 | 3.5 |
| 卡西米尔附加真伤比例 | `kasimierzTrueAttackFraction` | 倍率 | 0.08 | 0.09 | 0.1 |
| 技能起手时间 | `startupSeconds` | 秒 | 0.333333 | 0.333333 | 0.333333 |
| 攻速倍率 | `attackSpeedMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗比例 | `healFraction` | 倍率 | 0 | 0 | 0 |
| 治疗范围增量 | `healRangeBonus` | 距离单位 | 0 | 0 | 0 |
| 防御倍率 | `defenseMultiplier` | 倍率 | 1 | 1 | 1 |
| 治疗间隔 | `healingInterval` | 秒 | 0 | 0 | 0 |
| 生命倍率 | `healthMultiplier` | 倍率 | 1 | 1 | 1 |
| 法伤承受倍率 | `damageAmplification` | 倍率 | 1 | 1 | 1 |
| 移速倍率 | `moveMultiplier` | 倍率 | 1 | 1 | 1 |
| 灼燃积累比例 | `elementFraction` | 倍率 | 0 | 0 | 0 |
| 消弹概率 | `eraseChance` | 倍率 | 0 | 0 | 0 |

- 平时不攻击也不阻挡；技能态按原配置挡3，五目标分别扣甲。
- 满40秒才达到技能蓄力加成上限；AI可以更早开技能，不能把满蓄力峰值当实战常驻DPS。

## 黑流树海：独立九守军

以下为v2后续试玩目标，不能当作当前已运行树海资产。仍保留三波16/16/18DP、每波45秒、最终推进120秒的既有波次规则；其截止不能套到普通13关。

### D01 · 米格鲁

| 参数/规则 | 值 |
| --- | --- |
| position | {x: -4, y: 0.04, z: 2.5} |
| relayPosition | 不适用 |
| stats.tier | forest |
| stats.hp | 420 |
| stats.attack | 28 |
| stats.armor | 14 |
| stats.resistance | 0 |
| stats.interval | 2 |
| stats.range | 0.85 |
| stats.blocks | 3 |
| stats.damageType | Physical |
| stats.windup | 0.5 |
| stats.actionDuration | 1 |
| stats.splashRadius | 0 |
| stats.healingRange | 0 |
| stats.highGround | 否 |
| stats.noOrdinaryAttack | 否 |
| stats.rangedAttackMultiplier | 1 |
| stats.criticalChance | 0 |
| stats.criticalMultiplier | 1.5 |
| stats.extraHealChance | 0 |
| stats.blockRadius | 0.65 |
| skillId | BeagleGuard |
| skillParameters.initialSP | 0 |
| skillParameters.requiredSP | 40 |
| skillParameters.spPerSecond | 1 |
| skillParameters.duration | 20 |
| skillParameters.defenseMultiplier | 1.4 |
| skillParameters.attackMultiplier | 1 |
| skillParameters.attackSpeedMultiplier | 1 |
| skillParameters.healFraction | 0 |
| skillParameters.healRangeBonus | 0 |
| skillParameters.healingInterval | 0 |
| skillParameters.healthMultiplier | 1 |
| skillParameters.effectRadius | 0 |
| skillParameters.damageAmplification | 1 |
| skillParameters.moveMultiplier | 1 |
| skillParameters.elementFraction | 0 |
| skillParameters.eraseChance | 0 |
| skillParameters.chargeSeconds | 0 |
| skillParameters.targets | 1 |

- 此配置只用于黑流树海，不能作为所有13关的默认六星面板。
- 保留资产中的站位、动作、距离范围和实际阻挡。

### D08 · 史都华德

| 参数/规则 | 值 |
| --- | --- |
| position | {x: -5, y: 0.76, z: 1.5} |
| relayPosition | 不适用 |
| stats.tier | forest |
| stats.hp | 280 |
| stats.attack | 40 |
| stats.armor | 4 |
| stats.resistance | 0 |
| stats.interval | 2.4 |
| stats.range | 2.6 |
| stats.blocks | 0 |
| stats.damageType | Arts |
| stats.windup | 0.5 |
| stats.actionDuration | 0.866667 |
| stats.splashRadius | 0 |
| stats.healingRange | 3.5 |
| stats.highGround | 是 |
| stats.noOrdinaryAttack | 否 |
| stats.rangedAttackMultiplier | 1 |
| stats.criticalChance | 0 |
| stats.criticalMultiplier | 1.5 |
| stats.extraHealChance | 0 |
| stats.blockRadius | 0.65 |
| skillId | StewardStrike |
| skillParameters.initialSP | 0 |
| skillParameters.requiredSP | 4 |
| skillParameters.spPerSecond | 0 |
| skillParameters.duration | 0 |
| skillParameters.attackMultiplier | 1.7 |
| skillParameters.hits | 1 |
| skillParameters.spPerOrdinaryAttack | 1 |
| skillParameters.spDuringEmpoweredAttack | 0 |
| skillParameters.attackSpeedMultiplier | 1 |
| skillParameters.healFraction | 0 |
| skillParameters.healRangeBonus | 0 |
| skillParameters.defenseMultiplier | 1 |
| skillParameters.healingInterval | 0 |
| skillParameters.healthMultiplier | 1 |
| skillParameters.effectRadius | 0 |
| skillParameters.damageAmplification | 1 |
| skillParameters.moveMultiplier | 1 |
| skillParameters.elementFraction | 0 |
| skillParameters.eraseChance | 0 |
| skillParameters.chargeSeconds | 0 |
| skillParameters.targets | 1 |

- 此配置只用于黑流树海，不能作为所有13关的默认六星面板。
- 保留资产中的站位、动作、距离范围和实际阻挡。

### D23 · Mon3tr

| 参数/规则 | 值 |
| --- | --- |
| position | {x: 0, y: 0.76, z: -0.5} |
| relayPosition | {x: -1, y: 0.04, z: -1.5} |
| stats.tier | forest |
| stats.hp | 560 |
| stats.attack | 22 |
| stats.armor | 10 |
| stats.resistance | 0 |
| stats.interval | 2.833333 |
| stats.range | 3 |
| stats.blocks | 3 |
| stats.damageType | Physical |
| stats.windup | 0.35 |
| stats.actionDuration | 2.833333 |
| stats.splashRadius | 0 |
| stats.healingRange | 3 |
| stats.highGround | 是 |
| stats.noOrdinaryAttack | 是 |
| stats.rangedAttackMultiplier | 1 |
| stats.criticalChance | 0 |
| stats.criticalMultiplier | 1.5 |
| stats.extraHealChance | 0 |
| stats.blockRadius | 0.65 |
| stats.ordinarySingleTargetHps | 7.765 |
| stats.chainTotalHpsIfThreeHurtInRange | 17.956 |
| stats.skillTrueDpsPerTarget | 49.5 |
| skillId | Mon3trMeltdown |
| skillParameters.initialSP | 7 |
| skillParameters.requiredSP | 15 |
| skillParameters.spPerSecond | 1 |
| skillParameters.duration | 14 |
| skillParameters.attackMultiplier | 3 |
| skillParameters.skillAttackInterval | 1.333333 |
| skillParameters.maxTargets | 3 |
| skillParameters.effectRadius | 0.95 |
| skillParameters.maxHealthAddition | 250 |
| skillParameters.selfHealthLossPerSecond | 4 |
| skillParameters.attackSelfHealFraction | 0.4 |
| skillParameters.chainAdditionalTargets | 2 |
| skillParameters.chainHopRadius | 2 |
| skillParameters.chainHopMultiplier | 0.75 |
| skillParameters.spPerHealingAction | 1 |
| skillParameters.returnProtectionSeconds | 1 |
| skillParameters.returnActionSeconds | 0.833333 |
| skillParameters.startupSeconds | 0.7 |
| skillParameters.attackSpeedMultiplier | 1 |
| skillParameters.healFraction | 0 |
| skillParameters.healRangeBonus | 0 |
| skillParameters.defenseMultiplier | 1 |
| skillParameters.healingInterval | 0 |
| skillParameters.healthMultiplier | 1 |
| skillParameters.damageAmplification | 1 |
| skillParameters.moveMultiplier | 1 |
| skillParameters.elementFraction | 0 |
| skillParameters.eraseChance | 0 |
| skillParameters.chargeSeconds | 0 |
| skillParameters.targets | 1 |
| skillParameters.effectiveSPPerSecond | 0 |

- 此配置只用于黑流树海，不能作为所有13关的默认六星面板。
- 保留资产中的站位、动作、距离范围和实际阻挡。

### D29 · 塞雷娅

| 参数/规则 | 值 |
| --- | --- |
| position | {x: 0, y: 0.04, z: -1.5} |
| relayPosition | 不适用 |
| stats.tier | forest |
| stats.hp | 700 |
| stats.attack | 32 |
| stats.armor | 16 |
| stats.resistance | 0 |
| stats.interval | 1.2 |
| stats.range | 0.9 |
| stats.blocks | 3 |
| stats.damageType | Physical |
| stats.windup | 0.4 |
| stats.actionDuration | 1.166667 |
| stats.splashRadius | 0 |
| stats.healingRange | 3 |
| stats.highGround | 否 |
| stats.noOrdinaryAttack | 否 |
| stats.rangedAttackMultiplier | 1 |
| stats.criticalChance | 0 |
| stats.criticalMultiplier | 1.5 |
| stats.extraHealChance | 0 |
| stats.blockRadius | 0.65 |
| stats.ordinarySingleTargetHps | 26.667 |
| skillId | SariaCalcification |
| skillParameters.initialSP | 15 |
| skillParameters.requiredSP | 55 |
| skillParameters.spPerSecond | 1 |
| skillParameters.duration | 14 |
| skillParameters.healAttackFractionPerSecond | 0.2 |
| skillParameters.auraHealingInterval | 1 |
| skillParameters.damageAmplification | 1.2 |
| skillParameters.moveMultiplier | 0.6 |
| skillParameters.spPerEffectiveHeal | 1 |
| skillParameters.effectRadius | 3 |
| skillParameters.startupSeconds | 0.7 |
| skillParameters.stopsOrdinaryAttack | 1 |
| skillParameters.attackMultiplier | 1 |
| skillParameters.attackSpeedMultiplier | 1 |
| skillParameters.healFraction | 0.2 |
| skillParameters.healRangeBonus | 0 |
| skillParameters.defenseMultiplier | 1 |
| skillParameters.healingInterval | 0 |
| skillParameters.healthMultiplier | 1 |
| skillParameters.elementFraction | 0 |
| skillParameters.eraseChance | 0 |
| skillParameters.chargeSeconds | 0 |
| skillParameters.targets | 1 |

- 此配置只用于黑流树海，不能作为所有13关的默认六星面板。
- 保留资产中的站位、动作、距离范围和实际阻挡。

### D28 · 望

| 参数/规则 | 值 |
| --- | --- |
| position | {x: 1, y: 0.76, z: 3.5} |
| relayPosition | {x: 0, y: 0, z: 0} |
| stats.tier | forest |
| stats.hp | 420 |
| stats.attack | 20 |
| stats.armor | 5 |
| stats.resistance | 0 |
| stats.interval | 2.2 |
| stats.range | 2.8 |
| stats.blocks | 0 |
| stats.damageType | Physical |
| stats.windup | 0.4 |
| stats.actionDuration | 2.2 |
| stats.splashRadius | 0 |
| stats.healingRange | 0 |
| stats.highGround | 是 |
| stats.noOrdinaryAttack | 否 |
| stats.rangedAttackMultiplier | 1 |
| stats.criticalChance | 0 |
| stats.criticalMultiplier | 1.5 |
| stats.extraHealChance | 0 |
| stats.blockRadius | 0.65 |
| stats.trapDamage | 48 |
| skillId | WangGambit |
| skillParameters.initialSP | 15 |
| skillParameters.requiredSP | 48 |
| skillParameters.spPerSecond | 1 |
| skillParameters.duration | 99999 |
| skillParameters.trapAttackMultiplier | 2.4 |
| skillParameters.stockInitial | 7 |
| skillParameters.stockCap | 7 |
| skillParameters.stockGrantOnSkill | 6 |
| skillParameters.ammo | 12 |
| skillParameters.extraPiecesPerPlacement | 2 |
| skillParameters.placementInterval | 2.2 |
| skillParameters.linkDistance | 1.1 |
| skillParameters.triggerRadius | 0.85 |
| skillParameters.explosionRadius | 1.5 |
| skillParameters.noTargetEndSeconds | 4 |
| skillParameters.effectRadius | 3 |
| skillParameters.startupSeconds | 0.35 |
| skillParameters.attackMultiplier | 1 |
| skillParameters.attackSpeedMultiplier | 1 |
| skillParameters.healFraction | 0 |
| skillParameters.healRangeBonus | 0 |
| skillParameters.defenseMultiplier | 1 |
| skillParameters.healingInterval | 0 |
| skillParameters.healthMultiplier | 1 |
| skillParameters.damageAmplification | 1 |
| skillParameters.moveMultiplier | 1 |
| skillParameters.elementFraction | 0 |
| skillParameters.eraseChance | 0 |
| skillParameters.chargeSeconds | 0 |
| skillParameters.targets | 1 |

- 此配置只用于黑流树海，不能作为所有13关的默认六星面板。
- 保留资产中的站位、动作、距离范围和实际阻挡。

### D30 · 玛恩纳

| 参数/规则 | 值 |
| --- | --- |
| position | {x: 4, y: 0.04, z: 0.5} |
| relayPosition | 不适用 |
| stats.tier | forest |
| stats.hp | 650 |
| stats.attack | 10 |
| stats.armor | 10 |
| stats.resistance | 0 |
| stats.interval | 1.2 |
| stats.range | 0.9 |
| stats.blocks | 3 |
| stats.damageType | Physical |
| stats.windup | 0.4 |
| stats.actionDuration | 1.2 |
| stats.splashRadius | 0 |
| stats.healingRange | 0 |
| stats.highGround | 否 |
| stats.noOrdinaryAttack | 是 |
| stats.rangedAttackMultiplier | 1 |
| stats.criticalChance | 0 |
| stats.criticalMultiplier | 1.5 |
| stats.extraHealChance | 0 |
| stats.blockRadius | 0.65 |
| stats.idleBlocks | 0 |
| stats.skillBlocks | 3 |
| skillId | MlynarGlory |
| skillParameters.initialSP | 10 |
| skillParameters.requiredSP | 45 |
| skillParameters.spPerSecond | 1 |
| skillParameters.duration | 18 |
| skillParameters.attackMultiplier | 1.4 |
| skillParameters.chargeSeconds | 40 |
| skillParameters.idleChargeAttackBonus | 2 |
| skillParameters.skillChargeAttackBonus | 4 |
| skillParameters.attackBonusLossPerOrdinaryKill | 0.1 |
| skillParameters.targets | 5 |
| skillParameters.effectRadius | 3.2 |
| skillParameters.kasimierzTrueAttackFraction | 0.09 |
| skillParameters.startupSeconds | 0.333333 |
| skillParameters.attackSpeedMultiplier | 1 |
| skillParameters.healFraction | 0 |
| skillParameters.healRangeBonus | 0 |
| skillParameters.defenseMultiplier | 1 |
| skillParameters.healingInterval | 0 |
| skillParameters.healthMultiplier | 1 |
| skillParameters.damageAmplification | 1 |
| skillParameters.moveMultiplier | 1 |
| skillParameters.elementFraction | 0 |
| skillParameters.eraseChance | 0 |

- 此配置只用于黑流树海，不能作为所有13关的默认六星面板。
- 保留资产中的站位、动作、距离范围和实际阻挡。

### D24 · 新约能天使

| 参数/规则 | 值 |
| --- | --- |
| position | {x: 5, y: 0.76, z: 1.5} |
| relayPosition | {x: -5.5, y: 0.04, z: 2} |
| stats.tier | forest |
| stats.hp | 420 |
| stats.attack | 16 |
| stats.armor | 4 |
| stats.resistance | 0 |
| stats.interval | 1.333333 |
| stats.range | 2.6 |
| stats.blocks | 0 |
| stats.damageType | Physical |
| stats.windup | 0.35 |
| stats.actionDuration | 1.333333 |
| stats.splashRadius | 0 |
| stats.healingRange | 0 |
| stats.highGround | 是 |
| stats.noOrdinaryAttack | 否 |
| stats.rangedAttackMultiplier | 1 |
| stats.criticalChance | 0 |
| stats.criticalMultiplier | 1.5 |
| stats.extraHealChance | 0 |
| stats.blockRadius | 0.65 |
| stats.skillPhysicalDpsVsArmor0_12_30[0] | 82.5 |
| stats.skillPhysicalDpsVsArmor0_12_30[1] | 37.5 |
| stats.skillPhysicalDpsVsArmor0_12_30[2] | 3.75 |
| skillId | ExusiaiDelivery |
| skillParameters.initialSP | 10 |
| skillParameters.requiredSP | 38 |
| skillParameters.spPerSecond | 1 |
| skillParameters.duration | 99999 |
| skillParameters.attackMultiplier | 1.1 |
| skillParameters.perBulletAttackMultiplier | 1.25 |
| skillParameters.bulletsPerAttack | 5 |
| skillParameters.ammo | 35 |
| skillParameters.bulletSpacing | 0.033333 |
| skillParameters.reinforcementOnlyOnce | 1 |
| skillParameters.reinforcementSP | 4 |
| skillParameters.landingAttackMultiplier | 2 |
| skillParameters.landingRadius | 1.3 |
| skillParameters.selfMaxHealthLossFractionPerSecond | 0.01 |
| skillParameters.minimumSelfHealth | 1 |
| skillParameters.startupSeconds | 0.7 |
| skillParameters.effectRadius | 2.6 |
| skillParameters.attackSpeedMultiplier | 1 |
| skillParameters.healFraction | 0 |
| skillParameters.healRangeBonus | 0 |
| skillParameters.defenseMultiplier | 1 |
| skillParameters.healingInterval | 0 |
| skillParameters.healthMultiplier | 1 |
| skillParameters.damageAmplification | 1 |
| skillParameters.moveMultiplier | 1 |
| skillParameters.elementFraction | 0 |
| skillParameters.eraseChance | 0 |
| skillParameters.chargeSeconds | 0 |
| skillParameters.targets | 1 |

- 此配置只用于黑流树海，不能作为所有13关的默认六星面板。
- 保留资产中的站位、动作、距离范围和实际阻挡。

### D25 · 斩业星熊

| 参数/规则 | 值 |
| --- | --- |
| position | {x: 6, y: 0.04, z: -1.5} |
| relayPosition | {x: -5.5, y: 0.04, z: 2} |
| stats.tier | forest |
| stats.hp | 600 |
| stats.attack | 22 |
| stats.armor | 14 |
| stats.resistance | 0 |
| stats.interval | 1.6 |
| stats.range | 0.9 |
| stats.blocks | 3 |
| stats.damageType | Physical |
| stats.windup | 0.35 |
| stats.actionDuration | 1.6 |
| stats.splashRadius | 0 |
| stats.healingRange | 0 |
| stats.highGround | 否 |
| stats.noOrdinaryAttack | 否 |
| stats.rangedAttackMultiplier | 1 |
| stats.criticalChance | 0 |
| stats.criticalMultiplier | 1.5 |
| stats.extraHealChance | 0 |
| stats.blockRadius | 0.65 |
| stats.skillArtsDpsPerTarget | 55 |
| stats.finalArtsDpsPerTarget | 110 |
| stats.normalSkillHp | 840 |
| stats.maxDebtBudget | 840 |
| skillId | HoshigumaInferno |
| skillParameters.initialSP | 10 |
| skillParameters.requiredSP | 45 |
| skillParameters.spPerSecond | 1 |
| skillParameters.duration | 28 |
| skillParameters.attackMultiplier | 2 |
| skillParameters.healthMultiplier | 1.4 |
| skillParameters.targets | 2 |
| skillParameters.normalSkillHits | 2 |
| skillParameters.finalSkillHits | 4 |
| skillParameters.hitSpacing | 0.08 |
| skillParameters.debtMaxHealthMultiplier | 1 |
| skillParameters.noDamageBeforeDebtRecovery | 4 |
| skillParameters.debtRecoveryMaxHealthFractionPerSecond | 0.05 |
| skillParameters.autoCloseAfterSeconds | 14 |
| skillParameters.finalProtectionSeconds | 6 |
| skillParameters.effectRadius | 2.5 |
| skillParameters.startupSeconds | 0.7 |
| skillParameters.attackSpeedMultiplier | 1 |
| skillParameters.healFraction | 0 |
| skillParameters.healRangeBonus | 0 |
| skillParameters.defenseMultiplier | 1 |
| skillParameters.healingInterval | 0 |
| skillParameters.damageAmplification | 1 |
| skillParameters.moveMultiplier | 1 |
| skillParameters.elementFraction | 0 |
| skillParameters.eraseChance | 0 |
| skillParameters.chargeSeconds | 0 |

- 此配置只用于黑流树海，不能作为所有13关的默认六星面板。
- 保留资产中的站位、动作、距离范围和实际阻挡。

### D11 · 安赛尔

| 参数/规则 | 值 |
| --- | --- |
| position | {x: 7, y: 0.76, z: -2.5} |
| relayPosition | 不适用 |
| stats.tier | forest |
| stats.hp | 280 |
| stats.attack | 32 |
| stats.armor | 4 |
| stats.resistance | 0 |
| stats.interval | 2.85 |
| stats.range | 2 |
| stats.blocks | 0 |
| stats.damageType | Physical |
| stats.windup | 0.4 |
| stats.actionDuration | 1 |
| stats.splashRadius | 0 |
| stats.healingRange | 2 |
| stats.highGround | 是 |
| stats.noOrdinaryAttack | 否 |
| stats.rangedAttackMultiplier | 1 |
| stats.criticalChance | 0 |
| stats.criticalMultiplier | 1.5 |
| stats.extraHealChance | 0.15 |
| stats.blockRadius | 0.65 |
| stats.ordinarySingleTargetHps | 11.228 |
| skillId | AnselRange |
| skillParameters.initialSP | 10 |
| skillParameters.requiredSP | 35 |
| skillParameters.spPerSecond | 1 |
| skillParameters.duration | 18 |
| skillParameters.attackMultiplier | 1.3 |
| skillParameters.healRangeBonus | 2 |
| skillParameters.extraHealChance | 0.15 |
| skillParameters.extraHealTargets | 1 |
| skillParameters.attackSpeedMultiplier | 1 |
| skillParameters.healFraction | 0 |
| skillParameters.defenseMultiplier | 1 |
| skillParameters.healingInterval | 0 |
| skillParameters.healthMultiplier | 1 |
| skillParameters.effectRadius | 0 |
| skillParameters.damageAmplification | 1 |
| skillParameters.moveMultiplier | 1 |
| skillParameters.elementFraction | 0 |
| skillParameters.eraseChance | 0 |
| skillParameters.chargeSeconds | 0 |
| skillParameters.targets | 1 |

- 此配置只用于黑流树海，不能作为所有13关的默认六星面板。
- 保留资产中的站位、动作、距离范围和实际阻挡。

## 附加规范：derivedEntities

| 参数/规则 | 值 |
| --- | --- |
| [0].id | D21_DRONE |
| [0].name | 荒芜拉普兰德浮游单元 |
| [0].sourceUnitId | D21 |
| [0].role | 不可被单独攻击的既有效果/路标，不新增实体生命系统 |
| [0].stages[0].tier | 1 |
| [0].stages[0].hp | 不适用 |
| [0].stages[0].attackable | 否 |
| [0].stages[0].blocks | 0 |
| [0].stages[0].independentUnit | 否 |
| [0].stages[0].count | 3 |
| [0].stages[0].perDroneMaximumHit | 9.36 |
| [0].stages[0].areaHitPerSecond | 4.16 |
| [0].stages[0].fearSeconds | 0.75 |
| [0].stages[0].radius | 0.9 |
| [0].stages[1].tier | 2 |
| [0].stages[1].hp | 不适用 |
| [0].stages[1].attackable | 否 |
| [0].stages[1].blocks | 0 |
| [0].stages[1].independentUnit | 否 |
| [0].stages[1].count | 3 |
| [0].stages[1].perDroneMaximumHit | 18.2 |
| [0].stages[1].areaHitPerSecond | 9.1 |
| [0].stages[1].fearSeconds | 1 |
| [0].stages[1].radius | 0.9 |
| [0].stages[2].tier | 3 |
| [0].stages[2].hp | 不适用 |
| [0].stages[2].attackable | 否 |
| [0].stages[2].blocks | 0 |
| [0].stages[2].independentUnit | 否 |
| [0].stages[2].count | 3 |
| [0].stages[2].perDroneMaximumHit | 29.7 |
| [0].stages[2].areaHitPerSecond | 16.2 |
| [0].stages[2].fearSeconds | 1.25 |
| [0].stages[2].radius | 0.9 |
| [1].id | D22_WALL |
| [1].name | 余火墙 |
| [1].sourceUnitId | D22 |
| [1].role | 不可被单独攻击的既有效果/路标，不新增实体生命系统 |
| [1].stages[0].tier | 1 |
| [1].stages[0].hp | 不适用 |
| [1].stages[0].attackable | 否 |
| [1].stages[0].blocks | 0 |
| [1].stages[0].independentUnit | 否 |
| [1].stages[0].directDamage | 0 |
| [1].stages[0].accumulationPerCrossingArtsHit | 1.96 |
| [1].stages[0].scorchTrueBurst | 180 |
| [1].stages[0].eraseChance | 0.08 |
| [1].stages[1].tier | 2 |
| [1].stages[1].hp | 不适用 |
| [1].stages[1].attackable | 否 |
| [1].stages[1].blocks | 0 |
| [1].stages[1].independentUnit | 否 |
| [1].stages[1].directDamage | 0 |
| [1].stages[1].accumulationPerCrossingArtsHit | 4.29 |
| [1].stages[1].scorchTrueBurst | 280 |
| [1].stages[1].eraseChance | 0.1 |
| [1].stages[2].tier | 3 |
| [1].stages[2].hp | 不适用 |
| [1].stages[2].attackable | 否 |
| [1].stages[2].blocks | 0 |
| [1].stages[2].independentUnit | 否 |
| [1].stages[2].directDamage | 0 |
| [1].stages[2].accumulationPerCrossingArtsHit | 7.488 |
| [1].stages[2].scorchTrueBurst | 420 |
| [1].stages[2].eraseChance | 0.12 |
| [2].id | D23_RELAY |
| [2].name | Mon3tr地面重构路标 |
| [2].sourceUnitId | D23 |
| [2].role | 不可被单独攻击的既有效果/路标，不新增实体生命系统 |
| [2].stages[0].tier | 1 |
| [2].stages[0].hp | 不适用 |
| [2].stages[0].attackable | 否 |
| [2].stages[0].blocks | 0 |
| [2].stages[0].independentUnit | 否 |
| [2].stages[0].directDamage | 0 |
| [2].stages[0].bodyHpAddition | 250 |
| [2].stages[0].skillBodyTrueHit | 44.8 |
| [2].stages[0].skillBodyTargets | 3 |
| [2].stages[0].skillBodyRange | 0.95 |
| [2].stages[1].tier | 2 |
| [2].stages[1].hp | 不适用 |
| [2].stages[1].attackable | 否 |
| [2].stages[1].blocks | 0 |
| [2].stages[1].independentUnit | 否 |
| [2].stages[1].directDamage | 0 |
| [2].stages[1].bodyHpAddition | 350 |
| [2].stages[1].skillBodyTrueHit | 76.8 |
| [2].stages[1].skillBodyTargets | 3 |
| [2].stages[1].skillBodyRange | 0.95 |
| [2].stages[2].tier | 3 |
| [2].stages[2].hp | 不适用 |
| [2].stages[2].attackable | 否 |
| [2].stages[2].blocks | 0 |
| [2].stages[2].independentUnit | 否 |
| [2].stages[2].directDamage | 0 |
| [2].stages[2].bodyHpAddition | 500 |
| [2].stages[2].skillBodyTrueHit | 119 |
| [2].stages[2].skillBodyTargets | 3 |
| [2].stages[2].skillBodyRange | 0.95 |
| [3].id | D26_EYE |
| [3].name | 凛御银灰协同眼 |
| [3].sourceUnitId | D26 |
| [3].role | 不可被单独攻击的既有效果/路标，不新增实体生命系统 |
| [3].stages[0].tier | 1 |
| [3].stages[0].hp | 不适用 |
| [3].stages[0].attackable | 否 |
| [3].stages[0].blocks | 0 |
| [3].stages[0].independentUnit | 否 |
| [3].stages[0].markerDirectDamage | 0 |
| [3].stages[0].partnerOriginPhysicalHit | 36.4 |
| [3].stages[0].lineHalfWidth | 0.8 |
| [3].stages[1].tier | 2 |
| [3].stages[1].hp | 不适用 |
| [3].stages[1].attackable | 否 |
| [3].stages[1].blocks | 0 |
| [3].stages[1].independentUnit | 否 |
| [3].stages[1].markerDirectDamage | 0 |
| [3].stages[1].partnerOriginPhysicalHit | 69.6 |
| [3].stages[1].lineHalfWidth | 0.8 |
| [3].stages[2].tier | 3 |
| [3].stages[2].hp | 不适用 |
| [3].stages[2].attackable | 否 |
| [3].stages[2].blocks | 0 |
| [3].stages[2].independentUnit | 否 |
| [3].stages[2].markerDirectDamage | 0 |
| [3].stages[2].partnerOriginPhysicalHit | 115.2 |
| [3].stages[2].lineHalfWidth | 0.8 |
| [4].id | D27_SNOW |
| [4].name | 圣聆初雪积雪格 |
| [4].sourceUnitId | D27 |
| [4].role | 不可被单独攻击的既有效果/路标，不新增实体生命系统 |
| [4].stages[0].tier | 1 |
| [4].stages[0].hp | 不适用 |
| [4].stages[0].attackable | 否 |
| [4].stages[0].blocks | 0 |
| [4].stages[0].independentUnit | 否 |
| [4].stages[0].entryArtsHit | 4.8 |
| [4].stages[0].maxLayers | 5 |
| [4].stages[0].maxMoveReduction | 0.4 |
| [4].stages[0].triggerRadius | 0.7 |
| [4].stages[1].tier | 2 |
| [4].stages[1].hp | 不适用 |
| [4].stages[1].attackable | 否 |
| [4].stages[1].blocks | 0 |
| [4].stages[1].independentUnit | 否 |
| [4].stages[1].entryArtsHit | 10.4 |
| [4].stages[1].maxLayers | 5 |
| [4].stages[1].maxMoveReduction | 0.45 |
| [4].stages[1].triggerRadius | 0.7 |
| [4].stages[2].tier | 3 |
| [4].stages[2].hp | 不适用 |
| [4].stages[2].attackable | 否 |
| [4].stages[2].blocks | 0 |
| [4].stages[2].independentUnit | 否 |
| [4].stages[2].entryArtsHit | 19 |
| [4].stages[2].maxLayers | 5 |
| [4].stages[2].maxMoveReduction | 0.5 |
| [4].stages[2].triggerRadius | 0.7 |
| [5].id | D28_CHESS |
| [5].name | 望棋子 |
| [5].sourceUnitId | D28 |
| [5].role | 不可被单独攻击的既有效果/路标，不新增实体生命系统 |
| [5].stages[0].tier | 1 |
| [5].stages[0].hp | 不适用 |
| [5].stages[0].attackable | 否 |
| [5].stages[0].blocks | 0 |
| [5].stages[0].independentUnit | 否 |
| [5].stages[0].artsExplosion | 36 |
| [5].stages[0].triggerRadius | 0.85 |
| [5].stages[0].explosionRadius | 1.5 |
| [5].stages[0].linkDistance | 1.1 |
| [5].stages[0].initialStock | 7 |
| [5].stages[0].stockCap | 7 |
| [5].stages[1].tier | 2 |
| [5].stages[1].hp | 不适用 |
| [5].stages[1].attackable | 否 |
| [5].stages[1].blocks | 0 |
| [5].stages[1].independentUnit | 否 |
| [5].stages[1].artsExplosion | 61.6 |
| [5].stages[1].triggerRadius | 0.85 |
| [5].stages[1].explosionRadius | 1.5 |
| [5].stages[1].linkDistance | 1.1 |
| [5].stages[1].initialStock | 7 |
| [5].stages[1].stockCap | 7 |
| [5].stages[2].tier | 3 |
| [5].stages[2].hp | 不适用 |
| [5].stages[2].attackable | 否 |
| [5].stages[2].blocks | 0 |
| [5].stages[2].independentUnit | 否 |
| [5].stages[2].artsExplosion | 96 |
| [5].stages[2].triggerRadius | 0.85 |
| [5].stages[2].explosionRadius | 1.5 |
| [5].stages[2].linkDistance | 1.1 |
| [5].stages[2].initialStock | 7 |
| [5].stages[2].stockCap | 7 |

## 附加规范：reserveAliases

| 参数/规则 | 值 |
| --- | --- |
| [0].id | D24_RESERVE_D01 |
| [0].name | 新约能天使投递备援米格鲁 |
| [0].baseId | D01 |
| [0].source | Assets/Data/Resources/DefenderTrials/D24.asset helper reserve=1 |
| [0].stages[0].tier | 1 |
| [0].stages[0].hp | 260 |
| [0].stages[0].attack | 16 |
| [0].stages[0].armor | 8 |
| [0].stages[0].resistance | 0 |
| [0].stages[0].interval | 1.6 |
| [0].stages[0].range | 0.85 |
| [0].stages[0].blocks | 3 |
| [0].stages[0].damageType | Physical |
| [0].stages[0].windup | 0.5 |
| [0].stages[0].actionDuration | 1 |
| [0].stages[0].splashRadius | 0 |
| [0].stages[0].healingRange | 0 |
| [0].stages[0].highGround | 否 |
| [0].stages[0].noOrdinaryAttack | 否 |
| [0].stages[0].rangedAttackMultiplier | 1 |
| [0].stages[0].criticalChance | 0 |
| [0].stages[0].criticalMultiplier | 1.5 |
| [0].stages[0].extraHealChance | 0 |
| [0].stages[0].blockRadius | 0.65 |
| [0].stages[0].ordinaryDpsVsArmor0_12_30[0] | 10 |
| [0].stages[0].ordinaryDpsVsArmor0_12_30[1] | 2.5 |
| [0].stages[0].ordinaryDpsVsArmor0_12_30[2] | 0.625 |
| [0].stages[0].reinforcementSP | 3 |
| [0].stages[1].tier | 2 |
| [0].stages[1].hp | 520 |
| [0].stages[1].attack | 32 |
| [0].stages[1].armor | 14 |
| [0].stages[1].resistance | 0 |
| [0].stages[1].interval | 1.6 |
| [0].stages[1].range | 0.85 |
| [0].stages[1].blocks | 3 |
| [0].stages[1].damageType | Physical |
| [0].stages[1].windup | 0.5 |
| [0].stages[1].actionDuration | 1 |
| [0].stages[1].splashRadius | 0 |
| [0].stages[1].healingRange | 0 |
| [0].stages[1].highGround | 否 |
| [0].stages[1].noOrdinaryAttack | 否 |
| [0].stages[1].rangedAttackMultiplier | 1 |
| [0].stages[1].criticalChance | 0 |
| [0].stages[1].criticalMultiplier | 1.5 |
| [0].stages[1].extraHealChance | 0 |
| [0].stages[1].blockRadius | 0.65 |
| [0].stages[1].ordinaryDpsVsArmor0_12_30[0] | 20 |
| [0].stages[1].ordinaryDpsVsArmor0_12_30[1] | 12.5 |
| [0].stages[1].ordinaryDpsVsArmor0_12_30[2] | 1.25 |
| [0].stages[1].reinforcementSP | 4 |
| [0].stages[2].tier | 3 |
| [0].stages[2].hp | 850 |
| [0].stages[2].attack | 50 |
| [0].stages[2].armor | 20 |
| [0].stages[2].resistance | 0 |
| [0].stages[2].interval | 1.6 |
| [0].stages[2].range | 0.85 |
| [0].stages[2].blocks | 3 |
| [0].stages[2].damageType | Physical |
| [0].stages[2].windup | 0.5 |
| [0].stages[2].actionDuration | 1 |
| [0].stages[2].splashRadius | 0 |
| [0].stages[2].healingRange | 0 |
| [0].stages[2].highGround | 否 |
| [0].stages[2].noOrdinaryAttack | 否 |
| [0].stages[2].rangedAttackMultiplier | 1 |
| [0].stages[2].criticalChance | 0 |
| [0].stages[2].criticalMultiplier | 1.5 |
| [0].stages[2].extraHealChance | 0 |
| [0].stages[2].blockRadius | 0.65 |
| [0].stages[2].ordinaryDpsVsArmor0_12_30[0] | 31.25 |
| [0].stages[2].ordinaryDpsVsArmor0_12_30[1] | 23.75 |
| [0].stages[2].ordinaryDpsVsArmor0_12_30[2] | 12.5 |
| [0].stages[2].reinforcementSP | 6 |
| [0].notes[0] | 本作样板已有备援，不是召唤出第二个新物种；没有预置备援时不得凭空生成。 |
| [1].id | D26_RESERVE_LOW_D01 |
| [1].name | 凛御银灰低费备援米格鲁 |
| [1].baseId | D01 |
| [1].source | Assets/Data/Resources/DefenderTrials/D26.asset reserve-low |
| [1].stages[0].tier | 1 |
| [1].stages[0].hp | 260 |
| [1].stages[0].attack | 16 |
| [1].stages[0].armor | 8 |
| [1].stages[0].resistance | 0 |
| [1].stages[0].interval | 1.6 |
| [1].stages[0].range | 0.85 |
| [1].stages[0].blocks | 3 |
| [1].stages[0].damageType | Physical |
| [1].stages[0].windup | 0.5 |
| [1].stages[0].actionDuration | 1 |
| [1].stages[0].splashRadius | 0 |
| [1].stages[0].healingRange | 0 |
| [1].stages[0].highGround | 否 |
| [1].stages[0].noOrdinaryAttack | 否 |
| [1].stages[0].rangedAttackMultiplier | 1 |
| [1].stages[0].criticalChance | 0 |
| [1].stages[0].criticalMultiplier | 1.5 |
| [1].stages[0].extraHealChance | 0 |
| [1].stages[0].blockRadius | 0.65 |
| [1].stages[0].ordinaryDpsVsArmor0_12_30[0] | 10 |
| [1].stages[0].ordinaryDpsVsArmor0_12_30[1] | 2.5 |
| [1].stages[0].ordinaryDpsVsArmor0_12_30[2] | 0.625 |
| [1].stages[0].reserveCost | 8 |
| [1].stages[1].tier | 2 |
| [1].stages[1].hp | 520 |
| [1].stages[1].attack | 32 |
| [1].stages[1].armor | 14 |
| [1].stages[1].resistance | 0 |
| [1].stages[1].interval | 1.6 |
| [1].stages[1].range | 0.85 |
| [1].stages[1].blocks | 3 |
| [1].stages[1].damageType | Physical |
| [1].stages[1].windup | 0.5 |
| [1].stages[1].actionDuration | 1 |
| [1].stages[1].splashRadius | 0 |
| [1].stages[1].healingRange | 0 |
| [1].stages[1].highGround | 否 |
| [1].stages[1].noOrdinaryAttack | 否 |
| [1].stages[1].rangedAttackMultiplier | 1 |
| [1].stages[1].criticalChance | 0 |
| [1].stages[1].criticalMultiplier | 1.5 |
| [1].stages[1].extraHealChance | 0 |
| [1].stages[1].blockRadius | 0.65 |
| [1].stages[1].ordinaryDpsVsArmor0_12_30[0] | 20 |
| [1].stages[1].ordinaryDpsVsArmor0_12_30[1] | 12.5 |
| [1].stages[1].ordinaryDpsVsArmor0_12_30[2] | 1.25 |
| [1].stages[1].reserveCost | 8 |
| [1].stages[2].tier | 3 |
| [1].stages[2].hp | 850 |
| [1].stages[2].attack | 50 |
| [1].stages[2].armor | 20 |
| [1].stages[2].resistance | 0 |
| [1].stages[2].interval | 1.6 |
| [1].stages[2].range | 0.85 |
| [1].stages[2].blocks | 3 |
| [1].stages[2].damageType | Physical |
| [1].stages[2].windup | 0.5 |
| [1].stages[2].actionDuration | 1 |
| [1].stages[2].splashRadius | 0 |
| [1].stages[2].healingRange | 0 |
| [1].stages[2].highGround | 否 |
| [1].stages[2].noOrdinaryAttack | 否 |
| [1].stages[2].rangedAttackMultiplier | 1 |
| [1].stages[2].criticalChance | 0 |
| [1].stages[2].criticalMultiplier | 1.5 |
| [1].stages[2].extraHealChance | 0 |
| [1].stages[2].blockRadius | 0.65 |
| [1].stages[2].ordinaryDpsVsArmor0_12_30[0] | 31.25 |
| [1].stages[2].ordinaryDpsVsArmor0_12_30[1] | 23.75 |
| [1].stages[2].ordinaryDpsVsArmor0_12_30[2] | 12.5 |
| [1].stages[2].reserveCost | 8 |
| [1].notes[0] | 首次交换费用后实际价格依当前备援列表改变，初始8不等于必定支付8。 |
| [2].id | D26_RESERVE_HIGH_D04 |
| [2].name | 凛御银灰高费备援玫兰莎（试验搭档） |
| [2].baseId | D04 |
| [2].source | Assets/Data/Resources/DefenderTrials/D26.asset reserve-high |
| [2].stages[0].tier | 1 |
| [2].stages[0].hp | 200 |
| [2].stages[0].attack | 30 |
| [2].stages[0].armor | 2 |
| [2].stages[0].resistance | 0 |
| [2].stages[0].interval | 2.5 |
| [2].stages[0].range | 0.9 |
| [2].stages[0].blocks | 3 |
| [2].stages[0].damageType | Physical |
| [2].stages[0].windup | 0.966667 |
| [2].stages[0].actionDuration | 2 |
| [2].stages[0].splashRadius | 0 |
| [2].stages[0].healingRange | 0 |
| [2].stages[0].highGround | 否 |
| [2].stages[0].noOrdinaryAttack | 否 |
| [2].stages[0].rangedAttackMultiplier | 1 |
| [2].stages[0].criticalChance | 0 |
| [2].stages[0].criticalMultiplier | 1.5 |
| [2].stages[0].extraHealChance | 0 |
| [2].stages[0].blockRadius | 0.65 |
| [2].stages[0].ordinaryDpsVsArmor0_12_30[0] | 12 |
| [2].stages[0].ordinaryDpsVsArmor0_12_30[1] | 7.2 |
| [2].stages[0].ordinaryDpsVsArmor0_12_30[2] | 0.4 |
| [2].stages[0].reserveCost | 20 |
| [2].stages[1].tier | 2 |
| [2].stages[1].hp | 420 |
| [2].stages[1].attack | 62 |
| [2].stages[1].armor | 4 |
| [2].stages[1].resistance | 0 |
| [2].stages[1].interval | 2.5 |
| [2].stages[1].range | 0.9 |
| [2].stages[1].blocks | 3 |
| [2].stages[1].damageType | Physical |
| [2].stages[1].windup | 0.966667 |
| [2].stages[1].actionDuration | 2 |
| [2].stages[1].splashRadius | 0 |
| [2].stages[1].healingRange | 0 |
| [2].stages[1].highGround | 否 |
| [2].stages[1].noOrdinaryAttack | 否 |
| [2].stages[1].rangedAttackMultiplier | 1 |
| [2].stages[1].criticalChance | 0 |
| [2].stages[1].criticalMultiplier | 1.5 |
| [2].stages[1].extraHealChance | 0 |
| [2].stages[1].blockRadius | 0.65 |
| [2].stages[1].ordinaryDpsVsArmor0_12_30[0] | 24.8 |
| [2].stages[1].ordinaryDpsVsArmor0_12_30[1] | 20 |
| [2].stages[1].ordinaryDpsVsArmor0_12_30[2] | 12.8 |
| [2].stages[1].reserveCost | 20 |
| [2].stages[2].tier | 3 |
| [2].stages[2].hp | 700 |
| [2].stages[2].attack | 100 |
| [2].stages[2].armor | 6 |
| [2].stages[2].resistance | 0 |
| [2].stages[2].interval | 2.5 |
| [2].stages[2].range | 0.9 |
| [2].stages[2].blocks | 3 |
| [2].stages[2].damageType | Physical |
| [2].stages[2].windup | 0.966667 |
| [2].stages[2].actionDuration | 2 |
| [2].stages[2].splashRadius | 0 |
| [2].stages[2].healingRange | 0 |
| [2].stages[2].highGround | 否 |
| [2].stages[2].noOrdinaryAttack | 否 |
| [2].stages[2].rangedAttackMultiplier | 1 |
| [2].stages[2].criticalChance | 0 |
| [2].stages[2].criticalMultiplier | 1.5 |
| [2].stages[2].extraHealChance | 0 |
| [2].stages[2].blockRadius | 0.65 |
| [2].stages[2].ordinaryDpsVsArmor0_12_30[0] | 40 |
| [2].stages[2].ordinaryDpsVsArmor0_12_30[1] | 35.2 |
| [2].stages[2].ordinaryDpsVsArmor0_12_30[2] | 28 |
| [2].stages[2].reserveCost | 20 |
| [2].notes[0] | 保留当前样板挡3配置；此非玫兰莎规范挡1身份，作为试验派生别名独列，不自动投入正式13关。正式关卡若使用规范玫兰莎应选择D04。 |

## 附加规范：coverage

| 参数/规则 | 值 |
| --- | --- |
| implementedDistinctUnitCount | 22 |
| implementedIds[0] | D01 |
| implementedIds[1] | D02 |
| implementedIds[2] | D03 |
| implementedIds[3] | D04 |
| implementedIds[4] | D05 |
| implementedIds[5] | D06 |
| implementedIds[6] | D07 |
| implementedIds[7] | D08 |
| implementedIds[8] | D09 |
| implementedIds[9] | D10 |
| implementedIds[10] | D11 |
| implementedIds[11] | D12 |
| implementedIds[12] | D21 |
| implementedIds[13] | D22 |
| implementedIds[14] | D23 |
| implementedIds[15] | D24 |
| implementedIds[16] | D25 |
| implementedIds[17] | D26 |
| implementedIds[18] | D27 |
| implementedIds[19] | D28 |
| implementedIds[20] | D29 |
| implementedIds[21] | D30 |
| notImplementedIds[0] | D13 |
| notImplementedIds[1] | D14 |
| notImplementedIds[2] | D15 |
| notImplementedIds[3] | D16 |
| notImplementedIds[4] | D17 |
| notImplementedIds[5] | D18 |
| notImplementedIds[6] | D19 |
| notImplementedIds[7] | D20 |
| notImplementedNames[0] | 蛇屠箱 |
| notImplementedNames[1] | 古米 |
| notImplementedNames[2] | 慕斯 |
| notImplementedNames[3] | 拉普兰德 |
| notImplementedNames[4] | 白雪 |
| notImplementedNames[5] | 远山 |
| notImplementedNames[6] | 调香师 |
| notImplementedNames[7] | 苏苏洛 |
| notImplementedPolicy | 仅有预览/后续批次清单，没有独立DefenderTrials资产和已接入技能；不伪造现有参数，可另设计未来表。 |
| forestCount | 9 |
| ordinaryTestSupportAliases | support/partner/blocker/helper全部为D01试验搭档；caster-a/caster-b为D08/D09试验别名；统一回指规范人物表，不当作新干员。 |
| talulahPhaseFixture | TalulahPhaseDefenders.asset中的三名50000HP克洛丝只用于双阶段回归夹具，不参与正式关卡难度；不得把它作为本作真实干员基础面板。 |

## 附加规范：formulas

| 参数/规则 | 值 |
| --- | --- |
| physicalHit | max(1, attackAfterAllMultipliers - armor) |
| artsHit | max(1, attackAfterAllMultipliers * (1-resistance/100)) |
| singleTargetDps | sum(expected independent hit damage per action) / effective interval |
| effectiveInterval | ordinaryInterval / attackSpeedMultiplier; keep complete animation at the same configured speed |
| kroosCycle | 4 ordinary attacks then a double empowered attack; crit separately each arrow, armor separately each arrow |
| stewardCycle | 4 ordinary arts attacks then one 1.9x arts attack, so sustained pre-resistance modifier=(4+1.9)/5=1.18 |
| medicHps | healingAttack / healingInterval; only actual missing HP counts |
| mon3trChainTotalHps | attack / 2.833333 * (1+0.75+0.75^2), if three distinct wounded legal targets |
| forestProtectionDebt | normalSkillHp = baseHP*healthMultiplier; debtBudget = normalSkillHp*debtMaxHealthMultiplier; final protection is time-window pressure, not extra finite HP |
