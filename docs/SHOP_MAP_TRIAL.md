# 大地图、可开关商店与扁平备战席

2026-10-30 · 霍利屹 · [任务 #48](https://github.com/Peanora/YesterdayConvoy/issues/48) · [PR #47](https://github.com/Peanora/YesterdayConvoy/pull/47)

最新布局：地图占约75%屏幕高度，10格备战席压成下沿横向短卡槽。商店默认收起，展开后覆盖地图上方，不缩小或移动地图。保留已确认的卡片画面、羁绊标签、费用渐变色与右侧刷新/锁定排列，使用已有泵站回廊，地图格局和原俯视角度保持。

## 打开与操作

本机1920×1080试玩：`D:/游戏开发/YesterdayConvoy-ShopUI/Builds/ShopMap/YesterdayConvoy-Shop.exe`。保留完整ShopMap目录，不能只复制EXE。Unity 6000.6.5f1打开本分支的 `Assets/_Game/ShopTrials/ShopMap.unity` 点Play；重建Windows使用 `YesterdayConvoy.Editor.ShopMapTools.Build`，输出 `Builds/ShopMap`。

- 右上“打开商店/收起商店”或 **Tab** 切换；**Esc** 收起并取消拖动。开关不刷新、不扣款、不移动镜头，不重置棋子或战斗。商店覆盖部分拦截地图点击；展开不自动暂停。
- 点商店角色图片购买。右下角预告买下这一张后的最终二星/三星：二星银色、三星金色，星标在固定位置闪烁，角色图标有对应银/金光效；按最新反馈不使用绕图标旋转的小星星。连锁三星直接显示金色三星；预告不占库存、不扣钱，源石不足时仍不能购买。
- 升级4源石换4XP，经验溢出保留；刷新2源石，锁店不刷新不扣费。羁绊目前是标签展示。
- 下沿横向卡槽中，头像在左，星级和名字在右。点击切换编队；拖动时商店自动收起，再拖到红门部署，战斗自动开始。战斗后交易冻结，商店仍可开关查看。
- 备战角色向下拖入最下沿出售区，出现报价后松手。拖回、区外松手、Esc、失焦均取消；卡槽底部也有足够向下拖动空间。轻点或抖动不出售。
- “满席两星/满席三星”载入10格材料并展开商店。点击第一张弩手直接购买并合成，不弹确认，分别剩9格/7格，只扣一次；第二张猎狗不能合成时拒绝且不扣钱。
- 升星后仅在对应备战棋子的头像/星标上显示银色或金色反馈，不弹中央成功面板，也不显示成功文字。
- “重新测试”回默认材料和收起状态；“退出测试”回原怪物菜单并恢复镜头视口/FOV。

![收起商店的大地图](ui-previews/shop/shop-map-ready.png)
![展开的覆盖式商店](ui-previews/shop/shop-map-shop-open.png)
![商店银色二星预告](ui-previews/shop/shop-rank-silver-ready.png)
![商店金色三星预告](ui-previews/shop/shop-rank-gold-ready.png)
![合成后备战三星，无成功弹窗](ui-previews/shop/shop-map-three-star.png)

## 比例、连接与范围

虚拟坐标1280×720：地图从y64到602，高538（74.7%）；备战带y632到720，高88，单卡高46。上一版备战带146、单卡80，现单卡高度减少42.5%，头像等比显示。1920×1080时地图实际高807像素。商店展开只增加上层绘制和输入遮挡，镜头始终一致，不能把之前的小地图FOV放大倍数当成当前屏幕占比。

布局参考方舟战斗画面的大地图与下沿部署条，参照[实机画面](https://note.com/grsn_note/n/n33f60d3ce1fa)；这不是逐像素复刻或原作UI提取。没有导入新的参考游戏UI贴图/动画，继续使用项目既有样式与素材。

`ShopTrialModel`计算交易；`ShopMapTrial`连接卡定义、编队、覆盖层与输入；`ShopMapBootstrap`负责独立入口及Tab/Esc。所有商店点击共用同一原子事务，覆盖区域使用与地图输入相同的可用区域判断。运行时克隆地图asset，不写回Resources。未修改InitialDemo、原地图配置、Prefab、Packages或ProjectSettings。

仍是10种基础角色测试池（M02/M03/M05/M08、原生源石虫、M15～M19），非完整正式商店或羁绊章节。按角色种类编队，同种只加入一次；沿用原卡DP/生命/冷却/部署次数。星级影响库存、展示和出售，暂不增加战斗属性。未实现战后收入、通关XP、存档、完整高费卡池或复杂首领编队。售出1/2/3星到账c/3c−1/9c−1，返库1/3/9张，规则保持。

## 实际验证与交付

本轮完整278/278 EditMode（edit-rank.xml）与4/4定向GPU PlayMode（play-rank.xml）通过。验证预告反复读取不改棋子/钱/库存、2/3星预告匹配实际结果、无钱不能买、满席无确认一次合成、重复购买不重复扣款、购买/出售后预告更新；场景覆盖合成后反馈为空、覆盖层、实际部署和地图恢复。

本轮最终构建和独立Player结果见[QA](QA.md)。证据目录 `D:/游戏开发/artifacts/shop-map/`：edit-rank.xml、play-rank.xml、build-rank-final.log、player-rank-final.log与player-rank-final截图。连续实机帧记录固定银色/金色星标的闪烁与光效，旋转小星星已移除；截图本身不能证明动画。[二星动态](ui-previews/shop/silver-rank.webm)、[三星动态](ui-previews/shop/gold-rank.webm)各由16张连续实机帧按8fps合成，仅展示动态，不作精确计时或游戏帧率证明。旧完整74 PlayMode、Node7/7和浏览器检查是历史结果，本轮未重跑；浏览器目录仍保留v12确认/中央面板旧展示，不作为最新Unity行为。

本轮取消确认和成功弹窗是用户最新修订，已同步GDD第7节，覆盖旧文档的确认步骤。视觉参考[官方TFT商店动画及二/三星效果改进方向](https://followthefix.teamfighttactics.leagueoflegends.com/)；银/金光晕、旋转光弧、轮廓及星标均由本项目GUI绘制，没有提取云顶特效贴图或音效。新增ShopPromotionEffects.cs和meta随源码提交，无新增外部运行媒体。

未测第二台电脑、全新克隆构建、长时压力与人类数值平衡。本机通过不代表接收端已验收；接收者需按上述入口验证开关商店、招募/合成/出售/部署与退出。

角色图片/动画复用已跟踪资源及meta；源石运行图 `Assets/StreamingAssets/ShopTrial/originium.png` 的来源、哈希和真实权利状态见[记录](ui-previews/shop/assets/originium-source.json)。没有新增外部运行依赖。不自动合并main、ZIP或Releases，构建与日志不入Git。
