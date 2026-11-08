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
        void DrawWaveHeader(bool preparing)
        {
            if(!Battle.WavesEnabled)return;
            ValidateWaveCardClick();
            var waves=Battle.WavePlan.Waves;
            Fill(new Rect(209,8,361,46),ApprovedUi.Paper);
            Fill(new Rect(737,8,273,46),ApprovedUi.Paper);
            if(preparing)
            {
                float width=341f/Math.Max(1,waves.Count);
                for(int i=0;i<waves.Count;i++)
                {
                    float x=219+i*width;
                    GUI.Label(new Rect(x,11,width-5,21),$"{i+1}波 {waves[i].LaneId} · {waves[i].DP}DP",LoopText(12,TextAnchor.MiddleLeft,null,600));
                    GUI.Label(new Rect(x,33,width-5,16),$"出兵 {waves[i].DeploymentSeconds:0} 秒",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                }
                GUI.Label(new Rect(747,11,253,22),$"全程 {WaveClockText(Battle.WavePlan.TotalDuration)} · 末段推进 {Battle.WavePlan.FinalAdvanceSeconds:0} 秒",LoopText(12,TextAnchor.MiddleLeft,null,600));
                GUI.Label(new Rect(747,34,253,15),"换波清余量 · 次数/CD/双方状态全关保留",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                return;
            }
            bool terminal=Battle.Phase==BattlePhase.Won||Battle.Phase==BattlePhase.Lost;
            string phase=terminal?(Battle.TimedOut?"时间耗尽":Battle.Phase==BattlePhase.Won?"突破成功":"作战结束"):
                Battle.FinalAdvanceActive?"最终推进 · 禁止出兵":$"第 {Battle.CurrentWaveIndex+1}/{Battle.WaveCount} 波 · {Battle.CurrentWaveLaneId}";
            string remaining=terminal?"":$"  {WaveClockText(Battle.WaveRemaining)}";
            GUI.Label(new Rect(219,11,341,22),phase+remaining,LoopText(15,TextAnchor.MiddleLeft,null,700));
            GUI.Label(new Rect(219,35,341,15),Battle.FinalAdvanceActive?"部署已关闭 · 场上单位继续作战，仍可释放技能":
                $"本波预算 {Battle.CurrentWaveBudget} DP · 当前 {Battle.DP} DP · 整关共享次数",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            int next=Battle.CurrentWaveIndex+1;
            string preview=next<waves.Count?$"下波 {waves[next].LaneId} · {waves[next].DP} DP · {waves[next].DeploymentSeconds:0} 秒":
                Battle.FinalAdvanceActive?"全部出兵结束":"下阶段：最终推进，禁止出兵";
            GUI.Label(new Rect(747,11,253,22),preview,LoopText(12,TextAnchor.MiddleLeft,null,600));
            GUI.Label(new Rect(747,35,253,15),$"总截止剩余 {WaveClockText(Battle.BattleTimeRemaining)} · 余量不结转",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
        }

        // The path is a presentation of immutable model lanes. Its phase uses the combat clock.
        void DrawWaveRoutes()
        {
            if(!Battle.WavesEnabled||Camera.main==null)return;
            var occupied=new HashSet<string>(Battle.Units.Where(u=>u.Active).Select(u=>u.RouteId));
            string current=Battle.CurrentWaveLaneId;
            var clip=new Rect(0,60,1280,492);
            GUI.BeginGroup(clip);
            try
            {
                foreach(var lane in Battle.Lanes)
                {
                    bool open=lane.Id==current&&Battle.DeploymentWindowOpen;
                    if(!open&&!occupied.Contains(lane.Id))continue;
                    DrawWaveLane(lane,open?new Color(1,1,1,.96f):new Color(1,1,1,.28f),clip.position);
                }
            }
            finally{GUI.EndGroup();}
        }
        void DrawWaveLane(DeploymentLane lane,Color color,Vector2 origin)
        {
            const double spacing=.9,dash=.4;
            double phase=Battle.Clock*.8%spacing,travelled=0;
            for(int i=1;i<lane.Points.Count;i++)
            {
                Vector3 a=lane.Points[i-1],b=lane.Points[i];double length=Vector3.Distance(a,b);
                if(length<.0001)continue;
                double end=travelled+length;
                double start=Math.Floor((travelled-phase)/spacing)*spacing+phase;
                for(;start<end;start+=spacing)
                {
                    double from=Math.Max(travelled,start),to=Math.Min(end,start+dash);
                    if(to<=from)continue;
                    Vector3 p=Vector3.Lerp(a,b,(float)((from-travelled)/length))+Vector3.up*.06f;
                    Vector3 q=Vector3.Lerp(a,b,(float)((to-travelled)/length))+Vector3.up*.06f;
                    Vector2 p2=GUIPosition(Camera.main.WorldToScreenPoint(p))-origin,q2=GUIPosition(Camera.main.WorldToScreenPoint(q))-origin;
                    ShopLine(p2,q2,color,2);
                }
                travelled=end;
            }
        }
    }
}
