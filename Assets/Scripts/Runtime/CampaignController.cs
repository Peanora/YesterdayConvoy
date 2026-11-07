using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public bool CampaignActive=>Loop!=null&&Loop.Campaign;
        public bool SynergySandbox=>CampaignActive&&Loop.Sandbox;
        string commissionTarget;
        string contractCard;MarineAdaptation marineForm=MarineAdaptation.Barrier,marineSecond=MarineAdaptation.Hunt;int contractTargetIndex;
        InfectionForm infectionForm=InfectionForm.Swarm,infectionSecond=InfectionForm.Shell;
        static CardDefinition GolemCard=>Resources.Load<CardDefinition>("Support/GolemCard");
        bool reunionRanged;
        bool campaignConstructOpen;
        int campaignConstructTab;
        Vector2 campaignSynergyScroll;
        Rect CampaignPanelRect=>Loop!=null&&Loop.CanShop?new Rect(258,255,405,252):new Rect(16,325,405,223);
        static Rect CampaignConstructRect=>new Rect(252,155,722,352);
        bool CampaignUiModalOpen=>CampaignActive&&(inspectedSynergy!=null||campaignConstructOpen);
        public Rect CampaignConstructToggleRect=>new Rect(12,SynergyListRect.yMax+6,174,28);
        Rect CampaignQuickActionsRect=>new Rect(12,CampaignConstructToggleRect.yMax+7,174,222);
        bool CampaignOverlayBlocksPoint(Vector2 point)=>CampaignActive&&(CampaignUiModalOpen||SynergyListRect.Contains(point)||CampaignConstructToggleRect.Contains(point)||
            !Loop.CanShop&&Loop.CanDeploy&&(CampaignQuickActionsRect.Contains(point)||Battle.MudrockTier>=2&&GolemCardRect.Contains(point)));
        GUIStyle CampaignText(int size=13,TextAnchor alignment=TextAnchor.MiddleLeft,Color? color=null,int weight=400)=>ApprovedUi.Text(scene.InterfaceFont,size,alignment,color??ApprovedUi.Ink,weight);
        public IEnumerator PrepareCampaign(bool sandbox,int seed=0,SynergyTrialDefinition trial=null)
        {
            if(trial!=null){if(!sandbox)throw new InvalidOperationException("Dedicated trials require sandbox mode.");trial.Validate();}
            var ids=trial!=null?trial.initialMembers.Select(m=>m.cardId).ToArray():sandbox?new[]{"CARD_M07","CARD_M08","CARD_M11","CARD_M12","CARD_M13","CARD_M06","CARD_M14"}:new[]{"CARD_M07","CARD_M08"};
            var preloadIds=ids.Concat(ids.Count(CampaignBalance.Reunion)>=2?new[]{"CARD_M07","CARD_M08"}:Array.Empty<string>()).Distinct().ToArray();
            if(ids.Except(monsterTests.Where(o=>o.Playable).Select(o=>o.Id)).Any())throw new InvalidOperationException("A starting trial member has no playable model.");
            var cards=monsterTests.Where(o=>o.Playable&&preloadIds.Contains(o.Id)).Select(o=>o.Definition.Card);
            if(ids.Count(CampaignBalance.MudrockCrew)>=2)cards=cards.Concat(new[]{GolemCard});
            var guards=trial!=null?trial.encounter.defenders:CampaignPlan.Guards(3,sandbox);
            bool secondPhase=trial!=null&&trial.initialMembers.Any(m=>m.cardId=="CARD_TALULAH_TRIAL"&&m.star==3);
            return Prepare(Plan(guards,cards,true,talulahSecondPhase:secondPhase),()=>StartCampaign(sandbox,seed,trial),trial!=null?"准备"+trial.displayTitle:"准备羁绊挑战");
        }
        public void StartCampaign(bool sandbox,int seed=0,SynergyTrialDefinition trial=null)
        {
            if(trial!=null){if(!sandbox)throw new InvalidOperationException("Dedicated trials require sandbox mode.");trial.Validate();}
            CancelArtPreparation();EnterShopTrial(0,StrongholdChallengeLayout.Id,trial?.encounter);activeSynergyTrial=trial;
            if(seed==0)seed=Guid.NewGuid().GetHashCode();
            shopDefinitions.Clear();
            foreach(var option in monsterTests)if(option.Playable&&CampaignBalance.Supports(option.Id))shopDefinitions[option.Id]=option.Definition;
            shop=new ShopTrialModel(shopDefinitions.Values.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}),seed);
            shop.EnableCampaign(sandbox);
            string[] initial=trial!=null?trial.initialMembers.Select(m=>m.cardId).ToArray():sandbox?new[]{"CARD_M07","CARD_M08","CARD_M11","CARD_M12","CARD_M13","CARD_M06","CARD_M14"}:new[]{shop.Roles.Where(r=>r.Cost==1).OrderBy(r=>r.Id).ElementAt(new System.Random(seed).Next(shop.Roles.Count(r=>r.Cost==1))).Id};
            shop.Preset(trial!=null?trial.StartingPieces():initial.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}).ToArray(),Array.Empty<string>(),trial!=null?trial.initialLevel:sandbox?8:1,0,10);
            Loop=new BattleShopRun(shop,seed,false,true,sandbox);shopLineup.Clear();shopLineup.AddRange(initial);
            golemBenchmark=false;golemTrainingTab=false;campaignConstructOpen=false;campaignConstructTab=0;campaignSynergyScroll=Vector2.zero;inspectedSynergy=null;commissionTarget=initial[0];contractCard=initial[0];marineForm=MarineAdaptation.Barrier;marineSecond=MarineAdaptation.Hunt;contractTargetIndex=0;reunionRanged=false;infectionForm=InfectionForm.Swarm;infectionSecond=InfectionForm.Shell;
            if(trial!=null)
            {
                infectionForm=trial.infectionPrimary;infectionSecond=trial.infectionSecondary;
                marineForm=trial.marinePrimary;marineSecond=trial.marineSecondary;
                if(!string.IsNullOrEmpty(trial.contractCardId))contractCard=trial.contractCardId;
            }
            lastInfectionChoiceTier=0;
            if(sandbox){Loop.SandboxPreparation();shop.Refresh(true);}
            ConfigureLoopStage();if(sandbox)SetShopVisible(true);
        }
        ManualBattle CreateCampaignBattle()
        {
            var deck=shopLineup.Where(shopDefinitions.ContainsKey).ToList();
            if(deck.Count==0)deck.Add("CARD_M08");
            int dp=SynergySandbox?100:CampaignPlan.BattleDP[Loop.Stage-1];
            int defense=SynergySandbox?6:Mathf.Clamp(1+(Loop.Stage-2)/2,1,7);
            var profiles=deck.Select(id=>CampaignProfile(id,dp,defense)).ToArray();
            var first=profiles[0];
            var battle=new ManualBattle(first.Unit,defenderTrial.route,definition.Engagement.DefenderPosition,first.Combat,null,defenderTrial.Snapshot(),defenderTrial.LaneSnapshot());
            battle.ConfigureCardLoadout(profiles.Skip(1));
            battle.ConfigureStrongholdMap(false);
            battle.ConfigureSynergies(shopLineup.Select(id=>shop.Bench.Where(p=>p.Id==id).OrderByDescending(p=>p.Star).First()),reunionRanged,infectionForm,infectionSecond,Loop.GolemExperience,contractCard,marineForm,marineSecond,Loop.MarineExperience,Loop.GolemInvestment,
                extraGuerrillaMembers:PreparedGuerrillaTransferIds);
            battle.ConfigureGramophones(FindObjectsByType<GramophoneAnchor>(FindObjectsSortMode.None).Select(a=>a.Snapshot()));
            if(Loop.RewardStage)battle.ConfigureSupply();
            foreach(var defender in battle.Defenders)battle.ConfigureDefenderHighGround(defender.EntityId,defender.Position.y>.5f);
            return battle;
        }
        public void ClaimCampaignSupply()
        {
            if(!CampaignActive||!Loop.RewardStage||!Loop.CanDeploy)return;
            // A guaranteed stop has no combat simulation, no HP cost and exactly one receipt.
            Battle.CompleteSupply();StepBattleShopLoop();
        }
        void ReplaySandbox()
        {
            Loop.SandboxPreparation();ConfigureLoopStage();SetShopVisible(true);
        }
        public ShopTrialModel.Result CommissionCampaignCard(string id)
        {
            if(!CampaignActive||!Loop.CanShop||Battle.Phase!=BattlePhase.Ready)return new ShopTrialModel.Result{Message="仅整备时可委托"};
            var snapshot=CaptureShopFlowSnapshot();var result=shop.Commission(id);
            if(result.Success){SyncShopDeck(false);BeginShopArrival(result);BeginShopFlow(snapshot,result,shopPointer);}
            ShopNotice(result.Message);return result;
        }
        public bool RestartCampaignEncounter(){if(!CampaignActive||!Loop.RestartEncounter())return false;ConfigureLoopStage();return true;}
        void RestartCurrentRun()
        {
            if(CampaignActive)StartCampaign(Loop.Sandbox,Loop.Seed,activeSynergyTrial);else StartBattleShopLoop(Loop.Seed,Loop.LateGame);
        }
        bool IsLineupPiece(ShopTrialModel.Piece piece)=>shopLineup.Contains(piece.Id)&&shop.Bench.Where(p=>p.Id==piece.Id).OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).First().Instance==piece.Instance;
        Rect CampaignPieceRect(int index)
        {
            if(index<0||index>=shop.Bench.Count)return Rect.zero;
            var piece=shop.Bench[index];
            if(!Loop.CanShop)return IsLineupPiece(piece)?LoopRosterRect(shopLineup.IndexOf(piece.Id)):Rect.zero;
            return ShopPreparationPieceRect(index);
        }
        void DrawCampaignModels(){if(Event.current.type==EventType.Repaint&&shopModelCamera!=null)shopModelCamera.Render();if(shopModelTarget!=null)GUI.DrawTexture(new Rect(0,0,1280,720),shopModelTarget,ScaleMode.StretchToFill,true);}
        void DrawCampaignControls(Vector2 point)
        {
            if(!CampaignActive)return;
            if(!Loop.CanShop&&!Loop.CanDeploy){campaignConstructOpen=false;inspectedSynergy=null;return;}
            // DrawLoopInterface supplies the point converted before GUI.matrix was applied.
            // Converting Event.mousePosition again here closes the left column at large sizes.
            var e=Event.current;
            if(CampaignUiModalOpen&&e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape)
            {campaignConstructOpen=false;inspectedSynergy=null;CancelShopDrag();e.Use();}
            if(CampaignUiModalOpen&&e.type==EventType.MouseDown&&e.button==0)
            {
                Rect panel=campaignConstructOpen?CampaignConstructRect:SynergyDetailRect;
                if(!panel.Contains(point)&&!SynergyListRect.Contains(point)&&!CampaignConstructToggleRect.Contains(point))
                {campaignConstructOpen=false;inspectedSynergy=null;CancelShopDrag();e.Use();}
            }
            DrawGuerrillaTransferBadges();DrawSynergySidebar();
            var toggle=CampaignConstructToggleRect;
            Fill(toggle,new Color(.125f,.195f,.28f));ApprovedUi.Border(toggle,new Color(.52f,.60f,.69f,.38f));
            bool toggleClicked=GUI.Button(toggle,GUIContent.none,GUIStyle.none);
            GUI.Label(toggle,campaignConstructOpen?"收起构筑与培养":"构筑与培养  展开",CampaignText(12,TextAnchor.MiddleCenter,new Color(.90f,.94f,.97f),600));
            if(toggleClicked)
            {campaignConstructOpen=!campaignConstructOpen;inspectedSynergy=null;CancelShopDrag();}
            bool enabled=GUI.enabled;GUI.enabled=enabled&&!CampaignUiModalOpen;
            if(!Loop.CanShop&&Loop.CanDeploy)DrawCampaignQuickActions();
            GUI.enabled=enabled;
            if(campaignConstructOpen)DrawCampaignConstructOverlay();
        }
        void DrawCampaignConstructOverlay()
        {
            Rect r=CampaignConstructRect;Fill(r,ApprovedUi.Paper);ApprovedUi.Border(r,ApprovedUi.Rule);
            GUI.Label(new Rect(r.x+16,r.y+10,545,27),Loop.CanShop?"构筑与培养":"构筑与培养 · 本场已锁定",CampaignText(18,weight:700));
            if(LoopButton(new Rect(r.xMax-69,r.y+10,53,27),"关闭")){campaignConstructOpen=false;return;}
            if(LoopButton(new Rect(r.x+16,r.y+45,160,28),"运营与测试样板",campaignConstructTab==0))campaignConstructTab=0;
            if(LoopButton(new Rect(r.x+184,r.y+45,160,28),"阵营选择与培养",campaignConstructTab==1))campaignConstructTab=1;
            if(LoopButton(GuerrillaTransferTabRect,"游击转职 "+GuerrillaTransferIds.Count+"/2",campaignConstructTab==2))campaignConstructTab=2;
            var oldMatrix=GUI.matrix;bool oldEnabled=GUI.enabled;
            try
            {
                if(campaignConstructTab==0)
                {
                    GUI.matrix=oldMatrix*Matrix4x4.Translate(new Vector3(r.x, r.y+84-CampaignPanelRect.y,0));
                    DrawCampaignSettings();
                }
                else if(campaignConstructTab==1)
                {
                    GUI.matrix=oldMatrix*Matrix4x4.Translate(new Vector3(r.x+16-680,r.y+84-270,0));
                    DrawRemainingPreparation();
                }
                else DrawGuerrillaTransferPanel();
            }
            finally {GUI.matrix=oldMatrix;GUI.enabled=oldEnabled;}
        }
        void DrawCampaignQuickActions()
        {
            float x=CampaignQuickActionsRect.x,y=CampaignQuickActionsRect.y;
            if(Loop.RewardStage)
            {
                Fill(new Rect(x,y,157,48),ApprovedUi.Paper);
                GUI.Label(new Rect(x+8,y+3,141,18),"补给关 · 无需部署",CampaignText(11));
                if(LoopButton(new Rect(x+5,y+22,147,23),"领取补给 · 无损通关",true))ClaimCampaignSupply();
                return;
            }
            bool enabled=GUI.enabled;
            if(Battle.ReunionTier>0||Battle.SnowTier>0)
            {
                Fill(new Rect(x,y,157,20),ApprovedUi.Paper);GUI.Label(new Rect(x+7,y,143,20),$"援军 {Battle.MusterUsed} · 碎冰 {Battle.Shatters}",CampaignText(11));y+=22;
            }
            if(Battle.GuerrillaTier>0)
            {
                var rule=GuerrillaRules.ForTier(Battle.GuerrillaTier);
                Fill(new Rect(x,y,157,20),ApprovedUi.Paper);GUI.Label(new Rect(x+7,y,143,18),$"军令 {Battle.Orders} · 满{rule.Cost}自动",CampaignText(11));
                Fill(new Rect(x+7,y+18,143*Mathf.Clamp01((float)Battle.Orders/rule.Cost),2),ApprovedUi.Gold);y+=22;
                string status=Battle.HalfOrderPending?"等待后半":Battle.GuerrillaOrderBusy?"五段追击中":Battle.Orders>=rule.Cost?"等待出手者":"积攒军令";
                Fill(new Rect(x,y,rule.AllowsHalf?76:157,25),ApprovedUi.Paper);
                GUI.Label(new Rect(x+5,y,rule.AllowsHalf?71:147,25),status,CampaignText(10));
                GUI.enabled=enabled&&Battle.Phase==BattlePhase.Running&&!Battle.IsPaused&&!Battle.GuerrillaOrderBusy&&Battle.Orders>=3;
                if(rule.AllowsHalf&&LoopButton(new Rect(x+81,y,76,25),Battle.HalfOrderPending?"后半3点":"前半3点"))UseCampaignOrder(true);
                y+=29;GUI.enabled=enabled;
            }
            y=DrawRemainingBattleActions(x,y);
            if(SynergySandbox&&Loop.CanDeploy&&LoopButton(new Rect(x,y+4,157,26),"结束验证，返回整备"))ReplaySandbox();
            if(Battle.MudrockTier>=2)DrawGolemCard();
            GUI.enabled=enabled;
        }
        void DrawCampaignSettings()
        {
            float y=CampaignPanelRect.y;
            GUI.Label(new Rect(25,y+5,388,26),$"整合{Battle.ReunionTier} 雪怪{Battle.SnowTier} 感染{Battle.InfectionTier} 游击{Battle.GuerrillaTier} 泥岩{Battle.MudrockTier}",CampaignText(12,weight:600));
            var rule=GuerrillaRules.ForTier(Battle.GuerrillaTier);
            GUI.Label(new Rect(25,y+31,650,24),$"援军{Battle.MusterUsed} 碎冰{Battle.Shatters} 军令{Battle.Orders}"+(rule.Tier>0?$"（开战{rule.InitialOrders} / 满{rule.Cost}点自动整令 / 不限次）":"")+" · "+(Battle.MudrockTier>=2?$"巨像{Battle.GolemStage}级 XP{Loop.GolemExperience} 投{Loop.GolemInvestment}":"巨像未解锁"),CampaignText(11));
            if(Loop.CanShop)
            {
                GUI.Label(new Rect(25,y+56,388,22),SynergySandbox?(golemBenchmark?"IV原数值基准 · 无限金币 · 九张库存 · DP100":"测试倍率：怪物血×4 伤×2 · 守军血×12 伤×8"):"每次整备限一次低费委托，失败不扣费",CampaignText(11));
                var eligible=shop.Bench.Select(p=>p.Id).Distinct().Where(id=>shop.Definition(id).Cost<=3).OrderBy(id=>id).ToArray();
                if(!eligible.Contains(commissionTarget))commissionTarget=eligible.FirstOrDefault();
                if(commissionTarget!=null&&LoopButton(new Rect(25,y+84,230,30),"委托："+shop.Definition(commissionTarget).Name))commissionTarget=eligible[(Array.IndexOf(eligible,commissionTarget)+1)%eligible.Length];
                GUI.enabled=commissionTarget!=null&&(!shop.CommissionUsed||SynergySandbox);
                if(LoopButton(new Rect(266,y+84,142,30),"委托 原价+2"))CommissionCampaignCard(commissionTarget);GUI.enabled=true;
                if(LoopButton(new Rect(25,y+121,230,30),reunionRanged?"援军：弩手":"援军：士兵")){reunionRanged=!reunionRanged;SyncShopDeck(false);}
                if(LoopButton(new Rect(266,y+121,142,30),"切换测试")){ReturnToTrialScene();return;}
                if(LoopButton(new Rect(25,y+158,383,30),"选择感染形态  ·  主："+FormName(infectionForm)+
                    (Battle.InfectionTier>=6?" / 副："+FormName(infectionSecond):" / 副形态6档解锁")))
                {inspectedSynergy="感染生物";campaignConstructOpen=false;CancelShopDrag();}
                GUI.Label(new Rect(25,y+192,388,22),"也可直接点击左侧感染生物；开战时锁定",CampaignText(11));
                if(SynergySandbox)
                    for(int i=0;i<8;i++)if(LoopButton(new Rect(25+(i%4)*96,y+214+(i/4)*20,90,18),new[]{"冰雪样板","感染样板","游击样板","泥岩样板","牧群样板","深池样板","承契样板","海嗣样板"}[i])){campaignConstructOpen=false;PrepareSandboxRoster(i);}
            }
            else
            {
                GUI.Label(new Rect(25,y+69,620,28),$"援军选择：{(reunionRanged?"弩手":"士兵")}    感染主形态：{FormName(infectionForm)}    副形态：{FormName(infectionSecond)}",CampaignText(13));
                GUI.Label(new Rect(25,y+108,620,55),"构筑选择已于开战前锁定。本场军令、承契爆发和火种等操作在左侧动作栏。\n战斗暂停时可以部署，技能与羁绊主动仍需恢复后使用。",CampaignText(12));
            }
        }
    }
}
