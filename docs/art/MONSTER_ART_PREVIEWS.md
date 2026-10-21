# 已确认怪物美术：源码侧预览与本地媒体

负责人：霍利屹 / shigaharuki17。范围：M02、M03、M04、M05、M06、M07、M08、M09的已确认美术预览。当前用户逐项看稿确认，并要求每完成一个先保存GitHub；不将当前用户身份假定为干宸骅，不虚构主导者审批或认领Issue。

## Unity查看入口

- `Yesterday Convoy > Art > Approved Monster Previews`：切换M02猎狗、M03游击队猎犬、M04高能源石虫、M05酸液源石虫、M06冰爆源石虫、M07士兵、M09术师，播放/暂停/逐帧查看同一模型的卡面与动作。
- `Yesterday Convoy > Art > M08 Archer Approved Preview`：已确认弩手的独立窗口，保留兼容。

所有窗口仅属于Editor程序集，不注册可玩卡牌、不修改战斗Runtime、主场景、Prefab、配置、Packages或ProjectSettings。使用编辑器时间，不声称已接入战斗模拟时钟。普通怪无技能接口、稳定UnitId、独立战斗参数及战斗样板仍待协调；没有复制假技能帧通过校验。

## 当前资源与缺口

| 任务 | 原作模型 | 原版动作，60fps | 当前确认与缺口 |
| --- | --- | --- | --- |
| M02 猎狗 | enemy_1000_gopro | 待机60/行走48/攻击60帧 | 用户确认V1；未匹配实机步幅、速度与转身 |
| M03 游击队猎犬 | enemy_1077_sotihd | 待机60/奔跑32/攻击60帧 | 用户确认V1；没有独立慢走源动作 |
| M04 高能源石虫 | enemy_1021_bslime | 待机60/移动60/攻击150/死亡蓄爆60帧 | 用户同意写入V1；完整死亡爆炸特效未补 |
| M05 酸液源石虫 | enemy_1004_mslime | 待机120/蠕动120/喷吐80帧 | 用户确认V1；酸液弹道、腐蚀命中特效和削甲未接入 |
| M06 冰爆源石虫 | enemy_1067_snslime | 待机60/移动60/攻击150/死亡蓄爆60帧 | 用户认可当前V1；完整冰爆特效、寒冷、冻结未接入 |
| M07 士兵 | enemy_1002_nsabr | 待机60/行走60/攻击60帧 | 用户确认V1；未接入战斗 |
| M09 术师 | enemy_1011_wizard / Wizard | 待机120/行走60/攻击72帧 | 用户确认V1；法术弹道/命中特效未补 |
| M08 弩手 | enemy_1003_ncbow | 待机60/行走60/射击60帧 | 用户确认V1；没有战斗接入 |

卡面复用同一战斗模型排版，不是独立绘制的高清立绘。动作维持原版时长，例如M04攻击2.5秒/M05待机2秒，不统一压成1秒。固定640×640透明画布；M02～M07及M09根点(320,490)，M08根点(280,550)。源帧不逐帧重定位或变形。预计游戏尺寸未经Unity实机镜头校准。

## 本地媒体与公开源码

本地PNG/manifest放在`Assets/StreamingAssets/ApprovedMonsterArt/M02`至`M07`和`Assets/StreamingAssets/ArcherOriginal`，均被.gitignore排除，包括父目录.meta。M02～M07共1739张PNG（含7张600×840卡面）；M08另181张PNG。Unity缺包时显示安装提示，不联网下载原作素材。

在具备合法可用资源且Unity关闭时，使用PowerShell 7：

```powershell
./tools/install_approved_monster_art.ps1 -Source "本地Approved-M02-M06目录"
./tools/install_archer_original.ps1 -Source "本地M08 approved-package目录"
```

安装器先核对固定文件列表、唯一清单项及SHA256，再向对应专用目录复制；不接受清单内任意额外路径，不删除来源或覆盖其他角色。

原角色、贴图和动作权利属于《明日方舟》原权利人，具体原画师/动画师未确认，公开再分发许可未确认。原模型登记可从对应PRTS单位页的“敌人模型”核对，例如 https://prts.wiki/w/酸液源石虫 。源文件前缀为`https://torappu.prts.wiki/assets/enemy_spine/<原作模型>/`。PRTS页面可见不构成开放素材许可，也不能用Wiki文字许可覆盖游戏媒体。本轮看稿认可不等于取得公开发行授权。

**公共提交只包含Editor代码与.meta、安装工具、来源/验收说明和ignore规则。原版素材、派生PNG、卡面、视频、GIF、含媒体预览页不上传公共GitHub。** 完整源文件地址、校验值、处理说明、用户确认原话及软件许可保存在工程外各V1目录sources.json/approval.json/交付说明.md和本地包manifest中。离线渲染工具没有引入Unity或公共源码。

## 基线、验证与阶段

前几轮在ZIP副本完成工程外预览和源码侧窗口；本轮新克隆到独立`YesterdayConvoy`目录，以远端main提交70b9078为基线，创建`codex/monster-art-previews-m02-m05-m08`分支，只迁入本任务明确文件。旧ZIP副本和预览工作区完整保留，未stash或覆盖他人修改。旧接入文档中的“无.git副本”描述属于历史，不代表本轮克隆现状。

实际检查：新增Editor脚本用Unity6000.6.5f1自带Roslyn和UnityEditor/UnityEngine参考程序集独立编译；安装脚本实际运行并核对媒体校验值；暂存明确源码后运行`python tools/check_repository.py`检查Git文件列表与.meta。日志在本机artifacts/github-art-save，详见本轮交付结果。

完整Unity项目导入、窗口人工操作、Play测试和Player构建未通过本轮验证：此前Unity启动在许可证阶段报告缺少授权并以198退出，该条件尚未改变。单文件C#编译不等于Unity整体编译或玩法通过。本轮不制作试玩ZIP/Releases，不自动合并main。所有角色仍是美术确认/源码侧预览阶段，B战斗样板与C正式流程未完成。

2026-10-22后续：可选包新增M09，安装集合为M02～M07及M09（M08仍用独立安装器）；原作媒体共1739张PNG。M09模型显式选择Wizard皮肤，来源含Ark-Models骨骼/图集镜像与PRTS贴图，详见角色记录。
