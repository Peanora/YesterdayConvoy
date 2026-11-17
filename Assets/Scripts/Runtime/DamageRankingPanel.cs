using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        struct DamageRankingRow
        {
            internal string CardId;
            internal float Damage;
        }
        readonly List<DamageRankingRow> damageRankingRows=new List<DamageRankingRow>();
        ManualBattle damageRankingBattle;
        Vector2 damageRankingScroll;
        int damageRankingFrame=-1;
        GUIStyle damageRankingScrollbar;

        void RefreshDamageRankingRows()
        {
            if(damageRankingBattle==Battle&&damageRankingFrame==Time.frameCount)return;
            if(damageRankingBattle!=Battle){damageRankingScroll=Vector2.zero;damageRankingBattle=Battle;}
            damageRankingFrame=Time.frameCount;damageRankingRows.Clear();
            foreach(var card in Battle.Cards)damageRankingRows.Add(new DamageRankingRow{CardId=card.Settings.CardId});
            if(damageRankingRows.Count==0)damageRankingRows.Add(new DamageRankingRow{CardId=Battle.Settings.CardId});
            foreach(var entry in Battle.DisplayDamageRanking)
            {
                int index=damageRankingRows.FindIndex(row=>row.CardId==entry.CardId);
                var row=new DamageRankingRow{CardId=entry.CardId,Damage=entry.Damage};
                if(index>=0)damageRankingRows[index]=row;else damageRankingRows.Add(row);
            }
            damageRankingRows.Sort((a,b)=>{int order=b.Damage.CompareTo(a.Damage);return order!=0?order:string.CompareOrdinal(a.CardId,b.CardId);});
        }
        static string RankingDamageText(float amount)=>amount.ToString(amount>0&&amount<1?"0.##":"N0",CultureInfo.InvariantCulture);

        void DrawDamageRanking()
        {
            if(!DamageRankingVisible||Battle==null)return;
            RefreshDamageRankingRows();
            var rows=new List<ApprovedShopWidgets.RankingRow>();
            foreach(var entry in damageRankingRows)
            {
                bool shared=entry.CardId==ManualBattle.SharedDamageCardId,pursuit=entry.CardId==ManualBattle.GuerrillaDamageCardId,golem=entry.CardId==CampaignBalance.GolemCardId;
                string name=pursuit?"游击队追击":shared?"羁绊 / 场地":golem?"泥岩巨像":"支援单位";Sprite portrait=null;
                if(shopDefinitions.TryGetValue(entry.CardId,out var role)){name=role.Card.DisplayName;portrait=OriginalTalulahArt.CardPortrait(role.Card);}
                rows.Add(new ApprovedShopWidgets.RankingRow{CardId=entry.CardId,Name=name,Damage=entry.Damage,Portrait=portrait,Shared=shared,Pursuit=pursuit,Golem=golem});
            }
            string state=Battle.Phase==BattlePhase.Ready?"待开战":Battle.IsPaused?"已暂停":Battle.Phase==BattlePhase.Running?"实时统计":"本场结算";
            ApprovedShopWidgets.DrawRanking(rows,Battle.TotalPlayerDamage,state,scene.InterfaceFont,ref damageRankingScroll,ref damageRankingScrollbar);
        }
    }
}
