using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        readonly HashSet<string> guerrillaTransfers=new HashSet<string>(StringComparer.Ordinal);
        ShopTrialModel guerrillaTransferShop;
        Vector2 guerrillaTransferScroll;
        const float GuerrillaTransferRowHeight=42;

        public static Rect GuerrillaTransferTabRect=>new Rect(604,200,180,28);
        public static Rect GuerrillaTransferListRect=>new Rect(268,290,690,188);
        public bool GuerrillaTransfersVisible=>CampaignActive&&campaignConstructOpen&&campaignConstructTab==2;
        bool GuerrillaTransfersEditable=>CampaignActive&&Loop.CanShop&&Battle!=null&&Battle.Phase==BattlePhase.Ready&&!Battle.IsPaused&&!ArtLoading;

        public IReadOnlyList<string> GuerrillaTransferIds
        {
            get { PruneGuerrillaTransfers();return Array.AsReadOnly(guerrillaTransfers.OrderBy(id=>id,StringComparer.Ordinal).ToArray()); }
        }
        public IReadOnlyList<string> PreparedGuerrillaTransferIds
            =>Array.AsReadOnly(GuerrillaTransferIds.Where(shopLineup.Contains).ToArray());
        public IReadOnlyList<string> GuerrillaTransferCandidateIds
        {
            get
            {
                PruneGuerrillaTransfers();
                return Array.AsReadOnly(shop==null?Array.Empty<string>():shop.Bench.Select(p=>p.Id).Distinct()
                    .Where(id=>!CampaignBalance.Guerrilla(id)).OrderBy(id=>id,StringComparer.Ordinal).ToArray());
            }
        }
        public bool IsGuerrillaTransfer(string cardId)
        {PruneGuerrillaTransfers();return cardId!=null&&guerrillaTransfers.Contains(cardId);}

        void ResetGuerrillaTransfers()
        {guerrillaTransfers.Clear();guerrillaTransferShop=shop;guerrillaTransferScroll=Vector2.zero;}
        void PruneGuerrillaTransfers()
        {
            // Selection belongs to one challenge's shop, not to a serialized card or a battle entity.
            // Getters may remove stale selections, but never rebuild or mutate the current battle.
            if(!ReferenceEquals(guerrillaTransferShop,shop))ResetGuerrillaTransfers();
            if(shop==null){guerrillaTransfers.Clear();return;}
            guerrillaTransfers.RemoveWhere(id=>CampaignBalance.Guerrilla(id)||!shop.Bench.Any(p=>p.Id==id));
        }
        public bool SetGuerrillaTransfer(string cardId,bool enabled)
        {
            PruneGuerrillaTransfers();
            if(!GuerrillaTransfersEditable||string.IsNullOrEmpty(cardId)||CampaignBalance.Guerrilla(cardId)||!shop.Bench.Any(p=>p.Id==cardId))return false;
            bool selected=guerrillaTransfers.Contains(cardId);
            if(selected==enabled)return true;
            if(enabled&&guerrillaTransfers.Count>=2)return false;
            if(enabled)guerrillaTransfers.Add(cardId);else guerrillaTransfers.Remove(cardId);
            SyncShopDeck(false);
            return true;
        }

        public Rect GuerrillaTransferRowRect(string cardId)
        {
            if(!GuerrillaTransfersVisible)return Rect.zero;
            var ids=GuerrillaTransferCandidateIds;int index=-1;
            for(int i=0;i<ids.Count;i++)if(ids[i]==cardId){index=i;break;}
            if(index<0)return Rect.zero;
            var list=GuerrillaTransferListRect;
            var row=new Rect(list.x,list.y+index*GuerrillaTransferRowHeight-guerrillaTransferScroll.y,list.width-18,GuerrillaTransferRowHeight-3);
            return row.yMin>=list.yMin&&row.yMax<=list.yMax?row:Rect.zero;
        }
        public Rect GuerrillaTransferButtonRect(string cardId)
        {
            var row=GuerrillaTransferRowRect(cardId);
            return row.width<=0?Rect.zero:new Rect(row.xMax-112,row.y+4,104,30);
        }

        void DrawGuerrillaTransferPanel()
        {
            var ids=GuerrillaTransferCandidateIds;int selected=GuerrillaTransferIds.Count,prepared=PreparedGuerrillaTransferIds.Count;
            GUI.Label(new Rect(268,238,690,23),"准备阶段免费指定最多两种非原生游击角色；开战锁定",CampaignText(13,weight:600));
            var summary=CurrentSynergies.FirstOrDefault(s=>s.Name=="游击队");
            var rule=GuerrillaRules.ForTier(summary?.Tier??0);
            string preview=summary==null||summary.Tier==0?"游击未达2种":$"游击{summary.Count}种 / {summary.Tier}档 · 满{rule.Cost}点自动整令 · 开战军令{rule.InitialOrders}";
            GUI.Label(new Rect(268,263,690,20),$"已指定 {selected}/2 · 当前入编 {prepared}/2    "+preview,CampaignText(11,color:ApprovedUi.Muted));
            var list=GuerrillaTransferListRect;Fill(list,new Color(.84f,.89f,.92f,.75f));
            guerrillaTransferScroll=GUI.BeginScrollView(list,guerrillaTransferScroll,
                new Rect(0,0,list.width-18,Mathf.Max(list.height,ids.Count*GuerrillaTransferRowHeight)),false,false);
            try
            {
                if(ids.Count==0)GUI.Label(new Rect(12,10,list.width-45,48),"尚未持有可转职角色。先招募一名非原生游击角色，再来指定。",CampaignText(12));
                for(int i=0;i<ids.Count;i++)
                {
                    string id=ids[i];bool assigned=IsGuerrillaTransfer(id),inLineup=shopLineup.Contains(id);
                    Rect row=new Rect(0,i*GuerrillaTransferRowHeight,list.width-18,GuerrillaTransferRowHeight-3);
                    Fill(row,assigned?new Color(.95f,.91f,.80f):ApprovedUi.Paper);
                    if(shopDefinitions.TryGetValue(id,out var definition))DrawTestPortrait(new Rect(6,row.y+3,32,32),definition.Card.Portrait);
                    GUI.Label(new Rect(46,row.y+1,row.width-173,22),shop.Definition(id).Name+(assigned?" · 游击转职":""),CampaignText(13,weight:assigned?600:400));
                    GUI.Label(new Rect(46,row.y+22,row.width-173,16),inLineup?"编队中 · 指定后计入游击种类，保留原羁绊":"备战中 · 保留指定，编入队伍后才计数",CampaignText(10,color:ApprovedUi.Muted));
                    bool wasEnabled=GUI.enabled;GUI.enabled=wasEnabled&&GuerrillaTransfersEditable&&(assigned||selected<2);
                    if(LoopButton(new Rect(row.xMax-112,row.y+4,104,30),assigned?"取消转职":"指定游击",assigned))SetGuerrillaTransfer(id,!assigned);
                    GUI.enabled=wasEnabled;
                }
            }
            finally{GUI.EndScrollView();}
            GUI.Label(new Rect(268,482,690,19),GuerrillaTransfersEditable?"免费，可取消；同名升星继承，卖完该种类归还名额，新挑战清空。":"本场已锁定；转职仅在下一次整备阶段可更改。",CampaignText(11,color:ApprovedUi.Muted));
        }
        void DrawGuerrillaTransferBadges()
        {
            if(!CampaignActive||shop==null)return;
            for(int i=0;i<shop.Bench.Count;i++)
            {
                if(!IsGuerrillaTransfer(shop.Bench[i].Id))continue;
                Rect card=ShopPieceRect(i);if(card.width<=0||card.height<=0)continue;
                var badge=new Rect(card.x+3,card.y+(Loop.CanShop?35:76),Mathf.Min(86,card.width-6),15);
                Fill(badge,new Color(.95f,.84f,.50f,.97f));
                GUI.Label(badge,"游击转职",CampaignText(9,TextAnchor.MiddleCenter,new Color(.24f,.20f,.12f),600));
            }
        }
    }
}
