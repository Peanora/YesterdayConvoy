using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    public sealed class FormationSynergyTrial
    {
        public FormationCampaignRules.DataRecord Trait { get; }
        public int NodeIndex { get; }
        public IReadOnlyList<string> Roster { get; }
        public IReadOnlyList<string> BadgeWearers { get; }
        public string Hint { get; }

        FormationSynergyTrial(FormationCampaignRules.DataRecord trait, int nodeIndex, string[] roster,
            string[] badgeWearers, string hint)
        {
            Trait = trait;
            NodeIndex = nodeIndex;
            Roster = Array.AsReadOnly(roster);
            BadgeWearers = Array.AsReadOnly(badgeWearers);
            Hint = hint;
        }

        public static IReadOnlyList<FormationSynergyTrial> All { get; } = Array.AsReadOnly(new[]
        {
            Create("整合", "1-4", "结算获得 30 希望，获胜再加 2。留意跨过希望里程碑后的奖励。",
                first: new[] { "M20", "TAL" }, badges: new[] { "M18", "M36" }),
            Create("雪怪", "1-6", "让凿冰人和远程雪怪打同一个目标，观察两次寒冷形成冻结、凿冰人击碎后全队回技力。",
                first: new[] { "M13", "M14" }),
            Create("泥岩", "2-4", "开战看 IV 阶巨像和护盾。保持四人上阵打完，再战可观察 V 阶巨像。",
                first: new[] { "M28", "M27" }, badges: new[] { "M29" }),
            Create("深池", "1-6", "观察受击守军持续燎烧，以及带燎烧的守军倒下后，附近深池成员获得余烬和技力。",
                first: new[] { "M29", "M31" }),
            Create("萨卡兹", "2-1", "在搭档面板切换血契、重契和双契，对照开战后的生命、攻击与普攻节奏。",
                first: new[] { "M28", "M20", "M24", "M38", "M42", "M27" }),
            Create("海嗣", "1-6", "可把溟痕铺到交战一侧，观察神经爆发后溟痕扩张。四名天然海嗣各触发一次后全队海嗣提速。",
                first: new[] { "M33", "M35", "M34", "M36" }, badges: new[] { "M18" }),
            Create("伊比利亚", "1-6", "让安纳斯塔西奥和瓦列里奥一起攻击，观察多段伤害累积罪证后的下一次主技。"),
            Create("岁影", "2-4", "易落子后，留意岁下一次显现的落爪伤害，以及易收到的技力。"),
            Create("国度", "2-1", "把圣愚和黑蛇放在同一侧，让国度和战壕覆盖同一批守军，观察重叠处的法伤。"),
            Create("连击", "2-4", "看裂兽、安纳斯塔西奥或维维美的多段攻击，连击八人档每累积三段原生伤害会追加补击。",
                first: new[] { "M20", "B22", "WIS" }, badges: new[] { "B04", "M16" }),
            Create("迅攻", "2-1", "让锏或裂兽持续攻击同一个重装，观察普攻越来越密、每三次普攻额外回技力。",
                first: new[] { "B04", "B22" }),
            Create("复苏", "2-1", "等梅菲斯特、坎诺特的治疗溢出转成护盾，观察主 C 获盾后的下一次主技。",
                first: new[] { "M28", "B08" }),
            Create("爆破", "2-4", "让塔露拉、霜星或掷能者攻击紧凑守军，观察扩大后的范围伤害和首段减抗。"),
            Create("提词", "2-1", "看首言者和萨卡兹术师的技力条，一方主技命中时会给另一方补技力。",
                first: new[] { "M36", "M38" }),
            Create("便车", "1-6", "开战看两对乘客随重型载具移动并各自攻击。载具倒下时，乘客会落地。",
                first: new[] { "M11", "SLUG", "M18", "B14" }),
            Create("接棒", "2-7", "把逐火战士等放前排，爱国者留后。其它成员首次返场时，观察爱国者的接力层数。",
                first: new[] { "M20", "M30", "B24" }),
            Create("剧场", "2-4", "让主角弑君者先接敌。血量低于四成后再次受击时，观察最近的幕后分担伤害。",
                first: new[] { "B03" }, badges: new[] { "M30" }),
            Create("行商","2-1","军需交易与三星产金。"),
            Create("军械库","2-1","整备切换维维美模式。"),
            Create("画中天地","2-1","选择画境中心并观察附近减伤。"),
            Create("蒸汽","2-1","三名驾驶员仅合为一台原型机，主驾驶装备触发、返场与进门。"),
            Create("同频", "2-1", "观察巫王和蔓德拉完成四次普攻后，领奏巫王获得技力与护盾，下一次主技动作加快。",
                first: new[] { "B11", "M32" }),
        });

        public static FormationSynergyTrial Find(string traitId) =>
            All.FirstOrDefault(trial => trial.Trait.Id == traitId);

        static FormationSynergyTrial Create(string traitId, string nodeId, string hint,
            string[] first = null, string[] badges = null)
        {
            var trait = FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles)
                .Single(rule => rule.Id == traitId);
            int nodeIndex = Array.FindIndex(FormationCampaignRules.Nodes, node => node.Id == nodeId);
            string[] natural = FormationCampaignRules.AllUnits
                .Where(unit => unit.Factions.Contains(traitId) || unit.Styles.Contains(traitId))
                .Select(unit => unit.CardId).ToArray();
            var badgeWearers = ResolveCards(badges).ToList();
            int deficit=trait.Levels.Max()-natural.Length;
            foreach(var candidate in FormationCampaignRules.AllUnits.Where(unit=>!natural.Contains(unit.CardId)).Select(unit=>unit.CardId))
                if(badgeWearers.Count<deficit&&!badgeWearers.Contains(candidate))badgeWearers.Add(candidate);
            var badgeArray=badgeWearers.ToArray();
            var firstMembers = ResolveCards(first);
            int count = trait.Levels.Max();
            var roster = firstMembers.Concat(natural).Distinct().Take(count - badgeArray.Length)
                .Concat(badgeWearers).Concat(traitId=="行商"?new[]{"CARD_B01"}:Array.Empty<string>()).ToArray();

            if (nodeIndex < 0 || FormationCampaignRules.Nodes[nodeIndex].DefenderCount == 0 ||
                firstMembers.Any(card => !natural.Contains(card)) || badgeWearers.Any(natural.Contains) ||
                badgeArray.Length > 2 || badgeArray.Length > 0 && !FormationCampaignRun.BadgeTraits.Contains(traitId) ||
                roster.Length != count+(traitId=="行商"?1:0) || roster.Distinct().Count() != roster.Length)
                throw new InvalidOperationException("Invalid formation synergy trial: " + traitId);
            return new FormationSynergyTrial(trait, nodeIndex, roster, badgeArray, hint);
        }

        static string[] ResolveCards(string[] ids) => (ids ?? Array.Empty<string>()).Select(id =>
            FormationCampaignRules.Find(id)?.CardId ??
            throw new InvalidOperationException("Unknown trial card: " + id)).ToArray();
    }
}
