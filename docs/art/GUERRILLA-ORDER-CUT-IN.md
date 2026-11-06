# 游击队恒速拼屏与真实全场追击

人类于2026-11-06认可项目外V2动态视频并授权接入。V1只改变整屏图片透明度的演出已被否决，旧技术结果保留在QA历史中，不能作为V2完成证据。

新演出依次为中央晶核出现、角色玻璃切面各自从外围向中心聚拢、短暂冲击与停留、切面裂为独立三角碎片、旋转漂移/收缩并逐渐消散、恢复战场。人物纹理来自此前认可的七角色AI概念裁片，原PNG字节保持；不会把整张海报或预制视频当作动态演出播放。

九个放射切面的成员顺序固定为M15战士、M16狙击手、M03猎犬、M20爱国者、M19萨卡兹战士、M18盾卫、M17传令兵，再加底部两个扩展槽。切面角度为230/190/150/270/310/350/30/70/110度（屏幕向右为0，顺时针）。当前编队决定显示，未部署或已经离场也仍显示，未入编队者原位留暗玻璃，不挤动其他人。

UseCampaignOrder成功后，ShowGuerrillaOrder复制去重的编队游击成员交给独立GuerrillaOrderView。显示层不选择攻击受益者，不写资源、军令或伤害。Play可额外接收已判定的转职成员ID与Sprite；现阶段上游仍只有原生成员，因此两扩展槽为空，本轮不新增转职玩法。成功的前/后半令可各播一次，失败不播。

2026-11-06追加人类要求后采用2.6演出秒节奏（1×/2×相同）：0.08秒开始逐片入场，约0.475秒聚齐、0.49秒冲击、0.59–0.90秒短停；0.90–2.10秒每片分成12个独立带纹理碎片，随后只留少量碎光至2.60秒。演出使用由Controller.Tick推进的独立累计时间，1×/2×等速；原音pitch保持1，暂停冻结几何和音频，既有技能菜单0.03×慢速保留。终局直接等待Playing结束，不将该时域混入战斗PresentationClock；终局不再结算新军令伤害。重开、换关和禁用清理。

最新人类明确选择“全场守军都实际受伤”。模型TryGuerrillaOrder成功时提交GuerrillaOrderVolley，保留费用/次数/参与人数/资格/半令排除和近战护盾；Advance(战斗delta,演出delta)在0.9演出秒对每个存活守军各结算原参与者60/80/90%攻击预算，逐源保留物/法、冻结增伤以及目标护甲/抗性/减免；不再叠加旧单体CombatShot，不分摊全场总量，不递归产令或固有技能。来源提前死亡/复起/离场不贡献，终局取消未出手批次。

每批Hits只记录各目标实际HP损失。碎裂点接蓝白贯穿扫击，暗幕在0.94–1.36秒退开，玻璃碎片仍漂移但迅速降低遮挡；真实受伤点爆光和大号数字、目标数/总伤害读同批Hits，不预测Attack或写HP。View的BindVolley只绑定记录，绘制和音效不触发伤害。普通战斗在2×仍加速，军令出手与原声保持固定节奏。

完整几何、晶核、吸入线、交叉光束和碎光通过同一独立CommandBuffer离屏渲染路径生成，显式设全viewport、关闭scissor并为每层绑定纹理；LateUpdate提前合成，OnGUI只绘制最终RT，支持实际GPU输出验证；透明度正确合成后用IMGUI前景绘制，保持既有16:9视口与输入。运行时资源只包含人物纹理、必要Shader、原声音频和meta，不导入看稿HTML、参考录屏或加工工作区。

用户明确要“拼屏碎裂／总攻击出手声”。GuerrillaOrder/followup-original是从本机《崩坏：星穹铁道》只读取得的原Wwise事件Ev_sfx_GridFight_FollowUp_Timeline二倍速媒体745947550的前0.90秒，与已批准V2样本字节一致。保持原音高，只含样本原有末30ms淡出；48kHz双声道PCM16，在演出0.90秒碎裂时播放一次，运行增益0.55与看稿一致。它不是入场声，也不是合成替代音；旧shing和生成器已退役。

实际参考为[货币战争追击总攻击视频](https://www.bilibili.com/video/BV1so1QBNEzs/)的145.75秒起片段，已查看完整入场、停留、碎裂与返回战场关键帧。两次视频中原声起音与本机媒体存在实际波形对应，见source-records/guerrilla-original-audio.json；这里借用原作运动结构，不声称原客户端完整粒子/Timeline工程逐帧移植。原图来源见source-records/guerrilla-fullscreen.json及Atlas旁SOURCE.md。

角色设计和音频均保留其原有真实权利状态。用户看稿确认、接入及源码上传要求不是权利人授权，不将原音或派生图登记为CC0。

此前a5b844e的验证为单体追击/倍速耦合旧版历史，本轮恒速与全场追击的新结果以QA为准。独立Player显式入口保持-guerrillaOrderSmokeTest -demoEvidenceDir <目录>，可选-guerrillaOrderCaptureFrames输出实际逐模拟帧画面；只在Development显式参数下运行。耐久守军只作开发夹具，不代表自然平衡通过。

Windows产物保持Builds/Windows-GuerrillaOrder/YesterdayConvoy.exe；Editor入口YesterdayConvoy.Editor.GuerrillaOrderBuildTools.BuildWindows复用原角色准备、场景清单和配置验证。不自动ZIP/Releases，不自动合并PR。
