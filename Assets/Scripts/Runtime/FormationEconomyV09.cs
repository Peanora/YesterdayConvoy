using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    /// <summary>Local economic traits inspired by the earlier Garrison pact rules; no combat stat bonuses.</summary>
    public sealed partial class FormationEconomyV09
    {
        public const int MemberDiscountLayers=100, GlobalDiscountLayers=325, MiracleChainLayers=325, PromotionLayers=32;
        static readonly string[] foresight = { "M17", "M16", "B05", "B09", "B12" };
        static readonly string[] miracle = { "B01", "B22", "M38", "B03", "B23" };
        static readonly Lazy<Dictionary<string,string>> identities=new Lazy<Dictionary<string,string>>(()=>FormationCampaignRules.AllUnits.SelectMany(u=>new[]{(u.Id,u.Id),(u.CardId,u.Id)}).ToDictionary(p=>p.Item1,p=>p.Item2));
        public static readonly FormationCampaignRules.DataRecord[] Traits = {
            new FormationCampaignRules.DataRecord { Id="远见", Name="远见", Levels=new[]{2,3},
                Effect="上阵和备战合计2名不同远见激活，3名加速。每关首次购买每种远见牌叠1/2层，刷新不叠层；每20层得2金币。100层且持有2种二星远见：远见商品减1金；325层且持有3种二星远见：全商店减1金，最低0金。出售仍按原费用，可赚差价。",
                Extra="上阵本体原生主技每种每关前6次各叠2/3层；三星及以上每次再+1层，每关首个主技额外3层、三星额外6层，战后入账。每种远见首次达到三星另得32层，同种本局限一次。层数本局保留，折扣需持续满足二星成员数；赠牌不计首购，但可触发首次三星里程碑；所有刷新均不叠层。不增加战斗属性。" },
            new FormationCampaignRules.DataRecord { Id="奇迹", Name="奇迹", Levels=new[]{2,3},
                Effect="上阵和备战合计2名不同奇迹激活，3名加速。每关首次购买每种奇迹牌叠1/2层，刷新不叠层；每100层得10金币。成功手动刷新有10%＋90%×层数/325的概率送下一次免费刷新，无80%上限；325层且奇迹激活即为100%。",
                Extra="上阵本体原生主技每种每关前6次各叠1/2层；三星及以上每次再+1层，每关首个主技额外3层、三星额外6层，战后入账。每种奇迹首次达到三星另得32层，同种本局限一次。免费刷新可续免费次数，但不能给自己叠层。两条羁绊均封顶325层，满层不再因叠层返款；奇迹不足2种成员时暂停连锁。不增加战斗属性。" }
        };
        readonly ShopTrialModel shop;
        readonly int rulesVersion;
        bool previousRules => rulesVersion==2;
        bool currentRules => rulesVersion>=4;
        readonly HashSet<string> promotedKinds = new HashSet<string>();
        int MemberThreshold => previousRules?120:MemberDiscountLayers;
        int GlobalThreshold => previousRules?300:currentRules?GlobalDiscountLayers:240;
        int MiracleThreshold => previousRules?450:currentRules?MiracleChainLayers:360;
        readonly Random random;
        readonly HashSet<string> settled = new HashSet<string>();
        readonly HashSet<string> purchasedKinds = new HashSet<string>();
        string currentNode;
        public int ForesightLayers { get; private set; }
        public int MiracleLayers { get; private set; }
        public int EarnedGold { get; private set; }
        public int EarnedRefreshes { get; private set; }
        public bool Enabled => shop.FormationEconomy && !shop.UnlimitedGold && FormationBalanceV09.Active != FormationBalanceV09.Variant.Baseline;
        public static bool IsTrait(string trait) => trait == "远见" || trait == "奇迹";
        public static bool Member(string id, string trait)
        {
            string key=id!=null&&identities.Value.TryGetValue(id,out var resolved)?resolved:null;
            return key!=null && (trait=="远见"?foresight:trait=="奇迹"?miracle:Array.Empty<string>()).Contains(key);
        }
        public int Count(string trait) => shop.Bench.Where(piece=>Member(piece.Id,trait)).Select(piece=>piece.Id).Distinct().Count();
        public int Qualified(string trait) => shop.Bench.Where(piece=>piece.Star>=2&&Member(piece.Id,trait)).Select(piece=>piece.Id).Distinct().Count();
        public int Level(string trait) => Enabled ? Count(trait)>=3?3:Count(trait)>=2?2:0 : 0;
        public bool GlobalDiscountReady => Enabled&&ForesightLayers>=GlobalThreshold&&Qualified("远见")>=3;
        public bool MemberDiscountReady => Enabled&&ForesightLayers>=MemberThreshold&&Qualified("远见")>=2;
        public bool MiracleChainReady => Enabled&&MiracleLayers>=MiracleThreshold&&(currentRules?Level("奇迹")>=2:Qualified("奇迹")>=3);
        public float FreeChance => Level("奇迹")<2?0:MiracleChainReady?1:currentRules?Math.Min(1f,.10f+.90f*MiracleLayers/MiracleChainLayers):Math.Min(.8f,.10f+MiracleLayers*.0012f);
        public FormationEconomyV09(ShopTrialModel shop,int seed):this(shop,seed,false) {}
        // Explicit historical comparison only; normal campaigns always construct the current rules.
        internal FormationEconomyV09(ShopTrialModel shop,int seed,bool previousRules):this(shop,seed,previousRules?2:4) {}
        internal FormationEconomyV09(ShopTrialModel shop,int seed,int rulesVersion)
        {
            this.rulesVersion=rulesVersion;
            this.shop=shop; random=new Random(seed^0x725fc);
            shop.PurchaseDiscount=id=>GlobalDiscountReady || MemberDiscountReady&&Member(id,"远见")?1:0;
            shop.PurchaseCompleted=Purchased;
            shop.ManualRefreshCompleted=Refreshed;
        }
        public void BeginNode(string node)
        {
            if(string.IsNullOrEmpty(node)||node==currentNode)return;
            currentNode=node;
        }
        int Gain(string trait) => Level(trait)>=3?2:Level(trait)>=2?1:0;
        void Add(string trait,int amount)
        {
            if(!Enabled || amount<=0 || Level(trait)<2)return;
            int old=trait=="远见"?ForesightLayers:MiracleLayers;
            int next=currentRules?Math.Min(trait=="远见"?GlobalDiscountLayers:MiracleChainLayers,old+amount):old+amount;
            if(trait=="远见")ForesightLayers=next;else MiracleLayers=next;
            int step=trait=="远见"?20:100, payout=trait=="远见"?2:10;
            int gold=(next/step-old/step)*payout;
            shop.AddGold(gold); EarnedGold+=gold;
        }
        void Purchased(string id)
        {
            if(!Enabled)return;
            ObservePromotions();
            if(purchasedKinds.Contains(currentNode+"|"+id))return;
            if(Member(id,"远见")&&Level("远见")>=2||Member(id,"奇迹")&&Level("奇迹")>=2)purchasedKinds.Add(currentNode+"|"+id);
            if(Member(id,"远见"))Add("远见",Gain("远见"));
            if(Member(id,"奇迹"))Add("奇迹",Gain("奇迹"));
        }
        public void ObservePromotions()
        {
            if(!Enabled||previousRules)return;
            foreach(string trait in new[]{"远见","奇迹"})
            {
                if(Level(trait)<2)continue;
                foreach(var piece in shop.Bench.Where(p=>p.Star>=3&&Member(p.Id,trait)))
                    if(promotedKinds.Add(trait+"|"+piece.Id))Add(trait,currentRules?PromotionLayers:trait=="远见"?24:36);
            }
        }
        void Refreshed()
        {
            if(!Enabled)return;
            if(random.NextDouble()<FreeChance){shop.FreeRefreshes++;EarnedRefreshes++;}
        }
        public void SettleCasts(string node, IEnumerable<FormationBattleModel.CastEvent> casts)
        {
            if(!Enabled || !settled.Add(node))return;
            ObservePromotions();
            var counted=new Dictionary<string,int>();
            foreach(var cast in casts)
            {
                // Only the real recruited body contributes. Hidden drivers and summons have no native cast event.
                var piece=shop.Bench.FirstOrDefault(p=>p.Instance==cast.Instance&&FormationCampaignRules.Find(p.Id)?.Id==cast.UnitId);
                if(!shop.IsLineupInstance(cast.Instance) || piece==null)continue;
                if(counted.GetValueOrDefault(cast.UnitId)>=6)continue;
                counted[cast.UnitId]=counted.GetValueOrDefault(cast.UnitId)+1;
                foreach(string trait in new[]{"远见","奇迹"})
                    if(Member(cast.UnitId,trait))
                    {
                        int gain=Gain(trait)+(currentRules&&trait=="远见"&&Level(trait)>=2?1:0);
                        if(!previousRules&&counted[cast.UnitId]==1)Add(trait,piece.Star>=3?6:3);
                        Add(trait,gain+(!previousRules&&piece.Star>=3?1:0));
                    }
            }
        }
        public string Summary => $"远见 {ForesightLayers}/{GlobalThreshold}层 · 奇迹 {MiracleLayers}/{MiracleThreshold}层 · 免费 {FreeChance:P1}";
        public string Detail(string trait) => trait=="远见"
            ? $"当前 {ForesightLayers}/{GlobalThreshold} 层 · 二星及以上成员 {Qualified("远见")}/3 · 累计返款 {EarnedGold} 金（含奇迹）。折扣 {(GlobalDiscountReady?"全商店−1":MemberDiscountReady?"远见牌−1":"未满足层数或练度")}；层数保留，折扣需维持对应练度。三星里程碑32层/种，每种本局一次；三星前6次主技各再+1层。"
            : $"当前 {MiracleLayers}/{MiracleThreshold} 层 · 不同成员 {Count("奇迹")}/2 · 免费概率 {FreeChance:P1} · 已触发 {EarnedRefreshes} 次。无限连锁需{MiracleThreshold}层与2种成员（星级不限）；三星里程碑32层/种，每种本局一次，刷新不叠层。";
        internal static string[] Tags(string id,IEnumerable<string> tags) => tags
            .Concat(new[]{"远见","奇迹"}.Where(trait=>Member(id,trait))).Distinct().ToArray();
    }
}
