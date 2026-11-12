using System;
using System.Collections;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderVfxSceneTests
    {
        static SpriteRenderer[] Native(GameObject root,string id)=>root.GetComponentsInChildren<SpriteRenderer>().Where(r=>
            r.enabled&&r.sprite!=null&&r.name.StartsWith("Original VFX / defender/"+id+"/",StringComparison.Ordinal)).ToArray();
        [UnityTest] public IEnumerator ActualLateMapShowsSixEffectsAndKeepsWinningDeploymentPlan()
        {
            float prior=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartLateBattleShopLoop(73);yield return null;
                for(int warm=0;warm<3000&&c.ArtLoading;warm++)yield return null;
                Assert.That(c.ArtLoading,Is.False,"The real late-map art preparation must finish before deployment orders.");
                var plan=JsonUtility.FromJson<StrongholdChallengePlan.Plan>(Resources.Load<TextAsset>("StrongholdChallengeSolution").text);
                string[] ids={"D21","D22","D27","D28","D29","D30"};
                string[] raw={"whitw2_skill_03_token_trail_01","yu_skill3_range_01","sbell2_skill_03_start_01",
                    "wang_passive_01_buff_02","demkni_skill_03_range","mlynar_skill_03_start_01"};
                var seen=new System.Collections.Generic.HashSet<string>();int index=0,video=0;bool recording=false;
                bool evidence=Environment.GetCommandLineArgs().Contains("-defenderVfxEvidence");
                if(evidence)Directory.CreateDirectory(Path.GetFullPath("artifacts/vfx-map-frames"));
                for(int tick=0;tick<5200&&c.Loop.CanDeploy;tick++)
                {
                    while(index<plan.orders.Length&&plan.orders[index].time<=c.Battle.Clock+.001)
                    {
                        var order=plan.orders[index++];int bench=c.Shop.Bench.ToList().FindIndex(p=>p.Id==order.card);
                        Assert.That(bench,Is.GreaterThanOrEqualTo(0),"Winning plan needs its original prepared card: "+order.card);
                        var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Lanes[order.lane].Entry));
                        c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(bench).center);
                        c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
                    }
                    c.Tick(.05f);yield return null;
                    var fx=Object.FindAnyObjectByType<DefenderSkillVfxView>();
                    if(fx!=null)for(int n=0;n<ids.Length;n++)
                        if(!seen.Contains(ids[n])&&Native(fx.gameObject,ids[n]).Any(r=>r.name.Contains(raw[n])&&FrameIndex(r)>=6&&FrameIndex(r)<=20))
                        {seen.Add(ids[n]);Capture("actual-map-original-"+ids[n]);if(ids[n]=="D27")recording=true;}
                    if(evidence&&recording&&video<60)Capture("vfx-map-frames/"+(video++).ToString("D4"));
                }
                Assert.That(seen,Is.EquivalentTo(ids),"Actual native map event families: "+string.Join(",",seen));
                Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won),"Original natural deployment plan must still win without damage or defender shortcuts.");
                c.RestartBattle();yield return null;
                var restarted=Object.FindAnyObjectByType<DefenderSkillVfxView>();
                Assert.That(restarted.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.name.StartsWith("Original VFX /",StringComparison.Ordinal)),Is.False);
                c.LeaveShopTrial();
            }
            finally{Time.timeScale=prior;}
        }
        static int FrameIndex(SpriteRenderer renderer)
        {string name=renderer.sprite.name;return int.TryParse(name.Substring(name.LastIndexOf('/')+1),out int index)?index:-1;}
        [UnityTest] public IEnumerator TenRealSkillEventFamiliesRenderPauseAndRestartWithoutCombatWrites()
        {
            float prior=Time.timeScale;GameObject root=null;
            try
            {
                yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
                string[] ids={"D21","D22","D23","D24","D25","D26","D27","D28","D29","D30"};
                var kinds=new[]{DefenderSkillKind.LapplandHavoc,DefenderSkillKind.YuKitchen,DefenderSkillKind.Mon3trMeltdown,
                    DefenderSkillKind.ExusiaiDelivery,DefenderSkillKind.HoshigumaInferno,DefenderSkillKind.SilverAshReign,
                    DefenderSkillKind.PramanixSnow,DefenderSkillKind.WangGambit,DefenderSkillKind.SariaCalcification,DefenderSkillKind.MlynarGlory};
                for(int k=0;k<ids.Length;k++)
                {
                    string id=ids[k];
                    var skill=new DefenderSkillSettings(kinds[k],1,1,1,10,healFraction:.2f,healthMultiplier:1.7f,effectRadius:10,
                        damageAmplification:1.4f,moveMultiplier:.6f,elementFraction:.5f,eraseChance:1,chargeSeconds:2,targets:3);
                    var spec=new DefenderSettings(id,id,id,Vector3.right*2,100000,20,0,20,2,
                        role:id=="D29"?DefenderRole.Guardian:DefenderRole.Fighter,windup:.15f,actionDuration:.5f,skill:skill,
                        canAttack:id!="D27"&&id!="D30",relayPosition:Vector3.right*2);
                    var b=new ManualBattle(new ManualBattleSettings("c","u",30,1,20,1,.1f,100000,.3f,1,1,1,1),
                        new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                        new EngagementSettings(unitAttack:10,unitInterval:1,unitRange:20,projectileTime:.2f),roster:new[]{spec});
                    ManualBattle binding=b;root=new GameObject("Original VFX event fixture "+id);
                    root.AddComponent<AdvancedDefenderView>().Bind(()=>binding,()=>b.Clock);
                    b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);
                    bool observed=false,quiet=false,nativeTail=false;
                    for(int i=0;i<120;i++)
                    {
                        b.Advance(.05f);yield return null;var active=Native(root,id);
                        if(id=="D27")
                        {
                            Assert.That(active.Count(r=>r.name.EndsWith("sbell2_skill_03_start_01",StringComparison.Ordinal)),Is.LessThanOrEqualTo(1));
                            double since=b.Clock-b.Defender.LastActionTime;
                            if(observed&&since>1.3&&since<1.5)nativeTail|=active.Any(r=>r.name.EndsWith("sbell2_skill_03_start_01",StringComparison.Ordinal));
                            if(observed&&since>1.7&&since<1.95)quiet|=!active.Any(r=>r.name.EndsWith("sbell2_skill_03_start_01",StringComparison.Ordinal));
                        }
                        // A normal native shot can appear before the AI chooses its skill.
                        if(observed||active.Length==0||b.Defender.SkillCasts==0)continue;
                        observed=true;Capture(id+"-original-vfx");
                        b.SetPaused(true);var frames=active.Select(r=>r.sprite).ToArray();var positions=active.Select(r=>r.transform.position).ToArray();
                        double clock=b.Clock;float hp=b.Units[0].Health;
                        for(int f=0;f<3;f++){b.Advance(1);yield return null;}
                        Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.Units[0].Health,Is.EqualTo(hp));
                        for(int n=0;n<active.Length;n++)
                        {
                            Assert.That(active[n].sprite,Is.SameAs(frames[n]));Assert.That(active[n].transform.position,Is.EqualTo(positions[n]));
                            Assert.That(active[n].transform.localScale,Is.EqualTo(Vector3.one));
                        }
                        b.SetPaused(false);if(id!="D27")break;
                    }
                    Assert.That(observed,Is.True,id+" skill casts="+b.Defender.SkillCasts+" clock="+b.Clock);
                    Assert.That(b.Defender.SkillCasts,Is.GreaterThan(0),id+" must reach a real skill event before this fixture passes.");
                    if(id=="D27"){Assert.That(nativeTail,Is.True,"Approved original wind must keep its source tail beyond 1.2 seconds.");Assert.That(quiet,Is.True,"Snow attack wind must stop after its original 1.6333-second duration.");}
                    b.Restart();yield return null;
                    Assert.That(Native(root,id),Is.Empty,"A restarted run must not retain old native shots/fields.");
                    binding=null;yield return null;Object.Destroy(root);root=null;yield return null;
                }
            }
            finally{if(root!=null)Object.Destroy(root);Time.timeScale=prior;}
        }
        // Optional evidence only; routine regressions do not generate hundreds of screenshots.
        static void Capture(string name)
        {
            if(!Environment.GetCommandLineArgs().Contains("-defenderVfxEvidence"))return;
            var cam=Camera.main;var previous=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;
            var target=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                cam.targetTexture=target;cam.aspect=16f/9;cam.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                Directory.CreateDirectory(Path.GetFullPath("artifacts"));File.WriteAllBytes(Path.GetFullPath("artifacts/"+name+".png"),texture.EncodeToPNG());
            }
            finally{cam.targetTexture=previous;cam.aspect=aspect;RenderTexture.active=active;Object.Destroy(target);Object.Destroy(texture);}
        }
    }
}
