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
            Defense("1-1", "奖励", 1f, .65f, "D06 D04", "D07"),
            Defense("1-2", "奖励", 1f, .70f, "D06 D05", "D07 D09"),
            Defense("1-3", "战斗战备", 1.10f, .80f, "D02 D06 D04", "D07 D08"),
            Defense("1-4", "战斗", 1.64f, .95f, "D01 D05", "D07 D09 D10"),
            Supply("1-5"),
            Defense("1-6", "战斗", 2.20f, 1.19f, "D22 D01 D04", "D08 D09 D12"),
            Defense("1-7", "遭遇", 2.85f, 1.25f, "D02 D04 D05", "D07 D09 D11"),
            Defense("1-8", "奖励", 1f, .80f, "D06 D04 D05", "D07 D09"),
            Defense("1-9", "首领", 3.25f, 1.32f, "D25 D01 D05", "D10 D07 D09"),
            Defense("2-1", "战斗", 5.10f, 1.54f, "D01 D05 D04", "D24 D08 D12"),
            Defense("2-2", "战斗战备", 6.60f, 1.70f, "D22 D03 D33", "D07 D08 D10"),
            Supply("2-3"),
            Defense("2-4", "战斗", 9.60f, 1.88f, "D02 D01 D04", "D23 D07 D08 D12"),
            Defense("2-5", "遭遇", 13f, 2.03f, "D01 D34 D05", "D27 D07 D09 D11"),
            Defense("2-6", "奖励", 1f, 1.20f, "D06 D04 D05", "D07 D08 D09"),
            Defense("2-7", "首领", 17f, 2.20f, "D29 D01 D04 D05", "D07 D08 D09 D12"),
            Defense("3-1", "战斗", 27f, 2.58f, "D01 D30 D04 D05", "D21 D12 D09 D07"),
            Defense("3-2", "战斗战备", 36f, 2.76f, "D26 D01 D04 D06", "D07 D09 D35 D12"),
            Supply("3-3"),
            Defense("3-4", "战斗", 48f, 2.99f, "D22 D01 D05 D32", "D28 D07 D08 D12 D11"),
            Defense("3-5", "遭遇", 60f, 3.18f, "D36 D01 D02 D04", "D09 D31 D10 D12 D08"),
            Defense("3-6", "奖励", 1f, 1.80f, "D06 D04 D05", "D07 D08 D09"),
            Defense("3-7", "首领", 1f, 1f, "D27", "", 150f),
        };

        public const string SupernovaBossNodeId = "WIS-BOSS";
        public static readonly BossDefinition SupernovaBoss = new BossDefinition
        {
            NodeId = SupernovaBossNodeId, ArtId = "D31", Name = "维什戴尔 · 超新星",
            Health = 220000, Attack = 145, Armor = 30, MagicResistance = 25,
            Interval = 1.05f, Range = 5.5f,
            Effect = "第一阶段「饱和复仇」同时攻击最多三名目标，随后切入过载四连射。击破首段生命后切换至第二阶段「爆裂黎明」，每轮发射六枚范围炮弹。击倒首领后进入蓝门。"
        };

        public const string MakotoBossNodeId = "MAKOTO-BOSS";
        internal const string MakotoBossArtId = "D36";
        public static readonly BossDefinition MakotoBoss = new BossDefinition
        {
            NodeId = MakotoBossNodeId, ArtId = MakotoBossArtId, Name = "结城理 · 见证荣光",
            Health = 120000, Attack = 125, Armor = 30, MagicResistance = 25,
            Interval = 1.7f, Range = 4.5f,
            Effect = "「开辟明日的剑刃」分三段：本体近身挥剑，塔纳托斯高速攻击弱点，俄耳甫斯守备。"
                + "守备期间物理与法术减伤12%，每秒回复800点生命，最多8次。击倒后进入蓝门。"
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
                Health = 500000, Attack = 155, Armor = 35, MagicResistance = 30,
                Interval = 1.5f, Range = 4.25f, InitialSP = 0, RequiredSP = 0, ActionSeconds = 1,
                Effect = "首领周围的低地积雪逐层加深。第一阶段「霜涛覆岭」的命中让积雪扩散并持续造成法伤。第一段生命耗尽时，初雪和全场进攻方冻结2秒。随后恢复满血，以更快攻速持续施放「群山俯首」，直至被击倒。"
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
                "普攻的浮游单元追加法伤，对同一目标逐次提高。接敌后12秒首次开技，三个浮游单元独立追猎40秒；每秒最多对附近两个目标结算75%攻击力法伤，重复范围不叠加。技能结束后30秒回转。", 18, 30),
            new GuardDefinition("D22", "余", 2, 6,
                "进攻方进入3.2格范围后展开火墙，持续6秒。穿墙的合格敌方弹道伤害降低15%，每22秒可重新展开。", 22, 22),
            new GuardDefinition("D23", "Mon3tr", 5, 6,
                "最多链疗3名守军，每次跳转治疗量乘75%。完成6次有效治疗且预留落点内有合法目标后，转形进攻10秒，每战一次。", 0, 6),
            new GuardDefinition("D24", "新约能天使", 3, 6,
                "接敌后10秒首次开技，随后每25秒一次。4轮连射，每轮5发，每发55%攻击力物伤并独立结算护甲。", 15, 25),
            new GuardDefinition("D25", "斩业星熊", 2, 6),
            new GuardDefinition("D26", "凛御银灰", 1, 6,
                "接敌后12秒首次开技，持续48秒。期间连续向目标挥出冰刃，每次贯穿最多3人，各造成240%攻击力物伤。技能结束后24秒回转。", 12, 24),
            new GuardDefinition("D27", "圣聆初雪", 6, 6,
                "接敌后10秒生成半径2格、持续8秒的雪区。每秒造成60%攻击力法伤并减速25%，同时至多存在一片雪区。", 14, 24),
            new GuardDefinition("D28", "望", 6, 6,
                "进攻方接近落子区域后，天下劫先落中心棋，再每0.25秒向四邻落一颗。五颗落齐后按落子顺序引爆。每颗对1.4格内最多3人造成180%攻击力法伤并减速2秒；收盘后24秒可再布。", 24, 24),
            new GuardDefinition("D29", "塞雷娅", 2, 6),
            new GuardDefinition("D30", "玛恩纳", 1, 6,
                "未照耀的荣光持续26秒，技能中每1.2秒挥砍一次，每次攻击范围内最多5人，平时收剑蓄力。", 20, 45),
            new GuardDefinition("D31", "维什戴尔 · 超新星", 3, 6,
                "超新星攻擂首领。炮弹造成范围伤害，生命低于一半时提高攻速。", 0, 0),
            new GuardDefinition("D32", "赤刃明霄陈", 1, 6,
                "赤霄·天喟持续20秒。向固定方向对最多3个地面目标连续斩击，每轮3段。龙形剑气贴地移动并沿障碍转向。", 15, 28),
            new GuardDefinition("D33", "予愿安洁莉娜", 1, 6,
                "第三技能进入飞行姿态，在原19格范围内最多索敌三人，31发结束。", 22, 36),
            new GuardDefinition("D34", "克莱门莎", 1, 6,
                "第三技能攻击正前方四格内最多三人。腐蚀累积触发十字爆发，爆发消耗弹药。", 28, 45),
            new GuardDefinition("D35", "凯尔希·思衡托", 5, 6,
                "第二技能发射10枚范围药弹，造成伤害或治疗，落地后追加相反效果。", 20, 35),
            new GuardDefinition(MakotoBossArtId, MakotoBoss.Name, 1, 6, MakotoBoss.Effect),
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
            if (nodeId == "3-5" && artId == SupernovaBoss.ArtId)
                return "两段生命。首段「饱和复仇」由最多三目标齐射转入过载四连发。转段后发动「爆裂黎明」，每轮发射六枚范围炮弹。";
            if (nodeId == "3-5" && artId == MakotoBossArtId)
                return "三段生命。依次以竖琴远射与自疗、囚锁恐惧与处决、剑刃弱点打击迎战。末段余血45%时转守备，减伤12%并限次自疗。";
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
