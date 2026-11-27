using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class FormationCampaignRun
    {
        internal void ResetBalanceFixture()
        {
            selections.Clear();rewardSelections.Clear();items.Clear();augments.Clear();PreplanId=null;
        }
        internal void RequestBalanceAugment()=>RequestAugment();
    }
    public sealed partial class FormationShopSession
    {
        internal static FormationShopSession BalanceFixture(int seed,string node,string hero,bool enhanced)
        {
            var session=StartPractice(seed,Array.FindIndex(FormationCampaignRules.Nodes,entry=>entry.Id==node));
            session.Run.ResetBalanceFixture();
            string core=hero=="FOUR"?"B04":hero=="FIVE"?"WIS":FormationBalanceV09.Heroes.Single(entry=>entry.Id==hero).Extra;
            // Eight bodies. Equipment is fixed by stage, identical across variants. No badges or preplan.
            string[] support = core switch
            {
                "M11" or "M13" => new[]{"M18","M28","M12","B09","M25","M14",core=="M11"?"M13":"M11"},
                "M21" => new[]{"M18","M28","M25","M17","B09","M24","M38"},
                "B22" => new[]{"M18","M28","M25","M17","B09","M16","B23"},
                "M16" => new[]{"M18","M28","M25","M17","B09","B22","B23"},
                "M27" => new[]{"M18","M28","M25","M17","B09","M42","M38"},
                "M42" => new[]{"M18","M28","M25","M17","B09","M27","M12"},
                "M31" => new[]{"M18","M28","M25","M17","M30","M32","B07"},
                "B23" => new[]{"M18","M28","M25","M17","B09","B22","B25"},
                _ => new[]{"M18","M28","M25","M17","B09","M14","M12"}
            };
            var ids=new[]{core}.Concat(support).Distinct().ToArray();
            if(enhanced&&hero!="FOUR"&&hero!="FIVE")session.Run.ApplyHero(hero);
            session.Shop.EnableFormationEconomy();
            session.Shop.Preset(ids.Select((id,index)=>new ShopTrialModel.Piece{
                Id=FormationCampaignRules.Find(id).CardId,
                Star=index==0?(hero is "FOUR" or "FIVE"?2:3):FormationCampaignRules.Find(id).Cost>=4&&node[0]!='3'?1:2
            }),Array.Empty<string>(),8,0,0);
            session.Seats=new ShopSeatLayout(true);session.lineup.Clear();session.lineup.AddRange(session.Shop.Bench.Select(piece=>piece.Id));
            session.instancePositions.Clear();session.kindPositions.Clear();session.Synchronize();
            session.Run.MainCarryInstance=session.Shop.Bench[0].Instance;
            void Equip(int index,params string[] keys)
            {
                foreach(string key in keys)
                    if(!session.Run.Equip(session.Run.AddItem(key,countBudget:false).Instance,session.Shop.Bench[index].Instance).Success)
                        throw new InvalidOperationException("Balance fixture equipment rejected");
            }
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-formationBalanceItemCount");
            int gearAt=Array.IndexOf(args,"-formationBalanceGear");string gear=gearAt>=0?args[gearAt+1]:"specialized";
            bool attackGear=gear!="original"&&core is "M11" or "M13" or "M16";
            if(attackGear)Equip(0,"AA","BC","BE");
            else if(gear!="original"&&core=="M42")Equip(0,"CC","DD","CF");
            else Equip(0,FormationCampaignRules.Find(core).DamageType=="法"?"CC":"AA","AF","CF");
            Equip(1,"DD","DE");Equip(4,"CE");
            int count=index>=0?int.Parse(args[index+1]):node=="3-7"?18:node is "3-4" or "3-5"?16:node[0]=='3'?14:12;
            var extras=new[]{(2,"DD"),(2,"CD"),(3,"CC"),(3,"CE"),(4,"CC"),(5,"CC"),(5,"AF"),(6,"CC"),(6,"CF"),(7,"AF"),(7,"CC"),(1,"CD")};
            foreach(var extra in extras.Take(Math.Max(0,count-6)))Equip(extra.Item1,extra.Item2);
            return session;
        }
    }
    public sealed partial class FormationBattleController
    {
        internal void RefreshBalanceTiming()=>RefreshFormationAnimationProfiles();
        internal string BalanceStartState=>$"ready={Ready},phase={Model?.Phase},blocked={CampaignInteractionBlocked},choice={Campaign?.HasBlockingChoice},opening={Campaign?.OpeningRewardsPending},canShop={Session?.CanShop},art={RosterArtReady},overflow={OverflowCount},notice={notice}";
        internal IEnumerator OpenBalanceFixture(string node,string hero,bool enhanced,int fixtureSeed)
        {
            seed=fixtureSeed;Session=FormationShopSession.BalanceFixture(seed,node,hero,enhanced);
            Model=new FormationBattleModel(seed,SelectFormationTerrain(Session.Run));Session.ApplyTo(Model);
            ResetFormationPreparation("数值试玩 · 固定八人，装备按关卡配齐；仅比较战斗，不代表自然成型通关率。");
            foreach(string id in RequiredFormationArtIds().ToArray())yield return WarmFormationArt(id);
            double deadline=Time.realtimeSinceStartupAsDouble+60;
            while((!RosterArtReady||CampaignInteractionBlocked)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            if(!RosterArtReady||CampaignInteractionBlocked)throw new InvalidOperationException("Fixture not ready: "+BalanceStartState);
            yield return null;
            RefreshFormationAnimationProfiles();SyncActors();
        }
    }
    /// <summary>Opt-in rendered Windows Player comparison. Every step runs the production combat model.</summary>
    public sealed class FormationBalanceEvidence : MonoBehaviour
    {
        [Serializable] public sealed class Battle
        {
            public string variant,hero,node,outcome,preplan,coreGear;public bool enhanced;
            public int seed,cardCost,itemCount,deaths,coreDeaths,casts,coreCasts,frames,remaining;public float progress,coreDamage,totalDamage,guardHealth;public double seconds,clearSeconds=-1;
        }
        [Serializable] public sealed class Report
        {
            public string scope="Rendered Windows Player; fixed legal eight-body rosters, same equipment per stage across all variants, real authored maps and original animation contacts. Accelerated 0.025s simulation steps, no forced damage, SP or healing. Fixture cards/items are preconfigured, not naturally recruited; these are not campaign win rates.";
            public bool passed,stopped;public int uiChecks;public List<Battle> battles=new List<Battle>();public List<string> exceptions=new List<string>();
        }
        public static bool Drives {get;private set;}
        static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationBalanceCompare")>=0;
        static bool Preview=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationBalancePreview")>=0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>Drives=false;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(Requested||Preview){var root=new GameObject("Local v0.9 balance verification");DontDestroyOnLoad(root);root.AddComponent<FormationBalanceEvidence>();}
        }
        FormationBattleController controller;readonly Report report=new Report();string directory;bool selecting,showPicker=true;string node="2-7";
        void Awake()=>Application.logMessageReceived+=Observe;
        void OnDestroy(){Application.logMessageReceived-=Observe;Drives=false;}
        void Observe(string text,string stack,LogType type){if(type is LogType.Exception or LogType.Error or LogType.Assert)report.exceptions.Add(type+": "+text+"\n"+stack);}
        IEnumerator Start()=>Guard(Work());
        IEnumerator Guard(IEnumerator work)
        {
            var stack=new Stack<IEnumerator>();stack.Push(work);
            while(stack.Count>0)
            {
                object next=null;Exception error=null;bool more=false;
                try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception failure){error=failure;}
                if(error!=null)
                {
                    Debug.LogException(error);report.exceptions.Add(error.ToString());
                    if(directory!=null)Save(false);Drives=false;if(Requested)Application.Quit(1);yield break;
                }
                if(!more){stack.Pop();continue;}if(next is IEnumerator child){stack.Push(child);continue;}
                yield return next;
            }
        }
        IEnumerator Work()
        {
            Application.runInBackground=true;
            if(SceneManager.GetActiveScene().name!=FormationBattleController.SceneName)yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            while((controller=FindAnyObjectByType<FormationBattleController>())==null||!controller.Ready)yield return null;
            if(Preview){yield return Pick("H11");yield break;}
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-formationEvidenceDir");
            directory=Path.GetFullPath(at>=0?args[at+1]:"artifacts/v09/player");Directory.CreateDirectory(directory);
            FormationBalanceV09.Select(FormationBalanceV09.Variant.B);
            int offeringSeed=20261127;
            for(int attempt=0;attempt<100;attempt++,offeringSeed++)
            {
                var sample=FormationShopSession.BalanceFixture(offeringSeed,"1-3","H11",false);sample.Run.RequestBalanceAugment();
                if(sample.Run.Choices.Any(choice=>choice.Id=="H11"))break;
                if(attempt==99)throw new InvalidOperationException("No silver hero offer within deterministic seed range");
            }
            yield return controller.OpenBalanceFixture("1-3","H11",false,offeringSeed);
            controller.Campaign.RequestBalanceAugment();yield return new WaitForSecondsRealtime(1.2f);
            var offered=controller.Campaign.Choices;
            if(offered.Count!=3||offered.Count(choice=>choice.Strategy?.Kind=="专属")!=1||
                !offered.Any(choice=>choice.Strategy?.Kind=="经济")||!offered.Any(choice=>choice.Strategy?.Kind=="战力")||
                offered.Any(choice=>choice.Strategy?.Tier!="银"))throw new InvalidOperationException("Actual hero drawer has invalid tier or choice coverage");
            report.uiChecks+=5;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"hero-offer.png"));yield return null;
            int choiceIndex=Array.FindIndex(offered.ToArray(),choice=>choice.Id=="H11");int stock=controller.Shop.Available("CARD_M11");
            if(!controller.ChooseCampaignOption(choiceIndex).Success||controller.Campaign.HeroAugment!="H11"||controller.Shop.Available("CARD_M11")!=stock-1)
                throw new InvalidOperationException("Production hero choice or finite gift failed");
            report.uiChecks+=3;Save(false);
            if(Array.IndexOf(args,"-formationBalanceChoiceOnly")>=0){report.passed=true;Save(true);Application.Quit(0);yield break;}
            string Option(string key,string fallback){int index=Array.IndexOf(args,key);return index>=0&&index+1<args.Length?args[index+1]:fallback;}
            Drives=true;
            foreach(string variantName in Option("-formationBalanceProfiles","Baseline,A,B,C").Split(','))
            foreach(string hero in Option("-formationBalanceTeams",string.Join(",",FormationBalanceV09.Heroes.Select(entry=>entry.Id).Concat(new[]{"FOUR","FIVE"}))).Split(','))
            foreach(string stage in Option("-formationBalanceStages","2-7,3-7").Split(','))
            foreach(int fixtureSeed in Option("-formationBalanceSeeds","20261127").Split(',').Select(int.Parse))
            {
                var variant=(FormationBalanceV09.Variant)Enum.Parse(typeof(FormationBalanceV09.Variant),variantName,true);
                FormationBalanceV09.Select(variant);
                bool enhanced=variant!=FormationBalanceV09.Variant.Baseline&&Array.IndexOf(args,"-formationBalanceNoHeroes")<0;
                yield return controller.OpenBalanceFixture(stage,hero,enhanced,fixtureSeed);
                var model=controller.Model;var shop=controller.Shop;
                var battle=new Battle{variant=variant.ToString(),hero=hero,node=stage,seed=model.Seed,enhanced=enhanced,
                    cardCost=shop.Bench.Sum(piece=>shop.Definition(piece.Id).Cost*ShopTrialModel.Copies(piece.Star)),
                    guardHealth=model.Defenders.Sum(guard=>guard.MaxHealth+model.GuardFutureHealth(guard)),preplan=controller.Campaign.PreplanId,
                    itemCount=shop.Bench.Sum(piece=>controller.Campaign.Equipment(piece.Instance).Count),
                    coreGear=string.Join("/",controller.Campaign.Equipment(shop.Bench[0].Instance).Select(item=>item.Key))};
                controller.StartBattle();
                if(model.Phase!=FormationPhase.Fighting)throw new InvalidOperationException("Could not start balance fixture "+hero+": "+controller.BalanceStartState);
                // Allow normal camera opening, then drive only the model; renderer still updates every frame.
                float wait=Time.realtimeSinceStartup;
                while(controller.FormationBattleOpeningActive&&Time.realtimeSinceStartup-wait<10)yield return null;
                bool shot=false;bool record=Array.IndexOf(args,"-formationBalanceRecord")>=0;int clipFrame=0;
                while(model.Phase==FormationPhase.Fighting||model.Phase==FormationPhase.Exiting)
                {
                    bool recordFrame=record&&model.Clock>=40&&model.Clock<50;
                    for(int step=0;step<(recordFrame?1:20)&&(model.Phase==FormationPhase.Fighting||model.Phase==FormationPhase.Exiting);step++)
                    {controller.RefreshBalanceTiming();model.Advance(.025f);}
                    if(battle.clearSeconds<0&&model.Defenders.All(guard=>!guard.Alive))battle.clearSeconds=model.Clock;
                    battle.frames++;yield return null;
                    if(recordFrame)
                    {
                        string clips=Path.Combine(directory,"clip-"+variant+"-"+hero+"-"+stage);Directory.CreateDirectory(clips);
                        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(clips,(clipFrame++).ToString("D5")+".png"));
                    }
                    if(!shot&&model.Clock>=35&&hero=="H11")
                    {shot=true;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,variant+"-"+stage+".png"));}
                    if(report.exceptions.Count>0)break;
                }
                battle.seconds=model.Clock;battle.outcome=model.Phase.ToString();battle.progress=model.EncounterProgress;
                battle.remaining=model.Defenders.Count(guard=>guard.Alive);battle.deaths=model.Units.Sum(unit=>unit.Deaths);
                battle.casts=model.SkillCasts.Count;battle.totalDamage=model.Units.Sum(unit=>unit.DamageDone);battle.coreDamage=model.Units[0].DamageDone;
                battle.coreDeaths=model.Units[0].Deaths;battle.coreCasts=model.SkillCasts.Count(cast=>cast.Instance==model.Units[0].Instance);
                report.battles.Add(battle);Save(false);
                Debug.Log("BALANCE_V09 "+JsonUtility.ToJson(battle));
                if(report.exceptions.Count>0){Save(false);Application.Quit(1);yield break;}
                if(File.Exists(Path.Combine(directory,"stop-after-battle.flag"))){report.stopped=true;Save(false);Application.Quit(0);yield break;}
            }
            report.passed=true;Save(true);Application.Quit(0);
        }
        void Save(bool done){File.WriteAllText(Path.Combine(directory,"comparison.json"),JsonUtility.ToJson(report,true));}
        IEnumerator Pick(string hero)
        {
            selecting=true;Drives=true;
            yield return controller.OpenBalanceFixture(node,hero,true,20261127);
            controller.Shop.EnableFormationEconomy(unlimited:true);
            Drives=false;selecting=false;showPicker=false;
        }
        void OnGUI()
        {
            if(!Preview||controller==null||!controller.Ready)return;
            GUI.depth=-100;
            if(!showPicker){if(GUI.Button(new Rect(12,80,190,32),"数值试玩 · 切换阵容/关卡"))showPicker=true;return;}
            GUILayout.BeginArea(new Rect(12,80,290,470),GUI.skin.box);
            GUILayout.Label("低费专属战备 · 本地实战比较");GUILayout.Label("固定8人/关卡装备，不代表自然运营。当前 "+node);
            if(GUILayout.Button(node=="2-7"?"切换至终关3-7":"切换至首领2-7"))node=node=="2-7"?"3-7":"2-7";
            GUI.enabled=!selecting;
            foreach(var hero in FormationBalanceV09.Heroes)if(GUILayout.Button(hero.Name+" · "+FormationCampaignRules.Find(hero.Extra).Name))StartCoroutine(Pick(hero.Id));
            if(GUILayout.Button("四费标准阵容"))StartCoroutine(Pick("FOUR"));
            if(GUILayout.Button("五费标准阵容"))StartCoroutine(Pick("FIVE"));
            GUI.enabled=true;GUILayout.EndArea();
        }
    }
}
