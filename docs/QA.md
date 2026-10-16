# T00 验证证据

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
