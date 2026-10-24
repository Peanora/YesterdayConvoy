using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    /// <summary>Separate facility integration trial. Gameplay uses the normal ManualBattle model.</summary>
    public sealed class FacilityTrialController:MonoBehaviour
    {
        public int mapIndex;
        public Vector3[] route;
        public Vector3[] deployableTiles;
        public ManualBattleDefinition[] roles;
        public Font interfaceFont;
        public ManualBattle Battle {get;private set;}
        public int SelectedRole {get;private set;}
        readonly Dictionary<string,Transform> actors=new Dictionary<string,Transform>();
        readonly List<Transform> facilityViews=new List<Transform>(),sealViews=new List<Transform>();
        readonly List<LineRenderer> beams=new List<LineRenderer>();
        Material glow,solid,lineMaterial;
        Transform presentation;
        long request;
        float speed=1;
        string feedback="点击部署，让角色沿已确认的地图路线行进";
        static readonly string[] Titles={"工业转运站 · 祭坛脉冲","雪原中继站 · 霜星封地","芦苇水道 · 留声机 / 燃烧芦苇"};
        void Awake()
        {
            Application.runInBackground=true;
            glow=new Material(Shader.Find("Unlit/Color"));solid=new Material(Shader.Find("Standard"));solid.color=new Color(.16f,.23f,.28f);
            lineMaterial=new Material(Shader.Find("Sprites/Default"));
            presentation=new GameObject("Facility trial presentation").transform;
            ResetTrial(0);
        }
        IEnumerator Start()
        {
            string[] args=Environment.GetCommandLineArgs();
            string requested=Array.Find(args,a=>a.StartsWith("--facility-map="));
            if(requested!=null&&int.TryParse(requested.Substring(15),out int n)&&n>=0&&n<3&&n!=mapIndex){SceneManager.LoadScene(n);yield break;}
            string selected=Array.Find(args,a=>a.StartsWith("--facility-role="));
            if(selected!=null&&int.TryParse(selected.Substring(16),out int r)&&r>=0&&r<roles.Length)ResetTrial(r);
            string evidence=Array.Find(args,a=>a.StartsWith("--facility-check="));if(evidence==null)yield break;
            string folder=evidence.Substring(17);Directory.CreateDirectory(folder);
            Time.timeScale=0;
            bool deployed=Deploy();bool captured=false;
            for(int i=0;i<1800&&Battle.Phase==BattlePhase.Running;i++)
            {
                Battle.Advance(.1f);Sync();yield return null;
                bool effect=mapIndex==0?Battle.AltarHits>0||Battle.Units.Any(u=>u.AltarEmpowered):
                    mapIndex==1?Battle.SealedTileCount>0:
                    SelectedRole==1?Battle.Ignitions>0:Battle.AttackerGramophoneHits>0;
                if(effect&&!captured)
                {
                    captured=true;
                    // Keep the naturally triggered effect visible long enough to show the flame/pulse, without injecting status.
                    if(mapIndex==2&&SelectedRole==1){Battle.Advance(.4f);Sync();yield return null;}
                    yield return new WaitForEndOfFrame();
                    var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,$"map-{mapIndex}-role-{SelectedRole}.png"),shot.EncodeToPNG());Destroy(shot);
                    break;
                }
            }
            Battle.SetPaused(true);double clock=Battle.Clock;Battle.Advance(10);bool pause=Battle.Clock==clock;
            int altar=Battle.AltarHits,seal=Battle.SealedTileCount,ignitions=Battle.Ignitions,gram=Battle.GramophoneHits;
            Battle.Restart();Sync();bool reset=Battle.Phase==BattlePhase.Ready&&Battle.Units.Count==0&&Battle.SealedTileCount==0&&Battle.AltarHits==0&&Battle.Ignitions==0&&Battle.GramophoneHits==0;
            Debug.Log($"FACILITY_PLAYER_CHECK: map={mapIndex}; role={SelectedRole}; deployed={deployed}; effect={captured}; pause={pause}; reset={reset}; altar={altar}; sealed={seal}; ignitions={ignitions}; gramophone={gram}");
            Application.Quit(deployed&&captured&&pause&&reset?0:1);
        }
        public void ResetTrial(int role)
        {
            SelectedRole=role;request=0;
            foreach(Transform child in presentation)Destroy(child.gameObject);
            actors.Clear();facilityViews.Clear();sealViews.Clear();beams.Clear();
            var original=roles[role].Snapshot();var c=roles[role].Engagement.Snapshot();
            var settings=new ManualBattleSettings(original.CardId,original.UnitId,30,1,12,1,.5f,
                Math.Max(1000,original.Health),original.Speed,1,2,1,1);
            var combat=new EngagementSettings(unitAttack:c.UnitAttack,unitArmor:c.UnitArmor,unitRange:c.UnitRange,unitInterval:c.UnitInterval,
                defenderHealth:15000,defenderAttack:8,defenderArmor:12,defenderRange:3,defenderInterval:1,blockCount:0,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,
                attackDuration:c.AttackDuration,attackWindup:c.AttackWindup,effects:c.Effects,canAttack:c.CanAttack);
            Vector3 guard=mapIndex==0?new Vector3(-2,.35f,1):mapIndex==1?new Vector3(-2,.35f,1):role==1?new Vector3(-3,.35f,0):new Vector3(2.5f,.35f,-1);
            Battle=new ManualBattle(settings,route,guard,combat);
            var facilities=new List<FacilitySettings>();
            if(mapIndex==0)facilities.Add(new FacilitySettings(FacilityKind.Altar,new Vector3(1,0,1),3.2f));
            if(mapIndex==2)
            {
                facilities.Add(new FacilitySettings(FacilityKind.Gramophone,new Vector3(4,0,-1),4));
                foreach(var p in new[]{new Vector3(-3,0,0),new Vector3(-3,0,-1),new Vector3(-3,0,-2),new Vector3(-2,0,-2)})
                    facilities.Add(new FacilitySettings(FacilityKind.Reeds,p,.5f,1,4));
            }
            Battle.ConfigureFacilities(facilities,mapIndex==1?deployableTiles:null);
            foreach(var f in Battle.Facilities)
            {
                var node=new GameObject(f.Settings.Kind.ToString()).transform;node.SetParent(presentation);node.position=f.Settings.Position;
                facilityViews.Add(node);
                if(f.Settings.Kind==FacilityKind.Reeds)
                {
                    for(int i=0;i<7;i++)Mesh(node,new Vector3((i%3-1)*.16f,.28f,(i/3-1)*.18f),new Vector3(.035f,.55f,.035f),new Color(.48f,.44f,.23f));
                    var flames=new GameObject("Flames").transform;flames.SetParent(node,false);
                    for(int k=0;k<5;k++)
                    {
                        var flame=Mesh(flames,new Vector3((k%3-1)*.18f,.65f,(k/3-.5f)*.22f),new Vector3(.12f,1.15f,.12f),Color.white);
                        flame.GetComponent<Renderer>().sharedMaterial=glow;
                        var block=new MaterialPropertyBlock();block.SetColor("_Color",k%2==0?new Color(1,.3f,.04f):new Color(1,.72f,.18f));flame.GetComponent<Renderer>().SetPropertyBlock(block);
                        flame.localRotation=Quaternion.Euler(0,k*31,12);
                    }
                    flames.gameObject.SetActive(false);
                }
                else
                {
                    Mesh(node,new Vector3(0,.48f,0),new Vector3(.48f,.18f,.48f),new Color(.12f,.17f,.22f));
                    var crystal=Mesh(node,new Vector3(0,.9f,0),new Vector3(.23f,.65f,.23f),f.Settings.Kind==FacilityKind.Altar?new Color(.95f,.25f,.18f):Color.cyan);
                    crystal.localRotation=Quaternion.Euler(0,45,25);
                }
                var ring=new GameObject("Effect radius").AddComponent<LineRenderer>();ring.transform.SetParent(node,false);
                ring.useWorldSpace=false;ring.sharedMaterial=lineMaterial;ring.widthMultiplier=.012f;ring.positionCount=65;
                for(int k=0;k<=64;k++){float a=k*Mathf.PI*2/64;ring.SetPosition(k,new Vector3(Mathf.Cos(a)*f.Settings.Radius,.05f,Mathf.Sin(a)*f.Settings.Radius));}
            }
            var defender=Mesh(presentation,guard,new Vector3(.5f,1,.5f),new Color(.2f,.6f,.85f));defender.name="Defender";
            actors[Battle.Defender.EntityId]=defender;
            feedback="独立设施试玩：数值为本作缩放测试值";Sync();
        }
        Transform Mesh(Transform owner,Vector3 pos,Vector3 scale,Color color)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(go.GetComponent<Collider>());go.transform.SetParent(owner,false);
            go.transform.localPosition=pos;go.transform.localScale=scale;
            var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=solid;var props=new MaterialPropertyBlock();props.SetColor("_Color",color);renderer.SetPropertyBlock(props);return go.transform;
        }
        public bool Deploy()
        {
            Battle.Begin();var receipt=Battle.TryDeploy(Battle.RunId,++request,Battle.Entry,true);feedback=receipt.Success?"部署成功":receipt.Failure.ToString();Sync();return receipt.Success;
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))Battle.SetPaused(!Battle.IsPaused);
            Battle.Advance(Time.deltaTime*speed);Sync();
        }
        void Sync()
        {
            if(Battle==null)return;
            foreach(var unit in Battle.Units)
            {
                if(!actors.TryGetValue(unit.EntityId,out Transform t))
                {
                    t=new GameObject(unit.EntityId).transform;t.SetParent(presentation);
                    var visual=new GameObject("Original animated sprite",typeof(SpriteRenderer),typeof(DemoSpriteVisual));visual.transform.SetParent(t,false);
                    visual.GetComponent<DemoSpriteVisual>().Bind(Battle,unit,roles[SelectedRole].Card);actors.Add(unit.EntityId,t);
                }
                t.position=unit.Motion.Position;t.gameObject.SetActive(unit.Active||Battle.Clock<unit.DeathTime+1.5);
            }
            if(actors.TryGetValue(Battle.Defender.EntityId,out Transform defender))defender.gameObject.SetActive(Battle.Defender.Alive);
            int i=0;
            foreach(var f in Battle.Facilities)
            {
                Transform node=facilityViews[i++];var line=node.GetComponentInChildren<LineRenderer>();
                Color color=f.Settings.Kind==FacilityKind.Altar?new Color(1,.26f,.12f):f.Settings.Kind==FacilityKind.Reeds?(f.Burning?new Color(1,.4f,.05f):new Color(.6f,.55f,.25f)):
                    f.Owner==FacilityOwner.Attackers?new Color(1,.35f,.2f):f.Owner==FacilityOwner.Defenders?Color.cyan:Color.gray;
                Color ringColor=color;ringColor.a=Battle.Clock-f.LastActivation<.65?.65f:.22f;
                line.startColor=line.endColor=ringColor;
                line.widthMultiplier=Battle.Clock-f.LastActivation<.65?.035f:.009f;
                if(f.Settings.Kind==FacilityKind.Reeds)
                {
                    foreach(var renderer in node.GetComponentsInChildren<MeshRenderer>())
                    {var block=new MaterialPropertyBlock();block.SetColor("_Color",color);renderer.SetPropertyBlock(block);}
                    var flames=node.Find("Flames");flames.gameObject.SetActive(f.Burning);
                    flames.localScale=new Vector3(1,.85f+.2f*Mathf.Sin((float)Battle.Clock*11+i),1);
                }
            }
            if(sealViews.Count!=Battle.SealedTileCount)
            {
                foreach(var old in sealViews)Destroy(old.gameObject);sealViews.Clear();
                foreach(var cell in Battle.SealedTiles)
                    sealViews.Add(Mesh(presentation,new Vector3(cell.x,.42f,cell.y),new Vector3(.83f,.10f,.83f),new Color(.035f,.07f,.12f)));
            }
            int used=0;
            foreach(var shot in Battle.CombatShots)
            {
                if(shot.Resolved||Battle.Clock<shot.StartedAt)continue;
                if(used==beams.Count)
                {var line=new GameObject("Combat projectile").AddComponent<LineRenderer>();line.transform.SetParent(presentation);line.sharedMaterial=lineMaterial;line.positionCount=2;line.widthMultiplier=.035f;beams.Add(line);}
                var beam=beams[used++];beam.gameObject.SetActive(true);float fraction=(float)((Battle.Clock-shot.StartedAt)/(shot.ImpactAt-shot.StartedAt));
                Vector3 p=Vector3.Lerp(shot.Origin,shot.Destination,fraction)+Vector3.up*.5f;
                beam.SetPosition(0,p);beam.SetPosition(1,p+Vector3.up*.15f);beam.startColor=beam.endColor=shot.FromDefender?Color.cyan:Color.yellow;
            }
            for(int j=used;j<beams.Count;j++)beams[j].gameObject.SetActive(false);
        }
        void OnGUI()
        {
            if(Battle==null)return;if(interfaceFont!=null)GUI.skin.font=interfaceFont;
            float scale=Screen.width/1920f;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            GUI.Box(new Rect(20,18,780,128),"");var title=new GUIStyle(GUI.skin.label){fontSize=25};
            GUI.Label(new Rect(35,25,750,36),Titles[mapIndex],title);
            string phase=Battle.IsPaused?"已暂停":Battle.Phase==BattlePhase.Ready?"待部署":Battle.Phase==BattlePhase.Running?"交战中":Battle.Phase==BattlePhase.Won?"突破成功":"本场结束";
            GUI.Label(new Rect(35,65,750,26),$"{roles[SelectedRole].Card.DisplayName}  ·  {phase}  ·  时间 {Battle.Clock:0.0}  ·  {feedback}");
            GUI.Label(new Rect(35,94,750,26),$"祭坛命中 {Battle.AltarHits}  |  封地 {Battle.SealedTileCount}  |  留声机命中 {Battle.GramophoneHits} / 治疗 {Battle.FacilityHealing:0}  |  点燃 {Battle.Ignitions}");
            float bottom=Screen.height/scale-90;
            for(int n=0;n<3;n++)if(GUI.Button(new Rect(20+n*150,bottom,140,32),new[]{"工业转运站","雪原中继站","芦苇水道"}[n]))SceneManager.LoadScene(n);
            for(int n=0;n<roles.Length;n++)if(GUI.Button(new Rect(500+n*170,bottom,160,32),roles[n].Card.DisplayName))ResetTrial(n);
            if(GUI.Button(new Rect(20,bottom+40,140,35),"部署角色"))Deploy();
            if(GUI.Button(new Rect(170,bottom+40,140,35),Battle.IsPaused?"继续":"暂停（空格）"))Battle.SetPaused(!Battle.IsPaused);
            if(GUI.Button(new Rect(320,bottom+40,140,35),"重新开始"))ResetTrial(SelectedRole);
            if(GUI.Button(new Rect(470,bottom+40,140,35),$"速度 ×{speed:0}"))speed=speed==1?3:1;
            foreach(var u in Battle.Units.Where(u=>u.Active))Label(u.Motion.Position+Vector3.up*2.4f,$"{u.Health:0}/{u.MaxHealth:0}"+(u.AltarEmpowered?" 法术化":""));
            foreach(var d in Battle.Defenders.Where(d=>d.Alive))Label(d.Position+Vector3.up*1.2f,$"守军 {d.Health:0}"+(Battle.DefenderCamouflaged(d)?" 迷彩":""));
            foreach(var f in Battle.Facilities)Label(f.Settings.Position+Vector3.up*1.6f,f.Settings.Kind==FacilityKind.Gramophone?$"留声机 {(f.Owner==FacilityOwner.Attackers?"进攻方":f.Owner==FacilityOwner.Defenders?"守方":"未占领")} {f.Capture:0.0}/{f.Settings.CaptureTime:0}":f.Settings.Kind==FacilityKind.Reeds?(f.Burning?"燃烧":"芦苇"):"祭坛");
        }
        void Label(Vector3 point,string text)
        {
            var p=Camera.main.WorldToScreenPoint(point);float scale=Screen.width/1920f;
            GUI.Box(new Rect(p.x/scale-90,(Screen.height-p.y)/scale,180,23),text);
        }
        void OnDestroy(){if(presentation!=null)Destroy(presentation.gameObject);if(glow!=null)Destroy(glow);if(solid!=null)Destroy(solid);if(lineMaterial!=null)Destroy(lineMaterial);}
    }
}
