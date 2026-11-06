# 原作共用特效运行契约

状态：Reader/Layer、CommonDirector与新增测试已在主工程。这里只记录接入合同与边界，不代替真实Player验收；新增测试等待根任务运行Unity。

## 文件、资源与像素

- `OriginalCombatVfxArt`只读两个生产者独占的manifest：`Assets/StreamingAssets/OriginalCombatVfx/Monsters`和`Defenders`。common/与monster/键归前者，defender/归后者；source原key保留，避免把一个角色的特效换名冒充另一个。
- Schema：version=1、fps=30、encoding=`sqrt-linear-rgb-effective-alpha`。每clip有key/count/width/height/columns/ppu/pivotX/pivotY/duration/hdrRange/loop/directional/forwardDegrees、pages(file/count/width/height)。最多600帧、4对齐图格、2048页面。JSON中额外来源字段可保留。
- PNG存`RGB=sqrt(nativeLinearRGB/hdrRange)`，A为原有效遮蔽率。不得把Alpha=0的加色RGB抹掉；贴图共享现有缓存/预处理路径，材质用原TalulahNative Shader，逐renderer `_HdrRange`。
- 本工程当前Gamma色空间是已核实边界，不改ProjectSettings，不声称这条缓存路径已在Linear色空间验收。
- pages允许角色子目录，但每clip所有页必须同目录，无绝对路径、`.`、`..`。Reader把该相对目录移到ArtSequence.Root，page.file转basename；root预热/预处理需用实际`Sequence.Root + page.file`。
- 元数据读取不解码PNG；`SequencesFor(keys)`返回既有ArtSequence，解码与释放仍由ArtMemoryCache。缺少约定key在Get/预热时明确失败，不把丢素材标成成功。

## 尺度与方向

PPU由capturePPU/native-to-game比例得到，pivot由原job.anchor投影并按整段共用裁边修正。每角色实际比例仍需根任务用身体原比例和实机投影核准，不能盲套塔露拉1.607。

Layer.Show没有隐藏FootLift、Y拉伸、淡化、随机缩放，普通层Transform scale固定1；worldAnchor由caller明确指定脚底、身体命中或原挂点。弹头`ShowOriented`按摄像机平面方向减源forwardDegrees旋转，非定向层不转。forward必须由原源或实际PNG核实。

`ShowGround`是单独明确调用的水平地面投影适配：原anchor和高度不变，法线朝上，图像Y轴沿摄像机up在地面的投影，Y世界尺度取`1 / abs(dot(camera.up, groundForward))`。此补偿在正交镜头下保留Sprite实际XY平面的投影尺度；本作真实轻透视镜头下，地面各点距镜头不同，会自然出现轻微远近缩放，不承诺所有角点与billboard严格重合。PPU、UV和脚底anchor不变，不为消除透视现象另加投影变形。它不改Shader深度测试，也不偷偷抬高；地面原高度由调用方提供，上方几何仍能遮挡。脚底施法、目标地面提示、生成/死亡、增益和羁绊爆裂使用此入口，抬高的身体命中与弹道仍用普通Show。

`ShowBeam`是显式端点适配：生产者pivot须为源束起点，referenceLength为源两端投影距离的game单位。只沿投影长度/referenceLength伸展，保留原纹理与宽度；这不是原生LineRenderer或粒子束流运行时重建。普通Show不会隐式做此适配。

## 调用与唯一所有者

`OriginalCombatVfxLayer(root)`使用Begin→Show/ShowOriented/ShowBeam→End；Clear立即隐藏再延迟Destroy，Dispose同时清材质。它只读模拟age，不写血量、攻击、SP、路线或单位逻辑Transform。

`CommonCombatVfxDirector`的PresentShot返回是否接管事件，包括延迟、取消和已结束窗口。不能拿Show的当前可见帧bool恢复旧诊断圈。Root在EngagementView先交专属native所有者，再交共用导演。

- 维维美GIF漫画、塔露拉、爱国者、泥岩、蔓德拉、梅菲斯特不交共用shot。M31喷火由B专属束流处理；受阻挡/沉默时另行发出的普通CombatShot由共用命中族适配，二者读取不同事件，不叠加。
- C专属普通投射：D03/05/11/12与D21–D30；共用：D01/02/04/06近战、D07弩、D08法术、D09火命中/输入目标层、D10治疗。
- D08强化弹读原stward_skill_01 trail/hit；D09原projectiles为空，不能造紫弹；目标层lava_attack_01_hit与火命中分开。
- B已实证M05/11/12/14/16/26/42/30/36/38普通投射键，共用导演直接消费，B不重复画这些shot。M12第三击按真实ColdDuration选原强化起手，不借六发Empowered标记；未有持续模型状态的snmage_skill_01_buff不强造Aura。
- M34原近战命中也直接使用enemy_dslntc_attack_01_hit。其余普通怪以common物理/远程/法术族作明确的原公共素材适配，不声称恢复了每个actor独特ProjectileDefinition。
- 现有Puff是普通出生/死亡唯一触发。EventDuration按该事件/角色取实际片长；五名已有native死亡Owner返回0，不把别人的长片段作为全局尾时长。B只画DeathBlasts/Fuel爆炸，不再叠普通死亡。
- 首12名S1共享ignite attack/defense/support来自原Skills字段。Cardigan自疗读heal-hit；Kroos/Steward没有虚构Aura。Root每帧调用PresentDefenderBuffs即可，无额外监听器。

## 预热与验证

CommonDirector.RequiredKeys为18项有限全集（17共用加碎冰必需的firstar原冰命中），SequencesFor()可直接并入本场加载；怪物与守军特定原key另由B/C本场Key表提供。禁止为一个角色全包重解码。

`common/common_gongjian_trail_01`此前因原TrailRenderer零点龄同步采样透明而暂缺，现已通过真实PlayMode点龄回放补齐并进入生产manifest。SequencesFor、弹道显示及尾段计算保留仅对这个精确key的缺失防护：若文件未配齐则不画弩箭飞行，不退回自画线，原命中仍显示；其他约定素材缺失照常报错。当前生产包不以此防护遗漏弩箭。

新增Editor测试核元数据/路径/数量/命名空间、无解码生命周期、原所有者排除、精确S1映射和共用依赖齐全；PlayMode核真实资源、池复用/立即隐藏、单位scale和HDR属性。同clock取帧不随Unity墙钟推进。真正的GPU像素、透明边缘、PPU和整体外观仍须根任务Player切片验证，Renderer数量不是画面验收。

接线补充：PresentBattleBursts只读已Resolved的感染爆裂、真实GolemPulses及ShatterEvents，分别用原火法/斩击/冰命中作明确羁绊适配，不产生伤害。PendingUntil只根据本场已经开始的原cast/target/hit、已发出burst、真实单位Spawn/Death及已触发one-shot buff计算绝对片尾；未结算未来队列和无限loop不增加全局延迟。目标提前死亡可以取消flight/hit，但已发caster原尾段继续；Erased仍按现有取消规则抑制。旧单怪图无ArtId的克洛丝，仅在精确名字克洛丝与id=kroos/kroos-正整数时视为D07，不把其他空ArtId猜成弩手。
