using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        [SerializeField] private Sprite skillIcon;
        private Vector2 pointerScreen;
        private int dragRunId;
        private bool settlementPointerDown;
        public bool Dragging { get; private set; }
        public bool SkillMenuOpen { get; private set; }
        public float EffectiveSimulationSpeed => Battle==null || TestSelectionOpen || Battle.IsPaused ? 0 : SkillMenuOpen&&!Battle.AutomaticSkillsEnabled ? .03f :
            Battle.WavesEnabled&&Battle.GuerrillaOrderBusy?Mathf.Min(1,SimulationSpeed):SimulationSpeed;
        public static Rect RosterSlot => new Rect(1126,556,112,144);

        private static Vector2 GUIPosition(Vector2 screen) => TacticalViewport.Current.FromScreen(screen);
        public static Vector2 ScreenPosition(Vector2 gui) => TacticalViewport.Current.ToScreen(gui);
        private bool IsBattlefield(Vector2 screen) => ShopTrialActive?ShopMapPointAvailable(GUIPosition(screen)):new Rect(0,66,1280,486).Contains(GUIPosition(screen));
        private bool PointerGround(Vector2 screen,out Vector3 point)
        {
            Ray ray=Camera.main.ScreenPointToRay(screen);
            var plane=new Plane(Vector3.up,Battle.Entry);
            if(plane.Raycast(ray,out float distance)) { point=ray.GetPoint(distance); return true; }
            point=Battle.Entry; return false;
        }

        public bool ProcessPointerEvent(EventType type, Vector2 guiPosition, int button)
        {
            if(TestSelectionOpen) return false;
            if(button!=0) return false;
            Vector2 screen=new Vector2(guiPosition.x,Screen.height-guiPosition.y);
            if(SettlementVisible)
            {
                if(type==EventType.MouseDown)
                { settlementPointerDown=SettlementRestartRect.Contains(GUIPosition(screen)); selectionReturnPointerDown=SettlementReturnRect.Contains(GUIPosition(screen)); return true; }
                if(type==EventType.MouseUp)
                {
                    bool restart=settlementPointerDown && SettlementRestartRect.Contains(GUIPosition(screen));
                    bool returnToMenu=selectionReturnPointerDown && SettlementReturnRect.Contains(GUIPosition(screen));
                    settlementPointerDown=selectionReturnPointerDown=false;
                    if(returnToMenu) ReturnToMonsterTests(); else if(restart) RestartBattle();
                    return true;
                }
                return type==EventType.MouseDrag;
            }
            if(type==EventType.MouseDown)
            {
                bool handled=IsBattlefield(screen);
                for(int i=0;i<RosterCount;i++)handled|=CardSlot(i).Contains(GUIPosition(screen));
                ProcessPointerDown(screen);
                return handled;
            }
            if(type==EventType.MouseDrag && Dragging) { ProcessPointerMove(screen); return true; }
            if(type==EventType.MouseUp && Dragging) { ProcessPointerUp(screen); return true; }
            return false;
        }

        public void ProcessPointerDown(Vector2 screen)
        {
            if(TestSelectionOpen) return;
            pointerScreen=screen;
            if(Battle.Phase==BattlePhase.Won || Battle.Phase==BattlePhase.Lost) { CancelInteraction(); return; }
            int cardIndex=-1;for(int i=0;i<RosterCount;i++)if(CardSlot(i).Contains(GUIPosition(screen)))cardIndex=i;
            if(cardIndex>=0)
            {
                selectedRosterIndex=cardIndex; CloseSkillMenu(); Dragging=true; dragRunId=Battle.RunId; Selected=true;
                Feedback="拖到红门后松手部署；右键/Esc取消"; UpdatePreview(); return;
            }
            if(Dragging) return;
            if(SkillMenuOpen && SkillPopupRect.Contains(GUIPosition(screen)))
            { if(Battle.AutomaticSkillsEnabled)return;if(TryBossRosterPopupPointer(GUIPosition(screen)))return;if(SkillCastRect.Contains(GUIPosition(screen))) CastFocusedSkill(); return; }
            string hit=IsBattlefield(screen) ? HitUnit(screen) : null;
            if(hit!=null) { OpenSkillMenu(hit); return; }
            if(SkillMenuOpen) { CloseSkillMenu(); Feedback="已取消技能选择"; return; }
        }

        public void ProcessPointerMove(Vector2 screen) { pointerScreen=screen; UpdatePreview(); }

        public DeploymentReceipt ProcessPointerUp(Vector2 screen)
        {
            if(TestSelectionOpen) return new DeploymentReceipt(DeployFailure.NotStarted);
            pointerScreen=screen;
            if(!Dragging) return new DeploymentReceipt(DeployFailure.NotStarted);
            Dragging=false;
            DeploymentReceipt receipt;
            if(dragRunId!=Battle.RunId) receipt=new DeploymentReceipt(DeployFailure.StaleRun);
            else if(!IsBattlefield(screen) || !PointerGround(screen,out Vector3 point)) receipt=new DeploymentReceipt(DeployFailure.WrongEntry);
            else receipt=TryDeployAt(point,true);
            Selected=false; UpdatePreview();
            if(!receipt.Success)
            {
                Feedback=receipt.Failure==DeployFailure.WrongEntry ? "部署已取消：请拖到红门" : Explain(receipt.Failure);
                ShowNotice(Feedback);
            }
            return receipt;
        }

        public void CancelInteraction()
        {
            Dragging=false; Selected=false; CloseSkillMenu();
            Feedback="拖动底部角色卡至红门部署";
            if(preview!=null) preview.enabled=false;
            if(entryOutline!=null) entryOutline.enabled=false;
        }

        public Rect UnitScreenRect(string id)
        {
            var unit=Battle.FindUnit(id);
            if(unit==null) return Rect.zero;
            Camera cam=Camera.main;
            Vector2 feet=GUIPosition(cam.WorldToScreenPoint(unit.Motion.Position));
            Vector2 head=GUIPosition(cam.WorldToScreenPoint(unit.Motion.Position+cam.transform.up*UnitVisualHeight(unit)));
            Vector2 side=GUIPosition(cam.WorldToScreenPoint(unit.Motion.Position+cam.transform.right*.58f));
            return Rect.MinMaxRect(feet.x-Mathf.Abs(side.x-feet.x),head.y,feet.x+Mathf.Abs(side.x-feet.x),feet.y+5);
        }

        public string HitUnit(Vector2 screen)
        {
            var hits=new List<DeployedUnitState>();
            foreach(var unit in Battle.Units)
            {
                if(unit.Active && (UnitScreenRect(unit.EntityId).Contains(GUIPosition(screen)) ||
                    ReadyMarker(unit.EntityId) && ReadyMarkerRect(unit.EntityId).Contains(GUIPosition(screen)))) hits.Add(unit);
            }
            hits.Sort((a,b)=>
            {
                int order=DemoSpriteVisual.GetSortingOrder(Camera.main,b.Motion.Position).CompareTo(DemoSpriteVisual.GetSortingOrder(Camera.main,a.Motion.Position));
                return order!=0 ? order : b.SpawnTime.CompareTo(a.SpawnTime);
            });
            if(hits.Count==0) return null;
            if((SkillMenuOpen || TacticalMapActive) && hits.Count>1)
            {
                int current=hits.FindIndex(u=>u.EntityId==selectedEntityId);
                if(current>=0) return hits[(current+1)%hits.Count].EntityId;
            }
            return hits[0].EntityId;
        }

        public void OpenSkillMenu(string id)
        {
            if(TestSelectionOpen) return;
            var picked=Battle.FindUnit(id);
            if(Battle.AutomaticSkillsEnabled)
            {
                if(picked?.Active!=true||Battle.Phase!=BattlePhase.Running)return;
                Dragging=false;Selected=false;selectedEntityId=id;SkillMenuOpen=true;
                Feedback="技能按原作时机自动触发；点空白关闭信息";UpdatePreview();return;
            }
            if(BossRosterCatalog.Contains(picked?.CardId)&&Battle.BossRosterSkills(picked).Count==0){selectedEntityId=id;Feedback=CardForUnit(picked).DisplayName+"的天赋会自动触发";return;}
            if(TacticalMapActive && picked?.Active==true && !Battle.UnitCombat(picked).HasSkill)
            {CloseSkillMenu();selectedEntityId=id;Feedback=CardForUnit(picked).DisplayName+"没有主动技能";return;}
            if(!Battle.EngagementEnabled || !Battle.UnitCombat(Battle.FindUnit(id)).HasSkill || Battle.Phase!=BattlePhase.Running || Battle.FindUnit(id)?.Active!=true) return;
            Dragging=false; Selected=false; selectedEntityId=id; SkillMenuOpen=true;
            Feedback="选择角色技能；点击空白处取消"; UpdatePreview();
        }
        public void CloseSkillMenu() { SkillMenuOpen=false; }
        public SkillFailure CastFocusedSkill()
        {
            if(Battle?.AutomaticSkillsEnabled==true){Feedback="技能自动触发；此面板仅显示状态";return SkillFailure.NotReady;}
            if(!SkillMenuOpen) return SkillFailure.MissingEntity;
            var result=ActivateSelectedSkill();
            if(result==SkillFailure.None) CloseSkillMenu();
            else ShowNotice(Feedback);
            return result;
        }

        public Rect SkillPopupRect
        {
            get
            {
                Rect unit=UnitScreenRect(selectedEntityId);
                if(IsBossRosterSelection||Battle.AutomaticSkillsEnabled){int count=DisplayedBossSkills(Battle.FindUnit(selectedEntityId)).Count;float h=count>0?76+(Battle.AutomaticSkillsEnabled?72:57)*count:212;return new Rect(Mathf.Clamp(unit.xMax+12,16,940),Mathf.Clamp(unit.yMin-8,78,Mathf.Max(78,550-h)),300,h);}
                float x=unit.xMax+18;
                if(x+214>1264) x=unit.xMin-232;
                var panel=new Rect(Mathf.Clamp(x,16,1050),Mathf.Clamp(unit.yMin-8,78,382),214,154);
                if(DamageRankingVisible&&panel.Overlaps(DamageRankingPanelRect))panel.x=DamageRankingPanelRect.x-panel.width-12;
                return panel;
            }
        }
        public Rect SkillCastRect { get { Rect panel=SkillPopupRect; return new Rect(panel.x+14,panel.y+45,66,66); } }

        private bool ReadyMarker(string id) => !Battle.AutomaticSkillsEnabled&&Battle.IsSkillReady(id);
        public Rect ReadyMarkerRect(string id)
        {
            var unit=Battle.FindUnit(id); if(unit==null) return Rect.zero;
            Vector2 head=GUIPosition(Camera.main.WorldToScreenPoint(unit.Motion.Position+Camera.main.transform.up*1.8f));
            return new Rect(head.x-40,head.y-31,80,26);
        }
        private static readonly Color ReadyGold=new Color(.96f,.77f,.29f);

        private void DrawSkillIcon(Rect rect,Color color)
        {
            Color previous=GUI.color; GUI.color=color;
            if(skillIcon!=null)
            {
                Rect source=skillIcon.textureRect; Texture2D texture=skillIcon.texture;
                GUI.DrawTextureWithTexCoords(rect,texture,new Rect(source.x/texture.width,source.y/texture.height,source.width/texture.width,source.height/texture.height));
            }
            GUI.color=previous;
        }

        private void DrawInteractionOverlay()
        {
            DrawBossPreparation();
            if(SkillMenuOpen&&!Battle.AutomaticSkillsEnabled) Fill(new Rect(0,66,1280,486),new Color(0,0,0,.16f));
            foreach(var unit in Battle.Units)
            {
                if(!unit.Active || !ReadyMarker(unit.EntityId)) continue;
                Rect badge=ReadyMarkerRect(unit.EntityId);
                Fill(badge,ReadyGold); Fill(new Rect(badge.x+1,badge.y+1,78,24),new Color(.055f,.09f,.12f));
                DrawSkillIcon(new Rect(badge.x+4,badge.y+3,20,20),ReadyGold);
                GUI.Label(new Rect(badge.x+26,badge.y+1,54,24),"就绪",smallStyle);
            }
            if(Dragging && !IsBattlefield(pointerScreen))
            {
                Vector2 p=GUIPosition(pointerScreen);
                Fill(new Rect(p.x-30,p.y-76,60,70),new Color(.24f,.51f,.58f,.85f));
                Sprite portrait=OriginalTalulahArt.CardPortrait(DeploymentDefinition.Card); Rect source=portrait.textureRect;
                GUI.DrawTextureWithTexCoords(new Rect(p.x-28,p.y-74,56,66),portrait.texture,
                    new Rect(source.x/portrait.texture.width,source.y/portrait.texture.height,source.width/portrait.texture.width,source.height/portrait.texture.height));
            }
            if(!SkillMenuOpen) return;
            if(IsBossRosterSelection){DrawBossRosterPopup();return;}
            if(Battle.AutomaticSkillsEnabled){DrawAutomaticUnitInfo();return;}
            var selected=Battle.FindUnit(selectedEntityId);
            if(selected==null || !selected.Active) return;
            Rect bounds=UnitScreenRect(selectedEntityId),panel=SkillPopupRect,button=SkillCastRect;
            Fill(new Rect(bounds.xMin,bounds.yMax-4,bounds.width,3),ReadyGold);
            Fill(panel,new Color(.04f,.075f,.10f,.98f)); Fill(new Rect(panel.x,panel.y,panel.width,3),ReadyGold);
            GUI.Label(new Rect(panel.x+14,panel.y+10,145,29),SkillName,textStyle);
            int entityNumber=1;
            foreach(var unit in Battle.Units) { if(unit.EntityId==selectedEntityId) break; entityNumber++; }
            GUI.Label(new Rect(panel.x+166,panel.y+12,42,26),$"#{entityNumber:00}",smallStyle);
            bool ready=Battle.CheckSkill(selectedEntityId)==SkillFailure.None;
            Fill(button,ready ? ReadyGold : new Color(.22f,.29f,.32f));
            DrawSkillIcon(new Rect(button.x+9,button.y+9,48,48),ready ? new Color(.06f,.10f,.13f) : new Color(.55f,.62f,.65f));
            var combat=Battle.UnitCombat(selected);
            string charge=combat.MudrockCapture&&Battle.IsSkillActionPlaying(selected)?"占据施法中":combat.MudrockCapture&&Battle.CheckSkill(selectedEntityId)==SkillFailure.NoTarget?"无可占据装置":combat.FrostSeal && Battle.IsSkillActionPlaying(selected) ? "冰封施法中" : combat.FireRain && Battle.IsSkillActionPlaying(selected) ? $"火雨 {selected.SkillAmmo} 波" : selected.SkillAmmo>0 ? $"弹药 {selected.SkillAmmo}/6" : $"技力 {selected.SkillSP:0}/{combat.RequiredSP:0}";
            if(SilverWolfCombat.IsWolf(selected))
            {
                if(SilverWolfCombat.Transforming(selected,Battle.Clock)) charge="变身中";
                else if(selected.SkillAmmo>0 || Battle.IsSkillActionPlaying(selected)) charge=$"盲盒 {selected.SkillAmmo}/{combat.SkillAmmo}";
            }
            GUI.Label(new Rect(panel.x+93,panel.y+45,116,26),charge,smallStyle);
            GUI.Label(new Rect(panel.x+93,panel.y+76,116,26),Battle.IsPaused ? "暂停中" : ready ? "点击释放" : Battle.IsSkillActionPlaying(selected)||selected.SkillAmmo>0 ? "技能生效中" : combat.FrostSeal && Battle.Clock<selected.IceRingStartedAt+3 ? "冰环动作中" : "技力回复中",smallStyle);
            GUI.Label(new Rect(panel.x+14,panel.y+119,194,25),Battle.IsPaused ? "恢复后可释放技能" : "0.03×  ·  点空白处取消",smallStyle);
        }

        void DrawAutomaticUnitInfo()
        {
            var unit=Battle.FindUnit(selectedEntityId);if(unit?.Active!=true)return;
            var panel=SkillPopupRect;var combat=Battle.UnitCombat(unit);
            Fill(panel,new Color(.04f,.075f,.10f,.98f));Fill(new Rect(panel.x,panel.y,panel.width,3),ReadyGold);
            GUI.Label(new Rect(panel.x+12,panel.y+9,panel.width-24,27),CardForUnit(unit).DisplayName+" · 信息",textStyle);
            GUI.Label(new Rect(panel.x+12,panel.y+41,panel.width-24,24),$"生命 {unit.Health:0}/{unit.MaxHealth:0}",smallStyle);
            string description=BossRosterMenuDescription(unit.CardId)??(combat.Effects.Healer?"自动治疗受伤友军；不攻击守军":combat.HasSkill?SkillName+" · 满足条件时自动施放":"普通攻击与天赋按战斗条件自动触发");
            if(bossDescriptionStyle==null)bossDescriptionStyle=new GUIStyle(smallStyle){wordWrap=true};
            GUI.Label(new Rect(panel.x+12,panel.y+68,panel.width-24,54),Battle.AutomaticSkillStatus(unit),bossDescriptionStyle);
            GUI.Label(new Rect(panel.x+12,panel.y+128,panel.width-24,48),description,bossDescriptionStyle);
            GUI.Label(new Rect(panel.x+12,panel.yMax-26,panel.width-24,22),"信息不减速 · 点空白关闭",smallStyle);
        }

        private IEnumerator InteractionFrame(string directory,string name)
        {
            if(directory==null) { yield return null; yield break; }
            Directory.CreateDirectory(directory);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));
            yield return null; yield return null;
        }

        private IEnumerator VerifyInteraction(string directory)
        {
            Time.timeScale=0;BossRosterSmokeResult result=null;
            yield return BossRosterSmokeProbe.Run(this,directory,new[]{wisadelDefinition.Card.CardId},value=>result=value,"s3");
            Time.timeScale=1;Debug.Log("INTERACTION_AUTOMATIC_CHECK: "+JsonUtility.ToJson(result));
            Application.Quit(result?.passed==true?0:1);
        }

    }
}
