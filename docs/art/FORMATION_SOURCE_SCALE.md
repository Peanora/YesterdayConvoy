# 棋盘角色：源比例、愚人节维维美与万火归一清晰度

2026-11-18，按本轮用户修订恢复原资源比例并直接接入。此记录区分源元数据与实际GPU验收；子任务没有运行主工程Unity、没有提交或推送。

## 比例依据

`FormationUnitCatalog.Definition.VisualHeight`不再按小/中/重或费用把身体压成1.1/1.5/1.95。原生Boss读`BossRosterArt.Height`，旧包读已有`ArtCatalog.Height`（无该值时按idle格高/PPU），维维美读本轮确认的原愚人节game#9包，用户GIF保留回退。该查询只读清单，不解码动画像素。占地、人口、碰撞和战斗数值未随美术放大。

Boss原包采用既有塔露拉校准基线：每个原骨骼单位0.0045世界单位。它可恢复同源模型比例，**不是已经证明与原客户端每卡Unity根缩放、地图和镜头完全相同**。原actor Graphic缩放大多0.27，塔露拉/蔓德拉0.28、坎诺特0.24；还需在本作相机下核对，不把元数据一致当作像素级还原。

| 角色 | 旧高度 | 源高度 | 本轮呈现高度 |
|---|---:|---:|---:|
| 狂躁异质裂兽 B22 | 1.10 | 2.619757 | 2.619757 |
| 万火归一 B08 | 1.95 | 3.030295 | 3.030295 |
| 爱国者 M20/B17 | 1.90 | 2.810791 | 2.810791 |
| 巫王 B11 | 1.95 | 3.050106 | 3.050106 |
| 卡德霍 B14 | 1.95 | 3.300965 | 3.300965 |
| 圣徒 B13 | 1.95 | 3.061855 | 3.061855 |
| 塔露拉 B16 | 1.85 | 1.937684 | 1.937684 |
| 岁 B21 | 2.60 | 11.696982 | **6.00，场景Boss适配例外** |

岁原身体约18世界单位宽、11.697高，会超出右侧12×8战场。依主导者本轮确认采用高度6的等比上限，约为源比例51.3%、等比宽约9.2；没有继续使用旧小体型2.6。该例外及镜头保护须在实机验，不声称1:1。旧模式`BossRosterArt.VisualScale=.42`不用于新棋盘的这条比例计算。

## 万火归一重新采样

原图集把所有动作共用一个巨大视口，待机身体只有约62像素高；原512格与PPU20.397对应大量透明空间。直接放大现有帧不能恢复细节。

本轮使用本地原Spine 3.8.99骨骼、原796×796贴图和原atlas，复用已有隔离headless WebGL采样器，逐动作使用固定视口。A形态512格，B形态1024格，每页仍不超过2048；每段保持固定根点，按视口计算各自PPU，换动作时世界根点和骨骼比例一致。没有AI放大、重画、改原贴图或改动作曲线。

- 17动作、2065帧、352新图集页；182,201,076 PNG字节。
- A_Idle PPU由20.397提升至99.299，线性采样约4.87倍；B_Idle为40.794，约2倍。
- 85个alpha抽样时刻边缘像素0。B_Skill_3_End首帧在原低清页也完全透明，予以保留，未补画。
- 原时长、事件、帧数、循环与透明等待元数据逐项不变；新页SHA、源输入SHA和每段PPU已验证。
- 原低清页留作来源对照，当前runtime manifest只引用`hq-*`。`HQ-CAPTURE.json`与`SOURCE-RECORD.json`记录新采样；不能沿用“新帧与旧帧字节相同”的旧结论。

复现工具为`tools/art/resample_formation_body.cjs`和`install_formation_body_resample.py`。`package_boss_roster.py --task B08 --verify-only`识别此明确重采样策略，仍校验原输入、时长/事件/帧数、新页哈希、尺寸、PPU和旧对照页，明报`originalBytesAndHashesPreserved=false`。

## 维维美的实际皮肤

原来`CARD_WISADEL_APRIL`被核心Boss映射覆盖成B20超新星。本轮首先恢复用户GIF，继而核实并取得正确原生皮肤：`char_1035_wisdel@game#9`，原模型`char_1035_wisdel_game_9`；它与`@sale#14`超新星是两款资源。正式卡现在读取独立`BossRoster/WIS_APRIL_G9/main`，显式B20仍可做超新星来源检查，未删除资源。

原Spine为3.8.99，包含25个非零动作与一个零时长Default：真实Idle、普攻ABC/向下ABC、完整S1/S2/S3、Start、Die与Stun。60fps原样采样3207帧/210页，125个alpha抽样边缘像素全0；512格逐动作固定视口/根点/PPU，使用同一0.0045世界比例。所有原动作时长与OnAttack事件保留，PNG仅做逐像素验证的无损编码。原包没有Move，不能把用户GIF的移动片或静态平移冒称新增原生行走。

原普通Attack与S1的OnAttack为0.600000024秒，S2Loop/OverloadLoop为0.266666681秒，S3Loop为0.533333361秒；这些是源事件，不再靠旧GIF猜测。S2/S3 Begin本身没有OnAttack，不能与后续发射动作混成同一个接触点。详细来源、原Prefab缩放、技能和素材边界见`WISADEL_APRIL_ORIGINAL.md`。

原19帧走路、28帧普攻、100帧爆裂黎明、静态待机与6张表情裁片及CardDefinition引用均保留，作为已批准历史来源/回退，不覆盖为超新星。来源仍见`WISADEL-COMIC-2026-10-19.md`。

## 旧接触点恢复

18名有普攻的正式旧M角色原清单缺`attackContact`。已先确认其旧`Assets/Data/Battles/Mxx.asset`的attackDuration与源完整动作时长一致，再把已有attackWindup写回清单并标`EXISTING_APPROVED_ENGAGEMENT_WINDUP`来源；这是已有批准配置，不冒充新提取的原骨骼OnAttack事件。囊海无普攻不加；源石虫已有原接触点；维维美本轮由原game#9 Spine补回精确OnAttack。

`FormationUnitArt`提供`Duration`、`ContactTimes`、`ContactTime`和`SourceAction`只读源元数据；未知ContactTime为NaN，HasOrdinaryAttack按实际attack片存在性。固定动画2×由调用者统一应用一次，元数据不预先乘除速度。

## 验证边界

新增`FormationProjectileProfiles.TryFor(artId,nativePhase,out Spec)`提供纯数值原普攻弹道参数，不读Unity资产。原始`NATIVE-ATTACK-PROJECTILES.json`按原字节导入为`BossRoster/ORIGINAL-ATTACK-PROJECTILES.json`，来源记录保留原版本与SHA256。另从同版`COMPLETE-NATIVE-BINDINGS.json`保留霜星、蔓德拉的原attack和projectile字段子集、原快照SHA。

共30条原阶段路由、21名角色：原23条/14名（B01、B05、B07、B08、B09、B11、B12、B15、B23、B26、霜星M14、正确愚人节维维美，以及返场后的B13与蔓德拉M32），再加M11、M12、M16、M26、M42、M36、M38共7名。B13/M32首阶段近战、塔露拉普通近战和未知记录不虚构原速度。B01为原speed5的ParacurveMovement、抬高0.3/近距阈值1.3；B15为8，其余已映射者10。维维美逐项验证原Default Attack_A/B/C均为同一projectile_chr_wisdel_default，game#9皮肤仅替换特效，因此用原speed10/AdvancedMovement，别名WISADEL_APRIL/WIS_APRIL_G9，不借B20。B08的runtime第二形态映射原M2/source phase3；B26阴阳原弹道外观不同但这组速度/延迟标量相同。B05/B07/B08/B23原blocked combat近战分支不用于这张普通远程attack表，本作没有直接照搬原阻挡状态机。

新增7名的原`AdvancedMovement`字段均为speed10、delayToStart0、无dynamicSpeed；M26/M42共用`projectile_enemy_mdsnpr`，M26仅保留历史兼容。原6条Prefab记录（含M26/M42共享）按字节保留在`ORIGINAL-LEGACY-RANGED-PROJECTILES.json`，来源记录保留原包SHA256=`d7015e88d8839086c14b11c766367716834000c7556e09ccda79b8f5e35a887c`与逐角色路由；不把旧批准0.25秒当作原飞行速度。生成器支持`--output-root`先在artifacts形成同路径输出，避免主Unity验证期间改编译输入。

旧M11/M12/M16/M38/M24/M42/M31/M36的0.25秒和旧愚人节维维美0.22秒只作为`APPROVED-LEGACY-FLIGHT.json`单列的既有改编固定飞行时间；已取得原speed的角色运行时优先原speed，该历史文件不被TryFor使用。近战旧配置0.02秒也不能当作弹道。源字段不证明完整原客户端运动管理、追踪目标或伤害调度已经复现，新的Launch→Impact绑定由Root/Timing集成验证。

已运行纯本地原Spine重采样、源哈希/元数据/新图集验证、旧接触点同步一致性检查及静态差异检查，并目视旧/新A_Idle图集。未运行主工程Unity、EditMode、PlayMode或Windows构建；相机边缘、地面脚点、全部阶段、正常交易和真对局由Root集成后验证。原作媒体权利状态沿原来源记录，不因重新采样变成自有或CC0。

## 血条与巨物实机补充

B08/B21逐动作presentationTop来自原页全部帧alpha>8可见像素的并集，保留固定pivot/PPU后换算世界源顶；原几何captureBounds含大量空白，不能直接当头顶。原report、逐页SHA和差异保留在各包ORIGINAL-MOTION-BOUNDS/PRESENTATION-TOPS-SOURCE，工具为sync_formation_presentation_tops.py。HUD乘身体实际uniform scale，并使用显示足点（含便车抬高）；无元数据者保持源Height。B08第二形态才切到既有巨物固定包络，保护真实高翼身体；普通站位和商店开关不重构镜头。c610583视频是修正前对照，最终GPU/Player见QA。
