using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationUnitCatalogTests
    {
        [Test] public void EveryAuthoredCardHasMatchingFormationIdentityAndCost()
        {
            var cards=AssetDatabase.FindAssets("t:CardDefinition",new[]{"Assets/Data/Cards"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<CardDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Assert.That(cards.Length,Is.EqualTo(62));
            Assert.That(FormationUnitCatalog.All.Count,Is.EqualTo(cards.Length));
            foreach(var card in cards)
            {
                var definition=FormationUnitCatalog.Find(card.CardId);
                Assert.That(definition,Is.Not.Null,card.CardId);
                var rules=FormationCampaignRules.Find(card.CardId);
                if(rules!=null&&card.CardId.StartsWith("CARD_B",StringComparison.Ordinal))
                {
                    Assert.That(definition.Cost,Is.EqualTo(rules.Cost),card.CardId);
                    Assert.That(definition.Stats.HP,Is.EqualTo(rules.Health),card.CardId);
                    Assert.That(definition.Stats.Attack,Is.EqualTo(rules.Attack),card.CardId);
                    if(card.CardId.StartsWith("CARD_B",StringComparison.Ordinal))
                    {Assert.That(card.RecruitCost,Is.EqualTo(rules.Cost));Assert.That(card.DisplayName,Is.EqualTo(rules.Name));}
                }
                else
                {
                    Assert.That(definition.Name,Is.EqualTo(card.DisplayName),card.CardId);
                    Assert.That(definition.Cost,Is.EqualTo(card.RecruitCost),card.CardId);
                    Assert.That(definition.Stats,Is.SameAs(CampaignBalance.Profiles[card.CardId]),card.CardId);
                }
                Assert.That(FormationUnitCatalog.Find(definition.ArtId),Is.SameAs(definition));
            }
        }

        [Test] public void LargeOriginalBodiesHaveMultipleCellsAndVariedVisualHeights()
        {
            Assert.That(FormationUnitCatalog.Find("M10").Footprint,Is.EqualTo(Vector2Int.one),"The existing starter must still fit one cell.");
            foreach(string id in new[]{"M20","M28","M34","M36"})
            {
                var large=FormationUnitCatalog.Find(id);
                Assert.That(large.Footprint,Is.EqualTo(new Vector2Int(2,2)),id);
                Assert.That(large.VisualHeight,Is.GreaterThan(FormationUnitCatalog.Find("M07").VisualHeight),id);
            }
            foreach(var definition in FormationUnitCatalog.All)
            {
                Assert.That(definition.Footprint.x,Is.InRange(1,FormationBattleModel.DeploymentColumns),definition.CardId);
                Assert.That(definition.Footprint.y,Is.InRange(1,FormationBattleModel.Height),definition.CardId);
                Assert.That(definition.VisualHeight,Is.GreaterThan(0),definition.CardId);
                Assert.That(definition.TemplateKind,Is.InRange(0,5),definition.CardId);
            }
            Assert.That(FormationUnitCatalog.Find("M02").VisualHeight,Is.LessThan(FormationUnitCatalog.Find("M07").VisualHeight));
        }

        [Test] public void AllExistingRolesPreserveShopEligibilityAndSupportDoesNotBecomeAPurchasableCard()
        {
            Assert.That(FormationUnitCatalog.All.Count(d=>d.ShopEligible),Is.EqualTo(CampaignBalance.Profiles.Keys.Count(CampaignBalance.ShopEligible)+19));
            Assert.That(FormationUnitCatalog.Find("M41").ShopEligible,Is.False,"Roster support must not silently unpause the silver-wolf shop role.");
            var golem=FormationUnitCatalog.Find(CampaignBalance.GolemCardId);
            Assert.That(golem,Is.Not.Null);
            Assert.That(golem.ShopEligible,Is.False);
            Assert.That(golem.Stats,Is.Null,"Summon stage stats are owned by the source summon rules.");
            CollectionAssert.DoesNotContain(FormationUnitCatalog.All,golem);
            Assert.That(FormationUnitCatalog.Find("B21"),Is.Not.Null,"Approved 岁界园版 now has an original runtime package.");
            Assert.That(FormationUnitCatalog.Find(null),Is.Null);
        }

        [Test] public void ExistingSixPrototypeMappingsRemainStable()
        {
            string[] ids={"M10","M02","M07","M08","M09","M17"};
            for(int i=0;i<ids.Length;i++)Assert.That(FormationUnitCatalog.Find(ids[i]).TemplateKind,Is.EqualTo(i),ids[i]);
        }

        [Test] public void CatalogLookupDoesNotDecodeAnimationAtlases()
        {
            int loads=ArtMemoryCache.TextureLoads;
            foreach(var definition in FormationUnitCatalog.All)
            {
                Assert.That(FormationUnitCatalog.Find(definition.CardId),Is.SameAs(definition));
                if(definition.ArtRoot!=null)
                    Assert.That(System.IO.File.Exists(ArtSourcePaths.Resolve(definition.ArtRoot+"/manifest.json")),Is.True,definition.CardId);
            }
            Assert.That(FormationUnitCatalog.Find("WISADEL_APRIL").ArtRoot,Is.Null,"The original native outfit takes precedence; preserve the approved serialized Sprite fallback.");
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }

        [Test] public void ArtMetadataCoversAllPackagesWithoutInventingMissingMotionOrDecodingAtlases()
        {
            var cards=AssetDatabase.FindAssets("t:CardDefinition",new[]{"Assets/Data/Cards"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<CardDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            FormationUnitArt.RegisterCards(cards);
            int loads=ArtMemoryCache.TextureLoads;
            foreach(var definition in FormationUnitCatalog.All)
            {
                var art=FormationUnitArt.For(definition.CardId);
                Assert.That(art,Is.Not.Null,definition.CardId);
                Assert.That(FormationUnitArt.For(definition.ArtId),Is.SameAs(art),definition.CardId);
                Assert.That(art.Height,Is.GreaterThan(0),definition.CardId);
                Assert.That(art.HasAction("idle"),Is.True,definition.CardId);
                Assert.That(art.Duration("idle"),Is.GreaterThan(0),definition.CardId);
                Assert.That(art.HasAction("missing-action"),Is.False,definition.CardId);
                Assert.That(art.Duration("missing-action"),Is.Zero,definition.CardId);
                Assert.That(art.Frame("missing-action",0),Is.Null,definition.CardId);
            }
            Assert.That(FormationUnitArt.For("M33").HasAction("attack"),Is.False,"The source crawler has no attack clip.");
            Assert.That(FormationUnitArt.For("TALULAH_TRIAL").HasAction("death"),Is.True,"Talulah's actual extra death clip is registered.");
            var vivi=FormationUnitArt.For("WISADEL_APRIL");
            Assert.That(vivi.HasAction("walk"),Is.True,"Restore the separate user-approved walk frames with the April outfit.");
            Assert.That(vivi.Duration("walk"),Is.EqualTo(.95f).Within(.00001));
            Assert.That(vivi.HasAction("attack"),Is.True);
            Assert.That(vivi.HasAction("death"),Is.True,"The new mode reads the original April game#9 death motion.");
            Assert.That(vivi.Duration("death"),Is.GreaterThan(0));
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }

        [Test] public void ApprovedFortySixUseDistinctCatalogEntriesAndExcludeRemovedCards()
        {
            Assert.That(FormationCampaignRules.AllUnits.Length,Is.EqualTo(46));
            CollectionAssert.AllItemsAreUnique(FormationCampaignRules.AllUnits.Select(u=>u.CardId));
            foreach(var unit in FormationCampaignRules.AllUnits)
                Assert.That(FormationUnitCatalog.Find(unit.CardId),Is.Not.Null,unit.CardId);
            foreach(string removed in new[]{"CARD_M04","CARD_M05","CARD_M06","CARD_M41"})
                Assert.That(FormationCampaignRules.Find(removed),Is.Null,removed);
            // Removed formal roles remain addressable by the historical demos.
            Assert.That(FormationUnitCatalog.Find("M04"),Is.Not.Null);
            Assert.That(FormationUnitCatalog.Find("M41"),Is.Not.Null);
        }
    }
}
