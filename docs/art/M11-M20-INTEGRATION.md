# M11–M20 已确认美术接入

2026-10-22，用户确认十只项目外卡面和动作预览后授权接入。从实时核实的main `6be1300`建立`codex/m11-m20-art-integration`，原工程未跟踪的PackageManagerSettings.asset保持原样。本轮目标：十只角色在正常启动选择页可以进入独立基础交战测试。

## 实际角色与范围

| ID | 角色 | 原模型 | 动作总数 | 特殊保留 |
| --- | --- | --- | ---: | --- |
| M11 | 雪怪狙击手 | enemy_1066_snbow | 4 | 无 |
| M12 | 雪怪术师 | enemy_1068_snmage | 4 | 无 |
| M13 | 雪怪小队凿冰人 | enemy_1069_icebrk | 4 | 无 |
| M14 | 霜星 | enemy_1505_frstar | 6 | Skill_1、Skill_2原动作名；非冬痕版本 |
| M15 | 游击队战士 | enemy_1078_sotisc | 4 | 无 |
| M16 | 游击队狙击手 | enemy_1079_sotisp | 4 | 无 |
| M17 | 游击队传令兵 | enemy_1080_sotidp | 4 | 无 |
| M18 | 游击队盾卫 | enemy_1081_sotisd | 6 | Move_Begin、Move_End |
| M19 | 游击队萨卡兹战士 | enemy_1084_sotidm | 8 | 强化后四动作 |
| M20 | 爱国者 | enemy_1506_patrt | 11 | 第二阶段、三段复起、Skill |

共10张透明卡面、55段动作、5,451帧。游戏本轮实际使用每只的基础idle/walk/attack/death，额外15段保存在对应`ExtraClips`目录，不加载为战斗技能、不触发原作阶段机制。卡面沿用本次确认的透明裁切与比例，不重绘，不增加独立精绘立绘。

源模型由既有本地Spine3.8评估工具离线渲染为60fps透明PNG图集，没有向Unity增加Spine运行时、2D Animation包或任何其他依赖。原始骨骼、加工库、看稿页面和视频留在项目外；游戏用图集、卡面、清单和必要meta随源码交付。

## 入口和数据连接

正常打开`InitialDemo`并Play，启动页显示可玩单怪样板，向下滚动选择M11–M20。进入后开始战斗，将底部卡拖到红门；暂停期间仍可部署。返回选择页会清理本场状态，重新进入完整重置。

- `LocalMonsterArt`扩展内部任务注册，按`CARD_Mxx`读取`Assets/StreamingAssets/MonsterCombatArt/Mxx/manifest.json`及PNG，保持缺包拒绝而非换其他角色。
- `CardDefinition`沿用既有卡面/战斗图接口；`Resources/MonsterTests/Mxx.asset`引用对应Card、Unit和Engagement数据资产，现有选择页自动发现，不改公共运行接口。
- `DemoSpriteVisual`沿用现有表现：行走读取路程/参考速度，待机和攻击读取模拟时钟，死亡读取独立展示尾段；根节点与碰撞不由美术修改。
- `ManualBattle`沿用寻路、索敌、停步攻击、伤害、暂停和穿门结算。没有新增冻结、指挥光环、复活、二阶段或主动技能。
- `MonsterCombatTools.PrepareNextBatch`幂等创建40份独立配置；已经存在的角色配置只校验，不覆盖人工数值。新角色试验数值不改旧M01–M10与塔露拉。

## 试验数值

所有新测试12DP、守方防御1、3次部署、部署CD1秒；霜星/爱国者卡牌招募档5费，独立样板部署费3DP，两类费用分开。其余试验参数见各自配置与工具分支，所有数值可由主导者试玩调整，不冒充原作数值或正式运营平衡。

雪怪狙击手/游击队狙击手采用现有远程物理，雪怪术师/霜星采用现有远程法术，其余采用现有近战。传令兵暂只验证身体动作和基础交战；并未为迁就美术发明指挥机制。首领的完整特殊机制另立任务确认。

## 来源与真实权利

具体PRTS页面、实际下载URL、源文件SHA256、作者与权利状态见[source-records/m11-m20-approved.json](source-records/m11-m20-approved.json)。例如[霜星](https://prts.wiki/w/霜星)、[爱国者](https://prts.wiki/w/爱国者)。作者按原作权利人记录，具体动画师未确认；这些模型/纹理/动作不是CC0，公开再分发许可未确认。用户确认看稿和要求运行素材随Git上传，不等于取得权利人授权。

## 验证与限制

实际Unity导入、测试、构建及独立Player结果以[QA](../QA.md)本轮记录为准；项目外5451帧的透明度/裁切/帧数/哈希与11个MP4解码检查不替代Unity测试。

没有修改主场景、Prefab、Packages或ProjectSettings；没有制作ZIP或更新Releases；不自动合并main。现阶段仍是独立单卡样板，不包含正式商店、升星、羁绊与混合编队。没有独立原作投射物、全场冰冻或范围光环；沿用既有通用攻击反馈。
