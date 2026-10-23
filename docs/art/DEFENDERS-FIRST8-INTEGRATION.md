# 首批8名守军接入记录 · 2026-10-23

用户在查看项目外原作模型预览后，明确要求“8个角色写进项目，再做一个复杂点的地图”，并确认了独立守军配置、战斗接口、表现与入口的修改范围。本轮在基于实时 `origin/main` 6be1300 的 `codex/defender-mixed-map` 独立目录制作。原工程未跟踪的 PackageManagerSettings 保留；不合入其他未合并功能分支。

## 实际运行资源

资源在 `Assets/StreamingAssets/DefenderArt/<ID>/`，随本分支代码、配置和必要 `.meta` 一起提交。每位包含透明卡面、正面战斗待机/攻击/死亡图集、运行清单和具体来源记录；斑点治疗采用其原Skill姿势，但不等同于完整原技能。静止守军使用正面动作与左右翻转，不把基建行走混入战斗。背面、部署下落、基建Move和其他未使用的技能动画不大量导入。

| ID | 角色 | 原作模型 | 具体页面 |
| --- | --- | --- | --- |
| D01 | 米格鲁 | char_122_beagle | [PRTS米格鲁](https://prts.wiki/w/米格鲁) |
| D03 | 斑点 | char_284_spot | [PRTS斑点](https://prts.wiki/w/斑点) |
| D04 | 玫兰莎 | char_208_melan | [PRTS玫兰莎](https://prts.wiki/w/玫兰莎) |
| D07 | 克洛丝 | char_124_kroos | [PRTS克洛丝](https://prts.wiki/w/克洛丝) |
| D08 | 史都华德 | char_210_stward | [PRTS史都华德](https://prts.wiki/w/史都华德) |
| D09 | 炎熔 | char_121_lava | [PRTS炎熔](https://prts.wiki/w/炎熔) |
| D10 | 芙蓉 | char_120_hibisc | [PRTS芙蓉](https://prts.wiki/w/芙蓉) |
| D12 | 梓兰 | char_278_orchid | [PRTS梓兰](https://prts.wiki/w/梓兰) |

素材来自上轮已保存并核对的默认原作模型，采用已有60fps骨骼曲线栅格输出；本轮只筛选、复制和配置，未声称AI重新绘制或新建骨骼。卡面为待机透明裁切，不是新绘精细立绘。玫兰莎保留Attack_Start / Attack_Loop / Attack_End，攻击前摇参考原OnAttack事件；炎熔完整保留1.5667秒动作。每个角色以待机首帧可见高度统一到1.25世界单位，通过清单PPU定标，所有动作保留固定脚底Pivot；Transform保持单位缩放。

原角色、纹理和动画属于《明日方舟》原权利人，具体画师/动画师未确认；不是CC0，公开再分发许可未确认。PRTS是具体资料和模型来源，不是开放授权证明。用户明确要求运行素材随源码上传，此决定不登记为权利人授权。源地址与原始文件SHA保留于每位 `sources.json`。本轮不分发Spine加工运行库、原始骨骼或项目外预览。

地图复用原 `Assets/_Game/Art/Materials/Industrial` 已登记材质，新增地形/风机/排水槽等为Unity基础几何组合，没有抓取原作关卡图片或新增不明来源环境包。技能/治疗弹道为项目程序表现，不声称复刻完整原作粒子。

## 关键连接

`DefenderTrialDefinition` 保存固定路线、8份位置和参数以及材质引用；`Snapshot()`转换为只读 `DefenderSettings`。`ManualBattle`复制快照，并为每名守军创建独立 `DefenderState`，负责索敌、阻挡、攻击、治疗、伤害和死亡，运行状态不回写配置。无roster的旧入口仍从旧EngagementSettings创建克洛丝，保持原单怪兼容。

攻击和治疗由 `CombatShot` 记录前摇后的出手时间与命中时间。医疗不会把治疗姿势误接成敌方伤害，不会复活；炎熔范围伤害只结算一次。`DefenderArt`读取清单构建Sprite帧，`DefenderRosterView`按照同一模拟时钟显示待机/动作/死亡；它不移动逻辑实体、不扣血。`EngagementView`继续绘制弹道和反馈，`ManualBattleController`显示各自最大生命值的血条。

`DefenseTrialMap`只创建临时地图并借用红蓝门/镜头；退出时恢复原状态，不保存主场景。初始选择页有独立入口。原漫画克洛丝和旧单怪配置仍保留，未覆盖场景或Prefab。

## 验证范围

PNG逐文件检查RGBA和最大2048尺寸；清单固定帧数、布局和Pivot由实际Unity加载核验。真实编译、配置、EditMode、GPU PlayMode和独立Player结果见 [QA](../QA.md) 本轮记录。人类外观与关卡体验验收、完整八人技能和第二台机器离线验证另行进行，不将自动测试通过登记为最终平衡或美术主观验收。
