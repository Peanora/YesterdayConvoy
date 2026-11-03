# 盟约#04地图材质与特效来源

2026-10-31，按用户要求补地图特效及贴图质量。沿用实际地块、两条路线、传送时间、源石伤害和预算，视觉组件不写战斗数值。

## 既有贴图的实际接入

- 混凝土：ambientCG Concrete034 Color，1024×512，CC0。原始页面与许可见[既有Credits](CREDITS.md)，文件像素及导入设置保持。
- 金属面板：Quaternius Modular Sci-Fi Megakit Standard，Trim01/Trim02 Color及Normal，均为2048×2048、CC0。使用图集面板区域的UV缩放与偏移，同一区域采样颜色和法线，接入本图工事/顶盖/设备，不把整张图集任意拉伸成墙。原许可保留在[许可文件](licenses/quaternius-modular-scifi-standard.txt)。
- `StrongholdMapArt.asset`显式引用贴图和两个专用Shader，进入Resources依赖，避免编辑器可见而Player剥离。
- 贴图不是本轮新下载或AI生成；没有对旧贴图文件作放大、重采样或覆盖。既有资产清单作者/许可条目保持，本文件记录本图新增使用范围。

| 实际复用文件 | SHA256 |
| --- | --- |
| concrete-color.jpg | `8ceb9186d990b31fce785e06fdc6974c160f1d27c43f6160266f164deba713aa` |
| trim-01-color.png | `a8f3271be2aa9c450ef5ef85c15bf8bcaeeaa48f5fc616df12d1334a96e78605` |
| trim-01-normal.png | `1aec7ad47a642fc68216b5a3119341b7dca038c1fc9493df8a15ad4aa27ddbcf` |
| trim-02-color.png | `7b13c6a7d82b434314237c4b1930b8e12ddc69a70ffef28d73cbd178d3ff6293` |
| trim-02-normal.png | `7faaa7059192f43aded9cc2f93917dd54fd14545509c37318d4e6bf0a8104960` |

## 本项目制作的表现

`StrongholdSurface.shader`为本轮代码：材质颜色、图集法线、金属/粗糙度、世界坐标微小脏污变化；地面用混凝土与轻微色差，金属用2K图集，不冒称新增整套4K PBR或烘焙AO。通风格栅、顶盖紧固点、警戒涂装、侧设备、管线和深色多面晶体由几何生成，均无新增碰撞体。

`StrongholdEnergy.shader`与`StrongholdMapVfx`为本轮代码：四块源石脉冲/上升碎光、感染单位与守军的橙色脚环、青色传送入口呼吸光、等待光柱/能量、中央出场扩散光和突破蓝光。使用一张动态网格与一个透明材质批量绘制；不是新增光源、Bloom后处理或原作粒子素材。没有旋转星星或绕行光弧。

准备阶段光效按本地真实时间活动；进入战斗后按战斗时钟，暂停冻结、重开清空、切图退出释放Mesh/材质。感染效果依据实际感染状态，传送效果依据实际等待/次数变化，不额外创建战斗实体。传送结束计数增加时出场脉冲一次，显示时间1秒。

范围仍是本图视觉升级，未修改原主场景、既有地图asset、Prefab、Packages、ProjectSettings或全局质量设置。未覆盖原作全部部署特效和音效，低配GPU/长时性能需实际测试。最新实机与验证见[地图试玩说明](../STRONGHOLD_MAP_04.md)和[QA](../QA.md)。
