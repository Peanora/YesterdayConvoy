# 最小怪物配置（T01）

配置目录：`Assets/_Game/Data/Units/`。在 Unity Project 中选择 U01/U02/U03，即可在 Inspector 编辑。也可通过 `Assets/Create/Yesterday Convoy/Unit Definition` 创建资产。创建后填写唯一稳定 ID，再执行菜单 `Yesterday Convoy/Configuration/Validate Units`。

| 字段 | 约定 |
| --- | --- |
| Unit Id | 必填稳定 ID，区分大小写，不允许前后空白；与显示名/文件名无关。引用建立后改 ID 需要单独迁移 |
| Display Name | 必填，可修改的显示名；不用于判断是否同一种怪物 |
| Move Speed | 大于 0 的有限浮点数，单位为“世界单位/模拟秒”；拒绝 0、负值、NaN 和正负无穷 |
| Gate Damage | 大于 0 的整数，一只实体穿门时应扣除的防御值；不能默认为全员 1 |

本版配置只覆盖需要移动和穿门的原型怪物；将来若新增不移动或不造成穿门伤害的单位，应在相应任务中明确规则后扩展校验。T01 未创建路线、单位实例、HP、部署、首领技能或商店数据。

## 占位样例

| ID | 临时显示名 | 移速 | 穿门伤害 |
| --- | --- | --- | --- |
| U01 | Prototype Fast | 1.5 | 1 |
| U02 | Prototype Durable | 1 | 2 |
| U03 | Prototype Leader | 0.75 | 4 |

穿门伤害取自 GDD 8.2 的试验参数；移速是本任务新增的试验初值。名称只是占位，“Durable”“Leader”尚无耐打/首领机制，不代表正式内容已确认。

菜单 `Yesterday Convoy/Configuration/Create Prototype Units` 仅创建缺失样例，遇到已有资产会输出 `UNIT_SAMPLE_SKIP`，不覆盖人工值或重建 GUID。已有资产不合法时仍会校验失败，不能用重复生成掩盖错误。

## 校验与构建

`Validate Units` 扫描 `Assets` 下所有 `UnitDefinition`，不限制于样例目录。成功日志为 `UNIT_CONFIGURATION_PASS: N unit definitions; no errors`。失败时 Console 的 `UNIT_CONFIGURATION_FAIL` 列出路径、字段和原因；重复 ID 同时指出冲突双方。空配置集合和缺失资产也会被拒绝。校验不改数据、不自动修复、不自动生成样例。

任何常规 Unity Player 构建（含现有 Build Windows 菜单）会由 `UnitConfigurationBuildCheck` 在构建前调用同一校验；不合法时抛出 BuildFailedException 阻止构建。对已打开但尚未保存的 Inspector 修改，校验读取当前内存配置；提交前仍需保存资产。

## 类、接口和数据流

- `UnitDefinition`：ScriptableObject，保存上述四个私有序列化字段，只通过只读属性对运行时代码开放。
- `UnitConfigurationValidator.FindProjectDefinitions()`：按资产路径排序发现配置；`Validate(definitions)` 返回诊断列表，不修改配置。`UnitConfigurationIssue` 包含资产、路径、字段和消息。
- `UnitConfigurationTools`：提供生成样例与 `ValidateProject()` 菜单/命令行入口，将失败诊断转换为可读构建错误。
- `UnitConfigurationBuildCheck.OnPreprocessBuild()`：所有 Player 构建前调用 `ValidateProject()`，避免只在特定构建脚本中生效。

配置校验流为 Inspector → `.asset` → Validator → Console 或构建拦截。T02 已通过场景中的 PrototypeUnit 引用 U01，读取速度/ID 到独立 RouteMotion 状态；位置不写回配置。U01 随场景引用进入 Player，U02/U03 仍只是未被入口引用的样例；详细连接见 [MOVEMENT](MOVEMENT.md)。

Runtime、Editor、Editor.Tests 三个 asmdef 仅用于让测试引用工程代码并隔离编辑器/测试程序集，不增加框架或包依赖。现有 BootstrapStatus 脚本文件及其 GUID 保持不变，移动到 Runtime 程序集后的入口已实际回归。

## 验证入口

Unity 菜单 `Window/General/Test Runner` → EditMode → `YesterdayConvoy.Editor.Tests`。也可在关闭同目录编辑器后，在工程根目录执行：

```powershell
$editorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
New-Item -ItemType Directory -Path artifacts -Force | Out-Null
& $editorPath -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -assemblyNames YesterdayConvoy.Editor.Tests -testResults "$PWD\artifacts\t01-editmode-results.xml" -logFile "$PWD\artifacts\t01-editmode.log" | Out-Null
& $editorPath -batchmode -nographics -quit -projectPath "$PWD" -executeMethod YesterdayConvoy.Editor.UnitConfigurationTools.ValidateProject -logFile "$PWD\artifacts\t01-validate.log" | Out-Null
```

运行测试命令不额外加 `-quit`，由测试运行器完成后退出；同时检查退出码和 XML，不能把“找到 0 个测试”当作通过。命令参数核对了 [Unity 6000.6 官方测试命令行参考](https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/test-framework-introduction/reference-command-line)，并以本机实际执行为准。

测试在 `Assets/__T01Fixtures_<随机标识>` 创建并重新导入临时合法/非法资产，结束后使用 Unity AssetDatabase 删除；不要把中断遗留的测试目录提交为正式配置。当前验证没有遗留临时资产。实际证据见 QA 与 `verification/T01.json`。
