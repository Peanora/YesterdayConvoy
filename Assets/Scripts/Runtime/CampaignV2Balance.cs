using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace YesterdayConvoy
{
    // The adopted JSON snapshots are the numerical source. Legacy profiles remain
    // separate so opening an earlier trial does not retune its authored content.
    public static class CampaignV2Balance
    {
        [Serializable] sealed class PlayerFile {public Player[] units;}
        [Serializable] sealed class Player
        {public string id;public int cost;public Star[] stars;public Skill[] skills;}
        [Serializable] sealed class Star
        {
            public int star,copies,dp,uses,gate;
            public float hp,attack,armor,resistance,interval,range,speed,attackDuration,attackWindup,effectiveAttackWindup,projectileTime,nonWaveCooldown,waveCooldown,spawnHp;
            public bool canOrdinaryAttack;
            public string damageType;
        }
        [Serializable] sealed class Skill {public string id;public SkillParameter[] parameters;}
        [Serializable] sealed class SkillParameter
        {public string key;public float star1,star2,star3,star4;public float Value(int star)=>star==1?star1:star==2?star2:star==3?star3:star4;}
        [Serializable] sealed class SynergyFile {public Synergy[] synergies;public Summon[] summons;}
        [Serializable] sealed class Synergy {public string id;public Tier[] tiers;}
        [Serializable] sealed class Tier {public int count;public Parameter[] parameters;}
        [Serializable] sealed class Parameter
        {public string key,text;public bool numeric;public float number;public float[] list;}
        [Serializable] sealed class Summon {public string id;public SummonCommon common;public SummonStage[] stages;}
        [Serializable] sealed class SummonCommon
        {public float usesPerBattle,speed,intervalSeconds,attackDurationSeconds,windupSeconds,projectileSeconds,gateDamage,SP,skill;public bool synergySupport;}
        [Serializable] sealed class SummonStage
        {
            public int stage;
            public float requiredXP,requiredKinds,requiredTotalStars,hp,attack,armor,resistance,dp,range,targets,thirdHitPrimaryPhysical,thirdHitSidePhysical,thirdHitSideTargets,thirdHitSideRadius,globalThirdHitArtsFraction,healPerSuccessfulAttackFraction;
            public bool controlImmune,canHitHighGround;
        }
        static Dictionary<string,Player> players;
        static Dictionary<string,Synergy> synergies;
        static Summon golem;
        static readonly BindingFlags Fields=BindingFlags.Public|BindingFlags.Instance;

        static string Source(string name)
        {
            var text=Resources.Load<TextAsset>("CampaignV2/"+name);
            if(text==null)throw new InvalidOperationException("Missing adopted campaign v2 source: "+name);
            return text.text;
        }
        static void Load()
        {
            if(players!=null)return;
            // JsonUtility has fixed field types. The source's two boolean skill
            // parameters become numeric flags in memory, never in the saved JSON.
            string playerJson=Regex.Replace(Source("player-roster"),"(\\\"star[1-4]\\\"\\s*:\\s*)(true|false)",m=>m.Groups[1].Value+(m.Groups[2].Value=="true"?"1":"0"));
            var data=JsonUtility.FromJson<PlayerFile>(playerJson);
            if(data?.units==null||data.units.Length!=43||data.units.Sum(u=>u.stars.Length)!=138||data.units.Sum(u=>u.skills.Length)!=92)
                throw new InvalidOperationException("Incomplete adopted campaign v2 player roster.");
            var nextPlayers=new Dictionary<string,Player>(StringComparer.Ordinal);
            foreach(var u in data.units)
            {
                if(string.IsNullOrWhiteSpace(u.id)||!nextPlayers.TryAdd(u.id,u)||u.cost<1||u.cost>5)throw new InvalidOperationException("Invalid v2 player identity.");
                foreach(var s in u.stars)
                    if(s.star<1||s.star>(u.cost==1?4:3)||s.copies!=(int)Math.Pow(3,s.star-1)||s.dp<=0||s.uses<=0||s.hp<=0||s.interval<s.attackDuration||s.attack<0)
                        throw new InvalidOperationException("Invalid adopted v2 player row: "+u.id);
            }
            // Numeric, textual and list-valued synergy parameters retain distinct
            // types. A numeric lookup of an unknown/textual key fails explicitly.
            string synergyJson=Source("synergies");
            synergyJson=Regex.Replace(synergyJson,"\\\"value\\\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?)",m=>"\"number\":"+m.Groups[1].Value+",\"numeric\":true");
            synergyJson=Regex.Replace(synergyJson,"\\\"value\\\"\\s*:\\s*(true|false)",m=>"\"number\":"+(m.Groups[1].Value=="true"?"1":"0")+",\"numeric\":true");
            synergyJson=Regex.Replace(synergyJson,"\\\"value\\\"\\s*:\\s*\\\"","\"text\":\"");
            synergyJson=Regex.Replace(synergyJson,"\\\"value\\\"\\s*:\\s*\\[","\"list\":[");
            var effects=JsonUtility.FromJson<SynergyFile>(synergyJson);
            if(effects?.synergies==null||effects.synergies.Length!=9||effects.synergies.Sum(s=>s.tiers.Length)!=29)
                throw new InvalidOperationException("Incomplete adopted v2 synergy table.");
            var nextSynergies=effects.synergies.ToDictionary(s=>s.id,StringComparer.Ordinal);
            var nextGolem=effects.summons?.SingleOrDefault(s=>s.id=="mudrock-golem");
            if(nextGolem?.stages==null||nextGolem.stages.Length!=5)throw new InvalidOperationException("Missing adopted v2 golem table.");
            players=nextPlayers;synergies=nextSynergies;golem=nextGolem;
        }

        public static int PlayerCount {get{Load();return players.Count;}}
        public static int StarRowCount {get{Load();return players.Values.Sum(u=>u.stars.Length);}}
        public static int SkillCount {get{Load();return players.Values.Sum(u=>u.skills.Length);}}
        public static string[] PlayerIds {get{Load();return players.Keys.OrderBy(id=>id,StringComparer.Ordinal).ToArray();}}
        static Player PlayerFor(string id)
        {Load();if(!players.TryGetValue(id,out var p))throw new ArgumentException("Unknown adopted v2 card: "+id);return p;}
        static Star Row(string id,int star)=>PlayerFor(id).stars.SingleOrDefault(s=>s.star==star)??throw new ArgumentOutOfRangeException(nameof(star));
        public static int Cost(string id)=>PlayerFor(id).cost;
        public static float StatValue(string id,int star,string key)
        {
            var field=typeof(Star).GetField(key,Fields);if(field==null)throw new ArgumentException("Unknown v2 stat: "+key);
            object value=field.GetValue(Row(id,star));return Numeric(value,key);
        }
        static float Numeric(object value,string key)
        {
            if(value is bool b)return b?1:0;
            if(value is int i)return i;
            if(value is float f)return f;
            throw new ArgumentException("V2 value is not numeric: "+key);
        }
        public static bool TrySkillValue(string id,int star,string skillId,string key,out float value)
        {
            var u=PlayerFor(id);_=Row(id,star);
            var p=u.skills.FirstOrDefault(s=>s.id==skillId)?.parameters.FirstOrDefault(s=>s.key==key);
            value=p==null?0:p.Value(star);return p!=null;
        }
        public static float SkillValue(string id,int star,string skillId,string key)
        {
            if(!TrySkillValue(id,star,skillId,key,out float value))throw new ArgumentException("Unmapped adopted v2 skill: "+id+"/"+skillId+"/"+key);
            return value;
        }
        static Parameter SynergyParameter(string id,int tier,string key)
        {
            Load();if(!synergies.TryGetValue(id,out var s))throw new ArgumentException("Unknown v2 synergy: "+id);
            var p=s.tiers.SingleOrDefault(t=>t.count==tier)?.parameters.SingleOrDefault(v=>v.key==key);
            return p??throw new ArgumentException("Unknown adopted v2 synergy parameter: "+id+"/"+tier+"/"+key);
        }
        public static bool TrySynergyValue(string id,int tier,string key,out float value)
        {
            var p=SynergyParameter(id,tier,key);value=p.number;return p.numeric;
        }
        public static float SynergyValue(string id,int tier,string key)
        {
            if(!TrySynergyValue(id,tier,key,out float value))throw new ArgumentException("V2 synergy parameter is textual/list-valued: "+id+"/"+key);
            return value;
        }
        public static float[] SynergyList(string id,int tier,string key)
        {var p=SynergyParameter(id,tier,key);return p.list==null?throw new ArgumentException("Not a v2 list: "+key):(float[])p.list.Clone();}
        public static string SynergyText(string id,int tier,string key)=>SynergyParameter(id,tier,key).text??throw new ArgumentException("Not a v2 text: "+key);
        // Stage zero selects the summon-wide common fields.
        public static float SummonValue(int stage,string key)
        {
            Load();object record=stage==0?(object)golem.common:golem.stages.SingleOrDefault(s=>s.stage==stage);
            if(record==null)throw new ArgumentOutOfRangeException(nameof(stage));
            var field=record.GetType().GetField(key,Fields);
            return field==null?throw new ArgumentException("Unknown adopted v2 summon field: "+key):Numeric(field.GetValue(record),key);
        }
        public static int StarFor(ManualBattleSettings settings)
        {
            var u=PlayerFor(settings.CardId);int star=settings.DeploymentCost-u.stars[0].dp+1;
            _=Row(settings.CardId,star);return star;
        }

        public static BasicRosterProfile Build(ManualBattleDefinition data,int star,int dp,int defense)
        {
            if(data==null||data.Engagement==null)throw new ArgumentNullException(nameof(data));
            var original=data.Snapshot();var c=data.Engagement.Snapshot();
            // The explicit deferred exception keeps its current battle implementation.
            if(original.CardId==SilverWolfCombat.CardId)return CampaignBalance.Build(data,star,dp,defense);
            var r=Row(original.CardId,star);string id=original.CardId;var e=c.Effects;
            float Skill(string name,string key)=>SkillValue(id,star,name,key);
            float blast=id=="CARD_M04"?Skill("deathBlast","damage"):id=="CARD_M06"?Skill("coldBlast","damage"):e.BlastAttack;
            float radius=id=="CARD_M04"?Skill("deathBlast","radius"):id=="CARD_M06"?Skill("coldBlast","radius"):e.BlastRadius;
            float delay=id=="CARD_M04"?Skill("deathBlast","delay"):id=="CARD_M06"?Skill("coldBlast","delay"):e.BlastDelay;
            var effects=new MonsterEffectSettings(damageType:r.damageType=="arts"?CombatDamageType.Arts:CombatDamageType.Physical,
                resistance:e.Resistance,armorShred:id=="CARD_M05"?Skill("armorShred","armorReductionPerHit"):e.ArmorShred,
                blastAttack:blast,blastRadius:radius,blastDelay:delay,coldDuration:id=="CARD_M06"?Skill("coldBlast","coldDuration"):e.ColdDuration,
                frozenAttackMultiplier:id=="CARD_M11"?Skill("frozenBonus","frozenMultiplier"):id=="CARD_M13"?Skill("frozenExecution","frozenMultiplier"):e.FrozenAttackMultiplier,
                coldAttackEvery:id=="CARD_M12"?(int)Skill("thirdCold","attacksRequired"):e.ColdAttackEvery,
                attackColdDuration:id=="CARD_M12"?Skill("thirdCold","coldDuration"):e.AttackColdDuration,
                frostNova:e.FrostNova,regenerationFraction:id=="CARD_M21"||id=="CARD_M22"||id=="CARD_M23"?Skill("regeneration","regenerationFraction"):e.RegenerationFraction,
                attackTargets:id=="CARD_M24"?(int)Skill("threeTargetDrain","targets"):e.AttackTargets,
                lifeSteal:id=="CARD_M24"?Skill("threeTargetDrain","lifeSteal"):e.LifeSteal,
                healer:e.Healer,artsBarrier:e.ArtsBarrier,unitResistance:r.resistance,mudrock:e.Mudrock,phalanx:e.Phalanx,flamechaser:e.Flamechaser,
                flamerazer:e.Flamerazer,mandragora:e.Mandragora,nervousCreeper:e.NervousCreeper,harvester:e.Harvester,firstSpeaker:e.FirstSpeaker,shackler:e.Shackler,
                tacticalCommander:e.TacticalCommander,orderRunner:e.OrderRunner,orderSniper:e.OrderSniper,tauntLevel:e.TauntLevel,patriot:e.Patriot,
                talulahSecondPhase:id==TalulahTiming.CardId&&Skill("threeStarPhase","enabled")>0);
            string manual=id=="CARD_M14"?"frostSeal":id=="CARD_M28"?"capture":id==TalulahTiming.CardId?"manualRain":id==ViviCombat.CardId?"skillShell":null;
            float initial=c.InitialSP,required=c.RequiredSP,recharge=c.SPPerSecond,skillAttack=c.SkillAttack,skillInterval=c.SkillInterval,skillRange=c.SkillRange,projectile=r.projectileTime;
            int ammo=c.SkillAmmo;
            if(manual!=null)
            {
                initial=Skill(manual,"initialSp");required=Skill(manual,"requiredSp");recharge=Skill(manual,"spPerSecond");
                skillInterval=Skill(manual,manual=="skillShell"?"shotInterval":"actionDuration");
                skillRange=Skill(manual,manual=="skillShell"?"skillRange":manual=="manualRain"?"targetRange":"range");
                skillAttack=manual=="capture"?0:Skill(manual,manual=="frostSeal"?"damage":manual=="manualRain"?"damagePerWavePerTarget":"damagePerShell");
                ammo=manual=="manualRain"?(int)Skill(manual,"waves"):manual=="skillShell"?(int)Skill(manual,"ammo"):0;
            }
            // Capture has no damage, but EngagementSettings' existing positive
            // skill-attack placeholder is not a damage source for that action.
            if(c.MudrockCapture&&skillAttack==0)skillAttack=1;
            var combat=new EngagementSettings(r.attack,r.armor,r.range,r.interval,c.DefenderHealth,c.DefenderAttack,c.DefenderArmor,c.DefenderRange,c.DefenderInterval,c.BlockCount,c.BlockRadius,
                initial,required,recharge,ammo,skillAttack,skillInterval,skillRange,projectile,c.FireRain,c.HasSkill,c.Melee,r.attackDuration,r.effectiveAttackWindup,effects,r.canOrdinaryAttack,
                c.DefenderDamageType,c.RevealsInvisible,c.DefenderCanHitAir,c.FrostSeal,c.MudrockCapture);
            var settings=new ManualBattleSettings(id,original.UnitId,dp,defense,r.uses,r.dp,r.nonWaveCooldown,r.hp,r.speed,r.gate,original.TowerRange,original.TowerDamage,original.ShotInterval);
            return new BasicRosterProfile(settings,combat,true);
        }
    }
}
