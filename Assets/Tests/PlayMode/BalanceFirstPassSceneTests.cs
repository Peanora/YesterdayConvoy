using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class BalanceFirstPassSceneTests
    {
        ManualBattleController controller;float previousScale;
        [SetUp]public void Setup()=>previousScale=Time.timeScale;
        [TearDown]public void Cleanup()
        {
            if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();
            Time.timeScale=previousScale;
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("ReunionTrial");
            for(int i=0;i<1800;i++)
            {
                controller=UnityEngine.Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox&&controller.Loop.CanShop&&!controller.ArtLoading&&!controller.ShopPanelTransitioning)
                {yield return null;Time.timeScale=0;yield break;}
                yield return null;
            }
            Assert.Fail("The saved balance trial did not finish preparing.");
        }
        IEnumerator Run(string name,params object[] args)=>(IEnumerator)typeof(ManualBattleController)
            .GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,args);
        static void Check(string name,bool valid)=>Assert.That(valid,Is.True,name);

        [UnityTest]public IEnumerator SelectedTotalStarsAndFourStarPointerDeploymentSurvivePauseAndRestart()
        {
            yield return Load();
            yield return Run("BalanceFirstPassTrialChecks",(Action<string,bool>)Check,null);
        }
        [UnityTest]public IEnumerator FormalD07StagesRenderAndExchangeRealRangedAttacksWithoutFreeTransfers()
        {
            yield return Load();
            foreach(int stage in new[]{7,8,11})yield return Run("BalanceFirstPassStageChecks",stage,(Action<string,bool>)Check,null);
        }
    }
}
