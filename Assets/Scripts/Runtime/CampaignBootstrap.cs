using System;
using System.Collections;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class CampaignBootstrap:MonoBehaviour
    {
        public bool sandbox=true;
        public SynergyTrialDefinition trialDefinition;
        IEnumerator Start()
        {
            yield return null;
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-approvedUiSmokeTest")>=0||Array.IndexOf(args,"-sandboxFeaturesSmokeTest")>=0)yield break; // Explicit driver owns this diagnostic startup.
            if(!sandbox&&Array.IndexOf(args,"-synergySandbox")>=0){UnityEngine.SceneManagement.SceneManager.LoadScene("SynergyTrial");yield break;}
            if(!sandbox&&Array.IndexOf(args,"-mudrockCultivation")>=0){UnityEngine.SceneManagement.SceneManager.LoadScene("MudrockCrewTrial");yield break;}
            if(Array.IndexOf(args,"-mudrockSmokeTest")>=0){UnityEngine.SceneManagement.SceneManager.LoadScene(MudrockTrialBootstrap.SceneName);yield break;}
            if(Array.Exists(args,a=>a=="-talulahSmokeTest"||a=="-talulahMotionEvidence"||a=="-talulahFireRainV12Test"||a=="-patriotVfxSmokeTest")){UnityEngine.SceneManagement.SceneManager.LoadScene("MonsterTrial");yield break;}
            if(!sandbox&&(Array.IndexOf(args,"-patriotShopSmokeTest")>=0||Array.IndexOf(args,"-mephistoVfxSmokeTest")>=0||Array.IndexOf(args,"-mandragoraVfxSmokeTest")>=0||Array.IndexOf(args,"-synergySandboxSmokeTest")>=0||Array.IndexOf(args,"-advancedSynergySmokeTest")>=0||Array.IndexOf(args,"-remainingSynergySmokeTest")>=0||Array.IndexOf(args,"-synergyReworkSmokeTest")>=0)){UnityEngine.SceneManagement.SceneManager.LoadScene("SynergyTrial");yield break;}
            if(Array.Exists(args,a=>a=="-frostSealSmokeTest"||a=="-lateBattleShopSmokeTest"||a=="-battleShopLoopSmokeTest")){UnityEngine.SceneManagement.SceneManager.LoadScene("BattleShop");yield break;}
            var controller=FindAnyObjectByType<ManualBattleController>();
            bool smoke=Array.IndexOf(args,"-patriotShopSmokeTest")>=0||Array.IndexOf(args,"-mephistoVfxSmokeTest")>=0||Array.IndexOf(args,"-mandragoraVfxSmokeTest")>=0||Array.IndexOf(args,"-campaignSmokeTest")>=0||Array.IndexOf(args,"-synergySandboxSmokeTest")>=0||Array.IndexOf(args,"-advancedSynergySmokeTest")>=0||Array.IndexOf(args,"-remainingSynergySmokeTest")>=0||Array.IndexOf(args,"-synergyReworkSmokeTest")>=0;
            if(smoke)Application.runInBackground=true;
            yield return controller.PrepareCampaign(sandbox,smoke?44:0,trialDefinition);
            if(Array.IndexOf(args,"-patriotShopSmokeTest")>=0)yield return controller.VerifyPatriotShop();else if(Array.IndexOf(args,"-mephistoVfxSmokeTest")>=0)yield return controller.VerifyMephisto();else if(Array.IndexOf(args,"-mandragoraVfxSmokeTest")>=0)yield return controller.VerifyMandragora();else if(Array.IndexOf(args,"-synergyReworkSmokeTest")>=0)yield return controller.VerifySynergyRework();else if(Array.IndexOf(args,"-remainingSynergySmokeTest")>=0)yield return controller.VerifyRemainingSynergies();else if(Array.IndexOf(args,"-advancedSynergySmokeTest")>=0)yield return controller.VerifyAdvancedSynergies();else if(smoke)yield return controller.VerifyCampaign();
        }
    }
}
