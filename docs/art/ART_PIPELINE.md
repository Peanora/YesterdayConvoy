# 资源工作流

当前角色样板入口为 `Yesterday Convoy/Animation/Build Second Body Sample`（FloatingVisualTools.Build，会先确保双足样板存在）。通用 Demo 换装入口 `Yesterday Convoy/Initial Demo/Apply Approved Art`（DemoArtTools.Apply）仍配置原 Sprite/石板/卡牌，并保留已有分层表现；恢复角色层次时先运行 Animation 菜单。已有认可卡图保持原文件。验证入口 DemoArtTools.Validate，构建前会调用。生成记录见 GENERATION-2026-10-17.md，下文为原通用资源库流程。

当前根目录为 `Assets/_Game/Art/`，沿用工程已有 `_Game`，不另建混杂根目录。当前只创建实际有文件的 UI/Icons、VFX/Common、Audio/SFX、Audio/BGM、Fonts；Characters、Cards、Environment 等在实际选定素材后创建。旧 Materials、Scenes、脚本 GUID 不移动。

1. 从具体原作者页面确认许可、作者、允许修改和原文件分发条件，再下载官网文件。遇到登录/许可确认按原站流程处理；不找镜像绕过。原 ZIP、页面快照、未选素材放 `artifacts/art-research`，被忽略，不整包拷入 Assets。
2. 解压只读取确定文件，跳过 `.DS_Store`、`__MACOSX`、示例工程和重复变体。保留原文件名→新文件名映射及原许可。图标 SVG 源在 `docs/art/source-svg`，不是默认可被 Unity 导入的矢量资源。
3. PNG 检查真实 Alpha、非空包围盒和源像素尺寸，缩放保持比例。当前图标使用官网白色透明 PNG，从 512×512 缩至 256×256，保留 SVG；不直接从 JPEG 抠出伪透明角色。
4. `Yesterday Convoy/Art/Configure and Validate Library`（ArtResourceTools.ConfigureAndValidate）只处理此新资源目录。图标为 Sprite Single、居中 Pivot、256 PPU/Bilinear/Clamp/无 mipmap；粒子纹理为 Default/Bilinear/透明/无 mipmap。PNG 不压缩，后续实测再调压缩与 Atlas。
5. 短 OGG 用 DecompressOnLoad、Mono；背景 OGG 用 Streaming、保留声道。验证 Unity 解码长度/采样，不把解码通过称为已试听或音量平衡通过。保留原 OGG 未转码源。
6. 字体使用官方静态 NotoSansSC-Regular.otf，附 OFL；验证简中、英文、数字和符号样本，不声称抽测覆盖全部 Unicode。现有 Demo 仍使用系统字体，接入新 UI 时才绑定此字体。
7. 每个通过导入的实际文件进入 ASSET_MANIFEST；VERIFIED 只表示所写验证范围，不意味着已连到战斗。角色、地图、效果 Prefab、卡牌立绘没做完就保持 CANDIDATE/NOT_STARTED/AI_PENDING。

## 下一阶段两角色验证约定

先选近战与法系两个外形差异明显的样张。原包已有动作优先；无完整动作时用统一画布/脚底 Pivot 的 idle、移动摇摆、攻击前冲、受击闪色、死亡渐隐。血条、阴影、技能挂点独立，部署和受击由表现接口触发。只在新建的 ArtPreview 场景/Prefab 中验证，不覆盖 InitialDemo 或改变 UnitDefinition/费用/攻击规则。

斜视角下检查贴地、不上下跳、遮挡排序、左右翻转与转弯缺视图；角色伪动画必须在游戏暂停时停止。卡面无文字，UI 以独立控件叠加费用、多个羁绊、次数和两套 CD。当前尚未创建这些 Prefab、Animator 或 UI，不能靠资源文件存在勾选整套流程完成。

批量验证需关闭同工程 Editor，再以当前锁定 Unity 执行 `-batchmode -nographics -quit -projectPath ... -executeMethod YesterdayConvoy.Editor.ArtResourceTools.ConfigureAndValidate -logFile ...`。核对退出 0 与 ART_IMPORT_PASS；导入日志和检查路径名单在 artifacts/art-research。当前没有新增第三方 Unity 包。
# 分层角色样板（2026-10-17）

`LayeredCharacterTools.Build`：在不存在时制作 `Prefabs/Characters/U01Visual.prefab`，保留已有 Prefab 人工编辑；绑定 InitialDemo，并建立 AnimationLab。两张部件 PNG 原样保留，按 Alpha 连通区域识别 12 部件，生成 `U01Layers/LayeredSprites.asset` 中 52 个原生 Sprite。头部保留原图与几何遮罩，关节端点检查实际 Alpha；PPU/Pivot/安装角保存在资产中。运行时仅动画骨架节点，不创建裁片网格或材质。

预览入口为战斗右上“动画预览”，也可打开 `Assets/_Game/Scenes/AnimationLab.unity`。四个关键姿势、停止/继续、暂停/恢复、重开用于检查姿态和接缝。采用现有两段腿部 IK 与 Transform 动画，未引入 2D Animation/Animator 依赖；只读 Sprite 资产与每实例动画状态分离。侧/斜背体块目前复用正背部件，不能标记为完整专业多方向绘制。

第三阶段：FloatingVisualTools.Build 只在不存在时创建灰焰 Prefab，复用已存在双足资源，保存第二体型引用到 Lab。`UnitVisualFrame` 为共同只读输入，`UnitVisualPresentation` 提供 Present/ResetVisual/MotionWeight；双足和漂浮分别实现 Apply，状态保留在各实例，不写 Sprite 库。暂停冻结模拟时间；停止仅停止路线推进，漂浮仍可待机。灰焰是单视图近圆形无腿样板，未制作战斗卡或改规则。原生阴影 Sprite 使用已有 Kenney CC0 纹理。源 PNG 与生成提示词/清单保持可追踪，不自动引入蒙皮包。
