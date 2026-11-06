using System;
using System.Collections;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class BlackForestBootstrap:MonoBehaviour
    {
        public DefenderTrialDefinition encounter;
        ManualBattleController controller;
        IEnumerator Start()
        {
            yield return null;
            controller=FindAnyObjectByType<ManualBattleController>();
            yield return controller.PrepareBlackForest(encounter);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-blackForestSmokeTest")>=0){Application.runInBackground=true;yield return controller.VerifyBlackForest();}
        }
        void Update()
        {
            if(controller==null||controller.ArtLoading||!controller.ShopTrialActive)return;
            if(Input.GetKeyDown(KeyCode.Tab))controller.SetShopVisible(!controller.ShopVisible);
            if(Input.GetKeyDown(KeyCode.Escape)){controller.CancelShopDrag();controller.SetShopVisible(false);controller.CloseDefenderInspector();}
        }
    }
}
