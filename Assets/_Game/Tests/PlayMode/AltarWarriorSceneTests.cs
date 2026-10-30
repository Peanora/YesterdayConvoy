using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace YesterdayConvoy.Tests
{
    public sealed class AltarWarriorSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator RealAltarSwitchesThreeLivingPosesAndEmpoweredDeathUsesOriginalClip()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Tests/Fixtures/01-IndustrialTransfer.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<FacilityTrialController>();Assert.That(c.Deploy(),Is.True);var u=c.Battle.Units[0];var poses=new HashSet<string>();
                for(int i=0;i<1200&&u.Active;i++)
                {
                    c.Battle.Advance(.05f);yield return null;var v=Object.FindObjectsByType<DemoSpriteVisual>().Single();
                    if(!u.AltarEmpowered)Assert.That(v.Renderer.sprite.name,Does.Not.Contain("extra_"));
                    else {poses.Add(v.FrameState);Assert.That(v.Renderer.sprite.name,Does.StartWith("M19/extra_"));}
                    if(poses.Contains("AltarWalk")&&poses.Contains("AltarIdle")&&poses.Contains("AltarAttack"))break;
                }
                Assert.That(poses,Does.Contain("AltarWalk").And.Contain("AltarIdle").And.Contain("AltarAttack"));
                var card=c.roles[0].Card;
                var settings=new ManualBattleSettings(card.CardId,card.Unit.UnitId,10,1,1,1,1,10,.001f,1,1,1,1);
                var combat=new EngagementSettings(unitAttack:1,unitRange:.01f,unitInterval:2,defenderHealth:1000,defenderAttack:100,defenderRange:5,
                    initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,projectileTime:.5f);
                var b=new ManualBattle(settings,new[]{Vector3.zero,Vector3.forward*100},Vector3.forward,combat);
                b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Altar,Vector3.zero,interval:.2f,power:100)});b.Begin();var receipt=b.TryDeploy(b.RunId,1,b.Entry);var dying=b.FindUnit(receipt.EntityId);
                var root=new GameObject("Altar death fixture");var child=new GameObject("Original M19",typeof(SpriteRenderer),typeof(DemoSpriteVisual));child.transform.SetParent(root.transform,false);
                double clock=0;var death=child.GetComponent<DemoSpriteVisual>();death.Bind(b,dying,card,()=>clock);
                b.Advance(.25f);clock=b.Clock;yield return null;Assert.That(dying.AltarEmpowered,Is.True);Assert.That(dying.Health,Is.EqualTo(10),"Altar damage must be immune, not merely recoloured.");
                b.Advance(.5f);clock=b.Clock;yield return null;Assert.That(dying.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(death.FrameState,Is.EqualTo("AltarDeath"));Assert.That(death.Renderer.sprite.name,Does.StartWith("M19/extra_die_2/"));
                int frame=death.FrameIndex;clock+=.2;yield return null;Assert.That(death.FrameIndex,Is.GreaterThan(frame));Object.Destroy(root);
                c.ResetTrial(0);yield return null;Assert.That(c.Battle.Units,Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
