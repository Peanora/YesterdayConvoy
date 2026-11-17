using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    // Approved shop layout and artwork, backed by the shared finite-stock transactions and seats.
    public sealed partial class FormationBattleController
    {
        bool marketVisible=true,shopOddsVisible,finalRewardClaimed;
        int shopDragInstance,selectedPiece,shopInputBlockedFrame=-1;
        public bool MarketVisible=>marketVisible;
        public static Rect FormationLineupRect(int index)=>new Rect(188+index*94.2f,584,94,50);
        public static Rect FormationReserveRect(int index)=>new Rect(188+index*94.2f,655,94,50);
        public static Rect FormationMarketRect=>ApprovedUi.ShopPanel;
        public static Rect FormationOfferRect(int index)=>ApprovedUi.Offer(index);
        static Rect FormationOddsRect=>new Rect(306,96,670,441);
        static Rect FormationSellLeft=>new Rect(0,65,92,500);
        static Rect FormationSellRight=>new Rect(1188,65,92,500);
        bool CanTrade=>Ready&&Session!=null&&Session.CanShop&&Model.Phase==FormationPhase.Preparation;
        public void SetMarketVisible(bool visible)
        {marketVisible=visible&&Model!=null&&Model.Phase==FormationPhase.Preparation;shopOddsVisible=false;CancelFormationDrag();}
        void CancelFormationDrag(){pointerHeld=false;dragSlot=-1;shopDragInstance=0;}
        void SelectFirstLineup()
        {
            if(Model==null)return;
            var selected=Model.Units.FirstOrDefault(u=>u.Instance==selectedPiece);
            if(selected==null&&selectedPiece>0&&Shop.Bench.Any(p=>p.Instance==selectedPiece)){SelectedSlot=-1;return;}
            selected=selected??Model.Units.FirstOrDefault();SelectedSlot=selected?.Slot??-1;selectedPiece=selected?.Instance??0;
        }
        void SelectShopPiece(int instance){selectedPiece=instance;SelectedSlot=Model.Units.FindIndex(u=>u.Instance==instance);}
        ShopTrialModel.Result ShopAction(Func<ShopTrialModel.Result> action)
        {
            if(!CanTrade)return new ShopTrialModel.Result{Message="仅战前整备时可以交易。"};
            Session.SavePositions(Model);var result=action();notice=result.Message;
            if(result.Success)
            {Session.ApplyTo(Model);if(result.Instance>0&&Shop.Bench.Any(p=>p.Instance==result.Instance))selectedPiece=result.Instance;SelectFirstLineup();SyncActors();}
            return result;
        }
        public ShopTrialModel.Result BuyOffer(int index)=>ShopAction(()=>Session.Purchase(index));
        public ShopTrialModel.Result RefreshMarket()=>ShopAction(()=>Session.Refresh());
        public ShopTrialModel.Result BuyExperience()=>ShopAction(()=>Session.BuyExperience());
        public ShopTrialModel.Result SellPiece(int instance)=>ShopAction(()=>Session.Sell(instance));
        public ShopTrialModel.Result MovePiece(int instance,bool lineup,int seat)=>ShopAction(()=>Session.Move(instance,lineup,seat));
        bool FormationShopBlocksPoint(Vector2 point)=>shopOddsVisible||point.y>=565||point.y<65||marketVisible&&FormationMarketRect.Contains(point);
        bool FindFormationSeat(Vector2 point,out bool lineup,out int seat)
        {
            for(int i=0;i<10;i++)
            {
                if(FormationLineupRect(i).Contains(point)){lineup=true;seat=i;return true;}
                if(FormationReserveRect(i).Contains(point)){lineup=false;seat=i;return true;}
            }
            lineup=false;seat=-1;return false;
        }
        bool FormationShopPointerDown(Vector2 point)
        {
            if(shopOddsVisible){shopInputBlockedFrame=Time.frameCount;if(!FormationOddsRect.Contains(point))shopOddsVisible=false;CancelFormationDrag();return true;}
            if(marketVisible&&FormationMarketRect.Contains(point)){CancelFormationDrag();return true;}
            if(!FindFormationSeat(point,out bool lineup,out int seat))return false;
            int instance=Session.Seats.At(lineup,seat);
            if(instance>0){SelectShopPiece(instance);shopDragInstance=instance;}else CancelFormationDrag();
            return true;
        }
        bool FormationShopPointerUp(Vector2 point)
        {
            if(shopDragInstance<=0)return FormationShopBlocksPoint(point);
            if(FormationSellLeft.Contains(point)||FormationSellRight.Contains(point)){SellPiece(shopDragInstance);return true;}
            if(FindFormationSeat(point,out bool lineup,out int seat)){MovePiece(shopDragInstance,lineup,seat);return true;}
            return FormationShopBlocksPoint(point);
        }
        void PlaceReserve(int instance,Vector2Int cell)
        {
            if(cell.x<0||cell.x>=FormationBattleModel.DeploymentColumns||cell.y<0||cell.y>=FormationBattleModel.Height)
            {notice="只能在左侧部署区布阵，卡牌与金币保持原样。";return;}
            var existing=Model.Units.FirstOrDefault(u=>u.Instance==instance);
            if(existing==null)
            {
                int seat=Session.Seats.FirstEmpty(true,Shop.Level);
                if(seat<0){notice="上阵席位已满；可拖到已有上阵卡交换，或购买经验升级。";return;}
                var result=MovePiece(instance,true,seat);if(!result.Success)return;
                existing=Model.Units.FirstOrDefault(u=>u.Instance==instance);
            }
            if(existing!=null){SelectShopPiece(instance);Place(existing.Slot,cell);}
        }
        void DrawFormationPortrait(Rect rect,string cardId)
        {
            var portrait=LocalMonsterArt.For(cardId)?.Portrait;if(portrait==null||portrait.texture==null)return;
            float aspect=portrait.rect.width/portrait.rect.height;
            float width=Mathf.Min(rect.width,rect.height*aspect),height=width/aspect;
            var fitted=new Rect(rect.center.x-width*.5f,rect.yMax-height,width,height);
            var uv=new Rect(portrait.rect.x/portrait.texture.width,portrait.rect.y/portrait.texture.height,portrait.rect.width/portrait.texture.width,portrait.rect.height/portrait.texture.height);
            GUI.DrawTextureWithTexCoords(fitted,portrait.texture,uv,true);
        }
        void DrawFormationMarket()
        {
            var panel=FormationMarketRect;Fill(panel,ApprovedUi.Paper);ApprovedUi.Border(panel,ApprovedUi.Rule);
            Label(new Rect(panel.x+9,panel.y+3,160,24),"招募商店",15,ApprovedUi.Ink);
            Label(new Rect(panel.x+112,panel.y+5,390,20),"五格商品 / 点击购买 / 三张同星自动合成",11,ApprovedUi.Muted);
            if(Button(new Rect(panel.x+678,panel.y+4,274,22),"查看等级1–10招募概率"))shopOddsVisible=true;
            if(Button(new Rect(panel.x+980,panel.y+4,86,22),"收起商店"))SetMarketVisible(false);
            if(Button(new Rect(panel.x+956,panel.y+36,110,32),"刷新 2金币",CanTrade))RefreshMarket();
            if(Button(new Rect(panel.x+956,panel.y+74,110,32),Shop.Locked?"已锁定 · 解锁":"锁定商店",CanTrade))Session.SetLocked(!Shop.Locked);
            Label(new Rect(panel.x+956,panel.y+115,110,38),"库存有限\n每关免费刷新",10,ApprovedUi.Muted,TextAnchor.MiddleCenter);
            for(int i=0;i<5;i++)DrawFormationOffer(i);
        }
        void DrawFormationOffer(int index)
        {
            var r=FormationOfferRect(index);string id=Shop.Offers[index];
            if(id==null){Fill(r,new Color(.52f,.60f,.65f,.65f));ApprovedUi.Border(r,ApprovedUi.Rule);Label(r,"暂无商品",12,White,TextAnchor.MiddleCenter);return;}
            var role=Shop.Definition(id);var tier=ApprovedUi.Tier(role.Cost);Fill(r,new Color(tier.r*.65f,tier.g*.65f,tier.b*.65f));
            Fill(new Rect(r.x,r.y,r.width,4),tier);ApprovedUi.Border(r,tier,1.5f);
            DrawFormationPortrait(new Rect(r.x+r.width*.34f,r.y+5,r.width*.64f,108),id);
            Fill(new Rect(r.x,r.y+73,r.width,47),new Color(.09f,.15f,.20f,.94f));
            int copies=Shop.Bench.Where(p=>p.Id==id).Sum(p=>ShopTrialModel.Copies(p.Star));
            var preview=Shop.PreviewPurchase(index);bool affordable=Shop.CanAfford(role.Cost);
            if(copies>0)ApprovedUi.Border(r,new Color(.86f,.94f,1,.65f),1.5f);
            if(preview.Success&&preview.FinalStar>1&&affordable)
            {
                float pulse=.65f+.35f*Mathf.Sin(Time.unscaledTime*5);ApprovedUi.Border(r,new Color(1,.91f,.59f,pulse),2);
                Fill(new Rect(r.xMax-64,r.y+6,58,18),ApprovedUi.Gold);Label(new Rect(r.xMax-64,r.y+6,58,18),"可合成 "+preview.FinalStar+"★",9,ApprovedUi.Ink,TextAnchor.MiddleCenter);
            }
            if(!affordable)Fill(r,new Color(.16f,.16f,.16f,.76f));
            var ink=affordable?White:new Color(.7f,.7f,.7f);
            Label(new Rect(r.x+11,r.y+7,52,27),role.Cost+" 金",22,ink);
            Label(new Rect(r.x+10,r.y+75,r.width-20,22),role.Name,role.Name.Length>6?12:14,ink);
            Label(new Rect(r.x+10,r.y+99,r.width-20,17),$"持有 {copies} / {Shop.Capacity(id)}  ·  {string.Join(" / ",CampaignBalance.Tags(id))}",9,affordable?White:Muted);
            if(!affordable)Label(new Rect(r.xMax-66,r.y+6,60,18),"金币不足",10,White,TextAnchor.MiddleCenter);
            bool enabled=GUI.enabled;GUI.enabled=enabled&&CanTrade&&affordable;
            if(GUI.Button(r,new GUIContent("",role.Name+" · "+role.Cost+"金币"),GUIStyle.none))BuyOffer(index);
            GUI.enabled=enabled;
        }
        void DrawFormationPreparation()
        {
            Fill(new Rect(0,565,1280,155),ApprovedUi.Paper);DrawFormationResources(true);
            Label(new Rect(188,567,620,15),$"上阵 {Model.Units.Count} / {Shop.Level}   拖动交换 · 同名只能一张出战",11,ApprovedUi.Ink);
            Label(new Rect(188,638,800,15),"备战席   拖到上阵席位或左侧战场加入军阵；拖到战场两侧出售",11,ApprovedUi.Muted);
            for(int i=0;i<10;i++){DrawFormationSeat(true,i);DrawFormationSeat(false,i);}
            if(Button(new Rect(1142,579,126,43),"开始进攻",Model.Units.Count>0,true))StartBattle();
            if(Button(new Rect(1142,634,126,30),"新挑战"))ResetDemo();
            Label(new Rect(1142,674,126,29),"Tab 开闭商店",11,ApprovedUi.Muted,TextAnchor.MiddleCenter);
        }
        void DrawFormationSeat(bool lineup,int index)
        {
            var r=lineup?FormationLineupRect(index):FormationReserveRect(index);bool unlocked=!lineup||index<Shop.Level;
            Fill(r,unlocked?new Color(.80f,.85f,.88f):new Color(.68f,.73f,.76f));ApprovedUi.Border(r,ApprovedUi.Rule);
            int instance=Session.Seats.At(lineup,index);var piece=Shop.Bench.FirstOrDefault(p=>p.Instance==instance);
            if(piece==null){Label(r,unlocked?(index+1).ToString():"等级 "+(index+1),10,ApprovedUi.Muted,TextAnchor.MiddleCenter);return;}
            var role=Shop.Definition(piece.Id);Fill(new Rect(r.x,r.y,3,r.height),ApprovedUi.Tier(role.Cost));
            DrawFormationPortrait(new Rect(r.x+3,r.y+2,39,r.height-4),piece.Id);
            Label(new Rect(r.x+41,r.y+6,r.width-44,18),role.Name,role.Name.Length>5?8:10,ApprovedUi.Ink);
            Label(new Rect(r.x+42,r.y+27,r.width-45,17),new string('★',piece.Star),10,new Color(.52f,.39f,.16f));
            if(instance==selectedPiece)ApprovedUi.Border(r,new Color(.24f,.55f,.58f),2);
        }
        void DrawFormationResources(bool preparing)
        {
            var r=new Rect(10,575,166,132);Fill(r,ApprovedUi.Paper);ApprovedUi.Border(r,ApprovedUi.Rule);Fill(new Rect(r.x,r.y,3,r.height),ApprovedUi.Gold);
            Label(new Rect(23,581,44,21),"金币",12,ApprovedUi.Muted);Label(new Rect(72,576,91,29),Shop.Gold.ToString(),26,new Color(.45f,.34f,.16f),TextAnchor.MiddleRight);
            Label(new Rect(23,613,76,22),"等级 "+Shop.Level,15,ApprovedUi.Ink);Label(new Rect(99,617,64,18),Shop.Level==10?"已达上限":$"{Shop.XP}/{Shop.NextLevelXP}",11,ApprovedUi.Muted,TextAnchor.MiddleRight);
            Fill(new Rect(23,641,140,4),ApprovedUi.Rule);Fill(new Rect(23,641,140*(Shop.Level==10?1:Mathf.Clamp01((float)Shop.XP/Shop.NextLevelXP)),4),new Color(.29f,.48f,.63f));
            if(preparing){if(Button(new Rect(23,657,140,28),"买经验 +4 · 4金",CanTrade&&Shop.Level<10))BuyExperience();}
            else Label(new Rect(23,654,140,35),"战斗中交易锁定",12,ApprovedUi.Muted,TextAnchor.MiddleCenter);
        }
        void DrawFormationSaleTargets()
        {
            var piece=Shop.Bench.FirstOrDefault(p=>p.Instance==shopDragInstance);if(piece==null)return;
            var quote=Shop.Quote(shopDragInstance);var point=TacticalViewport.Current.FromScreen(Input.mousePosition);
            foreach(var rect in new[]{FormationSellLeft,FormationSellRight})
            {
                Fill(rect,rect.Contains(point)?new Color(.92f,.86f,.79f,.97f):new Color(.85f,.89f,.91f,.95f));ApprovedUi.Border(rect,ApprovedUi.Rule);
                Label(new Rect(rect.x+5,rect.center.y-43,rect.width-10,25),"松开出售",15,ApprovedUi.Ink,TextAnchor.MiddleCenter);
                Label(new Rect(rect.x+5,rect.center.y-8,rect.width-10,32),"+"+quote.Value+" 金",22,new Color(.56f,.43f,.23f),TextAnchor.MiddleCenter);
            }
            DrawFormationPortrait(new Rect(point.x-24,point.y-64,48,60),piece.Id);
        }
        void DrawFormationOdds()
        {
            Fill(new Rect(0,65,1280,655),new Color(.015f,.03f,.04f,.75f));var r=FormationOddsRect;Fill(r,ApprovedUi.Paper);ApprovedUi.Border(r,ApprovedUi.Rule);
            Label(new Rect(r.x+20,r.y+13,520,28),"招募概率 · 每格先抽费用，再按剩余库存抽角色",17,ApprovedUi.Ink);
            if(Button(new Rect(r.xMax-72,r.y+13,52,26),"关闭"))shopOddsVisible=false;
            for(int cost=1;cost<=5;cost++)Label(new Rect(r.x+122+(cost-1)*95,r.y+59,90,23),cost+"费",13,ApprovedUi.Ink,TextAnchor.MiddleCenter);
            for(int level=1;level<=10;level++)
            {
                float y=r.y+87+(level-1)*25;if(level==Shop.Level)Fill(new Rect(r.x+20,y,r.width-40,25),new Color(.75f,.83f,.85f));
                Label(new Rect(r.x+25,y,90,25),level+(level==Shop.Level?"  当前":""),12,ApprovedUi.Ink,TextAnchor.MiddleCenter);
                var weights=ShopProbabilityUi.Baseline(level);
                for(int cost=0;cost<5;cost++)Label(new Rect(r.x+122+cost*95,y,90,25),weights[cost]+"%",12,ApprovedUi.Ink,TextAnchor.MiddleCenter);
            }
            var actual=ShopProbabilityUi.FirstDraw(Shop,Shop.Level);
            Label(new Rect(r.x+24,r.y+350,r.width-48,42),"当前实际首格费用概率："+string.Join(" / ",actual.Select((v,i)=>(i+1)+"费 "+v.ToString("0.#")+"%")),11,ApprovedUi.Muted);
            Label(new Rect(r.x+24,r.y+397,r.width-48,22),"缺少的费用档按现有库存重新归一化；展示中的商品也占用牌库。",11,ApprovedUi.Muted);
        }
    }
}
