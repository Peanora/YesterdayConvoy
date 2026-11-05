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
            bool pool=shop.Roles.Count==42&&shop.Roles.Count(r=>r.Cost==5)==7;
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
            bool investment=InvestCampaignGolem(10)&&Loop.GolemInvestment==30&&!InvestCampaignGolem(1)&&Battle.GolemStage==2;
            PrepareSandboxRoster(0);while(ArtLoading)yield return null;StartLoopNextBattle();Battle.Begin();
            var receipt=Battle.TryDeployRole(Battle.RunId,8001,Battle.Entry,"CARD_M14");var frost=Battle.FindUnit(receipt.EntityId);
            for(int i=0;i<250&&frost!=null&&!Battle.IsSkillReady(frost.EntityId);i++){Tick(.1f);if(i%10==0)yield return null;}
            bool snow=frost!=null&&Battle.SnowTier==5&&Battle.TryActivateSkill(Battle.RunId,8002,frost.EntityId)==SkillFailure.None;
            Tick(.7f);yield return null;yield return null;
            snow&=GetComponent<FrostSealView>().VisibleEffects>0;
            if(dir!=null){ScreenCapture.CaptureScreenshot(Path.Combine(dir,"04-five-snow-storm.png"));yield return null;yield return null;}
            Tick(8);snow&=Battle.SnowstormPulses.Count==4;ReplaySandbox();snow&=Battle.SnowstormPulses.Count==0;
            Debug.Log($"SYNERGY_REWORK_PLAYER_CHECK: pool={pool}; preset={preset}; pausedDrag={deployment}; ultimateSoloClear={clear}; investment={investment}; snowPulsesAndReset={snow}");
            Application.Quit(pool&&preset&&deployment&&clear&&investment&&snow?0:1);
        }
    }
}
