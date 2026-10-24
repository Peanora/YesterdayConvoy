using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class NeuralEffectView:MonoBehaviour
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
                var line=new GameObject("Neural injury effect").AddComponent<LineRenderer>();line.transform.SetParent(transform,false);
                if(material==null)material=new Material(Shader.Find("Sprites/Default"));line.sharedMaterial=material;lines.Add(line);
            }
            var l=lines[used++];l.enabled=true;l.sortingOrder=19010;l.widthMultiplier=width;l.startColor=l.endColor=color;l.positionCount=count;return l;
        }
        void LateUpdate()
        {
            used=0;var b=source?.Invoke();
            if(b!=null)
            {
                foreach(var pulse in b.CreeperPulses)
                {
                    float age=(float)(b.Clock-pulse.Time);if(age<0||age>.5f)continue;
                    float distance=.3f+Mathf.Min(1,age/.25f)*1.5f;
                    for(int k=0;k<16;k++)
                    {
                        float a=k*Mathf.PI/8;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                        var spike=Line(.045f,new Color(.72f,.96f,.74f,1-age/.5f),3);
                        spike.SetPosition(0,pulse.Position+dir*distance+Vector3.up*.25f);
                        spike.SetPosition(1,pulse.Position+dir*(distance+.3f)+Vector3.up*.55f);
                        spike.SetPosition(2,pulse.Position+dir*(distance+.36f)+Vector3.up*.2f);
                    }
                }
                if(b.EngagementEnabled&&b.CombatSettings.Effects.Harvester)
                    foreach(var u in b.Units)
                    {
                        if(!u.Active||u.HarvesterPhase!=HarvesterPhase.Combat)continue;
                        bool pulse=b.Clock-u.LastHarvesterAuraAt<.3;
                        var aura=Line(pulse?.035f:.012f,new Color(.25f,.9f,.7f,pulse?.65f:.25f),49);
                        for(int k=0;k<=48;k++){float a=k*Mathf.PI/24;aura.SetPosition(k,u.Motion.Position+new Vector3(Mathf.Cos(a)*2.5f,.15f,Mathf.Sin(a)*2.5f));}
                    }
                foreach(var bubble in b.CorrosiveBubbles)
                {
                    float age=(float)(b.Clock-bubble.StartedAt);if(age<0||age>5.5f)continue;
                    var lift=Camera.main!=null?Camera.main.transform.up:Vector3.up;
                    var center=Vector3.Lerp(bubble.Origin,bubble.Anchor,Mathf.Clamp01(age/3))+lift*2.35f;
                    var orb=Line(.055f,new Color(.6f,.85f,1,.85f),33);
                    var up=Camera.main!=null?Camera.main.transform.up:Vector3.up;
                    var right=Camera.main!=null?Camera.main.transform.right:Vector3.right;
                    for(int k=0;k<=32;k++){float a=k*Mathf.PI/16;orb.SetPosition(k,center+(up*Mathf.Sin(a)+right*Mathf.Cos(a))*(.3f+.05f*Mathf.Sin(age*8)));}
                    foreach(var offset in new[]{Vector3.zero,Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
                    {
                        var tile=Line(.025f,new Color(.45f,.7f,1,.4f),5);var pos=bubble.Anchor+offset+Vector3.up*.14f;
                        tile.SetPosition(0,pos+new Vector3(-.45f,0,-.45f));tile.SetPosition(1,pos+new Vector3(-.45f,0,.45f));
                        tile.SetPosition(2,pos+new Vector3(.45f,0,.45f));tile.SetPosition(3,pos+new Vector3(.45f,0,-.45f));tile.SetPosition(4,tile.GetPosition(0));
                    }
                    var target=b.FindDefender(bubble.LastTargetId);
                    if(target!=null&&b.Clock-bubble.LastPulseAt<.25)
                    {var beam=Line(.07f,new Color(.85f,.65f,1),2);beam.SetPosition(0,center);beam.SetPosition(1,target.Position+Vector3.up*.7f);}
                }
                foreach(var d in b.Defenders)
                {
                    if(!d.Alive||d.NeuralUntil<=b.Clock)continue;
                    Vector3 up=Camera.main!=null?Camera.main.transform.up:Vector3.up;
                    Vector3 right=Camera.main!=null?Camera.main.transform.right:Vector3.right;
                    var halo=Line(.04f,new Color(1,.86f,.25f),33);
                    for(int k=0;k<=32;k++)
                    {float a=k*Mathf.PI/16+(float)b.Clock*2;halo.SetPosition(k,d.Position+up*1.55f+right*(Mathf.Cos(a)*.28f)+Vector3.forward*(Mathf.Sin(a)*.18f));}
                }
            }
            for(int i=used;i<lines.Count;i++)lines[i].enabled=false;
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
