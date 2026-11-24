# 棋盘完整挑战 v0.7：角色原作资源接入

用户于 2026-11-17 采纳完整 v0.7 设计，并明确同意“按推荐执行并直接接入”缺失角色。正式棋盘使用设计中的 46 个角色；兼容目录保留旧 43 张与新增 19 张 Boss，共 62 张。兼容目录不等于正式商店池。

## 来源与身份

本轮直接复用本地已验证的 `codex/boss-roster-integration` 原作媒体，原有 GUID、源 PNG、原动作片长、事件和枢轴不重新生成。其原加工来源为 `E:/珍贵资料/昨日圆车_美术预览/2026-11-11_26位Boss原作整备-v1`。运行不依赖这个项目外目录。

- `Assets/Art/Characters/Monsters/BossRoster` 与 `Assets/Data/ArtCatalogs/BossRoster`：26 个本体的 27 个正背视图，以及 8 个技能附属模型，共 35 个包、433 段动作、58,583 帧、2,722 张图集。附属物不增加招募卡。
- `Assets/Art/VFX/BossRoster` 与 `Assets/Data/ArtCatalogs/BossRosterFx`：21 套原生 HDR 特效包；来源、版本、逐帧哈希和原挂点绑定保留。
- 新增本体：B01、B03、B04、B05、B07、B08、B09、B10、B11、B12、B13、B14、B15、B21、B22、B23、B24、B25、B26。
- 七名既有核心在新棋盘模式优先读取后来已获批准的原作资源：梅菲斯特 B02、蔓德拉 B06、塔露拉 B16、爱国者 B17、冬痕霜星 B18、泥岩 B19、超新星维维美 B20。塔露拉第八章模型与 2026 版黑蛇 B24 身份分开。
- 其他既有角色沿用原批准包。M30 新棋盘复起/第二阶段使用其已有 ExtraClips，M31 施法使用已有原起手/循环/收手。旧模式的原 CardDefinition 资源选择与费用不因此重写。

原作媒体属于《明日方舟》原权利人，非 CC0。用户同意接入不等于取得权利人再分发许可。每包 `SOURCE-RECORD.json` 保留具体原网页、资源版本和哈希。

## 运行绑定

| 内容 | 接入位置 | 真实触发 |
|---|---|---|
| 卡片、费用、单位引用 | `FormationRosterTools.Install()` | 只生成/更新新 19 张 Boss 资产；正式店的全部费用读 v0.7 Rules |
| 原作身体动作 | `FormationUnitArt` / `FormationActorPresentation` | 真实移动、普攻、主技、死亡，所有动画使用同一战斗时钟 |
| 原生阶段与复起 | `NativePhase` / `NativePhaseReturningUntil` | 战斗逻辑确认阶段或返场后才换片；普通模式复活不伪装成原生阶段 |
| 原生施法与命中 FX | 原 `BossRosterFxArt` + 新棋盘绑定 | 真实施法开始或已应用的 `FormationHit`，不由特效再造成伤害 |
| 卡德霍猎犬 | `P14H` 原附属模型 | 真实 `Model.Summons`，不是游击队猎犬的替身 |
| 易的两道梁 | `P26` 原附属模型 | 实际落子命中的碰撞点，按本作技能演出相向合拢 |
| 泥岩巨像 | 既有 `GOLEM` 原作包 | 真实巨像召唤对象与其层级数值 |

身体片长映射到 v0.7 的动作窗口；这属于本作改编，不宣称重现原客户端全部技能调度。裂兽的四段切割使用原攻击动作，未把原作投石技能冒充切割。维维美分别使用原 S1/S2/S3；超新星模型本身没有 walk，移动时保留真实待机，不生成假行走。

图集按当前动作和前瞻窗口准备；长片分段读取。只有本场上阵/守军/必要召唤预热，不一次解码完整 26 人动作库。基础元数据查询不解码 PNG。构建由 `PreparedArtTools` 把完整运行清单与预压缩派生页打入 EXE；源素材与必要 `.meta` 随本地源码提交。

## 本轮已执行的资源检查

1. `package_boss_roster.py --all --verify-only`：35 包全部通过，源图字节/哈希、帧数、片长、原事件、枢轴与页尺寸保持。结果 `Library/FormationCampaignEvidence/body-source-verify.log`。
2. `verify_boss_roster_fx.py`：21 包、389 段、25,908 帧、390 页通过与原 HDR 帧逐像素对照，原绑定嵌入清单与侧文件一致。结果 `Library/FormationCampaignEvidence/fx-source-verify.json`。

上述资源检查不代替本轮 Unity 编译、GPU、完整流程或 EXE 验证。最终集成证据由主导者统一写入 STATUS / QA；以实际运行结果为准。当前范围没有执行推送或 PR 操作。
