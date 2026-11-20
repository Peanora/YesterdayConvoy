using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class CampaignV2BalanceTests
    {
        [Serializable] sealed class Expected {public Unit[] units;}
        [Serializable] sealed class Unit {public string id;public int cost;public Row[] stars;}
        [Serializable] sealed class Row
        {
            public int star,dp,uses,gate;
            public float hp,attack,armor,resistance,interval,range,speed,nonWaveCooldown,attackDuration,effectiveAttackWindup,projectileTime;
            public bool canOrdinaryAttack;
        }
        static ManualBattleDefinition Definition(string id)
        {
            string path=id=="CARD_TALULAH_TRIAL"?"Assets/Data/Battles/TalulahTrial.asset":id=="CARD_WISADEL_APRIL"?"Assets/Data/Battles/ManualDemo.asset":null;
            var data=path!=null?AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(path):Resources.Load<ManualBattleDefinition>("MonsterTests/"+(id=="CARD_ORIGINIUM_SLUG"?"OriginiumSlug":id.Substring(5)));
            Assert.That(data,Is.Not.Null,id);Assert.That(data.Card.CardId,Is.EqualTo(id));return data;
        }
        static ManualBattle Battle(BasicRosterProfile profile,bool v2=true)
        {
            var b=new ManualBattle(profile.Unit,new[]{Vector3.zero,Vector3.right*100},Vector3.forward*20,profile.Combat,
                roster:new[]{new DefenderSettings("guard","guard",null,Vector3.forward*20,100000,1,0,.01f,100,canAttack:false)});
            if(v2)b.ConfigureCampaignV2(CampaignJourneyDefinition.StageCount);return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,long request=1)
        {if(b.Phase==BattlePhase.Ready)b.Begin();var r=b.TryDeploy(b.RunId,request,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}

        [Test]public void BoundaryFixturesCompareFiniteDualBossAndSingleCoreInvestmentsAcrossTheDifficultyCurve()
        {
            var method=typeof(YesterdayConvoy.Editor.CampaignJourneySimulationTools).GetMethod("BuildBoundaryFixtures",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.That(method,Is.Not.Null);
            var fixtures=((Array)method.Invoke(null,null)).Cast<object>().ToArray();
            T Field<T>(object f,string name)=>(T)f.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(f);
            var group=fixtures.Where(f=>Field<string>(f,"InvestmentGroup")=="late-eight-58-gold").ToArray();
            Assert.That(group.Select(f=>Field<string>(f,"InvestmentRole")),Is.EquivalentTo(new[]{"dual-boss","vivi-core","talulah-core","no-five-cost"}));
            foreach(var fixture in group)
            {
                string role=Field<string>(fixture,"InvestmentRole");var ids=Field<string[]>(fixture,"Ids");var stars=Field<int[]>(fixture,"Stars");
                Assert.That(ids.Length,Is.EqualTo(8),role);Assert.That(ids.Distinct().Count(),Is.EqualTo(8),role);
                Assert.That(stars.Length,Is.EqualTo(ids.Length),role);Assert.That(stars,Is.All.InRange(1,2),role);
                Assert.That(Field<int>(fixture,"Level"),Is.EqualTo(8));Assert.That(Field<int[]>(fixture,"Stages"),Is.EqualTo(new[]{9,10,12}));
                Assert.That(ids.Select((id,i)=>CampaignV2Balance.Cost(id)*ShopTrialModel.Copies(stars[i])).Sum(),Is.EqualTo(58),role);
                var shop=new ShopTrialModel(ids.Select(id=>new ShopTrialModel.Role{Id=id,Name=id,Cost=CampaignV2Balance.Cost(id)}),44);
                shop.EnableJourneyEconomy();shop.Preset(ids.Select((id,i)=>new ShopTrialModel.Piece{Id=id,Star=stars[i]}),Array.Empty<string>(),8,0,0);
                Assert.That(shop.SetLineup(ids,shop.Bench.Select(p=>p.Instance)),Is.True,role);shop.Validate();
                foreach(string boss in new[]{"CARD_TALULAH_TRIAL","CARD_WISADEL_APRIL"})
                {
                    bool required=role=="dual-boss"||role==(boss=="CARD_WISADEL_APRIL"?"vivi-core":"talulah-core");
                    Assert.That(ids.Contains(boss),Is.EqualTo(required),role+" "+boss);
                    if(required)Assert.That(stars[Array.IndexOf(ids,boss)],Is.EqualTo(2),role+" "+boss);
                }
                if(role=="no-five-cost")Assert.That(ids.Select(CampaignV2Balance.Cost),Is.All.LessThan(5));
                if(role=="dual-boss")Assert.That(ids.Count(id=>CampaignBalance.Tags(id).Contains("整合运动")),Is.LessThan(8),"The reported mixed Boss lineup must not silently become Reunion 8.");
            }
            var covered=fixtures.Where(f=>!string.IsNullOrEmpty(Field<string>(f,"MatchGroup"))).SelectMany(f=>Field<int[]>(f,"Stages")).Distinct();
            Assert.That(covered,Is.EquivalentTo(new[]{2,3,5,6,7,9,10,12}));
        }

        [TestCase(1,1500f)][TestCase(2,3000f)][TestCase(3,7500f)]
        public void ViviJourneyHealthUsesTheRetunedStarBudgetWithoutChangingLegacyTrials(int star,float health)
        {
            var source=Definition("CARD_WISADEL_APRIL");string before=EditorJsonUtility.ToJson(source);
            Assert.That(CampaignV2Balance.Build(source,star,50,6,waveMode:true).Unit.Health,Is.EqualTo(health));
            Assert.That(CampaignBalance.Build(source,1,50,6).Unit.Health,Is.EqualTo(680));
            Assert.That(EditorJsonUtility.ToJson(source),Is.EqualTo(before));
        }

        [Test]public void RuntimeSnapshotsMatchTheAdoptedSourcesAndCoverAllRowsAndActions()
        {
            foreach(string file in new[]{"player-roster","synergies"})
                Assert.That(Resources.Load<TextAsset>("CampaignV2/"+file).text,Is.EqualTo(File.ReadAllText("docs/balance/v2/inputs/"+file+".json")));
            Assert.That(CampaignV2Balance.PlayerCount,Is.EqualTo(43));Assert.That(CampaignV2Balance.StarRowCount,Is.EqualTo(138));Assert.That(CampaignV2Balance.SkillCount,Is.EqualTo(92));
        }

        [Test]public void ManualSkillsUseFinalPerStarBudgetsAndLegacyDefinitionsStayIndependent()
        {
            var talulah=Definition("CARD_TALULAH_TRIAL");var current=CampaignBalance.Build(talulah,3,50,6);var next=CampaignV2Balance.Build(talulah,3,50,6);
            Assert.That(next.Combat.SkillAttack,Is.EqualTo(450));Assert.That(next.Combat.InitialSP,Is.EqualTo(24));Assert.That(next.Combat.RequiredSP,Is.EqualTo(36));
            Assert.That(current.Combat.SkillAttack,Is.EqualTo(240));Assert.That(CampaignBalance.Build(talulah,3,50,6).Combat.SkillAttack,Is.EqualTo(240));
            Assert.That(CampaignV2Balance.Build(Definition("CARD_M14"),2,50,6).Combat.SkillAttack,Is.EqualTo(560));
            Assert.That(CampaignV2Balance.Build(Definition("CARD_WISADEL_APRIL"),2,50,6).Combat.SkillAttack,Is.EqualTo(520));
            Assert.That(CampaignV2Balance.Build(Definition("CARD_M28"),2,50,6).Combat.RequiredSP,Is.EqualTo(35));
        }

        [Test]public void DeploymentAppliesExactBarrierLayerAndHpBudgetsAndRestartKeepsV2Enabled()
        {
            var profile=CampaignV2Balance.Build(Definition("CARD_M28"),2,50,6);var b=Battle(profile);var u=Deploy(b);
            Assert.That(u.MaxHealth,Is.EqualTo(4200));Assert.That(u.ArtsBarrierRemaining,Is.EqualTo(1026.666667f).Within(.01f));
            Assert.That(u.ArtsBarrierLayerSize,Is.EqualTo(342.222222f).Within(.01f));Assert.That(u.ArtsBarrierLayers,Is.EqualTo(3));
            b.SetPaused(true);b.Advance(30);Assert.That(b.Clock,Is.Zero);b.Restart();Assert.That(b.CampaignV2Enabled,Is.True);
            var second=Deploy(b);Assert.That(second.MaxHealth,Is.EqualTo(4200));Assert.That(second.ArtsBarrierRemaining,Is.EqualTo(u.ArtsBarrierRemaining));
        }

        [Test]public void TacticalCommandUsesItsActualStarAndOrdinaryTrialsKeepTheirPreviousAura()
        {
            var v2=Battle(CampaignV2Balance.Build(Definition("CARD_M17"),3,50,6));Deploy(v2);
            Assert.That(v2.TacticalAttackMultiplier,Is.EqualTo(1.14f).Within(.0001f));Assert.That(v2.TacticalArmorBonus,Is.EqualTo(2));
            var old=Battle(CampaignBalance.Build(Definition("CARD_M17"),3,50,6),false);Deploy(old);
            Assert.That(old.CampaignV2Enabled,Is.False);Assert.That(old.TacticalAttackMultiplier,Is.EqualTo(1.1f).Within(.0001f));Assert.That(old.TacticalArmorBonus,Is.EqualTo(1));
        }

        [Test]public void StrictParametersDistinguishMissingKeysNumbersListsAndPermanentText()
        {
            Assert.Throws<ArgumentException>(()=>CampaignV2Balance.SkillValue("CARD_M07",1,"ordinary","missing"));
            Assert.Throws<ArgumentException>(()=>CampaignV2Balance.SynergyValue("sarkaz",2,"shieldDurationSeconds"));
            Assert.That(CampaignV2Balance.SynergyText("sarkaz",2,"shieldDurationSeconds"),Is.EqualTo("until_consumed_or_final_removal"));
            var pulses=CampaignV2Balance.SynergyList("snow",5,"weatherPulseAt");Assert.That(pulses,Is.EqualTo(new float[]{0,2,4,6}));
            pulses[0]=99;Assert.That(CampaignV2Balance.SynergyList("snow",5,"weatherPulseAt")[0],Is.Zero);
            Assert.That(CampaignV2Balance.SummonValue(5,"hp"),Is.EqualTo(5200));Assert.That(CampaignV2Balance.SummonValue(5,"controlImmune"),Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignV2Balance.Build(Definition("CARD_M18"),4,50,6));
        }
    }
}
