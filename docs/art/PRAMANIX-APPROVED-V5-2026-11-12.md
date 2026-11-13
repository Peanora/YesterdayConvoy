# 圣聆初雪 V5 看稿接入

2026-11-12，用户在完整地图预览后明确要求“按这个做进游戏”。本文件记录本次接入绑定，不表示已与原客户端完全一致；实测结果由本轮QA/Player报告记录。

## 常驻圈、雪地接触与图层修正（2026-11-12）

用户确认要求“技能持续期间全程可见”。查实buff01/range/flow三段都含一次性冷启动淡入，旧三秒整段循环会从接近峰值的第89帧跳回全空第0帧。现在仅初次播放0–3秒，之后重复原1–3秒稳态；caster/hit攻击脉冲时序保持，未改变RGBA、尺寸、HDR或FPS。稳态重复仍有粒子相位差，不称原客户端无缝循环。

用户另确认原雪地接触01看稿，并明确要求怪物不能被特效覆盖、攻击也要有特效。D27地面/常驻/攻击风圈/两种目标命中均使用绝对地面排序26–36，演员基础排序最低1024；原billboard与真实地面深度不变。源像素不重绘，角色身体后画，周边光仍可见。技能攻击起手start_01和技能攻击命中skill_03_hit继续读取实际CombatShot，未被雪地反馈取代。

原sbell2_buff_01_hit_01在原enemy_enter buff中用于入雪地成功法伤结算；02需要S2 mark，不接到当前S3。01经既有Vulkan原shader单实例采样，90帧窗口中仅1–34帧非零，35–89帧全RGBA为零。源码保留完整未改的PNG/无损RGBA页，运行按30fps播放0–35帧/1.2秒，保留全部可见内容及空尾，不把空白延长成3秒命中效果。原PPU64、pivot(.52173913,.48744316)、HDR16保留，SHA见来源清单。

SixthDefenderSkills在原TryDefenderDamageUnit成功结算后记录内部只读PramanixSnowImpact（包含护盾吸收，不仅HP下降）；保存真实来源/目标/时间/接触点，同帧同源目标只合并显示，原各雪格伤害和SnowHits计数保持。DefenderSkillVfxView仅消费journal播放01，不生成假CombatShot、不额外扣血、不跟随移动；暂停/尾段/重开沿用共享时钟和原层池。原目标挂点2精确高度和专用音效仍未完整移植；足点放置与层级按本次看稿和用户反馈适配。

## 已批准 C：补回循环风雪（2026-11-12）

以下记录初版C批准范围，当前播放和层级以本页上方修正为准。

用户看过B/C动态对比后明确选择“接入 C：补回循环风雪”。现沿用相同90帧/30fps、240×132、PPU39.38461685、pivot(.5,.4731722745)、HDR16原采样；PNG与无损gzip RGBA及meta随源码保存，RGBA SHA256为6d4517dfa50be53283ac5912f97aee0b22b7e425ae382b6daf6cffbfe8d1741d。

DefenderVfxArt将flow纳入关卡预加载；DefenderSkillVfxView仅在真实D27存活、对局有效、技能active时显示单个owner层，年龄读取clock−SkillStartedAt，按批准C重复三秒采样，不随攻击重新计时。排序偏移29与Map05显示抬高.025沿用C；暂停共用模拟钟，技能结束/死亡/重开/切场清理。没有新增战斗结算或修改技能数据。验证结果见QA。

本次批准的是C的明确适配：owner原点、三秒缓存循环和技能结束立即隐藏。原客户端挂点7、准确停止发射后的尾段和循环接缝仍未完全核实，不将本次接入登记为100%原客户端还原。

## 后续视觉差异修正（2026-11-12）

人类指出实机仍明显偏离原版，重新核对发现两项确定的接入错误：源采样工程在线性浮点目标捕获，而游戏是Gamma色空间，原PramanixNative解码后没有执行批准稿的sRGB输出转换；Map05施法者范围/脚下层深度锚点.732低于实际高台顶.739，批准稿则在.757。现按Gamma条件补输出转换，并只给Map05的施法者caster/buff01/range加.025显示偏移，不改变逻辑坐标、目标命中层、雪格高度或战斗数据。

真实GPU标定在修复前准确失败（期望通道0.539，实际0.251）；修复后标定、高台范围可见性和既有原生层回归4/4通过。原RGBA/PPU/pivot/HDRRange没有变化；原图和循环时长不重画、不加亮度倍率。后续Player/构建结果见QA。

原地面Atlas已直接查看：ground01/02/03本来就是平面雪纹。Vulkan代表探针中默认Animator/显式原态/关闭原禁用的控制粒子三组逐像素一致；朝向试验不能产生PV冰簇，未采用。原PV冰簇归属尚未核实，不把演示目标/其他实体擅自当成本作雪格。

原S3 active buff引用的switch_mode数值event0有CreateEffect(sbell2_skill_03_flow)，原Prefab_isLoop=1、holdByOwner=1；因此已知有一次创建的原循环风雪。event0/1的名字仍来自工具映射，spawnLocation7、UVRotation与原准确结束回收未完全核实。C确认前仅做项目外B/C补层候选；后按上方用户确认接入。其他原客户端挂点、相机与最终合成限制保留，不将显示修正或补层称为完整复刻。

| 内容 | 批准稿与源资源 | 真实读取/事件 |
| --- | --- | --- |
| 身体/行走 | V5所用D27原前向、原M03行走与待机PNG；Unity原D27正背面资源不替换 | DefenderRosterView / DemoSpriteVisual，原动作窗口、当前攻速和共享模拟钟 |
| 施法者 | sbell2_skill_03_start_01，49帧/30fps=1.633333秒 | 真实D27攻击组的action起点，完整源片长，不再压到1.2秒 |
| 目标命中 | sbell2_skill_03_hit，45帧/30fps=1.5秒 | CombatShot实际Applied与ImpactAt，按真实目标，不造成额外伤害 |
| 雪地接触 | 原sbell2_buff_01_hit_01，保留非零源帧及空尾共36帧/1.2秒 | 原入雪地成功结算→内部journal→真实接触点一次性播放；不套用S3 hit或S2的02 |
| 技能范围/脚下 | sbell2_skill_03_buff_01、sbell2_skill_03_range | 真实D27存活与DefenderSkillActive，不采用看稿五次攻击的固定节奏 |
| 循环风雪 | C确认的sbell2_skill_03_flow，90帧/30fps三秒原采样 | 每个owner读取自身SkillStartedAt和DefenderSkillActive，整段技能持续循环，不按攻击重建 |
| 积雪 | sbell2_ground_01/02/03 | 实际SnowCells：1层/2–4层/5层；清零、死亡、重开立即消失，不使用看稿三秒换层 |
| 接地 | V2人物alpha形状影、环境色和脚底深度 | 仅Map05HeatExchange启用投影影和V2色彩，其他地图保留已有接触影与风格 |

身体原片段60fps；三种S3攻击各2秒，原OnAttack标记约0.666667秒。游戏S3攻速倍率1.3及现有Windup/真实ImpactAt保持，未为匹配看稿改变伤害、SP、动作窗口或攻速。2秒间隔在实战可缩为1.53846秒，1.63333秒原caster尾段可能与下一真实攻击轻微重叠，按真实攻击组去重而不删原尾段。

原HDR采样的RGBA通过无损gzip `.bytes` 资源保存，运行上传为RGBA32 linear，保留alpha为0时合法的加色RGB。含C及原接触01共11页、9段，原PNG来源与原RGBA SHA256见 `Assets/Data/ArtCatalogs/OriginalCombatVfx/Defenders/D27-approved-v5-sources.json`。其他角色沿用原prepared纹理合同。Resources资源随Player自动包含，原PNG与必要meta随源码跟踪。

来源沿用2026-11-12热交换站V1/V5与后续C外部看稿的原Shader采样；原角色/特效权利归原权利人。用户看稿与接入要求不登记为开放再分发许可。buff02原挂点9未核实，继续未绑定；flow按已批准C接入，原精确生命周期/UV驱动限制如上。积雪原采样的最终合成仍与原客户端有差异，不能称完整客户端复刻。
