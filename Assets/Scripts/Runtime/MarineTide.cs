using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class MarineCellState
    {
        public Vector2Int Cell {get;internal set;}
        public Vector3 Position {get;internal set;}
        public double BornAt {get;internal set;}
        public bool IsSeed {get;internal set;}
    }

    public sealed partial class ManualBattle
    {
        readonly List<MarineCellState> marineCells=new List<MarineCellState>();
        readonly Dictionary<Vector2Int,MarineCellState> marineCellLookup=new Dictionary<Vector2Int,MarineCellState>();
        readonly HashSet<Vector2Int> marineTerrain=new HashSet<Vector2Int>();
        readonly HashSet<string> marineSeededKinds=new HashSet<string>(StringComparer.Ordinal);
        bool marineTerrainConfigured;
        Vector2 marineGridOffset;
        double marineSpreadElapsed;
        static readonly Vector2Int[] MarineNeighbours={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};

        // Cell keys are relative to the authored grid; their world centres include its x/z offset.
        public IReadOnlyList<MarineCellState> MarineCells=>marineCells.AsReadOnly();
        public bool MarineActive=>synergyEnabled&&MarineTier>0&&Phase==BattlePhase.Running&&units.Any(IsLivingMarine);
        public int MarineTileLimit=>(int)V2Synergy("marine",MarineTier,"tileLimit",MarineTier==4?16:MarineTier==2?8:0);
        public float MarineSpreadInterval=>V2Synergy("marine",MarineTier,"spreadIntervalSeconds",MarineTier==4?4:6);
        public double MarineSpreadRemaining=>MarineCells.Count==0?MarineSpreadInterval:Math.Max(0,MarineSpreadInterval-marineSpreadElapsed);
        public int MarineSeededKinds=>marineSeededKinds.Count;
        public int MarineSpreadCount {get;private set;}
        public float MarineGroundNeuralDealt {get;private set;}
        public float MarineBonusNeuralDealt {get;private set;}

        Vector2Int MarineCell(Vector3 position)=>new Vector2Int(Mathf.FloorToInt(position.x-marineGridOffset.x+.5f),Mathf.FloorToInt(position.z-marineGridOffset.y+.5f));
        Vector3 MarineWorldPosition(Vector2Int cell,float height=0)=>new Vector3(cell.x+marineGridOffset.x,height,cell.y+marineGridOffset.y);
        bool IsLivingMarine(DeployedUnitState unit)=>unit!=null&&unit.Active&&!unit.IsReviving&&!unit.SynergySupport&&!StrongholdInTransit(unit.EntityId)&&Tag(unit.CardId,"海嗣");

        /// <summary>Explicit authored floor cells; configuration survives a restart, runtime growth does not.</summary>
        public void ConfigureMarineTerrain(IEnumerable<Vector3> groundCells,Vector2 gridOffset=default)
        {
            if(Phase!=BattlePhase.Ready||units.Count>0)throw new InvalidOperationException("Configure marine terrain before deployment.");
            if(groundCells==null)throw new ArgumentNullException(nameof(groundCells));
            if(float.IsNaN(gridOffset.x)||float.IsInfinity(gridOffset.x)||float.IsNaN(gridOffset.y)||float.IsInfinity(gridOffset.y))
                throw new ArgumentException("Marine grid offset must be finite.",nameof(gridOffset));
            var cells=groundCells.ToArray();
            if(cells.Any(p=>float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.z)))
                throw new ArgumentException("Marine terrain must contain finite world positions.",nameof(groundCells));
            marineGridOffset=gridOffset;marineTerrain.Clear();
            foreach(var p in cells)marineTerrain.Add(MarineCell(p));
            marineTerrainConfigured=true;ResetMarineTide();
        }

        bool MarineGround(Vector2Int cell)
        {
            if(marineTerrainConfigured)return marineTerrain.Contains(cell);
            // A model with no authored terrain only knows its supplied waypoints, never an infinite plane.
            return lanes.Any(l=>l.Points.Any(p=>MarineCell(p)==cell));
        }

        void ResetMarineTide()
        {
            marineCells.Clear();marineCellLookup.Clear();marineSeededKinds.Clear();marineSpreadElapsed=0;
            MarineSpreadCount=0;MarineGroundNeuralDealt=MarineBonusNeuralDealt=0;
        }

        void AddMarineCell(Vector2Int cell,bool seed)
        {
            if(marineCellLookup.TryGetValue(cell,out var existing)){if(seed)existing.IsSeed=true;return;}
            var state=new MarineCellState{Cell=cell,Position=MarineWorldPosition(cell,.065f),BornAt=Clock,IsSeed=seed};
            marineCells.Add(state);marineCellLookup.Add(cell,state);
        }

        void OnMarineContact(DeployedUnitState unit)
        {
            if(!synergyEnabled||MarineTier==0||Phase!=BattlePhase.Running||IsPaused||!IsLivingMarine(unit)||marineSeededKinds.Contains(unit.CardId))return;
            var cell=MarineCell(unit.Motion.Position);
            if(!MarineGround(cell))return;
            // Growth reserves one cell for each not-yet-contacted kind, so a new root never displaces old ground.
            if(!marineCellLookup.ContainsKey(cell)&&marineCells.Count>=MarineTileLimit)return;
            marineSeededKinds.Add(unit.CardId);AddMarineCell(cell,true);
        }

        public float MarineNeuralMultiplier(DeployedUnitState unit)=>MarineActive&&IsLivingMarine(unit)&&marineCellLookup.ContainsKey(MarineCell(unit.Motion.Position))?V2Synergy("marine",MarineTier,"nativeNeuralMultiplier",MarineTier==4?2:1.5f):1;

        float MarineCandidateDistance(Vector2Int cell,bool defendersFirst)
        {
            var p=MarineWorldPosition(cell);
            if(defendersFirst)
                return defenders.Where(d=>d.Alive&&!d.HighGround).Select(d=>GroundDistance(p,d.Position)).DefaultIfEmpty(float.MaxValue).Min();
            return units.Where(IsLivingMarine).Select(u=>GroundDistance(p,u.Motion.Position)).DefaultIfEmpty(float.MaxValue).Min();
        }

        void SpreadMarineTide()
        {
            int unseeded=synergyStars.Keys.Count(id=>Tag(id,"海嗣")&&!marineSeededKinds.Contains(id));
            int available=Math.Max(0,MarineTileLimit-unseeded-marineCells.Count);
            if(available==0)return;
            // Snapshot the frontier: a tick grows one cardinal ring, never recursively crosses the whole map.
            var next=marineCells.SelectMany(c=>MarineNeighbours.Select(n=>c.Cell+n)).Distinct()
                .Where(c=>MarineGround(c)&&!marineCellLookup.ContainsKey(c))
                .OrderBy(c=>MarineCandidateDistance(c,true)).ThenBy(c=>MarineCandidateDistance(c,false))
                .ThenBy(c=>c.y).ThenBy(c=>c.x).Take(available).ToArray();
            foreach(var cell in next){AddMarineCell(cell,false);MarineSpreadCount++;}
        }

        void StepMarineTide(float step)
        {
            if(!MarineActive||marineCells.Count==0)return;
            marineSpreadElapsed+=step;
            while(marineSpreadElapsed+1e-6>=MarineSpreadInterval)
            {marineSpreadElapsed=Math.Max(0,marineSpreadElapsed-MarineSpreadInterval);SpreadMarineTide();}
            foreach(var defender in defenders)
                if(defender.Alive&&!defender.HighGround&&marineCellLookup.ContainsKey(MarineCell(defender.Position)))
                    MarineGroundNeuralDealt+=ApplyNeuralDamage(defender,V2Synergy("marine",MarineTier,"groundNeuralPerSecond",3)*step);
        }
    }
}
