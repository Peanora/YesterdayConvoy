using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Procedural warning, blast and ice shards. Observes events; never applies damage.</summary>
    public sealed class MonsterDeathEffectView : MonoBehaviour
    {
        private sealed class Burst { internal LineRenderer Warning, Ring, Shards; }
        private readonly List<Burst> bursts=new List<Burst>();
        private ManualBattleController owner;
        private Material material;
        private int run=-1;
        public int VisibleBlasts { get; private set; }
        public void Initialize(ManualBattleController controller) { owner=controller;Clear();run=-1; }
        private LineRenderer Line(string label,float width)
        {
            if(material==null) material=new Material(Shader.Find("Sprites/Default"));
            var line=new GameObject(label,typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(transform,false);line.sharedMaterial=material;line.useWorldSpace=true;
            line.widthMultiplier=width;line.numCapVertices=3;line.enabled=false;return line;
        }
        private void LateUpdate()
        {
            if(owner?.Battle==null || owner.TestSelectionOpen) { Clear();return; }
            var battle=owner.Battle;if(run!=battle.RunId) { Clear();run=battle.RunId; }
            VisibleBlasts=0;var camera=Camera.main;if(camera==null)return;
            for(int i=0;i<battle.DeathBlasts.Count;i++)
            {
                if(i>=bursts.Count) bursts.Add(new Burst {Warning=Line("Death blast warning",.025f),Ring=Line("Death blast shockwave",.08f),Shards=Line("Death blast shards",.06f)});
                var burst=bursts[i];var data=battle.DeathBlasts[i];double time=owner.PresentationClock;
                double age=time-data.DetonateAt;bool warning=age<0,active=data.Resolved && age>=0 && age<.65;
                burst.Warning.enabled=warning;burst.Ring.enabled=burst.Shards.enabled=active;
                if(!warning && !active) continue;
                VisibleBlasts++;Color color=data.Cold ? new Color(.35f,.85f,1) : new Color(1,.5f,.08f);
                int order=DemoSpriteVisual.GetSortingOrder(camera,data.Position)+55;
                burst.Warning.sortingOrder=burst.Ring.sortingOrder=burst.Shards.sortingOrder=order;
                if(warning)
                {
                    float progress=(float)((time-data.ScheduledAt)/System.Math.Max(.001,data.DetonateAt-data.ScheduledAt));
                    color.a=.25f+.5f*progress;GroundCircle(burst.Warning,data.Position,data.Radius,color);continue;
                }
                float t=(float)age/.65f;color.a=1-t;
                GroundCircle(burst.Ring,data.Position,Mathf.Lerp(.15f,data.Radius,t),color);
                burst.Shards.loop=false;burst.Shards.positionCount=36;
                for(int j=0;j<12;j++)
                {
                    float angle=j*Mathf.PI/6;Vector3 axis=camera.transform.right*Mathf.Cos(angle)+camera.transform.up*Mathf.Sin(angle);
                    Vector3 center=data.Position+camera.transform.up*.35f;
                    float radius=data.Radius*(.25f+t*.65f);
                    burst.Shards.SetPosition(j*3,center+axis*radius*.6f);
                    burst.Shards.SetPosition(j*3+1,center+axis*radius);
                    burst.Shards.SetPosition(j*3+2,center+axis*radius*.6f);
                }
                burst.Shards.startColor=Color.Lerp(color,Color.white,.4f);burst.Shards.endColor=color;
            }
        }
        private static void GroundCircle(LineRenderer line,Vector3 center,float radius,Color color)
        {
            line.loop=true;line.positionCount=64;line.startColor=line.endColor=color;
            for(int i=0;i<64;i++) { float angle=i*Mathf.PI/32;line.SetPosition(i,center+Vector3.up*.08f+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius); }
        }
        private void Clear()
        {
            foreach(var burst in bursts) { Destroy(burst.Warning.gameObject);Destroy(burst.Ring.gameObject);Destroy(burst.Shards.gameObject); }
            bursts.Clear();VisibleBlasts=0;
        }
        private void OnDestroy() { Clear();if(material!=null) Destroy(material); }
    }
}
