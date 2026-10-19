using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Scene/input adapter for the single-card level. Simulation and transactions belong to ManualBattle.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class ManualBattleController : MonoBehaviour
    {
        [SerializeField] private InitialDemo scene;
        [SerializeField] private ManualBattleDefinition definition;
        [SerializeField] private AudioClip uiSound, hitSound;
        private EngagementView combatView;
        private string selectedEntityId;
        private bool diagnostic;
        private readonly Dictionary<string, Transform> views = new Dictionary<string, Transform>();
        private readonly List<string> removed = new List<string>();
        private DemoCardView cardView;
        private SpriteRenderer preview;
        private LineRenderer entryOutline;
        private Material previewMaterial;
        private GUIStyle textStyle, smallStyle, numberStyle, buttonStyle;
        private long requestSequence;
        public ManualBattle Battle { get; private set; }
        public ManualBattleDefinition Definition => definition;
        public bool Selected { get; private set; }
        public string Feedback { get; private set; } = "点击底部角色卡，再点击红门部署";
        public int ViewCount => views.Count;
        public float SimulationSpeed { get; private set; } = 1;
        public bool Muted { get; private set; }
        public string SelectedEntityId => selectedEntityId;
        public EngagementView CombatView => combatView;

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            diagnostic = Application.isBatchMode || Array.IndexOf(args,"-deploymentSmokeTest") >= 0 || Array.IndexOf(args,"-combatSmokeTest") >= 0;
            if (Array.IndexOf(args,"-demoSmokeTest") >= 0) { UseLegacyPreview(); return; }
            ValidateConfiguration();
            scene.enabled = false;
            scene.Monster.gameObject.SetActive(false);
            cardView = FindAnyObjectByType<DemoCardView>();
            bool combat = definition.Engagement != null && Array.IndexOf(args,"-deploymentSmokeTest") < 0;
            Battle = CreateBattle(combat);
            CreatePreview();
            if (combat) EnableCombatView();
            if(Array.IndexOf(args,"-deploymentSmokeTest") >= 0)
            {
                Application.runInBackground = true;
                int index=Array.IndexOf(args,"-demoEvidenceDir");
                string directory=index>=0 && index+1<args.Length ? args[index+1] : null;
                StartCoroutine(VerifyManualBattle(directory));
            }
            if(Array.IndexOf(args,"-combatSmokeTest") >= 0)
            {
                Application.runInBackground = true;
                int index=Array.IndexOf(args,"-demoEvidenceDir");
                StartCoroutine(VerifyEngagement(index>=0 && index+1<args.Length ? args[index+1] : null));
            }
        }

        private ManualBattle CreateBattle(bool combat) => combat
            ? new ManualBattle(definition.Snapshot(),scene.Waypoints,definition.Engagement.DefenderPosition,definition.Engagement.Snapshot())
            : new ManualBattle(definition.Snapshot(),scene.Waypoints,scene.Tower.position);

        private void EnableCombatView()
        {
            scene.Tower.gameObject.SetActive(false);
            var range=GameObject.Find("Tower Range"); if(range!=null) range.SetActive(false);
            combatView=new GameObject("Kroos engagement presentation").AddComponent<EngagementView>();
            combatView.Initialize(this,definition.Engagement,uiSound,hitSound);
        }

        public void UseDeploymentDiagnostic()
        {
            diagnostic=true; ClearViews();
            if(combatView!=null) { combatView.gameObject.SetActive(false); Destroy(combatView.gameObject); }
            scene.Tower.gameObject.SetActive(true);
            Battle=CreateBattle(false); selectedEntityId=null; Selected=false;
        }

        public void ValidateConfiguration()
        {
            if(scene==null || definition==null || definition.Card!=scene.Card)
                throw new InvalidOperationException("Manual battle: scene, definition and equipped card must match.");
            _ = new ManualBattle(definition.Snapshot(),scene.Waypoints,scene.Tower.position);
        }

        // Historical animation/rendering tests explicitly opt into the old single-unit diagnostic.
        public void UseLegacyPreview()
        {
            enabled = false;
            if(combatView!=null) { combatView.gameObject.SetActive(false); Destroy(combatView.gameObject); }
            scene.Tower.gameObject.SetActive(true);
            scene.Monster.gameObject.SetActive(true);
            scene.enabled = true;
        }

        public void SelectCard(bool selected)
        {
            Selected=selected;
            Feedback=selected ? "已选中：点击红门部署；右键取消" : "点击底部角色卡，再点击红门部署";
        }

        public void StartBattle() { Battle.Begin(); Feedback="选择角色，分批进攻蓝门"; }

        public DeploymentReceipt TryDeployAt(Vector3 point)
        {
            if(!Selected) { Feedback="请先点击底部角色卡"; return new DeploymentReceipt(DeployFailure.NotStarted); }
            // A valid first deployment starts the simulation. A wrong click does not start the battle.
            if(Battle.Phase==BattlePhase.Ready && Battle.IsEntry(point)) Battle.Begin();
            var result=Battle.TryDeploy(Battle.RunId,++requestSequence,point);
            if(result.Success) Selected=false;
            Feedback=result.Success ? $"部署成功  ·  战斗费用 -{Battle.Settings.DeploymentCost}；再次部署请重新选牌" : Explain(result.Failure);
            SyncViews();
            return result;
        }

        public void RestartBattle()
        {
            ClearViews(); Battle.Restart(); requestSequence=0; Selected=false;
            selectedEntityId=null; SimulationSpeed=1;
            scene.ShotBeam.enabled=false; Feedback="点击底部角色卡，再点击红门部署";
        }

        private void Update()
        {
            if(Battle==null) return;
            if(Input.GetKeyDown(KeyCode.Space)) Battle.SetPaused(!Battle.IsPaused);
            if(Input.GetKeyDown(KeyCode.Alpha1)) SelectCard(true);
            if(Input.GetKeyDown(KeyCode.E)) ActivateSelectedSkill();
            if(Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) SelectCard(false);
            if(Input.GetMouseButtonDown(0) && PointerOnBattlefield() && TryPointerGround(out Vector3 point)) TryDeployAt(point);
            Tick(Time.deltaTime);
        }

        public void Tick(float delta)
        {
            Battle.Advance(delta*SimulationSpeed);
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
                foreach(var unit in Battle.Units) if(unit.EntityId==pair.Key && unit.Active) { alive=true; break; }
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
                    // A template used by a diagnostic may own generated cutouts; each new view owns its own rig.
                    foreach(Transform child in visual.transform)
                    { child.gameObject.SetActive(false); Destroy(child.gameObject); }
                    visual.Bind(Battle,unit,Battle.EngagementEnabled ? definition.Engagement.WisadelAttackSprite : null);
                    views.Add(unit.EntityId,view);
                    clone.SetActive(true);
                }
                view.position=unit.Motion.Position;
            }
            bool beam=!Battle.EngagementEnabled && Battle.Phase==BattlePhase.Running && Battle.Clock<Battle.LastShotTime+0.18;
            scene.ShotBeam.enabled=beam;
            if(beam)
            {
                Vector3 offset=Battle.LastShotPosition-scene.Tower.position; offset.y=0;
                if(offset.sqrMagnitude>0.001f) scene.Muzzle.parent.rotation=Quaternion.LookRotation(offset);
                scene.ShotBeam.SetPosition(0,scene.Muzzle.position);
                scene.ShotBeam.SetPosition(1,Battle.LastShotPosition+Vector3.up*0.65f);
            }
            if(Battle.Phase==BattlePhase.Won || Battle.Phase==BattlePhase.Lost) Selected=false;
            var selectedUnit=Battle.FindUnit(selectedEntityId);
            if(Battle.EngagementEnabled && (selectedUnit==null || !selectedUnit.Active))
            { selectedEntityId=null; foreach(var unit in Battle.Units) if(unit.Active) { selectedEntityId=unit.EntityId; break; } }
        }

        public void SelectSkillUnit(string id) { if(Battle.FindUnit(id)?.Active==true) selectedEntityId=id; }
        public SkillFailure ActivateSelectedSkill()
        {
            SkillFailure result=Battle.TryActivateSkill(Battle.RunId,++requestSequence,selectedEntityId);
            Feedback=result==SkillFailure.None ? "爆裂黎明已开启：强化炮击弹药装填完成" :
                result==SkillFailure.Paused ? "暂停中：恢复后再确认释放技能" :
                result==SkillFailure.NotReady ? "技力尚未回满" : result==SkillFailure.AlreadyActive ? "技能正在生效，剩余弹药尚未用完" : "请先选择场上的维什戴尔";
            return result;
        }
        public void ToggleSpeed() => SimulationSpeed=SimulationSpeed==1 ? 2 : 1;
        private void OnApplicationFocus(bool focus)
        { if(!focus && !diagnostic && Battle!=null) Battle.SetPaused(true); }

        private void CreatePreview()
        {
            previewMaterial=new Material(Shader.Find("Sprites/Default"));
            preview=new GameObject("Deployment preview",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            preview.sprite=definition.Card.BattleSprite; preview.sharedMaterial=previewMaterial;
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
            preview.enabled=entryOutline.enabled=Selected;
            if(!Selected) return;
            Vector3 point=Battle.Entry;
            if(PointerOnBattlefield()) TryPointerGround(out point);
            DeployFailure failure=Battle.CheckDeployment(point);
            bool valid=failure==DeployFailure.None || failure==DeployFailure.NotStarted;
            Color tint=Battle.IsPaused ? new Color(1,0.75f,0.25f) : valid ? new Color(0.4f,1,0.7f) : new Color(1,0.35f,0.3f);
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
            // Space is owned by Update; do not also activate the last focused GUI button.
            if(Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Space) Event.current.Use();
            EnsureStyles();
            Matrix4x4 old=GUI.matrix;
            Color oldBackground=GUI.backgroundColor;
            GUI.backgroundColor=new Color(0.55f,0.81f,0.89f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            Fill(new Rect(0,0,1280,66),new Color(0.035f,0.05f,0.065f,0.94f));
            GUI.Label(new Rect(26,17,310,32),"昨日圆车  /  工业防线",textStyle);
            GUI.Label(new Rect(346,8,180,26),"战斗费用",smallStyle);
            GUI.Label(new Rect(347,24,180,40),Battle.DP.ToString(),numberStyle);
            Fill(new Rect(531,0,218,66),new Color(0.08f,0.13f,0.17f));
            Fill(new Rect(549,17,25,23),new Color(0.42f,0.78f,0.91f));
            Fill(new Rect(553,40,17,5),new Color(0.42f,0.78f,0.91f));
            Fill(new Rect(558,45,7,4),new Color(0.42f,0.78f,0.91f));
            Fill(new Rect(554,21,15,14),new Color(0.08f,0.13f,0.17f));
            GUI.Label(new Rect(589,4,135,23),"守方防御",smallStyle);
            GUI.Label(new Rect(590,25,150,36),$"{Battle.Defense} / {Battle.Settings.InitialDefense}",numberStyle);
            GUI.Label(new Rect(770,20,150,28),$"场上 {Battle.ActiveCount}  ·  穿门 {Battle.Breaches}",smallStyle);
            if(Battle.EngagementEnabled)
            {
                if(GUI.Button(new Rect(930,14,64,38),Muted ? "静音" : "音效",buttonStyle)) Muted=!Muted;
                if(GUI.Button(new Rect(1002,14,76,38),SimulationSpeed+"x",buttonStyle)) ToggleSpeed();
                DrawDefenderHUD();
            }
            bool controls=Battle.Phase==BattlePhase.Running;
            GUI.enabled=controls;
            if(GUI.Button(new Rect(1090,14,160,38),Battle.IsPaused ? "继续  [空格]" : "暂停  [空格]",buttonStyle)) Battle.SetPaused(!Battle.IsPaused);
            GUI.enabled=true;
            foreach(var unit in Battle.Units)
            {
                if(!unit.Active) continue;
                Vector3 screen=Camera.main.WorldToScreenPoint(unit.Motion.Position+Camera.main.transform.up*1.8f);
                float x=screen.x*1280f/Screen.width,y=720-screen.y*720f/Screen.height;
                Fill(new Rect(x-31,y,62,7),new Color(0.03f,0.05f,0.06f));
                Fill(new Rect(x-29,y+2,58*unit.Health/Battle.Settings.Health,3),new Color(0.45f,0.91f,0.61f));
            }
            Fill(new Rect(0,552,1280,168),new Color(0.035f,0.05f,0.065f,0.97f));
            Fill(new Rect(0,552,1280,2),new Color(0.28f,0.37f,0.43f));
            string status=Battle.Phase==BattlePhase.Won ? "进攻成功！守方防御归零" : Battle.Phase==BattlePhase.Lost ? "进攻失败：场上清空，已无可用部署资源" :
                Battle.IsPaused ? "战术暂停：可选牌和预览，恢复后点击红门部署" : Feedback;
            GUI.Label(new Rect(26,565,870,32),status,textStyle);
            if(!Battle.EngagementEnabled) GUI.Label(new Rect(26,608,800,27),$"部署消耗 {Battle.Settings.DeploymentCost}  ·  剩余次数 {Battle.RemainingUses}/{Battle.Settings.DeploymentLimit}  ·  费用不自然回复",smallStyle);
            string cooldown=Battle.CooldownRemaining>0 ? $"部署冷却 {Battle.CooldownRemaining:0.0}s" : "部署就绪";
            GUI.Label(new Rect(26,Battle.EngagementEnabled ? 589 : 651,Battle.EngagementEnabled ? 850 : 540,26),
                Battle.EngagementEnabled ? $"部署 {Battle.Settings.DeploymentCost}费  ·  剩余 {Battle.RemainingUses}/{Battle.Settings.DeploymentLimit}次  ·  {cooldown}  ·  每次先[1]选牌 / 右键取消" : cooldown+"  ·  每次先[1]选牌 / 右键取消",smallStyle);
            if(Battle.EngagementEnabled) DrawSkills();
            if(GUI.Button(new Rect(Battle.EngagementEnabled ? 860 : 474,649,170,44),Battle.Phase==BattlePhase.Ready ? "开始作战" : "重新开始",buttonStyle))
            { if(Battle.Phase==BattlePhase.Ready) StartBattle(); else RestartBattle(); }
            if(cardView!=null && cardView.DrawRoster(Battle,Selected)) SelectCard(!Selected);
            GUI.backgroundColor=oldBackground;
            GUI.matrix=old;
        }

        private void DrawDefenderHUD()
        {
            var d=Battle.Defender;
            Vector3 screen=Camera.main.WorldToScreenPoint(d.Position+Camera.main.transform.up*1.95f);
            float x=screen.x*1280f/Screen.width,y=720-screen.y*720f/Screen.height;
            if(!d.Alive) return;
            GUI.Label(new Rect(x-70,y-23,180,24),Battle.CombatSettings.BlockCount==0 ? "克洛丝 · 高台远程" : $"克洛丝  阻挡 {d.BlockedCount}/{Battle.CombatSettings.BlockCount}",smallStyle);
            Fill(new Rect(x-48,y,96,8),new Color(0.03f,0.05f,0.06f));
            Fill(new Rect(x-46,y+2,92*d.Health/Battle.CombatSettings.DefenderHealth,4),new Color(0.96f,0.66f,0.3f));
        }

        private void DrawSkills()
        {
            int index=0;
            foreach(var unit in Battle.Units)
            {
                if(!unit.Active)continue;
                string charge=unit.SkillAmmo>0 ? $"弹药 {unit.SkillAmmo}/6" : $"技力 {unit.SkillSP:0}/{Battle.CombatSettings.RequiredSP:0}";
                string label=(unit.EntityId==selectedEntityId ? "▶ " : "")+$"{index+1:00}  {charge}";
                if(GUI.Button(new Rect(26+index*190,616,181,32),label,buttonStyle)) SelectSkillUnit(unit.EntityId);
                index++;
            }
            var selected=Battle.FindUnit(selectedEntityId);
            string info=selected==null ? "部署后选择实体，技力满后按 E 释放" :
                selected.SkillAmmo>0 ? "爆裂黎明生效中；弹药用尽后重新回复技力" : "技力自动回复，与部署费用无关";
            GUI.Label(new Rect(26,661,540,27),info,smallStyle);
            GUI.enabled=Battle.CheckSkill(selectedEntityId)==SkillFailure.None;
            if(GUI.Button(new Rect(580,649,245,44),"爆裂黎明  [E]",buttonStyle)) ActivateSelectedSkill();
            GUI.enabled=true;
        }

        private void EnsureStyles()
        {
            if(textStyle!=null) return;
            textStyle=new GUIStyle(GUI.skin.label){font=scene.InterfaceFont,fontSize=20};
            smallStyle=new GUIStyle(textStyle){fontSize=16};
            numberStyle=new GUIStyle(textStyle){fontSize=27,fontStyle=FontStyle.Bold};
            buttonStyle=new GUIStyle(GUI.skin.button){font=scene.InterfaceFont,fontSize=18,fontStyle=FontStyle.Bold};
            foreach(var state in new[]{buttonStyle.normal,buttonStyle.hover,buttonStyle.active,buttonStyle.focused})
            { state.background=Texture2D.whiteTexture; state.textColor=new Color(0.02f,0.06f,0.08f); }
        }

        private static void Fill(Rect rect,Color color)
        { Color previous=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=previous; }

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
            foreach(var view in views.Values) if(view!=null) { view.gameObject.SetActive(false); Destroy(view.gameObject); }
            views.Clear();
        }

        private void OnDisable()
        {
            ClearViews();
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

        private IEnumerator VerifyManualBattle(string directory)
        {
            Time.timeScale=0;
            if(directory!=null) { Directory.CreateDirectory(directory); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory,"manual-ready.png")); yield return null; }
            SelectCard(true);
            int dp=Battle.DP,uses=Battle.RemainingUses;
            bool invalid=!TryDeployAt(scene.BlueGate.position).Success && Battle.DP==dp && Battle.RemainingUses==uses && Battle.Phase==BattlePhase.Ready;
            bool first=TryDeployAt(Battle.Entry).Success;
            SelectCard(true);
            bool cooldown=TryDeployAt(Battle.Entry).Failure==DeployFailure.CoolingDown && Battle.Units.Count==1;
            Battle.SetPaused(true); double clock=Battle.Clock; Vector3 position=Battle.Units[0].Motion.Position;
            Battle.Advance(3);
            bool paused=Battle.Clock==clock && Battle.Units[0].Motion.Position==position && TryDeployAt(Battle.Entry).Failure==DeployFailure.Paused;
            Battle.SetPaused(false);
            bool deployed=first;
            for(int i=1;i<Battle.Settings.DeploymentLimit;i++) { Battle.Advance(2); SelectCard(true); deployed &= TryDeployAt(Battle.Entry).Success; }
            bool waits=Battle.DP==0 && Battle.ActiveCount>0 && Battle.Phase==BattlePhase.Running;
            yield return null;
            if(directory!=null) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory,"manual-multi.png")); yield return null; }
            for(int i=0;i<160 && Battle.Phase==BattlePhase.Running;i++) { Battle.Advance(0.25f); SyncViews(); yield return null; }
            bool won=Battle.Phase==BattlePhase.Won && Battle.Defense==0;
            int breaches=Battle.Breaches; Battle.Advance(10);
            bool once=Battle.Breaches==breaches && Battle.Defense==0;
            if(directory!=null) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory,"manual-won.png")); yield return null; }
            int oldRun=Battle.RunId; RestartBattle();
            bool reset=Battle.RunId==oldRun+1 && Battle.DP==Battle.Settings.InitialDP && Battle.RemainingUses==Battle.Settings.DeploymentLimit && Battle.Units.Count==0 && views.Count==0 && !Selected;
            SelectCard(true);
            for(int attempt=0;attempt<Battle.Settings.DeploymentLimit && Battle.Phase!=BattlePhase.Lost;attempt++)
            {
                SelectCard(true); deployed &= TryDeployAt(Battle.Entry).Success;
                for(int tick=0;tick<100 && Battle.ActiveCount>0 && Battle.Phase==BattlePhase.Running;tick++)
                { Battle.Advance(0.5f); SyncViews(); yield return null; }
            }
            bool lost=Battle.Phase==BattlePhase.Lost && Battle.ActiveCount==0 && Battle.Defense==Battle.Settings.InitialDefense;
            if(directory!=null) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory,"manual-lost.png")); yield return null; }
            Time.timeScale=1;
            bool pass=invalid && cooldown && paused && deployed && waits && won && once && reset && lost;
            Debug.Log($"MANUAL_BATTLE_CHECK: invalid={invalid}; cooldown={cooldown}; paused={paused}; deployed={deployed}; waitForUnits={waits}; won={won}; gateOnce={once}; reset={reset}; lost={lost}.");
            Application.Quit(pass ? 0 : 1);
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
