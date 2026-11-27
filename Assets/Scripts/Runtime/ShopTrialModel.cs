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
        public sealed class RewardReservation
        {
            public int Token {get;}
            public string Id {get;}
            internal RewardReservation(int token,string id){Token=token;Id=id;}
        }
        public static readonly int[][] Odds = {
            new[]{100,0,0,0,0},new[]{100,0,0,0,0},new[]{100,0,0,0,0},new[]{65,25,10,0,0},
            new[]{45,33,20,2,0},new[]{30,40,25,5,0},new[]{19,30,40,10,1},new[]{18,25,32,22,3},
            new[]{15,20,25,30,10},new[]{5,10,20,40,25} };
        public static readonly int[] Experience = {0,2,4,6,10,20,32,48,70,90,0};
        static readonly int[] FormationExperience={0,2,4,6,10,20,36,56,80,100,0};
        readonly Dictionary<string,Role> roles;
        readonly Dictionary<string,int> pool;
        readonly List<Piece> bench=new List<Piece>();
        readonly string[] offers=new string[5];
        readonly Random random;
        readonly Dictionary<int,RewardReservation> rewardReservations=new Dictionary<int,RewardReservation>();
        int sequence,reservationSequence;
        public bool CampaignEconomy {get;private set;}
        public bool JourneyEconomy {get;private set;}
        public bool UnlimitedGold {get;private set;}
        public bool CommissionUsed {get;private set;}
        public bool FormationEconomy {get;private set;}
        public int ExtraPopulation {get;set;}
        public int PopulationCap=>Math.Min(FormationEconomy?14:11,Level+ExtraPopulation);
        public int RefreshPrice {get;set;}=2;
        public int PurchasedXP {get;set;}=4;
        public int FreeRefreshes {get;set;}
        public int InterestDivisor {get;set;}=10;
        public int InterestCap {get;set;}=5;
        public int PendingTalulahChoice {get;private set;}
        public bool TalulahLocked {get;private set;}
        public bool TalulahEnhanced {get;private set;}
        public string TalulahId {get;private set;}
        public Func<string,double> RecruitmentWeight {get;set;}
        internal Func<string,int> PurchaseDiscount;
        internal Action<string> PurchaseCompleted;
        internal Action ManualRefreshCompleted;
        public int Price(string id)=>Math.Max(0,roles[id].Cost-Math.Max(0,PurchaseDiscount?.Invoke(id)??0));
        public bool TransactionBlocked=>FormationEconomy&&PendingTalulahChoice!=0;
        public void EnableFormationEconomy(string talulahId="CARD_TALULAH_TRIAL")=>EnableFormationEconomy(false,talulahId);
        internal void EnableFormationEconomy(bool unlimited,string talulahId="CARD_TALULAH_TRIAL")
        {FormationEconomy=true;JourneyEconomy=false;CampaignEconomy=false;UnlimitedGold=unlimited;TalulahId=roles.ContainsKey(talulahId)?talulahId:null;}
        readonly HashSet<string> lineup=new HashSet<string>();
        readonly HashSet<int> lineupInstances=new HashSet<int>();
        public int NextLevelXP=>FormationEconomy?FormationExperience[Level]:JourneyEconomy?CampaignJourneyEconomy.RequiredXP(Level):(CampaignEconomy?CampaignPlan.XP:Experience)[Level];
        public bool CanAfford(int amount)=>UnlimitedGold||Gold>=amount;
        public void EnableCampaign(bool unlimited=false){CampaignEconomy=true;JourneyEconomy=false;UnlimitedGold=unlimited;}
        public void EnableJourneyEconomy(bool unlimited=false){EnableCampaign(unlimited);JourneyEconomy=true;}
        public bool IsLineupInstance(int instance)=>lineupInstances.Contains(instance);
        public bool SetLineup(IEnumerable<string> ids,IEnumerable<int> preferredInstances=null)
        {
            var next=new HashSet<string>(ids);
            if(next.Count>PopulationCap||next.Any(id=>!bench.Any(p=>p.Id==id))||
                !FormationEconomy&&bench.Count-next.Count>ShopSeatLayout.ReserveCapacity)return false;
            var preferred=new HashSet<int>(preferredInstances??Enumerable.Empty<int>());
            var selected=bench.Where(p=>preferred.Contains(p.Instance)).ToArray();
            if(preferredInstances!=null&&(preferred.Count!=next.Count||selected.Length!=preferred.Count||selected.Any(p=>!next.Contains(p.Id))||selected.Select(p=>p.Id).Distinct().Count()!=next.Count))return false;
            lineup.Clear();foreach(var id in next)lineup.Add(id);
            lineupInstances.Clear();foreach(int instance in preferred)lineupInstances.Add(instance);return true;
        }
        public void BeginPreparation(){CommissionUsed=false;}
        int ReserveCount(IReadOnlyList<Piece> pieces) => pieces.Count - lineup.Count(id => pieces.Any(piece => piece.Id == id));
        internal bool ReserveFull => ReserveCount(bench) >= ShopSeatLayout.ReserveCapacity;
        internal int ReserveOverflowCount => Math.Max(0, ReserveCount(bench) - ShopSeatLayout.ReserveCapacity);
        internal string GuaranteedRefreshOffer {get;set;}
        internal string HeroRecruitId;
        internal int HeroRefreshes,HeroGuarantees;
        public int Gold {get;private set;}=80;
        public int Level {get;private set;}=5;
        public int XP {get;private set;}=18;
        public bool Locked {get;set;}
        public IReadOnlyList<Piece> Bench=>bench.AsReadOnly();
        public IReadOnlyList<string> Offers=>Array.AsReadOnly(offers);
        public IReadOnlyCollection<Role> Roles=>roles.Values;
        public int Available(string id)=>pool[id];
        public int ReservedRewards(string id)=>rewardReservations.Values.Count(ticket=>ticket.Id==id);
        public IReadOnlyCollection<RewardReservation> RewardReservations=>rewardReservations.Values;
        public bool IsReservationActive(RewardReservation ticket)=>ticket!=null&&rewardReservations.TryGetValue(ticket.Token,out var actual)&&ReferenceEquals(actual,ticket);
        public Role Definition(string id)=>roles[id];
        public int Capacity(string id)=>StockForCost(roles[id].Cost);
        public int MaxStar(string id)=>MaxStarForCost(roles[id].Cost);
        public static int StockForCost(int cost)=>cost switch {1=>30,2=>25,3=>18,4=>10,5=>9,_=>throw new ArgumentOutOfRangeException(nameof(cost))};
        public static int MaxStarForCost(int cost){_=StockForCost(cost);return cost==1?4:3;}
        public static int Copies(int star)=>star switch {1=>1,2=>3,3=>9,4=>27,_=>throw new ArgumentOutOfRangeException(nameof(star))};
        static Result Fail(string text)=>new Result {Message=text};

        public ShopTrialModel(IEnumerable<Role> definitions,int seed=73191)
        {
            roles=definitions.Select(r=>new Role{Id=r.Id,Name=r.Name,Cost=r.Cost}).ToDictionary(r=>r.Id,StringComparer.Ordinal);
            if(roles.Count==0||roles.Values.Any(r=>r.Cost<1||r.Cost>5))throw new ArgumentException("Invalid shop roles");
            pool=roles.Keys.ToDictionary(id=>id,Capacity);random=new Random(seed);
        }
        public void Preset(IEnumerable<Piece> pieces,IEnumerable<string> cards,int level=5,int xp=18,int gold=80)
        {
            var b=pieces.Select(p=>p.Copy()).ToArray();var o=cards.ToArray();
            if(b.Length>10||o.Length>5||level<1||level>10||gold<0||xp<0||level<10&&xp>=(FormationEconomy?FormationExperience[level]:JourneyEconomy?CampaignJourneyEconomy.RequiredXP(level):(CampaignEconomy?CampaignPlan.XP:Experience)[level]))throw new ArgumentException("Invalid trial preset");
            var reserved=roles.Keys.ToDictionary(id=>id,id=>0);
            foreach(var p in b){if(!roles.ContainsKey(p.Id)||p.Star<1||p.Star>MaxStar(p.Id))throw new ArgumentException("Invalid piece");reserved[p.Id]+=Copies(p.Star);}
            foreach(var id in o){if(!roles.ContainsKey(id))throw new ArgumentException("Invalid offer");reserved[id]++;}
            if(reserved.Any(pair=>pair.Value>Capacity(pair.Key)))throw new ArgumentException("Preset exceeds stock");
            bench.Clear();sequence=0;foreach(var p in b){p.Instance=++sequence;bench.Add(p);}
            rewardReservations.Clear();
            Array.Clear(offers,0,5);Array.Copy(o,offers,o.Length);
            foreach(var id in roles.Keys)pool[id]=Capacity(id)-reserved[id];
            Level=level;XP=level==10?0:xp;Gold=gold;Locked=false;GuaranteedRefreshOffer=null;rewards.Clear();WinStreak=0;lineup.Clear();lineupInstances.Clear();CommissionUsed=false;Validate();
        }
        public Result PreviewPurchase(int index)=>PlanPurchase(index,out _);
        Result PlanPurchase(int index,out List<Piece> planned)
        {
            planned=null;
            if(index<0||index>=5||offers[index]==null)return Fail("该位置没有可购买棋子");
            var result = PlanIncoming(offers[index], out planned);
            if(result.Success)result.Value=Price(offers[index]);
            if (FormationEconomy && ReserveFull && ReserveCount(planned) >= ReserveCount(bench))
                return Fail("备战席已满，本次招募不能通过升星腾出位置");
            return result;
        }
        Result PlanIncoming(string id,out List<Piece> planned,int incomingStar=1)
        {
            var role=roles[id];
            planned=bench.Select(p=>p.Copy()).ToList();var incoming=new Piece{Instance=sequence+1,Id=id,Star=incomingStar};planned.Add(incoming);
            int target=incoming.Instance,star=incomingStar;
            for(int tier=1;tier<MaxStar(id);tier++)
            {
                var group=planned.Where(p=>p.Id==id&&p.Star==tier).OrderByDescending(p=>IsLineupInstance(p.Instance)).ThenBy(p=>p.Instance).Take(3).ToArray();
                if(group.Length!=3)continue;
                group[0].Star++;planned.Remove(group[1]);planned.Remove(group[2]);target=group[0].Instance;star=group[0].Star;
                if(FormationEconomy&&id==TalulahId&&!TalulahLocked&&role.Cost<5&&star==2)break;
            }
            if(!FormationEconomy&&ReserveCount(planned)>ShopSeatLayout.ReserveCapacity)
                return Fail("备战席已满；本次不能合成，不扣金币");
            return new Result{Success=true,Id=id,Instance=target,FinalStar=star,Value=role.Cost,Message=star>1?$"{role.Name} 晋升 {star}星":"招募成功"};
        }
        public Result Purchase(int index)
        {
            if(TransactionBlocked)return Fail("先完成塔露拉抉择");
            var result=PlanPurchase(index,out var planned);if(!result.Success)return result;
            if(!CanAfford(result.Value))return Fail("金币不足");
            bench.Clear();bench.AddRange(planned);if(!UnlimitedGold)Gold-=result.Value;offers[index]=null;sequence++;DetectTalulahChoice();RemoveMaxStarOffers();Validate();PurchaseCompleted?.Invoke(result.Id);return result;
        }
        public Result Quote(int instance)
        {
            var p=bench.Find(c=>c.Instance==instance);if(p==null)return Fail("棋子不存在");
            int value=roles[p.Id].Cost*Copies(p.Star)-(p.Star==1?0:FormationEconomy&&p.Star==4?2:1);
            return new Result{Success=true,Id=p.Id,Instance=instance,FinalStar=p.Star,Value=value,Message=roles[p.Id].Name};
        }
        public Result Sell(int instance)
        {
            if(TransactionBlocked)return Fail("先完成塔露拉抉择");
            var quote=Quote(instance);if(!quote.Success)return quote;
            bench.RemoveAll(p=>p.Instance==instance);lineupInstances.Remove(instance);if(!UnlimitedGold)Gold+=quote.Value;pool[quote.Id]+=Copies(quote.FinalStar);Validate();return quote;
        }
        public Result Upgrade()
        {
            if(TransactionBlocked)return Fail("先完成塔露拉抉择");
            if(Level==10)return Fail("已达到最高等级");if(!CanAfford(4))return Fail("升级需要4金币");
            int old=Level;if(!UnlimitedGold)Gold-=4;XP+=FormationEconomy?PurchasedXP:4;while(Level<10&&XP>=NextLevelXP){XP-=NextLevelXP;Level++;}if(Level==10)XP=0;
            return new Result{Success=true,Value=Level-old,Message=Level>old?$"等级提升至 {Level} · 编队上限 {Level}":"经验 +4"};
        }
        public Result Refresh(bool free=false)
        {
            if(TransactionBlocked)return Fail("先完成塔露拉抉择");
            bool coupon=!free&&!UnlimitedGold&&FormationEconomy&&FreeRefreshes>0;int price=FormationEconomy?RefreshPrice:2;
            if(Locked)return Fail("已锁定，请先解锁");if(!free&&!coupon&&!CanAfford(price))return Fail($"刷新需要{price}金币");
            var next=new Dictionary<string,int>(pool);foreach(var id in offers)if(id!=null)next[id]++;
            var row=Odds[Level-1];
            string guaranteed=GuaranteedRefreshOffer;
            bool heroPaid=FormationEconomy&&!free&&!coupon&&!UnlimitedGold&&HeroRecruitId!=null&&FormationBalanceV09.Active!=FormationBalanceV09.Variant.Baseline;
            int heroLimit=HeroRecruitId!=null&&roles.TryGetValue(HeroRecruitId,out var heroRole)&&heroRole.Cost==1?1:2;
            bool heroDue=heroPaid&&(HeroRefreshes+1)%5==0&&HeroGuarantees<heroLimit&&!bench.Any(piece=>piece.Id==HeroRecruitId&&piece.Star>=3);
            bool heroInserted=heroDue&&guaranteed==null&&next[HeroRecruitId]>0&&row[roles[HeroRecruitId].Cost-1]>0;
            if(heroInserted)guaranteed=HeroRecruitId;
            if(guaranteed!=null&&(!roles.TryGetValue(guaranteed,out var guaranteedRole)||next[guaranteed]<=0||row[guaranteedRole.Cost-1]<=0))return Fail("保底卡当前无库存或未解锁，不扣费");
            bool Allowed(Role role)=>!FormationEconomy||!bench.Any(p=>p.Id==role.Id&&p.Star>=MaxStar(role.Id));
            if(!roles.Values.Any(r=>row[r.Cost-1]>0&&next[r.Id]>0&&Allowed(r)))return Fail("当前等级库存已空，不扣费");
            var drawn=new string[5];int count=0;
            for(int slot=0;slot<5;slot++)
            {
                if(slot==0&&guaranteed!=null){drawn[slot]=guaranteed;next[guaranteed]--;count++;continue;}
                var available=roles.Values.Where(r=>row[r.Cost-1]>0&&next[r.Id]>0&&Allowed(r)).ToArray();if(available.Length==0)break;
                var costs=available.Select(r=>r.Cost).Distinct().OrderBy(c=>c).ToArray();double n=random.NextDouble()*costs.Sum(c=>row[c-1]);int cost=costs.Last();
                foreach(int c in costs){n-=row[c-1];if(n<0){cost=c;break;}}
                var candidates=available.Where(r=>r.Cost==cost).ToArray();double k=random.NextDouble()*candidates.Sum(r=>next[r.Id]*(RecruitmentWeight?.Invoke(r.Id)??1));var chosen=candidates.Last();
                foreach(var r in candidates){k-=next[r.Id]*(RecruitmentWeight?.Invoke(r.Id)??1);if(k<0){chosen=r;break;}}
                drawn[slot]=chosen.Id;next[chosen.Id]--;count++;
            }
            foreach(var id in roles.Keys)pool[id]=next[id];Array.Copy(drawn,offers,5);if(coupon)FreeRefreshes--;else if(!free&&!UnlimitedGold)Gold-=price;GuaranteedRefreshOffer=null;if(heroPaid)HeroRefreshes++;if(heroInserted)HeroGuarantees++;Validate();
            if(FormationEconomy&&!free&&!UnlimitedGold)ManualRefreshCompleted?.Invoke();
            return new Result{Success=true,Message=count<5?$"库存不足，仅刷新{count}张":"刷新完成"};
        }
        public Result Commission(string id)
        {
            if(TransactionBlocked)return Fail("先完成塔露拉抉择");
            if(!CampaignEconomy)return Fail("本模式未开放委托");
            if(CommissionUsed&&!UnlimitedGold)return Fail("本关委托已使用");
            if(!roles.TryGetValue(id,out var role)||role.Cost>3&&!UnlimitedGold||!bench.Any(p=>p.Id==id)||bench.Any(p=>p.Id==id&&p.Star>=MaxStar(id))||Odds[Level-1][role.Cost-1]==0)return Fail(UnlimitedGold?"委托需已持有、已解锁且未达最高星的角色":"委托需已持有、已解锁且未达最高星的1–3费角色");
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
        public RoundReward SettleRound(string key,bool won,int baseGold=5,int victoryXP=2,bool countsForStreak=true)
        {
            if(baseGold<0||!JourneyEconomy&&victoryXP<0)throw new ArgumentOutOfRangeException();
            if(string.IsNullOrEmpty(key))throw new ArgumentException("Missing settlement key");
            if(rewards.TryGetValue(key,out var old))return old;
            if(countsForStreak)WinStreak=won?WinStreak+1:0;
            int rewardScale=JourneyEconomy?CampaignJourneyEconomy.RewardGoldMultiplier:1;
            var receipt=new RoundReward{Key=key,Won=won,Base=baseGold,GoldBefore=Gold,Interest=Math.Min(FormationEconomy?InterestCap:5,Gold/(FormationEconomy?InterestDivisor:10))*rewardScale,
                Streak=won&&countsForStreak?(FormationEconomy?(WinStreak>=6?3:WinStreak>=4?2:WinStreak>=2?1:0):JourneyEconomy?CampaignJourneyEconomy.StreakBonus(WinStreak):Math.Min(3,Math.Max(0,WinStreak-1)))*rewardScale:0,
                Experience=FormationEconomy?victoryXP:JourneyEconomy?CampaignJourneyEconomy.SettlementExperience:won?victoryXP:0};
            if(!UnlimitedGold)Gold+=receipt.Total;XP+=receipt.Experience;
            while(Level<10&&XP>=NextLevelXP){XP-=NextLevelXP;Level++;}if(Level==10)XP=0;
            rewards.Add(key,receipt);Validate();return receipt;
        }
        public void AddGold(int amount){if(Gold+amount<0)throw new ArgumentOutOfRangeException(nameof(amount));if(!UnlimitedGold)Gold+=amount;}
        public void AddExperience(int amount)
        {if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));XP+=amount;while(Level<10&&XP>=NextLevelXP){XP-=NextLevelXP;Level++;}if(Level==10)XP=0;}
        public RewardReservation ReserveRewardCandidate(string id)
        {
            if(TransactionBlocked||!roles.ContainsKey(id)||pool[id]<=0)return null;
            var ticket=new RewardReservation(++reservationSequence,id);pool[id]--;rewardReservations.Add(ticket.Token,ticket);Validate();return ticket;
        }
        public Result GrantReserved(RewardReservation ticket)
        {
            if(TransactionBlocked)return Fail("先完成塔露拉抉择");
            if(!IsReservationActive(ticket))return Fail("该补给预占已领取或返库");
            var result=PlanIncoming(ticket.Id,out var planned);if(!result.Success)return result;
            // Transfers the exact reserved copy; available stock is neither consumed again nor released.
            bench.Clear();bench.AddRange(planned);rewardReservations.Remove(ticket.Token);sequence++;
            DetectTalulahChoice();RemoveMaxStarOffers();Validate();return result;
        }
        public bool ReleaseReservation(RewardReservation ticket)
        {
            if(!IsReservationActive(ticket))return false;
            rewardReservations.Remove(ticket.Token);pool[ticket.Id]++;Validate();return true;
        }
        public Result Grant(string id,int star=1)
        {
            if(TransactionBlocked)return Fail("先完成塔露拉抉择");
            if(!roles.ContainsKey(id)||star<1||star>MaxStar(id))return Fail("无效奖励角色");
            if(bench.Any(p=>p.Id==id&&p.Star>=MaxStar(id)))return Fail("已持有最高星角色");
            int copies=Copies(star);if(pool[id]<copies)return Fail("库存不足，奖励进入待领");
            var result=PlanIncoming(id,out var planned,star);if(!result.Success)return result;
            bench.Clear();bench.AddRange(planned);pool[id]-=copies;sequence++;DetectTalulahChoice();RemoveMaxStarOffers();Validate();return result;
        }
        public Result GrantAtLeast(string id,int star)
        {
            if(TransactionBlocked)return Fail("先完成塔露拉抉择");
            if(!roles.ContainsKey(id)||star<1||star>MaxStar(id))return Fail("无效星级");
            if(bench.Any(p=>p.Id==id&&p.Star>=star))return new Result{Success=true,Id=id,Message="已有所需星级"};
            var existing=bench.Where(p=>p.Id==id).OrderByDescending(p=>IsLineupInstance(p.Instance)).ThenBy(p=>p.Instance).ToArray();
            if(existing.Length==0)return Grant(id,star);
            int owned=existing.Sum(p=>Copies(p.Star));int missing=Copies(star)-owned;
            if(pool[id]<missing)return Fail("缺少库存，升星奖励进入待领");
            var survivor=existing[0];survivor.Star=star;
            foreach(var other in existing.Skip(1))bench.Remove(other);
            pool[id]-=missing;DetectTalulahChoice();RemoveMaxStarOffers();Validate();return new Result{Success=true,Id=id,Instance=survivor.Instance,FinalStar=star,Message="角色补至目标星级"};
        }
        void RemoveMaxStarOffers()
        {if(!FormationEconomy)return;for(int i=0;i<offers.Length;i++)if(offers[i]!=null&&bench.Any(p=>p.Id==offers[i]&&p.Star>=MaxStar(p.Id))){pool[offers[i]]++;offers[i]=null;}}
        void DetectTalulahChoice()
        {
            if(!FormationEconomy||TalulahId==null||TalulahLocked||roles[TalulahId].Cost>=5)return;
            PendingTalulahChoice=bench.Any(p=>p.Id==TalulahId&&p.Star==2)?roles[TalulahId].Cost:0;
        }
        public Result ResolveTalulah(bool upgradeCost)
        {
            if(PendingTalulahChoice==0||TalulahId==null)return Fail("没有塔露拉抉择");
            var piece=bench.Where(p=>p.Id==TalulahId&&p.Star==2).OrderByDescending(p=>IsLineupInstance(p.Instance)).ThenBy(p=>p.Instance).First();
            if(upgradeCost)
            {
                int newCost=roles[TalulahId].Cost+1;
                int held=bench.Where(p=>p.Id==TalulahId&&p.Instance!=piece.Instance).Sum(p=>Copies(p.Star));
                int reserved=held+1+offers.Count(id=>id==TalulahId)+ReservedRewards(TalulahId);
                if(reserved>StockForCost(newCost))return Fail("在售、持有与奖励预占数量超过新费用库存");
                piece.Star=1;roles[TalulahId].Cost=newCost;pool[TalulahId]=Capacity(TalulahId)-reserved;
            }
            else {TalulahLocked=true;TalulahEnhanced=true;}
            PendingTalulahChoice=0;Validate();return new Result{Success=true,Id=TalulahId,Instance=piece.Instance,FinalStar=piece.Star,Value=roles[TalulahId].Cost,Message=upgradeCost?"塔露拉升费；在售商品同步改价":"塔露拉永久强化，锁定当前费用"};
        }
        public void Validate()
        {
            if(Gold<0||!FormationEconomy&&ReserveOverflowCount>0||bench.Select(p=>p.Instance).Distinct().Count()!=bench.Count||bench.Any(p=>!roles.ContainsKey(p.Id)||p.Star<1||p.Star>MaxStar(p.Id)))throw new InvalidOperationException("Invalid shop state");
            foreach(var id in roles.Keys)
                if(pool[id]<0||pool[id]+offers.Count(o=>o==id)+bench.Where(p=>p.Id==id).Sum(p=>Copies(p.Star))+ReservedRewards(id)!=Capacity(id))throw new InvalidOperationException("Stock mismatch: "+id);
        }
    }
}
