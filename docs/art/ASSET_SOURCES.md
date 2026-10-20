# 来源调查与第一版方案

## 2026-10-20 塔露拉视频反馈调查

实际调查并采用[Soluna Software / Lots of game effects](https://opengameart.org/content/lots-of-game-effects)：Fire01/ToonExplosion31，明确CC0；比单张纹理更适合连续火流。[BenHickling Animated Fire](https://opengameart.org/content/animated-fire)为CC0但偏低分辨率像素；Kenney Particle Pack项目已有、不重复下载，Smoke Particles暂不引入。商业游戏资源镜像没有提供新的再分发许可，不导入其贴图/模型。[比较、下载与使用记录](TALULAH-VIDEO-REVISION-2026-10-20.md)。


## 2026-10-19漫画角色与特效检索

本轮新增实际素材来自用户四GIF，无新增网上下载。已查BWIKI“绝对主角-维什戴尔”、蓝GAG喵皮肤慢镜展示、PRTS原角色与Kenney Particle Pack；前两者用于身份/表现研究，未提供可公开再分发原始美术授权，后者CC0但不能补齐同系列角色/表情。选择用户提供的19/静态1/28/100帧与本地裁片/原生漫画特效。具体URL、读取限制、选用理由和本地路径见[WISADEL-COMIC-2026-10-19.md](WISADEL-COMIC-2026-10-19.md)。

2026-10-18 后续布局反馈：无新下载。首版导入的两个墙与栏杆模型停用（保留引用/来源），改用原生混凝土墙体与周边服务区统一层次；当前可见外部模型为箱体、控制台、通风设备，原共用贴图继续复用。下表为首轮选型过程，当前实际使用状态以 CSV 的 Usage/Notes 与最新 ART_PROGRESS 为准。

## 2026-10-18：固定《明日方舟》相近地图方向后的重新选型

此次只选与固定方向相符的环境资源，不再保留中世纪/地牢作为地图替代。参照分析为 [PRTS 龙门商业街地图](https://prts.wiki/w/炎国_龙门商业街) 的模块高台、灰蓝工业表面、警戒边线和红蓝空间标记；原游戏图片只保存在忽略 Git 的研究目录，不作为本项目贴图、关卡布局或可分发素材。

| 具体资源与许可页 | 实际检查 | 决策 |
| --- | --- | --- |
| [Quaternius Modular Sci-Fi MegaKit](https://quaternius.com/packs/modularscifimegakit.html) / [官方 itch 页](https://quaternius.itch.io/modular-sci-fi-megakit) | 免费 Standard 包实际下载并检查内置 License_Standard.txt（CC0）；可用 FBX/OBJ/glTF。自带 Unity 工程属于 Source 付费档，不假设免费；本次未购买 | **选用**六个有效 FBX：箱体、控制台、通风件、围栏、两种围墙；仅选四张共用颜色/法线贴图，自行适配当前 Built-in 材质。排除外星生物、标志与无关模块 |
| [ambientCG Concrete034](https://ambientcg.com/view?id=Concrete034) / [Asphalt012](https://ambientcg.com/view?id=Asphalt012) / [许可](https://docs.ambientcg.com/license/) | 官方 1K-JPG 包实际下载并查看颜色图，CC0；混凝土 1024×512、沥青 1024×1024 | **选用**两张原始 Color JPG，统一蓝灰色调；其他位移/大图/工程文件不导入 |
| [Kenney City Kit Industrial](https://kenney.nl/assets/city-kit-industrial) | 官方 CC0 包与实际预览已检查，厂房/烟囱/集装箱齐全 | **不用于当前地图**：整套更偏城市经营模型、白墙和鲜色标记。只做研究比较，不能因免费直接整包铺进场景 |
| [ambientCG MetalPlates006](https://ambientcg.com/view?id=MetalPlates006) / [MetalPlates013](https://ambientcg.com/view?id=MetalPlates013) | 实看前者颜色图、后者材质预览；均明确 CC0 | **排除**：前者鱼鳞纹样、后者锈蚀/反差过重，不符合此次克制的战术地表。未导入 |
| 首次调查的 Kenney 地牢、0x72 像素地牢 | 原研究包仍保留 | **不作地图备选**，已被人类固定方向覆盖 |

实际实现：`TacticalMapTools.Apply` 保留原路线/单位/相机投影，以原生模块几何、上述合法贴图和少量选用模型构成检修场地；高台、排水沟、服务管道、边缘导向线、战术红蓝入口均是表现，不增加部署区域或阻挡规则。本轮基础资源足够，不需要 AI 生成地图图片。

来源包、授权网页与未选用预览留在 `artifacts/map-style`；只有实际使用的 12 个外部文件进入 `Assets/_Game/Art/Environment/Industrial`。清单逐项记录原文件、许可和验证范围；最终观感仍需人类与参照对照，不因 Unity 通过就声称风格验收完成。

最新：用户已选定认可的 AI 卡图为当前 Demo 画风，六张实际使用图的来源和提示词见 [GENERATION-2026-10-17.md](GENERATION-2026-10-17.md)；现有中文字体也已接入 Demo。下文保留首次调查时的候选比较，非当前选型结论。

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
