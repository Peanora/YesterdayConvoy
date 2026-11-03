using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class OverlappingEntrySceneTests
    {
        [UnityTest]public IEnumerator FiveDifferentCardsCanDragIntoTheSameOccupiedGateWhilePaused()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;float old=Time.timeScale;Time.timeScale=0;
            try
            {
                var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartLateBattleShopLoop(73);yield return null;c.StartBattle();c.Battle.SetPaused(true);
                var entry=c.Battle.Entry;var point=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(entry));int expected=24;
                for(int i=0;i<5;i++)
                {
                    var piece=c.Shop.Bench[i];expected-=c.Battle.FindCard(piece.Id).Settings.DeploymentCost;
                    c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(i).center);c.ProcessShopPointer(EventType.MouseDrag,point);c.ProcessShopPointer(EventType.MouseUp,point);
                    Assert.That(c.Battle.Units.Count,Is.EqualTo(i+1));Assert.That(c.Battle.DP,Is.EqualTo(expected));
                    Assert.That(c.Battle.Units.Last().Motion.Position,Is.EqualTo(entry));
                    Assert.That(c.Battle.FindCard(piece.Id).RemainingUses,Is.EqualTo(3));
                }
                Assert.That(c.Battle.Units.Select(u=>u.EntityId).Distinct().Count(),Is.EqualTo(5));
                c.Tick(10);Assert.That(c.Battle.Clock,Is.Zero);Assert.That(c.Battle.IsPaused,Is.True);
                c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(0).center);c.ProcessShopPointer(EventType.MouseDrag,point);c.ProcessShopPointer(EventType.MouseUp,point);
                Assert.That(c.Battle.Units.Count,Is.EqualTo(5),"Overlapping is allowed; the same card's cooldown still applies.");Assert.That(c.Battle.DP,Is.EqualTo(expected));
                c.LeaveShopTrial();
            }
            finally{Time.timeScale=old;}
        }
    }
}
