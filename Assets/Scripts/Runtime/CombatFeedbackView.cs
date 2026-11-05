using System;
using System.Text;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Draw-only labels: no RaycastTarget, GUI.Button, control ID or Event.Use.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class CombatFeedbackView : MonoBehaviour
    {
        readonly CombatFeedbackPresentation presentation=new CombatFeedbackPresentation();
        readonly StringBuilder statusText=new StringBuilder(64);
        Func<ManualBattle> battle;
        Func<double> clock;
        Func<bool> visible;
        Func<Vector2,bool> blocked;
        Func<Font> font;
        Func<CombatFeedbackTarget,string,float> height;
        GUIStyle numberStyle,shortStyle,stateStyle;
        Font styleFallback;
        public CombatFeedbackPresentation Presentation=>presentation;
        public void Bind(Func<ManualBattle> model,Func<double> presentationClock,Func<Font> fallbackFont,
            Func<bool> shouldDraw=null,Func<Vector2,bool> overlayBlocksPoint=null,Func<CombatFeedbackTarget,string,float> visualHeight=null)
        {battle=model;clock=presentationClock;font=fallbackFont;visible=shouldDraw;blocked=overlayBlocksPoint;height=visualHeight;presentation.Clear();numberStyle=shortStyle=stateStyle=null;}
        void LateUpdate()=>Refresh();
        public void Refresh()=>presentation.Sync(battle?.Invoke(),clock?.Invoke()??0);
        bool Project(Camera camera,Vector3 world,CombatFeedbackTarget type,string id,out Vector2 result)
        {
            Vector3 projected=camera.WorldToScreenPoint(world+camera.transform.up*(height?.Invoke(type,id)??1.7f));
            result=TacticalViewport.Current.FromScreen(projected);
            return projected.z>0&&new Rect(0,66,1280,642).Contains(result)&&!(blocked?.Invoke(result)??false);
        }
        void OnGUI()
        {
            if(battle?.Invoke()==null||Camera.main==null||!(visible?.Invoke()??true))return;
            Refresh();var model=battle();double now=clock();var camera=Camera.main;
            var previousMatrix=GUI.matrix;var previousColor=GUI.color;bool previousEnabled=GUI.enabled;
            try
            {
                GUI.matrix=TacticalViewport.Current.Matrix;GUI.color=Color.white;GUI.enabled=true;
                foreach(var bubble in presentation.Visible)
                {
                    if(!Project(camera,bubble.WorldPosition,bubble.TargetType,bubble.TargetId,out var p))continue;
                    float age=Mathf.Clamp01((float)((now-bubble.StartedAt)/CombatFeedbackPresentation.Lifetime));
                    float x=bubble.Kind==CombatFeedbackKind.HealthRestored?25:bubble.Kind==CombatFeedbackKind.ShieldAbsorbed?-27:0;
                    // Alternate two small lanes for nearby buckets; no random walk or huge jumps.
                    x+=(bubble.Bucket%2==0?-6:6);
                    Rect rect=new Rect(p.x-70+x,p.y-35-age*29,140,27);
                    if(blocked?.Invoke(rect.center)??false)continue;
                    Color color=CombatFeedbackPresentation.ColorFor(bubble.Kind,bubble.Status);
                    color.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,age));
                    DrawText(rect,CombatFeedbackPresentation.AmountText(bubble),color,bubble.Kind==CombatFeedbackKind.StatusApplied?14:18,650);
                }
                var states=model.CombatStatuses;int drawn=0;
                for(int i=0;i<states.Count&&drawn<64;i++)
                {
                    var first=states[i];bool already=false;
                    for(int j=0;j<i;j++)if(states[j].TargetType==first.TargetType&&states[j].TargetId==first.TargetId){already=true;break;}
                    if(already||!Project(camera,first.WorldPosition,first.TargetType,first.TargetId,out var p))continue;
                    statusText.Clear();int labels=0;
                    for(int j=i;j<states.Count;j++)
                    {
                        var s=states[j];if(s.TargetType!=first.TargetType||s.TargetId!=first.TargetId||s.Until<=model.Clock)continue;
                        if(labels++>0)statusText.Append(" · ");statusText.Append(CombatFeedbackPresentation.StatusText(s.Status));
                        if(labels==4)break;
                    }
                    if(labels==0)continue;
                    var rect=new Rect(p.x-100,p.y+19,200,18);if(blocked?.Invoke(rect.center)??false)continue;
                    DrawText(rect,statusText.ToString(),CombatFeedbackPresentation.ColorFor(CombatFeedbackKind.StatusApplied,first.Status),12,500);drawn++;
                }
            }
            finally{GUI.matrix=previousMatrix;GUI.color=previousColor;GUI.enabled=previousEnabled;}
        }
        void DrawText(Rect rect,string value,Color color,int size,int weight)
        {
            Font fallback=font?.Invoke();
            if(numberStyle==null||styleFallback!=fallback)
            {
                styleFallback=fallback;
                numberStyle=ApprovedUi.Text(fallback,18,TextAnchor.MiddleCenter,Color.white,650);
                shortStyle=ApprovedUi.Text(fallback,14,TextAnchor.MiddleCenter,Color.white,650);
                stateStyle=ApprovedUi.Text(fallback,12,TextAnchor.MiddleCenter,Color.white,500);
            }
            var style=size==18?numberStyle:size==14?shortStyle:stateStyle;
            style.normal.textColor=new Color(.015f,.025f,.035f,color.a*.9f);
            GUI.Label(new Rect(rect.x+1,rect.y+1,rect.width,rect.height),value,style);
            style.normal.textColor=color;GUI.Label(rect,value,style);
        }
        void OnDisable()=>presentation.Clear();
    }

    public sealed partial class ManualBattleController
    {
        bool CombatFeedbackPresentationPending=>Battle!=null&&Battle.CombatFeedback.Count>0&&
            PresentationClock<Battle.CombatFeedback[Battle.CombatFeedback.Count-1].At+CombatFeedbackPresentation.Lifetime;
        void InitializeCombatFeedback()
        {
            gameObject.AddComponent<CombatFeedbackView>().Bind(()=>Battle,()=>PresentationClock,()=>scene.InterfaceFont,
                ()=>!ArtLoading&&!TestSelectionOpen&&Battle!=null&&Battle.Phase!=BattlePhase.Ready,
                p=>ShopTrialActive?ShopUiBlocksPoint(p):!new Rect(0,66,1280,486).Contains(p)||(SkillMenuOpen&&SkillPopupRect.Contains(p)),
                (type,id)=>type==CombatFeedbackTarget.Monster&&Battle.FindUnit(id)!=null?UnitVisualHeight(Battle.FindUnit(id))+.12f:1.75f);
        }
    }
}
