using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class DefenderRosterView : MonoBehaviour
    {
        private sealed class View { public string Id; public DefenderArt Art,Rear; public SpriteRenderer Renderer; public bool Back,Initialized; public double Action=double.NegativeInfinity; }
        private readonly List<View> visuals=new List<View>();
        private ManualBattleController owner;
        private Material material;
        public int Count => visuals.Count;
        public SpriteRenderer FindVisual(string id) => visuals.Find(v=>v.Id==id)?.Renderer;
        public bool FacingBack(string id)=>visuals.Find(v=>v.Id==id)?.Back??false;
        public void Initialize(ManualBattleController controller)
        {
            owner=controller; material=new Material(owner.TacticalMapActive?Resources.Load<Shader>("TacticalActor"):Shader.Find("Sprites/Default"));
            foreach(var d in owner.Battle.Defenders)
            {
                var r=new GameObject(d.Settings.Name+" / "+d.EntityId,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                r.transform.SetParent(transform,false); r.sharedMaterial=material;
                visuals.Add(new View { Id=d.EntityId,Art=DefenderArt.Load(d.Settings.ArtId),Rear=owner.TacticalMapActive?DefenderArt.Load(d.Settings.ArtId+"B"):null,Renderer=r });
            }
            Present();
        }
        private void LateUpdate() => Present();
        private void Present()
        {
            if(owner==null || owner.Battle==null || !owner.DefenderTrialActive) return;
            var b=owner.Battle; var camera=Camera.main; double clock=owner.PresentationClock;
            foreach(var v in visuals)
            {
                var d=b.FindDefender(v.Id); if(d==null) { v.Renderer.enabled=false; continue; }
                var r=v.Renderer; double age=clock-d.LastActionTime;
                bool action=age>=0 && age<d.Settings.ActionDuration;
                if(d.Alive && d.FrozenUntil<=b.Clock)
                {
                    if(action && v.Action!=d.LastActionTime)
                    {
                        // Retain the launched attack's target point until its full action completes.
                        for(int i=b.CombatShots.Count-1;i>=0;i--)
                        {var shot=b.CombatShots[i];if(shot.FromDefender && shot.SourceId==d.EntityId && shot.StartedAt>=d.LastActionTime){Face(v,d.Position,shot.Destination);break;}}
                        v.Action=d.LastActionTime;v.Initialized=true;
                    }
                    else if(!action)
                    {
                        var targetUnit=b.FindUnit(d.TargetId);var targetAlly=b.FindDefender(d.TargetId);
                        if(targetUnit!=null)Face(v,d.Position,targetUnit.Motion.Position);
                        else if(targetAlly!=null)Face(v,d.Position,targetAlly.Position);
                        else if(!v.Initialized)
                        {
                            var nearest=b.Lanes[0].Entry;
                            foreach(var lane in b.Lanes)if((lane.Entry-d.Position).sqrMagnitude<(nearest-d.Position).sqrMagnitude)nearest=lane.Entry;
                            Face(v,d.Position,nearest);
                        }
                        v.Initialized=true;
                    }
                }
                var art=v.Back && v.Rear!=null?v.Rear:v.Art;
                double actionAge=v.Back && v.Rear!=null && d.Settings.ActionDuration>0 ? age*art.ActionDuration/d.Settings.ActionDuration : age;
                r.enabled=d.Alive || clock-d.DeathTime<v.Art["death"].Duration;
                if(!r.enabled) continue;
                if(!d.Alive) r.sprite=v.Art["death"].At(clock-d.DeathTime,false);
                else if(d.FrozenUntil>b.Clock) { if(r.sprite==null) r.sprite=art["idle"].At(0,true); }
                else if(action) r.sprite=d.LastActionHealing ? art.HealAt(actionAge) : art.ActionAt(actionAge);
                else r.sprite=art["idle"].At(owner.DefenderIdleClock,true);
                r.transform.position=d.Position; r.transform.rotation=camera.transform.rotation;
                r.transform.localScale=Vector3.one;
                r.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,d.Position)+30;
                r.color=b.Clock<d.LastHitTime+.12 ? new Color(1,.65f,.65f) :
                    d.ColdUntil>b.Clock || d.FrozenUntil>b.Clock ? new Color(.5f,.8f,1) : Color.white;
            }
        }
        private static void Face(View view,Vector3 origin,Vector3 target)
        {
            var delta=target-origin;
            if(delta.z>.18f)view.Back=true;else if(delta.z<-.18f)view.Back=false;
            // Original operator front and rear sheets are authored facing right.
            if(Mathf.Abs(delta.x)>.08f)view.Renderer.flipX=delta.x<0;
        }
        private void OnDestroy() { if(material!=null) Destroy(material); }
    }
}
