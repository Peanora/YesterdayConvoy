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
            public bool Success;
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
        public bool CampaignEconomy {get;private set;}
        public bool UnlimitedGold {get;private set;}
        public bool CommissionUsed {get;private set;}
        readonly HashSet<string> lineup=new HashSet<string>();
        public int NextLevelXP=> (CampaignEconomy?CampaignPlan.XP:Experience)[Level];
        public bool CanAfford(int amount)=>UnlimitedGold||Gold>=amount;
        public void EnableCampaign(bool unlimited=false){CampaignEconomy=true;UnlimitedGold=unlimited;}
        public bool SetLineup(IEnumerable<string> ids)
        {
            var next=new HashSet<string>(ids);
            if(next.Count>Level||next.Any(id=>!bench.Any(p=>p.Id==id))||CampaignEconomy&&bench.Count-next.Count>10)return false;
            lineup.Clear();foreach(var id in next)lineup.Add(id);return true;
        }
        public void BeginPreparation(){CommissionUsed=false;}
        internal int TrainingCrewCount=>lineup.Count(CampaignBalance.MudrockCrew);
        internal bool SpendTrainingGold(int amount)
        {
            if(!CampaignEconomy||amount<=0||!CanAfford(amount))return false;
            if(!UnlimitedGold)Gold-=amount;return true;
        }
        bool Fits(List<Piece> pieces)=>pieces.Count<=(CampaignEconomy?10+lineup.Count(id=>pieces.Any(p=>p.Id==id)):10);
        internal string GuaranteedRefreshOffer {get;set;}
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
            if(b.Length>10||o.Length>5||level<1||level>10||gold<0||xp<0||level<10&&xp>=(CampaignEconomy?CampaignPlan.XP:Experience)[level])throw new ArgumentException("Invalid trial preset");
            var reserved=roles.Keys.ToDictionary(id=>id,id=>0);
            foreach(var p in b){if(p.Star<1||p.Star>3||!roles.ContainsKey(p.Id))throw new ArgumentException("Invalid piece");reserved[p.Id]+=Copies(p.Star);}
            foreach(var id in o){if(!roles.ContainsKey(id))throw new ArgumentException("Invalid offer");reserved[id]++;}
            if(reserved.Values.Any(n=>n>9))throw new ArgumentException("Preset exceeds stock");
            bench.Clear();sequence=0;foreach(var p in b){p.Instance=++sequence;bench.Add(p);}
            Array.Clear(offers,0,5);Array.Copy(o,offers,o.Length);
            foreach(var id in roles.Keys)pool[id]=9-reserved[id];
            Level=level;XP=level==10?0:xp;Gold=gold;Locked=false;GuaranteedRefreshOffer=null;rewards.Clear();WinStreak=0;lineup.Clear();CommissionUsed=false;Validate();
        }
        public Result PreviewPurchase(int index)=>PlanPurchase(index,out _);
        Result PlanPurchase(int index,out List<Piece> planned)
        {
            planned=null;
            if(index<0||index>=5||offers[index]==null)return Fail("该位置没有可购买棋子");
            return PlanIncoming(offers[index],out planned);
        }
        Result PlanIncoming(string id,out List<Piece> planned)
        {
            var role=roles[id];
            planned=bench.Select(p=>p.Copy()).ToList();var incoming=new Piece{Instance=sequence+1,Id=id,Star=1};planned.Add(incoming);
            int target=incoming.Instance,star=1;
            for(int tier=1;tier<3;tier++)
            {
                var group=planned.Where(p=>p.Id==id&&p.Star==tier).Take(3).ToArray();
                if(group.Length!=3)continue;
                group[0].Star++;planned.Remove(group[1]);planned.Remove(group[2]);target=group[0].Instance;star=group[0].Star;
            }
            if(!Fits(planned))return Fail("备战席已满；本次不能合成，不扣源石");
            return new Result{Success=true,Id=id,Instance=target,FinalStar=star,Value=role.Cost,Message=star>1?$"{role.Name} 晋升 {star}星":"招募成功"};
        }
        public Result Purchase(int index)
        {
            var result=PlanPurchase(index,out var planned);if(!result.Success)return result;
            if(!CanAfford(result.Value))return Fail("源石不足");
            bench.Clear();bench.AddRange(planned);if(!UnlimitedGold)Gold-=result.Value;offers[index]=null;sequence++;Validate();return result;
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
            bench.RemoveAll(p=>p.Instance==instance);if(!UnlimitedGold)Gold+=quote.Value;pool[quote.Id]+=Copies(quote.FinalStar);Validate();return quote;
        }
        public Result Upgrade()
        {
            if(Level==10)return Fail("已达到最高等级");if(!CanAfford(4))return Fail("升级需要4源石");
            int old=Level;if(!UnlimitedGold)Gold-=4;XP+=4;while(Level<10&&XP>=NextLevelXP){XP-=NextLevelXP;Level++;}if(Level==10)XP=0;
            return new Result{Success=true,Value=Level-old,Message=Level>old?$"等级提升至 {Level} · 编队上限 {Level}":"经验 +4"};
        }
        public Result Refresh(bool free=false)
        {
            if(Locked)return Fail("已锁定，请先解锁");if(!free&&!CanAfford(2))return Fail("刷新需要2源石");
            var next=new Dictionary<string,int>(pool);foreach(var id in offers)if(id!=null)next[id]++;
            var row=Odds[Level-1];
            string guaranteed=GuaranteedRefreshOffer;
            if(guaranteed!=null&&(!roles.TryGetValue(guaranteed,out var guaranteedRole)||next[guaranteed]<=0||row[guaranteedRole.Cost-1]<=0))return Fail("保底卡当前无库存或未解锁，不扣费");
            if(!roles.Values.Any(r=>row[r.Cost-1]>0&&next[r.Id]>0))return Fail("当前等级库存已空，不扣费");
            var drawn=new string[5];int count=0;
            for(int slot=0;slot<5;slot++)
            {
                if(slot==0&&guaranteed!=null){drawn[slot]=guaranteed;next[guaranteed]--;count++;continue;}
                var available=roles.Values.Where(r=>row[r.Cost-1]>0&&next[r.Id]>0).ToArray();if(available.Length==0)break;
                var costs=available.Select(r=>r.Cost).Distinct().OrderBy(c=>c).ToArray();double n=random.NextDouble()*costs.Sum(c=>row[c-1]);int cost=costs.Last();
                foreach(int c in costs){n-=row[c-1];if(n<0){cost=c;break;}}
                var candidates=available.Where(r=>r.Cost==cost).ToArray();int k=random.Next(candidates.Sum(r=>next[r.Id]));var chosen=candidates.Last();
                foreach(var r in candidates){k-=next[r.Id];if(k<0){chosen=r;break;}}
                drawn[slot]=chosen.Id;next[chosen.Id]--;count++;
            }
            foreach(var id in roles.Keys)pool[id]=next[id];Array.Copy(drawn,offers,5);if(!free&&!UnlimitedGold)Gold-=2;GuaranteedRefreshOffer=null;Validate();
            return new Result{Success=true,Message=count<5?$"库存不足，仅刷新{count}张":"刷新完成"};
        }
        public Result Commission(string id)
        {
            if(!CampaignEconomy)return Fail("本模式未开放委托");
            if(CommissionUsed&&!UnlimitedGold)return Fail("本关委托已使用");
            if(!roles.TryGetValue(id,out var role)||role.Cost>3||!bench.Any(p=>p.Id==id)||bench.Any(p=>p.Id==id&&p.Star==3)||Odds[Level-1][role.Cost-1]==0)return Fail("委托需已持有、已解锁且未三星的1–3费角色");
            if(pool[id]<=0)return Fail("无可抽库存；请检查当前商品");
            var result=PlanIncoming(id,out var planned);if(!result.Success)return result;
            result.Value=role.Cost+2;
            if(!CanAfford(result.Value))return Fail("委托金币不足");
            bench.Clear();bench.AddRange(planned);pool[id]--;sequence++;
            if(!UnlimitedGold)Gold-=result.Value;CommissionUsed=true;Validate();return result;
        }
        public sealed class RoundReward
        {
            public string Key; public bool Won;
            public int Base=5,Interest,Streak,Experience,GoldBefore;
            public int Total=>Base+Interest+Streak;
        }
        readonly Dictionary<string,RoundReward> rewards=new Dictionary<string,RoundReward>();
        public int WinStreak {get;private set;}
        public RoundReward SettleRound(string key,bool won,int baseGold=5,int victoryXP=2)
        {
            if(baseGold<0||victoryXP<0)throw new ArgumentOutOfRangeException();
            if(string.IsNullOrEmpty(key))throw new ArgumentException("Missing settlement key");
            if(rewards.TryGetValue(key,out var old))return old;
            WinStreak=won?WinStreak+1:0;
            var receipt=new RoundReward{Key=key,Won=won,Base=baseGold,GoldBefore=Gold,Interest=Math.Min(5,Gold/10),Streak=won?Math.Min(3,Math.Max(0,WinStreak-1)):0,Experience=won?victoryXP:0};
            if(!UnlimitedGold)Gold+=receipt.Total;XP+=receipt.Experience;
            while(Level<10&&XP>=NextLevelXP){XP-=NextLevelXP;Level++;}if(Level==10)XP=0;
            rewards.Add(key,receipt);Validate();return receipt;
        }
        public void Validate()
        {
            if(Gold<0||bench.Count>(CampaignEconomy?10+lineup.Count(id=>bench.Any(p=>p.Id==id)):10)||bench.Select(p=>p.Instance).Distinct().Count()!=bench.Count)throw new InvalidOperationException("Invalid shop state");
            foreach(var id in roles.Keys)
                if(pool[id]<0||pool[id]+offers.Count(o=>o==id)+bench.Where(p=>p.Id==id).Sum(p=>Copies(p.Star))!=9)throw new InvalidOperationException("Stock mismatch: "+id);
        }
    }
}
