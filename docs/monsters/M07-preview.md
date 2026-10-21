# M07 士兵 V1：待用户确认

负责人霍利屹。本轮按逐只看稿与每项先保存GitHub的要求，完成普通士兵的工程外预览。前一项M06当前造型与动作已获用户继续指令，源码侧美术入口已推送2898ef4；本记录不将该批准扩大到M07。

## 原作参考和交付

核对 https://prts.wiki/w/士兵 中的模型登记，使用普通士兵enemy_1002_nsabr，不是弩手、双持剑士或其他士兵变体。直接源文件前缀 https://torappu.prts.wiki/assets/enemy_spine/enemy_1002_nsabr/ 。保留原版兜帽面具、黑灰战术服、橙黄扣件与近战刀刃。

工程外M07-Soldier-v1目录包含：preview-board.png（同一模型卡面/放大/预计尺寸对照）、card.png（600×840模型排版卡面，非独立绘制高清立绘）、idle.mp4（待机）、walk.mp4（双足行走）、attack.mp4（挥刀）。另有同名GIF、640×640透明待机PNG、三动作各六时刻检查图contact-sheet.jpg、sources.json和verification.json。

Idle/Move_Loop/Attack各为原版1秒60帧，每段MP4按原速循环至6秒，1280×720/60fps/无音频；GIF960×540/25fps/3秒。使用本地PixiJS6.5.10/pixi-spine3.1.2离线渲染，软件许可与原始下载文件留在本地，不引入Unity或公共源码。固定缩放0.8、根点(320,490)、共同裁切框，不逐帧重定位、不重绘或拉伸。预计待机角色约123px高，未校准Unity镜头。

## 检查、限制与阶段

已核对原作模型登记、透明轮廓/画布留白及动作六时刻检查图；3段MP4完整解码退出0。自然程度最终待人类看稿。行走为原地右斜前方向循环，无路线位移、步幅速度匹配或状态混合。原模型还包含其他移动方向，但本轮只交Move_Loop，不将额外方向、跑步或死亡声称为已验收。攻击只有角色挥刀，没有命中目标、伤害或战斗配置。无新增主动技能。

本轮M07未导入Assets、未注册可玩卡牌、未修改Runtime/主场景/Prefab/Packages/ProjectSettings。Unity/Play/Player验证未运行。A阶段待用户确认，B/C未开始。

原美术和动作权利属于《明日方舟》原权利人，具体原画师/动画师未确认，公开再分发许可未确认；不套用Wiki文字许可，不标CC0。原版媒体及派生PNG/视频只留本地，公共GitHub只保存本说明与校验值，未上传媒体。原始文件具体地址和SHA256见本地sources.json。

确认记录：等待用户确认M07-Soldier-v1具体文件。

## 交付校验值

| 文件 | SHA256 |
| --- | --- |
| preview-board.png | dd6a2ae3f7ac60d176a48430649b6af5a40f20d65e0bdcaebeb40c2514e80ad4 |
| card.png | fc5e2e2c5655976997628911b4e20cf534db6c4a6425832ae88256f3fad781d0 |
| idle.mp4 | bb5a8a5adefb51450fa2eb65361ee9d5054381b460bab87dbbfff2b2349630c9 |
| walk.mp4 | 81e7c926ea5ebfbc7f8acf2d78abf8816f6d956e2ca40472b901951a3b437920 |
| attack.mp4 | adc871a4f6731244681030967454cc1d862ba4b5c3f1a92efb9b7b6a086fac52 |
