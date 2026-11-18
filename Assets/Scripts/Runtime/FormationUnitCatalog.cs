using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>
    /// Shared deployment/presentation metadata for the existing authored roster. This is not a
    /// migration of original skills. Source-size queries read metadata only, never pixel pages.
    /// </summary>
    public static class FormationUnitCatalog
    {
        public sealed class Definition
        {
            public string CardId { get; }
            public string ArtId { get; }
            public string ArtRoot { get; }
            public string Name { get; }
            public int Cost { get; }
            public Vector2Int Footprint { get; }
            readonly float fallbackVisualHeight;
            public float VisualHeight
            {
                get
                {
                    var original=BossRosterArt.For(CardId);
                    if(original!=null)
                        // Sui is a stage-sized set piece: preserve its proportions at an
                        // explicit six-unit formation cap rather than normalizing by rarity.
                        return original.Task=="B21"?Mathf.Min(6,original.Height):original.Height;
                    if(ArtRoot!=null)
                    {
                        var source=ArtCatalog.Read(ArtRoot);
                        if(source.Height>0)return source.Height;
                        if(source.Clips.TryGetValue("idle",out var idle))return idle.Height/idle.Ppu;
                    }
                    return fallbackVisualHeight;
                }
            }
            // Existing formation prototypes: 0 tank, 1 dog, 2 fighter, 3 bow, 4 caster, 5 support.
            public int TemplateKind { get; }
            public CampaignBalance.Stats Stats { get; }
            public bool ShopEligible { get; }

            internal Definition(string cardId, string name, string artRoot, int templateKind,
                float visualHeight, Vector2Int footprint, bool supportOnly = false, string artIdOverride = null)
            {
                CardId = cardId;
                ArtId = artIdOverride ?? (supportOnly ? "GOLEM" : cardId.Substring(5));
                ArtRoot = artRoot;
                Name = name;
                TemplateKind = templateKind;
                fallbackVisualHeight = visualHeight;
                Footprint = footprint;
                if (!supportOnly)
                {
                    CampaignBalance.Profiles.TryGetValue(cardId, out var stats);
                    var rules = FormationCampaignRules.Find(cardId);
                    if (stats == null && rules != null)
                        stats = new CampaignBalance.Stats(rules.Cost, rules.Cost + 1, rules.Health,
                            rules.Attack, rules.Armor, rules.Interval, rules.Range, rules.MoveSpeed, 1);
                    if (stats == null) throw new InvalidOperationException("Missing authored formation profile: " + cardId);
                    Stats = stats;
                    Cost = stats.Cost;
                    ShopEligible = rules != null || CampaignBalance.ShopEligible(cardId);
                }
                // GOLEM is only presentation metadata for an existing synergy support, not a card
                // role. Its stage-based stats remain owned by the existing summon rules (Stats=null).
            }
        }

        static readonly ReadOnlyCollection<Definition> all;
        static readonly Dictionary<string,Definition> byId = new Dictionary<string,Definition>(StringComparer.Ordinal);
        public static IReadOnlyList<Definition> All => all;

        static FormationUnitCatalog()
        {
            var list = new List<Definition>();
            Add(list,"M02","猎狗",1,.90f);
            Add(list,"M03","游击队猎犬",1,.90f);
            Add(list,"M04","高能源石虫",1,.78f);
            Add(list,"M05","酸液源石虫",4,.82f);
            Add(list,"M06","冰爆源石虫",1,.82f);
            Add(list,"M07","士兵",2,1.12f);
            Add(list,"M08","弩手",3,1.30f);
            Add(list,"M09","术师",4,1.10f);
            Add(list,"M10","重装防御者",0,1.20f);
            Add(list,"M11","雪怪狙击手",3,1.34f);
            Add(list,"M12","雪怪术师",4,1.34f);
            Add(list,"M13","雪怪小队凿冰人",2,1.30f);
            Add(list,"M14","霜星",4,1.75f);
            Add(list,"M15","游击队战士",2,1.31f);
            Add(list,"M16","游击队狙击手",3,1.33f);
            Add(list,"M17","游击队传令兵",5,1.50f);
            Add(list,"M18","游击队盾卫",0,1.43f);
            Add(list,"M19","游击队萨卡兹战士",2,1.37f);
            Add(list,"M20","爱国者",0,1.90f,2);
            Add(list,"M21","宿主士兵",2,1.33f);
            Add(list,"M22","宿主拾荒者",2,1.43f);
            Add(list,"M23","宿主流浪者",2,1.43f);
            Add(list,"M24","萨卡兹宿主百夫长",0,1.55f);
            Add(list,"M25","梅菲斯特",5,1.46f);
            Add(list,"M26","泥岩小队掷能者",4,1.52f);
            Add(list,"M27","泥岩小队践行者",0,1.38f);
            Add(list,"M28","泥岩",0,1.85f,2);
            Add(list,"M29","深池方阵战士",0,1.50f);
            Add(list,"M30","深池逐火战士",2,1.42f);
            Add(list,"M31","深池焚毁者",4,1.60f);
            Add(list,"M32","蔓德拉",4,1.44f);
            Add(list,"M33","囊海爬行者",0,1.52f);
            Add(list,"M34","钵海收割者",2,1.90f,2);
            Add(list,"M35","壳海狂奔者",1,1.10f);
            Add(list,"M36","首言者",3,1.95f,2);
            Add(list,"M37","萨卡兹大剑手",2,1.46f);
            Add(list,"M38","萨卡兹术师",4,1.86f);
            Add(list,"M41","银狼 LV.999",4,1.50f);
            Add(list,"M42","泥岩小队掷能者组长",4,1.42f);
            Add(list,"M43","泥岩小队践行者组长",0,1.28f);
            AddSpecial(list,"CARD_ORIGINIUM_SLUG","源石虫","OriginiumSlugOriginal",1,1.0f);
            AddSpecial(list,"CARD_TALULAH_TRIAL","塔露拉",null,4,1.85f);
            // Prefer original game#9 metadata through BossRosterArt; the approved user GIF
            // Sprite references remain the fallback if that original package is absent.
            AddSpecial(list,"CARD_WISADEL_APRIL","维维美",null,3,292f/173.333333f);
            foreach (var role in BossRosterCatalog.Entries)
            {
                var rules = FormationCampaignRules.Find(role.CardId);
                if (rules == null) continue;
                bool support = rules.Role.Contains("辅助");
                int kind = support ? 5 : rules.DamageType == "法" || rules.DamageType == "真" ? 4 : rules.Range > 2 ? 3 : 2;
                var definition = new Definition(role.CardId, rules.Name, "BossRoster/" + role.Task + "/main", kind,
                    1.5f, rules.Size == "重" ? new Vector2Int(2,2) : Vector2Int.one);
                list.Add(definition); Register(definition);
            }
            // Historical metadata remains available to simulations. Only the
            // official challenge roster owns authored card and art references.
            list.RemoveAll(definition=>FormationCampaignRules.Find(definition.CardId)==null);
            all = list.AsReadOnly();
            if (all.Count != FormationCampaignRules.AllUnits.Length)
                throw new InvalidOperationException("Formation art must cover the current challenge roster.");
            Register(new Definition(CampaignBalance.GolemCardId,"泥岩巨像","MonsterCombatArt/GOLEM",0,2.0f,new Vector2Int(2,2),true));
            Register(new Definition("AUX_P14H","卡德霍猎犬","BossRoster/P14H/main",1,.90f,Vector2Int.one,true,"P14H"));
            Register(new Definition("AUX_P26","易的梁","BossRoster/P26/main",4,1.15f,Vector2Int.one,true,"P26"));
        }

        static void Add(List<Definition> list, string artId, string name, int templateKind, float visualHeight, int size = 1)
        {
            var definition = new Definition("CARD_"+artId,name,"MonsterCombatArt/"+artId,templateKind,visualHeight,new Vector2Int(size,size));
            list.Add(definition); Register(definition);
        }

        static void AddSpecial(List<Definition> list, string cardId, string name, string root, int kind, float height)
        {
            var definition = new Definition(cardId,name,root,kind,height,Vector2Int.one);
            list.Add(definition); Register(definition);
        }

        static void Register(Definition definition)
        {
            byId.Add(definition.CardId,definition);
            byId.Add(definition.ArtId,definition);
        }

        public static Definition Find(string artOrCardId)
        {
            if (string.IsNullOrEmpty(artOrCardId)) return null;
            return byId.TryGetValue(artOrCardId,out var definition) ? definition : null;
        }
    }
}
