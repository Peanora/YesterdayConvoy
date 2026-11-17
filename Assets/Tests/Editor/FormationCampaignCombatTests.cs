using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    /// <summary>Controlled combat fixtures isolate mechanics; these are not natural shop-acquisition runs.</summary>
    public sealed class FormationCampaignCombatTests
    {
        static FormationBattleModel Fixture(out FormationCampaignRun run,params string[] ids)
        {
            var shop=new ShopTrialModel(FormationCampaignRules.AllUnits.Select(p=>new ShopTrialModel.Role{Id=p.CardId,Name=p.Name,Cost=p.Cost}),71);
            shop.EnableFormationEconomy();shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),10,0,80);
            run=new FormationCampaignRun(shop,71);int safety=0;while(run.HasBlockingChoice){Assert.That(++safety,Is.LessThan(100));Assert.That(run.Choose(0).Success,Is.True);}
            shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=FormationCampaignRules.Find(id).CardId,Star=1}),Array.Empty<string>(),10,0,80);
            var model=new FormationBattleModel(71);model.BindCampaign(run);
            Assert.That(model.ApplyRoster(shop.Bench.Select((p,i)=>new FormationRosterEntry{Instance=p.Instance,Star=p.Star,ArtId=p.Id,Spawn=new Vector2((i%4)*3,(i/4)*3)}),10),Is.True);
            return model;
        }
        static FormationDefender Target(FormationBattleModel model)
        {
            model.Defenders.RemoveRange(1,model.Defenders.Count-1);var d=model.Defenders[0];
            d.Position=new Vector2(15,3);d.MaxHealth=100000000;d.Attack=0;d.Range=.01f;d.Armor=0;d.MagicResistance=0;
            return d;
        }
        static void Enter(FormationBattleModel model)
        {foreach(var u in model.Units){u.Position=new Vector2(14.5f,3);u.Health=u.MaxHealth;}}
        static void Advance(FormationBattleModel model,float seconds)
        {for(int i=0;i<Mathf.CeilToInt(seconds/.025f);i++)model.Advance(.025f);}

        [Test]
        public void CampaignUsesApprovedPanelAndNaturalRecoveryInsteadOfSixTemplates()
        {
            var model=Fixture(out var run,"M25","M14");
            Assert.That(model.Units[0].MaxHealth,Is.EqualTo(700));Assert.That(model.Units[0].Attack,Is.EqualTo(90));Assert.That(model.Units[0].SkillRequired,Is.EqualTo(18));
            Target(model);Assert.That(model.StartBattle(),Is.True);
            float charge=model.Units[0].SkillCharge;model.Paused=true;Advance(model,2);Assert.That(model.Units[0].SkillCharge,Is.EqualTo(charge));
            model.Paused=false;Advance(model,2);Assert.That(model.Units[0].SkillCharge,Is.EqualTo(charge+2).Within(.001));
        }
        [TestCase("SLUG")][TestCase("M03")][TestCase("M11")][TestCase("M17")][TestCase("M21")][TestCase("M35")]
        [TestCase("B01")][TestCase("B22")][TestCase("M12")][TestCase("M13")][TestCase("M16")][TestCase("M25")]
        [TestCase("M27")][TestCase("M29")][TestCase("M30")][TestCase("M33")][TestCase("M38")][TestCase("B03")]
        [TestCase("B05")][TestCase("M18")][TestCase("M24")][TestCase("M42")][TestCase("M31")][TestCase("M34")]
        [TestCase("TAL")][TestCase("B09")][TestCase("B14")][TestCase("B23")][TestCase("M14")][TestCase("M28")]
        [TestCase("M32")][TestCase("M36")][TestCase("B04")][TestCase("B07")][TestCase("B15")][TestCase("B24")]
        [TestCase("B25")][TestCase("B26")][TestCase("M20")][TestCase("WIS")][TestCase("B08")][TestCase("B10")]
        [TestCase("B11")][TestCase("B12")][TestCase("B13")][TestCase("B21")]
        public void EveryApprovedRoleDispatchesItsNativeSkillOrExplicitPassive(string id)
        {
            var model=Fixture(out var run,id);Target(model);Assert.That(model.StartBattle(),Is.True);Enter(model);
            var u=model.Units.Single();u.SkillCharge=u.SkillRequired;Advance(model,6);
            Assert.That(float.IsNaN(u.Health)||float.IsNaN(u.SkillCharge),Is.False);
            if(u.SkillRequired>0)Assert.That(u.LastSkillAt,Is.GreaterThanOrEqualTo(0),id);
            else Assert.That(FormationCampaignRules.Find(id).Recovery,Is.EqualTo("被动"));
        }
        [Test]
        public void RipperSkillDealsFourIndependentNativeSegmentsAndNeverRecursesOnDerivedDamage()
        {
            var model=Fixture(out var run,"B22");Target(model);Assert.That(model.StartBattle(),Is.True);Enter(model);
            var u=model.Units.Single();u.SkillCharge=u.SkillRequired;Advance(model,1.05f);
            Assert.That(model.Hits.Count(h=>h.FromId==u.Id&&h.Skill&&!h.Heal),Is.EqualTo(4));
            Assert.That(u.SkillCharge,Is.LessThan(u.SkillRequired));
        }
        [Test]
        public void AttackRecoveryWaitsForCompletedAutosAndIgnoresNaturalTime()
        {
            var model=Fixture(out var run,"M11");Target(model);Assert.That(model.StartBattle(),Is.True);
            var u=model.Units.Single();Advance(model,1);Assert.That(u.SkillCharge,Is.Zero);
            Enter(model);Advance(model,.025f);Assert.That(u.SkillCharge,Is.EqualTo(1));
        }
        [Test]
        public void EncounterProgressKeepsHistoricalDamageAfterDefenderHealing()
        {
            var model=Fixture(out var run,"M11");var d=Target(model);d.MaxHealth=100;Assert.That(model.StartBattle(),Is.True);Enter(model);Advance(model,.025f);
            float progress=model.EncounterProgress;Assert.That(progress,Is.GreaterThan(0));d.Health=d.MaxHealth;
            model.Paused=true;Advance(model,1);Assert.That(model.EncounterProgress,Is.EqualTo(progress));
        }
        [Test]
        public void NativePhaseReturnsBeforeModeRevivalAndRetainsCharge()
        {
            var model=Fixture(out var run,"M30");var d=Target(model);d.Attack=100000;d.Range=2;Assert.That(model.StartBattle(),Is.True);Enter(model);
            var u=model.Units.Single();Advance(model,.025f);Assert.That(u.Alive,Is.False);Assert.That(u.Deaths,Is.Zero);Assert.That(u.NativePhaseReturningUntil,Is.GreaterThan(0));
            d.Attack=0;float sp=u.SkillCharge;Advance(model,3.9f);Assert.That(u.Alive,Is.False);Assert.That(u.SkillCharge,Is.EqualTo(sp));
            Advance(model,.15f);Assert.That(u.Alive,Is.True);Assert.That(u.NativePhase,Is.EqualTo(1));Assert.That(u.Deaths,Is.Zero);
        }
        [Test]
        public void ClearingDefendersStillRequiresARealBodyAtTheGate()
        {
            var model=Fixture(out var run,"M11");Target(model);Assert.That(model.StartBattle(),Is.True);Enter(model);
            foreach(var d in model.Defenders){d.Health=0;d.Alive=false;}
            model.Advance(.025f);Assert.That(model.Phase,Is.EqualTo(FormationPhase.Exiting));
            model.Units.Single().Position=new Vector2(FormationBattleModel.Width-1,3);model.Advance(.025f);Assert.That(model.Phase,Is.EqualTo(FormationPhase.Won));
        }
        [Test]
        public void ComboAndResonanceRequireDistinctSpeciesAndEquippedBadges()
        {
            var model=Fixture(out var run,"B22","B23","B25","WIS","M20","B13","M25","M32","B11");
            int beast=model.Units.First(u=>u.Name=="狂躁异质裂兽").Instance;
            Assert.That(run.Equip(run.AddItem("徽章:同频").Instance,beast).Success,Is.True);
            Assert.That(run.Equip(run.AddItem("AC").Instance,beast).Success,Is.True);
            Assert.That(run.Equip(run.AddItem("CF").Instance,beast).Success,Is.True);
            foreach(string name in new[]{"梅菲斯特","蔓德拉"})Assert.That(run.Equip(run.AddItem("徽章:连击").Instance,model.Units.First(u=>u.Name==name).Instance).Success,Is.True);
            model.BindCampaign(run);Assert.That(model.ActiveSynergies["连击"],Is.EqualTo(8));Assert.That(model.ActiveSynergies["同频"],Is.EqualTo(5));
            run.ResonanceLeaderInstance=beast;run.ResonancePartnerInstance=model.Units.First(u=>u.Name=="圣徒").Instance;run.MainCarryInstance=beast;
            Target(model);Assert.That(model.StartBattle(),Is.True);Enter(model);Advance(model,75);
            Assert.That(model.BattleEvents.Count(e=>e.Contains("同频")),Is.GreaterThan(30));
            Assert.That(model.Units.First(u=>u.Instance==beast).LastSkillAt,Is.GreaterThan(65));
            var casts=model.SkillCasts.Where(c=>c.Instance==beast&&c.At>=45).Select(c=>c.At).ToArray();
            var gaps=casts.Skip(1).Zip(casts,(later,earlier)=>later-earlier).ToArray();
            TestContext.WriteLine($"RIPPER_RUNTIME casts={casts.Length}; mature_gap={gaps.DefaultIfEmpty(0).Min():0.000}..{gaps.DefaultIfEmpty(0).Max():0.000}");
            Assert.That(gaps.Length,Is.GreaterThan(10));Assert.That(gaps.Min(),Is.GreaterThanOrEqualTo(.89));
        }

        [TestCase(1)][TestCase(2)][TestCase(3)]
        public void WisadelThreePreparationChoicesUseDifferentRecoveryAndActionStates(int skill)
        {
            var model=Fixture(out var run,"WIS");run.WisadelSkill=skill;model.BindCampaign(run);Target(model);
            Assert.That(model.StartBattle(),Is.True);Enter(model);var u=model.Units.Single();
            Assert.That(u.SkillRequired,Is.EqualTo(skill==1?3:skill==2?24:28));
            u.SkillCharge=u.SkillRequired;Advance(model,.025f);
            Assert.That(u.SkillAnimation,Is.EqualTo(skill==1?"Skill_1":skill==2?"Skill_2_Begin":"Skill_3_Begin"));
            Advance(model,5);Assert.That(model.Hits.Any(h=>h.FromId==u.Id),Is.True);
        }

        [Test]
        public void DegenbrecherDistributesEightStrikesAcrossThreeTargetsInsteadOfTriplingThem()
        {
            var model=Fixture(out var run,"B04");
            for(int i=0;i<model.Defenders.Count;i++)
            {var d=model.Defenders[i];d.Position=new Vector2(15+i*.2f,3);d.MaxHealth=100000000;d.Attack=0;d.Range=.01f;d.Armor=0;}
            Assert.That(model.StartBattle(),Is.True);Enter(model);var u=model.Units.Single();u.SkillCharge=u.SkillRequired;Advance(model,1.25f);
            var strikes=model.Hits.Where(h=>h.FromId==u.Id&&h.Skill&&!h.Heal).ToArray();
            Assert.That(strikes.Length,Is.EqualTo(8));Assert.That(strikes.Select(h=>h.ToId).Distinct().Count(),Is.EqualTo(3));
        }

        [TestCase("1-3")][TestCase("2-4")][TestCase("3-7")]
        public void PresetBudgetedLineupsFightUnmodifiedNodeDefenders(string nodeId)
        {
            // Previous nodes are skipped deliberately: this measures the real combat at a
            // documented assembled board, not acquisition probability or a natural full run.
            string[] ids=nodeId=="1-3"?new[]{"M03","M11","M12"}:nodeId=="2-4"?new[]{"M14","M12","M13","M25","M18","B23"}:new[]{"B22","B23","B25","WIS","M20","B13","M25","M32","B11"};
            var model=Fixture(out var run,ids);
            int safety=0;
            while(run.CurrentNode.Id!=nodeId)
            {
                Assert.That(++safety,Is.LessThan(30));
                if(!run.NodeSettled)run.SettleBattle(true,1,3,0,false);
                Assert.That(run.AdvanceNode(),Is.True);
                while(run.HasBlockingChoice)Assert.That(run.Choose(0).Success,Is.True);
            }
            run.Shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=FormationCampaignRules.Find(id).CardId,Star=id=="B22"?3:2}),Array.Empty<string>(),10,0,80);
            Assert.That(model.ApplyRoster(run.Shop.Bench.Select((p,i)=>new FormationRosterEntry{Instance=p.Instance,Star=p.Star,ArtId=p.Id,Spawn=new Vector2(9-(i/4)*3,(i%4)*2)}),10),Is.True);
            if(nodeId=="3-7")
            {
                int beast=model.Units.First(u=>u.Name=="狂躁异质裂兽").Instance;
                run.MainCarryInstance=run.ResonanceLeaderInstance=beast;run.ResonancePartnerInstance=model.Units.First(u=>u.Name=="圣徒").Instance;
                foreach(string key in new[]{"徽章:同频","AC","CF"})Assert.That(run.Equip(run.AddItem(key).Instance,beast).Success,Is.True);
                foreach(string name in new[]{"梅菲斯特","蔓德拉"})Assert.That(run.Equip(run.AddItem("徽章:连击").Instance,model.Units.First(u=>u.Name==name).Instance).Success,Is.True);
                // Fourteen occupied equipment slots total, including the three badges. This
                // keeps the prebuilt endgame inside the document's component-budget horizon.
                foreach(var equipment in new[]{("M25","CE"),("M25","BC"),("M32","CC"),("B11","CC"),("B11","CF"),("B13","CC"),("B13","CF"),("WIS","AA"),("WIS","AF")})
                {
                    string name=FormationCampaignRules.Find(equipment.Item1).Name;
                    Assert.That(run.Equip(run.AddItem(equipment.Item2).Instance,model.Units.First(u=>u.Name==name).Instance).Success,Is.True);
                }
            }
            model.BindCampaign(run);
            float health=model.Defenders.Sum(d=>d.MaxHealth);Assert.That(model.StartBattle(),Is.True);
            Advance(model,float.IsPositiveInfinity((float)model.LimitSeconds)?180:(float)model.LimitSeconds+1);
            TestContext.WriteLine($"PRESET_RULES preplan={run.PreplanId}; augments={string.Join(",",run.AugmentIds)}; equipped={run.AllEquipment.Count(e=>e.Owner!=0)}");
            TestContext.WriteLine($"PRESET_NODE {nodeId}: phase={model.Phase}; clock={model.Clock:0.000}; initialHP={health:0}; progress={model.EncounterProgress:P2}; remaining={model.RemainingDefenders}; deaths={model.Units.Sum(u=>u.Deaths)}; damage={model.Units.Sum(u=>u.DamageDone):0}");
            TestContext.WriteLine("PRESET_REMAINING "+string.Join(";",model.Defenders.Where(d=>d.Alive).Select(d=>$"{d.Name}:{d.Health:0}/{d.MaxHealth:0}@{d.Position}")));
            Assert.That(model.EncounterProgress,Is.GreaterThan(.25f),"An assembled board must at least earn encounter progress against the real node curve.");
            Assert.That(model.Units.Sum(u=>u.DamageDone),Is.GreaterThan(100));
            Assert.That(model.Clock,Is.GreaterThan(0));
            Assert.That(model.Phase,Is.EqualTo(FormationPhase.Won));
        }

        [Test]
        public void ManualSeaCellsMustBeFourConnectedAndWithinCurrentTraitBudget()
        {
            var model=Fixture(out var run,"M33","M35");
            run.SeaCellIds.Add(14+3*FormationBattleModel.Width);run.SeaCellIds.Add(16+3*FormationBattleModel.Width);
            Assert.That(model.ValidateSeaPlacement(out _),Is.False);
            run.SeaCellIds.Add(15+3*FormationBattleModel.Width);Assert.That(model.ValidateSeaPlacement(out _),Is.True);
            model.RefreshSeaPreview();Assert.That(model.SeaCells.Count,Is.EqualTo(3));
            run.SeaCellIds.Add(17+3*FormationBattleModel.Width);run.SeaCellIds.Add(18+3*FormationBattleModel.Width);
            Assert.That(model.StartBattle(),Is.False);
        }

        [TestCase("M17")][TestCase("M03")][TestCase("M11")]
        public void ApprovedOneCostRolesCanDeployFourStarsRegardlessOfLegacyCatalogCost(string id)
        {
            var model=Fixture(out var run,id);
            run.Shop.Preset(new[]{new ShopTrialModel.Piece{Id=FormationCampaignRules.Find(id).CardId,Star=4}},Array.Empty<string>(),10,0,80);
            var card=run.Shop.Bench.Single();
            Assert.That(model.ApplyRoster(new[]{new FormationRosterEntry{Instance=card.Instance,ArtId=card.Id,Star=4,Spawn=new Vector2(FormationBattleModel.DeploymentColumns-1,3)}},10),Is.True);
            Assert.That(model.Units.Single().MaxHealth,Is.EqualTo(FormationCampaignRules.Find(id).Health*5.832f).Within(.01f));
        }

        [Test]
        public void NativeAndModeReturnFromOneSourceShareExactlyOneHandoffCredit()
        {
            var model=Fixture(out var run,"M30","M14","M32","B24","M20","B08");
            var source=model.Units.First(u=>u.Name=="深池逐火战士");
            run.RelayReceiverInstance=model.Units.First(u=>u.Name=="万火归一").Instance;model.BindCampaign(run);
            var guard=Target(model);guard.Attack=100000;guard.Range=2;Assert.That(model.StartBattle(),Is.True);
            source.Position=new Vector2(14.5f,3);Advance(model,.025f);Assert.That(source.Alive,Is.False);
            guard.Attack=0;Advance(model,4.05f);Assert.That(source.Alive,Is.True);Assert.That(source.NativePhase,Is.EqualTo(1));
            Assert.That(model.BattleEvents.Count(e=>e.Contains("接棒:")),Is.EqualTo(1));
            guard.TargetId=source.Id;guard.Attack=100000;
            for(int i=0;i<100&&source.Alive;i++)model.Advance(.025f);
            Assert.That(source.Alive,Is.False);Assert.That(source.Deaths,Is.EqualTo(1));guard.Attack=0;
            Advance(model,8.05f);Assert.That(source.Alive,Is.True);
            Assert.That(model.BattleEvents.Count(e=>e.Contains("接棒:")),Is.EqualTo(1),"Native and mode returns must share a source credit.");
        }

        static void ReachNode(FormationCampaignRun run,string nodeId)
        {
            int safety=0;
            while(run.CurrentNode.Id!=nodeId)
            {
                Assert.That(++safety,Is.LessThan(30));
                if(!run.NodeSettled)run.SettleBattle(true,1,3,0,false);
                Assert.That(run.AdvanceNode(),Is.True);
                while(run.HasBlockingChoice)Assert.That(run.Choose(0).Success,Is.True);
            }
        }
        [TestCase("先锋",8,20,1)]
        [TestCase("近卫",8,22,1.4f)]
        [TestCase("重装",10,24,1)]
        [TestCase("狙击",8,24,1.5f)]
        [TestCase("术师",6,22,1.4f)]
        [TestCase("医疗",10,24,1.2f)]
        [TestCase("辅助",10,24,1.4f)]
        public void AuthoredDefenderTemplatesPreserveInitialChargeThresholdAndRealSkillAction(string name,float initial,float required,float seconds)
        {
            Fixture(out var run,"M11");ReachNode(run,"1-3");FormationBattleModel model=null;FormationDefender guard=null;
            for(int seed=1;seed<=100&&guard==null;seed++)
            {
                var candidate=new FormationBattleModel(seed);candidate.BindCampaign(run);
                var match=candidate.Defenders.FirstOrDefault(d=>d.Name==name);
                if(match==null)continue;model=candidate;guard=match;
            }
            Assert.That(guard,Is.Not.Null,"The ordinary node generator must expose the authored template.");
            var card=run.Shop.Bench.First(p=>p.Id==FormationCampaignRules.Find("M11").CardId);
            Assert.That(model.ApplyRoster(new[]{new FormationRosterEntry{Instance=card.Instance,Star=card.Star,ArtId=card.Id,Spawn=new Vector2(FormationBattleModel.DeploymentColumns-1,3)}},10),Is.True);
            Assert.That(guard.SkillCharge,Is.EqualTo(initial));Assert.That(guard.SkillRequired,Is.EqualTo(required));
            model.Defenders.RemoveAll(d=>d!=guard);guard.Position=new Vector2(15,3);
            Assert.That(model.StartBattle(),Is.True);Assert.That(guard.SkillCharge,Is.EqualTo(initial));Assert.That(guard.SkillRequired,Is.EqualTo(required));
            var fighter=model.Units.Single();fighter.Position=new Vector2(14.5f,3);
            guard.SkillCharge=guard.SkillRequired;guard.Health-=1;
            model.Advance(.025f);
            Assert.That(guard.LastSkillAt,Is.GreaterThanOrEqualTo(0));Assert.That(guard.PresentationDuration,Is.EqualTo(seconds).Within(.0001));
        }

        [TestCase("1-9",12,24,1)]
        [TestCase("2-7",10,24,1)]
        [TestCase("3-7",12,26,3)]
        public void BossesUseTheirOwnAuthoredInitialChargeAndActionInsteadOfAUniversalTwelve(string nodeId,float initial,float required,float seconds)
        {
            var model=Fixture(out var run,"M11");ReachNode(run,nodeId);model.BindCampaign(run);
            var spec=FormationCampaignRules.Bosses.Single(b=>b.NodeId==nodeId);var boss=model.Defenders.Single(d=>d.Name==spec.Name);
            Assert.That(boss.SkillCharge,Is.EqualTo(initial));Assert.That(boss.SkillRequired,Is.EqualTo(required));
            model.Defenders.RemoveAll(d=>d!=boss);boss.Position=new Vector2(15,3);
            Assert.That(model.StartBattle(),Is.True);Assert.That(boss.SkillCharge,Is.EqualTo(initial));
            model.Units.Single().Position=new Vector2(14.5f,3);boss.SkillCharge=boss.SkillRequired;
            model.Advance(.025f);
            Assert.That(boss.LastSkillAt,Is.GreaterThanOrEqualTo(0));Assert.That(boss.PresentationDuration,Is.EqualTo(seconds).Within(.0001));
            Assert.That(boss.MaxHealth,Is.EqualTo(spec.Health));Assert.That(boss.Attack,Is.EqualTo(spec.Attack));
        }
    }
}
