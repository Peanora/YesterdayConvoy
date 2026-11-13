# 怪物技能自动触发：原作时机合同

本轮把已有技能的释放入口改为自动。本表登记源字段与适配，不把生成器检查当作实机证据；正式测试和独立Player结果见QA最新条目。

数据以本次选定26位的PRTS原文、`original.sourceSkillRows`和精确native Actor为准，避开旧试玩技能的8SP/半满初始化。原两体B15/B24取2026活动第二形态，不混同章节旧黑蛇。

## 运行合同

游戏控制器在开战与逐帧统一调用 `EnableAutomaticSkills()`，各模式没有玩家可切换的手动技能开关。独立模型复现应同样启用此调度；保留的低层手动事务夹具用于隔离单项机制，不代表普通玩法。

- 保留 `Id/Initial/Interval/Range/Priority/Blocked` 与二参 `For`；新增自然/攻击SP、动作期间停充、条件、每区域次数上限与来源状态。
- `AttackSP>0` 时 Initial/Interval=0，仅每轮真实普通攻击记1次，不按命中目标数、弹道数或多段伤害记数。
- `SPCost>0 && AttackSP==0` 为自然SP。Initial/Interval只是基准1SP/s的等候参考，需实际保存初始SP、动态回复与阻回。
- `MaximumCasts=1` 按原模式/区域独立计数。B12原首两区域结束清SP，区域切换须刷新区域合同。
- `StartsAfterAction`仅标明确SP停充，不把native `_resetMainAbilityCdWhenCastEnd`（普攻CD）误作技能CD从动作结束起计。
- `Condition`决定可施放事实；未满足时保留就绪状态，不支出技力或重置CD。范围0不表示无目标即可施放，仍须条件与已有技能检查。

|角色|本作阶段|技能ID|首次/周期参考秒|SP初始/消耗|攻击回复|条件/半径|每模式上限|来源状态|
|---|---|---|---|---|---|---|---|---|
|B01|1|c4|9 / 21|0 / 0|0|Standard / 2.5|-1|Exact|
|B03|1|blink|15 / 15|0 / 0|0|Blocked; Standard|-1|Exact|
|B04|1|dash|0 / 30|0 / 0|0|Blocked; Standard|-1|Exact|
|B04|1|blizzard|15 / 15|0 / 0|0|GlobalTarget / 1.5|-1|Exact|
|B04|2|dash|0 / 30|0 / 0|0|Blocked; Standard|-1|Exact|
|B04|2|blizzard|15 / 15|0 / 0|0|GlobalTarget / 1.5|-1|Exact|
|B05|1|blink|2 / 15|0 / 0|0|Blocked; Standard|-1|Exact|
|B05|1|flare|0 / 15|0 / 0|0|InAttackRange|-1|Exact|
|B07|1|sacrifice|5 / 70|0 / 0|0|FlamechaserAlly|-1|Exact|
|B07|1|dominate|5 / 30|0 / 0|0|GlobalTarget / 2|-1|Exact|
|B07|2|sacrifice|5 / 60|0 / 0|0|FlamechaserAlly|-1|Exact|
|B07|2|dominate|0 / 30|0 / 0|0|GlobalTarget / 2|-1|Exact|
|B08|1|grantFire|0 / 0|0 / 10|10|InAttackRange|-1|Exact|
|B08|1|blind|30 / 55|0 / 0|0|GlobalTarget|-1|Exact|
|B08|3|grantFire|0 / 0|0 / 5|5|InAttackRange|-1|Exact|
|B08|3|blind|40 / 55|0 / 0|0|GlobalTarget|-1|Exact|
|B08|3|flameDance|30 / 70|0 / 0|0|BurningTarget|-1|Exact|
|B09|1|absurdity|15 / 35|0 / 0|0|ActiveCamera|-1|Exact|
|B09|2|absurdity|17 / 35|0 / 0|0|ActiveCamera|-1|Exact|
|B09|2|intermission|10 / 32|0 / 0|0|CameraTarget|-1|Exact|
|B10|1|potion|23 / 23|0 / 23|0|Standard|-1|Exact|
|B11|1|sluggish|10 / 45|0 / 0|0|GlobalTarget / 2.2|-1|Exact|
|B11|2|sluggish|10 / 45|0 / 0|0|GlobalTarget|-1|Exact|
|B11|2|edict|10 / 50|0 / 0|0|Standard|-1|Exact|
|B12|1|survival|180 / 180|0 / 180|0|Standard；停充|1|Exact|
|B12|2|survival|280 / 280|0 / 280|0|Standard；停充|1|Exact|
|B12|2|blackCrown|0 / 0|0 / 0|0|BlackCrownCharge|-1|Exact|
|B12|2|idea|15 / 15|0 / 0|0|Standard|-1|Exact|
|B13|2|rite|13 / 20|0 / 0|0|Standard|-1|Exact|
|B13|3|rite|13 / 20|0 / 0|0|Standard|-1|Exact|
|B14|1|traverse|20 / 40|0 / 0|0|Standard|-1|Exact|
|B14|1|roar|20 / 20|0 / 0|0|Standard|-1|Exact|
|B15|1|sow|15 / 30|0 / 0|0|Standard|-1|Exact|
|B15|2|sow|15 / 30|0 / 0|0|Standard|-1|Adapted|
|B21|1|insomnia|30 / 50|20 / 50|0|AwayModeTarget；停充|-1|Exact|
|B21|2|insomnia|30 / 50|20 / 50|0|AwayModeTarget；停充|-1|Exact|
|B22|2|rock|10 / 20|0 / 0|0|GlobalTarget|-1|Exact|
|B23|1|gospel|10 / 20|0 / 0|0|InAttackRange|-1|Exact|
|B23|1|seek|5 / 10|0 / 0|0|CannonAvailable|-1|Exact|
|B23|2|gospel|10 / 20|0 / 0|0|InAttackRange|-1|Exact|
|B24|1|incite|15 / 30|0 / 0|0|Standard|-1|Exact|
|B24|2|incite|15 / 30|0 / 0|0|Standard|-1|Exact|
|B25|1|judgement|0 / 0|0 / 5|5|InAttackRange|-1|Exact|
|B25|1|retribution|60 / 60|0 / 0|0|InAttackRange|-1|Exact|
|B25|2|judgement|0 / 0|0 / 4|4|InAttackRange|-1|Exact|
|B25|2|retribution|50 / 50|0 / 0|0|InAttackRange|-1|Exact|
|B26|1|balance|20 / 20|0 / 0|0|GlobalTarget|-1|Exact|
|B26|1|beams|25 / 25|0 / 0|0|Standard|-1|Exact|
|B26|2|balance|20 / 20|0 / 0|0|GlobalTarget|-1|Exact|
|B26|2|beams|25 / 25|0 / 0|0|Standard|-1|Exact|

## 非周期与已有自动所有权

- B01 W：仅本表周期合同；未额外添加阈值或死亡定时器。
- B02 梅菲斯特：heal=普攻治疗，沿既有自动普攻。
- B03 弑君者：仅本表周期合同；未额外添加阈值或死亡定时器。
- B04 锏：killWindow=每损失25%HP阈值，沿已自动阈值；不变成CD循环。
- B05 “大总统”汉科：仅本表周期合同；未额外添加阈值或死亡定时器。
- B06 蔓德拉：gaze=源初5/CD15；collapse=二形初20/CD15。旧Core机制负责，For不重复。
- B07 爱布拉娜／“领袖”：仅本表周期合同；未额外添加阈值或死亡定时器。
- B08 “万火归一”：慢步/回血/形态选择是死亡与场上旧火状态；phase2无周期技能。
- B09 “酒神”：意兴盎然=初始0、每3次攻击回复的强化下一普攻，保留普攻所有权，当前无独立runtime技能ID。
- B10 坎诺特：旧10s周期被动必须关闭，避免与23SP自动喷洒重复。
- B11 赫尔昏佐伦／巫王：folly=每损失50%HP和复起起始强制触发；annihilation=每形态300s一次超时/随后退场，不入周期For；二形进入时强制edict与周期10/50需唯一所有权。源另有一形闪现5/30，当前未有独立runtime技能ID，本轮不新增机制。
- B12 “阿米娅”，炉芯终曲：blackCrown=二形计数>0且范围内有目标，初始计数1，创意到达+1；AOEAttack native为EnemySkill而非普攻。survival每区域/模式最多1次、首次两区结束清SP；eternal=最终复起后强制触发。思维混乱/终结实相减费等原遗物条件未移植，使用基准180/280，不能称包含全部原关卡修正。
- B13 “圣徒”：sacrament=真实被击倒与外波结束触发，不是周期。地→飞、飞→地各最多2次；原至少1.167/.967s且等待后两波结束，再飞行登场1.65/地面登场1.58s。无原外波时仅采用原最短等待+登场片长是明确适配，回满HP，不能无限半血回血。多重普攻沿现有自动攻击。
- B14 卡德霍，黑流之源：echoes/猎犬→痛楚波=死亡与对象条件，不周期。原450s湮灭为超时退场、当前无独立runtime技能ID，本轮不新增机制。痛楚与猎犬自身技能沿既有独立自动对象时钟。
- B15 最后的圣愚：obituary=0%/第二形态HP阈值，domainBurst=搭档倒下联动；以TL-10或TL-S-4原关卡条件为准。二形讹谬有blkhlySharedSkill身份但副页无独立CD表，沿第一形态15/30作最小适配，SourceStatus.Adapted；未取得二形独立时点。
- B16 塔露拉：ignite19/19、detonate35/35、二形energy75/50已有Core自动；原两次阶段火雨已自动，手动六波演示雨不再额外自动。
- B17 爱国者：投枪=毁灭姿态初15/CD20，沿既有阶段调度。生命献祭/献祭表9999不是9999s周期，来自重生阶段强制链，不能照填For。
- B18 霜星：冰环10.5/10.5、黑冰35/35与二形寒潮0，旧Core负责；不重复额外FrostSeal试玩施法。
- B19 泥岩：屏障刷新17/17既有自动；占据35/35且存在非己方留声机，旧Core负责，不首帧满35SP。
- B20 维维美：专3：S1攻击回复0/2；S2自动15/25，持续25s期间停充；S3自动40/50，首10s、六发弹药及末发动作期间停充。魂灵0/5自动独立时钟。旧Core负责，For不重复。
- B21 “岁”（界园版）：mountains=进入停留模式3s后的复合跟随，非9999s周期；awaken=损失50%HP强制链；起飞=20s停留/10s阈值冷却结束链。insomnia原最小CD5同时消耗50SP、初始20；通常首30s，停留模式阻回；≥6角色类目标时+1.5SP/s，不能永久固定30/50秒。原终不寐600s一次退场当前无独立runtime技能ID，不虚构到普通不寐周期。
- B22 狂躁异质裂兽：rage/flee=已自动被动与阶段，不定时手动施放；rock仅二形。
- B23 安纳斯塔西奥，赎罪者：pierce=二形每损失33.5%HP且被阻挡，源CD5；属于阈值/阻挡条件，不定时每5s冲刺。
- B24 不死的黑蛇（2026版）：deathKnock=致命/二形HP阈值，deployMarked=真实待部署区再部署交互，不是周期主动。二形incite由blkhlySharedSkill延续15/30。
- B25 圣徒瓦列里奥：penalty=附近目标死亡/撤退事件，absorb=真实塑能基质接近与0.1s尝试间隔，均不是周期施法。judgement按实际普通攻击回SP5/4，不按每弹/每目标计。
- B26 易：仅本表周期合同；未额外添加阈值或死亡定时器。

## 原始26位技能时点摘要

以下为来源字段，斜杠/空白保留其原义，9999不自动解释为普通周期。该表不把通用8SP写回原作数字。

|角色|原技能|原首次CD|原CD|原SP消耗|来源|
|---|---|---|---|---|---|
|B01|C4|9|21||[PRTS](https://prts.wiki/w/W%28%E6%95%8C%E6%96%B9%29)|
|B03|闪现|15|15||[PRTS](https://prts.wiki/w/%E5%BC%91%E5%90%9B%E8%80%85%28%E6%95%8C%E6%96%B9%29)|
|B04|速杀|0|30||[PRTS](https://prts.wiki/w/%E9%94%8F%28%E6%95%8C%E6%96%B9%29)|
|B04|肆虐风雪|15|15||[PRTS](https://prts.wiki/w/%E9%94%8F%28%E6%95%8C%E6%96%B9%29)|
|B05|闪现|2|15||[PRTS](https://prts.wiki/w/%E2%80%9C%E5%A4%A7%E6%80%BB%E7%BB%9F%E2%80%9D%E6%B1%89%E7%A7%91)|
|B05|发射照明弹|0|15||[PRTS](https://prts.wiki/w/%E2%80%9C%E5%A4%A7%E6%80%BB%E7%BB%9F%E2%80%9D%E6%B1%89%E7%A7%91)|
|B06|蔓德拉的注目|5|15||[PRTS](https://prts.wiki/w/%E8%94%93%E5%BE%B7%E6%8B%89)|
|B06|坍塌 [第二形态]|20|15||[PRTS](https://prts.wiki/w/%E8%94%93%E5%BE%B7%E6%8B%89)|
|B07|予取予夺|5/5|70/60||[PRTS](https://prts.wiki/w/%E2%80%9C%E9%A2%86%E8%A2%96%E2%80%9D)|
|B07|主宰|5/0|30/30||[PRTS](https://prts.wiki/w/%E2%80%9C%E9%A2%86%E8%A2%96%E2%80%9D)|
|B08|赐火 [旋转] [最后一舞]|/|/|10/5|[PRTS](https://prts.wiki/w/%E2%80%9C%E4%B8%87%E7%81%AB%E5%BD%92%E4%B8%80%E2%80%9D)|
|B08|焚光 [旋转] [最后一舞]|30/40|55||[PRTS](https://prts.wiki/w/%E2%80%9C%E4%B8%87%E7%81%AB%E5%BD%92%E4%B8%80%E2%80%9D)|
|B08|炽焰燃烧 [最后一舞]|30|70||[PRTS](https://prts.wiki/w/%E2%80%9C%E4%B8%87%E7%81%AB%E5%BD%92%E4%B8%80%E2%80%9D)|
|B09|意兴盎然 [第一形态] [第二形态]|/|/|3|[PRTS](https://prts.wiki/w/%E2%80%9C%E9%85%92%E7%A5%9E%E2%80%9D)|
|B09|渗散荒诞 [第一形态] [第二形态]|15/17|35/35||[PRTS](https://prts.wiki/w/%E2%80%9C%E9%85%92%E7%A5%9E%E2%80%9D)|
|B09|幕间不息 [第二形态]|10|32||[PRTS](https://prts.wiki/w/%E2%80%9C%E9%85%92%E7%A5%9E%E2%80%9D)|
|B10|药剂喷洒|/|/|23|[PRTS](https://prts.wiki/w/%E5%9D%8E%E8%AF%BA%E7%89%B9)|
|B11|造物灭亡|300|300||[PRTS](https://prts.wiki/w/%E8%B5%AB%E5%B0%94%E6%98%8F%E4%BD%90%E4%BC%A6%EF%BC%8C%E2%80%9C%E5%B7%AB%E7%8E%8B%E2%80%9D)|
|B11|闪现 [第一形态]|5|30||[PRTS](https://prts.wiki/w/%E8%B5%AB%E5%B0%94%E6%98%8F%E4%BD%90%E4%BC%A6%EF%BC%8C%E2%80%9C%E5%B7%AB%E7%8E%8B%E2%80%9D)|
|B11|慵钝惩戒 [第一形态] [第二形态]|10|45||[PRTS](https://prts.wiki/w/%E8%B5%AB%E5%B0%94%E6%98%8F%E4%BD%90%E4%BC%A6%EF%BC%8C%E2%80%9C%E5%B7%AB%E7%8E%8B%E2%80%9D)|
|B11|痴愚惩戒|/|/||[PRTS](https://prts.wiki/w/%E8%B5%AB%E5%B0%94%E6%98%8F%E4%BD%90%E4%BC%A6%EF%BC%8C%E2%80%9C%E5%B7%AB%E7%8E%8B%E2%80%9D)|
|B11|君王诏制 [第二形态]|10|50||[PRTS](https://prts.wiki/w/%E8%B5%AB%E5%B0%94%E6%98%8F%E4%BD%90%E4%BC%A6%EF%BC%8C%E2%80%9C%E5%B7%AB%E7%8E%8B%E2%80%9D)|
|B12|存续先兆|/|/|180 / 180 / 180 / 280|[PRTS](https://prts.wiki/w/%E2%80%9C%E9%98%BF%E7%B1%B3%E5%A8%85%E2%80%9D%EF%BC%8C%E7%82%89%E8%8A%AF%E7%BB%88%E6%9B%B2)|
|B12|黑冠余威 [第二形态]|/|/||[PRTS](https://prts.wiki/w/%E2%80%9C%E9%98%BF%E7%B1%B3%E5%A8%85%E2%80%9D%EF%BC%8C%E7%82%89%E8%8A%AF%E7%BB%88%E6%9B%B2)|
|B12|无言的期盼 [第二形态]|15|15||[PRTS](https://prts.wiki/w/%E2%80%9C%E9%98%BF%E7%B1%B3%E5%A8%85%E2%80%9D%EF%BC%8C%E7%82%89%E8%8A%AF%E7%BB%88%E6%9B%B2)|
|B13|启圣仪式 [第二形态·地面] [第二形态·飞行]|13|20||[PRTS](https://prts.wiki/w/%E2%80%9C%E5%9C%A3%E5%BE%92%E2%80%9D)|
|B13|多重普通攻击 [第二形态·地面] [第二形态·飞行]|0|0||[PRTS](https://prts.wiki/w/%E2%80%9C%E5%9C%A3%E5%BE%92%E2%80%9D)|
|B14|痛吼 [初始形态]|20|20||[PRTS](https://prts.wiki/w/%E5%8D%A1%E5%BE%B7%E9%9C%8D%EF%BC%8C%E9%BB%91%E6%B5%81%E4%B9%8B%E6%BA%90)|
|B14|行于荒原 [初始形态]|20|40||[PRTS](https://prts.wiki/w/%E5%8D%A1%E5%BE%B7%E9%9C%8D%EF%BC%8C%E9%BB%91%E6%B5%81%E4%B9%8B%E6%BA%90)|
|B14|湮灭 [初始形态]|450|450||[PRTS](https://prts.wiki/w/%E5%8D%A1%E5%BE%B7%E9%9C%8D%EF%BC%8C%E9%BB%91%E6%B5%81%E4%B9%8B%E6%BA%90)|
|B14|感受痛楚 [休整形态]|/|/||[PRTS](https://prts.wiki/w/%E5%8D%A1%E5%BE%B7%E9%9C%8D%EF%BC%8C%E9%BB%91%E6%B5%81%E4%B9%8B%E6%BA%90)|
|B15|【讹谬的播撒】|15|30||[PRTS](https://prts.wiki/w/%E6%9C%80%E5%90%8E%E7%9A%84%E5%9C%A3%E6%84%9A)|
|B15|【丧亡的赐告】|0|3||[PRTS](https://prts.wiki/w/%E6%9C%80%E5%90%8E%E7%9A%84%E5%9C%A3%E6%84%9A)|
|B16|点燃|19.0|19.0||[PRTS](https://prts.wiki/w/%E2%80%9C%E4%B8%8D%E6%AD%BB%E7%9A%84%E9%BB%91%E8%9B%87%E2%80%9D)|
|B16|引爆|35.0|35.0||[PRTS](https://prts.wiki/w/%E2%80%9C%E4%B8%8D%E6%AD%BB%E7%9A%84%E9%BB%91%E8%9B%87%E2%80%9D)|
|B16|召唤能量聚合体 [狂暴状态]|75|50||[PRTS](https://prts.wiki/w/%E2%80%9C%E4%B8%8D%E6%AD%BB%E7%9A%84%E9%BB%91%E8%9B%87%E2%80%9D)|
|B17|投枪 [毁灭姿态]|15|20||[PRTS](https://prts.wiki/w/%E7%88%B1%E5%9B%BD%E8%80%85)|
|B17|生命献祭|9999|9999||[PRTS](https://prts.wiki/w/%E7%88%B1%E5%9B%BD%E8%80%85)|
|B17|献祭|9999|9999||[PRTS](https://prts.wiki/w/%E7%88%B1%E5%9B%BD%E8%80%85)|
|B18|冰环|10.5|10.5||[PRTS](https://prts.wiki/w/%E9%9C%9C%E6%98%9F%EF%BC%8C%E2%80%9C%E5%86%AC%E7%97%95%E2%80%9D)|
|B18|冰环[重生]|10.5|10.5||[PRTS](https://prts.wiki/w/%E9%9C%9C%E6%98%9F%EF%BC%8C%E2%80%9C%E5%86%AC%E7%97%95%E2%80%9D)|
|B18|冻结|35|35||[PRTS](https://prts.wiki/w/%E9%9C%9C%E6%98%9F%EF%BC%8C%E2%80%9C%E5%86%AC%E7%97%95%E2%80%9D)|
|B18|冻结[重生]|35|35||[PRTS](https://prts.wiki/w/%E9%9C%9C%E6%98%9F%EF%BC%8C%E2%80%9C%E5%86%AC%E7%97%95%E2%80%9D)|
|B18|召唤寒潮[重生]|0|0||[PRTS](https://prts.wiki/w/%E9%9C%9C%E6%98%9F%EF%BC%8C%E2%80%9C%E5%86%AC%E7%97%95%E2%80%9D)|
|B19|刷新屏障|17|17||[PRTS](https://prts.wiki/w/%E6%B3%A5%E5%B2%A9%28%E6%95%8C%E6%96%B9%29)|
|B19|占据|35|35||[PRTS](https://prts.wiki/w/%E6%B3%A5%E5%B2%A9%28%E6%95%8C%E6%96%B9%29)|
|B20|定点清算|0||2|[PRTS](https://prts.wiki/w/%E7%BB%B4%E4%BB%80%E6%88%B4%E5%B0%94)|
|B20|饱和复仇|15||25|[PRTS](https://prts.wiki/w/%E7%BB%B4%E4%BB%80%E6%88%B4%E5%B0%94)|
|B20|爆裂黎明|40||50|[PRTS](https://prts.wiki/w/%E7%BB%B4%E4%BB%80%E6%88%B4%E5%B0%94)|
|B21|撼山河 [停留模式]|9999|9999|/|[PRTS](https://prts.wiki/w/%E2%80%9C%E5%B2%81%E2%80%9D)|
|B21|不寐|5|5|50|[PRTS](https://prts.wiki/w/%E2%80%9C%E5%B2%81%E2%80%9D)|
|B21|不寐 (全场撤退)|9999|9999|0|[PRTS](https://prts.wiki/w/%E2%80%9C%E5%B2%81%E2%80%9D)|
|B21|终不寐|600|0||[PRTS](https://prts.wiki/w/%E2%80%9C%E5%B2%81%E2%80%9D)|
|B21|起飞|/|/|/|[PRTS](https://prts.wiki/w/%E2%80%9C%E5%B2%81%E2%80%9D)|
|B22|落石 [第二形态]|10|20||[PRTS](https://prts.wiki/w/%E7%8B%82%E8%BA%81%E5%BC%82%E8%B4%A8%E8%A3%82%E5%85%BD)|
|B23|福音泼洒|10|20||[PRTS](https://prts.wiki/w/%E5%AE%89%E7%BA%B3%E6%96%AF%E5%A1%94%E8%A5%BF%E5%A5%A5%EF%BC%8C%E8%B5%8E%E7%BD%AA%E8%80%85)|
|B23|寻觅罪孽 [第一形态]|5|10||[PRTS](https://prts.wiki/w/%E5%AE%89%E7%BA%B3%E6%96%AF%E5%A1%94%E8%A5%BF%E5%A5%A5%EF%BC%8C%E8%B5%8E%E7%BD%AA%E8%80%85)|
|B23|穿心救赎 [第二形态]|0|5||[PRTS](https://prts.wiki/w/%E5%AE%89%E7%BA%B3%E6%96%AF%E5%A1%94%E8%A5%BF%E5%A5%A5%EF%BC%8C%E8%B5%8E%E7%BD%AA%E8%80%85)|
|B24|【怨怒的煽动】|15|30||[PRTS](https://prts.wiki/w/%E4%B8%8D%E6%AD%BB%E7%9A%84%E9%BB%91%E8%9B%87)|
|B24|【死意的直叩】|0|3||[PRTS](https://prts.wiki/w/%E4%B8%8D%E6%AD%BB%E7%9A%84%E9%BB%91%E8%9B%87)|
|B25|审判 [第一形态]|0|0|5|[PRTS](https://prts.wiki/w/%E5%9C%A3%E5%BE%92%E7%93%A6%E5%88%97%E9%87%8C%E5%A5%A5)|
|B25|报偿 [第一形态]|60|60||[PRTS](https://prts.wiki/w/%E5%9C%A3%E5%BE%92%E7%93%A6%E5%88%97%E9%87%8C%E5%A5%A5)|
|B25|审判 [第二形态]|0|0|4|[PRTS](https://prts.wiki/w/%E5%9C%A3%E5%BE%92%E7%93%A6%E5%88%97%E9%87%8C%E5%A5%A5)|
|B25|报偿 [第二形态]|50|50||[PRTS](https://prts.wiki/w/%E5%9C%A3%E5%BE%92%E7%93%A6%E5%88%97%E9%87%8C%E5%A5%A5)|
|B26|相合|20|20||[PRTS](https://prts.wiki/w/%E6%98%93)|
|B26|承梁|25|25||[PRTS](https://prts.wiki/w/%E6%98%93)|

## 来源边界与适配

- `SourceStatus.Exact`表示所填基准时点/计费方式有本次原文或native证据，不表示本作伤害、地图、遗物、外部波次与完整Torappu客户端完全复刻。
- B21原最低5秒冷却与50SP双门：初始20SP通常30秒就绪；停留阻回20秒，至少6个角色类目标时原SP回复额外+1.5。实际适配必须保存这些事实，不能硬编固定50秒轮询替代SP。
- B11二形入场强制诏制、B13有限死亡返回和B12计数黑冠均由相应真实状态入口负责。原B11每形态300秒灭亡仅登记来源，未引入本作的倒计时判负或一个额外周期清场。
- B13启圣仪式的原CD即使不在对应模式也不会暂停，地/飞返回不要重新给13秒初始CD。
- B13本作缺原外波时采用原最短等待加登场动作，并保留地/飞各2次上限，是明确时序适配；不把来源空白填成任意周期。
- B11原一形闪现、B14原450秒湮灭与B21原600秒终不寐当前没有独立runtime技能ID；本轮没有扩这些技能机制，也没有用同名试玩主动冒称已补。
- 旧7核心按现有专属director/状态机自动化，未加入新19的For避免双播、双结算。
- 固定守军不会像原作玩家临时向Boss近处部署。技能就绪而目标还停在较远普攻边界时，本作沿已有路线靠近技能范围；阻挡类仅向仍有阻挡容量的地面干员靠近。目标合法性和原条件仍须满足，没有增加动态寻路。

来源：项目外 `E:\珍贵资料\昨日圆车_美术预览\2026-11-11_26位Boss原作整备-v1\skills\ROSTER-SKILL-MAP.json`，SHA256 `8f9574e6de77edb61e909b0c4109b4c5f1fbed57d2b1f5c911fcb17b35ed6dbe`；各 `Bxx/source-wiki.txt` 与 `native-fx/AUTOMATIC-TIMING-NATIVE-RAW.json` / `AUTOMATIC-TIMING-NATIVE-SKILLS.json` 保存原字段。源清单中的旧 `integration.NOT_IMPORTED` 属预览历史状态，不是当前工程是否接入的判据。
