using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class GuerrillaTrialController
    {
        IEnumerator VerifyRunner(string folder)
        {
            Directory.CreateDirectory(folder);Time.timeScale=0;bool deployed=Deploy("CARD_M15");var u=Battle.Units[0];Advance(1);yield return null;
            double first=u.Motion.DistanceTravelled;Advance(.5f);yield return null;double normal=u.Motion.DistanceTravelled-first;yield return Capture(folder,"runner-normal");
            deployed&=Deploy("CARD_M17");first=u.Motion.DistanceTravelled;Advance(.5f);yield return null;double commanded=u.Motion.DistanceTravelled-first;yield return Capture(folder,"runner-commanded");
            bool distances=Math.Abs(normal-u.Motion.Speed*.5)<.001&&Math.Abs(commanded-(u.Motion.Speed+.3)*.5)<.001;
            Battle.SetPaused(true);first=u.Motion.DistanceTravelled;Advance(10);bool pause=u.Motion.DistanceTravelled==first;
            Debug.Log($"ORDER_RUNNER_CHECK: deployed={deployed}; commanders={Battle.TacticalCommanders}; normalDistance={normal}; commandDistance={commanded}; distances={distances}; pause={pause}; noInjectedStatus=True");
            ResetTrial();yield return null;bool reset=Battle.Units.Count==0&&Battle.TacticalCommanders==0;
            Application.Quit(deployed&&distances&&pause&&reset?0:1);
        }
    }
}
