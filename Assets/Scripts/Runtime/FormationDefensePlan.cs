using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public static partial class FormationCampaignRules
    {
        public sealed class NodeDefinition
        {
            public string Id, Kind;
            public int Plane, BaseGold, XP;
            public float HealthScale, AttackScale, TimeLimit;
            internal string[] Front = Array.Empty<string>(), Rear = Array.Empty<string>();
            public int DefenderCount => Front.Length + Rear.Length;
        }

        public sealed class BossDefinition
        {
            public string NodeId, ArtId, Name, Effect;
            public float Health, Attack, Armor, MagicResistance, Interval, Range, InitialSP, RequiredSP, ActionSeconds;
        }

        public sealed class GuardDefinition
        {
            public string ArtId { get; }
            public string Name { get; }
            public int Role { get; }
            public int Rarity { get; }
            public string Effect { get; }
            internal float? InitialSP { get; }
            internal float? RequiredSP { get; }

            internal GuardDefinition(string artId, string name, int role, int rarity = 3, string effect = null,
                float? initialSP = null, float? requiredSP = null)
            {
                ArtId = artId;
                Name = name;
                Role = role;
                Rarity = rarity;
                Effect = effect;
                InitialSP = initialSP;
                RequiredSP = requiredSP;
            }
        }

        internal sealed class GuardFormation
        {
            public Vector2Int[] Front { get; }
            public Vector2Int[] Rear { get; }
            readonly Vector2Int reconstruction, chess;

            public GuardFormation(Vector2Int[] front, Vector2Int[] rear, Vector2Int reconstruction, Vector2Int chess)
            {
                Front = front;
                Rear = rear;
                this.reconstruction = reconstruction;
                this.chess = chess;
            }

            public Vector2Int? SkillCell(string artId) => artId switch
            {
                "D23" => reconstruction,
                "D28" => chess,
                _ => null,
            };
        }

        static Vector2Int GuardCell(int columnsFromGate, int row) =>
            new Vector2Int(FormationBattleModel.Width - 1 - columnsFromGate, row);

        internal static readonly GuardFormation NorthGuardFormation = new GuardFormation(
            new[] { GuardCell(6, 2), GuardCell(6, 3), GuardCell(6, 1), GuardCell(5, 2) },
            new[] { GuardCell(4, 2), GuardCell(4, 1), GuardCell(3, 2), GuardCell(3, 1), GuardCell(3, 3) },
            GuardCell(5, 1), GuardCell(5, 3));

        internal static readonly GuardFormation SouthGuardFormation = new GuardFormation(
            new[] { GuardCell(5, 5), GuardCell(5, 6), GuardCell(5, 7), GuardCell(4, 5) },
            new[] { GuardCell(3, 5), GuardCell(3, 6), GuardCell(2, 5), GuardCell(2, 6), GuardCell(2, 7) },
            GuardCell(4, 6), GuardCell(4, 7));

        public static readonly NodeDefinition[] Nodes =
        {
            Defense("1-1", "奖励", .42f, .65f, "D06 D04", "D07"),
            Defense("1-2", "奖励", .55f, .70f, "D01 D05", "D07 D09"),
            Defense("1-3", "战斗战备", 1.05f, .84f, "D02 D06 D04", "D07 D08"),
            Defense("1-4", "战斗", 1.55f, 1f, "D01 D05", "D07 D09 D10"),
            Supply("1-5"),
            Defense("1-6", "战斗", 2f, 1.17f, "D22 D01 D04", "D08 D09 D12"),
            Defense("1-7", "遭遇", 2.65f, 1.24f, "D02 D04 D05", "D07 D09 D11"),
            Defense("1-8", "奖励", 1.30f, .80f, "D06 D04 D05", "D07 D09"),
            Defense("1-9", "首领", 2.90f, 1.32f, "D25 D01 D05", "D10 D07 D09"),
            Defense("2-1", "战斗", 4.60f, 1.54f, "D01 D05 D04", "D24 D08 D12"),
            Defense("2-2", "战斗战备", 5.80f, 1.70f, "D22 D03", "D09 D07 D08 D10"),
            Supply("2-3"),
            Defense("2-4", "战斗", 8.20f, 1.88f, "D02 D01 D04", "D23 D07 D08 D12"),
            Defense("2-5", "遭遇", 10.40f, 2.03f, "D01 D06 D05", "D27 D07 D09 D11"),
            Defense("2-6", "奖励", 5f, 1.20f, "D06 D04 D05", "D07 D08 D09"),
            Defense("2-7", "首领", 15f, 2.20f, "D29 D01 D04 D05", "D07 D08 D09 D12"),
            Defense("3-1", "战斗", 21f, 2.58f, "D01 D02 D04 D05", "D21 D12 D09 D07"),
            Defense("3-2", "战斗战备", 29f, 2.76f, "D26 D01 D04 D06", "D07 D09 D10 D12"),
            Supply("3-3"),
            Defense("3-4", "战斗", 35f, 2.99f, "D22 D01 D05 D06", "D28 D07 D08 D12 D11"),
            Defense("3-5", "遭遇", 42f, 3.18f, "D26 D01 D02 D04", "D24 D09 D10 D12 D08"),
            Defense("3-6", "奖励", 16f, 1.80f, "D06 D04 D05", "D07 D08 D09"),
            Defense("3-7", "首领", 1f, 1f, "D27", "", 150f),
        };

        public static readonly BossDefinition[] Bosses =
        {
            new BossDefinition
            {
                NodeId = "1-9", ArtId = "D25", Name = "斩业星熊",
                Health = 19000, Attack = 165, Armor = 35, MagicResistance = 20,
                Interval = 2.6f, Range = 1.5f, InitialSP = 12, RequiredSP = 24, ActionSeconds = 1,
                Effect = "获得10%最大生命护盾6秒。反伤实际所受伤害的15%，每秒最多反伤自身攻击力的100%。50%生命转段，攻速提高20%。"
            },
            new BossDefinition
            {
                NodeId = "2-7", ArtId = "D29", Name = "塞雷娅",
                Health = 145000, Attack = 300, Armor = 40, MagicResistance = 40,
                Interval = 2.8f, Range = 2.5f, InitialSP = 10, RequiredSP = 24, ActionSeconds = 1,
                Effect = "治疗生命最低的两名守军各4%最大生命，自身获得5%最大生命护盾8秒。50%生命后，技能同时降低进攻方20%受治疗效果8秒。"
            },
            new BossDefinition
            {
                NodeId = "3-7", ArtId = "D27", Name = "圣聆初雪",
                Health = 500000, Attack = 230, Armor = 35, MagicResistance = 30,
                Interval = 3.2f, Range = 4.25f, InitialSP = 0, RequiredSP = 0, ActionSeconds = 1,
                Effect = "第一阶段「霜涛覆岭」，攻击落点积雪并向邻格扩散，雪地持续造成法伤。第一段生命耗尽时冻结全场进攻方2秒，随后以完整的第二段生命释放「群山俯首」。第二段生命耗尽后才算消灭。"
            },
        };

        static readonly IReadOnlyDictionary<string, GuardDefinition> Guards = new[]
        {
            new GuardDefinition("D01", "米格鲁", 2),
            new GuardDefinition("D02", "卡缇", 2),
            new GuardDefinition("D03", "斑点", 2),
            new GuardDefinition("D04", "玫兰莎", 1),
            new GuardDefinition("D05", "月见夜", 1),
            new GuardDefinition("D06", "翎羽", 0),
            new GuardDefinition("D07", "克洛丝", 3),
            new GuardDefinition("D08", "史都华德", 4),
            new GuardDefinition("D09", "炎熔", 4),
            new GuardDefinition("D10", "芙蓉", 5),
            new GuardDefinition("D11", "安赛尔", 5),
            new GuardDefinition("D12", "梓兰", 6),
            new GuardDefinition("D21", "荒芜拉普兰德", 4, 6,
                "接敌后12秒首次派出浮游单元，随后每30秒一次。追猎8秒，每秒造成75%攻击力法伤，最多波及半径1.5格内两人。", 18, 30),
            new GuardDefinition("D22", "余", 2, 6,
                "进攻方进入3.2格范围后展开火墙，持续6秒。穿墙的合格敌方弹道伤害降低15%，每22秒可重新展开。", 22, 22),
            new GuardDefinition("D23", "Mon3tr", 5, 6,
                "最多链疗3名守军，每次跳转治疗量乘75%。完成6次有效治疗且预留落点内有合法目标后，转形进攻10秒，每战一次。", 0, 6),
            new GuardDefinition("D24", "新约能天使", 3, 6,
                "接敌后10秒首次开技，随后每25秒一次。4轮连射，每轮5发，每发55%攻击力物伤并独立结算护甲。", 15, 25),
            new GuardDefinition("D25", "斩业星熊", 2, 6),
            new GuardDefinition("D26", "凛御银灰", 1, 6,
                "接敌后12秒首次开技，随后每24秒一次。向宽2格的直线打击最多3人，各造成240%攻击力物伤。", 12, 24),
            new GuardDefinition("D27", "圣聆初雪", 6, 6,
                "接敌后10秒生成半径2格、持续8秒的雪区。每秒造成60%攻击力法伤并减速25%，同时至多存在一片雪区。", 14, 24),
            new GuardDefinition("D28", "望", 6, 6,
                "预留格上的棋子触发时打击半径1.4格内最多3人，各造成180%攻击力法伤并减速2秒；每24秒可重设一枚。", 24, 24),
            new GuardDefinition("D29", "塞雷娅", 2, 6),
            new GuardDefinition("D30", "玛恩纳", 1, 6),
        }.ToDictionary(guard => guard.ArtId);

        static readonly float[] EncounterHealth = { 1f, 1.7f, 3f, 5.5f };
        static readonly float[] EncounterAttack = { 1f, 1.08f, 1.18f, 1.30f };

        public static float EncounterHealthScale(int tier) => EncounterHealth[Mathf.Clamp(tier, 1, EncounterHealth.Length) - 1];
        public static float EncounterAttackScale(int tier) => EncounterAttack[Mathf.Clamp(tier, 1, EncounterAttack.Length) - 1];

        public static GuardDefinition DefenderByArtId(string artId) =>
            artId != null && Guards.TryGetValue(artId, out var guard) ? guard : null;

        public static string DefenderSkillDescription(string nodeId, string artId)
        {
            if (nodeId == "1-6" && artId == "D22")
                return "进攻方首次进入3.2格范围后展开火墙，持续6秒，每战一次。穿墙的合格敌方弹道伤害降低15%。";
            var boss = Bosses.FirstOrDefault(guard => guard.NodeId == nodeId && guard.ArtId == artId);
            if (boss != null) return boss.Effect;
            var defender = DefenderByArtId(artId);
            return defender == null ? null : defender.Effect ?? Defenders[defender.Role].Effect;
        }

        static NodeDefinition Defense(string id, string kind, float health, float attack, string front, string rear, float limit = 0)
        {
            return new NodeDefinition
            {
                Id = id,
                Kind = kind,
                Plane = id[0] - '0',
                BaseGold = 5,
                XP = 2,
                HealthScale = health,
                AttackScale = attack,
                TimeLimit = limit > 0 ? limit : kind == "奖励" ? float.PositiveInfinity : kind == "首领" ? 120 : 90,
                Front = front.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries),
                Rear = rear.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries),
            };
        }

        static NodeDefinition Supply(string id) => new NodeDefinition
        {
            Id = id, Kind = "补给", Plane = id[0] - '0', HealthScale = 1, AttackScale = 1, TimeLimit = 90,
        };

        internal static float RoundedGuardStat(float baseStat, float scale, decimal eliteScale) =>
            (float)Math.Round((decimal)baseStat * (decimal)scale * eliteScale, MidpointRounding.AwayFromZero);
    }
}
