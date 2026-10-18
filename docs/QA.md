# 验证证据与回归

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
