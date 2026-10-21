# M09 术师 V1：待用户确认

负责人霍利屹。按逐只看稿、每项完成先保存GitHub的约定，制作普通术师工程外预览。M08已确认跳过；M07已确认并以645a0f2推送源码侧Editor入口。本记录不扩大对其他单位的批准。

## 原作核对和实际交付

核对 https://prts.wiki/w/术师 的原版模型登记enemy_1011_wizard，明确选择Wizard皮肤；不是术师组长或高阶术师，不自行添加法杖。保留兜帽、面具、深色长衣和原版手势。

原站下载骨骼/图集时出现超时，实际从 https://github.com/isHarryh/Ark-Models/tree/main/models_enemies/1011_wizard 取得skel与atlas；贴图来自 https://torappu.prts.wiki/assets/enemy_spine/enemy_1011_wizard/enemy_1011_wizard.png 。实际逐文件下载地址和SHA256在本地sources.json/download-record.json，未把镜像下载误记为原站下载。镜像维护者isHarryh不等于原作美术作者，具体原画/动画师未确认。

工程外M09-Caster-v1含：同一模型卡面/放大/预计尺寸对照preview-board.png、600×840卡面排版card.png、idle.mp4/walk.mp4/cast.mp4、同名GIF、640×640透明待机图、动作六时刻检查图及来源/验证记录。卡面为模型排版，不是独立绘制高清立绘。

原版Idle为2秒120帧、Move_Loop为1秒60帧、Attack为1.2秒72帧，原速循环。各MP4为1280×720/60fps/6秒，无音频；GIF为960×540/25fps/3秒。沿用本地PixiJS6.5.10/pixi-spine3.1.2离线渲染，显式setSkinByName(Wizard)。工具许可本地保留，未引入Unity或公共源码。所有帧固定缩放0.8、根点(320,490)、共同裁切，不逐帧定位、拉伸或AI重绘。预计待机角色约118px高，未校准Unity镜头。

## 检查与缺口

已检查Wizard皮肤存在且正确加载、透明轮廓/画布边缘、每动作六时刻检查图，并完成三段视频完整解码（退出0）。行走为原地循环，没有路线移动、速度匹配、转身或动作混合。法术攻击仅角色手势，没有完整法术弹道、命中特效或法术伤害接入。cast.mp4仅为文件名，不代表新增主动技能。死亡和起停动作未交付为已验收预览。

没有向Assets导入M09，没有卡牌配置或战斗注册，没有修改Runtime/共享场景/Prefab/Packages/ProjectSettings。Unity/Play/Player验证未运行。A阶段待用户看稿，B/C未开始。

原美术/角色/动画权利属于《明日方舟》原权利人；公开再分发许可未确认，不套用Wiki文字许可或镜像仓库软件许可，不标CC0。原版媒体及派生图像/视频仅本地，公共提交只含本说明及校验值。

确认记录：待用户确认M09-Caster-v1，不把M07的“可以”解释为M09批准。

## 本地交付校验值

| 文件 | SHA256 |
| --- | --- |
| preview-board.png | ce595b21557ed86c02801641a6e5c637aef278c1a29b7f655657214853e19521 |
| card.png | 6a68a2f2dd68737b93a02819de39933add115ef97d0bf240cefcc7df9ce4293b |
| idle.mp4 | f3dddcd294d18c5e2f7d770e5a2249a22980ba10794da996cf9708b57924776d |
| walk.mp4 | 46f8563426068f648ae6b14e5403a2ca0dac9aff56c8e3d411809e330156a7bc |
| cast.mp4 | 894a6f343226714e437fcea346eb710314ce90ecb594326974ac6c10b88dd713 |

## 后续用户确认与源码侧入口

2026-10-22（Asia/Shanghai）当前用户看稿后回复“可以”，确认M09-Caster-v1卡面、待机/行走/法术攻击手势。已加入Approved Monster Previews编辑器窗口及本地包安装器，120/60/72帧保持2/1/1.2秒原速。原作媒体继续Git忽略。本条覆盖待确认历史；不代表法术弹道/命中特效、伤害逻辑或战斗卡牌已接入。确认记录保存在本地approval.json。
