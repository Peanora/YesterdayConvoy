# 美术需求 Checklist

## 动画样板第二、三阶段（2026-10-17 工程验收）

- [x] U01 独立身体部件、关节覆盖与 52 个持久 Sprite。
- [x] 可编辑 U01Visual Prefab；原卡面与头部像素保留。
- [x] 接触/压低/经过/抬起四关键姿势、起停、暂停、重开。
- [x] 无腿灰焰独立 Prefab，与双足共同运动输入、独立表现。
- [x] 实际 Unity 测试、Windows 构建和图形 Player 证据，见 QA。
- [ ] 人类最终观感验收；第二机离线与多单位性能验证。

此清单只确认动画样板工程范围，灰焰尚非战斗卡，不据此勾选下方整套 P0 内容。下方保留最初资源调查进度，当前具体接入状态优先看 STATUS/ART_PROGRESS 与清单 Usage。

2026-10-17。每项对应 ASSET_MANIFEST.csv 的 REQ-ID。VERIFIED 及勾选仅表示所引用素材文件通过验证，不代表玩法或 UI 已实现。P0 数量是任务书建议上限，法系/辅助可复用或在 P1 扩充。主场景已有几何/OnGUI 仍是占位，未算正式美术完成。

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

- [ ] **REQ-B-05** [P0] 卡牌费用显示 — NOT_STARTED； 

- [ ] **REQ-B-06** [P0] 卡牌名称区域 — NOT_STARTED； 

- [ ] **REQ-B-07** [P0] 多羁绊展示区 — NOT_STARTED； 

- [ ] **REQ-B-08** [P0] 技能标识 — NOT_STARTED； 

- [ ] **REQ-B-09** [P0] 星级展示 — NOT_STARTED； 

- [ ] **REQ-B-10** [P0] 卡牌选中状态 — NOT_STARTED； 

- [ ] **REQ-B-11** [P0] 不可部署状态 — NOT_STARTED； 

- [ ] **REQ-B-12** [P0] 部署冷却遮罩 — NOT_STARTED； 

- [ ] **REQ-B-13** [P0] 单场剩余部署次数 — NOT_STARTED； 

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

- [ ] **REQ-D-02** [P0] 中景建筑山体 — CANDIDATE； 预渲染投影需测试；未导入地图

- [ ] **REQ-D-03** [P0] 前景装饰 — CANDIDATE； 预渲染投影需测试；未导入地图

- [ ] **REQ-D-04** [P0] 地面 Tileset — CANDIDATE； 预渲染投影需测试；未导入地图

- [ ] **REQ-D-05** [P0] 战斗通行区域 — CANDIDATE； 现有几何/OnGUI 占位，可复用；不算正式美术完成

- [ ] **REQ-D-06** [P0] 怪物出生区域 — CANDIDATE； 现有几何/OnGUI 占位，可复用；不算正式美术完成

- [ ] **REQ-D-07** [P0] 守军站位区域 — NOT_STARTED； 

- [ ] **REQ-D-08** [P0] 蓝门 — CANDIDATE； 现有几何/OnGUI 占位，可复用；不算正式美术完成

- [ ] **REQ-D-09** [P0] 蓝门激活受影响反馈 — NOT_STARTED； 

- [ ] **REQ-D-10** [P0] 防御值 UI — CANDIDATE； 现有几何/OnGUI 占位，可复用；不算正式美术完成

- [ ] **REQ-D-11** [P0] 进蓝门效果 — NOT_STARTED； 

- [ ] **REQ-D-12** [P0] 通行路线标识 — CANDIDATE； 现有几何/OnGUI 占位，可复用；不算正式美术完成

- [ ] **REQ-D-13** [P0] 可部署区域标识 — NOT_STARTED； 

- [ ] **REQ-D-14** [P0] 非法部署区域反馈 — NOT_STARTED； 

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

- [ ] **REQ-G-02** [P0] 守方剩余防御值 — CANDIDATE； 现有几何/OnGUI 占位，可复用；不算正式美术完成

- [ ] **REQ-G-03** [P0] 怪物卡牌栏 — NOT_STARTED； 

- [ ] **REQ-G-04** [P0] 部署冷却显示 — NOT_STARTED； 

- [ ] **REQ-G-05** [P0] 单场剩余部署次数 — NOT_STARTED； 

- [ ] **REQ-G-06** [P0] 单次召唤数量 — NOT_STARTED； 

- [ ] **REQ-G-07** [P0] 首领主动技能按钮 — NOT_STARTED； 

- [ ] **REQ-G-08** [P0] 首领技能冷却遮罩 — NOT_STARTED； 

- [ ] **REQ-G-09** [P0] 羁绊状态 — NOT_STARTED； 

- [ ] **REQ-G-10** [P0] 技能 Tooltip — NOT_STARTED； 

- [ ] **REQ-G-11** [P0] 暂停设置入口 — NOT_STARTED； 

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
