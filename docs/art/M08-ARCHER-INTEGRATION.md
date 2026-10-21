> 后续更新：本任务已迁入真实Git克隆与独立功能分支；当前入口、M05状态、媒体安装和验证以[统一记录](MONSTER_ART_PREVIEWS.md)为准。下文为原接入批次的历史说明。

# M08 弩手：已确认美术的源码侧预览入口

2026-10-21，霍利屹本轮任务。当前用户查看M08-Archer-v1后明确回复“可以，写进源码里”，据此保存这版美术并新增专用Editor预览入口。此处不将当前用户的身份写成干宸骅，不虚构单独的主导者确认、Issue、稳定UnitId或战斗参数。

## 当前交付范围

Unity中选择 `Yesterday Convoy > Art > M08 Archer Approved Preview`。可查看同一模型的卡面、待机、原地行走、射击；支持切换动作、播放/暂停和逐帧检查，同时展示放大与预计游戏尺寸。此入口位于Editor程序集，不随Player运行，也不注册可玩卡牌。

源文件：`Assets/_Game/Editor/M08ArcherArtPreview.cs`，仅新增此专用窗口。每次只保留当前帧和卡面纹理，关闭窗口释放，不更改场景、Prefab、现有动画配置、Runtime或公共接口。编辑器预览采用编辑器时间，不能当作已接入战斗模拟时钟。

原作媒体安装于 `Assets/StreamingAssets/ArcherOriginal/`，已用.gitignore排除该目录与目录.meta。含1张600×840卡面、idle/walk/attack各60张640×640透明帧、manifest.json，共182个媒体/清单文件。没有导入Spine运行库、原skel或部件贴图。帧图保留已确认V1的生成字节、固定根点(280,550)及60fps/1秒循环，不重新生成或变形。卡面保留V1排版中的“待确认”字样，这是已确认原图的历史文字，不擅自修图。

本机来源工作区：`D:/游戏开发/YesterdayConvoy-ArtPreviews/M08-Archer-v1/`。可安装包在其 `approved-package/` 子目录。经合法协调取得同一包后，在Unity关闭时执行：

```powershell
powershell -File tools/install_archer_original.ps1 -Source "D:\游戏开发\YesterdayConvoy-ArtPreviews\M08-Archer-v1\approved-package"
```

安装工具先校验固定181张PNG、清单唯一项和SHA256，再复制到唯一专用目录，不删除来源，不改其他角色。缺少媒体时预览窗口显示安装说明，不联网下载或以其他怪物冒充弩手。

## 来源和公开范围

原作普通弩手：`enemy_1003_ncbow`；参考页面 https://prts.wiki/w/弩手 与 https://prts.wiki/w/弩手/spine 。实际源文件地址前缀 `https://torappu.prts.wiki/assets/enemy_spine/enemy_1003_ncbow/`。原美术和动画作者具体姓名未确认，原权利属于《明日方舟》原权利人。公开可见不等于开放再分发，本轮确认美术也不等于取得公开许可。原作媒体及派生帧、卡面、视频仅留本地，不自动放入公共PR。

本轮可供代码审查的内容仅为专用Editor脚本及.meta、安装工具、本文、.gitignore。逐文件原素材来源/处理/校验与工具许可保存在工程外sources.json、交付说明.md及本地包manifest.json中；不修改共享资源CSV和Credits。使用本地预览包不代表已核实第三方发行许可。

## 尚未接入战斗的原因

现有CardDefinition.HasFrameAnimation要求walk/attack/skill三类帧，MonsterTestSelection据此拒绝缺技能帧的配置；DemoSpriteVisual.Bind也使用同一要求。普通弩手没有本轮批准的主动技能，不复制塔露拉技能或塞假帧规避校验。

后续战斗接入需协调：普通怪无技能接口、稳定UnitId/CardId、独立战斗参数及测试配置，再按已批准范围接入统一模拟时钟与测试目录。当前没有部署、索敌、伤害、死亡/穿门、重开或商店接入，不标记B阶段通过。待机/行走/射击已获当前用户美术确认，游戏镜头尺寸、步幅匹配和最终观感仍待实机验收。

## 基线与验证

本地仍为无.git的ZIP源码副本；开工前与原ZIP的816文件一致。不能检查Git暂存状态、同步main、创建真实功能分支或提交PR，不声称已推送。未创建或更新试玩ZIP/Releases。

验证结果见本机 `artifacts/m08-archer/`；只有实际执行成功的结果才记通过。Unity批处理执行 `YesterdayConvoy.Editor.M08ArcherArtPreview.ValidateLocalPackage` 会编译Editor程序集并逐张解码校验181张PNG；这不代替窗口人工操作或战斗测试。

本轮实际结果：安装工具在PowerShell 7执行成功，181张PNG校验值一致。对ZIP基线816文件加本轮4个新增审查文件调用仓库检查器的inspect函数，静态检查通过；这是无Git副本的显式文件列表检查，不冒充git ls-files检查。仅既有.gitignore发生预期修改，其余既有源码/场景/配置保持原字节。使用Unity 6000.6.5f1自带Roslyn及其UnityEditor/UnityEngine参考程序集独立编译本次Editor脚本，退出0，日志csharp-validation.log；这属于单文件C#编译，不等于Unity项目导入编译。

Unity批处理确实尝试启动，但许可证初始化报告0个可用entitlement、缺少com.unity.editor.headless，并在执行验证方法前以198退出。未确认完整Unity编译、PNG的Unity解码方法执行或窗口实际操作；不把启动器的返回0当作编辑器成功。详细日志unity-validation.log。Play/战斗回归和Player构建未运行，本次没有Runtime变化。
