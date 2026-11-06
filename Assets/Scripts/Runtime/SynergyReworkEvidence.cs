using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public IEnumerator VerifySynergyRework()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-demoEvidenceDir");
            string dir=index>=0&&index+1<args.Length?args[index+1]:null;
            if(dir!=null)Directory.CreateDirectory(dir);
            Time.timeScale=0;
            PrepareSandboxRoster(10);
            for(int ready=0;ready<3;){yield return null;ready=ArtLoading?0:ready+1;}
            bool pool=shop.Roles.Count==43&&shop.Roles.Count(r=>r.Cost==5)==8;
            bool preset=Battle.GolemStage==4&&Battle.MudrockTier==5&&Battle.Defenders.Count==6&&Battle.Defenders.All(d=>d.Settings.Skill.Enabled);
            if(dir!=null){ScreenCapture.CaptureScreenshot(Path.Combine(dir,"01-golem-training.png"));yield return null;yield return null;}
            StartLoopNextBattle();Battle.Begin();Battle.SetPaused(true);yield return null;
            var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Entry));
            ProcessShopPointer(EventType.MouseDown,GolemCardRect.center);ProcessShopPointer(EventType.MouseDrag,gate);ProcessShopPointer(EventType.MouseUp,gate);
            var golem=Battle.Units.FirstOrDefault(u=>u.Golem);Tick(1);
            bool deployment=golem!=null&&Battle.DP==86&&Battle.Clock==0&&Battle.UnitControlImmune(golem);
            if(dir!=null){ScreenCapture.CaptureScreenshot(Path.Combine(dir,"02-paused-ultimate.png"));yield return null;yield return null;}
            Battle.SetPaused(false);
            for(int i=0;i<12000&&golem!=null&&golem.Active&&Battle.Defenders.Any(d=>d.Alive);i++)
            {Tick(.05f);if(i%10==0)yield return null;}
            bool clear=golem!=null&&golem.Health>0&&Battle.Defenders.All(d=>!d.Alive)&&Battle.Defenders.Sum(d=>d.SkillCasts)>0;
            if(dir!=null){ScreenCapture.CaptureScreenshot(Path.Combine(dir,"03-six-star-defense-cleared.png"));yield return null;yield return null;}
            ReplaySandbox();PrepareSandboxRoster(8);while(ArtLoading)yield return null;
            bool cultivation=Loop.GolemExperience==6&&Battle.GolemStage==2&&Battle.GolemGrowthEarned==0;
            ReplaySandbox();yield return ChargeStormForEvidence(8001);
            bool snow=Battle.SnowTier==5&&Battle.Shatters==3&&Battle.SnowstormActive&&Battle.Units.All(u=>u.CardId!="CARD_M14");
            snow&=GetComponent<FrostSealView>().VisibleEffects>0;
            if(dir!=null){ScreenCapture.CaptureScreenshot(Path.Combine(dir,"04-five-snow-storm.png"));yield return null;yield return null;}
            Tick(8);snow&=Battle.SnowstormPulses.Count==4;ReplaySandbox();snow&=Battle.SnowstormPulses.Count==0;
            Debug.Log($"SYNERGY_REWORK_PLAYER_CHECK: pool={pool}; preset={preset}; pausedDrag={deployment}; ultimateSoloClear={clear}; cultivation={cultivation}; snowPulsesAndReset={snow}");
            Application.Quit(pool&&preset&&deployment&&clear&&cultivation&&snow?0:1);
        }
    }
}
