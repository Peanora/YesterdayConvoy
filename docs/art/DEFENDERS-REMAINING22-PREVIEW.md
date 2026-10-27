# 剩余22名守军模型与动作预览

2026-10-23。用户明确选择继续制作清单剩余22名；本轮先完成项目外美术预览，不补首批8人的技能，不改Unity资源或战斗规则。全体角色与队友流程见[TODO_list](../../TODO_list.md)。首批8人的基础防线在独立[PR #17](https://github.com/Peanora/YesterdayConvoy/pull/17)，不属于本PR的Unity接入。

## 实际存在的资源

工作区：`E:/珍贵资料/昨日圆车_美术预览/2026-10-23_守军剩余22名-v1`。双击`index.html`离线看稿，22名/423段动作可切换、暂停、慢放和逐帧；同时展示放大与约128px角色高度参考。全体总览`defenders-remaining22-board.png`，基础/进阶12名总览`defenders-remaining12-board.png`，六星10名总览`defenders-sixstars10-board.png`。

各Dxx目录有透明`card.png`、1280×720 60fps `review.mp4`、多帧`review.gif`、角色`manifest.json`、`sources.json`与视频片段时间索引。实际取得66份默认原作正面/背面/基建模型变体，423段38147帧；1091个清单内RGBA PNG（含运行包卡面副本），另有22张独立透明卡面、22视频和22动图。PNG最大文件4678139字节，清单内PNG合计1240545392字节。没有用AI补画或将静态分镜称作视频；卡面是待机透明裁切，并非新绘高分辨率立绘。

## 角色与具体来源

| ID | 角色 | 默认原作模型 | 位置 | 具体资料页 |
| --- | --- | --- | --- | --- |
| D02 | 卡缇 | `char_209_ardign` | 地面 | [卡缇](https://prts.wiki/w/%E5%8D%A1%E7%BC%87) |
| D05 | 月见夜 | `char_283_midn` | 地面 | [月见夜](https://prts.wiki/w/%E6%9C%88%E8%A7%81%E5%A4%9C) |
| D06 | 翎羽 | `char_192_falco` | 地面 | [翎羽](https://prts.wiki/w/%E7%BF%8E%E7%BE%BD) |
| D11 | 安赛尔 | `char_212_ansel` | 高台 | [安赛尔](https://prts.wiki/w/%E5%AE%89%E8%B5%9B%E5%B0%94) |
| D13 | 蛇屠箱 | `char_150_snakek` | 地面 | [蛇屠箱](https://prts.wiki/w/%E8%9B%87%E5%B1%A0%E7%AE%B1) |
| D14 | 古米 | `char_196_sunbr` | 地面 | [古米](https://prts.wiki/w/%E5%8F%A4%E7%B1%B3) |
| D15 | 慕斯 | `char_185_frncat` | 地面 | [慕斯](https://prts.wiki/w/%E6%85%95%E6%96%AF) |
| D16 | 拉普兰德 | `char_140_whitew` | 地面 | [拉普兰德](https://prts.wiki/w/%E6%8B%89%E6%99%AE%E5%85%B0%E5%BE%B7) |
| D17 | 白雪 | `char_118_yuki` | 高台 | [白雪](https://prts.wiki/w/%E7%99%BD%E9%9B%AA) |
| D18 | 远山 | `char_109_fmout` | 高台 | [远山](https://prts.wiki/w/%E8%BF%9C%E5%B1%B1) |
| D19 | 调香师 | `char_181_flower` | 高台 | [调香师](https://prts.wiki/w/%E8%B0%83%E9%A6%99%E5%B8%88) |
| D20 | 苏苏洛 | `char_298_susuro` | 高台 | [苏苏洛](https://prts.wiki/w/%E8%8B%8F%E8%8B%8F%E6%B4%9B) |
| D21 | 荒芜拉普兰德 | `char_1038_whitw2` | 高台 | [荒芜拉普兰德](https://prts.wiki/w/%E8%8D%92%E8%8A%9C%E6%8B%89%E6%99%AE%E5%85%B0%E5%BE%B7) |
| D22 | 余 | `char_2026_yu` | 地面 | [余](https://prts.wiki/w/%E4%BD%99) |
| D23 | Mon3tr | `char_4179_monstr` | 高台→地面形态 | [Mon3tr](https://prts.wiki/w/Mon3tr) |
| D24 | 新约能天使 | `char_1041_angel2` | 地面 | [新约能天使](https://prts.wiki/w/%E6%96%B0%E7%BA%A6%E8%83%BD%E5%A4%A9%E4%BD%BF) |
| D25 | 斩业星熊 | `char_1044_hsgma2` | 地面 | [斩业星熊](https://prts.wiki/w/%E6%96%A9%E4%B8%9A%E6%98%9F%E7%86%8A) |
| D26 | 凛御银灰 | `char_1045_svash2` | 地面 | [凛御银灰](https://prts.wiki/w/%E5%87%9B%E5%BE%A1%E9%93%B6%E7%81%B0) |
| D27 | 圣聆初雪 | `char_1046_sbell2` | 高台 | [圣聆初雪](https://prts.wiki/w/%E5%9C%A3%E8%81%86%E5%88%9D%E9%9B%AA) |
| D28 | 望 | `char_2027_wang` | 高台 | [望](https://prts.wiki/w/%E6%9C%9B) |
| D29 | 塞雷娅 | `char_202_demkni` | 地面 | [塞雷娅](https://prts.wiki/w/%E5%A1%9E%E9%9B%B7%E5%A8%85) |
| D30 | 玛恩纳 | `char_4064_mlynar` | 地面 | [玛恩纳](https://prts.wiki/w/%E7%8E%9B%E6%81%A9%E7%BA%B3) |

具体meta/骨骼/atlas/纹理地址与原文件SHA在各sources.json及ACQUISITION.json；不是搜索页或仅网站主页。

## 动作与未做项

- 本批22名战斗文件均无Move；基建Move单独展示，不直接混入战斗行走。
- 安赛尔、调香师、苏苏洛、塞雷娅和医疗Mon3tr的Attack为治疗。Mon3tr采用干员版，保留技能战斗形态，未用凯尔希旧召唤物代替。
- 玛恩纳与圣聆初雪原文件没有常规Attack；保留技能/攻击阶段，不将技能循环冒充普攻。凛御银灰与望的多套基础动作、原生技能阶段完整保留。新约能天使保留起手/循环/收手及多方向技能阶段。
- 普通动作共用固定画幅与根点；部署和不同技能组分组取景，保留scale/pivot换算，未逐帧裁切居中。右侧大特效可能超出固定预览面板，左侧可看动作全貌；这不等于图集切片裁切。
- 原模型自带效果随动作保留；没有另做完整投射物、棋子、召唤实体或场景后处理。各角色战斗机制、AI和数值未实现；动作切换不代表已有Unity动画状态机。

## 发现的问题与实际修复

视觉复核翎羽Move发现基建贴图错位。实际核对22份基建PNG尺寸均小于atlas声明尺寸；原UV坐标按声明尺寸记录。预览渲染器现先将纹理匹配到声明画布，再导出这22份基建Move，原始文件与SHA保留。此操作恢复坐标对应，不提高原图分辨率或生成细节。TEXTURE-SIZE-AUDIT.json与BASE-RENDER-REPORT.json记录核对和重导出，旧有错位的中间图/视频不计最终证据。

## 真实检查

- 实际解析66份模型，原模型版本均Spine3.8.99；本地预览工具逐段采样原骨骼曲线，198个原文件SHA再次核对。没有向Unity添加Spine包或运行库。
- 修复后ASSET-QA：全部423段38147帧的布局、页尺寸、哈希、RGBA、Alpha>16边缘/整段非空及行走多姿势检查通过。1091个清单内PNG最大2048，22张透明卡面最大256、四的倍数、保留透明边缘。
- 修复后VIDEO-QA：22个MP4完整ffmpeg解码，1280×720/60fps，合计41159视频帧；22个GIF确有多帧，不把编码进程返回0单独视作有效视频。
- 修复后VIEWER-QA：真实Chrome以file://加载22名/423段，所有图集实际加载成功、0JS错误。离线网页不需要网站登录或在线依赖。
- 已查看全体分组总览、Mon3tr实际页面、Mon3tr技能战斗姿态、圣聆初雪三技能、玛恩纳三技能的实际MP4帧，以及修复后的翎羽Move八姿势取样。已生成新约能天使攻击视频取样；未以看一张图替代完整人工动作质量验收。
- DELIVERY-QA核对原始来源及所有图集SHA、尺寸、Alpha、卡面和22套视频/GIF/清单的实际存在。记录在项目外，不将加工区或预览自动推送Git。

**本轮未运行Unity导入、EditMode、PlayMode或Windows构建；未改Assets/场景/Prefab/Packages/ProjectSettings，未更新EXE/ZIP/Releases。** 人类美术确认、Unity世界尺度/方向/切图、游戏机制、状态机、完整外置特效及接收端实机验证仍待做。技术QA通过不登记为人类批准。

## 来源、许可与制作连接

原角色、贴图及动画归《明日方舟》原权利人，具体画师/动画师未确认；非CC0，开放修改/游戏分发/公开再分发许可未确认。PRTS是具体来源而非开放授权证明；用户的非商业用途或Git上传指令不等于权利人授权。保留本地Spine许可/README；不分发加工运行库。

制作连接为：具体角色页与meta→对应默认骨骼/atlas/纹理及SHA→原曲线采样与声明纹理尺寸匹配→固定分组根点的透明图集→清单驱动的HTML/MP4/GIF→帧、Alpha、解码及浏览器检查。没有新增游戏关键类或更改战斗接口；确认接入后，实际使用的运行素材与必要meta将按持续规则随源码提交。
