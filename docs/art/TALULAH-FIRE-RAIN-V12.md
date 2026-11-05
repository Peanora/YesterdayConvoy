# 塔露拉火雨 V12 运行表现

人类于2026-11-05确认项目外V12看稿并要求接入。范围为火雨火流、动态地面燃烧、原素材命中、轻微屏幕泛红及短促震屏；不更改技能伤害、技力、施法锁定、路线或地图。

## 接入方式

`FireRainArt`按需读取Resources/TalulahFireRain清单和9张PNG，共534帧（地面144、火流351、命中39），30fps。图集保留预乘及零Alpha加色RGB，Standalone导入为BC7；原PNG、清单、Shader和.meta随源码交付。

`FireRainView`读取每个实体的FireRainStartedAt、真实CombatShot命中和统一Battle.Clock，按本局/实体/施法时间去重。它只绘制，不施加伤害。以场景已保存的Cell、Tile surface、Platform cap和High ground inset几何生成贴地热光并布置错相火焰，不使用看稿背景或屏幕遮罩，不生成或修改地形。深度测试保留，角色前后顺序来自现有脚底排序。

现有技能仍为6秒、6次全场伤害；为保留V12的稀疏密度和2秒波距，视觉将每两次结算组合为一面五行火流（0.25/2.25/4.25秒起势，2.5世界单位/秒）。离场余火及最后1.6秒熄灭为独立表现，不延长人物施法或增加伤害。不同地图宽度决定火流离场时间；这不是原关卡十波调度的完整复刻。画面命中火焰始终读实际命中事件，不根据火流位置额外命中。

`FireRainScreenEffect`和FireRainScreen Shader只偏移最终世界画面，不移动逻辑相机或IMGUI界面；轻震最多3.2/1280画宽，短促衰减。暖红基础约3.2%，边缘最多再2.5%，命中短脉冲最多再2.2%。暂停时全部读取同一冻结时钟，结束、重开、换战斗或结算清理。

## 来源及权利

原资源为明日方舟旧版敌方首领“不死的黑蛇”，沿用项目外V9/V10/V12已核实缓存与固定镜头离线采样。

- 包：`pkgrps_btl_pfb_fx_enemy_1.dat`，原UnityFS缓存MD5为`46ac009a973053d7ee3a8f4cdd1b201e`。
- 具体记录地址：<https://ak.hycdn.cn/assetbundle/official/Android/assets/26-10-29-15-36-11-33649006/pkgrps_btl_pfb_fx_enemy_1.dat>。
- 火流：`enemy_bsnake_reborn_01_trail`；命中：`enemy_bsnake_reborn_01_trail_hit_01`；地面适配：`enemy_bsnake_skill_01_buff`。
- 地面层去掉目标环、扰动壳、爆闪，调整主火焰发射率45/秒、寿命0.5–0.85秒、大小0.45–0.75、起点高度0.16、循环/延迟/形状，使用预滚后144帧和多相位。暖色、强度及地面空间分布是本作适配。
- 外部Stronghold Protocol导出/离线预览工具仅用于项目外加工，未作为游戏运行依赖或复制进Assets；运行图集打包脚本是本项目的`tools/build_talulah_fire_rain.py`，输入已采样RGBA文件，需Pillow。
- 原媒体权利属于原权利人，未确认开放再分发许可；非商业用途、人类提供或要求上传均不记为权利人授权。按项目持续交付要求提交实际运行子集，原包和加工工作区不入Git。

## 验证入口

GPU PlayMode：FireRainV12Tests、FireRainPresentationTests、TalulahSceneTests及相关原攻击/切换回归。

Development独立Player：`-talulahFireRainV12Test -demoEvidenceDir <输出目录>`，从正常塔露拉入口部署/施放，核对实际GPU像素、动态帧、五行齐头、地面覆盖、暂停、原6秒/6波、尾段不追加伤害及重开/切换清理。弱守军提前阵亡后不再产生目标命中，18次完整目标结算由既有高生命值逻辑用例验证。它是明确的自动化验证入口，不代表人工完成全部关卡或第二台电脑测试。实际结果见STATUS/QA。
