using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class BossRosterSceneTests
    {
        static IEnumerator Host(System.Action<ManualBattleController> done)
        {
            CampaignJourneySession.Clear();yield return SceneManager.LoadSceneAsync("MonsterTrial");
            for(int i=0;i<7200;i++)
            {
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                if(c!=null&&c.Battle!=null&&c.MonsterTests.Count>0&&!c.ArtLoading){done(c);yield break;}yield return null;
            }
            Assert.Fail("Actual MonsterTrial controller did not become ready");
        }
        [UnityTest,Timeout(900000)]public IEnumerator NewNineteenAutomaticallyCastWithReadOnlyInformationAndIndependentPausedState()
        {
            float old=Time.timeScale;Time.timeScale=0;
            try
            {
                ManualBattleController c=null;yield return Host(value=>c=value);BossRosterSmokeResult result=null;
                yield return BossRosterSmokeProbe.Run(c,null,BossRosterSmokeProbe.NewCards,value=>result=value);
                Assert.That(result,Is.Not.Null);Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
            }
            finally{Time.timeScale=old;}
        }
        [UnityTest,Timeout(900000)]public IEnumerator ExistingSevenAutomaticallyTriggerSourceSkillsAndKeepTheirNativePhasesAndModels()
        {
            float old=Time.timeScale;Time.timeScale=0;
            try
            {
                ManualBattleController c=null;yield return Host(value=>c=value);BossRosterSmokeResult result=null;
                yield return BossRosterSmokeProbe.Run(c,null,BossRosterSmokeProbe.CoreCards,value=>result=value);
                Assert.That(result,Is.Not.Null);Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
            }
            finally{Time.timeScale=old;}
        }
    }
}
