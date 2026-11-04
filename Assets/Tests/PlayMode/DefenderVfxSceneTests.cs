using System.Collections;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderVfxSceneTests
    {
        [UnityTest] public IEnumerator ActualLateMapShowsSixEffectsAndKeepsWinningDeploymentPlan()
        {
            float prior=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartLateBattleShopLoop(73);yield return null;
                var plan=JsonUtility.FromJson<StrongholdChallengePlan.Plan>(Resources.Load<TextAsset>("StrongholdChallengeSolution").text);
                string[] wanted={"wolf-wake","yu-fire","snow-front","wang-ink","saria-field","gold-sweep"};
                var seen=new System.Collections.Generic.HashSet<string>();int index=0,video=0;bool recording=false;
                Directory.CreateDirectory(Path.GetFullPath("artifacts/vfx-map-frames"));
                for(int tick=0;tick<5200&&c.Loop.CanDeploy;tick++)
                {
                    while(index<plan.orders.Length&&plan.orders[index].time<=c.Battle.Clock+.001)
                    {
                        var order=plan.orders[index++];int bench=c.Shop.Bench.ToList().FindIndex(p=>p.Id==order.card);
                        var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Lanes[order.lane].Entry));
                        c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(bench).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
                    }
                    c.Tick(.05f);yield return null;
                    var fx=Object.FindAnyObjectByType<DefenderSkillVfxView>();
                    if(fx!=null)foreach(string key in wanted)
                        if(!seen.Contains(key)&&(key=="yu-fire"?HasYuWall(fx.gameObject):fx.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.sprite!=null&&r.name.EndsWith(key)&&FrameIndex(r)>=6&&FrameIndex(r)<=20)))
                        {seen.Add(key);Capture("actual-map-"+key);if(key=="snow-front")recording=true;}
                    if(recording&&video<60)Capture("vfx-map-frames/"+(video++).ToString("D4"));
                }
                Assert.That(seen,Is.EquivalentTo(wanted),"Actual map effects observed: "+string.Join(",",seen));
                Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won));
                c.RestartBattle();yield return null;
                var restarted=Object.FindAnyObjectByType<DefenderSkillVfxView>();
                Assert.That(restarted.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.name.StartsWith("VFX /")),Is.False);
                c.LeaveShopTrial();
            }
            finally{Time.timeScale=prior;}
        }
        static int FrameIndex(SpriteRenderer renderer)
        {string name=renderer.sprite.name;return int.TryParse(name.Substring(name.LastIndexOf('/')+1),out var index)?index:-1;}
        static bool HasYuWall(GameObject root)=>root.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled&&r.name=="Yu firewall / connected fire");
        [UnityTest] public IEnumerator SixLiveSkillEventsRenderFreezeAndClearWithoutAffectingCombat()
        {
            float prior=Time.timeScale;GameObject root=null;
            try
            {
                yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
                string[] ids={"D21","D22","D27","D28","D29","D30"};
                var kinds=new[]{DefenderSkillKind.LapplandHavoc,DefenderSkillKind.YuKitchen,DefenderSkillKind.PramanixSnow,DefenderSkillKind.WangGambit,DefenderSkillKind.SariaCalcification,DefenderSkillKind.MlynarGlory};
                string[] expected={"wolf-wake","yu-fire","snow-front","wang-ink","saria-field","gold-sweep"};
                for(int k=0;k<ids.Length;k++)
                {
                    var skill=new DefenderSkillSettings(kinds[k],1,1,1,10,healFraction:.2f,healthMultiplier:1.5f,effectRadius:10,damageAmplification:1.4f,moveMultiplier:.6f,elementFraction:.5f,eraseChance:1,chargeSeconds:2,targets:3);
                    var spec=new DefenderSettings(ids[k],ids[k],ids[k],Vector3.right*2,100000,20,0,20,2,role:k==4?DefenderRole.Guardian:DefenderRole.Fighter,windup:.15f,actionDuration:.5f,skill:skill,canAttack:k!=2&&k!=5);
                    var b=new ManualBattle(new ManualBattleSettings("c","u",30,1,20,1,.1f,100000,.3f,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                        new EngagementSettings(unitAttack:10,unitInterval:1,unitRange:20,projectileTime:.2f),roster:new[]{spec});
                    ManualBattle binding=b;root=new GameObject("Real VFX test "+ids[k]);root.AddComponent<AdvancedDefenderView>().Bind(()=>binding);
                    var actor=new GameObject("Native operator",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();actor.transform.SetParent(root.transform);actor.transform.SetPositionAndRotation(spec.Position,Camera.main.transform.rotation);actor.sprite=DefenderArt.Load(ids[k])["idle"].Frames[0];actor.sortingOrder=DemoSpriteVisual.GetSortingOrder(Camera.main,spec.Position)+30;
                    b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);
                    bool observed=false,between=false;
                    for(int i=0;i<240;i++)
                    {
                        b.Advance(.05f);yield return null;
                        var active=root.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null&&r.name.StartsWith("VFX /")).ToArray();
                        if(k==2)
                        {
                            Assert.That(active.Count(r=>r.name.Contains("snow-")),Is.LessThanOrEqualTo(2),"One pair per attack, independent of target count");
                            if(observed&&b.Clock-b.Defender.LastActionTime>1.1&&b.Clock-b.Defender.LastActionTime<1.8)between|=!active.Any(r=>r.name.Contains("snow-"));
                        }
                        if(observed||!(k==1?HasYuWall(root):active.Any(r=>r.name.EndsWith(expected[k]))))continue;
                        observed=true;Capture(ids[k]+"-vfx");
                        b.SetPaused(true);var frames=active.Select(r=>r.sprite).ToArray();var positions=active.Select(r=>r.transform.position).ToArray();double clock=b.Clock;float hp=b.Units[0].Health;
                        for(int f=0;f<3;f++){b.Advance(1);yield return null;}
                        Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.Units[0].Health,Is.EqualTo(hp));
                        for(int n=0;n<active.Length;n++){Assert.That(active[n].sprite,Is.SameAs(frames[n]));Assert.That(active[n].transform.position,Is.EqualTo(positions[n]));}
                        b.SetPaused(false);
                        if(k!=2)break;
                    }
                    Assert.That(observed,Is.True,ids[k]+" casts="+b.Defender.SkillCasts+" clock="+b.Clock);
                    if(k==2)Assert.That(between,Is.True,"Attack interval must have no attack snow effect");
                    b.Restart();yield return null;
                    Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.name.StartsWith("VFX /")),Is.Empty);
                    Assert.That(HasYuWall(root),Is.False);
                    binding=null;yield return null;Object.Destroy(root);root=null;yield return null;
                }
            }
            finally{if(root!=null)Object.Destroy(root);Time.timeScale=prior;}
        }
        static void Capture(string name)
        {
            var cam=Camera.main;var previous=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;
            var target=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{cam.targetTexture=target;cam.aspect=16f/9;cam.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Path.GetFullPath("artifacts/"+name+".png"),texture.EncodeToPNG());}
            finally{cam.targetTexture=previous;cam.aspect=aspect;RenderTexture.active=active;Object.Destroy(target);Object.Destroy(texture);}
        }
    }
}
