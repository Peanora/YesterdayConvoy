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
            // Apply once on normal Player startup; never force the size while the user resizes.
            // Explicit command-line resolution/fullscreen choices remain available for validation.
            if(!Application.isEditor&&Array.IndexOf(args,"-screen-width")<0&&Array.IndexOf(args,"-screen-height")<0&&Array.IndexOf(args,"-screen-fullscreen")<0)
            {
                int width=Mathf.Min(1280,Mathf.Max(640,Screen.currentResolution.width-100));
                int height=Mathf.Min(720,Mathf.Max(360,Screen.currentResolution.height-100));
                Screen.SetResolution(width,height,FullScreenMode.Windowed);
            }
            if(Array.IndexOf(args,"-lateBattleShopSmokeTest")>=0||Array.IndexOf(args,"-battleShopLoopSmokeTest")>=0||Array.IndexOf(args,"-strongholdChallengeSmokeTest")>=0||Array.IndexOf(args,"-strongholdMapSmokeTest")>=0||Array.IndexOf(args,"-shopMapSmokeTest")>=0)Application.runInBackground=true;
        }
        IEnumerator Start()
        {
            yield return null;
            controller=FindAnyObjectByType<ManualBattleController>();
            bool legacy=Array.IndexOf(Environment.GetCommandLineArgs(),"-shopMapSmokeTest")>=0;
            bool normal=Array.IndexOf(Environment.GetCommandLineArgs(),"-strongholdMapSmokeTest")>=0;
            bool challenge=Array.IndexOf(Environment.GetCommandLineArgs(),"-strongholdChallengeSmokeTest")>=0;
            if(!legacy&&!normal&&!challenge)
            {
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-battleShopLoopSmokeTest")>=0)controller.StartBattleShopLoop();
                else controller.StartLateBattleShopLoop();
            }
            else controller.EnterShopTrial(0,legacy?"city":normal?StrongholdMapLayout.Id:StrongholdChallengeLayout.Id);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-battleShopLoopSmokeTest")>=0)yield return controller.VerifyBattleShopLoop();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-lateBattleShopSmokeTest")>=0)yield return controller.VerifyLateBattleShop();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-strongholdChallengeSmokeTest")>=0)yield return controller.VerifyStrongholdChallenge();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-strongholdMapSmokeTest")>=0)yield return controller.VerifyStrongholdMap();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-shopMapSmokeTest")>=0)
                yield return controller.VerifyShopMap();
        }
        void Update()
        {
            if(controller==null||!controller.ShopTrialActive)return;
            if(!controller.BattleShopLoopActive&&Input.GetKeyDown(KeyCode.Tab))controller.SetShopVisible(!controller.ShopVisible);
            if(Input.GetKeyDown(KeyCode.Escape)){controller.CancelShopDrag();if(!controller.BattleShopLoopActive)controller.SetShopVisible(false);controller.CloseDefenderInspector();}
        }
        void OnApplicationFocus(bool focused){if(!focused&&controller!=null)controller.CancelShopDrag();}
    }
}
