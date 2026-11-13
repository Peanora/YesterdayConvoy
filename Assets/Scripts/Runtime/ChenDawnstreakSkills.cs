using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    internal readonly struct ChenDawnWavePoint
    {
        internal readonly Vector3 Position;
        internal readonly double At;
        internal readonly bool Turn;
        internal ChenDawnWavePoint(Vector3 position,double at,bool turn=false){Position=position;At=at;Turn=turn;}
    }

    // One real projectile, rather than a skill-age sprite at the caster's feet.
    internal sealed class ChenDawnWaveState
    {
        public int Sequence {get;internal set;}
        public string SourceId {get;internal set;}
        public Vector3 Position {get;internal set;}
        public Vector3 Direction {get;internal set;}
        public double StartedAt {get;internal set;}
        public double EndsAt {get;internal set;}
        public double EndedAt {get;internal set;}=double.PositiveInfinity;
        public double LastTurnedAt {get;internal set;}=double.NegativeInfinity;
        public int Turns {get;internal set;}
        public int Hits {get;internal set;}
        public bool Active=>Launched&&double.IsPositiveInfinity(EndedAt);
        internal bool Launched;
        internal float Attack;
        internal double AdvancedUntil,NextSelectAt;
        internal readonly HashSet<string> HitTargets=new HashSet<string>();
        internal readonly List<ChenDawnWavePoint> Points=new List<ChenDawnWavePoint>();
        public IReadOnlyList<ChenDawnWavePoint> History=>Points.AsReadOnly();
    }

    public sealed partial class ManualBattle
    {
        // Source: AdvancedMovement speed 1.5; ChangeStateWithHighlandBehaviour
        // hitDistance .25, clearTargetsAlreadyHitListWhenHit. Range sphere radius 1.3.
        readonly List<ChenDawnWaveState> chenDawnWaves=new List<ChenDawnWaveState>();
        readonly List<Bounds> chenDawnObstacles=new List<Bounds>();
        readonly Dictionary<string,Vector3> chenDawnDirections=new Dictionary<string,Vector3>();
        Bounds chenDawnBoard;
        bool chenDawnTerrain;
        int chenDawnSequence;
        internal IReadOnlyList<ChenDawnWaveState> ChenDawnWaves=>chenDawnWaves.AsReadOnly();
        public Vector3 ChenDawnFacing(DefenderState d)=>d!=null&&chenDawnDirections.TryGetValue(d.EntityId,out var direction)?direction:Vector3.left;
        void ResetChenDawn(){chenDawnWaves.Clear();chenDawnDirections.Clear();chenDawnSequence=0;}

        // Geometry is supplied once from the saved map. This never constructs terrain.
        internal void ConfigureChenDawnTerrain(IEnumerable<Bounds> highlands,Bounds board,IEnumerable<Bounds> portals=null)
        {
            if(board.size.x<=0||board.size.z<=0)throw new ArgumentException("A finite authored battlefield is required.");
            chenDawnBoard=board;chenDawnTerrain=true;chenDawnObstacles.Clear();
            if(highlands!=null)chenDawnObstacles.AddRange(highlands);
            if(portals!=null)chenDawnObstacles.AddRange(portals);
        }
        static Vector3 ChenCardinal(Vector3 delta)=>Mathf.Abs(delta.x)>=Mathf.Abs(delta.z)?new Vector3(delta.x<0?-1:1,0,0):new Vector3(0,0,delta.z<0?-1:1);
        void BeginChenDawnSkill(DefenderState d)
        {
            DeployedUnitState nearest=null;
            foreach(var u in units)if(CanDefenderTarget(u)&&(nearest==null||GroundDistance(d.Position,u.Motion.Position)<GroundDistance(d.Position,nearest.Motion.Position)))nearest=u;
            // This game's defenders have no manual facing placement: choose a cardinal
            // direction once at activation, then keep source-style fixed S3 facing.
            var direction=nearest!=null?ChenCardinal(nearest.Motion.Position-d.Position):ChenDawnFacing(d);
            chenDawnDirections[d.EntityId]=direction;d.Ammo=0;
            d.AdvancedSkillWasActive=true;d.SkillActionUntil=Clock+13.0/30;
            d.NextAttackTime=Math.Max(d.NextAttackTime,d.SkillActionUntil);
            var wave=new ChenDawnWaveState{Sequence=++chenDawnSequence,SourceId=d.EntityId,Position=d.Position,Direction=direction,
                StartedAt=d.SkillActionUntil,EndsAt=d.SkillActiveUntil,AdvancedUntil=d.SkillActionUntil,NextSelectAt=d.SkillActionUntil,Attack=DefenderSkillAttack(d)};
            chenDawnWaves.Add(wave);
            SeventhEvent(SeventhDefenderEventKind.SkillStart,d,null,d.Position,Clock,d.SkillActiveUntil,direction);
        }
        internal bool ChenDawnInRange(DefenderState d,DeployedUnitState unit,bool active)
        {
            if(!CanDefenderTarget(unit)||UnitAirborne(unit))return false;
            if(unit.BlockerId==d.EntityId)return true;
            var delta=unit.Motion.Position-d.Position;
            var forward=active?ChenDawnFacing(d):ChenCardinal(delta);
            float col=Vector3.Dot(delta,forward),row=Vector3.Dot(delta,new Vector3(forward.z,0,-forward.x));
            // Original range 3-12: (row +/-1,col0/1), (row0,col0/1/2/3).
            return active?(Mathf.Abs(row)<=.5f&&col>=-.5f&&col<=3.5f||Mathf.Abs(row)<=1.5f&&col>=-.5f&&col<=1.5f):
                Mathf.Abs(row)<=.5f&&col>=-.5f&&col<=1.5f;
        }
        bool ScheduleChenDawnAttack(DefenderState d,DeployedUnitState primary)
        {
            bool active=DefenderSkillActive(d);
            var targets=new List<DeployedUnitState>();
            if(ChenDawnInRange(d,primary,active))targets.Add(primary);
            var others=new List<DeployedUnitState>();
            foreach(var u in units)if(u!=primary&&ChenDawnInRange(d,u,active))others.Add(u);
            others.Sort((a,b)=>PreferDefenderTarget(d,a,b)?-1:PreferDefenderTarget(d,b,a)?1:string.CompareOrdinal(a.EntityId,b.EntityId));
            targets.AddRange(others);
            if(targets.Count==0){d.TargetId=null;return true;}
            if(targets.Count>(active?3:1))targets.RemoveRange(active?3:1,targets.Count-(active?3:1));
            float rate=Math.Max(.0001f,DefenderAttackRate(d));
            d.TargetId=targets[0].EntityId;d.LastActionTime=Clock;d.LastActionRate=rate;d.LastActionHealing=false;d.LastAttackSkill=active;
            d.NextAttackTime=Clock+d.Settings.Interval/rate;
            foreach(var unit in targets)
                for(int hit=0;hit<(active?3:1);hit++)
                {
                    double at=Clock+(active?(16+hit*2)/30.0:13.0/30)/rate;
                    QueueDefenderShot(d,new CombatShot(d.EntityId,unit.EntityId,true,active,d.Position,unit.Motion.Position,at,0,
                        DefenderSkillAttack(d)*(active?1.8f:1),damageType:CombatDamageType.Arts){ChenDawnHitIndex=hit});
                }
            ShotsFired++;LastShotTime=Clock;LastShotEntityId=d.TargetId;LastShotPosition=targets[0].Motion.Position;d.SeventhAttacks++;
            return true;
        }
        static bool ChenContainsXZ(Bounds rectangle,Vector3 point)=>point.x>rectangle.min.x+.00001f&&point.x<rectangle.max.x-.00001f&&point.z>rectangle.min.z+.00001f&&point.z<rectangle.max.z-.00001f;
        CombatDamageType ChenDawnWeaknessDamageType(DeployedUnitState target,float attack)
        {
            float physical=CalculateDamage(attack,UnitArmor(target),UnitResistance(target),CombatDamageType.Physical);
            float arts=CalculateDamage(attack,UnitArmor(target),UnitResistance(target),CombatDamageType.Arts);
            return physical>arts+.0001f?CombatDamageType.Physical:CombatDamageType.Arts;
        }
        bool TryChenDawnDodge(DefenderState d,CombatDamageType type)
        {
            if(d==null||!d.Alive||d.Settings.Skill.Kind!=DefenderSkillKind.ChenDawnstreak||!d.ChenDawnDodgeReady||
                type!=CombatDamageType.Physical&&type!=CombatDamageType.Arts)return false;
            d.ChenDawnDodgeReady=false;d.ChenDawnLastDodgeAt=Clock;return true;
        }
        void StepChenDawnTalents()
        {
            foreach(var d in defenders)
            {
                if(!d.Alive||d.Settings.Skill.Kind!=DefenderSkillKind.ChenDawnstreak||Clock+1e-6<d.ChenDawnNextTalentAt)continue;
                // Original unmoduled E2 talent: integer percentages [30,161).
                float scale=(30+Mathf.FloorToInt(d.Roll()*131))*.01f;
                d.SkillHealthRestored+=RestoreDefenderHealth(d,DefenderSkillAttack(d)*scale);
                d.ChenDawnDodgeReady=true;d.ChenDawnLastTalentAt=Clock;d.ChenDawnNextTalentAt=Clock+7;
            }
        }
        bool ChenObstacleAhead(Vector3 position,Vector3 direction,float distance)
        {
            if(!chenDawnTerrain)return false;
            Vector3 probe=position+direction*distance;
            if(probe.x<chenDawnBoard.min.x||probe.x>chenDawnBoard.max.x||probe.z<chenDawnBoard.min.z||probe.z>chenDawnBoard.max.z)return true;
            foreach(var obstacle in chenDawnObstacles)if(ChenContainsXZ(obstacle,probe))return true;
            return false;
        }
        static void ChenPoint(ChenDawnWaveState wave,double at,bool turn=false)
        {
            wave.Points.Add(new ChenDawnWavePoint(wave.Position,at,turn));
            if(wave.Points.Count>2048)wave.Points.RemoveRange(0,wave.Points.Count-2048);
        }
        void ChenSelectAt(ChenDawnWaveState wave,DefenderState source,double at)
        {
            foreach(var unit in units)
            {
                if(!CanDefenderTarget(unit)||GroundDistance(wave.Position,unit.Motion.Position)>1.3f||!wave.HitTargets.Add(unit.EntityId))continue;
                QueueDefenderShot(source,new CombatShot(source.EntityId,unit.EntityId,true,true,wave.Position,unit.Motion.Position,at,0,wave.Attack*5.5f,damageType:CombatDamageType.Arts)
                    {ChenDawnWaveSequence=wave.Sequence,ChenDawnCurrentHealthRatio=.06f,SeventhSecondary=true,PersistAfterSourceDeath=true},source.SkillStartedAt,1);
                wave.Hits++;
            }
            ChenPoint(wave,at);wave.NextSelectAt=at+.03;
        }
        void StepChenDawnWaves(float step)
        {
            StepChenDawnTalents();
            foreach(var wave in chenDawnWaves)
            {
                if(!double.IsPositiveInfinity(wave.EndedAt))continue;
                var source=FindDefender(wave.SourceId);
                bool finish=source==null||!source.Alive||Clock>=wave.EndsAt;
                double until=Math.Min(Clock,wave.EndsAt);
                if(source!=null&&!source.Alive)until=Math.Min(until,source.DeathTime);
                if(source==null||until<wave.StartedAt)
                {
                    if(finish)wave.EndedAt=until;
                    continue;
                }
                if(!wave.Launched)
                {
                    wave.Launched=true;ChenPoint(wave,wave.StartedAt);
                    SeventhEvent(SeventhDefenderEventKind.DawnWaveLaunched,source,null,wave.Position,wave.StartedAt,wave.EndsAt,wave.Direction);
                    ChenSelectAt(wave,source,wave.StartedAt);
                }
                // Substeps retain source movement speed and cannot jump a thin map cell.
                while(wave.AdvancedUntil<until-1e-7)
                {
                    float dt=(float)Math.Min(.02,Math.Min(until-wave.AdvancedUntil,wave.NextSelectAt-wave.AdvancedUntil));double at=wave.AdvancedUntil+dt;
                    int turns=0;var nextDirection=wave.Direction;
                    while(turns<4&&ChenObstacleAhead(wave.Position,nextDirection,.25f))
                    {
                        nextDirection=new Vector3(nextDirection.z,0,-nextDirection.x);turns++;
                    }
                    // A fully enclosed point must not manufacture four turns and
                    // clear the hit set every frame without any projectile travel.
                    if(turns>0&&turns<4)
                    {
                        wave.Direction=nextDirection;wave.Turns+=turns;
                        wave.LastTurnedAt=at;wave.HitTargets.Clear();ChenPoint(wave,at,true);
                        SeventhEvent(SeventhDefenderEventKind.DawnWaveTurned,source,null,wave.Position,at,wave.EndsAt,wave.Direction);
                    }
                    if(turns<4)wave.Position+=wave.Direction*(1.5f*dt);
                    if(at+1e-7>=wave.NextSelectAt)
                        ChenSelectAt(wave,source,at);
                    wave.AdvancedUntil=at;
                }
                if(finish)
                {
                    wave.EndedAt=until;ChenPoint(wave,until);
                    SeventhEvent(SeventhDefenderEventKind.DawnWaveEnded,source,null,wave.Position,until,until,wave.Direction);
                }
            }
            chenDawnWaves.RemoveAll(w=>!double.IsPositiveInfinity(w.EndedAt)&&Clock-w.EndedAt>3);
        }
    }
}
