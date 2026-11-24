# 愚人节维维美：绝对主角原资源

2026-11-18，本轮用户明确要求直接恢复原作愚人节皮肤。正式卡ID仍是`CARD_WISADEL_APRIL`；新原生包为`BossRoster/WIS_APRIL_G9/main`，不复用B20超新星身份。

## 身份和来源

- 原`skin_table.json`中的`char_1035_wisdel@game#9`，显示名“绝对主角”，battleSkin为`char_1035_wisdel_game#9`。`@sale#14`显示名“超新星”，严格分开。
- 原表：[ArknightsGameData/skin_table.json](https://raw.githubusercontent.com/Kengxxiao/ArknightsGameData/master/zh_CN/gamedata/excel/skin_table.json)。本次读到3,945,749字节，SHA256=`6bcaa2ade4f6fffb43a5dc3e1aee8ae220c18c177c4f13afcdfd88d57dc2fb14`；保留维什戴尔条目子集，不下载整个仓库。
- 原身体仅定向取得PRTS既有源的front三文件：`.skel`88,351字节、`.atlas`3,016字节、PNG68,765字节，合计160,132字节。逐文件URL与SHA见新包`SOURCE-RECORD.json`。
- 本地已下载原版本`26-10-29-15-36-11-33649006`的`skinpack/char_1035_wisdel.ab`也含正确game#9 Prefab；该包SHA=`943a0b9fd845c6215767055d55b0b3065a7adbdac54aec8f830904be5d31a047`。原字段保留在`ORIGINAL-SKIN-PREFAB.json`。

## 身体和事件

原Spine3.8.99，21骨骼、17槽位。25个非零动作全部采样，保留1个零时长Default。512格/60fps，共3207帧、210页；PNG无损重编码前后逐页RGBA完全一致。源事件、时长、根点不变。相机和正常战斗仍需Root实际GPU验收，纯采样不等于游戏已验收。

| 动作 | 源时长 | 原OnAttack |
|---|---:|---:|
| Attack_A/B/C、Attack_Down_A/B/C | 2.099999905秒 | 0.600000024秒 |
| Skill_1 | 2.099999905秒 | 0.600000024秒 |
| Skill_2_Begin / End | 0.333333343 / 0.100000001秒 | 无 |
| Skill_2_Loop / Overload_Loop | 1.399999976秒 | 0.266666681秒 |
| Skill_3_Begin / End | 0.833333313 / 0.5秒 | 无 |
| Skill_3_Loop、Skill_Down_3_Loop | 5秒 | 0.533333361秒 |
| Die | 1秒 | 无 |

S3原OnAttackFinished为1.5秒，另有4.800000191秒的`skill_storage_w`事件。原包还有Idle、S2普通/过载待机和启停、S3普通/向下待机、Start与Stun。**没有Move动作**：运行中可移动位置时保留真实Idle，不以静态平移声称恢复了原行走。动画固定2×只由表现桥应用一次，以上源秒数不预先改速。

原Prefab FaceSwitcher缩放0.270000011，SkeletonDataAsset.scale=0.00999999978；原client骨骼比例约0.0027，字段如实保存。本棋盘与其余原生角色采用相同校准标尺0.0045世界单位/原骨骼单位，Idle可见高度约1.312，不再按费用或Size压缩。采样PPU由每动作固定视口计算，不能把它冒充原客户端PPU。

## 原特效边界

game#9 SkinHooker只把6条S3的buff_02_F、start、trail、hit、hit_02、hit_03替换成`*_game#9`，并把普通attack_a_start与attack_b_start替换成默认attack_c_start。S1/S2沿用默认原FX，不应套用sale#14素材。

这些专属FX已在本地`battle/prefabs/effects/wisdel.ab`，无需下载整库。原普通弹道`projectile_chr_wisdel_default`为AdvancedMovement，speed10、delayToStart0；game#9皮肤没有更换其弹道逻辑。实际FX包/运行事件由同轮独立接入负责，不能把取得源Prefab等同完整原客户端调度已经复现。

用户GIF、旧CardDefinition Sprite数组与6张表情裁片全部保留作为历史来源/回退。原资源仍属于明日方舟相关权利方，不登记CC0或开放再分发许可。

复现工具：`tools/art/capture_wisadel_april.cjs`、`optimize_original_capture.py`、`install_wisadel_april.py`。没有启动主工程Unity或提交；实际集成测试由Root统一记录。
