# 当前工程状态

更新：2026-10-17。当前入口为 **InitialDemo**：一个怪物、一座塔、一个红门、一个蓝门，斜俯视立体棋盘。点击开始 → 怪物移动 → 塔自动攻击 → 突破扣门或怪物死亡 → 显示结果并可重开。T00–T02 保留为历史基线。

## 最新：第二、三阶段动画样板完成（2026-10-17）

第二阶段已提交 `754f758`：U01 独立部件、52 个持久化 Sprite、U01Visual Prefab、四关键姿势和独立预览。第三阶段新增同风格的无腿灰焰与 `Assets/_Game/Prefabs/Characters/AshWispVisual.prefab`，在 `AnimationLab.unity` 与 U01 同屏，用同一条 RouteMotion、同一模拟时钟驱动；左右分开仅是展示位置偏移。灰焰不进入战斗卡池、不新增费用/技能。

`DemoSpriteVisual → UnitVisualFrame → UnitVisualPresentation` 为共同表现入口；BipedVisualPresentation 驱动关节/IK，FloatingVisualPresentation 驱动悬浮/呼吸/倾斜与阴影。两者各自持有动画状态，读取原运动输入，暂停/重开不推进独立墙钟、不修改逻辑根节点旋转或缩放。资源/配置只读，没有引入 Unity 包或改战斗规则；原卡面、右向正面与 0.75 速度保留。

双击 `Builds/Windows/YesterdayConvoy.exe` 后点右上“动画预览”，可停止/继续、暂停/恢复、重开，或点击四个放大关键姿势；“返回战斗 Demo”可回主场景。`-animationLab` 可直接进入预览，两个场景均已纳入构建。

本轮 37/37 Edit Mode、11/11 Play Mode 通过；最终 Windows 构建 **242759557 字节、0 错误**。真实图形 Player 预览 stop/pause/restart/sharedInput/secondBody 与战斗五项检查全 True，两个进程均退出 0。已查看双角色全图、关键姿势和行走帧；动图 `artifacts/stages23/two-body-preview.gif`，四姿势图 `final-keyposes.png`。详细证据/失败修复见 QA。

仍待人工观感验收：AI 部件细节与方向体块复用有局限，当前不是完整手绘多方向蒙皮动画。完整鼠标 UI、第二机离线、多单位压力测试未运行；重型/多足专用动画未在本轮扩充。

## 前次：第二阶段分层角色样板（2026-10-17）

用户已明确授权继续第二、第三阶段。第二阶段现已实现：原身体裁片换为带完整关节覆盖的正/背两张部件图；编辑器按实际 Alpha 区域识别 12 部件/图，生成 52 个独立 Sprite 与 `Assets/_Game/Prefabs/Characters/U01Visual.prefab`。头部使用原贴图遮罩、卡面原样保留，右向仍为原三分之四正面、移速 0.75。侧/斜背身体复用正/背部件，不冒充完整多方向专业分层绘制。

战斗右上新增“动画预览”，打开 `AnimationLab`；可看四个关键姿势、起停、暂停和重开。`DemoSpriteVisual` 把只读 `UnitVisualFrame` 传给 `BipedVisualPresentation`，`DemoWalkPuppet` 操作 Prefab 保存的关节；Sprite/PPU/Pivot 由编辑器预先保存，运行时不创建裁片网格/材质，实例状态彼此独立。仍采用 Transform 与已有两段 IK，未新增 Unity 包。

36/36 Edit Mode、最终 10/10 Play Mode、Windows 构建（235409441 字节、0 错误）通过；真实 Player 预览 stop/pause/restart 与战斗五项均 True，已查看关键姿势与实机行走。证据 `artifacts/stages23/stage2-*`，详细失败修复与限制见 QA。第三阶段正在继续；人工鼠标完整流程、第二机离线、多单位性能未测，观感仍待用户验收。

## 前次：向右行走恢复原正面姿态（2026-10-17）

用户要求向右走保持原来的样子，不用侧身。`DemoWalkPuppet` 将右向稳定展示角设为三分之四正面，恢复原正面裁片的横向重心与前后脚站位；向下仍用修正后的共同落点，侧面/斜背面保留在转角中间过程。运动输入经 DemoSpriteVisual 传入的连接不变，速度 0.75、步频、路线与战斗数值不改。

Play Mode 8/8、Windows 构建 222722869 字节、0 错误通过；真实图形 Player 五项结算检查全 True、退出 0，已查看右向正面行走接触图。更新包为 `Builds/Windows/YesterdayConvoy.exe`，实机动图在 `artifacts/right-pose/walk-closeup.gif`，详细结果见 QA。本次未重跑 Edit Mode。未新增图片、包或修改场景/Prefab。自然程度仍待用户试玩；完整鼠标交互、第二机、多单位性能未测。

## 前次：半速、向下步态与方向转身（2026-10-17）

用户确认实际移动和腿部动作都慢一倍：U01 配置速度 **1.5→0.75**，步相仍按距离、Stride=0.64，周期约 **0.853 秒**。没有调整全局时钟、塔攻击伤害/间隔或生命滑块。减速后受攻击更多，默认 100 生命会被击败，现有滑块调到 **140** 可验证突破；30 生命仍用于快速失败。此结果已在 Play Mode 验证，不默改平衡数值补偿。

向下步态缩短纵深步幅并统一左右落地基准，减少短腿反复触达长度限制的跛脚感。新增右侧面和斜背面两个 PNG（来源/许可/提示词见 art/GENERATION-2026-10-17.md），与原正背面形成四套裁片 Rig。转向约 0.38 秒，头部领先、身体跟随、双脚小幅换步，依次使用方向姿势；取消透明叠化，左右只镜像视觉 Rig。卡面和正向斜俯视相机不变。

数据连接仍为 `InitialDemo/RouteMotion → DemoSpriteVisual.PresentMotion → DemoWalkPuppet`。逻辑层只修改 U01 移速配置；转向状态、步态和头身分时选图属于视觉表现，不阻塞移动、不旋转逻辑根节点。Editor 工具绑定两个新方向到原场景，GUID 保留，未新增包或怪物 Prefab。

最终 34/34 Edit Mode、8/8 Play Mode、Windows 构建（222722285 字节、0 错误）通过。实际相机采集：侧向 32 帧、向下 32 帧、两转角 96 帧、起停预览 66 帧；已查看相应接触图。动图在 `artifacts/animation-feedback`，详细 Player 结算结果见 QA。试玩仍为 `Builds/Windows/YesterdayConvoy.exe`。

仍有方向图间的造型跳变、裁片接缝，最终自然程度待用户试玩；未运行完整鼠标流程、第二机离线和多单位性能验证。独立部件与关节遮挡补绘仍属后续目标。

## 前次：动画优化第一阶段（2026-10-17）

用户要求把自然行走目标与分阶段方案写入项目，并先完成低成本过渡修正。目标已写入 GDD，完整计划见 [art/ANIMATION_PLAN.md](art/ANIMATION_PLAN.md)。当前仍是正向斜俯视 2.5D，保留获认可卡面和原 PNG；未新增素材、Animator、Prefab 或依赖。

U01 的纸片步态现在反复起停都按约 0.18 秒平滑进入/退出，停止保留步相逐渐回站姿；正背面约 0.16 秒淡化，去除原来最窄 28% 的压扁转身。步相仍跟随实际距离，腿部伸长限制为 4%，踝点同步限制并校正支撑行程；膝盖保持原关节弯曲方向。暂停不推进过渡，重开重置状态。路线、移速、属性和战斗结算不变。

关键连接：`InitialDemo/RouteMotion` 提供距离、方向和模拟时间；`DemoSpriteVisual.PresentMotion` 接收只读运动输入，`DemoWalkPuppet` 计算视觉子节点姿态。`InitialDemo` 仅在已有烟雾验证分支新增独立美术克隆预览；正常玩法未增加停走规则。当前死亡/穿门仍立即隐藏，起停收脚能力由该预览验证。

34/34 Edit Mode、7/7 Play Mode 通过；Windows 构建 210123301 字节、0 错误；真实 Direct3D 12 Player 四项进攻结算检查全部 True、退出 0。已查看 32 张行走/48 张转角/66 张起停实机帧的接触图与完整相机截图，GIF 在 `artifacts/animation-phase1`。试玩仍双击 `Builds/Windows/YesterdayConvoy.exe`。

限制：淡化时有短暂重影，分区接缝与纵深步态仍受源图限制；未运行完整鼠标流程、第二机或多单位性能测试。机器测试通过不等于观感获认可；第二阶段应补独立部件、关节遮挡区域和方向关键姿势，而非继续堆整图变形。

## 前次：连续关节步态与路线转向（2026-10-17）

用户否决了前版三姿势循环的观感。当前改为原图分区的纸片关节动画：髋、膝、小腿、鞋掌独立运动，支撑/抬脚/回收连续变化，手臂与尾巴跟随；头部沿用原图以减少整图换帧漂移。新增一张背向 PNG；按 RouteMotion.Direction 切换朝向，朝远处走显示背面，转角短暂变窄换面，正向俯视镜头不变。`DemoWalkPuppet` 为该角色定制，不是通用动画框架，没有新增 Unity 包或全局 skill。

最终 34/34 Edit Mode、6/6 Play Mode 通过；支撑脚位置、鞋底水平、细小时间步关节变化、回收抬脚、背向切换、转身/受击暂停与重开均已检查。Windows 构建 210121268 字节、0 错误；实际 Direct3D 12 Player 突破/穿门单次结算/死亡/重开四项 True、退出 0。32 张行走帧、48 张转角帧已查看，实机动图为 `artifacts/puppet-demo/walk-closeup.gif`、`turn-closeup.gif`；试玩入口不变。

仍是单角色的定制分区原型，关节遮挡与切面转身有简化，尚非完整多方向专业骨骼/逐帧动画。接缝、节奏等最终观感待用户试玩；人工完整鼠标操作、第二机验证未运行。社区 skill 推荐见 art/SKILL_RECOMMENDATIONS.md；四个 skill 已按后续用户指令安装至用户技能目录，下一轮对话可用，未增加 Unity 包或依赖。

## 前次：三姿势步态与镜头回正（用户否决步态观感）

InitialDemo 已改为正向斜俯视：相机左右偏航归零、棋盘横边水平，保留下俯角与立体地面。新增 `wisadel-walk-left.png` / `wisadel-walk-right.png`，与原站姿组成左右跨步、收步循环，并加轻微起伏/摆身。DemoSpriteVisual 读取 RouteMotion 实际行走距离驱动步态，暂停不换帧，重开归零；配置、移速和战斗结算不变。原 5 费卡面保持原图。

最终 34/34 Edit Mode、6/6 Play Mode 通过；Windows 构建成功、0 错误、216408485 字节；真实 Direct3D 12 Player 突破/死亡/穿门单次结算/重开全部 True、退出 0。已查看正向棋盘和 12 张连续实际相机帧，动图位于 `artifacts/walk-demo/walk-preview.gif`、`walk-closeup.gif`；试玩入口仍为 `Builds/Windows/YesterdayConvoy.exe`。截图不含 OnGUI 操作栏。

仍为三个姿势的简化走路，前后向专用帧、完整攻击/死亡动画未制作；AI 帧间存在细微造型变化。人工鼠标 UI 完整操作、第二机离线测试未运行。测试与生成提示词见 QA、art/GENERATION-2026-10-17.md。

## 前次可玩画面：5 费卡与同系列美术

2026-10-17 用户认可的蔑视表情卡图已原样保存为维什戴尔卡牌素材，CardDefinition.recruitCost=5。InitialDemo 左侧实际显示卡面/名称/费用；战场怪物、防御炮、红蓝门和石板已替换为同系列 AI 美术，保持原斜视角与进攻逻辑。新增 CardDefinition、DemoCardView、DemoSpriteVisual 与编辑器 DemoArtTools，中文字体已内置接入。保存路径/提示词/来源见 docs/art/GENERATION-2026-10-17.md。

当前 Windows 包已更新，双击 Builds/Windows/YesterdayConvoy.exe 即可试玩。34 项 Edit Mode、5 项 Play Mode、Windows 构建和图形 Player 突破/死亡/重复结算保护/重开通过；已查看包含 5 费卡的真实相机画面 artifacts/art-demo/demo-action.png。尚无购买或部署扣费，5 费已登记为招募费；动画为单帧摇摆/受击闪色，正式技能与平衡未实现。手工点击完整 UI、第二机验证未执行。

## 前次美术资源调查（历史）

用户再次确认斜俯视 2.5D。本轮完成美术 Phase 1–3 和通用资源的少量 Phase 4：实际导入并在 Unity 6000.6.5f1 验证 16 个图标、6 张粒子纹理、8 个音频和 1 个中文 OTF，共 31 个资源，放在 Assets/_Game/Art；来源、许可、SVG 源、完整需求和对比图见 [art/ART_PROGRESS.md](art/ART_PROGRESS.md)。资源库未接到现有 Demo，现有场景、Runtime、包清单和规则没有改动。暂无新角色/地图替换、AI 图、可播放效果 Prefab 或新 UI；未构建新版 Player。下一美术目标为两角色独立预览工作流，之后再扩到 P0。

## 本次可玩交付

- 直接双击 `Builds/Windows/YesterdayConvoy.exe`；或 Unity 打开 `Assets/_Game/Scenes/InitialDemo.unity`，按 Play 后点击“开始进攻”。菜单 `Yesterday Convoy/Initial Demo/Open or Create` 也能打开该场景。
- 默认 100 生命：真实 Player 中挨 4 次攻击，剩 36 生命突破蓝门，防御 8→7。滑块改为 30 后重新开始：挨 2 次攻击死亡，蓝门保持 8。参数为试验值，100 生命突破不等于整关胜利。
- 圣经/AGENTS 已按用户要求修订：前期每次交付玩家可见功能，相关逻辑、场景、镜头、最小 UI 一起做，按风险验证，简短记录后提交。
- Unity 编译与场景校验通过；34/34 Edit Mode、4/4 Play Mode 全通过；Windows 构建成功，0 构建错误；真实图形 Player 验证突破、穿门只扣一次、死亡、重开全部通过，退出 0。
- 已查看 `artifacts/initial-demo/demo-action.png`，确认斜视角、棋盘厚度、怪物、红蓝门和塔攻击可见；图片为主相机渲染，不含 OnGUI。日志和未测项见 QA 最新一节。
- 新增 `InitialDemo` 管理本次进攻与 UI，读取 U01 配置并复用 `RouteMotion` 推进 XZ 路线；按距离触发塔伤害，再结算死亡或进门；`InitialDemoTools` 保存真实场景与引用。旧 Bootstrap 与原移动测试保留。
- 尚未人工点击按钮/滑块或在第二台电脑验证。中文临时使用 Windows 字体，正式美术、字体资源和玩法平衡待定；尚无手动部署、费用、商店或羁绊。

## 已确认

- Windows 离线单机、键鼠、类似《明日方舟》的斜俯视；当前采用 2.5D 棋盘。
- 四条强制开发原则已写入 GDD 与 AGENTS。
- 原准备包五份文档已逐份 SHA256 校验后迁入 docs，保留原始清单。
- 引擎已确认沿用 `6000.6.5f1`；D01–D05 采用本次决定，D06 暂不设累计回费上限。
- 最高 3 星；商店五格、每种九张基础牌、1–5 招募费、等级 1–10。用户追问已确认编队卡槽上限＝等级，战场实体数可超过等级。
- SHOP-P01–P03 已逐项确认：采用十级概率表、未购牌刷新返库、出售 1/2/3 星返库 1/3/9 张；空费用档按剩余合法档原比例归一化，不足五张提示缺货。

## 技术选择与未决问题

- Unity `6000.6.5f1` 已获用户确认，包清单与锁文件本轮不变。
- 已有一怪一塔进攻小闭环；GDD v0.1-r4 仍未达到需要部署/技能策略实验的 v0.2 准入条件。
- 文档任务 R02 已完成三项确认同步；`proposals/SHOP-v0.1.md` 保留原路径，现记录已确认规则，尚未录入运行配置。
- 等级经济、金币数值、备战容量与各星成长待另行设计。
- 第二机验证、正式中文字体、美术与人工键鼠交互尚未进行。

## T01 历史结果

- `UnitDefinition` 包含稳定 ID、显示名、移速和穿门伤害；U01/U02/U03 三份样例已保存，数值仍为试验参数。
- 校验菜单能够定位缺失/重复 ID、空名称、非法数值；所有 Player 构建前执行同一配置检查。
- Unity Test Framework 1.8.0（工程原有包）实际运行 17 项 Edit Mode 测试，17 通过、0 失败、0 跳过。
- 重复生成后 3 个资产与 3 个 meta 的 SHA256 不变；23 个资产/目录的 meta 完整、GUID 唯一，无遗留临时测试资产。
- 本轮 Windows x64 构建成功，0 构建错误；新 Player 的 Direct3D 12 启动与五帧入口烟雾检查通过，退出 0。
- 新增程序集定义用于 Runtime/Editor/Tests 隔离；未改已有场景、BootstrapStatus 源文件/GUID、包清单或 ProjectSettings。
- 操作与接口说明见 `CONFIGURATION.md`，完整证据与限制见 QA / `verification/T01.json`。

## 工程事实与验证

- Unity：`6000.6.5f1 (3ff58d469c8a)`；当前入口：`Assets/_Game/Scenes/InitialDemo.unity`，旧 Bootstrap 仅作历史回归。
- 最终 Windows x64 构建退出 0，0 构建错误，最终无 C# 编译警告。
- T00/T01 已完成基础启动回归；T02 新 Player 在 Direct3D 12 下完整走完 15 世界单位路线并停在 (5,-1.5,0)，退出 0，恰好一条到达日志。
- T02 重复初始化不改变场景、材质及其 meta；40 个唯一 GUID、meta 完整，无遗留测试资产。
- 基线已提交：`11b4bfa`；干净 clone 重新导入、编译、场景检查通过，退出 0。
- 原准备包已在核对迁移清单与 Git 提交后删除，资料保存在 docs。
- 完整原始日志位置、检查限制、类与接口说明见 `docs/QA.md`。

## T02 历史结果

- 单只 U01 读取配置速度 1.5，6 个固定路点、总长 15，约 10 模拟秒到达。运行状态与配置分离，无碰撞或动态寻路。
- 以双精度路线距离推导位置，跨拐点不丢失本步距离，到终点后保持静止；零模拟时间不移动。
- 最终 34 项 Edit Mode（含 T01 回归、数值边界与场景非法引用）和 3 项 Play Mode 全通过；0 失败、0 跳过。
- Windows 构建成功，0 错误；实际 Player 完整移动与三张主相机画面采集通过，已查看起点/中途/终点图片核对可见路线、单位和标记。
- Player 曾发现浮点进度漂移，已复现并修复；失败日志与回归 XML 保留在 artifacts，详见 QA。
- 未改 UnitDefinition 或样例数值、包清单、ProjectSettings；入口场景 GUID 保留。使用说明见 MOVEMENT，机器可读证据见 `verification/T02.json`。
- 未人工点击 Unity/游戏 UI，截图不含 OnGUI；未在第二台电脑验证，尚无蓝门伤害/胜负/战斗测试。

## 下一步

建议下一功能为“手动点击红门部署怪物”：把自动开局改为玩家发起部署，接入费用/次数/CD 与可理解的失败提示，形成能操作的进攻选择。先试玩当前 Demo 确认视角；旧 T03/T04 中本 Demo 所需的穿门、生命和塔攻击已经合并实现，无需再单独启动这些碎片。升级经济、备战容量等后续设计仍未定案。
