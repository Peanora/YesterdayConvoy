using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        static readonly Color MarineInk=new Color(.14f,.39f,.47f);
        static readonly Color MarineCyan=new Color(.42f,.91f,.94f);
        ManualBattle marineUiBattle;
        int marineUiRun=-1;
        bool marineCoverageHighlighted;
        MarineTideView marineTideView;
        public bool MarineCoverageHighlighted {get{SyncMarineFeedback();return marineCoverageHighlighted;}}
        public Rect MarineCoverageToggleRect {get;private set;}
        public int MarineNeuralHudCount {get;private set;}
        public int MarineBeneficiaryHudCount {get;private set;}
        bool MarineFieldFeedbackVisible=>CampaignActive&&Battle!=null&&Loop.CanDeploy&&Battle.MarineTier>0&&Battle.Phase==BattlePhase.Running&&
            !loopMenuOpen&&!loopTestSettlementOpen&&!shopVisible&&!ShopPanelTransitioning&&!CampaignUiModalOpen;

        void SyncMarineFeedback()
        {
            if(marineUiBattle!=Battle||marineUiRun!=(Battle?.RunId??-1))
            {marineUiBattle=Battle;marineUiRun=Battle?.RunId??-1;marineCoverageHighlighted=false;MarineCoverageToggleRect=Rect.zero;}
            // EngagementView has its own scene object; it is not a child of this controller.
            if(combatView!=null&&(marineTideView==null||marineTideView.gameObject!=combatView.gameObject))
                marineTideView=combatView.GetComponent<MarineTideView>();
            // Coverage inspection is a presentation selection. A shop transition or modal may
            // hide its labels, but must not silently undo a successful inspection action.
            if(marineTideView!=null)marineTideView.HighlightCoverage=marineCoverageHighlighted;
        }

        public bool ToggleMarineCoverage()
        {
            if(!CampaignActive||Battle==null||Battle.MarineTier==0||!Loop.CanDeploy)return false;
            SyncMarineFeedback();marineCoverageHighlighted=!marineCoverageHighlighted;
            if(marineTideView!=null)marineTideView.HighlightCoverage=marineCoverageHighlighted;
            return true;
        }

        void DrawMarineRow(Rect row)
        {
            if(Battle.MarineTier==0)return;
            string state=Battle.MarineCells.Count==0?"接敌落种":Battle.MarineActive?"蔓延中":"休眠";
            GUI.Label(SynergySupplementRect(row),$"溟痕 {Battle.MarineCells.Count}/{Battle.MarineTileLimit} · {state}",CampaignText(9,color:new Color(.60f,.88f,.91f)));
        }

        float DrawMarineBattleActions(float x,float y)
        {
            SyncMarineFeedback();
            Fill(new Rect(x,y,157,39),ApprovedUi.Paper);
            int kinds=CurrentSynergies.First(s=>s.Name=="海嗣").Count;
            int reserved=Math.Max(0,kinds-Battle.MarineSeededKinds);
            bool waitingForRoots=reserved>0&&Battle.MarineCells.Count>=Battle.MarineTileLimit-reserved;
            string state=Battle.IsPaused?"暂停":Battle.MarineCells.Count==0?"待接敌":!Battle.MarineActive?"休眠":Battle.MarineCells.Count>=Battle.MarineTileLimit?"已铺满":waitingForRoots?"待新根":"蔓延";
            GUI.Label(new Rect(x+7,y+1,143,18),$"溟痕 {Battle.MarineCells.Count}/{Battle.MarineTileLimit} · {state}",CampaignText(11,color:MarineInk,weight:600));
            string next=Battle.MarineCells.Count>=Battle.MarineTileLimit?"覆盖达到上限":waitingForRoots?$"预留{reserved}格":$"扩散 {Battle.MarineSpreadRemaining:0.0}s";
            GUI.Label(new Rect(x+7,y+20,143,17),$"落种 {Battle.MarineSeededKinds}/{kinds} · {next}",CampaignText(9,color:ApprovedUi.Muted));
            MarineCoverageToggleRect=new Rect(x,y+42,157,25);
            if(LoopButton(MarineCoverageToggleRect,marineCoverageHighlighted?"收起溟痕范围":"查看溟痕范围",marineCoverageHighlighted))ToggleMarineCoverage();
            return y+71;
        }

        void DrawMarineFieldFeedback()
        {
            SyncMarineFeedback();MarineNeuralHudCount=MarineBeneficiaryHudCount=0;
            if(!MarineFieldFeedbackVisible||Camera.main==null)return;
            var camera=Camera.main;
            foreach(var defender in Battle.Defenders)
            {
                if(!defender.Alive||defender.NeuralAccumulated<=0&&defender.NeuralUntil<=Battle.Clock)continue;
                Vector3 screen=camera.WorldToScreenPoint(defender.Position+camera.transform.up*1.6f);
                if(screen.z<=0)continue;
                Vector2 p=GUIPosition(screen);
                if(!MarineFeedbackPointVisible(p))continue;
                bool burst=defender.NeuralUntil>Battle.Clock;
                float progress=burst?Mathf.Clamp01((float)((defender.NeuralUntil-Battle.Clock)/10)):Mathf.Clamp01(defender.NeuralAccumulated/100);
                float top=p.y+(defender.Settings.Skill.Enabled?15:9);
                Fill(new Rect(p.x-25,top,50,5),new Color(.025f,.09f,.12f,.95f));
                Fill(new Rect(p.x-24,top+1,48*progress,3),burst?new Color(1,.84f,.42f):MarineCyan);
                string text=burst?$"爆发 {defender.NeuralUntil-Battle.Clock:0.0}s":$"神经 {defender.NeuralAccumulated:0}/100";
                Rect caption=new Rect(p.x-43,top+7,86,16);Fill(caption,new Color(.03f,.10f,.14f,.83f));
                GUI.Label(caption,text,CampaignText(10,TextAnchor.MiddleCenter,burst?new Color(1,.91f,.66f):MarineCyan,600));
                MarineNeuralHudCount++;
            }
            foreach(var unit in Battle.Units)
            {
                float multiplier=Battle.MarineNeuralMultiplier(unit);
                if(multiplier<=1)continue;
                Vector3 screen=camera.WorldToScreenPoint(unit.Motion.Position+camera.transform.up*UnitVisualHeight(unit));
                if(screen.z<=0)continue;
                Vector2 p=GUIPosition(screen);if(!MarineFeedbackPointVisible(p))continue;
                Rect label=new Rect(p.x-35,p.y-20,70,16);Fill(label,new Color(.03f,.14f,.17f,.89f));
                var effects=Battle.UnitCombat(unit).Effects;
                bool neuralSource=effects.NervousCreeper||effects.Harvester||effects.FirstSpeaker;
                GUI.Label(label,neuralSource?$"神经 ×{multiplier:0.#}":"溟痕前锋",CampaignText(10,TextAnchor.MiddleCenter,MarineCyan,600));
                MarineBeneficiaryHudCount++;
            }
            if(marineCoverageHighlighted)
            {
                Rect legend=new Rect(837,66,425,81);Fill(legend,ApprovedUi.Paper);ApprovedUi.Border(legend,ApprovedUi.Rule);
                GUI.Label(new Rect(849,70,399,20),"溟痕范围 · 亮边为已侵蚀地面",CampaignText(13,color:MarineInk,weight:600));
                GUI.Label(new Rect(849,91,399,24),"海嗣驻留增强原生神经；地面守军每秒积累3点。",CampaignText(11,color:MarineInk));
                int reserved=Math.Max(0,CurrentSynergies.First(s=>s.Name=="海嗣").Count-Battle.MarineSeededKinds);
                GUI.Label(new Rect(849,115,399,22),reserved>0?$"预留{reserved}格供尚未接敌的海嗣落种；部署新成员开辟前线。":"全部成员已落种；扩散覆盖至多"+Battle.MarineTileLimit+"格。",CampaignText(11,color:MarineInk));
            }
        }

        bool MarineFeedbackPointVisible(Vector2 point)=>point.x>184&&point.x<1260&&point.y>65&&point.y<523;
    }
}
