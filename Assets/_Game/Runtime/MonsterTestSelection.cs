using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class MonsterTestOption
    {
        public string Id { get; }
        public string Name { get; }
        public bool Playable => Definition!=null;
        internal ManualBattleDefinition Definition { get; }
        internal MonsterTestOption(string name,ManualBattleDefinition definition=null)
        { Name=name; Definition=definition; Id=definition!=null ? definition.Card.CardId : "pending/"+name; }
    }

    public sealed partial class ManualBattleController
    {
        private readonly List<MonsterTestOption> monsterTests=new List<MonsterTestOption>();
        private Vector2 playableScroll,pendingScroll;
        private string pendingFilter="";
        private GUIStyle menuTitle,menuHeading,menuCardName,menuBody,menuSmall,menuSearch;
        private bool selectionReturnPointerDown;
        public bool TestSelectionOpen { get; private set; }
        public IReadOnlyList<MonsterTestOption> MonsterTests => monsterTests.AsReadOnly();
        public static Rect TestReturnRect => new Rect(24,579,246,42);
        public static Rect SettlementReturnRect => new Rect(480,546,320,42);

        private void BuildMonsterTestCatalog()
        {
            monsterTests.Clear();
            monsterTests.Add(new MonsterTestOption(wisadelDefinition.Card.DisplayName,wisadelDefinition));
            monsterTests.Add(new MonsterTestOption(talulahDefinition.Card.DisplayName,talulahDefinition));
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var option in monsterTests) ids.Add(option.Id);
            var extra=Resources.LoadAll<ManualBattleDefinition>("MonsterTests");
            Array.Sort(extra,(a,b)=>string.CompareOrdinal(a.name,b.name));
            foreach(var data in extra)
            {
                try
                {
                    _=data.Snapshot(); var combat=data.Engagement.Snapshot();
                    if(!data.Card.HasFrameAnimation || combat.CanAttack && LocalMonsterArt.For(data.Card.CardId)!=null && !LocalMonsterArt.For(data.Card.CardId).HasAttack || combat.HasSkill && (data.Card.SkillFrames?.Count ?? 0)==0)
                        throw new InvalidOperationException("Missing required battle animation frames.");
                    if(!ids.Add(data.Card.CardId)) throw new InvalidOperationException("Duplicate test card ID.");
                    monsterTests.Add(new MonsterTestOption(data.Card.DisplayName,data));
                }
                catch(Exception e) { Debug.LogWarning("MONSTER_TEST_CONFIG_REJECTED: "+data.name+"; "+e.Message); }
            }
            string[] candidates={"源石虫","猎狗","游击队猎犬","高能源石虫","酸液源石虫","冰爆源石虫",
                "士兵","弩手","术师","重装防御者","雪怪狙击手","雪怪术师","雪怪小队凿冰人","霜星",
                "游击队战士","游击队狙击手","游击队传令兵","游击队盾卫","游击队萨卡兹战士","爱国者",
                "宿主士兵","宿主拾荒者","宿主流浪者","萨卡兹宿主百夫长","梅菲斯特",
                "泥岩小队掷能者","泥岩小队践行者","泥岩","深池方阵战士","深池逐火战士","深池焚毁者","蔓德拉",
                "囊海爬行者","钵海收割者","壳海狂奔者","首言者","萨卡兹大剑手","萨卡兹术师","银狼"};
            var names=new HashSet<string>(StringComparer.Ordinal);
            foreach(var option in monsterTests) names.Add(option.Name);
            foreach(string name in candidates) if(names.Add(name)) monsterTests.Add(new MonsterTestOption(name));
        }
        private bool IsRegisteredTest(ManualBattleDefinition data)
        {
            if(defenderTrial!=null && data==defenderTrial.battle) return true;
            if(data!=null && (data==talulahDefinition || data.Card==scene?.Card)) return true;
            foreach(var option in monsterTests) if(option.Definition==data && option.Playable) return true;
            return false;
        }
        public bool TryEnterMonsterTest(string id)
        {
            foreach(var option in monsterTests)
                if(option.Id==id && option.Playable) { SwitchTestDefinition(option.Definition); return true; }
            return false;
        }
        public void ReturnToMonsterTests()
        {
            if(defenderTrial!=null) SwitchTestDefinition(wisadelDefinition);
            else { RestartBattle(); EnableCombatView(); }
            TestSelectionOpen=true; selectionReturnPointerDown=false;
            Feedback="选择一个怪物，开始独立测试";
        }
        private void DrawMonsterTestSelection()
        {
            if(menuTitle==null)
            {
                menuTitle=new GUIStyle(textStyle){fontSize=42,fontStyle=FontStyle.Bold};
                menuHeading=new GUIStyle(textStyle){fontSize=24,fontStyle=FontStyle.Bold};
                menuCardName=new GUIStyle(menuHeading){fontSize=20};
                menuBody=new GUIStyle(textStyle){fontSize=18,wordWrap=true};
                menuSmall=new GUIStyle(smallStyle){fontSize=15,wordWrap=true};
                menuSearch=new GUIStyle(GUI.skin.textField){font=scene.InterfaceFont,fontSize=16,padding=new RectOffset(9,9,6,6)};
            }
            Fill(new Rect(0,0,1280,720),new Color(.025f,.04f,.055f,.99f));
            Fill(new Rect(0,0,10,720),new Color(.37f,.8f,.86f));
            GUI.Label(new Rect(54,30,650,26),"昨日圆车  /  单怪测试",smallStyle);
            GUI.Label(new Rect(52,62,700,60),"选择测试怪物",menuTitle);
            GUI.Label(new Rect(56,126,680,42),"每次只测试一种怪物。重新进入时，战斗状态完整重置。",menuBody);
            int count=0; foreach(var option in monsterTests) if(option.Playable) count++;
            if(GUI.Button(new Rect(840,62,360,64),"八人防线 · 折线工业地图",buttonStyle)) TryEnterDefenderTrial();
            GUI.Label(new Rect(56,188,650,36),$"可玩样板  /  {count:00}",menuHeading);
            int columns=count<3 ? 2 : 3; float cardWidth=columns==2 ? 316 : 210;
            playableScroll=GUI.BeginScrollView(new Rect(48,236,690,390),playableScroll,new Rect(0,0,664,Mathf.Ceil(count/(float)columns)*364));
            int index=0;
            foreach(var option in monsterTests)
            {
                if(!option.Playable) continue;
                float x=index%columns*(cardWidth+16),y=index/columns*364;
                Fill(new Rect(x,y,cardWidth,344),new Color(.065f,.10f,.13f));
                Fill(new Rect(x,y,cardWidth,3),new Color(.38f,.82f,.86f));
                Fill(new Rect(x+14,y+16,cardWidth-28,172),new Color(.035f,.06f,.08f));
                Sprite portrait=OriginalTalulahArt.CardPortrait(option.Definition.Card);
                DrawTestPortrait(new Rect(x+23,y+20,cardWidth-46,164),portrait);
                GUI.Label(new Rect(x+20,y+200,cardWidth-40,38),option.Name,menuCardName);
                string detail=option.Definition==talulahDefinition ? "全图火雨\n技力与手动释放" : option.Definition==wisadelDefinition ? "远程炮击\n六发强化弹药" : option.Definition.Card.CardId==OriginalSlugArt.CardId ? "贴地蠕动 / 近战\n无主动技能" : !option.Definition.Engagement.Snapshot().CanAttack ? "基础动作 / 不普攻\n特殊机制待做" : "基础动作与普攻\n单怪交战样板";
                float frozenMultiplier=option.Definition.Engagement.Snapshot().Effects.FrozenAttackMultiplier;
                if(frozenMultiplier>1) detail=$"冻结目标攻击×{frozenMultiplier:0.##}\n物理伤害 / 命中判定";
                var effects=option.Definition.Engagement.Snapshot().Effects;
                if(effects.FrostNova) detail="范围冰环 / 降低攻速\n首次倒下复起 / 攻击强化";
                if(effects.RegenerationFraction>0) detail=$"自然回血 {effects.RegenerationFraction*100:0.##}% / 秒\n按各实体生命上限恢复";
                if(effects.LifeSteal>0) detail=$"最多攻击{effects.AttackTargets}个目标\n按实际伤害{effects.LifeSteal*100:0}%恢复生命";
                if(effects.Healer) detail="最多治疗3名受伤友军\n自动治疗 / 不攻击守军";
                if(effects.ArtsBarrier) detail="三层法术屏障 / 生命攻速强化\n法术测试靶 / 可观察破盾";
                if(effects.Mudrock) detail="攻击逐次蓄力 / 最多6层\n定时补充法术屏障";
                if(effects.Phalanx) detail="附近方阵单位相互加护甲\n折射 / 70法术抗性";
                if(effects.TacticalCommander) detail="战术命令 / 全场攻防强化\n多名传令兵可叠加 / 离场解除";
                if(effects.Patriot) detail="行军四连击 · 双阶段复起\n独立爱国者关可展示献祭和投枪";
                if(effects.TauntLevel>0) detail="高仇恨吸引范围内火力\n独立游击队关可掩护队友";
                if(effects.OrderSniper) detail="收到战术命令时同时攻击两人\n独立游击队关可同场展示";
                if(effects.OrderRunner) detail="收到战术命令时移速+0.3\n独立游击队关可同场展示";
                if(effects.Shackler) detail="一次性枷锁 / 禁止攻击与阻挡\n每秒法术伤害 / 可被中断";
                if(effects.FirstSpeaker) detail="物理普攻附神经损伤 / 侵蚀液泡\n延迟十字索敌 / 施放期间无敌";
                if(effects.Harvester) detail="受伤苏醒 / 临战加速和失血\n神经光环 / 阻挡时近战";
                if(effects.NervousCreeper) detail="不主动攻击 / 受伤范围反击\n神经损伤爆发 / 真实伤害与眩晕";
                if(effects.Mandragora) detail="石盾减伤 / 首次重生与悬浮\n凝视 / 支柱破盾和倒塌";
                if(effects.Flamerazer) detail="半秒喷火 / 积累灼燃\n死亡爆炸 / 同类连锁与支柱倒塌";
                if(effects.Flamechaser) detail="倒下化为5次受击的隐匿余烬\n余烬存活10秒后重生 / 芦苇灼燃";
                if(effects.ColdAttackEvery>0) detail=$"每{effects.ColdAttackEvery}次普攻附加寒冷\n法术伤害 / 寒冷叠加冻结";
                GUI.Label(new Rect(x+20,y+235,cardWidth-40,54),detail,menuSmall);
                if(GUI.Button(new Rect(x+18,y+298,cardWidth-36,36),"进入测试",buttonStyle)) TryEnterMonsterTest(option.Id);
                index++;
            }
            GUI.EndScrollView();
            Fill(new Rect(774,188,452,438),new Color(.045f,.07f,.095f));
            GUI.Label(new Rect(794,202,410,32),$"候选怪物  /  {monsterTests.Count-count:00} 待制作",menuHeading);
            GUI.SetNextControlName("MonsterTestSearch");
            pendingFilter=GUI.TextField(new Rect(794,247,410,32),pendingFilter,40,menuSearch);
            if(string.IsNullOrEmpty(pendingFilter)) GUI.Label(new Rect(805,251,380,24),"输入名字筛选待制作怪物",menuSmall);
            var pending=new List<MonsterTestOption>();
            foreach(var option in monsterTests) if(!option.Playable && (string.IsNullOrEmpty(pendingFilter) || option.Name.IndexOf(pendingFilter,StringComparison.OrdinalIgnoreCase)>=0)) pending.Add(option);
            pendingScroll=GUI.BeginScrollView(new Rect(790,292,426,306),pendingScroll,new Rect(0,0,400,pending.Count*34));
            for(int i=0;i<pending.Count;i++)
            {
                Fill(new Rect(0,i*34,396,30),i%2==0 ? new Color(.075f,.105f,.13f) : new Color(.055f,.08f,.105f));
                GUI.Label(new Rect(12,i*34+3,282,26),pending[i].Name,menuSmall);
                Color previous=GUI.color; GUI.color=new Color(.56f,.64f,.69f);
                GUI.Label(new Rect(310,i*34+3,80,26),"待制作",menuSmall); GUI.color=previous;
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(56,655,1160,38),"进入后拖动底部卡牌至红门部署；有主动技能的角色满技力后点选释放。可随时返回。",menuBody);
        }
        private static void DrawTestPortrait(Rect bounds,Sprite sprite)
        {
            if(sprite==null) return;
            Rect source=sprite.textureRect; float ratio=Mathf.Min(bounds.width/source.width,bounds.height/source.height);
            Rect target=new Rect(bounds.center.x-source.width*ratio/2,bounds.center.y-source.height*ratio/2,source.width*ratio,source.height*ratio);
            GUI.DrawTextureWithTexCoords(target,sprite.texture,new Rect(source.x/sprite.texture.width,source.y/sprite.texture.height,source.width/sprite.texture.width,source.height/sprite.texture.height));
        }

        private IEnumerator VerifyMonsterSelection(string directory)
        {
            Time.timeScale=0; ReturnToMonsterTests();
            int playable=0;foreach(var option in MonsterTests) if(option.Playable) playable++;
            bool initial=TestSelectionOpen && MonsterTests.Count==41 && playable==40;
            yield return InteractionFrame(directory,"monster-selection");
            // Show the formerly poster-backed cards beside a later transparent portrait.
            int portraitIndex=monsterTests.FindIndex(option=>option.Id=="CARD_M09");
            if(portraitIndex>=0)
            {
                playableScroll=new Vector2(0,portraitIndex/3*364);
                yield return InteractionFrame(directory,"selection-clean-portraits");
                playableScroll=Vector2.zero;
            }
            double clock=Battle.Clock; SelectCard(true); TryDeployAt(Battle.Entry); Tick(3);
            bool blocked=Battle.Units.Count==0 && Battle.Clock==clock && !Dragging;
            bool pending=!TryEnterMonsterTest("pending/银狼") && TestSelectionOpen;
            bool wisadel=TryEnterMonsterTest(wisadelDefinition.Card.CardId) && !TestSelectionOpen;
            SelectCard(true); bool deployed=TryDeployAt(Battle.Entry).Success;
            Tick(4.05f); OpenSkillMenu(Battle.Units[0].EntityId); ReturnToMonsterTests(); yield return null;
            bool cleared=TestSelectionOpen && !SkillMenuOpen && !Dragging && Battle.Units.Count==0 && Battle.DP==12 && Battle.Clock==0;
            bool talulah=TryEnterMonsterTest(talulahDefinition.Card.CardId) && Battle.Defenders.Count==3;
            SelectCard(true); TryDeployAt(Battle.Entry); Tick(4.05f); SelectSkillUnit(Battle.Units[0].EntityId); ActivateSelectedSkill(); Tick(1.25f);
            yield return InteractionFrame(directory,"selection-talulah");
            ReturnToMonsterTests(); yield return null;
            bool fireCleared=FindAnyObjectByType<FireRainView>().VisibleStreams==0 && Battle.SkillCasts==0;
            TryEnterMonsterTest(wisadelDefinition.Card.CardId); bool fresh=Battle.Units.Count==0 && Battle.Defenders.Count==1 && Battle.Defender.Alive && Battle.SkillCasts==0;
            yield return InteractionFrame(directory,"selection-wisadel");
            Debug.Log($"MONSTER_SELECTION_CHECK: initial={initial}; blocked={blocked}; pending={pending}; wisadel={wisadel}; deployed={deployed}; cleared={cleared}; talulah={talulah}; fireCleared={fireCleared}; fresh={fresh}.");
            Time.timeScale=1; Application.Quit(initial && blocked && pending && wisadel && deployed && cleared && talulah && fireCleared && fresh ? 0 : 1);
        }
    }
}
