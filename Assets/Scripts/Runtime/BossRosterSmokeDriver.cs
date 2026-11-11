using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    [Serializable] public sealed class BossRosterSmokeCheck
    {public string name,detail;public bool passed;}
    [Serializable] public sealed class BossRosterSmokeCase
    {
        public string cardId,task,mode="s3",fixture,skillId,sourceSprite,error;
        public bool passed,completed,cast,phaseFixture,conditionOnly,spIsolationChecked;
        public string automaticStatusBefore,automaticStatusAfter;public int automaticCasts,otherAutomaticCasts;public double firstAutomaticAt=-1,otherFirstAutomaticAt=-1;
        public double seconds;public float hpBefore,hpAfter,spBefore,spAfter,otherSpBefore,otherSpAfter;
        public int cameraPixels,cameraEdgePixels,textureWidth,textureHeight,eventsBefore,eventsAfter,objectsAfter;
        public List<BossRosterSmokeCheck> checks=new List<BossRosterSmokeCheck>();
    }
    [Serializable] public sealed class BossRosterSmokeResult
    {
        public bool passed;public string graphics,scope="Actual saved MonsterTrial controller, production preparation/pointer deployment and automatic skills; no SP/cast injection. Labelled durability/phase fixtures, not natural clears.";
        public List<BossRosterSmokeCase> cases=new List<BossRosterSmokeCase>();public List<string> errors=new List<string>();
    }
    // Shared by the explicit Development Player switch and PlayMode tests.
    public static class BossRosterSmokeProbe
    {
        public static readonly string[] CoreCards={"CARD_M25","CARD_M32","CARD_TALULAH_TRIAL","CARD_M20","CARD_M14","CARD_M28","CARD_WISADEL_APRIL"};
        public static string[] NewCards=>BossRosterCatalog.Entries.Select(e=>e.CardId).ToArray();
        public static string[] CardsFromFilter(string filter)
        {
            if(string.IsNullOrWhiteSpace(filter)||filter=="all")return NewCards.Concat(CoreCards).ToArray();
            return filter.Split(new[]{',',';'},StringSplitOptions.RemoveEmptyEntries).Select(value=>
            {
                string key=value.Trim();var art=BossRosterArt.For(key);
                if(art==null||!NewCards.Contains(art.CardId)&&!CoreCards.Contains(art.CardId))throw new ArgumentException("Unknown recruitable Boss filter: "+key);
                return art.CardId;
            }).Distinct().ToArray();
        }
        static void Check(BossRosterSmokeCase row,string name,bool value,string detail="")
        {row.checks.Add(new BossRosterSmokeCheck{name=name,passed=value,detail=detail});if(!value)Debug.LogError("BOSS_ROSTER_SMOKE_FAIL "+row.cardId+" "+name+" "+detail);}
        static IEnumerator Guard(IEnumerator root,Action<Exception> fail)
        {
            var stack=new Stack<IEnumerator>();stack.Push(root);
            try
            {
                while(stack.Count>0)
                {
                    bool moved=false;object item=null;Exception error=null;
                    try{moved=stack.Peek().MoveNext();if(moved)item=stack.Peek().Current;}catch(Exception e){error=e;}
                    if(error!=null){fail(error);yield break;}
                    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                    if(item is IEnumerator nested){stack.Push(nested);continue;}yield return item;
                }
            }
            finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();}
        }
        public static IEnumerator Run(ManualBattleController controller,string directory,IEnumerable<string> cards,Action<BossRosterSmokeResult> done,string viviMode="all")
        {
            var result=new BossRosterSmokeResult{graphics=SystemInfo.graphicsDeviceType.ToString()};
            if(directory!=null)Directory.CreateDirectory(directory);
            foreach(string id in cards)
            {
                string[] modes=id==ViviCombat.CardId?(viviMode=="all"?new[]{"s3","s1","s2"}:new[]{viviMode}):new[]{"default"};
                foreach(string mode in modes)
                {
                    var row=new BossRosterSmokeCase{cardId=id,mode=mode};result.cases.Add(row);
                    yield return Guard(Verify(controller,row,directory),e=>{row.error=e.ToString();result.errors.Add(id+"/"+mode+": "+e);});
                    row.passed=row.completed&&row.error==null&&row.checks.All(c=>c.passed);
                    Debug.Log("BOSS_ROSTER_CASE "+JsonUtility.ToJson(row));
                    if(directory!=null)File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
                }
            }
            result.passed=result.cases.Count>0&&result.errors.Count==0&&result.cases.All(c=>c.passed);
            if(directory!=null)File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));done?.Invoke(result);
        }
        static DemoSpriteVisual View(string id)=>UnityEngine.Object.FindObjectsByType<DemoSpriteVisual>().FirstOrDefault(v=>v.EntityId==id&&v.gameObject.activeInHierarchy);
        static IReadOnlyList<BossRosterSkillInfo> Skills(ManualBattle b,DeployedUnitState u)=>BossRosterCatalog.Contains(u.CardId)?b.BossRosterSkills(u):b.BossCoreSkills(u);
        static float ReadSkillSP(ManualBattle b,DeployedUnitState u)
        {var list=Skills(b,u);return list.Count>0?list[0].SP:u.SkillSP;}
        static bool HasAutomaticCast(ManualBattle b,DeployedUnitState u)=>b.AutomaticMonsterCasts.Any(e=>e.EntityId==u.EntityId&&e.At>=u.SpawnTime);
        static bool NativeAutomaticObserved(ManualBattle b,DeployedUnitState u)=>u.Spear!=null||b.Gazes.Any(g=>g.SourceId==u.EntityId);
        static bool NoPatriotSpearTarget(ManualBattle b)=>!b.Defenders.Any(d=>d.Alive&&d.HighGround&&!b.DefenderCamouflaged(d));
        static IEnumerator VerifyInformationInput(ManualBattleController c,BossRosterSmokeCase row,DeployedUnitState a,DeployedUnitState other)
        {
            var b=c.Battle;int casts=b.AutomaticMonsterCasts.Count,skills=b.SkillCasts;float sp=ReadSkillSP(b,a),otherSp=ReadSkillSP(b,other);
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.UnitScreenRect(a.EntityId).center));
            Check(row,"actual-unit-click-opens-information-without-casting",c.SkillMenuOpen&&b.AutomaticMonsterCasts.Count==casts&&b.SkillCasts==skills);
            if(c.SkillMenuOpen)c.ProcessPointerDown(c.BossRosterSmokeSkillPoint(0,true));
            Check(row,"information-row-click-does-not-cast-or-spend-either-sp",b.AutomaticMonsterCasts.Count==casts&&b.SkillCasts==skills&&ReadSkillSP(b,a)==sp&&ReadSkillSP(b,other)==otherSp);
            Check(row,"information-keeps-normal-simulation-speed",c.EffectiveSimulationSpeed==1);
            c.ToggleSpeed();double clock=b.Clock;c.Tick(.05f);yield return null;
            Check(row,"information-keeps-double-speed-and-real-clock",c.SkillMenuOpen&&c.EffectiveSimulationSpeed==2&&Math.Abs(b.Clock-clock-.1)<.0001);
            c.ToggleSpeed();c.ProcessPointerDown(ManualBattleController.ScreenPosition(new Vector2(12,500)));
            Check(row,"blank-click-closes-information",!c.SkillMenuOpen&&c.EffectiveSimulationSpeed==1);
        }
        static IEnumerator Verify(ManualBattleController c,BossRosterSmokeCase row,string directory)
        {
            yield return c.BossRosterSmokeEnter(row.cardId);
            Check(row,"production-entry-and-preparation",!c.ArtLoading&&c.Definition.Card.CardId==row.cardId&&c.Battle.Phase==BattlePhase.Ready);
            var art=BossRosterArt.For(row.cardId);row.task=art?.Task;
            Check(row,"approved-source-metadata",art!=null,art?.Model);
            if(art==null)yield break;
            // First use the unchanged authored battle and actual bottom-card pointer path.
            c.StartBattle();var receipt=c.BossRosterSmokeDrag();
            Check(row,"authored-bottom-card-drag",receipt.Success,receipt.Failure.ToString());
            if(!receipt.Success)yield break;
            c.Tick(.2f);yield return null;
            var natural=c.Battle.FindUnit(receipt.EntityId);var naturalView=View(natural.EntityId);
            Check(row,"authored-real-entity-view",naturalView!=null&&naturalView.Renderer.sprite!=null);
            Check(row,"production-automatic-skills-enabled",c.Battle.AutomaticSkillsEnabled);
            // Only source Sui has a legitimate initially invisible wait. Visibility is
            // checked below after its actual presence skill, not by substituting idle.
            if(row.task!="B21"&&naturalView!=null)
                Check(row,"authored-original-sprite",naturalView.Renderer.sprite.name.Contains("BossRoster/"+row.task+"/"),naturalView.Renderer.sprite.name);
            c.RestartBattle();yield return null;

            // Explicit test fixture: keep the saved camera, map, route and CardDefinition.
            // Only budgets/durability change; deployment, automatic timing, damage and views remain real.
            c.BossRosterSmokeInstallFixture(row.cardId==ViviCombat.CardId?row.mode:"s3",row.cardId==TalulahTiming.CardId);
            row.fixture="Saved MonsterTrial geometry + actual CardDefinition; runtime-only DP>=200, 3 uses/CD0.01s, monsterHP>=10000, guardHP1000000/attack1/interval100; observe up to 240 simulated seconds without SP/cast injection. Talulah phase fixture uses real three-star CampaignV2 profile.";
            c.StartBattle();var one=c.BossRosterSmokeDrag();Check(row,"fixture-first-real-drag",one.Success,one.Failure.ToString());
            if(!one.Success)yield break;
            c.Tick(.35f);yield return null;
            var two=c.BossRosterSmokeDrag();Check(row,"fixture-second-real-drag",two.Success,two.Failure.ToString());
            if(!two.Success)yield break;
            c.Tick(.1f);yield return null;
            var b=c.Battle;var a=b.FindUnit(one.EntityId);var other=b.FindUnit(two.EntityId);
            Check(row,"two-independent-entities",a!=other&&a.EntityId!=other.EntityId&&a.SpawnTime!=other.SpawnTime&&c.ViewCount>=2);
            float otherHp=other.Health;b.TryDamageUnit(a.EntityId,Math.Max(2,a.MaxHealth*.15f),CombatDamageType.Physical);
            Check(row,"one-entity-damage-does-not-change-other-hp",other.Health==otherHp);

            yield return VerifyInformationInput(c,row,a,other);
            var rosterA=b.BossRoster(a);var rosterB=b.BossRoster(other);
            if(rosterA!=null)Check(row,"separate-automatic-skill-state-and-sp-containers",rosterB!=null&&!ReferenceEquals(rosterA,rosterB)&&!ReferenceEquals(rosterA.SP,rosterB.SP));
            var coreA=b.BossCoreState(a);var coreB=b.BossCoreState(other);
            if(coreA!=null)Check(row,"separate-core-skill-state-and-sp",coreB!=null&&!ReferenceEquals(coreA,coreB));

            // Saint's first form is passive. Use a genuine lethal input,
            // then wait for the real rebirth state machine instead of assigning Phase.
            if(row.task=="B13")
            {
                row.phaseFixture=true;b.TryDamageUnit(a.EntityId,a.MaxHealth*20,CombatDamageType.Physical);
                for(int step=0;step<1600&&a.Active&&(b.BossRoster(a).Transitioning||b.BossRoster(a).Phase<2);step++)
                {c.Tick(.05f);if(step%10==0)yield return null;}
                Check(row,"saint-real-rebirth-enters-second-form",a.Active&&b.BossRoster(a).Phase>=2);
            }
            // These old first forms have no active cast to wait for. Enter their real
            // native second life before observing the source skill conditions.
            bool phaseObserved=row.task=="B06"||row.task=="B17";
            if(phaseObserved)yield return CorePhase(c,row,a,directory);
            row.eventsBefore=b.BossRosterEvents.Count;row.hpBefore=b.Defenders.Sum(d=>d.Health);
            row.spBefore=ReadSkillSP(b,a);row.otherSpBefore=ReadSkillSP(b,other);
            row.automaticStatusBefore=b.AutomaticSkillStatus(a);
            double until=b.Clock+240;
            while(a.Active&&b.Phase==BattlePhase.Running&&b.Clock<until&&!HasAutomaticCast(b,a)&&!NativeAutomaticObserved(b,a))
            {
                if(row.task=="B02"&&b.HealingCasts.Any(h=>h.Resolved&&h.HealthRestored>0))break;
                if(row.task=="B19"&&b.Clock-a.SpawnTime>=40&&b.Gramophones.Count==0)break;
                // Source Patriot throws only at high-ground targets; the saved default
                // field can legitimately have none. Its native phase is still checked.
                if(row.task=="B17"&&b.Clock-a.SpawnTime>=100&&NoPatriotSpearTarget(b))break;
                float otherBefore=ReadSkillSP(b,other);int otherCastsBefore=b.AutomaticMonsterCasts.Count(e=>e.EntityId==other.EntityId);
                c.Tick(.1f);
                if(HasAutomaticCast(b,a)&&b.AutomaticMonsterCasts.Count(e=>e.EntityId==other.EntityId)==otherCastsBefore)
                {
                    row.spIsolationChecked=true;
                    Check(row,"first-automatic-cast-does-not-spend-other-entity-sp",ReadSkillSP(b,other)+.0001f>=otherBefore);
                }
                if(((int)(b.Clock*10))%5==0)yield return null;
            }
            var automatic=b.AutomaticMonsterCasts.Where(e=>e.EntityId==a.EntityId).ToArray();
            var otherAutomatic=b.AutomaticMonsterCasts.Where(e=>e.EntityId==other.EntityId).ToArray();
            row.cast=automatic.Length>0||NativeAutomaticObserved(b,a);row.automaticCasts=automatic.Length;row.otherAutomaticCasts=otherAutomatic.Length;
            row.skillId=automatic.Length>0?automatic[0].SkillId:a.Spear!=null?"native-spear":b.Gazes.Any(g=>g.SourceId==a.EntityId)?"native-gaze":"passive-or-condition-not-met";
            if(automatic.Length>0)row.firstAutomaticAt=automatic[0].At;else if(a.Spear!=null)row.firstAutomaticAt=a.Spear.StartedAt;else if(b.Gazes.Any(g=>g.SourceId==a.EntityId))row.firstAutomaticAt=b.Gazes.First(g=>g.SourceId==a.EntityId).StartedAt;if(otherAutomatic.Length>0)row.otherFirstAutomaticAt=otherAutomatic[0].At;
            row.spAfter=ReadSkillSP(b,a);row.otherSpAfter=ReadSkillSP(b,other);row.automaticStatusAfter=b.AutomaticSkillStatus(a);
            row.conditionOnly=!row.cast&&(row.task=="B02"||row.task=="B19"&&b.Gramophones.Count==0||row.task=="B17"&&NoPatriotSpearTarget(b));
            Check(row,"real-automatic-cast-or-explicit-source-condition",row.cast||row.conditionOnly,row.automaticStatusAfter);
            if(automatic.Length>0)Check(row,"automatic-events-belong-to-their-own-deployed-entity",automatic.All(e=>e.CardId==a.CardId&&e.At>=a.SpawnTime)&&otherAutomatic.All(e=>e.CardId==other.CardId&&e.At>=other.SpawnTime));
            c.CloseSkillMenu();c.Tick(.12f);yield return null;
            var va=View(a.EntityId);var vb=View(other.EntityId);
            Check(row,"actual-source-sprite-texture",va!=null&&vb!=null&&va.Renderer.enabled&&va.Renderer.sprite!=null&&va.Renderer.sprite.texture is Texture2D&&va.Renderer.sprite.name.Contains("BossRoster/"+row.task+"/"),va?.FrameState);
            if(va!=null&&va.Renderer.sprite!=null)
            {
                row.sourceSprite=va.Renderer.sprite.name;row.textureWidth=va.Renderer.sprite.texture.width;row.textureHeight=va.Renderer.sprite.texture.height;
                row.cameraPixels=CameraDifference(Camera.main,va.Renderer,PathFor(directory,row,"source"),value=>row.cameraEdgePixels=value);
                Check(row,"original-body-contributes-real-camera-pixels",row.cameraPixels>5,row.cameraPixels.ToString());
                if(row.task=="B21")Check(row,"sui-body-fits-camera-without-border-clipping",row.cameraEdgePixels==0,row.cameraEdgePixels.ToString());
            }
            if(va!=null&&vb!=null)
            {
                b.SetPaused(true);double clock=b.Clock;var fa=va.Renderer.sprite;var fb=vb.Renderer.sprite;int ia=va.FrameIndex,ib=vb.FrameIndex;
                float ha=a.Health,hb=other.Health,sa=ReadSkillSP(b,a),sb=ReadSkillSP(b,other);int casts=b.AutomaticMonsterCasts.Count;string statusA=b.AutomaticSkillStatus(a),statusB=b.AutomaticSkillStatus(other);Vector3 pa=a.Motion.Position,pb=other.Motion.Position;c.Tick(3);yield return null;yield return null;
                Check(row,"shared-pause-freezes-both-source-frames-and-state",b.Clock==clock&&a.Health==ha&&other.Health==hb&&a.Motion.Position==pa&&other.Motion.Position==pb&&va.Renderer.sprite==fa&&vb.Renderer.sprite==fb&&va.FrameIndex==ia&&vb.FrameIndex==ib&&ReadSkillSP(b,a)==sa&&ReadSkillSP(b,other)==sb&&b.AutomaticMonsterCasts.Count==casts&&b.AutomaticSkillStatus(a)==statusA&&b.AutomaticSkillStatus(other)==statusB);
                b.SetPaused(false);
            }
            int observation=row.task=="B20"&&row.mode=="s2"?160:100;
            for(int i=0;i<observation&&b.Phase==BattlePhase.Running;i++){c.Tick(.1f);if(i%5==0)yield return null;}
            if(row.task=="B20"&&row.mode=="s2"&&row.cast)
            {
                var core=b.BossCoreState(a);
                Check(row,"s2-real-overload-half-and-source-action",core.Phase>=2&&b.BossRosterEvents.Any(e=>e.EntityId==a.EntityId&&e.Animation!=null&&e.Animation.Contains("Overload")));
                var overload=View(a.EntityId);if(overload!=null)CameraDifference(Camera.main,overload.Renderer,PathFor(directory,row,"overload"));
            }
            row.hpAfter=b.Defenders.Sum(d=>d.Health);row.eventsAfter=b.BossRosterEvents.Count;row.objectsAfter=b.BossRosterObjects.Count;
            if(row.cast)Check(row,"automatic-cast-produced-real-model-events-or-resource-transaction",a.SkillsUsed>0||row.eventsAfter>row.eventsBefore||NativeAutomaticObserved(b,a));
            if(row.task=="B02")Check(row,"automatic-healer-restores-real-health",b.HealingCasts.Any(h=>h.Resolved&&h.HealthRestored>0)&&a.HealthRecovered>0);
            if(!phaseObserved&&new[]{"B06","B16","B17","B18"}.Contains(row.task)&&a.Active&&b.Phase==BattlePhase.Running)
                yield return CorePhase(c,row,a,directory);
            row.seconds=b.Clock;
            c.RestartBattle();yield return null;yield return null;
            Check(row,"restart-clears-real-entities-events-and-objects",c.Battle.Units.Count==0&&c.ViewCount==0&&c.Battle.BossRosterEvents.Count==0&&c.Battle.BossRosterObjects.Count==0&&c.Battle.AutomaticMonsterCasts.Count==0);
            row.completed=true;
        }
        static IEnumerator CorePhase(ManualBattleController c,BossRosterSmokeCase row,DeployedUnitState unit,string directory)
        {
            var b=c.Battle;row.phaseFixture=true;double began=b.Clock;
            bool input=b.TryDamageUnit(unit.EntityId,unit.MaxHealth*100,CombatDamageType.Physical,ignoreResistance:100);
            bool entered=row.task=="B06"?unit.MandraPhase==MandragoraPhase.Reborn:row.task=="B16"?unit.TalulahPhase==TalulahPhase.FirstFall:
                row.task=="B17"?unit.PatriotPhase==PatriotPhase.Reborn:unit.IsReviving;
            Check(row,"labelled-lethal-input-triggers-real-native-transition",input&&entered);
            if(!entered)yield break;
            c.Tick(.15f);yield return null;var visual=View(unit.EntityId);
            string expected=row.task=="B06"?"Reborn":row.task=="B16"?"Reborn_1_Start":row.task=="B17"?"revive_1":"Skill_2";
            Check(row,"native-rebirth-body-is-real-source",visual!=null&&visual.Renderer.sprite!=null&&visual.Renderer.sprite.name.Contains("/"+expected+"/"),visual?.Renderer.sprite?.name);
            if(visual!=null)
            {
                b.SetPaused(true);double clock=b.Clock;var sprite=visual.Renderer.sprite;c.Tick(2);yield return null;
                Check(row,"native-transition-pause-freezes-original-frame",b.Clock==clock&&visual.Renderer.sprite==sprite);b.SetPaused(false);
                CameraDifference(Camera.main,visual.Renderer,PathFor(directory,row,"native-phase"));
            }
            for(int i=0;i<800&&unit.Active;i++)
            {
                bool waiting=row.task=="B06"?unit.MandraPhase==MandragoraPhase.Reborn:row.task=="B16"?unit.TalulahPhase!=TalulahPhase.Second:
                    row.task=="B17"?unit.PatriotPhase==PatriotPhase.Reborn:unit.IsReviving;
                if(!waiting)break;c.Tick(.1f);if(i%10==0)yield return null;
            }
            bool restored=unit.Active&&unit.Health>0&&(row.task=="B06"?unit.MandraPhase==MandragoraPhase.Second:row.task=="B16"?unit.TalulahPhase==TalulahPhase.Second:
                row.task=="B17"?unit.PatriotPhase==PatriotPhase.Destruction:unit.ReviveUsed&&!unit.IsReviving);
            Check(row,"real-second-life-restores-health-and-source-protection",restored&&b.UnitInvulnerable(unit),"elapsed="+(b.Clock-began).ToString("F2"));
        }
        static string PathFor(string directory,BossRosterSmokeCase row,string label)=>directory==null?null:Path.Combine(directory,row.task+"-"+row.mode+"-"+label+".png");
        static int CameraDifference(Camera camera,SpriteRenderer actor,string file,Action<int> edgeCount=null)
        {
            if(camera==null)throw new InvalidOperationException("Actual MonsterTrial camera missing");
            var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;Rect oldRect=camera.rect;bool enabled=actor.enabled;
            try
            {
                camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);actor.enabled=false;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();var off=image.GetPixels32();
                actor.enabled=enabled;camera.Render();image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();var on=image.GetPixels32();if(file!=null)File.WriteAllBytes(file,image.EncodeToPNG());
                int changed=0,edge=0;for(int i=0;i<on.Length;i++)if(Math.Abs(on[i].r-off[i].r)+Math.Abs(on[i].g-off[i].g)+Math.Abs(on[i].b-off[i].b)>12){changed++;int x=i%1280,y=i/1280;if(x<2||x>=1278||y<2||y>=718)edge++;}edgeCount?.Invoke(edge);return changed;
            }
            finally{actor.enabled=enabled;camera.targetTexture=oldTarget;camera.rect=oldRect;RenderTexture.active=oldActive;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);}
        }
    }
    public sealed class BossRosterSmokeDriver:MonoBehaviour
    {
        static bool installed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(installed||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-bossRosterSmokeTest"))return;
            installed=true;Application.runInBackground=true;var go=new GameObject("Explicit Boss roster Player verification");DontDestroyOnLoad(go);go.AddComponent<BossRosterSmokeDriver>();
        }
        IEnumerator Start()
        {
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");string folder=at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.Combine(Application.persistentDataPath,"BossRosterEvidence");
            CampaignJourneySession.Clear();if(SceneManager.GetActiveScene().name!="MonsterTrial")yield return SceneManager.LoadSceneAsync("MonsterTrial");
            ManualBattleController c=null;for(int i=0;i<7200;i++){c=FindAnyObjectByType<ManualBattleController>();if(c!=null&&c.Battle!=null&&c.MonsterTests.Count>0&&!c.ArtLoading)break;yield return null;}
            if(c==null){Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"result.json"),"{\"passed\":false,\"error\":\"MonsterTrial controller unavailable\"}");Application.Quit(1);yield break;}
            string[] cards=null;Exception parseError=null;int filterAt=Array.IndexOf(args,"-bossRosterSmokeTasks"),modeAt=Array.IndexOf(args,"-bossRosterSmokeViviSkill");
            string mode=modeAt>=0&&modeAt+1<args.Length?args[modeAt+1]:"all";
            try{cards=BossRosterSmokeProbe.CardsFromFilter(filterAt>=0&&filterAt+1<args.Length?args[filterAt+1]:"all");if(!new[]{"all","s1","s2","s3"}.Contains(mode))throw new ArgumentException("Invalid Vivi smoke mode: "+mode);}catch(Exception e){parseError=e;}
            if(parseError!=null){Directory.CreateDirectory(folder);var failed=new BossRosterSmokeResult();failed.errors.Add(parseError.ToString());File.WriteAllText(Path.Combine(folder,"result.json"),JsonUtility.ToJson(failed,true));Application.Quit(1);yield break;}
            Time.timeScale=0;BossRosterSmokeResult result=null;
            yield return BossRosterSmokeProbe.Run(c,folder,cards,value=>result=value,mode);
            Time.timeScale=1;Debug.Log("BOSS_ROSTER_PLAYER_RESULT "+JsonUtility.ToJson(result));Application.Quit(result?.passed==true?0:1);
        }
    }
    public sealed partial class ManualBattleController
    {
        internal IEnumerator BossRosterSmokeEnter(string id)
        {
            if(Battle!=null)Battle.SetPaused(false);ReturnToMonsterTests();
            var option=monsterTests.FirstOrDefault(x=>x.Id==id&&x.Playable);if(option==null)throw new InvalidOperationException("Registered Boss selection missing: "+id);
            PrepareMonsterEntry(option);float until=Time.realtimeSinceStartup+180;
            while(ArtLoading){if(Time.realtimeSinceStartup>until)throw new TimeoutException("Boss source preparation timed out: "+id);yield return null;}
            if(definition.Card.CardId!=id)throw new InvalidOperationException("Production selection did not enter "+id);
        }
        internal DeploymentReceipt BossRosterSmokeDrag()
        {
            Vector2 card=ScreenPosition(CardSlot(0).center),entry=Camera.main.WorldToScreenPoint(Battle.Entry);
            ProcessPointerDown(card);ProcessPointerMove(entry);return ProcessPointerUp(entry);
        }
        internal Vector2 BossRosterSmokeSkillPoint(int index,bool boss)=>ScreenPosition((boss?BossSkillButton(index):SkillCastRect).center);
        internal void BossRosterSmokeInstallFixture(string viviSkill,bool talulahThreeStar)
        {
            var previous=Battle;var unit=previous.Settings;var combat=previous.CombatSettings;
            if(talulahThreeStar){var profile=CampaignV2Balance.Build(definition,3,200,99);unit=profile.Unit;combat=profile.Combat;}
            var settings=new ManualBattleSettings(unit.CardId,unit.UnitId,Math.Max(200,unit.DeploymentCost*3),99,3,unit.DeploymentCost,.01f,Math.Max(10000,unit.Health),unit.Speed,unit.GateDamage,unit.TowerRange,unit.TowerDamage,unit.ShotInterval);
            var durable=new EngagementSettings(unitAttack:combat.UnitAttack,unitArmor:combat.UnitArmor,unitRange:combat.UnitRange,unitInterval:combat.UnitInterval,
                defenderHealth:1000000,defenderAttack:1,defenderArmor:combat.DefenderArmor,defenderRange:combat.DefenderRange,defenderInterval:100,blockCount:combat.BlockCount,blockRadius:combat.BlockRadius,
                initialSP:combat.InitialSP,requiredSP:combat.RequiredSP,spPerSecond:combat.SPPerSecond,skillAmmo:combat.SkillAmmo,skillAttack:combat.SkillAttack,skillInterval:combat.SkillInterval,skillRange:combat.SkillRange,
                projectileTime:combat.ProjectileTime,fireRain:combat.FireRain,hasSkill:combat.HasSkill,melee:combat.Melee,attackDuration:combat.AttackDuration,attackWindup:combat.AttackWindup,effects:combat.Effects,
                canAttack:combat.CanAttack,defenderDamageType:combat.DefenderDamageType,revealsInvisible:combat.RevealsInvisible,defenderCanHitAir:combat.DefenderCanHitAir,frostSeal:combat.FrostSeal,mudrockCapture:combat.MudrockCapture);
            ClearViews();CancelInteraction();selectedEntityId=null;requestSequence=0;terminalPresentationElapsed=0;readyIdleElapsed=0;SimulationSpeed=1;
            Battle=new ManualBattle(settings,previous.Lanes[0].Points,previous.Defenders[0].Position,durable,previous.Defenders.Skip(1).Select(d=>d.Position).ToArray(),deploymentLanes:previous.Lanes);
            selectedViviSourceSkill=viviSkill;ConfigureBossPreparation();EnableCombatView();UpdatePreview();
        }
    }
}
