# T02 固定路线移动演示

打开 `Assets/_Game/Scenes/Bootstrap.unity`，按 Play：橙色 U01 从绿色起点沿折线移动，到蓝色终点停止。共 6 个路点、长 15 世界单位；U01 从配置读取速度 1.5，因此约 10 模拟秒到达。蓝色方块仅为终点位置标记，尚无扣防御、销毁单位、胜负或奖励。

入口上方说明颜色含义，下方显示单位 ID、移速、路线进度和到达状态。重新开始一次演示需停止再进入 Play，尚未提供重试/暂停 UI。

## 编辑和生成

- 选择场景 `Movement Prototype`，在 Inspector 修改 Waypoints；坐标相对该对象，Play 开始时复制为世界坐标快照。
- `Prototype U01` 的 Definition 引用 `Assets/_Game/Data/Units/U01.asset`，初始化时复制速度和 ID，位置与进度保存到独立运行状态，不写回配置。
- 菜单 `Yesterday Convoy/Movement/Initialize Demo` 只在演示缺失时创建；已存在则校验并跳过，不重置路点、材质、引用或 GUID。`Baseline/Initialize` 也会补齐缺失的演示。
- 路线至少两点、至少一个非零线段，所有坐标与移速必须有限。相邻重复点允许并会跳过；不能用全部重合点表示有效路线。
- `Baseline/Validate` 重新加载并校验入口、配置引用、单一原型单位、路点及材质。正常构建脚本会执行它；Unity 的场景构建回调也校验 Bootstrap 场景。缺引用、非法路线、禁用的演示对象等会报错。

四份 `Prototype*.mat` 是 Unity 内置 Sprites/Default 着色器的纯色占位材质；几何方块没有物理碰撞器。尚未制作正式角色、Prefab、动态寻路或地图美术。

## 核心类、接口和数据流

1. `MovementPrototype` 是当前唯一模拟时间来源，在 Update 把 `Time.deltaTime` 传给单位；没有在各单位中使用独立 Update 或现实时间推进移动。
2. `PrototypeUnit.Initialize(worldWaypoints)` 读取只读 UnitDefinition 并创建 `RouteMotion`；`Simulate(delta)` 推进状态后更新 Transform。
3. `RouteMotion.Advance(delta)` 以双精度累计路线距离，再根据静态线段累计长度推导位置。拐弯不会丢弃本步剩余距离；进度被限制在总长内，到达后停住。路点在创建时复制，后续修改原数组不改变已开始的移动。
4. `MovementPrototypeTools` 保存/验证演示场景；`MovementPrototypeBuildCheck` 在 Unity 处理入口场景时复用校验。
5. `MovementSmokeCheck` 仅在 `-movementSmokeTest` 参数下启用，用于验证实际 Player 完成整条路线并保存可选截图。其 30 秒现实时间看门狗只会让测试进程失败，正常演示不启用它，不属于战斗超时判负。

数据流：场景路点快照 + UnitDefinition 初值 → RouteMotion 独立状态 → 单一 delta 驱动 → PrototypeUnit.Transform → 终点静止。当前没有向蓝门结算模块发送事件；T03 才定义穿门事件/扣防御/离场。

模拟 delta 为 0 时不移动，Play Mode 验证了 Time.timeScale=0 的情况。这只是为统一模拟时间保留正确行为，不表示战术暂停菜单或完整 T08 已实现。到达日志只在未到达→到达时输出一次，正常演示不循环生成单位。

## 自动验证

Edit Mode：`YesterdayConvoy.Editor.Tests`，包含 T01 回归及移动算法/场景非法引用测试。Play Mode：`YesterdayConvoy.PlayMode.Tests`，实际加载 Bootstrap 验证配置读取、移动、零时间停止、终点不循环和配置不被修改。

关闭同目录编辑器后可从根目录运行：

```powershell
$editorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
& $editorPath -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -assemblyNames YesterdayConvoy.Editor.Tests -testResults "$PWD\artifacts\t02-editmode-results.xml" -logFile "$PWD\artifacts\t02-editmode.log" | Out-Null
& $editorPath -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform PlayMode -assemblyNames YesterdayConvoy.PlayMode.Tests -testResults "$PWD\artifacts\t02-playmode-results.xml" -logFile "$PWD\artifacts\t02-playmode.log" | Out-Null
& $editorPath -batchmode -nographics -quit -projectPath "$PWD" -executeMethod YesterdayConvoy.Editor.BaselineTools.BuildWindows -logFile "$PWD\artifacts\t02-build-windows.log" | Out-Null
& "$PWD\Builds\Windows\YesterdayConvoy.exe" -movementSmokeTest -movementEvidenceDir "$PWD\artifacts\t02-player" -logFile "$PWD\artifacts\t02-player.log" | Out-Null
```

完整 Player 移动检查要等待实际走完路线，以 `T02_MOVEMENT_PASS` 和退出 0 为通过依据；用于采集画面的 Player 不传 `-batchmode`/`-nographics`。已有 `-baselineSmokeTest` 仍只验证五帧启动，不能代替完整移动检查。

截图 start/middle/end.png 和日志位于本地忽略目录 `artifacts/`。截图通过主相机渲染到临时 RenderTexture 后读取并写为 PNG，涵盖路线和单位，不包含 OnGUI 状态栏；每次捕获后恢复原渲染目标并释放临时纹理。QA / `verification/T02.json` 保存本次结果、测试名称和证据指纹；检查截图可证明当前占位镜头内容，不代表正式美术或完整 UI/玩家体验已验收。
