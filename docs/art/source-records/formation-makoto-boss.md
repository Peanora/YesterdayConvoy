# 结城理「见证荣光」的原作来源与绑定

使用 CROSSOVER 时装 `char_4217_makoto@p3r#1`，战斗 Prefab 为 `char_4217_makoto_p3r#1`。俄耳甫斯和塔纳托斯都在该皮肤骨骼内。正、背面动作取自同一版本模型。三段变身和三技能守备只有对应的正面源动作，棋盘在这些动作期间使用原正面。 [官方活动公告](https://ak.hypergryph.com/news/9681) · [原模型索引](https://torappu.prts.wiki/assets/char_spine/char_4217_makoto/meta.json)

对照官方技能 PV、原服装实录和该皮肤的正背面骨骼事件。原服装实录的三个段落分别从 `0:12`、`1:53`、`4:09` 开始。时装对比片左侧为「见证荣光」，`0:31–0:41` 可核对一技能发射、接触和治疗，`0:42–0:52` 可核对二技能琴谱、飞鸟和地面范围，`0:53–1:02` 可核对三技能弱点攻击与守备切换，约 `0:56–0:57` 进入俄耳甫斯。技能动作、事件时间和特效绑定以实际皮肤骨骼及下列客户端数据为准。 [官方技能 PV](https://www.bilibili.com/video/BV1RhtV6iEt3/) · [原服装技能实录](https://www.youtube.com/watch?v=U4uFXM2I1ME) · [时装技能对比](https://www.bilibili.com/video/BV1QStJ6mEn6/)

客户端资源版本 `26-10-29-15-36-11-33649006`。核验 `charpack/char_4217_makoto.ab`、`skinpack/char_4217_makoto.ab`、`battle/prefabs/effects/makoto.ab` 的原清单 MD5。角色 Actor、皮肤 `CharacterSkinHooker`、共享 projectile 和 buff holder 确认状态、施法、弹道与接触层；实际 URL、SHA-256、采样设备、裁切、尺寸适配及运行图集哈希见运行记录。媒体归原权利人，公开再分发许可未核实。 [运行来源记录](../../../Assets/Data/ArtCatalogs/BossRosterFx/MAKOTO_P3R_1/SOURCE-RECORD.json)

| 对局事件 | 原作动作与表现 | 棋盘绑定 |
| --- | --- | --- |
| 三段变身 | `Doll_Skill_{1,2,3_P1}_SwitchIn`，`makoto_change_01` | 一次变身动作触发一次皮肤特效；进入形态后显示常驻源层 |
| 一技能 · 俄耳甫斯的竖琴 | `Doll_Skill_1_Attack` 在源 `0.80s` 发射或治疗。皮肤 `SkinHooker` 将原投射物分成视觉和逻辑两部分，视觉走 `OriginMuzzle` | 区分法术和治疗起手。实际飞行才显示原 `trail_01`，按正背面源骨骼定位；真实接触才显示 `hit_01` 或 `heal_01` |
| 二技能 · 塔纳托斯的囚锁 | `Doll_Skill_2_Loop` 持续作战；Actor 不等待 Spine 攻击事件，攻击有 `0.80s` 源前摇。范围层独立于攻击；原恐惧处决检查间隔 `0.10s` | 区分原起手、常驻、地面、命中、处决层。处决效果只读独立处决事件 |
| 三技能 · 开辟明日的剑刃 | 塔纳托斯交替 `Doll_Skill_3_P1_Attack_A/B`。原 buff holder 分别将物理、法术接触映射到 `p1_hit_02`、`p1_hit_01` | 两种攻击方向复用前、后原特效，按实际伤害类别显示对应命中；由默认法术切成物理时显示弱点标记 |
| 三技能 · 切换俄耳甫斯 | `Doll_Skill_3_P1toP2_ChangeBegin` 为 `0.50s`，`ChangeEnd` 为 `1.00s`，后者在源 `0.80s` 触发变身特效。原俄耳甫斯每秒赋予治疗，目标先显示 `p2_heal_01`，`0.50s` 后恢复 | 守备常驻、地面圈和治疗读真实阶段与时钟。自疗、减伤及治疗次数为本作可通关的 DPS 验证规则 |

使用 Unity Vulkan 离线采样。交付的 36 段源特效均无采样裁边和 HDR 截断，压成 38 页无损 PNG。范围外圈按本作 4.5 格攻击半径调节，先播放源入场，再取常驻片段。原普通剑击、部署入场、组队总攻击和未出现在已验证治疗引用里的 `p2_heal_02/03` 未进入运行绑定。主工程 Player、棋盘遮挡和战斗结果见 [验收记录](../../QA.md)。
