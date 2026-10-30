using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    /// <summary>Preparation transactions. A failed purchase never mutates money, cards or stock.</summary>
    public sealed class ShopTrialModel
    {
        public sealed class Role { public string Id, Name; public int Cost; }
        public sealed class Piece
        {
            public int Instance, Star;
            public string Id;
            public Piece Copy() => new Piece { Instance=Instance, Star=Star, Id=Id };
        }
        public sealed class Result
        {
            public bool Success, NeedsConfirmation;
            public string Message, Id;
            public int Instance, FinalStar, Value;
        }
        public static readonly int[][] Odds = {
            new[]{100,0,0,0,0},new[]{100,0,0,0,0},new[]{100,0,0,0,0},new[]{65,25,10,0,0},
            new[]{45,33,20,2,0},new[]{30,40,25,5,0},new[]{19,30,40,10,1},new[]{18,25,32,22,3},
            new[]{15,20,25,30,10},new[]{5,10,20,40,25} };
        public static readonly int[] Experience = {0,2,4,6,10,20,32,48,70,90,0};
        readonly Dictionary<string,Role> roles;
        readonly Dictionary<string,int> pool;
        readonly List<Piece> bench=new List<Piece>();
        readonly string[] offers=new string[5];
        readonly Random random;
        int sequence;
        public int Gold {get;private set;}=80;
        public int Level {get;private set;}=5;
        public int XP {get;private set;}=18;
        public bool Locked {get;set;}
        public IReadOnlyList<Piece> Bench=>bench.AsReadOnly();
        public IReadOnlyList<string> Offers=>Array.AsReadOnly(offers);
        public IReadOnlyCollection<Role> Roles=>roles.Values;
        public int Available(string id)=>pool[id];
        public Role Definition(string id)=>roles[id];
        public static int Copies(int star)=>star==3?9:star==2?3:1;
        static Result Fail(string text)=>new Result {Message=text};

        public ShopTrialModel(IEnumerable<Role> definitions,int seed=73191)
        {
            roles=definitions.ToDictionary(r=>r.Id,StringComparer.Ordinal);
            if(roles.Count==0||roles.Values.Any(r=>r.Cost<1||r.Cost>5))throw new ArgumentException("Invalid shop roles");
            pool=roles.Keys.ToDictionary(id=>id,id=>9);random=new Random(seed);
        }
        public void Preset(IEnumerable<Piece> pieces,IEnumerable<string> cards,int level=5,int xp=18,int gold=80)
        {
            var b=pieces.Select(p=>p.Copy()).ToArray();var o=cards.ToArray();
            if(b.Length>10||o.Length>5||level<1||level>10||gold<0||xp<0||level<10&&xp>=Experience[level])throw new ArgumentException("Invalid trial preset");
            var reserved=roles.Keys.ToDictionary(id=>id,id=>0);
            foreach(var p in b){if(p.Star<1||p.Star>3||!roles.ContainsKey(p.Id))throw new ArgumentException("Invalid piece");reserved[p.Id]+=Copies(p.Star);}
            foreach(var id in o){if(!roles.ContainsKey(id))throw new ArgumentException("Invalid offer");reserved[id]++;}
            if(reserved.Values.Any(n=>n>9))throw new ArgumentException("Preset exceeds stock");
            bench.Clear();sequence=0;foreach(var p in b){p.Instance=++sequence;bench.Add(p);}
            Array.Clear(offers,0,5);Array.Copy(o,offers,o.Length);
            foreach(var id in roles.Keys)pool[id]=9-reserved[id];
            Level=level;XP=level==10?0:xp;Gold=gold;Locked=false;Validate();
        }
        public Result Purchase(int index,bool confirm=false)
        {
            if(index<0||index>=5||offers[index]==null)return Fail("该位置没有可购买棋子");
            string id=offers[index];var role=roles[id];if(Gold<role.Cost)return Fail("源石不足");
            var planned=bench.Select(p=>p.Copy()).ToList();var incoming=new Piece{Instance=sequence+1,Id=id,Star=1};planned.Add(incoming);
            int target=incoming.Instance,star=1;
            for(int tier=1;tier<3;tier++)
            {
                var group=planned.Where(p=>p.Id==id&&p.Star==tier).Take(3).ToArray();
                if(group.Length!=3)continue;
                group[0].Star++;planned.Remove(group[1]);planned.Remove(group[2]);target=group[0].Instance;star=group[0].Star;
            }
            if(planned.Count>10)return Fail("备战席已满；本次不能合成，不扣源石");
            if(bench.Count==10&&!confirm)return new Result{NeedsConfirmation=true,Message=$"备战席已满。花费{role.Cost}源石，合成{star}星后占用{planned.Count}/10格？"};
            bench.Clear();bench.AddRange(planned);Gold-=role.Cost;offers[index]=null;sequence++;Validate();
            return new Result{Success=true,Id=id,Instance=target,FinalStar=star,Value=role.Cost,Message=star>1?$"{role.Name} 晋升 {star}星":"招募成功"};
        }
        public Result Quote(int instance)
        {
            var p=bench.Find(c=>c.Instance==instance);if(p==null)return Fail("棋子不存在");
            int value=roles[p.Id].Cost*Copies(p.Star)-(p.Star==1?0:1);
            return new Result{Success=true,Id=p.Id,Instance=instance,FinalStar=p.Star,Value=value,Message=roles[p.Id].Name};
        }
        public Result Sell(int instance)
        {
            var quote=Quote(instance);if(!quote.Success)return quote;
            bench.RemoveAll(p=>p.Instance==instance);Gold+=quote.Value;pool[quote.Id]+=Copies(quote.FinalStar);Validate();return quote;
        }
        public Result Upgrade()
        {
            if(Level==10)return Fail("已达到最高等级");if(Gold<4)return Fail("升级需要4源石");
            int old=Level;Gold-=4;XP+=4;while(Level<10&&XP>=Experience[Level]){XP-=Experience[Level];Level++;}if(Level==10)XP=0;
            return new Result{Success=true,Value=Level-old,Message=Level>old?$"等级提升至 {Level} · 编队上限 {Level}":"经验 +4"};
        }
        public Result Refresh()
        {
            if(Locked)return Fail("已锁定，请先解锁");if(Gold<2)return Fail("刷新需要2源石");
            var next=new Dictionary<string,int>(pool);foreach(var id in offers)if(id!=null)next[id]++;
            var row=Odds[Level-1];
            if(!roles.Values.Any(r=>row[r.Cost-1]>0&&next[r.Id]>0))return Fail("当前等级库存已空，不扣费");
            var drawn=new string[5];int count=0;
            for(int slot=0;slot<5;slot++)
            {
                var available=roles.Values.Where(r=>row[r.Cost-1]>0&&next[r.Id]>0).ToArray();if(available.Length==0)break;
                var costs=available.Select(r=>r.Cost).Distinct().OrderBy(c=>c).ToArray();double n=random.NextDouble()*costs.Sum(c=>row[c-1]);int cost=costs.Last();
                foreach(int c in costs){n-=row[c-1];if(n<0){cost=c;break;}}
                var candidates=available.Where(r=>r.Cost==cost).ToArray();int k=random.Next(candidates.Sum(r=>next[r.Id]));var chosen=candidates.Last();
                foreach(var r in candidates){k-=next[r.Id];if(k<0){chosen=r;break;}}
                drawn[slot]=chosen.Id;next[chosen.Id]--;count++;
            }
            foreach(var id in roles.Keys)pool[id]=next[id];Array.Copy(drawn,offers,5);Gold-=2;Validate();
            return new Result{Success=true,Message=count<5?$"库存不足，仅刷新{count}张":"刷新完成"};
        }
        public void Validate()
        {
            if(Gold<0||bench.Count>10||bench.Select(p=>p.Instance).Distinct().Count()!=bench.Count)throw new InvalidOperationException("Invalid shop state");
            foreach(var id in roles.Keys)
                if(pool[id]<0||pool[id]+offers.Count(o=>o==id)+bench.Where(p=>p.Id==id).Sum(p=>Copies(p.Star))!=9)throw new InvalidOperationException("Stock mismatch: "+id);
        }
    }
}
