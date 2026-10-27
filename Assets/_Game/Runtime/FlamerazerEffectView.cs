using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Project-authored flame ribbons and fuel shockwaves. Reads only the simulation clock.</summary>
    public sealed class FlamerazerEffectView:MonoBehaviour
    {
        Func<ManualBattle> source;
        Material material;
        readonly List<LineRenderer> lines=new List<LineRenderer>();
        int used;
        public void Bind(Func<ManualBattle> battle){source=battle;}
        LineRenderer Line(string label,float width,Color color,int count)
        {
            if(used==lines.Count)
            {
                var l=new GameObject(label).AddComponent<LineRenderer>();l.transform.SetParent(transform,false);
                if(material==null)material=new Material(Shader.Find("Sprites/Default"));
                l.sharedMaterial=material;l.useWorldSpace=true;l.numCapVertices=4;lines.Add(l);
            }
            var line=lines[used++];line.enabled=true;line.widthMultiplier=width;
            line.startColor=line.endColor=color;line.positionCount=count;return line;
        }
        void LateUpdate()
        {
            used=0;var b=source?.Invoke();
            if(b!=null)
            {
                foreach(var cast in b.FlameSprays)
                {
                    if(cast.Ended||b.Clock<cast.StartedAt+.5)continue;
                    var u=b.FindUnit(cast.SourceId);var d=b.FindDefender(cast.TargetId);
                    if(u==null||!u.Active||u.FuelDying||d==null||!d.Alive)continue;
                    // The original actor is billboarded: place the nozzle in that same plane.
                    var camera=Camera.main;Vector3 right=camera!=null?camera.transform.right:Vector3.right;
                    Vector3 up=camera!=null?camera.transform.up:Vector3.up;
                    Vector3 from=u.Motion.Position+right*(cast.FlipX?-.9f:.9f)+up*1.15f,to=d.Position+up*.8f;
                    Vector3 across=Vector3.Cross((to-from).normalized,Vector3.up);
                    for(int ribbon=0;ribbon<5;ribbon++)
                    {
                        var line=Line("Flame spray ribbon",ribbon==2?.11f:.07f,ribbon==2?new Color(1,.88f,.35f,.95f):new Color(1,.26f,.025f,.8f),13);
                        for(int k=0;k<13;k++)
                        {
                            float t=k/12f,wave=Mathf.Sin((float)b.Clock*22-k*.9f+ribbon*1.6f);
                            line.SetPosition(k,Vector3.Lerp(from,to,t)+across*((ribbon-2)*.065f*t+wave*.035f*t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.15f+wave*.035f));
                        }
                    }
                }
                foreach(var blast in b.FuelExplosions)
                {
                    float age=(float)(b.Clock-blast.DetonateAt);if(!blast.Resolved||age<0||age>1.1f)continue;
                    var ring=Line("Fuel explosion shockwave",.13f*(1-age/1.1f)+.01f,new Color(1,.5f,.08f,1-age/1.1f),65);
                    float radius=b.CombatSettings.UnitRange*Mathf.Min(1,age/.4f);
                    for(int k=0;k<=64;k++){float a=k*Mathf.PI/32;ring.SetPosition(k,blast.Position+new Vector3(Mathf.Cos(a)*radius,.5f,Mathf.Sin(a)*radius));}
                }
            }
            for(int i=used;i<lines.Count;i++)lines[i].enabled=false;
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
