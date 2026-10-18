# 资源工作流

2026-10-18 布局更新：同一 Apply Tactical Map 工具设置试验俯角 52°、尺寸 4.9，并延展周边地面至视口外；保留固定偏航/滚转为 0。旧墙/栏杆实例禁用，用同系列原生墙体替代；禁用左侧卡牌子 Renderer。DemoCardView 的 DrawRoster 由 InitialDemo.OnGUI 在底部调用，顶部防御读取实际状态。无新增美术源图或包依赖。

验收完整布局时使用可见 Player 的 ScreenCapture（包含 OnGUI）；原 CaptureCamera 只含相机渲染，不足以验顶部/底栏。隐藏窗口下可能得到黑色 ScreenCapture，必须查看图片后才能记录视觉通过。截图仍需与鼠标滑块/开始/重开流程一起核对，不能仅以文件存在确认。

当前地图入口（2026-10-18）：`Yesterday Convoy/Initial Demo/Apply Tactical Map` → `TacticalMapTools.Apply`。读取场景原 waypoints，更新自有 Industrial Map 节点和红蓝门的 Tactical Gate 子节点，不写单位配置/移动脚本；新几何不带碰撞体。FBX 关闭动画/碰撞导入，选用共用贴图转换为当前 Built-in Standard 材质；原始文件不改写。资源位于 Art/Environment/Industrial，材质位于 Art/Materials/Industrial，来源和许可必须逐项登记。

地图存在时，“Apply Approved Art”会重新应用地图，避免把门和地面恢复成旧幻想题材；`DemoArtTools.Validate` 和构建前校验同时检查地图材质、路径与非阻挡约束。重复应用复用命名节点和已有材质，不重复创建地图。不得把静态高台/场景装饰登记为已实现部署或阻挡玩法。

当前 Demo 换装入口为 `Yesterday Convoy/Initial Demo/Apply Approved Art`（DemoArtTools.Apply）。它配置五张 Sprite 的 Point/Alpha/脚底 Pivot 与 PPU、石板材质，保存 5 费 CardDefinition，并把新表现和卡牌 Prefab 绑定到 InitialDemo；已有认可卡图保持原文件。验证入口 DemoArtTools.Validate，构建前会调用。生成记录见 GENERATION-2026-10-17.md，下文为原通用资源库流程。

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
