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
    public sealed class TalulahAttackPresentationTests
    {
        float oldTime;
        [SetUp]public void Setup()=>oldTime=Time.timeScale;
        [TearDown]public void Cleanup()=>Time.timeScale=oldTime;
        static SpriteRenderer[] Effects()=>Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Talulah native / attack-")).ToArray();
        static int Frame(SpriteRenderer renderer)=>int.Parse(renderer.sprite.name.Substring(renderer.sprite.name.LastIndexOf('/')+1));

        [UnityTest]public IEnumerator NativeCasterAndActualTargetHitAnimateIndependentlyWithoutChangingCombatOrFireRain()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.SwitchTrial(true);c.SelectCard(true);
            Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);
            for(int i=0;i<160&&!c.Battle.CombatShots.Any(s=>!s.FromDefender&&!s.FireRain);i++)c.Tick(.05f);
            var shot=c.Battle.CombatShots.First(s=>!s.FromDefender&&!s.FireRain);
            c.Tick((float)(shot.ImpactAt-.02-c.Battle.Clock));yield return null;
            Assert.That(Effects().Any(r=>r.name.EndsWith("attack-A-caster")),Is.True,"Native caster effects belong to the original windup.");
            Assert.That(Effects().Any(r=>r.name.EndsWith("attack-A-hit")),Is.False,"Target effects must wait for the real hit.");
            c.Tick(.10f);yield return null;
            Assert.That(shot.Applied,Is.True);
            var flame=Effects().Single(r=>r.name.EndsWith("attack-A-caster"));int frame=Frame(flame);
            Assert.That(Effects().Any(r=>r.name.EndsWith("attack-A-hit")),Is.True);
            Assert.That(flame.sharedMaterial.shader.name,Is.EqualTo("YesterdayConvoy/TalulahNative"));
            Assert.That(flame.sharedMaterial.shader.isSupported,Is.True);
            Assert.That(frame,Is.InRange(32,36),"Native caster frames must match the actor's source-time recovery.");
            var visible=Effects();var image=Capture(Camera.main,"talulah-native-actual.png");
            foreach(var r in visible)r.enabled=false;
            var hidden=Capture(Camera.main,null);foreach(var r in visible)r.enabled=true;
            Assert.That(image.Zip(hidden,(a,b)=>Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)).Average(),Is.GreaterThan(.02),"Actual GPU image must contain native caster and target effects.");
            c.Tick(.05f);yield return null;Assert.That(Frame(flame),Is.GreaterThan(frame));
            c.Battle.SetPaused(true);double clock=c.Battle.Clock;frame=Frame(flame);Vector3 point=flame.transform.position;
            c.Tick(10);yield return null;Assert.That(c.Battle.Clock,Is.EqualTo(clock));
            Assert.That(Frame(flame),Is.EqualTo(frame));Assert.That(flame.transform.position,Is.EqualTo(point));
            Assert.That(c.Battle.CombatSettings.UnitInterval,Is.EqualTo(1.7f));
            Assert.That(shot.ImpactAt-shot.StartedAt,Is.EqualTo(.65).Within(.001));
            c.Battle.SetPaused(false);c.Tick(.70f);yield return null;
            Assert.That(Effects().All(r=>r.sharedMaterial.shader.name=="YesterdayConvoy/TalulahNative"),Is.True);
            c.RestartBattle();yield return null;Assert.That(Effects(),Is.Empty);
            c.SelectCard(true);c.TryDeployAt(c.Battle.Entry);c.Tick(4.05f);c.SelectSkillUnit(c.Battle.Units[0].EntityId);
            Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None));c.Tick(.45f);yield return null;
            Assert.That(Object.FindAnyObjectByType<FireRainView>().VisibleStreams,Is.GreaterThan(0));
            c.SwitchTrial(false);yield return null;Assert.That(Effects(),Is.Empty);
        }

        [UnityTest]public IEnumerator OppositeRealShotsMirrorTheWholeBladeAndConcurrentFramesStayIndependent()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.SwitchTrial(true);
            var type=typeof(EngagementView).Assembly.GetType("YesterdayConvoy.TalulahAttackVfx");
            var root=new GameObject("Talulah facing fixture");var helper=Activator.CreateInstance(type,new object[]{root.transform});
            try
            {
                type.GetMethod("Begin").Invoke(helper,null);
                foreach(int side in new[]{-1,1})
                {
                    var b=new ManualBattle(c.Definition.Snapshot(),new[]{Vector3.right*side*3,Vector3.right*-side*8},Vector3.zero,c.Definition.Engagement.Snapshot());
                    b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);b.Advance(.75f);
                    var shot=b.CombatShots.First(s=>!s.FromDefender);
                    Assert.That(shot.Applied,Is.True);
                    double clock=shot.ImpactAt+(side<0?.05:.10);
                    type.GetMethod("Present").Invoke(helper,new object[]{shot,clock,Camera.main,1.3f/1.05f});
                    var fire=root.GetComponentsInChildren<SpriteRenderer>().Last(r=>r.enabled);
                    Assert.That(fire.flipX,Is.EqualTo(side>0),"Right-facing native frames mirror towards the actual shot target.");
                }
                type.GetMethod("End").Invoke(helper,null);
                var simultaneous=root.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled).ToArray();
                Assert.That(simultaneous.Length,Is.EqualTo(4));
                var casters=simultaneous.Where(r=>r.name.EndsWith("attack-A-caster")).ToArray();
                Assert.That(Frame(casters[0]),Is.LessThan(Frame(casters[1])),"Each attack must keep its own native frame.");
                Capture(Camera.main,"talulah-native-directions.png");
                type.GetMethod("Clear").Invoke(helper,null);
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled),Is.False);
            }
            finally{((IDisposable)helper).Dispose();Object.Destroy(root);}
        }

        static Color32[] Capture(Camera camera,string filename)
        {
            var target=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGBA32,false);
            var old=camera.targetTexture;var active=RenderTexture.active;var rect=camera.rect;
            try
            {
                camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                if(filename!=null){string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../artifacts"));System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,filename),texture.EncodeToPNG());}
                return texture.GetPixels32();
            }
            finally{camera.targetTexture=old;camera.rect=rect;RenderTexture.active=active;target.Release();Object.Destroy(target);Object.Destroy(texture);}
        }
    }
}
