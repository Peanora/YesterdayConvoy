using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        void DrawBlackForestRouteLabels(GUIStyle label)
        {
            DeploymentLane selected=null;
            if(shopDrag>=0&&shopMoving&&PointerGround(ScreenPosition(shopPointer),out var world))selected=Battle.FindEntryLane(world);
            foreach(var lane in Battle.Lanes){
                var p=GUIPosition(Camera.main.WorldToScreenPoint(lane.Entry));
                GUI.Label(new Rect(p.x-45,p.y-36,110,24),lane.Id+" → "+lane.ExitId,label);
            }
            if(selected==null)return;
            Color color=new Color(1,.77f,.34f);
            for(int i=1;i<selected.Points.Count;i++){
                Vector2 a=GUIPosition(Camera.main.WorldToScreenPoint(selected.Points[i-1]+Vector3.up*.04f)),b=GUIPosition(Camera.main.WorldToScreenPoint(selected.Points[i]+Vector3.up*.04f));
                ShopLine(a,b,color,2);Vector2 mid=Vector2.Lerp(a,b,.55f),direction=(b-a).normalized,wing=new Vector2(-direction.y,direction.x)*4;
                ShopLine(mid-direction*7+wing,mid,color);ShopLine(mid-direction*7-wing,mid,color);
            }
        }
        public IEnumerator PrepareBlackForest(DefenderTrialDefinition encounter)
        {
            if(encounter==null||encounter.mapId!=BlackForestLayout.Id)throw new InvalidOperationException("Missing authored forest encounter");
            return Prepare(Plan(encounter.defenders,new[]{encounter.battle.Card},true),()=>{
                EnterShopTrial(0,BlackForestLayout.Id,encounter);
                ShopNotice("地形试走：拖动备战角色到任一红门；三种特殊地面仅改变外观，暂未配置守军");
            },"准备树海交错回廊");
        }
        public IEnumerator VerifyBlackForest()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string dir=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.persistentDataPath,"BlackForestEvidence");
            bool geometry=FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None).Length==238;
            var field=FindAnyObjectByType<AuthoredBattlefield>();
            bool materials=field!=null&&field.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader.isSupported&&m.mainTexture!=null));
            bool nativeGates=field!=null&&field.GetComponentsInChildren<Transform>().Count(t=>t.name.Contains(" / Native tile_start"))==3&&field.GetComponentsInChildren<Transform>().Count(t=>t.name.Contains(" / Native tile_end"))==2&&field.GetComponentsInChildren<LineRenderer>().Length==0;
            bool routes=true,pause=true,reset=true,shared=true;
            Time.timeScale=0;SetShopVisible(false);yield return new WaitForSecondsRealtime(3.2f);yield return InteractionFrame(dir,"forest-ready");
            for(int lane=0;lane<3;lane++){
                if(lane>0)EnterShopTrial(0,BlackForestLayout.Id);
                yield return null;
                var route=Battle.Lanes[lane];int before=Battle.Defense;
                Vector2 source=ShopPieceRect(0).center,target=GUIPosition(Camera.main.WorldToScreenPoint(route.Entry));
                ProcessShopPointer(EventType.MouseDown,source);ProcessShopPointer(EventType.MouseDrag,target);ProcessShopPointer(EventType.MouseUp,target);
                bool deployed=Battle.Units.Count==1;
                var u=deployed?Battle.Units[0]:null;
                routes&=deployed&&u.RouteId==route.Id&&u.ExitId==route.ExitId;
                if(!deployed)continue;
                Battle.SetPaused(true);double clock=Battle.Clock;Vector3 start=u.Motion.Position;float health=u.Health;
                Tick(5);pause&=Battle.Clock==clock&&u.Motion.Position==start;Battle.SetPaused(false);
                for(int frame=0;frame<6000&&u.Active;frame++){
                    Tick(.05f);routes&=u.RouteId==route.Id&&u.ExitId==route.ExitId;
                    if(frame==300)yield return InteractionFrame(dir,"forest-lane-"+(lane+1));
                    if(frame%150==0)yield return null;
                }
                routes&=u.Resolution==UnitResolution.Breached&&Vector3.Distance(u.Motion.Position,route.Exit)<.01f;
                shared&=Battle.Defense==Mathf.Max(0,before-Battle.Settings.GateDamage)&&Mathf.Approximately(u.Health,health);
                EnterShopTrial(0,BlackForestLayout.Id);reset&=Battle.Phase==BattlePhase.Ready&&Battle.Units.Count==0&&TacticalMapId==BlackForestLayout.Id;
            }
            yield return InteractionFrame(dir,"forest-restarted");Time.timeScale=1;
            bool ok=geometry&&materials&&nativeGates&&routes&&pause&&shared&&reset;
            Debug.Log($"BLACK_FOREST_CHECK: geometry={geometry}; materials={materials}; nativeGates={nativeGates}; fixedRoutes={routes}; pause={pause}; sharedDefense={shared}; restart={reset}; authoredTiles=238; visualOnlyTerrain=True");
            Application.Quit(ok?0:1);
        }
    }
}
