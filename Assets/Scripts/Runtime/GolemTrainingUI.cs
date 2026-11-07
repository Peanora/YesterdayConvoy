using System;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        bool golemTrainingTab,golemBenchmark;
        static readonly Color GolemEarth=new Color(.64f,.45f,.18f);
        static readonly Color GolemLight=new Color(.94f,.79f,.43f);
        public static Rect GolemSettlementBarRect=>new Rect(206,439,330,14);
        public static Rect GolemTrainingBarRect=>new Rect(282,326,550,14);
        BattleShopRun golemFeedbackLoop;
        int golemFeedbackBefore,golemFeedbackTarget,golemFeedbackEarned,golemFeedbackCapacity=1;
        int golemFeedbackBeforeStage=1,golemFeedbackAfterStage=1;
        bool golemFeedbackPending,golemFeedbackStarted,golemFeedbackComplete;
        double golemFeedbackStartedAt;
        string golemFeedbackReason="";
        const float GolemFeedbackDuration=.85f;

        static string GolemLevelName(int stage)=>new[]{"","I","II","III","IV","V"}[Mathf.Clamp(stage,1,5)];
        void BindGolemExperienceFeedback()
        {
            if(ReferenceEquals(golemFeedbackLoop,Loop))return;
            ResetGolemExperienceFeedback();
        }
        void ResetGolemExperienceFeedback()
        {
            golemFeedbackLoop=Loop;golemFeedbackBefore=golemFeedbackTarget=Loop?.GolemExperience??0;
            golemFeedbackEarned=0;golemFeedbackCapacity=1;golemFeedbackBeforeStage=golemFeedbackAfterStage=1;
            golemFeedbackPending=golemFeedbackStarted=golemFeedbackComplete=false;golemFeedbackStartedAt=0;golemFeedbackReason="";
        }
        float GolemFeedbackFraction
        {
            get
            {
                if(golemFeedbackComplete||golemFeedbackEarned<=0)return 1;
                if(!golemFeedbackStarted)return 0;
                return Mathf.Clamp01((float)(Time.realtimeSinceStartupAsDouble-golemFeedbackStartedAt)/GolemFeedbackDuration);
            }
        }
        public float GolemDisplayedExperience
        {
            get
            {
                BindGolemExperienceFeedback();
                if(!golemFeedbackPending)return Loop?.GolemExperience??0;
                float t=GolemFeedbackFraction;return Mathf.Lerp(golemFeedbackBefore,golemFeedbackTarget,1-(1-t)*(1-t)*(1-t));
            }
        }
        public int GolemFeedbackEarned {get{BindGolemExperienceFeedback();return golemFeedbackEarned;}}
        public int GolemFeedbackBeforeStage {get{BindGolemExperienceFeedback();return golemFeedbackBeforeStage;}}
        public int GolemFeedbackAfterStage {get{BindGolemExperienceFeedback();return golemFeedbackAfterStage;}}
        public bool GolemFeedbackAnimating {get{BindGolemExperienceFeedback();return golemFeedbackPending&&golemFeedbackStarted&&!golemFeedbackComplete&&golemFeedbackEarned>0&&GolemFeedbackFraction<1;}}
        public string GolemFeedbackText
        {
            get
            {
                BindGolemExperienceFeedback();
                if(!golemFeedbackPending)return "战斗结束后自动结算巨像经验";
                string growth=golemFeedbackAfterStage>golemFeedbackBeforeStage?" · 升级 "+GolemLevelName(golemFeedbackBeforeStage)+" → "+GolemLevelName(golemFeedbackAfterStage):" · 巨像 "+GolemLevelName(golemFeedbackAfterStage);
                return $"巨像经验 +{golemFeedbackEarned}"+growth+(golemFeedbackEarned==0?" · "+golemFeedbackReason:"");
            }
        }
        public string GolemCapacityText
        {
            get
            {
                if(!CampaignActive||Battle==null)return "编入至少2种泥岩小队成员后开启培养";
                int count=Battle.MudrockCrewCount,stars=Battle.MudrockTotalStars,capacity=Battle.GolemCapacity;
                string status=Loop.GolemExperience>=GolemGrowth.Threshold(capacity)?"已达当前"+GolemLevelName(capacity)+"级上限":"当前上限"+GolemLevelName(capacity);
                if(capacity>=5)return status+"；V级为最高阶段";
                int next=capacity+1,requiredCount=next==2?2:next==3?4:next==4?5:6,requiredStars=next==2?0:next==3?7:next==4?10:12;
                string needed="";
                if(count<requiredCount)needed=$"还需{requiredCount-count}种成员";
                if(stars<requiredStars)needed+=(needed.Length>0?"、":"")+$"总星级再加{requiredStars-stars}";
                return status+"；"+GolemLevelName(next)+"上限："+(needed.Length>0?needed:"条件已满足")+"。历史经验保留，封顶后不预存。";
            }
        }
        void BeginGolemExperienceFeedback(int beforeXP)
        {
            BindGolemExperienceFeedback();
            golemFeedbackBefore=Math.Max(0,beforeXP);golemFeedbackTarget=Loop.GolemExperience;
            golemFeedbackEarned=Loop.LastGolemGrowth;golemFeedbackCapacity=Loop.LastGolemCapacity;
            golemFeedbackBeforeStage=GolemGrowth.Stage(golemFeedbackBefore,golemFeedbackCapacity);
            golemFeedbackAfterStage=GolemGrowth.Stage(golemFeedbackTarget,golemFeedbackCapacity);
            golemFeedbackPending=Battle.MudrockCrewCount>=2||golemFeedbackTarget>0;golemFeedbackStarted=false;golemFeedbackComplete=false;
            golemFeedbackReason=Loop.RewardStage?"补给关不发经验":Battle.MudrockCrewCount<2?"小队不足2种，未开启培养":"已达当前培养上限";
        }
        void CompleteGolemExperienceFeedback()
        {
            BindGolemExperienceFeedback();golemFeedbackComplete=true;
        }
        static void DrawGolemExperienceBar(Rect rect,float displayed,int capacity,bool animate=false)
        {
            int target=GolemGrowth.Threshold(capacity);
            Fill(rect,new Color(.68f,.71f,.70f,.9f));
            float fraction=target>0?Mathf.Clamp01(displayed/target):0;
            if(fraction>0)
            {
                Fill(new Rect(rect.x,rect.y,rect.width*fraction,rect.height),GolemEarth);
                Fill(new Rect(rect.x,rect.y,rect.width*fraction,Mathf.Min(3,rect.height)),GolemLight);
                if(animate)Fill(new Rect(rect.x+Mathf.Max(0,rect.width*fraction-3),rect.y,Mathf.Min(3,rect.width*fraction),rect.height),new Color(1,.91f,.65f));
            }
            for(int stage=2;stage<capacity;stage++)
            {
                float x=rect.x+rect.width*GolemGrowth.Threshold(stage)/target;
                Fill(new Rect(x,rect.y,1,rect.height),new Color(.96f,.94f,.87f,.8f));
            }
            ApprovedUi.Border(rect,new Color(.42f,.43f,.38f),1);
        }
        void DrawGolemSettlementFeedback()
        {
            if(!CampaignActive)return;
            BindGolemExperienceFeedback();
            if(!golemFeedbackPending)return;
            // The battle may finish before its death animations. Start only when the result is painted.
            if(golemFeedbackPending&&!golemFeedbackStarted&&!golemFeedbackComplete&&Event.current.type==EventType.Repaint)
            {golemFeedbackStarted=true;golemFeedbackStartedAt=Time.realtimeSinceStartupAsDouble;}
            var panel=new Rect(200,399,342,105);Fill(panel,new Color(.94f,.925f,.86f,.96f));
            GUI.Label(new Rect(206,402,330,24),GolemFeedbackEarned>0?$"巨像经验 +{GolemFeedbackEarned}":"巨像经验 +0",CampaignText(15,color:GolemEarth,weight:700));
            string stage=GolemFeedbackAfterStage>GolemFeedbackBeforeStage?"升级 "+GolemLevelName(GolemFeedbackBeforeStage)+" → "+GolemLevelName(GolemFeedbackAfterStage):"巨像 "+GolemLevelName(GolemFeedbackAfterStage);
            string progress=golemFeedbackCapacity>1?Mathf.FloorToInt(GolemDisplayedExperience)+" / "+GolemGrowth.Threshold(golemFeedbackCapacity)+" XP":"未解锁培养 · 历史经验"+Mathf.FloorToInt(GolemDisplayedExperience);
            GUI.Label(new Rect(206,426,330,13),stage+" · "+progress,CampaignText(10,color:ApprovedUi.Ink,weight:600));
            DrawGolemExperienceBar(GolemSettlementBarRect,GolemDisplayedExperience,golemFeedbackCapacity,GolemFeedbackAnimating);
            var reason=CampaignText(10,TextAnchor.UpperLeft,ApprovedUi.Muted);reason.wordWrap=true;
            string explanation=golemFeedbackEarned>0?$"{golemFeedbackBefore} → {golemFeedbackTarget} XP，已保存。"+(golemFeedbackTarget>=GolemGrowth.Threshold(golemFeedbackCapacity)?"已达本场培养上限。":"胜负均结算，补给与退出不发。")
                :golemFeedbackReason+(golemFeedbackCapacity<5&&Battle.MudrockCrewCount>=2?"；补足人数与总星级后继续。":"。");
            GUI.Label(new Rect(206,459,330,39),explanation,reason);
        }
        void DrawGolemSidebarProgress(Rect row)
        {
            int capacity=Battle.GolemCapacity,xp=Loop.GolemExperience,stage=GolemGrowth.Stage(Loop.GolemExperience,Battle.GolemCapacity);
            string progress=capacity>1?GolemLevelName(stage)+"级 · "+xp+"/"+GolemGrowth.Threshold(capacity)+" XP":"未解锁 · 历史XP "+xp;
            GUI.Label(SynergySupplementRect(row),progress,CampaignText(9,color:GolemLight,weight:600));
            DrawGolemExperienceBar(SynergyProgressRect(row),xp,capacity);
        }
        void DrawGolemTraining()
        {
            int capacity=Battle.GolemCapacity,xp=Loop.GolemExperience;
            GUI.Label(new Rect(694,313,550,23),$"巨像 {GolemLevelName(Battle.GolemStage)} · 小队{Battle.MudrockTier}档 · {Battle.MudrockCrewCount}种 / 总星级{Battle.MudrockTotalStars}",CampaignText(14,weight:700));
            string progress=capacity>1?$"累计经验 {xp} / {GolemGrowth.Threshold(capacity)} · 可培养上限 {GolemLevelName(capacity)}":$"培养未解锁 · 历史经验 {xp} · 请编入至少2种小队成员";
            GUI.Label(new Rect(694,337,550,18),progress,CampaignText(11,color:GolemEarth,weight:600));
            // DrawRemainingPreparation translates this local rectangle to GolemTrainingBarRect.
            DrawGolemExperienceBar(new Rect(694,357,550,14),xp,capacity);
            int raw=Battle.GolemBattleExperience,award=Loop.RewardStage?0:GolemGrowth.Award(xp,capacity,raw);
            string expected=Loop.RewardStage?"本关为补给，不发经验":$"下场战斗预计 +{award} XP（{Battle.MudrockTier}档＋总星级÷2向下取整 = {raw}）";
            GUI.Label(new Rect(694,374,550,20),expected,CampaignText(11));
            var hint=CampaignText(11,TextAnchor.UpperLeft,ApprovedUi.Muted);hint.wordWrap=true;
            GUI.Label(new Rect(694,398,550,34),GolemCapacityText,hint);
            if(SynergySandbox)
            {
                GUI.Label(new Rect(694,433,550,12),"补卡使用真实库存与合成；下排预置直接设定阶段并重置编队。",CampaignText(9,color:ApprovedUi.Muted));
                string[] ids={"CARD_M26","CARD_M27","CARD_M42","CARD_M43","CARD_M28"};
                string[] names={"掷能","践行","掷长","践长","泥岩"};
                bool enabled=GUI.enabled;
                for(int i=0;i<ids.Length;i++)
                {
                    string id=ids[i];
                    GUI.enabled=enabled&&shop.Bench.Any(p=>p.Id==id)&&!shop.Bench.Any(p=>p.Id==id&&p.Star==3)&&shop.Available(id)>0;
                    if(LoopButton(new Rect(694+i*110,445,104,20),"补1张 "+names[i]))CommissionCampaignCard(id);
                }
                GUI.enabled=enabled;
                for(int i=0;i<5;i++)if(LoopButton(new Rect(694+i*110,465,104,26),new[]{"I 从零","II 成长","III 巨像","IV 终极","V 完全体"}[i]))PrepareSandboxRoster(i==0?3:i+7);
            }
            else GUI.Label(new Rect(694,437,550,42),"战斗结束自动培养，不用投入金币、不要求每人二星。\n经验跨关保留；新挑战清零。左侧小队条目可随时查看。",hint);
        }
    }
}
