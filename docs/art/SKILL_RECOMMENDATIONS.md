# 游戏开发 Skill 筛选（2026-10-17）

已在线阅读下列社区仓库的具体 SKILL.md。当前只有既有 imagegen 用于实际图像编辑；下面四个候选均未安装，也未以安装后的 skill 在本项目验证。Skill 是工作指导，不包含可直接使用的角色骨骼、动作素材或自动动画成品。

| 顺序 | Skill / 具体来源 | 适合本项目的用途 | 范围限制 |
| --- | --- | --- | --- |
| 1 | [unity-animation](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/main/skills/unity/unity-animation/SKILL.md) | Animator 状态、参数、过渡、Blend Tree；组织待机/行走/攻击 | 偏 Animator/Mecanim，不能直接提供 2D 拆图或自然步态；Humanoid IK 不能直接套到 2D Sprite |
| 2 | [create-game-assets](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/main/skills/disciplines/create-game-assets/SKILL.md) | 风格一致、朝向、尺寸、透明度、切图、动作稳定性和实际场景检查 | 负责生产流程，图像编辑仍交给已安装 imagegen；生成结果不是验收证据 |
| 3 | [game-feel](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/main/skills/disciplines/game-feel/SKILL.md) | 缓动、重心反馈、受击、死亡与音画反馈 | 在基本动作正确后使用；摇晃、闪色不能替代走路动作 |
| 4 | [unity-scriptableobjects](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/main/skills/unity/unity-scriptableobjects/SKILL.md) | 卡牌、怪物和技能配置，区分配置与每个实例的运行状态 | 不为本原型默认引入事件总线或大规模架构改写 |

优先组合：unity-animation + create-game-assets；game-feel 用于后续打磨，ScriptableObjects 用于卡牌/招募内容。四个来源均属于同一 Apache-2.0 社区仓库，非 Unity 或 OpenAI 官方技能。Unity 项的文档基线是 6000.3，本工程仍锁定 6000.6.5f1，实际 API/包以本工程验证为准。

如用户之后要求安装，可用现有 skill-installer 按上面仓库路径选择性安装；此次仅筛选推荐，没有更改用户全局 skill、Unity 包清单或依赖。

另一个值得单独评估的是 [UnitySkills / Besty0728](https://github.com/Besty0728/Unity-Skills)：MIT，含 Unity 编辑器插件、REST 工具与 agent 指导，提供场景、Animator、逐帧采样和测试等编辑器操作。它比单个 Markdown skill 更重，需要向 Unity 工程安装插件并验证连接；不是仅安装一个 skill 就能自动制作自然动画，本轮未接入。

## 本次动画验收口径

旧版整图三个姿势循环已被用户否决其观感，不能因功能测试通过就称其自然。新的单角色原型采用原图多边形分层、髋/膝/踝关节与支撑/回收轨迹；先验证支撑脚不滑、脚掌不随小腿歪转、回收脚抬离地面、细小时间步下持续变姿势、路线转角切换正背面、暂停冻结和重开。

连续关节运动与换向可自动检查，素材接缝、体积和节奏是否自然仍须看实际 Player 动图与人工体验；它不等于专业逐帧或完整多方向骨骼动画。当前为单角色的定制验证，没有新增动画插件或额外引擎依赖。
