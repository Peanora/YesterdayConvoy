using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    enum FormationBossTrial { None, Pramanix, Supernova, Makoto }

    /// <summary>One finite v0.7 challenge. Scene/UI only choose options; this owns rewards and their once-only keys.</summary>
    public sealed partial class FormationCampaignRun
    {
        static readonly FormationCampaignRules.NodeDefinition SupernovaTrialNode = new FormationCampaignRules.NodeDefinition
        {
            Id = FormationCampaignRules.SupernovaBossNodeId,
            Kind = "首领",
            Plane = 3,
            HealthScale = 1,
            AttackScale = 1,
            TimeLimit = 150,
            Front = new[] { "D31" }
        };

        static readonly FormationCampaignRules.NodeDefinition MakotoTrialNode = new FormationCampaignRules.NodeDefinition
        {
            Id = FormationCampaignRules.MakotoBossNodeId,
            Kind = "首领",
            Plane = 3,
            HealthScale = 1,
            AttackScale = 1,
            TimeLimit = 150,
            Front = new[] { FormationCampaignRules.MakotoBoss.ArtId }
        };

        public sealed class ChoiceOption
        {
            public string Id, Name, Description;
            internal string Outcome, Icon;
            internal int Cost;
            internal Action Apply;
            internal FormationCampaignRules.DataRecord Strategy;
            internal FormationCampaignRules.UnitDefinition SupplyUnit;
            internal string SupplyComponent;
        }
        sealed class Selection
        {
            public string Title;
            public List<ChoiceOption> Options;
            public Func<List<ChoiceOption>> Reroll;
            public bool Rerolled;
            public int Picks=1;
            public bool FirstSupplyComponentWithheld;
        }
        public sealed class ItemInstance
        {
            public int Instance, Stacks, Owner;
            public string Key;
            public string Name=>ItemName(Key);
        }
        public sealed class BallReward
        {
            public string Node { get; }
            public string Color { get; }
            public int Gold { get; }
            public int Refreshes { get; }
            public string Recruit { get; }
            public string Choice { get; }
            public IReadOnlyList<string> Items { get; }

            internal BallReward(string node, string color, int gold, int refreshes, string recruit, string choice, string[] items)
            {
                Node = node; Color = color; Gold = gold; Refreshes = refreshes;
                Recruit = recruit; Choice = choice; Items = Array.AsReadOnly(items);
            }
        }
        public sealed class RewardDrop
        {
            public int Id { get; }
            public string Node { get; }
            public string Color { get; }
            public string Component { get; }
            internal BallPayout Payout { get; }
            internal RewardDrop(int id,string node,string color,BallPayout payout,string component = null)
            { Id = id; Node = node; Color = color; Payout = payout; Component = component; }
        }
        internal sealed class BattleSettlement
        {
            internal string NodeId { get; }
            internal bool Won { get; }
            internal int SquadHPBefore { get; }
            internal int SquadHPAfter { get; }
            internal int BaseGold { get; }
            internal int Interest { get; }
            internal int Streak { get; }
            internal int DebtPaid { get; }
            internal int BonusGold { get; }
            internal int Experience { get; }
            internal int Hope { get; }
            internal int Gold => BaseGold + Interest + Streak - DebtPaid + BonusGold;
            internal IReadOnlyList<RewardDrop> Drops { get; }

            internal BattleSettlement(string nodeId, ShopTrialModel.RoundReward receipt, int squadHPBefore,
                int squadHPAfter, int debtPaid, int bonusGold, int bonusExperience, int hope, RewardDrop[] drops)
            {
                NodeId = nodeId;
                Won = receipt.Won;
                SquadHPBefore = squadHPBefore;
                SquadHPAfter = squadHPAfter;
                BaseGold = receipt.Base + debtPaid;
                Interest = receipt.Interest;
                Streak = receipt.Streak;
                DebtPaid = debtPaid;
                BonusGold = bonusGold;
                Experience = receipt.Experience + bonusExperience;
                Hope = hope;
                Drops = Array.AsReadOnly(drops);
            }
        }
        public sealed class RewardRecruit
        {
            public string CardId { get; }
            public string Name { get; }
            public int Star { get; }
            internal ShopTrialModel.RewardReservation Reservation { get; }
            internal RewardRecruit(string cardId,string name,int star,ShopTrialModel.RewardReservation reservation = null)
            { CardId = cardId; Name = name; Star = star; Reservation = reservation; }
        }
        public sealed class BallPayout
        {
            public int Gold { get; internal set; }
            public int Refreshes { get; internal set; }
            public IReadOnlyList<RewardRecruit> Recruits => recruits;
            public IReadOnlyList<string> Items => items;
            internal readonly List<string> items = new List<string>();
            internal readonly List<RewardRecruit> recruits = new List<RewardRecruit>();
            internal string Choice;
            internal string[] BadgeOptions;
            internal int EquipmentValue => items.Sum(EquipmentWeight) + (Choice == "advanced" ? 2 : Choice == "component" ? 1 : 0);
        }
        sealed class Delivery { public string Id; public int Star=1, Remaining=1; public bool AtLeast, KeepWhenMax; public ShopTrialModel.RewardReservation Reservation; }
        readonly ShopTrialModel shop;
        readonly Random random;
        readonly Queue<Selection> selections=new Queue<Selection>();
        readonly Queue<Selection> rewardSelections=new Queue<Selection>();
        readonly List<ItemInstance> items=new List<ItemInstance>();
        readonly List<Delivery> waiting=new List<Delivery>();
        readonly List<string> augments=new List<string>();
        readonly List<string> openedBalls=new List<string>();
        readonly List<BallReward> ballRewards = new List<BallReward>();
        readonly List<RewardDrop> pendingBalls = new List<RewardDrop>();
        readonly Dictionary<int,RewardDrop> openingBalls = new Dictionary<int,RewardDrop>();
        IReadOnlyList<RewardRecruit> openingRecruits = Array.Empty<RewardRecruit>();
        string openingComponent;
        int ballSequence;
        readonly HashSet<string> settled=new HashSet<string>();
        static readonly int[] HopeMilestones = { 10, 25, 45, 75, 110, 160, 220, 300 };
        readonly HashSet<int> hopeClaims=new HashSet<int>();
        readonly HashSet<int> suppliedPlanes=new HashSet<int>();
        readonly HashSet<int> marchPlanes=new HashSet<int>();
        readonly HashSet<int> preplanSeen=new HashSet<int>();
        readonly HashSet<string> commanderIncomeNodes=new HashSet<string>();
        readonly Dictionary<string,int> veteranStacks=new Dictionary<string,int>();
        readonly Dictionary<string,int> modulus=new Dictionary<string,int>();
        readonly Dictionary<string,int> badgeCounts=new Dictionary<string,int>();
        int sequence, equipmentGranted, manualRefreshes, prepaidSupply, trainingStart=-1, trainingPays;
        int savings, lossCompensation, growthFund, plannedPurchases, doubleSupplies, inspectSupplies, debt;
        int longChipPlane, contractPays, procurementStart=-1;
        string contractId, veteranId;
        bool graduationPaid, talulahFinalReward, deferredAutoRefresh, deferredSupply;
        int lockedReunionCount;
        bool synergyTrialSettled;
        IReadOnlyList<ChoiceOption> talulahThreeCostChoices, talulahFourCostChoices;
        public int NodeIndex {get;private set;}
        public FormationCampaignRules.NodeDefinition CurrentNode => bossTrial switch
        {
            FormationBossTrial.Supernova => SupernovaTrialNode,
            FormationBossTrial.Makoto => MakotoTrialNode,
            _ => FormationCampaignRules.Nodes[NodeIndex]
        };
        public bool IsPractice {get;}
        readonly FormationBossTrial bossTrial;
        public bool IsPramanixBossTrial => bossTrial == FormationBossTrial.Pramanix;
        public bool IsSupernovaBossTrial => bossTrial == FormationBossTrial.Supernova;
        public bool IsMakotoBossTrial => bossTrial == FormationBossTrial.Makoto;
        internal bool IsStandaloneBossTrial => IsSupernovaBossTrial || IsMakotoBossTrial;
        internal FormationCampaignRules.BossDefinition StandaloneBoss => bossTrial switch
        {
            FormationBossTrial.Supernova => FormationCampaignRules.SupernovaBoss,
            FormationBossTrial.Makoto => FormationCampaignRules.MakotoBoss,
            _ => null
        };
        internal string BossTrialName => bossTrial switch
        {
            FormationBossTrial.Pramanix => "圣聆初雪",
            FormationBossTrial.Supernova or FormationBossTrial.Makoto => StandaloneBoss.Name,
            _ => null
        };
        internal bool IsBossTrial => bossTrial != FormationBossTrial.None;
        internal bool IsDifficultyTrial => IsPractice && !IsBossTrial && GuardTrialArt == null && SynergyTrial == null;
        internal string GuardTrialArt { get; set; }
        public FormationSynergyTrial SynergyTrial { get; }
        public string LastTrialProgress { get; private set; }
        internal BattleSettlement LastBattleSettlement { get; private set; }
        internal bool ShowsHopeRewards => SynergyTrial?.Trait.Id == "整合";
        public int SquadHP {get;private set;}=100;
        public int Hope {get;private set;}
        internal int NextHopeMilestone => Array.Find(HopeMilestones, milestone => milestone > Hope);
        public bool Finished {get;private set;}
        public bool Won {get;private set;}
        public bool NodeSettled=>settled.Contains(CurrentNode.Id);
        public int CurrentEncounterTier {get;set;}=1;
        public string PreplanId {get;private set;}
        public IReadOnlyList<string> AugmentIds=>augments;
        public IReadOnlyList<string> OpenedBalls=>openedBalls;
        public IReadOnlyList<BallReward> BallRewards => ballRewards;
        public IReadOnlyList<RewardDrop> PendingBalls => pendingBalls;
        public bool OpeningRewardsPending => openingComponent != null;
        internal IReadOnlyList<RewardRecruit> OpeningRecruits => openingRecruits;
        internal string OpeningComponent => openingComponent;
        public int PendingRewardChoices => rewardSelections.Count;
        public string RewardChoiceTitle => rewardSelections.Count == 0 ? null : rewardSelections.Peek().Title;
        public IReadOnlyList<ChoiceOption> RewardChoices => rewardSelections.Count == 0 ? Array.Empty<ChoiceOption>() : rewardSelections.Peek().Options;
        public int ExtraPopulation=>shop.ExtraPopulation;
        public int TalulahCost=>shop.TalulahId==null?3:shop.Definition(shop.TalulahId).Cost;
        public bool TalulahEnhanced=>shop.TalulahEnhanced;
        public int MainCarryInstance {get;set;}
        public int SecondCarryInstance {get;set;}
        public int ResonanceLeaderInstance {get;set;}
        public int ResonancePartnerInstance {get;set;}
        public int PromptPairA {get;set;}
        public int PromptPairB {get;set;}
        public int RelayReceiverInstance {get;set;}
        public int TheatreLeadInstance {get;set;}
        public int TheaterLeadInstance {get=>TheatreLeadInstance;set=>TheatreLeadInstance=value;}
        public int WisadelSkill {get;set;}=3;
        public int SarkazPact {get;set;}
        public int MudrockXP {get;private set;}
        public bool SarkazBloodPact {get;set;}=true;
        public readonly Dictionary<int,int> RidePairs=new Dictionary<int,int>();
        public readonly List<int> SeaCellIds=new List<int>();
        public string LastSummary {get;private set;}="请选择开局装备与战略预案";
        public int FreeRefreshes=>shop.FreeRefreshes;
        public int PendingDeliveries=>waiting.Sum(x=>x.Remaining);
        internal int PendingTalulahChoiceFee => shop.PendingTalulahChoice;
        public bool HasBlockingChoice => PendingTalulahChoiceFee > 0 || selections.Count > 0;
        public string ChoiceTitle => PendingTalulahChoiceFee > 0
            ? "塔露拉升星抉择"
            : selections.TryPeek(out var selection) ? selection.Title : null;
        public IReadOnlyList<ChoiceOption> Choices => PendingTalulahChoiceFee switch
        {
            3 => talulahThreeCostChoices ??= CreateTalulahChoices(3, "焚城", "余烬之誓",
                "攻击 +20% · 技能伤害 +25%\n所需技力 22 → 18 · 本局锁定3费"),
            4 => talulahFourCostChoices ??= CreateTalulahChoices(4, "火雨", "焚城之心",
                "主技 3 段 → 4 段 · 本局锁定4费\n施法获得20%最大生命护盾，持续8秒"),
            _ => selections.TryPeek(out var selection) ? selection.Options : Array.Empty<ChoiceOption>()
        };

        static IReadOnlyList<ChoiceOption> CreateTalulahChoices(int currentFee, string nextSkill,
            string enhancement, string enhancementEffect)
        {
            int nextFee = currentFee + 1;
            return Array.AsReadOnly(new[]
            {
                new ChoiceOption
                {
                    Id = "tal-up", Name = "升费", Outcome = $"变为{nextFee}费一星", Icon = "ascend", Cost = nextFee,
                    Description = $"获得「{nextSkill}」 · 无需金币或希望\n在售同名卡同步改价"
                },
                new ChoiceOption
                {
                    Id = "tal-enhance", Name = enhancement, Outcome = $"保留{currentFee}费二星", Icon = "ember", Cost = currentFee,
                    Description = enhancementEffect
                }
            });
        }
        internal int SupplyPicksRemaining => shop.PendingTalulahChoice == 0 && selections.TryPeek(out var supply) &&
            supply.Options.Count > 0 && supply.Options[0].SupplyUnit != null ? supply.Picks : 0;
        internal bool SupplyEquipmentWithheld => SupplyPicksRemaining > 0 && selections.Peek().FirstSupplyComponentWithheld;
        public bool CanRerollChoice=>shop.PendingTalulahChoice==0&&selections.Count>0&&selections.Peek().Reroll!=null&&!selections.Peek().Rerolled;
        public IReadOnlyList<ItemInstance> Inventory=>items.Where(x=>x.Owner==0).ToArray();
        public IReadOnlyList<ItemInstance> AllEquipment=>items;
        public IReadOnlyList<ItemInstance> Equipment(int pieceInstance)=>items.Where(x=>x.Owner==pieceInstance).ToArray();
        public bool HasAugment(string id)=>augments.Contains(id);
        public int VeteranStacks(string unitId)=>veteranStacks.TryGetValue(UnitId(unitId),out var value)?value:0;
        public ShopTrialModel Shop=>shop;
        public FormationEconomyV09 Economy {get;private set;}

        public FormationCampaignRun(ShopTrialModel shop,int seed) : this(shop,seed,null) {}

        internal FormationCampaignRun(ShopTrialModel shop,int seed,int? practiceNode,FormationSynergyTrial trial=null,
            IReadOnlyList<string> openingStarters = null, FormationBossTrial bossTrial = FormationBossTrial.None)
        {
            this.shop=shop??throw new ArgumentNullException(nameof(shop));random=new Random(seed^0x51f09);
            Economy=new FormationEconomyV09(shop,seed);
            SynergyTrial=trial;
            this.bossTrial = bossTrial;
            IsPractice=practiceNode.HasValue;
            if(IsPractice&&!SelectPracticeNode(practiceNode.Value))throw new ArgumentOutOfRangeException(nameof(practiceNode));
            if (trial != null)
            {
                foreach (string id in trial.BadgeWearers)
                {
                    var badge = AddItem("徽章:" + trial.Trait.Id);
                    var result = Equip(badge.Instance, shop.Bench.Single(piece => piece.Id == id).Instance);
                    if (!result.Success) throw new InvalidOperationException(result.Message);
                }
                if (trial.Trait.Id == "泥岩") MudrockXP = 74;
                if (trial.Trait.Id == "萨卡兹") { SarkazPact = 2; SarkazBloodPact = true; }
                return;
            }
            if (IsBossTrial) return;
            if (openingStarters != null)
            {
                PrepareOpeningDelivery(openingStarters);
                LastSummary = "开局物资发放中";
            }
            else
            {
                RequestComponents("开局基础装备 · 三选一",3);
                RequestPreplan();
            }
        }

        internal bool SelectPracticeNode(int node)
        {
            if (IsStandaloneBossTrial) return node == 0;
            if(!IsPractice||node<0||node>=FormationCampaignRules.Nodes.Length||FormationCampaignRules.Nodes[node].DefenderCount==0)return false;
            if (SynergyTrial != null && node != SynergyTrial.NodeIndex) return false;
            if (IsPramanixBossTrial && FormationCampaignRules.Nodes[node].Id != "3-7") return false;
            if(NodeIndex!=node)CurrentEncounterTier=1;
            NodeIndex=node;
            synergyTrialSettled=false;
            LastTrialProgress=null;
            return true;
        }
        public ShopTrialModel.Result Choose(int index)
        {
            if(index<0||index>=Choices.Count)return Fail("无效选项");
            if(shop.PendingTalulahChoice>0){var result=shop.ResolveTalulah(index==0);if(result.Success)OnShopChanged();return result;}
            if(selections.Count==0)return Fail("没有待选奖励");
            var selection=selections.Peek();var option=selection.Options[index];
            if (--selection.Picks <= 0) selections.Dequeue();
            else
            {
                selection.Options.RemoveAt(index);
                selection.FirstSupplyComponentWithheld = false;
            }
            option.Apply?.Invoke();OnShopChanged();return Ok("已选择："+option.Name);
        }
        public ShopTrialModel.Result ChooseReward(int index)
        {
            if (HasBlockingChoice || OpeningRewardsPending || openingBalls.Count != 0 || index < 0 || index >= RewardChoices.Count) return Fail("没有可领取的选项");
            var option = rewardSelections.Dequeue().Options[index];
            option.Apply?.Invoke(); OnShopChanged(); return Ok("已选择："+option.Name);
        }
        public bool RerollChoice()
        {if(!CanRerollChoice)return false;var s=selections.Peek();s.Options=s.Reroll();s.Rerolled=true;return true;}
        void Request(string title,IEnumerable<ChoiceOption> choices,Func<List<ChoiceOption>> reroll=null,int picks=1,
            bool firstSupplyComponentWithheld = false)
        {
            var options = choices.ToList();
            if (options.Count == 0) return;
            selections.Enqueue(new Selection
            {
                Title = title, Options = options, Reroll = reroll, Picks = Math.Min(picks, options.Count),
                FirstSupplyComponentWithheld = firstSupplyComponentWithheld
            });
        }
        ChoiceOption Option(string id,string name,string description,Action apply)=>new ChoiceOption{Id=id,Name=name,Description=description,Apply=apply};
        ChoiceOption StrategyOption(FormationCampaignRules.DataRecord record, Action apply) =>
            new ChoiceOption{Id=record.Id,Name=record.Name,Description=FormationBalanceV09.ChoiceText(record),Apply=apply,Strategy=record};
        ChoiceOption SupplyOption(FormationCampaignRules.UnitDefinition unit, string component, string description, Action apply) =>
            new ChoiceOption
            {
                Id = unit.Id, Name = component == null ? unit.Name : unit.Name + " + " + ItemName(component),
                Description = description, Apply = apply, SupplyUnit = unit, SupplyComponent = component
            };
        List<T> Shuffle<T>(IEnumerable<T> source)=>source.OrderBy(_=>random.Next()).ToList();
        string CardId(string id)=>FormationCampaignRules.Find(id)?.CardId??id;
        string UnitId(string id)=>FormationCampaignRules.Find(id)?.Id??id;
        void RequestPreplan()
        {
            List<ChoiceOption> Draw()
            {
                var choices=Shuffle(Enumerable.Range(0,FormationCampaignRules.AllPreplans.Length).Where(i=>!preplanSeen.Contains(i))).Take(3).ToArray();
                foreach(int i in choices)preplanSeen.Add(i);
                return choices.Select(i=>{var plan=FormationCampaignRules.AllPreplans[i];return StrategyOption(plan,()=>ApplyPreplan(plan.Id));}).ToList();
            }
            Request("开局投资环境 · 战略预案三选一",Draw(),Draw);
        }
        void ApplyPreplan(string id)
        {
            PreplanId=id;
            switch(id)
            {
                case "P01":shop.AddGold(6);break;
                case "P04":shop.AddGold(18);break;
                case "P06":shop.FreeRefreshes+=3;break;
                case "P07":RequestAdvanced("军械预付",3);prepaidSupply=2;break;
                case "P11":
                    var roles=FormationCampaignRules.AllUnits.Where(x=>x.Cost==4).ToArray();
                    var selected=new List<FormationCampaignRules.UnitDefinition>();
                    selected.Add(Shuffle(roles.Where(x=>x.Role=="辅助")).First());selected.Add(Shuffle(roles.Where(x=>x.Role!="辅助")).First());
                    selected.Add(Shuffle(roles.Except(selected)).First());
                    Request("精英先遣 · 选择一名4费",selected.Select(x=>Option(x.Id,x.Name,x.Role+" · "+x.Effect,()=>Give(x.Id))));break;
                case "P12":var veteran=Shuffle(FormationCampaignRules.AllUnits.Where(x=>x.Cost==1&&x.Role!="辅助")).First();veteranId=veteran.Id;Give(veteran.Id,2);break;
                case "P13":RequestBadge();break;
                case "P15":Give("M17");Give("M25");break;
                case "P16":Give("M03");Give("M16");break;
            }
            shop.RecruitmentWeight=unit=>id=="P15"&&HasNativeTrait(unit,"整合")||id=="P16"&&HasNativeTrait(unit,"迅攻")?1.35:1;
        }
        public void OnManualRefresh()
        {manualRefreshes++;if(PreplanId=="P02"&&manualRefreshes==16){shop.AddGold(18);AddItem("复制器");}OnShopChanged();}
        public void OnPurchase(string id)
        {if(plannedPurchases>0&&NodeIndex>procurementStart&&shop.Definition(id).Cost>=3&&modulus.GetValueOrDefault("purchase-node",-1)!=NodeIndex){modulus["purchase-node"]=NodeIndex;shop.FreeRefreshes++;}OnShopChanged();}
        public void OnShopChanged()
        {
            foreach(var item in items.Where(x=>x.Owner!=0&&!shop.Bench.Any(p=>p.Instance==x.Owner)))item.Owner=0;
            if(PreplanId=="P03"&&shop.Level>=8&&trainingStart<0)trainingStart=NodeIndex;
            if(HasAugment("ES5")&&!graduationPaid&&shop.Level>=8){graduationPaid=true;shop.AddGold(18);}
            var cannott=shop.Bench.FirstOrDefault(p=>p.Star>=3&&FormationCampaignRules.Find(p.Id)?.Name=="坎诺特");
            if(cannott!=null)NotifyCannottThreeStar(cannott.Instance);
            RefreshV08Economy();
            DrainDeliveries();
            if(deferredAutoRefresh&&!shop.TransactionBlocked)
            {deferredAutoRefresh=false;if(!shop.Locked)shop.Refresh(true);DrainDeliveries();}
            if(deferredSupply&&!shop.TransactionBlocked){deferredSupply=false;RequestSupply();}
            if(!talulahFinalReward&&TalulahCost==5&&shop.Bench.Any(x=>x.Id==shop.TalulahId&&x.Star>=3))
            {
                talulahFinalReward=true;
                foreach(var unit in FormationCampaignRules.AllUnits.Where(x=>x.Factions.Contains("整合")&&x.Id!="TAL"))Give(unit.Id,3,true);
                LastSummary="塔露拉终局奖励：全部整合成员补至三星，缺货奖励进入待领";
            }
        }
        public void Give(string id,int star=1,bool atLeast=false)
        {
            id=CardId(id);if(!shop.Roles.Any(x=>x.Id==id))return;
            if(atLeast&&shop.Bench.Any(x=>x.Id==id&&x.Star>=star))return;
            var result=atLeast?shop.GrantAtLeast(id,star):shop.Grant(id,star);
            if(!result.Success)waiting.Add(new Delivery{Id=id,Star=star,Remaining=1,AtLeast=atLeast});
        }
        void GiveReserved(ShopTrialModel.RewardReservation ticket)
        {
            if(!shop.GrantReserved(ticket).Success)waiting.Add(new Delivery{Id=ticket.Id,Reservation=ticket});
        }
        void DrainDeliveries()
        {
            if(shop.TransactionBlocked)return;
            foreach(var gift in waiting.ToArray())
            {
                if(gift.Reservation!=null)
                {
                    if(!shop.IsReservationActive(gift.Reservation)||shop.GrantReserved(gift.Reservation).Success)waiting.Remove(gift);
                    if(shop.TransactionBlocked)break;
                    continue;
                }
                if(shop.Bench.Any(x=>x.Id==gift.Id&&x.Star>=shop.MaxStar(gift.Id))){if(!gift.KeepWhenMax)waiting.Remove(gift);continue;}
                var result=gift.AtLeast?shop.GrantAtLeast(gift.Id,gift.Star):shop.Grant(gift.Id,gift.Star);
                if(result.Success&&--gift.Remaining==0)waiting.Remove(gift);
                if(shop.TransactionBlocked)break;
            }
        }
        public void LockBattleLineup()
        {lockedReunionCount=TraitCount("整合");v08BattleLocked=true;}
        public int TraitCount(string trait)=>FormationEconomyV09.IsTrait(trait)?Economy.Count(trait):shop.Bench.Where(x=>shop.IsLineupInstance(x.Instance)&&HasTrait(x,trait)).Select(x=>x.Id).Distinct().Count();
        public bool HasTrait(ShopTrialModel.Piece piece,string trait)=>HasNativeTrait(piece.Id,trait)||Equipment(piece.Instance).Any(x=>BadgeTrait(x.Key)==trait);
        public static bool HasNativeTrait(string id,string trait)
        {var unit=FormationCampaignRules.Find(id);return unit!=null&&(unit.Factions.Contains(trait)||unit.Styles.Contains(trait));}
        public ShopTrialModel.RoundReward SettleBattle(bool won,float progress,int initialCount,int remainingCount,bool bossAlive,IEnumerable<FormationBattleModel.CastEvent> economyCasts=null)
        {
            string key=CurrentNode.Id;if(IsPractice||Finished||OpeningRewardsPending||CurrentNode.Kind=="补给")return null;
            if(!settled.Add(key))return null;
            int healthBefore = SquadHP;
            int previousDropSequence = ballSequence;
            bool reward=CurrentNode.Kind=="奖励";
            int debtPaid = Math.Min(3, debt);
            debt -= debtPaid;
            var receipt=shop.SettleRound("formation-v07-"+key,won,5-debtPaid,2,!reward);
            Economy.SettleCasts(key,economyCasts??Array.Empty<FormationBattleModel.CastEvent>());
            int loss=won||reward?0:Math.Min(18,(CurrentNode.Plane==1?3:CurrentNode.Plane==2?5:7)+(int)Math.Ceiling(6d*remainingCount/Math.Max(1,initialCount))+(bossAlive?3:0));
            if(!won&&!reward&&PreplanId=="P14"&&CurrentNode.Kind=="遭遇")loss=Math.Min(21,loss+3);
            SquadHP=Math.Max(0,SquadHP-loss);
            if(savings>0){shop.AddGold(2);savings--;}
            if(!won&&!reward&&lossCompensation>0){shop.AddGold(4);lossCompensation--;}
            int bonusExperience = 0;
            if (PreplanId=="P05" && won && (CurrentNode.Kind=="战斗" || CurrentNode.Kind=="战斗战备") && marchPlanes.Add(CurrentNode.Plane))
            {
                bonusExperience = 4;
                shop.AddGold(4);
                shop.AddExperience(bonusExperience);
            }
            int gainedHope = AwardReunionHope(won);
            if(reward)RewardNode(key);
            if(EarnedEncounterBalls(progress))
            {foreach(string ball in EncounterBalls(CurrentEncounterTier))OpenBall(ball);if(PreplanId=="P14")OpenBall("蓝");}
            foreach(var piece in shop.Bench.Where(p=>shop.IsLineupInstance(p.Instance)))
            {
                if(UnitId(piece.Id)==veteranId)veteranStacks[veteranId]=Math.Min(10,VeteranStacks(veteranId)+1);
                foreach(var item in Equipment(piece.Instance).Where(x=>x.Key=="EE"))item.Stacks=Math.Min(10,item.Stacks+1);
            }
            AwardMudrockExperience();
            // Cannott's legacy ID may differ; metadata name is the stable fallback for the economy payout.
            foreach(var piece in shop.Bench.Where(p=>p.Star>=3&&shop.IsLineupInstance(p.Instance)&&FormationCampaignRules.Find(p.Id)?.Name=="坎诺特"))
                if(commanderIncomeNodes.Add(key))shop.AddGold(30);
            LastSummary=$"{key} {(won?"胜利":"结算")} · {receipt.Total}金币 +2XP"+(loss>0?$" · 队伍生命−{loss}":"");
            OnShopChanged();
            if(SquadHP==0){Finished=true;Won=false;}
            else if(NodeIndex==FormationCampaignRules.Nodes.Length-1){Finished=true;Won=won;}
            int bonusGold = shop.UnlimitedGold ? 0 : shop.Gold - receipt.GoldBefore - receipt.Total;
            LastBattleSettlement = new BattleSettlement(key, receipt, healthBefore, SquadHP, debtPaid,
                bonusGold, bonusExperience, gainedHope, pendingBalls.Where(drop => drop.Id > previousDropSequence).ToArray());
            return receipt;
        }

        internal void SettleSynergyTrial(bool won)
        {
            if (SynergyTrial == null || synergyTrialSettled) return;
            synergyTrialSettled = true;
            if (ShowsHopeRewards)
            {
                int gained = AwardReunionHope(won);
                LastTrialProgress = $"本场希望 +{gained} · 累计 {Hope}";
            }
            if (SynergyTrial.Trait.Id == "泥岩")
            {
                int gained = AwardMudrockExperience();
                LastTrialProgress = $"本场培育经验 +{gained} · 累计 {MudrockXP} XP";
            }
        }

        int AwardReunionHope(bool won)
        {
            if (lockedReunionCount < 3) return 0;
            int earned = lockedReunionCount >= 9 ? 30 : lockedReunionCount >= 7 ? 14 : lockedReunionCount >= 5 ? 7 : 3;
            if (won) earned += 2;
            AddHope(earned);
            return earned;
        }

        int AwardMudrockExperience()
        {
            int count = TraitCount("泥岩");
            if (count < 2) return 0;
            int stars = shop.Bench.Where(piece => shop.IsLineupInstance(piece.Instance) && HasNativeTrait(piece.Id, "泥岩")).Sum(piece => piece.Star);
            int cap = count >= 5 ? 99 : count >= 3 ? 79 : 23;
            int before = MudrockXP;
            MudrockXP = Math.Min(cap, before + (count + stars) / 2);
            return MudrockXP - before;
        }

        public bool AdvanceNode()
        {
            if(Finished||!NodeSettled||HasBlockingChoice||OpeningRewardsPending||openingBalls.Count!=0||NodeIndex+1>=FormationCampaignRules.Nodes.Length)return false;
            int oldPlane=CurrentNode.Plane;NodeIndex++;CurrentEncounterTier=1;openedBalls.Clear();
            if(growthFund>0){shop.AddGold(3);growthFund--;}
            if(contractPays>0){Give(contractId);contractPays--;}
            if(plannedPurchases>0)plannedPurchases--;
            if(trainingStart>=0&&NodeIndex>trainingStart&&trainingPays<3){shop.AddExperience(12);trainingPays++;}
            if(CurrentNode.Plane!=oldPlane)
            {
                if(PreplanId=="P01")shop.AddGold(CurrentNode.Plane==2?10:14);
                if(PreplanId=="P06")shop.FreeRefreshes+=CurrentNode.Plane==2?4:5;
                if(longChipPlane!=0&&CurrentNode.Plane>longChipPlane){shop.AddGold(16);longChipPlane=0;}
            }
            shop.BeginPreparation();if(!shop.Locked){if(shop.TransactionBlocked)deferredAutoRefresh=true;else shop.Refresh(true);}
            EnterNode();OnShopChanged();return true;
        }
        void EnterNode()
        {
            EnterV08Node();
            if(CurrentNode.Kind=="补给"){if(shop.TransactionBlocked)deferredSupply=true;else RequestSupply();}
            else if(CurrentNode.Kind=="战斗战备")RequestAugment();
        }
        void RequestSupply()
        {
            int plane=CurrentNode.Plane;
            var candidateList=new List<FormationCampaignRules.UnitDefinition>();
            var tickets=new Dictionary<string,ShopTrialModel.RewardReservation>();
            for(int i=0;i<5;i++)
            {
                int cost=random.Next(100)<(plane==1?70:60)?plane+1:plane+2;
                var pool=FormationCampaignRules.AllUnits.Where(x=>shop.Definition(x.CardId).Cost==cost&&shop.Available(x.CardId)>0&&!candidateList.Contains(x)).ToArray();
                if(pool.Length==0)pool=FormationCampaignRules.AllUnits.Where(x=>shop.Definition(x.CardId).Cost>=plane+1&&shop.Definition(x.CardId).Cost<=plane+2&&shop.Available(x.CardId)>0&&!candidateList.Contains(x)).ToArray();
                if(pool.Length>0)
                {
                    var unit=pool[random.Next(pool.Length)];var ticket=shop.ReserveRewardCandidate(unit.CardId);
                    if(ticket!=null){candidateList.Add(unit);tickets.Add(unit.Id,ticket);}
                }
            }
            var candidates=candidateList.ToArray();var picked=new HashSet<string>();
            int picks=doubleSupplies>0?2:1;if(doubleSupplies>0)doubleSupplies--;
            int componentForfeit=prepaidSupply>0?1:0;if(prepaidSupply>0)prepaidSupply--;
            int initialPicked=0;
            if(candidates.Length==0)
            {shop.AddGold(plane*4);if(componentForfeit==0)AddItem(RandomComponent());settled.Add(CurrentNode.Id);return;}
            bool inspect=inspectSupplies>0;if(inspect)inspectSupplies--;
            void FinishSupply()
            {
                foreach(var candidate in candidates.Where(unit=>!picked.Contains(unit.Id)))shop.ReleaseReservation(tickets[candidate.Id]);
                settled.Add(CurrentNode.Id);LastSummary="补给已领取";
            }
            void FinishMainChoices()
            {
                var remaining=candidates.Where(unit=>!picked.Contains(unit.Id)).ToArray();
                if(!inspect||remaining.Length==0){FinishSupply();return;}
                Request("入库抽检 · 额外选择一名角色",remaining.Select(unit=>SupplyOption(unit,null,"只领取角色，不领取装备；与主补给共用已预占的候选。",()=>
                {picked.Add(unit.Id);GiveReserved(tickets[unit.Id]);FinishSupply();})));
            }
            var options=candidates.Select(unit=>
            {
                string component=RandomComponent();return SupplyOption(unit,component,$"{shop.Definition(unit.CardId).Cost}费一星 · {unit.Role}",()=>
                {picked.Add(unit.Id);GiveReserved(tickets[unit.Id]);if(initialPicked++>0||componentForfeit==0)AddItem(component);if(initialPicked>=Math.Min(picks,candidates.Length))FinishMainChoices();});
            }).ToArray();
            Request("补给 · 五选"+(picks==2?"二":"一"),options,picks:picks,firstSupplyComponentWithheld:componentForfeit>0);
        }
        void RequestAugment()
        {
            int plane=CurrentNode.Plane,roll=random.Next(100);string tier=plane==1?(roll<70?"银":roll<98?"金":"彩"):plane==2?(roll<25?"银":roll<90?"金":"彩"):(roll<10?"银":roll<70?"金":"彩");
            if(PreplanId=="P09"&&plane==1&&tier=="银")tier="金";if(PreplanId=="P10"&&plane==3)tier="彩";
            List<ChoiceOption> Draw()
            {
                bool badgeTaken=augments.Any(IsBadgeAugment);
                var candidates=FormationCampaignRules.AllAugments.Where(a=>a.Tier==tier&&a.Planes.Contains(plane)&&!augments.Contains(a.Id)&&(!badgeTaken||!IsBadgeAugment(a.Id))&&!(a.Id=="EG2"&&HasAugment("EP4"))&&!(a.Id=="EP4"&&HasAugment("EG2"))&&
                    (a.Id!="EG3"||shop.Bench.Any(p=>shop.Definition(p.Id).Cost<=3))&&(a.Id!="EP3"||shop.Bench.Any(p=>shop.Definition(p.Id).Cost>=4))).ToArray();
                var picks=new List<FormationCampaignRules.DataRecord>();
                foreach(string kind in new[]{"经济","战力"})
                {
                    var pool=candidates.Where(a=>a.Kind==kind).ToArray();var ordinary=Shuffle(pool.Where(a=>!IsBadgeAugment(a.Id))).ToArray();var badge=Shuffle(pool.Where(a=>IsBadgeAugment(a.Id))).ToArray();
                    var choice=badge.Length>0&&random.NextDouble()<.08?badge[0]:ordinary.FirstOrDefault();if(choice!=null)picks.Add(choice);
                }
                var hero=Shuffle(EligibleHeroes(tier,plane)).FirstOrDefault();
                if(hero!=null){picks.Add(hero);return picks.Select(a=>StrategyOption(a,()=>ApplyAugment(a.Id))).ToList();}
                string thirdKind=random.Next(2)==0?"经济":"战力";
                var thirdPool=candidates.Except(picks).Where(a=>a.Kind==thirdKind).ToArray();
                var third=(random.NextDouble()<.08?Shuffle(thirdPool.Where(a=>IsBadgeAugment(a.Id))).FirstOrDefault():null)??Shuffle(thirdPool.Where(a=>!IsBadgeAugment(a.Id))).FirstOrDefault()??Shuffle(candidates.Except(picks).Where(a=>!IsBadgeAugment(a.Id))).FirstOrDefault();if(third!=null)picks.Add(third);
                return picks.Select(a=>StrategyOption(a,()=>ApplyAugment(a.Id))).ToList();
            }
            Request("战略战备 · "+tier+"档三选一",Draw(),Draw);
        }
        public static bool IsBadgeAugment(string id)=>new[]{"CG1","CG2","CP3","CP4","CP5"}.Contains(id);
        void ApplyAugment(string id)
        {
            if(FormationBalanceV09.Heroes.Any(hero=>hero.Id==id)){ApplyHero(id);return;}
            augments.Add(id);
            switch(id)
            {
                case "ES1":shop.AddGold(10);break;
                case "ES2":shop.AddGold(4);savings=4;break;
                case "ES3":shop.AddGold(4);lossCompensation=4;break;
                case "ES4":shop.FreeRefreshes+=8;break;
                case "ES5":shop.AddExperience(4);break;
                case "ES6":shop.AddGold(2);inspectSupplies=2;break;
                case "EG1":shop.AddGold(6);growthFund=6;break;
                case "EG2":shop.AddGold(10);shop.InterestCap=8;break;
                case "EG3":RequestContract(1,3);break;
                case "EG4":shop.AddExperience(8);shop.PurchasedXP=6;break;
                case "EG5":shop.FreeRefreshes+=8;longChipPlane=CurrentNode.Plane;break;
                case "EG6":shop.AddGold(10);plannedPurchases=7;procurementStart=NodeIndex;break;
                case "EP1":shop.AddGold(40);AddItem("复制器");AddItem("复制器");break;
                case "EP2":shop.AddGold(75);debt=36;break;
                case "EP3":RequestContract(4,5);break;
                case "EP4":shop.AddGold(20);shop.InterestDivisor=8;shop.InterestCap=10;break;
                case "EP5":shop.AddGold(8);doubleSupplies=2;break;
                case "EP6":shop.AddGold(12);shop.RefreshPrice=1;break;
                case "CG1":AddItem("徽章:同频");Give("M25");break;
                case "CG2":AddItem("徽章:整合");Give("M25");AddHope(10);break;
                case "CP1":shop.ExtraPopulation=1+CrownPopulation;break;
                case "CP3":AddItem("徽章:同频");break;
                case "CP4":AddItem("徽章:便车");break;
                case "CP5":AddItem("徽章:接棒");break;
                case "CP6":Request("满载军械 · 第一件",Shuffle(FormationCampaignRules.AllItems).Take(3).Select(x=>Option(x.Id,x.Name,x.Effect,()=>{AddItem(x.Id);Request("满载军械 · 第二件",Shuffle(FormationCampaignRules.AllItems.Where(y=>y.Id!=x.Id)).Take(3).Select(y=>Option(y.Id,y.Name,y.Effect,()=>AddItem(y.Id))));})));break;
            }
        }
        void RequestContract(int min,int max)
        {
            var owned=shop.Bench.Select(p=>p.Id).Distinct().Where(id=>shop.Definition(id).Cost>=min&&shop.Definition(id).Cost<=max).ToArray();
            Request("供货合同 · 选择已拥有角色",owned.Select(id=>Option(id,shop.Definition(id).Name,"立即与随后三个新节点各领取一张，实际占库，缺货待领。",()=>{contractId=id;contractPays=3;Give(id);} )));
        }
        public void AddHope(int amount)
        {
            Hope+=amount;
            foreach(int threshold in HopeMilestones)if(Hope>=threshold&&hopeClaims.Add(threshold))
            {
                switch(threshold)
                {
                    case 10:shop.AddGold(6);shop.FreeRefreshes+=2;break;
                    case 25:Give("TAL");break;
                    case 45:OpenBall("蓝");shop.AddGold(8);break;
                    case 75:RequestComponents("75希望 · 基础装备自选",6);Give("TAL");break;
                    case 110:OpenBall("金");AddItem("徽章:整合");break;
                    case 160:Give("TAL");AddItem("复制器");break;
                    case 220:OpenBall("金");OpenBall("金");shop.AddGold(18);break;
                    case 300:AddItem("复制器");AddItem("复制器");shop.AddGold(40);OpenBall("彩");break;
                }
            }
        }
        static int OpeningRewardBallCount(string id) => id switch { "1-1" => 2, "1-2" => 3, _ => 0 };

        void RewardNode(string id)
        {
            int[] counts={OpeningRewardBallCount("1-1"),OpeningRewardBallCount("1-2"),7,9,12};string[] ids={"1-1","1-2","1-8","2-6","3-6"};int[][] odds={new[]{70,28,2,0},new[]{55,40,5,0},new[]{40,48,11,1},new[]{30,50,18,2},new[]{22,48,27,3}};
            int index=Array.IndexOf(ids,id);if(index<0)return;
            for(int i=0;i<counts[index];i++){int roll=random.Next(100),sum=0;for(int k=0;k<4;k++){sum+=odds[index][k];if(roll<sum){OpenBall(new[]{"白","蓝","金","彩"}[k]);break;}}}
            if(PreplanId=="P08"&&new[]{"1-1","2-6","3-6"}.Contains(id)) DropRewardComponent(id);
            int floor = id == "1-1" ? 2 : id == "1-2" ? 3 : 0;
            int committed = equipmentGranted + pendingBalls.Sum(drop=>drop.Payout.EquipmentValue) +
                rewardSelections.Sum(selection=>selection.Options.Min(option=>EquipmentWeight(option.Id)));
            for (int missing = floor-committed; missing>0; missing--) DropRewardComponent(id);
        }
        void DropRewardComponent(string node)
        {
            string component = RandomComponent();
            var payout = new BallPayout();
            payout.items.Add(component);
            pendingBalls.Add(new RewardDrop(++ballSequence,node,"",payout,component));
        }

        void PrepareOpeningDelivery(IReadOnlyList<string> starterIds)
        {
            var recruits = new List<RewardRecruit>(starterIds.Count);
            foreach (string id in starterIds)
            {
                var ticket = shop.ReserveRewardCandidate(id);
                if (ticket == null) throw new InvalidOperationException("Missing stock for starter " + id);
                recruits.Add(new RewardRecruit(id, shop.Definition(id).Name, 1, ticket));
            }
            openingRecruits = recruits.AsReadOnly();
            openingComponent = Shuffle("ABCDEF").First().ToString();
        }

        internal bool CompleteOpeningDelivery()
        {
            if (!OpeningRewardsPending) return false;
            foreach (var recruit in openingRecruits) DeliverRewardRecruit(recruit);
            AddItem(openingComponent);
            openingRecruits = Array.Empty<RewardRecruit>();
            openingComponent = null;
            RequestPreplan();
            LastSummary = "开局物资已入库";
            OnShopChanged();
            return true;
        }

        static int EquipmentWeight(string key) => key.Length == 1 && "ABCDEF".Contains(key) ? 1 :
            FormationCampaignRules.AllItems.Any(item=>item.Id==key) ? 2 : 0;

        public void OpenBall(string color)
        {
            if (IsPractice && !ShowsHopeRewards || color != "白" && color != "蓝" && color != "金" && color != "彩") return;
            string node = CurrentNode.Id;
            pendingBalls.Add(new RewardDrop(++ballSequence,node,color,RollBall(color,node=="1-1"||node=="1-2")));
        }

        internal bool CanBeginBall => !HasBlockingChoice && !OpeningRewardsPending && !shop.ReserveFull;

        public BallPayout BeginBall(int id)
        {
            if (!CanBeginBall || openingBalls.ContainsKey(id)) return null;
            var drop = pendingBalls.Find(ball => ball.Id == id);
            if (drop == null) return null;
            openingBalls.Add(id, drop);
            return drop.Payout;
        }

        BallPayout RollBall(string color,bool early)
        {
            var payout = new BallPayout();
            int roll = random.Next(100);
            if(color=="白")
            {
                if (roll < (early ? 40 : 50)) payout.Gold = 3;
                else if (roll < (early ? 60 : 75)) { payout.Gold = 2; payout.Refreshes = 1; }
                else if (roll < (early ? 80 : 87))
                {
                    payout.Gold = 1;
                    for(int pick=0;pick<3;pick++) RollBallRecruit(payout,1,1);
                }
                else if (roll < 90) { payout.Gold = 1; RollBallRecruit(payout,1,2); }
                else payout.items.Add(RandomComponent());
            }
            else if(color=="蓝")
            {
                if (roll < (early ? 45 : 53)) payout.items.Add(RandomComponent());
                else if (roll < (early ? 60 : 68)) { payout.items.Add(RandomComponent()); payout.Gold = 2; }
                else if (roll < (early ? 75 : 88)) payout.Gold = 6;
                else if (roll < (early ? 90 : 96))
                {
                    RollBallRecruit(payout,2,1); RollBallRecruit(payout,2,1); RollBallRecruit(payout,3,1);
                }
                else { RollBallRecruit(payout,2,2); RollBallRecruit(payout,2,1); }
            }
            else if(color=="金")
            {if(roll<35){payout.items.Add(RandomComponent());payout.items.Add(RandomComponent());}else if(roll<65)payout.items.Add(RandomAdvanced());else if(roll<85){payout.Gold=12;payout.Refreshes=2;}else if(roll<93){payout.Choice="component";payout.Gold=6;}else if(roll<98)payout.items.Add(random.Next(2)==0?"赤宝石":"蓝宝石");else{payout.Choice="badge";payout.Gold=6;}}
            else if(color=="彩")
            {if(roll<35){payout.Choice="advanced";payout.items.Add(RandomComponent());payout.items.Add(RandomComponent());}else if(roll<60){payout.items.Add(random.Next(2)==0?"赤宝石":"蓝宝石");payout.Gold=10;}else if(roll<70){payout.items.Add("复制器");payout.Gold=15;}else if(roll<90){payout.items.Add(RandomAdvanced());payout.items.Add(RandomAdvanced());}else{payout.Choice="badge";payout.Gold=10;}}
            if (payout.Choice == "badge")
            {
                payout.BadgeOptions = Shuffle(BadgeTraits).Where(trait=>badgeCounts.GetValueOrDefault(trait)<2).Take(3).ToArray();
                if (payout.BadgeOptions.Length==0) payout.Choice="component";
            }
            return payout;
        }

        public BallReward CompleteBall(int id)
        {
            if (!openingBalls.TryGetValue(id, out var drop)) return null;
            var payout = drop.Payout;
            openingBalls.Remove(id);
            pendingBalls.Remove(drop);
            int choiceCount = rewardSelections.Count;
            shop.AddGold(payout.Gold); shop.FreeRefreshes += payout.Refreshes;
            var recruits = new List<string>();
            foreach (var recruit in payout.Recruits)
            {
                bool granted = DeliverRewardRecruit(recruit);
                recruits.Add(recruit.Star+"★ "+recruit.Name+(granted?"":"（待领）"));
            }
            foreach (string key in payout.Items) AddItem(key);
            switch (payout.Choice)
            {
                case "component": DeferRewardChoice("金球 · 基础装备自选",ComponentRewardOptions()); break;
                case "advanced": DeferRewardChoice("彩球 · 进阶装备自选",FormationCampaignRules.AllItems.Select(item=>
                    Option(item.Id,item.Name,item.Effect,()=>AddItem(item.Id)))); break;
                case "badge":
                    if (payout.BadgeOptions.Length > 0)
                        DeferRewardChoice("徽章匣 · 自选",payout.BadgeOptions.Select(trait=>Option("徽章:"+trait,trait+"徽章",
                            "添加羁绊标签；若已达同名上限，改为基础装备自选。",()=>
                            {
                                if (badgeCounts.GetValueOrDefault(trait)>=2) DeferRewardChoice("重复徽章拆解 · 基础装备自选",ComponentRewardOptions());
                                else AddItem("徽章:"+trait);
                            })));
                    else DeferRewardChoice("重复徽章拆解 · 基础装备自选",ComponentRewardOptions());
                    break;
            }
            var reward = new BallReward(drop.Node, drop.Color, payout.Gold, payout.Refreshes,
                recruits.Count == 0 ? null : string.Join("、",recruits),
                rewardSelections.Count > choiceCount ? rewardSelections.Last().Title : null, payout.Items.ToArray());
            if (drop.Component == null && drop.Node == CurrentNode.Id) openedBalls.Add(drop.Color);
            ballRewards.Add(reward);
            OnShopChanged();
            return reward;
        }

        bool DeliverRewardRecruit(RewardRecruit recruit)
        {
            bool granted = (recruit.Reservation == null
                ? shop.Grant(recruit.CardId,recruit.Star)
                : shop.GrantReserved(recruit.Reservation)).Success;
            if (!granted) waiting.Add(new Delivery { Id=recruit.CardId, Star=recruit.Star, KeepWhenMax=true,
                Reservation=recruit.Reservation });
            return granted;
        }

        IEnumerable<ChoiceOption> ComponentRewardOptions() => "ABCDEF".Select(letter=>letter.ToString()).Select(key=>
            Option(key,ItemName(key),FormationCampaignRules.FindRecord(key)?.Effect??"基础装备",()=>AddItem(key)));

        void DeferRewardChoice(string title,IEnumerable<ChoiceOption> options) =>
            rewardSelections.Enqueue(new Selection {Title=title,Options=options.ToList()});

        void RollBallRecruit(BallPayout payout,int cost,int star)
        {
            var units = FormationCampaignRules.AllUnits.Where(unit=>shop.Definition(unit.CardId).Cost==cost &&
                payout.Recruits.All(recruit=>recruit.CardId!=unit.CardId)).ToArray();
            if (units.Length == 0) throw new InvalidOperationException("Reward pack has no distinct character at cost "+cost);
            var unit = units[random.Next(units.Length)];
            payout.recruits.Add(new RewardRecruit(unit.CardId,unit.Name,star));
        }
        static readonly string[][] encounterBalls = { new[]{"蓝"}, new[]{"蓝","蓝"}, new[]{"金","蓝"}, new[]{"金","金","彩"} };
        static string[] EncounterBalls(int tier) => encounterBalls[tier >= 1 && tier <= 4 ? tier - 1 : 0];
        bool EarnedEncounterBalls(float progress) => CurrentNode.Kind == "遭遇" && progress > .25f;

        internal int GuardRewardCueLimit(float encounterProgress, int initialGuardCount)
        {
            if (IsPractice || Finished || NodeSettled) return 0;
            if (CurrentNode.Kind == "奖励")
            {
                int openingCount = OpeningRewardBallCount(CurrentNode.Id);
                return openingCount > 0 ? Math.Min(initialGuardCount, openingCount) : initialGuardCount;
            }
            return EarnedEncounterBalls(encounterProgress)
                ? EncounterBalls(CurrentEncounterTier).Length + (PreplanId == "P14" ? 1 : 0)
                : 0;
        }
        string RandomComponent()=>"ABCDEF"[random.Next(6)].ToString();
        string RandomAdvanced()=>FormationCampaignRules.AllItems[random.Next(FormationCampaignRules.AllItems.Length)].Id;
        void RequestComponents(string title,int count)
        {Request(title,Shuffle("ABCDEF".Select(c=>c.ToString())).Take(count).Select(key=>Option(key,ItemName(key),FormationCampaignRules.FindRecord(key)?.Effect??"基础装备",()=>AddItem(key))));}
        void RequestAdvanced(string title,int count,bool different=false)
        {Request(title,Shuffle(FormationCampaignRules.AllItems).Take(count).Select(x=>Option(x.Id,x.Name,x.Effect,()=>{if(different&&modulus.TryGetValue("CP6-item",out var first)&&FormationCampaignRules.AllItems[first].Id==x.Id){RequestAdvanced(title,count,true);return;}AddItem(x.Id);if(!different&&title.StartsWith("满载军械"))modulus["CP6-item"]=Array.FindIndex(FormationCampaignRules.AllItems.ToArray(),v=>v.Id==x.Id);})));}
        void RequestBadge()
        {Request("徽章匣 · 三选一",Shuffle(BadgeTraits).Where(t=>items.Count(x=>BadgeTrait(x.Key)==t)<2).Take(3).Select(t=>Option("徽章:"+t,t+"徽章","添加羁绊标签，占一格装备；无额外基础属性。",()=>AddItem("徽章:"+t))));}
        public static readonly string[] BadgeTraits={"整合","雪怪","泥岩","深池","萨卡兹","海嗣","连击","迅攻","复苏","爆破","提词","便车","剧场","接棒","同频","伊比利亚","蒸汽"};
        public ItemInstance AddItem(string key,bool countBudget=true)
        {
            string badge=BadgeTrait(key);
            if(badge!=null)
            {
                int count=badgeCounts.GetValueOrDefault(badge);
                if(count>=2)
                {var parts=key.Split(':');if(parts.Length>=3)return AddItem(parts[2],countBudget);RequestComponents("重复徽章拆解 · 基础装备自选",6);return null;}
                badgeCounts[badge]=count+1;
            }
            var item=new ItemInstance{Instance=++sequence,Key=key};items.Add(item);if(countBudget){if(key.Length==1&&"ABCDEF".Contains(key))equipmentGranted++;else if(FormationCampaignRules.AllItems.Any(x=>x.Id==key))equipmentGranted+=2;}RefreshV08Economy();return item;
        }
        public static string BadgeTrait(string key)=>key!=null&&key.StartsWith("徽章:")?key.Split(':')[1]:null;
        public static string ItemName(string key)=>key.StartsWith("徽章:")?key.Split(':')[1]+"徽章":key=="复制器"?"完美复制器":FormationCampaignRules.FindRecord(key)?.Name??key;
        public ShopTrialModel.Result Equip(int itemInstance,int pieceInstance)
        {
            if(HasBlockingChoice)return Fail("请先完成待选事项");var item=items.Find(x=>x.Instance==itemInstance);var piece=shop.Bench.FirstOrDefault(x=>x.Instance==pieceInstance);
            if(item==null||piece==null)return Fail("角色或装备不存在");
            if(item.Key.EndsWith("宝石")||item.Key=="复制器")return Fail("此道具不能装备");
            if(item.Owner==pieceInstance)return Ok("装备保持原位");var equipped=Equipment(pieceInstance);
            if(equipped.Count>=3)return Fail("每名角色最多三件装备");
            string badge=BadgeTrait(item.Key);
            if(badge!=null&&(HasNativeTrait(piece.Id,badge)||equipped.Any(x=>BadgeTrait(x.Key)!=null)))return Fail("已有该标签或已装备徽章");
            if(equipped.Any(x=>x.Key==item.Key)&&item.Key.Length>1)return Fail("同名进阶装备不可重复装备");
            item.Owner=pieceInstance;RefreshV08Economy();return Ok("装备已交给"+shop.Definition(piece.Id).Name);
        }
        public ShopTrialModel.Result Unequip(int itemInstance)
        {if(HasBlockingChoice)return Fail("请先完成待选事项");var item=items.Find(x=>x.Instance==itemInstance);if(item==null)return Fail("装备不存在");item.Owner=0;return Ok("已免费卸装");}

        internal ShopTrialModel.Result PreviewCombination(int a,int b,out string key)
            => ValidateCombination(items.Find(i=>i.Instance==a),items.Find(i=>i.Instance==b),out key);

        public ShopTrialModel.Result Combine(int a,int b)
        {
            var x=items.Find(i=>i.Instance==a);
            var y=items.Find(i=>i.Instance==b);
            var validation=ValidateCombination(x,y,out string key);
            if(!validation.Success)return validation;
            int owner=x.Owner==y.Owner?x.Owner:0;
            items.Remove(x);items.Remove(y);var next=AddItem(key,false);
            var piece=shop.Bench.FirstOrDefault(p=>p.Instance==owner);
            if(owner!=0&&piece!=null&&Equipment(owner).Count<3&&!Equipment(owner).Any(i=>i.Key==key)&&
                (BadgeTrait(key)==null||!HasNativeTrait(piece.Id,BadgeTrait(key))&&!Equipment(owner).Any(i=>BadgeTrait(i.Key)!=null)))next.Owner=owner;
            RefreshV08Economy();return Ok("合成："+next.Name);
        }

        ShopTrialModel.Result ValidateCombination(ItemInstance x,ItemInstance y,out string key)
        {
            key=null;
            if(HasBlockingChoice)return Fail("请先完成待选事项");
            if(x==null||y==null||x==y)return Fail("选择两个不同物品");
            bool Base(string s)=>s.Length==1&&"ABCDEF".Contains(s);
            if(Base(x.Key)&&Base(y.Key))key=string.Concat(new[]{x.Key[0],y.Key[0]}.OrderBy(c=>c));
            else if(x.Key.EndsWith("宝石")&&y.Key.EndsWith("宝石"))
                key=x.Key==y.Key?(x.Key=="赤宝石"?"CROWN":"AEGIS"):"CLOAK";
            else
            {
                var gem=x.Key.EndsWith("宝石")?x:y.Key.EndsWith("宝石")?y:null;var component=gem==x?y:x;
                if(gem!=null&&Base(component.Key))
                {string[] traits=gem.Key=="赤宝石"?new[]{"整合","雪怪","泥岩","深池","萨卡兹","海嗣"}:new[]{"连击","迅攻","复苏","接棒","同频","提词"};string trait=traits["ABCDEF".IndexOf(component.Key,StringComparison.Ordinal)];if(badgeCounts.GetValueOrDefault(trait)>=2)return Fail("同类徽章整局最多两枚");key="徽章:"+trait+":"+component.Key;}
            }
            return key==null?Fail("这两件物品没有合成配方"):Ok(ItemName(key));
        }
        public ShopTrialModel.Result UseCopier(int itemInstance,int pieceInstance)
        {
            if(HasBlockingChoice)return Fail("请先完成待选事项");var item=items.Find(x=>x.Instance==itemInstance&&x.Key=="复制器");var piece=shop.Bench.FirstOrDefault(p=>p.Instance==pieceInstance);
            if(item==null||piece==null)return Fail("复制器或目标不存在");var result=shop.Grant(piece.Id);if(!result.Success)return result;items.Remove(item);OnShopChanged();return result;
        }
        public void NotifySkillCast(int pieceInstance)
        {
            var piece=shop.Bench.FirstOrDefault(p=>p.Instance==pieceInstance);if(piece==null||FormationCampaignRules.Find(piece.Id)?.Name!="坎诺特")return;
            string key="cannott-"+CurrentNode.Id;if(modulus.GetValueOrDefault(key)>0)return;modulus[key]=1;shop.AddGold(piece.Star==1?2:piece.Star==2?4:10);
        }
        public void NotifyCannottThreeStar(int pieceInstance)
        {if(!shop.Bench.Any(piece=>piece.Instance==pieceInstance&&piece.Star==3&&FormationCampaignRules.Find(piece.Id)?.Id=="B10"))return;
            if(modulus.GetValueOrDefault("cannott-3star")!=0)return;modulus["cannott-3star"]=1;modulus["v08-can-node-"+CurrentNode.Id]=1;shop.AddGold(300);}
        static ShopTrialModel.Result Ok(string message)=>new ShopTrialModel.Result{Success=true,Message=message};
        static ShopTrialModel.Result Fail(string message)=>new ShopTrialModel.Result{Message=message};
    }
}
