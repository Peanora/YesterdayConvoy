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
            var l=lines[used++];l.enabled=true;l.widthMultiplier=width;l.startColor=l.endColor=color;l.positionCount=count;return l;
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
