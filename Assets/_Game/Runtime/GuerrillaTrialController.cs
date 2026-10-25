using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class GuerrillaTrialController:MonoBehaviour
    {
        public ManualBattleDefinition[] roles;
        public Vector3[] route;
        public Font interfaceFont;
        public ManualBattle Battle {get;private set;}
        readonly Dictionary<string,Transform> actors=new Dictionary<string,Transform>();
        readonly List<LineRenderer> shots=new List<LineRenderer>();
        Transform presentation;
        Material material;
        long request;
        string feedback="先部署战士/狙击手，再部署传令兵观察支援叠加";
        float speed=1;
        Vector2 rosterScroll;
        static string ShortName(string card)=>string.IsNullOrEmpty(card)?"无":card=="CARD_M03"?"猎犬":card=="CARD_M15"?"战士":card=="CARD_M16"?"狙击手":card=="CARD_M17"?"传令兵":card=="CARD_M18"?"盾卫":"萨卡兹战士";
        void Start()
        {
            material=new Material(Shader.Find("Sprites/Default"));ResetTrial();Application.runInBackground=true;
            string evidence=Array.Find(Environment.GetCommandLineArgs(),a=>a.StartsWith("--guerrilla-check="));
            string runner=Array.Find(Environment.GetCommandLineArgs(),a=>a.StartsWith("--runner-check="));
            if(runner!=null)StartCoroutine(VerifyRunner(runner.Substring(15)));
            else
            {
                string sniper=Array.Find(Environment.GetCommandLineArgs(),a=>a.StartsWith("--sniper-check="));
                if(sniper!=null)StartCoroutine(VerifySniper(sniper.Substring(15)));
                else
                {
                    string taunt=Array.Find(Environment.GetCommandLineArgs(),a=>a.StartsWith("--taunt-check="));
                    if(taunt!=null)StartCoroutine(VerifyTaunt(taunt.Substring(14)));
                    else if(evidence!=null)StartCoroutine(VerifyCommands(evidence.Substring(18)));
                }
            }
        }
        public void ResetTrial()
        {
            if(presentation!=null)Destroy(presentation.gameObject);actors.Clear();shots.Clear();
            presentation=new GameObject("Independent guerrilla combat").transform;
            var primary=roles.Single(r=>r.Card.CardId=="CARD_M17");var original=primary.Snapshot();var c=primary.Engagement.Snapshot();
            var settings=new ManualBattleSettings(original.CardId,original.UnitId,30,1,12,1,.5f,original.Health,original.Speed,1,1,1,1);
            var guard=new EngagementSettings(unitAttack:c.UnitAttack,unitArmor:c.UnitArmor,unitRange:c.UnitRange,unitInterval:c.UnitInterval,
                defenderHealth:2500,defenderAttack:18,defenderArmor:3,defenderRange:4,defenderInterval:1,blockCount:1,blockRadius:.65f,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,
                attackDuration:c.AttackDuration,attackWindup:c.AttackWindup,projectileTime:c.ProjectileTime,effects:c.Effects);
            Battle=new ManualBattle(settings,route,new Vector3(-2.5f,.35f,-2),guard,new[]{new Vector3(-1.5f,.35f,-2)});
            Battle.ConfigureRosterProfiles(roles.Select(r=>new BasicRosterProfile(r.Snapshot(),r.Engagement.Snapshot())));request=0;
            foreach(var d in Battle.Defenders)
            {
                var v=new GameObject(d.EntityId,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();v.transform.SetParent(presentation,false);
                v.sprite=primary.Engagement.KroosSprite;v.sharedMaterial=material;actors.Add(d.EntityId,v.transform);
            }
            Sync();
        }
        public bool Deploy(string cardId)
        {Battle.Begin();var result=Battle.TryDeployRole(Battle.RunId,++request,Battle.Entry,cardId);feedback=result.Success?"部署成功":result.Failure.ToString();Sync();return result.Success;}
        public void Advance(float delta){Battle.Advance(delta);Sync();}
        void Update(){Advance(Time.deltaTime*speed);if(Input.GetKeyDown(KeyCode.Space))Battle.SetPaused(!Battle.IsPaused);}
        void Sync()
        {
            foreach(var u in Battle.Units)
            {
                if(!actors.TryGetValue(u.EntityId,out var root))
                {
                    root=new GameObject(u.EntityId).transform;root.SetParent(presentation,false);
                    var child=new GameObject(u.CardId,typeof(SpriteRenderer),typeof(DemoSpriteVisual));child.transform.SetParent(root,false);
                    child.GetComponent<SpriteRenderer>().sharedMaterial=material;child.GetComponent<DemoSpriteVisual>().Bind(Battle,u,roles.Single(r=>r.Card.CardId==u.CardId).Card);actors.Add(u.EntityId,root);
                }
                root.position=u.Motion.Position;root.gameObject.SetActive(u.Active||Battle.Clock<u.DeathTime+1.5);
            }
            foreach(var d in Battle.Defenders)
            {
                var root=actors[d.EntityId];root.position=d.Position;root.rotation=Camera.main.transform.rotation;root.localScale=Vector3.one*.85f;root.gameObject.SetActive(d.Alive);
                var v=root.GetComponent<SpriteRenderer>();v.sortingOrder=DemoSpriteVisual.GetSortingOrder(Camera.main,d.Position)+30;
                v.color=Battle.Clock<d.LastHitTime+.15?new Color(1,.5f,.5f):Color.white;
            }
            int used=0;
            foreach(var s in Battle.CombatShots.Where(s=>!s.Resolved&&Battle.Clock>=s.StartedAt))
            {
                if(used==shots.Count){var l=new GameObject("Actual projectile").AddComponent<LineRenderer>();l.transform.SetParent(presentation,false);l.sharedMaterial=material;l.positionCount=2;l.widthMultiplier=.045f;l.sortingOrder=19000;shots.Add(l);}
                var line=shots[used++];line.enabled=true;var p=Vector3.Lerp(s.Origin,s.Destination,(float)((Battle.Clock-s.StartedAt)/(s.ImpactAt-s.StartedAt)))+Vector3.up*.7f;
                line.SetPosition(0,p);line.SetPosition(1,p+Vector3.up*.15f);line.startColor=line.endColor=s.FromDefender?Color.cyan:Color.yellow;
            }
            for(int i=used;i<shots.Count;i++)shots[i].enabled=false;
        }
        void OnGUI()
        {
            if(Battle==null)return;GUI.matrix=Matrix4x4.Scale(Vector3.one*(Screen.width/1280f));if(interfaceFont!=null)GUI.skin.font=interfaceFont;
            var title=new GUIStyle(GUI.skin.label){fontSize=23};var text=new GUIStyle(GUI.skin.label){fontSize=16};var button=new GUIStyle(GUI.skin.button){fontSize=16};
            GUI.Box(new Rect(12,12,790,100),"");GUI.Label(new Rect(24,18,760,32),"游击队 · 同场指挥试验",title);
            GUI.Label(new Rect(24,54,760,26),$"命令 {Battle.TacticalCommanders}层 · 攻击 ×{Battle.TacticalAttackMultiplier:0.0} · 护甲 +{Battle.TacticalArmorBonus:0} · 费用 {Battle.DP} · 剩余部署 {Battle.RemainingUses}",text);
            GUI.Label(new Rect(24,80,760,26),feedback+" · "+(Battle.Phase==BattlePhase.Won?"突破成功":Battle.Phase==BattlePhase.Lost?"本场结束":Battle.IsPaused?"已暂停":"试验中"),text);
            GUI.Box(new Rect(12,624,1256,84),"");
            for(int i=0;i<roles.Length;i++)if(GUI.Button(new Rect(24+i*150,638,142,38),ShortName(roles[i].Card.CardId)+"（1费）",button))Deploy(roles[i].Card.CardId);
            if(GUI.Button(new Rect(940,638,95,38),Battle.IsPaused?"继续":"暂停",button))Battle.SetPaused(!Battle.IsPaused);
            if(GUI.Button(new Rect(1042,638,95,38),"重开",button))ResetTrial();if(GUI.Button(new Rect(1144,638,95,38),$"×{speed}",button))speed=speed==1?3:1;
            GUI.Label(new Rect(24,678,1200,26),"同场参数试验 · 每次部署1费用 · 战术命令随存活传令兵实时变化",text);
            var living=Battle.Units.Where(u=>u.Active).ToArray();
            GUI.Box(new Rect(1008,12,260,540),"");GUI.Label(new Rect(1020,20,236,28),$"场上部队 {living.Length}",title);
            for(int i=0;i<Battle.Defenders.Count;i++)GUI.Label(new Rect(1020,52+i*25,236,26),$"守军{i+1}→{ShortName(Battle.FindUnit(Battle.Defenders[i].TargetId)?.CardId)}  {Battle.Defenders[i].Health:0}血",text);
            rosterScroll=GUI.BeginScrollView(new Rect(1018,112,240,428),rosterScroll,new Rect(0,0,216,living.Length*56));
            for(int i=0;i<living.Length;i++)
            {var u=living[i];GUI.Label(new Rect(0,i*56,216,26),$"{ShortName(u.CardId)} · {u.Health:0}生命",text);GUI.Label(new Rect(0,i*56+24,216,26),$"攻{Battle.UnitCombat(u).UnitAttack*Battle.UnitAttackMultiplier(u):0.0}  甲{Battle.UnitArmor(u):0.0}  速{u.Motion.Speed*Battle.UnitMoveMultiplier(u):0.0}",text);}
            GUI.EndScrollView();
        }
        IEnumerator Capture(string folder,string name)
        {yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);}
        IEnumerator VerifyCommands(string folder)
        {
            Directory.CreateDirectory(folder);Time.timeScale=0;bool deployed=Deploy("CARD_M15");Advance(.8f);yield return null;
            deployed&=Deploy("CARD_M16");Advance(.8f);yield return null;deployed&=Deploy("CARD_M17");Advance(.8f);yield return null;deployed&=Deploy("CARD_M17");yield return null;
            bool stacked=Battle.TacticalCommanders==2&&Mathf.Abs(Battle.TacticalAttackMultiplier-1.2f)<.001f&&Battle.Units.Select(u=>u.CardId).Distinct().Count()==3;
            yield return Capture(folder,"commands-two");bool removed=false;
            for(int i=0;i<1400;i++){Advance(.1f);yield return null;if(Battle.TacticalCommanders==0){removed=true;yield return Capture(folder,"commands-removed");break;}}
            bool deaths=Battle.Units.Where(u=>u.CardId=="CARD_M17").All(u=>u.Resolution==UnitResolution.Dead&&Battle.CombatShots.Any(s=>s.FromDefender&&s.Applied&&s.TargetId==u.EntityId));
            Battle.SetPaused(true);double clock=Battle.Clock;Advance(20);bool pause=Battle.Clock==clock;
            Debug.Log($"TACTICAL_COMMAND_CHECK: deployed={deployed}; threeRealRoles={stacked}; stacked={stacked}; naturalCommanderDeaths={deaths}; removed={removed}; attackBack={Battle.TacticalAttackMultiplier}; armorBack={Battle.TacticalArmorBonus}; pause={pause}; noInjectedDamage=True");
            ResetTrial();yield return null;bool reset=Battle.Units.Count==0&&Battle.TacticalCommanders==0;
            Application.Quit(deployed&&stacked&&deaths&&removed&&pause&&reset?0:1);
        }
        void OnDestroy(){if(material!=null)Destroy(material);if(presentation!=null)Destroy(presentation.gameObject);}
    }
}
