# 工程目录与场景规范

2026-11-03 · 人类确认。日常只打开 `E:/珍贵资料/昨日圆车` 这一个Unity工程，不要把Git工作目录、试玩构建目录当作多个游戏工程来回切换。分支与PR用于协作，不能代替清晰的Assets目录。

## 在Unity里查看地图

- `Assets/Scenes/MonsterTrial.unity`：怪物试用入口。地图已保存，未运行时即可查看、选择并编辑。Play后选择怪物或进入既有守军技能样板。
- `Assets/Scenes/BattleShop.unity`：最新六星防线与战后商店。地图地块、工事、材质保存于场景；Play后仍是两关演示与霍利屹原版横卡商店。第二关复用同一地图，只改变守军配置，并不是另一张隐藏地图。
- Unity菜单 `Yesterday Convoy → Scenes` 可直接打开这两张地图。场景不在Hierarchy里自动全部列出；Hierarchy只列当前打开的场景。

## 目录责任

| 目录 | 内容 |
| --- | --- |
| Assets/Scenes | 所有正式地图场景，每张地图一份独立.unity |
| Assets/Art/Characters | 人物与怪物的Unity导入资源 |
| Assets/Art/Environment | 地图纹理、地形配套材质与网格 |
| Assets/Art/Cards、UI、VFX、Materials | 卡面、界面、特效、共用材质/Shader |
| Assets/Audio、Fonts | 音频与字体 |
| Assets/Prefabs | 可复用对象；Resources/Maps保存已制作地图的共享预制资源 |
| Assets/Data | 卡牌、守军、地图、技能等配置，Resources内保留稳定加载名 |
| Assets/Scripts/Runtime、Editor | 运行脚本、编辑器工具，分别保留程序集边界 |
| Assets/Tests | EditMode与PlayMode测试，不包含其他可玩地图场景 |
| Assets/StreamingAssets | 现有逐帧动画原始运行图集、清单等；这是Unity专用目录，不是第二套项目 |

角色在StreamingAssets中的已跟踪动画继续保留，避免无关改变加载格式。使用运行图集的人应从Data角色配置和来源记录定位，不复制同一套资源。

## 新地图必须遵守

1. 先在Assets/Scenes中新建并保存独立场景，起清晰名字；地图地形、平台、障碍和门均应在编辑模式可见、可选、可保存。
2. 复用Prefab与材质，引用GUID和.meta一起提交。配置放Assets/Data，运行状态不能写回配置。
3. 禁止在点击Play时用循环重新生成整张地图，也不得用菜单切换同一场景中的隐藏地形生成器代替场景资产。
4. 允许代码生成战斗角色、动态特效、部署预览和路线反馈。这些是运行表现，不是地形来源。
5. 修改场景中的地图实例会保留到运行中；重开关卡只清战斗状态，不覆盖地形编辑。场景依赖的材质、Mesh必须为已保存资产，不能引用上次运行临时对象。
6. 构建场景清单包含两个当前入口，正式试玩默认从BattleShop进入。新增地图须显式加入清单，并补场景引用与实际试玩验证。

## 清理范围

已退役泵站/金叶/旧五行盟约地图、旧三图测试场景、游击队独立地图场景和对应地图生成入口。保留角色资源、核心怪物/守军能力、纯战斗逻辑和仍有效的能力测试。旧资源与历史场景由Git保存；不要运行历史生成工具恢复旧目录。

当前大地图的迁移工具仅用于一次性将既有几何保存成资产，不在Play调用，也不是以后创建地图的默认入口。共享地图Prefab供测试在怪物试用宿主中复用；正式BattleShop使用自己场景里已保存的地图实例。
