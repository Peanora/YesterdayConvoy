# M02～M10 运行素材交付审计

## 2026-10-22 运行素材全部随源码交付

用户在说明旧忽略规则及未确认公开再分发许可后明确要求“所有的都上传”。本轮解除现有原作美术目录忽略，补齐新版塔露拉与源石虫的21张PNG、2份运行清单及25个已有meta（共48文件，媒体约33.3 MB），保留GUID、原尺寸和动画数据；M02～M10已由PR #12入库，不重复制作。原作角色、贴图及动画权利仍属原权利人，上传请求不替代权利人许可。以下仅本地 / Git忽略表述为历史，不再代表这些运行文件的交付状态。

接收者拉取包含本轮的版本即可取得全部12个现有测试入口的运行媒体，无需安装作者工作区、额外CombatPackages、Spine运行时或在线下载。原始模型、离线加工工具和无关预览不属于运行交付范围；缓存、构建、日志及本地AGENTS继续忽略。来源沿用塔露拉PRTS enemy_1515_bsnake与源石虫enemy_1007_slime记录，不把公开可下载当作开放许可。

日期：2026-10-22；负责人：霍利屹。范围为九个已确认单怪测试，不扩展 M01、塔露拉或正式商店/升星/羁绊。

## 本轮决定与权利记录

用户先要求仅提交可分发素材，随后在获知原作公开再分发许可未核实后，明确要求“直接上传，我们是学生作业用”。本轮按该明确请求上传运行依赖。**学生作业是用途说明，不是权利人的许可；截至本记录，没有收到可核对的授权原文或许可链接，不把这些素材标为 CC0、原创或已获公开分发授权。** 原作角色、贴图和动画权利仍属原权利人，具体原画/动画作者未确认。项目代码、程序绘制效果和第三方原作媒体的权利分别记录。

逐角色原始下载地址、源文件哈希、原许可记录和转换过程见 [完整来源记录](source-records/monster-combat-runtime.json)。来源主要为 PRTS 原作模型镜像，术师骨骼/图集来自 isHarryh/Ark-Models 镜像。镜像的软件许可、Wiki 文本许可不作为原作美术许可。该记录提供来源透明度，不宣称完成权利清理。

## 忽略规则与提交范围

检查前 `.gitignore` 的 `/Assets/StreamingAssets/MonsterCombatArt/` 和对应目录 `.meta` 规则同时排除了 PNG、清单及其 `.meta`。本轮只移除这两条规则，保留 Library、Temp、Builds、AGENTS 以及其他原作预览目录的忽略规则；没有取消整个忽略文件，也没有强行添加被忽略的其他目录。

新增运行文件位于 `Assets/StreamingAssets/MonsterCombatArt/`：56 PNG（9 卡面、47 图集页）、9 个 manifest、65 个文件 `.meta` 和10个目录 `.meta`，共140文件。所有 `.meta` 来自已运行版本，逐字节保留，不重新生成 GUID。清单中逐 PNG 的 SHA256 均与本轮文件一致。

| 任务 | 模型 | PNG（含卡面） | 动画帧数 |
| --- | --- | ---: | ---: |
| M02 猎狗 | enemy_1000_gopro | 5 | 208 |
| M03 游击队猎犬 | enemy_1077_sotihd | 5 | 212 |
| M04 高能源石虫 | enemy_1021_bslime | 7 | 330 |
| M05 酸液源石虫 | enemy_1004_mslime | 8 | 370 |
| M06 冰爆源石虫 | enemy_1067_snslime | 7 | 330 |
| M07 士兵 | enemy_1002_nsabr | 5 | 240 |
| M08 弩手 | enemy_1003_ncbow | 5 | 240 |
| M09 术师 | enemy_1011_wizard / Wizard | 7 | 302 |
| M10 重装防御者 | enemy_1006_shield | 7 | 306 |
| 合计 | | 56 | 2538 |

九只均有 idle / walk / attack / death，M03 的 walk 对应原版奔跑。固定脚底与画布、原60fps时长；卡面和战斗为同一模型，卡面不是独立高清立绘。弩箭、术法/酸液弹道、命中、爆炸预警/冲击圈/碎片、状态提示为程序绘制，未缺少待上传的特效贴图。既有地图、字体、音效和共用素材已被 Git 跟踪，许可沿用 [ASSET_LICENSES](ASSET_LICENSES.md) 与 [Credits](CREDITS.md)；本轮未替它们重新授予许可。

## 接收与构建

拉取包含本轮资产的功能分支/最终合入版本，Unity **6000.6.5f1** 打开工程，等待包解析和首次导入。无需拷贝作者的 Library、预览工作区或 CombatPackages，也无需执行本地媒体安装脚本。打开 `Assets/_Game/Scenes/InitialDemo.unity`，Play 后选择九只对应角色，拖底部卡至红门部署。

构建入口：Unity 菜单 `Yesterday Convoy → Baseline → Build Windows`；输出 `Builds/Windows/YesterdayConvoy.exe`，须保留同目录 Data 与 DLL。角色读取 `Application.streamingAssetsPath/MonsterCombatArt/<Mxx>/manifest.json` 及清单图片，不依赖本机绝对路径、在线下载或 Spine 运行时。

源码依赖 PR #11 的四只怪物与特殊能力代码，本轮分支包含该代码，并同步 main 的 PR #10 交付规范。审阅者应以本轮完整交付 PR 为准，避免重复集成相同代码；不自动合并。

## 未纳入本轮的原作文件与交接

| 范围 | 未提交原因 | 交接方式 |
| --- | --- | --- |
| 九只原始 `.skel` / `.atlas` / 原贴图、离线渲染器、逐帧源图 | 游戏只读取已打包 PNG，不需要这些开发源文件；公开分发许可仍未核实 | 来源链接与哈希已记录。需要重加工时，负责人先核对使用/共享权限，再安排获准渠道；本轮未创建公开下载或另传原文件 |
| ArcherOriginal / ApprovedMonsterArt 的旧预览、视频和截图 | 被本轮运行图集替代，不是运行依赖，许可状态未变 | 保留原本地工作区，不在公共 PR 重复发布；需要验稿由负责人协调有权使用的方式 |
| M01 / 塔露拉原作可选包 | 不在本轮九只任务内，未取得新的公开分发依据 | 继续按各自说明和许可边界处理；缺包走既有待制作/旧样板回退，不影响九只入口 |

本轮不发布 ZIP / Releases，不把学生用途写成正式商业发行许可。另一台真实电脑的人类验收仍应由接收者记录，不能用同机独立克隆代替。

## 本轮验证

验证源码：`11444fa9cc79cb7ced64ffd364db0874a3c33925`（本轮后续提交只补记录）。GitHub 该提交的清单已通过连接器读取确认。验证目录为同机独立 Git 克隆，Git 对象取自本地已推送提交；未复制忽略文件、Library、预览目录、额外素材包或旧构建。它不是第二台电脑验收。

- 开始时没有 Library / ApprovedMonsterArt / ArcherOriginal；Unity6000.6.5f1 首次解析/导入并构建成功，`BASELINE_BUILD_PASS: 327027210 bytes; 0 errors`。
- 56 PNG 与文件 meta 全部被 Git 跟踪；源文件和新 Player 的 StreamingAssets 图片逐一匹配各 manifest SHA256。75个已有 meta 保留，Unity导入后无跟踪文件变化。
- EditMode：69通过、0失败、0跳过。完整 GPU PlayMode：26通过、0失败、6跳过，跳过为4项 M01源石虫原作包与2项塔露拉原作包检查；本轮九只相关检查无跳过。
- 新构建九只独立 Player：M08、M02、M03、M04、M05、M06、M07、M09、M10 全部退出0；每只 entered / paused / attacked / resolved / three / won / noSkill / clean 均为True，M06真实叠加冻结检查为True。各只选择/暂停/行走/攻击/胜负完整HUD截图已生成；查看了菜单、弩手攻击、冰爆冻结、术师攻击画面。
- 四组旧流程 combatSmokeTest / talulahSmokeTest / interactionSmokeTest / monsterSelectionSmokeTest 全部退出0。菜单实际显示11个可玩、30个待制作。
- 仓库静态检查1093个跟踪文件通过，`git diff --check`通过。日志/截图保存在独立验证目录 artifacts/receiver-*，不随公共PR再上传原作实机画面。

接收者拉取本PR分支即可获得九只所需运行媒体；不需额外安装包。仍未执行另一台真实电脑、人类完整键鼠通关、长时低配与所有分辨率检查。
