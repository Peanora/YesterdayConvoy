# M25 梅菲斯特：原作治疗特效预览 V1

2026-11-05 · 负责人：霍利屹 · [任务 #70](https://github.com/Peanora/YesterdayConvoy/issues/70)。用户要求继续按原版移植制作，沿用先看稿、确认后接入。本轮基于 main `a37f0e50565fd8b3928110ce48d2b44a327e04ec`，不依赖尚未合并的曼德拉 PR #69。

## 实际交付

工程外目录：`YesterdayConvoy-ArtPreviews/Mephisto-2026-11-05/V1/`（仓库旁的本地工作区，不随源码克隆）。

- `梅菲斯特_原作特效预览_V1.html`：Chrome / Edge 交互看稿，逐片段、0.5×慢放、暂停、拖动、事件定位、放大、镜像与同步缩小对照。
- `梅菲斯特_原作战斗特效预览_V1.mp4`：35.5秒、1280×720、30fps、1065帧，包含单独的反向片段。缩小窗口只近似游戏尺寸，未用正式游戏相机测量。
- `预览说明.md`：逐动作覆盖、原始绑定、媒体与工具许可区别、适配和限制。
- `reference/`：固定版本索引、原始包核验、骨骼事件、挂点及来源记录；`verification.json` / `preview-verification.json`：本地验证结果。加工脚本与原包均在工程外。

## 原作映射和已完成范围

使用人形 `enemy_1507_mephi`，不混用歌者 `enemy_1514_smephi`。原角色 prefab `battle/enm_pfb_17.ab` 的 Heal 组件直接绑定 `mephi_attack_01_start` / `mephi_attack_01_hit`；HealAura 直接绑定 `mephi_buff_01_start`。三个效果均位于 `battle/prefabs/effects/mephi.ab`，本次使用原生粒子、材质、纹理和动画曲线，未新绘替代。

| 片段 | 已完成表现 | 时点与适配 |
| --- | --- | --- |
| 三目标治疗 | 原完整施法、手部烟羽、三个宿主脚底光环与上升亮点 | Attack 全长3.1666667秒，OnAttack在1.1000000秒；原延时字段1.1秒由看稿显式驱动 |
| 反向检查 | 人物及特效一起镜像，字幕正常 | 检查左右观感，未验证正式游戏转向 |
| 手部细节 | 原施法特效跟随R_Hand逐帧位置 | 原FollowMountPoint=6，与Special1对应 |
| 持续回复效果 | 原循环粒子、预滚与停止发射后的尾段 | 单独挂在梅菲斯特Special0/Muzzle_2展示；各受益宿主的挂点回退未验证 |
| 连续施法 | 两次施法均从原动作起点播放 | 6秒间隔取本作既有独立样板，不新增规则 |
| 浅底对照 | 原光环、烟羽与亮部的透明边缘检查 | 不修改原材质颜色和混合 |

现有游戏已经有自动治疗逻辑和通用表现；本轮没有替换它。原骨骼1.1秒事件与当前游戏1.2秒治疗前摇的差异，留待确认接入时协调。

## 来源、处理与权利

- 原角色参考：[PRTS 梅菲斯特](https://prts.wiki/w/梅菲斯特)。原骨骼、atlas、PNG取自 `https://torappu.prts.wiki/assets/enemy_spine/enemy_1507_mephi/`，各文件URL/SHA256保存在本地 `reference/sources.json`。场内目标复用现有M21/M22/M23原动作图集。
- 原作资源版本固定为 `26-10-29-15-36-11-33649006`，不是“最新客户端”的声明。[索引镜像](https://raw.githubusercontent.com/ArknightsAssets/ArknightsAssets2/cn/bundles/hot_update_list.json)及[清单结构](https://github.com/MooncellWiki/torappu/blob/main/assets/ResourceManifest.fbs)用于定位；沿角色实际引用确认三个效果。官方包来源为 `https://ak.hycdn.cn/assetbundle/official/Android/assets/26-10-29-15-36-11-33649006/`。
- 两个原始包按对应索引MD5通过核验，SHA256保存在本地 `reference/bundle-sources.json`。必要共享Shader、材质、纹理复用先前已核验缓存，7项必要资源再次核验；独立Unity6000.6.5f1/Vulkan实际渲染。派生包只转换载入平台标记，原始包字节保留。
- 辅助匿名脚本包 `anon/33b097cf1de33a1bc2942aa4ec7963ac.bin` 下载内容未通过清单MD5，排除可信依赖及渲染输入；不把其类型名当成已验证的原作运行代码。需要的序列化字段来自已核验的角色/效果包。
- 工具：UnityPy1.25.4；AK LZ4适配参考MooncellWiki/UnityPy提交 `781badde6d636f101279fef76f461bb8dd0e1ffc`（MIT工具）。本地PixiJS/Spine工具复用前轮环境，Spine运行库有独立许可。工具许可不授权游戏媒体。
- 游戏媒体归原权利人所有，公开再分发许可仍未确认。本PR只有预览记录，不包含原包、原骨骼或派生预览媒体；实际看稿文件留上述本地目录。后续正式运行素材仍按现行仓库规则与源码交付，记录真实权利状态。

## 本轮验证及限制

独立Unity采样和930个原生画面输出退出0，六段最终MP4完整解码退出0。Chrome六片段尺寸/时长、慢放、暂停冻结、拖动、镜像、主/小窗口同步检查通过，0页面脚本错误；已目视检查原生关键帧、最终编码视频画面和浏览器看稿。

相同角色姿态、1.4秒时开/关效果，RGB最大通道差>8的像素分别为：施法者381，三个目标4961/4933/4942；并非只验证对象存在。原生材质实际支持Vulkan且纹理非空，最终封装先保留RGB再合成，避免丢弃alpha为0的加色亮部。

尚未运行原作完整程序集/调度脚本；延时、跟随、循环停止和片段编排由预览重建，未经原作实战逐帧对照，不宣称完全一致。无音效；无歌者、宿主转化、新主动、专属羁绊或入店。持续回复效果的受益宿主挂点策略仍待验证。预览站位、目标存在与重复间隔均为看稿条件，没有真实治疗/伤害结算。

本轮未修改游戏Assets、运行代码、动画配置、场景、Prefab、Packages/ProjectSettings，未构建游戏EXE、未做游戏Player验证、未验证第二台电脑。现有游戏构建不代表本次特效已接入。待用户针对V1确认后再推进接入。
