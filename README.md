# 昨日圆车

Unity 工程根目录：`E:/珍贵资料/昨日圆车`。当前可玩：点击角色卡、从红门分批部署同种怪物，防御塔自动攻击；蓝门防御归零通关，资源耗尽且场上清空失败，可以重新挑战。

当前保留用户认可的像素感角色、臭脸防御炮和卡面；地图为铺满背景的低饱和工业场地、方格高台与红蓝线框入口。顶部显示战斗费用、防御、场上数量，底部角色卡可选中，显示部署费、次数与冷却。地图必须与《明日方舟》风格相近，见圣经第 9 节；[手动部署功能实机图](docs/art/previews/manual-deployment-v1.png)。环境来源与许可见 [来源调查](docs/art/ASSET_SOURCES.md)。当前卡牌招募费仍为 5，本关试验部署费为 2；商店与拖拽部署未实现。角色生成记录见 [生成记录](docs/art/GENERATION-2026-10-17.md)。

- Unity：`6000.6.5f1`（2026-10-16 用户已确认沿用）。
- 平台：Windows x64 离线单机；键鼠；斜俯视 2.5D 棋盘。
- 入口：`Assets/_Game/Scenes/InitialDemo.unity`；旧 Bootstrap 仅保留作移动回归。
- 规则圣经：[docs/GDD.md](docs/GDD.md)；当前事实：[docs/STATUS.md](docs/STATUS.md)；证据：[docs/QA.md](docs/QA.md)。

## 打开和运行

本仓库包含 Unity 源工程，`Builds` 试玩包和 `Library` 缓存不纳入 Git。首次从 GitHub 下载：使用 Git 克隆（或下载 ZIP 并解压），在 Unity Hub 中添加含 `Assets`、`Packages`、`ProjectSettings` 的项目根文件夹，以 **6000.6.5f1** 打开，等待导入后按下面的方法运行。美术文件使用普通 Git 保存，无需 Git LFS。

本机可双击 `Builds/Windows/YesterdayConvoy.exe`。**点击右下角角色卡，再点击地图左侧红门**；第一次合法部署会开始作战，也可以先点“开始作战”。卡牌保持选中，可在每次 2 秒冷却结束后继续点红门。空格/顶部按钮暂停恢复，右键/Esc 取消选牌；暂停期间可预览，恢复后需再次点击红门确认，不会自动部署。

本关试验值：初始费用 **8**，每次部署 **2**，最多 **6** 次（本关费用只够四次），蓝门防御 **3**，单只血量 **100**。费用不自然恢复。尽量紧凑地分批出怪；一只死后再送下一只会被逐个击败。防御归零显示进攻成功，场上清空且无可部署资源显示失败；点击“重新挑战”完整重置。当前不需要调生命滑块。

在编辑器运行：Unity Hub 添加并打开本工程 → Project 面板展开 `Assets/_Game/Scenes` → 双击 **InitialDemo** → 顶部 ▶ Play → 按上述方式选牌部署。也可用菜单 `Yesterday Convoy/Initial Demo/Open or Create` 打开正确场景。关卡只读参数位于 `Assets/_Game/Data/Battles/ManualDemo.asset`；仅需修复缺失绑定时才用 `Enable Manual Deployment` 菜单，重复执行不会覆盖已有参数。

菜单 `Yesterday Convoy/Baseline` 提供 Initialize、Validate、Build Windows，均已接到新 Demo；使用 **Build Windows** 可在本机生成上述试玩包。初始化保留已有场景内容。当前使用像素感同系列美术与纸片关节动画；生命、塔攻击参数与布局为试验值。

## 命令行验证

关闭同目录 Unity 编辑器后，在仓库根目录 PowerShell 执行（日志目录需先存在）：

```powershell
New-Item -ItemType Directory -Path artifacts -Force | Out-Null
$editorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
& $editorPath -batchmode -nographics -quit -projectPath "$PWD" -executeMethod YesterdayConvoy.Editor.BaselineTools.Validate -logFile "$PWD\artifacts\validate.log" | Out-Null
& $editorPath -batchmode -nographics -quit -projectPath "$PWD" -executeMethod YesterdayConvoy.Editor.BaselineTools.BuildWindows -logFile "$PWD\artifacts\build-windows.log" | Out-Null
& "$PWD\Builds\Windows\YesterdayConvoy.exe" -deploymentSmokeTest -demoEvidenceDir "$PWD\artifacts\manual-demo" -logFile "$PWD\artifacts\player-smoke.log" | Out-Null
```

检查退出码及 `MANUAL_BATTLE_CONFIG_PASS`、`BASELINE_BUILD_PASS`、`MANUAL_BATTLE_CHECK`（九项均 True），不能仅以文件存在判断通过。当前脚本执行非法部署、CD、暂停、多个实体、等待场上结算、胜利、穿门幂等、重试和失败。带采图参数时，完整 HUD 的 ScreenCapture 需要可见窗口；隐藏窗口可能是黑图，必须实际查看。`-demoSmokeTest` 仍是旧单怪动画/渲染诊断，不代表当前正常玩法；旧 Bootstrap 的移动诊断保留。

提交 Assets、Packages、ProjectSettings、文档与必要 `.meta`；Library、Logs、UserSettings、Builds、artifacts 等本地生成物已忽略。每个可运行功能验收后本地提交，远端推送和发布另按用户授权。

## 文档入口

- [美术资源进度与实物验证](docs/art/ART_PROGRESS.md)、[风格路线](docs/art/ART_STYLE_GUIDE.md)、[素材清单 CSV](docs/art/ASSET_MANIFEST.csv)、[Credits](docs/art/CREDITS.md)：当前地图实际使用 12 个外部 CC0 文件，原有角色/资源库保留，清单注明各自验证范围。
- [圣经 / GDD](docs/GDD.md)：已定规则、建议、待定项以及四条强制开发原则。
- [待做清单与规则填写表](docs/PLANNING_CHECKLIST.md)：A组已回填，其余按功能阶段补充；推荐方案未自动生效。
- [后续规则详细填写](docs/proposals/RULES_FOLLOWUP.md)：A组答案已回填，继续回答战斗边界与B01–B10经济问题；新干员交战/多门规则尚未实现。
- [工程决策](docs/DECISIONS.md)：用户确认与技术假设分别记录。
- [已完成建项 T00](docs/tasks/T00.md)：唯一目标及验收标准。
- [规则文档任务 R01](docs/tasks/R01.md)、[确认同步 R02](docs/tasks/R02.md)；[商店规范](docs/proposals/SHOP-v0.1.md) 的概率、返库与缺货核心规则已确认，尚未实现。
- [最小怪物配置与校验](docs/CONFIGURATION.md)：Inspector 字段、校验菜单、构建拦截、Edit Mode 测试和数据流；[T01 验收](docs/tasks/T01.md)。
- [固定路线移动演示](docs/MOVEMENT.md)：T02 场景、时间驱动、移动状态、Play Mode 与完整 Player 验证；[T02 验收](docs/tasks/T02.md)。
- [开发与协作手册](docs/DEVELOPMENT.md)：路线图、任务方法与参考；其中“尚未建项”属于原始准备阶段上下文。
- [课程准备原稿](docs/COURSE_PREPARATION.md)：保留课程资料；实际进度以 STATUS 为准。
- [迁移清单](docs/PREPARATION_MIGRATION.json)：原准备包五份文件的文件名、目标路径与原始 SHA256；GDD 与入口迁移后会有记录在 Git 中的更新。

## 入口如何连接

`ManualBattleDefinition` 保存只读试验值与卡牌引用，`ManualBattle` 创建独立费用/次数/CD/实体状态，复用 `RouteMotion` 移动并统一推进塔攻击和结算。`ManualBattleController` 将选牌/入口点击转换为部署请求，实例化独立视觉并绘制 HUD；`DemoSpriteVisual`/`DemoWalkPuppet` 只读取各实体运动与同一模拟时钟。原 `InitialDemo` 保存场景绑定和历史单怪诊断，普通入口由控制器接管。Editor 与测试程序集不进入 Player。
