using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>
    /// Real finite-stock preparation for the existing recruitable catalog. The battle never owns money,
    /// card copies or XP. This short-flow starting grant and income are tuning, not acquisition proof.
    /// </summary>
    public sealed class FormationShopSession
    {
        public static FormationShopSession Current { get; private set; }
        public ShopTrialModel Shop { get; private set; }
        public ShopSeatLayout Seats { get; private set; }
        public int Seed { get; private set; }
        public int Round { get; private set; } = 1;
        public ShopTrialModel.RoundReward LastReward { get; private set; }
        public bool CanShop => model == null || model.Phase == FormationPhase.Preparation;

        readonly List<string> lineup = new List<string>();
        readonly Dictionary<int, Vector2> instancePositions = new Dictionary<int, Vector2>();
        readonly Dictionary<string, Vector2> kindPositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        FormationBattleModel model;

        public static FormationShopSession StartNew(int seed)
        {
            Current = new FormationShopSession(seed); return Current;
        }

        public FormationShopSession(int seed)
        {
            Seed = seed;
            Shop = new ShopTrialModel(FormationUnitCatalog.All.Where(role => role.ShopEligible).Select(role =>
                new ShopTrialModel.Role { Id = role.CardId, Name = role.Name, Cost = role.Cost }), seed);
            Shop.EnableJourneyEconomy();
            Shop.Preset(new[]
            {
                new ShopTrialModel.Piece { Id = "CARD_M07", Star = 1 },
                new ShopTrialModel.Piece { Id = "CARD_M10", Star = 1 },
                new ShopTrialModel.Piece { Id = "CARD_M08", Star = 1 }
            }, Array.Empty<string>(), level: 3, xp: 0, gold: 12);
            Seats = new ShopSeatLayout();
            lineup.AddRange(Shop.Bench.Select(piece => piece.Id));
            Synchronize(); Shop.BeginPreparation(); Shop.Refresh(true);
        }

        public void ApplyTo(FormationBattleModel target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (target.Seed != Seed || target.Phase != FormationPhase.Preparation)
                throw new InvalidOperationException("Formation shop can only bind the matching challenge in preparation.");
            if (!target.PrepareShopRound(Round)) throw new InvalidOperationException("Invalid formation round.");
            model = target; Synchronize();
            if (!TryProject(Seats.Lineup(Shop.Bench).ToArray(), out var entries, out var failure))
                throw new InvalidOperationException(failure);
            if (!target.ApplyRoster(entries, Shop.Level)) throw new InvalidOperationException("Shop lineup cannot be projected into this formation battle.");
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
            if (result.Success) Synchronize();
            return result;
        }

        public ShopTrialModel.Result Refresh()
        {
            if (!CanShop) return Unavailable();
            return Shop.Refresh();
        }

        public ShopTrialModel.Result BuyExperience()
        {
            if (!CanShop) return Unavailable();
            var result = Shop.Upgrade();
            if (result.Success) Synchronize();
            return result;
        }

        public ShopTrialModel.Result Sell(int instance)
        {
            if (!CanShop) return Unavailable();
            SavePositions(model);
            var result = Shop.Sell(instance);
            if (result.Success) Synchronize();
            return result;
        }

        public ShopTrialModel.Result Move(int instance, bool toLineup, int seat)
        {
            if (!CanShop) return Unavailable();
            if (!TryPlanSeatMove(instance, toLineup, seat, out var selected, out var failure) ||
                !TryProject(selected, out var entries, out failure))
                return new ShopTrialModel.Result { Message = failure };
            var origin = Seats.For(instance).Value;
            if (!Seats.TryMove(Shop.Bench, Shop.Level, instance, toLineup, seat, out failure))
                return new ShopTrialModel.Result { Message = failure };
            SavePositions(model);
            if (model != null && !model.ApplyRoster(entries, Shop.Level))
            {
                Seats.TryMove(Shop.Bench, Shop.Level, instance, origin.Lineup, origin.Index, out _);
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
            if (enterSeat >= 0 && !Seats.TryMove(Shop.Bench, Shop.Level, instance, true, enterSeat, out var failure))
                return new ShopTrialModel.Result { Message = failure };
            SavePositions(model);
            if (!model.ApplyRoster(entries, Shop.Level))
            {
                if (enterSeat >= 0) Seats.TryMove(Shop.Bench, Shop.Level, instance, origin.Lineup, origin.Index, out _);
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
                enterSeat = Seats.FirstEmpty(true, Shop.Level);
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
            if (!FormationBattleModel.CanBuildRoster(entries, Shop.Level))
                return FormationDeploymentPreview.Invalid("完整编队无法放入左侧部署区，已保留原位置", cells);
            return preview;
        }

        bool TryPlanSeatMove(int instance, bool toLineup, int seat, out ShopTrialModel.Piece[] selected, out string failure)
        {
            selected = null; failure = null;
            var origin = Seats.For(instance);
            if (!origin.HasValue || seat < 0 || seat >= (toLineup ? Shop.Level : 10))
            { failure = "该席位尚未解锁"; return false; }
            int swapped = Seats.At(toLineup, seat);
            selected = Shop.Bench.Where(piece => piece.Instance == instance ? toLineup :
                piece.Instance == swapped ? origin.Value.Lineup : Seats.InLineup(piece.Instance))
                .OrderBy(piece => piece.Instance == instance ? seat :
                    piece.Instance == swapped ? origin.Value.Index : Seats.For(piece.Instance).Value.Index).ToArray();
            if (selected.Select(piece => piece.Id).Distinct(StringComparer.Ordinal).Count() != selected.Length)
            { failure = "同名角色只能有一张出战；可与其直接交换"; return false; }
            if (selected.Length > Shop.Level || Shop.Bench.Count - selected.Length > 10)
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
            if (!FormationBattleModel.CanBuildRoster(entries, Shop.Level))
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
            if (model == null || model.Phase != FormationPhase.Won || round != Round || model.Round != Round) return null;
            // Stable key survives a same-round retry. Neither replay nor repeated GUI frames can farm it.
            LastReward = Shop.SettleRound("formation-win-" + round, true);
            return LastReward;
        }

        public bool AdvanceRound()
        {
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
            if (model == null || model.Round != Round) return false;
            SavePositions(model); model.Retry(); ApplyTo(model);
            // No BeginPreparation, free refresh, reward, gold reset or RNG reset on a retry.
            return true;
        }

        void Synchronize()
        {
            Seats.Synchronize(Shop.Bench, lineup, Shop.Level);
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
                case "CARD_M10": return new Vector2(5, 2);
                case "CARD_M02": return new Vector2(4, 5);
                case "CARD_M07": return new Vector2(4, 3);
                case "CARD_M08": return new Vector2(2, 3);
                case "CARD_M09": return new Vector2(2, 5);
                case "CARD_M17": return new Vector2(1, 2);
                default:
                    var definition = FormationUnitCatalog.Find(id);
                    if (definition == null) throw new ArgumentException("Unsupported formation card: " + id);
                    return new Vector2(FormationBattleModel.DeploymentColumns - definition.Footprint.x,
                        (FormationBattleModel.Height - definition.Footprint.y) / 2);
            }
        }
    }
}
