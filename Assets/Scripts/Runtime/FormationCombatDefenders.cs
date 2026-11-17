using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        void TickCampaignDefender(FormationDefender d)
        {
            var s=State(d);if(Buff(d,"stun")>0||Buff(d,"freeze")>0||Clock<d.ActionUntil||Clock<d.NextAttackAt)return;
            var candidates=Units.Concat(Summons).Where(u=>u.Enabled&&u.Alive&&HasEnteredBattleArea(u)&&(u.Position-d.Position).sqrMagnitude<=d.Range*d.Range&&!(d.Range<2&&State(u).RidingCarrier!=0)).ToArray();
            var target=candidates.Where(u=>Buff(u,"taunt")>0).OrderBy(u=>(u.Position-d.Position).sqrMagnitude).FirstOrDefault()??candidates.FirstOrDefault(u=>u.Id==d.TargetId)??candidates.OrderBy(u=>(u.Position-d.Position).sqrMagnitude).ThenBy(u=>u.Id).FirstOrDefault();
            bool skill=d.SkillCharge+1e-5>=d.SkillRequired;
            if(d.Healer)
            {
                var wounded=Defenders.Where(o=>o.Alive&&(o.Position-d.Position).sqrMagnitude<=d.Range*d.Range&&o.Health<o.MaxHealth).OrderBy(o=>o.Health/o.MaxHealth).Take(skill?2:1).ToArray();
                if(wounded.Length==0)return;BeginAction(d,skill?s.GuardActionSeconds:.35f,skill);d.NextAttackAt=Clock+d.Interval;if(skill)d.SkillCharge=0;
                foreach(var other in wounded){float raw=d.Attack*(skill?1.6f:1)+other.MaxHealth*(skill?.04f:.02f);other.Health=Mathf.Min(other.MaxHealth,other.Health+raw);d.HealingDone+=raw;}return;
            }
            if(s.Boss&&skill)
            {
                int plane=Campaign.CurrentNode.Plane;
                if(plane==1){d.SkillCharge=0;d.Shield=d.MaxHealth*.1f;s.Shields.Clear();s.Shields.Add(new CombatShield{Amount=d.Shield,Until=Clock+6,Key="boss"});BeginAction(d,s.GuardActionSeconds,true);return;}
                if(plane==2)
                {
                    d.SkillCharge=0;BeginAction(d,s.GuardActionSeconds,true);d.Shield=d.MaxHealth*.05f;s.Shields.Clear();s.Shields.Add(new CombatShield{Amount=d.Shield,Until=Clock+8,Key="boss"});
                    foreach(var ally in Defenders.Where(o=>o.Alive).OrderBy(o=>o.Health/o.MaxHealth).Take(2))ally.Health=Mathf.Min(ally.MaxHealth,ally.Health+ally.MaxHealth*.04f);
                    if(s.Stage>0)foreach(var u in Units.Where(u=>u.Enabled&&u.Alive))GiveBuff(u,"heal_down",.2f,8);return;
                }
                if(target!=null)
                {
                    d.SkillCharge=0;BeginAction(d,s.GuardActionSeconds,true);var centre=target.Position;int strikes=s.Stage>=2?5:3;
                    Schedule(Clock+s.GuardActionSeconds,()=>
                    {
                        if(!d.Alive)return;
                        var victims=Units.Concat(Summons).Where(u=>u.Enabled&&u.Alive&&HasEnteredBattleArea(u)&&Mathf.Abs(u.Position.y-centre.y)<=1).OrderBy(u=>Mathf.Abs(u.Position.x-centre.x)).Take(5).ToArray();
                        for(int i=0;i<strikes;i++)foreach(var u in victims)CombatDamage(d,u,d.Attack*1.6f,0,true,false);
                    });return;
                }
            }
            if(target==null)return;d.TargetId=target.Id;Face(d,target.Position);
            float action=skill?s.GuardActionSeconds:.35f;BeginAction(d,action,skill);
            d.NextAttackAt=Clock+d.Interval/Mathf.Max(.1f,1-(Buff(d,"cold")>0?.2f:0)-Buff(d,"nerve_slow"));
            if(skill)d.SkillCharge=0;
            if(s.Template==2&&skill)
            {d.Shield=d.MaxHealth*.25f;s.Shields.Clear();s.Shields.Add(new CombatShield{Amount=d.Shield,Until=Clock+8,Key="guard"});return;}
            int type=s.Template==4||s.Template==6?1:0;
            int count=skill&&s.Template==1?3:1;
            var targets=candidates.OrderBy(u=>u==target?0:1).ThenBy(u=>(u.Position-target.Position).sqrMagnitude).Take(count).ToArray();
            if(s.Template==4&&skill)targets=candidates.Where(u=>(u.Position-target.Position).sqrMagnitude<=1.8f*1.8f).ToArray();
            if(s.Template==6&&skill){foreach(var u in targets)GiveBuff(u,"slow",.4f,6);}
            int hits=s.Template==3&&skill?3:1;float coefficient=skill?(s.Template==3?.8f:s.Template==1?1.5f:s.Template==4?1.8f:1.6f):1;
            for(int i=0;i<hits;i++)foreach(var u in targets)CombatDamage(d,u,d.Attack*coefficient,type,skill,false);
        }
    }
}
