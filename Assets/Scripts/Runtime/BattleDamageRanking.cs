using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace YesterdayConvoy
{
    public readonly struct BattleDamageEntry
    {
        public readonly string CardId;
        public readonly float Damage;
        public readonly float GuerrillaDamage;
        internal BattleDamageEntry(string cardId,float damage,float guerrillaDamage)
        {CardId=cardId;Damage=damage;GuerrillaDamage=guerrillaDamage;}
    }

    public sealed partial class ManualBattle
    {
        public const string SharedDamageCardId="__shared_effects";
        readonly Dictionary<string,float> cardDamage=new Dictionary<string,float>(StringComparer.Ordinal);
        readonly Dictionary<string,float> cardGuerrillaDamage=new Dictionary<string,float>(StringComparer.Ordinal);
        readonly List<BattleDamageEntry> damageRanking=new List<BattleDamageEntry>();
        ReadOnlyCollection<BattleDamageEntry> visibleDamageRanking;
        bool damageRankingDirty;
        bool creditGuerrillaDamage;
        public float TotalPlayerDamage {get;private set;}
        public float TotalGuerrillaDamage {get;private set;}
        public IReadOnlyList<BattleDamageEntry> DamageRanking
        {
            get
            {
                if(damageRankingDirty)
                {
                    damageRanking.Clear();
                    foreach(var pair in cardDamage)
                    {
                        cardGuerrillaDamage.TryGetValue(pair.Key,out float guerrillaDamage);
                        damageRanking.Add(new BattleDamageEntry(pair.Key,pair.Value,guerrillaDamage));
                    }
                    damageRanking.Sort((a,b)=>{int value=b.Damage.CompareTo(a.Damage);return value!=0?value:string.CompareOrdinal(a.CardId,b.CardId);});
                    damageRankingDirty=false;
                }
                return visibleDamageRanking??(visibleDamageRanking=damageRanking.AsReadOnly());
            }
        }
        void ResetDamageRanking()
        {
            cardDamage.Clear();cardGuerrillaDamage.Clear();damageRanking.Clear();
            TotalPlayerDamage=TotalGuerrillaDamage=0;damageRankingDirty=creditGuerrillaDamage=false;
        }
        // The existing HP-loss sink records redirected and clamped losses exactly once.
        // Mark only this synchronous attack as a subset; it must not run on-hit procs.
        void DamageGuerrillaDefenderFrom(DefenderState target,float attack,CombatDamageType type,DeployedUnitState source,float weight)
        {
            bool previous=creditGuerrillaDamage;creditGuerrillaDamage=true;
            try{DamageDefenderFrom(target,attack,type,source,true,weight);}
            finally{creditGuerrillaDamage=previous;}
        }
        // Credit only applied HP loss. Debt, blocked/erased attacks and healing are not damage.
        // Sources remain in the battle model after defeat, so their late death bursts retain credit.
        void CreditDamage(DeployedUnitState source,float amount)
        {
            if(amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return;
            string id=string.IsNullOrEmpty(source?.CardId)?SharedDamageCardId:source.CardId;
            cardDamage.TryGetValue(id,out float previous);cardDamage[id]=previous+amount;
            TotalPlayerDamage+=amount;damageRankingDirty=true;
            if(creditGuerrillaDamage)
            {
                cardGuerrillaDamage.TryGetValue(id,out float priorGuerrilla);
                cardGuerrillaDamage[id]=priorGuerrilla+amount;TotalGuerrillaDamage+=amount;
            }
        }
    }
}
