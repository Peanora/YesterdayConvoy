# 怪物原素材特效整备

本轮替换早期自制的怪物攻击、命中、爆炸、冰封、喷火、神经损伤与阶段附件。角色身体动画和全部战斗规则保留；塔露拉的人类确认橙色穿透火浪、爱国者、梅菲斯特、泥岩和曼德拉的既有原素材导演保留。

## 运行资源与事件

资源位于 `Assets/StreamingAssets/OriginalCombatVfx/Monsters`。`manifest.json` 描述独立原层，`SOURCE-RECORD.json` 保存来源、原生采样、完整片长和适配，`BINDINGS.json` 保存从原Actor、projectile和buff模板读取的证据。`OriginalCombatVfxArt/Layer` 是共享读取和显示契约；本目录不包含看稿中的示意人物或固定靶子。

| 角色 | 真实事件 | 显示模块与原层 |
| --- | --- | --- |
| 高能源石虫、冰爆源石虫 | DeathBlasts | MonsterDeathEffectView；bslime/snslime原死亡爆炸。圆形危险半径仍是功能提示，旧爆炸圈和碎线退役 |
| 酸液源石虫、雪怪狙击手/术师、传令兵、泥岩掷能者/组长 | CombatShot | 共享普通攻击导演；根据真实原projectile链选择酸液、弩箭、冰弹、传令弹及能量弹 |
| 霜星 | IceRings、FrostSeals、SnowstormPulses、IsReviving | FrostSealView；caster起手和冰环、真实目标格的黑冰起始/持续层、三层原复起 |
| 逐火者 | FlamePhase、NearBurningReeds | MonsterOriginalEffectView；原余烬、入余烬和复起。燃烧苇草状态才显示原武器火焰附件 |
| 焚毁者 | FlameSprays实际伤害tick、FuelExplosions | FlamerazerEffectView；原喷火束、目标命中、燃料爆炸。没有用五条程序火线或冲击圈代替 |
| 囊海爬行者、钵海收割者 | CreeperPulses、HarvesterPhase | NeuralEffectView；原双层刺击、觉醒和狂暴附件 |
| 首言者 | CorrosiveBubbles实际施法/目标/最后命中 | NeuralEffectView；施法者、目标与命中各自独立，没有烘进一个假目标 |
| 萨卡兹术师 | Shackles | NeuralEffectView；原caster起手、原两端锁链与目标buff |
| 神经损伤爆发 | LastNeuralBurstAt | 原common_nerve_injury_01_hit；原buff模板实证为ep_break_sanity。它是一次爆发，不是循环播放的普通眩晕圈 |
| 其他普通攻击/出生/死亡 | 已有真实shot/puff | 原common族的明确适配；没有把通用库宣称为每只怪物的专属特效 |

## 时间与尺度

- 30fps，RGBA编码为`sqrt-linear-rgb-effective-alpha`，共享原生烘焙Shader解码HDR，并保留alpha为0的加色RGB。不能用普通透明PNG Shader替代。
- 共用裁边和脚下原点；cell按4像素对齐，页不超过2048。PPU来自原采样相机，普通层Transform scale固定1；两点束通过明确的原参考长度适配真实端点。
- 已知源颗粒的delay、burst、lifetime和原UV动画均保留。持续原层的采样循环明确记录为本作适配；未声称包含原客户端全部Torappu调度。
- 霜星三层原复起完整9.2667秒，按已确认方案映射为本作5秒。保持层的先后与相对延迟，不改变复起规则。
- 所有导演读取同一呈现时钟；暂停冻结。已发射的一次性尾段在终局延续，常驻循环不拖延结算。PendingUntil只读实际事件的片长，未使用全局固定10秒延迟。

## 验证边界

原生采样使用独立项目、Unity6000.6.5f1、Vulkan、RTX4060 Laptop；原包来自官方固定版本`26-10-29-15-36-11-33649006`，逐项验证内层AB MD5。采样检查可见性、裁边和HDR截断。原生采样通过不等于主工程Player通过；集成编译、实景、暂停/重开和构建的最终结果以`docs/QA.md`为准。

`MonsterOriginalVfxSceneTests`通过两只真实爆虫的伤害入口检验独立原爆炸、暂停选帧和重开清理。旧霜星回归需要检查新的真实目标黑冰，而不是继续断言旧生成稿的资源名或固定Renderer数量。

素材权利仍归原权利人，未获得开放再分发许可。用户已要求运行资源随源码上传，该上传决定没有登记为权利人授权。
