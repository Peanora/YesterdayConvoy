using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        bool sarkazContractBadgeDragging;
        Vector2 sarkazContractBadgePointer;
        public string PreparedContractCard=>Battle?.ContractCard;
        public bool SarkazContractBadgeDragging=>sarkazContractBadgeDragging;
        bool SarkazContractBadgeEditable=>CampaignActive&&Loop.CanShop&&Battle.Phase==BattlePhase.Ready&&!ArtLoading&&!ShopPanelTransitioning&&!CampaignUiModalOpen&&!loopMenuOpen&&!loopTestSettlementOpen;
        static bool IsSarkazContractCard(string id)=>id!=null&&CampaignBalance.Tags(id).Contains("萨卡兹");

        public Rect SarkazContractBadgeRect
        {
            get
            {
                if(!CampaignActive||shop==null)return Rect.zero;
                if(!CurrentSynergies.Any(s=>s.Name=="萨卡兹"))return Rect.zero;
                // The header's blank space keeps the badge clear of shop prices and cards.
                // Include the authored frame's four-unit margin beyond the old mode title.
                return new Rect(SynergyListRect.xMax+20,12,40,40);
            }
        }

        public bool TryAssignSarkazContract(string cardId)
        {
            if(!SarkazContractBadgeEditable){ShopNotice("开战后承契者已锁定，请在整备阶段指定");return false;}
            if(!IsSarkazContractCard(cardId)){ShopNotice("无法使羁绊外的角色成为承契者");return false;}
            if(!shopLineup.Contains(cardId)||!shop.Bench.Any(p=>p.Id==cardId))
            {ShopNotice("请将承契徽章拖到编队中的萨卡兹角色");return false;}
            if(contractCard!=cardId||PreparedContractCard!=cardId){contractCard=cardId;SyncShopDeck(false);}
            ShopNotice("承契者："+shop.Definition(cardId).Name+" · 部署自动生效，持续本场");
            return true;
        }

        public void CancelSarkazContractBadgeDrag()=>sarkazContractBadgeDragging=false;

        // ShopPointerEvent calls this before purchase, sale or formation-card input.
        // Returning true owns the entire gesture, including an invalid release.
        public bool ProcessSarkazContractBadgePointer(EventType type,Vector2 point)
        {
            bool pointer=type==EventType.MouseDown||type==EventType.MouseDrag||type==EventType.MouseMove||type==EventType.MouseUp;
            if(!pointer)return false;
            sarkazContractBadgePointer=point;
            if(sarkazContractBadgeDragging)
            {
                if(!SarkazContractBadgeEditable){CancelSarkazContractBadgeDrag();return true;}
                if(type==EventType.MouseUp)
                {
                    CancelSarkazContractBadgeDrag();
                    for(int i=0;i<shop.Bench.Count;i++)
                    {
                        if(!ShopPieceRect(i).Contains(point))continue;
                        var piece=shop.Bench[i];
                        if(!IsSarkazContractCard(piece.Id)){TryAssignSarkazContract(piece.Id);return true;}
                        if(!IsLineupPiece(piece)){ShopNotice("请将承契徽章拖到编队中的萨卡兹角色");return true;}
                        TryAssignSarkazContract(piece.Id);return true;
                    }
                    // No card was under the release: cancel without buying, selling or deploying.
                }
                return true;
            }
            var badge=SarkazContractBadgeRect;
            if(type!=EventType.MouseDown||badge.width<=0||!badge.Contains(point))return false;
            if(!SarkazContractBadgeEditable){ShopNotice("开战后承契者已锁定，请在整备阶段指定");return true;}
            CancelShopDrag();sarkazContractBadgeDragging=true;return true;
        }

        void DrawSarkazContractBadgeArt(Rect rect,float alpha=1)
        {
            Color old=GUI.color;
            try
            {
                GUI.color=new Color(1,1,1,old.a*alpha);
                var frame=SynergyBadgePresentation.FrameFor(SynergyBadgePresentation.GradeFor("萨卡兹",Battle.SarkazTier));
                if(frame!=null)GUI.DrawTexture(new Rect(rect.x-4,rect.y-4,rect.width+8,rect.height+8),frame,ScaleMode.ScaleToFit,true);
                var icon=SynergyBadgePresentation.IconFor("萨卡兹");
                if(icon!=null)GUI.DrawTexture(new Rect(rect.x+7,rect.y+7,rect.width-14,rect.height-14),icon,ScaleMode.ScaleToFit,true);
            }
            finally{GUI.color=old;}
        }

        void DrawSarkazContractBadges()
        {
            var badge=SarkazContractBadgeRect;if(badge.width<=0){CancelSarkazContractBadgeDrag();return;}
            if(!SarkazContractBadgeEditable)CancelSarkazContractBadgeDrag();
            bool covered=CampaignUiModalOpen||loopMenuOpen||loopTestSettlementOpen;
            if(!covered)
            {
                DrawSarkazContractBadgeArt(badge,Loop.CanShop?1:.65f);
                GUI.Label(new Rect(badge.xMax+6,badge.y+4,66,15),"承契徽章",CampaignText(9,TextAnchor.MiddleLeft,Color.white,600));
                GUI.Label(new Rect(badge.xMax+6,badge.y+20,66,14),Loop.CanShop?"拖至编队":"本场锁定",CampaignText(9,TextAnchor.MiddleLeft,new Color(.78f,.83f,.90f)));
            }
            if(shop==null)return;
            for(int i=0;i<shop.Bench.Count;i++)
            {
                var piece=shop.Bench[i];if(!IsLineupPiece(piece))continue;
                var card=ShopPieceRect(i);if(card.width<=0||card.height<=0)continue;
                if(piece.Id==PreparedContractCard)
                {
                    var mark=new Rect(card.xMax-27,card.y+26,23,23);
                    DrawSarkazContractBadgeArt(mark);
                    var name=new Rect(card.x+3,card.y+(Loop.CanShop?44:73),Mathf.Max(1,card.width-6),15);
                    Fill(name,new Color(.38f,.27f,.51f,.95f));GUI.Label(name,"承契者",CampaignText(9,TextAnchor.MiddleCenter,Color.white,600));
                }
                if(sarkazContractBadgeDragging&&IsSarkazContractCard(piece.Id))
                    ApprovedUi.Border(card,new Color(.82f,.65f,1,.85f));
            }
            if(sarkazContractBadgeDragging&&!covered)
            {
                var ghost=new Rect(sarkazContractBadgePointer.x-20,sarkazContractBadgePointer.y-20,40,40);
                DrawSarkazContractBadgeArt(ghost,.9f);
                var hint=new Rect(Mathf.Clamp(sarkazContractBadgePointer.x+24,0,1076),Mathf.Clamp(sarkazContractBadgePointer.y-11,0,693),204,27);
                Fill(hint,ApprovedUi.Paper);GUI.Label(hint,"拖至萨卡兹编队卡指定承契者",CampaignText(11,TextAnchor.MiddleCenter));
            }
        }
    }
}
