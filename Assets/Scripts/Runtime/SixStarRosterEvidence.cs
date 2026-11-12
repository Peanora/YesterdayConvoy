using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace YesterdayConvoy
{
    // Opt-in development-Player observation. Enters the authored trials and uses
    // their finite cards; never grants health/SP, forces damage or issues a skill.
    public sealed partial class ManualBattleController
    {
        [Serializable] internal sealed class SixStarCase
        {
            public string id,scene;public bool passed;public int casts,deployments,bodyPixels,skillPixels,skillBodyPixels,monsterPixels,hitPixels,shots,damageShots;
            public double clock;public string outcome;public string[] checks,sourceKeys;
        }
        [Serializable] internal sealed class SixStarResult
        {
            public bool passed,completed;public string graphics;public SixStarCase[] cases;public string[] errors;
            public string scope="Authored defender trials, normal production selection/start/pointer deployment with existing finite cards and automatic natural SP. Real camera body/native FX differences, pause and restart. Programmatic pointer input, not OS mouse or natural campaign balance. No health/SP/target/damage/skill/result injection.";
        }
        static SpriteRenderer[] SixStarFx(string id)=>Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.sprite!=null&&r.sprite.name.StartsWith("OriginalVfx/defender/"+id+"/",StringComparison.Ordinal)).ToArray();
        static bool SixStarSelected(string id,string key)
        {
            string token=id=="D21"?"whitw2_skill_03_":id=="D22"?"yu_skill3_":id=="D23"?"monstr_skill_03_":id=="D24"?"angel2_skill_3_":id=="D25"?"hsgma2_skill_03_":id=="D26"?"svash2_skill_03_":id=="D27"?"sbell2_skill_03_":id=="D28"?"wang_skill_03_":id=="D29"?"demkni_skill_03_":id=="D30"?"mlynar_skill_03_":id=="D32"?"chen2_skill_03_":id=="D33"?"aglna2_skill_03_":id=="D34"?"clemnt_skill_03_":"kalts2_skill_02_";
            return key.Contains("/"+token)||id=="D28"&&key.EndsWith("/wang_passive_01_buff_02",StringComparison.Ordinal);
        }
        static bool SixStarNewHit(string id,string key)=>key.EndsWith(id=="D32"?"/chen2_skill_03_hit_01":id=="D33"?"/aglna2_skill_03_hit_01":id=="D34"?"/clemnt_skill_03_hit_01":"/kalts2_skill_02_range_damage_01",StringComparison.Ordinal);
        static string SixStarFrames(IEnumerable<SpriteRenderer> fx)=>string.Join("|",fx.OrderBy(r=>r.GetEntityId().ToString()).Select(r=>r.GetEntityId()+"/"+r.sprite.GetEntityId()+"/"+r.transform.position+"/"+r.transform.lossyScale));
        static int SixStarPixels(SpriteRenderer[] renderers,string directory,string label,bool save)
        {
            var camera=Camera.main;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            var target=new RenderTexture(640,360,24);var a=new Texture2D(640,360,TextureFormat.RGB24,false);var b=new Texture2D(640,360,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;a.ReadPixels(new Rect(0,0,640,360),0,0);a.Apply();
                foreach(var r in renderers)r.enabled=false;
                camera.Render();b.ReadPixels(new Rect(0,0,640,360),0,0);b.Apply();var first=a.GetPixels32();var second=b.GetPixels32();int pixels=0;
                for(int i=0;i<first.Length;i++)if(Math.Abs(first[i].r-second[i].r)+Math.Abs(first[i].g-second[i].g)+Math.Abs(first[i].b-second[i].b)>12)pixels++;
                if(save){File.WriteAllBytes(Path.Combine(directory,label+".png"),a.EncodeToPNG());File.WriteAllBytes(Path.Combine(directory,label+"-without.png"),b.EncodeToPNG());}
                return pixels;
            }
            finally
            {
                foreach(var r in renderers)if(r!=null)r.enabled=true;camera.targetTexture=oldTarget;RenderTexture.active=oldActive;target.Release();Destroy(target);Destroy(a);Destroy(b);
            }
        }
        internal IEnumerator VerifySixStarRoster(string directory,string only=null)
        {
            Directory.CreateDirectory(directory);diagnostic=true;Application.runInBackground=true;
            var results=new List<SixStarCase>();var errors=new List<string>();var result=new SixStarResult{graphics=SystemInfo.graphicsDeviceName};
            Application.LogCallback listener=(condition,trace,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(condition);};Application.logMessageReceived+=listener;
            float previous=Time.timeScale;Time.timeScale=0;
            try
            {
                var ids=DefenderBatch4Ids.Concat(DefenderBatch5Ids).Concat(DefenderBatch6Ids).Concat(DefenderBatch7Ids).Where(id=>only==null||id==only).ToArray();
                foreach(string id in ids)
                {
                    var item=new SixStarCase{id=id};results.Add(item);var checks=new List<string>();bool pass=true;
                    void Check(string name,bool value){checks.Add(name+"="+value);pass&=value;Debug.Log("SIXSTAR_CHECK "+id+" "+name+"="+value);}
                    var data=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);Check("authored-trial-exists",data!=null);if(data==null){item.checks=checks.ToArray();continue;}
                    var cards=new[]{data.battle.Card}.Concat((data.rosterCards??Array.Empty<ManualBattleDefinition>()).Select(c=>c.Card));bool entered=false;
                    yield return Prepare(Plan(data.defenders,cards,data.Tactical),()=>entered=TryEnterDefenderSkillTrial(id),"六星实机检查 "+id);
                    yield return null;yield return null;Check("production-selection",entered&&DefenderSkillTrialId==id);
                    var battle=Battle;var defender=battle.FindDefender(id);var body=Object.FindAnyObjectByType<DefenderRosterView>()?.FindVisual(id);
                    Check("body-and-native-director",defender!=null&&body!=null&&DefenderVfxArt.SupportsDefender(defender));
                    Tick(4);yield return null;Check("preparation-animation-only",battle.Clock==0&&defender.SkillCasts==0&&defender.SkillSP==defender.Settings.Skill.InitialSP);
                    item.scene=SceneManager.GetActiveScene().name;
                    if(body!=null){item.bodyPixels=SixStarPixels(new[]{body},directory,id+"-body",true);Check("actual-body-gpu",item.bodyPixels>20);}
                    StartBattle();battle.SetPaused(true);Vector2 gate=Camera.main.WorldToScreenPoint(battle.Entry);
                    bool Deploy(){ProcessPointerDown(ScreenPosition(CardSlot(2).center));ProcessPointerMove(gate);bool accepted=ProcessPointerUp(gate).Success;if(accepted)item.deployments++;return accepted;}
                    Check("paused-pointer-deploy",Deploy());double pausedClock=battle.Clock;float sp=defender.SkillSP;Tick(3);yield return null;
                    Check("paused-clock-and-sp",battle.Clock==pausedClock&&defender.SkillSP==sp);battle.SetPaused(false);
                    var seen=new HashSet<string>();int extras=0,probes=0,hitProbes=0;bool pausedFx=false,captured=false;double firstCast=double.NaN;
                    for(int tick=0;tick<2000&&battle.Phase==BattlePhase.Running;tick++)
                    {
                        Tick(.1f);yield return null;
                        if(extras<5&&tick>20+extras*20){if(Deploy())extras++;}
                        if(defender.SkillCasts>0&&double.IsNaN(firstCast))firstCast=battle.Clock;
                        var allFx=SixStarFx(id);foreach(var renderer in allFx)seen.Add(renderer.name);
                        var fx=allFx.Where(r=>SixStarSelected(id,r.name)).ToArray();
                        if(Array.IndexOf(DefenderBatch7Ids,id)>=0&&defender.SkillCasts>0&&hitProbes<24)
                        {
                            var hits=allFx.Where(r=>SixStarNewHit(id,r.name)).ToArray();
                            if(hits.Length>0&&battle.CombatShots.Any(s=>s.SourceId==id&&s.Applied&&s.DamageDealt>0))
                            {int pixels=SixStarPixels(hits,directory,id+"-target-hit",item.hitPixels==0);hitProbes++;item.hitPixels=Math.Max(item.hitPixels,pixels);}
                        }
                        if(defender.SkillCasts>0&&fx.Length>0&&probes<24&&(tick%3==0||!captured))
                        {
                            int pixels=SixStarPixels(fx,directory,id+"-skill",!captured);probes++;item.skillPixels=Math.Max(item.skillPixels,pixels);
                            if(pixels>20&&(item.skillBodyPixels<=20||item.monsterPixels<=20))
                            {
                                item.skillBodyPixels=Math.Max(item.skillBodyPixels,SixStarPixels(new[]{body},directory,id+"-body-during-skill",item.skillBodyPixels<=20));
                                var monsters=Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Where(v=>v.EntityId!=null&&battle.FindUnit(v.EntityId)?.Active==true&&v.Renderer!=null&&v.Renderer.enabled).Select(v=>v.Renderer).ToArray();
                                if(monsters.Length>0)item.monsterPixels=Math.Max(item.monsterPixels,SixStarPixels(monsters,directory,id+"-monsters-during-skill",item.monsterPixels<=20));
                            }
                            if(pixels>20&&!captured){captured=true;yield return TacticalFrame(directory,id+"-visible-player");}
                            if(pixels>20&&!pausedFx)
                            {
                                battle.SetPaused(true);double clock=battle.Clock;var health=battle.Units.Select(u=>u.Health).ToArray();var positions=battle.Units.Select(u=>u.Motion.Position).ToArray();
                                var guardHealth=battle.Defenders.Select(d=>d.Health).ToArray();var guardPositions=battle.Defenders.Select(d=>d.Position).ToArray();var bodyFrame=body.sprite;
                                string frame=SixStarFrames(SixStarFx(id));Tick(2);yield return null;
                                Check("active-native-pause-freezes-frames-and-combat",battle.Clock==clock&&health.SequenceEqual(battle.Units.Select(u=>u.Health))&&positions.SequenceEqual(battle.Units.Select(u=>u.Motion.Position))&&
                                    guardHealth.SequenceEqual(battle.Defenders.Select(d=>d.Health))&&guardPositions.SequenceEqual(battle.Defenders.Select(d=>d.Position))&&body.sprite==bodyFrame&&SixStarFrames(SixStarFx(id))==frame);
                                pausedFx=true;battle.SetPaused(false);
                            }
                        }
                        if(captured&&pausedFx&&!double.IsNaN(firstCast)&&battle.Clock-firstCast>8&&(Array.IndexOf(DefenderBatch7Ids,id)<0||item.hitPixels>0))break;
                    }
                    item.casts=defender.SkillCasts;item.clock=battle.Clock;item.outcome=battle.Phase.ToString();
                    item.shots=battle.CombatShots.Count(s=>s.SourceId==id);item.damageShots=battle.CombatShots.Count(s=>s.SourceId==id&&s.Applied&&s.DamageDealt>0);
                    Check("natural-auto-skill",item.casts>0);Check("actual-original-skill-gpu",item.skillPixels>20&&pausedFx);
                    Check("body-and-monsters-remain-visible-during-selected-fx",item.skillBodyPixels>20&&item.monsterPixels>20);
                    if(Array.IndexOf(DefenderBatch7Ids,id)>=0){Check("new-skill-real-interaction",Batch7SkillObserved(id));Check("real-selected-target-hit-gpu",item.hitPixels>0);}
                    RestartBattle();yield return null;yield return null;var reset=Battle.FindDefender(id);
                    Check("restart-clears-real-combat-and-native-effects",Battle.Clock==0&&Battle.Units.Count==0&&reset.SkillCasts==0&&reset.Health==reset.MaxHealth&&SixStarFx(id).Length==0);
                    ReturnToMonsterTests();yield return null;yield return null;Check("returns-to-normal-selection",TestSelectionOpen&&!DefenderSkillTrialActive);
                    item.sourceKeys=seen.OrderBy(s=>s).ToArray();item.checks=checks.ToArray();item.passed=pass;Debug.Log("SIXSTAR_CASE "+JsonUtility.ToJson(item));
                    File.WriteAllText(Path.Combine(directory,"progress.json"),JsonUtility.ToJson(new SixStarResult{cases=results.ToArray(),errors=errors.ToArray()},true));
                }
                result.completed=true;
            }
            finally
            {
                Time.timeScale=previous;Application.logMessageReceived-=listener;result.cases=results.ToArray();result.errors=errors.ToArray();result.passed=result.completed&&results.Count>0&&results.All(r=>r.passed)&&errors.Count==0;
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));Debug.Log("SIXSTAR_ROSTER_RESULT passed="+result.passed+" cases="+results.Count+" errors="+errors.Count);Application.Quit(result.passed?0:1);
            }
        }
    }
    public sealed class SixStarRosterSmokeDriver:MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-sixStarRosterSmokeTest"))return;started=true;
            var host=new GameObject("Explicit six-star roster Player observer");DontDestroyOnLoad(host);host.AddComponent<SixStarRosterSmokeDriver>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;yield return null;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir"),role=Array.IndexOf(args,"-sixStarOnly");
            string directory=at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.Combine(Application.persistentDataPath,"SixStarRosterEvidence");
            yield return Object.FindAnyObjectByType<ManualBattleController>().VerifySixStarRoster(directory,role>=0&&role+1<args.Length?args[role+1]:null);
        }
    }
}
