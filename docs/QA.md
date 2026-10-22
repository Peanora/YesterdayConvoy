# 验证证据与回归

## 2026-10-22 M02～M10独立单怪接入（霍利屹）

- 授权/范围：当前用户逐项确认V1造型，再要求从M08开始接入此前M02～M10任务。基线main e5c1d7d，新分支codex/monster-archer-combat，[任务#8](https://github.com/Peanora/YesterdayConvoy/issues/8)/[首批PR#9（人类已合入）](https://github.com/Peanora/YesterdayConvoy/pull/9) / [后续PR#11](https://github.com/Peanora/YesterdayConvoy/pull/11)。先M08，再逐只完成并分别正常提交推送其余角色。最终同步发现PR9已由人类合入，按项目规则从新main a306949创建codex/monster-effects-and-roster-completion，移植剩余5个自己的提交；无冲突，文件树与完整验证版93a7a8d一致，随后仅更新审查链接。既存.vscode保留不提交；无主场景、Prefab、Packages/ProjectSettings改动，无覆盖旧ZIP目录/他人修改。
- Unity6000.6.5f1许可证经当前用户修复后实际启动/编译/单位配置校验退出0，m08-unity-retry.log；后续九份独立Unit/Card/Engagement/ManualBattle资产均由Unity准备工具实际创建并验证，不是手填GUID。原版媒体打包2538帧/56PNG，安装器逐包SHA256/文件清单实际校验；运行时实际加载自己角色的idle/walk/attack/death和卡面，M09选Wizard皮肤，原作媒体及截图始终Git忽略。
- 最终EditMode69/69通过、零跳过、进程0：artifacts/combat-final-edit.xml/log。新增真实结算覆盖物理/术伤及最小1伤害、削甲按命中单次叠加/目标隔离/重开、死亡来源消失后延迟爆炸/范围/幂等/暂停/最终判负等待、寒冷再叠冻结与到期/暂停/禁止攻击、旧默认兼容及非法配置拒绝。
- 最终GPU PlayMode32项中26通过、0失败、6项因本目录未装M01和塔露拉原版包而跳过；进程0，artifacts/combat-final-play.xml/log。新角色相关检查无跳过：当前九只独立部署/原版帧/暂停和帧冻结、无技能/无SP、正常攻击/2倍速三次部署击杀/穿门、返回清场/重开；隔离低生命模型验证每只真正Die帧且逻辑根节点不动；场景实际验证爆炸预警/爆炸可见/伤害/暂停清理，酸液削甲、术师按法抗真实扣血，以及两只冰爆错峰部署后的冻结/停止攻击/暂停/重开。
- 最终Windows Development构建494094825字节、0错误、退出0：artifacts/combat-final-build.log，Builds/Windows/YesterdayConvoy.exe。最终冻结/爆炸尾段已纳入；随后仅补强Player诊断为跨完整部署流程检查攻击、给慢速重甲单位更长等待，游戏规则/数值未改。无ZIP/Releases。
- 独立Player九只分别执行-monsterCombatSmokeTest，最终全部退出0，entered/paused/attacked/hit/resolved/three/won/noSkill/clean均True，M06额外frozen=True。日志artifacts/combat-player-Mxx.log；每只普通可见D3D12窗口输出完整1280×720 HUD截图，已查看各角色协同攻击、选择页、M04爆炸和M06冻结；artifacts/combat-player-contact.jpg为真实截图裁切检查图，不是美术模拟图。截图与原作媒体不上传公共Git。隐藏窗口未输出截图的首次M08运行不算视觉证据。
- 首轮独立Player有两项诊断限制：单只M06会先阵亡，现严格在协同部署阶段也检查其真实普通攻击；单只M10击杀后仍需慢速走到门，等待延长至60模拟秒，未改速度凑结果。失败日志保留combat-player-M06-first-failed.log/M10-first-failed.log；最终九项全部通过。冻结首轮错误地在首次爆炸后才部署第二只，超出寒冷窗口；改验正常相隔1秒重叠部署。最终M06仍速度0.65、寒冷10秒，不扩大持续时间迎合测试。
- 旧独立Player四组回归均全True/退出0：combatSmokeTest、talulahSmokeTest、interactionSmokeTest、monsterSelectionSmokeTest；日志artifacts/combat-legacy-*.log。塔露拉使用本目录已有回退样板，不冒充其缺失原版动作的零跳过验证；M01缺包未声称本轮原版Player通过。
- 限制：本轮是九只独立单卡测试，A为当前用户确认、B为代码/自动验证后待主导者人工审查集成，C商店/星级/库存/羁绊/混编未完成。未执行完整真人键鼠通关、第二机、低配/长时/全分辨率测试；实体同位置可能重叠，未改避让或队列。克洛丝沿用旧漫画资源；新增弹道/爆炸/碎片为程序绘制简化表现，不声称原版完整粒子复刻。原作公开再分发许可未确认，不自动合并main。


## 2026-10-21 源石虫单怪可玩测试

- 授权与隔离：人类确认工程外V1美术，再明确确认Card/战斗/视图/输入/菜单接口及独立配置范围。从最新main70b9078（PR #4/#5已合入）创建codex/originium-slug-test；原目录codex/roster-draft六项既存修改不暂存、不覆盖。未修改主场景、Prefab、Packages、ProjectSettings。
- 配置创建：OriginiumSlugTools.Prepare退出0；3个Unit定义校验通过，SLUG_CONFIGURATION_PASS local art=True。只新增本角色4份资产及Resources目录，保留原维维美/塔露拉参数。已批准的6图集+portrait+manifest共8文件通过安装脚本校验复制，原作媒体及.meta均Git忽略，未上传。
- Unity6000.6.5f1 EditMode：64/64通过、0跳过，进程0，artifacts/slug-edit.xml / slug-edit.log。包含无技能零资源/禁止施放、正常近战穿门、无技能耗尽判负、配置拒绝与原场景/角色回归。
- GPU PlayMode：25/25通过、0跳过，进程0，artifacts/slug-play.xml / slug-play.log。4项源石虫覆盖暂停拖动立即部署扣费、模拟/帧冻结、无技能面板与技力、实际近战帧、运行中死亡/暂停、最后死亡0.5秒尾段与判负/清理、三只连续部署胜利/穿门、逐只部署失败与重开。原21项技能/火雨/选择页/图层等仍通过。
- 首轮Edit有1项测试错误地使用struct参数无参构造，取到C#零初始化而非可选参数构造；修正为显式参数，未改原有运行语义。首轮GPU有1项错误地要求多实体同一步只能有一次穿门；既有模型允许该步多个单位同时到达（各实体只结算一次），改验胜利/防御归零与现有幂等回归，不改路径/穿门规则。后续两套全绿。最后显示层仅调整选择卡排列/描述换行，独立Player复核。
- Windows构建：artifacts/slug-build.log，BASELINE_BUILD_PASS、0错误、进程0；最终325342065字节。产物Builds/Windows/YesterdayConvoy.exe。无ZIP/Releases。
- 独立Player：源石虫-slugSmokeTest十三项全部True、退出0；暂停部署、移动、技能拒绝、攻击/死亡清理、资源重开、三只通关、返回与旧角色切换均实际执行。正常可见D3D12窗口取得完整1280×720 HUD截图（选择、暂停、移动、攻击、死亡、集群、胜利），并查看比例/血条/图层/入口；三张卡并排、描述显式两行，无文字截断。artifacts/slug-player/visible-final，图片不上传公共Git。
- 独立Player回归：combatSmokeTest十三项、talulahSmokeTest十三项、interactionSmokeTest八项、monsterSelectionSmokeTest九项全True、进程各0，日志artifacts/slug-player/<flag>.log。回归进程未请求截图，不将隐藏窗口画面当视觉证据。
- 仓库静态检查839个跟踪文件通过，必要.meta/配置GUID引用与暂存差异检查；CSV保持21列；最终更改列表和源码绑定见Git提交/本地Builds/Windows/SOURCE.txt。代码、来源与配置进PR，原作文件/实机图片不进PR。
- 限制：未执行完整真人鼠标键盘通关、第二机/无原作包真实克隆运行、低配/长时性能与全部分辨率。克洛丝仍为现有漫画资源；多只同位置可能重叠，未改队列/寻路或避让规则；原128×128贴图放大有限。未实现正式商店、升星、羁绊或混合多卡。数值由人类试玩继续调整。

## 2026-10-21：启动选择单怪测试

- 用户明确确认控制器/输入/切换接口和新增选择页/测试范围，授权作为PR #4续作。只改相关运行时、测试与文档，不改主场景、Prefab、玩法配置、包或原目录既存修改；不制作ZIP。
- Unity6000.6.5f1：61/61 EditMode、21/21 GPU PlayMode，0跳过，进程均0。artifacts/monster-menu-edit.xml / monster-menu-play.xml。新增3项覆盖正常启动拦截输入/空跑、39个待制作/未知ID拒绝、两个样板进入、施法与拖动返回清理、结算完整按下/松开、跨怪物重新进入。原图层、交战、暂停及火雨测试保留断言并显式选择所测样板。
- Windows构建：BASELINE_BUILD_PASS 321558422字节，0错误，进程0，artifacts/monster-menu-build.log。
- 独立Player：-monsterSelectionSmokeTest九项、-talulahSmokeTest十三项、-combatSmokeTest十三项、-interactionSmokeTest八项均全True，进程各0；artifacts/monster-menu-player/各flag.log。原版人物包实际加载，未回退。
- 屏幕取证单独核对：隐藏D3D12的ScreenCapture失败、隐藏D3D11为黑图，均不计成功。缩小到实际窗口显示条件后，正常窗口D3D11及默认D3D12均获得1280×720真实完整截图；9项入口检查仍全True、退出0。已查看选择页/塔露拉战斗HUD，无文字截断或遮挡，左下返回入口可见。正常窗口证据保存在artifacts/monster-menu-player/visible-default及visible-d3d11，保留失败日志以区分。
- 当前两个内置样板可玩，其余39个只展示待制作。新增Resources/MonsterTests配置注册使用现有校验，尚未制作第三只怪物来完成真实新配置接入验证；不能把入口开放等同于普通怪/无主动技能/完整多卡系统已支持。
- 文档52个本地链接、39个待制作名称、暂存差异与仓库静态检查815个跟踪文件通过，新增脚本.meta齐全；静态检查不代替上述实际Unity/Player验证。
- 未执行：完整真人鼠标键盘通关、第二台电脑、低配/长时性能与所有分辨率人工测试。本轮未重新生成美术；含原版角色的截图只留本地，不上传公共Git；ZIP未更新。

## 2026-10-21：已确认火雨预览的表现接入

- 范围：FireRainView、视觉专用FireRainScreenEffect/两张shader、新增CC0碎焰图集及处理/导入工具、对应Play测试与原动作取证。保留人物和玩法；未改主场景、Prefab、配置、Packages、ProjectSettings或原目录六项既存修改。
- 导入/编译：FireRainArtTools.Prepare实际退出0；640×640无损透明图集、25帧/Alpha归一、两shader资源检查通过，artifacts/fire-rain-import.log。
- Unity6000.6.5f1：EditMode 61/61，GPU PlayMode 18/18，零跳过，两进程均0。artifacts/fire-rain-edit.xml / fire-rain-play.xml。新增检查包含实际GPU画面后处理差异、暂停全画面冻结、火浪向右/贴图帧变化、目标死后原地播完、自然结束/重开清除色调与偏移、切回维维美，相机Transform保持。
- Windows构建：BASELINE_BUILD_PASS 321546090字节、0错误、进程0，artifacts/fire-rain-build.log。
- 独立Player：-talulahSmokeTest十三项、-combatSmokeTest十三项、-interactionSmokeTest八项全True，各退出0；artifacts/fire-rain-player/各flag.log。原版人物加载日志TALULAH_ORIGINAL_LOADED，未回退。
- -talulahMotionEvidence：walk36/attack33/rain180，cast=True、进程0；查看真实相机图中的大火浪、亮核/碎焰、场景色调及完整六秒帧序列。相机图不含IMGUI HUD，不能称为完整人工键鼠录屏；本轮未新增音频。
- 仓库静态检查810个跟踪文件通过，新增资源/脚本.meta完整，CSV各行21列及暂存差异核对通过；不将此项代替上述Unity/GPU/Player验证。
- 未执行：完整人工键鼠通关、第二台电脑离线、低配/长时性能与所有宽高比人工检查。源图分辨率有限，无真实体积火焰/热折射，非原作完整特效。原版媒体和含其图像的本地取证不上传公共Git；不更新ZIP/Releases。

## 2026-10-21：已确认V4原版塔露拉的本地接入

- 用户先明确确认原版模型预览，再授权接入；只改角色显示、卡牌/拖动预览与对应测试，不改主场景、Prefab、玩法模型/配置、Packages、ProjectSettings或维维美。
- 离线渲染13张透明图集、卡面与清单，15个本地文件、762帧（idle288/walk216/attack140/cast118）。源根点固定为Sprite Pivot，PPU88.8888889；Unity运行时动态Sprite共用只读缓存，暂停/变速均读取原模拟时钟，GPU用DXT5。原作媒体与派生图集Git忽略，不自动公开；缺包回退和安装入口见[接入说明](art/TALULAH-ORIGINAL-INTEGRATION.md)。
- Unity6000.6.5f1 EditMode：61/61通过，0跳过，artifacts/talulah-original-edit.xml。
- GPU PlayMode最终复核：16/16通过，0跳过，原版Sprite名称/帧推进、根位置不被动画修改、暂停帧冻结、目标全灭仍继续技能/移动锁、结束恢复移动、重开及维维美回归通过。artifacts/talulah-original-play-final.xml / .log；最终进程退出0。初次XML已16通过但外层退出状态异常，额外复核后获得明确进程0，不忽略该状态。
- Windows独立构建：BASELINE_BUILD_PASS 319893271字节，0错误，进程0；artifacts/talulah-original-build.log。
- 实际最终Player：-talulahSmokeTest十三项、-combatSmokeTest十三项、-interactionSmokeTest八项全True，分别退出0；前者日志含TALULAH_ORIGINAL_LOADED，确认装包路径实际生效，没有回退旧图。
- -talulahMotionEvidence：walk36/attack33/rain12真实相机帧，cast=True、退出0，同样含原版已加载日志。已检查放大行走/攻击与正常地图火雨图；相机取证不含IMGUI HUD，非完整鼠标人工通关或全屏录制。证据在artifacts/talulah-original-player，各组日志同名。
- 原作普攻2.333秒适配现有1.7秒间隔并对齐0.65秒命中；施法适配六秒（抬手0.8/维持到5.4/收势0.6），维持阶段为原姿态固定帧。未变动伤害、攻速配置、实际移速、暂停与技能移动锁。角色原版，火雨仍为原CC0近似效果，克洛丝仍漫画版。
- 原目录六项既存修改保持隔离。仅将本任务源码/安装工具/文档和必要.meta提交到原功能分支；原图集、原骨骼、Spine运行时、Player/日志/实机图不进公共PR。没有制作/更新ZIP或Releases。

未测：完整人工键鼠通关、最终攻速映射的主观节奏、第二台电脑离线、低配/长时性能；不把代码和图片检查当成人类对最终实机的认可。其他克隆缺少本地图包时仅能看到保留的技术样板，原版专用Play测试会明确跳过；本机最终没有跳过。


## 2026-10-21：美术风格文档与预览验收状态

仅文档修改：逐字核对用户美术段落，检查GDD有效方向与旧漫画历史的优先级、相关美术规范和STATUS一致；登记塔露拉项目外预览未通过，原版相似度与动作自然度须分别人工确认。核对Git差异仅含本任务文档，不修改原目录六项既存内容。git diff --check与tools/check_repository.py通过（787个跟踪文件）；仅为格式/仓库静态核对。Unity、EditMode/PlayMode、Player与构建均未运行：本轮无运行时代码或资源改动，旧测试记录仅属于旧实现，不作为新美术认可。未生成图片/视频、未导入、未更新ZIP。美术实际资源与动作仍待下一次预览，不标记VERIFIED。

## 2026-10-20：按用户三段视频修订塔露拉动作/全场火流

继续PR #4同一功能分支，保留原目录六项既存修改；用户本轮明确要求按视频改普攻/火雨/行走与头部辨识度，并停止默认更新ZIP。共享文件范围沿用本功能此前具体确认；本轮不改场景、Prefab、Packages/ProjectSettings、战斗模型/数值与维维美资源。

- 实际解码三段MP4（31.95/16.71/5.40秒），查看全片接触表、攻击/步态局部时序；视频只作参考，不导入原游戏贴图。采样/元数据在artifacts/video-reference。
- OpenAI内置image_gen真实生成新版卡面/身体部件/手臂部件，Alpha0–255；候选16格行走图有背景光晕和重复，否决并未导入。Unity精确分区/按Alpha裁切、比例归一化到PPU256、关节Pivot；不拉伸图形掩盖大小问题。
- TalulahRevisionTools.Apply真实执行；重复执行检查37个派生PNG/meta、卡牌/Art/战斗配置和主场景，SHA256相同，无重复对象、无覆盖玩法数值。两张CC0特效原PNG哈希与来源页记录在docs/art/source-records/talulah-v2-resources.json。
- EditMode：61/61、0跳过。新增支撑阶段脚部后移与距离匹配、摆腿离地、周期/接触衔接连续检查；原60模型/配置用例保留。
- GPU PlayMode：16/16、0跳过。实际塔露拉脚部36帧采样有超过20个中间姿态、位移等于原速度×模拟时间；普攻蓄势/挥击/收势武器角度连续。原技能交互用例增加13部件/手部火焰与全场火流暂停位置/旋转/帧一致性；原维维美与遮挡回归保持。
- Windows独立构建：290353067字节、0错误、退出0。运行本地Player验证，不制作ZIP；最终源码提交在Git日志中。
- Player：-talulahSmokeTest 13项、-combatSmokeTest 13项、-interactionSmokeTest 8项全部True、退出0。实际输入适配器/模型回归，非人工键鼠通关。
- -talulahMotionEvidence真实Player捕获36张行走、33张攻击、12张火雨相机图，cast=True/退出0。只由捕获帧编码GIF/MP4，不插造动作帧；行走截取稳定后约一周期。角色预览为诊断放大跟随相机，火雨为原地图相机，均不包含IMGUI HUD，不冒充整屏截图。

已核图：[步态](art/previews/talulah-walk-v2.gif)、[攻击](art/previews/talulah-attack-v2.gif)、[全图火流](art/previews/talulah-fire-rain-v2-camera.png)。原始结果：artifacts/talulah-revision-edit.xml、talulah-revision-play.xml、talulah-revision-build.log、revised-talulahSmokeTest.log、revised-combatSmokeTest.log、revised-interactionSmokeTest.log、talulah-motion.log。首次录像发现蓄势末端跳回中立，修复连续插值并增加实际武器转角断言后复测；初版火流太小，补足尺寸、辉光、残焰和暖光后重新录制。

未测/限制：完整人工键鼠通关、第二机离线、低配和长时性能、所有路线方向的动作自然度待用户评价。当前为固定前侧视造型的程序化分层动画，不含转身、多方向立绘、脚踝/脚趾独立骨骼或复杂布料。火流含48条装饰流与18处残焰，不按装饰数量追加伤害。旧ZIP保持85789690字节、最后修改2026-10-20 20:47:26；本轮不声称旧ZIP已更新，不更新Releases。


## 2026-10-20：塔露拉火雨可玩样板

交付复核：源码提交6028b339235cd0256c68fa881c3499330db8cef8，PR #4已创建、repository-checks工作流39424822689成功；未合并main。该干净源码重新构建，仍为290538195字节/0错误。原目录Builds/TalulahTrial-6028b33.zip（85789690字节）完整解压并核验264个文件SHA256；从解压目录Game真实启动，塔露拉13项、维维美13项交战/8项交互全部True、三次退出0，日志artifacts/下分别为zip-talulahSmokeTest.log、zip-combatSmokeTest.log、zip-interactionSmokeTest.log。后续本条交付说明为纯文档更新，不改变包对应源码6028b33。

环境：Unity6000.6.5f1(3ff58d469c8a)、Windows x64、RTX4060/D3D12；基线main 0dec185，独立工作目录/分支codex/talulah-fire-rain。原目录四项文档修改和未跟踪CONTRIBUTING/PackageManagerSettings不改动。用户明确确认共享战斗、配置接口、表现及InitialDemo绑定修改范围，任务[#3](https://github.com/Peanora/YesterdayConvoy/issues/3)。

- `TalulahTools.Prepare`：真实Unity导入两张PNG/12精确切片，创建单位/卡/战斗参数并绑定主场景；绑定保存后重新打开校验，原场景仅新增talulahDefinition一行，无地图/镜头变动。
- `-runTests -testPlatform EditMode`：60/60通过，0跳过；新增FireRainTests三例，验证射程外三守军同受伤、请求幂等不耗DP、恰好6波18个事件、不重复伤害、暂停、重开/旧run拒绝、全灭后锁定至结束与恢复SP。其余为原模型/配置回归。
- `-runTests -testPlatform PlayMode`（保留GPU）：15/15通过，0跳过；新增实际场景切换、暂停拖放、手动点击技能、0.03慢速、三目标伤害、帧动画暂停与返回原卡栏。原14个场景/表现/交互用例保留，含720/1440遮挡回归。
- `BaselineTools.BuildWindows`：真实Windows构建290538195字节、0错误、退出0。构建目录Builds/Windows含exe/Data/Mono/DLL。最终打包源码提交见包内SOURCE.txt。
- 最终Player `-talulahSmokeTest`：drag/pauseDeploy/ready/slow/cast/global/freeze/locked/allDead/resume/won/reset/original共13项全True、退出0；测试真实Player输入适配器及模型，非人工键鼠全程通关。
- 同一构建 `-combatSmokeTest`：维维美交战13项全True；`-interactionSmokeTest`：暂停部署/冻结/非法位置/就绪/慢速/取消/施放/重开8项全True；均退出0。
- `TalulahTools.Prepare`重复执行：四份新增数据资产及主场景SHA256前后相同，未覆盖人工数值或创建重复资产。
- PNG真实RGBA与Alpha范围0–255，1448×1086按4×3切成362×362；卡面1153×1364。Player通过RenderTexture导出[相机图](art/previews/talulah-fire-rain-camera.png)，已人工检查塔露拉举剑/漫画火焰、三克洛丝高台、地图层级和地面落点。该图不包含IMGUI HUD，不冒充整屏截图；HUD绑定/技能交互由PlayMode与Player输入适配测试覆盖。

中途发现并修复：首次生成后的场景引用未持久保存（首轮PlayMode 14通过/1失败）；改为重新载入资产再绑定，并保存后重开断言，最终15/15。首次编译smoke脚本DistanceTravelled double/float类型错误已修复。隐藏窗口普通ScreenCapture无PNG，CaptureScreenshotAsTexture亦返回空；撤回对通用截图入口的试探改动，改用塔露拉专用相机RenderTexture输出，图像确已存在。首轮尾迹过于接近光条，补充程序化三色火焰形状后重跑GPU测试/构建/Player并核图。

未测/限制：完整人工键鼠胜负流程、第二机离线、长时运行/低配性能、所有未来单位体型、多角色并行施法尚未验收。八帧AI走路仍有局部比例变化；普攻仅两个独立姿势，施法仅两个姿势，非商用品质完整动画。火雨/生命/SP均为本轮试验数值，未做正式平衡。未实现羁绊运营/商店。不会把第三方角色AI同人图登记为CC0。

原始日志/XML：artifacts/talulah-import.log、talulah-bind.log、talulah-edit.xml、talulah-play.xml、talulah-build.log、talulah-player.log、wisadel-combat.log、wisadel-interaction.log；本机artifacts不提交，仅本条摘要与相机PNG入库。公开试玩发布遵循人工main集成后统一发布规则，本轮提供本地功能分支审阅包。


## 2026-10-20：设计分支同步main与文档冲突整理

用户要求保留双方并整理冲突。基于设计提交1056daf，在独立工作目录合入main提交3c5a729（协作规范PR #2）；实际冲突仅为STATUS/QA标题后同位置新增段落。将main的协作规范记录、设计分支三条历史记录逐条保留，补充v0.4最新设计入口，未整文件选择ours/theirs。CONTRIBUTING、AGENTS.template和COLLABORATION保留main版本，GDD/羁绊方案保持设计分支版本。原工程四项已修改文档、未跟踪CONTRIBUTING与PackageManagerSettings不编辑、不暂存、不提交。

本轮检查通过：逐段核对双方674段历史内容全部保留；四份文档37个本地链接存在；冲突标记、暂存差异/空白和repository-checks仓库静态检查通过。main的CONTRIBUTING/AGENTS.template/COLLABORATION与来源一致，设计分支的GDD/羁绊方案未被改写，Assets/Packages/ProjectSettings无变化。Unity编译、EditMode、PlayMode、Windows构建和人工试玩均未运行；仅文档同步，不新增试玩包、不直接合并main。银狼接管及其他新机制仍未实现。

## 2026-10-20：三名开发者的 Codex 协作规范

仅整合AGENTS / CONTRIBUTING及必要文档引用：按用户12条逐项核对，保留原玩法、Unity、验证和发布规则；按后续明确要求改为Agent自动执行新功能Git检查 / main同步 / 开分支及完成后的提交 / 推送 / PR，未提交修改或冲突必须先报告。禁止直接写main或自动合并。文档链接、Git差异与空白检查通过；独立worktree的repository-checks本机静态检查通过（不含Unity编译）；原目录存在上一轮文档修改及原未跟踪ProjectSettings/PackageManagerSettings.asset，已报告并保持原样。Unity编译、EditMode / PlayMode、构建与试玩均未运行（本轮无代码或资源变更）；未验证队友本机AGENTS配置是否完成，不发布新试玩包。用户随后授权推送，本次在最新main的独立worktree只集成本任务文档，原目录与原未跟踪文件保留；功能分支提交并推送、创建PR，不合并main。

## 2026-10-20：纠正怪物/干员误读

用户明确“怪物，维什戴尔为特例”。GDD F12纠正为进攻方采用方舟敌方怪物；v0.2六名干员提案明确撤回，STATUS误将方向记为已定的段落改正。新增v0.3六种敌方单位候选，在线核对PRTS敌人页；费用/标签/技能未确认，不继承错误干员数值。未改代码/素材/配置/场景，未下载资源；仅运行文档/仓库静态检查，未运行Unity或构建，不新增试玩包。

## 2026-10-20：方舟角色提案修订

将用户“角色统一为明日方舟角色”写入GDD F12；v0.2候选名单仍标建议。在线查阅PRTS对应角色页，核对米格鲁防御强化、玫兰莎攻击强化、白面鸮治疗/技力光环等参考，不把改编当原作复刻；未下载素材。明确旧R02—R06技能/数值失效，以及医疗、法伤/法抗、范围伤害和技能触发方式待确认；未改代码、资产、包或场景。仅文档检查，未运行Unity/自动战斗测试/平衡模拟/构建，不重发二进制。

## 2026-10-20：首批怪物与羁绊设计提案

ROSTER-v0.1为设计文档，不是可运行功能。核对现有规则：9张/3合1、同名限编队一张、战前固定/仅最高档、多标签、Gold与DP分离、无自然DP/倒计时、现行停步索敌不变；维维美仍只在试验关。首批3只1费、2只2费、1只3费，概率表4级才解锁2/3费，已注明开局辅助单卡与极小牌库风险；未擅改商店概率或赠卡池。护盾/治疗/友方回SP等为新增建议，并未实现。未运行Unity编译、EditMode/PlayMode、构建或战斗平衡模拟；不生成新试玩包。

## 2026-10-20：多人协作配置

GitHub REST核实仓库只有Peanora管理员、main原无保护；向shigaharuki17/1164571767发送push（写入）权限邀请，邀请ID338658702/338658707，未把邀请当成已接受。Unity当前m_SerializationMode=2、m_Mode=Visible Meta Files，无需改场景或ProjectSettings。新增功能分支/PR协作说明与模板、CODEOWNERS及无Unity许可依赖的repository-checks。

本地检查当前Git跟踪源文件通过；临时目录负例验证缺失.meta和重复GUID会报错，未修改实际Assets。git diff --check通过。未运行Unity编译/EditMode/PlayMode/Windows构建（仅文档、GitHub配置与静态检查脚本，不改游戏代码/资源），未做成员第二机克隆/导入，原试玩包不重发。远端Actions与分支保护实际结果在配置完成后补记。

实际完成：配置提交8eb4134已推送，远端[Actions 39381891937](https://github.com/Peanora/YesterdayConvoy/actions/runs/39381891937) completed/success。main保护REST写入成功：repository-checks且strict=true、1个审批/代码负责人审查/新提交撤销旧审批、讨论解决、线性历史、禁止强推/删除；enforce_admins=false，保留主导者应急路径，未声称管理员也被硬性阻止。检查到1164571767已接受邀请并为write；另一位待最终邀请核对。脚本/文档不增加Unity运行时类：Actions调用tools/check_repository.py只读Git文件与.meta，PR模板和CODEOWNERS连接人工验收与集成；没有配置云端Unity构建。

## 2026-10-20：试玩版发布与交付规则

用户授权今后开发默认同步源码/已验证Windows试玩包。已推送1285e55，AGENTS.md从当前Git索引移除并在.gitignore忽略，本地文件仍存在；未重写历史。GitHub REST实际返回仓库private=false，保持原公开状态，未调用可见性设置接口。创建demo-2026-10-20预发布：上传成功并验证本地SHA256与GitHub返回digest/字节数一致后取消draft，发布地址https://github.com/Peanora/YesterdayConvoy/releases/tag/demo-2026-10-20。

附件YesterdayConvoy-Windows-x64-2026-10-20.zip为83630536字节，SHA256 d639cb803d15a0809f9c861b68d606488df59145c12d203f2f1e9d316a3c0b06。检查ZIP共258项，含exe、UnityPlayer.dll和Data/level0，无AGENTS.md。上一节解压Player战斗13项/交互8项全通过；本轮仅更新包内运行说明及交付文档，未修改游戏二进制、未重建。发布说明明确二进制来自2026-10-19清理后构建、标签对应源码1285e55；后续为文档/交付配置变化。未第二机离线或长期性能核验。

## 2026-10-20：可玩包整理与解压验证

确认Builds被.gitignore排除，上次git push仅同步源工程。将现有2026-10-19清理后Windows构建整理为artifacts/release-2026-10-20/YesterdayConvoy-Windows-x64-2026-10-20.zip（约80MiB），包含完整Game目录、运行说明及更新后的Credits/漫画资源来源说明；未重编译，3578475至e1cf5d4间Assets/Packages/ProjectSettings无已提交差异。实际解压ZIP后运行Player：ENGAGEMENT_CHECK十三项、INTERACTION_CHECK八项全True；交互进程退出0。未完整人工通关、第二机离线核验。文件仅在本地准备，尚未上传GitHub Releases。

## 2026-10-19：实际编辑器空场景排查

- 原窗口标题为Untitled，Hierarchy仅有空场景；查看项目Logs/Editor.log未发现本次运行异常。
- 使用既有Open Current Scene菜单打开InitialDemo，场景校验记录INITIAL_DEMO_SCENE_PASS。
- 在用户正在使用的Unity编辑器内进入Play，实际观察地图、新克洛丝、顶部生命1/1与DP12、底部卡栏。实际拖卡到红门成功部署维维美：场景出现run-1/unit-1，DP12→10、次数1/1→0/1，并观察到沿路线前进。
- 原因是编辑器当前打开空场景；未修改代码/Prefab/场景/美术，不需要新增类或接口。未重跑自动化套件或构建，未完整人工通关。用户继续操作编辑器，保留其当前播放/暂停状态。

## 2026-10-19：旧资产与诊断清理

Unity编辑器由用户保存并关闭后执行。LegacyAssetCleanup迁移保留现有Scene/Prefab/配置GUID，先清场景旧引用和卡牌Prefab旧子物体，保存新版克洛丝与隐藏漫画模板，再计算所有保留序列化资产的递归依赖；有候选仍被引用时直接拒绝删除。实际移除33项资产（13PNG/17材质/2单位样本/Bootstrap）及meta，另清11个旧脚本/测试及meta、3个空目录。完整资产路径在artifacts/legacy-cleanup/deleted-assets.txt，历史可从0fc408a恢复；13个图像清单状态改REMOVED。许可/来源文档保留。模型ManualBattle、EngagementCombat、RouteMotion及当前关卡/卡牌数值、GIF文件与镜头配置未改。

- 迁移：migration.log，LEGACY_CLEANUP_PASS与场景/帧/地图校验通过、退出0。当前场景不再有旧炮台、范围圈、射线和旧棋盘；旧单怪/纸片/Bootstrap运行与生成入口移除。InitialDemo只存绑定，DemoSpriteVisual只读漫画帧，EngagementView复用保存的克洛丝，避免运行时再次生成第二份人物。
- EditMode57/57通过、0跳过、退出0：editmode.xml/log。旧Bootstrap专用校验退役，由当前场景编辑状态引用/唯一入口/退役文件与meta确实不存在的检查接替；其余费用、战斗、技能、单位配置与路线模型测试保留。
- GPU PlayMode首轮和最终均14/14通过、0跳过、退出0：playmode.xml/log与playmode-final.xml/log。旧纸片步态、Bootstrap/单向炮台专用测试随资源退役；保留并移植真实像素遮挡测试到当前漫画角色。最终720：opaque20410/paintBleed0/solidOccluded20410；1440：81506/0/81506；排序均9760。正常帧/暂停/技能续播移动锁、单只胜败、结算按钮、重开及拖动/技能选择回归通过。
- Windows x64构建277913345字节/0错误/退出0：build.log，BASELINE_BUILD_PASS；当前1个单位配置通过。构建前追加未运行场景Camera.Render至editor-preview.png，已实际查看新版克洛丝与地图，无旧人物/炮台；副本docs/art/previews/editor-current-only.png。该图是编辑状态实际相机输出，不含OnGUI，不代表完整HUD人工试玩。
- 更新后的真实Player：combat.log交战十三项、interaction.log交互八项全部True，两个进程均退出0。
- 删除引用复查：从Git提取47个已删除meta的GUID，扫描所有保留unity/prefab/asset/mat/anim/controller/meta，残留引用0；未改变Packages/Unity版本、ManualDemo数值或当前卡牌引用。

未执行：完整人工键鼠通关、第二机离线、长期性能；仅查看编辑态实际渲染，未重新人工验收所有HUD分辨率。本轮测试数量变化来自删除不再存在的诊断，不是把当前失败用例禁用；最终零跳过。原未跟踪ProjectSettings/PackageManagerSettings.asset保留，不纳入提交。外部课程Word未修改；仅本地Git提交，不推送/发布。

## 2026-10-19：单只维维美与胜负结算

正常ManualDemo改为守方生命1、部署次数1，CardDefinition显示名维维美；编辑器新建默认值同步。内部ID/资产路径/源素材名称不改。ManualBattleController新增结果遮罩/面板，显示胜负、守方生命、本场部署与技能次数，无虚构金币/经验；ManualBattleInteraction使用真实IMGUI鼠标事件处理按钮完整点击，结算期间吞掉底层鼠标交互。重开仍走原RestartBattle，清理单位/射击/选择/拖动/提示，恢复费用/次数/生命/技力/暂停/速度。显式旧炮台诊断仍使用6次/3防御历史快照，不把Demo限制推广到模型。

- EditMode57/57通过、0跳过、退出0：artifacts/settlement/editmode.xml、editmode.log，原多实体、技能、事务、穿门与移动锁模型回归通过。
- GPU PlayMode24/24通过、0跳过：playmode.xml、playmode.log。正常场景确认新卡名、生命1、次数1；单只技能击杀并穿门一次获胜，走真实结算按钮按下/松开事件重开，断言完整清理。暂停/冷却后仍不能再部署且不误扣；失败后鼠标不能穿透结算，按下移出/只有松开不能误重开。旧正常入口双实体重叠选中用例替换为单只失败结算用例；目标死后原地播完整技能的真实场景改为单只完成，模型多实体覆盖仍保留。
- Windows x64构建341058541字节、0错误、退出0：build.log，BASELINE_BUILD_PASS；场景/单位/逐帧/地图配置引用通过。无新场景/Prefab/素材/依赖，必要meta不变。
- 最终真实Player：combat.log交战十三项、interaction.log交互八项均True，进程退出0。单只获胜、不放技能失败、暂停、穿门幂等及重开实际可运行。
- 真实窗口人工输入：看到卡名维维美、顶部1/1生命、卡牌1/1次；成功从底栏拖到红门，DP12→10、次数归零；不放技能出现红色失败结算，统计1/1生命/部署1/技能0；实际点击面板按钮恢复准备、DP12/生命1/次数1/干员满血/场上0。已查看完整HUD及失败面板，无文字裁切。第一次原生拖动未生效，点开始后再次拖动成功；未把首次无结果动作记作通过。

未完成：人工单只胜利流程与绿色成功面板观感、第二机离线/长期性能。另尝试隐藏Player带截图参数（screens.log），交战全True但screens目录未产生PNG；不将其当截图或完整视觉证据。自动场景已验证胜利结算/按钮数据和实际行为。保留原未跟踪PackageManagerSettings.asset，本轮不提交该文件、不推送。外部Word报告是先前版本，项目课程Markdown已同步当前参数。

## 2026-10-19：克洛丝待机与界面减字

只改EngagementView视觉偏移、ManualBattleController文字显示与ManualBattleInteraction失败提示连接，扩展真实场景测试；无美术/配置/场景/Prefab/依赖改动。待机2.8秒周期，向屏幕上方0–0.04世界单位，未拉伸图片；准备阶段用视觉时间，开战继承相位，战斗用共享Clock，射击后坐力仍独立叠加。去掉克洛丝名称/类型、正常入口左下角常驻提示，保留血条与功能UI；错误2.5秒居中提示（UI现实时间，不推进战斗），胜负居中，重开清除提示。

- GPU PlayMode24/24通过、0失败/0跳过：artifacts/kroos-idle/playmode.xml、playmode.log。新增准备阶段真实场景待机检查：Clock仍0/无部署，视觉缓慢变化且偏移不超过0.04，逻辑位置不变。原倍速/暂停用例增加战斗起伏变化、方向/幅度、逻辑位置不变、暂停冻结与重开复位断言；原战斗、拖动、技能移动锁等回归通过。
- Windows构建341056621字节/0错误/退出0：build.log，BASELINE_BUILD_PASS；场景/单位/逐帧美术/地图引用校验通过。日志末尾有退出时Curl error42，构建成功标志和进程退出0均已核对。
- 更新后的实际Windows Player交战十三项全部True、退出0：combat.log（ENGAGEMENT_CHECK）；覆盖暂停、远程停步、手动技能、击杀/胜败/穿门幂等/重开。
- 静态核对正常入口不再绘制头顶文字/左下角说明；部署失败与技能失败接到居中短提示。git diff --check通过，未跟踪的ProjectSettings/PackageManagerSettings.asset保持原样且不提交。

未运行：本轮EditMode（战斗模型不变）、人工完整HUD/连续动作观感、第二台电脑、长期性能/各分辨率。自动场景测试和隐藏Player不等于人工试玩或完整HUD截图；起伏幅度/周期尚待用户主观确认。无新素材来源/许可证变化，本地提交、不推送。

## 2026-10-19：克洛丝漫画战场图替换

改动：新增kroos-comic.png与Unity meta，EngagementTools导入入口/默认路径，WisadelVsKroos.asset只有kroosSprite一行引用变化；补充原场景用例对新图/Bilinear的断言。未改Runtime、场景/Prefab、战斗配置数值、维什戴尔或地图。

- 原图检查：1153×1364 RGBA、Alpha0–255、992567字节，输出与项目文件SHA256一致；Alpha>16有效范围(86,67)-(1137,1307)，按原1.8可见高度/脚底Pivot导入。可见边缘完整；极低Alpha像素延伸到画布边缘，不宣称无任何透明噪点。原始最终图及完整提示词见art/GENERATION-KROOS-COMIC-2026-10-19.md。首轮候选细节偏多，未进入Assets。
- Unity资源导入：artifacts/kroos-comic/import.log，KROOS_COMIC_PASS，退出0；仅新图/Bilinear/Single/FullRect/无mipmap/无压缩，配置数值与场景不改。
- GPU PlayMode23/23通过、0跳过、退出0：playmode.xml/log。新增同一场景断言当前DefenderVisual实际绑定kroos-comic/Bilinear；高台脚底/范围/阻挡0、攻击/死亡/穿门/重开、技能原地锁定与其他回归通过。
- Windows x64构建341056961字节、0错误、退出0：build.log；场景/怪物/逐帧资源引用校验通过。
- 最终真实Direct3D12 Player交战十三项全True，退出0：combat.log。
- 已查看实际GPU相机两角色同屏：artifacts/wisadel-gif/camera/walk.png；副本docs/art/previews/kroos-comic-scene.png。是实际Camera.Render场景画面，不含OnGUI/HUD，不是概念图或人工鼠标试玩。也已查看两轮实际image_gen输出。

未执行：本轮EditMode（仅资源/引用/Editor导入）、完整人工键鼠通关、第二机、长期性能/所有尺寸观感；没有新增或验证克洛丝逐帧动画。使用原单姿势+已有射击/受击/死亡反馈，不能声称拥有GIF级新动作。公开素材/角色权利未确认，本轮仅本地原型/本地提交，不推送/发布；原未跟踪PackageManagerSettings.asset保留且不纳入提交。

## 2026-10-19：技能播放期间锁定实际移动

用户澄清覆盖前一轮允许动作播放时逻辑移动的选择。新增战斗模型IsSkillActionPlaying作为共同依据：已发射技能以LastAttackTime+SkillInterval为结束时刻，目标死亡/弹药清零不解除，移动和逐帧显示使用同一个窗口。结束跨步仅推进解锁后的时间，不修改移速配置；暂停无现实时间倒计时。伤害/弹药/技力时机与1.75倍大头不变。

- EditMode57/57通过、0跳过，退出0：artifacts/skill-movement-lock/editmode.xml/log。新增单发即击杀/弹药耗尽用例，验证目标死后位移不变、暂停冻结、结束前0.01秒仍锁定，随后0.04秒只有0.03秒产生位移；原死亡释放阻挡用例更新为先释放阻挡但仍等待技能动作结束。
- GPU PlayMode23/23通过、0跳过，退出0：playmode.xml/log。真实双单位击杀场景改为断言无目标时IsMoving=false、持续推进技能帧而路线距离保持不变；到第99帧仍锁定，动作结束后切Walk且真实位移恢复；暂停/重开与旧战斗/输入/遮挡回归均通过。
- Windows构建341061713字节、0错误、退出0：build.log；场景/逐帧/怪物引用校验通过。
- 最终真实Direct3D12 Player交战十三项全True、退出0：combat.log。没有将自动接口检查称为人工试玩。

未执行：完整人工连续动画/键鼠通关、第二机离线、长期性能。未新增素材/场景/Prefab/依赖，已有未跟踪PackageManagerSettings.asset保留且不提交。本轮没有重新做美术观感验收，1.75倍大头与原资源不变。仅本地提交，不推送/发布。

## 2026-10-19：目标死亡后的技能续播与大头尺寸

只改DemoSpriteVisual的显示优先级与EngagementView表情印章缩放。已发射的技能以LastAttackTime/LastAttackEmpowered及共享Clock保持完整攻击动作窗口，优先于目标/移动显示；100帧/3.2秒不变。特效缩放(.48+age*.32)→(.84+age*.56)，全阶段1.75倍。未改战斗模型、数值、素材/引用、碰撞体或地图。

- GPU PlayMode23/23通过、0跳过：artifacts/skill-followthrough/playmode.xml/log。新增真实场景双单位流程，由技能击杀克洛丝，确认施放者仍活着且已恢复逻辑移动、HasCombatTarget=false时仍为Skill、后续帧继续推进；暂停20秒冻结；在动作结束前0.01秒到第99帧，再推进0.03秒才切Walk；重开清理。原场景用例同时断言放大大头的实际Transform，其他交战/输入/遮挡回归通过。
- Windows构建341061645字节、0错误、退出0：build.log；场景/怪物/逐帧资源引用校验通过。
- 最终实际Direct3D12 Player交战十三项全True、退出0：combat.log。自动检查不是人工通关。
- 已查看实际GPU Camera.Render画面：artifacts/wisadel-gif/camera/skill.png（放大表情），skill-after-target-death.png（干员已消失、维什戴尔继续技能帧）。测试沿用已有相机采图入口；副本为docs/art/previews/wisadel-comic-large-head.png与wisadel-comic-followthrough.png，不含OnGUI/HUD。

未执行：本轮EditMode（纯表现优先级/尺寸）、完整人工连续动画/技能键鼠流程、第二机、长时性能/所有分辨率观感。自身死亡/穿门仍移除角色，不生成独立幽灵动作；终局仍冻结模拟，不为动作延长游戏结算。1.75倍是视觉试验，主观大小待用户反馈。保留原未跟踪PackageManagerSettings.asset，不提交；无新下载/AI美术、未推送。

## 2026-10-19：用户GIF逐帧角色替换与漫画命中

实际新增155张PNG：19走路、1静态待机、28普攻、100技能、1卡面、6表情裁片；新ComicArtTools仅导入/绑定现有CardDefinition帧引用，不重建地图/场景或改战斗配置。原GIF已本地备份且不进Assets；Pillow无损合成GIF处置/透明后转RGBA，无黑底键控、补绘、逐帧重新居中或缩放。全路径/源hash见docs/art/source-records/wisadel-comic.json。

| 实际检查 | 结果 | artifacts/wisadel-gif本地证据 |
| --- | --- | --- |
| 源文件/Alpha | 四GIF帧数19/23/28/100，均50ms；所有导出帧与6裁片Alpha0–255，PNG可解码，保留源尺寸；静态仅用GIF2首帧 | source-report.json、source-contact.png；版本化convert_supplied_wisadel_gifs.py与source-records |
| Unity导入/帧引用 | COMIC_ART_PASS，19/28/100/6引用全齐、固定Pivot/PPU/Bilinear/FullRect/无mipmap/无压缩；全新PNG与脚本由Unity创建meta | import.log，退出0 |
| EditMode | 56/56通过、0跳过、退出0；原六发逐次消耗测试增加最后一发类型保留断言，原暂停/伤害/回转/资源结算通过 | editmode.xml/log |
| 最终GPU PlayMode | 22/22通过、0跳过；新用例检查暂停部署立即显示新静态图、19/28/100帧、步相推进/暂停冻结、无旧Puppet/无flip/无变形、正常/技能帧切换、开火前保持首帧、技能命中表情；原完整战斗/拖动/镜头与720/1440旧遮挡诊断仍通过 | playmode-final.xml/log；首版playmode.xml同22/22 |
| 构建/配置引用 | Windows x64构建341061081字节、0错误、退出0，DEMO_ART_PASS/COMIC_ART_PASS/场景怪物校验 | build.log |
| 最终实际Direct3D12 Player | 交互八项与战斗十三项全部True，两个进程均退出0；未采Player截图 | interaction.log、combat.log |
| 实际视觉 | 已查看最终游戏相机1280×720走路/普攻/技能三图，源透明轮廓和技能翻滚、黑边黄白爆炸/表情印章可见，无旧转身混合；源19/静态/28/100选帧与6裁片接触表也已查看 | camera/walk.png、attack.png、skill.png；副本docs/art/previews/wisadel-comic-*.png |

GPU相机图通过测试的Camera.Render离屏渲染保存，属于实际场景与Sprite/特效输出，不包含OnGUI顶部/底栏，也不是桌面鼠标验收。当前卡面更换由资产绑定与Alpha/源图一致性检查覆盖；完整HUD观感未实机人工查看。旧InitialDemo单怪诊断明确保留旧纸片，它的原“卡牌BattleSprite必须等于旧模板图”断言改为历史贴图非空；新的正常入口用例明确断言暂停出生图等于新Card.BattleSprite，未借历史诊断放宽正常美术绑定要求。

中间检查修正：代码审查发现克隆OnEnable会恢复旧idlePose，补为已绑定逐帧卡时立即用新静态图，新测试覆盖暂停出生；首次技能开火前改为保持首帧，避免沿用上次普攻计时后跳回。最终22项通过后只更新文档/转换工具稳定写入，未再改Runtime。

未执行：全100帧逐帧人工审批、完整人工动画连续播放/拖动/技能通关、第二台电脑离线、多角色/大规模长时性能；本轮没有用自动测试声称主观动作自然或达到原作特效。源5秒技能压缩到3.2秒为纯表现适配，手感待用户试玩。PNG原像素9MB多，未压缩纹理使构建增加86280388字节（约82.3MiB），后续图集/压缩评估未做；不把本次22/22当作性能成绩。无新网络素材下载、无AI生成、无新增依赖；保留未跟踪PackageManagerSettings.asset，不纳入提交。来源权利范围仅本地原型，未推送/公开发布。

## 2026-10-19：血条下蓝色技力与六格黄色弹药

改动仅ManualBattleController新增DrawSkillResourceBar，读取unit.SkillSP/SkillAmmo与CombatSettings.RequiredSP/SkillAmmo；蓝条按比例填充，技能期间六格黄条逐格变暗，耗尽切回蓝条。未修改EngagementCombat结算、场景、Prefab、配置或资源引用，无新素材/依赖。原单向攻击诊断没有技能，不额外显示技力条。

- EditMode56/56通过、0跳过：artifacts/skill-resource-bar/editmode.xml/log。扩展AmmoIsFiniteAndSPOnlyRechargesAfterItIsSpent，从两发改为六发逐次断言6→5→4→3→2→1→0；每次消耗间暂停保持弹药/SP；弹药剩余时不回技力，耗尽后回复并回满。测试专用高生命守军避免正常M01提前击杀，未改正常关卡数值。
- Windows构建254780693字节、0错误：build.log，BASELINE_BUILD_PASS，退出0；场景引用/配置校验通过。
- 实际Direct3D12 Player交互八项全True：player.log，INTERACTION_CHECK；实际战斗十三项全True、进程退出0：combat.log，ENGAGEMENT_CHECK。交互日志中的五次截图失败是隐藏窗口无法抓屏，不冒充截图验证成功。
- 原生鼠标：通过computer-use从准备状态拖卡到红门，实际DP12→10、场上1，并观察到蓝条；后续观察满技力蓝条与头顶就绪提示。角色在再次点击前已死亡，未完成手动技能施放；桌面随后切换至其他应用，未继续抢占焦点。该次结果补充此前拖动未验收状态，不代表所有拖动边界或完整技能流程已人工通过。

未执行：本轮PlayMode（改动为读取状态绘制HUD，核心结算未改）、黄色六格/每发变化/耗尽回转的完整人工视觉验收、第二机与长期压力。没有新增截图；黄色条通过实现审查与真实弹药状态测试，视觉效果仍需用户试玩核对。原未跟踪PackageManagerSettings.asset保留且不提交，未推送/发布。

## 2026-10-19：拖动部署、暂停落卡与角色旁技能

范围：输入与技能HUD小闭环，用户明确暂停合法落卡立即扣费/生成但战斗仍暂停。ManualBattle显式allowWhilePaused只豁免暂停部署拒绝，其余费用/次数/CD/入口/场次与幂等验证保留；旧诊断默认仍拒绝暂停。角色旁面板/头顶标志复用已有CC BY3.0图标。场景唯一改动为skillIcon引用，地图/相机/角色/只读玩法数值不变。

| 实际验证 | 结果 | 本地证据（artifacts/drag-skill-ui，不提交Git） |
| --- | --- | --- |
| Unity导入/绑定 | EngagementTools.Initialize退出0，图标引用落场景，新C#及meta已导入 | setup.log |
| EditMode | 56/56通过、0跳过、退出0；显式暂停事务、重复请求不重复扣费/生成、错误入口、暂停CD不前进/不能绕过 | editmode.xml/log；完成模型改动后执行，后续只改输入表现 |
| 最终真实GPU PlayMode | 21/21通过、0跳过；包括原720/1440遮挡/镜头/交战回归及6项交互测试 | playmode-events-final.xml/log |
| 交互边界 | 暂停落卡只生成一次/错误落点及冷却不花费；面板使共享时钟0.03×、取消恢复2×且不放技能、点击图标只释放一次；完全暂停优先且就绪可见；选中死亡关闭；重开拒绝旧拖动；重叠角色循环；同一模拟帧内分发按下/拖动/松开完成部署 | BattleInteractionTests；最后一项调用与OnGUI共用事件分发器，不是操作系统鼠标注入 |
| 最终Windows x64构建 | 254780509字节、0错误；场景/怪物引用校验通过 | build-events-final.log，BASELINE_BUILD_PASS |
| 最终实际Direct3D12 Player交互 | pausedDrop/onceFrozen/invalid/ready/slow/cancel/cast/reset八项全True，进程退出0 | interaction-events-final.log；-interactionSmokeTest |
| 最终实际Player战斗回归 | invalid/initialSP/paused/stopped/ranged/noBlock/selectionReset/manual/killed/won/gateOnce/reset/lost十三项全True、退出0 | combat-events-final.log；-combatSmokeTest，历史接口兼容检查 |
| 实机视觉 | 已查看1280×720完整技能面板、头顶就绪与暂停落卡；暂停落卡显示DP10、场上1，面板显示SP12/12与0.03× | final-player/ui-skill-focus.png、player/ui-paused-placed.png；面板副本docs/art/previews/drag-skill-focus-v1.png |

真实鼠标试验：正常Player开始/空格暂停后尝试拖卡，未生成/未扣费；随后桌面截图显示焦点已切到其他应用，无法归因于快速边沿漏采或正常失焦取消。未把这次试验记为通过。最终版将鼠标按下/拖动/松开交给OnGUI队列事件，Update只更新预览与快捷键；自动测试覆盖同一帧事件顺序，不证明真实Windows鼠标路径已验收。用户应优先试玩慢拖/快拖、暂停松手和恢复后技能操作。

中间测试保留：playmode-events为无图形设备运行，19通过/1失败/1跳过。失败是测试在OnGUI之外创建Unity Event，type读为Ignore，未送到分发器；改为显式传递事件类型/坐标，OnGUI调用同一分发器；没有放宽部署断言。跳过项是需要GPU的遮挡验证，最终有GPU重跑21/21、0跳过。早期playmode19/19、加入重叠点击后20/20也保留，最终结果以21项为准。

截图在最后事件分发修订前采集，布局/资源与最终版相同；最终Player检查未重复采图，不将旧图称为最终输入版截图。未执行：最终原生鼠标完整流程、完整人工技能胜负通关、第二台电脑离线、超宽屏与长时性能/平衡；640×360小窗口曾出现但低于目标720p，不视为该尺寸可读性验收。保留原未跟踪ProjectSettings/PackageManagerSettings.asset，不纳入提交。未推送/发布。

## 2026-10-19：参照图俯角与轻透视

只改Editor相机设置/对应验证和场景镜头及原四个Sprite朝向/排序，未改Runtime、地图布局/材质、玩法参数或依赖。主入口采用60°/FOV24/距目标24，偏航滚转0，替代52°正交；Bootstrap仍正交。

- Unity配置保存/引用校验退出0：artifacts/camera-reference/setup.log，TACTICAL_MAP_PASS。
- 最终真实GPU PlayMode15/15、0跳过、退出0：playmode-final.xml/log。真实场景检查轻透视/FOV24/60°、前排相同宽度地格比后排大10–30%、红蓝门未落在HUD区内，屏幕→地面往返误差<0.001世界单位；720/1440地面遮挡仍通过。
- Windows构建254503296字节、0错误、退出0：build.log。
- 真实Direct3D12 Player十三项全部True、退出0：player.log；已查看完整准备HUD，新镜头前大后小与更多顶面可见，保存docs/art/previews/waterworks-camera-v2.png。未伪称交战帧已生成。

首次运行14/15：像素遮挡用例旧设置仅改orthographicSize，透视下该设置不生效，角色测试图只有750有效像素，低于原有>1000的取样门槛；paintBleed仍为0。将测试镜头视野转换为距20时半高3的等效透视FOV，保持原取样尺寸/阈值，最终15/15通过，没有修改正常游戏FOV或放宽像素断言。失败结果保留在playmode.xml/log。

未执行：本轮EditMode、完整人工鼠标键盘通关、第二机离线/超宽屏。自动检查覆盖相机射线映射，不等于人工入口点击验收；从一张20:9截图估计的视角不代表取得原作精确相机参数。后续人类观感反馈可继续微调。

## 2026-10-19：地图按用户截图进行层次与材质迭代

只修改Editor地图生成、共享材质、原创静态水面Shader和场景可视结构。保留原路点/相机52°/尺寸4.9、角色/克洛丝高台脚底/所有玩法配置；无新碰撞体、导航或依赖。13份新材质复用原贴图，所有新资产带Unity生成meta。

| 实际验证 | 结果 | 本地证据（artifacts/map-polish） |
| --- | --- | --- |
| Unity生成/配置检查 | refined退出0，TACTICAL_MAP_PASS；无碰撞体、原路线端点未抬高、相机正向/全画幅、引用/材质检查通过 | setup-refined.log |
| Windows x64构建 | refined退出0，254503296字节，0错误；无Shader error或C#编译错误 | build-refined.log |
| 实际Direct3D12 Player | refined退出0，ENGAGEMENT_CHECK十三项全True：范围、不阻挡、每次选牌、SP/技能、暂停、胜负/穿门幂等/重开 | refined-player.log |
| GPU PlayMode | 最终15/15通过、0跳过，退出0；真实场景交战、部署/暂停、步态和720/1440遮挡仍通过 | playmode.xml / playmode.log |
| 遮挡像素 | height720：paintBleed0/前景遮挡1436；height1440：paintBleed0/前景遮挡5745，角色排序不受分辨率影响 | playmode日志GROUND_SORT_PIXELS |
| 实机画面 | 已查看最终准备/胜利/失败完整HUD，地块/高台/水渠/支墩/人物与入口可读；主交战截图仍未落盘，未伪称检查该帧 | refined-player/combat-ready.png、combat-won.png、combat-lost.png；ready副本在docs/art/previews/waterworks-map-v1.png |

首次样板已成功生成/构建/Player，但视觉检查发现地面偏亮、水面过平；后续降低材质/环境光、添加程序水面与外围支墩，最终验证针对该细化版。原截图仅作构图/层次研究，没有把商业游戏像素复制入Assets。新增程序化水面使用静态世界坐标，不随现实时间推进，未引入暂停中持续流动的新动画。

未运行：本轮EditMode（核心逻辑/参数未改）、第二台电脑离线、完整人工点击通关、长时间压力/FPS、非16:9和低端GPUShader兼容测试。自动测试不证明达到原作美术质量；建筑/磨损/道具种类仍为原型，最终主观观感待人类反馈。新增约数百个静态可视模块，有长期性能评估余地；不以当前成功构建声称已达60fps目标。

## 2026-10-19：远程、高台、选牌与初始费用反馈

范围：配置改为初始DP12、普通射程3.2/技能3.8、克洛丝高台(-1,0.35,1)/阻挡0；成功部署清选牌，失败保留。未改原通用范围/阻挡算法、移动速度、路线、卡面或依赖。

| 实际验证 | 结果 | 本地证据（artifacts/ranged-feedback） |
| --- | --- | --- |
| EditMode | 55/55通过、0跳过，进程退出0 | editmode.xml / editmode.log |
| GPU PlayMode | 15/15通过、0跳过，退出0；真实场景断言射程一致/技能不缩短、首发在3世界单位外、全程不阻挡、克洛丝脚底高于实际平台；新增一次部署后须重选/重复点击不扣钱、失败保留选择 | playmode.xml / playmode.log |
| Windows构建 | 253842945字节、0错误、退出0；场景/配置引用校验通过 | build.log |
| 实际图形Player | invalid/initialSP/paused/stopped/ranged/noBlock/selectionReset/manual/killed/won/gateOnce/reset/lost十三项全True，退出0 | player.log |
| 历史单向攻击兼容 | 部署诊断九项全True、退出0；测试按当前12费/六次和每次选牌操作更新 | deployment-regression.log |
| 画面 | 已查看真实Player准备、胜利、失败1280×720完整HUD，克洛丝站高台、12费、无阻挡说明正确；交战帧没有落盘，未作视觉证据 | player/combat-ready.png / combat-won.png / combat-lost.png |

原有顺序送兵失败测试不再断言“失败时守军一定存活”：远程普攻可逐步伤害并最终击杀她，但剩余可穿门实体仍不足3，照常失败；这是用户要求的远程交战产生的结果，不为维持旧断言调整攻击/生命。暂停、独立SP、技能手动/幂等、击杀继续走、穿门与重开回归均通过。原GPU720/1440遮挡回归仍在15项中。

未执行：完整人工鼠标键盘胜负通关、第二机离线、非16:9和长期压力/平衡；本轮没有新的完整交战视觉帧。Player脚本通过同一控制器显式重新选牌/部署，不能冒充人工点击验收。未生成新美术、未推送或发布；原未跟踪PackageManagerSettings.asset保留。


## 2026-10-19：M01克洛丝交战与手动技力

目标：在原部署闭环中完成范围交战/阻挡/干员死亡继续走，并通过各实体独立技力、手动六发炮击改变突破结果；保持原地图、行走和0.75速度。新配置/场景/两图已由Unity导入保存meta，无新依赖或引擎升级。

| 实际验证 | 结果 | 本地证据（不提交Git） |
| --- | --- | --- |
| 配置导入/场景绑定 | 退出0；ENGAGEMENT_CONFIG_PASS，Sprite与音效引用有效 | artifacts/engagement/initialize-final.log |
| 最终EditMode | 55/55通过，无失败/跳过；新增9项覆盖阻挡溢出/释放、停走、最低伤害、SP独立/手动/幂等、暂停、弹药用尽、死亡取消、重开旧请求 | editmode-final.xml / editmode-final.log，同目录 |
| 最终真实GPU PlayMode | 14/14通过，0跳过；新增3项真实场景验证技能杀干员后继续走/胜利/重开、统一时钟2倍速与暂停、无技能逐只部署失败；旧720/1440遮挡/步态回归仍通过 | playmode-final.xml / playmode-final.log |
| Windows x64 Development/Mono | 构建成功253842305字节、0错误，进程退出0 | build-final.log，BASELINE_BUILD_PASS |
| 真实图形Player | Direct3D12，退出0；invalid/initialSP/paused/stopped/manual/killed/won/gateOnce/reset/lost十项全部True | player.log，ENGAGEMENT_CHECK |
| 旧部署/动画诊断兼容 | 分别9项和5项全True、各退出0；显式诊断路径，不等同正常交战入口 | deployment-regression.log / animation-regression.log |
| 完整HUD图像 | 可见Player采集ready/fighting/won/lost四张1280×720，已逐张查看：原炮隐藏、克洛丝/攻击图/血条/每实体SP弹药/顶部资源及终局可读 | player/combat-*.png；交战图复制到docs/art/previews/engagement-v1.png |
| 实际窗口输入 | 已点击/按键验证1选牌、红门扣费生成、空格暂停恢复、暂停按E拒绝且SP保持、2倍速、静音、重开恢复8DP/3防/清实体/1倍速 | 本轮Computer Use截图观察；无独立输入录像 |

首次GPU PlayMode为13/14：失败用例终局已自动取消选牌，直接TryDeployAt先返回“未选牌”，测试却期待模型Finished。补足SelectCard前置条件后14/14通过，没有为迁就断言改玩法。初次额外Edit/构建启动命令未等待Windows GUI进程，造成工程占用、构建退出1；随后改用等待进程顺序运行，最终Edit55和build-final均成功，失败日志保留。没有把失败运行列为成功。

关键连接：配置快照只读；ManualBattle部分实现EngagementCombat持有单位/干员/弹道、SP/弹药；每个请求与每发弹道只结算一次，源/目标死亡取消未生效效果。Controller将部署/实体选择/E键和HUD接到同一接口，EngagementView只按Battle.Clock呈现弹道/爆炸/受击/死亡与音效。暂停同时冻结SP/移动/攻击/CD/特效，2倍速整体倍乘模拟delta。

未执行/限制：未完成纯人工一整局技能通关；窗口按E时两次单独部署的实体已被击杀，观察到明确无目标提示，不据此伪称人工成功开技能。完整手动技能流程由真实Player脚本/场景测试验证。失焦暂停代码已接，未做跨应用失焦往返的专项交互验收；第二机离线、正式关间流程、长期压力/FPS、非16:9未测。基础SFX已导入并绑定，静音UI已试，不宣称音频听感/混音/BGM验收。攻击/死亡是原型关键姿势与程序反馈，非商业完整动作集；同人权利状态不变。

复核：关闭同目录编辑器，用Unity6000.6.5f1执行EngagementTools.Initialize/常规Edit与Play测试，再BaselineTools.BuildWindows。当前主Player参数为`-combatSmokeTest`，可选`-demoEvidenceDir <绝对目录>`；旧`-deploymentSmokeTest`和`-demoSmokeTest`只作回归。ScreenCapture完整HUD需可见窗口，不能用未查看黑图当视觉证据。

## X/B规则回填与第5周课程材料（2026-10-18，仅文档）

只读解析用户指定DOCX的正文/表格文本，核对原文明确本次仅要求开发准备，不要求课前完成可玩原型/HUD/小地图。创建course/WEEK5_PREPARATION.md，区分设计循环/实际单卡循环、已有自动测试/待人工演练、干宸骅实际主导/另两名成员待执行分工；第4周反馈写未收到。用户确认10月20日验收，覆盖原未知日期，不编造成员成果。原DOCX未修改，未生成或验证Word/PDF排版。

回填X01–X05/B01–B10与E01/E06信息到GDD/AGENTS/DECISIONS/填写表，另记经济设计约束，明确失败基础奖/重试防刷和常规局不买空卡池仍需方案与模拟。核对现有ProjectVersion、入口场景/试玩exe存在，阅读已存QA；本次是文件/文档核对，**未运行Unity、Edit/Play Mode、构建或人工试玩**，不把以前46/11的结果算本轮新验证。代码/场景/Prefab/包/美术不变，无新增类或运行数据流。文档检查git diff --check；原未跟踪PackageManagerSettings.asset保留，不进行新推送。

## A组规则同步与GitHub推送（2026-10-18，仅文档）

逐项核对用户A01–A10答案，更新AGENTS/GDD/DECISIONS/填写表/STATUS，并新建后续详细问项。已明确区分已答、暂定、未覆盖边界与委托数值；不可拆塔和单门限制被覆盖，不将停止攻击/复活/索敌顺序/多门防御等未答细节静默定案。无新增类或代码/场景/Prefab/美术/依赖改动，**未运行Unity、测试或构建**；当前游戏仍是上次手动部署版本。文档检查使用 `git diff --check`，原未跟踪PackageManagerSettings.asset排除。按本轮明确授权推送现有私有仓库main；实际推送与远端提交核验见本轮执行结果，不把源码推送等同于发布试玩包或验证新玩法。

## 规则填写表整理（2026-10-18，仅文档）

阅读 AGENTS、当前 STATUS、GDD、DECISIONS、商店提案、T01/T02 和实际 UnitDefinition/CardDefinition/手动关卡代码，汇总后续依赖到 `PLANNING_CHECKLIST.md`。区分已定规则、现有单卡实现、占位配置与未获确认的建议；重点标出5费卡身份/正式定位、升星“数量”的语义、短流程与十级经济、少量卡池概率的取舍。人工核对编号、填写空栏、引用和建议状态。仅修改文档，代码/场景/Prefab/资源未改，**未运行新的 Unity 测试或构建**，不重复声称上一任务的46/11项是本次验证；无新增类或运行数据流。原未跟踪 PackageManagerSettings.asset 保留。

## 最新：单卡手动部署与胜负重开（2026-10-18）

唯一验收目标：让当前单种怪物 Demo 成为可以手动分批部署、打赢或失败并重新挑战的一关。新增 ManualBattle 纯状态模型、只读 ManualBattleDefinition、场景/输入适配 ManualBattleController、幂等 ManualDeploymentTools、ManualDemo 关卡资产及两组测试；修改 InitialDemo 场景绑定、DemoCardView、DemoSpriteVisual 和旧测试显式诊断入口。地图/相机/角色 PNG/Prefab 源资产、Unit/Card 既有配置和包依赖不改。原未跟踪 PackageManagerSettings.asset 保留，不提交。

试验配置为 DP8、防御3、部署费2、次数6、CD2秒、生命100；招募费仍5，移速仍0.75。GDD 已标明与旧多卡草案不同，未把试验平衡登记为人类确认。四次紧凑部署和四次逐只送出分别覆盖胜利与失败；费用耗尽但仍有单位时继续结算，可支付且尚有次数时空场等待，无自然回费/超时判负。

本轮证据目录为 `artifacts/manual-deployment`（本地忽略，不把日志/构建加入源码）：

- `initialize.log`：真实 Unity **6000.6.5f1** 执行场景绑定和配置检查，退出 0，`MANUAL_BATTLE_CONFIG_PASS`，新资产与脚本 meta 已生成。
- 最终 `editmode-final.xml`：**46/46 通过、0 失败**，含本轮 12 项部署模型测试：失败不扣资源、重复请求只扣一次、费用/次数边界、入口占用、多实体独立状态、CD 等待、暂停下移动/攻击/CD 冻结、最后单位结算、无自然回费/超时、重开与旧局请求、塔目标和非法配置/时间。暂停测试在实际处于塔射程并已开火的状态下冻结时钟。
- 最终 `playmode-delivery.xml` / `.log`：带真实图形设备 **11/11 通过、0 跳过**。本轮两项真实场景测试验证正常入口无自动单位、选牌/错误位置/独立实例、部署费与招募费区分、刚出生步态从零过渡、暂停、四次部署胜利、资源耗尽等待、逐只送出失败、完整重置及资产值不受运行影响。原 9 项动画/移动/真实 GPU 遮挡回归显式启用旧单怪诊断；720/1440 地面穿透仍为 0，正常前景实体遮挡保留。
- 最终 `build-final.log`：场景/美术引用/怪物及手动关卡校验通过；Windows x64 构建 **241208220 字节、0 错误**，无 C# 编译错误/警告。`Builds/Windows/YesterdayConvoy.exe` 更新。
- 最终真实 Player `player-final.log`，`-deploymentSmokeTest`，退出 **0**：`invalid/cooldown/paused/deployed/waitForUnits/won/gateOnce/reset/lost` 九项全 True。同一最终构建的旧 `-demoSmokeTest`，`legacy-player.log`，`breach/gateOnce/killed/restart/default100Killed` 五项全 True，退出 **0**。
- 首个可见功能构建完整 HUD 截图 `player/manual-ready.png`、`manual-multi.png`、`manual-won.png`、`manual-lost.png` 已逐张查看：初始空场 DP8/防御3、四单位 DP0/剩余次数2、三次突破防御0、逐只送出失败；字形、材质、CD/费用/次数可见。多单位图复制为 `docs/art/previews/manual-deployment-v1.png`。这批截图早于最后的出生步态计时、费用数字纵向位置与空格焦点保护修正；后续构建与测试通过，但未重新采集完整可见 HUD，不声称截图是最终逐像素版本。

原始及最终测试均保留，所有实际执行套件通过。本轮新增空格事件消费，避免 OnGUI 焦点按钮和 Update 同时响应造成重开/重复动作；目前仅编译和场景回归通过，未完成人工键盘焦点验收。实机自动检查使用正常部署/重开公共入口，但不是自动点击输入；完整鼠标/键盘逐项验收、第二机离线、非16:9、多种怪物/大量单位性能 **未运行**。打开正常试玩窗口后观察到选牌/部署画面，用户正在操作，未继续接管或据此声称人工全流程通过。

数据连接：`ManualBattleDefinition.Snapshot()` 只读卡与单位数据，`ManualBattle` 独占费用、次数、冷却、塔攻击、实体身份和胜负；`ManualBattleController.TryDeployAt()` 转换输入并同步视图/反馈，`DemoSpriteVisual.Bind()` 将独立实体运动及同一模拟时钟送入原纸片动画。碰撞体不随动画改变，无新动态寻路。旧 InitialDemo 保留场景引用与显式历史诊断，普通运行由新控制器接管。

## 最新：行走时地面标记与高台穿透角色（2026-10-18）

改动范围：`DemoSpriteVisual.cs` 的 GetSortingOrder；`DemoArtTools.cs` 和 `TacticalMapTools.cs` 共用该排序，黄/青两种地面材质改不透明，装饰高台 0.48→0.30；InitialDemo 场景更新相应排序/高台尺寸，`InitialDemoTests.cs` 增加一项真实 GPU 像素回归。未修改动画姿态、逻辑坐标、速度、路线、配置、Prefab 源资产、HUD 或依赖；原未跟踪 PackageManagerSettings.asset 保留。

- 原代码实测复现（`artifacts/ground-sorting/repro.xml` / `repro.log`）：720 高度人物排序 2624，1499 个不透明像素无穿透；1440 高度排序变为 **-2752**，5947/5947 个人物像素被后方黄色面覆盖。是实际 GPU 读回结果，不是仅比较属性。
- 首次修正归一化排序与涂装材质后，简化遮挡测试通过，但 `player/walk-16.png` 的实际场景仍有高台顶面切脸。没有以这次通过结束任务：将装饰高台降至 0.30，回归测试加入出错路段的实际 Cell / Platform cap / Warning lip 三个对象。
- 最终 Unity **6000.6.5f1**，带真实图形设备运行 **Play Mode 9/9、0 跳过**，`playmode-final.xml`；不使用 `-nographics`。空图形设备时该像素测试会明确 Ignore，不能当作渲染验证通过。其余 8 项原移动/转身/暂停/结算测试通过。
- 最终像素证据 `playmode-final.log`：720 高度，不透明像素 **1436**、穿透 **0**、前景实体遮挡 **1436**；1440 高度分别 **5745 / 0 / 5745**，两次人物排序均 **9760**。保留正常 ZTest，未使用 ZTest Always 或把人物强制画到全部实体之上。
- 场景/资源/怪物配置校验通过，黄/青涂装验证要求不透明队列；最终 Windows x64 构建 **241179083 字节、0 错误**，无 C# 编译错误/警告，`build-final.log`。
- 最终真实 Direct3D 12 Player 退出 **0**，`player-final.log` 的 `breach/gateOnce/killed/restart/default100Killed` 全 True。以 2560×1440 窗口参数启动；相机采集函数仍输出固定 1280×720，严格的双分辨率覆盖以以上 GPU RenderTexture 测试为准。
- 已查看 `player-final/walk-16.png`、`down-16.png`、`turn-48.png`，人物面部不再被后方黄条/高台横切；相机图不含 OnGUI，隐藏窗口的完整 HUD 黑图不作有效视觉证据。

连接关系仍为 RouteMotion → DemoSpriteVisual → DemoWalkPuppet；只修正表现排序，纸片关节沿用原输入。地图涂装按真实地表深度绘制，高台仅改变装饰几何，不参与碰撞或寻路。本轮未运行 Edit Mode、鼠标 HUD 重测、第二机、多单位/其他镜头高度验证；今后提高高台或改变俯角需重跑这项实景遮挡测试。

## 最新：全画幅地图与战斗 HUD 布局（2026-10-18）

玩家可验收目标：地图铺满战斗背景、减轻低俯角造成的压扁感，上方显示真实防御、下方放现有角色卡与进攻操作。固定《明日方舟》相近地图方向和此次布局反馈写入 GDD/AGENTS；52°/正交尺寸 4.9 是试验实现值，不是人类指定的精确相机参数。

主要修改：`TacticalMapTools.cs`、`DemoCardView.cs`、`InitialDemo.cs`、`InitialDemo.unity`、`InitialDemoTests.cs`、五个既有材质与两个新增材质（Yard / Contact Shadow，meta 齐全）；同步 STATUS、美术规范/进度/清单、README。卡面/角色图片、单位数值、路线、包清单和 Prefab 源资产未改。旧墙/栏杆实例和左侧卡牌 Renderer 仅禁用，保留资源引用。原未跟踪 PackageManagerSettings.asset 保留、不提交。

- Unity **6000.6.5f1**，本轮 **Edit Mode 34/34、Play Mode 8/8**，`artifacts/battle-layout/editmode.xml`、`playmode-final.xml`。既有闭环新增相机角度、视口四角地面覆盖、旧左侧卡牌隐藏检查；原移动/半速/转向/暂停/结算回归通过。
- 首轮 Play Mode **7/8**（`playmode.xml`）：旧抬脚断言直接要求世界 Y 抬高 0.04，俯角改变后视觉平面抬脚的世界 Y 投影减小。改为沿视觉节点 up 检查抬脚 >0.04，同时保留脚高于实际地面的断言；动画实现/步相未改。失败记录保留，没有降低动作数值以迎合镜头。
- 上述测试后仅修正血条沿相机 up 定位、开始按钮对比度、卡片上移 8 像素避开开发版水印；未重复与这三项 UI 细节无关的完整测试套件，而在最终构建/实际鼠标流程复核。
- 场景/美术/怪物配置校验通过，包含 `TACTICAL_MAP_PASS`（全视口地面、正向 52°、无新增地图碰撞体、材质有效）。最终 Windows x64 构建 **241178685 字节、0 错误**，`build-delivery.log`，无 C# 编译错误/警告。
- 最终构建另运行真实 Direct3D 12 Player 自动回归，退出 **0**，`player-final.log` 五项全部 True：突破、穿门单次结算、30 生命死亡、重开、默认 100 生命死亡。这次未传采图参数；完整 HUD 视觉证据另见可见窗口记录，不能用隐藏窗口渲染替代。
- 可见图形 Player 的完整 ScreenCapture 在 `player-visible/hud-ready.png`、`hud-action.png`、`hud-breached.png`：实际防御 8/8→7/8、角色名/招募费 5、底栏/按钮/血条可见。此批图在最后卡片上移 8 像素之前采集；最终窗口只读截图保存为 `docs/art/previews/battle-layout-v2.png`（含窗口边框，652×397）。不是 AI 概念图。首次隐藏窗口 ScreenCapture 为黑图，未作有效视觉证据；保留失败图以区分相机 RenderTexture 与完整窗口截图。
- 最终构建已用实际鼠标验证：拖动生命滑块到 **34** → 开始进攻 → 按钮/滑块在运行中禁用 → 三次塔攻击后失败，防御仍 **8/8**；拖至 **140** → 点击重开 → 角色重新从红门出发 → 八次塔攻击后突破，顶部 **7/8**，显示扣除 1 点防御。`mouse-playtest.log` 保存同一实际交互的 `DEMO_DEFEAT: shots=3; gate=8` 和 `DEMO_BREACH: shots=8; gate=7; health=12`。最终画面卡名不与开发版水印重叠。用户随后开始操作窗口，停止接管输入并保留该窗口。

数据连接：`TacticalMapTools.Apply` 仅读原 route，生成延展地面/同材质墙体/接地阴影并设置镜头；逻辑移动与碰撞规则不变。`InitialDemo.OnGUI` 从 GateDefense 绘制顶部防御，并调用 `DemoCardView.DrawRoster` 读取原 CardDefinition 画底栏。开始/重开仍调用同一个 BeginAttack，招募费 5 仍不代表部署扣费。

未运行：第二机离线、非 16:9/超宽屏、多单位性能、正式多卡部署和人类最终风格验收。周边场地仍为模块原型，角色同人素材权利状态不变；不将鼠标闭环或技术测试通过等同于风格已获认可。

## 最新：固定地图美术方向与工业场地（2026-10-18）

唯一玩家可见目标：把当前 Demo 地图替换为与《明日方舟》相近的正向斜俯视工业战术场地，并把方向写成不可随意替换的规则。GDD/AGENTS/美术规范同步；不以测试通过代替人类风格认可。

主要改动：新增 `Assets/_Game/Editor/TacticalMapTools.cs` 与 12 个外部资源、10 个共享材质及必要 meta；修改 `DemoArtTools.cs`、`InitialDemo.unity`、既有 `InitialDemoTests.cs` 与文档清单。源站/原包许可/作者/具体文件均记入 art 文档。运行时代码、单位配置、角色/卡牌 Prefab、包清单未改；原未跟踪 `ProjectSettings/PackageManagerSettings.asset` 保留、不提交。

- Unity **6000.6.5f1**：本轮 **Edit Mode 34/34**（`artifacts/map-style/editmode.xml`）；最终 **Play Mode 8/8**（`playmode-final.xml`）。既有进攻闭环检查增加地图根节点/无碰撞体/旧门隐藏与新线框门断言；其余移动、半速、暂停、重开回归通过。Edit Mode 在最后一轮灯光/墙朝向调整之前运行，最终 Play Mode、构建和 Player 在调整后重跑。
- 首轮 Apply 因 LineRenderer 的 Unity 空对象判断抛出 MissingComponentException（`apply.log`）。改为明确 `== null` 判断后生成成功；后续再次 Apply 原名复用节点，修正过亮灯光、墙/围栏端面朝向，`apply-polished.log` 退出 0。失败记录保留，未计入成功。
- 最终场景/美术引用、怪物配置检查通过，含 `TACTICAL_MAP_PASS`；Windows x64 构建 **241116085 字节、0 错误**（`build-final.log`），无 C# 编译错误/警告。
- 最终真实图形 Player **Direct3D 12** 退出 **0**（`player-final.log`）：`breach=True; gateOnce=True; killed=True; restart=True; default100Killed=True`。140 生命突破、单次穿门、30/默认 100 生命死亡与重开均保持原结果。
- 已查看最终实际相机 `player-final/demo-action.png`，复制到 `docs/art/previews/tactical-map-v1.png`：无紫色丢材质，方格/高台/入口可辨，原卡牌/角色保留。烟雾采集同时生成 walk 32、down 32、turn 96、start-stop 66 帧；本轮不据此新增“动画观感通过”结论。相机截图不含 OnGUI，不能证明鼠标 UI 已人工操作。

连接关系：`TacticalMapTools.Apply` 读取 InitialDemo 的 waypoints，仅构建场景表现；所有新地形/设备无 Collider，不参与寻路和伤害。`DemoArtTools.Validate → TacticalMapTools.ValidatePresentation` 校验材质/路线端点/无碰撞体，构建沿用既有校验链。运行时仍为 `InitialDemo/RouteMotion → DemoSpriteVisual → DemoWalkPuppet`。

未运行：完整鼠标 UI 流程、第二机离线、多单位性能与人类最终风格验收。当前无完整城市远景；原角色同人素材的分发限制依然适用，CC0 环境许可不改变角色许可。来源包和日志留在忽略 Git 的 artifacts，提交仅包含实际使用资源与必要授权文档。

## GitHub 仓库建立与源码上传（2026-10-17）

用户明确授权创建仓库并上传；GitHub 账号核验为 Peanora，已创建私有仓库 `https://github.com/Peanora/YesterdayConvoy`，本地 `origin` 指向该地址，使用 `main` 上传当前工程和提交历史。保留回退前提交历史，不重写历史、不上传构建或创建 Release。

本轮检查：当前工作区只有原未跟踪 PackageManagerSettings.asset；跟踪文件中无 Library/Temp/Logs/Builds/artifacts、私钥文件或常见令牌模式；全部历史最大 Blob 约 8.3 MB，无需 Git LFS。README 的 Unity 版本、入口场景、构建入口、100/140 生命结果与当前工程核对，修改仅 README/STATUS/QA。上传完成核验使用远端 main SHA 与本地 HEAD 比对，并读取 GitHub 仓库可见性。

本轮**未运行** Unity、Edit/Play Mode 或构建：无运行代码、资源或场景变化，沿用前节回退任务的实际验证；未进行 GitHub 下载后在第二机导入。原未跟踪文件不纳入上传。

## 最新：恢复 e7e88c3 工程快照（2026-10-17）

目标为本会话上一交付 `e7e88c3`（右向恢复三分之四正面）。回退前 HEAD=`397b4d6`，已建备份分支 `backup/before-rollback-397b4d6`。以目标快照恢复全部已跟踪文件，后续分层/灰焰/预览及修复退出当前工程；以新的回退提交保留全部历史，未使用 reset --hard、clean 或清空未跟踪文件。原 `ProjectSettings/PackageManagerSettings.asset` 保留。

- 恢复后及 Unity 验证后执行 `git diff --exit-code e7e88c3 -- Assets Packages ProjectSettings AGENTS.md`，均退出 0；工程文件与目标一致。文档仅 STATUS/QA 另增回退记录。
- Unity **6000.6.5f1**，本次重新运行 **Edit Mode 34/34、Play Mode 8/8**，无失败；日志/XML 在 `artifacts/rollback-e7e88c3`，不是沿用历史测试结果。
- 场景/美术引用、怪物配置校验通过；Windows x64 构建 **222722868 字节、0 错误**，无 C# 错误/警告，`build.log`。重新生成当前试玩包，恢复后的 Credits/来源记录同步到包目录。
- 真实图形 Player 退出 **0**，`player.log`：`breach=True; gateOnce=True; killed=True; restart=True; default100Killed=True`；对应 140 生命突破、单次穿门、30/100 生命死亡与重开。未新增或重做美术素材。

恢复的连接为 `InitialDemo/RouteMotion → DemoSpriteVisual → DemoWalkPuppet`，表现直接使用原纸片关节，右向正面、0.75 移速与原转角逻辑均回到目标版本。AnimationLab/漂浮样板不在当前构建入口。完整鼠标交互、第二机离线与多单位性能未测；原有裁片接缝和方向造型差异不属于此次回退修复范围。

## 最新：恢复向右正面行走（2026-10-17）

按用户要求，仅修改 `DemoWalkPuppet.cs` 的右向展示角和正面裁片横向站位；`InitialDemoTests.cs` 对应改为检查原正面步态，同时确认转角仍经过侧面/斜背面。DemoSpriteVisual 继续传入原运动数据，实际移速 0.75、纵深步态修正、转向状态与战斗结算不变。未改 PNG、场景、Prefab、配置或包，清单更新侧面图用途。

- Unity 6000.6.5f1，**Play Mode 8/8**，`artifacts/right-pose/playmode.xml`：正面支撑脚/水平鞋底/连续关节、半速、向下对称、转角中间图、暂停/重开与进攻流程通过。本次未重跑 Edit Mode；此前 34 项通过属于上一轮证据。
- Windows 构建 **222722869 字节、0 错误**，无 C# 错误/警告；场景/美术引用与怪物配置校验通过，`build.log`。
- 真实图形 Player 退出 **0**，`player.log` 五项全部 True：突破、穿门单次结算、低血量死亡、重开、默认 100 生命死亡。
- 重新采集实际相机画面并查看 `walk-contact.png`，确认稳定右向使用原正面造型；`walk-closeup.gif` 为 32 张真实帧，约 0.85 秒一轮。沿用采集入口同时保留 down/turn/start-stop 帧与 GIF，未修改源美术。

完整鼠标交互、第二机离线、多单位性能未测；裁片接缝与图间造型变化仍待观感验收。原未跟踪 `ProjectSettings/PackageManagerSettings.asset` 保留、不提交。

## 最新：半速、向下步态与转身反馈（2026-10-17）

用户明确选择实际前进速度和腿部动作均慢一倍。U01.moveSpeed 从 1.5 改为 0.75，Stride 保持 0.64，步周期约 0.853 秒；路线、塔伤害/间隔、生命滑块和全局时钟不变。减速增加射程内停留时间，未私自调整数值抵消：真实 140 生命突破，默认 100 与 30 生命死亡，重开正常。

主要修改：`Data/Units/U01.asset`；`DemoWalkPuppet.cs`（纵深步幅/共同落点、四方向 Rig、头身分时转向、换步）；`DemoSpriteVisual.cs`（两个新方向引用）；`DemoArtTools.cs`（导入/绑定/校验）；`InitialDemo.unity`（新增引用，原 GUID 保留）；`InitialDemo.cs`（采集与新增默认血量烟雾检查）；`InitialDemoTests.cs`。两个新 PNG 与 meta、清单/许可/生成记录已加入，原卡面和其他美术原文件未改，未引入 Unity 包。

- Unity **6000.6.5f1**。Edit Mode **34/34**（`artifacts/animation-feedback/editmode.xml`）；最终 Play Mode **8/8**（`playmode-final.xml`）。本轮无失败测试。
- 新增完整向下步周期检查：64 个采样，左右相差半周期时接触轨迹一致（误差 <0.003 世界单位）；转向经过侧面/斜背面中间方向、只有一颗可见头、材质不透明，结束显示背面。既有支撑脚、抬脚、关节长度、连续动作、暂停/转向冻结、重开、进攻流程继续通过；断言实际移速 0.75，并增加默认 100 生命死亡结果检查。
- `DemoArtTools.Apply` 成功，场景新引用已保存；构建时 `DEMO_ART_PASS` 和怪物配置检查通过。最终 Windows x64 构建 **222722285 字节、0 错误**，无 C# 错误/警告，`build-final.log`。
- 最终真实 **Direct3D 12 Player** 退出 **0**，`player-final.log`：`breach=True; gateOnce=True; killed=True; restart=True; default100Killed=True`。140 用于突破；100/30 用于默认/低血量死亡。测试不把“更慢但受更多攻击”的结果隐藏为动画问题。
- 两个新增 PNG：侧面 1145×1374、斜背面 1145×1373，均 RGBA、Alpha 0–255；原样复制生成源，Unity 使用有效高度 1.65、Point、Single、脚底 Pivot。实际输入/提示词见 art/GENERATION-2026-10-17.md；非正式分层素材。
- 真实相机帧与四个 GIF 位于 `artifacts/animation-feedback`：`walk-00..31.png`、`down-00..31.png`、`turn-00..95.png`、`start-stop-00..65.png`。已查看侧向/向下/两个转角接触图；动图仅裁切放大实机帧，步周期 GIF 按 32 帧约 0.85 秒、转角 3.2 秒播放。截图不含 OnGUI。图像帧采自首次同表现代码构建；最终额外运行新增默认生命回归，未重复制图。

限制：转向是四套方向裁片、头身领先与脚部换步组成的简化动作，图间仍有造型跳变和关节接缝；尚未补完整方向帧或关节遮挡，不能宣称商业级连续转身。脚底几何轨迹测试不代替实际观感验收。**未运行**完整鼠标交互、第二机离线或多单位性能测试。原未跟踪 `ProjectSettings/PackageManagerSettings.asset` 保留并排除提交。

## 最新：动画优化第一阶段（2026-10-17）

目标和范围见 `art/ANIMATION_PLAN.md`，GDD 已同步。修改 `DemoWalkPuppet.cs`（起停权重、正背面淡化、腿长与落点限制、膝盖弯曲方向）、`DemoSpriteVisual.cs`（只读表现输入、独立重开复位）、`InitialDemo.cs`（仅 CLI 美术预览采集分支）、`InitialDemoTests.cs`（过渡/根节点隔离/四方向腿长回归）。未改 RouteMotion、场景、Prefab、配置或美术资产，未新增 Unity 包。状态/美术规范/计划同步更新。

- Unity **6000.6.5f1**：最终 **34/34 Edit Mode、7/7 Play Mode** 全通过；`artifacts/animation-phase1/editmode.xml`、`playmode-final.xml`。新增独立起停与重复起步测试，覆盖过渡中冻结、480 个四方向小步的腿长上限与不改逻辑位置/旋转/缩放/距离/HP/门防御；原有支撑脚、连续关节、路线转背、受击暂停与进攻回归继续通过。
- 首轮 Play Mode 5/7：腿长限制造成原超远支撑点滑动，已收短支撑行程、校正踝部偏移和身体高度后恢复支撑脚检查；新增淡化使受击色 Alpha 小于 1，测试改为校验 RGB 与合法透明度，并继续断言暂停后实际材质完整颜色不变。失败记录保留在 `playmode.xml`，没有删除或标记通过。
- 场景与卡牌/正背面引用 `DEMO_ART_PASS`；Windows x64 构建 **210123301 字节、0 错误**，无 C# 错误/警告，`build.log`。
- 真实图形 Player **Direct3D 12** 退出 **0**；`INITIAL_DEMO_CHECK: breach=True; gateOnce=True; killed=True; restart=True`，`player.log`。
- 主相机实际帧：`walk-00..31.png`、`turn-00..47.png`、`start-stop-00..65.png` 与 `demo-action.png`。已查看三组接触图与完整相机截图。`walk-closeup.gif`、`turn-closeup.gif`、`start-stop-closeup.gif` 仅从这些帧裁切/放大整理，未加工源美术；行走采样 16 帧、约 430ms，转角 48 帧约 1.6s，起停 66 帧约 2.2s，截图不含 OnGUI。
- 起停预览使用独立视觉克隆、手动提供模拟时间/移动输入；不推进原实体、不造成战斗伤害，不是正常关卡中新增的停止机制。正常死亡/穿门仍按原规则立即隐藏。

已观察限制：正背面淡化短暂重影，源图裁片存在关节接缝，尚缺正式侧面/遮挡补绘；纵深运动不能据横向锁脚测试推断为全方向无滑步。**未运行**完整鼠标流程、第二机离线和多单位性能测试；自然程度待用户试玩，不声称商业级动作已完成。原有未跟踪 `ProjectSettings/PackageManagerSettings.asset` 保留、不纳入本次提交。

## 最新补充：连续关节步态与转向（2026-10-17）

用户否决前版三姿势的自然程度；此前导入、功能测试的通过不代表其动作观感已获认可。本轮停止整图切步，新增 DemoWalkPuppet：按原图多边形分区，连续计算髋/膝/踝与独立鞋掌、支撑/回收轨迹，并控制手臂与尾巴。RouteMotion 只新增只读 Direction，原路线与移动积分不变；DemoSpriteVisual 传入距离/方向/模拟时钟和受击颜色。新增一张背面 PNG，朝远处走显示背面，转角快速切面；镜头、卡面、属性与玩法保持。

- Unity 6000.6.5f1 编译/引用检查通过，无 C# 错误/警告；日志在 `artifacts/puppet-demo`。
- Edit Mode **34/34**、最终 Play Mode **6/6** 通过：实测身体向前时支撑脚保持位置、鞋底水平，回收脚离地，0.01 秒模拟步产生连续关节变化；暂停冻结关节/转身/实际渲染受击色，向远处切背面，重开恢复。
- Windows x64 构建 **210121268 字节、0 错误**（`build.log`）；实际 Direct3D 12 Player 退出 0，`breach/gateOnce/killed/restart` 四项 True（`player.log`）。
- 实际相机帧：`walk-00..31.png`、`turn-00..47.png`，已查看步态接触图与两次转角的正背面；GIF 为这些真实帧整理，行走 GIF 为 16 个采样、约 430ms 一轮。证据 `walk-closeup.gif`、`turn-closeup.gif`、`contact-final.png`、`turn-contact.png`，不包含 OnGUI。
- 背面图 1145×1373、Alpha 0–255，生成源与项目 PNG 字节一致；来源/实际提示词见 `art/GENERATION-2026-10-17.md`。

未执行：完整鼠标 UI 交互、第二机离线测试。自然程度仍属观感验收；当前是单角色定制的分区关节原型，遮挡补绘、完整侧面/多角度转身仍不足，不能称为专业完整动画素材。已核查社区 skill，推荐见 `art/SKILL_RECOMMENDATIONS.md`，未安装。无 Unity 新包、无全局 skill 改动、无推送/公开发布。

## T00 历史验证证据

日期：2026-10-16（Asia/Shanghai）。机器：当前 Windows 开发机。引擎：6000.6.5f1 (3ff58d469c8a)。唯一验收目标是 Unity 空工程基线，未实现玩法。

阅读提示：以下保留 T00 执行时的证据与未决项快照。之后用户已确认沿用引擎，并决定 D01–D06；当前规则见 GDD v0.1-r2 / DECISIONS。历史运行结果不因此变化。

## 已执行

| 检查 | 实际结果 | 本地原始证据（忽略 Git） |
| --- | --- | --- |
| Unity 实际创建、导入与 C# 编译 | 进程退出 0；最初发现一个弃用 API 调用，已改用本机编译器提示的新重载 | `artifacts/create-project.log` |
| 入口初始化与引用校验 | 退出 0；`BASELINE_INITIALIZE_PASS`、`BASELINE_VALIDATE_PASS`；一个入口组件、正交主相机、无丢失脚本，构建入口正确 | `artifacts/initialize.log` |
| 最终 Windows x64 Development / Mono 构建 | 退出 0；`Result: Success`；`BASELINE_BUILD_PASS: 157549657 bytes; 0 errors`；最终日志无 C# 编译错误或警告 | `artifacts/build-windows-final.log` |
| 实际 Windows exe 无图形启动 | 退出 0；`BASELINE_ENTRY_AWAKE`；实际运行五帧后 `BASELINE_SMOKE_PASS`，确认场景、组件与正交相机 | `artifacts/player-smoke.log` |
| 实际 Windows exe 图形设备启动 | 退出 0；Direct3D 12 / RTX 4060 Laptop GPU 初始化成功，实际运行五帧后 `BASELINE_SMOKE_PASS`；没有人工画面验收 | `artifacts/player-graphics-smoke.log` |
| 初始化幂等性 | 再次 Initialize 退出 0，场景与场景 meta 的 SHA256 前后不变，没有重复入口或重建 GUID | `artifacts/initialize-repeat.log` |
| Assets 元数据 | 7 个资源/目录均有 `.meta`，GUID 唯一，无缺失或孤立 `.meta` | 本轮 PowerShell 检查 |
| 缓存排除 | `git check-ignore` 确认 Library、Temp、Logs、Builds、artifacts、UserSettings 不进入源码 | `.gitignore` |
| 准备资料迁移 | 五份原文件复制时逐一比较 SHA256 一致；随后仅更新 GDD、入口和课程稿的当前状态说明，原始源文件未被改动 | `docs/PREPARATION_MIGRATION.json` |
| 干净 Git 副本恢复 | 从基线提交 `11b4bfa` 本地 clone 到 `artifacts/recovery-check`，初始不带 Library；Unity 重新导入、编译并校验入口成功，进程退出 0，`BASELINE_VALIDATE_PASS` | `artifacts/recovery-validate.log` |
| 原准备包清理 | 核验精确绝对目录、五份原文件未改变、迁移目标均已提交 Git 后，逐文件删除原资料并移除空目录；确认原目录不存在 | 迁移清单与本轮 PowerShell 检查 |

原始日志中 Unity 退出阶段有 `Curl error 42: Callback aborted` 与后台会话清理提示；相同运行明确记录成功退出 0，未将这些提示当作玩法错误，也未声称日志完全无提示。

## 构建指纹

- `Builds/Windows/YesterdayConvoy.exe` SHA256：`B70442F90F50CD91F7AF7BF2272602645FC5D02C4D3156FB221FC27C328D2436`。
- `Builds/Windows/YesterdayConvoy_Data/Managed/Assembly-CSharp.dll` SHA256：`5DDB44730BD64C64646C5B9497E34D6053AB3483A9474C489AA044569CAB0598`。
- 构建大小为 Unity BuildReport 给出的总大小，不能仅凭 exe 指纹代替完整发布目录；运行须保留 exe 旁的 Data、UnityPlayer 等文件。

## 未执行与风险

- 未建立或运行 Edit Mode / Play Mode NUnit 测试套件；本次证据是实际 Unity 编译、场景检查、构建与 Windows 运行，不能混称为单元测试通过。
- 未人工查看 Unity Game 视图、点击键鼠或验收渲染画面；无图形启动通过不证明画面正确。
- 未在第二台电脑离线运行，未进行真实离线断网测试；首次打开干净工程仍可能需要包缓存或网络。
- 尚无战斗系统、中文 UI 字体、正式资源、配置资产与 Prefab；不存在已完成功能的玩法测试。
- 引擎版本为本机沿用的暂定技术选择，待用户最后确认。

## 新增关键类、接口与数据流

`BootstrapStatus` 是空入口状态组件：场景加载触发 Awake → 普通运行显示状态；带 `-baselineSmokeTest` 启动时在 Update 观察五帧 → 校验 Bootstrap 场景和主相机 → 输出 PASS/FAIL 并返回进程退出码。

`BaselineTools` 是仅编辑器使用的工具：`Initialize` 设置文本序列化、可见 meta 和 2D 工作模式，首次创建入口；`Validate` 从磁盘加载入口并检查组件、相机、缺失脚本与构建场景列表；`BuildWindows` 先校验，再把该场景交给 BuildPipeline，构建失败抛出异常。重复初始化不重建已有入口场景。

未引入战斗接口、状态容器或配置数据流。场景通过 Unity 生成的脚本 GUID 绑定运行时组件；Editor 工具不会编入 Player。

## 可复现命令的依据

启动与构建参数核对了 [Unity 6000.6 官方命令行文档](https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/command-line-arguments/editor)，并以本机实际执行结果为准。操作命令见根目录 README。

## Git 与恢复记录

- 可运行空工程基线提交：`11b4bfa`。其后仅补充验证、清理状态与 Git 属性配置，没有改动运行时代码、场景或包清单。
- 干净副本使用本机现有包缓存与许可，因此只证明同机从源码恢复，不证明另一台电脑或完全离线首次导入。
- 初次 `git diff --check` 报告 Unity 自带 YAML 空值的尾部空格以及 Markdown 双空格换行；保留引擎原始序列化格式和 Markdown 换行，在 `.gitattributes` 针对这些格式允许行尾空格。C# 仍执行默认空白检查。
- 恢复副本中 Unity 新生成了 IDE 用 `.vsconfig`；已在主仓库忽略，本次没有把本机 IDE 文件当成游戏资源提交。
- 递归删除命令被自动审批拒绝，随后用 apply_patch 明确删除已核对的五个原文件，再以非递归操作删除空目录，成功且确认不存在。
- 完整日志和恢复副本保留在被 Git 忽略的 `artifacts/` 中；最小 Windows 包保留在被忽略的 `Builds/Windows/`，均未上传或发布。

## R01：规则记录与商店提案（2026-10-16）

改动范围：GDD、DECISIONS、STATUS、AGENTS、README、文档入口、协作手册，新增 `tasks/R01.md` 与 `proposals/SHOP-v0.1.md`；未改 Assets、Packages 或 ProjectSettings。

已执行的静态检查：从提案 Markdown 解析出连续 1–10 级、每行五个概率，逐个检查 0–100% 且每行合计 100%，通过；验证三合一对应 1/3/9 基础牌及示例 `45% × 9/27 = 15%`、`45% × 3/21 ≈ 6.43%`，通过；检查旧 GDD 的仅 2 星、固定四格、三档无限池及强制累计回费上限不再作为当前规则；`git diff --check` 通过。

本轮没有运行 Unity、Edit/Play Mode 测试、构建、抽样模拟或实玩，因为只修改文档。先前 T00 结果仅证明空入口基线，不能证明这些新规则已经实现。概率来源是本轮读取的社区 Wiki，尚未独立核验官方/游戏客户端；不视为平衡已通过。

新增关键类/核心接口：无。提案中的数据流是等级 → 费用概率 → 有限牌库 → 五格商店 → 购买/合成/出售账本 → 开战编队快照，仍未实现。已确认规则与待确认概率/返库/缺货策略分别标记。

风险：小内容量下同费用怪物过少，持有增加并不在所有情况下逐步降低刷出率；暂不限制累计回费可能拉长战斗；升级节奏与三场流程尚未设计。未擅自扩充怪物、经济系统或限制机制。

## R02：商店三项确认同步（2026-10-16）

改动：GDD v0.1-r3、商店规范、DECISIONS、STATUS、AGENTS、README、文档入口与任务记录。将用户三项“是”登记为已定；旧提案记录注明其历史状态。刷新收费、升级经济等附加建议未冒充已确认。

静态检查：十级概率表与 R01 提交保持完全一致，各行合计 100%；仅改 Markdown 文档，Git 空白检查通过。未运行 Unity、构建、商店或平衡测试，未改代码/接口，商店尚未实现。预期数据流仍为等级 → 概率 → 库存 → 商店及交易账本 → 编队快照。经济数值与小内容量适配风险仍在。

## T01：最小怪物配置可校验（2026-10-16）

环境仍为 Unity 6000.6.5f1，Test Framework 1.8.0 / NUnit 来自原有锁文件，未更改包依赖。新增 UnitDefinition、校验与生成工具、构建前回调、Runtime/Editor/Tests 程序集定义、三份占位配置及必要 meta。已有场景、BootstrapStatus 源文件和 GUID 保持不变。

| 已执行检查 | 实际结果 | 本地原始证据（不进 Git） |
| --- | --- | --- |
| Unity 编译、样例创建和项目配置校验 | 退出 0；创建 U01/U02/U03，`UNIT_CONFIGURATION_PASS: 3 unit definitions; no errors`；无 C# 编译错误/警告 | `artifacts/t01-create-units.log` |
| Edit Mode 合法/非法已保存资产测试 | 17/17 通过，0 失败、0 跳过，进程退出 0；NUnit 运行耗时约 0.817 秒 | `artifacts/t01-editmode-results.xml`、`artifacts/t01-editmode.log` |
| 重复创建样例 | 三次 `UNIT_SAMPLE_SKIP`；3 份资产和 3 份 meta 的 SHA256 前后相同；校验仍通过 | `artifacts/t01-repeat-create.log` |
| Windows x64 Development/Mono 构建及原入口回归 | 场景检查通过；真实构建前自动输出 `UNIT_CONFIGURATION_PASS`；构建成功、总大小 157551089 字节、0 错误，进程退出 0 | `artifacts/t01-build-windows.log` |
| 新 Player 启动 | Direct3D 12 初始化；`BASELINE_ENTRY_AWAKE`、`BASELINE_SMOKE_PASS`；实际五帧后退出 0，验证 asmdef 分离未破坏入口绑定 | `artifacts/t01-player-smoke.log` |
| meta、测试清理与范围 | 23 个唯一 GUID，资源/文件夹 meta 完整，无孤立 meta；无 `__T01Fixtures_*` 遗留；未改场景、Packages、ProjectSettings | 本轮 PowerShell 与 Git 差异检查 |

失败路径证据来自实际 Unity 导入的临时 `.asset`：重复 ID 的诊断包含 First/Second 两条资产路径；非法 gateDamage 的项目配置被 `UnitConfigurationBuildCheck.OnPreprocessBuild` 拒绝；其余覆盖空/空白 ID、ID 前后空白、空名称、0/负移速、NaN、正负无穷、0/负穿门伤害、缺失资产和空集合。合法配置与非法配置校验前后的资产文本相同，没有静默修复。诊断原文保留在 NUnit XML 的 test-case output 中。

上述负向构建测试直接调用真实构建前回调并断言 BuildFailedException，不等同于另跑一遍完整的失败 Player 构建；实际正向 Player 构建证明该回调已挂入 Unity 构建管线。测试清理临时资产，没有把非法样例纳入源码。

未执行：人工点击 Inspector/菜单、Game 画面和交互验收、Play Mode 测试、第二机运行、移动/穿门/战斗/商店测试。当前场景未引用或加载配置；已构建 Player 仍为空入口，不能据此声称战斗数据流已运行。新增速度 1.5/1/0.75、伤害 1/2/4 只是试验初值。当前正移速/正穿门伤害约束只适用于本版原型，新增静止单位等机制需另行扩展。

日志中有 Unity 后台许可令牌更新与退出清理提示，但实际许可可用、编译/测试/构建和 Player 均以 0 退出；未将后台提示伪称为“日志全无错误”。可复核摘要、每个测试名与证据指纹见 `verification/T01.json`。

关键类/接口：UnitDefinition 通过四个只读属性供后续运行组件读取；Validator 的 FindProjectDefinitions/Validate 返回诊断；Tools 的 ValidateProject 将诊断变成可读错误；BuildCheck 在任何常规 Player 构建前复用它。数据流为 Inspector → 配置资产 → 校验 → Console/构建拦截；后续移动状态与配置分离。操作说明见 CONFIGURATION。

## T02：单只怪物固定路线移动（2026-10-16）

改动：新增 RouteMotion、PrototypeUnit、MovementPrototype、显式测试参数下启用的 MovementSmokeCheck，场景生成/校验与构建场景回调、移动 Edit/Play Mode 测试；更新 Bootstrap 场景及状态说明，新增 4 份纯色占位材质，更新文档。UnitDefinition/样例参数、包清单与锁文件、ProjectSettings 均未改；场景及原脚本 GUID 保留。

| 检查 | 实际结果 | 本地原始证据（忽略 Git） |
| --- | --- | --- |
| 最终 Edit Mode | 34/34 通过，0 失败、0 跳过，退出 0；含 T01 全部回归、折线跨点、步长一致、重复点、零时间、无效参数、场景缺引用/零长路线与进度单调/上界回归 | `artifacts/t02-editmode-results.xml`、`artifacts/t02-editmode.log` |
| 最终 Play Mode | 3/3 通过，0 失败、0 跳过，退出 0；真实加载 Bootstrap，验证配置速度与实际移动、Time.timeScale=0 停止、终点不循环且配置不改 | `artifacts/t02-playmode-results.xml`、`artifacts/t02-playmode.log` |
| Windows x64 Development/Mono 构建 | 退出 0；场景/怪物配置校验通过，最终 BuildReport 157567428 字节、0 构建错误；最终无 C# 编译警告或错误 | `artifacts/t02-build-windows.log` |
| 实际图形 Player 完整移动 | Direct3D 12，U01 speed=1.5、length=15；到达 (5,-1.5,0)，`T02_MOVEMENT_PASS`，进程退出 0；恰好一次 `ROUTE_ARRIVED`，到达状态下观察五帧 | `artifacts/t02-player.log` |
| 相机画面检查 | 1280×720 的起点/中途/终点三图已查看；路线、绿起点、橙单位、蓝终点可见，三图位置与进度相符；图片不含 OnGUI 状态栏 | `artifacts/t02-player/start.png`、`middle.png`、`end.png` |
| 生成幂等性 | 重复生成返回 `MOVEMENT_SCENE_SKIP`；场景、场景 meta、4 个材质及其 meta 的 SHA256 均不变 | `artifacts/t02-repeat-initialize.log` |
| 资源与范围 | 40 个唯一 GUID，meta 完整无孤立项，无 T01 临时资产遗留；Player 仅包含 YesterdayConvoy.Runtime 项目程序集，没有 Editor/Tests 项目程序集 | PowerShell/Git 检查 |

真实发现与修复：第一版 Edit/Play Mode 通过后，实际 Player 检出终点前进度浮点回退。固定步长 0.0002 秒测试随后复现进度 15.000299621060549 超过总长 15（该轮 3 个参数测试中 1 失败、2 通过）。原算法逐帧积分 Vector3 位置并累计浮点段距；修复为双精度总距离积分、静态线段插值推导位置。最终保留 0.0002/0.1/1÷60 秒三种步长回归，严格验证单调性与上界，全部通过。原失败日志为 `t02-player-first-failure.log`，失败复现为 `t02-progress-repro.xml` / `.log`。

验证工具修正：隐藏窗口下 ScreenCapture.CaptureScreenshot 记录“Failed to capture screen shot”，虽然移动通过，但图片未写出且验证退出 1（保留 `t02-player-capture-failure.log`）；改为临时主相机 RenderTexture 读取并以 PNG 写出后，三图成功且进程退出 0。画面为真实当前场景相机渲染，不是游戏全窗口或人工点击截图。曾出现的 Renderer 状态属性编译错误和测试在未保存空场景中 additive 建场失败也已修正；后者改用隔离 PreviewScene，最终全部验证通过。

未执行：人工编辑器/键鼠交互、OnGUI 完整画面验收、第二机离线验证、蓝门扣防御/实体离场/部署/阻挡/胜负等后续玩法测试。蓝色标记尚无门结算；程序正常模式没有战斗超时规则或暂停 UI。几何布局/速度只是试验值，正式视角、美术与玩法平衡尚待验证。

关键连接：场景 MovementPrototype 提供统一 delta，PrototypeUnit.Initialize 从只读配置创建 RouteMotion，Simulate/Advance 推进独立位置；状态投影到 Transform 后显示。场景生成器保留人工值，构建校验复用路点与引用检查；测试型看门狗和相机采集仅显式参数启用。详细接口与复核命令见 MOVEMENT，日志/图片/源码指纹见 `verification/T02.json`。

Git 属性沿用对 Unity YAML 空值尾随空格的处理，为新增 `.mat` 同样指定文本 LF 与允许空值行尾空格，保留引擎序列化输出；C# 默认空白检查不变。

## 最初可玩 Demo：一怪一塔、红蓝门与斜俯视（2026-10-16）

新增 InitialDemo 运行组件、InitialDemoTools 场景生成/校验、一项 Play Mode 闭环用例、InitialDemo 场景、11 份占位材质及必要 meta。Baseline 构建/校验转向新入口，EditorBuildSettings 首场改为 InitialDemo；EditorSettings 默认使用 3D 工作模式。GDD、AGENTS、README、STATUS、DEVELOPMENT 与 DECISIONS 同步任务粒度和入口。U01 等既有配置、旧 Bootstrap、包清单不变；用户打开编辑器产生的未跟踪 PackageManagerSettings.asset 保留，不纳入此次提交。

| 实际检查 | 结果 | 本地证据（忽略 Git） |
| --- | --- | --- |
| Unity 编译与新场景校验 | 退出 0；INITIAL_DEMO_SCENE_PASS，无 C# 编译错误/警告 | `artifacts/initial-demo-setup.log` |
| Edit Mode / Play Mode | 34/34 与 4/4 通过，0 失败、0 跳过；新增用例加载真实场景，验证斜镜头、突破、门只扣一次、死亡、重开 | `artifacts/initial-demo-editmode.xml`、`initial-demo-playmode.xml` 与同名日志 |
| Windows x64 Development/Mono 构建 | 退出 0，157694405 字节，0 构建错误 | `artifacts/initial-demo-build.log` |
| 真实图形 Player | Direct3D 12；100 生命挨 4 次攻击剩 36，进门防御 8→7；30 生命挨 2 次攻击死亡，门保持 8；重复结算保护与重开通过，INITIAL_DEMO_CHECK 四项 True，退出 0 | `artifacts/initial-demo-player.log` |
| 主相机画面 | 已查看 1280×720 真实渲染，斜俯视棋盘、怪物、红蓝门、塔和攻击射线可见 | `artifacts/initial-demo/demo-action.png` |

关键连接：InitialDemo.BeginAttack 重建本局 HP/门防御/攻击计时/RouteMotion；Simulate 以统一 delta 移动、判射程并扣血，死亡先于进门结算，终态不再扣门。OnGUI 的开始/重开调用同一个接口，生命滑块只在非运行时启用。InitialDemoTools 仅为编辑器入口，场景在 Unity 内生成并保存 GUID 引用，运行时不重建美术对象。

未执行：人工点击完整 UI、OnGUI 全窗口视觉验收、第二机/断网验证；场景图片不含 OnGUI。中文临时依赖 Windows 系统字体，尚未内置正式字体。生命 30–140、塔射程 2.8、每秒伤害 16、蓝门初始 8 及布局为试验值，尚无正式平衡、费用/手动部署、阻挡、商店或完整挑战胜负。编辑器后台许可令牌提示仍存在，实际编译/测试/构建均成功，不宣称日志绝无后台提示。

## 美术调查与通用素材入库（2026-10-17）

按用户任务书优先完成 Phase 1–3，并小量执行 Phase 4。保留用户再次确认的斜俯视 2.5D。新增独立 Art 资源目录、来源/许可/Credits/需求 CSV/风格与流程文档、实际源图对比和编辑器 ArtResourceTools；没有修改现有场景、Runtime、配置、包清单或 ProjectSettings。用户原有未跟踪 PackageManagerSettings.asset 保留，不纳入此次提交。

实际 Unity 编译/导入：`ArtResourceTools.ConfigureAndValidate` 最终退出 0，`ART_IMPORT_PASS: icons=16; textures=6; audio=8; fonts=1; total=31`，无 C# 编译错误/警告。图标检查 Sprite/256px/Alpha 和导入设置，粒子 Texture 正常载入，8 个 OGG 解码有效（短音效 0.094–0.846 秒、背景 206 秒双声道），静态 Noto Sans SC 字体简中/英文/数字/标点/★→ 字形样本通过。日志 `artifacts/art-research/unity-import-final.log`，名单 `unity-verified-paths.txt`。磁盘 PNG 检查透明像素与非空前景；已查看真实来源对比图和已导入素材联系表。96 个 Assets/_Game GUID 唯一、meta 完整；31 条实际资产逐条匹配 Unity 成功名单、文件和许可；CSV 另追踪 161 需求、6 原包、3 对比图，共 201 条。

关键连接：官方下载/筛选→Art 目录→TextureImporter/AudioImporter/Font→逐个 LoadAsset 与字形检查→清单状态和 Credits。编辑器菜单只配置当前图标/粒子/音频/指定字体，跳过将来的 Characters/Environment 图片，不改场景，也不引入运行时依赖。资源目前均未绑定到玩法。

第三方许可文本含原始制表空行、混合换行与文件末尾空行；仅对 docs/art/licenses/*.txt 关闭换行规范化和空白告警，以原字节保留授权文件，C# 等其他文件检查不变。

未执行：音频试听/音量与循环接缝验收、角色动画/切片/斜视角绑定、正式 UI、粒子 Prefab、独立 Player 字体验证、第二机测试。本轮无玩法变动，未重复运行 Edit/Play Mode 或构建；之前 Demo 结果不能代替本轮美术验证。未生成 AI 图片、未购买、未导入 Asset Store 或授权不明资源。Phase 5 两角色工作流和 Phase 6 P0 尚未完成；条件性 AI 缺口与中英文提示词见 AI_PENDING。

## 认可卡牌与系列实机换装（2026-10-17）

实际变更：原样保存用户认可的卡面，新增 5 张内置 image_gen 输出；新 CardDefinition/WisadelApril.asset 登记招募费 5，DemoCardView/FiveCostCard.prefab 将图、名、费连到相机内卡牌；DemoSpriteVisual 处理镜头朝向、脚底排序、模拟时间摇摆与受击闪色。DemoArtTools 在 Unity 中配置透明纹理/PPU/Pivot，并更新原场景引用；旧网格仅禁用 Renderer，逻辑挂点与路线、U01 数值未改。HUD 接入内置中文字体，地面材质更新，构建前检查卡牌和四个 Sprite。无包升级、无 ProjectSettings 修改。

| 检查 | 真实结果 | 本地原始证据 |
| --- | --- | --- |
| 编译/场景/卡牌绑定 | 最终退出 0，无 C# 错误/警告；DEMO_ART_PASS：认可卡面、5 费、4 Sprite、地面、内置字体 | artifacts/art-demo/setup-final.log |
| Edit Mode | 34/34 通过，0 失败/跳过 | artifacts/art-demo/editmode.xml / editmode.log |
| Play Mode | 最终 5/5 通过，0 失败/跳过；包含 5 费/卡面/战场图引用、突破/死亡/重开及真实受击闪色状态下冻结 | artifacts/art-demo/playmode-final.xml / playmode-final.log |
| Windows 构建 | 成功、0 错误，203809489 字节 | artifacts/art-demo/build.log |
| 实际图形 Player | Direct3D 12、退出 0；100 生命 4 发后剩 36，门 8→7；30 生命 2 发死亡、门 8；gateOnce/killed/restart/breach 四项 True | artifacts/art-demo/player.log |
| 实际相机画面 | 已查看：左侧认可卡图、中文卡名和 5 费清晰，角色/炮/红蓝门/石板可见且贴地；未包含 OnGUI 顶栏与底部操作条 | artifacts/art-demo/demo-action.png |
| 资产完整性 | 五张 RGBA 图 Alpha 0–255、地面 RGB；认可原图字节完全相同；121 个 GUID 无重复、meta 完整 | 本轮文件/像素/引用检查，清单与生成记录 |

首次场景检查发现新建卡资源在切换场景后引用未保存，已改为先保存资源、开场景后重新取持久化资源引用；最终场景/Prefab 与 Player 都正确绑定。首次失败留 setup.log，没有把失败当成功。生成来源和最终提示词见 GENERATION-2026-10-17.md；当前试玩包附 Credits、字体许可与美术来源说明。

未执行：人工鼠标点击完整 HUD、第二机离线验证、性能/平衡验收；美术为单帧 Sprite 与程序摇摆/闪色，没有完整逐帧攻击/死亡动画。5 费尚未接购买/部署扣费，正式技能未实现；卡牌/战场角色为用户指定 AI 同人原型，没有把它标成 CC0 或已取得正式发行授权。没有推送或公开发布。

## 2026-10-17：走路动作与镜头回正

实际改动：DemoSpriteVisual 按 RouteMotion.DistanceTravelled 播放左跨步/收步/右跨步/收步，配合小幅起伏/摆身；复用 InitialDemo 模拟状态，暂停不换步、重开重置。新增两张透明姿势图及 meta，原卡面不变。DemoArtTools 配置姿势、相机偏航归零并保留现有场景对象；初次建场景也使用正向俯视。战斗路线、速度、伤害和结算未改。

- Unity 6000.6.5f1 编译、最终场景与素材引用检查退出 0，无 C# 错误/警告：`artifacts/walk-demo/setup-final.log`。
- Edit Mode **34/34**、最终 Play Mode **6/6** 通过；新增测试验证实际不同腿部姿势、暂停保持姿势/旋转、重开首步、棋盘行列无侧倾：`editmode.xml`、`playmode-final.xml`。
- Windows x64 构建成功、0 错误、216408485 字节：`artifacts/walk-demo/build.log`。
- 实际图形 Player Direct3D 12，退出 0；`breach=True; gateOnce=True; killed=True; restart=True`：`player.log`。
- 实际相机采集 12 张行走帧，每张相差 0.05 模拟秒；已查看两脚跨步、收步与水平棋盘：`walk-00.png` 至 `walk-11.png`、`walk-contact-sheet.png`。GIF 为真实帧整理，未用概念图代替实机：`walk-preview.gif`、跟随裁切放大的 `walk-closeup.gif`。这些相机画面不含 OnGUI。

首轮右步图没有正确交换腿序，已弃用并通过明确条纹腿方位重生成；最终两张 PNG 均为 1145×1374、Alpha 0–255，接入并重新运行 Play Mode 与构建/Player。仍是三姿势简化动画，AI 细节存在轻微帧间变化；前后向步态、攻击/死亡专用帧、完整鼠标 UI 与第二机离线测试未运行。本轮不声称正式美术或完整动画体系已完成。
