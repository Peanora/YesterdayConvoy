# 美术工作进度

## 第二、三阶段工程目标完成（2026-10-17）

U01 独立部件/52 Sprite/Prefab/关键姿势已完成并接入战斗；第三阶段新增1199×1312透明灰焰 PNG、AshWispVisual Prefab，与双足共用 RouteMotion/UnitVisualFrame 输入，使用独立的悬浮表现。地面阴影复用 Kenney CC0 circle_05，未新下载或改原纹理；灰焰只在预览中，不计入正式战斗内容完成数。

37 Edit、11 Play、最终构建、真实 Player 预览与战斗回归通过。已查看双角色尺寸/悬浮/关键姿势，实际动图在 artifacts/stages23/two-body-preview.gif。资源条目、来源、完整提示词与 Credits 已同步；最终人工观感、完整鼠标、第二机与多单位性能未验收。阶段2提交754f758，阶段3随后独立提交，无新动画包。

以下为此前阶段记录，当前状态以上方与 STATUS/QA 为准。

## 第二阶段已实现：独立部件与视觉 Prefab（2026-10-17）

正背源图各 12 个带完整端点的部件，52 个保存的原生 Sprite（含原图四方向头部遮罩）已接入 U01Visual Prefab。图集不假定网格精确，编辑器识别 Alpha 连通区域后配置关节、PPU/Pivot；Sprite Renderer 装配尺寸不依赖任意 Transform 拉伸。原卡面保留，右向正面、0.75 速度保留。侧/斜背身体复用正背层，不作为完整专业多视图素材。

新 AnimationLab 提供四个关键姿势与起停/暂停/重开，战斗按钮可进入。36 Edit、10 Play、构建和真实 Player 通过，实际四姿势与步态已查看。素材清单/来源/Credits 同步，细节见 QA 和 ART_PIPELINE。第三阶段正继续，最终观感待用户验收；无新动画包。

最新补充（2026-10-17）：按用户要求，向右行走恢复原三分之四正面裁片及横向站位；侧面资源仅用于转角中间姿势。未新建或修改 PNG/导入/场景引用，半速与向下步态修正保留。最新验证见 STATUS/QA，以下“侧向行走使用新图”为上一轮历史状态。

## 最新：步态反馈与中间转身姿势（2026-10-17）

新增 `wisadel-side.png`（1145×1374）、`wisadel-rear-quarter.png`（1145×1373），内置 image_gen 按既有正背面生成，源 PNG 原样保留；RGBA Alpha 均为 0–255，Point/Single/脚底 Pivot/世界高度 1.65。源文件和实际提示词已追加 GENERATION-2026-10-17.md，清单、Credits、许可状态已同步，仍是同人原型，非 CC0。

新图用于侧向行走和转向中间姿势，保留原正背面与卡面。U01 移动和步频按人类确认减半；纵深步幅缩短、双脚落地一致。约 0.38 秒的转向让头先转、身体跟上、脚部换步，不再透明叠化。新增资产 VERIFIED 仅表示 Alpha/导入/引用、Play Mode、真实 Player 姿势序列验证通过，不代表观感已获人类认可。

34 Edit Mode、8 Play Mode、Windows 构建通过；已查看侧向、向下、两个转角的实机接触图。证据位于 `artifacts/animation-feedback`，详见 QA。当前仍是定制分区，图间比例细节与接缝待改；完整独立部件/遮挡补绘/专业连续转身仍未交付。

## 前次：动画优化第一阶段（2026-10-17）

自然行走目标与后续素材计划已写入 [ANIMATION_PLAN.md](ANIMATION_PLAN.md)。本次没有新增或修改图片、卡面、导入配置和许可，仍复用现有正背面图。起停逐渐进入/退出步态，转身取消压扁、改用短淡化；腿长最多伸长 4%，同步限制落点并调整支撑行程。`DemoSpriteVisual.PresentMotion` 将运动输入与定制纸片表现连接，未安装骨骼包。

34 Edit Mode、7 Play Mode 通过；Windows 构建和真实图形 Player 四项结算回归通过。已查看行走、转角、独立起停预览的实际画面，证据 `artifacts/animation-phase1`，详细过程见 QA。起停预览不是新增战斗停走规则；死亡/穿门隐藏时机不变。

观感仍待用户试玩：淡化有短暂重影、裁片接缝仍在，缺独立部件/关节遮挡补绘/完整方向关键姿势。第二阶段尚未制作；当前不提高素材清单的验证范围，也不将机器通过记为自然程度验收。

## 前次：连续关节与背向视图（2026-10-17）

前版三姿势循环被用户指出不自然，已退为历史素材。当前从原正面图与新增 `wisadel-rear.png` 形成网格分区，用髋/膝/踝关节、独立鞋掌和支撑/回收轨迹连续运动；走向远处时显示背面，转弯有快速切面过渡，卡面和正向俯视镜头保留。原 PNG 没有被覆盖或裁成不可恢复文件，没有新增付费工具或 Unity 包。生成来源与提示词见 GENERATION-2026-10-17.md。

最终 34 Edit Mode、6 Play Mode 通过；实际 Player 32 张行走帧、48 张转角帧可见连续姿势与正背面。Windows 构建 210121268 字节、0 错误，四项核心进攻结算检查全 True。背向 PNG 已通过 Alpha/导入/引用/实机检查；旧两张跨步 PNG 仅保留原导入验证，不再作自然动画交付。

局限：分区坐标为当前角色手工配置，缺少遮挡处补绘和完整侧面转身素材；不能把脚部数值测试当成观感认可。完整鼠标操作、第二机验证未运行。社区 skill 的筛选内容和未安装状态见 SKILL_RECOMMENDATIONS.md。

## 前次：左右跨步与正向俯视（2026-10-17）

新增两个真实透明步态 PNG（1145×1374），接入原战场 Sprite 的三姿势行走循环；原卡面不改。角色按路线距离换步，辅以轻微起伏/摆身，暂停冻结、重开复位；相机取消左右偏航，保留俯角与厚度。当前实际库含 39 个图像、音频和字体文件。仅此角色有该行走帧，不代表其他角色已完成动画。

两帧 Alpha 0–255，实际 Unity 导入/引用检查通过；最终 34 Edit Mode、6 Play Mode 全通过，Windows 构建 216408485 字节、0 错误。真实图形 Player 四项结算检查全 True；已查看 12 张实际相机连续画面与步态四阶段接触图，动图在 `artifacts/walk-demo`。状态清单中两帧 VERIFIED 的范围仅为本 Demo 的导入、换帧、冻结/重开与画面检查。生成来源和完整提示词见 GENERATION-2026-10-17.md。

限制：三个姿势不等于完整骨骼动画；仅右向三分之四帧，前后方向与攻击/死亡专用帧仍缺，面部/轮廓有细微帧间差异。完整鼠标 UI、第二机离线验证未运行。

## 最新：认可卡牌与实机换装（2026-10-17）

已将用户认可的蔑视表情卡图原样保存到 `Cards/Portraits/wisadel-april-approved.png`，新增 `Data/Cards/WisadelApril.asset`，CardDefinition.recruitCost=5；场景左侧 FiveCostCard Prefab 显示实际卡面、名称和费用。另实际生成并接入战场 Sprite、防御炮、红门、蓝门、石板共五张美术，目录仍为 Assets/_Game/Art。与旧素材库合计 37 张/个实际图像、音频和字体资源。

当前 InitialDemo 已替换旧几何占位的可见表现，保留斜俯视棋盘、路点、移速、试验生命与伤害。移动摇摆与受击闪色跟随模拟时间，暂停不推进；角色进门/死亡仍按原规则结算。旧网格 Renderer 被禁用以保留逻辑挂点，未删旧 Bootstrap。此前 31 个资源中的中文字体已接到 HUD/卡牌，未把其余图标、音频都自动接入。

最终验证：34/34 Edit Mode、5/5 Play Mode 通过；5 费数据、卡面/战场引用、四个 Sprite、真实受击状态下暂停冻结、突破/死亡/重开均有实际检查。Windows 构建 203809489 字节、0 错误；真实图形 Player 四项 INITIAL_DEMO_CHECK 均 True、退出 0。已查看 `artifacts/art-demo/demo-action.png`，包含真实战场与相机内卡牌/费用，未包含 OnGUI 顶部栏/底部按钮。

来源、原文件和最终提示词在 [GENERATION-2026-10-17.md](GENERATION-2026-10-17.md)。前五张透明图已读取 Alpha（0–255），地面 RGB 不透明；认可卡图与源文件字节完全一致。121 个项目资源 GUID 唯一、meta 完整。未人工点击最终 UI、未做第二机测试；单帧程序动画、试验属性和卡牌购买/部署/技能等仍有限制，未宣称 P0 全内容完成。

## 前次资源调查（历史）

日期：2026-10-17。本轮目标：检查实际工程、比较两套路线、形成第一版方案，并小量获取可通用资源。已完成 Phase 1–3，Phase 4 完成通用素材小批导入；Phase 5–7 尚未执行，不宣称 P0 美术整套完成。

## 实际结果

- 版本：Unity **6000.6.5f1 (3ff58d469c8a)**，Built-in；实际目录、场景/材质/数据、缺少的资源见 ASSET_SOURCES。保留斜俯视 2.5D，用户本轮明确回复确认；未修改镜头、玩法、场景、Prefab、Runtime、包清单或 ProjectSettings。
- 路线：推荐继续非像素、暗色冷灰幻想方向；GameArt2D Knight/Zombie 与 Kenney 地牢仅候选，免费非像素整套角色仍有缺口。0x72 是统一像素省工备选，已检查其真实 ZIP 和源帧；风格/镜头测试之前不批量导入角色/地图。实际对比图为 previews/style-comparison.png，素材样本为 previews/imported-library.png，均已查看。
- 已下载的原包：Kenney Monster Builder、Isometric Miniature Dungeon、Particle Pack、UI Audio、RPG Audio，以及官方免捐赠流程获取的 0x72 v1.7。原包与页面快照在 `artifacts/art-research`，不进 Git；未下载整站素材。
- 已放入 `Assets/_Game/Art` 并实际验证：**16 个图标 PNG、6 张粒子纹理、7 个短 OGG、1 个背景 OGG、1 个静态 OTF，共 31 个资源**。另有字体 OFL.txt、必要 meta；16 个 SVG 源保存在 docs/art/source-svg。PNG 的透明像素与非空前景均已读取 Alpha 检查。无 AI 图片、无购买。
- 资源 CSV 除 31 条实际导入文件外，还逐项追踪任务书需求；字段含具体来源、作者、许可依据、署名/修改/分发条件、实际路径、原文件名、使用位置、状态。未实现项没有假路径，未获取项没有勾完成。

## 当前 Unity 验证

命令：锁定 Unity 执行 `YesterdayConvoy.Editor.ArtResourceTools.ConfigureAndValidate`，最终退出 **0**。日志：`artifacts/art-research/unity-import-final.log`；逐文件成功名单：`unity-verified-paths.txt`。

原始结果：`ART_IMPORT_PASS: icons=16; textures=6; audio=8; fonts=1; total=31.`

- 图标：真实 Sprite Single、256×256、256 PPU、Bilinear、Clamp、无 mipmap、Alpha；粒子纹理正常载入。
- 音频：8 个文件实际解码，短音效约 0.094–0.846 秒；Dungeon Ambience 为 **206 秒、双声道**，采用 Streaming。尚未试听或核对循环接缝，不能据此宣称音频体验已验收。
- 字体：静态 NotoSansSC-Regular 成功导入，简中/英文字母/数字/标点/★→ 样本 HasCharacter 全通过。此为抽测；尚未把字体绑定 Demo，也未验证独立 Player 中文 UI。
- 没有 C# 编译错误/警告；Assets/_Game 的 96 个 GUID 唯一、meta 无缺失，CSV 的 31 个资源逐个匹配 Unity 验证路径和本地许可文件。另有 161 条需求、6 条原包和 3 条文档对比图记录，CSV 共 201 条。没有玩法变更，本轮未重复运行 Edit/Play Mode 全套、未构建新 Player。以前的 Demo 测试不计作本轮美术验收。

VERIFIED 的范围仅为上述导入/文件验证。31 条资源的 Usage 明确为“资源库，未绑定到玩法”，它们并不代表已有 16 个技能、6 个特效 Prefab 或音频播放接口。

## 缺口、风险与下一步

缺口：正式同系列战斗角色、卡牌立绘/框架、地图实际投影、3 守军、状态挂点、部署/受击/死亡表现、六种可播放特效、可编辑战斗/商店/详情 UI、音效事件接入、正式战斗配乐。已有几何/OnGUI 可以复用，但不算正式资源完成。

优先下一目标：**两个不同轮廓角色的可运行美术工作流**。先用近战/法系样张在独立 ArtPreview 场景中验证卡面→Sprite→待机/移动/攻击/受击/死亡→血条/阴影/部署和攻击反馈，镜头保持当前斜俯视。验证成功再扩到 P0。若非像素角色补齐工时过高，再整套选择 0x72；不要边做边无规划混搭。

AI_PENDING 提供两种原创概念的中英文卡面/Sprite 提示词，均为未生成的条件性缺口；本轮不调用生成工具凑数量。完整待办见 ASSET_CHECKLIST。Asset Store 项仅记录人工登录与许可导入步骤，没有绕过验证。

## 关键接口与数据流

本轮只新增编辑器类 **ArtResourceTools**：官网原文件→筛选/重命名→独立 Art 目录→Texture/Audio/Font Importer→逐个 LoadAsset 验证→CSV 状态与 Credits。`ConfigureAndValidate` 不打开或保存任何场景，不接战斗事件。已有 InitialDemo / RouteMotion / UnitDefinition 接口与数据流保持原样；下一阶段用独立表现组件连接已有模拟状态。
