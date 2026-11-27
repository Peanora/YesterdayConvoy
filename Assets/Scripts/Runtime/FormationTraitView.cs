using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    public enum FormationTraitMemberState { Missing, Reserve, Lineup }

    /// <summary>A read-only view of the real owned cards; it never recruits, equips or changes a lineup.</summary>
    public sealed class FormationTraitMemberView
    {
        public FormationCampaignRules.UnitDefinition Unit { get; }
        public FormationTraitMemberState State { get; }
        public int Cost { get; }
        public int Star { get; }
        public int Instance { get; }
        public bool FromBadge { get; }
        public string CardId => Unit.CardId;
        public string Name => Unit.Name;
        public string StateText => State == FormationTraitMemberState.Lineup ? "已上阵" : State == FormationTraitMemberState.Reserve ? "备战中" : "未拥有";

        internal FormationTraitMemberView(FormationCampaignRules.UnitDefinition unit, ShopTrialModel shop,
            IEnumerable<ShopTrialModel.Piece> candidates, bool fromBadge)
        {
            Unit = unit; FromBadge = fromBadge; Cost = shop.Definition(unit.CardId).Cost;
            var selected = candidates.OrderByDescending(p => shop.IsLineupInstance(p.Instance)).ThenByDescending(p => p.Star).ThenBy(p => p.Instance).FirstOrDefault();
            Instance = selected?.Instance ?? 0; Star = selected?.Star ?? 0;
            State = selected == null ? FormationTraitMemberState.Missing : shop.IsLineupInstance(selected.Instance) ? FormationTraitMemberState.Lineup : FormationTraitMemberState.Reserve;
        }
    }

    public sealed class FormationTraitView
    {
        public FormationCampaignRules.DataRecord Rule { get; }
        public string Id => Rule.Id;
        public string Name => Rule.Name;
        public int Count { get; }
        public int ActiveLevel { get; }
        public int NextLevel { get; }
        public int MissingForNext => Math.Max(0, NextLevel - Count);
        public bool Active => ActiveLevel > 0;
        public SynergyVisualGrade Grade => GradeFor(Rule, ActiveLevel);
        public bool IsFaction { get; }
        public IReadOnlyList<FormationTraitMemberView> Members { get; }
        public IReadOnlyList<FormationTraitMemberView> BadgeMembers { get; }

        public static SynergyVisualGrade GradeFor(FormationCampaignRules.DataRecord rule, int level)
        {
            if (rule == null || level <= 0) return SynergyVisualGrade.Inactive;
            if (FormationCampaignRules.AllFactions.Contains(rule) && FormationCampaignRun.BadgeTraits.Contains(rule.Id) && level == rule.Levels.Last()) return SynergyVisualGradeForPrismatic();
            int index = Array.IndexOf(rule.Levels, level);
            return index < 0 ? SynergyVisualGrade.Inactive : (SynergyVisualGrade)Math.Min(4, index + 1);
        }

        static SynergyVisualGrade SynergyVisualGradeForPrismatic() => (SynergyVisualGrade)4;

        public int NaturalMemberCount => Members.Count;
        public int HighestBadgeRequirement => FormationCampaignRun.BadgeTraits.Contains(Id) ? Math.Max(0,Rule.Levels.Last()-NaturalMemberCount) : 0;

        FormationTraitView(FormationCampaignRules.DataRecord rule, FormationCampaignRun run, ShopTrialModel shop)
        {
            Rule = rule; Count = run.TraitCount(rule.Id);
            ActiveLevel = rule.Levels.Where(l => l <= Count).DefaultIfEmpty(0).Max();
            NextLevel = rule.Levels.Where(l => l > Count).DefaultIfEmpty(0).Min();
            IsFaction = FormationCampaignRules.AllFactions.Contains(rule);
            Members = FormationCampaignRules.AllUnits.Where(u => FormationCampaignRun.HasNativeTrait(u.CardId, rule.Id))
                .Select(u => new FormationTraitMemberView(u, shop, shop.Bench.Where(p => p.Id == u.CardId), false))
                .OrderByDescending(u => u.State).ThenBy(u => u.Cost).ThenBy(u => u.Name, StringComparer.Ordinal).ToArray();
            BadgeMembers = shop.Bench.Where(p => !FormationCampaignRun.HasNativeTrait(p.Id, rule.Id) && run.HasTrait(p, rule.Id))
                .GroupBy(p => p.Id).Select(g => new FormationTraitMemberView(FormationCampaignRules.Find(g.Key), shop, g, true))
                .OrderByDescending(u => u.State).ThenBy(u => u.Cost).ThenBy(u => u.Name, StringComparer.Ordinal).ToArray();
        }

        public static FormationTraitView For(FormationCampaignRun run, ShopTrialModel shop, string id)
        {
            if (run == null || shop == null || string.IsNullOrEmpty(id)) return null;
            var rule = FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles).FirstOrDefault(t => t.Id == id);
            return rule == null ? null : new FormationTraitView(rule, run, shop);
        }

        public static FormationTraitView[] List(FormationCampaignRun run, ShopTrialModel shop, bool includeEmpty = false)
        {
            if (run == null || shop == null) return Array.Empty<FormationTraitView>();
            return FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles)
                .Where(t => includeEmpty || run.TraitCount(t.Id) > 0).Select(t => new FormationTraitView(t, run, shop))
                .OrderByDescending(t => t.Active).ThenByDescending(t => t.ActiveLevel)
                .ThenBy(t => t.NextLevel == 0 ? 0 : t.MissingForNext).ThenByDescending(t => t.Count)
                .ThenBy(t => t.IsFaction ? 0 : 1).ThenBy(t => t.Name, StringComparer.Ordinal).ToArray();
        }
    }
}
