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
        public const string GolemCardId="CARD_GOLEM_SUPPORT";
        public static readonly Dictionary<string,Stats> Profiles=new Dictionary<string,Stats>{
            {"CARD_ORIGINIUM_SLUG",new Stats(1,2,190,28,0,1.8f,0.7f,0.65f,1)},
            {"CARD_M02",new Stats(1,2,125,22,0,1.25f,0.7f,1.1f,1)},
            {"CARD_M03",new Stats(1,2,150,24,2,1.4f,0.7f,1.0f,1)},
            {"CARD_M04",new Stats(2,3,200,38,0,2.8f,0.7f,0.6f,1)},
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
            {"CARD_M21",new Stats(1,2,170,22,1,1.7f,0.75f,0.65f,1)},
            {"CARD_M22",new Stats(2,3,250,36,4,1.9f,0.8f,0.7f,1)},
            {"CARD_M23",new Stats(3,4,350,54,5,1.9f,0.85f,0.65f,1)},
            {"CARD_M24",new Stats(4,6,650,90,10,3.0f,1.0f,0.55f,2)},
            {"CARD_M26",new Stats(2,3,220,42,4,2.2f,2.5f,0.65f,1)},
            {"CARD_M27",new Stats(3,4,420,62,10,2.2f,0.85f,0.55f,1)},
            {"CARD_M28",new Stats(5,10,1500,140,24,2.5f,1.2f,0.5f,2)},
            {"CARD_M42",new Stats(3,4,350,66,6,2.2f,2.7f,0.65f,1)},
            {"CARD_M43",new Stats(4,6,850,105,18,2.4f,1.0f,0.5f,2)},
            {"CARD_M29",new Stats(2,3,320,40,11,2.2f,0.8f,0.55f,1)},
            {"CARD_M30",new Stats(3,4,330,58,6,1.8f,0.85f,0.75f,1)},
            {"CARD_M31",new Stats(4,6,520,90,7,3.0f,2.2f,0.55f,2)},
            {"CARD_M32",new Stats(5,10,1000,110,10,1.8f,1.5f,0.5f,2)},
            {"CARD_M33",new Stats(3,4,600,0,0,1.0f,1.6f,0.45f,1)},
            {"CARD_M34",new Stats(3,4,350,52,6,3.0f,0.85f,0.75f,1)},
            {"CARD_M35",new Stats(3,4,280,52,4,1.5f,0.75f,1.1f,1)},
            {"CARD_M36",new Stats(4,6,600,82,5,2.8f,3.3f,0.6f,2)},
            {"CARD_M37",new Stats(2,3,280,48,7,2.1f,0.85f,0.65f,1)},
            {"CARD_M38",new Stats(3,4,260,68,1,2.8f,2.8f,0.65f,1)},
            {"CARD_TALULAH_TRIAL",new Stats(5,10,850,112,8,1.7f,3.2f,0.55f,2)},
            {"CARD_WISADEL_APRIL",new Stats(5,9,680,78,5,1.7f,3.2f,0.65f,2)},
        };
        public static bool Supports(string id)=>id!=null&&Profiles.ContainsKey(id);
        static readonly HashSet<string> ReunionMembers=new HashSet<string>{"CARD_M02","CARD_M03","CARD_M06","CARD_M07","CARD_M08","CARD_M09","CARD_M10","CARD_M11","CARD_M12","CARD_M13","CARD_M14","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M21","CARD_M22","CARD_M23","CARD_TALULAH_TRIAL"};
        public static bool Reunion(string id)=>id!=null&&ReunionMembers.Contains(id);
        static readonly HashSet<string> SnowMembers=new HashSet<string>{"CARD_M06","CARD_M11","CARD_M12","CARD_M13","CARD_M14"};
        public static bool Snow(string id)=>id!=null&&SnowMembers.Contains(id);
        static readonly HashSet<string> InfectedMembers=new HashSet<string>{"CARD_ORIGINIUM_SLUG","CARD_M02","CARD_M03","CARD_M04","CARD_M05","CARD_M06"};
        public static bool Infected(string id)=>id!=null&&InfectedMembers.Contains(id);
        static readonly HashSet<string> GuerrillaMembers=new HashSet<string>{"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19"};
        public static bool Guerrilla(string id)=>id!=null&&GuerrillaMembers.Contains(id);
        static readonly HashSet<string> MudrockCrewMembers=new HashSet<string>{"CARD_M26","CARD_M27","CARD_M42","CARD_M43","CARD_M28"};
        public static bool MudrockCrew(string id)=>id!=null&&MudrockCrewMembers.Contains(id);
        static readonly Dictionary<string,string[]> labels=new Dictionary<string,string[]>{
            {"CARD_ORIGINIUM_SLUG",new[]{"感染生物"}},
            {"CARD_M02",new[]{"感染生物","整合运动"}},
            {"CARD_M03",new[]{"感染生物","游击队","整合运动"}},
            {"CARD_M04",new[]{"感染生物"}},
            {"CARD_M05",new[]{"感染生物"}},
            {"CARD_M06",new[]{"感染生物","雪怪小队","整合运动"}},
            {"CARD_M07",new[]{"整合运动"}},
            {"CARD_M08",new[]{"整合运动"}},
            {"CARD_M09",new[]{"整合运动"}},
            {"CARD_M10",new[]{"整合运动"}},
            {"CARD_M11",new[]{"雪怪小队","整合运动"}},
            {"CARD_M12",new[]{"雪怪小队","整合运动"}},
            {"CARD_M13",new[]{"雪怪小队","整合运动"}},
            {"CARD_M14",new[]{"雪怪小队","整合运动"}},
            {"CARD_M15",new[]{"游击队","整合运动"}},
            {"CARD_M16",new[]{"游击队","整合运动"}},
            {"CARD_M17",new[]{"游击队","整合运动"}},
            {"CARD_M18",new[]{"游击队","整合运动"}},
            {"CARD_M19",new[]{"游击队","整合运动","萨卡兹"}},
            {"CARD_M21",new[]{"牧群","整合运动"}},
            {"CARD_M22",new[]{"牧群","整合运动"}},
            {"CARD_M23",new[]{"牧群","整合运动"}},
            {"CARD_M24",new[]{"牧群","萨卡兹"}},
            {"CARD_M26",new[]{"泥岩小队","萨卡兹"}},
            {"CARD_M27",new[]{"泥岩小队","萨卡兹"}},
            {"CARD_M28",new[]{"泥岩小队","萨卡兹"}},
            {"CARD_M42",new[]{"泥岩小队","萨卡兹"}},
            {"CARD_M43",new[]{"泥岩小队","萨卡兹"}},
            {"CARD_M32",new[]{"深池"}},
            {"CARD_M29",new[]{"深池"}},
            {"CARD_M30",new[]{"深池"}},
            {"CARD_M31",new[]{"深池"}},
            {"CARD_M33",new[]{"海嗣"}},
            {"CARD_M34",new[]{"海嗣"}},
            {"CARD_M35",new[]{"海嗣"}},
            {"CARD_M36",new[]{"海嗣"}},
            {"CARD_M37",new[]{"萨卡兹"}},
            {"CARD_M38",new[]{"萨卡兹"}},
            {"CARD_TALULAH_TRIAL",new[]{"整合运动"}},
            {"CARD_WISADEL_APRIL",new[]{"萨卡兹"}},
        };
        public static string[] Tags(string id)=>labels.TryGetValue(id,out var list)?(string[])list.Clone():Array.Empty<string>();
        public static float HPScale(int cost,int star)=>star==1?1:cost==5?star==2?1.9f:5:cost==4?star==2?1.8f:3.2f:star==2?1.75f:3;
        public static float AttackScale(int cost,int star)=>star==1?1:cost==5?star==2?1.7f:3:cost==4?star==2?1.6f:2.6f:star==2?1.5f:2.15f;
        public static float AbilityScale(int cost,int star)=>star==1?1:cost==5?star==2?1.9f:5:cost==4?star==2?1.75f:3.5f:star==2?1.6f:2.7f;
        public static BasicRosterProfile Build(ManualBattleDefinition data,int star,int initialDP,int defense)
        {
            if(star<1||star>3)throw new ArgumentOutOfRangeException(nameof(star));
            var s=data.Snapshot();var c=data.Engagement.Snapshot();var r=Profiles[s.CardId];
            float ability=AbilityScale(r.Cost,star);var e=c.Effects;
            var effects=CopyEffects(e,ability);
            // Never shorten a complete authored attack clip to fit a spreadsheet interval.
            float interval=Mathf.Max(r.Interval,c.AttackDuration);
            var combat=new EngagementSettings((r.Attack>0?r.Attack*AttackScale(r.Cost,star):c.UnitAttack*ability),r.Armor+2*(star-1),r.Range,interval,
                c.DefenderHealth,c.DefenderAttack,c.DefenderArmor,c.DefenderRange,c.DefenderInterval,c.BlockCount,c.BlockRadius,
                c.InitialSP,c.RequiredSP,c.SPPerSecond,c.SkillAmmo,c.SkillAttack*ability,c.SkillInterval,c.SkillRange,c.ProjectileTime,
                c.FireRain,c.HasSkill,c.Melee,c.AttackDuration,c.AttackWindup,effects,c.CanAttack,c.DefenderDamageType,c.RevealsInvisible,c.DefenderCanHitAir,c.FrostSeal,c.MudrockCapture);
            var unit=new ManualBattleSettings(s.CardId,s.UnitId,initialDP,defense,CampaignPlan.Uses(r.Cost),CampaignPlan.DP(r.Cost,r.BaseDP,star),
                new[]{0,2,3,4,5,8}[r.Cost],Mathf.Floor(r.HP*HPScale(r.Cost,star)+.5f),r.Speed,r.Gate+(star==3?1:0),s.TowerRange,s.TowerDamage,s.ShotInterval);
            return new BasicRosterProfile(unit,combat,true);
        }

        public static MonsterEffectSettings CopyEffects(MonsterEffectSettings e,float ability=1,float blastFraction=1,bool secondary=false)=>new MonsterEffectSettings(
            damageType:e.DamageType,resistance:e.Resistance,armorShred:secondary?0:e.ArmorShred,blastAttack:e.BlastAttack*ability*blastFraction,
            blastRadius:e.BlastRadius,blastDelay:e.BlastDelay,coldDuration:secondary?0:e.ColdDuration,frozenAttackMultiplier:e.FrozenAttackMultiplier,
            coldAttackEvery:secondary?0:e.ColdAttackEvery,attackColdDuration:secondary?0:e.AttackColdDuration,frostNova:e.FrostNova,regenerationFraction:e.RegenerationFraction,
            attackTargets:e.AttackTargets,lifeSteal:e.LifeSteal,healer:e.Healer,artsBarrier:e.ArtsBarrier,
            unitResistance:e.UnitResistance,mudrock:e.Mudrock,phalanx:e.Phalanx,flamechaser:e.Flamechaser,
            flamerazer:e.Flamerazer,mandragora:e.Mandragora,nervousCreeper:e.NervousCreeper,harvester:e.Harvester,
            firstSpeaker:e.FirstSpeaker,shackler:e.Shackler,tacticalCommander:e.TacticalCommander,orderRunner:e.OrderRunner,
            orderSniper:e.OrderSniper,tauntLevel:e.TauntLevel,patriot:e.Patriot);
        public static EngagementSettings CopyCombat(EngagementSettings c,float attack,float armor,MonsterEffectSettings e)=>new EngagementSettings(
            attack,armor,c.UnitRange,c.UnitInterval,c.DefenderHealth,c.DefenderAttack,c.DefenderArmor,c.DefenderRange,c.DefenderInterval,c.BlockCount,c.BlockRadius,
            c.InitialSP,c.RequiredSP,c.SPPerSecond,c.SkillAmmo,c.SkillAttack,c.SkillInterval,c.SkillRange,c.ProjectileTime,c.FireRain,c.HasSkill,c.Melee,c.AttackDuration,c.AttackWindup,e,c.CanAttack,c.DefenderDamageType,c.RevealsInvisible,c.DefenderCanHitAir,c.FrostSeal,c.MudrockCapture);
    }
}
