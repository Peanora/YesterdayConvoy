# 游击队全屏协同闪现

人类于2026-11-06认可项目外V1，并明确以当前编队为显示范围：尚未部署的编队成员也显示，备战未入编队者对应切面留空。固定位置依次为左上战士M15、左中狙击手M16、左下猎犬M03、中央爱国者M20、右上萨卡兹战士M19、右中盾卫M18、右下传令兵M17。缺位不重排其他人物。

左右两侧保留暗色斜切扩展槽。`GuerrillaOrderView.Play`接受已判定的成员ID和可选Sprite字典，未来非原生成员进入这两槽；目前上游仅查询现有游击队名单，扩展槽保持空，未实现转职装备/改标签或改变羁绊档位。

军令成功事务在`SynergyInteractions.UseCampaignOrder`调用`ShowGuerrillaOrder`，从当前`shopLineup`复制去重的成员列表。独立`GuerrillaOrderView`不读取部署/存活状态、不选择攻击受益者、不写战斗资源。原伤害与护盾仍由`ManualBattle.TryGuerrillaOrder`结算。失败不播放；成功的前/后半令各可播放一次。

资源为已认可AI概念PNG原字节及一个生产Shader，按固定分区遮住未入编队人物，主标题共享。两扩展Sprite同样在这一Shader中合成，没有固定整图露出全部人物。16:9等比例适配共用`TacticalViewport`，不消费鼠标事件。0.14秒平滑淡入、0.18秒停留、0.40秒平滑淡出（共0.72秒）为本项目试验值；读取`PresentationClock`，暂停冻结、2倍/技能慢速跟随。终局只延续表现尾，不推进战斗伤害；重开/切关/禁用停止并清除。

生产材质以straight alpha合成到复用RenderTexture，再通过IMGUI队列绘制；本组件每次OnGUI保留自身排序深度，使峰值全屏盖住HUD、血条和卡栏，淡入淡出时平滑恢复下层画面。矩阵/颜色恢复不影响其他绘制。实际屏幕对照和1920×1080实图已验证，不只依赖离屏材质像素。

原创合成音效`GuerrillaOrder/shing`为0.64秒、48kHz双声道PCM16，约-6dBFS峰值。无原作采样、无人声说sing。`tools/art/build_guerrilla_shing.py`可复现，播放器跟随静音、暂停及速度。作者通过解码和数值验证，但未实际试听；最终音色和音量由用户听感确认。

AI图使用项目现有七种原作角色卡面作身份参考，不是原客户端立绘提取或已取得权利人授权；用户批准接入和源码上传不改变既有角色来源/权利限制。原稿和音效来源记录为`source-records/guerrilla-fullscreen.json`、`source-records/guerrilla-shing.json`，Atlas旁另有`SOURCE.md`。货币战争视频本轮未能实际播放，不声称逐帧复现其节奏或音色。

真实验证见`docs/QA.md`。独立Player显式开发入口为`-guerrillaOrderSmokeTest -demoEvidenceDir <目录>`；可选`-guerrillaOrderCaptureFrames`输出逐模拟帧的实际Player画面。该入口只在Development且显式参数下安装，不影响正常玩家流程；耐久守军夹具不是自然平衡验证。

本轮Windows输出到`Builds/Windows-GuerrillaOrder/YesterdayConvoy.exe`，避免覆盖用户当前运行的旧`Builds/Windows`试玩。对应Editor构建入口为`YesterdayConvoy.Editor.GuerrillaOrderBuildTools.BuildWindows`，复用项目角色准备、场景清单及已有配置验证。
