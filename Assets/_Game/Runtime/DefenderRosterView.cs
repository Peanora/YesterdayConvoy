using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class DefenderRosterView : MonoBehaviour
    {
        private sealed class View { public string Id; public DefenderArt Art,Rear; public SpriteRenderer Renderer; public LineRenderer Skill; public bool Back,Initialized; public double Action=double.NegativeInfinity; }
        private readonly List<View> visuals=new List<View>();
        private ManualBattleController owner;
        private Material material;
        private Material skillMaterial;
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
                LineRenderer ring=null;
                if(d.Settings.Skill.Enabled)
                {
                    if(skillMaterial==null)skillMaterial=new Material(Shader.Find("Sprites/Default"));
                    ring=new GameObject("Defender skill feedback",typeof(LineRenderer)).GetComponent<LineRenderer>();ring.transform.SetParent(transform,false);
                    ring.sharedMaterial=skillMaterial;ring.useWorldSpace=true;ring.loop=true;ring.positionCount=40;ring.widthMultiplier=.035f;
                }
                visuals.Add(new View { Id=d.EntityId,Art=DefenderArt.Load(d.Settings.ArtId),Rear=owner.TacticalMapActive?DefenderArt.Load(d.Settings.ArtId+"B"):null,Renderer=r,Skill=ring });
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
                bool action=age>=0 && age<d.Settings.ActionDuration/d.LastActionRate;
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
                double actionAge=d.Settings.ActionDuration>0&&((v.Back&&v.Rear!=null)||d.Settings.Skill.Enabled) ? age*d.LastActionRate*art.ActionDuration/d.Settings.ActionDuration : age;
                r.enabled=d.Alive || clock-d.DeathTime<v.Art["death"].Duration;
                if(v.Skill!=null&&!d.Alive)v.Skill.enabled=false;
                if(!r.enabled) continue;
                if(!d.Alive) r.sprite=v.Art["death"].At(clock-d.DeathTime,false);
                else if(d.FrozenUntil>b.Clock) { if(r.sprite==null) r.sprite=art["idle"].At(0,true); }
                else if(action) r.sprite=d.LastActionHealing ? art.HealAt(d.Settings.ActionDuration>0?age*d.LastActionRate*art.HealDuration/d.Settings.ActionDuration:age) : art.ActionAt(actionAge);
                else if(b.DefenderSkillActive(d)&&d.Settings.Skill.Kind==DefenderSkillKind.BeagleGuard)r.sprite=art.SkillIdleAt(b.Clock-d.SkillStartedAt);
                else r.sprite=art["idle"].At(owner.DefenderIdleClock,true);
                r.transform.position=d.Position; r.transform.rotation=camera.transform.rotation;
                r.transform.localScale=Vector3.one;
                r.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,d.Position)+30;
                r.color=b.Clock<d.LastHitTime+.12 ? new Color(1,.65f,.65f) :
                    d.ColdUntil>b.Clock || d.FrozenUntil>b.Clock ? new Color(.5f,.8f,1) : Color.white;
                if(v.Skill!=null)
                {
                    bool healing=d.Settings.Skill.Kind==DefenderSkillKind.CardiganRecovery||d.Settings.Skill.Kind==DefenderSkillKind.AnselRange||d.Settings.Skill.Kind==DefenderSkillKind.SpotHealingMode||d.Settings.Skill.Kind==DefenderSkillKind.HibiscusHealing;
                    Color color=healing?new Color(.3f,1,.6f):d.Settings.Skill.Kind==DefenderSkillKind.MidnightEnchant?new Color(.65f,.5f,1):new Color(1,.8f,.2f);
                    v.Skill.enabled=d.Alive&&(b.DefenderSkillActive(d)||b.Clock-d.SkillStartedAt<.7);
                    v.Skill.startColor=v.Skill.endColor=color;v.Skill.sortingOrder=r.sortingOrder+1;
                    for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40;v.Skill.SetPosition(i,d.Position+new Vector3(Mathf.Cos(a)*.48f,.035f,Mathf.Sin(a)*.48f));}
                    if(b.DefenderSkillActive(d))r.color=Color.Lerp(r.color,color,.12f);
                }
            }
        }
        private static void Face(View view,Vector3 origin,Vector3 target)
        {
            var delta=target-origin;
            if(delta.z>.18f)view.Back=true;else if(delta.z<-.18f)view.Back=false;
            // Original operator front and rear sheets are authored facing right.
            if(Mathf.Abs(delta.x)>.08f)view.Renderer.flipX=delta.x<0;
        }
        private void OnDestroy() { if(material!=null) Destroy(material);if(skillMaterial!=null)Destroy(skillMaterial); }
    }
}
