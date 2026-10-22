# 塔露拉：已确认原版预览的本地接入（2026-10-21）

## 2026-10-22 当前交付状态

用户明确要求现有运行素材全部上传；本角色运行PNG、manifest与原meta现随Git提供，普通克隆无需额外安装本地包。下方仅本地 / Git忽略 / 必须安装说明为历史；安装脚本保留供包维护使用。未更改角色、美术、动作或战斗数值，原作公开再分发许可仍未确认。当前范围与验证见[交付审计](MONSTER-RUNTIME-DELIVERY.md)。

用户已明确确认V4原版模型/动作预览“符合预期，就按这个做”。本轮只接入塔露拉造型、待机/行走/普攻/施法、卡牌与拖动预览；保留当前火雨、SP、伤害/攻击间隔、路线、速度、暂停、移动锁和维维美。没有修改主场景、Prefab、公共配置接口、Packages或ProjectSettings。

## 实际资源与安装

原作对象为enemy_1515_bsnake（“不死的黑蛇”）的塔露拉模型。公开模型源页面：https://prts.wiki/w/“不死的黑蛇”/spine ，源数据前缀为https://torappu.prts.wiki/assets/enemy_spine/enemy_1515_bsnake/ 。

使用项目外已确认的模型与Spine 3.8评估渲染工具，离线导出13张透明图集、1张卡面与manifest.json，共15个本地文件。原skel/atlas贴图和Spine运行时均不进入Unity。闲置288帧/30fps/9.6秒、行走216帧/60fps/3.6秒、攻击140帧/60fps/2.333秒、施法118帧/60fps/1.967秒，共762帧。源绘制比例统一，角色根点写入每个clip的Sprite Pivot，禁止逐帧重定位/变形；图集固定PPU88.8888889、Bilinear、DXT5，无mipmap。PNG原文件约29.5MB，图集压缩显存估计约42MB，不含运行时对象开销；未做低配长期性能测量。

本机项目外工作区：E:/珍贵资料/昨日圆车_美术预览/2026-10-21_塔露拉-v4/original-package。
本机安装位置：Assets/StreamingAssets/TalulahOriginal/（含同名.meta，全部Git忽略）。
manifest SHA256：e3bc3cc0d87a4ce07c2e5746fe8c4f8efab0613cddeafc9d49dba71475eadae5。

在具备合法可用的同一资源包、Unity未运行时安装：

    powershell -File tools/install_talulah_original.ps1 -Source "资源包目录"

安装脚本先检查全部文件存在再复制，不删除源文件；重复安装只覆盖该专用目录中的同名文件。游戏自动加载StreamingAssets的本地文件，不写回ScriptableObject。没有安装或资源损坏时记录TALULAH_ORIGINAL_UNAVAILABLE/INVALID并回退到已有技术样板；不会以缺失引用破坏其他开发者的克隆。缺包时原版美术专用PlayMode检查明确跳过，不能报告为已测。本机最终测试为16通过、0跳过。

## 连接与时间映射

- OriginalTalulahArt读取并校验本地清单，只缓存只读纹理/Sprite与四个clip，所有实体共用一份。程序集内部接口，不改变已有公共配置类/场景引用。
- DemoSpriteVisual按同一个ManualBattle.Clock、单位DistanceTravelled与攻击/技能时间选择Sprite，继续相机朝向、命中变红、深度排序；装包后不创建旧TalulahRig，不旋转/缩放角色逻辑根节点。
- 行走以DistanceTravelled/0.75映射源动画秒数，原速0.75时一轮3.6秒，停止/暂停不会继续踏步。
- 原作普攻OnAttack事件为源时间1.0333333秒。显示侧将该标记对齐既有ProjectileTime=0.65秒，其余收势适配原UnitInterval=1.7秒。仅调整视觉播放时间，不改发射/伤害结算；源2.333秒动作在本游戏的节奏因此与原速展示有区别。
- 施法保留原版抬手/收势素材，适配既有六秒火雨：0.8秒抬手、维持原施法姿势至5.4秒、最后0.6秒收势。目标全死也继续显示完整窗口，实际移动锁仍由模型负责，暂停共享冻结。维持阶段是固定姿势，场上火流继续按原模拟时钟播放，未宣称制作了新的原版六秒连续施法。
- DemoCardView、ManualBattleInteraction、ManualBattleController及TalulahTrial均从同一缓存取卡面/初始姿态；没有更改CardDefinition资产和字段，无新角色或商店逻辑。
- 关卡火雨继续使用现有CC0火焰与48条装饰流，并非原作完整全屏特效；克洛丝仍为现有漫画资源，本次未换图。

## 来源、许可和公开仓库

原作角色、贴图和动作属于原权利人；PRTS公开展示不构成CC0或开放再分发许可，用户认可造型和本地接入也不等于取得公开授权。原版媒体及其派生图集/实机预览仅留本地，已加入.gitignore，不随公共PR上传；公共仓库只提交读取/播放代码、安装工具和说明。不包含任何Spine运行时代码，不新增Unity包。其他成员需要另行准备可合法使用的本地媒体；普通克隆不会自动获得本次原版美术。

本轮不制作ZIP、不上传Releases。独立本地Player仅用于验证。具体执行结果见docs/QA.md当前条目；人类尚需试玩最终游戏内节奏，低配/长时与第二台电脑未测。
