using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Read-only visual selection for the seven existing cards. No phase, damage,
    // movement, ammo or SP is created here. Actual combat events own every clip.
    internal static partial class BossCoreArt
    {
        // Implemented by the core mechanism owner in its own partial source.
        // With no implementation the C# compiler removes this optional query.
        static partial void QueryMechanicFrame(ManualBattle b,DeployedUnitState u,ref string raw,ref double age,ref bool loop,ref bool found);
        readonly struct Frame
        {
            internal readonly string Raw;internal readonly double Age;internal readonly bool Loop;
            internal Frame(string raw,double age,bool loop=false){Raw=raw;Age=age;Loop=loop;}
        }
        static Frame F(string raw,double age,bool loop=false)=>new Frame(raw,age,loop);
        static bool Window(double now,double start,double duration)=>!double.IsInfinity(start)&&now>=start&&now<start+duration;
        static float Parameter(ManualBattle b,DeployedUnitState u,string skill,string key,float fallback)=>b.V2Skill(u,skill,key,fallback);
        internal static IEnumerable<ArtSequence> SequencesFor(string cardId)
        {
            if(!BossRosterArt.IsCoreCard(cardId))yield break;
            var art=BossRosterArt.For(cardId);if(art==null)yield break;
            foreach(var clip in art.AllSequences)yield return clip;
            // Both operator views are genuinely used by world-space aim/travel.
            if(cardId==ViviCombat.CardId)
                foreach(var clip in BossRosterArt.For(cardId,"back").AllSequences)yield return clip;
        }
        internal static bool TryFrame(ManualBattle b,DeployedUnitState u,double presentation,
            out BossRosterArt art,out string raw,out double age,out bool loop)
        {
            art=null;raw=null;age=0;loop=false;
            if(b==null||u==null||!BossRosterArt.IsCoreCard(u.CardId))return false;
            art=BossRosterArt.For(u.CardId);
            if(art==null)throw new InvalidDataException("Approved core model is missing: "+u.CardId);
            // Herd reconstruction has its own explicit presentation owner. It is
            // never reclassified here as the character's native second phase.
            if(u.IsReviving&&b.HerdSpiritFor(u.EntityId)!=null)return false;
            double now=u.Resolution==UnitResolution.Dead?presentation:b.Clock;
            Frame frame;
            string mechanicRaw=null;double mechanicAge=0;bool mechanicLoop=false,mechanicFound=false;
            QueryMechanicFrame(b,u,ref mechanicRaw,ref mechanicAge,ref mechanicLoop,ref mechanicFound);
            if(mechanicFound&&u.Active&&!u.IsReviving)frame=F(mechanicRaw,mechanicAge,mechanicLoop);
            else switch(art.Task)
            {
                case "B02": frame=Mephisto(b,u,art,now);break;
                case "B06": frame=Mandragora(b,u,art,now);break;
                case "B16": frame=Talulah(b,u,art,now);break;
                case "B17": frame=Patriot(b,u,art,now);break;
                case "B18": frame=Frost(b,u,art,now);break;
                case "B19": frame=Mudrock(b,u,art,now);break;
                case "B20": frame=Vivi(b,u,art,now);break;
                default:return false;
            }
            if(art.Task=="B20"&&u.Resolution!=UnitResolution.Dead&&RearView(b,u,now))
            {
                var rear=BossRosterArt.For(u.CardId,"back");
                // The original rear set omits death and a few release endings.
                // Those exact motions use the approved front set, not a fake rear clip.
                if(rear.HasClip(frame.Raw)||rear.HasZeroDurationState(frame.Raw))art=rear;
            }
            if(!art.HasClip(frame.Raw)&&!art.HasZeroDurationState(frame.Raw))
                throw new InvalidDataException("Unmapped original core action: "+art.Task+"/"+frame.Raw);
            raw=frame.Raw;age=double.IsInfinity(frame.Age)||double.IsNaN(frame.Age)?0:Math.Max(0,frame.Age);loop=frame.Loop;
            // A returned zero-duration source name means transparent native wait.
            // The caller must hide it, not request a frame or substitute an idle.
            return true;
        }
        static Frame Motion(ManualBattle b,DeployedUnitState u,BossRosterArt art,double now,string idle,string move,string attack)
        {
            double rate=Math.Max(.01,u.LastAttackRate),elapsed=now-u.LastAttackTime;
            if(attack!=null&&Window(now,u.LastAttackTime,b.UnitActionDuration(u)/rate))return F(attack,elapsed*rate);
            if(u.IsMoving&&move!=null)return F(move,u.Motion.DistanceTravelled/Math.Max(.01,art.ReferenceSpeed),true);
            return F(idle,now-u.SpawnTime,true);
        }
        static Frame Mephisto(ManualBattle b,DeployedUnitState u,BossRosterArt art,double now)
        {
            if(u.Resolution==UnitResolution.Dead)return F("Die",now-u.DeathTime);
            if(Window(now,u.HealingStartedAt,b.UnitCombat(u).AttackDuration))return F("Attack",now-u.HealingStartedAt);
            return Motion(b,u,art,now,"Idle","Move",null);
        }
        static Frame Mandragora(ManualBattle b,DeployedUnitState u,BossRosterArt art,double now)
        {
            bool second=u.MandraPhase==MandragoraPhase.Second;
            if(u.Resolution==UnitResolution.Dead)return F(u.StoneShield?"C2_Die_1":"C2_Die_2",now-u.DeathTime);
            if(u.MandraPhase==MandragoraPhase.Reborn)
            {
                double total=Parameter(b,u,"secondPhase","delay",5);
                return F("Reborn",(now-u.MandraPhaseStartedAt)/Math.Max(.001,total)*art["Reborn"].Duration);
            }
            string prefix=second?"C2":"C1";
            if(second&&u.PillarStunnedUntil>now)return F("C2_Stun",now-u.StoneShieldBrokenAt);
            var gaze=u.Gaze;
            if(gaze!=null&&now>=gaze.StartedAt)
            {
                if(!gaze.Ended)
                {
                    double elapsed=now-gaze.StartedAt,begin=art[prefix+"_Skill_1_Begin"].Duration;
                    return elapsed<begin?F(prefix+"_Skill_1_Begin",elapsed):F(prefix+"_Skill_1_Loop",elapsed-begin,true);
                }
                if(Window(now,gaze.EndedAt,art[prefix+"_Skill_1_End"].Duration))return F(prefix+"_Skill_1_End",now-gaze.EndedAt);
            }
            if(second&&Window(now,u.PillarCommandStartedAt,Parameter(b,u,"pillar","actionDuration",1.5f)))return F("C2_Skill_3",now-u.PillarCommandStartedAt);
            if(Window(now,u.StoneShieldRestoredAt,Parameter(b,u,"stoneShield","restoreActionDuration",1.5f)))return F(prefix+"_Skill_2",now-u.StoneShieldRestoredAt);
            return Motion(b,u,art,now,second?u.StoneShield?"C2_Idle_1":"C2_Idle_2":"C1_Idle",
                second?u.StoneShield?"C2_Move_1":"C2_Move_2":"C1_Move",second?u.StoneShield?"C2_Attack_1":"C2_Attack_2":"C1_Attack");
        }
        static Frame Patriot(ManualBattle b,DeployedUnitState u,BossRosterArt art,double now)
        {
            if(u.Resolution==UnitResolution.Dead)return F("Die",now-u.DeathTime);
            if(u.PatriotPhase==PatriotPhase.Reborn)
            {
                double elapsed=now-u.PatriotPhaseStartedAt,total=Parameter(b,u,"rebirth","rebirthDuration",60);
                double begin=art["revive_1"].Duration,end=art["revive_3"].Duration;
                if(elapsed<begin)return F("revive_1",elapsed);
                if(elapsed>=total-end)return F("revive_3",elapsed-(total-end));
                return F("revive_2",elapsed-begin,true);
            }
            if(b.PatriotCasting(u))return F("Skill",now-u.Spear.StartedAt);
            bool second=u.PatriotPhase==PatriotPhase.Destruction;
            return Motion(b,u,art,now,second?"Idle_2":"Idle_1",second?"Move_2":"Move_1",second?"Attack_2":"Attack_1");
        }
        static Frame Talulah(ManualBattle b,DeployedUnitState u,BossRosterArt art,double now)
        {
            if(u.Resolution==UnitResolution.Dead)return F(u.TalulahPhase==TalulahPhase.Dead?"Reborn_2_End":"Die",now-u.DeathTime);
            double phase=now-u.TalulahPhaseStartedAt;
            if(u.TalulahPhase==TalulahPhase.FirstFall)
                return phase<art["Reborn_1_Start"].Duration?F("Reborn_1_Start",phase):F("Reborn_1_Idle",0,true);
            if(u.TalulahPhase==TalulahPhase.FirstRain)return F("Reborn_1_Idle",0,true);
            if(u.TalulahPhase==TalulahPhase.Second&&phase<art["Reborn_1_End"].Duration)return F("Reborn_1_End",phase);
            if(u.TalulahPhase==TalulahPhase.FinalFall)return F("Reborn_2_Start",phase);
            if(u.TalulahPhase==TalulahPhase.FinalRain)
            {
                double tail=now-(u.TalulahAutomaticRainStartedAt-Parameter(b,u,"threeStarPhase","fallDuration",5));
                return tail<art["Reborn_2_Start"].Duration?F("Reborn_2_Start",tail):F("Reborn_2_Idle",0,true);
            }
            bool second=b.TalulahSecondForm(u);string suffix=second?"B":"A";
            if(b.IsSkillActionPlaying(u))
            {
                string raw="Skill_1_"+suffix;double total=b.UnitCombat(u).SkillInterval,elapsed=now-u.FireRainStartedAt;
                // Existing six-second player fire rain holds a real source pose;
                // it does not invent a native looping clip or extend source metadata.
                double raise=Math.Min(.8,total*.2),lower=Math.Min(.6,total*.2),hold=Math.Min(1.1666667,art[raw].Duration);
                double t=elapsed<raise?elapsed/Math.Max(.001,raise)*hold:elapsed<total-lower?hold:
                    hold+(elapsed-(total-lower))/Math.Max(.001,lower)*(art[raw].Duration-hold);
                return F(raw,t);
            }
            return Motion(b,u,art,now,"Idle_"+suffix,"Move_"+suffix,"Attack_"+suffix);
        }
        static Frame Frost(ManualBattle b,DeployedUnitState u,BossRosterArt art,double now)
        {
            if(u.Resolution==UnitResolution.Dead)return F("Die",now-u.DeathTime);
            if(u.IsReviving)
            {
                // Verified native mapping: RebornTalent -> animKey Reborn -> Skill_2.
                // Preserve the approved gameplay delay; fit the whole 7.2s original gesture.
                double delay=Parameter(b,u,"revive","delay",5),elapsed=now-(u.ReviveAt-delay);
                return F("Skill_2",elapsed/Math.Max(.001,delay)*art["Skill_2"].Duration);
            }
            if(Window(now,u.FrostSealStartedAt,b.UnitCombat(u).SkillInterval))return F("Skill_1",now-u.FrostSealStartedAt);
            if(Window(now,u.IceRingStartedAt,Parameter(b,u,"iceRing","actionDuration",3)))return F("Skill_1",now-u.IceRingStartedAt);
            return Motion(b,u,art,now,"Idle","Move","Attack");
        }
        static Frame Mudrock(ManualBattle b,DeployedUnitState u,BossRosterArt art,double now)
        {
            if(u.Resolution==UnitResolution.Dead)return F("Die",now-u.DeathTime);
            if(Window(now,u.MudrockCaptureStartedAt,Parameter(b,u,"capture","actionDuration",1.5f)))return F("Skill_1",now-u.MudrockCaptureStartedAt);
            if(Window(now,u.BarrierRefreshStartedAt,ManualBattle.MudrockRefreshDuration))return F("Skill_2",now-u.BarrierRefreshStartedAt);
            return Motion(b,u,art,now,"Idle","Move",u.OrdinaryAttacksFired>=3?"Attack_2":"Attack_1");
        }
        static Frame Vivi(ManualBattle b,DeployedUnitState u,BossRosterArt art,double now)
        {
            if(u.Resolution==UnitResolution.Dead)return F("Die",now-u.DeathTime);
            double elapsed=now-u.LastAttackTime;
            if(u.LastAttackEmpowered&&Window(now,u.LastAttackTime,b.UnitCombat(u).SkillInterval))
                return F("Skill_3_Loop",elapsed/b.UnitCombat(u).SkillInterval*art["Skill_3_Loop"].Duration);
            if(u.SkillAmmo>0)return F("Skill_3_Idle",now-u.SpawnTime,true);
            // Operators have no Move source; this is the explicit idle translation adaptation.
            return Motion(b,u,art,now,"Idle",null,"Attack_A");
        }
        static bool RearView(ManualBattle b,DeployedUnitState u,double now)
        {
            Vector3 direction=u.Motion.Direction;
            bool action=Window(now,u.LastAttackTime,Math.Max(b.UnitActionDuration(u),u.LastAttackEmpowered?b.UnitCombat(u).SkillInterval:0)/Math.Max(.01,u.LastAttackRate));
            if(action)
                for(int i=b.CombatShots.Count-1;i>=0;i--)
                {var shot=b.CombatShots[i];if(!shot.FromDefender&&shot.SourceId==u.EntityId&&shot.StartedAt>=u.LastAttackTime){direction=shot.Destination-shot.Origin;break;}}
            else if(!u.IsMoving)
            {var target=b.FindDefender(u.CombatTargetId);if(target!=null)direction=target.Position-u.Motion.Position;}
            if(!action&&u.IsMoving&&!b.UnitControlImmune(u))
            {
                if(u.LureUntil>now){if(u.Motion.DistanceTravelled>u.LureDistance)direction=-direction;}
                else if(u.FearUntil>now)direction=-direction;
            }
            return direction.z>.18f;
        }
    }
}
