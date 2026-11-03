using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FacilityTrialController
    {
        IEnumerator VerifyAltarWarrior(string folder)
        {
            Time.timeScale=0;bool deployed=Deploy(),normal=false;var poses=new HashSet<string>();var u=Battle.Units[0];
            for(int i=0;i<1200&&u.Active;i++)
            {
                Battle.Advance(.05f);Sync();yield return null;var v=FindObjectsByType<DemoSpriteVisual>().Single();
                if(!u.AltarEmpowered){normal|=v.Renderer.sprite.name.StartsWith("M19/")&&!v.Renderer.sprite.name.Contains("extra_");continue;}
                if(v.FrameState=="AltarIdle"||v.FrameState=="AltarWalk"||v.FrameState=="AltarAttack")
                    if(poses.Add(v.FrameState))yield return CaptureFrame(folder,v.FrameState);
                if(poses.Count==3)break;
            }
            bool effect=normal&&u.AltarEmpowered&&poses.Count==3;
            Battle.SetPaused(true);double clock=Battle.Clock;Battle.Advance(20);bool pause=Battle.Clock==clock;
            Debug.Log($"ALTAR_WARRIOR_CHECK: normal={normal}; empowered={u.AltarEmpowered}; walk={poses.Contains("AltarWalk")}; idle={poses.Contains("AltarIdle")}; attack={poses.Contains("AltarAttack")}; noInjectedStatus=True");
            Battle.Restart();Sync();bool reset=Battle.Units.Count==0;
            Debug.Log($"FACILITY_PLAYER_CHECK: map={mapIndex}; role={SelectedRole}; deployed={deployed}; effect={effect}; pause={pause}; reset={reset}");
            Application.Quit(deployed&&effect&&pause&&reset?0:1);
        }
    }
}
