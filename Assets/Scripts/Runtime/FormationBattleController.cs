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
        public int SelectedSlot {get;private set;}=0;
        public static Rect BattleRect=>new Rect(0,0,1280,720);
        public static Rect BoardInputRect=>new Rect(188,65,816,418);
        public Camera battleCamera;
        public FormationBattleTerrainAuthoring terrain;
        public Material actorTemplate,lineTemplate;
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
        Vector2 pointerStart;
        string notice="拖动场上角色换位，或选中下方角色后点击左侧格子。";
        static readonly string[] MonsterIds={"M10","M02","M07","M08","M09","M17"};

        IEnumerator Start()
        {
            Session=FormationShopSession.Current??FormationShopSession.StartNew(seed);seed=Session.Seed;
            Model=new FormationBattleModel(seed,terrain!=null?terrain.BuildLayout():null);Session.ApplyTo(Model);
            Model.LimitSeconds=roundSeconds;Model.ReviveSeconds=reviveSeconds;
            if(battleCamera==null)battleCamera=Camera.main;
            actorRoot=new GameObject("Dynamic formation actors").transform;
            actorRoot.SetParent(transform,false);
            actorMaterial=actorTemplate!=null?new Material(actorTemplate):new Material(Shader.Find("Sprites/Default"));
            lineMaterial=lineTemplate!=null?new Material(lineTemplate):new Material(Shader.Find("Hidden/Internal-Colored"));
            lineMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull",0);lineMaterial.SetInt("_ZWrite",0);
            // Only existing approved character packages are reused; no new character artwork.
            var roots=MonsterIds.Select(id=>"MonsterCombatArt/"+id)
                .Concat(Enumerable.Range(1,12).Select(i=>"DefenderArt/D"+i.ToString("00"))).ToArray();
            foreach(string root in roots)
            {
                loading="准备原有动作  "+root.Split('/').Last();
                var catalog=ArtCatalog.Read(root);art[root.Split('/').Last()]=catalog;pinned.Add(root);
                ArtMemoryCache.Pin(pinned);
                if(root.StartsWith("MonsterCombatArt/",StringComparison.Ordinal))_ = catalog.Portrait;
                foreach(var clip in catalog.Clips.Values)
                    if(clip.Key=="idle"||clip.Key=="walk"||clip.Key.StartsWith("attack",StringComparison.Ordinal)||clip.Key=="death"||clip.Key=="heal")
                        yield return clip.Warm();
                yield return null;
            }
            sounds=gameObject.AddComponent<AudioSource>();sounds.spatialBlend=0;sounds.volume=.15f;
            hitSound=Tone("Formation hit",180,.055f);skillSound=Tone("Formation skill",420,.16f);
            Ready=true;EnsureOriginalShop();SelectFirstLineup();SyncActors();approvedShop.SyncShopBenchModels();
            Debug.Log("FORMATION_DEMO_READY: saved rectangle, existing flipbooks, finite shop and instance-based lineup");
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
            Model.Advance(Time.unscaledDeltaTime);
            approvedShop?.Tick();
            if(Input.GetKeyDown(KeyCode.Tab))SetMarketVisible(!MarketVisible);
            if(Input.GetKeyDown(KeyCode.Space)&&Model.Phase!=FormationPhase.Preparation)Model.Paused=!Model.Paused;
            if(Input.GetKeyDown(KeyCode.Escape))
            {if(shopOddsVisible)shopOddsVisible=false;else if(MarketVisible)SetMarketVisible(false);else if(Model.Phase==FormationPhase.Fighting||Model.Phase==FormationPhase.Exiting)Model.Paused=true;CancelFormationDrag();}
            SyncActors();
            if(soundEnabled&&Model.Clock-lastSoundAt>.09)
            {
                var hit=Model.Hits.LastOrDefault();
                if(hit!=null&&hit.At>lastSoundAt){sounds.PlayOneShot(hit.Skill?skillSound:hitSound,hit.Heal?.45f:1);lastSoundAt=Model.Clock;}
            }
        }
        void OnApplicationFocus(bool focus)
        {if(!focus){CancelFormationDrag();if(Ready&&(Model.Phase==FormationPhase.Fighting||Model.Phase==FormationPhase.Exiting))Model.Paused=true;}}
        public static Vector3 WorldPoint(Vector2 cell)=>new Vector3(cell.x-6.5f,.12f,cell.y-3.5f);
        public Vector3 ActorWorldPoint(Vector2 cell)=>WorldPoint(cell)+Vector3.up*(Model?.Terrain?.HeightAt(cell)??0);
        public Vector3 ActorWorldPoint(FormationActor actor)=>ActorWorldPoint(actor.Position);
        public static Vector2 CellScreen(Vector2 cell)=>TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(WorldPoint(cell)));
        bool TryCell(Vector2 point,out Vector2Int cell)
        {
            if(!BoardInputRect.Contains(point)||FormationShopBlocksPoint(point)){cell=default;return false;}
            var screen=TacticalViewport.Current.ToScreen(point);
            var ray=battleCamera.ScreenPointToRay(screen);var plane=new Plane(Vector3.up,new Vector3(0,.12f,0));
            if(!plane.Raycast(ray,out float distance)){cell=default;return false;}
            var p=ray.GetPoint(distance);cell=new Vector2Int(Mathf.RoundToInt(p.x+6.5f),Mathf.RoundToInt(p.z+3.5f));
            return cell.x>=0&&cell.x<14&&cell.y>=0&&cell.y<8;
        }
        public void ProcessPointer(EventType type,Vector2 point)
        {
            if(!Ready||Model.Phase!=FormationPhase.Preparation)return;
            formationShopPointer=point;
            if(type==EventType.MouseDown)
            {
                CancelFormationDrag();pointerHeld=true;pointerStart=point;
                if(FormationShopPointerDown(point))return;
                if(FormationShopBlocksPoint(point)||!BoardInputRect.Contains(point)){CancelFormationDrag();return;}
                // Pick the visible character above its feet before projecting onto the ground grid.
                // A click on a sprite's head must select it, not move it into the tile behind it.
                FormationFighter picked=null;float nearest=float.MaxValue;
                if(BoardInputRect.Contains(point))
                    foreach(var unit in Model.Units.Where(u=>u.Enabled))
                    {
                        var foot=CellScreen(unit.Spawn);
                        if(!new Rect(foot.x-27,foot.y-72,54,79).Contains(point))continue;
                        float distance=Mathf.Abs(point.x-foot.x)+Mathf.Abs(point.y-(foot.y-28))*.15f;
                        if(distance<nearest){nearest=distance;picked=unit;}
                    }
                if(picked!=null){SelectedSlot=picked.Slot;selectedPiece=picked.Instance;dragSlot=picked.Slot;shopDragInstance=picked.Instance;}
                else if(TryCell(point,out var cell))
                {
                    var on=Model.Units.FirstOrDefault(u=>u.Enabled&&Vector2.Distance(u.Spawn,cell)<.1f);
                    if(on!=null){SelectedSlot=on.Slot;selectedPiece=on.Instance;dragSlot=on.Slot;shopDragInstance=on.Instance;}
                    else if(SelectedSlot>=0&&SelectedSlot<Model.Units.Count&&Model.Units[SelectedSlot].Enabled)Place(SelectedSlot,cell);
                    else if(selectedPiece>0)PlaceReserve(selectedPiece,cell);
                }
            }
            if(type==EventType.MouseUp)
            {
                if(pointerHeld&&Vector2.Distance(point,pointerStart)>6)
                {
                    if(!FormationShopPointerUp(point)&&TryCell(point,out var cell))
                    {if(dragSlot>=0)Place(dragSlot,cell);else if(shopDragInstance>0)PlaceReserve(shopDragInstance,cell);}
                }
                CancelFormationDrag();SyncActors();
            }
        }
        void Place(int slot,Vector2Int cell)
        {
            bool ok=Model.TryPlace(slot,cell.x,cell.y);
            notice=ok?"站位已保存：下关和复活均沿用此位置。":"只能放在左侧部署区；非法落点保留原位置。";
            if(ok)Session.SavePositions(Model);
            SyncActors();
        }
        public void StartBattle()
        {
            if(!Ready||Model.Phase!=FormationPhase.Preparation)return;
            Session.SavePositions(Model);Session.ApplyTo(Model);
            if(!Model.StartBattle()){notice="请先将至少一名已购买角色加入上阵席位。";return;}
            SetMarketVisible(false);CancelFormationDrag();notice="自动攻击与技能已开启；清空守军后前往蓝门。";SyncActors();
        }
        public void Retry()
        {if(!Ready)return;Session.SavePositions(Model);Session.Retry();lastSoundAt=-10;SetMarketVisible(true);SelectFirstLineup();notice="同一防线已恢复；已购买角色与站位保留，重试不重复领取奖励。";SyncActors();}
        public void NextRound()
        {
            if(!Ready||Model.Phase!=FormationPhase.Won)return;
            Session.SavePositions(Model);var reward=Session.SettleWin(Model.Round);
            if(Model.Round<5)Session.AdvanceRound();
            lastSoundAt=-10;SetMarketVisible(Model.Phase==FormationPhase.Preparation);SelectFirstLineup();
            notice=reward!=null?$"通关奖励 +{reward.Total}金币、+{reward.Experience}经验；军团与站位已保留。":"本关奖励已领取，军团与站位保留。";SyncActors();
        }
        public void ResetDemo()
        {
            seed=unchecked(seed+1);finalRewardClaimed=false;selectedPiece=0;formationInspectedTag=null;formationMenuOpen=false;Session=FormationShopSession.StartNew(seed);Model.Reset(seed);Session.ApplyTo(Model);
            Model.LimitSeconds=roundSeconds;Model.ReviveSeconds=reviveSeconds;lastSoundAt=-10;
            SetMarketVisible(true);approvedShop?.ResetShopPresentation();approvedShop?.SyncShopBenchModels();SelectFirstLineup();notice="新挑战：从真实金币、有限牌库与三人军团开始。";SyncActors();
        }
        internal void SyncActors()
        {
            if(!Ready)return;
            var current=new HashSet<int>();
            foreach(var unit in Model.Units){if(!unit.Enabled)continue;current.Add(unit.Id);Present(unit,false);}
            foreach(var defender in Model.Defenders){current.Add(defender.Id);Present(defender,true);}
            foreach(var id in bodies.Keys.Where(id=>!current.Contains(id)).ToArray()){Destroy(bodies[id].gameObject);bodies.Remove(id);}
        }
        void Present(FormationActor actor,bool defender)
        {
            if(!bodies.TryGetValue(actor.Id,out var body))
            {
                body=new GameObject(actor.Name,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                body.transform.SetParent(actorRoot,false);body.sharedMaterial=actorMaterial;bodies[actor.Id]=body;
                ActorGrounding.Attach(body,1.1f);
            }
            double now=Model.Clock,deathAge=now-actor.LastDeathAt;
            body.enabled=actor.Alive||deathAge>=0&&deathAge<.65;
            if(!body.enabled)return;
            var catalog=art[actor.ArtId];
            bool acting=actor.Alive&&now<actor.ActionUntil&&actor.LastActionAt>=0;
            string key=!actor.Alive?"death":actor.IsMoving?"walk":acting?"attack":"idle";
            if(!catalog.Clips.TryGetValue(key,out var clip))clip=catalog.Clips["idle"];
            double age=key=="death"?deathAge:key=="attack"?now-actor.LastActionAt:now;
            if(key=="attack")
            {
                // Fit the complete existing action into the same model action window that locks movement.
                double progress=Math.Max(0,Math.Min(.999999,age/Math.Max(.001,actor.ActionUntil-actor.LastActionAt)));
                catalog.Clips.TryGetValue("attack_start",out var begin);catalog.Clips.TryGetValue("attack_end",out var end);
                double total=clip.Duration+(begin?.Duration??0)+(end?.Duration??0),at=progress*total;
                if(begin!=null&&at<begin.Duration){clip=begin;age=at;}
                else if(at<(begin?.Duration??0)+clip.Duration)age=at-(begin?.Duration??0);
                else {age=at-(begin?.Duration??0)-clip.Duration;clip=end??clip;}
            }
            int frame=(int)(Math.Max(0,age)*(clip.Count/clip.Duration));
            frame=key=="death"||key=="attack"?Mathf.Clamp(frame,0,clip.Count-1):frame%clip.Count;
            body.sprite=clip.Frames[frame];body.flipX=actor.Facing.x<-.01f;
            body.transform.position=ActorWorldPoint(actor);body.transform.rotation=battleCamera.transform.rotation;
            // Preserve each original frame's proportions and foot pivot.
            float scale=1.12f/Mathf.Max(.5f,catalog.Height>0?catalog.Height:1.4f);
            body.transform.localScale=Vector3.one*scale;
            body.sortingOrder=5000-Mathf.RoundToInt(actor.Position.y*50);
            bool skill=now-actor.LastSkillAt<.45;
            body.color=now-actor.LastHitAt<.13?new Color(1,.5f,.42f):skill?new Color(1,.89f,.58f):Color.white;
        }
        void OnRenderObject()
        {
            if(!Ready||Camera.current!=battleCamera)return;
            lineMaterial.SetPass(0);GL.PushMatrix();GL.MultMatrix(Matrix4x4.identity);GL.Begin(GL.LINES);
            foreach(var h in Model.Hits)
            {
                float age=(float)(Model.Clock-h.At);if(age<0||age>.24f)continue;
                var c=h.Heal?new Color(.35f,1,.65f):h.Skill?new Color(1,.66f,.2f):new Color(1,.88f,.6f);
                c.a=1-age/.24f;GL.Color(c);
                GL.Vertex(ActorWorldPoint(h.From)+Vector3.up*.5f);GL.Vertex(ActorWorldPoint(h.To)+Vector3.up*.5f);
            }
            GL.End();GL.PopMatrix();
        }
        void OnDestroy()
        {
            approvedShop?.Dispose();if(formationCoin!=null)Destroy(formationCoin);if(formationStar!=null)Destroy(formationStar);
            ArtMemoryCache.Pin(Array.Empty<string>());
            if(actorMaterial!=null)Destroy(actorMaterial);if(lineMaterial!=null)Destroy(lineMaterial);
            if(hitSound!=null)Destroy(hitSound);if(skillSound!=null)Destroy(skillSound);
        }
    }
}
