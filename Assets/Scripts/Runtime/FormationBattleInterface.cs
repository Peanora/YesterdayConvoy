using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        static readonly Color Dark=new Color(.045f,.075f,.105f,.96f),Panel=ApprovedUi.Paper;
        static readonly Color Cyan=new Color(.28f,.85f,.88f),Gold=new Color(1,.79f,.38f),White=new Color(.9f,.94f,.96f),Muted=new Color(.55f,.67f,.73f);
        static readonly Color DefenderHealthRed = new Color32(226, 53, 58, 255);
        static readonly Color BossHealthRed = new Color32(237, 48, 46, 255);
        const double BossHealthRefillSeconds = .75;
        static readonly Vector2[] DamageNumberOutline =
        {
            new Vector2(-2, 0), new Vector2(2, 0), new Vector2(0, -2), new Vector2(0, 2),
            new Vector2(-1.5f, -1.5f), new Vector2(-1.5f, 1.5f),
            new Vector2(1.5f, -1.5f), new Vector2(1.5f, 1.5f)
        };
        Vector2 formationSynergyScroll,formationRankingScroll;
        GUIStyle formationRankingScrollbar, formationDamageNumberStyle;
        string formationInspectedTag;
        enum FormationMenuPage { Actions, DifficultyTrials, SynergyTrials, GuardTrials }
        FormationMenuPage formationMenuPage;
        GUIStyle Text(int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)=>ApprovedUi.Text(null,size,align,color,size>=22?650:400);
        static void Fill(Rect rect,Color color)=>ApprovedUi.Fill(rect,color);
        void Label(Rect rect,string text,int size=16,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft)=>GUI.Label(rect,text,Text(size,color??White,align));
        bool Button(Rect rect,string text,bool active=true,bool accent=false)
        {
            bool old=GUI.enabled;GUI.enabled=old&&active;
            bool clicked=approvedShop.LoopButton(rect,text,accent);GUI.enabled=old;return clicked;
        }
        public static Rect RosterRect(int slot)=>FormationLineupRect(slot);
        void OnGUI()
        {
            var input=Event.current;
            if (input?.type == EventType.KeyDown && input.keyCode == KeyCode.Escape) ApprovedUi.ReleaseScrollDrag();
            if(Ready&&input!=null&&input.button==0&&(input.type==EventType.MouseDown||input.type==EventType.MouseDrag||input.type==EventType.MouseUp))
            {
                var point=TacticalViewport.Current.FromTopLeft(input.mousePosition);
                if (!ProcessFormationEquipmentPointer(input.type, point) &&
                    !ProcessFormationRewardPointer(input.type, point)) ProcessPointer(input.type,point);
                else input.Use();
            }
            var previous=GUI.matrix;bool enabled=GUI.enabled;GUI.matrix=TacticalViewport.Current.Matrix;
            try
            {
                if(!Ready){Fill(new Rect(0,0,1280,720),Dark);Label(new Rect(150,250,980,80),loading,23,Cyan,TextAnchor.MiddleCenter);return;}
                GUI.enabled=enabled&&!formationMenuOpen;
                if (DrawStrategyChoice())
                {
                    GUI.enabled=enabled;
                    DrawFormationMenuControls();
                    return;
                }
                if(CombatViewActive)
                {
                    DrawWorldLabels();
                    GUI.enabled=enabled&&!formationMenuOpen;
                    DrawFormationCombatHud();
                    DrawFormationRanking();
                    DrawFormationPauseOverlay();
                    GUI.enabled=enabled;
                    DrawFormationMenuControls();
                    return;
                }
                GUI.enabled=enabled&&!shopOddsVisible&&!formationMenuOpen&&!CampaignInteractionBlocked&&shopInputBlockedFrame!=Time.frameCount &&
                    !FormationEquipmentDetailsRect.Contains(input.mousePosition);
                DrawWorldLabels();DrawFormationTraitActivation();DrawFormationHeader();DrawFormationSidebars();
                DrawCampaignToolbar();
                DrawFormationSupplies();
                bool preparing=Model.Phase==FormationPhase.Preparation;
                DrawFormationPreparation();
                DrawFormationResources(preparing);
                DrawFormationScoutControl();
                approvedShop.DrawShopFlows();
                if(preparing)DrawFormationMarket();
                if(!FormationStrategyHistoryOpen)DrawFormationStrategyHistoryToggle();
                if(pointerHeld&&shopDragInstance>0&&Vector2.Distance(formationShopPointer,pointerStart)>6)DrawFormationSaleTargets();
                if(Model.Phase==FormationPhase.Won||Model.Phase==FormationPhase.Lost)DrawSettlement();
                GUI.enabled=enabled&&!formationMenuOpen;
                if(shopOddsVisible)DrawFormationOdds();
                bool showingSupply = Campaign?.SupplyPicksRemaining > 0;
                DrawCampaignOverlays();
                if(!CampaignInteractionBlocked&&!formationMenuOpen&&!shopOddsVisible)
                {
                    DrawCampaignTraitOverlay();
                    DrawFormationRouteDetails();
                    DrawFormationInspection();
                    DrawFormationEquipmentOverlay();
                }
                if (!showingSupply) DrawFormationRewardFlights();
                DrawHeldFormationActor();
                DrawFormationOpeningDelivery();
                DrawFormationExitConfirmation();
                GUI.enabled=enabled;
                if (!formationExitOpen) DrawFormationMenuControls();
            }
            finally
            {
                if (input?.rawType == EventType.MouseUp && input.button == 0) ApprovedUi.ReleaseScrollDrag();
                GUI.enabled=enabled;GUI.matrix=previous;TacticalViewport.Current.DrawBars();
            }
        }
        void DrawFormationHeader()
        {
            if (Campaign != null) { DrawFormationOperationsHeader(); return; }
            bool prep=Model.Phase==FormationPhase.Preparation;
            string phase=prep?"整备阶段":Model.Paused?"已暂停":Model.Phase==FormationPhase.Fighting?"战斗进行中":Model.Phase==FormationPhase.Exiting?"前往蓝门":"本关结束";
            Fill(new Rect(16,8,185,46),ApprovedUi.Paper);Fill(new Rect(16,8,3,46),new Color(.29f,.44f,.55f));
            GUI.Label(new Rect(28,11,165,24),Campaign==null?$"{Model.Round}/5 · 矩形攻坚":$"{Campaign.CurrentNode.Id} · {Campaign.CurrentNode.Kind}",approvedShop.LoopText(17,TextAnchor.MiddleLeft,null,700));
            GUI.Label(new Rect(28,36,165,15),(Campaign?.IsPractice==true?"难度试玩 / ":"军阵行动 / ")+phase,approvedShop.LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            Fill(new Rect(578,8,151,46),ApprovedUi.Paper);ApprovedShopPresentation.DrawLoopShield(new Vector2(589,20),new Color(.40f,.56f,.67f));
            GUI.Label(new Rect(613,14,76,21),"守军防线",approvedShop.LoopText(12));
            GUI.Label(new Rect(613,34,87,14),"存活干员",approvedShop.LoopText(9,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            GUI.Label(new Rect(690,12,32,33),Model.RemainingDefenders.ToString(),approvedShop.LoopText(26,TextAnchor.MiddleCenter,null,700));
            Fill(new Rect(1008,8,110,46),ApprovedUi.Paper);
            GUI.Label(new Rect(1016,10,94,17),"剩余时间",approvedShop.LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            GUI.Label(new Rect(1016,27,94,23),double.IsInfinity(Model.TimeRemaining)?"不限时":Model.TimeRemaining.ToString("0.0")+" 秒",approvedShop.LoopText(20,TextAnchor.MiddleLeft,null,700));
            if(Button(new Rect(1174,8,42,42),Model.Paused?"继续":"暂停",Model.Phase==FormationPhase.Fighting||Model.Phase==FormationPhase.Exiting))Model.Paused=!Model.Paused;
        }
        void DrawFormationSidebars()
        {
            if(Campaign!=null){DrawCampaignSynergies();DrawFormationRanking();return;}
            var ids=Session.Seats.Lineup(Shop.Bench).Select(piece=>piece.Id).ToArray();var summaries=SynergySummary.From(ids);
            ApprovedShopWidgets.DrawSynergies(summaries,Model.Phase==FormationPhase.Preparation,ApprovedUi.Font(null),ref formationSynergyScroll,
                s=>{formationInspectedTag=formationInspectedTag==s.Name?null:s.Name;CancelFormationDrag();},
                (r,s)=>GUI.Label(new Rect(r.x+58,r.y+35,r.width-64,12),"仅标签 · 未启用增益",approvedShop.LoopText(9,TextAnchor.MiddleLeft,new Color(.77f,.88f,.82f))),
                s=>s.Name,false);
            if(formationInspectedTag!=null)
            {
                var rect=new Rect(200,244,380,112);Fill(rect,ApprovedUi.Paper);ApprovedUi.Border(rect,ApprovedUi.Rule);
                GUI.Label(new Rect(rect.x+14,rect.y+10,350,26),formationInspectedTag+" · 编队标签",approvedShop.LoopText(17,TextAnchor.MiddleLeft,null,600));
                var style=approvedShop.LoopText(12,TextAnchor.UpperLeft,ApprovedUi.Muted);style.wordWrap=true;
                GUI.Label(new Rect(rect.x+14,rect.y+46,350,58),"现有角色已适配布阵与占地；战斗采用简化自动技能，原羁绊效果尚未接入。这里只显示真实编队的种类标签。",style);
            }
            DrawFormationRanking();
        }
        void DrawFormationRanking()
        {
            if (Model.Phase == FormationPhase.Preparation)
            {
                formationRankingScroll = Vector2.zero;
                return;
            }
            var rows = Model.Units.Where(u => u.Enabled).OrderByDescending(u => u.DamageDone).ThenBy(u => u.ArtId, StringComparer.Ordinal)
                .Select(u => new ApprovedShopWidgets.RankingRow
                {
                    CardId = "CARD_" + u.ArtId,
                    Name = u.Name,
                    Damage = u.DamageDone,
                    Portrait = FormationUnitArt.For(u.ArtId)?.Portrait
                }).ToArray();
            bool combat = CombatViewActive;
            string state = combat ? (Model.Paused ? "已暂停" : "实时统计") : "本场结算";
            Vector2? panelPosition = combat ? new Vector2(ApprovedUi.RecruitList.x, 72) : null;
            ApprovedShopWidgets.DrawRanking(rows, rows.Sum(row => row.Damage), state, ApprovedUi.Font(null),
                ref formationRankingScroll, ref formationRankingScrollbar, panelPosition, showCaption: false);
        }
        void DrawFormationCombatRoster()
        {
            for(int i=0;i<Model.Units.Count;i++)
            {
                float step=928f/Mathf.Max(10,Model.Units.Count);
                var u=Model.Units[i];var r=new Rect(188+i*step,581,step-4,124);
                approvedShop.DrawLoopCostBackground(r,Shop.Definition("CARD_"+u.ArtId).Cost);
                DrawFormationPortrait(new Rect(r.x+2,r.y+2,r.width-4,96),"CARD_"+u.ArtId);
                approvedShop.DrawShopStars(new Rect(r.xMax-8-u.Star*12,r.y+2,u.Star*12,12),u.Star,new Color(1,.94f,.72f));
                Fill(new Rect(r.x+1,r.y+48,r.width-2,48),new Color(.06f,.11f,.16f,.82f));
                GUI.Label(new Rect(r.x+5,r.y+50,r.width-10,17),u.Name,approvedShop.LoopText(u.Name.Length>6?9:11,TextAnchor.MiddleLeft,Color.white,600));
                GUI.Label(new Rect(r.x+5,r.y+72,r.width-10,18),u.Alive?$"生命 {u.Health:0}":$"复活 {u.RespawnRemaining:0.0}s",approvedShop.LoopText(10,TextAnchor.MiddleLeft,Color.white));
                GUI.Label(new Rect(r.x+5,r.y+100,r.width-10,17),$"伤害 {u.DamageDone:0}",approvedShop.LoopText(10,TextAnchor.MiddleLeft,Color.white));
            }
            Fill(new Rect(1128,581,135,124),ApprovedUi.Paper);
            GUI.Label(new Rect(1138,590,113,23),"剩余时间",approvedShop.LoopText(12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            GUI.Label(new Rect(1136,612,113,54),Model.TimeRemaining.ToString("0.0"),approvedShop.LoopText(34,TextAnchor.MiddleLeft,null,700));
            if(Campaign==null&&Button(new Rect(1138,673,113,25),"返回整备"))Retry();
        }
        void CloseFormationMenu(){formationMenuOpen=false;formationMenuPage=FormationMenuPage.Actions;}

        void OpenFormationTrialPicker(FormationMenuPage page)
        {
            formationMenuOpen=true;
            formationMenuPage=page;
            CancelFormationDrag();
        }

        void DrawFormationMenuControls()
        {
            bool operations = Ready && Campaign != null && !CombatViewActive;
            var toggle=operations?OperationsMenuRect:new Rect(1222,CombatViewActive?12:8,42,CombatViewActive?36:42);
            if(operations?OperationsButton(toggle,"菜单",formationMenuOpen):Button(toggle,"菜单"))
            {
                if(formationMenuOpen)CloseFormationMenu();
                else
                {
                    if (!CampaignInteractionBlocked) CloseCampaignPanels();
                    CloseFormationTraitDetails();
                    CloseFormationInspection();
                    CloseFormationRouteDetails();
                    CancelFormationEquipmentInteraction();
                    CloseFormationEquipmentDetails();
                    formationMenuOpen=true;CancelFormationDrag();
                }
            }
            if(!formationMenuOpen)return;
            if(formationMenuPage==FormationMenuPage.DifficultyTrials){DrawPracticePicker();return;}
            if(formationMenuPage==FormationMenuPage.SynergyTrials){DrawSynergyTrialPicker();return;}
            if(formationMenuPage==FormationMenuPage.GuardTrials){DrawGuardTrialPicker();return;}
            bool practice=Campaign?.IsPractice==true;
            var r=ApprovedUi.Menu;r.y=operations?83:r.y;
            bool preparation = operations && Model.Phase == FormationPhase.Preparation && !CampaignInteractionBlocked;
            bool history = operations && Campaign.BallRewards.Count > 0;
            r.height=(practice?383:345)+(preparation?37:0)+(history?37:0);
            Fill(r,ApprovedUi.Paper);ApprovedUi.Border(r,ApprovedUi.Rule);
            GUI.Label(new Rect(r.x+12,r.y+9,r.width-60,27),"系统菜单",approvedShop.LoopText(19,TextAnchor.MiddleLeft,null,700));
            if(Button(new Rect(r.xMax-36,r.y+9,24,24),"×"))CloseFormationMenu();
            if(Button(new Rect(r.x+12,r.y+44,r.width-24,30),soundEnabled?"关闭音效":"开启音效"))soundEnabled=!soundEnabled;
            if(Button(new Rect(r.x+12,r.y+81,r.width-24,30),practice?"重试当前关卡":"重新开始本次挑战")){ResetDemo();return;}
            if(Button(new Rect(r.x+12,r.y+118,r.width-24,30),"难度试玩 · 无限金币"))OpenFormationTrialPicker(FormationMenuPage.DifficultyTrials);
            if(Button(new Rect(r.x+12,r.y+155,r.width-24,30),"羁绊试玩"))OpenFormationTrialPicker(FormationMenuPage.SynergyTrials);
            if(Button(new Rect(r.x+12,r.y+192,r.width-24,30),"首领试玩 · 圣聆初雪")){SelectPramanixBossTrial();return;}
            if(Button(new Rect(r.x+12,r.y+229,r.width-24,30),"首领试玩 · 维什戴尔·超新星")){SelectSupernovaBossTrial();return;}
            if(Button(new Rect(r.x+12,r.y+266,r.width-24,30),"六星守军试玩"))OpenFormationTrialPicker(FormationMenuPage.GuardTrials);
            if(Button(new Rect(r.x+12,r.y+303,r.width-24,30),"首领试玩 · 结城理")){SelectMakotoBossTrial();return;}
            if(practice&&Button(new Rect(r.x+12,r.y+340,r.width-24,30),"返回正式挑战（新开）"))StartNewFormationChallenge(true);
            float next = r.y + (practice ? 377 : 340);
            if (preparation)
            {
                if (Button(new Rect(r.x + 12, next, r.width - 24, 30), "装备与合成"))
                { CloseFormationMenu(); OpenFormationInventory(); return; }
                next += 37;
            }
            if (history && Button(new Rect(r.x + 12, next, r.width - 24, 30), "战利品记录"))
            { CloseFormationMenu(); OpenFormationRewardHistory(); }
        }

        void DrawPracticePicker()
        {
            bool practice = Campaign?.IsDifficultyTrial == true;
            var r=CampaignWindow("难度试玩 · 无限金币",practice
                ?"换关和重试保留阵容、装备及站位。"
                :"选择战斗关卡后会新开一局难度试玩。",false);
            if(Button(new Rect(r.xMax-57,r.y+15,38,30),"×")){CloseFormationMenu();return;}
            for(int plane=1;plane<=3;plane++)
            {
                float x=r.x+22+(plane-1)*300,y=r.y+88;
                CampaignLabel(new Rect(x,y,280,24),$"位面 {plane}",16,true);
                y+=27;
                for(int i=0;i<FormationCampaignRules.Nodes.Length;i++)
                {
                    var node=FormationCampaignRules.Nodes[i];
                    if(node.Plane!=plane||node.DefenderCount==0)continue;
                    bool current=practice&&i==Campaign.NodeIndex;
                    string time=float.IsPositiveInfinity(node.TimeLimit)?"不限时":$"{node.TimeLimit:0} 秒";
                    var board = FormationBattleMaps.DisplayName(FormationBattleMaps.ForNode(node.Id, node.Plane));
                    if(Button(new Rect(x,y,284,39),$"{node.Id}  {node.Kind}   ·   {board}   ·   {node.DefenderCount} 守军   ·   {time}",true,current))
                    {SelectPracticeBattle(i);return;}
                    y+=47;
                }
            }
        }

        void DrawGuardTrialPicker()
        {
            var r = CampaignWindow("六星守军试玩", "选一名守军，在同源石谷观察普攻与技能。", false);
            if (Button(new Rect(r.xMax - 57, r.y + 15, 38, 30), "×")) { CloseFormationMenu(); return; }
            for (int i = 0; i < 15; i++)
            {
                string id = "D" + (21 + i);
                var guard = FormationCampaignRules.DefenderByArtId(id);
                if (guard == null) continue;
                var button = new Rect(r.x + 24 + i / 8 * 444, r.y + 88 + i % 8 * 45, 416, 37);
                if (Button(button, guard.Name, true, Campaign?.GuardTrialArt == id)) { SelectGuardTrial(id); return; }
            }
        }

        void DrawSynergyTrialPicker()
        {
            var r=CampaignWindow("羁绊试玩", "选择羁绊进入预设军阵。可直接开战，也可先调整角色、搭档与站位。选择关卡会新开一局。", false);
            if(Button(new Rect(r.xMax-57,r.y+15,38,30),"×")){CloseFormationMenu();return;}
            var trials=FormationSynergyTrial.All;
            for(int i=0;i<trials.Count;i++)
            {
                var trial=trials[i];
                var card=new Rect(r.x+22+i%3*300,r.y+96+i/3*64,287,55);
                bool current=Campaign?.SynergyTrial?.Trait.Id==trial.Trait.Id;
                bool clicked=Button(card,string.Empty,true,current);
                CampaignLabel(new Rect(card.x+12,card.y+5,card.width-24,19),trial.Trait.Name,15,true);
                CampaignLabel(new Rect(card.x+12,card.y+29,card.width-24,19),"人数档位  "+string.Join(" / ",trial.Trait.Levels),11);
                if(clicked){SelectSynergyTrial(trial.Trait.Id);return;}
            }
        }
        void DrawWorldLabels()
        {
            var labelFrame=CombatViewActive?new Rect(0,64,1280,656):BoardInputRect;
            foreach(var actor in Model.Units.Where(u=>u.Enabled).Cast<FormationActor>().Concat(Model.Defenders).Concat(Model.Summons))
            {
                if(!actor.Alive||!FormationActorVisibleInView(actor))continue;
                var guard = actor as FormationDefender;
                bool isBoss = guard != null && Model.IsBossDefender(guard);
                float top=formationLabelHeights.TryGetValue(actor.Id,out var labelTop)?labelTop:ActorPresentationHeight(actor);
                var p=TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(FormationDisplayPoint(actor)+battleCamera.transform.up*(top+.12f)));
                if(p.y<labelFrame.yMin+5||p.y>labelFrame.yMax-3)continue;
                float width = isBoss ? 260 : 46;
                float height = isBoss ? 12 : 5;
                var bar = new Rect(p.x - width * .5f, p.y, width, height);
                float fraction = Mathf.Clamp01(actor.Health / actor.MaxHealth);
                Fill(bar, new Color(.015f, .025f, .03f, .9f));
                if (isBoss)
                {
                    float refill = (float)((Model.Clock - guard.BossHealthRefillAt) / BossHealthRefillSeconds);
                    fraction = Mathf.Min(fraction, Mathf.SmoothStep(0, 1, refill));
                    Fill(new Rect(bar.x + 1, bar.y + 1, bar.width - 2, bar.height - 2), new Color32(54, 17, 21, 255));
                }
                Fill(new Rect(bar.x + 1, bar.y + 1, (bar.width - 2) * fraction, bar.height - 2),
                    isBoss ? BossHealthRed : guard != null ? DefenderHealthRed : Cyan);
                Label(new Rect(p.x-65,p.y-18,130,18),actor.Name,isBoss?13:11,White,TextAnchor.MiddleCenter);
                if (Model.Phase == FormationPhase.Preparation && actor is FormationFighter fighter)
                    DrawEquippedItemStrip(fighter.Instance, new Vector2(p.x + 27, p.y - 6), 18);
            }
            if(Model.Phase==FormationPhase.Preparation)
                foreach(var u in Model.Units.Where(u=>u.Enabled))
                {
                    if(!FormationActorVisibleInView(u))continue;
                    var p=CellScreen(u.SpawnCenter);
                    Label(new Rect(p.x-44,p.y-6,88,22),new string('★',u.Star),11,Gold,TextAnchor.MiddleCenter);
                }
            if(Event.current.type!=EventType.Repaint)return;
            foreach(var h in Model.Hits)
            {
                double age=Model.Clock-h.At;if(age<0||age>.8)continue;
                if(!FormationPointVisibleInView(h.To))continue;
                if(h.ShowDamageNumber||h.Heal)DrawFormationHealthPopup(h,(float)age,labelFrame);
            }
        }

        void DrawFormationHealthPopup(FormationHit hit, float age, Rect labelFrame)
        {
            var anchor=FormationFeedbackContactPoint(FormationFeedbackActor(hit.ToId),hit.To,.68f);
            var point=TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(anchor));
            if(!labelFrame.Contains(point))return;
            float remaining=1-age/.8f;
            float rise=12+48*(1-remaining*remaining);
            int lane=(int)Mathf.Repeat((int)Math.Round(hit.At*20)+hit.FromId*2,3)-1;
            const float width=208, height=50;
            float x=Mathf.Clamp(point.x+lane*25-width*.5f,labelFrame.xMin+2,labelFrame.xMax-width-2);
            float y=Mathf.Clamp(point.y-rise-height*.5f,labelFrame.yMin+2,labelFrame.yMax-height-2);
            var rect=new Rect(x,y,width,height);

            float size=age<.10f ? Mathf.SmoothStep(28,39,age/.10f)
                : Mathf.SmoothStep(39,32,Mathf.InverseLerp(.10f,.24f,age));
            float alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.43f,.8f,age));
            formationDamageNumberStyle??=ApprovedUi.Text(null,32,TextAnchor.MiddleCenter,Color.white,650);
            var style=formationDamageNumberStyle;
            style.fontSize=Mathf.RoundToInt(size);
            string amount=hit.Amount is > 0 and < 1 ? hit.Amount.ToString("0.0#") : hit.Amount.ToString("0");
            string number=hit.Heal?"+"+amount:amount;
            style.normal.textColor=hit.Heal?new Color(.035f,.13f,.045f,alpha):new Color(.13f,.025f,.035f,alpha);
            foreach(var offset in DamageNumberOutline)
                GUI.Label(new Rect(rect.position+offset,rect.size),number,style);
            style.normal.textColor=hit.Heal?new Color(.35f,1f,.43f,alpha):new Color(1f,.30f,.28f,alpha);
            GUI.Label(rect,number,style);
        }

        void DrawSettlement()
        {
            if(Campaign?.IsPractice==true){DrawPracticeResult();return;}
            if(Campaign!=null){if(!Campaign.Finished||AwaitingOptionalRewards&&!campaignResultSummaryOpen)DrawCampaignResult();return;}
            bool won=Model.Phase==FormationPhase.Won;Fill(new Rect(380,172,520,324),Dark);ApprovedUi.Border(new Rect(380,172,520,324),won?Cyan:Gold,2);
            Label(new Rect(405,191,470,45),won?"防线肃清 · 蓝门突破":"进攻未完成",29,won?Cyan:Gold,TextAnchor.MiddleCenter);
            Label(new Rect(410,245,460,32),won?$"剩余 {Model.TimeRemaining:0.0} 秒":$"仍有 {Model.Defenders.Count(d=>d.Alive)} 名守军，或未及时进门",16,White,TextAnchor.MiddleCenter);
            var top=Model.Units.OrderByDescending(u=>u.DamageDone).FirstOrDefault();
            if(top!=null)Label(new Rect(410,287,460,25),$"本关输出：{top.Name} {top.DamageDone:0}  ·  全队阵亡 {Model.Units.Sum(u=>u.Deaths)} 次",15,Muted,TextAnchor.MiddleCenter);
            Label(new Rect(410,327,460,43),won?"领取通关金币与经验，带着已有军阵继续整备。":"返回同关调整军阵；重试保留购买成果，不重复发奖。",14,Muted,TextAnchor.MiddleCenter);
            if(Button(new Rect(410,400,220,47),"同关整备重试"))Retry();
            if(Button(new Rect(650,400,220,47),Model.Round<5?"领取奖励 · 下一关":finalRewardClaimed?"开始新挑战":"领取奖励 · 完成挑战",won,true))
            {
                if(Model.Round<5)NextRound();else if(finalRewardClaimed)ResetDemo();
                else {var reward=Session.SettleWin(Model.Round);finalRewardClaimed=reward!=null;notice=reward!=null?$"最终奖励 +{reward.Total}金币、+{reward.Experience}经验；新挑战前保留购买记录。":"奖励尚未结算。";}
            }
        }
    }
}
