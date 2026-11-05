using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Approved flipbooks follow actual cast/impact events; no damage or timers are owned here.</summary>
    public sealed class FrostSealView:MonoBehaviour
    {
        Func<ManualBattle> source;
        ManualBattle observed;
        int run=-1,used;
        Material material;
        readonly List<SpriteRenderer> sprites=new List<SpriteRenderer>();
        FrostSealScreenEffect screen;
        public int VisibleEffects=>used;
        public void Bind(Func<ManualBattle> battle)=>source=battle;
        void Draw(string key,double age,Vector3 position,Camera camera,int order,bool flip=false)
        {
            var frame=FrostSealArt.Load(key).At(age);if(frame==null)return;
            if(used==sprites.Count)
            {
                var r=new GameObject("FrostSeal / "+key,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                r.transform.SetParent(transform,false);
                if(material==null)material=new Material(Shader.Find("Sprites/Default"));r.sharedMaterial=material;sprites.Add(r);
            }
            var renderer=sprites[used++];renderer.sprite=frame;renderer.enabled=true;renderer.flipX=flip;
            renderer.transform.localScale=Vector3.one*(key=="ice"?.82f:1);
            renderer.transform.SetPositionAndRotation(position,camera.transform.rotation);
            renderer.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,position)+order;
        }
        void LateUpdate()
        {
            var battle=source?.Invoke();var camera=Camera.main;used=0;
            if(observed!=battle||run!=(battle?.RunId??-1))
            {
                foreach(var r in sprites)Destroy(r.gameObject);sprites.Clear();observed=battle;run=battle?.RunId??-1;
            }
            float pulseAge=-1;
            if(battle!=null&&camera!=null)
            {
                if(screen==null||screen.gameObject!=camera.gameObject)
                {if(screen!=null)Destroy(screen);screen=camera.gameObject.AddComponent<FrostSealScreenEffect>();}
                foreach(var pulse in battle.SnowstormPulses)
                {
                    double age=battle.Clock-pulse.At;
                    if(age<0||age>=1.4)continue;
                    foreach(string id in pulse.Targets)
                    {var d=battle.FindDefender(id);if(d!=null)Draw("storm",age,d.Position+camera.transform.up*.8f,camera,30);}
                }
                foreach(var cast in battle.FrostSeals)
                {
                    if(!cast.Resolved||cast.Cancelled)continue;
                    double age=battle.Clock-cast.ImpactAt;
                    if(age<0||age>=5.3)continue;
                    Draw("storm",age,cast.Position,camera,34);
                    for(int i=0;i<cast.FrozenTargets.Count;i++)
                    {
                        var target=battle.FindDefender(cast.FrozenTargets[i]);
                        if(target!=null)Draw("ice",age,target.Position,camera,38,i%2==1);
                    }
                    if(age<.35)pulseAge=(float)age;
                }
            }
            for(int i=used;i<sprites.Count;i++)sprites[i].enabled=false;
            if(screen!=null)screen.Present(pulseAge);
        }
        void OnDestroy(){if(material!=null)Destroy(material);if(screen!=null)Destroy(screen);}
    }
}
