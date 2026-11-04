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
        InfectionForm infectionForm=InfectionForm.Swarm,infectionSecond=InfectionForm.Shell;
        static CardDefinition GolemCard=>Resources.Load<CardDefinition>("Support/GolemCard");
        bool reunionRanged;
        Rect CampaignPanelRect=>Loop!=null&&Loop.CanShop?new Rect(258,255,405,250):new Rect(16,365,405,Loop!=null&&SynergySandbox?176:128);
        public IEnumerator PrepareCampaign(bool sandbox,int seed=0)
        {
            var ids=sandbox?new[]{"CARD_M07","CARD_M08","CARD_M11","CARD_M12","CARD_M13","CARD_M06"}:new[]{"CARD_M07","CARD_M08"};
            var cards=monsterTests.Where(o=>o.Playable&&ids.Contains(o.Id)).Select(o=>o.Definition.Card);
            return Prepare(Plan(CampaignPlan.Guards(3,sandbox),cards,true),()=>StartCampaign(sandbox,seed),"准备羁绊挑战");
        }
        public void StartCampaign(bool sandbox,int seed=0)
        {
            CancelArtPreparation();EnterShopTrial(0,StrongholdChallengeLayout.Id);
            if(seed==0)seed=Guid.NewGuid().GetHashCode();
            shopDefinitions.Clear();
            foreach(var option in monsterTests)if(option.Playable&&CampaignBalance.Supports(option.Id))shopDefinitions[option.Id]=option.Definition;
            shop=new ShopTrialModel(shopDefinitions.Values.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}),seed);
            shop.EnableCampaign(sandbox);
            string[] initial=sandbox?new[]{"CARD_M07","CARD_M08","CARD_M11","CARD_M12","CARD_M13","CARD_M06"}:new[]{shop.Roles.Where(r=>r.Cost==1).OrderBy(r=>r.Id).ElementAt(new System.Random(seed).Next(shop.Roles.Count(r=>r.Cost==1))).Id};
            shop.Preset(initial.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),sandbox?8:1,0,10);
            Loop=new BattleShopRun(shop,seed,false,true,sandbox);shopLineup.Clear();shopLineup.AddRange(initial);
            commissionTarget=initial[0];reunionRanged=false;infectionForm=InfectionForm.Swarm;infectionSecond=InfectionForm.Shell;
            if(sandbox){Loop.SandboxPreparation();shop.Refresh(true);}
            ConfigureLoopStage();if(sandbox)SetShopVisible(true);
        }
        ManualBattle CreateCampaignBattle()
        {
            var deck=shopLineup.Where(shopDefinitions.ContainsKey).ToList();
            if(deck.Count==0)deck.Add("CARD_M08");
            int dp=SynergySandbox?100:CampaignPlan.BattleDP[Loop.Stage-1];
            int defense=SynergySandbox?6:Mathf.Clamp(1+(Loop.Stage-2)/2,1,7);
            var profiles=deck.Select(id=>CampaignBalance.Build(shopDefinitions[id],shop.Bench.Where(p=>p.Id==id).Select(p=>p.Star).DefaultIfEmpty(1).Max(),dp,defense)).ToArray();
            var first=profiles[0];
            var battle=new ManualBattle(first.Unit,defenderTrial.route,definition.Engagement.DefenderPosition,first.Combat,null,defenderTrial.Snapshot(),defenderTrial.LaneSnapshot());
            battle.ConfigureCardLoadout(profiles.Skip(1));
            battle.ConfigureStrongholdMap(false);
            battle.ConfigureSynergies(shopLineup.Select(id=>shop.Bench.Where(p=>p.Id==id).OrderByDescending(p=>p.Star).First()),reunionRanged,infectionForm,infectionSecond,Loop.GolemExperience);
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
            var result=shop.Commission(id);
            if(result.Success){SyncShopDeck(false);BeginShopArrival(result);}
            ShopNotice(result.Message);return result;
        }
        public bool RestartCampaignEncounter(){if(!CampaignActive||!Loop.RestartEncounter())return false;ConfigureLoopStage();return true;}
        void RestartCurrentRun()
        {
            if(CampaignActive)StartCampaign(Loop.Sandbox,Loop.Seed);else StartBattleShopLoop(Loop.Seed,Loop.LateGame);
        }
        bool IsLineupPiece(ShopTrialModel.Piece piece)=>shopLineup.Contains(piece.Id)&&shop.Bench.Where(p=>p.Id==piece.Id).OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).First().Instance==piece.Instance;
        Rect CampaignPieceRect(int index)
        {
            if(index<0||index>=shop.Bench.Count)return Rect.zero;
            var piece=shop.Bench[index];
            if(!Loop.CanShop)return IsLineupPiece(piece)?LoopRosterRect(shopLineup.IndexOf(piece.Id)):Rect.zero;
            int slot=IsLineupPiece(piece)?shopLineup.IndexOf(piece.Id):shop.Bench.Where(p=>!IsLineupPiece(p)).ToList().FindIndex(p=>p.Instance==piece.Instance);
            return new Rect(16+slot*124,IsLineupPiece(piece)?531:650,116,62);
        }
        void DrawCampaignBench(GUIStyle style)
        {
            Fill(new Rect(0,508,1280,89),LoopInk);Fill(new Rect(0,632,1280,88),LoopInk);
            GUI.Label(new Rect(16,507,1100,23),$"编队 {shopLineup.Count}/{shop.Level}（上排） · 独立备战 {shop.Bench.Count-shopLineup.Count}/10（下排） · 点击调整，向下拖出售",style);
            for(int i=0;i<shop.Bench.Count;i++)
            {
                var piece=shop.Bench[i];var r=CampaignPieceRect(i);DrawLoopCostBackground(r,shop.Definition(piece.Id).Cost);
                if(ShopBenchModel(piece.Instance)==null)DrawTestPortrait(new Rect(r.x+2,r.y+1,42,51),OriginalTalulahArt.CardPortrait(shopDefinitions[piece.Id].Card));
                GUI.Label(new Rect(r.x+43,r.y+4,73,23),new string('★',piece.Star),style);
                GUI.Label(new Rect(r.x+2,r.y+39,112,22),shop.Definition(piece.Id).Name,new GUIStyle(style){fontSize=11});
            }
        }
        void DrawCampaignModels(){if(Event.current.type==EventType.Repaint&&shopModelCamera!=null)shopModelCamera.Render();if(shopModelTarget!=null)GUI.DrawTexture(new Rect(0,0,1280,720),shopModelTarget,ScaleMode.StretchToFill,true);}
        void DrawCampaignControls()
        {
            if(!CampaignActive)return;
            var oldMatrix=GUI.matrix;bool oldEnabled=GUI.enabled;
            try { GUI.enabled=oldEnabled&&inspectedSynergy==null;GUI.matrix=oldMatrix*Matrix4x4.Translate(new Vector3(CampaignPanelRect.x-16,0,0));DrawCampaignSettings(); }
            finally { GUI.matrix=oldMatrix;GUI.enabled=oldEnabled; }
            DrawSynergySidebar();
        }
        void DrawCampaignSettings()
        {
            float y=CampaignPanelRect.y;Fill(new Rect(16,y,405,CampaignPanelRect.height),new Color(.04f,.07f,.09f,.94f));
            GUI.Label(new Rect(25,y+5,388,26),$"整合{Battle.ReunionTier} 雪怪{Battle.SnowTier} 感染{Battle.InfectionTier} 游击{Battle.GuerrillaTier} 泥岩{Battle.MudrockTier}",LoopText(12));
            GUI.Label(new Rect(25,y+31,388,24),$"援军{Battle.MusterUsed} 碎冰{Battle.Shatters} 军令{Battle.Orders} · "+(Battle.MudrockTier>=2?$"巨像{Battle.GolemStage}级 XP{Loop.GolemExperience}":"巨像未解锁"),LoopText(11));
            if(Loop.CanShop)
            {
                GUI.Label(new Rect(25,y+56,388,22),SynergySandbox?"无限金币 · 九张库存 · DP100 · 委托不限次数":"每次整备限一次低费委托，失败不扣费",LoopText(11));
                var eligible=shop.Bench.Select(p=>p.Id).Distinct().Where(id=>shop.Definition(id).Cost<=3).OrderBy(id=>id).ToArray();
                if(!eligible.Contains(commissionTarget))commissionTarget=eligible.FirstOrDefault();
                if(commissionTarget!=null&&LoopButton(new Rect(25,y+84,230,30),"委托："+shop.Definition(commissionTarget).Name))commissionTarget=eligible[(Array.IndexOf(eligible,commissionTarget)+1)%eligible.Length];
                GUI.enabled=commissionTarget!=null&&(!shop.CommissionUsed||SynergySandbox);
                if(LoopButton(new Rect(266,y+84,142,30),"委托 原价+2"))CommissionCampaignCard(commissionTarget);GUI.enabled=true;
                if(LoopButton(new Rect(25,y+121,230,30),reunionRanged?"援军：弩手":"援军：士兵")){reunionRanged=!reunionRanged;SyncShopDeck(false);}
                if(LoopButton(new Rect(266,y+121,142,30),"切换测试")){ReturnToTrialScene();return;}
                if(LoopButton(new Rect(25,y+158,230,30),"感染主形态："+FormName(infectionForm)))SetCampaignForms((InfectionForm)(((int)infectionForm+1)%3),infectionSecond);
                if(LoopButton(new Rect(266,y+158,142,30),"六档副："+FormName(infectionSecond)))SetCampaignForms(infectionForm,(InfectionForm)(((int)infectionSecond+1)%3));
                GUI.Label(new Rect(25,y+192,388,22),"形态战前锁定；六档副形态只取二档效果",LoopText(11));
                if(SynergySandbox)
                    for(int i=0;i<4;i++)if(LoopButton(new Rect(25+i*96,y+218,90,26),new[]{"冰雪样板","感染样板","游击样板","泥岩样板"}[i]))PrepareSandboxRoster(i);
            }
            else if(Loop.RewardStage&&Loop.CanDeploy)
            {if(LoopButton(new Rect(25,y+75,380,48),"补给关无需部署 · 点击领取补给",true))ClaimCampaignSupply();}
            else
            {
                GUI.enabled=Loop.CanDeploy&&Battle.Phase==BattlePhase.Running&&!Battle.IsPaused&&Battle.GuerrillaTier>0;
                if(LoopButton(new Rect(25,y+68,185,32),"协同整令"))UseCampaignOrder(false);
                GUI.enabled=GUI.enabled&&Battle.GuerrillaTier==6;
                if(LoopButton(new Rect(220,y+68,185,32),Battle.HalfOrderPending?"后半令 · 3点":"前半令 · 3点"))UseCampaignOrder(true);GUI.enabled=true;
                GUI.Label(new Rect(25,y+105,388,22),Battle.InfectionTier==0?"感染未激活":"感染："+FormName(infectionForm)+(Battle.InfectionTier==6?" + "+FormName(infectionSecond):""),LoopText(11));
                if(SynergySandbox&&Loop.CanDeploy&&LoopButton(new Rect(25,y+136,380,30),"结束本次验证，返回整备"))ReplaySandbox();
                DrawGolemCard();
            }
        }
    }
}
