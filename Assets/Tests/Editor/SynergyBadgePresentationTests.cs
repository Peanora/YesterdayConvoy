using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class SynergyBadgePresentationTests
    {
        [Test]public void NineNamedResourcesHaveApprovedDimensionsAndIndependentSources()
        {
            var hashes=new HashSet<string>();var icons=new HashSet<Texture2D>();
            foreach(var entry in SynergyTrialCatalog.Entries)
            {
                var icon=SynergyBadgePresentation.IconFor(entry.Tag);Assert.That(icon,Is.Not.Null,entry.Tag);
                Assert.That(icon.width,Is.EqualTo(256));Assert.That(icon.height,Is.EqualTo(256));
                string path=AssetDatabase.GetAssetPath(icon);
                Assert.That(path,Does.EndWith("/UI/Synergies/"+entry.Id+".png"),entry.Tag);
                Assert.That(icons.Add(icon),Is.True,"Different identities cannot share a fallback resource.");
                using(var sha=SHA256.Create())Assert.That(hashes.Add(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))),Is.True,"Different glyphs need different source bytes.");
            }
            Assert.That(icons.Count,Is.EqualTo(9));Assert.That(SynergyBadgePresentation.IconFor("未知羁绊"),Is.Null);
            Assert.That(SynergyBadgePresentation.IconFor(null),Is.Null);
        }

        static SynergySummary Summary(string id,int count)
        {
            var entry=SynergyTrialCatalog.Find(id);
            return SynergySummary.From(entry.Members.Take(count)).Single(row=>row.Name==entry.Tag);
        }
        static SynergyVisualGrade Grade(SynergySummary row)=>SynergyBadgePresentation.GradeFor(row.Name,row.Tier);

        [Test]public void DiamondFramesLoadSeparatelyAndFullThresholdsDoNotReplaceActualCounts()
        {
            var frames=new HashSet<Texture2D>();
            foreach(SynergyVisualGrade grade in Enum.GetValues(typeof(SynergyVisualGrade)))
            {
                var frame=SynergyBadgePresentation.FrameFor(grade);Assert.That(frame,Is.Not.Null,grade.ToString());
                Assert.That(frame.width,Is.EqualTo(256));Assert.That(frame.height,Is.EqualTo(256));
                Assert.That(AssetDatabase.GetAssetPath(frame),Does.EndWith("/UI/Synergies/Frames/plate-"+grade.ToString().ToLowerInvariant()+".png"));
                Assert.That(frames.Add(frame),Is.True,"A separate plate supplies each grade, instead of tinting the white glyph.");
            }
            var counter=SynergyBadgePresentation.CounterFor();Assert.That(counter,Is.Not.Null);
            Assert.That(counter.width,Is.EqualTo(256));Assert.That(counter.height,Is.EqualTo(256));
            Assert.That(AssetDatabase.GetAssetPath(counter),Does.EndWith("/UI/Synergies/Frames/count-dot.png"));
            var roster=new[]{"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M07","CARD_M08"};
            var reunion=SynergySummary.From(roster).Single(row=>row.Name=="整合运动");
            Assert.That(reunion.Count,Is.EqualTo(9));Assert.That(reunion.Tier,Is.EqualTo(6));
            Assert.That(Grade(reunion),Is.EqualTo(SynergyVisualGrade.Gold),"The count bubble is nine while the activated threshold and plate remain six/gold.");
            var sequences=new Dictionary<string,int[]>{
                {"reunion",new[]{2,4,6}},{"snow",new[]{2,5}},{"infection",new[]{2,4,6}},
                {"guerrilla",new[]{2,4,6,8,9}},{"mudrock",new[]{2,4,5,6}},
                {"herd",new[]{2,4}},{"deep-pool",new[]{2,4}},{"marine",new[]{2,4}},{"sarkaz",new[]{2,4,6}}
            };
            foreach(var pair in sequences)CollectionAssert.AreEqual(pair.Value,Summary(pair.Key,1).Thresholds,pair.Key+" keeps the complete sequence, including inactive future thresholds.");
        }

        [Test]public void UnequalRosterThresholdsKeepTheirActualMetalGrades()
        {
            var snowFour=Summary("snow",4);Assert.That(snowFour.Count,Is.EqualTo(4));Assert.That(snowFour.Tier,Is.EqualTo(2));
            Assert.That(Grade(snowFour),Is.EqualTo(SynergyVisualGrade.Bronze),"Four snow kinds have not activated the five-kind threshold.");
            Assert.That(Grade(Summary("snow",5)),Is.EqualTo(SynergyVisualGrade.Silver));
            Assert.That(Grade(Summary("mudrock",4)),Is.EqualTo(SynergyVisualGrade.Silver));
            Assert.That(Grade(Summary("mudrock",5)),Is.EqualTo(SynergyVisualGrade.Gold),"Five kinds mean different grades for snow and Mudrock.");
            var mudrock=SynergySummary.From(SynergyTrialCatalog.Find("mudrock").Members.Concat(new[]{"CARD_M07"}),
                extraMudrockMembers:new[]{"CARD_M07"}).Single(row=>row.Name=="泥岩小队");
            Assert.That(mudrock.Count,Is.EqualTo(6));Assert.That(mudrock.Tier,Is.EqualTo(6));
            Assert.That(Grade(mudrock),Is.EqualTo(SynergyVisualGrade.Prismatic),"The transferred sixth kind activates the prismatic plate.");
            foreach(string id in new[]{"reunion","infection","sarkaz"})Assert.That(Grade(Summary(id,6)),Is.EqualTo(SynergyVisualGrade.Gold),id);
            foreach(string id in new[]{"herd","deep-pool","marine"})Assert.That(Grade(Summary(id,4)),Is.EqualTo(SynergyVisualGrade.Silver),id);
            foreach(var entry in SynergyTrialCatalog.Entries)Assert.That(SynergyBadgePresentation.GradeFor(entry.Tag,0),Is.EqualTo(SynergyVisualGrade.Inactive),entry.Tag);
        }

        [Test]public void RealTransferMembershipCrossesGoldToPrismaticAndNineRetainsIt()
        {
            string[] native={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20"};
            var roster=native.Concat(new[]{"CARD_M07","CARD_M08"}).ToArray();
            Func<string[],SynergySummary> guerrilla=extra=>SynergySummary.From(roster,extra).Single(row=>row.Name=="游击队");
            var seven=guerrilla(Array.Empty<string>());Assert.That(seven.Count,Is.EqualTo(7));Assert.That(seven.Tier,Is.EqualTo(6));Assert.That(Grade(seven),Is.EqualTo(SynergyVisualGrade.Gold));
            var eight=guerrilla(new[]{"CARD_M07"});Assert.That(eight.Tier,Is.EqualTo(8));Assert.That(Grade(eight),Is.EqualTo(SynergyVisualGrade.Prismatic));
            var nine=guerrilla(new[]{"CARD_M07","CARD_M08"});Assert.That(nine.Tier,Is.EqualTo(9));Assert.That(Grade(nine),Is.EqualTo(SynergyVisualGrade.Prismatic));
            Assert.That(Grade(guerrilla(new[]{"CARD_M07","CARD_M07"})),Is.EqualTo(SynergyVisualGrade.Prismatic),"Duplicate designations cannot fabricate a ninth member.");
            Assert.That(guerrilla(new[]{"CARD_M07","CARD_M07"}).Count,Is.EqualTo(8));
        }

        [Test]public void OnlyPrismaticUsesSeveralHueFamiliesWhileMetalColorsStayStable()
        {
            var hues=new HashSet<int>();
            foreach(float height in new[]{0f,.2f,.4f,.6f,.8f,1f})
            {
                var color=SynergyBadgePresentation.ColorFor(SynergyVisualGrade.Prismatic,height);
                Color.RGBToHSV(color,out float hue,out float saturation,out float value);
                Assert.That(saturation,Is.GreaterThan(.1f));Assert.That(value,Is.GreaterThan(.5f));hues.Add((int)(hue*12));
            }
            Assert.That(hues.Count,Is.GreaterThanOrEqualTo(3),"A gold or white highlight is not prismatic coloring.");
            foreach(var grade in new[]{SynergyVisualGrade.Inactive,SynergyVisualGrade.Bronze,SynergyVisualGrade.Silver,SynergyVisualGrade.Gold})
                Assert.That(SynergyBadgePresentation.ColorFor(grade,0),Is.EqualTo(SynergyBadgePresentation.ColorFor(grade,1)),grade.ToString());
            Assert.That(new[]{SynergyVisualGrade.Inactive,SynergyVisualGrade.Bronze,SynergyVisualGrade.Silver,SynergyVisualGrade.Gold}.Select(grade=>SynergyBadgePresentation.ColorFor(grade)).Distinct().Count(),Is.EqualTo(4));
        }
    }
}
