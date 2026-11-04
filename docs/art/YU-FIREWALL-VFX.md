# 余：偏红火墙

2026-11-04，人类确认项目外动态方向，并要求“火焰颜色改成偏红，参考原作，然后直接做”。预览目录为`E:/珍贵资料/昨日圆车_美术预览/2026-11-03_余火墙-v1`；预览不是Unity实机，游戏实机图与测试结果见QA。

## 表现与连接

`DefenderSkillVfxView`只在余真实火墙出现时创建内部`YuFirewallVfx`，从`DefenderWallActive`、`DefenderWallPoint`、`WallNormal`与模拟`Clock`读取位置、方向和生命周期。技能起势仍由原作角色动作负责；1.6秒原起势之后沿线点燃。深红/朱红火面、错峰上卷火舌、根部热光、火星、薄烟分层绘制，结束后收焰并留下短暂余烬。暂停冻结；重开/切换清理。

每堵墙六个共享纹理网格批次，复用25帧火焰；不是把每个火舌生成独立物体，也没有新增大型逐帧图集。`YuFirewall.shader`保留纹理Alpha与明暗，映射为偏红色阶，使用深度测试。根部保留少量暖光，不把整墙变成黄色亮片；前焰低而薄，后焰高，避免遮住人物主体。

[单人测试实机](yu-firewall-actual.png)为1280×720的GPU相机采样，不含IMGUI；墙的位置和朝向来自该次真实技能事件。地图上高台等不透明地形会遮挡其后的低处火焰，不能把穿透地形的屏幕贴纸当作目标表现。

原有中心与法线、原表现约±5.76世界单位的线段分布保持。墙的游戏判定仍由战斗模型决定；视觉宽度不是新碰撞规则。伤害、范围、阻挡、灼燃、消弹、技力、持续时间和其他五名守军的特效没有修改。没有编辑场景、Prefab、Packages或ProjectSettings。

## 素材与权利

- 火焰：复用`Assets/Art/VFX/Skills/Talulah/Resources/FireRainEmbers.png`，highwizard / Animated flame，CC0；依据`source-records/fire-rain-embers.json`。
- 薄烟：复用`Assets/Art/VFX/Skills/Talulah/Resources/TalulahAttackSmoke.png`，Kenney Particle Pack，CC0；依据`TALULAH-ATTACK-VFX.md`与原粒子记录。没有再次复制PNG。
- 根部软光、余烬、分层网格、颜色和时序为本次程序组合；没有新下载或AI图片。新增运行依赖仅私有Shader及代码，连同meta提交。
- 余D22原角色与技能动作保持，来源仍见`source-records/defender-batch4-approved.json`；原作角色不标为CC0，不将用户上传授权写成权利人授权。

## 验证边界

实际结果以`../QA.md`本轮记录为准。需要分别检查真实技能起势/位置、两个墙法线、实际偏红GPU像素、连续帧、暂停、收尾与重开；构建和独立Player闭环不能替代人工对观感的最终判断。不声称已获得原作完整特效或逐像素还原，也没有进行整体性能优化。
