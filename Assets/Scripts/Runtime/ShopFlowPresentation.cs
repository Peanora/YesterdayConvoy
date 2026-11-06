using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        const float ShopFlowLaunch=.055f,ShopFlowStagger=.042f,ShopFlowFlight=.470f;
        const float ShopFlowGather=.610f,ShopFlowReveal=.650f,ShopFlowFinish=1.350f,ShopFlowChain=.690f;
        sealed class ShopFlowSnapshot
        {
            internal readonly List<ShopTrialModel.Piece> Pieces=new List<ShopTrialModel.Piece>();
            internal readonly Dictionary<int,Rect> Bounds=new Dictionary<int,Rect>();
        }
        sealed class ShopFlowMaterial { internal int Instance; internal Rect Bounds; internal bool Existing; }
        sealed class ShopFlowStep
        {
            internal int Star;
            internal Rect Anchor;
            internal readonly List<ShopFlowMaterial> Materials=new List<ShopFlowMaterial>();
        }
        sealed class ShopFlow
        {
            internal string Id;
            internal int Instance,OldStar,Star;
            internal float At,Reveal,Duration;
            internal Vector2 Origin;
            internal readonly List<ShopFlowStep> Steps=new List<ShopFlowStep>();
            internal bool Merge=>Steps.Count>0;
        }
        readonly List<ShopFlow> shopFlows=new List<ShopFlow>();

        ShopFlowSnapshot CaptureShopFlowSnapshot()
        {
            var snapshot=new ShopFlowSnapshot();if(shop==null)return snapshot;
            for(int i=0;i<shop.Bench.Count;i++)
            {
                var piece=shop.Bench[i];snapshot.Pieces.Add(piece.Copy());
                snapshot.Bounds[piece.Instance]=ShopPieceRect(i);
            }
            return snapshot;
        }
        void BeginShopFlow(ShopFlowSnapshot snapshot,ShopTrialModel.Result result,Vector2 origin)
        {
            if(snapshot==null||result==null||!result.Success||shop==null)return;
            PruneShopFlows();EnsureShopAura();
            var flow=new ShopFlow{Id=result.Id,Instance=result.Instance,Star=result.FinalStar,At=Time.unscaledTime,
                Origin=origin,OldStar=snapshot.Pieces.FirstOrDefault(p=>p.Instance==result.Instance)?.Star??1};
            // This repeats the model's read-only grouping order only to locate consumed visual sources.
            // The incoming placeholder is never a real card ID and never reaches game state.
            const int incoming=int.MinValue;
            var planned=snapshot.Pieces.Select(p=>p.Copy()).ToList();
            planned.Add(new ShopTrialModel.Piece{Id=result.Id,Instance=incoming,Star=1});
            for(int tier=1;tier<3;tier++)
            {
                var group=planned.Where(p=>p.Id==result.Id&&p.Star==tier).Take(3).ToArray();
                if(group.Length!=3)continue;
                var step=new ShopFlowStep{Star=tier+1,Anchor=snapshot.Bounds.TryGetValue(group[0].Instance,out var anchor)?anchor:new Rect(origin,Vector2.one)};
                foreach(var material in group)
                {
                    bool existing=snapshot.Bounds.TryGetValue(material.Instance,out var bounds);
                    step.Materials.Add(new ShopFlowMaterial{Instance=material.Instance,Bounds=existing?bounds:new Rect(origin,Vector2.one),Existing=existing});
                }
                flow.Steps.Add(step);group[0].Star++;planned.Remove(group[1]);planned.Remove(group[2]);
            }
            if(flow.Merge)
            {
                var consumed=new HashSet<int>(flow.Steps.SelectMany(s=>s.Materials).Select(m=>m.Instance));
                shopFlows.RemoveAll(old=>consumed.Contains(old.Instance));
                flow.Reveal=ShopFlowReveal+(flow.Steps.Count-1)*ShopFlowChain;
                flow.Duration=ShopFlowFinish+(flow.Steps.Count-1)*ShopFlowChain;
                shopPromotedInstance=result.Instance;shopPromotionAt=flow.At+flow.Reveal;
            }
            else {flow.Reveal=.630f;flow.Duration=.950f;}
            shopFlows.Add(flow);
            var arrival=shopArrivals.LastOrDefault(a=>a.Instance==result.Instance);
            if(arrival!=null){arrival.At=flow.At;arrival.Origin=origin;arrival.BoundToFlow=true;arrival.Merge=flow.Merge;}
            UpdateShopBenchModels();
        }
        void ClearShopFlows(){shopFlows.Clear();shopPromotedInstance=-1;shopPromotionAt=-100;}
        void PruneShopFlows()
        {
            if(!ShopTrialActive||Battle==null||Loop!=null&&!Loop.CanShop||Loop==null&&Battle.Phase!=BattlePhase.Ready)
            {ClearShopFlows();return;}
            float now=Time.unscaledTime;
            shopFlows.RemoveAll(f=>now-f.At>f.Duration||!shop.Bench.Any(p=>p.Instance==f.Instance)||shopMoving&&shopDrag==f.Instance);
        }
        float ShopFlowPromotionRemaining(int instance)
        {
            if(shop==null)return 0;
            float now=Time.unscaledTime;
            for(int i=shopFlows.Count-1;i>=0;i--)
            {
                var flow=shopFlows[i];float age=now-flow.At;
                if(flow.Instance==instance&&flow.Merge&&age>=flow.Reveal&&age<flow.Duration)
                    return 1-Mathf.Clamp01((age-flow.Reveal)/(flow.Duration-flow.Reveal));
            }
            return 0;
        }
        int ShopDisplayedStar(ShopTrialModel.Piece piece)
        {
            for(int i=shopFlows.Count-1;i>=0;i--)
                if(shopFlows[i].Instance==piece.Instance&&shopFlows[i].Merge&&Time.unscaledTime-shopFlows[i].At<shopFlows[i].Reveal)
                    return shopFlows[i].OldStar;
            return piece.Star;
        }
        static Vector2 ShopFlowFoot(Rect rect)=>new Vector2(rect.center.x,rect.y+55);
        static float ShopFlowEase(float t)=>t<.5f?2*t*t:1-Mathf.Pow(-2*t+2,2)/2;
        static Vector2 ShopFlowPoint(Vector2 a,Vector2 b,float t)
        {
            Vector2 delta=b-a,first=a+new Vector2(delta.x*.14f,delta.y*.5f),second=b-new Vector2(delta.x*.2f,delta.y*.2f);
            float u=1-t;return u*u*u*a+3*u*u*t*first+3*u*t*t*second+t*t*t*b;
        }
        void DrawShopFlows()
        {
            PruneShopFlows();if(Event.current.type!=EventType.Repaint||shopFlows.Count==0)return;
            float now=Time.unscaledTime;
            foreach(var flow in shopFlows)
            {
                int slot=shop.Bench.ToList().FindIndex(p=>p.Instance==flow.Instance);if(slot<0)continue;
                var bounds=ShopPieceRect(slot);if(bounds.width<=0||bounds.height<=0)continue;
                Vector2 end=ShopFlowFoot(bounds);float elapsed=now-flow.At;
                if(!flow.Merge)
                {
                    float progress=(elapsed-.055f)/.560f;
                    if(progress>=0&&progress<=1)DrawShopFlowTrace(flow.Origin,end,progress,new Color(.90f,.80f,.57f));
                    DrawShopFlowBurst(end,(elapsed-.615f)/.300f,new Color(.91f,.81f,.59f),1);
                    continue;
                }
                for(int index=0;index<flow.Steps.Count;index++)
                {
                    var step=flow.Steps[index];float local=elapsed-index*ShopFlowChain;
                    bool final=index==flow.Steps.Count-1;
                    Vector2 anchor=final?end:ShopFlowFoot(step.Anchor);Color color=ShopRankColor(step.Star);
                    for(int n=0;n<step.Materials.Count;n++)
                    {
                        var material=step.Materials[n];Vector2 start=material.Existing?ShopFlowFoot(material.Bounds):flow.Origin;
                        float progress=(local-ShopFlowLaunch-n*ShopFlowStagger)/ShopFlowFlight;
                        if(progress>=0&&progress<=1&&Vector2.Distance(start,anchor)>8)
                        {
                            Vector2 head=DrawShopFlowTrace(start,anchor,progress,color);
                            DrawShopFlowPortrait(flow.Id,head+new Vector2(0,-6),24,.48f);
                        }
                        if(local>0&&local<.190f&&material.Existing)DrawShopFlowPortrait(flow.Id,start+new Vector2(0,-16),55,(1-local/.190f)*.7f);
                    }
                    DrawShopFlowBurst(anchor,(local-ShopFlowGather)/.300f,color,step.Star);
                    if(!final&&local>.620f&&local<.890f)
                    {
                        DrawShopFlowPortrait(flow.Id,anchor+new Vector2(0,-22),53,Mathf.Min(1,(.890f-local)/.150f));
                        GUI.Label(new Rect(anchor.x-32,anchor.y-59,64,19),"★★",ApprovedUi.Text(scene.InterfaceFont,13,TextAnchor.MiddleCenter,color,600));
                    }
                }
            }
        }
        void DrawShopFlowPortrait(string id,Vector2 center,float size,float alpha)
        {
            if(!shopDefinitions.TryGetValue(id,out var data))return;
            Color old=GUI.color;GUI.color=old*new Color(1,1,1,Mathf.Clamp01(alpha));
            DrawTestPortrait(new Rect(center.x-size/2,center.y-size/2,size,size),OriginalTalulahArt.CardPortrait(data.Card));GUI.color=old;
        }
        Vector2 DrawShopFlowTrace(Vector2 start,Vector2 end,float progress,Color color)
        {
            float p=Mathf.Clamp01(progress);
            for(int strand=0;strand<3;strand++)
            {
                float offset=(strand-1)*2.67f,lag=strand*.012f;Vector2 previous=Vector2.zero;
                for(int segment=0;segment<=24;segment++)
                {
                    float q=Mathf.Clamp01(p-lag-.17f+segment*.17f/24);
                    Vector2 point=ShopFlowPoint(start,end,ShopFlowEase(q))+new Vector2(1,.5f)*Mathf.Sin(q*Mathf.PI)*offset;
                    if(segment>0)ShopLine(previous,point,new Color(color.r,color.g,color.b,strand==1?.72f:.35f),strand==1?2.7f:.9f);
                    previous=point;
                }
            }
            Vector2 head=ShopFlowPoint(start,end,ShopFlowEase(p));Color old=GUI.color;
            GUI.color=new Color(color.r,color.g,color.b,.55f);GUI.DrawTexture(new Rect(head.x-9,head.y-9,18,18),shopAura);
            GUI.color=new Color(1,.97f,.85f,.95f);GUI.DrawTexture(new Rect(head.x-3,head.y-3,6,6),shopAura);GUI.color=old;
            return head;
        }
        void DrawShopFlowBurst(Vector2 point,float progress,Color color,int star)
        {
            if(progress<0||progress>1)return;
            float fade=Mathf.Sin(progress*Mathf.PI),radius=20+31*progress;Color old=GUI.color;
            GUI.color=new Color(color.r,color.g,color.b,fade*.6f);GUI.DrawTexture(new Rect(point.x-radius,point.y-radius,radius*2,radius*2),shopAura);
            if(star>1){GUI.color=new Color(color.r,color.g,color.b,fade*.5f);GUI.DrawTexture(new Rect(point.x-16,point.y-113,32,120),shopAura);}
            GUI.color=old;
            float width=17+16*progress,height=3+3*progress;
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI*2/24,b=(i+1)*Mathf.PI*2/24;
                ShopLine(point+new Vector2(Mathf.Cos(a)*width,Mathf.Sin(a)*height+5),point+new Vector2(Mathf.Cos(b)*width,Mathf.Sin(b)*height+5),new Color(color.r,color.g,color.b,fade*.65f),1);
            }
            for(int i=0;i<(star>1?12:6);i++)
            {
                Vector2 spark=point+new Vector2(Mathf.Sin(i*2.4f)*37*progress,-12-(10+i*5.3f)*progress);
                ShopLine(spark,spark+new Vector2(Mathf.Cos(i)*2,-4),new Color(color.r,color.g,color.b,fade*.8f),.8f);
            }
        }
    }
}
