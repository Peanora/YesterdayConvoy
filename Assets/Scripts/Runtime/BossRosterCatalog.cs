using System;
using System.Collections.Generic;
using System.Linq;
namespace YesterdayConvoy
{
    // Integration values are provisional; existing seven core cards retain their authored profiles.
    public static class BossRosterCatalog
    {
        public readonly struct Entry
        {
            public readonly string Task,CardId,Name,UnitId;
            public readonly int Cost;
            public int DeploymentCost=>Task=="B10"?8:Cost+1;
            public readonly bool Melee;
            public readonly CombatDamageType DamageType;
            public readonly float Health,Attack,Armor,Range,Speed,Interval,Windup,AttackDuration;
            public Entry(string task,string name,string unit,int cost,float hp,float attack,float armor,float range,float speed,float interval,float windup,float duration,bool melee,CombatDamageType type)
            {Task=task;CardId="CARD_"+task;Name=name;UnitId=unit;Cost=cost;Health=hp;Attack=attack;Armor=armor;Range=range;Speed=speed;Interval=interval;Windup=windup;AttackDuration=duration;Melee=melee;DamageType=type;}
        }
        static readonly Entry[] entries={
            new Entry("B01","W","B01_enemy_1504_cqbw",1,290.00000000f,37.00000000f,2.00000000f,2.50000000f,0.65000000f,2.40000000f,0.30000001f,1.29999995f,false,CombatDamageType.Physical),
            new Entry("B03","弑君者","B03_enemy_1502_crowns",1,290.00000000f,37.00000000f,2.00000000f,0.85000000f,0.65000000f,2.40000000f,0.46666667f,1.00000000f,true,CombatDamageType.Physical),
            new Entry("B04","锏","B04_enemy_1525_blkswb",2,380.00000000f,49.00000000f,4.00000000f,0.85000000f,0.65000000f,2.40000000f,0.46666667f,1.60000002f,true,CombatDamageType.Physical),
            new Entry("B05","“大总统”汉科","B05_enemy_2139_shdbj2",2,380.00000000f,49.00000000f,4.00000000f,2.50000000f,0.65000000f,2.40000000f,0.33333334f,1.33333337f,false,CombatDamageType.Physical),
            new Entry("B07","爱布拉娜／“领袖”","B07_enemy_1536_ncrmcr",2,380.00000000f,49.00000000f,4.00000000f,2.00000000f,0.65000000f,2.40000000f,0.50000000f,1.16666663f,false,CombatDamageType.Arts),
            new Entry("B08","“万火归一”","B08_enemy_1563_fthlgj",4,560.00000000f,73.00000000f,8.00000000f,3.00000000f,0.65000000f,2.40000000f,0.89999998f,1.66666663f,false,CombatDamageType.Arts),
            new Entry("B09","“酒神”","B09_enemy_1568_dirctr",2,380.00000000f,49.00000000f,4.00000000f,2.50000000f,0.65000000f,3.10000000f,1.83333337f,3.00000000f,false,CombatDamageType.Arts),
            new Entry("B10","坎诺特","B10_enemy_2036_syshop",4,560.00000000f,73.00000000f,8.00000000f,0.85000000f,0.35000000f,2.40000000f,0.00000000f,0.00000000f,false,CombatDamageType.Physical),
            new Entry("B11","赫尔昏佐伦／巫王","B11_enemy_1548_ltniak",4,560.00000000f,73.00000000f,8.00000000f,2.50000000f,0.65000000f,2.40000000f,1.23333335f,2.00000000f,false,CombatDamageType.Arts),
            new Entry("B12","“阿米娅”，炉芯终曲","B12_enemy_2092_skzamy",3,470.00000000f,61.00000000f,6.00000000f,10.00000000f,0.65000000f,3.10000000f,1.50000000f,3.00000000f,false,CombatDamageType.Arts),
            new Entry("B13","“圣徒”","B13_enemy_1567_pope",4,560.00000000f,73.00000000f,8.00000000f,0.85000000f,0.65000000f,2.40000000f,1.13333333f,2.26666665f,true,CombatDamageType.Physical),
            new Entry("B14","卡德霍，黑流之源","B14_enemy_2145_cadejo",3,470.00000000f,61.00000000f,6.00000000f,2.00000000f,0.65000000f,2.40000000f,0.80000001f,1.60000002f,false,CombatDamageType.Physical),
            new Entry("B15","最后的圣愚","B15_enemy_1603_hlystp",3,470.00000000f,61.00000000f,6.00000000f,12.00000000f,0.65000000f,2.60000000f,1.00000000f,2.50000000f,false,CombatDamageType.Arts),
            new Entry("B21","“岁”（界园版）","B21_enemy_2119_dyshhj",5,650.00000000f,85.00000000f,10.00000000f,0.85000000f,0.65000000f,2.40000000f,0.00000000f,0.00000000f,true,CombatDamageType.Physical),
            new Entry("B22","狂躁异质裂兽","B22_enemy_1571_mirbst",1,290.00000000f,37.00000000f,2.00000000f,1.00000000f,0.65000000f,2.43330011f,0.63330001f,2.33330011f,true,CombatDamageType.Physical),
            new Entry("B23","安纳斯塔西奥，赎罪者","B23_enemy_1561_crzjdg",3,470.00000000f,61.00000000f,6.00000000f,1.80000000f,0.65000000f,2.40000000f,0.50000000f,1.66666663f,false,CombatDamageType.Physical),
            new Entry("B24","不死的黑蛇（2026版）","B24_enemy_1602_blksnk",3,470.00000000f,61.00000000f,6.00000000f,1.00000000f,0.65000000f,3.10000000f,1.56666672f,3.00000000f,false,CombatDamageType.Physical),
            new Entry("B25","圣徒瓦列里奥","B25_enemy_1594_presaint",3,470.00000000f,61.00000000f,6.00000000f,2.50000000f,0.65000000f,2.40000000f,0.56666666f,1.33333337f,true,CombatDamageType.Physical),
            new Entry("B26","易","B26_enemy_2118_dylbhm",4,560.00000000f,73.00000000f,8.00000000f,2.00000000f,0.65000000f,2.40000000f,0.76666665f,1.50000000f,false,CombatDamageType.Arts),
        };
        static readonly Dictionary<string,Entry> byId=entries.ToDictionary(x=>x.CardId,StringComparer.Ordinal);
        public static IReadOnlyList<Entry> Entries=>Array.AsReadOnly(entries);
        public static bool Contains(string cardId)=>cardId!=null&&byId.ContainsKey(cardId);
        public static bool TryGet(string cardId,out Entry entry){entry=default;return cardId!=null&&byId.TryGetValue(cardId,out entry);}
    }
}
