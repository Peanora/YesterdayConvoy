# 认可卡面与同系列实机美术

2026-10-17，用户认可蔑视表情版本，明确要求保存为 **5 费卡**，按该风格生成一系列游戏内美术并替换旧 Demo。此决定覆盖上一轮“非像素优先、AI 暂不制作”的候选建议；斜俯视 2.5D 保留。费用对应已定 1–5 招募费用档，本轮保存为 CardDefinition.recruitCost=5，尚未实现购买或部署扣费。5 费本身不新增首领分类、技能或属性规则。

使用内置 **image_gen**，没有使用 CLI/API Key、外部付费素材或 Python 绘制代替生成。认可卡面从已生成文件逐字节复制，未再生成或修图；新生成 5 张独立美术。源文件保留在 Codex generated_images，项目消费副本如下。

| 用途 | 项目文件（Assets/_Game/Art 下） | 原始生成文件 | 实际尺寸 |
| --- | --- | --- | --- |
| 已认可卡牌立绘 | Cards/Portraits/wisadel-april-approved.png | exec-11a853e7-1a56-4492-b208-7acdbda7498e.png | 1254×1254 RGBA |
| 战场小人 | Characters/Monsters/Boss/wisadel-april-battle.png | exec-dfc21c65-9f44-42f2-b1da-e4df9906d816.png | 1145×1374 RGBA |
| 防御炮 | Characters/Defenders/grumpy-cannon.png | exec-3fbc514b-e918-46f1-9072-2a69993a0e5c.png | 1254×1254 RGBA |
| 红门 | Environment/Portal/rune-gate-red.png | exec-0787ef93-1919-4487-a4bb-77dca14e17ed.png | 1122×1402 RGBA |
| 蓝门 | Environment/Portal/rune-gate-blue.png | exec-b62ee09d-87f4-42d3-9bb5-497237390bff.png | 1199×1312 RGBA |
| 石板材质 | Environment/Tilesets/slate-floor.png | exec-9a21f5cc-2390-472b-9848-4cd2f741fbb7.png | 1254×1254 RGB |

全部有实际文件。前五张 Alpha 已读取确认存在透明区与有效前景；地面不透明。PPU 按不透明包围盒与目标世界高度计算，脚底 Pivot 按实际有效像素定位，不靠每个对象任意缩放补尺寸。游戏小人、炮、门为面向相机的 Sprite，棋盘仍为有厚度的 XZ 几何地面。单帧小人用模拟时间驱动摇摆和受击闪色，不是完整逐帧动画。地面提示词请求可平铺，但未单独认证其边缘完全无缝；当前格子有明确接缝。

## 来源与条件

生成平台记录为 OpenAI image_gen，条款核查 [OpenAI ROW Terms of Use](https://openai.com/policies/row-terms-of-use/)（2026-01-01 生效，2026-10-17 查阅）。输出归属条款不代替第三方角色授权。卡面/战场角色为用户指定的《明日方舟》维什戴尔愚人节形象的 AI 同人原型，**不登记为 CC0、原创独占角色或已取得正式发行授权**。炮、门、地面以认可图片为画风参照新生成，同样不擅自指定 CC0。仅本地工程与试玩包，未推送或公开发布。字体沿用 Noto Sans SC / OFL，构建随附来源说明。

## 最终生成提示词

所有新图以认可卡面 `wisadel-april-approved.png` 为显式图片参考。战场角色是身份/造型参考；其他对象仅作画风参考。战场角色、炮与门设置 transparent_background=true；地面为 false。下面是实际发送的提示词，门的模板分别以 red / blue 和对应色词展开，各调用一次。

### 战场角色

```text
Use case: stylized-concept. Reference image is the APPROVED card portrait and precise character identity/style reference, not a target to copy unchanged. Create ONE game-ready battlefield sprite of this same Wiš'adel April Fools chibi character, maintaining silver bob hair, red antenna-horns, tiny dark-red side horns, crescent ahoge, gold half-lidded sideways disdainful eyes and smug small smirk, red-black cropped tactical clothes, black shorts/boots and red-tipped demon tail. Preserve the oversized chibi head personality. Adapt her into a simplified full-body mild three-quarter battle idle facing screen-right, feet clearly separated and fully visible, small fists resting at sides; all details readable around 100-140 px on a 2.5D tactical battlefield. Refine to controlled crisp pixel clusters and limited 24-32 color shading like an enlarged 128-pixel-tall game sprite, strong dark stepped outlines, no microtexture, no glowy eyes or painterly blur. Keep the user-approved dismissive confident expression, NOT a vacant round-eyed stare. Camera sees the upright character with a slight view of the tops of boots/shoulders suitable for an oblique board, no baked diamond floor. One isolated sprite centered with 10% transparent margins on all sides, no floor shadow, no pedestal, no background, no UI/text/watermark or additional views. True transparent alpha. Do not render a card or spritesheet.
```

### 防御炮

```text
Use case: stylized-concept. The reference image supplies STYLE ONLY: humorous chibi pixel-game aesthetic with deliberate stepped dark outlines, silver-gray and charcoal base, deep crimson accents, warm amber details and restrained 2-3 shade ramps. Create ONE original game-ready enemy defense cannon tower sprite matching this visual family. Squat chunky gray stone-and-metal pedestal, oversized single dark cannon barrel aimed diagonally toward screen-left, two small half-lidded amber inspection lights on the turret give it a comically grumpy mechanical face. Tiny rivets, red armor braces, visually readable silhouette, about twice as tall as its base width. It should look like a playful dark-fantasy defensive structure rather than a realistic military weapon. Mild three-quarter view from slightly above for a 2.5D tactical board; symmetrical sturdy flat base bottom, no diamond platform. Crisp pixel sprite look at about 128 logical pixels tall, readable enlarged square clusters, controlled shading, no photorealism, no smooth 3D rendering. One centered structure entirely visible, 10% transparent margins, genuine transparent alpha background, no scenery, no text, no card frame, no floor, no cast shadow, no character or other props.
```

### 石板

```text
Use case: stylized-concept. Reference image is STYLE ONLY, not image content to put on the texture. Create ONE seamless tileable dark cool-gray stone floor texture for a humorous chibi pixel-art 2.5D tactical board. Orthographic direct TOP DOWN flat square texture with no perspective whatsoever. Large chunky worn stone slabs, dark stepped outlines and a restrained charcoal/slate/silver 8-color palette with a few small muted burgundy cracks; understated so characters and paths remain readable. Consistent pixel-art surface similar to a 64x64 or 128x128 logical pixel texture enlarged with nearest-neighbor square clusters. All four edges tile seamlessly, no edge border or bright central focal point. No icons, no characters, no faces, no objects, no writing, no lighting gradients, no transparent areas, no shadows cast by objects. Fill the complete square image with the ground texture. This is a reusable material albedo, not a map illustration or isometric tile.
```

### 红蓝门模板

红：`hue=crimson-red; key=red`；蓝：`hue=icy cyan-blue; key=blue`。

```text
Use case: stylized-concept. Reference image is STYLE ONLY, to match the humorous chibi pixel-game art family, not content to put into the object. Create ONE game-ready {hue} magical portal gate sprite for a 2.5D tactical board. Compact stout dark charcoal stone arch, chunky silver-gray bevels and blocky buttresses, paired small horn-like stone finials on top, a few restrained {hue} rune insets around the frame, big open passage filled with a softly luminous but clearly pixel-clustered {hue} magical swirl. The {key} color must be instantly recognizable. Same restrained dark-fantasy chibi aesthetic as the reference: deliberate stepped black outlines, 2-3 shade ramps, crisp enlarged pixel clusters as if designed about 128 logical pixels tall, not smooth painted illustration or photorealistic 3D. Entire gate structure in near-frontal mild three-quarter view, just slightly above so small top planes show, matching an upright sprite on an oblique board. Arch is tall and wide enough for a chibi unit to walk through, open passage is visually obvious, it must read as an entrance/exit rather than a monster or building to attack. Stable flat narrow threshold touching the bottom, no extended diamond floor platform. Camera orientation: front face visible, right side plane subtly visible. Frame is about 1.3 times taller than wide. One centered fully visible object with at least 10% transparent margins, true transparent alpha outside the gate and through any gaps; portal interior can be luminous opaque. No text, no letters, no numbers, no logos, no characters, no scenery, no cast shadow or extra objects. This is the {key} member of a matching red/blue portal pair with identical dark stone architecture.
```

生成图片有风格化和视角误差，红蓝门是同一设计语汇的两张独立图，不是逐像素相同结构。实际是否好看以当前 Player 相机画面与人类试玩为准。


## 2026-10-17：行走动作补帧

用户要求走路有动作、镜头回正。使用内置 image_gen 编辑已有战场 Sprite，保留原卡面、表情、造型与配色；生成单独的左右跨步图，不生成整张 Sprite Sheet。最终资产放在 `Assets/_Game/Art/Characters/Monsters/Boss/wisadel-walk-left.png` 和 `wisadel-walk-right.png`；许可与同人原型范围沿用上节。首轮第二张腿序重复，已弃用并重试，不算作完成素材。

左步来源：`exec-35691f07-585d-47f6-94f9-68705dcce217.png`，以原战场图为编辑目标；右步来源：`exec-33fc1d1d-b35c-4a04-81c1-2f427b304fda.png`，以左步图为编辑目标。均为 1145×1374 RGBA，项目副本保持生成 PNG 字节，仅 Unity 配置 PPU/Pivot。未采用的 `exec-de060880-1208-4312-bb4f-392cc6cdf7de.png` 留在 generated_images，不进入最终素材目录。

播放采用“左跨步→原站姿作收步→右跨步→收步”，一轮对应 0.9 世界单位；配合 0.025 单位重心起伏与 ±1.2° 轻摆。单帧不是完整骨骼动画，原站姿仅作过渡；头发/面部存在 AI 帧间细微差异，不能称为逐像素锁定。仅右向三分之四表现，前后方向专用步态暂未制作。相机左右偏航归零，保留下俯角与棋盘厚度。

左跨步实际提示词：

```text
Use case: precise-object-edit. Asset type: transparent Unity 2D walking animation pose. Input image is the edit target: existing approved game sprite. Preserve the head, face, half-lidded disdainful expression, hair, horns, colors, outline, body proportions and exact art style. Keep the same three-quarter facing RIGHT direction. Change ONLY the small body below the neck into a convincing WALKING contact pose: screen-left boot extended forward toward RIGHT, screen-right boot trailing behind to LEFT.  Arms counter-swing naturally, no weapons. Keep the giant head unchanged in size, position and expression. Whole character fully visible, transparent background, no shadow, no text, no layout panels. Match original canvas 1145x1374, original head-top and boot-floor baseline and character scale. This is one single animation frame, not a sprite sheet. Clear separated legs, small restrained stride appropriate for short chibi legs. Do not redraw the face or restyle.
```

右跨步重试实际提示词：

```text
undefined
```
