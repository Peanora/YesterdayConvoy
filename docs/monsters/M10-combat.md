# M10 重装防御者：独立战斗接入

负责人霍利屹，任务 https://github.com/Peanora/YesterdayConvoy/issues/8 。当前用户已确认V1预览，并授权从M08开始逐只接入；不假定已获主导者最终PR审查。

InitialDemo怪物选择进入重装防御者。初始12 DP、3次部署、CD1秒，M10每次2 DP/招募2金币，其余1 DP/招募1金币。生命340/攻击40/护甲14/速度0.55/范围0.75/间隔2.6。守军攻击18/护甲3/范围3.2/间隔1/不阻挡，M10关生命200、其余140；这些是单卡试验值，不是最终平衡。无主动技能与技力，物理近战。原模型OnAttack事件对齐伤害前摇、1秒完整收势后续行。M03保留原版奔跑，不伪造慢走；M10按精英身份记录。击杀续行、死亡、穿门与重开真实走既有模型。

四份独立资产为Data/Units、Cards、Battles、Resources/MonsterTests下M10.asset；稳定卡ID CARD_M10。CardDefinition→LocalMonsterArt提供自己的卡面与帧，ManualBattle快照/实体处理状态，DemoSpriteVisual只显示动作，不移动根节点或扣血。不改主场景、Prefab、Packages/ProjectSettings。

原模型enemy_1006_shield，参考 https://prts.wiki/w/重装防御者 。权利属于明日方舟原权利人，具体画师/动画师及公开再分发许可未确认。V1同造型卡面为模型排版，补充原版Die动作供本轮游戏接入。来源/原文件校验/工具许可保留工程外V1，combat-events.json保存事件，CombatPackages/M10/manifest含图集校验；256px固定画布、固定根点、60fps。安装到Git忽略的Assets/StreamingAssets/MonsterCombatArt/M10，公共PR没有原作媒体或实机画面。缺包时角色保持待制作。

Unity6000.6.5f1实际配置创建退出0；GPU roster两项通过、零失败零跳过，遍历本轮已注册角色的原版帧、独立部署/暂停/帧冻结/无技能、2倍速三次部署攻击击杀穿门、返回清场与重开；隔离低生命模型核对每只真实死亡帧和逻辑根点不变。日志artifacts/m10-*。最终全体Edit/Play与独立Player将在本批次结束补录，未把M08已有结果冒充本角色Player结果。

尚未完整人工键鼠通关、第二机/长时性能、全方向转身；商店、升星、羁绊、多种怪混编与C阶段未实现。最终数值与公共接口待主导者审查。
