using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class ArtLoadingTests
    {
        long budget;
        FormationBattleController controller;

        [SetUp]public void Setup(){budget=ArtMemoryCache.BudgetBytes;ArtMemoryCache.Clear();}
        [TearDown]public void Cleanup(){ArtMemoryCache.BudgetBytes=budget;ArtMemoryCache.Pin(Array.Empty<string>());}

        IEnumerator OpenBoard()
        {
            FormationShopSession.StartNew(20261117);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            controller=Object.FindAnyObjectByType<FormationBattleController>();
            Assert.That(controller,Is.Not.Null);
            double deadline=Time.realtimeSinceStartupAsDouble+60;
            while(!controller.Ready&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(controller.Ready,Is.True);
        }

        [UnityTest]public IEnumerator BoardMetadataAndPortraitDoNotDecodeBattleActions()
        {
            yield return OpenBoard();
            int before=ArtMemoryCache.TextureLoads;
            foreach(var option in FormationUnitCatalog.All)
                Assert.That(FormationUnitArt.For(option.CardId).HasAction("idle"),Is.True,option.CardId);
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(before));
            var card=controller.formationCards.Single(item=>item.CardId=="CARD_TALULAH_TRIAL");
            var portrait=card.Portrait;
            Assert.That(portrait,Is.Not.Null);
            Assert.That(portrait.name,Is.EqualTo("B16/portrait"));
            Assert.That(card.Portrait,Is.SameAs(portrait));
            Assert.That(ArtMemoryCache.TextureLoads-before,Is.LessThanOrEqualTo(1));
        }

        [Test]public void NonOriginalCardRetainsItsSerializedPortraitFallback()
        {
            var card=ScriptableObject.CreateInstance<CardDefinition>();
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            var sentinel=Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f));
            try
            {
                const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(CardDefinition).GetField("cardId",flags).SetValue(card,"PORTRAIT_FALLBACK_TEST");
                typeof(CardDefinition).GetField("portrait",flags).SetValue(card,sentinel);
                Assert.That(card.Portrait,Is.SameAs(sentinel));
            }
            finally{Object.Destroy(sentinel);Object.Destroy(texture);Object.Destroy(card);}
        }

        [UnityTest]public IEnumerator PreparedPixelsKeepFramesAndSafeEvictionReloadsOnlyUnusedClips()
        {
            var art=DefenderArt.Load("D01");
            int png=ArtMemoryCache.RuntimePngLoads,prepared=ArtMemoryCache.PreparedLoads;
            var frames=art["idle"].Frames;
            var first=frames[0];
            Assert.That(frames.Length,Is.GreaterThan(1));
            Assert.That(ArtMemoryCache.PreparedLoads,Is.GreaterThan(prepared));
            Assert.That(ArtMemoryCache.RuntimePngLoads,Is.EqualTo(png));
            Assert.That(first.texture.isReadable,Is.False);
            Assert.That(first.rect.height,Is.GreaterThan(32));
            var inactive=DefenderArt.Load("D04")["idle"].Frames[0];
            ArtMemoryCache.Pin(new[]{"DefenderArt/D01/idle"});
            ArtMemoryCache.BudgetBytes=1;
            ArtMemoryCache.Trim();
            yield return null;
            Assert.That(first!=null,Is.True,"The pinned board guard frame must survive an eviction pass.");
            Assert.That(inactive==null,Is.True);
            Assert.That(art["idle"].Frames[0],Is.SameAs(first));
            Assert.That(DefenderArt.Load("D04")["idle"].Frames[0]!=null,Is.True);
        }

        [UnityTest]public IEnumerator BoardShopIdleWindowStaysPinnedDuringCachePressure()
        {
            yield return OpenBoard();
            const string preview="B10";
            Sprite sprite=null;
            double deadline=Time.realtimeSinceStartupAsDouble+20;
            while(sprite==null&&Time.realtimeSinceStartupAsDouble<deadline)
            {
                sprite=controller.FormationShopIdleFrame(preview,.25);
                yield return null;
            }
            Assert.That(sprite,Is.Not.Null);
            Assert.That(FormationArtAssertions.IsActualIdleFrame(preview,sprite),Is.True);
            ArtMemoryCache.BudgetBytes=1;
            ArtMemoryCache.Trim();
            for(int frame=0;frame<8;frame++)
            {
                yield return null;
                Assert.That(controller.FormationShopIdleFrame(preview,.25),Is.SameAs(sprite));
            }
        }
    }
}
