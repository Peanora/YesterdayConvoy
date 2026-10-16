# T00 验证证据

日期：2026-10-16（Asia/Shanghai）。机器：当前 Windows 开发机。引擎：6000.6.5f1 (3ff58d469c8a)。唯一验收目标是 Unity 空工程基线，未实现玩法。

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

## 后续验收记录

干净 Git 副本恢复和准备包最终删除结果将在执行后追加，不提前写通过。
