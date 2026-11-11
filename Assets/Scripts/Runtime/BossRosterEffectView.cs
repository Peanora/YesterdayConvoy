using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Consumes simulation facts only. It never causes damage, spawns a logical unit or advances a battle clock.</summary>
    public sealed class BossRosterEffectView:MonoBehaviour
    {
        sealed class Playing
        {
            internal long Activation;internal string Entity,Skill,TargetId;internal double At,End;
            internal Vector3 Position,Target;internal BossRosterFxArt.Clip Clip;internal BossRosterFxArt.Binding Binding;
        }
        Func<ManualBattle> source;Func<double> presentation;ManualBattle observed;int run=-1;long fallbackId;
        readonly List<Playing> playing=new List<Playing>();readonly HashSet<long> events=new HashSet<long>();
        readonly HashSet<CombatShot> launched=new HashSet<CombatShot>(),resolved=new HashSet<CombatShot>(),flights=new HashSet<CombatShot>();
        readonly Dictionary<(string,long),string> activationAnimations=new Dictionary<(string,long),string>();
        readonly HashSet<(string,double)> casts=new HashSet<(string,double)>();
        BossRosterFxLayer layer;BossRosterDeviceView devices;
        public bool UseCombatShotFallback {get;set;}=true;
        public int VisibleEffects=>layer?.VisibleCount??0;
        public int VisibleDevices=>devices?.VisibleDevices??0;
        public int ActiveEffects=>playing.Count;
        public static bool OwnsCard(string cardId)=>BossRosterFxArt.OwnsCard(cardId);
        public double PendingUntil
        {
            get
            {
                var battle=source?.Invoke();if(battle==null||battle!=observed||run!=battle.RunId)return double.NegativeInfinity;
                double end=double.NegativeInfinity;
                foreach(var value in playing)if(value.At<=battle.Clock+1e-6&&!double.IsInfinity(value.End))end=Math.Max(end,value.End);
                return Math.Max(end,devices?.PendingUntil??double.NegativeInfinity);
            }
        }
        public void Bind(Func<ManualBattle> battle,Func<double> presentationClock=null)
        {Clear();source=battle;presentation=presentationClock;if(devices==null)devices=gameObject.GetComponent<BossRosterDeviceView>()??gameObject.AddComponent<BossRosterDeviceView>();devices.Bind(battle,presentationClock);}
        static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
        void Observe(ManualBattle battle)
        {
            if(observed==battle&&run==(battle?.RunId??-1))return;
            Clear();observed=battle;run=battle?.RunId??-1;
        }
        // This primitive entry deliberately has no dependency on a not-yet-added battle event property.
        // The controller may forward BossRosterVisualEvent fields unchanged, including both real hit indices.
        public void Receive(long id,long activationId,int eventRunId,string entityId,string cardId,string kind,string skillKey,
            string nativeSkillKey,string animation,int phase,int hitIndex,double at,double endAt,Vector3 position,Vector3 targetPosition,
            string targetId,bool applied,bool cancelled,int region=1)
        {
            var battle=source?.Invoke();Observe(battle);
            if(kind=="Despawn")devices?.NotifyRemoved(eventRunId,targetId,at);
            if(battle!=null&&eventRunId==run&&Finite(at)&&!cancelled)devices?.NotifyFact(id,activationId,eventRunId,cardId,kind,skillKey,phase,at,endAt,position,targetPosition,targetId,applied,hitIndex);
            if(battle==null||eventRunId!=run||!OwnsCard(cardId)||!Finite(at)||string.IsNullOrEmpty(entityId)||string.IsNullOrEmpty(kind)||!events.Add(id))return;
            if(!string.IsNullOrEmpty(animation))activationAnimations[(entityId,activationId)]=animation;
            else activationAnimations.TryGetValue((entityId,activationId),out animation);
            if(kind=="SkillEnd"||kind=="StateEnd"||kind=="Death"||kind=="Despawn"||kind=="PhaseStart")
            {
                bool ownerEnds=kind=="Death"||kind=="PhaseStart"||kind=="Despawn"&&targetId==entityId;
                foreach(var value in playing)
                    if(value.Entity==entityId&&!value.Binding.projectileLife&&value.Binding.attach!="projectile"&&value.Binding.finishOnEnd&&(ownerEnds||(kind=="SkillEnd"||kind=="StateEnd")&&value.Activation==activationId))value.End=Math.Min(value.End,at);
            }
            if(cancelled)return;
            var task=BossRosterFxArt.TaskForCard(cardId);
            foreach(var binding in BossRosterFxArt.Bindings(task))
            {
                if(binding.kind!=kind||!string.IsNullOrEmpty(binding.skill)&&binding.skill!=skillKey||!string.IsNullOrEmpty(binding.nativeSkill)&&!string.Equals(binding.nativeSkill,nativeSkillKey,StringComparison.OrdinalIgnoreCase)||binding.phase>=0&&binding.phase!=phase||binding.hitIndex>=0&&binding.hitIndex!=hitIndex||binding.region>0&&binding.region!=region||!string.IsNullOrEmpty(binding.animation)&&!string.IsNullOrEmpty(animation)&&!AnimationMatches(binding.animation,animation))continue;
                if(kind=="Impact"&&!applied&&!binding.allowUnapplied)continue;
                float facing=Vector3.Dot(targetPosition-position,Camera.main!=null?Camera.main.transform.right:Vector3.right);
                if(binding.direction=="left"&&facing>=0||binding.direction=="right"&&facing<0)continue;
                var clip=BossRosterFxArt.Get(BossRosterFxArt.Key(task,binding.key));
                double end=binding.loop?double.PositiveInfinity:at+clip.Duration;
                if(binding.finishOnEnd&&Finite(endAt)&&endAt>=at)end=Math.Min(end,endAt);
                playing.Add(new Playing{Activation=activationId,Entity=entityId,Skill=skillKey,TargetId=targetId,At=at,End=end,Position=position,Target=targetPosition,Clip=clip,Binding=binding});
            }
        }
        internal static bool AnimationMatches(string expected,string actual)
        {
            if(string.IsNullOrEmpty(expected)||string.IsNullOrEmpty(actual))return true;
            string Normalize(string value)=>value.StartsWith("A_",StringComparison.Ordinal)||value.StartsWith("B_",StringComparison.Ordinal)?value.Substring(2):value;
            expected=Normalize(expected);actual=Normalize(actual);
            return expected==actual||(expected=="Attack"||expected=="Combat")&&actual.StartsWith(expected+"_",StringComparison.Ordinal);
        }
        void ReadExistingShots(ManualBattle battle)
        {
            foreach(var shot in battle.CombatShots)
            {
                if(shot.FromDefender||shot.Healing||shot.SynergyOrder||shot.FireRain)continue;
                var unit=battle.FindUnit(shot.SourceId);if(unit==null||!OwnsCard(unit.CardId)||!UseCombatShotFallback&&unit.CardId!="CARD_WISADEL_APRIL"&&unit.CardId!="CARD_M14")continue;
                bool vivi=unit.CardId=="CARD_WISADEL_APRIL";string shotSkill=vivi&&shot.Empowered?"s3":"attack";string shotNative=vivi&&shot.Empowered?"skchr_wisdel_3":"Attack";string shotAnimation=vivi?(shot.Empowered?"Skill_3_Loop":"Attack_A"):"Attack";
                var settings=battle.UnitCombat(unit);double actionAt=shot.StartedAt-settings.AttackWindup/Math.Max(.001f,unit.LastAttackRate);
                long activation=BitConverter.DoubleToInt64Bits(actionAt);
                if(shot.Erased){playing.RemoveAll(e=>e.Entity==unit.EntityId&&e.Activation==activation&&e.Binding.attach=="projectile");continue;}
                if(launched.Add(shot)&&casts.Add((unit.EntityId,actionAt)))
                    Receive(--fallbackId,activation,battle.RunId,unit.EntityId,unit.CardId,"AttackStart",shotSkill,shotNative,shotAnimation,unit.CardId=="CARD_M14"&&unit.ReviveUsed?2:1,0,
                        actionAt,actionAt+settings.AttackDuration/Math.Max(.001f,unit.LastAttackRate),shot.Origin,shot.Destination,shot.TargetId,false,false);
                if(!shot.Resolved&&battle.Clock>=shot.StartedAt&&flights.Add(shot))
                    Receive(--fallbackId,activation,battle.RunId,unit.EntityId,unit.CardId,"ProjectileSpawn",shotSkill,shotNative,shotAnimation,unit.CardId=="CARD_M14"&&unit.ReviveUsed?2:1,0,shot.StartedAt,shot.ImpactAt,shot.Origin,shot.Destination,shot.TargetId,false,false);
                if(shot.Resolved&&!shot.Applied)playing.RemoveAll(e=>e.Entity==unit.EntityId&&e.Activation==activation&&e.Binding.attach=="projectile");
                if(shot.Resolved&&resolved.Add(shot)&&shot.Applied)
                    Receive(--fallbackId,activation,battle.RunId,unit.EntityId,unit.CardId,"Impact",shotSkill,shotNative,shotAnimation,unit.CardId=="CARD_M14"&&unit.ReviveUsed?2:1,0,
                        shot.ImpactAt,shot.ImpactAt,shot.Origin,shot.Destination,shot.TargetId,true,false);
            }
        }
        static float CasterVisualScale(Playing value)
        {
            if(value.Binding.attach!="owner")return 1;
            // These seven exact native references are Sui's body electricity, casting
            // and withdrawal layers. The global ThreeStrikes range is also owner-bound,
            // so attachment alone must never scale its world area or target hit sprites.
            switch(value.Clip.Key)
            {
                case "boss/B21/enemy_dyshhj_lighting":
                case "boss/B21/enemy_dyshhj_lighting_right":
                case "boss/B21/enemy_dyshhj_skill_01_hit_01":
                case "boss/B21/enemy_dyshhj_die":
                case "boss/B21/enemy_dyshhj_die_02_start_01":
                case "boss/B21/enemy_dyshhj_die_02_start_02":
                case "boss/B21/enemy_dyshhj_die_02_hit_01":
                    return BossRosterArt.For("CARD_B21")?.VisualScale??1;
                default:return 1;
            }
        }
        void LateUpdate()
        {
            var battle=source?.Invoke();Observe(battle);if(battle==null)return;
            ReadExistingShots(battle);
            if(layer==null)layer=new BossRosterFxLayer(transform);
            layer.Begin();var camera=Camera.main;double now=presentation?.Invoke()??battle.Clock;
            for(int i=playing.Count-1;i>=0;i--)
            {
                var value=playing[i];if(now>=value.End){playing.RemoveAt(i);continue;}if(now<value.At)continue;
                var owner=battle.FindUnit(value.Entity);
                if(value.Binding.attach=="owner"&&owner!=null&&owner.Active)value.Position=owner.Motion.Position;
                if(value.Binding.attach=="target"&&value.Binding.loop&&!string.IsNullOrEmpty(value.TargetId))
                {var target=battle.FindDefender(value.TargetId);if(target==null||!target.Alive){playing.RemoveAt(i);continue;}value.Target=target.Position;}
                Vector3 position=value.Binding.attach=="target"?value.Target:value.Position;
                if(value.Binding.attach=="projectile")
                {
                    if(!Finite(value.End))continue;
                    float progress=Mathf.Clamp01((float)((now-value.At)/Math.Max(.001,value.End-value.At)));
                    position=Vector3.Lerp(value.Position,value.Target,progress);
                }
                position+=new Vector3(value.Binding.offsetX,value.Binding.offsetY,value.Binding.offsetZ);
                Vector3 direction=value.Target-value.Position;bool projectile=value.Binding.attach=="projectile";
                bool flip=!projectile&&string.IsNullOrEmpty(value.Binding.direction)&&camera!=null&&Vector3.Dot(direction,camera.transform.right)<0;
                layer.Show(value.Clip,now-value.At,position,camera,flip,value.Binding.loop,direction,projectile,visualScale:CasterVisualScale(value));
            }
            layer.End();
        }
        public void Clear()
        {
            layer?.Clear();devices?.Clear();playing.Clear();events.Clear();launched.Clear();resolved.Clear();flights.Clear();casts.Clear();activationAnimations.Clear();fallbackId=0;observed=null;run=-1;
        }
        void OnDisable()=>Clear();
        void OnDestroy(){Clear();layer?.Dispose();layer=null;}
    }
}
