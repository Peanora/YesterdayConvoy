using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        int waveClickInstance=-1,waveClickRun,waveClickIndex;
        Vector2 waveClickStart;
        Vector2 routeLastGuiPointer;
        bool routeGuiPointerKnown;
        bool WaveClickBattle=>ShopTrialActive&&Battle!=null&&Battle.WavesEnabled&&
            (Loop==null?Battle.Phase==BattlePhase.Running:Loop.CanDeploy&&!Loop.CanShop);

        void CancelWaveCardClick(){waveClickInstance=-1;}
        void ValidateWaveCardClick()
        {
            if(waveClickInstance>=0&&(!WaveClickBattle||Battle.RunId!=waveClickRun||
                Battle.CurrentWaveIndex!=waveClickIndex||!Battle.DeploymentWindowOpen))CancelWaveCardClick();
        }

        // A click owns one down/up pair. Moving off the original card never becomes a deployment drag.
        bool HandleWaveCardPointer(EventType type,Vector2 point,int button)
        {
            ValidateWaveCardClick();
            if(!WaveClickBattle)return false;
            if(button!=0){bool pressed=waveClickInstance>=0;CancelWaveCardClick();return pressed;}
            if(type==EventType.MouseDown)
            {
                CancelWaveCardClick();
                for(int i=0;i<shop.Bench.Count;i++)
                {
                    if(!ShopPieceRect(i).Contains(point))continue;
                    var piece=shop.Bench[i];
                    if(!IsLineupPiece(piece))return true;
                    CloseSkillMenu();CloseDefenderInspector();Dragging=false;Selected=false;
                    shopDrag=-1;shopMoving=false;
                    if(!Battle.DeploymentWindowOpen)
                    {ShopNotice("出兵已结束，场上单位继续推进；仍可使用技能");return true;}
                    waveClickInstance=piece.Instance;waveClickRun=Battle.RunId;
                    waveClickIndex=Battle.CurrentWaveIndex;waveClickStart=point;
                    return true;
                }
                return false;
            }
            if(waveClickInstance<0)return false;
            int bench=shop.Bench.ToList().FindIndex(p=>p.Instance==waveClickInstance);
            if(bench<0){CancelWaveCardClick();return true;}
            bool inside=ShopPieceRect(bench).Contains(point)&&(point-waveClickStart).sqrMagnitude<=64;
            if(type==EventType.MouseMove||type==EventType.MouseDrag)
            {if(!inside)CancelWaveCardClick();return true;}
            if(type!=EventType.MouseUp)return false;
            CancelWaveCardClick();
            if(!inside)return true;
            var selected=shop.Bench[bench];int index=shopLineup.IndexOf(selected.Id);
            var lane=Battle.Lanes.FirstOrDefault(l=>l.Id==Battle.CurrentWaveLaneId);
            if(index<0||!IsLineupPiece(selected)||lane==null||!Battle.CanDeployOnLane(lane.Id))return true;
            selectedRosterIndex=index;Selected=true;
            var receipt=TryDeployAt(lane.Entry,true);Selected=false;
            if(receipt.Success)Feedback=$"已部署至 {lane.Id} · 剩余 {Battle.DP} DP";
            ShopNotice(receipt.Success?Feedback:Explain(receipt.Failure));
            return true;
        }

        static string WaveClockText(double seconds)
        {
            int total=(int)Math.Ceiling(Math.Max(0,seconds));
            return $"{total/60:00}:{total%60:00}";
        }
        public Rect WaveHeaderContentRect
        {
            get
            {
                var badge=SarkazContractBadgeRect;
                float left=badge.width>0?Mathf.Max(219,badge.xMax+78):219;
                return new Rect(left,11,560-left,39);
            }
        }
        void DrawWaveHeader(bool preparing)
        {
            if(!Battle.WavesEnabled)return;
            ValidateWaveCardClick();
            var waves=Battle.WavePlan.Waves;
            var content=WaveHeaderContentRect;
            // Keep the existing white badge caption over the dark battlefield, outside this paper panel.
            Fill(new Rect(content.x-4,8,574-content.x,46),ApprovedUi.Paper);
            Fill(new Rect(737,8,273,46),ApprovedUi.Paper);
            if(preparing)
            {
                float width=content.width/Math.Max(1,waves.Count);
                for(int i=0;i<waves.Count;i++)
                {
                    float x=content.x+i*width;
                    GUI.Label(new Rect(x,11,width-5,21),$"{i+1}波 {waves[i].LaneId}",LoopText(11,TextAnchor.MiddleLeft,null,600));
                    GUI.Label(new Rect(x,33,width-5,16),$"{waves[i].DP}DP · {waves[i].DeploymentSeconds:0}秒",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                }
                GUI.Label(new Rect(747,11,253,22),$"全程 {WaveClockText(Battle.WavePlan.TotalDuration)} · 末段推进 {Battle.WavePlan.FinalAdvanceSeconds:0} 秒",LoopText(12,TextAnchor.MiddleLeft,null,600));
                GUI.Label(new Rect(747,34,253,15),"换波清余量 · 次数/CD/双方状态全关保留",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                return;
            }
            bool terminal=Battle.Phase==BattlePhase.Won||Battle.Phase==BattlePhase.Lost;
            string phase=terminal?(Battle.TimedOut?"时间耗尽":Battle.Phase==BattlePhase.Won?"突破成功":"作战结束"):
                Battle.FinalAdvanceActive?"最终推进 · 禁止出兵":$"第 {Battle.CurrentWaveIndex+1}/{Battle.WaveCount} 波 · {Battle.CurrentWaveLaneId}";
            string remaining=terminal?"":$"  {WaveClockText(Battle.WaveRemaining)}";
            GUI.Label(new Rect(content.x,11,content.width,22),phase+remaining,LoopText(15,TextAnchor.MiddleLeft,null,700));
            GUI.Label(new Rect(content.x,35,content.width,15),Battle.FinalAdvanceActive?"停止出兵 · 场上继续作战 / 可用技能":
                $"本波预算 {Battle.CurrentWaveBudget} DP · 当前 {Battle.DP} DP · 整关共享次数",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            int next=Battle.CurrentWaveIndex+1;
            string preview=next<waves.Count?$"下波 {waves[next].LaneId} · {waves[next].DP} DP · {waves[next].DeploymentSeconds:0} 秒":
                Battle.FinalAdvanceActive?"全部出兵结束":"下阶段：最终推进，禁止出兵";
            GUI.Label(new Rect(747,11,253,22),preview,LoopText(12,TextAnchor.MiddleLeft,null,600));
            GUI.Label(new Rect(747,35,253,15),$"总截止剩余 {WaveClockText(Battle.BattleTimeRemaining)} · 余量不结转",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
        }

        // Repaint also sees pointer motion when the Player sends no MouseMove event.
        void TrackRouteHover(Vector2 point)
        {
            if(!routeGuiPointerKnown||point!=routeLastGuiPointer)shopPointer=point;
            routeLastGuiPointer=point;routeGuiPointerKnown=true;
        }

        // A preview replaces the current guide instead of layering another route over it.
        DeploymentLane RoutePreviewLane()
        {
            if(Battle==null||!ShopMapPointAvailable(shopPointer)||
                !PointerGround(ScreenPosition(shopPointer),out var world))return null;
            return Battle.FindEntryLane(world);
        }

        // Both the campaign maps and forest render the same underlay, moving highlights and arrows.
        public IReadOnlyList<WaveRouteGuideStroke> WaveRouteGuideStrokes()
        {
            var result=new List<WaveRouteGuideStroke>();
            if(Battle==null||Camera.main==null||Battle.Phase==BattlePhase.Won||Battle.Phase==BattlePhase.Lost)return result;
            var lane=RoutePreviewLane();
            if(lane==null&&Battle.WavesEnabled&&Battle.DeploymentWindowOpen)
                lane=Battle.Lanes.FirstOrDefault(item=>item.Id==Battle.CurrentWaveLaneId);
            if(lane==null)return result;
            bool open=Battle.WavesEnabled&&lane.Id==Battle.CurrentWaveLaneId&&Battle.DeploymentWindowOpen;
            var clip=new Rect(0,62,1280,488);var view=TacticalViewport.Current;var camera=Camera.main;
            AppendWaveRouteStrokes(result,lane,false,open,RouteGuideGeometry.FullPath(lane.Points),camera,view,clip);
            AppendWaveRouteStrokes(result,lane,true,open,RouteGuideGeometry.Pulses(lane.Points,Battle.Clock),camera,view,clip);
            return result;
        }
        void AppendWaveRouteStrokes(List<WaveRouteGuideStroke> result,DeploymentLane lane,bool pulse,bool open,
            List<RouteGuideSpan> spans,Camera camera,TacticalViewport view,Rect clip)
        {
            foreach(var span in spans)
                if(!Battle.ApprovedPortalSegment(lane.Id,span.EdgeIndex)&&
                    RouteGuideGeometry.TryProject(span,camera.WorldToScreenPoint,view,clip,out var projection))
                    result.Add(new WaveRouteGuideStroke(lane.Id,open,pulse,projection));
        }
        void DrawWaveRoutes()
        {
            if(Event.current.type!=EventType.Repaint)return;
            // No BeginGroup: rotating GUI.matrix inside a group also transforms its clip/origin.
            // All endpoints remain in the same 1280x720 canvas as projected unit feet and HUD.
            bool preview=RoutePreviewLane()!=null;
            foreach(var stroke in WaveRouteGuideStrokes())
            {
                var p=stroke.Projection;
                bool highlighted=stroke.Open||preview;
                if(!stroke.Pulse)
                {
                    DrawWaveRouteStroke(p.From,p.To,new Color(1,1,1,highlighted?.18f:.055f),1);
                    continue;
                }
                Color color=new Color(1,1,1,highlighted?.96f:.22f);
                if(highlighted)DrawWaveRouteStroke(p.From,p.To,new Color(.015f,.025f,.035f,.7f),4);
                DrawWaveRouteStroke(p.From,p.To,color,2);
                if(!p.HeadVisible)continue;
                Vector2 direction=(p.To-p.From).normalized,wing=new Vector2(-direction.y,direction.x)*2.5f;
                DrawWaveRouteStroke(p.To-direction*4+wing,p.To,color,1.6f);
                DrawWaveRouteStroke(p.To-direction*4-wing,p.To,color,1.6f);
            }
        }
        static void DrawWaveRouteStroke(Vector2 from,Vector2 to,Color color,float width)
        {
            Matrix4x4 previous=GUI.matrix;
            try
            {
                GUI.matrix=RouteGuideGeometry.StrokeMatrix(previous,from,to);
                Fill(new Rect(0,-width*.5f,Vector2.Distance(from,to),width),color);
            }
            finally{GUI.matrix=previous;}
        }
    }
}
