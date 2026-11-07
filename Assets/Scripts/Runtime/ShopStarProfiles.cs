using System;
using UnityEngine;

namespace YesterdayConvoy
{
    // Legacy shop maps retain their own one-star tuning and apply the shared star growth to copies.
    public static class ShopStarProfiles
    {
        public static BasicRosterProfile Build(ManualBattleSettings mapped,EngagementSettings authored,int cost,int star)
        {
            if(star<1||star>ShopTrialModel.MaxStarForCost(cost))throw new ArgumentOutOfRangeException(nameof(star));
            if(star==1)return new BasicRosterProfile(mapped,authored,true);
            float ability=CampaignBalance.AbilityScale(cost,star);
            bool support=CampaignBalance.Profiles.TryGetValue(mapped.CardId,out var stats)&&stats.Attack<=0;
            var combat=new EngagementSettings(authored.UnitAttack*(support?ability:CampaignBalance.AttackScale(cost,star)),
                authored.UnitArmor+2*(star-1),authored.UnitRange,authored.UnitInterval,
                authored.DefenderHealth,authored.DefenderAttack,authored.DefenderArmor,authored.DefenderRange,authored.DefenderInterval,
                authored.BlockCount,authored.BlockRadius,authored.InitialSP,authored.RequiredSP,authored.SPPerSecond,
                authored.SkillAmmo,authored.SkillAttack*ability,authored.SkillInterval,authored.SkillRange,authored.ProjectileTime,
                authored.FireRain,authored.HasSkill,authored.Melee,authored.AttackDuration,authored.AttackWindup,
                CampaignBalance.CopyEffects(authored.Effects,ability),authored.CanAttack,authored.DefenderDamageType,
                authored.RevealsInvisible,authored.DefenderCanHitAir,authored.FrostSeal,authored.MudrockCapture);
            var unit=new ManualBattleSettings(mapped.CardId,mapped.UnitId,mapped.InitialDP,mapped.InitialDefense,mapped.DeploymentLimit,
                CampaignPlan.DP(cost,mapped.DeploymentCost,star),mapped.Cooldown,
                Mathf.Floor(mapped.Health*CampaignBalance.HPScale(cost,star)+.5f),mapped.Speed,mapped.GateDamage+(star>=3?1:0),
                mapped.TowerRange,mapped.TowerDamage,mapped.ShotInterval);
            return new BasicRosterProfile(unit,combat,true);
        }
    }

    public sealed partial class ManualBattleController
    {
        BasicRosterProfile ShopStarProfile(ManualBattleDefinition data,ManualBattleSettings mappedSettings)
        {
            if(data==null)throw new ArgumentNullException(nameof(data));
            int star=ShopTrialActive?(ShopLineupPiece(mappedSettings.CardId)?.Star??1):1;
            return ShopStarProfiles.Build(mappedSettings,data.Engagement.Snapshot(),data.Card.RecruitCost,star);
        }
    }
}
