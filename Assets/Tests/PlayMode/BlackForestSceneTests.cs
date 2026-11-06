using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class BlackForestSceneTests
    {
        [UnityTest]public IEnumerator SavedTerrainPersistsAcrossShopTransactionsRestartAndSceneExit()
        {
            yield return SceneManager.LoadSceneAsync(BlackForestLayout.SceneName);yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();int frames=0;while((!c.ShopTrialActive||c.ArtLoading)&&frames++<1800)yield return null;
            Assert.That(c.TacticalMapId,Is.EqualTo(BlackForestLayout.Id));Assert.That(c.Battle.Defenders,Is.Empty);
            var tile=Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None).First(t=>t.coordinate==new Vector2Int(8,8));Vector3 moved=tile.transform.position+Vector3.up*.1f;tile.transform.position=moved;
            var count=Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None).Length;c.SetShopVisible(true);yield return new WaitForSecondsRealtime(.3f);
            c.PurchaseShopCard(1);Assert.That(tile.transform.position,Is.EqualTo(moved));c.SetShopVisible(false);yield return new WaitForSecondsRealtime(.3f);
            Assert.That(c.Battle.Lanes.Count,Is.EqualTo(3));Assert.That(c.Battle.Lanes.Select(l=>l.ExitId).Distinct().Count(),Is.EqualTo(2));
            var red=c.Battle.Lanes[2];var target=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(red.Entry));
            Assert.That(c.ShopMapPointAvailable(target),Is.True,"All red gates must remain interactive outside UI.");
            var rect=c.ShopPieceRect(0);
            var source=rect.center;c.ProcessShopPointer(EventType.MouseDown,source);c.ProcessShopPointer(EventType.MouseDrag,target);c.ProcessShopPointer(EventType.MouseUp,target);
            Assert.That(c.Battle.Units.Count,Is.EqualTo(1));Assert.That(c.Battle.Units[0].RouteId,Is.EqualTo("R3"));
            c.Battle.SetPaused(true);var p=c.Battle.Units[0].Motion.Position;c.Tick(5);Assert.That(c.Battle.Units[0].Motion.Position,Is.EqualTo(p));
            c.EnterShopTrial(0,BlackForestLayout.Id);yield return null;Assert.That(tile.transform.position,Is.EqualTo(moved));Assert.That(Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None).Length,Is.EqualTo(count));
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Assert.That(Object.FindAnyObjectByType<BlackForestTile>(),Is.Null);
        }
    }
}
