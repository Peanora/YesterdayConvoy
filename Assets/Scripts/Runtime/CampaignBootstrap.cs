using System;
using System.Collections;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class CampaignBootstrap:MonoBehaviour
    {
        public bool sandbox=true;
        IEnumerator Start()
        {
            yield return null;
            var args=Environment.GetCommandLineArgs();
            if(Array.Exists(args,a=>a=="-talulahSmokeTest"||a=="-talulahMotionEvidence")){UnityEngine.SceneManagement.SceneManager.LoadScene("MonsterTrial");yield break;}
            if(!sandbox&&Array.IndexOf(args,"-synergySandboxSmokeTest")>=0){UnityEngine.SceneManagement.SceneManager.LoadScene("SynergyTrial");yield break;}
            if(Array.Exists(args,a=>a=="-frostSealSmokeTest"||a=="-lateBattleShopSmokeTest"||a=="-battleShopLoopSmokeTest")){UnityEngine.SceneManagement.SceneManager.LoadScene("BattleShop");yield break;}
            var controller=FindAnyObjectByType<ManualBattleController>();
            bool smoke=Array.IndexOf(args,"-campaignSmokeTest")>=0||Array.IndexOf(args,"-synergySandboxSmokeTest")>=0;
            if(smoke)Application.runInBackground=true;
            yield return controller.PrepareCampaign(sandbox,smoke?44:0);
            if(smoke)yield return controller.VerifyCampaign();
        }
    }
}
