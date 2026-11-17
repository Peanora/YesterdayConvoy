# 完整棋盘模式设计基线 v0.7

用户于 **2026-11-17** 采纳本版本，要求写入项目并作为后续开发依据。完整游戏建立在最新棋盘模式及其最新地图上；本目录保存完整设计内容，具体实现状态与验证记录分别见 [STATUS](../STATUS.md) 和 [QA](../QA.md)。

## 阅读顺序与文件用途

1. [完整设计正文（Markdown）](FULL_GAME_v0.7.md)：原 Word 的全部正文、53 张表格和11个外部参照链接，便于代码检索和逐条实现。
2. [原 Word 文档](昨日圆车_完整数值设计初稿_v0.7.docx)：用户审阅并采纳的排版原件，原样保留。
3. [结构化参数快照](parameters.v0.7.json)：与该 Word 配套的 v0.7 参数原件，包含角色、羁绊、装备、预案、战略战备、节点、守军、奖励和触发规则；未经重新核对不改动快照。
4. [设计圣经](../GDD.md)、[棋盘战斗说明](../FORMATION_BATTLE.md)：查看与当前工程入口、地图和实现的连接。

后续用户明确修订优先；旧文档、旧试玩参数与本版冲突时，以本版为完整棋盘模式开发基线。v0.7 原稿中“候选”“模型”等表述原样保留，表示数值证据的状态，**不表示本版未获开发采纳，也不表示内容已在游戏中实现或经实战平衡验证**。旧模式的兼容保留不应把旧参数重新带入新流程。

## 内容范围

- 三个位面、23 节点（20 场战斗、3 次补给），完整收入、经验、商店、有限库存与升星规则。
- 16 种开局战略预案、9 个阵营和9个流派；开局三张随机一费卡与预案独立。
- 46 名角色的费用、职能、面板、技力、动作时间、技能及成员归属。
- 6 件基础装备、21 件进阶装备、12 种宝石合成配方，以及36项经济/战力战略战备。
- 整合希望、塔露拉二星升费/强化与商店同步改费、同频联动、接棒时刻、随机奖励球、遭遇档位、守军和首领。
- 已执行模型结果及其限制；没有把模型输出登记为实战通关率。

## 完整章节索引

- [本版改动](FULL_GAME_v0.7.md#section-01)
- [1  一局流程](FULL_GAME_v0.7.md#section-02)
- [2  战略预案经营方向](FULL_GAME_v0.7.md#section-03)
- [2续  战略预案构筑方向](FULL_GAME_v0.7.md#section-04)
- [2续  收入与升级](FULL_GAME_v0.7.md#section-05)
- [3  商店、星级和库存](FULL_GAME_v0.7.md#section-06)
- [4  战斗和通用数值](FULL_GAME_v0.7.md#section-07)
- [5  伤害、叠层和触发口径](FULL_GAME_v0.7.md#section-08)
- [6  整合运动：积累与兑现](FULL_GAME_v0.7.md#section-09)
- [7  塔露拉：升费、实时改价和终局](FULL_GAME_v0.7.md#section-10)
- [8  阵营羁绊：雪怪、泥岩、深池](FULL_GAME_v0.7.md#section-11)
- [9  阵营羁绊：萨卡兹、海嗣和固定关系](FULL_GAME_v0.7.md#section-12)
- [10  流派羁绊：连击、迅攻、复苏、爆破](FULL_GAME_v0.7.md#section-13)
- [11  流派羁绊 提词 便车 剧场](FULL_GAME_v0.7.md#section-14)
- [11续  接棒时刻与深池的分工](FULL_GAME_v0.7.md#section-15)
- [12  同频异响：角色之间的行动联动](FULL_GAME_v0.7.md#section-16)
- [13  角色职能与装备投放](FULL_GAME_v0.7.md#section-17)
- [14  1费角色 1/2](FULL_GAME_v0.7.md#section-18)
- [15  1费角色 2/2](FULL_GAME_v0.7.md#section-19)
- [16  2费角色 1/2](FULL_GAME_v0.7.md#section-20)
- [17  2费角色 2/2](FULL_GAME_v0.7.md#section-21)
- [18  3费角色 1/2](FULL_GAME_v0.7.md#section-22)
- [19  3费角色 2/2](FULL_GAME_v0.7.md#section-23)
- [20  4费角色 1/2](FULL_GAME_v0.7.md#section-24)
- [21  4费角色 2/2](FULL_GAME_v0.7.md#section-25)
- [22  5费角色 1/2](FULL_GAME_v0.7.md#section-26)
- [23  5费角色 2/2](FULL_GAME_v0.7.md#section-27)
- [24  基础装备与固定合成](FULL_GAME_v0.7.md#section-28)
- [25  进阶装备 1/3](FULL_GAME_v0.7.md#section-29)
- [26  进阶装备 2/3](FULL_GAME_v0.7.md#section-30)
- [27  进阶装备 3/3](FULL_GAME_v0.7.md#section-31)
- [28  奖励节点：白球、蓝球、金球、彩球](FULL_GAME_v0.7.md#section-32)
- [29  宝石转职、补给和物品](FULL_GAME_v0.7.md#section-33)
- [30  战略战备：经济类](FULL_GAME_v0.7.md#section-34)
- [31  战略战备：银档和金档战力](FULL_GAME_v0.7.md#section-35)
- [32  战略战备：彩档战力和出现范围](FULL_GAME_v0.7.md#section-36)
- [33  守军模板和难度曲线](FULL_GAME_v0.7.md#section-37)
- [34  三名首领与战斗词条](FULL_GAME_v0.7.md#section-38)
- [35  遭遇关：档位、25%奖励与结算](FULL_GAME_v0.7.md#section-39)
- [36  裂兽构筑：普通、半成型与极限](FULL_GAME_v0.7.md#section-40)
- [37  数值观察：已执行的候选模型](FULL_GAME_v0.7.md#section-41)
- [38  资源账、希望进度与后续调参](FULL_GAME_v0.7.md#section-42)
- [39  设计参照](FULL_GAME_v0.7.md#section-43)
