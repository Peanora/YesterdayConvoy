> 后续更新：本任务已迁入真实Git克隆与独立功能分支；当前入口、M05状态、媒体安装和验证以[统一记录](MONSTER_ART_PREVIEWS.md)为准。下文为原接入批次的历史说明。

# M02～M04：已确认美术的源码预览入口

当前用户逐项确认M02猎狗、M03游击队猎犬，并在查看M04后要求“写入代码，向下继续做”。本轮将这三项已确认V1加入独立Editor预览窗口。M04仍明确保留完整死亡爆炸特效缺口，不把用户同意入库解释为特效已经补完。

## 打开入口

Unity菜单 `Yesterday Convoy > Art > M02-M04 Approved Previews`。下拉切换角色，分别支持原版待机、移动/奔跑、攻击及M04死亡蓄爆；可播放/暂停和逐帧检查，卡面、放大形象及小尺寸参考同屏显示。所有循环保持原帧数和60fps：M02为60/48/60帧，M03为60/32/60帧，M04为60/60/150/60帧。没有把2.5秒攻击压成1秒，也没有复制假技能帧。

仅新增 `Assets/_Game/Editor/ApprovedMonsterArtPreview.cs` 及其.meta；不修改既有M08窗口、Runtime、主场景、Prefab、配置、包或项目设置。编辑器使用编辑器时钟；不是已接入战斗模拟时钟。窗口仅保留卡面与当前帧纹理，切角色和关闭时释放。

## 本地媒体与安装

本地目录 `Assets/StreamingAssets/ApprovedMonsterArt/M02/`、`M03/`、`M04/` 已全部被.gitignore排除，包括父目录.meta。三包分别169、153、331张PNG，合计653张，另各1份manifest。各卡面600×840，动作固定640×640透明画布，根点(320,490)。保留已确认V1文件字节，原卡面上“待确认”是历史排版文字，不另行重绘。

可恢复包本机位于 `D:/游戏开发/YesterdayConvoy-ArtPreviews/Approved-M02-M04/`。经协调取得合法可用的同一资源后、关闭Unity，使用PowerShell 7执行：

```powershell
./tools/install_approved_monster_art.ps1 -Source "D:\游戏开发\YesterdayConvoy-ArtPreviews\Approved-M02-M04"
```

脚本在任何复制前检查三个包的固定文件列表与SHA256，不从清单接受任意文件路径，不删除来源或改变其他角色。媒体缺失时窗口显示安装提示，不联网下载或以其他角色替代。

## 来源与范围

原作页面分别为 https://prts.wiki/w/猎狗 、 https://prts.wiki/w/游击队猎犬 、 https://prts.wiki/w/高能源石虫 。原作模型分别enemy_1000_gopro、enemy_1077_sotihd、enemy_1021_bslime；sourceModel不等于已批准的本游戏UnitId。直接源文件地址、SHA256、原始工具许可和确认文件保存在对应工程外V1目录的sources.json、approval.json、交付说明.md。

原角色/贴图/动作权利属于《明日方舟》原权利人，具体原画/动画师未确认，公开再分发许可未确认。确认美术不等于取得再分发许可；原作媒体及派生图集/预览仅本地保存，不自动加入公共PR。源码侧只新增专用Editor脚本、安装工具、说明和ignore规则，不改共享CSV/Credits。

尚未注册可玩卡牌、稳定UnitId、战斗参数或无技能接口。移动为原地循环，未匹配实际速度、路线、方向切换；M04死亡只有蓄爆，没有完整爆炸特效/伤害。B战斗样板/C正式流程均未完成。本轮没有技能、部署、削甲、羁绊或其他玩法改动。

## 验证

新Editor脚本使用Unity6000.6.5f1自带Roslyn及对应UnityEditor/UnityEngine参考程序集独立编译，退出0；这不是Unity项目导入编译。安装脚本实际执行并核对全部653张PNG。日志和静态检查在artifacts/m02-m04-art。当前Unity许可证前次已在编译前以198退出，未修复许可前不重复声称Unity项目验证成功；本轮未进行窗口人工操作、Play测试或Player构建。

源工程仍为无.git的ZIP副本，无法检查暂存状态/同步最新main/创建真实功能分支或PR。原M08改动全部保留，不初始化假Git历史。本轮未更新ZIP/Releases。
