# M08 弩手：独立战斗接入

负责人霍利屹，任务 https://github.com/Peanora/YesterdayConvoy/issues/8 。当前用户已确认M08-Archer-v1，并于2026-10-22要求从M08开始把已分配角色接入游戏。基线main e5c1d7d，分支codex/monster-archer-combat；不假定当前用户为干宸骅，不代替主导者PR审查。

## 入口与行为

安装本地合法可用资源后，InitialDemo启动选择“弩手”。底部卡拖到红门部署，可空格暂停并暂停部署。独立测试初始12 DP、每次1 DP、3次、冷却1秒。弩手生命110、攻击22、护甲2、移动0.75、射程3.1、间隔1.8秒；守军生命140、攻击18、护甲3、射程3.2、间隔1秒、不阻挡。金币招募费用1独立保留。所有数值为本次试验，非原作数值或最终平衡。

攻击时停步，原版Attack的OnAttack事件32/60秒后发射，0.18秒弹道命中，按物理护甲计算最低1伤害；完整1秒动作收势后恢复移动。没有主动技能，不充SP/显示技能条或打开慢速选技界面。死亡播放原版1秒Die，不用待机帧冒充；受击使用现有红色闪烁。击杀后继续移动、穿门只结算一次，返回菜单或重开清理状态。

## 文件和数据连接

- 四份独立资产：Data/Units/M08.asset、Data/Cards/M08.asset、Data/Battles/M08.asset、Data/Resources/MonsterTests/M08.asset，分别保存稳定单位ID M08_ARCHER、卡ID CARD_M08、试验交战参数、部署和关卡快照。
- 复用Resources怪物发现入口；LocalMonsterArt加载可选本地图集；CardDefinition提供自己的卡面和战斗帧；DemoSpriteVisual只读取模拟/死亡展示时钟，不结算伤害或移动逻辑根节点。
- EngagementSettings新增默认0的攻击前摇/动作时长，旧样板保持原时序。ManualBattleController仅延长本地角色死亡显示，战斗结束后的展示时钟不继续结算战斗。主场景、Prefab、Packages、ProjectSettings不改。

## 本地媒体与来源

原模型enemy_1003_ncbow，参考 https://prts.wiki/w/弩手 。使用此前确认的同一版本Idle/Move_Loop/Attack及原模型Die，补充攻击事件记录与死亡动作核对。卡面为同一模型排版，不是独立绘制高清立绘。原作媒体权利属于明日方舟原权利人，具体原画/动画师及公开再分发许可未确认；本次用户批准不等于发行许可。

包在工程外CombatPackages/M08，以256px固定画布、固定脚底与每循环60fps处理为4套图集（240帧）及portrait。安装位置Assets/StreamingAssets/MonsterCombatArt/M08，整个目录和meta已Git忽略，公共PR无原作媒体、截图或视频。来源、原文件SHA256、处理工具许可沿用本地M08-Archer-v1/sources.json；combat-events.json记录攻击事件，manifest逐文件SHA256。打包/安装入口：

```powershell
python tools/package_monster_combat.py --previews "本地预览根目录" --output "本地CombatPackages" --task M08
./tools/install_monster_combat.ps1 -Source "本地CombatPackages" -Task M08
```

运行时缺包/坏包记录MONSTER_LOCAL_ART_UNAVAILABLE，菜单保持待制作，不冒用其他角色资源。新增原作包不通过公共仓库下载。

## 当前验证与限制

Unity6000.6.5f1实际编译和PrepareArcher配置创建通过。M08专项GPU Play3/3通过、0跳过：暂停拖动/扣费/帧冻结、移动/OnAttack前摇与实际延迟伤害、无技能、单只死亡/暂停/清理/重开、三只协同胜利/穿门幂等/返回清场。完整GPU回归28项中22通过、0失败、6项因本机未装M01/塔露拉原版包而跳过，未冒充零跳过。

EditMode64/64通过、0跳过。Windows构建成功（464057728字节、0错误）。独立Player九项流程检查全True、退出0；普通可见D3D12窗口输出7张完整1280×720 HUD截图，已查看怪物选择、攻击比例和血条。隐藏窗口虽逻辑通过但未输出截图，不记为视觉验证。日志与图片在本机artifacts/m08-*，原作画面不上传公共Git。尚未完整人工键鼠通关、第二机、低配长时测试。原版武器朝向沿用已确认固定斜前方向；未新增转身/全方向动画。卡牌商店、升星、羁绊和多种怪混编不在此单卡样板中，C阶段未完成。
