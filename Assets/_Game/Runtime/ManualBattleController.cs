using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Scene/input adapter for the single-card level. Simulation and transactions belong to ManualBattle.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed partial class ManualBattleController : MonoBehaviour
    {
        [SerializeField] private InitialDemo scene;
        [SerializeField] private ManualBattleDefinition definition;
        [SerializeField] private AudioClip uiSound, hitSound;
        [SerializeField] private EngagementView combatView;
        private string selectedEntityId;
        private bool diagnostic;
        private readonly Dictionary<string, Transform> views = new Dictionary<string, Transform>();
        private readonly List<string> removed = new List<string>();
        private DemoCardView cardView;
        private SpriteRenderer preview;
        private LineRenderer entryOutline;
        private Material previewMaterial;
        private GUIStyle textStyle, smallStyle, numberStyle, buttonStyle, noticeStyle, resultStyle;
        private string notice;
        private float noticeUntil;
        private long requestSequence;
        public ManualBattle Battle { get; private set; }
        public ManualBattleDefinition Definition => definition;
        public bool Selected { get; private set; }
        public string Feedback { get; private set; } = "拖动底部角色卡至红门部署";
        public int ViewCount => views.Count;
        public float SimulationSpeed { get; private set; } = 1;
        public bool Muted { get; private set; }
        public string SelectedEntityId => selectedEntityId;
        public EngagementView CombatView => combatView;
        private double terminalPresentationElapsed;
        public double PresentationClock => Battle.Clock+terminalPresentationElapsed;
        private bool SlugActive => definition.Card.CardId==OriginalSlugArt.CardId;
        private LocalMonsterArt CurrentLocalArt => LocalMonsterArt.For(definition.Card.CardId);
        private double DeathTail => Math.Max(CurrentLocalArt?.DeathDuration ?? (SlugActive ? .5 : 0),DefenderTrialActive ? System.Math.Max(1,RosterDeathTail) : 0);
        private bool DeathPresentationPending
        {
            get
            {
                foreach(var unit in Battle.Units) if(unit.Resolution==UnitResolution.Dead && PresentationClock<unit.DeathTime+DeathTail) return true;
                foreach(var blast in Battle.DeathBlasts) if(blast.Resolved && PresentationClock<blast.DetonateAt+.65) return true;
                if(DefenderTrialActive) foreach(var d in Battle.Defenders) if(!d.Alive && PresentationClock<d.DeathTime+1) return true;
                return false;
            }
        }
        public bool SettlementVisible => Battle!=null && (Battle.Phase==BattlePhase.Won || Battle.Phase==BattlePhase.Lost) &&
            !DeathPresentationPending;
        public static Rect SettlementRestartRect => new Rect(480,454,320,54);

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            diagnostic = Application.isBatchMode || Array.IndexOf(args,"-combatSmokeTest") >= 0 || Array.IndexOf(args,"-interactionSmokeTest") >= 0;
            wisadelDefinition=definition;
            BuildMonsterTestCatalog();
            ValidateConfiguration();
            scene.enabled = false;
            scene.Monster.gameObject.SetActive(false);
            cardView = FindAnyObjectByType<DemoCardView>();
            Battle = CreateBattle();
            CreatePreview();
            EnableCombatView();
            gameObject.AddComponent<FireRainView>().Initialize(this);
            bool smoke=Array.Exists(args,a=>a=="-combatSmokeTest" || a=="-interactionSmokeTest" || a=="-talulahSmokeTest" || a=="-talulahMotionEvidence" || a=="-monsterSelectionSmokeTest" || a=="-slugSmokeTest" || a=="-monsterCombatSmokeTest");
            TestSelectionOpen=!smoke;
            int rosterSmoke=Array.IndexOf(args,"-tacticalRosterSmokeTest");
            if(rosterSmoke>=0 && rosterSmoke+1<args.Length)
            {
                diagnostic=true;Application.runInBackground=true;TestSelectionOpen=false;
                int evidence=Array.IndexOf(args,"-demoEvidenceDir");
                StartCoroutine(VerifyTacticalRoster(args[rosterSmoke+1],evidence>=0 && evidence+1<args.Length ? args[evidence+1] : null));
            }
            int mapSmoke=Array.IndexOf(args,"-tacticalMapSmokeTest");
            if(mapSmoke>=0 && mapSmoke+1<args.Length)
            {
                diagnostic=true;Application.runInBackground=true;TestSelectionOpen=false;
                int evidence=Array.IndexOf(args,"-demoEvidenceDir");
                StartCoroutine(VerifyTacticalMap(args[mapSmoke+1],evidence>=0 && evidence+1<args.Length ? args[evidence+1] : null));
            }
            if(Array.IndexOf(args,"-defenderTrialSmokeTest")>=0)
            {
                diagnostic=true; Application.runInBackground=true; TestSelectionOpen=false;
                int evidence=Array.IndexOf(args,"-demoEvidenceDir");
                StartCoroutine(VerifyDefenderTrial(evidence>=0 && evidence+1<args.Length ? args[evidence+1] : null));
            }
            int monsterSmoke=Array.IndexOf(args,"-monsterCombatSmokeTest");
            if(monsterSmoke>=0 && monsterSmoke+1<args.Length)
            { diagnostic=true; Application.runInBackground=true; int evidence=Array.IndexOf(args,"-demoEvidenceDir");
              StartCoroutine(VerifyMonsterCombat(args[monsterSmoke+1],evidence>=0 && evidence+1<args.Length ? args[evidence+1] : null)); }
            if(Array.IndexOf(args,"-slugSmokeTest")>=0)
            { diagnostic=true; Application.runInBackground=true; int evidence=Array.IndexOf(args,"-demoEvidenceDir");
              StartCoroutine(VerifySlug(evidence>=0 && evidence+1<args.Length ? args[evidence+1] : null)); }
            if(Array.IndexOf(args,"-monsterSelectionSmokeTest")>=0)
            { diagnostic=true; Application.runInBackground=true; int evidence=Array.IndexOf(args,"-demoEvidenceDir");
              StartCoroutine(VerifyMonsterSelection(evidence>=0 && evidence+1<args.Length ? args[evidence+1] : null)); }
            if(Array.IndexOf(args,"-talulahMotionEvidence")>=0)
            { diagnostic=true; Application.runInBackground=true; int evidence=Array.IndexOf(args,"-demoEvidenceDir");
              StartCoroutine(VerifyTalulahMotion(evidence>=0 && evidence+1<args.Length ? args[evidence+1] : null)); }
            if(Array.IndexOf(args,"-talulahSmokeTest")>=0)
            { diagnostic=true; Application.runInBackground=true;
              int evidence=Array.IndexOf(args,"-demoEvidenceDir");
              StartCoroutine(VerifyTalulah(evidence>=0 && evidence+1<args.Length ? args[evidence+1] : null)); }
            if(Array.IndexOf(args,"-combatSmokeTest") >= 0)
            {
                Application.runInBackground = true;
                int index=Array.IndexOf(args,"-demoEvidenceDir");
                StartCoroutine(VerifyEngagement(index>=0 && index+1<args.Length ? args[index+1] : null));
            }
            if(Array.IndexOf(args,"-interactionSmokeTest") >= 0)
            {
                Application.runInBackground=true;
                int index=Array.IndexOf(args,"-demoEvidenceDir");
                StartCoroutine(VerifyInteraction(index>=0 && index+1<args.Length ? args[index+1] : null));
            }
        }

        private ManualBattle CreateBattle()
        {
            var result=new ManualBattle(definition.Snapshot(),
            defenderTrial!=null ? defenderTrial.route : scene.Waypoints,
            definition.Engagement.DefenderPosition,definition.Engagement.Snapshot(),definition.Engagement.ExtraDefenders,
            defenderTrial!=null ? defenderTrial.Snapshot() : null,
            defenderTrial!=null ? defenderTrial.LaneSnapshot() : null);
            if(TacticalMapActive && defenderTrial.rosterCards?.Length>0)
            {
                var profiles=new System.Collections.Generic.List<BasicRosterProfile>();
                foreach(var data in defenderTrial.rosterCards)profiles.Add(new BasicRosterProfile(data.Snapshot(),data.Engagement.Snapshot()));
                result.ConfigureCardLoadout(profiles);
            }
            return result;
        }

        private void EnableCombatView()
        {
            if(combatView==null) combatView=new GameObject("Kroos engagement presentation").AddComponent<EngagementView>();
            combatView.Initialize(this,definition.Engagement,uiSound,hitSound);
        }

        public void ValidateConfiguration()
        {
            if(scene==null || definition==null || !IsRegisteredTest(definition) || definition.Engagement==null || !definition.Card.HasFrameAnimation || scene.Monster==null)
                throw new InvalidOperationException("Current battle scene/configuration is incomplete.");
            var checkedBattle = CreateBattle();
            if(checkedBattle.CombatSettings.HasSkill && (definition.Card.SkillFrames?.Count ?? 0)==0)
                throw new InvalidOperationException("A skill-enabled card requires skill animation frames.");
            if(talulahDefinition==null || !talulahDefinition.Card.HasFrameAnimation || !talulahDefinition.Engagement.FireRain || talulahDefinition.Engagement.ExtraDefenders.Count!=2)
                throw new InvalidOperationException("Talulah trial scene binding/configuration is incomplete.");
            _ = talulahDefinition.Snapshot(); _ = talulahDefinition.Engagement.Snapshot();
        }

        public void SelectCard(bool selected)
        {
            if(TestSelectionOpen) return;
            Selected=selected;
            Feedback=selected ? "已选中：点击红门部署；右键取消" : "点击底部角色卡，再点击红门部署";
        }

        public void StartBattle() { if(TestSelectionOpen) return; Battle.Begin(); Feedback=$"拖动底部卡牌至红门，部署{definition.Card.DisplayName}"; }

        public DeploymentReceipt TryDeployAt(Vector3 point, bool allowWhilePaused = false)
        {
            if(TestSelectionOpen || !Selected) { Feedback="请先选择怪物，再拖动底部角色卡"; return new DeploymentReceipt(DeployFailure.NotStarted); }
            // A valid first deployment starts the simulation. A wrong click does not start the battle.
            if(Battle.Phase==BattlePhase.Ready && Battle.IsEntry(point)) Battle.Begin();
            var result=Battle.HasCardLoadout ? Battle.TryDeployRole(Battle.RunId,++requestSequence,point,DeploymentDefinition.Card.CardId,allowWhilePaused) : Battle.TryDeploy(Battle.RunId,++requestSequence,point,allowWhilePaused);
            if(result.Success) Selected=false;
            Feedback=result.Success ? $"部署成功  ·  战斗费用 -{DeploymentDefinition.Snapshot().DeploymentCost}；再次部署请重新选牌" : Explain(result.Failure);
            SyncViews();
            return result;
        }

        public void RestartBattle()
        {
            terminalPresentationElapsed=0;
            notice=null;
            selectedRosterIndex=0;
            settlementPointerDown=false;
            selectionReturnPointerDown=false;
            ClearViews(); Battle.Restart(); requestSequence=0; Selected=false;
            selectedEntityId=null; SimulationSpeed=1; CancelInteraction();
            Feedback="拖动底部角色卡至红门部署";
        }

        private void Update()
        {
            if(Battle==null || TestSelectionOpen) return;
            if(Input.GetKeyDown(KeyCode.Space)) Battle.SetPaused(!Battle.IsPaused);
            if(Dragging) ProcessPointerMove(Input.mousePosition);
            if(Input.GetKeyDown(KeyCode.E) && SkillMenuOpen) CastFocusedSkill();
            if(Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) CancelInteraction();
            Tick(Time.deltaTime);
        }

        public void Tick(float delta)
        {
            if(TestSelectionOpen) return;
            if(Battle.Phase==BattlePhase.Lost || Battle.Phase==BattlePhase.Won)
                terminalPresentationElapsed=Math.Min(DeathTail+.1,terminalPresentationElapsed+delta*EffectiveSimulationSpeed);
            Battle.Advance(delta*EffectiveSimulationSpeed);
            SyncViews();
            UpdatePreview();
        }

        private static bool PointerOnBattlefield()
        {
            float y=(Screen.height-Input.mousePosition.y)*720f/Screen.height;
            return y>=66 && y<552;
        }

        private bool TryPointerGround(out Vector3 point)
        {
            Ray ray=Camera.main.ScreenPointToRay(Input.mousePosition);
            var ground=new Plane(Vector3.up,Battle.Entry);
            if(ground.Raycast(ray,out float distance)) { point=ray.GetPoint(distance); return true; }
            point=Battle.Entry; return false;
        }

        private void SyncViews()
        {
            if(Battle==null) return;
            removed.Clear();
            foreach(var pair in views)
            {
                bool alive=false;
                foreach(var unit in Battle.Units) if(unit.EntityId==pair.Key && (unit.Active || DeathTail>0 && unit.Resolution==UnitResolution.Dead && PresentationClock<unit.DeathTime+DeathTail)) { alive=true; break; }
                if(!alive) { if(pair.Value!=null) { pair.Value.gameObject.SetActive(false); Destroy(pair.Value.gameObject); } removed.Add(pair.Key); }
            }
            foreach(string id in removed) views.Remove(id);
            foreach(var unit in Battle.Units)
            {
                if(!unit.Active) continue;
                if(!views.TryGetValue(unit.EntityId,out Transform view))
                {
                    var clone=Instantiate(scene.Monster.gameObject);
                    clone.name=unit.EntityId;
                    view=clone.transform;
                    var visual=clone.GetComponentInChildren<DemoSpriteVisual>(true);
                    visual.WorldUpright=TacticalMapActive;
                    visual.Bind(Battle,unit,CardForUnit(unit),()=>PresentationClock);
                    views.Add(unit.EntityId,view);
                    clone.SetActive(true);
                }
                view.position=unit.Motion.Position;
            }
            if(Battle.Phase==BattlePhase.Won || Battle.Phase==BattlePhase.Lost) CancelInteraction();
            var selectedUnit=Battle.FindUnit(selectedEntityId);
            if(SkillMenuOpen && (selectedUnit==null || !selectedUnit.Active)) CloseSkillMenu();
            if(Battle.EngagementEnabled && (selectedUnit==null || !selectedUnit.Active))
            { selectedEntityId=null; foreach(var unit in Battle.Units) if(unit.Active) { selectedEntityId=unit.EntityId; break; } }
        }

        public void SelectSkillUnit(string id) { if(Battle.FindUnit(id)?.Active==true) selectedEntityId=id; }
        public SkillFailure ActivateSelectedSkill()
        {
            if(TestSelectionOpen) return SkillFailure.NotRunning;
            SkillFailure result=Battle.TryActivateSkill(Battle.RunId,++requestSequence,selectedEntityId);
            Feedback=result==SkillFailure.None ? (Battle.CombatSettings.FireRain ? "全屏火雨已开启：六波火焰覆盖全部守军" : "爆裂黎明已开启：强化炮击弹药装填完成") :
                result==SkillFailure.Paused ? "暂停中：恢复后再确认释放技能" :
                result==SkillFailure.NotReady ? "技力尚未回满" : result==SkillFailure.AlreadyActive ? "技能正在生效，剩余弹药尚未用完" : $"请先选择场上的{definition.Card.DisplayName}";
            return result;
        }
        public void ToggleSpeed() => SimulationSpeed=SimulationSpeed==1 ? 2 : 1;
        private void OnApplicationFocus(bool focus)
        { if(!focus && !diagnostic && Battle!=null) { Battle.SetPaused(true); CancelInteraction(); } }

        private void CreatePreview()
        {
            previewMaterial=new Material(Shader.Find("Sprites/Default"));
            preview=new GameObject("Deployment preview",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            preview.sprite=OriginalTalulahArt.BattleSprite(definition.Card); preview.sharedMaterial=previewMaterial;
            entryOutline=new GameObject("Red gate deployment area",typeof(LineRenderer)).GetComponent<LineRenderer>();
            entryOutline.sharedMaterial=previewMaterial; entryOutline.widthMultiplier=0.025f;
            entryOutline.positionCount=5; entryOutline.useWorldSpace=true;
            Vector3 p=Battle.Entry+Vector3.up*0.015f;
            entryOutline.SetPositions(new[]{p+new Vector3(-0.6f,0,-0.6f),p+new Vector3(-0.6f,0,0.6f),p+new Vector3(0.6f,0,0.6f),p+new Vector3(0.6f,0,-0.6f),p+new Vector3(-0.6f,0,-0.6f)});
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if(preview==null) return;
            var map=defenderTrialMap!=null ? defenderTrialMap.GetComponent<TacticalMapEnvironment>() : null;
            if(map!=null) map.ShowLane(null);
            preview.enabled=entryOutline.enabled=Selected;
            if(!Selected) return;
            Vector3 point=Battle.Entry;
            if(Dragging)
            {
                if(!IsBattlefield(pointerScreen) || !PointerGround(pointerScreen,out point)) { preview.enabled=false; return; }
            }
            else if(PointerOnBattlefield()) TryPointerGround(out point);
            var lane=Battle.FindEntryLane(point);
            if(map!=null && Dragging) map.ShowLane(lane);
            if(lane!=null) point=lane.Entry;
            Vector3 p=(lane!=null ? lane.Entry : point)+Vector3.up*.015f;
            entryOutline.SetPositions(new[]{p+new Vector3(-.6f,0,-.6f),p+new Vector3(-.6f,0,.6f),p+new Vector3(.6f,0,.6f),p+new Vector3(.6f,0,-.6f),p+new Vector3(-.6f,0,-.6f)});
            preview.sprite=OriginalTalulahArt.BattleSprite(DeploymentDefinition.Card);
            DeployFailure failure=Battle.CheckCardDeployment(DeploymentDefinition.Card.CardId,point,Dragging);
            bool valid=failure==DeployFailure.None || failure==DeployFailure.NotStarted;
            Color tint=valid ? new Color(0.4f,1,0.7f) : new Color(1,0.35f,0.3f);
            preview.transform.position=point; preview.transform.rotation=Camera.main.transform.rotation;
            preview.sortingOrder=DemoSpriteVisual.GetSortingOrder(Camera.main,point)+24;
            preview.color=new Color(tint.r,tint.g,tint.b,0.30f);
            entryOutline.startColor=entryOutline.endColor=tint;
            foreach(var unit in Battle.Units)
                if(unit.Active && Vector3.Distance(unit.Motion.Position,point)<ManualBattle.EntryClearance) preview.enabled=false;
        }

        private void OnGUI()
        {
            if(Battle==null) return;
            if(TestSelectionOpen)
            {
                EnsureStyles(); Matrix4x4 previousMatrix=GUI.matrix; Color previousBackground=GUI.backgroundColor;
                GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
                GUI.backgroundColor=new Color(.55f,.81f,.89f); DrawMonsterTestSelection();
                GUI.matrix=previousMatrix; GUI.backgroundColor=previousBackground; return;
            }
            // IMGUI keeps queued pointer edges, even when a quick down/up occurs between simulation frames.
            var pointerEvent=Event.current;
            if((Battle.EngagementEnabled || SettlementVisible) && ProcessPointerEvent(pointerEvent.type,pointerEvent.mousePosition,pointerEvent.button)) pointerEvent.Use();
            // Space is owned by Update; do not also activate the last focused GUI button.
            if(Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Space) Event.current.Use();
            EnsureStyles();
            Matrix4x4 old=GUI.matrix;
            Color oldBackground=GUI.backgroundColor;
            GUI.backgroundColor=new Color(0.55f,0.81f,0.89f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            if(SettlementVisible)
            {
                DrawSettlement();
                GUI.backgroundColor=oldBackground; GUI.matrix=old; return;
            }
            Fill(new Rect(0,0,1280,66),new Color(0.035f,0.05f,0.065f,0.94f));
            GUI.Label(new Rect(26,17,310,32),TacticalMapActive ? defenderTrial.mapTitle : "昨日圆车  /  工业防线",textStyle);
            GUI.Label(new Rect(346,8,180,26),"战斗费用",smallStyle);
            GUI.Label(new Rect(347,24,180,40),Battle.DP.ToString(),numberStyle);
            Fill(new Rect(531,0,218,66),new Color(0.08f,0.13f,0.17f));
            Fill(new Rect(549,17,25,23),new Color(0.42f,0.78f,0.91f));
            Fill(new Rect(553,40,17,5),new Color(0.42f,0.78f,0.91f));
            Fill(new Rect(558,45,7,4),new Color(0.42f,0.78f,0.91f));
            Fill(new Rect(554,21,15,14),new Color(0.08f,0.13f,0.17f));
            GUI.Label(new Rect(589,4,135,23),"守方生命",smallStyle);
            GUI.Label(new Rect(590,25,150,36),$"{Battle.Defense} / {Battle.Settings.InitialDefense}",numberStyle);
            GUI.Label(new Rect(770,20,150,28),$"场上 {Battle.ActiveCount}  ·  穿门 {Battle.Breaches}",smallStyle);
            if(Battle.EngagementEnabled)
            {
                if(GUI.Button(new Rect(930,14,64,38),Muted ? "静音" : "音效",buttonStyle)) Muted=!Muted;
                if(GUI.Button(new Rect(1002,14,76,38),(SkillMenuOpen ? "0.03x" : SimulationSpeed+"x"),buttonStyle)) ToggleSpeed();
                DrawDefenderHUD();
            }
            bool controls=Battle.Phase==BattlePhase.Running;
            GUI.enabled=controls;
            if(GUI.Button(new Rect(1090,14,160,38),Battle.IsPaused ? "继续  [空格]" : "暂停  [空格]",buttonStyle)) Battle.SetPaused(!Battle.IsPaused);
            GUI.enabled=true;
            foreach(var unit in Battle.Units)
            {
                if(!unit.Active) continue;
                Vector3 screen=Camera.main.WorldToScreenPoint(unit.Motion.Position+Camera.main.transform.up*UnitVisualHeight(unit));
                float x=screen.x*1280f/Screen.width,y=720-screen.y*720f/Screen.height;
                Fill(new Rect(x-31,y,62,7),new Color(0.03f,0.05f,0.06f));
                Fill(new Rect(x-29,y+2,58*unit.Health/unit.MaxHealth,3),new Color(0.45f,0.91f,0.61f));
                if(unit.ArtsBarrierRemaining>0)
                {
                    Fill(new Rect(x-31,y+10,62,5),new Color(.03f,.05f,.1f));
                    Fill(new Rect(x-29,y+11,58*unit.ArtsBarrierRemaining/(unit.ArtsBarrierLayerSize*3),3),new Color(.3f,.7f,1));
                    GUI.Label(new Rect(x-70,y-28,140,26),$"屏障×{unit.ArtsBarrierLayers}",smallStyle);
                }
                if(unit.IsReviving) GUI.Label(new Rect(x-70,y-28,140,26),$"复起 {Math.Max(0,unit.ReviveAt-Battle.Clock):0.0}s",smallStyle);
                else if(Battle.CombatSettings.Effects.FrostNova && unit.ReviveUsed) GUI.Label(new Rect(x-70,y-28,140,26),"第二形态",smallStyle);
                else if(Battle.Clock<unit.LastHealTime+.2) GUI.Label(new Rect(x-70,y-28,140,26),"生命恢复",smallStyle);
                if(Battle.CombatSettings.Effects.Mudrock) GUI.Label(new Rect(x-70,y+18,140,26),$"蓄力 {Math.Min(6,unit.OrdinaryAttacksFired)}/6",smallStyle);
                if(unit.Shackle!=null&&!unit.Shackle.Ended)GUI.Label(new Rect(x-70,y-28,180,26),$"枷锁 {20-(Battle.Clock-unit.Shackle.StartedAt):0.0}s",smallStyle);
                if(Battle.FirstSpeakerCasting(unit))GUI.Label(new Rect(x-70,y-28,220,26),$"侵蚀液泡 · 无敌 {5-(Battle.Clock-unit.BubbleStartedAt):0.0}s",smallStyle);
                if(Battle.CombatSettings.Effects.Harvester)GUI.Label(new Rect(x-70,y-28,200,26),unit.HarvesterPhase==HarvesterPhase.Dormant?"待命":unit.HarvesterPhase==HarvesterPhase.Waking?"苏醒中":$"临战 ×6 · 失血{unit.HarvesterHealthLost:0}",smallStyle);
                if(Battle.CombatSettings.Effects.Mandragora)GUI.Label(new Rect(x-70,y-28,200,26),unit.MandraPhase==MandragoraPhase.Reborn?"重生中":$"{(unit.MandraPhase==MandragoraPhase.Second?"第二阶段":"第一阶段")} · {(unit.StoneShield?"石盾":"破盾")}",smallStyle);
                if(Battle.CombatSettings.Effects.Flamerazer&&unit.Spray!=null&&!unit.Spray.Ended)GUI.Label(new Rect(x-70,y-28,160,26),$"喷火 {unit.Spray.Ticks}跳",smallStyle);
                if(Battle.CombatSettings.Effects.Phalanx) GUI.Label(new Rect(x-70,y-28,160,26),$"方阵 +{Battle.UnitArmor(unit)-Battle.CombatSettings.UnitArmor:0}甲",smallStyle);
                if(Battle.CombatSettings.Effects.Flamechaser&&unit.FlamePhase!=FlamechaserPhase.Soldier)
                    GUI.Label(new Rect(x-70,y-28,180,26),unit.FlamePhase==FlamechaserPhase.Ember?$"余烬 {unit.Health:0}/5 · {(Battle.UnitInvisible(unit)?"隐匿":"显形")}":unit.FlamePhase==FlamechaserPhase.Transforming?"余烬转化":"重生中",smallStyle);
                if(Battle.EngagementEnabled && Battle.UnitCombat(unit).HasSkill) DrawSkillResourceBar(x,y+9,unit);
            }
            Fill(new Rect(0,552,1280,168),new Color(0.035f,0.05f,0.065f,0.97f));
            Fill(new Rect(0,552,1280,2),new Color(0.28f,0.37f,0.43f));
            if(notice!=null && Time.unscaledTime<noticeUntil) DrawNotice(new Rect(330,82,620,38),notice);
            Rect restartButton=Battle.HasCardLoadout ? new Rect(292,579,170,42) : new Rect(Battle.EngagementEnabled ? 860 : 474,649,170,44);
            if(GUI.Button(restartButton,Battle.Phase==BattlePhase.Ready ? "开始作战" : "重新开始",buttonStyle))
            { if(Battle.Phase==BattlePhase.Ready) StartBattle(); else RestartBattle(); }
            DrawTrialSelector();
            if(Battle.HasCardLoadout) DrawTacticalRoster();
            else if(cardView!=null && cardView.DrawRoster(Battle,Selected,!Battle.EngagementEnabled)) SelectCard(!Selected);
            if(Battle.EngagementEnabled) DrawInteractionOverlay();
            GUI.backgroundColor=oldBackground;
            GUI.matrix=old;
        }

        private void DrawSkillResourceBar(float x,float y,DeployedUnitState unit)
        {
            Fill(new Rect(x-31,y,62,7),new Color(0.03f,0.05f,0.06f));
            if(Battle.CombatSettings.FireRain && Battle.IsSkillActionPlaying(unit))
            {
                float remaining=1-Mathf.Clamp01((float)((Battle.Clock-unit.FireRainStartedAt)/Battle.CombatSettings.SkillInterval));
                Fill(new Rect(x-29,y+2,58*remaining,3),new Color(1,.38f,.12f));
            }
            else if(unit.SkillAmmo>0)
            {
                int capacity=Battle.CombatSettings.SkillAmmo;
                float width=(58-2*(capacity-1))/(float)capacity;
                for(int i=0;i<capacity;i++)
                    Fill(new Rect(x-29+i*(width+2),y+2,width,3),i<unit.SkillAmmo ?
                        new Color(.98f,.77f,.22f) : new Color(.20f,.24f,.27f));
            }
            else
            {
                float charge=Mathf.Clamp01(unit.SkillSP/Battle.CombatSettings.RequiredSP);
                Fill(new Rect(x-29,y+2,58*charge,3),new Color(.20f,.61f,1));
            }
        }

        private void DrawSettlement()
        {
            bool won=Battle.Phase==BattlePhase.Won;
            Color accent=won ? new Color(.44f,.88f,.74f) : new Color(.98f,.43f,.35f);
            Fill(new Rect(0,0,1280,720),new Color(.015f,.025f,.035f,.80f));
            Fill(new Rect(330,174,620,424),new Color(.045f,.075f,.095f,.99f));
            Fill(new Rect(330,174,620,4),accent);
            GUI.Label(new Rect(360,195,560,26),"作战结束",noticeStyle);
            Color old=GUI.color; GUI.color=accent;
            GUI.Label(new Rect(360,233,560,58),won ? "突破成功" : "进攻失败",resultStyle);
            GUI.color=old;
            GUI.Label(new Rect(350,299,580,32),won ? $"{VictoryUnitName}已穿过蓝门" : "进攻单位已全部倒下，本场部署机会已用尽",noticeStyle);
            string[] labels={"守方剩余生命","本场部署","技能释放"};
            string[] values={Battle.Defense+" / "+Battle.Settings.InitialDefense,Battle.Units.Count.ToString(),Battle.SkillCasts.ToString()};
            for(int i=0;i<3;i++)
            {
                float x=365+i*184;
                GUI.Label(new Rect(x,351,182,26),labels[i],noticeStyle);
                GUI.Label(new Rect(x,382,182,42),values[i],resultStyle);
            }
            if(GUI.Button(SettlementRestartRect,"重新开始",buttonStyle)) RestartBattle();
            if(GUI.Button(SettlementReturnRect,"返回怪物选择",buttonStyle)) ReturnToMonsterTests();
            GUI.Label(new Rect(360,514,560,25),"重置本场费用、角色、生命与技能状态",smallStyle);
        }

        private void DrawDefenderHUD()
        {
            foreach(var d in Battle.Defenders)
            {
            Vector3 screen=Camera.main.WorldToScreenPoint(d.Position+Camera.main.transform.up*(TacticalMapActive ? 1.6f : defenderTrial!=null ? 1.32f : 1.95f));
            float x=screen.x*1280f/Screen.width,y=720-screen.y*720f/Screen.height;
            if(!d.Alive) continue;
            float width=TacticalMapActive ? 62 : 96;
            Fill(new Rect(x-width/2,y,width,7),new Color(0.03f,0.05f,0.06f));
            Fill(new Rect(x-width/2+2,y+2,(width-4)*d.Health/d.MaxHealth,3),new Color(0.96f,0.66f,0.3f));
            string status=Battle.DefenderShackled(d)?"枷锁禁锢":d.NeuralUntil>Battle.Clock ? $"神经爆发 {Math.Max(0,d.NeuralUntil-Battle.Clock):0.0}s" : d.NeuralAccumulated>0 ? $"神经损伤 {d.NeuralAccumulated:0}/100" : d.FrozenUntil>Battle.Clock ? "冻结" : d.ColdUntil>Battle.Clock ? "寒冷" : d.FrostSlowUntil>Battle.Clock ? "冰环减速" :
                !DefenderTrialActive && d.Settings.DamageType==CombatDamageType.Arts ? "法术测试靶" : "";
            if(d.ArmorReduction>0) status+=(status.Length>0 ? " / " : "")+"护甲 -"+d.ArmorReduction.ToString("0");
            if(defenderTrial!=null && !TacticalMapActive) GUI.Label(new Rect(x-60,y-24,120,24),d.Settings.Name,smallStyle);
            if(status.Length>0) GUI.Label(new Rect(x-90,y+10,180,26),status,smallStyle);
            }
        }

        private void EnsureStyles()
        {
            if(textStyle!=null) return;
            textStyle=new GUIStyle(GUI.skin.label){font=scene.InterfaceFont,fontSize=20};
            smallStyle=new GUIStyle(textStyle){fontSize=16};
            noticeStyle=new GUIStyle(textStyle){alignment=TextAnchor.MiddleCenter};
            numberStyle=new GUIStyle(textStyle){fontSize=27,fontStyle=FontStyle.Bold};
            resultStyle=new GUIStyle(numberStyle){fontSize=36,alignment=TextAnchor.MiddleCenter};
            buttonStyle=new GUIStyle(GUI.skin.button){font=scene.InterfaceFont,fontSize=18,fontStyle=FontStyle.Bold};
            foreach(var state in new[]{buttonStyle.normal,buttonStyle.hover,buttonStyle.active,buttonStyle.focused})
            { state.background=Texture2D.whiteTexture; state.textColor=new Color(0.02f,0.06f,0.08f); }
        }

        private static void Fill(Rect rect,Color color)
        { Color previous=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=previous; }

        private void ShowNotice(string message) { notice=message; noticeUntil=Time.unscaledTime+2.5f; }
        private void DrawNotice(Rect rect,string message)
        { Fill(rect,new Color(0.035f,0.05f,0.065f,0.94f)); GUI.Label(rect,message,noticeStyle); }

        private static string Explain(DeployFailure reason)
        {
            switch(reason)
            {
                case DeployFailure.WrongEntry: return "无法部署：只有红门是合法入口";
                case DeployFailure.NoDP: return "费用不足：等待场上单位继续进攻";
                case DeployFailure.NoUses: return "本场部署次数已用尽";
                case DeployFailure.CoolingDown: return "部署冷却中，请稍候";
                case DeployFailure.EntryBusy: return "入口暂时被占用，请稍候";
                case DeployFailure.Paused: return "暂停中：恢复后点击红门部署";
                case DeployFailure.Finished: return "本场已结束，请重新挑战";
                default: return "请先开始作战";
            }
        }

        private void ClearViews()
        {
            terminalPresentationElapsed=0;
            foreach(var view in views.Values) if(view!=null) { view.gameObject.SetActive(false); Destroy(view.gameObject); }
            views.Clear();
        }

        private void OnDisable()
        {
            ClearViews(); CancelInteraction();
            if(preview!=null) preview.enabled=false;
            if(entryOutline!=null) entryOutline.enabled=false;
        }

        private void OnDestroy()
        {
            ClearViews();
            if(preview!=null) Destroy(preview.gameObject);
            if(entryOutline!=null) Destroy(entryOutline.gameObject);
            if(previewMaterial!=null) Destroy(previewMaterial);
        }

        private IEnumerator VerifyEngagement(string directory)
        {
            Time.timeScale=0;
            if(directory!=null) { Directory.CreateDirectory(directory); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory,"combat-ready.png")); yield return null; }
            SelectCard(true);
            int dp=Battle.DP;
            bool invalid=!TryDeployAt(scene.BlueGate.position).Success && Battle.DP==dp;
            TryDeployAt(Battle.Entry); Tick(0.5f);
            bool initial=ActivateSelectedSkill()==SkillFailure.NotReady && Battle.DP==dp-Battle.Settings.DeploymentCost;
            Battle.SetPaused(true);
            double clock=Battle.Clock; float sp=Battle.Units[0].SkillSP;
            Tick(5);
            bool paused=Battle.Clock==clock && Battle.Units[0].SkillSP==sp && ActivateSelectedSkill()==SkillFailure.Paused;
            RestartBattle(); SelectCard(true); TryDeployAt(Battle.Entry);
            bool stopped=false, manual=false, captured=false;
            bool selectionReset=!Selected && TryDeployAt(Battle.Entry).Failure==DeployFailure.NotStarted;
            bool ranged=false, noBlock=Battle.CombatSettings.BlockCount==0;
            int deployed=1;
            for(int i=0;i<2000 && Battle.Phase==BattlePhase.Running;i++)
            {
                Tick(0.05f);
                if(deployed<Battle.Settings.DeploymentLimit && Battle.Clock+0.00001>=deployed*2)
                { SelectCard(true); if(TryDeployAt(Battle.Entry).Success) deployed++; }
                foreach(var unit in Battle.Units)
                {
                    stopped |= unit.Active && unit.HasCombatTarget && !unit.IsMoving;
                    noBlock &= !unit.IsBlocked;
                    Vector3 gap=unit.Motion.Position-Battle.Defender.Position; gap.y=0;
                    ranged |= unit.Active && unit.HasCombatTarget && gap.magnitude>3;
                    if(Battle.CheckSkill(unit.EntityId)==SkillFailure.None)
                    { SelectSkillUnit(unit.EntityId); manual |= ActivateSelectedSkill()==SkillFailure.None; }
                }
                yield return null;
                if(directory!=null && !captured && Battle.CombatShots.Count>=3)
                { captured=true; yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory,"combat-fighting.png")); yield return null; }
            }
            bool killed=!Battle.Defender.Alive;
            bool won=Battle.Phase==BattlePhase.Won && Battle.Defense==0;
            int breaches=Battle.Breaches; Tick(20);
            bool once=Battle.Breaches==breaches;
            if(directory!=null) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory,"combat-won.png")); yield return null; }
            RestartBattle();
            bool reset=Battle.Defender.Alive && Battle.Defender.Health==Battle.CombatSettings.DefenderHealth && Battle.SkillCasts==0 && Battle.CombatShots.Count==0 && Battle.Units.Count==0 && !Battle.IsPaused;
            SelectCard(true);
            for(int attempt=0;attempt<Battle.Settings.DeploymentLimit && Battle.Phase!=BattlePhase.Lost;attempt++)
            {
                SelectCard(true); TryDeployAt(Battle.Entry);
                for(int i=0;i<1000 && Battle.ActiveCount>0 && Battle.Phase==BattlePhase.Running;i++) { Tick(0.05f); yield return null; }
            }
            bool lost=Battle.Phase==BattlePhase.Lost && Battle.ActiveCount==0;
            if(directory!=null) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory,"combat-lost.png")); yield return null; }
            Time.timeScale=1;
            bool pass=invalid && initial && paused && stopped && ranged && noBlock && selectionReset && manual && killed && won && once && reset && lost;
            Debug.Log($"ENGAGEMENT_CHECK: invalid={invalid}; initialSP={initial}; paused={paused}; stopped={stopped}; ranged={ranged}; noBlock={noBlock}; selectionReset={selectionReset}; manual={manual}; killed={killed}; won={won}; gateOnce={once}; reset={reset}; lost={lost}.");
            Application.Quit(pass?0:1);
        }
    }
}
