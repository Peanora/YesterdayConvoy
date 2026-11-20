using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    // Preparation positions belong to card instances. Combat keeps its compact, kind-based loadout.
    public sealed class ShopSeatLayout
    {
        public const int ReserveCapacity = 10;

        public readonly struct Seat
        {
            public readonly bool Lineup;
            public readonly int Index;
            public Seat(bool lineup,int index){Lineup=lineup;Index=index;}
        }
        readonly Dictionary<int,Seat> seats=new Dictionary<int,Seat>();
        readonly Dictionary<int,string> lastIds=new Dictionary<int,string>();
        readonly bool overflowEnabled;

        public ShopSeatLayout(bool overflowEnabled = false) => this.overflowEnabled = overflowEnabled;
        public int OverflowCount => seats.Values.Count(seat => !seat.Lineup && seat.Index >= ReserveCapacity);
        public bool ReserveFull => FirstEmpty(false, ReserveCapacity) < 0;

        public Seat? For(int instance)=>seats.TryGetValue(instance,out var seat)?seat:(Seat?)null;
        public bool InLineup(int instance)=>seats.TryGetValue(instance,out var seat)&&seat.Lineup;
        public int At(bool lineup,int index)=>seats.Where(p=>p.Value.Lineup==lineup&&p.Value.Index==index).Select(p=>p.Key).FirstOrDefault();
        public int FirstEmpty(bool lineup,int capacity)=>Enumerable.Range(0,capacity).Where(i=>At(lineup,i)==0).DefaultIfEmpty(-1).First();
        public IEnumerable<ShopTrialModel.Piece> Lineup(IReadOnlyList<ShopTrialModel.Piece> pieces)
            =>pieces.Where(p=>InLineup(p.Instance)).OrderBy(p=>seats[p.Instance].Index);

        public void Synchronize(IReadOnlyList<ShopTrialModel.Piece> pieces,IEnumerable<string> ids,int level)
        {
            var wanted=ids.Distinct().Where(id=>pieces.Any(p=>p.Id==id)).ToArray();
            if(wanted.Length>level||!overflowEnabled&&pieces.Count-wanted.Length>ReserveCapacity)
                throw new InvalidOperationException("Invalid preparation capacity");
            var previous=seats.ToDictionary(p=>p.Key,p=>p.Value);
            var oldByKind=previous.Where(p=>p.Value.Lineup&&lastIds.ContainsKey(p.Key))
                .ToDictionary(p=>lastIds[p.Key],p=>p.Value.Index,StringComparer.Ordinal);
            seats.Clear();
            foreach(string id in wanted)
            {
                var candidates=pieces.Where(p=>p.Id==id).ToArray();
                var chosen=candidates.FirstOrDefault(p=>previous.TryGetValue(p.Instance,out var s)&&s.Lineup&&lastIds.TryGetValue(p.Instance,out var oldId)&&oldId==p.Id)
                    ??candidates.OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).First();
                int slot=oldByKind.TryGetValue(id,out int old)&&old<level&&At(true,old)==0?old:FirstEmpty(true,level);
                seats.Add(chosen.Instance,new Seat(true,slot));
            }
            // Keep every surviving reserve's chosen position before assigning new cards/material survivors.
            foreach(var piece in pieces.Where(p=>!seats.ContainsKey(p.Instance)))
                if(previous.TryGetValue(piece.Instance,out var old)&&!old.Lineup&&old.Index<ReserveCapacity&&At(false,old.Index)==0&&lastIds.TryGetValue(piece.Instance,out var oldId)&&oldId==piece.Id)
                    seats.Add(piece.Instance,old);

            // A vacancy goes to the oldest overflow first. Fresh cards follow in acquisition order.
            var waiting = pieces.Where(piece => !seats.ContainsKey(piece.Instance)).OrderBy(piece =>
                previous.TryGetValue(piece.Instance, out var old) && !old.Lineup && old.Index >= ReserveCapacity &&
                lastIds.TryGetValue(piece.Instance, out var oldId) && oldId == piece.Id ? old.Index : int.MaxValue);
            int overflow = ReserveCapacity;
            foreach (var piece in waiting)
            {
                int empty = FirstEmpty(false, ReserveCapacity);
                seats.Add(piece.Instance, new Seat(false, empty >= 0 ? empty : overflow++));
            }
            lastIds.Clear();foreach(var piece in pieces)lastIds.Add(piece.Instance,piece.Id);
        }

        internal bool CanTarget(bool lineup, int index, int level) => index >= 0 &&
            (lineup ? index < level : index < ReserveCapacity || overflowEnabled && ReserveFull && index <= ReserveCapacity + OverflowCount);

        public bool TryMove(IReadOnlyList<ShopTrialModel.Piece> pieces,int level,int instance,bool lineup,int index,out string failure)
        {
            failure=null;
            if(!seats.TryGetValue(instance,out var origin)||!CanTarget(lineup,index,level))
            {failure="该席位尚未解锁";return false;}
            int target=At(lineup,index);
            if(target==instance)return true;
            var next=seats.ToDictionary(p=>p.Key,p=>p.Value);
            next[instance]=new Seat(lineup,index);
            if(target!=0)next[target]=origin;
            var selected=pieces.Where(p=>next[p.Instance].Lineup).ToArray();
            if(selected.Select(p=>p.Id).Distinct().Count()!=selected.Length)
            {failure="同名角色只能有一张出战；可与其直接交换";return false;}
            if(!overflowEnabled&&pieces.Count-selected.Length>ReserveCapacity)
            {failure="备战席已满，无法移出编队";return false;}
            seats.Clear();foreach(var item in next)seats.Add(item.Key,item.Value);
            return true;
        }
        public void CompactLineup()
        {
            var ordered=seats.Where(p=>p.Value.Lineup).OrderBy(p=>p.Value.Index).Select(p=>p.Key).ToArray();
            for(int i=0;i<ordered.Length;i++)seats[ordered[i]]=new Seat(true,i);
        }
    }
}
