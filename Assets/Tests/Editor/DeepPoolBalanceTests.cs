using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class DeepPoolBalanceTests
    {
        static readonly string[] Deep={"CARD_M29","CARD_M30","CARD_M31","CARD_M32"};
        static readonly Vector3[] Route={Vector3.zero,Vector3.right*100};
        static readonly MethodInfo Damage=typeof(ManualBattle).GetMethod("DamageDefender",BindingFlags.Instance|BindingFlags.NonPublic);
        readonly List<string> balanceRows=new List<string>();
        static float Q(int star)=>1+.25f*(star-1);
        static ShopTrialModel.Piece Piece(string id,int star=1)=>new ShopTrialModel.Piece{Id=id,Star=star};
        static DefenderSettings Guard(string id,float x,float hp=10000,float armor=0,bool pressure=false)=>
            new DefenderSettings(id,id,null,Vector3.right*x,hp,pressure?20:1,armor,1.2f,2,2,canAttack:pressure);
        static ManualBattleSettings QuietUnit(string id)=>new ManualBattleSettings(id,id,1000,100,20,1,.01f,10000,.01f,1,1,1,1);
        static EngagementSettings QuietCombat(float resistance=0)=>new EngagementSettings(unitAttack:1,unitRange:.01f,unitInterval:100,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,
            effects:new MonsterEffectSettings(resistance:resistance));
        // Quiet fixtures exercise actual damage/state entry points without incidental attacks or forced status writes.
        static ManualBattle Quiet(int tier=2,int star=1,float resistance=0,DefenderSettings[] guards=null)
        {
            var combat=QuietCombat(resistance);guards=guards??new[]{Guard("a",20),Guard("b",20.5f),Guard("c",21)};
            var b=new ManualBattle(QuietUnit(Deep[0]),Route,guards[0].Position,combat,roster:guards);
            b.ConfigureCardLoadout(Deep.Skip(1).Concat(new[]{"CARD_M07"}).Select(id=>new BasicRosterProfile(QuietUnit(id),combat,true)));
            b.ConfigureSynergies(Deep.Take(tier).Select(id=>Piece(id,star)));b.Begin();return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,string card="CARD_M29",long request=1)
        {
            var r=b.TryDeployRole(b.RunId,request,b.Entry,card);Assert.That(r.Success,Is.True,r.Failure.ToString());return b.FindUnit(r.EntityId);
        }
        static void Hit(ManualBattle b,DefenderState d,float attack,DeployedUnitState source,bool derived=false)=>
            Damage.Invoke(b,new object[]{d,attack,CombatDamageType.Arts,derived,source,1f});
        static void Advance(ManualBattle b,float seconds,float slice=.05f)
        {
            double end=b.Clock+seconds;
            while(b.Clock+1e-7<end&&b.Phase==BattlePhase.Running)b.Advance((float)Math.Min(slice,end-b.Clock));
        }

        [TestCase(2,1,0)][TestCase(2,2,0)][TestCase(2,3,50)]
        [TestCase(4,1,0)][TestCase(4,2,50)][TestCase(4,3,0)]
        public void FourSecondBudgetIsIndependentOfFrameSlicesAndDoesNotRefreshItself(int tier,int star,float resistance)
        {
            float power=(tier==2?15:25)*Q(star),expected=power*4*(1-resistance/100);
            var totals=new List<float>();
            foreach(float slice in new[]{.01f,.05f,.37f,4.05f})
            {
                var b=Quiet(tier,star,resistance);var source=Deploy(b);Hit(b,b.Defender,1,source);
                Assert.That(b.DeepBurnPower(b.Defender.EntityId),Is.EqualTo(power).Within(.001));
                Assert.That(b.DeepBurnRemaining(b.Defender.EntityId),Is.EqualTo(4).Within(.001));
                float hp=b.Defender.Health;Advance(b,4.05f,slice);float loss=hp-b.Defender.Health;totals.Add(loss);
                Assert.That(loss,Is.EqualTo(expected).Within(power*.051f+.08f));
                Assert.That(b.DeepBurnRemaining(b.Defender.EntityId),Is.Zero);
                hp=b.Defender.Health;Advance(b,3);Assert.That(b.Defender.Health,Is.EqualTo(hp));
                Assert.That(b.DeepCharges,Is.Zero,"A living hard target still receives the full damage budget without kill rewards.");
            }
            Assert.That(totals.Max()-totals.Min(),Is.LessThanOrEqualTo(power*.051f+.1f));
        }

        [Test]public void RepeatedHitsRefreshOneBurnAndPauseFreezesDamageAndExpiry()
        {
            var b=Quiet();var a=Deploy(b);Hit(b,b.Defender,1,a);Advance(b,3);
            b.Advance(.02f);var second=Deploy(b,request:2);Hit(b,b.Defender,1,second);
            Assert.That(b.DeepBurnPower(b.Defender.EntityId),Is.EqualTo(15));
            float hp=b.Defender.Health;Advance(b,1);Assert.That(hp-b.Defender.Health,Is.EqualTo(15).Within(.2));
            double remaining=b.DeepBurnRemaining(b.Defender.EntityId);hp=b.Defender.Health;b.SetPaused(true);b.Advance(100);
            Assert.That(b.DeepBurnRemaining(b.Defender.EntityId),Is.EqualTo(remaining));Assert.That(b.Defender.Health,Is.EqualTo(hp));
            b.SetPaused(false);Advance(b,3.1f);Assert.That(b.DeepBurnRemaining(b.Defender.EntityId),Is.Zero);
            b.Restart();Assert.That(b.DeepSeeds,Is.Empty);Assert.That(b.DeepBurnRemaining(b.Defender.EntityId),Is.Zero);
        }

        [Test]public void ExhaustedArmyWaitsForTheLastBurnToFinishAndPauseFreezesThatWait()
        {
            var settings=new ManualBattleSettings("CARD_M29","M29",1,1,1,1,.01f,100,.01f,1,1,1,1);
            var guard=Guard("last-target",20,1000);
            var b=new ManualBattle(settings,Route,guard.Position,QuietCombat(),roster:new[]{guard});
            // M30 contributes to the battle snapshot but is not an available deployment card.
            b.ConfigureSynergies(Deep.Take(2).Select(id=>Piece(id)));b.Begin();var source=Deploy(b);
            Assert.That(b.DP,Is.Zero);Assert.That(b.RemainingUses,Is.Zero);
            Hit(b,b.Defender,1,source);float hp=b.Defender.Health;
            Assert.That(b.TryDamageUnit(source.EntityId,1000,CombatDamageType.Arts),Is.True);Assert.That(source.Active,Is.False);
            Advance(b,.5f);Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));
            Assert.That(hp-b.Defender.Health,Is.EqualTo(7.5f).Within(.05));
            double clock=b.Clock,remaining=b.DeepBurnRemaining(b.Defender.EntityId);float pausedHP=b.Defender.Health;
            b.SetPaused(true);b.Advance(100);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.Defender.Health,Is.EqualTo(pausedHP));
            Assert.That(b.DeepBurnRemaining(b.Defender.EntityId),Is.EqualTo(remaining));Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));
            b.SetPaused(false);Advance(b,3.6f);Assert.That(b.DeepBurnRemaining(b.Defender.EntityId),Is.Zero);
            Assert.That(hp-b.Defender.Health,Is.EqualTo(60).Within(.1));Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost));
        }

        [Test]public void DerivedAndNonDeepDamageDoNotIgniteAndOneKindDoesNotActivateTheTrait()
        {
            var b=Quiet();var a=Deploy(b);var outsider=Deploy(b,"CARD_M07",2);
            Hit(b,b.Defender,10,outsider);Hit(b,b.Defender,10,a,true);
            Assert.That(b.DeepBurnRemaining(b.Defender.EntityId),Is.Zero);
            b.Restart();b.ConfigureSynergies(new[]{Piece("CARD_M29"),Piece("CARD_M29")});b.Begin();
            a=Deploy(b);Hit(b,b.Defender,10,a);Assert.That(b.DeepTier,Is.Zero);Assert.That(b.DeepBurnRemaining(b.Defender.EntityId),Is.Zero);
        }

        [Test]public void MixedStarsUseTheFormationAverageRatherThanTheAttackingEntityStar()
        {
            var b=Quiet(4);b.Restart();b.ConfigureSynergies(new[]{Piece(Deep[0],3),Piece(Deep[1]),Piece(Deep[2]),Piece(Deep[3])});b.Begin();
            Hit(b,b.Defender,1,Deploy(b));Assert.That(b.DeepBurnPower(b.Defender.EntityId),Is.EqualTo(25*1.125f).Within(.001));
        }

        [TestCase(2,4)][TestCase(4,6)]
        public void FirstLethalHitCreatesOneSeedAndSpentSeedsStillCountTowardTheBattleCap(int tier,int cap)
        {
            var guards=Enumerable.Range(0,9).Select(i=>Guard("fragile-"+i,20+5*i,1)).Concat(new[]{Guard("survivor",20.8f)}).ToArray();
            var b=Quiet(tier,guards:guards);var a=Deploy(b);
            for(int i=0;i<9;i++)
            {
                Hit(b,b.Defenders[i],1,a);Hit(b,b.Defenders[i],1,a);
                if(tier==4&&i==0)Assert.That(b.TryDetonateDeep(b.RunId,100),Is.True);
            }
            Assert.That(b.DeepSeeds.Count,Is.EqualTo(cap));Assert.That(b.DeepCharges,Is.EqualTo(cap-(tier==4?1:0)));
        }

        [Test]public void SpreadBurnCanCreateTheNextSeedButCannotRefreshItselfIndefinitely()
        {
            var b=Quiet(guards:new[]{Guard("first",20,1),Guard("second",21.5f,45),Guard("safe",24,10000)});var a=Deploy(b);
            Hit(b,b.Defenders[0],1,a);Assert.That(b.DeepCharges,Is.EqualTo(1));
            Assert.That(b.DeepBurnPower(b.Defenders[1].EntityId),Is.EqualTo(15));Advance(b,3.2f);
            Assert.That(b.Defenders[1].Alive,Is.False);Assert.That(b.DeepCharges,Is.EqualTo(2));Advance(b,5);
            Assert.That(b.DeepBurnRemaining(b.Defenders[2].EntityId),Is.Zero);
        }

        [TestCase(1)][TestCase(2)][TestCase(3)]
        public void DetonationUsesOneSeedHasAnExactBudgetAndKeepsPauseAndRequestIdempotence(int star)
        {
            var b=Quiet(4,star);var a=Deploy(b);
            Hit(b,b.Defenders[0],20000,a);Hit(b,b.Defenders[1],20000,a);Assert.That(b.DeepCharges,Is.EqualTo(2));
            b.SetPaused(true);Assert.That(b.TryDetonateDeep(b.RunId,10),Is.False);Assert.That(b.DeepCharges,Is.EqualTo(2));b.SetPaused(false);
            float hp=b.Defenders[2].Health;Assert.That(b.TryDetonateDeep(b.RunId,10),Is.True);
            Assert.That(hp-b.Defenders[2].Health,Is.EqualTo(180*Q(star)).Within(.01));Assert.That(b.DeepCharges,Is.EqualTo(1));
            hp=b.Defenders[2].Health;Assert.That(b.TryDetonateDeep(b.RunId,10),Is.True);Assert.That(b.Defenders[2].Health,Is.EqualTo(hp));
            Assert.That(b.DeepCharges,Is.EqualTo(1));Assert.That(b.DeepDetonations,Is.EqualTo(1));
            Assert.That(b.TryDetonateDeep(b.RunId-1,11),Is.False);Assert.That(b.TryDetonateDeep(b.RunId,11),Is.True);
            Assert.That(b.TryDetonateDeep(b.RunId,12),Is.False);Assert.That(b.DeepDetonations,Is.EqualTo(2));
        }

        [Test]public void DetonationSkipsAnOldEmptySeedAndNeverReturnsSeedsFromItsOwnKills()
        {
            var b=Quiet(4,guards:new[]{Guard("old",20,1),Guard("new",30,1),Guard("victim",30.8f,150)});var a=Deploy(b);
            Hit(b,b.Defenders[0],1,a);Hit(b,b.Defenders[1],1,a);
            Assert.That(b.TryDetonateDeep(b.RunId,1),Is.True);
            Assert.That(b.DeepSeeds[0].Spent,Is.False);Assert.That(b.DeepSeeds[1].Spent,Is.True);
            Assert.That(b.Defenders[2].Alive,Is.False);Assert.That(b.DeepSeeds.Count,Is.EqualTo(2));Assert.That(b.DeepCharges,Is.EqualTo(1));
            Assert.That(b.TryDetonateDeep(b.RunId,2),Is.False,"The remaining old seed has no living target and must not be spent.");
            Assert.That(b.DeepCharges,Is.EqualTo(1));
        }

        [Test]public void ASeedHitsAtMostFourDistinctLivingTargets()
        {
            var guards=new[]{Guard("seed",20,1)}.Concat(Enumerable.Range(0,5).Select(i=>Guard("target-"+i,20.2f+i*.2f))).ToArray();
            var b=Quiet(4,guards:guards);Hit(b,b.Defender,1,Deploy(b));var hp=b.Defenders.Skip(1).Select(d=>d.Health).ToArray();
            Assert.That(b.TryDetonateDeep(b.RunId,1),Is.True);
            for(int i=0;i<5;i++)Assert.That(hp[i]-b.Defenders[i+1].Health,Is.EqualTo(i<4?180:0).Within(.01));
        }

        static BasicRosterProfile Authored(string id,int star)
        {
            var data=Resources.Load<ManualBattleDefinition>("MonsterTests/"+id.Substring(5));Assert.That(data,Is.Not.Null,id);
            return CampaignBalance.Build(data,star,1000,100);
        }
        static ManualBattle Natural(string[] ids,int star,bool synergy,DefenderSettings[] guards,bool deployAll=true)
        {
            var profiles=ids.Select(id=>Authored(id,star)).ToArray();
            var b=new ManualBattle(profiles[0].Unit,Route,guards[0].Position,profiles[0].Combat,roster:guards);
            b.ConfigureCardLoadout(profiles.Skip(1));b.ConfigureSynergies(synergy?ids.Select(id=>Piece(id,star)):Enumerable.Empty<ShopTrialModel.Piece>());b.Begin();
            long request=0;foreach(var id in deployAll?ids:ids.Take(1))Deploy(b,id,++request);return b;
        }
        sealed class Reading
        {
            internal double Elapsed,FirstDamage=-1,FirstBurn=-1,FirstSeed=-1;
            internal float Damage,EarlyDamage=-1;internal int Kills,Survivors,Seeds,Detonations;
        }
        static Reading Observe(ManualBattle b,float limit=90,bool detonate=false)
        {
            var result=new Reading();float initial=b.Defenders.Sum(d=>d.Health);
            while(b.Clock<limit&&b.Phase==BattlePhase.Running&&b.Defenders.Any(d=>d.Alive))
            {
                b.Advance(.05f);float loss=initial-b.Defenders.Sum(d=>d.Health);
                if(result.EarlyDamage<0&&b.Clock>=2)result.EarlyDamage=loss;
                if(result.FirstDamage<0&&loss>0)result.FirstDamage=b.Clock;
                if(result.FirstBurn<0&&b.Defenders.Any(d=>b.DeepBurnRemaining(d.EntityId)>0))result.FirstBurn=b.Clock;
                if(result.FirstSeed<0&&b.DeepSeeds.Count>0)result.FirstSeed=b.Clock;
                if(detonate&&b.DeepCharges>0)b.TryDetonateDeep(b.RunId,100000+b.DeepDetonations);
            }
            result.Elapsed=b.Clock;result.Damage=initial-b.Defenders.Sum(d=>d.Health);result.Kills=b.Defenders.Count(d=>!d.Alive);
            result.Survivors=b.Units.Count(u=>u.Active);result.Seeds=b.DeepSeeds.Count;result.Detonations=b.DeepDetonations;return result;
        }
        static string Number(double n)=>n.ToString("0.####",CultureInfo.InvariantCulture);
        void Record(string scenario,int star,bool enabled,Reading r)
        {
            string row=string.Join(",",scenario,star,enabled?1:0,Number(r.Elapsed),Number(r.FirstDamage),Number(r.FirstBurn),Number(r.FirstSeed),
                Number(r.Damage),r.Kills,r.Survivors,r.Seeds,r.Detonations,Number(r.EarlyDamage));
            balanceRows.Add(row);TestContext.WriteLine("DEEP_BALANCE "+row);
        }
        [OneTimeTearDown]public void SaveActualNaturalBattleReadings()
        {
            if(balanceRows.Count==0)return;
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"..","artifacts","deep-pool","balance.csv"));Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path,new[]{"scenario,star,synergy,elapsed,first_damage,first_burn,first_seed,damage,kills,survivors,seeds,detonations,damage_at_2s"}.Concat(balanceRows),new UTF8Encoding(false));
            TestContext.WriteLine("Natural deep-pool balance readings: "+path);
        }

        [TestCase(1,false)][TestCase(2,false)][TestCase(3,false)]
        [TestCase(1,true)][TestCase(2,true)][TestCase(3,true)]
        public void TwoAuthoredMembersWithoutReedsNaturallyStartAndImproveOpenAndHardTargets(int star,bool hard)
        {
            var ids=Deep.Take(2).ToArray();var guards=new[]{Guard("hard-target",1,hard?1200:400,hard?18:4,true)};
            var off=Observe(Natural(ids,star,false,guards));var on=Observe(Natural(ids,star,true,guards));
            string name=hard?"pair_hard":"pair_open";Record(name,star,false,off);Record(name,star,true,on);
            Assert.That(on.FirstBurn,Is.GreaterThanOrEqualTo(on.FirstDamage).And.LessThanOrEqualTo(on.FirstDamage+.051));
            Assert.That(on.FirstBurn,Is.LessThan(4),"The two-member trait starts at the first actual hit without reeds, a kill, or M31.");
            Assert.That(on.Kills,Is.EqualTo(1));Assert.That(off.Kills,Is.EqualTo(1));
            if(hard)Assert.That(on.Elapsed,Is.LessThan(off.Elapsed));else Assert.That(on.Elapsed,Is.LessThanOrEqualTo(off.Elapsed));
            Assert.That(on.EarlyDamage,Is.GreaterThan(off.EarlyDamage),"An extra DOT must deal damage before a kill, even when a soft target still dies on the same discrete attack.");
            Assert.That(on.FirstSeed,Is.GreaterThan(on.FirstBurn),"This is an initially living hard target, so its early benefit cannot be a kill reward.");
            TestContext.WriteLine("TTK reduction "+name+" star="+star+": "+Number(100*(1-on.Elapsed/off.Elapsed))+"%");
        }

        [TestCase("CARD_M31")][TestCase("CARD_M32")]
        public void AuthoredSprayAndGazeStartTheBurnWithoutAnOrdinaryAttackOrForcedBurn(string card)
        {
            var ids=new[]{card,"CARD_M29"};var guard=new DefenderSettings("near","near",null,Vector3.right*2,10000,1,0,1,2,0,canAttack:false);
            var b=Natural(ids,1,true,new[]{guard},false);var source=b.Units.Single();
            // Only the native caster is deployed; M29 exists exclusively in the pre-battle formation snapshot.
            while(b.Clock<12&&(card=="CARD_M31"?(source.Spray==null||source.Spray.Ticks==0):(source.Gaze==null||source.Gaze.Ticks==0)))b.Advance(.05f);
            if(card=="CARD_M31")Assert.That(source.Spray.Ticks,Is.GreaterThan(0));else Assert.That(source.Gaze.Ticks,Is.GreaterThan(0));
            Assert.That(source.OrdinaryAttacksFired,Is.Zero);Assert.That(b.DeepBurnPower(b.Defender.EntityId),Is.EqualTo(15));
            Assert.That(b.DeepBurnRemaining(b.Defender.EntityId),Is.GreaterThan(3.6));
        }

        [TestCase(false)][TestCase(true)]
        public void FourAuthoredMembersRemainUsefulInSparseAndDenseDefenses(bool dense)
        {
            var guards=Enumerable.Range(0,6).Select(i=>Guard("target-"+i,4+i*(dense?.6f:4),400,4)).ToArray();
            var off=Observe(Natural(Deep,1,false,guards),120);var on=Observe(Natural(Deep,1,true,guards),120,true);
            Record(dense?"four_dense":"four_sparse",1,false,off);Record(dense?"four_dense":"four_sparse",1,true,on);
            Assert.That(on.FirstBurn,Is.LessThan(10));Assert.That(on.Kills,Is.GreaterThanOrEqualTo(off.Kills));Assert.That(on.Seeds,Is.LessThanOrEqualTo(6));
            Assert.That(on.Detonations,Is.LessThanOrEqualTo(2));if(on.Kills==6&&off.Kills==6)Assert.That(on.Elapsed,Is.LessThan(off.Elapsed));
        }
    }
}
