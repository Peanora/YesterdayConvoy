using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class GuerrillaHighTierSceneTests
    {
        static readonly string[] Roster={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M07","CARD_M08"};
        static readonly string[] NativeSlots={"CARD_M15","CARD_M16","CARD_M03","CARD_M20","CARD_M19","CARD_M18","CARD_M17"};
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        ManualBattleController controller;
        float scale;
        long request=970000;
        [SetUp]public void Setup()=>scale=Time.timeScale;
        [TearDown]public void Cleanup()
        {if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();Time.timeScale=scale;}
        object Invoke(string name,params object[] args)=>typeof(ManualBattleController).GetMethod(name,Hidden).Invoke(controller,args);
        void Field(string name,object value)=>typeof(ManualBattleController).GetField(name,Hidden).SetValue(controller,value);
        IEnumerator WaitForPreparation()
        {
            for(int i=0;i<1800&&(controller.ArtLoading||controller.ShopPanelTransitioning);i++)yield return null;
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("GuerrillaTrial");
            for(int i=0;i<1800;i++)
            {controller=Object.FindAnyObjectByType<ManualBattleController>();if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading)break;yield return null;}
            Assert.That(controller,Is.Not.Null);Assert.That(controller.SynergySandbox,Is.True);Time.timeScale=0;
            yield return WaitForPreparation();
        }
        void Preset(bool reserve=false)
        {
            // Explicit inventory fixture; no battle resource, contact, receipt or hit is manufactured.
            controller.Shop.Preset(Roster.Concat(reserve?new[]{"CARD_M05"}:Array.Empty<string>()).Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),9,0,100);
            var lineup=(List<string>)typeof(ManualBattleController).GetField("shopLineup",Hidden).GetValue(controller);
            lineup.Clear();lineup.AddRange(Roster);Invoke("SyncShopDeck",false,false);controller.SetShopVisible(true);
            Field("inspectedSynergy",null);
        }
        void ClickPiece(string id)
        {
            int index=controller.Shop.Bench.ToList().FindIndex(p=>p.Id==id);Assert.That(index,Is.GreaterThanOrEqualTo(0));
            var point=controller.ShopPieceRect(index).center;
            controller.ProcessShopPointer(EventType.MouseDown,point);controller.ProcessShopPointer(EventType.MouseUp,point);
        }
        IEnumerator Start(int tier)
        {
            Assert.That(controller.StartLoopNextBattle(),Is.True);yield return WaitForPreparation();
            var b=controller.Battle;Assert.That(b.GuerrillaTier,Is.EqualTo(tier));Assert.That(b.Orders,Is.Zero);
            b.Begin();Assert.That(b.Orders,Is.EqualTo(2));
            // Durable, non-attacking guards preserve saved routes/positions. Ordinary attackers
            // still deploy, travel, contact and earn their own finite per-entity credit.
            foreach(var d in b.Defenders)
            {
                typeof(DefenderState).GetProperty("Health").SetValue(d,1000000f);
                typeof(DefenderState).GetProperty("MaxHealth").SetValue(d,1000000f);
                typeof(DefenderState).GetField("NextAttackTime",Hidden).SetValue(d,1000000d);
                typeof(DefenderState).GetProperty("SkillSP").SetValue(d,-1000f);
            }
            foreach(var device in b.Gramophones)
            {
                typeof(GramophoneState).GetField("NextAttack",Hidden).SetValue(device,b.Clock+1000);
                typeof(GramophoneState).GetField("NextHeal",Hidden).SetValue(device,b.Clock+1000);
            }
        }
        IEnumerator Deploy(string id)
        {
            for(int i=0;i<240;i++)
            {
                if(controller.Battle.TryDeployRole(controller.Battle.RunId,++request,controller.Battle.Entry,id).Success)yield break;
                controller.Tick(.05f);if(i%10==0)yield return null;
            }
            Assert.Fail("Real deployment failed: "+id);
        }
        void StopOrdinary()
        {foreach(var u in controller.Battle.Units)typeof(DeployedUnitState).GetField("NextAttackTime",Hidden).SetValue(u,controller.Battle.Clock+1000);}
        void AdvanceTo(GuerrillaOrderView view,float age)
        {
            float delta=age-view.Age;Assert.That(delta,Is.GreaterThanOrEqualTo(-.0001f));
            controller.Tick(Mathf.Max(0,delta)/Mathf.Min(1,controller.EffectiveSimulationSpeed));
        }
        static Color32[] Capture(GuerrillaOrderView view)
        {
            Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null),"Requires actual GPU rendering.");
            var target=new RenderTexture(640,360,0,RenderTextureFormat.ARGB32);
            var image=new Texture2D(640,360,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            try
            {
                target.Create();view.RenderTo(target);RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();return image.GetPixels32();
            }
            finally{RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(image);}
        }
        static int PortraitPixelDifference(Color32[] a,Color32[] b,int slot)
        {
            var point=new Vector2(slot==7?.61f:.39f,.77f);int changed=0;
            for(int y=(int)((1-point.y-.07f)*360);y<(int)((1-point.y+.07f)*360);y++)
                for(int x=(int)((point.x-.07f)*640);x<(int)((point.x+.07f)*640);x++)
                {
                    int i=y*640+x;
                    if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)+Math.Abs(a[i].a-b[i].a)>24)changed++;
                }
            return changed;
        }
        [UnityTest]public IEnumerator TransferSelectionUsesOwnedKindsAndSurvivesPromotionButNotFinalSaleOrNewChallenge()
        {
            yield return Load();Preset(true);yield return WaitForPreparation();
            Assert.That(controller.SetGuerrillaTransfer("CARD_M03",true),Is.False,"Native kinds cannot consume a transfer slot.");
            Assert.That(controller.SetGuerrillaTransfer("CARD_M07",true),Is.True);
            Assert.That(controller.SetGuerrillaTransfer("CARD_M07",true),Is.True);
            Assert.That(controller.SetGuerrillaTransfer("CARD_M08",true),Is.True);
            Assert.That(controller.GuerrillaTransferIds.Count,Is.EqualTo(2));
            Assert.That(controller.SetGuerrillaTransfer("CARD_M05",true),Is.False);
            Assert.That(controller.Battle.GuerrillaTier,Is.EqualTo(9));
            ClickPiece("CARD_M08");
            Assert.That(controller.IsGuerrillaTransfer("CARD_M08"),Is.True,"Owned reserve keeps its designation.");
            Assert.That(controller.PreparedGuerrillaTransferIds.Contains("CARD_M08"),Is.False,"Reserve does not count in the battle snapshot.");
            Assert.That(controller.Battle.GuerrillaTier,Is.EqualTo(8));
            Assert.That(controller.CommissionCampaignCard("CARD_M07").Success,Is.True);
            Assert.That(controller.CommissionCampaignCard("CARD_M07").Success,Is.True);
            Assert.That(controller.Shop.Bench.Single(p=>p.Id=="CARD_M07").Star,Is.EqualTo(2));
            Assert.That(controller.IsGuerrillaTransfer("CARD_M07"),Is.True);
            Assert.That(controller.PreparedGuerrillaTransferIds.Contains("CARD_M07"),Is.True);
            foreach(var p in controller.Shop.Bench.Where(p=>p.Id=="CARD_M07").ToArray())Assert.That(controller.SellShopCard(p.Instance).Success,Is.True);
            Assert.That(controller.IsGuerrillaTransfer("CARD_M07"),Is.False);
            yield return controller.PrepareCampaign(true,9711);
            Assert.That(controller.GuerrillaTransferIds,Is.Empty,"A new challenge cannot inherit the old designations.");
        }
        [UnityTest]public IEnumerator TransferSnapshotLocksInCombatAndPersistsAcrossRealCampaignStageTransition()
        {
            yield return Load();yield return controller.PrepareCampaign(false,9712);
            // The first two ordinary campaign encounters are guaranteed supply stops.
            // Claim them through the actual supply transaction, then test a real fighting stage.
            while(controller.Loop.RewardStage)
            {
                if(controller.Loop.CanShop){Assert.That(controller.StartLoopNextBattle(),Is.True);yield return WaitForPreparation();}
                Assert.That(controller.Battle.GuaranteedSupply,Is.True);controller.ClaimCampaignSupply();
                Assert.That(controller.Battle.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(controller.OpenLoopShop(),Is.True);
                yield return WaitForPreparation();
            }
            Assert.That(controller.Loop.Stage,Is.EqualTo(3));Preset();yield return WaitForPreparation();
            Assert.That(controller.SetGuerrillaTransfer("CARD_M07",true),Is.True);Assert.That(controller.SetGuerrillaTransfer("CARD_M08",true),Is.True);
            Assert.That(controller.StartLoopNextBattle(),Is.True);yield return WaitForPreparation();controller.Battle.Begin();
            var members=controller.Battle.GuerrillaMemberIds.ToArray();
            Assert.That(controller.SetGuerrillaTransfer("CARD_M07",false),Is.False);
            CollectionAssert.AreEqual(members,controller.Battle.GuerrillaMemberIds);
            Field("loopTestSettlementOpen",true);Assert.That((bool)Invoke("TryTestSettlement",true),Is.True);
            Assert.That(controller.OpenLoopShop(),Is.True);
            CollectionAssert.AreEquivalent(new[]{"CARD_M07","CARD_M08"},controller.GuerrillaTransferIds);
            Assert.That(controller.Battle.GuerrillaTier,Is.EqualTo(9));
        }
        [UnityTest]public IEnumerator NineTierEarlyOrderHasRealExtensionPortraitsGlobalDamageFixedSpeedAndFiniteIncomeBeyondFifthCast()
        {
            yield return Load();Preset();yield return WaitForPreparation();
            Assert.That(controller.SetGuerrillaTransfer("CARD_M07",true),Is.True);Assert.That(controller.SetGuerrillaTransfer("CARD_M08",true),Is.True);
            yield return Start(9);var b=controller.Battle;var view=controller.GetComponent<GuerrillaOrderView>();
            controller.Tick(.05f);Assert.That(b.LatestGuerrillaOrder,Is.Null,"No deployed source may spend the initial points.");Assert.That(b.Orders,Is.EqualTo(2));
            yield return Deploy("CARD_M03");Assert.That(b.Units.Single().OrdinaryAttacksFired,Is.Zero);StopOrdinary();
            Assert.That(b.LatestGuerrillaOrder,Is.Null,"Deployment alone does not advance the simulation.");
            controller.Tick(.05f);var volley=b.LatestGuerrillaOrder;
            Assert.That(volley,Is.Not.Null,"The next positive simulation step automatically spends the full starting order.");
            Assert.That(b.Orders,Is.Zero);Assert.That(view.PlayCount,Is.EqualTo(1));Assert.That(volley.Age,Is.EqualTo(0).Within(.0001));
            Assert.That(controller.UseCampaignOrder(true),Is.False);
            var hp=b.Defenders.Where(d=>d.Alive).ToDictionary(d=>d.EntityId,d=>d.Health);
            CollectionAssert.AreEqual(NativeSlots,view.SlotMembers.Take(7));CollectionAssert.AreEqual(new[]{"CARD_M07","CARD_M08"},view.SlotMembers.Skip(7));
            AdvanceTo(view,.65f);yield return null;yield return null;Assert.That(view.VisiblePortraitCount,Is.EqualTo(9));
            AdvanceTo(view,.85f);Assert.That(volley.CompletedStrikes,Is.Zero);Assert.That(volley.Hits,Is.Empty);
            b.SetPaused(true);controller.Tick(3);Assert.That(view.Age,Is.EqualTo(.85f).Within(.0001));Assert.That(hp.All(pair=>b.FindDefender(pair.Key).Health==pair.Value),Is.True);
            b.SetPaused(false);if(controller.SimulationSpeed!=2)controller.ToggleSpeed();double clock=b.Clock;
            controller.Tick(.04f);Assert.That(b.Clock-clock,Is.EqualTo(.08).Within(.0001));Assert.That(view.Age,Is.EqualTo(.89f).Within(.0001));
            controller.Tick(.02f);Assert.That(volley.CompletedStrikes,Is.EqualTo(1));Assert.That(volley.Resolved,Is.False);
            foreach(var hit in volley.Strikes[0].Hits)Assert.That(hp[hit.TargetId]-b.FindDefender(hit.TargetId).Health,Is.EqualTo(hit.Damage).Within(.01f));
            AdvanceTo(view,1.05f);Capture(view);yield return null;
            var original=controller.GetComponents<AudioSource>().Single(source=>source.clip==Resources.Load<AudioClip>("GuerrillaOrder/followup-original"));
            var slash=controller.GetComponents<AudioSource>().Single(source=>source.clip==Resources.Load<AudioClip>("GuerrillaOrder/five-slash-charge"));
            Assert.That(original.pitch,Is.EqualTo(1));Assert.That(slash.pitch,Is.EqualTo(1));
            b.SetPaused(true);Capture(view);int originalSample=original.timeSamples,slashSample=slash.timeSamples;float frozen=view.Age;
            controller.Tick(3);yield return new WaitForSecondsRealtime(.06f);
            Assert.That(original.timeSamples,Is.EqualTo(originalSample));Assert.That(slash.timeSamples,Is.EqualTo(slashSample));
            Assert.That(view.SoundPaused&&view.SlashSoundPaused,Is.True);Assert.That(view.Age,Is.EqualTo(frozen));
            b.SetPaused(false);AdvanceTo(view,2.4f);Capture(view);
            Assert.That(volley.CompletedStrikes,Is.EqualTo(4));Assert.That(view.DisplayedStrikeCount,Is.EqualTo(4));Assert.That(view.ChargeProgress,Is.GreaterThan(0));
            b.SetPaused(true);Capture(view);slashSample=slash.timeSamples;controller.Tick(3);yield return new WaitForSecondsRealtime(.06f);
            Assert.That(view.SlashSoundPaused,Is.True);Assert.That(slash.timeSamples,Is.EqualTo(slashSample));
            b.SetPaused(false);AdvanceTo(view,2.81f);Capture(view);
            Assert.That(volley.Resolved&&!volley.Cancelled,Is.True);Assert.That(volley.CompletedStrikes,Is.EqualTo(5));
            Assert.That(volley.Strikes[4].Hits.Sum(hit=>hit.Damage),Is.GreaterThan(volley.Strikes[0].Hits.Sum(hit=>hit.Damage)*2.5f));
            Assert.That(view.DisplayedStrikeCount,Is.EqualTo(5));Assert.That(view.DisplayedDamage,Is.EqualTo(volley.Hits.Sum(hit=>hit.Damage)).Within(.01));
            AdvanceTo(view,GuerrillaOrderVolley.PresentationDuration+.01f);Capture(view);b.Begin();Assert.That(b.Orders,Is.Zero,"Begin cannot replenish a spent starting grant.");
            foreach(var unit in b.Units)typeof(DeployedUnitState).GetField("NextAttackTime",Hidden).SetValue(unit,0d);
            foreach(string id in Roster.Where(id=>id!="CARD_M20"&&id!="CARD_M03")){yield return Deploy(id);controller.Tick(.2f);}
            foreach(string id in new[]{"CARD_M07","CARD_M08"}){yield return Deploy(id);controller.Tick(.2f);}
            for(int i=0;i<3200&&b.OrdersUsed<5&&b.Phase==BattlePhase.Running;i++)
            {controller.Tick(.05f);if(i%10==0)yield return null;}
            Assert.That(b.OrdersUsed,Is.GreaterThanOrEqualTo(5),"Real finite contact income must automatically support the fifth full order, beyond the old cap.");
            Assert.That(b.CombatShots.Any(shot=>!shot.FromDefender&&!shot.SynergyOrder&&shot.Applied),Is.True);
            StopOrdinary();
            for(int i=0;i<3200&&(b.Orders>=2||b.GuerrillaOrderBusy||b.CombatShots.Any(shot=>!shot.Resolved));i++)
            {controller.Tick(.05f);if(i%10==0)yield return null;}
            Assert.That(b.Orders,Is.LessThan(2));int points=b.Orders,used=b.OrdersUsed,plays=view.PlayCount;
            controller.Tick(1);Assert.That(b.OrdersUsed,Is.EqualTo(used));Assert.That(b.Orders,Is.EqualTo(points));Assert.That(view.PlayCount,Is.EqualTo(plays));
            Assert.That(b.Orders+2*b.OrdersUsed-2,Is.InRange(8,2*Roster.Length),"Automatic orders cannot recursively manufacture contact points.");
        }
        [UnityTest]public IEnumerator EightTierNeedsOneRealContactBeforeSpendingItsStartingTwoPoints()
        {
            yield return Load();Preset();yield return WaitForPreparation();Assert.That(controller.SetGuerrillaTransfer("CARD_M07",true),Is.True);
            Assert.That(controller.SetGuerrillaTransfer("CARD_M08",true),Is.True);ClickPiece("CARD_M07");
            yield return Start(8);var b=controller.Battle;var view=controller.GetComponent<GuerrillaOrderView>();yield return Deploy("CARD_M03");
            Assert.That(controller.UseCampaignOrder(false),Is.False);Assert.That(b.Orders,Is.EqualTo(2));Assert.That(view.PlayCount,Is.Zero);
            for(int i=0;i<1800&&b.LatestGuerrillaOrder==null&&b.Phase==BattlePhase.Running;i++){controller.Tick(.02f);if(i%10==0)yield return null;}
            Assert.That(b.LatestGuerrillaOrder,Is.Not.Null,"The first real contact supplies point3 and automatically commits the full order.");
            Assert.That(b.OrdersUsed,Is.EqualTo(1));Assert.That(controller.UseCampaignOrder(true),Is.False);Assert.That(b.Orders,Is.Zero);
            Assert.That(GuerrillaRules.ForTier(b.GuerrillaTier).AttackMultiplier,Is.EqualTo(1.8f));
            Assert.That(b.LatestGuerrillaOrder.SourceIds,Is.Not.Empty);
            Assert.That(view.SlotMembers[7],Is.Null,"The first selected transfer kept in reserve leaves its own fixed slot empty.");
            Assert.That(view.SlotMembers[8],Is.EqualTo("CARD_M08"),"The second transfer must not shift into the first slot.");
        }
        [UnityTest]public IEnumerator TalulahTransferUsesPreparedOriginalPortraitInTheActualOrderAndRenderedExtension()
        {
            const string talulah="CARD_TALULAH_TRIAL";
            yield return Load();var roster=Roster.Select(id=>id=="CARD_M07"?talulah:id).ToArray();
            // Only the owned inventory is a fixture. Art preparation, transfer selection,
            // deployment and the successful order all use their production paths.
            controller.Shop.Preset(roster.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),9,0,100);
            var lineup=(List<string>)typeof(ManualBattleController).GetField("shopLineup",Hidden).GetValue(controller);
            lineup.Clear();lineup.AddRange(roster);Invoke("SyncShopDeck",false,false);controller.SetShopVisible(true);Field("inspectedSynergy",null);
            yield return WaitForPreparation();
            yield return (IEnumerator)Invoke("Prepare",Invoke("CurrentBattlePlan"),null,"准备塔露拉转职回归");
            Assert.That(controller.ArtLoading,Is.False);Assert.That(controller.Loop.CanShop,Is.True);
            Assert.That(controller.SetGuerrillaTransfer(talulah,true),Is.True);
            Assert.That(controller.SetGuerrillaTransfer("CARD_M08",true),Is.True);
            Invoke("PrepareNextLoopBattle");yield return WaitForPreparation();
            Assert.That(controller.Loop.CanShop,Is.False,"The normal preparation completion must enter the encounter.");
            var b=controller.Battle;Assert.That(b.GuerrillaTier,Is.EqualTo(9));Assert.That(b.Orders,Is.Zero);
            b.Begin();Assert.That(b.Orders,Is.EqualTo(2));
            yield return Deploy("CARD_M03");StopOrdinary();
            Assert.That(b.Units.Single().OrdinaryAttacksFired,Is.Zero);
            controller.Tick(.05f);Assert.That(b.LatestGuerrillaOrder,Is.Not.Null,"The nine-member early full order is automatic.");Assert.That(b.OrdersUsed,Is.EqualTo(1));
            var view=controller.GetComponent<GuerrillaOrderView>();AdvanceTo(view,.65f);
            var definitions=(Dictionary<string,ManualBattleDefinition>)typeof(ManualBattleController).GetField("shopDefinitions",Hidden).GetValue(controller);
            var card=definitions[talulah].Card;
            var originalType=typeof(CardDefinition).Assembly.GetType("YesterdayConvoy.OriginalTalulahArt",true);
            var original=originalType.GetMethod("For",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{card});
            Assert.That(original,Is.Not.Null);
            var expected=(Sprite)originalType.GetProperty("Portrait",Hidden).GetValue(original);
            var serialized=(Sprite)typeof(CardDefinition).GetField("portrait",Hidden).GetValue(card);
            Assert.That(expected,Is.Not.Null);Assert.That(expected.name,Is.EqualTo("TalulahOriginal/portrait"));
            Assert.That(serialized,Is.Not.Null);Assert.That(serialized,Is.Not.SameAs(expected),"Keep the genuine previous serialized art as the negative control.");
            Assert.That(card.Portrait,Is.SameAs(expected));
            int slot=view.SlotMembers.ToList().IndexOf(talulah);Assert.That(slot,Is.InRange(7,8));
            var actualSprites=(Sprite[])typeof(GuerrillaOrderView).GetField("extensions",Hidden).GetValue(view);
            Assert.That(actualSprites[slot-7],Is.SameAs(expected),"The real controller bridge must supply the original portrait.");
            Assert.That(view.VisiblePortraitCount,Is.EqualTo(9));var actual=Capture(view);
            var portraits=new Dictionary<string,Sprite>{{talulah,expected},{"CARD_M08",definitions["CARD_M08"].Card.Portrait}};
            var members=b.GuerrillaMemberIds.ToArray();var assignments=controller.GuerrillaTransferIds.ToArray();
            view.Play(members,portraits,assignments);view.AdvancePresentation(.65f);var originalPixels=Capture(view);
            CollectionAssert.AreEqual(originalPixels,actual,"The actual order pixels must match an explicitly supplied original portrait at the same animation age.");
            portraits[talulah]=serialized;view.Play(members,portraits,assignments);view.AdvancePresentation(.65f);var legacyPixels=Capture(view);
            Assert.That(PortraitPixelDifference(originalPixels,legacyPixels,slot),Is.GreaterThan(20),"The Talulah extension must visibly differ from the previous serialized portrait.");
        }
    }
}
