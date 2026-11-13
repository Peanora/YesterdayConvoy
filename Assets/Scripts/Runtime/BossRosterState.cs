using System;
using System.Collections.Generic;
using UnityEngine;
namespace YesterdayConvoy
{
    public enum BossObjectInteraction { None, Health, HitCount, CameraSeal, Occupy, Absorb }
    public sealed class BossRosterSkillInfo
    {
        public string SkillId,Name,Description;public string Id=>SkillId;
        public bool Ready;public float SP,MaxSP;public SkillFailure Failure;
    }
    public sealed class BossRosterState
    {
        public string EntityId,CardId,SourceVariant="TL-10",Animation="",Polarity="yin";
        public int Phase=1,Region=1,Matrix,BlackCrownCharges=1,Generation;
        public double AnimationAt,AnimationUntil,LockedUntil,InvulnerableUntil,NextAuraAt,NextPassiveAt;
        public bool AnimationLoop,Transitioning,Final,PartnerDown,Present=true,Fled;
        public float Shield,AttackMultiplier=1,DamageMultiplier=1,UnattributedDamage,AnimationRate=1;
        public readonly Dictionary<string,float> SP=new Dictionary<string,float>();
        public readonly Dictionary<string,double> Timers=new Dictionary<string,double>();
        public readonly Dictionary<string,int> Counts=new Dictionary<string,int>();
        public readonly Dictionary<string,float> DamageByDefender=new Dictionary<string,float>();
        public readonly HashSet<string> Consumed=new HashSet<string>();
        internal readonly Dictionary<string,string> NativeOverrides=new Dictionary<string,string>();
        internal DefenderState IncomingDefender;
        internal long Activation;
    }
    public sealed class BossRosterObject
    {
        public string Id,Kind,Name,OwnerId,CardId,TargetId,Polarity="yin";public long ActivationId;
        public string Animation="Idle";public double AnimationAt,AnimationUntil;public bool AnimationLoop=true;
        public BossObjectInteraction Interaction;public double InvulnerableUntil,ControlledUntil;
        public Vector3 Position,Velocity,TargetPosition;public float Health,MaxHealth,Shield,Radius=1,PowerMultiplier=1;
        public double StartedAt,Until=double.PositiveInfinity,NextAt,AttackStartedAt=double.NegativeInfinity,ShotAt=double.NegativeInfinity,ImpactAt=double.NegativeInfinity,DestroyedAt=double.PositiveInfinity,BuffUntil;public bool Removed,Safe,Blocking;
        public bool Alive=>!Removed&&Health>0;
        public string EntityId=>Id;public string SourceId=>OwnerId;public bool Active=>Alive;public double SpawnAt=>StartedAt;
        internal readonly HashSet<string> HitIds=new HashSet<string>();
    }
    public sealed class BossRosterVisualEvent
    {
        public readonly long Id,ActivationId;public readonly int RunId,Phase,HitIndex,Region;
        public readonly string EntityId,CardId,Kind,SkillKey,NativeSkillKey,Animation,TargetId;
        public readonly double At,EndAt;public readonly Vector3 Position,TargetPosition;public readonly bool Applied,Cancelled;
        internal BossRosterVisualEvent(long id,long activation,int run,string entity,string card,string kind,string skill,string native,string animation,int phase,int hit,double at,double end,Vector3 position,Vector3 target,string targetId,bool applied,bool cancelled,int region=1)
        {Id=id;ActivationId=activation;RunId=run;EntityId=entity;CardId=card;Kind=kind;SkillKey=skill;NativeSkillKey=native;Animation=animation;Phase=phase;HitIndex=hit;At=at;EndAt=end;Position=position;TargetPosition=target;TargetId=targetId;Applied=applied;Cancelled=cancelled;Region=region;}
    }
}
