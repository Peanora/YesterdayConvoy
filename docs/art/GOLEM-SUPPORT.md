# 泥岩巨像支援资源

2026-11-04，人类批准项目外四段真实动作预览及接入范围。来源：[PRTS泥岩巨像模型页](https://prts.wiki/w/泥岩巨像/spine)，模型 `enemy_1092_mdgint`。媒体归《明日方舟》原权利人，具体画师/动画师未确认。非CC0，开放修改、游戏分发、公开再分发许可未确认；人类接入及上传决定不等于权利人授权。

原模型skel/atlas/PNG下载于项目外，按原动画60fps离线渲染透明图集：Idle192帧/3.2秒、Move120帧/2秒、Attack311帧/约5.166667秒、Die71帧/约1.166667秒，共694帧、16页PNG、1张静态卡面。时长、切片、枢轴见manifest；保留Alpha，脚底与现有世界单位校准。未改画人物、不声称AI生成，未添加Spine运行库。

运行目录：`Assets/StreamingAssets/MonsterCombatArt/GOLEM`。提交实际图集、卡面、manifest及.meta，骨骼原件与加工运行库留项目外。批准预览：`E:/珍贵资料/昨日圆车_美术预览/2026-11-04_三条羁绊-v1`，不是游戏运行依赖。

支援定义在 `Assets/Data/Resources/Support`，显式工具 `GolemSupportTools.Create` 幂等维护配置，不进入商店。`LocalMonsterArt`读取离线图集，`ArtPreparation`按编队预载，部署后沿用受击、血条、死亡管线。原普攻约5.167秒，2.767秒命中，保留完整动作。

Unity实际导入、编译、配置与缓存路径、GPU暂停拖动/完整普攻动作锁/自然获胜培养至II级检查通过。原Alpha与导出规格在项目外处理时检查。此VERIFIED只表示上述范围，不代表取得开放版权许可或完整13关自然平衡；最终构建与独立Player结果见[QA](../QA.md)。
