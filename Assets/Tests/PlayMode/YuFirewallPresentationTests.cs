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
        static MeshRenderer[] Visible(GameObject root)=>root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Yu firewall / ")).ToArray();
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
            var renderers=Visible(fx.gameObject);Assert.That(renderers.Length,Is.EqualTo(6),"Layered quads are batched, not one object per flame.");
            var body=renderers.Single(r=>r.name.EndsWith("connected fire"));var mesh=body.GetComponent<MeshFilter>().sharedMesh;
            var oldUV=mesh.uv;c.Tick(.12f);yield return null;Assert.That(mesh.uv.SequenceEqual(oldUV),Is.False);
            var image=Capture(true);foreach(var r in renderers)r.enabled=false;var hidden=Capture(false);foreach(var r in renderers)r.enabled=true;
            int reds=image.Zip(hidden,(a,b)=>a.r>110&&a.r>a.g*1.7f&&a.r>a.b*1.5f&&a.r-b.r>20?1:0).Sum();
            Assert.That(reds,Is.GreaterThan(100),"Actual GPU output must have visible red fire, not just correctly named meshes.");
            c.Battle.SetPaused(true);double clock=c.Battle.Clock;var vertices=mesh.vertices;oldUV=mesh.uv;c.Tick(10);yield return null;
            Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(mesh.vertices,Is.EqualTo(vertices));Assert.That(mesh.uv,Is.EqualTo(oldUV));
            c.RestartBattle();yield return null;Assert.That(Visible(fx.gameObject),Is.Empty);
            c.SwitchTrial(false);yield return null;
        }
        [UnityTest]public IEnumerator WallFollowsBothRealNormalsAndEmbersEndOnSimulationClock()
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
                    var body=Visible(root).Single(r=>r.name.EndsWith("connected fire"));var bounds=body.GetComponent<MeshFilter>().sharedMesh.bounds;
                    var axis=Vector3.Cross(b.Defender.WallNormal,Vector3.up);
                    Assert.That(Vector3.Distance(bounds.center,b.DefenderWallPoint(b.Defender)),Is.LessThan(.5));
                    Assert.That(Math.Abs(Vector3.Dot(bounds.size,axis)),Is.GreaterThan(11));
                    b.Advance(2.1f);yield return null;Assert.That(b.DefenderWallActive(b.Defender),Is.False);Assert.That(Visible(root),Is.Not.Empty);
                    b.Advance(1.3f);yield return null;Assert.That(Visible(root),Is.Empty);
                    b.Restart();yield return null;Assert.That(root.GetComponentsInChildren<MeshRenderer>(),Is.Empty);
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
                if(save)System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath("artifacts/yu-firewall-actual.png"),texture.EncodeToPNG());
                return texture.GetPixels32();
            }
            finally{cam.targetTexture=previous;cam.rect=rect;RenderTexture.active=active;target.Release();Object.Destroy(target);Object.Destroy(texture);}
        }
    }
}
