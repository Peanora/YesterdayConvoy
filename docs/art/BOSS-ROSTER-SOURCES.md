# 26 位 Boss 原作模型与动作来源

记录日期：2026-11-11。此登记由现有安装清单、原作获取记录、外部模型报告及逐包 `SOURCE-RECORD.json` 交叉生成。机器可读全文见 [boss-roster-sources.json](boss-roster-sources.json)。

本轮沿用 7 张已有 CardId，新增 19 张卡；技能附属物另计，不占招募卡名额。维维美保留“超新星”正面与背面，因此 26 位角色对应 27 个本体视图。全部本体均使用此次确认的原作资源；旧资源目录不据此认定被删除。

## 范围与计数

| 项目 | 数量 |
| --- | ---: |
| 招募角色 | 26（新增 19、沿用 7） |
| 本体视图／正时长动作 | 27／393 |
| 必要技能附属模型／正时长动作 | 8／40 |
| 合计视图／正时长动作 | 35／433 |
| 60 FPS 源采样帧 | 58,583 |
| 动作 PNG 图集页 | 2,722 |
| 零时长状态（仅元数据） | 54 |
| 原 PNG 总量（含各包 portrait） | 2,621,729,692 字节 |

计数仅包含 `Assets/Art/Characters/Monsters/BossRoster` 与对应 `Assets/Data/ArtCatalogs/BossRoster` 的身体动作包。原粒子、3D 装置、音频、Library 预处理缓存及构建包不计入这些数字；433 段不等于 433 个已在自然战斗中逐段验收的技能。

所有列出的原骨架版本为 Spine **3.8.99**，skin 名为 `default`；此 skin 字符串只表示骨架选用的 skin，不表示所有角色都是角色生涯中的同一种版本。以下原模型 ID 与选用说明共同固定身份。

## 26 位角色

| 编号 | 角色／选用版本 | 实际 CardId | 原模型 ID | 正时长动作 | 来源 |
| --- | --- | --- | --- | ---: | --- |
| B01 | W | `CARD_B01` | `enemy_1504_cqbw` | 6 | [PRTS](https://prts.wiki/w/W%28%E6%95%8C%E6%96%B9%29) |
| B02 | 梅菲斯特 | `CARD_M25` | `enemy_1507_mephi` | 4 | [PRTS](https://prts.wiki/w/%E6%A2%85%E8%8F%B2%E6%96%AF%E7%89%B9) |
| B03 | 弑君者 | `CARD_B03` | `enemy_1502_crowns` | 6 | [PRTS](https://prts.wiki/w/%E5%BC%91%E5%90%9B%E8%80%85%28%E6%95%8C%E6%96%B9%29) |
| B04 | 锏 | `CARD_B04` | `enemy_1525_blkswb` | 16 | [PRTS](https://prts.wiki/w/%E9%94%8F%28%E6%95%8C%E6%96%B9%29) |
| B05 | “大总统”汉科 | `CARD_B05` | `enemy_2139_shdbj2` | 9 | [PRTS](https://prts.wiki/w/%E2%80%9C%E5%A4%A7%E6%80%BB%E7%BB%9F%E2%80%9D%E6%B1%89%E7%A7%91) |
| B06 | 蔓德拉 | `CARD_M32` | `enemy_1523_mandra` | 22 | [PRTS](https://prts.wiki/w/%E8%94%93%E5%BE%B7%E6%8B%89) |
| B07 | 爱布拉娜／“领袖” | `CARD_B07` | `enemy_1536_ncrmcr` | 17 | [PRTS](https://prts.wiki/w/%E2%80%9C%E9%A2%86%E8%A2%96%E2%80%9D) |
| B08 | “万火归一” | `CARD_B08` | `enemy_1563_fthlgj` | 17 | [PRTS](https://prts.wiki/w/%E2%80%9C%E4%B8%87%E7%81%AB%E5%BD%92%E4%B8%80%E2%80%9D) |
| B09 | “酒神” | `CARD_B09` | `enemy_1568_dirctr` | 15 | [PRTS](https://prts.wiki/w/%E2%80%9C%E9%85%92%E7%A5%9E%E2%80%9D) |
| B10 | 坎诺特 | `CARD_B10` | `enemy_2036_syshop` | 3 | [PRTS](https://prts.wiki/w/%E5%9D%8E%E8%AF%BA%E7%89%B9) |
| B11 | 赫尔昏佐伦／巫王 | `CARD_B11` | `enemy_1548_ltniak` | 18 | [PRTS](https://prts.wiki/w/%E8%B5%AB%E5%B0%94%E6%98%8F%E4%BD%90%E4%BC%A6%EF%BC%8C%E2%80%9C%E5%B7%AB%E7%8E%8B%E2%80%9D) |
| B12 | “阿米娅”，炉芯终曲 | `CARD_B12` | `enemy_2092_skzamy` | 22 | [PRTS](https://prts.wiki/w/%E2%80%9C%E9%98%BF%E7%B1%B3%E5%A8%85%E2%80%9D%EF%BC%8C%E7%82%89%E8%8A%AF%E7%BB%88%E6%9B%B2) |
| B13 | “圣徒” | `CARD_B13` | `enemy_1567_pope` | 28 | [PRTS](https://prts.wiki/w/%E2%80%9C%E5%9C%A3%E5%BE%92%E2%80%9D) |
| B14 | 卡德霍，黑流之源 | `CARD_B14` | `enemy_2145_cadejo` | 9 | [PRTS](https://prts.wiki/w/%E5%8D%A1%E5%BE%B7%E9%9C%8D%EF%BC%8C%E9%BB%91%E6%B5%81%E4%B9%8B%E6%BA%90) |
| B15 | 最后的圣愚 | `CARD_B15` | `enemy_1603_hlystp` | 12 | [PRTS](https://prts.wiki/w/%E6%9C%80%E5%90%8E%E7%9A%84%E5%9C%A3%E6%84%9A) |
| B16 | 塔露拉／旧“不死的黑蛇”敌方模型（enemy_1515_bsnake） | `CARD_TALULAH_TRIAL` | `enemy_1515_bsnake` | 16 | [PRTS](https://prts.wiki/w/%E2%80%9C%E4%B8%8D%E6%AD%BB%E7%9A%84%E9%BB%91%E8%9B%87%E2%80%9D) |
| B17 | 爱国者 | `CARD_M20` | `enemy_1506_patrt` | 11 | [PRTS](https://prts.wiki/w/%E7%88%B1%E5%9B%BD%E8%80%85) |
| B18 | 霜星，“冬痕”（enemy_1510_frstar2） | `CARD_M14` | `enemy_1510_frstar2` | 7 | [PRTS](https://prts.wiki/w/%E9%9C%9C%E6%98%9F%EF%BC%8C%E2%80%9C%E5%86%AC%E7%97%95%E2%80%9D) |
| B19 | 泥岩 | `CARD_M28` | `enemy_1511_mdrock` | 7 | [PRTS](https://prts.wiki/w/%E6%B3%A5%E5%B2%A9%28%E6%95%8C%E6%96%B9%29) |
| B20 | 维什戴尔“超新星”（sale_14），正面与背面 | `CARD_WISADEL_APRIL` | `char_1035_wisdel_sale_14` | 25+17 | [PRTS](https://prts.wiki/w/%E7%BB%B4%E4%BB%80%E6%88%B4%E5%B0%94) |
| B21 | “岁”（界园版，enemy_2119_dyshhj） | `CARD_B21` | `enemy_2119_dyshhj` | 19 | [PRTS](https://prts.wiki/w/%E2%80%9C%E5%B2%81%E2%80%9D) |
| B22 | 狂躁异质裂兽 | `CARD_B22` | `enemy_1571_mirbst` | 9 | [PRTS](https://prts.wiki/w/%E7%8B%82%E8%BA%81%E5%BC%82%E8%B4%A8%E8%A3%82%E5%85%BD) |
| B23 | 安纳斯塔西奥，赎罪者 | `CARD_B23` | `enemy_1561_crzjdg` | 23 | [PRTS](https://prts.wiki/w/%E5%AE%89%E7%BA%B3%E6%96%AF%E5%A1%94%E8%A5%BF%E5%A5%A5%EF%BC%8C%E8%B5%8E%E7%BD%AA%E8%80%85) |
| B24 | 不死的黑蛇（2026版，enemy_1602_blksnk） | `CARD_B24` | `enemy_1602_blksnk` | 12 | [PRTS](https://prts.wiki/w/%E4%B8%8D%E6%AD%BB%E7%9A%84%E9%BB%91%E8%9B%87) |
| B25 | 圣徒瓦列里奥 | `CARD_B25` | `enemy_1594_presaint` | 31 | [PRTS](https://prts.wiki/w/%E5%9C%A3%E5%BE%92%E7%93%A6%E5%88%97%E9%87%8C%E5%A5%A5) |
| B26 | 易 | `CARD_B26` | `enemy_2118_dylbhm` | 12 | [PRTS](https://prts.wiki/w/%E6%98%93) |

B16 为 `enemy_1515_bsnake`，B24 为 `enemy_1602_blksnk`，两者独立保留。B18 已固定为“冬痕”`enemy_1510_frstar2`，不是旧普通霜星 `enemy_1505_frstar`。B20 使用 `char_1035_wisdel_sale_14` 正背模型；原构筑 CardId 保持 `CARD_WISADEL_APRIL`。

B21 的真实 Player 截图 `Library/BossRosterEvidence/player/B21-default-source.png` 显示原体双翼超出固定战场相机上沿。因此游戏呈现采用 **0.42 倍统一缩放**，由 `BossRosterArt.VisualScale` 提供；其余角色为 1 倍。`Height` 仍是原源高度，`VisualHeight = Height × VisualScale` 用于接地／显示高度。此项为本作固定镜头的显示适配，原 PNG、PPU、pivot、动作时长与像素哈希均保持；不是重绘或重新采样。实际共享视图接线、缩放后画面与重建结果由本轮正式 QA 记录。

## 8 类必要技能附属模型

| 编号 | 用途／版本 | 原模型 ID | 正时长动作／帧／页 | 来源 |
| --- | --- | --- | --- | --- |
| P12I | B12 仅剩的创意；独立原身体 | `enemy_2094_skzamb` | 4／491／22 | [PRTS](https://prts.wiki/w/仅剩的创意/spine) |
| P14 | B14 痛楚化身；保留独立原身体 | `enemy_2146_cadesd` | 8／828／37 | [PRTS](https://prts.wiki/w/%E7%97%9B%E6%A5%9A%E5%8C%96%E8%BA%AB) |
| P14H | B14 猎犬proto；Skill_Loop为原透明等待，附着由独立原FX呈现 | `enemy_2137_shsdgo` | 7／900／40 | [PRTS](https://prts.wiki/w/猎犬proto/spine) |
| P15C | B15 时间啊，向前！OB19版惘核；不是第十六章同名enemy_10128_fearpj | `enemy_10262_dsjred` | 4／201／10 | [PRTS](https://prts.wiki/w/“惘核”(OB19)/spine) |
| P20 | B20 超新星对应魂灵sale#14；不是维维美本体复制 | `token_10035_wisdel_wward_sale#14` | 4／366／16 | [官方资源包](https://ak.hycdn.cn/assetbundle/official/Android/assets/26-10-29-15-36-11-33649006/skinpack_token_10035_wisdel_wward.dat) |
| P24D | B24 殆因；独立原身体 | `enemy_10263_dsjblk` | 4／201／10 | [PRTS](https://prts.wiki/w/“殆因”/spine) |
| P25M | B25 塑能基质；独立原身体 | `enemy_10212_klhz` | 5／634／28 | [PRTS](https://prts.wiki/w/塑能基质/spine) |
| P26 | B26 梁；独立原身体 | `enemy_2122_dybgzd` | 4／265／13 | [PRTS](https://prts.wiki/w/%E6%A2%81) |

P20 从官方 `skinpack_token_10035_wisdel_wward` 提取 **sale#14** 原骨架与纹理；原文件 SHA 保留。采样时仅在 GPU 上传路径对解码贴图做预乘以正确处理透明 bleed，未修改磁盘原 PNG；该差异与材质字段记在对应 `SOURCE-RECORD.json` / `CAPTURE-QA.json`。规范文件名的 atlas 仅统一纹理页名，原带 `#14` 文件仍保留为来源证据。

这里的 8 类是 Spine 身体附属库。摄影机、调谐节点、旧日之火及昔日道标等原 3D／纯 FX 本体由独立特效目录登记，不以主 Boss 身体代替，也不挤入此 8 类或 26 张卡计数。

## 源身份与完整性

每个视图的 `SOURCE-RECORD.json` 记录来源页、原 skel／atlas／PNG 的 URL 或官方包 URL、原文件 SHA-256、源图集清单 SHA、逐页 PNG SHA 与权利状态。本登记 JSON 逐视图列出这些原文件摘要，并指回工程内来源记录；B20 正背文件分别登记。绝对本机加工路径已在此 JSON 规范为相对于外部预览根目录的路径，运行不依赖该外部目录。

本登记写入前实际读取并复核了 **105 份原文件 SHA-256**、35 份来源记录，且逐视图计数与 `installation.json` / `PACKAGE-QA.json` 一致。采样按原片长保留 60 FPS 帧及终点帧；打包保留采样图集的 PNG 原字节、时长、原事件负载、pivot 与页序，原骨架和原贴图另存于外部来源目录。长动作仅按页界拆成不超过 600 帧的缓存段；完整源动作名称和片长继续可读，不将全库默认常驻加载。

B08 `B_Skill_3_Loop`、B21 `Idle` 及猎犬 P14H `Skill_Loop` 为原透明等待；零时长状态只留元数据。坎诺特原源无 Attack／Die，部分固定单位无 Move；缺动作不以伪造姿势补齐。默认可见别名、动作锁与阶段选择属于游戏接线，不能从源文件存在直接推定全部技能调度已还原。

## 如何阅读验证状态（README）

`manifest.json` / `installation.json` 中的 `IMPORTED_NOT_BOUND`，以及 `PACKAGE-QA.json` / `SOURCE-RECORD.json` 中的 `NOT_RUN` 或 `NOT_RUN_BY_PACKAGER`，是**打包时快照**。它们表示打包器不运行 Unity，不是后续正式接入验证的最新结论。当前绑定、机制、场景、PlayMode、Windows Player 与人工复核范围以 [docs/QA.md](../QA.md) 和 [docs/STATUS.md](../STATUS.md) 的本轮记录为准。

打包 QA 已检查源 PNG 哈希、时长、事件、pivot、帧数与页序；外部 WebGL GPU 采样检查可见帧和裁边。它们不能替代 Unity 的真实卡牌拖部署、伤害／SP、独立实体、阶段、暂停或重开验收。B22 已有真实 Unity 代表相机、实际扣血与暂停证据；这不外推为全部角色 Player 通过。

本登记只读到的正式 Editor 快照为 `Library/BossRosterEvidence/full-editmode.xml`：**1088／1088 Passed，0 Failed**，结束时间 `2026-11-11 14:07:12Z`。此数字是完整 Editor 套件结果，不是逐段动作 GPU、两组场景 PlayMode或独立 Player 的结论。Library 日志是本地证据，不是需要提交的运行依赖；最终通过范围和后续重跑由本轮 QA 统一记录。此文档编写者未运行 Unity。

## 权利状态

主导交付补充：运行源码 `22b6df5a` 的最终独立 D3D11 Player 已通过 26 位／28 场、453 项检查；B21 等比缩放后身体贡献 46,475 相机像素、边缘裁切 0，最终 PNG 已目视核对。源资源及打包快照保持不变，完整结果见 QA；不据此宣称 433 段动作均已在自然战斗中逐段验收。

角色造型、纹理、骨架、动作和原作特效属于鹰角网络及相关原权利人。**未获得或确认这些原素材的再分发许可**；它们不因非商业用途、可下载、PRTS 展示或用户提供而变成 CC0／开放素材。PRTS 是资料与资源镜像来源，页面可访问不代表权利人授权。

用户要求将已接入运行素材随源码提交、推送，这项指示仅说明项目交付范围，**不等于原权利人的授权**。Spine 预览运行库另有自身许可证；外部预览保留 `SPINE-LICENSE.txt`，本登记不授予该运行库或原作资源的权利。
