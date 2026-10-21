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
        public float EffectiveSimulationSpeed => Battle==null || Battle.IsPaused ? 0 : SkillMenuOpen ? .03f : SimulationSpeed;
        public static Rect RosterSlot => new Rect(1126,556,112,144);

        private static Vector2 GUIPosition(Vector2 screen) => new Vector2(screen.x*1280/Screen.width,720-screen.y*720/Screen.height);
        public static Vector2 ScreenPosition(Vector2 gui) => new Vector2(gui.x*Screen.width/1280,(720-gui.y)*Screen.height/720);
        private static bool IsBattlefield(Vector2 screen) => new Rect(0,66,1280,486).Contains(GUIPosition(screen));
        private bool PointerGround(Vector2 screen,out Vector3 point)
        {
            Ray ray=Camera.main.ScreenPointToRay(screen);
            var plane=new Plane(Vector3.up,Battle.Entry);
            if(plane.Raycast(ray,out float distance)) { point=ray.GetPoint(distance); return true; }
            point=Battle.Entry; return false;
        }

        public bool ProcessPointerEvent(EventType type, Vector2 guiPosition, int button)
        {
            if(button!=0) return false;
            Vector2 screen=new Vector2(guiPosition.x,Screen.height-guiPosition.y);
            if(SettlementVisible)
            {
                if(type==EventType.MouseDown) { settlementPointerDown=SettlementRestartRect.Contains(GUIPosition(screen)); return true; }
                if(type==EventType.MouseUp)
                {
                    bool restart=settlementPointerDown && SettlementRestartRect.Contains(GUIPosition(screen));
                    settlementPointerDown=false;
                    if(restart) RestartBattle();
                    return true;
                }
                return type==EventType.MouseDrag;
            }
            if(type==EventType.MouseDown)
            {
                bool handled=RosterSlot.Contains(GUIPosition(screen)) || IsBattlefield(screen);
                ProcessPointerDown(screen);
                return handled;
            }
            if(type==EventType.MouseDrag && Dragging) { ProcessPointerMove(screen); return true; }
            if(type==EventType.MouseUp && Dragging) { ProcessPointerUp(screen); return true; }
            return false;
        }

        public void ProcessPointerDown(Vector2 screen)
        {
            pointerScreen=screen;
            if(Battle.Phase==BattlePhase.Won || Battle.Phase==BattlePhase.Lost) { CancelInteraction(); return; }
            if(RosterSlot.Contains(GUIPosition(screen)))
            {
                CloseSkillMenu(); Dragging=true; dragRunId=Battle.RunId; Selected=true;
                Feedback="拖到红门后松手部署；右键/Esc取消"; UpdatePreview(); return;
            }
            if(Dragging) return;
            if(SkillMenuOpen && SkillPopupRect.Contains(GUIPosition(screen)))
            { if(SkillCastRect.Contains(GUIPosition(screen))) CastFocusedSkill(); return; }
            string hit=IsBattlefield(screen) ? HitUnit(screen) : null;
            if(hit!=null) { OpenSkillMenu(hit); return; }
            if(SkillMenuOpen) { CloseSkillMenu(); Feedback="已取消技能选择"; return; }
        }

        public void ProcessPointerMove(Vector2 screen) { pointerScreen=screen; UpdatePreview(); }

        public DeploymentReceipt ProcessPointerUp(Vector2 screen)
        {
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
            Vector2 head=GUIPosition(cam.WorldToScreenPoint(unit.Motion.Position+cam.transform.up*1.72f));
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
            if(SkillMenuOpen && hits.Count>1)
            {
                int current=hits.FindIndex(u=>u.EntityId==selectedEntityId);
                if(current>=0) return hits[(current+1)%hits.Count].EntityId;
            }
            return hits[0].EntityId;
        }

        public void OpenSkillMenu(string id)
        {
            if(!Battle.EngagementEnabled || Battle.Phase!=BattlePhase.Running || Battle.FindUnit(id)?.Active!=true) return;
            Dragging=false; Selected=false; selectedEntityId=id; SkillMenuOpen=true;
            Feedback="选择角色技能；点击空白处取消"; UpdatePreview();
        }
        public void CloseSkillMenu() { SkillMenuOpen=false; }
        public SkillFailure CastFocusedSkill()
        {
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
                float x=unit.xMax+18;
                if(x+214>1264) x=unit.xMin-232;
                return new Rect(Mathf.Clamp(x,16,1050),Mathf.Clamp(unit.yMin-8,78,382),214,154);
            }
        }
        public Rect SkillCastRect { get { Rect panel=SkillPopupRect; return new Rect(panel.x+14,panel.y+45,66,66); } }

        private bool ReadyMarker(string id) => Battle.IsSkillReady(id);
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
            if(SkillMenuOpen) Fill(new Rect(0,66,1280,486),new Color(0,0,0,.16f));
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
                Sprite portrait=OriginalTalulahArt.CardPortrait(definition.Card); Rect source=portrait.textureRect;
                GUI.DrawTextureWithTexCoords(new Rect(p.x-28,p.y-74,56,66),portrait.texture,
                    new Rect(source.x/portrait.texture.width,source.y/portrait.texture.height,source.width/portrait.texture.width,source.height/portrait.texture.height));
            }
            if(!SkillMenuOpen) return;
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
            string charge=Battle.CombatSettings.FireRain && Battle.IsSkillActionPlaying(selected) ? $"火雨 {selected.SkillAmmo} 波" : selected.SkillAmmo>0 ? $"弹药 {selected.SkillAmmo}/6" : $"技力 {selected.SkillSP:0}/{Battle.CombatSettings.RequiredSP:0}";
            GUI.Label(new Rect(panel.x+93,panel.y+45,116,26),charge,smallStyle);
            GUI.Label(new Rect(panel.x+93,panel.y+76,116,26),Battle.IsPaused ? "暂停中" : ready ? "点击释放" : selected.SkillAmmo>0 ? "技能生效中" : "技力回复中",smallStyle);
            GUI.Label(new Rect(panel.x+14,panel.y+119,194,25),Battle.IsPaused ? "恢复后可释放技能" : "0.03×  ·  点空白处取消",smallStyle);
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
            Time.timeScale=0; StartBattle(); Battle.SetPaused(true);
            Vector2 card=ScreenPosition(RosterSlot.center),entry=Camera.main.WorldToScreenPoint(Battle.Entry);
            ProcessPointerDown(card); ProcessPointerMove(entry);
            yield return InteractionFrame(directory,"ui-paused-drag");
            bool placed=ProcessPointerUp(entry).Success && Battle.IsPaused && Battle.DP==10 && Battle.Units.Count==1;
            ProcessPointerUp(entry); Tick(10);
            bool once=Battle.Units.Count==1 && Battle.Clock==0 && Battle.Units[0].SkillSP==8 && Battle.CooldownRemaining==2;
            ProcessPointerDown(card);
            bool invalid=ProcessPointerUp(Camera.main.WorldToScreenPoint(scene.BlueGate.position)).Failure==DeployFailure.WrongEntry && Battle.DP==10;
            yield return InteractionFrame(directory,"ui-paused-placed");
            Battle.SetPaused(false); Tick(4);
            string id=Battle.Units[0].EntityId;
            bool ready=Battle.IsSkillReady(id) && Battle.SkillCasts==0;
            yield return InteractionFrame(directory,"ui-skill-ready");
            ToggleSpeed(); ProcessPointerDown(ScreenPosition(UnitScreenRect(id).center));
            double clock=Battle.Clock; Tick(1);
            bool slow=SkillMenuOpen && System.Math.Abs(Battle.Clock-clock-.03)<.0001 && EffectiveSimulationSpeed==.03f;
            yield return InteractionFrame(directory,"ui-skill-focus");
            ProcessPointerDown(ScreenPosition(new Vector2(12,500)));
            bool cancel=!SkillMenuOpen && EffectiveSimulationSpeed==2 && Battle.SkillCasts==0 && Battle.IsSkillReady(id);
            ProcessPointerDown(ScreenPosition(UnitScreenRect(id).center));
            ProcessPointerDown(ScreenPosition(SkillCastRect.center));
            bool cast=!SkillMenuOpen && Battle.SkillCasts==1 && Battle.Units[0].SkillAmmo==6 && !Battle.IsSkillReady(id) && Battle.DP==10 && EffectiveSimulationSpeed==2;
            yield return InteractionFrame(directory,"ui-skill-cast");
            RestartBattle();
            bool reset=!Dragging && !SkillMenuOpen && Battle.Units.Count==0 && Battle.DP==12 && SimulationSpeed==1;
            Time.timeScale=1;
            Debug.Log($"INTERACTION_CHECK: pausedDrop={placed}; onceFrozen={once}; invalid={invalid}; ready={ready}; slow={slow}; cancel={cancel}; cast={cast}; reset={reset}.");
            Application.Quit(placed && once && invalid && ready && slow && cancel && cast && reset ? 0 : 1);
        }
    }
}
