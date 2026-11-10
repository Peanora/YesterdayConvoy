using System.Linq;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        string StyleSidebarProgress(string name)
        {
            if(Battle==null||Battle.Phase==BattlePhase.Ready)return "打法 · 战前锁档";
            switch(CombatStyleRules.Find(name).Style)
            {
                case CombatStyle.Assault:return "破阵目标 "+Battle.Defenders.Count(d=>d.Alive&&Battle.StyleRuptureRemaining(d)>0);
                case CombatStyle.Marksman:return "暴露目标 "+Battle.Defenders.Count(d=>d.Alive&&Battle.StyleExposureRemaining(d)>0);
                case CombatStyle.Mystic:return "已触发脉冲 "+Battle.StylePulses;
                case CombatStyle.Bulwark:return "已发动掩护 "+Battle.StyleWards;
                case CombatStyle.Frenzy:return "已进入狂热 "+Battle.StyleRages;
                case CombatStyle.Coordination:return "接续 "+Battle.StyleCoordinations+"次 · SP＋"+Battle.StyleSPGranted.ToString("0.#");
                default:return "打法 · 战前锁档";
            }
        }
    }
}
