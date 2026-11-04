using System;
using System.Collections.Generic;
using UnityEngine;
namespace YesterdayConvoy
{
    // First playable roster: Reunion/Snow foundations, plus the three existing manual-skill five-cost cards.
    public static class CampaignBalance
    {
        public sealed class Stats
        {
            public readonly int Cost,BaseDP,Gate;
            public readonly float HP,Attack,Armor,Interval,Range,Speed;
            public Stats(int cost,int dp,float hp,float attack,float armor,float interval,float range,float speed,int gate)
            {Cost=cost;BaseDP=dp;HP=hp;Attack=attack;Armor=armor;Interval=interval;Range=range;Speed=speed;Gate=gate;}
        }
        public static readonly Dictionary<string,Stats> Profiles=new Dictionary<string,Stats>{
            {"CARD_ORIGINIUM_SLUG",new Stats(1,2,190,28,0,1.8f,0.7f,0.65f,1)},
            {"CARD_M02",new Stats(1,2,125,22,0,1.25f,0.7f,1.1f,1)},
            {"CARD_M03",new Stats(1,2,150,24,2,1.4f,0.7f,1.0f,1)},
            {"CARD_M05",new Stats(2,3,190,30,0,2.2f,2.7f,0.6f,1)},
            {"CARD_M06",new Stats(2,3,160,34,0,2.8f,0.7f,0.6f,1)},
            {"CARD_M07",new Stats(1,2,175,24,4,1.8f,0.75f,0.7f,1)},
            {"CARD_M08",new Stats(1,2,125,24,1,1.9f,3.1f,0.65f,1)},
            {"CARD_M09",new Stats(1,2,120,30,0,2.4f,2.6f,0.65f,1)},
            {"CARD_M10",new Stats(2,3,380,34,12,2.6f,0.75f,0.5f,1)},
            {"CARD_M11",new Stats(1,2,135,25,1,1.9f,3.1f,0.65f,1)},
            {"CARD_M12",new Stats(2,3,200,45,1,2.2f,2.7f,0.65f,1)},
            {"CARD_M13",new Stats(3,4,390,68,8,3.2f,0.85f,0.55f,1)},
            {"CARD_M14",new Stats(5,8,720,76,6,2.4f,3.2f,0.6f,2)},
            {"CARD_M15",new Stats(1,2,180,26,4,1.8f,0.75f,0.75f,1)},
            {"CARD_M16",new Stats(2,3,190,45,2,2.1f,3.2f,0.65f,1)},
            {"CARD_M17",new Stats(3,4,300,34,4,2.2f,2.8f,0.65f,1)},
            {"CARD_M18",new Stats(4,6,750,58,18,2.5f,0.8f,0.45f,2)},
            {"CARD_M19",new Stats(3,4,360,56,8,2.0f,0.85f,0.65f,1)},
            {"CARD_TALULAH_TRIAL",new Stats(5,10,850,112,8,1.7f,3.2f,0.55f,2)},
            {"CARD_WISADEL_APRIL",new Stats(5,9,680,78,5,1.7f,3.2f,0.65f,2)},
        };
        public static bool Supports(string id)=>id!=null&&Profiles.ContainsKey(id);
        public static bool Reunion(string id)=>Supports(id)&&id!="CARD_ORIGINIUM_SLUG"&&id!="CARD_M05"&&id!="CARD_WISADEL_APRIL";
        public static bool Snow(string id)=>id=="CARD_M06"||id=="CARD_M11"||id=="CARD_M12"||id=="CARD_M13"||id=="CARD_M14";
        public static float HPScale(int cost,int star)=>star==1?1:cost==5?star==2?1.9f:5:cost==4?star==2?1.8f:3.2f:star==2?1.75f:3;
        public static float AttackScale(int cost,int star)=>star==1?1:cost==5?star==2?1.7f:3:cost==4?star==2?1.6f:2.6f:star==2?1.5f:2.15f;
        public static float AbilityScale(int cost,int star)=>star==1?1:cost==5?star==2?1.9f:5:cost==4?star==2?1.75f:3.5f:star==2?1.6f:2.7f;
        public static BasicRosterProfile Build(ManualBattleDefinition data,int star,int initialDP,int defense)
        {
            if(star<1||star>3)throw new ArgumentOutOfRangeException(nameof(star));
            var s=data.Snapshot();var c=data.Engagement.Snapshot();var r=Profiles[s.CardId];
            float ability=AbilityScale(r.Cost,star);var e=c.Effects;
            var effects=new MonsterEffectSettings(damageType:e.DamageType,resistance:e.Resistance,
                armorShred:s.CardId=="CARD_M05"?1+star:e.ArmorShred,
                blastAttack:e.BlastAttack*ability,blastRadius:e.BlastRadius,blastDelay:e.BlastDelay,coldDuration:e.ColdDuration,
                frozenAttackMultiplier:e.FrozenAttackMultiplier,coldAttackEvery:e.ColdAttackEvery,attackColdDuration:e.AttackColdDuration,
                frostNova:e.FrostNova,unitResistance:e.UnitResistance,tacticalCommander:e.TacticalCommander,orderRunner:e.OrderRunner,orderSniper:e.OrderSniper,tauntLevel:e.TauntLevel);
            // Never shorten a complete authored attack clip to fit a spreadsheet interval.
            float interval=Mathf.Max(r.Interval,c.AttackDuration);
            var combat=new EngagementSettings(r.Attack*AttackScale(r.Cost,star),r.Armor+2*(star-1),r.Range,interval,
                c.DefenderHealth,c.DefenderAttack,c.DefenderArmor,c.DefenderRange,c.DefenderInterval,c.BlockCount,c.BlockRadius,
                c.InitialSP,c.RequiredSP,c.SPPerSecond,c.SkillAmmo,c.SkillAttack*ability,c.SkillInterval,c.SkillRange,c.ProjectileTime,
                c.FireRain,c.HasSkill,c.Melee,c.AttackDuration,c.AttackWindup,effects,c.CanAttack,c.DefenderDamageType,c.RevealsInvisible,c.DefenderCanHitAir,c.FrostSeal);
            var unit=new ManualBattleSettings(s.CardId,s.UnitId,initialDP,defense,CampaignPlan.Uses(r.Cost),CampaignPlan.DP(r.Cost,r.BaseDP,star),
                new[]{0,2,3,4,5,8}[r.Cost],Mathf.Floor(r.HP*HPScale(r.Cost,star)+.5f),r.Speed,r.Gate+(star==3?1:0),s.TowerRange,s.TowerDamage,s.ShotInterval);
            return new BasicRosterProfile(unit,combat,true);
        }
    }
}
