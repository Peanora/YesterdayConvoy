using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class MandragoraEffectView:MonoBehaviour
    {
        Func<ManualBattle> source;
        Material material;
        readonly List<LineRenderer> lines=new List<LineRenderer>();
        int used;
        public void Bind(Func<ManualBattle> battle){source=battle;}
        LineRenderer Line(float width,Color color,int count)
        {
            if(used==lines.Count)
            {
                var l=new GameObject("Stone and gaze effect").AddComponent<LineRenderer>();l.transform.SetParent(transform,false);
                if(material==null)material=new Material(Shader.Find("Sprites/Default"));
                l.sharedMaterial=material;l.useWorldSpace=true;lines.Add(l);
            }
            var line=lines[used++];line.enabled=true;line.widthMultiplier=width;
            line.startColor=line.endColor=color;line.positionCount=count;return line;
        }
        void Ring(Vector3 p,float radius,Color color,float width)
        {
            var line=Line(width,color,49);
            for(int k=0;k<=48;k++){float a=k*Mathf.PI/24;line.SetPosition(k,p+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
        }
        void LateUpdate()
        {
            used=0;var b=source?.Invoke();
            if(b!=null&&b.EngagementEnabled&&b.CombatSettings.Effects.Mandragora)
            {
                foreach(var u in b.Units)
                {
                    if(!u.Active)continue;
                    if(u.StoneShield)
                    {
                        Ring(u.Motion.Position+Vector3.up*.65f,.65f,new Color(.67f,.73f,.55f,.8f),.045f);
                        for(int j=0;j<4;j++)
                        {
                            float a=(float)b.Clock*1.2f+j*Mathf.PI/2;
                            Vector3 p=u.Motion.Position+new Vector3(Mathf.Cos(a)*.65f,1+Mathf.Sin(a*2)*.12f,Mathf.Sin(a)*.65f);
                            var stone=Line(.1f,new Color(.54f,.57f,.49f),2);stone.SetPosition(0,p);stone.SetPosition(1,p+Vector3.up*.16f);
                        }
                    }
                    if(b.UnitAirborne(u))Ring(u.Motion.Position+Vector3.up*.1f,1.7f,new Color(.74f,.82f,.3f,.32f),.015f);
                    var g=u.Gaze;
                    if(g==null||g.Ended||b.Clock<g.StartedAt+.8333334)continue;
                    var d=b.FindDefender(g.TargetId);if(d==null||!d.Alive)continue;
                    Vector3 up=Camera.main!=null?Camera.main.transform.up:Vector3.up;
                    var beam=Line(.055f,new Color(.91f,.94f,.37f),2);
                    beam.SetPosition(0,u.Motion.Position+up*1.7f);beam.SetPosition(1,d.Position+up*.9f);
                    Ring(d.Position+Vector3.up*.12f,.38f,new Color(.95f,.86f,.3f),.03f);
                }
                foreach(var c in b.PillarCommands)
                {
                    if(c.Resolved)continue;
                    for(int j=1;j<=2;j++)Ring(c.Pillar.Settings.Position+c.Direction*j+Vector3.up*.4f,.43f,new Color(1,.23f,.12f,.7f+.25f*Mathf.Sin((float)b.Clock*8)),.04f);
                }
                foreach(var cell in b.SealedTiles)Ring(new Vector3(cell.x,.4f,cell.y),.42f,new Color(.13f,.16f,.12f),.16f);
            }
            for(int i=used;i<lines.Count;i++)lines[i].enabled=false;
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
