using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class DefenderRosterView : MonoBehaviour
    {
        private sealed class View
        {
            public string Id;public DefenderArt Art,Rear;public SpriteRenderer Renderer,RebirthTail;public LineRenderer Skill;
            public bool Back,Down,SkillDown,Initialized,FirstAction,SkillAction,RebirthFlip;
            public int ActionVariant=-1;public double Action=double.NegativeInfinity,SkillStart=double.NegativeInfinity,RebirthAt=double.NegativeInfinity;
            public double ActionDuration,ContactAt;public float ActionRate=1;
        }
        private readonly List<View> visuals=new List<View>();
        private ManualBattleController owner;
        private Material material;
        private Material skillMaterial;
        public int Count => visuals.Count;
        public SpriteRenderer FindVisual(string id) => visuals.Find(v=>v.Id==id)?.Renderer;
        public bool FacingBack(string id)=>visuals.Find(v=>v.Id==id)?.Back??false;
        public bool FacingDown(string id)=>visuals.Find(v=>v.Id==id)?.Down??false;
        public void Initialize(ManualBattleController controller)
        {
            owner=controller; material=new Material(owner.TacticalMapActive?Resources.Load<Shader>("TacticalActor"):Shader.Find("Sprites/Default"));
            foreach(var d in owner.Battle.Defenders)
            {
                var r=new GameObject(d.Settings.Name+" / "+d.EntityId,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                r.transform.SetParent(transform,false); r.sharedMaterial=material;
                if(owner.TacticalMapActive)ActorGrounding.Attach(r);
                LineRenderer ring=null;
                if(d.Settings.Skill.Enabled&&!DefenderVfxArt.SupportsArtId(d.Settings.ArtId))
                {
                    if(skillMaterial==null)skillMaterial=new Material(Shader.Find("Sprites/Default"));
                    ring=new GameObject("Defender skill feedback",typeof(LineRenderer)).GetComponent<LineRenderer>();ring.transform.SetParent(transform,false);
                    ring.sharedMaterial=skillMaterial;ring.useWorldSpace=true;ring.loop=true;ring.positionCount=40;ring.widthMultiplier=.035f;
                }
                SpriteRenderer tail=null;
                if(d.Settings.Skill.Kind==DefenderSkillKind.Mon3trMeltdown)
                {
                    tail=new GameObject("Mon3tr original rebirth death tail",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                    tail.transform.SetParent(transform,false);tail.sharedMaterial=material;tail.enabled=false;
                    if(owner.TacticalMapActive)ActorGrounding.Attach(tail);
                }
                visuals.Add(new View { Id=d.EntityId,Art=DefenderArt.Load(d.Settings.ArtId),Rear=owner.TacticalMapActive?DefenderArt.Load(d.Settings.ArtId+"B"):null,Renderer=r,Skill=ring,RebirthTail=tail });
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
                var d=b.FindDefender(v.Id); if(d==null) { v.Renderer.enabled=false;if(v.RebirthTail!=null)v.RebirthTail.enabled=false;continue; }
                var r=v.Renderer;if(d.Reserve){r.enabled=false;if(v.RebirthTail!=null)v.RebirthTail.enabled=false;continue;} double age=clock-d.LastActionTime;
                if(v.Action!=d.LastActionTime&&age>=0)CaptureAction(v,d,b);
                bool action=age>=0&&age<v.ActionDuration;
                if(d.Alive && d.FrozenUntil<=b.Clock)
                {
                    if(!action)
                    {
                        var targetUnit=b.FindUnit(d.SeventhMoving?d.SeventhInterceptTarget:d.TargetId);var targetAlly=b.FindDefender(d.TargetId);
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
                if(d.Settings.Skill.Kind==DefenderSkillKind.ChenDawnstreak&&d.SkillCasts>0&&
                    (b.DefenderSkillActive(d)||b.Clock-d.SkillEndedAt<v.Art.SkillEndDuration))
                    Face(v,d.Position,d.Position+b.ChenDawnFacing(d));
                if(v.SkillStart!=d.SkillStartedAt){v.SkillStart=d.SkillStartedAt;v.SkillDown=v.Down;}
                var art=v.Back && v.Rear!=null?v.Rear:v.Art;
                if(d.Settings.Skill.Kind==DefenderSkillKind.Mon3trMeltdown&&b.DefenderSkillActive(d))art=v.Art;
                bool skillActive=b.DefenderSkillActive(d);double skillAge=b.Clock-d.SkillStartedAt,startup=System.Math.Max(0,d.SkillActionUntil-d.SkillStartedAt);
                bool skillDown=d.Settings.Skill.Kind==DefenderSkillKind.ExusiaiDelivery?v.SkillDown:v.Down;
                bool flyingDeath=d.Settings.Skill.Kind==DefenderSkillKind.AngelinaWishes&&d.DeathTime>=d.SkillStartedAt&&d.DeathTime<d.SkillActiveUntil;
                float deathDuration=flyingDeath?v.Art.DurationOf("skill_death"):v.Art["death"].Duration;
                PresentRebirthTail(v,d,camera,clock);
                r.enabled=d.Alive || clock-d.DeathTime<deathDuration;
                if(v.Skill!=null&&!d.Alive)v.Skill.enabled=false;
                if(!r.enabled) continue;
                if(!d.Alive) r.sprite=flyingDeath?v.Art.PhaseAt("skill_death",clock-d.DeathTime):v.Art["death"].At(clock-d.DeathTime,false);
                else if(d.FrozenUntil>b.Clock) { if(r.sprite==null) r.sprite=art["idle"].At(0,true); }
                // Existing campaign tiers can override startup. Fit the full source delivery
                // gesture to that actual lock; the default 1.6-second trial keeps source speed.
                else if(skillActive&&d.Settings.Skill.Kind==DefenderSkillKind.ExusiaiDelivery&&skillAge<startup&&art.DurationOf("skill_cast")>0)r.sprite=art.PhaseAt("skill_cast",skillAge*art.DurationOf("skill_cast")/startup);
                else if(skillActive&&d.Settings.Skill.Kind==DefenderSkillKind.AngelinaWishes&&d.SeventhMoving)r.sprite=art.PhaseAt("skill_move",b.Clock-d.SeventhMoveStartedAt,true)??art.IdleAt(skillAge);
                else if(skillActive&&d.Settings.Skill.Kind==DefenderSkillKind.AngelinaWishes&&b.Clock-d.SeventhMoveEndedAt<art.DurationOf("skill_restart"))r.sprite=art.PhaseAt("skill_restart",b.Clock-d.SeventhMoveEndedAt);
                else if(skillActive&&d.Settings.Skill.Kind==DefenderSkillKind.Mon3trMeltdown&&skillAge<System.Math.Max(0,d.SkillActionUntil-d.SkillStartedAt)&&art.DurationOf("skill_restart")>0)
                    r.sprite=art.PhaseAt("skill_restart",skillAge*art.DurationOf("skill_restart")/System.Math.Max(.001,d.SkillActionUntil-d.SkillStartedAt));
                else if(b.IsAdvancedDefender(d)&&(skillActive||b.IsFifthActive(d)||b.Clock-d.SkillEndedAt<art.SkillEndDuration))r.sprite=art.AdvancedPose(skillAge,(d.Settings.Skill.Kind==DefenderSkillKind.HoshigumaInferno?System.Math.Max(d.SkillActiveUntil,d.ProtectionUntil):d.SkillActiveUntil)-b.Clock,b.Clock-d.SkillEndedAt,action&&!(skillActive&&d.Settings.Skill.Kind==DefenderSkillKind.SariaCalcification),age,v.ActionDuration,v.ActionVariant,skillDown,v.ContactAt);
                else if(action) r.sprite=art.ActionPose(age,v.ActionDuration,v.ContactAt,v.ActionVariant,v.Down,d.LastActionHealing,v.FirstAction);
                else if(b.DefenderSkillActive(d)&&d.Settings.Skill.Kind==DefenderSkillKind.BeagleGuard)r.sprite=art.SkillIdleAt(b.Clock-d.SkillStartedAt);
                else r.sprite=art.StartAt(owner.DefenderIdleClock,v.ActionVariant%4>=2)??art.IdleAt(owner.DefenderIdleClock,System.Math.Max(0,v.ActionVariant));
                r.transform.position=d.Position; r.transform.rotation=camera.transform.rotation;
                r.transform.localScale=Vector3.one*(owner.BossArenaActive&&d.EntityId==BossArenaRules.BossId?owner.ArenaVisualScale:1);
                r.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,d.Position)+30;
                r.color=b.Clock<d.LastHitTime+.12 ? new Color(1,.65f,.65f) :
                    d.ColdUntil>b.Clock || d.FrozenUntil>b.Clock ? new Color(.5f,.8f,1) : Color.white;
                if(v.Skill!=null)
                {
                    bool healing=d.Settings.Skill.Kind==DefenderSkillKind.CardiganRecovery||d.Settings.Skill.Kind==DefenderSkillKind.AnselRange||d.Settings.Skill.Kind==DefenderSkillKind.SpotHealingMode||d.Settings.Skill.Kind==DefenderSkillKind.HibiscusHealing;
                    Color color=healing?new Color(.3f,1,.6f):d.Settings.Skill.Kind==DefenderSkillKind.MidnightEnchant?new Color(.65f,.5f,1):new Color(1,.8f,.2f);
                    v.Skill.enabled=!DefenderVfxArt.SupportsArtId(d.Settings.ArtId)&&d.Alive&&(b.DefenderSkillActive(d)||b.Clock-d.SkillStartedAt<.7);
                    v.Skill.startColor=v.Skill.endColor=color;v.Skill.sortingOrder=r.sortingOrder+1;
                    for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40;v.Skill.SetPosition(i,d.Position+new Vector3(Mathf.Cos(a)*.48f,.035f,Mathf.Sin(a)*.48f));}
                    if(b.DefenderSkillActive(d)&&!DefenderVfxArt.SupportsArtId(d.Settings.ArtId))r.color=Color.Lerp(r.color,color,.12f);
                }
            }
        }
        static void CaptureAction(View view,DefenderState defender,ManualBattle battle)
        {
            CombatShot first=null;
            foreach(var shot in battle.CombatShots)
                if(shot.FromDefender&&!shot.Drone&&!shot.SeventhSecondary&&shot.SourceId==defender.EntityId&&System.Math.Abs(shot.DefenderActionStartedAt-defender.LastActionTime)<.00001&&
                    (first==null||shot.StartedAt<first.StartedAt))first=shot;
            view.FirstAction=double.IsNegativeInfinity(view.Action)||defender.LastActionTime>view.Action+view.ActionDuration+.05||view.SkillAction!=defender.LastAttackSkill;
            view.SkillAction=defender.LastAttackSkill;view.Action=defender.LastActionTime;view.ActionVariant++;
            view.ActionRate=first!=null?Mathf.Max(.0001f,first.DefenderActionRate):Mathf.Max(.0001f,defender.LastActionRate);
            // Mon3tr's transformed claw is a different native clip from its
            // 2.833-second healing gesture. Do not stretch its recovery to that gesture.
            bool mon3trClaw=defender.Settings.Skill.Kind==DefenderSkillKind.Mon3trMeltdown&&first!=null&&
                first.DefenderSkillWasActive==true&&!first.Healing;
            view.ActionDuration=view.Art.ActionDurationFor(mon3trClaw,defender.Settings.ActionDuration,view.ActionRate);
            view.ContactAt=first!=null?first.StartedAt-view.Action:defender.Settings.Windup/view.ActionRate;
            if(first==null&&defender.Settings.Skill.Kind==DefenderSkillKind.KaltsitEsperanta&&battle.DefenderSkillActive(defender))
                foreach(var evt in battle.SeventhDefenderEvents)
                    if(evt.SourceId==defender.EntityId&&evt.Kind==SeventhDefenderEventKind.HealingPulse&&evt.At>=view.Action&&evt.At<view.Action+view.ActionDuration)
                    {view.ContactAt=evt.At-view.Action;break;}
            if(first!=null)Face(view,first.Origin,first.Destination);
            else
            {
                var unit=battle.FindUnit(defender.TargetId);var ally=battle.FindDefender(defender.TargetId);
                if(unit!=null)Face(view,defender.Position,unit.Motion.Position);else if(ally!=null)Face(view,defender.Position,ally.Position);
            }
            view.Initialized=true;
        }
        void PresentRebirthTail(View view,DefenderState defender,Camera camera,double clock)
        {
            if(view.RebirthTail==null)return;
            double age=clock-defender.LastMon3trRebirthDeathAt;
            var sprite=view.Art.PhaseAt("skill_rebirth_end",age);var tail=view.RebirthTail;
            tail.enabled=sprite!=null;if(!tail.enabled)return;
            if(view.RebirthAt!=defender.LastMon3trRebirthDeathAt){view.RebirthAt=defender.LastMon3trRebirthDeathAt;view.RebirthFlip=view.Renderer.flipX;}
            tail.sprite=sprite;tail.flipX=view.RebirthFlip;tail.color=Color.white;
            tail.transform.position=defender.LastMon3trRebirthDeathPosition;tail.transform.rotation=camera.transform.rotation;tail.transform.localScale=Vector3.one;
            tail.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,defender.LastMon3trRebirthDeathPosition)+30;
        }
        private static void Face(View view,Vector3 origin,Vector3 target)
        {
            var delta=target-origin;
            // Source DOWN is the unit's cardinal facing; platform height is unrelated.
            if(delta.x*delta.x+delta.z*delta.z>.0064f)view.Down=delta.z<-.18f&&Mathf.Abs(delta.z)>=Mathf.Abs(delta.x);
            if(delta.z>.18f)view.Back=true;else if(delta.z<-.18f)view.Back=false;
            // Original operator front and rear sheets are authored facing right.
            if(Mathf.Abs(delta.x)>.08f)view.Renderer.flipX=delta.x<0;
        }
        private void OnDestroy() { if(material!=null) Destroy(material);if(skillMaterial!=null)Destroy(skillMaterial); }
    }
}
