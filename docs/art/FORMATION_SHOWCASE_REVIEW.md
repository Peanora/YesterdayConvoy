# 当前棋盘：全角色动作与羁绊表现核对

核对日期：2026-11-18。范围是 v0.7 正式池的 **46名角色**，不将兼容目录的62张卡、技能附属物或守军计入人数。本页用于辅助用户观看并指出不自然之处；**以下结论来自当前代码和资源清单的静态核对，不等于46人已逐一通过实机、美术或原作还原验收**。最终观感由用户看连续视频判断。

当前任务只制作现状演示和记录问题，不借录像修改正式数值、动作、特效或羁绊。尚未执行本页所述问题的修复。

## 演示产物与记录状态

拟导出到项目本地的 `artifacts/formation-showcase`：

- [演示索引](../../artifacts/formation-showcase/index.html)：按角色/羁绊定位，附录制范围和时间点。
- [全部怪物](../../artifacts/formation-showcase/00-全部怪物.mp4)：逐人展示走路、普攻、主技或被动。
- [羁绊联动](../../artifacts/formation-showcase/01-羁绊联动.mp4)：展示当前可触发的组合表现。

这些是**计划路径，本页编写时不宣称文件已完成**。实际录制入口、对应源码、分辨率/帧率、是否有音轨、夹具及程序输入范围、缺录项和实看片段由主导者在交付时补记。定向演示夹具与自然对局应分开说明，不能把直接触发动作/事件的演示称为自然构筑流程。

## 46人资源范围

运行依据：[正式设计](../design/FULL_GAME_v0.7.md)、[参数快照](../design/parameters.v0.7.json)、[正式规则表](../../Assets/Scripts/Runtime/FormationCampaignRules.cs)。原作接入来源和权利记录见 [FORMATION_ROSTER_V07](FORMATION_ROSTER_V07.md)。

| 身体资源组 | 人数 | 角色ID与名称 |
|---|---:|---|
| 既有核心改用BossRoster原身体 | 7 | M25 梅菲斯特→B02；M32 蔓德拉→B06；TAL 塔露拉→B16；M20 爱国者→B17；M14 霜星→B18；M28 泥岩→B19；WIS 维维美→B20前视 |
| 新增BossRoster身体 | 19 | B01 W、B03 弑君者、B04 锏、B05 大总统汉科、B07 爱布拉娜、B08 万火归一、B09 酒神、B10 坎诺特、B11 赫尔昏佐伦巫王、B12 阿米娅炉芯终曲、B13 圣徒、B14 卡德霍黑流之源、B15 最后的圣愚、B21 岁界园版、B22 狂躁异质裂兽、B23 安纳斯塔西奥赎罪者、B24 不死的黑蛇、B25 圣徒瓦列里奥、B26 易 |
| 沿用既有身体包 | 20 | SLUG 源石虫、M03 游击队猎犬、M11 雪怪狙击手、M17 游击队传令兵、M21 宿主士兵、M35 壳海狂奔者、M12 雪怪术师、M13 雪怪小队凿冰人、M16 游击队狙击手、M27 泥岩小队践行者、M29 深池方阵战士、M30 深池逐火战士、M33 囊海爬行者、M38 萨卡兹术师、M18 游击队盾卫、M24 萨卡兹宿主百夫长、M42 泥岩小队掷能者组长、M31 深池焚毁者、M34 钵海收割者、M36 首言者 |

七名核心的资源映射见 [BossRosterArt.cs](../../Assets/Scripts/Runtime/BossRosterArt.cs) 第30–31行。正式模式在 [FormationBattleModel.cs](../../Assets/Scripts/Runtime/FormationBattleModel.cs) 第373行进入 `TickCampaign`，其技能由 [FormationCombatSkills.cs](../../Assets/Scripts/Runtime/FormationCombatSkills.cs) 逐人实现；不能因为兼容目录仍有 `TemplateKind` 就判定正式46人仍只使用旧六模板。

## 动作中需要重点看什么

### 1. 四名角色没有源行走动作

| 角色 | 清单证据 | 当前行为及录像标注 |
|---|---|---|
| B11 巫王 | [B11清单](../../Assets/Data/ArtCatalogs/BossRoster/B11/main/manifest.json) 无Move/walk | 移动时回退待机，可能看到滑行；标“源包无行走，当前用待机移动” |
| B14 卡德霍 | [B14清单](../../Assets/Data/ArtCatalogs/BossRoster/B14/main/manifest.json) 无Move/walk | 同上 |
| WIS/B20 维维美 | [B20前视清单](../../Assets/Data/ArtCatalogs/BossRoster/B20/front/manifest.json) 无Move/walk | 实际使用超新星原模型，不是旧漫画walk；同上 |
| B21 岁 | [B21清单](../../Assets/Data/ArtCatalogs/BossRoster/B21/main/manifest.json) 无Move/walk | 当前位移沿用待机；不能称完整行走动画 |

回退入口见 [FormationActorPresentation.cs](../../Assets/Scripts/Runtime/FormationActorPresentation.cs) 第137–139、285–296行。源清单不存在某动作与该动作遗漏绑定是两种不同情况。

另有三项须按设计解释：B10坎诺特的源片只有Idle/Move/Skill，无普攻和死亡；其本作普攻改为治疗。B21岁设计上没有普攻。M33囊海爬行者也不普攻，受伤累计触发被动反击，当前没有独立反击身体动作（[FormationCombatDamage.cs](../../Assets/Scripts/Runtime/FormationCombatDamage.cs) 第151–154行）。这些片段应展示真实被动/治疗，不能伪造一次攻击补镜头。

### 2. 二阶段走路存在明确映射问题

[FormationActorPresentation.cs](../../Assets/Scripts/Runtime/FormationActorPresentation.cs) 第278–285行把逻辑动作 `walk` 拼成 `C2_Walk_1`、`Walk_2` 或 `B_Walk`，但源清单动作名是 `C2_Move_1`、`Move_2`、`B_Move`，因此会回退 `idle`。

- 可真实进入相关阶段的角色包括：M32/B06 蔓德拉、M20/B17 爱国者、B08 万火归一、B24 不死的黑蛇、B04 锏。
- TAL/B16 的二阶段分支也有同样命名问题；该分支在现有正常流程的可达性未由本次静态核对证明，不应把它写成已实机复现。
- B13圣徒使用专门的 `B_Move_1` 分支，不属于这一处拼名错误。

建议视频至少包含爱国者或蔓德拉一次原生返场后的移动，与首阶段对照。阶段触发规则见 [FormationCombatSynergies.cs](../../Assets/Scripts/Runtime/FormationCombatSynergies.cs) 第149–202行和 [FormationCombatDamage.cs](../../Assets/Scripts/Runtime/FormationCombatDamage.cs) 第120行；普通模式复活不能冒充原生二阶段。

### 3. 普攻身体被压缩，伤害也未等待源接触帧

[FormationCombatV07.cs](../../Assets/Scripts/Runtime/FormationCombatV07.cs) 第297行给普通攻击的动作窗口为 `min(0.36, 当前攻击间隔×0.45)` 秒；[FormationActorPresentation.cs](../../Assets/Scripts/Runtime/FormationActorPresentation.cs) 第184、286–291行将整段源普攻压进该窗口。因此即使录像以1×游戏速度播放，身体动作仍可能非常急促。

以下是源清单片长与当前基础间隔下0.36秒窗口的**静态计算示例**，不是录制软件加速，也不是实机逐帧测量：

| 角色 | 源普攻片长 | 当前窗口 | 约相当于源片速度 |
|---|---:|---:|---:|
| B09 酒神 | 3.00秒 | 0.36秒 | 8.3倍 |
| B22 裂兽 | 2.33秒 | 0.36秒 | 6.5倍 |
| WIS 维维美 | 2.10秒 | 0.36秒 | 5.8倍 |
| M28 泥岩 | 2.00秒 | 0.36秒 | 5.6倍 |
| B04 锏 / M14 霜星 | 1.60秒 | 0.36秒 | 4.4倍 |
| M20 爱国者 | 1.50秒 | 0.36秒 | 4.2倍 |
| B01 W | 1.30秒 | 0.36秒 | 3.6倍 |

普攻 `BeginAction` 后立即进入 `NativeSegment` 结算（`FormationCombatV07.cs` 第297–313行），没有读取源 `OnAttack` 接触点再落伤害。攻击“起手还没到位、目标先受伤”的感觉需要在连续画面中查看。主技也有本作独立动作窗口，不能将当前演示标成“原作完整原速还原”。

20名沿旧包角色中，SLUG和M33是被动；M31有已有施法起手/循环/收手专门分支；其余17人的主技身体大多复用普攻（`FormationActorPresentation.cs` 第172–184行）。梅菲斯特B02也明确用attack表现施法，圣徒B13首阶段亦采用原攻击动作。数值技能不同，不代表已有不同身体动作。

## 技能特效目前的实际覆盖

1. **公共命中兜底较多。** 上述20名沿旧包角色在此Formation表现入口未读取各自原专属FX，伤害/治疗使用公共斩击、法术、治疗命中。梅菲斯特B02、蔓德拉B06、爱国者B17、泥岩B19亦未在此路径读取对应BossRosterFx包。塔露拉B16另有A/B点燃施法者与目标层。证据见 `FormationActorPresentation.cs` 第303–338、363–366、394–403行。
2. **21套BossRosterFx导入不等于整套原技能已绑定。** 当前主要绘制owner的AttackStart/SkillStart、已应用伤害的Impact和Death/部分reborn；不处理ProjectileSpawn、StateStart/End、PhaseStart等完整生命周期，且起手最多取3层、命中最多取2层（第359–395行）。
3. **原挂点信息未充分使用。** `ShowFormationEffect/ShowFormationClip`（第414–425行）未按绑定的offset、animation、hitIndex、direction等信息重新挂点或定向；目前主要放在逻辑位置附近、面向相机。这可能影响贴身火焰、横向弹道和地面层的观感，需实看片段判断。
4. **所有命中还统一叠短直线。** [FormationBattleController.cs](../../Assets/Scripts/Runtime/FormationBattleController.cs) 第216–224行对每个 `Model.Hits` 画约0.24秒施法者到目标的线，包含治疗、近战和范围命中。它不是每个角色的原生弹道，可能让不同角色都看起来像在发激光。
5. B14猎犬使用真实 `P14H` 附属本体；B26易的两道梁使用 `P26` 原身体并在实际碰撞点合拢，见 `FormationActorPresentation.cs` 第31–35、385–386、427–452行。附属物不增加正式卡数量。

上述属于当前绑定范围，不是根据单张截图断言每个源FX都缺失。公共命中存在也不代表原专属效果已接入；源素材存在也不代表运行中会触发。

## 羁绊表现：有数值，不一定有专门特效

正式规则包含9阵营和9流派。大多数羁绊当前通过数值、攻击/施法频率、返场或公共伤害/治疗显现，没有独立战场特效。录像应显示真实组合和结果，并标注“当前无独立特效”，不能临时叠新光效冒充已完成。

| 羁绊/联动 | 当前可见内容 | 需要标明的限制 |
|---|---|---|
| 海嗣 | [FormationCampaignInterface.cs](../../Assets/Scripts/Runtime/FormationCampaignInterface.cs) 第308–320行绘静态青色溟痕方块 | 不是原生流动溟痕；神经/触发主要见命中和数值 |
| 泥岩 | [FormationCombatSynergies.cs](../../Assets/Scripts/Runtime/FormationCombatSynergies.cs) 第122–127行创建真实巨像本体 | 召唤本体可见不代表成长各阶段都有独立FX |
| 搭个便车 | 乘客位置随重型载体移动 | 第71–79行及 `FormationCombatV07.cs` 第278行直接令乘客位置=载体，身体无骑乘偏移，当前可能互相重叠 |
| 连击、迅攻 | 攻击段数/频率变化，公共命中和数字 | 没有各自专用连击或加速光效 |
| 复苏、提词、同频 | 治疗/施法频率与协同数值变化 | 没有专门的配对连线、返SP或节拍特效；公共治疗线不能当作这些羁绊的专属表现 |
| 接棒、剧场 | 原生阶段、返场和增益变化 | 阶段身体有独立分支，但没有完整羁绊触发FX |
| 整合、雪怪、深池、萨卡兹、伊比利亚、岁影、国度 | 各自资源/数值/状态与伤害结果 | 当前没有整套专属战场视觉；勿把角色原技能FX自动归为羁绊FX |

燃烧、雪、国度、黑蛇火沟等 `zones` 在 [FormationCombatDamage.cs](../../Assets/Scripts/Runtime/FormationCombatDamage.cs) 第225–254行参与真实周期结算，但表现层没有直接绘制这些zone对象，通常只见周期命中。护盾、冻结、增益层数也没有完整的独立显示层。是否应新增、补原FX或改演出，待用户看稿指出具体段落后再定。

## 后续实际录制结果

待主导者补记：视频是否完成、角色/羁绊覆盖数量、时间索引、真实触发与夹具触发范围、源码/构建、音轨、可见问题及未录项。静态核对不能替代此记录；录完视频也不自动代表美术验收通过。
