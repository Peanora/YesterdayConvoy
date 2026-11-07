# 羁绊达档反馈

2026-11-07。人类在项目外V2动态看稿后明确要求补充《货币战争》原版音效并接入游戏。范围为整备编队卡的达档反馈，不修改羁绊门槛、效果、库存、资源或战斗快照。

## 采用看稿与绑定

| 项目 | 采用看稿 / 读取点 | 事件与时钟 | 状态 |
| --- | --- | --- | --- |
| 卡顶菱形与细边 | 项目外V2；复用已确认九枚白徽与铜/银/金/青绿最高阶底牌 | `SyncShopDeck`之后观察真实`CurrentSynergies`；准备阶段呈现时钟 | 已绑定，实机结果见QA |
| 新入队卡短亮框 | 同一批次前后编队不同种类的差集 | 与该批达档同时；转职不伪造新入队 | 已绑定，实机结果见QA |
| 达档提示音 | `Resources/SynergyActivation/bond-original`，原作金档羁绊UI纯音 | 每批一次，与徽记首个可见帧对齐；原音高 | 已绑定；来源与验证见本轮来源记录和QA |

官方参考为[《走近星穹——「货币战争」特别节目》4:20](https://www.bilibili.com/video/BV1kc1uBkE4s/?t=260.2)。实际检查720p视频260.217–262.183秒的60个连续原始帧，没有时间抽样。刻夏入队后，前台三张、后台两张相关卡出现菱形；无关罗刹没有。入队强框在260.283秒起，共同反馈260.317秒起，白亮峰区260.750–261.017秒，262.083秒已消失，边界约一帧。

核实的是该视频银蓝色“5群攻”一例。本作复用V2的动势，并按既有各羁绊徽记、卡片尺寸与已确认阶色适配；金/青绿最高阶不是已经逐帧核实的原客户端独立特效。强整圈亮框只用于新入队卡；其他关联成员显示淡细顶边与卡顶玻璃菱形，没有人数圆点、星级改变、常驻金框或大横幅。

## 运行连接

`SynergyActivationFeedback`只读`SynergySummary`的真实`Tier/Members`，保存当前档位而不是历史最高档位。只有向上跨档生成新批次，同档刷新、升星、换形态不重复；降档或整行消失后重新达档会再次播放。游击8到9虽然同为青绿最高阶，实际档位上升仍触发。同一次更新多羁绊合为一批，音效起一次，同卡多个徽记横排。快速再次跨档以最新批次接管，退编成员和失效旧档即时剪除。

`SynergyActivationUI`连接原`SyncShopDeck/Tick/DrawLoopInterface`，按当前编队ID重新定位`ApprovedUi.LineupSlot`，不对备战同名副本显示。观察身份跟随`Shop`实例，避免每次重建`Battle`误触发。普通换关保留观察值，新挑战重新观察初始阵容。

现有购买入场尚未抵达时，反馈等待原`ShopFlow.Reveal`，不向空卡位提前播放。感染选择、构筑弹窗、菜单与加载期间暂缓或冻结；关闭后继续。声音遵守现有`Muted`，恢复不重新起音。整备Ready阶段本来不能调用战斗暂停，本轮不改变该规则；整备菜单冻结呈现和`AudioSource`，进入战斗前立即清掉反馈，战斗暂停时没有遗留演出。离开与组件禁用同样立即清理。反馈只改变展示，不推进战斗、扣钱、部署、造成伤害或改变库存。

原音是本机原始Wwise媒体解码所得的1.4905秒48kHz双声道PCM16，保留原音高和字节，不混入旁白或BGM。事件名称`Ev_sfx_ui_feedback_activity_GridFight_Trait_Gold`由标准Wwise FNV1及同银行多条语义一致事件推断；真实事件→动作→Sound→媒体链已核实，不称从官方明文表恢复的名称。该金档提示音适配本作所有达档批次，没有宣称复制原作铜/银/青绿最高阶每档独立声音，也未声称与官方银蓝演示逐波形一致。详细链路与真实权利状态见[音源记录](source-records/synergy-activation-original-audio.json)。

## 验证入口

- EditMode：`SynergyActivationFeedbackTests`；实际执行范围和数量见QA。
- PlayMode：`SynergyActivationSceneTests`及受影响场景回归，使用真实配置与编队入口，不直接写战斗档位。
- 独立Player：`-synergyActivationSmokeTest -demoEvidenceDir <目录>`，合法七张库存含一张无关海嗣；外部真实鼠标操作原生编队和感染关闭按钮。捕获完整OnGUI触发前、峰值、暂停、消退与模态前后画面，并比较卡顶像素，不能用Camera-only渲染代替。
- 本轮本机构建入口：`YesterdayConvoy.Editor.SynergyActivationTools.BuildWindows`，输出`Builds/Windows-SynergyActivation/YesterdayConvoy.exe`，保留原正在运行的Windows产物。未创建ZIP或Releases。

最终提交、原音来源、实际测试结果、未测项与本机EXE对应关系以STATUS/QA为准。项目外看稿、参考视频、解包工具和缓存不放入运行Assets或推送。
