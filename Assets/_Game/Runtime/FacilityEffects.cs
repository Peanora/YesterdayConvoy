using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum FacilityKind { Altar, Gramophone, Reeds, Pillar }
    public enum FacilityOwner { Neutral, Attackers, Defenders }
    public readonly struct FacilitySettings
    {
        public readonly FacilityKind Kind;
        public readonly Vector3 Position;
        public readonly float Radius, Interval, Power, CaptureTime;
        public readonly bool InitiallyBurning;
        public FacilitySettings(FacilityKind kind,Vector3 position,float radius=2.5f,float interval=6,float power=30,float captureTime=10,bool initiallyBurning=false)
        {
            if(!Enum.IsDefined(typeof(FacilityKind),kind))throw new ArgumentException("Unknown facility");
            foreach(float v in new[]{position.x,position.y,position.z,radius,interval,power,captureTime})
                if(float.IsNaN(v)||float.IsInfinity(v))throw new ArgumentException("Non-finite facility value");
            if(radius<=0||interval<=0||power<=0||captureTime<=0)throw new ArgumentException("Facility values must be positive");
            Kind=kind;Position=position;Radius=radius;Interval=interval;Power=power;CaptureTime=captureTime;InitiallyBurning=initiallyBurning;
        }
    }
    public sealed class FacilityState
    {
        public FacilitySettings Settings { get; }
        public FacilityOwner Owner {get;internal set;}
        public float Capture {get;internal set;}
        public bool Burning {get;internal set;}
        public bool Collapsed {get;internal set;}
        public Vector3 FallDirection {get;internal set;}
        public int Activations {get;internal set;}
        public double LastActivation {get;internal set;}=double.NegativeInfinity;
        public double IgnitedAt {get;internal set;}=double.NegativeInfinity;
        internal double NextAction,NextHeal,NextSpread,SafeUntil;
        internal float Extinguishing;
        internal FacilityState(FacilitySettings settings){Settings=settings;Reset();}
        internal void Reset()
        {
            Collapsed=false;FallDirection=Vector3.zero;
            Owner=FacilityOwner.Neutral;Capture=Extinguishing=0;Activations=0;LastActivation=double.NegativeInfinity;
            Burning=Settings.InitiallyBurning;IgnitedAt=Burning?0:double.NegativeInfinity;
            NextAction=Settings.Kind==FacilityKind.Reeds?1:Settings.Interval;NextHeal=5;NextSpread=15;SafeUntil=0;
        }
    }
    public sealed partial class ManualBattle
    {
        readonly List<FacilityState> facilities=new List<FacilityState>();
        readonly HashSet<Vector2Int> sealedTiles=new HashSet<Vector2Int>();
        readonly Dictionary<string,double> nextSeal=new Dictionary<string,double>(),nextCapture=new Dictionary<string,double>();
        Vector3[] sealCandidates=Array.Empty<Vector3>();
        uint sealRandom=1777;
        public IReadOnlyList<FacilityState> Facilities=>facilities.AsReadOnly();
        public IEnumerable<Vector2Int> SealedTiles=>sealedTiles;
        public int SealedTileCount=>sealedTiles.Count;
        public int AltarHits {get;private set;}
        public int GramophoneHits {get;private set;}
        public int AttackerGramophoneHits {get;private set;}
        public float FacilityHealing {get;private set;}
        public int BurnBursts {get;private set;}
        public int Ignitions {get;private set;}
        public void ConfigureFacilities(IEnumerable<FacilitySettings> settings,IEnumerable<Vector3> deployableTiles=null)
        {
            if(Phase!=BattlePhase.Ready||units.Count>0||!EngagementEnabled)throw new InvalidOperationException("Configure facilities before battle");
            var snapshot=(settings??throw new ArgumentNullException(nameof(settings))).ToArray();
            var cells=(deployableTiles??Array.Empty<Vector3>()).ToArray();
            foreach(var s in snapshot) _=new FacilitySettings(s.Kind,s.Position,s.Radius,s.Interval,s.Power,s.CaptureTime,s.InitiallyBurning);
            foreach(var p in cells)if(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.z)||float.IsInfinity(p.z))throw new ArgumentException("Invalid tile");
            facilities.Clear();facilities.AddRange(snapshot.Select(s=>new FacilityState(s)));sealCandidates=cells;ResetFacilities();
        }
        static Vector2Int Cell(Vector3 p)=>new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.z));
        public bool IsTileSealed(Vector3 p)=>sealedTiles.Contains(Cell(p));
        static bool IsRole(DeployedUnitState u,string id)=>u.UnitId.StartsWith(id+"_",StringComparison.Ordinal)||u.UnitId==id;
        void ResetFacilities()
        {
            foreach(var f in facilities)f.Reset();sealedTiles.Clear();rubbleTiles.Clear();nextSeal.Clear();nextCapture.Clear();sealRandom=1777;
            AltarHits=GramophoneHits=AttackerGramophoneHits=BurnBursts=0;Ignitions=facilities.Count(f=>f.Settings.Kind==FacilityKind.Reeds&&f.Burning);FacilityHealing=0;
        }
        void TrueDamage(DeployedUnitState u,float amount)
        {
            if(!u.Active||u.IsReviving)return;
            ApplyUnitHealthDamage(u,amount);
        }
        void TrueDamage(DefenderState d,float amount)
        {
            if(!d.Alive)return;d.Health=Math.Max(0,d.Health-amount);d.LastHitTime=Clock;
            if(!d.Alive){d.DeathTime=Clock;d.TargetId=null;}
        }
        public bool TryIgniteReeds(Vector3 position,float radius=1.5f)
        {
            if(float.IsNaN(radius)||float.IsInfinity(radius)||radius<=0||float.IsNaN(position.x)||float.IsNaN(position.z)||float.IsInfinity(position.x)||float.IsInfinity(position.z))throw new ArgumentException("Invalid ignition range");
            if(Phase!=BattlePhase.Running||IsPaused)return false;
            bool lit=false;
            foreach(var f in facilities)
                if(f.Settings.Kind==FacilityKind.Reeds&&!f.Burning&&Clock>=f.SafeUntil&&GroundDistance(position,f.Settings.Position)<=radius)
                {f.Burning=true;f.IgnitedAt=Clock;f.NextSpread=Clock+15;f.NextAction=Clock+1;f.Extinguishing=0;Ignitions++;lit=true;}
            return lit;
        }
        public bool DefenderCamouflaged(DefenderState d)=>d!=null&&d.Alive&&d.BlockedCount==0&&facilities.Exists(f=>f.Settings.Kind==FacilityKind.Reeds&&!f.Burning&&Cell(f.Settings.Position)==Cell(d.Position));
        void StepFacilities(float step)
        {
            StepSealing();
            foreach(var f in facilities)
            {
                var s=f.Settings;
                if(s.Kind==FacilityKind.Altar)
                {
                    if(Clock+1e-6<f.NextAction)continue;f.NextAction=Clock+s.Interval;Activate(f);
                    foreach(var u in units)
                    {
                        if(!u.Active||u.IsReviving||GroundDistance(u.Motion.Position,s.Position)>s.Radius)continue;
                        if(IsRole(u,"M19")){u.AltarEmpowered=true;continue;}
                        if(IsRole(u,"M20"))continue;
                        TrueDamage(u,s.Power);AltarHits++;
                    }
                    foreach(var d in defenders)if(d.Alive&&!DefenderCamouflaged(d)&&GroundDistance(d.Position,s.Position)<=s.Radius){TrueDamage(d,s.Power);AltarHits++;}
                }
                else if(s.Kind==FacilityKind.Gramophone)StepGramophone(f,step);
                else if(s.Kind==FacilityKind.Reeds&&f.Burning)
                {
                    bool occupied=defenders.Exists(d=>d.Alive&&Cell(d.Position)==Cell(s.Position));
                    if(occupied)
                    {
                        f.Extinguishing+=step;
                        if(f.Extinguishing>=20){f.Burning=false;f.SafeUntil=Clock+5;f.Extinguishing=0;continue;}
                    }
                    if(!occupied&&Clock+1e-6>=f.NextSpread)
                    {f.NextSpread=Clock+15;TryIgniteReeds(s.Position,1.01f);}
                    if(Clock+1e-6<f.NextAction)continue;f.NextAction=Clock+1;Activate(f);
                    foreach(var d in defenders)
                    {
                        if(!d.Alive||Cell(d.Position)!=Cell(s.Position))continue;
                        DamageDefender(d,s.Power,CombatDamageType.Arts);
                        ApplyBurn(d,s.Power);
                    }
                }
            }
        }
        void Activate(FacilityState f){f.Activations++;f.LastActivation=Clock;}
        void StepSealing()
        {
            if(!CombatSettings.Effects.FrostNova||sealCandidates.Length==0)return;
            foreach(var u in units)
            {
                if(!u.Active||u.IsReviving)continue;
                if(!nextSeal.TryGetValue(u.EntityId,out double due))due=u.SpawnTime+30;
                if(Clock+1e-6<due)continue;nextSeal[u.EntityId]=Clock+30;
                var options=sealCandidates.Where(p=>!IsTileSealed(p)&&GroundDistance(p,u.Motion.Position)<=2).ToList();
                for(int i=0;i<2&&options.Count>0;i++)
                {
                    sealRandom=sealRandom*1664525+1013904223;int index=(int)(sealRandom%(uint)options.Count);
                    var p=options[index];options.RemoveAt(index);sealedTiles.Add(Cell(p));
                    foreach(var d in defenders)if(d.Alive&&Cell(d.Position)==Cell(p))TrueDamage(d,d.Health);
                }
            }
        }
        void StepGramophone(FacilityState f,float step)
        {
            var s=f.Settings;
            int attackers=units.Count(u=>u.Active&&!u.IsReviving&&GroundDistance(u.Motion.Position,s.Position)<=1.5f);
            int guards=defenders.Count(d=>d.Alive&&GroundDistance(d.Position,s.Position)<=1.5f);
            f.Capture=Mathf.Clamp(f.Capture+(attackers-guards)*step,-s.CaptureTime,s.CaptureTime);
            if(f.Capture>=s.CaptureTime-1e-5)f.Owner=FacilityOwner.Attackers;
            if(f.Capture<=-s.CaptureTime+1e-5)f.Owner=FacilityOwner.Defenders;
            foreach(var u in units)
            {
                if(!u.Active||u.IsReviving||!CombatSettings.Effects.Mudrock||GroundDistance(u.Motion.Position,s.Position)>2.5f)continue;
                if(!nextCapture.TryGetValue(u.EntityId,out double due))due=u.SpawnTime+20;
                if(Clock<due)continue;nextCapture[u.EntityId]=Clock+20;f.Capture=s.CaptureTime;f.Owner=FacilityOwner.Attackers;Activate(f);
            }
            if(f.Owner==FacilityOwner.Neutral)return;
            if(Clock+1e-6>=f.NextAction)
            {
                f.NextAction=Clock+1;
                if(f.Owner==FacilityOwner.Attackers)
                {
                    var d=defenders.Where(d=>d.Alive&&GroundDistance(d.Position,s.Position)<=s.Radius).OrderBy(d=>d.Health).FirstOrDefault();
                    if(d!=null){DamageDefender(d,d.MaxHealth*.05f,CombatDamageType.Arts);GramophoneHits++;AttackerGramophoneHits++;Activate(f);}
                }
                else
                {
                    var u=units.Where(u=>u.Active&&!u.IsReviving&&GroundDistance(u.Motion.Position,s.Position)<=s.Radius).OrderBy(u=>u.Health/u.MaxHealth).FirstOrDefault();
                    if(u!=null){TryDamageUnit(u.EntityId,u.MaxHealth*.05f,CombatDamageType.Arts);GramophoneHits++;Activate(f);}
                }
            }
            if(Clock+1e-6<f.NextHeal)return;f.NextHeal=Clock+5;
            if(f.Owner==FacilityOwner.Attackers)
            {
                var u=units.Where(u=>u.Active&&!u.IsReviving&&u.Health<u.MaxHealth&&GroundDistance(u.Motion.Position,s.Position)<=s.Radius).OrderBy(u=>u.Health/u.MaxHealth).FirstOrDefault();
                if(u!=null){float before=u.Health;HealUnit(u,u.MaxHealth*.1f);FacilityHealing+=u.Health-before;}
            }
            else
            {
                var d=defenders.Where(d=>d.Alive&&d.Health<d.MaxHealth&&GroundDistance(d.Position,s.Position)<=s.Radius).OrderBy(d=>d.Health).FirstOrDefault();
                if(d!=null){float before=d.Health;d.Health=Math.Min(d.MaxHealth,d.Health+d.MaxHealth*.1f);FacilityHealing+=d.Health-before;}
            }
        }
    }
}
