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
        static SpriteRenderer[] Effects()=>Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Talulah attack / ")).ToArray();
        [UnityTest]public IEnumerator ActualSlashFlowsPausesHitsAndClearsWithoutChangingCombatOrFireRain()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.SwitchTrial(true);c.SelectCard(true);Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);
            for(int i=0;i<160&&!c.Battle.CombatShots.Any(s=>!s.FromDefender&&!s.FireRain);i++)c.Tick(.05f);
            var shot=c.Battle.CombatShots.First(s=>!s.FromDefender&&!s.FireRain);
            c.Tick((float)(shot.StartedAt+.47-c.Battle.Clock));yield return null;
            var flame=Effects().First(r=>r.name.EndsWith("flowing flame"));Sprite frame=flame.sprite;var point=flame.transform.position;
            c.Tick(.06f);yield return null;Assert.That(flame.sprite,Is.Not.SameAs(frame));Assert.That(flame.transform.position,Is.Not.EqualTo(point));
            c.Battle.SetPaused(true);double clock=c.Battle.Clock;frame=flame.sprite;point=flame.transform.position;
            c.Tick(10);yield return null;Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(flame.sprite,Is.SameAs(frame));Assert.That(flame.transform.position,Is.EqualTo(point));
            c.Battle.SetPaused(false);c.Tick((float)(shot.ImpactAt+.12-c.Battle.Clock));yield return null;
            Assert.That(shot.Applied,Is.True);Assert.That(Effects().Any(r=>r.name.EndsWith("ballistic ember")),Is.True);
            Assert.That(Effects().Any(r=>r.name.EndsWith("rising smoke")),Is.True);
            Assert.That(c.Battle.CombatSettings.UnitInterval,Is.EqualTo(1.7f));Assert.That(shot.ImpactAt-shot.StartedAt,Is.EqualTo(.65).Within(.001));
            var visible=Effects();var image=Capture(Camera.main,true);foreach(var r in visible)r.enabled=false;
            var hidden=Capture(Camera.main,false);foreach(var r in visible)r.enabled=true;
            Assert.That(image.Zip(hidden,(a,b)=>Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)).Average(),Is.GreaterThan(.02),"Actual GPU image must contain the new layered effects.");
            c.RestartBattle();yield return null;Assert.That(Effects(),Is.Empty);
            c.SelectCard(true);c.TryDeployAt(c.Battle.Entry);c.Tick(4.05f);c.SelectSkillUnit(c.Battle.Units[0].EntityId);
            Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None));c.Tick(.45f);yield return null;
            Assert.That(Object.FindAnyObjectByType<FireRainView>().VisibleStreams,Is.GreaterThan(0));
            c.SwitchTrial(false);yield return null;Assert.That(Effects(),Is.Empty);
        }
        [UnityTest]public IEnumerator NaturalShotsFromOppositeSidesOrientTheFlameTowardTheirOwnTarget()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.SwitchTrial(true);
            var type=typeof(EngagementView).Assembly.GetType("YesterdayConvoy.TalulahAttackVfx");
            foreach(int side in new[]{-1,1})
            {
                var root=new GameObject("Talulah direction fixture");var helper=Activator.CreateInstance(type,new object[]{root.transform});
                try
                {
                    var b=new ManualBattle(c.Definition.Snapshot(),new[]{Vector3.right*side*3,Vector3.right*-side*8},Vector3.zero,c.Definition.Engagement.Snapshot());
                    b.Begin();b.TryDeploy(b.RunId,1,b.Entry);b.Advance(.5f);var shot=b.CombatShots.First(s=>!s.FromDefender);
                    type.GetMethod("Begin").Invoke(helper,null);type.GetMethod("Present").Invoke(helper,new object[]{shot,b.Clock,Camera.main});type.GetMethod("End").Invoke(helper,null);
                    var fire=root.GetComponentsInChildren<SpriteRenderer>().First(r=>r.enabled&&r.name.EndsWith("flowing flame"));
                    var toward=Vector3.ProjectOnPlane(shot.Destination-shot.Origin,Camera.main.transform.forward).normalized;
                    Assert.That(Vector3.Dot(fire.transform.up,toward),Is.GreaterThan(.9f));
                }
                finally{((IDisposable)helper).Dispose();Object.Destroy(root);}
            }
        }
        static Color32[] Capture(Camera camera,bool save)
        {
            var target=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGBA32,false);
            var old=camera.targetTexture;var active=RenderTexture.active;var rect=camera.rect;
            try
            {
                camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                if(save){string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../artifacts"));System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"talulah-attack-actual.png"),texture.EncodeToPNG());}
                return texture.GetPixels32();
            }
            finally{camera.targetTexture=old;camera.rect=rect;RenderTexture.active=active;target.Release();Object.Destroy(target);Object.Destroy(texture);}
        }
    }
}
