# 塔露拉漫画美术与火雨试验

2026-10-20，用户指定制作塔露拉美术并实际游玩，主动技能为参考《明日方舟》的全屏火雨。用户另行确认：保留维维美原关，以切换入口提供三名克洛丝的塔露拉样板；允许扩展共享战斗接口、表现与主场景绑定。本轮唯一写入者为主导者的 Codex，任务 [#3](https://github.com/Peanora/YesterdayConvoy/issues/3)。

## 来源与风格

- 身份资料：[PRTS 塔露拉](https://prts.wiki/w/塔露拉)，银发、龙角、长剑、黑红军装。选军装造型，沿用现有大头短身、粗黑线、平涂漫画方向。未下载其他商业游戏角色贴图进入工程。
- 火雨参考《明日方舟》的全场火焰压迫感；[“不死的黑蛇”资料](https://prts.wiki/w/“不死的黑蛇”)用于调查全场火焰表现，不将其身份与塔露拉本人混同。**“焚天火雨”名称、六波伤害与所有数值为本作试验改编，不声称复刻原作技能。**
- 生成工具：本次会话可用的 OpenAI 内置 `image_gen`，两次真实生成。参照现有 `kroos-comic.png` 和用户 WisadelComic/idle/idle-000.png 统一画风；第二次参照第一次生成的塔露拉。
- 输出为第三方角色的 AI 同人原型，并非 CC0，也不表示取得原角色正式商业发行许可。原角色归《明日方舟》相关权利方；工具输出条件与第三方角色权利分开记录。[OpenAI 使用条款](https://openai.com/policies/terms-of-use/)。未购买素材。

## 实际文件与处理

| 文件（均位于 Assets/_Game/Art/Characters/Monsters/Boss/TalulahComic） | 实际内容 | 技术规格 |
| --- | --- | --- |
| talulah-portrait.png | 单人全身静态卡面 | 1153×1364 RGBA，Alpha 0–255 |
| talulah-actions.png | 4列×3行，12个等大姿势 | 1448×1086 RGBA，每格362×362，Alpha 0–255 |

原始输出名称：`exec-2a699401-4ef5-49eb-b6e5-2b7b1ec390ab.png` 与 `exec-189e7d76-3b3e-4905-bfdc-87163521a12a.png`，生成目录为本机 `.codex/generated_images/01a14a88-fd28-7581-81a4-106d14ef247d`。复制到工程，保留原始文件；未以 Python 重绘角色或伪造图片。

行走使用前两行八帧；第9格站立，第10格挥剑，第11–12格举剑施法。普攻序列为站立→挥剑→站立，共三次取样，只有两个独立姿势；施法两姿势循环不是完整逐帧施法。不能将这些写作商业级完整动作集。无转身动画，和当前试玩方向约定一致。AI 走路帧仍可能有局部比例和脚步差异，待人工试玩评价后修帧。

由 `TalulahTools` 使用 Unity Multiple Sprite 精确切图，Bilinear、无MipMap、无压缩、FullRect、脚底附近固定Pivot(.65,.025)，PPU为362/1.8。卡面独立Single Sprite。保持原图比例；不逐帧拉伸、缩放或自动裁切居中。角色根节点只沿原路线移动，视觉节点面向现有相机，不改相机、地图、高台或原维维美资产。

## 生成提示词记录

第一次核心提示：

> Create game-ready transparent PNG character asset: Talulah from Arknights, fan game prototype. Match the attached two assets' exaggerated comic CHIBI style: thick pure black outlines, flat colors, oversized head, very short body and boots, bold readable expression. Character identity: silver-white short layered hair, two black dragon horns, stern confident slightly unimpressed expression, red-orange eyes, black military double-breasted coat with red inner cape, dark skirt, tall black boots, long dark sword with glowing orange blade edge. Full body, front three-quarter view looking slightly right, sword lowered on her right, free hand raised with a tiny orange flame. Not pixel art, not realistic rendering. Single neutral standing pose, no ground shadow, no scenery, no words or frame. True transparent background, all horns sword and cape within image. Do not copy rabbit ears, face or clothing of reference Kroos.

第二次核心提示：

> Create a production animation sprite sheet of EXACTLY the attached chibi Talulah character. Transparent background, precisely aligned 4 columns x 3 rows, 12 equal cells. Preserve identical costume, face, proportions and line thickness, feet at the same baseline. First two rows: eight-frame walk cycle to the right, three-quarter front face: left contact, weight down, passing, right reaches, right contact, weight down, passing, left reaches. Arms counter swing, sword low, cape follows. Third row: neutral; sword slash; fire rain sword raised with free hand out; stronger casting glow. No labels, text, grid lines or scenery. Separate full-body poses, transparent gutters. Flat polished bold black ink comic art matching reference.

以上为实际提示的核心约束摘要，非模型返回的尺寸承诺；实际输出尺寸以文件表为准。

## 技能与连接

`TalulahTrial.asset` → `TalulahFireRain.asset` / `Talulah.asset` → `ManualBattle` 的只读配置快照。塔露拉初始技力8/12，1SP/模拟秒；手动释放后6秒原地施法、6波全图攻击，每波攻击48、按现有护甲公式结算，不耗DP。六波在0.25、1.25…5.25秒发射，0.65秒后命中。三名克洛丝各210生命；塔露拉180生命，部署费2/DP12/一次部署/穿门扣1，招募价5只作数据记录。均为试玩参数，非用户最终平衡定案。

`FireRainView` 制作24处交错的火焰轨迹与地面预警圈；`EngagementView` 表现真实命中与漫画爆发。装饰落雨不重复造成伤害，所有伤害由模型按守军ID结算。模型、帧动画、特效、技能面板慢速、暂停与倍速共用模拟时钟；目标全灭后仍原地播完。自身死亡会清理表现并取消未生效伤害，重开/切换清空状态。

验证及限制见 [QA](../QA.md) 最新塔露拉条目；资源清单 VERIFIED 只覆盖所列实际测试，不表示完整P0角色/羁绊已实现。当前不实现塔露拉准备阶段的兵力调配羁绊。
