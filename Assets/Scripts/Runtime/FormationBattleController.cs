using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    // Scene terrain is authored by FormationBattleTools; this component creates actors only.
    public sealed partial class FormationBattleController : MonoBehaviour
    {
        public const string SceneName="FormationBattle";
        public FormationBattleModel Model {get;private set;}
        public FormationShopSession Session {get;private set;}
        public ShopTrialModel Shop=>Session?.Shop;
        public bool Ready {get;private set;}
        public bool CombatViewActive=>Ready&&Model!=null&&(Model.Phase==FormationPhase.Fighting||Model.Phase==FormationPhase.Exiting);
        public bool FormationPreparationUiVisible=>Ready&&!CombatViewActive;
        public int SelectedSlot {get;private set;}=0;
        public static Rect BattleRect=>new Rect(0,0,1280,720);
        public static Rect BoardInputRect=>new Rect(188,64,816,514);
        public Camera battleCamera;
        public FormationBattleTerrainAuthoring terrain;
        public Material actorTemplate,lineTemplate;
        public CardDefinition[] formationCards=Array.Empty<CardDefinition>();
        public int seed=20261117;
        public float roundSeconds=90,reviveSeconds=8;
        readonly Dictionary<string,ArtCatalog> art=new Dictionary<string,ArtCatalog>();
        readonly Dictionary<int,SpriteRenderer> bodies=new Dictionary<int,SpriteRenderer>();
        readonly List<string> pinned=new List<string>();
        Material actorMaterial,lineMaterial;
        Transform actorRoot;
        string loading="正在准备战斗动作…";
        double lastSoundAt=-10;
        AudioSource sounds;
        AudioClip hitSound,skillSound;
        bool soundEnabled=true;
        int dragSlot=-1;
        bool pointerHeld;
        bool pendingClickDeployment;
        Vector2 pointerStart;
        string notice="拖动场上角色换位，或选中下方角色后点击左侧格子。";

        IEnumerator Start()
        {
            FormationUnitArt.RegisterCards(formationCards);
            bool fullCampaign=!FormationBattleSmokeDriver.Requested&&Array.IndexOf(Environment.GetCommandLineArgs(),"-formationLegacyDemo")<0;
            Session=FormationShopSession.Current??FormationShopSession.StartNew(seed,fullCampaign:fullCampaign);seed=Session.Seed;
            Model=new FormationBattleModel(seed,terrain!=null?terrain.BuildLayout():null);Session.ApplyTo(Model);
            if(Session.Run!=null)Model.BindCampaign(Session.Run);
            else {Model.LimitSeconds=roundSeconds;Model.ReviveSeconds=reviveSeconds;}
            if(battleCamera==null)battleCamera=Camera.main;
            actorRoot=new GameObject("Dynamic formation actors").transform;
            actorRoot.SetParent(transform,false);
            actorMaterial=actorTemplate!=null?new Material(actorTemplate):new Material(Shader.Find("Sprites/Default"));
            lineMaterial=lineTemplate!=null?new Material(lineTemplate):new Material(Shader.Find("Hidden/Internal-Colored"));
            lineMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull",0);lineMaterial.SetInt("_ZWrite",0);
            // Metadata covers the roster; only current actors warm their actual action pages.
            foreach(string id in RequiredFormationArtIds().ToArray())yield return WarmFormationArt(id);
            sounds=gameObject.AddComponent<AudioSource>();sounds.spatialBlend=0;sounds.volume=.15f;
            hitSound=Tone("Formation hit",180,.055f);skillSound=Tone("Formation skill",420,.16f);
            Ready=true;EnsureOriginalShop();SelectFirstLineup();SyncActors();approvedShop.SyncShopBenchModels();
            InitializeFormationCamera();
            Debug.Log(Session.Run!=null?"FORMATION_CAMPAIGN_READY: approved v0.7 full challenge, finite economy and authored board":"FORMATION_DEMO_READY: legacy formation test");
        }
        static AudioClip Tone(string name,float hz,float duration)
        {
            int count=(int)(22050*duration);var data=new float[count];
            for(int i=0;i<count;i++){float t=i/22050f;data[i]=Mathf.Sin(t*hz*2*Mathf.PI)*(1-i/(float)count)*.35f;}
            var clip=AudioClip.Create(name,count,1,22050,false);clip.SetData(data,0);return clip;
        }
        void Update()
        {
            if(!Ready)return;
            battleCamera.rect=TacticalViewport.Current.CameraRect(BattleRect);
            RefreshFormationAnimationProfiles();
            if(!FormationShowcaseRecorder.Drives(this))
            {Model.Advance(Time.unscaledDeltaTime);TickCampaignSettlement();}
            RefreshFormationOperatingVisibility();
            TickFormationCamera(Time.unscaledDeltaTime);
            approvedShop?.Tick();
            if(Input.GetKeyDown(KeyCode.Tab)&&Model.Phase==FormationPhase.Preparation&&!CampaignInteractionBlocked&&!formationMenuOpen)ToggleFormationMarket();
            if(Input.GetKeyDown(KeyCode.V))ToggleFormationView();
            if(Input.GetKeyDown(KeyCode.Space)&&Model.Phase!=FormationPhase.Preparation)Model.Paused=!Model.Paused;
            if(Input.GetKeyDown(KeyCode.Escape))
            {if(campaignInventoryOpen||campaignFormationOpen||campaignRouteOpen||campaignScoutingOpen)CloseCampaignPanels();else if(shopOddsVisible)shopOddsVisible=false;else if(CloseFormationTraitDetails()){}else if(MarketVisible)SetMarketVisible(false);else if(Model.Phase==FormationPhase.Fighting||Model.Phase==FormationPhase.Exiting)Model.Paused=true;CancelFormationDrag();}
            EnsureFormationArt();UpdateDeploymentPreview();SyncActors();
            if(soundEnabled&&Model.Clock-lastSoundAt>.09)
            {
                var hit=Model.Hits.LastOrDefault();
                if(hit!=null&&hit.At>lastSoundAt){sounds.PlayOneShot(hit.Skill?skillSound:hitSound,hit.Heal?.45f:1);lastSoundAt=Model.Clock;}
            }
        }
        void OnApplicationFocus(bool focus)
        {if(!focus){CancelFormationDrag();if(Ready&&(Model.Phase==FormationPhase.Fighting||Model.Phase==FormationPhase.Exiting))Model.Paused=true;}}
        public static Vector3 WorldPoint(Vector2 cell)=>new Vector3(cell.x-(FormationBattleModel.Width-1)*.5f,.12f,cell.y-(FormationBattleModel.Height-1)*.5f);
        public Vector3 ActorWorldPoint(Vector2 cell)=>WorldPoint(cell)+Vector3.up*(Model?.Terrain?.HeightAt(cell)??0);
        public Vector3 ActorWorldPoint(FormationActor actor)=>ActorWorldPoint(actor.Position);
        public static Vector2 CellScreen(Vector2 cell)=>TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(WorldPoint(cell)));
        bool TryCell(Vector2 point,out Vector2Int cell)
        {
            if(!CanEditFormationView||!BoardInputRect.Contains(point)||FormationShopBlocksPoint(point)){cell=default;return false;}
            var screen=TacticalViewport.Current.ToScreen(point);
            var ray=battleCamera.ScreenPointToRay(screen);var plane=new Plane(Vector3.up,new Vector3(0,.12f,0));
            if(!plane.Raycast(ray,out float distance)){cell=default;return false;}
            // A 2x2 body's centre lies on a grid seam. Choose one side consistently rather
            // than alternating at .5 ties with banker's rounding as the body crosses columns.
            var p=ray.GetPoint(distance);cell=new Vector2Int(Mathf.FloorToInt(p.x+FormationBattleModel.Width*.5f+.0001f),Mathf.FloorToInt(p.z+FormationBattleModel.Height*.5f+.0001f));
            return cell.x>=0&&cell.x<FormationBattleModel.Width&&cell.y>=0&&cell.y<FormationBattleModel.Height;
        }
        public void ProcessPointer(EventType type,Vector2 point)
        {
            if(!Ready||Model.Phase!=FormationPhase.Preparation||CampaignInteractionBlocked)return;
            // Detail cards also overlap part of the lineup rail. They get first refusal
            // before either terrain painting or seat dragging can capture the pointer.
            if(FormationTraitDetailsOpen&&FormationTraitDetailsRect.Contains(point)){CancelFormationDrag();return;}
            if(ProcessCampaignSeaPointer(type,point))return;
            formationShopPointer=point;
            if(type==EventType.MouseDown)
            {
                CancelFormationDrag();pointerHeld=true;pointerStart=point;
                if(FormationShopPointerDown(point))return;
                if(!CanEditFormationView){CancelFormationDrag();return;}
                if(FormationShopBlocksPoint(point)||!BoardInputRect.Contains(point)){CancelFormationDrag();return;}
                // Pick the visible character above its feet before projecting onto the ground grid.
                // A click on a sprite's head must select it, not move it into the tile behind it.
                FormationFighter picked=null;float nearest=float.MaxValue;
                if(BoardInputRect.Contains(point))
                    foreach(var unit in Model.Units.Where(u=>u.Enabled))
                    {
                        var foot=CellScreen(unit.SpawnCenter);
                        if(!FormationActorScreenRect(unit).Contains(point))continue;
                        float distance=Mathf.Abs(point.x-foot.x)+Mathf.Abs(point.y-(foot.y-28))*.15f;
                        if(distance<nearest){nearest=distance;picked=unit;}
                    }
                if(picked!=null){SelectedSlot=picked.Slot;selectedPiece=picked.Instance;dragSlot=picked.Slot;shopDragInstance=picked.Instance;PrepareFieldDeploymentGrab(picked,point);}
                else if(TryCell(point,out var cell))
                {
                    var on=Model.Units.FirstOrDefault(u=>u.Enabled&&u.DeploymentCells.Contains(cell));
                    if(on!=null){SelectedSlot=on.Slot;selectedPiece=on.Instance;dragSlot=on.Slot;shopDragInstance=on.Instance;PrepareFieldDeploymentGrab(on,point);}
                    else if(SelectedSlot>=0&&SelectedSlot<Model.Units.Count&&Model.Units[SelectedSlot].Enabled)
                    {var selected=Model.Units[SelectedSlot];shopDragInstance=selected.Instance;dragSlot=selected.Slot;deploymentGrabOffset=Vector2Int.zero;deploymentGrabGroundOffset=Vector3.zero;pendingClickDeployment=true;}
                    else if(selectedPiece>0)
                    {shopDragInstance=selectedPiece;deploymentGrabOffset=Vector2Int.zero;deploymentGrabGroundOffset=Vector3.zero;pendingClickDeployment=true;}
                }
            }
            if(type==EventType.MouseDrag)UpdateDeploymentPreview();
            if(type==EventType.MouseUp)
            {
                if(pointerHeld&&Vector2.Distance(point,pointerStart)>6)
                {
                    if(!FormationShopPointerUp(point)&&TryCell(point,out var cell))
                    {var anchor=DeploymentAnchor(cell);if(dragSlot>=0)Place(dragSlot,anchor);else if(shopDragInstance>0)PlaceReserve(shopDragInstance,anchor);}
                }
                else if(pointerHeld&&pendingClickDeployment&&TryCell(point,out var clickCell))
                {if(dragSlot>=0)Place(dragSlot,clickCell);else if(shopDragInstance>0)PlaceReserve(shopDragInstance,clickCell);}
                CancelFormationDrag();SyncActors();
            }
        }
        void Place(int slot,Vector2Int cell)
        {
            var unit=Model.Units.FirstOrDefault(u=>u.Slot==slot);
            if(unit!=null)ShopAction(()=>Session.TryDeploy(unit.Instance,cell));
        }
        public void StartBattle()
        {
            if(!Ready||Model.Phase!=FormationPhase.Preparation||CampaignInteractionBlocked||Campaign!=null&&(!Session.CanShop||Campaign.CurrentNode.Kind=="补给"))return;
            if(!RosterArtReady){notice="正在准备上阵角色动作，请稍候。";return;}
            if(Campaign!=null&&!Model.ValidateSeaPlacement(out var seaReason)){notice=seaReason;return;}
            Session.SavePositions(Model);Session.ApplyTo(Model);
            if(!Model.StartBattle()){notice="请先将至少一名已购买角色加入上阵席位。";return;}
            campaignSeaPainting=false;CloseFormationTraitDetails();SetMarketVisible(false);CancelFormationDrag();RefreshFormationOperatingVisibility();notice="自动攻击与技能已开启；清空守军后前往蓝门。";SyncActors();
            SetFormationView(true);
        }
        public void Retry()
        {if(!Ready)return;if(Session.Run!=null){notice="完整挑战的战斗结果已结算，请继续下一节点。";return;}Session.SavePositions(Model);Session.Retry();lastSoundAt=-10;SetMarketVisible(false);SelectFirstLineup();notice="同一防线已恢复；已购买角色与站位保留，重试不重复领取奖励。";SyncActors();SetFormationView(false);}
        public void NextRound()
        {
            if(Session?.Run!=null){ContinueCampaign();return;}
            if(!Ready||Model.Phase!=FormationPhase.Won)return;
            Session.SavePositions(Model);var reward=Session.SettleWin(Model.Round);
            if(Model.Round<5)Session.AdvanceRound();
            lastSoundAt=-10;SetMarketVisible(false);SelectFirstLineup();
            notice=reward!=null?$"通关奖励 +{reward.Total}金币、+{reward.Experience}经验；军团与站位已保留。":"本关奖励已领取，军团与站位保留。";SyncActors();
            SetFormationView(false);
        }
        public void ResetDemo()
        {
            bool fullCampaign=Session?.Run!=null;
            seed=unchecked(seed+1);finalRewardClaimed=false;selectedPiece=0;formationInspectedTag=null;formationMenuOpen=false;Session=FormationShopSession.StartNew(seed,fullCampaign:fullCampaign);Model.Reset(seed);Session.ApplyTo(Model);
            if(fullCampaign)Model.BindCampaign(Session.Run);else {Model.LimitSeconds=roundSeconds;Model.ReviveSeconds=reviveSeconds;}lastSoundAt=-10;
            campaignResultSettled=false;campaignInventoryOpen=false;campaignFormationOpen=false;
            CloseCampaignPanels();campaignSeaPainting=false;
            SetMarketVisible(false);approvedShop?.ResetShopPresentation();approvedShop?.SyncShopBenchModels();SelectFirstLineup();notice=fullCampaign?"新挑战：随机三名一费，3级、8金币。先选基础装备与战略预案。":"新挑战：无限测试金币，有限牌库与三人军团。";SyncActors();
            SetFormationView(false);
        }
        internal void SyncActors()
        {
            if(!Ready)return;
            var current=new HashSet<int>();
            foreach(var unit in Model.Units){if(!unit.Enabled)continue;current.Add(unit.Id);Present(unit,false);}
            foreach(var defender in Model.Defenders){current.Add(defender.Id);Present(defender,true);}
            foreach(var summon in Model.Summons){current.Add(summon.Id);Present(summon,false);}
            PresentFormationNativeEffects();
            foreach(var id in bodies.Keys.Where(id=>!current.Contains(id)).ToArray()){Destroy(bodies[id].gameObject);bodies.Remove(id);}
        }
        void Present(FormationActor actor,bool defender)=>PresentFormationActor(actor,defender);
        void OnRenderObject()
        {
            if(!Ready||Camera.current!=battleCamera)return;
            DrawDeploymentGrid();
            DrawCampaignSeaCells();
        }
        void OnDestroy()
        {
            DisposeFormationNativeEffects();
            approvedShop?.Dispose();if(formationCoin!=null)Destroy(formationCoin);if(formationStar!=null)Destroy(formationStar);
            ArtMemoryCache.Pin(Array.Empty<string>());
            if(actorMaterial!=null)Destroy(actorMaterial);if(lineMaterial!=null)Destroy(lineMaterial);
            if(hitSound!=null)Destroy(hitSound);if(skillSound!=null)Destroy(skillSound);
        }
    }
}
