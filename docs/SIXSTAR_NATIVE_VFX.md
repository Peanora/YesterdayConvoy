# 六星原几何特效接入（2026-11-14）

D21–D26、D28–D30、D32–D35 改用原 Mesh、ParticleSystem、Animator、纹理和独立材质。D27 保留已认可的分层帧路径。这里记录运行接入与实际验证范围，不表示用户已认可全部角色达到原客户端观感。

## 运行连接

`ArtPreparation` 按本场角色预载 `OriginalNativeFxCatalog` 的自包含 Resources bundle 闭包。`DefenderSkillVfxView` 使用真实技能、攻击、棋子伤害与剑气历史事件，按稳定事件 ID 持有实例；`OriginalNativeFxLayer` 手动推进原粒子、动画、UV 和材质时钟，暂停时不推进。Player 不依赖作者电脑的加工目录。

原材质先保留纹理引用，再切换 GLES→D3D 的 shader，并从原序列化字段恢复 float/color/ST/keywords。不能从不支持的 Android shader 用 getter 读取值：实际曾得到0并变黑。19个专用 shader 族保留原公式、混合与深度状态；两个 Unity stock 族使用真实 stock Material 引用。没有用统一曝光或补色遮盖资源问题。

身体挂点按正在显示的正背动作帧读取；SpawnLocation 和 FollowMountPoint 的枚举分开解释。命中使用真实目标，未持有的命中层在结算时固定位置。猎狗和源石虫的静态 Hit 点已从匹配当前身体源的原 Transform 核实；其他怪物尚未取得原点，不冒称其后备位置是原挂点。

D32 使用正确的赤刃明霄陈 `char_1050_chen3`。六条原材质尾迹沿实际剑气历史生成网格，保留各自寿命、宽度、渐变、采样距离和真实转角。该适配网格尚未与原 TrailRenderer 做逐顶点等同验证，不能把两类原 shader 的隔离 GPU 对照外推成整条历史网格完全一致。

## 逐事件纠正

- 银灰的 `range_01` 由原 BirdAura/BirdAura_Self 各持有一份，随本体/联动目标的技能生命周期结束；攻击只发原出手层。旧代码按每次攻击累积永久冰场，实机隔离确认同时叠了6份，已撤销。原 OnFinishAnime、结束参数和回收延迟单独恢复。
- 能天使普通与S3枪口按原 ON_SPELL_ON 开火时刻播放；五连射一组去重，弹体在各次发射帧固定真实 Muzzle 起点。原弹体只确证 TARGET 追踪，未伪称恢复其未核的目标挂点默认算法。
- 望新增原棋子触发/受击资源，绑定成功伤害结算事件；无限持有范围不再被旧采样窗口强制截断。
- 克莱门莎读取原 subFXDelayConfigs 的分段延迟；四方向范围按原基轴和初始旋转量化，不能连续转成菱形。
- 安洁莉娜保留原 LineRenderer 材质、宽度与UV，几何按本作既有径向索敌范围表达为战术格边界；未默改成原客户端19格索敌。凯尔希保留完整原3D范围与前后层。
- 余的可见火墙在保存棋盘边缘结束，避免在水面继续堆格；原伤害墙逻辑范围未改。

## 来源与处理

来源、官方版本/索引校验、历史匿名依赖限制及原包哈希保留于 `Assets/Data/ArtCatalogs/NativeDefenders/sources.json`。原媒体属 Hypergryph 专有资源；用户要求接入/上传不等于权利人授予开放再分发许可。

processed bundles 只做运行兼容处理：平台标记、原压缩纹理解码为RGBA32 level0/mip1、移除 GameObject→原MonoBehaviour组件引用。原脚本字段先保存在 catalog；处理审计逐一核对其余原对象、纹理像素与 typed 字段。首次加载旧序列化包仍有源脚本警告，不屏蔽日志；已去掉每次实例化重复刷警告的组件引用。

`OriginalNativeFxImportTools.Apply` 接受准备包路径，`EnsureBuiltinReferences` 在首次载入原包前保存 stock 引用，`Validate` 核对运行 catalog 与包闭包。Editor 隔离诊断仅在显式 `-nativeFxDiagnosticDir` 参数下运行。

## 验证与限制

实际测试、构建源码和独立 Player 结果统一见 `QA.md` 当日条目。源字段/离线编译、Editor相机录像、Player图形检查与用户观感验收分开记录。

最终运行源码b0a144ad，完整EditMode1160/1160、相关图形10/10在相同生产实现78b564d6上通过，之后只修开发版观察器。最终Windows构建0错误，独立Player全14位295/295、Map05/初雪30/30通过；均记录实际进程退出0和运行DLL。全角色视频采用本次源码的连续相机帧，Map05另用最终EXE重新录制。D23/D28/D35的60秒采集未覆盖完整尾段，D25中断后覆盖尾段，不能外推成全部自然结束与所有条件回调已验。

固定镜头、角色索敌朝向、本作技能/数值和关卡属于现有游戏适配，未以美术任务重写。完整原管理器、所有条件回调、D24未记录的空投前运动、D26无对应原骨的伙伴武器动作、其他目标的精确原挂点及第二台机器仍有限制；不称原客户端逐帧完全复刻。
