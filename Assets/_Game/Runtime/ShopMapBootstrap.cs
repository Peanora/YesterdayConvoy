using System;
using System.Collections;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class ShopMapBootstrap : MonoBehaviour
    {
        ManualBattleController controller;
        IEnumerator Start()
        {
            yield return null;
            controller=FindAnyObjectByType<ManualBattleController>();controller.EnterShopTrial();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-shopMapSmokeTest")>=0)
                yield return controller.VerifyShopMap();
        }
        void Update(){if(controller!=null&&Input.GetKeyDown(KeyCode.Escape))controller.CancelShopDrag();}
        void OnApplicationFocus(bool focused){if(!focused&&controller!=null)controller.CancelShopDrag();}
    }
}
