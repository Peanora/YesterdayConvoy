using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class CampaignBootstrap:MonoBehaviour
    {
        public bool sandbox=true;
        public SynergyTrialDefinition trialDefinition;
        static bool approvedLaunchConsumed;
        IEnumerator Start()
        {
            yield return null;
            var args=Environment.GetCommandLineArgs();
            if(!sandbox&&Array.Exists(args,a=>a=="-bossArena"||a=="-bossArenaSmokeTest")){UnityEngine.SceneManagement.SceneManager.LoadScene(BossArenaRules.SceneName);yield break;}
            // These dedicated drivers own scene startup; do not race them with
            // the default journey's asynchronous load.
            if(!sandbox&&Array.Exists(args,a=>a=="-campaignJourneySmokeTest"||a=="-viviCombatSmokeTest"||a=="-bossRosterSmokeTest"))yield break;
            if(ApprovedMapSmokeDriver.Requested)yield break;
            if(!approvedLaunchConsumed&&Array.IndexOf(args,"-approvedMaps")>=0){approvedLaunchConsumed=true;UnityEngine.SceneManagement.SceneManager.LoadScene("MonsterTrial");yield break;}
            int approved=Array.IndexOf(args,"-approvedMap");
            if(!approvedLaunchConsumed&&approved>=0&&approved+1<args.Length&&int.TryParse(args[approved+1],out int mapIndex))
            {
                var map=ApprovedMapCatalog.All.FirstOrDefault(m=>m.index==mapIndex);
                if(map==null)throw new InvalidOperationException("Approved map index must be 1..8");
                approvedLaunchConsumed=true;UnityEngine.SceneManagement.SceneManager.LoadScene(map.sceneName);yield break;
            }
            if(!sandbox&&(ReunionCoalitionSmokeDriver.Requested||BalanceFirstPassSmokeDriver.Requested))yield break;
            if(Array.Exists(args,a=>a=="-blackForest"||a=="-blackForestSmokeTest"||a=="-waveBattleSmokeTest")){UnityEngine.SceneManagement.SceneManager.LoadScene(BlackForestLayout.SceneName);yield break;}
            if(!sandbox&&Array.IndexOf(args,"-marineTideSmokeTest")>=0)yield break;
            if(!sandbox&&Array.IndexOf(args,"-marineTide")>=0){UnityEngine.SceneManagement.SceneManager.LoadScene("MarineTrial");yield break;}
            if(Array.IndexOf(args,"-approvedUiSmokeTest")>=0||Array.IndexOf(args,"-damageRankingSmokeTest")>=0||Array.IndexOf(args,"-sandboxFeaturesSmokeTest")>=0||SynergyBadgeSmokeDriver.Requested||!sandbox&&Array.IndexOf(args,"-herdSpiritSmokeTest")>=0)yield break; // Explicit drivers own startup; saved sandbox still prepares the herd trial.
            if(!sandbox&&Array.IndexOf(args,"-synergySandbox")>=0){UnityEngine.SceneManagement.SceneManager.LoadScene("SynergyTrial");yield break;}
            if(!sandbox&&Array.IndexOf(args,"-mudrockCultivation")>=0){UnityEngine.SceneManagement.SceneManager.LoadScene("MudrockCrewTrial");yield break;}
            if(Array.IndexOf(args,"-mudrockSmokeTest")>=0){UnityEngine.SceneManagement.SceneManager.LoadScene(MudrockTrialBootstrap.SceneName);yield break;}
            if(Array.Exists(args,a=>a=="-talulahSmokeTest"||a=="-talulahMotionEvidence"||a=="-talulahFireRainV12Test"||a=="-patriotVfxSmokeTest")){UnityEngine.SceneManagement.SceneManager.LoadScene("MonsterTrial");yield break;}
            if(!sandbox&&(Array.IndexOf(args,"-patriotShopSmokeTest")>=0||Array.IndexOf(args,"-mephistoVfxSmokeTest")>=0||Array.IndexOf(args,"-mandragoraVfxSmokeTest")>=0||Array.IndexOf(args,"-synergySandboxSmokeTest")>=0||Array.IndexOf(args,"-advancedSynergySmokeTest")>=0||Array.IndexOf(args,"-remainingSynergySmokeTest")>=0||Array.IndexOf(args,"-synergyReworkSmokeTest")>=0)){UnityEngine.SceneManagement.SceneManager.LoadScene("SynergyTrial");yield break;}
            if(Array.Exists(args,a=>a=="-frostSealSmokeTest"||a=="-lateBattleShopSmokeTest"||a=="-battleShopLoopSmokeTest")){UnityEngine.SceneManagement.SceneManager.LoadScene("BattleShop");yield break;}
            var controller=FindAnyObjectByType<ManualBattleController>();
            bool smoke=Array.IndexOf(args,"-patriotShopSmokeTest")>=0||Array.IndexOf(args,"-mephistoVfxSmokeTest")>=0||Array.IndexOf(args,"-mandragoraVfxSmokeTest")>=0||Array.IndexOf(args,"-campaignSmokeTest")>=0||Array.IndexOf(args,"-synergySandboxSmokeTest")>=0||Array.IndexOf(args,"-advancedSynergySmokeTest")>=0||Array.IndexOf(args,"-remainingSynergySmokeTest")>=0||Array.IndexOf(args,"-synergyReworkSmokeTest")>=0;
            if(smoke)Application.runInBackground=true;
            if(!sandbox&&!smoke){controller.BeginJourney();yield break;}
            yield return controller.PrepareCampaign(sandbox,smoke?44:0,trialDefinition);
            if(Array.IndexOf(args,"-patriotShopSmokeTest")>=0)yield return controller.VerifyPatriotShop();else if(Array.IndexOf(args,"-mephistoVfxSmokeTest")>=0)yield return controller.VerifyMephisto();else if(Array.IndexOf(args,"-mandragoraVfxSmokeTest")>=0)yield return controller.VerifyMandragora();else if(Array.IndexOf(args,"-synergyReworkSmokeTest")>=0)yield return controller.VerifySynergyRework();else if(Array.IndexOf(args,"-remainingSynergySmokeTest")>=0)yield return controller.VerifyRemainingSynergies();else if(Array.IndexOf(args,"-advancedSynergySmokeTest")>=0)yield return controller.VerifyAdvancedSynergies();else if(smoke)yield return controller.VerifyCampaign();
        }
    }
}
