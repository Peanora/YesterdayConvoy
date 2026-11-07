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
    public sealed class MudrockGrowthSceneTests
    {
        static readonly string[] Crew={"CARD_M26","CARD_M27","CARD_M42","CARD_M43","CARD_M28"};
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        ManualBattleController controller;
        float scale;
        [SetUp]public void Setup()=>scale=Time.timeScale;
        [TearDown]public void Cleanup()
        {if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();Time.timeScale=scale;}
        object Invoke(string name,params object[] args)=>typeof(ManualBattleController).GetMethod(name,Hidden).Invoke(controller,args);
        void Field(string name,object value)=>typeof(ManualBattleController).GetField(name,Hidden).SetValue(controller,value);
        IEnumerator Ready()
        {
            for(int i=0;i<1800&&(controller.ArtLoading||controller.ShopPanelTransitioning);i++)yield return null;
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False,"Preparation did not finish.");
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("MudrockCrewTrial");
            for(int i=0;i<1800;i++)
            {controller=Object.FindAnyObjectByType<ManualBattleController>();if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading)break;yield return null;}
            Assert.That(controller,Is.Not.Null);Assert.That(controller.SynergySandbox,Is.True);Time.timeScale=0;yield return Ready();
        }
        void Preset(bool full=false)
        {
            // Inventory is an explicit fixture. XP remains zero and all later growth uses
            // production battle/controller settlement; no training preset is selected.
            var ids=full?Crew.Concat(new[]{"CARD_M07","CARD_M08"}).ToArray():Crew.Take(2).ToArray();
            controller.Shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=full&&CampaignBalance.MudrockCrew(id)?2:1}),Array.Empty<string>(),8,0,100);
            var lineup=(List<string>)typeof(ManualBattleController).GetField("shopLineup",Hidden).GetValue(controller);
            lineup.Clear();lineup.AddRange(full?ids.Take(6):ids);Invoke("SyncShopDeck",false,false);controller.SetShopVisible(true);
            Field("campaignConstructOpen",false);Field("inspectedSynergy",null);
        }
        void ClickPiece(string id)
        {
            Field("campaignConstructOpen",false);Field("inspectedSynergy",null);
            int index=controller.Shop.Bench.ToList().FindIndex(p=>p.Id==id);Assert.That(index,Is.GreaterThanOrEqualTo(0));
            var point=controller.ShopPieceRect(index).center;controller.ProcessShopPointer(EventType.MouseDown,point);controller.ProcessShopPointer(EventType.MouseUp,point);
        }
        IEnumerator Start()
        {Assert.That(controller.StartLoopNextBattle(),Is.True);yield return Ready();controller.Battle.Begin();}
        void TerminalFixture(bool won=true)
        {
            // This fixture proves the absence of contact/deployment tasks and the actual
            // settlement connection. Natural battle completion is covered separately.
            var b=controller.Battle;
            Assert.That((bool)typeof(ManualBattle).GetMethod("FinishForTesting",Hidden).Invoke(b,new object[]{b.RunId,won}),Is.True);
            controller.Tick(0);
        }
        static Color32[] CaptureBar(Rect rect)
        {
            Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));
            var capture=ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                var viewport=TacticalViewport.Current;var bottom=viewport.ToScreen(new Vector2(rect.x,rect.yMax));
                int x=Mathf.RoundToInt(bottom.x),y=Mathf.RoundToInt(bottom.y),width=Mathf.Max(1,Mathf.RoundToInt(rect.width*viewport.Scale)),height=Mathf.Max(1,Mathf.RoundToInt(rect.height*viewport.Scale));
                Assert.That(x,Is.GreaterThanOrEqualTo(0));Assert.That(y,Is.GreaterThanOrEqualTo(0));
                Assert.That(x+width,Is.LessThanOrEqualTo(capture.width));Assert.That(y+height,Is.LessThanOrEqualTo(capture.height));
                return capture.GetPixels(x,y,width,height).Select(c=>(Color32)c).ToArray();
            }
            finally{Object.Destroy(capture);}
        }
        [UnityTest]public IEnumerator FreeSingleTransferSurvivesRealPromotionAndReserveButNotFinalSaleOrNewChallenge()
        {
            yield return Load();Preset(true);yield return Ready();Assert.That(controller.Loop.GolemExperience,Is.Zero);
            Assert.That(controller.SetMudrockTransfer("CARD_M26",true),Is.False);
            Assert.That(controller.SetMudrockTransfer("CARD_M03",true),Is.False,"An unowned kind cannot be designated.");
            int gold=controller.Shop.Gold;Assert.That(controller.SetMudrockTransfer("CARD_M07",true),Is.True);
            Assert.That(controller.SetMudrockTransfer("CARD_M07",true),Is.True);Assert.That(controller.SetMudrockTransfer("CARD_M08",true),Is.False);
            Assert.That(controller.Shop.Gold,Is.EqualTo(gold));Assert.That(controller.Battle.MudrockTier,Is.EqualTo(6));
            Assert.That(SynergyBadgePresentation.GradeFor("泥岩小队",controller.Battle.MudrockTier),Is.EqualTo(SynergyVisualGrade.Prismatic));
            Assert.That(controller.Battle.MudrockTotalStars,Is.EqualTo(11));Assert.That(controller.Battle.GolemCapacity,Is.EqualTo(4));
            Assert.That(controller.SetGuerrillaTransfer("CARD_M07",true),Is.True,"The two designation systems retain their separate slots.");
            Assert.That(controller.IsMudrockTransfer("CARD_M07"),Is.True);Assert.That(controller.IsGuerrillaTransfer("CARD_M07"),Is.True);
            ClickPiece("CARD_M07");Assert.That(controller.IsMudrockTransfer("CARD_M07"),Is.True);
            Assert.That(controller.PreparedMudrockTransferIds,Is.Empty);Assert.That(controller.Battle.MudrockCrewCount,Is.EqualTo(5));
            Assert.That(SynergyBadgePresentation.GradeFor("泥岩小队",controller.Battle.MudrockTier),Is.EqualTo(SynergyVisualGrade.Gold));
            ClickPiece("CARD_M07");Assert.That(controller.Battle.MudrockCrewCount,Is.EqualTo(6));
            Assert.That(SynergyBadgePresentation.GradeFor("泥岩小队",controller.Battle.MudrockTier),Is.EqualTo(SynergyVisualGrade.Prismatic));
            Assert.That(controller.CommissionCampaignCard("CARD_M07").Success,Is.True);
            Assert.That(controller.CommissionCampaignCard("CARD_M07").Success,Is.True);
            Assert.That(controller.Shop.Bench.Single(p=>p.Id=="CARD_M07").Star,Is.EqualTo(2));
            Assert.That(controller.IsMudrockTransfer("CARD_M07"),Is.True);Assert.That(controller.Battle.MudrockTotalStars,Is.EqualTo(12));
            Assert.That(controller.Battle.GolemCapacity,Is.EqualTo(5));Assert.That(controller.Battle.GolemStage,Is.EqualTo(1),"Capacity is not a free XP grant.");
            Assert.That(controller.SellShopCard(controller.Shop.Bench.Single(p=>p.Id=="CARD_M07").Instance).Success,Is.True);
            Assert.That(controller.IsMudrockTransfer("CARD_M07"),Is.False);Assert.That(controller.MudrockTransferIds,Is.Empty);
            Assert.That(controller.SetMudrockTransfer("CARD_M08",true),Is.True);
            yield return controller.PrepareCampaign(true,9907);
            Assert.That(controller.MudrockTransferIds,Is.Empty);Assert.That(controller.Loop.GolemExperience,Is.Zero);
        }
        [UnityTest]public IEnumerator SandboxRoundsAutomaticallyGrowWithoutDeploymentStopAtCapAndResumeAfterRealRosterProgress()
        {
            yield return Load();Preset();yield return Ready();
            for(int round=1;round<=3;round++)
            {
                Assert.That(controller.Loop.SandboxRound,Is.EqualTo(round));yield return Start();
                Assert.That(controller.Battle.Units,Is.Empty);Assert.That(controller.SetMudrockTransfer("CARD_M07",true),Is.False);
                TerminalFixture(round!=2);
                Assert.That(controller.Loop.GolemExperience,Is.EqualTo(round==1?3:6));
                Assert.That(controller.Loop.LastGolemGrowth,Is.EqualTo(round<=2?3:0));
                Assert.That(controller.Loop.Reward.Key,Is.EqualTo("sandbox-round-"+round));
                controller.Tick(0);Assert.That(controller.Loop.GolemExperience,Is.EqualTo(round==1?3:6));
                Invoke("ReplaySandbox");yield return Ready();
            }
            Assert.That(controller.Battle.GolemStage,Is.EqualTo(2));Assert.That(controller.Battle.GolemExperience,Is.EqualTo(6));
            Preset(true);yield return Ready();Assert.That(controller.SetMudrockTransfer("CARD_M07",true),Is.True);
            Assert.That(controller.CommissionCampaignCard("CARD_M07").Success,Is.True);Assert.That(controller.CommissionCampaignCard("CARD_M07").Success,Is.True);
            Assert.That(controller.Battle.GolemCapacity,Is.EqualTo(5));Assert.That(controller.Loop.GolemExperience,Is.EqualTo(6));
            yield return Start();var members=controller.Battle.MudrockMemberIds.ToArray();
            Assert.That(controller.SetMudrockTransfer("CARD_M07",false),Is.False);CollectionAssert.AreEqual(members,controller.Battle.MudrockMemberIds);
            TerminalFixture();Assert.That(controller.Loop.GolemExperience,Is.EqualTo(18));
            Invoke("ReplaySandbox");yield return Ready();Assert.That(controller.Battle.GolemStage,Is.EqualTo(3));
            Assert.That(controller.IsMudrockTransfer("CARD_M07"),Is.True);
            for(int i=0;i<3;i++){yield return Start();TerminalFixture();Invoke("ReplaySandbox");yield return Ready();}
            Assert.That(controller.Loop.GolemExperience,Is.EqualTo(48));Assert.That(controller.Battle.GolemStage,Is.EqualTo(5));
            yield return Start();controller.Battle.SetPaused(true);
            var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(controller.Battle.Entry));
            controller.ProcessShopPointer(EventType.MouseDown,ManualBattleController.GolemCardRect.center);
            controller.ProcessShopPointer(EventType.MouseDrag,gate);controller.ProcessShopPointer(EventType.MouseUp,gate);
            var golem=controller.Battle.Units.Single(u=>u.Golem);Assert.That(golem.MaxHealth,Is.EqualTo(7500));
            Assert.That(controller.Battle.UnitCombat(golem).UnitAttack,Is.EqualTo(2000));Assert.That(controller.Battle.DP,Is.EqualTo(86));
            controller.Tick(1);Assert.That(controller.Battle.Clock,Is.Zero);Assert.That(controller.Battle.UnitControlImmune(golem),Is.True);
        }
        [UnityTest]public IEnumerator SettlementRendersRealExperienceFillLevelUpAndCapExplanation()
        {
            yield return Load();Preset();yield return Ready();controller.OpenGolemTrainingPanel();Assert.That(controller.GolemTrainingPanelVisible,Is.True);
            Field("campaignConstructOpen",false);yield return Start();TerminalFixture();
            Assert.That(controller.GolemFeedbackEarned,Is.EqualTo(3));Assert.That(controller.GolemFeedbackBeforeStage,Is.EqualTo(1));
            yield return new WaitForEndOfFrame();var before=CaptureBar(ManualBattleController.GolemSettlementBarRect);
            yield return new WaitForSecondsRealtime(1.05f);yield return new WaitForEndOfFrame();var after=CaptureBar(ManualBattleController.GolemSettlementBarRect);
            Assert.That(controller.GolemDisplayedExperience,Is.EqualTo(3).Within(.001));Assert.That(controller.GolemFeedbackAnimating,Is.False);
            Assert.That(before.Length,Is.EqualTo(after.Length));
            int changed=before.Where((c,i)=>Math.Abs(c.r-after[i].r)+Math.Abs(c.g-after[i].g)+Math.Abs(c.b-after[i].b)>20).Count();
            Assert.That(changed,Is.GreaterThan(20),"The actual OnGUI XP bar must contribute changing pixels, not merely update a field.");
            Invoke("ReplaySandbox");yield return Ready();yield return Start();TerminalFixture();
            Assert.That(controller.GolemFeedbackBeforeStage,Is.EqualTo(1));Assert.That(controller.GolemFeedbackAfterStage,Is.EqualTo(2));
            Assert.That(controller.GolemFeedbackEarned,Is.EqualTo(3));
            yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(controller.GolemDisplayedExperience,Is.EqualTo(6).Within(.001));Assert.That(controller.GolemFeedbackText,Does.Contain("II"));
            Invoke("ReplaySandbox");yield return Ready();yield return Start();TerminalFixture();yield return new WaitForEndOfFrame();
            Assert.That(controller.GolemFeedbackEarned,Is.Zero);Assert.That(controller.GolemFeedbackText+controller.GolemCapacityText,Does.Contain("上限"));
            Assert.That(controller.GolemDisplayedExperience,Is.EqualTo(6));
        }
        [UnityTest,Category("MudrockNatural")]public IEnumerator DedicatedTrialCanNaturallyGrowFromZeroToFiveUsingOnlyNormalRecruitmentAndCombat()
        {
            yield return Load();Assert.That(controller.Loop.GolemExperience,Is.Zero);Assert.That(controller.Battle.GolemStage,Is.EqualTo(1));
            Assert.That(controller.MudrockTransferIds,Is.Empty);Assert.That(controller.Shop.Bench.All(p=>p.Star==1),Is.True);
            Assert.That(controller.SetMudrockTransfer("CARD_M08",true),Is.True);
            foreach(string id in Crew.Concat(new[]{"CARD_M08"}))
            {
                Assert.That(controller.CommissionCampaignCard(id).Success,Is.True,id+" first real recruitment");
                Assert.That(controller.CommissionCampaignCard(id).Success,Is.True,id+" second real recruitment");
                Assert.That(controller.Shop.Bench.Single(p=>p.Id==id).Star,Is.EqualTo(2));
            }
            Assert.That(controller.Battle.GolemCapacity,Is.EqualTo(5));Assert.That(controller.Loop.GolemExperience,Is.Zero,"Promotion must not manufacture training XP.");
            string[] order={"CARD_M28","CARD_M43","CARD_M27","CARD_M26","CARD_M42","CARD_M08"};long request=990000;
            for(int round=1;round<=4;round++)
            {
                yield return Start();var b=controller.Battle;
                foreach(string id in order)Assert.That(b.TryDeployRole(b.RunId,++request,b.Entry,id).Success,Is.True);
                Assert.That(b.TryDeployGolem(b.RunId,++request,b.Entry).Success,Is.True);
                for(int tick=0;tick<12000&&controller.Loop.CanDeploy;tick++)
                {
                    if(tick>0&&tick%40==0)foreach(string id in order)b.TryDeployRole(b.RunId,++request,b.Entry,id);
                    controller.Tick(.05f);if(tick%10==0)yield return null;
                }
                Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won),"Unmodified authored defenders and normal deployment must allow natural round "+round);
                Assert.That(controller.Loop.LastGolemGrowth,Is.EqualTo(12));Assert.That(controller.Loop.GolemExperience,Is.EqualTo(12*round));
                Assert.That(b.Units.Any(u=>u.Golem),Is.True);Assert.That(b.CombatShots.Any(s=>!s.FromDefender&&s.Applied),Is.True);
                Invoke("ReplaySandbox");yield return Ready();
            }
            Assert.That(controller.Battle.GolemStage,Is.EqualTo(5));Assert.That(controller.Battle.GolemCapacity,Is.EqualTo(5));
        }
    }
}
