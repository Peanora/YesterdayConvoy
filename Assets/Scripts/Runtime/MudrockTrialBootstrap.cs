using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class MudrockTrialBootstrap:MonoBehaviour
    {
        public const string SceneName="MudrockTrial";
        public GramophoneAnchor[] devices;
        public void Configure(ManualBattle battle)=>battle.ConfigureGramophones(devices.Select(d=>d.Snapshot()));
        IEnumerator Start()
        {
            yield return null;
            var controller=FindAnyObjectByType<ManualBattleController>();controller.OpenMudrockTrial();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-mudrockSmokeTest")>=0)yield return controller.VerifyMudrock();
        }
    }
    public sealed partial class ManualBattleController
    {
        internal void OpenMudrockTrial(){TestSelectionOpen=false;Feedback="拖动泥岩至红门 · 35技力后点击泥岩，占据最近的非本方留声机";}
        public void EnterMudrockTrial()=>UnityEngine.SceneManagement.SceneManager.LoadScene(MudrockTrialBootstrap.SceneName);
    }
}
