using System;
using System.Linq;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        internal const float SteamSourceContact=5.7f;
        internal int SteamTier=>Campaign==null?0:Level("蒸汽");
        internal float SteamSourceDuration=>SteamTier>=3?13.2f:7.5f;

        void ResolveSteamSkill(FormationFighter unit,FormationDefender target,FormationPresentationAction presentation)
        {
            double rate=Math.Max(.01,presentation.Rate);
            for(int index=0;index<3;index++)
            {
                ScheduleOwned(unit,presentation.ContactAt+index*.2/rate,()=>
                {
                    var victim=target?.Alive==true?target:FighterTarget(unit);
                    if(victim!=null)SkillArea(unit,victim,1.2f,0,1.8f,3);
                });
            }
            if(SteamTier<3)return;
            for(int index=0;index<3;index++)
                ScheduleOwned(unit,presentation.At+(9.2+index*.8)/rate,()=>
                    NativeSegment(unit,Defenders.Where(enemy=>enemy.Alive).ToArray(),unit.Attack*1.8f,1,true,0,true));
        }
    }
}
