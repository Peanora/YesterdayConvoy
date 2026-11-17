using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>
    /// Shared deployment/presentation metadata for the existing authored roster. This is not a
    /// migration of original skills. Looking up metadata never opens or warms animation packages.
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
            public float VisualHeight { get; }
            // Existing formation prototypes: 0 tank, 1 dog, 2 fighter, 3 bow, 4 caster, 5 support.
            public int TemplateKind { get; }
            public CampaignBalance.Stats Stats { get; }
            public bool ShopEligible { get; }

            internal Definition(string cardId, string name, string artRoot, int templateKind,
                float visualHeight, Vector2Int footprint, bool supportOnly = false)
            {
                CardId = cardId;
                ArtId = supportOnly ? "GOLEM" : cardId.Substring(5);
                ArtRoot = artRoot;
                Name = name;
                TemplateKind = templateKind;
                VisualHeight = visualHeight;
                Footprint = footprint;
                if (!supportOnly)
                {
                    if (!CampaignBalance.Profiles.TryGetValue(cardId, out var stats))
                        throw new InvalidOperationException("Missing authored formation profile: " + cardId);
                    Stats = stats;
                    Cost = stats.Cost;
                    ShopEligible = CampaignBalance.ShopEligible(cardId);
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
            AddSpecial(list,"CARD_TALULAH_TRIAL","塔露拉","TalulahOriginal",4,1.85f);
            // The approved Wisadel package is serialized Sprite frames, not an ArtCatalog root.
            AddSpecial(list,"CARD_WISADEL_APRIL","维维美",null,3,1.40f);
            all = list.AsReadOnly();
            if (all.Count != 43) throw new InvalidOperationException("Existing formation roster must cover 43 authored cards.");
            Register(new Definition(CampaignBalance.GolemCardId,"泥岩巨像","MonsterCombatArt/GOLEM",0,2.0f,new Vector2Int(2,2),true));
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
