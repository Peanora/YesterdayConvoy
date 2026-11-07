# 验证证据与回归

## 2026-11-07：萨卡兹2/4/6/8/9众魂之契

- 从实时main `f6d52bd`建立`codex/sarkaz-souls`，快进承接当前已在用且待审的集成头`3f34762`（PR #94），保留既有角色/地图与运行素材。仅本任务脚本、测试、规则/验证记录与试玩快捷脚本纳入提交；无场景/Prefab/Packages/ProjectSettings/素材改动，无ZIP/Releases或自动合并main。
- `SarkazContract`持有承契窗口、20秒CD、请求幂等及独立众魂盾池；编队快照固定2/4/6/8/9，部署按萨卡兹常驻生命系数初始化，伤害入口消耗真实盾并发护盾吸收反馈；UI读取真实状态。`UnitBaseHealth`保留泥岩法盾破/回时的生命倍率。新增入场体获得剩余窗口一次盾，九档每实体20秒补盾，支援/灵体/分体排除。设施真伤在原生无敌时提前拒绝，避免白扣盾。个人SP/动作/复起阶段不重置。
- Unity6000.6.5f1完整EditMode **654/654**（`artifacts/sarkaz/edit.xml`）；后续补原生无敌回归并复验萨卡兹/徽记 **41/41**（`edit-final.xml`，含新增1项）。相关D3D11 PlayMode **13/13**（`play.xml`）：五档真实预置/库存守恒/暂停普通拖动、九场景初始编队、牧群/深池/海嗣既有交互、泥岩原作法盾、爱国者双阶段与伤害反馈。未跑全量PlayMode。
- `f27a12b`首轮Player69/70，唯一失败为九档自然受击未发生：验收选择M38，其原生20秒枷锁覆盖首次补盾观察期。保留`player/result.json`和截图/日志；仅将6/8/9验收伙伴改为无控制M26，不修改生产数值/地形/伤害、不过滤失败断言、不注入生命/SP/时钟/胜利。
- 最终运行源码 **`bef9d82b58e1f7ba3ff602297e64282b48f29852`**；现有`DeepPoolBuildTools.BuildWindows`按动态完整保存场景目录构建，**1063527732 bytes / 0 errors / 退出0**（`build-final.log`）。完整1974文件同步常用`Builds/Windows`，逐文件存在/长度核对一致，运行DLL SHA256 `87C92DEA718E7C3BF7DE58168A8F004A16BF1D767E3BD92115C3A96988EC9418`一致；SOURCE.txt记录该源码，新增萨卡兹试玩.cmd快捷入口。
- 工程外TEMP启动1600×900可见D3D11独立Player **70/70、退出0**（`player-final/result.json`、`player-final-exit.txt`）。五档生产预置与普通指针部署、无伤启动、同卡共享/六档半额/八档完整、暂停/幂等/CD均通过。两名普通实体即可享九档；实际二/九档射击攻击129.6/240、实际扣血125.6/236，九档生命1792与盾1075.2，守军真实受击消耗盾后20秒自动补盾通过。使用既有专项怪物生命×4/攻击×2测试倍率，不作为正式战役平衡结论。已实看`01-nine-tier-preparation.png`、`08-nine-tier-auto-refill.png`等真实渲染截图，紫色盾条、共鸣/补盾HUD、完整五档说明与彩色徽记可见。动作由现有生产指针/控制器驱动，不冒称OS人工鼠标操作。
- 仓库7639跟踪文件校验通过；原52项VFX meta与开工SHA256逐文件相同，`.vscode/`和`PackageManagerSettings.asset`保留不提交。未执行第二机、全分辨率、自然13关成型率/长局平衡；当前强度仍是获准接入的试玩参数。


## 2026-11-07：泥岩六档彩色徽记修正

- 运行源码`7c7ad44`：仅`SynergyBadgePresentation.GradeFor`为泥岩6档选择现有Prismatic，其余2/4/5档保持铜/银/金；侧栏与激活反馈复用同一选择，未修改实际门槛、转职、培养或战斗。显示说明表同步，未改素材、场景、Prefab、Packages或ProjectSettings。
- Unity6000.6.5f1相关EditMode **19/19**（`artifacts/mudrock-prismatic/edit.xml`），D3D11 PlayMode **4/4**（`play.xml`），0失败/跳过。既有测试补充真实五原生+第六人转职的彩色映射，以及移出第六人退金、重新编入恢复彩色。仓库检查7628跟踪文件通过，`git diff --check`通过。
- 现有`DeepPoolBuildTools.BuildWindows`按动态完整场景目录构建，**1063504436 bytes / 0 errors / 退出0**（`build.log`）；未改构建设置。同步全部1973文件到常用`Builds/Windows`，无缺失/长度差；运行DLL SHA256一致，`SOURCE.txt`记录7c7ad44。
- 先以隐藏窗口运行泥岩专项，截图黑色，经验动画中间帧检查失败（19/20，退出1，`player/result.json`）；不计为可见画面通过，也未为此改生产逻辑。改为工程外TEMP启动1280×720可见D3D11独立Player，**20/20、退出0**（`player-visible/result.json`）。已实看`01-zero-xp-cultivation.png`的五人金色与`10-free-sixth-member-transfer.png`的六人青绿虹彩/人数6。此专项操作由既有控制器驱动，不冒称OS人工点击；保留七次真实战斗结算及后续明示IV/V预置的原验证边界。
- 原52份VFX导入meta与开工SHA256逐文件相同，`.vscode/`与`ProjectSettings/PackageManagerSettings.asset`继续保留不提交。只提交本任务显示代码、既有测试断言和文档，沿PR #94交付；未创建ZIP/Releases、未自动合并main。未执行全量Edit/PlayMode、第二机、其他分辨率或自然长局平衡。

## 2026-11-07：PR 冲突整理与海嗣依赖整合

- 最终以同一工程合并海嗣依赖与树海：运行源码6b02a5900cbcea0045628c306b626ef69b4098b1，完整EditMode619/619，0失败/跳过（edit-final.xml）。新版徽记下泥岩/溟痕补充行采用共用坐标，1px经验条不画覆盖填充的边框。新增地图沿用保存地形、显式terrainOnly及兼容旧调用的可选参数；启动路由组合各专项和原诊断，不改原有主场景或Packages。
- D3D11 PlayMode首轮32项31过1败：游击六档多人夹具未提交自动整令。生产自动整令链与main相同；单项在增强诊断后1/1通过，最终游击与泥岩两类11/11（play-order-repro-fixed.xml、play-followup.xml）。因此32个不同相关用例均获得通过，但不是单次32/32；未稳定复现并定位首轮组合顺序问题，不以改玩法或减少断言消除失败。诊断文字初次误用不存在的RouteMotion.Distance导致编译退出，已更正为实际DistanceTravelled；原失败日志保留。
- Windows构建入口DeepPoolBuildTools.BuildWindows使用动态场景目录，包含树海及全部既有场景，1063504437 bytes / 0 errors / 退出0（build-final.log）。同一运行源码的独立Player均从工程外TEMP启动，1280×720、Direct3D11：树海geometry/materials/nativeGates/fixedRoutes/pause/sharedDefense/restart七项全True；海嗣15/15、自然Won、16格/4次爆发、实际地面开关差9018像素；深池17/17、2.85秒首次燎烧/8.25秒首种、同刻两目标引爆360真实HP。三个可见窗口均退出0，已目视核对实际地图、覆盖/神经HUD及引爆截图。范围查看通过生产ToggleMarineCoverage调用，部署走普通ProcessShopPointer，不冒称OS鼠标操作。结果分别在player-forest-visible.log、player-marine-visible/result.json、player-deep-visible/result.json。
- 初轮隐藏Player：树海逻辑七项通过但截图全黑；海嗣13/15，HUD与像素失败且地面差0。此轮不计画面通过，重新打开实际窗口后以上可见Player全部通过。保留黑图与失败结果，未改生产画面或放宽像素门槛。
- 完整构建已复制到Builds/Windows，EXE、运行DLL与SOURCE.txt对应6b02a59；无ZIP/Releases。仓库7628跟踪文件检查通过。开工56文件快照与收尾SHA256一致；原52处meta只有自动新增未启用WebGL配置，按既有确认保留不提交。未运行完整PlayMode、第二机、全分辨率或自然长局/全部阵容平衡。
- 实时 main 为 f6d52bd（#97），本地原深池 e7a9aff 与它文件树相同。13 个旧 PR 已通过 #80/#89/#96 完整进入 main；以原头祖先关系及整合分支/main 全树相等核实后关闭重复 PR，保留原分支与历史说明。
- 将 #94（包含 #90 泥岩、#91 牧群与 #93 银狼）同 main 逐段合并：保留深池 LastTick/首击产种/耗尽尾伤、游击五斩与新版徽记，同时接溟痕落种、灵体起身、泥岩培养与银狼。泥岩/溟痕补充行改用当前徽记侧栏坐标，避免覆盖名称与门槛。
- 初轮完整 EditMode 613 项：612 通过、1 失败，为旧徽记测试仍期待泥岩 2/4/5，而新规则已开放 6 档；按实际规则更新期待后该测试类 5/5 通过。结果在 artifacts/pr-conflict-integration/edit-marine.xml 与 edit-marine-fix.xml。该阶段尚未运行 GPU/独立 Player；后续整合验收另记。
- 仓库检查 6785 跟踪文件通过。原 52 项特效 meta、.vscode 与 PackageManagerSettings.asset 保留不提交；未 stash、未新建项目副本、未强推或远端合并 main。

## 2026-11-07：深池命中燎烧与单火种引爆

- 人类确认本轮方案并确认既存52处Mandragora/Mephisto VFX meta和两个本地项原样保留。fetch核实UI PR #96已squash合入最新main `2b7e449`（树与原UI分支`fb82933`相同），当前`codex/deep-pool-balance`从该main建立；原文件哈希快照在`artifacts/deep-pool/preexisting-hashes.json`，不纳入任务提交。
- 2/4深池普攻/原生技能真实命中即独立燎烧，15/25×Q法伤/秒、4秒、同名取高刷新，原生灼燃减抗独立。首击击杀和燎烧击杀可留种，每守军一次、本场累计4/6枚（含已用），传播1.8/2.2格最多2/3人。4档耗1种、180×Q、1.8半径最多4人、本场2次；从全部未耗种选有活目标的一枚，引爆不返种。来源补齐M31喷火/死爆与M32注目/光环，既有状态标记/伤害反馈及详情同步。
- Unity6000.6.5f1完整EditMode **555/555**，0失败/跳过（`artifacts/deep-pool/edit-delivery.xml`），新增深池28项。相关D3D11 PlayMode **12个不同用例全部通过**：首轮已有11项通过，修正新用例的VFX宿主定位后单项1/1通过（`play.xml`与`play-final.xml`）。未声称全量PlayMode。
- 首轮Edit 550/552：M32夹具技能观察窗太短/尚未完全隔离普通攻击，以及三星低血目标要求严格少一轮攻击，均修正测试而非抬高伤害。新Play meta初建GUID多一位，修正并按32位hex/唯一性复查；GPU首轮新用例错误从Controller找VFX，实际应从绑定CombatView获取，Evidence一并修正。失败记录保留，不计通过。
- 正式CampaignBalance原属性、无芦苇、同阵容开关羁绊的自然对照：一星方阵+逐火对400血/4甲，8.10→6.30秒；1200血/18甲，36.90→26.00秒。硬目标二/三星20.70→16.10、13.50→10.70秒。三星400血目标同为3.35秒，但前2秒伤害202.70→227.45，离散攻击轮次未减少。四成员一星六守军：密集30.75→20.60秒，稀疏60.05→57.65秒。数据见本地`balance.csv`；密集组新规则包括合法主动引爆，稀疏无合法引爆目标，不能把该差值当纯DOT倍率。
- 单次燎烧4秒预算60/100×Q，按法抗结算；新测试覆盖1/2/3星、50法抗、分步积分、不自续、重复命中不叠、普通/原生来源、暂停重开、首击/持续伤害产种、去重/累计上限及引爆事务/有效旧种选择/四目标上限。Q保持`1+0.25×(平均星级−1)`。数值仅通过上述确定性对照，不代表十三关随机平衡或所有阵容最终定案。
- 最后补齐耗尽边界：有存活守军的未到期燎烧纳入待结算事件；最后一只死亡/无可用部署时继续结算完整尾伤，暂停冻结，结束后才判负。该新增回归随完整EditMode555/555通过；相关GPU12项在此模型补充前已通过，最终Player使用补充后的源码。
- 最终运行源码 **c4d489d3df6cb9251d3e67fec0f624a8d4f9e241**，Windows x64 Development构建 **1050831921 bytes、0 errors**（`artifacts/deep-pool/build-final.log`）。D3D11独立1280×720 Player **17/17**，正常结束：只正常拖放蔓德拉，2.85模拟秒燎烧、8.25秒自然首种，同刻耗1种引爆，对两名守军共扣360真实HP；暂停不扣/不走时钟、请求幂等、重开清空、M31原喷火与燎烧/原特效连接均True。输出`player-final/result.json`和五张完整OnGUI截图；已实看燎烧标志、真实伤害、单种引爆及原喷火模型画面。拖放走普通ProcessShopPointer路径，没有状态/火种/生命注入，不冒称OS鼠标操作或整局通关。首轮隐藏窗口虽模型17项通过但截图全黑，仅作模型记录；最终使用正常渲染窗口重新验证并实看非黑图，不将前轮黑图计为画面证据。
- `Builds/Windows-DeepPool`整套同步至常用`Builds/Windows`，1938文件无缺失/长度差，EXE、实际Runtime DLL与SOURCE.txt哈希一致（`copy-check.json`）。直接运行`Builds/Windows/深池试玩.cmd`进入已保存的专项场景，不自动退；普通`YesterdayConvoy.exe`也包含同一深池规则。仓库检查6638个跟踪文件通过；原52处VFX meta及本机项逐文件哈希与开工56文件快照相同。无场景/Prefab/Packages/ProjectSettings任务修改，没有新增或替换美术，火种充当逐火环境火源本轮未做。不自动合并main、ZIP或Releases。
- 未做全量PlayMode、第二机、十三关随机成型率/完整通关、所有阵容/全分辨率或长时间性能；稀疏防线的传播收益仍低于密集防线，是本轮保留的范围定位。

## 2026-11-07：档位变化即时中断与已出现条目去重

- 用户报告4泥岩/4萨卡兹演出期间加入第五位泥岩应立即改播5泥岩；进一步明确“已经出现过就算已播”，被打断但同档不变的4萨卡兹不补播，未开始的才保留。沿原codex/synergy-icons-tier-ui与PR #96继续，fetch确认main b1d55e7为祖先，原52处VFX meta及两个本地项隔离保留。
- 根因是旧Feedback仅将新档追加，且未区分已开始与尚未开始。现在任何真实Tier变化都立即重建最新候选、清旧Age/gap、分配新Sequence；原音通过已有Sequence同步立即停止。候选取原活动/待播及变化条目，按最新视觉档位/内置顺序排序，使用最新Members。首次有效正向Advance记录该名/档已开始；Age0或paused不记录，同档刷新保持Age/Sequence。未完成入场标记随重建保留，不提前给空卡位播放。Clear(false)不复活被主动清掉的旧队列，真实退档/归零后再达仍可重播。
- 实际配置M26/M27/M42/M43前四为4泥岩+4萨卡兹，M28加入后为5泥岩金、萨卡兹人数5但Tier仍4银。去重按真实Tier，不按人数或仅按颜色。没有改标签、门槛、库存、伤害、场景、Prefab、包、工程设置或V4绘制/音源。
- Unity6000.6.5f1完整EditMode **527/527**（`artifacts/synergy-interrupt-edit.xml`），反馈14/14，覆盖萨卡兹未开始/已开始未完/已完三时点、Age0阻塞、旧gap立即取消、快速6→4→2→0→2、同档刷新、入场标记、Clear(false)及浮点边界。D3D11 PlayMode **26/26**（`artifacts/synergy-interrupt-play.xml`），反馈交互7/7及原界面/感染/商店/侧栏19项，0失败/跳过；合法泥岩阵容覆盖三分支、旧音立即停/新音一次、5→4立即新反馈。仓库检查6630个跟踪文件及差异空白检查通过。
- 运行源码 **064fb9e4c6daa2e77b2a1c19d0172e86a206d46b**；`SynergyActivationTools.BuildWindows`真实Windows x64 Development构建 **1050818044字节、0错误**（`artifacts/synergy-interrupt-build.log`）。完整同步至通常入口`Builds/Windows/YesterdayConvoy.exe`及SOURCE.txt；1937个构建文件无缺失/长度差，EXE、YesterdayConvoy.Runtime.dll、resources.assets/resS、globalgamemanagers及源码标记哈希一致（`artifacts/synergy-interrupt-build-copy-check.json`）。
- 独立Player **15/15**，1280×720，正常退出。入口`-synergyActivationInterruptSmokeTest`，结果`artifacts/synergy-interrupt-player-1280/result.json`明确mode=`mudrock-interruption-two-scenarios`，只跑本次两个独立合法五张库存夹具，不冒称旧54项流程或自然挑战。两次真实OS点击M28入队：先在4泥岩Age.5时升级，后在4萨卡兹Age.5时升级；两者都立即换新Sequence/Age0、停止旧AudioSource，再独立起一次5泥岩原音。第一种保留未开始4萨卡兹、第二种不补播已开始4萨卡兹，最终队列/声音清理且库存/金币不变。
- 捕获并实看九张完整OnGUI图，含中断前、重算Age0旧框消失、5泥岩峰值、未开始4萨卡兹保留、已开始4萨卡兹被替换及清理；路径`artifacts/synergy-interrupt-player-1280/interrupt-01...09*.png`。画面为独立Player输出，非Camera-only或项目外合成；测试夹具及手动呈现推进不冒称实时自然长局通关。
- 原52处Mandragora/Mephisto meta与本轮开工哈希逐字节保持，.vscode/及PackageManagerSettings本地项未提交。未重跑全量PlayMode、旧54项Player全流程、1920尺寸、第二台电脑、自然长局、专项帧时或主观听感；V4绘制及音源未变，前次双尺寸结果只作旧版本证据。无ZIP/Releases、不自动合并main。

## 2026-11-07：V4旋转亮框、阶色优先与逐条播放

- 人类确认项目外V4并明确排序后接入。延续codex/synergy-icons-tier-ui及既有PR #96，fetch确认main为b1d55e7且是本分支祖先，没有另建工程或切换工作目录。修改限定Feedback/Presentation/UI/Evidence、两组现有测试及表现文档/STATUS/QA。
- `SynergyActivationFeedback`观察真实Tier/Members，同一观察的升档按`GradeFor`视觉档位降序、同阶按`SynergyTrialCatalog.Entries`内置顺序；每个Batch只有一条Pulse、单独Sequence，1.78秒后净空0.27秒。后来的触发追加、不重启有效活动条；降档/移出裁剪活动与待播，暂停冻结条及间隔，切战/离开全部清理。Edit用反向九条输入和泥岩5金先于萨卡兹6金，排除输入顺序及人数二次排序；近终点浮点舍入和大步跨间隔同样覆盖。
- `SynergyActivationPresentation`按确认V4移植整圈亮框、外框−64°→+6°→0、内框短反旋与缩放回落，白徽延后0.13秒显现且不跟随旋转。IMGUI柔光用两张首次生成后缓存的遮罩近似Canvas模糊，没有每帧纹理/Material；扫光按矩形转角切段，减少冗余短线。原九徽、各档底牌、青绿最高阶及原音字节不改。
- `SynergyActivationUI`按每条Sequence起一次原音，追加/模态恢复不重起；按队列边界重新检查购买到位。复核发现已播放条可能被随后同ID副本的购买光流误暂停，已将到位等待限定在Age==0，并以合法双份商店的两次真实购买验证不冻结、不重播、不额外升档/合成。
- Unity6000.6.5f1完整EditMode **522/522**（`artifacts/synergy-v4-edit.xml`），其中反馈9/9；最终D3D11 PlayMode **25/25**（`artifacts/synergy-v4-play-final.xml`），包含反馈6/6及ApprovedUi、感染、商店与侧栏原19项回归，均0失败/跳过。首轮Play25/25后补入同类复购边界，再次跑最终25/25；不把重复运行累加为新测试数。仓库检查6630个跟踪文件及差异空白检查通过。
- 运行源码 **b05643edbeebf880162ec7fd12b06072184cb2b5**，`SynergyActivationTools.BuildWindows`真实Windows x64 Development构建 **1050808480字节、0错误**，日志`artifacts/synergy-v4-build.log`。验证后将Windows-SynergyActivation完整同步至常用`Builds/Windows`，1937文件无缺失/长度差异，EXE、实际`YesterdayConvoy.Runtime.dll`、resources.assets/resS、globalgamemanagers及SOURCE.txt逐字节哈希一致；记录`artifacts/synergy-v4-build-copy-check.json`。
- 最终可见独立Player：`artifacts/synergy-v4-player-1280/result.json`和`...-1920/result.json`各 **54/54**，尺寸实为1280×720/1920×1080，均正常退出。每轮7次真实OS点击，含原单条入队/移出/重达、关闭菜单、M16一次入队同时触发金整合与铜游击、净空菜单关闭、感染提示关闭。合法七张库存及模态打开夹具明确标识，没有注入Tier、伤害或非法库存，不冒称自然通关。
- 捕获完整OnGUI `.08/.20/.40/.66s`四张旋转帧，及金、净空、铜、清理与模态前后画面；根与只读复核均实看关键画面。金档六关联卡徽记差异像素720p为285–328、1080p为653–742；六卡四边像素每边720p至少208、1080p至少456。铜档只亮两游击卡，四边720p至少161、1080p至少390；其余成员没有复用金徽，无关海嗣及净空无残留徽记。逐条独立原音、净空静音、菜单Age/采样和净空计时冻结、关闭恢复不重起、感染模态不吞反馈、切战与离开清理均通过。
- 原52处Mandragora/Mephisto meta与本轮开工快照逐字节保持，原`.vscode/`和PackageManagerSettings本地项不提交；场景/Prefab/Packages/ProjectSettings没有任务修改。未重跑全量PlayMode、第二台电脑、自然长局、低配专项帧时或主观听感。IMGUI线条略硬、柔光略窄；首卡徽记短时经过编队栏标题，与确认稿相同，费用/星级/卡名仍可辨。源素材权利状态沿用记录，不把接入授权写成权利人授权。无ZIP/Releases、不自动合并main。

## 2026-11-07：羁绊达档反馈与原版金档提示音

- Unity6000.6.5f1完整EditMode **518/518**，0失败/跳过（`artifacts/synergy-activation-edit.xml`）；相关D3D11 PlayMode新用例最终 **5/5**（`synergy-activation-play-delivery.xml`），原侧栏/感染提示/商店界面回归 **19/19**（`synergy-activation-regression.xml`），均0失败/跳过。覆盖真实2/4/6档、消失/降档后重达、同档换人剪除、同批多羁绊、游击8到9同阶色仍触发、菜单时钟/静音/切战/新挑战、实际购买原入场未完成不提前播。
- 首次新GPU运行没有发现测试（新增测试meta GUID长度不合法），0项不计成功；修正GUID后发现Ready阶段的战斗暂停本来无效，按既有规则将准备冻结用例改为真实整备菜单阻断，没有改变战斗暂停机制。新增购买到位用例后最终5/5。首轮Player误复用会主动关闭模态的侧栏夹具；最终改成合法感染编队降档再重达，自动打开原形态提示，并将验收菜单关闭目标改为原生关闭按钮。
- 最终运行源码 **ad0ebc6555379c1ab89ac137030a493338a15356**，本机Windows x64 Development / D3D11构建 **1050790156 bytes，0 errors**（`artifacts/synergy-activation-build-final.log`）。完整专项目录`Builds/Windows-SynergyActivation`验证后同步到常用`Builds/Windows`，`SOURCE.txt`记录源码；不创建ZIP或Releases。
- 最终可见独立Player：`artifacts/synergy-activation-player-final-1280/result.json`与`...-1920/result.json`各 **24/24**。每轮5次真实OS鼠标点击：备战入队、关闭整备菜单、移出、重新入队、关闭感染提示。菜单打开是显式状态夹具；事件和档位来自合法库存及原编队/输入路径，没有写Battle.Tier/伤害或篡改库存规则。两轮均正常退出。
- 捕获完整OnGUI触发前、.5秒峰值、菜单冻结、消退以及感染提示前后六帧/轮，均已实看。实际卡顶反馈像素：1280为士兵187、弩手252、术师256、重装253、游击战士259、狙击手255；1920分别442/546/564/556/564/556。无关海嗣两轮均 **0**，可排除给全队误加同一徽记。检查音源确为交付的Resources原音、同批只起一次，菜单中Age与音频采样冻结、恢复不重播；同档重建安静，感染模态不吞反馈，切战/离开立即清理。
- 原音为本机原始Wwise媒体的未剪裁PCM解码，事件与真实媒体链见`docs/art/source-records/synergy-activation-original-audio.json`。Gold名称由哈希与同bank多条语义一致事件推断，不是明文表恢复；没有声称与官方银蓝片段逐波形一致，或复制所有档位独立声音。源WAV和带音项目外看稿完整解码通过，未进行主观人工听感验收。
- 原52处Mandragora/Mephisto meta与开工基线逐字节保持，`.vscode/`、PackageManagerSettings本地项不提交；场景、Prefab、Packages、ProjectSettings没有任务修改。未重跑全量PlayMode、第二机或自然长局平衡。模型、夹具及OS点击验收不冒称完整自然挑战通关。

## 2026-11-07：最高阶V2青绿校色、旧粉色预留

- 人类确认项目外V2青绿虹彩并要求原粉色留供后续五费专属。沿用codex/synergy-icons-tier-ui及PR #96；fetch确认main仍b1d55e7、任务分支与origin一致且PR未合并。原52处VFX meta与开工快照逐文件SHA一致，原两个本地未跟踪项不提交。无场景/Prefab/Packages/ProjectSettings任务修改。
- 现用最高阶PNG逐字节等于确认V2（SHA256 e934697f80f04426ee7cfe46824276ad514e73b34112ca6972ca0b97ad5727d7），原meta/GUID保持。原粉色PNG逐字节保存为Reserved/plate-five-cost-exclusive-pink.png（SHA256 4f8e1d273289a87617c5ab945470490cf2c72ad4f74a9276e941dd695b2c3f25），原SVG也逐字节保留；预留PNG及目录用独立新GUID，无碰撞、未忽略，不在Resources内且无运行代码引用。原九徽及其他档位不改，不新增五费专属玩法。
- Unity6000.6.5f1相关EditMode5/5（artifacts/synergy-mint-edit.xml）、D3D11 PlayMode3/3（artifacts/synergy-mint-play.xml）通过，0失败/跳过；验证真实档位、资源独立加载与透明GPU上传、九条布局及滚动后详情输入保护。未重跑全量测试。
- 既有“多色”条件无法区分旧粉色与新青绿色，因此Player额外检查真实底牌上部6%–18%的青绿色差，排除白徽、人数圆点及边缘；至少8样本，平均G-R>.08且B-R>.03，仍保留完整真实编队/滚动/点击状态检查。源PNG只读对照：确认V2该区G-R约+.294，旧粉约-.106，不能将源图静态对照当成实机验证。下面旧版测试不作为本轮完成证据。
- 运行源码7cdb33d6092a36d376ac7b6576e81ba01acd7f33：Windows x64构建1050474618字节、0错误（artifacts/synergy-mint-build.log）。本地Builds/Windows/YesterdayConvoy.exe及SOURCE.txt记录对应源码；后续仅补验证文档，不生成ZIP或Releases。
- 1920×1080与1280×720可见D3D11独立Player各51/51（artifacts/synergy-mint-player-1920/result.json、synergy-mint-player-1280/result.json），均正常退出。两轮各实际5次点击＋1次滚动，完成游击详情开关、九条战斗列表到底、末行详情开关和返回整备；13张完整OnGUI截图/轮，原合法库存/转职夹具保持。实际8/9档及详情关闭后的底牌均为4种色相族，1920上部72样本G-R=.2913、B-R=.2202，1280上部30样本G-R=.2944、B-R=.2227；白徽合格像素分别182/86。已实看两种尺寸青绿→冰蓝→淡紫与确认V2一致，人数和完整档位可读。
- 仓库6611跟踪文件及Unity元数据检查通过；粉色原PNG/SVG原字节与新活跃资源GUID均保持，原52处VFX meta与开工快照仍完全一致。预留粉色无运行引用，未来五费专属机制尚未实现。未重跑全量Edit/Play、第二机、故障注入或自然长局平衡，不将验证夹具当自然完整通关。

## 2026-11-07：确认稿菱形底牌、白徽与实际人数

- 人类确认项目外侧栏原稿V1后要求接入。沿用codex/synergy-icons-tier-ui和PR #96；本轮fetch确认origin/main仍b1d55e7、任务分支与origin一致后继续。仅显示与验证代码、六张小型运行PNG/必要meta及SVG作者源/来源记录；九枚V3徽记不变。原52处VFX meta逐文件SHA256保持、两个本地未跟踪项不提交。
- Unity6000.6.5f1完整EditMode513/513，0失败/跳过（artifacts/synergy-diamond-edit.xml）；相关D3D11 PlayMode首轮与感染副行修正后均16/16，0失败/跳过（artifacts/synergy-diamond-play.xml、synergy-diamond-play-final.xml）。新增帧资源导入及透明GPU上传、真实9种整合仍6档金底但人数9、全九种完整门槛、合法十卡覆盖九行、雪怪5/感染2副行、滚动后的末行点击坐标与构筑/完整快捷区不侵占552部署栏。保留详情输入保护、商店/感染/雪怪既有回归。几何反射夹具不冒称真实OS点击；最后的开发回执IO重试由后续双分辨率Player实际验证。
- 菱形底牌从批准稿SVG确定性导出，五个金属灰/铜/银/金/镀彩底与一个无数字黑圆点共六张256RGBA32PNG。白徽、人数与门槛由现有数据独立绘制；currentTier只决定底牌，Count决定圆点，完整门槛当前档加粗白色。运行图片、meta及来源随源码，未改羁绊逻辑或场景/Prefab/包/设置。
- 运行源码1f4967b833acd0c7574a0a87bda081526191ccdd：BaselineTools.BuildWindows成功，1050473302字节、0错误（artifacts/synergy-diamond-build-delivery.log）。入口Builds/Windows/YesterdayConvoy.exe，SOURCE.txt注明提交与环境。构建后仅更新验证文档；不创建ZIP/Releases。
- 最终1920×1080和1280×720可见D3D11独立Player分别48/48，结果为artifacts/synergy-diamond-delivery-1920/result.json及synergy-diamond-delivery-1280/result.json，均正常退出。两轮各5次真实OS点击＋1次真实滚动：打开/关闭游击详情、列表滚动到底、末行萨卡兹详情开关、原快捷按钮返回整备。gui-step/ack只等待外部操作并核对真实GUI状态，不注入GUI事件或直接设置滚动/详情状态。每轮13张完整OnGUI截图，实际8/9档及关闭详情后的彩底均采到5种色相族，白徽分别183/86个合格像素；不是对旧彩色glyph采样。已看完整九条整备、四条战斗滚动及动态副行，两种分辨率均无感染文字裁切。
- 早期实机发现战斗滚动区缩窄后感染副行自动换行、12高裁切；已压缩空格、明确单行并按真实字体测量必要时降10→9字号，Play16/16及最终两轮实图复验。早期1280和一次1920验收被ack文件并发读写的Sharing violation中断，原子替换仍能短暂占用；开发driver现捕获IOException下一帧重试，保留300秒超时、匹配ID与accepted()状态双检查。未把早期中断/修正前画面当最终通过证据。
- 仓库6607个跟踪文件与Unity元数据检查通过；原52处VFX meta与开工快照逐文件SHA256完全一致，九徽PNG/meta不变，无场景/Prefab/Packages/ProjectSettings任务修改。Player使用真实场景及明确合法库存/转职夹具，不宣称自然运营或完整通关；未运行全量PlayMode、第二机、缺失资源故障注入或自然长局平衡。下方旧版数据仅为历史。

## 2026-11-07：九羁绊图标、铜银金与超高档镀彩

- 人类确认V3九枚图标并要求接入、按激活档位显示阶色。当前任务分支codex/synergy-icons-tier-ui，从已同步origin/main b1d55e7建立，快进保留已在GitHub的五斩e096351；不自动合并PR #95或main。原52处VFX meta逐文件比对保持，.vscode/和PackageManagerSettings.asset不提交。只改侧栏显示与开发验证入口，无场景、Prefab、包或项目设置任务修改。
- 9张256×256透明PNG与批准V3逐字节一致，必要meta与来源记录随源码。保留原徽/原合约与六枚改编设计的区别；显示颜色不烘焙进PNG。资源键UI/Synergies/{id}，无mipmap、Bilinear/Clamp、sRGB、RGBA32不压缩，不开ReadWrite。
- 完整EditMode512/512，0失败/跳过（artifacts/synergy-badge-edit.xml）。新增4例验证九个独立资源、雪怪2/5与泥岩2/4/5等真实门槛、合法游击转职7→8→9和镀彩多色族，不改人数/库存。
- 相关D3D11 PlayMode15/15，0失败/跳过（artifacts/synergy-badge-play.xml）。新增GPU透明轮廓上传、真实Campaign/SynergyTrial档位与详情阻断；回归ApprovedUi、感染弹窗/锁定、天气状态和已有羁绊详情后方输入。此处GPU纹理检查不等同完整OnGUI截图。
- 运行源码689a30e7cf47031bb8668d2ab54ada6fa26cf718：BaselineTools.BuildWindows成功，1048888761字节、0错误（artifacts/synergy-badge-build.log）。本地Builds/Windows/YesterdayConvoy.exe与SOURCE.txt交付，PreparedArt校验/构建收集保持，未创建ZIP或Release。代码与素材提交后构建，后续仅补验证文档。
- 显式-synergyBadgeSmokeTest的1920×1080与1280×720可见D3D11独立Player均29/29通过，八张完整OnGUI截图/轮；结果分别在artifacts/synergy-badge-player-1920/result.json及synergy-badge-player-1280/result.json。两轮各以computer-use实际点击游击队行打开详情、点击关闭，共4次OS鼠标点击；gui-step/ack只等待并核对外部操作，没有注入GUI事件或直接设置详情状态。实际8/9档与关闭后的彩色图标区均采得6个色相族，库存、DP、档位及详情后方输入保护通过，两轮进程均正常退出。已查看实际UI和裁切阶色对照，未把Camera-only渲染当成OnGUI证据。
- Player使用真实Campaign与SynergyTrial场景，明确的1星Shop.Preset库存夹具和正常转职操作形成未激活/铜/银/金/8彩/9彩；不直接改羁绊档位、伤害或军令值，不冒称自然运营/长局通关。未测全量PlayMode、第二机、缺失资源故障注入或自然长局平衡。仓库6587跟踪文件校验通过；九PNG/meta均跟踪，构建后原52处meta逐字节保持，ProjectSettings/Packages无任务差异，两个既有本地项不提交。

## 2026-11-07：游击自动五段、蓄力重斩与震屏

- 用户确认项目外五斩V2后授权接入。基线实时origin/main b1d55e7，分支codex/guerrilla-auto-five-strikes。保留原52处VFX meta（逐文件SHA与开始时一致）、.vscode/和PackageManagerSettings.asset；不提交这些既存本地项。无场景/Prefab/Packages/ProjectSettings任务修改。
- Unity6000.6.5f1：最终完整EditMode508/508（artifacts/guerrilla-auto-edit-final.xml）、相关D3D11 PlayMode38/38（artifacts/guerrilla-auto-play-final.xml）通过。模型覆盖真实收入自动发令、无人/控制/暂停/半令待完成不空放、所有档无次数cap、忙碌尾段防重叠、逐刀混伤/护甲/最小伤害预算、来源/目标失效、取消保留已损、精确五时点和1×2×同结果。GPU覆盖实际自动军令/9槽原作塔露拉、每刀真实HP与记录、15/40分配、蓄力保持四段、两源暂停采样/原音高、真实镜头后处理像素/三帧hold/位移/归零、原作头像加载、战役/专项/其他羁绊流程。
- 过程记录：首轮编译发现新增验收夹具误把DeployedUnit.NextHealAt写在Defender，删除无效行后编译通过。首轮Edit505/508的3失败均为float子步累计导致Age在精确2.8低约1.43微秒，内部改double累计、公开float不变后508/508，未放宽2.8断言。首轮GPU35/38：一例仍手动花已被自动消费的军令；两例严格HP夹具的旧多源弹道无法在第一刀前排空。改为检测实际自动批次、严格分支真实8档单M03首接敌补第三点自动发令，保留排空与.85秒上界后38/38。清掉旧版额外4束扫光和.975白闪，五刀仅由真实Strikes驱动。
- 最终运行源码221c515884fb05d63a2e5f3b18b43c10bd377605：Windows构建1046503985字节、0错误（artifacts/guerrilla-auto-build.log），产物Builds/Windows-GuerrillaOrder/YesterdayConvoy.exe。可见1920×1080 D3D11独立Player49/49通过（artifacts/guerrilla-auto-player/result.json），进程58912正常退出、日志完整清理。9级9种库存/两真实转职/Begin初始2点，空源不空放，实际M03部署后自动一次；未写假Orders/Hits。每守军前四段各实际21、末段56，4名守军总实际掉血560；逐段HP、累计记录、末刀更高与原作塔露拉绑定通过。
- 已查看原作九肖像、蓄力4/5、末刀5/5及实际伤害/震屏截图。两源原音/FX各在两个完整批次只播一次，暂停timeSamples冻结；单独迟观察的终局批次不补播已过声段。1×/2×峰值生产层像素一致、伤害一致，2×逐帧Age最大误差2.3841858e-7秒，普通模拟仍2×。后处理GPU开关差2073597像素，逻辑相机不动；终局保留第一段已损且取消其余，末尾两源/hold/Offset/Zoom/Roll和重开清理通过。
- 导出246张实际Player逐演出帧、60fps/4.1秒（含0.1秒前导及清理），项目外guerrilla-auto-five-in-game.mp4已完整解码检查。视频后配同一跟踪原声（.55）及纯FX（1）并对齐前导，不称实时系统音频录制。耐久守军1000HP、延后普攻/独立装置攻击治疗仅为隔离军令预算的明确夹具，不代表自然长局平衡验收。原Unity/GPU测例夹具不同，细节见代码。
- 仓库6555跟踪文件检查通过，新纯FX资源649004字节、3.38秒及Shader/meta均随源码跟踪，原followup出手声哈希不变且不重复混入。原52处meta开始/结束SHA全部一致，两个本地项未提交。未运行全量PlayMode、第二机、全分辨率或自然长局平衡，未另行主观试听；不自动ZIP/Releases或合并main。构建后仅文档补记。

## 2026-11-07：海嗣溟痕蔓延

- 最新main实时核实为b1d55e7；同一工程从同步main建立codex/marine-nethersea，再快进承接当前已可试玩76f0bd2（泥岩、牧群与银狼12个尚未进main的依赖提交），不自动合并main。原52份Mandragora/Mephisto meta与两个本地未跟踪项按既定方案原样保留，SHA256核对未变。
- Unity6000.6.5f1：完整EditMode首轮及最终均562/562（artifacts/marine-edit.xml、marine-edit-final.xml）。新增核心边界与旧适应参数退役回归；后续补充致死纯真伤接触，最终完整复验覆盖。
- D3D11相关PlayMode29个不同用例均获通过：首轮27/29暴露范围查看绑定错误及新Battle查看状态残留；修正实际combatView桥与状态同步后28/29，剩余为单格范围高亮实际相机仅17像素、低于30像素要求。增强实际边缘对比与宽度后，末轮MarineTideSceneTests 3/3（marine-visual-focused.xml）。没有降低断言。其余回归含RemainingSynergy、DedicatedSynergy、FirstSpeaker、GuerrillaOrder、HerdSpirit与BattleInteraction；最终不同用例共29，非完整PlayMode。
- artifacts/marine-balance.json：正式CampaignBalance星级/DP/守方防御、正式第6/10/13关守军及保存路线，1–3星、两种2人组合、4人组合与同4人关闭效果对照共36组。固定轮换部署、真实费用/CD/技能/神经/胜负，无注血或强制判胜。较弱第6关可能无爆发已结束；1星第10关四人首次爆发由9.2秒提前到3.6秒，第13关11.4→8.05秒；通关时间还受路线、部署预算与高台影响，不承诺总是更快。该审计不包含商店运营、成型概率或最优操作。
- 本轮新增8张原作溟痕PNG共381201字节及必要meta，Shader使用01/04/03/flow_43_2四张；另外四张保留同一原依赖来源，不宣称它们已参与画面。原版本、包MD5、CAB/PathID和PNG SHA256见art/source-records/marine-tide-original.json。原图加本作模拟时钟Shader适配，非完整原客户端材质/粒子重建。
- 最终运行源码`8322d2a145b43790a4a712009e84bb5affeeb9b1`：Windows x64构建1046241663字节、0错误（artifacts/marine-build-final.log）。实机看稿后保留原PNG字节，Shader主层改取原028_03材质冷色参数，减少灰石块观感；该Shader变更单独GPU3/3复验（marine-cold-tint.xml）。此前971f651构建0错误/14项Player通过保留为初稿记录，不冒称最终构建。
- 最终1600×900、D3D11可见独立Player14/14全True（artifacts/marine-tide-player-final/result.json），生产部署/费用/CD/真实接敌/原生神经均正常，实际鼠标点击左侧范围查看。0.55模拟秒首次落种，4.55秒首次扩散，最终16格、4次爆发，自然Won，覆盖开关在实际屏幕贡献13965个差异像素，暂停与重开清理通过，最终进程62364正常退出。专项攻击×2的首爆0.8秒不作为正式1星平衡证据；正常属性对照见36组预算审计。未注血/伤害、未用FinishForTesting证明自然结果，未把模型部署调用冒称OS拖动部署。
- 已查看真实游戏相机/屏幕中的原纹理、覆盖亮边、受益标志、神经条与爆发倒计时。图片01至06在artifacts/marine-tide-player-final；源码manifest标明只有4张纹理实际进入Shader，其余原依赖不冒称绑定。模型/界面均没有海嗣XP结算或旧适应效果。最终预算审计marine-balance-final.log正常退出，完整Edit562/562、29个不同相关GPU案例与最终冷色GPU3/3的验证范围分开记录。
- 本地普通启动Builds/Windows/海嗣溟痕.cmd（-marineTide），不会运行自动验收或自动退出；工具入口tools/play_marine_tide.cmd。保留整个Windows目录。原52份meta及本地.vscode/、PackageManagerSettings.asset SHA256再次核对原样未变、不入本任务提交。未测完整PlayMode、第二台电脑、全分辨率和正式13关自然运营平衡；不自动ZIP/Releases。

## 2026-11-07：银狼 LV.999 / 左右向与三种盲盒

- Unity 6000.6.5f1：完整 EditMode 549/549，包含三结果分别命中、资源仅结算一次、连抽同款、第三发动作锁与普攻中排队变身；D3D11 相关 PlayMode 22/22（选择、战役与羁绊卡池）。后续只调整独立验收入口的后台运行和相机取证时机；最终运行源码 eadffc0 Windows x64 构建 1,045,906,436 字节，0错误，正式美术打包 105份清单。未运行全量 PlayMode。
- 最终独立 Player 1600×900、Direct3D 11、退出0：19项全部通过。普通入口部署、原路线和显式反向路线、左右出拳、真实源石虫、手动变身、三结果各自命中、暂停和重开均已验。独立验证以固定随机种子覆盖三种结果；玩家正常游玩没有固定种子。准备50页、PNG回退0。
- 最初提前编译遇到尚未写完的表现类，随后修复验收脚本引用。第一次隐藏窗口没有推进；开启验收专用后台运行后，交换链截图全黑，不能算图像证据。改从真实游戏相机读回；修正先读状态后绘制的一帧偏差，最终8张1600×900实机图均检查。隐藏银狼再重绘，角色差异9176–20638像素；三种命中层差异12022–42727像素。该相机图不包含屏幕 IMGUI。
- 直接复验：YesterdayConvoy.exe -silverWolfSmokeTest -force-d3d11 -screen-width 1600 -screen-height 900 -demoEvidenceDir <独立目录>；普通试玩用 -silverWolfTrial。未做第二台电脑、全分辨率和长期平衡试玩。未修改场景、Prefab、Packages或ProjectSettings，52项原有VFX meta和已有本地未跟踪文件未提交。

## 2026-11-07：牧群冰蓝灵体与倒下起身

- 人类先认可项目外V1冰蓝半透明看稿，再明确授权接入与倒下起身。`codex/herd-spirit`从实时`origin/main b1d55e7`建立，FF承接当前已验收试玩`52dcad5`（未合并PR #90），不自动合并。原52个Mandragora/Mephisto meta和两个本地未跟踪项按既有确认隔离；开工保存52项字节SHA，本轮Unity后核对无变化。
- Unity6000.6.5f1/D3D11完整EditMode544/544，`artifacts/herd-spirit-edit.xml`。新增逻辑覆盖非牧群源卡也适用、来源/实体关联、不再花费部署、重组期间全门禁/暂停、恢复交战、完整10/12有效寿命、不递归、最后一张卡不提前判负与重开。
- 相关GPU PlayMode25/25，`herd-spirit-play.xml`：HerdSpiritSceneTests、RemainingSynergySceneTests、DedicatedSynergySceneTests、MonsterAbilitySceneTests、FieldAnimationSceneTests、TalulahDeathPresentationTests中实际匹配的25项。新增三项验证真实守军击杀→倒下/原尸体隐藏→起身→原动作、暂停帧/Shader/散雾几何冻结、隔离相机明暗底实际GPU蓝色/透光、多实体与死亡/终局/重开清理。
- 静态交叉审查发现并在GPU验证前修正实心接触阴影残留、自然寿命到期瞬间复亮。再增加到期不复亮与阴影归零的实际回归，`herd-spirit-expiry.xml`3/3；这是上述三个用例的复验，不累计为额外不同测试。寿命段停用守军后续攻击为明确隔离夹具，真实自然守军击杀另有用例。
- 核心模型/表现与测试提交`ef708eb958650d7036990b2f3db37edf32d382c1`，之后两次提交只修正显式验收启动与可选连续导帧，不改普通入口或模型/Shader。最终运行源码`c550c75`，Windows x64构建1045880387字节、0错误（`herd-spirit-build-delivery.log`），本地Builds/Windows/YesterdayConvoy.exe。
- 可见1600×900 D3D11最终Player，参数`-herdSpiritSmokeTest -herdSpiritMotionEvidence -demoEvidenceDir <绝对目录>`：`artifacts/herd-spirit-player-delivery/result.json`17项全True，进程27456完成后正常结束。由真实守军伤害触发首个M21最终死亡，原位置/路线、单实体倒下、冰蓝重组替换原尸体、起身期间逻辑门禁、暂停帧/Shader/散雾、恢复原动作及完整寿命、材质实际GPU差566像素均通过。之后显式致死仅为消散验证夹具，检查不递归、暂停消散、释放角色、正常重开及清空关联。已实际查看完整屏幕original/falling/rising/active/dissolved截图，不将模型字段当画面。
- 可选导帧重新使用重开后的原关卡、正常部署与自然守军击杀，保存91张30fps实际Player帧，项目外编码约3.03秒全屏与局部放大无声MP4；这是逐模拟帧预览，不是实时系统音视频录屏。前65%/最多0.65源秒的原死亡帧适配倒下/反向起身，不声称重新制作了原作骨骼动画。
- 首轮独立Player52052因本轮验收flag无条件跳过新载HerdTrial的PrepareCampaign而一直等待，停止本次诊断，不计通过；把排除限定到初始非sandbox后，第二轮Player13096的17检查已全True，但可选录制再次调用已重开的StartLoopNextBattle返回false，整轮结果仍记false。最小复现收敛到已准备战场：仅在CanShop时调用StartLoopNextBattle，CanDeploy时直接进入，最终27456全部通过。没有因此修改正式玩法或放宽任何检查。
- 仓库6566跟踪文件检查通过；该脚本不代替Unity或Player。无新增运行图片/外部依赖，原图集保持，新增Shader/C#/meta正常跟踪，来源与数据连接见art/HERD-SPIRIT.md。
- 未运行完整PlayMode、第二机、全分辨率、所有怪物逐一人工动作验收或长期自然平衡。不自动ZIP/Releases，不提交概念加工图、日志、构建或本地AGENTS。

## 2026-11-07：树海PR吸收资源分类后的复验

- 运行源码83c783394468d8d2297df6eac843937b601e0a09，已吸收main b1d55e7；四份文档新增段落全部保留，原场景与门资源不回退。
- merged-edit-results.xml：完整EditMode510/510、0失败/跳过。merged-play-results.xml：BlackForestSceneTests、ArtLoadingTests与MonsterSelectionTests共10/10，0失败/跳过；不冒称全量PlayMode。
- 仓库7387跟踪文件静态检查通过。merged-build.log：Windows构建1058042785报告字节、0错误；实际输出目录约1.37GB，报告汇总不等于目录总体积。
- merged-player.log：可见1280×720 Direct3D11 Player，三路真实备战指针部署与完整模拟走线，geometry/materials/nativeGates/fixedRoutes/pause/sharedDefense/restart全部True，退出0。已查看merged-player/forest-ready.png，原地形、三红两蓝和弩手备战正常显示。保留显式Tick(.05)步进条件，不把它当成人工完整长局测试。
- 本次复验不重测main上所有新增角色的完整战斗；原门装饰动画、第二机、低配长局、平衡与新克隆试玩仍未验收。

## 2026-11-07：树海原素材与原版门接入

- 运行源码bcd3417，独立BlackForestCrossing场景。原素材看稿确认后接入；原地形238格与路线38/40/42不变，无碰撞/射线或地形效果新增。
- Unity6000.6.5f1全量EditMode449/449；GPU PlayMode BlackForestSceneTests 1/1，覆盖真实拖动部署、商店交易、暂停、地块编辑后重开保留及退出清理。XML在本机artifacts/map-draft-20261106/native/edit-results.xml、play-results.xml。
- NativeAssets验证通过：3 tile_start/2 tile_end、原网格/材质/贴图持久化，无LineRenderer占位门，无碰撞体；原临时门已替换。142环境模型及8门层部件保留原UV/法线，门保留顶点色，兼容Shader透明与加色。
- 仓库6655跟踪文件检查通过；Windows构建0错误（windows-build.log，报告2430901013 bytes，含构建阶段数据，目录实际大小以交付BUILD-INFO为准）。
- 可见1280×720 Direct3D11独立Player，正常准备入口后三个红门逐一通过备战指针拖动部署，沿完整路线自然穿门；固定路线/绑定蓝门、暂停、共享防御、重开与nativeGates/materials均True，退出0。截图player/forest-ready.png、forest-lane-1/2/3.png、forest-restarted.png已目视核对角色与地图。模拟测试使用Time.timeScale=0并显式Tick(.05)，不注入伤害或跳过路程；不将其称为人工完整长局试玩。
- 原门装饰粒子与动画、原场景Shader/烘焙光照/后处理未复刻；守军、平衡、第二机/新克隆、低配长时性能未验收。未将项目外看稿作为Player证据。

## 2026-11-07：StreamingAssets源资源分类与构建交付

- 分支从实时`origin/main b3d947a`建立，FF承接当前试玩`8ca1c11`及未合并#88依赖；人类已确认既存52项特效meta原样保留。本任务不暂存/提交这些改动，也保留.vscode/与PackageManagerSettings.asset；不改场景、Prefab、Packages或ProjectSettings，不自动合并main，不创建ZIP/Releases。
- 1644个源文件按图片/JSON分类迁移，StreamingAssets仅保留说明。迁移前后逐文件SHA256和GUID核对通过；独立Git blob核查1527张PNG与1848个既有文件/目录GUID保持。117份JSON含103份运行manifest及14份来源/维护记录，原图/清单/必要meta全部随源码跟踪；来源及权利状态不因迁移改变。原DefaultImporter已由Unity真实导入为TextureImporter（图片）/对应文本导入器，GUID未重建；首次导入的版本升级提示不冒称C#编译失败。
- `artifacts/asset-layout-edit.xml`：Unity6000.6.5f1完整EditMode **504/504**，0失败/跳过。新增3项覆盖分类清单/不解码、ExtraClips/阶段白名单与陈旧缓存排除、拒绝向工程源目录导出。原塔露拉19段增量和41页阶段断言保留；缩略图指纹包含裁切模式及cell，避免同PNG改清单格子后复用旧裁切。
- `artifacts/asset-layout-play.xml`：D3D11相关PlayMode **32/32**，0失败/跳过。涵盖ArtLoading、MonsterSelection、Campaign、ShopMap、DefenderVfx、MonsterOriginalVfx、OriginalCombatVfxLayer、TalulahScene/Phase/Attack/Death；不是全量PlayMode。准备阶段实际复用1487源PNG缓存，重建64张v2指纹缩略；动作帧/模拟时序和原PNG内容未改。
- 运行源码`b3b7e2b73744ff9435495f09e45d61c0e24280b1`，`BaselineTools.BuildWindows`真实成功、退出0、0错误（`artifacts/asset-layout-build.log`）。BuildReport汇总1,045,805,952字节；此值不包括后处理另外复制的资源，不将它与旧版本汇总值直接比较为包体缩减。后处理按manifest生成1655文件：1551个ycart、103个manifest、1个商店PNG，共316,257,980字节；没有复制14份来源记录、37张未绑定塔露拉页或旧Library散落缓存。有效ExtraClips继续交付。项目源文件保留，不要求队友另传缓存。
- `artifacts/asset-layout-build-audit.json`：独立按源manifest重算预期并逐文件比较构建产物/源JSON或缓存SHA256，1655文件缺失0、多余0、字节差异0。`-artPayloadSmokeTest`开发Player真实逐页读取和GPU上传全部1551纹理，六大包均有manifest和图集，商店PNG实际解码，PNG回退0、错误0、退出0（`asset-layout-payload-player/result.json`）。这是加载验证，不是1551项独立玩法测试。
- 可见1280×720 D3D11 Player：`-nativeVfxSmokeTest D27` **8/8**，真实事件/原生Sprite/Shader与尺寸/暂停/实际GPU变化/不写伤害/重开通过，10282个原生层开关差像素；`-talulahManualRainSmokeTest` **13/13**，自然承伤双倒下与最终死亡、B阶段手动火雨、手焰换帧/暂停/恢复/自然消散及六秒雨结束通过。已查看D27全屏和塔露拉B火焰展开实图；没有把测试夹具或脚本Tick冒称OS鼠标操作/自然平衡。日志与PNG分别在`artifacts/asset-layout-native-player`、`asset-layout-talulah-player`。
- 维护脚本实际验证：5套PowerShell安装器解析、2套Python AST、动作包只读12段/1167帧/25页+2隐藏等待态、native只读39段/4218帧/83页，源报告哈希/帧SHA/逐像素/网格/填充/枢轴通过。未执行重新安装或重打包写入。
- 本地交付`Builds/Windows/YesterdayConvoy.exe`，应保留整个Windows目录。未运行第二机离线、所有分辨率、完整PlayMode、长局平衡或人工完整通关。本轮没有早期美术删除授权：项目外12组/213资源预览保留，A06/A08/A10/A11共25资源约4.37MiB仅静态未发现引用，等待人类看稿确认后另行删除并验证。

## 2026-11-06：塔露拉转职追击头像修复

- 原因：ShowGuerrillaOrder经CardDefinition.Portrait读取转职肖像，通用getter漏了OriginalTalulahArt分支，因而回退到Talulah.asset序列化的旧TalulahComic/V2卡面；原作PNG及PreparedArt源哈希正常。现优先读取已有TalulahOriginal/portrait，不新增/替换图片，不改场景、Prefab、配置资产或GUID。
- Unity6000.6.5f1：完整EditMode501/501（artifacts/talulah-pursuit-edit.xml）、相关D3D11 PlayMode26/26（artifacts/talulah-pursuit-play.xml）通过。覆盖原游击高档/恒速全场演出、塔露拉阶段/攻击/死亡，以及冷读原作单头像≤1纹理、其他卡序列化回退。新增真实九种编队准备→两转职→Begin赠令→M03远距部署→成功军令，0.65秒实际第九槽Sprite等于原作；实际RenderTo与显式原作供图像素相同，与旧serialized图在塔露拉区域显著不同。
- 运行源码728c37de7aa73b9d1b01774906a0fdf61f5c1f78：Windows构建2449477554字节、0错误（artifacts/talulah-pursuit-build.log），本地Builds/Windows-GuerrillaOrder/YesterdayConvoy.exe。可见1920×1080 D3D11独立Player9/9通过（artifacts/talulah-pursuit-player/result.json），进程9136正常退出；实际第九槽为TalulahOriginal/portrait，112324个区域像素与原作对照差0、与旧图差98565。已查看talulah-pursuit-peak.png，左下为当前原作塔露拉。真实只执行一次军令并消耗开战2点，其余三次Play仅为同年龄纯渲染对照与恢复，不新增军令/伤害。
- 仓库6429跟踪文件检查通过，必要新脚本meta已跟踪，现有原作图片/meta持续跟踪。构建产生的52处默认未覆盖WebGL导入噪声逐文件严格比对后恢复，仅保留原.vscode/和PackageManagerSettings.asset未跟踪项。未运行全量PlayMode、第二机或缺失/损坏原作媒体故障注入；未重新生成美术，不自动ZIP/Releases或合并main。构建后仅文档补记。

## 2026-11-06：游击8/9档、整令无次数上限及真实转职

- 人类已确认8/9草案并覆盖为所有档位取消整令次数上限，后续明确本轮加入准备阶段手动指定最多两种转职角色。保持原功能分支/PR #88，实时main仍b3d947a，两个既有本地未跟踪项保留。没有改场景、Prefab、配置资产、Packages或ProjectSettings。
- 完整EditMode501/501（guerrilla-high-edit-final.xml）、相关D3D11 PlayMode48/48（guerrilla-high-play-final.xml）通过。核心新增11个case证明2/4/6/8/9各可超过原次数并仅受资源限制、9远距先手/8补点、20普通实体全参与、技能/控制/支援排除、普通动作/CD不重置、开战赠令唯一、6档半令循环、额外成员快照原子/去重/锁档。新增4个综合Play证明真实转职生命周期、正式补给→实战→跨关、真实积令支持第5次整令、9张真实肖像与备战空槽不挤位、8/9实际HP和恒速音画；回归商店/跨关/9专项与旧羁绊。首次编译修正详情滚动区局部变量重名；首次场景46/48两失败是补给关已Won无法开发判胜、长HP窗口混入留声机治疗，改走正式补给流程及.89→.91命中瞬间后保留严格断言。最终Windows与真实点击Player结果如下；下面旧条目仅代表历史。


- 运行源码774743815c70b91120bf24f882ba8a4846d3d15d：Windows构建2449462995字节、0错误（guerrilla-high-build.log），本机Builds/Windows-GuerrillaOrder/YesterdayConvoy.exe。仓库6427个跟踪文件/meta检查通过，新增规则/转职/验证脚本meta完整；现有图集、原声和角色卡面复用，未增加位图或音频，原PNG/音频字节与GUID保持。每轮52处仅默认未覆盖WebGL导入噪声均经逐文件严格比对恢复，两个原本本地未跟踪项保留。
- 1920×1080渲染的可见D3D11独立Player32/32（guerrilla-high-player/result.json）通过。通过computer-use对实际窗口完成5次点击：构筑入口→转职tab→指定士兵M07→指定弩手M08→关闭，侧栏实际7→8→9、两个已购卡显示徽标，prepare-eight-tier/prepare-nine-tier截图确认；不是只调SetGuerrillaTransfer接口冒充GUI。验证程序gui-step/ack只等待与核对外部点击，不注入GUI事件。
- 8档开战2点不能整令，第一次真实接触补到3后按180%整令扣3；9档未接敌、尚未普攻的单源即可耗2先手，全场4守军在.9演出秒各实际损失140、合计560，严格.89→.91 HP窗口与Hits相等，后续留声机治疗正常保留。真实额外收入10点加初始2点支持6次整令，随后余额0拒绝；军令派生不产点，重复Begin/同场同步不刷2，新场Begin恰给2一次，转职跨同shop重开保持。8/9只整令，战斗锁定成员，固定七原生+两真实转职肖像、2×恒速/原声pitch1、真实声音样本暂停与全场数字均通过。
- 可选输出204张实际Player逐演出帧，9名普通来源、9张肖像、2×标头及完整2.6秒演出。项目外guerrilla-nine-tier.mp4为60fps/3.4秒实机逐帧，后配同一原声资源0.55增益、视频1.0秒出手，非实时系统录音；视频是另一次九来源出手，界面总伤害11743为取整显示，不能与上述单源严格窗口560混写。已看准备界面、九肖像峰值和实际扣血图，最终进程45240正常退出。
- 未运行全量PlayMode、第二机、全分辨率、长局自然平衡或另行主观试听。9级9种库存、耐久守军、延后守军攻击/技能和测试用补给/结算是明确夹具，部署、真实接触收入、整令支出和HP仍走生产逻辑；无人工写Orders/Hits。转职按种类、免费整备指定，不涉及道具掉落或跨挑战存档。转职肖像复用原卡面，未重绘高清头像。构建后仅文档/来源记录更新，不自动ZIP/Releases或合并main。

## 2026-11-06：游击演出恒速与真实全场追击

- 人类本轮明确二倍速不影响演出，并回答“全场守军都实际受伤”。沿用原分支/PR #88，实时main仍b3d947a，开始时仅有已约定保留的两未跟踪项。模型统一全场命中与演出反馈在本轮授权范围，未改场景/Prefab/Packages/ProjectSettings。
- Unity6000.6.5f1完整EditMode490/490（guerrilla-global-edit.xml）、相关D3D11 PlayMode27/27（guerrilla-global-play-final.xml）通过。新增5个模型case与3个综合GPU场景case；覆盖全场/远路/高台、混合物法与减免、真实HP预算、.89前无伤/.9统一一次、半令/幂等/来源死亡/暂停/终局取消/重开，以及1×/2×同实际时间同动画像素、战斗仍2×、原声pitch1、真实命中数字独立GPU贡献。首轮26/27唯一失败是多目标用例误选双守军泛用场景；改为实际四守军GuerrillaTrial，保留提前死亡后仍多目标的断言后27/27通过。最终布局将合计条移到下方空区，GPU重跑27/27（guerrilla-global-play-layout.xml）通过；下面V2记录为旧版历史。

| 项目 | 采用来源 | 真实触发/时钟 | 运行读取点 | 当前状态 |
|---|---|---|---|---|
| 九角色切面 | 已确认V2原图/几何 | 成功军令编队快照；独立演出时钟 | GuerrillaOrderView | VERIFIED，GPU/Player恒速通过 |
| 原总攻音 | 已确认原Wwise声段 | 0.9演出秒；1×/2×同音高 | followup-original/UpdateSound | VERIFIED，样本暂停/音高通过 |
| 全场实际伤害 | 本轮人类明确规则 | 模型GuerrillaOrderVolley；Advance双delta | 每个存活守军DamageDefender | VERIFIED，模型/GPU/Player实际HP通过 |
| 扫击/受伤数字 | 现有原生网格/字体 | 只读同批实际Hits；重开/终局取消清理 | BattlefieldImpact/BindVolley | VERIFIED，GPU开关/Player实图通过 |


- 最终运行源码e747ff0bd9183ea924fba28166e128b06e1a2f46，模型/核心测试代码与c2c534b的Edit490/490完全一致，最终GPU27/27；Windows构建2449425852字节、0错误（guerrilla-global-build-final.log），本机Builds/Windows-GuerrillaOrder/YesterdayConvoy.exe。没有新增图片/音频资源或改GUID，新增群攻模型及meta随源码跟踪，仓库6417文件检查通过。
- 1920×1080可见D3D11最终Player28/28（guerrilla-global-player-final/result.json）通过：.85前HP未变/原音未播，.9统一真实全场结算，四个guard各实际损失927.8125、合计3711.25；1.3秒目标数字/合计与记录一致。2×战斗时Tick(.05)推进Clock .10、演出Age .05、原音pitch1；捕获第84帧Age79/60仍在播放，完整2.6演出秒后清理。实际全屏七点覆盖、缺位/扩展、暂停/重开、终局取消不扣血与尾段均通过，已看全场命中实图；合计下移避免遮住头顶数字。
- 最终捕获204张实际Player逐演出帧，2×标头，60fps/3.4秒；项目外guerrilla-global-2x.mp4后配同一跟踪原声、增益0.55，在视频1.0秒出手（0.1前导+0.9演出），不是实时系统音视频录制。耐久/延后普攻、自然排空旧弹道仅为真实军令独立验证夹具，不假造Orders或Hits；源码军令伤害不受该夹具影响。最终进程54692正常退出，未在本轮重现下方旧版导帧UIAutomation退出异常。
- 未运行全量PlayMode、第二机、全分辨率及长局自然平衡，未另行主观试听。全场每目标预算保留原档位倍率属于本轮试验实现，尚未宣布平衡定案；原参与者资格仍需已有合法近战/射程目标，未部署拼屏角色不贡献伤害。最终构建后仅文档/来源记录变化，不自动ZIP/Releases或合并main。

## 2026-11-06：游击队V2真实几何汇聚与原音

- 用户在项目外V2动态视频看稿后明确“可以，做进游戏”，确认声段为拼屏碎裂/总攻击出手。沿用codex/guerrilla-fullscreen-cutin及原PR #88；实时main仍b3d947a，当前V1基线c8a3281，原两个本地未跟踪项不动。没有改场景、Prefab、Packages或ProjectSettings，不改军令事务及伤害。
- 原音源为Ev_sfx_GridFight_FollowUp_Timeline的Double_Speed媒体745947550前0.90秒，和已批准V2样本字节一致；末30ms原有淡出，PCM16/48kHz/双声道，峰值-1.14807dBFS，RMS-12.366999。只读原包获取及两次公开视频波形匹配见art/source-records/guerrilla-original-audio.json。原合成音和生成器退役，不把用户接入/上传授权登记为权利人授权。
- 最终运行源码a5b844e9fb0a2d482f3423ab03ed50cb4e0b3835：Unity6000.6.5f1完整EditMode485/485（artifacts/guerrilla-v2-edit-final.xml）、相关D3D11 PlayMode24/24（guerrilla-v2-play-final.xml）通过。4个综合游击用例验证真实交战产令/成功快照、失败不播、九槽逐个缺位/两扩展GPU、切面真实外移到聚齐、108碎片几何、原声0.90秒只播一次、实际AudioSource样本/几何/GPU暂停冻结、2倍/0.03倍、重开与终局不再复闪。终局表现时钟保持单调，避免PendingUntil消失后回退。
- 早期独立Player两次暴露完整效果仅出现在顶部窄条，离屏参考七点均为零，显式modelview重置没有解决。缩小到不含战斗的RenderProbe后，改为独立CommandBuffer明确目标/全viewport/关闭scissor/独立矩阵，每batch用PropertyBlock绑定纹理，LateUpdate先合成，OnGUI只绘制RT；同时修正原生字体取Alpha。GPU增加继承变换/状态恢复及22px硬件scissor回归；最终无战斗Probe1/1通过，不再把早期离屏通过冒称屏幕正确。
- 最终Windows构建2449412581字节、0错误（guerrilla-v2-build-final.log），本机Builds/Windows-GuerrillaOrder/YesterdayConvoy.exe对应a5b844e。1920×1080可见D3D11 Player25/25通过（guerrilla-v2-player-final/result.json）：实际攒令/支出、未部署M20、满编/退编M19空位、聚拢/碎裂/独立消散/清理、0.9秒原音及真实暂停样本、2倍速、重开、终局跨声点且HP/Clock不变、尾段结束后不复活。完整屏幕在顶部按钮/血条/通知/底部卡栏七点对照生产合成通过；已查看实际满编、缺位、碎片消散和恢复战场PNG。
- 可选入口输出204张真实Player逐模拟帧（60fps/3.4秒，含前导和清理），项目外MP4后配同一跟踪原音资源及运行增益0.55，不是实时系统音视频录制。图片、原音、Shader和必要meta随源码正常跟踪；完整仓库6415文件检查通过。每次Unity产生的52个默认未覆盖WebGL导入段均逐文件与HEAD严格比对后恢复，原两个本地未跟踪项保留。
- 额外退出复验：导出204帧的50508进程在25项完成后退出时曾残留，正常CloseMainWindow后日志出现UIAutomationCore/UiaDisconnectAllProviders、USER32/COM及UnityPlayer原生异常栈；仅清理了本次验证进程。相同EXE随后不导帧复验24/24通过（guerrilla-v2-player-exit/result.json），进程4012正常退出，日志有完整CodeReloadManager/PlayerConnection清理。未定位该次原生异常根因，不将其登记为已修复，也不归因为特效脚本或截图；第二机未验。
- 未运行完整PlayMode、第二台电脑、全分辨率、长局自然平衡或另行主观试听。耐久守军/延后攻击和显式终局仅为开发夹具，部署、命中、攒令与军令支出仍走实际事务。构建后仅更新文档和来源验证状态，不自动ZIP/Releases或合并main。下面V1结果仅为历史。

## 2026-11-06：游击队V1历史技术结果（表现已被否决）

- 用户已认可V1构图，明确“按当前编队显示”，缺席成员固定空位、预留两个转职槽、淡入淡出与短音效。新分支codex/guerrilla-fullscreen-cutin自实时main b3d947a建立，FF承接当前试玩0e98e1d依赖；原.vscode/和PackageManagerSettings.asset保留。没有改场景、Prefab、Packages或ProjectSettings，没有实现转职玩法或更改军令数值。
- `artifacts/guerrilla-order-edit.xml`完整EditMode485/485；`artifacts/guerrilla-order-play.xml`相关D3D11 PlayMode23/23。新增3个综合场景用例验证真实部署/普通攻击产令后成功才播、准备/暂停失败不播、完整编队包含未部署M20、普通点击把M15退入备战后固定空位、暂停时实际AudioSource.timeSamples与透明度不推进、2倍速、自然淡出和重开。GPU使用生产材质逐个缺席七名人物，对应人物区域变化且其余人物区域不变；真实Sprite显示在两扩展槽，第3名不挤掉原成员。其余回归AdvancedSynergySceneTests、CampaignSceneTests、BattleInteractionTests、OverlappingEntrySceneTests；不是全量PlayMode。
- 新测试和显式Player入口使用百万生命、延后守军攻击的运行时夹具，部署、普通攻击、军令积累与支出走原事务；没有写军令值或强制触发成功，不作为自然难度/平衡证明。首个Player在退编后取源夹具未收敛且隐藏窗口截图全黑，未计通过；改保留原守军坐标并按正常CD顺序部署两轮，嵌套异常保证写失败结果。没有改军令值/伤害或放宽成功条件。
- 可见Player首次发现HUD、血条和卡栏盖在闪现图上。先改完整Shader合成到复用RenderTexture并用GUI队列绘制，相关图形重跑`guerrilla-order-play-final.xml`23/23，但完整屏幕仍暴露排序被撤销；缩小到GUIBehaviour的depth在finally被恢复，改每个GUI事件保留本组件深度。增加真实屏幕与生产材质在顶部按钮、血条、中央通知和底部卡栏的7点像素对照，不只检查离屏素材。该最终排序改动由下一条Player补验，没有把先前23项冒称成最新排序的全量GUI验证。
- 最终运行源码`6fadf6d`的Windows构建2449336168字节、0错误（`artifacts/guerrilla-order-build-layer.log`）。1920×1080 Direct3D11独立Player`artifacts/guerrilla-order-player-layer/result.json`19/19通过：真实产令成功一次、失败/暂停失败不重播、未部署M20仍显示、七成员和两个空扩展、淡入/峰值/淡出、实际声音暂停、2倍速、自然消失、重开、正常退编后原槽空、终局表现不推进战斗，以及完整屏幕实际遮住HUD。已查看满编、右上缺位、淡入及淡出PNG；不是只看离屏Shader或反射字段。
- 可选逐帧入口输出66张实际Player画面；项目外`guerrilla-in-game-v1.mp4`按60fps编码，并后配同一音效资源及运行音量0.55，方便用户看淡入淡出/试听；它是逐模拟帧预览，非实时音视频录屏。最终未重跑完整PlayMode、第二台电脑、全分辨率、长局自然难度或主观音色验收。源码构建后仅补文档，运行文件与6fadf6d相同；本轮52处仅默认未覆盖WebGL导入段经逐文件比对恢复，不覆盖他人改动，原两未跟踪项保持。仓库6414个跟踪文件与Unity元数据检查通过。
- `shing.wav`原创程序合成，无原作采样或人声。ffprobe/ffmpeg核对0.64秒、48kHz双声道PCM16、峰值约-6dBFS、0削波、首尾静音和重复生成SHA一致。作者未实际试听，不称为精确复刻参考音色。参考货币战争公开视频未成功实际播放，.72秒节奏是本项目试验值。

## 2026-11-06：感染集群误关窗与重新达档弹窗

- 旧843a34e运行源码的1920×1080可见Player，Sky实际点击左侧感染行后点击集群主按钮，窗口立即关闭且主形态仍为爆裂，已复现（infection-choice-before-player/reproduction.json，复现后主动结束本次诊断，不算通过）。原因是GUI矩阵前后的鼠标坐标被重复换算，左列落在面板边界外。只改私有DrawCampaignControls取外层正确逻辑点，原形态事务保留。
- 感染当前档位在SyncShopDeck成功刷新后比较，2/4/6上升边沿弹，下降更新观察值但不弹；普通同档重建/刷新/换形态不弹。StartCampaign新挑战重置，普通换关/返回整备保持。复用既有面板，无新美术、规则数值或场景变动。
- infection-prompts-play.xml：相关D3D11 PlayMode32/32通过。新两项使用实际编队卡点击处理完成6→4→6重达，以及全退编到0再逐一回编1/2/3/4/5/6的提示/同档边界；初始6自动打开、选择集群后面板保持、刷新/形态重建不重复、开战选择与换预置拒绝、返回整备同档不弹、新挑战重弹均通过。关闭弹窗为明确UI完成反射夹具；不直接写档位或编队。实际鼠标另验，不将处理器测试冒称OS点击。原爆裂测试完成初始选择后继续核真实命中、GPU、暂停/终局/重开；回归九场景、其他羁绊、商店/UI、Campaign和拖动。
- ff1e84de6ad05ca5a9820560904df2ee18733c03运行源码Windows构建2444461405字节、0错误（infection-prompts-build.log）。1920×1080可见D3D11独立Player16/16（infection-prompts-player/result.json）：实际鼠标选择集群后立即显示“主集群”且窗口保持，再选择甲壳/爆裂及副形态，低档禁用与战斗锁定通过；初始6自动打开、直接编队夹具降4不弹/恢复6自动重开也通过。2/4/6逐一退编/回编的操作处理器路径由上述场景测试验证，未冒称全部OS鼠标操作。旧爆裂两目标实际85伤害、786个独立效果开关差像素、暂停/尾段/重开复验通过，沿用明确耐久靶/显式致死/终局夹具，不作自然平衡证明。已核对即时集群选中画面与保存的形态/低档实图。
- 本轮未重跑EditMode或全量套件；第二机、所有分辨率和长局平衡未验收。分支codex/infection-choice-prompts从实时main b3d947a建立，FF承接当前试玩0f8ee06（未合并#85及其依赖）；原两个本地未跟踪项隔离保留，不自动合并。52处仅默认WebGL导入段逐文件比对后恢复；仓库6388文件/meta检查通过。构建后仅补验证文档，运行文件与ff1e84d相同。

## 2026-11-06：塔露拉B阶段手动火雨卡帧

- 原因在TalulahAttackVfx.PresentUnit：B点火层与身体抬手共用hold=1.1666667，六秒动作的0.8–5.4秒冻结在ignite-B-caster/035。原素材59帧有变化，不是全局Clock停走。仅B附着层改为读取本次施法的真实经过时间，按原速单次1.9667秒完成爆发/消散；保留身体抬手和六秒地面火浪/移动锁、A表现、伤害/SP及全部阶段。
- 先加真实场景回归，修复前talulah-manual-before.xml 0/1，明确在1.25→1.5秒仍同035帧处失败；未放宽断言。修复后talulah-manual-play.xml相关D3D11 PlayMode11/11，包含自然克洛丝伤害→首倒/自动雨→B、自然技力满后手动施放、实际附着层GPU开关差、换帧/地火位移、暂停/恢复、2.1秒短层清除而火雨/锁仍在、4秒持续、6.1秒清除与普攻恢复、重开，并回归原A攻击/死亡/橙雨/地图适配。
- talulah-manual-edit.xml相关EditMode13/13，回归两次倒下、10波自动雨、保护、取消/死亡、星级开关、手动六波/SP及暂停。没有重跑全量Edit/Play或正式平衡测试。独立Player复用SandboxFeaturesEvidence自然阶段路径，补B手焰换帧/暂停/消散与六秒结束检查，新增显式-talulahManualRainSmokeTest仅运行塔露拉段；普通启动不执行。
- 运行源码843a34eb85be1dc286511784fc7a5872ee6c61f3的Windows构建2444460790字节、0错误（talulah-manual-build.log）。可见D3D11独立Player13/13（talulah-manual-player/result.json），以-sandboxFeaturesSmokeTest配合-talulahManualRainSmokeTest运行塔露拉自然阶段段落：克洛丝真实射击约12.15秒首倒、45.15秒B、77.25秒终倒、110.25秒真死亡；自然SP就绪后手动施放，手焰可见/换帧/暂停恢复/单次消散而火雨继续、六秒正常结束均通过。已查看1.25秒、1.5秒与2.15秒实机截图，确认爆发→展开→消散而非保持大贴图。不是鼠标点击或全九场景重验。
- 分支从实时main b3d947a建立、FF承接9d90d1e的现有试玩依赖，不自动合并；仅本轮表现/证据/回归与状态文档变动，不改场景、Prefab、Packages或ProjectSettings。52处仅默认未覆盖WebGL导入段经逐文件比对恢复；仓库6388文件/meta校验通过。原.vscode/与PackageManagerSettings.asset保留不提交覆盖。全量测试、第二机、密集多塔露拉并发和长期自然平衡未测；构建后仅补文档，运行文件与843a34e相同。

## 2026-11-06：感染形态与爆裂反馈接入

- 人类采用外部V1并确认共享接口/UI/反馈范围，codex/infection-feedback自实时main b3d947a建立并FF承接#83的68bbc2b依赖；保留原两个未跟踪项，不改场景、Prefab、Packages或ProjectSettings。
- `infection-edit.xml`完整485/485。新增8项覆盖只读源信息/40法抗与三星、实际致死截断、目标数/边界/死者、暂停/重复死亡/重开、零命中、固有90物爆与羁绊35法爆独立、集群副体不重复爆裂。命中结果来自原结算，不通过显示层猜测。
- `infection-play.xml`D3D11相关28/28。新增实际感染场景运行时耐久守军拷贝，原部署/最终死亡/0.2秒延迟触发两目标各85；检查预告、独立效果GPU开关、实际命中标签、暂停几何/时钟/伤害、终局1.2秒短尾及重开。运行时守军改数值用于明确夹具，原资产健康值保持。回归包括原感染/牧群自然受击触发、独立九场景、GUI遮挡/商店/拖动及原特效地面投影；并非全量PlayMode或自然整局平衡。
- 最终运行源码af5c1899fab3c505c8f4860c24030e8005161c5a的Windows构建2444458990字节、0错误（infection-build-final.log）。可见D3D11独立Player13/13（infection-player-verified/result.json）：使用真实鼠标逐次点选三种主形态、不同副形态、4档禁用项和战斗锁定项；不是调用选择API代替GUI。已检查shape-choice-player、shape-four-tier-player及infection-burst-player实图，独立爆裂层开关差786像素。
- Player爆裂段为明确开发夹具：猎狗走原部署事务，在保存路线的可见位置承受显式致死；两名100000生命耐久守军在真实半径内、其余移远并延后攻击，原事件结算两次实际85法伤/合计170。暂停延迟不推进、恢复只结算一次、原特效/实际命中标签、终局表现尾和重开通过。终局调用既有显式验证入口，不冒称自然通关或正常数值平衡；界面选择段使用真实场景与原选择事务。
- 初始PowerShell点击辅助因Windows前台限制退出，不算通过且已从最终交付删除；首个Sky实机尝试超过旧30秒等待失败，随后仅将开发证据等待改为300秒、超时写失败并退出，重新执行上述最终Player通过。没有放宽游戏或伤害断言。52处仅默认WebGL导入噪声经逐文件比对恢复。仓库6388个跟踪文件与Unity元数据检查通过。构建后只更新文档并移除不可用辅助脚本，运行文件未变；原两个未跟踪项保留。第二机、全分辨率和多单位爆裂密集叠加的长期主观可读性未验收。

## 2026-11-06：暴风雪伤害与5档冰封触发

- 人类确认5档双触发与每波“60+目标最大生命2%”法伤试验值；沿用codex/snowstorm-feedback / PR #83，main仍b3d947a，原两个未跟踪项继续隔离。不新增美术，复用已通过的风雪与真实伤害反馈。
- `snow-damage-edit.xml`完整EditMode477/477：四波法伤、平均星级/40法抗、真实扣血反馈、冻结保护不免伤、死者跳过、2/4档霜星冰封不触发、5档冰封可触发、自动环不触发、部分寒意清零、天气期间第二实体开大不续时/不清已有脉冲、三次碎冰原路径、暂停/重开/无第五波均通过。
- `snow-damage-play.xml`D3D11相关图形/交互17/17通过，包括无霜星部署的真实三次碎冰、四批天气GPU差、暂停顶点/倒计时、天气淡出、提前终局/晚观察清理、重开、Campaign与拖动入口。
- 运行源码7b8cae9b48a607a60e2c50be0f0cfa397aad10fc Windows构建2444426724字节、0错误（snow-damage-build.log）。可见D3D11独立Player16/16、天气开关差81520像素（snow-damage-player/result.json）。原14项外新增实际全场扣血反馈、仅部署霜星自然充满SP后手动冰封启动天气；后一条5档、0次碎冰、1次角色技能。已查看frostnova-weather-start截图的天气、提示与实际就绪消耗，不将截图当自然通关证据。
- Player采用既有显式开发夹具：近门百万生命耐久靶、延后守军攻击/技能、第一条路径提供寒冷状态；真实普通攻击和手动技能仍走正常模型，未写天气计数或私有启动。伤害表达式与法抗/死亡/保护边界由领域测试精确核对，不以夸大的夹具血量认定正式平衡。场景、Prefab、包、工程设置不改；52处仅默认WebGL导入噪声经逐文件比对恢复。原两个未跟踪项保留；自然整局平衡、第二机、长期极端组合未验收。

## 2026-11-06：暴风雪三次碎冰触发修订

- 人类明确确认“雪怪5档累计3次羁绊碎冰→自动8秒暴风雪”；继续codex/snowstorm-feedback / PR #83，实时main仍b3d947a，保留#82依赖和原两个未跟踪隔离项。
- `snow-charge-edit.xml`完整EditMode468/468。正常普攻与明确冻结状态夹具验证1/2/3进度、单次三目标碎冰仅计1、2档不蓄积、霜星冰封/自动冰环不触发、天气期间不蓄积或续时、结束后新三次、8秒四脉冲且天气不直接扣血、角色死亡及暂停/重开。
- `snow-charge-play.xml`D3D11相关PlayMode17/17。实际SynergyTrial五种战前快照，仅部署M11/M12，以TryApplyCold状态夹具提供冻结，真实不同原卡普攻产生三次碎冰；未直接写寒意计数、未调用私有天气启动，也未改变角色或守军资产。图形差异、暂停顶点/倒计时、自然尾段、提前终局/晚观察、重开及现有Campaign/拖动交互通过。
- `snow-charge-reset.xml`补充“累计1点后重开，再真实碎冰只得到新1点”1/1通过。当前469个领域用例按完整468加新增1项的最新结果均通过，未称单次全量469/469。
- 运行源码6a548f9314b1dfdc85f4b1dc5d890cec078bd072 Windows构建2444424328字节、0错误（snow-charge-build.log）。可见D3D11独立Player14/14，四批天气独立开关差81520像素（snow-charge-player/result.json），已检查charge-two-of-three和weather-running图。检查真实三次碎冰、未部署霜星、归零、四批天气、实际提示、暂停、恢复、2倍、期间不积、提前终局清理、重开及8秒四次寒潮/尾段。
- 开发Player证据入口使用明确近门耐久靶/延后守军攻击及寒冷状态夹具，真实普攻产生碎冰；终局沿用现有显式调试按钮，不是自然失败证明。没有修改角色/守军资产、场景、Prefab、Packages、ProjectSettings；52处默认WebGL导入噪声经逐文件比较恢复。原两个未跟踪项保留。自然阵容触发频率与整局平衡、第二机、极端并发技能长期测试尚未验收。

## 2026-11-06：雪怪暴风雪反馈

- 人类采用项目外动态看稿及明确接口范围；从实时main b3d947a建立codex/snowstorm-feedback，带入未合并#82的9d12217依赖。保留原.vscode/及PackageManagerSettings.asset，不提交覆盖。
- `snowstorm-edit.xml`全量466/466；新增/扩展真实不续时、到期可重触发、暂停、霜星取消/复起/最终死亡不撤销天气、2档不触发和只读起止/剩余时间。原脉冲/保护/伤害不变。
- `snowstorm-play.xml`相关D3D11 19/19。新两项从真实五人阵容与霜星蓄SP施法开始，核四批天气Mesh的独立GPU开关差、显示不改Clock/HP、暂停顶点/剩余时间冻结、恢复运动、8秒四脉冲及0.65尾、重开。提前终局使用明确FinishForTesting夹具，核HUD立即隐藏、0.5秒表现尾、Clock/HP不再推进；不是自然胜负证据。
- 首次独立Player11/12，发现提前结算后首次画面观察较晚时尾段起点延后。改为冻结的战斗Clock作为真实结算时刻，并把“无首帧观察即推进0.7秒”加入原终局测试；`snowstorm-play-final.xml`两项2/2，相关19项最新结果均通过，未放宽断言或改战斗规则。
- 最终运行源码1f9be45 Windows构建2444422571字节、0错误（snowstorm-build-final.log）。可见D3D11独立Player12/12，天气四批网格开关产生81604个明显差异像素；真实五人编队/霜星蓄技施法、天气提示、暂停顶点与倒计时、恢复运动、2倍同钟、提前结算清除、重开、8秒四脉冲和自然淡出全部通过。提前结算是明确的原调试按钮夹具，未称自然胜负。
- 实际查看weather-start-paused、weather-running、weather-ended截图；后者保留仍未到期的目标状态，不借天气结束统一解冻。证据artifacts/snowstorm-player-final/result.json。原纹理/程序风声已随源码，未宣称原作天气技能或完整Shader复现。
- 仓库6377个跟踪文件/meta检查通过；52处仅新增默认未覆盖WebGL段的导入噪声经逐文件核对恢复。场景、Prefab、Packages、ProjectSettings未变，两个原本地未跟踪项保留。第二机及多首领同时开技能的长期组合/声音主观平衡未验收。

## 2026-11-06：原作特效全范围接入验证

- 已确认完整看稿及剩余同范围直接接入。194段独立原素材（怪物/共用80、守军114）、199页、12369帧进入StreamingAssets并随源码交付。保留原五名导演、角色身体/棋子、塔露拉橙火浪与全部既有规则；只额外按本轮要求修维维美朝向与群伤。
- 首批D27实际Windows D3D11 Player8项通过、5467个效果开关差像素，已检查中心棋盘截图；构建/运行证据在artifacts/native-d27-centered。该证据不是所有194段逐项人工验收。
- 集成EditMode首次462/463；唯一旧断言把总预热固定为5/24，新增公共层后失效。改为验证二阶段相对增加19段且不解码纹理，TalulahPhase定向10/10通过。按用例取最新结果，463项逻辑全部通过。
- 集成GPU PlayMode首次125/131。六项失败分别为旧霜星层数/余mesh节点断言、守军技能夹具在普通出手提前结束，以及两处结算夹具未等待新原死亡尾段。修复后native-play-final.xml定向21/21，包含恢复的原大地图六人S3特效及原StrongholdChallengeSolution真实通关/重开；不改敌方或伤害凑测试。
- 实机发现足底billboard下半被地板裁，新增显式XZ地面投影与俯角补偿，保留挂点/PPU/原图及正常深度遮挡。native-ground-play.xml首次17/18；新角点测试误把Sprite.bounds的非零Z厚度当实际图面，改为真实XY平面后native-ground-final.xml 1/1，严格正交误差<.01px及实际轻透视下上下半GPU/opaque遮挡通过。不是放宽阈值或改Shader绕过地板。
- 当前133项场景用例按上述全量/定向记录取最新结果全部通过；余旧“EmbersEnd”测试改名为“OriginalLoopsClear”描述真实原循环清除，合并统计时视为同一用例。并非宣称单次全量133/133或逐帧人工验收全部素材。
- 最终Windows构建源9a113a4，2444013103字节、0错误（native-ground-build.log）。D3D11可见Player：D27 8/8、同刻开关效果差10282像素；D22 8/8、差4360像素，均核真实事件、原Shader/单位Renderer尺度、暂停不扣血、相机绘制不改伤害及重开清理；已检查两幅实际截图。证据native-ground-d27/result.json与native-ground-d22/result.json。D22早先探针错误要求近战余发出CombatShot，远程怪停在范围外导致夹具未收敛；最终改核真实火墙施放事件，不改正常战斗。
- 开发期间main以b3d947a压缩合并#80，其文件树与已继承的1f29370完全相同。保留旧工作分支，从实时main新建codex/vivi-original-vfx重落本轮增量为2ca5b7c，跟已实测9a113a4文件树完全相同；后续仅文档，无需重建相同运行文件。未直接改main、未重写旧分支或整文件丢弃双方修改。
- 仓库6350个跟踪文件/meta检查通过；本轮场景、Prefab、Packages、ProjectSettings无变更。导入器52处只增未覆盖WebGL段，经逐文件比较后恢复；原.vscode/及PackageManagerSettings.asset仍保留未跟踪。第二机、全角色人工长局和完整原客户端附加控制脚本未验收。
- 安赛尔/凛御银灰两条拖尾缺原驱动；望额外独爆和原邻接线未定位；荒芜拉普兰德一层额外自身UV旋流未重放；余按规则结束清除原循环，尚未重放原StopEmitting尾。原空控制节点不是可见资源。来源与适配见art/source-records/original-defender-vfx-2026-11-06.json及art/MONSTER_ORIGINAL_VFX-2026-11-06.md。

## 2026-11-06：维维美朝向/群伤与原特效准备

- 实时main a37f0e5，功能分支codex/vivi-facing-splash承接未合并#80，保留两个已确认本地隔离项。
- 全量EditMode451/451（artifacts/vivi-edit.xml）。ViviCombatTests覆盖群伤边界/独立护甲、原目标先死、6弹/SP/暂停/取消、默认非Vivi行为、朝向/免控/动作锁；ViviMovementSceneTests核对两种实际SpriteRenderer模式与GIF退行时帧仍正播。
- 图形首次19/20，失败为旧BattleInteraction结算夹具未等待上一轮新跳字1.05秒尾段；改测试等待并验证战斗Clock不走，重跑9/9。按fullname取最新结果，相关20个用例全通过；不是新增第二轮20个测试，也未称全量PlayMode通过。文件vivi-play.xml与vivi-play-final.xml。
- 独立Player专用-viviCombatSmokeTest只在Development显式触发，运行快照夹具不改资产：右行/左转/恐惧/暂停，以及三原克洛丝5000HP/3甲、落点0/1.8/2.4的真实普攻→技力→手动技能群伤。b385cb9运行源码构建0错误，D3D11实际Player14项检查全部true（artifacts/vivi-player/result.json），两近目标各102实际伤害、2.4外目标0，消费1发弹药；已查看左右/恐惧/群伤截图。
- 此条为开工时取源/采样记录，后续已通过完整看稿并进入上述集成验证。原代表探针不等于整套生命周期或游戏接入完成。全部范围见art/VFX-ORIGINAL-REPLACEMENT.md。

## 2026-11-06：战斗可观察性与专项场景集成

- 基线main a37f0e5，吸收V5和已确认角色整合成果。只保留原.vscode/与PackageManagerSettings.asset，不提交覆盖；不自动合并。
- Unity6000.6.5f1：全量EditMode433/433（features-edit-final.xml）；新增GPU PlayMode6/6（features-play-new.xml），受影响回归46/46（features-play-regression.xml），合计52/52。未称全量PlayMode通过。
- 九关逐个实际载入，最高档初始成员、42卡、正常九张库存、无限金币、默认独立守军和重开/改Inspector不重复倍率均通过。感染/牧群以正常守军攻击自然触发。其余羁绊的完整自然操作/培养平衡尚未逐项验收。
- 三星塔露拉专项使用真实克洛丝射击，无注入致死：经过FirstFall/FirstRain/Second/FinalFall/FinalRain/Dead；最终6375上限、守军存活与重开4250通过。10项领域边界覆盖单阶段、暂停/SP、保护、只终死一次、Lure/Fear朝向与取消剑光。
- 跳字8项逻辑+2项场景覆盖实际过量伤害/治疗、盾吸收分离、复起不虚报、冻结刷新/免控、256日志/96屏显上限、暂停/重开、点击与拖动不被消费。
- Editor实际保存10个新场景（总15），并纳入统一构建目录。修复生成工具在batch空SceneSetup恢复异常和跨场景加载导致模板资产失效；保存的原5场景无差异，不在Play重建地形。
- 4379d35运行源码Windows构建0错误，包含15保存场景。Direct3D11可见独立Player的53项检查全部为true（features-player/result.json），包括自然塔露拉全阶段、自动雨暂停、B手动火雨、九场景真实载入与拖动、培养未赠送。实际看过伤害跳字/冻结标记/B施法/最终退场及结算截图。自然阶段约12.15秒首倒、45.15秒B、77.25秒终倒、110.25秒真死亡。冻结的Player图采用明确TryApplyCold API夹具，不冒称自然触发；伤害和塔露拉死亡来自真实克洛丝射击。
- 仅测试meta GUID和文档在构建后修正，Runtime/Editor运行代码与4379d35相同；仓库校验5812文件通过。第一次生成工具的batch空setup/资产卸载故障和测试无技能夹具问题已修复，最终工具与相关套件通过。
- 第二机、不同分辨率的完整组合、全菜单人工点选、所有羁绊自然长局平衡和完整原客户端规则未验收。


## 2026-11-06：V5界面回归

- 基线main a37f0e5，吸收057e090依赖；保留原两个本地未跟踪项。
- 全量EditMode421/421；最终GPU PlayMode23/23（ApprovedUi、ShopMap、Campaign、基础/后期两关闭环与Stronghold交互）。XML：artifacts/ui-v5-edit.xml、ui-v5-final-play.xml。
- 首次全量GPU运行无进展中止，不记通过。定向回归定位红门投影进入新卡栏，已修复取景预算。暂停拖动、侧清单原子购买、左右出售/取消、字体、两关真实通关及买卡带入第二关通过。
- 尚待独立Player构建/截图复核；第二机、全角色自然通关与长期平衡未测。


独立Windows构建31085d3通过（0错误），D3D11 Player的11项指针夹具检查全部通过。隐藏窗口的屏幕截图全黑，不作为图像证据；可见Player重跑已查看合并、真实出售报价与暂停部署画面。截图保存在artifacts/ui-v5-visible；首张仍在启动闪屏，未采用。根据画面追加角色头顶取景余量，后续最终集成回归验证。

## 2026-11-05：42卡本地整合与加强羁绊测试

- 基线实时main a37f0e5；用户确认新分支codex/synergy-test-integration，在唯一E盘工程吸收#75 d0ea863及#74 155e6b5（含#69/#71），未自动合并GitHub main。先用merge-tree报告ManualBattleController与GDD/STATUS/QA四处冲突，确认后逐段兼容双方死亡尾段及文档，没有整文件选一方。原.vscode/及PackageManagerSettings.asset保留未跟踪。
- 静态审计：42种实际CardId与CampaignBalance逐一对应，原41名除银狼、加M42/M43；费用9/10/11/5/7。41个人物资源根、53份身体清单、589页及.meta在目标Git树跟踪且无缺页；维维美用现有Sprite。完整名单/真实技能缺口见MONSTER_COMPLETION_AUDIT，不将模型或导入资源等同全部原作机制完成。
- 仅自由验证快照加强：怪物HP×4、基础攻击/治疗/SkillAttack预算×2、固有爆炸×2；守军HP×12/攻击×8；巨像及IV原基准保持。新SynergySandboxTuningTests验证混编爱国者、技能和医疗、暂停扣DP、重开不叠乘、源资产及正常模式不变、IV基准6000HP。固定羁绊伤害未统一放大。
- synergy-integration-edit.xml完整EditMode421/421，通过、零跳过。首轮synergy-integration-play.xml完整场景110/116，6失败：三名新五费诊断的二星HP仍是旧值、雪怪三星测试旧HP405、原角色数量断言40，以及加强守军后的单兵深池不能完成自然击杀。对应修正诊断期望、42卡和405×4；深池用四名真实成员协作击杀，不强制造伤或改触发规则。最终synergy-integration-play-final.xml完整GPU D3D11 PlayMode116/116，通过、零跳过，83.19秒；含三名原生特效像素、原塔露拉/火雨/死亡、商店/技能/九条羁绊及新倍率边界。
- 仓库最终5689个已跟踪文件及meta检查通过；无冲突标记/缺失GUID，场景、Prefab、Packages、ProjectSettings无变更。导入器自动补的52个非覆盖WebGL默认段经逐文件验证只有该段变化后清除，保留已提交源meta与Standalone设置，未改他人文件。
- 最终运行源码d4e6552ae7679278668bd5174223ada7fef4e412；synergy-integration-build.log：BASELINE_BUILD_PASS 2380009757 bytes / 0 errors。Builds/Windows/SOURCE.txt绑定源码/分支/依赖，邻接羁绊测试.cmd正常启动-synergySandbox而非诊断参数，目录保留完整EXE/Data/DLL及Word手册，无ZIP/Releases。
- 从系统TEMP工作目录运行最终独立D3D11 EXE：synergy-player-sandbox.log的snapshot passed；advanced日志pool/split/army/golem/paused/naturalGrowth全部True；remaining日志pool/herd/nativeDeep/contract/marineGrowth全True；rework日志pool/preset/pausedDrag/ultimateSoloClear/investment/snowPulsesAndReset全True；patriot日志16项事件全True且七组GPU像素差非零。五个独立进程真实退出码均0。培养/深池包含自然交战；契约/后期复起/强制结算等沿用明确诊断夹具，不能称为所有阶段自然通关。
- 正常可见窗口额外使用-synergySandbox（无SmokeTest），实际看到∞、8级/雪怪5整合6、42卡商店；鼠标点开始下一战后，将士兵拖上方红门，DP100→98；空格暂停，再拖弩手到同红门，DP98→96，暂停保持且人物显示；点击结束验证返回∞整备。使用Computer Use实际窗口截图逐次观察，没有以后台黑图或进程存活代替画面。窗口留在整备页供用户继续试玩，日志synergy-normal-launch.log无异常。未进行完整13关人工自然通关、42角色逐个自然触发全部机制、第二机/新克隆或长时平衡/性能测量。
- docs/SYNERGY_PLAYTEST_GUIDE.md及Word手册完成，13页实际渲染并逐页检查。Windows无LibreOffice，使用独立隐藏Word16 COM只读导出PDF，再由原render_docx.py/Poppler栅格化；中间文件与报告在artifacts/synergy-guide，未进入源码交付。原稿与Word源校验一致，表头重复、表格行不拆分；不把文档排版检查算游戏测试。

## 2026-11-05：塔露拉原素材普攻、动作资源与死亡显示

人类通过单独普攻看稿并要求将死亡等此前动作一起接入。本轮沿用PR #75与`codex/talulah-map-fire-facing`，保留早期橙色火雨及原战斗数值。当前触发和未绑定资源明确分开，见[接入清单](art/TALULAH-NATIVE-VISUALS.md)。

- 原生独立采样25组全部实际PASS，来源Shader支持、捕获边缘和HDR超限均为0；已有晶体等14组复用原报告。全量原生39段/4218帧/83页，PNG22,323,799字节；统一裁边、单次镜像、4像素补齐、pivot/PPU、零Alpha加色RGB与页面容量只读复验通过。
- 身体补充12段/1167帧/25页及2隐藏姿态只读复验通过。旧四动作和朝向不变，Die80帧/2.666667秒进入运行预加载；其余身体与34组特效不预加载、不伪造机制触发。
- `artifacts/talulah-original-visual-complete.xml`：最终相关GPU PlayMode 11/11通过，覆盖真实普攻施法者/目标的GPU像素、左右与两个实例独立帧、暂停、商店编队、火雨回归、完整死亡、终局显示时钟和重开清理。首轮共享失败已缩小诊断为新纹理误作Cubemap导入，专用导入器现明确2D；随后修正Clear的立即隐藏，最终完整复跑通过。
- `artifacts/talulah-original-edit.xml`：完整EditMode409/409通过。BattleShop结算绘制采用同一死亡显示门限，经济与战斗结束仍按原模型立即结算。
- [原生普攻真实相机截图](art/talulah-native-attack-actual.png)：已目视检查，来自运行中的MonsterTrial，原生剑焰与真实目标命中均可见。不是V13固定背景合成。

运行源码`09c2c64d75060ed69e299b0696093de0c1b5945d`的Windows构建通过，`artifacts/talulah-original-build.log`为0错误。工程外工作目录启动独立Direct3D12 Player（显式batch自动化夹具），`talulah-original-player/talulah-native-result.json`为passed=true，真实进程退出0；施法者/目标GPU平均RGB差17.3238/255、暂停和完整死亡检查通过。随后原`TALULAH_CHECK`的拖动、暂停部署、技力、慢速选技、橙色火雨、全场伤害、施法锁定、胜利、重开与返回13项均为True。

[独立Player死亡截图](art/talulah-native-death-actual.png)已目视核对；截图为死亡第1秒，不把单张图称为全段录像。死亡测试使用明确夹具调用正常Defeat路径并置终局，以验证最后一个实体死亡后仍播完整片段；不冒称自然关卡中触发。Player内额外确认78页未绑定特效PNG与死亡PreparedArt资源随包存在，不依赖作者外部工作区。

本机`Builds/Windows/YesterdayConvoy.exe`已更新，`SOURCE.txt`绑定上述运行源码，后续文档提交不改变可执行内容；不创建ZIP/Releases。完整机制还原、二阶段、点燃/引爆/晶体触发、音效、完整人工通关、第二台电脑及长时间性能未验证。

## 2026-11-05：早期橙色火浪恢复与实时地图范围

从实时`origin/main`的a37f0e5建立`codex/talulah-map-fire-facing`。人类看图确认恢复2026-10-21橙色火焰版；没有采用V13原作尖头火球。修改只限表现、相关验证和说明，原两个未跟踪项保留，未修改场景、Prefab、Packages、ProjectSettings、技能数值或寻路。

- `artifacts/fire-orange-edit.xml`：FireRainTests 3/3通过，覆盖原全场六波伤害、幂等、暂停、施法锁定与SP恢复。
- `artifacts/fire-orange-play.xml`：9项相关GPU场景首轮8通过，唯一失败是新增地图扩张测试夹具复制了静态合批地块，其Renderer仍用原烘焙bounds。缩小到单例确认后，改用真实可移动的新测试地块；`fire-orange-adaptive-real-geometry.xml`最终1/1通过。9项均获覆盖，包括大地图商店买入/部署、原普攻、动作、暂停像素冻结、自然结束与重开清理。
- `fire-orange-gpu/fire-orange-result.json`：真实相机平均RGB差20.6057/255；峰值37条橙色火浪；弱守军实际产生15次目标伤害后阵亡，完整18事件由逻辑用例验证。施法仍六秒，余焰未追加伤害。
- [大地图实机相机截图](art/talulah-fire-orange-actual.png)：来自BattleShop关间商店买入塔露拉、第二关正常拖动部署和手动施放后的相机，已目视检查。不是历史看稿背景，也不是完整HUD录像。
- 项目外V14方向片15秒、450帧、完整解码通过；原Move_A支撑足向左回蹬，默认前进朝右。V13将未镜像原图配负向位移导致倒走；游戏朝向本来正确，未反转`DemoSpriteVisual`。方向片未作为游戏资源导入。

关键连接：`FireRainView`读取每个实体的施法时刻与`Battle.Clock`；仅施法时从当前场景启用的地表顶面四角取得投影范围，动态分配行数/火浪实例并更新暖光和碎焰。宽图只调整表现速度，穿过守军不检测碰撞；伤害仍由原CombatShot负责。`EngagementView`恢复原CC0命中帧，`FireRainScreenEffect`恢复早期暖暗和震动。32帧火流、25帧碎焰和Shader已在Git中，未新下载或遗漏运行素材。

运行源码`755ac92cd10d28af420314364454a927d4bf9b92`通过Windows构建，`artifacts/fire-orange-build.log`记录`BASELINE_BUILD_PASS`、0错误。随后从工程外工作目录启动独立EXE、Direct3D12/RTX4060，`fire-orange-player/fire-orange-result.json`为passed=true，真实进程退出0；平均RGB差20.6019/255、峰值37条火浪、六秒/15次实际目标伤害、动画/暂停/自然结束/重开/切换检查通过，已目视核对真实Player截图。开发验证沿用旧`-talulahFireRainV12Test`参数以兼容既有入口，当前验证的是恢复后的橙色版。

最新本机入口`Builds/Windows/YesterdayConvoy.exe`，同目录`SOURCE.txt`绑定上述运行源码；后续文档提交不改变可执行内容。未创建ZIP/Releases。完整人工关卡、第二台电脑和长期性能未测。

## 2026-11-05：爱国者五费商店、混编与原版表现

任务[#73](https://github.com/Peanora/YesterdayConvoy/issues/73)，分支codex/patriot-five-cost-shop，main基线a37f0e5；本地保留PR #71（含#69）而非丢弃已完成角色。M20实际卡牌、动画和原生19组/99页资源均已在main跟踪，本轮只复用，未改忽略规则。源文件/配套.meta继续提交，原.vscode隔离保留。来源与权限状态不变，见[M20说明](monsters/M20_PATRIOT_VFX.md)。

- 完整EditMode **421/421通过、零跳过**，artifacts/patriot-shop-edit.xml。新增五项覆盖白名单、星级与标签、次卡实例防御/四次接触、三星60秒复起及高台投矛、双实例指挥不叠加/重开、五费刷新/有限库存/满席合成与普通经济只扣5。
- 最终相关GPU **36/36通过、零跳过**，artifacts/patriot-shop-gpu-final.xml。含M20商店专属、原M20全部阶段/朝向、M25、M32、其他角色能力、羁绊、Campaign、高难度商店、霜星。首轮35/36，唯一失败是新诊断把无限金币当普通扣费；已修正为保留无限金币并校验购买价值5，未改真实经济。
- 专属验证从SynergyTrial真实五费九张、满备战合成二星、原M20模型、暂停拖动到实际红门；随后使用明确的近静止开发夹具，以M08为主卡、二星M20为次卡验证四连击、60秒倒地、1140最大生命复起、保护、毁灭、自动高台投矛、真实伤害/灼烧、最终死亡和重开/返回清理。七阶段均有同相机开关原生表现的实际GPU像素差，不能把夹具称为普通关自然完整通关。
- 运行源码**ba014335eed5f7de6b3cb7efc7aa75babb47ae51** Windows构建报告2,309,684,448 bytes、0错误，实际Windows目录1,282,187,423 bytes。独立窗口1920×1080/Direct3D12运行，16项PATRIOT_SHOP_EVENT全True、PATRIOT_SHOP_CHECK passed=True、退出0。artifacts/patriot-shop-build.log / patriot-shop-player.log / patriot-shop-player/十张实际截图与结果文本。已目视核对五费卡面、二星模型备战、真实部署和原生特效。七阶段开/关RGB平均像素差约0.606/3.794/36.349/4.548/5.573/10.010/0.375，均有可见GPU贡献。
- .gitignore继续允许已跟踪的角色/特效资源，没有全局取消规则；仓库5431文件/meta检查通过。未做第二机、新克隆、全13关平衡与长期性能；原音效和五费构筑专属仍未新增。无场景/Prefab/Packages/ProjectSettings变化，不自动ZIP/Releases或合并main。

## 2026-11-05：梅菲斯特原生治疗、五费入池和实际Player

任务[#72](https://github.com/Peanora/YesterdayConvoy/issues/72)，用户确认V1并要求接入游戏和商店。继续codex/mephisto-native-vfx-preview / PR #71，保留已完成曼德拉#69依赖。新运行素材为4PNG/285帧及清单、原比例挂点和.meta，复用已跟踪的兼容材质/Shader；页SHA256、四组布局、无采样边界、全部忽略来源/实际跟踪检查通过。原包和加工区不入Git，真实权利/适配见[M25说明](monsters/M25-NATIVE-VFX.md)。

- 完整EditMode首415/416，唯一失败为旧卡池总数40断言；按新增M25更新后最终416/416通过、零跳过。artifacts/mephisto-edit-final.xml。
- 专属GPU两项最终通过：真实五费九张库存、满备战合成二星、原模型、暂停拖动、三目标治疗与不提前发光、实际像素、无伤害普攻、暂停冻结、重复/并发、朝向、死亡取消、增幅解除尾段、重开/返回。诊断夹具初版缺守军图号已补齐；正常入口画面采样从粒子起始空帧移到命中后0.3秒。最终广域相关GPU33/33通过、零跳过，含M25、旧角色能力、曼德拉、羁绊/牧群、Campaign、后期商店、霜星；不是全工程GPU遍历。artifacts/mephisto-regression.xml。
- 源码93eaa20dc7fa201e00c65508096aaa11d4c46207 Windows构建报告2,309,677,578 bytes/0错误，目录1,282,180,435 bytes。独立窗口1920×1080、Direct3D12运行，21项MEPHI_EVENT全True，MEPHISTO_VFX_CHECK passed=True，进程退出0。artifacts/mephisto-build.log / mephisto-player.log / mephisto-player/七张完整截图。
- 开/关特效同相机同时间RGB差：命中3595像素、手部35、持续回复473、正常单怪自然受伤治疗363；编辑器与独立Player均可见，已目视核对完整HUD/模型/亮部。多目标夹具为明确诊断用的近静止四路线与主动造伤，保留实际地形遮挡；普通M25入口不注入受伤，真实守军攻击触发治疗。
- 无场景/Prefab/Packages/ProjectSettings修改，.vscode保持。当前Builds/Windows是本轮版本；未做第二机、全新克隆构建、13关自然平衡、长时性能、原作逐帧完整调度或音效。不自动ZIP/Releases，不自动合并main。

## 2026-11-05：蔓德拉原作特效、升星与商店

任务[#67](https://github.com/Peanora/YesterdayConvoy/issues/67)，从main 2c04ba7继续codex/mandragora-vfx-preview。用户确认原作V1动态看稿后要求接入游戏与商店。无场景/Prefab/Packages/ProjectSettings修改，原.vscode/保持。

提交6e2be32创建PR #69后，主线已更新为a37f0e5（塔露拉火雨V12及爱国者原生特效）。已先报告CampaignBootstrap、QA、STATUS三处冲突，逐项保留全部测试路由和双方记录，未整文件选择一方。`mandragora-main-edit.xml`完整412/412通过；`mandragora-main-gpu.xml`相关27/27通过，包含蔓德拉、爱国者、塔露拉火雨、商店及羁绊。仓库5393文件/meta检查通过。下列早期验证记录仍对应同步前版本，最终集成版本以本段后续构建/Player记录为准。

最终整合版 `mandragora-main-build.log`：Windows 2,307,445,781 bytes / 0错误，后处理后完整目录1,279,948,491字节。从工程外工作目录运行 `mandragora-main-player.log`可见窗口、`mandragora-main-patriot.log`与`mandragora-main-fire.log`三个独立D3D11 Player均退出0，分别得到MANDRAGORA_VFX_CHECK passed=True、PATRIOT_VFX_CHECK_PASS、FIRE_RAIN_V12_PASS。三项保留原伤害、暂停及清理边界；最终蔓德拉实际窗口截图随源码保存于monsters/previews/M32-native。此轮仍未做第二机、新克隆和完整自然战斗通关。

- `mandragora-edit.xml`：56/56定向逻辑通过，覆盖原M32技能边界、升星副卡复起生命和伤害、混编对空/柱击破盾、五费刷新与满席三合一，以及相关Campaign/羁绊逻辑。
- `mandragora-gpu.xml`：21个相关GPU用例首轮20通过，新增蔓德拉用例的普攻取帧早于真实0.833秒出手加0.22秒弹道和首模拟步，调整为1.2秒。`mandragora-final-gpu.xml`蔓德拉2/2最终通过；19个其他商店、霜星、泥岩和羁绊场景回归已通过，共21个不同用例。没有把重复跑过次数相加。
- 初次本轮后台测试卡在原有WaitForEndOfFrame截图等待，停止本轮进程后改为跨帧截图与显式相机输出。补盾夹具最初忽略35法抗导致伤害未跨40%阈值，修正开发测试输入，不放宽技能阈值。实际普攻一次出手/一次命中、凝视暂停冻结、破盾中断、封地断束、左右朝向、两个实例各自光束、重开清理均通过。
- 原生Vulkan捕获后，三方向倒柱边界检查发现裁切，扩大取景并重烘焙；最终42段2595帧48页、无捕获边界裁切。一个原脚本容器输出全透明，未放入清单冒充图像。`check_atlas.py`检查42/48布局通过，PNG总15,220,657字节，所有必要meta成对。忽略检查没有排除新增运行素材，未取消.gitignore。
- Windows `mandragora-build.log`：BASELINE_BUILD_PASS 2,081,468,345 bytes、0错误；PreparedArt预处理仍可重建并随Player自动安装，运行图集采用Windows BC7。此为Unity构建报告大小，后处理后的完整目录约1.05GB。
- `mandragora-player.log`后台独立D3D11 Player退出0，事件全部True；该次ScreenCapture全黑，不作为画面证据。`mandragora-player-visible.log`可见独立窗口复验退出0，实际商店/备战模型/战斗截图已人工查看。凝视仅关闭光束得到739/810个变化像素，倒柱仅关闭本次左向倒塌得到5422个变化像素；破盾13069、复起19449、死亡2844。两实例光束数2。不是仅凭Renderer计数验收。
- 移除两处旧的“等待蔓德拉入池”界面提示后，`mandragora-final-build.log`重建同为2,081,468,345 bytes / 0错误；`mandragora-final-player.log`最终可见独立Player全部True、退出0。`mandragora-campaign-player.log`既有正式挑战自动结算闭环通过（stage13 / gold329 / 退出0），不记为完整自然战斗通关。

正常商店购买、满席两星合成与暂停红门拖动走真实入口；复起/倒柱/破盾等后期用明确开发夹具，在实际地图相机中读取真实模拟事件。夹具使用固定位置/低移速/高生命测试守军，经伤害接口触发阶段，不能算作正常13关自然通关。正常地图未新增支柱，倒柱要求地图配置已有支柱。新原作媒体来源、权利状态与适配见[说明](monsters/M32-NATIVE-VFX.md)和[源记录](art/source-records/mandragora-native.json)。无音效；第二机/全新克隆、长期性能和强度平衡未测。不自动合并/ZIP/Releases。

## 2026-11-05：塔露拉火雨V12运行接入

人类批准项目外V12。实时main为2c04ba7，本地已整合爱国者的4cb5aa8作为依赖，功能分支codex/talulah-fire-rain-v12；本轮运行源码a0769a8d845b1c5998f7de50a5d37e185fa4e517。未修改场景文件、Prefab、Packages、ProjectSettings或技能规则，保留原两个未跟踪项。

- `artifacts/fire-v12-edit.xml`：完整409/409 EditMode通过，含既有高生命值下6波/18目标伤害、暂停、施法锁定和重开逻辑。
- `artifacts/fire-v12-final-play.xml`：最终18/18相关GPU用例通过，覆盖新原素材动态图集/像素/地面、五行齐头、暂停屏幕与帧冻结、自然尾段/重开/切换，原攻击、怪物选择、基础/后期两关与Campaign回归。BattleShop实际保存地图通过测试结算进入商店购买塔露拉，再拖动进第二关真实施法，完整地面覆盖截图已目视核对；测试结算不是自然通关证据。
- 早期开发轮因在MonoBehaviour字段初始化MaterialPropertyBlock导致异常，已移至Awake；随后新夹具误假定弱守军始终承受18次命中，改为核对6波弹药耗尽、真实阵亡和余焰不新增伤害，强生命值18次由原逻辑用例验证。中间曾有测试直接调用internal接口编译失败，已复用原测试结算入口。最终通过记录为上列文件，不把这些开发失败计为通过。
- `artifacts/fire-v12-build.log`：Unity6000.6.5f1/Windows x64 Development，BASELINE_BUILD_PASS 2241177910 bytes、0错误；缓存后处理后的完整Windows目录约1213.7MB。构建对应运行源码a0769a8，后续文档提交不改变可执行内容。
- `artifacts/fire-v12-player.log`、`fire-v12-player/fire-v12-result.json`与退出文件：从工程外工作目录运行实际EXE，Direct3D12/RTX4060，passed=true，退出0。真实相机开/关火焰平均RGB绝对差12.1285/255，118处地面火焰，正常弱守军实际产生15个目标命中后阵亡；原6秒施法结束后余焰没有追加事件。相邻采样的帧和位置变化、暂停像素不变、逻辑相机不被震动移动、自然结束/重开/切换清理均通过。截图已目视核对，Editor大地图截图不冒称来自Player。
- 仓库检查5275文件通过；9张运行PNG、JSON、Shader和.meta无忽略遗漏，保留加色RGB，BC7由专属导入器配置。源说明与打包脚本入库，原包、外部加工工作区、构建、日志、本地AGENTS不入Git。

未做全量PlayMode、完整人工关卡平衡试玩、第二台电脑或全新克隆验收。效果为固定镜头离线帧与本作地形组合；6秒玩法使用三面2秒间隔的稀疏火流，不能称原作十波调度逐像素还原。原媒体真实权利状态见[来源与接入说明](art/TALULAH-FIRE-RAIN-V12.md)。沿用功能分支推送/PR/人工合并，不自动合并main、ZIP或Releases。

## 2026-11-05：爱国者V2接入

任务[#62](https://github.com/Peanora/YesterdayConvoy/issues/62)，基线main `8f4d604`，分支 `codex/patriot-native-vfx`。功能源码 `ef02d37`，最终Windows纹理优化源码 `b0d4b7fbe1ec7be625ac53aa59d47be9f9b86fee`；后续状态/QA文档不改变可执行内容。

- `artifacts/patriot-full-edit.xml`：完整357/357通过；新增真实四次伤害接触边界，投矛1.7秒出手/1.92秒命中，原60秒复起、15秒保护、目标、伤害、取消等13项专属逻辑通过。
- `artifacts/patriot-full-play.xml`：首轮87/88，原CampaignSceneTests的SandboxHasAuthoredMapUnlimitedShopAndRealThreeStarStats在开始下一战断言失败；独立`patriot-campaign-recheck.xml`4/4通过，完整复跑`patriot-full-play-recheck.xml`88/88通过。未修改商店/战役逻辑来掩盖失败；该首次失败原因尚未单独确定，不称作已修复根因。
- `artifacts/patriot-hq-play.xml`：最终高清图集2/2专属GPU通过；Windows压缩后`patriot-bc7-play.xml`再2/2通过。实际主入口自然部署并命中；明确的阶段开发夹具覆盖四连击、首次倒下、复起末段、毁灭普攻、远端高台投矛、灼烧、最终死亡。检查真实GPU显示/隐藏像素差、材质支持、暂停位置与帧冻结、重开/换战斗清理。
- 第二个GPU用例使用真实战斗模型分别向左右投矛，检查原角色动作与上抛效果一致镜像；蓄力中致命伤害取消真实待发投矛及整套蓄力/落矛表现，不留假命中特效。
- 原生Vulkan烘焙采用固定粒子种子与黑白背景浮点合成；最终HDR峰值30.25122，小于编码范围32，亮度截断像素0，各通道透射误差峰值约1.43e-6。19组完整图集的最外边缘亮度均为0，修正早期复起方形裁边；512像素大效果/256像素其余效果，30fps。
- 来源记录逐项核验101个运行文件（99 PNG+catalog+Shader）的暂存哈希。检查仓库.gitignore、共享info/exclude、可用全局忽略来源，所有新增任务文件无被忽略项；必要.meta与资源配对、GUID保留。不修改共享资源CSV、场景、Prefab、Packages/ProjectSettings，原未跟踪.vscode保留不提交。
- Windows专用BC7保留零alpha像素的加色RGB；与未压缩独立Player截图对比，8张全图RGB平均绝对误差最大约0.305/255（不是逐像素无损）。`PatriotArtTools.Configure`只处理本角色目录；源PNG不变，全部99个GUID保持。
- `artifacts/patriot-final-windows-build.log`：Windows x64 Development构建0错误，Unity后处理前1976537795 bytes；处理1237个缓存文件/替换Player内1176张角色PNG后，整个Windows目录979961019 bytes（约980MB）。源PNG保留；缓存可重建，不需队友另传。未优化时约1574MB，最终纹理设置减少约595MB。
- `artifacts/patriot-final-player.log`、`patriot-final-player/patriot-runtime-check.json`：从工程外工作目录启动实际打包程序，Direct3D12、普通爱国者入口/阶段夹具/真实伤害/暂停/重开均passed；`patriot-final-player-exit.txt`为0。8张GPU截图在同证据目录。测试使用`-patriotVfxSmokeTest`，普通启动不运行夹具。

本机试玩入口：`Builds/Windows/YesterdayConvoy.exe`，必须保留整个Windows目录。BUILD-INFO记录最终源码b0d4b7f。对外拉取的源工程包含全部新增Resources依赖与.meta，本地构建和独立运行已验证；未实际做第二台机器或全新克隆构建，不扩大这一结论。原作效果是离线生成的固定视角二维表现，不是原作三维粒子/全部战斗脚本的移植；本次无音效，整段原作蓄力前置调度未逐帧核对。真实来源、权利状态与逐文件哈希见[独立记录](art/source-records/patriot-v2-native.json)。不自动合并、ZIP或Releases。

## 2026-11-04：剩余四条羁绊与最新main集成

实时main8f4d604（塔露拉普攻V8 #60），分支codex/synergy-four-traits带入未合并#59。入口CampaignBootstrap及STATUS/QA有冲突，已先报告，逐段保留V8入口和羁绊验证/完整记录，不整文件选单方。保留原.vscode/及PackageManagerSettings.asset，不提交覆盖；不改场景地形、Prefab、包、工程设置。

- four-compile.log：实际Unity导入/编译及41角色配置校验通过。首轮测试编译失败：测试调用internal FinishForTesting，改为反射调用，未扩大运行权限。
- four-edit2.xml：383/384，唯一失败为60与60.0000038的浮点精确断言，改用合理容差。four-final-edit.xml完整385/385通过，新增覆盖转化队列/原进度/暂停寿命/限额/原生爆炸最终死亡、火种DOT预算/非递归/未来4档原子引爆、承契真实越阈/自损/幂等/指定目标、海嗣保护次数/共享/减甲到期/猎食/跨关幂等。
- four-play.xml完整96/96 GPU通过，包含全部原怪物/各批守军/商店/旧五条和三项新场景用例：四预置逐个正常卡栏暂停拖动、原M21转化模型与承契爆发、原生M31喷火实际火种及M33反击渗透和培养。未用伪造灼烧代替这条实机触发证据。
- 最后收紧海嗣接触记录，真实受伤在统一入伤接口登记，输出须实际损失目标生命，不靠无效击打培养；four-boundaries.xml定向16/16通过。four-final-play.xml最终相关GPU 3/3通过，实际模型/拖动/原生火种/反击与跨关仍可用。首版运行源码a7ecc12，four-build.log构建0错误；four-player.log五项全部True，four-campaign-player.log结算流程通过/stage13/gold329，four-talulah-player.log全部13项True，三个退出文件均0。

窗口实际检查已看到八种样板、海嗣4/泥岩2/萨卡兹2、主副适应、完整效果和成员。发现详情关闭按钮点击穿透到后方商品，会误购买；修复为弹窗期间保留GUI控件序号但忽略底层商店/战场输入，并阻断原始拖动入口。four-modal-play.xml相关4/4通过，包含弹窗后方不能部署/不扣费、关闭后可正常拖动。最终原生关闭按钮的鼠标复验因窗口控制工具本轮后段不可用而未执行，不把此前发现问题的点击记作修复后通过。最终运行源码36808ca，four-final-build.log：BASELINE_BUILD_PASS 1800304349 bytes、0错误；预处理1255文件、1193源PNG保留，后处理目录786412537 bytes。four-final-player.log以独立EXE batchmode/D3D11运行，pool/herd/nativeDeep/contract/marineGrowth五项全True、退出0；该次不是人工鼠标验证。仓库4773跟踪文件/meta与暂存检查通过，未提交原两项本地文件。

完整13关自然通关、长期随机运营/强度平衡、第二机与新克隆未测。深池4档为配置逻辑验证，当前正常卡池只能2档；不将测试快照中的未来M32误标为已入池。转化体沿用原模型/动作和文字状态，火种复用已有地面标记，未新增完整原作美术特效。无ZIP/Releases。

## 2026-11-04：塔露拉普攻V8

任务[#58](https://github.com/Peanora/YesterdayConvoy/issues/58)，从main 220741f建立codex/talulah-attack-flipbook-v8。特效源码7f779ef，最终构建源码d510f65；后续交付文档不改变可执行内容。运行图集SHA256、完整生成提示词和权利记录见[独立来源记录](art/source-records/talulah-v8-fire.json)，新素材未被忽略，Resources依赖与必要.meta一起提交。

- `artifacts/talulah-v8-fit-play.xml`：最终地面适配2/2 GPU通过；`talulah-v8-related-play.xml`最终7/7，`talulah-v8-shop-play.xml`3/3，共10个不同相关场景用例通过，非全量PlayMode。
- 实际单怪攻击检查起焰前无特效、真实命中后绘制、帧持续前进、GPU像素差、暂停时钟/帧/位置冻结、尾部消失、重开/换入口清理；原手动火雨保持。左右攻击为真实战斗模型的专用夹具，检查整体镜像、并发材质帧独立与地形遮挡，不称为自然关卡双向录像。
- `artifacts/talulah-v8-actual.png`是最终Unity相机真实攻击截图；`talulah-v8-directions.png`是上述方向夹具截图。首轮下方焰尾被地面裁平，保留深度测试后通过画布尺寸和高度适配修正，末次截图已人工查看。
- `artifacts/talulah-v8-edit.xml`：完整356/356 EditMode通过；仓库静态检查通过。相关商店回归覆盖第一关结算、购买塔露拉、第二关拖动部署、技力和原火雨。
- 首次专用Player检查发现新Campaign默认入口会覆盖旧塔露拉诊断流程；补充CampaignBootstrap私有参数路由至MonsterTrial，无场景文件或正常游玩入口改变。不是火雨数值回归；修复后重新构建并通过专用Player检查，不以首轮失败冒充通过。
- `artifacts/talulah-v8-final-build.log`：Unity 6000.6.5f1 Windows x64 Development构建1778282995 bytes、0错误，后处理目录781705361 bytes；图集及Shader在构建资源报告中。`Builds/Windows/YesterdayConvoy.exe`对应源码d510f65，EXE SHA256 `b70442f90f50cd91f7af7bf2272602645fc5d02c4d3156fb221fc27c328d2436`；分发必须连同_Data、UnityPlayer.dll及其余依赖整个目录一起携带。
- 最终独立Player `talulah-v8-final-tower-player.log`塔露拉13项全True，`talulah-v8-final-player.log`高难度两关闭环9项全True，均退出0；专用入口路由修复通过这次最终Player验证。没有把Editor GPU截图写作独立Player截图。

本轮不声称全量PlayMode、长时平衡、性能优化、第二台电脑或全新克隆测试完成。工程外V8预览与游戏GPU证据分开；没有重画人物或提取参考录屏像素。

## 2026-11-04：羁绊左栏与正常部署回归

sidebar-edit.xml：13/13定向EditMode通过，检查重复同种卡去重、激活与未激活计数、未实现标签及效果文本。sidebar-final-play.xml：7/7 GPU通过，包含完整卡栏MouseDown/Drag/Up普通部署（97DP）、暂停第二张部署（93DP）且时钟不动、补给/整备拒绝部署且不消费，并保留感染/军令/巨像原五项回归。未重跑全部其余逻辑/场景、13关自然平衡或第二机。本轮未改场景/Prefab/包/工程设置；沿用PR59与原两个未跟踪项隔离。运行源码abd062e：sidebar-build.log构建Windows x64成功，0错误，后处理目录783583847 bytes；sidebar-player.log六项检查全部True、退出0。原生窗口实际点击未激活整合运动1/2，详情显示三档效果和本队成员，关闭后恢复操作。新详情不推进额外模拟、不改变规则。仓库4756文件/meta与暂存差异检查通过。

## 2026-11-04：三条扩展羁绊与36卡池

基线实时main `220741f`，新分支`codex/synergy-infection-guerrilla-mudrock`。人类确认公共接口与巨像预览，范围见[v1.2](proposals/SYNERGY-EXPANSION-v1.2.md)。保留场景地形/Prefab/包/工程设置及原两个未跟踪项。

- `synergy2-author.log`、`synergy2-compile.log`：GOLEM_SUPPORT_ASSET_PASS，显式创建支援配置、导入原作图集并实际编译通过。
- `synergy2-final-edit.xml`完整367/367通过。后增半令排除前半参与者边界，`synergy2-boundary-edit.xml`12/12通过，合计368个不同逻辑用例有通过记录，不称为一次完整368。覆盖卡池/五费排除、原库存/星级、分体扣费/穿门0/固有自爆分摊、死亡爆裂幂等、主副形态、军令不递归、半令、暂停/无目标不消费、巨像一次部署/成长和混编固有能力。
- `synergy2-play.xml`首轮7/8：新4项均通过。旧升星用例最后一张卡已被商店占库而委托失败；用例改为优先购买实际商品，保留九张库存规则。
- `synergy2-full-play.xml`89/90：巨像暂停生成正确使用Move首帧，用例误要求Idle，修为检查真实GOLEM库及帧0，不改暂停逻辑。
- `synergy2-final-full-play.xml`完整91/91 GPU通过：36卡/四样板、形态锁定/分体、真实军令、暂停拖动巨像/完整攻击，以及自然胜利获4培养经验、返回整备进入II级；原怪物/守军/两关商店和旧羁绊一并回归。

未做13关完整自然通关、长期经济/强度人工平衡、第二机或新克隆验证。自然胜利证据仅限本次验证地图与部署路径，不承诺全部随机构筑必胜。无ZIP/Releases。最终构建与独立Player结果如下。

最终运行源码 `da9928c`，`synergy2-build.log`：Windows x64 Development，BASELINE_BUILD_PASS 1797467201 bytes/0 errors（后处理前），离线资源后处理1255文件、替换1193张角色PNG，目录783575272 bytes；源PNG仍随仓库保留。`Builds/Windows/BUILD_INFO.json`登记完整源码提交。

- 最初隐藏窗口启动的`synergy2-player.log`停在初始化，没有完成证据，已终止该测试进程；改为正常可见窗口运行，不修改游戏逻辑。
- `synergy2-visible-player.log`：pool/split/army/golem/paused/naturalGrowth全部True；退出文件为0，实际自然交战获得4经验再进入II级。
- `synergy2-campaign-player.log`：passed=True、stage=13、gold=329，退出0。普通战斗为测试判胜，只证明跨关、结算、商店和补给衔接，不称13关自然通关。
- 最终原生窗口通过computer-use实际点击：领取首补给10→21、进入商店、切换测试、进入无限验证、切换泥岩样板、开始战斗、巨像拖到红门，DP100→96且正确显示巨像，返回整备无报错。窗口留在泥岩样板整备供人类试玩；未把工具截图另存为文件。
- 仓库4754跟踪文件及Unity元数据检查通过，全部GOLEM运行文件已跟踪、哈希与manifest一致、无100MB以上单文件。暂存差异检查通过；原.vscode/和PackageManagerSettings.asset仍未跟踪，AGENTS未提交。本轮无场景地形、Prefab、Packages或ProjectSettings差异，无ZIP/Releases。


## 2026-11-04：三阶段与首批羁绊接入

基于实时main b4b8b2c，新分支codex/synergy-first-loop吸收前一轮企划提交作为依赖；人类具体确认共享接口、场景和构建清单范围。独立新增Campaign/SynergyTrial，保留原两场景与旧试验；两项原本地未跟踪文件不提交覆盖。规格/关键连接见[v1.1](proposals/CAMPAIGN-SYNERGY-v1.1.md)。

- `campaign-author.log`：明确编辑器创作保存两个新场景与四项构建清单，不在运行时生成地形，复用已保存Mesh/材质和门户，原两个场景未覆盖。
- `campaign-edit.xml`首轮350/352，两个失败是旧用例仍要求仅两张场景；改为四张及新默认入口后`campaign-final-edit.xml`352/352。继续新增完整首发角色各星/动作窗口、独立备战和Boss配置检查，`campaign-verified-edit.xml`完整355/355通过。
- `campaign-play.xml`首轮7/8，暂停拖动用例在商店收起过渡期间立即投放；缩小重现并让用例等待实际收起，同时把羁绊控件从地图左侧中段移到战斗顶部，避免遮挡部署区域。`campaign-new-play.xml`3/3通过。
- `campaign-natural-play.xml`4/4，包含新关真实冻结/两种直接命中/碎冰、集结援军、最终自然穿门获胜，非强制判胜。
- `campaign-full-play.xml`完整86/86 GPU通过，覆盖原怪物、各批守军技能、原两关商店、霜星和新四项场景。
- 最后补充奖励关按Begin也直接保送、碎冰来源不跨冻结批次累计、部署反馈显示实际升星DP：`campaign-final-boundaries.xml`11/11定向EditMode，`campaign-final-play.xml`6/6定向GPU（新四项+原两关闭环）通过。新增一个逻辑用例与先前全量合计356个不同EditMode用例有通过记录，不称为一次全量356/356。
- 无限金币真正不扣款，500次刷新仍保留原金数和九张库；8次低费委托合到三星；正常委托每次整备限成功一次，失败不扣费/次数；独立备战达到10+2编队后拒绝额外无合成购买。各星读取真实新模式HP/攻击/DP与次数，所有首发原动作窗口保持。
- `tools/balance/analyze_campaign_economy.py`固定种子对3/7种五费、0/3张已持有、100/140/180额外预算各10000次抽样；收入273～329含开局，最终战前242～293未扣支出。结果为获取模型，不是完整运营/通关概率，不把最后一场收入提前使用。

最终窗口检查发现整备顶部旧收藏计数仍写10格，已改为独立备战计数，并在准备面板提供“切换测试”；退出场景后立即停止旧面板绘制。`campaign-ui-play.xml`新四项GPU回归4/4通过；场景退出的空Loop保护由后续独立窗口实际点击核验。

- 最终运行源码提交`56469354c88bd3d456afd527cf32ec3d83bcd2a0`，`campaign-final-build.log`：Windows x64 Development，BASELINE_BUILD_PASS 1775481332 bytes、0错误（Unity后处理前统计）；离线纹理替换完成后目录合计778953042 bytes。1237个处理文件、1176张原PNG保持，不手工重传缓存。
- `campaign-final-player.log`独立13关流程passed=True、stage=13、gold=329，`campaign-final-player-exit.txt`为0。这里普通战斗使用测试判胜，只验证真实跨关/商店/结算/奖励幂等和最大存钱路径；不称为13关自然通关。
- `campaign-final-sandbox-player.log`无限金币/九张库存/整合6雪怪4/暂停援军/返回整备passed=True，退出0。此前同功能构建的`campaign-legacy-player.log`原高难度两关9项全True、退出0，原流程另有完整86项及定向6项覆盖。
- 实际可见原生窗口经computer-use操作：领取首关补给10→21、进入商店、正确独立备战计数、准备时切换测试、选择无限金币关，看到∞/8级/整合6雪怪4及六卡，窗口保留供人类试玩。后台ScreenCapture曾报Failed to capture screen shot，未获得文件，不把它计作截图证据；本次窗口图来自实际界面工具观察。
- 仓库4698个跟踪文件/meta与暂存差异检查通过，原两个未跟踪项保留。最终文档补记不改变运行源码；Builds内BUILD-INFO记录上述源码提交。

未做完整13关自然通关、长时人工平衡、第二机或完整41卡经济策略模拟；Boss参数和300金币体验仍需试玩。其他七条羁绊、八名五费构筑专属、银狼没有因本轮批准被记作已上线。无ZIP/Releases。

## 2026-11-04：羁绊与数值企划的纸面检查

本轮仅设计文档与离线分析，基线origin/main为b4b8b2c（已包含加载优化#55），分支codex/synergy-balance-plan。正文见[企划v1.0](proposals/SYNERGY-BALANCE-v1.0.md)，可编辑候选数据见[JSON](proposals/SYNERGY-BALANCE-v1.0.data.json)，复现入口`py -X utf8 tools/balance/check_synergy_plan.py`。

- 实际执行纸面脚本并通过：41个唯一角色参数、九条羁绊最高档成员数量、七套阵容已列最高档、升星生命/攻击单调性、概率行总和、累计经验和三条不透支现金预算。
- 12个有限牌库情景各5000次，固定种子20261104。五格逐槽费用概率、同费剩余库存加权、同店占库和刷新返库；只买一个目标，首店免费，含买牌及后续2金币刷新费用。结果写入本地忽略的artifacts/synergy-plan-analysis.json；正文记录均值/中位数/90分位，脚本随文档提交可重现。
- 样例：3级指定1费三星均价91.0金币；9级指定5费二星107.3；10级指定5费三星（7种五费）301.9。均不含升级、其他卡购买或利息损失，不解释为通关成本或概率。现有40卡与未来银狼分开计数。
- 正文UTF-8/本地链接/九条羁绊章节、参数与表格核对通过；仓库静态检查4674个跟踪文件通过，暂存差异检查通过，仅本任务五个文件。全部新增规则标为待确认，不修改GDD或运行资产。

未运行Unity编译、EditMode/PlayMode、独立Player、EXE构建或人工平衡试玩；未模拟完整策略、动态转型、备战位及关卡胜负。现金可支付不等于买得到指定卡或能赢，纸面强度指标不代替实战。纯文档任务按CONTRIBUTING无需重建EXE；原两个本地未跟踪项保留不提交，无ZIP/Releases。

## 2026-11-04：菜单、进关、商店与图集缓存优化

已获共享接口/构建流程具体范围确认。从实时main建立codex/animation-loading，带入PR54火墙依赖。新ArtCatalog/ArtSequence/ArtMemoryCache/ArtPreparation、PreparedArtTools分别负责元数据、按段动作、受保护缓存、入口分帧准备与离线压缩；原四个角色读取器保留调用接口。调整配置校验、菜单、商店/下一关及构建钩子；无场景/Prefab/包/工程设置修改，已知本地未跟踪文件保留。

计时范围是测试内场景初始化/完整余正面动作，在各自独立Unity进程运行同一基准；不含启动编辑器/编译/首次离线预处理，也没有清空操作系统文件缓存，不称作硬盘冷启动或EXE启动成绩。单次同机样本：

| 项目 | 优化前 | 最终独立进程 |
|---|---:|---:|
| 怪物菜单初始化 | 26701ms | 328ms |
| 余全部正面动作 | 1658ms | 205ms |
| 菜单时Unity已分配内存 | 2143578733 bytes | 409812505 bytes |

- `art-loading-before.xml/.log`与`art-loading-before.txt`为修改前1/1基准；`art-loading-cold-after.xml/.log`、`art-loading-after.txt`为最终独立进程1/1基准。中途混合套件的61/67ms属于已预热结果，不用来宣传。
- `art-prepare.log`：1176个原PNG及61个独立卡面首次处理91851ms，1237个处理文件共278976175 bytes；后续Play检查`rebuilt=0/hash_bytes=0/198ms`。正式构建校验源内容指纹，Play用长度/修改时间快速识别未变文件。1176个原PNG全部Git跟踪，原图及GUID不移动、不删除；Library派生缓存自动生成。
- `art-loading-edit.xml`首344/345，修复Unity外部销毁缓存Sprite的重载兼容后，`art-loading-final-edit.xml`完整345/345通过。
- `art-loading-full-play.xml`完整首79/81。其一是原通用怪物断言仍假定霜星无主动技力，改为读取既定20SP/有技能时暂停不可施放；其二是场景几何用例仍假定进关两帧内完成，增加等待加载结束。`art-loading-final-play.xml`相关6/6通过，覆盖37只怪物部署/暂停/攻击/获胜和重进，以及原地形编辑不被重开覆盖。
- `art-loading-shop-play.xml`7/7通过：新5项加载/缓存用例、霜星正常购买后走实际下一关预加载入口、原后期两关闭环。包括元数据校验不加载战斗帧、真实预处理页读取/不走PNG回退、活跃保护/离场回收/重载、跨帧准备时模拟冻结、取消旧启动不覆盖新挑战、极小缓存预算下当前商品不被回收而反复加载。保留场景用例均获覆盖；不把补测结果说成一次完整套件零失败。
- 本关六名完整守军及五张编队分帧准备约2.5秒，313页读取、PNG回退0（测试环境样本）。动作缓存1GiB为软目标，活跃工作集大时可超出；这不是整个游戏内存/显存硬上限。未做降帧、降分辨率或技能数值变化。

首次独立构建验证捕获商店当前商品待机被预算回收导致反复准备，已补入活跃保护并通过上述专门回归。另一个隐藏霜星验证进程因原诊断开关未开启后台运行而停在首帧，已停止，不计通过；补齐该诊断开关后以最终构建重测。最终构建/Player记录见后续条目。

- `art-loading-final-build.log`：Windows x64 Development、0错误；后处理实际装入1237个文件，在Player副本替换1176张PNG，目录当时合计777665295 bytes。Unity原BuildReport在后处理前统计1774194346 bytes，不能把它当成替换后的实际包体。
- 最终`art-loading-final-late-player.log`高难度两关9项全True；`art-loading-final-basic-player.log`基础两关8项全True；`art-loading-final-frost-player.log`正常种子56购买/拖动/手动技能/暂停/动作锁/尾部清理/重开全True。对应三个`*-exit.txt`均退出0。最终原生进程313页本关准备约1524ms、PNG回退0；当前商店商品只准备一次，不再反复清空。
- 按最终结果合并核对，82个不同PlayMode用例均有通过记录，无未解决失败；不是一次全量82/82跑完。新增7项商店/预加载回归在最终缓存保护修复后通过。原始PNG仍1176/1176被Git跟踪，源项目没有删除图或改GUID。
- 最终`art-loading-final-pipeline.log`再次编译/校验处理流程，1176源PNG、重建0、通过；缓存根目录由Application.dataPath推导。仓库4671跟踪文件/meta检查与暂存差异检查通过，暂存仅本任务36个文件，不含已知两个本地未跟踪项、构建、缓存或AGENTS。

尚未进行第二台机器、全新克隆、长时人工试玩或战斗整体CPU/GPU帧率分析，不宣称所有持续卡顿均已解决。不创建ZIP/Releases，原作媒体权利记录保持。

## 2026-11-04：余偏红火墙

用户批准动态方向并明确追加偏红色直接接入。从实时origin/main fdbbc69建立codex/yu-firewall-vfx，仅改余的私有火墙表现、新增内部辅助/Shader、调整表现断言和文档；不改战斗模型、人物资源、其他技能、场景、Prefab、包或工程设置。已知.vscode和PackageManagerSettings.asset保留且不提交。

- `artifacts/yu-firewall-play.xml`首轮4/4 GPU通过，包含第四批四名原技能场景、新火墙2项、六人真实事件场景。
- `artifacts/yu-firewall-final-play.xml`最终色阶4/4 GPU通过，包含新火墙2项、六人真实事件和实际高难度地图完整获胜计划。与首轮合计5个不同GPU用例通过。验证原1.6秒起势后点燃、实际红色GPU像素、火焰帧变化、两个真实墙法线/中心、暂停几何与帧冻结、原技能结束后收尾、重开/切换清理；其他五名守军特效仍出现，初雪攻击间隔无攻击风雪。
- `artifacts/yu-firewall-logic.xml`：AdvancedDefenderTests 15/15 EditMode通过，覆盖原消弹、法术穿墙元素量、近战不消除、临时生命强化/回复及第四批其余规则。没有修改这些规则。
- [实机图](art/yu-firewall-actual.png)来自真实D22单人测试的1280×720相机RenderTexture，不含IMGUI。高难度地图也由GPU场景验证，不把项目外视频称作实机。

- `artifacts/yu-firewall-build.log`：Windows x64 Development，BASELINE_BUILD_PASS **1774183498 bytes、0错误**；标准`Builds/Windows/YesterdayConvoy.exe`已更新。
- `artifacts/yu-firewall-player.log`：独立Player高难度两关闭环`won/complete/pause/purchased/carry/reset/framing/restart/exit`全部True，`yu-firewall-player-exit.txt`退出0。此项验证独立运行通用闭环，新火墙的具体颜色/几何/生命周期由前述GPU用例验证。
- 仓库4656个跟踪文件、Unity meta与暂存差异检查通过；新增运行Shader、源码、测试及meta均跟踪，复用的火焰/烟雾PNG原本已跟踪，没有本地媒体缺口。

本轮不跑完整EditMode/其余全部PlayMode，不做长时人工平衡、整体性能优化、全新克隆或第二机。原作角色权利状态保持；火焰/烟雾复用已跟踪CC0资源，新增Shader/meta随源码。不生成ZIP/Releases，不自动合并main。

## 2026-11-03：塔露拉普攻分层表现

人类确认项目外动态看稿后继续codex/project-layout-scenes / PR53。仅EngagementView私有普攻绘制、内部TalulahAttackVfx、独立Shader和Resources烟雾及测试/文档；不修改战斗模型、卡牌配置、人物动作、火雨、场景、Prefab、Packages或ProjectSettings。既有.vscode与PackageManagerSettings.asset未提交覆盖。

- talulah-attack-play.xml：9/9 GPU PlayMode通过、0失败/跳过，过滤TalulahAttackPresentationTests、TalulahSceneTests、FireRainPresentationTests、TestSettlementSceneTests。新2项验证实际塔露拉交战中的火焰帧/位置连续变化、暂停不推进、真实命中与薄烟/火星、1.7秒间隔/0.65秒命中保持、GPU像素有实际特效、重开/换角色清理和原火雨可见。左右方向使用公共战斗模型从正反侧产生的真实普攻事件，在独立表现宿主检查火焰朝向目标；不冒称自然地图转身验收。
- 实机图art/talulah-attack-actual.png为1280×720相机RenderTexture读取，不含IMGUI覆盖层；人物已有受击染红未改。项目外视频不是Unity实机。
- talulah-attack-build.log：Windows x64 Development，BASELINE_BUILD_PASS 1774171185 bytes、0错误，标准Builds/Windows/YesterdayConvoy.exe更新。talulah-attack-player.log原高难度两关闭环9项全True，talulah-attack-player-exit.txt记录真实退出0；这项是通用闭环回归，塔露拉新普攻画面由上述GPU场景验证。
- 仓库4648跟踪文件及Unity meta检查通过，暂存仅本任务文件。
- 复用FireRainEmbers原25帧；新烟雾与Assets/Art/VFX/Common/particle-smoke-01.png字节及SHA256相同（e8724c219e8d35859167fc0a7e207e13c72ccf0c29704909b9bb3d3dc71c6cf7），97,452字节，原Kenney CC0依据保持。新Resources文件/Shader/.meta随源码；原作角色权利记录保持。

首轮助手方法重命名笔误DrawDrawSprite造成编译失败，修正后上述9项全部通过。测试失败或未运行项不记通过。本轮为私有表现改动，未重跑全量EditMode/其余PlayMode，不做长时人工、第二机/全新克隆或整体性能优化；没有新增音效或ZIP/Releases。

关键数据流：现有CombatShot/Clock→私有普攻助手→帧/几何/粒子池，仅渲染；未实际命中的事件不伪造爆燃。每帧重采样而非独立计时，暂停停帧，重开清理。只在首次塔露拉普攻时创建助手；深度测试隔离于原FireRainAdditive。

## 2026-11-03：霜星手动冰封与正常商店混编

用户批准霜星看稿后，在具体范围确认中要求同时纳入高难度商店。继续codex/project-layout-scenes / PR53；保留既有未跟踪.vscode与ProjectSettings/PackageManagerSettings.asset，不提交覆盖。

- `frost-final-edit.xml`：完整345/345 EditMode、0失败/跳过。新增4项覆盖1.1秒延迟/范围法伤/只冻最近两名/重复请求、暂停与3秒移动锁及SP、目标死亡后完整施法、混编只复起霜星且回自己的上限、施法者倒下取消未命中事件、互斥技能配置。既有普通攻击/冰环/冻结/复起与经济逻辑回归保持。
- `frost-final-play.xml`：相关14/14 GPU PlayMode、0失败/跳过，含FrostSealSceneTests、原霜星自然冰环/复起、TestSettlementSceneTests、FireRainPresentationTests、BattleShopLoopSceneTests、LateBattleShopSceneTests、MonsterSelectionTests。实际M14菜单入口、20点初始SP、12.05秒自然就绪、原Skill_1帧连续前进、3秒实际位置锁、真实目标冻结、黑冰/风雪Sprite、暂停停帧、动作结束但尾部仍显示、到期及重开无残留均通过。回归旧火雨/基础闭环与正常后期通关。
- 混编GPU场景用真实角色列表搜索一个正常随机种子，没有向游戏注入霜星商品：第一关用明确开发测试判胜，首次进店仍保底塔露拉，其他格正常刷出霜星；扣5金币、九张库存、备战M14/idle、第二关真实拖动、按自己的技力施放成功。基础模式不注册霜星，重开移除本次购买。
- 媒体检查：24PNG、28,411,363字节、570帧/60fps；RGBA、Alpha含0/非零、最大2048、逐页SHA核对，复用烟雾与原Kenney粒子字节相同。源/权利见art/FROSTNOVA-SKILL-IMPORT.json，不把用户上传授权记为原作开放许可。
- `frost-final-build.log`：BASELINE_BUILD_PASS，Windows x64 Development，1,773,111,245 bytes、0 errors，保持两个显式场景。标准Builds/Windows/YesterdayConvoy.exe已更新。
- `frost-delivery-frost.log` / `frost-delivery-frost-exit.txt`：独立Player，正常随机种子56、实际购买/拖动/手动施法/暂停/动作锁/尾部/重开汇总True，进程真实退出0。同一最终EXE的frost-delivery-late.log原高难度两关闭环9项全True，frost-delivery-late-exit.txt确认退出0。此霜星证据经开发测试结算进入商店，不冒称人工从第一关自然通关；本轮自动Player没有留存截图，实际图像由GPU相机另验。

实际图像检查：frost-visual-final.xml再次2/2通过（重复覆盖不累加14项），相机RenderTexture读取1280×720实际GPU画面，见[黑冰实机图](art/frostnova-actual.png)。为避免黑冰遮住目标脸部，保持原形状等比例收敛至0.82；没有重画目标或霜星。最终frost-final-build.log再次构建1,773,111,245 bytes/0错误。仓库4638跟踪文件检查通过。

首轮Editor校验访问internal资源类造成编译错误，改为现有配置/文件依据；首轮EditMode 344/345中混编用例未配置独立卡槽导致部署fixture为空，修正为实际ConfigureCardLoadout后完整345通过。失败轮没有记作通过。相关GPU首轮7/7，扩展后最终14/14。实际相机图像复验单项结果见下，不把重复用例累加为额外功能覆盖。

关键连接：只读EngagementDefinition快照→每实体SP/施法事件→FrostSeal延迟伤害与FrozenUntil；DemoSpriteVisual读原Skill_1，FrostSealView/Art读事件和模拟时钟显示，不结算伤害。冷色屏幕微移不改变相机/输入，混编不借用首卡技能。未进行全余下PlayMode、长时人工平衡、第二机/全新克隆或性能优化；不制作ZIP/Releases。

## 2026-11-03：进店免费刷新保证5费塔露拉

明确授权范围及“自动进店第一排”来自用户两项确认，继续codex/project-layout-scenes / PR53；原.vscode与PackageManagerSettings.asset未改/未提交。

- ShopTrialModel内部GuaranteedRefreshOffer只在刷新事务成功时清除，先将未购商品返库，再从实际库存保留第一格；锁店、无钱、卡片未解锁/无库存失败均不扣费/不消耗保底，Preset清理。普通调用签名和抽卡权重保持。
- BattleShopLoop仅高难度注册现有5费塔露拉，并在第一关胜利打开商店前设置保底；正常免费刷新执行保底。基础卡池不变，后续随机仍按十级费用概率。
- ShopBenchModels复用塔露拉原待机帧；BasicRosterProfile只为原TALULAH/火雨配置增加明确例外。EngagementCombat技能资格、技能面板/技力条及FireRainView改为读取实际实体配置。此前主卡是无技能弩手时会被错误拒绝，或者缺火雨特效，现由场景用例验证。
- talulah-shop-all-edit.xml：341/341完整EditMode通过，0失败/跳过，包括原技能兼容、刷新事务与九张库存/保底失败边界。
- talulah-shop-ready-play.xml：14/14 GPU通过，0失败/跳过。新增实际流程用例先用开发测试结算结束第一关，验证免费首格塔露拉/无额外扣费，购买38→33、备战TalulahOriginal/idle、切换商店不再补货、第二关真实拖动、初动4.2秒技力满、手动火雨成功且VisibleStreams>0，重开移除购买卡，基础模式不注册塔露拉。并回归旧单卡火雨、原商店、测试结算与两关闭环。

最终 talulah-shop-build.log：Windows x64 Development，1672420786 bytes、0错误；talulah-shop-player.log 两关闭环9项全True，自动验收Player实际退出0（见talulah-shop-ready-delivery.out）。本地标准Builds/Windows/YesterdayConvoy.exe已更新，无ZIP/Releases。

首次编译的测试引用了错误的战斗级FireRainStartedAt，改为实体字段；首次GPU购买暴露基础名单拒绝技能角色，下一轮暴露主卡无技能导致NoSkill，修复资格/显示/特效按实体取配置后，上述最终14项通过。失败轮不记通过。未人工完整点选/长时平衡、第二机或重跑全部PlayMode；本次保底为试玩覆盖，不写成正式经济概率。

## 2026-11-03：目录迁移与显式地图场景

主导者E盘最终验证：已切到codex/project-layout-scenes，Unity实际导入及AUTHORED_SCENES_VALIDATION_PASS通过，无缺失脚本；原.vscode三文件与PackageManagerSettings.asset共四文件SHA256核对不变。清掉BattleShop中残留的隐藏Industrial Map后，organized-windows-final.log再次校验通过，正式Builds/Windows/YesterdayConvoy.exe构建1672419494 bytes、0错误。此轮清理仅移除原本已隐藏的重复场地，不改战斗配置；最终E盘Player结果见后续记录。不要再让人类切到Codex工作目录打开工程。
E盘标准EXE organized-final-player.log：后期两关9项全True，真实购买/带卡/重置/取景及退出通过；未捕获异常。最终校验允许以后在Assets/Scenes新增更多独立地图，不把当前两图数量永久写死。

范围由用户明确确认：从main dc92829吸收PR52原商店恢复；移动资源并保留GUID，保存两个可编辑场景，退役其他地图/生成器与对应场景测试，更新路径/菜单/构建和目录规范；最终同步到主导者E盘单一日常工程。未升级Unity或包。

- layout-edit-final.xml：339/339 EditMode通过。包括新增检查两个实际场景、500以上已保存地形Renderer、持久材质/Mesh、无缺失脚本；原场景数和八人混编测试按退役范围更新。
- layout-play-final.xml：完整68项中67通过，剩余一项旧断言要求隐藏原地图，与本轮复用静态场地相反；修改该断言后layout-play-confirm.xml三项通过。其余逻辑未为此改动。
- layout-entry-play.xml：最终11/11 GPU通过，覆盖真实场景返回、重开不重建/不丢弃地块编辑、八人技能、基础/后期购买带卡及测试结算。保留的69个不同场景用例均已覆盖通过（不是单次完整69项运行）。
- layout-build.log：AUTHORED_SCENES_VALIDATION_PASS；最终Windows1672761774 bytes、0错误。构建明确包含BattleShop与MonsterTrial。
- layout-player-late.log / layout-player-basic.log：最终独立Player分别9/8项全True，均退出0；完成真实部署、胜利、购买、第二关、暂停和重开。
- 编辑模式Camera.Render实证：[商店战斗地图](art/BattleShop-authored.png) / [怪物试用地图](art/MonsterTrial-authored.png)，两图均在未进入Play时渲染。并非AI概念稿。角色由运行时生成，商店场景编辑模式显示已保存地形。
- 仓库检查4562个跟踪文件通过（新增证据图/文档后以最终提交复核数为准），scene/meta/GUID检查通过。核心角色运行图集没有修改或漏传。

迁移首轮编译发现共用截图方法曾在删除的地图诊断文件中，已提取保留；一次性Editor烘焙器改为普通编辑器类后成功；运行回归发现守军独立配置标记Tactical但仍需试用场地，以及退出销毁顺序引用，已修正并以上述最终回归为准。失败轮不记最终通过。

未做：人工遍历每个角色/技能、第二台电脑、全新克隆与长时性能；已退役旧地图宿主中的专门场景回归不再运行，能力核心逻辑保留。同步E盘保留原未跟踪.vscode和PackageManagerSettings.asset，不提交/覆盖；最终导入校验另附本节后续记录。无ZIP/Releases。

## 2026-11-03：恢复霍利屹战后商店

修改BattleShopInterface与ShopMapTrial：把原商店的头部/横卡覆盖层抽为私有DrawShopMarket，让旧商店与关间商店共用同一绘制；原卡面/费用底色/羁绊/刷新/锁店/升星保持。关间只加开始下一战，保留闭环状态机、原子交易、战斗卡栏和现有备战模型；未改公共签名、场景、Prefab、Packages或ProjectSettings。

- restore-shop-final-play.xml：11/11 GPU PlayMode通过，0失败/跳过，覆盖ShopMapSceneTests、TestSettlementSceneTests、LateBattleShopSceneTests、BattleShopLoopSceneTests。扩展既有测试，验证关间收起/展开不改金币/商品/相机，收起也不允许部署，并继续购买、第二关与终局。
- restore-shop-final-build.log：最终Windows x64 Development，1677281994 bytes、0错误，Unity6000.6.5f1。
- restore-shop-final-player.log：最终独立Player LATE_BATTLE_SHOP_CHECK 9项全True：两关获胜、通关、暂停、实际购买、带卡、战斗重置、取景、新挑战和退出。
- 原生窗口首轮实际点击测试结算→胜利→商店，检查顶部五横卡/侧边标签、橙色刷新和锁图标、下方待机平台；点击2费酸液源石虫，金币38→36，商品消失并播放落地。发现底部旧战斗卡栏残影，随后增加清底及下一战文字对比度，再运行以上最终11项和构建。最终新增两行显示修正后未再次完成全套人工按钮点击，不冒称最终人工全流程通过。

本轮完整EditMode/完整PlayMode未重跑；最近339/92完整结果属于前轮合并验证。未测第二台电脑/全新克隆、全分辨率和长期性能，性能优化继续暂缓。本轮无新媒体，无新增授权或运行依赖，不制作ZIP/Releases。最终素材/源码以本分支提交、本地BUILD-INFO及PR为准。

## 2026-11-03：两个商店 PR 的历史冲突整合回归

#47（商店）与 #51（两关闭环）均因 #50 squash 带入共享商店/地图代码而冲突。先逐文件核对 #47 与 main：主线包含其全部功能并追加挑战/详情，解决6个冲突后 Git tree 与 e14c8ca 完全一致，未丢弃任意商店行为。#47 正常 squash 合并为0821c2c。#51 吸收整理后的共享历史与实时main，解决9个重叠文件，保留闭环分支及旧入口、TacticalViewport坐标、独立奖励与现有两份证据；最终除本段文档更新外，与cbcdad1的运行文件逐字节相同。

- artifacts/pr-merge-edit.xml：完整339/339 EditMode通过，0失败/跳过。
- artifacts/pr-merge-play.xml：完整92/92 GPU PlayMode通过，0失败/跳过，覆盖商店、两关闭环、测试结算、旧地图与特效。
- py -3 tools/check_repository.py：4619跟踪文件通过；差异空白/冲突标记检查通过。
- artifacts/pr-merge-build.log：STRONGHOLD_BUILD_PASS，1677283162 bytes，0错误，Unity6000.6.5f1、Windows x64 Development。
- artifacts/pr-merge-player-late.log：后期两关9项全True，购买、跨关、暂停、取景、重开和退出通过，退出0。
- artifacts/pr-merge-player-basic.log：基础两关8项全True，实际购买卡进入第二关、奖励流程、暂停、重开和退出通过，退出0。
- artifacts/pr-merge-player-shop.log：原商店19项全True，落地/金光收尾检查True，退出0。

上述为本轮重新执行结果；未重复人工点击全流程、第二台电脑/全新克隆或长期性能。没有新增素材、游戏规则或公共接口，不更新ZIP/Releases。源文件、必要.meta与已有运行资源保持跟踪；本轮合并依据用户明确指令，不绕过GitHub保护。

## 2026-11-03：新版商店与底部卡栏费用底色

定位：git blame确认旧ShopMapTrial中ShopTier/费用渐变来自shigaharuki17的7a4e700；新版BattleShopInterface的DrawLoopShop/DrawLoopRoster只填LoopPanel，缺少该调用。仅修改此绘制文件，新增私有共用底色绘制，原ShopTier和费用/星级/DP数据不变；不改公共接口、素材、场景、Prefab、包或设置。

- cost-background-play.xml：相关5/5 GPU场景通过，无失败/跳过，原基础/后期闭环、招募带入第二关和测试胜负流程保持。未为此低影响绘制修复添加镜像测试或重复全量逻辑/PlayMode。
- cost-background-build.log：Windows构建1677288333 bytes、errors=0，报告总量不是EXE大小。
- 最终独立Player通过Windows界面工具实际查看：准备阶段卡栏1费灰、2费绿、3费蓝、4费紫；测试判胜后进入商店，2费商品绿、3费商品蓝且同费同色，图片原配色和名称/价格可读，GUI颜色未泄漏。5费金沿用原映射，本次随机商品中未出现，未作实机5费抽验。

未做第二台电脑或全量回归；没有新增费用规则、5费商品或新美术方向。资料见BATTLE_SHOP_LOOP，源码与新本地构建对应提交见BUILD-INFO；不生成ZIP/Releases或自动合并main。

## 2026-11-03：测试结算开关

改动为BattleShopInterface的开关/面板/测试标记、BattleShopLoop私有控制、ManualBattle程序集内部终态入口、ShopMapTrial面板点穿保护，新增TestSettlementSceneTests及meta。Debug.isDebugBuild保护，普通部署/伤害/胜负判断不调用测试入口。

- test-settlement-edit.xml：相关18/18，0失败/跳过；覆盖原ManualBattle与战后金币/利息/经验/库存/幂等逻辑。
- test-settlement-play.xml：相关5/5 GPU场景，0失败/跳过；包含新2例准备直接胜利、暂停带单位失败、默认关闭/关闭拒绝/点穿保护、重复请求不改判不发钱、真实购买与第二关重置/通关、失败终局与重开；原基础/后期两关闭环回归同时通过。
- test-settlement-build.log：StrongholdMapTools.Build成功，Windows1677287732 bytes、errors=0（报告总量，不是EXE大小）。
- 使用Windows电脑界面工具在此最终独立Player实际点击：展开开关、判胜利，看到第1关测试结算与金币30→38；点击前往商店并真实招募，金币38→35；进入第2关后开关恢复关闭，DP24；展开判失败，看到本次挑战结束与金币35→43，没有商店继续按钮。只操作新开的验证Player，原用户对局未点击/关闭。

未运行完整逻辑/全部PlayMode套件、第二台电脑或非Development Player；后者由条件保护但未另构建。此为测试快捷入口，不将直接判胜当成自然通关或平衡证据。无新美术素材，不改场景/Prefab/包/设置；本地新目录交付，源码仍走功能分支/PR，不合并main或生成ZIP。

## 2026-11-03：六人分层技能特效

接入范围、逐页SHA与来源见[特效说明](art/DEFENDER-SKILL-VFX.md)和[来源记录](art/source-records/defender-vfx-v4.json)。主要改动：AdvancedDefenderView、EngagementView、DefenderRosterView；新增DefenderSkillVfxView、DefenderVfxArt、DefenderVfxImporter、两份测试及49张运行PNG/清单/meta。没有改技能或经济逻辑。

- `vfx-release-edit.xml`：最终完整339/339，0失败/跳过；新增12项非循环有效窗口、纹理尺寸/过滤和持续片段时钟循环检查。PNG共674帧、49页、20,671,871字节，RGBA/尺寸/页内计数/未触边检查通过。
- `vfx-full-play-final.xml`：完整90/90 GPU场景，0失败/跳过。新增真实六人技能事件的贴图显示、暂停同帧同位置、不推进生命/时钟、初雪攻击间隔无攻击风雪、多目标仅一组前后层、重开清理；大地图沿原部署方案实际通关，全部六组新特效均出现。
- 随后只加宽金斩纹理、修正火焰循环采样接缝，`vfx-release-play.xml`最终2/2再次通过，并重新采集实际地图画面；完整339也在最终纹理上再次通过。最后保留塞雷娅普通近战原弹道的兼容判断，`vfx-final-saria-play.xml`相关3/3通过；未重复无关完整逻辑与90项场景。
- `vfx-build-final.log`：最终StrongholdMapTools.Build成功，Windows1,677,286,421 bytes、errors=0（报告总量不是EXE大小）。`vfx-players.json`三个最终独立Player：late/basic/legacy-challenge全部退出0，未捕获运行异常，后期/基础战后商店、卡牌带入下一关及旧挑战仍通过。
- 查看了真实大地图狼形、火墙、风雪和金斩相机截图；[连续实机相机采样](art/defender-vfx-gameplay.mp4)为60张真实Unity GPU画面、20fps/3秒，含前后风雪展开与散尽，不含OnGUI HUD，也不是人工录屏。项目外看稿不冒充实机证据。

发现并修复：首次图集导入的类型/刷新状态导致Resources返回null；明确2D导入并逐页ForceUpdate校验49页后，最终全部能读取。首次完整GPU89/90发现重开后的望特效池仅隐藏未销毁；按战斗对象/RunId更换销毁池中旧对象，完整90/90复验通过。另修新测试把同对象下正常角色误算成残留的断言，仅检查VFX对象；未弱化棋子旧清理测试。失败日志/XML保留在artifacts。

未运行：人工完整试玩、第二台电脑/全新克隆、性能Profiler/低配压力。原作完整VFX工程仍缺，这组是适配重制；新增PNG约20.7MB磁盘资源，不能等同GPU内存或宣称性能改善。原角色媒体权利状态保持原记录，用户接入/上传决定不构成权利人许可。未制作ZIP/Releases、不自动合并main；PR #51既存冲突另行处理。


## 2026-11-03：场上六名干员动作与原作棋子接入

按人类确认看稿范围接入30段角色追加动作与望棋子3段，共2,755帧、70PNG/93,688,811字节、13清单及必要meta。主要文件为DefenderArt、DefenderRosterView、AdvancedDefenderView、12个角色目录、D28Chess和两份测试；未改战斗结算、技能数值、路线、商店、场景/Prefab、包或工程设置。具体资源与数据流见[接入说明](art/FIELD-ANIMATION-COMPLETION.md)。

- `field-full-edit-final.xml`：完整327/327，失败/跳过0。新增9例资源尺度、原点、变体、棋子与销毁缓存恢复检查。首轮全量321/326暴露先前加载的Sprite被Unity清理后仍留在缓存；修复失效检查并增加重现测试，最终完整复验通过。没有为此改变启动加载策略或做性能优化。
- `field-full-play.xml`：完整88/88 GPU场景，失败/跳过0；新增六人入场→待机、暂停冻结/不推进逻辑、比例/朝向、重开，以及真实棋子创建/触发动画/暂停/清理。旧角色、技能、部署、地图、跨关与商店场景保持。棋子表现测试用具备动作间隙的独立配置保证能触发技能；最初测试配置的动作占满间隔而未释放技能，仅修测试配置，生产数值未改。
- `field-build.log`：Windows构建1589837955 bytes、errors=0，使用StrongholdMapTools.Build，独立ShopMap入口；报告总量不是EXE大小。
- `field-players.json`：后期两关、基础两关、旧六星挑战三份最终独立Player退出0，后期won/complete/pause/purchased/carry/reset/framing/restart/exit全部True。隐藏窗口下ScreenCapture失败，本轮不把它当真实屏幕截图通过；画面抽查来自GPU测试的[相机渲染](art/field-late-idle.png)，不含OnGUI HUD，不是HTML看稿或人工实屏验收。
- `REPOSITORY_CHECK_PASS`；70张运行PNG及对应meta均已进入索引，逐页SHA与导入记录一致，最大PNG3269290 bytes，无单文件上传超限。没有加入原始skel/atlas、加工区、Spine渲染库、构建、AGENTS或缓存。

未运行：第二台电脑、人工完整试玩、启动性能与内存分析。原作完整刀光/风雪/火墙/追猎/治疗特效仍有缺口，塞雷娅原背面三技能仍缺；不能把本轮记为完整技能特效还原。新增资源约94MB，加载优化按人类要求继续暂缓。

本轮沿用PR #51；开工已fetch核实main=e14c8ca。与main的12处既存合并冲突未在本轮改动，不自动合并。EXE对应源码与本地交付目录见BUILD-INFO。


## 2026-11-03：取消红门拥堵限制、原生16:9联动窗口

本轮只实现用户要求的两项操作修订，技能还原和启动/卡顿仅答复、未修改相关实现。

- `qol-edit.xml`：完整318/318逻辑，0失败/跳过。原三项拥堵拒绝用例改为占用红门也能部署，同时保留CD/费用/次数/暂停/幂等/非法位置保护；新增8边/角锚点与4种可用区域约束。
- `qol-play.xml`：最终完整86/86 GPU场景，0失败/跳过。新增暂停五张不同怪物拖入同一红门、同位置五实体/各自扣费次数、同卡CD仍冻结阻止重复部署。原技能/地图/交易/跨关等场景保持。
- 首轮85/86有一项余消弹场景未形成所需的消弹观测；放开重合后原连续出兵测试的分布发生变化。测试场景明确限制为单弩手持续射击，保留灼燃、治疗、消弹、动作/暂停/清理全部断言；技能代码与原配置未改。随后完整86项复验通过。
- `qol-build.log`：最终Windows构建1496110308 bytes、errors=0（报告总量，不是EXE大小）。新增原生分支在Windows Mono x64 Player实际编译运行，不把Editor中的条件编译跳过当验证。
- `qol-players.json`：5个最终Player全部退出0、无捕获异常、均安装窗口约束：native-window、late、basic、legacy-shop、legacy-challenge。后期9项全True、购买角色进入第二关；旧商店/旧六星挑战继续通过。

原生窗口测试对本进程发送Windows WM_SIZING（右边、底边、左上角）并应用返回矩形，实际客户区1408×792、1550×872、1678×944；随后实际最大化为2534×1425，再还原。aspect/axesCoupled/pausedState/noBars全True；宽高比例取整误差<=1像素，窗口内部不出现大片留黑，暂停、DP、时钟未重置。[横边](art/window-coupled-width.png) · [竖边](art/window-coupled-height.png) · [最大化](art/window-coupled-maximized.png)。这是原生消息和系统窗口尺寸验证，未记为人工鼠标拖边验收。独立Player启动请求1024×640后实际1024×576，[真实画面](art/window-coupled-ready.png)，两关闭环通过。Unity编辑器测试不安装窗口消息回调、不调整编辑器布局；其任意比例Game视图仍可留边。

实现只对本进程Windows窗口安装comctl32子类回调，未处理消息转发，销毁时卸载；系统窗口组件无需新包或外部插件。尺寸数学独立，不每帧强制SetResolution；仅启动/系统贴靠等产生非16:9客户区时归一化。实现依据：[WM_SIZING](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-sizing)、[WM_GETMINMAXINFO](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-getminmaxinfo)、[SetWindowSubclass](https://learn.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-setwindowsubclass)。

未改场景、Prefab、Packages、ProjectSettings、技能/素材加载代码。未做第二台电脑、其他Windows/DPI/多屏穷举、真实鼠标所有边角手感或长时缩放压力。性能仅只读代码检查：MonsterTestSelection.BuildMonsterTestCatalog→ManualBattleDefinition.Snapshot→CardDefinition.Portrait/HasFrameAnimation→LocalMonsterArt.For同步读取/解码整套图集、Sprite.Create、Texture2D.Compress；这是确认存在的加载路径，尚未测量各环节时间，未把它推断为所有运行卡顿的唯一瓶颈。本轮没有性能优化或新的原作技能还原。


## 2026-11-03交付：后期两关、等比例窗口与原生UI

2026-11-02人类已批准项目外UI看稿和具体范围；2026-11-03完成交付整理。沿用codex/battle-shop-loop / PR #51，不合并main。独立后期预置7级/30金币/24DP、5种怪物对抗6名已有完整技能的六星守军；战后购买/编队带入第二关，旧1级基础模式和旧独立试玩保留。规则、文件连接与操作见[BATTLE_SHOP_LOOP](BATTLE_SHOP_LOOP.md)。

| 本轮实际执行证据（本工作目录artifacts） | 结果 |
| --- | --- |
| late-editmode-final.xml | 完整306/306 EditMode，0失败/跳过；含两关真实混编获胜、技能保留/站位变化、4种窗口比例的坐标往返和相机比例 |
| late-playmode-final2.xml | 完整85/85 GPU PlayMode，0失败/跳过；包含原角色/火雨/地图/商店、基础闭环和新增后期两关，战后新购M15实际进入第二关 |
| late-drag-regression.xml | 最后补齐“关间备战拖动取消保持商店可操作”的修正后，追加1/1后期真实两关场景回归通过；完整85套在这项小修前执行，未假称之后又完整重跑 |
| late-release-build.log | 最后修正后Windows重建1496100411 bytes、errors=0；为Unity报告产物总量，不是EXE大小 |
| late-players.json | 最终构建5个独立Player全部退出0，无捕获异常：后期1280×720、后期1024×640、基础闭环、旧商店、旧六星挑战 |

两个后期Player的won/complete/pause/purchased/carry/reset/framing/restart/exit全True。两场均真实部署与技能战斗；关间使用正常金币刷新、购买新增M15并编队，第二关真的出现该单位；金币/等级/库存沿用同一经济对象，DP/实体重建，重开恢复后期初始资源。没有直接扣守军生命/强制胜利来通过。基础闭环的8项、旧商店及购买落地/收起拦截、旧六星挑战12项继续通过。

1024×640真实截图为1024×576内容，上下各32黑边，黑边像素实际为(0,0,0)。缩放和输入统一使用TacticalViewport，未继续分别按宽/高拉伸。真实鼠标检查：弩手拖到上红门24→23DP；暂停后酸液源石虫拖到下红门23→21DP且保持暂停；猎狗拖到非法位置后21DP及4次保持。对同一窗口实际最大化后仍21DP且暂停，画面居中留边、角色/UI比例保持；原生截图[最大化实机](art/late-demo-native-maximized.png)。本轮鼠标拖边未能通过自动化命中窗口边框，未将其记为实测成功；可调整边框属性沿用前轮已验证的窗口设置。本轮已验证最大化和两种独立窗口尺寸，不等于穷举所有拖边/DPI情形。

[战斗](art/late-demo-ready.png) · [结算](art/late-demo-settlement.png) · [商店与原立体备战](art/late-demo-shop.png) · [通关](art/late-demo-complete.png) · [1024×640留黑](art/late-demo-letterbox.png)。均为最终Unity Player截图，非HTML看稿或AI重绘。

验证中修正了遗漏的旧绘制调用与Unity库不支持的LINQ重载；等比例化后旧场景测试的鼠标换算改用新坐标约定，地图可见性在画布内检查。火雨GPU抓图使用独立16:9 RenderTexture的完整视口并恢复现场，避免把桌面窗口留边带入离屏采样；原技能/冻结/收尾断言保留，最终完整套件通过。关间拖动不再收起必需商店，Tab/Esc仅在旧模式继续切换商店；完成小修后追加场景与最终5个Player复验。

本轮不修改场景文件、Prefab、Packages或ProjectSettings；测试/构建期间Unity重写设置的规范化Git内容与HEAD一致，未纳入提交。原E盘工程未切分支，既存未跟踪PackageManagerSettings.asset哈希继续保持312F6AF74DD911D570E7F4B6660F4D0D37682F2146564CEE297229B558A230D2，不提交/覆盖。复用原角色/地图/运行素材、保留来源和真实权利记录。新增源码/meta和实际截图随功能分支交付；无ZIP/Releases。

未测第二台电脑离线、全新克隆、全随机商店/阵容、低配长时和多屏/DPI穷举；人类难度仍待试玩调整。当前仅两关/基础10种可运营怪物，升星不加战斗属性、羁绊未实现，不代表完整后期自走棋。真实EXE与对应源码提交见本地BUILD-INFO；旧验证记录按当时版本阅读。


## 2026-11-02：可调整窗口大小

本轮明确按人类“自由调节窗口大小”要求，将ProjectSettings/ProjectSettings.asset的resizableWindow从0设1；普通Player启动在ShopMapBootstrap.Awake设置最多1280×720窗口，只执行一次，显式-screen参数保持。ShopMapTrial在尺寸变化后重算镜头取景，ManualBattleController.Update调用，不改变战斗状态，仅取消未完成拖拽。

window-resize-final-build.log：Windows1496083727 bytes、0错误。window-native-results.json：实际Win32窗口resizeBorder/maximizeButton均True，普通启动1280×720；同一进程通过原生窗口接口调整为980×620、1180×660，各等待2秒后保持，未被程序强制恢复。首次检查过早采到场景加载前的旧尺寸，改为等启动尺寸生效再验收，未把该次失败记为通过。

window-loop-results.json：1024×640与1280×720两例独立Player全部退出0，八项闭环检查全True，无捕获异常，包含购买/实际新单位/通关/暂停/重置/退出。已查看1024×640商店截图。没有写额外镜像测试，也未重跑完整EditMode/PlayMode；下方299/84是此前闭环交付的结果。未穷举极端纵横比、多显示器/DPI与所有旧场景，未做人工拖边手感验收。


## 2026-11-02：两关闭环及角色接地

本轮最终完整299/299 EditMode、84/84 GPU PlayMode，0失败/跳过：artifacts/loop-grounded-EditMode.xml、loop-grounded-PlayMode.xml。新增5项逻辑覆盖奖励幂等/利息先快照/上限/连胜/XP、锁店和库存跨关、免费刷新与购买跨关、失败终局、战斗中不能运营；新增2项GPU为真实两场战斗/购买单位上场/重置与重复结算、失败奖励/禁止运营/新挑战清理，并检查脚底阴影高度和比例。

最终StrongholdMapTools.Build通过：1496083099 bytes、errors=0，artifacts/loop-grounded-build.log。此为Unity构建报告统计，不是EXE大小。artifacts/loop-release-players.json的loop、legacy-shop、legacy-challenge三个最终Player均退出0，无捕获异常。新闭环won/complete/pause/purchased/purchasedUnitInStage2/combatReset/restart/exit全True；旧商店及六星挑战继续通过。已查看结算/商店/通关和接地后实际截图，不以AI看稿替代实机证据。

初次编译测试引用了非公开坐标工具，改用测试局部坐标转换；第一次场景暴露SyncShopDeck的准备阶段限制挡住新关创建，修为显式换关重建，不绕过普通交易限制。独立Player首轮在商店滑回未完就模拟向上方红门出兵，被原覆盖层正确拦截；验收按实际操作等待0.25秒后重新出兵并最终通过，未把该次失败当通过。最后美术接入后上述完整套件和三个Player重新运行。

源图/原动作未改，阴影由本项目程序生成，Shader保留原脚底深度方案，仅增加局部染色；不是完整动态光照。主场景、Prefab、Packages、ProjectSettings未改，原E盘未跟踪PackageManagerSettings.asset的SHA仍312F6AF74DD911D570E7F4B6660F4D0D37682F2146564CEE297229B558A230D2，不提交/覆盖。

未测第二台电脑、干净克隆、全随机阵容/种子、低配长时性能和人工平衡；升星仍不增加战斗属性，羁绊仍为标签。此前其他日期验证按其阶段阅读，不算本轮重新执行。详情与截图见[BATTLE_SHOP_LOOP](BATTLE_SHOP_LOOP.md)。


2026-11-01名牌修订：仅移除头顶名字后的地形文字，不改战斗规则。build-clean-defender-names.log：1496063048 bytes、0错误；player-clean-defender-names.log：12项全True，14次部署126.35秒0DP通关。已查看无地形后缀的实机名牌并更新5张截图；本轮未重复完整逻辑/场景套件。

## 2026-11-01：六星挑战扩建版

[试玩、数值、参考打法与边界](STRONGHOLD_SIX_STAR_CHALLENGE.md)。新增9行双翼布局与六星技能混合防线；实际高台/地面近战索敌限制；每个蓝门各突破一次；两个红门拖动路线与点击守军详情。

| 执行证据（D:/游戏开发/artifacts/shop-map/） | 结果 |
| --- | --- |
| edit-challenge.xml | 完整292/292通过，0失败/跳过：墙/高台/固定路障绕行、角色深复制不写原asset、近战/远程高台规则、重开、两目标、真实混合通关与单兵种/单翼负例 |
| challenge-timing.xml | 此后追加参考波次整体提前/延后1秒容错，挑战类11/11通过；原完整套件不重复，总独立逻辑用例294 |
| play-challenge.xml | 11/11 GPU场景通过：原6商店+原3地图+新2挑战，涵盖双红门预览/移开隐藏、真实平台、详情面板拦截/开店关闭、重开/两旧图切换和清理 |
| challenge-balance.csv / challenge-balance.log | 180受限脚本6次通关；其余有失败与未完成，不能称人类胜率。保存成功请求重放，未用伤害/生命作弊 |
| build-challenge-delivery.log | 最终Windows1496063048 bytes、0错误；此前显示修正包括弩手初始编队文字、下移目标说明及去掉易混淆的伤害类型泛称 |
| player-challenge-delivery.log | 最终Player12项全True：布局/镜头/点击详情/路线/清除路线/购买/拖动部署/暂停/战斗中详情/双目标胜利/重开/旧图 |

实际1920×1080可见Player通过真实备战席拖动14次，126.3500019秒，剩余0DP，7次传送，4次实际穿门（重复同一目标不重复扣另一目标防御）。守军发动次数D21=1、D22=1、D27=1、D28=2、D29=1、D30=1。5张最终截图存docs/ui-previews/stronghold-challenge；已查看路线颜色/编号/箭头、详情实时字段/范围圈、平台/地面身份、战斗和结算画面。

首轮编译发现详情面板使用不存在的名称字段，已修正并重跑；首轮实机发现初始单卡没有多卡状态时误写未编队，以及说明挡住名字，已修正重建、以上述最终日志为准。未把这些失败/旧画面记作最终通过。

未测：人工平衡、第二台电脑/全新克隆构建、完整其余PlayMode、低配及长时性能。原图共享防御规则保持；仅挑战版启用每目标一次与近战不可打高台。星级不加战斗属性。没有新增素材许可声明或新原作媒体。旧记录保持历史证据。

## 2026-10-31：地图材质、源石与传送光效

[来源与材质使用范围](art/STRONGHOLD_MAP_MATERIALS.md)。运行纹理复用已有CC0文件及meta，像素/导入设置不变；新增两Shader、显式引用StrongholdMapArt.asset和一张动态特效网格。不改格点、路线和战斗数值。

- play-stronghold-polish.xml：9/9定向GPU PlayMode通过，0失败/跳过。原6项商店、原2项地图继续通过；新增验证2K纹理引用、3类法线材质、两个Shader实际支持、特效不生成战斗实体、战斗暂停冻结/继续推进、退出清理。
- 此后只补Smoke模式Awake提前后台运行，以及结算时用PresentationClock让瞬时光效结束。build-stronghold-polish-delivery.log：Windows1496034691 bytes、0错误；最终运行源码此后未改。
- player-stronghold-polish-delivery.log：原13项玩法和texturedSurfaces/batchedEnergy/pause/portalFeedback/effectsEnded共18项全True，无捕获异常/Shader错误。编队30.89秒模拟时间获胜，11次部署、2次突破、7次传送、9次感染触发、166点感染伤害；结算推进1.2秒后瞬时光圈/光柱清空。
- 最终实际1920×1080窗口59张证据帧，复核贴图/晶体/等待传送/出场；docs/ui-previews/stronghold04更新5张实机截图与24帧源石、28帧传送WebM。8fps观察用编码，不作为实际游戏FPS/精确计时证明。
- 初次可见窗口失焦停在启动阶段、未生成有效画面，不记通过。提前开启自动验收后台运行后重新构建并以上述最终Player为准。

证据根D:/游戏开发/artifacts/shop-map/。本轮未重跑完整EditMode及其余PlayMode，前一版283逻辑和旧图23项保持历史记录。未测低配GPU/长时性能/第二台电脑或全新克隆构建；不冒称4K全套PBR、烘焙AO或Bloom后处理。原主场景/地图asset/Prefab/Packages/ProjectSettings、共享贴图和meta未改，.vscode继续未跟踪。

## 2026-10-31：盟约#04格点、源石与传送

[来源、操作与适配边界](STRONGHOLD_MAP_04.md)。原格点转录自固定提交a24a8ad，原始参考JSON仅本地；工程不包含原地图贴图或模型。

| 实际执行 | 最终结果 |
| --- | --- |
| edit-stronghold-final.xml | 完整283/283通过，0失败/跳过。5项新逻辑用例检查格点/路线绕开封闭与固定工事、感染攻击/攻速/真实扣血/暂停/清理、3秒传送与不可直接受击、旧图无感染、耗尽失败和重开。 |
| play-stronghold-final.xml | 8/8定向GPU PlayMode通过：6项既有商店，2项新图。验证新图招募、两路拖动、满席合成、倍率仅本图、旧asset字节不变；实际模型传送时隐藏、暂停不恢复、出场后同模型恢复，无额外实体；切图与退出清理。 |
| build-stronghold.log | Windows1495965600 bytes、0错误，构建命令StrongholdMapTools.Build。构建后仅补了测试和文档，运行源码未再改。 |
| player-stronghold.log | layout/framed/purchases/drag/paused/infected/portal/combat/won/merge/reset/isolation/exit共13项全True，无捕获异常。11次部署，模拟30.85秒后2次突破胜利，7次传送、9次感染触发、166点总感染伤害。 |
| player-stronghold-city.log | 新构建用旧商店Smoke入口回归泵站：原19项、特效结束及3项表现共23项全True，无捕获异常。 |
| 画面 | 查看实际1920×1080准备、战斗；六张实机证据中准备/商店/18秒战斗/结算四张保存docs/ui-previews/stronghold04。原两图未被覆写。 |

证据根目录D:/游戏开发/artifacts/shop-map/。本轮未测第二台电脑、全新克隆构建、其余完整PlayMode/全角色地图组合、长期性能及人工平衡。原始工事在此为固定结构，不包含原作可移动/可破坏工事；地面路线为本作适配，并非原作飞行Boss轨迹。本机运行通过不记为接收端验收。项目生成.vscode仍未跟踪。

## 2026-10-31：用户录屏参考长平台、高位落地

查看完整42.17秒本地录屏及购买片段逐帧，参照连续厚平台、从右向左排列和高处快速落地；记录见[来源与观察](ui-previews/shop/REFERENCE-RECORDING.md)。模型正常配色，白色下落光痕、青白落地环/碎光，待机衔接；没有导入录屏中的原作部署图集或UI资产。

- play-recording-bench.xml：6/6定向地图GPU PlayMode通过，0失败/跳过。覆盖右向左布局、入场固定横坐标/下降/待机、快速合成与出售清理、暂停、商店开关拦截、满席直接二/三星、轻点不出售和向下拖动出售、部署及退出恢复。
- build-recording-bench.log：Windows1495946440 bytes，0错误。
- player-recording-bench.log：19项交易/布局/部署标志、特效结束及closingBlocksMap/purchaseArrivals/reserveCellEntry共23项全True，无捕获异常。最终可见1920×1080窗口保存94张实机图，已查看准备、上方下落、接地青白环及三星金光。
- docs/ui-previews/shop：8张截图、4段连续实机帧WebM同步更新。8fps为观察用慢放，不代表实际游戏帧率或精确动画时长。原录屏和参考帧仅位于本地，不提交。

证据根目录D:/游戏开发/artifacts/shop-map/。本轮没有重跑完整EditMode/其他PlayMode；旧278项完整记录为历史结果。第二台电脑、全新克隆构建、长时性能及人类手感验收仍未测。原地图asset、主场景、Prefab、Packages和ProjectSettings保持；Unity生成的.vscode保持未跟踪。

## 2026-10-31：相连整备格与原地显现落地

用户明确选定角色在整备格内显现、落地，再转入待机。play-reserve-entry-final.xml：6/6地图GPU PlayMode通过，0失败/跳过。验证实际入场模型横坐标始终与目标格一致、先高于地面且逐渐显现下降、0.72秒后转入正常待机；快速连买三张合成仅保留一个结果入场模型，入场中出售会立即清理；此前开关、部署、满席二/三星、底边出售及退出清理继续通过。

build-reserve-entry.log：Windows1495945652 bytes、0错误。player-reserve-entry.log：原19项、特效结束及closingBlocksMap/purchaseArrivals/reserveCellEntry共23项全部True，无捕获异常。已查看格内显现中、下降中与待机的实际1920×1080画面，备战为相连地板格，无角色从商店飞过地图。四段WebM与八张当前截图同步重录替换，8fps慢放不作为实际游戏帧率证明。

入场使用现有战斗帧配合透明度、短距垂直位移和落地变形，没有新增原作入场逐帧序列或外部媒体。没有重跑完整EditMode/其余PlayMode，未测第二台电脑、全新克隆、全分辨率或长期性能。入口与限制见[试玩指南](SHOP_MAP_TRIAL.md)。

## 2026-10-31：动态模型备战席与商店/购买/三星特效

play-model-bench-final.xml：5/5地图GPU PlayMode通过，0失败/跳过。覆盖暂停时开关进度和待机帧继续变化、快速反向开关不跳位、过渡拦截覆盖地图且不改钱/牌、购买落位不生成战斗实体、台座底边轻点不售/向下拖动可售、满席二/三星合成、落位后特效启动并1秒结束、退出清理实际模型。此后仅补模型拖动跟随、刷新图标在滑动裁剪区的绘制和预览录制，最终重建并实机复核；未重跑完整EditMode或其余PlayMode。

build-model-bench-delivery.log：Windows1495943851 bytes、0错误。player-model-bench-delivery.log：19项SHOP_MAP_CHECK、SHOP_BENCH_EFFECTS_EXPIRE、closingBlocksMap、purchaseArrivals共22项全部True，无捕获异常。已查看实际1920×1080静态/滑入中/飞行中/三星金光截图，刷新图标位置正常、棋子立于台座、无旋转星标或光弧。动态来源为本作现有战斗帧，真实SpriteRenderer对象由独立透明相机渲染，非卡面缩略图、非新增三维网格。

shop-toggle.webm（16帧）、purchase-and-three-star.webm（36帧）及银/金预告短片由实机帧8fps编码，属于慢放演示，不能当作实际游戏帧率或动画精确计时。录制先按真实时间取帧，再编码PNG，避免压缩耗时跳过短暂动画。旧shop-map-confirm.png仍是历史截图。

首轮Player发现新台座底边出售空间不足，已按剩余向下距离适配阈值并加入交互断言；中间录制轮采样过快、未覆盖到落位金光，不记为通过，已修正采样间隔后重录。滑动裁剪区原先旋转矩阵绘制刷新图标位置错误，已改成运行时生成的静态箭头纹理。最终以上22项与画面为交付证据。未测第二台电脑、全新克隆、全部分辨率或长期GPU性能；额外透明模型相机的性能仍需低配机实测。

## 2026-10-30：商店和备战升星全部取消旋转

删除ShopPromotionEffects共用绘制中的整段旋转光弧和开关参数；两个入口均只保留不旋转的亮起、轮廓和星标，备战仍1秒结束且星标全程静态。build-no-rank-rotation.log：Windows1495930340 bytes、0错误；player-no-rank-rotation.log原19项加特效结束共20项全部True，无捕获异常。商店二/三星各16张连续实机帧已重录，查看两档最终帧，确认没有绕行光弧；银/金截图与WebM均替换为本版，视频8fps仅展示动态，不作为游戏帧率证明。本次仅删除显示逻辑，未重跑EditMode/GPU PlayMode；未测第二台电脑或全新克隆。

## 2026-10-30：备战星标与升星瞬间取消旋转

备战星标调用始终关闭动画，头像升星反馈明确关闭旋转光弧；保留1秒亮起和扩散边框，商店预告保持。build-bench-static-stars.log：Windows1495931056 bytes、0错误；player-bench-static-stars.log：原19项及特效结束检查共20项全部True，无捕获异常。已查看三星合成瞬间实际截图，备战无旋转光弧，星标固定；shop-map-three-star.png更新为本版。此前结束后的二/三星静态截图与本轮完全一致。本次仅显示调整，未重复完整EditMode或GPU PlayMode；上一轮4/4暂停结束验证仍为历史结果。未测第二台电脑/全新克隆。

## 2026-10-30：备战席持续特效修复

play-bench-expiry.xml：4/4地图GPU PlayMode通过，0失败/跳过。在原满席二/三星场景增加断言：已有高星棋子静态，本次合成目标立即播放，Time.timeScale=0等待1.1秒后特效归零，星级不变；满席直接合成、无成功文字、地图与镜头恢复继续通过。build-bench-expiry.log：Windows1495931056 bytes、0错误。未重跑完整EditMode或其他PlayMode；278 EditMode为上一轮结果，模型本轮未改。

player-bench-expiry.log：原19项SHOP_MAP_CHECK全部True，新增SHOP_BENCH_EFFECTS_EXPIRE=True（二/三星即时启动、等待后结束且星级保留），共20项。实际1920×1080截图在player-bench-expiry；已查看三星静态备战画面，无头像光晕/光弧，无中央成功提示。二/三星结束后截图随docs/ui-previews/shop保存。未测第二台电脑或全新克隆构建。

## 2026-10-30：直接满席合成与银/金星标反馈

完整edit-rank.xml：278/278通过，0失败/跳过；其中商店模型7项，覆盖重复预告只读、2/3星预告等于实际合成、一次满席购买只扣一次、重复购买不扣款、无钱不能买、购买/出售后标记更新。play-rank.xml：4/4 GPU地图交互通过，满席立即合成且ShopFeedback为空，原地图和镜头不变，覆盖店开关/遮挡、编队部署、退出恢复继续通过。

随后仅删除旋转小星星，保留固定位置星标闪烁和银金光效。build-rank-final.log：Windows1495929844 bytes、0错误。player-rank-final.log：layout/framing/overlay/flatBench/fullReject/rankPreview/noNotice/fullTwo/fullThree/cancel/sale/level/lock/refresh/gateVisible/deployed/frozen/simulation/exit共19个布尔标志全True，无捕获异常。预告不实际买牌，满席合成一次点击完成，无确认/中央成功面板或成功文字；失败反馈仍保留。

已查看最终银/金预告、三星合成后截图。每档16张连续实机帧记录光弧和固定星标亮度变化；silver-rank.webm/gold-rank.webm由这些帧8fps编码，不把该视频帧率冒充游戏帧率。docs/ui-previews/shop/shop-rank-*.png与shop-map-three-star.png为最终实机；旧shop-map-confirm.png是历史截图，不代表当前UI。证据根D:/游戏开发/artifacts/shop-map。未重跑其余完整PlayMode、Node/浏览器；未测第二台电脑或全新克隆构建。

## 2026-10-30：覆盖式商店与扁平备战席

[最新操作与范围](SHOP_MAP_TRIAL.md)。play-overlay.xml：4/4定向GPU PlayMode通过，覆盖默认收起、开关保留钱/库存/镜头、覆盖区不穿透、关闭待确认购买不扣款、拖动自动收起、扁平比例、全部路线/门、满席2/3星、实际部署、购买不叠加缩放及退出恢复。完整276/74未在本轮重跑。

最后仅将出售向下距离22→12虚拟像素适配46像素卡槽，并把独立Player出售流程改为从卡槽底边起拖。build-overlay-final.log：Windows1495927470 bytes、0错误；player-overlay-final.log：layout/framing/overlay/flatBench/fullReject/fullConfirm/fullTwo/fullThree/cancel/sale/level/lock/refresh/gateVisible/deployed/frozen/simulation/exit全True，无捕获异常。18项为布尔标志，magnification是FOV诊断值，不作新屏幕比例指标。

1920×1080实际收起/展开截图已查看：地图高807像素，备战带132像素、单卡69像素，商店上层盖住地图而不改变取景。最终截图保存docs/ui-previews/shop/shop-map-*.png。运行输入与自动验证共用事务和指针函数；没有声称完成第二台电脑或真实人类平衡验收。未改原场景/地图asset、Prefab、包或设置；本地.vscode保持未提交。

## 2026-10-30：商店地图与满备战席合成

同日镜头补充：中间地图放大1.4倍并按有效路线/守军居中，商店、备战席、1920×1080和地图格局保持。3/3地图交互复测通过，验证所有路线/红门可见、购买不累积缩放、退出恢复视口和FOV；最新Windows1495925555 bytes、0错误，Player16项布尔标志全True、magnification=1.40，实际截图已复核。证据：play-zoom.xml、build-zoom.log、player-zoom.log；下文首版完整276/74结果不记为本次重跑。

[范围、构建和限制](SHOP_MAP_TRIAL.md)。本机证据目录 `D:/游戏开发/artifacts/shop-map/`，测试与构建输出不入Git。

| 实际验证 | 结果 |
| --- | --- |
| 完整EditMode / GPU PlayMode | 276/276、74/74通过，0失败/跳过；edit-all.xml、play-all.xml。新增5项事务/3项场景：满席确认两星/连锁三星、失败不扣钱、报价返库、经验溢出、锁定、原地图不变、弩手与新购买猎狗使用自身配置部署、战斗冻结交易。 |
| 最终Windows / 独立Player | build-delivery.log：1495921684 bytes、0错误。player-delivery.log：layout/fullReject/fullConfirm/fullTwo/fullThree/cancel/sale/level/lock/refresh/gateVisible/deployed/frozen/simulation/exit全部True，无捕获异常；实际可见窗口截图在player-delivery。 |
| 浏览器 | Node 7/7，Chrome1672×941离线验证商店在上/备战在下、满席2/3星、取消不扣钱、中央大提示和底部出售，无页面错误。 |
| 画面复核 | 查看最终Player准备/三星截图，修正技能长文字重叠、星形字形裁切与按钮对比度。完整套件在显示修正前通过；保留原设计和1920×1080布局后又通过3/3地图交互（play-layout-final.xml），最终重新构建并重跑独立Player。docs/ui-previews/shop/shop-map-*.png为最终实机。 |

首轮受限环境Unity许可证无法识别，正常Windows环境重试构建成功；首次隐藏窗口截图为黑屏，不作为画面通过证据，后续以可见窗口重新运行并复核。未升级Unity/包或改许可证。未测第二台电脑、全新克隆构建、长时压力、正式数值平衡；10角色基础池和未实现星级战斗倍率不冒称完整自走棋系统。

## 2026-10-29：第六批三名六星守军

D26/D27/D28的独立所选S3与原生模型接入。具体适配/未做项见[第六批说明](DEFENDER-SKILLS-BATCH6.md)。从实时main34ec756创建新分支，包含未合并#45依赖，不自动写main。

| 实际验证 | 结果 |
| --- | --- |
| 最终完整EditMode | 271/271通过、0跳过，artifacts/b6-compatible-EditMode.xml。新增8项覆盖费用交换/真实备援与玩家DP隔离、实际直线命中/脆弱及暂停、诱导路线进度/雪层上限、棋子实际伤害/弹药/重置、路线转角投影、配置/原生S3、连续攻击时间边界、忽略法抗不能额外制造负法抗。 |
| 最终完整GPU PlayMode | 71/71通过、0跳过，artifacts/b6-compatible-PlayMode.xml。新场景验证三人原模型、直立比例/屏幕平行、准备待机、暂停拖动/SP/帧冻结、实际技能效果、重开和返回，包含旧19人、怪物能力与双地图回归。 |
| 最终Windows | BaselineTools.BuildWindows通过，1495880061 bytes、0错误，artifacts/b6-compatible-build.log。数值为Unity构建报告统计，不是EXE单文件大小。 |
| 最终独立Player | D26/D27/D28/D25/D22/city/ruins七例全部退出0、无捕获异常，artifacts/b6-compat-player-results.json；新增三人entered/models/preparation/deployed/paused/effect/assault/cast/won/reset/restored全部True。伤害边界另由逻辑测试验证；截图不是完整数值证明。 |
| 资源与画面 | 99张PNG共196469931字节，逐页SHA与导入记录匹配；6份运行清单、必要meta、原生动作、来源记录齐全。已查看选择页与三技能实机截图，docs/art/defender-batch6-*.png。资源/源码跟踪检查见本轮提交。 |

初次校验发现初雪无普攻应标记noOrdinaryAttack，已修正；第一次逻辑测试仅路线投影浮点精确比较失败，改用微小误差。场景复现发现银灰满SP被连续普攻的浮点时间边界饿死，统一技能与攻击的时间误差后通过；提高样板生命不能解决该根因，不将其当修复。修正已触发棋子残留连线与清理、初雪法抗忽略下限，并核对银灰7级S3参数后，之后最终复核发现忽略法抗的下限不能抹去旧灼燃负法抗；修正并在余的真实火墙流程中加入伤害断言后，上述最终完整两套与7例Player全部重新运行。失败记录未登记为通过。

未改主场景/Prefab/Packages/ProjectSettings。原工程既存PackageManagerSettings.asset保持原SHA256：312F6AF74DD911D570E7F4B6660F4D0D37682F2146564CEE297229B558A230D2；不提交、不覆盖。EXE与源码提交见STATUS和本地BUILD-INFO；无ZIP/Releases。

未测第二台电脑离线、全新克隆构建、长时压力、全怪物×全部守军技能穷举。守军技能为自动调度；风雪之眼未做自由部署/转移及19费交易，望未做玩家选格/主动结束和全部盘面限制，雪格/棋子/外部特效仍为程序近似；正式数值和美术效果待人类试玩。


## 2026-10-29：六星守军第五批

D23/D24/D25独立所选S3接入，完整范围与明确缺口见[第五批说明](DEFENDER-SKILLS-BATCH5.md)。基线main34ec756，codex/defender-skills-batch5包含第四批#44依赖，等待人工合并，不自动写main。

| 实际验证 | 结果 |
| --- | --- |
| 完整EditMode | 263/263通过、零跳过：artifacts/b5-final-EditMode.xml。新8项覆盖五发逐发结算/扣弹、致命返回与暂停、真实伤害与重开、负生命有限/被击倒、法术连击/11秒退场、模型配置、实际备援部署及6SP、实际致命伤转移。 |
| 完整GPU PlayMode | 70/70通过、零跳过：artifacts/b5-final-PlayMode.xml。包含旧16人、原怪物能力和双地图，以及本轮三人原模型、屏幕平行/scale1、准备待机、暂停部署/SP/帧冻结、三技能状态、重开和返回。 |
| 后续定向复核 | 最后将Mon3tr返回的1秒不死与技能强化分开后，8/8逻辑、1/1三人场景通过，artifacts/b5-release-EditMode.xml / b5-release-PlayMode.xml。随后仅修整保护阶段HUD和Mon3tr原返回帧，没有再改战斗逻辑；未把完整两套记录为在这些表现调整后重跑。 |
| Windows | 最终BaselineTools.BuildWindows通过：见artifacts/b5-build-transfer-final.log统计（最终构建）、0错误，artifacts/b5-build-transfer-final.log。此为Unity构建报告统计，不等于EXE单文件大小。 |
| 独立Player | 最终三人D23/D24/D25全部退出0，artifacts/b5-verified-player-results.json；entered/models/preparation/deployed/paused/effect/assault/cast/won/reset/restored全True。此前同战斗逻辑还跑了D21/city/ruins，均退出0，artifacts/b5-player-results.json；最后HUD/返回帧调整后未重跑这三例。技能状态观察计数不是所有命中类型的穷举，关键伤害/保护边界由前述逻辑测试另行验证。 |
| 美术/文件 | 131PNG原字节SHA、6份清单完整索引已核对，原生动作与脚底规范保留；选择页、三人实际技能截图已查看，docs/art/defender-batch5-*.png。最后补充临死保护剩余时间与原返回动作。源资产、meta、来源与权利记录随源码交付。 |

首轮编译发现菜单循环文本替换错误，已局部修正；一次原生图集导入中返回/收手重名导致布局不符，已消除重复键并核对每页哈希。定向GPU还发现临死阶段误回普通攻击帧，修正后完整70项通过；失败日志没有登记为成功验证。其他模块只在必要调用点接入新的扣血入口，原场景/Prefab/包/设置没有更改。

本地试玩路径见STATUS，目录BUILD-INFO保存实际源码提交与EXE哈希。原工程既存PackageManagerSettings.asset未提交或覆盖，SHA256仍为312F6AF74DD911D570E7F4B6660F4D0D37682F2146564CEE297229B558A230D2。仓库完整性、暂存差异和媒体跟踪检查通过；不上传ZIP/Releases。

最终扣血核对又修正了保护者应先付出自身生命、剩余才计入负生命；同时禁止临死保护期再次开启技能。263项完整EditMode重新全部通过（artifacts/b5-transfer-final.xml），增强断言验证队友1血与保护者实际掉血。随后重建最终Windows并复核三个Player；完整70项GPU没有在此最后两处逻辑修正后重跑，不将此前结果改称本轮重跑。

未执行第二台电脑离线、全新克隆构建、长时性能、全怪物×三技能穷举或人类最终美术/平衡验收。重构体为路标、投递为固定候选点及单名未登场备援，不包含完整召唤物销毁/部署列表/再部署系统，也未包含三人其余技能/全部天赋/模组或原作完整外部特效；这些限制不能登记为完成。

## 2026-10-28：六星守军第四批

当前任务只交付D21/D22/D29/D30所选S3的独立可玩样板；第二/第三批跳过，原12人与双地图保留。基线main34ec756，功能分支codex/defender-skills-batch4；未自动合并main。机制、文件和缺口见[第四批说明](DEFENDER-SKILLS-BATCH4.md)。

| 实际执行 | 结果与本地证据 |
| --- | --- |
| 导入/配置 | DefenderBatch4Tools.Apply成功，DEFENDER_BATCH4_CONFIG_PASS，artifacts/b4-import.log。8份正背面清单和154PNG可加载，基础角色无缺页；玛恩纳明确没有普通攻击，塞雷娅背面缺失S3的回退记录保留。 |
| 最终EditMode | 255/255通过、零跳过，artifacts/b4-verified-EditMode.xml。新增15项验证蓄力/不攻不挡/暂停、5目标不重复/技能结束重蓄/击杀扣特性、全场追猎/恐惧真实后退/重选/死亡清理/范围不叠加、群疗回SP/法术易伤与真实减速/结束、余生命变更与4人生效恢复、跨墙消弹/灼燃爆发与法抗、自己的法术及其他物理不触发/近战不消、固定路线退行与输入边界、四套模型配置。 |
| 最终完整GPU PlayMode | 69/69通过、零跳过，artifacts/b4-verified-PlayMode.xml。含原怪物能力、队友机制、原12人/混合防线/双地图和本轮4人逐个入口；验证原动作、屏幕平行比例/scale1、准备idle、暂停拖放/SP与动作冻结、技能实际效果、重开与返回。 |
| 最终Windows构建 | BaselineTools.BuildWindows成功，1163869074 bytes、0错误，artifacts/b4-build-verified.log。构建统计为Unity报告，不等同EXE单文件大小。 |
| 最终独立Player | artifacts/b4-verified-player-results.json及b4-verified-player-*.log：D21/D22/D29/D30/D07/D03/D06/city/ruins共9例均退出0、无记录到的Exception。新4人entered/models/preparation/deployed/paused/effect/assault/cast/won/reset/restored全True；旧三人和双地图实际技能/部署/胜负/重开/退出也通过。 |
| 画面核对 | 检查真实Player的选择页及四人技能截图；调整搭档避免玛恩纳同点重叠、法术搭档被上方HUD遮挡，再以最终配置回归并截图。见docs/art/defender-batch4-*.png。不是完整逐帧艺术验收。 |

首次EditMode有测试调用漏传伤害类型，编译修正后两处多实体测试仍因低速单位未离开红门而失败；按实际部署规则改测试后通过，未为测试放宽部署限制。后续补齐范围法术跨墙、扩大范围的嘲讽选择和技能治疗计数，最后重新完整跑255 EditMode/69 GPU及构建；以上最终结果不使用失败运行充数。

本机构建交付路径见STATUS和目录BUILD-INFO.txt。必要运行PNG/清单/meta全部随源码提交，逐PNG SHA已对照导入记录；保留原媒体权利未确认状态。仓库完整性/差异检查通过，主场景、Prefab、包与设置无改动；原工程既存PackageManagerSettings.asset SHA256仍为312F6AF74DD911D570E7F4B6660F4D0D37682F2146564CEE297229B558A230D2。

未执行第二台电脑离线、多人新克隆完整构建、长时性能、全部怪物×四技能穷举、人类最终美术/手感/数值验收。主动技能自动AI、圆形范围、固定路线恐惧、灼燃量级、未实现天赋/模组及原作完整外部特效的限制见本批说明；不声称本轮实现了四人全部原作系统。

## 2026-10-27：#43 保留两批记录并同步主分支

按人类要求整理STATUS/QA冲突：#42先同步main8dbdd5a，再合入#43。两份文档的原八人章节、新四人章节及既往验证全文均保留，补齐当前状态与压缩合并说明；没有把前批4人的结果与后批8人结果混为一次运行。最终相对7aa26ed仅STATUS/QA新增说明，所有运行文件、配置、素材和meta一致；main8dbdd5a已成为本分支祖先。

本轮实际执行仓库结构/Unity元数据检查、两份文档的本地链接与冲突标记检查、暂存差异和运行文件等同性核对。Unity编译、EditMode、PlayMode和Windows构建均未重跑；现有EXE仍对应7aa26ed的已测运行内容，不新打包、不更新ZIP/Releases。PR内容冲突整理后正常推送，仍由人类审查合并。

## 2026-10-27：原8名守军所选技能补齐

本轮按人类已确认的范围，在codex/defender-original-eight-skills上完成8人技能，并应用到原八人防线及city/ruins；分支起于同步main172dc06并吸收PR #40/#42，未合并main。详细机制、原文链接、配置、动作与未做项见[本轮说明](DEFENDER-ORIGINAL-EIGHT-SKILLS.md)。

| 实际验证 | 结果及证据 |
| --- | --- |
| 配置/幂等 | OriginalEightSkillTools.Apply/Validate通过；复跑后既有配置差异完全一致，artifacts/eight-skills-idempotence.log。8个新独立配置、三份混合防线均有对应技能，旧4个独立配置不变。 |
| 最终EditMode | 240/240通过、零跳过；artifacts/eight-skills-final-edit.xml。新增12项覆盖防御/破甲与过期、治疗切换、伤害倍率、攻击回技力与逐箭扣甲、来源死亡取消后箭、高甲/嘲讽优先、医疗不攻击、停顿实际距离/暂停/过期、群攻与攻速、三份混合配置。 |
| 完整GPU PlayMode | 最后68/68通过、零跳过，artifacts/eight-skills-final-play.xml；包含原怪物/队友特殊能力、旧4人、原防线/双地图，以及新8人逐个入口的原模型、屏幕平行比例、战前idle、暂停拖动/SP/动作冻结、真实技能效果、重开与返回。 |
| 最终Windows构建 | BaselineTools.BuildWindows通过，995002061 bytes、0错误；artifacts/eight-skills-release-build.log。最后修改为单人HUD说明避让及素材SHA索引，未改PNG或已验证战斗逻辑。 |
| 原8人独立Player | D01/D03/D04/D07/D08/D09/D10/D12均实际退出0，artifacts/eight-player-Dxx/player.log。ORIGINAL_EIGHT_SKILL的entered/models/preparation/deployed/paused/effect/assault/cast/won/reset/restored全部True。以猎狗观察技能后用塔露拉完成实际胜利。 |
| 最终产物复核 | 最终EXE再跑D07、D03、旧D06与city/ruins，全部退出0，artifacts/eight-release-*/player.log。两图四卡独立DP/次数/CD、全部红门比例、准备待机、暂停、正背面/左右、无误继承技能、真实破甲、0.03×点选施放、胜利/重开/返回全部True。D06玩家猎狗不要求主动技能。 |
| 资源/仓库 | 新5PNG共4712263 bytes，原SHA保留，必要meta和完整运行清单跟踪；补齐7个旧已跟踪图集页的manifest.files哈希索引，无PNG变更。Git忽略/实际跟踪、GUID/引用与保护路径检查通过，check_repository仅是结构检查，不替代Unity运行证据。 |

初次新测试有2处测试编译错误（约束名和构造参数）及5处样板设置/时间窗口错误；按现有入口占用/CD、合法门间距和投射物延迟修正测试，未放宽运行规则。最终240 Edit和68 GPU全部通过。实际检查米格鲁技能起手、斑点治疗、克洛丝连射及双地图画面，截图见本轮说明；没有把单张截图当成完整逐帧美术验收。

交付本地EXE：`E:/珍贵资料/昨日圆车_本地试玩/2026-10-27-原八名守军完整技能/YesterdayConvoy.exe`。对应源码、复制校验与操作见同目录BUILD-INFO.txt；复制后重新启动检查结果保留在artifacts/eight-delivered-D07。全目录文件复制保持SHA256一致，保留字体/音频/美术权利记录。不创建ZIP/Releases，不自动合并main。

原E:/珍贵资料/昨日圆车仍保持main及既存未跟踪设置，PackageManagerSettings.asset SHA256仍为312F6AF74DD911D570E7F4B6660F4D0D37682F2146564CEE297229B558A230D2。本轮主场景文件、Prefab、Packages和ProjectSettings均未改。

未运行第二台电脑离线、长时性能、正式平衡、所有正背面动作逐帧人工验收；没有复刻原作逐格范围/全部培养天赋/整套外置技能特效。剩余18名守军仍待后续。

## 2026-10-27：#42 压缩合并后的冲突整理

人类要求整理冲突。已同步main8dbdd5a，证实其文件树5c451604与#40最终9314705完全一致；在#42分支逐文件按此共同内容三方合并，再加入main父历史。12个冲突文件包括共享脚本及文档；解决后完整暂存树与e3e64d4原已测树相同，未通过整文件选择某一方丢弃改动。随后只给STATUS/QA新增本轮说明，原设计、来源和所有测试记录保留。

实际执行：Git冲突索引/标记清理、文件树等同检查、文档内容及本地链接检查、git diff --check、tools/check_repository.py。Unity编译、EditMode、PlayMode及Windows构建本轮均未运行；代码、素材和配置与既有#42已测版本完全相同，不需要重打试玩包。没有修改主场景/Prefab/Packages/ProjectSettings或原工程既存未跟踪设置，不自动合并main。

## 2026-10-27：守军技能第一批

本轮从已同步main172dc06创建codex/defender-skills-batch1，并以PR #40现有提交为依赖。角色/模型/技能范围经人类选定为“22名连同完整技能分批制作”，本轮为其中4名的可玩闭环。相关代码、数据流、来源、数值及机制适配见[本批说明](DEFENDER-SKILLS-BATCH1.md)。

| 实际验证 | 结果及阶段 |
| --- | --- |
| 配置/素材 | DefenderSkillTrialTools.CreateBatch1与ValidateBatch1通过；4组独立配置/8份正背面包，53个运行文件/45PNG。保留原PNG，尺度/脚底元数据规范化。 |
| EditMode | 228/228、零跳过，artifacts/defender-batch1-release-EditMode.xml。新增12项覆盖实际恢复40%/不复活、物理→法术→物理、伤害倍率/远程惩罚/暴击、攻速与前摇/暂停、扩范围实际治疗及附加目标去重、冻结不能施放、无普攻与非法配置，以及旧寒冷只改变间隔、不误改原前摇。 |
| 完整GPU PlayMode | 最终67/67全部通过、零跳过，artifacts/defender-batch1-release-PlayMode.xml。首次新四人场景因远程对手未进入翎羽射程而失败，改为近身猎狗；最终完整回归包含旧8人、地图、怪物/队友能力与新四人。 |
| 最终定向GPU | 1/1通过、零跳过，artifacts/defender-batch1-scene-final.xml；同一项依次覆盖4角色，翎羽使用专用低伤高生命猎狗检查近战；准备idle/动作比例/合法拖动/暂停SP冻结/各技能真实效果/重开返回均通过。随后增加旧行为兼容性保护，最终再次运行全部67项通过。 |
| 最终Windows | BaselineTools.BuildWindows，990260719 bytes、0错误；artifacts/defender-batch1-final-build.log。 |
| 独立Player | D02/D05/D06/D11全部退出0；artifacts/defender-batch1-player-Dxx/player.log，entered/models/preparation/deployed/paused/skill/effect/playerSkillOrNotRequired/won/reset/restored全部True。D06玩家单位无技能，playerSkillOrNotRequired表示该检查不适用。 |
| 最后文字/菜单 | 只调整选择页底部操作说明及证据截图后重新构建，D02再启动通过；artifacts/defender-batch1-release-D02/player.log。运行逻辑未变。 |

初次Edit编译因测试把attackTargets放错构造参数失败，改为既有MonsterEffectSettings后通过，最终新增兼容性用例共228项通过；没有修改游戏接口迁就测试。近战角色的远程对手失败缩小为实际射程关系后修正样板，不放宽技能索敌。所有失败中间结果保留在本地日志，不计为最终通过。

本轮没有修改主场景、Prefab、Packages或ProjectSettings；没有创建Spine运行库或游戏新包。素材及meta随源码跟踪；工作区/缓存/日志/构建/AGENTS不提交。未做第二台电脑离线、长时性能/正式平衡、全部动作逐帧人工验收或剩余18人技能；不把四个技能样板等同于全部原作培养/经济系统。

## 2026-10-27 后续源码上传

用户在本地验收交付后明确要求上传GitHub，授权将`4e2c79f`运行修正及其着色器/meta、测试和实机截图推送原功能分支、更新PR #40。此前“仅本地”说明为历史执行边界。上传前工作区干净，已fetch核对远端PR仍开放、分支可正常快进；运行依赖均已跟踪。此次只更新交付说明并提交，没有修改运行代码，沿用下方真实Unity/Player证据，不重复声称运行了测试。无main合并、EXE上传、ZIP或Release操作。

## 2026-10-27：本地人物比例与战前待机修正

**范围。** 用户明确要求不上传GitHub，本轮仅本地开发、Git提交与Windows构建。修改DemoSpriteVisual、DefenderRosterView、ManualBattleController/TalulahTrial的准备相位及TacticalRosterEvidence和对应PlayMode测试；新增原创TacticalActor.shader及Unity生成的meta，无新外部美术素材。主场景、Prefab、Packages、ProjectSettings不变。

**原因与连接。** 先前世界竖直角色面以Y×2补偿60°镜头，偏离画面中心时产生透视歪斜。现在使用屏幕平行、等比例Sprite；新着色器从脚底竖直平面重建遮挡深度，避免单纯billboard被高台切断身体，也没有关闭全部深度遮挡。塔露拉进入原有目标/移动左右镜像流程，按原UnitInterval锁定普攻朝向，施法保持方向，逻辑根节点不旋转。准备阶段的readyIdleElapsed只供守军idle使用，开战后叠加Battle.Clock；攻击、受击、伤害和SP仍使用原模拟时间。

| 实际验证 | 结果 |
| --- | --- |
| 初步定向GPU PlayMode | 5/5通过。新增3项检查：准备阶段8人idle有帧变化且不推进战斗，开始不跳帧/暂停冻结；两图全部七门和八人位置的屏幕轴正交/等像素比例；塔露拉在左右转折改变朝向，纵向保留朝向且逻辑根不旋转。 |
| 完整GPU PlayMode | 66/66通过、0跳过，`artifacts/proportions-full-play.xml`；此轮在最终深度着色器接入前完成。 |
| 最终显示定向GPU | 5/5通过、0跳过，`artifacts/proportions-release-play.xml`；包含角色材质正确及Shader支持检查，日志无Shader error。 |
| 最终Windows | Unity6000.6.5f1，Windows x64；949,183,473 bytes、0错误；`artifacts/proportions-final-build.log`无Shader编译错误。 |
| 泵站最终Player | 退出0，`artifacts/proportions-final-city/player.log`；entered/cards/drag/independent/paused/facing/preparation/noSkill/attacked/acid/slow/cast/won/reset/restored全部True。 |
| 环庭最终Player | 退出0，同上；`artifacts/proportions-final-ruins/player.log`。 |

**图像检查。** 实际查看最终EXE的泵站下方/上方出生与混编截图、环庭上方出生截图；人物轴向与比例正常，原先腰部被高台切断的条带已消除。两图全部七个出生点原图保存在上述artifacts目录。提交中的实机示例：[泵站上方红门](art/tactical-actors-proportions-city.png) / [环庭上方红门](art/tactical-actors-proportions-ruins.png)。这些是运行画面，不是概念稿。

**修复中发现。** 隐藏窗口启动时截图接口返回空，改为可见窗口后完成最终检查；纯billboard的中间稿出现高台切割，已由脚底深度解决。着色器首次条件表达式编译错误已修正，最终构建和运行日志没有该错误。失败/中间稿不作为最终可玩证据。

**限制。** 塔露拉为左右镜像转向，没有新绘完整转身/背面动作；守军继续使用既有正背面资源，死亡仍沿用前面。没有重画身体比例、添加新角色、修改路线/数值/技能或声称商业动画质量。本轮未重跑EditMode，未做第二台电脑离线、非Windows平台、长时性能或完整人工平衡；最终深度修正只重跑受影响5项和两图独立Player，不将先前66项冒称为最终着色器完整回归。

## 2026-10-27：V3高台迷宫与混合小怪

**确认与范围。** 人类确认项目外V3实际几何图/动作视频与README的接口范围后，在PR #40原分支接入。保留原4红2蓝/3红2蓝及七条固定路线；泵站高台110/198格，环庭116/198格。只调整非通道地形、已批准的显示/卡栏/部署与必要来源配置；主场景文件、Prefab、Packages和ProjectSettings未修改。

| 实际检查 | 结果与范围 |
| --- | --- |
| Unity配置/资源 | `TacticalMapV3Tools.Apply`两图通过；4卡、8人背面图集及前面兼容加载成功。后续`TacticalMapV2Tools.Validate`仍通过。 |
| EditMode | 216/216通过、零跳过；新增每卡费用/CD/次数、暂停与重开、重复请求卡身份、1DP可用低费卡、小怪无技能、真实来源破甲等边界。 |
| 完整GPU PlayMode | 63/63通过、零跳过；保留原怪物/设施/守军回归；新增两图四卡混编与攻击期间目标死亡后朝向保持。 |
| 最后定向GPU | 2/2通过；四张卡走MouseDown/MouseDrag/MouseUp实际事件分发，暂停与独立状态正确，点击无技能单位关闭旧技能面板，左右基准与完整攻击朝向锁通过。 |
| 最终Windows构建 | Unity6000.6.5f1 / Windows x64，`BaselineTools.BuildWindows`，949,175,014 bytes，0错误。 |
| 泵站独立Player | 退出0；entered/cards/drag/independent/paused/facing/noSkill/attacked/acid/slow/cast/won/reset/restored全部True。 |
| 环庭独立Player | 退出0；同上全部True。3红门时先恢复一秒让出生点让开，再暂停放第4卡，不绕过入口占用限制。 |
| 原维维美交互Player | 退出0，暂停部署、一次扣费/冻结、非法取消、就绪、0.03×聚焦、取消、释放、重开8项全部True。 |
| 仓库与媒体 | 3045个跟踪文件完整性检查通过；8份新增背面包PNG/JSON哈希、meta与忽略规则核对通过，未混入AGENTS、构建或工程设置。 |

最终Player用1920×1080正常可见窗口运行 `-tacticalRosterSmokeTest city` / `ruins`。实际4卡产生各自原造型与动作，画面同场8个实体；酸液命中真实降低守军护甲，只有塔露拉有SP/火雨。用点击角色/技能图标的路径检查0.03×聚焦、施放与恢复；无技能小怪不能打开技能面板或增长SP。自动化Player是确定性检查，不替代长期人工试玩。

**实机画面。** [泵站混编](art/tactical-v3-runtime-city.png) / [环庭混编](art/tactical-v3-runtime-ruins.png)，来自最终EXE；与项目外编排预演区分。实机发现并处理了卡栏挡住重开按钮、血条压头、左右翻转基准相反的问题。原背面玫兰莎1秒攻击按本作2秒动作窗口映射，不额外结算伤害。背面包无死亡，统一沿用既有正面死亡，未虚构素材。

**连接与文件。** `DefenderTrialDefinition.rosterCards`和两份MapTrials资产提供只读卡/高台配置，`BattleCardState`保存每卡运行次数与下一次可部署时间，`ManualBattle`完成原子事务及共用DP；旧单卡/游击队入口继续兼容。`UnitCombat`把实体自己的参数送到技能、伤害与动画；`TacticalLoadout`和输入适配器绘制/拖动四卡，`DefenderRosterView`按已发射攻击目标锁朝向，`DefenderArt`加载正背面包；`DemoSpriteVisual`仅在本试验竖直显示并镜像小怪，不旋转逻辑根节点。具体文件/资源与来源见[V3说明](art/TACTICAL-MAPS-V3.md)。

**交付与未测。** 功能分支提交/推送/PR #40待人工审查，不合并main；本地EXE位于 `E:/珍贵资料/昨日圆车_本地试玩/2026-10-27-迷宫高台V3/YesterdayConvoy.exe`，对应源码见BUILD-INFO.txt。新增8份背面运行媒体及meta默认随源码提交，无ZIP/Releases。没有做第二台电脑离线、长时间手动操作、每位每帧人工美术验收或正式挑战平衡；当前每卡试验值见V3说明。尚未接入其余22名/六星、完整8人技能、商店或正式编队。


## 2026-10-27：多门 V2 双地图接入

**范围与版本。** 从 main `172dc06` 创建 `codex/tactical-maps-v2`；人类已确认4红2蓝泵站、3红2蓝环庭以及共享接口范围。本轮只修改配置/运行适配/地图显示/菜单/对应测试，并迁移旧能力回归；未改主场景文件、Prefab、Packages、ProjectSettings。原工程既存设置文件SHA256保持 `312F6AF74DD911D570E7F4B6660F4D0D37682F2146564CEE297229B558A230D2`。没有自动合并main、ZIP或Releases。

**实际运行。** Unity `6000.6.5f1 (3ff58d469c8a)`，Windows x64 / RTX4060 / 1920×1080：

| 检查 | 真实结果与范围 |
| --- | --- |
| 配置生成/导入 | `TacticalMapV2Tools.Prepare`及Validate通过；2地图/7固定路线/每图2蓝门/8独立守军/3张运行PNG绑定。幂等创建地图资产，既有资产保留。 |
| 审批布局核对 | 7条路线的每个序列化坐标、红门和蓝门身份与审批JSON完全一致；三张PNG的SHA/meta/忽略规则通过。 |
| EditMode | 213/213，通过、零跳过；含多入口路径、共用防御幂等、暂停扣费/冻结/共享CD、忙入口隔离、重复事务与配置防歧义。 |
| 完整GPU PlayMode | 61/61，通过、零跳过；原怪物与队友能力、8守军交战/医疗及新两图均覆盖。 |
| 视觉整理后定向GPU | 2/2，通过；7门实际屏幕拖放、反复返回/重进/卸载清理、双图技能交战与胜利重置。 |
| 最终Windows构建 | `BaselineTools.BuildWindows`成功，921,699,503 bytes，0 errors，仍只有InitialDemo构建场景。 |
| 泵站独立Player | 退出0；entered/gates/drag/paused/invalid/attacked/skill/won/reset/restored全部True。 |
| 环庭独立Player | 退出0；同上全部True。 |
| 原维维美交互Player | 退出0；暂停拖放、一次扣费与冻结、非法取消、技力、慢速点选、取消/释放、重开8项全部True。 |
| 仓库完整性 | 2959个跟踪文件检查通过；三张新PNG及meta正常跟踪，无孤立meta、冲突标记或本地构建/AGENTS误提交。 |

独立Player参数 `-tacticalMapSmokeTest city` / `ruins`，可选 `-demoEvidenceDir`；诊断截图需要正常可见图形窗口。隐藏窗口的截图接口曾返回null，不作为画面证据，已改为明确报错退出；实际交付使用可见窗口重新完整运行并检查截图。原网页看稿浏览器测试未执行（本地HTTP启动被自动审批拒绝），不算Unity或Player证据。

**实机画面。** [泵站](art/tactical-v2-runtime-city.png) / [环庭](art/tactical-v2-runtime-ruins.png)。截图是最终EXE的实际输出，非概念图。实机复核发现并修正：基础块顶面与地面片共面造成条纹；平台设备遮挡角色；运行时Standard透明变体在Player中缺失造成树冠方形底。最终使用分离的顶面高度、边缘设备、现有Sprite透明材质。新两图隐藏守军名字、使用紧凑血条，拖动到门时高亮该门路线，无战斗中切场景按钮。

**文件与连接。** `DeploymentLane`保存不可变路线快照，`ManualBattle`选择红门后绑定单位的RouteId/ExitId，旧单路线调用保持兼容；仍由原战斗模型处理费用、暂停、攻击/技能和蓝门共享防御。`DefenderTrialDefinition`保存地图/守军静态数据，`TacticalMapEnvironment`只生成视觉，`DefenseTrialMap`负责借用与恢复相机/原地图/门显隐，主选择页与控制器复用原拖动卡牌。旧设施/游击队/爱国者能力转到独立中性测试底板，无旧地图切换UI。

**交付与未测。** 新版在功能分支、待人工合并；本地试玩 `E:/珍贵资料/昨日圆车_本地试玩/2026-10-27-双地图V2/YesterdayConvoy.exe`，对应源码提交见同目录BUILD-INFO.txt。地图仍为塔露拉对原八人防线的样板，DP24/费2/12次/防御1是试验值；正式编队/商店/全角色地图平衡未实现。未做第二台电脑离线验证、长期手动游玩和最终美术验收，不宣称达到原作完整美术精细度。


## 2026-10-27：PR #14 / #16 / #17 与周末 main 的兼容整合

基线：实时fetch的 `origin/main cbe321b543f54fa20f8efc694639ea276101c9b5`，文件树与已合并#27最终 `7ec2860` 完全相同。继续干净的 `codex/defender-mixed-map` / PR #17，依次merge main、原#16和#14，保留全部父提交及双方意图；没有rebase、强推、整文件ours/theirs、main提交或自动合并。原工程main及未跟踪的PackageManagerSettings保持不动。初版运行源码 `e0ce1c39fd64958f90e0acb82f9c4f88008dc00b`；最终运行源码 `16315a863bcfa86e39ed769132854c12e934b306` 另修正下述场景卸载及诊断文字问题；后续仅文档。

- main的6个冲突文件：EngagementCombat、EngagementView、ManualBattleController、MonsterEffects、QA、ASSET_MANIFEST；随后合入#16/#14的同位置文档插入均并存。独立守军生命/护甲/范围/医疗/群攻保留，同时复用统一TryDamageUnit处理法抗、屏障、复活及死亡；枷锁/神经眩晕、冻结/冰环/凝视减速、嘲讽、待结算爆炸均保留。旧守军转换携带原伤害类型；CombatShot保留main的coldDuration位置及两分支新增字段。留声机改用守军自身生命上限，避免医疗错误封顶。
- Unity **6000.6.5f1：209/209 EditMode，58/58 GPU PlayMode，零失败、零跳过**。新增4个交叉逻辑用例：独立法术守军实际打掉屏障而非直接扣HP、致命攻击触发霜星复活、枷锁使独立守军不阻挡/不起攻击、不同最大生命守军的留声机治疗不超量。完整Play包含8人切换/暂停拖放/治疗/火雨/返回，以及原怪物、设施与首领场景。证据：artifacts/integration-edit.xml、integration-play.xml及同名log。
- 最终16315a8 Windows Development Build **900,715,441字节、0错误**；40份单位及场景校验通过，日志artifacts/integration-final-build.log。主场景、已有Prefab、Packages、ProjectSettings与main无差异。没有重建队友三个独立设施/游击队/爱国者Player；它们的实际场景由完整GPU Play覆盖。
- 初版e0ce1c3独立Player共10组：八人防线、combat、talulah、interaction、monsterSelection、M09、M14、M27、M33、M38，全部退出0，检查符合预期。M14自然冰环/复起、M27显式法术靶真实破盾、M38原动作与20次枷锁、M33不普攻均成立；M33 attacked=False是预期，policy=True。结果artifacts/integration-player-results.json，逐项日志integration-player-*.log。并非真人完整键鼠通关。
- 资源：625个manifest列出文件哈希与meta通过；九张PR14卡面从原idle重新计算像素一致；资源清单303个唯一ID。全部717份StreamingAssets非meta文件与最终Player逐一SHA一致，1495份媒体/目录meta等均已Git跟踪，无依赖仅留作者电脑。仓库2936跟踪文件静态检查及git diff --check通过；检查不冒充Unity测试。
- 完整本地试玩目录：`E:/珍贵资料/昨日圆车_本地试玩/2026-10-27-PR整合-16315a8/YesterdayConvoy.exe`。965个构建文件复制后逐一SHA匹配，另附BUILD_INFO.json和游玩说明；保留完整目录，不单取EXE。该包对应PR #17，未自动合入main；不ZIP、不更新Releases。
- 首次隐藏Player逻辑检查通过，但ScreenCapture失败，不算画面证据。随后从复制目录用可见D3D12窗口重跑八人防线和选择页，均退出0，实际取得9张HUD图，查看了八人站位及M09/M10卡面。画面中发现真实守军显示“法术测试靶”，已限制该文字只出现在诊断关。
- 文案修正后的5项定向Play首次4过/1失败，暴露切场景时DefenseTrialMap.OnDestroy访问已销毁红蓝门的MissingReferenceException（artifacts/integration-final-menu.xml）。修正为幂等清理、逐个判空，只恢复借用的旧相机；新增活动防线直接重载场景回归。最终16315a8 **59/59完整GPU Play通过，零失败零跳过**（artifacts/integration-final-play.xml）。核心战斗代码未再改变，209项Edit结果保留，未冒称在文案/场景清理修复后重跑Edit。
- 最终16315a8构建复制后再次从交付目录可见运行：八人防线与选择页两个Player退出0，所有检查True、无异常，再取得5+4张完整HUD图；已查看最终8人站位/无诊断文字、实战火雨和卡面。日志integration-final-player-defenders/selection.log，图片artifacts/integration-final-visible/，结果integration-final-visible-results.json。前10组广泛Player是在e0ce1c3执行，未冒称均在最终文案/清理补丁后重跑。

关键连接：DefenderTrialDefinition → 每人的DefenderSettings/DefenderState → CombatShot → 共享伤害/控制/设施逻辑 → DefenderRosterView与EngagementView；旧单怪仍从原EngagementSettings构造兼容守军。剩余22名仅合并预览文档，未导入或确认；8人完整技能、正式商店/阵容/羁绊仍未实现。未执行第二台电脑、长时低配或人类连续通关；来源与原作再分发权利状态按原记录，上传决定不是权利人许可。

## 2026-10-25：透明角色卡面与课堂入口修复

M02至M10不再把带说明文字的portrait海报缩进卡牌，而是复用本角色已有idle首帧并去除外围透明留白。M11及后续原有透明立绘保持。纹理仅在首次裁出Sprite时保留CPU读取，随后释放可读副本；同一卡复用缓存。不生成新PNG、不改原meta/GUID或战斗动作。

Unity6000.6.5f1选择页GPU PlayMode 3/3通过；Windows构建846,411,702字节、0错误。新版选择页Player的40个可玩入口、禁止待制作项、部署/返回/清理全True；M09术师、M10重装防御者各自部署、真实攻击/受击、单只结算、三只突破与返回清理全True，三个Player退出0且无媒体加载失败。已查看与雪怪狙击手并列的真实画面。此轮未重跑198项逻辑或完整56项场景，不把历史结果当成本轮新跑。日志artifacts/portrait-*，实机图见[卡面修正](monsters/previews/selection-clean-portraits.png)。

本地第8周课堂包的00入口漏设代码页，中文HTML名被系统代码页误读。00和03入口均改为纯ASCII脚本内容、设置UTF-8并指向同级classroom-demo.html；实际CMD执行退出0，浏览器21图加载、方向键切换及1366×768适配通过。课堂包不入Git、不创建Release；第二机与完整真人课堂演练仍待验收。

## 2026-10-25：M20双阶段与真实投枪

198/198 Edit、56/56完整GPU Play通过，最后诊断修正后定向2/2通过。四Windows构建0错误；独立持久/机动Player均退出0，实际98次献祭306.3457伤害、60秒复起、四次命中、投枪实扣血和二阶段原行走可见。加载清单、原入口阻挡位置、截图过渡帧等修正与完整实机结果见[M20说明](monsters/M20-PATRIOT-EFFECTS.md)。

## 2026-10-25：M18盾卫掩护

186/186 Edit、定向1/1及完整54/54 GPU Play通过。修正验证误读守军弹道未使用统计字段，实际生命减少与Applied命中成立；最终三Windows构建0错误。独立Taunt和原完整M18 Player均退出0，详细失败修正、实机图和边界见[M18说明](monsters/M18-SHIELD-TAUNT.md)。

## 2026-10-25：M34待命、临战与神经光环

148/148 Edit、47/47完整GPU Play通过，最终自耗不持续刷新受击状态的定向M34场景1/1通过。独立设施Windows自然苏醒、临战、近战附伤、光环、神经爆发及暂停重开通过；完整入口三次合法部署与突破通过。原入口占用失败的原因、修正和实际边界见[M34交付](monsters/M34-HARVESTER-EFFECTS.md)。

## 2026-10-25：M33神经反击

136/136 Edit、46/46完整GPU Play及最终M33定向场景1/1通过。独立设施自然受击反击/神经眩晕/失去阻挡后行走，完整无普攻M33三只突破和清理均退出0；未把反击冒充普通攻击、未注入伤害。证据、边界和来源见[M33交付](monsters/M33-CREEPER-NEURAL-EFFECTS.md)。

## 2026-10-25：M32石盾与双阶段

127/127 Edit、45/45完整GPU Play通过、零失败零跳过；独立设施与完整M32 Windows实际Player均退出0。补齐ExtraClips帧名称后重跑完整回归，保留真实资源身份断言。两阶段、手动支柱破盾、凝视、范围伤害与预警倒柱实际触发，暂停/重开通过。来源、缩放和接收端未验收范围见[M32记录](monsters/M32-MANDRAGORA-EFFECTS.md)。

## 2026-10-25：M31喷火与燃料连锁

114/114 Edit、44/44完整GPU Play通过、零跳过；最终守军无阻挡配置下37角色回归1/1再次通过。设施Windows自然三只部署触发原喷火三段、灼燃、3次连爆与1根支柱倒塌，暂停/重开正常；完整M31入口交战/突破/清理通过，两条Player均退出0。初次旧攻击统计漏计喷火的失败及修复、缩放参数、实际边界见[M31记录](monsters/M31-FLAMERAZER-EFFECTS.md)。尚未第二机测试，不将EXE构建成功当作接收端已验收。

## 2026-10-25：M29与M30角色续作

M29使用暂停前通过86 Edit/41 Play的构建，补完独立Player并单独保存。新分支集成设施后，完整104/104 Edit、43/43 GPU Play通过、零跳过；其后仅修正自动验收应恢复预置火点的判断与补充断言，定向M30场景1/1通过。M30设施Player的隐匿→原动作重生、反隐→5次受击消亡两条路径最终均退出0；完整入口M29/M30正常部署/受击/攻击/突破/胜负/清理通过。Windows构建、初次验证器误报与边界见[M30记录](monsters/M30-FLAMECHASER-EFFECTS.md)。不将构建成功当作第二机验收。

## 2026-10-24：三地图设施战斗试玩

最终模型95/95 Edit、41/41完整GPU Play通过，零失败零跳过，使用精确Unity6000.6.5f1。新场景自然部署检查祭坛法术化、30秒封两格、泥岩夺取后留声机真实攻击、逐火战士靠近点燃、原图集动作、暂停和重开清理；旧40场景测试也通过。独立Windows构建与四种实际Player触发、来源、限制、失败修正见[设施效果交付](maps/FACILITY-EFFECTS.md)。不把模型试验和几何靶当成正式多角色挑战验收；未第二机测试。

## 2026-10-24：已有模型效果续作

见[逐角色证据和失败修正](monsters/EXISTING-MODEL-EFFECTS.md)，任务#20/PR#21。M14冰环/首次复起：79 Edit、35 GPU Play全过，实际Player退出0。M21–23回血：80 Edit、36 GPU Play全过，三只各自实际Player退出0。M24三目标法术吸血：81 Edit、37 GPU Play全过，实际Player退出0。M25自动三目标治疗：83 Edit、38完整GPU Play全过/零跳过，Windows构建0错误，实际Player治疗232、无伤害普攻、完整流程均True并退出0。每只在验证后独立提交推送，未做封地等设施联动或全角色完成声明。

日志/结果分别artifacts/effects-*、host-*、centurion-*、mephisto-*。最后完整回归包含之前角色场景，不冒充所有旧角色Player在每次构建重跑。已查看冰环/治疗实际HUD截图，原PNG/meta与场景/Prefab/Packages/ProjectSettings未改。第一次受限环境Unity许可198、初次测试编译类型错误、霜星靶生命/射程与诊断等待不足均如实修正重跑；不把失败版本记为成功。M24推送时提升权限的静态工具遇所有权校验失败，随后正常环境静态2539文件通过。未第二机、人类全通或长时性能验收，无ZIP/Releases/自动合并。

## 2026-10-24：雪怪效果最终10组Player回归

运行源码bf44797，后续仅文档；Edit77/77、GPU Play34/34零跳过，最终Windows846214302字节/0错误。10组最终Player全部退出0：M11普通/明确冻结靶场、M13普通/明确冻结靶场、M12正常寒冷/叠冻、M06冰爆，及原交战/塔露拉/交互/选择页四组。两个bonus=True；M12 cold/frozen/noInjectedStatus=True。已查看完整HUD和实际状态/命中反馈，日志artifacts/snow-final-*与图snow-final-player/。仓库2538文件、差异空白与文件范围核对通过，原图集/meta及场景/Prefab/Packages/ProjectSettings无变更。没有执行第二机、真人全通、长期低配验收；其余角色特殊能力与正式羁绊仍待做。详情[效果交付](monsters/SNOW-EFFECTS.md)，依赖PR15，续作PR19不自动合并，无ZIP/Releases。

## 2026-10-24：凿冰人冻结增伤与雪怪最终构建

M13按命中冻结状态采用250%攻击倍率再扣护甲，普通62、冻结计算159.5但本关实际扣血最多140。Edit77/77、GPU Play34/34零跳过，最终Windows846214302字节/0错误；M13普通与明确冻结靶场两组Player均退出0、bonus=True，实际HUD与浅青近战反馈已查看。M11/M12已分别完成并推送；最终其他Player回归与来源说明见[效果交付](monsters/SNOW-EFFECTS.md)。不声称已做霜星/其他首领、跨卡混编或雪怪羁绊。

## 2026-10-24：雪怪术师第三击寒冷

M12每第三次普通攻击的命中施加10秒寒冷，逐实体计数；两只真实部署叠加冻结，非诊断注入。M12守军生命140→400仅为显示完整链路的样板调整。Edit76/76、GPU Play34/34零跳过，Windows846214278字节/0错误，实际Player所有检查通过且cold/frozen/noInjectedStatus均True，完整HUD已查看；证据与来源见[效果交付](monsters/SNOW-EFFECTS.md)。未改场景/Prefab/Packages/ProjectSettings，未做跨卡混编/羁绊/完整首领能力；无ZIP/Releases。

## 2026-10-24：雪怪狙击手冻结增伤

[任务#18](https://github.com/Peanora/YesterdayConvoy/issues/18)，基于PR15/3847094的独立分支codex/monster-snow-effects。M11按命中时冻结状态提升至150%攻击后扣护甲，不消耗冻结；只有寒冷无增伤，飞行中到期无增伤。默认零值保持其他角色，M06共用寒冷入口语义不变。Edit74/74、GPU Play33/33零跳过，Windows846212458字节/0错误；普通与明确标记冻结靶场两组Player全退出0，bonus=True，已查看完整HUD。普通单卡关不会自行冻结，不宣称跨卡混编或雪怪羁绊已做。测试失败修正与证据路径见[效果交付](monsters/SNOW-EFFECTS.md)。未完整人类键鼠通关/第二机/长期低配；无ZIP/Releases或自动合并。

## 2026-10-22：M21–M38十八位接入，银狼暂缓

- 授权/基线：用户确认已展示18位素材，明确排除银狼，并确认继续PR #15、原main既存未跟踪设置隔离与公共不普攻接口范围。开工fetch核实main仍6be1300、功能分支333f437，独立目录干净；未改原main或其设置，不自动合并。
- 资源/配置：新增18张透明卡面、119段10,519帧；312 PNG、26份清单，72份Card/Unit/Engagement/MonsterTests配置。71段基础清单中的梅菲斯特治疗施法仅保留，不作攻击；实际使用70段基础动作，48段ExtraClips仅留存。无M41文件/配置/图像，也未带入原骨骼、加工运行库或预览。
- Unity导入：6000.6.5f1，PrepareRemainingBatch实际退出0，18个MONSTER_CONFIGURATION_PASS、40份单位与InitialDemo场景校验通过。首次导入因新增测试误访问内部CombatTargetId出现CS1061，改为通过公开HasCombatTarget和实际弹道/伤害验证，重新导入退出0。
- EditMode：最终72/72，0失败/跳过，artifacts/m21-edit-final.xml。新增覆盖不普攻单位过射程仍移动且无己方伤害、暂停、穿门一次、仍能阻挡和受伤死亡、既存配置默认攻击及仅两份新配置禁用。首次Edit为71/72，新增测试误用new EngagementSettings()得到结构体零值；改为显式调用既有带参构造，不改产品构造语义；最终重跑通过，失败记录保留。
- GPU PlayMode：32/32，0失败/跳过，artifacts/m21-play.xml。Roster遍历全部37个CARD_M（M02–M38），检查正常注册、实际图帧、暂停冻结、无技能、三次部署、按配置攻击/不攻击、通关穿门、重进清理、死亡原帧及根节点不移动；既有维维美/塔露拉/源石虫等回归同套通过。菜单与底栏后续文案修正后另3/3入口Play通过，artifacts/m21-menu-final.xml。
- Windows：最终BuildWindows退出0，BASELINE_BUILD_PASS 846218362字节、0错误，artifacts/m21-build-final.log；运行源码6608c96139dbe544e06e56ab327fe87dea8f0b08。后续仅补证据文档。构建SOURCE.txt绑定该提交和真实覆盖范围。
- 18只独立Player：artifacts/player-m21-m38/results.json，M21–M38均退出0；entered/paused/hit/resolved/three/won/noSkill/clean均True。16只攻击者attacked=True，M25/M33为False且独立MONSTER_ATTACK_POLICY_CHECK policy=True（受到敌方伤害时仍移动、没有普攻）；frozen为旧M06专用分支默认值，不作为新怪冻结能力证据。此轮在底栏文案修正前构建上运行，战斗与素材和最终相同。
- 最终构建6组Player：artifacts/player-m21-final/results.json。重跑M25/M33验证不普攻与新底栏，其余selection/talulah/interaction/wisadel旧流程均退出0、检查通过；选择页实际40可玩/1待制作银狼。两轮共24个独立进程通过，无产品运行异常或缺图回退。
- 实际视觉检查：已查看D3D12 1280×720完整HUD、八字长名菜单、宿主士兵/泥岩/蔓德拉攻击、梅菲斯特/囊海爬行者与新底栏、海嗣/萨卡兹及最后一页截图，角色透明度/比例/脚点/血条可读，名称无截断。截图仅本地artifacts/player-m21-m38与player-m21-final。没有把截图或整张平移当作新生成动画；使用确认稿离线原动作帧。
- Git/构建审计：artifacts/m21-media-audit.json，312 PNG/26清单及必要meta全部实际Git跟踪，源文件SHA256匹配清单与最终构建；清单内容匹配；最大PNG3,019,918字节，无单文件超GitHub限制。仓库/本机忽略核对未排除素材，M41在源与构建均不存在；AGENTS/缓存/日志/构建不提交。repository-checks最终结果见提交/PR。
- 本地副本：E:/珍贵资料/昨日圆车_本地试玩/2026-10-22-M21-M38-6608c96/YesterdayConvoy.exe；902文件逐一SHA256匹配构建。不生成ZIP/Releases。原工程main仍需人工合并PR并更新，不能将旧main目录当作此版。
- 关键连接：Resources测试配置→CardDefinition/LocalMonsterArt读取图集→DemoSpriteVisual按模拟时钟/路程播放；EngagementDefinition的noOrdinaryAttack默认false，快照CanAttack追加参数默认true，ManualBattle对两位禁普攻但保留受伤/阻挡/暂停/穿门；没有新治疗或首领机制。
- 未执行/限制：未做真人连续键鼠完整通关、第二台电脑、低配/长时/全分辨率或专项加载性能测试；菜单首次读取全部卡池图集，尚未专项优化。梅菲斯特治疗、囊海神经损伤、首领阶段/召唤/复活、正式羁绊/运营尚未实现；沿用通用攻击特效，未还原独立原作全套特效。原作权利状态保留，非CC0且公开再分发许可未确认。PR14旧卡面统一仍是独立任务，不宣称本轮已合入它。

## 2026-10-22：M11–M20已确认美术与独立基础交战

- 授权/基线：用户先确认十只项目外预览，再确认保留原main未跟踪PackageManagerSettings.asset的独立目录方案。从实时核实main6be1300建立codex/m11-m20-art-integration，不改原目录。范围仅基础动作/卡面/单卡普攻配置，额外阶段动作保留，不新增技能/光环/复活机制。
- 导入：Unity6000.6.5f1首次完整导入，MonsterCombatTools.PrepareNextBatch实际退出0；十只均MONSTER_CONFIGURATION_PASS，22份Unit定义校验通过，InitialDemo校验通过。10张透明卡面、55段5451帧（40段参与基础交战、15段留存）来自已确认源模型。212PNG、14清单及必要meta随源码跟踪，无新包/Spine运行时；来源和真实许可状态见art/M11-M20-INTEGRATION.md。
- Edit Mode：69/69通过、0失败/跳过，artifacts/batch-edit.xml / batch-edit-retry.log。首次batch-edit.log因Unity IL Post Processor runner未能启动、找不到IPC pipe而退出1，没有测试结果；保留失败记录，重新启动并等待进程后通过，未因此改动产品代码或包依赖。
- GPU Play Mode：32/32通过、0失败/跳过，artifacts/batch-play.xml / batch-play.log。既有Roster测试覆盖全部19个CARD_M样板，新增断言要求M11–M20实际注册而非静默待制作；每只实际检查图帧来源、暂停位置/帧冻结、无技能、三次部署/攻击/穿门胜利、重进资源清理和独立死亡动画根点。选择测试原“雪怪狙击手待制作”改验仍待制作的宿主士兵，保留未知ID拒绝。
- 菜单后续修整：长名字体缩为20px，避免八字名超出170px名称区域；仅取证入口自动滚动到被测角色。此后针对选择页补跑3/3 Play通过、0跳过，artifacts/batch-menu-final.xml。未为小字样更改再重复整套战斗测试；最终Player检查名称与入口。
- 最终Windows构建：源码b74265ddb92342ee953091370262998a2d225a41；BaselineTools.BuildWindows退出0，BASELINE_BUILD_PASS 539842988 bytes / 0 errors，artifacts/batch-build-delivery.log。此前构建日志batch-build-final.log为菜单字号修整前版本；不是最终交付来源。Builds/Windows/SOURCE.txt绑定源码、分支、Unity和基线；后续只补文档记录。
- 十只独立Player：artifacts/player-batch/results.json，M11–M20各自-monsterCombatSmokeTest均退出0，entered/paused/attacked/hit/resolved/three/won/noSkill/clean全部True。输出中的frozen为旧M06专用分支默认值，不能当成新怪冻结能力测试。此轮在最终菜单字号/取证滚动修改前构建上完成，战斗配置/逻辑与最终相同。
- 最终构建Player：artifacts/player-final/results.json。M13/M19重跑真实部署/战斗/清理并采集完整新菜单，退出0、全True；另-monsterSelectionSmokeTest九项、-talulahSmokeTest十三项、-interactionSmokeTest八项、-combatSmokeTest十三项全部True、各退出0。两轮合计16个独立Player进程，无失败；塔露拉日志original=True，未以回退图冒充原动作。
- 实际视觉检查：正常D3D12窗口ScreenCapture输出完整1280×720 HUD，已查看雪怪/霜星/盾卫/爱国者交战、M13和M19所在菜单页；名称无截断，卡面无烘焙字、保留比例，脚底/血条/高台层次可读。截图在artifacts/player-batch与player-final，仅本地，不上传预览工作区。单一路线多只实体可能重叠，未修改避让规则；克洛丝仍用现有资源。
- 资源交付审计：artifacts/media-audit.json；新增212PNG与14清单全部实际Git跟踪，PNG SHA256匹配来源清单及最终Build StreamingAssets，清单内容与构建一致，meta均配对；最大单PNG2094011字节，不需LFS。55动作/5451帧核对一致，ExtraClips实际包含在构建但不被本轮运行加载。仓库及本机忽略核对未排除新媒体；AGENTS仍忽略。
- 交付副本：E:/珍贵资料/昨日圆车_本地试玩/2026-10-22-M11-M20-b74265d/YesterdayConvoy.exe；564文件逐一SHA256匹配最终构建，没有ZIP/Releases。
- 仓库静态检查1689个跟踪文件通过，diff --check通过；主场景/Prefab/Packages/ProjectSettings无暂存差异，原工程既存未跟踪设置保留。来源记录/CSV/Credits/排期更新，不冒充原作许可已获得；不自动合并main。
- 未执行/限制：真人完整连续键鼠通关、第二台电脑、低配/长时/全分辨率验收未执行。当前只接入每只基础四动作，不含正式技能/首领阶段/原作独立投射物/全场特效、商店/升星/羁绊或混编。未重新绘制十张精绘立绘；霜星为普通enemy_1505_frstar而非冬痕。PR14旧角色透明卡面统一仍独立于本任务的main基线，不能把它当作本轮已集成。

## 2026-10-22：已确认卡面统一接入

用户对项目外V1（透明角色 / 去验稿海报包装 / 十二张实际尺寸对照）回复“可以”后实施。基于main6be1300，最终运行源码a39798d9e6b137380ecd40459f54be65631a6bc3；后续只补资源清单与验证文档。只修改九张portrait、对应SHA256和加工工具，未改C#、战斗图集、meta、场景 / Prefab / 配置数值。

- 资源与加工：九张RGBA透明卡面、非空轮廓 / 透明边缘 / 等比例裁切、最长边256且宽高均为4的倍数；各运行清单的全部文件SHA256匹配。重复执行生成器字节不变；除portrait哈希外，清单所有参数与原版一致。首次实机发现非4倍数画布触发Compress警告，已只补透明边距、重建并重新运行以下最终验证，未将首次输出当最终证据。
- Unity6000.6.5f1：实际场景 / 12份配置校验通过，Windows构建359865248字节、0错误、退出0。未重跑EditMode / PlayMode。
- 最终Player：monsterSelectionSmokeTest九项、slugSmokeTest十三项、combatSmokeTest十三项、talulahSmokeTest十三项、interactionSmokeTest八项全True、各退出0；新增M02/M08/M09普通可见窗口monsterCombatSmokeTest三组各十个输出检查全True、各退出0。共8组 / 86项输出检查；冻结字段在非冰爆样板中仅表示不适用，未据此宣称本轮验证冰爆机制。
- 实际视觉：三组可见D3D12窗口各生成7张完整1280×720 HUD图片。已查看菜单（12可玩、29待制作）、弩手暂停 / 攻击、猎狗菜单 / 攻击、术师暂停，确认原验稿海报文字消失、卡面主体放大、横向 / 方形 / 纵向比例正常，实际费用与名称继续由既有UI绘制。最终八组日志无Compress失败、Exception或缺包回退。

证据在本机artifacts/card-portraits-final-build.log、card-portraits-final-player/及verify_card_portraits.py；早期有压缩警告的日志在无final的同名前缀下，属于被替换记录。此为现有帧裁切 / 排版，不是新AI绘画、动作生成或手动通关；原作权利状态不变。未完整人工键鼠通关、第二机 / 低配 / 全分辨率验收；本机没有队友640px原始帧，完整原始帧重打包未执行，共用卡面函数已通过运行包再生成验证。独立EXE只交本地，不更新ZIP / Releases，不自动合并main。

## 2026-10-22：补齐塔露拉 / 源石虫素材，干净克隆完整复现

源码提交8be0d18d5431def15c69e76f693f5e9279e93ca1，基于main b3455c6；后续提交仅补验证文档。同机独立Git克隆，起始无Library、旧构建或额外媒体；所有素材均来自该提交，没有复制忽略目录或运行安装器。Unity6000.6.5f1首次导入、入口场景及12份单位配置校验通过，Windows x64构建360329251字节、0错误、退出0；导入后跟踪文件无变化。

新Player依次实际运行14组：monsterSelectionSmokeTest、slugSmokeTest、talulahSmokeTest、M02～M10各自monsterCombatSmokeTest、combatSmokeTest、interactionSmokeTest。全部进程退出0，所有输出检查为True；源石虫13项、塔露拉13项含原版加载、九只实际进入 / 部署 / 攻击 / 单只结算 / 三次部署 / 通关 / 无主动技能 / 返回清理均通过，M06冻结检查True。选择页启动日志确认九个本地图集及塔露拉 / 源石虫包实际加载，无缺包回退；维维美沿用Git中已跟踪的动作。Git内77 PNG与11清单共88个运行文件逐一与Player的StreamingAssets SHA256一致。仓库静态1141文件、差异空白检查通过。

证据目录：本机独立克隆 `E:/珍贵资料/昨日圆车_交付验证/2026-10-22-runtime-art/artifacts/`，包含runtime-delivery-build.log与各角色 / 流程日志；本地Player输出在该克隆Builds/Windows。没有生成新的美术或录制动作视频。本轮未重跑EditMode / PlayMode、未人工完整键鼠通关、未第二台真实电脑或低配长时验收；同机干净克隆不作为另一位队友已签收。无ZIP / Releases，原作权利状态见[交付审计](art/MONSTER-RUNTIME-DELIVERY.md)。

## 2026-10-22：M02～M10 素材入库与干净克隆复现

对应完整交付PR #12；测试提交11444fa9cc79cb7ced64ffd364db0874a3c33925。验证在同机独立Git克隆执行，Git对象由本地已推送提交取得，开始无Library/旧预览/额外包，未借用作者忽略文件。首次Unity6000.6.5f1导入与Windows构建成功：327027210字节、0错误。56 PNG、9清单及75原meta均纳入Git；逐PNG源/构建SHA256一致，Unity导入后跟踪文件未变。

已有EditMode69通过/0失败/0跳过；完整GPU PlayMode26通过/0失败/6跳过（4项M01源石虫原作包、2项塔露拉原作包），本轮九只无跳过。新Player依次M08/M02/M03/M04/M05/M06/M07/M09/M10：9/9退出0，进入/暂停拖拽/攻击/单只结算/三次部署/穿门胜利/无主动技能/返回清理均True，M06叠加冻结True。九只完整HUD截图已生成，菜单11可玩/30待制作；已查看菜单、弩手攻击、冰爆冻结与术师攻击。旧combatSmokeTest/talulahSmokeTest/interactionSmokeTest/monsterSelectionSmokeTest四组全退出0。静态1093文件、差异空白检查通过。

本地证据：独立验证目录artifacts/receiver-build.log、receiver-EditMode.xml、receiver-PlayMode.xml、receiver-player-Mxx.log、receiver-player/Mxx/及四组receiver-*SmokeTest.log。只读来源与交付记录见[素材交付审计](art/MONSTER-RUNTIME-DELIVERY.md)，实机画面不另入公共Git。后续仅记录提交不重建相同二进制。未第二台真实电脑、人类完整键鼠通关、长期/低配/全分辨率验证，不宣称接收者已签收。许可状态依原记录，学生用途不作为权利人授权；无ZIP/Releases。

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

2026-10-25 M35：f765017构建实际Player退出0，部署/暂停/普攻/受击/单只结算/三只突破/胜负/清理全部True；原型号无个人技能，补齐说明和攻击截图，无运行代码改动或新Unity测试。详见monsters/M35-BASELINE-VERIFIED.md。

2026-10-25 M37：f765017构建实际Player退出0，部署/暂停/普攻/受击/单只结算/三只突破/胜负/清理全部True；保留原型号无个人技能，未套用魔剑士机制。说明和攻击实机图见monsters/M37-BASELINE-VERIFIED.md。

2026-10-25 M36：155/155 Edit、48/48完整GPU Play通过。最后修正液泡遮挡并重建，最终完整846348794/设施732297630字节、0错误；M36 Player退出0，三段原动作、液泡、3个目标/5次神经爆发、完整胜负和清理通过。日志artifacts/m36-*，图与规则见monsters/M36-FIRST-SPEAKER-EFFECTS.md；未第二机。

2026-10-25 M38：165 Edit、49完整GPU Play全部通过。Windows设施732309893/完整846361054字节、0错误；自然20次350伤害、沉默3次52.5伤害中断、完整入口三段动作/一次枷锁/正常胜负清理三条Player退出0，暂停重开通过。来源与边界见monsters/M38-SHACKLER-EFFECTS.md，未第二机。

2026-10-25 M19：GPU定向1/1与完整50/50 Play通过；本轮无新增Edit。Windows设施732312502/完整846363661字节、0错误，两条Player退出0；自然祭坛后强化待机/行走/攻击可见，无祭坛普通完整流程正常。强化死亡为真实受击GPU配置覆盖，未做Player死亡或第二机。见monsters/M19-ALTAR-ART.md。

2026-10-25 M17：173/173 Edit、51/51完整GPU Play通过；最终Windows游击队729858246/设施732327882/完整846379057字节、0错误。独立真实三造型/两名传令兵、自然守军击杀后支援归零、暂停重开Player退出0；原完整M17也实机通过。最后仅修正独立UI文字布局后重建实机复核。来源/部署限制/交接见monsters/M17-TACTICAL-COMMAND.md，未第二机。

2026-10-25 M15：177/177 Edit、52/52完整GPU Play通过。最终Windows游击队729860909/设施732330543/完整846381722字节、0错误；Runner Player半秒真实距离0.4→0.55、暂停重开通过，原M15完整流程通过。来源/死亡恢复的模型覆盖与限制见monsters/M15-ORDER-RUNNER.md，未第二机。

2026-10-25 M16：181/181 Edit、53/53完整GPU Play通过；最终Windows游击队729865402/设施732335036/完整846386230字节、0错误。Sniper Player无命令1、有命令2个不同目标均真实命中/只计一次攻击/暂停重开通过，原M16完整流程通过。单目标/多来源上限/死亡恢复/迷彩另有模型覆盖，见monsters/M16-ORDER-SNIPER.md；未第二机。

## 2026-10-23 八人守军混合防线与工业折线地图

- 授权：人类明确将已预览8人接入，并确认独立守军数据/公共战斗接口/表现/入口范围。以实时origin/main 6be1300创建独立codex/defender-mixed-map；原工程main的ProjectSettings/PackageManagerSettings.asset保留，不提交、不覆盖。其他未合并角色PR不作为此任务基线。
- 资源：实际复制默认原作正面idle/attack/death、玫兰莎攻击三阶段、斑点治疗姿势和透明卡面，64个运行文件、48PNG、27段/2200帧，合计54738004字节（不含meta）。逐PNG检查RGBA与最大2048，Unity真实加载全部清单和Sprite布局；运行素材与源码一并跟踪，来源和未确认公开再分发许可如实记录。无新增Spine运行库或原始骨骼。不同角色待机高度通过PPU规范为1.25世界单位，所有动作固定Pivot；不缩放逻辑根节点。
- 编译/准备：DefenderTrialTools.Prepare实际创建3份独立配置并校验，退出0，DEFENDER_TRIAL_CONFIGURATION_PASS。首次准备失败是新Resources父目录不存在，已改用工程既有Data/Resources；不把失败记为成功。准备工具只在缺失时初始化，不覆盖现存调值。
- 最终EditMode：artifacts/defenders-final-editmode.xml，76/76通过，0失败、0跳过、进程0。新增7项覆盖独立最大生命/快照/重开、分实体阻挡与溢出、医疗不攻击/不过量、暂停中的待结算治疗、受疗者死亡不复活、空血医疗不出手、非法配置/重复ID、治疗范围不扩张近战范围；旧69项回归通过。
- 最终GPU PlayMode：artifacts/defenders-final-playmode.xml，34/34通过，0失败、0跳过、进程0。新增2项实际加载入口、8个本角色动作、单位缩放/位置、暂停下正常卡槽拖放到新镜头红门、动画冻结、攻击姿势、火雨/治疗/胜利、重开，返回清理并恢复原工业地图/相机。原32项回归通过。中间局部2项复核记录defenders-final-play.xml不替代最终整套结果。
- 最终Windows：BaselineTools.BuildWindows，artifacts/defenders-final-build.log，BASELINE_BUILD_PASS 415097285 bytes; 0 errors，退出0。无主场景/Prefab/Packages/ProjectSettings修改，无ZIP/Releases。新路由和地形只在试玩期间创建；配置为只读设计数据，运行快照独立保存。
- 独立Player：正常D3D12窗口运行-defenderTrialSmokeTest，artifacts/defender-player-release.log，退出0；entered/models/deployed/paused/attacked/healed/skill/won/reset/restored全部True。使用真实卡槽按下/移到投影红门/松手的输入适配，非绕过坐标限制的直接部署。
- 独立旧Player：-combatSmokeTest、-talulahSmokeTest、-interactionSmokeTest、-monsterSelectionSmokeTest分别退出0，四份defender-regression-*.log对应全部_CHECK标志True。保留原单怪参数、技力/六发、火雨移动锁、暂停、技能面板、胜负重开与选择页。
- 实机视觉：artifacts/defender-player-release/有ready与3/6/9/12秒五张完整1280×720 HUD截图，已查看ready和6秒火雨。首轮发现玫兰莎被后一格高台挡住，已错开站位到(2,.04,-2)；拉开路线/地面的明暗并复核。首次使用相对截图路径导致未保存，未算作视觉证据；最终使用绝对路径的截图确实存在。
- 数据流：DefenderTrialDefinition→不可变DefenderSettings→ManualBattle/DefenderState→CombatShot攻击/治疗→DefenderRosterView/EngagementView显示；同一模拟时钟处理动作、前摇、命中、暂停与重开。逻辑不依赖帧图，不由动画扣血；地图退出恢复借用的场景对象，既有静态场景不保存改动。
- 限制：8人完整原作技能、克洛丝二连射/天赋、史都华德高甲优先、梓兰停顿、怪物法抗配置、完整角色特效尚未制作。斑点按伤员自动切换治疗是基础样板，不是原作技力技能。使用简化通用弹道/命中与治疗圈；没有新增受击独立动画。未完整人工键鼠通关、第二台电脑离线或长时/低配性能验证。火雨容易清场，数值仅用于模型与地形观察，不宣称正式关卡平衡。主观美术/手感待人类试玩。

- 本地交付：运行代码提交 a576ea00fe336071df702cbeebbc5ee2a53ccaa4；完整401个构建文件共415097285字节已逐文件SHA256匹配复制到 E:/珍贵资料/昨日圆车_本地试玩/2026-10-23-八人防线-a576ea0/，另附BUILD_INFO.json与实机截图。代码和运行素材已推送PR #17（https://github.com/Peanora/YesterdayConvoy/pull/17），待人工合并；此后仅补交付记录，不改变已验证运行代码。

## 2026-10-23：守军清单与首批8名项目外预览

- 纯文档与项目外美术任务：根目录TODO_list含D01–D30唯一角色，12开荒/8进阶/10六星，区分位置与原作代表技能。D01/D03/D04/D07/D08/D09/D10/D12为首批。未将数值建议、守军AI适配或未制作机制登记为已实现。
- 已取得8名默认外观各正面/背面/基建的24份Spine3.8.99骨骼、atlas、纹理与模型meta；来源URL、原文件SHA256和真实权利状态保存在项目外sources.json。原作Battle模型无Move，8段基建行走独立展示。芙蓉Attack明确为治疗，玫兰莎攻击起手/循环/收手分别保留。
- 项目外ASSET-QA：74段5236帧图集尺寸/帧数/哈希、透明边界/整段非空检查通过；VIDEO-QA：8个1280×720、60fps MP4完整解码通过，8个48帧GIF存在；VIEWER-QA：file://离线页面8名/74段全部图集读取成功，0JS错误。DELIVERY-QA：154个PNG文件哈希/RGBA/最大2048尺寸，8张透明卡面尺寸和Alpha核对通过。
- 已查看8名总览、浏览器截图和米格鲁待机/芙蓉治疗/玫兰莎攻击视频画面（放大及约128px尺寸参考）。部署Start单独取景，避免高处落下动作把普通待机缩得过小；素材非AI补帧，卡面为原待机透明裁切等比缩放。
- 文档核对：30个唯一ID与角色，首批8名均在表中，参考与文档链接、git diff --check、仓库静态检查。静态检查不代表Unity验证；具体检查输出以本轮提交/PR为准。
- 未执行Unity导入/EditMode/PlayMode/Windows构建、真实守军交战与人工美术验收。不更新EXE/ZIP/Releases；未修改原工程未跟踪设置、共享场景/Prefab/包/配置或旧角色。独立治疗光束/技能特效/受击、治疗/索敌/技能AI逻辑尚未制作，A待人类看稿，B/C待做。
- 原作非CC0、公开再分发许可未确认；来源资料不等于权利人授权。工作区和预览不自动上传，本次PR只交文档。详情见[首批预览记录](art/DEFENDERS-FIRST8-PREVIEW.md)。

## 2026-10-23：剩余22名守军预览QA

- 用户选择继续制作清单剩余22名，沿用现有独立文档分支codex/defender-art-todo / PR #16；已fetch核实main仍6be1300，文档分支干净并与远端一致。原main未跟踪PackageManagerSettings保留，原8人玩法PR #17及其他工作目录未改。
- 实际66个默认模型变体、198个原始文件，逐角色具体资料页/meta/骨骼/atlas/纹理与SHA存在。423段38147帧按原Spine3.8.99曲线实际输出；22份基建原纹理小于atlas声明，视觉发现错位后按声明画布匹配纹理并重导出，不覆盖原文件，不冒充提高清晰度。
- 修复后全部帧/图集布局、SHA、RGBA/Alpha边界、整段非空和行走多姿势通过；1091清单内PNG最大2048，22张独立卡面透明/等比/最大256核对通过。VIDEO-QA最终22个1280×720 60fps MP4完整解码、22个多帧GIF存在；VIEWER-QA真实file://22名423段全部图集加载成功、0JS错误。DELIVERY-QA再次核对原源文件与图集SHA及全部预览文件。
- 实际查看造型总览/离线页、Mon3tr/圣聆初雪/玛恩纳技能的MP4帧与修复后翎羽Move姿势序列；新约能天使取样实际存在。完整原作粒子/形态实体、AI与游戏机制未实现；自动检查不替代人类美术确认。普通治疗、无普攻角色、基础攻击多段、技能各阶段、基建行走均明确区分。
- 本轮没有Unity资源/代码改动，未运行Unity导入、EditMode、PlayMode、Windows构建或实机交战，不更新EXE/ZIP/Releases。原始加工区与未接入预览仅项目外保留；确认后实际运行素材仍随代码上传。文档核对30唯一ID、首批8/其余22的状态、链接及仓库静态检查；具体文件见[本批记录](art/DEFENDERS-REMAINING22-PREVIEW.md)。
- 文档交付核对：30个唯一守军ID、首批8人/余22人状态和本轮相对链接全部通过；git diff --cached --check通过，repository-checks检查1144个跟踪文件通过。本轮仅6份Markdown改动，无运行资产或代码改动。

## 2026-11-05 泥岩与留声机独立试玩

基线origin/main 8f4d604，带入已确认PR61依赖4aead99，新分支codex/mudrock-gramophone。新增静态保存的MudrockTrial场景，保留原四场景与两个既存未跟踪项。

完整EditMode393/393通过（artifacts/mudrock-edit-final.xml）；完整D3D11 GPU PlayMode99/99通过（artifacts/mudrock-gpu-full.xml），其中2项泥岩实际场景、8项新纯逻辑。覆盖延迟/幂等占据、全施法锁移动、暂停、死亡取消、不耗费无目标、实际攻疗、邻接守军夺回、重开与配置原子拒绝；场景检查有原模型/图集/Shader实际绘制。首轮旧场景数量和即时补盾断言已按本轮确认范围改为5场景与原Skill_2命中时机，复验全过。

新Shader保留预乘与加法RGB，24非空图层58页PNG约23.6MB；角色原始动作继续走已有加载缓存。构建/独立Player证据待下方补充，不把编辑器通过当作已构建。未测：第二机、任意镜头逐像素对照、长期平衡、完整原作声音/留声机外部弹道。

最终交付补充：
- 首次源码badbba8独立Player逻辑全True；batchmode截图为黑帧，仅作为逻辑检查，不作为视觉证据。正常窗口重新运行后取得真实画面，发现底部仍是维维美卡面与东装置过低，修正启动绑定并移动装置后新增断言。
- 修正后9/9相关GPU通过（artifacts/mudrock-final-boundary.xml，泥岩/原交战/商店闭环）。
- 最终运行源码bb7db8e，Windows构建0错误，1997621649字节；产物Builds/Windows/YesterdayConvoy.exe。
- 最终正常窗口Player：artifacts/mudrock-native-final.log中MUDROCK_PLAYER的drag/pause/charge/capture/actionLock/device/restart=True；四张真实截图位于artifacts/mudrock-native-final，已目视核对泥岩卡面、原模型、屏障、就绪标志、占据动作与装置位置。Player自动流程完成并退出，无异常。
- 原两个未跟踪文件不提交。未自动ZIP/Releases；源码和运行素材走功能PR，不自动合并。


## 2026-11-05 感染/雪怪/泥岩成长重制

- 分支codex/synergy-growth-rework；远端main=8f4d604，已正常fetch并核对，按授权吸收未合并PR64依赖。原有.vscode/和PackageManagerSettings.asset保留。
- SynergyReworkTools.Prepare真实运行成功；只向Campaign/SynergyTrial各增加三台已保存装置，没有重生成地形/路线或覆盖主场景。
- EditMode第一次407/408：新增风雪测试漏配FrostNova标志；修正测试配置。中途新增PlayMode测试误用internal读取器造成编译失败，改用公开资源入口。最终Logs/synergy-rework-edit3.xml为408/408，无跳过。
- D3D11完整PlayMode：Logs/synergy-rework-play.xml为100/101；唯一失败为既有全怪物用例仍断言37个CARD_M角色。改为39，Logs/synergy-rework-roster.xml该全遍历1/1通过；最终101项均实际覆盖，没有将失败结果隐藏为一次全绿运行。
- 新增验证：独立双爆炸115实际伤害、雪怪4人低档/5人不消耗冻结、0/2/4/6秒脉冲、暂停/重开、成长全部门槛、投入扣费与唯一关间上限、补给不产培养、终极三目标只回血一次与第三击全场伤害；图形验证真实卡面、动作资源、暂停拖动、泥岩占据配置与三台锚点。
- 终极巨像基准：现有Maps/BattleShop完整六星配置、原固定路线/高低台/技能与索敌，单巨像28.300000秒清空6名守军，1次守军技能实际施放，存活且末次回复后HP6000。测试没有降低守军数值或关闭技能；这不是所有未来守军与地图必胜证明。
- 新增组长27张动作图集＋2卡面PNG共17546295字节，680帧/60fps。项目外10秒视频已完整解码；运行清单、源URL/哈希、原权利状态与meta随源码。外部Spine代码/原工作区不入项目。
- 当前未完成：Windows构建/独立Player、第二机、长期平衡和完整局成型概率。后续实际结果追加于此。

补充：完整场景验证后增加“已冻结目标仍有天气”的3/3定向通过，逻辑15/15复验。实机目视发现低位风雪被已保存地形遮挡，新增天气显示只抬高视觉节点0.8相机上方向，不改逻辑位置或原冰封特效挂点；D3D11真实相机启用/禁用风雪像素差异1/1通过（>30像素实际变化），不再只依赖renderer.enabled。独立Player六项全True、退出0；交付版本仍在复验。

最终交付：运行源码cfad4a2，Logs/synergy-rework-build-verified.log记录BASELINE_BUILD_PASS 2015199842 bytes、0 errors。Logs/synergy-rework-verified-player.log六项(pool/preset/pausedDrag/ultimateSoloClear/investment/snowPulsesAndReset)全部True；通过Python等待真实Player进程退出，VERIFIED_PLAYER_EXIT_CODE=0。Logs/synergy-rework-verified下四张截图已目视核对，培养面板明确标注预置，不把加载页截图当完成。最后只把天气抬升限制到新增脉冲调用，原冰封挂点保持。共408项完整逻辑通过、最后15项逻辑复验；102种GPU场景用例均获通过验证（100/101原整套＋角色总数修正全遍历1/1＋天气3/3＋像素1/1）。

进入：EXE默认挑战，菜单→返回测试选择→羁绊验证·无限金币，右侧“巨像培养”提供I–IV；IV预置直接设培养24/投入80/五人二星并换完整六星防线，开始下一战后拖巨像卡即可。正式模式不提供这种进度赠送。第二机、完整局成型概率/长期平衡及所有GUI按钮真实鼠标逐项点击未做。不ZIP/Releases，不自动合并。原作素材开放授权仍未确认，来源与权利记录保留。


## 2026-11-05 本地双角色特效整合

2026-11-05 本地试玩：main 2c04ba7 + 爱国者 V2 14bda74，运行源码 9826603。塔露拉 V8 已在 main。Windows 构建 0 错误，两个角色 GPU 回归 4/4。爱国者、塔露拉火雨、塔露拉动作、羁绊试玩共四个独立 Player 进程退出 0。复起和投矛使用开发阶段测试。未重跑全套测试或第二机。 本轮验证日志在 Logs/local-vfx-integration 和 Logs/local-patriot-talulah-build.log。

## 2026-11-07 泥岩经验培养、6档和连续专项

基线为实时main b1d55e7，功能分支codex/mudrock-growth-feedback。原52项Mandragora/Mephisto PNG.meta及.vscode/、PackageManagerSettings.asset按此前已确认方案保留，不纳入任务提交。无场景地形/Prefab/包/工程设置修改；泥岩专项只调整自己的卡组与守军配置。

- Unity6000.6.5f1完整EditMode：artifacts/mudrock-growth-edit.xml 538/538通过，真实进程退出0。
- 相关D3D11 PlayMode：artifacts/mudrock-growth-play.xml 24/24通过，覆盖原羁绊/九专项/暂停部署/商店/游击转职回归。
- 首轮发现新增场景测试meta GUID为33位，Unity忽略该资产；已更正为生成的32位GUID并单独补跑。artifacts/mudrock-growth-new-play.xml四项中三项通过：免费转职生命周期、自动经验/封顶/连轮、默认专项真实招募合成和自然四战从0培养至48/V；唯一失败为测试运行器禁止batchmode的WaitForEndOfFrame，尚未进入像素断言。
- 同一UI像素用例改用非batch窗口渲染运行，artifacts/mudrock-growth-ui-play.xml 1/1通过、退出0，实际屏幕XP条前后像素差、I→II提示、实际获得量和封顶说明均通过。共28个不同相关PlayMode用例已获通过，不将首次3/4失败记录改称一次全绿。
- 经验阈值、阵容容量、无部署/击杀任务、Won/Lost自动结算、补给/中断不发、实际增量截断、历史XP保留、三星补偿一星、九张库存/正式高费委托拒绝已由逻辑测试覆盖。自然测试使用现有正常战斗和真实委托，不使用FinishForTesting或篡改守军。

本轮Windows构建/独立Player与可见窗口验收尚未运行，完成后追加实际结果。未做完整13关自然成型率、长期平衡、第二机或全分辨率验收。


独立Player补充：运行源码aabb23b、artifacts/mudrock-growth-build.log构建1045846289字节、0错误、真实Unity退出0。以TEMP为工作目录启动1920×1080 Direct3D11可见Player，`-mudrockGrowthSmokeTest -mudrockGrowthRequireClicks`的20项检查全部True、真实Player退出0（artifacts/mudrock-growth-player/result.json）。正常委托/三合一及7次自然胜利形成XP0→6→6→14→24→30→42→48，无FinishForTesting或临时生命/攻击夹具；V正常部署验证7500血/2000攻/14DP/每场一次/免控。鼠标真实点击构筑、阵营培养、巨像培养和免费弩手转职，六次GUI等待均由实际窗口操作完成；看过初始培养页、真实经验插值/升级、封顶说明、转职及自然V结算截图。17张截图仅本地artifacts，显式IV/V预置与自然链分开记录。

之后仅新增普通`-mudrockCultivation`启动路由及tools/play_mudrock_trial.cmd，源码c2e568e；用于交付可正常持续游玩的直达入口，不带自动化烟测。最终同目录构建、直达入口结果在下方追加；上述7战自然链对应aabb23b的完全相同培养/战斗代码。未做正式13关自然成型概率、长期平衡、第二机或全分辨率验证。保留原52项meta字节一致、两个未跟踪项不动；不ZIP/Releases、不自动合并main。


最终直达入口：c2e568e源码的artifacts/mudrock-growth-final-build.log为1045846309字节、0错误，真实Unity退出0。已复制tools/play_mudrock_trial.cmd到Builds/Windows/泥岩培养.cmd；使用同一普通参数-mudrockCultivation从TEMP启动1600×900 Direct3D11，实际进入五原生一星加弩手、0经验的独立专项；鼠标直接点击左侧泥岩条目打开培养面板，已观察人数/总星/当前上限/下场预计经验及真实补卡按钮。普通窗口仍正常响应并停留供人类试玩，不会执行自动退出。最终可玩入口为Builds/Windows/YesterdayConvoy.exe或同目录泥岩培养.cmd，保留整个Windows目录。
