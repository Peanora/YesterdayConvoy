using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy
{
    /// <summary>Read-only infection blast range, actual-hit markers and short labels. Native fire remains owned by CommonCombatVfxDirector.</summary>
    public sealed class InfectionBurstFeedback:MonoBehaviour
    {
        public const float FeedbackLifetime=1.2f;
        const int Segments=64;
        static readonly Color Amber=new Color(1,.70f,.35f);
        readonly List<LineRenderer> markers=new List<LineRenderer>();
        readonly List<Label> labels=new List<Label>();
        struct Label {internal Vector2 Point;internal float Alpha;internal int Hits;}
        ManualBattleController owner;ManualBattle observed;int run=-1;
        Material material;GUIStyle textStyle;
        public int VisibleMarkers {get;private set;}
        public int VisibleBursts {get;private set;}
        public int VisibleLabels=>labels.Count;
        public int LastVisibleHitCount {get;private set;}
        public double PendingUntil
        {
            get
            {
                var battle=owner?.Battle;double end=double.NegativeInfinity;
                if(battle!=null)foreach(var burst in battle.InfectionBursts)
                    if(burst.Resolved&&burst.At<=battle.Clock+1e-6)end=Math.Max(end,burst.At+FeedbackLifetime);
                return end;
            }
        }
        public void Bind(ManualBattleController controller){owner=controller;Hide();observed=null;run=-1;}
        LineRenderer Marker(int index)
        {
            while(markers.Count<=index)
            {
                if(material==null)material=new Material(Shader.Find("Sprites/Default")){name="Infection burst / functional markers"};
                var r=new GameObject("Infection burst / marker",typeof(LineRenderer)).GetComponent<LineRenderer>();r.transform.SetParent(transform,false);
                r.sharedMaterial=material;r.useWorldSpace=true;r.loop=true;r.positionCount=Segments;r.widthMultiplier=.025f;
                r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.numCornerVertices=2;r.enabled=false;markers.Add(r);
            }
            return markers[index];
        }
        void Ring(ref int used,string name,Vector3 position,float radius,float width,Color color,Camera camera)
        {
            var r=Marker(used++);r.name="Infection burst / "+name;r.enabled=true;r.widthMultiplier=width;
            r.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,position)+45;r.startColor=r.endColor=color;
            for(int i=0;i<Segments;i++)
            {
                float angle=i*Mathf.PI*2/Segments;
                r.SetPosition(i,position+new Vector3(Mathf.Cos(angle)*radius,.035f,Mathf.Sin(angle)*radius));
            }
        }
        void LateUpdate()
        {
            var battle=owner?.Battle;var camera=Camera.main;
            if(observed!=battle||run!=(battle?.RunId??-1)){Hide();observed=battle;run=battle?.RunId??-1;}
            labels.Clear();LastVisibleHitCount=VisibleBursts=0;int used=0;
            if(battle!=null&&camera!=null&&!owner.TestSelectionOpen&&!owner.ArtLoading&&battle.Phase!=BattlePhase.Ready)
            {
                double clock=owner.PresentationClock;
                foreach(var burst in battle.InfectionBursts)
                {
                    float age=(float)(clock-burst.At);
                    if(age<-.2001f||age>=FeedbackLifetime)continue;
                    if(age<0||burst.Resolved&&age<.56f)
                    {
                        Color color=Amber;color.a=age<0?.42f:.56f*(1-age/.56f);
                        Ring(ref used,"real damage radius",burst.Position,burst.Radius,.023f,color,camera);
                    }
                    if(!burst.Resolved||age<0)continue;
                    if(age<.9f)VisibleBursts++;
                    if(age<.22f)
                    {
                        float progress=Mathf.SmoothStep(0,1,age/.22f);Color color=new Color(1,.91f,.67f,(1-progress)*.75f);
                        Ring(ref used,"impact wave",burst.Position,burst.Radius*Mathf.Lerp(.25f,1,progress),.035f,color,camera);
                    }
                    if(age<.28f)
                    {
                        Color color=Amber;color.a=.8f*(1-age/.28f);
                        foreach(var hit in burst.Hits)
                            Ring(ref used,"actual hit "+hit.EntityId,hit.Position,Mathf.Lerp(.22f,.34f,age/.28f),.03f,color,camera);
                    }
                    if(!owner.InfectionFeedbackLabelsVisible)continue;
                    Vector3 screen=camera.WorldToScreenPoint(burst.Position+camera.transform.up*1.15f);
                    if(screen.z<=camera.nearClipPlane)continue;
                    Vector2 point=TacticalViewport.Current.FromScreen(screen);
                    point.x=Mathf.Clamp(point.x,286,1168);point.y=Mathf.Clamp(point.y,88,516);
                    if(!owner.InfectionFeedbackPointVisible(point))continue;
                    float alpha=Mathf.Min(Mathf.Clamp01(age/.05f),Mathf.Clamp01((FeedbackLifetime-age)/.3f));
                    labels.Add(new Label{Point=point,Alpha=alpha,Hits=burst.Hits.Count});LastVisibleHitCount=burst.Hits.Count;
                }
            }
            VisibleMarkers=used;for(int i=used;i<markers.Count;i++)markers[i].enabled=false;
        }
        void OnGUI()
        {
            if(owner==null||!owner.InfectionFeedbackLabelsVisible||labels.Count==0)return;
            var matrix=GUI.matrix;Color color=GUI.color;bool enabled=GUI.enabled;
            try
            {
                GUI.matrix=TacticalViewport.Current.Matrix;GUI.enabled=true;
                if(textStyle==null)textStyle=ApprovedUi.Text(null,14,TextAnchor.MiddleCenter,new Color(.39f,.23f,.13f),600);
                foreach(var label in labels)
                {
                    GUI.color=new Color(1,1,1,label.Alpha);Rect r=new Rect(label.Point.x-104,label.Point.y-20,208,29);
                    Color fill=GUI.color;GUI.color=new Color(.94f,.94f,.90f,.96f*label.Alpha);GUI.DrawTexture(r,Texture2D.whiteTexture);
                    GUI.color=new Color(.73f,.37f,.16f,label.Alpha);GUI.DrawTexture(new Rect(r.x,r.y,3,r.height),Texture2D.whiteTexture);
                    GUI.color=fill;GUI.Label(new Rect(r.x+6,r.y,r.width-9,r.height),"感染爆裂 · 命中 "+label.Hits,textStyle);
                }
            }
            finally {GUI.matrix=matrix;GUI.color=color;GUI.enabled=enabled;}
        }
        void Hide(){labels.Clear();VisibleMarkers=VisibleBursts=LastVisibleHitCount=0;foreach(var r in markers)if(r!=null)r.enabled=false;}
        void Release(){Hide();foreach(var r in markers)if(r!=null)Destroy(r.gameObject);markers.Clear();if(material!=null)Destroy(material);material=null;}
        void OnDisable()=>Release();
        void OnDestroy()=>Release();
    }
    public sealed partial class ManualBattleController
    {
        internal bool InfectionFeedbackLabelsVisible=>Battle!=null&&Battle.Phase!=BattlePhase.Ready&&!ArtLoading&&!TestSelectionOpen&&
            !loopMenuOpen&&!loopTestSettlementOpen&&!shopVisible&&!ShopPanelTransitioning&&!CampaignUiModalOpen;
        internal bool InfectionFeedbackPointVisible(Vector2 point)=>new Rect(178,66,1086,486).Contains(point)&&
            (ShopTrialActive?!ShopUiBlocksPoint(point):!SkillMenuOpen||!SkillPopupRect.Contains(point));
    }
}
