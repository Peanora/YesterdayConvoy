using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace YesterdayConvoy
{
    public readonly struct BattleDamageEntry
    {
        public readonly string CardId;
        public readonly float Damage;
        internal BattleDamageEntry(string cardId,float damage){CardId=cardId;Damage=damage;}
    }

    public sealed partial class ManualBattle
    {
        public const string SharedDamageCardId="__shared_effects";
        readonly Dictionary<string,float> cardDamage=new Dictionary<string,float>(StringComparer.Ordinal);
        readonly List<BattleDamageEntry> damageRanking=new List<BattleDamageEntry>();
        ReadOnlyCollection<BattleDamageEntry> visibleDamageRanking;
        bool damageRankingDirty;
        public float TotalPlayerDamage {get;private set;}
        public IReadOnlyList<BattleDamageEntry> DamageRanking
        {
            get
            {
                if(damageRankingDirty)
                {
                    damageRanking.Clear();
                    foreach(var pair in cardDamage)damageRanking.Add(new BattleDamageEntry(pair.Key,pair.Value));
                    damageRanking.Sort((a,b)=>{int value=b.Damage.CompareTo(a.Damage);return value!=0?value:string.CompareOrdinal(a.CardId,b.CardId);});
                    damageRankingDirty=false;
                }
                return visibleDamageRanking??(visibleDamageRanking=damageRanking.AsReadOnly());
            }
        }
        void ResetDamageRanking(){cardDamage.Clear();damageRanking.Clear();TotalPlayerDamage=0;damageRankingDirty=false;}
        // Credit only applied HP loss. Debt, blocked/erased attacks and healing are not damage.
        // Sources remain in the battle model after defeat, so their late death bursts retain credit.
        void CreditDamage(DeployedUnitState source,float amount)
        {
            if(amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return;
            string id=string.IsNullOrEmpty(source?.CardId)?SharedDamageCardId:source.CardId;
            cardDamage.TryGetValue(id,out float previous);cardDamage[id]=previous+amount;
            TotalPlayerDamage+=amount;damageRankingDirty=true;
        }
    }
}
