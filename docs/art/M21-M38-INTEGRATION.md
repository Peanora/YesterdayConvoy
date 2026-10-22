# M21–M38 已确认美术接入

2026-10-22，用户要求“银狼先不做，其他的做到项目里”，并确认继续PR #15及不普攻接口范围。沿用独立目录和codex/m11-m20-art-integration；原工程main及未跟踪ProjectSettings/PackageManagerSettings.asset保留。开工fetch后main仍为6be1300、PR15分支333f437，工作目录干净；本轮不自动合并。

## 角色与实际素材

| ID | 角色 | 原模型 | 动作段数 / 采样帧 |
| --- | --- | --- | ---: |
| M21 | 宿主士兵 | `enemy_1043_zomsabr` | 4 / 230 |
| M22 | 宿主拾荒者 | `enemy_1044_zomstr` | 4 / 314 |
| M23 | 宿主流浪者 | `enemy_1044_zomstr_2` | 4 / 314 |
| M24 | 萨卡兹宿主百夫长 | `enemy_1114_rgrdmn` | 4 / 432 |
| M25 | 梅菲斯特 | `enemy_1507_mephi` | 4 / 572 |
| M26 | 泥岩小队掷能者 | `enemy_1090_mdsnpr` | 4 / 286 |
| M27 | 泥岩小队践行者 | `enemy_1091_mdhmmr` | 4 / 394 |
| M28 | 泥岩(敌方) | `enemy_1511_mdrock` | 7 / 855 |
| M29 | 深池方阵战士 | `enemy_1169_duphx` | 4 / 622 |
| M30 | 深池逐火战士 | `enemy_1288_duskls` | 9 / 595 |
| M31 | 深池焚毁者 | `enemy_1173_duspfr` | 13 / 644 |
| M32 | 蔓德拉 | `enemy_1523_mandra` | 22 / 2484 |
| M33 | 囊海爬行者 | `enemy_1152_dsurch` | 3 / 153 |
| M34 | 钵海收割者 | `enemy_1151_dslntc` | 7 / 915 |
| M35 | 壳海狂奔者 | `enemy_1147_dshond` | 4 / 214 |
| M36 | 首言者 | `enemy_1154_dsmant` | 8 / 521 |
| M37 | 萨卡兹大剑手 | `enemy_1010_demon` | 7 / 470 |
| M38 | 萨卡兹术师 | `enemy_1022_dmage` | 7 / 504 |

18张透明卡面、119段 / 10,519帧、312个PNG、26份清单；PNG总306,209,775字节，最大单张3,019,918字节。60fps离线逐帧采样，保持固定根节点与比例；沿用用户确认预览，不重绘、不引入Spine运行库或Unity包。原骨骼、评估工具、加工源和预览留在项目外。

71段基础清单包括17个attack键（M33无attack）；梅菲斯特attack实际是原作治疗施法，仅保留资源，不在战斗中作为伤害动作播放。因此70段基础动作参与当前表现，另外48段为ExtraClips保留素材。没有导入M41银狼、配置或生成图。

## 使用与连接

打开InitialDemo并Play，正常启动选择页为40个可玩样板 / 1个待制作（银狼）。向下滚动选择角色，进入并开始，底部卡拖到红门松手部署；暂停仍可部署，战斗暂停。三次部署、12DP、CD1秒、守方防御1均为样板试验值。卡牌招募费遵循候选档位，部署费另为1–3DP，不冒充运营系统已完成。

- 每位新增Card/Unit/Engagement/MonsterTests四份ScriptableObject，共72份配置。Resources自动注册，未改场景、Prefab、Packages或ProjectSettings。
- EngagementDefinition新增默认false的noOrdinaryAttack，快照映射到EngagementSettings.CanAttack；构造参数追加且默认true，既存角色继续攻击。ManualBattle不让不普攻单位因普通射程停步/索敌，不产生其普攻弹道；仍处理被阻挡、受伤、死亡、暂停与穿门。
- M25梅菲斯特与M33囊海爬行者禁用普攻。前者是人形版本，治疗施法素材不等于实现治疗；后者没有原作攻击动作，严格以声明noAttackClip的三段清单加载，不制造攻击帧。
- LocalMonsterArt按CARD_Mxx读取StreamingAssets/MonsterCombatArt/Mxx的图集，DemoSpriteVisual只读模拟时钟、路程及攻击/死亡时刻。无普攻时不访问缺失attack；表现不修改逻辑位置或碰撞。
- 首领额外动作仍不驱动阶段切换。蔓德拉基础使用同组C2_*_2（含C2_Die_2），其他阶段/Reborn保留；钵海收割者普通idle/walk配Skill_Attack，只做基础攻击样板，不代表实现狂暴状态转换。泥岩为敌方重甲版本。

## 来源与范围限制

来源页面、实际文件URL、原文件SHA256和作者未确认项见[source-records/m21-m38-approved.json](source-records/m21-m38-approved.json)。原角色/纹理/动作权利归《明日方舟》原权利人，非CC0、公开再分发许可未确认；用户看稿和Git上传决定不登记为权利人许可。PRTS提供查看/资料，不是开放素材授权证明。M23原资料查看器名称与模型有差异，按宿主流浪者条目指向的enemy_1044_zomstr_2记录。

本轮为基础美术与独立战斗测试，不含完整治疗、神经损伤、复活、阶段、召唤、特定投射物、全场技能、正式商店/升星/羁绊。攻击反馈沿用现有通用效果；额外动作未接到玩法。银狼保留为待制作，不把先前项目外生成稿视为接入。

## 实际验证

Unity6000.6.5f1导入、40份单位及入口场景校验通过；EditMode72/72通过（新增不普攻范围移动、阻挡/死亡与旧配置兼容性）。32/32 GPU PlayMode、修正文案后3/3菜单Play通过。最终Windows构建846218362字节/0错误，18只独立Player及最终6组回归均通过，源码6608c96139dbe544e06e56ab327fe87dea8f0b08；批次与最终覆盖差异及未测项见[QA](../QA.md)。默认仅本地EXE，不制作ZIP或Releases。
