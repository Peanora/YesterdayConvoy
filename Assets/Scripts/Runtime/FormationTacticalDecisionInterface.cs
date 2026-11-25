using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        sealed class TacticalDecisionEntry
        {
            public string Key;
            public DecisionOption MenuOption;
            public Func<DecisionRequest> Build;
        }

        string tacticalDecisionPage;
        int hitchDecisionSlot = -1;

        internal void OpenFormationTacticalDecisions()
        {
            if (Campaign == null || Model.Phase != FormationPhase.Preparation || CampaignInteractionBlocked) return;
            OpenFormationOperationsPanel();
            tacticalDecisionPage = null;
            hitchDecisionSlot = -1;
            campaignFormationOpen = true;
        }

        void CloseFormationTacticalDecisions()
        {
            campaignFormationOpen = false;
            tacticalDecisionPage = null;
            hitchDecisionSlot = -1;
        }

        DecisionRequest BuildFormationTacticalDecision()
        {
            if (!campaignFormationOpen || Campaign == null || Model.Phase != FormationPhase.Preparation) return null;

            var run = Campaign;
            var lineup = Session.Seats.Lineup(Shop.Bench).ToArray();
            var entries = new List<TacticalDecisionEntry>();
            ShopTrialModel.Piece[] Members(string trait) => lineup.Where(piece => run.HasTrait(piece, trait)).ToArray();

            void Add(string key, string title, string outcome, string icon, Func<DecisionRequest> build,
                string unavailable = null)
            {
                entries.Add(new TacticalDecisionEntry
                {
                    Key = key,
                    MenuOption = new DecisionOption { Name = title, Outcome = unavailable ?? outcome, Icon = icon,
                        Disabled = unavailable != null },
                    Build = build
                });
            }

            void AddRole(string key, string title, string purpose, string icon, ShopTrialModel.Piece[] members,
                int current, Action<int> select, int other = 0, string unavailable = null,
                Func<ShopTrialModel.Piece, string> describe = null)
            {
                var candidates = members.Where(piece => piece.Instance != other).ToArray();
                var chosen = candidates.FirstOrDefault(piece => piece.Instance == current);
                Add(key, title, chosen == null ? "自动推荐" : Shop.Definition(chosen.Id).Name, icon,
                    () => BuildTacticalRosterDecision(key, title, purpose, icon, candidates, chosen?.Instance ?? 0, select, describe),
                    unavailable ?? (candidates.Length == 0 ? "暂无可选上阵角色" : null));
            }

            AddRole("carry-primary", "主C优先级 1", "优先获得指定主C的支援效果。", "mark",
                lineup, run.MainCarryInstance, value => run.MainCarryInstance = value, run.SecondCarryInstance);
            AddRole("carry-secondary", "主C优先级 2", "第一优先角色无法受援时，优先选择该角色。", "mark",
                lineup, run.SecondCarryInstance, value => run.SecondCarryInstance = value, run.MainCarryInstance);

            var resonance = Members("同频");
            var resonanceLeader = resonance.FirstOrDefault(piece => piece.Instance == run.ResonanceLeaderInstance) ?? resonance.FirstOrDefault();
            string resonanceUnavailable = resonance.Length < 2 ? "需上阵2名同频成员" : null;
            AddRole("resonance-lead", "同频异响 · 领奏者", "累计4拍触发异响，领奏者获得技力并加快下一次主技动作。", "pair",
                resonance, run.ResonanceLeaderInstance, value => run.ResonanceLeaderInstance = value,
                run.ResonancePartnerInstance, resonanceUnavailable);
            AddRole("resonance-partner", "同频异响 · 伴奏者", "与领奏者共同累积拍点，特定伴奏者会提供额外效果。", "pair",
                resonance, run.ResonancePartnerInstance, value => run.ResonancePartnerInstance = value,
                resonanceLeader?.Instance ?? 0, resonanceUnavailable, ResonancePartnerEffect);

            var prompt = Members("提词");
            string promptUnavailable = prompt.Length < 2 ? "需上阵2名提词成员" : null;
            bool manualPromptPair = run.PromptPairA != run.PromptPairB &&
                prompt.Any(piece => piece.Instance == run.PromptPairA) && prompt.Any(piece => piece.Instance == run.PromptPairB);
            void SelectPromptPair(bool first, int value)
            {
                int other = first ? run.PromptPairB : run.PromptPairA;
                if (value == 0) other = 0;
                else if (other == value || !prompt.Any(piece => piece.Instance == other))
                    other = prompt.First(piece => piece.Instance != value).Instance;
                run.PromptPairA = first ? value : other;
                run.PromptPairB = first ? other : value;
            }
            const string promptPurpose = "搭档释放原生主技时，按羁绊档位互相传递技力。";
            AddRole("prompt-first", "提词接力 · 搭档 A", promptPurpose, "pair", prompt, manualPromptPair ? run.PromptPairA : 0,
                value => SelectPromptPair(true, value), manualPromptPair ? run.PromptPairB : 0, promptUnavailable);
            AddRole("prompt-second", "提词接力 · 搭档 B", promptPurpose, "pair", prompt, manualPromptPair ? run.PromptPairB : 0,
                value => SelectPromptPair(false, value), manualPromptPair ? run.PromptPairA : 0, promptUnavailable);

            var relay = Members("接棒");
            AddRole("relay", "接棒时刻 · 接棒者", "其他成员首次完成转阶段、复起或模式返场后，可为接棒者叠加增伤与攻速。", "mark",
                relay, run.RelayReceiverInstance, value => run.RelayReceiverInstance = value,
                unavailable: relay.Length < 2 ? "需上阵2名接棒成员" : null);
            var theater = Members("剧场");
            AddRole("theater", "无间剧场 · 主角", "主角生命低于40%后，最近的存活幕后成员可替其分担伤害。", "mark",
                theater, run.TheaterLeadInstance, value => run.TheaterLeadInstance = value,
                unavailable: theater.Length < 2 ? "需上阵2名剧场成员" : null);

            int sarkazCount = run.TraitCount("萨卡兹");
            string sarkazStatus = run.SarkazPact == 2 && sarkazCount < 6 ? "双契 · 待达6人" : SarkazContractName(run.SarkazPact);
            Add("sarkaz", "萨卡兹契约", sarkazStatus, "pact",
                () => BuildSarkazTacticalDecision(run, sarkazCount), sarkazCount < 2 ? "需上阵2名萨卡兹成员" : null);
            Add("wisadel", "维维美 · 整备技能", $"S{run.WisadelSkill}", "skill",
                () => BuildWisadelTacticalDecision(run), Shop.Bench.Any(piece => piece.Id == "CARD_WISADEL_APRIL") ? null : "尚未招募维维美");

            int hitchCount = run.TraitCount("便车");
            int hitchSlots = (hitchCount >= 6 ? 2 : hitchCount >= 2 ? 1 : 0) + (run.HasAugment("CP4") ? 1 : 0);
            Add("hitch", "搭个便车", $"可安排 {hitchSlots} 组搭乘", "pair",
                () => BuildHitchTacticalDecision(run, lineup, hitchSlots), hitchSlots == 0 ? "需上阵2名便车成员" : null);
            Add("sea", "海嗣 · 溟痕", run.SeaCellIds.Count == 0 ? "自动铺设" : $"已布置 {run.SeaCellIds.Count} 格", "mark",
                null, run.TraitCount("海嗣") < 2 ? "需上阵2名海嗣成员" : null);

            int iberia=run.TraitCount("伊比利亚");
            Add("v08-iberia","伊比利亚 · 全队誓约",iberia>=4?"可选择两项":"可选择一项","pact",()=>BuildV08IberiaDecision(run,iberia),iberia<2?"需上阵2名伊比利亚成员":null);
            Add("v08-trade","行商 · 军需交易",run.MilitaryTradeAvailable?"8金币 · 本位面1次":"本位面已用或未上阵","mark",()=>BuildV08TradeDecision(run),!run.MilitaryTradeAvailable?"需行商上阵且本位面未成交":null);
            Add("v08-sui","画中天地 · 画境中心",run.SuiCentre.HasValue?run.SuiCentre.Value.ToString():"默认岁起始格","mark",()=>BuildV08SuiDecision(run),run.TraitCount("画中天地")<1?"需岁上阵":null);
            AddRole("v08-steam","蒸汽骑士 · 主驾驶","普通装备触发只继承主驾驶；所有驾驶员保留羁绊计数。","mark",Members("蒸汽"),run.SteamMainDriver,value=>run.SteamMainDriver=value,unavailable:run.TraitCount("蒸汽")==0?"需蒸汽成员上阵":null);
            Add("v08-population","人口来源",$"等级{Shop.Level} + 战备{(run.HasAugment("CP1")?1:0)} + 冠冕{run.CrownPopulation} = {Shop.PopulationCap}","mark",()=>BuildV08PopulationDecision(run));

            var page = entries.Find(entry => entry.Key == tacticalDecisionPage);
            if (page?.Build != null && !page.MenuOption.Disabled) return page.Build();
            tacticalDecisionPage = null;

            return new DecisionRequest
            {
                Key = "tactical-menu",
                Title = "军阵抉择",
                Subtitle = "选一项调整下场作战配置",
                Navigation = true,
                Options = entries.Select(entry => entry.MenuOption).ToArray(),
                Confirm = index =>
                {
                    if ((uint)index >= (uint)entries.Count || entries[index].MenuOption.Disabled) return false;
                    if (entries[index].Key == "sea")
                    {
                        EnterFormationSeaPaintingFromDecision();
                        return true;
                    }
                    tacticalDecisionPage = entries[index].Key;
                    return true;
                },
                Close = CloseFormationTacticalDecisions
            };
        }

        DecisionRequest BuildTacticalRosterDecision(string key, string title, string purpose, string icon,
            IReadOnlyList<ShopTrialModel.Piece> candidates, int current, Action<int> select,
            Func<ShopTrialModel.Piece, string> describe)
        {
            var options = new List<DecisionOption>(candidates.Count + 1)
            {
                new DecisionOption { Name = "自动推荐", Icon = "auto", Current = current == 0 }
            };
            foreach (var piece in candidates)
            {
                var role = Shop.Definition(piece.Id);
                options.Add(new DecisionOption
                {
                    Name = role.Name,
                    Outcome = $"{piece.Star}星",
                    Description = describe?.Invoke(piece),
                    Icon = icon,
                    ArtId = piece.Id,
                    Cost = role.Cost,
                    Current = piece.Instance == current
                });
            }
            return TacticalOptionRequest(key + ":" + string.Join(",", candidates.Select(piece => piece.Instance)),
                title, purpose, options, index => select(index == 0 ? 0 : candidates[index - 1].Instance));
        }

        static string ResonancePartnerEffect(ShopTrialModel.Piece piece) => FormationCampaignRules.Find(piece.Id)?.Id switch
        {
            "M25" => "领奏者主技结束后，回复其最大生命的4%。",
            "B09" => "领奏者主技结束后，追加30%的摄影回放伤害。",
            "M32" => "异响同时给领奏者8%最大生命护盾，持续4秒。",
            "B11" => "每次异响额外给领奏者1点自然技力。",
            "B13" => "领奏者的原生主技伤害也能累积拍点，每次最多6拍。",
            _ => null
        };

        DecisionRequest BuildHitchTacticalDecision(FormationCampaignRun run, ShopTrialModel.Piece[] lineup, int slots)
        {
            bool Heavy(ShopTrialModel.Piece piece) => FormationCampaignRules.Find(piece.Id)?.Size == "重";
            var passengers = lineup.Where(piece => !Heavy(piece) && run.HasTrait(piece, "便车")).ToArray();
            var carriers = lineup.Where(Heavy).ToArray();
            var configured = run.RidePairs.Take(slots).ToArray();
            var active = new (ShopTrialModel.Piece Passenger, ShopTrialModel.Piece Carrier)[slots];
            for (int index = 0; index < configured.Length; index++)
            {
                var pair = configured[index];
                var passenger = passengers.FirstOrDefault(piece => piece.Instance == pair.Key);
                var carrier = carriers.FirstOrDefault(piece => piece.Instance == pair.Value);
                if (passenger == null || carrier == null || active.Take(index).Any(ride => ride.Carrier == carrier)) continue;
                active[index] = (passenger, carrier);
            }

            if (hitchDecisionSlot >= 0 && hitchDecisionSlot < slots)
                return BuildHitchSlotDecision(run, hitchDecisionSlot, passengers, carriers, active);

            hitchDecisionSlot = -1;
            var options = new DecisionOption[slots];
            for (int index = 0; index < slots; index++)
            {
                var ride = active[index];
                options[index] = new DecisionOption
                {
                    Name = $"搭乘编组 {index + 1}",
                    Outcome = index > configured.Length ? "先指定上一组" :
                        ride.Passenger == null ? "自动推荐" : $"{Shop.Definition(ride.Passenger.Id).Name} → {Shop.Definition(ride.Carrier.Id).Name}",
                    Icon = "pair",
                    Disabled = index > configured.Length
                };
            }

            return new DecisionRequest
            {
                Key = "tactical:hitch:" + slots,
                Title = "搭个便车",
                Subtitle = "已指定搭档优先，剩余名额自动配对",
                Navigation = true,
                Back = true,
                Options = options,
                Confirm = index =>
                {
                    if ((uint)index >= (uint)slots || options[index].Disabled) return false;
                    hitchDecisionSlot = index;
                    return true;
                },
                Close = () => tacticalDecisionPage = null
            };
        }

        DecisionRequest BuildHitchSlotDecision(FormationCampaignRun run, int slot,
            ShopTrialModel.Piece[] passengers, ShopTrialModel.Piece[] carriers,
            (ShopTrialModel.Piece Passenger, ShopTrialModel.Piece Carrier)[] active)
        {
            var otherRides = active.Where((_, index) => index != slot).ToArray();
            var candidates = (from passenger in passengers
                              where !otherRides.Any(ride => ride.Passenger == passenger)
                              from carrier in carriers
                              where !otherRides.Any(ride => ride.Carrier == carrier)
                              select (Passenger: passenger, Carrier: carrier)).ToArray();
            var current = active[slot];
            var options = new List<DecisionOption>(candidates.Length + 1)
            {
                new DecisionOption { Name = "自动推荐", Description = "依据体型及距离匹配剩余角色。", Icon = "auto", Current = current.Passenger == null }
            };
            foreach (var pair in candidates)
            {
                var passenger = Shop.Definition(pair.Passenger.Id);
                options.Add(new DecisionOption
                {
                    Name = "乘客", Outcome = passenger.Name, ArtId = pair.Passenger.Id, Cost = passenger.Cost,
                    Description = "载具 · " + Shop.Definition(pair.Carrier.Id).Name,
                    Current = pair == current
                });
            }

            string optionIds = string.Join(",", candidates.Select(pair => $"{pair.Passenger.Instance}-{pair.Carrier.Instance}"));
            return new DecisionRequest
            {
                Key = $"tactical:hitch-slot:{slot}:{optionIds}",
                Title = $"搭个便车 · 编组 {slot + 1}",
                Subtitle = "便车成员搭乘一名重型队友",
                Footer = candidates.Length == 0 ? "暂无可配对乘客与载具" : null,
                Back = true,
                Options = options,
                Confirm = index =>
                {
                    if ((uint)index >= (uint)options.Count) return false;
                    var ordered = run.RidePairs.ToList();
                    if (slot < ordered.Count) ordered.RemoveAt(slot);
                    if (index > 0)
                    {
                        var chosen = candidates[index - 1];
                        ordered.RemoveAll(pair => pair.Key == chosen.Passenger.Instance);
                        ordered.Insert(Math.Min(slot, ordered.Count),
                            new KeyValuePair<int, int>(chosen.Passenger.Instance, chosen.Carrier.Instance));
                    }
                    run.RidePairs.Clear();
                    foreach (var pair in ordered) run.RidePairs.Add(pair.Key, pair.Value);
                    RefreshCampaignPreparation();
                    hitchDecisionSlot = -1;
                    return true;
                },
                Close = () => hitchDecisionSlot = -1
            };
        }

        static string SarkazContractName(int pact) => pact switch { 1 => "重契", 2 => "双契", _ => "血契" };

        DecisionRequest BuildSarkazTacticalDecision(FormationCampaignRun run, int count)
        {
            int attack = count >= 6 ? 55 : count >= 4 ? 35 : 20;
            int health = count >= 6 ? 70 : count >= 4 ? 45 : 25;
            var options = new[]
            {
                new DecisionOption { Name = "血契", Outcome = $"攻击 +{attack}%", Description = count>=10?"彩档免除生命代价":"最大生命 −12%", Icon = "pact", Current = run.SarkazPact == 0 },
                new DecisionOption { Name = "重契", Outcome = $"最大生命 +{health}%", Description = count>=10?"彩档免除间隔代价":"普攻间隔 ×1.15", Icon = "pact", Current = run.SarkazPact == 1 },
                new DecisionOption { Name = "双契", Outcome = "同时启用血契和重契", Description = count >= 10 ? "彩档保留收益并免除双方代价" : count >= 6 ? "同时承担两种契约的代价" : "需上阵6名萨卡兹成员", Icon = "pact", Current = run.SarkazPact == 2, Disabled = count < 6 }
            };

            return TacticalOptionRequest($"sarkaz:{count}", "萨卡兹契约", "契约效果只作用于萨卡兹成员。", options, index =>
            {
                run.SarkazPact = index;
                run.SarkazBloodPact = index != 1;
            });
        }

        DecisionRequest BuildWisadelTacticalDecision(FormationCampaignRun run)
        {
            var options = new[]
            {
                new DecisionOption { Name = "一技能", Outcome = "攻击回复 · 0/3 技力", Description = "连续攻击4段，每段造成240%攻击力物伤；三星12段、每段1500%真伤。", Icon = "skill", Current = run.WisadelSkill == 1 },
                new DecisionOption { Name = "二技能", Outcome = "自然回复 · 12/24 技力", Description = "强化普攻8秒，至多攻击3人，各造成200%攻击力物伤；三星全体目标、每人1000%真伤。", Icon = "skill", Current = run.WisadelSkill == 2 },
                new DecisionOption { Name = "三技能", Outcome = "自然回复 · 16/28 技力", Description = "每隔1.1秒发射一发炮弹，共6发；每发造成范围物伤，三星12发、每发全场2000%真伤，间隔0.7秒。", Icon = "skill", Current = run.WisadelSkill == 3 }
            };

            for(int index=0;index<options.Length;index++)options[index].Disabled=!run.CanSelectWisadelMode(index+1);
            return TacticalOptionRequest("wisadel", "维维美 · 整备技能", null, options, index => run.SelectWisadelMode(index + 1));
        }

        DecisionRequest TacticalOptionRequest(string key, string title, string subtitle, IReadOnlyList<DecisionOption> options, Action<int> apply)
        {
            return new DecisionRequest
            {
                Key = "tactical:" + key,
                Title = title,
                Subtitle = subtitle,
                Options = options,
                Confirm = index =>
                {
                    if ((uint)index >= (uint)options.Count || options[index].Disabled) return false;
                    apply(index);
                    RefreshCampaignPreparation();
                    tacticalDecisionPage = null;
                    return true;
                },
                Back = true,
                Close = () => tacticalDecisionPage = null
            };
        }
    }
}
