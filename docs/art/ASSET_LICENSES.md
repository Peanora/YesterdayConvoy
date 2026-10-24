# 授权与分发记录

## 2026-10-22 M21–M38 已确认接入，银狼排除

18位《明日方舟》敌方角色的卡面、原作离线采样图集与来源记录随源码交付。权利归原作权利人，具体原画/动画师未确认，非CC0且公开再分发许可未确认；用户上传指令不等于权利人授权。PRTS不是开放许可证明。逐个页面、实际下载地址、哈希与当前使用范围见[source-records/m21-m38-approved.json](source-records/m21-m38-approved.json)及[接入记录](M21-M38-INTEGRATION.md)。未向Unity添加Spine运行库，原模型/加工库/预览不入库。

## 2026-10-22 M11–M20 已确认运行素材

十只角色的原作派生卡面、40段基础动作与15段保留动作随源码交付。具体来源、作者未确认项、实际下载地址及源文件哈希见[source-records/m11-m20-approved.json](source-records/m11-m20-approved.json)。这些角色/纹理/动作非CC0，权利仍属原作权利人，未确认公开再分发许可；用户看稿与上传指示不登记为权利人授权。原始骨骼、项目外评估运行库、未接入预览不入库；Unity只使用PNG图集，没有引入Spine运行依赖。[接入范围](M11-M20-INTEGRATION.md)。

## 2026-10-22 交付范围修订（不变更原作权利状态）

用户明确要求现有运行素材全部上传，塔露拉 / 源石虫的运行PNG、清单和meta本轮改为Git跟踪；M02～M10已由PR #12跟踪。下方“仅本地 / 不自动上传”是此前范围，现已被明确上传请求覆盖。原作美术仍非CC0，公开再分发许可未确认，上传决定不登记为权利人授权；来源、作者归属和加工记录继续保留，见[交付审计](MONSTER-RUNTIME-DELIVERY.md)。

## 2026-10-21 源石虫本地原作图集

[PRTS源石虫模型页](https://prts.wiki/w/源石虫/spine)的enemy_1007_slime。原作贴图、角色和动作属于原权利人，非CC0，未确认开放再分发许可；用户确认本地预览/接入不等于授权公开再分发。源文件/派生PNG/卡面/实机图本地保存并Git忽略。Unity仅用PNG，不引入Spine运行时；项目外评估渲染器保留原README/LICENSE。[接入说明](ORIGINIUM-SLUG-INTEGRATION.md)、[文件哈希](source-records/originium-slug.json)。

## 2026-10-21 火雨碎焰

[highwizard / Animated flame / Fire sprite Sheet](https://opengameart.org/content/animated-flame-fire-sprite-sheet)原页明确CC0及无需署名/可商业使用；下载25帧黑底PNG，按保持发光亮度的Alpha公式转换并排为640×640派生图集。可随本项目源码和游戏分发，仍保留作者和来源。[原文件/加工哈希](source-records/fire-rain-embers.json)、[接入记录](FIRE-RAIN-INTEGRATION-2026-10-21.md)。此许可只涵盖该火焰，不涵盖原作塔露拉和含其图像的预览；本轮没有上传后者。

## 2026-10-21 塔露拉原版数据的本地派生图集

来源为PRTS公开的enemy_1515_bsnake原版模型；角色/贴图/动作属于原权利人，非CC0，未确认开放再分发许可。用户确认的是本地V4预览与接入；PNG图集、卡面、清单与实机图仅留本地，不随公共PR自动上传。离线评估渲染工具在项目外，Unity只使用导出的PNG，无Spine运行时或新包。[完整说明](TALULAH-ORIGINAL-INTEGRATION.md)。本轮不发布ZIP/Releases。

## 2026-10-20 塔露拉V2与Soluna Software特效

V2卡面/部件由内置image_gen生成，为第三方角色同人原型，非CC0；三段用户视频仅作动作参考、不导入原贴图。实际采用[Soluna Software / Lots of game effects](https://opengameart.org/content/lots-of-game-effects)的Fire01.png和ToonExplosion31.png，具体原始页面明确CC0及无需署名；仍保留作者。原PNG未改，仅Unity切图，许可依据/哈希见source-records/talulah-v2-resources.json。[详细记录](TALULAH-VIDEO-REVISION-2026-10-20.md)。


## 2026-10-20 塔露拉 AI 同人原型

两张PNG由OpenAI内置image_gen真实生成，沿用项目漫画风格；未下载商业游戏原贴图。塔露拉为《明日方舟》角色，非CC0，工具输出条件不替代第三方角色权利。来源、提示约束、路径与实际用途见 [生成记录](GENERATION-TALULAH-2026-10-20.md)。


2026-10-19清理说明：13张被新版替代的旧图已从Assets删除，清单标记REMOVED。以下生成记录、作者/许可证文字保留为来源历史；记录旧图不表示旧文件仍在工程或当前构建使用。当前漫画版的第三方角色权利范围不因清理改变，历史可从Git查阅。

## 2026-10-19克洛丝漫画重绘

kroos-comic.png由OpenAI内置image_gen两轮生成，选第二轮；以用户提供的维什戴尔漫画帧作风格参照，旧AI克洛丝作身份参照。非CC0，原角色为明日方舟；工具输出与第三方角色权利分开，未取得新的公开发行/再分发许可。本轮只作本地课程原型和本地提交，未推送/发布；完整源路径/提示词/实际使用见[GENERATION-KROOS-COMIC-2026-10-19.md](GENERATION-KROOS-COMIC-2026-10-19.md)。旧kroos-battle.png保留为历史。

## 2026-10-19用户提供的四个漫画GIF

维什戴尔本轮四个GIF由用户提供，按明确要求转成148动作/待机PNG、1静态卡面及6个表情裁片；IP/角色为明日方舟，GIF导出者未提供。不是CC0，未取得公开再分发许可，不套用Wiki文字授权。仅本地课程原型与本地提交，未推送/发布；完整源文件名、技术转换、参考链接和权利范围见[WISADEL-COMIC-2026-10-19.md](WISADEL-COMIC-2026-10-19.md)。漫画锯齿轮廓/播放逻辑为项目代码，表情像素来自用户原GIF；没有声称新生成AI图片。旧AI卡面/持炮图保留为历史资源。

## 2026-10-19 M01新增两张AI同人原型

`kroos-battle.png`与`wisadel-attack.png`由OpenAI内置image_gen以本工程认可角色画风生成，分别参考用户指定的克洛丝和维什戴尔。非CC0，未获得第三方角色正式发行授权，不把输出归属等同角色许可；未抓取商业游戏原图。具体源文件/原始提示词/实际使用/验证见[GENERATION-2026-10-19.md](GENERATION-2026-10-19.md)。本轮仅本地原型，未新推送/公开发布。基础SFX复用Kenney UI Audio click1与RPG Audio metalClick（既有CC0/许可文件不变）。

## 2026-10-18 工业战术地图

- Quaternius Modular Sci-Fi MegaKit **Standard 免费档**：内置原始许可保存为 `licenses/quaternius-modular-scifi-standard.txt`，CC0 1.0；[作者页面](https://quaternius.com/packs/modularscifimegakit.html)、[官方下载页](https://quaternius.itch.io/modular-sci-fi-megakit)。本次只引入所选 FBX 和贴图，自行转换 Built-in 材质，未使用付费 Source 引擎工程。
- ambientCG Concrete034、Asphalt012：具体资源页和[许可说明](https://docs.ambientcg.com/license/)明确 CC0，允许修改、游戏分发和原文件纳入项目，无强制署名。项目仍保留来源与 Credits；本次下载和导入的为原始 Color 文件。
- 地块布置、警戒线、管道与入口线框为项目原生制作；不是从参考游戏提取的模型或贴图。PRTS 上的原游戏截图仅用于研究，不采用 Wiki 文档许可推断游戏图片可再分发，不进入 Assets/受版本控制的预览。

本次动画反馈新增 `wisadel-side.png`、`wisadel-rear-quarter.png`，沿用既有同人原型的权利状态，未取得新的第三方授权、不登记为 CC0。实际使用、源文件及验证范围逐项见 ASSET_MANIFEST.csv，完整生成输入与提示词见 GENERATION-2026-10-17.md。

2026-10-17 更新：用户明确授权保存认可的 AI 同人卡面并用同系列美术替换本地 Demo，随后要求补走路动作。六张初始文件、两张历史步态和一张背向视图按 AI 生成来源记录，不采用 CC0 标签；其中维什戴尔相关形象保留第三方角色 IP 状态，未取得正式发行授权。通用素材原许可不变。生成工具、输入/输出及条款链接见 GENERATION-2026-10-17.md。下文首次调查的“未引入 AI 图片”为当时历史状态。

日期：2026-10-17。具体作者、页面、原始文件、修改和本地路径以 ASSET_MANIFEST.csv 为准。此表登记实际来源与条件，不保证原作者之外的第三方权利。

| 来源 | 原始许可 / 授权依据 | 修改与游戏分发 | 署名与原文件公开 |
| --- | --- | --- | --- |
| Kenney 五个已下载包 | 每个具体官网页面标 CC0；原包 License.txt 已逐个保留于 licenses/kenney-*.txt | CC0 允许复制、修改、分发；本轮不改原纹理和音频 | 无强制署名，保留 Kenney 来源；原资产可随仓库分发 |
| 0x72 DungeonTileset II v1.7 | 原作者具体页面标 Asset license CC0；页面快照留 artifacts，技术 README 留 licenses/0x72-README.txt；**README 本身没有许可文字，不单独当授权证明** | 允许修改与随游戏分发；代码 MIT 未引入 | 无强制署名；本轮只有来源帧对比图进入 docs，原 ZIP 不入 Git |
| Game-icons 选定 16 图形 | 每张原页显示作者和 CC BY 3.0；库 license.txt 留 licenses/CC-BY-3.0.txt | 允许修改与分发，需正确署名、链接许可、说明修改 | docs/art/CREDITS.md 按作者/图形列明；SVG 与 PNG 可按 CC BY 条件公开，不能移除来源记录或暗示作者背书 |
| Noto Sans SC Regular | 官方 Sans/LICENSE，SIL OFL 1.1，留 licenses/OFL-1.1.txt；Assets 字体旁另存 OFL.txt | 可内嵌游戏、随包分发；本轮为未修改静态字重 | 按 OFL 保留版权与许可；不得单独售卖字体。若后续修改/子集化需重查保留名称要求 |
| Dungeon Ambience / yd | [原始具体页面](https://opengameart.org/content/dungeon-ambience) 标 CC0 | 允许修改和随游戏分发 | 署名非强制，仍保留作者与页面；未经听感/循环接缝验收 |
| 项目已有几何与纯色材质 | 本工程 Demo 自行创建，无外部美术文件 | 继续复用 | 本轮不声称整个工程源码已采用 CC0 |

未引入：Craftpix 自定义许可项、Asset Store EULA 项、CC BY-SA/GPL 项、授权不明项、商业游戏抓取内容、付费素材、AI 图片。候选 GameArt2D 的免费许可见原站 license.html，尚未下载其原文件；与付费素材的再分发限制不可混淆。

通用许可原文：[CC0](https://creativecommons.org/publicdomain/zero/1.0/)、[CC BY 3.0](https://creativecommons.org/licenses/by/3.0/)、本地 licenses/OFL-1.1.txt。发布时把实际使用素材对应的 CREDITS 和所需许可随游戏提供；本轮未改构建器自动打包这些文件，不能把“已有文档”当成已经写入发布包。
