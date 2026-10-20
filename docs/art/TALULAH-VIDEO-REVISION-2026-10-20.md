# 塔露拉：按三段参考视频修订

本轮玩家可见目标：同一塔露拉试玩关中，改善辨识度、连续步态、挥剑与全场火流表现，保留已验收的部署、索敌、伤害、SP、暂停、胜负与重开规则。继续PR #4与codex/talulah-fire-rain，不修改原目录六项既存内容，不修改主地图/场景/相机，不新增Unity包。用户同时修订：没有明确要求时不生成、更新或上传ZIP。

## 实际查看的视频

| 用户文件 | 时长与解码信息 | 观察与本轮处理 |
| --- | --- | --- |
| b233d8aee3b880ad63c1d07a463408ea.mp4 | 31.95秒，H.264，编码1080×2400，显示旋转后2400×1080 | 蓄势、宽幅黑红挥击、火焰余势；改为连续武器动作和有厚度的弧形斩击，不再只有瞬间换图/细弹道 |
| 18c525489bbcf79fad7b68947ebb05b3.mp4 | 16.71秒，同上 | 画面有多排横贯战场的火流、长尾焰、地面燃烧与暖光；替换上一版稀疏的竖直小火滴与预警圈 |
| 551f8f2c82038af2f3c88427874c2454.mp4 | 5.40秒，同上 | 小步幅、身体重心/衣摆跟随；改用连续的腿部关节动作，避免只在两个极端姿势间切换 |

使用本机临时分析依赖imageio-ffmpeg（artifacts/media-tools）解码，检查全片接触表及局部时序；未向Unity安装视频解码包。视频包含原作画面和B站播放器/弹幕，只作为用户给定参考，原视频及其截图均不导入游戏，不作为CC0资源提交。

## 新形象与真实生成

以旧塔露拉与用户第三段视频为参照，使用本会话OpenAI内置image_gen真实生成：

- `exec-e87c5112-606c-4b45-8fd6-3cbdb7e898ea.png`：新版静态卡面，1154×1363 RGBA。
- `exec-2bf9c364-ceab-4d3e-b4b6-6b340f5d11ac.png`：九部件源图，1296×1214 RGBA。
- `exec-1ef8bbe9-2389-412f-8dfb-e14f222659ab.png`：四手臂部件源图，1154×1363 RGBA。

文件位于`Assets/_Game/Art/Characters/Monsters/Boss/TalulahComic/V2/`。新版去掉呆毛、圆橙眼/猫嘴/腮红与红色角纹，采用偏分银灰短发、较小黑龙角、冷灰眼与直线嘴形，保留黑红军装/披风/长剑。与维维美拉开头部辨识度，继续使用漫画描边与平涂。第三方角色的AI同人原型并非CC0，权利记录沿用前版，不新增商业发行许可。

核心提示约束：

> Preserve black military uniform, scarlet-lined cape, epaulettes, boots and long sword; bold comic outlines and flat cel colors. Remove ahoge; neat asymmetric side-part silver-gray bob, charcoal undersides, smaller slender backward dark dragon horns without red streaks; narrow steel-gray eyes, no orange slit pupils, no blush or catlike mouth. Calm stern expression. Full-body transparent neutral pose.

部件提示约束：

> Technical cutout parts atlas, exact same character and view, separate complete head, torso, cape, skirt, left/right upper legs, left/right shin+boot and sword. Additional atlas: left/right upper sleeves, left/right forearms with open casting hand and gripping fist. Hidden joint overlap, transparent margins, no full character or background, no labels/grid lines.

另一次16格AI行走图`exec-4cd60e5c-4302-4396-8435-954119e8a486.png`存在背景光晕和步态重复，已否决，不进入Assets，也不标记为合格动画。没有把加帧数当作自然步态的证明。

## 部件处理与动作连接

`TalulahRevisionTools.Apply`按实际源图的区域裁切，扫描Alpha边界、固定比例缩放、补透明边缘，输出13张部件PNG；统一PPU256、Bilinear、关节Pivot。该步骤是确定性裁切/技术归一化，不是Python重绘。源图与最终部件均保留，GUID不替换；重复执行只改美术绑定，不重设生命/攻击/SP等人工数值。源躯干已经带短裙，独立裙片备用、不叠画，避免双腰带。

`Talulah.asset`引用`TalulahArt.asset`，其中只保存Sprite引用。`DemoSpriteVisual`是唯一视觉入口：维维美继续播放原漫画帧；塔露拉由它调用`TalulahRig.Present`，不再运行旧八帧动画。模型根节点由原移动逻辑持有，Rig只改自己的部件局部位置/旋转，不控制碰撞或模型位置。没有安装Spine/2D Animation，也不声称使用了Unity SpriteSkin。

- 支撑阶段：脚相对身体线性后移，抵消步幅对应的前进距离；摆腿阶段：平滑前摆与正弦抬脚；双腿错半周期，双骨IK求膝盖，连续帧插值不交换两张脚图。
- 起停约0.16模拟秒过渡，头/躯干轻微重心变化，披风延迟跟随，武器与手臂反向摆动；不使用整张角色弹跳代替迈步。
- 普攻：0–0.40秒蓄势，0.40–0.66秒挥剑，随后收势至1.08秒；对应现有0.65秒命中，实际攻击间隔/射程不改。挥击显示黑红外沿/亮色刀光与命中特效。
- 技能：前0.65秒逐渐举剑/抬手，随后原地保持施法；原六秒六波全场伤害与模型移动锁不改，目标全灭仍播完。所有关节/火焰帧/光效读取战斗模拟时钟。

仍属轻量程序化分层动画：腿部各两段，靴子与小腿同图，尚无独立脚趾/脚踝变形；不等同商业级完整骨骼绑定，也没有新增转身动画。

## 新增资源调查与实际采用

| 资源 | 页面/许可 | 结论 |
| --- | --- | --- |
| Soluna Software / Lots of game effects | https://opengameart.org/content/lots-of-game-effects ，明确CC0、允许不署名 | 实际下载Fire01.png（1024×1024，8×4共32帧）与ToonExplosion31.png（512×512，4×4共16帧），接入横向火流/残焰和命中；保留作者与页面记录 |
| BenHickling / Animated Fire | https://opengameart.org/content/animated-fire ，CC0，64×64/60帧 | 有完整动画，偏像素且分辨率更低，本轮不引入 |
| Kenney Particle Pack | https://kenney.nl/assets/particle-pack ，CC0，512纹理 | 项目已有部分粒子纹理，可复用；单张纹理不足以直接给出参考中的流动火焰，本轮不重复下载整包 |
| Kenney Smoke Particles | https://kenney.nl/assets/smoke-particles ，CC0 | 可作未来烟雾候选，本轮不为收集而批量导入 |
| Arknights-Base镜像 | https://github.com/kiraio-moe/Arknights-Base ，仓库声明原游戏资源属于Hypergryph | 只核查技术方向，未找到新的再分发授权，不下载/导入其商业游戏模型或贴图 |

两张采用的CC0 PNG位于`Assets/_Game/Art/VFX/Skills/Talulah/`，源文件未重绘，Unity按原布局切图；授权依据快照见`source-records/talulah-v2-resources.json`。`FireRainView`播放48条分六排的横向火流、地面残焰与暖光，只负责装饰；`EngagementView`负责各次真实命中的表现，伤害依旧由`EngagementCombat`结算，不按48条装饰流重复扣血。

验证结果、已执行/未执行项目及实际预览见[QA](../QA.md)最新条目。旧ZIP保持原样；本轮生成独立Player仅用于验证，不创建新压缩包、不更新Releases。
