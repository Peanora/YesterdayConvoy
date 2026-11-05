# V5 商店与作战界面

2026-11-06。人类已确认V5项目外看稿及具体接入范围。当前实现/验收状态以STATUS和QA的对应记录为准。

界面采用A的上方五格商店、C的同步招募清单与B的连续人物平台。顶部与羁绊使用分开的浅灰信息片；编队/备战有完整灰、绿、蓝、紫、金费用底板，并保留费用数字。持有卡常亮，可合成商品只在边缘以1.5秒周期轻闪，文字和费用底色不闪。合并恢复V3小肖像与银白/金色光流，不采用已被否定的V4大卡慢速合并。

左右出售区仅在整备拖卡时出现，显示真实Quote，松手执行原Sell；拖回或Esc取消。公开布局与输入同源，侧清单和原五格共用Offers[index]，不能产生第二套库存。购买、自动编队、九张基础牌、满席合成、费用、技能、路线均保持。游戏买入新类型且编队有空位时仍会补入编队，因此光流按真实落位播放，不照搬HTML示例的固定备战落位。

继续沿用IMGUI、1280×720逻辑坐标与TacticalViewport等比例转换，未引入UI框架或依赖。角色使用既有真实待机帧和透明RenderTexture，新增的UI/特效不会重建地图。

关键连接：Purchase/Commission前保存收藏实例与Rect快照，原模型一次提交后以最终Instance/Star生成纯表现流；效果不扣钱、不合成、不部署。新合并接管被消耗材料的旧表现，刷新不取消，卖出保留卡/重开/切换后清理。委托、八类原羁绊样板、感染主副形态、援军、承契、海嗣与巨像培养等收纳进可展开面板，不删除已有功能。

## 字体与权利

`Assets/Fonts/Resources/UI/YesterdayConvoyUI-{Regular,Semibold,Bold}.ttf`由Noto Sans SC可变源离线固定wght为400/600/700生成，非浏览器变量字重，也不依赖Windows字体。派生家族名Yesterday Convoy UI，不把源字形声称原创。每个字体保留31,036字形/30,890码点，三者实际轮廓不同；原版权和SIL OFL 1.1在字体元数据与同目录OFL.txt保留。原场景字体GUID没有替换。

源文件：https://github.com/google/fonts/tree/main/ofl/notosanssc 。源许可：https://raw.githubusercontent.com/google/fonts/main/ofl/notosanssc/OFL.txt 。字体实例化依据：https://fonttools.readthedocs.io/en/latest/varLib/instancer.html 。静态文件在工程外使用fontTools 4.68.0准备，工具未加入Unity Packages。源包、临时虚拟环境、无关看稿与日志不入游戏Assets。

购买/合并方向曾实际查看官方节目 https://www.bilibili.com/video/BV1kc1uBkE4s/?t=188 的材料亮起、弧线汇入与抵达闪亮。本作时序是已确认看稿参数：普通约0.63秒，二星0.65秒揭星/1.35秒结束，三星链段间0.69秒/2.04秒结束；不称原作逐帧复刻。地图、角色与旧素材的真实权利状态保持原来源记录。

## 验证入口

- EditMode原ShopTrialTests及全量逻辑。
- PlayMode的ShopMapSceneTests、ApprovedUiSceneTests和现有Campaign/两关闭环回归。
- 本轮独立Player显式参数`-approvedUiSmokeTest -demoEvidenceDir <路径>`：使用明确的指针夹具覆盖真实五格/侧清单、合并、Quote、取消、暂停部署并截图。不是完整自然通关，也不把程序指针夹具称为Windows鼠标人工验收。
- 不使用当前未路由的历史`-shopMapSmokeTest`冒充本轮验收。

没有自动合并main，不制作ZIP/Releases；本地EXE、源码提交与真实未测项在本轮QA中记录。
