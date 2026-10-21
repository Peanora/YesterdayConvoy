# 美术需求 Checklist

## 2026-10-20 塔露拉V2

- [x] 新头部/卡面、独立身体与手臂部件、连续腿部和挥剑动作已接入；实际验证见QA。
- [x] CC0火流32帧/命中16帧，横向全场火流/残焰/暖光，暂停同步。
- [ ] 人工确认所有路径方向、步幅与特效密度；脚踝/脚趾、转身、多方向完整动作不在本轮。


## 2026-10-20 塔露拉范围

- [x] 单张卡面、八帧行走、两姿势普通攻击、两姿势施法已接入可切换试验关。
- [x] 火雨全图伤害、技力、血条、预警/落雨/命中与暂停连接；验证见QA。
- [ ] 人工确认步态与火雨密度、完整逐帧攻击/受击/死亡、正式羁绊与商店联动。


## 2026-10-19 M01范围补充

- [x] 克洛丝试验干员单姿势/血条/范围/受击与死亡反馈：已Unity/Player验证，不等于3类守军完整动作。
- [x] 维什戴尔持炮关键姿势/轻微后坐力/普通与强化弹道爆炸：已实机验证，不等于完整逐帧攻击或多目标范围伤害。
- [x] 每实体技力/六发弹药、手动技能按钮、暂停/2倍速/静音：已接真实逻辑。
- [x] 两张新AI PNG来源、Alpha、导入参数、许可边界记录：见GENERATION-2026-10-19和MANIFEST。
- [ ] 两角色完整独立动作集、更多体型样板、BGM/完整音量设置、正式多怪/商店UI；下方原任务书P0数量保留，局部原型不冒充全部完成。

更新 2026-10-18。每项对应 ASSET_MANIFEST.csv 的 REQ-ID。VERIFIED 及勾选只代表该项注明的验证范围，资源库导入不代表玩法实现；单卡原型 UI 的实机验证也不代表完整正式 UI 或人工输入验收。P0 数量是任务书建议上限，法系/辅助可复用或在 P1 扩充。

## A 怪物战斗

- [ ] **REQ-A-01** [P0] 普通近战怪物 Sprite — CANDIDATE； 两套路线已有候选，主角色未选定，未导入

- [ ] **REQ-A-02** [P0] 普通远程怪物 Sprite — CANDIDATE； 两套路线已有候选，主角色未选定，未导入

- [ ] **REQ-A-03** [P0] 普通肉盾怪物 Sprite — CANDIDATE； 两套路线已有候选，主角色未选定，未导入

- [ ] **REQ-A-04** [P0] 普通高速怪物 Sprite — CANDIDATE； 两套路线已有候选，主角色未选定，未导入

- [ ] **REQ-A-05** [P1] 法系怪物 Sprite — CANDIDATE； 两套路线已有候选，主角色未选定，未导入

- [ ] **REQ-A-06** [P1] 辅助召唤怪物 Sprite — CANDIDATE； 两套路线已有候选，主角色未选定，未导入

- [ ] **REQ-A-07** [P0] 精英怪物 Sprite — CANDIDATE； 两套路线已有候选，主角色未选定，未导入

- [ ] **REQ-A-08** [P0] 首领怪物 Sprite — CANDIDATE； 两套路线已有候选，主角色未选定，未导入

- [ ] **REQ-A-09** [P0] 怪物生成视觉效果 — NOT_STARTED； 

- [ ] **REQ-A-10** [P0] 怪物脚底接触阴影 — NOT_STARTED； 

- [ ] **REQ-A-11** [P0] 怪物选中高亮 — NOT_STARTED； 

- [ ] **REQ-A-12** [P0] 怪物受击反馈 — NOT_STARTED； 

- [ ] **REQ-A-13** [P0] 怪物死亡效果 — NOT_STARTED； 

- [ ] **REQ-A-14** [P0] 怪物状态图标 — NOT_STARTED； 

- [ ] **REQ-A-15** [P0] 怪物生命条 — CANDIDATE； 现有几何/OnGUI 占位，可复用；不算正式美术完成

- [ ] **REQ-A-16** [P0] 普通怪物待机 Idle — NOT_STARTED； Two-character workflow in Phase 5; source animations or pose/Transform animation as needed

- [ ] **REQ-A-17** [P0] 普通怪物移动 Move — NOT_STARTED； Two-character workflow in Phase 5; source animations or pose/Transform animation as needed

- [ ] **REQ-A-18** [P0] 普通怪物攻击 Attack — NOT_STARTED； Two-character workflow in Phase 5; source animations or pose/Transform animation as needed

- [ ] **REQ-A-19** [P0] 普通怪物受击 Hit — NOT_STARTED； Two-character workflow in Phase 5; source animations or pose/Transform animation as needed

- [ ] **REQ-A-20** [P0] 普通怪物死亡 Death — NOT_STARTED； Two-character workflow in Phase 5; source animations or pose/Transform animation as needed

## B 卡牌

- [ ] **REQ-B-01** [P0] 每种怪物卡牌立绘 — AI_PENDING； 条件性缺口，先定风格并复用合法现成资源；未生成

- [ ] **REQ-B-02** [P0] 普通卡牌底板 — NOT_STARTED； 

- [ ] **REQ-B-03** [P0] 精英卡牌底板 — NOT_STARTED； 

- [ ] **REQ-B-04** [P0] 首领卡牌底板 — NOT_STARTED； 

- [x] **REQ-B-05** [P0] 卡牌费用显示 — VERIFIED；当前底栏单卡显示招募费 5；不代表部署消费实现

- [x] **REQ-B-06** [P0] 卡牌名称区域 — VERIFIED；当前底栏单卡实机可读，保留认可卡面

- [ ] **REQ-B-07** [P0] 多羁绊展示区 — NOT_STARTED； 

- [ ] **REQ-B-08** [P0] 技能标识 — NOT_STARTED； 

- [ ] **REQ-B-09** [P0] 星级展示 — NOT_STARTED； 

- [ ] **REQ-B-10** [P0] 卡牌选中状态 — NOT_STARTED； 

- [ ] **REQ-B-11** [P0] 不可部署状态 — NOT_STARTED； 

- [x] **REQ-B-12** [P0] 部署冷却遮罩 — VERIFIED；单卡原型真实 CD 遮罩，Player HUD 已查看；全输入人工验收未完成

- [x] **REQ-B-13** [P0] 单场剩余部署次数 — VERIFIED；单卡原型读取真实剩余次数，重开重置；多卡布局未完成

- [ ] **REQ-B-14** [P0] 单次召唤数量标识 — NOT_STARTED； 

- [ ] **REQ-B-15** [P0] 卡牌描述面板 — NOT_STARTED； 

## C 守军

- [ ] **REQ-C-01** [P0] 普通近战守军 — CANDIDATE； 像素同系列造型候选；未导入

- [ ] **REQ-C-02** [P0] 远程守军 — CANDIDATE； 像素同系列造型候选；未导入

- [ ] **REQ-C-03** [P0] 坦克守军 — CANDIDATE； 像素同系列造型候选；未导入

- [ ] **REQ-C-04** [P1] 特殊技能守军 — NOT_STARTED； 

- [ ] **REQ-C-05** [P0] 守军攻击效果 — NOT_STARTED； 

- [ ] **REQ-C-06** [P0] 守军生命条 — NOT_STARTED； 

- [ ] **REQ-C-07** [P0] 守军受击反馈 — NOT_STARTED； 

- [ ] **REQ-C-08** [P0] 守军死亡效果 — NOT_STARTED； 

- [ ] **REQ-C-09** [P0] 守军技能预警 — NOT_STARTED； 

- [ ] **REQ-C-10** [P0] 守军站位标识 — NOT_STARTED； 

## D 地图

- [ ] **REQ-D-01** [P0] 远景背景 — CANDIDATE； 预渲染投影需测试；未导入地图

- [x] **REQ-D-02** [P0] 中景建筑山体 — VERIFIED（Demo 工业围墙/设备）；CC0 实体模型接入并经 Player 查看，非完整城市远景；风格观感待人类验收

- [x] **REQ-D-03** [P0] 前景装饰 — VERIFIED（Demo）；原生排水沟、边缘护沿与导向线，接入 Player

- [x] **REQ-D-04** [P0] 地面 Tileset — VERIFIED（3D 模块/贴图）；CC0 混凝土/沥青用于原生方格与高台，不是 2D Tilemap 资产

- [x] **REQ-D-05** [P0] 战斗通行区域 — VERIFIED（Demo）；按原路线铺设地面，不增加阻挡或部署规则

- [x] **REQ-D-06** [P0] 怪物出生区域 — VERIFIED（Demo）；红色战术线框入口，原生成/移动规则保持

- [ ] **REQ-D-07** [P0] 守军站位区域 — NOT_STARTED； 

- [x] **REQ-D-08** [P0] 蓝门 — VERIFIED（Demo）；蓝色战术线框出口，穿门单次结算通过

- [ ] **REQ-D-09** [P0] 蓝门激活受影响反馈 — NOT_STARTED； 

- [x] **REQ-D-10** [P0] 防御值 UI — VERIFIED；顶部盾形标志与实际防御数，实机突破 8→7；最终观感待人类验收

- [ ] **REQ-D-11** [P0] 进蓝门效果 — NOT_STARTED； 

- [x] **REQ-D-12** [P0] 通行路线标识 — VERIFIED（Demo）；区分地表与克制边线，实机画面已检查

- [x] **REQ-D-13** [P0] 可部署区域标识 — VERIFIED；当前红门范围线/角色预览和颜色反馈；不是自由地格部署

- [x] **REQ-D-14** [P0] 非法部署区域反馈 — VERIFIED；错误入口预览变红/文字提示且不扣资源；全输入人工验收未完成

## E 羁绊

- [ ] **REQ-E-01** [P0] 羁绊通用图标模板 — NOT_STARTED； 

- [ ] **REQ-E-02** [P0] 首批四种羁绊图标 — IMPORTED；ICON-SYNERGY-* 四个占位图形，尚未确认正式羁绊名称或接入 UI

- [ ] **REQ-E-03** [P0] 羁绊专属配色 — NOT_STARTED； 

- [ ] **REQ-E-04** [P0] 羁绊激活状态 — NOT_STARTED； 

- [ ] **REQ-E-05** [P0] 羁绊未激活状态 — NOT_STARTED； 

- [ ] **REQ-E-06** [P0] 羁绊进度显示 — NOT_STARTED； 

- [ ] **REQ-E-07** [P0] 羁绊等级显示 — NOT_STARTED； 

- [ ] **REQ-E-08** [P0] 羁绊详情面板 — NOT_STARTED； 

- [ ] **REQ-E-09** [P0] 羁绊触发特效 — NOT_STARTED； 

- [ ] **REQ-E-10** [P0] 多羁绊排列规范 — NOT_STARTED； 

## F 商店阵容

- [ ] **REQ-F-01** [P0] 商店背景面板 — NOT_STARTED； 

- [ ] **REQ-F-02** [P0] 商品卡牌容器 — NOT_STARTED； 

- [ ] **REQ-F-03** [P0] 购买按钮 — NOT_STARTED； 

- [ ] **REQ-F-04** [P0] 价格显示 — NOT_STARTED； 

- [ ] **REQ-F-05** [P0] 商店刷新按钮 — NOT_STARTED； 

- [ ] **REQ-F-06** [P0] 当前费用显示 — NOT_STARTED； 

- [ ] **REQ-F-07** [P0] 费用不足提示 — NOT_STARTED； 

- [ ] **REQ-F-08** [P0] 首领稀有度标识 — NOT_STARTED； 

- [ ] **REQ-F-09** [P0] 购买反馈 — NOT_STARTED； 

- [ ] **REQ-F-10** [P0] 卡牌详情弹窗 — NOT_STARTED； 

- [ ] **REQ-F-11** [P0] 阵容区域 — NOT_STARTED； 

- [ ] **REQ-F-12** [P0] 羁绊统计区域 — NOT_STARTED； 

- [ ] **REQ-F-13** [P0] 当前已选单位 — NOT_STARTED； 

- [ ] **REQ-F-14** [P0] 星级显示 — NOT_STARTED； 

- [ ] **REQ-F-15** [P0] 升星反馈 — NOT_STARTED； 

- [ ] **REQ-F-16** [P0] 按钮悬停按下效果 — NOT_STARTED； 

## G 战斗 HUD

- [ ] **REQ-G-01** [P0] 当前费用 — NOT_STARTED； 

- [x] **REQ-G-02** [P0] 守方剩余防御值 — VERIFIED；顶部实际状态绑定，穿门单次结算/重开回归通过

- [x] **REQ-G-03** [P0] 怪物卡牌栏 — VERIFIED；当前单卡可选中并手动部署，显示独立部署费/招募费/次数/CD；多卡/商店/拖拽未实现

- [x] **REQ-G-04** [P0] 部署冷却显示 — VERIFIED；单卡原型数字/遮罩读取同一模拟时钟，暂停冻结

- [x] **REQ-G-05** [P0] 单场剩余部署次数 — VERIFIED；单卡原型实际次数与重置，Play Mode/Player 通过

- [ ] **REQ-G-06** [P0] 单次召唤数量 — NOT_STARTED； 

- [ ] **REQ-G-07** [P0] 首领主动技能按钮 — NOT_STARTED； 

- [ ] **REQ-G-08** [P0] 首领技能冷却遮罩 — NOT_STARTED； 

- [ ] **REQ-G-09** [P0] 羁绊状态 — NOT_STARTED； 

- [ ] **REQ-G-10** [P0] 技能 Tooltip — NOT_STARTED； 

- [ ] **REQ-G-11** [P0] 暂停设置入口 — CANDIDATE；原型暂停按钮/空格已接入，设置未实现；人工键盘焦点验收未完成

- [ ] **REQ-G-12** [P0] 胜利失败反馈 — NOT_STARTED； 

- [ ] **REQ-G-13** [P0] 部署预览选中反馈 — NOT_STARTED； 

## H 技能特效

- [ ] **REQ-H-01** [P0] 普通近战命中 — NOT_STARTED； 纹理候选见 VFX-TEX-*；尚无已实现 ParticleSystem/Prefab

- [ ] **REQ-H-02** [P0] 普通远程弹道 — NOT_STARTED； 纹理候选见 VFX-TEX-*；尚无已实现 ParticleSystem/Prefab

- [ ] **REQ-H-03** [P0] 魔法攻击 — NOT_STARTED； 纹理候选见 VFX-TEX-*；尚无已实现 ParticleSystem/Prefab

- [ ] **REQ-H-04** [P0] 爆炸范围攻击 — NOT_STARTED； 纹理候选见 VFX-TEX-*；尚无已实现 ParticleSystem/Prefab

- [ ] **REQ-H-05** [P0] 治疗增益 — NOT_STARTED； 纹理候选见 VFX-TEX-*；尚无已实现 ParticleSystem/Prefab

- [ ] **REQ-H-06** [P0] 减速控制 — NOT_STARTED； 纹理候选见 VFX-TEX-*；尚无已实现 ParticleSystem/Prefab

- [ ] **REQ-H-07** [P1] 召唤出现 — NOT_STARTED； 纹理候选见 VFX-TEX-*；尚无已实现 ParticleSystem/Prefab

- [ ] **REQ-H-08** [P1] 首领主动技能 — NOT_STARTED； 纹理候选见 VFX-TEX-*；尚无已实现 ParticleSystem/Prefab

- [ ] **REQ-H-09** [P1] 怪物死亡消散 — NOT_STARTED； 纹理候选见 VFX-TEX-*；尚无已实现 ParticleSystem/Prefab

- [ ] **REQ-H-10** [P1] 蓝门通过反馈 — NOT_STARTED； 纹理候选见 VFX-TEX-*；尚无已实现 ParticleSystem/Prefab

## I 技能图标

- [x] **REQ-I-01** [P0] 物理攻击 — VERIFIED；ICON-PHYSICAL 已入库，具体页面与验证范围见 ICON-PHYSICAL；仅对应资源文件验证通过，界面/玩法未接入

- [x] **REQ-I-02** [P0] 魔法攻击 — VERIFIED；ICON-MAGIC 已入库，具体页面与验证范围见 ICON-MAGIC；仅对应资源文件验证通过，界面/玩法未接入

- [x] **REQ-I-03** [P0] 防御 — VERIFIED；ICON-DEFENSE 已入库，具体页面与验证范围见 ICON-DEFENSE；仅对应资源文件验证通过，界面/玩法未接入

- [x] **REQ-I-04** [P0] 生命 — VERIFIED；ICON-HEALTH 已入库，具体页面与验证范围见 ICON-HEALTH；仅对应资源文件验证通过，界面/玩法未接入

- [x] **REQ-I-05** [P0] 移动速度 — VERIFIED；ICON-SPEED 已入库，具体页面与验证范围见 ICON-SPEED；仅对应资源文件验证通过，界面/玩法未接入

- [x] **REQ-I-06** [P0] 治疗 — VERIFIED；ICON-HEALING 已入库，具体页面与验证范围见 ICON-HEALING；仅对应资源文件验证通过，界面/玩法未接入

- [ ] **REQ-I-07** [P0] 护盾 — NOT_STARTED； 

- [x] **REQ-I-08** [P0] 减速 — VERIFIED；ICON-SLOW 已入库，具体页面与验证范围见 ICON-SLOW；仅对应资源文件验证通过，界面/玩法未接入

- [x] **REQ-I-09** [P0] 眩晕 — VERIFIED；ICON-STUN 已入库，具体页面与验证范围见 ICON-STUN；仅对应资源文件验证通过，界面/玩法未接入

- [x] **REQ-I-10** [P0] 召唤 — VERIFIED；ICON-SUMMON 已入库，具体页面与验证范围见 ICON-SUMMON；仅对应资源文件验证通过，界面/玩法未接入

- [x] **REQ-I-11** [P0] 回费 — VERIFIED；ICON-REFUND 已入库，具体页面与验证范围见 ICON-REFUND；仅对应资源文件验证通过，界面/玩法未接入

- [x] **REQ-I-12** [P0] 冷却 — VERIFIED；ICON-COOLDOWN 已入库，具体页面与验证范围见 ICON-COOLDOWN；仅对应资源文件验证通过，界面/玩法未接入

- [x] **REQ-I-13** [P0] 星级 — VERIFIED；ICON-STAR 已入库，具体页面与验证范围见 ICON-STAR；仅对应资源文件验证通过，界面/玩法未接入

- [ ] **REQ-I-14** [P0] 羁绊激活 — NOT_STARTED； 

- [ ] **REQ-I-15** [P1] 部署次数 — NOT_STARTED； 

- [ ] **REQ-I-16** [P1] 群体召唤 — NOT_STARTED； 

- [ ] **REQ-I-17** [P1] 首领技能 — NOT_STARTED； 

- [ ] **REQ-I-18** [P1] 穿门防御损失 — NOT_STARTED； 

## J 通用 UI

- [ ] **REQ-J-01** [P1] 主菜单 — NOT_STARTED； 

- [ ] **REQ-J-02** [P1] 开始游戏按钮 — CANDIDATE； 现有几何/OnGUI 占位，可复用；不算正式美术完成

- [ ] **REQ-J-03** [P1] 设置菜单 — NOT_STARTED； 

- [ ] **REQ-J-04** [P1] 怪物图鉴 — NOT_STARTED； 

- [ ] **REQ-J-05** [P1] 关卡选择如需要 — NOT_STARTED； 

- [ ] **REQ-J-06** [P1] 胜利界面 — NOT_STARTED； 

- [ ] **REQ-J-07** [P1] 失败界面 — NOT_STARTED； 

- [ ] **REQ-J-08** [P1] 重新开始按钮 — CANDIDATE； 现有几何/OnGUI 占位，可复用；不算正式美术完成

- [x] **REQ-J-09** [P0] 基础字体资源 — VERIFIED；FONT-NOTO-SC 静态 Regular，尚未绑定现有 Demo；仅对应资源文件验证通过，界面/玩法未接入

- [ ] **REQ-J-10** [P1] 通用面板背景 — NOT_STARTED； 

- [ ] **REQ-J-11** [P1] 通用按钮 — NOT_STARTED； 

- [ ] **REQ-J-12** [P1] Tooltip 模板 — NOT_STARTED； 

- [ ] **REQ-J-13** [P1] 数字状态提示样式 — NOT_STARTED； 

## K 音频

- [ ] **REQ-K-01** [P0] UI 点击 — IMPORTED；SFX-CLICK1 点击素材；尚未接到按钮

- [ ] **REQ-K-02** [P0] 购买成功 — NOT_STARTED； 

- [ ] **REQ-K-03** [P0] 商店刷新 — NOT_STARTED； 

- [ ] **REQ-K-04** [P0] 部署成功 — NOT_STARTED； 

- [ ] **REQ-K-05** [P0] 费用不足 — NOT_STARTED； 

- [ ] **REQ-K-06** [P0] 普通攻击 — NOT_STARTED； 

- [ ] **REQ-K-07** [P0] 魔法攻击 — NOT_STARTED； 

- [ ] **REQ-K-08** [P0] 攻击命中 — NOT_STARTED； 

- [ ] **REQ-K-09** [P0] 单位死亡 — NOT_STARTED； 

- [ ] **REQ-K-10** [P0] 首领技能 — NOT_STARTED； 

- [ ] **REQ-K-11** [P0] 羁绊激活 — NOT_STARTED； 

- [ ] **REQ-K-12** [P0] 蓝门通过 — NOT_STARTED； 

- [ ] **REQ-K-13** [P0] 胜利 — NOT_STARTED； 

- [ ] **REQ-K-14** [P0] 失败 — NOT_STARTED； 

- [ ] **REQ-K-15** [P0] 背景音乐 — IMPORTED；BGM-DUNGEON 背景氛围候选，尚未试听/接入

## P1 扩充

- [ ] **REQ-P1-01** [P1] 普通怪物补至六种 — NOT_STARTED； 

- [ ] **REQ-P1-02** [P1] 精英补至两种 — NOT_STARTED； 

- [ ] **REQ-P1-03** [P1] 首领第二种可选 — NOT_STARTED； 

- [ ] **REQ-P1-04** [P1] 守军补至四种 — NOT_STARTED； 

- [ ] **REQ-P1-05** [P1] 技能状态图标补至二十 — NOT_STARTED； 

- [ ] **REQ-P1-06** [P1] 通用特效补至八种 — NOT_STARTED； 

- [ ] **REQ-P1-07** [P1] 完整战斗反馈 — NOT_STARTED； 

## 本次明确授权的卡牌与换装

- [x] 认可蔑视表情卡图原样保存；5 费登记并在卡牌显示。
- [x] 同系列战场角色、防御炮、红门、蓝门、地面实际接入 InitialDemo。
- [x] 卡牌 Prefab、内置中文字体、移动摇摆、受击闪色与暂停检查。
- [ ] 其他卡牌/角色和完整逐帧动画；购买/部署费用事务与正式技能。

详见 GENERATION-2026-10-17.md / ART_PROGRESS.md；上方首次调查的未完成数量不因这次六张图自动全部勾选。
