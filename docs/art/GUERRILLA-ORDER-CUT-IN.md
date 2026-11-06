# 游击队恒速拼屏与真实全场追击

人类于2026-11-06认可项目外V2动态视频并授权接入。V1只改变整屏图片透明度的演出已被否决，旧技术结果保留在QA历史中，不能作为V2完成证据。

新演出依次为中央晶核出现、角色玻璃切面各自从外围向中心聚拢、短暂冲击与停留、切面裂为独立三角碎片、旋转漂移/收缩并逐渐消散、恢复战场。人物纹理来自此前认可的七角色AI概念裁片，原PNG字节保持；不会把整张海报或预制视频当作动态演出播放。

九个放射切面的成员顺序固定为M15战士、M16狙击手、M03猎犬、M20爱国者、M19萨卡兹战士、M18盾卫、M17传令兵，再加底部两个扩展槽。切面角度为230/190/150/270/310/350/30/70/110度（屏幕向右为0，顺时针）。当前编队决定显示，未部署或已经离场也仍显示，未入编队者原位留暗玻璃，不挤动其他人。

UseCampaignOrder成功后，ShowGuerrillaOrder读取模型GuerrillaMemberIds战前快照（原生＋实际入编转职），交给独立GuerrillaOrderView；转职肖像复用对应CardDefinition.Portrait。Play额外接收两槽指定顺序，选中但留在备战的转职原位留空，另一位不挤过来。显示层只读实际成员/命中记录，不选择攻击受益者，不写资源或军令。成功整令及6档前后半令可各播一次，失败不播。

塔露拉的通用Portrait优先读取OriginalTalulahArt.For(card).Portrait，来源为已接入的StreamingAssets/TalulahOriginal/portrait.png（或其同源PreparedArt缩略图），不再因漏掉原作分支而读取序列化的旧TalulahComic/V2卡面。仅加载头像及动作元数据，不在读卡面时解码整套动作；其他角色原有读取与序列化回退继续保留。追击沿用同一通用入口，无需独立替换图片或修改GUID。

2026-11-06追加人类要求后采用2.6演出秒节奏（1×/2×相同）：0.08秒开始逐片入场，约0.475秒聚齐、0.49秒冲击、0.59–0.90秒短停；0.90–2.10秒每片分成12个独立带纹理碎片，随后只留少量碎光至2.60秒。演出使用由Controller.Tick推进的独立累计时间，1×/2×等速；原音pitch保持1，暂停冻结几何和音频，既有技能菜单0.03×慢速保留。终局直接等待Playing结束，不将该时域混入战斗PresentationClock；终局不再结算新军令伤害。重开、换关和禁用清理。

2026-11-06后续已确认并接入2/4/6/8/9档及所有档整令无次数上限；8档开战2/耗3/180%、9档开战2/耗2/300%，高档全体合格普通实体、不受射程或普通动作窗口限制；低档人数/资格沿用，6档半令可循环配对，8/9不拆。开战赠令只在Ready→Running给一次，积令每实体1点/每种2点保持。准备阶段“构筑与培养→游击转职”免费指定最多两种持有非原生角色，战斗锁定，备战保留/入编计数、升星继承/售光回收、同挑战跨关保留/新挑战清空；其他原标签不变。

最新人类明确选择“全场守军都实际受伤”。模型TryGuerrillaOrder成功时提交GuerrillaOrderVolley，保留费用/次数/参与人数/资格/半令排除和近战护盾；Advance(战斗delta,演出delta)在0.9演出秒对每个存活守军各结算该档参与者60/80/90/180/300%攻击预算，逐源保留物/法、冻结增伤以及目标护甲/抗性/减免；不再叠加旧单体CombatShot，不分摊全场总量，不递归产令或固有技能。来源提前死亡/复起/离场不贡献，终局取消未出手批次。

每批Hits只记录各目标实际HP损失。碎裂点接蓝白贯穿扫击，暗幕在0.94–1.36秒退开，玻璃碎片仍漂移但迅速降低遮挡；真实受伤点爆光和大号数字、目标数/总伤害读同批Hits，不预测Attack或写HP。View的BindVolley只绑定记录，绘制和音效不触发伤害。普通战斗在2×仍加速，军令出手与原声保持固定节奏。

完整几何、晶核、吸入线、交叉光束和碎光通过同一独立CommandBuffer离屏渲染路径生成，显式设全viewport、关闭scissor并为每层绑定纹理；LateUpdate提前合成，OnGUI只绘制最终RT，支持实际GPU输出验证；透明度正确合成后用IMGUI前景绘制，保持既有16:9视口与输入。运行时资源只包含人物纹理、必要Shader、原声音频和meta，不导入看稿HTML、参考录屏或加工工作区。

用户明确要“拼屏碎裂／总攻击出手声”。GuerrillaOrder/followup-original是从本机《崩坏：星穹铁道》只读取得的原Wwise事件Ev_sfx_GridFight_FollowUp_Timeline二倍速媒体745947550的前0.90秒，与已批准V2样本字节一致。保持原音高，只含样本原有末30ms淡出；48kHz双声道PCM16，在演出0.90秒碎裂时播放一次，运行增益0.55与看稿一致。它不是入场声，也不是合成替代音；旧shing和生成器已退役。

实际参考为[货币战争追击总攻击视频](https://www.bilibili.com/video/BV1so1QBNEzs/)的145.75秒起片段，已查看完整入场、停留、碎裂与返回战场关键帧。两次视频中原声起音与本机媒体存在实际波形对应，见source-records/guerrilla-original-audio.json；这里借用原作运动结构，不声称原客户端完整粒子/Timeline工程逐帧移植。原图来源见source-records/guerrilla-fullscreen.json及Atlas旁SOURCE.md。

角色设计和音频均保留其原有真实权利状态。用户看稿确认、接入及源码上传要求不是权利人授权，不将原音或派生图登记为CC0。

最新728c37d补塔露拉原作头像读取：完整Edit501/501、相关GPU Play26/26、独立Player9/9、Windows构建0错误通过。显式-talulahPursuitPortraitSmokeTest -demoEvidenceDir <目录>验证真实九人转职军令及同年龄原作/旧卡面纯渲染对照，实际第九槽和原作一致，诊断Play不新增军令。此前7747438的8/9档、无整令次数上限和实际点击Player32/32及GPU48/48为前版本记录；原恒速全场追击e747ff0、a5b844e结果见QA历史。独立Player原显式入口保持-guerrillaOrderSmokeTest -demoEvidenceDir <目录>，可选-guerrillaOrderCaptureFrames输出实际逐演出帧画面，追加-guerrillaOrderCaptureDoubleSpeed验证2×战斗时仍完整播放；只在Development显式参数下运行。耐久守军只作开发夹具，不代表自然平衡通过。

高档独立验收参数为-guerrillaHighTierSmokeTest（可追加-guerrillaHighTierCaptureFrames）；该入口以9级9种库存与耐久/延后攻击守军作明确夹具，GUI需实际操作，不自动假点选。

Windows产物保持Builds/Windows-GuerrillaOrder/YesterdayConvoy.exe；Editor入口YesterdayConvoy.Editor.GuerrillaOrderBuildTools.BuildWindows复用原角色准备、场景清单和配置验证。不自动ZIP/Releases，不自动合并PR。
