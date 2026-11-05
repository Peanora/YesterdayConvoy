using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class PatriotPresentationTests
    {
        [UnityTest]public IEnumerator AuthoredEntryAndAllNativePhasesRenderOnTheActualGpuAndPauseResetCorrectly()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            var method=typeof(ManualBattleController).GetMethod("VerifyPatriotPresentation",BindingFlags.Instance|BindingFlags.NonPublic);
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../artifacts/patriot-playmode"));
            yield return (IEnumerator)method.Invoke(c,new object[]{path,false});
            Assert.That(File.ReadAllText(Path.Combine(path,"patriot-runtime-check.json")),Does.Contain("\"passed\":true"));
        }
        [UnityTest]public IEnumerator RealSpearFacingMatchesItsActorInBothDirectionsAndFatalWindupCancelsAllCastEffects()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;float old=Time.timeScale;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.TryEnterMonsterTest("CARD_M20"),Is.True);
            var root=new GameObject("Patriot directional fixture");var view=root.AddComponent<PatriotEffectView>();
            var material=new Material(Shader.Find("Sprites/Default"));
            try
            {
                foreach(int side in new[]{-1,1})
                {
                    Vector3 entry=new Vector3(side*2,.35f,0);
                    var s=new ManualBattleSettings("CARD_M20","M20_PATRIOT",30,10,12,1,.1f,1000,.001f,1,1,1,1);
                    var combat=new EngagementSettings(defenderHealth:10000,defenderRange:.001f,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,
                        attackDuration:1.5f,attackWindup:.75f,effects:c.Definition.Engagement.Snapshot().Effects);
                    var b=new ManualBattle(s,new[]{entry,entry+Vector3.forward*100},Vector3.zero,combat);
                    b.ConfigureDefenderHighGround(b.Defenders[0].EntityId,true);b.Begin();var deployed=b.TryDeploy(b.RunId,1,b.Entry);var u=b.FindUnit(deployed.EntityId);
                    b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);b.Advance(75.1f);Assert.That(u.Spear,Is.Not.Null);
                    b.Advance((float)(u.Spear.StartedAt+1.8-b.Clock));view.Bind(()=>b);
                    var actorRoot=new GameObject("Patriot directional actor");actorRoot.transform.position=u.Motion.Position;
                    var body=new GameObject("Original Patriot motion",typeof(SpriteRenderer),typeof(DemoSpriteVisual));body.transform.SetParent(actorRoot.transform,false);
                    var sprite=body.GetComponent<SpriteRenderer>();sprite.sharedMaterial=material;
                    var visual=body.GetComponent<DemoSpriteVisual>();visual.WorldUpright=true;visual.Bind(b,u,c.Definition.Card);yield return null;
                    bool left=Vector3.Dot(u.Spear.Shot.Destination-u.Spear.Shot.Origin,Camera.main.transform.right)<0;
                    Assert.That(visual.FrameState,Is.EqualTo("PatriotSpear"));Assert.That(sprite.flipX,Is.EqualTo(left));
                    var projectile=root.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.enabled&&r.name=="Patriot effect / skill_01_start_02");
                    Assert.That(projectile.transform.localScale.x<0,Is.EqualTo(left));
                    var pending=u.Spear.Shot;Assert.That(pending.Applied,Is.False);Assert.That(b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts),Is.True);yield return null;
                    Assert.That(pending.Resolved&&!pending.Applied,Is.True);
                    Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.name.StartsWith("Patriot effect / skill_")),Is.False);
                    b.Restart();yield return null;Assert.That(view.VisibleEffects,Is.Zero);Object.Destroy(actorRoot);
                }
            }
            finally{Time.timeScale=old;Object.Destroy(root);Object.Destroy(material);}
        }
    }
}
