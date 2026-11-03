using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class PatriotEffectView:MonoBehaviour
    {
        Func<ManualBattle> battle;
        Material material;
        readonly List<LineRenderer> rings=new List<LineRenderer>();
        public void Bind(Func<ManualBattle> getter){battle=getter;material=new Material(Shader.Find("Sprites/Default"));}
        void LateUpdate()
        {
            var b=battle?.Invoke();int used=0;
            if(b!=null)foreach(var u in b.Units)
            {
                if(!u.Active||!b.IsPatriot(u))continue;
                bool pulse=b.Clock<u.LastImmolationAt+.3,shield=u.PatriotPhase==PatriotPhase.March,invulnerable=b.Clock<u.PatriotInvulnerableUntil;
                if(!pulse&&!shield&&!invulnerable)continue;
                if(used==rings.Count){var l=new GameObject("Patriot ability feedback").AddComponent<LineRenderer>();l.transform.SetParent(transform,false);l.sharedMaterial=material;l.positionCount=49;l.widthMultiplier=.035f;l.sortingOrder=16000;rings.Add(l);}
                var ring=rings[used++];ring.enabled=true;ring.startColor=ring.endColor=pulse?new Color(1,.24f,.05f):invulnerable?Color.white:new Color(.9f,.65f,.22f);
                float radius=pulse?1.75f:.7f;for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;ring.SetPosition(i,u.Motion.Position+new Vector3(Mathf.Cos(a)*radius,.08f,Mathf.Sin(a)*radius));}
            }
            for(int i=used;i<rings.Count;i++)rings[i].enabled=false;
        }
        void OnDestroy(){foreach(var l in rings)if(l!=null)Destroy(l.gameObject);if(material!=null)Destroy(material);}
    }
}
