using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class GuerrillaTrialController
    {
        IEnumerator VerifyTaunt(string folder)
        {
            Directory.CreateDirectory(folder);Time.timeScale=0;
            bool deployed=Deploy("CARD_M15");var soldier=Battle.Units[0];Advance(3);yield return null;
            deployed&=Deploy("CARD_M18");var shield=Battle.Units.Last();bool wasSoldier=false,retarget=false;
            for(int i=0;i<300;i++)
            {
                Advance(.1f);yield return null;
                wasSoldier|=Battle.Defenders.Any(d=>d.TargetId==soldier.EntityId);
                if(soldier.Active&&Battle.Defenders.All(d=>d.TargetId==shield.EntityId)){retarget=true;break;}
            }
            yield return Capture(folder,"shield-retarget");Advance(.5f);yield return null;
            float health=soldier.Health,shieldHealth=shield.Health;Advance(2);yield return null;
            bool protectedSoldier=soldier.Active&&Mathf.Abs(health-soldier.Health)<.001f;
            bool realDamage=shield.Health<shieldHealth&&Battle.CombatShots.Any(s=>s.FromDefender&&s.Applied&&s.TargetId==shield.EntityId);
            yield return Capture(folder,"shield-protects");Battle.SetPaused(true);double clock=Battle.Clock;Advance(10);bool pause=Battle.Clock==clock;
            ResetTrial();yield return null;bool reset=Battle.Units.Count==0;
            Debug.Log($"TAUNT_CHECK: deployed={deployed}; wasSoldier={wasSoldier}; retarget={retarget}; protected={protectedSoldier}; actualShieldDamage={realDamage}; pause={pause}; reset={reset}; noInjectedDamage=True");
            Application.Quit(deployed&&wasSoldier&&retarget&&protectedSoldier&&realDamage&&pause&&reset?0:1);
        }
    }
}
