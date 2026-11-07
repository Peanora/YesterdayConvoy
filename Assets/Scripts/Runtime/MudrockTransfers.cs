using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        readonly HashSet<string> mudrockTransfers=new HashSet<string>(StringComparer.Ordinal);
        ShopTrialModel mudrockTransferShop;
        Vector2 mudrockTransferScroll;
        const float MudrockTransferRowHeight=42;

        public static Rect MudrockTransferTabRect=>new Rect(792,200,166,28);
        public static Rect MudrockTransferListRect=>new Rect(268,290,690,188);
        public bool MudrockTransfersVisible=>CampaignActive&&campaignConstructOpen&&campaignConstructTab==3;
        public bool GolemTrainingPanelVisible=>CampaignActive&&campaignConstructOpen&&campaignConstructTab==1&&golemTrainingTab;
        bool MudrockTransfersEditable=>SynergySandbox&&Loop.CanShop&&Battle!=null&&Battle.Phase==BattlePhase.Ready&&!Battle.IsPaused&&!ArtLoading;

        public IReadOnlyList<string> MudrockTransferIds
        {
            get { PruneMudrockTransfers();return Array.AsReadOnly(mudrockTransfers.OrderBy(id=>id,StringComparer.Ordinal).ToArray()); }
        }
        public IReadOnlyList<string> PreparedMudrockTransferIds
            =>Array.AsReadOnly(MudrockTransferIds.Where(shopLineup.Contains).ToArray());
        public IReadOnlyList<string> MudrockTransferCandidateIds
        {
            get
            {
                PruneMudrockTransfers();
                return Array.AsReadOnly(shop==null?Array.Empty<string>():shop.Bench.Select(p=>p.Id).Distinct()
                    .Where(id=>!CampaignBalance.MudrockCrew(id)).OrderBy(id=>id,StringComparer.Ordinal).ToArray());
            }
        }
        public bool IsMudrockTransfer(string cardId)
        {PruneMudrockTransfers();return cardId!=null&&mudrockTransfers.Contains(cardId);}

        void ResetMudrockTransfers()
        {mudrockTransfers.Clear();mudrockTransferShop=shop;mudrockTransferScroll=Vector2.zero;}
        void PruneMudrockTransfers()
        {
            // Designation belongs to one challenge's collection, independently of guerrilla training.
            // Reading the selection only removes stale ownership; it never rebuilds a live battle.
            if(!ReferenceEquals(mudrockTransferShop,shop))ResetMudrockTransfers();
            if(shop==null){mudrockTransfers.Clear();return;}
            mudrockTransfers.RemoveWhere(id=>CampaignBalance.MudrockCrew(id)||!shop.Bench.Any(p=>p.Id==id));
        }
        public bool SetMudrockTransfer(string cardId,bool enabled)
        {
            PruneMudrockTransfers();
            if(!MudrockTransfersEditable||string.IsNullOrEmpty(cardId)||CampaignBalance.MudrockCrew(cardId)||!shop.Bench.Any(p=>p.Id==cardId))return false;
            bool selected=mudrockTransfers.Contains(cardId);
            if(selected==enabled)return true;
            if(enabled&&mudrockTransfers.Count>=1)return false;
            if(enabled)mudrockTransfers.Add(cardId);else mudrockTransfers.Remove(cardId);
            SyncShopDeck(false);
            return true;
        }
        public void OpenGolemTrainingPanel()
        {
            if(!CampaignActive||!Loop.CanShop&&!Loop.CanDeploy)return;
            campaignConstructOpen=true;campaignConstructTab=1;golemTrainingTab=true;inspectedSynergy=null;CancelShopDrag();
        }

        public Rect MudrockTransferRowRect(string cardId)
        {
            if(!MudrockTransfersVisible)return Rect.zero;
            var ids=MudrockTransferCandidateIds;int index=-1;
            for(int i=0;i<ids.Count;i++)if(ids[i]==cardId){index=i;break;}
            if(index<0)return Rect.zero;
            var list=MudrockTransferListRect;
            var row=new Rect(list.x,list.y+index*MudrockTransferRowHeight-mudrockTransferScroll.y,list.width-18,MudrockTransferRowHeight-3);
            return row.yMin>=list.yMin&&row.yMax<=list.yMax?row:Rect.zero;
        }
        public Rect MudrockTransferButtonRect(string cardId)
        {
            var row=MudrockTransferRowRect(cardId);
            return row.width<=0?Rect.zero:new Rect(row.xMax-112,row.y+4,104,30);
        }
        void DrawMudrockTransferPanel()
        {
            var ids=MudrockTransferCandidateIds;int selected=MudrockTransferIds.Count,prepared=PreparedMudrockTransferIds.Count;
            GUI.Label(new Rect(268,238,690,23),SynergySandbox?"测试关免费指定一种非原生角色加入泥岩小队；开战锁定":"正式转职需泥岩小队徽章；可在取得徽章后整备",CampaignText(13,weight:600));
            var summary=CurrentSynergies.FirstOrDefault(s=>s.Name=="泥岩小队");
            string preview=summary==null||summary.Tier==0?"小队未达2种":$"小队{summary.Count}种 / {summary.Tier}档 · 总星级{Battle.MudrockTotalStars} · 巨像上限{Battle.GolemCapacity}级";
            GUI.Label(new Rect(268,263,690,20),$"已指定 {selected}/1 · 当前入编 {prepared}/1    "+preview,CampaignText(11,color:ApprovedUi.Muted));
            var list=MudrockTransferListRect;Fill(list,new Color(.89f,.87f,.82f,.8f));
            mudrockTransferScroll=GUI.BeginScrollView(list,mudrockTransferScroll,
                new Rect(0,0,list.width-18,Mathf.Max(list.height,ids.Count*MudrockTransferRowHeight)),false,false);
            try
            {
                if(ids.Count==0)GUI.Label(new Rect(12,10,list.width-45,48),"尚未持有可转职角色。先招募一名非泥岩小队角色，再来指定。",CampaignText(12));
                for(int i=0;i<ids.Count;i++)
                {
                    string id=ids[i];bool assigned=IsMudrockTransfer(id),inLineup=shopLineup.Contains(id);
                    Rect row=new Rect(0,i*MudrockTransferRowHeight,list.width-18,MudrockTransferRowHeight-3);
                    Fill(row,assigned?new Color(.94f,.88f,.72f):ApprovedUi.Paper);
                    if(shopDefinitions.TryGetValue(id,out var definition))DrawTestPortrait(new Rect(6,row.y+3,32,32),definition.Card.Portrait);
                    GUI.Label(new Rect(46,row.y+1,row.width-173,22),shop.Definition(id).Name+(assigned?" · 泥岩转职":""),CampaignText(13,weight:assigned?600:400));
                    GUI.Label(new Rect(46,row.y+22,row.width-173,16),inLineup?"编队中 · 计入小队人数与总星级，保留原羁绊":"备战中 · 保留指定，编入队伍后才计数",CampaignText(10,color:ApprovedUi.Muted));
                    bool wasEnabled=GUI.enabled;GUI.enabled=wasEnabled&&MudrockTransfersEditable&&(assigned||selected<1);
                    if(LoopButton(new Rect(row.xMax-112,row.y+4,104,30),assigned?"取消转职":"加入小队",assigned))SetMudrockTransfer(id,!assigned);
                    GUI.enabled=wasEnabled;
                }
            }
            finally{GUI.EndScrollView();}
            GUI.Label(new Rect(268,482,690,19),!SynergySandbox?"正式转职需要对应徽章；免费指定仅在测试关开放。":MudrockTransfersEditable?"测试专用免费指定，可取消；升星继承，售光释放；与游击转职独立。":"本场已锁定；下一次整备可更改，游击转职不受影响。",CampaignText(11,color:ApprovedUi.Muted));
        }
        void DrawMudrockTransferBadges()
        {
            if(!CampaignActive||shop==null)return;
            for(int i=0;i<shop.Bench.Count;i++)
            {
                string id=shop.Bench[i].Id;if(!IsMudrockTransfer(id))continue;
                Rect card=ShopPieceRect(i);if(card.width<=0||card.height<=0)continue;
                // Reserve a separate line when the same kind also carries a guerrilla designation.
                float offset=Loop.CanShop?35:76;if(IsGuerrillaTransfer(id))offset+=16;
                var badge=new Rect(card.x+3,card.y+offset,Mathf.Min(86,card.width-6),15);
                Fill(badge,new Color(.86f,.77f,.55f,.97f));
                GUI.Label(badge,"泥岩转职",CampaignText(9,TextAnchor.MiddleCenter,new Color(.25f,.20f,.12f),600));
            }
        }
    }
}
