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
    }
}
