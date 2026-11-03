# 场上六名干员动作补齐（2026-11-03）

人类已确认项目外原速动作看稿，并明确批准DefenderArt、DefenderRosterView、AdvancedDefenderView及测试的接入范围。本轮沿用codex/battle-shop-loop / PR #51，保留两关闭环及所有原试验入口。

## 玩家能看到什么

- 后期两关闭环中的荒芜拉普兰德、余、圣聆初雪、望、塞雷娅、玛恩纳有原作正背面入场，随后进入待机。开始战斗不重新播放已结束的入场；动作不会阻止真实攻击调度。
- 初雪三技能轮换A/B/C动作；望轮换四种普通攻击姿态及三技能A/B，姿态对应另一套待机；玛恩纳待机中偶尔播放原作Idle_2小动作。
- 望的棋子用原作模型替代圆圈：落子、待机、触发三个阶段；未激活时偏暗。棋子的连通/触发/伤害/库存与弹药规则不变。
- 所有动作沿用原模拟时钟；暂停冻结，重开重新播放入场并清理旧棋子。角色仍使用相机对齐、原有正背面/左右朝向与脚底阴影，不增加自创转身。

## 资源与连接

新增30段角色片段及3段棋子，共2,755帧、70张PNG、93,688,811字节。角色追加在`Assets/StreamingAssets/DefenderArt/D21{B}/D22{B}/D27{B}/D28{B}/D29{B}/D30{B}`，棋子在`D28Chess`。保留原资产GUID；仅新增文件生成新meta。PNG原字节随源码提交，源skel/atlas、项目外看稿和Spine渲染库不引入工程。

`DefenderArt`读取manifest及可选逐片段pixelsPerUnit，按源像素与世界比例创建Sprite，兼容旧清单单一PPU；原接口默认第0变体。补充失效缓存检查，在Unity销毁缓存Sprite后重新读取，避免返回已销毁对象。它不改技能/攻击配置或资源加载策略。

`DefenderRosterView`依据本实体LastActionTime推进视觉变体，选择正背面动作；按实际动作窗口映射到原动画，不修改伤害、射速或技能SP。脚底枢轴与逐片段PPU保证大取景入场和小取景待机保持同一世界尺寸，Transform仍为等比1。已发射动作保留原有朝向锁定。

`AdvancedDefenderView`订阅原有ChessPieces状态，按创建时间/触发时间选择棋子Sprite；按棋子对象身份跟踪与清理，时钟来自ManualBattle，表现不施加伤害。

## 限制

- 塞雷娅源背面模型没有三技能Begin/Loop/End，仍用真实背面待机适配，不伪造背面施法。
- 望Start/Start_2中的黑剪影逐渐显现来自已确认的原始模型导出。
- 本轮不包含完整原作独立刀光、追猎实体、风雪、火墙或治疗粒子；现有简化外部特效仍保留，不能称为全套原作特效还原。
- 新增图集约94MB，未进行启动速度、加载方式或运行帧率优化。
- 未改场景、Prefab、Packages、ProjectSettings、技能数值、路线、商店或窗口逻辑。

原始URL及权利状态见[source records](source-records/field-animation-completion-approved.json)，页哈希见[import records](FIELD-ANIMATION-IMPORT.json)，实际测试见[QA](../QA.md)。原作媒体非CC0、开放再分发许可未确认；用户接入/上传决定不等于权利人许可。VERIFIED只指QA列明的技术验证范围。

## 集成状态

开工工作区干净，已fetch并核实origin/main=e14c8ca。预检发现当前PR #51与该main有12处既存合并冲突（主要为此前吸收#50后其被squash合并，以及两关闭环的输入/商店/文档后续修改）。本轮没有发起merge或修改这些冲突文件；以现有可玩功能分支验证、构建并更新PR，不能称为已合并main。
