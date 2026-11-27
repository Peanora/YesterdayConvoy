using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    /// <summary>Local economic traits inspired by the earlier Garrison pact rules; no combat stat bonuses.</summary>
    public sealed class FormationEconomyV09
    {
        static readonly string[] foresight = { "M17", "M16", "B05", "B09", "B12" };
        static readonly string[] miracle = { "B01", "B22", "M38", "B03", "B23" };
        public static readonly FormationCampaignRules.DataRecord[] Traits = {
            new FormationCampaignRules.DataRecord { Id="远见", Name="远见", Levels=new[]{2,3},
                Effect="上阵和备战合计2名不同远见激活，3名加速。成功手动刷新或购买远见牌，各叠2/4层；每10层得2金币。80层：远见牌永久减1金；150层：全部商品永久减1金，最低0金。出售仍按原费用返还，可买卖赚取差价。",
                Extra="远见本体实战施放原生主技，每次叠1/2层，结算时入账。层数与折扣本局保留；经济返款需羁绊激活，不增加战斗属性。" },
            new FormationCampaignRules.DataRecord { Id="奇迹", Name="奇迹", Levels=new[]{2,3},
                Effect="上阵和备战合计2名不同奇迹激活，3名加速。成功手动刷新或购买奇迹牌，各叠2/4层；每100层得20金币。每次成功手动刷新后，以18%＋层数×0.3%的概率送1次免费刷新，上限100%，可连续触发。",
                Extra="奇迹本体实战施放原生主技，每次叠1/2层，结算时入账。免费手动刷新可叠层、触发；自动过关刷新、赠牌、失败购买与无限金币试玩不触发。不增加战斗属性。" }
        };
        readonly ShopTrialModel shop;
        readonly Random random;
        readonly HashSet<string> settled = new HashSet<string>();
        public int ForesightLayers { get; private set; }
        public int MiracleLayers { get; private set; }
        public int EarnedGold { get; private set; }
        public int EarnedRefreshes { get; private set; }
        public bool Enabled => shop.FormationEconomy && !shop.UnlimitedGold && FormationBalanceV09.Active != FormationBalanceV09.Variant.Baseline;
        public static bool IsTrait(string trait) => trait == "远见" || trait == "奇迹";
        public static bool Member(string id, string trait)
        {
            string key=FormationCampaignRules.AllUnits.FirstOrDefault(unit=>unit.Id==id||unit.CardId==id)?.Id;
            return key!=null && (trait=="远见"?foresight:trait=="奇迹"?miracle:Array.Empty<string>()).Contains(key);
        }
        public int Count(string trait) => shop.Bench.Where(piece=>Member(piece.Id,trait)).Select(piece=>piece.Id).Distinct().Count();
        public int Level(string trait) => Enabled ? Count(trait)>=3?3:Count(trait)>=2?2:0 : 0;
        public float FreeChance => Level("奇迹")>=2 ? Math.Min(1,.18f+MiracleLayers*.003f) : 0;
        public FormationEconomyV09(ShopTrialModel shop,int seed)
        {
            this.shop=shop; random=new Random(seed^0x725fc);
            shop.PurchaseDiscount=id=>Enabled && (ForesightLayers>=150 || ForesightLayers>=80 && Member(id,"远见"))?1:0;
            shop.PurchaseCompleted=Purchased;
            shop.ManualRefreshCompleted=Refreshed;
        }
        int Gain(string trait) => Level(trait)>=3?4:Level(trait)>=2?2:0;
        void Add(string trait,int amount)
        {
            if(!Enabled || amount<=0 || Level(trait)<2)return;
            int old=trait=="远见"?ForesightLayers:MiracleLayers;
            int next=old+amount;
            if(trait=="远见")ForesightLayers=next;else MiracleLayers=next;
            int step=trait=="远见"?10:100, payout=trait=="远见"?2:20;
            int gold=(next/step-old/step)*payout;
            shop.AddGold(gold); EarnedGold+=gold;
        }
        void Purchased(string id)
        {
            if(!Enabled)return;
            if(Member(id,"远见"))Add("远见",Gain("远见"));
            if(Member(id,"奇迹"))Add("奇迹",Gain("奇迹"));
        }
        void Refreshed()
        {
            if(!Enabled)return;
            Add("远见",Gain("远见")); Add("奇迹",Gain("奇迹"));
            if(random.NextDouble()<FreeChance){shop.FreeRefreshes++;EarnedRefreshes++;}
        }
        public void SettleCasts(string node, IEnumerable<FormationBattleModel.CastEvent> casts)
        {
            if(!Enabled || !settled.Add(node))return;
            foreach(var cast in casts)
            {
                // Only the real recruited body contributes. Hidden drivers and summons have no native cast event.
                if(!shop.IsLineupInstance(cast.Instance) || !shop.Bench.Any(p=>p.Instance==cast.Instance&&FormationCampaignRules.Find(p.Id)?.Id==cast.UnitId))continue;
                foreach(string trait in new[]{"远见","奇迹"})
                    if(Member(cast.UnitId,trait))Add(trait,Gain(trait)/2);
            }
        }
        public string Summary => $"远见 {ForesightLayers}层 · 奇迹 {MiracleLayers}层 · 免费 {FreeChance:P0}";
        public string Detail(string trait) => trait=="远见"
            ? $"当前 {ForesightLayers} 层 · 已得 {EarnedGold} 金币（含奇迹）；折扣 {(ForesightLayers>=150?"全商店−1":ForesightLayers>=80?"远见牌−1":"未解锁")}；折扣不降低出售价值。"
            : $"当前 {MiracleLayers} 层 · 免费刷新概率 {FreeChance:P0} · 已触发 {EarnedRefreshes} 次；可连续触发，无单关收益上限。";
        internal static string[] Tags(string id,IEnumerable<string> tags) => tags
            .Concat(new[]{"远见","奇迹"}.Where(trait=>Member(id,trait))).Distinct().ToArray();
    }
}
