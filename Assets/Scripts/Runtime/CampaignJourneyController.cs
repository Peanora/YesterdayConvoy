using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public bool JourneyActive=>CampaignJourneySession.Current!=null&&ReferenceEquals(Loop,CampaignJourneySession.Current.Loop)&&Loop!=null;
        public CampaignJourneyStage JourneyStage=>JourneyActive?CampaignJourneyCatalog.Stage(Loop.Stage):null;
        public ApprovedMapDefinition JourneyMap=>JourneyActive?CampaignJourneySession.Current.Map:null;
        bool CanOperateJourneyShop=>JourneyActive&&Loop.CanShop&&Battle.Phase==BattlePhase.Ready&&!ArtLoading;
        public ShopTrialModel.Result UpgradeJourneyShop()
        {
            if(!CanOperateJourneyShop)return new ShopTrialModel.Result{Message="仅整备时可购买经验"};
            var result=shop.Upgrade();if(result.Success){shopUpgradeAt=Time.unscaledTime;SyncShopDeck();}ShopNotice(result.Message);return result;
        }
        public ShopTrialModel.Result RefreshJourneyShop()
        {
            if(!CanOperateJourneyShop)return new ShopTrialModel.Result{Message="仅整备时可刷新"};
            var result=shop.Refresh();if(result.Success)shopRefreshAt=Time.unscaledTime;ShopNotice(result.Message);return result;
        }
        public bool LockJourneyShop(bool value){if(!CanOperateJourneyShop)return false;shop.Locked=value;return true;}
        public void BeginJourney(int seed=0)
        {
            CancelArtPreparation();CampaignJourneyCatalog.Current.Validate();
            var session=CampaignJourneySession.Begin(seed);
            SceneManager.LoadScene(session.Map.sceneName);
        }
        public void ExitJourney()
        {
            CancelArtPreparation();CampaignJourneySession.Clear();
            SceneManager.LoadScene("MonsterTrial");
        }
        void CaptureJourney()
        {
            if(!JourneyActive)return;
            var session=CampaignJourneySession.Current;
            session.Shop=shop;session.Loop=Loop;session.Seats=shopSeats;
            session.Lineup.Clear();session.Lineup.AddRange(shopLineup);
            session.Contract=contractCard;session.Commission=commissionTarget;
            session.InfectionPrimary=infectionForm;session.InfectionSecondary=infectionSecond;
            session.MarinePrimary=marineForm;session.MarineSecondary=marineSecond;
            session.ReunionRanged=reunionRanged;session.InfectionChoiceTier=lastInfectionChoiceTier;
        }
        void RecordJourneyRound()
        {
            if(!JourneyActive)return;
            var session=CampaignJourneySession.Current;
            if(session.Results.Any(r=>r.stage==Loop.Stage))return;
            session.Results.Add(new CampaignJourneySession.RoundSummary{stage=Loop.Stage,level=shop.Level,
                deployments=Battle.Units.Count(u=>!u.SynergySupport),won=Battle.Phase==BattlePhase.Won,
                supply=JourneyStage.IsSupply,seconds=Battle.Clock,damage=Battle.TotalPlayerDamage});
            CaptureJourney();
        }
        string JourneyCompletionText()
        {
            var results=CampaignJourneySession.Current.CompletedRounds;
            return $"十三关行动完成 · {results.Count(r=>!r.supply)}场战斗\n累计伤害 {results.Sum(r=>r.damage):0} · 部署 {results.Sum(r=>r.deployments)}次\n终局 {shop.Level}级 · {shop.Bench.Count}张收藏";
        }
        bool AdvanceJourney()
        {
            if(!JourneyActive||!Loop.OpenShop())return false;
            CaptureJourney();CancelArtPreparation();
            SceneManager.LoadScene(CampaignJourneySession.Current.Map.sceneName);
            return true;
        }
        public IEnumerator PrepareJourneyStage()
        {
            var session=CampaignJourneySession.Current??throw new InvalidOperationException("No journey to resume.");
            var stage=CampaignJourneyCatalog.Stage(session.Stage);var map=session.Map;
            if(SceneManager.GetActiveScene().name!=map.sceneName)throw new InvalidOperationException("Journey requires its authored map scene.");
            var background=stage.encounter??CampaignJourneyCatalog.Current.stages.First(s=>s.stage>=session.Stage&&s.encounter!=null).encounter;
            EnterShopTrial(0,map.id,background);
            activeApprovedMap=null;activeSynergyTrial=null;
            shopDefinitions.Clear();
            foreach(var option in monsterTests)if(option.Playable&&CampaignBalance.Supports(option.Id))shopDefinitions[option.Id]=option.Definition;
            if(session.Shop==null)
            {
                shop=new ShopTrialModel(shopDefinitions.Values.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}),session.Seed);
                shop.EnableCampaign(false);
                var starters=shop.Roles.Where(r=>r.Cost==1).OrderBy(r=>r.Id).ToArray();
                string id=starters[new System.Random(session.Seed).Next(starters.Length)].Id;
                shop.Preset(new[]{new ShopTrialModel.Piece{Id=id,Star=1}},Array.Empty<string>(),1,0,10);
                Loop=new BattleShopRun(shop,session.Seed,campaign:true,journeyV2:true);
                session.Shop=shop;session.Loop=Loop;session.Lineup.Add(id);
                session.Contract=session.Commission=id;
            }
            else {shop=session.Shop;Loop=session.Loop;}
            shopLineup.Clear();shopLineup.AddRange(session.Lineup);
            if(session.Seats!=null){shopSeats=session.Seats;seatedShop=shop;}
            contractCard=session.Contract;commissionTarget=session.Commission;
            infectionForm=session.InfectionPrimary;infectionSecond=session.InfectionSecondary;
            marineForm=session.MarinePrimary;marineSecond=session.MarineSecondary;
            reunionRanged=session.ReunionRanged;lastInfectionChoiceTier=session.InfectionChoiceTier;
            ResetGuerrillaTransfers();ResetMudrockTransfers();
            var guards=stage.encounter==null?Array.Empty<DefenderPlacement>():stage.encounter.defenders;
            var ids=shopLineup.Concat(new[]{"CARD_M07","CARD_M08"}).Distinct();
            var cards=ids.Where(shopDefinitions.ContainsKey).Select(id=>shopDefinitions[id].Card);
            yield return Prepare(Plan(guards,cards,true,talulahSecondPhase:shopLineup.Contains("CARD_TALULAH_TRIAL")&&ShopLineupPiece("CARD_TALULAH_TRIAL")?.Star==3),()=>{
                ConfigureLoopStage();TestSelectionOpen=false;approvedMapMenuOpen=false;
                if(Loop.CanShop)SetShopVisible(true);
                ShopNotice(stage.tip);CaptureJourney();
            },"准备十三关 · "+stage.title);
        }
        void ConfigureJourneyStage()
        {
            var stage=JourneyStage;
            shopMapCopy.defenders=stage.encounter==null?Array.Empty<DefenderPlacement>():JsonUtility.FromJson<JourneyGuards>(JsonUtility.ToJson(new JourneyGuards{items=stage.encounter.defenders})).items;
            shopMapCopy.mapTitle=$"{stage.stage}/13 · {stage.title}";
            shopMapCopy.terrainOnly=stage.IsSupply;
        }
        void DrawJourneyBriefing()
        {
            var stage=JourneyStage;if(stage==null)return;
            float x=268,y=245;
            GUI.Label(new Rect(x,y,674,28),stage.stage+" / 13 · "+stage.title+" · "+(stage.IsSupply?"保送补给":stage.type==CampaignJourneyStageType.Boss?"首领战":"突围战"),CampaignText(17,weight:600));
            var text=CampaignText(13);text.wordWrap=true;
            GUI.Label(new Rect(x,y+34,674,52),stage.tip,text);
            var next=stage.IsSupply?CampaignJourneyCatalog.Current.stages.FirstOrDefault(s=>s.stage>stage.stage&&!s.IsSupply):stage;
            if(next==null)return;
            GUI.Label(new Rect(x,y+90,674,24),$"{(stage.IsSupply?"下一场实战":"本场")}：{next.title}    DP {next.dp} / 共享防御 {next.defense}",CampaignText(13,weight:600));
            for(int i=0;i<next.encounter.defenders.Length;i++)
            {
                var g=next.encounter.defenders[i];
                GUI.Label(new Rect(x,y+119+i*21,674,22),$"{g.displayName}  · HP {g.health:0}  攻击/治疗 {g.attack:0.#}  护甲 {g.armor:0.#}",CampaignText(12));
            }
            GUI.Label(new Rect(x,y+230,674,22),"按路线自动索敌；穿门清空防御即胜利，普通关没有倒计时。",CampaignText(11,color:ApprovedUi.Muted));
        }
        [Serializable]sealed class JourneyGuards {public DefenderPlacement[] items;}
    }
}
