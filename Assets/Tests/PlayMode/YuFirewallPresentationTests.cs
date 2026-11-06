using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class YuFirewallPresentationTests
    {
        float previous;
        [SetUp]public void Setup()=>previous=Time.timeScale;
        [TearDown]public void Cleanup()=>Time.timeScale=previous;
        static SpriteRenderer[] Visible(GameObject root)=>root.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null&&
            r.name.StartsWith("Original VFX / defender/D22/yu_skill3_range_01",StringComparison.Ordinal)).ToArray();
        static SpriteRenderer[] GridSegments(GameObject root)=>Visible(root).Where(r=>!r.name.EndsWith("_end",StringComparison.Ordinal)).ToArray();
        [UnityTest]public IEnumerator ActualYuTrialRendersRedFireAfterGestureFreezesAndRestarts()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.TryEnterDefenderSkillTrial("D22"),Is.True);yield return null;
            c.StartBattle();var gate=Camera.main.WorldToScreenPoint(c.Battle.Entry);
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.CardSlot(2).center));c.ProcessPointerMove(gate);Assert.That(c.ProcessPointerUp(gate).Success,Is.True);
            var d=c.Battle.FindDefender("D22");var fx=Object.FindAnyObjectByType<DefenderSkillVfxView>();bool before=false,seen=false;
            for(int i=0;i<2000&&d.Alive&&!seen;i++)
            {
                c.Tick(.05f);
                if(i==45){c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.CardSlot(3).center));c.ProcessPointerMove(gate);c.ProcessPointerUp(gate);}
                if(d.SkillCasts>0&&c.Battle.Clock<d.SkillActionUntil){yield return null;Assert.That(Visible(fx.gameObject),Is.Empty);before=true;}
                if(c.Battle.DefenderWallActive(d)&&c.Battle.Clock>d.SkillActionUntil+.6){yield return null;seen=Visible(fx.gameObject).Length>0;}
                if(i%100==0)yield return null;
            }
            Assert.That(before&&seen,Is.True,"Original gesture must precede the real skill wall.");
            var renderers=Visible(fx.gameObject);Assert.That(renderers.Length,Is.EqualTo(14),"Twelve native grid children and two spatial end caps.");
            Assert.That(GridSegments(fx.gameObject).Length,Is.EqualTo(12));
            Assert.That(renderers.All(r=>r.sharedMaterial.shader.name=="YesterdayConvoy/TalulahNative"&&r.transform.localScale==Vector3.one),Is.True);
            var body=GridSegments(fx.gameObject)[0];var priorFrame=body.sprite;c.Tick(.12f);yield return null;
            Assert.That(body.sprite,Is.Not.SameAs(priorFrame),"The source particle capture must advance on the simulation clock.");
            var image=Capture(true);foreach(var r in renderers)r.enabled=false;var hidden=Capture(false);foreach(var r in renderers)r.enabled=true;
            int reds=image.Zip(hidden,(a,b)=>a.r>110&&a.r>a.g*1.7f&&a.r>a.b*1.5f&&a.r-b.r>20?1:0).Sum();
            Assert.That(reds,Is.GreaterThan(100),"Actual GPU output must have visible original red fire, not just correctly named renderers.");
            c.Battle.SetPaused(true);double clock=c.Battle.Clock;
            var frames=renderers.Select(r=>r.sprite).ToArray();var positions=renderers.Select(r=>r.transform.position).ToArray();
            c.Tick(10);yield return null;Assert.That(c.Battle.Clock,Is.EqualTo(clock));
            for(int n=0;n<renderers.Length;n++){Assert.That(renderers[n].sprite,Is.SameAs(frames[n]));Assert.That(renderers[n].transform.position,Is.EqualTo(positions[n]));}
            c.RestartBattle();yield return null;Assert.That(Visible(fx.gameObject),Is.Empty);
            c.SwitchTrial(false);yield return null;
        }
        [UnityTest]public IEnumerator WallFollowsBothRealNormalsAndOriginalLoopsClearOnSimulationClock()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            foreach(var position in new[]{Vector3.right*2,Vector3.forward*2})
            {
                var skill=new DefenderSkillSettings(DefenderSkillKind.YuKitchen,100,100,1,4,healthMultiplier:1.5f,effectRadius:10);
                var spec=new DefenderSettings("D22","Yu","D22",position,100000,1,0,20,2,skill:skill);
                var b=new ManualBattle(new ManualBattleSettings("c","u",30,1,20,1,.1f,100000,.1f,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},Vector3.one,new EngagementSettings(unitAttack:1,unitRange:20),roster:new[]{spec});
                ManualBattle binding=b;var root=new GameObject("Yu wall orientation fixture");root.AddComponent<DefenderSkillVfxView>().Bind(()=>binding);
                try
                {
                    b.Begin();b.TryDeploy(b.RunId,1,b.Entry);b.Advance(1.5f);yield return null;Assert.That(Visible(root),Is.Empty);
                    b.Advance(.6f);yield return null;Assert.That(b.DefenderWallActive(b.Defender),Is.True);
                    var walls=Visible(root);Assert.That(walls.Length,Is.EqualTo(14));
                    var center=b.DefenderWallPoint(b.Defender);var axis=Vector3.Cross(b.Defender.WallNormal,Vector3.up).normalized;
                    // The source sprites billboard; gameplay geometry lives in their independent world attachments.
                    var anchors=walls.Select(r=>r.transform.parent.position).ToArray();
                    var mean=anchors.Aggregate(Vector3.zero,(a,z)=>a+z)/anchors.Length;
                    Assert.That(Vector3.Distance(mean,center),Is.LessThan(.01));
                    var extent=anchors.Select(p=>Vector3.Dot(p-center,axis)).ToArray();
                    Assert.That(extent.Max()-extent.Min(),Is.EqualTo(11.52).Within(.02));
                    Assert.That(anchors.All(p=>Math.Abs(Vector3.Dot(p-center,b.Defender.WallNormal))<.01),Is.True);
                    // Native *_end keys are spatial caps, not a temporal ember/outro animation.
                    // The missing original pool/StopEmitting controller is explicitly adapted by clearing this loop.
                    b.Advance(2.1f);yield return null;Assert.That(b.DefenderWallActive(b.Defender),Is.False);Assert.That(Visible(root),Is.Empty);
                    b.Advance(1.3f);yield return null;Assert.That(Visible(root),Is.Empty);
                    b.Restart();yield return null;Assert.That(Visible(root),Is.Empty);
                    binding=null;yield return null;
                }
                finally{Object.Destroy(root);}
            }
        }
        static Color32[] Capture(bool save)
        {
            var cam=Camera.main;var target=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGBA32,false);
            var previous=cam.targetTexture;var active=RenderTexture.active;var rect=cam.rect;
            try
            {
                cam.targetTexture=target;cam.rect=new Rect(0,0,1,1);cam.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                if(save&&Environment.GetCommandLineArgs().Contains("-defenderVfxEvidence"))System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath("artifacts/yu-firewall-actual.png"),texture.EncodeToPNG());
                return texture.GetPixels32();
            }
            finally{cam.targetTexture=previous;cam.rect=rect;RenderTexture.active=active;target.Release();Object.Destroy(target);Object.Destroy(texture);}
        }
    }
}
