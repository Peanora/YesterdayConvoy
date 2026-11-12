# 圣聆初雪 V5 看稿接入

2026-11-12，用户在完整地图预览后明确要求“按这个做进游戏”。本文件记录本次接入绑定，不表示已与原客户端完全一致；实测结果由本轮QA/Player报告记录。

| 内容 | 批准稿与源资源 | 真实读取/事件 |
| --- | --- | --- |
| 身体/行走 | V5所用D27原前向、原M03行走与待机PNG；Unity原D27正背面资源不替换 | DefenderRosterView / DemoSpriteVisual，原动作窗口、当前攻速和共享模拟钟 |
| 施法者 | sbell2_skill_03_start_01，49帧/30fps=1.633333秒 | 真实D27攻击组的action起点，完整源片长，不再压到1.2秒 |
| 目标命中 | sbell2_skill_03_hit，45帧/30fps=1.5秒 | CombatShot实际Applied与ImpactAt，按真实目标，不造成额外伤害 |
| 技能范围/脚下 | sbell2_skill_03_buff_01、sbell2_skill_03_range | 真实D27存活与DefenderSkillActive，不采用看稿五次攻击的固定节奏 |
| 积雪 | sbell2_ground_01/02/03 | 实际SnowCells：1层/2–4层/5层；清零、死亡、重开立即消失，不使用看稿三秒换层 |
| 接地 | V2人物alpha形状影、环境色和脚底深度 | 仅Map05HeatExchange启用投影影和V2色彩，其他地图保留已有接触影与风格 |

身体原片段60fps；三种S3攻击各2秒，原OnAttack标记约0.666667秒。游戏S3攻速倍率1.3及现有Windup/真实ImpactAt保持，未为匹配看稿改变伤害、SP、动作窗口或攻速。2秒间隔在实战可缩为1.53846秒，1.63333秒原caster尾段可能与下一真实攻击轻微重叠，按真实攻击组去重而不删原尾段。

原HDR采样的RGBA通过无损gzip `.bytes` 资源保存，运行上传为RGBA32 linear，保留alpha为0时合法的加色RGB。9页、7段、原PNG来源与原RGBA SHA256见 `Assets/Data/ArtCatalogs/OriginalCombatVfx/Defenders/D27-approved-v5-sources.json`。其他角色沿用原prepared纹理合同。Resources资源随Player自动包含，原PNG与必要meta随源码跟踪。

来源沿用2026-11-12热交换站V1/V5外部看稿的原Shader采样；原角色/特效权利归原权利人。用户看稿与接入要求不登记为开放再分发许可。buff02原挂点9未核实、flow触发/UV驱动不明，两者继续未绑定。积雪原采样的最终合成仍与原客户端有差异，不能称完整客户端复刻。
