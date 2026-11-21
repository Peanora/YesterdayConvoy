# 同源石谷：卫戍协议 #02 原作地形

2026-11-21，用户确认「同源石谷」项目外近景与全图，授权接入现役棋盘。按用户要求，场内石板、岩壁、苔裂地面、木桥、岸边植被与水面都取自本机《明日方舟》**卫戍协议·规程 #02 常规场地**的同一原作地形套件。未复用被否决的雨庭泵站或另行生成贴图。

## 输入与复核

| 内容 | 本机来源 |
| --- | --- |
| 地形网格和原 UV | `PersistentData/Bundles/battle/prefabs/levelprefabs/activities/act3autochess/level_act3autochess_m02/level_act3autochess_m02_sub_01/level_act3autochess_m02_sub_01.ab`，更新清单 `26-11-13-06-34-18-84612175`。 |
| 砂岩、植被与水底贴图 | `StreamingAssets/AB/Windows/arts/maps/map_autochesssand/res.ab`，随包清单 `26-10-20-11-10-38-04170990`。 |
| 原作对照 | 本机 `arts/ui/stage/mappreviews/act3autochess_m02_sub_01.ab`，核对高台、裂石、木栈和边岸。为游戏内静态预览，无动态时码。 |
| 水面滚动 | 原场 `MT_AutochessSand_water` 的 `_FlowSpeed=(.10,.07)`；兼容着色器使用同原包的水底纹理和法线，适配池形、光照与颜色，不声称复刻原作反射、焦散或原水 Shader。原作没有与本棋盘重排位置对应的战斗时码。 |

[导出索引](../../../Assets/Art/Environment/FormationCanyon/source.json) 包含各包版本、SHA-256、MeshRenderer/网格对象 ID、导出贴图哈希。用 `tools/art/export_formation_canyon.py` 从本机资源复导，原 AB 不入仓。原作素材开放再分发许可未核实。红蓝门复用现役场景已确认的原透明门；未改其网格、材质或位置。

## 场景适配

- `FormationBattle.unity` 仍是唯一正式入口。板面 22×8、左侧 10×8、红门列 10、蓝门列 21，角色、镜头和战斗数值不变。
- 采用确认稿的暖砂岩、苔裂石、折一排的原纹样地带、近侧浅渠与短桥。实测大水域会影响 1-3 与望的既定战斗结果，浅渠收在原掩体的 2×1 格内（列 12–13、行 1，水面低于地面 .30），列 14 的平木桥和两岸可走。上侧旧掩体用同源低岩替换，仍保留两处高台。没有固定行进线；水格不可走或站守军，不加减速或伤害。
- 场景地形与美术对象在编辑模式保存。重排、局部裁取芦苇、镜头适配、材质兼容与现场灯光属于本作适配。进入 Play 不重造地图；`FormationCanyonWater` 仅更新水面相位，暂停冻结。
- 需要重建此版本时使用编辑器菜单 `Yesterday Convoy → Formation Battle → Author Formation Canyon`；这是重铺当前关卡的命令，不是运行时入口。
