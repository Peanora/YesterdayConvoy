# 认可卡面与同系列实机美术

## 第三阶段：灰焰漂浮样板（2026-10-17）

以既有战场图仅作画风参照，用内置 image_gen 制作独立的无腿灰焰样板。它不是新战斗卡、未配置费用/生命/技能。源文件 `exec-7f3b0bf2-f992-4eb8-83cb-464c3779abd4.png` 原样复制为 `Assets/_Game/Art/Characters/Monsters/Normal/ash-wisp.png`，1199×1312 RGBA、Alpha 0–255，Point、Single、可见高度 1.2。悬浮/呼吸/倾斜为视觉子节点的轻量动画，不套双足 Rig。

地面阴影复用已在工程内的 Kenney Particle Pack / circle_05（CC0）；原 PNG 未改，`Normal/WispSprites.asset` 保存原生 Sprite 包装，Prefab 中压成地面椭圆并着黑色。`AshWispVisual.prefab` 已接入 AnimationLab，与双足读取同一距离/方向/模拟时间。图形 Player 的 stop/pause/restart/sharedInput/secondBody 全 True。

灰焰按原创烟灵用途生成，不作商业游戏原图提取；AI 输出不自动登记为 CC0，也不作一揽子权利清理声明。部件图/头部所涉维什戴尔同人状态另按原记录保持。单视图近圆形烟灵用于全向漂浮样板，不等于已制作多方向精细动画。

实际提示词：

```text
Use case: original game asset, second body type to test a Unity 2.5D animation pipeline. Reference supplies STYLE ONLY: humorous dark-fantasy chibi, crisp stepped pixel-cluster outlines, silver-gray/charcoal with restrained crimson and warm amber highlights, simple 2-3 shade ramps. Create ONE ORIGINAL small FLOATING ASH WISP creature, no legs and no human body, distinct from the reference character. A squat pear-shaped silver-gray smoky ghost orb, comically disdainful half-lidded amber eyes, tiny jagged mouth, two tiny charcoal/red horn nubs, three tapering dark smoke tails at its bottom instead of feet, small red ember core tucked inside the body. Clear chunky readable silhouette, rounded top and wavy lower smoke edges, mischievous restrained expression, readable at 80-100 pixels tall. Front three-quarter view from slightly above, upright flat 2D sprite for the same tactical board. No accessories, no weapons, no clothing, no recognizable third-party character, no human limbs. Whole creature centered and completely visible with at least 12% transparent margins, genuine transparent alpha, NO floor, NO shadow, no scenery, no words, no labels, no card frame, single sprite not a sheet. Art should look from the same game family as the reference but be an original simple creature.
```



## 第二阶段：独立部件与可复用视觉样板（2026-10-17）

用户授权完成动画计划第二、第三阶段。第二阶段使用内置 image_gen，按现有正背面身份参照生成正/背两张部件源图；卡面和原四方向 PNG 未修改。每图经 Alpha 连通区域实际识别为 12 个独立部件，不假定 AI 网格准确。关节位置为完整不透明圆端，Unity 原生 Sprite 子资产按关节 Pivot、PPU 和静态安装角归一化，不在运行时拉伸图片补尺寸。

- `U01Layers/front-parts.png` ← `exec-d6912825-a2cc-461b-9633-a4ff3d1914a9.png`。
- `U01Layers/rear-parts.png` ← `exec-eee36f84-00d4-4d43-90f5-bb7e41dd86b1.png`。
- 两图均原样复制生成文件；准确尺寸/Alpha 见清单。Sprite Rect 裁去 torso 多余连袖和 boot shaft 多余鞋头，在成品中组合独立部件；源 PNG 保留。
- 原头部用原贴图和保存的 Sprite 几何遮罩，不重新生成表情。编辑器将 4 个方向共 52 个 Sprite 写入 `U01Layers/LayeredSprites.asset`。
- `Assets/_Game/Prefabs/Characters/U01Visual.prefab` 保存关节、四视图与引用。侧向/斜背体块复用正/背部件，仅用于短转角；不是四方向全套手绘层。
- 此批维什戴尔身体仍属于既有同人原型，原条款与第三方角色权利状态不变，不声明 CC0 或正式发行授权。PNG 源、原卡面、字体等许可状态保持。

正面部件实际提示词：

```text
Use case: game cutout animation parts atlas. Input images are approved front/back identity references. Create ONE TRANSPARENT production PARTS ATLAS, 4 equal columns x 3 equal rows, exactly TWELVE completely SEPARATE body pieces of this same tiny red/black chibi body. NO head, NO complete character, NO assembled limbs. Uniform invisible 4x3 grid, each piece centered in its own cell with at least 15% transparent padding, nothing crosses a cell boundary, no grid lines or text. Landscape 4:3 canvas, preferably 2048x1536. All pieces are FRONT/three-quarter view matching input1, crisp stepped dark pixel-cluster outlines, black/charcoal with deep crimson and bare pale skin, same outfit; NOT realistic and NOT smooth 3D. Read slots left-to-right top-to-bottom: 1 combined torso (red-black cropped open jacket, black cross-strap crop top, belly and black short shorts, NO arms/legs/head); 2 near UPPER ARM (short red-black sleeve to elbow, round shoulder cap); 3 near FOREARM (pale forearm plus black fingerless glove, elbow round cap at top, hand bottom); 4 far UPPER ARM same sleeve; 5 far FOREARM with glove; 6 near THIGH (pale upper leg with red-white thigh band, rounded skin cap at hip, rounded knee bottom); 7 near BOOT SHAFT (black boot calf from knee to ankle, red trim and small silver buckle, NO foot/toe, round closed ends); 8 near BOOT FOOT (chunky black short shoe, red sole stripe, toes pointing right, rounded closed ankle top); 9 far THIGH (pale upper leg, NO band, closed round hip/knee); 10 far BOOT SHAFT same black/red calf, no toes; 11 far BOOT FOOT same chunky shoe toes right; 12 complete black CURVED TAIL with red arrow tip, root end closed and rounded. Arms and leg pieces must have full opaque rounded overlap pads at all covered joints, draw the hidden portions too: no cutouts/holes/jagged cut edges at shoulders elbows hips knees ankles. This is replacing sliced original art, so complete volumes and joint overlap are the priority. All pieces separated on real transparent alpha, no background, shadow, labels, captions, interface or extra pieces.
```

背面部件实际提示词：

```text
Use case: cutout character rig atlas, BACK VIEW counterpart of reference parts atlas (image2), image1 is approved rear identity. Produce ONE true-transparent 4 COLUMN x 3 ROW atlas with exactly twelve isolated body components, same slot layout as image2, but rear/three-quarter-rear artwork of the red-black chibi costume. NO HEAD and no full characters. Every part inside its own equal cell with at least 15% empty padding, all centers aligned in an invisible 4x3 grid. Landscape 4:3, ideally 2048x1536. Slots left-to-right top-to-bottom: 1 standalone TORSO from neck through short shorts, back of red/black cropped jacket and exposed lower back, closed shoulders but NO arms attached; 2 near red/black UPPER ARM with round shoulder and elbow overlap caps; 3 near pale FOREARM + black fingerless glove; 4 far UPPER ARM; 5 far FOREARM + glove; 6 near pale THIGH with red-white band and closed hip/knee ends; 7 near black BOOT CALF SHAFT ONLY knee-to-ankle, red cuff, rounded ankle bottom with NO SOLE OR TOES; 8 near rear-view BOOT FOOT ONLY with closed rounded ankle top and red sole trim; 9 far pale THIGH with no band; 10 far BOOT CALF SHAFT ONLY, NO SOLE/TOES; 11 far rear-view BOOT FOOT; 12 complete curved black TAIL with red arrow tip and closed rounded root. Draw the hidden joint areas as solid full volumes so pieces overlap without gaps when rotating, no joint cutout holes. Retain dark stepped pixel-cluster outlines, simple charcoal/crimson/pale-skin ramps, tiny readable chibi equipment details and the reference design. Opaque components on transparent alpha, no shadows, no cell borders, no lettering or labels, no floating extra accessories. Each slot exactly ONE isolated component. Torso must not have hanging arms, boot shaft must not contain a second shoe.
```



## 后续修订：半速步态与实际转向姿势（2026-10-17）

用户反馈步频过快、向下跛脚、转身衔接生硬，并确认实际前进速度和腿部动作均减半。U01 速度由 1.5 改为 0.75，Stride 保持 0.64；没有放慢全局时钟或改变攻击数值。正面纵深步幅改为侧向投影的 35%，去除左右脚不同的落地高度。

用内置 image_gen 编辑既有正背面身份参考，新增两个方向，源 PNG 原样复制进项目，卡面未修改。新文件仍是单张整图，运行时分区，不是已补齐遮挡的独立部件。
- `wisadel-side.png`：`exec-299b2895-645a-4dbe-88fd-aa3e4ae8ff46.png`，右侧面，1145×1374。
- `wisadel-rear-quarter.png`：`exec-4c108e0d-3d2b-4ed8-8a10-1a84a7d0cfed.png`，右斜背面，1145×1373。
- 同目录 `Assets/_Game/Art/Characters/Monsters/Boss/`；世界显示高度 1.65、Point、Single、透明 Alpha。完整术语/第三方角色权利状态沿用本文现有同人原型说明，不声明 CC0 或正式发行授权。

转向采用约 0.38 秒的角度状态，头领先身体、脚部小幅换步，依次使用方向姿势；不再将正背面透明叠化称为转身。左右方向由各视觉 Rig 镜像，逻辑根节点不旋转。仍有方向关键图之间造型差异，观感以实际 Player 为准。

右侧面实际提示词：

```text
Use case: precise-object-edit. Input image 1 is the exact approved front character identity; input image 2 is the same character's back. Create ONE intermediate turn key pose: a strict RIGHT-FACING SIDE PROFILE (90-degree facing right), of precisely this silver-haired red-horned chibi character. Preserve the exact large-head/tiny-body proportions, half-lidded disdainful amber eye, black crop top with cross straps, tiny black shorts, red and black cropped jacket, fingerless gloves, black short boots with red trim, red thigh band, black tail with red arrow tip, curled silver forelock and paired horn ribbons. Pixel-clustered hard outlines and same restrained 2-3 shade ramp; do not make a polished smooth illustration. View is slightly from above, suitable for the same 2.5D board. Change only view and pose. Whole body straight upright, both feet resting on a common level floor baseline, knees softly bent, clearly separate both boots and both legs, no huge stride. Nose and visible eye look SCREEN RIGHT, far eye occluded by face, torso also side-on. Arms relaxed slightly separated from body; same costume, no new props. SINGLE isolated full character centered, true transparent alpha, no text, no sheet, no labels, no scenery, no ground shadow. Portrait canvas around 1145x1374 with small clear margins, character including hair and feet entirely visible. This is a frame for turning the reference character, not a different character.
```

斜背面实际提示词：

```text
Use case: precise-object-edit. Asset: ONE intermediate character TURNING pose for a Unity 2.5D game. Images 1 and 2 define the exact approved front and back identity, image 3 is the new right-side key pose. Generate this SAME character at 135-degree RIGHT-REAR THREE-QUARTER view: body facing away and toward screen RIGHT, between the strict right-side view and full back. Most of the head is silver back hair; only a very thin right cheek edge may show, no two-eye front face. Preserve head size, forelock loop, paired red horns and ribbons, silver bob hair, black-red cropped jacket, shorts, gloves, tiny boots with red trim, same thigh band and black arrow-tip tail. Tail comes out behind toward LEFT. Keep exact proportions, stepped pixel-clustered outlines and 2-3 shade ramps, same drawing style as reference sprites. Slight overhead orthographic view for an upright sprite on a tactical board, no perspective floor. Both legs readable and softly bent, a small foot pivot as if turning in place, no wide walking stride. Arms relaxed. Full body and horn ribbons entirely visible and centered with small margins, transparent alpha background, portrait near 1145x1374. One character only, no sheet, no labels, no text, no scenery, no shadow, no props. This must be a true intermediate orientation, NOT the full-back image and NOT the side image repeated. Do not alter costume or invent additional hair accessories.
```



## 后续修订：连续关节步态与背向素材（2026-10-17）

用户否决了三姿势循环的自然程度。当前 Demo 已停止播放左右跨步整图；原图和旧姿势保留在资源库，不能把旧测试通过等同于用户认可动作。新增 `Assets/_Game/Art/Characters/Monsters/Boss/wisadel-rear.png`（1145×1373 RGBA），来源 `exec-48c3fe8b-6d72-41e7-b7ea-20298b653447.png`，使用内置 image_gen，以原战场图为身份/画风参考生成背面。源 PNG 不改字节，Alpha 0–255；许可与本文件原 AI 同人范围一致。

正面原图与背面图由 Unity 网格多边形分区形成头、躯干、手臂、大腿、小腿、鞋掌和尾巴；没有声称已获得分层 PSD 或专业骨骼原文件。`DemoWalkPuppet` 用持续的支撑/回收轨迹与两段腿部 IK 控制髋/膝/踝，鞋掌单独旋转，动作跟随路线距离和模拟时钟。向远处走使用背面，左右可镜像，转角使用短暂的变窄/换面过渡；不是完整多角度转身动画。分区坐标只适合这个角色，自动测试不代替对接缝和节奏的人工观感验收。

实际背向生成提示词：

```text
Use case: identity-preserve. Asset type: rear-facing idle sprite for the SAME Unity chibi game character. Input image is character identity and exact pixel-paint style reference. Show her BACK while she walks away from the camera toward the upper right, three-quarter REAR view. Her FACE AND EYES MUST NOT BE VISIBLE. The back of her silver-gray hair covers the back of her oversized head. Preserve the exact horn/ribbon/ahoge silhouette, red-black cropped jacket, short black shorts, red/white stripe on one thigh, chunky black boots, black tail with red tip, proportions and palette. Neutral relaxed standing pose with feet a little separated, arms at sides; no crossed legs. Match reference canvas 1145x1374, overall character height and boot-floor baseline. Huge head occupies same upper 65%, small torso and legs remain in lower 35%. Entire body and tail fully visible, genuine transparent background, no floor, no shadow, no scenery, no interface, no letters, no text. This is one single rear view sprite, not a sprite sheet. Preserve small-scale readability and consistent crisp pixel-like edges.
```


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
