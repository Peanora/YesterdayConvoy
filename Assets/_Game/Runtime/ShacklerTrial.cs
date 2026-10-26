using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FacilityTrialController
    {
        public int SilenceShacklers()
        {int count=0;foreach(var u in Battle.Units)if(Battle.TryDisableShackler(u.EntityId,3,true))count++;return count;}

        IEnumerator VerifyShackler(string folder,bool silence)
        {
            Time.timeScale=0;bool deployed=Deploy(),begin=false,loop=false,end=false,stopped=false,interrupted=false;
            var u=Battle.Units[0];int shots=0;
            for(int i=0;i<1200;i++)
            {
                Battle.Advance(.05f);Sync();yield return null;
                var cast=u.Shackle;if(cast==null)continue;
                if(!begin){begin=true;yield return CaptureFrame(folder,"shackle-begin");shots=Battle.ShotsFired;}
                if(!loop&&cast.Ticks>=3){loop=true;stopped=Battle.ShotsFired==shots&&Battle.DefenderShackled(Battle.Defender);yield return CaptureFrame(folder,"shackle-loop");}
                if(silence&&loop&&!interrupted){interrupted=SilenceShacklers()>0;yield return CaptureFrame(folder,"shackle-interrupted");}
                if(cast.Ended&&!end){end=true;yield return CaptureFrame(folder,"shackle-end");}
                if(end&&u.OrdinaryAttacksFired>0&&Battle.ShotsFired>shots)break;
            }
            var s=u.Shackle;bool released=!Battle.DefenderShackled(Battle.Defender),once=Battle.Shackles.Count==1;
            bool effect=begin&&loop&&end&&stopped&&released&&once&&s!=null&&s.DamageDealt>0&&u.OrdinaryAttacksFired>0&&
                (silence?interrupted&&s.Interrupted&&s.Ticks<20:!s.Interrupted&&s.Ticks==20);
            Battle.SetPaused(true);double clock=Battle.Clock;Battle.Advance(10);bool pause=Battle.Clock==clock;
            Debug.Log($"SHACKLER_FACILITY_CHECK: silence={silence}; begin={begin}; loop={loop}; end={end}; stopped={stopped}; released={released}; once={once}; ticks={s?.Ticks}; damage={s?.DamageDealt}; effect={effect}; noInjectedStatus=True");
            Battle.Restart();Sync();bool reset=Battle.Shackles.Count==0&&Battle.Units.Count==0;
            Debug.Log($"FACILITY_PLAYER_CHECK: map={mapIndex}; role={SelectedRole}; deployed={deployed}; effect={effect}; pause={pause}; reset={reset}");
            Application.Quit(deployed&&effect&&pause&&reset?0:1);
        }
    }
}
