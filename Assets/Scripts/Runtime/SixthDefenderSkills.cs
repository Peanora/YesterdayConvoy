using System;
using System.Collections.Generic;
using UnityEngine;
namespace YesterdayConvoy
{
    internal readonly struct PramanixSnowImpact
    {
        internal readonly int RunId;
        internal readonly long Sequence;
        internal readonly string SourceId,TargetId;
        internal readonly double At;
        internal readonly Vector3 Position;
        internal PramanixSnowImpact(int run,long sequence,string source,string target,double at,Vector3 position)
        {RunId=run;Sequence=sequence;SourceId=source;TargetId=target;At=at;Position=position;}
    }
    public sealed class SixthCell
    {
        public string SourceId {get;internal set;}
        public Vector3 Position {get;internal set;}
        public int Layers {get;internal set;}
        public bool Active {get;internal set;}
        public double Time {get;internal set;}
        internal readonly HashSet<string> Visitors=new HashSet<string>();
    }
    public sealed partial class ManualBattle
    {
        readonly List<SixthCell> snowCells=new List<SixthCell>(),chessPieces=new List<SixthCell>();
        readonly List<PramanixSnowImpact> pramanixSnowImpacts=new List<PramanixSnowImpact>();
        long pramanixSnowImpactSequence;
        internal IReadOnlyList<PramanixSnowImpact> PramanixSnowImpacts=>pramanixSnowImpacts;
        void RecordPramanixSnowImpact(DefenderState source,DeployedUnitState target,Vector3 position)
        {
            // Adjacent snow cells retain their individual damage. Their simultaneous
            // contact with the same owner/target produces one visual burst.
            for(int i=pramanixSnowImpacts.Count-1;i>=0&&pramanixSnowImpacts[i].At==Clock;i--)
                if(pramanixSnowImpacts[i].SourceId==source.EntityId&&pramanixSnowImpacts[i].TargetId==target.EntityId)return;
            if(pramanixSnowImpacts.Count==128)pramanixSnowImpacts.RemoveAt(0);
            pramanixSnowImpacts.Add(new PramanixSnowImpact(RunId,++pramanixSnowImpactSequence,source.EntityId,target.EntityId,Clock,position));
        }
        public IReadOnlyList<SixthCell> SnowCells=>snowCells.AsReadOnly();
        public IReadOnlyList<SixthCell> ChessPieces=>chessPieces.AsReadOnly();
        public float DefenderBudget {get;private set;}=30;
        void InitializeV2SixthDefenders()
        {
            if(!CampaignV2Enabled)return;
            DefenderState silver=null;
            foreach(var d in defenders)
            {
                if(d.Settings.Skill.Kind==DefenderSkillKind.WangGambit)d.ChessStock=(int)V2DefenderSkill(d,"stockInitial",7);
                if(silver==null&&d.Settings.Skill.Kind==DefenderSkillKind.SilverAshReign)silver=d;
            }
            if(silver==null)return;
            DefenderBudget=V2DefenderSkill(silver,"independentBudgetInitial",30);
            DefenderState low=null,high=null;
            foreach(var d in defenders)if(d.Reserve){if(low==null||d.ReserveCost<low.ReserveCost)low=d;if(high==null||d.ReserveCost>high.ReserveCost)high=d;}
            if(low!=null&&high!=low){low.ReserveCost=(int)V2DefenderSkill(silver,"reserveLowCost",8);high.ReserveCost=(int)V2DefenderSkill(silver,"reserveHighCost",20);}
        }
        public bool IsSixth(DefenderState d)=>d!=null&&(int)d.Settings.Skill.Kind>=(int)DefenderSkillKind.SilverAshReign;
        public float SixthVulnerability(DeployedUnitState u)=>u!=null&&u.VulnerableUntil>Clock?1+u.Vulnerability:1;
        public bool SixthReveals(DeployedUnitState u)
        {
            foreach(var d in defenders)if(d.Settings.Skill.Kind==DefenderSkillKind.SilverAshReign&&DefenderSkillActive(d)&&
                (GroundDistance(d.Position,u.Motion.Position)<=DefenderRange(d)||FindDefender(d.PartnerId) is DefenderState ally&&ally.Alive&&GroundDistance(ally.Position,u.Motion.Position)<=DefenderRange(d)))return true;
            return false;
        }
        bool BeginSixthSkill(DefenderState d)
        {
            if(!IsSixth(d))return false;d.SkillActionUntil=Clock+V2DefenderSkill(d,"startupSeconds",.35f);d.NextAttackTime=d.SkillActionUntil;d.NextSixthAt=Clock;
            switch(d.Settings.Skill.Kind)
            {
                case DefenderSkillKind.SilverAshReign:
                    DefenderBudget+=V2DefenderSkill(d,"budgetInstant",6);
                    if(!d.CostsExchanged)
                    {
                        DefenderState low=null,high=null;
                        foreach(var a in defenders)if(a.Reserve){if(low==null||a.ReserveCost<low.ReserveCost)low=a;if(high==null||a.ReserveCost>high.ReserveCost)high=a;}
                        if(low!=null&&high!=low){int cost=low.ReserveCost;low.ReserveCost=high.ReserveCost;high.ReserveCost=cost;d.CostsExchanged=true;}
                    }
                    d.BudgetReceived=0;
                    foreach(var a in defenders)if(a.Reserve&&DefenderBudget>=a.ReserveCost)
                    {DefenderBudget-=a.ReserveCost;a.Reserve=false;a.Position=a.Settings.RelayPosition;a.HighGround=false;d.PartnerId=a.EntityId;d.Reinforcements++;break;}
                    if(d.PartnerId==null)foreach(var a in defenders)if(a!=d&&a.Alive){d.PartnerId=a.EntityId;break;}
                    break;
                case DefenderSkillKind.PramanixSnow:
                    d.NextSnowAt=Clock+V2DefenderSkill(d,"snowIntervalSkill",1.5f);
                    foreach(var u in units)if(CanDefenderTarget(u)&&!UnitControlImmune(u)&&GroundDistance(d.Position,u.Motion.Position)<=DefenderRange(d))
                    {u.LureUntil=Clock+V2DefenderSkill(d,"lureDuration",10);u.LureDistance=u.Motion.ProjectDistance(d.Position);d.Lures++;}
                    break;
                case DefenderSkillKind.WangGambit:
                    int grant=(int)V2DefenderSkill(d,"stockGrantOnSkill",8),cap=(int)V2DefenderSkill(d,"stockCap",7);
                    int overflow=Math.Max(0,d.ChessStock+grant-cap);d.ChessStock=Math.Min(cap,d.ChessStock+grant);d.Ammo=(int)V2DefenderSkill(d,"ammo",20);d.LastActionTime=Clock;
                    foreach(var u in units)if(CanDefenderTarget(u)&&GroundDistance(d.Position,u.Motion.Position)<=DefenderRange(d))
                        {if(overflow<=0)break;if(AddChess(d,new Vector3(Mathf.Round(u.Motion.Position.x),.04f,Mathf.Round(u.Motion.Position.z)),false))overflow--;}
                    break;
            }
            return true;
        }
        bool StepUnitLure(DeployedUnitState u,float dt)
        {
            if(u.LureUntil<=Clock||UnitControlImmune(u))return false;
            if(IsSkillActionPlaying(u))return false;
            u.IsBlocked=u.HasCombatTarget=false;u.BlockerId=u.CombatTargetId=null;
            if(Clock<u.LastAttackTime+UnitActionDuration(u)/u.LastAttackRate){u.IsMoving=false;return true;}
            double before=u.Motion.DistanceTravelled;u.Motion.ApproachDistance(u.LureDistance,dt*UnitMoveMultiplier(u));u.IsMoving=Math.Abs(u.Motion.DistanceTravelled-before)>.00001;
            return true;
        }
        void StepSixthDefenders(float dt)
        {
            foreach(var u in units)u.SnowMove=1;
            foreach(var d in defenders)
            {
                if(!IsSixth(d)||!d.Alive)continue;
                if(d.AdvancedSkillWasActive&&!DefenderSkillActive(d))
                {d.AdvancedSkillWasActive=false;d.SkillEndedAt=d.SkillActiveUntil;}
                if(d.Settings.Skill.Kind==DefenderSkillKind.SilverAshReign&&DefenderSkillActive(d))
                {
                    float budget=V2DefenderSkill(d,"budgetDuringSkillTotal",24);
                    float rate=CampaignV2Enabled?budget/d.Settings.Skill.Duration:.5f;
                    float total=Math.Min(budget,(float)(Clock-d.SkillStartedAt)*rate);DefenderBudget+=Math.Max(0,total-d.BudgetReceived);d.BudgetReceived=total;
                }
                if(d.Settings.Skill.Kind==DefenderSkillKind.PramanixSnow&&Clock>=d.NextSnowAt)
                {
                    d.NextSnowAt=Clock+(DefenderSkillActive(d)?V2DefenderSkill(d,"snowIntervalSkill",1.5f):V2DefenderSkill(d,"snowIntervalIdle",10));
                    var generated=new HashSet<Vector2Int>();
                    foreach(var lane in Lanes)for(int i=1;i<lane.Points.Count;i++)
                    {
                        var a=lane.Points[i-1];var z=lane.Points[i];int count=Mathf.CeilToInt(Vector3.Distance(a,z));
                        for(int k=0;k<=count;k++)
                        {
                            var p=Vector3.Lerp(a,z,count==0?0:k/(float)count);p.x=Mathf.Round(p.x);p.z=Mathf.Round(p.z);
                            if(GroundDistance(p,d.Position)>DefenderRange(d)||!generated.Add(new Vector2Int((int)p.x,(int)p.z)))continue;
                            var cell=snowCells.Find(c=>c.SourceId==d.EntityId&&GroundDistance(c.Position,p)<.1f);
                            if(cell==null){cell=new SixthCell{SourceId=d.EntityId,Position=p,Active=true};snowCells.Add(cell);}
                            cell.Layers=Math.Min((int)V2DefenderSkill(d,"snowMaxLayers",5),cell.Layers+1);
                        }
                    }
                }
                if(d.Settings.Skill.Kind==DefenderSkillKind.WangGambit)
                {

                    if(Clock>=d.NextSixthAt)
                    {
                        d.NextSixthAt=Clock+V2DefenderSkill(d,"placementInterval",2);
                        foreach(var u in units)if(CanDefenderTarget(u)&&GroundDistance(d.Position,u.Motion.Position)<=DefenderRange(d))
                        {
                            var p=new Vector3(Mathf.Round(u.Motion.Position.x),.04f,Mathf.Round(u.Motion.Position.z));
                            if(!DefenderSkillActive(d))p+=u.Motion.Direction;
                            if(d.ChessStock>0||DefenderSkillActive(d)&&d.Ammo>0){if(AddChess(d,p,true)){d.ChessStock=Math.Max(0,d.ChessStock-1);d.LastActionTime=Clock;d.LastActionHealing=false;}}
                            break;
                        }
                    }
                    if(DefenderSkillActive(d)&&Clock-d.LastActionTime>V2DefenderSkill(d,"noTargetEndSeconds",4)){d.ChessStock=Math.Min((int)V2DefenderSkill(d,"stockCap",7),d.ChessStock+d.Ammo);d.Ammo=0;}
                    if(DefenderSkillActive(d)&&d.Ammo==0&&d.SkillActiveUntil-Clock>d.Settings.ActionDuration+.05){d.SkillActiveUntil=Clock+d.Settings.ActionDuration;}
                }
            }
            foreach(var cell in snowCells)
            {
                var d=FindDefender(cell.SourceId);if(d==null||!d.Alive||cell.Layers==0)continue;
                var now=new HashSet<string>();
                foreach(var u in units)if(u.Active&&!u.IsReviving&&!UnitAirborne(u)&&GroundDistance(cell.Position,u.Motion.Position)<V2DefenderSkill(d,"snowCellRadius",.7f))
                {
                    now.Add(u.EntityId);u.SnowMove=Math.Min(u.SnowMove,1-cell.Layers*V2DefenderSkill(d,"snowSlowPerLayer",.1f));
                    if(!cell.Visitors.Contains(u.EntityId))
                    {
                        var contact=u.Motion.Position;
                        if(TryDefenderDamageUnit(u,d.Settings.Attack*V2DefenderSkill(d,"snowEntryAttackFraction",.6f),CombatDamageType.Arts,sourceDefender:d))
                            RecordPramanixSnowImpact(d,u,contact);
                        d.SnowHits++;
                    }
                }
                bool departed=false;foreach(string id in cell.Visitors)if(!now.Contains(id)){departed=true;break;}
                cell.Visitors.Clear();foreach(string id in now)cell.Visitors.Add(id);
                if(departed)cell.Layers=0;
            }
            foreach(var piece in chessPieces)
            {
                var d=FindDefender(piece.SourceId);if(d==null||!d.Alive){piece.Active=false;continue;}
                if(!piece.Active)continue;bool trigger=false;
                foreach(var u in units)trigger|=CanDefenderTarget(u)&&GroundDistance(u.Motion.Position,piece.Position)<V2DefenderSkill(d,"triggerRadius",.85f);
                if(!trigger)continue;piece.Active=false;piece.Time=Clock;d.ChessTriggers++;
                foreach(var u in units)if(CanDefenderTarget(u)&&GroundDistance(piece.Position,u.Motion.Position)<=V2DefenderSkill(d,"explosionRadius",1.5f))TryDefenderDamageUnit(u,d.Settings.Attack*V2DefenderSkill(d,"trapAttackMultiplier",3.2f),CombatDamageType.Arts,sourceDefender:d);
            }
            chessPieces.RemoveAll(c=>c.Time>0&&Clock-c.Time>.5);
        }
        bool AddChess(DefenderState d,Vector3 p,bool consume)
        {
            if(chessPieces.Exists(c=>c.SourceId==d.EntityId&&c.Time==0&&GroundDistance(c.Position,p)<.1f))return false;
            var piece=new SixthCell{SourceId=d.EntityId,Position=p};chessPieces.Add(piece);
            foreach(var other in chessPieces)if(other!=piece&&other.SourceId==piece.SourceId&&other.Time==0&&GroundDistance(p,other.Position)<=V2DefenderSkill(d,"linkDistance",1.1f)&&(Mathf.Abs(p.x-other.Position.x)<.1f||Mathf.Abs(p.z-other.Position.z)<.1f))other.Active=piece.Active=true;
            if(consume&&DefenderSkillActive(d)){int extra=Math.Min((int)V2DefenderSkill(d,"extraPiecesPerPlacement",3),d.Ammo);d.Ammo-=extra;for(int i=1;i<=extra;i++){var child=new SixthCell{SourceId=d.EntityId,Position=p+Vector3.forward*i,Active=true};chessPieces.Add(child);}piece.Active=true;}
            else if(DefenderSkillActive(d))piece.Active=true;
            return true;
        }
        bool ScheduleSixthAttack(DefenderState d,DeployedUnitState primary,float travel)
        {
            if(!IsSixth(d))return false;
            var kind=d.Settings.Skill.Kind;
            if(!DefenderSkillActive(d))return kind==DefenderSkillKind.PramanixSnow;
            if(kind==DefenderSkillKind.WangGambit)return true;
            d.LastActionTime=Clock;d.LastActionHealing=false;d.LastActionRate=d.Settings.Skill.AttackSpeedMultiplier;d.NextAttackTime=Clock+d.Settings.Interval/DefenderAttackRate(d);
            SixthAttackFrom(d,d.Position,primary,travel);
            if(kind==DefenderSkillKind.SilverAshReign&&FindDefender(d.PartnerId) is DefenderState ally&&ally.Alive)SixthAttackFrom(d,ally.Position,primary,travel);
            return true;
        }
        void SixthAttackFrom(DefenderState d,Vector3 origin,DeployedUnitState primary,float travel)
        {
            Vector3 dir=primary.Motion.Position-origin;dir.y=0;dir.Normalize();
            foreach(var u in units)
            {
                if(!CanDefenderTarget(u)||GroundDistance(origin,u.Motion.Position)>DefenderRange(d))continue;
                bool silver=d.Settings.Skill.Kind==DefenderSkillKind.SilverAshReign;
                if(silver){var delta=u.Motion.Position-origin;delta.y=0;float along=Vector3.Dot(delta,dir);if(along<0||(delta-dir*along).magnitude>V2DefenderSkill(d,"lineHalfWidth",.8f))continue;}
                combatShots.Add(new CombatShot(d.EntityId,u.EntityId,true,true,origin,u.Motion.Position,Clock+d.Settings.Windup/d.LastActionRate,travel,DefenderSkillAttack(d)*(silver?1:V2DefenderSkill(d,"areaAttackMultiplier",2.1f)),damageType:silver?CombatDamageType.Physical:CombatDamageType.Arts){Vulnerability=silver?V2DefenderSkill(d,"vulnerability",.25f):0,IgnoreResistance=silver?0:V2DefenderSkill(d,"ignoreResistance",10)});d.SixthHits++;
            }
        }
    }
}
