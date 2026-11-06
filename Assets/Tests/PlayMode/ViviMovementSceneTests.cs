using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class ViviMovementSceneTests
    {
        static void Set(DeployedUnitState u,string name,object value)=>typeof(DeployedUnitState).GetProperty(name).SetValue(u,value);
        [UnityTest]public IEnumerator BothVisualModesMirrorRetreatAndKeepTheGifWalkingForwardDuringFear()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            var frames=Object.FindAnyObjectByType<ManualBattleController>().Definition.Card;
            foreach(bool upright in new[]{false,true})
            {
                var settings=new ManualBattleSettings(frames.CardId,"VIVI",100,1,1,2,.1f,1000,.75f,1,1,1,1);
                var combat=new EngagementSettings(unitRange:.01f,defenderRange:.01f,blockCount:0);
                var b=new ManualBattle(settings,new[]{Vector3.zero,Vector3.right*10},Vector3.forward*100,combat);b.Begin();
                var receipt=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(receipt.Success,Is.True);var u=b.FindUnit(receipt.EntityId);
                var root=new GameObject("Vivi logical root");var child=new GameObject("Vivi visual",typeof(SpriteRenderer));child.transform.SetParent(root.transform,false);
                var visual=child.AddComponent<DemoSpriteVisual>();visual.WorldUpright=upright;visual.Bind(b,u,frames);
                try
                {
                    b.Advance(.2f);root.transform.position=u.Motion.Position;yield return null;
                    Assert.That(visual.Renderer.flipX,Is.False);int frame=visual.FrameIndex;double distance=u.Motion.DistanceTravelled;
                    Set(u,"FearUntil",b.Clock+10);b.Advance(.05f);root.transform.position=u.Motion.Position;yield return null;
                    Assert.That(u.Motion.DistanceTravelled,Is.LessThan(distance));Assert.That(visual.Renderer.flipX,Is.True,"The visual mirrors in both the single trial and tactical maps.");
                    Assert.That(visual.FrameIndex,Is.GreaterThan(frame),"Retreat accumulates positive step distance instead of rewinding GIF frames.");
                    frame=visual.FrameIndex;b.SetPaused(true);var position=root.transform.position;yield return null;yield return null;
                    Assert.That(visual.FrameIndex,Is.EqualTo(frame));Assert.That(visual.Renderer.flipX,Is.True);
                    Assert.That(root.transform.position,Is.EqualTo(position));Assert.That(root.transform.localScale,Is.EqualTo(Vector3.one));
                    b.SetPaused(false);Set(u,"FearUntil",double.NegativeInfinity);b.Advance(.05f);yield return null;
                    Assert.That(visual.Renderer.flipX,Is.False);
                }
                finally{Object.Destroy(root);}
                yield return null;
            }
        }
    }
}
