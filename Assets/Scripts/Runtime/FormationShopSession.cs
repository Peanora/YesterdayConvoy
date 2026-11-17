using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>
    /// Real finite-stock preparation for the six formation prototypes. The battle never owns money,
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
            Shop = new ShopTrialModel(new[]
            {
                new ShopTrialModel.Role { Id = "CARD_M10", Name = "重装防御者", Cost = 2 },
                new ShopTrialModel.Role { Id = "CARD_M02", Name = "猎狗", Cost = 1 },
                new ShopTrialModel.Role { Id = "CARD_M07", Name = "士兵", Cost = 1 },
                new ShopTrialModel.Role { Id = "CARD_M08", Name = "弩手", Cost = 1 },
                new ShopTrialModel.Role { Id = "CARD_M09", Name = "术师", Cost = 1 },
                new ShopTrialModel.Role { Id = "CARD_M17", Name = "游击队传令兵", Cost = 3 }
            }, seed);
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
            var pieces = Seats.Lineup(Shop.Bench).ToArray();
            var positions = new Dictionary<int, Vector2>();
            var occupied = new HashSet<Vector2>();
            // Reserve all surviving saved positions before assigning a newly recruited character.
            foreach (var piece in pieces)
            {
                Vector2 cell;
                bool saved = instancePositions.TryGetValue(piece.Instance, out cell) || kindPositions.TryGetValue(piece.Id, out cell);
                if (saved && occupied.Add(cell)) positions.Add(piece.Instance, cell);
            }
            foreach (var piece in pieces)
            {
                if (positions.ContainsKey(piece.Instance)) continue;
                Vector2 cell = DefaultPosition(piece.Id);
                if (!occupied.Add(cell))
                {
                    bool found = false;
                    for (int x = FormationBattleModel.DeploymentColumns - 1; x >= 0 && !found; x--)
                        for (int y = 0; y < FormationBattleModel.Height; y++)
                        {
                            var candidate = new Vector2(x, y);
                            if (!occupied.Add(candidate)) continue;
                            cell = candidate; found = true; break;
                        }
                    if (!found) throw new InvalidOperationException("No formation deployment cell is available.");
                }
                positions.Add(piece.Instance, cell);
            }
            var entries = pieces.Select(piece => new FormationRosterEntry
            {
                Instance = piece.Instance, ArtId = piece.Id.Substring(5), Star = piece.Star, Spawn = positions[piece.Instance]
            }).ToArray();
            if (!target.ApplyRoster(entries, Shop.Level)) throw new InvalidOperationException("Shop lineup cannot be projected into this formation battle.");
            SavePositions(target);
        }

        public void SavePositions(FormationBattleModel source)
        {
            if (source == null || source.Seed != Seed) return;
            foreach (var unit in source.Units)
            {
                var piece = Shop.Bench.FirstOrDefault(card => card.Instance == unit.Instance && card.Id == "CARD_" + unit.ArtId);
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
            SavePositions(model);
            if (!Seats.TryMove(Shop.Bench, Shop.Level, instance, toLineup, seat, out var failure))
                return new ShopTrialModel.Result { Message = failure };
            lineup.Clear(); lineup.AddRange(Seats.Lineup(Shop.Bench).Select(piece => piece.Id));
            Synchronize();
            return new ShopTrialModel.Result { Success = true, Instance = instance, Message = toLineup ? "已加入军阵，原站位保留" : "已移至备战" };
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
                default: throw new ArgumentException("Unsupported formation card: " + id);
            }
        }
    }
}
