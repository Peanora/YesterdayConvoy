# 来源调查与第一版方案

核查日期：2026-10-17。仅原作者/发布方具体页面作为授权依据。原包和页面快照留在被 Git 忽略的 `artifacts/art-research/`；具体导入文件见 ASSET_MANIFEST.csv。

## 已有资源优先复用

真实工程为 Unity 6000.6.5f1，Built-in 渲染管线，代码位于 Assets/_Game。已有两个场景 InitialDemo / Bootstrap、15 份纯色材质、U01–U03 数据。一个怪物、一座塔、棋盘和红蓝门均为场景内几何体，项目创建；可以保留几何、布局、射程圈、攻击线和 HP 显示。没有 Prefab、角色 PNG、动画 Controller、音频、内置字体、卡牌/商店 UI。InitialDemo 使用 OnGUI，动态读取 Windows 字体；Packages 未安装 uGUI、2D Sprite Editor 或 Vector Graphics 包，不假设有 Tilemap/切片/矢量导入工作流。

## 地图与主角色系列

| 具体资源 / 作者 | 许可 / 成本 | 内容与动画 | 斜俯视 / 一致性 / 决策 |
| --- | --- | --- | --- |
| [Monster Builder Pack / Kenney](https://kenney.nl/assets/monster-builder-pack) | CC0 / 免费 | 实际包为身体、四肢、眼嘴等组合部件；无已拼好的完整战斗动作 | 非像素，角色轮廓可组合，但偏可爱，组装和武器动作需人工；已下载研究，未导入 |
| [Isometric Miniature Dungeon / Kenney](https://kenney.nl/assets/isometric-miniature-dungeon) | CC0 / 免费 | 实际包含 Angle/Isometric 多朝向 PNG，石墙、地面、楼梯、门、道具 | 非像素主地图候选；需要与当前镜头匹配投影、冷色改色；已下载，未作为当前地图铺入 |
| [Platformer Pack Medieval / Kenney](https://kenney.nl/assets/platformer-pack-medieval) | CC0 / 免费 | 270 项平台地图资源，实际官网预览已看 | 正侧面平台，不符合本次确认的棋盘视角；不下载整包 |
| [Platformer Art Extended Enemies / Kenney](https://kenney.nl/assets/platformer-art-extended-enemies) | CC0 / 免费 | 官网预览有史莱姆、飞虫、幽灵等及部分姿态 | 色彩与造型偏平台卡通，缺幻想职业装备体系；仅查预览，不导入 |
| [The Knight / GameArt2D](https://www.gameart2d.com/the-knight-free-sprites.html) | Freebies 为 CC0 / 免费 | 页面列出 1 骑士、7 动作、每动作 10 帧、PNG 序列与 DragonBones 源 | 非像素守军/角色模板候选；侧向、大头卡通；未下载整包，未验证与镜头兼容 |
| [The Zombies / GameArt2D](https://www.gameart2d.com/the-zombies-free-sprites.html) | Freebies 为 CC0 / 免费 | 页面列出男女两角色、各 4 动作、最多 10 帧 | 非像素怪物候选；现代服装和卡通比例需处理，不能冒充已有 6 类幻想怪；未下载 |
| [Graveyard Tileset / GameArt2D](https://www.gameart2d.com/free-graveyard-platformer-tileset.html) | Freebies 为 CC0 / 免费 | 20 地块、10 装饰，PNG/SVG/源文件 | 可组成同站非像素侧视体系，但视角与当前棋盘不符，故不采用 |
| [DungeonTileset II / 0x72](https://0x72.itch.io/dungeontileset-ii) | 图像 CC0；代码 MIT（本轮不引入代码）/ 免费自愿捐赠 | 已经官网正常免捐赠流程下载 v1.7 ZIP，370 个独立 PNG 帧/图块；README 与文件已检查 | 地牢、兽人/恶魔/骑士/精灵同系列，零预算覆盖最好；完整像素备选，未进 Unity 主资源目录 |
| [Monsters Creatures Fantasy / LuizMelo](https://luizmelo.itch.io/monsters-creatures-fantasy) | 页面 CC0 / 免费自愿捐赠 | 多种像素怪物与动作，官网图示已检查 | 更偏横版侧视，不与 0x72 直接混用；本轮未下载 |
| [Free Monster 2D Game Items / Craftpix](https://craftpix.net/freebies/free-monster-2d-game-items/) | 自定义 royalty-free；须另读具体许可 | 40 张静态搞怪怪物，AI/EPS/PNG；不是动作包 | 风格不合，无动画；不导入、不把 Free 当成 CC0 |

GameArt2D 的免费项许可依据是 [Free Assets License](https://www.gameart2d.com/license.html)，与其付费项“不得单独再分发”条款分开。本轮没有下载或引入付费资源。

0x72 美术角色映射建议：普通近战 orc_warrior、远程 goblin+弓、肉盾 masked_orc、高速 chort；精英 ogre；首领 big_demon；守军 knight_m / elf_m / dwarf_m；法系和辅助可用 orc_shaman / doc。这里只说明造型覆盖，不给这些角色擅自增加玩法。除原生 idle/run 外，攻击/死亡需补伪动画。卡面立绘未覆盖。对比图见 previews/pixel-source-samples.png。

## UI、图标、特效、音频与字体

| 具体资源 | 核查结果与决策 |
| --- | --- |
| [Fantasy UI Borders / Kenney](https://kenney.nl/assets/fantasy-ui-borders) | CC0，140 项；官网预览已检查，装饰偏多，仅留卡牌边框备选，不下载整包 |
| [UI Pack Adventure / Kenney](https://kenney.nl/assets/ui-pack-adventure) | CC0，130 项，官网预览有九宫格面板和进度条；保留中性灰蓝款参考，未导入 |
| [UI Pack RPG Expansion / Kenney](https://kenney.nl/assets/ui-pack-rpg-expansion) | CC0，85 项；年代/风格与 Adventure 不同，不因同作者就混搭；仅页面调查 |
| [Game-icons.net](https://game-icons.net/) | 已选 12 技能状态+4 羁绊占位图形；每张具体原页和作者在 CSV。SVG 源留 docs/art/source-svg，游戏 PNG 256px；CC BY 3.0，署名见 CREDITS |
| [Particle Pack / Kenney](https://kenney.nl/assets/particle-pack) | CC0；官网写 80 项，ZIP 另有旋转/黑背景变体。实际仅选 Transparent 的 slash/spark/magic/circle/star/smoke 六张；可复用于六种效果，**本轮仅纹理，不是六种已实现特效** |
| [Smoke Particles / Kenney](https://kenney.nl/assets/smoke-particles) | CC0，70 项；与粒子包用途重复，本轮不下载 |
| [Impact Sounds / Kenney](https://kenney.nl/assets/impact-sounds) | CC0，130 项；有 RPG Audio 的短金属反馈样本，暂不额外下载同用途整包 |
| [RPG Audio / Kenney](https://kenney.nl/assets/rpg-audio) | CC0，50 音效；已下载原包，仅导入刀挥动、金属点响、金币、脚步四个 OGG |
| [UI Audio / Kenney](https://kenney.nl/assets/ui-audio) | CC0，50 音效；已下载原包，仅导入点击/悬停/切换三个 OGG |
| [Dungeon Ambience / yd](https://opengameart.org/content/dungeon-ambience) | 具体页面标 CC0；暗地牢氛围背景候选，下载原始 dungeon002.ogg；适配和听感未验收，不称正式战斗主题曲 |
| [Dark Intro / Nikke](https://opengameart.org/content/dark-intro) | CC0，但页面定位含科幻/太空；不作为首选，不下载 |
| [Cartoon FX Remaster Free / Jean Moreno](https://assetstore.unity.com/packages/vfx/particles/cartoon-fx-remaster-free-109565) | 页面 Free、Standard Asset Store EULA、R1.5.1，2025-12-18；仅列 Built-in/URP 的旧 Unity 兼容表，不能冒称 6000.6.5f1 已兼容。需登录→Add to My Assets→本工程 Package Manager/My Assets 下载，人工核对导入项。未导入、不绕过登录，不公开上传包源文件 |
| [Noto Sans SC / Google Fonts](https://fonts.google.com/noto/specimen/Noto+Sans+SC)；[官方静态 SC 字重](https://github.com/notofonts/noto-cjk/tree/main/Sans/SubsetOTF/SC) | SIL OFL 1.1；选择静态 Regular OTF，避免变量字体导入兼容性假设。保留原字重名及许可证；字形样本由 Unity 验证，尚未替换 Demo 字体 |

综合方案：地图主路线保留现有几何并试用非像素石墙/装饰；角色先比较同风格近战和法系两个样张，未选定前不批量造资产。图标/字体/粒子/音效独立于最终角色画风，可先建立可复用库。未选付费项、未付款、未引入额外 Unity 包。
