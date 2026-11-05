using System;
using System.Collections.Generic;
using System.Linq;
namespace YesterdayConvoy
{
    // Shared by preparation, combat snapshots and the inspection panel. All gates are cumulative.
    public static class GolemGrowth
    {
        public static int Stage(int experience,int invested,IEnumerable<ShopTrialModel.Piece> formation)
        {
            var crew=formation.Where(p=>CampaignBalance.MudrockCrew(p.Id)).GroupBy(p=>p.Id).Select(g=>g.Max(p=>p.Star)).ToArray();
            return Stage(experience,invested,crew.Length,crew.Length==5&&crew.All(s=>s>=2));
        }
        public static int Stage(int experience,int invested,int crew,bool allTwoStar)
        {
            if(experience<0||invested<0)throw new ArgumentOutOfRangeException();
            if(crew>=5&&allTwoStar&&experience>=24&&invested>=80)return 4;
            if(crew>=4&&experience>=12&&invested>=45)return 3;
            if(crew>=2&&experience>=4&&invested>=20)return 2;
            return 1;
        }
        public static int Cost(int stage)=>new[]{0,4,5,8,14}[stage];
        public static string Requirements(int stage)=>stage==1?"II：培养4 · 投入20 · 2名成员":stage==2?"III：培养12 · 投入45 · 4名成员":stage==3?"IV：培养24 · 投入80 · 5名全二星":"IV 终极巨像已解锁";
    }
    public sealed partial class ManualBattle
    {
        public bool UnitControlImmune(DeployedUnitState u)=>PatriotControlImmune(u)||u!=null&&u.Golem&&GolemStage==4;
    }
}
