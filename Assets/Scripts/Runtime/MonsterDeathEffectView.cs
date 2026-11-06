using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Only explosive-death events. Common birth/death remains owned by EngagementView.</summary>
    public sealed class MonsterDeathEffectView:MonoBehaviour
    {
        readonly List<LineRenderer> warnings=new List<LineRenderer>();
        ManualBattleController owner;ManualBattle observed;Material material;
        OriginalCombatVfxLayer layer;int run=-1;
        public int VisibleBlasts{get;private set;}
        public void Initialize(ManualBattleController controller){owner=controller;Clear();run=-1;}
        LineRenderer Warning(int index)
        {
            while(warnings.Count<=index)
            {
                if(material==null)material=new Material(Shader.Find("Sprites/Default"));
                var r=new GameObject("Explosive death / functional danger radius",typeof(LineRenderer)).GetComponent<LineRenderer>();
                r.transform.SetParent(transform,false);r.sharedMaterial=material;r.loop=true;r.useWorldSpace=true;r.widthMultiplier=.025f;r.positionCount=64;r.enabled=false;warnings.Add(r);
            }
            return warnings[index];
        }
        void LateUpdate()
        {
            if(layer==null)layer=new OriginalCombatVfxLayer(transform);
            var b=owner?.Battle;var cam=Camera.main;
            if(observed!=b||run!=(b?.RunId??-1)){Clear();observed=b;run=b?.RunId??-1;}
            layer.Begin();VisibleBlasts=0;int used=0;
            if(b!=null&&cam!=null&&!owner.TestSelectionOpen)
            {
                double now=owner.PresentationClock;
                foreach(var e in b.DeathBlasts)
                {
                    double age=now-e.DetonateAt;
                    if(age<0&&now>=e.ScheduledAt)
                    {
                        var r=Warning(used++);r.enabled=true;r.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,e.Position)+45;
                        Color color=e.Cold?new Color(.35f,.85f,1,.6f):new Color(1,.5f,.08f,.6f);r.startColor=r.endColor=color;
                        for(int k=0;k<64;k++){float a=k*Mathf.PI/32;r.SetPosition(k,e.Position+new Vector3(Mathf.Cos(a)*e.Radius,.08f,Mathf.Sin(a)*e.Radius));}
                        VisibleBlasts++;
                    }
                    if(e.Resolved&&layer.ShowGround("monster/"+(e.Cold?"snslime_die_01_start":"bslime_die_01_start"),age,e.Position,cam,false,50))VisibleBlasts++;
                }
            }
            for(int i=used;i<warnings.Count;i++)warnings[i].enabled=false;
            layer.End();
        }
        void Clear(){layer?.Clear();foreach(var r in warnings)if(r!=null)r.enabled=false;VisibleBlasts=0;}
        void OnDisable(){Clear();}
        void OnDestroy(){layer?.Dispose();foreach(var r in warnings)if(r!=null)Destroy(r.gameObject);if(material!=null)Destroy(material);}
    }
}
