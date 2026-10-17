# 美术资源 Credits

素材库首次准备于 2026-10-17；当前 Demo 已接入同系列 AI 场景美术与中文字体，其余资源是否使用以清单为准。发布时保留实际使用项的署名和许可，详情 ASSET_MANIFEST.csv。

## Game-icons.net

以下图标来自 [Game-icons.net](https://game-icons.net/)，采用 [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/)。修改：官网透明白色 PNG 从 512 缩至 256，保留 SVG；未宣称作者认可本项目。

- [crossed-swords](https://game-icons.net/1x1/lorc/crossed-swords.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-physical.png
- [magic-swirl](https://game-icons.net/1x1/lorc/magic-swirl.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-magic.png
- [checked-shield](https://game-icons.net/1x1/lorc/checked-shield.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-defense.png
- [sprint](https://game-icons.net/1x1/lorc/sprint.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-speed.png
- [snail](https://game-icons.net/1x1/lorc/snail.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-slow.png
- [star-swirl](https://game-icons.net/1x1/lorc/star-swirl.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-stun.png
- [pentagram-rose](https://game-icons.net/1x1/lorc/pentagram-rose.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-summon.png
- [two-coins](https://game-icons.net/1x1/delapouite/two-coins.html) — Delapouite；Assets/_Game/Art/UI/Icons/icon-refund.png
- [stopwatch](https://game-icons.net/1x1/lorc/stopwatch.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-cooldown.png
- [wolf-head](https://game-icons.net/1x1/lorc/wolf-head.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-synergy-beast.png
- [rune-sword](https://game-icons.net/1x1/lorc/rune-sword.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-synergy-arcane.png
- [spiked-shell](https://game-icons.net/1x1/lorc/spiked-shell.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-synergy-armored.png
- [health-normal](https://game-icons.net/1x1/sbed/health-normal.html) — sbed；Assets/_Game/Art/UI/Icons/icon-healing.png
- [round-star](https://game-icons.net/1x1/delapouite/round-star.html) — Delapouite；Assets/_Game/Art/UI/Icons/icon-star.png
- [death-skull](https://game-icons.net/1x1/sbed/death-skull.html) — sbed；Assets/_Game/Art/UI/Icons/icon-synergy-undead.png
- [heart-inside](https://game-icons.net/1x1/lorc/heart-inside.html) — Lorc；Assets/_Game/Art/UI/Icons/icon-health.png

## 其他素材

- Kenney：Particle Pack、RPG Audio、UI Audio，CC0；研究预览含 Monster Builder Pack、Isometric Miniature Dungeon。保留原包作者说明和额外贡献名单于 licenses/。
- 0x72 / Robert：[DungeonTileset II](https://0x72.itch.io/dungeontileset-ii)，CC0；本轮仅源帧对比图进入文档。
- GameArt2D：[Knight / Zombie Freebies](https://www.gameart2d.com/freebies.html)，[CC0 免费项许可](https://www.gameart2d.com/license.html)；本轮仅官网预览用于路线比较。
- yd：[Dungeon Ambience](https://opengameart.org/content/dungeon-ambience)，CC0；原始 dungeon002.ogg。
- Noto 字体版权与作者见 licenses/OFL-1.1.txt；NotoSansSC-Regular.otf 原字重未修改，SIL OFL 1.1。

## 后续接入的 AI 美术（2026-10-17）

第二阶段新增 U01Layers/front-parts.png、rear-parts.png，由同一内置 image_gen 参照既有角色补绘；52 个 Sprite、关节绑定和 Prefab 由 Unity 编辑器整理。原头部像素与卡面保留；这些身体部件继承同人原型的第三方角色权利状态，不声明 CC0。源文件与完整提示词见 GENERATION-2026-10-17.md。

用户认可的维什戴尔愚人节卡面及同风格战场角色，由 OpenAI 内置 image_gen 生成并经用户选择；角色形象为《明日方舟》相关同人原型，不作为 CC0 或已取得正式发行授权登记。防御炮、红蓝门和地面为同工具按画风参照新生成。实际文件、生成来源、最终提示词与条款链接见 GENERATION-2026-10-17.md。没有抓取商业游戏原图作为游戏内资产，没有公开发布或推送。工程自己的几何占位物不因此改为上述第三方许可。

同日新增的 `wisadel-walk-left.png`、`wisadel-walk-right.png` 由内置 image_gen 编辑既有战场图获得，用于本地 Demo 行走；来源、提示词和使用范围同上。

随后新增 `wisadel-rear.png`，同工具按既有角色生成背向视图，用于当前连续关节原型；上述两张跨步图已退为历史资源。原正面图与背向 PNG 在 Unity 中按多边形分区渲染，原文件保持不变；使用范围与来源说明同上。

本次步态反馈修正新增 `wisadel-side.png` 和 `wisadel-rear-quarter.png`，由同一内置 image_gen 以既有正背面为身份参照生成，用于侧向行走与转身中间姿势；同人原型与第三方角色权利状态不变。源文件、实际提示词、验证范围见 GENERATION-2026-10-17.md 和 ASSET_MANIFEST.csv。
