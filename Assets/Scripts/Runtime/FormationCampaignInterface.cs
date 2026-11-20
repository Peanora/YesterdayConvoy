using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        bool campaignInventoryOpen,campaignFormationOpen,campaignRouteOpen,campaignScoutingOpen,campaignResultSettled,campaignSeaPainting;
        bool campaignRewardChoiceOpen,campaignResultSummaryOpen;
        int campaignSelectedItem,campaignCombineItem,campaignSelectedPiece;
        Vector2 campaignChoiceScroll,campaignItemScroll,campaignUnitScroll,campaignRouteScroll,campaignScoutScroll,campaignTraitScroll;
        public FormationCampaignRun Campaign=>Session?.Run;
        bool AwaitingOptionalRewards=>HasGroundRewards||Campaign?.PendingRewardChoices>0;
        bool CampaignInteractionBlocked=>Campaign!=null&&(Campaign.OpeningRewardsPending||Campaign.HasBlockingChoice||Campaign.Finished&&!AwaitingOptionalRewards||
            campaignInventoryOpen||campaignFormationOpen||campaignRouteOpen||campaignScoutingOpen||campaignRewardChoiceOpen||
            campaignResultSummaryOpen||formationExitOpen||FormationStrategyHistoryOpen);

        GUIStyle CampaignText(int size=13,bool bold=false,TextAnchor align=TextAnchor.UpperLeft)
        {
            var style=new GUIStyle(approvedShop.LoopText(size,align,null,bold?650:400));
            style.wordWrap=true;style.clipping=TextClipping.Clip;return style;
        }
        void CampaignLabel(Rect rect,string value,int size=13,bool bold=false)=>GUI.Label(rect,value??"",CampaignText(size,bold));
        void TickCampaignSettlement()
        {
            if(Campaign==null||campaignResultSettled||(Campaign.IsPractice&&Campaign.SynergyTrial==null)||
                (Model.Phase!=FormationPhase.Won&&Model.Phase!=FormationPhase.Lost))return;
            campaignResultSettled=true;
            Session.SettleBattle(Model.Phase==FormationPhase.Won,Model.EncounterProgress,Model.InitialDefenderCount,Model.RemainingDefenders,Model.BossAlive);
            notice=Campaign.LastTrialProgress??Campaign.LastSummary;
        }
        public ShopTrialModel.Result ChooseCampaignOption(int index)
        {
            if(Campaign==null)return new ShopTrialModel.Result{Message="当前为旧试玩模式"};
            var result=Session.Choose(index);notice=result.Message;
            if(result.Success)RefreshCampaignPreparation();
            return result;
        }
        public bool ContinueCampaign()
        {
            if(Campaign==null||Campaign.HasBlockingChoice||Campaign.Finished||openingGroundBall!=null)return false;
            Session.SavePositions(Model);
            if(!Session.AdvanceNode())return false;
            FormationStrategyHistoryOpen=false;
            campaignResultSettled=false;lastSoundAt=-10;selectedPiece=0;formationInspectedTag=null;
            RefreshCampaignPreparation();SetMarketVisible(false);SetFormationView(false);
            notice=Campaign.CurrentNode.Kind=="补给"?"选择一组补给，之后继续下一节点。":"新防线已经公开。先侦察，再调整装备、搭档与站位。";
            return true;
        }
        void RefreshCampaignPreparation()
        {
            if(Model.Phase==FormationPhase.Preparation)
            {
                Session.ApplyTo(Model);Model.BindCampaign(Campaign);
                SelectFirstLineup();SyncActors();approvedShop?.SyncShopBenchModels();
            }
        }
        void CampaignAction(ShopTrialModel.Result result)
        {notice=result.Message;if(result.Success)RefreshCampaignPreparation();}
        internal bool OpenGroundRewardChoices()
        {
            if(Campaign?.PendingRewardChoices>0&&!CombatViewActive&&!CampaignInteractionBlocked)
            {
                CloseCampaignPanels(); SetMarketVisible(false); CloseFormationTraitDetails();
                campaignChoiceScroll=Vector2.zero; campaignRewardChoiceOpen=true;
                return true;
            }
            return false;
        }
        internal ShopTrialModel.Result ChooseGroundRewardOption(int index)
        {
            var result=Campaign.ChooseReward(index);
            CampaignAction(result);
            if(Campaign.PendingRewardChoices==0)campaignRewardChoiceOpen=false;
            campaignChoiceScroll=Vector2.zero;
            return result;
        }
        void CloseCampaignPanels()
        {
            campaignInventoryOpen=campaignFormationOpen=campaignRouteOpen=campaignScoutingOpen=campaignRewardChoiceOpen=campaignResultSummaryOpen=false;
            FormationStrategyHistoryOpen=false;
            rewardHistoryOpen=false;formationExitOpen=false;
            CloseFormationInspection();CloseFormationRouteDetails();CloseFormationEquipmentDetails();CancelFormationEquipmentInteraction();CancelFormationDrag();
        }

        void DrawCampaignToolbar()
        {
            if(Campaign==null)return;
            bool prep=Model.Phase==FormationPhase.Preparation;
            if(prep&&!MarketVisible&&!ShopPanelTransitioning&&Campaign.TraitCount("海嗣")>=2)
            {
                if(OperationsButton(new Rect(16,575,145,21),campaignSeaPainting?"完成溟痕布置":"布置溟痕",campaignSeaPainting))
                {campaignSeaPainting=!campaignSeaPainting;CloseCampaignPanels();SetMarketVisible(false);if(campaignSeaPainting)SetFormationView(true);}
                if(campaignSeaPainting&&OperationsButton(new Rect(16,600,145,21),"恢复推荐"))
                {Campaign.SeaCellIds.Clear();Model.RefreshSeaPreview();}
            }
            string resources=$"希望 {Campaign.Hope} · 刷新券 {Campaign.FreeRefreshes}";
            if(Campaign.SynergyTrial?.Trait.Id=="泥岩")resources=$"巨像培育经验 {Campaign.MudrockXP} · "+resources;
            if(!string.IsNullOrEmpty(notice)&&notice!=Campaign.SynergyTrial?.Hint)resources+="  ·  "+notice;
            OperationsLabel(new Rect(117,FormationExperienceRect.yMin-20,1014,17),
                resources+(Campaign.PendingDeliveries>0?"  ·  库存待领 "+Campaign.PendingDeliveries+" 份":""),
                10,OperationsMuted);
        }
        void DrawCampaignOverlays()
        {
            if(Campaign==null)return;
            if(Campaign.HasBlockingChoice){DrawCampaignChoices();return;}
            if(campaignRewardChoiceOpen&&Campaign.PendingRewardChoices>0){DrawCampaignChoices(true);return;}
            if(Campaign.Finished&&(!AwaitingOptionalRewards||campaignResultSummaryOpen)){DrawCampaignFinished();return;}
            if(FormationStrategyHistoryOpen)DrawFormationStrategyHistory();
            else if(campaignInventoryOpen)DrawCampaignInventory();
            else if(campaignFormationOpen)DrawCampaignFormation();
            else if(campaignRouteOpen)DrawCampaignRoute();
            else if(campaignScoutingOpen)DrawCampaignScouting();
        }
        Rect CampaignWindow(string title,string subtitle,bool close=true)
        {
            Fill(new Rect(0,64,1280,650),new Color(.04f,.07f,.10f,.65f));
            var r=new Rect(172,82,936,525);Fill(r,ApprovedUi.Paper);ApprovedUi.Border(r,ApprovedUi.Rule,1);
            CampaignLabel(new Rect(r.x+22,r.y+18,r.width-90,30),title,23,true);
            CampaignLabel(new Rect(r.x+22,r.y+52,r.width-48,37),subtitle,12);
            if(close&&Button(new Rect(r.xMax-57,r.y+15,38,30),"×"))CloseCampaignPanels();
            return r;
        }
        void DrawCampaignChoices(bool optional = false)
        {
            var run=Campaign;
            var r=CampaignWindow(optional?run.RewardChoiceTitle:run.ChoiceTitle,optional?"可关闭面板，稍后再选。":"选择会保留本局。初始角色、库存和已选奖励不会因重抽而重置。",optional);
            var choices=(optional?run.RewardChoices:run.Choices).ToArray();int columns=choices.Length==2?2:3;
            float width=(r.width-62-(columns-1)*12)/columns;
            var viewport=new Rect(r.x+22,r.y+96,r.width-44,345);
            float contentHeight=Mathf.Ceil(choices.Length/(float)columns)*185;
            campaignChoiceScroll=GUI.BeginScrollView(viewport,campaignChoiceScroll,new Rect(0,0,viewport.width-18,Mathf.Max(viewport.height,contentHeight)));
            for(int i=0;i<choices.Length;i++)
            {
                var option=choices[i];var card=new Rect(i%columns*(width+12),i/columns*185,width,173);
                Fill(card,new Color(.90f,.91f,.90f));ApprovedUi.Border(card,ApprovedUi.Rule);
                var art = SupplyItemArt(option.Id);
                if (art != null) DrawSupplyItem(new Rect(card.x+11,card.y+7,32,32),option.Id);
                float textInset = art != null ? 51 : 12;
                CampaignLabel(new Rect(card.x+textInset,card.y+11,card.width-textInset-10,28),option.Name,16,true);
                CampaignLabel(new Rect(card.x+12,card.y+44,card.width-24,84),option.Description,12);
                if(Button(new Rect(card.x+12,card.yMax-35,card.width-24,27),"选择"))
                {
                    if(optional)
                    {
                        ChooseGroundRewardOption(i);
                    }
                    else ChooseCampaignOption(i);
                    GUI.EndScrollView();return;
                }
            }
            GUI.EndScrollView();
            if(optional)return;
            if(Button(new Rect(r.x+22,r.yMax-61,190,35),"免费重抽本组一次",run.CanRerollChoice))run.RerollChoice();
            CampaignLabel(new Rect(r.x+231,r.yMax-57,660,36),run.CanRerollChoice?(run.ChoiceTitle?.Contains("战略预案")==true?"换成未展示的预案，原组选项不会返回。":"重抽本组战略战备，不改变已经选定的战备。 "):"完成本次选择后继续。",12);
        }
        void DrawCampaignFinished()
        {
            var r=CampaignWindow(Campaign.Won?"三个位面突破 · 挑战完成":"挑战结束",Campaign.Won?"你的军阵已突破最终防线。":Campaign.SquadHP<=0?"队伍生命已耗尽，本局构筑到此结束。":"未能突破最终防线，本局挑战结束。",AwaitingOptionalRewards);
            CampaignLabel(new Rect(r.x+25,r.y+120,r.width-50,90),$"抵达 {Campaign.CurrentNode.Id}  ·  等级 {Shop.Level}  ·  金币 {Shop.Gold}  ·  希望 {Campaign.Hope}\n战略预案：{FormationCampaignRules.FindRecord(Campaign.PreplanId)?.Name??"未选择"}",18,true);
            CampaignLabel(new Rect(r.x+25,r.y+220,r.width-50,110),"本局战略战备：\n"+string.Join("、",Campaign.AugmentIds.Select(id=>FormationCampaignRules.FindRecord(id)?.Name??id)),15);
            if(Button(new Rect(r.center.x-137.5f,r.yMax-80,275,45),"重新开始完整挑战",true,true))ResetDemo();
        }
        void DrawCampaignResult()
        {
            bool won=Model.Phase==FormationPhase.Won;
            var r=new Rect(246,164,554,350);Fill(r,ApprovedUi.Paper);ApprovedUi.Border(r,ApprovedUi.Rule,2);
            CampaignLabel(new Rect(r.x+25,r.y+20,r.width-50,35),won?"蓝门突破 · 本关胜利":"本关失利 · 调整后继续",25,true);
            CampaignLabel(new Rect(r.x+25,r.y+68,r.width-50,85),Campaign.LastSummary,15);
            CampaignLabel(new Rect(r.x+25,r.y+159,r.width-50,70),$"队伍生命 {Campaign.SquadHP}  ·  金币 {Shop.Gold}  ·  希望 {Campaign.Hope}\n遭遇削减进度 {Model.EncounterProgress:P1}  ·  本关只结算一次",14);
            var names=new[]{"白","蓝","金","彩"};
            for(int i=0;i<4;i++)
            {
                float x=r.x+25+i*125;
                var art=SupplyOrbArt(names[i]);
                if(art!=null)GUI.DrawTexture(new Rect(x,r.y+221,32,32),art,ScaleMode.ScaleToFit,true);
                CampaignLabel(new Rect(x+35,r.y+228,88,23),names[i]+"球 ×"+Campaign.PendingBalls.Count(ball=>ball.Node==Campaign.CurrentNode.Id&&ball.Color==names[i]),13,true);
            }
            if(HasGroundRewards) CampaignLabel(new Rect(r.x+25,r.y+258,r.width-50,20),"棋盘前沿的战利品可以留到下次整备领取。",11);
            string next=Campaign.HasBlockingChoice?"先完成必要选择":Campaign.Finished?"查看最终结算":"继续下一节点";
            if(Button(new Rect(r.x+25,r.yMax-75,r.width-50,48),next,!Campaign.HasBlockingChoice&&openingGroundBall==null,true))
            {
                if (Campaign.Finished) campaignResultSummaryOpen=true;
                else ContinueCampaign();
            }
        }
        void DrawPracticeResult()
        {
            var trial=Campaign.SynergyTrial;
            string progress=Campaign.LastTrialProgress;
            bool showProgress=!string.IsNullOrEmpty(progress);
            var r=new Rect(350,198,580,showProgress?270:234);Fill(r,ApprovedUi.Paper);ApprovedUi.Border(r,ApprovedUi.Rule,2);
            string outcome=Model.Phase==FormationPhase.Won?"蓝门突破":"本关失利";
            string title=trial?.Trait.Name??Campaign.CurrentNode.Id;
            CampaignLabel(new Rect(r.x+25,r.y+20,r.width-50,40),$"{title}  ·  {outcome}",25,true);
            CampaignLabel(new Rect(r.x+25,r.y+84,r.width-50,35),$"用时 {Model.Clock:0.0} 秒  ·  剩余守军 {Model.RemainingDefenders}/{Model.InitialDefenderCount}  ·  守军生命削减 {Model.EncounterProgress:P0}",15);
            if(showProgress)CampaignLabel(new Rect(r.x+25,r.y+122,r.width-50,67),progress,14,true);
            if(Button(new Rect(r.x+25,r.yMax-72,254,46),"重试本关",true,true)){ResetDemo();return;}
            if(Button(new Rect(r.x+296,r.yMax-72,259,46),trial==null?"选择其他关卡":"选择其他羁绊"))
                OpenFormationTrialPicker(trial==null?FormationMenuPage.DifficultyTrials:FormationMenuPage.SynergyTrials);
        }
        void DrawCampaignInventory()
        {
            var r=CampaignWindow("装备与合成","选一件物品，再选角色装备；选两件基础件／宝石可合成固定成品。整备可免费拆下，物品栏不限数量。");
            var view=new Rect(r.x+20,r.y+97,425,330);var inventory=Campaign.Inventory.ToArray();
            campaignItemScroll=GUI.BeginScrollView(view,campaignItemScroll,new Rect(0,0,403,Mathf.Max(330,inventory.Length*48)));
            for(int i=0;i<inventory.Length;i++)
            {
                var item=inventory[i];var row=new Rect(0,i*48,396,43);
                Fill(row,campaignSelectedItem==item.Instance||campaignCombineItem==item.Instance?new Color(.81f,.87f,.87f):new Color(.91f,.92f,.91f));
                DrawSupplyItem(new Rect(6,row.y+3,36,36),item.Key);
                CampaignLabel(new Rect(51,row.y+10,223,29),item.Name,13,true);
                if(Button(new Rect(280,row.y+6,107,30),campaignSelectedItem==item.Instance?"已选第一件":"选择"))
                {if(campaignSelectedItem==0||campaignSelectedItem==item.Instance){campaignSelectedItem=item.Instance;campaignCombineItem=0;}else campaignCombineItem=item.Instance;}
            }
            GUI.EndScrollView();
            var units=Shop.Bench.OrderByDescending(p=>Session.Seats.InLineup(p.Instance)).ThenBy(p=>p.Instance).ToArray();
            var targetView=new Rect(r.x+465,r.y+97,448,330);
            campaignUnitScroll=GUI.BeginScrollView(targetView,campaignUnitScroll,new Rect(0,0,428,Mathf.Max(330,units.Length*77)));
            for(int i=0;i<units.Length;i++)
            {
                var piece=units[i];var equipment=Campaign.Equipment(piece.Instance).ToArray();var row=new Rect(0,i*77,421,71);
                Fill(row,campaignSelectedPiece==piece.Instance?new Color(.81f,.87f,.87f):new Color(.91f,.92f,.91f));
                CampaignLabel(new Rect(8,row.y+5,225,24),Shop.Definition(piece.Id).Name+" "+new string('★',piece.Star)+(Session.Seats.InLineup(piece.Instance)?" · 上阵":" · 备战"),12,true);
                if(Button(new Rect(292,row.y+5,119,26),"装备所选物品",campaignSelectedItem>0))
                {campaignSelectedPiece=piece.Instance;var item=inventory.FirstOrDefault(x=>x.Instance==campaignSelectedItem);CampaignAction(item?.Key=="复制器"?Campaign.UseCopier(item.Instance,piece.Instance):Campaign.Equip(campaignSelectedItem,piece.Instance));campaignSelectedItem=campaignCombineItem=0;}
                for(int j=0;j<equipment.Length;j++)
                {
                    var slot=new Rect(8+j*134,row.y+35,129,28);
                    Fill(slot,TraitCard);DrawSupplyItem(new Rect(slot.x+2,slot.y+2,24,24),equipment[j].Key);
                    TraitLabel(new Rect(slot.x+30,slot.y,95,28),equipment[j].Name+" ×",11,TraitInk);
                    if(TraitHit(slot,"卸下 "+equipment[j].Name))CampaignAction(Campaign.Unequip(equipment[j].Instance));
                }
            }
            GUI.EndScrollView();
            var first=Campaign.AllEquipment.FirstOrDefault(x=>x.Instance==campaignSelectedItem);
            var second=Campaign.AllEquipment.FirstOrDefault(x=>x.Instance==campaignCombineItem);
            CampaignLabel(new Rect(r.x+22,r.yMax-84,670,62),first==null?"基础件：A 钢刃 / B 弹簧 / C 晶片 / D 护甲板 / E 生命核心 / F 战术终端":first.Name+"："+CampaignItemDescription(first.Key)+(second==null?"":"\n第二件："+second.Name),12);
            if(Button(new Rect(r.xMax-218,r.yMax-78,190,34),"合成所选两件",first!=null&&second!=null))
            {CampaignAction(Campaign.Combine(first.Instance,second.Instance));campaignSelectedItem=campaignCombineItem=0;}
            if(Button(new Rect(r.xMax-218,r.yMax-39,190,25),"清空选择"))campaignSelectedItem=campaignCombineItem=0;
        }
        int CycleCampaignPiece(int current,IEnumerable<ShopTrialModel.Piece> candidates)
        {
            var ids=new[]{0}.Concat(candidates.Select(p=>p.Instance)).Distinct().ToArray();int pos=Array.IndexOf(ids,current);
            return ids[(pos+1+ids.Length)%ids.Length];
        }
        string CampaignItemDescription(string key)
        {
            var data=FormationCampaignRules.FindRecord(key);
            if(data==null)
            {
                var badge = FormationCampaignRun.BadgeTrait(key);
                if (badge != null)
                {
                    var parts = key.Split(':');
                    return "增加「" + badge + "」羁绊标签，占一格装备。每名角色最多携带一枚徽章。\n" +
                        (parts.Length > 2 ? CampaignItemDescription(parts[2]) : "无额外基础属性。");
                }
                return key switch
                {
                    "赤宝石" => "与指定基础装备合成阵营徽章，成品继承该基础装备的属性。",
                    "蓝宝石" => "与指定基础装备合成流派徽章，成品继承该基础装备的属性。",
                    "复制器" => "获得一张目标角色当前形态的一星卡，占用对应卡池库存。",
                    _ => string.Empty
                };
            }
            if (key.Length == 1 && !string.IsNullOrWhiteSpace(data.Effect)) return data.Effect;
            var names=new Dictionary<string,string>{{"attack_pct","攻击"},{"attack_speed","攻速"},{"hp_pct","最大生命"},{"power","能力强度"},{"armor","护甲"},{"mr","法抗"},{"sp_per_second","自然技力/秒"}};
            string stats=string.Join(" / ",data.Stats.Select(p=>(names.TryGetValue(p.Key,out var name)?name:p.Key)+"+"+(p.Key=="attack_pct"||p.Key=="attack_speed"||p.Key=="hp_pct"||p.Key=="power"?(p.Value*100).ToString("0.#")+"%":p.Value.ToString("0.##"))));
            return string.IsNullOrWhiteSpace(stats) ? data.Effect :
                string.IsNullOrWhiteSpace(data.Effect) ? stats : stats + "\n" + data.Effect;
        }
        string CampaignPieceName(int instance)
        {var p=Shop.Bench.FirstOrDefault(x=>x.Instance==instance);return p==null?"自动推荐":Shop.Definition(p.Id).Name;}
        void CampaignPairLine(float y,string label,int value,Action<int> set,IEnumerable<ShopTrialModel.Piece> candidates)
        {
            CampaignLabel(new Rect(205,y,285,28),label,14,true);
            if(Button(new Rect(502,y-2,550,31),CampaignPieceName(value)+"  ›"))set(CycleCampaignPiece(value,candidates));
        }
        void DrawCampaignFormation()
        {
            var r=CampaignWindow("军阵搭档与技能","点击右侧轮换角色；自动推荐会优先选择合法成员。配对与主C优先级只在开战时锁定，本场死亡不改变羁绊档位。");
            var run=Campaign;var lineup=Session.Seats.Lineup(Shop.Bench).ToArray();
            IEnumerable<ShopTrialModel.Piece> Trait(string tag)=>lineup.Where(p=>run.HasTrait(p,tag));
            float y=r.y+104;
            CampaignPairLine(y,"主C优先级 1",run.MainCarryInstance,v=>run.MainCarryInstance=v,lineup);y+=37;
            CampaignPairLine(y,"主C优先级 2",run.SecondCarryInstance,v=>run.SecondCarryInstance=v,lineup);y+=37;
            CampaignPairLine(y,"同频异响 · 领奏者",run.ResonanceLeaderInstance,v=>run.ResonanceLeaderInstance=v,Trait("同频"));y+=37;
            CampaignPairLine(y,"同频异响 · 伴奏者",run.ResonancePartnerInstance,v=>run.ResonancePartnerInstance=v,Trait("同频"));y+=37;
            CampaignPairLine(y,"提词接力 · 搭档 A",run.PromptPairA,v=>run.PromptPairA=v,Trait("提词"));y+=37;
            CampaignPairLine(y,"提词接力 · 搭档 B",run.PromptPairB,v=>run.PromptPairB=v,Trait("提词"));y+=37;
            CampaignPairLine(y,"接棒时刻 · 接棒者",run.RelayReceiverInstance,v=>run.RelayReceiverInstance=v,Trait("接棒"));y+=37;
            CampaignPairLine(y,"无间剧场 · 主角",run.TheaterLeadInstance,v=>run.TheaterLeadInstance=v,Trait("剧场"));y+=43;
            if(Button(new Rect(205,y,265,33),"维维美整备技能：S"+run.WisadelSkill))run.WisadelSkill=run.WisadelSkill%3+1;
            string pact=run.SarkazPact==0?"血契":run.SarkazPact==1?"重契":"双契（6人）";
            if(Button(new Rect(485,y,265,33),"萨卡兹："+pact)){run.SarkazPact=(run.SarkazPact+1)%3;run.SarkazBloodPact=run.SarkazPact!=1;}
            if(Button(new Rect(765,y,287,33),"保存搭档并返回")){RefreshCampaignPreparation();CloseCampaignPanels();}
            CampaignLabel(new Rect(205,y+43,847,30),"便车将按体型和距离推荐合法乘客／载具；重契降低攻频，血契降低生命，双契需达6萨卡兹。",11);
        }
        void DrawCampaignRoute()
        {
            var r=CampaignWindow("行动记录与发展路线","本次挑战的环境、战略战备和三个位面节点。战斗结束统一结算，补给不发基础收入。");
            var viewport=new Rect(r.x+22,r.y+96,r.width-44,400);
            campaignRouteScroll=GUI.BeginScrollView(viewport,campaignRouteScroll,new Rect(0,0,viewport.width-18,860));
            CampaignLabel(new Rect(0,0,850,45),"战略预案："+(FormationCampaignRules.FindRecord(Campaign.PreplanId)?.Name??"待选择")+"\n"+FormationCampaignRules.FindRecord(Campaign.PreplanId)?.Effect,13,true);
            float y=58;
            foreach(string id in Campaign.AugmentIds)
            {var a=FormationCampaignRules.FindRecord(id);CampaignLabel(new Rect(0,y,850,53),$"{a?.Tier}档 · {a?.Name}　{a?.Effect}",12);y+=58;}
            y+=10;
            for(int i=0;i<FormationCampaignRules.Nodes.Length;i++)
            {var n=FormationCampaignRules.Nodes[i];CampaignLabel(new Rect(0,y,850,23),$"{(i<Campaign.NodeIndex?"✓":i==Campaign.NodeIndex?"▶":"·")}  {n.Id}　{n.Kind}　{(n.Kind=="补给"?"补给包五选一":n.DefenderCount+"守军 · 生命×"+n.HealthScale+" · 攻击×"+n.AttackScale)}",12,i==Campaign.NodeIndex);y+=25;}
            GUI.EndScrollView();
        }
        void DrawCampaignScouting()
        {
            var trial=Campaign.SynergyTrial;
            string title=trial==null?$"{Campaign.CurrentNode.Id} · {Campaign.CurrentNode.Kind}":trial.Trait.Name+" · 羁绊试玩";
            double limit=trial==null?Campaign.CurrentNode.TimeLimit:Model.LimitSeconds;
            string time=double.IsPositiveInfinity(limit)?"不限时":$"{limit:0} 秒";
            var r=CampaignWindow("怪物信息",title+" · "+time);
            if(Button(new Rect(r.x+22,r.y+86,112,29),"本关守军",true,!campaignScoutOwn))
            {campaignScoutOwn=false;campaignScoutScroll=Vector2.zero;}
            if(Button(new Rect(r.x+140,r.y+86,112,29),"我方阵容",true,campaignScoutOwn))
            {campaignScoutOwn=true;campaignScoutScroll=Vector2.zero;}
            if(Model.Phase==FormationPhase.Preparation&&Button(new Rect(r.xMax-194,r.y+86,170,29),
                ViewingDefenders?"返回我方布阵":"查看守军部署 [V]"))
            {CloseCampaignPanels();ToggleFormationView();return;}
            if(campaignScoutOwn)
            {
                var lineup=Model.Units.Where(unit=>unit.Enabled).ToArray();
                var area=new Rect(r.x+22,r.y+128,r.width-44,r.height-149);
                campaignScoutScroll=GUI.BeginScrollView(area,campaignScoutScroll,
                    new Rect(0,0,area.width-18,Mathf.Max(area.height,lineup.Length*72)));
                for(int i=0;i<lineup.Length;i++)
                {
                    var unit=lineup[i];float y=i*72;
                    DrawFormationPortrait(new Rect(0,y,57,62),"CARD_"+unit.ArtId);
                    CampaignLabel(new Rect(69,y+3,area.width-101,25),unit.Name+"  "+new string('★',unit.Star),15,true);
                    CampaignLabel(new Rect(69,y+32,area.width-101,22),
                        $"生命 {unit.MaxHealth:0}　攻击 {unit.Attack:0}　护甲 {unit.Armor:0}　法抗 {unit.MagicResistance:0}",12);
                }
                GUI.EndScrollView();return;
            }
            bool encounter=trial==null&&Campaign.CurrentNode.Kind=="遭遇"&&Model.Phase==FormationPhase.Preparation;
            if(encounter)
            {
                for(int i=1;i<=4;i++)
                    if(Button(new Rect(r.x+22+(i-1)*219,r.y+123,207,34),$"{i}档 · HP×{FormationCampaignRules.EncounterHealthScale(i):0.##}",true,Campaign.CurrentEncounterTier==i))
                    {Campaign.CurrentEncounterTier=i;RefreshCampaignPreparation();}
                int tier=Campaign.CurrentEncounterTier;
                string reward=new[]{"蓝球","2个蓝球","金球＋蓝球","2个金球＋彩球"}[tier-1];
                CampaignLabel(new Rect(r.x+22,r.y+163,r.width-44,32),$"当前档攻击×{FormationCampaignRules.EncounterAttackScale(tier):0.##}，额外奖励：{reward}。削减进度严格＞25%才领取；失败照常扣队伍生命并清连胜。",12);
            }
            float top=encounter?199:128;
            var view=new Rect(r.x+22,r.y+top,r.width-44,r.height-top-20);
            campaignScoutScroll=GUI.BeginScrollView(view,campaignScoutScroll,new Rect(0,0,view.width-18,Mathf.Max(view.height,Model.Defenders.Count*110)));
            int row=0;
            foreach(var d in Model.Defenders)
            {
                var guard=FormationCampaignRules.DefenderByArtId(d.ArtId);
                string line=guard==null?"":(guard.Role<=2?"前排":"后排")+(guard.Rarity==6?" · 六星　":"　");
                CampaignLabel(new Rect(0,row*110,850,24),line+d.Name+"　生命 "+d.MaxHealth.ToString("0")+"　攻击 "+d.Attack.ToString("0")+"　护甲 "+d.Armor.ToString("0")+"　法抗 "+d.MagicResistance.ToString("0")+"　射程 "+d.Range.ToString("0.0"),14,true);
                CampaignLabel(new Rect(0,row*110+28,850,25),"技力 "+d.SkillCharge.ToString("0")+" / "+d.SkillRequired.ToString("0")+"　攻击间隔 "+d.Interval.ToString("0.00")+"秒　部署格 "+d.Position,12);
                string effect=FormationCampaignRules.DefenderSkillDescription(Campaign.CurrentNode.Id,d.ArtId);
                CampaignLabel(new Rect(0,row*110+57,850,42),effect,12);row++;
            }
            GUI.EndScrollView();
        }
        void DrawCampaignSynergies() => DrawFormationTraitSidebar();
        bool ProcessCampaignSeaPointer(EventType type,Vector2 point)
        {
            if(!campaignSeaPainting||Campaign==null)return false;
            if(type!=EventType.MouseUp||CameraTransitioning||!BoardInputRect.Contains(point)||FormationShopBlocksPoint(point))return true;
            var ray=battleCamera.ScreenPointToRay(TacticalViewport.Current.ToScreen(point));
            var plane=new Plane(Vector3.up,new Vector3(0,.12f,0));
            if(!plane.Raycast(ray,out float distance))return true;
            var world=ray.GetPoint(distance);
            int x=Mathf.FloorToInt(world.x+FormationBattleModel.Width*.5f+.0001f),y=Mathf.FloorToInt(world.z+FormationBattleModel.Height*.5f+.0001f);
            if(x<FormationBattleModel.DeploymentColumns+1||x>=FormationBattleModel.Width-1||y<0||y>=FormationBattleModel.Height)
            {notice="溟痕请布置在右侧战场内。";return true;}
            int id=x+y*FormationBattleModel.Width,cap=Campaign.TraitCount("海嗣")>=5?10:Campaign.TraitCount("海嗣")>=4?7:4;
            if(!Campaign.SeaCellIds.Remove(id))
            {if(Campaign.SeaCellIds.Count>=cap){notice="当前羁绊最多布置 "+cap+" 格溟痕。";return true;}Campaign.SeaCellIds.Add(id);}
            Model.RefreshSeaPreview();notice=Model.ValidateSeaPlacement(out string reason)?$"溟痕 {Campaign.SeaCellIds.Count}/{cap} 格；保持上下左右连通。":reason;
            return true;
        }
        void DrawCampaignSeaCells()
        {
            if(Campaign==null||Model.SeaCells.Count==0||Model.Phase!=FormationPhase.Preparation)return;
            lineMaterial.SetPass(0);GL.PushMatrix();GL.MultMatrix(Matrix4x4.identity);GL.Begin(GL.QUADS);
            foreach(var cell in Model.SeaCells)
            {
                if(!FormationPointVisibleInView(cell))continue;
                Vector3 p=ActorWorldPoint(cell)+Vector3.up*.026f;
                GL.Color(new Color(.12f,.60f,.65f,.32f));
                GL.Vertex(p+new Vector3(-.43f,0,-.43f));GL.Vertex(p+new Vector3(-.43f,0,.43f));GL.Vertex(p+new Vector3(.43f,0,.43f));GL.Vertex(p+new Vector3(.43f,0,-.43f));
            }
            GL.End();GL.PopMatrix();
        }
    }
}
