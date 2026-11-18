using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationOnBoardMergePresentationTests
    {
        FormationBattleController controller;
        [TearDown] public void Freeze() { if (controller?.Model != null) controller.Model.Paused = true; }

        Vector2 StagePoint(Vector3 world)
        {
            var point = controller.ShopModelCamera.WorldToViewportPoint(world);
            return new Vector2(point.x * 1280, (1 - point.y) * 720);
        }

        [UnityTest]
        public IEnumerator BoardMergeTargetsActualVisibleBodyAndSuppressesHiddenScoutingStage()
        {
            foreach (bool scouting in new[] { false, true })
            {
                // Explicit legal-stock presentation fixture: two one-star copies and the
                // third in the real shop. It does not establish natural acquisition odds.
                var session = FormationShopSession.StartNew(20261117);
                session.Shop.Preset(new[] { new ShopTrialModel.Piece { Id = "CARD_M21", Star = 1 },
                    new ShopTrialModel.Piece { Id = "CARD_M21", Star = 1 } },
                    new[] { "CARD_M21" }, level: 4, xp: 0, gold: 12);
                yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
                double deadline = Time.realtimeSinceStartupAsDouble + 600;
                while (Time.realtimeSinceStartupAsDouble < deadline)
                {
                    controller = Object.FindAnyObjectByType<FormationBattleController>();
                    if (controller != null && controller.Ready && controller.RosterArtReady) break;
                    yield return null;
                }
                Assert.That(controller, Is.Not.Null); Assert.That(controller.Ready && controller.RosterArtReady, Is.True);
                var fighter = controller.Model.Units.Single(); int instance = fighter.Instance;
                Assert.That(session.Seats.InLineup(instance), Is.True);
                Assert.That(session.Shop.Bench.Count(p => !session.Seats.InLineup(p.Instance)), Is.EqualTo(1));
                Assert.That(session.TryDeploy(instance, new Vector2Int(5, 4)).Success, Is.True);
                controller.SetFormationView(scouting); controller.SetMarketVisible(true);
                deadline = Time.realtimeSinceStartupAsDouble + 3;
                while (controller.ShopPanelTransitioning && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                yield return null;
                fighter = controller.Model.Units.Single(); var anchor = fighter.Spawn;
                var foot = FormationBattleController.CellScreen(fighter.SpawnCenter);
                var target = controller.FormationRecruitmentTargetRect(instance);
                if (scouting) Assert.That(target, Is.EqualTo(Rect.zero));
                else
                {
                    Assert.That(target.width > 0 && target.height > 0, Is.True);
                    Assert.That(Vector2.Distance(new Vector2(target.center.x, target.y + 55), foot), Is.LessThan(.01f),
                        "A deployed merge targets its true board foot, without a hidden screen lineup rail.");
                    Assert.That(Vector2.Distance(foot, new Vector2(0, 55)), Is.GreaterThan(100));
                }

                var receipt = controller.BuyOffer(0);
                Assert.That(receipt.Success, Is.True); Assert.That(receipt.FinalStar, Is.EqualTo(2));
                Assert.That(receipt.Instance, Is.EqualTo(instance));
                Assert.That(session.Shop.Bench.Count, Is.EqualTo(1)); Assert.That(session.Shop.Bench.Single().Star, Is.EqualTo(2));
                Assert.That(controller.Model.Units.Single().Star, Is.EqualTo(2));
                Assert.That(controller.Model.Units.Single().Spawn, Is.EqualTo(anchor));
                Assert.That(controller.ShopBenchModel(instance), Is.Not.Null);
                Assert.That(controller.ShopBenchModel(instance).enabled, Is.False, "An on-board fighter has no persistent duplicate in the reserve stage.");
                var arrival = controller.ShopEntranceModel(instance);
                Assert.That(arrival, Is.Not.Null);
                Assert.That(arrival.enabled, Is.EqualTo(!scouting));
                float nearestToFoot = float.PositiveInfinity;
                deadline = Time.realtimeSinceStartupAsDouble + 3;
                while (controller.ShopArrivalCount > 0 && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    arrival = controller.ShopEntranceModel(instance);
                    if (arrival != null)
                    {
                        if (scouting) Assert.That(arrival.enabled, Is.False, "A hidden left-side merge cannot emit a stage sprite at the fallback origin.");
                        else nearestToFoot = Mathf.Min(nearestToFoot, Vector2.Distance(StagePoint(arrival.transform.position), foot));
                    }
                    Assert.That(controller.ShopBenchModel(instance).enabled, Is.False);
                    yield return null;
                }
                if (!scouting) Assert.That(nearestToFoot, Is.LessThan(8), "The actual recruitment flow must approach the real board destination, not (0,55).");
                else Assert.That(controller.FormationRecruitmentTargetRect(instance), Is.EqualTo(Rect.zero));
                session.Shop.Validate();
                yield return Capture(scouting ? "playmode-onboard-merge-hidden-left" : "playmode-onboard-merge-visible-left");
            }
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../artifacts/formation-currency-layout"));
                Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory, name + ".png"), texture.EncodeToPNG());
            }
            finally { Object.Destroy(texture); }
        }
    }
}
