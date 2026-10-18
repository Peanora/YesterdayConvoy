# 昨日圆车

Unity工程根目录：`E:/珍贵资料/昨日圆车`。当前可玩M01：维什戴尔从红门分批出兵，与克洛丝停步交战，技力满后手动开启爆裂黎明，击杀干员后继续穿门；防御归零胜利，进攻资源耗尽且空场失败，可重新开始。

保留认可卡面/步态和全画幅工业地图；新增克洛丝与持炮姿势，来源见[本轮生成记录](docs/art/GENERATION-2026-10-19.md)。[当前完整实机图](docs/art/previews/engagement-v1.png)。招募费5与部署费2分别显示；商店、升星、羁绊和多关经济尚未实现。

- Unity：`6000.6.5f1`（2026-10-16 用户已确认沿用）。
- 平台：Windows x64 离线单机；键鼠；斜俯视 2.5D 棋盘。
- 入口：`Assets/_Game/Scenes/InitialDemo.unity`；旧 Bootstrap 仅保留作移动回归。
- 规则圣经：[docs/GDD.md](docs/GDD.md)；当前事实：[docs/STATUS.md](docs/STATUS.md)；证据：[docs/QA.md](docs/QA.md)。

## 打开和运行

本仓库包含 Unity 源工程，`Builds` 试玩包和 `Library` 缓存不纳入 Git。首次从 GitHub 下载：使用 Git 克隆（或下载 ZIP 并解压），在 Unity Hub 中添加含 `Assets`、`Packages`、`ProjectSettings` 的项目根文件夹，以 **6000.6.5f1** 打开，等待导入后按下面的方法运行。美术文件使用普通 Git 保存，无需 Git LFS。

本机可双击`Builds/Windows/YesterdayConvoy.exe`。点击右下角色卡（或按`1`），再点击左侧红门；CD结束后继续点红门部署。底栏`01/02…`选择场上实体，技力`12/12`时点“爆裂黎明”或按`E`，开启该实体独立六发强化弹；不扣DP，不会自动施放。空格暂停/恢复，暂停可选择、恢复后再次确认；右键/Esc取消选牌。顶部切1×/2×、音效静音；失焦自动暂停。“重新开始”恢复整关状态。

试验值DP8/部署2/最多6次（费用够4次）/CD2秒/HP100/守方防御3，技力初始8/所需12/每秒1。建议CD好就连续部署四只，及时给场上实体开技能；逐只送出且不放技能会被克洛丝逐个击败。这是原型操作样例，不是正式平衡结论。

在编辑器运行：Unity Hub 添加并打开本工程 → Project 面板展开 `Assets/_Game/Scenes` → 双击 **InitialDemo** → 顶部 ▶ Play → 按上述方式选牌部署。也可用菜单 `Yesterday Convoy/Initial Demo/Open or Create` 打开正确场景。关卡只读参数位于 `Assets/_Game/Data/Battles/ManualDemo.asset`及`WisadelVsKroos.asset`；仅需修复缺失绑定时才用`Enable Wisadel vs Kroos`菜单，重复执行不会覆盖已有参数。

菜单 `Yesterday Convoy/Baseline` 提供 Initialize、Validate、Build Windows，均已接到新 Demo；使用 **Build Windows** 可在本机生成上述试玩包。初始化保留已有场景内容。当前使用像素感同系列美术与纸片关节动画；交战、技力与布局参数均为试验值。

## 命令行验证

关闭同目录 Unity 编辑器后，在仓库根目录 PowerShell 执行（日志目录需先存在）：

```powershell
New-Item -ItemType Directory -Path artifacts -Force | Out-Null
$editorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
& $editorPath -batchmode -nographics -quit -projectPath "$PWD" -executeMethod YesterdayConvoy.Editor.BaselineTools.Validate -logFile "$PWD\artifacts\validate.log" | Out-Null
& $editorPath -batchmode -nographics -quit -projectPath "$PWD" -executeMethod YesterdayConvoy.Editor.BaselineTools.BuildWindows -logFile "$PWD\artifacts\build-windows.log" | Out-Null
& "$PWD\Builds\Windows\YesterdayConvoy.exe" -combatSmokeTest -demoEvidenceDir "$PWD\artifacts\manual-demo" -logFile "$PWD\artifacts\player-smoke.log" | Out-Null
```

检查实际退出码及`ENGAGEMENT_CONFIG_PASS`、`BASELINE_BUILD_PASS`、`ENGAGEMENT_CHECK`十项全True。当前检查停步、技力/手动技能、干员死亡、继续穿门、胜负、暂停、幂等与重开；带采图参数时完整HUD的ScreenCapture需要可见窗口，必须实际查看。`-deploymentSmokeTest`是旧单向攻击九项诊断，`-demoSmokeTest`是旧单怪五项动画诊断，不代表正常入口玩法。

提交 Assets、Packages、ProjectSettings、文档与必要 `.meta`；Library、Logs、UserSettings、Builds、artifacts 等本地生成物已忽略。每个可运行功能验收后本地提交，远端推送和发布另按用户授权。

## 文档入口

- [10月20日第5周验收准备](docs/course/WEEK5_PREPARATION.md)：本次课程准备稿、真实分工和剩余清单；完整6–10关游戏为后续目标。
- [美术资源进度与实物验证](docs/art/ART_PROGRESS.md)、[风格路线](docs/art/ART_STYLE_GUIDE.md)、[素材清单 CSV](docs/art/ASSET_MANIFEST.csv)、[Credits](docs/art/CREDITS.md)：当前地图实际使用 12 个外部 CC0 文件，原有角色/资源库保留，清单注明各自验证范围。
- [圣经 / GDD](docs/GDD.md)：已定规则、建议、待定项以及四条强制开发原则。
- [待做清单与规则填写表](docs/PLANNING_CHECKLIST.md)：A/X/B/C–F与M01/G01已回填，剩余数值/角色由委托设计与后续指定；推荐未自动生效。
- [后续规则详细填写](docs/proposals/RULES_FOLLOWUP.md)：保留原提问并回填战斗/经济答案；多门/经济尚未实现，交战以STATUS为准。
- [工程决策](docs/DECISIONS.md)：用户确认与技术假设分别记录。
- [已完成建项 T00](docs/tasks/T00.md)：唯一目标及验收标准。
- [规则文档任务 R01](docs/tasks/R01.md)、[确认同步 R02](docs/tasks/R02.md)；[商店规范](docs/proposals/SHOP-v0.1.md) 的概率、返库与缺货核心规则已确认，尚未实现。
- [最小怪物配置与校验](docs/CONFIGURATION.md)：Inspector 字段、校验菜单、构建拦截、Edit Mode 测试和数据流；[T01 验收](docs/tasks/T01.md)。
- [固定路线移动演示](docs/MOVEMENT.md)：T02 场景、时间驱动、移动状态、Play Mode 与完整 Player 验证；[T02 验收](docs/tasks/T02.md)。
- [开发与协作手册](docs/DEVELOPMENT.md)：路线图、任务方法与参考；其中“尚未建项”属于原始准备阶段上下文。
- [课程准备原稿](docs/COURSE_PREPARATION.md)：保留课程资料；实际进度以 STATUS 为准。
- [迁移清单](docs/PREPARATION_MIGRATION.json)：原准备包五份文件的文件名、目标路径与原始 SHA256；GDD 与入口迁移后会有记录在 Git 中的更新。

## 入口如何连接

`ManualBattleDefinition`与`EngagementDefinition`保存只读试验配置；`ManualBattle`/`EngagementCombat`维护独立单位、干员生命/目标、SP/弹药和弹道命中。`ManualBattleController`连接输入、HUD与实体视觉；`DemoSpriteVisual`/`DemoWalkPuppet`和`EngagementView`只读取同一模拟时钟展示步态/持炮/弹道/受击/死亡。原`InitialDemo`保留场景引用和历史诊断。Editor与测试程序集不进入Player。
