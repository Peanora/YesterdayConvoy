namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        // Test-mode snapshots only. Authored assets, normal campaigns and the IV solo benchmark stay unchanged.
        const float SandboxMonsterHealth=4, SandboxMonsterAttack=2;
        const float SandboxDefenderHealth=12, SandboxDefenderAttack=8;
        bool BoostSandboxStats=>SynergySandbox&&!BlackForestChallengeActive&&!ApprovedMapActive&&!golemBenchmark;

        BasicRosterProfile CampaignProfile(string id,int dp,int defense)
        {
            int star=ShopLineupPiece(id)?.Star??1;
            var source=CampaignBalance.Build(shopDefinitions[id],star,dp,defense,waveMode:shopMapCopy!=null&&shopMapCopy.attackWaves!=null);
            if(!BoostSandboxStats)return source;
            var u=source.Unit;var c=source.Combat;
            var unit=new ManualBattleSettings(u.CardId,u.UnitId,u.InitialDP,u.InitialDefense,u.DeploymentLimit,u.DeploymentCost,
                u.Cooldown,u.Health*SandboxMonsterHealth,u.Speed,u.GateDamage,u.TowerRange,u.TowerDamage,u.ShotInterval);
            var combat=new EngagementSettings(c.UnitAttack*SandboxMonsterAttack,c.UnitArmor,c.UnitRange,c.UnitInterval,
                c.DefenderHealth,c.DefenderAttack,c.DefenderArmor,c.DefenderRange,c.DefenderInterval,c.BlockCount,c.BlockRadius,
                c.InitialSP,c.RequiredSP,c.SPPerSecond,c.SkillAmmo,c.SkillAttack*SandboxMonsterAttack,c.SkillInterval,c.SkillRange,c.ProjectileTime,
                c.FireRain,c.HasSkill,c.Melee,c.AttackDuration,c.AttackWindup,CampaignBalance.CopyEffects(c.Effects,SandboxMonsterAttack),
                c.CanAttack,c.DefenderDamageType,c.RevealsInvisible,c.DefenderCanHitAir,c.FrostSeal,c.MudrockCapture);
            return new BasicRosterProfile(unit,combat,true);
        }
    }
}
