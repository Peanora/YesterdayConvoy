# 塔露拉普攻分层动效

2026-11-03用户确认talulah-attack-preview-v1.mp4。仅替换普攻表现，不修改人物动作、攻击间隔/伤害/命中时点、火雨、路线、场景、Prefab、包或工程设置。高难度商店买入的塔露拉共用。

EngagementView的私有PresentTalulahSlash转交内部TalulahAttackVfx，关闭旧等宽弧线和整块命中图。辅助类只读取CombatShot/Clock，绘制短火痕、连续火焰、接触细光、碎焰、弹道火星和上浮薄烟；未实际命中的事件不伪造命中爆燃。按目标的相机平面方向旋转特效，不旋转角色或碰撞体。暂停重复采样同一时刻，重开/切换清理；第一次真正出现塔露拉普攻时才创建资源，普通角色不额外加载此助手。

复用现有FireRainEmbers连续火焰25帧（highwizard / Animated flame，CC0，原记录source-records/fire-rain-embers.json）。烟雾拷贝现有Kenney Particle Pack的particle-smoke-01到Resources/TalulahAttackSmoke以进入Player；PNG原字节相同，仅新建独立meta GUID，没有移动原图或破坏引用。新私有TalulahAttackAdditive沿用原加法公式，ZTest LEqual，不改变FireRainAdditive，也不让普攻穿过实体地形。细光/局部暖反光由小型程序纹理和线绘制，不额外导出大图集。本轮无新AI图、无新原作媒体，原角色既有权利状态保持。

[Unity实际画面](talulah-attack-actual.png)为GPU相机RenderTexture读取，不含IMGUI；项目外看稿不当作实机。现有受击变红、守军比例及射程圈未改。

实际验证和最终构建见[QA](../QA.md)；本轮不做整体性能优化、专属音效、全角色人工长时试玩或第二机验证。
