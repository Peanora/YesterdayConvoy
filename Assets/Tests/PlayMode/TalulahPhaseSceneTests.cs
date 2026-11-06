using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class TalulahPhaseSceneTests
    {
        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(TalulahPhaseTrialBootstrap.SceneName);
            for(int frame=0;frame<1200;frame++)
            {
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                if(c!=null&&!c.ArtLoading&&!c.TestSelectionOpen&&c.Definition.Card.CardId=="CARD_TALULAH_TRIAL"){yield return null;yield break;}
                yield return null;
            }
            Assert.Fail("Talulah phase scene startup timed out.");
        }
        [UnityTest]public IEnumerator DedicatedSceneUsesThreeStarSnapshotAndActualKroosDamage()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();var bootstrap=Object.FindAnyObjectByType<TalulahPhaseTrialBootstrap>();
            Assert.That(c.ShopTrialActive,Is.False);Assert.That(c.Battle.CombatSettings.Effects.TalulahSecondPhase,Is.True);
            Assert.That(c.Battle.Settings.Health,Is.EqualTo(4250));Assert.That(c.Battle.CombatSettings.UnitAttack,Is.EqualTo(336));Assert.That(c.Battle.CombatSettings.UnitArmor,Is.EqualTo(12));
            Assert.That(c.Battle.Defenders.Count,Is.EqualTo(3));Assert.That(c.Battle.Defenders.All(d=>d.HighGround&&d.Settings.ArtId=="D07"&&d.MaxHealth==50000&&d.Settings.Attack==140&&d.Settings.Interval==1&&d.Settings.Armor==40),Is.True);
            var baseline=bootstrap.encounter.defenders.Select(d=>d.health).ToArray();
            c.SelectCard(true);var receipt=c.TryDeployAt(c.Battle.Entry);Assert.That(receipt.Success,Is.True,c.Feedback);var unit=c.Battle.FindUnit(receipt.EntityId);
            bool firstFall=false,firstRain=false,second=false,finalFall=false,finalRain=false;int hits=0;
            for(int tick=0;tick<2300&&unit.Resolution!=UnitResolution.Dead;tick++)
            {
                // No TryDamageUnit, forced kill, phase setter or test settlement: only actual guard projectiles.
                c.Tick(.1f);firstFall|=unit.TalulahPhase==TalulahPhase.FirstFall;firstRain|=unit.TalulahPhase==TalulahPhase.FirstRain;
                second|=unit.TalulahPhase==TalulahPhase.Second;finalFall|=unit.TalulahPhase==TalulahPhase.FinalFall;finalRain|=unit.TalulahPhase==TalulahPhase.FinalRain;
                hits=c.Battle.CombatShots.Count(s=>s.FromDefender&&s.Resolved&&s.Applied);
                if(tick%10==0)yield return null;
            }
            Assert.That(hits,Is.GreaterThan(0));Assert.That(firstFall&&firstRain&&second&&finalFall&&finalRain,Is.True,"Natural damage must pass both complete death/rain sequences.");
            Assert.That(unit.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(unit.MaxHealth,Is.EqualTo(6375));Assert.That(c.Battle.Defenders.All(d=>d.Alive),Is.True);
            CollectionAssert.AreEqual(baseline,bootstrap.encounter.defenders.Select(d=>d.health));
            c.RestartBattle();Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.Settings.Health,Is.EqualTo(4250));Assert.That(c.Battle.CombatSettings.Effects.TalulahSecondPhase,Is.True);
        }

        [UnityTest]public IEnumerator SecondPhaseManualRainAnimatesShortCasterThenClearsWhileFullRainContinues()
        {
            float previousScale=Time.timeScale;
            try
            {
                yield return Load();Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                c.SelectCard(true);var receipt=c.TryDeployAt(c.Battle.Entry);Assert.That(receipt.Success,Is.True,c.Feedback);
                var unit=c.Battle.FindUnit(receipt.EntityId);bool firstFall=false,firstRain=false;
                for(int tick=0;tick<1800;tick++)
                {
                    // Reach B through the authored Kroos projectiles; no forced death, phase or SP setters.
                    c.Tick(.05f);firstFall|=unit.TalulahPhase==TalulahPhase.FirstFall;firstRain|=unit.TalulahPhase==TalulahPhase.FirstRain;
                    if(unit.TalulahPhase==TalulahPhase.Second&&c.Battle.Clock>=unit.TalulahPhaseStartedAt+3&&c.Battle.IsSkillReady(unit.EntityId))break;
                    if(tick%10==0)yield return null;
                }
                Assert.That(firstFall&&firstRain,Is.True);Assert.That(unit.TalulahPhase,Is.EqualTo(TalulahPhase.Second));
                Assert.That(c.Battle.IsSkillReady(unit.EntityId),Is.True);c.SelectSkillUnit(unit.EntityId);
                Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None));
                double began=unit.FireRainStartedAt,distance=unit.Motion.DistanceTravelled;
                c.Tick(1.25f);yield return null;
                var caster=SecondCast();Sprite first=caster.sprite;
                var stream=Object.FindAnyObjectByType<FireRainView>().GetComponentsInChildren<SpriteRenderer>().First(r=>r.enabled&&r.name=="Horizontal fire stream");
                Vector3 streamPoint=stream.transform.position;
                c.Tick(.25f);yield return null;
                caster=SecondCast();Assert.That(caster.sprite,Is.Not.EqualTo(first),"B caster flame must advance instead of holding the actor's raised-hand frame for four seconds.");
                Assert.That(stream.transform.position.x,Is.GreaterThan(streamPoint.x),"The orange ground wave must keep travelling during the hand effect.");
                var visible=Capture(Camera.main);caster.enabled=false;var hidden=Capture(Camera.main);caster.enabled=true;
                Assert.That(visible.Zip(hidden,(a,b)=>System.Math.Abs(a.r-b.r)+System.Math.Abs(a.g-b.g)+System.Math.Abs(a.b-b.b)).Count(d=>d>8),Is.GreaterThan(5),"The native B caster must change actual GPU pixels.");

                c.Battle.SetPaused(true);double pausedClock=c.Battle.Clock;Sprite pausedFrame=caster.sprite;streamPoint=stream.transform.position;
                c.Tick(10);yield return null;
                Assert.That(c.Battle.Clock,Is.EqualTo(pausedClock));Assert.That(SecondCast().sprite,Is.EqualTo(pausedFrame));Assert.That(stream.transform.position,Is.EqualTo(streamPoint));
                c.Battle.SetPaused(false);c.Tick(.15f);yield return null;
                Assert.That(SecondCast().sprite,Is.Not.EqualTo(pausedFrame));

                c.Tick((float)(began+2.1-c.Battle.Clock));yield return null;
                Assert.That(SecondCasters(),Is.Empty,"The native 1.9667-second caster is one shot, not a frozen or repeated burst.");
                var rain=Object.FindAnyObjectByType<FireRainView>();Assert.That(rain.VisibleStreams,Is.GreaterThan(0));
                stream=rain.GetComponentsInChildren<SpriteRenderer>().First(r=>r.enabled&&r.name=="Horizontal fire stream");streamPoint=stream.transform.position;Sprite fireFrame=stream.sprite;
                c.Tick(.17f);yield return null;
                Assert.That(stream.transform.position.x,Is.GreaterThan(streamPoint.x));Assert.That(stream.sprite,Is.Not.EqualTo(fireFrame));
                c.Tick((float)(began+4-c.Battle.Clock));yield return null;
                Assert.That(SecondCasters(),Is.Empty);Assert.That(rain.VisibleStreams,Is.GreaterThan(0));
                Assert.That(c.Battle.IsSkillActionPlaying(unit),Is.True);Assert.That(unit.Motion.DistanceTravelled,Is.EqualTo(distance),"Ending the short visual must not unlock the six-second game action.");
                c.Tick((float)(began+6.1-c.Battle.Clock));yield return null;
                Assert.That(c.Battle.IsSkillActionPlaying(unit),Is.False);Assert.That(rain.VisibleStreams,Is.Zero);Assert.That(SecondCasters(),Is.Empty);
                c.Tick(1.9f);yield return null;
                Assert.That(c.Battle.CombatShots.Any(s=>!s.FromDefender&&!s.FireRain&&s.SourceId==unit.EntityId&&s.StartedAt>=began+6),Is.True,"Normal B attacks must resume after the unchanged manual action window.");
                c.RestartBattle();yield return null;
                Assert.That(c.Battle.Units,Is.Empty);Assert.That(rain.VisibleStreams,Is.Zero);Assert.That(SecondCasters(),Is.Empty);
            }
            finally{Time.timeScale=previousScale;}
        }

        static SpriteRenderer[] SecondCasters()=>Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.enabled&&r.name=="Talulah native / ignite-B-caster").ToArray();
        static SpriteRenderer SecondCast()
        {
            var casters=SecondCasters();Assert.That(casters.Length,Is.EqualTo(1),"The active B manual cast needs exactly one native caster layer.");return casters[0];
        }
        static Color32[] Capture(Camera camera)
        {
            var target=new RenderTexture(640,360,24);var image=new Texture2D(640,360,TextureFormat.RGBA32,false);
            var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;var previousRect=camera.rect;
            try
            {
                camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();return image.GetPixels32();
            }
            finally{camera.targetTexture=previousTarget;camera.rect=previousRect;RenderTexture.active=previousActive;target.Release();Object.Destroy(target);Object.Destroy(image);}
        }
    }
}
