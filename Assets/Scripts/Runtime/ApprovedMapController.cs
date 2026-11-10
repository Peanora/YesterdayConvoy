using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        ApprovedMapDefinition activeApprovedMap;
        bool approvedMapMenuOpen;
        static bool approvedLaunchMenuShown;
        void TryOpenApprovedLaunchMenu()
        {
            if(approvedLaunchMenuShown||SceneManager.GetActiveScene().name!="MonsterTrial"||FindAnyObjectByType<ApprovedMapBootstrap>()!=null||Array.IndexOf(Environment.GetCommandLineArgs(),"-approvedMaps")<0)return;
            approvedLaunchMenuShown=true;OpenApprovedMapMenu();
        }
        public ApprovedMapDefinition ApprovedMap=>JourneyActive?JourneyMap:activeApprovedMap!=null&&TacticalMapId==activeApprovedMap.id?activeApprovedMap:null;
        public bool ApprovedMapActive=>CampaignActive&&!JourneyActive&&ApprovedMap!=null;
        public bool ApprovedMapMenuOpen=>approvedMapMenuOpen;
        public void OpenApprovedMapMenu(){approvedMapMenuOpen=true;TestSelectionOpen=true;}
        public IEnumerator PrepareApprovedMap(ApprovedMapDefinition map)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));map.Validate();
            var cards=monsterTests.Where(o=>o.Playable&&ApprovedMapPlan.StartingIds.Contains(o.Id)).Select(o=>o.Definition.Card).ToArray();
            if(cards.Length!=8)throw new InvalidOperationException("An approved map starter is missing playable art");
            return Prepare(Plan(map.encounter.defenders,cards,true),()=>{
                StartCampaign(true,encounterOverride:map.encounter);
                TestSelectionOpen=false;approvedMapMenuOpen=false;
                ShopNotice(map.index==2?"排洪渠与坑洞不可通行；本关暂不启用推击。拖动卡牌到任一红门进攻":map.index==4?"地道单向通行1.5秒，出口重新交战；拖到红门可查看完整路线":map.index==5?"热泵亮黄预警、亮橙喷发；喷发对双方造成伤害，暂停会冻结":"自由整备后开始战斗；拖动卡牌到红门部署，路线与守军均为本图配置");
            },"准备"+map.title);
        }
        void DrawApprovedMapMenu()
        {
            Fill(new Rect(0,0,1280,720),new Color(.035f,.055f,.065f,.99f));
            GUI.Label(new Rect(48,28,920,46),"战术地图",menuTitle);
            GUI.Label(new Rect(50,81,1060,30),"八张独立战场 · 选择地图后自由整备，再从固定红门发起进攻",menuBody);
            if(GUI.Button(new Rect(1050,34,180,40),"返回怪物测试",buttonStyle)){approvedMapMenuOpen=false;return;}
            foreach(var map in ApprovedMapCatalog.All)
            {
                int i=map.index-1;var r=new Rect(48+i%4*298,132+i/4*258,282,242);
                Fill(r,new Color(.09f,.135f,.155f));
                if(map.preview!=null)GUI.DrawTexture(new Rect(r.x,r.y,r.width,158),map.preview,ScaleMode.ScaleToFit);
                GUI.Label(new Rect(r.x+12,r.y+163,r.width-24,30),map.index.ToString("00")+"  "+map.title,menuCardName);
                string detail=map.index==2?"双桥排洪渠 · 坑洞禁行":map.index==4?"错位双庭院 · 配对地道":map.index==5?"三处热泵 · 外圈绕行":map.encounter.lanes.Length+"条固定路线 · 独立守军";
                GUI.Label(new Rect(r.x+12,r.y+199,r.width-24,30),detail,menuSmall);
                if(GUI.Button(r,GUIContent.none,GUIStyle.none)){CancelArtPreparation();SceneManager.LoadScene(map.sceneName);return;}
            }
            GUI.Label(new Rect(50,665,1180,30),"地道与热泵按战斗速度推进；坑洞目前仅作危险地形，尚未启用推击能力。",menuSmall);
        }
        void DrawApprovedMapRoutes(GUIStyle label)
        {
            DrawWaveRoutes();
            var selected=RoutePreviewLane();
            if(selected==null)return;
            foreach(var portal in ApprovedMap.mechanics.portals??Array.Empty<ApprovedPortal>())if(portal.routeId==selected.Id)
            {
                var p=GUIPosition(Camera.main.WorldToScreenPoint(selected.Points[portal.sourcePointIndex]));
                var q=GUIPosition(Camera.main.WorldToScreenPoint(selected.Points[portal.destinationPointIndex]));
                GUI.Label(new Rect(p.x-65,p.y-30,130,24),"地道 · "+portal.transitSeconds.ToString("0.#")+"秒",new GUIStyle(label){alignment=TextAnchor.MiddleCenter,fontSize=13});
                GUI.Label(new Rect(q.x-40,q.y-30,80,24),"地道出口",new GUIStyle(label){alignment=TextAnchor.MiddleCenter,fontSize=13});
            }
        }
    }
}
