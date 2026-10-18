# M01克洛丝与维什戴尔持炮姿势生成记录

日期：2026-10-19（本地）。用途：用户点名的M01交战与手动爆裂黎明样板。实际工具OpenAI内置image_gen，设置transparent_background=true，以本工程`wisadel-april-battle.png`为输入参考；没有下载商业游戏原图、没有购买或修改原卡面/步态。两张源PNG原样复制，未用代码修图；Unity仅设置导入/PPU/脚底Pivot。

| 文件（Assets/_Game/Art下） | 生成源文件名 | 实际尺寸/Alpha | 当前使用与验证 |
| --- | --- | --- | --- |
| Characters/Defenders/kroos-battle.png | exec-ffd4bf1a-561e-4b96-ba88-bcad7beb9846.png | 1145×1374 RGBA，Alpha0–255，57.3%全透明 | 克洛丝常态/射击后坐力/受击闪色/死亡渐隐；Single、Point、FullRect，按可见高度约1.8世界单位导入，场景/Player实际显示 |
| Characters/Monsters/Boss/wisadel-attack.png | exec-ca05758d-fdd1-4337-a7cd-eb2c33ab90ea.png | 1144×1375 RGBA，Alpha0–255，49.8%全透明 | 有目标时持炮关键姿势；Single、Point、FullRect，约1.65世界单位；原步态不变，Player实际炮击可见 |

两图原始输出目录：`C:/Users/31016/.codex/generated_images/01a14a88-fd28-7581-81a4-106d14ef247d/`。宽高与透明度已经读取检查，源图边缘存在少量低Alpha像素、耳/武器接近边缘；未声称完美安全边距。实际尺寸不强拉伸到128px，统一世界高度/脚底定位，在主相机中缩小显示。

来源/权利：OpenAI image_gen输出，用户指定《明日方舟》克洛丝和维什戴尔AI同人原型；输出条款不等于第三方角色授权。沿用[既有生成记录](GENERATION-2026-10-17.md)与[OpenAI条款链接](https://openai.com/policies/row-terms-of-use/)，未作新的发行授权确认。**非CC0、未获得正式发行授权，公开原始资源/商业分发范围未定。** 仅当前本地/私有课程原型；未新推送或发布。

Unity验证范围：配置与纹理实际导入、引用、原图Alpha、主场景交战显示、55 Edit/14 GPU Play、构建和真实Player十项通过。已查看完整HUD实机四张图，[交战预览](previews/engagement-v1.png)。VERIFIED仅指这一范围，不代表人类已认可美感、完整动作集/原作技能或发行许可。

表现限制：克洛丝没有分层肢体和逐帧射击集；维什戴尔仅新增持炮关键姿势，加轻微程序后坐力。弹道/爆炸/部署圈/死亡消散由Unity线与色彩变化生成，没有伪称AI生成了完整SpriteSheet。下一步按用户试玩意见补角色独立的预备/发射/收势动作，不先扩全套角色。

## 实际生成提示词

以下从本次工具调用记录逐字提取，两次均引用上述本工程战场图；克洛丝只参考画风比例，维什戴尔保持身份。

### 克洛丝

```text
Use-case: stylized-concept / production game sprite. Reference is STYLE AND PROPORTION ONLY: match this existing project's chibi pixel-feel art, hard stepped black outlines, shaded pixel clusters, clear silhouette and slightly contemptuous/comical cute expression. Generate a NEW, DIFFERENT character: Kroos (original young version) from Arknights, brown-orange shoulder-length hair with bangs, two long natural brown rabbit ears, relaxed CLOSED eyes and small smile, dark olive green/black tactical hooded jacket, muted orange scarf details, black shorts and tactical boots. Full body STANDING stationary marksman, three-quarter FRONT view, aiming a compact mechanical CROSSBOW toward screen LEFT, left foot slightly ahead, readable uncut boots on shared horizontal baseline. Match reference head-to-body proportions and broad clusters, no realistic painting, keep lower body readable. Real transparent RGBA background, no floor, no shadow baked into image, no scenery, no text, no logos or watermark, no UI, no frame, only ONE complete character, no contact sheet. Weapon must be clearly a crossbow not a cannon. Leave transparent margin all around. This is a fan-art prototype for the user's Unity game. Do not reproduce the reference silver-haired character; it only anchors the visual family.
```

### 维什戴尔攻击姿势

```text
Use-case identity-preserve game sprite edit. Edit the provided Wisadel fan-art battle sprite into ONE standing ATTACK KEY POSE for the same Unity 2.5D game. Preserve exactly the same silver hairstyle, red black horns, golden half-lidded contemptuous eyes and smug smile, head/body proportions, red-black cropped tactical costume, boots, tail, pixel-cluster outlines and palette. Add a clearly visible compact but heavy dark gray and red shoulder-fired grenade launcher/large-barrel gun held with both hands, AIMING SCREEN RIGHT. Three-quarter FRONT body and face remain visible, NOT pure side profile. Weapon held at chest/shoulder height, keep entire muzzle and boots within frame, stable both feet on common baseline. No explosion or muzzle flash in this source image: they will be animated separately in-engine. It must look like the SAME exact chibi character simply raising her launcher, not redesigned or another character. Leave 8 percent transparent safe margins around all features. Real transparent RGBA; no floor/shadow/background/scenery/UI/text/watermark. One character only, no sprite sheet or duplicate poses.
```
