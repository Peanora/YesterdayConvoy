# 已确认火雨预览的游戏接入

用户于2026-10-21回复“可以”，确认项目外10秒火雨预览：更粗的横向火浪、连续翻卷亮核/长尾、碎焰、暖色压暗和短冲击/衰减震屏。只接入这些表现，保留已批准的塔露拉人物、六秒技能/六波伤害、SP、暂停、移动锁及维维美。没有改主场景、Prefab、公共配置字段、Packages或ProjectSettings。不更新ZIP/Releases。

## 连接与范围

- `FireRainView`沿用现有控制器入口，每帧读取`ManualBattle.Clock`及当前施法实体的`FireRainStartedAt`。六排火浪从左向右分批入场，32帧主火焰与25帧碎焰分别错相；复用固定数量Renderer，不逐帧创建对象。附加小火舌、余烬和加色光晕，不调用伤害接口。
- `FireRainScreenEffect`仅由该表现组件运行时挂到主相机，使用当前工程的Built-in管线`OnRenderImage/Graphics.Blit`。单次全屏后处理负责暖色/边缘压暗、开场轻闪和像素级图像偏移；不移动或旋转相机Transform，不改变碰撞体或怪物位置。现有IMGUI HUD在相机图像后绘制，保持稳定。
- `FireRainAdditive.shader`用于主火焰、尾焰与碎焰的加色合成；`FireRainScreen.shader`用于场景色调和衰减偏移。均通过专用Resources目录显式加载以保留Player资源，不改GraphicsSettings的Always Included列表。
- 新碎焰图集为640×640、5×5，每帧128×128，Bilinear/Clamp、无mipmap、无NPOT缩放、无损RGBA；`FireRainArtTools.Prepare`只配置此新增纹理和检查着色器，不修改场景或玩法资产。重复运行保留GUID。
- `TalulahTrial`既有动作取证入口扩为180个火雨帧，按1/30模拟秒采样覆盖完整六秒。它是自动取证，不是人工键鼠测试。

暂停、2倍速和选技能的0.03倍速均通过原战斗时钟影响全部特效；无独立实时时钟或随机逐帧抖动。目标全灭仍维持到原技能窗口结束；结束、重开、切关和组件禁用时关闭火焰并将色调/偏移归零。没有新增hit-stop或延长技能逻辑锁。

项目外预览10秒包含静态对照与头尾展示，游戏不额外插入这段时间；暖色/火焰起势和收尾适配现有六秒窗口。预览与Unity使用同一套32/25帧资源，游戏使用GPU加色合成，光照/色彩空间和像素细节不会与浏览器合成逐像素相同。具体震幅、透明度、入场速度仍为可调试验值，不是原作反求数值。

## 来源与许可

1. 主火焰复用已登记的[Soluna Software / Lots of game effects](https://opengameart.org/content/lots-of-game-effects)，CC0，32帧Fire01.png；未改旧PNG及其GUID。
2. 碎焰来自[highwizard / Animated flame / Fire sprite Sheet](https://opengameart.org/content/animated-flame-fire-sprite-sheet)，具体页面明确CC0、可商业使用及无需署名。实际原下载为`https://opengameart.org/sites/default/files/firespritesheet.zip`，25张RGB黑底PNG，并非原生透明图。`tools/prepare_fire_embers.py`将黑底转为发光保持Alpha、按5×5排版，不重绘动作。输入ZIP与输出PNG的SHA256、加工步骤见[source record](source-records/fire-rain-embers.json)，通用许可文本见`licenses/CC0-1.0.txt`。仍在Credits保留作者。
3. 本轮未引入原作火雨贴图、视频提取帧、Spine运行时或新的AI图片。塔露拉原版媒体及含该媒体的实机图/视频仍只留本地，不随公共PR上传。

已检索的JT8-3参考包括[舟舟哟哟的演示](https://www.bilibili.com/video/BV1RYHYzsE2X/)；外部播放器412，未声称完整观看。实际逐帧参考为用户此前提供的第二段16.71秒视频。不能把本近似火焰称为已经取得或完整还原原版全套特效。

技术依据：[Unity OnRenderImage（仅Built-in）](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnRenderImage.html)、[Graphics.Blit](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Graphics.Blit.html)。实际版本6000.6.5f1，已在本机GPU Editor及独立Player验证，而非据文档推断成功。

## 验证和局限

本轮结果见[QA](../QA.md)：61 Edit/18 GPU Play全通过、零跳过，独立Windows构建和三组Player回归通过，完整六秒相机取证已查看。新增GPU检查比较实际渲染输出，验证后处理生效、暂停同帧冻结、自然结束/重开清除，以及相机Transform保持原值。

实际Player取证只有相机图，不包含IMGUI HUD；不冒充完整人工交互或录屏。未做第二机离线、低配/长期性能和全分辨率人工检查。源火焰分辨率有限，无真实体积模拟、热折射或新音频；视觉尚非原作逐像素还原。
