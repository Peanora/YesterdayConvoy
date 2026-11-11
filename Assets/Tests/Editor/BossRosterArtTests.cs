using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class BossRosterArtTests
    {
        [Test]public void RepresentativeMetadataKeepsExactSourceMotionsAndDoesNotDecodeImages()
        {
            int loads=ArtMemoryCache.TextureLoads;
            var art=BossRosterArt.For("CARD_B22");
            Assert.That(art,Is.Not.Null);
            Assert.That(art.Model,Is.EqualTo("enemy_1571_mirbst"));
            Assert.That(art.SourceNames.Count(),Is.EqualTo(9));
            Assert.That(art.Sequences.Count(),Is.EqualTo(5));
            Assert.That(art.AllSequences.Count(),Is.EqualTo(9));
            Assert.That(art.ZeroDurationStates.Single().name,Is.EqualTo("Default"));
            Assert.That(art.HasClip("Default"),Is.False);
            Assert.That(art.HasZeroDurationState("Default"),Is.True);
            Assert.That(art.SequencesFor(new[]{"Default"}).Any(),Is.False);
            Assert.That(art["attack"],Is.SameAs(art["Attack"]));
            Assert.That(art["attack"].OnAttackTimes.ToArray(),Is.EqualTo(new[]{.6333000063896179f,.8666999936103821f}));
            Assert.That(art.AttackContact,Is.EqualTo(.6333000063896179f));
            Assert.That(art.AttackDuration,Is.EqualTo(2.3333001136779785f));
            Assert.That(art["Idle"].Ppu,Is.EqualTo(.3198760616926739f/.0045f).Within(.0001f));
            Assert.That(art["Idle"].Pivot,Is.EqualTo(new Vector2(.5007354684146905f,.28756155886646484f)));
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }

        [Test]public void OriginalSampleTimeIncludesTheTerminalFrameWithoutRetimingOrPrewarming()
        {
            int loads=ArtMemoryCache.TextureLoads;
            var clip=BossRosterArt.For("B22")["Idle"];
            Assert.That(clip.Count,Is.EqualTo(481));
            Assert.That(clip.Duration,Is.EqualTo(8));
            Assert.That(clip.FrameIndex(-1,false),Is.EqualTo(0));
            Assert.That(clip.FrameIndex(1d/60,false),Is.EqualTo(1));
            Assert.That(clip.FrameIndex(7.99,false),Is.EqualTo(479));
            Assert.That(clip.FrameIndex(8,false),Is.EqualTo(480));
            Assert.That(clip.FrameIndex(100,false),Is.EqualTo(480));
            Assert.That(clip.FrameIndex(8,true),Is.EqualTo(0));
            Assert.That(clip.FrameIndex(8.5,true),Is.EqualTo(30));
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }

        [Test]public void LongOriginalClipPreparationSelectsOnlyIntersectingCacheSegments()
        {
            int loads=ArtMemoryCache.TextureLoads;
            var art=BossRosterArt.For("B10");var clip=art["Idle"];
            Assert.That(clip.Count,Is.EqualTo(1402));
            Assert.That(clip.Sequences.Select(s=>s.Count),Is.EqualTo(new[]{600,600,202}));
            Assert.That(art.SequencesFor("Idle",0,0).Count(),Is.EqualTo(1));
            Assert.That(art.SequencesFor("Idle",9.99,.04).Select(s=>s.Key),Is.EqualTo(new[]{"Idle~000","Idle~001"}));
            Assert.That(art.SequencesFor("Idle",19.99,.04).Select(s=>s.Key),Is.EqualTo(new[]{"Idle~001","Idle~002"}));
            Assert.That(art.SequencesFor("Idle",clip.Duration-.01,.03,true).Select(s=>s.Key),Is.EqualTo(new[]{"Idle~002","Idle~000"}));
            Assert.That(art.SequencesFor("Idle",20,100,false).Select(s=>s.Key),Is.EqualTo(new[]{"Idle~002"}));
            Assert.That(art.SequencesFor(new[]{"Idle","Idle"}).Count(),Is.EqualTo(3));
            Assert.That(clip.ReadyFor(0,.1),Is.EqualTo(clip.Sequences.First().Ready));
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }

        [Test]public void AllApprovedModelsAndAuxiliariesKeepTheirActualLegacyCardBindings()
        {
            int loads=ArtMemoryCache.TextureLoads;int bodyClips=0;
            for(int i=1;i<=26;i++)
            {
                var art=BossRosterArt.For("B"+i.ToString("00"));
                Assert.That(art,Is.Not.Null);bodyClips+=art.SourceNames.Count();
                Assert.That(art.HasClip("idle"),Is.True);
                Assert.That(art["idle"].TransparentWait,Is.False);
                Assert.That(art.AllSequences.All(s=>s.Count<=600),Is.True);
            }
            bodyClips+=BossRosterArt.For("B20","back").SourceNames.Count();
            Assert.That(bodyClips,Is.EqualTo(393));
            Assert.That(BossRosterArt.For("P14").SourceNames.Count(),Is.EqualTo(8));
            Assert.That(BossRosterArt.For("AUX_P14H").SourceNames.Count(),Is.EqualTo(7));
            Assert.That(BossRosterArt.For("AUX_P14H")["Skill_Loop"].TransparentWait,Is.True);
            Assert.That(BossRosterArt.For("AUX_P20").SourceNames.Count(),Is.EqualTo(4));
            Assert.That(BossRosterArt.For("AUX_P26").SourceNames.Count(),Is.EqualTo(4));
            var additions=new[]{"P12I","P15C","P24D","P25M"};
            var models=new[]{"enemy_2094_skzamb","enemy_10262_dsjred","enemy_10263_dsjblk","enemy_10212_klhz"};
            var counts=new[]{4,4,4,5};
            for(int i=0;i<additions.Length;i++)
            {
                var auxiliary=BossRosterArt.For("AUX_"+additions[i]);
                Assert.That(auxiliary.Model,Is.EqualTo(models[i]));
                Assert.That(auxiliary.SourceNames.Count(),Is.EqualTo(counts[i]));
                Assert.That(auxiliary.HasClip("idle"),Is.True);
            }
            Assert.That(BossRosterArt.For("AUX_P12I").HasAttack,Is.False);
            Assert.That(BossRosterArt.For("AUX_P15C").HasZeroDurationState("Die_2"),Is.True);
            Assert.That(BossRosterArt.For("AUX_P24D").HasZeroDurationState("Die_2"),Is.True);
            Assert.That(BossRosterArt.For("B10").HasAttack,Is.False);
            Assert.That(BossRosterArt.For("B10").DeathDuration,Is.Zero);
            Assert.That(BossRosterArt.For("B11").HasClip("walk"),Is.False);
            Assert.That(BossRosterArt.For("B14").HasClip("walk"),Is.False);
            Assert.That(BossRosterArt.For("B21")["Idle"].TransparentWait,Is.True);
            Assert.That(BossRosterArt.For("B21")["idle"].Source,Is.EqualTo("Skill_Idle_A"));
            var cards=new[]{"CARD_M25","CARD_M32","CARD_TALULAH_TRIAL","CARD_M20","CARD_M14","CARD_M28","CARD_WISADEL_APRIL"};
            var tasks=new[]{"B02","B06","B16","B17","B18","B19","B20"};
            for(int i=0;i<cards.Length;i++)Assert.That(BossRosterArt.For(cards[i]),Is.SameAs(BossRosterArt.For(tasks[i])));
            Assert.That(BossRosterArt.For("CARD_M14").Model,Is.EqualTo("enemy_1510_frstar2"));
            Assert.That(BossRosterArt.For("CARD_WISADEL_APRIL").View,Is.EqualTo("front"));
            Assert.That(BossRosterArt.For("CARD_WISADEL_APRIL","back").HasClip("death"),Is.False);
            Assert.That(BossCoreArt.SequencesFor("CARD_WISADEL_APRIL").Count(),Is.EqualTo(BossRosterArt.For("B20").AllSequences.Count()+BossRosterArt.For("B20","back").AllSequences.Count()));
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }
    }
}
