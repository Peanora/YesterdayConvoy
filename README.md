# 昨日圆车

Unity 工程根目录：`E:/珍贵资料/昨日圆车`。当前可玩：一个怪物从红门出发，防御塔自动攻击，怪物突破蓝门或死亡，结果后可重新开始。

当前保留用户认可的像素感角色、臭脸防御炮和卡面；地图为铺满背景的低饱和工业场地、方格高台与红蓝线框入口，正向斜俯视角已提高，顶部显示实际防御，底部放角色卡和进攻操作。地图必须与《明日方舟》风格相近，规则见圣经第 9 节；[当前完整实机图](docs/art/previews/battle-layout-v2.png) 的最终观感待用户验收。环境选用明确 CC0 素材，具体选型与许可见 [来源调查](docs/art/ASSET_SOURCES.md)。底栏显示维什戴尔和招募费 5；购买/拖拽部署/扣费尚未实现。角色生成文件与提示词见 [生成记录](docs/art/GENERATION-2026-10-17.md)。

- Unity：`6000.6.5f1`（2026-10-16 用户已确认沿用）。
- 平台：Windows x64 离线单机；键鼠；斜俯视 2.5D 棋盘。
- 入口：`Assets/_Game/Scenes/InitialDemo.unity`；旧 Bootstrap 仅保留作移动回归。
- 规则圣经：[docs/GDD.md](docs/GDD.md)；当前事实：[docs/STATUS.md](docs/STATUS.md)；证据：[docs/QA.md](docs/QA.md)。

## 打开和运行

本仓库包含 Unity 源工程，`Builds` 试玩包和 `Library` 缓存不纳入 Git。首次从 GitHub 下载：使用 Git 克隆（或下载 ZIP 并解压），在 Unity Hub 中添加含 `Assets`、`Packages`、`ProjectSettings` 的项目根文件夹，以 **6000.6.5f1** 打开，等待导入后按下面的方法运行。美术文件使用普通 Git 保存，无需 Git LFS。

本机已有构建时可双击 `Builds/Windows/YesterdayConvoy.exe`，点击底栏“开始进攻”。当前速度已减半，默认 100 生命会被塔击败；把生命滑块调到 **140** 可突破蓝门，调到 **30** 可快速观察死亡。结束后点击“重新开始”。这里只结算这次进攻，蓝门仍有防御时不宣告整关胜利。

在编辑器运行：Unity Hub 添加并打开本工程 → 下方 Project 面板展开 `Assets/_Game/Scenes` → 双击 **InitialDemo** 场景文件 → 顶部 ▶ Play → Game 视图点击“开始进攻”。也可用菜单 `Yesterday Convoy/Initial Demo/Open or Create` 打开正确场景，再按 Play。

菜单 `Yesterday Convoy/Baseline` 提供 Initialize、Validate、Build Windows，均已接到新 Demo；使用 **Build Windows** 可在本机生成上述试玩包。初始化保留已有场景内容。当前使用像素感同系列美术与纸片关节动画；生命、塔攻击参数与布局为试验值。

## 命令行验证

关闭同目录 Unity 编辑器后，在仓库根目录 PowerShell 执行（日志目录需先存在）：

```powershell
New-Item -ItemType Directory -Path artifacts -Force | Out-Null
$editorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
& $editorPath -batchmode -nographics -quit -projectPath "$PWD" -executeMethod YesterdayConvoy.Editor.BaselineTools.Validate -logFile "$PWD\artifacts\validate.log" | Out-Null
& $editorPath -batchmode -nographics -quit -projectPath "$PWD" -executeMethod YesterdayConvoy.Editor.BaselineTools.BuildWindows -logFile "$PWD\artifacts\build-windows.log" | Out-Null
& "$PWD\Builds\Windows\YesterdayConvoy.exe" -demoSmokeTest -demoEvidenceDir "$PWD\artifacts\initial-demo" -logFile "$PWD\artifacts\player-smoke.log" | Out-Null
```

检查退出码及日志中的 `INITIAL_DEMO_SCENE_PASS`、`BASELINE_BUILD_PASS`、`INITIAL_DEMO_CHECK`（五项均 True），不能仅以文件存在判断通过。Demo Player 检查使用真实图形渲染，自动执行突破/死亡/重开；相机图片不包含 OnGUI，不能代替人工点击界面或第二台电脑验证。

提交 Assets、Packages、ProjectSettings、文档与必要 `.meta`；Library、Logs、UserSettings、Builds、artifacts 等本地生成物已忽略。每个可运行功能验收后本地提交，远端推送和发布另按用户授权。

## 文档入口

- [美术资源进度与实物验证](docs/art/ART_PROGRESS.md)、[风格路线](docs/art/ART_STYLE_GUIDE.md)、[素材清单 CSV](docs/art/ASSET_MANIFEST.csv)、[Credits](docs/art/CREDITS.md)：当前地图实际使用 12 个外部 CC0 文件，原有角色/资源库保留，清单注明各自验证范围。
- [圣经 / GDD](docs/GDD.md)：已定规则、建议、待定项以及四条强制开发原则。
- [工程决策](docs/DECISIONS.md)：用户确认与技术假设分别记录。
- [已完成建项 T00](docs/tasks/T00.md)：唯一目标及验收标准。
- [规则文档任务 R01](docs/tasks/R01.md)、[确认同步 R02](docs/tasks/R02.md)；[商店规范](docs/proposals/SHOP-v0.1.md) 的概率、返库与缺货核心规则已确认，尚未实现。
- [最小怪物配置与校验](docs/CONFIGURATION.md)：Inspector 字段、校验菜单、构建拦截、Edit Mode 测试和数据流；[T01 验收](docs/tasks/T01.md)。
- [固定路线移动演示](docs/MOVEMENT.md)：T02 场景、时间驱动、移动状态、Play Mode 与完整 Player 验证；[T02 验收](docs/tasks/T02.md)。
- [开发与协作手册](docs/DEVELOPMENT.md)：路线图、任务方法与参考；其中“尚未建项”属于原始准备阶段上下文。
- [课程准备原稿](docs/COURSE_PREPARATION.md)：保留课程资料；实际进度以 STATUS 为准。
- [迁移清单](docs/PREPARATION_MIGRATION.json)：原准备包五份文件的文件名、目标路径与原始 SHA256；GDD 与入口迁移后会有记录在 Git 中的更新。

## 入口如何连接

`InitialDemo.unity` 保存相机、棋盘、怪物、塔和红蓝门的真实对象与引用。`InitialDemo.BeginAttack` 创建单局状态，读取只读 U01 配置，`Simulate` 用同一个 delta 推进 `RouteMotion`、塔攻击和生命，终点只扣一次蓝门防御；OnGUI 按钮调用同一个 BeginAttack 重开。`InitialDemoTools` 只在编辑器生成/校验场景，`BaselineTools.BuildWindows` 构建这个入口；Editor 和测试程序集不进入 Player。
