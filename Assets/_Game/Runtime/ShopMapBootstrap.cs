using System;
using System.Collections;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class ShopMapBootstrap : MonoBehaviour
    {
        ManualBattleController controller;
        void Awake()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-strongholdChallengeSmokeTest")>=0||Array.IndexOf(args,"-strongholdMapSmokeTest")>=0||Array.IndexOf(args,"-shopMapSmokeTest")>=0)Application.runInBackground=true;
        }
        IEnumerator Start()
        {
            yield return null;
            controller=FindAnyObjectByType<ManualBattleController>();
            bool legacy=Array.IndexOf(Environment.GetCommandLineArgs(),"-shopMapSmokeTest")>=0;
            bool normal=Array.IndexOf(Environment.GetCommandLineArgs(),"-strongholdMapSmokeTest")>=0;
            controller.EnterShopTrial(0,legacy?"city":normal?StrongholdMapLayout.Id:StrongholdChallengeLayout.Id);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-strongholdChallengeSmokeTest")>=0)yield return controller.VerifyStrongholdChallenge();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-strongholdMapSmokeTest")>=0)yield return controller.VerifyStrongholdMap();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-shopMapSmokeTest")>=0)
                yield return controller.VerifyShopMap();
        }
        void Update()
        {
            if(controller==null||!controller.ShopTrialActive)return;
            if(Input.GetKeyDown(KeyCode.Tab))controller.SetShopVisible(!controller.ShopVisible);
            if(Input.GetKeyDown(KeyCode.Escape)){controller.CancelShopDrag();controller.SetShopVisible(false);controller.CloseDefenderInspector();}
        }
        void OnApplicationFocus(bool focused){if(!focused&&controller!=null)controller.CancelShopDrag();}
    }
}
