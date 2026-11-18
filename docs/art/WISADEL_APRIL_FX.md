# 愚人节维维美：原生特效接入

2026-11-18 最新修订。对照上一版棋盘录像发现，原 `skill_03_trail_game#9` 在当前画幅表现为短红色拖尾，大头辨识不足。将项目内已经确认的用户 GIF 表情裁片绑定给实际三技能炮弹，按弹序选帧，随炮弹从原 F_Muzzle 到实际目标运动。原拖尾和接触爆炸仍从现有图集播放。三技能间隔调到 1.1 秒，近距离飞行至少 0.5 秒，普攻和一技能保留独立时钟。已确认的 19 帧走路按实际行走距离取帧，恢复原先每 0.75 单位/秒对应一秒素材时间的步幅标定。

2026-11-18。身份为 `char_1035_wisdel@game#9`／绝对主角，正式卡仍为 `CARD_WISADEL_APRIL`。独立任务 `WIS_APRIL_G9`；B20／超新星 `sale#14` 保留为另一份源，禁止混用。当前状态为 **BOUND**；本次原生离线采样已通过，主工程 GPU 与 Player 验证由集成任务另记。

## 来源与采样

仅使用本地已校验 `battle/prefabs/effects/wisdel.ab`，版本 `26-10-29-15-36-11-33649006`，SHA256 `0878029bd9480d70c9f0589cf260deeb897a77a2047fc304bce3e8aa41b214d0`。SkinHooker 仅将 S3 六条 `buff_02_f/start/trail/hit/hit_02/hit_03` 替换为 `*_game#9`，普通 A/B 起手替换为默认 C 起手。S1/S2继续使用默认原资源。

复用已有工程外 native-baker，Unity 6000.6.5f1、Vulkan、RTX4060 Laptop、固定种子、1/60 模拟、30fps。31条原 Prefab 全部得到可见输出，最终31/31 PASS，零裁边、零 HDR 截断；32张运行页合计3,406,207字节。非弹道512画布，弹道1024画布、原视口12、1秒观察。原10单位/秒的弹道与相机同速跟随，保留根点，避免图集本身再走一遍。

保持原材质、Shader、粒子、网格、FxDelay、UV 和有效透明遮蔽；HDR 为 `sqrt(linearRGB/range)`，数值图集按线性纹理导入。Torappu 客户端脚本不执行，挂点、飞行、状态寿命由本作真实事件提供。尺寸按原身体 `.0045/.0027=1.666667` 标尺写入PPU；FollowMountPoint不继承FaceSwitcher `.27`，不得再乘一次。

原 Spine 在 OnAttack 的真实 F_Muzzle 为骨骼 `(100,100)`，映射身体平面偏移 `(.45,.45)`；Down 普攻为 `(.3375,.1125)`。原字段保存于包内 `ORIGINAL-MUZZLES.json`，不使用 BoneFollower 尚未跟随时的Prefab占位，也不重复叠加FaceSwitcher位移。

## 只读事件绑定

| 本作真实事件 | 原源层 | 时钟、归属 |
|---|---|---|
| 普攻主动作 | `attack_c_start` | action.At源年龄，原`.55`秒FxDelay保留，贴显示根 |
| 普攻真实飞行 | `attack_01_trail` | 仅LaunchedAt至ContactAt；原F_Muzzle到实际目标 |
| 普攻真实命中、终止 | `attack_01_hit`／`hit_02` | 每目标OnHit；每次真实冲击仅一份地面OnStop |
| S1真实主施放 | `skill_01_start`／`start_02` | 唯一主动作，不因多段子弹重播施法者起手 |
| S1真实子弹与命中 | `skill_01_trail`、`hit`／`hit_02` | 仅已建立飞行区间播trail；无飞行资料时不伪造 |
| S2实际wisadel增益 | `skill_02_buff` | 实际8秒增益窗口；动作和命中保留该次真实窗口身份 |
| S2强化普攻、命中 | `skill_02_start`、`hit`／`hit_02` | 原S2无移动主弹道，不沿用普通trail；无过载状态不播overload |
| S3每一枚真实子弹 | `skill_03_start_game#9`／`trail_game#9` | ProjectileOnly cue的Loop At，源`.5`秒FxDelay；仅实际飞行显示trail |
| S3起手瞄准目标层 | `skill_03_hit_03_game#9` | 源S3/AimEff是inputTarget／ON_CAST_START，不是OnHit；按Loop源年龄seek |
| S3命中、终止 | `skill_03_hit_game#9`／`hit_02_game#9` | 实际Hits；范围伤害不按受击人数重复地面爆炸 |
| S3实际频道 | 默认 `skill_03_buff_f` | 主动作Begin与首接触之前读真实parent，之后读实际BusyUntil |

S3首子cue在真实首发回调才创建；此前由已存在parent及原Begin `.833333313` 秒恢复首Loop的起手层，子cue存在后不重复。后续瞄准层在真实子cue建立时seek，缺少提前确定目标的事件，不能声称完整重现原客户端提前瞄准的前缀。所有年龄使用模拟Clock与源Rate；暂停、重开、换模型、清理复用Formation独立层池。

## 保留的边界

- 六脸 GIF 是先前获确认的用户素材。本次将其作为三技能的可见弹体，命中继续使用正确 game#9 图集中的水果粒子、星形与烟雾层。
- 原 `buff_02_f_game#9` 属于S3触发召唤的buff，出生、炸弹挂层、S1额外震荡弹、S2过载也需各自真实状态。资源保留在独立包，不借普通伤害假触发，不新增召唤、伤害或增益。
- 固定镜头仅采用现有正面、左右翻面路径；原up/down陪衬源已保存，不把未接的三向客户端脚本写成完整实现。
- 原Shader已在采样器得到支持与可见输出，这不替代本作相机、目标、双实体、暂停、清理、打包Player的最终验收。

运行读取：`FormationWisadelAprilFx.cs` → `BossRosterFxArt` → 独立 `BossRosterFx/WIS_APRIL_G9` 清单与 `BossRoster/WIS_APRIL_G9` 图集。`TaskForCard(CARD_WISADEL_APRIL)`返回新身份，`CARD_B20`仍是B20。预热只选本局技能所需源层，已有原生Boss通道不再同时绘制该角色。

复现工具：`tools/art/prepare_wisadel_april_fx.py`只读本地依赖，`pack_wisadel_april_fx.py`按公共框无损打包。证据工作区 `artifacts/formation-visual-repair/WIS-FX`保存最终job/report、源字段、原包闭包、逐帧哈希；运行包 `SOURCE-RECORD.json`保留必要来源。原素材权利状态未变化，不登记为CC0或开放再分发许可。本子任务未运行主工程Unity、未构建、未提交。
