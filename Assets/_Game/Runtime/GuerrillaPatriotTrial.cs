using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class GuerrillaTrialController
    {
        IEnumerator VerifyPatriotMobility(string folder)
        {
            Directory.CreateDirectory(folder);Time.timeScale=0;bool deployed=Deploy("CARD_M20");var u=Battle.Units[0];bool moving=false;double previous=0;bool actualMove=false;
            for(int i=0;i<1200&&u.Active;i++)
            {
                Advance(.1f);yield return null;
                if(u.PatriotPhase==PatriotPhase.Destruction&&u.IsMoving)
                {if(!moving){moving=true;previous=u.Motion.DistanceTravelled;}else if(u.Motion.DistanceTravelled>previous+.2&&actors[u.EntityId].GetComponentInChildren<DemoSpriteVisual>().Renderer.sprite.name.StartsWith("M20/extra_move_2/")){actualMove=true;break;}}
            }
            yield return Capture(folder,"patriot-second-walk");var v=actors[u.EntityId].GetComponentInChildren<DemoSpriteVisual>();bool original=v.Renderer.sprite.name.StartsWith("M20/extra_move_2/");bool natural=u.PatriotPhase==PatriotPhase.Destruction&&Battle.CombatShots.Any(s=>s.FromDefender&&s.Applied&&s.TargetId==u.EntityId);
            Debug.Log($"PATRIOT_MOBILITY_CHECK: deployed={deployed}; actualMove={actualMove}; originalSecondWalk={original}; naturalFirstDeath={natural}; noInjectedHealth=True");
            ResetTrial();yield return null;Application.Quit(deployed&&actualMove&&original&&natural?0:1);
        }
        IEnumerator VerifyPatriot(string folder)
        {
            Directory.CreateDirectory(folder);Time.timeScale=0;bool deployed=Deploy("CARD_M20");var u=Battle.Units[0];
            bool four=false,begin=false,loop=false,end=false,second=false,spear=false,hit=false;
            double reborn=double.NaN,secondAt=double.NaN;
            for(int i=0;i<1300&&u.Active;i++)
            {
                Advance(.1f);yield return null;
                if(!four&&u.PatriotPhase==PatriotPhase.March&&u.OrdinaryAttacksFired>0)
                {var volley=Battle.CombatShots.Where(s=>!s.FromDefender&&s.SourceId==u.EntityId).Take(4).ToArray();if(volley.Length==4&&volley.All(s=>s.Applied&&s.DamageDealt>0)){four=true;yield return Capture(folder,"patriot-four-hit");}}
                if(u.PatriotPhase==PatriotPhase.Reborn)
                {
                    if(double.IsNaN(reborn))reborn=u.PatriotPhaseStartedAt;
                    double age=Battle.Clock-reborn;
                    if(!begin&&age>.3){begin=true;yield return Capture(folder,"patriot-reborn-begin");}
                    if(!loop&&age>3){loop=true;yield return Capture(folder,"patriot-reborn-loop");}
                    if(!end&&age>57){end=true;yield return Capture(folder,"patriot-reborn-end");}
                }
                if(!second&&u.PatriotPhase==PatriotPhase.Destruction)
                {second=true;secondAt=u.PatriotPhaseStartedAt;yield return Capture(folder,"patriot-destruction");}
                if(!spear&&Battle.PatriotCasting(u)){spear=true;yield return Capture(folder,"patriot-spear-windup");}
                if(spear&&u.Spear.Shot.Applied&&u.Spear.Shot.DamageDealt>0)
                {hit=true;yield return Capture(folder,"patriot-spear-hit");break;}
            }
            bool duration=secondAt-reborn>=59.99&&secondAt-reborn<60.1;
            bool natural=Battle.CombatShots.Any(s=>s.FromDefender&&s.Applied&&s.TargetId==u.EntityId);
            bool highGround=u.Spear!=null&&Battle.FindDefender(u.Spear.TargetId).HighGround;
            Battle.SetPaused(true);double clock=Battle.Clock;Advance(10);bool pause=Battle.Clock==clock;
            int ticks=u.ImmolationTicks;float damage=u.ImmolationDamage;ResetTrial();yield return null;bool reset=Battle.Units.Count==0;
            Debug.Log($"PATRIOT_CHECK: deployed={deployed}; four={four}; naturalDamage={natural}; begin={begin}; loop={loop}; end={end}; reborn60={duration}; second={second}; spear={spear}; actualSpearHit={hit}; highGround={highGround}; immolationTicks={ticks}; trueDamage={damage}; pause={pause}; reset={reset}; noInjectedHealth=True");
            Application.Quit(deployed&&four&&natural&&begin&&loop&&end&&duration&&second&&spear&&hit&&highGround&&ticks>0&&damage>0&&pause&&reset?0:1);
        }
    }
}
