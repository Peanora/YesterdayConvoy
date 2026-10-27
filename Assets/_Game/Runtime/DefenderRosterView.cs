using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class DefenderRosterView : MonoBehaviour
    {
        private sealed class View { public string Id; public DefenderArt Art; public SpriteRenderer Renderer; }
        private readonly List<View> visuals=new List<View>();
        private ManualBattleController owner;
        private Material material;
        public int Count => visuals.Count;
        public SpriteRenderer FindVisual(string id) => visuals.Find(v=>v.Id==id)?.Renderer;
        public void Initialize(ManualBattleController controller)
        {
            owner=controller; material=new Material(Shader.Find("Sprites/Default"));
            foreach(var d in owner.Battle.Defenders)
            {
                var r=new GameObject(d.Settings.Name+" / "+d.EntityId,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                r.transform.SetParent(transform,false); r.sharedMaterial=material;
                visuals.Add(new View { Id=d.EntityId,Art=DefenderArt.Load(d.Settings.ArtId),Renderer=r });
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
                r.enabled=d.Alive || clock-d.DeathTime<v.Art["death"].Duration;
                if(!r.enabled) continue;
                if(!d.Alive) r.sprite=v.Art["death"].At(clock-d.DeathTime,false);
                else if(d.FrozenUntil>b.Clock) { if(r.sprite==null) r.sprite=v.Art["idle"].At(0,true); }
                else if(age>=0 && age<d.Settings.ActionDuration) r.sprite=d.LastActionHealing ? v.Art.HealAt(age) : v.Art.ActionAt(age);
                else r.sprite=v.Art["idle"].At(b.Clock,true);
                r.transform.position=d.Position; r.transform.rotation=camera.transform.rotation;
                r.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,d.Position)+30;
                var unit=b.FindUnit(d.TargetId); var ally=b.FindDefender(d.TargetId);
                Vector3? target=unit!=null ? unit.Motion.Position : ally?.Position;
                if(target.HasValue && Mathf.Abs(target.Value.x-d.Position.x)>.01f) r.flipX=target.Value.x>d.Position.x;
                r.color=b.Clock<d.LastHitTime+.12 ? new Color(1,.65f,.65f) :
                    d.ColdUntil>b.Clock || d.FrozenUntil>b.Clock ? new Color(.5f,.8f,1) : Color.white;
            }
        }
        private void OnDestroy() { if(material!=null) Destroy(material); }
    }
}
