# M02 猎狗：单怪战斗接入

霍利屹 / shigaharuki17，任务[#8](https://github.com/Peanora/YesterdayConvoy/issues/8)。当前用户已确认M02-Hound-v1，并授权从M08开始逐只接入；不冒认主导者最终审查。

从InitialDemo怪物选择进入“猎狗”：12 DP、每次1 DP、3次、冷却1秒，生命110、攻击30、护甲0、速度1.1、近战范围0.75、间隔1.4秒。守军生命140、攻击18、护甲3、范围3.2、不阻挡。数值为独立单卡试验，不是正式平衡。无主动技能、无技力。物理攻击原版OnAttack前摇0.6秒、接触延迟0.02秒，1秒完整动作收势。击杀续行、穿门幂等、死亡/暂停/重开沿用已验证通路。

四份独立资产位于Data/Units、Cards、Battles和Resources/MonsterTests下的M02.asset（稳定卡ID CARD_M02，单位M02_HOUND）。复用LocalMonsterArt、DemoSpriteVisual和原战斗模型，无第二套Manager；不改主场景、Prefab、Packages/ProjectSettings。已验证与M08并存，不共享实体生命、目标或状态。

原作enemy_1000_gopro，参考 https://prts.wiki/w/猎狗 ，权利属于明日方舟原权利人，具体画师/动画师和公开再分发许可未确认。原版帧、图集/卡面、实机画面仅本地。沿用本地M02-Hound-v1来源/处理记录，补充Die40帧，合计待机60/行走48/攻击60/死亡40帧。固定根点，256px画布、60fps图集，包CombatPackages/M02；安装到Git忽略的Assets/StreamingAssets/MonsterCombatArt/M02。public代码只含加载器、安装/打包工具和配置，不上传原作媒体。

Unity6000.6.5f1配置准备退出0；GPU Play两项全通过、零跳过，循环实际覆盖当前M02/M08的独立部署、原版帧、暂停、无技能、正常攻击、2倍速三次部署击杀/穿门、返回清场、重开恢复；隔离低生命模型核对每只实际死亡帧和逻辑位置不变。记录artifacts/m02-*。最终本角色独立Player通过；完整批次验证见QA。

尚未人工完整键鼠通关、第二机/长期性能。没有新增转身；卡面为模型排版。商店、升星、羁绊、混编和正式C阶段未实现。最终数值及公共接口由主导者审查。

最终批次验证（2026-10-22）：Edit69/69、GPU Play26通过/0失败/6项旧资源缺包跳过，本角色独立Windows Player部署/交战/受击/死亡或穿门/重开流程退出0。M06另有实际冻结检查，所有角色原版死亡帧已在GPU隔离模型验证；不声称每种配置下单只必死。最新证据、限制和入口见[批次交付](M02-M10-combat.md)及[QA](../QA.md)。
