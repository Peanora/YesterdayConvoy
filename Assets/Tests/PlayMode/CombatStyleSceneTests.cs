using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy.Tests
{
    public sealed class CombatStyleSceneTests
    {
        float oldTime;
        [SetUp]public void Setup()=>oldTime=Time.timeScale;
        [TearDown]public void Cleanup()=>Time.timeScale=oldTime;
        [UnityTest]public IEnumerator SixSavedScenesSupportRealPointerDeploymentNaturalProcsPauseAndRestart()
        {
            var report=new CombatStyleSmokeDriver.Report();string dir=Path.GetFullPath("Logs/combat-styles-play-evidence");
            yield return CombatStyleSmokeDriver.Exercise(report,dir);Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"result.json"),JsonUtility.ToJson(report,true));
            Assert.That(report.checks.Where(c=>!c.passed).Select(c=>c.name),Is.Empty);
        }
        [UnityTest]public IEnumerator FormalJourneyShowsStyleTagsAndBothTrialMenuTabsRemainAvailable()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");ManualBattleController c=null;
            yield return CampaignJourneyEvidenceProbe.InitialHost(found=>c=found);c.BeginJourney(44);yield return CampaignJourneyEvidenceProbe.Ready(1,found=>c=found);
            Assert.That(c.Battle.CombatStylesEnabled,Is.True);Assert.That(c.Battle.CampaignV2Enabled,Is.True);
            var current=(SynergySummary[])typeof(ManualBattleController).GetProperty("CurrentSynergies",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);
            Assert.That(current.Any(s=>CombatStyleRules.Find(s.Name)!=null),Is.True);
            c.ExitJourney();yield return CampaignJourneyEvidenceProbe.Host(found=>c=found);
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(ManualBattleController).GetField("dedicatedSynergyMenuOpen",flags).SetValue(c,true);
            foreach(bool styleTab in new[]{false,true})
            {
                typeof(ManualBattleController).GetField("dedicatedStyleTab",flags).SetValue(c,styleTab);
                yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
                var screen=ScreenCapture.CaptureScreenshotAsTexture();Assert.That(screen,Is.Not.Null);
                var pixels=screen.GetPixels32();Assert.That(pixels.Count(p=>p.r>80||p.g>80||p.b>80),Is.GreaterThan(pixels.Length/20));
                string path=Path.GetFullPath("Logs/combat-styles-play-evidence");Directory.CreateDirectory(path);File.WriteAllBytes(Path.Combine(path,styleTab?"menu-six-styles.png":"menu-nine-factions.png"),screen.EncodeToPNG());Object.Destroy(screen);
            }
            typeof(ManualBattleController).GetField("dedicatedSynergyMenuOpen",flags).SetValue(c,false);
        }
    }
}
