using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>
    /// Real finite-stock preparation for the existing recruitable catalog. The battle never owns money,
    /// card copies or XP. Unlimited test gold bypasses payment, while stock, XP and population remain real.
    /// </summary>
    public sealed class FormationShopSession
    {
        public static FormationShopSession Current { get; private set; }
        public ShopTrialModel Shop { get; private set; }
        public ShopSeatLayout Seats { get; private set; }
        public int Seed { get; private set; }
        public int Round { get; private set; } = 1;
        public FormationCampaignRun Run {get;private set;}
        public ShopTrialModel.RoundReward LastReward { get; private set; }
        public bool CanShop => (model == null || model.Phase == FormationPhase.Preparation)&&
            (Run==null||!Run.Finished&&!Run.HasBlockingChoice&&!Run.OpeningRewardsPending&&!Run.NodeSettled);

        readonly List<string> lineup = new List<string>();
        readonly Dictionary<int, Vector2> instancePositions = new Dictionary<int, Vector2>();
        readonly Dictionary<string, Vector2> kindPositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        FormationBattleModel model;

        public static FormationShopSession StartNew(int seed,bool fullCampaign=false)
        {
            Current = new FormationShopSession(seed,fullCampaign); return Current;
        }

        internal static FormationShopSession StartPractice(int seed,int node)
        {
            Current = new FormationShopSession(seed,true,node); return Current;
        }

        internal static FormationShopSession StartSynergyTrial(int seed, FormationSynergyTrial trial)
        {
            Current = new FormationShopSession(seed, true, trial.NodeIndex, trial);
            return Current;
        }

        public FormationShopSession(int seed,bool fullCampaign=false) : this(seed,fullCampaign,null) {}

        FormationShopSession(int seed,bool fullCampaign,int? practiceNode,FormationSynergyTrial trial=null)
        {
            Seed = seed;
            Shop = new ShopTrialModel((fullCampaign?FormationCampaignRules.AllUnits.Select(role=>new ShopTrialModel.Role{Id=role.CardId,Name=role.Name,Cost=role.Cost}):FormationUnitCatalog.All.Where(role => role.ShopEligible).Select(role =>
                new ShopTrialModel.Role { Id = role.CardId, Name = role.Name, Cost = role.Cost })), seed);
            if(fullCampaign)
            {
                Shop.EnableFormationEconomy(unlimited:practiceNode.HasValue);
                var random=new System.Random(seed^0x317a9);
                var starting = trial?.Roster ?? FormationCampaignRules.AllUnits.Where(role => role.Cost == 1)
                    .OrderBy(_ => random.Next()).Take(3).Select(role => role.CardId).ToArray();
                var pieces = (practiceNode.HasValue ? starting : Array.Empty<string>()).Select(id => new ShopTrialModel.Piece { Id = id,
                    Star = trial?.Trait.Id == "泥岩" && FormationCampaignRun.HasNativeTrait(id, "泥岩") ? 3 : 1 });
                Shop.Preset(pieces, Array.Empty<string>(), level: Math.Max(3, starting.Count), xp: 0, gold: 8);
                Run=new FormationCampaignRun(Shop,seed,practiceNode,trial,practiceNode.HasValue ? null : starting);
                Round=Run.NodeIndex+1;
            }
            else
            {
            // Character-testing demo only. Other Journey callers keep the default finite economy.
            Shop.EnableJourneyEconomy(unlimited: true);
            Shop.Preset(new[]
            {
                new ShopTrialModel.Piece { Id = "CARD_M21", Star = 1 },
                new ShopTrialModel.Piece { Id = "CARD_M18", Star = 1 },
                new ShopTrialModel.Piece { Id = "CARD_M11", Star = 1 }
            }, Array.Empty<string>(), level: 3, xp: 0, gold: 12);
            }
            Seats = new ShopSeatLayout();
            lineup.AddRange(Shop.Bench.Select(piece => piece.Id));
            Synchronize(); Shop.BeginPreparation(); Shop.Refresh(true);
        }

        public void ApplyTo(FormationBattleModel target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (target.Seed != Seed || target.Phase != FormationPhase.Preparation)
                throw new InvalidOperationException("Formation shop can only bind the matching challenge in preparation.");
            if(Run!=null)target.BindCampaign(Run);
            if (!target.PrepareShopRound(Round)) throw new InvalidOperationException("Invalid formation round.");
            model = target; Synchronize();
            if (!TryProject(Seats.Lineup(Shop.Bench).ToArray(), out var entries, out var failure))
                throw new InvalidOperationException(failure);
            if (!target.ApplyRoster(entries, Shop.PopulationCap)) throw new InvalidOperationException("Shop lineup cannot be projected into this formation battle.");
            SavePositions(target);
        }

        public void SavePositions(FormationBattleModel source)
        {
            if (source == null || source.Seed != Seed) return;
            foreach (var unit in source.Units)
            {
                var definition = FormationUnitCatalog.Find(unit.ArtId);
                var piece = Shop.Bench.FirstOrDefault(card => card.Instance == unit.Instance && card.Id == definition?.CardId);
                if (piece == null) continue;
                instancePositions[unit.Instance] = unit.Spawn; kindPositions[piece.Id] = unit.Spawn;
            }
        }

        public ShopTrialModel.Result Purchase(int index)
        {
            if (!CanShop) return Unavailable();
            SavePositions(model);
            var result = Shop.Purchase(index);
            if (result.Success) {Run?.OnPurchase(result.Id);Synchronize();}
            return result;
        }

        public ShopTrialModel.Result Refresh()
        {
            if (!CanShop) return Unavailable();
            var result=Shop.Refresh();if(result.Success)Run?.OnManualRefresh();return result;
        }

        public ShopTrialModel.Result BuyExperience()
        {
            if (!CanShop) return Unavailable();
            var result = Shop.Upgrade();
            if (result.Success) {Run?.OnShopChanged();Synchronize();}
            return result;
        }

        public ShopTrialModel.Result Sell(int instance)
        {
            if (!CanShop) return Unavailable();
            SavePositions(model);
            var result = Shop.Sell(instance);
            if (result.Success) {Run?.OnShopChanged();Synchronize();}
            return result;
        }

        public ShopTrialModel.Result Move(int instance, bool toLineup, int seat)
        {
            if (!CanShop) return Unavailable();
            if (!TryPlanSeatMove(instance, toLineup, seat, out var selected, out var failure) ||
                !TryProject(selected, out var entries, out failure))
                return new ShopTrialModel.Result { Message = failure };
            var origin = Seats.For(instance).Value;
            if (!Seats.TryMove(Shop.Bench, Shop.PopulationCap, instance, toLineup, seat, out failure))
                return new ShopTrialModel.Result { Message = failure };
            SavePositions(model);
            if (model != null && !model.ApplyRoster(entries, Shop.PopulationCap))
            {
                Seats.TryMove(Shop.Bench, Shop.PopulationCap, instance, origin.Lineup, origin.Index, out _);
                return new ShopTrialModel.Result { Message = "编队状态已变化，请重新选择落点" };
            }
            lineup.Clear(); lineup.AddRange(Seats.Lineup(Shop.Bench).Select(piece => piece.Id));
            Synchronize(); Remember(entries);
            return new ShopTrialModel.Result { Success = true, Instance = instance, Message = toLineup ? "已加入军阵，原站位保留" : "已移至备战" };
        }

        public FormationDeploymentPreview PreviewDeploy(int instance, Vector2Int anchor) =>
            PlanDeploy(instance, anchor, out _, out _);

        public ShopTrialModel.Result TryDeploy(int instance, Vector2Int anchor)
        {
            var preview = PlanDeploy(instance, anchor, out var entries, out int enterSeat);
            if (!preview.Valid) return new ShopTrialModel.Result { Message = preview.Failure };
            var origin = Seats.For(instance).Value;
            if (enterSeat >= 0 && !Seats.TryMove(Shop.Bench, Shop.PopulationCap, instance, true, enterSeat, out var failure))
                return new ShopTrialModel.Result { Message = failure };
            SavePositions(model);
            if (!model.ApplyRoster(entries, Shop.PopulationCap))
            {
                if (enterSeat >= 0) Seats.TryMove(Shop.Bench, Shop.PopulationCap, instance, origin.Lineup, origin.Index, out _);
                return new ShopTrialModel.Result { Message = "编队状态已变化，请重新选择落点" };
            }
            lineup.Clear(); lineup.AddRange(Seats.Lineup(Shop.Bench).Select(piece => piece.Id));
            Synchronize(); Remember(entries);
            return new ShopTrialModel.Result { Success = true, Instance = instance, Message = "站位已保存，下关与复活沿用此位置" };
        }

        FormationDeploymentPreview PlanDeploy(int instance, Vector2Int anchor, out List<FormationRosterEntry> entries, out int enterSeat)
        {
            entries = null; enterSeat = -1;
            var piece = Shop.Bench.FirstOrDefault(card => card.Instance == instance);
            var definition = piece == null ? null : FormationUnitCatalog.Find(piece.Id);
            var cells = definition == null ? default : new RectInt(anchor.x, anchor.y, definition.Footprint.x, definition.Footprint.y);
            if (!CanShop || model == null) return FormationDeploymentPreview.Invalid("只能在战前部署角色", cells);
            if (definition == null || !Seats.For(instance).HasValue) return FormationDeploymentPreview.Invalid("角色不在当前收藏中", cells);
            if (!FormationDeployment.Fits(cells)) return FormationDeploymentPreview.Invalid("完整占地必须位于左侧部署区", cells);
            var selected = Seats.Lineup(Shop.Bench).ToArray();
            if (!Seats.InLineup(instance))
            {
                enterSeat = Seats.FirstEmpty(true, Shop.PopulationCap);
                if (enterSeat < 0) return FormationDeploymentPreview.Invalid("上阵席位已满，请先换下角色或升级", cells);
                if (!TryPlanSeatMove(instance, true, enterSeat, out selected, out var seatFailure))
                    return FormationDeploymentPreview.Invalid(seatFailure, cells);
            }
            if (!TryProject(selected, out entries, out var failure)) return FormationDeploymentPreview.Invalid(failure, cells);
            int moving = entries.FindIndex(entry => entry.Instance == instance);
            var preview = FormationDeployment.Preview(entries.Select(entry => FormationDeployment.Cells(entry.Spawn,
                FormationUnitCatalog.Find(entry.ArtId).Footprint)).ToArray(), moving, anchor, entries.Select(entry => entry.Instance).ToArray());
            if (!preview.Valid) return preview;
            entries[moving].Spawn = new Vector2(anchor.x, anchor.y);
            if (preview.SwapIndex >= 0)
                entries[preview.SwapIndex].Spawn = new Vector2(preview.SwapCells.x, preview.SwapCells.y);
            if (!FormationBattleModel.CanBuildRoster(entries, Shop.PopulationCap))
                return FormationDeploymentPreview.Invalid("完整编队无法放入左侧部署区，已保留原位置", cells);
            return preview;
        }

        bool TryPlanSeatMove(int instance, bool toLineup, int seat, out ShopTrialModel.Piece[] selected, out string failure)
        {
            selected = null; failure = null;
            var origin = Seats.For(instance);
            if (!origin.HasValue || seat < 0 || seat >= (toLineup ? Shop.PopulationCap : 10))
            { failure = "该席位尚未解锁"; return false; }
            int swapped = Seats.At(toLineup, seat);
            selected = Shop.Bench.Where(piece => piece.Instance == instance ? toLineup :
                piece.Instance == swapped ? origin.Value.Lineup : Seats.InLineup(piece.Instance))
                .OrderBy(piece => piece.Instance == instance ? seat :
                    piece.Instance == swapped ? origin.Value.Index : Seats.For(piece.Instance).Value.Index).ToArray();
            if (selected.Select(piece => piece.Id).Distinct(StringComparer.Ordinal).Count() != selected.Length)
            { failure = "同名角色只能有一张出战；可与其直接交换"; return false; }
            if (selected.Length > Shop.PopulationCap || Shop.Bench.Count - selected.Length > 10)
            { failure = "备战席或上阵席位已满"; return false; }
            return true;
        }

        bool TryProject(IReadOnlyList<ShopTrialModel.Piece> selected, out List<FormationRosterEntry> entries, out string failure)
        {
            entries = null; failure = null;
            var positions = new Dictionary<int, Vector2>(); var occupied = new List<RectInt>();
            // Preserve complete saved footprints before searching space for new cards.
            foreach (var piece in selected)
            {
                var definition = FormationUnitCatalog.Find(piece.Id);
                if (definition == null) { failure = "角色缺少可用战斗定义"; return false; }
                var current = model?.Units.FirstOrDefault(unit => unit.Instance == piece.Instance && unit.ArtId == definition.ArtId);
                Vector2 anchor;
                bool saved = current != null;
                if (saved) anchor = current.Spawn;
                else if (instancePositions.TryGetValue(piece.Instance, out anchor)) saved = true;
                else
                {
                    var sameKind = model?.Units.FirstOrDefault(unit => unit.Instance > 0 && unit.ArtId == definition.ArtId);
                    if (sameKind != null) { anchor = sameKind.Spawn; saved = true; }
                    else saved = kindPositions.TryGetValue(piece.Id, out anchor);
                }
                if (saved && FormationDeployment.TryCells(anchor, definition.Footprint, out var cells) && FormationDeployment.IsFree(cells, occupied))
                { positions.Add(piece.Instance, anchor); occupied.Add(cells); }
            }
            foreach (var piece in selected.OrderByDescending(piece =>
                FormationUnitCatalog.Find(piece.Id).Footprint.x * FormationUnitCatalog.Find(piece.Id).Footprint.y))
            {
                if (positions.ContainsKey(piece.Instance)) continue;
                var definition = FormationUnitCatalog.Find(piece.Id); Vector2 anchor = DefaultPosition(piece.Id);
                bool found = FormationDeployment.TryCells(anchor, definition.Footprint, out var cells) && FormationDeployment.IsFree(cells, occupied);
                if (!found)
                    for (int x = FormationBattleModel.DeploymentColumns - definition.Footprint.x; x >= 0 && !found; x--)
                        for (int y = 0; y <= FormationBattleModel.Height - definition.Footprint.y; y++)
                        {
                            var candidate = new RectInt(x, y, definition.Footprint.x, definition.Footprint.y);
                            if (!FormationDeployment.IsFree(candidate, occupied)) continue;
                            anchor = new Vector2(x, y); cells = candidate; found = true; break;
                        }
                if (!found) { failure = "左侧没有能容纳完整占地的空位，已保留原编队"; return false; }
                positions.Add(piece.Instance, anchor); occupied.Add(cells);
            }
            entries = selected.Select(piece => new FormationRosterEntry
            {
                Instance = piece.Instance, ArtId = FormationUnitCatalog.Find(piece.Id).ArtId,
                Star = piece.Star, Spawn = positions[piece.Instance]
            }).ToList();
            if (!FormationBattleModel.CanBuildRoster(entries, Shop.PopulationCap))
            { failure = "编队包含重复、越界或不可用角色"; return false; }
            return true;
        }

        void Remember(IEnumerable<FormationRosterEntry> entries)
        {
            foreach (var entry in entries)
            {
                instancePositions[entry.Instance] = entry.Spawn;
                kindPositions[FormationUnitCatalog.Find(entry.ArtId).CardId] = entry.Spawn;
            }
        }

        public bool SetLocked(bool locked)
        {
            if (!CanShop) return false;
            Shop.Locked = locked; return true;
        }

        public ShopTrialModel.RoundReward SettleWin(int round)
        {
            if(Run!=null)return SettleBattle(true,model?.EncounterProgress??0,model?.InitialDefenderCount??0,model?.RemainingDefenders??0,model?.BossAlive??false);
            if (model == null || model.Phase != FormationPhase.Won || round != Round || model.Round != Round) return null;
            // Stable key survives a same-round retry. Neither replay nor repeated GUI frames can farm it.
            LastReward = Shop.SettleRound("formation-win-" + round, true);
            return LastReward;
        }

        public bool AdvanceRound()
        {
            if(Run!=null)return AdvanceNode();
            if (model == null || model.Round != Round || model.Phase != FormationPhase.Won || Round >= 5) return false;
            SavePositions(model); SettleWin(Round);
            if (!model.NextRound()) return false;
            Round = model.Round; LastReward = null;
            Shop.BeginPreparation();
            if (!Shop.Locked) Shop.Refresh(true);
            ApplyTo(model); return true;
        }

        public bool Retry()
        {
            if(Run!=null)return false;
            if (model == null || model.Round != Round) return false;
            SavePositions(model); model.Retry(); ApplyTo(model);
            // No BeginPreparation, free refresh, reward, gold reset or RNG reset on a retry.
            return true;
        }

        public ShopTrialModel.Result Choose(int index)
        {
            if(Run==null||model!=null&&model.Phase!=FormationPhase.Preparation&&model.Phase!=FormationPhase.Won&&model.Phase!=FormationPhase.Lost)return Unavailable();
            var result=Run.Choose(index);if(result.Success){Synchronize();if(model!=null&&model.Phase==FormationPhase.Preparation)ApplyTo(model);}return result;
        }
        public bool RerollChoice()=>Run!=null&&Run.RerollChoice();
        internal Vector2[] OpeningRecruitCenters(IReadOnlyList<FormationCampaignRun.RewardRecruit> recruits)
        {
            // During opening, no cards can be bought or placed before these first copies arrive.
            var starters = recruits.Select((recruit, index) => new ShopTrialModel.Piece
                { Id = recruit.CardId, Star = recruit.Star, Instance = index + 1 }).ToArray();
            if (!TryProject(starters, out var placements, out var failure))
                throw new InvalidOperationException(failure);
            return placements.Select(entry => entry.Spawn +
                (Vector2)(FormationUnitCatalog.Find(entry.ArtId).Footprint - Vector2Int.one) * .5f).ToArray();
        }

        internal FormationCampaignRun.BallReward CompleteBall(int id)
        {
            var opening = Run?.PendingBalls.FirstOrDefault(drop => drop.Id == id && drop.IsOpening);
            var reward = Run?.CompleteBall(id);
            if (reward != null)
            {
                if (opening != null) lineup.AddRange(opening.Payout.Recruits.Select(recruit => recruit.CardId));
                Synchronize();
            }
            return reward;
        }
        public ShopTrialModel.RoundReward SettleBattle(bool won,float progress,int initialCount,int remainingCount,bool bossAlive)
        {
            if(Run==null)return won?SettleWin(Round):null;
            if(model!=null&&model.Phase!=FormationPhase.Won&&model.Phase!=FormationPhase.Lost)return null;
            if (Run.IsPractice)
            {
                Run.SettleSynergyTrial(won);
                Synchronize();
                return null;
            }
            bool already=Run.NodeSettled;
            LastReward=Run.SettleBattle(won,progress,initialCount,remainingCount,bossAlive);
            if(!already&&model!=null)Shop.AddGold(model.BattleGold);
            Synchronize();return LastReward;
        }

        internal FormationBattleModel PreparePracticeBattle(int node)
        {
            if(model==null||Run?.SelectPracticeNode(node)!=true)return null;
            SavePositions(model);
            Round=Run.NodeIndex+1;
            var next=new FormationBattleModel(Seed,model.Terrain);
            ApplyTo(next);
            return next;
        }
        public bool AdvanceNode()
        {
            if(Run==null||model==null)return false;
            SavePositions(model);if(!Run.AdvanceNode())return false;
            Round=Run.NodeIndex+1;LastReward=null;model.Retry();ApplyTo(model);return true;
        }

        void Synchronize()
        {
            Seats.Synchronize(Shop.Bench, lineup, Shop.PopulationCap);
            var selected = Seats.Lineup(Shop.Bench).ToArray();
            lineup.Clear(); lineup.AddRange(selected.Select(piece => piece.Id));
            if (!Shop.SetLineup(lineup, selected.Select(piece => piece.Instance)))
                throw new InvalidOperationException("Formation shop seats violate the real shop capacities.");
            Shop.Validate();
        }

        static ShopTrialModel.Result Unavailable() => new ShopTrialModel.Result { Message = "战斗中不能操作商店，请先返回整备" };

        static Vector2 DefaultPosition(string id)
        {
            switch (id)
            {
                case "CARD_M10": return new Vector2(FormationBattleModel.DeploymentColumns - 1, 2);
                case "CARD_M02": return new Vector2(FormationBattleModel.DeploymentColumns - 2, 5);
                case "CARD_M07": return new Vector2(FormationBattleModel.DeploymentColumns - 2, 3);
                case "CARD_M08": return new Vector2(FormationBattleModel.DeploymentColumns - 4, 3);
                case "CARD_M09": return new Vector2(FormationBattleModel.DeploymentColumns - 4, 5);
                case "CARD_M17": return new Vector2(FormationBattleModel.DeploymentColumns - 5, 2);
                default:
                    var definition = FormationUnitCatalog.Find(id);
                    if (definition == null) throw new ArgumentException("Unsupported formation card: " + id);
                    return new Vector2(FormationBattleModel.DeploymentColumns - definition.Footprint.x,
                        (FormationBattleModel.Height - definition.Footprint.y) / 2);
            }
        }
    }
}
