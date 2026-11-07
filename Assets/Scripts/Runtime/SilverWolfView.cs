using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // All frames are authored in billboard space around actor or target feet. Attack selection,
    // damage and resource grants belong to ManualBattle; rendering never draws another box.
    internal sealed class SilverWolfView
    {
        readonly Transform parent;
        readonly Material material;
        readonly List<SpriteRenderer> layers=new List<SpriteRenderer>();
        LocalMonsterArt art;
        int used;
        internal int VisibleCount=>used;
        internal double PendingUntil {get;private set;}=double.NegativeInfinity;

        internal SilverWolfView(Transform parent,Material material)
        {
            this.parent=parent;
            this.material=material;
        }

        internal static bool Facing(Vector3 direction,Camera camera,bool previous)
        {
            float horizontal=Vector3.Dot(direction,camera.transform.right);
            return Mathf.Abs(horizontal)>.01f?horizontal<0:previous;
        }

        internal static string ActorClip(WolfBoxKind box)
        {
            switch(box)
            {
                case WolfBoxKind.Sword:return "extra_sword";
                case WolfBoxKind.Bomb:return "extra_bomb";
                case WolfBoxKind.Bean:return "extra_bean";
                default:throw new ArgumentOutOfRangeException(nameof(box));
            }
        }

        internal static string ResultName(WolfBoxKind box)
        {
            switch(box)
            {
                case WolfBoxKind.Sword:return "超超超大剑";
                case WolfBoxKind.Bomb:return "爆爆爆炸弹";
                case WolfBoxKind.Bean:return "怪怪怪味豆";
                default:return string.Empty;
            }
        }

        internal void Begin(){used=0;PendingUntil=double.NegativeInfinity;}

        internal void Present(CombatShot shot,ManualBattle battle,Camera camera,double now)
        {
            if(shot.Erased || shot.Resolved&&!shot.Applied)return;
            if(art==null)art=LocalMonsterArt.For(SilverWolfCombat.CardId);
            bool punch=shot.WolfBox==WolfBoxKind.None;
            var result=shot.WolfBox;
            string key=punch?"extra_fx_punch":result==WolfBoxKind.Sword?"extra_fx_sword":result==WolfBoxKind.Bomb?"extra_fx_bomb":"extra_fx_bean";
            var targetClip=art[key];
            double age=(now-shot.WolfActionAt)*shot.WolfActionRate;
            if(age<0 || age>=targetClip.Duration)return;
            PendingUntil=Math.Max(PendingUntil,shot.WolfActionAt+targetClip.Duration/shot.WolfActionRate);
            var defender=battle.FindDefender(shot.TargetId);
            Vector3 destination=defender?.Position??shot.Destination;
            bool flip=Facing(shot.Destination-shot.Origin,camera,false);
            // The sword afterstroke attaches to the highest-health victim selected by the model.
            if(result==WolfBoxKind.Sword && age>=SilverWolfCombat.SwordFinish && shot.WolfBonusTargetId!=null)
                destination=battle.FindDefender(shot.WolfBonusTargetId)?.Position??destination;
            Show(key,targetClip,age,destination,camera,flip,40);
            if(punch)return;

            string projectile;
            float travel;
            switch(result)
            {
                case WolfBoxKind.Sword:
                    projectile="extra_proj_sword";
                    travel=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.46f,1.76f,(float)age));
                    break;
                case WolfBoxKind.Bomb:
                    projectile="extra_proj_bomb";
                    travel=Mathf.InverseLerp(1.92f,2.54f,(float)age);
                    break;
                default:
                    projectile="extra_proj_bean";
                    travel=Mathf.InverseLerp(1.64f,2.29f,(float)age);
                    break;
            }
            Show(projectile,art[projectile],age,Vector3.Lerp(shot.Origin,destination,travel),camera,flip,41);
        }

        void Show(string key,LocalMonsterArt.Clip clip,double age,Vector3 position,Camera camera,bool flip,int order)
        {
            if(used==layers.Count)
            {
                var r=new GameObject("Silver Wolf effect",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                r.transform.SetParent(parent,false);r.sharedMaterial=material;layers.Add(r);
            }
            var layer=layers[used++];
            if(layer.sprite==null || layer.sprite.name.IndexOf(key,StringComparison.Ordinal)<0)layer.name="Silver Wolf / "+key;
            layer.sprite=clip.At(age,false,out _);
            layer.enabled=true;layer.flipX=flip;
            layer.transform.SetPositionAndRotation(position,camera.transform.rotation);
            layer.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,position)+order;
        }

        internal void End()
        {
            for(int i=used;i<layers.Count;i++)layers[i].enabled=false;
        }

        internal void Clear()
        {
            foreach(var r in layers){r.enabled=false;UnityEngine.Object.Destroy(r.gameObject);}
            layers.Clear();Begin();
        }

        // Single-character trial uses actual defending entities with slug art and close-range contact.
        internal static void PresentSlug(SpriteRenderer renderer,ManualBattle battle,DefenderState defender,Camera camera,double time,float idleLead)
        {
            var art=OriginalSlugArt.For(OriginalSlugArt.CardId);
            string key="idle";
            double age=time+idleLead;
            if(!defender.Alive)
            {
                key="death";age=time-defender.DeathTime;
                if(age>=art[key].Duration){renderer.enabled=false;return;}
            }
            else
            {
                double swing=(battle.Clock-defender.LastActionTime)*defender.LastActionRate;
                if(swing>=0 && swing<art["attack"].Duration)
                {
                    key="attack";age=swing;
                }
                var target=battle.FindUnit(defender.TargetId);
                var direction=(target?.Motion.Position??battle.Entry)-defender.Position;
                renderer.flipX=Facing(direction,camera,renderer.flipX);
            }
            renderer.enabled=true;
            renderer.sprite=art[key].At(age,key=="idle",out _);
            renderer.transform.SetPositionAndRotation(defender.Position,camera.transform.rotation);
            renderer.color=time>=defender.LastHitTime && time<defender.LastHitTime+.14?new Color(1,.5f,.45f):Color.white;
            renderer.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,defender.Position)+30;
        }
    }
}
