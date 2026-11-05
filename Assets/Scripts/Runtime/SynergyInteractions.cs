using System;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        bool golemDragging;
        Vector2 golemPointer;
        static Rect GolemSlot=>new Rect(1115,454,145,88);
        public static Rect GolemCardRect=>GolemSlot;
        static string FormName(InfectionForm form)=>form==InfectionForm.Swarm?"集群":form==InfectionForm.Shell?"甲壳":"爆裂";
        public bool SetCampaignForms(InfectionForm form,InfectionForm second)
        {
            if(!CampaignActive||!Loop.CanShop||Battle.Phase!=BattlePhase.Ready||!Enum.IsDefined(typeof(InfectionForm),form)||!Enum.IsDefined(typeof(InfectionForm),second))return false;
            infectionForm=form;infectionSecond=second==form?(InfectionForm)(((int)form+1)%3):second;SyncShopDeck(false);return true;
        }
        static string[] SandboxRoster(int preset)=>preset>=8?new[]{"CARD_M26","CARD_M27","CARD_M42","CARD_M43","CARD_M28","CARD_M07","CARD_M08"}:preset==1?new[]{"CARD_ORIGINIUM_SLUG","CARD_M02","CARD_M03","CARD_M04","CARD_M05","CARD_M06","CARD_M11","CARD_M12"}:
            preset==2?new[]{"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M08","CARD_M05"}:
            preset==3?new[]{"CARD_M26","CARD_M27","CARD_M07","CARD_M08","CARD_M05","CARD_M10"}:
            preset==4?new[]{"CARD_M21","CARD_M22","CARD_M23","CARD_M24","CARD_M07","CARD_M08","CARD_M10","CARD_M05"}:
            preset==5?new[]{"CARD_M29","CARD_M30","CARD_M31","CARD_M32","CARD_M05","CARD_M07","CARD_M10","CARD_M11"}:
            preset==6?new[]{"CARD_M19","CARD_M24","CARD_M26","CARD_M27","CARD_M37","CARD_M38","CARD_WISADEL_APRIL","CARD_M18"}:
            preset==7?new[]{"CARD_M33","CARD_M34","CARD_M35","CARD_M36","CARD_M26","CARD_M27","CARD_M08","CARD_M10"}:new[]{"CARD_M07","CARD_M08","CARD_M11","CARD_M12","CARD_M13","CARD_M06","CARD_M14"};
        void PrepareSandboxRoster(int preset)
        {
            var ids=SandboxRoster(preset);var cards=ids.Select(id=>shopDefinitions[id].Card);
            if(ids.Count(CampaignBalance.MudrockCrew)>=2)cards=cards.Concat(new[]{GolemCard});
            QueueArt(Plan(preset==10?BattleShopStages.Defenders(2,true):shopMapCopy.defenders,cards,true),()=>SetSandboxRoster(preset),"准备羁绊样板");
        }
        public bool SetSandboxRoster(int preset)
        {
            if(!SynergySandbox||!Loop.CanShop||preset<0||preset>10)return false;
            var ids=SandboxRoster(preset);golemBenchmark=preset==10;
            shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=preset==10&&CampaignBalance.MudrockCrew(id)?2:1}),Array.Empty<string>(),Math.Max(8,shop.Level),0,10);
            if(preset==3||preset>=8){Loop.PresetGolemGrowth(preset==8?4:preset==9?12:preset==10?24:0,preset==8?20:preset==9?45:preset==10?80:0);golemTrainingTab=true;}
            contractCard=ids[0];contractTargetIndex=0;shopLineup.Clear();shopLineup.AddRange(ids);commissionTarget=ids[0];shop.Refresh(true);ConfigureLoopStage();SetShopVisible(true);return true;
        }
        public bool UseCampaignOrder(bool half)
        {
            if(!CampaignActive||!Loop.CanDeploy)return false;
            bool ok=Battle.TryGuerrillaOrder(Battle.RunId,++requestSequence,half);
            if(!ok)ShopNotice("军令/次数不足，或没有完成动作且有合法目标的单位");return ok;
        }
        bool HandleGolemPointer(EventType type,Vector2 point,int button)
        {
            if(!CampaignActive||!Loop.CanDeploy||Loop.RewardStage||Battle.MudrockTier<2)return false;
            if(type==EventType.MouseDown&&button==0&&GolemSlot.Contains(point))
            {golemDragging=true;golemPointer=point;CloseSkillMenu();return true;}
            if(!golemDragging)return false;golemPointer=point;
            if(type==EventType.MouseUp&&button==0)
            {
                golemDragging=false;
                if(PointerGround(ScreenPosition(point),out var world))
                {
                    if(Battle.Phase==BattlePhase.Ready&&Battle.IsEntry(world))Battle.Begin();
                    var result=Battle.TryDeployGolem(Battle.RunId,++requestSequence,world,true);ShopNotice(result.Success?"巨像已部署，本场限一次":Explain(result.Failure));SyncViews();
                }
            }
            return type==EventType.MouseDown||type==EventType.MouseDrag||type==EventType.MouseUp;
        }
        void DrawGolemCard()
        {
            if(Battle.MudrockTier<2||!Loop.CanDeploy)return;
            Fill(GolemSlot,LoopPanel);DrawTestPortrait(new Rect(GolemSlot.x+4,GolemSlot.y+2,66,82),GolemCard.Portrait);
            GUI.Label(new Rect(GolemSlot.x+70,GolemSlot.y+10,72,26),"巨像 "+Battle.GolemStage,LoopText(13));
            GUI.Label(new Rect(GolemSlot.x+70,GolemSlot.y+40,72,32),Battle.GolemDeployed?"本场已用":Battle.GolemCost+"DP · 拖动",LoopText(11));
            if(golemDragging)DrawTestPortrait(new Rect(golemPointer.x-45,golemPointer.y-100,90,100),GolemCard.Portrait);
        }
    }
}
