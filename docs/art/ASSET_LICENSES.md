# 授权与分发记录

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
