# 超新星维什戴尔 Boss 的原作资源记录

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
