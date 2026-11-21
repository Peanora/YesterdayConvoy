# 超新星维什戴尔 Boss 的原作资源记录

## 2026-11-21 爆裂黎明的爆炸峰值与覆盖

对照用户提供的原作峰值截帧，关注白热爆芯、两侧红黑飞溅、上升碎片和高位 WIS 字样。上一版把地面与上扬粒子采在一张竖向卡片上，卡片的下沿穿入棋盘，正常深度测试切掉了向下展开的爆花。此次把 `03_hit_02` 原 prefab 的贴地分支与上扬分支分别在原生 Vulkan 路径采样。两段为 48 PPU，共 80 帧，并复用已经核对过的原 WIS 图层。原生白热爆芯由上扬采样前段发出。 [原包分层采样](../../../Assets/Data/ArtCatalogs/BossRosterFx/B20/SOURCE-RECORD-HD.json) · [离线层对照](../../../artifacts/formation-supernova-peak/native/split-peak-preview.jpg)

三层仍从同一真实炮弹落点按停止时序触发。地面层贴着落点地形顶面铺开，绘制于角色脚点之前。上扬爆花和 WIS 字样保持直立，并只修正这两张局部卡片的深度，使下缘留在该地块顶面以上，保持屏幕构图与地形深度测试。世界尺寸适配为地面原采样 1.27 倍、上扬 1.24 倍、字样 1.06 倍。首领伤害按本关 2.2 格半径结算，飞行、伤害与装填时序沿用现有关卡。

## 2026-11-21 重做「爆裂黎明」并提高二技能出手频率

逐帧核对 `sale#14` 原作战斗展示 30.3–31.3 秒的白粉翼焰、目标锁定、凝实弹芯和红黑立体落点，33.2–33.7 秒见下一次爆炸与翼焰持续。官方档案的「动作一／二」是皮肤动态立绘，战斗对照来自实战画面及对应原包。 [超新星战斗实录](https://www.bilibili.com/video/BV148G9z1En9/?t=30) · [官方皮肤档案](https://ak.hypergryph.com/archive/dynamicCompile/202504974.html)

原角色 `baseAttackTime=2.1`，专三二技能 `base_attack_time=-.7`，正常与过载的基础间隔都是 1.4 秒。原作 25 秒技能在中点进入过载，原 `MultiRangedAttackWithResetSpellCnt` 额外发射三次，`triggerDelta=.1` 秒。本关按玩家反馈使用 1.05 秒齐射与 0.90 秒过载轮间隔，维持开技约 8 秒进入过载及四发的源间距。基础动作 2×，每轮四发间的 0.05 秒基础间隔仍受攻速状态影响。 [原角色表](https://raw.githubusercontent.com/Kengxxiao/ArknightsGameData/master/zh_CN/gamedata/excel/character_table.json) · [原技能表](https://raw.githubusercontent.com/Kengxxiao/ArknightsGameData/master/zh_CN/gamedata/excel/skill_table.json)

二、三技能 20 层共 1,119 帧从 `sale#14` 原 prefab、粒子和着色器以 Vulkan 重采。常见图层为 32 PPU，直接命中为 76.8 PPU，爆炸字样为 48 PPU。采样保留原 HDR 及透明像素的加色通道。棋盘构建为这两组 HDR 图集保留完整 RGBA 像素，避免 BC3 压缩在爆炸渐变及暗红字样上引入色块。二技能分别保留普通和过载炮口、命中与延迟落地。三技能根据射向选原左／右、上、下炮口，沿真实发射至接触间隔移动原生弹芯，真实受击继续独立结算。 [高分辨率采样及逐页校验](../../../Assets/Data/ArtCatalogs/BossRosterFx/B20/SOURCE-RECORD-HD.json)

原 `wisdel_s_3[trigger_spawn]` 在拥有者处挂上 `03_buff_02_F/B` 白粉翼焰，并通过 `OnPlayEmitter` 执行一次 `03_buff_03`。`FollowMountPoint` 类型为 `SPECIAL_0`，皮肤正背分别绑定 `F_Chest_I` 和 `B_Chest_I6`，在待机和开炮时均约为源坐标 `(-3, 192)`。翼焰在六发技能状态内保持，起势后循环已采样的 1–2 源秒稳态。原作魂灵之影是另一条召唤与攻击路径，原展示里的细红线也由它发出。本关恢复式样沿用首领六发重炮机制，尖星锁定继续使用本体原 `AimEff`，伤害、攻击目标选择和炮弹速度沿用本关规则。

`03_hit_02` 是原炮弹停止事件，含地面闪光、碎片、碗形爆炸以及 `static_offset/fixed/name (1)/(2)` 中的原字样，不是条件触发的天赋余震。将落地粒子和原 `spw_sk_02` 红黑字样分开采样，字样恢复 `FxUVRotation` 并选用原图集的 WIS 单元，以较浅的采样角保留立起的轮廓。两层都随同一真实落点与原停止延迟触发，棋盘上分别绘制，字样略上移以避免将其压在贴身混战中。原炮弹接触声音仍沿用，原落点额外的 `WISDEL_S3_BOMB_EFF` 和锁定原声尚缺本地波形。 [原生分层离线对照](../../../artifacts/formation-supernova-original-fx/original-s3-stop-layer-composite.jpg)

## 2026-11-21 正背面高清重采与攻击辨识

独立 Boss 从本页所列的 `sale#14` 原骨骼和原贴图重新渲染。用每源单位 0.78 像素、249.6 PPU 输出 30 fps，沿用每单位身体宽度 320 源单位的换算。旧二技能正背面为 93.45/97.38 PPU，旧三技能为 160 PPU。当前 23 段、1,841 帧、147 页包含正背面二技能、过载、三技能和前视退场。帧数、原事件与脚点随清单逐项核验。 [前视重采记录](../../../Assets/Data/ArtCatalogs/DefenderArt/D31/ExtraClips/SOURCE-HD.json) · [背视重采记录](../../../Assets/Data/ArtCatalogs/DefenderArt/D31B/ExtraClips/SOURCE-HD.json) · [等尺寸采样对照](../../../artifacts/formation-supernova-boss-hd/source-body-match.jpg)

攻击层仍用 B20 的原 HDR 图集。源炮口采样中，二技能常规和过载的第一处可见火光在第 7 帧，三技能在第 16 帧。三技能原 `FxDelay._delayTime` 为 0.5 源秒。运行时让这些亮帧与各自 `OnAttack` 发射点对齐，三技能飞行使用从第 4 帧稳定亮核开始的原拖尾，接触时结束。每次过载出手仍分别触发。Gamma 工程在分层材质输出时转换一次，保留透明像素中的加色 RGB。炮口、拖尾和贴身命中采用局部前景排序，地面层留在相邻身体下方，维持地形深度。二技能小幅扩大炮口和命中，三技能小幅扩大瞄准、弹道和贴身命中，地面余波维持原采样尺寸。 [原生特效采样](../../../Assets/Data/ArtCatalogs/BossRosterFx/B20/SOURCE-RECORD.json)

## 首版来源与原事件对照

- 身体使用 `char_1035_wisdel@sale#14` 的前、后骨骼。对照 `D31/SOURCE.json` 与 `D31B/SOURCE.json` 的三个 SHA-256，逐个核对缓存中的 `.skel`、`.atlas`、`.png`。新增 `ExtraClips` 来自 `2026-11-11_26位Boss原作整备-v1/atlases/B20/{front,back}` 的既有 60 fps 采样。隔帧取原像素，以 30 fps 保存并按同一源脚点裁切。按原 D31 的 320 骨骼单位/格换算每个片段的 PPU，保留原拼接序列和末帧，无重绘或逐帧拉伸。S3 待机和开炮复用 D31/D31B 的 `Skill_3_Idle` 与 `Skill_3_Loop`，退场复用原 D31 前视 `Die`。来源见 [D31 前视](../../../Assets/Data/ArtCatalogs/DefenderArt/D31/SOURCE.json) 和 [D31B 后视](../../../Assets/Data/ArtCatalogs/DefenderArt/D31B/SOURCE.json)。
- 核对同批 `B20-front-all-actions.mp4` 的 S2 入场 25.97 秒、S2 连射 36.53 秒、S2 过载入场 37.93 秒、过载连射 48.70 秒、S3 入场 50.10 秒和 S3 炮击 59.50 秒，以及 `B20-all-fx.mp4` 的 S2 命中 57–63 秒、过载炮口 63–66 秒、普通炮口 66–69 秒、S3 接触 78–81 秒、瞄准 81–84 秒、实际命中 84–87 秒、炮口 87–90 秒、飞行 90–93 秒。视频是本地的原资产离线采样。前后视原骨骼、`CharacterSkinHooker` 与 `Actor/Projectile/EffectBehaviour` 来自 `26-10-29-15-36-11-33649006`，特效复用 [B20 现有原生采样](../../../Assets/Data/ArtCatalogs/BossRosterFx/B20/SOURCE-RECORD.json)。

| 本关绑定 | 原作字段与时间 | 运行触发与位置 |
|---|---|---|
| 二技能饱和复仇 | `Skill_2_Begin` 10/30 秒，`Skill_2_Loop` 42/30 秒，`OnAttack` 8/30 秒。`RangedAttack` 发出 `projectile_chr_wisdel_s2`。 | 单次炮口固定在原 Spine `F_Muzzle`／`B_Muzzle`。Boss 本轮使用的多目标打击由同一次 `OnAttack` 发出。 |
| 二技能过载 | `Skill_2_Overload_Begin` 5/30 秒，`Overload_Loop` 42/30 秒，其 `OnAttack` 8/30 秒。`MultiRangedAttackWithResetSpellCnt` 的 `additionalTimes=3`、`triggerDelta=0.1`。 | 同一身体动作，四次真实出手各自播放专用 `02_overload_start` 炮口。原 `Overload_End` 为 15/30 秒。 |
| 二技能命中与状态 | `AttachToTarget._delayToStart=.10`、`_delayAfterReached=1`，无飞行主图。`02_hit` 为 reached，`02_hit_02` 为 stop 地面层。`ActiveBuffAbility` 与 `BuffToOwnerDuringAbility` 对应 `02_buff`／`02_buff_02`。 | 开火后按同一模拟时钟延迟接触；受击点和地面层取结算位置。常驻效果由阶段状态开始、转段或击杀结束。 |
| 三技能爆裂黎明 | `Skill_3_Begin` 25/30 秒；`Skill_3_Loop` 150/30 秒，`OnAttack` 16/30 秒，`OnAttackFinished` 45/30 秒，`skill_storage_w` 142/30 秒；`Skill_3_End` 15/30 秒。 | 原 `03_start` 在炮口；独立 `AimEff` 使用 `03_hit_03` 在目标；原 `03_buff_f` 绑定前向的装弹状态。 |
| 三技能飞行和接触 | `projectile_chr_wisdel_s3` 的 `AdvancedMovement` 速度 `10`、跟随目标转向、没有起飞延迟、触及后 `.15` 秒停止。`03_trail` 是飞行主图，`03_hit` 为 hit，`03_hit_02` 为 stop 地面。 | 以真实弹道移动，落点按结算点播放一次地面余波，受伤目标分别出现原命中。源骨骼采样的炮口在 D31 比例下转换至棋盘，适配现有高台和镜头。 |

原包 Projectile prefab 与 `_graphic`、`_logic` 两种拆分体取自 `battle/prefabs/[uc]projectiles.ab`，SHA-256 `d7015e88d8839086c14b11c766367716834000c7556e09ccda79b8f5e35a887c`。前视 `F_Muzzle` 在 S2 发射时为 `(268,87)`、S3 为 `(424,265)`，背视 `B_Muzzle` 分别为 `(318,130)`、`(303,520)`，数值是四舍五入后的骨骼坐标，过载四次同坐标。三技能原 `03_buff_02_f` 由独立残影生成事件触发，本关无残影玩法。后视 `03_buff_B` 无可用的已采样运行页，本关背向时不使用前向的常驻图。地面排序和爆炸覆盖按本作棋盘调校，动作基准原作 2×，实机核验见 QA。

音频复用 [既有原音频导入记录](formation-original-combat-audio-2026-11-20.json) 对应的事件和资源。二、三技能开场使用 `battle.ON_SKILL_START.skchr_wisdel_2`、`battle.ON_SKILL_START.skchr_wisdel_3`；二技能普通出手使用 `battle.ON_ABILITY_ON.char_1035_wisdel.attack.1`，三技能出手使用 `battle.ON_ABILITY_ON.char_1035_wisdel.attack.3.1`；二技能普通和过载共用 `battle.ON_PROJECTILE_HIT.projectile_chr_wisdel_s2`，三技能接触使用 `battle.ON_PROJECTILE_HIT.projectile_chr_wisdel_s3`。过载独立的 `battle.ON_CUSTOM_TRIGGER.WISDEL_S_OVERLOAD` 和 `battle.ON_ABILITY_ON.char_1035_wisdel.attack.2` 音频尚未导入。事件对应关系见 [原作音频数据表](https://raw.githubusercontent.com/Kengxxiao/ArknightsGameData/master/zh_CN/gamedata/excel/audio_data.json)。

身体、特效和音源归属鹰角网络。沿用原记录所述的本地课程原型用途，公开再分发许可仍待确认。
