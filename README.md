# 昨日圆车

Unity 工程根目录：`E:/珍贵资料/昨日圆车`。已完成工程基线、T01 怪物配置/校验、T02 单只怪物沿固定路线移动；尚无部署、战斗或商店。

- Unity：`6000.6.5f1`（2026-10-16 用户已确认沿用）。
- 平台：Windows x64 离线单机；键鼠；类似《明日方舟》的俯视 2D。
- 入口：`Assets/_Game/Scenes/Bootstrap.unity`。
- 规则圣经：[docs/GDD.md](docs/GDD.md)；当前事实：[docs/STATUS.md](docs/STATUS.md)；证据：[docs/QA.md](docs/QA.md)。

## 打开和运行

Unity Hub → Add / 从磁盘添加项目 → 选择本目录，使用上述精确版本打开。打开入口场景并按 Play：橙色 U01 沿折线从绿色起点移动，到蓝色终点停止，下方显示进度。路线长 15 世界单位，配置移速 1.5，约 10 模拟秒完成。临时说明为英文，尚无部署/战斗；中文 UI 与字体留给后续具体任务。

菜单 `Yesterday Convoy/Baseline` 提供 Initialize、Validate、Build Windows。Initialize 在场景缺失时创建入口，已存在时只校验，避免重置人工场景内容。Windows 产物在 `Builds/Windows/YesterdayConvoy.exe`。

## 命令行验证

关闭同目录 Unity 编辑器后，在仓库根目录 PowerShell 执行（日志目录需先存在）：

```powershell
New-Item -ItemType Directory -Path artifacts -Force | Out-Null
$editorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
& $editorPath -batchmode -nographics -quit -projectPath "$PWD" -executeMethod YesterdayConvoy.Editor.BaselineTools.Validate -logFile "$PWD\artifacts\validate.log" | Out-Null
& $editorPath -batchmode -nographics -quit -projectPath "$PWD" -executeMethod YesterdayConvoy.Editor.BaselineTools.BuildWindows -logFile "$PWD\artifacts\build-windows.log" | Out-Null
& "$PWD\Builds\Windows\YesterdayConvoy.exe" -batchmode -nographics -baselineSmokeTest -logFile "$PWD\artifacts\player-smoke.log" | Out-Null
```

检查退出码及日志中的 `BASELINE_VALIDATE_PASS`、`BASELINE_BUILD_PASS`、`BASELINE_SMOKE_PASS`，不能仅以文件存在判断通过。无图形烟雾检查只验证入口绑定和运行帧，不能代替实际 Game 画面、键鼠交互或第二台电脑验证。

提交 Assets、Packages、ProjectSettings、文档与必要 `.meta`；Library、Logs、UserSettings、Builds、artifacts 等本地生成物已忽略。每个可运行功能验收后本地提交，远端推送和发布另按用户授权。

## 文档入口

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

`Bootstrap.unity` 引用正交主相机、状态组件和移动原型。`MovementPrototype` 把统一 delta 交给 `PrototypeUnit`，后者从 U01 配置创建独立 `RouteMotion` 状态并更新位置。`BaselineTools` 负责初始化、校验和构建同一入口。`-baselineSmokeTest` 仍只检查五帧启动；`-movementSmokeTest` 才运行完整路线并验证到达，见 MOVEMENT。
