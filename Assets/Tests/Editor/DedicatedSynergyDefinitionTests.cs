using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using YesterdayConvoy.Editor;

namespace YesterdayConvoy.Tests
{
    public sealed class DedicatedSynergyDefinitionTests
    {
        [Test]public void GeneratedDefaultsHaveNineDistinctScenesAndIndependentEncounters()
        {
            Assert.That(SynergyTrialCatalog.Entries.Count,Is.EqualTo(9));
            Assert.That(SynergyTrialCatalog.Entries.Select(e=>e.Id).Distinct().Count(),Is.EqualTo(9));
            var definitions=SynergyTrialCatalog.Entries.Select(e=>AssetDatabase.LoadAssetAtPath<SynergyTrialDefinition>(e.DefinitionPath)).ToArray();
            Assert.That(definitions.All(d=>d!=null),Is.True,"Run CreateMissingScenes before these generated-asset tests.");
            Assert.That(definitions.Select(d=>d.encounter).Distinct().Count(),Is.EqualTo(9));
            foreach(var entry in SynergyTrialCatalog.Entries)
            {
                var data=AssetDatabase.LoadAssetAtPath<SynergyTrialDefinition>(entry.DefinitionPath);data.Validate();
                Assert.That(data.initialMembers.Select(m=>m.cardId),Is.EqualTo(entry.Members));
                Assert.That(SynergySummary.From(data.initialMembers.Select(m=>m.cardId)).Single(s=>s.Name==entry.Tag).Tier,Is.EqualTo(entry.MaximumTier));
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.ScenePath),Is.Not.Null);
                var snapshot=data.CopyDefenders();float original=data.encounter.defenders[0].health;snapshot[0].health=1;
                Assert.That(data.encounter.defenders[0].health,Is.EqualTo(original),"Runtime multiplier/mutation must not reach the baseline asset.");
            }
        }
        [Test]public void InfiniteGoldStillReservesOneThreeOrNineCopiesPerStartingCard()
        {
            var roles=CampaignBalance.Profiles.Select(p=>new ShopTrialModel.Role{Id=p.Key,Name=p.Key,Cost=p.Value.Cost});
            foreach(var entry in SynergyTrialCatalog.Entries)
            {
                var data=AssetDatabase.LoadAssetAtPath<SynergyTrialDefinition>(entry.DefinitionPath);Assert.That(data,Is.Not.Null);
                var pieces=data.StartingPieces();pieces[0].Star=3;
                var shop=new ShopTrialModel(roles,44);shop.EnableCampaign(true);shop.Preset(pieces,Array.Empty<string>(),data.initialLevel,0,10);
                Assert.That(shop.UnlimitedGold,Is.True);Assert.That(shop.Available(pieces[0].Id),Is.Zero);
                for(int r=0;r<3;r++)
                {
                    Assert.That(shop.Refresh(true).Success,Is.True);
                    foreach(var role in shop.Roles)
                    {
                        int owned=shop.Bench.Where(p=>p.Id==role.Id).Sum(p=>p.Star==3?9:p.Star==2?3:1);
                        Assert.That(shop.Available(role.Id)+owned+shop.Offers.Count(id=>id==role.Id),Is.EqualTo(9),role.Id);
                    }
                    Assert.That(shop.Offers.Contains(pieces[0].Id),Is.False);shop.Validate();
                }
            }
        }
        [Test]public void InvalidInspectorFormationIsRejectedWithoutEditingSourceAssets()
        {
            var source=AssetDatabase.LoadAssetAtPath<SynergyTrialDefinition>(SynergyTrialCatalog.Find("snow").DefinitionPath);Assert.That(source,Is.Not.Null);
            var copy=UnityEngine.Object.Instantiate(source);
            try
            {
                copy.initialLevel=1;Assert.Throws<InvalidOperationException>(()=>copy.Validate());copy.initialLevel=8;
                copy.initialMembers=new[]{new SynergyTrialStartingCard{cardId="CARD_M11"},new SynergyTrialStartingCard{cardId="CARD_M11"}};
                Assert.Throws<InvalidOperationException>(()=>copy.Validate());
                copy.initialMembers=new[]{new SynergyTrialStartingCard{cardId="CARD_M11",star=4}};Assert.Throws<InvalidOperationException>(()=>copy.Validate());
            }
            finally {UnityEngine.Object.DestroyImmediate(copy);}
            Assert.That(source.initialLevel,Is.EqualTo(8));source.Validate();
        }
        [Test]public void BuildCatalogIncludesEveryAuthoredSceneAndAllNineTrials()
        {
            var paths=GameSceneCatalog.BuildPaths();Assert.That(paths[0],Is.EqualTo(CampaignTools.CampaignScene));
            foreach(var scene in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))Assert.That(paths,Does.Contain(scene));
            foreach(var entry in SynergyTrialCatalog.Entries)Assert.That(paths,Does.Contain(entry.ScenePath));
            Assert.That(paths.Distinct().Count(),Is.EqualTo(paths.Length));
        }
    }
}
