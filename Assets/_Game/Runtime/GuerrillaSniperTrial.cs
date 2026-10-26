using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class GuerrillaTrialController
    {
        IEnumerator VerifySniper(string folder)
        {
            Directory.CreateDirectory(folder);Time.timeScale=0;bool deployed=Deploy("CARD_M16");var u=Battle.Units[0];
            for(int i=0;i<500&&u.OrdinaryAttacksFired==0;i++){Advance(.1f);yield return null;}
            var initial=Battle.CombatShots.Where(s=>!s.FromDefender&&s.SourceId==u.EntityId).ToArray();bool normal=initial.Length==1;
            yield return Capture(folder,"sniper-single");int attacks=u.OrdinaryAttacksFired;deployed&=Deploy("CARD_M17");CombatShot[] pair=Array.Empty<CombatShot>();
            for(int i=0;i<300;i++)
            {
                Advance(.05f);yield return null;
                pair=Battle.CombatShots.Where(s=>!s.FromDefender&&s.SourceId==u.EntityId&&!s.Resolved).GroupBy(s=>s.StartedAt).Where(g=>g.Count()==2).Select(g=>g.ToArray()).FirstOrDefault()??Array.Empty<CombatShot>();
                if(pair.Length==2)break;
            }
            bool distinct=pair.Select(s=>s.TargetId).Distinct().Count()==2,applied=false,oneAttack=u.OrdinaryAttacksFired==attacks+1;
            if(pair.Length==2)
            {Advance(Mathf.Max(0,(float)(pair[0].ImpactAt-Battle.Clock)-.08f));yield return null;yield return Capture(folder,"sniper-two-projectiles");Advance(.2f);yield return null;applied=pair.All(s=>s.Applied&&s.DamageDealt>0);yield return Capture(folder,"sniper-two-hits");}
            Battle.SetPaused(true);double clock=Battle.Clock;Advance(10);bool pause=Battle.Clock==clock;
            Debug.Log($"ORDER_SNIPER_CHECK: deployed={deployed}; normalSingle={normal}; commandedTargets={pair.Length}; distinct={distinct}; bothApplied={applied}; oneAttack={oneAttack}; pause={pause}; noInjectedStatus=True");
            ResetTrial();yield return null;bool reset=Battle.Units.Count==0;
            Application.Quit(deployed&&normal&&distinct&&applied&&oneAttack&&pause&&reset?0:1);
        }
    }
}
