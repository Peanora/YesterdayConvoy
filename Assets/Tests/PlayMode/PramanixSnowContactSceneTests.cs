// Real snow-entry damage drives the approved original contact effect, without
// introducing an attack or another application of damage from presentation.
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
    public sealed class PramanixSnowContactSceneTests
    {
        const string Key="defender/D27/sbell2_buff_01_hit_01";
        const int ContactLayer=29;
        static ManualBattle NewBattle()
        {
            // No active S3 and no attacking unit: only naturally forming snow can deal damage.
            // At ten seconds the slow unit lies between two adjacent snow cells, exercising
            // same-source/target/frame merging without creating or moving a cell by hand.
            var skill=new DefenderSkillSettings(DefenderSkillKind.PramanixSnow,0,100,1,20,effectRadius:20);
            var guard=new DefenderSettings("snow-contact-source","snow-contact-source","D27",Vector3.right*2,
                100000,20,0,20,2,windup:.15f,actionDuration:.5f,skill:skill,canAttack:false);
            return new ManualBattle(new ManualBattleSettings("snow-contact-card","snow-contact-unit",30,1,20,1,.1f,100000,.05f,1,1,1,1),
                new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                new EngagementSettings(unitAttack:0,unitRange:.01f,unitInterval:100,canAttack:false,hasSkill:false,
                    initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0),roster:new[]{guard});
        }
        static SpriteRenderer[] Contacts(GameObject root)=>root.GetComponentsInChildren<SpriteRenderer>()
            .Where(r=>r.enabled&&r.sprite!=null&&r.name=="Original VFX / "+Key).ToArray();
        static Color32[] Capture(GameObject root,Camera camera,RenderTexture target,Texture2D read)
        {
            foreach(var r in root.GetComponentsInChildren<SpriteRenderer>())
                r.gameObject.layer=r.enabled&&r.name=="Original VFX / "+Key?ContactLayer:0;
            camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,target.width,target.height),0,0);read.Apply();
            return read.GetPixels32();
        }
        static void EqualCombat(ManualBattle shown,ManualBattle control)
        {
            Assert.That(shown.Clock,Is.EqualTo(control.Clock));
            Assert.That(shown.Units.Single().Health,Is.EqualTo(control.Units.Single().Health),"Rendering must not cause another snow damage application.");
            Assert.That(shown.CombatShots,Is.Empty,"Snow contacts must not fabricate attack/caster CombatShots.");
            Assert.That(control.CombatShots,Is.Empty);
            Assert.That(shown.PramanixSnowImpacts.Count,Is.EqualTo(control.PramanixSnowImpacts.Count));
        }

        [UnityTest]public IEnumerator NaturalSnowContactRendersOriginal01FromTheMergedRealImpactAndClearsOnRestart()
        {
            Assume.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));
            float priorTime=Time.timeScale;var priorActive=RenderTexture.active;Camera priorMain=null;
            GameObject root=null,cameraObject=null;RenderTexture target=null;Texture2D read=null;
            try
            {
                yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
                yield return OriginalCombatVfxArt.Get(Key).Sequence.Warm();
                priorMain=Camera.main;Assert.That(priorMain,Is.Not.Null);priorMain.tag="Untagged";
                cameraObject=new GameObject("Isolated actual Pramanix snow-contact camera");
                var camera=cameraObject.AddComponent<Camera>();camera.tag="MainCamera";camera.enabled=true;
                camera.orthographic=true;camera.orthographicSize=2.5f;camera.aspect=1;camera.cullingMask=1<<ContactLayer;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                camera.transform.position=new Vector3(0,8,-5);camera.transform.LookAt(Vector3.forward*.5f);
                target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
                read=new Texture2D(256,256,TextureFormat.RGBA32,false,true);camera.targetTexture=target;
                var shown=NewBattle();var control=NewBattle();
                root=new GameObject("Actual Pramanix passive snow contact");
                root.AddComponent<DefenderSkillVfxView>().Bind(()=>shown,()=>shown.Clock);
                foreach(var battle in new[]{shown,control})
                {battle.Begin();Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).Success,Is.True);}
                for(int tick=0;tick<240&&shown.PramanixSnowImpacts.Count==0;tick++)
                {
                    shown.Advance(.05f);control.Advance(.05f);yield return null;EqualCombat(shown,control);
                    if(shown.Clock<9.99)
                    {Assert.That(shown.PramanixSnowImpacts,Is.Empty);Assert.That(Contacts(root),Is.Empty);}
                }
                Assert.That(shown.SnowCells.Any(c=>c.Layers>0),Is.True,"Snow must form through the ordinary ten-second timer.");
                Assert.That(shown.Defender.SkillCasts,Is.Zero,"This is a snow-entry contact, not the S3 attack-hit effect.");
                Assert.That(shown.PramanixSnowImpacts.Count,Is.EqualTo(1),"Two overlapping same-frame cells must produce one visual contact.");
                var impact=shown.PramanixSnowImpacts.Single();var unit=shown.Units.Single();
                Assert.That(impact.RunId,Is.EqualTo(shown.RunId));Assert.That(impact.SourceId,Is.EqualTo(shown.Defender.EntityId));
                Assert.That(impact.TargetId,Is.EqualTo(unit.EntityId));Assert.That(impact.At,Is.InRange(9.99,10.1));
                var losses=shown.CombatFeedback.Where(e=>e.Kind==CombatFeedbackKind.HealthLost&&e.TargetId==unit.EntityId&&Math.Abs(e.At-impact.At)<.000001).ToArray();
                Assert.That(losses.Length,Is.GreaterThanOrEqualTo(2),"Both real cell entries still apply their existing damage; only the visual event merges.");
                Assert.That(Vector3.Distance(impact.Position,losses[0].WorldPosition),Is.LessThan(.00001f),
                    "The effect keeps the actual damage-contact point, even if the unit has moved by the end of Advance.");
                Assert.That(unit.MaxHealth-unit.Health,Is.EqualTo(losses.Sum(e=>e.Amount)).Within(.001f));
                // Let the original contact's initial frames develop; no fixed sprite/age is injected.
                Color32[] visible=null;
                for(int frame=0;frame<15;frame++)
                {
                    var pixels=Capture(root,camera,target,read);
                    if(pixels.Count(p=>p.r+p.g+p.b>12)>20){visible=pixels;break;}
                    shown.Advance(1f/30);control.Advance(1f/30);yield return null;EqualCombat(shown,control);
                }
                Assert.That(visible,Is.Not.Null,"Only the original contact-01 layer must reach more than twenty actual GPU pixels.");
                Assert.That(Contacts(root).Length,Is.EqualTo(1));
                Assert.That(Contacts(root)[0].sprite,Is.SameAs(OriginalCombatVfxArt.Get(Key).At(shown.Clock-impact.At)),"Contact frames use their real impact age.");
                long sequence=impact.Sequence;double clock=shown.Clock;float hp=unit.Health;
                shown.SetPaused(true);control.SetPaused(true);
                for(int frame=0;frame<3;frame++){shown.Advance(1);control.Advance(1);yield return null;}
                EqualCombat(shown,control);Assert.That(shown.Clock,Is.EqualTo(clock));Assert.That(unit.Health,Is.EqualTo(hp));
                Assert.That(shown.PramanixSnowImpacts.Single().Sequence,Is.EqualTo(sequence));
                CollectionAssert.AreEqual(visible,Capture(root,camera,target,read),"Pause freezes the original contact's actual GPU frame.");
                shown.Restart();control.Restart();yield return null;
                Assert.That(shown.PramanixSnowImpacts,Is.Empty);Assert.That(Contacts(root),Is.Empty);
                Assert.That(shown.CombatShots,Is.Empty);Assert.That(shown.Clock,Is.Zero);
            }
            finally
            {
                if(priorMain!=null)priorMain.tag="MainCamera";
                if(cameraObject!=null){cameraObject.GetComponent<Camera>().targetTexture=null;Object.Destroy(cameraObject);}
                RenderTexture.active=priorActive;
                if(target!=null){target.Release();Object.Destroy(target);}if(read!=null)Object.Destroy(read);
                if(root!=null)Object.Destroy(root);Time.timeScale=priorTime;
            }
            yield return null;
        }
    }
}
