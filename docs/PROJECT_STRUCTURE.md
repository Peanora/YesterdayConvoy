# 工程目录与场景规范

2026-11-09十三关行动v2：复用现有八个保存地图场景，构建清单仍24场景；Resources/CampaignV2新增Journey、八份正式StageXXBattle与v2只读数值JSON，Assets/Data/CampaignV2保存批准流程与authoring输入。静态Journey会话只携带经济/编队数据，进入各场景后恢复，不携带旧场景视觉对象或在Play搭地形。正式入口与编辑覆盖边界见[十三关说明](CAMPAIGN_JOURNEY_V2.md)。

2026-11-08八图V2：新增 `Assets/Scenes/Map01SunsetSquare.unity` 至 `Map08EbbHarbor.unity` 八个独立保存战场，构建清单共24场景。实际场景缩略图、持久Mesh和材质在 `Assets/Art/Environment/ApprovedMaps`；只读地图/路线/守军配置在 `Assets/Data/Resources/ApprovedMaps`。从怪物选择页“战术地图 · 八张新地图”进入；编辑方法、显式重建工具边界和地道/热泵试验规则见[八图说明](APPROVED_MAPS_V2.md)。日常只需打开本工程相应场景，Play不生成整张地图。

2026-11-08波次试玩：`BlackForestCrossing.unity`继续保存全部地形；地图配置新增`attackWaves`，引用`Assets/Data/Resources/Maps/BlackForestWaves.asset`保存三波入口/独立DP/部署时长/末段推进。编辑配置无需修改场景，运行状态由ManualBattle独立持有。默认十三关及旧专项不自动套用波次；树海入口改为“黑流树海 · 波次试玩”。

2026-11-08树海六星防线：`Assets/Scenes/BlackForestCrossing.unity`保持保存地形，`Assets/Data/Resources/Maps/BlackForestCrossing.asset`已配置九名守军（六名六星）。从测试选择进入“树海交错回廊 · 六星防线”，8级自由整备、正常角色数值、50DP/6共享防御。入口和规则见[地图说明](BLACK_FOREST_CROSSING.md)，覆盖下文此前无守军的阶段记录。

2026-11-06树海地图：新增 `Assets/Scenes/BlackForestCrossing.unity`，共16个保存场景，由GameSceneCatalog自动纳入构建。17×14地形、三红两蓝、根系与三种视觉特殊地面直接保存在场景；从怪物测试选择进入“树海交错回廊 · 地形试走”。本轮无守军，不替换既有挑战地图，编辑与操作见[树海地图说明](BLACK_FOREST_CROSSING.md)。

2026-11-06资源分类整理：原StreamingAssets运行原图已迁入下表的Art分类；JSON清单和来源记录移入`Assets/Data/ArtCatalogs`。工程内StreamingAssets只留说明，Windows构建时自动收集运行清单与预处理纹理到Player的StreamingAssets。逻辑包名、原图内容和GUID保留；这不表示同意删除早期美术。

2026-11-06树海地图加入前保存场景总数15（加入树海后16）：原Campaign、BattleShop、MonsterTrial、SynergyTrial、MudrockTrial；新增TalulahPhaseTrial，以及Assets/Scenes/SynergyTrials下9个专项。全部由GameSceneCatalog统一进入构建。专项源配置在Assets/Data/SynergyTrials，塔露拉守军在Assets/Data/Trials。新增场景沿用保存地形副本，可独立编辑，不在Play重建地图；具体入口和修改方法见DEDICATED_SYNERGY_TRIALS.md。下文旧入口数量属于目录迁移初版记录。

2026-11-03 · 人类确认。日常只打开 `E:/珍贵资料/昨日圆车` 这一个Unity工程，不要把Git工作目录、试玩构建目录当作多个游戏工程来回切换。分支与PR用于协作，不能代替清晰的Assets目录。

## 在Unity里查看地图

- `Assets/Scenes/MudrockTrial.unity`：泥岩与可争夺留声机独立试玩，地形和装置位置均保存于场景。
- `Assets/Scenes/Campaign.unity`：三阶段13关挑战，当前默认构建入口；地形直接保存在场景，关卡仅配置化更换守军。
- `Assets/Scenes/SynergyTrial.unity`：独立羁绊验证，8级、无限金币，整合/雪怪与升星均真实生效。
- `Assets/Scenes/MonsterTrial.unity`：怪物试用入口。地图已保存，未运行时即可查看、选择并编辑。Play后选择怪物或进入既有守军技能样板。
- `Assets/Scenes/BattleShop.unity`：最新六星防线与战后商店。地图地块、工事、材质保存于场景；Play后仍是两关演示与霍利屹原版横卡商店。第二关复用同一地图，只改变守军配置，并不是另一张隐藏地图。
- Unity菜单 `Yesterday Convoy → Scenes` 可直接打开四个入口。场景不在Hierarchy里自动全部列出；Hierarchy只列当前打开的场景。

## 目录责任

| 目录 | 内容 |
| --- | --- |
| Assets/Scenes | 所有正式地图场景，每张地图一份独立.unity |
| Assets/Art/Characters | 人物与怪物；Defenders/Flipbooks和Monsters/Flipbooks为守军/怪物动作图集，Boss/TalulahOriginal及OriginiumSlugOriginal为独立原版动作包 |
| Assets/Art/Environment | 地图纹理、地形配套材质与网格 |
| Assets/Art/Cards、UI、VFX、Materials | 卡面、界面、特效、共用材质/Shader |
| Assets/Audio、Fonts | 音频与字体 |
| Assets/Prefabs | 可复用对象；Resources/Maps保存已制作地图的共享预制资源 |
| Assets/Data | 卡牌、守军、地图、技能等配置；ArtCatalogs按逻辑包保存动画/特效JSON清单和来源记录；Resources内保留稳定加载名 |
| Assets/Scripts/Runtime、Editor | 运行脚本、编辑器工具，分别保留程序集边界 |
| Assets/Tests | EditMode与PlayMode测试，不包含其他可玩地图场景 |
| Assets/StreamingAssets | 仅说明文件；Windows Player中的同名目录由构建工具生成，不再存放工程源图集 |

角色PNG按角色分类保存在Art，特效在Art/VFX/OriginalCombat与Art/VFX/Skills/Talulah/Unbound，商店源石图在Art/UI/Shop。`ArtSourcePaths`把稳定逻辑名解析为编辑器源路径或Player路径；清单内页名、动作时序和Library/PreparedArt缓存键保持。使用运行图集的人应从Data角色配置、ArtCatalogs和来源记录定位，不复制同一套资源。首次导入会生成正常TextureImporter预览；实际动作仍读原PNG或预处理纹理，详情见[角色加载说明](ART_LOADING.md)。

## 新地图必须遵守

1. 先在Assets/Scenes中新建并保存独立场景，起清晰名字；地图地形、平台、障碍和门均应在编辑模式可见、可选、可保存。
2. 复用Prefab与材质，引用GUID和.meta一起提交。配置放Assets/Data，运行状态不能写回配置。
3. 禁止在点击Play时用循环重新生成整张地图，也不得用菜单切换同一场景中的隐藏地形生成器代替场景资产。
4. 允许代码生成战斗角色、动态特效、部署预览和路线反馈。这些是运行表现，不是地形来源。
5. 修改场景中的地图实例会保留到运行中；重开关卡只清战斗状态，不覆盖地形编辑。场景依赖的材质、Mesh必须为已保存资产，不能引用上次运行临时对象。
6. 构建场景清单包含Campaign、BattleShop、MonsterTrial、SynergyTrial，正式试玩默认从Campaign进入。新增地图须显式加入清单，并补场景引用与实际试玩验证。

## 清理范围

已退役泵站/金叶/旧五行盟约地图、旧三图测试场景、游击队独立地图场景和对应地图生成入口。保留角色资源、核心怪物/守军能力、纯战斗逻辑和仍有效的能力测试。旧资源与历史场景由Git保存；不要运行历史生成工具恢复旧目录。

当前大地图的迁移工具仅用于一次性将既有几何保存成资产，不在Play调用，也不是以后创建地图的默认入口。共享地图Prefab供测试在怪物试用宿主中复用；正式BattleShop使用自己场景里已保存的地图实例。

本机标准试玩：`Builds/Windows/YesterdayConvoy.exe`，构建用Yesterday Convoy → Baseline → Build Windows。保留整个Windows目录。地图编辑时查看场景地形；进入Play后才创建战斗角色、HUD和商店。

2026-11-05新增 Assets/Scenes/MudrockTrial.unity：独立泥岩/留声机争夺试玩，场景保存地形和装置位置。共五个构建场景，默认Campaign保持。
